---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/test]
aliases: [liveRegions Regex zwei Blockkommentare]
status: abgenommen
prio: P4
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf P, Teil 2 in Lauf Q, Teil 1 (docs/pm-sitzung-2026-11-03-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-364 · Restkanten der Nachschau über `a25a1a6`: zwei Blockkommentare, ⓘ-Schleife der Tablet-Spec

Zwei 🟢 der Nachschau über Lauf P, Teil 2, keiner eine Entgleitung:

1. **`liveRegions.test.ts:79`** – kein Fall mit zwei Blockkommentaren hintereinander; die gierige Mutante `[\s\S]*` statt
   `[\s\S]*?` verschluckte `/* a */ x && /* b */` unbemerkt. Weg: ein Selbsttest-Fall mit zwei Blockkommentaren.
2. **`e2e/responsive-vater.spec.ts:69-77`** – seit der Anker-Prüfung je Frame (B-273) schließt das ⓘ bei *jeder* Bewegung des
   Knopfs, auch durch spät nachladenden Inhalt nach `networkidle`; `popoverFit` riefe dann `querySelector` auf ein fehlendes
   Element. Ungemessen, geringes Risiko.

## User Story

Als **Entwickler** möchte ich, dass jede Form der Kommentar-Entfernung eine Probe hat, damit ein Umbau der Regex auffällt.

## Ist-Stand am Code (Stand `a25a1a6`)

- `frontend/src/liveRegions.test.ts:79` entfernt `/* … */` nicht-gierig; kein Fall mit zwei Blockkommentaren.
- `frontend/e2e/responsive-vater.spec.ts:69-77` öffnet jedes ⓘ einer Route nacheinander.

## Die echte Lücke

Punkt 1 wie beschrieben; Punkt 2 ist eine Vermutung ohne Messung.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Ein Selbsttest-Fall mit zwei Blockkommentaren** um das `&&`.
2. **Die ⓘ-Schleife der Tablet-Spec bleibt**, bis sie einmal flackert (ungemessen, geringes Risiko) – dieselbe Abwägung wie
   bei der 10-s-Grenze in B-357. *Kosten:* fällt sie, meldet sie `querySelector` auf `null` statt der Ursache.

## Akzeptanzkriterien

1. Die gierige Mutante der Kommentar-Entfernung macht einen Fall rot.

## Schätzung

`XS`, frontend (Test). Testweg: `liveRegions.test.ts`.

## Verlauf

- **2026-11-03** — angelegt aus der Nachschau zu Lauf P, Teil 2 (`frontend-reviewer`, 🟢 ×2). Aus Code gelesen.
- **2026-11-03** — Lauf Q, Teil 2: autonom gegrillt (Freigabe 1), gebaut. Neuer Fall grün; Mutante „gierig" (`[\s\S]*`)
  **1 failed** an „&& zwischen zwei Blockkommentaren". Vitest **621/621**.
- **2026-11-03** — Review `frontend-reviewer` und `pugling-reviewer`: kein 🔴; ein 🟡 (InfoHint noch nicht gestaged – AK 1 von B-362 gilt erst nach dem Commit, dort per `git ls-files --eol` geprüft, siehe Protokoll; kein Code geändert, ohne Zählung); drei 🟢 (Umbruch, zwei Kommentar-Formulierungen) ohne Zählung behoben. Abnahme Lauf Q, Teil 2: volle Suite **1059/1059**, Vitest **621/621**, E2E-Gesamtlauf **107 passed** (`exit=0`, nach dem letzten Edit). Rollengang: der Entwickler ist die Rolle (die Probe); Creator, Vater, Sohn Regressionszeugen. **Abgenommen**, Commit siehe Protokoll ([Lauf Q](../pm-sitzung-2026-11-03-nacht.md)).
