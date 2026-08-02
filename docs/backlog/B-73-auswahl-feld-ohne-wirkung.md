---
tags: [typ/story, status/ausformuliert, bereich/backend, rolle/student]
aliases: [Auswahl ohne Wirkung, Question.Choices tot]
status: ausformuliert
prio: P2
art: Defekt
quelle: B-69 (Entscheidung 4)
---

# B-73 · Das Auswahl-Feld verspricht Multiple-Choice, das Kind bekommt Freitext

## User Story

Als **Creator** möchte ich, dass die Antwortmöglichkeiten, die ich zu einer Verständnisfrage eintrage,
beim Kind auch **ankommen** — oder dass das Feld gar nicht erst da steht, wenn die Ausspielung sie nicht
tragen kann.

## Ist-Stand am Code

### Die Kette ist an genau einer Stelle unterbrochen

| Station | Ort | Trägt die Optionen? |
| --- | --- | --- |
| Editor | [exerciseConfig.tsx:516](../../frontend/src/vater/exerciseConfig.tsx) | ✅ ein Feld je Möglichkeit (seit [B-69](B-69-wiederhol-felder-alternativen.md)) |
| Vertrag | `Question.Choices` ([ExerciseConfigs.cs:14](../../backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs)) | ✅ `List<string>?` |
| Speicherung | `ConfigJson` der Übung | ✅ unverändert |
| **Inhalts-Atome** | `AnswerChecking.FromQuestions` ([BuiltInExerciseTypes.cs:307](../../backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs)) | ❌ **hier fallen sie weg** |
| Karten-Facetten | `PositionPlayService.CardFacets` (`:112-124`) ruft `type.Choices(…)` | ✅ würde sie durchreichen |
| Übungstyp | `ReadingExerciseType`/`ListeningExerciseType` (`:11-35`) | ❌ erben `Choices => null` ([ExerciseTypeBase.cs:38](../../backend/Pugling.Api/Exercises/ExerciseTypeBase.cs)) |
| Sohn-Oberfläche | [SohnPractice.tsx:237-239](../../frontend/src/sohn/SohnPractice.tsx), [SohnTest.tsx:153-155](../../frontend/src/sohn/SohnTest.tsx) | ✅ rendert `card.choices`, wenn vorhanden |

Die einzige Zeile, die den Inhalt verliert:

```csharp
// BuiltInExerciseTypes.cs:307
[.. questions.Select((q, i) => new ContentItem(i, q.Prompt, q.Answer, [q.Answer]))]
```

`q.Choices` wird nicht gelesen. Und `Choices(…)` überschreibt **nur** `VocabularyExerciseType`
([VocabularyExerciseType.cs:54](../../backend/Pugling.Api/Exercises/VocabularyExerciseType.cs)) — Lese-
und Hörverstehen erben die Basis mit `null`.

**Beide Enden stehen also fertig da.** Das ist der wichtigste Befund: Es fehlt nicht die Oberfläche und
nicht der Vertrag, sondern die Verbindung im Übungstyp.

### Warum es keine Ein-Zeilen-Sache ist

Lese- und Hörverstehen sind als **reine Inhalts-Übungen** deklariert (`:18`, `:32`):

```csharp
ExerciseCheckMode.None, null /* PlayRoute */, null /* Method */, [...]
```

Daraus folgt dreierlei:

- Die Stufe ist `SelfAssess` ([ExerciseTypeBase.cs:29](../../backend/Pugling.Api/Exercises/ExerciseTypeBase.cs)),
  weil weder Übung noch Position eine andere setzt (`PositionPlayService.cs:57-61`). Bei
  `SelfAssess` wird die Lösung **aufgedeckt** — das Kind beurteilt sich selbst.
- `StageOptions` ist leer (`ExerciseTypeBase.cs:44`), es gibt für diese Typen also gar keine
  umschaltbare Abfrageform — weder im Testmodus noch am Fahrplan.
- Das Ziel gilt nicht über Trefferzahl, sondern über **gespielte Runden**
  (`PositionProgressService.cs:94ff`).

Antwortmöglichkeiten anzubieten und die Lösung gleichzeitig aufzudecken widerspricht sich. Die Optionen
durchzureichen genügt darum nicht — es braucht eine Aussage darüber, **wie** diese Typen abgefragt werden.

### Was daran nicht wehtut

- **Kein Bestand ist betroffen:** In der lokalen Entwicklungs-DB gibt es **keine einzige** gespeicherte
  `choices`-Liste. Der Fehler ist latent; er kostet heute niemanden Punkte.
- **Keine falsche Bewertung:** Weil nichts geprüft wird (`CheckMode.None`), führt das fehlende
  Multiple-Choice zu keiner falschen Note — anders als bei [B-65](B-65-vokabel-mehrere-uebersetzungen.md).
  Der Schaden ist vergeudete Creator-Arbeit und ein Versprechen, das die Oberfläche nicht hält.

### Ein Nachbarbefund, nicht belegt

