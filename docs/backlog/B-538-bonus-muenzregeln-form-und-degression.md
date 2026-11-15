---
tags: [typ/story, status/gegrillt, bereich/gamification, rolle/student, rolle/supervisor]
aliases: [Bonus-Münzregeln, Degression je Wiederholung, getippt zahlt drei]
status: gegrillt
prio: P1
art: Wunsch
quelle: B-537 (Teil a, Entscheidungen 4–7)
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-538 · Bonus-Münzregeln: Form bestimmt den Wert, Wiederholung halbiert ihn

Teil a von [B-537](B-537-lehrplan-als-kapitel-pflicht-und-kuer.md). Ist-Stand am Code (Punkte 2, 5, 6, 7) und
Begründungen stehen dort; diese Story trägt nur ihren Ausschnitt.

## User Story

Als *Sohn* möchte ich nach der Pflicht im Bonus-Training für schwerere Abfragen mehr Münzen bekommen und Übungen
wiederholen dürfen, auch wenn jede Wiederholung am selben Tag weniger bringt, damit sich zusätzliches Üben lohnt.
Als *Vater* möchte ich, dass es außer dem Bonus-Training keinen zweiten freiwilligen Münzweg gibt, damit die Pflicht
immer zuerst kommt.

## Ist-Stand am Code

Siehe [B-537 → Ist-Stand](B-537-lehrplan-als-kapitel-pflicht-und-kuer.md#ist-stand-am-code), Punkte 2, 5, 6 und 7.

## Entscheidungen

Übernommen aus B-537: **4** (Positionen ohne Pflicht zahlen 0), **5** (Auswahl 1, getippt 3), **6** (Halbierung
je Lauf am selben Tag, alle Übungsarten), **7** (Deckel-Vorgabe 90).

## Akzeptanzkriterien

1. Eine richtige Antwort in einer Plan-Position mit `Cadence.None` bucht **keine** Münzen (weder Basis noch Combo
   noch Tempo); Leitner-Stand und Lernstand bewegen sich weiter wie bisher.
2. Bonus-Training zahlt je Treffer: Auswahl 1, getippt 3, Selbsteinschätzung 0 (Vorgaben in
   `Gamification:ExtraTraining`).
3. Derselbe Übungs-Lauf am selben Tag zahlt beim 1. Mal 100 %, beim 2. 50 %, beim 3. 25 %, ab dem 4. 0 % – der
   Lauf-Wert wird abgerundet. Am nächsten Tag zahlt der erste Lauf wieder 100 %.
4. Nicht-Vokabel-Übungen bleiben nach einem Lauf im Bonus-Vorrat und sind wiederholbar (Ausnahme wie heute: was
   schon Plan-Position des Kindes ist, bleibt draußen). Die Formleiter der Vokabel-Übungen bleibt.
5. Tagesdeckel-Vorgabe 90; ein am Kind gesetzter Deckel gewinnt weiter.
6. Der Bonus-Vorrat zeigt je Übung, was der nächste Lauf heute wert ist (z. B. „50 %").
7. Die Arcade zeigt bei einer Position ohne Pflicht „ohne Münzen"; der Positions-Editor im Vater-Web blendet die
   Punkte-Felder für `Cadence.None` aus.

## Verlauf

- 2026-11-15 · `gegrillt` angelegt durch Teilung von B-537 (Entscheidung 11); Entscheidungen dort.
