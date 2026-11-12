---
tags: [typ/story, status/abgenommen, bereich/werkzeug, rolle/creator]
aliases: [NoSubject Ausweg im Kreis, gebundenes Profil Reihe ohne Fach]
status: abgenommen
prio: P2
art: Defekt
groesse: "XS"
wo: "backend"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-tageslauf.md (Nachschau Sprint 48 in der Retro von Sprint 49)
unverifiziert: false
entgangen_bei: [B-508]
grund: ""
ersetzt_durch: []
---

# B-511 · Ein an eine Reihe ohne Fach gebundenes Profil schickt mit „--series" im Kreis

Nachschau von Sprint 48 (frischer `pugling-reviewer` an `7c17e07`, gelesen). Ein 🟡, ein 🟢.

1. 🟡 `ProfileFit.NoSubject` (B-508) rät „wähle mit --series eine Reihe mit Fach". Ist das mit `--profile` genannte Profil
   an genau diese fachlose Reihe gebunden (`SeriesId = X`, der Server erlaubt das), lehnt das Briefing es auf jeder
   anderen Reihe ab (`ProfileFit.Problem`: „lass --series weg") – zurück zum Anfang. Kein Test fuhr den Fall, dass das
   Profil die Reihe liefert: alle B-508-Tests gaben `--series` an.
2. 🟢 Der Hilfetext unter `--series` (`AgentCommands.cs`, `PrintUsage`) sagte „das Fach kommt aus dem Profil, sonst aus der
   Reihe" – umgekehrt zum Code (B-488) und seit B-508 falsch für die schreibenden Verben.

## Entscheidungen

Autonom gegrillt (`art: Defekt`, Tageslauf, Nutzerrolle übertragen):

1. **Hält ein genanntes Profil an dieser Reihe fest, nennt die Meldung ein anderes Profil statt `--series`.** Ein
   automatisch gewähltes Profil (kein `--profile`) hält nicht fest: mit `--series` wählt die Konsole dann gar kein Profil
   vom Buch. Kosten: ein zusätzlicher Parameter an `NoSubject`.
2. Hilfetext: „das Fach kommt aus der Reihe – create/plan/exam brauchen eine Reihe mit Fach, briefing zeigt sonst das des
   Profils".

## Akzeptanzkriterien

1. `create --profile <an fachlose eigene Reihe gebunden>` ohne `--series`: Fehlbedienung mit „hat kein Fach" und
   „--profile <id>", ohne „--series", kein Modellaufruf.

## Schätzung

**Größe: XS** — ein Parameter, ein Satzteil, ein Test.

## Verlauf

- **2026-11-13** — angelegt (Nachschau Sprint 48, Retro Sprint 49), autonom gegrillt (Defekt), geschätzt XS; im Sprint 50
  mit B-510 gebaut (dieselbe Senke).
- **2026-11-13** — gebaut (Sprint 50). `NoSubject(series, boundByNamedProfile)`; nach dem Review steht die Sperre für ein
  genanntes, an eine fachlose Reihe gebundenes Profil **vor** der Ortswahl (🟡: mit `--series` auf eine Reihe mit Fach lief
  der Aufruf sonst in „lass --series weg" des Briefings – Zähler 1 von 5). Der spätere Zweig an der Fachstelle war danach
  für schreibende Verben unerreichbar und ist entfernt. Hilfetext berichtigt.
  - AK 1 → `Gebundenes_Profil_an_Reihe_ohne_Fach_nennt_ein_anderes_Profil` (ohne `--series-unit`: das Profil liefert die
    Reihe); Q1 (Satz immer „--series") → rot, Q2 (Vorprüfung aus) → rot.
  - Review-Fall → `Gebundenes_Profil_an_Reihe_ohne_Fach_endet_auch_mit_series_an_der_Sperre`; Q2 → rot.
  - Verifikation: `dotnet test Pugling.sln -c Release` 1434/1434; Mutanten-Probe nach der letzten Änderung (Debug,
    gefiltert) 8/8 rot je am genannten Test, Grundlinie grün (Tabelle im Protokoll, Sprint 50/51).
  - Rollengang Creator: jeder AK als Konsolenaufruf gegen den echten In-Process-Server (Modell gefälscht). Vater, Sohn:
    Regressionszeugen (Suite grün, kein Pfad ihrer Ebenen im Diff). Abgenommen.
