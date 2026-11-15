---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/klassenarbeit, bereich/training, rolle/supervisor]
aliases: [Assistent Klassenarbeit-Termin, Stufenleiter aus dem Assistenten]
status: abgenommen
wartet_auf: ""
prio: P2
art: Wunsch
groesse: M
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-08-rollengang.md#feedback-vater
grund: ""
nachgeschaut: 2026-11-15
ersetzt_durch: []
---

# B-423 · Der Assistent „Klassenarbeit vorbereiten" fragt den Termin, legt die Klassenarbeit an und baut die Stufenleiter

Im Rollengang vom 2026-11-08 wählte der Vater im Assistenten das Ziel „Klassenarbeit vorbereiten". Der
Assistent fragte **nicht nach dem Termin**, setzte pauschal 14 Tage, schrieb ab Tag 1 die Tipp-Stufe in jede
Position und legte **keine** Klassenarbeit an – obwohl es unter „Betreuen" eine Klassenarbeiten-Seite mit
Termin, Übungsbündel und Note gibt. Zwei Flächen, die dasselbe meinen, wissen nichts voneinander; der Plan
endet unabhängig vom Arbeitstermin, und [B-248](B-248-klassenarbeit-countdown-treibt-den-plan.md) (Countdown
und Zusatzpflicht beim Kind) hätte keine Klassenarbeit, auf die es sich beziehen könnte.

## User Story

Als *Vater* möchte ich im Assistenten den **Termin der Klassenarbeit** eingeben, damit der Plan bis zum Vortag
läuft, die Klassenarbeit mit ihren Übungen unter „Klassenarbeiten" steht und das Kind in den ersten Tagen
schonend (Selbsteinschätzung) und danach streng (Tippen) abgefragt wird – ohne dass ich drei Seiten pflege.

## Ist-Stand am Code

- **Assistent:** `VaterWizard.tsx:48-51` – `GOALS.Klassenarbeit = { duration: 14, stage: 4, typed: true }`;
  `:99`/`:242` Dauer als freie Zahl, `:619` Feld „Dauer (Tage)"; `:368` schreibt `stage: defaultStage` in jede
  Position. Kein `stageSchedule`, kein Termin, kein Aufruf der Klassenarbeiten-API (keine Treffer für
  `class-tests`/`scheduledDate` in der Datei).
- **Stufenleiter existiert serverseitig:** `CreatePositionDto.StageSchedule` (`StudyPlanDtos.cs:132-134`, Typ `StageStep` in `StudyPlanBaseTypes.cs:18`),
  angewendet in `PositionPlayService.StageForDay` (`Services/Shared/PositionPlayService.cs:73-81`): je `StageStep(DayNumber,
  Stage)` gilt ab dem Plantag `DayNumber`. Im Frontend ungenutzt (nur im generierten `contract.ts`);
  [B-16](B-16-positions-formular-umfang.md) hat den Listen-Editor im Positions-Formular bewusst zurückgestellt
  (Entscheidung 2).
- **Klassenarbeit-API:** `POST supervisor/class-tests` mit `CreateClassTestDto(ChildId, Title, Topic, SubjectId,
  ScheduledDate, …)` (`ClassTestDtos.cs:20`), Übungen per `POST class-tests/{id}/exercises`
  (`KlassenarbeitenController.cs:187`). Die Seite `/vater/class-tests` zeigt Termin, Status, Übungen, Note.
- **Anzeigestufe am Tag ist nicht testbar** (`PositionTestsController.cs:154`, `StageNotTestable`) – eine
  Leiter darf darum **keinen** ganzen Tag auf `ShowBoth` legen, sonst reißt das Tagesziel ohne Test. Die
  Erstbegegnung je Karte ist [B-419](B-419-erstbegegnung-neue-karte-erst-zeigen.md).
- `wizardFinish.ts:110-113` streift `stage` für Nicht-Vokabel-Typen – dasselbe muss für die Leiter gelten.
- Gemessen: Plan 2 „Französisch – Klassenarbeit vorbereiten", 2026-11-08 – 2026-11-21, beide Positionen
  Stufe 4 ab Tag 1; `/vater/class-tests` zeigt nur die zwei geseedeten Arbeiten.

## Die echte Lücke

