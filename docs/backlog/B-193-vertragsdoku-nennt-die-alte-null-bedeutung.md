---
tags: [typ/story, status/geschaetzt, bereich/backend, bereich/doku, rolle/supervisor]
aliases: [PointsGoalMet null = 20, Swagger sagt die alte Regel]
status: geschaetzt
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau 2026-09-01 zu B-45
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-45]
wartet_auf: ""
---

# B-193 · Das Vertragsdokument nennt für `PointsGoalMet` noch die alte `null`-Bedeutung

## User Story

Als **Entwickler an der API** möchte ich, dass die Beschreibung eines Feldes sagt, was der Server tut,
damit ich nicht gegen eine Regel baue, die es seit B-45 nicht mehr gibt.

## Ist-Stand am Code

`backend/Pugling.Contracts/Supervisor/StudyPlanDtos.cs:99` beschreibt `<param name="PointsGoalMet">` mit
**„`null` = 20"**. Seit `48f06b1` gilt das nicht mehr: `null` heißt „die Empfehlung der Übung, sofern sie
in (0, 200] liegt — sonst 20".

**Die sechs Geschwister direkt darunter (`:101-105`) tragen die richtige Formel.** Es ist also keine
vergessene Konvention, sondern eine einzelne übersehene Zeile — ausgerechnet die, die der Supervisor in
Swagger zum `null`-Verhalten liest.

**Warum das mehr ist als ein Tippfehler:** Dasselbe Commit-Paar hat `LearnEntities.cs:68` und die
`ExercisePayload`-Beschreibung umgeschrieben, **weil** sie logen. Die Story hat also gewusst, dass die
Doku nachzuziehen ist, und eine Stelle verfehlt. Der Text fließt über `///` ins OpenAPI-Dokument, ist
also Vertragsoberfläche und nicht bloß Quelltext.

## Die echte Lücke

Kein Tor prüft, ob eine Beschreibung des `null`-Verhaltens mit dem Code übereinstimmt — und es ließe sich
auch keines mechanisch bauen. Der Fund gehört darum in die Nachschau, wo er entstanden ist.

## Entscheidungen

1. **Die Zeile wird auf die Formel ihrer Geschwister gebracht**, nicht frei umformuliert.
   *Begründung:* Die vier Zeilen darunter tragen dasselbe Muster und sind richtig; eine sechste
   Formulierung derselben Aussage wäre eine weitere Fassung, die driften kann. *Kosten:* keine.
2. **Die Grenze wird mitgenannt.** Nach B-192 lautet die Wahrheit: die Empfehlung der Übung, sofern sie in
   (0, 200] liegt — sonst 20. *Begründung:* Ohne die Grenze wäre die neue Zeile schon wieder halbwahr, und
   genau diese Halbwahrheit ist der Anlass dieser Story. *Kosten:* Die Zeile wird länger als ihre
   Geschwister; das ist der Preis dafür, dass sie stimmt.
3. **Diese Story kommt NACH B-192.** *Begründung:* Sonst beschriebe sie einen Zustand, den es erst danach
   gibt. *Kosten:* keine — dieselbe Sitzung.

## Akzeptanzkriterien

1. Die `PointsGoalMet`-Zeile in `StudyPlanDtos.cs` beschreibt das tatsächliche Verhalten inklusive Grenze.
2. `docs/openapi/v1.json` trägt den neuen Text (der Testlauf erzeugt es).
3. Keine andere Vorgabewert-Beschreibung derselben Datei widerspricht dem Code — **nachgezählt**, nicht
   vermutet, und die Zahl steht im `## Verlauf`.

## Schätzung

- **Größe: XS** — eine Zeile plus die erzeugten Artefakte. **Migration/Vertragsbruch: nein.**
- **Testweg:** kein eigener Test, und das ist argumentiert statt ausgelassen. Das Tor ist der ohnehin
  laufende `DocsCaptureTests`-Durchgang, der `docs/openapi/v1.json` neu schreibt: Bliebe der alte Text
  stehen, wäre die Story nicht erledigt. Akzeptanzkriterium 3 ist eine **Zählung**, kein Test.

## Offene Punkte

1. Nur diese Zeile, oder alle Aussagen über `null`-Vorgabewerte im Vertrag einmal gegenlesen?
   Empfehlung: **greppen** — das Muster ist trennscharf, die Menge auszählbar, und die Zahl entscheidet,
   ob es eine XS- oder eine S-Story ist. Nicht vermuten. Verwandt: B-102 hat die Regel für
   Vorgabewert-Angaben schon einmal geschärft.
2. `DocsCaptureTests` schreibt `docs/api-examples` bei jedem Lauf neu — wandert der Text automatisch mit,
   oder gibt es eine zweite Fassung? Nachsehen.

## Verlauf

- 2026-09-01 · Aufgenommen aus der **Nachschau** zu B-45 (Pflichthandlung der Retrospektive, Nachtlauf).
  `entgangen_bei: [B-45]`.
- 2026-09-01 · **`ausformuliert` → `gegrillt` → `geschaetzt`** (zweiter Nachtlauf, Freigabe 1). Fundort
  selbst nachgelesen: Die `PointsGoalMet`-Zeile sagt „`null` = 20", die vier Zeilen darunter tragen die
  richtige Formel — der Kontrast steht wörtlich nebeneinander. XS.
