---
tags: [typ/story, status/verworfen, bereich/klassenarbeit, bereich/training, rolle/student, rolle/supervisor]
aliases: [Klausur-Countdown, Klassenarbeit treibt den Plan]
status: verworfen
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/knowunity-vergleich-plan.md#wo-knowunity-weiter-ist
grund: "geteilt beim Schätzen am 2026-11-20 (Entscheidung 1) – als Ganzes XL: Sichtbarkeit, Bereitschaft und Verdichtungs-Pflicht, die Verdichtung selbst noch einmal in Kern, Folgen und Oberfläche. Entscheidungen und Akzeptanzkriterien leben hier und werden von den Teil-Stories nach Nummer zitiert"
ersetzt_durch: [B-750, B-751, B-752, B-757, B-758]
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

1. ~~**Aufteilen in drei Stories?** Empfehlung: ja – (a) Sichtbarkeit beim Kind, (b) Bereitschaft für Vokabel-Bündel, (c) Verdichtungs-Pflicht; (a) ist unabhängig
   baubar und der Teil, der kein Münz-Thema berührt. Der Vorbereitungslauf teilt nicht selbst; das passiert beim Schätzen.~~ → Entscheidung 1.
2. ~~**Wann entsteht die Zusatzpflicht?** Empfehlung: nicht von einem Job, sondern lazy beim ersten Lesen der Tagessicht im Fenster (wie die Abrechnung), idempotent
   je (Klassenarbeit, Übung); Alternative: der Vater löst sie beim Anlegen aus (B-423-Assistent). Berührt die Malus-Regeln: für den Nutzer.~~ → Entscheidung 2.
3. ~~**Pflicht-Ende.** Empfehlung: ein neues, nullbares Datum an der Position (`DutyUntil`), das `HasDutyOn` mitliest; `UntilDone` passt nicht (nur für Nacharbeit).
   Das ist ein Schema-Eingriff mit Migration an die Kette und ein Eintrag in den Schema-Toren.~~ → Entscheidung 3.
4. ~~**Eine Position je Übung oder eine gebündelte?** Empfehlung: eine je Übung (eine Position hat genau eine Übung, `PlanPosition.ExerciseId`); Tag-Bündel werden beim
   Auslösen **eingefroren**, damit ein später getaggtes Item keine neue Pflicht mitten im Fenster erzeugt. Entscheidung für das Grillen.~~ → Entscheidung 4.
5. ~~**Kein passender Plan.** Empfehlung: kein Plan des Fachs ⇒ keine Zusatzpflicht, der Vater bekommt einen Hinweis in `class-tests`; die App legt keinen Plan nebenbei an.~~ → Entscheidung 5.
6. ~~**Mehrere Arbeiten im Fenster.** Empfehlung: je Übung höchstens eine Zusatzposition, das früheste Ende gewinnt nicht – beide Termine verlängern sie bis zum späteren.~~ → Entscheidung 6.
7. ~~**Abgesagt oder geschrieben.** Empfehlung: `Cancelled`/`Written` beenden die Zusatzpflicht am selben Tag ohne Malus für den Tag danach.~~ → Entscheidung 7.
8. ~~**Wie viel pro Tag?** Empfehlung: die Dosis folgt der Bündelgröße geteilt durch die Tage im Fenster, nach oben begrenzt; Zahl als Konfiguration, nicht als Konstante (Gesamtbelastung
   neben der normalen Pflicht ist eine Produktfrage).~~ → Entscheidung 8.
9. ~~**Bereitschaft für Nicht-Vokabel.** Empfehlung: zunächst nur Vokabel-Bündel anzeigen, alle anderen als „kein Maß" – nicht schätzen.~~ → Entscheidung 9.

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-20 (Sonnet) auf Anweisung des Nutzers (Empfehlung übernommen), unbestätigt.*

Schon entschieden und hier nicht angefasst: Pflicht mit Malus, festes Fenster mit Default und Änderung durch den Vater, tägliche
Übungsrunde über das Bündel, Bündel-Übungen als Positionen im aktiven Plan (Nutzer 2026-10-23, Delegation 2026-11-05, Verlauf).
Die Entscheidungen tragen die Teile **(a)** Sichtbarkeit, **(b)** Bereitschaft und **(c)** Verdichtungs-Pflicht.

