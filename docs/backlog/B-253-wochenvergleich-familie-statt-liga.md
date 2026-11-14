---
tags: [typ/story, status/abgenommen, bereich/gamification, rolle/student, rolle/supervisor]
aliases: [Familien-Liga, Wochenvergleich]
status: abgenommen
prio: P3
art: Wunsch
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: docs/knowunity-vergleich-plan.md#wo-knowunity-weiter-ist
grund: ""
nachgeschaut: 2026-11-14
ersetzt_durch: []
---

# B-253 · Wochenvergleich gegen die eigene Vorwoche (statt einer Liga)

Knowunity motiviert über eine Wochen-Liga mit bis zu 30 Mitschülern und Rängen von Bronze bis Gold. Eine
Liga mit Fremden ist für ein Kind um 11 Jahre ohne Moderation nicht vertretbar und passt nicht zur
Familien-App. Zu prüfen war eine eingehegte Form; der Nutzer hat am 2026-10-23 entschieden (G-6): das Kind
vergleicht sich **nur mit der eigenen Vorwoche**. Dieselbe Druck-Frage wie in
[B-222](B-222-wochen-streak-als-default.md); Ränge liegen bei [B-221](B-221-level-leiter-fuer-auszeichnungen.md).

## User Story

Als *Sohn* möchte ich sehen, wie viele Münzen ich diese Woche schon verdient habe und wie viele letzte Woche bis
zum selben Tag, damit ich merke, ob ich dranbleibe – ohne mich mit anderen messen zu müssen.

## Ist-Stand am Code

Gelesen am 2026-11-13 (Worktree `backlog-lauf`, `699b02a7`):

- Es gibt keinen Wochenvergleich: kein `Week`-Feld in `backend/Pugling.Contracts/Student/`, kein „Vorwoche" in API oder
  Arcade (`grep` über Api, Contracts, `frontend/src`).
- `GET student/me/points` liefert nur das Guthaben (`backend/Pugling.Api/Controllers/Student/MeController.cs:26-35`),
  `GET student/me/points/entries` die Buchungen seitenweise ohne Zeitraum-Filter (`:43-56`). Die Arcade müsste die
  Wochensumme aus allen Seiten addieren – das gehört nach API-First auf den Server.
- Währung je Buchungsart: `backend/Pugling.Api/Services/Shared/PointKindCurrency.cs:19-28`. **Münzen** sind `Base`,
  `Goal`, `Manual`, `ShopCoins`, `GoalPenalty`, `ObjectiveCoins`, `DailyBoxCoins`, `ExtraTrainingCoins`; Combo, Speed,
  Mission und Achievement zahlen in **Gems**.
- Wochenlogik Montag–Sonntag UTC gibt es schon: `GamificationService.WeekMonday` (`backend/Pugling.Api/Services/Shared/GamificationService.cs:171`).
- Ort: im Tab „Erfolge" steht bereits der Punkte-Chip `frontend/src/sohn/SohnProgress.tsx:41`; die Startseite wird von
  [B-525](B-525-sohn-startseite-als-schlangenpfad.md) umgebaut.

## Die echte Lücke

Die Buchungen liegen vollständig vor; es fehlt eine serverseitige Summe je Woche und eine Zeile, die sie dem Kind zeigt.

## Offene Punkte

1. ~~Was zählt als „verdient"?~~ → Entscheidung 1.
2. ~~Woche Montag–Sonntag UTC?~~ → Entscheidung 2.
3. ~~Ort: Startseite oder Fortschritt?~~ → Entscheidung 4.
4. ~~Gegen die ganze Vorwoche oder bis zum selben Tag?~~ → Entscheidung 3.

## Entscheidungen

