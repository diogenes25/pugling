---
tags: [typ/story, status/abgenommen, bereich/api, rolle/supervisor, rolle/student]
aliases: [Unerreichbare Pflicht, nur getippt ohne Stufe]
status: abgenommen
prio: P1
art: Defekt
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: "Gesamt-Review und volle Playwright-Suite des Backlog-Laufs 2026-11-14 (docs/pm-sitzung-2026-11-14-backlog-lauf.md, Abschluss)"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-469, B-16]
---

# B-552 · „Nur getippte Tests“ ohne getippte Stufe macht die Pflicht unerreichbar – und kostet täglich Malus

Seit B-550 rechnet der Server die Lernkontrolle je Position (`PositionPlayService.LearningCheckOf`). Die volle
Playwright-Suite zeigte damit, was die alte Frontend-Ableitung verdeckte: Eine Vokabel-Position mit
`requireTypedTest: true` und **ohne** Stufe prüft auf der Vorgabestufe des Typs (`ExerciseTypeBase.DefaultStage` =
Selbsteinschätzung), ein solcher Versuch wird nie gewertet (`PositionTestsController`: `Graded = IsTypedStage`), das
Tagesziel ist also nie erreichbar und der Malus bucht jeden Tag. Bis B-550 stand dort in der Plan-Zeile „Test getippt“;
jetzt steht „Ziel nicht erreichbar …“ (`LearningCheckKind.Unreachable`). Der Server **lehnt** die Kombination aber nur
bei einem Stufenplan ab (`RequireTypedTestValidation`), nicht bei einer selbst eingeschätzten Basisstufe. Das ist genau
die Konfiguration „Häkchen an, Stufe auf Standard“, die das Positionsformular (B-16) mit einem Klick erzeugt.

Vorschlag zum Grillen: dieselbe Ablehnung (`validation_error`) auch für die Basisstufe beim Anlegen und beim PATCH, der
eine der Hälften setzt; Bestandspositionen bleiben (Meldung `Unreachable`), der Vater sieht sie rot. Vorher an einer
Kopie der echten DB zählen, wie viele Positionen heute so stehen (Friedrichs sechs Vokabel-Positionen tragen
`RequireTypedTest = true` – laut B-535 bestanden seine Tests, ihre Stufe ist also getippt; nachmessen).

## User Story

Als *Vater* möchte ich beim Anlegen oder Ändern einer Position sofort hören, wenn „nur getippte Tests“ auf einer Stufe
steht, die nie getippt prüft, damit ich keine Pflicht verteile, die mein Kind nicht erfüllen kann. Als *Sohn* möchte
ich keinen Malus für ein Ziel bekommen, das ich gar nicht erreichen kann.

## Ist-Stand am Code

Gemessen am 2026-11-15 im Worktree `backlog-lauf-1115` (Stand `d41df00a`), Pfade relativ zu `backend/Pugling.Api/`,
sofern nicht anders genannt.

- **Die Lernkontrolle kennt den Fall schon.** `Services/Shared/PositionPlayService.cs:94` leitet die Basisstufe ab
  (`pos.Stage ?? pos.Exercise?.DefaultStage ?? type.DefaultStage`), `:98` lässt sie nur gelten, wenn der Stufenplan
  nicht schon an Tag 1 beginnt, `:103-104` meldet `Unreachable`, sobald `RequireTypedTest` auf ein
  selbsteingeschätztes Segment trifft. `StageForDay` (`:73-81`, Rückfallkette `:80`) spielt dieselbe Kette.
- **Gewertet wird nur getippt.** `Controllers/Student/PositionTestsController.cs:155` (`typed = type.IsTypedStage(stage)`),
  `:178` (`Graded = typed`). Für Vokabeln ist getippt `LetterBoxes/FreeText/Audio/MultipleChoice/LetterScramble`
  (`Services/Shared/StageMechanics.cs:17-19`), `ShowBoth` ist Anzeigestufe (`Exercises/VocabularyExerciseType.cs:73`),
  `SelfAssess` (2) ist die Vorgabe jedes Typs ohne eigene (`Exercises/ExerciseTypeBase.cs:37`). Lückentext und
  Zuordnung tragen eigene, getippte Vorgaben (`Exercises/BuiltInExerciseTypes.cs:206`, `:324`), die übrigen Typen
  werten jede Stufe als getippt (`ExerciseTypeBase.cs:43`) – betroffen sind praktisch nur **Vokabel-Übungen**.
