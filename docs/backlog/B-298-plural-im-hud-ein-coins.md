---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/student]
aliases: [Singular im HUD, "1 coins", Pluralformen i18next]
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
- **2026-10-27** — in Lauf F, Teil 1 ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)) als `Defekt` unter Freigabe 1
  ausformuliert, geschätzt (XS, nur Frontend) und gebaut. Rote Probe zuerst: sechs `it.each`-Fälle (je Sprache
  `count: 1` gegen `count: 2`) in `i18n.test.ts` → **5 failed** erwartet und gemessen (en `streak` ist vorher wie
  nachher „1-day streak“). Fix: `coins_one/_other`, `gems_one/_other`, `streak_one/_other` in `de.ts` und `en.ts`
  → **10/10**, `tsc` sauber. Mutanten: en `coins_one` gestrichen → **2 failed** (Parität + neuer Fall); en
  `streak_one` → „-days streak“ → **1 failed**.
- **2026-10-27** — Review (`frontend-reviewer`): 🟡 `sprache.spec.ts` sicherte `/\d+ coins/` – bei Saldo 1 heißt es
  jetzt „1 coin“, Kind 1 teilt den Saldo mit anderen Specs → `/^\d+ coins?$/` (**zählt**, Zähler 1). 🟢 ohne Zählung:
  Testkommentar nannte den falschen Rückfall (gemessen vom Prüfer: Deutsch zeigt den Schlüssel, Englisch fällt auf
  Deutsch) → korrigiert; en-`streak`-Fall ergänzt statt mit falscher Begründung weggelassen. 🟢 zurückgestellt: fällt
  `{ count }` an der Aufrufstelle `SohnApp.tsx:108/109` weg, bleibt alles grün (Label „hud.gems“) – keine Story,
  der Literal-/Aufrufstellen-Wächter ist Vorschlag (M) bzw. Teil von B-295. Vitest **473/473**, E2E **98 passed**
  (`exit=0`). Rollengang: der Sohn-Pfad läuft als `sprache.spec.ts` (Englisch, HUD-Label), Vater/Creator als
  Regressionszeugen (volle E2E grün, kein Pfad von ihnen im Diff). **`abgenommen`.**
