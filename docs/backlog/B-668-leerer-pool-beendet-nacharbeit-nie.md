---
tags: [typ/story, status/in-arbeit, bereich/gamification, rolle/student]
aliases: [Nacharbeit ohne Inhalte kostet täglich Malus]
status: in-arbeit
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: "PM-Sitzung 2026-11-18, Runde 4 – Nachschau von Sprint 86 (177027d9)"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-636]
---

# B-668 · Bei leerem Pool endet „täglich, bis erledigt“ nie, und der Malus läuft weiter

## Beobachtung

Aus der Nachschau von [B-636](B-636-pflicht-bis-erledigt-mit-abschlussbonus.md), am Code gelesen, nicht nachgestellt:
`LastTaskDoneOnAsync` liefert bei leerem Pool `null` (`PositionProgressService.cs`, `total == 0`), also stempelt weder der
Rundenstart noch der PATCH ein Ende; `RemediationDayMetAsync` verlangt eine nicht leere Runde. Leert der Creator die
Übung (oder fällt der Pool sonst auf 0), ist die Pflicht unerfüllbar und kostet jeden Tag Malus.

Verwandt, älter (B-635): eine beendete Nacharbeit, die der Vater zurück auf „Tagesziel“ stellt, lebt mit leerer Runde
wieder auf.

## User Story

Als *Sohn* möchte ich keine Nacharbeit bekommen, die ich nie beenden kann, damit sie mich nicht jeden Tag Münzen kostet.

## Ist-Stand am Code

Gelesen und per Test nachgestellt am 2026-11-18 auf `6b961cb5`.

- **Der Pool kann nachträglich nicht leer werden:** eine Übung in einem Plan darf ihre Liste nicht kürzen
  (`ExerciseControllerBase.UpdateConflictAsync`, `backend/Pugling.Api/Controllers/Creator/ExerciseControllerBase.cs:115-125`,
  B-328), und `ItemCount` 0 heißt „alle“ (`PositionPlayService.PoolSize`, `PositionPlayService.cs:262-263`).
- **Aber leer anfangen kann er:** ein Lückentext ohne Lücken wird angelegt – die Übungs-Validierung prüft nur
  Store-Schlüssel (`ClozeController.ValidateConfigAsync`, `backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs:358-366`;
  die Prüfung „At least one gap“ gilt nur für die separate Entität `ClozeText`). Die Nacharbeit prüfte nur die Typ-Fähigkeit
  (`PlanPositionsController.RemediationProblem`). Rote Probe: Anlegen 201, PATCH `remediation: true` erwartet 400, gemessen
  **200**.

## Die echte Lücke

Der einzige Eingang zu einem leeren Pool ist das Einschalten der Nacharbeit auf einer Übung ohne Aufgaben.

## Offene Punkte

1. ~~Bei leerem Pool ein Ende stempeln, den Tag als erfüllt werten, oder das Einschalten ablehnen?~~ → Entscheidung 1.

## Entscheidungen

Autonom gegrillt im Nachtlauf 2026-11-18 (`art: Defekt`).

1. **Das Einschalten der Nacharbeit wird abgelehnt** (`validation_error`, bei Anlegen und PATCH), wenn die Übung keine
   Aufgabe hat. *Begründung:* das ist der einzige Eingang (oben); „Tag erfüllt“ zahlte täglich Ziel-Punkte fürs Öffnen
   (die Invariante aus Sprint 86, Fund Y1), ein gestempeltes Ende sofort nach dem Einschalten wäre eine Nacharbeit, die es
   nie gab. *Kosten:* eine Zeile mehr in `RemediationProblem`; eine bestehende leere Nacharbeit (in der echten DB keine
   bekannt, die 11 stammen aus Klassenarbeiten mit Inhalt) bliebe – sie wird nicht nachträglich repariert.

## Akzeptanzkriterien

1. Lückentext ohne Lücken: PATCH `remediation: true` und POST mit `remediation: true, cadence: UntilDone` ⇒ 400
   `validation_error`. *Rot, wenn die Prüfung den Inhalt nicht liest.*

## Schätzung

**Größe: XS** — eine Bedingung an einer bestehenden Prüfung. `migration: nein`, `vertragsbruch: nein` (gleicher Code,
neuer Text), `wo: backend`. Testweg: `RemediationPositionTests.Nacharbeit_ohne_Aufgaben_wird_abgelehnt`,
`pugling-reviewer`. Rollengang: kein UI-Pfad (die Fehlermeldung kommt über das bestehende Banner).

## Verlauf

- **2026-11-18** — angelegt aus der Nachschau von Sprint 86 (`unverifiziert: true`).
- **2026-11-18** — Nachtlauf: am Code nachgestellt (rote Probe: PATCH 200 statt 400), autonom gegrillt (Entscheidung 1),
  geschätzt XS/backend; in Arbeit.
- **2026-11-18** — AK 1 → `backend/Pugling.Api.Tests/RemediationPositionTests.cs:892`, R1 (Inhaltsprüfung aus, `PlanPositionsController.cs:136`) → rot. Gemessen an `6b961cb5 + Arbeitsbaum geändert`, 2026-11-18 22:37 (Skriptkopie mit Schreib-Wiederholung, siehe Retro).
- **2026-11-18** — Review `pugling-reviewer` (Nachreview): kein Fund; kein anderer Weg legt eine Nacharbeit an (Seed, `WeakWordReviewService`, Agent über die API); 🟢 bestehende leere Nacharbeiten in der echten DB werden nicht repariert – in Entscheidung 1 als Kosten benannt.
- **2026-11-18** — Verifikation nach der letzten Bearbeitung: `dotnet test Pugling.sln -c Release` **1882/1882**, Vitest **1249/1249**, `tsc -b` sauber, `dotnet format --verify-no-changes` ohne Fund außer Zeilenende-Artefakten an nicht berührten Zeilen; E2E `nacharbeit-bis-erledigt`, `nacharbeit`, `full-flow` **3 passed** (Ausweichports 5211/5181, Server von Playwright nach der letzten Bearbeitung gestartet).
- **2026-11-18** — Rollengang: kein UI-Pfad (Meldung über das bestehende Banner); Ersatz der Integrationstest über den echten PATCH/POST.