**Querfrage mit [B-659](B-659-pflicht-wird-kuer-bricht-die-serie.md) (dort Entscheidung 2, gleiche Antwort):** Das Pflicht-Ende ist
dieselbe nullbare Spalte `DutyUntil` an der Position mit demselben Helfer `DutyCadenceOn` und derselben Schließ-Regel; wer zuerst
baut, legt sie an (B-659 braucht zusätzlich `CadenceUntil`, diese Story nicht). **Querfrage mit B-218:** Die Zusatz-Pflicht ist eine
echte Positions-Pflicht, keine Zusatzrunde. **Überschneidung mit B-680/B-644 (Tagesplan rückwärts von der Klausur,
nicht entschieden):** Teil (c) steht für sich und setzt B-680 nicht voraus; es kann später von B-680 ersetzt werden
(Entscheidung 11). Abgenommene Entscheidungen werden nicht berührt (B-423 legt nur Termin und Klassenarbeit an).

1. **Die Story wird beim Schätzen in drei Stories geteilt: (a) Sichtbarkeit beim Kind, (b) Bereitschaft für Vokabel-Bündel,
   (c) Verdichtungs-Pflicht.** Diese Datei trägt die Entscheidungen und Akzeptanzkriterien, die Teil-Stories zitieren sie nach
   Nummer (Muster B-623 → B-628/B-629/B-630). Die alte Id geht dabei auf `verworfen: geteilt`, die neuen Ids werden **beim
   Schätzen neu gezählt** (höchste vorhandene heute B-712, eine andere Sitzung kann parallel vergeben). Reihenfolge: (a) und
   (b) sind unabhängig voneinander baubar und berühren keine Münzen, (c) kommt zuletzt. *Kosten:* drei Stories statt einer,
   dafür kein XL. *Verworfen:* alles in einer Story (Backend L, Frontend M, eine Migration, ein Eingriff im Kern der
   Pflicht-Regel). *Schließt aus:* ein Kind, das die Countdown-Anzeige erst mit der Pflicht bekommt.
2. **Teil (c), Auslöser: Abgleich beim Kind-Login, im selben Schritt wie die Abrechnung, nie auf einem GET und nie durch
   einen Job.** Eine Abgleich-Funktion (idempotent) erzeugt oder passt die Zusatzpositionen an; sie läuft beim Kind-Login
   (`AuthController.cs:85`, wie `SettleClosedPeriodsAsync`) und synchron, wenn der Vater die Klassenarbeit ändert (Fenster,
   Termin, Status). Gelesen wird die Klassenarbeit, nicht der Ereigniszeitpunkt. Idempotenz je (Klassenarbeit, Übung) über
   `PlanPosition.SourceKlassenarbeitId` (nullbar, FK `SetNull`) und `Klassenarbeit.PrepStartedOn` (nullbar, Tag des ersten
   Auslösens). *Begründung:* Es gibt keinen Zeitgeber (Kommentar `AuthController.cs:83-84`), und eine Position, die nach
   ihrem Anlegetag zählt (`DutyBegins`, B-613 Entscheidung 1), schafft keinen rückwirkenden Malus. *Kosten:* Hat das Kind sich
   mehrere Tage nicht eingeloggt, beginnt die Pflicht erst am Anlegetag; das Fenster ist dann kürzer. Zwei neue nullbare
   Spalten hier (mit denen aus 3 und 10 **eine** Migration an der Kette), ein zweiter Aufruf-Ort im
   `KlassenarbeitenController`. *Verworfen:* der Vater löst beim Anlegen aus (B-423-Assistent): das Fenster friert ein, und
   AK 8 (das Fenster folgt dem Vater) wäre nicht zu halten; und das Lesen der Tagessicht (ein GET würde schreiben).
   *Schließt aus:* eine Pflicht ab dem ersten Fenstertag unabhängig vom Login (AK 4 ist so formuliert).
3. **Pflicht-Ende: die gemeinsame Spalte `DutyUntil` (letzter Pflichttag), `UntilDone` bleibt Nacharbeit.** Eine Zusatzposition
   trägt `Cadence = Daily` und `DutyUntil` = Tag vor dem Termin; am Termintag und danach schuldet sie nichts, auch nicht für
   spät abgerechnete Tage (die Abrechnung überspringt Perioden nach `DutyUntil`). *Begründung:* `UntilDone` mit `CompletedOn`
   passt nur für Nacharbeit (`PlanPositionEntities.cs:140-146`). *Kosten:* Migration (nullbar, ohne Backfill), ein Eintrag in
   den Schema-Toren, der Helfer an den Lesestellen (`PositionProgressService.cs:55-56`, `:197-198`, `:264-266`, `:325`) und
   eine Obergrenze in `SettleClosedPeriodsAsync` (`:761`). *Verworfen:* eine Mission oder ein angehobenes Ziel bestehender
   Positionen (der Vater hat „Pflicht mit Malus über das Bündel“ entschieden).
