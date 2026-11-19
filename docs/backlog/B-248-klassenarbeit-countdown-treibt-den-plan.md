---
tags: [typ/story, status/ausformuliert, bereich/klassenarbeit, bereich/training, rolle/student, rolle/supervisor]
aliases: [Klausur-Countdown, Klassenarbeit treibt den Plan]
status: ausformuliert
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/knowunity-vergleich-plan.md#wo-knowunity-weiter-ist
grund: ""
ersetzt_durch: []
---

# B-248 · Die Klassenarbeit wird beim Kind sichtbar und verdichtet den Plan vor dem Termin

Eine `Klassenarbeit` trägt ein `ScheduledDate` und ein Übungsbündel (direkt über `KlassenarbeitExercise`
und über Tags via `KlassenarbeitTag`), wirkt aber auf nichts: der Plan ändert sich vor dem Termin nicht,
und die Sohn-App zeigt die Arbeit nicht an (in `frontend/src/sohn/` kommt sie nur in einem Kommentar in
`SohnTest.tsx` vor). Knowunity baut den Tagesplan aus dem Prüfungstermin und verdichtet ihn, je näher er
rückt. Die Idee: (a) Countdown in der Sohn-Arcade („Englisch-Arbeit in 5 Tagen"), (b) eine
Bereitschaftsanzeige „wackelig → sicher" über die Items des Bündels, (c) die Positionen, deren Übungen
im Bündel liegen, werden in einem Fenster vor dem Termin stärker gewichtet – etwa durch eine zusätzliche
Tagesmission oder ein angehobenes Ziel. Der Vater setzt den Termin, die App erzwingt die Vorbereitung:
das ist die Kernidee in Reinform. Offen ist, ob die Verdichtung Pflicht (mit Malus) oder Angebot (mit
Bonus) sein soll und wer das Fenster festlegt.

## User Story

Als *Schüler* möchte ich sehen, wann meine nächste Klassenarbeit ist und wie sicher ich in ihrem Stoff schon bin, damit ich weiß, wofür ich
gerade lerne – und als *Vater* möchte ich, dass vor dem Termin eine zusätzliche Pflicht über den Stoff der Arbeit entsteht, damit die App die
Vorbereitung erzwingt, die ich mit dem Termin angekündigt habe.

## Ist-Stand am Code

Gemessen am 2026-11-19 auf `main`; Pfade relativ zu `backend/Pugling.Api/` bzw. `frontend/src/`, wenn nicht anders genannt.

- **Die Klassenarbeit ist reine Vater-Sache.** Entität `Klassenarbeit` mit `ScheduledDate`, `Status`, `Grade`, Fach optional
  (`Models/KlassenarbeitEntities.cs:67-88`), Bündel aus `KlassenarbeitExercise` (`:92`) und `KlassenarbeitTag` (`:106`). Der einzige Controller liegt unter
  `api/v1/supervisor/class-tests` (`Controllers/Supervisor/KlassenarbeitenController.cs:18`); lesen und schreiben prüfen Betreuung über
  `AuthAccess.OwnsChildAsync` (`:44`, `:65`, `:99`), Schreiben zusätzlich `Roles.Supervisor` (`:92`). Ein Student-Endpunkt oder -DTO zur Klassenarbeit:
  nicht gefunden (`Controllers/Student`, `Contracts/Student`).
- **Sohn-App kennt sie nicht.** Unter `sohn/` kommt „Klassenarbeit" nur als Kommentar vor (`sohn/SohnTest.tsx:43`).
- **Das Bündel wird aufgelöst, aber nur dort.** `LoadRelevantExercisesAsync` (`KlassenarbeitenController.cs:316-327`, **privat**): direkte Übungen
  plus alle Übungen, die einen verlinkten Tag tragen; die Route `…/{id}/practice` liefert sie mit `DaysUntil` (`:260-269`,
  `Contracts/Supervisor/ClassTestDtos.cs`, `PracticeResponse`). Das „geteilt mit B-39" der Notiz stimmt nicht: außerhalb des Controllers hat die Auflösung
  keinen Nutzer; `ExerciseUsageQueries` zählt nur `KlassenarbeitExercise` (`Services/Shared/ExerciseUsageQueries.cs:71-94`), nicht die Tags.
- **Wirkung heute:** nur die Note, über `ObjectiveEvaluationService` (`Services/Shared/ObjectiveEvaluationService.cs:89-90`), und `repeat`
  (`KlassenarbeitenController.cs:276`). Plan und Pflicht bleiben unberührt.
- **Pflicht wird aus Positionen gemacht.** Eine Position bindet **eine** Übung an einen Plan mit Takt (`Models/PlanPositionEntities.cs:24,51`),
  Malus `PenaltyCoins`; ob sie am Tag eine Pflicht ist, sagt `PositionProgressService.HasDutyOn`/`IsObligationOn` (`Services/Shared/PositionProgressService.cs:53-72`).
  Ein **Pflicht-Ende** gibt es nur als `UntilDone` mit `CompletedOn` für Nacharbeit (`Models/PlanPositionEntities.cs:140-146`); ein „Pflicht bis zum
  Termin" ist nicht gebaut.
- **Es gibt einen Vorläufer für „Position aus Bündel erzeugen".** `POST …/positions/from-words`
  (`Controllers/Supervisor/PlanPositionsController.cs:275`, B-584) baut eine Übung und hängt sie als Tagespflicht an; `review_already_in_plan` verhindert Dubletten.
- **Nur ein aktiver Plan je Kind und Fach** (`Controllers/Supervisor/StudyPlansController.cs:166`); `Klassenarbeit.SubjectId` ist optional, ein passender Plan
  kann also fehlen. Der Assistent aus B-423 (abgenommen) legt Termin, Klassenarbeit und Stufenleiter an (`VaterWizard.tsx`), setzt aber nichts bei Kind.
- **Bereitschaft:** ein Beherrschungsmaß gibt es nur für Vokabel-Items (`ItemProgress`, `Models/ItemProgressEntities.cs:18-48`, Schwelle 50 %
  `WeakBelowPercent`). Für andere Übungstypen: nicht gefunden.

## Die echte Lücke

Drei getrennte Lücken, die die Idee bündelt:

1. **Sichtbarkeit (a):** Ein Student-Lesepfad auf die nächste Klassenarbeit des Kindes mit `DaysUntil`. Klein, ohne Münz-Wirkung.
2. **Bereitschaft (b):** nur für Vokabel-Bündel berechenbar; für den Rest gibt es keinen Messwert.
3. **Verdichtung (c):** die eigentliche Pflicht-Änderung (Entscheidungen 2026-10-23/11-05: Pflicht mit Malus, Fenster 7 Tage Default, tägliche Runde über das Bündel,
   Standard-Malus). Dafür fehlen (i) ein Weg, aus dem Bündel Positionen im aktiven Plan zu machen, (ii) ein **Pflicht-Ende am Termin** und
   (iii) eine Antwort darauf, wer das auslöst, weil es keinen Scheduler gibt (`PositionProgressService` rechnet lazy).

Zu (c) kreuzen sich zwei offene Richtungen: B-680/B-644 wollen den Tagesplan je Fach rückwärts von der Klausur erzeugen. Ob (c) neben oder in ihnen steht, entscheidet der Nutzer.

## Offene Punkte

Bereits entschieden (Nutzer 2026-10-23, Delegation 2026-11-05, siehe Verlauf): Pflicht mit Malus, festes Fenster mit Default (Anhaltspunkt 7 Tage) und
Änderung durch den Vater, tägliche Übungsrunde über das Bündel, Bündel-Übungen als Positionen im aktiven Plan, Malus = Standard-Malus. Offen:

1. **Aufteilen in drei Stories?** Empfehlung: ja – (a) Sichtbarkeit beim Kind, (b) Bereitschaft für Vokabel-Bündel, (c) Verdichtungs-Pflicht; (a) ist unabhängig
   baubar und der Teil, der kein Münz-Thema berührt. Der Vorbereitungslauf teilt nicht selbst; das passiert beim Schätzen.
2. **Wann entsteht die Zusatzpflicht?** Empfehlung: nicht von einem Job, sondern lazy beim ersten Lesen der Tagessicht im Fenster (wie die Abrechnung), idempotent
   je (Klassenarbeit, Übung); Alternative: der Vater löst sie beim Anlegen aus (B-423-Assistent). Berührt die Malus-Regeln: für den Nutzer.
3. **Pflicht-Ende.** Empfehlung: ein neues, nullbares Datum an der Position (`DutyUntil`), das `HasDutyOn` mitliest; `UntilDone` passt nicht (nur für Nacharbeit).
   Das ist ein Schema-Eingriff mit Migration an die Kette und ein Eintrag in den Schema-Toren.
4. **Eine Position je Übung oder eine gebündelte?** Empfehlung: eine je Übung (eine Position hat genau eine Übung, `PlanPosition.ExerciseId`); Tag-Bündel werden beim
   Auslösen **eingefroren**, damit ein später getaggtes Item keine neue Pflicht mitten im Fenster erzeugt. Entscheidung für das Grillen.
5. **Kein passender Plan.** Empfehlung: kein Plan des Fachs ⇒ keine Zusatzpflicht, der Vater bekommt einen Hinweis in `class-tests`; die App legt keinen Plan nebenbei an.
6. **Mehrere Arbeiten im Fenster.** Empfehlung: je Übung höchstens eine Zusatzposition, das früheste Ende gewinnt nicht – beide Termine verlängern sie bis zum späteren.
7. **Abgesagt oder geschrieben.** Empfehlung: `Cancelled`/`Written` beenden die Zusatzpflicht am selben Tag ohne Malus für den Tag danach.
8. **Wie viel pro Tag?** Empfehlung: die Dosis folgt der Bündelgröße geteilt durch die Tage im Fenster, nach oben begrenzt; Zahl als Konfiguration, nicht als Konstante (Gesamtbelastung
   neben der normalen Pflicht ist eine Produktfrage).
9. **Bereitschaft für Nicht-Vokabel.** Empfehlung: zunächst nur Vokabel-Bündel anzeigen, alle anderen als „kein Maß" – nicht schätzen.

## Akzeptanzkriterien (Entwurf)

1. Ein Kind sieht seine nächste geplante Klassenarbeit mit Titel, Datum und Tagen bis zum Termin; eine abgesagte oder geschriebene steht nicht dort. *Rot, wenn der
   Student-Endpunkt `Status` ignoriert.*
2. Ein anderes Kind und ein fremder Vater sehen sie nicht. *Rot, wenn die Route ohne Betreuungs-Prüfung hängt.*
3. Das Bündel des Kindes umfasst direkte Übungen **und** Tag-Übungen ohne Dubletten. *Rot, wenn die Auflösung nur `KlassenarbeitExercise` liest.*
4. Im Fenster vor dem Termin trägt jede Bündel-Übung eine tägliche Pflicht im aktiven Plan des Fachs, mit Standard-Malus; vor dem Fenster nicht. *Rot, wenn die Pflicht vom
   ersten Tag an gilt.*
5. Am Termintag und danach schuldet die Zusatzposition nichts mehr, auch nicht für spät abgerechnete Tage. *Rot, wenn das Pflicht-Ende fehlt oder `SettleClosedPeriodsAsync` es
   nicht liest.*
6. Das zweimalige Auslösen derselben Klassenarbeit legt keine zweite Position an. *Rot, wenn die Idempotenz fehlt.*
7. Eine Übung, die schon als Position im Plan liegt, bekommt keine zweite Position, sondern wird für das Fenster zur Pflicht gehoben oder bleibt, wie sie ist (je nach Entscheidung 4). *Rot,
   wenn eine Dublette entsteht.*
8. Der Vater verschiebt das Fenster an der Klassenarbeit, die schon angelegte Pflicht folgt. *Rot, wenn das Fenster nur beim Anlegen gelesen wird.*

## Verlauf

- **2026-10-23** — angelegt (Quelle: Knowunity-Vergleich, Etappe 1 des Übernahmeplans).
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): besteht; „wirkt auf nichts“ ist zu stark – Noten fließen in die Objective-Auswertung (`ObjectiveEvaluationService.cs:89-90`), es gibt den `repeat`-Endpunkt (`KlassenarbeitenController.cs:276`). Die Bündel-Auflösung teilt sie mit B-39.
- **2026-10-23** — **Zwei Entscheidungen des Nutzers** in der Grill-Runde G-7 der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): (1) Die Verdichtung vor dem Termin ist **Pflicht mit Malus** – im Fenster entsteht eine zusätzliche Pflicht über das Bündel der Klassenarbeit, und Verpassen kostet Münzen wie jede andere Pflicht (Kernidee: der Vater setzt den Termin, die App erzwingt die Vorbereitung). (2) Das **Fenster hat einen festen Default und ist an der Klassenarbeit durch den Vater änderbar**; der Default-Wert selbst wird beim Ausformulieren vorgeschlagen (Anhaltspunkt 7 Tage). Beim Ausformulieren gegen den Code zu klären: wie die Zusatzpflicht entsteht (eigene Position im aktiven Plan, Mission oder angehobenes Ziel bestehender Positionen), Höhe des Malus und wie das Bündel aufgelöst wird (geteilt mit B-39).
- **2026-10-23** — **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt am 2026-10-23 für alle an diesem Tag im Dialog entschiedenen Stories):** ein Nachtlauf darf diese Story auf Grundlage der Entscheidungen selbst ausformulieren, schätzen und bauen (Paket NP-22); trifft er auf eine Produktfrage, die die Entscheidungen nicht hergeben, hält er an. Erteilt in der Grill-Runde der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)).
- **2026-11-05** — Prioritäts-Beleg von außen: Baobab wirbt vor allem damit, genau das zu üben, was in der Arbeit drankommt. Vergleich in `docs/baobab-vergleich-plan.md` (Sammel-Story [B-374](B-374-baobab-vergleich-sammel-story.md)). Keine Änderung an den Entscheidungen.
- **2026-11-05** — **Rest-Entscheidung im Auftrag des Nutzers** (getroffen von Claude; Delegation im Dialog am 2026-11-05, [Protokoll](../pm-sitzung-2026-11-05-delegation.md)): die Zusatzpflicht im Fenster ist eine tägliche Übungsrunde über das Bündel; Bündel-Übungen außerhalb des Plans werden als Positionen in den aktiven Plan aufgenommen; Malus = Standard-Malus einer Tagespflicht.
- **2026-11-13** — Backlog-Abgleich gegen den Code: besteht unverändert. Hängt faktisch an [B-423](B-423-assistent-klassenarbeit-termin-und-stufenleiter.md): dort legt der Assistent Termin und Klassenarbeit an und verweist die Kind-Seite ausdrücklich hierher. Ein Student-Endpunkt zur Klassenarbeit fehlt weiter (kein Student-Controller/-DTO).
- **2026-11-14** — Abgleich Backlog-Lauf: Ist-Stand bestätigt – kein Student-Endpunkt und keine Sohn-Ansicht zur Klassenarbeit (in `frontend/src/sohn/` nur der Kommentar `SohnTest.tsx:38`), Entität unverändert (`KlassenarbeitEntities.cs:79-87`); Noten fließen in die Objective-Auswertung (`ObjectiveEvaluationService.cs:91`, jetzt unter `Services/Shared`), `repeat` unter `KlassenarbeitenController.cs:276`.
- **2026-11-15** — Prioritäts-Beleg von außen, der dritte nach Knowunity und Baobab: RocketTutor verkauft einen Klausur- und Wochenplaner, der Zeit und Aufgaben rückwärts vom Prüfungstermin verteilt. Vergleich in `docs/rockettutor-vergleich-plan.md` (Sammel-Story [B-581](B-581-rockettutor-vergleich-sammel-story.md)). Keine Änderung an den Entscheidungen.
- **2026-11-19** — ausformuliert (Vorbereitungslauf, Sonnet): Ist-Stand am Code neu gemessen. Korrekturen: die Bündel-Auflösung ist privat im Controller und hat **keinen** zweiten Nutzer (das „geteilt mit B-39" trägt nicht); ein Pflicht-Ende gibt es nur als `UntilDone` für Nacharbeit, „Pflicht bis zum Termin" braucht ein neues Feld (Migration). Neun offene Punkte mit Empfehlung, keine Entscheidung getroffen; Aufteilung in Sichtbarkeit/Bereitschaft/Pflicht empfohlen. Überschneidungen: B-680/B-644 (Tagesplan rückwärts von der Klausur), B-423 (abgenommen), B-249.
