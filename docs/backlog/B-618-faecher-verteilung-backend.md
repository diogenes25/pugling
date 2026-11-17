---
tags: [typ/story, status/geschaetzt, bereich/lehrplan, bereich/auswertung, rolle/supervisor, rolle/student]
aliases: [Fächer-Verteilung Backend, Tagesbudget neuer Wörter, Fächerliste des Kindes]
status: geschaetzt
wartet_auf: "B-644 (Tagesplaner) – nicht bauen, bis B-644 gegrillt ist"
prio: P2
art: Wunsch
groesse: L
wo: backend
migration: ja
vertragsbruch: nein
quelle: B-616
grund: ""
ersetzt_durch: []
---

# B-618 · Die Fächer eines Kindes teilen ein Tagesbudget neuer Wörter in Prozent (Backend)

Teil 1 von zwei aus [B-616](B-616-faecher-gewichtung-in-prozent.md) (dort gegrillt, beim Schätzen geteilt). Der Vater legt je Kind eine
Fächerliste mit Prozentwerten und ein Tagesbudget neuer Wörter fest; der Server verteilt das Budget nach den
Prozenten auf die Vokabel-Positionen der Fächer und hält jede Änderung der Verteilung als datierten Stand fest.
Diese Story baut das per API; die Oberfläche folgt in [B-619](B-619-faecher-verteilung-vater-web-und-sohn-app.md).

## User Story

Als *Vater* möchte ich je Kind festlegen, welche Fächer es übt und wie sich ein Tagesbudget neuer Wörter
prozentual auf sie verteilt, und die Folge dieser Verteilungen über die Zeit lesen, damit das schwache Fach den
größten Teil der neuen Wörter bekommt, ohne dass ich jede Position einzeln nachstelle.

## Ist-Stand am Code

Gemessen am 2026-11-17 auf `main` (Stand `aca3bade`, für B-616), Pfade relativ zu `backend/Pugling.Api/`.

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

### Wo die Tagesdosis heute gerechnet und gelesen wird (gemessen beim Schätzen, Stand `bf5fd841`)

- Die Regel lebt an **einer** Stelle, synchron und je Position: `Services/Shared/PositionPlayService.cs:32`
  (Instanz) und `:38-41` (statisch, „the one place the rule lives“). Sie kennt nur die Position, weder das Kind
  noch die anderen Positionen des Tages.
- **Fünf Stellen lesen sie:** der Rundenaufbau (`Services/Shared/PositionPlayService.cs:271-272`, mit
  `WithinDailyDose` `:282`), die Antwortprüfung (`Controllers/Student/PositionPracticeController.cs:434`), der
  Abschlusstest (`Controllers/Student/PositionTestsController.cs:167`), die Schonung des Einführungstags bei der
  Malus-Abrechnung (`Services/Shared/PositionProgressService.cs:542`) und die Positions-Antwort an den Vater
  (`Controllers/Supervisor/PlanPositionsController.cs:45`, dort in einer Projektion über den statischen Aufruf).
- „Nichts fällig heißt erledigt“ gibt es für eine leere Runde schon
  (`Services/Shared/PositionProgressService.cs:155-166`, `PlayedRoundMeetsGoalAsync`).
- Ein Feld am Kind, das nur der Vater setzt, hat ein Vorbild: `BonusDailyCoinCap`
  (`../Pugling.Contracts/Supervisor/ChildDtos.cs:15`, `:23`, `:36`;
  `Controllers/Supervisor/ChildrenController.cs:25-28`).
- Letztes Glied der Migrationskette: `Data/Migrations/20261117061523_StudyPlanSeriesUnit.cs`.

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

Keine. Alle Punkte sind in [B-616](B-616-faecher-gewichtung-in-prozent.md) entschieden; der dort zurückgestellte Punkt 10 (welche Daten einen
Vorschlag speisen) gehört zu [B-617](B-617-verteilung-aus-dem-lernstand-vorschlagen.md).

## Entscheidungen

