---
tags: [typ/story, status/geschaetzt, bereich/lehrplan, bereich/klassenarbeit, rolle/creator, rolle/student]
aliases: [Täglicher Lehrplan, Just-in-time-Plan, Privatlehrer-Plan]
status: geschaetzt
prio: P1
art: Wunsch
groesse: L
wo: backend
migration: nein
vertragsbruch: nein
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

**Stand:** Stellvertretend gegrillt im Vorbereitungslauf 2026-11-20 auf Anweisung des Nutzers („Entscheide die offenen Fragen nach deiner Empfehlung“), unbestätigt. Die Rahmenentscheidungen A bis G gelten für den ganzen Cluster B-644, B-680 bis B-683, B-662, B-663, B-702 und B-703; führend ist B-681.

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

Alle Punkte entschieden; die Empfehlung steht durchgestrichen daneben.

1. ~~**Wie verhält sich B-680 zu B-644?** Fast alle Bausteine (Tagesauswertung, Leistungsprofil, Tagesplaner, Zeitgeber, Lerntage, Reset zum Stichtag) stehen dort. Empfehlung: B-680 wird **keine zweite Planer-Story**, sondern die Produktseite von B-644 – es bleibt eine Karte, und was in B-680 an Entscheidungen neu fällt (Zielbegriff Notenstufe, Wer-entscheidet), wird in B-644 als Regel eingearbeitet. Alternativ B-680 mit B-644 zusammenlegen (`verworfen`, `ersetzt_durch`) – das entscheidet der Nutzer.~~ → Entscheidung 1.
2. ~~**Entscheiden Regeln oder ein Sprachmodell über den Tagesplan?** B-644 Entscheidung 10 sagt: Regeln in C#, Sprachmodell nur für Inhalt und Prosa. Das Leitbild sagt „Lehrer-Agent“. Empfehlung: bei der Regel-Lösung bleiben – die Pflicht kostet Münzen und muss nachrechenbar und wiederholbar sein; „Lehrer-Agent“ ist dann der Name des Planers für das Kind, nicht ein LLM-Aufruf. *Rollenschnitt Vater/Lehrer – Nutzer entscheidet.*~~ → Entscheidung 2.
3. ~~**Was bleibt für den Vater an der Steuerung?** Leitbild: nur Belohnung. B-644 (Entscheidungen 2, 4, 7, 8): Boden und Deckel, Pause und Streichen einer Aufgabe liegen beim Vater. Empfehlung: Der Vater behält eine **Notbremse** (Pause eintragen, eine Aufgabe streichen), setzt aber keine Mengen mehr; Boden und Deckel werden Voreinstellungen des Planers. Ob das gewollt ist, hängt an B-681. *Rollenschnitt – Nutzer entscheidet.*~~ → Entscheidung 3.
4. ~~**Wie wird eine Pflicht, die erst heute entsteht, gegen den Malus fair?** Heute steht die Pflicht Wochen vorher fest (`PenaltyCoins` an der Position, `PlanPositionEntities.cs:86`). Empfehlung: Der Plan des Tages steht ab 6 Uhr fest und ist danach eingefroren (B-644, Selbstbetrug); der Malus gilt je verpasster Pflichtaufgabe, nie rückwirkend für Aufgaben, die das Kind nicht sehen konnte (Tag ohne Plan = Pause, B-644 Punkt 15). *Malus-Regeln – Nutzer entscheidet.*~~ → Entscheidung 4.
5. ~~**Was ersetzt „KI-Plan entsteht inaktiv“, wenn niemand freigibt?** Heute ist die Freigabe die Sicherung gegen fehlerhaften Maschineninhalt (`PlanPlanner.cs:101`). Empfehlung: Schattenmodus eine Woche (B-644 Entscheidung 14) und danach Deckel plus Boden als harte Grenzen – der Planer kann die Pflicht weder über den Deckel heben noch auf null senken.~~ → Entscheidung 5.
6. ~~**Münz-Ökonomie:** Wie viel ist eine erfüllte Tagespflicht in Münzen wert, wenn das Ziel die Notenstufe ist, nicht mehr die Pflicht? Empfehlung: Münzen bleiben an der erfüllten Tagespflicht und an der Klausur, die Notenstufe ist Auszeichnung ohne Münzwert, bis B-682 das anders entscheidet. *Münz-Ökonomie – Nutzer entscheidet.*~~ → Entscheidung 6.
7. ~~**Läuft der Planer im Server oder als Agent gegen die API?** Empfehlung: im Server, als idempotenter träger Aufruf (B-644, Der 6-Uhr-Lauf); der Konsolen-Agent bleibt für Inhalte zuständig.~~ → Entscheidung 7.
8. ~~**Stoff ohne Termin:** `StudyPlan.EndDate` und `Klassenarbeit.ScheduledDate` sind Pflichtfelder. Empfehlung: im Karten-Schnitt von B-644 (Punkt 13) mit entscheiden; hier nicht vorwegnehmen.~~ → Entscheidung 8.
9. ~~**Was sind „freie Pläne“ ohne Klausur?** Empfehlung: dieselbe Planer-Regel ohne Rückwärtsrechnung (Tempo aus dem Leistungsprofil, Boden bleibt), kein eigener Pfad.~~ → Entscheidung 9.
10. ~~**Wie weit trägt ein Plan, wenn das Ziel die Notenstufe ist?** Das Prognosemodell „Lernstand → Note“ gibt es nicht (B-682). Empfehlung: B-680 baut zuerst ohne Notenstufe auf Termin, Stoff und Beherrschung der Wortliste; die Notenstufe ist ein späterer Eingang des Planers, keine Voraussetzung.~~ → Entscheidung 10.
11. ~~**Reihenfolge der Karte:** Empfehlung: Karte (`/wayfinder`) erst, nachdem der Nutzer Punkt 1 bis 3 entschieden hat; vorher wäre jedes Ticket eine Vermutung.~~ → Entscheidung 11.

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-20 (Sonnet) auf Anweisung des Nutzers (Empfehlung übernommen), unbestätigt.*

