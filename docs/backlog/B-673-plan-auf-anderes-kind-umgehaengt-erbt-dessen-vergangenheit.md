---
tags: [typ/story, status/ausformuliert, bereich/gamification, rolle/student, rolle/supervisor]
aliases: [Plan umhängen erbt Malus]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: "Nachtlauf 2026-11-18, Nachschau von Sprint 87 (fe13531c, pugling-reviewer, Hinweis 2) – docs/pm-sitzung-2026-11-18.md, Runde 5"
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-673 · Ein Plan, den der Vater einem anderen Kind gibt, bringt die Vergangenheit des ersten Kindes mit

Der Vater kann in der Plan-Bearbeitung das Kind wechseln. Der Plan wechselt den Besitzer, nimmt aber Spielstand, Serie
und Abrechnungs-Anfang des ersten Kindes mit – das zweite Kind erbt dessen Tage und womöglich dessen Malus.

## User Story

Als Schüler möchte ich einen Plan, den Papa mir zuweist, mit einer leeren Vergangenheit beginnen, damit weder die
Tage meines Bruders meine Serie verfälschen noch seine offenen Tage mir Münzen kosten.

## Ist-Stand am Code

Gelesen am 2026-11-19, nicht per Test nachgestellt.

1. **Umhängen ist ein ausgebauter Weg, kein Versehen.** `UpdatePlanDto.ChildId` ist Vertrag
   (`backend/Pugling.Contracts/Supervisor/StudyPlanDtos.cs:66`, `:72-73`: „Moves the plan to another of the caller's own
   children"); die Plan-Bearbeitung im Frontend bietet die Kind-Auswahl an
   (`frontend/src/vater/VaterPlanDetail.tsx:161`, `:179-181`); ein Test sichert die Fach-Regel beim Wechsel
   (`PlanSubjectTests.cs:262-278`).
2. **Der Wechsel ändert nur `ChildId`.** `StudyPlansController.Update`
   (`backend/Pugling.Api/Controllers/Supervisor/StudyPlansController.cs:183-187`) setzt `plan.ChildId`; `ActivatedAt`
   ändert sich nur beim Einschalten eines ausgeschalteten Plans (Zeile 220) oder beim Verschieben des Starts (Zeilen
   225-229). Positionen, `PositionItemProgress`, `PracticeSessions`, `TestAttempts`, `PositionGoalRewards` und
   `PositionGoalPenalties` bleiben am Plan, also beim neuen Kind. (Das Fach des Zielkinds wird danach über
   `DeactivateSameSubjectPlansAsync`, Zeile 232, bereinigt – das ist der einzige Nachlauf.)
3. **Der Spielstand hängt am Plan, nicht am Kind.** `TestAttempt` und `PracticeSession` tragen keine `ChildId`; sie werden
   über `StudyPlan.ChildId` zugeordnet (`MetricsService.cs:31-41`, `PositionProgressService.cs:856-859`). Die
   Zuordnung läuft nach dem Wechsel also automatisch auf das neue Kind.
4. **Serie und Tage:** `DutyStateAsync` (`PositionProgressService.cs:461-471`) prüft `IsGoalMetAsync` gegen die Sitzungen
   und Versuche des Plans – die des ersten Kindes. `IsCompleteDay` (Zeilen 439-443) zählt `Done` ohne `Active`. Die
   erfüllten Tage des ersten Kindes werden damit zu **erfüllten Tagen des zweiten** (zu seinen Gunsten); die offenen
   Tage eines aktiven Plans (`ActiveOn`: `day >= ActivatedAt`, Zeile 82, bei einem Plan, der seit dem `StartDate`
   läuft) zu **offenen Tagen** – die Serie des neuen Kindes reißt an Tagen, die es nie hätte spielen können.
5. **Malus:** `SettleClosedPeriodsAsync` (Zeilen 754-840) holt alle Positionen der Pläne des Kindes, nimmt als Beginn
   den ersten Spieltag des Plans aus `FirstPlayDaysAsync` (Zeilen 849-866) – die Einführungstage und Versuche des
   **ersten** Kindes – und rechnet nicht abgerechnete Perioden der letzten 14 Tage (`MaxSettleLookbackDays`, Zeile 25)
   ab. `PlanDueForPeriod` (Zeilen 677-679) lässt sie wegen `ActivatedAt <= StartDate` fällig. Folge bei einem gespielten Plan: das neue Kind
   zahlt `PenaltyCoins` für Perioden, in denen es nie Zugang hatte. Bereits gebuchte Belohnungen und Strafen des ersten
   Kindes liegen als `PositionGoalReward`/`PositionGoalPenalty` an der Position; die Idempotenz-Prüfung (Zeilen 791-794)
   überspringt diese Perioden – für das zweite Kind heißt das: keine Belohnung dort, keine Strafe dort.
6. **Reine Vermutung der Idee, hier nicht belegt:** „ActivatedAt = heute" als Lösung. Nach dem Code reicht das nicht
   (siehe Lücke).
7. Ein Test für „Plan mit Spielstand umhängen": **nicht gefunden** (`PlanSubjectTests.cs` prüft nur den Fach-Wechsel
   eines leeren Plans).

## Die echte Lücke

