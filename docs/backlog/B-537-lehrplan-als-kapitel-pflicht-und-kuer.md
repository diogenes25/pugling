---
tags: [typ/story, status/verworfen, bereich/gamification, rolle/supervisor, rolle/student, rolle/creator]
aliases: [Lehrplan als Kapitel, Pflicht und Kür, abnehmende Münzen bei Wiederholung]
status: verworfen
prio: P1
art: Wunsch
quelle: "Nutzer im Chat 2026-11-15: Vervollständigung des Übungsaufbaus – ein Lehrplan stellt ein Kapitel dar"
grund: "geteilt — gegrillt am 2026-11-15 (elf Entscheidungen hier), gebaut wird in drei unabhängigen Teilen: Bonus-Münzregeln, ein Plan je Fach, Plan aus Unit"
ersetzt_durch: [B-538, B-539, B-553]
---

# B-537 · Lehrplan als Kapitel: tägliche Pflicht plus großer Kür-Pool mit abnehmenden Münzen

Ein Lehrplan soll ein **Kapitel** abbilden: Beginnt ein neues Kapitel, wird ein Lehrplan erstellt, der sich in
etwa **vier Wochen** durcharbeiten lässt, mit passend erzeugten Übungen. Ein Teil der Übungen ist **Pflicht**, so
dass in jedem Fach jeden Tag etwas gemacht wird. Daneben gibt es eine **große Anzahl zusätzlicher Übungen**, mit
denen sich das Kind freiwillig Münzen erarbeiten kann. **Schwierigere Übungen bringen mehr Münzen**; wird eine
Übung **wiederholt**, sinkt der Münzertrag mit jeder Wiederholung – dieselbe Übung mehrfach zu machen ist erlaubt,
lohnt sich aber immer weniger.

**Begriff:** „Kapitel" heißt im Fachmodell **Unit** (`SeriesUnit`); ein `Chapter` gibt es seit B-106 nicht mehr.
Diese Story schreibt darum „Unit" und meint das Kapitel des Lehrwerks.

## User Story

Als *Vater* möchte ich zu Beginn einer neuen Unit mit einem Schritt einen Lehrplan für rund vier Wochen bekommen,
der jeden Tag eine kleine Pflicht verlangt und daneben viele freiwillige Übungen anbietet, damit mein Kind die
Unit verlässlich durcharbeitet, ohne dass ich jede Übung einzeln plane. Als *Sohn* möchte ich mir über die Pflicht
hinaus Münzen erarbeiten können – mehr für schwere Übungen, weniger, wenn ich dieselbe Übung nur wiederhole –,
damit sich zusätzliches Üben lohnt, aber nicht das Abgrasen einer leichten Übung.

## Ist-Stand am Code

Der größte Teil des Gerüsts steht schon; die Idee ist eher eine **Zusammenführung** als ein Neubau.

1. **Ein Lehrplan hat eine Laufzeit, aber keine Unit.** `StudyPlan` trägt `StartDate`/`EndDate` und optional ein
   `SubjectId`, aber keinen Bezug auf eine `SeriesUnit` (`Models/StudyPlanEntities.cs:65-82`). Die Unit hängt nur
   mittelbar an den Übungen (`SeriesUnitId`, `Models/CurriculumEntities.cs:153`). Das Kind trägt je Lehrwerk eine
   aktuelle Unit (`Textbook.CurrentUnitId`, `Models/AdminEntities.cs:180`).
2. **Pflicht und Kür gibt es je Position schon.** `PlanPosition.Cadence` kennt `None` = „freies Üben ohne
   Pflicht", `Daily`, `Weekly` (`Pugling.Contracts/Common/PlanPositionBaseTypes.cs:4-10`,
   `Models/PlanPositionEntities.cs:50-51`); der Malus greift nur bei Daily/Weekly
   (`Models/PlanPositionEntities.cs:85-91`, `Services/Shared/PositionProgressService.cs:209`).