## Entscheidungen

Entscheidungen 2, 3, 4, 6 und 13 sind die Rahmenentscheidungen A, B, E, F und D (führend B-681); Entscheidung 8 gilt gleichlautend in B-644.

1. **B-680 ist die Produktseite von B-644 und kein zweiter Planer.** B-680 liefert die Planer-Regeln je Fach (Etappen E3 und E4 von B-644) und wird erst gebaut, nachdem B-644 E0 bis E2 gelaufen sind (Ortszeit und Lerntage, Auswertung und Profil, Schattenmodus). Begründung: Fast alle Bausteine stehen in B-644 und seinen 14 Entscheidungen des Vaters; zwei Stories mit demselben Planer würden dieselbe Regel zweimal entscheiden. Kosten: B-680 ist eine Story lang nur eine Abhängigkeit; was neu entschieden wird (Notenstufe, Wer-entscheidet), steht hier und in B-681, nicht doppelt in B-644. Verworfen: B-680 mit B-644 zusammenlegen (`verworfen`, `ersetzt_durch`) – verlöre die Produktsicht (Fach, Klausur, Folgetag) und den Zuschnitt auf Etappen. Schließt aus: einen eigenen Planer-Pfad neben B-644.
2. **Der serverseitige Planer entscheidet den Tagesplan, nicht ein Sprachmodell (Rahmen A, führend B-681).** Der Tagesplan entsteht durch den Planer aus B-644 mit festen Regeln in C#; „Lehrer-Agent“ ist der Name dieses Planers gegenüber dem Kind, kein Modellaufruf. Der Konsolen-Agent (`Pugling.Agent.Creator`) bleibt das Werkzeug für Inhalte und entscheidet keine Pflicht. Begründung: Die Pflicht kostet Münzen und muss nachrechenbar und wiederholbar sein (B-644 Entscheidung 10); ein Agent mit Plan-Steuerung bräuchte außerdem ein eigenes Konto und einen Zugriff auf Kinder, den es nicht gibt (Eigentum läuft über `SupervisorLink`, `AuthAccess.cs:112`). Kosten: Der Planer ist nur so klug wie seine Regeln; ein unvorhergesehenes Muster bleibt unentdeckt, bis jemand eine Regel schreibt. Verworfen: ein Sprachmodell, das den Plan je Kind täglich selbst festlegt – nicht nachrechenbar und mit dem lokalen Modell ausfallgefährdet. Schließt aus: einen frei „erfundenen“ Plan je Kind, wie ihn der Satz „Lehrer-Agent erzeugt jeden Tag den Plan“ nahelegt; das Kind bekommt einen täglichen Plan nach seinem Lernstand, kein Modell-Ergebnis. (Beantwortet Punkt 2.)
3. **Der Vater setzt Leitplanken und hat eine Notbremse, er legt keine Tagesmengen fest (Rahmen B, führend B-681).** Je Kind setzt der Vater einmal Boden und Deckel für Pflichtaufgaben und neue Wörter, den Malus-Betrag und Pausen (B-644 Entscheidungen 2 bis 5 und 8) und behält die Notbremse: eine nicht begonnene Aufgabe streichen oder eine eigene dazulegen (B-644 Entscheidung 7). Er baut keinen Plan und bestimmt keine Tagesmenge. Begründung: Ein Planer braucht eine menschliche Grenze gegen Fehlplanung; die vier Zahlen sind die eigenen Entscheidungen des Vaters vom 2026-11-17 und bleiben unberührt; Belohnung, Shop und Einlösen bleiben seine Aufgabe, die Tagesarbeit nicht. Kosten: Der Vater trägt weiter vier Zahlen und eine Pause je Kind; „nur Belohnung“ aus dem Leitbild ist damit nicht wörtlich erfüllt (abgewichen von B-680 Punkt 3, der Boden und Deckel zu Planer-Voreinstellungen machte). Verworfen: Boden und Deckel als reine Planer-Voreinstellung – hübe B-644 Entscheidung 4 auf und ließe den Planer ohne harte Grenze. Schließt aus: einen Vater, der gar nicht mehr in die Mengen eingreifen kann; die User Story „ich möchte die Plansteuerung nicht mehr selbst tragen“ gilt für die tägliche Arbeit, nicht für die Grenzen. (Beantwortet Punkt 3.)
4. **Der Tagesplan steht ab 6 Uhr fest, und Malus kostet nur, was das Kind sehen konnte (Rahmen E, führend B-681).** Der Plan des Tages ist ab 6 Uhr Ortszeit eingefroren (B-644 Entscheidung 9); eine verpasste Pflichtaufgabe eines gesehenen Plans kostet genau einmal den Malus-Betrag des Kindes (B-644 Entscheidung 5); ein Tag, an dem nie ein Plan entstand, zählt wie eine Pause (B-644 Entscheidung 19). Begründung: Heute steht die Pflicht Wochen vorher an der Position (`PlanPosition.PenaltyCoins`); eine Pflicht, die erst heute früh entsteht, darf nicht rückwirkend oder unsichtbar kosten (Leitbild: „nicht unfair machen“). Kosten: Wird der Plan um 6 Uhr nicht erzeugt, entgeht dem Kind ein Pflichttag (als Pause beim Vater gemeldet); ein erzeugter Plan lässt sich nur über die Notbremse des Vaters ändern. Verworfen: Malus über die Pflicht an der Position auch für Planer-Kinder. Schließt aus: Strafe für Aufgaben, die das Kind nie angezeigt bekam oder die der Planer nach dem Erzeugen verändert hätte. (Beantwortet Punkt 4.)
5. **An die Stelle von „KI-Plan entsteht inaktiv“ treten eine Woche Schattenmodus (B-644 Entscheidung 14), danach Boden und Deckel als harte Grenzen und Rahmen G für den Inhalt.** Der Planer kann die Pflicht weder über den Deckel heben noch unter den Boden senken. Begründung: Die heutige Freigabe sichert gegen fehlerhaften Maschineninhalt (`PlanPlanner.cs:101`); der Planer ist deterministischer Code (Rahmen A), seine Gefahr ist Fehlplanung, nicht Fehlinhalt, und die fangen Schattenwoche und Grenzen. Kosten: Eine Woche, in der der Planer nichts bewirkt; ein Regelfehler innerhalb der Grenzen trifft Münzen (die Notbremse des Vaters bleibt). Verworfen: eine Freigabe je Tagesplan – sie ließe das Kind an jedem Morgen ohne Aufgaben, an dem der Vater nicht hineinsieht (B-644 Entscheidung 7). Schließt aus: einen Tagesplan, der ohne Blick des Vaters über den Deckel oder unter den Boden fällt.
6. **Münzen hängen an der erfüllten Pflicht, nicht an Notenstufe oder Klausurnote (Rahmen F, führend B-681).** Zielpunkte, Truhe und Serie bleiben wie heute an der erfüllten Pflicht; in diesem Zyklus gibt es keine Münzprämie für eine Notenstufe oder eine Klausurnote, die Notenstufe ist Auszeichnung ohne Münzwert (`RewardPoints` 0). Begründung: Die Stufe ist aus Lerndaten geschätzt und keine harte Leistung wie ein bestandener Test, die Note wird von Hand eingetragen; eine Münzprämie darauf trüge Prognosefehler und Eintragsfehler in das Guthaben (Wallet-Invarianten). Die Probeklausur B-249 ist ebenfalls ohne Münzen entschieden. Kosten: Eine gute Klausur belohnt der Vater von Hand (`POST children/{}/points`, Manual); für die Stufe gibt es Ehre statt Geld. Verworfen: Klausurprämie in Münzen. Schließt aus: ein Kind, das für eine Eins in der Klausur automatisch Münzen bekommt. (Beantwortet Punkt 6.)
7. **Der Planer läuft im Server als träger, idempotenter Aufruf beim ersten Zugriff nach 6 Uhr Ortszeit; ein kleiner Hintergrunddienst ruft dieselbe Methode um 6 Uhr (B-644 Entscheidung 9).** Begründung: Ein Codepfad, und der Plan entsteht auch, wenn der Heimserver um 6 Uhr aus war; im Backend gibt es bisher keinen Zeitgeber (`IHostedService`, `BackgroundService`, `PeriodicTimer` ohne Treffer, 2026-11-19). Kosten: Der erste Zugriff nach 6 Uhr zahlt die Rechenzeit; der Hintergrunddienst ist das erste zeitgesteuerte Stück des Backends. Verworfen: der Planer als Konsolen-Agent gegen die API – abhängig von Handstart und lokalem Modell. Schließt aus: einen Tag ohne Plan, weil niemand den Agenten gestartet hat.
8. **`StudyPlan.EndDate` wird optional, `Klassenarbeit.ScheduledDate` bleibt Pflicht.** Der Stoffrahmen bleibt der `StudyPlan` (Fach, Unit, Start); ein Plan ohne Termin trägt kein `EndDate`. Ein noch nicht feststehender Klausurtermin heißt: noch keine Klassenarbeit angelegt, sie entsteht beim Nachtragen. Begründung: B-644 Entscheidung 1 (Termin optional, kommt oft später); das ist die kleinste Änderung am Modell, ohne zweites Aggregat mit eigenem Eigentumsfilter und eigener Aktivierungsregel je Fach (B-539). Kosten: `EndDate` wird an 29 Fundstellen gelesen (Api und Contracts ohne Migrationen und Tests, Zählung 2026-11-20; Api-Dateien: PlanOverviewController, StudentPlansController, StudyPlansController, MediaDemandService, PositionPlayService, PositionProgressService, ChildrenDashboardService) – jede braucht den Fall „ohne Ende“; im Vertrag wird das Feld nullable (Vertragsbruch), dazu eine Migration. Verworfen: eine eigene, schlankere Stoffrahmen-Entität mit Wegfall des `StudyPlan` zum Stichtag – zweite Gruppe für die Aktivierungsregel, zweite Eigentumsprüfung, und die Positionen hängen am Container. Schließt aus: einen Stoffrahmen, der ein Pflicht-Enddatum vortäuscht, und eine Klassenarbeit ohne Datum (Termin unbekannt heißt: keine Klassenarbeit). (Beantwortet Punkt 8; gleichlautend B-644 Entscheidung 17.)
9. **Ein freier Plan ohne Klausur ist derselbe Planer ohne Rückwärtsrechnung:** Das Tempo kommt aus dem Leistungsprofil zwischen Boden und Deckel (B-644 Entscheidung 2), es gibt keinen eigenen Pfad. Begründung: Eine zweite Regelmenge für „frei“ würde doppelt gepflegt und könnte vom Hauptpfad abweichen. Kosten: Ohne Termin fehlt der Druck; das Tempo hängt am Kind und am Boden. Verworfen: der freie Plan als eigener Modus. Schließt aus: ein Fach ohne Klausur, dessen Stoff nie wieder drankommt – der Boden gilt über alle Fächer, und die Verteilung nach Schwäche (B-644 Regel 5) holt es zurück.
10. **Die Notenstufe ist kein Eingang des ersten Baus.** Der Planer rechnet auf Termin, Stoff und Beherrschung der Wortliste (Anteil `MasteredItems` am Stoff, B-682 Entscheidung 1). Begründung: Das Prognosemodell „Lernstand → Note“ gibt es nicht, und B-682 darf B-680 nicht blockieren. Kosten: Das Leitbild „der Planer arbeitet auf die Zielnote hin“ wird erst in einer späteren Ausbaustufe erfüllt; bis dahin plant er auf „Stoff bis Termin beherrscht“. Verworfen: den Planer erst nach B-682 bauen. Schließt aus: eine Planmenge, die von einer Notenstufe abhängt, solange es keine gibt.
11. **B-680 bekommt keine eigene Karte (`/wayfinder`).** Die Fragen sind entschieden, die Etappen E0 bis E5 von B-644 sind der Schnitt. Begründung: Eine Karte lohnt, wenn die Punkte voneinander abhängen und man die Kette nicht sieht (README); hier hängen sie an Rahmenentscheidungen, die gefallen sind. Kosten: Die Entscheidungen sind stellvertretend und unbestätigt; widerspricht der Nutzer bei den Rahmenentscheidungen A bis C, wird doch eine Karte nötig. Verworfen: eine Karte vor dem Grillen. Schließt aus: Ticket-Dateien unter `karten/B-680/`.
12. **Der Tagesplan ist eindeutig je Kind und Lerntag; das Fach ist Eigenschaft der Aufgabe, nicht des Plans.** Boden und Deckel gelten über alle Fächer (B-644 Entscheidung 4), die Verteilung folgt Stundenplan und Schwäche (B-644 Regeln 3 bis 5). Begründung: So steht es in B-644, und eine Pflicht je Fach und Tag würde die vier Zahlen je Kind zu vier je Fach vervielfachen. Kosten: Ein Fach kann an einem Tag keine Pflichtaufgabe haben; die User Story „jeden Tag für jedes Fach“ gilt als „für die Fächer, die laut Verteilung dran sind“. Verworfen: ein Pflichtminimum je Fach und Tag. Schließt aus: ein Fach, das jeden Tag drankommen muss, auch wenn der Stundenplan es nicht verlangt und nichts fällig ist.
13. **Klausurtermin und Note trägt der Vater ein (Rahmen D, führend B-681).** Die Schreibrouten der Klassenarbeit bleiben `Roles.Supervisor` (`KlassenarbeitenController.cs:92`, `:130`); Planer, Agent und eine spätere Foto-Auswertung (B-684) lesen Termin und Note oder schlagen sie nur vor, die Freigabe bleibt beim Vater. Begründung: Er hat den Schulkontakt (WebUntis, Heft, Klausur), und eine Note beeinflusst Stufe und Pflicht – kein Konto, das davon profitiert oder Pflichten setzt, trägt sie selbst ein. Kosten: ein Handgriff mehr nach jedem Scan; solange B-684 fehlt, bleibt das Eintragen Handarbeit. Verworfen: Note direkt aus dem Scan übernehmen. Schließt aus: ein Kind oder einen Agenten, der die eigene Note einträgt (Selbstbetrug).

