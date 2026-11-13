---
tags: [typ/story, status/idee, bereich/api, bereich/gamification, rolle/student]
aliases: [Tempo-Bonus-Messung, Antwortzeit je Karte]
status: idee
prio: P3
art: Wunsch
wo: beides
quelle: "B-526, Entscheidung 11 und Risiko 1 der Schätzung (2026-11-13)"
unverifiziert: false
grund: ""
ersetzt_durch: []
wartet_auf: ""
---

# B-530 · Antwortzeit ab der sichtbaren Karte messen

Der Schnell-Bonus misst die Antwortzeit als Abstand zur vorigen Antwort der Sitzung. Seit das Blatt aus
[B-526](B-526-lektionsbildschirm-festes-raster.md) auf „Weiter“ wartet, zählt die Lesezeit dort in die nächste
Karte – der Bonus belohnt dann das Wegdrücken der Rückmeldung. Dieselbe Wirkung hatte schon [B-434](B-434-schnellbonus-misst-die-lesezeit-der-loesung-mit.md) für das Anhalten nach einem Fehler (B-420) gemeldet; sie ist hier aufgegangen. Messstelle: `PositionPracticeController.cs:418-420`. Gewünscht ist eine Messung ab dem Moment, in dem
die Karte sichtbar wird. Offen ist vor allem, wie der Server einer vom Client gemeldeten Zeit trauen kann, ohne
dass sich der Bonus erschwindeln lässt (heute schützt die Server-Uhr samt Untergrenze von einer Sekunde).

## Verlauf

- 2026-11-13 · `idee` – abgespalten aus B-526; Prio P3 ist ein Vorschlag.
- 2026-11-13 · Backlog-Abgleich: B-434 hier aufgenommen (dieselbe Messstelle); Code-Behauptung bestätigt, `unverifiziert` entfällt.