- Der Vorschlag der Nachschau, beim Kindwechsel eines aktiven Plans `ActivatedAt = heute` zu setzen, repariert beim
  **gespielten** Plan den **Malus** (`PlanDueForPeriod` verlangt dann `from >= ActivatedAt`) und die **offenen** Tage
  der Serie (`ActiveOn` ist davor falsch). Er repariert **nicht**, dass der erledigte Spielstand des ersten Kindes dem zweiten gutgeschrieben
  wird (`Done` hängt an den Sitzungen des Plans, nicht an `ActivatedAt`), noch dass dessen Leitner-Stufen und
  Einführungsstände (`PositionItemProgress`) am Plan hängen bleiben.
- Das Umhängen eines **ungespielten** Plans ist harmlos – bis auf `ActivatedAt`: ein seit seinem Start aktiver,
  ungespielter Plan lässt die Serie des neuen Kindes über seine vergangenen Tage reißen (Beleg 4).
- Das Umhängen eines **gespielten** Plans hat keinen sauberen Sinn: der Spielstand gehört dem ersten Kind. Gemeint ist
  fast immer „gib meinem Zweiten dasselbe Programm" – das ist ein Kopieren, kein Umhängen.

## Offene Punkte

1. **Darf man einen gespielten Plan überhaupt umhängen?** *Empfehlung: nein.* Sperre wie bei Positionen
   (`position_has_data`, `PlanPositionsController.cs:409-411`): hat der Plan `PracticeSessions`, `TestAttempts` oder
   `PositionItemProgress`, antwortet der PATCH mit `childId` mit `409` und einem additiven Fehlercode; das Kopieren eines
   Plans für ein zweites Kind ist ein eigenes Vorhaben (Backlog). Verworfen: den Spielstand beim Umhängen löschen (still
   zerstörend) und ihn mitnehmen (das heutige Verhalten).
2. **Ungespielte Pläne:** *Empfehlung:* umhängen bleibt erlaubt; ein aktiver Plan bekommt dabei `ActivatedAt = heute`
   (wie ein neues Einschalten, `StudyPlansController.cs:220`), damit Serie und Abrechnung des neuen Kindes erst ab dem
   Tag der Zuweisung laufen. Das wendet die Fairness-Regel von B-613 an, es ändert sie nicht.
3. **Berührt das die Malus-Regeln?** Nein: die Empfehlung setzt die bestehende Regel „was vor dem Einschalten lag,
   kann nicht verpasst worden sein" (`PlanDueForPeriod`, Zeile 673) auf einen weiteren Schreibweg an. Zur Kenntnis,
   weil sie die Abrechnung des neuen Kindes betrifft.
4. **Gesperrter Fall im UI:** die Kind-Auswahl in `VaterPlanDetail.tsx:179` müsste bei einem gespielten Plan abgeschaltet
   oder erklärt werden (Frontend folgt dem Backend, API-First).

## Akzeptanzkriterien (Entwurf)

Je Kriterium: die Änderung, die es rot macht. Entwurf – final erst nach Bestätigung der Punkte 1 und 2.

1. Plan mit einer Sitzung oder einem Versuch: PATCH `childId` → `409`; der Plan bleibt beim ersten Kind, Serie und
   Konto beider Kinder sind unverändert. *Rot heute (200, Besitzer wechselt); rot, wenn die Sperre fehlt oder nur
   `TestAttempts` prüft.*
2. Aktiver, ungespielter Plan, der seit vor drei Tagen läuft; das Zielkind hat diese Tage mit einem anderen Plan
   (anderes Fach) erfüllt: nach dem Umhängen reißt seine Serie nicht (`ChildStreakAsync` ohne Lücke an den drei
   Vortagen), `ActivatedAt` steht auf dem Tag des Wechsels. *Rot heute (der neue Plan hat dort offene Pflichten);
   rot, wenn `ActivatedAt` nicht gesetzt wird.* (Der Malus ist bei einem ungespielten Plan nicht betroffen: ohne Spieltag
   in `FirstPlayDaysAsync` rechnet die Abrechnung nichts ab. Das ist Absicht und braucht kein Kriterium.)
3. Ein inaktiver, ungespielter Plan wird umgehängt: kein Eingriff in `ActivatedAt` (das setzt erst das Einschalten).
   *Rot, wenn der Kindwechsel `ActivatedAt` bedingungslos überschreibt.*
4. Ein PATCH `childId` auf das **gleiche** Kind oder ohne `childId` ändert weder `ActivatedAt` noch löst er die Sperre
   aus. *Rot, wenn die Prüfung vor dem `newChildId != plan.ChildId`-Zweig (Zeile 183) greift.*
5. Der Fach-Nachlauf beim Wechsel (`PlanSubjectTests.cs:262`) bleibt grün. *Rot, wenn die Sperre die Reihenfolge der
   Speicherungen verändert.*

## Verlauf

- **2026-11-18** — angelegt aus der Nachschau von Sprint 87 (Hinweis 2), unverifiziert.
- **2026-11-19** — ausformuliert (Vorbereitungslauf): am Code belegt; die Produktfrage ist beantwortet, das Umhängen ist
  ein ausgebauter Vater-Weg (Vertrag und Formular). Der Vorschlag `ActivatedAt = heute` allein genügt nicht; Empfehlung
  Sperre für gespielte Pläne plus `ActivatedAt` bei ungespielten.
