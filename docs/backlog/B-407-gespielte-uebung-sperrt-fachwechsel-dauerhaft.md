---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/frontend, rolle/creator]
aliases: [Gespielte Übung sperrt Fachwechsel dauerhaft, Art im Bearbeiten-Dialog]
status: abgenommen
prio: P3
art: Frage
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-05.md#lauf-ab
grund: ""
nachgeschaut: 2026-11-15
ersetzt_durch: []
---

# B-407 · Eine gespielte Übung mit einer Art des alten Fachs sperrt den Fachwechsel ihrer Reihe dauerhaft

Seit B-291 sperrt eine Übung mit einer Kategorie („Art") des alten Fachs den Fachwechsel der Reihe. Die Art ist im Bearbeiten-Dialog nicht
änderbar, also bleibt nur Löschen – und das ist gesperrt, solange die Übung benutzt wird (`exercise_in_use`). Ist ihre Lehrplan-Position
schon gespielt, lässt sie sich nicht mehr entfernen (`position_has_data`), steckt sie im Plan eines fremden Kindes, kommt der Creator gar
nicht heran. Dann kann die Reihe ihr Fach **nie** mehr wechseln.

**Frage:** Ist das hinnehmbar (ein Fachwechsel einer benutzten Reihe ist selten und eher ein Fehler), oder braucht es einen Ausweg – etwa
die Art im Bearbeiten-Dialog änderbar bzw. entfernbar machen (dann gilt wieder die Regel, dass ein `PUT` die Kategorie gegen das Fach der
Reihe prüft)?

## User Story

Als **Creator** möchte ich die Art einer Übung im Bearbeiten-Dialog ändern oder entfernen können, damit eine Reihe, deren Übungen schon
gespielt werden, ihr Fach noch wechseln kann, ohne dass ich eine Übung löschen muss, an der der Lernstand eines Kindes hängt.

## Ist-Stand am Code

- **Die Sperre:** [TextbookSeriesController.cs:195-198](../../backend/Pugling.Api/Controllers/Creator/TextbookSeriesController.cs#L195)
  zählt Übungen der Reihe mit einer Art oder einem Grammatik-Thema des alten Fachs; der Aufruf steht in `Update` bei `:231-232` (Methode `SubjectInUseProblemAsync`, `:193`).
- **Die API hat den Ausweg schon:** Der `PUT` einer Übung nimmt `CategoryId` an
  ([ExerciseAuthoringDtos.cs:29](../../backend/Pugling.Contracts/Creator/ExerciseAuthoringDtos.cs#L29)), prüft sie nur gegen das
  **aktuelle** Fach der Reihe ([ExerciseControllerBase.cs:470](../../backend/Pugling.Api/Controllers/Creator/ExerciseControllerBase.cs#L470),
  `CategoryValid` erlaubt `null`, `:161-165`) und schreibt sie bei `:501`. Eine Spiel-Sperre auf dem `PUT` gibt es für die Art nicht
  (`UpdateConflictAsync`, `:115`, Aufruf `:486`, betrifft nur Inhaltslisten).
- **Die Oberfläche versperrt ihn:** [ExerciseEditModal.tsx:251](../../frontend/src/vater/ExerciseEditModal.tsx#L251) reicht
  `detail.categoryId` unverändert durch (Kommentar `:43`: „Kategorie" gehört zu den Feldern, die das Formular nicht anfasst). Die
  Grammatik-Themen dagegen sind im Dialog änderbar (`GrammarTopicPicker`, `:404-409`) – für sie besteht der Ausweg also schon,
  belegt durch `ReiheFachwechselTests.Ausweg_Themen_Herausnehmen_Entsperrt_Den_Wechsel` (`:148`).
- **Arten laden kann das Frontend:** `api.categories(subjectId)` wird im Wizard schon benutzt (`VaterWizard.tsx:158`, ebenso `ExerciseFilterBar.tsx:47`).

## Die echte Lücke

Der Satz „nie mehr" stimmt für die API nicht, aber für den Vater an der Oberfläche: Er sieht die Art im Dialog nicht und kann sie nicht
entfernen. Ein Backend-Test, der den API-Ausweg pinnt, fehlt ebenfalls.

## Offene Punkte

1. ~~Hinnehmen oder Ausweg?~~ → Entscheidung 1
2. ~~Ausweg im Backend oder im Dialog?~~ → Entscheidung 2
3. ~~Welche Arten bietet der Dialog an?~~ → Entscheidung 3

## Entscheidungen

Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13, [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).

1. **Es braucht einen Ausweg, der keine Übung löscht.** Begründung: Der einzige heute sichtbare Ausweg (Löschen) nähme dem Kind den
   Lernstand, und die echte DB darf das nicht still verlieren. *Kosten:* ein Feld mehr im Dialog.
2. **Der Ausweg liegt im Bearbeiten-Dialog, das Backend bleibt, wie es ist.** Die Story folgt dem eigenen Vorschlag: die Art wird im
   Dialog änderbar und entfernbar, der `PUT` prüft sie weiter gegen das aktuelle Fach der Reihe. Ein Backend-Test pinnt den Weg.
   *Kosten:* keine Vertragsänderung; der Test kostet einen Fall.
3. **Der Dialog bietet die Arten des Fachs der Reihe plus „– keine Art –".** Begründung: Mehr nimmt der Server ohnehin nicht an
   (`invalid_reference`). *Kosten:* eine Ladeanfrage beim Öffnen; schlägt sie fehl, bleibt die Art unverändert (Muster `ListenAusfall`).

## Akzeptanzkriterien

1. API: Eine Übung mit einer Art des alten Fachs, deren Lehrplan-Position schon gespielt ist, lässt sich per `PUT` mit
   `categoryId: null` speichern (`200`); danach geht der Fachwechsel der Reihe durch.
2. Dialog: Im Bearbeiten-Dialog steht ein Feld „Art" mit den Arten des Reihen-Fachs und „– keine Art –"; der `PUT` schickt den Wert
   aus dem Formular statt aus dem geladenen Stand.
3. Ohne Änderung am Feld schickt der Dialog die geladene Art unverändert mit (der Vollersatz verliert nichts).

## Schätzung

**Größe: S** – ein Feld im Dialog, eine Zeile in `payloadFrom`, ein Backend-Test. `migration: nein`, `vertragsbruch: nein`
(Vertrag unverändert).

**Risiken:**

- `payloadFrom` ist exportiert und gegen genau diese Falle getestet (`ExerciseEditModal.test.ts:44`): wer `categoryId` aus dem Formular
  nimmt, muss `initialForm` mit der geladenen Art füllen, sonst leert ein reines Titel-Speichern die Art still.
- Eine Übung einer fachlosen Reihe (Altbestand vor B-106) bekommt keine Art-Liste; das Feld zeigt dann nur „– keine Art –".

**Angriffsplan (Backend zuerst):**

1. `ReiheFachwechselTests.cs`: Fall für AK 1 schreiben (sollte ohne Code-Änderung grün sein – er pinnt den vorhandenen Weg).
2. Frontend: `categoryId` in `MetaForm`/`initialForm`, Auswahlfeld mit `api.categories(detail.subjectId)`, `payloadFrom` nimmt
   `form.categoryId`; Kommentar `:43` nachziehen.
3. Vitest in `ExerciseEditModal.test.ts`, kurzer E2E-Schritt optional.

**Testweg:**

- `backend/Pugling.Api.Tests/ReiheFachwechselTests.cs`, neuer Fall `Ausweg_Art_Entfernen_Entsperrt_Den_Wechsel_Auch_Bei_Gespielter_Uebung`
  (AK 1). Rot, wenn jemand dem Übungs-`PUT` eine Spiel-Sperre für die Art gibt oder `CategoryValid` `null` ablehnt – genau die
  Änderungen, die den Ausweg wieder schließen. Da er heute grün ist, wird er als **Absichtserklärung** beschriftet (CLAUDE.md, Vorbild
  `wizardSearch.test.ts`) und seine rote Probe per Mutante belegt.
- `frontend/src/vater/ExerciseEditModal.test.ts`: (AK 2) geänderte Art landet im Payload – rot, solange `payloadFrom` `detail.categoryId`
  nimmt; (AK 3) unveränderte Art bleibt – rot, wenn `initialForm` die Art nicht übernimmt.

## Verlauf

- **2026-11-05** — angelegt aus dem Review zu B-406 (Lauf AB, Sprint 18; `pugling-reviewer`, 🔴 am Satz – der versprach einen Ausweg, den
  es in diesen Fällen nicht gibt; der Satz ist berichtigt, die Grenze selbst ist eine Produktfrage). Am Code gelesen, nicht gemessen.
- **2026-11-13** — **ausformuliert** (Backlog-Lauf): am Code belegt, dass der API-`PUT` die Art schon ändern und leeren darf, auch bei
  gespielter Übung; die Lücke liegt im Dialog.
- **2026-11-13** — **gegrillt**: Entscheidungen 1–3 (der Vorschlag der Story übernommen). Stakeholder-Entscheidung im Auftrag des Nutzers
  (Backlog-Lauf 2026-11-13, [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).
- **2026-11-13** — **geschaetzt**: S, beides, keine Migration, kein Vertragsbruch.
- **2026-11-13** — Im Backlog-Lauf 2026-11-13 bewusst **nicht** in einen Sprint genommen: der Dialog-Teil ändert `ExerciseEditModal.tsx`, das die parallele B-534-Arbeit unkommittiert umbaut. Nach B-534 baubar wie geschätzt.
- **2026-11-14** — Abgleich Backlog-Lauf: Kern gilt (Dialog reicht `detail.categoryId` weiter durch, `ExerciseEditModal.tsx:251`; kein Art-Feld). Zeilenangaben im Ist-Stand berichtigt. Der Hinderungsgrund vom 2026-11-13 ist weg: B-534 ist committet (`a3e5c1ba`), `ExerciseEditModal.tsx` ist wieder frei.
- **2026-11-14** — Sprint 76, gebaut: Backend unverändert (Entscheidung 2), Absichtserklärung `ReiheFachwechselTests.Ausweg_Art_Entfernen_Entsperrt_Den_Wechsel_Auch_Bei_Gespielter_Uebung` (AK 1, prüft vorab `position_has_data` und `exercise_in_use`); Dialog: Feld „Art" (`CategoryField`, Arten des Reihen-Fachs + „– keine Art –", geladene Art bleibt bei Ladeausfall wählbar), `MetaForm.categoryId`, `initialForm` übernimmt die geladene Art, `payloadFrom` nimmt `form.categoryId`; Vitest in `ExerciseEditModal.test.ts` (AK 2, AK 3).
- **2026-11-14** — **abgenommen** (Sprint 76). volle Suite 1650/1650 (Release, nach den Review-Fixes), Vitest 1079/1079, `tsc -b` grün; Rollengang als Playwright gegen frisch gestartete Server nach dem letzten Edit: 38 passed (Assistent inkl. `assistent-klassenarbeit`, Lehrwerk-/Reihen-/Fachlehrer-Formulare, `vater-von-null`, `kind-anlegen-id`, `uebung-teilspeichern`, `erstbegegnung`); Reviews `pugling-reviewer` + `frontend-reviewer`, Funde im Sprint behoben; Commit `8e970493`. Mutante Q8 (`categoryId` nicht aus dem Formular) rot (AK 2). Backend-Test als Absichtserklärung beschriftet (Ausweg „Art entfernen“ war serverseitig schon offen). Rollengang: `uebung-teilspeichern` grün.
- **2026-11-15** — Nachschau im Gesamt-Review des Laufs (Backend + Frontend über `40dd9bfa..HEAD`): Funde aus dem Zusammenspiel im Lauf behoben (Lernkontrolle vom Server `learningCheck`, Truhe/Bonus-Texte aus `rewardsUnlocked`, Agent prüft `canAssign` vor dem Schreiben, `--cadence None` abgelehnt, Truhen-Meldung und Tutorial-Halbsatz); siehe Protokoll „Abschluss“.
