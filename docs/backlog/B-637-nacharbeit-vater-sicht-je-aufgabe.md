---
tags: [typ/story, status/geschaetzt, bereich/klassenarbeit, bereich/auswertung, rolle/supervisor]
aliases: [Nacharbeit-Auswertung, Stufen je Aufgabe]
status: geschaetzt
prio: P1
art: Wunsch
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: B-632
grund: ""
ersetzt_durch: []
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
