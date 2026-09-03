---
tags: [typ/story, status/ausformuliert, bereich/frontend, typ/test]
aliases: [leere Zusicherung Einkleidung hinzufuegen]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: Nachschau 2026-09-03 zu B-206 (Nachtlauf)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-206]
wartet_auf: ""
---

# B-211 · Eine Zusicherung prüft den Ausgangszustand und kann darum nie fallen

Der **fünfte** gemessene Fall dieser Klasse in diesem Repo (nach B-154, B-161, B-157 zweimal) — und der
erste, den die Nachschau statt eines Reviewers gefunden hat.

## User Story

Als **Entwickler** möchte ich, dass ein Testfall, der die `busy`-Verdrahtung eines Knopfes behauptet, auch
fällt, wenn diese Verdrahtung verschwindet.

## Ist-Stand am Code

`frontend/src/vater/InterestChips.test.tsx:95` im Fall „sperrt … solange gespeichert wird":

```ts
expect(screen.getByRole("button", { name: "Einkleidung hinzufügen" }).hasAttribute("disabled")).toBe(true)
```

Der Knopf trägt `disabled={disabled || pick === ""}`. Der Test **wählt nichts aus**, `pick === ""` ist also
wahr — der Knopf ist im Ausgangszustand ohnehin gesperrt. **Rote Probe (gemessen):** `disabled ||` aus der
Bedingung streichen, und Zeile 95 bleibt grün.

Zeile 94 (der Entfernen-Knopf) trägt dagegen echt — der Fall ist also nicht wertlos, nur zur Hälfte.

## Offene Punkte

1. Eine Auswahl treffen und dann prüfen, oder den Knopf mit gesetztem `pick` rendern? *Empfehlung:
   Auswahl treffen* — das ist der Weg, den ein Nutzer nimmt, und er deckt zugleich, dass `pick` überhaupt
   gesetzt werden kann.

## Verlauf

- **2026-09-03** — aus der **Nachschau** zu B-206 (Nachtlauf), im Abnahme-Diff `5836585` entstanden. Rote
  Probe gemessen: Zusicherung bleibt grün, wenn `disabled` aus der Bedingung fällt.
