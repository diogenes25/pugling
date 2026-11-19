---
tags: [typ/story, status/ausformuliert, bereich/gamification, rolle/student, rolle/supervisor]
aliases: [Pflicht zur Kür, gelöschte Pflicht bricht die Serie]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
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

1. **Historie oder Überspringen?** Soll eine früher erfüllte Pflicht für vergangene Tage weiter zählen, oder genügt es,
   dass ein Tag ohne Pflicht die Serie überspringt? *Empfehlung: Historie.* Überspringen würde den Test aus Beleg 2
   kippen (nach dem Ende der Nacharbeit gilt ein Tag ohne Pflicht heute bewusst als unvollständig) und der Serie jede
   Lücke durchgehen lassen, sobald der Vater alles auf Kür stellt.
2. **Wie sieht die Historie aus?** *Empfehlung:* zwei nullable Spalten an `PlanPosition` – „Pflicht bis“ und die
   Taktung zu diesem Zeitpunkt –, gesetzt beim Wechsel Pflicht → Kür; `HasDutyOn` wertet sie für Tage ≤ „bis“ mit der
   alten Taktung aus (die Wochen-Pflicht muss weiter als Woche gelten, `IsGoalMetAsync` rechnet bei `None` sonst tageweise,
   `PositionProgressService.cs:198`). Das ist **eine** Migration auf der echten DB, ohne Backfill (Bestandszeilen:
   `NULL` = „nie Kür gewesen“), gegen eine Kopie zu proben. Verworfen als Alternative: ein eingefrorenes Tages-Urteil
   (neue Tabelle je Kind und Tag) – würde auch B-613/B-673 mittragen, ist aber ein Umbau der Serien-Logik.
3. **Pflicht → Kür → Pflicht mit abdecken?** Eine Spanne je Position kann nur **eine** frühere Pflicht-Phase halten.
   *Empfehlung: nein, hier nicht* – bekannter Rest, als Grenze in die Entscheidung schreiben und notfalls eine Story
   anlegen. Der zweite Wechsel überschreibt die Spanne.
4. **Plan löschen:** *Empfehlung:* sperren, sobald der Plan Spieldaten trägt (`409` mit neuem, additivem Code,
   Muster `position_has_data`), mit dem Hinweis „stattdessen ausschalten“. Ein ausgeschalteter Plan behält seine
   erfüllten Tage (`IsCompleteDay` zählt `Done` ohne `Active`, Zeile 442). Verworfen: Löschen erlauben und die Tage
   einfrieren – bräuchte die Tages-Tabelle aus Punkt 2.
5. **MALUS-/MÜNZ-FRAGE (für den Nutzer):** Soll eine früher erfüllte Pflicht, die heute Kür ist, auch für die
   **Abrechnung** noch offener Perioden als Pflicht gelten? *Empfehlung: nein – Malus unverändert.* Wer eine Pflicht
   auf Kür stellt, erlässt sie; das gilt heute (Beleg 6). Die Historie soll nur Serie, Missionen, Zeitachse und
   Kistenstufe betreffen. Die Wirkung auf die Kistenstufe (Münzen) ist die gewollte Rückgängig-Machung eines
   Defekts, keine neue Regel; die Frage bleibt trotzdem beim Nutzer, weil sie die Münz-Ökonomie berührt.

## Akzeptanzkriterien (Entwurf)

Je Kriterium: die Änderung, die es rot macht. Entwurf – final erst nach dem Grillen der Punkte 1-5.

1. Ein Plan mit einer einzigen Tagespflicht, an vier Tagen erfüllt, Serie 4: nach PATCH `cadence: None` bleiben
   `ChildStreakAsync` und `CompleteChildDaysAsync` für diese vier Tage bei „vollständig". *Rot heute (0). Rot auch,
   wenn die Spanne beim Wechsel nicht geschrieben wird.*
2. Derselbe Fall, Tag **nach** der Umstellung: ohne weitere Pflicht unvollständig, die Serie zählt dort nicht weiter.
   *Rot, wenn jemand den Fall 1 durch „Tage ohne Pflicht zählen als vollständig" repariert.*
3. Eine Wochen-Pflicht, die auf Kür gestellt wird, gilt für die Tage der alten Woche weiter nach **Wochen**-Regel
   (Test mit einer Woche, die am Mittwoch erfüllt wurde: auch Donnerstag bis Sonntag bleiben vollständig). *Rot, wenn die
   Taktung nicht mitgespeichert wird (dann wird tageweise gewertet).*
4. Eine Position, die vor der Abrechnung auf Kür gestellt wird, bucht für nicht abgerechnete Perioden weiterhin keinen
   Malus. *Rot, wenn die Historie in den Filter von `SettleClosedPeriodsAsync` (Zeile 761) durchschlägt.*
5. Ein Plan mit Spieldaten lässt sich nicht löschen (`409`, Code vom Typ `plan_has_data`), ein leerer schon (`204`);
   die Serie des Kindes ist nach dem abgewiesenen Löschen unverändert. *Rot, solange der Plan-Delete ungesperrt ist.*
6. Bestand: nach der Migration ist die Spanne bei jeder Position `NULL`, an einer Kopie der echten DB ändern sich Serie
   und Malus-Stand der Kinder nicht (Vorbild `ActivatedAtMigrationTests`). *Rot, wenn die Migration einen Wert
   vorbelegt.*

## Verlauf

- **2026-11-18** — angelegt aus dem Review von B-613 (Sprint 87), am Code gelesen.
- **2026-11-19** — ausformuliert (Vorbereitungslauf): Ist-Stand belegt; Positions-Löschen gegenstandslos
  (`position_has_data`), offen bleibt Pflicht → Kür und Plan löschen; Pflicht → Kür → Pflicht als breiterer Rest
  gefunden; Frage zum Malus als offener Punkt 5 ausdrücklich dem Nutzer überlassen.
