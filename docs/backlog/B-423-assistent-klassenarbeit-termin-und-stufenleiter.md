---
tags: [typ/story, status/geschaetzt, bereich/frontend, bereich/klassenarbeit, bereich/training, rolle/supervisor]
aliases: [Assistent Klassenarbeit-Termin, Stufenleiter aus dem Assistenten]
status: geschaetzt
prio: P2
art: Wunsch
groesse: M
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-08-rollengang.md#feedback-vater
unverifiziert: false
grund: ""
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

- **Assistent:** `VaterWizard.tsx:45-48` – `GOALS.Klassenarbeit = { duration: 14, stage: 4, typed: true }`;
  `:94`/`:232` Dauer als freie Zahl, `:597` Feld „Dauer (Tage)"; `:358` schreibt `stage: defaultStage` in jede
  Position. Kein `stageSchedule`, kein Termin, kein Aufruf der Klassenarbeiten-API (keine Treffer für
  `class-tests`/`scheduledDate` in der Datei).
- **Stufenleiter existiert serverseitig:** `CreatePositionDto.StageSchedule` (`StudyPlanDtos.cs:100-104`),
  angewendet in `PositionPlayService.StageForDay` (`PositionPlayService.cs:56-64`): je `StageStep(DayNumber,
  Stage)` gilt ab dem Plantag `DayNumber`. Im Frontend ungenutzt (nur im generierten `contract.ts`);
  [B-16](B-16-positions-formular-umfang.md) hat den Listen-Editor im Positions-Formular bewusst zurückgestellt
  (Entscheidung 2).
- **Klassenarbeit-API:** `POST supervisor/class-tests` mit `CreateClassTestDto(ChildId, Title, Topic, SubjectId,
  ScheduledDate, …)` (`ClassTestDtos.cs:20`), Übungen per `POST class-tests/{id}/exercises`
  (`KlassenarbeitenController.cs:187`). Die Seite `/vater/class-tests` zeigt Termin, Status, Übungen, Note.
- **Anzeigestufe am Tag ist nicht testbar** (`PositionTestsController.cs:169`, `StageNotTestable`) – eine
  Leiter darf darum **keinen** ganzen Tag auf `ShowBoth` legen, sonst reißt das Tagesziel ohne Test. Die
  Erstbegegnung je Karte ist [B-419](B-419-erstbegegnung-neue-karte-erst-zeigen.md).
- `wizardFinish.ts:109-112` streift `stage` für Nicht-Vokabel-Typen – dasselbe muss für die Leiter gelten.
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

## Akzeptanzkriterien

1. Ziel „Klassenarbeit" → Schritt 2 zeigt ein Pflichtfeld „Termin"; ohne Termin ist „Weiter" gesperrt und das
   Label nennt den Grund; Termin < heute + 2 Tage → gesperrt mit Grund.
2. Der Plan endet am Vortag des Termins; Schritt 5 zeigt Zeitraum und „legt die Klassenarbeit am <Termin> an".
3. Jede Vokabel-Position trägt `stageSchedule` `[1→2, ceil(n/3)+1→Tipp-Stufe]`; Nicht-Vokabel-Positionen
   tragen keine Leiter (wie heute keine Stufe).
4. Nach dem Anlegen steht unter `/vater/class-tests` eine Arbeit mit Termin, Fach und allen gewählten Übungen.
5. Scheitert `POST class-tests`, ist der Plan angelegt und das Ende-Banner nennt die fehlende Klassenarbeit mit
   Link.
6. Die Ziele „Aufholen"/„Regelmäßig" verhalten sich unverändert (Dauer frei, keine Leiter, keine Arbeit).
7. Die Plan-Detailseite zeigt je Position die Leiter lesbar („Selbsteinschätzung bis Tag 4, dann Freitext").

## Schätzung

**Größe: M** — ein Feld, eine Ableitung (reine Funktion in `wizardFinish.ts`, testbar), zwei weitere Aufrufe,
Anzeige in Feinschliff/Überblick/Plan-Detail; Vitest + E2E. Kein Vertrag, kein Schema.

**Risiken:** (1) `wizardFinish` ist heute eine reine Funktion mit Rundlauf-Tests – die Leiter dort ableiten,
nicht im Komponentenzustand. (2) Zeitzone: Termin und Planende sind `DateOnly` (UTC-Regel aus `CLAUDE.md`) –
keine `new Date()`-Umrechnung mit Uhrzeit. (3) Der zweite Aufruf nach dem Plan ist ein zweiter Schreibpfad –
`useAction`-Sperre und `disabled={busy}` bleiben Pflicht; Teilfehler sichtbar, nicht still.

**Angriffsplan:** (1) `wizardFinish.ts`: `planDatesFor(goal, termin)` und `stageScheduleFor(goal, n,
defaultStage)` mit Tests; (2) Schritt 2 Terminfeld + Sperre; (3) Feinschliff-Satz, Überblick-Zeile;
(4) Abschluss: Plan → Klassenarbeit → Übungen, Teilfehler-Banner; (5) Plan-Detail liest `stageSchedule`;
(6) `VaterWizard.test.tsx`, E2E `assistent-klassenarbeit.spec.ts` (Rollengang Vater: Termin eingeben → Plan +
Arbeit sichtbar).

**Testweg:** `wizardFinish.test.ts`, `VaterWizard.test.tsx` (Vitest), neue Spec
`frontend/e2e/assistent-klassenarbeit.spec.ts`; `old-phrases.py` wegen geänderter Überblick-Texte.

## Verlauf

- **2026-11-08** — angelegt aus dem Rollengang Vater ([Protokoll](../pm-sitzung-2026-11-08-rollengang.md)),
  Ist-Stand belegt, Entscheidungen vom PM unter dem Auftrag „nachtlauffähig machen" (Vetorecht beim Nutzer),
  geschätzt. Abgegrenzt gegen B-248 (Kind-Seite, Zusatzpflicht) und B-16 (Positions-Formular).
- **2026-11-08** — **Freigabe des Nutzers (Autonomer Modus, Opt-in):** die sieben vorgelegten Entscheidungen der Fable-Sitzung hat der Nutzer im Dialog bestätigt („Ja, so fahren“, Antwort auf die Frage nach den Entscheidungen für die erste Nacht, [Protokoll](../pm-sitzung-2026-11-08-rollengang.md)). Ein Nachtlauf darf diese Story auf Grundlage der `## Entscheidungen` bauen; trifft er auf eine Produktfrage, die sie nicht hergeben, hält er an.
