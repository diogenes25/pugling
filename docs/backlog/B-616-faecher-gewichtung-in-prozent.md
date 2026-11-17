---
tags: [typ/story, status/ausformuliert, bereich/lehrplan, bereich/auswertung, rolle/supervisor, rolle/student]
aliases: [Fächer-Gewichtung, Prozent-Aufteilung der Fächer, Schieberegler Fächer, Stärke aus den Daten]
status: ausformuliert
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Vater im Chat 2026-11-17, nach dem Grillen von B-615"
grund: ""
ersetzt_durch: []
---

# B-616 · Die Fächer eines Kindes werden prozentual gewichtet und verschieben sich mit dem Lernstand

Je Kind wird festgelegt, welche Fächer es hat bzw. üben will, und wie sich die tägliche Übung prozentual auf
diese Fächer verteilt, etwa mit Schiebereglern. Beispiel Friedrich: Französisch ist miserabel, Englisch sehr
gut, also 90 % Französisch zu 10 % Englisch; kommt Mathe dazu, vielleicht 70 / 20 / 10. Wird das Kind in
einem Fach besser, verschiebt sich die Aufteilung. Die Verteilung wird über die Zeit festgehalten. Später
soll Pugling aus den Übungen selbst erkennen, wo Stärken und Schwächen liegen, zum Beispiel aus Trefferquote
und Antwortzeit, und die Aufteilung daraus vorschlagen.

Offen schon beim Aufnehmen: Wer legt Fächer und Gewichte fest, der Vater oder das Kind? (Legt das Kind fest,
kann es ein ungeliebtes Fach kleinrechnen.) Das Verhältnis zu
[B-615](B-615-tagesmission-faechermix-nach-staerke-und-stundenplan.md) ist zu klären: Dort sind am
2026-11-17 **drei Stufen** je Fach entschieden (Entscheidung 4: `schwach`/`normal`/`stark` mit Dosis-Faktor),
die Prozent-Aufteilung wäre eine feinere Alternative dazu. Eine datengetriebene Stärke ist dort als
Folge-Story vorgesehen (Entscheidung 8); diese Idee ist ein Kandidat dafür. Ebenso offen: Worauf beziehen
sich die Prozente (Zahl der Pflichten, neue Wörter, Zeit), und wie passen sie zur Obergrenze aus
B-615 Entscheidung 7?

## User Story

Als *Vater* möchte ich je Kind festlegen, welche Fächer es übt und wie sich die tägliche Übung prozentual auf
sie verteilt, und sehen, wie sich diese Verteilung über die Zeit verschoben hat, damit das schwache Fach den
größten Teil der Übung bekommt, ohne dass ich jede Position einzeln nachstelle. Als *Sohn* möchte ich sehen,
wie meine Übung auf die Fächer verteilt ist, damit ich verstehe, warum ich heute mehr Französisch als Englisch
bekomme.

## Ist-Stand am Code

Gemessen am 2026-11-17 auf `main` (Stand `aca3bade`), Pfade relativ zu `backend/Pugling.Api/`.

### Die Fächer eines Kindes: nirgends festgelegt, nur abgeleitet

- Es gibt **keine Zuordnung Kind × Fach**. `Child` (`Models/AdminEntities.cs:61`) trägt Klasse, Schulart,
  Interessen und `BonusDailyCoinCap`, aber keine Fächerliste und kein Gewicht.
- Ein Fach erscheint beim Kind nur mittelbar, an vier Stellen: am Lehrwerk des Kindes (`Textbook.SubjectId`,
  optional, `Models/AdminEntities.cs:159`), am Stundenplan (`TimetableEntry.SubjectId`,
  `Models/TimetableEntities.cs:12`), am Plan (`StudyPlan.SubjectId`, im Vertrag Pflicht, in der Spalte nullable,
  `Models/StudyPlanEntities.cs:83`) und an der Klassenarbeit (`Models/KlassenarbeitEntities.cs:73`).
