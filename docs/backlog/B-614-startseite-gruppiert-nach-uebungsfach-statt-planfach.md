---
tags: [typ/story, status/ausformuliert, bereich/frontend, rolle/student]
aliases: [Fach der Übung statt des Plans]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Nachschau Sprint 83 im Nachtlauf 2026-11-17 (docs/pm-sitzung-2026-11-17-nachtlauf.md)"
grund: ""
ersetzt_durch: []
entgangen_bei: [B-606]
---

# B-614 · Die Startseite gruppiert nach dem Fach der Übung, nicht nach dem Fach des Plans

`PositionStatus.SubjectName` kommt aus `SubjectNameOf` (`Series.Subject?.Name ?? Series.SubjectName`, B-606), die
Startseite gruppiert danach (`tagessicht.ts`). Seit B-539 trägt jeder Plan ein Pflichtfach – die Gruppierung ignoriert
es: eine fachfremde Übung landet unter einer anderen Überschrift, ein Freitext „English" neben dem Katalognamen
„Englisch" ergibt zwei Gruppen, Positionen zweier Pläne teilen sich eine. Naheliegend: den Fachnamen des **Plans**
liefern.

## User Story

Als Schüler möchte ich auf der Startseite alle Aufgaben eines Plans unter dem Fach sehen, dem der Plan gehört, damit
meine Tagesliste so gegliedert ist, wie der Vater die Pläne angelegt hat – und nicht je nachdem, wie ein Lehrwerk
sein Fach geschrieben hat.

## Ist-Stand am Code

- **Quelle des Fachnamens:** `SubjectNameOf(pos)` liest das Fach der **Reihe** der Übung – Katalogname, sonst der
  Freitext der Reihe, sonst leer (`backend/Pugling.Api/Services/Shared/PositionProgressService.cs:391-392`). Es geht in
  `PositionStatus.SubjectName` (`PositionProgressService.cs:344`; Vertrag `backend/Pugling.Contracts/Shared/ProgressDtos.cs:49`).
- **Der Plan trägt ein Fach:** `StudyPlan.SubjectId`/`Subject` (`backend/Pugling.Api/Models/StudyPlanEntities.cs:83-84`),
  im Vertrag Pflicht, in der Spalte nullable (Altbestand, gelöschtes Fach → `SetNull`, Kommentar davor).
  `StudyPlansController` leitet es beim Anlegen ab und verlangt es (`StudyPlansController.cs:102-104`).
- **Der Plan kommt ohne Fach in die Berechnung:** `ComputeChildDayAsync` lädt die Pläne `AsNoTracking` ohne `Include`
  (`PositionProgressService.cs:409-412`); `ComputeDayAsync` (`:305`) bekommt den `StudyPlan` übergeben und hat dessen
  `Subject` damit nur, wenn der Aufrufer es lädt. Die Positionen laden dagegen Reihe und Reihen-Fach
  (`LoadPositionsAsync`, `:285-286`).
- **Nichts erzwingt, dass die Übung zum Fach des Plans passt:** in `PlanPositionsController` kommt „Subject" nicht vor
  (`grep` ohne Treffer). Nur der Wiederholungs-Pfad prüft es (`WeakWordReviewService.cs:87`,
  `ReviewSubjectMismatch`). Eine fachfremde Übung im Plan ist also erreichbar.
- **Gruppierung im Frontend:** `nachFach` gruppiert nach `pos.subjectName ?? ""` (`frontend/src/sohn/tagessicht.ts:25`),
  einziger Aufrufer `frontend/src/sohn/SohnHome.tsx:84`; die Überschrift ist `f.fach || t("home.noSubject")` (`:134`).
  Andere Leser von `PositionStatus.subjectName` im Sohn-Frontend: nicht gefunden.
