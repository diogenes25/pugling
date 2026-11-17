---
tags: [typ/story, status/gegrillt, bereich/lehrplan, bereich/auswertung, rolle/supervisor, rolle/student]
aliases: [Fächer-Gewichtung, Prozent-Aufteilung der Fächer, Schieberegler Fächer, Stärke aus den Daten]
status: gegrillt
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

## Offene Punkte

Alle in der Grill-Runde vom 2026-11-17 entschieden oder ausdrücklich zurückgestellt.

1. ~~**Wovon sind die Prozente ein Anteil?**~~ → Entscheidung 1.
2. ~~**Ersetzen die Prozente die drei Stufen aus B-615?**~~ → Entscheidung 2.
3. ~~**Wer legt Fächer und Gewichte fest?**~~ → Entscheidung 3.
4. ~~**Woher kommt das Tagesbudget?**~~ → Entscheidung 4.
5. ~~**Was wird aus `NewItemsPerDay` an der Position?**~~ → Entscheidung 5.
6. ~~**Was heißt „die Fächer des Kindes“?**~~ → Entscheidung 6.
7. ~~**Fach mit Anteil, aber ohne aktiven Plan.**~~ → Entscheidung 7.
8. ~~**Wie wird der Verlauf festgehalten?**~~ → Entscheidung 8.
9. ~~**Verschiebung von selbst oder als Vorschlag?**~~ → Entscheidung 9.
10. **Welche Daten speisen den Vorschlag?** **Zurückgestellt** in die Folge-Story
    [B-617](B-617-verteilung-aus-dem-lernstand-vorschlagen.md) (Entscheidung 9). Gehört nicht zum Umfang
    dieser Story.
11. ~~**Rundung.**~~ → Entscheidung 10.
12. ~~**Müssen die Prozentwerte zusammen 100 ergeben?**~~ (Folgefrage aus Punkt 7) → Entscheidung 11.
13. ~~**Position außerhalb der Fächerliste oder in einem Plan ohne Fach.**~~ (Folgefrage aus Punkt 6) →
    Entscheidung 12.

## Entscheidungen

Gegrillt im Chat am 2026-11-17 mit dem Vater, zwölf Fragen einzeln, jeweils die Empfehlung gewählt.

1. **Die Prozente teilen ein Tagesbudget neuer Wörter.** 90 / 10 bei einem Budget von 20 heißt 18 und 2 neue
   Wörter. *Begründung:* `NewItemsPerDay` ist der einzige Dosis-Regler, den es schon gibt, und neue Wörter
   zählt der Server selbst; Minuten meldet der Client und taugen nicht als Pflichtmaß. *Kosten:* Die Verteilung
   wirkt nur auf Vokabel-Positionen. Grammatik und andere Übungen ohne Tagesdosis bleiben unberührt, ein Fach
   ohne Vokabeln (Mathe) lässt sich so nicht gewichten.
2. **Die Prozente ersetzen die drei Stufen aus B-615.** Ein kleiner Anteil ist „stark“, ein großer „schwach“.
   Ein Fach mit **0 %** verlangt nur die fälligen Wiederholungen; ist nichts fällig, gilt seine Pflicht als
   erfüllt, kostet keinen Malus und zählt für Truhe und Serie. Ab 1 % bleibt das Pflichtziel der Position, wie
   der Vater es gesetzt hat. *Begründung:* ein Mechanismus statt zwei, die sich widersprechen könnten.
   *Kosten:* In B-615 sind die Entscheidungen 4 und 10 damit abgelöst und sein Teil (b) geht hier auf. Drei
   Knöpfe wären einfacher zu bedienen gewesen als Regler. Und zwischen 0 % und 1 % liegt ein Sprung: Erst bei
   genau 0 % entfällt die Pflicht auf neue Wörter.
3. **Nur der Vater setzt Fächer und Prozentwerte, das Kind liest.** Schreiben ist auf die Rolle `Supervisor`
   gegated. *Begründung:* Das Kind soll ein ungeliebtes Fach nicht kleinrechnen können. *Kosten:* keine
   Mitsprache des Kindes; der Vater pflegt die Verteilung allein.
4. **Das Tagesbudget ist ein Wert je Kind, vom Vater gesetzt.** Ohne gesetztes Budget verteilt der Server
   nichts, und alles verhält sich wie heute. *Begründung:* eine klare Vorgabe statt eines Nebenprodukts der
   Positionen. *Kosten:* ein neues Feld am Kind (Migration an die Kette) und ein Wert mehr, den der Vater
   einstellen muss.