4. **Eine Position je Übung, das Bündel wird beim ersten Auslösen eingefroren.** Eine Position hat genau eine Übung
   (`PlanPosition.ExerciseId`); beim ersten Abgleich (`PrepStartedOn` gesetzt) werden direkte und Tag-Übungen ohne Dubletten
   aufgelöst und je eine Zusatzposition angelegt. Eine Übung, die danach in das Bündel kommt oder einen Tag bekommt, erzeugt
   **keine** neue Pflicht mitten im Fenster; der Vater hängt sie bei Bedarf von Hand an. Liegt die Übung schon als Position im
   Plan, entsteht keine zweite: sie bleibt, wie sie ist (AK 7). *Kosten:* ein nachgetaggtes Item erscheint nicht von selbst.
   *Verworfen:* das Bündel bei jedem Login neu lesen (die Pflicht wüchse unter den Füßen des Kindes). *Schließt aus:* ein
   „lebendes“ Bündel.
5. **Kein passender Plan heißt keine Zusatzpflicht, und der Vater erfährt es.** Hat das Kind keinen aktiven Plan im Fach der
   Klassenarbeit (oder ist `SubjectId` leer), entsteht nichts, und die Klassenarbeiten-Liste meldet das additiv („keine
   Vorbereitungs-Pflicht, weil …“). Die App legt keinen Plan nebenbei an (`StudyPlansController.cs:166`). *Kosten:* ein
   Feld mehr in der Antwort. *Verworfen:* einen Plan anlegen. *Schließt aus:* dass die Kernidee („die App erzwingt die
   Vorbereitung“) **still** ausfällt.
6. **Mehrere Arbeiten im Fenster: je Übung höchstens eine Zusatzposition, sie läuft bis zum spätesten Termin.** Die Position
   behält ihre erste `SourceKlassenarbeitId`; `DutyUntil` ist der späteste Tag vor einem Termin einer geplanten Arbeit des
   Kindes, deren Bündel die Übung enthält (der Abgleich rechnet es neu). *Kosten:* der Abgleich liest alle geplanten
   Klassenarbeiten des Kindes. *Verworfen:* das früheste Ende gewinnt (die zweite Arbeit stünde ohne Vorbereitung).
7. **Abgesagt oder geschrieben schließt die Zusatzpflicht nach der Schließ-Regel von B-659.** Der Abgleich nach der
   Statusänderung schließt die laufende Periode: erfüllt → heute zählt mit, offen → heute zählt nicht mit; ab dem Tag danach
   gibt es weder Pflicht noch Malus. *Kosten:* der Abgleich teilt den Helfer mit B-659. *Schließt aus:* Malus für einen Tag,
   an dem die Arbeit schon abgesagt war.
8. **Dosis: Karten der Übung geteilt durch die Tage des Fensters, nach oben begrenzt, mit Vorschau für den Vater.**
   Die Zahl je Position = aufrunden(Karten / Fenstertage), begrenzt durch einen Konfigurationswert (Startwert beim Schätzen
   an echten Bündeln messen); welches Feld (`ItemCount` oder die Neu-Dosis) sie trägt, entscheidet das Schätzen. Der Vater
   sieht im Dialog der Klassenarbeit „etwa N Karten je Tag“ als reine Zahl. **Keine harte Tagessumme.** *Begründung:* Eine
   harte Tagesgrenze würde Übungen still zu Kür machen, die Pflicht bliebe unerfüllt. *Kosten:* Ein großes Bündel erzeugt
   viel Last; der Vater steuert sie über Fenster und Bündel. *Verworfen:* Tagessumme hart gedeckelt.
9. **Teil (b), Bereitschaft nur für Vokabel-Bündel, aus dem vorhandenen Beherrschungsmaß.** Anzeige: Anteil der Vokabel-Items
   des Bündels, die nicht schwach sind (Schwelle `WeakBelowPercent` = 50, `ChildVocabularyProgressController.cs:47`);
   „wackelig“ und „sicher“ benennen diesen Anteil mit der vorhandenen Schwelle, es gibt keine neue. Für andere Typen steht
   „kein Maß“ (Feld leer, nicht geschätzt). *Kosten:* Grammatik-Bündel haben keine Anzeige.
   *Verworfen:* ein Maß für Nicht-Vokabel (es gibt keines, `ItemProgress` ist vokabelgebunden, `ItemProgressService.cs:41`).
