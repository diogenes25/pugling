---
tags: [typ/story, status/abgenommen, bereich/gamification, rolle/student]
aliases: []
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: "beides"
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-26.md#retrospektive
grund: ""
ersetzt_durch: []
entgangen_bei: [B-593]
nachgeschaut: ""
---

# B-1026 · Sparziel: zu weiter Fang machte Serverfehler zum 404, Vorlesename ohne den sichtbaren Text

Fund im Merge-Review der Läufe M/N/O (2026-11-26) über die abgenommene Arbeit von B-593: `SavingsGoalService.SetAsync` fing
jede Datenbank-Ausnahme als „unbekanntes Angebot“ (404) – auch eine belegte oder defekte Datenbank; der Knopf „★ Mein Ziel“
trug einen Vorlesenamen ohne seinen sichtbaren Text (WCAG 2.5.3); die Prognose im Kind-Hub klang wertend („bei seinem Tempo“).
Behoben in `5446640c` (nur `SQLITE_CONSTRAINT` wird 404, Name „Als Ziel: …“/„Mein Ziel: …“, „im bisherigen Tempo“). Nachträglich
als Story abgelegt in der Retrospektive von Sprint 192.

## Verlauf

- **2026-11-26** — angelegt und `abgenommen` mit Commit `5446640c` (E2E prüft den angehefteten Namen; Suite am Endstand
  `4e70c583` 2609/2609); Rollengang: E2E `sparziel` lief im Merge-Review grün (B-1020 Verlauf).
