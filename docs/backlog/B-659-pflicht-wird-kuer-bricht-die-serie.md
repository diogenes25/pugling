---
tags: [typ/story, status/in-arbeit, bereich/gamification, rolle/student, rolle/supervisor]
aliases: [Pflicht zur Kür, gelöschte Pflicht bricht die Serie]
status: in-arbeit
prio: P3
art: Defekt
groesse: L
wo: backend
migration: ja
vertragsbruch: nein
quelle: "PM-Sitzung 2026-11-18, Runde 3 (Sprint 87), Review von B-613 (pugling-reviewer, 🟡2)"
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-659 · Wird eine Pflicht zur Kür (oder gelöscht), bricht die Serie rückwirkend

Stellt der Vater die einzige Pflicht eines Plans auf „Kür“ oder löscht er den Plan, gelten die Tage, an denen das Kind
sie erfüllt hat, nachträglich als unvollständig – die Serie fällt, und mit ihr die Stufe der Tageskiste.

## User Story

Als Schüler möchte ich, dass meine erfüllten Tage Tage bleiben, wenn Papa später eine Pflicht in Kür umwandelt oder
einen Plan wegräumt, damit meine Serie nur reißt, wenn ich selbst einen Tag verpasst habe.

## Ist-Stand am Code

Gelesen am 2026-11-19, nicht per Test nachgestellt (kein Server, keine Probe).

1. **Die Pflicht-Frage liest den heutigen Takt.** `PositionProgressService.HasDutyOn`
   (`backend/Pugling.Api/Services/Shared/PositionProgressService.cs:54-56`) liefert für `pos.Cadence == None`
   `false` – an **jedem** Tag. `IsObligationOn` (Zeile 75) baut darauf auf. Eine Spanne „war Pflicht von … bis …“
   gibt es nicht: `PlanPosition` kennt nur `DutySince` (`Models/PlanPositionEntities.cs:152`, der Anfang) und
   `CompletedOn` (Zeile 145, nur für `UntilDone`). Ein „Pflicht bis“ oder eine Taktung-Momentaufnahme an der
   Position: **nicht gefunden**.
2. **Ein Tag ohne jede Pflicht ist ein unvollständiger Tag.** `DutyStateAsync` (Zeilen 461-471) meldet bei null
   Pflichten `HasDuty = false`; `IsCompleteDay` (Zeilen 439-443) verlangt `withDuty.Any(p => p.Done)`. Das ist
   Absicht und getestet: `RemediationPositionTests.Beendete_Nacharbeit_traegt_die_Serie_nicht_weiter`
   (`RemediationPositionTests.cs:685-703`) erwartet nach dem Ende der einzigen Pflicht Serie 0.
3. **Die Serie liest rückwärts über alle Pläne.** `ChildStreakAsync` (Zeilen 479-506) und `CompleteChildDaysAsync`
   (Zeilen 512-529) laufen je Tag über `DutyStateAsync`. Dort steckt der Schaden: das heutige `Cadence = None` löscht
   die Pflicht aller vergangenen Tage. Hat der Plan weitere Pflichten oder tragen andere Pläne den Tag, fällt nichts
   (`IsCompleteDay` braucht nur **eine** erfüllte Pflicht).
4. **Wer die Serie liest:** die Tageskiste skaliert über `StreakBoundedAsync` (`DailyBoxService.cs:59-64`,
   `StreakTiers`-Multiplikator auf Münzen und Gems), dazu Missionen und Auszeichnungen
   (`MetricsService.cs:44-47`), die Tagessicht (`ChildTodayController.cs:34`) und die Planübersicht
   (`PlanOverviewController.cs:41`). Ein gerissener Rückblick senkt also nachträglich die Kistenstufe.
5. **Pflicht → Kür schreibt nichts mit.** Im PATCH (`PlanPositionsController.cs:357-361`) setzt nur der Wechsel
   Kür → Pflicht (und Ende einer `UntilDone`-Nacharbeit → anderer Takt) `DutySince`; `pos.Cadence = None` überschreibt
   den Takt kommentarlos (Zeile 361).
6. **Die Abrechnung des Malus** filtert bereits auf `p.Cadence != GoalCadence.None`
   (`PositionProgressService.cs:761`): eine auf Kür gestellte Position wird für noch nicht abgerechnete Perioden
   (Fenster `MaxSettleLookbackDays = 14`, Zeile 25) nicht mehr belastet. Das ist heutiges Verhalten, nicht Teil
   dieser Story.
7. **Positionen löschen** ist nur ohne Spieldaten möglich: `PlanPositionsController.cs:409-411` liefert
   `409 position_has_data`, sobald es `TestAttempts` oder `PracticeSessions` gibt. Eine Position, die nie gespielt
   wurde, hat nie eine Pflicht erfüllt.
