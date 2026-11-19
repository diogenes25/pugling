---
tags: [typ/story, status/abgenommen, bereich/gamification, rolle/student, rolle/supervisor]
aliases: [Beendete Nacharbeit zurückgestellt kostet Malus]
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: "Nachtlauf 2026-11-18, Review B-666 (pugling-reviewer, Fund 2) – docs/pm-sitzung-2026-11-18.md, Runde 5"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-636]
nachgeschaut: 2026-11-19
---

# B-669 · Eine beendete Nacharbeit, zurück auf Tagesziel gestellt, kostet Malus für die Tage seit ihrem Ende

Gefunden im Review von B-666, außerhalb dessen Diff (Code aus B-636): Eine „bis erledigt“-Nacharbeit endet mit
`CompletedOn`; danach hat sie keine Pflicht, die Abrechnung bucht für diese Tage nichts. Stellt der Vater sie später auf
„Tagesziel“ zurück, bleibt `DutySince` unverändert (nur Kür→Takt setzt es, B-613), und die nächste Abrechnung wertet jeden
Tag seit dem Start nach der Tagesregel – bis zu 14 Tage Malus für Tage ohne Pflicht.

## User Story

Als *Sohn* möchte ich für die Tage nach meiner geschafften Nacharbeit nichts zahlen, auch wenn Papa sie später wieder als
Tagesziel einstellt. Als *Vater* möchte ich eine fertige Nacharbeit wieder einschalten können, ohne rückwirkend zu strafen.

## Ist-Stand am Code

Gelesen und per Test nachgestellt am 2026-11-18 (Arbeitsstand nach B-666). `PlanPositionsController.Patch` setzte
`DutySince` nur bei Kür → Takt (`backend/Pugling.Api/Controllers/Supervisor/PlanPositionsController.cs:350-352` vor dem
Fix). `SettleClosedPeriodsAsync` beginnt bei `FirstDueDay` = max(`DutyBegins`, Planstart). Rote Probe (zurückdatiert:
Start vor 6 Tagen, Ende vor 4 Tagen, heute zurück auf Tagesziel): Malus-Tage vor heute erwartet keine, gemessen sechs
(13.–17.11. und weitere, Ausgabe von xUnit gekürzt).

## Die echte Lücke

Eine beendete „bis erledigt“-Position ist pflichtlos wie eine Kür-Position; ihr Wechsel zurück in einen Takt ist also eine
**neue** Pflicht und muss wie B-613 ab heute zählen.

## Offene Punkte

1. ~~`DutySince` setzen oder `CompletedOn` löschen?~~ → Entscheidung 1.

## Entscheidungen

Autonom gegrillt im Nachtlauf 2026-11-18 (`art: Defekt`).

1. **`DutySince = heute`, wenn eine beendete „bis erledigt“-Position in einen anderen Takt wechselt; `CompletedOn` bleibt.**
   *Begründung:* dieselbe Regel wie B-613 (pflichtlos → Pflicht zählt ab heute); `CompletedOn` ist zugleich die Anzeige
   „geschafft am“ und das „Ende ist endgültig“ aus B-636. *Kosten:* nie abgerechnete Tage *vor* dem Ende (das Kind loggte
   sich nicht ein) werden nicht mehr nachgeholt – zugunsten des Kindes; die Serie kann dadurch nur steigen (wie B-613
   Risiko 8).

## Akzeptanzkriterien

1. Beendete Nacharbeit, Ende vor 4 Tagen, heute zurück auf Tagesziel ⇒ kein Malus für einen Tag vor heute. *Rot, wenn der
   Wechsel das alte Pflicht-Datum behält.*

## Schätzung

**Größe: XS** — eine Bedingung an der B-613-Zeile. `migration: nein`, `vertragsbruch: nein`, `wo: backend`. Risiko: die
Bedingung darf Daily ↔ Weekly nicht mitnehmen (B-613 hat das ausdrücklich ausgenommen). Angriffsplan: roter Fall, eine
Bedingung, grün. Testweg: `RemediationPositionTests.Beendete_Nacharbeit_zurueck_auf_Tagesziel_bestraft_keine_Tage_davor`,
`pugling-reviewer`. Rollengang: kein UI-Pfad (das Formular schickt denselben PATCH); Ersatz der Integrationstest.

## Verlauf

- **2026-11-18** — angelegt aus dem Review von B-666 (Fund außerhalb des Diffs, zählt nicht auf den Nachtlauf-Zähler);
  im selben Sprint als Defekt bearbeitet (Nachtlauf-Freigabe 3): ausformuliert, rote Probe (sechs Tage statt keiner),
  autonom gegrillt, geschätzt XS/backend; in Arbeit.
- **2026-11-18** — AK 1 → `backend/Pugling.Api.Tests/RemediationPositionTests.cs:857`, D1 (Bedingung aus, `PlanPositionsController.cs:359`) → rot. Gemessen an `6b961cb5 + Arbeitsbaum geändert`, 2026-11-18 22:37 (Skriptkopie mit Schreib-Wiederholung, siehe Retro).
- **2026-11-18** — Review `pugling-reviewer` (Nachreview): kein 🔴/🟡; kein Konflikt mit `StampFinishedRemediationAsync` im selben PATCH; 🟢 auch Tage *vor* dem Ende verlassen die Historie (zugunsten des Kindes) → Satz im Kommentar ergänzt (ohne Zählung).
- **2026-11-18** — Verifikation nach der letzten Bearbeitung: `dotnet test Pugling.sln -c Release` **1882/1882**, Vitest **1249/1249**, `tsc -b` sauber, `dotnet format --verify-no-changes` ohne Fund außer Zeilenende-Artefakten an nicht berührten Zeilen; E2E `nacharbeit-bis-erledigt`, `nacharbeit`, `full-flow` **3 passed** (Ausweichports 5211/5181, Server von Playwright nach der letzten Bearbeitung gestartet).
- **2026-11-18** — Rollengang: kein UI-Pfad; Ersatz der Integrationstest über den echten PATCH und die Abrechnung.
- **2026-11-18** — abgenommen: `f5b04d26` (Sprint 93, Nachtlauf).
- **2026-11-19** — nachgeschaut (PM-Sitzung 2026-11-19, Nachschau über `f5b04d26` bzw. `796314db`/`8f3d12db`): ohne Befund: `DutySince = heute` nur beim Wechsel einer beendeten Nacharbeit auf Tag/Woche; zurück auf „bis erledigt“ bleibt beendet.
