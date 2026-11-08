---
tags: [typ/story, status/abgenommen, bereich/beides]
aliases: [Tor 409-Codes deutscher Text]
status: abgenommen
nachgeschaut: "2026-11-08"
prio: P2
art: Aufräumen
groesse: M
wo: beides
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-05.md#lauf-z
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

## User Story

Als Vater möchte ich jede Sperrmeldung, die ich an einem eigenen Knopf auslöse, auf Deutsch lesen – und als Entwickler möchte ich,
dass ein neuer 409-Code ohne deutschen Text nicht durchkommt.

## Ist-Stand am Code

Gemessen 2026-11-08 (Skript: je `ApiErrors`-Feld mit Status 409 die Fundstellen unter `backend/Pugling.Api`): 33 Codes, 9 mit
deutschem Text. Von den 24 übrigen:

- **17 erreicht ein Erwachsener** (Creator-/Supervisor-Controller oder deren Services): `duplicate_key`, `duplicate_achievement`,
  `duplicate_vocabulary_in_exercise`, `duplicate_key_result`, `media_variant_exists`, `media_already_linked`, `duplicate_email`,
  `duplicate_profile_name`, `duplicate_category_name`, `duplicate_publisher`, `grammar_topic_in_use`, `last_owner`, `vocabulary_in_use`,
  `position_has_data`, `timetable_slot_taken`, `purchase_not_open` (Kauf schon storniert), `activation_not_pending` (Anfrage schon
  beantwortet). Kein Frontend-Code behandelt einen davon gesondert.
- **5 zeigt nur die Arcade** (`Controllers/Student`, Kauf im `ShopService`): `skin_already_unlocked`, `shop_insufficient_stock`,
  `media_no_alternative`, `media_not_on_card`, `interest_wish_limit_reached`. *Berichtigt im Review:* zuerst stand hier auch
  `concurrency_conflict` – den erreicht aber auch der Vater (Stornieren, Genehmigen, Ablehnen im Shop, `ShopService` → `ShopController`),
  er bekam darum einen deutschen Text; aus 17 wurden 18. Die Arcade übersetzt **nicht** über
  `GERMAN_PROBLEM_TEXT`, sondern über `problem.*` in i18n (`frontend/src/lib/chromeText.tsx`, `PROBLEM_KEYS`) – dort fehlen sie
  ebenso → [B-415](B-415-arcade-sperrcodes-ohne-text.md).
- `conflict` ist der unbenutzte Default.

## Entscheidungen

1. **Das Tor prüft alle 409-Codes, nicht nur die der Erwachsenen-Ebenen**: Tabelle **oder** Ausnahme mit Grund. *Entscheidung im Lauf
   (Aufräumen), getroffen von Claude.* Begründung: „welche Ebene liefert den Code" liest ein Test nur über Quelltext-Grep, und Services
   werden von mehreren Ebenen gerufen (`ShopService`); eine Ausnahme mit Grund ist dagegen eine bewusste Zeile. Kosten: sechs
   Ausnahmezeilen (5 Arcade + `conflict`).
2. **Das Tor ist ein Backend-Test** (`ProblemTextGuardTests`): `ApiErrors` per Reflexion, die Tabelle aus `frontend/src/lib/api.ts` per
   Regex – wie `ClientRouteGuardTests` Quelldateien liest. Er prüft auch die Gegenrichtung: eine Ausnahme, die es nicht mehr braucht,
   ist rot.
3. **Die 17 Texte folgen der Meldungsregel** (BZ): sie nennen, was sperrt; „wähle einen anderen" nur bei Namensdubletten (die eigene
   Sperre).

## Akzeptanzkriterien

