---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/student]
aliases: [k.title ?? k.scope, Datum folgt Browser-Locale]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf G Teil 1 in Lauf G, Teil 2 (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
nachgeschaut: "2026-10-28"
---

# B-317 · Eine Ziel-Etappe ohne Titel zeigt dem Kind den rohen Enum-Wert

`MyObjectives.tsx` zeigt `k.title ?? k.scope` – in der Etappenliste und in der Feier. Ohne Titel liest das Kind
`Exercise`, `SeriesUnit` oder `Subject`, in beiden Sprachen. Nebenbei (selbe Nachschau): Daten im Shop-Verlauf und in
den Anfragen (`toLocaleDateString()`) folgen der Browser-Locale, nicht der Arcade-Sprache.

`unverifiziert`: aus dem Code gelesen, nicht im Bild gesehen. Schon vor B-304 so; außerhalb dessen Umfang notiert.

## User Story

Als Kind möchte ich bei einer Etappe ohne Titel lesen, was sie misst, und Daten in meiner Arcade-Sprache – keinen
Programmwert wie „SeriesUnit“.

## Ist-Stand am Code

`frontend/src/sohn/MyObjectives.tsx:43,87` (vor dem Fix): `k.title ?? k.scope`; `SohnShop.tsx:323,384`:
`toLocaleDateString()` ohne Sprache.

## Akzeptanzkriterien

1. Eine Etappe ohne Titel zeigt je Bereich (Übung, Unit, Fach) einen Text – in der Liste und in der Feier, in beiden
   Sprachen.
2. Die Daten in Shop-Verlauf und Anfragen folgen der Arcade-Sprache.

## Entscheidungen

1. Ein Text je `KeyResultScope` („Etappe in einer Unit“ …) statt eines generischen „Etappe“ – mehr gibt die Antwort
   ohne Namen der Ziele nicht her. Kosten: drei Schlüssel je Sprache.
2. Das Datum über `useShopText().date` (`toLocaleDateString(i18n.language)`). Kosten: keine.

## Schätzung

XS, `frontend`, keine Migration, kein Vertragsbruch.

## Verlauf

- **2026-10-27** — angelegt aus der Nachschau zu Lauf G, Teil 1 (Randbefunde, außerhalb des Umfangs).
- **2026-10-28** — in Lauf H, Teil 2 ([Protokoll](../pm-sitzung-2026-10-28-nacht.md)) ausformuliert, geschätzt (XS) und gebaut. **Gemessen** gegen den
  Ausgangsstand: Liste und Feier → je **1 failed**; Datum englisch → **1 failed**, Datum deutsch **grün** – die lokale
  Test-Locale ist Deutsch (gemessen), die alte Fassung ohne Sprache fängt je Umgebung nur einer der beiden Fälle; darum
  je Fall die Mutante „fest in der anderen Sprache“ → je **1 failed**. 🟢 aus dem Review genommen: zweiter Bereich
  (`Exercise`) im Listenfall, Mutante „fester Schlüssel“ → **1 failed**; das Datum in `RequestsTab` bleibt ohne eigenen
  Fall (derselbe Helfer), ohne Zählung. Vitest **555/555**. Rollengang: die volle E2E-Suite gegen frisch gestartete Server (**99 passed**, `exit=0`, nach der letzten Änderung gestartet); Browser-Extension nicht genutzt. **`abgenommen`.**
- **2026-10-28** — Commit `f96d358` (Lauf H, Teil 2).
- **2026-10-28** — nachgeschaut (Lauf I, frische Reviewer am Commit `f96d358`): drei Scope-Schlüssel gegen `KeyResultScope` in `docs/openapi/v1.json` vollständig; en-Gleichstand über `i18n.test.ts:16`; Datumsfälle ohne Zeitzonenkante; kein weiteres `toLocale*String` in `sohn/`. Kein Befund.
