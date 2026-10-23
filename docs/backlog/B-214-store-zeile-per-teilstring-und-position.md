---
tags: [typ/story, status/idee, bereich/frontend]
aliases: [bilder.spec greift die Store-Zeile per first, Teilstring plus Position]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: Review-Runde zu B-153 (2026-09-04)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-214 · Die Store-Zeile wird per Teilstring **und** Position gegriffen — dieselbe Bauart wie B-153

`frontend/e2e/bilder.spec.ts:110` (nach dem Umbau von B-153) greift die Zeile der Vokabel so:

```ts
const row = vater.getByRole("row", { name: new RegExp(WORD) }).first();
```

Das ist ein Teilstring-Vergleich **plus** `.first()` — genau die Bauart, die B-153 an anderer Stelle
gerade entfernt hat, und der Kommentar drei Zeilen weiter unten argumentiert selbst dagegen („ein Bezug
per Position bleibt auch dann grün, wenn er das Falsche trifft").

**Warum das nicht harmlos ist:** Der Vokabel-Filter des Servers sucht `Contains`, und die Datenbank ist
innerhalb eines Playwright-Laufs zwischen den Specs geteilt. Sobald irgendwo eine Vokabel wie
`la ville nouvelle` in den Store gerät, trifft der Bezug zwei Zeilen, `.first()` greift still eine davon,
und die Bilder landen an der falschen Vokabel. Rot würde das erst im **Sohn**-Teil ab `:195` („kein
Bild") — rund hundert Zeilen und zwei Browser-Kontexte von der Ursache entfernt.

Im Unterschied zu B-153 flackert hier heute **nichts**: `la ville` ist im Seed eindeutig, und keine andere
Spec legt eine Vokabel mit diesem Präfix an. Es ist eine Falle, keine Störung.

## Offene Punkte

1. Wie greift man die Zeile stattdessen? Der zugängliche Name einer Tabellenzeile ist ihr gesamter Text
   (Wort, Übersetzung, Tags, Knöpfe) — ein `exact: true` ist hier also **nicht** die Antwort.
   *Empfehlung: über eine Zelle statt über die Zeile (`getByRole("cell", { name: WORD, exact: true })`
   und von dort mit `locator("xpath=ancestor::tr")` bzw. einem `filter({ has: … })` auf die Zeile), oder
   der Suchtreffer wird vorher auf genau eine Zeile eingeengt und das ausdrücklich zugesichert.*
2. Gilt derselbe Griff noch anderswo? *Empfehlung: beim Ausformulieren einmal über alle Specs greppen —
   `.first()` direkt hinter einem `RegExp`-Namen ist ein Muster, das sich suchen lässt.*

## Verlauf

- **2026-09-04** — aus der Review-Runde zu [B-153](B-153-bilder-spec-flackert-im-vollen-lauf.md).
  Bewusst **nicht** dort mitgebaut: B-153 hatte eine belegte Ursache und eine reproduzierbare rote Probe,
  dieser Punkt hat beides nicht, und die Reparatur ist keine Zeile (offener Punkt 1). Ein Fund beim Bauen
  wird eine eigene Story (README, „Ein Fund beim Bauen…").
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): besteht (`bilder.spec.ts:112`); ein Durchsuchen aller Specs findet kein weiteres Zeilen-Muster dieser Art. XS, autonom.
