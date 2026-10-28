---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/student]
aliases: [Doppeltes Enter überspringt eine Karte]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: frontend-reviewer zu B-05 in Lauf D (docs/pm-sitzung-2026-10-26-tag.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-284 · Ein doppeltes Enter während der Prüfung überspringt eine Karte

In `frontend/src/sohn/SohnPractice.tsx` (`judge`) läuft `next()` bewusst außerhalb von `action.run`, und zwar immer.
Verwirft `action.run` einen zweiten Aufruf wegen der Sperre (`false`), weil ein Review noch läuft, ruft `judge` trotzdem
`next()` auf. Ein doppeltes Enter überspringt dann eine Karte. Das gilt für Buchstabenkästchen und Textfeld schon
länger, nicht erst seit [B-05](B-05-buchstaben-tausch.md). Aus Code gelesen (`frontend-reviewer`), nicht gemessen.

Vorschlag: `next()` nur aufrufen, wenn der Lauf nicht verworfen wurde.

## User Story

Als Kind möchte ich, dass ein doppelt gedrücktes Enter (oder ein Doppeltipp) genau **eine** Antwort zählt und genau
**eine** Karte weiterschaltet – keine Karte soll ungefragt an mir vorbeiziehen.

## Ist-Stand am Code

`frontend/src/sohn/SohnPractice.tsx` (`judge`, vor dem Fix): `await action.run(…); next();`. Die Sperre in `useAction`
verwirft den zweiten Aufruf (`run` liefert `false`, ohne Server-Aufruf), `next()` läuft trotzdem; der erste Aufruf ruft
nach seiner Antwort ein zweites `next()` – `setIdx((i) => i + 1)` zweimal, Karte 2 kommt nie. **Gemessen** (neuer
Komponententest, Ausgangsstand): der Bildschirm steht danach auf „Karte 3 / 3“. Das innere `try/catch` schluckt jeden
Fehler, `run` liefert hier also `false` **nur** für „verworfen“.

## Akzeptanzkriterien

1. Zweimal Absenden, während die erste Bewertung läuft, schickt eine Bewertung und rückt genau eine Karte weiter.
2. Ein einzelnes Absenden rückt weiter wie bisher, auch wenn die Bewertung scheitert.

## Entscheidungen

1. `if (ran) next()` statt eines zweiten Refs: die Sperre sagt schon, wer verworfen wurde. Kosten: keine; die Sperre
   bleibt die eine Quelle (Muster `frontend-schreib-primitive`).

## Schätzung

XS, `frontend`, keine Migration, kein Vertragsbruch.

## Verlauf

- **2026-10-26** — angelegt aus dem `frontend-reviewer` zu B-05 in Lauf D ([Protokoll](../pm-sitzung-2026-10-26-tag.md)).
- **2026-10-28** — in Lauf I, Teil 1 ([Protokoll](../pm-sitzung-2026-10-28-nacht.md)) unter Freigabe 1 ausformuliert,
  gegrillt, geschätzt (XS) und gebaut. Rote Probe gegen den Ausgangsstand: neuer Fall in `SohnPractice.test.tsx`
  **1 failed** (erwartet „zwei“, gemessen „Karte 3 / 3“ mit „drei“); nach dem Fix **1 passed**.
- **2026-10-28** — Review (`frontend-reviewer`): 🟡 Mutante `if (!ran) next()` blieb grün (die Zwischen-Zusicherung fehlte),
  AK 2 nur als Text. Behoben: Zwischenstand „eins“ nach einem leeren `act` (ohne das prüfte die Zeile vor dem
  verworfenen Aufruf – gemessen: Mutante zuerst weiter grün) und ein Fall „gescheiterte Bewertung rückt weiter“.
  Mutanten danach: `!ran` **2 failed**, alter Code **1 failed**, innerer Fang weg **1 failed**; Kontrolle **2 passed**.
  🟢 ohne Zählung angenommen: ein Enter während „anderes Bild“ tut jetzt sichtbar nichts (vorher: Karte ohne Bewertung
  weiter). Vitest **563/563**, `tsc -b` sauber, volle E2E **99 passed** (`exit=0`, nach der letzten Änderung
  gestartet) – der Spielweg der Suite als Rollengang; den Doppel-Enter selbst trägt der Komponententest, Browser-Extension
  nicht genutzt. **`abgenommen`.**
