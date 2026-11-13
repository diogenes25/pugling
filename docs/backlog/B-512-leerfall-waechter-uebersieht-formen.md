---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [leerfall.test.ts Formen, Wächter umgedreht]
status: abgenommen
prio: P3
art: Defekt
groesse: "XS"
wo: "frontend"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-tageslauf.md (Nachschau Sprint 49 in der Retro von Sprint 50/51)
unverifiziert: false
entgangen_bei: [B-505]
grund: ""
ersetzt_durch: []
nachgeschaut: 2026-11-13
---

# B-512 · Der Leerfall-Wächter übersieht gängige Formen der Seitenfrage

Nachschau von Sprint 49 (frischer `frontend-reviewer` an `8a5f20a`, gelesen, Vitest 45/45). Ein 🟡, zwei 🟢.

1. 🟡 `frontend/src/leerfall.test.ts` verspricht „rot, wenn eine geblätterte Liste ihren Leerfall wieder an die Länge der
   Seite hängt", kennt aber nur fünf Formen. Per `node` gegen die echte Regex: die Wahrheitswert-Ternäre
   (`x.data?.items.length ? <table/> : <p>Noch keine …</p>`), `> 0`, `== 0`, `items?.length`, `data!.items` liefern 0
   Treffer. Der Selbsttest prüfte nur, was das Muster schon kannte.
2. 🟢 B-505 AK 3, zweite Hälfte („`total = 0` zeigt es"): eine Liste, die ihren Leerfall ganz verliert, blieb grün.
3. 🟢 Die Mindestzahl „≥ 8 Quellen" nagelt den heutigen Stand fest.

## Entscheidungen

Autonom gegrillt (`art: Defekt`, Tageslauf):

1. **Muster umdrehen:** jede Länge einer geladenen Seite (`x.data?.items.length`, mit `!`, ohne `?`, `items?.length`) in
   einer Datei mit `<Pager` ist ein Fund; erlaubt ist nur `TruncationHint shown=` (zählt die gezeigten Zeilen). Die
   Zwischenvariable bleibt als benannte Lücke.
2. Jede Datei mit `<Pager` trägt mindestens einen Leerfall am Bestand (`total === 0`, auch `?? 0)`).
3. Mindestzahl auf „> 0"; die Schärfe trägt der Selbsttest (sieben Formen, die erlaubte Stelle, beide Fehlerarten).

## Akzeptanzkriterien

1. Die Ternäre in einer geblätterten Datei macht den Wächter rot.
2. Eine geblätterte Datei ohne `total === 0` macht ihn rot.

## Verlauf

- **2026-11-13** — angelegt (Nachschau Sprint 49, Retro Sprint 50/51), autonom gegrillt, geschätzt XS; in Sprint 52 gebaut.
- **2026-11-13** — gebaut (Sprint 52). AK 1 → `leerfall.test.ts` (T5: Ternäre in `VaterKonto` → rot); AK 2 → T6 (Leerfall am
  Bestand entfernt → rot); Selbsttest über sieben Formen. Review: keine Fehlalarme im heutigen Code; offene Lücken benannt
  (Zwischenvariable, `total === 0` je Datei statt je Liste, Pager in Kind-Komponente).
  - Verifikation: Vitest 816/816, `tsc -b` sauber; Mutanten T1–T7 7/7 rot nach der letzten Änderung (Tabelle im
    Protokoll, Sprint 52); Playwright 140/140 auf 5201/5174 (vor dem Fokus-Nachzug T7, der einen Komponententest trägt).
  - Rollengang Vater/Sohn: E2E-Suite gegen echten Server und UI; Creator Regressionszeuge. Abgenommen.
- **2026-11-13** — nachgeschaut (Retro Sprint 54/55, frischer `frontend-reviewer` an `59fc0a8`, gelesen, Vitest 68/68).
  Geprüft: Rücklesen symmetrisch zu Aufbau und Edit-Modal; Fokus nur nach „Jetzt abfragen", nie beim ersten Laden;
  Tooltip während des Ladens und im Ausfall; Wächter über alle `items.length` und `Paged<`-Dateien. Nur 🟢 →
  [B-515](B-515-nachschau-reste-sprint-52.md).
