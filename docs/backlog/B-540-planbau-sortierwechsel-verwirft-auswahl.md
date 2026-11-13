---
tags: [typ/story, status/idee, bereich/frontend, rolle/supervisor]
aliases: [Planbau verwirft Auswahl beim Umsortieren]
status: idee
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: "B-408, Nebenbefund beim Schätzen (Backlog-Lauf 2026-11-13, docs/pm-sitzung-2026-11-13-backlog-lauf.md)"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: "B-534 (die parallele Sitzung baut PlanPositions.tsx um; nicht committet)"
---

# B-540 · Im Planbau verwirft ein Sortierwechsel die gewählte Übung

Im Planbau (`frontend/src/vater/PlanPositions.tsx`) steckt „beste zuerst“ im Auswahl-Schlüssel `filterKey`
(`PlanPositions.tsx:480`). Wer eine Übung gewählt hat und dann nur die Sortierung umstellt, verliert die Auswahl,
obwohl die Treffermenge dieselbe bleibt. Abgetrennt aus [B-408](B-408-assistent-zeigt-keine-bewertung.md): dort
entsteht die reine Funktion `auswahlNachUmsortieren` in `wizardSearch.ts` für den Assistenten; diese Story nimmt sie
für den Zwilling im Planbau (Angriffsplan-Schritt 3 von B-408).

Abgetrennt, weil eine parallele Sitzung `PlanPositions.tsx` gerade für B-534 umbaut und ihre Arbeit nicht committet
ist – ein Eingriff jetzt erzeugte einen Merge-Konflikt in fremder Arbeit.

## Akzeptanzkriterien (Entwurf aus B-408)

1. Der Auswahl-Schlüssel im Planbau enthält die Sortierung nicht mehr.
2. Nach dem Laden der umsortierten Seite bleibt die gewählte Übung, wenn sie noch auf der Seite steht
   (`auswahlNachUmsortieren`); sonst wird geleert, mit Hinweis.

## Verlauf

- **2026-11-13** — angelegt im Backlog-Lauf 2026-11-13 aus B-408 (Nebenbefund), `wartet_auf` B-534.