## Beziehungen zu anderen Stories (nur gelesen)

- **[B-644](B-644-tagesplaner-statt-fester-lauf.md)** (`gegrillt`, P1, 14 Entscheidungen des Vaters und sechs stellvertretende): große
  inhaltliche Überschneidung, siehe oben. Konflikte mit dem Leitbild: Entscheidung 7 (Vater kann eingreifen) und 10
  (Regeln statt Sprachmodell).
- **[B-681](B-681-rollenschnitt-vater-belohnt-lehrer-unterrichtet.md)** (`gegrillt`): entscheidet, wer den Plan steuert;
  Voraussetzung für Punkt 3.
- **[B-682](B-682-notenstufe-je-fach-statt-liga.md)** (`gegrillt`): liefert das Ziel (Notenstufe), von dem der Plan
  zurückrechnet; B-680 darf nicht darauf warten (Punkt 10).
- **[B-683](B-683-notenstufe-an-der-echten-klausur-eichen.md)** (`gegrillt`): die echte Note eicht die Prognose; liest
  `Klassenarbeit.Grade`, das schon vorhanden ist.
- **[B-684](B-684-korrigierte-klausur-einscannen.md)** (`idee`, P3): bringt Note und Fehler an die Klassenarbeit;
  die Fehlerart ist auch ein Eingang des Leistungsprofils in B-644.