- **Die Validierung prüft nur zwei der drei Hälften.** `Exercises/RequireTypedTestValidation.cs:28-31` (`ProblemText`)
  prüft den Typ (gibt es überhaupt eine getippte Stufe?), `:38-42` (`LadderProblemText`) nur die Schritte des
  Stufenplans – **die Basisstufe prüft niemand**.
- **Aufrufer Position:** Anlegen `Controllers/Supervisor/PlanPositionsController.cs:170-174` (effektiver Wert
  `dto.RequireTypedTest ?? exercise.DefaultRequireTypedTest`), PATCH `:253-260` (Leiterprüfung nur, wenn der PATCH
  `RequireTypedTest` oder `StageSchedule` berührt; `Stage`/`ClearStage` `:271-273` lösen keine Prüfung aus).
- **Aufrufer Creator:** `Controllers/Creator/ExerciseControllerBase.cs:384-390` (Anlegen) und `:475-478` (PUT) prüfen
  `DefaultStage` und `DefaultRequireTypedTest` **je für sich** – dieselbe Form eine Ebene höher: eine Vokabel-Übung mit
  `defaultRequireTypedTest: true` ohne `defaultStage` ist zulässig und vererbt die unerreichbare Kombination an jede
  Position, die beides offen lässt (`PlanPositionsController.cs:199`; der KI-Agent legt Positionen genau so an,
  `../Pugling.Agent.Creator/PlanPlanner.cs:237`, `:243` – `Stage: null`, `RequireTypedTest: null`).
- **Das Formular erzeugt den Fall mit einem Klick.** `frontend/src/vater/PlanPositions.tsx:740-745`: beim Wählen einer
  Übung übernimmt das Formular `requireTypedTest` aus der Übung und setzt die Stufe auf „Standard der Übung“ (`""`);
  das Häkchen (`:470`) lässt sich unabhängig von der Stufe (`:416-418`) setzen.
- **Der Wizard auch:** `frontend/src/vater/VaterWizard.tsx:682-684` bietet alle Vokabelformen (auch 2 ·
  Selbsteinschätzung), `:695` das Häkchen; `frontend/src/vater/wizardFinish.ts:145-152` nimmt das Häkchen nur bei einer
  Leiter heraus (B-423, Entscheidung 5), nicht bei einer selbsteingeschätzten festen Stufe.
- **Die Anzeige für Bestand steht:** `frontend/src/vater/PlanPositions.tsx:993-997` zeigt `Unreachable` rot mit Grund.
- **Tests heute:** `../Pugling.Api.Tests/LernkontrolleTests.cs:127-135` legt genau diese Position per API an und
  erwartet `Unreachable` – nach dem Fix ist der Anlegeweg zu, der Fall wandert in eine Bestandszeile.

**Messung an einer Kopie der echten `pugling.db`** (2026-11-15; Kopie im Scratchpad, Original nicht geöffnet,
`-wal`/`-shm` lagen nicht vor): 39 Positionen, davon 13 mit `RequireTypedTest = 1` (12 Vokabel, 1 Lückentext).
**Betroffen: 0.** Alle 12 Vokabel-Positionen tragen eine feste getippte Stufe (4, eine des Demo-Kinds 5), keine einen
Stufenplan – Friedrichs sechs (Ids 30–35) und Theodors vier (26–29) stehen auf Stufe 4. Auch **keine** Vokabel-Übung
trägt `DefaultRequireTypedTest = 1` mit leerer oder selbsteingeschätzter `DefaultStage`.

## Die echte Lücke

