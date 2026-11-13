---
tags: [typ/story, status/idee, bereich/frontend, rolle/student]
aliases: [Lektionsraster, Rückmeldung als Blatt, Abschlussbild]
status: idee
prio: P1
art: Wunsch
wo: frontend
quelle: "docs/duolingo-snipwise-vergleich-plan.md#entscheidung-am-prototyp (Lektionsbildschirm des Prototyps, Nutzer im Chat 2026-11-13)"
unverifiziert: true
grund: ""
ersetzt_durch: []
wartet_auf: ""
---

# B-526 · Lektionsbildschirm nach festem Raster

Üben und Test der Sohn-Arcade sollen die Geometrie bekommen, die die Kinder aus Duolingo und Snipwise
kennen: oben Schließen und Fortschrittsbalken, darunter die **Arbeitsanweisung** als Überschrift, die
Frage in einer **Sprechblase** am Skin-Maskottchen, die Antwortfläche, und unten **ein** Knopf. Nach
„Prüfen" erscheint die Rückmeldung als **Blatt an derselben Stelle** (richtig/falsch, Lösung, Erklärung
auf Wunsch, „Weiter") statt als Toast. Am Ende steht ein Abschlussbild mit Münzen, Genauigkeit und Zeit.

Vorlage ist `frontend/src/prototyp/Lektion.tsx` (Wegwerf-Code, lokal bewertet). Nicht geprüft: wie das
Raster auf die bestehenden Antwortmodi von `SohnPractice`/`SohnTest` passt (Selbsteinschätzung,
Buchstabenkästchen, Hören, Wortkasten aus B-524) und was im Test gilt, der bewusst **kein**
Zwischenfeedback zeigt. Die Pfad-Startseite ist [B-525](B-525-sohn-startseite-als-schlangenpfad.md).

## Verlauf

- 2026-11-13 · `idee` – aufgenommen nach der Variantenwahl am Prototyp; Prio P1 ist ein Vorschlag.
