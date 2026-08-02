---
tags: [typ/story, status/idee, bereich/backend, bereich/frontend, rolle/student]
aliases: [Lückentext ohne Lücke, Welche Lücke ist gemeint, Wortbank kommt nie an]
status: idee
prio: P1
art: Defekt
quelle: B-75 (Grill-Runde, Entscheidung 2)
unverifiziert: true
---

# B-76 · Der Lückentext sagt dem Kind nicht, welche Lücke gemeint ist

Ein Lückentext mit zwei Lücken liefert dem Kind **zweimal dieselbe Karte**. Welche Lücke gerade gefragt
ist, steht nirgends; die Wortbank, die der Stufenname verspricht, kommt gar nicht an; und die rohe
Vorlagensyntax `{{1}}` steht im Text.

Anders als bei [B-75](B-75-lese-hoerverstehen-ohne-inhalt.md) trifft das **heute** jemanden: Der
Lückentext liegt als Position im Seed (`Seed.cs:346-353`, Stufe `TranslationWordBank`), Lese- und
Hörverstehen liegen dort nicht.

## Der Befund, am laufenden System gemessen

Angelegt wurde exakt die geseedete Übung (`Seed.cs:1022-1038`), gespielt als Kind:

```json
--- TranslationWordBank (die geseedete Stufe) ---
[{"itemIndex":0,"prompt":"A: {{1}}, how are you? B: I'm {{2}}, thank you.","choices":null,"reveal":"Hello"},
 {"itemIndex":1,"prompt":"A: {{1}}, how are you? B: I'm {{2}}, thank you.","choices":null,"reveal":"fine"}]

--- FreeText ---
[{"itemIndex":0,"prompt":"A: {{1}}, how are you? B: I'm {{2}}, thank you.","choices":null,"reveal":null},
 {"itemIndex":1,"prompt":"A: {{1}}, how are you? B: I'm {{2}}, thank you.","choices":null,"reveal":null}]
```

Drei Dinge stecken darin:

1. **Die Lücke ist nicht adressiert.** `ClozeExerciseType.ItemsOf` setzt zwar `GapIndex`
   ([BuiltInExerciseTypes.cs:118](../../backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs), ebenso
   [ExerciseContentResolver.cs:123](../../backend/Pugling.Api/Services/Shared/ExerciseContentResolver.cs)),
   aber das Feld erreicht **nur** `PreviewItem`, den Testmodus des Vaters
   ([ExercisePreviewDtos.cs:10](../../backend/Pugling.Contracts/Creator/ExercisePreviewDtos.cs)).
   `PracticeCard` und `TestItem` führen es nicht. Auf der Freitext-Stufe soll das Kind also tippen, ohne
   zu wissen, wonach gefragt ist.
2. **Die Wortbank kommt nie an.** `ClozeConfig.WordBank` ist gefüllt, der Typ führt `wordBank` als
   Fähigkeit — `choices` ist trotzdem `null`. Die Stufe heißt „Wortbank" und liefert keine.
3. **Die Vorlagensyntax ist sichtbar.** `SohnPractice.tsx` rendert `card.prompt` roh; im Frontend gibt es
   keine Behandlung von `{{n}}`.

Auf der geseedeten Stufe ist das Kind nicht völlig blockiert — sie ist nicht getippt, also liefert die
Karte die Lösung (`reveal`), und es bleibt eine Umdreh-Karte. Der Preis ist trotzdem hoch: Die Stufe tut
nicht, was ihr Name sagt, und die Lücken-Zuordnung ist Raten. Auf `FreeText` ist es echtes Raten.

## Warum das mit B-75 zusammenhängt — und warum es trotzdem getrennt läuft

Beide Defekte haben dieselbe Ursache: **Die Karte trägt nicht, was der Typ braucht.** Sie fassen darum
dieselbe Naht an (`PositionPlayService.CardFacets`, `PracticeCard`/`TestItem`, `PreviewItem`). Getrennt
laufen sie, weil B-75s Entscheidung E1 **additiv** ist: ein Feld für den Trägertext widerspricht einem
Feld für die Lückennummer nicht. Zusammengelegt wäre die Story größer als L, und das Backlog kennt kein XL.

## Zu prüfen beim Ausformulieren

- Ob `GapIndex` auf die Karte gehört oder ob die **Vorlage** serverseitig aufgelöst wird (etwa: nur die
  gefragte Lücke bleibt als Platzhalter stehen, die übrigen zeigen ihre Lösung — was aber bei ungelösten
  Nachbarlücken verrät, was noch kommt). Das ist die eigentliche fachliche Frage.
- Warum `Choices` für den Lückentext nicht überschrieben ist, obwohl `IExerciseType.Choices` genau dafür
  da ist — und ob die Wortbank je Lücke oder je Übung gelten soll (bei mehreren Lücken teilen sie sich
  einen Pool, und ein verbrauchtes Wort wäre ein Hinweis).
- Ob die Klausur (`TestItem`) dieselbe Lücke-Adressierung braucht — der Lückentext ist
  `ExerciseCheckMode.StudyPlanTest`, wird also auch als Abschlusstest gespielt.
- Ob weitere Typen dieselbe Lücke haben. Geprüft ist bisher: Lese-/Hörverstehen (B-75) und der
  Lückentext. Ungeprüft: Matching, Translation, List, Grammar, Birkenbihl.

## Verlauf

- **2026-08-02** — angelegt aus der Grill-Runde zu B-75. Der Befund selbst ist am laufenden System
  belegt (Wegwerf-Integrationstest, Ausgabe oben); `unverifiziert` steht trotzdem, weil die Story als
  Ganzes — Ist-Stand, echte Lücke, Akzeptanzkriterien — noch nicht ausformuliert ist. P1 vom Nutzer
  gesetzt, mit Vorrang vor B-75: dieser Defekt wirkt heute an der geseedeten Familie.