Der Server lehnt „nur getippt“ auf einer selbsteingeschätzten **Basisstufe** nicht ab – weder an der Position (Anlegen,
PATCH über `Stage`/`ClearStage`/`RequireTypedTest`) noch an der Übungs-Vorgabe des Creators. Die Lernkontrolle zeigt es
danach rot, aber die Pflicht läuft schon, und der Malus bucht. An der echten DB heute 0 Fälle – eine offene Tür, kein
eingetretener Schaden.

## Offene Punkte

1. ~~Ablehnen oder still korrigieren (Häkchen weg / Stufe auf getippt)?~~ → Entscheidung 1.
2. ~~Gehört die Creator-Seite (`DefaultStage` × `DefaultRequireTypedTest`) dazu?~~ → Entscheidung 2.
3. ~~Was geschieht mit Bestandspositionen?~~ → Entscheidung 3.
4. ~~Prüft der PATCH auch, wenn nur `Stage`/`ClearStage` die Kombination kippt?~~ → Entscheidung 4.
5. ~~Wie verhält sich der Wizard (Stufe 2 + Häkchen)?~~ → Entscheidung 5.
6. ~~Ändert der Creator später `DefaultStage` einer benutzten Übung, kippen erbende Positionen – blockieren?~~ →
   Entscheidung 6.

## Entscheidungen

Gegrillt im Backlog-Lauf 2026-11-15. Die Empfehlung der Story („dieselbe Ablehnung auch für die Basisstufe; Bestand
bleibt, der Vater sieht ihn rot“) ist übernommen.

1. **Ablehnen mit `validation_error`, nicht still korrigieren.** *Stakeholder-Entscheidung im Auftrag des Nutzers.*
   Begründung: dasselbe Muster wie Typ- und Leiterprüfung (B-93, B-550); eine stille Korrektur änderte eine Aussage des
   Vaters, ohne dass er es sieht. Kosten: ein bisher gültiger Request (Häkchen + Standardstufe) liefert jetzt `400` –
   ein Verhaltensbruch, kein Vertragsbruch (kein Feld ändert sich).
2. **Die Creator-Seite gehört dazu.** *Stakeholder-Entscheidung im Auftrag des Nutzers.* `defaultRequireTypedTest: true`
   mit einer selbsteingeschätzten **effektiven** Vorgabestufe (`DefaultStage ?? type.DefaultStage`) wird beim Anlegen
   und beim PUT abgelehnt. Begründung: B-108 hat genau diese Fehlerklasse eine Ebene höher geschlossen; ohne sie träfe
   die neue Ablehnung den Vater beim Zuweisen für eine Vorgabe, die der Creator gesetzt hat, und der Standardweg des
   Formulars (Übung wählen → Häkchen und Stufe aus der Übung) wie der des KI-Agenten liefe auf `400`. Kosten: zweiter
   Aufrufer, ein Test mehr; an der echten DB 0 betroffene Übungen.
3. **Bestandspositionen bleiben unverändert und spielbar; der Vater sieht sie rot (`Unreachable`).** Keine Migration,
   keine Datenkorrektur. *Stakeholder-Entscheidung im Auftrag des Nutzers.* Begründung: heute 0 Fälle; ein stilles
   Umschreiben der Daten der Söhne wäre gefährlicher als die rote Zeile. Ein PATCH, der keine der vier Hälften berührt
   (Punkte, Malus, Rhythmus …), bleibt erlaubt – wie bei der Leiterregel (`PlanPositionsController.cs:255-256`).
   Kosten: ein solcher Bestand bucht weiter Malus, bis der Vater ihn ändert; bei 0 Fällen hingenommen.
4. **Der PATCH prüft die Kombination nach dem PATCH, sobald er `RequireTypedTest`, `Stage`, `ClearStage` oder
   `StageSchedule` berührt** – mit derselben Segmentregel wie `LearningCheckOf` (die Basisstufe zählt nur, wenn der
   Stufenplan nicht an Tag 1 beginnt). Validierung und Lernkontrolle teilen **eine** Segment-Berechnung, statt sie zu
   kopieren. *Stakeholder-Entscheidung im Auftrag des Nutzers.* Begründung: sonst kippt „Stufe auf Standard“
   (`clearStage`) eine gültige Position ungeprüft, und zwei Kopien der Regel liefen auseinander. Kosten:
   `LadderProblemText` geht in einer breiteren Prüfung auf.
