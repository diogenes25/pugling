---
tags: [typ/story, status/idee, bereich/backend, bereich/frontend, rolle/student]
aliases: [Birkenbihl ohne Dekodierung, Wort-für-Wort kommt nicht an]
status: idee
prio: P2
art: Defekt
quelle: B-76 (Grill-Runde, Entscheidung 1)
unverifiziert: true
---

# B-78 · Die Birkenbihl-Dekodierung erreicht das Kind nicht

Die Birkenbihl-Methode **ist** die Wort-für-Wort-Dekodierung: Ein Satz der Lernsprache steht über seiner
positionsgenauen Entschlüsselung in der Muttersprache, grammatikunabhängig. Genau diese Zuordnung kommt
beim Kind nicht an.

`BirkenbihlExerciseType.ItemsOf` baut
`new ContentItem(i, s.LearningSentence, s.NaturalTranslation, [s.NaturalTranslation])`
([BuiltInExerciseTypes.cs:99](../../backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs)) — die
`Decoding` des Satzes ([ExerciseConfigs.cs:249](../../backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs))
wird nicht gelesen. Übrig bleibt Satz → natürliche Übersetzung, also eine gewöhnliche
Übersetzungskarte. Die Methode, die dem Übungstyp den Namen gibt, findet nicht statt.

Die Übung liegt als Position im geseedeten Plan
([Seed.cs:389-390](../../backend/Pugling.Api/Data/Seed.cs), `GoalCadence.None`).

**Nur am Code belegt, nicht nachgespielt.** Die Beweislage ist eindeutig — der Konstruktor-Aufruf nimmt
vier Argumente, keins davon die Dekodierung, und `PracticeCard` hat kein Feld, das Wortpaare tragen
könnte —, aber gespielt wurde diese Position bisher nicht. Das ist der erste Schritt beim Ausformulieren.

## Warum das eine eigene Story ist

In der Grill-Runde zu [B-76](B-76-lueckentext-karte-ohne-luecke.md) wurde nach **Defekt** getrennt, nicht
nach der gemeinsamen Naht. Birkenbihl passt in keine der beiden vorhandenen Formen:

- Es ist **nicht** [B-75](B-75-lese-hoerverstehen-ohne-inhalt.md) („übungsweiter Text fällt weg"): Die
  Dekodierung gehört zum einzelnen Satz, nicht zur Übung, und sie ist kein Text, sondern eine **Liste von
  Wortpaaren** (`WordPair`) mit übungsweit eindeutiger `WordId`. Ein `Passage`-Feld nähme sie nicht auf.
- Es ist **nicht** B-76 („das Atom kann sich nicht ausweisen"): Die Karten sind unterscheidbar, jeder Satz
  steht für sich.

Die Reparatur braucht also eine dritte Form auf der Karte — strukturierte, positionsgenaue Wortpaare.

## Zu prüfen beim Ausformulieren

- Zuerst eine Birkenbihl-Position durchspielen und die Karte ansehen.
- Wie die Dekodierung auf die Karte kommt: als eigene strukturierte Liste, oder als vorformatierter Text
  (Zeile 1 Original, Zeile 2 Gloss)? Die positionsgenaue Ausrichtung ist der Kern der Methode und geht in
  einem Fließtext verloren.
- Was die Stufe bedeutet. Der Typ ist `ExerciseCheckMode.None` und erbt `IsTypedStage => true` — er
  verlangt also getippte Antworten, obwohl der Vertrag die Methode ausdrücklich als „verzichtet bewusst
  auf aktives Abfragen" beschreibt (`ExerciseConfigs.cs:221-225`). Berührt dieselbe Frage wie
  [B-73](B-73-auswahl-feld-ohne-wirkung.md).
- Ob der Dekodierungs-Editor des Vaters (Wort-Austausch, `.../words/{wordId}`) ein Gegenstück in der
  Ausspielung braucht — heute pflegt er etwas, das niemand zu sehen bekommt.

## Verlauf

- **2026-08-02** — angelegt aus der Grill-Runde zu B-76, Entscheidung 1. `prio: P2` statt P1: Die Position
  ist geseedet, trägt aber `GoalCadence.None` — sie ist keine Pflicht, und niemand verliert Münzen daran.
  Nicht vom Nutzer ausdrücklich bestätigt.