- **[B-248](B-248-klassenarbeit-countdown-treibt-den-plan.md)** (`idee`): die Klassenarbeit verdichtet den Plan – geht
  in B-644 (Regel 4) auf. **[B-618](B-618-faecher-verteilung-backend.md)** (`geschaetzt`, wartet auf B-644): Verteilung
  über die Fächer. **[B-630](B-630-rueckmeldung-im-ki-creator.md)** (`geschaetzt`): Rückmeldungen im KI-Creator.

## Akzeptanzkriterien

Final. Je Kriterium die Änderung, die es rot macht.

1. Je Kind und Lerntag entsteht genau ein gespeicherter Tagesplan (die Aufgaben tragen das Fach); zweimaliges Auslösen legt keinen zweiten an. *Rot, wenn* der Unique-Index auf (Kind, Tag) fehlt oder der Aufruf nicht idempotent ist.
2. Bei gleicher Stoff-Wortliste ergibt „Termin in 3 Tagen“ mehr neue Wörter als „Termin in 3 Wochen“, bis zum Deckel. *Rot, wenn* der Planer den Termin ignoriert.
3. Ohne Termin (Plan ohne `EndDate`, keine Klassenarbeit) ist der Tagesplan nicht leer und liegt mindestens am Boden. *Rot, wenn* der Boden nicht angewandt wird.
4. Ein nachgetragener oder verschobener Termin ändert den Plan des Folgetags, nicht den eingefrorenen laufenden. *Rot, wenn* der heutige Plan neu gerechnet wird.
5. Ein zweiter Abruf am selben Tag liefert dieselbe Zusammenstellung, auch wenn sich der Lernstand dazwischen ändert. *Rot, wenn* der Plan beim Abruf neu gerechnet wird.
6. Eine bis Tagesende nicht erledigte Pflichtaufgabe kostet genau einmal den Malus-Betrag, eine erledigte nichts, ein Tag ohne erzeugten Plan nichts. *Rot, wenn* der Malus für einen nie gesehenen Plan oder doppelt gebucht wird.
7. Gleiche Daten und gleiche Uhr ergeben denselben Plan; der Planer-Dienst hängt weder von einem Chat-Client noch von ungesätem Zufall ab. *Rot, wenn* eine solche Abhängigkeit in den Konstruktor kommt oder zwei Läufe mit `FakeTimeProvider` verschiedene Pläne liefern.
8. Jede Aufgabe trägt eine Begründung, die aus den Eingangsdaten entsteht (Klausur in n Tagen, Wort schwach). *Rot, wenn* der Text konstant ist oder bei verschiedenen Ausgangsdaten gleich lautet.
9. Die Pflicht liegt immer zwischen Boden und Deckel; liegt das nötige Tempo über dem Deckel, bekommt der Vater eine Meldung mit der fehlenden Menge. *Rot, wenn* der Deckel überschritten oder die Meldung unterdrückt wird.
10. *Absichtserklärung.* Der Planer-Dienst hat keine Abhängigkeit auf einen Notenstufen-Dienst. *Rot, wenn* der Konstruktor einen solchen erhält, bevor B-682 gebaut und bestätigt ist.

