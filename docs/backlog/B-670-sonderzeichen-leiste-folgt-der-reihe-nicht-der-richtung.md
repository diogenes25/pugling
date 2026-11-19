---
tags: [typ/story, status/ausformuliert, bereich/training, rolle/student]
aliases: [Sonderzeichen-Leiste falsche Sprache]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: beides
migration: ""
vertragsbruch: ""
quelle: "Nachtlauf 2026-11-18, Nachschau von B-661 (96a3491d, frontend-reviewer) – docs/pm-sitzung-2026-11-18.md, Runde 5"
grund: ""
ersetzt_durch: []
entgangen_bei: [B-661]
---

# B-670 · Die Sonderzeichen-Leiste folgt der Sprache der Reihe, nicht der Sprache, in der das Kind tippt

Am Code bestätigt (Arbeitsstand 2026-11-18): `PositionPlayService.KeyboardLangOf` liefert je **Übung** die
`SourceLanguage` der Reihe, unabhängig von der Richtung der Karte; alle drei Spielpfade reichen das durch. Bei einer
Französisch-Karte „vorn nach hinten“ (Antwort deutsch) zeigt die Leiste die französischen Zeichen statt ä/ö/ü/ß; bei
Theodors umgedrehter Englisch-Karte erscheint gar keine. B-661 AK 1 sagt „je getippter Sprache“.

Geringer Schaden (deutsche Handy-Tastaturen haben Umlaute, nichts wird verraten).

## User Story

Als Schüler möchte ich unter dem Antwortfeld die Sonderzeichen der Sprache angeboten bekommen, in der ich gerade
tippe, damit ich weder ein „î“ noch ein „ß“ suchen muss.

## Ist-Stand am Code

- **Eine Sprache je Übung:** `KeyboardLangOf(exercise)` gibt `exercise.SeriesUnit?.Series?.SourceLanguage` zurück
  (`backend/Pugling.Api/Services/Shared/PositionPlayService.cs:449`; Kommentar `:444-447`: die gelernte Sprache der Reihe).
  Aufrufer: `PositionPracticeController.cs:315`, `PositionTestsController.cs:54`, `ExtraTrainingService.cs:433`; im Vertrag
  `KeyboardLang` an `PracticeCard`, `TestItem` und der Extra-Training-Karte (`PracticeDtos.cs:134`, `TestDtos.cs:52`,
  `ExtraTrainingDtos.cs:71`). Die Karte (`ContentItem`) trägt kein Sprachfeld dafür.
- **Was getippt wird, hängt an Karte und Stufe:** eine Vokabelkarte „vorn nach hinten“ fragt das fremde Wort (Antwort =
  Übersetzung); `back-to-front` tauscht je Karte (`ExerciseContentProvider.cs:96-102`, `Swap` ab `:116`, `Reversed = true`);
  bei `both` entsteht je Wort ein Paar. Auf Schreibstufen wird die Vorwärtskarte durch ihre vertauschte Form `Written`
  ersetzt (`VocabularyExerciseType.cs:116`; gebaut in `ExerciseContentResolver.cs:117-122`) – dort tippt das Kind das
  gelernte Wort.
- **Eine Regel „Antwortsprache je Karte“ gibt es schon:** `answerLanguage = card.Reversed ? language.Source : language.Target`
  für den Artikel-Vergleich (`ExerciseContentResolver.cs:127-130`, B-421), mit den Sprachen je Vokabel. Die Reihe hat
  zusätzlich `TargetLanguage` (`CurriculumEntities.cs:49`; der Seed setzt `"de"`, `Seed.cs:745`).
- **Frontend:** `leistenZeichen(sprache, wort?)` (`frontend/src/lib/sonderzeichen.ts:22`) schlägt
  `JE_SPRACHE[sprache.toLowerCase()]` nach (`:23`); Einträge `fr`, `de`, `es` (`:8-12`). Eine Form wie „fr-FR“ trifft
  nichts: ein Abschneiden am Bindestrich ist nicht gefunden. In der echten DB stehen nackte Codes (nicht am Code, nur aus
  dem Seed `Seed.cs:745` belegbar: dort `"de"`). Aufrufer: `Lektion.tsx:176`, `Lernkarte.tsx:98-99`, jeweils mit
  `karte.keyboardLang`.
