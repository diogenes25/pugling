---
tags: [typ/story, status/geschaetzt, bereich/lehrplan, bereich/training, rolle/student, rolle/supervisor]
aliases: [Dosis aus der Tagesaufgabe]
status: geschaetzt
prio: P1
art: Wunsch
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: B-736
grund: ""
ersetzt_durch: []
---

# B-1038 · Im scharfen Modus nennt die Tagesaufgabe die Zahl der neuen Wörter

Dritter Teil aus der Teilung von [B-736](B-736-pflicht-aus-der-tagesaufgabe.md) (Nachtlauf P, 2026-11-27): Kriterium 9. Baut auf
[B-1036](B-1036-pflicht-quelle-tagesaufgabe.md) auf.

## Ist-Stand am Code

`PositionPlayService.DailyDose` (`Services/Shared/PositionPlayService.cs:230`, `:236`) liest `NewItemsPerDay`; Leser: die Lernrunde (`:497`),
die Antwortprüfung (`PositionPracticeController.cs:595`), der Testanteil (`PositionTestsController.cs:183`), die Vater-Maske
(`PlanPositionsController.cs:48`). **0 heißt dort „keine Dosis“ (alles), nicht „keine neuen Wörter“** – eine Aufgabe mit `NewWords = 0`
(nur Wiederholung, Deckel erreicht) muss aber null neue Wörter bringen.

## Entscheidungen (geerbt)

B-736 Kriterium 9: das Kind bekommt genau so viele neue Wörter, wie die Aufgabe nennt; Vater-Maske und Spielen kennen dieselbe Dosis.

### Offener Punkt (für den Vater)

Eine Position **ohne** Aufgabe an einem scharfen Tag (Kür) spielt mit ihrer eigenen `NewItemsPerDay` – damit lässt sich der Wörter-Deckel des
Planers über Kür umgehen. Ob Kür an scharfen Tagen neue Wörter bringen darf, ist eine Produktfrage und hier nicht entschieden.

## Akzeptanzkriterien

1. An einem scharfen Tag bringt eine Aufgabe genau `NewWords` neue Wörter, auch 0. *Rot, wenn* eine Aufgabe mit 0 neue Wörter einführt
   oder mit 3 mehr als 3.
2. Die Vater-Maske zeigt dieselbe Dosis wie das Spielen. *Rot, wenn* beide verschieden sind.
3. An Tagen ohne scharfen Plan bleibt `NewItemsPerDay`. *Rot, wenn* sich das Bestandsverhalten ändert.

## Schätzung

**Größe: S** — eine Dosis-Funktion mit Aufgaben-Quelle und einem Wert „keine neuen“; vier Leser.

## Verlauf

- **2026-11-27** — angelegt durch Teilung von B-736 (Nachtlauf P), `geschaetzt`; der offene Punkt zur Kür berührt die Kriterien nicht und bleibt beim Vater.
