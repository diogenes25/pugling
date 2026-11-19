---
tags: [typ/story, status/gegrillt, bereich/gamification, rolle/student, rolle/supervisor]
aliases: [Plan umhängen erbt Malus]
status: gegrillt
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

1. ~~**Darf man einen gespielten Plan überhaupt umhängen?**~~ → Entscheidung 1.
2. ~~**Ungespielte Pläne:**~~ → Entscheidung 2.
3. ~~**Berührt das die Malus-Regeln?**~~ → Entscheidung 3.
4. ~~**Gesperrter Fall im UI:**~~ → Entscheidung 4.
5. ~~**Was heißt „gespielt“?**~~ → Entscheidung 1 (neu beim Grillen).

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-19 (Sonnet), unbestätigt.*

1. **Ein gespielter Plan lässt sich nicht umhängen: `PATCH` mit einem anderen `childId` antwortet `409` mit dem neuen,
   additiven Code `plan_has_data`. „Gespielt“ heißt: an irgendeiner Position des Plans liegt mindestens eine
   `PracticeSession`, ein `TestAttempt` oder eine `PositionItemProgress`-Zeile.** Begründung: Der Spielstand hängt am Plan
   und wird über `StudyPlan.ChildId` dem Kind zugeordnet (Ist-Stand 3); ein Umhängen kann ihn dem zweiten Kind nur
   mitgeben oder zerstören, beides falsch. Die drei Tabellen sind absichtlich mehr als die Positions-Sperre prüft
   (`PlanPositionsController.cs:409-411`: nur `TestAttempts` und `PracticeSessions`): Die Abrechnung des Malus beginnt am
   ersten Tag, an dem das Kind ein Wort **eingeführt** hat (`FirstPlayDaysAsync`, `PositionProgressService.cs:849-866`,
   liest `PositionItemProgress.IntroducedAt`), und genau das übernähme das zweite Kind. Belegt am Code: die Sperre ist
   ein **Vorbild**, keine Repo-weite Konvention. Fundstelle: `position_has_data` an der Position
   (`PlanPositionsController.cs:409-411`, Fehlercode `ApiErrors.cs:205`, deutscher Satz `frontend/src/lib/api.ts:188`).
   Gegenfundstelle: `StudyPlansController.Delete` löscht einen ganzen Plan samt Spielstand ohne jede Sperre
   (`StudyPlansController.cs`, Methode `Delete`, Kommentar „Removes its positions via cascade incl. progress, practice
   sessions, test attempts and goal rewards“). Die Regel hier gilt also für das **Umhängen**, nicht für jede
   Plan-Änderung, und stützt sich auf den Besitz des Spielstands, nicht auf eine Konvention. Verworfen: den Spielstand
   beim Umhängen löschen (still zerstörend, der erste Sohn verlöre Tage) und ihn mitnehmen (das heutige Verhalten: das
   zweite Kind erbt Serie und Malus-Beginn). Kosten: Der Vater kann einen gespielten Plan nicht mehr dem Bruder geben; er
   legt für ihn einen neuen an. Ein bloß einmal geöffneter Plan ist nur gesperrt, wenn eine `PositionItemProgress`-Zeile
   entstand – nach dem Code erzeugt sie nur `ProgressForAsync` auf Spielwegen (`PositionPlayService.cs:633-646`). Ausgeschlossener Fall:
   „gib meinem Zweiten dasselbe Programm“ aus einem schon gespielten Plan. Gegen die User Story („mit einer leeren
   Vergangenheit beginnen“) ist das die richtige Lücke: Ein Umhängen kann nicht leer beginnen lassen, ohne dem ersten Kind
   etwas zu nehmen; das Kopieren eines Plans ist ein Wunsch für eine eigene Story, nicht dieser Defekt.
2. **Ein ungespielter Plan darf umgehängt werden; war er aktiv und bleibt er es, setzt der Wechsel `ActivatedAt` auf
   heute.** Begründung: Die bestehende Regel (B-613, `StudyPlanEntities.cs:90-97`, `ActiveOn` `PositionProgressService.cs:82`):
   Tage vor dem Einschalten zählen zugunsten des Kindes. Das Zielkind schaltet den Plan mit dem Wechsel faktisch ein;
   `StudyPlansController.Update` setzt `ActivatedAt = today` an genau dieser Stelle beim Einschalten (Zeile 220). Es wird
   schlicht `today` gesetzt, ohne `max` mit dem alten Wert: bei einem Plan mit künftigem Start steht `ActivatedAt = today`
   schon beim normalen Einschalten vor dem Start, und `ActiveOn` zusammen mit dem Start ergibt dasselbe. Ein inaktiver Plan
   bleibt unberührt (das Einschalten setzt es später); schaltet derselbe `PATCH` ihn gleichzeitig ein, gilt die
   bestehende Zeile 220. Der Wechsel sitzt **vor** dem Block für verschobene Starts (Zeilen 225-229), damit dieser auf dem neuen
   Stand rechnet. Verworfen: `ActivatedAt` bedingungslos beim Kindwechsel überschreiben (ein inaktiver Plan würde ein
   Datum tragen, das niemand je eingeschaltet hat). Kosten: ein weiterer Schreibweg für `ActivatedAt` neben Einschalten und
   Start-Verschieben. Ausgeschlossener Fall: ein Plan, den das Zielkind **rückwirkend** bespielen soll (Nachholen) –
   dafür gibt es Nachtrag durch den Vater, nicht den Wechsel; gegen die User Story („weder die Tage meines Bruders meine
   Serie verfälschen noch …“) trägt es, weil das Kind erst ab dem Zuweisungstag Pflichten hat.
