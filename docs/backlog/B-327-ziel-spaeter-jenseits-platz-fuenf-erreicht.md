---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/student]
aliases: [B-257 nur für den ersten Besuch, spätere Besuche laden take 5]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf I, Teil 1 in Lauf I, Teil 2 (docs/pm-sitzung-2026-10-28-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-257]
wartet_auf: ""
---

# B-327 · Ein Ziel, das nach dem ersten Besuch jenseits von Platz fünf erreicht wird, feiert die App verspätet

[B-257](B-257-zielfeier-verspaetet-jenseits-der-ersten-fuenf.md) lud nur beim **ersten** Besuch alle aktiven Ziele, danach
wieder `take: 5`. Ein Ziel (oder eine Etappe), das zwischen zwei späteren Besuchen auf Platz 6+ erreicht wird, kam darum
nie in die Gesehen-Menge – rutscht es nach vorn (der Vater deaktiviert frühere Ziele), feiert die App „🏆 ZIEL
GESCHAFFT! +N 🪙“ für einen alten Erfolg. Das verletzt die User Story von B-257; dessen Entscheidung 1 nahm an, der Fall
zähle nur beim ersten Besuch.

## User Story

Als Kind möchte ich nur für einen Erfolg gefeiert werden, den ich gerade geschafft habe – auch wenn das Ziel gerade
nicht unter den ersten fünf stand.

## Ist-Stand am Code

`frontend/src/sohn/MyObjectives.tsx` (Stand `c1a4e3f`): `take: firstVisit ? 500 : PAGE`. **Gemessen**: Besuche
[1 offen] → [1–6 offen] → [1–5 offen, 6 erreicht] → [6 erreicht] → in Besuch 4 `celebrate` **2-mal** (erwartet: keinmal).

## Akzeptanzkriterien

1. Ein auf Platz 6+ erreichtes Ziel wird nie nachträglich gefeiert, egal bei welchem Besuch es erreicht wurde.
2. Die Liste zeigt weiter höchstens fünf.

## Entscheidungen

1. Jeder Besuch lädt bis `MaxTake` (500), die Liste schneidet auf fünf; die Sonderregel „erster Besuch“ entfällt. Kosten:
   ein größerer Abruf bei jedem Öffnen – die aktiven Ziele eines Kindes sind wenige. Ersetzt B-257 Entscheidung 1.
2. Ein jenseits von Platz fünf **frisch** erreichtes Ziel wird beim nächsten Besuch gefeiert, obwohl es nicht in der
   Liste steht – das ist eine rechtzeitige Feier, keine verspätete. Kosten: keine.

## Schätzung

XS, `frontend`, keine Migration, kein Vertragsbruch.

## Verlauf

- **2026-10-28** — angelegt aus der Nachschau (frischer `frontend-reviewer`, `git show c1a4e3f`) in Lauf I, Teil 2
  ([Protokoll](../pm-sitzung-2026-10-28-nacht.md)); Defekt im Increment dieses Laufs → Limit 2. Im Sprint gebaut:
  rote Probe **1 failed** (2 Feiern), danach `MyObjectives.test.tsx` **15/15**. Mutanten: nur eine Seite → **3 failed**,
  Schlüssel ohne `v2` → **2 failed**. Der Fall „lädt ab dem zweiten Besuch nur die erste Seite“ aus B-257 ist ersetzt.
- **2026-10-28** — Delta-Review (`frontend-reviewer`, gemessen): `take: PAGE` → **3 failed**, alter Code → nur der B-327-Fall rot (trennt sich vom Erst-Besuch). 🟢 ohne Zählung genommen: Entscheidung 2 bekam eine Zusicherung (Mutante „nur gezeigte feiern“ → **1 failed**), Titel und Kommentare des Blocks berichtigt. Vitest **569/569**, `tsc -b` sauber, volle E2E **99 passed** (alle `exit=0`, E2E nach der letzten Änderung gestartet); Browser-Extension nicht genutzt. **`abgenommen`.**