## Schätzung

*Geschätzt im Vorbereitungslauf 2026-11-20 (Sonnet), stellvertretend, unbestätigt.* B-680 ist die Produktseite von
[B-644](B-644-tagesplaner-statt-fester-lauf.md) (Entscheidung 1) und trägt nach der Teilung von B-644 genau die **Planer-Regeln, die den
Termin, den Stundenplan und die Fächer betreffen** (Regeln 3 bis 5 und Teilprüfung aus B-644, Etappe E4a). Sie ist darum
keine zweite Planer-Story, sondern der Aufsatz auf den Planer-Kern [B-734](B-734-planer-kern-und-schattenmodus.md).

**Größe: L** — Anker: eine DB-Umbau-Etappe wie E6, ohne Schema: drei neue Eingänge des Planers (Klassenarbeiten, Stundenplan, Schwäche
je Fach), eine Rückwärtsrechnung mit Meldung, Regeln mit vielen Grenzfällen.

- **`migration: nein`** — die Meldung „nötiges Tempo über dem Deckel" liegt in der JSON-Spalte `Warnings` von `DailyPlan`, die
  [B-734](B-734-planer-kern-und-schattenmodus.md) anlegt; `Klassenarbeit` und `TimetableEntry` bleiben unverändert (nachgesehen:
  `Models/KlassenarbeitEntities.cs:67`, `:79`, `:92`, `:106`; `Models/TimetableEntities.cs:7`, `:14`).
