---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [Nachschau-Reste Sprint 57, Ziel-Wähler ganze Reihe]
status: abgenommen
prio: P3
art: Defekt
groesse: "S"
wo: "frontend"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-tageslauf.md (Nachschau Sprint 57 in der Retro von Sprint 59/60)
unverifiziert: false
entgangen_bei: [B-513, B-515]
grund: ""
ersetzt_durch: []
nachgeschaut: 2026-11-13
---

# B-518 · Nachschau-Reste Sprint 57: „ganze Reihe" im Ziel-Wähler, zwei Verdrahtungen ohne Test

Nachschau von Sprint 57 (frischer `frontend-reviewer` an `9553854`, gelesen, Vitest 31/31). Drei 🟡, zwei 🟢.

1. 🟡 `VaterZiele.tsx`: die leere Unit hieß „ganze Reihe", die Übungsliste zeigt seit B-513 nur die Übungen der Reihe –
   gespeichert wird aber das ganze Fach (`scopeToDto` kennt keine Reihe). Die Oberfläche bestätigte einen Bereich, den
   der Server nicht kennt.
2. 🟡 `PlanPositions.tsx`: `filter.seriesId` in den Abhängigkeiten der Suche hatte keinen Test – fehlte es, wechselte die
   Reihe, ohne dass die Abfrage neu liefe.
3. 🟡 `SohnPractice.tsx`: Fokus auf die Meldung nach einer gescheiterten neuen Runde ohne Test.
4. 🟢 `VaterZiele` importiert `reiheFilter` aus einer Seite; 🟢 `role="alert"` + Fokus könnte doppelt angesagt werden.

## Entscheidungen

Autonom gegrillt (`art: Defekt`, Tageslauf):

1. **Die leere Wahl sagt, was gespeichert wird** („keine – das Ziel zählt das ganze Fach", „alle Übungen des Fachs" bzw.
   „… der Unit"). Ein Reihen-Scope am Ziel (API + Migration) ist ein eigener Wunsch, nicht dieser Defekt.
2. `planbauKriterien` als Funktion mit einem erschöpfenden Test über alle Felder von `ExerciseFilter` (ein neues Feld
   ohne Testeintrag macht schon den Typprüfer rot).
3. Fokus-Test für den Fehlerfall. Die 🟢 bleiben offen (Geräte-Check für die Ansage).

## Akzeptanzkriterien

1. Die leere Unit verspricht keinen Reihen-Bereich.
2. Fehlt ein Filterfeld in den Kriterien, ist ein Test rot.
3. Scheitert die neue Runde, steht der Fokus auf der Meldung.

## Verlauf

- **2026-11-13** — angelegt (Nachschau Sprint 57, Retro Sprint 59/60), autonom gegrillt, geschätzt S; in Sprint 61 gebaut.
- **2026-11-13** — gebaut (Sprint 61). AK 1 → `VaterZiele.test.ts` (Z1, R3 → rot; nach Review auch der Fall „Übung gewählt");
  AK 2 → `planbauKriterien`-Test (K1 → rot; nach Review auch die Normalisierung, R4 → rot); AK 3 → Fokus-Test (F1 → rot).
  - Verifikation: `dotnet test Pugling.sln -c Release` 1462/1462, Vitest 854/854, `tsc -b` sauber, Python-unittest grün,
    Playwright 140/140 auf 5201/5174 (vor den letzten Beschriftungs- und U+FEFF-Nachzügen, die Komponententests tragen);
    Mutanten nach der letzten Änderung rot (Tabellen im Protokoll, Sprints 60–62).
  - Rollengang: Vater (Plan-Seite, Assistent, Ziel-Wähler) per E2E und Komponententests; Creator (Konsole) gegen den
    In-Process-Server; Sohn Regressionszeuge. Abgenommen.
- **2026-11-13** — nachgeschaut (Sprint 63, frische `pugling-reviewer`/`frontend-reviewer` an `e37f65e`, gelesen, Vitest 102/102). Geprüft: CI-Schritt läuft (python3, Arbeitsverzeichnis, Pfade), xUnit-Leser findet die Liste, alle drei Leser stimmen Schritt für Schritt überein, `Map`-Aufrufer laden die Übung. Kein 🔴; Reste → [B-521](B-521-nachschau-reste-sprint-60-62.md).