- Die Lernstand-Sicht leitet „die Fächer des Kindes“ aus den Übungen ab, die dem Kind je zugewiesen waren
  (`Services/Student/ChildLearnProgressService.cs:50` `LoadRelevantAsync`, `:160` `SubjectsAsync`). Ein Fach
  ohne zugewiesene Übung kommt dort nicht vor.

### Die tägliche Menge: je Position, ohne Budget über die Fächer

- Wie viel ein Kind täglich tun muss, ergibt sich allein aus den Positionen: Taktung, Ziel und
  `NewItemsPerDay` (`Models/PlanPositionEntities.cs:40`, 0 bis 200, 0 = keine Kappe;
  `../Pugling.Contracts/Supervisor/StudyPlanDtos.cs:205`). Die wirksame Dosis rechnet
  `Services/Shared/PositionPlayService.cs:39-41` je Position.
- Die Tagessicht summiert die Pflichten aller aktiven Pläne (`Services/Shared/PositionProgressService.cs:295`
  `ComputeChildDayAsync`), kennt aber **kein Tagesbudget** und keine Aufteilung. Es gibt also nichts, wovon ein
  Prozentwert ein Anteil sein könnte.
- Die Obergrenze „höchstens N Pflicht-Positionen pro Tag“ ist in B-615 entschieden (Entscheidung 7), aber
  noch nicht gebaut.

### Daten für eine Stärke aus den Übungen: Trefferquote ja, Antwortzeit nein

- **Trefferquote und Beherrschung je Fach liegen fertig vor.** `MasteryRollup`
  (`../Pugling.Contracts/Student/LearnProgressDtos.cs:11`) trägt `CorrectPercent`, `AvgMasteryPercent`,
  `WeakItems`, `IntroducedItems` und `LastActivityAt`; `SubjectProgressResponse` (`:16`) liefert das je Fach
  unter `student/children/{childId}/learn/subjects`
  (`Controllers/Student/ChildLearnProgressController.cs:19`, `:31`). Die Zahlen decken **nur Vokabel-Items** ab.
- **Die Antwortzeit wird nicht gespeichert.** Der Server rechnet sie beim Antworten für Combo und Tempo-Bonus
  aus (`Controllers/Student/PositionPracticeController.cs:542`), schreibt aber nur Richtig/Falsch und den
  Zeitpunkt weg (`ReviewEvent`, `Models/StudyPlanEntities.cs:146-152`; `ItemReviewEvent`,
  `Models/ItemProgressEntities.cs:61`). Gespeichert ist nur die aktive Übungszeit je Sitzung
  (`PracticeSession.ActiveSeconds`, `Models/StudyPlanEntities.cs:112`), die der Client per Heartbeat meldet.
- **Noten** tragen die Klassenarbeiten je Fach (`Models/KlassenarbeitEntities.cs:73`, `:82`).

### Verlauf über die Zeit: nicht vorhanden

- Es gibt keine Tabelle, die einen Stand über die Zeit festhält; die Suche nach `Snapshot`- oder
  `History`-Klassen in `Models/` findet nichts. Der Lernstand ist immer der heutige.

### Oberfläche

- Im Frontend gibt es keinen Schieberegler; die Suche nach `type="range"` unter `frontend/src` findet nichts.

### Verwandte Stories

- [B-615](B-615-tagesmission-faechermix-nach-staerke-und-stundenplan.md) (`gegrillt`): Entscheidung 3 legt die
  Stärke an Kind × Fach, Entscheidung 4 gibt ihr drei Stufen, Entscheidung 8 lässt sie zuerst nur den Vater
  setzen, Entscheidung 10 regelt `stark` ohne fällige Karten. Entscheidung 14: Teil (b) wird erst geschätzt,
  wenn diese Story gegrillt ist. Die Entscheidungen 3, 4, 8 und 10 stehen hier also wieder zur Wahl.