Grundlage bleibt die **Entscheidung des Nutzers G-6 (2026-10-23)**: nur die eigene Vorwoche, keine Geschwister, keine
Ränge. Die folgenden Schnitt-Entscheidungen sind Stakeholder-Entscheidungen im Auftrag des Nutzers (Backlog-Lauf
2026-11-13, [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).

1. **Verdient = positive Münz-Buchungen außer `Manual`.** *Begründung:* Ein Kauf im Shop, ein Malus oder ein Geschenk
   des Vaters sagt nichts über das Lernen dieser Woche; sonst „sinkt" die Woche, weil das Kind etwas gekauft hat. Gems
   (Combo/Tempo/Missionen) bleiben draußen – eine Zahl, eine Währung, wie im Beispiel des Nutzers („340 Münzen").
   *Kosten:* Der Malus taucht im Vergleich nicht auf – er bleibt im Konto sichtbar.
2. **Woche Montag–Sonntag UTC**, über das vorhandene `WeekMonday`. *Begründung:* dieselbe Woche wie Wochenmissionen und
   Wochen-Pflichtziele. *Kosten:* Sonntagnacht lokal kann schon die neue Woche zählen (bekannter UTC-Fallstrick).
3. **Verglichen wird mit der Vorwoche bis zum selben Zeitpunkt** („letzte Woche bis jetzt"), die ganze Vorwoche steht
   klein daneben. *Begründung:* Am Montagmorgen gegen eine volle Vorwoche zu verlieren ist unfair und demotiviert – die
   Druck-Frage aus B-222. *Kosten:* ein zweites Feld.
4. **Ort: Tab „Erfolge", eine Zeile unter dem Punkte-Chip.** Neutral formuliert, ohne Rot und ohne „schlechter".
   *Begründung:* Die Startseite wird gerade umgebaut (B-525), „Erfolge" ist der Ort für Rückschau; Lob ja, Tadel nein.
   *Kosten:* einen Tipp tiefer als auf der Startseite.
5. **Neuer Endpunkt `GET student/me/points/week`** → `WeekCoinsResponse(DateOnly WeekStart, int ThisWeek,
   int LastWeekSoFar, int LastWeekTotal)`, nur `Roles.Student` wie der übrige `MeController`. Der Vater sieht die
   Buchungen ohnehin im Konto; ein Supervisor-Gegenstück ist nicht Teil dieser Story. *Kosten:* ein Endpunkt, eine
   Abfrage mit Zeitfenster.

## Akzeptanzkriterien

1. Uhr auf Mittwoch 12:00 UTC; Buchungen: Mo +30 `Base`, Di +20 `Goal`, Di −50 `ShopCoins`, Di +100 `Manual`,
   Di −10 `GoalPenalty`, Di +5 Gems-`Combo` → `thisWeek = 50`. *Rot, wenn* Shop, Malus, `Manual` oder Gems mitzählen.
2. Vorwoche: Mo +40, Mi 11:00 +10, Mi 13:00 +7, Sa +20 → `lastWeekSoFar = 50`, `lastWeekTotal = 77`. *Rot, wenn* der
   Schnitt nicht am selben Zeitpunkt der Vorwoche liegt.
3. `weekStart` ist der Montag der laufenden Woche; eine Buchung Sonntag 23:59 UTC zählt zur alten Woche. *Rot, wenn*
   die Woche am Sonntag beginnt.
4. Ein Supervisor-Token bekommt 403 (wie `points`). *Rot, wenn* das Rollen-Gate fehlt.
5. Fortschritt zeigt „Diese Woche 50 🪙 · letzte Woche bis jetzt 50"; bei weniger als letzte Woche kein Rot, kein
   Wort „schlechter"; sind beide Zahlen 0, entfällt die Zeile. *Rot, wenn* die Zeile fehlt oder bei 0/0 erscheint.

## Schätzung

**Größe: S** – ein Endpunkt mit einer gefilterten Summe, ein DTO, eine Zeile im Frontend. `wo: beides`,
`migration: nein`, `vertragsbruch: nein` (neuer Endpunkt, additiv).

**Risiken:** Die Summe über `CreatedAt` muss in SQLite serverseitig laufen (Fallstrick der Lernstand-Aggregation:
`DateTime`-Vergleich in SQL statt im Speicher). `DocsCaptureTests` braucht feste Uhr. Der Satz zur Druck-Frage gilt:
fühlt sich der Vergleich im Rollengang wie Druck an, wird die Zeile still – nicht verschärft.

**Angriffsplan:** (1) neue Datei `backend/Pugling.Api.Tests/WochenvergleichTests.cs` mit `_factory.Clock`, AK 1–4 rot;
(2) Endpunkt in `MeController`, `WeekMonday` als geteilter Helfer; (3) `pugling-reviewer`; (4) Client-Methode in
`Pugling.Client`, Frontend-API + Zeile in `SohnProgress.tsx`; (5) Vitest AK 5; (6) rote Probe; `frontend-reviewer`.

**Testweg:** `backend/Pugling.Api.Tests/WochenvergleichTests.cs` (AK 1–4); `frontend/src/sohn/SohnProgress.test.tsx`
(AK 5). Rollengang: Erfolge-Tab des Sohns nach einer Übungsrunde.

**Nicht in dieser Story:** Vergleich mit Geschwistern oder Rängen (G-6 ausgeschlossen), eine Ansicht für den Vater,
Verlauf über mehrere Wochen.

## Verlauf

- **2026-10-23** — angelegt (Quelle: Knowunity-Vergleich, Etappe 6 des Übernahmeplans).
- **2026-10-23** — **Entscheidung des Nutzers** in der Grill-Runde der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)) (G-6): das Kind vergleicht sich **nur mit der eigenen Vorwoche** („diese Woche 340 Münzen, letzte Woche 290“). Es gibt keinen Vergleich mit Geschwistern und keine Ränge. **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt am 2026-10-23 für alle an diesem Tag im Dialog entschiedenen Stories):** ein Nachtlauf darf diese Story auf Grundlage der Entscheidungen selbst ausformulieren, schätzen und bauen (Paket NP-21); trifft er auf eine Produktfrage, die die Entscheidungen nicht hergeben, hält er an.
- **2026-11-13** — ausformuliert (Tageslauf, Restzeit): Ist-Stand belegt, drei Schnitt-Fragen; die Entscheidung G-6 des Nutzers bleibt die Grundlage.
- **2026-11-13** — `gegrillt`: Schnitt-Entscheidungen 1–5 (positive Münzen ohne `Manual`, Woche Mo–So UTC, Vergleich bis zum selben Zeitpunkt, Ort „Erfolge", eigener Endpunkt); `art` von `Frage` auf `Wunsch` – die Frage hat G-6 beantwortet, übrig ist eine Fähigkeit. Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13, [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)); G-6 unverändert.
- **2026-11-13** — `geschaetzt`: S, beides, ohne Migration und Vertragsbruch.
- **2026-11-13** — `in-arbeit` (Sprint 72): neuer Endpunkt `GET student/me/points/week` (`MeController.PointsWeek`, nur `Roles.Student`) → `WeekCoinsResponse(WeekStart, ThisWeek, LastWeekSoFar, LastWeekTotal)`; Summen in SQL über `WalletService.WeekCoinsAsync` (positive Münzbuchungen ohne `Manual`, Woche Mo–So UTC über das jetzt öffentliche `GamificationService.WeekMonday`, Vorwoche geschnitten bei `now − 7 Tage`, obere Grenze `now` gegen künftig gestempelte Buchungen). Client `StudentApi.GetWeekCoinsAsync`, Frontend `api.weekCoins`, Zeile `WeekComparison` unter der Zusammenfassung im Tab „Erfolge" (neutral, entfällt bei 0/0, still bei Ladefehler; de/en). Tests: neue `WochenvergleichTests` (AK 1–4, jede mit eigener eingefrorener Uhr in einem anderen Jahr), `SohnProgress.test.tsx` „Erfolge, Wochenvergleich" (AK 5). AK 4 ist Absichtserklärung: zwei Wände (Rollen-Gate und fehlender `cid`-Claim) – die Probe misst M5 grün. Rote Probe:

  Gemessen an `36ebffe5 + Arbeitsbaum geändert`, 2026-11-13 23:44.

  | Mutante | Stelle | erwartet | gemessen | Anmerkung |
  |---|---|---|---|---|
  | M1 | backend/Pugling.Api/Services/Shared/WalletService.cs:47 | red | red | `Manual` zählt mit (AK 1) |
  | M2 | backend/Pugling.Api/Services/Shared/WalletService.cs:62 | red | red | Shop/Malus ziehen ab (AK 1) |
  | M3 | backend/Pugling.Api/Services/Shared/WalletService.cs:67 | red | red | Vorwoche tagesgenau statt zeitgenau geschnitten (AK 2) |
  | M4 | backend/Pugling.Api/Services/Shared/GamificationService.cs:172 | red | red | Woche beginnt am Sonntag (AK 3) |
  | M5 | backend/Pugling.Api/Controllers/Student/MeController.cs:21 | green | green | Rollen-Gate allein nicht tragend (AK 4, Absichtserklärung) |
  | M6 | backend/Pugling.Api/Services/Shared/WalletService.cs:65 | red | red | diese Woche ab Vorwochen-Montag (AK 1) |
  | M7 | frontend/src/sohn/SohnProgress.tsx:34 | red | red | Zeile auch bei 0/0 (AK 5) |
  | M8 | frontend/src/sohn/SohnProgress.tsx:62 | red | red | Zeile fehlt (AK 5) |

  Grundlinie nach dem Zurückspielen: green.
- **2026-11-14** — Code-Review-Fund behoben: eine Shop-Stornierung bucht ihre Rückerstattung als **positive** `ShopCoins`-Zeile (`ShopService.CancelPurchaseAsync`) und zählte damit als „verdient". `WalletService.EarnedCoinKinds` schließt jetzt neben `Manual` auch `ShopCoins` und `GoalPenalty` aus (beide können positiv gebucht werden: Rückerstattung bzw. Malus-Korrektur, keins ist Lernen). Test `WochenvergleichTests.Stornierung_Und_Malus_Korrektur_Zaehlen_Nicht`. Rote Probe:

  Gemessen an `ae9fb08a + Arbeitsbaum geändert`, 2026-11-14 00:40.

  | Mutante | Stelle | erwartet | gemessen | Anmerkung |
  |---|---|---|---|---|
  | BE1 | backend/Pugling.Api/Services/Shared/WalletService.cs:48 | red | red | `ShopCoins` wieder verdient |
  | BE2 | backend/Pugling.Api/Services/Shared/WalletService.cs:48 | red | red | `GoalPenalty` wieder verdient |

  Grundlinie nach dem Zurückspielen: green.
- **2026-11-13** — **abgenommen** mit Commit `ff6a08fb + fbd78e04` (Sprint 72, Backlog-Lauf 2026-11-13). Verifikation: Backend 1599/1599 grün (dotnet test Pugling.sln -c Release), Vitest 998/998, tsc -b sauber, Mutanten-Probe je AK-Fall rot (Tabelle oben). Review: Code-Review – Fund behoben in `fbd78e04`: eine Shop-Stornierung (positive `ShopCoins`-Zeile) und eine Malus-Korrektur zählten als „verdient“; jetzt ausgeschlossen, Test `Stornierung_Und_Malus_Korrektur_Zaehlen_Nicht`. Rollengang: volle Playwright-Suite gegen frisch gestartete Server auf Ausweichports (API 5216, Web 5189) nach dem letzten Edit: **145 passed** (alle Specs). Den Tab „Erfolge“ (Woche, Frist) fährt keine Spec eigens; er ist über RTL-Tests belegt – ein Handblick am Gerät bleibt für den Menschen.
- **2026-11-14** — Handblick durch E2E ersetzt: `frontend/e2e/erfolge-woche-frist.spec.ts` – 2 passed (zusammen mit `bonus-deckel-voll.spec.ts`, Ausweichports 5217/5190). Rote Probe: `<WeekComparison />` entfernt → rot an „Diese Woche … · letzte Woche bis jetzt 0“.
- **2026-11-14** — Nachschau Backlog-Lauf: ohne Fund. „Verdient“ = positive Buchungen der Münzarten außer `Manual`/`ShopCoins`/`GoalPenalty` (`backend/Pugling.Api/Services/Shared/WalletService.cs:47-48`), also `Base`, `Goal`, `ObjectiveCoins`, `DailyBoxCoins`, `ExtraTrainingCoins` – keine dieser Arten wird irgendwo negativ gebucht (Gegenbuchungen nur `ShopService.cs:159`, `PositionProgressService.cs:475`, Skins in Gems). Drei Summen in der DB mit UTC-Montag aus `GamificationService.WeekMonday` (`WalletService.cs:56-73`); Endpunkt nur mit Kind-Claim (`backend/Pugling.Api/Controllers/Student/MeController.cs:42-50`); Zeile entfällt bei 0/0 (`frontend/src/sohn/SohnProgress.tsx:34`).
