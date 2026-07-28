---
tags: [typ/plan, bereich/api, bereich/frontend, bereich/doku, rolle/supervisor, rolle/student]
aliases: [Remarks, Anmerkungen, Testnotizen, Befunde]
---

# Umbauplan: Remarks – kontextreiche Anmerkungen beim Testen

Status: **Umgesetzt (Etappen 1–8), Code-Review abgearbeitet.** Stand 2026-07-27. Backend warnungsfrei, 441 Tests grün
(inkl. `RemarkTests`: Erfassen samt Kontext, Sichtbarkeitstrennung Student/Supervisor/fremder Supervisor,
Rückkanal mit erhaltener Antwort bei `Planned`, PATCH-Clear, Folgeanmerkung, Paging), Migration
`AddRemarks`. Frontend: Fehler-Ringpuffer (`src/lib/remarks.ts`), Kontext-Provider
(`src/lib/remarkContext.tsx`) und das Widget im Vater-Web (`src/components/RemarkWidget.tsx`, Alt+A);
9 Vitest-Tests und 9 Playwright-E2E grün (`e2e/anmerkungen.spec.ts` neu, inkl. Sohn-Arcade). Widget in
beiden Apps (Arcade mit `bottomOffset={96}`, damit die klebende `.sohn-nav` bedienbar bleibt).
Skill `anmerkungen` (Einsatz A)
gegen eine laufende Instanz verifiziert. Export als Markdown über `GET remarks/export` nach
[docs/anmerkungen/](anmerkungen/README.md); `creator`/`supervisor`/`student` lesen ihn als Schritt 0,
`pm-loop` den Befund als stärkste Feedback-Quelle (Schritt 2).

**Die beiden zuletzt offenen Punkte sind erledigt** (2026-07-27):

