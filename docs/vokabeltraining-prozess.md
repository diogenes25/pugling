# Vokabeltraining – Prozess- & Entstehungs-Log

> 📖 **Historischer Entstehungs-Log** (wie das Study-Plan-System in 8 Iterationen wuchs). Die
> *aktuelle* Referenz steht im Wiki: [01 · Architektur](../wiki/01-ueberblick-architektur.md),
> [04 · Lernplan bauen](../wiki/04-lernplan-bauen.md), [05 · Punkte & Bonus](../wiki/05-punkte-und-bonus.md).

Dokumentation des End-to-End-Durchlaufs „Vokabeltest in 10 Tagen" – reine API/Datenstruktur/Prozess-Phase, kein Frontend.

## 1. Ausgangslage (Anforderung von Vater Klaus)

- In **10 Tagen** schreibt Sohn **Peter** einen Vokabeltest: **10 Vokabeln**, einfache Wörter.
- Peter soll **jeden Tag mind. 20 min** alle 10 Vokabeln üben.
- Peter muss **jeden Tag einen Abschlusstest mit ≥ 80 %** bestehen – **unabhängig** davon, ob die 20 min schon erreicht sind (zwei getrennte Tagespflichten).
- Der **Lernfortschritt muss dokumentiert** sein: Klaus muss sehen, *wie* und *was* Peter gelernt hat und *wie die Tests ausfielen*.
- Der Test ist ein klassischer **Lernkarten-Test in 5 Stufen** (steigende Schwierigkeit).
- Punkte als Motivation; Klaus behält die Kontrolle.

### Die 5 Teststufen