- [B-536](B-536-aufbau-uebungen-statt-versagen.md) (`gegrillt`): Ein schwaches Fach braucht leichtere Übungen,
  nicht nur mehr.

## Die echte Lücke

1. **Es fehlt die Bezugsgröße.** Ein Prozentwert braucht ein Ganzes. Heute gibt es kein Tagesbudget über die
   Fächer, nur einzelne Positionen mit eigener Dosis. Ohne die Entscheidung, *wovon* die Prozente ein Anteil
   sind, ist „90 zu 10“ nicht baubar.
2. **Es fehlt die Fächerliste des Kindes.** Sie existiert nur abgeleitet und an vier Stellen verschieden.
3. **Zwei Mechaniken für dieselbe Sache.** Die drei Stufen aus B-615 und die Prozente steuern beide, wie viel
   ein Fach bekommt. Beide zu bauen heißt, dass sie sich widersprechen können.
4. **Der Verlauf** der Verteilung über die Zeit hat keinen Träger.
5. **Die Stärke aus den Daten** hat für Vokabeln eine fertige Grundlage (Trefferquote, Beherrschung), für die
   Antwortzeit keine, und für Nicht-Vokabel-Übungen gar keine.

## Entwurf der Akzeptanzkriterien

Vorläufig. Sie setzen die Empfehlungen zu den offenen Punkten voraus und ändern sich mit ihnen.

1. Der Vater legt je Kind eine Fächerliste mit Prozentwerten an, die zusammen 100 ergeben. Eine Liste mit
   anderer Summe ⇒ `400 validation_error`. *Rot ohne Summenprüfung.*
2. Ein fremdes Kind liefert 404; das Kind selbst liest die Verteilung, kann sie aber nicht schreiben (403).
   *Rot ohne Eigentumsprüfung bzw. wenn der Student schreiben darf.*
3. Bei einem Tagesbudget von 20 neuen Wörtern und 90 / 10 führt der Tag 18 neue Wörter im ersten und 2 im
   zweiten Fach ein. *Rot, wenn die Verteilung nicht in die Tagesdosis eingeht.*
4. Fällige Wiederholungen bleiben unabhängig vom Prozentwert Pflicht, auch bei 0 %. *Rot, wenn ein Fach mit
   0 % keine Wiederholungen mehr verlangt.*
5. Jede Änderung der Verteilung legt einen datierten Stand ab; der Vater liest die Folge der Stände je Kind.
   *Rot, wenn eine Änderung den alten Stand überschreibt.*
6. Das Vater-Web zeigt die Verteilung am Kind mit Reglern, die zusammen 100 halten, und speichert sie.
   *Rot, wenn die Seite die gespeicherten Werte nicht rendert.*

## Offene Punkte

Jeweils mit meiner Empfehlung. Die Punkte 1 und 2 entscheiden über alles Weitere.

1. **Wovon sind die Prozente ein Anteil?** (a) Von einem **Tagesbudget neuer Wörter** je Kind, (b) von der Zahl
   der Pflicht-Positionen pro Tag (Obergrenze aus B-615 Entscheidung 7), (c) von einer Übungszeit in Minuten.
   *Empfehlung:* (a). `NewItemsPerDay` ist der einzige Dosis-Regler, den es schon gibt, und neue Wörter lassen
   sich zählen. Minuten meldet der Client selbst, sie taugen nicht als Pflichtmaß. Folge: Die Prozente wirken
   nur auf Vokabel-Positionen.
2. **Ersetzen die Prozente die drei Stufen aus B-615?** *Empfehlung:* ja. Ein Mechanismus statt zwei: Ein
   kleiner Anteil ist „stark“, ein großer „schwach“, und 0 % heißt „nur fällige Wiederholungen“ (das deckt
   B-615 Entscheidung 10 ab). Dann werden die Entscheidungen 4 und 10 dort durch diese Story ersetzt, und
   Teil (b) von B-615 geht hier auf. Kosten: B-615 muss nachgezogen werden, und die Stufen waren einfacher
   zu bedienen.
