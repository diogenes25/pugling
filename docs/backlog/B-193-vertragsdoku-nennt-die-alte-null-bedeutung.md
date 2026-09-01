---
tags: [typ/story, status/ausformuliert, bereich/backend, bereich/doku, rolle/supervisor]
aliases: [PointsGoalMet null = 20, Swagger sagt die alte Regel]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
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