| Stufe | Enum | Ablauf | Bewertung (Server) |
|------:|------|--------|--------------------|
| 1 | `ShowBoth` | Vokabel + Übersetzung werden angezeigt | Anzeige (immer „gesehen") |
| 2 | `SelfAssess` | Vokabel → aufdecken → „gewusst? Ja/Nein" | Selbsteinschätzung (Client meldet `wasKnown`) |
| 3 | `LetterBoxes` | Übersetzung tippen, Länge bekannt (Buchstabenfelder), Buchstaben-Tipps | getippt, normalisierter Vergleich |
| 4 | `FreeText` | Übersetzung frei eintippen | getippt, normalisierter Vergleich |
| 5 | `Audio` | Vokabel wird vorgelesen → frei eintippen | getippt, normalisierter Vergleich |

## 2. Rollen

- **Klaus (Vater)** – bedient die API, erstellt Lerngrundlagen, Lehrplan und Punkteregeln, wertet aus.
- **Peter (Sohn)** – übt und testet, gibt Feedback.
- **PM** – vermittelt zwischen Vater und Entwicklung, achtet auf Prozess/Prod-Tauglichkeit.
- **Senior C#-Dev** – setzt sauberen, wartbaren Code um.

## 3. Architektur (Ergebnis)

Aufbauend auf den bestehenden Bausteinen: Vokabel-Store (`Vocabulary`, Single Source of Truth), Personen (`Father`/`Child`), Punkte-Ledger (`ChildPointsEntry`).

### Datenmodell (`Models/StudyPlanEntities.cs`)

- **`StudyPlan`** – Lehrplan eines Kindes: Zeitraum, Tagesanforderungen (`DailyMinutesRequired`, `DailyTestRequired`, `DailyTestPassPercent`, `DefaultTestStage`, `RequireTypedTest`), Punkteregeln (`PointsMinutesMet`, `PointsTestPassed`, `PointsDayCompleteBonus`).
- **`StudyPlanItem`** – Vokabel im Plan (FK → `Vocabulary`, `Restrict`: Vokabel nicht löschbar, solange im Plan).
- **`PracticeSession`** + **`ReviewEvent`** – Übungszeit (`ActiveSeconds`) und was geübt wurde (welche Vokabel, welche Stufe, richtig?).
- **`TestAttempt`** + **`TestItemResult`** – ein Testversuch je Tag/Stufe mit Einzelergebnissen (`GivenAnswer`, `WasCorrect`, `HintsUsed`).
- **`StudyDayReward`** – protokolliert vergebene Tagesbelohnungen; **Unique-Index (Plan, Tag, Art)** → Punkte fließen nie doppelt (idempotent).

### Logik (`Services/StudyProgressService.cs`)

- `ComputeDayAsync` – berechnet je Tag: geübte Minuten, Minuten-Ziel erreicht?, bester Testscore, Test bestanden?, Tag komplett?, vergebene Punkte, **offene Pflichten** (klartext).
- `EvaluateAndAwardAsync` – wertet nach jeder Aktivität aus und schreibt fällige Punkte **idempotent** gut.
- `Normalize` – Antwortvergleich (trim, klein, Mehrfach-Leerzeichen). `IsTyped` – getippte Stufen (3–5).

### API-Endpunkte

| Zweck | Route |
|-------|-------|
| Vater: Lehrplan CRUD + Vokabeln | `POST/GET/PATCH /api/v1/study-plans`, `…/{id}/items` |
| Vater: Tag-für-Tag-Fortschritt | `GET /api/v1/study-plans/{id}/progress` |
| Vater/Sohn: Ein-Blick-Status heute | `GET /api/v1/study-plans/{id}/today` |
| Vater: Lern-Doku + Testhistorie | `GET /api/v1/study-plans/{id}/report` |
| Sohn: Üben (Zeit + Wiederholungen) | `POST …/{id}/practice-sessions`, `…/heartbeat`, `…/review`, `…/end` |
| Sohn: Test (5 Stufen) | `POST …/{id}/tests` (Start), `…/hint`, `…/submit`, `GET …/{attemptId}` |

**Prinzip:** Der Test-Start liefert die Karten **ohne Lösung** (außer Stufe 1). Getippte Stufen bekommen nur Länge (Stufe 3) bzw. Audio-URL (Stufe 5). Bewertung + Punktevergabe passieren serverseitig beim `submit`.

## 4. Prozess-Log

### Iteration 1 – Grundgerüst & Happy Path

Umgesetzt: komplettes Datenmodell, Service, drei Controller (Plan, Practice, Tests mit 5 Stufen).

**Live durchgespielt (Vater legt an, Sohn übt/testet):**

- Klaus legt 10 Vokabeln (en→de) + Lehrplan (10 Tage, 20 min, ≥80 %) an.
- Tag 1: 20 min → +10; Test 60 % (Fail) → keine Testpunkte; Test 90 % (Pass) → +20 +10 Bonus → **Tag komplett**.
- Tag 3: nur 10 min, aber Test 100 % → **Test bestanden, Tag NICHT komplett** (belegt: Test zählt unabhängig von der Zeit).
- Tag 4: 25 min geübt, Test 2× durchgefallen → **Tag NICHT komplett** (nur Minuten-Punkte).
- Vater-Report zeigt pro Vokabel Wiederholungen/Test-Treffer + vollständige Testhistorie; Punkte-Ledger stimmt (idempotent).

**Beobachtete Probleme / Erfahrungen:**

- `DateOnly` als Tages-Schlüssel funktioniert mit EF Core 10 + SQLite problemlos.
- Zum Nachstellen der 10-Tage-Laufzeit an einem realen Tag: `day`-Override auf Practice/Test-Start eingebaut. **→ Smell:** Sohn könnte damit Tage faken (siehe PM-Feedback).
- Windows-/Git-Bash-Terminal zeigt Umlaute in `json.tool` doppelt kodiert an – reines Anzeige-Artefakt, in der DB/Response korrekt (per sauberer UTF-8-Dekodierung verifiziert: „Häuser").

### Rollen-Feedback nach Iteration 1

- **Peter (Sohn):** „Macht mit Punkten Spaß, aber ich sehe nicht, welche Wörter ich *noch nicht* kann, und meinen Streak nicht während des Übens."
- **Klaus (Vater):** „Bei Stufe 2 klickt Peter einfach ‚gewusst' – das fühlt sich nach Schummeln an. Ich will echten Lernerfolg **erzwingen** können. Außerdem will ich **auf einen Blick** sehen, ob Peter heute seine Pflicht erfüllt hat."
- **PM:** „Der `day`-Override ist ein Schummel-Vektor. In Prod nur serverseitiges Datum bzw. Vater/Admin-only. Und: der eigentliche Abschlusstest sollte getippte Stufen verlangen können."
- **Senior-Dev:** „`GET` eines Testversuchs gibt ein anonymes Objekt zurück → für OpenAPI besser ein DTO. Ansonsten Code ok; Punktevergabe sauber idempotent."

### Iteration 2 – Kontrolle, Motivation, Anti-Schummel

Eingearbeitet:

- **`RequireTypedTest`** (Vater-Schalter): zählt ein Test nur als bestanden, wenn er auf einer **getippten** Stufe (3–5) läuft. Selbsteinschätzung wird dann zur reinen Übung → Klaus erzwingt echtes Wissen.
- **`GET …/today`**: Ein-Blick-Status – Pflicht heute erfüllt?, Streak, offene Pflichten (Klartext), **schwache Vokabeln** (Mastery < Bestehensgrenze) zum gezielten Üben.
- **Mastery-Quote pro Vokabel** im Report/`today` (Anteil korrekter Test-Antworten) → Klaus sieht, was Peter *wirklich* kann.
- **Clean-up:** Testversuch-`GET` liefert jetzt ein typisiertes DTO (`AttemptDetail`).

**Live verifiziert (Plan mit `requireTypedTest=true`):**

- SelfAssess 100 % → `testPassed(Tag)=false`, Tag unvollständig, Hinweis „Test muss getippt werden (Stufe ≥ Buchstabenfelder)". ✅ Anti-Schummel greift.
- LetterBoxes 90 % (getippt) → zählt, Tag komplett, +40. ✅
- `today`: `dutyDone=true`, `streak=1`, schwache Vokabel `moon` mit `mastery=50 %` (über beide Tests aggregiert). ✅

## 5. Wie die Software „zum Lernerfolg zwingt" – und Klaus die Kontrolle behält

1. **Zwei harte Tagespflichten** (Zeit **und** bestandener Test) – unabhängig, wie von Klaus gefordert. `today.dutyDone` = beides erfüllt.
2. **Anti-Schummel:** mit `RequireTypedTest` muss Peter die Übersetzung tatsächlich tippen – „gewusst"-Klicken zählt nicht.
3. **Bestehensgrenze** (`DailyTestPassPercent`, Default 80 %) von Klaus einstellbar; beliebige Wiederholungen erlaubt, aber es zählt der **beste** Versuch.
4. **Motivation:** gestufte Schwierigkeit, Buchstaben-Tipps (Stufe 3), Punkte je Teilziel + Tagesbonus, sichtbarer **Streak**, gezielte **Schwachstellen-Liste**.
5. **Volle Transparenz für Klaus:** `progress` (Tag-für-Tag), `report` (pro Vokabel + Testhistorie), `today` (Status) und der Punkte-Ledger.

### Iteration 3 – Stufen-Fahrplan (Schwierigkeits-Rampe)

Klaus wollte, dass die Schwierigkeit über die 10 Tage steigt. Umgesetzt:

- **`StudyPlan.StageSchedule`** – Liste `{ DayNumber, Stage }` (JSON-Spalte). `StudyProgressService.StageForDay` ermittelt die für einen Tag geltende Stufe (letzter passender Schritt, sonst `DefaultStage`).
- Test-Start **ohne** explizite Stufe nutzt automatisch die Tagesstufe; `today.recommendedStage` zeigt sie an.

**Verifiziert:** Fahrplan `Tag1→1, Tag3→2, Tag5→3, Tag7→4, Tag9→5`; `today` an Tag 1 → Stufe 1; Test-Start (Override Tag 5) → automatisch `LetterBoxes` (Stufe 3). ✅

### Iteration 4 – Zweites Lernverfahren „Lückentext" (mit Generalisierung)

**Design-Runde der 4 Beteiligten:**

- **PM:** „Lückentext ist ein zweites Verfahren. Kopieren wir Plan/Zeit/Punkte/Test, divergieren die Pfade. Der Rahmen muss verfahrensneutral werden."
- **Senior-Dev:** „Ich generalisiere den Rahmen, statt zu duplizieren. Vokabel-Flow wird danach neu getestet."
- **Sohn:** „Lückentext soll auch Stufen haben und sich anfühlen wie ein Spiel (Wortpool zum Antippen)."
- **Klaus:** „Zeit, Punkte und Mindest-Testquote gelten wie immer."

**Generalisierung (verfahrensneutraler Rahmen):**

- `StudyPlan.Method` (`Vocabulary` | `Cloze`). `StudyPlanItem` referenziert **Vokabel ODER Lückentext** (zwei nullbare FKs, `ContentId` als neutraler Bezug).
- `TestAttempt` wird neutral: `StageValue` (int) + **`Graded`** (zählt der Versuch als „echt"?). Der `StudyProgressService` kennt keine Stufen-Enums mehr – er liest nur `Passed`/`Graded`/Minuten.
- `TestItemResult`/`ReviewEvent` nutzen `ContentId` (+ `GapIndex` bei Lücken). `report`/`today` aggregieren generisch (`ItemStat`).

**Lückentext-Verfahren:**

- **`ClozeText`-Store** (Lerngrundlage, `api/v1/learn/cloze-texts`): Text mit `{{1}}`-Platzhaltern, `Gaps` (Lösung + Alternativen), optionaler `WordBank` und Übersetzung – Gaps/WordBank als JSON-Spalten, eindeutiger `Key`, Löschschutz solange in einem Plan verwendet.
- **4 Stufen** (`ClozeStage`), die zwei Hilfen togglen:

  | Stufe | Enum | Übersetzung? | Wortpool? | gewertet? |
  |------:|------|:---:|:---:|:---:|
  | 1 | `WordBank` | – | ✓ | – |
  | 2 | `TranslationWordBank` | ✓ | ✓ | – |
  | 3 | `TranslationFreeText` | ✓ | – | ✓ (Freitext) |
  | 4 | `FreeText` | – | – | ✓ (Freitext) |

- **Cloze-Tests** (`api/v1/study-plans/{id}/cloze-tests`): Start liefert Texte mit Lücken (ohne Lösungen), je Stufe passend Übersetzung/Wortpool; Tipp deckt Zufallsbuchstaben (nur Freitext-Stufen); Submit bewertet **pro Lücke** (normalisiert, Alternativen erlaubt), Score = korrekte Lücken / Gesamtlücken; Punkte über den geteilten `StudyProgressService`.

**Verifiziert (Vater legt an, Sohn testet alle 4 Stufen):**

- Stufe 1: Wortpool sichtbar, keine Übersetzung; Alternativen „Hi/good" akzeptiert → 100 %.
- Stufe 2: Übersetzung + Wortpool sichtbar.
- Stufe 3: Übersetzung, kein Wortpool; Tipp liefert Buchstabe; getippt 100 % → **gewertet**, +20.
- Stufe 4: keine Hilfen; 1 Lücke falsch → 75 % (= Grenze) bestanden.
- Vater-`report` (generisch): Mastery pro Lückentext über Gap-Treffer (cz_family 83 %); Testhistorie mit Stufe + `graded`.
- **Beide Verfahren teilen denselben Punkte-Ledger** des Kindes. **Kein Regress** im Vokabel-Flow (Fahrplan/Start/Submit weiter grün).

### Iteration 5 – Auth & Rollenverteilung

**Design-Runde:**

- **PM:** „PINs sind schon da. Proportional: PIN-Login → JWT mit Rollen (`Vater`/`Sohn`) + Eigentums-Prüfung. Der `day`-Backfill wird Vater-only – damit ist der Schummel-Vektor geschlossen."
- **Senior-Dev:** „JWT-Bearer, `AuthAccess`-Service für Ownership, Swagger-Authorize-Button. Bootstrapping: Vater-Anlage bleibt anonym (Registrierung)."

**Umgesetzt:**

- **Login** (`/api/v1/auth/father`, `/api/v1/auth/child`) per Id + PIN → signiertes JWT (`TokenService`, `JsonWebTokenHandler`, HS256, 12 h). Claims: Rolle, `fid`/`cid`, Name. `Child.Pin` ergänzt.
- **Rollen:** `[Authorize(Roles=Vater)]` auf Stores (Vokabel/Lückentext), Katalog, Admin (Fathers/Children) und allen Plan-Mutationen. `[Authorize]` (authentifiziert) auf Lesen/Üben/Testen.
- **Ownership** (`AuthAccess`): Sohn nur eigene Pläne (`plan.ChildId == cid`), Vater nur Pläne eigener Kinder. In Practice/Tests/Cloze zentral über einen `IAsyncActionFilter` (planId → Existenz + Ownership); Children-Admin über `IActionFilter` (Route-`fatherId` == Token-`fid`).
- **Anti-Schummel:** `day`-Backfill (Tag ≠ heute) nur für Vater.
- **Swagger:** OpenAPI-Dokument-Transformer fügt ein `Bearer`-Schema + globale Security-Anforderung hinzu → „Authorize"-Button.

**Verifiziert (live):**

- Ohne Token → **401**; falscher PIN → **401**; `me` liefert korrekte Claims.
- Sohn: Plan anlegen / Vokabel-Store lesen+schreiben → **403** (Rolle); eigenen Plan lesen, `today`, Üben, Test → **200/201**; Backfill → **403**.
- Vater: Backfill → **201**; eigener Plan/Progress → **200**.
- **Cross-Family:** Vater2 auf Plan/Kinder von Familie 1 → **403**; eigene → **200**.
- OpenAPI enthält `securitySchemes: [Bearer]` + globale Security.

**Erfahrungen/Stolpersteine:**

- Öffentliche Filter-Interface-Methoden (`OnActionExecuting…`) werden von MVC als Actions interpretiert → Startup-Crash. Fix: `[NonAction]`.
- Microsoft.OpenApi **2.x** hat Breaking Changes: Namespace `Microsoft.OpenApi` (nicht `.Models`); `doc.Security` statt `SecurityRequirements`; Referenzen via `OpenApiSecuritySchemeReference`; `Components.SecuritySchemes` ist per Default `null` (muss initialisiert werden, sonst NRE im Transformer).
- `Jwt:Key` hat einen Dev-Fallback; in Prod zwingend über Konfiguration setzen.

### Iteration 6 – Drittes Lernverfahren „Zuordnung / Matching"

**Design-Runde:** Da Matching Wort ↔ Übersetzung zuordnet, nutzt es den **bestehenden Vokabel-Store** als Inhalt – kein neuer Content-Store nötig. Nur Test-Mechanik + Stufen sind neu; Plan/Zeit/Punkte/Practice/Fortschritt/Auth kommen unverändert aus dem generischen Rahmen.

**4 Stufen (`MatchStage`), objektiv – daher immer `Graded`:**

| Stufe | Enum | Prompt → Auswahl | Ablenker? |
|------:|------|------------------|:---:|
| 1 | `Direct` | Wort → Übersetzung | – |
| 2 | `Distractors` | Wort → Übersetzung | ✓ |
| 3 | `Reverse` | Übersetzung → Wort | – |
| 4 | `ReverseDistractors` | Übersetzung → Wort | ✓ |

- **`MatchingTestsController`** (`api/v1/study-plans/{id}/matching-tests`): Start liefert die Prompts und einen **gemischten Auswahl-Pool** (korrekte Antworten + bei Ablenker-Stufen zusätzliche Einträge aus anderen Vokabeln); Submit ordnet je Vokabel eine Auswahl zu und bewertet gegen die erwartete Seite (normalisiert). Punkte über den geteilten `StudyProgressService`.
- Nur `LearningMethod.Matching` änderte sich am Modell (+ `MatchStage`, + Default-Stufe). **Kein** neuer Store, **keine** Änderung am Rahmen – der Beleg, dass die Generalisierung aus It. 4 trägt.

**Verifiziert (als angemeldeter Sohn):** Stufe 1 korrekt → 100 %, +20 Punkte; Stufe 4 mit gewähltem Ablenker („house" statt „dog") → 80 % bestanden; Pool enthält in Ablenker-Stufen echte Fremd-Wörter; Vokabel-Test-Endpoint auf Matching-Plan → 400; Fahrplan/`today` liefern die Tagesstufe.

### Iteration 7 – Stundenplan-Steuerung + Inhalts-Bewertung durch den Sohn

**Use-Case:** Der Lernplan richtet sich nach dem Schul-Stundenplan – am Tag vor dem Unterricht wird der bereits gelernte Stoff wiederholt (Vorbereitung), am Unterrichtstag neuer Stoff gelernt. Zusätzlich bewertet der Sohn Übungen 5-stufig.

**Umgesetzt (verfahrensneutral, nutzt den generischen Rahmen):**

- **`TimetableEntry`** (Kind × Katalog-Fach × Wochentag) + `StudyPlan.SubjectId` koppeln Plan und Stundenplan; `NewItemsPerLesson` steuert die Einführungs-Chargengröße.
- **`StudyPlanItem.IntroducedAt`** trennt „schon gelernt" von „neu" (explizite Einführung).
- **`ScheduleService`** leitet je Tag den Modus ab: Unterrichtstag → **New** (nächste `NewItemsPerLesson` un-eingeführte Inhalte, danach als eingeführt markiert), sonst → **Review** (bereits eingeführte); Tag davor = Vorbereitung. Ohne Fach/Stundenplan unverändert (alle Inhalte).
- Alle drei Test-`Start`-Endpunkte ziehen automatisch den **Tages-Pool**; `today` liefert Modus, Vorbereitungs-Flag, Grund und die fälligen Inhalte.
- **`ContentRating`** + `RatingsController`: Sohn bewertet einen Plan-Inhalt (`SehrGut/Gut/Neutral/Schlecht/Fehler` + Kommentar); der Vater sieht die Bewertungen im `report`.

**Verifiziert (live, mit Auth):** Englisch dienstags → Di = New (dog/cat), nächster Di = nächste Charge (sun/tree); Mo/Sa = Review der eingeführten; `today` zeigt Modus + fällige Inhalte; Sohn-Bewertung erscheint im Vater-Report; Bewertung eines fremden Inhalts → 400; Sohn darf Stundenplan nicht pflegen → 403.

**Erfahrung:** Dank der verfahrensneutralen Basis kam das Feature ohne Änderung an `TestAttempt`/`StudyProgressService` aus – nur Auswahl-Logik (Service) + zwei kleine Entitäten + Scoping im `Start`. Die 5-stufige Bewertung liefert dem Vater die Signale, um den Plan an den echten Unterricht anzupassen (z. B. „Schlecht = noch nicht behandelt" → zurückstellen, „Fehler" → Übung korrigieren).

### Iteration 8 – Leitner-Boxen vereinheitlichen (Karteikasten im neuen System)

**Ausgangslage:** Echtes Karteikasten-Lernen (Boxen 1–5, Intervalle 1/2/4/7/14, Fälligkeit) gab es nur im **Legacy-System** (`Topic`/`VocabCard` + `SessionsController`, `User`-basiert, Vater-only, global). Das neue Study-Plan-System terminierte Wiederholungen bis dahin über den Stundenplan (Unterrichtstag = neu, sonst *alle* eingeführten) – ohne Fälligkeit pro Karte. Es gab also zwei parallele „Karteikästen".

**Design-Runde:**

- **PM:** „Zwei Mechanismen sind ein Wartungsrisiko. Leitner gehört ins neue System – mit Vater/Kind-Auth, Punkten pro Kind und Reporting, die dort schon existieren."
- **Senior-Dev:** „Additiv und opt-in pro Plan (`UseLeitner`), damit kein Regress an Vokabel/Cloze/Matching/Stundenplan entsteht. Da ein `StudyPlan` genau einem Kind gehört, ist Box-State pro `StudyPlanItem` automatisch pro Kind – kein neues ‚State-pro-Kind'-Konstrukt nötig."
- **Klaus (Vater):** „Ich will Intervalle einstellen können und im Report sehen, in welcher Box eine Karte steht und wann sie wieder dran ist."

**Umgesetzt (additiv, opt-in, verfahrensneutral):**

- **`StudyPlan`**: `UseLeitner`, `MaxBox` (Standard 5), `BoxIntervalDays` (JSON-Spalte, Standard `[0,1,2,4,7,14]`). **`StudyPlanItem`**: `Box`, `DueOn`, `ReviewCount`, `LastReviewedAt` (= Box-Zustand pro Kind).
- **`ScheduleService.ApplyReview`** kapselt die Leitner-Mathematik (richtig → eine Box höher, Fälligkeit + Intervall; falsch → Box 1, sofort wieder fällig). `SelectAsync` filtert den Wiederholungs-Pool bei `UseLeitner` auf die **fälligen** Karten (sortiert nach Box/Fälligkeit); das Stundenplan-Verhalten (New/Review) bleibt unverändert.
- **`PracticeSessionsController.Review`** ist **server-autoritativ**: der Sohn schickt seine Antwort (`givenAnswer`/`gaps`, bzw. `wasKnown` nur für Anzeige-/Selbsteinschätzungs-Stufen), der Server bewertet sie über den gemeinsamen `AnswerGrader` gegen die Lösung, schreibt bei Leitner-Plänen die Box fort **und bucht Punkte ins Kind-Ledger** (`ChildPointsEntry`); Rückgabe `{ wasCorrect, expected, awarded, box, dueOn, combo, comboBonus }`. Selbsteinschätzungs-Stufen bei `RequireTypedTest` sowie Nicht-Leitner-Pläne liefern `204` (nur Protokoll, kein Regress). Lösungsfreie Übungskarten kommen über `GET …/{sid}/cards`.
- **`PointsService`**: die box-gewichtete Formel (neu = 10, Wiederholung = `max(2, 8−Box)` × Zeitfenster-Multiplikator) als `(reviewCount, box)`-Überladung freigelegt; der Legacy-`VocabCard`-Pfad delegiert darauf (DRY).
- **Reporting**: `report`/`today` zeigen pro Karte `Box`/`DueOn`; `today.dueItems` spiegelt die fälligen Karten.
- **Konverter vereinheitlicht:** `POST …/matching/{id}/to-study-plan` erzeugt aus den Paaren einer Zuordnungs-Übung Vokabeln (Wort = links, Übersetzung = rechts, stabiler Key → idempotent) und einen `StudyPlan` (`Method=Matching`, `UseLeitner=true`), ans Katalog-Fach gekoppelt. Der frühere `to-flashcards` (Legacy-`Topic`) entfällt.
- **Legacy als veraltet markiert:** `VocabController` (`/api/v1/vocab`) und `SessionsController` (`/api/v1/sessions`) mit Deprecation-Hinweis + Swagger-Tag „Deprecated – Legacy …" und Verweis auf den Nachfolger. Die Entitäten selbst bleiben ohne `[Obsolete]` (sonst Warnungs-Kaskade über den geteilten `PointsService`); sie sind nur noch für Altbestände.

**Verifiziert (frische DB, echte Auth):**

- Leitner-Vokabelplan: richtig → Box 1→2, Fälligkeit +2 Tage, +10 ins Kind-Ledger; falsch → Box 1, sofort fällig, 0 Punkte. `today.dueItems` enthält nur die fälligen Karten. Nicht-Leitner-Plan → `review` weiter `204`.
- Konverter: Erdkunde-Zuordnung (16 Paare) → Plan mit 16 Karten (Box 1), ans Fach gekoppelt; der **Matching-Test läuft unverändert auf dem erzeugten Plan** (Beleg, dass die generierten Vokabeln voll integriert sind); alter `to-flashcards` → 404.

**Erfahrung:** Der generische Rahmen aus It. 4 trug erneut – Leitner klinkte sich als opt-in ein, ohne Test-/Fortschritts-Logik zu berühren. Die UTC-basierte Tagesrechnung (`DateTime.UtcNow`) ist konsistent über Session-Start, Auswertung und Fälligkeit; ein Test schlug nur scheinbar fehl, weil das Prüfskript die lokale statt der Server-Uhr nahm – der `today`-Endpunkt (Server-Uhr) bestätigte die Fälligkeits-Semantik korrekt.

## 6. Bewusst offen / nächste Schritte

- **Auth:** `day`-Override und Sohn/Vater-Trennung sind bis zur Auth-Einführung ungeschützt. `day` ist als **Backfill/Test-Feature** markiert und sollte in Prod Vater/Admin-only sein.
- **Stufen-Progression:** aktuell wählt man die Teststufe je Versuch; denkbar ist ein Fahrplan (Tag 1–2 Stufe 1–2, später 3–5) als Plan-Eigenschaft.
- **Score-Toleranz:** bei Substantiven ggf. Artikel tolerieren; Tippfehler-Nachsicht (Levenshtein).
- **Erinnerungen/Warnungen:** Push wenn Tagespflicht offen oder ein Tag verpasst wurde.
- **Audio:** `pronunciationAudioUrl` an den Vokabeln pflegen (Blob/CDN) für Stufe 5.
- **Schema-Management:** Umstieg von `EnsureCreated()` auf EF-Migrationen (bei bestehender `pugling.db` müssen die neuen Tabellen sonst per DB-Neuanlage entstehen).
