---
tags: [typ/story, status/verworfen, bereich/gamification, bereich/punkte, rolle/student, rolle/supervisor]
aliases: [Wochen-Streak, Streak-Taktung]
status: verworfen
prio: P3
art: Frage
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/khan-academy-vergleich-plan.md#wo-khan-weiter-ist
unverifiziert: false
grund: "bleibt wie es ist – Entscheidung des Nutzers am 2026-10-23 (G-6): der Tages-Streak bleibt. Bei Pugling kommt der Druck aus der Pflicht (PenaltyCoins), der Streak ist reines Zuckerbrot (DailyBoxOptions.cs:25, PositionProgressService.cs:405). Khans Argument gegen Tagesdruck trifft darum nicht"
ersetzt_durch: []
---

# B-222 · Prüfauftrag: ist der Tages-Streak der richtige Default?

[B-105](B-105-taegliche-belohnungsbox.md) hat den Streak als **Tages**-Serie erfüllter Pflicht gebaut,
mit hartem Reset und Eskalation ab 7 und 30 Tagen. Khan Academy hat seinen Tages-Streak 2021 bewusst
abgeschafft und 2024 durch einen **Wochen**-Streak ersetzt (mindestens ein Skill je Woche auf
Proficient), weil Tages-Streaks Druck ohne Lerneffekt erzeugten. Zu prüfen: ob der Tages-Streak für ein
Kind mit `Daily`-Pflicht der richtige Default bleibt, ob die Taktung dem Pflicht-Rhythmus der Positionen
folgen sollte oder ob der Supervisor sie je Kind wählen können muss. Eine belegte Antwort „bleibt wie es
ist" ist ein zulässiges Ergebnis.

## Verlauf

- **2026-10-17** — angelegt (Quelle: Khan-Academy-Vergleich, Etappe 5 des Übernahmeplans).
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): der Streak ist reiner Belohnungs-Multiplikator (`DailyBoxOptions.cs:25`, `PositionProgressService.cs:405`) – Zuckerbrot, nicht Peitsche (die ist `PenaltyCoins`). Khans Argument gegen Tagesdruck trifft damit nur halb; „bleibt“ mit dieser Begründung ist ein naheliegender Ausgang.
- **2026-10-23** — **verworfen** in der Grill-Runde der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)) (G-6): der Tages-Streak bleibt der Default.