5. **Der Server verteilt den Anteil eines Fachs gleichmäßig auf dessen Vokabel-Positionen.** Ein gesetztes
   `NewItemsPerDay` bleibt die Obergrenze der einzelnen Position. *Begründung:* Der Vater stellt nur noch die
   Prozente; der Feinregler je Position bleibt erhalten. *Kosten:* Kappt `NewItemsPerDay` eine Position, wird
   der Anteil des Fachs nicht ausgeschöpft. Bei gleichmäßiger Verteilung laufen mehrere Lektionen eines Fachs
   nebeneinander statt nacheinander. `DailyDoseInForce` im Vertrag muss die verteilte Dosis ausweisen.
6. **Die Fächer des Kindes sind eine eigene Liste Kind × Fach**, dieselbe Tabelle, die den Prozentwert trägt
   (B-615 Entscheidung 3). *Begründung:* Die Verteilung kippt nicht, wenn ein Plan endet oder dazukommt.
   *Kosten:* eine neue Tabelle mit Migration, und eine fünfte Stelle, an der ein Fach am Kind hängt (neben
   Lehrwerk, Stundenplan, Plan und Klassenarbeit); der Vater pflegt sie von Hand.
7. **Ein Anteil ohne aktiven Plan verfällt, und das Vater-Web warnt.** Die übrigen Fächer bekommen genau ihren
   gesetzten Anteil, es wird nicht umverteilt. *Begründung:* Die Verteilung bleibt, wie der Vater sie gesetzt
   hat. *Kosten:* Das Budget wird dann nicht ausgeschöpft, bis der Vater einen Plan anlegt oder die Verteilung
   ändert.
8. **Jede Änderung der Verteilung legt einen datierten Stand ab.** Der Vater liest die Folge der Stände.
   *Begründung:* klein, und zeigt genau die Verschiebungen. *Kosten:* eine zweite Tabelle für die Stände; eine
   lückenlose Zeitreihe je Tag gibt es nicht, ein Diagramm muss zwischen den Ständen treppenförmig zeichnen.
9. **Die Verschiebung aus dem Lernstand ist ein Vorschlag, den der Vater bestätigt, und eine eigene
   Folge-Story** ([B-617](B-617-verteilung-aus-dem-lernstand-vorschlagen.md)). Diese Story baut nur die von
   Hand gesetzte Verteilung. *Begründung:* Die Pflicht des Kindes ändert sich nie ohne den Vater, und die
   Grundlage wartet nicht auf die Vorschlagslogik. *Kosten:* Bis B-617 gebaut ist, verschiebt sich nichts von
   selbst; der Satz „wird das Kind besser, verschiebt sich die Aufteilung“ aus der Idee ist hier nicht erfüllt.
10. **Rundung: abrunden, der Rest geht an das Fach mit dem größten Anteil.** 90 / 10 von 15 ergibt 14 und 1.
    Bei gleich großen Anteilen entscheidet die kleinere Fach-Id. *Begründung:* Die Summe trifft das Budget
    immer genau. *Kosten:* Ein sehr kleiner Anteil kann durch die Rundung auf 0 neue Wörter fallen, obwohl er
    nicht 0 % ist; das Pflichtziel der Position bleibt dann trotzdem bestehen (Entscheidung 2).
11. **Die Prozentwerte eines Kindes ergeben zusammen genau 100.** Der Server weist jede andere Summe ab, die
    Regler im Vater-Web halten die Summe selbst. *Begründung:* eindeutig, das ganze Budget ist immer vergeben.
    *Kosten:* Die Liste wird immer als Ganzes geschrieben, nie ein Fach allein; „heute insgesamt weniger“ geht
    nur über das Budget.
12. **Eine Vokabel-Position außerhalb der Fächerliste oder in einem Plan ohne Fach läuft wie heute.** Sie führt
    neue Wörter nach ihrem eigenen `NewItemsPerDay` ein und zählt nicht ins Budget; das Vater-Web zeigt solche
    Positionen an. *Begründung:* Bestehende Pläne ändern ihr Verhalten nicht, bis der Vater das Fach in die
    Liste nimmt. *Kosten:* Das Budget ist keine harte Tagesgrenze, solange solche Positionen laufen. Das löst
    B-615 Entscheidung 9 für die Stärke ab (dort „rechnet mit `normal`“).

## Akzeptanzkriterien

Final. Je Fall die Änderung, die seinen Test rot macht.

- **AK 1.** Der Vater schreibt je Kind eine Fächerliste mit Prozentwerten und liest sie zurück. Eine Summe
  ungleich 100, ein doppeltes Fach oder ein Wert außerhalb 0 bis 100 ⇒ `400 validation_error`. *Rot ohne
  Summenprüfung.*
- **AK 2.** Ein fremdes Kind liefert 404. Das Kind selbst liest die Verteilung, ein Schreibversuch mit
  Student-Token liefert 403. *Rot ohne Eigentumsprüfung bzw. wenn der Student schreiben darf.*
