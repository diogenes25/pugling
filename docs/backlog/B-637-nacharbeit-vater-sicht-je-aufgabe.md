---
tags: [typ/story, status/abgenommen, bereich/klassenarbeit, bereich/auswertung, rolle/supervisor]
aliases: [Nacharbeit-Auswertung, Stufen je Aufgabe]
status: abgenommen
prio: P1
art: Wunsch
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: B-632
grund: ""
ersetzt_durch: []
nachgeschaut: 2026-11-18
---

# B-637 · Nacharbeit: der Vater sieht je Aufgabe Stufen und Erledigt-Stand

Teil E aus [B-632](B-632-schultest-nacharbeiten-mit-leichter-schalten.md). Leichter schalten kostet keine Münzen
(B-632, Entscheidung 4). Der Ausgleich ist, dass der Vater sieht, wie oft sein Sohn welche Hilfe genommen hat.

## User Story

Als *Vater* möchte ich je Aufgabe der Nacharbeit sehen, wie oft sie auf welcher Stufe gelöst wurde und ob sie schon
frei erledigt ist, damit ich weiß, wo mein Sohn noch Hilfe braucht.

## Ist-Stand am Code

Gelesen am 2026-11-17 (`b1bf80c8`):

- Die Auswertung je Position liefert der `PositionReportService`
  (`backend/Pugling.Api/Services/Supervisor/PositionReportService.cs`). Zähler je Stufe kennt er nicht.
- Die Zähler und den Erledigt-Stand speichert erst B-635.

## Die echte Lücke

Es gibt keine Sicht auf die Hilfe-Stufen je Aufgabe.

## Entscheidungen

Aus B-632, Entscheidung 4: keine Münzkosten fürs Leichterschalten, dafür ein sichtbarer Zähler für den Vater.

## Akzeptanzkriterien

1. Die Positionsauswertung einer Nacharbeit zeigt je Aufgabe:
   - die deutsche Vorgabe,
   - erledigt oder offen,
   - wie oft sie auf welcher Stufe gelöst wurde (Freitext, Buchstabenkästen, Buchstabensalat, Auswahl).
2. Die Sicht ist nur für den Supervisor des Kindes erreichbar (`ChildOwnershipFilter` bzw. `PlanOwnershipFilter`).
3. Die Auswertung gibt keine Lösungen an eine Rolle heraus, zu der `Student` gehört (Lösungsfeld-Regel).

## Schätzung

**S.** Die Daten liest B-635. Diese Story ergänzt die Auswertung um eine Projektion und eine Tabelle in der
Vater-Sicht.

- **`migration: nein`:** Die Daten legt B-635 an.
- **`vertragsbruch: nein`:** Die Report-DTOs werden additiv erweitert.
- **Abhängigkeit:** B-635.
- **Testweg:**
  - Integrationstest in `RemediationPositionTests`: Die Zähler stimmen nach einer Runde mit Hilfe.
  - Ownership-Test für ein fremdes Kind.
  - Vitest für die Vater-Tabelle.

## Verlauf

- **2026-11-17** — aus B-632 geteilt (Teil E), auf `geschaetzt` übernommen: Ist-Stand, Entscheidung und Schätzung
  stammen aus B-632.