3. **Wer legt Fächer und Gewichte fest?** *Empfehlung:* nur der Vater, das Kind liest. Legt das Kind fest,
   rechnet es das ungeliebte Fach klein; die Regel des Projekts ist, das serverseitig zu verhindern.
4. **Woher kommt das Tagesbudget?** Ein fester Wert je Kind, vom Vater gesetzt, oder die Summe der heutigen
   `NewItemsPerDay` aller Positionen? *Empfehlung:* ein Wert je Kind. Die Summe der Positionen wäre keine
   Vorgabe, sondern ein Nebenprodukt.
5. **Was wird aus `NewItemsPerDay` an der Position?** Bleibt es als Kappe je Position bestehen, oder verteilt
   der Server den Anteil des Fachs selbst auf dessen Positionen? *Empfehlung:* Der Server verteilt gleichmäßig
   auf die Vokabel-Positionen des Fachs; `NewItemsPerDay` bleibt als Obergrenze je Position, wenn gesetzt.
6. **Was heißt „die Fächer des Kindes“?** Eine eigene Liste (Kind × Fach, dieselbe Tabelle wie die Gewichte)
   oder weiter abgeleitet aus Plänen und Lehrwerken? *Empfehlung:* eigene Liste. Sie ist die Tabelle, die
   B-615 Entscheidung 3 ohnehin vorsieht.
7. **Fach mit Anteil, aber ohne aktiven Plan.** Der Anteil läuft ins Leere. Abweisen, warnen oder auf die
   anderen Fächer umverteilen? *Empfehlung:* warnen und nicht umverteilen, damit die Verteilung bleibt, wie
   der Vater sie gesetzt hat.
8. **Wie wird der Verlauf festgehalten?** Ein datierter Stand je Änderung oder ein Stand je Tag?
   *Empfehlung:* je Änderung. Das ist klein und zeigt genau die Verschiebungen.
9. **„Wird das Kind besser, verschiebt sich die Aufteilung“: von selbst oder als Vorschlag?** *Empfehlung:*
   als Vorschlag, den der Vater bestätigt, und als **eigene Folge-Story**. Von selbst hieße, dass sich die
   Pflicht ohne Zutun des Vaters ändert.
10. **Welche Daten speisen den Vorschlag?** Trefferquote und Beherrschung je Fach liegen vor; die Antwortzeit
    müsste erst gespeichert werden, und Noten gibt es nur nach Klassenarbeiten. *Empfehlung:* mit
    `CorrectPercent` und `AvgMasteryPercent` beginnen; die Antwortzeit nur speichern, wenn der Vorschlag ohne
    sie zu grob ist. Gehört in die Folge-Story aus Punkt 9.
11. **Rundung.** 90 / 10 von 15 neuen Wörtern sind 13,5 und 1,5. *Empfehlung:* abrunden und den Rest dem Fach
    mit dem größten Anteil geben, damit die Summe das Budget trifft.

## Verlauf

- **2026-11-17** — angelegt aus dem Chat mit dem Vater.
- **2026-11-17** — **Entscheidung des Vaters** (B-615 Entscheidung 14): Teil (b) von B-615 (Stärke je Fach) wird erst geschätzt, wenn diese Story gegrillt ist, damit die Tabelle Kind × Fach nur eine Migration braucht. Diese Story blockiert also B-615 Teil (b).
- **2026-11-17** — ausformuliert gegen `main` `aca3bade`: Es gibt weder eine Fächerliste des Kindes noch ein
  Tagesbudget, von dem ein Prozentwert ein Anteil sein könnte. Trefferquote und Beherrschung je Fach liegen
  fertig vor (`MasteryRollup`), die Antwortzeit wird nicht gespeichert, ein Verlauf über die Zeit hat keinen
  Träger. Elf offene Punkte, die ersten beiden (Bezugsgröße, Verhältnis zu den drei Stufen) tragen den Rest.