5. **Wizard: bei einer selbsteingeschätzten Test-Stufe geht das Häkchen für Vokabel-Positionen nicht mit** – wie schon
   bei der Leiter (`wizardFinish.ts:145-152`, B-423 Entscheidung 5); die Zusammenfassung sagt es dazu. Im
   Positionsformular zeigt der `StatusBanner` die Server-Ablehnung, und `fieldHelp.ts` (`requireTypedTest`) bekommt
   einen Satz „braucht eine getippte Stufe“. *Stakeholder-Entscheidung im Auftrag des Nutzers.* Begründung: der Wizard
   legt viele Positionen in einem Rutsch an – ein `400` je Vokabel-Position wäre ein halb gebauter Plan; im
   Einzelformular ist die Ablehnung die ehrliche Rückmeldung. Kosten: im Wizard zählt bei Stufe 2 dann die
   Selbsteinschätzung (Lernkontrolle gelb: „Selbsteinschätzung zählt“).
6. **Eine spätere Änderung der Übungs-Vorgabe wird nicht wegen erbender Positionen blockiert.** Die Creator-Prüfung
   urteilt über das Paar der Übung; erbt eine Position, die das Häkchen selbst trägt, eine neue selbsteingeschätzte
   Vorgabe, zeigt die Lernkontrolle sie rot. *Stakeholder-Entscheidung im Auftrag des Nutzers.* Begründung: Creator und
   Vater sind verschiedene Sitze; eine Creator-Änderung an fremden Plänen zu messen, griffe in Pläne, die der Creator
   nicht sieht. Kosten: eine sichtbare, nicht stumme Restlücke.

## Akzeptanzkriterien

1. `POST …/positions` mit Vokabel-Übung, `requireTypedTest: true`, ohne `stage` und ohne Stufenplan liefert `400`
   `validation_error`; mit `stage: 4` `201`. *Rot ohne die Basisstufen-Prüfung im Anlegen* (heute `201`).
2. Dasselbe, wenn `requireTypedTest` aus der Übungs-Vorgabe kommt und die Position nichts sagt (Übungs-Vorgabe per DB
   gesetzt, weil AK 6 den Creator-Weg schließt). *Rot, wenn nur `dto.RequireTypedTest` statt des effektiven Werts
   geprüft wird.*
3. Mit Stufenplan ab Tag 1 (`[{dayNumber: 1, stage: 4}]`) ist die Basisstufe egal: `201`, Lernkontrolle `Typed`.
   *Rot, wenn die Prüfung die Basisstufe auch dann wertet, wenn der Plan an Tag 1 beginnt.*
4. `PATCH clearStage: true` auf eine Position `requireTypedTest + stage 4` (Übung ohne `defaultStage`) liefert `400`
   und lässt die Position unverändert; `PATCH requireTypedTest: true` auf eine Position ohne Stufe ebenso.
   *Rot ohne Prüfung bei `Stage`/`ClearStage` bzw. ohne die Basisstufe im PATCH.*
5. Ein PATCH, der nur `pointsGoalMet` ändert, auf eine Bestandsposition im Zustand `Unreachable` (per DB gesetzt)
   liefert `200`, die Lernkontrolle meldet weiter `Unreachable`. *Rot, wenn die Prüfung bei jedem PATCH läuft.*
6. Creator: Vokabel-Übung mit `defaultRequireTypedTest: true` und ohne `defaultStage` (bzw. `defaultStage: 2`) →
   `400` beim Anlegen und beim PUT; mit `defaultStage: 4` gültig. *Rot ohne die Creator-Prüfung.*
7. Wizard (`wizardFinish`): Vokabel-Übung, Stufe 2, Häkchen an → DTO trägt `requireTypedTest: false`; Stufe 4 →
   `true`. *Rot, wenn die Ausnahme nur an der Leiter hängt.*