| Fall | erwartet |
|---|---|
| 1 | jeder der 17 Codes hat einen deutschen Text (`errorMessage.test.ts`, je Code: nicht die englische Zeile) |
| 2 | ein neuer 409-Code ohne Text und ohne Ausnahme → Tor rot, Meldung nennt den Code |
| 3 | eine Ausnahme für einen Code, der einen Text hat oder nicht mehr existiert → Tor rot |
| 4 | das Tor sieht genug: ≥ 30 Codes mit 409 und ≥ 9 Tabelleneinträge (Selbstschutz gegen ein leeres Grün) |

## Schätzung

**Größe: M** — 17 Texte gegen ihre Fundstellen gelesen, ein Tor, ein Vitest je Code. Ein neues Tor beendet den Lauf (Limit 2) – dieser
Lauf ist ohnehin ein Sprint. Reihenfolge im Sprint: B-413 (neuer Code) vor B-411, damit das Tor ihn gleich mit prüft.

## Verlauf

- **2026-11-07** — angelegt aus Retro-Vorschlag (BV) mit Erstmessung (33/24); Entscheidung des Nutzers: „Ja, wie empfohlen".
- **2026-11-08** — ausformuliert mit Messung (33/9, davon 17 Erwachsene, 6 Kind, 1 Default), gegrillt (Aufräumen, im Lauf entschieden, beschriftet) und geschätzt; Sprint 21.
- **2026-11-08** — gebaut. 18 deutsche Texte (17 + `concurrency_conflict` aus dem Review), das Tor `ProblemTextGuardTests` (beide Richtungen, gemeinsamer Selbstschutz ≥ 30 Codes / ≥ 9 Einträge) mit sechs begründeten Ausnahmen (5 Arcade + `conflict`). Belege: Fall 1 ← 18 Mutanten „B411-DE <code> fehlt“ (je ein Fall in `errorMessage.test.ts`), Fall 2 ← B411-G1/-G4, Fall 3 ← B411-G2, Fall 4 ← B411-G3.
- **2026-11-08** — abgenommen. Backend 1126/1126, Vitest 675/675, `tsc -b` sauber, Playwright **119 passed** (ganze Suite, Server vom Lauf nach dem letzten Edit gestartet; der erste Lauf war 117/2 – siehe Sprint-Protokoll). Mutanten mit `.claude/scripts/mutant-probe.py`, gemessen nach dem letzten Test-Edit an `c910cf8 + Arbeitsbaum`, 2026-11-08: 34 von 34 rot am eigenen Fall, Grundlinie danach grün (Tabelle im Protokoll `docs/pm-sitzung-2026-11-08.md`). **Zähler 4 von 5** (Sprint 21, beide Reviewer je zwei 🟡). Commit: siehe `git log -- docs/backlog/B-411-tor-409-codes-haben-deutschen-text.md` (Lauf AD, Sprint 21). Review: `concurrency_conflict` erreicht auch den Vater (Shop) – die Messung „nur das Kind“ war falsch, Text statt Ausnahme; Wörter an die Oberfläche angeglichen („Key“, „Messlatte“, „Ziel-Metrik“, „Verwalten“); Schlüsselwörter der Tests je Satz eindeutig. `verlag-fehler-am-platz-der-seite.spec.ts` erwartete die englische Zeile → auf den deutschen Satz umgestellt. **Rollengang** Vater = dieselbe Spec (Verlags-Dublette zeigt den deutschen Satz). Neues Tor → **Limit 2**.
- **2026-11-08** — Nachschau (Lauf AE, frischer `pugling-reviewer` auf `4c25504`): Zahlen nachgerechnet (vorher 33 Codes/9 Texte, nachher 34/28, sechs ohne Text = sechs Ausnahmen), jede Ausnahme gegen ihre Fundstelle (nur Student-Controller bzw. `ForStatus`), jeder der 18 neuen Texte gegen die Server-Bedingung, Regex des Tors gegen Kommentar- und CRLF-Zeilen. Kein 🔴/🟡; 🟢: von den zwei Bedingungen in `Every_Exception_Is_Still_Needed` ist nur eine durch eine Mutante belegt, und deren Definition ist nicht abgelegt – im Protokoll vermerkt, keine Story.
