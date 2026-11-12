---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/student]
aliases: [Wortbank Handy, Wortbank als Chips]
status: abgenommen
prio: P1
art: Defekt
groesse: S
wo: "frontend"
migration: nein
vertragsbruch: nein
quelle: Nachmessung zu B-486 auf Wunsch des Nutzers, 2026-11-12 (Wegwerf-DB, 375×812)
entgangen_bei: []
grund: ""
ersetzt_durch: []
---

# B-489 · Sohn-App: Lückentext mit Wortbank am Handy kaum bedienbar

Gemessen bei 375×812 an einem Lückentext mit acht Lücken und 22 Wörtern in der Wortbank (Stufe 2):

- **Übungsrunde** (`/sohn/practice/{id}`): der Text in 22 px füllte den ganzen Schirm, der erste Wortknopf lag
  bei 1068 px (unter dem Rand), die Seite war 2521 px hoch. Beim Wählen war das „?“ nicht mehr zu sehen.
- **Klausur** (`/sohn/test/{id}`): erste vier Knöpfe sichtbar, Seite 1814 px.

Ursache: `.btn` hat `width: 100%` – jedes Wort einer Wortbank stand in einer eigenen vollbreiten Zeile, obwohl
die Gruppe `flexWrap` trägt. Dazu kein Höhenlimit für einen langen Trägertext.

## User Story

Als Sohn möchte ich am Handy die gefragte Lücke und die Wortbank zugleich sehen, damit ich wähle, ohne zwischen
Text und Knöpfen hin- und herzuscrollen.

## Akzeptanzkriterien

1. Ab `WORD_BANK_MIN` (7) Wahlmöglichkeiten erscheinen sie als Chips nebeneinander (`.choices-bank`), in Übung und
   Klausur. Multiple-Choice mit wenigen Optionen bleibt vollbreit.
2. Die aktive Lückentext-Karte begrenzt den Text auf 40vh und rollt die gefragte Lücke darin ins Bild
   (`ClozePrompt fitAsked`). Die Ergebnisliste der Klausur rollt nicht.
3. Chips bleiben mindestens 44 px hoch.

## Verlauf

- 2026-11-12 – gebaut (Commit 093112d): `lib/choices.ts` (`isWordBank`/`choicesClass`), `ClozePrompt` mit `fitAsked`, Vater-Testmodus
  (B-486) auf denselben Mechanismus umgestellt. Vitest 758/758, `tsc -b` sauber; rote Probe (Grenze 4, `fitAsked`
  ohne Wirkung) lässt genau die tragenden Fälle fallen.
- 2026-11-12 – Rollengang bei 375×812 gegen eine Wegwerf-DB (Backend :5201, Heimnetz-Server unberührt):
  Übung 2521 → 1071 px Seite, erster Chip bei 564 px statt 1068 px, Chips 44 px hoch; bei Karte 6 steht das „?“
  sichtbar im Textkasten. Klausur 1814 → 1079 px. Vater-Testmodus weiter mit Kasten und „?“.
  **Rest:** die letzten zwei Chip-Reihen (von sieben) liegen in der Übung noch unter der Navigationsleiste und
  brauchen einen kurzen Wisch. Bewusst nicht kleiner gemacht: unter 40vh wird ein langer Text unlesbar eng.
