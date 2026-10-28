---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/student]
aliases: [useErrorMessage englischer Zweig ungedeckt, ROUND DONE Zweig läuft nie]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf G Teil 2 in Lauf H, Teil 1 (docs/pm-sitzung-2026-10-28-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-305]
wartet_auf: ""
---

# B-319 · Zwei Zusicherungen aus B-305 können nicht fallen

Die Nachschau zu [B-305](B-305-arcade-spielweg-auf-englisch.md) (Commit `43b5eef`) fand zwei Stellen, die nach einem
Beleg aussehen und keinen tragen:

1. **`useErrorMessage`, englischer Zweig für Codes außerhalb von `PROBLEM_KEYS`** (`frontend/src/lib/chromeText.tsx:37-38`).
   Der englische Fall in `arcadeSpielweg.test.tsx` (~Zeile 143) prüft nur `exhausted` (läuft über den `problem.*`-Zweig
   davor) und einen unbekannten Code, für den `errorMessage` byte-gleich dasselbe liefert. Mutante „Zeilen 37-39 →
   `return errorMessage(e)`“ (Englisch bekäme für `duplicate_textbook_series` u. a. den deutschen Satz) bliebe grün; der
   Kommentar „Rot, wenn die Sprachweiche fehlt“ stimmt für diesen Fall nicht. Dasselbe gilt für den Zeitpunkt-Fall
   (~Zeile 151): die Sprache wird nur im `i18n.t`-Zweig geprüft.
2. **`sprache.spec.ts:122`** erwartet nach der ersten Karte „Card 2“ **oder** „ROUND DONE!“. Die Seed-Übung hat mehrere
   Einträge, der zweite Zweig läuft nie; Mutante `practice.roundDone` zurück auf „RUNDE FERTIG!“ bliebe grün – genau das
   Muster, das B-305 beim Zweig „Next →“ selbst gestrichen hat.

Vorschlag des Prüfers: im englischen Fall zusätzlich einen bekannten Nicht-Arcade-Code erwarten
(`duplicate_textbook_series` → „Duplicate series. (Ref: …)“) und den Kommentar richtig zuordnen; in der Spec den Zweig
streichen und fest „Card 2“ erwarten.

`unverifiziert`: aus dem Code hergeleitet, nicht gemessen (der Prüfer durfte keine Tests fahren).

## User Story

Als Entwickler möchte ich, dass die Zusicherungen aus B-305 fallen, wenn das passiert, was sie zu prüfen behaupten.

## Ist-Stand am Code

Siehe oben (`chromeText.tsx:37-38`, `arcadeSpielweg.test.tsx` ~143/151, `sprache.spec.ts:122`, Stand vor dem Fix).

## Akzeptanzkriterien

1. Der englische Fall und der Zeitpunkt-Fall fallen, wenn die Weiche für Codes außerhalb von `problem.*` fehlt bzw. die
   Sprache beim Render festhält.
2. `sprache.spec.ts` erwartet nach der ersten Karte fest „Card 2“.

## Entscheidungen

1. Ein bekannter Nicht-Arcade-Code (`duplicate_textbook_series`) als gemeinsamer Fall der Beschreibung; der Zweig
   „ROUND DONE!“ wird gestrichen, nicht belegt (das Rundenende braucht eine Position mit genau einem fälligen Eintrag –
   eigener Aufwand, hier nicht verlangt). Kosten: `practice.roundDone` bleibt ohne Beleg.

## Schätzung

XS, `frontend` (Tests), keine Migration, kein Vertragsbruch.

## Verlauf

- **2026-10-28** — angelegt aus der Nachschau zu B-305 in Lauf H, Teil 1 ([Protokoll](../pm-sitzung-2026-10-28-nacht.md)),
  Funde GELB 1 und GELB 2 des frischen `frontend-reviewer`.
- **2026-10-28** — in Lauf H, Teil 2 ([Protokoll](../pm-sitzung-2026-10-28-nacht.md)) gebaut. **Gemessen:** Mutante des Prüfers „nach dem Schlüssel immer
  `errorMessage`“ → **2 failed** (englischer und Zeitpunkt-Fall, je an der neuen `known`-Zeile 152/162); Mutante
  „Sprache beim Render festhalten“ → **1 failed** (Zeile 162); vorher blieben beide grün. Kontrolle **10 passed**.
  `sprache.spec.ts` ohne `.or(…)`; der Seed hat 12 Einträge, „Card 2“ ist sicher (vom Reviewer nachgezählt). Vitest
  **555/555**. Rollengang: die volle E2E-Suite gegen frisch gestartete Server (**99 passed**, `exit=0`, nach der letzten Änderung gestartet); Browser-Extension nicht genutzt. **`abgenommen`.**
