---
tags: [typ/protokoll, bereich/backlog]
---

# PM-Sitzung: Nachtlauf 2026-11-17

## Auftrag und Ausgangswert

„Starte einen Nachtlauf" – unbeaufsichtigter Backlog-Lauf nach `pm-loop`, [docs/nachtlauf.md](nachtlauf.md)
(Freigaben 1–10) und [docs/backlog/README.md](backlog/README.md). Arbeit direkt auf `main` (Stand `4586e493`, 27
Commits ungepusht); der Heimnetz-Server läuft auf `:5200` aus seinem Publish-Ordner und wird nicht angefasst –
Messungen nur auf Ausweichports gegen Wegwerf-DBs. Keine weitere Sitzung schreibt im Checkout (Worktree
`backlog-lauf-1116` ist nach `main` gemergt und ruht).

**Ausgangswert:** 29 offen (Index vom 2026-11-16).

## Auswahl

Baubar nach Freigabe 8/9 bzw. autonom grillbar nach Freigabe 1:

| Id | Art | Stufe | Prio | Größe | Urteil |
| --- | --- | --- | --- | --- | --- |
| B-539 | Wunsch | `geschaetzt` | P1 | über L | teilen (Entscheidung 9), Teil B zuerst |
| B-580 | Wunsch | `geschaetzt` | P1 | L | braucht B-539 Teil A (Fach und Unit am Plan) |
| B-223, B-377 | Wunsch | `geschaetzt` | P3 | M | baubar, Thema fremd zu Sprint 83 |
| B-558, B-602, B-603, B-605 | Defekt | `idee` | P3 | — | autonom grillbar |
| B-248 | Wunsch | `idee` | P2 | — | Freigabe des Nutzers (NP-22), hängt an B-220/B-39 |
| übrige `Wunsch`/`Frage` auf `idee`/`ausformuliert` | | | | | gesperrt (Freigabe 1/8) |

Roter Faden der Nacht: **„ein aktiver Plan je Fach"** (B-539 → B-580), weil beide P1 sind und der Vater sie im
Dialog entschieden hat. Danach, wenn der Lauf noch steht, die Test-Flackerer (B-602, B-603, B-605).

## Refinement

- **B-539 geteilt** nach ihrer Entscheidung 9: Teil B „Kind-Tagessicht" ist **B-606** (`geschaetzt`, M, keine
  Migration, kein Vertragsbruch); B-539 behält AK 1–4 (L). Abweichend von README „Teilen" bleibt B-539 bestehen,
  weil Entscheidung 9 das so festlegt und B-580 auf die Id verweist.

## Sprint 83 — Ziel & Umfang

**Sprint-Ziel (Sicht Sohn):** „Auf der Startseite sehe ich alle Pflichten des Tages aus allen meinen Plänen nach
Fach, und die Tagesbox gibt es erst, wenn alle erledigt sind – mit einem Plan ändert sich für mich nichts."

**Umfang:** B-606.

**Entwickler-Brief:** Quelle der Wahrheit ist eine Kind-Tagesrechnung in `PositionProgressService` über alle
aktiven, laufenden Pläne; Box, Bonus-Sperre, Metrik-Serie und Overview-Serie lesen nur sie (Regel an der Senke).
Guards: Idempotenz-Vorprüfung der Box vor der Aggregation; Student-Endpunkt nur `[Authorize]` mit
`IsStudent`/`IsSupervisor` inline wie die Nachbarn; kein Lösungsfeld. Keine Migration. Testweg: neue
`KindTagessichtTests.cs`, Bestandstests als Gegenprobe, Vitest am Gruppier-Baustein, E2E-Rollengang.

## Iteration 83 — umgesetzt

- **Backend (API-First):** `PositionProgressService.ComputeChildDayAsync` (Kind-Tag über alle aktiven, laufenden Pläne),
  `IsCompleteDay`/`ChildStreakAsync`/`CompleteChildDaysAsync` (Serie nach Entscheidung 6); Truhe
  (`DailyBoxService.EvaluateAndAwardAsync(childId…)`), Bonus-Sperre, Missions-Metrik, `overview`- und
  `overview/progress`-Serie und Dashboard lesen nur dort. Neuer Endpunkt `GET student/children/{childId}/today`
  (Abweichung von `student/today`: Muster der Nachbarn, `ChildOwnershipFilter`). `PositionStatus` additiv um `PlanId`,
  `PlanTitle`, `SubjectName`. Keine Migration, kein Vertragsbruch.
