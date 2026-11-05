---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/creator]
aliases: [Pflichtfelder optional gemeint]
status: abgenommen
prio: P3
art: Frage
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-05.md#lauf-x
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-398 · Zwei Felder sind im Vertrag Pflicht, im Code aber optional gemeint

- `BirkenbihlSentenceInput.NaturalTranslation` (`backend/Pugling.Contracts/Creator/ExerciseAuthoringDtos.cs:115`) ist nicht-nullbar,
  also antwortet ein fehlendes oder `null` mit `400 … field is required`. `AddSentence` behandelt `null` aber mit `?? ""`
  (`ExerciseControllers.cs` um `:557`) – toter Code; die Absicht war offenbar „optional“. Auch die eigene Meldung für einen leeren
  `LearningSentence` erscheint bei `null` nie.
- `Gap.Answer` (`backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs:80`) heißt in der Summary „Rückfall für Lücken ohne
  Speicher-Bezug“ – eine Lücke mit `vocabKey` und `answer: null` wird trotzdem abgewiesen; Frontend und Tests schicken `""`.

**Frage** an den Nutzer: optional machen (`string?`) oder als Pflicht dokumentieren? Beides ist ein Vertragsthema.

## Entscheidungen

Entscheidung im Auftrag des Nutzers, getroffen von Claude (Dialog 2026-11-05, Auftrag „Starte 4 Nachtläufe … kommt es zu Fragen,
kläre diese selber nach bestem Wissen und Gewissen"). Leitlinie: im Zweifel die kleinere Form.

1. **`NaturalTranslation` wird optional** (`string?`) – in `BirkenbihlSentenceInput` **und** `DecodePreviewInput` (dort stand dasselbe
   tote `?? ""`, vom Reviewer nicht genannt). Die Handler meinten es immer so; `""` war schon erlaubt, `null` heißt jetzt dasselbe.
   *Kosten:* keine – die Lockerung ist additiv, kein Client bricht.
2. **Die Sprachen in `DecodePreviewInput` bleiben Pflicht**, das tote `?? ""` fliegt raus: ohne Sprachpaar findet der Store nichts
   (`SourceLanguage == ""`), „optional" wäre eine leere Vorschau.
3. **`Gap.Answer` bleibt Pflicht** und wird in der Summary so dokumentiert (`""` bei Speicher-Bezug). Optional machen hieße sieben
   Lesestellen (`AnswerGrader`, `ExerciseContentResolver`, `BuiltInExerciseTypes`, …) plus Altbestand in der JSON-Spalte anfassen – die
   größere Form, ohne dass ein Client darunter leidet (Frontend und Agent schicken `""`).

## Akzeptanzkriterien

- Satz anlegen **ohne** `naturalTranslation` → `201`, `naturalTranslation: ""`.
- Satz anlegen mit `naturalTranslation: null` → `201`.
- Dekodier-Vorschau ohne `naturalTranslation` → `200`.
- `Gap`-Summary nennt die Pflicht.

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu Lauf X, Sprint 10 (frischer `pugling-reviewer`, 🟢). Am Code gelesen, nicht gemessen.
- **2026-11-05** — entschieden (im Auftrag, siehe oben), geschätzt und gebaut in Lauf Y, Sprint 12. **Rote Probe** gegen den
  Ausgangszustand: `BirkenbihlExerciseTests.cs:192` erwartet `201`, gemessen `400`. Volle Suite 1105/1105.
- **2026-11-05** — Review `pugling-reviewer`: kein Fund zu B-398; Mutanten je Zusicherung benannt und plausibel: `BirkenbihlSentenceInput.
  NaturalTranslation` zurück auf `string` → rot `:192` (und `:197`, nie allein erreichbar, da derselbe Typ); `DecodePreviewInput.
  NaturalTranslation` zurück → rot nur `:201`; `?? ""` im Satz-Handler entfernt → rot `:193`. Gemessen ist nur die erste (Ausgangszustand).
  Vertrag `docs/openapi/v1.json` und `frontend/src/lib/contract.ts` neu erzeugt, `tsc` sauber.
- **2026-11-05** — **abgenommen.** Rollengang Creator als Integrationsfall (die Oberfläche schickt `naturalTranslation` immer mit); Vater
  und Sohn als Regressionszeugen: Playwright 115 passed nach dem letzten Code-Edit. Commit: siehe nächster Eintrag.