- **`vertragsbruch: nein`** — kein Vertragstyp ändert sich; die Warnungen erscheinen im Antworttyp von `daily-plans` aus B-734.
- **`wo: backend`** — die Oberfläche (Plan mit Warnung) liefert B-734; Eingriffe und Sohn-Anzeige liefert
  [B-738](B-738-eingriffe-bonus-und-sohn-begruendung.md).

### Zuordnung der Akzeptanzkriterien

Die Kriterien oben sind der Produktschnitt über mehrere Teilstories; jedes hat genau einen Ort, an dem es gebaut und rot geprobt wird:

| Kriterium | Wo gebaut |
| --- | --- |
| 1 (ein Tagesplan je Kind und Tag) | [B-734](B-734-planer-kern-und-schattenmodus.md) Kriterium 1 |
| 2 (Termin treibt die Menge), 4 (Nachtrag wirkt am Folgetag), 9 (Deckel und Meldung) | **diese Story** |
| 3 (ohne Termin mindestens der Boden) | diese Story, mit dem offenen Plan aus [B-735](B-735-stoffrahmen-ohne-termin.md) |
| 5 (zweiter Abruf liefert dasselbe), 7 (deterministisch) | B-734 Kriterien 6 und 5 |
| 6 (Malus genau einmal, nie für unsichtbare Pläne) | [B-736](B-736-pflicht-aus-der-tagesaufgabe.md) |
| 8 (Begründung) | B-734 Kriterium 4, hier um Termin- und Stundenplan-Bausteine erweitert |
| 10 (keine Notenstufen-Abhängigkeit) | diese Story als Absichtserklärung |

