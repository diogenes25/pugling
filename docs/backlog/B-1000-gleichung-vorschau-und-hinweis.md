---
tags: [typ/story, status/idee, bereich/frontend, rolle/supervisor]
aliases: []
status: idee
prio: P3
art: Aufräumen
groesse: ""
wo: "beides"
migration: ""
vertragsbruch: ""
quelle: "Review von B-712 im Nachtlauf M 2026-11-25 (docs/pm-sitzung-2026-11-25-m.md)"
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-1000 · „Gleichung lösen“: Vater-Vorschau mit Mengen-Feldern, Trenner-Hinweis am Server statt im Frontend

Zwei Restpunkte aus dem Review von [B-712](B-712-sohn-eingabe-loesungsmenge.md), bewusst nicht im Sprint, weil das
Sprint-Ziel (der Sohn gibt Lösungsmengen in Feldern ein) ohne sie hält: (1) Der Testmodus des Vaters
(`frontend/src/vater/ExercisePreviewModal.tsx`) liest `PreviewItem.setAnswer` nicht und zeigt weiter das Textfeld mit dem
Trenner-Hinweis – der Vater probiert also eine andere Eingabe aus, als sein Kind bekommt. (2) Die Arcade blendet auf einer
Karte mit `setAnswer` **jeden** Hinweis aus (`SohnPractice`, `SohnBonus`, `SohnMischrunde`), nicht nur den Trenner-Hinweis;
heute trägt eine solche Karte keinen anderen, ein späterer echter Hinweis verschwände aber still. Sauberer: der Server lässt
`SeparatorHint` weg, wo er `SetAnswer` setzt (wie schon auf der Auswahlstufe, `QuadraticEquationExerciseType.CardForStage`).
