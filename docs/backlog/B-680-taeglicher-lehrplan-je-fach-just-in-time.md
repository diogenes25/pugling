---
tags: [typ/story, status/ausformuliert, bereich/lehrplan, bereich/klassenarbeit, rolle/creator, rolle/student]
aliases: [Täglicher Lehrplan, Just-in-time-Plan, Privatlehrer-Plan]
status: ausformuliert
prio: P1
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/leitbild-privatlehrer.md
grund: ""
ersetzt_durch: []
---

# B-680 · Der Lehrer-Agent erzeugt jeden Tag den Plan je Fach aus Klausur, Stoff und Lernstand

Heute ist ein `StudyPlan` eine feste Sammlung von Positionen, die der Vater oder der KI-Creator (`plan`, `assign`) auf
Zuruf anlegt; täglich wandern nur Tagesdosis, Leitner-Fälligkeit und Tagesmission. Das Leitbild vom 2026-11-19 verlangt
**Privatunterricht**: einmal am Tag legt ein Lehrer-Agent je Kind und Fach fest, was heute dran ist – ausgerichtet auf
den nächsten Klausurtermin und dessen Stoff, auf den Lernstand des Kindes und das Ziel (die Notenstufe, B-682). Der
Umfang ergibt sich aus dem, was bis zum Ziel noch fehlt. Gibt es keine Klausur, läuft ein freier Plan, damit das Kind
im Stoff bleibt. Die tägliche Pflicht mit dem Stick bleibt der Grundgedanke.

**Vorbehalt:** Diese Story hängt am Richtungswechsel „Leitbild Privatlehrer“ (2026-11-19), den der Nutzer entscheidet.
Sie wurde im Vorbereitungslauf nur **ausformuliert**; jede Frage unter „Offene Punkte“ steht mit Empfehlung da und ist
**nicht entschieden**. Es gibt bewusst keinen Abschnitt `## Entscheidungen` mit Inhalt.

## User Story

Als *Schüler* möchte ich jeden Tag für jedes Fach genau die Aufgaben bekommen, die mich bis zur nächsten Klausur (oder,
ohne Klausur, im Stoff) weiterbringen, damit ich nicht an einem Plan arbeite, der gestern gemacht wurde und zu meinem
Stand von heute nicht mehr passt. Als *Vater* möchte ich diese Plansteuerung nicht mehr selbst tragen, damit ich mich
auf die Belohnung konzentrieren kann – und die Pflicht trotzdem erzwungen bleibt (verpasste Pflicht kostet Münzen).

## Ist-Stand am Code

Gemessen am 2026-11-19 auf `main` (Stand `47791819`). Pfade relativ zu `backend/`. Die Zeilennummern in
`PositionProgressService.cs` haben sich seit dem Ist-Stand in B-644 verschoben (dort noch Stand `36185a56`) und sind
hier neu gelesen.

### Was heute den Tagesplan je Fach „erzeugt“: nichts – er wird aus festen Positionen gerechnet

- **Die Pflicht steht an der Position, nicht am Tag.** `PlanPosition` trägt `NewItemsPerDay`, `Cadence`,
  `GoalThreshold` und `PenaltyCoins` (`Pugling.Api/Models/PlanPositionEntities.cs:40`, `:51`, `:75`, `:86`). Die
  Taktung kennt nur `None`/`Daily`/`Weekly`.
- **Der `StudyPlan` ist der Container mit Fach, Unit und Pflicht-Enddatum:** `SubjectId` (`Models/StudyPlanEntities.cs:83`),
  `SeriesUnitId` (`:86`), `EndDate` ohne `?` (`:89`) und `Active` (`:90`). Ein Plan ohne Termin lässt sich damit nicht
  ausdrücken; „kein Termin, freier Plan“ aus dem Leitbild hat heute keine Entsprechung.
- **Die Tagessicht rechnet, sie legt nichts an:** `PositionProgressService.ComputeChildDayAsync`
  (`Services/Shared/PositionProgressService.cs:407`) fasst alle aktiven Pläne eines Kindes für einen Tag zusammen
  (`ChildDayRollup`); die Sohn-App liest sie über `ChildTodayController` (`Controllers/Student/ChildTodayController.cs:19`,
  `:27`). Es entsteht keine gespeicherte Tagesaufgabe, die „heute neu“ festgelegt würde.
- **Wer Pläne anlegt:** der Vater über `StudyPlansController` (`Controllers/Supervisor/StudyPlansController.cs`; ein
  zweiter aktiver Plan je Fach wird dort abgewiesen, B-539) und der KI-Creator als Konsolen-Agent außerhalb der API
  (`Pugling.Agent.Creator/PlanPlanner.cs:101`, legt den Plan bewusst **inaktiv** an, `PlanAssigner.cs:224`).