8. Bestand: `Nur_Getippt_Auf_Selbsteinschaetzung_Ist_Unerreichbar` legt die Position direkt in der DB an (der API-Weg
   ist zu) und erwartet weiter `Unreachable`. *Rot, wenn `LearningCheckOf` den Fall nicht mehr erkennt.*

## Schätzung

**Größe S**, `wo: beides`, `migration: nein`, `vertragsbruch: nein`. Vergleich: wie B-547 (S, beides) – eine geteilte
Prüffunktion, drei Aufrufer, ein Satz im Wizard; kleiner als B-03 (M), weil kein neuer Pfad entsteht.

**Risiken**

- **Bestehende Tests und Seeds legen die Kombination an.** Mindestens `LernkontrolleTests.cs:127-135`; per Suche nach
  `requireTypedTest = true` ohne `stage` in `Pugling.Api.Tests` und `frontend/e2e` nachziehen (die E2E
  `lernkontrolle-sichtbar.spec.ts:51` setzt schon `stage: 4`). Seeds, die `DefaultRequireTypedTest` setzen, dürfen
  nach Entscheidung 2 nicht scheitern – Seed ist kein HTTP-Weg, aber eine geseedete Übung mit diesem Paar wäre selbst
  ein Fund.
- **Zwei Regeln für ein Segment.** Wird die Segmentregel kopiert statt geteilt, laufen Validierung und Lernkontrolle
  auseinander (Entscheidung 4) – darum zuerst die Extraktion.
- **Doku-Capture:** neue `400`-Beispiele nur, wenn ein `DocsCaptureTests`-Fall den Weg fährt; sonst unverändert.

**Angriffsplan (Backend zuerst)**

1. Segment-Hilfsfunktion aus `PositionPlayService.LearningCheckOf` herausziehen; `RequireTypedTestValidation` bekommt
   eine Prüfung über (Typ, effektives `requireTypedTest`, Basisstufe, Stufenplan), `LadderProblemText` geht darin auf.
2. `PlanPositionsController.Create` (Basisstufe `dto.Stage ?? exercise.DefaultStage ?? type.DefaultStage`) und
   `Update` (Auslöser `RequireTypedTest`/`Stage`/`ClearStage`/`StageSchedule`; Zustand nach dem PATCH, vor jeder
   Zuweisung).
3. `ExerciseControllerBase` Anlegen/PUT: Paar `DefaultRequireTypedTest` × effektive `DefaultStage`.
4. Tests AK 1–6 und 8; dann Frontend: `wizardFinish.ts` + Zusammenfassung, Satz in `fieldHelp.ts`, Vitest AK 7.

**Testweg**

- `Pugling.Api.Tests/LernkontrolleTests.cs`: AK 1–5 und 8 (Bestandszeile über den `AppDbContext` im Factory-Scope).
- `Pugling.Api.Tests/BirkenbihlExerciseTests.cs` (Heimat des B-108-Tests, `:405-411`): AK 6.
- `frontend/src/vater/wizardFinish.test.ts`: AK 7.
- Rote Probe je Fall wie in den AKs benannt; AK 1 muss gegen den heutigen Code rot sein (`201`).
- Rollengang: E2E `frontend/e2e/lernkontrolle-sichtbar.spec.ts` und die Planbau-Specs grün; als Vater im
  Positionsformular Häkchen an + Stufe „Standard der Übung“ → Fehlermeldung sichtbar.
- Reviewer: `pugling-reviewer` und `frontend-reviewer`.

## Verlauf

- **2026-11-15** — angelegt im Abschluss des Backlog-Laufs 2026-11-14: die E2E `lernkontrolle-sichtbar` erwartete für
  genau diese Position „Test getippt“ und fiel, nachdem der Server die Lernkontrolle selbst rechnete.
- **2026-11-15** — `ausformuliert` im Backlog-Lauf 2026-11-15: Ist-Stand am Code belegt, an der DB-Kopie 0 betroffene
  Positionen und 0 betroffene Übungen gemessen.
