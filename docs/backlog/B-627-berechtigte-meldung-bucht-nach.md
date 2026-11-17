---
tags: [typ/story, status/idee, bereich/training, bereich/gamification, rolle/supervisor, rolle/student]
aliases: [berechtigte Meldung, Nachbuchen nach Rückmeldung]
status: idee
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "B-623, Entscheidung 6 (Grill-Runde mit dem Vater, 2026-11-17)"
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-627 · Eine vom Vater als berechtigt bestätigte Meldung bucht den Fehler des Kindes nach

Meldet das Kind zu einer Karte „falsch übersetzt“ oder „zu streng bewertet“ ([B-623](B-623-feedback-knopf-an-der-karte.md))
und hat es recht, zählt seine Antwort trotzdem als Fehler: Kasten zurück, keine Münzen, womöglich Malus für
die verpasste Pflicht. B-623 hat bewusst entschieden, dass eine Meldung selbst nichts bewirkt (Anti-Cheat).
Die Idee hier: Der Vater setzt eine Meldung auf **„berechtigt“**, und der Fehler, den das Kind an dieser
Karte gemacht hat, wird nachträglich als richtig gewertet – Kasten und Punkte nachgebucht, ein Malus dieses
Tages entfällt, wenn er nur daran hing.

Zu klären beim Ausformulieren: welche Antworten genau umgebucht werden (nur die gemeldete, alle seit der
Meldung, alle zu dieser Karte), wie das mit der Wallet-Invariante (`ConcurrencyStamp`) und der Tageslogik der
Pflicht zusammengeht, und ob ein bereits verbuchter Malus zurückerstattet wird.

## Verlauf

- **2026-11-17** — angelegt als Folge-Story aus B-623, Entscheidung 6.