10. **Fenster: Standard 7 Tage, je Klassenarbeit änderbar, 0 schaltet die Zusatzpflicht für diese Arbeit ab.** Neues nullbares
    Feld `Klassenarbeit.PrepWindowDays` (leer = Standard aus der Konfiguration, Startwert 7). *Kosten:* ein Feld, Migration (die
    eine aus 2). *Verworfen:* ein fester Wert (Entscheidung des Nutzers: änderbar).
11. **Malus und Belohnung der Zusatzposition: ein Standard-Malus je Klassenarbeit und Tag, auf die Positionen verteilt; die
    Zusatzposition zahlt keine eigene Ziel-Belohnung.** Der Malus einer verpassten Tagesrunde ist der Standard-Malus **einer**
    Tagespflicht (Entscheidung vom 2026-11-05), nicht je Übung. Er wird beim Anlegen ganzzahlig auf die Zusatzpositionen
    verteilt (`PenaltyCoins` je Position, `PlanPositionEntities.cs:91`, der Rest auf die erste); der Wert kommt aus der
    Konfiguration (Startwert beim Schätzen aus den echten Beträgen). `PointsGoalMet` ist 0: Das Kind bekommt die normalen
    Kartenpunkte, keine Ziel-Münzen. *Begründung:* Je Übung gerechnet vervielfachte sich der Malus mit der Größe des Bündels (acht
    Übungen, acht Mal der Standard-Malus an einem Tag). *Kosten:* Rundung; der Vater kann `PenaltyCoins` danach je Position von
    Hand ändern, der Abgleich überschreibt es nicht. **Hier würde der Nutzer am ehesten anders entscheiden** (Malus-Höhe,
    keine Belohnung). *Verworfen:* der volle Malus je Übung und eine Ziel-Belohnung (Gesamtlast und Münzfluss ungeprüft).
    *Schließt aus:* eine Vorbereitungs-Pflicht, die Münzen **bringt**.
12. **Teil (c) steht für sich, B-680 darf es später ersetzen.** B-680/B-644 (Tagesplan je Fach rückwärts von der Klausur) sind vom
    Nutzer noch nicht entschieden; die Zusatzpositionen sind gewöhnliche Positionen im aktiven Plan, bleiben also mit jeder
    Tagesplan-Lösung verträglich. Reihenfolge: (a) und (b) zuerst; beim Schätzen von (c) wird der Stand von B-680 neu
    gelesen. *Kosten:* (c) kann verworfen werden, wenn B-680 es übernimmt; (a) und (b) bleiben davon unberührt.

## Akzeptanzkriterien

Je Kriterium die Änderung, die es rot macht. Teil (a) Sichtbarkeit: 1 bis 2; Teil (b) Bereitschaft: 3 und 4; Teil (c)
Verdichtungs-Pflicht: 5 bis 14.

1. Ein Kind sieht seine nächste geplante Klassenarbeit mit Titel, Datum und Tagen bis zum Termin; eine abgesagte oder
   geschriebene steht nicht dort. *Rot, wenn der Student-Endpunkt `Status` ignoriert.*
2. Ein anderes Kind und ein fremder Vater sehen sie nicht. *Rot, wenn die Route ohne Betreuungs-Prüfung hängt.*
3. Das Bündel des Kindes umfasst direkte Übungen **und** Tag-Übungen ohne Dubletten. *Rot, wenn die Auflösung nur
   `KlassenarbeitExercise` liest (die Auflösung ist heute privat im Controller, `KlassenarbeitenController.cs:316-327`; ein
   zweiter Leser braucht sie als geteilte Funktion).*
4. Die Bereitschaft nennt für ein Vokabel-Bündel den Anteil der nicht schwachen Items mit der Schwelle 50 und für ein
   Bündel ohne Vokabeln „kein Maß“, nie eine Schätzung. *Rot, wenn ein Nicht-Vokabel-Bündel eine Zahl liefert oder die
   Schwelle von `WeakBelowPercent` abweicht.*
5. Beim ersten Login im Fenster trägt jede Bündel-Übung eine tägliche Pflicht im aktiven Plan des Fachs, ab dem Anlegetag,
   mit dem verteilten Standard-Malus; vor dem Fenster nicht. *Rot, wenn die Pflicht vor dem Fenster entsteht oder rückwirkend
   Malus bucht (`DutyBegins` nicht gelesen).*
6. Am Termintag und danach schuldet die Zusatzposition nichts mehr, auch nicht für spät abgerechnete Tage. *Rot, wenn
   `DutyUntil` fehlt oder `SettleClosedPeriodsAsync` es nicht liest (Test: Login nach dem Termin, kein Malus für den Termintag).*