3. **Genau ein aktiver Plan je Kind.** Wird ein Plan aktiv, schaltet der Server alle anderen des Kindes ab
   (`Controllers/Supervisor/StudyPlansController.cs:105`, `:109-116`, Anti-Cheat). „In **jedem Fach** jeden Tag"
   geht also nur, wenn **ein** Plan die Positionen mehrerer Fächer trägt – das geht heute schon, weil die
   Fachzuordnung aus der Übung folgt (`Models/PlanPositionEntities.cs:27`); `StudyPlan.SubjectId` ist nur
   Klassifizierung (`Models/StudyPlanEntities.cs:73-74`).
4. **Ein Lehrplan pro Unit lässt sich schon erzeugen – aber zwei Wochen, nur Pflicht.** Der KI-Creator (`plan`)
   legt je erzeugter Übung eine Position an, alle mit `GoalCadence.Daily`
   (`../Pugling.Agent.Creator/PlanPlanner.cs:240`, `:289`); Vorgabe-Laufzeit 14 Tage
   (`../Pugling.Agent.Creator/AgentCommands.cs:26`), die Tagesdosis der Buch-Wortliste wird über die Laufzeit
   verteilt (`AgentCommands.cs:369`). Der Plan entsteht inaktiv (Agent-`CLAUDE.md`, Abschnitt `PlanPlanner`).
5. **Der Kür-Pool existiert als Bonus-Training (B-533, abgenommen).** Extra-Übungen zum aktuellen Thema, offen erst
   nach erfüllter Tagespflicht, Münzen nur für geprüfte Treffer, Tagesdeckel 30
   (`Services/Student/ExtraTrainingService.cs:9-22`, `Services/Student/ExtraTrainingOptions.cs:14`). Der Pool wird
   **aus dem Katalog** der Units gezogen, nicht aus dem Plan; Übungen, die schon Plan-Position sind, fallen heraus
   (`ExtraTrainingService.cs:19-21`, `:247-248`).
6. **„Schwerer = mehr Münzen" gibt es nur über die Abfrageform, nicht über die Übung.** Bonus-Training zahlt 1
   Münze je Treffer bei Auswahl, 2 bei Tippen (`ExtraTrainingOptions.cs:16-18`). Eine **Schwierigkeit der Übung**
   gibt es im Modell nicht – gesucht nach `Difficulty`: nur die Stufen der einzelnen Verfahren und der
   `StageSchedule` einer Position (`Models/PlanPositionEntities.cs:128`).
7. **„Wiederholung zahlt weniger" gibt es zweimal, aber anders als gewünscht.**
   - Im Plan je **Karte** nach Leitner: neu = `NewContentPoints` (10), danach `max(2, 8 − Box)`
     (`Services/Shared/ScoringService.cs:110-117`); gezahlt wird nur eine **fällige** Karte, höchstens einmal am Tag
     (`Controllers/Student/PositionPracticeController.cs:397`, `:402`). Freies Wiederholen im Info-Modus zahlt nichts.
   - Im Bonus-Training je **Übung**: eine Nicht-Vokabel-Übung zahlt genau einmal; eine Vokabel-Übung darf wieder,
     fragt aber mit jedem Lauf **schwerer** statt **billiger** (Selbsteinschätzung → Auswahl → Tippen,
     `ExtraTrainingService.cs:14-16`).
   Ein **abnehmender Münzfaktor je Wiederholung derselben Übung** existiert nirgends.

## Die echte Lücke

1. **Kein „Unit → Vier-Wochen-Plan" in einem Schritt mit Pflicht/Kür-Mischung.** Der Generator macht alles zur
   Tagespflicht und plant zwei Wochen; einen Kür-Anteil kennt er nicht.
2. **Kein Bezug Plan ↔ Unit.** Ohne ihn kann weder der Vater sehen, welches Kapitel ein Plan abdeckt, noch der
   Server „neues Kapitel beginnt" an etwas festmachen.
3. **Keine Übungsschwierigkeit**, an der sich der Münzwert festmachen ließe.
4. **Keine Degression je Wiederholung** – heute ist es „einmal und nie wieder" (Nicht-Vokabeln) oder „gleich viel,
   aber schwerer" (Vokabeln).
5. **Nicht gefunden / nicht sicher:** ob „in jedem Fach täglich" heute für Theodor/Friedrich tatsächlich an der
   Ein-Plan-Regel scheitert oder ob ihre Pläne schon fachübergreifend sind – nur an der echten DB (Kopie) prüfbar.

## Offene Punkte