Übernommen aus [B-616](B-616-faecher-gewichtung-in-prozent.md) (gegrillt im Chat am 2026-11-17 mit dem Vater, zwölf Fragen einzeln, jeweils die Empfehlung gewählt). Wo eine Entscheidung das Vater-Web nennt, liefert diese Story die Angabe per API; die Anzeige baut [B-619](B-619-faecher-verteilung-vater-web-und-sohn-app.md).

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

Final, übernommen aus B-616 (dort AK 1 bis 12). Je Fall die Änderung, die seinen Test rot macht.

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

## Schätzung

**Größe: L** — wie eine DB-Umbau-Etappe: zwei neue Tabellen und ein Feld mit Migration, dazu ein Eingriff in
die Dosis-Regel, die fünf Stellen lesen, und in die Pflicht-Rechnung. Kein M, weil die Dosis heute je Position
und synchron gerechnet wird und künftig das Kind, seine Fächerliste und alle Vokabel-Positionen des Tages
kennen muss.

- **`migration: ja`** — neue Tabelle Kind × Fach mit Prozentwert, neue Tabelle für die datierten Stände, neues
  Feld für das Tagesbudget am Kind. Angehängt an `20261117061523_StudyPlanSeriesUnit`. Bestandszeilen bleiben
  unberührt: Budget leer, Liste leer.
- **`vertragsbruch: nein`** — alles additiv: neue Endpunkte, ein neues Feld in `ChildResponse` und den
  Schreib-DTOs des Kindes. `DailyDoseInForce` behält seine Form, weist aber künftig die verteilte Dosis aus.
  `Pugling.Client` bekommt die neuen Aufrufe.

### Risiken

1. **Verhalten der echten Kinder.** Die Söhne lernen auf der echten DB. AK 3 ist die Sicherung: ohne gesetztes
   Budget ändert sich nichts. Der Test dazu muss vor allem anderen stehen.
2. **Die Dosis-Regel wird vom Kind abhängig.** Der statische Aufruf in der Projektion
   (`PlanPositionsController.cs:45`) kann die Verteilung nicht kennen. Er braucht die vorab je Kind gerechnete
   Verteilung, sonst zeigt der Vater eine andere Dosis, als das Spiel anwendet (genau die Drift, gegen die
   B-480 die Regel an einen Ort gelegt hat).
3. **Die Verteilung muss innerhalb eines Tages stabil sein.** Sie hängt an der Menge der aktiven
   Vokabel-Positionen. Kommt mittags eine Position dazu, ändern sich die Anteile der anderen; was heute schon
   eingeführt wurde, zählt weiter gegen die Dosis (`WithinDailyDose`). Ein Test mit zweiter Runde am selben Tag
   nach einer Planänderung gehört dazu.
4. **0 % greift in die Pflicht- und Malus-Rechnung.** Das ist der Anti-Cheat-Kern (`PlayedRoundMeetsGoalAsync`,
   Einführungstag-Schonung `:542`, `ClosedPeriods`). Die Regel „nichts fällig heißt erledigt“ darf nicht auf
   Übungen ohne Inhalt durchschlagen (die Unterscheidung in `:155-166` bleibt).
5. **Positionen mit Dosis 0 aus anderem Grund.** Eine Übung, die als Menge wertet, hat schon heute Dosis 0
   (B-478). Sie nimmt an der Verteilung nicht teil und darf den Anteil ihres Fachs nicht schlucken.
6. **Wechselwirkung mit B-615 Teil (c).** Die Stundenplan-Taktung macht eine Position nur an manchen Tagen zur
   Pflicht; ob sie an den übrigen Tagen an der Verteilung teilnimmt, ist dort beim Schätzen zu klären, nicht
   hier. Wird B-615 Teil (c) zuerst gebaut, ist diese Schätzung nachzusehen.

### Angriffsplan (Backend, eine Story, in dieser Reihenfolge)

1. Schema: Tabelle Kind × Fach (Prozentwert, eindeutig je Kind und Fach), Tabelle der Stände, Budget am Kind;
   Migration anhängen; `SchemaGuardTests` grün.
