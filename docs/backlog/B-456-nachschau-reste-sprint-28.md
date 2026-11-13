---
tags: [typ/story, status/abgenommen, bereich/vater-web, rolle/creator]
aliases: [Nachschau-Reste Sprint 28]
status: abgenommen
prio: P3
art: Defekt
groesse: "S"
wo: frontend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-10-fertigstellung.md
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: 2026-11-13
---

# B-456 · „Meine Übungen (0)" neben „Lade…", ein ungetesteter Zweig und ein falscher Tooltip im Manifest-Ausfall

Drei Reste aus der Nachschau zu Sprint 28, keiner ein Defekt im Verhalten:
die Überschrift in `frontend/src/vater/VaterExercises.tsx:182` zeigt `(0)` während des ersten Ladens und im Ausfall;
der Zweig „ohne Fach + Schalter → Alle Übungen" (`:181`, B-431 Entscheidung 2) hat keine Zusicherung; und
`frontend/src/vater/VaterFachlehrer.tsx:441-445` sagt bei ausgefallenem Manifest „Der Server kennt diesen Typ nicht
mehr." über einen gültigen Typ. Daneben Altbestand: der Filter „Reihe" ohne Unit schränkt die Übungsliste nicht ein
(`VaterExercises.tsx:103-108`, `seriesId` geht nicht in die Abfrage).

## Entscheidungen

Autonom gegrillt (`art: Defekt`, Tageslauf):

1. Die Zahl der Überschrift erscheint nur, wenn sie gemessen ist und zu den Filtern gehört (`gemesseneZahl`: Daten da,
   nicht `stale`) – kein „(0)" neben „Lade…" oder im Ausfall.
2. Im Manifest-Ausfall (leere Typliste) sagt der Tooltip nichts über einen gespeicherten Typ; mit Manifest bleibt „kennt
   diesen Typ nicht mehr" für einen wirklich unbekannten.
3. Der Zweig „ohne Fach + Schalter → Alle Übungen" bleibt ohne Zusicherung (🟢, ein Ausdruck).
4. Der Altbestand „Reihe ohne Unit filtert nicht" braucht einen Suchparameter im Backend → abgeteilt als
   [B-513](B-513-reihe-filter-der-uebungsliste-filtert-nicht.md).

## Akzeptanzkriterien

1. `gemesseneZahl` liefert ohne Daten und bei `stale` keine Zahl.
2. Ohne Manifest trägt der Typ-Knopf keinen Tooltip „nicht mehr".

## Verlauf

- **2026-11-10** — angelegt aus der Nachschau zu Sprint 28 (Retro Sprint 29).
- **2026-11-13** — ausformuliert, autonom gegrillt (Defekt), geschätzt S (Tageslauf, Sprint 52).
- **2026-11-13** — gebaut (Sprint 52). AK 1 → `VaterExercises.test.ts` „nennt keine Zahl ohne Daten …" (T3 → rot); AK 2 →
  `VaterFachlehrer.test.tsx` „behauptet ohne Manifest nicht …" (T4 → rot) und die Gegenprobe mit Manifest. Die Verdrahtung
  an der Überschrift trägt kein Test (Vorbild `artenPending`, vom Review so bestätigt).
  - Verifikation: Vitest 816/816, `tsc -b` sauber; Mutanten T1–T7 7/7 rot nach der letzten Änderung (Tabelle im
    Protokoll, Sprint 52); Playwright 140/140 auf 5201/5174 (vor dem Fokus-Nachzug T7, der einen Komponententest trägt).
  - Rollengang Vater/Sohn: E2E-Suite gegen echten Server und UI; Creator Regressionszeuge. Abgenommen.
- **2026-11-13** — nachgeschaut (Retro Sprint 54/55, frischer `frontend-reviewer` an `59fc0a8`, gelesen, Vitest 68/68).
  Geprüft: Rücklesen symmetrisch zu Aufbau und Edit-Modal; Fokus nur nach „Jetzt abfragen", nie beim ersten Laden;
  Tooltip während des Ladens und im Ausfall; Wächter über alle `items.length` und `Paged<`-Dateien. Nur 🟢 →
  [B-515](B-515-nachschau-reste-sprint-52.md).
