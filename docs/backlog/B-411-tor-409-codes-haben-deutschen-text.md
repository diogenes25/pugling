---
tags: [typ/story, status/idee, bereich/beides]
aliases: [Tor 409-Codes deutscher Text]
status: idee
prio: P2
art: Aufräumen
groesse: ""
wo: beides
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-05.md#lauf-z
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-411 · Tor: jeder 409-Code einer Creator-/Supervisor-Action hat einen deutschen Text oder einen begründeten Ausnahmeeintrag

Retro-Vorschlag (BV), entschieden am 2026-11-07 als Tor-Story. B-403 entstand, weil der neue Code `series_subject_in_use`
in `GERMAN_PROBLEM_TEXT` (`frontend/src/lib/api.ts:82`) fehlte. Das Vater-Web zeigte deshalb den englischen `detail`-Text, und
das sieht in der Oberfläche wie ein Defekt aus. Kein Tor verbindet `ApiErrors` mit der deutschen Tabelle.

**Gemessen am 2026-11-07** (Regex über `ApiErrors.cs` und den Tabellenblock): 33 Codes mit Status 409, davon 24 ohne deutschen
Eintrag, zum Beispiel `position_has_data`, `vocabulary_in_use`, `grammar_topic_in_use`, `last_owner`, `duplicate_key_result`
und `media_variant_exists`. Noch nicht unterschieden ist, welche davon eine Creator-/Supervisor-Action liefert. Manche treffen
nur die englische Sohn-Arcade (`skin_already_unlocked`, `shop_insufficient_stock`) oder sind technisch
(`concurrency_conflict`, `conflict`). Das gehört zum Ausformulieren.

Die neuen Texte folgen der Meldungsregel vom 2026-11-07 (`pm-loop` Step 5, (BZ)): Sie nennen, was sperrt, und keine Auswege.

**Erst messen, dann scharf stellen:** zuerst die Zuordnung Code → aufrufende Ebene ermitteln (Grep über
`ProblemWithCode(ApiErrors.…)` je `Controllers/{Tier}`), dann Texte nachziehen oder Ausnahmen mit Grund eintragen, zuletzt das
Tor. Ein neues Tor beendet den Lauf (`docs/backlog/README.md`, Limit 2).

## Verlauf

- **2026-11-07** — angelegt aus Retro-Vorschlag (BV) mit Erstmessung (33/24); Entscheidung des Nutzers: „Ja, wie empfohlen".