Dazu kommen hier die Kriterien aus B-644, die der Termin und der Stundenplan tragen: Teilprüfung (B-644 Kriterium 16), Stundenplan-Vorrang
(Kriterium 9), nachgetragener Termin ändert den Folgetag (Kriterium 8). Sie sind unten mit der roten Änderung geschrieben.

### Zusätzliche Kriterien dieser Story

Die Nummern setzen die Kriterien oben fort.

<!-- markdownlint-disable MD029 -->

11. Der Planer liest **mehr als eine** Klassenarbeit je Fach (Teilprüfung): Eine nahe Teilprüfung über eine Übung verdichtet den Plan
    auf ihren Termin vor der späteren Klausur. *Rot, wenn* nur die späteste Klassenarbeit wirkt.
12. Das Fach von heute und morgen laut Stundenplan hat in der Verteilung Vorrang; ein Fach ohne Stundenplan-Eintrag bleibt über den
    Boden dran. *Rot, wenn* der Stundenplan ignoriert wird oder ein Fach ohne Eintrag nie vorkommt.
13. Die Verteilung über die Fächer folgt der Schwäche (Beherrschung aus dem Profil, [B-733](B-733-tagesauswertung-und-leistungsprofil.md)),
    innerhalb von Boden und Deckel. *Rot, wenn* bei gleichen Daten das schwächere Fach nicht mehr Aufgaben bekommt.
14. Ein nachgetragener Termin ändert den Plan des Folgetags und nie den eingefrorenen heutigen (Kriterium 4 prüft genau diese
    Trennung mit einem Nachtrag mitten am Tag). *Rot, wenn* der heutige Plan neu gerechnet wird.

<!-- markdownlint-enable MD029 -->

### Risiken

- **Klassenarbeits-Bündel auf Übungsebene:** Eine Teilprüfung ist nur so fein wie die Übung (Entscheidung 16 in B-644). Die geladenen
  Bücher schneiden die Wortlisten je Abschnitt (ein Beispiel aus `scripts/friedrich-vokabeltest-2026-11-24.ps1`: Unité 5 als zwei
  Übungen mit 10 und 29 Wörtern); eine Funktion „Übung teilen" fand die Suche in `Controllers/Creator/` **nicht**. Die echte
  Datenbank ist hier absichtlich nicht geprüft.
- **Unbestimmte Rückwärtsrechnung:** „Nötiges Tempo" mit mehreren Klassenarbeiten und mehreren Fächern ist eine Verteilungsaufgabe, kein
  Dreisatz; die Regel gehört vor dem Bau auf Papier und in einen reinen Funktionstest ohne Datenbank.
- **Stundenplan als Eingang:** Der Stundenplan wird heute nur gespeichert und gelesen (`Controllers/Supervisor/TimetableController.cs:25`),
  kein Dienst nutzt ihn; sein Format (Wochentag, optionale Uhrzeit) trägt „morgen" ohne Weiteres.
- **Schattenwoche:** Die Regeln sollten laufen, bevor der Vater den Stichtag setzt. B-680 Entscheidung 1 baut sie erst „nach E0 bis E2";
  das ließe die Schattenwoche ohne diese Regeln laufen. Entschieden am 2026-11-20 (stellvertretend, auf Anweisung des Nutzers): Der Schattenmodus wird erst eingeschaltet, wenn B-735 und die Regeln dieser Story gebaut sind; B-734 liefert den Kern, der Schattenmodus bleibt bis dahin aus.

### Angriffsplan

1. Reine Regelfunktionen (Termin-Tempo, Fächerverteilung, Stundenplan-Vorrang) mit Funktionstests ohne Datenbank.
2. Anbindung an den Planer-Kern aus B-734; Eingänge: Klassenarbeiten des Kindes, Stundenplan, Profil.
3. Warnung „nötiges Tempo über dem Deckel" in `Warnings`; Begründungsbausteine für Termin und Fach.
4. Integrationstests über die Student- und Supervisor-API.

### Testweg

