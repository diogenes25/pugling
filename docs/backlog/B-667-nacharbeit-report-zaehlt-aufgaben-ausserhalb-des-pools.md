---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [Nacharbeit 2/3 obwohl fertig]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: "PM-Sitzung 2026-11-18, Runde 4 – Nachschau von Sprint 86 (177027d9)"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-637]
---

# B-667 · Der Nacharbeits-Report zeigt Aufgaben außerhalb des Pools als „offen“ und zählt gegen alle Inhalte

## Beobachtung

Gefunden in der Nachschau von [B-637](B-637-nacharbeit-vater-sicht-je-aufgabe.md), am Code bestätigt (`fe13531c`): Seit
dem Review-Fund G2 liefert der Server für Zeilen jenseits des Pools `remediation: null`. `NacharbeitReport`
(`frontend/src/vater/PlanPositions.tsx` ~:1068–1090) zeigt trotzdem jede Zeile – ohne `doneAt` als „offen“ – und rechnet
im Kopf `{erledigt}/{report.totalItems}`.

**Szenario:** Position mit `itemCount = 2` bei 3 Inhalten, beide Aufgaben erledigt. Der Vater liest „Nacharbeit: 2/3
erledigt · geschafft am …“ und eine Zeile „offen“, die nie abgefragt wird. Widerspricht B-637 AK 1.

## User Story

Als *Vater* möchte ich im Nacharbeits-Report genau die Aufgaben sehen, die mein Kind nacharbeiten muss, damit eine fertige
Nacharbeit „2/2 erledigt“ heißt und keine Zeile „offen“ zeigt, die nie abgefragt wird.

## Ist-Stand am Code

Gelesen am 2026-11-18 auf `6b961cb5`, per Vitest nachgestellt. Der Server liefert `remediation` genau für
`item.Index < pool` (`backend/Pugling.Api/Services/Supervisor/PositionReportService.cs:50`, `:60`). `NacharbeitReport`
(`frontend/src/vater/PlanPositions.tsx:1069-1090` vor dem Fix) zählte `{erledigt}/{report.totalItems}` und zeigte jede
Zeile. Rote Probe gegen den alten Stand: Fall „zählt und zeigt nur die Aufgaben des Pools“ rot, Gegenprobe grün
(erwartet 1 rot / 1 grün, gemessen 1 failed / 1 passed).

## Die echte Lücke

Die Oberfläche kannte die Pool-Grenze nicht, die der Server seit Review-Fund G2 (Sprint 86) schon zieht.

## Offene Punkte

1. ~~Zeilen hinter dem Pool ausblenden oder als „nicht Teil der Nacharbeit“ zeigen?~~ → Entscheidung 1.

## Entscheidungen

Autonom gegrillt im Nachtlauf 2026-11-18 (`art: Defekt`).

1. **Ausblenden und gegen die Anzahl der Pool-Aufgaben zählen.** *Begründung:* der Report-Teil heißt „Nacharbeit“, die
   anderen Inhalte gehören nicht dazu; der Server markiert die Grenze schon (`remediation: null`). *Kosten:* der Vater
   sieht hier nicht mehr, dass die Übung mehr Inhalte hat – das zeigt die Positionszeile („Inhalte“).

## Akzeptanzkriterien

1. Zwei erledigte Pool-Aufgaben, eine Zeile mit `remediation: null` ⇒ „Nacharbeit: 2/2 erledigt“, kein „offen“, die
   Zeile hinter dem Pool fehlt. *Rot mit `report.totalItems` bzw. ohne Filter.*
2. Gegenprobe: eine offene Pool-Aufgabe bleibt „offen“ und zählt mit (1/2). *Rot, wenn auf `doneAt` statt auf
   `remediation` gefiltert wird.*

## Schätzung

**Größe: XS** — eine Filterzeile plus Vitest, kleiner als B-02. `migration: nein`, `vertragsbruch: nein`,
`wo: frontend`. Risiko: keins über die Komponente hinaus (einziger Aufrufer `PlanPositions.tsx:1033`).
Angriffsplan: Vitest rot, Filter, grün. Testweg: `PlanPositionsNacharbeit.test.tsx`, E2E
`nacharbeit-bis-erledigt.spec.ts` (Rollengang Vater), `frontend-reviewer`.

## Verlauf

- **2026-11-18** — angelegt aus der Nachschau von Sprint 86.
- **2026-11-18** — Nachtlauf: ausformuliert (rote Probe 1 failed / 1 passed gegen den alten Stand), autonom gegrillt
  (Entscheidung 1), geschätzt XS/frontend; in Arbeit.
- **2026-11-18** — AK 1 → `frontend/src/vater/PlanPositionsNacharbeit.test.tsx:20`, F1 (Filter entfernt, `PlanPositions.tsx:1088`) → rot · AK 2 → `:33` (Gegenprobe; trägt gegen Filtern auf `doneAt`, keine eigene Mutante). Gemessen an `6b961cb5 + Arbeitsbaum geändert`, 2026-11-18 22:37 (Skriptkopie mit Schreib-Wiederholung, siehe Retro).
- **2026-11-18** — Review `frontend-reviewer`: zu B-667 kein Fund (Server liefert `remediation` genau für `item.Index < pool`, `PositionReportService.cs:50,60`).
- **2026-11-18** — Verifikation nach der letzten Bearbeitung: `dotnet test Pugling.sln -c Release` **1882/1882**, Vitest **1249/1249**, `tsc -b` sauber, `dotnet format --verify-no-changes` ohne Fund außer Zeilenende-Artefakten an nicht berührten Zeilen; E2E `nacharbeit-bis-erledigt`, `nacharbeit`, `full-flow` **3 passed** (Ausweichports 5211/5181, Server von Playwright nach der letzten Bearbeitung gestartet).
- **2026-11-18** — Rollengang Vater: `frontend/e2e/nacharbeit-bis-erledigt.spec.ts:86` liest „Nacharbeit: 2/2 erledigt“ – passed.
- **2026-11-18** — abgenommen: `f5b04d26` (Sprint 93, Nachtlauf).
