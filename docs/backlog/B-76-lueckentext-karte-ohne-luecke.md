---
tags: [typ/story, status/ausformuliert, bereich/backend, bereich/frontend, rolle/student]
aliases: [Lückentext ohne Lücke, Welche Lücke ist gemeint, Wortbank kommt nie an]
status: ausformuliert
prio: P1
art: Defekt
quelle: B-75 (Grill-Runde, Entscheidung 2)
---

# B-76 · Der Lückentext sagt dem Kind nicht, welche Lücke gemeint ist

## User Story

Als **Kind** möchte ich bei einem Lückentext sehen, **welche** Lücke gerade gefragt ist — und die
Wortbank bekommen, die die Stufe verspricht —, damit ich die Aufgabe lösen kann, statt zwischen zwei
gleich aussehenden Karten zu raten.

## Ist-Stand am Code

Angelegt wurde exakt die geseedete Übung ([Seed.cs:1022-1038](../../backend/Pugling.Api/Data/Seed.cs)),
gespielt als Kind über `/practice-sessions/{id}/cards`:

```json
--- TranslationWordBank (die geseedete Stufe) ---
[{"itemIndex":0,"prompt":"A: {{1}}, how are you? B: I'm {{2}}, thank you.","choices":null,"reveal":"Hello"},
 {"itemIndex":1,"prompt":"A: {{1}}, how are you? B: I'm {{2}}, thank you.","choices":null,"reveal":"fine"}]

--- FreeText ---
[{"itemIndex":0,"prompt":"A: {{1}}, how are you? B: I'm {{2}}, thank you.","choices":null,"reveal":null},
 {"itemIndex":1,"prompt":"A: {{1}}, how are you? B: I'm {{2}}, thank you.","choices":null,"reveal":null}]
```

Drei Befunde stecken darin:

1. **Die Lücke ist nicht adressiert.** `ClozeExerciseType.ItemsOf` setzt `GapIndex`
   ([BuiltInExerciseTypes.cs:118](../../backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs), ebenso
   [ExerciseContentResolver.cs:123](../../backend/Pugling.Api/Services/Shared/ExerciseContentResolver.cs)),
   aber das Feld erreicht **nur** `PreviewItem`, den Testmodus des Vaters
   ([ExercisePreviewDtos.cs:10](../../backend/Pugling.Contracts/Creator/ExercisePreviewDtos.cs)).
   `PracticeCard` ([PracticeDtos.cs:27-29](../../backend/Pugling.Contracts/Student/PracticeDtos.cs)) und
   `TestItem` ([TestDtos.cs:11-12](../../backend/Pugling.Contracts/Student/TestDtos.cs)) führen es nicht.
2. **Die Wortbank kommt nie an.** `ClozeConfig.WordBank` ist gefüllt, das Manifest führt `wordBank` als
   Fähigkeit (`BuiltInExerciseTypes.cs:112`) — `choices` ist trotzdem `null`. Der Grund:
   `IExerciseType.Choices` ist genau dafür da, wird aber **nur** von `VocabularyExerciseType`
   überschrieben ([:54](../../backend/Pugling.Api/Exercises/VocabularyExerciseType.cs)); alle anderen
   Typen erben die Vorgabe `null` ([ExerciseTypeBase.cs:38](../../backend/Pugling.Api/Exercises/ExerciseTypeBase.cs)).
   Die Stufe heißt „Wortbank" (`ClozeEntities.cs:14`) und liefert keine.
3. **Die Vorlagensyntax ist sichtbar.** `SohnPractice.tsx:228` rendert `card.prompt` roh; im Frontend gibt
   es keine Behandlung von `{{n}}`.

Auf der geseedeten Stufe ist das Kind nicht blockiert — sie ist nicht getippt, also liefert die Karte die
Lösung (`reveal`), und es bleibt eine Umdreh-Karte. Auf `FreeText` — ebenfalls geseedet
(`Seed.cs:357-365`) — ist es echtes Raten.

### Es ist nicht der Lückentext allein

Beim Ausformulieren war offen, ob weitere Typen dieselbe Lücke haben. Sie haben. Am laufenden System
gemessen, gleiche Methode:

| Typ | Was das Kind bekommt | Bewertung |
|---|---|---|
| `List` | **Alle** Karten tragen denselben `prompt` (die Anweisung der Übung), z. B. dreimal „Nenne die Bundeslaender." — und ohne Anweisung ist er `""`. `ContentItem(i, c.Instruction ?? "", …)` (`BuiltInExerciseTypes.cs:269`) | Derselbe Defekt, **reiner**: die geseedete Liste hat 16 Einträge, also 16 gleiche Karten |
| `Matching` | `Direct` und `Distractors` liefern **identische** Karten, `choices: null` in beiden (`:159`) | Die Stufe `Distractors` (`StudyPlanEntities.cs:26`) liefert keine Ablenker |
| `Grammar` | Die übergreifende `Instruction` („Setze das Verb ins Simple Past.") wird verworfen, nur der Einzel-Prompt kommt an (`:64`) | Milder: der Einzel-Prompt trägt für sich |
| `Translation`, `Arithmetic` | Prompt ist die Aufgabe selbst | Unauffällig |

Nicht am laufenden System, sondern nur am Code geprüft: **`Birkenbihl`** baut
`ContentItem(i, s.LearningSentence, s.NaturalTranslation, …)` (`:99`) — die `Decoding`, die
Wort-für-Wort-Entschlüsselung, wird verworfen (`ExerciseConfigs.cs:249`). Das ist der ganze Zweck der
Methode; übrig bleibt eine gewöhnliche Übersetzungskarte. Sollte vor dem Grillen nachgespielt werden.

`List` und `Matching` liegen wie der Lückentext als **Positionen im Seed** (`Seed.cs:369-385`, `:401-402`).

## Die echte Lücke

Es ist ein einziger Konstruktionsfehler mit zwei Gesichtern, und beide sitzen in derselben Naht
(`PositionPlayService.CardFacets` → `PracticeCard`/`TestItem`):

1. **Übungsweiter Inhalt hat keinen Platz.** Text, Anweisung, Wortbank gehören der *Übung*, nicht dem
   Inhalts-Atom. Wer sie trotzdem ausliefern will, hat heute nur den `Prompt` — also wird kopiert
   (Lückentext, Liste) oder weggeworfen (Grammatik, Birkenbihl). Kopieren macht die Karten
   ununterscheidbar, Wegwerfen nimmt dem Kind die Aufgabe.
2. **Das Atom kann sich nicht ausweisen.** `GapIndex` existiert, kommt aber nur beim Vater an. Ohne ein
   „welches von diesen" ist eine Karte, deren Prompt sie mit anderen teilt, nicht beantwortbar — egal wie
   gut der Prompt ist.

Der zweite Punkt ist der eigentliche: Selbst wenn der Text sauber übertragen wird, bleiben zwei Lücken
zwei gleiche Karten. **`Choices`** ist dabei kein dritter Fehler, sondern eine ungenutzte Vorkehrung —
der Haken ist da, nur hängt außer dem Vokabel-Typ niemand etwas hinein.

Beachtenswert für den Zuschnitt: [B-75](B-75-lese-hoerverstehen-ohne-inhalt.md) hat für Punkt 1 bereits
entschieden (E1: additives Feld auf der Karte). Punkt 2 ist neu und gehört hierher.

## Offene Punkte

1. **Wie weist sich das Atom aus — Nummer oder aufgelöste Vorlage?** Entweder die Karte trägt `GapIndex`
   und das Frontend hebt die Lücke hervor, oder der Server liefert den Text bereits aufgelöst (nur die
   gefragte Lücke bleibt Platzhalter). — *Empfehlung: die Nummer.* Auflösen hieße entscheiden, was mit den
   **anderen** Lücken passiert: zeigt man ihre Lösung, verrät man die Nachbarkarten; lässt man sie als
   `{{2}}` stehen, hat man das Problem nur verschoben. Die Nummer ist außerdem additiv und für Liste und
   Zuordnung gleichermaßen brauchbar (dort wäre es eher ein „Nummer/Gesamt").
2. **Ein Feld für alle oder je Typ eines?** Lückennummer, Listenposition, Paar-Index sind dasselbe
   Bedürfnis in drei Kleidern. — *Empfehlung: ein Feld*, etwa `ItemLabel` als fertiger Text
   („Lücke 2", „3 von 16"). Der Server weiß, was der Typ meint; das Frontend soll es nur anzeigen, nicht
   deuten. Kosten: der Text ist dann serverseitig deutsch — bisher sind Meldungstexte englisch und die
   Oberfläche macht die Formulierung. Die Gegenvariante (rohe Zahl plus Frontend-Formulierung) hält die
   i18n-Linie, verteilt aber die Typkenntnis wieder ins Frontend.
3. **Wächst diese Story auf Liste, Zuordnung, Grammatik und Birkenbihl?** — *Empfehlung: ja für Liste und
   Zuordnung* (beide geseedet, beide derselbe Defekt, dieselbe Naht), **nein für Grammatik und
   Birkenbihl**: dort fehlt übungsweiter Inhalt, aber die Karten sind unterscheidbar — das ist B-75s
   Muster, nicht dieses. Kosten der Empfehlung: die Story wird größer; dafür wird die Naht **einmal**
   angefasst statt dreimal.
4. **Bekommt der Lückentext seine Wortbank über `Choices`?** — *Empfehlung: ja*, das ist der vorgesehene
   Haken. Offen bleibt die fachliche Frage: gilt der Pool je Übung (dann taucht ein bereits verbrauchtes
   Wort erneut auf) oder je Lücke (dann müsste die Config das hergeben — sie tut es nicht)?
5. **Gilt dasselbe für die Klausur?** Der Lückentext ist `ExerciseCheckMode.StudyPlanTest`, wird also auch
   als Abschlusstest gespielt. — *Empfehlung: ja, `TestItem` zieht mit.* Sonst ist die Übungsrunde
   lösbar und die Klausur nicht — und die Klausur ist die, die zählt.
6. **Was passiert mit der rohen Vorlagensyntax?** — *Empfehlung: an Punkt 1 hängen* — wird die Lücke
   hervorgehoben, muss das Frontend `{{n}}` ohnehin erkennen und ersetzen. Getrennt gelöst hieße, denselben
   Parser zweimal zu schreiben.

## Akzeptanzkriterien (Entwurf)

- Zwei Lücken eines Lückentexts liefern zwei **unterscheidbare** Karten; das Kind sieht, welche gefragt
  ist.
- Die Stufe „Wortbank" liefert eine Wortbank.
- Die rohe Vorlagensyntax `{{n}}` erscheint nirgends in der Sohn-Ansicht.
- Dasselbe gilt in der Klausur, nicht nur beim Üben.
- Regressionstest, der vorher rot ist: eine gespielte Position mit **zwei** Lücken, geprüft darauf, dass
  die Karten sich unterscheiden — heute wären sie zeichengleich.

## Verlauf

- **2026-08-02** — angelegt aus der Grill-Runde zu B-75. Der Befund war schon dort am laufenden System
  belegt; P1 vom Nutzer gesetzt, mit Vorrang vor B-75, weil dieser Defekt heute an der geseedeten Familie
  wirkt.
- **2026-08-02** — ausformuliert. Die offene Frage „haben weitere Typen dieselbe Lücke?" ist beantwortet,
  und die Antwort ist größer als erwartet: **Liste** trifft es reiner als den Lückentext (16 geseedete
  Einträge, 16 gleiche Karten; ohne Anweisung sogar ein leerer Prompt), **Zuordnung** liefert auf beiden
  Stufen identische Karten ohne die versprochenen Ablenker, **Grammatik** verwirft die übergreifende
  Anweisung. Damit ist erkennbar, dass es *ein* Konstruktionsfehler ist und nicht drei Einzelfälle: Die
  Karte hat keinen Platz für übungsweiten Inhalt, und das Inhalts-Atom kann sich nicht ausweisen. Ob die
  Story auf Liste und Zuordnung wächst, ist als offener Punkt 3 formuliert — nicht entschieden.