- **2026-11-15** — `gegrillt` im Backlog-Lauf 2026-11-15: sechs Stakeholder-Entscheidungen im Auftrag des Nutzers
  (Ablehnen, Creator-Seite mit, Bestand bleibt rot, PATCH-Auslöser, Wizard, keine Sperre über erbende Positionen).
- **2026-11-15** — `geschaetzt` im Backlog-Lauf 2026-11-15: S, beides, keine Migration, kein Vertragsbruch.
- **2026-11-15** — `geschaetzt` → **`abgenommen`** in Sprint 77 des Backlog-Laufs 2026-11-15, Code-Commit `627e081f`.
  Senke: `PositionPlayService.LearningCheckOf(type, requireTypedTest, baseStage, schedule)` – dieselbe Rechnung zeigt dem
  Vater „unerreichbar“ und lehnt über `RequireTypedTestValidation.UnreachableProblemText` ab (die alte
  `LadderProblemText` ist darin aufgegangen). Aufrufer: Anlegen, PATCH, Creator-Anlegen/PUT (`DefaultPairProblem`).
  Assistent: eine Regel `haekchenFaelltBeiVokabelnWeg` für Abschluss und Satz an den Vater.
  **Review-Funde behoben:** (1) der PATCH prüfte bei *gesendetem* statt *geändertem* Feld – das Formular schickt
  `requireTypedTest` und die Stufe bei jedem Speichern, Entscheidung 3 hätte für den Vater nicht gegolten; jetzt
  Auslöser bei Änderung; (2) der Assistent nahm das Häkchen still weg – jetzt Satz in Schritt 4 und im Überblick;
  (3) Feldhilfe sprach von „dem Server“ und versprach eine Ablehnung, die es im Assistenten nicht gibt – umformuliert;
  (4) veraltete Kommentare/Klassen-`<summary>` nachgezogen; (5) Test für den leeren Stufenplan bei „nur getippt“.
  Bewusst offen (kein eigener Story-Wert, im Protokoll vermerkt): die Ablehnung erscheint im Formular englisch
  (`validation_error` steht nicht in `GERMAN_PROBLEM_TEXT`, wie schon die Leiterregel); die Stufe 2 des Assistenten ist
  eine Kopie der Vokabel-Skala, weil das Manifest keine Stufenart trägt.
  - AK 1 → `LernkontrolleTests.Nur_Getippt_Ohne_Getippte_Basisstufe_Wird_Abgelehnt`, Mutante B552-AK1 → rot.
  - AK 2 → `…Geerbtes_Nur_Getippt_…`, Mutante B552-AK2 → rot.
  - AK 3 → `…Stufenplan_Ab_Tag_Eins_…`, Mutante B552-AK3 → rot.
  - AK 4 → `…Patch_Der_Die_Kombination_Kippt_…`, Mutanten B552-AK4a/4b → rot; leerer Stufenplan →
    `…Leerer_Stufenplan_Bei_Nur_Getippt_…`, Mutante R5 → rot.
  - AK 5 → `…Bestand_Nur_Getippt_…_Und_Aenderbar` (Punkte-PATCH, `clearStage` ohne Stufe, Formular-PATCH mit unveränderter
    Stufe und Häkchen), Mutanten B552-AK5, R1 (drei) → rot.
  - AK 6 → `…Creator_Vorgabe_Nur_Getippt_…`, Mutanten B552-AK6a/6b → rot.
  - AK 7 → `wizardFinish.test.ts` „nimmt das Häkchen bei selbst eingeschätzter Stufe …“, „sagt dem Vater …“; Mutanten
    B552-AK7, F2 → rot.
  - AK 8 → `…Bestand_…`, Mutante B552-AK8 → rot.
  - Belege: volle Suite **1670/1670**, Vitest **1093/1093**, `tsc -b` grün; Rollengang Vater (Playwright, frische Server,
    Ports 5251/5191): `position-zurueck-auf-standard.spec.ts` (Häkchen an, Stufe Standard → Ablehnung sichtbar) und
    sechs Nachbar-Specs – **13 passed**. Reviewer: `pugling-reviewer`, `frontend-reviewer`.
