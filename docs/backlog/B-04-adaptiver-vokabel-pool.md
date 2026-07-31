---
tags: [typ/story, status/geschaetzt, bereich/training, bereich/auswertung, lerntechnik/vokabeln, rolle/supervisor, rolle/student]
aliases: [Adaptiver Pool, Idee 3]
status: geschaetzt
prio: P4
art: Wunsch
groesse: M
wo: backend
migration: offen
vertragsbruch: nein
quelle: docs/backlog-vokabellernen.md#runde-1--idee-3-adaptiver-vokabel-pool-je-position
---

# B-04 · Adaptiver Vokabel-Pool je Position

## User Story

Als Vater möchte ich an einer Vokabel-Position einen größeren Pool (z. B. 100 Vokabeln) mit einer täglichen
Teilmenge (z. B. 20/Tag) hinterlegen, wobei im Abschlusstest nicht gekonnte Vokabeln in den nächsten Tag
nachrücken — sodass über die Laufzeit möglichst jede Pool-Vokabel einmal korrekt im Abschlusstest vorkommt.

## Ist-Stand am Code · Entscheidungen

→ Grill-Protokoll vom 2026-07-30, **Idee 3**, Entscheidungen 8–10:
[backlog-vokabellernen.md](../backlog-vokabellernen.md#runde-1--idee-3-adaptiver-vokabel-pool-je-position).

Kern, belegt: `ItemCount` ist **kein Tageskontingent, sondern ein Abschneiden**
(`PositionPlayService.DueItemIndicesAsync:75-89`, `Enumerable.Range(0, poolSize)` plus Fortschrittsfilter
`p.ItemIndex < poolSize`) — Vokabel 21 bis 100 einer Übung mit `ItemCount = 20` sind **nie** dran.
Umgekehrt macht „Durchgefallenes kommt wieder" **Leitner heute schon**. Es fehlen genau zwei Dinge: ein
Deckel auf die Tagesmenge und ein Pool, der nicht abgeschnitten wird — beides in *dieser einen Methode*.
Die ursprüngliche L/XL-Schätzung kam nicht von der Mechanik, sondern von der ungeklärten Semantik.

## Akzeptanzkriterien

1. `ItemCount` (oder ein Nachfolgefeld) begrenzt den **Pool** und schneidet die Item-Liste nicht mehr ab;
   ein Tagesdeckel begrenzt die **täglich ausgespielte** Menge aus den fälligen Items.
2. Zuvor durchgefallene vor noch nie gezeigten (`WeakestFirst` deckt das vermutlich ab — zu prüfen).
3. Eine im Abschlusstest korrekt beantwortete Pool-Vokabel wird nicht mehr ausgespielt; fällt sie später
   durch, kehrt sie zurück.
4. Fortschrittsanzeige „X von Y geschafft" über die Laufzeit.
5. Ein **`KeyResult`** auf Übungs-Scope kann „Pool durch bis Datum" verbindlich machen.

## Schätzung

**Größe: M** (vorher L/XL) — zwei Änderungen in `DueItemIndicesAsync`, eine Test-Treffer-Abfrage, ein
additiver Metrik-Wert, eine Fortschrittsanzeige. Die separate Design-Etappe ist nach dem Grillen entfallen.

- **`migration: offen`** — die eine bewusst zurückgestellte Frage: braucht der Tagesdeckel ein **neues
  Feld** (⇒ Migration) oder wird `ItemCount` **umgedeutet** (⇒ keine)? Das ist die erste Entscheidung beim
  Anfassen, nicht ein Versäumnis der Schätzung.
- **Nachbarschaft, am 2026-07-30 aufgelöst:** Kriterium 5 hing ursprünglich an `LearnGoal` — **E13 hat die
  Ebene gelöscht** (`6471e1d`), ihre Rolle trägt jetzt das `KeyResult` eines Objectives. Das ist für diese
  Story eine *Verbesserung*: `KeyResult` belohnt schon idempotent per Lazy Settlement
  (`ObjectiveRewardService`), Kriterium 5 braucht also keine eigene Belohnungsmechanik mehr. Die im
  Grill-Protokoll erwogene Erweiterung „ein additiver `LearnGoalMetric`-Wert" heißt jetzt
  `KeyResultMetric` — und dort ist zu prüfen, ob „im Abschlusstest einmal korrekt" ein neuer Wert ist oder
  auf einen bestehenden abbildet.
- **Nebeneffekt:** Macht B-02 nachträglich gegenstandslos, weil der heutige Hilfetext dann *stimmt*.
- **Testweg:** Integrationstest über mehrere simulierte Tage (Pool 5, Deckel 2): jede Vokabel kommt dran,
  Durchgefallenes kehrt zurück, Gekonntes fällt raus.

## Verlauf

- **2026-07-30** — geerntet aus dem Grill-Protokoll vom selben Tag, Stufe `geschaetzt` übernommen. Beim
  Ernten zusätzlich gefunden: die Abhängigkeit von `LearnGoal` kollidiert mit E13 des DB-Umbaus.
