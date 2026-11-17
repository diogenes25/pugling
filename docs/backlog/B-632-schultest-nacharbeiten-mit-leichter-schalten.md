---
tags: [typ/story, status/gegrillt, bereich/klassenarbeit, rolle/student, rolle/supervisor, lerntechnik/vokabeln]
aliases: [Schultest nacharbeiten, Nacharbeit, leichter schalten, Hilfe-Leiter]
status: gegrillt
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

## Offene Punkte

1. ~~Wie wird ein ganzer Satz bewertet?~~ → Entscheidung 1. „I fell asleep during the film / the movie“: Hinterlegt der Vater die
   erlaubten Varianten selbst (wie `TranslationItem.Alternatives`), oder wird nur das unterstrichene Zielwort
   frei getippt, in einem vorgegebenen englischen Satz (wie die Lücke beim Lückentext)? *Empfehlung:* Satz mit
   Lücke für das Zielwort. Dann bleibt die Prüfung eindeutig, und die Leiter aus B-599 passt fast unverändert.
   Kosten: Der Vater muss für jede Satz-Aufgabe einen englischen Satz schreiben. Theodor übt dann den Satzrahmen
   (Zeitform, Artikel) nicht mit, an dem er im Test auch scheiterte.
2. ~~Buchstabensalat bei mehreren Wörtern?~~ → Entscheidung 2. Heute geht er nur bei einem Wort mit höchstens 12 Zeichen.
   *Empfehlung:* Bei Wendungen und Sätzen werden die Wörter gemischt, nicht die Buchstaben. Bei einem
   Einzelwort bleibt es beim Buchstabensalat.
3. ~~Wie viel Wahl hat das Kind?~~ → Entscheidung 3. Darf es nur eine Stufe nach der anderen leichter schalten, oder auch direkt
   zur Auswahl springen? *Empfehlung:* nur stufenweise. So trifft jede Hilfe zuerst die kleinste Stufe.
4. ~~Kostet „leichter“ etwas?~~ → Entscheidung 4. Es gibt diese Möglichkeiten: keine Kosten (die Wiederholung ist die Strafe),
   weniger Münzen für diese Aufgabe wie in B-223, oder ein Zähler, den der Vater sieht. *Empfehlung:* keine
   Münzkosten, aber der Vater sieht den Zähler (Akzeptanzkriterium 7). Die Nacharbeit soll ermutigen, und
   B-223 ist noch nicht gebaut.
5. ~~Wie lange gilt die Pflicht, und was passiert, wenn sie nicht erledigt wird?~~ → Entscheidung 5. Gilt sie einmal bis erledigt,
   mit Malus an jedem Tag ohne Fortschritt? Oder täglich eine Runde, bis alle Aufgaben erledigt sind?
   *Empfehlung:* täglich eine Runde. Malus gibt es nur für einen Tag ohne gespielte Runde, und mit der letzten
   frei richtigen Aufgabe endet die Pflicht. Kosten: eine neue Taktung neben `Daily` und `Weekly`.
6. ~~Neuer Übungstyp oder Spielart einer Position?~~ → Entscheidung 6. Die Mechanik (selbst leichter schalten, wiederkommen, bis
   frei richtig) könnte auch für Vokabeln und Lückentexte nützlich sein. *Empfehlung:* eine Einstellung an der
   Position („Nacharbeit“) statt eines Parallel-Typs (Projektregel: kein Parallel-Stack). Getragen würde sie
   zuerst vom Lückentext, dessen Leiter es schon gibt.
7. ~~Woher kommen die Aufgaben?~~ → Entscheidung 7. Die Prompts sind die Sätze der Lehrerin, und sie lehnen sich an die
   Beispielsätze des Buchs an. *Empfehlung:* Die Nacharbeit eines eigenen Tests des Kindes ist eine
   ausdrückliche Ausnahme von der Regel „keine Buchtexte in die App“. Sie bleibt privat beim Kind und landet
   nicht im geteilten Katalog. Das muss der Vater entscheiden.
8. ~~Hängt die Nacharbeit an der `Klassenarbeit`?~~ → Entscheidung 8. Möglich wäre: Klassenarbeit auf „geschrieben“, Note
   eintragen, Nacharbeit daraus anlegen. *Empfehlung:* zuerst ohne diese Verknüpfung. Die Nacharbeit ist eine
   Position im Plan, und die Verknüpfung mit der Klassenarbeit kommt später, wenn sie fehlt.