1. ~~Wo lebt die Kür – im Plan oder im Bonus-Training?~~ → Entscheidung 3.
2. ~~Braucht der Plan einen Unit-Bezug (`StudyPlan.SeriesUnitId`)?~~ → Entscheidung 2.
3. ~~Mehrere Fächer, ein Plan – oder die Ein-Plan-Regel lockern auf „ein aktiver Plan je Fach"?~~ → Entscheidung 1.
4. ~~Woraus ergibt sich „schwer"?~~ → Entscheidung 5.
5. ~~Wie soll die Degression aussehen?~~ → Entscheidung 6.
6. ~~Gilt die Degression auch für Nicht-Vokabel-Übungen~~ → Entscheidung 6.
7. ~~Vier Wochen als neue Generator-Vorgabe (`--days 28`)?~~ → Entscheidung 9.
8. ~~Wie viel Pflicht pro Tag und Fach?~~ → Entscheidung 8.
9. ~~Verhältnis zu B-535 und B-536?~~ → Entscheidung 11.
10. ~~Muss das ein Schritt sein, oder reicht es, wenn der Vater den Plan im Wizard aus der Unit befüllt?~~ → Entscheidung 9.
11. ~~Passt die Story in eine Sitzung?~~ → Entscheidung 11.

## Entscheidungen

Gegrillt am 2026-11-15 im Dialog mit dem Nutzer (Vater). Jede Entscheidung mit Begründung und Kosten.

1. **Ein aktiver Plan je Fach** (statt je Kind). Nur so passt „ein Kapitel = ein Lehrplan" zu „jeden Tag in jedem
   Fach"; jedes Fach hat seinen eigenen Vier-Wochen-Takt. Der Anti-Cheat-Grund der Ein-Plan-Regel („das leichteste
   wählen") bleibt je Fach gewahrt. Kosten: jede Stelle, die „den" aktiven Plan sucht (Tagespflicht, Tagesbox,
   Bonus-Sperre, Malus), wird zu einer Menge – der größte Umbau dieser Story.
2. **Fach ist Pflicht am Plan, Unit optional.** Die Regel aus 1 braucht ein eindeutiges Fach; ein Plan ohne
   Kapitel (Klassenarbeitsvorbereitung, Mathe ohne Buch) bleibt möglich. Mit Unit wird das Fach aus der Reihe
   übernommen, fehlt es dort, wählt der Vater. Kosten: Migration, die das Fach bestehender Pläne aus ihren Übungen
   ableitet (Mehrheit der Positionen); Pflichtfeld im Wizard; neue optionale Spalte für die Unit.
3. **Die Kür lebt im Bonus-Training, gebunden an die Unit des Plans** – nicht als Plan-Positionen. Die
   Anti-Farm-Regeln (erst Pflicht, nur geprüfte Treffer, Tagesdeckel) existieren dort schon; ein zweiter Münzweg
   hätte eigene Lücken. Kosten: der Vater sieht die Kür nicht im Plan, sondern als „Bonus-Übungen dieser Unit";
   Einzelausschluss gibt es nicht.
4. **Plan-Positionen ohne Pflicht (`Cadence.None`) bleiben, zahlen aber keine Münzen mehr.** Sie schlossen sonst
   eine Farm neben dem Bonus-Training (offen vor der Pflicht, kein Deckel). Kosten: die Oberfläche muss „ohne
   Münzen" ehrlich zeigen, die Punkte-Felder solcher Positionen werden im Editor wirkungslos und ausgeblendet.
5. **„Schwer" ergibt sich nur aus der Abfrageform: Auswahl 1, getippt 3 Münzen je Treffer** (Selbsteinschätzung
   weiter 0). Prüfbar und nicht durch eine falsche Einstufung auszuhebeln; ein Schwierigkeitsfeld der Übung wäre
   geraten. Kosten: zwei Übungen derselben Form zahlen je Treffer gleich, egal wie anspruchsvoll der Stoff ist –
   ein Feld kann nachgezogen werden, wenn sich das als zu grob erweist.