- **Es gibt keinen Zeitgeber:** kein `IHostedService`, `BackgroundService`, `PeriodicTimer` und kein `TimeZoneInfo` im
  ganzen Backend (Suche ohne Treffer). Der Malus wird träge beim Kind-Login abgerechnet
  (`Controllers/AuthController.cs:85`, `Controllers/Student/MeController.cs:296` → `SettleClosedPeriodsAsync`).
  „Heute“ ist überall UTC (Details und Zählung der Fundstellen: B-644).

### Klausurdaten: vorhanden, aber ohne Wirkung auf die Pflicht

- `Klassenarbeit` hat Kind, Fach, **Pflicht-Datum** (`Models/KlassenarbeitEntities.cs:67`, `:79`), eine optionale Note
  `Grade` (`:82`) und `GradeComment` (`:84`) sowie einen Stoff aus Übungen (`KlassenarbeitExercise`, `:92`) und Tags
  (`KlassenarbeitTag`, `:106`). Schreiben darf nur der Supervisor (`Controllers/Supervisor/KlassenarbeitenController.cs:92`,
  `:130`, `:172`); die Note wird auf 1,0 bis 6,0 geprüft (`:47`).
- Gelesen wird die Klassenarbeit nur dort: im eigenen Controller und im `ObjectiveEvaluationService` für das Ziel-Maß
  `ClassTestGrade` (`Services/Shared/ObjectiveEvaluationService.cs:24`, `:37`). Eine Pflicht, einen Plan oder eine
  Tagesdosis speist sie nicht (`grep` nach `Klassenarbeit` in `Services/` findet nur `ExerciseUsageQueries` und diesen
  Dienst).

### Lernstand: je Wort vollständig, je Fach als Note nicht

- Jede Übungsantwort liegt als `ItemReviewEvent` vor (`Models/ItemProgressEntities.cs:61`), der Lernstand je Wort über alle
  Pläne als `ItemProgress` mit Beherrschung in Prozent (`:18`, `:41`); „schwach“ ist unter `WeakBelowPercent = 50` (`:39`).
  Testantworten: `TestItemResult` (`Models/StudyPlanEntities.cs:202`).
- Eine Größe „Lernstand des Fachs“ oder „Notenstufe“ gibt es nicht (keine Entität, kein Dienst; Suche nach Notenstufe/Prognose
  im Backend ohne Treffer). Verwandt ist nur das Ziel-Maß `ClassTestGrade`, das eine **eingetragene** Note liest, keine
  Prognose.
- Eine Fehlerart wird nirgends gespeichert; ein Leistungsprofil je Fach existiert nicht (Befund aus B-644, hier nicht neu
  gemessen).

### Rollen und Gates

- Die Plan-Steuerung liegt unter `supervisor/…` hinter `Roles.Supervisor` (`Auth/AuthAccess.cs:13`,
  `KlassenarbeitenController.cs:92`); der KI-Creator läuft als Creator-Rolle gegen dieselbe API. Ein „Lehrer“ im Sinne
  des Leitbilds ist heute das Lehrer-Konto (Creator ohne Betreuungsauftrag, `docs/lehrer-konto-plan.md`) – ohne
  Plan-Steuerung. Das ist der Berührungspunkt zu B-681.

## Die echte Lücke

B-680 beschreibt in der Sache fast dasselbe wie die schon gegrillte [B-644](B-644-tagesplaner-statt-fester-lauf.md)
(Tagesplaner statt festem Lauf, 14 Entscheidungen des Vaters vom 2026-11-17). Neu gegenüber B-644 sind nur zwei Dinge:

1. **Wer entscheidet:** B-644 legt fest, dass **feste Regeln in C#** den Plan zusammenstellen und ein Sprachmodell nur
   außerhalb der API Inhalte baut (B-644, Entscheidung 10). „Lehrer-Agent“ im Leitbild klingt nach einem Agenten, der
   den Plan selbst entscheidet. Beides ist nicht dasselbe; es ist die wichtigste offene Frage (Punkt 1).
2. **Wozu:** Das Ziel des Plans ist nicht mehr nur „Pflicht erfüllt“, sondern eine **Notenstufe** (B-682), und die
   Klausur ist der Fixpunkt, an dem rückwärts gerechnet wird (B-683 eicht die Prognose).

Fehlend am Code (zusätzlich zur Liste in B-644): ein Maß „was fehlt bis zum Ziel“ je Fach – es gibt weder ein Ziel pro
Fach noch eine Rechnung dahin; und ein Plan ohne Pflicht-Enddatum.

## Offene Punkte

Jede Frage mit Empfehlung. **Nichts davon ist entschieden.**

