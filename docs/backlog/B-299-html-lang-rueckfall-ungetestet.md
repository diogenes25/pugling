---
tags: [typ/story, status/idee, bereich/frontend, bereich/test, rolle/student]
aliases: [html lang Rückfall ungetestet, AK 4 zweite Hälfte]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: Nachschau zu Lauf E Teil 8 (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-294]
wartet_auf: ""
---

# B-299 · Der Rückfall von `<html lang>` auf Deutsch beim Verlassen der Arcade existiert nur als Code

AK 4 von [B-294](B-294-i18n-infrastruktur-und-startbildschirm.md) hat zwei Hälften: `<html lang>` wechselt mit
der Sprache **und** fällt beim Verlassen der Arcade auf Deutsch zurück. `sprache.spec.ts` prüft nur die erste.
Mutante (in der Nachschau gemessen): die Aufräumzeile `document.documentElement.lang = "de"` im Effekt von
`SohnApp.tsx` streichen → Build, Vitest **467** und die volle E2E bleiben grün. Nutzerpfad, der dann falsch ist:
Kind auf Englisch meldet sich ab → `SohnLogin` ist weiter deutsch (B-295), aber `lang="en"` bleibt stehen – genau
das Szenario, das Entscheidung 5 aus B-85 verhindern soll.

**Behebung:** in `sprache.spec.ts` nach dem Umschalten auf Englisch abmelden (oder `page.goto("/")`) und
`toHaveAttribute("lang", "de")` zusichern, bevor zurückgeschaltet wird; die Mutante oben muss dann **1 failed** geben.

## Verlauf

- **2026-10-27** — angelegt aus der Nachschau zu Lauf E Teil 8 (`1ee9bf0`) ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)),
  Fund 2 des frischen Prüfers; `entgangen_bei: B-294`.
- **2026-10-27** — Commit `aab2ef9` (Lauf E, Teil 9; nicht gepusht).
