---
tags: [typ/protokoll, bereich/produkt, rolle/creator, rolle/supervisor, rolle/student]
aliases: [PM-Sitzung 2026-08-04, Runde "Was angelegt ist, kommt an"]
---

# PM-Sitzung: Was der Creator anlegt, muss beim Kind ankommen

**Datum:** 2026-08-04 · **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Die nächste Runde bestimmen — mit am laufenden Produkt belegten Befunden statt aus dem Backlog
abgeschrieben.

## Ausgangslage

Seit der letzten Sitzung (2026-07-05) ist die Arbeit vom PM-Protokoll in den
[Backlog](backlog/README.md) gewandert: 94 Stories, davon **~50 auf `geschaetzt`** (baubereit),
zuletzt abgenommen B-08, B-10, B-73. **Offene P1: genau eine** (B-07, XS, Betriebsschritt DB-Umbau).
Der Anmerkungs-Befund [2026-08-02](anmerkungen/befund-2026-08-02.md) (#1–#13, echter Nutzer, echte App)
ist vollständig in Stories geerntet (B-63…B-69) — davon B-65 und B-69 bereits abgenommen.

**Konsequenz für diese Runde:** Der Wertbeitrag ist nicht das Sammeln, sondern das **Ordnen** und das
**Nachprüfen am laufenden Stand**. Es wurde nichts Neues erfunden.

### Was heute wirklich beobachtet wurde (nicht abgeschrieben)

Backend auf `:5200` gestartet, Frontend lief auf `:5173`; gespielt als **Demo-Vater** (Adult 3) und
**Demo-Kind** (Child 2) gegen den geseedeten Plan 1 („Demo: Alle Lernarten", 16 Positionen).

| Befund | Beobachtung am laufenden Produkt | Story |
|---|---|---|
| Birkenbihl liefert seine Methode nicht aus | `GET creator/exercises/3` trägt die volle Dekodierung (`What→Was`, `is→ist`, `your→dein`, …). Die Karte des Kindes (`…/positions/12/practice-sessions/1/next`) liefert `prompt: "What is your name?"` und **kein Feld für die Dekodierung**. Der Creator hat sie angelegt, der Sohn sieht sie nie. | [B-78](backlog/B-78-birkenbihl-dekodierung-erreicht-kind-nicht.md) |
| Eine unsinnige Stufe verschenkt die Lösung | `PATCH …/positions/3 {"stage":99}` → **200**. Danach die Karte des Kindes: `stage: 99`, `reveal: "hallo"`. Kein Fehler, keine Warnung — die Position gibt die Lösungen heraus. (Position danach auf `stage: 6` zurückgesetzt und geprüft.) | [B-79](backlog/B-79-position-stufe-unvalidiert.md) |
| Das Aufdecken zeigt nur eine von mehreren richtigen Übersetzungen | Der Store trägt seit B-65 `translationAlternatives`; `PositionPlayService.CardFacets` reicht als `Reveal` ausschließlich `item.Answer` durch ([PositionPlayService.cs:140-172](../backend/Pugling.Api/Services/Shared/PositionPlayService.cs)). Beobachtet: Karten der Stufe 1 kommen mit genau einem `reveal`. | [B-70](backlog/B-70-selbsteinschaetzung-nur-primaerloesung.md) |

## Runde 1 — Feedback der drei Rollen

Die O-Töne stützen sich auf die drei Beobachtungen oben, auf den Nutzer-Befund vom 2026-08-02 und auf
die code-belegten Ist-Stände der `geschaetzt`-Stories. Nichts davon ist geraten.

### Feedback Creator (O-Ton)

**Baut sich gut:** Fach → Kapitel → typisierte Übung läuft; ich kann alle Lernarten anlegen, die
Vorschau vor dem Zuweisen gibt es, der Vokabelspeicher trägt inzwischen mehrere gleichwertige
Übersetzungen.

**Fehlt / nervt:**

1. **Ich lege Inhalt an, der nicht ankommt.** Ich habe zu jedem Satz die Wort-für-Wort-Dekodierung
   getippt — das *ist* die Birkenbihl-Methode. Beim Kind kommt eine gewöhnliche Übersetzungskarte an.
   Dann kann ich mir die Arbeit auch sparen.
2. **Das Lehrwerk ist eine Ebene Freitext.** Verlag, Reihe, Band, Unit, Themen, Grammatik — alles
   einzeilige Textfelder. Ich pflege dieselbe Reihe dreimal und kann nichts wiederverwenden.
3. **Der Fachlehrer fragt mich nach Fach und Sprachen, die im gewählten Lehrwerk längst stehen.**

**Top-3:** B-78 · B-63 (Träger, mit B-64/B-66-Umfeld) · B-67

### Feedback Vater (O-Ton)

**Gefällt:** Der Plan mit Positionen, eigenen Zielen und Zeitfenstern je Pflicht sitzt (B-10). Die
Lösungslecks der letzten Runden sind zu (B-80/81/82) — darauf verlasse ich mich jetzt.

**Stört / fehlt:**

1. **Ich kann eine Position lautlos kaputtmachen.** Ich tippe eine Stufe falsch, bekomme ein
   freundliches „gespeichert" — und verschenke damit die Lösungen an mein Kind. Genau die Sorte
   Selbstbetrug, wegen der ich diese App überhaupt haben wollte.
2. **Kein Überblick über mehrere Kinder** (B-39) und **der Lehrplan-Assistent hat keinen Durchstich**
   (B-58) — ich weiß nicht, ob er morgen noch tut, was er heute tut.
3. **Die Punkte-Empfehlung des Creators kann ich nicht übernehmen** (B-45) — ich tippe Zahlen ab.

**Top-3:** B-79 · B-39 / B-58 · B-45

### Feedback Sohn (O-Ton)

**Mega:** Die Karten kommen jetzt vollständig an (Lesetext, Lückenadresse, Reihenfolge) — vorher war
das Raten. Zwei richtige Übersetzungen werden beim Tippen beide gezählt.

**Nervt:**

1. **Beim Aufdecken sehe ich nur eine Lösung.** Ich denke „sehr groß", aufgedeckt wird „riesig", und
   ich muss mich selbst als falsch eintragen. Ich hatte es *richtig*.
2. **Bei den Buchstabenkästchen tippe ich Leerzeichen und Kommas**, die sowieso feststehen. Das ist
   Fummelei, kein Lernen.
3. **Birkenbihl ist einfach nur wieder Übersetzen.** Wo ist das Wort-für-Wort, das mir das erklärt?

**Top-3:** B-70 · B-66 · B-78

## PM-Synthese & Priorisierung

**Die Beobachtung, die die Runde bestimmt:** Alle drei Rollen zeigen — unabhängig voneinander — auf
**dieselbe Naht**: die Strecke zwischen dem, was der Creator anlegt / der Vater einstellt, und dem,
was beim Kind auf der Karte landet. Der Creator beklagt Inhalt, der nicht ausgeliefert wird (B-78);
der Vater eine Einstellung, die lautlos die Lösung verschenkt (B-79); der Sohn eine Rückmeldung, die
seine richtige Antwort verschweigt (B-70). Das ist **kein Sammelsurium, sondern ein Riss** — und es
ist derselbe Riss, den die letzten Runden von der anderen Seite bearbeitet haben (B-75/76/77 Inhalt,
B-80/81/82 Lecks, B-65 Alternativen).

### Roter Faden: „Was angelegt ist, kommt beim Kind richtig an."

| Prio | Story | Größe | Wo | Warum jetzt |
|---|---|---|---|---|
| **P0 dieser Runde** | [B-78](backlog/B-78-birkenbihl-dekodierung-erreicht-kind-nicht.md) Birkenbihl-Dekodierung erreicht das Kind | M | beides | Ein ganzer Übungstyp führt seine Methode nicht aus, obwohl die Daten da sind. Vom Creator **und** vom Sohn benannt. Live belegt. |
| **P0 dieser Runde** | [B-79](backlog/B-79-position-stufe-unvalidiert.md) Stufe wird gegen nichts geprüft | S | backend | Verschenkt Lösungen ohne jede Rückmeldung — dieselbe Klasse wie die geschlossenen Lecks, nur über den Vater. Live belegt. |
| **P0 dieser Runde** | [B-70](backlog/B-70-selbsteinschaetzung-nur-primaerloesung.md) Aufdecken zeigt nur die primäre Übersetzung | S | beides | Letzte Meile von B-65: das Kind bestraft sich für eine richtige Antwort. Am Malus (`PenaltyCoins`) hängt echtes Geld. |
| P1 Mitläufer | [B-66](backlog/B-66-buchstabenkaestchen-trennzeichen.md) Trennzeichen im Buchstabenkästchen | M | beides | Sitzt in derselben Karten-Projektion (`CardFacets`/`answerLength`) — billiger zusammen als einzeln. Nur, wenn die drei P0 sitzen. |
| P2 nächste Runde | [B-63](backlog/B-63-lehrwerk-hierarchie.md) (+ B-64, B-67) Lehrwerk-Hierarchie | L | beides | Der größte Kundenwunsch (6 von 13 Anmerkungen), aber Migration + eigene Runde. B-67 (S) ist der billigste Nutzen daraus. |
| P2 nächste Runde | [B-39](backlog/B-39-supervisor-dashboard.md) · [B-58](backlog/B-58-assistent-e2e.md) · [B-45](backlog/B-45-creator-punkte-empfehlung.md) | L/S/S | beides | Vater-Überblick und Absicherung — wichtig, aber kein Riss. |
| P3 | [B-07](backlog/B-07-db-umbau-restetappen.md) | XS | Betrieb | Einziges offenes P1 im Backlog, aber ein **Betriebsschritt** — gehört an den Deploy, nicht in eine Feature-Runde. |

**Warum nicht der Lehrwerk-Block zuerst,** obwohl er die meisten Anmerkungen trägt: Er ist `L` mit
Migration und berührt Verlag/Reihe/Band/Unit in einem Schnitt (der Befund rechnet sonst mit *drei*
Migrationen an derselben Tabelle). Er verdient eine eigene Runde mit ausgeruhtem Kontext. Der Riss
oben dagegen macht **heute** ausgelieferte Funktionen unwahr — und zwei der drei Teile sind `S`.

**Nichts Neues zu erfassen:** Jeder heute beobachtete Punkt hat bereits eine Story mit code-belegtem
Ist-Stand. Das ist selbst ein Ergebnis — der Backlog ist gegenüber dem laufenden Produkt vollständig.

## Entwickler-Brief (roter Faden, Reihenfolge Backend zuerst)

1. **B-79 zuerst** (S, rein Backend, kein Vertragsbruch): Stufe gegen `IExerciseType.StageOptions`
   prüfen in `POST`/`PATCH` `…/study-plans/{planId}/positions`, additiver Fehlercode in `ApiErrors`,
   Integrationstest für „unbekannte Stufe → 400" **und** für die Kehrseite (bekannte Stufe bleibt 200).
   Zuerst, weil es das Tor ist, hinter dem die beiden anderen spielen.
2. **B-70** (S, beides): `Reveal` in `PositionPlayService.CardFacets` um die gleichwertigen
   Übersetzungen erweitern — die *eine* geteilte Stelle, an der Übung, Test **und** Vater-Vorschau
   hängen. Anti-Cheat-Regel unverändert: nur wo die Lösung ohnehin gezeigt wird (`typed == false`).
3. **B-78** (M, beides): Dekodierung aus `ExerciseConfigs` in die Karte tragen (`BirkenbihlExerciseType`
   liest `s.Decoding`), Vertrags-DTO ergänzen, Sohn-Arcade zeigt Wort-über-Wort. Fallstrick aus der
   Story: die Position steht mit `GoalCadence.None` im Seed — der Durchstich muss sie also spielen,
   nicht nur die Pflichtpositionen.
4. **B-66 nur danach** und nur, wenn 1–3 grün und reviewt sind.

**Verifikation je Schritt:** `dotnet test Pugling.sln -c Release` mit echter Zahl, `pugling-reviewer`
vor jeder Abnahme, für die Karten-Änderungen zusätzlich `/smoke-test`, für die Arcade ein E2E.

## Iteration 1 — umgesetzt (B-79 → B-70 → B-78)

Reihenfolge wie gebrieft: Backend zuerst, das Tor vor den beiden Ausspiel-Fixes.

### B-79 — die Stufe wird geprüft (Backend)

- Neue Prüfung `StageProblem` im `PlanPositionsController`, aufgerufen in `Create` **und** `Update` vor
  dem ersten Schreiben; prüft `stage` **und** jeden Schritt des `stageSchedule` gegen
  `IExerciseType.StageOptions`. Fehler ist der bestehende `ApiErrors.ValidationError` (kein neuer
  Vertragswert), die Meldung nennt die erlaubten Stufen mit Namen.
- **Beim Bauen aufgefallen und entschieden** (die Story hatte es nicht sehen können): `TestStage.ShowBoth`
  — die Kennenlern-Stufe, die der Seed benutzt (`Seed.cs` einmal als `Stage`, einmal im `StageSchedule`) und
  die [docs/vokabeltraining-prozess.md](vokabeltraining-prozess.md) als Stufe 1 führt — **fehlte in
  `StageOptions`**. Die Prüfung hätte sie also abgewiesen. Statt die Regel aufzuweichen ist die Liste
  vervollständigt worden (`ShowBoth` als „Beide zeigen (Kennenlernen)"): `StageOptions` ist damit *die*
  Stufenmenge eines Typs, nicht nur ein Vorschau-Menü. Nebenwirkung: die Vater-Vorschau bietet die Stufe
  jetzt auch an — richtig, denn zuweisen ließ sie sich immer schon.
- Damit dieselbe Falle nicht wiederkommt: neuer Wächter
  `ExerciseTypeManifestTests.StageOptions_Enthalten_JedenWert_DesZugehoerigenStufenEnums` mit **gepinnter**
  Zuordnung (Vokabel→`TestStage`, Cloze→`ClozeStage`) und der Gegenprobe, dass jeder andere Typ seine
  leere Liste behält.

### B-70 — das Aufdecken zeigt jede gleichwertige Antwort (beides)

- `PositionPlayService.CardFacets` liefert zusätzlich `RevealAlternatives` — an derselben
  Anti-Cheat-Grenze wie `Reveal` (`typed` → beides `null`), gefüllt aus `AcceptedAnswers` ab Index 1.
- Additiv im Vertrag: `PracticeCard`, `TestItem`, `PreviewItem`; alle drei Aufrufer verdrahtet.
- Frontend: **eine** geteilte Komponente `RevealAlternatives` („auch richtig: …") statt dreier Kopien,
  eingebunden in Sohn-Übung, Sohn-Klausur und Vater-Vorschau.

### B-78 — die Dekodierung erreicht das Kind (beides)

- `ContentItem.Decoding` (additiv), `BirkenbihlExerciseType.ItemsOf` liest `s.Decoding`, `CardFacets`
  reicht sie **unbedingt** durch (Material, keine Lösung — dieselbe Regel wie `Passage`).
- `BirkenbihlExerciseType.IsTypedStage => false`: die Methode lernt durch Lesen, der geerbte Standard
  `true` hatte ein Eingabefeld neben die Dekodierung gestellt.
- Frontend: neue Komponente `BirkenbihlDecoding` (je Wort eine Spalte, Lernwort über Gloss; die **Spalte**
  ist die umbrechende Einheit, sonst zeigt die Ausrichtung auf das falsche Wort) auf der Vorderseite der
  Karte und in der Vater-Vorschau.

### Verifikation (gemessen, nicht behauptet)

| Was | Ergebnis |
|---|---|
| `dotnet test Pugling.sln -c Release` | **708/708 grün**, 0 Warnungen |
| Frontend `npm test` (Vitest/RTL) | **116/116 grün** (3 neue Fälle in zwei neuen Komponententests) |
| Frontend `npm run build` (`tsc -b` + Vite) | sauber |
| Live am laufenden Server (`:5200`, Demo-Plan) | Birkenbihl-Karte trägt jetzt die Dekodierung (`What→Was`, `is→ist`, `your→dein`, `name→Name`) **und** `reveal` statt einer Tipp-Aufforderung; `PATCH …/positions/3 {"stage":99}` → **400 `validation_error`** mit der Liste der erlaubten Stufen, `{"stage":6}` weiterhin **200** |
| B-70 | über den Integrationstest an der **echten** Card-Antwort belegt (`revealAlternatives` enthält die Alternative, auf getippter Stufe `null`) plus Komponententest — **nicht** live nachgespielt: im Demo-Datenstand trägt keine Vokabel erklärte Alternativen, und dafür welche anzulegen hätte Testdaten in den Demo-Bestand geschrieben |
| Neue Tests | B-79: zwei Fälle (unbekannte Stufe abgewiesen inkl. `stageSchedule`, Typ ohne Stufenwahl nimmt weiter jeden Wert) + Wächter · B-70: Selbsteinschätzung deckt Alternativen auf / getippt nicht, Richtungstausch deckt keine auf · B-78: Dekodierung erreicht Kind **und** Vorschau ohne Tippen, plus die Assertion, die im alten Test fehlte |

Die Demo-Position 3 stand für die Live-Probe kurz auf `stage: 99` und steht wieder auf `6` (nachgeprüft).

## Reviews — und was sie geändert haben

Beide Reviews (`pugling-reviewer`, `frontend-reviewer`) melden **keinen Blocker**. Sie haben die Runde
trotzdem verbessert; das Wichtigste zuerst:

- **Eine zweite Tür zum selben Schaden**, die B-79 übersehen hatte: `Exercise.DefaultStage` wird vom
  Creator-Pfad ungeprüft geschrieben, und die Ausspielung fällt darauf zurück, sobald die Position keine
  Stufe nennt — für die meisten Typen der Normalfall. Ein `defaultStage: 99` hätte weiter die Lösung
  verschenkt. Die Prüfung sitzt jetzt als geteilter Helfer (`StageValidation`) hinter **beiden**
  Schreibpfaden, mit eigenem Test. Ohne das Review wäre B-79 halb behoben abgenommen worden.
- **Testlücke bei B-78:** geprüft war nur die Form der Karte, nicht das Durchspielen. Der Test spielt die
  Position jetzt und belegt den bis dahin unbespielten Pfad „nicht getippt + kein Item-Fortschritt"
  (0 Punkte, Box unbewegt, kein Fehler) — genau das Risiko, das die Story selbst notiert hatte.
- **A11y:** Wort und Bedeutung waren nur *visuell* ein Paar; ein Screenreader las „HowWiearebist". Jetzt
  trägt jedes Paar einen vorgelesenen Trenner, ein Wort ohne Bedeutung bekommt keinen.
- **Zwei CSS-Fallen:** die „auch richtig"-Zeile stand mit UA-Vorgabemargen ~20 px von ihrer Lösung entfernt
  (Abstand jetzt in der Klasse, nicht als Prop an zwei Aufrufern); und `justify-content: center` gilt nur
  noch in der Sohn-Karte — im linksbündigen Vater-Testmodus begann die Dekodierung mittig unter einem
  linksbündigen Satz, womit die Ausrichtung, die ihr Zweck ist, dort verloren war.
- Dazu Kleinkram: eine falsch platzierte XML-Doku zurückgeschoben, die neuen Blockkommentare ins Englische
  gezogen, ein Testfall für „dasselbe Wort zweimal im Satz" (der Kommentar behauptete es, nichts prüfte es).
- **Als Story ausgelagert statt hier mitgenommen:**
  [B-93](backlog/B-93-birkenbihl-einstellungen-ohne-wirkung.md) — `RequireTypedTest` ist für Birkenbihl jetzt
  unerfüllbar, und im Positions-Test deckt die Karte auf, ohne die Dekodierung zu tragen. Kein Bestandsschaden,
  aber zwei Einstellungen, die eine Wirkung versprechen, die sie nicht haben.

**Verifikation nach den Reviews:** Backend **709/709 grün** (ein Test mehr als vorher), Frontend
**118/118**, Build sauber.

## Runde 2 — Abnahme

- **Creator: signiert.** „Was ich anlege, kommt an — ich habe die Dekodierung im Testmodus gesehen, Wort für
  Wort, dieselbe Ansicht, die das Kind bekommt." Live gegen `creator/exercises/3/preview` geprüft
  (`typed: false`, alle vier Wortpaare). **Ausdrücklich zurückgestellt:** die Lehrwerk-Hierarchie (B-63/B-64,
  sein Top-2) und das Vorbelegen des Fachlehrers (B-67) — nicht Teil dieser Runde.
- **Vater: signiert.** „Ich kann eine Position nicht mehr lautlos zur Lösungsausgabe machen — nicht am Plan
  und nicht an der Übung." Live geprüft: `400` mit Begründung, gültige Stufe weiter `200`. **Zurückgestellt:**
  Mehr-Kind-Überblick (B-39), Durchstich des Assistenten (B-58), Punkte-Empfehlung übernehmen (B-45).
- **Sohn: geliefert, ein Blick fehlt.** Inhaltlich signiert: die Dekodierung ist da (live an der Karte
  gesehen), die Karte verlangt kein Tippen mehr, und beim Aufdecken zählt jede gleichwertige Übersetzung
  auch sichtbar. **Was kein Test abnehmen kann** und darum als benannte menschliche Prüfung offen bleibt:
  wie die Wort-für-Wort-Spalten am echten Bildschirm sitzen (Umbruch bei langen Sätzen, Lesbarkeit der
  kleinen Gloss, Wirkung auf dem Handy). Die Chrome-Anbindung war in dieser Sitzung nicht verfügbar, ein
  eigener Blick war also nicht möglich. **Konkrete Prüfung:** Sohn-Arcade auf `:5173`, Kind 2 / PIN 2222,
  Demo-Plan → Position 12 („Birkenbihl: Getting to know each other") einmal ansehen. Der Vorbehalt läuft mit
  [B-31](backlog/B-31-geraete-vorbehalt-klang.md) als stehende Geräte-Prüfung weiter.
- **Bewusst nicht behauptet:** kein „alle drei zufrieden ✅" über die Optik. Der Rest ist gemessen.

## Offene Roadmap

Die durable Liste ist **[docs/backlog/](backlog/README.md)** — hier steht nur die *Begründung* der
aktuellen Ordnung (siehe Tabelle oben). Alles, was diese Runde nicht baut, liegt dort bereits als
Story; es entstand kein Carry-over ohne Story.

**Stehender Geräte-Vorbehalt** ([B-31](backlog/B-31-geraete-vorbehalt-klang.md)): Klang, Haptik und
Tippgefühl sind nie maschinell abgenommen. Der Vorbehalt bleibt offen, bis der Nutzer sie einmal am
echten Handy gegenhört — kein Testlauf ersetzt das.

## Stand am Ende dieser Sitzung

**Geplant, gebaut, geprüft — mit einer benannten menschlichen Prüfung offen.** Der rote Faden ist
vollständig umgesetzt (B-79, B-70, B-78), zwei Reviews sind eingearbeitet, und aus der Runde ist eine neue
Story entstanden (B-93) statt eines Vermerks. Creator und Vater haben gegen das laufende Produkt
abgenommen; beim Sohn ist der Inhalt abgenommen und die **Optik** der Wort-für-Wort-Ansicht wartet auf
einen Blick am Bildschirm.

Die drei Stories stehen auf `in-arbeit` mit belegter Verifikation. `abgenommen` verlangt laut
[Backlog-Regeln](backlog/README.md) einen genannten Commit — der fehlt noch, es ist nichts committet.

### Konkreter Änderungsstand (für Review)

- **Backend:** `PlanPositionsController` (Stufenprüfung), neu `Exercises/StageValidation.cs`,
  `ExerciseControllerBase` (Standard-Stufe), `VocabularyExerciseType` (`ShowBoth` in `StageOptions`),
  `BuiltInExerciseTypes` (Birkenbihl: Dekodierung + `IsTypedStage`), `ExerciseContentProvider`
  (`ContentItem.Decoding`), `PositionPlayService` (`CardFacets` + zwei Facetten),
  `PositionPracticeController`, `PositionTestsController`, `ExercisePreviewService`.
- **Vertrag:** `PracticeCard`, `TestItem`, `PreviewItem` — je additive Trailing-Parameter, kein Bruch.
- **Frontend:** neu `components/RevealAlternatives.tsx`, `components/BirkenbihlDecoding.tsx` (+ Tests),
  `SohnPractice`, `SohnTest`, `ExercisePreviewModal`, `index.css`, Alias in `lib/types.ts`.
- **Tests:** 6 neue/erweiterte Backend-Fälle + 1 Wächter, 5 Frontend-Fälle.
- **Mitgezogen (erzeugt):** `docs/openapi/v1.json`, `docs/api-examples/study-plans.md`,
  `OpenApi/openapi-examples.generated.json`, `frontend/src/lib/contract.ts`.
- **Keine Migration, keine Schemaänderung, kein Vertragsbruch.**
