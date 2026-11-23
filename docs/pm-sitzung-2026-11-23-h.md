---
tags: [typ/protokoll, bereich/pm]
---

# PM-Sitzung: Nachtlauf H – Mischrunde über alle Pläne

**Datum:** 2026-11-23  ·  **Moderation:** PM (Nachtlauf, unbeaufsichtigt)
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Die Mischrunde aus B-218 bauen: fällige Karten über alle spielbaren Pläne in einer freiwilligen Runde, mit
Kasten der Herkunftsposition, Münzen unter dem gemeinsamen Bonus-Deckel und einem Einstieg in der Sohn-App.

Auftrag: „Starte 3 Nachtläufe" – Lauf H, gelesen als der Auftragstext aus [nachtlauf.md](nachtlauf.md) mit den
Freigaben 1–11. Paket fest vorgegeben: B-764, B-765, B-766 in dieser Reihenfolge; nichts außerhalb. Eigener Worktree
(Branch `worktree-agent-a0aab32232d36b515`, Basis `a782b4b2`), parallel zu den Läufen G und I. Story-Ids nur aus
B-920 … B-939, Sprints 130–132, API-Port 5221, Vite 5194; :5200 (Heimnetz, echte DB) bleibt unberührt.

## Runde 1 — Auswahl

Kein neuer Befund unter `docs/anmerkungen/` aufgenommen (Paket ist vorgegeben). Alle drei Stories sind `geschaetzt` +
`Wunsch` – baubar nach Freigabe 8. Ihre Entscheidungen stammen aus dem Vorbereitungslauf vom 2026-11-20 (Sonnet,
„stellvertretend, unbestätigt"); die Stories tragen sie als nummerierte `## Entscheidungen`, also gilt Freigabe 8 und
nicht Freigabe 9. Vorbedingung B-660 ist `abgenommen`. Die fremde Arbeit an `SohnApp.tsx` (B-766 Ist-Stand) ist
seit dem 2026-11-20 committet (Worktree sauber an `a782b4b2`).

| Sprint | Stories | Thema |
| --- | --- | --- |
| 130 | B-764, B-765 | Mischrunde im Backend: Auswahl, Lauf, Wertung, Münzen |
| 131 | B-766 | Mischrunde in der Sohn-App |

Zwei Sprints statt einem, weil Backend und Oberfläche getrennt prüfbar sind und der Fünfer-Zähler je Sprint so nicht
von drei Stories gemeinsam gespeist wird.

## Sprint 130 — Ziel & Umfang

**Sprint-Ziel:** Der Sohn kann nach seinen Tageszielen per API eine Mischrunde starten, die fällige, schon gelernte
Karten aus mehreren Plänen zieht (keine mit offener Pflicht), sie beantworten, und jede Karte rückt im Kasten ihrer
eigenen Position – mit Münzen nur für geprüfte Treffer und höchstens bis zum Deckel, den er mit dem Bonus-Training teilt.
**Umfang:** B-764 (M, backend, Migration), B-765 (L, backend).
**Entwickler-Brief:** Quelle der Wahrheit für „offen/gesperrt" und „offene Pflicht" ist **eine** Tagessicht
(`PositionProgressService.ComputeChildDayAsync`): die Sperre liest sie über `ExtraTrainingService.GateOf`, die Auswahl
schließt jede Position mit `Cadence != None && !GoalMet` aus (Woche = laufende Periode). Neue Tabellen `MixedReviewRuns`
und `MixedReviewCards` (Migration `MixMixedReviewRuns`), ein offener Lauf je Kind per gefiltertem Unique-Index. Gewertet
wird nur über die geteilten Bausteine: `ExercisePreviewService.CheckAsync` (gruppiert nach Übung und Stufe) und
`PositionPlayService.ApplyReview` auf der `PositionItemProgress` der Herkunftsposition – der
`PositionPracticeController` bleibt unberührt, ebenso die Aufbau-Stufen-Logik (gelesen nur über `PlayedExercise`).
Senke der Münzen ist `ExtraTrainingService.CoinsTodayAsync`: sie summiert beide Quellen, damit Bonus-Training und
Mischrunde denselben Deckel sehen. Testweg: `MischrundeAuswahlTests`, `MischrundeWertungTests`, ein Fall in
`ExtraTrainingTests`, Mutanten-Probe je Fall, `pugling-reviewer`.
