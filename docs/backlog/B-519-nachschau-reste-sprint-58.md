---
tags: [typ/story, status/abgenommen, bereich/werkzeug, rolle/creator]
aliases: [Nachschau-Reste Sprint 58, Herkunft der Reihe]
status: abgenommen
prio: P3
art: Defekt
groesse: "XS"
wo: "backend"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-tageslauf.md (Nachschau Sprint 58 in der Retro von Sprint 59/60)
unverifiziert: false
entgangen_bei: [B-516, B-482]
grund: ""
ersetzt_durch: []
nachgeschaut: 2026-11-13
---

# B-519 · Nachschau-Reste Sprint 58: die Meldung nennt eine Reihe, aber nicht, woher sie kommt

Nachschau von Sprint 58 (frischer `pugling-reviewer` an `022dfcf`, gelesen). Ein 🟡, zwei 🟢.

1. 🟡 `AgentCommands.cs`: „Reihe 7 hat kein Fach" für eine Reihe, die der Nutzer nie genannt hat – sie kam aus der
   Bindung des Profils bzw. dem Lehrbuch des Kindes. B-516 hatte den Profil-Teil gestrichen, ohne die Herkunft anders
   zu nennen.
2. 🟢 B-516 AK 1 („in keinem Zweig") wurde nur im Zweig „eigene Reihe" rot.
3. 🟢 `GrammarOrder`: ‚ und ‘ fehlten in der Trenner-Klasse.

## Entscheidungen

1. Die Aufrufstelle stellt die Herkunft voran: „Creator-Profil P ist an Reihe 7 gebunden: …", „Das Lehrbuch von Kind K
   steht auf Reihe 7: …". Kein Ausweg dazu (B-516 bleibt).
2. Die Tests der Zweige „fremd" und „Grundbestand" prüfen ebenfalls, dass kein `--series`/`--profile` steht.
3. ‚ und ‘ in die Trenner-Klasse.

## Verlauf

- **2026-11-13** — angelegt (Nachschau Sprint 58, Retro Sprint 59/60), autonom gegrillt, geschätzt XS; in Sprint 61 gebaut.
- **2026-11-13** — gebaut (Sprint 61). P1, P2 (Herkunft weg) → rot; Q1 (‚‘ weg) → rot; die Zweig-Tests prüfen jetzt
  `--series`/`--profile` in allen drei Zweigen. Review: 🟢 die Reihe steht in der Meldung doppelt – bewusst gelassen.
  - Verifikation: `dotnet test Pugling.sln -c Release` 1462/1462, Vitest 854/854, `tsc -b` sauber, Python-unittest grün,
    Playwright 140/140 auf 5201/5174 (vor den letzten Beschriftungs- und U+FEFF-Nachzügen, die Komponententests tragen);
    Mutanten nach der letzten Änderung rot (Tabellen im Protokoll, Sprints 60–62).
  - Rollengang: Vater (Plan-Seite, Assistent, Ziel-Wähler) per E2E und Komponententests; Creator (Konsole) gegen den
    In-Process-Server; Sohn Regressionszeuge. Abgenommen.
- **2026-11-13** — nachgeschaut (Sprint 63, frische `pugling-reviewer`/`frontend-reviewer` an `e37f65e`, gelesen, Vitest 102/102). Geprüft: CI-Schritt läuft (python3, Arbeitsverzeichnis, Pfade), xUnit-Leser findet die Liste, alle drei Leser stimmen Schritt für Schritt überein, `Map`-Aufrufer laden die Übung. Kein 🔴; Reste → [B-521](B-521-nachschau-reste-sprint-60-62.md).
