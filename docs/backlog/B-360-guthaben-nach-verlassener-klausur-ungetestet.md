---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/test, rolle/sohn]
aliases: [refreshWallet nach dem Verlassen ungetestet]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf P, Teil 1 in Lauf P, Teil 2 (docs/pm-sitzung-2026-11-03-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-356]
wartet_auf: ""
---

# B-360 · Dass die verlassene Klausur das Guthaben auffrischt, trägt kein Test

B-356 Entscheidung 3 hat zwei Hälften: nach dem Verlassen frischt `finish` Guthaben **und** Serie auf. Getestet war nur die
Serie; `SohnTest.test.tsx:24` ersetzte `refreshWallet` durch `() => {}`. Die Mutante „`refreshWallet()` im Verlassen-Zweig
gestrichen" (`SohnTest.tsx:70`) blieb grün.

## User Story

Als **Sohn** möchte ich nach einer verlassenen, aber abgegebenen Klausur die verdienten Münzen in der Kopfzeile sehen, und
als **Entwickler**, dass ein Test das hält.

## Ist-Stand am Code (Stand `bffb583`)

- `frontend/src/sohn/SohnTest.tsx:70` ruft `refreshWallet()` und `setStreak(…)`; `SohnTest.test.tsx:24` beobachtet nur
  `setStreak`.

## Die echte Lücke

Eine Hälfte einer Entscheidung ohne Zusicherung (die (AJ)-Zeile je Entscheidungsteil fehlte für diese Hälfte).

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **`refreshWallet` als `vi.fn()`, `toHaveBeenCalledTimes(abgegeben ? 1 : 0)`** im bestehenden `it.each`.

## Akzeptanzkriterien

1. Fällt `refreshWallet()` aus dem Verlassen-Zweig, wird der Fall „fertiger Versuch" rot.

## Schätzung

`XS`, nur Test. Testweg: `SohnTest.test.tsx`.

## Verlauf

- **2026-11-03** — angelegt aus der Nachschau zu Lauf P, Teil 1 (frischer `frontend-reviewer` über `bffb583`, 🟡). Im selben
  Sprint bearbeitet (Freigabe 3). Testlücke, das Verhalten stimmte: neuer Fall grün; Mutante „ohne `refreshWallet()` nach
  dem Verlassen" **1 failed** (AK 1). `SohnTest.test.tsx` **18/18**.
- **2026-11-03** — Review `frontend-reviewer`: sauber – `refreshWallet` wird nur im Verlassen-Zweig gerufen (`SohnTest.tsx:70`), der Zweig `:76` ist bei verlassener Seite unerreichbar; `toHaveBeenCalledTimes(abgegeben ? 1 : 0)` ist scharf.
- **2026-11-03** — Abnahme Lauf P, Teil 2: Vitest **620/620**, `tsc -b` sauber, E2E-Gesamtlauf **107 passed** (`exit=0`, nach dem letzten Code-Edit gestartet). Rollengang: Sohn maschinell über `SohnTest.test.tsx` (verlassene Klausur, Kopfzeile bekommt Münzen und Serie) und die Klausur-Specs; Creator, Vater Regressionszeugen. **Abgenommen**, Commit siehe Protokoll ([Lauf P](../pm-sitzung-2026-11-03-nacht.md)).