- Neue Klasse `PlanerTerminRegelnTests` (Unit, ohne Datenbank): Termin in 3 Tagen gegen 3 Wochen bei gleicher Wortliste, ohne Termin am
  Boden, Deckel und Meldung. Rote Probe je Kriterium: den Termin aus dem Eingang entfernen lässt Kriterium 2 fallen; den Deckel
  entfernen Kriterium 9.
- `PlanerFaecherTests` (Stundenplan-Vorrang, Schwäche, mehrere Klassenarbeiten); Muster `TagsRatingsTimetableTests` (Stundenplan) und
  `KlassenarbeitenTests` (Klassenarbeiten) für den Aufbau.
- `PlanerNachtragTests` (Integration, `TestClock`): Termin nachtragen, heutiger Plan unverändert, Plan des Folgetags geändert.
- Kriterium 10: Reflexionsprüfung in `ConventionGuardTests`, dass der Konstruktor des Planers keinen Notenstufen-Dienst trägt (Rot-Probe:
  einen solchen Parameter einführen).
- Vor der Abnahme: `pugling-reviewer`. Ein Rollengang als Vater (Termin nachtragen, Plan des nächsten Tages) entfällt ohne
  Oberfläche nicht: der Plan-Verlauf aus B-734 zeigt die Warnung; E2E `frontend/e2e/tagesplan-schatten.spec.ts` wird um den Termin
  erweitert.

### Reihenfolge und Abhängigkeiten

Nach [B-734](B-734-planer-kern-und-schattenmodus.md) und [B-735](B-735-stoffrahmen-ohne-termin.md); vor [B-736](B-736-pflicht-aus-der-tagesaufgabe.md).
Die Notenstufe ([B-682](B-682-notenstufe-je-fach-statt-liga.md)) ist keine Voraussetzung.

## Verlauf

- **2026-11-27** — Befund aus dem Rollengang von B-734 (Sprint 195, Kopie der echten DB): Der Schattenplan für Friedrich trägt
  132 fällige Wiederholungen an einem Tag, davon 58 in einer Position. Wiederholungen zählen nach B-734 bewusst nicht gegen den
  Wörter-Deckel, und der Aufgaben-Deckel begrenzt nur die Zahl der Aufgaben, nicht ihren Umfang. Die Begrenzung des Rückstands
  gehört zu dieser Story (B-734, Entscheidung 8).

- **2026-11-19** — angelegt aus dem Leitbild-Gespräch mit dem Vater ([Leitbild](../leitbild-privatlehrer.md)).
- **2026-11-19** — `idee` → `ausformuliert` (Vorbereitungslauf, Sonnet): Ist-Stand am Code belegt. Kern: Die Story
  überschneidet sich weitgehend mit der schon gegrillten B-644; neu sind nur das Ziel „Notenstufe“ und die Frage, ob ein
  Agent oder feste Regeln den Plan entscheiden. Elf offene Punkte mit Empfehlung, **nichts entschieden** (Rollenschnitt,
  Münz-Ökonomie und Malus-Regeln liegen beim Nutzer).
- **2026-11-20** — `ausformuliert` → `gegrillt` (Vorbereitungslauf, Sonnet, stellvertretend auf Anweisung des Nutzers, unbestätigt): Alle elf Punkte entschieden. Kern: B-680 ist die Produktseite von B-644 (E3/E4), der Planer entscheidet mit festen Regeln im Server, der Vater behält Leitplanken und Notbremse, Notenstufe ist kein Eingang des ersten Baus, `StudyPlan.EndDate` wird optional.
- **2026-11-20** — `gegrillt` → `geschaetzt` (Vorbereitungslauf, Sonnet, stellvertretend, unbestätigt): nach der Teilung von B-644 trägt B-680 die Planer-Regeln für Termin, Stundenplan und Fächerverteilung (E4a) als Aufsatz auf B-734. L, backend, keine Migration, kein Vertragsbruch; Zuordnung der zehn Kriterien auf B-734, B-735, B-736 und diese Story, vier Kriterien ergänzt. Eintrittsbedingung `gegrillt` erfüllt (elf Punkte entschieden).
- **2026-11-20** — Reihenfolge entschieden (stellvertretend auf Anweisung des Nutzers, unbestätigt): Die Schattenwoche startet erst, wenn B-735 und die Planer-Regeln dieser Story (Termin, Stundenplan, Fächerverteilung) gebaut sind, damit der Vater den vollständigen Planer prüft und nicht nur einen Teil. B-734 wird vorher gebaut, der Schattenmodus bleibt bis dahin ausgeschaltet. Kosten: Der Planer läuft später an; der Vorteil ist, dass die Schattenwoche die Regeln misst, die später Münzen kosten.
