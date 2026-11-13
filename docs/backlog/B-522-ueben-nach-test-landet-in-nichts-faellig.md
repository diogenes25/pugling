---
tags: [typ/story, status/in-arbeit, bereich/frontend, rolle/student]
aliases: [Üben nach dem Test landet in „Nichts fällig"]
status: in-arbeit
prio: P3
art: Wunsch
groesse: S
wo: "beides"
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-12-tageslauf.md (Sprint 64, Review B-430)
unverifiziert: false
entgangen_bei: []
grund: ""
ersetzt_durch: []
---

# B-522 · „▶ Üben" nach einem verpatzten Test landet bei Leitner-Positionen oft in „Nichts fällig 🎉"

Gefunden vom `frontend-reviewer` an [B-430](B-430-nach-test-diese-woerter-ueben.md) (c), dort bewusst als Grenze
angenommen. Bei `useLeitner=true` zeigt die Übungsrunde nur fällige Karten; wer erst übt und dann testet, hat heute
nichts mehr fällig. Das Kind, eben durchgefallen, sieht „Nichts fällig 🎉 – Mach den Test oder komm später wieder."
und „🎯 Zum Test" – bei 0 Restversuchen führt das in `test_attempts_exhausted`.

## User Story

Als *Sohn* möchte ich nach einem verpatzten Test auf „▶ Üben" einen Bildschirm sehen, der zu meiner Lage passt – entweder
Karten zum Üben oder eine ehrliche Auskunft, wann es weitergeht –, damit ich nicht nach einer Niederlage „🎉" und einen
Test angeboten bekomme, den ich heute nicht mehr schreiben darf.

## Ist-Stand am Code

Gelesen am 2026-11-13 (Worktree `backlog-lauf`, Stand nach Sprint 69 / `699b02a7`):

- `frontend/src/sohn/SohnPractice.tsx:334`: `const testable = session.current?.testable ?? true;` – das einzige Gate.
- `frontend/src/sohn/SohnPractice.tsx:336-346`: Phase `empty` zeigt `practice.nothingDue` („Nichts fällig 🎉"), darunter
  `nothingDueTest` bzw. `nothingDueLater`, und bei `testable` den Knopf „🎯 Zum Test". Der Bildschirm weiß weder, dass er
  von einem verpatzten Test kommt, noch ob heute noch Versuche übrig sind.
- `frontend/src/sohn/SohnPractice.tsx:374`: dasselbe Gate am Rundenende („🎯 Weiter zum Test", `practice.onToTest`).
- `frontend/src/i18n/de.ts:320-323`, `:331`: die Texte; keiner für „nach dem Test" oder „Versuche aufgebraucht".
- `Testable` heißt **Tag/Stufe**, nicht Restversuche: `backend/Pugling.Contracts/Student/PracticeDtos.cs:6-14`
  („mirrors `PositionStatus.Testable`"), gesetzt in `backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:35-38`
  über `progress.IsTestable(pos, plan, s.Day)`.
- Die Sperre der Versuche lebt nur im Test-Controller: `PositionTestsController.cs:49` (`MaxAttemptsPerDay = 2`),
  `:150-165` (offener Versuch wird fortgesetzt; sonst gezählt wird der **Start**, `used >= MaxAttemptsPerDay` →
  `test_attempts_exhausted`), `:473-476` (dieselbe Zählung für `attemptsRemaining` der Abgabe).
- `frontend/src/sohn/SohnTest.tsx:185`: `onPractice={() => nav(`/sohn/practice/${positionId}`)}` – ohne State;
  `:380-385`: „▶ Üben" bei `!passed`, „Nochmal" nur bei `attemptsRemaining > 0` (B-62).
- `PositionStatus.DueCount` (`backend/Pugling.Contracts/Shared/ProgressDtos.cs:24-28`) kennt die Fälligkeit je Position,
  aber die Test-Abgabe (`SubmitResponse`) trägt sie nicht.
- Bei `useLeitner=false` filtert die Runde nicht nach Fälligkeit – das Problem betrifft nur Leitner-Positionen.

## Die echte Lücke

Zwei Dinge: (a) Der Übungsbildschirm bietet den Test nach `Testable` an, ohne die Restversuche zu kennen – auch auf dem
Weg von der Startseite. (b) Der Leertext feiert („🎉"), obwohl das Kind gerade durchgefallen ist.

## Offene Punkte

1. ~~Variante (1) Knopf „▶ Üben" nur bei `DueCount > 0`, (2) eigener Leertext, (3) beides?~~ → Entscheidung 1.
2. ~~Gilt (2) auch für den Weg von der Startseite?~~ → Entscheidung 2.
3. ~~Wie erfährt der Übungsbildschirm die Restversuche?~~ → Entscheidung 3.

## Entscheidungen

Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13, [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).

1. **Variante (2) – der Übungsbildschirm wird ehrlich; „▶ Üben" bleibt.** Abweichend von der PM-Empfehlung (3).
   *Begründung:* Mit einem ehrlichen Leerbildschirm kostet der Umweg einen Tipp und sagt dem Kind, wann es weitergeht –
   das ist die Lage, die die User Story verlangt. Variante (1) bräuchte die Fälligkeit in der Test-Abgabe (oder einen
   zweiten Abruf der Übersicht) und versteckte einen Knopf, den das Kind gerade erwartet. *Kosten:* ein leerer Umweg
   bleibt möglich; siehe „Nicht in dieser Story".
2. **Ja, die Restversuchs-Auskunft gehört in den Übungsbildschirm selbst**, nicht in den Navigation-State. *Begründung:*
   Von der Startseite aus („Üben" nach zwei verbrauchten Versuchen) ist die Lage dieselbe; eine Herkunftsangabe allein
   heilte nur einen von zwei Wegen. *Kosten:* ein Feld mehr in der Sitzungsantwort.
3. **Additives Feld `TestAttemptsLeft` (int) an `SessionResponse`**, berechnet wie die Sperre in `Start`: offener
   Versuch zählt als startbar (≥ 1), sonst `MaxAttemptsPerDay − heute gestartete` (≥ 0); für den Supervisor
   `MaxAttemptsPerDay` (er ist von der Sperre ausgenommen, wie `SubmitResponse`). Die Zählung wandert in **eine** Methode
   (`PositionProgressService`), die `PositionTestsController.Start`/`Submit` und `PositionPracticeController.Map` teilen.
   *Begründung:* `Testable` bleibt, was es ist (Tag/Stufe, B-114) – eine Umdeutung bräche die Doku-Gleichheit mit
   `PositionStatus.Testable`. Eine Quelle verhindert, dass Anzeige und Sperre auseinanderlaufen. *Kosten:* `Map` wird
   asynchron (alle Aufrufer im Controller); kein Vertragsbruch, Frontend-Typ und `Pugling.Client` ziehen additiv nach.
4. **Leertext ohne 🎉 nach einem Test.** `SohnTest` navigiert mit State `{ nachTest: true }`; dann lautet der Titel
   „Heute nichts mehr fällig" statt „Nichts fällig 🎉". *Begründung:* Nach einer Niederlage zu feiern ist der zweite
   Teil der Lücke und kostet nur einen State. Ein Neuladen verliert den State und zeigt wieder 🎉 – hinnehmbar, die
   Auskunft über die Versuche (Entscheidung 3) bleibt richtig. *Kosten:* ein Text, ein Zweig.

## Akzeptanzkriterien

1. `GET`/`POST` der Übungssitzung liefert `testAttemptsLeft`: 2 ohne Test heute, 1 nach einem gestarteten, 0 nach zwei
   gestarteten und abgegebenen; ≥ 1, solange ein Versuch offen ist. *Rot, wenn* das Feld fehlt oder die Zählung den
   offenen Versuch als verbraucht wertet.
2. Der Supervisor bekommt `MaxAttemptsPerDay`, auch nach zwei Versuchen des Kindes. *Rot, wenn* die Ausnahme fehlt.
3. `Start` des Tests weist weiter mit `test_attempts_exhausted` ab – jetzt über die geteilte Methode. *Rot, wenn* die
   Sperre beim Umzug verloren geht (bestehender Fall bleibt grün).
4. Übungsbildschirm, nichts fällig, `testAttemptsLeft = 0`: Titel „Heute nichts mehr fällig", Text „Deine Testversuche
   für heute sind aufgebraucht. Morgen geht's weiter.", **kein** Knopf „🎯 Zum Test". *Rot, wenn* der Knopf nur an
   `testable` hängt.
5. Rundenende mit `testAttemptsLeft = 0`: kein „🎯 Weiter zum Test". *Rot, wenn* `SohnPractice.tsx:374` nur `testable` prüft.
6. Nichts fällig, Herkunft „nach dem Test", Versuche übrig: Titel ohne 🎉, Knopf „🎯 Zum Test" da. *Rot, wenn*
   `SohnTest` keinen State mitgibt oder der Titel ihn nicht liest.
7. Nichts fällig, von der Startseite, Versuche übrig: unverändert „Nichts fällig 🎉" mit Test-Knopf. *Rot, wenn* der
   neue Titel auch ohne Herkunft erscheint.

## Schätzung

**Größe: S** – eine geteilte Zählung, ein additives Feld, ein asynchrones `Map`, zwei Texte und zwei Gates im Frontend.
`wo: beides`, `migration: nein`, `vertragsbruch: nein` (additiv).

**Risiken:** `Map` hat mehrere Aufrufer (`Start`, Sitzung lesen, Heartbeat/Ende); jeder muss die Position samt Plan geladen
haben, sonst rechnet `Testable` heute schon falsch (Kommentar `PositionPracticeController.cs:33-34`). Der Zählschritt
darf nicht in eine Schleife über viele Sitzungen geraten (N+1) – `Map` wird nur für eine Sitzung gerufen; beim Bau prüfen.
`DocsCaptureTests` schreiben das neue Feld in `docs/api-examples` (byte-stabil, kein zeitabhängiger Wert).

**Angriffsplan:** (1) Backend-Tests AK 1–3 in `PositionPracticeFlowTests.cs`, AK 1 rot gegen den Ist-Vertrag; (2) Zählung
nach `PositionProgressService` ziehen, `Start`/`Submit` darauf umstellen, Feld in `SessionResponse` und `Map`;
(3) `pugling-reviewer`; (4) Frontend: Typ, Gates an `:344`/`:374`, Texte in `de.ts`/`en.ts`, State in `SohnTest.tsx:185`;
(5) Vitest AK 4–7; (6) rote Probe je AK; (7) `frontend-reviewer`.

**Testweg:** `backend/Pugling.Api.Tests/PositionPracticeFlowTests.cs` (AK 1–3; der Sperrfall in den Test-Tests bleibt
Zeuge für AK 3); `frontend/src/sohn/SohnPractice.test.tsx` (AK 4, 5, 6, 7); `frontend/src/sohn/TestResult.test.tsx` bzw.
`SohnTest.test.tsx` für den mitgegebenen State (AK 6). Rollengang: Kind fällt zweimal durch, tippt „▶ Üben".

**Nicht in dieser Story:** Knopf „▶ Üben" am Testergebnis nur bei `DueCount > 0` (Variante 1). Bräuchte die Fälligkeit
in `SubmitResponse`; erst bauen, wenn der Rollengang zeigt, dass der eine leere Tipp stört.

## Verlauf

- **2026-11-13** — angelegt aus dem Review von B-430 (c), Tageslauf Sprint 64.
- **2026-11-13** — ausformuliert (Tageslauf, Restzeit): Ist-Stand am Code belegt, Lücke in (a)/(b) zerlegt, zwei offene Punkte; `art: Wunsch` – Grillen im Dialog.
- **2026-11-13** — Ist-Stand nach Sprint 69 neu gelesen (Zeilen verschoben: Leeransicht jetzt `SohnPractice.tsx:336-346`, Texte `de.ts:320-323`).
- **2026-11-13** — `gegrillt`: Entscheidungen 1–4 (ehrlicher Übungsbildschirm statt versteckter Knopf, Restversuche als additives Sitzungsfeld aus einer geteilten Zählung, kein 🎉 nach dem Test); Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13, [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).
- **2026-11-13** — `geschaetzt`: S, beides, ohne Migration und Vertragsbruch; Variante (1) ausdrücklich nicht in dieser Story.
- **2026-11-13** — `in-arbeit` (Sprint 72): die Tageszählung zieht als `PositionProgressService.TestAttemptsLeftAsync` (samt `MaxTestAttemptsPerDay`) aus dem Test-Controller in den Service; `Start`, `Submit` und `PositionPracticeController.MapAsync` teilen sie. Additives Feld `SessionResponse.TestAttemptsLeft`; `SohnPractice` sperrt „🎯 Zum Test"/„🎯 Weiter zum Test" auch an den Restversuchen, Leertext „Heute nichts mehr fällig" nach dem Test (State `{ nachTest: true }` aus `SohnTest`) oder bei aufgebrauchten Versuchen, dazu „Deine Testversuche für heute sind aufgebraucht. Morgen geht's weiter." (de/en). Tests: `PositionPracticeFlowTests.Sitzung_Nennt_Die_Restversuche_Des_Tests` (AK 1, AK 3), `…Sitzung_Zeigt_Dem_Supervisor_Die_Volle_Versuchszahl` (AK 2); `SohnPractice.test.tsx` „SohnPractice nach dem Test" (AK 4–7, mit Gegenprobe zu AK 5); `SohnTest.test.tsx` „gibt dem Üben mit, dass das Kind vom Test kommt" (AK 6). Rote Probe:

  Gemessen an `36ebffe5 + Arbeitsbaum geändert`, 2026-11-13 23:29.

  | Mutante | Stelle | erwartet | gemessen | Anmerkung |
  |---|---|---|---|---|
  | M1 | backend/Pugling.Api/Services/Shared/PositionProgressService.cs:103 | red | red | offener Versuch zählt als verbraucht (AK 1) |
  | M2 | backend/Pugling.Api/Services/Shared/PositionProgressService.cs:100 | red | red | Supervisor-Ausnahme fehlt (AK 2) |
  | M3 | backend/Pugling.Api/Controllers/Student/PositionTestsController.cs:144 | red | red | Sperre beim Start verloren (AK 3) |
  | M4 | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:41 | red | red | Sitzung zählt nicht selbst (AK 1) |
  | M5 | frontend/src/sohn/SohnPractice.tsx:341 | red | red | Knopf nur an `testable` (AK 4, AK 5) |
  | M6 | frontend/src/sohn/SohnPractice.tsx:343 | red | red | Titel liest den State nicht (AK 6) |
  | M7 | frontend/src/sohn/SohnPractice.tsx:343 | red | red | neuer Titel ohne Herkunft (AK 7) |
  | M8 | frontend/src/sohn/SohnTest.tsx:211 | red | red | `onPractice` ohne State (AK 6) |

  Grundlinie nach dem Zurückspielen: green. Abweichung vom Angriffsplan: der Fall „Versuche aufgebraucht, von der Startseite" bekommt ebenfalls den Titel ohne 🎉 (AK 4 verlangt ihn ohne Herkunftsbedingung).