- **Frontend:** Arcade-Startseite auf die Kind-Tagessicht, je Fach ein Abschnitt mit Überschrift, ein START über alle,
  „Los" merkt den Plan der Position; Plantitel-Zeile unter „Tagesmission" (jetzt `h1`).
- **Verifikation:** `dotnet test` 1776/1776, Vitest 1150/1150, `tsc -b` und `dotnet format` sauber; Mutanten M1–M12
  nach der letzten Änderung 12/12 rot am benannten Test (Tabelle in B-606). Reviews: 5 gezählte Funde (Grenze: mehr als
  5), alle behoben; Einzelheiten und die nicht gezählten im `## Verlauf` von B-606.
- **Commits:** `0901b4c8` (Backlog), `14fd6ae6` (B-606).

## Runde 83 — Abnahme

- **Sohn — abgenommen.** „Auf der Startseite stehen meine Sachen jetzt unter dem Fach, ‚Los' bringt mich in die richtige
  Übung, die Truhe kommt wie vorher erst nach der Pflicht." Belegt durch `sprint-83-rollengang.spec.ts`, `pfad.spec.ts`,
  `truhe.spec.ts` (3 passed, frisch gestarteter Server auf 5201/5174). Zwei Fächer nebeneinander sieht er erst mit
  B-539 Teil A; bis dahin tragen `KindTagessichtTests` und `tagessichtWeg.test.tsx` diesen Fall.
- **Vater — Regressionszeuge:** volle Playwright-Suite 166 passed (die eine rote war die neue Sohn-Spec, siehe B-606);
  Dashboard rechnet über die Kind-Sicht mit unveränderten Bestandstests; die Tagessicht seines Kindes liest er über
  denselben Endpunkt (in der Rollengang-Spec geprüft).
- **Creator — Regressionszeuge:** kein Pfad des Creators im Diff; Suite grün.

Sprint-Ziel gilt: alle Pflichten aus allen Plänen nach Fach, Truhe erst nach allen; mit einem Plan unverändert bis auf die
Fach-Überschrift und die über einen Kapitelwechsel weiterlaufende Serie (gewollt, Entscheidung 6, AK 3 nachgetragen).

## Retrospektive 83

**Nachschau:** Sprint 82 (B-597, B-599, B-598) gelesen vom `pugling-reviewer` (nur lesend). B-597: Drehung an jeder
Karten- und Wertungsstelle nachgerechnet, Obergrenze auf jedem Stufen-Weg, Rot-Probe gegen Mutante gerechnet – Funde
**B-607** (🟡, Hinweis verschwindet still auf Schreibstufen bei „Fremdwort → Deutsch") und **B-608** (🟢, Bonus-Kopf
nennt zu hohen Satz), beide `entgangen_bei: [B-597]`. B-599: Kästchen-Rückfall nur in `CardFacets`, Ablenker-Index gegen
beide Quellen, Bonus-Sätze gegen Entscheidung 7 – kein Fund. B-598: Lernkarte bucht nichts, Wiederholung höchstens
einmal, kein Weg in Test/Info/Bonus – kein Fund. `nachgeschaut` an allen dreien gesetzt. Index: **Nachgeschaut 488 von
510**.

