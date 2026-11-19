---
tags: [typ/story, status/gegrillt, bereich/training, bereich/backend, rolle/student, rolle/supervisor]
aliases: [Mischwiederholung, Mastery Challenge, Kind-weite Wiederholung]
status: gegrillt
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/khan-academy-vergleich-plan.md#wo-khan-weiter-ist
grund: ""
ersetzt_durch: []
---

# B-218 · Eine Mischwiederholung über alle Positionen des Kindes

Leitner läuft bei uns **pro Position**: fällige Karten werden nur in der Übungsrunde genau dieser
Position wieder vorgelegt. Khan Academy legt daneben alle 12 Stunden eine „Mastery Challenge" aus sechs
Fragen zu drei Skills vor, gewählt nach Zeit seit der letzten Wiederholung und aktueller Stufe; zwei
Fehler je Skill senken die Stufe, einer wird verziehen. Die Idee: eine kind-weite Wiederholungsrunde
aus den fälligsten Items aller spielbaren Positionen, zeitlich gedeckelt, mit derselben
Server-Autorität wie die Positions-Runde. Abzugrenzen von [B-04](B-04-adaptiver-vokabel-pool.md), das
den Pool **innerhalb** einer Position adaptiv macht.

## User Story

Als *Schüler* möchte ich eine kurze Wiederholungsrunde aus den Karten, die in all meinen Positionen gerade fällig
sind, damit ich nicht für jede Position einzeln anfangen muss und nichts Fälliges liegen bleibt – und als *Vater*
möchte ich, dass diese Runde freiwillig bleibt und keine Pflicht verschiebt.

## Ist-Stand am Code

Gemessen am 2026-11-19 auf `main`; Pfade relativ zu `backend/Pugling.Api/`, wenn nicht anders genannt.

- **Eine Mischwiederholung gibt es nicht.** Keine Fundstelle für „Mixed"/„Mischwiederholung" unter `Controllers/Student`
  und `Services/`; alle Spiel-Routen hängen an einer Position
  (`Controllers/Student/PositionPracticeController.cs:19`, `…/study-plans/{planId}/positions/{positionId}/practice-sessions`).
- **Fälligkeit lebt je Position, nicht je Kind.** `PositionItemProgress.DueOn` (`Models/PlanPositionEntities.cs:256`) ist
  der Termin der Karte in dieser Position; fällig heißt „nie gesehen oder `DueOn <= Tag`"
  (`Services/Shared/PositionPlayService.cs:629-630`). Der Zähler `DueCount` im Student-Vertrag
  (`Contracts/Shared/ProgressDtos.cs:47`) wird je Position aus `DueItemIndicesAsync` gerechnet
  (`Services/Shared/PositionProgressService.cs:328`, nur bei `UseLeitner`).
- **Der plan-übergreifende Lernstand taugt nicht als Auswahl-Baustein.** `ItemProgress`
  (`Models/ItemProgressEntities.cs:18-48`) trägt Box, Beherrschung, Zähler und `LastAnswerAt`, aber **kein Fälligkeitsdatum**
  und gilt nur für Vokabel-Items (`Services/Shared/ItemProgressService.cs:41`). Der Filter `onlyWeak`
  (`Controllers/Student/ChildVocabularyProgressController.cs:47`, Schwelle `WeakBelowPercent` = 50) sagt „schwach", nicht „fällig".
  Die frühere Fassung dieser Story nannte ihn als Baustein – für eine Wiederholung nach Termin ist es `PositionItemProgress`.
- **Leitner-Bewegung ist an die Position gebunden.** `PositionPlayService.ApplyReview` (`Services/Shared/PositionPlayService.cs:657-678`)
  setzt Box und `DueOn` der Karte; die Tür dahin ist `PositionPracticeController` (Plan-Sperre `plan_inactive` bei
  `PlanPlayableForChild`, `PositionPlayService.cs:66-67`, am Start `PositionPracticeController.cs:97`). Verzeihen eines
  einzelnen Fehlers gibt es schon je Position: `ForgivingDemotion` (`Models/PlanPositionEntities.cs:129`,
  angewandt `PositionPlayService.cs:672-673`) – das Khan-Muster „einer wird verziehen" ist also kein neues Feature.