## Entscheidungen

Gegrillt mit dem Vater am 2026-11-17.

1. **Gemischt nach Art der Aufgabe.** Einzelwörter und Wendungen („Richtung Osten“, „Eintritt“) tippt er ganz. Bei
   einem Satz tippt er nur das unterstrichene Zielwort in einen englischen Satz mit Lücke, und die deutsche Vorgabe
   steht darüber. *Begründung:* Im Test ging es um das Zielwort, die Prüfung bleibt eindeutig, und die Leiter des
   Lückentexts passt. *Kosten:* Den Satzrahmen (Zeitform, Artikel), an dem er im Test auch scheiterte, übt diese
   Nacharbeit nicht. Ein ganzer Satz mit allen Varianten wurde verworfen: Fehlt eine Variante, wird eine richtige
   Antwort als falsch gewertet.
2. **Buchstabensalat je Wort.** Bei einer Lösung aus mehreren Wörtern werden die Buchstaben jedes Wortes gemischt,
   und die Wortgrenzen bleiben stehen wie bei den Buchstabenkästen. Die Grenze von 12 Zeichen gilt je Wort.
   *Begründung:* Das bleibt eine echte Schreibhilfe, und die Leiter hat bei jeder Aufgabe dieselben Stufen.
   *Kosten:* `StageMechanics.IsScramblable` und die Kachelquelle (`LetterTileSource`) müssen mehrere Wörter können.
   Wörter zu mischen statt Buchstaben wurde verworfen, weil das bei *as soon as* fast geschenkt wäre.
3. **Leichter nur stufenweise, nie zurück.** Jeder Klick auf „leichter“ geht genau eine Stufe tiefer:
   Freitext → Buchstabenkästen → Buchstabensalat → Auswahl. *Begründung:* Er nimmt immer zuerst die kleinste
   Hilfe; bei freier Wahl würde der Sprung zur Auswahl zur Gewohnheit. *Kosten:* Die Stufe einer Karte wird zum
   Zustand der Sitzung, nicht mehr nur der Position, und der Server muss sie führen (siehe Akzeptanzkriterium 6).
   „Leichter erst nach einem Fehlversuch“ wurde verworfen, weil es zum Raten zwingt.
4. **Leichter schalten kostet nichts.** Die einzige Folge ist, dass die Aufgabe wiederkommt. Der Vater sieht je
   Aufgabe, wie oft und auf welcher Stufe gelöst wurde. *Begründung:* Die Nacharbeit folgt auf einen Misserfolg;
   kostet Hilfe Münzen, lernt das Kind, Hilfe zu meiden und zu raten. Münzen wirken nur, wenn sie das Kind
   erreichen (B-535). *Kosten:* Der Zähler je Aufgabe und Stufe muss gespeichert und dem Vater gezeigt werden.
   Ein Bonus für Aufgaben ohne Hilfe bleibt als spätere Idee offen.
5. **Täglich eine Runde, bis alles erledigt ist, mit Abschlussbonus.** Eine Runde führt einmal durch alle offenen
   Aufgaben; ganz gespielt erfüllt sie die Tagespflicht und bringt die eingestellten Münzen, auch wenn noch
   Aufgaben offen bleiben. Ein Tag ohne Runde kostet den eingestellten Malus. Ist die letzte Aufgabe frei richtig,
   endet die Pflicht: kein Malus mehr, die Position verschwindet aus der Tagesmission, und einmalig wird ein
   Abschlussbonus gebucht. *Begründung:* Das passt zum Rhythmus der übrigen Pflichten und verteilt das
   Wiederholen auf mehrere Tage. *Kosten:* Eine neue Taktung neben `Daily`/`Weekly` (Vertrag), eine Endbedingung in
   `PositionProgressService` und in der Malus-Abrechnung (`SettleClosedPeriodsAsync`), ein Feld für den
   Abschlussbonus und eine idempotente Buchung. Verworfen wurden „einmal mit Frist“ (lädt zum Aufschieben ein)
   und „täglich alles erledigen“ (ein sehr langer erster Abend).
