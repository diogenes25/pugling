---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/doku]
aliases: [frühe Rückgaben fehlen im B-369-Inventar, B-358 widerspricht sich]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: doku
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf S, Teil 2 in Lauf S, Teil 3 (docs/pm-sitzung-2026-11-04-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-370, B-358]
wartet_auf: ""
---

# B-373 · Ein Inventar, das wieder zu kurz war, und eine Begründung, die sich selbst widerspricht

Die Nachschau über `764f7b5` fand zwei Stellen, an denen Text aus diesem Lauf mehr behauptet, als der Code hält:

1. **B-370 AK 2** versprach, B-369 nenne „die frühen Rückgaben". Genannt waren zwei; per `return .*banner err` sind es acht –
   `VaterKind.tsx:46` und fünf in `VaterLernstand.tsx` fehlten. Wer B-369 nach dem Inventar baut, ließe sechs Lesefehler ohne
   „Erneut laden" liegen. Dieselbe Familie wie B-370 selbst: eine Zählung, die nur die schon bekannte Form sah.
2. **B-358** sagte nach dem Review in Entscheidung 1 „herstellbar, nur nicht lohnend", im Verlauf und im Entwickler-Brief des Protokolls
   stand weiter „keine rote Probe möglich".

## User Story

Als **Entwickler**, der B-369 baut, möchte ich ein vollständiges Inventar, und als Leser von B-358 eine Begründung, die nur eins sagt.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. Beide Texte berichtigen; keine Code-Änderung. Dazu zwei 🟢 derselben Nachschau: Zeilen `:78-81` statt `:79-82` in B-355, und die
   Brücke 25 → 26 in B-370 (`SohnSkins` fehlte in der 25).

## Akzeptanzkriterien

1. B-369 nennt acht frühe Rückgaben mit Zeilen; `grep "return .*banner err"` über `frontend/src` liefert dieselben acht.
2. Weder B-358 noch das Protokoll nennen eine rote Probe „nicht möglich".

## Schätzung

`XS`, nur Doku. Testweg: `grep`.

## Verlauf

- **2026-11-04** — angelegt aus der Nachschau zu Lauf S, Teil 2 (frischer `pugling-reviewer`, `git show 764f7b5`, read-only; zwei 🟡,
  zwei 🟢) und im selben Teil behoben, ohne Zählung (nur Text). AK 1: `grep -rn "return .*banner err" frontend/src` – erwartet 8,
  gemessen 8, deckungsgleich mit B-369. AK 2: `grep -n "rote Probe möglich"` über B-358 und Protokoll – nur noch das Zitat in der
  Review-Zeile. Es ist eine Entgleitung im Increment **dieses** Laufs: **Limit 2**, der Lauf endet nach Teil 3. **Abgenommen** mit
  Teil 3 (Rollengang: der Entwickler ist die Rolle; Doku, kein Verhalten).
