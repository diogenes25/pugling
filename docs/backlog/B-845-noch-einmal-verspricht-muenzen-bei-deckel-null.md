---
tags: [typ/story, status/abgenommen, bereich/frontend]
aliases: []
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: "Nachschau Nachtlauf 2026-11-22-c (Review von B-660)"
grund: ""
ersetzt_durch: []
nachgeschaut: ""
entgangen_bei: [B-660]
wartet_auf: ""
---

# B-845 · „Noch einmal“ verspricht Münzen, auch wenn der Vater den Bonus-Deckel auf 0 gesetzt hat

## Ist-Stand am Code

`frontend/src/sohn/NochEinmal.tsx:37-45` (Texte `againLocked*` in `de.ts`/`en.ts`): Der gesperrte Zweig sagt „danach kannst du Übungen im Bonus-Training für Münzen wiederholen“ auch bei `Child.BonusDailyCoinCap = 0`; es gibt dann nie Münzen. Der freigeschaltete Zweig behandelt den Fall richtig (`againNoCoins`). Vorschlag: im gesperrten Zweig `dailyCoinCap === 0` abfragen und einen Satz ohne „für Münzen“ zeigen (wie `capZero` in `SohnBonus.tsx`).

## User Story

Als Sohn möchte ich vor erfüllter Pflicht nur versprochen bekommen, was es nachher wirklich gibt, damit ich der App
glaube, wenn sie „für Münzen“ sagt.

## Entscheidungen

*Stellvertretend entschieden im Nachtlauf 2026-11-23-c (Defekt, Freigabe 1), unbestätigt.*

1. **Eigener Satz ohne „für Münzen“** bei `dailyCoinCap === 0` (`againLockedNoCoins`, `againLockedWeekNoCoins`, je
   Ein-/Mehrzahl, deutsch und englisch) – dieselbe Bedingung wie `capZero` in `SohnBonus.tsx`. Kosten: vier Texte je
   Sprache. Ein Deckel `null` (kein Deckel) und ein Deckel > 0 behalten den alten Satz.

## Akzeptanzkriterien

1. Sperre zu, Deckel 0: der Satz nennt die offenen Ziele und kein „für Münzen“. Rot ohne die Abfrage.
2. Sperre zu, Wochentakt, Deckel 0: dasselbe mit Wochenzielen. Rot, wenn nur der Tagessatz die Abfrage bekommt.
3. Sperre zu, Deckel 90: der alte Satz mit „für Münzen“ bleibt. Rot, wenn die Abfrage auf `!dailyCoinCap` (fängt `null`
   nicht ab) oder dauerhaft auf „ohne Münzen“ kippt – dafür ein Fall mit Deckel `null`.

## Schätzung

XS, frontend, keine Migration, kein Vertragsbruch. Testweg: `NochEinmal.test.tsx`; Rollengang über die bestehende E2E
`nochmal-nach-pflicht` (Regression).

## Verlauf

- **2026-11-22** — Aus der Nachschau von B-660 im Nachtlauf 2026-11-22-c angelegt (Reviewer-Fund 🟢, ohne Zählung).
- **2026-11-23** — ausformuliert, gegrillt (stellvertretend, Defekt) und geschätzt im Nachtlauf 2026-11-23-c.
- **2026-11-23** — in-arbeit (Sprint 118). Rote Probe vor dem Fix: die zwei Fälle „verspricht bei Deckel 0 keine Münzen“
  erwartet rot, gemessen rot (2/2); die zwei Gegenfälle (Deckel `null`, 90) erwartet grün, gemessen grün – sie richten sich
  gegen eine falsche Reparatur.
  - AK 1 → `NochEinmal.test.tsx` „verspricht bei Deckel 0 keine Münzen (Daily)“, F1 (Abfrage `=== 0` aus) → rot.
  - AK 2 → derselbe Fall (Weekly), F3 (Wochenzweig ohne Abfrage) → rot.
  - AK 3 → „verspricht bei Deckel null weiter Münzen“, F2 (`!dailyCoinCap`) → rot.
  Review (`frontend-reviewer`): kein Befund im Diff; drei Stellen außerhalb mit demselben falschen Versprechen bei Deckel 0
  oder erreichtem Deckel (Runden-Hinweis, Startseiten-Teaser, Listenzeilen) → [B-862](B-862-bonus-versprechen-bei-deckel-null-ausserhalb-noch-einmal.md),
  bewusst nicht in B-845: das Sprint-Ziel hält ohne sie, und der Teaser braucht ein Vertragsfeld.
- **2026-11-23** — abgenommen (Sprint 118, Commit siehe Protokoll `docs/pm-sitzung-2026-11-23-c.md`). Verifikation nach
  der letzten Änderung: Backend 2130/2130 (Release), Vitest 1361/1361, `tsc -b` sauber; Mutanten F1–F3, E1–E2, K1–K10 rot
  (K10 erst nach plan-weiten Lernstand-Zeilen im Aufbau, vorher grün = ohne Beleg). Rollengang Sohn per E2E gegen frisch
  gestartete Server (Ausweichports 5211/5184): `nochmal-nach-pflicht`, `bonus`, `bonus-deckel`, `bonus-deckel-voll`,
  `bonus-satz-je-karte`, `nacharbeit`, `nacharbeit-bis-erledigt` – 7 passed. Vater und Creator Regressionszeugen (Suite
  grün, `bonus-deckel` fährt die Vater-Seite des Deckels).