- **2026-11-18** — gebaut (Sprint 86, [PM-Sitzung](../pm-sitzung-2026-11-18.md)). `ItemReport.Remediation`
  (`RemediationTaskReport`: Vorgabe, Erledigt-Tag, Lösungen je Stufe in Leiter-Reihenfolge), `Report.Remediation` und
  `CompletedOn`; Vater-Web: Nacharbeits-Tabelle im Positions-Report mit Stufennamen aus dem Manifest.
  - AK 1, Vorgabe / erledigt-offen / Stufen in Leiter-Reihenfolge → `Auswertung_zeigt_je_Aufgabe_Vorgabe_Erledigt_und_Stufen`
    (backend/Pugling.Api.Tests/RemediationPositionTests.cs:571), R1/R2/R3 → rot; ohne Nacharbeit kein Nacharbeitsteil → `Auswertung_ohne_Nacharbeit_ohne_Nacharbeitsteil`
    (backend/Pugling.Api.Tests/RemediationPositionTests.cs:603), R4 → rot; Darstellung `stufenZaehler` (PlanPositions.test.ts:511), F9.
  - AK 2, nur der Supervisor des Kindes → bestehender Fall `OwnershipTests.FremderVater_KannPlanNichtSehen_403` auf
    derselben Route (`…/report`, `PlanOwnershipFilter`); keine neue Route, kein neuer Filter.
  - AK 3, keine Lösungen an `Student` → Rolle des Controllers (`Roles.Supervisor`) unverändert; Kind-Token 403 in
    backend/Pugling.Api.Tests/RemediationPositionTests.cs:571.
  - Mutanten R1–R4 gemessen im Lauf von B-636 (gleicher Befehl, 09:03): alle red, Grundlinie green.
- **2026-11-18** — Review: Report-Zeilen außerhalb des Pools tragen keinen Nacharbeitsteil mehr (G2) →
  `Geschrumpfter_Pool_beendet_die_Nacharbeit_beim_Start`, R5 → rot; Stufennamen im Vater-Report aus dem Manifest im E2E
  belegt (frontend-reviewer 🟡). Endmessung: R1–R5 red (Lauf von B-636, 09:41).

  Frontend, gemessen an `7c642bb9 + Arbeitsbaum geändert`, 2026-11-18 09:52 (F5 an `10ef6195 + geändert`, 09:55);
  Vitest `src/vater/PlanPositions.test.ts src/sohn/pfad.test.ts src/sohn/SohnHome.test.tsx`, E2E
  `e2e/nacharbeit-bis-erledigt.spec.ts` auf 5211/5181:

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | F1 | sohn/pfad.ts:36 (`istTagespflicht`) | red | red |
  | F2 | sohn/pfad.ts:49 (nächster Knoten) | red | red |
  | F3 | sohn/SohnHome.tsx:363 (`belohnungsZiele`) | red | red |
  | F4 | sohn/SohnHome.tsx:350 (`zielZaehler`) | red | red |
  | F5 | vater/PlanPositions.tsx:43 (`cadencesFor`) | red | red (3. Form) |
  | F6 | vater/PlanPositions.tsx:45 (`withRemediation`) | red | red |
  | F7 | vater/PlanPositions.tsx:308 (Bonus laden) | red | red |
  | F8 | vater/PlanPositions.tsx:363 (Bonus senden) | red | red |
  | F9 | vater/PlanPositions.tsx:1069 (`stufenZaehler`) | red | red |
  | F10 | sohn/SohnHome.tsx:32 (`cadenceKey`) | red | red |
  | E1 | vater/PlanPositions.tsx:482 (Bonusfeld) | red | red |
  | E3 | vater/PlanPositions.tsx:1037 (Nacharbeits-Report) | red | red |
  | E4 | sohn/SohnPractice.tsx:297 (Bonus in der Ansage) | red | red |

  Grundlinie nach dem Zurückspielen: green. Der 2-s-Jubel selbst ist kein Beleg (aria-hidden, sensorisch) – der Bonus
  steht seit dem Review dauerhaft in Ansage, Blatt und Rundensumme (E4).
- **2026-11-18** — abgenommen (`177027d9`): Rollengang Vater maschinell (`nacharbeit-bis-erledigt.spec.ts`, Server nach der letzten Bearbeitung gestartet), Sohn und Creator als Regressionszeugen; Protokoll [PM-Sitzung 2026-11-18](../pm-sitzung-2026-11-18.md).
- **2026-11-18** — nachgeschaut (Sprint-87-Lauf, `pugling-reviewer` über `177027d9`): Durchrutscher [B-667](B-667-nacharbeit-report-zaehlt-aufgaben-ausserhalb-des-pools.md) (Report zählt Aufgaben außerhalb des Pools).
