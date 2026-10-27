---
tags: [typ/story, status/idee, bereich/frontend, rolle/student]
aliases: [Singular im HUD, "1 coins", Pluralformen i18next]
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

# B-298 · HUD liest „1 coins“ / „1 Münzen“ vor – die Schlüssel kennen keinen Singular

`hud.coins`, `hud.gems` und `hud.streak` in `frontend/src/i18n/de.ts`/`en.ts` werden mit `{ count }` aufgerufen
(`SohnApp.tsx`), tragen aber nur eine Form. i18next löst Pluralformen über `<key>_one`/`<key>_other`; ohne sie fällt
es auf den Basistext zurück. Gemessen in der Nachschau: `en: "1 coins"`, `de: "1 Münzen"`, `"1 Tage Streak"`. Im
Deutschen war das vorher schon so (`${coins} Münzen`), im Englischen ist es neuer, falscher Text – und die Chips
sind `aria-live`, der Screenreader liest ihn vor.

**Behebung:** `coins_one: "{{count}} coin"`, `coins_other: "{{count}} coins"` (analog `gems`; deutsch
`Münze/Münzen`, `Tag/Tage`); der Paritäts-Test sieht `_one/_other` als normale Schlüssel und bleibt tragfähig.
Ein Vitest je Sprache mit `count: 1` und `count: 2`; Mutante „`_one` gestrichen“ → rot.

## Verlauf

- **2026-10-27** — angelegt aus der Nachschau zu Lauf E Teil 8 (`1ee9bf0`) ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)),
  Fund 1 des frischen Prüfers; `entgangen_bei: B-294`.
- **2026-10-27** — Commit `aab2ef9` (Lauf E, Teil 9; nicht gepusht).
