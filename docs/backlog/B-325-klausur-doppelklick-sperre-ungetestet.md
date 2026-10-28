---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/student]
aliases: [SohnTest Doppelklick ohne Fall]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf H, Teil 2 in Lauf I (docs/pm-sitzung-2026-10-28-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-320]
wartet_auf: ""
---

# B-325 · Die Doppelklick-Sperre der Klausur trägt keinen Fall

[B-320](B-320-klausur-fehler-erreicht-den-bildschirm-nicht.md) AK 3 („ein verworfener Doppelklick zeigt nichts“) und
das Ref-Gate aus B-49 in `frontend/src/sohn/SohnTest.tsx` (`action.run` um `answerTest`) hält kein Test. Mutante:
`action.run(…)` durch einen direkten Aufruf ersetzen → alle Fälle grün, ein Doppelklick schickt zwei `answerTest`.

Vorschlag: zweimal synchron klicken, solange `answerTest` hängt; `toHaveBeenCalledTimes(1)` und keine Meldung.

## User Story

Als Kind möchte ich, dass ein Doppelklick in der Klausur eine Antwort zählt – nicht zwei auf dieselbe Frage.

## Ist-Stand am Code

`SohnTest.tsx` `answerAndAdvance` liegt in `action.run` (Ref-Gate, B-49); richtig, aber ohne Fall.

## Akzeptanzkriterien

1. Zwei Klicks im selben Tick, während die Antwort hängt: ein `answerTest`, Weiterrücken auf die nächste Frage, kein
   Fehlerkasten.

## Entscheidungen

1. Ein Fall in `SohnTest.test.tsx` mit hängender Antwort. Kosten: keine.

## Schätzung

XS, `frontend`, keine Migration, kein Vertragsbruch.

## Verlauf

- **2026-10-28** — angelegt aus der Nachschau (frischer `frontend-reviewer`, `git show f96d358`) in Lauf I
  ([Protokoll](../pm-sitzung-2026-10-28-nacht.md)); aus Code gelesen, Mutante hergeleitet, nicht gemessen.
- **2026-10-28** — in Lauf I, Teil 2 ([Protokoll](../pm-sitzung-2026-10-28-nacht.md)) unter Freigabe 1 ausformuliert, gegrillt, geschätzt (XS) und gebaut. Kein Code-Fix (Testlücke). Mutante `action.run(fn)` → `fn()` → **1 failed**; Kontrolle **4/4**.
- **2026-10-28** — Review (`frontend-reviewer`) 🟡: der Fall erreichte die Ref-Sperre nie – zwischen zwei `fireEvent` rendert React, `disabled` fing den zweiten Klick ab (Mutante „Ref-Prüfung weg“ grün, gemessen). Behoben: beide Klicks in einem `act`; Mutante danach **1 failed**, Kontrolle **6/6**. 🟢 ohne Zählung: `.error-box`-Zusicherung als Nebenwirkung beschriftet. Vitest **569/569**, `tsc -b` sauber, volle E2E **99 passed** (alle `exit=0`, E2E nach der letzten Änderung gestartet); Browser-Extension nicht genutzt. **`abgenommen`.**
