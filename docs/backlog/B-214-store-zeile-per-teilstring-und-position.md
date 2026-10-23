---
tags: [typ/story, status/abgenommen, bereich/frontend]
aliases: [bilder.spec greift die Store-Zeile per first, Teilstring plus Position]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Review-Runde zu B-153 (2026-09-04)
unverifiziert: false
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

## User Story

Als **Entwickler, der eine rote E2E liest**, möchte ich, dass `bilder.spec.ts` genau die Vokabel greift, um die es
geht, damit ein Rot an der Ursache steht und nicht hundert Zeilen später im Sohn-Teil.

## Ist-Stand am Code

`frontend/e2e/bilder.spec.ts:112` (vor dem Bau): `getByRole("row", { name: new RegExp(WORD) }).first()` – Teilstring
plus Position; der Server-Filter sucht `Contains`, die DB ist zwischen den Specs eines Laufs geteilt.

## Akzeptanzkriterien

1. Die Zeile wird über die exakte Wort-Zelle gegriffen, und eine Mehrdeutigkeit fällt laut an dieser Stelle.
2. Eine Probe mit einer Zwillings-Vokabel zeigt: alt rot, neu grün.

## Offene Punkte

1. Wie greift man die Zeile stattdessen? Der zugängliche Name einer Tabellenzeile ist ihr gesamter Text
   (Wort, Übersetzung, Tags, Knöpfe) — ein `exact: true` ist hier also **nicht** die Antwort.
   *Empfehlung: über eine Zelle statt über die Zeile (`getByRole("cell", { name: WORD, exact: true })`
   und von dort mit `locator("xpath=ancestor::tr")` bzw. einem `filter({ has: … })` auf die Zeile), oder
   der Suchtreffer wird vorher auf genau eine Zeile eingeengt und das ausdrücklich zugesichert.*
2. Gilt derselbe Griff noch anderswo? *Empfehlung: beim Ausformulieren einmal über alle Specs greppen —
   `.first()` direkt hinter einem `RegExp`-Namen ist ein Muster, das sich suchen lässt.*

## Entscheidungen

Autonom gegrillt (`art: Defekt`, Freigabe 1).

1. **Über die Wort-Zelle, exakt, plus `toHaveCount(1)`** (Empfehlung aus Punkt 1): `getByRole("row").filter({ has: getByRole("cell",
   { name: WORD, exact: true }) })`. Die Wort-Zelle ist `<td>{v.word}</td>` (`VaterVocab.tsx:474`). *Kosten:* bricht, wenn die Zelle
   einmal mehr als das Wort trägt – dann laut an dieser Zeile, nicht hundert Zeilen später.
2. **Punkt 2:** die Sichtung fand kein weiteres `.first()`-Muster; der Reviewer nennt `full-flow.spec.ts:189` mit demselben
   Teilstring, dort aber ohne `.first()` – Strict Mode scheitert laut. Bewusst belassen.

## Schätzung

- **groesse:** XS · **wo:** frontend · **migration:** nein · **vertragsbruch:** nein. **Testweg:** Probe mit Zwillings-Vokabel, Playwright voll.

## Verlauf

- **2026-09-04** — aus der Review-Runde zu [B-153](B-153-bilder-spec-flackert-im-vollen-lauf.md).
  Bewusst **nicht** dort mitgebaut: B-153 hatte eine belegte Ursache und eine reproduzierbare rote Probe,
  dieser Punkt hat beides nicht, und die Reparatur ist keine Zeile (offener Punkt 1). Ein Fund beim Bauen
  wird eine eigene Story (README, „Ein Fund beim Bauen…").
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): besteht (`bilder.spec.ts:112`); ein Durchsuchen aller Specs findet kein weiteres Zeilen-Muster dieser Art. XS, autonom.
- **2026-10-23** — vierter Nachtlauf, Sprint 9 (NP-12, [Protokoll](../pm-sitzung-2026-10-23-nacht.md)): ausformuliert, gegrillt, gebaut. Rote Probe (Auflage 5, Skript): eine Zwillings-Vokabel „ala ville" (enthält WORD, sortiert davor) per API eingeschleust – **alte Fassung: 1 failed, rot an `:150` der Probe-Fassung** (Bilder-Panel nicht gefunden, weit hinter der Ursache, wie die Story vorhersagte); **neue Fassung: 1 passed**. Ohne Zwilling im vollen Lauf grün. `frontend-reviewer`: korrekt. Playwright voll **82 passed**. Rollengang: Ersatz benannt – die Spec selbst fährt den Weg im echten Browser. Abgenommen; Commit: `90387d5`.
