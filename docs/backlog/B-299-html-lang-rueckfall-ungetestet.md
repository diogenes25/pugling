---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/test, rolle/student]
aliases: [html lang Rückfall ungetestet, AK 4 zweite Hälfte]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
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
- **2026-10-27** — in Lauf F, Teil 1 ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)) als `Defekt` unter Freigabe 1
  ausformuliert, geschätzt (XS, nur Test) und gebaut: `sprache.spec.ts` meldet nach dem Neuladen über **Skins →
  Abmelden** ab (kein `page.goto` – das lädt `index.html` mit statischem `lang="de"` und verdeckte die Mutante) und
  sichert `lang="de"`; danach Wieder-Anmelden → Arcade wieder englisch, `lang="en"`. Grün: **1 passed**. Mutante
  „Aufräumzeile `document.documentElement.lang = "de"` gestrichen“ → **1 failed** an der neuen Zusicherung
  (erwartet `de`, gemessen `en`, Zeile 42). Review (`frontend-reviewer`): kein Fund an B-299; 🟢 Kommentar, dass
  „Abmelden“ bis B-295 ein deutsches Literal ist → ergänzt. Volle E2E **98 passed** (`exit=0`). Rollengang: die Spec
  ist der Sohn-Gang; Vater/Creator Regressionszeugen. **`abgenommen`.**
