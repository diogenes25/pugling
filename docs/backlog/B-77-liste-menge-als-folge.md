---
tags: [typ/story, status/idee, bereich/backend, rolle/student]
aliases: [Liste als Menge, 16 gleiche Karten, Ungeordnete Liste im Übungs-Pfad]
status: idee
prio: P1
art: Defekt
quelle: B-76 (Grill-Runde, Entscheidung 1)
unverifiziert: true
---

# B-77 · Der Übungs-Pfad bewertet eine ungeordnete Liste als Folge

Eine Liste, bei der die Reihenfolge **nicht** zählt, wird beim Üben trotzdem eintragsweise abgefragt: Karte
0 verlangt genau den ersten Eintrag, Karte 1 genau den zweiten. Das Kind kann nicht wissen, welcher gemeint
ist — die Karten sehen alle gleich aus.

Der Katalog-Check kann es richtig. `ListConfig.Ordered` sagt, ob die Reihenfolge zählt, und
`ListExerciseType.Check` bewertet eine ungeordnete Liste als **Menge**: jede Antwort darf auf jeden noch
offenen Eintrag passen
([BuiltInExerciseTypes.cs:276-295](../../backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs)).

Der Übungs-Pfad kennt diese Unterscheidung nicht. `ItemsOf` baut je Eintrag ein `ContentItem`, dessen
`AcceptedAnswers` genau diesen einen Eintrag enthalten (`:269`) — eine Folge, keine Menge.

Am laufenden System gemessen (drei Einträge, Freitext-Stufe):

```json
[{"itemIndex":0,"prompt":"Nenne die Bundeslaender.","reveal":null,"choices":null},
 {"itemIndex":1,"prompt":"Nenne die Bundeslaender.","reveal":null,"choices":null},
 {"itemIndex":2,"prompt":"Nenne die Bundeslaender.","reveal":null,"choices":null}]
```

**Das wirkt heute.** „Die 16 Bundesländer" liegt als Position im geseedeten Plan
([Seed.cs:401-402](../../backend/Pugling.Api/Data/Seed.cs)) — Wochenpflicht, Bestehensgrenze 90 % — und
die Übung setzt `Ordered` nicht (`Seed.cs:1162-1165`), ist also ungeordnet. Sechzehn zeichengleiche
Karten, jede verlangt ein bestimmtes Bundesland, und nichts sagt welches.

Ein zweiter, kleinerer Befund aus demselben Lauf: Ist `Instruction` nicht gesetzt (sie ist optional,
`ExerciseConfigs.cs:210-211`), ist der `prompt` der **leere String** — eine Karte ohne jeden Text.

## Warum das nicht [B-76](B-76-lueckentext-karte-ohne-luecke.md) ist

Beim Lückentext macht ein „welche Lücke ist gemeint" die Karte lösbar. Bei einer ungeordneten Liste gibt
es kein „welche" — jede Antwort ist gleich richtig, solange sie noch nicht dran war. Ein Etikett
„3 von 16" würde also nichts reparieren. Der Fehler sitzt nicht in der Adressierung, sondern im
Kartenmodell: eine Menge wird in Einzelkarten zerlegt, die es nicht gibt.

## Zu prüfen beim Ausformulieren

- Ob eine ungeordnete Liste überhaupt in den Leitner-Pfad gehört. Eine Alternative wäre **eine** Karte mit
  N Antwortfeldern — was aber Leitner je Eintrag (`PositionItemProgress`) unterläuft und mit dem
  Kartenmodell der Ausspielung bricht.
- Ob `Ordered = true` das Problem für sich nimmt: Dann ist „der dritte Eintrag" wohldefiniert, und die
  Story wird zu einem Adressierungs-Fall wie B-76 — aber nur für geordnete Listen.
- Was mit den bestehenden Daten passiert: Die geseedete Liste ist ungeordnet und hat eine 90-%-Pflicht;
  jede Reparatur ändert, wie sie bewertet wird.
- Ob der leere `prompt` gesondert behandelt gehört (Pflichtfeld beim Anlegen?) oder mit der Hauptfrage
  verschwindet.
- Ob die Klausur denselben Weg nimmt — `List` ist `ExerciseCheckMode.CatalogCheck`, also **kein**
  Abschlusstest; zu prüfen ist, was eine solche Position im Abschlusstest überhaupt tut.

## Verlauf

- **2026-08-02** — angelegt aus der Grill-Runde zu B-76, Entscheidung 1. `prio: P1` in Analogie zu B-76
  gesetzt (geseedet, wirkt heute, Wochenpflicht mit 90 %) — nicht vom Nutzer ausdrücklich bestätigt.
  Der Befund selbst ist am laufenden System belegt; `unverifiziert` steht, weil die Story als Ganzes noch
  nicht ausformuliert ist.
