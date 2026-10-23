---
tags: [typ/story, status/idee, bereich/klassenarbeit, bereich/training, rolle/student, rolle/supervisor]
aliases: [Klausur-Countdown, Klassenarbeit treibt den Plan]
status: idee
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/knowunity-vergleich-plan.md#wo-knowunity-weiter-ist
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-248 · Die Klassenarbeit wird beim Kind sichtbar und verdichtet den Plan vor dem Termin

Eine `Klassenarbeit` trägt ein `ScheduledDate` und ein Übungsbündel (direkt über `KlassenarbeitExercise`
und über Tags via `KlassenarbeitTag`), wirkt aber auf nichts: der Plan ändert sich vor dem Termin nicht,
und die Sohn-App zeigt die Arbeit nicht an (in `frontend/src/sohn/` kommt sie nur in einem Kommentar in
`SohnTest.tsx` vor). Knowunity baut den Tagesplan aus dem Prüfungstermin und verdichtet ihn, je näher er
rückt. Die Idee: (a) Countdown in der Sohn-Arcade („Englisch-Arbeit in 5 Tagen"), (b) eine
Bereitschaftsanzeige „wackelig → sicher" über die Items des Bündels, (c) die Positionen, deren Übungen
im Bündel liegen, werden in einem Fenster vor dem Termin stärker gewichtet – etwa durch eine zusätzliche
Tagesmission oder ein angehobenes Ziel. Der Vater setzt den Termin, die App erzwingt die Vorbereitung:
das ist die Kernidee in Reinform. Offen ist, ob die Verdichtung Pflicht (mit Malus) oder Angebot (mit
Bonus) sein soll und wer das Fenster festlegt.

## Verlauf

- **2026-10-23** — angelegt (Quelle: Knowunity-Vergleich, Etappe 1 des Übernahmeplans).