1. **Wie verhält sich B-680 zu B-644?** Fast alle Bausteine (Tagesauswertung, Leistungsprofil, Tagesplaner, Zeitgeber,
   Lerntage, Reset zum Stichtag) stehen dort. Empfehlung: B-680 wird **keine zweite Planer-Story**, sondern die
   Produktseite von B-644 – es bleibt eine Karte, und was in B-680 an Entscheidungen neu fällt (Zielbegriff
   Notenstufe, Wer-entscheidet), wird in B-644 als Regel eingearbeitet. Alternativ B-680 mit B-644 zusammenlegen
   (`verworfen`, `ersetzt_durch`) – das entscheidet der Nutzer.
2. **Entscheiden Regeln oder ein Sprachmodell über den Tagesplan?** B-644 Entscheidung 10 sagt: Regeln in C#,
   Sprachmodell nur für Inhalt und Prosa. Das Leitbild sagt „Lehrer-Agent“. Empfehlung: bei der Regel-Lösung bleiben –
   die Pflicht kostet Münzen und muss nachrechenbar und wiederholbar sein; „Lehrer-Agent“ ist dann der Name des
   Planers für das Kind, nicht ein LLM-Aufruf. *Rollenschnitt Vater/Lehrer – Nutzer entscheidet.*
3. **Was bleibt für den Vater an der Steuerung?** Leitbild: nur Belohnung. B-644 (Entscheidungen 2, 4, 7, 8):
   Boden und Deckel, Pause und Streichen einer Aufgabe liegen beim Vater. Empfehlung: Der Vater behält eine **Notbremse**
   (Pause eintragen, eine Aufgabe streichen), setzt aber keine Mengen mehr; Boden und Deckel werden Voreinstellungen des
   Planers. Ob das gewollt ist, hängt an B-681. *Rollenschnitt – Nutzer entscheidet.*
4. **Wie wird eine Pflicht, die erst heute entsteht, gegen den Malus fair?** Heute steht die Pflicht Wochen vorher fest
   (`PenaltyCoins` an der Position, `PlanPositionEntities.cs:86`). Empfehlung: Der Plan des Tages steht ab 6 Uhr fest und
   ist danach eingefroren (B-644, Selbstbetrug); der Malus gilt je verpasster Pflichtaufgabe, nie rückwirkend für
   Aufgaben, die das Kind nicht sehen konnte (Tag ohne Plan = Pause, B-644 Punkt 15). *Malus-Regeln – Nutzer entscheidet.*
5. **Was ersetzt „KI-Plan entsteht inaktiv“, wenn niemand freigibt?** Heute ist die Freigabe die Sicherung gegen
   fehlerhaften Maschineninhalt (`PlanPlanner.cs:101`). Empfehlung: Schattenmodus eine Woche (B-644 Entscheidung 14) und
   danach Deckel plus Boden als harte Grenzen – der Planer kann die Pflicht weder über den Deckel heben noch auf null
   senken.
6. **Münz-Ökonomie:** Wie viel ist eine erfüllte Tagespflicht in Münzen wert, wenn das Ziel die Notenstufe ist, nicht
   mehr die Pflicht? Empfehlung: Münzen bleiben an der erfüllten Tagespflicht und an der Klausur, die Notenstufe ist
   Auszeichnung ohne Münzwert, bis B-682 das anders entscheidet. *Münz-Ökonomie – Nutzer entscheidet.*
7. **Läuft der Planer im Server oder als Agent gegen die API?** Empfehlung: im Server, als idempotenter träger Aufruf
   (B-644, Der 6-Uhr-Lauf); der Konsolen-Agent bleibt für Inhalte zuständig.
8. **Stoff ohne Termin:** `StudyPlan.EndDate` und `Klassenarbeit.ScheduledDate` sind Pflichtfelder. Empfehlung: im
   Karten-Schnitt von B-644 (Punkt 13) mit entscheiden; hier nicht vorwegnehmen.
9. **Was sind „freie Pläne“ ohne Klausur?** Empfehlung: dieselbe Planer-Regel ohne Rückwärtsrechnung (Tempo aus dem
   Leistungsprofil, Boden bleibt), kein eigener Pfad.
10. **Wie weit trägt ein Plan, wenn das Ziel die Notenstufe ist?** Das Prognosemodell „Lernstand → Note“ gibt es nicht
    (B-682). Empfehlung: B-680 baut zuerst ohne Notenstufe auf Termin, Stoff und Beherrschung der Wortliste; die
    Notenstufe ist ein späterer Eingang des Planers, keine Voraussetzung.
11. **Reihenfolge der Karte:** Empfehlung: Karte (`/wayfinder`) erst, nachdem der Nutzer Punkt 1 bis 3 entschieden hat;
    vorher wäre jedes Ticket eine Vermutung.

## Beziehungen zu anderen Stories (nur gelesen)