- **Tests:** `tagessicht.test.ts` und `tagessichtWeg.test.tsx` setzen `subjectName` von Hand; ein Backend-Test, der den
  Fachnamen einer `PositionStatus` prüft, ist nicht gefunden (`KindTagessichtTests` kennt kein Fach;
  `FachnameFolgtDerFachIdTests` prüft Reihe, Profil und Lehrbuch, nicht den Status).

## Die echte Lücke

Die Behauptung der Idee stimmt am Code, mit einer Schärfung: die Gruppe hängt am **Lehrwerk**, nicht am Plan. Der Fehler
tritt auf, wenn (a) die Reihe eines Plans ein anderes Fach hat als der Plan oder (b) zwei Reihen desselben Fachs es
verschieden schreiben (Katalogname gegen Freitext) – oder wenn ein Plan nur Reihen ohne Fach enthält (Überschrift
„ohne Fach", obwohl der Plan ein Fach hat). Fall (a) ist nur möglich, weil die Anlage der Positionen es nicht prüft; wie
oft er vorkommt, ist nicht gemessen. Die Söhne haben je Fach einen Plan, darum fällt es heute kaum auf (P3 bleibt).

## Offene Punkte

1. **Wo wird umgestellt – am Server oder im Frontend?** Empfehlung: am Server. `SubjectName` bekommt die Bedeutung
   „Fach des Plans"; das Frontend liest nur dieses Feld (eine Fundstelle). Ein Gruppieren nach `planId` im Frontend
   würde die Fach-Reiter an den Plantitel binden, den der Vater frei benennt. Die Vertragsform bleibt, nur die Bedeutung
   ändert sich.
2. **Plan ohne Fach (Altbestand, `SetNull`)?** Empfehlung: Rückfall auf den heutigen Namen der Reihe, damit keine Gruppe
   ohne Namen entsteht, die es vorher mit Namen gab.
3. **Soll eine fachfremde Übung im Plan sichtbar markiert werden?** Empfehlung: nein, hier nicht. Ob die Anlage so etwas
   überhaupt zulassen soll, ist eine Frage an `PlanPositionsController` und eine eigene Story.
4. **Der Plantitel im Feld `SubjectName`?** Nein – das Fach ist `Subject.Name`, der Plantitel bleibt `PlanTitle`.

## Entscheidungen

Keine – eine Defekt-Story geht erst mit `gegrillt` in die Entscheidungen.

## Akzeptanzkriterien

Entwurf; je Kriterium die Änderung, die es rot macht.

1. Eine Position, deren Übung in einer Reihe mit anderem Fach liegt als der Plan, meldet in `student/children/{id}/today`
   das Fach des Plans. Rot, wenn `SubjectNameOf` weiter nur die Reihe liest.
2. Zwei Positionen desselben Plans, deren Reihen „Englisch" (Katalog) bzw. „English" (Freitext) heißen, melden denselben
   Fachnamen. Rot, wenn der Name aus der Reihe kommt.
3. Ein Plan ohne `SubjectId` meldet den Namen der Reihe wie bisher. Rot, wenn der Rückfall fehlt (dann leer).
4. Der Plan wird mit seinem Fach geladen, ohne zusätzliche Abfrage je Position. Rot, wenn `ComputeChildDayAsync` das
   `Subject` nicht lädt und das Fach still leer bleibt (AK 1 deckt es ab).
5. Zwei Pläne desselben Fachs stehen auf der Startseite weiter in einer Gruppe (`tagessicht.test.ts`, vorhanden). Rot,
   wenn die Gruppierung auf `planId` umgestellt wird.

## Verlauf

- **2026-11-17** — angelegt aus der Nachschau von Sprint 83 (🟢, `pugling-reviewer`), `entgangen_bei: [B-606]`.
- **2026-11-19** — ausformuliert (Vorbereitungslauf, Sonnet): am Code bestätigt; Lücke geschärft (Gruppe hängt am
  Lehrwerk; Plan wird ohne `Subject` geladen), fachfremde Positionen sind nicht ausgeschlossen. `unverifiziert` entfällt.
