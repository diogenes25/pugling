---
tags: [typ/story, status/geschaetzt, bereich/gamification, bereich/frontend, rolle/student]
aliases: [Missionen-Tab, Missionen mit Restzeit]
status: geschaetzt
prio: P3
art: Wunsch
groesse: XS
wo: beides
migration: nein
vertragsbruch: nein
quelle: "B-525, Entscheidung 9 (Grill-Runde 2026-11-13); docs/duolingo-snipwise-vergleich-plan.md"
grund: ""
ersetzt_durch: []
wartet_auf: ""
---

# B-528 · Missionen mit Restzeit (statt eines eigenen Tabs)

Duolingo gibt Missionen einen eigenen Ort: Tages-, Wochenend- und Monatsmission je mit Restzeit und
Belohnung. In Pugling wandert das Missionen-Panel mit
[B-525](B-525-sohn-startseite-als-schlangenpfad.md) von der Startseite in den Tab „Erfolge". Die Idee hier
ist der nächste Schritt: ein eigener Tab, der je Mission die Restzeit ihrer Periode zeigt. Geschnitten auf den
Kern: die **Restzeit** – im vorhandenen Panel, ohne sechsten Navigationseintrag und ohne Monatsperiode.

## User Story

Als *Sohn* möchte ich bei jeder Mission sehen, wie lange ich noch Zeit habe („noch 3 Tage", „nur noch heute"),
damit ich eine Wochenmission nicht erst am Montag als verpasst bemerke.

## Ist-Stand am Code

Gelesen am 2026-11-13 (Worktree `backlog-lauf`, `699b02a7`):

- Das Panel: `frontend/src/sohn/GamificationPanels.tsx:21-61` (`MissionsPanel`); je Mission
  `${m.current}/${m.target} · ${t(PERIOD_KEY[m.period])}` (`:56-58`), also nur „täglich/wöchentlich/einmalig", keine Frist.
- Eingebunden im Tab „Erfolge": `frontend/src/sohn/SohnProgress.tsx:7`, `:47`.
- Navigation: fünf Einträge `frontend/src/sohn/SohnApp.tsx:165-169` (Start, Erfolge, Konto, Shop, Skins).
- Vertrag: `MissionStatus(Id, Title, Metric, Period, Target, Current, Completed, RewardPoints)`
  (`backend/Pugling.Contracts/Student/LearnProgressDtos.cs:25-26`) – keine Frist.
- Der Server kennt das Periodenende schon: `GamificationService.PeriodWindow` (`backend/Pugling.Api/Services/Shared/GamificationService.cs:162-169`,
  Tag = heute, Woche = Montag…Sonntag UTC), benutzt in `MapMissionAsync` (`:96-102`).
- Perioden: `MissionPeriod` Daily/Weekly/OneOff (`backend/Pugling.Contracts/Common/GamificationBaseTypes.cs:24-31`), keine Monatsperiode.

## Die echte Lücke

Die Frist liegt im Server (`PeriodWindow.To`) und erreicht das Kind nicht. Ein eigener Ort fehlt dagegen nicht – das
Panel hat seinen Platz im Tab „Erfolge".

## Offene Punkte

1. ~~Rechtfertigt das einen sechsten Navigationseintrag?~~ → Entscheidung 1.
2. ~~Kommt eine Monatsperiode dazu?~~ → Entscheidung 2.
3. ~~Wer rechnet die Restzeit – Server oder Client?~~ → Entscheidung 3.

## Entscheidungen

Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13, [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).

1. **Kein eigener Tab.** *Begründung:* Fünf Einträge füllen die untere Leiste am Telefon; ein sechster für eine Handvoll
   Missionen, die der Vater anlegt, verdünnt die Navigation, und B-525 hat den Ort gerade erst entschieden („Erfolge").
   *Kosten:* Missionen bleiben einen Tipp tiefer als bei Duolingo.
2. **Keine Monatsperiode.** *Begründung:* Für ein Kind von 11–15 ist ein Monat zu weit weg, um zu steuern; die
   Pflichtziele des Vaters laufen täglich/wöchentlich. Ein neuer Enum-Wert zöge Supervisor-Formular, Metrik-Fenster und
   Tests nach. *Kosten:* keine Parität mit Duolingo.
3. **Der Server liefert `DaysLeft` (int?, additiv an `MissionStatus`)**: 0 = letzter Tag der Periode, `null` bei
   `OneOff`, aus `PeriodWindow(...).To − today`. *Begründung:* API-First, und der Tageswechsel ist UTC
   (Fallstrick „Zeit/UTC") – rechnete der Client mit seiner lokalen Uhr, stünde „nur noch heute" nach Mitternacht lokal
   falsch. *Kosten:* ein Feld; Stunden zeigt das bewusst nicht.
4. **Anzeige nur für offene Missionen:** „nur noch heute" (0), „noch 1 Tag", „noch N Tage"; erledigte zeigen weiter
   die Belohnung. *Begründung:* Eine Frist zu einer erledigten Mission ist Lärm. *Kosten:* keine.

## Akzeptanzkriterien

1. Tagesmission: `daysLeft = 0`. *Rot, wenn* das Feld fehlt oder die Woche statt des Tages zählt.
2. Wochenmission, Uhr auf Mittwoch: `daysLeft = 4`; auf Sonntag: `0`. *Rot, wenn* die Woche nicht am Sonntag (UTC) endet.
3. Einmalige Mission: `daysLeft = null`. *Rot, wenn* `OneOff` eine Zahl bekommt.
4. Panel: offene Wochenmission mit `daysLeft = 4` zeigt „noch 4 Tage", mit `0` „nur noch heute"; eine erledigte zeigt
   keine Frist. *Rot, wenn* die Frist fehlt oder auch bei erledigten erscheint.

## Schätzung

**Größe: XS** – ein additives Feld aus einem schon berechneten Fenster, ein Text im Panel. `wo: beides`,
`migration: nein`, `vertragsbruch: nein`.

**Risiken:** `DocsCaptureTests` halten `docs/api-examples` byte-stabil – `daysLeft` hängt am Wochentag und muss dort mit
der eingefrorenen Test-Uhr laufen oder maskiert werden. Sonst keine.

**Angriffsplan:** (1) Fälle AK 1–3 in `GamificationTests.cs` mit `_factory.Clock` (rot gegen den Ist-Vertrag);
(2) `DaysLeft` in `MapMissionAsync`; (3) Frontend-Typ, Text in `GamificationPanels.tsx:56-58`, Texte in `de.ts`/`en.ts`;
(4) Vitest AK 4; (5) rote Probe; Reviewer beider Seiten.

**Testweg:** `backend/Pugling.Api.Tests/GamificationTests.cs` (AK 1–3); `frontend/src/sohn/SohnProgress.test.tsx`
(AK 4, rendert das Panel). Kein E2E.

**Nicht in dieser Story:** eigener Missionen-Tab, Monatsperiode, Restzeit in Stunden.

## Verlauf

- 2026-11-13 · `idee` – abgespalten aus B-525; Prio P3 ist ein Vorschlag.
- **2026-11-13** — `ausformuliert`: am Code belegt – das Periodenende kennt der Server schon (`PeriodWindow`), es fehlt nur im Vertrag; Titel auf den Kern geschnitten.
- **2026-11-13** — `gegrillt`: Entscheidungen 1–4 (kein sechster Tab, keine Monatsperiode, `DaysLeft` vom Server); Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13, [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).
- **2026-11-13** — `geschaetzt`: XS, beides, ohne Migration und Vertragsbruch.
