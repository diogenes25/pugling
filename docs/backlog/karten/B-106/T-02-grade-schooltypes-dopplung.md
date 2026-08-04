# T-02 · Grade/SchoolTypes-Dopplung zwischen Exercise-Metadaten und Lehrwerk

Status: entschieden     <!-- offen | beansprucht | entschieden -->
Typ: grilling           <!-- research | prototype | grilling | task -->
Blockiert durch:

## Frage

`Exercise` trägt eigene Klassenstufe/Schulart-Metadaten (siehe „Übungs-Metadaten" in
`backend/Pugling.Api/CLAUDE.md`); `TextbookSeries`/`SeriesUnit` tragen ebenfalls `Grade`
(`CurriculumEntities.cs:57`) und potenziell `SchoolTypes`-Filter (B-63 Entscheidung 7). Sobald jede
Übung über `SeriesUnitId` an eine Unit mit eigenem `Grade` hängt: bleibt `Exercise.Grade` als
unabhängiges Feld bestehen (zwei Wahrheiten, die auseinanderlaufen können), wird es aus der Unit
abgeleitet (keine eigene Spalte mehr), oder ist es ein bewusst redundantes Anzeige-Feld mit
Validierung gegen die Unit? B-63 Entscheidung 8 hat exakt diese Frage bereits als „Umbau am
Übungs-Katalog, nicht am Lehrwerk-Modell" zurückgestellt — hier ist sie fällig.

## Antwort

**Keine Dopplung, zwei verschiedene Fragen — kein Schema-Umbau nötig.** Am Code nachgeprüft
(`LearnEntities.cs:77-81`, `CurriculumEntities.cs:28-29,57`): `Exercise` trägt `GradeMin`/`GradeMax`
(eine **Spanne**) + `SchoolTypes`, `SeriesUnit` trägt `Grade` (ein **einzelner** Wert = das Lehrwerk-Band,
z. B. „Access 8"), `TextbookSeries` trägt eigene `SchoolTypes`. Unterschiedliche Form (Spanne vs.
Einzelwert) heißt bereits: unterschiedliche Aussage.

- `SeriesUnit.Grade`/`TextbookSeries.SchoolTypes` beschreiben eine **reale Tatsache über das Lehrbuch**
  (welches Band, für welche Schulart gedruckt) — unabhängig davon, wer die Übungen daraus benutzen darf.
- `Exercise.GradeMin/Max`/`SchoolTypes` sind **Such-/Discovery-Metadaten der geteilten Bibliothek**
  (`ExerciseControllerBase.cs:150,268-269,340-341`, direkt vom Creator gesetzt, in `ExerciseSummary`/
  `ExerciseDetail` projiziert): sie beantworten „für wen ist DIESE Übung pädagogisch geeignet", nicht
  „aus welchem Lehrwerk-Band stammt sie". Eine Grammatik-Übung kann bewusst `gradeMin:5, gradeMax:7`
  tragen, obwohl ihre Unit zu „Access 8" gehört (Wiederholungsstoff für Klasse 8) — das ist **keine**
  auseinanderlaufende Wahrheit, sondern eine bewusst weitere Spanne.

Eine Ableitung aus der Unit würde diese Flexibilität kappen (jede Übung müsste exakt das Band ihrer
Unit tragen), eine Validierung dagegen bräuchte eine Regel, die es fachlich nicht gibt („Spanne muss
den Unit-Wert enthalten" wäre eine erfundene Einschränkung, keine bestehende Nutzer-Anforderung).
**Kosten: keine** — kein Code-Änderung, kein neues `ApiErrors`, keine Migration. Einzige denkbare
UX-Verbesserung (nicht Teil dieser Entscheidung, da rein additiv und optional): `VaterExerciseCreate.tsx`
könnte `gradeMin`/`gradeMax` beim Anlegen mit dem `Grade` der gewählten Unit vorbelegen (reine
Formular-Vorbelegung, keine Persistenz-Kopplung) — als eigene, kleine Idee für einen späteren Sprint,
falls ein Creator das je als Reibung meldet.