7. Das zweimalige Abgleichen derselben Klassenarbeit legt keine zweite Position an, und ein später nachgetaggtes Item legt
   keine neue Pflicht an. *Rot, wenn die Idempotenz (`SourceKlassenarbeitId`, `PrepStartedOn`) fehlt oder das Bündel bei jedem
   Login neu gelesen wird.*
8. Eine Übung, die schon als Position im Plan liegt, bekommt keine zweite Position und bleibt, wie sie ist. *Rot, wenn eine
   Dublette entsteht.*
9. Der Vater verschiebt Termin oder Fenster an der Klassenarbeit, die schon angelegte Pflicht folgt (`DutySince`, `DutyUntil`)
   beim nächsten Abgleich, und bei „Fenster 0“ entsteht keine Zusatzpflicht. *Rot, wenn das Fenster nur beim ersten Anlegen
   gelesen wird.*
10. Der Malus **eines Tages** einer Klassenarbeit ist der Standard-Malus, auf die Zusatzpositionen verteilt, nicht je Übung
    vervielfacht; die Zusatzposition zahlt keine Ziel-Belohnung. *Rot, wenn acht Übungen am verpassten Tag acht Mal den
    Standard-Malus buchen oder `PointsGoalMet` > 0 gesetzt wird.*
11. Abgesagt oder geschrieben: Der Abgleich schließt die laufende Periode nach der Regel von B-659 (erfüllt → heute zählt,
    offen → heute zählt nicht) und danach gibt es weder Pflicht noch Malus. *Rot, wenn der Status nicht gelesen wird oder
    immer „heute“ bzw. immer „gestern“ geschlossen wird.*
12. Kein aktiver Plan im Fach oder keine `SubjectId`: keine Zusatzpflicht, die Klassenarbeiten-Liste nennt den Grund. *Rot,
    wenn die App einen Plan anlegt oder der Grund fehlt.*
13. Zwei Arbeiten im Fenster, dieselbe Übung: eine Position bis zum späteren Termin. *Rot, wenn die zweite Arbeit eine
    zweite Position anlegt oder die erste das Ende festlegt.*
14. Bestand: nach der Migration sind alle neuen Spalten `NULL`, an einer Kopie der echten DB ändern sich Plan, Serie und
    Malus-Stand nicht (Vorbild `ActivatedAtMigrationTests`). *Rot, wenn die Migration etwas vorbelegt.*

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
- **2026-11-20** — gegrillt (Vorbereitungslauf, Sonnet, auf Anweisung des Nutzers „Entscheide die offenen Fragen nach deiner Empfehlung“; unbestätigt): **Teilung in drei Stories (Sichtbarkeit, Bereitschaft, Verdichtungs-Pflicht) beschlossen, vollzogen erst beim Schätzen** (Ids dann neu zählen); Auslöser = Abgleich beim Kind-Login (kein GET, kein Job); Pflicht-Ende über die mit B-659 gemeinsame Spalte `DutyUntil`; ein Standard-Malus je Klassenarbeit und Tag, auf die Positionen verteilt, keine eigene Ziel-Belohnung; (c) steht für sich neben B-680. Akzeptanzkriterien final.
- **2026-11-20** — beim Schätzen geteilt (Vorbereitungslauf, Sonnet, nach Entscheidung 1): (a) Sichtbarkeit beim Kind → [B-750](B-750-klassenarbeit-countdown-beim-kind.md) (M, beides), (b) Bereitschaft für Vokabel-Bündel → [B-751](B-751-klassenarbeit-bereitschaft-vokabeln.md) (M, beides), (c) Verdichtungs-Pflicht war als Ganzes XL und ist in [B-752](B-752-vorbereitungs-pflicht-im-fenster.md) (Kern, L, backend), [B-758](B-758-vorbereitungs-pflicht-folgt-der-arbeit.md) (Verschieben, Absagen, Schreiben, M, backend) und [B-757](B-757-vorbereitungs-pflicht-in-der-vater-oberflaeche.md) (Oberfläche, S, frontend) geschnitten. Diese Datei bleibt die Quelle der Entscheidungen 1 bis 12 und der Akzeptanzkriterien 1 bis 14. Beim Schätzen aufgefallen: Entscheidung 8 hatte kein eigenes Kriterium (jetzt AK 10 in B-752), und der Tagesplaner [B-644](B-644-tagesplaner-statt-fester-lauf.md) übernimmt die Terminpflicht und löst die Positions-Pflicht auf – ob (c) noch gebraucht wird, ist vor dem Bau zu klären.