Das Ziel „Klassenarbeit" ist im Assistenten nur ein Satz von Standardwerten. Es müsste die eine Information
abfragen, die alles andere bestimmt – den Termin – und daraus Planende, Klassenarbeit und Leiter ableiten.
Alle drei Server-Fähigkeiten sind da; es fehlt der Zusammenhang im Frontend.

## Offene Punkte

1. ~~Termin Pflicht oder optional?~~ → Entscheidung 1.
2. ~~Leiter mit welchen Stufen?~~ → Entscheidung 2.
3. ~~Klassenarbeit automatisch anlegen oder nur anbieten?~~ → Entscheidung 3.
4. Zusatzpflicht/Countdown beim Kind → bleibt bei B-248 (dort vom Nutzer entschieden).
5. ~~**Offen (Lauf AF, 2026-11-09): Leiter und „Nur getippte Tests zählen" schließen sich aus.**~~ →
   Entscheidung 5 (Variante a). Der Wortlaut bleibt zur Nachvollziehbarkeit stehen: Das Ziel
   „Klassenarbeit" setzt `requireTypedTest: true` (`VaterWizard.tsx:49`, `typed: true`). Ein Test auf der
   Selbsteinschätzungs-Stufe ist ungewertet (`PositionTestsController.cs:178`, `Graded = typed`), und das Tagesziel
   zählt bei `RequireTypedTest` nur gewertete Versuche (`PositionProgressService.cs:150`,
   `(!pos.RequireTypedTest || t.Graded)`). Mit Entscheidung 2 wäre das Tagesziel im ganzen ersten Drittel
   **unerreichbar**, und der Malus (Intensität „Normal": −5 🪙 je Tag) liefe gegen das Kind – genau das, was B-419
   abschaffen sollte. Zu entscheiden: (a) bei einer Leiter `requireTypedTest` aus (die Strenge kommt dann allein
   aus der Leiter), (b) Leiter ohne Selbsteinschätzung (z. B. Multiple-Choice-Stufe statt 2, falls getippt-gewertet),
   (c) im ersten Drittel kein Malus, oder (d) die Leiter fallen lassen. Entscheidungen 1, 3, 4 sind davon
   unberührt; der Lauf hat sie trotzdem nicht allein gebaut, weil die Story als Ganzes die Leiter verspricht.

## Entscheidungen

Getroffen vom PM in der Fable-Sitzung vom 2026-11-08 unter dem Auftrag des Nutzers, das Backlog
nachtlauffähig zu machen ([Protokoll](../pm-sitzung-2026-11-08-rollengang.md)). Vetorecht beim Nutzer.

1. **Beim Ziel „Klassenarbeit" ist der Termin Pflicht; Start und Dauer folgen daraus.** Start = heute,
   Ende = Termin − 1 Tag; das Feld „Dauer (Tage)" wird im Feinschliff für dieses Ziel zur Anzeige (berechnet).
   Liegt der Termin in weniger als zwei Tagen, sperrt der Weiter-Knopf mit Grund (Hausregel „Pflichtfeld →
   gesperrt, Label trägt den Grund"). Kosten: ein Datumsfeld in Schritt 2 und eine Ableitung in `wizardFinish`.
2. **Die Leiter hat zwei Stufen: erstes Drittel Selbsteinschätzung (2), danach die gewählte Tipp-Stufe.**
   `StageSchedule = [{DayNumber: 1, Stage: 2}, {DayNumber: ceil(n/3)+1, Stage: defaultStage}]`, nur für
   Vokabel-Positionen (wie `stage` in `wizardFinish.ts:109`). Im Feinschliff steht sie als ein Satz mit einem
   änderbaren Tag („Selbsteinschätzung bis Tag 4, danach Freitext"). Begründung: kein Tag auf `ShowBoth`
   (nicht testbar); die Kartenerstbegegnung kommt aus B-419. Kosten: zwei Felder in der Position, ein Satz im
   Feinschliff, keine Listen-Editor-UI (B-16 bleibt zurückgestellt).
3. **Die Klassenarbeit wird angelegt, nicht nur angeboten.** Nach dem Plan ruft der Assistent
   `POST class-tests` (Titel „<Fach>-Arbeit", `Topic` = Thema/Kapitel aus Schritt 2, `SubjectId`,
   `ScheduledDate` = Termin) und hängt jede gewählte Übung per `POST class-tests/{id}/exercises` an. Der
   Überblick (Schritt 5) sagt das vorher („legt die Klassenarbeit am 22.11. an"). Scheitert einer dieser
   Aufrufe, steht der Plan trotzdem, und die Meldung nennt die Klassenarbeit als fehlend mit Link auf die
   Seite. Begründung: ohne die Entität ist B-248 wirkungslos; der Vater pflegt sonst dasselbe zweimal.
   Kosten: n+1 Aufrufe nach dem Plan, ein Teilfehler-Fall im Ende-Banner.
4. **Kein Backend-Umbau.** Alles läuft über vorhandene Endpunkte; `wo: frontend`. Kosten: die Leiter ist im
   Positions-Formular danach nur lesbar sichtbar (Zeile „Stufe: 2 bis Tag 4, dann 4") – bearbeiten bleibt B-16.
5. **Trägt eine Position die Leiter, schickt der Assistent für sie `requireTypedTest: false`; die Strenge kommt
   allein aus der Leiter** (Variante a aus Offenem Punkt 5). Positionen ohne Leiter (Nicht-Vokabel-Typen)
   behalten den Wert des Häkchens „Nur getippte Tests zählen“ wie heute. Der Feinschliff sagt das am Häkchen in
   einem Satz („Vokabeln: Selbsteinschätzung zählt bis Tag 4, danach wird getippt“). Stakeholder-Entscheidung im
   Auftrag des Nutzers (2026-11-14). Begründung: Ab Tag `ceil(n/3)+1` legt der Server die Tipp-Stufe fest
   (`PositionPlayService.StageForDay`, `PositionPlayService.cs:73-81`; nur der Supervisor darf die Stufe wählen,
   `PositionTestsController.cs:149`), der Test ist dann ohnehin gewertet (`Graded = typed`, `:178`) – die
   Strenge der zweiten Phase hängt also nicht am Häkchen. In der ersten Phase macht erst `false` das Tagesziel
   erreichbar (`PositionProgressService.cs:150`); sonst liefe der Malus gegen ein Kind, das nichts falsch gemacht
   hat – die Fairness, die B-419 herstellen sollte. Verworfen: (b) braucht eine getippt-gewertete
   Auswahl-Stufe, die es nicht gibt (Backend-Umbau, widerspricht Entscheidung 4); (c) ein malusfreies erstes
   Drittel ist eine Server-Regel (ebenso); (d) gäbe das Versprechen der Story auf. Kosten: Im ersten Drittel
   zählt ein selbst eingeschätzter Test für das Tagesziel, und Übungsrunden auf der Selbsteinschätzungs-Stufe
   zahlen Münzen (`PositionPracticeController.cs:461`, `typed || !RequireTypedTest`) – begrenzt auf fällige
   Karten, einmal je Tag; der Vater sieht die Leiter am Plan (AK 7).
6. **Nachtrag zu Entscheidung 3:** Die Klassenarbeit entsteht mit **einem** Aufruf – `CreateClassTestDto` nimmt
   `ExerciseIds` direkt mit (`Pugling.Contracts/Supervisor/ClassTestDtos.cs:19-20`, Nebenbefund Lauf AF). Kein
   `POST class-tests/{id}/exercises` je Übung. Stakeholder-Entscheidung im Auftrag des Nutzers. Kosten: keine;
   der Teilfehler-Fall (AK 5) schrumpft auf einen Aufruf.
7. **Eine Leiter entsteht nur, wenn die gewählte Test-Stufe über der Selbsteinschätzung liegt (Stufe > 2); bei Stufe 1
   oder 2 schreibt der Assistent keine Leiter, die Positionen gehen wie bei den anderen Zielen an den Server.**
   Stakeholder-Entscheidung im Auftrag des Nutzers (2026-11-14, Review Sprint 76). Begründung: bei Stufe 2 wäre die Leiter
   ein Schritt von 2 auf 2 – sie schaltete nur `requireTypedTest` ab, ohne je strenger zu werden; bei Stufe 1 („Nur
   anzeigen“) läge die zweite Phase auf einer nicht testbaren Stufe, und das Tagesziel risse ohne Test. Verworfen: die
   Stufenauswahl beim Ziel „Klassenarbeit“ auf getippte Stufen zu kürzen – das wäre eine zweite, versteckte Stufenliste
   neben `VOCAB_FORMS`. Kosten: wer bei der Klassenarbeit Stufe 2 wählt, bekommt keine Leiter; der Satz im Feinschliff
   fehlt dann, und der Überblick zeigt keine Zeile „Stufenleiter“.

## Akzeptanzkriterien

1. Ziel „Klassenarbeit" → Schritt 2 zeigt ein Pflichtfeld „Termin"; ohne Termin ist „Weiter" gesperrt und das
   Label nennt den Grund; Termin < heute + 2 Tage → gesperrt mit Grund.
2. Der Plan endet am Vortag des Termins; Schritt 5 zeigt Zeitraum und „legt die Klassenarbeit am <Termin> an".
3. Jede Vokabel-Position trägt `stageSchedule` `[1→2, ceil(n/3)+1→Tipp-Stufe]`; Nicht-Vokabel-Positionen
   tragen keine Leiter (wie heute keine Stufe). *Rot, wenn* `stageScheduleFor` den Schwellentag falsch rundet
   (Vitest mit n = 7, 9, 14: Schwellentag 4, 4, 6).
4. Nach dem Anlegen steht unter `/vater/class-tests` eine Arbeit mit Termin, Fach und allen gewählten Übungen –
   angelegt mit **einem** `POST class-tests` samt `exerciseIds` (Entscheidung 6).
5. Scheitert `POST class-tests`, ist der Plan angelegt und das Ende-Banner nennt die fehlende Klassenarbeit mit
   Link.
6. Die Ziele „Aufholen"/„Regelmäßig" verhalten sich unverändert (Dauer frei, keine Leiter, keine Arbeit).
7. Die Plan-Detailseite zeigt je Position die Leiter lesbar („Selbsteinschätzung bis Tag 4, dann Freitext").
8. Eine Vokabel-Position mit Leiter wird mit `requireTypedTest: false` angelegt, auch wenn das Häkchen „Nur
   getippte Tests zählen“ gesetzt ist; eine Nicht-Vokabel-Position desselben Plans trägt den Wert des Häkchens
   (Entscheidung 5). *Rot, wenn* `wizardFinish` das Häkchen ungeprüft in jede Position schreibt (Vitest am
   erzeugten `CreatePositionDto`).
9. Rollengang Sohn in der E2E: am Plantag 1 erfüllt ein bestandener Test auf der Selbsteinschätzungs-Stufe das
   Tagesziel der Vokabel-Position. *Rot, wenn* AK 8 fehlt – dann zählt der ungewertete Versuch nicht
   (`PositionProgressService.cs:150`).

## Schätzung

**Größe: M** — ein Feld, zwei Ableitungen (reine Funktionen in `wizardFinish.ts`, testbar), ein weiterer
Aufruf, Anzeige in Feinschliff/Überblick/Plan-Detail; Vitest + E2E. Entscheidung 5 kostet eine Bedingung im
Positions-DTO und einen Satz im Feinschliff, Entscheidung 6 spart n Aufrufe – die Größe bleibt M (Anker B-03).

- **`migration: nein`** – kein Schema.
- **`vertragsbruch: nein`** – nur vorhandene Felder (`stageSchedule`, `requireTypedTest`, `CreateClassTestDto`).

**Risiken:** (1) `wizardFinish` ist heute eine reine Funktion mit Rundlauf-Tests – die Leiter und den
`requireTypedTest`-Schnitt dort ableiten, nicht im Komponentenzustand. (2) Zeitzone: Termin und Planende sind
`DateOnly` (UTC-Regel aus `CLAUDE.md`) – keine `new Date()`-Umrechnung mit Uhrzeit. (3) Der Aufruf nach dem Plan
ist ein zweiter Schreibpfad – `useAction`-Sperre und `disabled={busy}` bleiben Pflicht; Teilfehler sichtbar,
nicht still; ein Wiederholen nach Teilfehler darf den Plan nicht doppelt anlegen (`progress.planId`,
`wizardFinish.ts:95-98`). (4) B-476 senkt einzelne Karten auf eine niedrigere Sprosse – die Wertung hängt aber an
der Stufe des Versuchs (`PositionTestsController.cs:178`), Entscheidung 5 bleibt davon unberührt.

**Angriffsplan** (kein Backend, Entscheidung 4): (1) `wizardFinish.ts`: `planDatesFor(goal, termin)`,
`stageScheduleFor(goal, n, defaultStage)` und der `requireTypedTest`-Schnitt je Position, mit Tests;
(2) Schritt 2 Terminfeld + Sperre; (3) Feinschliff-Satz am Häkchen, Überblick-Zeile; (4) Abschluss: Plan →
Klassenarbeit (ein Aufruf), Teilfehler-Banner; (5) Plan-Detail liest `stageSchedule`; (6) `VaterWizard.test.tsx`,
E2E `assistent-klassenarbeit.spec.ts`.

**Testweg:** `wizardFinish.test.ts` (AK 3, AK 8), `VaterWizard.test.tsx` (AK 1, AK 2, AK 6), neue Spec
`frontend/e2e/assistent-klassenarbeit.spec.ts` (Rollengang Vater: Termin eingeben → Plan + Arbeit sichtbar, AK 4,
AK 7; Rollengang Sohn am Tag 1, AK 9; Teilfehler per `page.route` auf `class-tests`, AK 5) – am „N passed“
messen; `old-phrases.py` wegen geänderter Überblick-Texte.

## Verlauf

- **2026-11-08** — angelegt aus dem Rollengang Vater ([Protokoll](../pm-sitzung-2026-11-08-rollengang.md)),
  Ist-Stand belegt, Entscheidungen vom PM unter dem Auftrag „nachtlauffähig machen" (Vetorecht beim Nutzer),
  geschätzt. Abgegrenzt gegen B-248 (Kind-Seite, Zusatzpflicht) und B-16 (Positions-Formular).
- **2026-11-08** — **Freigabe des Nutzers (Autonomer Modus, Opt-in):** die sieben vorgelegten Entscheidungen der Fable-Sitzung hat der Nutzer im Dialog bestätigt („Ja, so fahren“, Antwort auf die Frage nach den Entscheidungen für die erste Nacht, [Protokoll](../pm-sitzung-2026-11-08-rollengang.md)). Ein Nachtlauf darf diese Story auf Grundlage der `## Entscheidungen` bauen; trifft er auf eine Produktfrage, die sie nicht hergeben, hält er an.
- **2026-11-09** — **angehalten** (Lauf AF, Freigabe 8): beim Bauen gefunden, dass Entscheidung 2 mit der Vorgabe
  `typed: true` des Ziels kollidiert (Offener Punkt 5, mit Belegen). Kein Code geschrieben. Zurück auf `ausformuliert`,
  `wartet_auf` die Entscheidung. Nebenbefund für den Bau: `CreateClassTestDto` nimmt `ExerciseIds` direkt mit
  (`ClassTestDtos.cs:20-21`) – Entscheidung 3 braucht also einen Aufruf, nicht n+1.
- **2026-11-14** — Abgleich Backlog-Lauf: gilt, kein Teil gebaut (kein `stageSchedule`, kein Termin, kein `class-tests`-Aufruf in `VaterWizard.tsx`/`wizardFinish.ts`); verrutschte Zeilen im Ist-Stand und in Offener Punkt 5 berichtigt (`VaterWizard.tsx:48-51`, `PositionPlayService.cs:73-81`, `PositionTestsController.cs:154,178`, `PositionProgressService.cs:150`). Offener Punkt 5 (Leiter vs. „Nur getippte Tests zählen") besteht unverändert und wartet weiter auf den Nutzer.
- **2026-11-14** — Refinement Backlog-Lauf: Offener Punkt 5 entschieden (Entscheidung 5, Variante a: Positionen mit Leiter ohne `requireTypedTest`, Strenge aus der Leiter; b–d verworfen) und Nebenbefund als Entscheidung 6 (Klassenarbeit mit einem Aufruf) aufgenommen, beides Stakeholder-Entscheidung im Auftrag des Nutzers; AK 8–9 neu, AK 3 mit roter Probe; zurück auf `geschaetzt` (M, frontend, keine Migration, kein Vertragsbruch), `wartet_auf` geleert.
- **2026-11-14** — Sprint 76, Frontend gebaut: Terminfeld (Pflicht) mit Sperre von „Weiter" in Schritt 2 (`KlassenarbeitTerminFeld`, `terminProblem`), Start/Dauer aus dem Termin abgeleitet und im Feinschliff nur lesbar (`planVorgaben`), Leiter `stageScheduleFor` mit änderbarem Schwellentag und Satz am Häkchen, `requireTypedTest: false` nur an Vokabel-Positionen mit Leiter, Klassenarbeit mit einem `POST class-tests` samt `exerciseIds` nach den Positionen (`progress.classTestId`, Teilfehler als `KlassenarbeitFehlt` mit Links, zweiter Anlauf ohne zweiten Plan), Überblick mit Zeitraum bis zum Vortag, Zeile „Stufenleiter" und „legt die Klassenarbeit am … an", Plan-Detailzeile „Stufen: … bis Tag n, dann …" (`leiterText`). Vitest: 11 neue Fälle in `wizardFinish.test.ts`, 5 in `VaterWizard.test.tsx`; neue E2E `assistent-klassenarbeit.spec.ts` (AK 1, 2, 4, 5, 7, 9) noch nicht gelaufen. Abweichung: Vitest für AK 1/2 am exportierten Baustein bzw. an den Zeilen-Funktionen statt am ganzen Bildschirm (Hausregel: kein nachgebauter Bildschirm); sieben Assistenten-Specs wählen jetzt ausdrücklich „Regelmäßig üben", weil das vorbelegte Ziel „Klassenarbeit" einen Termin verlangt.
- **2026-11-14** — Sprint 76, Review-Funde behoben: zweiter Anlauf nimmt den Klassenarbeit-Auftrag aus dem Durchgang, der den Plan anlegte (`progress.classTestAuftrag`), und Zurück/Stepper sind gesperrt, sobald der Plan steht; Leiter nur bei Stufe > 2 (Entscheidung 7), der Satz im Feinschliff nennt die Stufen über `leiterText` und sagt, dass „Nur getippte Tests zählen" dann nur für die übrigen Übungen gilt; Schwellentag als Rohtext (`SchwellenFeld`, geklemmt in `planVorgaben`, angewandt beim Verlassen); Teilfehler-Banner nennt „Übungen & Vorbereiten"; Kommentar in `e2e/erstbegegnung.spec.ts` berichtigt. Vitest: 6 neue Fälle (4 in `wizardFinish.test.ts`, 2 in `VaterWizard.test.tsx`); E2E-Satz in `assistent-klassenarbeit.spec.ts` nachgezogen.
- **2026-11-14** — **abgenommen** (Sprint 76). volle Suite 1650/1650 (Release, nach den Review-Fixes), Vitest 1079/1079, `tsc -b` grün; Rollengang als Playwright gegen frisch gestartete Server nach dem letzten Edit: 38 passed (Assistent inkl. `assistent-klassenarbeit`, Lehrwerk-/Reihen-/Fachlehrer-Formulare, `vater-von-null`, `kind-anlegen-id`, `uebung-teilspeichern`, `erstbegegnung`); Reviews `pugling-reviewer` + `frontend-reviewer`, Funde im Sprint behoben; Commit `8e970493`. Mutanten rot: Q9 Klassenarbeit ohne `exerciseIds` (AK 4), Q10 zweiter Anlauf mit neuem Termin (Review-Fund), Q11 Leiter bei Stufe ≤ 2 (Review-Fund, Entscheidung 7), Q12 `requireTypedTest` nicht `false` (AK 8/9). AK 1/2/5/7 trägt `assistent-klassenarbeit` (2 Fälle grün).
- **2026-11-14** — Sprint 76, Gesamt-Review: Namen der Leiterstufen im Assistenten aus dem Typ-Manifest (`types.stages("Vocabulary")`) statt aus `VOCAB_FORMS`; „Fach“ ist jetzt Pflichtfeld wie der Termin – Label „Fach (Pflicht)“ (per `htmlFor`), „Weiter“ gesperrt ohne Fach; acht Assistenten-Specs sprechen das Feld als „Fach (Pflicht)“ an.
- **2026-11-15** — Nachschau im Gesamt-Review des Laufs (Backend + Frontend über `40dd9bfa..HEAD`): Funde aus dem Zusammenspiel im Lauf behoben (Lernkontrolle vom Server `learningCheck`, Truhe/Bonus-Texte aus `rewardsUnlocked`, Agent prüft `canAssign` vor dem Schreiben, `--cadence None` abgelehnt, Truhen-Meldung und Tutorial-Halbsatz); siehe Protokoll „Abschluss“.