- **Aktive Pläne:** je Kind **ein aktiver Plan je Fach** (B-539; `Controllers/Supervisor/StudyPlansController.cs:166`). „Plan-weit"
  aus der Entscheidung vom 2026-10-23 meint damit heute **mehrere** aktive Pläne eines Kindes.
- **Das Bonus-Training ist die nächste Verwandte.** `ExtraTrainingController` (`Controllers/Student/ExtraTrainingController.cs:17`) mit
  `ExtraTrainingService` (`Services/Student/ExtraTrainingService.cs:28`): öffnet erst nach der Tagespflicht (`GateAsync`, Zeile 55/70),
  Tagesdeckel `DailyCoinCap` = 90 (`Services/Student/ExtraTrainingOptions.cs:14`), eigene Lauf-Entität `ExtraTrainingRun`
  (`Models/GamificationEntities.cs:124`), Pool nach Units der spielbaren Pläne (`ExtraTrainingService.cs:305`). Es bewegt **keine**
  Leitner-Box (B-533 Entscheidung 7) und wählt nach Units, nicht nach fälligen Karten.

## Die echte Lücke

Schmaler und anders als die Idee: Die Zutaten (Fälligkeit je Karte, Münzbuchung, Tagesdeckel, Lauf-Entität, Anti-Cheat-Gate)
gibt es alle – es fehlt die **Auswahl über Positionsgrenzen** und die Antwort auf die Frage, wohin die Leitner-Bewegung
einer Karte gebucht wird, wenn sie nicht in ihrer Position gespielt wurde. Das ist zugleich der Konflikt mit B-533:
eine Wiederholung, die keine Box bewegt, wiederholt nichts.

Ob es überhaupt eine **zweite** Münz-Kürrunde braucht oder ein Modus „fällig" des Bonus-Trainings genügt, ist die
Kernfrage (Offener Punkt 1).

## Offene Punkte

Bereits entschieden (Nutzer, 2026-10-23, siehe Verlauf): freiwilliges Angebot mit Münzen, kein Malus, zählt nicht auf Pflichten.
Offen ist, was daraus für die Umsetzung folgt:

1. ~~**Eigene Runde oder Modus „fällig" im Bonus-Training?** Empfehlung: Modus im Bonus-Training – Gate, Deckel, Lauf-Entität und
   Halbierungsregel (B-538) sind dort schon gebaut und getestet; ein zweites System würde zwei Münz-Kürrunden mit verschiedenen
   Regeln erzeugen. Berührt die Münz-Ökonomie (zusätzliche Münzquelle): Entscheidung gehört dem Nutzer.~~ → Entscheidung 1.
2. ~~**Bewegt die Runde die Leitner-Box der Position?** Empfehlung: ja, über `ApplyReview` auf der `PositionItemProgress` der
   Herkunftsposition, sonst ist es keine Wiederholung; das hebt B-533 Entscheidung 7 **für diesen Modus** auf. Gegenstück: die Runde
   zahlt Münzen nur für geprüfte Treffer und erneuert nie die Pflicht-Periode einer Position.~~ → Entscheidung 2.
3. ~~**Welche Pläne und Positionen?** Empfehlung: alle spielbaren Pläne des Kindes (`PlanPlayableForChild`), nur Positionen mit
   `UseLeitner`; Auswahl nach Überfälligkeit (`DueOn` am weitesten zurück), dann niedrigste Box; Rundengröße 10 Karten
   (Khan: 6 Fragen), als Option statt Konstante.~~ → Entscheidung 3.
4. ~~**Wie wird die Antwort gewertet, wenn die Karte in der Position eine andere Stufe hätte?** Empfehlung: die Stufe der Position am
   Tag (`StageForDay`, `PositionPlayService.cs:73`) – nie leichter, sonst wäre die Runde ein Schlupfloch für die Tipp-Stufe.~~ → Entscheidung 4.
