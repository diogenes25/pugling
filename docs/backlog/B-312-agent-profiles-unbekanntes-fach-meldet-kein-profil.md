---
tags: [typ/story, status/abgenommen, bereich/agent, rolle/creator]
aliases: [profiles --subject unbekannt, falsche Kein-Profil-Meldung]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf F Teil 5 in Lauf G, Teil 1 (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-308]
wartet_auf: ""
nachgeschaut: "2026-10-28"
---

# B-312 · `profiles --subject <unbekannt>` endet mit Exit 0 und der Behauptung, es gebe kein Profil

`AgentCommands.cs` (um Zeile 189–193): mit einem Fach-Filter, der keinen Treffer hat – auch einer Fach-Id, die es
nicht gibt –, druckt der Agent „Es gibt noch kein Creator-Profil. Ohne Profil arbeitet der Agent als Generalist.“
und endet mit 0. Das ist falsch, sobald Profile existieren; die Listen-Route prüft die Fach-Id nicht
(`CreatorProfilesController.cs:41`).

**Vorschlag des Prüfers:** mit Filter „Kein Profil für Fach X“ melden, oder die Fach-Id vorher prüfen
(`ResolveSubjectIdAsync`-Muster → Exit 2).

`unverifiziert`: aus dem Code hergeleitet. Ob eine unbekannte Fach-Id eine Fehlbedienung sein soll, folgt aus der
Linie von B-301/B-307/B-308 (falsche Id → Exit 2); die Meldung bei bekanntem Fach ohne Profil ist reine Wortwahl.

## User Story

Als Creator, der `pugling-creator profiles --subject <id>` aufruft, möchte ich bei einer falschen Fach-Id eine
Fehlbedienung mit der Id hören – nicht die falsche Auskunft, es gebe noch gar kein Profil.

## Ist-Stand am Code

`AgentCommands.ShowProfilesAsync` (`AgentCommands.cs:189-193` vor dem Fix) rief `ListProfilesAsync(subjectId)`; die Route
prüft die Fach-Id nicht (`CreatorProfilesController.cs:41`), eine leere Liste druckte „Es gibt noch kein
Creator-Profil“ und endete mit 0. Mit `--child` lief der Wert ungeprüft an `profiles/match`.

## Akzeptanzkriterien

1. `profiles --subject <unbekannt>` endet mit einer Fehlbedienung, die „Fach <id>“ nennt – mit und ohne `--child`.
2. Ein bekanntes Fach ohne Profil nennt das Fach statt „noch kein Profil“.

## Entscheidungen

1. Unbekannte Fach-Id → Fehlbedienung (Exit 2), geprüft vor dem `--child`-Zweig – die Linie von B-301/B-307/B-308.
   Kosten: ein zusätzlicher `GET subjects/{id}` bei jedem Aufruf mit `--subject`.

## Schätzung

XS, `backend` (Agent), keine Migration, kein Vertragsbruch. Testweg: Theorie in `CreatorAgentTests` (mit/ohne `--child`).
AK 2 ist Konsolenausgabe und bleibt ungetestet (benannt).

## Verlauf

- **2026-10-27** — angelegt aus der Nachschau zu Lauf F, Teil 5 (`45cbe7b`), Fund 2 (🟢)
  ([Protokoll](../pm-sitzung-2026-10-27-nacht.md), Lauf G Teil 1).
- **2026-10-28** — in Lauf H, Teil 1 ([Protokoll](../pm-sitzung-2026-10-28-nacht.md)) ausformuliert, geschätzt (XS) und gebaut. Entscheidung (Freigabe 1, aus
  der Linie B-301/B-307/B-308): eine unbekannte Fach-Id ist eine Fehlbedienung (Exit 2), geprüft **vor** dem
  `--child`-Zweig. **Gemessen:** `Profile_fuer_ein_unbekanntes_Fach_sind_eine_Fehlbedienung` (Theorie, mit und ohne
  `--child 1`; Prämisse: ein Profil existiert) → **2 failed** („No exception was thrown“). Ein bekanntes Fach ohne Profil
  meldet jetzt „Kein Creator-Profil für Fach N (Name)“ – reine Wortwahl, ungetestet (Konsolenausgabe), im Review als 🟢
  benannt, ohne Zählung. Suite **1000/1000 `exit=0`**. Rollengang: Tests sind der Weg. **`abgenommen`.**
- **2026-10-28** — Commit `c195ef2` (Lauf H, Teil 1).
- **2026-10-28** — nachgeschaut (Lauf H, Teil 2, frischer `pugling-reviewer` am Commit `c195ef2`, Dateien im Stand des Commits): Fach-Prüfung vor dem `--child`-Zweig (`AgentCommands.cs:159`);
  Mutanten „Prüfung entfernt“ (beide Zeilen rot) und „Prüfung nach dem `--child`-Zweig“ (nur die zweite rot – genau dafür
  steht sie) hergeleitet; die übrigen Id-Parameter (`--profile`, `--child`, `--unit`, `--series-unit`) als validiert
  bestätigt. Kein Entgleiten.
