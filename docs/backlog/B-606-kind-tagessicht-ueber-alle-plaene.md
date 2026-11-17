---
tags: [typ/story, status/abgenommen, bereich/lehrplan, rolle/student, rolle/supervisor]
aliases: [Kind-Tagessicht, student/today, Tagesbox über alle Pläne]
status: abgenommen
prio: P1
art: Wunsch
groesse: M
wo: beides
migration: nein
vertragsbruch: nein
quelle: B-539 (Teil B, Entscheidung 9)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: "2026-11-17: Truhe/Bonus/Dashboard/Serie/Metrik gegen ComputeChildDayAsync und IsCompleteDay nachgerechnet (DailyBoxService :35-38, ExtraTrainingService :259-268, ChildrenDashboardService :30-33, ChildStreakAsync :364-391), eine Serie für overview/progress/today/Truhe, Los merkt den Plan (SohnHome :280). Funde B-611, B-612, B-613 (🟡), B-614 (🟢)"
---

# B-606 · Eine Kind-Tagessicht über alle aktiven Pläne: Pflicht, Box und Serie

Teil B von [B-539](B-539-ein-aktiver-plan-je-fach.md), abgeteilt nach deren Entscheidung 9. Tagesbox, ihre Serie, die
Metrik-Serie und die Arcade-Tagesmission rechnen heute **je Plan**; Bonus-Sperre, Malus und Vater-Dashboard rechnen
schon über alle aktiven Pläne. Solange je Kind genau ein Plan aktiv ist, fällt das nicht auf – B-539 Teil A (ein
aktiver Plan je Fach) würde die Box aber zur Belohnung für das *leichteste* Fach machen. Diese Story zieht alle
Tagesrechnungen auf eine Quelle und ist mit einem aktiven Plan je Kind **verhaltensneutral**.

## User Story

Als *Sohn* möchte ich alle Pflichten des Tages an einer Stelle sehen, egal aus welchem Plan, und die Tagesbox erst
bekommen, wenn wirklich alles erledigt ist. Als *Vater* möchte ich, dass Box und Serie nicht an einem einzelnen
leichten Plan hängen.

## Ist-Stand am Code