- **Aufgezeichnete Beispiele.** `DocsCaptureTests` fährt jetzt eine Gruppe `remarks`
  ([docs/api-examples/remarks.md](api-examples/remarks.md), 12 Beispiele): erfassen samt Kontext →
  Log-Id lesen → Antwort zurückschreiben → Folgeanmerkung mit Verweis, dazu die Sichtbarkeitswand
  (der Sohn sieht die Anmerkung des Vaters **nicht** – deshalb 404 und nicht 403), der
  **Markdown-Export** samt Supervisor-Schranke und die Fehlerfälle.
  Damit ist `remark_not_found` belegt; die Abdeckung steigt auf **29 / 45** Codes.
  Der Export zwang die Capture-Harness dazu, den **Antwort-Medientyp** mitzuführen: Sein Rumpf ist
  Markdown und enthält selbst ` ```json `-Blöcke. Ein als JSON etikettierter dreifacher Zaun wäre eine
  Falschaussage *und* hätte die Seite zerlegt – jetzt bestimmen Sprache und Zaunlänge sich aus dem Inhalt
  (wie in `RemarkExportService.AppendFenced`), und nur JSON-Antworten wandern als Wert in die
  OpenAPI-Beispiele, wo sie geparst werden.
- **Reproduzierbare Doku.** Die Test-Factory löscht nach dem Start die geseedeten Zeitfenster
  (`PuglingWebAppFactory.CreateHost`) – ohne Multiplikator gilt Faktor 1,0, `awarded` steht fest bei 10
  statt zwischen 8, 10 und 15 zu wandern. Dabei fiel auf, dass die Zeitstempel-Maskierung in `Redact`
  ein abschließendes `Z` verlangte, das die API nie schreibt: Sie traf **nie**, und jeder Lauf schrieb
  sämtliche Zeitstempel neu. Jetzt greift sie mit und ohne Zonenkennung, und lauf-relative Datumswerte
  (Plan-Start/-Ende, Verlaufstage) werden zu `<date>` – feste Literale wie `2099-03-01` bleiben lesbar.
  Zwei Läufe hintereinander erzeugen nun **byte-identische** Dateien.
- **Smoke-Test.** `/smoke-test` prüft die Anmerkungen jetzt mit (Checks 9–10 in
  `.claude/scripts/smoke-checks.sh`): erfassen, Kontext wieder auslesen, unbekannte Id →
  `remark_not_found`. Das widerspricht dem Abschnitt „Verhältnis zu den bestehenden Test-Skills" nicht –
  dort geht es darum, dass der Smoke-Test keine *variablen Zusatzchecks aus einer Textdatei* fährt. Diese
  drei Checks sind fest und binär wie alle anderen.

> Beim Verifizieren von Etappe 6 gefunden und behoben: Ein Kontext-Bezug, der ins Leere zeigt
> (gelöschtes Kind, `/vater/kind/999` durch Tippfehler), ließ den POST mit **500** scheitern – das Widget
> schickt diese IDs automatisch mit, der Nutzer hätte seinen Text verloren. Tote Bezüge werden jetzt still
> verworfen (`RemarkTests.UngueltigerKontext_VerhindertDasErfassenNicht`); dieselbe Haltung wie die
> `SetNull`-FKs. Der ausdrücklich gesetzte `parentRemarkId` bleibt bewusst ein 400 – den setzt der Skill,
> ein Fehlgriff dort ist ein Fehler und keine verwelkte Automatik.

> Beim Bauen von Etappe 3 aufgefallen: Die E2E-Suite lief in die **Login-Bremse** (`PermitLimit` 10/Minute,
> pro IP – alle Specs teilen die localhost-Partition). Sie lag knapp darunter; die drei neuen Tests
> brachten sie darüber, was einen *späteren* Spec mit „Login fehlgeschlagen" umwarf. Behoben über
> `RateLimiting__LoginEnabled: "false"` im Backend-Env der `playwright.config.ts` – dasselbe Zugeständnis,
> das der In-Process-TestServer im Backend schon macht.

## Motivation / Zielbild

Beim Testen fallen Fragen und Beobachtungen an – zum Code, zur Funktion, zum Inhalt. Heute landen sie
in einem separaten Textdokument und werden von Hand nach Claude übertragen.

Der Schmerz ist dabei **nicht das Aufschreiben** – eine Zeile tippen geht schnell. Der Schmerz ist das
**Rekonstruieren des Wo**: Auf welcher Seite war ich? Welches Kind war ausgewählt? Welche Übung stand
offen? Ging vorher ein Request schief? Genau das weiß nur die laufende App, und genau das schreibt sich
heute niemand vollständig mit.

**Daraus folgt das Leitprinzip dieses Features:** Der automatische Kontext-Mitschnitt *ist* das Feature.
Ein Formular, das nur Freitext plus ein Dropdown speichert, wäre ein schlechteres Textdokument – dann
lieber gar nicht bauen.

### Der Rückkanal: gefragt im UI, beantwortet in Claude Code

Ein Teil der Anmerkungen sind **Fragen, die keine Codeänderung brauchen** – „Ich will meine E-Mail-Adresse
ändern und finde keine Stelle dafür." Dafür gibt es einen zweiten Ablauf:

1. Frage im Widget erfassen → Antwort des Servers nennt die **Id** („Gespeichert als **#123**").
2. In Claude Code: *„Beantworte die Frage 123."* Der Skill liest die Anmerkung samt Kontext über die
   laufende API, recherchiert im Code und antwortet – z. B. „Die API kann das über
   `PATCH api/v1/supervisor/fathers/{id}`, im Vater-Web gibt es dafür kein Formular."
3. Der Skill fragt nach: **beantwortet** (→ `Done`) oder **zurückgestellt** (→ `Planned`)?

Der entscheidende Punkt: Die **Antwort bleibt in beiden Fällen erhalten**. Ein zurückgestellter Fall ist
damit kein offener Zettel mehr, sondern ein bereits analysierter Backlog-Eintrag – die Vorarbeit für die
spätere Umsetzung ist getan.

Die Richtung bleibt dabei die des Gesamtkonzepts: **Eingabe über UI/API, Ausführung durch Claude Code,
angestoßen vom Menschen.** Nichts läuft automatisch los.

## Abgrenzung (was das ausdrücklich nicht ist)

> **Revidiert am 2026-07-28:** Die beiden folgenden Punkte galten so nicht mehr und sind unten ersetzt.
> Grund war ein konkreter Datenverlust, nicht ein Sinneswandel — siehe „Nachtrag: Verlauf".

- ~~**Kein Issue-Tracker.** Kein Zuweisen, keine Meilensteine. Vier Zustände, mehr nicht.~~
  → Zuweisen und Meilensteine bleiben draußen, die vier Zustände auch. Dazugekommen ist der **Verlauf**.
- ~~**Kein Chat.** Das Widget ist Eingang plus Lesesicht, kein Dialog.~~
  → Nachhaken ist jetzt möglich (Widget und `/vater/anmerkungen`). Die Sorge dahinter bleibt gültig und
  wird anders abgewehrt: **keine Zustellung, keine Ungelesen-Marker, keine Erwartung, dass jemand wartet.**
  Gelesen wird beim nächsten Testen oder im nächsten Skill-Lauf. Ein Beitrag je Arbeitsschritt, nicht je
  Gedanke.
- **Keine Schüler-Rückmeldung.** „Diese Übung war doof" vom echten Sohn ist ein anderes Feature. Das
  Datenmodell trägt es (Autor + Rolle), das UI zielt aber nicht darauf.
- **Kein Screenshot.** Bewusst verworfen: Bibliothek im Frontend plus Byte-Ablage, und die Medien-Ablage
  ist für Lernbilder gedacht, nicht für Debug-Artefakte.
- **Kein Ersatz für `pm-loop`.** Remarks sind ein *Zulieferer* für die Produktrunden, kein zweiter Loop.

## Getroffene Entscheidungen

1. **Erst Dev, später offen.** Datenmodell und API werden produktreif gebaut (Auth, Ownership, Paging);
   das UI-Widget blendet zunächst nur im Dev-Modus ein. Freischaltung ist später ein Ein-Zeilen-Schalter.
2. **Erfassen ist ein Feld.** Tastenkürzel → Textfeld → Enter. Kategorie ist **optional**, nicht Pflicht.
   Jede erzwungene Eingabe macht das Widget langsamer als das Textdokument – dann schläft die Nutzung ein.
3. **Name `Remark`, Route `api/v1/remarks`.** Englischer Typname wie die übrigen Entities, deutsche Doku.
   Tier-neutrale Route mit Präzedenz durch den `AuthController` unter `auth/` – so muss nichts umziehen,
   wenn daraus ein Produktfeature wird.
4. **Autor + Rolle werden mitgeschrieben.** Heute immer derselbe Mensch, aber das Modell muss dafür nicht
   später geändert werden.
5. **Kontext = App-Zustand + Fehler-Ringpuffer.** Ohne Screenshot, ohne Commit-Stand (der Zeitstempel
   lässt sich per `git log` korrelieren).
6. **Schlanker Status** (`Open`/`Planned`/`Done`/`Rejected`). Ohne ihn legt der Nachbereitungs-Skill bei
   jedem Lauf dieselben Anmerkungen wieder auf den Tisch – daran scheitern solche Systeme üblicherweise.
7. **API primär, Export auf Abruf.** Lesen über die API wie bei den Rollen-Skills; zusätzlich ein
   Markdown-Schnappschuss, damit beim Nacharbeiten kein Server laufen muss.
8. **Ein Skill mit zwei Einsätzen.** Mit Id = einzelne Frage sofort beantworten, ohne Id = Sammel-
   Nachbereitung. Beide teilen API-Zugang, Kontext-Lesen und Status-Schreiben; als zwei Skills wäre das
   dieselbe Mechanik doppelt. Die Entwicklungsentscheidung trifft weiterhin `pm-loop`.
9. **Die Antwort wird zurückgeschrieben**, nicht nur im Terminal ausgegeben – sonst ist die Verknüpfung
   von Frage und Antwort verloren und der Sammel-Skill sieht nicht, was schon erledigt ist.
10. **Optionales `ParentRemarkId`.** Entsteht aus der Antwort auf #123 eine neue Aufgabe, legt Claude #145
    mit Verweis an. Ein Feld, hält das Modell flach – und rückwirkend nachrüsten ginge nicht.
11. **Das Widget zeigt die eigenen letzten Anmerkungen** samt Status und Antwort (aufklappbar). Damit
    siehst du beim nächsten Testen direkt, was beantwortet wurde, ohne ins Terminal zu wechseln.
12. **Ein Eingang, mehrere Verbraucher.** Anmerkungen entstehen ausschließlich im Widget. Die
    bestehenden Test-Skills **lesen** sie, sie schreiben keine – Begründung im Abschnitt
    „Verhältnis zu den bestehenden Test-Skills".

## Sicherheitsregel: der Ringpuffer speichert nur Metadaten

Der Login-Request trägt **PINs im Body**. Ein Puffer, der Requests roh mitschreibt, legt PINs im Klartext
in die Produkt-DB – und der Export trüge sie ins Repo.

**Verbindlich:** Der Ringpuffer hält je Eintrag ausschließlich `method`, `path`, `status`, den `code` aus
den ProblemDetails und einen Zeitstempel. **Keine** Request-/Response-Bodies, **keine** Header, **keine**
Tokens, **keine** Query-Werte. Für „was ging schief" reicht das vollständig.

Dieselbe Regel gilt für den Zustands-Schnappschuss: nur IDs und Filterwerte, niemals geladene Entities.

## Datenmodell

Neue Datei `backend/Pugling.Api/Models/RemarkEntities.cs`:

| Feld | Typ | Anmerkung |
|---|---|---|
| `Id` | `int` | **Wird dem Nutzer angezeigt** – die „Log-Id" für „Beantworte Frage 123" |
| `CreatedAt` | `DateTime` | UTC, wie überall |
| `Text` | `string` | Pflicht, das einzige Eingabefeld |
| `Category` | `RemarkCategory` | `Unspecified` (Default), `Bug`, `Ui`, `Code`, `Content`, `Idea`, `Question` |
| `Status` | `RemarkStatus` | `Open` (Default), `Planned`, `Done`, `Rejected` |
| `Answer` | `string?` | Die Antwort von Claude Code – **bleibt auch bei `Planned` erhalten** |
| `AnsweredAt` | `DateTime?` | |
| `AnsweredBy` | `string?` | Protokollfeld, z. B. `claude-code`; bewusst `string`, damit später auch ein Mensch antworten kann |
| `ParentRemarkId` | `int?` | Selbstbezug, FK `SetNull` – Spur von der Frage zur daraus entstandenen Aufgabe |
| `AccountId` | `int` | Autor, aus dem `aid`-Claim |
| `AuthorRole` | `ProfileRole` | Rolle **zum Zeitpunkt der Erfassung** (Momentaufnahme, wie `SupervisorId` auf `ShopPurchase`) |
| `Route` | `string` | SPA-Pfad, z. B. `/vater/kind/3/lernstand` |
| `AppArea` | `string` | `vater` / `sohn` – explizit statt aus der Route geraten |
| `ChildId` | `int?` | FK **`SetNull`** |
| `ExerciseId` | `int?` | FK `SetNull` |
| `StudyPlanId` | `int?` | FK `SetNull` |
| `PlanPositionId` | `int?` | FK `SetNull` |
| `ContextJson` | `string?` | Zustands-Schnappschuss (Filter, offenes Modal, Auswahl) |
| `RecentErrorsJson` | `string?` | Ringpuffer-Metadaten, max. 10 Einträge |
| `UserAgent` | `string?` | |

**Zwei Fallstricke aus der CLAUDE.md, die hier zuschlagen:**

- **`ValueComparer` nicht vergessen.** `ContextJson` und `RecentErrorsJson` sind JSON-Spalten. Werden sie
  als typisierte Objekte gemappt, brauchen sie einen Comparer aus `Data/JsonValueComparer.cs`, sonst gehen
  In-Place-Änderungen still verloren. *Alternative, die ich empfehle:* beide als **rohen `string`** halten.
  Das Backend liest sie nie fachlich aus – nur der Skill tut das. Dann entfällt der Comparer ganz.
- **FKs auf `SetNull`** – auch `ParentRemarkId`. Sonst scheitert das Löschen eines Kindes, einer Übung oder
  einer Vorgänger-Anmerkung an einem Verweis. Der Kontext darf verblassen, er darf nichts blockieren.

## API

Neuer Controller `backend/Pugling.Api/Controllers/RemarksController.cs` – **tier-neutral**, direkt unter
`Controllers/` wie der `AuthController`, nicht in einem Ebenen-Ordner. Das ist eine bewusste Ausnahme von
der Drei-Ebenen-Taxonomie und gehört als solche in die CLAUDE.md.

| Verb | Route | Auth |
|---|---|---|
| `POST` | `api/v1/remarks` | `[Authorize]`; Antwort trägt die **Id** – das Widget zeigt sie an |
| `GET` | `api/v1/remarks` | `[Authorize]`, Sichtbarkeit s. u.; Paging + Sortierung über `PagingExtensions`/`SortingExtensions`, Filter `?status=&category=&childId=&appArea=&mine=` |
| `GET` | `api/v1/remarks/{id}` | `[Authorize]` – der Einstieg des Skills für „Frage 123" |
| `PATCH` | `api/v1/remarks/{id}` | `[Authorize]`; setzt Text/Kategorie/Status **und** `Answer`/`AnsweredAt`/`AnsweredBy` |
| `DELETE` | `api/v1/remarks/{id}` | `[Authorize]`, nur eigene bzw. betreute |
| `GET` | `api/v1/remarks/export` | `[Authorize(Roles = Roles.Supervisor)]`, liefert Markdown |

`?mine=true` liefert die eigenen letzten Anmerkungen – das ist die Abfrage hinter der Widget-Liste. Ohne
den Filter sähe der Supervisor auch die des Kindes, was im Widget nur stört.

**Sichtbarkeit** – inline getrennt über `IsSupervisor`/`IsStudent`, wie es die Student-Endpunkte schon tun:

- **Student** sieht ausschließlich **eigene** Anmerkungen (`AccountId == aid`). Wichtig für den Tag, an dem
  der echte Sohn das Widget sieht: Er darf die Testnotizen des Vaters nicht mitlesen – und deren Antworten
  erst recht nicht, die tragen Code-Details.
- **Supervisor** sieht eigene **plus** die der von ihm betreuten Kinder (`AuthAccess`).

**PATCH-Semantik** wie im Haus: `null` heißt „nicht angegeben". Zum Leeren der optionalen Kontext-Bezüge
braucht es ausdrückliche Schalter (`ClearChild`, `ClearExercise`, …) – und im Controller **erst der Wert,
dann der Schalter**, damit „leeren" gewinnt.

**Neue Codes additiv in `Errors/ApiErrors.cs`:** `remark_not_found`. Für leeren Text reicht der bestehende
`validation_error`.

**DTOs** ins Vertrags-Projekt unter `Pugling.Contracts/Shared/` (die Ressource ist ebenen-neutral). Namen
global eindeutig halten – `Remark…` als Präfix durchziehen, `Note`/`Comment`/`Answer` als Solitäre meiden.

## Frontend: Kontext-Erfassung

- **Fehler-Ringpuffer** in `src/lib/api.ts`. Dort gibt es mit `http<T>()` genau eine zentrale Fetch-Stelle
  (plus `httpForm`/`httpPaged`) – wenige Zeilen an drei Stellen oder ein gemeinsamer Wrapper. Zusätzlich
  `window.onerror` und `unhandledrejection`. Ringgröße 10, nur Metadaten (s. o.).
- **Zustands-Schnappschuss** über einen kleinen Context plus Hook `useRemarkContext()`, den einzelne
  Screens optional anreichern (`{ childId, exerciseId, filter }`). Screens, die nichts melden, liefern
  eben nur die Route – das Feature muss ohne flächendeckende Verkabelung funktionieren.

**Namenskonflikt beachten:** `src/lib/feedback.ts` ist bereits belegt (Ton + Haptik für die Sohn-Arcade).
Die neuen Dateien heißen `src/lib/remarks.ts` und `src/components/RemarkWidget.tsx`.

## Frontend: Widget

- Tastenkürzel öffnet ein einzelnes Textfeld; Enter speichert, Esc schließt.
- Nach dem Speichern erscheint kurz die **Id** – sichtbar und markierbar, denn sie ist der Schlüssel für
  „Beantworte Frage 123".
- Optionale Kategorie als Ein-Klick-Chips – überspringbar, nie Pflicht.
- **Aufklappbare Liste** der eigenen letzten Anmerkungen (`?mine=true`) mit Status und – wo vorhanden –
  der Antwort. Reine Lesesicht, kein Antwortfeld: Das Widget bleibt Eingang.
- Standardmäßig **eingeklappt**, klein, unten rechts, `pointer-events` nur auf dem Auslöser.
- Zwei Ausprägungen: unauffällig im Vater-Web, dezent in der Sohn-Arcade (die Arcade-Optik und die PWA
  dürfen nicht leiden).

**E2E-Risiko – der wichtigste Umsetzungshinweis:** `playwright.config.ts` startet `npm run dev`, also ist
`import.meta.env.DEV` in **allen** E2E-Läufen wahr. Das Widget läuft dort mit und kann Klicks abfangen
oder mit Tastatureingaben kollidieren. Es braucht deshalb einen ausdrücklichen Abschalter
(`localStorage`-Flag oder Query-Parameter), den die Playwright-Konfiguration setzt. Ohne den brechen
`full-flow.spec.ts` und `vater-von-null.spec.ts`.

## Export

`GET api/v1/remarks/export?status=open` liefert Markdown: je Anmerkung Id, Zeitstempel, Autor/Rolle, Route,
Kontext, Fehlerpuffer, Text und – falls vorhanden – die Antwort. Der Skill schreibt das Ergebnis nach
`docs/anmerkungen/`; damit ist der Stand versioniert und beim Nacharbeiten ist kein Server nötig.

**Der Export ist mehr als eine Notlösung – er ist die Brücke zu den Test-Skills.** Weil `creator`,
`supervisor`, `student` und `smoke-test` gegen eine Wegwerf-DB laufen (siehe nächster Abschnitt), können
sie die Anmerkungen *nicht* aus der Datenbank lesen. `docs/anmerkungen/` ist der einzige Weg, auf dem deine
Beobachtungen bei ihnen ankommen. Der Export rückt deshalb in den Etappen vor die Skill-Anbindung.

## Skill

Neuer Skill `.claude/skills/anmerkungen/SKILL.md`, Login über den vorhandenen Helfer
`.claude/scripts/tutorial-api.sh` (`login_father`) – kein neues Credential-Konzept.

**Einsatz A – einzelne Frage beantworten** (*„Beantworte die Frage 123"*):

1. `GET api/v1/remarks/123` – Text und Kontext lesen.
2. Im Code recherchieren. **Belegpflicht:** Jede Antwort nennt Datei und Zeile, oder sie sagt ausdrücklich
   „nicht sicher". Eine geratene Antwort in einem Logbuch ist schlimmer als keine – du glaubst ihr später.
3. Antwort ausgeben und per `PATCH` zurückschreiben (`Answer`, `AnsweredAt`, `AnsweredBy`).
4. Nachfragen: **beantwortet** (→ `Done`) oder **zurückgestellt** (→ `Planned`)? Entsteht daraus eine neue
   Aufgabe, legt der Skill sie als neue Anmerkung mit `ParentRemarkId` an.

**Einsatz B – Sammel-Nachbereitung** (ohne Id):

1. Offene Anmerkungen holen (API, sonst Export-Datei).
2. **Klassifizieren** – die Kategorie, die beim Erfassen leer bleiben durfte, wird hier aus dem Text
   abgeleitet.
3. Nach Thema gruppieren, Duplikate zusammenfassen, gegen den aktuellen Code prüfen (eine Anmerkung von
   vor zwei Wochen kann längst erledigt sein).
4. Priorisierten Befund nach `docs/` schreiben, Verarbeitetes auf `Planned` setzen.
5. Übergabe an `pm-loop` für die eigentliche Entwicklungsentscheidung.

Schritt 4 ist der, der das System am Leben hält: Was einmal eingeplant ist, taucht nicht wieder auf.

## Verhältnis zu den bestehenden Test-Skills

**Die Test-Skills schreiben keine Anmerkungen. Sie lesen sie.**

### Warum nicht schreiben

1. **Wegwerf-DB.** `.claude/scripts/tutorial-api.sh` setzt `DB="pugling_smoke.db"` und räumt am Ende mit
   `rm -f "$API_DIR/$DB"*` auf; `/smoke-test` macht dasselbe auf Port 5280. Das ist Absicht – die echte
   `pugling.db` bleibt unberührt, die Seed-IDs bleiben reproduzierbar. Ein Remark aus einem solchen Lauf
   würde am Ende desselben Laufs gelöscht.
2. **Das Kernfeature liefe leer.** Der Wert steckt in `Route`, `AppArea`, `ContextJson`,
   `RecentErrorsJson` – dem, was ein Mensch im Browser nicht mitschreibt. Ein Skill, der die API per curl
   fährt, hat nichts davon. Übrig bliebe Text plus Kategorie: genau das „schlechtere Textdokument", das
   oben als Grund steht, das Feature *nicht* zu bauen.
3. **Die vorhandenen Kanäle sind zweckmäßig verschieden, nicht willkürlich.** Die Rollen-Skills schreiben
   verifizierte Tutorials nach `docs/` (in Git, im Diff, auf GitHub lesbar), `/smoke-test` ist ein Gate mit
   Exit-Code, `pm-loop` liefert einen priorisierten Backlog. Eine DB-Zeile wäre für alle drei ein
   Rückschritt.

### Wie gelesen wird

`creator`, `supervisor` und `student` bekommen einen **optionalen Startschritt**: offene Anmerkungen aus
`docs/anmerkungen/` lesen, die zum eigenen Bereich passen, und im Durchlauf **gezielt nachtesten** statt nur
den Standardpfad abzuspulen. Eine Beobachtung wie „Shop-Kauf fühlt sich komisch an, Bestand aktualisiert
sich verzögert" wird so zur Testanweisung; das Ergebnis fließt in den Befund und – wo es die Bedienung
betrifft – ins jeweilige Tutorial.

`/smoke-test` **liest keine** Anmerkungen: Er ist ein schnelles Ja/Nein-Gate, und variable Zusatzchecks aus
einer Textdatei würden genau das aufweichen. Er prüft aber – seit den letzten Änderungen – die
Remarks-**Endpunkte** selbst (erfassen, Kontext, `remark_not_found`): feste, binäre Checks wie alle anderen.

## Etappen

| # | Inhalt | Prüfung |
|---|---|---|
| 1 | Backend: Entity, Migration `AddRemarks`, Contracts-DTOs, Controller, Sichtbarkeitsregeln, Antwort-Felder | `RemarkTests` (Anlegen je Rolle, Sichtbarkeitstrennung Student/Supervisor, PATCH-Clear, Antwort setzen, Paging) |
| 2 | Frontend-Kontext: Fehler-Ringpuffer + `useRemarkContext()` | Test, dass keine Bodies/Header im Puffer landen |
| 3 | Widget im Vater-Web: Erfassen + Id anzeigen + Liste der eigenen Anmerkungen | manuell + `/smoke-test` |
| 4 | Skill Einsatz A (einzelne Frage beantworten) | echter Durchlauf gegen laufende API |
| 5 | Export-Endpunkt nach `docs/anmerkungen/` | `DocsCaptureTests`-Muster |
| 6 | Leseanbindung: Startschritt in `creator`/`supervisor`/`student` | ein Rollen-Lauf, der eine notierte Beobachtung gezielt nachtestet |
| 7 | Widget in der Sohn-Arcade + E2E-Abschalter | beide Durchstich-E2E müssen grün bleiben |
| 8 | Skill Einsatz B + Anbindung an `pm-loop` | Durchlauf mit gesammelten Anmerkungen |

**Etappen 1–4 sind der Nutzensprung** – danach kannst du erfassen, die Id mitnehmen und Fragen beantworten
lassen. Das ist der Kreis, der sich im Alltag schließt.

**5–6 sind der zweite, kleinere Sprung:** Ab da fließen deine Beobachtungen in die Rollen-Läufe zurück. Der
Export steht davor, weil er die einzige Brücke zu den Wegwerf-DB-Skills ist.

7–8 sind Ausweitung: die Sohn-Arcade als zweiter Erfassungsort und die Sammel-Nachbereitung, die sich erst
lohnt, wenn genügend Anmerkungen zusammengekommen sind.

## Risiken

- **Die Nutzung schläft ein.** Das Hauptrisiko, und es ist ein Bedienrisiko, kein technisches. Gegenmittel
  ist allein die Reibungsarmut: ein Feld, ein Tastendruck. Wenn du nach zwei Wochen wieder ins Textdokument
  schreibst, ist das Feature gescheitert – und das ist dann eine ehrliche Antwort, kein Grund nachzurüsten.
- **Geratene Antworten.** Adressiert durch die Belegpflicht im Skill; eine unbelegte Antwort im Logbuch
  wirkt später wie eine geprüfte.
- **Scope-Creep zum vollen Chat.** Sobald ein Antwortfeld ins Widget wandert, brauchst du Threads und
  Ungelesen-Marker. Die Liste im Widget bleibt deshalb **lesend**.
- **PIN-Leck über den Ringpuffer.** Adressiert durch die Metadaten-Regel; gehört in einen Test, nicht nur
  in die Doku.
- **E2E-Bruch durch das Widget.** Adressiert durch den Abschalter in Etappe 5.
- **Dev-Daten in der Produkt-DB.** Bewusst in Kauf genommen; die Tabelle ist klein und isoliert, und die
  Alternative (zweite Datenbank) kostet mehr, als sie hier wert ist.
- **Taxonomie-Ausnahme.** `api/v1/remarks` liegt außerhalb der Drei-Ebenen-Struktur. Das ist begründet
  (die Ressource gehört keiner Ebene), muss aber in der CLAUDE.md stehen, sonst wirkt es beim nächsten
  Lesen wie ein Versehen.

## Nachtrag: Verlauf und God-Mode (2026-07-28)

**Anlass war ein Datenverlust, kein Sinneswandel.** Am 2026-07-27 wurden sieben Anmerkungen zuerst
analysiert (Antwort mit Datei- und Zeilenbelegen), dann wurden die Lücken gebaut — und die Umsetzungsnotiz
ging wieder als `answer` zurück und **überschrieb die Analyse**. Die Vorarbeit war weg. Genau die
Reihenfolge „analysieren → zurückstellen → später umsetzen" ist aber der gewollte Ablauf, und ein einzelnes
Feld kann sie nicht abbilden.

Zweiter Anlass: Der Skill meldete sich als Vater 1 an, die Anmerkungen lagen an Konto 11 — Ergebnis eine
**leere Liste**. Ein Werkzeug, das seine eigenen Einträge nicht findet, kostet mehr Zeit, als es spart.

Umgesetzt:

- **`RemarkComment`** (Tabelle, FK `Cascade` auf die Anmerkung, `SetNull` aufs Autor-Konto). `answer` bleibt
  die **eine gepinnte Auflösung**, der Verlauf trägt alles danach. `RemarkDto.CommentCount` liegt an der
  Anmerkung, damit Listen „3 Beiträge" ohne Nachladen zeigen (Projektion, kein `Include`).
- **Herkunft `Human`/`Assistant`** als eigenes Feld — nicht aus dem Konto abgeleitet, denn Claude schreibt
  über den Skill mit dem Token des Menschen. Daran hängt die **Wiederaufnahme**: Ein Beitrag des Menschen zu
  einer `Done`/`Rejected`-Anmerkung setzt sie zurück auf `Open`; ein Assistant-Beitrag nie (sonst riss jede
  Umsetzungsnotiz den eigenen Vorgang wieder auf).
- **Kontenübergreifend lesen als eigene Berechtigung** (`Remarks:GlobalRead`, Vorgabe `IsDevelopment()`).
  Auf den **Listen** braucht es das ausdrückliche `?scope=all` (sonst `403 remark_scope_forbidden`), damit
  die Vorgabe eng bleibt und die Widget-Liste nicht plötzlich fremde Einträge zeigt; beim Zugriff auf eine
  **einzelne Id** genügt die Berechtigung – so beantwortet der Skill eine Anmerkung aus jedem Testkonto.
  **Löschen** bleibt eng (Eigentümer bzw. `Admin`): Der Schalter heißt „global *read*". Ein **Student** ist
  immer ausgeschlossen.

  *Erster Anlauf war `Roles.Admin` als Bedingung — verworfen.* Die Rolle umgeht auch die RWX-Rechte auf
  Übungen (`ExercisePermissionService.cs:24/34/46`): Wer sie jedem Vater gibt, damit der Skill lesen kann,
  erlaubt jedem Vater zugleich, fremde Übungen zu ändern, zu löschen und umzurechten — `ExerciseGrant` wäre
  Dekoration. Und weil beim Testen ständig Wegwerf-Konten entstehen (ein frischer Vater ohne Übungen deckt
  auf, was der geseedete Papa nie zeigt), hätte jedes einzeln ein Flag gebraucht. Als **Break-Glass** bleibt
  `Admin` zusätzlich erlaubt, taugt aber nicht als Bedingung.
- **Bedienung:** knapper Aufklapper im Widget, vollständige Sicht auf `/vater/anmerkungen` (Filter, Paging,
  Kontext, Auflösung, Verlauf, Stand) — beide nur im Dev-Modus.

**Fallstricke, die dabei aufgefallen sind:**

- Ein geseedetes Konto zum Admin zu machen ist eine Falle: `Roles.Admin` umgeht auch die Übungs-Rechte, und
  weil die Test-Factory über eine Klasse geteilt wird, verlieren andere Tests still ihre Annahme „dieses
  Konto darf das nicht". Tests legen sich darum je Fall ein **eigenes** Konto an.
- Ein `IsAdmin`-Flag wirkt **erst nach neuer Anmeldung** — Rollen stecken im JWT. (Einer der Gründe, warum
  ein Konfigurationsschalter hier das bessere Werkzeug ist: Er wirkt sofort und für alle Konten.)
- **Berechtigungen nicht durch Ausprobieren erkunden.** Die Anmerkungs-Seite prüfte zuerst mit einem
  `scope=all&take=1`, ob sie den Umschalter zeigen darf. Der absichtlich provozierte 403 landete über
  `recordHttpError` im **Fehler-Ringpuffer** — und die nächste erfasste Anmerkung trug ein
  `remark_scope_forbidden` im Kontext, das mit der Beobachtung nichts zu tun hatte. Der Mitschnitt *ist*
  hier das Feature. Jetzt entsteht der Eintrag nur nach einem echten Klick.

## Verwandt

- [pm-loop-Skill](../.claude/skills/pm-loop/SKILL.md) – nimmt die aufbereiteten Befunde entgegen
- [docs/endpunkt-beziehungen.md](endpunkt-beziehungen.md) – Wissenskarte der Endpunkte
- [docs/obsidian.md](obsidian.md) – Doku-Konventionen
