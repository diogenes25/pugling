---
tags: [typ/story, status/abgenommen, bereich/auswertung, rolle/supervisor]
aliases: [Zeitachse ohne alte Pläne]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: "Backend-Review Sprint 79, Backlog-Lauf 2026-11-15 (docs/pm-sitzung-2026-11-15-backlog-lauf.md)"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
---

# B-558 · Die Zeitachse der Kinder vergisst einen Plan, sobald er abgeschaltet ist

Die Zeitachse (B-39, `GET supervisor/children/overview`) zählt je Tag nur Pläne, die **heute** aktiv sind
(`ChildrenDashboardService.TimelineAsync`, Filter `p.Active`) – dieselbe Regel wie die Tagesübersicht und die
Malus-Fairness. Weil je Kind nur ein Plan aktiv ist (`DeactivateSiblingPlansAsync`), schaltet jeder neue Plan den alten
ab, und dessen vergangene Wochen verschwinden aus der Zeitachse. Für „seine Kinder über die Zeit“ ist das zu wenig: der
Vater sieht nach einem Kapitelwechsel nur noch die Wochen seit dem Wechsel.

Bewusst nicht in Sprint 79 gebaut: das Sprint-Ziel (Überblick über die laufende Zeit) gilt ohne, und die Frage, was an
einem vergangenen Tag „aktiv“ war, braucht eine Entscheidung (der Plan trägt keine Aktivierungs-Historie; ein Plan, der
nie aktiv war – z. B. ein Entwurf des KI-Agenten –, darf nicht als verpasste Pflicht rot erscheinen). Mit B-539 (ein
aktiver Plan je Fach) wird die Lücke kleiner, nicht null.

## User Story

Als *Vater* möchte ich in der Zeitachse auch die Wochen vor einem Kapitelwechsel sehen, damit ich den Verlauf meines
Kindes über die Zeit beurteilen kann – ohne dass mir ein nie benutzter Entwurf als verpasste Pflicht angezeigt wird.

## Ist-Stand am Code

Gemessen am 2026-11-17 (Stand `4586e493`):

- `backend/Pugling.Api/Services/Supervisor/ChildrenDashboardService.cs:74-76` lädt nur Pläne mit `p.Active`;
  `:81-88` summiert je Tag Ziele, erfüllte Ziele und Punkte über diese Pläne; `:90` setzt die Tagespflicht als „alle
  Pläne mit Tagesziel erfüllt“, `null` ohne Tagesziel; `:96` macht eine Periode erfüllt, wenn ein Tag eine Pflicht trug
  und kein Tag offen war.
- Test: `backend/Pugling.Api.Tests/ChildrenOverviewTests.cs` (Zeitachse aus B-39).

## Die echte Lücke

Ein abgeschalteter Plan fällt aus allen Tagen heraus – auch aus denen, an denen das Kind in ihm gelernt hat. Ihn einfach
mitzuzählen wäre falsch herum: ein nie aktivierter Entwurf erschiene dann als lauter offene Pflicht-Tage.

## Entscheidungen

Autonom gegrillt im Nachtlauf 2026-11-17 (`art: Defekt`, Freigabe 1). Die Regel ist nicht neu erfunden, sondern die
Fairness-Regel aus [B-606](B-606-kind-tagessicht-ueber-alle-plaene.md) Entscheidung 6 (= B-539 Entscheidung 6),
übertragen auf die Zeitachse.

1. **Inaktive Pläne zählen nur zu Gunsten des Kindes.** An einem Tag, an dem ein heute inaktiver Plan lief, zählen seine
   **erfüllten** Ziele und seine **Punkte** mit, und hat er seine Tagespflicht erfüllt, zählt der Tag als erfüllt-fähig
   wie bei einem aktiven Plan. Eine **offene** Pflicht eines inaktiven Plans macht den Tag nie offen. Begründung:
   dieselbe Regel trägt schon der Malus (`PlanDueForPeriod`) und künftig die Kind-Serie; der Entwurf eines KI-Agenten
   erscheint so nie rot.
2. **Die Ziel-Summe (`GoalsTotal`) eines inaktiven Plans zählt nur an Tagen, an denen er mindestens ein Ziel erfüllt
   oder Punkte gebracht hat.** Begründung: sonst zeigte die Zeitachse für einen Entwurf „0 von 6 Zielen“ – eine offene
   Pflicht durch die Hintertür des Nenners. Kosten: an einem Tag, an dem das Kind im alten Plan nur teilweise gelernt
   hat, steht der Nenner des alten Plans voll drin – das ist richtig, denn der Plan war da offenbar in Gebrauch.
3. **Keine Migration, keine Aktivierungs-Historie.** Begründung: Entscheidung 6 von B-606 kommt ohne aus; eine Historie
   wäre ein Wunsch mit Schema-Kosten, kein Defekt-Fix.