`FromQuestions` baut den Prompt aus `q.Prompt` — der **Trägertext** (`ReadingConfig.Text`) bzw. das Audio
kommt in keinem `ContentItem` vor, und in `SohnPractice.tsx` habe ich keine Stelle gefunden, die ihn
nachlädt. Sieht danach aus, als sähe das Kind beim Spielen die Frage ohne den Text, zu dem sie gehört.
**Nicht nachgespielt** — es kann sein, dass Lese-/Hörverstehen praktisch nie als Position zugewiesen wird.
Gehört ohnehin in eine eigene Story (siehe offener Punkt 4).

## Die echte Lücke

Ein Eingabefeld erhebt eine Zusage, die die Ausspielung nicht einlöst: Der Creator tippt Ablenker, das
Kind bekommt sie nie zu sehen, und niemand erfährt davon. Die Verbindung fehlt an **einer** Stelle
(`FromQuestions` plus ein `Choices`-Override), aber sie herzustellen zwingt zu einer Entscheidung, die
bisher niemand getroffen hat: ob Lese- und Hörverstehen eine **abgefragte** Form haben sollen oder reine
Inhalts-Übungen bleiben.

## Offene Punkte

1. **Ausspielen oder Feld entfernen?** *Empfehlung: ausspielen.* Beide Enden stehen (Editor, Sohn-UI); es
   fehlt das Mittelstück. Eine Verständnisfrage mit vorgegebenen Antworten ist eine gängige Aufgabenform,
   und sie wegzuwerfen hieße, eine Fähigkeit abzubauen, die zu 80 % gebaut ist. Gegenrechnung: Entfernen
   wäre XS und sofort ehrlich.
2. **Wenn ausspielen: werden die Typen damit prüfbar (`CheckMode`)?** *Empfehlung: nein — `None` bleibt.*
   Der Wechsel auf einen echten Prüfmodus zöge Zielerreichung, Punkte und Malus nach sich; das ist eine
   andere Story. Stattdessen: eine **Stufe** `MultipleChoice` in `StageOptions` aufnehmen und
   `Choices(…)` überschreiben. Kosten: `IsTypedStage` muss für diese Stufe `true` liefern, damit die
   Lösung nicht zugleich aufgedeckt wird — sonst zeigt die Karte Optionen *und* Antwort.
3. **Was, wenn nur einige Fragen Optionen haben?** Die Stufe hängt an der Position, nicht am Item.
   *Empfehlung: je Item entscheiden — wo `Choices` steht, erscheinen sie; wo nicht, bleibt es bei der
   Selbsteinschätzung.* Das ist genau das Muster von `Choices(items, item, stage)`, das die Signatur schon
   vorsieht. Kosten: eine Runde kann gemischt aussehen; das ist verständlich, weil die Frage es hergibt.
4. **Der Trägertext erreicht das Kind offenbar nicht** (siehe oben). *Empfehlung: eigene Story, nicht
   hier* — und vorher am laufenden System nachspielen, statt es aus dem Code zu schließen. Wäre der Befund
   echt, änderte er die Prio dieser Story: Optionen zu einer Frage, deren Text fehlt, helfen wenig.
5. **Braucht das Feld eine Sperre, solange nichts ausgespielt wird?** *Empfehlung: nein.* Nach B-69 trägt
   es einen Hilfetext (`questionChoices`), und die Story hier ist der Weg, das Versprechen einzulösen —
   eine Warnung im Editor wäre eine dritte Stelle, die später wieder zurückgebaut werden müsste.

## Akzeptanzkriterien (Entwurf)

1. Eine Lese- oder Hörverstehen-Frage mit Antwortmöglichkeiten wird dem Kind **mit** diesen Möglichkeiten
   ausgespielt; ein Test, der vorher rot ist, hält das fest.
2. Eine Frage **ohne** Möglichkeiten verhält sich unverändert (Selbsteinschätzung).
3. Auf einer Stufe mit Antwortmöglichkeiten wird die Lösung **nicht** zugleich aufgedeckt.
4. Der Testmodus des Vaters zeigt dieselbe Form wie die Ausspielung beim Kind.
5. `CheckMode` bleibt `None`; Zielerreichung, Punkte und Malus verhalten sich unverändert.
6. Volle Suite grün; der Endpunkt-Abdeckungs-Wächter bleibt zufrieden.

## Verlauf

- **2026-08-02** — angelegt aus der Grill-Runde zu B-69, Entscheidung 4.
- **2026-08-02** — ausformuliert am Code. Der Befund ist bestätigt und zugleich entschärft: Die Kette ist
  an **einer** Stelle unterbrochen (`FromQuestions` liest `q.Choices` nicht, und der Typ überschreibt
  `Choices` nicht) — beide Enden, Editor und Sohn-Oberfläche, stehen fertig da. Entschärft, weil Lese- und
  Hörverstehen `CheckMode.None` tragen: Es wird nichts falsch bewertet, und im Bestand liegt **keine
  einzige** gespeicherte `choices`-Liste. Dafür ist die Reparatur keine Zeile: `SelfAssess` deckt die
  Lösung auf, `StageOptions` ist leer — Optionen anzubieten verlangt eine Aussage darüber, wie diese Typen
  abgefragt werden. Nebenbei aufgefallen und **nicht belegt**: Der Trägertext scheint das Kind gar nicht
  zu erreichen (Punkt 4).