Siehe [B-539 → Ist-Stand](B-539-ein-aktiver-plan-je-fach.md#ist-stand-am-code), Abschnitt „Was je Plan rechnet“ und
„Was schon über alle aktiven Pläne rechnet“ (gemessen 2026-11-15, Zeilen ggf. verschoben). Kurz: Tagesbox
`DailyBoxService` (ein Plan + dessen Serie), Box-Serie `PositionProgressService.StreakBoundedAsync` (ein Plan),
Metrik-Serie `MetricsService.CompleteDaysAsync` (Vereinigung über alle Pläne, auch inaktive), Arcade-Übersicht
`PlanOverviewController` (ein Plan), Arcade-Start `useChosenPlan`/`SohnHome` (ein Plan).

## Die echte Lücke

B-539 Lücke 3: die Tagesrechnungen haben keine gemeinsame Kind-Quelle.

## Entscheidungen

Übernommen aus B-539 (Stakeholder-Entscheidungen im Auftrag des Nutzers, 2026-11-15); nicht neu entschieden.

<!-- markdownlint-disable MD029 -->

6. **Eine Kind-Tagessicht über alle aktiven, laufenden Pläne ist die eine Quelle für Pflicht, Belohnungs-Sperre, Box
   und heutige Serie.** Für **vergangene** Tage (Serie, Missionen, Auszeichnungen) gilt: ein Tag ist vollständig, wenn
   mindestens ein Plan mit Pflicht an dem Tag sie erfüllt hat **und** kein heute aktiver Plan mit Pflicht an dem Tag
   offen ist. Inaktive Pläne zählen nur zu Gunsten des Kindes.
7. **Neuer Endpunkt `GET student/today`** (Kind-Tagessicht: Positionen aller aktiven, laufenden Pläne mit `PlanId`,
   Plan-Titel und Fachname, Pflichtstand, Serie, Box-Status). Die Arcade-Startseite zeigt ihn, nach Fach gruppiert;
   eine Position führt wie bisher in den Weg ihres Plans. `GET …/study-plans/{id}/overview` bleibt, seine Serie wird die
   Kind-Serie.

<!-- markdownlint-enable MD029 -->

Begründungen und Kosten: B-539, Entscheidungen 6 und 7.

## Akzeptanzkriterien

Aus B-539 AK 5 und 6. Je Fall die Änderung, die seinen Test rot macht.

1. (B-539 AK 5) Tagespflicht, Tagesbox, Bonus-Sperre, Malus und Serie rechnen über **alle** aktiven, laufenden Pläne
   des Kindes. Regressionstest mit zwei aktiven Plänen (heute nur per direktem DB-Setup herstellbar, weil die
   Ein-Plan-Sperre beim Anlegen greift): Pflicht des einen erfüllt, des anderen offen ⇒ `student/today` meldet Pflicht
   offen, **keine** Box verdient, Bonus gesperrt; beide erfüllt ⇒ genau eine Box. Serie: an Tag 1 beide erfüllt, an
   Tag 2 nur einer ⇒ Serie bricht; wird der offene Plan danach abgeschaltet, zählt Tag 2 (Entscheidung 6).
   *Rot mit der heutigen Box-Vergabe je Plan bzw. der Vereinigung in `MetricsService`.*
2. (B-539 AK 6) Die Arcade-Startseite zeigt die Pflichten aller aktiven Pläne nach Fach gruppiert (Fachname
   sichtbar); eine Position öffnet den Weg ihres Plans. *Rot, wenn die Startseite weiter `useChosenPlan` folgt*
   (Vitest am Gruppier-Baustein, E2E am Weg).
3. Verhaltensneutral mit einem Plan: die bestehenden Tests zu Tagesbox, Bonus-Sperre, Dashboard, Malus, Missionen und
   Arcade bleiben ohne inhaltliche Änderung grün. **Gewollt nicht neutral** (Entscheidung 6, nachgetragen nach dem
   Backend-Review): die Serie läuft jetzt über einen Kapitelwechsel hinweg weiter (die erfüllten Tage des abgeschalteten
   Plans zählen zu Gunsten des Kindes – real bei Friedrich, Plan 6 → 8 am 2026-11-16, höhere Truhen-Stufe möglich), und
   die Übersicht eines abgelaufenen Plans zeigt die Kind-Serie von heute statt der Serie bis zu seinem Ende.

## Schätzung

**M** – vier Backend-Stellen auf eine Quelle (`PositionProgressService`, `DailyBoxService`, `ExtraTrainingService`,
`MetricsService`), ein neuer Endpunkt mit Client und UI, Arcade-Startseite. Keine Migration, kein Vertragsbruch
(neuer Endpunkt, additive Felder). Vergleich: B-535 (M).

### Risiken

Aus B-539: **Box doppelt** – die Idempotenz-Vorprüfung (`DailyBoxService`) muss vor der Aggregation bleiben, sonst
kostet jede Antwort N Tagesrechnungen. **Serie über inaktive Pläne** – je Tag die Menge „heute aktive Pläne mit
Pflicht“; Kosten der Tagesrechnung je Tag beachten (B-460-Lehre).

#### Angriffsplan

B-539 Angriffsplan Schritte 1 und 2: `PositionProgressService.ComputeChildDayAsync(childId, day)` und eine Kind-Serie
nach Entscheidung 6; `DailyBoxService` auf Kind-Ebene; `ExtraTrainingService.GateAsync` und
`MetricsService.CompleteDaysAsync` auf dieselbe Quelle; `GET student/today` + additive `PositionStatus`-Felder
(`PlanId`, `PlanTitle`, `SubjectName`); Overview-Serie = Kind-Serie. Danach Arcade-Startseite auf `student/today`,
gruppiert nach Fach; `useChosenPlan` bleibt für den Weg.

##### Testweg

Neue Datei `Pugling.Api.Tests/KindTagessichtTests.cs` (AK 1); `DailyBoxTests`, `ExtraTrainingTests`,
`ChildrenDashboardTests`, `PflichtMalusTests` als Gegenprobe (AK 3); Vitest am Gruppier-Baustein (AK 2); E2E-Rollengang
für den Weg von der Startseite in die Position. Reviewer: `pugling-reviewer`, `frontend-reviewer`.

## Verlauf

- 2026-11-17 · `geschaetzt` angelegt im Nachtlauf 2026-11-17 durch Teilung von B-539 nach deren Entscheidung 9
  (Teil B zuerst). Entscheidungen, AKs und Schätzung übernommen, nicht neu entschieden; AK 3 macht die in
  Entscheidung 9 behauptete Verhaltensneutralität prüfbar.
- 2026-11-17 · `in-arbeit` – Sprint 83 im Nachtlauf 2026-11-17.
- 2026-11-17 · Gebaut (Subagent, API-First). Abweichung von Entscheidung 7, angenommen: die Route ist
  `GET student/children/{childId}/today` statt `student/today` – Muster der Nachbarn (`daily-box`, `extra-training`),
  `ChildOwnershipFilter`, der Vater liest für sein Kind. Senke: `PositionProgressService.ComputeChildDayAsync` (Tag),
  `IsCompleteDay`/`ChildStreakAsync`/`CompleteChildDaysAsync` (Serie nach Entscheidung 6); Truhe, Bonus-Sperre,
  Missions-Metrik, `overview`- und `overview/progress`-Serie und Dashboard lesen nur noch dort.
- 2026-11-17 · AK-Fälle → Test, Mutante:
  - AK 1, ein Plan offen ⇒ Pflicht offen, keine Truhe, Bonus zu → `KindTagessichtTests.cs:117`; M1 (Truhe bei *einem*
    erfüllten Ziel), M2 (Pflicht bei *einem* erfüllten Ziel), M5 (Truhe je Plan), M6 (Bonus je Plan) → rot.
  - AK 1, beide erfüllt ⇒ genau eine Truhe → derselbe Test (Claim-Zahl 1).
  - AK 1, Serie bricht am offenen Plan / zählt nach dessen Abschalten → `:158`; M3 (inaktiver Plan bricht weiter),
    M4 (Vereinigung wie früher in `MetricsService`) → rot; `overview/progress` = Kind-Serie: M9 (Serie je Plan) → rot.
  - AK 1, Missions-Metrik (Tage/Serie) → `:212`; M3, M4 → rot.
  - AK 1, abgeschalteter, noch laufender Plan hält den Tag nicht auf → `:192`; M10 (`p.Active` weg) → rot.
  - AK 2, Gruppierung nach Fach → `tagessicht.test.ts`; M7 (nach Plan) → rot. Startseite über zwei Pläne
    (Überschriften, ein START, „Los" merkt den Plan der Position, Plantitel-Zeile) → `tagessichtWeg.test.tsx:70`;
    M8 (Los merkt nichts), M11 (`naechsteId` fehlt), M12 (Fach- statt Plantitel) → rot.
  - AK 3, Neutralität mit einem Plan → volle Suite ohne inhaltliche Änderung an Bestandstests grün (zwei Aufrufe
    auf die neue Signatur umgestellt: `DailyBoxRangeTests` auf frisches Kind + interne Test-Naht `AwardIfUnlockedAsync`,
    `BelohnungenTagespflichtTests:114` auf `StreakBoundedAsync(childId…)`, Erwartungswerte unverändert).
- 2026-11-17 · Mutanten-Probe nach der letzten Änderung (`mutant-probe.py`, Debug, gemessen an `4586e493` + Arbeitsbaum,
  06:52): M1–M12 erwartet rot, gemessen rot (12/12), jeweils am benannten Test; Grundlinie danach grün.
- 2026-11-17 · Reviews: `frontend-reviewer` 1 🔴 + 3 🟡 + 4 🟢, `pugling-reviewer` 3 🟡 + 3 🟢. **Gezählt (4 von 5):**
  Pfad-E2E sucht den Plantitel (🔴, Plantitel-Zeile zurück), fehlender Verdrahtungstest der Startseite (🟡, Render-Test),
  `overview/progress` zeigte die Plan-Serie (🟡, Kind-Serie), zwei öffentliche Umgehungen der Senke (🟡, Truhen-Naht
  `internal`, Plan-Überladung der Serie gelöscht). **Nicht gezählt:** Flammen-Serie springt (trifft nicht zu – `overview`
  liefert seit B-606 ebenfalls die Kind-Serie); Planwahl auf der Fortschrittsseite (heute niemand betroffen, nur
  Kommentare + Vermerk in B-539); Neutralität über die Plangeschichte (gewollt nach Entscheidung 6, AK 3 nachgetragen);
  🟢 ohne Zählung behoben: `h1`/`aria-labelledby`, zwei tote i18n-Schlüssel, Testfall „inaktiver laufender Plan";
  🟢 zurückgestellt: doppelte Tagesrechnung auf dem Schreibweg → B-609, Zeitachse → B-558, „Los" per Mittelklick.
- 2026-11-17 · Verifikation: `dotnet build` 0/0, `dotnet test -c Release` 1776/1776, `dotnet format --verify-no-changes`
  still, `tsc -b` sauber, Vitest 1150/1150. Ein roter Lauf des Test-Tors (`SecurityHardeningTests.Login_mit_unbekannter_Id…`,
  Zeitvergleich) fiel unter doppelter Last (zwei volle Suiten parallel, 8 min); einzeln 3/3 grün.
- 2026-11-17 · abgenommen in Sprint 83 (14fd6ae6): Rollengang `frontend/e2e/sprint-83-rollengang.spec.ts` mit `pfad.spec.ts`
  und `truhe.spec.ts` **3 passed** gegen frisch gestarteten Server (Ausweichports 5201/5174); volle Playwright-Suite am
  Endstand vor der Spec-Korrektur **166 passed, 1 failed** (die neue Spec selbst: sie suchte den Übungstitel auf der
  Übungsseite, die ihn nicht zeigt – Fund 5 von 5, Zusicherung auf die Karte umgestellt); `dotnet test` 1776, Vitest 1150.
  Zwei Pläne sind im Browser erst mit B-539 Teil A sichtbar – bis dahin tragen `KindTagessichtTests` und
  `tagessichtWeg.test.tsx` die Zwei-Pläne-Fälle. Protokoll `docs/pm-sitzung-2026-11-17-nachtlauf.md`.