**Was die eigenen Tore durchgelassen haben:** (1) Der Bau-Auftrag schloss E2E aus („das mache ich"), und die Startseite
änderte einen Text, auf den `pfad.spec.ts:61` wartet – gefunden hat es der `frontend-reviewer` durch Lesen, nicht ein
Lauf. (2) Meine eigene Rollengang-Spec sicherte einen Text zu, den die Zielseite nie zeigt – gefunden erst vom vollen
E2E-Lauf. Beide sind dieselbe Klasse: eine Zusicherung über sichtbaren Text, die gegen den Code nicht abgeglichen war.

**Vorschlag (CY), nicht gelandet (Freigabe 3):** ein kleines Skript `e2e-text-touch.py`, das zu einem Frontend-Diff die
aus `.tsx`-Dateien **entfernten** sichtbaren Strings bzw. `t()`-Schlüssel sammelt und die E2E-Specs nennt, die sie per
`getByText`/`name:` erwarten – im Bau-Brief als Pflichtschritt vor der Übergabe. Es hätte Fund (1) vor dem Review
gezeigt; Fund (2) fängt es nicht (neue Spec, nichts entfernt) – dafür bleibt der volle E2E-Lauf vor der Abnahme.

**Bewusst ungedeckt:** Zwei aktive Pläne im echten Browser (erst mit B-539 Teil A herstellbar).

**Halt-Prüfung:** kein neues Tor gelandet; 5 gezählte Funde (Endlosschleife erst bei mehr als 5) → der Lauf geht weiter.

## Sprint 84 — Ziel & Umfang

**Sprint-Ziel (Sicht Vater):** „Ich lege für jedes Fach meines Kindes einen eigenen Plan an, ohne dass ein Plan den
anderen abschaltet; jeder Plan trägt sein Fach (und wahlweise die Unit), und in der Zeitachse sehe ich auch die Wochen
vor einem Kapitelwechsel."

**Umfang:** B-539 (Teil A, AK 1–4, L), B-558 (S).

**Entwickler-Brief:** Reihenfolge B-539 → B-558 (sequenziell; nur B-539 bringt eine Migration). Quelle der Wahrheit für
die Aktivierung ist `StudyPlansController` (Abschaltung nur in der Gruppe *Kind × Fach*, Pläne ohne Fach eine Gruppe);
Fach-Pflicht und Unit-Konsistenz an genau der Stelle, durch die Anlegen und PATCH laufen. Migration nur angehängt,
Rückfüllung als SQL, Probe gegen eine **Kopie** der echten DB. Vertragsbruch: Client, Agent (`PlanPlanner`), Wizard,
Test-Welle und E2E-Specs ziehen nach. Box/Serie/Pflicht über mehrere Pläne trägt seit Sprint 83 B-606. B-558 nutzt
dieselbe Fairness-Regel (inaktive Pläne nur zu Gunsten des Kindes) in `ChildrenDashboardService.TimelineAsync`.
Lehre aus Retro 83: der Bau-Auftrag läuft die betroffenen E2E-Specs selbst (Ausweichports), nicht erst der Koordinator.

## Iteration 84 — umgesetzt

- **B-539 Teil A (Backend):** Fach Pflicht im Vertrag (aus der Unit abgeleitet), `StudyPlan.SeriesUnitId`
  (Migration `20261117061523_StudyPlanSeriesUnit`, Rückfüllung des Mehrheitsfachs nur für Pläne ohne Fach, Gleichstand
  bleibt leer), Abschaltung nur je Kind × Fach, `validation_error` bei fehlendem/widersprüchlichem Fach, Abschalten
  bleibt immer frei. Probe auf einer Kopie der echten DB: Pläne und abhängige Zeilen unverändert.
- **B-539 Teil A (Frontend):** Plan-Formular, Bearbeiten und Wizard mit Fach (Pflicht) und Unit (optional),
  Fach-Spalte und „mehrfach aktiv" je Fach in der Planliste, „Aktivieren" ohne Fach gesperrt, Planwahl auf der
  Arcade-Fortschrittsseite; Test- und E2E-Welle (35 Specs) auf das Pflichtfach.
- **B-558:** Zeitachse zählt abgeschaltete Pläne nur zu Gunsten des Kindes (`TimelineDutyDone` neben `IsCompleteDay`).
- **B-610:** `assign` bricht bei mehreren spielbaren Plänen ab und verlangt `--plan-id` (Review-Fund, im Sprint behoben).
- **Verifikation:** `dotnet test` 1795/1795, Vitest 1160/1160 (ein voller Lauf von vier mit 1 nicht zuzuordnendem
  Ausfall, Muster B-603), Playwright **168 passed**. Mutanten nach der letzten Änderung: 14 von 15 rot am benannten
  Test; A4 grün und **äquivalent** (dieselbe Antwort über die Pflichtzeile, nur anderer `detail`) – kein Beleg, Regel hält.
- **Reviews:** 5 gezählte Funde (4 aus B-539, 1 aus B-558; Grenze: mehr als 5), alle behoben; einzeln im Verlauf von
  B-539 und B-558.
- **Commit:** `30711266`.

## Runde 84 — Abnahme

- **Vater — abgenommen.** „Ich lege für Englisch und Französisch je einen Plan an, beide bleiben an; erst ein zweiter
  Englisch-Plan schaltet den ersten ab. Ohne Fach kann ich nicht speichern, und ‚Aktivieren' ist bei einem alten Plan
  ohne Fach gesperrt, statt mir einen englischen Fehler zu zeigen. In der Zeitachse bleiben die Wochen vor dem
  Kapitelwechsel stehen." Belegt durch `zwei-faecher.spec.ts` (UI-Weg) und `vater-zeitachse.spec.ts`, beide in der vollen
  Suite (168 passed).
- **Sohn — abgenommen (Regressionsweg und neuer Fall).** „Ich sehe Englisch und Französisch getrennt, und die Truhe gibt
  es erst, wenn beides geschafft ist." Belegt im selben Lauf von `zwei-faecher.spec.ts`; Fortschrittsseite mit Planwahl
  durch `SohnProgress.test.tsx`.
- **Creator — Regressionszeuge:** `assign` fragt bei mehreren Plänen nach (`AssignAgentTests`), `PlanPlanner` schickt
  Fach und Unit; Creator-Specs grün in der vollen Suite.

Sprint-Ziel gilt.

## Retrospektive 84

**Nachschau:** Sprint 83 (B-606) gelesen vom `pugling-reviewer` gegen den Stand nach B-539 – Truhe, Bonus-Sperre,
Dashboard, Serie und Missions-Metrik gegen `ComputeChildDayAsync`/`IsCompleteDay` nachgerechnet, eine Serie für alle
Anzeigen bestätigt, „Los" merkt den Plan. **Drei Defekte entgangen**, alle 🟡 und alle erst real, seit B-539 mehrere
aktive Pläne zulässt: **B-611** (ein Wochenziel-Plan sperrt Truhe und Bonus über alle Pläne), **B-612** (Startseite zählt
Belohnungsziele anders als der Server), **B-613** (rückdatierter/wieder eingeschalteter Plan bricht die Serie
nachträglich); dazu 🟢 **B-614** (Gruppierung nach Übungs- statt Planfach). `nachgeschaut` an B-606 gesetzt.

**Was die eigenen Tore durchgelassen haben:** In Sprint 83 waren zwei aktive Pläne nur per DB-Setup herstellbar, und die
Tests von B-606 bauten genau den einen erdachten Fall (zwei Pläne, beide mit Tageszielen). Die Regel „über alle Pläne"
wurde nie gegen **verschiedenartige** Pläne geprüft – gemischter Takt, rückdatierter Start, ein Plan ohne Pflicht. In
Sprint 84 fanden die Reviews vier gezählte Funde in B-539 und einen in B-558; das Tor „Bau läuft betroffene E2E selbst"
aus Retro 83 hat getragen (35 Specs vom Bau nachgezogen, die volle Suite war beim ersten Koordinator-Lauf grün).

**Vorschlag (CZ), nicht gelandet (Freigabe 3):** ein Matrix-Test `KindTagessichtMatrixTests`, der für **jede**
Kombination zweier Pläne aus {nur Tagesziele, nur Wochenziele, gemischt, ohne Pflicht} × {aktiv, abgeschaltet} prüft,
dass `RewardsUnlocked`, `DutyDone` und die Serie der Kind-Sicht dieselbe Regel ergeben wie die eines einzigen Plans mit
der Vereinigung der Positionen – oder, wo Entscheidung 6 bewusst abweicht, die benannte Abweichung. Er hätte B-611
gefunden; B-613 nur mit einer Start-in-der-Vergangenheit-Dimension.

**Bewusst ungedeckt:** B-613 Teil 2 (Wiedereinschalten) braucht eine Entscheidung (kein Aktivierungsverlauf).

**Halt-Prüfung:** kein neues Tor gelandet; 5 gezählte Funde in Sprint 84 (Grenze: mehr als 5). Die drei Nachschau-Funde
liegen im Increment von Sprint 83, nicht in dem von Sprint 84 → der Lauf geht weiter. Weil B-539 sie live gemacht hat,
nimmt Sprint 85 die autonom grillbaren davon (B-611, B-612, B-614) auf.
