---
tags: [typ/story, status/geschaetzt, bereich/frontend, rolle/supervisor, rolle/student]
aliases: [Start-Stufe im Positionsformular, Stufenband]
status: geschaetzt
prio: P1
art: Wunsch
groesse: M
wo: frontend
migration: nein
vertragsbruch: nein
quelle: B-704
unverifiziert: false
grund: ""
ersetzt_durch: []
wartet_auf: "B-701 (Position spielt die Aufbau-Stufe) – nicht bauen, bevor B-701 gebaut ist"
---

# B-841 · Start-Stufe im Positionsformular, Stufenband beim Kind, Stufe in der Vater-Auswertung

Teil von [B-704](B-704-aufbau-weg-in-der-oberflaeche.md) (geteilt). Der Rest der Oberfläche zur Aufbau-Stufe; er
braucht [B-701](B-701-position-spielt-die-aufbau-stufe.md), weil Start-Stufe und Stufenband `CurrentBuildLevel` an der
Position lesen und schreiben.

## User Story

Als *Vater* möchte ich beim Anlegen einer Position sehen, ob zur Übung Vorstufen existieren, und festlegen, auf welcher
Stufe mein Kind beginnt. Als *Sohn* möchte ich sehen, auf welcher Stufe ich bin und wie viele noch kommen.

## Entscheidungen

Geerbt aus B-704 (am 2026-11-19 bestätigt): **Entscheidung 1** (Feld „Start-Stufe" nur, wenn die Übung Vorstufen hat),
**Entscheidung 3** (Stufenband „Stufe n von m" in der Kopfzeile von Üben und Test, kein Bedienelement),
**Entscheidung 4** (Vater-Auswertung zeigt die aktuelle Stufe; „Zeit auf der Stufe" bleibt in B-702).

## Akzeptanzkriterien

B-704 AK 3, 4, 5, 6 wörtlich (siehe dort), dazu AK 7 für die neuen Felder.

## Schätzung

**Größe: M** – wie in B-704 geschätzt, abzüglich des Editors (B-840). Testweg wie B-704: `PlanPositionsAufbau.test.tsx`,
`SohnStufenband.test.tsx`, E2E `aufbau-stufen.spec.ts` als Rollengang.

## Verlauf

- **2026-11-22** — Aus B-704 geteilt (Nachtlauf 2026-11-22-c, Opus). Wartet auf B-701; der Zuschnitt von B-701 ist
  offen (siehe dort, Nachprüfung vom 2026-11-22).
