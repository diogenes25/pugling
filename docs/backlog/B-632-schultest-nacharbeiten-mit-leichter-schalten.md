---
tags: [typ/story, status/ausformuliert, bereich/klassenarbeit, rolle/student, rolle/supervisor, lerntechnik/vokabeln]
aliases: [Schultest nacharbeiten, Nacharbeit, leichter schalten, Hilfe-Leiter]
status: ausformuliert
prio: P1
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Chat mit dem Vater, 2026-11-17, zu Theodors Vokabeltest Unit 1 (9/20), Auswertung in raw/english/real_tests/2026-11-09-theodor-vokabeltest.md (gitignored)"
grund: ""
ersetzt_durch: []
---

# B-632 · Einen geschriebenen Schultest als Pflicht nacharbeiten, mit „leichter" je Aufgabe

Theodor hat seinen Vokabeltest zu Unit 1 mit 9 von 20 Punkten geschrieben. Der Vater will, dass er **genau
diese Arbeit** als Pflicht nacharbeitet. Die Übung besteht aus den 20 Aufgaben des Tests. Zu jeder steht die
deutsche Vorgabe da, und Theodor tippt die englische Lösung frei, also ein Wort, eine Wendung oder einen ganzen
Satz. Kommt er nicht weiter, kann er **bei jeder Aufgabe selbst „leichter" wählen**, in festen Stufen:
Freitext → Buchstabenkästen → durchgewürfelte Buchstaben → Auswahl zwischen möglichen Lösungen.
Die leichteren Stufen helfen nur. **Erledigt ist eine Aufgabe erst, wenn er sie frei getippt richtig
hat.** Eine Aufgabe, die er mit Hilfe gelöst hat, kommt wieder. Der Ablauf geht in Runden: Jede Runde führt der
Reihe nach durch alle noch offenen Aufgaben. Ein Beispiel: Aufgabe 1 („Richtung Osten") löst er über die Auswahl,
Aufgabe 2 („Westeuropa") tippt er frei richtig. Dann ist Aufgabe 2 erledigt, Aufgabe 1 kommt in der nächsten
Runde wieder. Die Pflicht ist geschafft, wenn jede Aufgabe einmal frei getippt richtig war.

## User Story

Als *Vater* möchte ich einen Test, den mein Sohn in der Schule schlecht geschrieben hat, als Übung mit genau
diesen Aufgaben in seinen Plan legen. Die Übung ist eine Pflicht, die erst erledigt ist, wenn er jede Aufgabe frei
getippt richtig hat. So kann er sich nicht durchklicken, und er arbeitet die Arbeit nach, die die Schule
bewertet hat.

Als *Sohn* möchte ich bei einer Aufgabe, an der ich hänge, selbst eine leichtere Form wählen können. So komme ich
weiter, statt festzustecken, und sehe die richtige Lösung. Die Aufgabe kommt dann wieder, bis ich sie allein
schreibe.

## Ist-Stand am Code

Gelesen am 2026-11-17 auf `main` (`9629cf18`).

**Die Stufe wählt der Server, nie das Kind.** Diese Regel ist bewusst gesetzt. Die Story bricht sie, also ist die
Ausnahme hier eine Produktentscheidung.

- `PositionPracticeController.Review` setzt die Stufe auf dem Server durch. Der Kommentar dazu lautet
  „Enforce the stage server-side (not selectable by the client)“ (`backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:437`).
  Bewertet wird die Karte in der Stufe, in der sie gezeigt wurde: „so the client cannot pick its form“
  (`…PositionPracticeController.cs:441-442`).
- Die Stufe einer Karte kommt aus Tagesstufe, Leitner-Fach und Kalender (`PositionPlayService.CardStage`,
  `backend/Pugling.Api/Services/Shared/PositionPlayService.cs:180-193`). Eine Anfrage des Kindes gibt es nicht.

**Die Leiter, die der Vater beschreibt, gibt es schon, aber je Übungstyp und nur vom Server gestuft.**

- Beim **Lückentext** gibt es seit B-599 Übersetzung + Freitext, Buchstabenkästen, Buchstabensalat und Auswahl
  je Lücke (`ClozeStage.TranslationFreeText`, `LetterBoxes`, `LetterScramble`, `Choice`;
  `backend/Pugling.Api/Models/ClozeEntities.cs:16`, `:25`, `:30`, `:35`).
- Bei der **Vokabel** gibt es Buchstabenkästen, Freitext, Auswahl und Buchstabensalat (`TestStage`,
  `backend/Pugling.Api/Models/StudyPlanEntities.cs:44-56`).
- **Buchstabensalat** funktioniert nur bei einem einzelnen Wort aus 2–12 Buchstaben oder Ziffern
  (`StageMechanics.IsScramblable`, `backend/Pugling.Api/Services/Shared/StageMechanics.cs:107-108`,
  `MaxScrambleLength = 12` in `:100`). Bei einem Satz ist er also gar nicht vorgesehen; solche Karten werden heute
  als Buchstabenkästen gespielt (`StudyPlanEntities.cs:51-54`).

**Ein Typ für „deutsche Vorgabe → englischer Satz“ existiert, ist aber nicht prüfbar.**

- `TranslationExerciseType` heißt „Übersetzung“ und kennt Sätze mit erwarteter Übersetzung und
  `Alternatives` (`backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs:124-138`,
  `TranslationItem` in `backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs:178`). Sein Prüfmodus ist aber
  `ExerciseCheckMode.None` (`BuiltInExerciseTypes.cs:132`). Er hat **keine Stufen**: Die Grundklasse liefert
  leere `StageOptions` und ist auf jeder Stufe getippt (`backend/Pugling.Api/Exercises/ExerciseTypeBase.cs:43`, `:83`).
- `Regelaufgaben` (`GrammarTask` aus Vorgabe, Antwort und Regel-Tipp) hat ebenfalls den Prüfmodus `None`
  (`BuiltInExerciseTypes.cs:102-112`).

**Eine Karte kommt in der Runde nur als Lernkarte wieder, und dann höchstens einmal.**

- Bei einer Lernkarte (B-598) wird eine falsche oder aufgegebene Karte einmal hinten an die eingefrorene
  Reihenfolge angehängt (`…PositionPracticeController.cs:694-695`, Erkennung `IsRepeat` in `:141`). Für andere
  Karten gibt es kein „kommt wieder, bis frei richtig“.

**Eine Pflicht ist täglich oder wöchentlich, aber nie „einmal, bis erledigt“.**

- `GoalCadence` kennt nur `None`, `Daily` und `Weekly`
  (`backend/Pugling.Contracts/Common/PlanPositionBaseTypes.cs:7-11`). Eine Nacharbeit, die einmal erledigt ist und
  danach nicht wiederkommt, lässt sich damit nicht abbilden.
- Erfüllt ist eine Pflicht entweder durch einen bestandenen Positions-Test in der Periode oder, bei Typen ohne
  Prüfung, durch eine zu Ende gespielte Lernrunde (`PositionProgressService.IsGoalMetAsync`,
  `backend/Pugling.Api/Services/Shared/PositionProgressService.cs:144-153`; `PlayedRoundMeetsGoalAsync`
  `:165-187`). Gemessen wird dabei der Cursor, nicht wie viele Karten richtig waren.

**Die Klassenarbeit gibt es als Objekt, aber nur für die Vorbereitung.**

- `Klassenarbeit` trägt Datum, Status, Note und zugeordnete Übungen
  (`backend/Pugling.Api/Models/KlassenarbeitEntities.cs:67-99`). Ein Weg „geschrieben → Nacharbeit“ ist nicht
  angelegt.

**Verwandte Stories:**

- [B-223](B-223-gestufte-hinweise-vor-der-antwort.md) (`geschaetzt`): Hinweise, die das Kind anfordert und die
  Punkte kosten.
- [B-599](B-599-lueckentext-eingabearten-wie-vokabeln.md) (`abgenommen`): die Eingabearten des Lückentexts.
- [B-536](B-536-aufbau-uebungen-statt-versagen.md) (`gegrillt`): leichtere Vorstufen als eigene Übungen.
- [B-524](B-524-vokabeltest-wie-in-der-schule.md) (`abgenommen`): das Schulformat bei Friedrich.

## Die echte Lücke

Drei Dinge fehlen, und alle drei sind neu:

1. **Das Kind wählt selbst eine leichtere Stufe.** Das verlangt eine bewusste Ausnahme von der Regel „der Server
   wählt die Stufe“, die gegen Schummeln eingeführt wurde.
2. **Erledigt heißt „frei getippt richtig“, und alles andere kommt so oft wieder, bis es das ist.** Bisher kommt
   nur die Lernkarte wieder, und das nur einmal.
3. **Eine Pflicht, die einmal erledigt wird und dann endet.** Weder täglich noch wöchentlich passt.

Dazu kommt eine Inhaltsfrage: Ganze englische Sätze lassen sich nur prüfen, wenn die erlaubten Varianten
hinterlegt sind.

## Entwurf der Akzeptanzkriterien

1. Der Vater legt eine Nacharbeit mit den Aufgaben eines Tests an (deutsche Vorgabe, englische Lösung,
   erlaubte Varianten) und hängt sie als Pflicht an den Plan des Kindes.
2. Jede Aufgabe beginnt als Freitext. Das Kind kann sie Schritt für Schritt leichter schalten: Buchstabenkästen,
   dann durchgewürfelte Buchstaben, dann Auswahl. Wieder schwerer schalten kann es nicht.
3. Eine Antwort auf einer leichteren Stufe zeigt, ob sie richtig war, und die Lösung. Erledigt ist die Aufgabe
   damit **nicht**.
4. Eine Runde geht der Reihe nach durch alle offenen Aufgaben. Danach beginnt die nächste Runde mit den Aufgaben,
   die noch offen sind.
5. Ist jede Aufgabe einmal frei getippt richtig, ist die Pflicht erfüllt, und die Belohnung wird gebucht. Danach
   ist die Nacharbeit fertig und erzeugt keinen Malus mehr.
6. Der Server lässt sich nicht täuschen. Eine Antwort, die als „frei getippt“ ankommt, aber zu einer leichter
   geschalteten Karte gehört, erledigt die Aufgabe nicht.
7. Der Vater sieht je Aufgabe, wie oft und auf welcher Stufe gelöst wurde.

## Offene Punkte

1. **Wie wird ein ganzer Satz bewertet?** „I fell asleep during the film / the movie“: Hinterlegt der Vater die
   erlaubten Varianten selbst (wie `TranslationItem.Alternatives`), oder wird nur das unterstrichene Zielwort
   frei getippt, in einem vorgegebenen englischen Satz (wie die Lücke beim Lückentext)? *Empfehlung:* Satz mit
   Lücke für das Zielwort. Dann bleibt die Prüfung eindeutig, und die Leiter aus B-599 passt fast unverändert.
   Kosten: Der Vater muss für jede Satz-Aufgabe einen englischen Satz schreiben. Theodor übt dann den Satzrahmen
   (Zeitform, Artikel) nicht mit, an dem er im Test auch scheiterte.
2. **Buchstabensalat bei mehreren Wörtern?** Heute geht er nur bei einem Wort mit höchstens 12 Zeichen.
   *Empfehlung:* Bei Wendungen und Sätzen werden die Wörter gemischt, nicht die Buchstaben. Bei einem
   Einzelwort bleibt es beim Buchstabensalat.
3. **Wie viel Wahl hat das Kind?** Darf es nur eine Stufe nach der anderen leichter schalten, oder auch direkt
   zur Auswahl springen? *Empfehlung:* nur stufenweise. So trifft jede Hilfe zuerst die kleinste Stufe.
4. **Kostet „leichter“ etwas?** Es gibt diese Möglichkeiten: keine Kosten (die Wiederholung ist die Strafe),
   weniger Münzen für diese Aufgabe wie in B-223, oder ein Zähler, den der Vater sieht. *Empfehlung:* keine
   Münzkosten, aber der Vater sieht den Zähler (Akzeptanzkriterium 7). Die Nacharbeit soll ermutigen, und
   B-223 ist noch nicht gebaut.
5. **Wie lange gilt die Pflicht, und was passiert, wenn sie nicht erledigt wird?** Gilt sie einmal bis erledigt,
   mit Malus an jedem Tag ohne Fortschritt? Oder täglich eine Runde, bis alle Aufgaben erledigt sind?
   *Empfehlung:* täglich eine Runde. Malus gibt es nur für einen Tag ohne gespielte Runde, und mit der letzten
   frei richtigen Aufgabe endet die Pflicht. Kosten: eine neue Taktung neben `Daily` und `Weekly`.
6. **Neuer Übungstyp oder Spielart einer Position?** Die Mechanik (selbst leichter schalten, wiederkommen, bis
   frei richtig) könnte auch für Vokabeln und Lückentexte nützlich sein. *Empfehlung:* eine Einstellung an der
   Position („Nacharbeit“) statt eines Parallel-Typs (Projektregel: kein Parallel-Stack). Getragen würde sie
   zuerst vom Lückentext, dessen Leiter es schon gibt.
7. **Woher kommen die Aufgaben?** Die Prompts sind die Sätze der Lehrerin, und sie lehnen sich an die
   Beispielsätze des Buchs an. *Empfehlung:* Die Nacharbeit eines eigenen Tests des Kindes ist eine
   ausdrückliche Ausnahme von der Regel „keine Buchtexte in die App“. Sie bleibt privat beim Kind und landet
   nicht im geteilten Katalog. Das muss der Vater entscheiden.
8. **Hängt die Nacharbeit an der `Klassenarbeit`?** Möglich wäre: Klassenarbeit auf „geschrieben“, Note
   eintragen, Nacharbeit daraus anlegen. *Empfehlung:* zuerst ohne diese Verknüpfung. Die Nacharbeit ist eine
   Position im Plan, und die Verknüpfung mit der Klassenarbeit kommt später, wenn sie fehlt.

## Verlauf

- **2026-11-17** — aufgenommen (`idee`) aus dem Chat mit dem Vater zu Theodors Test.
- **2026-11-17** — ausformuliert: Ist-Stand am Code (`9629cf18`) zeigt drei echte Lücken. Die Stufe wählt
  bewusst nur der Server; eine Karte kommt nur als Lernkarte und nur einmal wieder; als Taktung gibt es nur
  täglich und wöchentlich. Die Leiter selbst gibt es beim Lückentext schon (B-599). Acht offene Punkte mit
  Empfehlung.
