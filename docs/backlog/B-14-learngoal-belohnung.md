---
tags: [typ/story, status/verworfen, bereich/punkte, bereich/auswertung, rolle/supervisor]
aliases: [Belohnung bei erreichtem Lernziel]
status: verworfen
prio: P3
art: Wunsch
quelle: memory/learn-goals.md
grund: erfüllt durch KeyResult/ObjectiveRewardService (DB-Umbau E13)
---

# B-14 · Idempotente Belohnung, wenn ein Lernziel erreicht ist — **gegenstandslos**

> **Erledigt durch Wegfall (DB-Struktur-Umbau E13, `6471e1d`).** Die Lernziel-Ebene ist entfallen; ihre
> Rolle übernimmt das `KeyResult` eines Objectives – und **das bezahlt schon**: `ObjectiveRewardService`
> bucht je erreichter Etappe `RewardPerKeyResult` und beim Voll-Abschluss `RewardOnComplete`, idempotent
> über zwei gefilterte Unique-Indizes, mit `child.ConcurrencyStamp`-Bump. Genau das Muster, das diese Story
> forderte – es existierte auf der anderen Ebene bereits.

Lernziele wurden live ausgewertet, aber das Erreichen zahlte nichts aus. Nötig gewesen wäre eine
**einmalige** Belohnung über `ScoringService`/`WalletService` mit `child.ConcurrencyStamp`-Bump — nach dem
Muster von `PositionGoalReward`.

## Warum `verworfen` und nicht `abgenommen`

Weil **für diese Story nichts gebaut und nichts verifiziert wurde**: Es gibt keinen Ist-Stand mit Belegen,
keine Akzeptanzkriterien, keinen Commit, der ihr zuzurechnen wäre. Die Fähigkeit kam auf einem anderen Weg
ins Produkt. `abgenommen` behauptete beides und läse sich in einem halben Jahr wie eine gelieferte Funktion —
das ist die stille Lüge, gegen die dieser Bereich gebaut ist (siehe README, „Gegenstandslos heißt
`verworfen`"). So bleibt die Spur lesbar, *warum* der Punkt weg ist, ohne ihn als Lieferung zu verbuchen.

## Verlauf

- **2026-07-30** — geerntet (ungeprüft). Kollision mit E13 beim Ernten gefunden.
- **2026-07-30** — von einer parallelen Sitzung als gegenstandslos erkannt und auf `abgenommen` gesetzt; der
  Wächter beanstandete das mit neun fehlenden Belegen. Nach Prüfung von `ObjectiveRewardService` und
  Rückfrage beim Nutzer auf **`verworfen`** korrigiert, Grund im Feld `grund`. Die Begründung der
  Parallelsitzung ist inhaltlich bestätigt und bleibt erhalten.