3. **Keine neue Malus-Regel.** Entscheidung 2 wendet die vorhandene Fairness-Regel („was vor dem Einschalten lag, kann nicht
   verpasst worden sein“, `PlanDueForPeriod`, `PositionProgressService.cs:673-679`) auf einen weiteren Schreibweg an,
   Entscheidung 1 hält einen Plan dort, wo sein Spielstand liegt. Keine Münzhöhe, kein Takt, keine Abrechnung ändert
   sich; wer heute einen gespielten Plan umhängt, bekommt künftig `409` statt eines stillen Erbes. Begründet, weil der Lauf
   Malus- und Münz-Regeln nicht neu setzen darf. Zur Kenntnis für die Bestätigung: Die Sperre verbietet einen heute
   möglichen Vater-Weg.
4. **Das Vater-Web zeigt den Grund, es sperrt die Kind-Auswahl nicht vorab.** `plan_has_data` bekommt in
   `frontend/src/lib/api.ts` (neben Zeile 188) einen deutschen Satz, den die Plan-Bearbeitung über ihr
   `StatusBanner` (`VaterPlanDetail.tsx:30`, `:86`) zeigt – nach dem Muster der Positions-Sperre. Das Feld `PlanResponse`
   bekommt **kein** Kennzeichen „gespielt“. Verworfen: ein Server-Kennzeichen wie `IsPlayable` (`StudyPlanDtos.cs:25-31`
   ist der Vorläufer: der Server trägt die Regel, der Client rechnet sie nicht nach) mit abgeschalteter Auswahl. Kosten:
   Der Vater erfährt es erst beim Speichern, und das Formular hat keinen Weg, die Auswahl vorher zu erklären.
   Ausgeschlossener Fall: ein Vater, der die Kind-Auswahl schon vorher gesperrt sehen wollte; das Umhängen kommt selten vor,
   und ein Vertragsfeld für einen Fehlerfall wäre mehr als der Defekt braucht. Wird es zum Ärger, ist das Kennzeichen eine
   additive Folgestory.

## Akzeptanzkriterien

Je Kriterium: die Änderung, die es rot macht.

1. Ein Plan mit einer `PracticeSession` – in zwei weiteren Fällen mit einem `TestAttempt`, mit einer
   `PositionItemProgress`-Zeile –: `PATCH` mit anderem `childId` liefert `409` `plan_has_data`; der Plan bleibt beim ersten
   Kind, Serie und Konto beider Kinder bleiben unverändert. Rot heute (200, der Besitzer wechselt); rot, wenn die Sperre nur
   eine der drei Tabellen prüft (jeder Fall fällt einzeln).
2. Der `409` ändert nichts: ein `PATCH` mit neuem Titel **und** anderem `childId` auf einen gespielten Plan lässt den Titel
   unverändert. Rot, wenn die Sperre erst nach `SaveChangesAsync` greift.
3. Aktiver, ungespielter Plan, der seit vor drei Tagen läuft; das Zielkind hat diese Tage mit einem anderen Plan (anderes
   Fach) erfüllt: nach dem Umhängen reißt seine Serie nicht (`ChildStreakAsync` ohne Lücke an den drei Vortagen),
   `ActivatedAt` steht auf dem Tag des Wechsels. Rot heute (der Plan hat dort offene Pflichten); rot, wenn
   `ActivatedAt` nicht gesetzt wird.
4. Ein inaktiver, ungespielter Plan wird umgehängt: `ActivatedAt` bleibt (der Test liest den Wert aus der Datenbank, die
   API zeigt ihn nicht). Rot, wenn der Kindwechsel `ActivatedAt` bedingungslos überschreibt.
5. Ein `PATCH` ohne `childId`, oder mit dem gleichen `childId`, auf einen **gespielten** Plan (nur Titel) liefert `200` und
   ändert `ActivatedAt` nicht. Rot, wenn die Prüfung vor dem `newChildId != plan.ChildId`-Zweig (`StudyPlansController.cs:183`)
   greift.
6. `Kindwechsel_Schaltet_Beim_Neuen_Kind_Dasselbe_Fach_Ab` (`PlanSubjectTests.cs:262`) bleibt grün. Rot, wenn die Sperre die
   Reihenfolge von Speichern und `DeactivateSameSubjectPlansAsync` verändert.
7. Das Vater-Web übersetzt `plan_has_data` in einen deutschen Satz (Muster `errorMessage.test.ts:72-80`, `exercise_in_use`).
   Rot, wenn der Code kein Mapping hat: der Vater sähe den englischen Server-Text.

## Verlauf

- **2026-11-18** — angelegt aus der Nachschau von Sprint 87 (Hinweis 2), unverifiziert.
- **2026-11-19** — ausformuliert (Vorbereitungslauf): am Code belegt; die Produktfrage ist beantwortet, das Umhängen ist
  ein ausgebauter Vater-Weg (Vertrag und Formular). Der Vorschlag `ActivatedAt = heute` allein genügt nicht; Empfehlung
  Sperre für gespielte Pläne plus `ActivatedAt` bei ungespielten.
- **2026-11-19** — gegrillt (Vorbereitungslauf, Sonnet, stellvertretend, unbestätigt): Entscheidungen 1 bis 4. Gespielter Plan: Umhängen gesperrt (`409 plan_has_data`, drei Tabellen, mehr als die Positions-Sperre prüft); ungespielter aktiver Plan: `ActivatedAt = heute` beim Wechsel (B-613 auf einem weiteren Schreibweg); keine neue Malus-Regel. Die Sperre ist ein Vorbild (`position_has_data`), keine Repo-Konvention (`StudyPlansController.Delete` löscht ohne Sperre). Akzeptanzkriterien final, je mit roter Änderung.
