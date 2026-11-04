---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/doku]
aliases: [Wizard-Kommentar nennt entfernten Effekt, Inventar B-369 zu klein]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf R, Teil 2 in Lauf S, Teil 1 (docs/pm-sitzung-2026-11-04-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-11-04"
entgangen_bei: [B-368, B-183]
wartet_auf: ""
---

# B-370 · Zwei Behauptungen aus Lauf R, Teil 2 halten nicht

Die Nachschau über `3ea3b1d` fand zwei Stellen, an denen Text mehr behauptet, als der Code hält:

1. **B-368 AK 2** („kein Kommentar nennt den entfernten Effekt") galt nur für `PlanPositions.tsx`. Der Zwilling
   `VaterWizard.tsx:721-724` sagte weiter, „der Effekt auf `[exercises.data]` läuft nie – der Knopf wäre wieder bedienbar";
   die Sperre hängt seit B-164 an `exercises.stale`.
2. **B-183** gab der abgeteilten B-369 und dem Wächter-Kommentar (`src/lesefehler.test.ts`) „21 Stellen" in Ternär-Form mit.
   Mehrzeilig gezählt sind es 22 (`PlanPositions.tsx:53`: das Banner steht eine Zeile unter `positions.error ? (`); dazu fehlten
   frühe Rückgaben mit Banner und sechs Arcade-Stellen. Wer B-369 nach dem Inventar baut, ließe nackte Lesefehler stehen.

## User Story

Als **Entwickler**, der B-369 baut oder den Wizard-Knopf ändert, möchte ich dem Kommentar und dem Inventar glauben können, damit
ich nicht an einem Effekt suche, den es nicht mehr gibt, oder Stellen übersehe.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Kommentar am Wizard** wie am Zwilling: die Sperre ist `exercises.stale`, weil die Deps der Daten nur bei Erfolg nachziehen.
2. **Inventar in B-369 und im Wächter-Kommentar ergänzen**, nicht den Wächter erweitern – das ist B-369s offene Gestaltungsfrage.

## Akzeptanzkriterien

1. Kein Kommentar in `src/` nennt einen Effekt auf `[exercises.data]` als Sperre.
2. B-369 nennt 22 Ternär-Stellen im Vater-Web, die frühen Rückgaben und die Arcade-Stellen; der Wächter-Kommentar nennt 22.

## Schätzung

`XS`, nur Kommentar und Story-Text. Testweg: `grep`; kein Verhalten ändert sich.

## Verlauf

- **2026-11-04** — angelegt aus der Nachschau zu Lauf R, Teil 2 (frischer `frontend-reviewer`, `git show 3ea3b1d`, read-only; zwei 🟡).
  Im selben Sprint behoben, ohne Zählung (nur Kommentar und Story-Text, Freigabe 3). Zahl selbst nachgerechnet: `x.error ? <banner
  err>` mehrzeilig über `frontend/src/**/*.tsx` – erwartet 22 im Vater-Web, gemessen 22 (dazu `VaterProfil.tsx:56` unter nacktem
  Namen und zwei Arcade-Stellen, zusammen 25). AK 1: `grep -rn "exercises.data\]" frontend/src` → 1 Treffer, die Dep-Liste des `merken`-Effekts (`VaterWizard.tsx:265` – Code, kein Kommentar).
- **2026-11-04** — Review `pugling-reviewer`: kein Fund. Nachgerechnet vom Prüfer: die neue Aussage am Wizard hält
  (`zeilenVeraltet = exercises.stale`, `useAsync.test.tsx:176-190` pinnt `stale` nach gescheiterter Abfrage); B-369 und der
  Wächter-Kommentar nennen 22, die Arcade-Liste 2 + 6 = 8.
- **2026-11-04** — Abnahme Lauf S, Teil 1: volle Suite **1068/1068**, Vitest **638/638**, `tsc -b` sauber, E2E-Gesamtlauf **109 passed** (`exit=0`, nach dem letzten Edit). Rollengang: der Entwickler ist die Rolle (Tor-Meldungen, Tor gegen Inline-Elemente im Listenpunkt); Creator über `ContractDocumentTests` und den Dokument-Diff (eine Zeile, Code- und Fettsatz da); Vater und Sohn Regressionszeugen (Suiten grün, nur Kommentare im Frontend). **Abgenommen**, Commit siehe Protokoll ([Lauf S](../pm-sitzung-2026-11-04-nacht.md)).
- **2026-11-04** — Nachschau in Lauf S, Teil 2 (frischer `pugling-reviewer`, `git show cc789ed`, read-only): kein 🔴/🟡, **keine Entgleitung**. Nachgerechnet: Ternär-Stellen im Vater-Web 22/22, mit `VaterProfil` und zwei Arcade-Stellen 25/25; Kommentar am Wizard gegen `useAsync.ts:69` (Deps nur bei Erfolg) und die drei Steuerelemente. 🟢 ohne Zählung behoben: der Wächter-Kommentar sagte „22 Stellen“ ohne „im Vater-Web“ (über ganz `src` sind es 26) – jetzt beides; in B-369 `MediaPickers` aus „frühe Rückgabe“ gelöst und die zwei Start-Fehler der Arcade (`SohnPractice.tsx:212`, `SohnTest.tsx:170`) als offene Zuordnung notiert.
