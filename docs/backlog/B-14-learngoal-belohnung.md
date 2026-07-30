---
tags: [typ/story, status/abgenommen, bereich/punkte, bereich/auswertung, rolle/supervisor]
aliases: [Belohnung bei erreichtem Lernziel]
status: abgenommen
prio: P3
quelle: memory/learn-goals.md
unverifiziert: true
---

# B-14 · Idempotente Belohnung, wenn ein Lernziel erreicht ist — **gegenstandslos**

> **Erledigt durch Wegfall (31.07.2026, DB-Struktur-Umbau E13).** Die Lernziel-Ebene ist entfallen; ihre
> Rolle übernimmt das `KeyResult` eines Objectives – und **das bezahlt schon**: `ObjectiveRewardService`
> bucht je erreichter Etappe `RewardPerKeyResult` und beim Voll-Abschluss `RewardOnComplete`, idempotent
> über zwei gefilterte Unique-Indizes, mit `child.ConcurrencyStamp`-Bump. Genau das Muster, das diese Story
> forderte – es existierte auf der anderen Ebene bereits.

Lernziele werden live ausgewertet, aber das Erreichen zahlt nichts aus. Nötig wäre eine **einmalige**
Belohnung über `ScoringService`/`WalletService` mit `child.ConcurrencyStamp`-Bump — nach dem Muster von
`PositionGoalReward`.

**Ungeprüft und wichtig:** **E13 des DB-Umbaus löscht `LearnGoal`** zugunsten von `KeyResult` (siehe B-07),
und `KeyResult` hat bereits ein Belohnungslog. Möglicherweise ist diese Story nach E13 gegenstandslos oder
schrumpft auf „Objective-Belohnung scharf stellen".

## Verlauf

- **2026-07-30** — geerntet (ungeprüft). Kollision mit E13 beim Ernten gefunden.