6. **Eine Einstellung „Nacharbeit“ an der Position, zuerst getragen vom Lückentext.** Ein Einzelwort wird eine
   Lücke ohne Satz mit der deutschen Vorgabe als Hinweis, ein Satz wird ein englischer Satz mit Lücke unter der
   deutschen Vorgabe. *Begründung:* Die Leiter aus B-599 ist erprobt, und die Mechanik wird ein Werkzeug, das sich
   später auch bei Vokabeln einschalten lässt. Ein eigener Übungstyp wäre ein Parallel-Stack. *Kosten:* Jede
   Lücke braucht eine eigene deutsche Vorgabe (neues optionales Feld an `Gap`). Bisher kommt der Hinweis nur aus
   dem Vokabelspeicher und ist dort nur das deutsche Wort.
7. **Keine Ausnahme von der Buchregel.** Die deutschen Vorgaben werden wörtlich aus dem Test übernommen, denn sie
   sind die Aufgaben selbst. Die englischen Rahmensätze werden neu geschrieben, mit demselben Zielwort in derselben
   Form, aber nicht wortgleich mit dem Buch. *Begründung:* Das Kind sieht genau seine Aufgaben, und die Regel
   „keine Buchtexte in die App“ bleibt ohne Ausnahme. *Kosten:* Die englischen Sätze werden für jede Nacharbeit
   von Hand geschrieben.
8. **Keine Verknüpfung mit der `Klassenarbeit`.** Die Nacharbeit ist eine Position im Plan, und ihr Titel nennt
   den Test. *Begründung:* Das Kind lernt dadurch nicht anders, und die Verknüpfung wäre ein zusätzlicher Weg
   durch Oberfläche und Vertrag. *Kosten:* Den Überblick „Test → Nacharbeit → erledigt“ gibt es vorerst nicht. Er
   kommt als eigene Story, wenn er fehlt.

## Akzeptanzkriterien

1. Der Vater legt eine Nacharbeit als Lückentext an. Je Aufgabe gibt es eine deutsche Vorgabe und die englische
   Lösung, bei Sätzen zusätzlich einen englischen Rahmensatz mit Lücke. Er hängt sie als Position mit der
   Einstellung „Nacharbeit“ an den Plan des Kindes.
2. Jede Aufgabe beginnt als Freitext. „Leichter“ geht genau eine Stufe tiefer: Buchstabenkästen, dann
   Buchstabensalat (je Wort gemischt, Wortgrenzen bleiben), dann Auswahl. Wieder schwerer schalten lässt sich nicht.
3. Eine Antwort auf einer leichteren Stufe zeigt, ob sie richtig war, und die Lösung. Erledigt ist die Aufgabe damit
   nicht.
4. Eine Runde geht der Reihe nach durch alle offenen Aufgaben. Ganz gespielt erfüllt sie die Tagespflicht und bringt
   die eingestellten Münzen. Ein Tag ohne Runde kostet den eingestellten Malus.
5. Ist jede Aufgabe einmal frei getippt richtig, wird einmalig der Abschlussbonus gebucht. Danach verschwindet die
   Position aus der Tagesmission und erzeugt keinen Malus mehr.
6. Der Server führt die Stufe jeder Karte. Eine Antwort zu einer leichter geschalteten Karte erledigt die Aufgabe
   nie, auch wenn der Client sie als Freitext schickt. Ein Integrationstest belegt das.
7. Der Vater sieht je Aufgabe, wie oft sie auf welcher Stufe gelöst wurde und ob sie erledigt ist.
8. Leichter schalten kostet keine Münzen.

## Verlauf

- **2026-11-17** — aufgenommen (`idee`) aus dem Chat mit dem Vater zu Theodors Test.
- **2026-11-17** — ausformuliert: Ist-Stand am Code (`9629cf18`) zeigt drei echte Lücken. Die Stufe wählt
  bewusst nur der Server; eine Karte kommt nur als Lernkarte und nur einmal wieder; als Taktung gibt es nur
  täglich und wöchentlich. Die Leiter selbst gibt es beim Lückentext schon (B-599). Acht offene Punkte mit
  Empfehlung.
- **2026-11-17** — gegrillt mit dem Vater: acht Entscheidungen (alle wie empfohlen, dazu ein Abschlussbonus), Akzeptanzkriterien final.