- **Tests:** `TastaturSpracheTests` hat `Uebungskarte_Nennt_Die_Lernsprache_Der_Reihe_Auch_Umgedreht` (back-to-front),
  `Testfrage_Nennt_Die_Lernsprache_Der_Reihe` (back-to-front) und `Reihe_Ohne_Sprache_Nennt_Keine`. Ein `front-to-back`-Fall
  mit Leiste: nicht gefunden (`Reihe_Ohne_Sprache…` nutzt `front-to-back`, prüft aber nur `null`).
  `sonderzeichen.test.ts` testet nur nackte Codes.

## Die echte Lücke

Die Idee stimmt, ist aber enger gefasst, als sie wirkt: der Server kennt die Antwortsprache **je Übung**, die Karte
verlangt sie **je Karte und Stufe**. Drei Fälle: (1) Vorwärtskarte auf einer getippten Nicht-Schreibstufe: Antwort
deutsch → heute falsche Leiste (französisch). (2) Vertauschte Karte oder Schreibstufe: Antwort gelernt → heute richtig.
(3) Typen ohne Richtung (Lückentext, Liste): die Antwort steht in der Lernsprache der Reihe → heute richtig; die
Zielsprache gilt hier **nicht**. Ein einfaches „immer `TargetLanguage`“ wäre also falsch, und `KeyboardLangOf` allein
reicht nicht – es braucht die Karte und die Stufe, auf der sie gespielt wird.

## Offene Punkte

1. **Wer entscheidet je Karte: Server oder Frontend?** Empfehlung: Server (Backend zuerst) – er kennt `Reversed`,
   `Written` und die Stufe (`IsTypedStage`), das Frontend nur den fertigen Wert. `keyboardLang` bleibt ein Vertragsfeld,
   nur sein Wert wird je Karte gerechnet (Vertragsbruch beim Schätzen prüfen).
2. **Reihe ohne `TargetLanguage`?** Empfehlung: keine Leiste (`null`) statt Rückfall auf „de“, wie B-661 es für eine Reihe
   ohne Sprache hält.
3. **Regionale Codes („fr-FR“)?** Empfehlung: im Frontend am Bindestrich abschneiden, ein eigenes Akzeptanzkriterium,
   kein eigener Bau.
4. **Lernkarte (Abschreiben)?** Dort ergänzt `leistenZeichen(…, wort)` die Zeichen des Wortes (`Lernkarte.tsx:98`), und
   das abgeschriebene Wort ist das gelernte. Empfehlung: dort unverändert lassen.

## Entscheidungen

Keine – eine Defekt-Story geht erst mit `gegrillt` in die Entscheidungen.

## Akzeptanzkriterien

Entwurf; je Kriterium die Änderung, die es rot macht.

1. Eine Vorwärtskarte (`front-to-back`) einer Reihe mit `SourceLanguage fr` und `TargetLanguage de` meldet auf getippter
   Stufe `keyboardLang = "de"` – Üben, Test und Extra-Training. Rot, wenn die Sprache weiter je Übung statt je Karte
   gerechnet wird.
2. Eine vertauschte Karte (`back-to-front` und die Schreibstufe einer Vorwärtskarte) meldet weiter `"fr"`. Rot, wenn
   Fall (1) mit „immer `TargetLanguage`“ gelöst wird.
3. Ein Lückentext und eine Liste einer französischen Reihe melden `"fr"`. Rot bei derselben Verallgemeinerung.
4. Eine Reihe ohne Sprache meldet `null` (vorhanden: `Reihe_Ohne_Sprache_Nennt_Keine`). Rot, wenn ein Standard-Code
   eingesetzt wird.
5. `leistenZeichen("fr-FR")` liefert die französische Menge. Rot ohne das Abschneiden am Bindestrich.

## Verlauf

- **2026-11-18** — angelegt (Quelle: Nachschau von B-661).
- **2026-11-19** — ausformuliert (Vorbereitungslauf, Sonnet): Fundstellen neu belegt; die Lücke ist je Karte **und**
  Stufe, nicht je Übung – „immer Zielsprache“ wäre für Lückentext und Liste falsch. `unverifiziert` entfällt (war schon
  `false`).