8. **Pläne löschen** hat keine solche Sperre: `StudyPlansController.cs:242-254` entfernt den Plan samt Positionen,
   Fortschritt, Sitzungen, Versuchen und Ziel-Belohnungen per Kaskade (Doku-Kommentar Zeilen 237-241). Der Plan fällt
   danach aus der Planliste von `ChildStreakAsync` (Zeile 481); ein Tag, den nur er trug, hat keine Pflicht mehr.
   Gebuchte Münzen im Konto bleiben stehen (die `ChildPointsEntries` hängen am Kind, nicht am Plan).
9. **Test für den Fall:** `RueckwirkendePflichtTests.cs` kennt weder `"None"` noch `Delete` (Suche 2026-11-19: keine
   Treffer). Ein Test „Pflicht → Kür" oder „Plan löschen" gegen die Serie: **nicht gefunden**.

## Die echte Lücke

Schmaler und breiter als die Beobachtung:

- **Schmaler:** Die Position zu löschen ist gegenstandslos. Gespielte Positionen lassen sich nicht löschen (Beleg 7),
  ungespielte haben nie eine Pflicht erfüllt. Die zweite offene Frage der Idee („Was gilt beim Löschen einer Position
  oder eines ganzen Plans?") reduziert sich auf den **Plan**.
- **Bleibt:** Pflicht → Kür (einzige oder letzte Pflicht der Tage) und Plan löschen. Beides macht erfüllte Tage zu
  Tagen ohne Pflicht.
- **Breiter:** Pflicht → Kür → Pflicht ist derselbe Mechanismus. Der zweite Wechsel setzt `DutySince` auf heute
  (`PlanPositionsController.cs:357-360`), `IsObligationOn` verneint damit alle früheren Tage – auch die, an denen die
  Pflicht erfüllt war. B-613 Entscheidung 6 nennt das „zu Gunsten des Kindes"; für die **Abrechnung** stimmt das, für
  die **Serie** nicht (ein Tag ohne Pflicht ist unvollständig, Beleg 2). Am Code gelesen, nicht nachgestellt.

## Offene Punkte

1. ~~**Historie oder Überspringen?** Soll eine früher erfüllte Pflicht für vergangene Tage weiter zählen, oder genügt es,
   dass ein Tag ohne Pflicht die Serie überspringt? *Empfehlung: Historie.* Überspringen würde den Test aus Beleg 2
   kippen (nach dem Ende der Nacharbeit gilt ein Tag ohne Pflicht heute bewusst als unvollständig) und der Serie jede
   Lücke durchgehen lassen, sobald der Vater alles auf Kür stellt.~~ → Entscheidung 1.
2. ~~**Wie sieht die Historie aus?** *Empfehlung:* zwei nullable Spalten an `PlanPosition` – „Pflicht bis“ und die
   Taktung zu diesem Zeitpunkt –, gesetzt beim Wechsel Pflicht → Kür; `HasDutyOn` wertet sie für Tage ≤ „bis“ mit der
   alten Taktung aus (die Wochen-Pflicht muss weiter als Woche gelten, `IsGoalMetAsync` rechnet bei `None` sonst tageweise,
   `PositionProgressService.cs:198`). Das ist **eine** Migration auf der echten DB, ohne Backfill (Bestandszeilen:
   `NULL` = „nie Kür gewesen“), gegen eine Kopie zu proben. Verworfen als Alternative: ein eingefrorenes Tages-Urteil
   (neue Tabelle je Kind und Tag) – würde auch B-613/B-673 mittragen, ist aber ein Umbau der Serien-Logik.~~ → Entscheidung 2.
3. ~~**Pflicht → Kür → Pflicht mit abdecken?** Eine Spanne je Position kann nur **eine** frühere Pflicht-Phase halten.
   *Empfehlung: nein, hier nicht* – bekannter Rest, als Grenze in die Entscheidung schreiben und notfalls eine Story
   anlegen. Der zweite Wechsel überschreibt die Spanne.~~ → Entscheidung 3.
4. ~~**Plan löschen:** *Empfehlung:* sperren, sobald der Plan Spieldaten trägt (`409` mit neuem, additivem Code,
   Muster `position_has_data`), mit dem Hinweis „stattdessen ausschalten“. Ein ausgeschalteter Plan behält seine
   erfüllten Tage (`IsCompleteDay` zählt `Done` ohne `Active`, Zeile 442). Verworfen: Löschen erlauben und die Tage
   einfrieren – bräuchte die Tages-Tabelle aus Punkt 2.~~ → Entscheidung 4.
5. ~~**MALUS-/MÜNZ-FRAGE (für den Nutzer):** Soll eine früher erfüllte Pflicht, die heute Kür ist, auch für die
   **Abrechnung** noch offener Perioden als Pflicht gelten? *Empfehlung: nein – Malus unverändert.* Wer eine Pflicht
   auf Kür stellt, erlässt sie; das gilt heute (Beleg 6). Die Historie soll nur Serie, Missionen, Zeitachse und
   Kistenstufe betreffen. Die Wirkung auf die Kistenstufe (Münzen) ist die gewollte Rückgängig-Machung eines
   Defekts, keine neue Regel; die Frage bleibt trotzdem beim Nutzer, weil sie die Münz-Ökonomie berührt.~~ → Entscheidung 5.

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-20 (Sonnet) auf Anweisung des Nutzers (Empfehlung übernommen), unbestätigt.*

**Querfrage mit [B-248](B-248-klassenarbeit-countdown-treibt-den-plan.md):** Beide Stories brauchen ein „Pflicht bis“ an
der Position. Es gibt **eine** Spalte, **einen** Helfer und **eine** Schließ-Regel (Entscheidung 2); wer zuerst baut, legt
sie an. **Überschneidung mit abgenommenen Entscheidungen, nicht geändert:** B-613 Entscheidung 5 und Risiko 6 (Kür → Pflicht
setzt „Pflicht seit“ auf heute, der zweite Wechsel verwirft die frühere Phase) bleiben; B-606 (ein Tag ohne jede Pflicht ist
unvollständig) bleibt.

1. **Historie statt Überspringen, und die Story gilt für die erfüllten Tage.** Ein erfüllter Tag bleibt vollständig, auch
   wenn die Pflicht später Kür wird; ein Tag **nach** dem Wechsel ohne jede Pflicht bleibt unvollständig.
   *Begründung:* Das Überspringen würde `RemediationPositionTests.Beendete_Nacharbeit_traegt_die_Serie_nicht_weiter`
   (`RemediationPositionTests.cs:685-703`) kippen und der Serie jede Lücke durchgehen lassen, sobald der Vater alles auf
   Kür stellt (Gegenfundstelle: `IsCompleteDay` verlangt `withDuty.Any(p => p.Done)`,
   `PositionProgressService.cs:439-443`). *Kosten:* zwei Spalten und ein Helfer (Entscheidung 2). *Verworfen:* Überspringen.
   *Gegen die User Story gerechnet:* Sie sagt „meine Serie reißt nur, wenn ich selbst einen Tag verpasst habe“. Das gilt
   rückwärts, für die erfüllten Tage. **Ausgeschlossen bleibt:** ein Kind, dem der Vater **alle** Pflichten nimmt, hat ab dem
   Wechsel keine vollständigen Tage mehr (B-606/B-636, nicht Gegenstand dieser Story). Wer das ändern will, ändert B-606.
2. **Die Historie ist ein Paar nullbarer Spalten an der Position, gelesen über einen Helfer.** `DutyUntil` (`DateOnly?`,
   letzter Pflichttag einschließlich) und `CadenceUntil` (`GoalCadence?`, der Takt bis dahin). Ein Helfer
   `DutyCadenceOn(pos, day)` liefert: `DutyUntil` leer → `Cadence`; `day <= DutyUntil` → `CadenceUntil ?? Cadence`; sonst
   `None`. `HasDutyOn`, `IsGoalMetAsync` und die Tagesansicht lesen ihn statt `pos.Cadence`
   (`PositionProgressService.cs:55-56`, `:197-198`, `:264-266`, `:325`). **Schließ-Regel** (gilt auch für B-248): Der Wechsel
   Pflicht → Kür schließt die **laufende Periode** (`PeriodRange(Takt, heute)`), wenn sie erfüllt ist
   (`DutyUntil` = letzter Tag der Periode, also bei einer erfüllten Woche der Sonntag), sonst `DutyUntil` = der Tag vor dem
   Periodenbeginn. Sie läuft **nur beim Übergang** Daily/Weekly → `None`, nicht bei jedem Speichern: das Positions-Formular
   schickt `cadence` bei jedem PATCH mit (B-613 Risiko 4). *Begründung:* Ein Feldpaar heilt Serie, Missionstage, Zeitachse
   und Kistenstufe an einer Stelle; die Wochen-Pflicht gilt weiter als Woche (`IsGoalMetAsync` rechnete bei `None` sonst
   tageweise, `:198`). *Kosten:* **eine** Migration auf der echten DB (nullbar, ohne Backfill, `NULL` = „nie Kür gewesen“; Muster
   `20261118181817_DutySinceActivatedAt`, Probe gegen eine Kopie), ein Eintrag in den Schema-Toren, der Helfer an fünf
   Stellen, und der PATCH muss „erfüllt?“ beim Wechsel kennen. *Verworfen:* ein eingefrorenes Tages-Urteil je Kind und Tag
   (neue Tabelle; Umbau der Serien-Logik, trägt B-613/B-673 nur mit). *Schließt aus:* mehr als **eine** frühere
   Pflicht-Phase je Position (Entscheidung 3).
3. **Pflicht → Kür → Pflicht ist ein bekannter Rest, kein Teil dieser Story.** Der Wechsel Kür → Pflicht setzt `DutySince`
   auf heute (B-613) und **löscht** `DutyUntil` und `CadenceUntil`; ohne das Löschen verneinte `day > DutyUntil` die neue
   Pflicht. Die alte Phase fällt damit aus der Serie, wie es B-613 Risiko 6 schon bestätigt hat. *Kosten:* ein Vater, der
   auf Kür und zurück stellt, bricht die frühere Serie weiter. *Verworfen:* eine Liste von Phasen je Position (die Tabelle
   aus Entscheidung 2 in kleiner). *Schließt aus:* „ich habe nur kurz auf Kür gestellt“. **Vorschlag:** bei Bedarf eine eigene
   Story; hier nicht angelegt.
4. **Plan löschen wird gesperrt, sobald er Spieldaten trägt:** `409` mit neuem, additivem Code `plan_has_data` (Muster
   `ApiErrors.PositionHasData`, `ApiErrors.cs:205`; Spieldaten wie dort: ein `TestAttempt` oder eine `PracticeSession` an
   irgendeiner Position des Plans); die Meldung nennt „stattdessen ausschalten“. Ein ausgeschalteter Plan behält seine
   erfüllten Tage (`IsCompleteDay` zählt `Done` ohne `Active`, `:442`). *Begründung:* Das Löschen vernichtet dazu den
   gesamten Spielstand des Kindes; in der Testphase mit echten Kindern ist das die Daten-Linie des Repos (CLAUDE.md,
   „EF-Migrationen“). *Kosten:* **Gegenfundstelle:** `PlanPositionCrudTests.Plan_Loeschen_EntferntPlanMitGespielterPosition`
   (`PlanPositionCrudTests.cs:199`) behauptet heute das Gegenteil und wird umgeschrieben; `VaterPlanDetail.tsx:34` zeigt den
   Fehler als Meldung; `scripts/_plan-api.ps1:89` rät „ggf. löschen“ und muss „ausschalten“ sagen (Skript ist nicht Teil
   dieses Laufs). Abgelöste Pläne bleiben in der Liste. *Verworfen:* Löschen erlauben und die Tage einfrieren (bräuchte die
   verworfene Tages-Tabelle). *Schließt aus:* einen Plan mit Spielstand wegräumen; ein Fehlplan, den das Kind schon
   gespielt hat, bleibt ausgeschaltet stehen.
5. **Der Malus bleibt unverändert; die Historie liest nur Serie, Missionen, Zeitachse und Kistenstufe.** Die Abrechnung
   filtert weiter auf die rohe `Cadence != None` (`PositionProgressService.cs:761`); eine auf Kür gestellte Position
   erlässt noch offene Perioden, wie heute. *Begründung:* Wer eine Pflicht auf Kür stellt, erlässt sie (Ist-Stand 6); die
   Story behebt eine Serien-Lücke, keine Malus-Regel. Die Kistenstufe (Münzen) folgt der Serie ohne Sonderregel: die
   Rücknahme eines Defekts, keine neue Ökonomie. *Kosten:* Die Abrechnung läuft lazy beim Login
   (`AuthController.cs:85`, Fenster `MaxSettleLookbackDays = 14`); stellt der Vater vor dem nächsten Login auf Kür, entfällt
   auch der Malus für eine schon abgelaufene, nie abgerechnete Periode. *Verworfen:* vor dem Wechsel abrechnen: das änderte
   das heutige Malus-Verhalten. *Schließt aus:* „Kür ab morgen, aber der verpasste Montag zählt noch“. **Bei Bedarf
   nachfragen:** ob der Wechsel vorher abrechnen soll.
6. **AK 4 und AK 2 sind Gegenproben.** Sie prüfen Zustände, die im Ausgangszustand schon stimmen; sie fallen nur durch eine
   falsche Reparatur und sind so beschriftet.

## Akzeptanzkriterien

Je Kriterium die Änderung, die es rot macht.

1. Ein Plan mit einer einzigen Tagespflicht, an vier Tagen erfüllt, Serie 4: nach PATCH `cadence: None` bleiben
   `ChildStreakAsync` und `CompleteChildDaysAsync` für diese vier Tage bei „vollständig“. *Rot heute (Serie 0). Rot auch, wenn
   `DutyUntil` beim Wechsel nicht geschrieben oder der Helfer `DutyCadenceOn` nicht gelesen wird.*
2. **Gegenprobe:** Derselbe Fall, ein Tag **nach** dem Wechsel, ohne weitere Pflicht: unvollständig. *Im Ausgangszustand
   schon so; rot nur, wenn jemand Fall 1 durch „Tage ohne Pflicht zählen als vollständig“ repariert.*
3. Eine Wochen-Pflicht, die am Mittwoch erfüllt wurde und danach auf Kür gestellt wird: Donnerstag bis Sonntag derselben
   Woche bleiben vollständig, die Vorwochen ebenso. *Rot, wenn `CadenceUntil` nicht mitgespeichert wird (dann wertet
   `IsGoalMetAsync` tageweise) oder `DutyUntil` nur auf heute zeigt.*
4. **Gegenprobe:** Eine Position, die vor der Abrechnung auf Kür gestellt wird, bucht für nicht abgerechnete Perioden
   weiterhin keinen Malus. *Im Ausgangszustand schon so; rot nur, wenn die Historie in den Filter von
   `SettleClosedPeriodsAsync` (`:761`) durchschlägt oder jemand vor dem Wechsel abrechnet.*
5. Ein Plan mit Spieldaten lässt sich nicht löschen (`409 plan_has_data`), ein leerer schon (`204`); die Serie des Kindes ist
   nach dem abgewiesenen Löschen unverändert. *Rot, solange der Plan-Delete ungesperrt ist. Der bestehende Test
   `Plan_Loeschen_EntferntPlanMitGespielterPosition` wird dafür umgeschrieben, nicht gelöscht.*
6. Bestand: nach der Migration sind `DutyUntil` und `CadenceUntil` bei jeder Position `NULL`; an einer Kopie der echten DB
   ändern sich Serie und Malus-Stand der Kinder nicht (Vorbild `ActivatedAtMigrationTests`). *Rot, wenn die Migration einen Wert
   vorbelegt.*
7. Der Wechsel schließt die laufende Periode richtig: Ist sie erfüllt, zählt heute mit (`DutyUntil` = Periodenende); ist sie
   offen, zählt heute nicht mit (`DutyUntil` = Tag vor dem Periodenbeginn). *Rot, wenn immer „heute“ oder immer „gestern“
   geschrieben wird.*
8. Ein Speichern mit `cadence: None` auf einer schon freien Position oder mit unverändertem Takt verschiebt `DutyUntil`
   nicht. *Rot, wenn jedes PATCH mit `cadence` die Historie neu schreibt.*
9. Pflicht → Kür → Pflicht: Nach dem zweiten Wechsel trägt die Position wieder eine Pflicht ab heute (`DutyUntil` und
   `CadenceUntil` sind gelöscht). *Rot, wenn die gelöschte Spanne die neue Pflicht verneint (die Serie der neuen Pflicht
   zählt dann nie).*

## Schätzung

**Größe: L** – wie eine DB-Umbau-Etappe (E6, Löschverhalten explizit plus Bestand mitnehmen): zwei neue Spalten an einem
Schema mit Bestandsdaten, ein neuer Lesehelfer an der Stelle, die Serie, Missionen, Zeitachse, Kistenstufe und Zielzahlung
gemeinsam tragen (`PositionProgressService`), eine neue Schreibregel im Positions-PATCH und eine Sperre im Plan-Löschpfad
mit einem umzuschreibenden Test. Jeder Teil ist klein, die Streuung der Lesestellen macht es zu einem L.

- **`migration: ja`** – nachgesehen: `DutyUntil` (`DateOnly?`) und `CadenceUntil` (`GoalCadence?`, in der DB als Text, G4)
  an `PlanPositions`. Die Migration hängt an die Kette (letztes Glied heute `20261119065732_ShopStockPerChild`; B-700 bis
  B-702 hängen sich bei ihrem Bau davor oder dahinter, die Kette wird nur verlängert), ohne Backfill, `NULL` = „nie Kür
  gewesen“, mit Probe gegen eine Kopie der echten DB (`SchemaGuardTests` G1b, G9: kein SQL-DEFAULT).
- **`vertragsbruch: nein`** – nachgesehen: `Pugling.Contracts` ändert sich nicht; der neue Fehlercode `plan_has_data` steht in
  `ApiErrors` der Api (additiv, Muster `PositionHasData`, `PlanPositionsController.cs:411`) und das Löschen der Position
  bleibt unberührt. Die Spalten sind kein Teil der Antwort.
- **`wo: backend`** – die Meldung des 409 zeigt die Vater-Oberfläche schon als Fehlerbanner (`VaterPlanDetail.tsx:34`, die
  Aktion läuft über `action.run`); der Bestätigungstext vor dem Löschen („… gehen verloren“) wird dadurch unwahr für Pläne
  mit Spielstand. Das ist eine Textzeile, kein Akzeptanzkriterium (siehe Risiken).
- **Reihenfolge mit B-248:** **B-659 baut zuerst** und legt `DutyUntil`, `CadenceUntil`, den Helfer `DutyCadenceOn` und die
  Schließ-Regel an. Der Teil (c) von B-248 ([B-752](B-752-vorbereitungs-pflicht-im-fenster.md) und
  [B-758](B-758-vorbereitungs-pflicht-folgt-der-arbeit.md)) hängt daran und legt nur seine eigenen Spalten an.

### Risiken

- **Lesestellen, die `pos.Cadence` roh lesen, sind mehr als die vier, die die Story nennt.** Nachgesehen: neben
  `HasDutyOn` (`:55-56`), `IsGoalMetAsync` (`:197-198`), Tagesstatus (`:264-266`) und der Tagesansicht (`:325`) liest auch
  `EvaluateAndAwardAsync` (`:547-548`) `pos.Cadence` für `PeriodStart` und den Eintrag, und `:731` fragt `pos.Cadence ==
  Daily`. Bleibt `:547` roh, bucht ein Wochenziel nach dem Wechsel zur Kür an den Tagen bis `DutyUntil` (Donnerstag bis
  Sonntag, AK 3) mit `Cadence = None` eine **zweite** Zielzahlung, denn der Doppelschutz (`SamePeriodCadences`) vergleicht
  die Taktung. Der Helfer gehört an **jede** Lesestelle außer dem Malus-Filter (`:761`, AK 4); ein Test „erfüllte Woche,
  Wechsel zur Kür, Login am Donnerstag: eine Buchung, nicht zwei“ ist Pflicht.
- **Geschenkte Serientage.** Ist die laufende Periode beim Wechsel erfüllt, gelten die Folgetage bis zum Periodenende ohne
  weiteres Spielen als vollständig (AK 3 will das ausdrücklich). Es sind bis zu sechs Tage; das ist Entscheidung 2, nicht
  Defekt, hebt aber die Kistenstufe an (Entscheidung 5).
- **Nacharbeit (`UntilDone`).** Der PATCH kennt zwei Wege in die Kür; die Schließ-Regel gilt nur für Daily/Weekly → None.
  Pflicht → Kür über beendete Nacharbeit läuft über `CompletedOn` (B-636, B-669), und
  `RemediationPositionTests.Beendete_Nacharbeit_traegt_die_Serie_nicht_weiter` (`RemediationPositionTests.cs:685-703`)
  darf nicht kippen.
- **„Erfüllt“ im Controller.** Die Schließ-Regel braucht `IsGoalMetAsync` im PATCH, das heute nur der Service kennt; der
  Controller bekommt eine Abhängigkeit mehr, und die Auswertung muss mit dem **alten** Takt vor dem Überschreiben laufen
  (`PlanPositionsController.cs:361`).
- **Neuer 409 im Löschpfad.** Wer Pläne mit Spielstand wegräumt, scheitert künftig. Außer dem umzuschreibenden Test
  (`PlanPositionCrudTests.cs:199`) fand die Suche im Test- und E2E-Bestand keinen zweiten Treffer; das Skript
  `scripts/_plan-api.ps1:89` rät noch „ggf. löschen“ (nicht Teil dieser Story).
- **Wiederholte Wechsel.** Pflicht → Kür → Pflicht bleibt bekannter Rest (Entscheidung 3); AK 9 hält nur fest, dass die
  Spanne dabei gelöscht wird.
- **Nicht nachgestellt:** die Aussage „heute Serie 0“ in AK 1 ist am Code gelesen, nicht durch einen Lauf belegt. Die rote
  Probe ist der erste Handgriff beim Bauen.

### Angriffsplan

1. Entity, EF-Konfiguration (Enum als Text) und Migration
   (`dotnet dotnet-ef migrations add DutyUntilCadenceUntil --project backend/Pugling.Api --output-dir Data/Migrations`).
2. Helfer `DutyCadenceOn(pos, day)` in `PositionProgressService` und an alle Lesestellen außer `:761`
   (`:55-56`, `:197-198`, `:264-266`, `:325`, `:547-548`, `:731`); danach eine Suche nach `.Cadence` in Services und
   Controllern, ob eine rohe Lesestelle übrig bleibt.
3. PATCH (`PlanPositionsController.cs:357-361`): nur beim Übergang Daily/Weekly → None die laufende Periode schließen
   (`PeriodRange`, erfüllt? mit altem Takt) und beide Spalten schreiben; beim Übergang Kür → Pflicht beide löschen.
4. `StudyPlansController.Delete` (`:242-254`): Spieldaten prüfen (ein `TestAttempt` oder eine `PracticeSession` an irgendeiner
   Position), `409` mit neuem `ApiErrors.PlanHasData`, `ProducesResponseType(409)` setzen.
5. Test umschreiben (`Plan_Loeschen_EntferntPlanMitGespielterPosition`), neue Fälle, Migrationstest.
6. `dotnet build Pugling.sln`, `dotnet format Pugling.sln`.

### Testweg

- Neue Klasse `PflichtWirdKuerSerieTests` in `backend/Pugling.Api.Tests` (Aufbau wie `RueckwirkendePflichtTests`, das Pläne,
  Positionen und die Serie über die API aufbaut): AK 1, 2, 3, 7, 8, 9, dazu der Doppelbuchungs-Fall aus den Risiken. AK 2 und
  AK 4 sind Gegenproben und so zu beschriften (Entscheidung 6).
- AK 4: ein Fall in `PflichtMalusTests` (Position vor der Abrechnung auf Kür, kein Malus für nicht abgerechnete Perioden).
- AK 5: `PlanPositionCrudTests.Plan_Loeschen_EntferntPlanMitGespielterPosition` wird umgeschrieben (Erwartung `409
  plan_has_data`, Plan und Serie unverändert); ein Fall für den leeren Plan (`204`). Heute rot, weil der Delete ungesperrt ist.
- AK 6: neue Klasse `DutyUntilMigrationTests` nach dem Muster `ActivatedAtMigrationTests` (Bestandszeile vor der Migration,
  beide Spalten danach `NULL`, Serie und Malus-Stand unverändert).
- `RemediationPositionTests`, `RueckwirkendePflichtTests`, `PflichtMalusTests`, `BelohnungenTagespflichtTests`,
  `ConventionGuardTests`, `SchemaGuardTests` und `EndpointCoverageGuard` bleiben grün; `DocsCaptureTests` schreibt das neue
  409-Beispiel beim nächsten Lauf mit.
- Vor der Abnahme: Agent `pugling-reviewer`; `/smoke-test`; Rollengang als Vater (Pflicht auf Kür, Serie des Kindes prüfen)
  und die Probe der Migration an einer Kopie der echten `pugling.db`.

## Verlauf

- **2026-11-18** — angelegt aus dem Review von B-613 (Sprint 87), am Code gelesen.
- **2026-11-19** — ausformuliert (Vorbereitungslauf): Ist-Stand belegt; Positions-Löschen gegenstandslos
  (`position_has_data`), offen bleibt Pflicht → Kür und Plan löschen; Pflicht → Kür → Pflicht als breiterer Rest
  gefunden; Frage zum Malus als offener Punkt 5 ausdrücklich dem Nutzer überlassen.
- **2026-11-20** — gegrillt (Vorbereitungslauf, Sonnet, auf Anweisung des Nutzers „Entscheide die offenen Fragen nach deiner Empfehlung“; unbestätigt): Historie statt Überspringen; nullbares Paar `DutyUntil`/`CadenceUntil` mit einem Helfer und einer Schließ-Regel, gemeinsam mit [B-248](B-248-klassenarbeit-countdown-treibt-den-plan.md); Pflicht → Kür → Pflicht bleibt bekannter Rest (B-613); Plan löschen wird bei Spieldaten gesperrt (`plan_has_data`, der Test `Plan_Loeschen_EntferntPlanMitGespielterPosition` kehrt um); Malus unverändert. Akzeptanzkriterien final.
- **2026-11-20** — geschaetzt (Vorbereitungslauf, Sonnet, auf Anweisung des Nutzers): L, backend, Migration ja, Vertrag nein. Der Helfer muss an mehr Lesestellen als die Story nennt (auch `EvaluateAndAwardAsync`, sonst Doppelbuchung des Wochenziels nach dem Wechsel); B-659 baut vor dem Teil (c) von B-248 und legt `DutyUntil`, `CadenceUntil` und die Schließ-Regel an.
- **2026-11-22** — in-arbeit (Nachtlauf B 2026-11-23, Sprint B2). Gebaut: Migration `20261122081510_DutyUntilCadenceUntil`
  (zwei nullbare Spalten, ohne Vorbelegung), Helfer `PositionProgressService.DutyCadenceOn` an `HasDutyOn`, `IsGoalMetAsync`,
  `OpenGoalProgressAsync`, `ComputeDayAsync` und `EvaluateAndAwardAsync`; Schließ-Regel `CloseDutyAsync` (im PATCH vor jeder
  anderen Zuweisung, damit „erfüllt?“ mit den Einstellungen der Periode urteilt); Löschsperre `409 plan_has_data` über den
  schon vorhandenen `PlanHasPlayDataAsync` aus B-673 (zählt zusätzlich eingeführte Wörter mit; ein Code, eine Prüfstelle).
  **Abweichungen, stellvertretend entschieden:** (a) Der Code `plan_has_data` gab es schon (B-673, Umhängen); er wird
  wiederverwendet, statt einen zweiten anzulegen. Der deutsche Satz nennt beide Sperren, den Ausweg „Deaktivieren“ nur am
  Löschen (Review 🟡2). (b) Die rohen Lesestellen in der Abrechnung (`IsIntroductionDaySparedAsync`) und im Missionsfilter
  von `ComputeDayAsync` bleiben roh: dort ist der Helfer äquivalent (die Spanne gibt es nur bei `Cadence = None`, solche
  Positionen erreichen die Abrechnung nicht), und eine ungetragene Änderung wäre Ballast. (c) Bestätigungstext des Vaters
  (`VaterPlanDetail.tsx`) und Client-Doku nachgezogen; `scripts/_plan-api.ps1:89` bleibt wie in der Story vorgesehen.
  Neue Spec `frontend/e2e/plan-loeschen-gesperrt.spec.ts`.
  **Rote Probe vor dem Fix** (`PflichtWirdKuerSerieTests`, 12 Fälle): 7 failed / 5 passed – erwartet: grün nur die beiden
  Gegenproben (AK 2, AK 4), der Malus-Maßstab, AK 9 und der Doppelbuchungs-Fall (die beiden letzten fallen erst unter einer
  falschen Reparatur, Mutanten M11/M12 unten).
  **Migration an einer Kopie der echten DB** (per SQLite-Backup-API gelesen, nur lesend geöffnet): Stand
  `20261119065732_ShopStockPerChild`, `database update` spielt `DutyUntilCadenceUntil` ein; 204 Positionen, 0× `DutyUntil`,
  0× `CadenceUntil` gesetzt. Damit liest jeder Tag den heutigen Takt wie vorher (`DutyUntilMigrationTests`); Serie und Malus
  der Kinder bewegen sich nicht. Die Kopie ist danach gelöscht.
  **Review `pugling-reviewer`:** 2× 🟡 (der Bericht sprach von drei, nummeriert waren zwei), beide mit Code-/Teständerung
  gezählt (Zähler Sprint B2: 2 von 5):
  🟡1 Helfer lieferte nach der Spanne den aktuellen Takt statt `None` (Entscheidung 2; für B-248 nötig) – behoben, plus
  Einzelfall `Nach_Der_Spanne_Ist_Keine_Pflicht`; 🟡2 der deutsche Satz bot „Deaktivieren“ auch fürs Umhängen an –
  behoben; die AK-9-Begründung im Controller-Kommentar stimmt damit wieder. 🟢3 Schließ-Regel vor die Zuweisungen
  gezogen; 🟢4 laufende Nacharbeit → Kür als [B-820](B-820-laufende-nacharbeit-zur-kuer-bricht-die-serie.md) angelegt
  (bewusst nicht hier, Entscheidung 2); 🟢5 Doku von `ApiErrors.PlanHasData` und ein Testkommentar korrigiert.
  `ActivatedAtMigrationTests` liest seit dieser Migration projiziert (es lud die ganze Entity auf einem älteren Stand und
  fiel an der neuen Spalte – gefunden im vollen Lauf, nicht vom Review).
  **Mutanten-Probe nach der letzten Änderung** (`mutant-probe.py`, Debug):

  | Mutante | Stelle | erwartet | gemessen | trägt |
  |---|---|---|---|---|
  | M1 `HasDutyOn` roh | PositionProgressService.cs:55 | red | red | AK 1 |
  | M2 Grenze `<` statt `<=` | :68 | red | red | AK 1 |
  | M3 Takt der Spanne ignoriert | :68 | red | red | AK 1 |
  | M14 nach der Spanne aktueller Takt | :69 | red | red | Entscheidung 2 (Einzelfall) |
  | M4 `IsGoalMetAsync` roh | :210 | red | red | AK 3 |
  | M5 Schließen auch ohne Wechsel | :230 | red | red | AK 8 |
  | M6 immer Periodenende | :232 | red | red | AK 7 offen |
  | M7 immer Tag davor | :232 | red | red | AK 7 erfüllt |
  | M8 `CadenceUntil` fehlt | :233 | red | red | AK 3 |
  | M9 Fortschrittsbalken roh | :293 | green | green | **ungetragen**: Anzeigewert eines offenen Ziels, innerhalb einer Spanne nur an verpassten Vergangenheitstagen erreichbar |
  | M10 Tagesansicht roh | :356 | red | red | Tagesansicht-Fall |
  | M11 Belohnung roh | :590 | red | red | Doppelbuchung |
  | M12 Spanne bleibt beim Zurück | PlanPositionsController.cs:357 | red | red | AK 9 |
  | M13 Löschen offen | StudyPlansController.cs:283 | red | red | AK 5 (beide Fälle) |
  | E1 Löschen offen (E2E) | StudyPlansController.cs:283 | red | red | `plan-loeschen-gesperrt.spec.ts` |
