---
tags: [typ/story, status/idee, bereich/vater-web, rolle/creator]
aliases: [Nachschau-Reste Sprint 28]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-10-fertigstellung.md
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-456 · „Meine Übungen (0)" neben „Lade…", ein ungetesteter Zweig und ein falscher Tooltip im Manifest-Ausfall

Drei Reste aus der Nachschau zu Sprint 28, keiner ein Defekt im Verhalten:
die Überschrift in `frontend/src/vater/VaterExercises.tsx:182` zeigt `(0)` während des ersten Ladens und im Ausfall;
der Zweig „ohne Fach + Schalter → Alle Übungen" (`:181`, B-431 Entscheidung 2) hat keine Zusicherung; und
`frontend/src/vater/VaterFachlehrer.tsx:441-445` sagt bei ausgefallenem Manifest „Der Server kennt diesen Typ nicht
mehr." über einen gültigen Typ. Daneben Altbestand: der Filter „Reihe" ohne Unit schränkt die Übungsliste nicht ein
(`VaterExercises.tsx:103-108`, `seriesId` geht nicht in die Abfrage).

## Verlauf

- **2026-11-10** — angelegt aus der Nachschau zu Sprint 28 (Retro Sprint 29).
