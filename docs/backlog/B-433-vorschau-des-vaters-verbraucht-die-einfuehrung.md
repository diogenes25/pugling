---
tags: [typ/story, status/idee, bereich/training, rolle/supervisor, rolle/student]
aliases: [Vater-Übung verbraucht Einführung, Supervisor-Runde führt ein]
status: idee
prio: P3
art: Frage
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-08-nacht.md
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-433 · Spielt der Vater eine Übungsrunde des Kindes, sind dessen neue Wörter danach „schon gesehen"

Seit B-419 ist die erste Begegnung eines Wortes eine Kennenlernkarte – gemerkt am Positions-Fortschritt
(`PositionItemProgress.IntroducedAt`). Der Vater darf die Übungsendpunkte des Kindes mitspielen (Vorschau/Nachtrag,
`PositionPracticeController` ohne Rollenzweig im `Review`). Spielt er eine Runde durch, setzt sein Review das
`IntroducedAt` – das Kind bekommt die Karte danach sofort getippt, ohne sie je gesehen zu haben.

Vom `pugling-reviewer` in Lauf AE als 🟢 gemeldet (gleiche Wurzel wie zuvor: Vater-Reviews schreiben den Fortschritt
des Kindes), bewusst nicht im Sprint: das Sprint-Ziel betrifft den Weg des Kindes. Die Frage für den Dialog: soll eine
Vater-Runde den Lernstand des Kindes überhaupt schreiben, oder ist der Nachtrag genau dafür da?

**Zweite Folge (Nachschau Sprint 22):** der Server entscheidet „Einführung" beim Review aus der Datenbank, nicht aus der
ausgelieferten Karte. Wird ein Wort zwischen `/cards` und dem Review woanders eingeführt (zweiter Tab, Vater-Runde),
wertet der Server das „Weiter" der Kennenlernkarte (ohne Antwort) als getippte, **falsche** Antwort und setzt die Karte
einen Kasten zurück – das Kind sieht davon nichts.

## Verlauf

- **2026-11-08** — angelegt aus dem Review von B-419 (Lauf AE, Sprint 22).
- **2026-11-08** — ergänzt um die zweite Folge aus der Nachschau zu Sprint 22 (Lauf AE).