6. **Degression je Wiederholung: jeder weitere abgeschlossene Lauf derselben Übung am selben Tag halbiert ihren
   Wert (100 → 50 → 25 → 0 %); am nächsten Tag wieder voll – für alle Übungsarten.** Tägliches Wiederholen ist beim
   Vokabellernen der Kern und wird nicht bestraft; gegen das Abgrasen an einem Tag wirken Halbierung und Deckel.
   Kosten: Nicht-Vokabel-Übungen (Lückentext usw.) verlieren ihr „genau ein Lauf" und zahlen am Folgetag wieder
   voll, obwohl die Lösung noch im Kopf sein kann – bewusst in Kauf genommen.
7. **Tagesdeckel des Bonus-Trainings: Vorgabe 90 Münzen**, der Deckel je Kind bleibt. Mit 3 je Treffer wäre 30
   nach einem Lauf erreicht und der große Kür-Vorrat wertlos; 90 ≈ zwei Drittel der Tagespflicht (≈ 140). Kosten:
   die Shop-Preise sind auf die alte Münzmenge geeicht – nach der ersten Woche prüft der Vater sie (kein Code).
8. **Tägliche Pflicht = Vokabeln mit Tagesdosis; Lückentext/Grammatik wöchentlich; der Rest Kür. Ein Fach ohne
   Vokabeln (Mathe) bekommt eine tägliche Übung.** Kommt mit den bestehenden Rhythmen aus, hält die Tagespflicht
   bei drei Fächern klein und macht Grammatik trotzdem verbindlich. Kosten: in Mathe ist die tägliche Pflicht
   jeden Tag dieselbe Übung.
9. **Ein Server-Endpunkt baut den Plan aus den Katalog-Übungen einer Unit** (28 Tage, Regeln aus 8); Vater-Wizard
   und KI-Agent rufen ihn beide. Im Backend gibt es kein Sprachmodell – der Agent füllt vorher nur den Katalog.
   Die Pflicht/Kür-Aufteilung ist eine Geschäftsregel und gehört in den Server (API-First). Kosten: neuer Endpunkt
   mit Vertrag und Client; `PlanPlanner` im Agenten wird umgebaut; Umgang mit dünn befüllten Units.
10. **Wird ein Plan mit Unit aktiv, setzt der Server die aktuelle Unit des Lehrbuchs – nur vorwärts.** „Neues
    Kapitel" ist ein Schritt, ein Wiederholungsplan zu einer früheren Unit verstellt nichts. Kosten: die
    Reihenfolge der Units muss verlässlich sein – sie steht in `SeriesUnit.Grade` + `OrderIndex`
    (`Models/CurriculumEntities.cs:72-75`).
11. **Geteilt in drei Stories, Reihenfolge B-535 → B-538 → B-539 → B-553.** B-538 Bonus-Münzregeln (4–7), B-539
    ein Plan je Fach (1, 2), B-553 Plan aus Unit (8–10, braucht B-539). B-535 zuerst, weil es Box und
    Bonus-Training für Friedrich erreichbar macht und B-539 dieselben Stellen umbaut. Keine Karte – die
    Entscheidungen hängen nicht mehr voneinander ab. B-536 (Aufbau-Übungen) bleibt getrennt; seine leichteren
    Übungen landen von selbst im Bonus-Vorrat.

## Akzeptanzkriterien

Final und je Teil-Story aufgeteilt: [B-538](B-538-bonus-muenzregeln-form-und-degression.md),
[B-539](B-539-ein-aktiver-plan-je-fach.md), [B-553](B-553-plan-aus-unit.md).

## Verlauf

- 2026-11-15 · `idee` angelegt aus dem Chat (Prio P2 vorgeschlagen, vom Nutzer zu bestätigen).
- 2026-11-15 · Prio vom Nutzer auf P1 gesetzt; `ausformuliert`: Ist-Stand am Code belegt (Plan ohne Unit,
  `Cadence.None`, Ein-Plan-Regel, Generator 14 Tage nur Pflicht, Bonus-Training als Kür, Leitner-Degression je Karte),
  elf offene Punkte mit Empfehlung.
- 2026-11-15 · `gegrillt`: elf Entscheidungen im Dialog mit dem Nutzer; offene Punkte durchgestrichen.
- 2026-11-15 · `verworfen` (geteilt) nach Entscheidung 11 in B-538, B-539, B-553; diese Story bleibt die Quelle
  für Ist-Stand und Begründungen.
