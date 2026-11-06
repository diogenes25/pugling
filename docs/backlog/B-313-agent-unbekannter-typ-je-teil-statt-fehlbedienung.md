---
tags: [typ/story, status/abgenommen, bereich/agent, rolle/creator]
aliases: [plan --types Tippfehler, unbekannter Übungstyp je Teil]
status: abgenommen
prio: P3
art: Frage
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf F Teil 5 in Lauf G, Teil 1 (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-313 · Soll ein unbekannter Übungstyp in `plan`/`exam` eine Fehlbedienung sein?

`plan --types Vocabulary,Clozee` erzeugt laut Code die gültigen Teile, legt einen Plan mit weniger Positionen an und
endet mit Exit 1: der unbekannte Typ wird **je Teil** gefangen (`CreatorPipeline.cs:26-30`, gefangen in
`PlanPlanner.cs:164` und `ExamPlanner.cs:79`). Der Commit von B-308 verspricht „Fehlbedienung = Exit 2“, und der
Kommentar in `PlanPlanner.cs:126` („All four checks come before the first model call“) zählt die Typ-Schlüssel nicht
mit. Im Review von B-308 wurde der Fall bewusst je Teil gelassen – es fehlt nur die festgehaltene Entscheidung.

**Die Frage an den Nutzer:** Tippfehler in `--types` vorab als Fehlbedienung abweisen (billig: `request.Types` gegen
`pipeline.SupportedTypes` vor dem Briefing prüfen), oder den Teil-Plan bewusst anlegen und nur melden?

## Entscheidungen

1. (aus dem Verlauf, im Auftrag) Ein unbekannter Typ in `--types` ist eine Fehlbedienung des ganzen Aufrufs, **vor** dem ersten
   Modellaufruf (Exit 2) – in `plan` **und** `exam`.
2. **(im Lauf ausformuliert)** Eine Prüfung an der Pipeline (`CreatorPipeline.EnsureSupported`), dieselbe Meldung wie beim einzelnen
   `create` (eine Fabrik `UnsupportedType`, keine zweite Formulierung); beide Planer rufen sie direkt nach „mindestens ein Typ".

## Akzeptanzkriterien

| Fall | Erwartet | Beleg | Mutante |
|---|---|---|---|
| `plan --types Vocabulary,Clozee` | Fehlbedienung mit „Clozee", **0** Modellaufrufe | `CreatorAgentTests.Plan_und_Klausur_mit_unbekanntem_Typ…(plan)` | Prüfung im `PlanPlanner` weg – gemessen: rot nur dieser Fall |
| `exam --types Vocabulary,Clozee --child 1` | dasselbe | `…(exam)` | Prüfung im `ExamPlanner` weg – gemessen: rot nur dieser Fall |

Der gültige Typ steht **zuerst**: eine Prüfung je Teil hätte für ihn das Modell schon gerufen, darum trägt `Calls == 0`.

## Verlauf

- **2026-10-27** — angelegt aus der Nachschau zu Lauf F, Teil 5 (`45cbe7b`), Fund 3 (🟢)
  ([Protokoll](../pm-sitzung-2026-10-27-nacht.md), Lauf G Teil 1). `Frage`: der Nachtlauf entscheidet das nicht.
- **2026-11-05** — **Entscheidung im Auftrag des Nutzers** (getroffen von Claude; Delegation im Dialog am 2026-11-05: „Wenn es etwas zu entscheiden gibt, übernimm meine Position und entscheide nach bestem Wissen und Gewissen“; [Protokoll](../pm-sitzung-2026-11-05-delegation.md)): Ein unbekannter Typ in `--types` wird vor dem ersten Modellaufruf als Fehlbedienung abgewiesen (Exit 2); kein Teil-Plan aus einem Tippfehler. **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt im Dialog am 2026-11-05 mit derselben Delegation):** ein Lauf darf diese Story auf Grundlage der Entscheidungen selbst ausformulieren, schätzen und bauen; trifft er auf eine Produktfrage, die sie nicht hergeben, hält er an.
- **2026-11-05** — ausformuliert, geschätzt und gebaut in Lauf AA, Sprint 17. **Rote Probe** gegen den Ausgangszustand: erwartet 2 rot,
  gemessen 2 rot („No exception was thrown"; der `exam`-Fall brauchte zuerst `--child`, sonst fiel er an einer anderen Fehlbedienung –
  berichtigt, dann nachgemessen). Mutanten je Aufrufstelle per Skript (Tabelle). `CreatorAgentTests` 85/85, volle Suite 1117/1117.
  `Pugling.Agent.Creator/CLAUDE.md` nennt die Regel.
- **2026-11-05** — Review `pugling-reviewer`: kein 🔴/🟡. Geprüft: Groß-/Kleinschreibung wie `CreateAsync`, Prüfung vor jedem Modellaufruf
  und Schreibzugriff (vor `DeriveTitleAsync`), keine Umgehung (ohne `--types` greifen feste, registrierte Vorgaben; `--dry-run` läuft durch
  denselben Pfad; `fill`/`profiles` nehmen kein `--types`). Zwei 🟢 kosmetisch (Kommentar nur im `ExamPlanner`, gleicher Theory-Parameter) –
  ohne Zählung zurückgestellt. **Zähler 0 von 5.**
- **2026-11-05** — **abgenommen.** Rollengang Creator (KI-Agent) als Integrationsfall über die echte Kommandozeile (`CommandLine.Parse` →
  `AgentCommands.RunAsync`) gegen den echten Host – das ist der Bedienweg des Agenten. Das Web (Vater/Sohn) berührt der Diff nicht; volle
  Suite 1117/1117 als Regression, keine E2E nötig (kein Laufzeit-Code im Web-Pfad). Commit: siehe nächster Eintrag.
