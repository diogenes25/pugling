---
tags: [typ/story, status/idee, bereich/gamification, rolle/student]
aliases: [Bonus-Versprechen bei Deckel null]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: beides
migration: offen
vertragsbruch: offen
quelle: "Review von B-845 im Nachtlauf 2026-11-23-c (docs/pm-sitzung-2026-11-23-c.md, frontend-reviewer 🟡)"
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-660]
wartet_auf: ""
---

# B-862 · Bonus-Training verspricht Münzen an drei weiteren Stellen, auch wenn der Deckel 0 ist oder erreicht

B-845 hat den Satz im leeren Üben berichtigt; dasselbe Versprechen steht an drei Stellen der Sohn-App, die einen
Bonus-Deckel von 0 bzw. einen erreichten Deckel nicht kennen:

- **Runden-Hinweis** `frontend/src/sohn/SohnBonus.tsx:329-335`: hängt nur an `run.runPercent`; der Server
  (`ExtraTrainingService.cs:284`, `RunPercent`) kennt den Deckel dort nicht. Der Sohn liest „Jede richtige Antwort bringt
  N 🪙“ und bekommt am Ende „Diesmal ohne Münzen“.
- **Startseiten-Teaser** `SohnBonus.tsx:68` (`BonusEinstieg`, `SohnHome.tsx:145`): „Extra-Münzen für extra Übung …“ auch
  bei Deckel 0; `TodayResponse` trägt keinen Deckel.
- **Listenzeilen** `SohnBonus.tsx:145`: der Münzsatz je Treffer steht unter der Karte „heute keine Münzen“.

Gelesen am Code (Review 2026-11-23), nicht nachgestellt. `entgangen_bei: B-660`, weil dessen Leerzustand das Versprechen
neu in den Weg gelegt hat.

## Verlauf

- **2026-11-23** — angelegt aus dem Review von B-845 (Nachtlauf 2026-11-23-c), bewusst nicht im Sprint 118.