## Akzeptanzkriterien

1. Plan A (Kind lernt an Tag 1, erfüllt die Tagespflicht) wird abgeschaltet, Plan B ist aktiv ⇒ die Zeitachse zeigt
   Tag 1 mit den Zielen und Punkten von Plan A und Pflicht erfüllt. *Rot mit dem heutigen Filter `p.Active`.*
2. Ein nie benutzter, inaktiver Plan mit Tagesziel läuft über den Zeitraum ⇒ kein Tag wird durch ihn offen, und sein
   Nenner erscheint nicht (`GoalsTotal` unverändert gegenüber ohne ihn). *Rot, wenn inaktive Pläne wie aktive gezählt
   werden.*
3. Ein inaktiver Plan, in dem das Kind an Tag 2 nur teilweise gelernt hat (Pflicht offen), während der aktive Plan seine
   Pflicht erfüllt ⇒ Tag 2 gilt als erfüllt. *Rot, wenn eine offene Pflicht eines inaktiven Plans den Tag offen macht.*
4. Aktive Pläne verhalten sich unverändert (Bestandstests in `ChildrenOverviewTests.cs` grün ohne inhaltliche
   Änderung).

## Schätzung

**S** – eine Methode in `ChildrenDashboardService`, drei Integrationstests. Kein Vertrag, keine Migration, kein
Frontend (die Zeitachse zeigt, was der Endpunkt liefert). Vergleich: B-219-Nachzug.

### Risiken

- Kosten: inaktive Pläne verlängern die Schleife Tage × Pläne (`MaxRangeDays` = 92 begrenzt). Pläne vorab auf solche
  beschränken, die im Zeitraum laufen – wie heute.
- B-606 baut eine Kind-Serie mit derselben Regel. Liegt dort ein wiederverwendbarer Baustein („Tag eines Plans zählt
  zu Gunsten“), wird er benutzt statt nachgebaut (Regel an der Senke).

#### Angriffsplan

`TimelineAsync`: Pläne ohne `p.Active`-Filter laden, je Tag aktive wie heute summieren, inaktive nur nach
Entscheidungen 1–2; Doku-Kommentar von `ChildPeriod`/`TimelineAsync` nachziehen („plans that are active now“).

##### Testweg

`ChildrenOverviewTests.cs`: drei neue Fälle (AK 1–3), Bestand als Gegenprobe (AK 4). Reviewer: `pugling-reviewer`.

## Verlauf

- **2026-11-15** — angelegt aus dem Backend-Review von Sprint 79 (Backlog-Lauf 2026-11-15); die Doku von `ChildPeriod`
  nennt die Regel seither ehrlich („plans that are active now“).
- **2026-11-17** — Nachtlauf 2026-11-17: `idee` → `geschaetzt` in einem Zug (autonom gegrillt, `art: Defekt`).
  Ist-Stand am Code gemessen, drei Entscheidungen aus der Fairness-Regel von B-606/B-539 Entscheidung 6, AKs mit
  Rot-Bedingung, S.
- 2026-11-17 · `in-arbeit` – Sprint 84 im Nachtlauf 2026-11-17.
- 2026-11-17 · Gebaut (Subagent, Sprint 84): `TimelineAsync` lädt alle im Zeitraum laufenden Pläne; die Regel liegt als
  `PositionProgressService.TimelineDutyDone` neben `IsCompleteDay` (B-606), die Serie bleibt unverändert.
  AK-Fälle → Test, Mutante: AK 1 → `ChildrenOverviewTests.cs:246`, T1 (`p.Active` zurück) → rot; AK 2 → `:267`, T2
  (Nenner inaktiver Pläne immer) → rot; AK 3 → `:292`; Review-Fund (`(p.Active || p.Done)` ungetestet) → `:315`, T3 → rot.
  Tag 2 lässt sich nur über die Woche unterscheiden: `ChildPeriod.DutyDone` ist `bool`, „keine Pflicht" und „offen"
  sehen am einzelnen Tag gleich aus (kein Vertragsbruch dafür). Gezählter Fund 🟡 (s. o.); 🟢 behoben: Satz „same daily
  rollup" → „same per-plan rollup". Hingenommen (Entscheidung 1): ein Tag, an dem das Kind die Pflicht eines später
  abgeschalteten Plans verpasst hat, steht danach neutral statt offen – der Malus dafür wurde trotzdem gebucht.
- 2026-11-17 · abgenommen in Sprint 84 (30711266): Rollengang `frontend/e2e/zwei-faecher.spec.ts` in der vollen Suite
  **168 passed** gegen frisch gestarteten Server (Ausweichports 5201/5174); `dotnet test` 1795, Vitest 1160; Mutanten-Tabelle
  im Verlauf von B-539. Protokoll `docs/pm-sitzung-2026-11-17-nachtlauf.md`.