2. Vertrag und Vater-Endpunkte: Liste als Ganzes schreiben und lesen (Summe 100, kein doppeltes Fach, 0 bis
   100), Budget am Kind, Folge der Stände. Schreiben `[Authorize(Roles = Roles.Supervisor)]` mit
   Eigentumsprüfung. Lese-Endpunkt für das Kind auf der Student-Ebene.
3. Verteilungsrechnung als eigener Dienst in `Services/Shared`: Kind und Tag hinein, Dosis je Position heraus
   (Rundung nach Entscheidung 10, gleichmäßig je Fach, Kappe der Position, Positionen außerhalb der Liste wie
   heute). Rein rechnend, mit Unit-Tests.
4. Die fünf Lesestellen der Dosis auf diesen Dienst umstellen; `DailyDoseInForce` daraus füllen.
5. 0-%-Regel in Pflicht und Malus.
6. Meldungen für den Vater per API: leerer Anteil (Entscheidung 7), Positionen außerhalb der Liste
   (Entscheidung 12).
7. `Pugling.Client` nachziehen, Doku-Beispiele (`DocsCaptureTests`) ergänzen.

### Testweg

- Neue Integrationstestklasse `FaecherVerteilungTests` in `Pugling.Api.Tests`: AK 1 bis 12, je AK mindestens
  ein Fall mit der genannten roten Probe. AK 3 zuerst.
- Unit-Tests für die Verteilungsrechnung (Rundung, Gleichstand, Kappe, leerer Anteil).
- Bestehende Wächter müssen grün bleiben, ohne angepasst zu werden: `DosisResteTests`, `PflichtMalusTests`,
  `VierWochenTests` (kein Budget gesetzt, also unverändertes Verhalten).
- Migration: ein Test nach dem Muster von `DailyBoxOpenedAtMigrationTests`, der eine DB auf dem Stand vor der
  Migration mit Bestandszeilen hochzieht.
- `SchemaGuardTests` und `ConventionGuardTests` (Rollen-Gate der neuen Endpunkte).
- `/smoke-test` gegen eine Wegwerf-DB; vor der Abnahme der Agent `pugling-reviewer`.
- Rollengang: per API als Vater setzen und als Kind die Tagesdosis lesen. Der Gang an der Oberfläche ist erst
  mit B-619 möglich; das steht dann im `## Verlauf`.

## Verlauf

- **2026-11-17** — angelegt als Teil 1 der Teilung von B-616 (Schnitt vom Vater gewählt). Entscheidungen und
  AK 1 bis 12 von dort übernommen, Ist-Stand um die Lesestellen der Tagesdosis ergänzt (Stand `bf5fd841`).
  Geschätzt: L, `backend`, Migration ja, Vertragsbruch nein.
- **2026-11-17** — B-615 ist geteilt worden; sein Teil (c), auf den Risiko 6 verweist, ist jetzt [B-621](B-621-stundenplan-pflicht-backend.md) (Backend) und [B-622](B-622-stundenplan-pflicht-vater-web-und-sohn-app.md) (Frontend). Die dort offene Frage, ob eine Stundenplan-Position an pflichtfreien Tagen an der Verteilung teilnimmt, steht in B-621 als Annahme (ja), vom Vater nicht bestätigt.
- **2026-11-17** — **Entscheidung des Vaters** (B-621 Entscheidung 9): Eine Position mit Taktung „Stundenplan“
  nimmt an jedem Tag an der Verteilung teil, auch an pflichtfreien Tagen. Risiko 6 ist damit beantwortet: Die
  Verteilungsrechnung behandelt die Taktung nicht gesondert.
- **2026-11-17** — **Entscheidung des Vaters (Grill-Runde B-644, Entscheidung 6): angehalten.** Die Story wird nicht gebaut, bis [B-644](B-644-tagesplaner-statt-fester-lauf.md) gegrillt ist: der Tagesplaner verlegt die Pflicht von der Position an die Tagesaufgabe. Stufe und Entscheidungen bleiben bestehen; danach wird sie als Regel des Planers überführt oder verworfen.
