---
tags: [typ/story, status/abgenommen, bereich/werkzeug, rolle/creator]
aliases: [NoSubject Profil an anderer Reihe, writes ungetestet]
status: abgenommen
prio: P2
art: Defekt
groesse: "XS"
wo: "backend"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-tageslauf.md (Nachschau Sprint 50/51 in der Retro von Sprint 52/53)
unverifiziert: false
entgangen_bei: [B-511]
grund: ""
ersetzt_durch: []
---

# B-514 · „--series" als Ausweg für ein Profil, das an eine andere Reihe gebunden ist

Nachschau von Sprint 50/51 (frischer `pugling-reviewer` an `2917449` + `82fdc4c`, gelesen). Zwei 🟡, ein 🟢.

1. 🟡 `create --profile P --series X --series-unit U`, P an Reihe Y (mit Fach) gebunden, X ohne Fach: `NoSubject(X)` rät
   „wähle mit --series eine Reihe mit Fach" – jede Reihe außer Y endet am Briefing („gehört zur Reihe Y … lass --series
   weg"). B-511 hatte nur das an X selbst gebundene Profil abgefangen. Kein Test fuhr ein an eine *andere* Reihe
   gebundenes Profil.
2. 🟡 `writes &&` in der Vorprüfung von B-511 (`AgentCommands.cs:498`): ohne die Bedingung endete `briefing --profile
   <gebunden an Reihe ohne Fach>` an der Fehlbedienung – kein Test.
3. 🟢 Der Test zu B-510 AK 2 war schon vor B-510 grün; sein Kommentar sagt das jetzt.

## Entscheidungen

Autonom gegrillt (`art: Defekt`, Tageslauf):

1. **`NoSubject` bekommt das genannte Profil** (ein automatisch gewähltes bindet nichts) und nennt je Fall **einen** Schritt,
   der allein durch beide Sperren führt (nach Review des ersten Entwurfs): kein Profil → „--series mit Fach"; Profil ohne
   Reihe → „--series eine Reihe in <Fach des Profils>"; an diese Reihe gebunden → „ohne --series ein Profil, das an eine
   Reihe mit Fach gebunden ist"; an eine andere Reihe Y → „lass --series/--series-unit weg, das Profil gehört zu Reihe Y".
2. Test für `briefing` mit dem gebundenen Profil (Exit 0).

## Akzeptanzkriterien

1. Profil an Y, `--series X` (ohne Fach): die Meldung nennt Reihe Y und rät nicht zu „wähle mit --series".
2. `briefing --profile <an fachlose Reihe gebunden>`: Exit 0.

## Verlauf

- **2026-11-13** — angelegt (Nachschau Sprint 50/51, Retro Sprint 52/53), autonom gegrillt, geschätzt XS; im Sprint 55 gebaut.
- **2026-11-13** — gebaut (Sprint 55). Review des ersten Entwurfs: 🟡 „oder wähle ein anderes Profil" ließ den Nutzer auf
  derselben fachlosen Reihe (Zähler 1); zwei 🟢 (zweischrittige Auswege) – alle drei durch Entscheidung 1 in der neuen
  Fassung behoben. Neue Regel aus der Retro (pm-loop Step 5): zwei Tests **führen den Rat aus** – das deckte sofort einen
  Fehler im Testaufbau auf (Profil mit fremdem Fach an seiner Reihe).
  - AK 1 → `Fremd_gebundenes_Profil_auf_Reihe_ohne_Fach_raet_nicht_zu_einer_anderen_Reihe` (inkl. Rat ausgeführt → Exit 0);
    V1 (genanntes Profil nicht weitergereicht) → rot.
  - AK 2 → `Briefing_mit_gebundenem_Profil_auf_Reihe_ohne_Fach_laeuft`; V3 (`writes` weg) → rot.
  - Zweig „an diese Reihe" → B-511-Test (Rat ausgeführt → Exit 0); V4 → rot. Zweig „ohne Reihe" → Theorie aus B-508;
    V5 → rot.
  - Verifikation: Suite 1438/1438, Mutanten 4/4 nach der letzten Änderung (Release, gefiltert).
  - Rollengang Creator: Konsolenaufrufe gegen den echten In-Process-Server; Vater, Sohn Regressionszeugen. Abgenommen.
