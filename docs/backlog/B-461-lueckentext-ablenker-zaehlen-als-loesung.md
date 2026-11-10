---
tags: [typ/story, status/in-arbeit, bereich/katalog, rolle/creator, rolle/student]
aliases: [Ablenker als Lösung, Cloze-Alternativen]
status: in-arbeit
prio: P1
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: B-460
entgangen_bei: []
grund: ""
ersetzt_durch: []
---

# B-461 · Ein KI-Lückentext wertet die Ablenker der Wortbank als richtige Lösung

Im ersten Lehrplan-Lauf gegen das Access-Lehrwerk (2026-11-10, `qwen2.5:14b-instruct`) entstand Übung 25
„Unit 1 · London Reisebericht“ mit Selbsttest 100 %. Das Modell hatte seine Ablenker in die `alternatives` jeder
Lücke geschrieben, also in die Liste der Schreibweisen, die der Server **als richtig** gelten lässt. Für
„They [went] to the Tower“ zählten damit auch „came“, „flew“ und „drove“ als richtig. Der Selbsttest bemerkt das
nicht, weil er nur die eigene Lösung einsetzt. Kein Tor fängt es, und kein Kind beschwert sich darüber.

`entgangen_bei` bleibt leer: Die `ClozeStrategy` stammt aus dem Aufbau des KI-Creators vor dem Backlog-Bereich,
keine abgenommene Story trägt sie.

## Die echte Lücke

Der Entwurf ließ das Modell zwei Listen füllen, die es nicht unterscheiden konnte: `alternatives` (richtig) und
`wordBank` (Lösungen plus Ablenker). Ein Prompt-Satz behebt das nicht verlässlich. Die Struktur muss es
unmöglich machen.

## Akzeptanzkriterien

1. Der Entwurf hat kein Feld mehr, in das ein Ablenker als Lösung geraten kann: Richtige Schreibweisen gibt es
   nur in der Klammer (`[didn't|did not]`), Ablenker nur in `distractors`.
2. Ein Ablenker, der zugleich eine Lösung ist, wird gestrichen und zählt nicht als Ablenker. Ursprünglich war
   das als Regelverletzung geplant. Der zweite Live-Lauf zeigte: Das Modell kopiert die Lösungen in
   `distractors`, weil es das Feld als Wortbank liest. In der Wortbank stehen die Lösungen aber ohnehin, deshalb
   ist das harmlos und kein Grund für eine Reparatur-Runde. Es braucht mindestens zwei echte Ablenker.
3. Ein Test, der rot wird, sobald Ablenker als Alternativen im Katalog landen.

## Verlauf

- **2026-11-10** — angelegt und in Arbeit genommen. Gefunden bei der Analyse der KI-Aufgabenerstellung,
  behoben im selben Umbau ([B-460](B-460-ki-aufgaben-programm-entscheidet.md)).