- **[B-644](B-644-tagesplaner-statt-fester-lauf.md)** (`ausformuliert`, P1, hat 14 Entscheidungen des Vaters): große
  inhaltliche Überschneidung, siehe oben. Konflikte mit dem Leitbild: Entscheidung 7 (Vater kann eingreifen) und 10
  (Regeln statt Sprachmodell).
- **[B-681](B-681-rollenschnitt-vater-belohnt-lehrer-unterrichtet.md)** (`idee`): entscheidet, wer den Plan steuert;
  Voraussetzung für Punkt 3.
- **[B-682](B-682-notenstufe-je-fach-statt-liga.md)** (`idee`): liefert das Ziel (Notenstufe), von dem der Plan
  zurückrechnet; B-680 darf nicht darauf warten (Punkt 10).
- **[B-683](B-683-notenstufe-an-der-echten-klausur-eichen.md)** (`idee`): die echte Note eicht die Prognose; liest
  `Klassenarbeit.Grade`, das schon vorhanden ist.
- **[B-684](B-684-korrigierte-klausur-einscannen.md)** (`idee`, P3): bringt Note und Fehler an die Klassenarbeit;
  die Fehlerart ist auch ein Eingang des Leistungsprofils in B-644.
- **[B-248](B-248-klassenarbeit-countdown-treibt-den-plan.md)** (`idee`): die Klassenarbeit verdichtet den Plan – geht
  in B-644 (Regel 4) auf. **[B-618](B-618-faecher-verteilung-backend.md)** (`geschaetzt`, wartet auf B-644): Verteilung
  über die Fächer. **[B-630](B-630-rueckmeldung-im-ki-creator.md)** (`geschaetzt`): Rückmeldungen im KI-Creator.

## Akzeptanzkriterien (Entwurf)

Jedes Kriterium nennt die Änderung, die es rot macht. Sie werden erst final, wenn die offenen Punkte 1 bis 3
entschieden sind; Zahlen und Namen sind Platzhalter.

1. Für jedes Kind und jedes Fach mit aktivem Stoff entsteht an jedem Lerntag genau eine gespeicherte Tagesvorgabe;
   zweimaliges Auslösen erzeugt keine zweite. *Rot, wenn* der Aufruf nicht idempotent ist (zweiter Lauf legt eine zweite
   Zeile an).
2. Mit einem eingetragenen Klausurtermin richtet sich die Menge neuer Wörter nach dem Abstand zum Termin und dem noch
   nicht beherrschten Rest der Stoff-Wortliste. *Rot, wenn* der Planer den Termin ignoriert (gleiche Menge bei Termin
   in drei Tagen und in drei Wochen).
3. Ohne Klausurtermin entsteht trotzdem eine nicht leere Tagesvorgabe am Boden. *Rot, wenn* ein Fach ohne Termin
   einen leeren Tag liefert.
4. Ein nachgetragener oder verschobener Termin ändert die Vorgabe des **Folgetags**, nicht die des laufenden Tages.
   *Rot, wenn* der laufende, eingefrorene Plan sich ändert.
5. Der Tagesplan ist nach dem Erzeugen für das Kind nicht neu würfelbar. *Rot, wenn* ein zweiter Abruf eine andere
   Zusammenstellung liefert.
6. Eine bis Tagesende nicht erledigte Pflichtaufgabe kostet genau einmal Münzen, eine erledigte nichts; ein Tag, an dem
   kein Plan entstand, kostet nichts. *Rot, wenn* der Malus für einen nie sichtbar gewesenen Plan gebucht wird oder
   doppelt.
7. Dieselben Daten ergeben denselben Plan (deterministisch). *Rot, wenn* der Planer von Zufall oder einem Sprachmodell
   abhängt.
8. Das Kind sieht je Aufgabe den Grund (Klausur in n Tagen, Wort schwach). *Rot, wenn* die Begründung fehlt oder bei
   unterschiedlichen Ausgangsdaten gleich lautet.
9. Die Notenstufe (B-682) ist **kein** Eingang dieses ersten Baus. *Rot, wenn* ein Test den Plan an eine Notenstufe
   bindet, die es noch nicht gibt.

## Verlauf

- **2026-11-19** — angelegt aus dem Leitbild-Gespräch mit dem Vater ([Leitbild](../leitbild-privatlehrer.md)).
- **2026-11-19** — `idee` → `ausformuliert` (Vorbereitungslauf, Sonnet): Ist-Stand am Code belegt. Kern: Die Story
  überschneidet sich weitgehend mit der schon gegrillten B-644; neu sind nur das Ziel „Notenstufe“ und die Frage, ob ein
  Agent oder feste Regeln den Plan entscheiden. Elf offene Punkte mit Empfehlung, **nichts entschieden** (Rollenschnitt,
  Münz-Ökonomie und Malus-Regeln liegen beim Nutzer).