- **AK 3.** Der Vater setzt das Tagesbudget am Kind und liest es zurück. Ohne Budget führt jede Position neue
  Wörter nach ihrem `NewItemsPerDay` ein, auch wenn eine Fächerliste gesetzt ist. *Rot, wenn die Verteilung
  ohne Budget greift.*
- **AK 4.** Budget 20, Verteilung 90 / 10, je Fach eine Vokabel-Position ⇒ der Tag führt 18 und 2 neue Wörter
  ein, und `DailyDoseInForce` weist 18 und 2 aus. *Rot, wenn die Verteilung nicht in die Tagesdosis eingeht.*
- **AK 5.** Budget 15, Verteilung 90 / 10 ⇒ 14 und 1. *Rot bei kaufmännischer Rundung (14 und 2) oder wenn der
  Rest verfällt (13 und 1).*
- **AK 6.** Ein Fach mit Anteil 18 und zwei Vokabel-Positionen ⇒ 9 und 9. Trägt eine davon
  `NewItemsPerDay = 5` ⇒ 5 und 9. *Rot, wenn die Kappe der Position überschritten oder der Rest auf die andere
  Position geschoben wird.*
- **AK 7.** Ein Fach mit 0 % und fälligen Karten: Die Tagespflicht ist mit den Wiederholungen erfüllt, es werden
  keine neuen Wörter eingeführt. *Rot, wenn die Pflicht weiter neue Wörter verlangt.*
- **AK 8.** Ein Fach mit 0 % und null fälligen Karten: Die Tagessicht meldet die Pflicht als erfüllt, der
  Tagesabschluss bucht keinen Malus. *Rot, wenn die Pflicht offen bleibt oder Malus kostet.*
- **AK 9.** Ein Fach mit 10 % ohne aktiven Plan: Die übrigen Fächer bekommen genau ihren Anteil (bei Budget 20
  und 70 / 20 / 10 also 14 und 4, nicht 16 und 4), und die Vater-Sicht des Kindes meldet den leeren Anteil.
  *Rot bei Umverteilung bzw. ohne Meldung.*
- **AK 10.** Eine Vokabel-Position, deren Fach nicht in der Liste steht, und eine in einem Plan ohne Fach führen
  neue Wörter nach ihrem `NewItemsPerDay` ein; die Anteile der gelisteten Fächer ändern sich dadurch nicht.
  *Rot, wenn sie 0 neue Wörter bekommt oder das Budget der anderen schmälert.*
- **AK 11.** Eine Position ohne Tagesdosis (Grammatik) verlangt bei jedem Prozentwert ihres Fachs über 0
  dasselbe. *Rot, wenn der Prozentwert ihr Ziel verändert.*
- **AK 12.** Zwei Änderungen der Verteilung ⇒ der Vater liest zwei datierte Stände in zeitlicher Folge, der
  ältere unverändert. Ein Speichern ohne Änderung legt keinen Stand ab. *Rot, wenn eine Änderung den alten
  Stand überschreibt bzw. jedes Speichern einen Stand erzeugt.*
- **AK 13.** Das Vater-Web zeigt am Kind Budget und Verteilung mit Reglern, die zusammen 100 halten, speichert
  sie und zeigt die gespeicherten Werte nach dem Neuladen. *Rot, wenn die Seite die Werte nicht rendert.*
- **AK 14.** Die Sohn-App zeigt die Verteilung lesend. *Rot, wenn sie fehlt oder editierbar ist.*

## Verlauf

- **2026-11-17** — angelegt aus dem Chat mit dem Vater.
- **2026-11-17** — **Entscheidung des Vaters** (B-615 Entscheidung 14): Teil (b) von B-615 (Stärke je Fach) wird erst geschätzt, wenn diese Story gegrillt ist, damit die Tabelle Kind × Fach nur eine Migration braucht. Diese Story blockiert also B-615 Teil (b).
- **2026-11-17** — ausformuliert gegen `main` `aca3bade`: Es gibt weder eine Fächerliste des Kindes noch ein
  Tagesbudget, von dem ein Prozentwert ein Anteil sein könnte. Trefferquote und Beherrschung je Fach liegen
  fertig vor (`MasteryRollup`), die Antwortzeit wird nicht gespeichert, ein Verlauf über die Zeit hat keinen
  Träger. Elf offene Punkte, die ersten beiden (Bezugsgröße, Verhältnis zu den drei Stufen) tragen den Rest.
- **2026-11-17** — gegrillt im Chat mit dem Vater: zwölf Entscheidungen, jeweils die Empfehlung; AKs final.
  Die Prozente ersetzen die drei Stufen aus B-615 (dort Entscheidungen 4 und 10 abgelöst, Teil (b) geht hier
  auf). Der Vorschlag aus dem Lernstand ist nach [B-617](B-617-verteilung-aus-dem-lernstand-vorschlagen.md)
  ausgelagert.