5. ~~**Zählt die Runde für Serie, Truhe oder Tagesziel?** Empfehlung: nein (Entscheidung 2026-10-23 „zählt nicht auf Pflichten"),
   Lesart gegen B-606 (Serie/Truhe über alle Pläne) beim Grillen prüfen.~~ → Entscheidung 5.
6. ~~**Nur Vokabel oder alle Typen?** Empfehlung: nur Typen mit Karten-Atomen und `PositionItemProgress` (Vokabel; Lückentext nur,
   wenn er Atome führt) – beim Schätzen am Typ-Manifest nachsehen.~~ → Entscheidung 7.

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-20 (Sonnet) auf Anweisung des Nutzers (Empfehlung übernommen), unbestätigt.*

Schon entschieden und hier nicht angefasst: die zwei Entscheidungen des Nutzers vom 2026-10-23 (Verlauf): plan-weit, freiwilliges
Angebot mit Münzen, kein Malus, zählt nicht auf Pflichten.

**Querfrage mit [B-660](B-660-tagesaufgaben-beliebig-oft-mit-sinkenden-muenzen.md) (dort Entscheidungen 1, 4 und 5, gleiche Antwort):**
Es gibt **einen** Münzweg für Zusatzrunden, das Bonus-Training, und **einen** Tagesdeckel. Eine **Wiederholung** bewegt keinen
Kasten (B-660); die **Mischwiederholung** bewegt ihn, weil sie die fällige Leitner-Wiederholung ist. **Überschneidung mit
abgenommenen Entscheidungen, nicht geändert:** B-533 Entscheidung 7 sagt „eine Bonus-Runde bewegt keine Leitner-Box“. Sie gilt
unverändert für die Bonus-Runde über eine Übung. Die Mischwiederholung ist **keine** Bonus-Runde in diesem Sinn; sie teilt nur
Sperre, Deckel und Münzart (Entscheidung 1). Die Empfehlung im Entwurf („hebt B-533 Entscheidung 7 für diesen Modus auf“)
wird deshalb nicht übernommen; die Wirkung ist dieselbe, der Wortlaut von B-533 bleibt wahr.

1. **Eine Spielart neben dem Bonus-Training, mit dessen Sperre, Deckel und Münzart, kein zweites System.** Die Runde öffnet
   erst nach den Tageszielen (`GateAsync`, `ExtraTrainingService.cs:258-270`), zählt gegen **denselben** Tagesdeckel
   (`CoinsTodayAsync`, `:299`, muss die Mischrunden mitsummieren) und bucht `PointKind.ExtraTrainingCoins`. *Begründung:* Zwei
   Münz-Kürrunden mit verschiedenen Regeln wären die Kosten des Gegenentwurfs; Gate, Deckel und Halbierungsregel (B-538) sind
   dort gebaut und getestet. *Kosten:* Die Runde umfasst mehrere Übungen, `ExtraTrainingRun` trägt heute genau eine
   (`Models/GamificationEntities.cs:124`); ob eine nullbare Übung oder eine eigene Lauf-Entität, entscheidet das Schätzen,
   Migration wahrscheinlich. Die Deckel-Summe über beide Quellen ist die feste Vorgabe. *Verworfen:* eigene Runde mit eigenem
   Deckel. *Schließt aus:* eine Mischrunde vor der Tagespflicht. *Gegen die User Story gerechnet:* „nichts Fälliges bleibt
   liegen“ gilt für Karten, die kein offenes Pflichtziel tragen (Entscheidung 3); Fälliges einer offenen Pflicht spielt die
   Pflicht selbst.
2. **Die Runde bewegt den Kasten der Herkunftsposition**, über `ApplyReview` auf der `PositionItemProgress` dieser Position
   (`PositionPlayService.cs:657-678`): richtig → eine Box höher und neues `DueOn`; falsch → Box 1 bzw. verzeihend abgestuft
   (`ForgivingDemotion`, `:672-673`), `DueOn` heute. *Begründung:* Eine Wiederholung, die keine Box bewegt, wiederholt nichts.
   *Kosten:* Zwei Quellen schreiben denselben Kasten; die Position bleibt die einzige Regel dafür (keine eigene Abstiegsregel).
   *Verworfen:* die Buchung auf `ItemProgress` (der plan-übergreifende Stand hat kein Fälligkeitsdatum,
   `ItemProgressEntities.cs:18-48`). *Schließt aus:* ein Verlängern oder Verkürzen des Abstands durch die Mischrunde
   selbst; sie ist nur eine zusätzliche Gelegenheit, die Karte zu spielen.
3. **Auswahl: nur Karten aus spielbaren Plänen, Positionen mit `UseLeitner` und ohne offene Pflicht der laufenden Periode.**
   Gewählt wird nach Überfälligkeit (`DueOn` am weitesten zurück), dann niedrigste Box; Rundengröße 10 als Option
   (`ExtraTrainingOptions`, Khan: 6). Ausgeschlossen sind Positionen, deren Pflichtziel der laufenden Periode noch offen ist.
   *Begründung (am Code gefunden):* Eine **leere** Lern-Runde einer Position mit Pool gilt als erfüllt
   (`PositionProgressService.cs`, `PlayedRoundMeetsGoalAsync`: „nothing was due“). Äße die Mischrunde die fälligen Karten
   einer Wochenpflicht vorher auf, wäre diese Pflicht durch das bloße Öffnen und Schließen einer leeren Runde erfüllt. Der
   Fall ist erreichbar, weil die Sperre schon durch die **täglichen** Ziele öffnet. *Kosten:* Karten einer offenen Pflicht
   bleiben bis zu deren eigener Runde liegen; Kür-Positionen und erfüllte Pflichten sind dabei. *Verworfen:* alle Positionen
   mischen und `GoalMet` getrennt prüfen: die Abhängigkeit bliebe unsichtbar. *Schließt aus:* die offene Wochenpflicht „vorab
   wegwiederholen“.
4. **Gewertet wird in der Stufe der Position am Tag** (`StageForDay`, `PositionPlayService.cs:73`), nie leichter. *Kosten:*
   eine Karte kann in einer Mischrunde in der schweren Stufe kommen, in der Position aber noch leicht stehen; die Runde richtet
   sich nach der Position. *Verworfen:* eine Einheitsstufe: sie wäre ein Schlupfloch für die Tipp-Stufe.
5. **Die Runde zählt nicht auf Serie, Truhe, Tagesziel und schreibt keine Lern-Sitzung.** Sie legt **keine**
   `PracticeSession` mit `Mode = Lern` an (das ist es, was die Pflicht misst), und `GoalMet` und `SettleClosedPeriodsAsync`
   bleiben unberührt. Serie und Truhe (B-606) lesen die Pflicht der Tage, nicht diese Runde. *Kosten:* Der Lernerfolg zeigt
   sich im Kasten und in den Münzen, nicht in der Tagessicht. *Verworfen:* die Runde als Lern-Sitzung der Position buchen
   (ließe sie als gespielte Pflicht-Runde zählen). *Schließt aus:* die Mischrunde als Weg, eine Pflicht zu erfüllen.
6. **Münzen nur für geprüfte Treffer, in der Zahl der Bonus-Form, ohne Halbierung.** Pro Treffer die Rate der gespielten Form
   (`CoinsPerChoiceHit`/`CoinsPerTypedHit`), kein Combo, kein Tempo-Bonus, kein Neu-Bonus; `RunPercent` gilt hier nicht.
   *Begründung:* Jede fällige Karte zahlt höchstens einmal je Fälligkeit: nach dem Treffer liegt `DueOn` in der Zukunft
   (`ApplyReview`, richtige Antwort); ein Fehltreffer zahlt nichts. Der Deckel bleibt die äußere Grenze. *Kosten:* offen für
   das Schätzen: Ist ein Intervall der Box 2 aufwärts 0 Tage, bliebe die Karte am selben Tag fällig und könnte mehrfach
   zahlen (`BoxIntervals`, gegen die Standardwerte lesen). *Verworfen:* Halbieren je Runde: es gäbe keine „Runde derselben
   Übung“, die zählt.
7. **Nur Vokabel zunächst.** Typen mit Karten-Atomen und `PositionItemProgress`; Lückentext erst, wenn er Atome führt (beim
   Schätzen am Typ-Manifest nachsehen). *Kosten:* andere Typen haben keine Mischrunde. *Schließt aus:* gemischte Runden über
   Grammatik oder Rechnen.
8. **Die Schreibstellen der Antwort werden beim Schätzen einzeln benannt.** Hier gilt nur: Kasten ja (2), Münzen nach 6,
   Lern-Sitzung nein (5). Ob `ItemReviewEvent`/`ItemProgress` mitgeschrieben werden (`PositionPracticeController.cs:709-733`),
   entscheidet das Schätzen gegen diese drei Vorgaben.

## Akzeptanzkriterien

Je Kriterium die Änderung, die es rot macht.

1. Hat ein Kind in zwei Positionen aus zwei aktiven Plänen je eine fällige Karte ohne offene Pflicht, bietet die Runde beide
   an. *Rot, wenn die Auswahl auf eine Position beschränkt bleibt.*
2. Eine Karte, deren `DueOn` in der Zukunft liegt, kommt nicht vor. *Rot, wenn die Auswahl nach `onlyWeak` statt nach `DueOn`
   filtert.*
3. Eine fällige Karte einer Position mit **offener Pflicht der laufenden Periode** kommt nicht vor, und ihre Pflicht bleibt
   offen. *Rot, wenn die Auswahl diese Positionen nicht ausschließt (dann wird eine leere Pflicht-Runde danach als erfüllt
   gewertet).*
4. Eine richtig beantwortete Karte steigt in der Box **ihrer Position** und bekommt dort ein neues `DueOn`; die Box einer
   anderen Position derselben Übung bleibt unberührt. *Rot, wenn die Buchung auf `ItemProgress` statt
   `PositionItemProgress` geht.*
5. Eine falsch beantwortete Karte fällt nach der `ForgivingDemotion`-Regel ihrer Position. *Rot, wenn die Runde eine eigene
   Abstiegsregel mitbringt.*
6. Die Runde zahlt Münzen nur für geprüfte Treffer, ohne Combo, Tempo- und Neu-Bonus, und höchstens bis zum **gemeinsamen**
   Tagesdeckel mit dem Bonus-Training; sie öffnet nicht vor der Tagespflicht. *Rot, wenn die Deckel-Summe die Mischrunden
   nicht mitzählt (Bonus-Runde plus Mischrunde überschreiten zusammen den Deckel) oder die Runde das Gate umgeht.*
7. Die Runde legt keine `PracticeSession` mit `Mode = Lern` an und ändert `GoalMet`, Serie und Truhe nicht; verpasst das Kind
   sie, entsteht kein Malus. *Rot, wenn eine Lern-Sitzung entsteht oder `SettleClosedPeriodsAsync` die Runde liest.*
8. Die Karte wird in der Stufe der Position am Tag gestellt, nie in einer leichteren. *Rot, wenn die Runde eine eigene
   Stufe wählt.*
9. Ein Student-Aufruf mit fremdem `childId` ist verboten; der Supervisor darf lesen, nicht spielen. *Rot, wenn die Route ohne
   `ChildOwnershipFilter` hängt.*
10. Ein inaktiver oder abgelaufener Plan liefert keine Karte in die Runde. *Rot, wenn die Auswahl `PlanPlayableForChild`
    auslässt.*
11. **Bestehendes Verhalten, Absichtserklärung:** Die Bonus-Runde über eine Übung bewegt weiter keine Box (B-533 Entscheidung
    7). *Rot, wenn jemand die Box-Buchung in den Bonus-Pfad statt in die Mischspielart legt.*

## Verlauf

- **2026-10-17** — angelegt (Quelle: Khan-Academy-Vergleich, Etappe 1 des Übernahmeplans).
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): besteht (alle Student-Routen hängen an `positions/{id}`). Haken: die Anti-Cheat-Sperre „ein aktiver Plan“ (`plan_inactive`) macht daraus faktisch eine plan-weite Runde – „kind-weit“ hieße über inaktive Pläne hinweg. Grundlage: `ItemProgress` (`ItemProgressService.cs:66`). Mit B-04/B-219 zusammen grillen.
- **2026-10-23** — **Zwei Entscheidungen des Nutzers** in der Grill-Runde G-5 der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): (1) Die Mischwiederholung läuft **plan-weit**, also über alle Positionen des aktiven Plans. Das passt zur Anti-Cheat-Sperre „ein aktiver Plan je Kind“ und braucht keine Ausnahme. (2) Sie ist ein **Angebot mit Münzen**: freiwillig, bringt Münzen wie eine Übungsrunde, kein Malus, und sie zählt nicht auf die Positions-Pflichten. **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt am 2026-10-23 für alle an diesem Tag im Dialog entschiedenen Stories):** ein Nachtlauf darf diese Story auf Grundlage der Entscheidungen selbst ausformulieren, schätzen und bauen (Paket NP-18); trifft er auf eine Produktfrage, die die Entscheidungen nicht hergeben, hält er an.
- **2026-11-13** — Teilbefund am Code (Tageslauf, letzte Minuten): Auswahl-Baustein vorhanden, Spiel-Sitzung über Positionen fehlt.
- **2026-11-13** — Backlog-Abgleich gegen den Code: **gegen [B-533](B-533-bonus-training-extra-muenzen.md) abgrenzen, bevor ausformuliert wird.** Das Bonus-Training (abgenommen, `ce9b9a8d`) hat schon die Form aus Entscheidung 2 – freiwillig, Münzen, kein Malus, zählt nicht auf Pflichten –, bewegt aber bewusst keine Leitner-Box (B-533 Entscheidung 7) und wählt nach Units statt nach fälligen Items. Zu klären: eigene Runde mit eigenem Tagesdeckel und Anti-Betrug, oder Ausbau des Bonus-Trainings um einen Modus „fällige Items“ – sonst entstehen zwei Münz-Kürrunden mit verschiedenen Regeln.
- **2026-11-14** — Abgleich Backlog-Lauf: Ist-Stand bestätigt – keine Mischwiederholung im Code; Auswahl-Baustein `vocabulary-progress` jetzt `ChildVocabularyProgressController.cs:19,38`; das Bonus-Training aus B-533 liegt unter `ExtraTrainingController.cs:16` (eigene Runden, keine Leitner-Bewegung). Die Abgrenzung gegen B-533 bleibt vor dem Ausformulieren offen.
- **2026-11-19** — ausformuliert (Vorbereitungslauf, Sonnet): Ist-Stand neu am Code gemessen. Korrektur an der Notiz: `ItemProgress` hat kein Fälligkeitsdatum, die Fälligkeit liegt je Position (`PositionItemProgress.DueOn`); „plan-weit" heißt seit B-539 mehrere aktive Pläne. Sechs offene Punkte mit Empfehlung, keine Entscheidung getroffen; Münz-Ökonomie-Frage (Offener Punkt 1/2) für den Nutzer. Überschneidung: B-533 (Bonus-Training), B-644/B-680 (Tagesplaner nennt B-218 als Zulieferer).
- **2026-11-20** — gegrillt (Vorbereitungslauf, Sonnet, auf Anweisung des Nutzers „Entscheide die offenen Fragen nach deiner Empfehlung“; unbestätigt): Spielart neben dem Bonus-Training mit dessen Sperre, gemeinsamem Deckel und `ExtraTrainingCoins`; bewegt den Kasten der Herkunftsposition (B-533 Entscheidung 7 bleibt wörtlich wahr, die Runde ist keine Bonus-Runde); Auswahl schließt Positionen mit offener Pflicht aus (leere Pflicht-Runde gilt sonst als erfüllt); keine Lern-Sitzung. Akzeptanzkriterien final.
