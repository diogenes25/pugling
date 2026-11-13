---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/student]
aliases: [Nachschau-Reste Sprint 52]
status: abgenommen
prio: P3
art: Defekt
groesse: "S"
wo: "frontend"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-tageslauf.md (Nachschau Sprint 52 in der Retro von Sprint 54/55)
unverifiziert: false
entgangen_bei: [B-443, B-456]
grund: ""
ersetzt_durch: []
nachgeschaut: 2026-11-13
---

# B-515 · Nachschau-Reste Sprint 52: Fokus bei leerer Runde, eine Zahl, die beim Blättern verschwindet

Nachschau von Sprint 52 (frischer `frontend-reviewer` an `59fc0a8`, gelesen, Vitest 68/68). Kein 🔴/🟡, drei 🟢:

1. `VaterExercises.tsx:191`: `gemesseneZahl` blendet die Zahl auch beim Blättern/Sortieren aus (`skip`/`sort`/`dir` stehen
   in den Abhängigkeiten); nach einer gescheiterten Seitenabfrage bleibt sie weg, während der `Pager` `total` zeigt.
   B-456 verlangte nur „gehört zu den Filtern".
2. `SohnPractice.tsx:163`/`:287`: liefert die Runde nach „Jetzt abfragen" keine Karten (`empty`) oder scheitert (`error`),
   fällt der Fokus auf `body`; `focusAfterLoad` bleibt danach `true` stehen.
3. `SohnPractice.test.tsx:375`: der Fokus-Test prüft nur `INPUT|BUTTON`, nicht das Antwortfeld der neuen Karte.

## Verlauf

- **2026-11-13** — angelegt (Nachschau Sprint 52, Retro Sprint 54/55).
- **2026-11-13** — gebaut (Sprint 57), autonom gegrillt (Defekt): (1) die Zahl der Überschrift hängt an den Filtern
  (`zahlZumFilter`, Z2 → rot); (2) leere oder gescheiterte Runde nach „Jetzt abfragen" fokussiert Überschrift bzw.
  Meldung (`role="alert"`), die Absicht verfällt mit jedem Ende des Ladens (Z1 → rot); (3) Fokus-Test prüft das
  Antwortfeld selbst.
  - Verifikation: `dotnet test Pugling.sln -c Release` 1439/1439, Vitest 825/825, `tsc -b` sauber, Playwright 140/140
    auf 5201/5174; Mutanten X1–X4, Y1, Y2, Z1, Z2 8/8 rot nach der letzten Änderung.
  - Rollengang Vater: E2E-Suite (Planbau, Übungsliste) gegen echten Server und UI; Sohn (B-515): Komponententests,
    E2E-Suite grün; Creator Regressionszeuge. Abgenommen.
- **2026-11-13** — nachgeschaut (Retro Sprint 59/60, frischer Reviewer, gelesen; Filter-Wege, Render-Phasen-Zustand bzw. NEU-Erkennung gegen die echten Unit-Notizen nachgerechnet). Befund: 🟡 Fokus nach Fehler ohne Test → [B-518](B-518-nachschau-reste-sprint-57.md).
