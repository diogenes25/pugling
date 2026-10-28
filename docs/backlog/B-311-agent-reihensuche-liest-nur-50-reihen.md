---
tags: [typ/story, status/abgenommen, bereich/agent, rolle/creator]
aliases: [Reihensuche des Agenten nur erste Seite, ResolveLocationAsync take 50]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf F Teil 5 in Lauf G, Teil 1 (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-307, B-308]
wartet_auf: ""
---

# B-311 · Der Agent sucht Reihe und Unit nur in den ersten 50 Reihen

`AgentCommands.ResolveLocationAsync` (`backend/Pugling.Agent.Creator/AgentCommands.cs`, um Zeile 455) ruft
`creator.ListSeriesAsync(ct: ct)` ohne Paging; der Client nimmt dann `take = 50` (`Pugling.Client/CreatorApi.cs`).
Wer nur `--series X` oder nur `--series-unit Y` angibt, wird bei einer gültigen Reihe/Unit jenseits der ersten 50 mit
einer **falschen Fehlbedienung (Exit 2)** abgewiesen („Es gibt keine Reihe mit Unit …“ bzw. „Unit Y gibt es nicht …
in keiner Reihe“). Der Prüfer nennt es dieselbe Paging-Falle wie früher bei den Interessen-Tags (Bezug nicht
geprüft).
Nebenbei: eine unbekannte `--series 987654` ohne Unit bekommt die falsche Diagnose statt ihrer Id.

**Vorschlag des Prüfers:** mit `--series` erst `GetSeriesAsync(id)` (404 → Fehlbedienung wie
`ResolveSubjectIdAsync`), dann `ListUnitsAsync`; die Reihenliste nur ohne `--series` und dann über alle Seiten.

`unverifiziert`: aus dem Code hergeleitet, nicht gemessen. Erster Schritt beim Ausformulieren: ein Test mit 51 Reihen
(die Test-DB hat durch `FreshUnitAsync` ohnehin mehr als 50) – rot, wenn die Behauptung stimmt.

## User Story

Als Creator, der den Agenten mit `--series` **oder** `--series-unit` allein aufruft, möchte ich, dass jede gültige Reihe
und Unit gefunden wird, egal wie groß der Katalog ist – damit eine richtige Id nicht als Tippfehler abgewiesen wird.

## Ist-Stand am Code

`AgentCommands.ResolveLocationAsync` (`backend/Pugling.Agent.Creator/AgentCommands.cs:455` vor dem Fix) las
`creator.ListSeriesAsync(ct: ct)` mit dem Client-Vorgabewert `take = 50` (`Pugling.Client/CreatorApi.cs:75`); der Server
sortiert nach Name, dann Id (`TextbookSeriesController.cs:93`). Eine Reihe jenseits von Platz 50 fiel durch, ebenso jede
Unit darin; eine unbekannte `--series` ohne Unit bekam „Es gibt keine Reihe mit Unit“ statt ihrer Id.

## Akzeptanzkriterien

1. `--series <id>` allein findet eine gültige Reihe jenseits der ersten Seite und nimmt ihre erste Unit.
2. `--series-unit <id>` allein findet eine gültige Unit in einer Reihe jenseits der ersten Seite.
3. Eine unbekannte `--series` ohne Unit ist eine Fehlbedienung, die die Id nennt.

## Entscheidungen

1. Eine genannte Reihe wird per Id geholt, nicht in einer Liste gesucht (Vorschlag des Prüfers). Kosten: ein Aufruf
   mehr bei `ResolveSubjectIdAsync`, der dieselbe Reihe noch einmal liest – vernachlässigbar für eine Konsolen-App.
2. Ohne `--series` werden alle Seiten gelesen, Seitengröße 50. Kosten: bei großem Katalog mehrere Aufrufe; die
   Unit-Suche ruft ohnehin je Reihe `ListUnitsAsync`, das überwiegt.

## Schätzung

XS–S, nur `backend` (Agent), keine Migration, kein Vertragsbruch. Testweg: Integrationstest in `CreatorAgentTests` mit
51 Reihen in der geteilten Test-DB.

## Verlauf

- **2026-10-27** — angelegt aus der Nachschau zu Lauf F, Teil 5 (`45cbe7b`), Fund 1 (🟡) des frischen
  `pugling-reviewer` ([Protokoll](../pm-sitzung-2026-10-27-nacht.md), Lauf G Teil 1). Geschwister außerhalb des Diffs:
  B-308 versprach „falsche Id → genau eine Fehlbedienung“, hier wird eine **richtige** Id als falsch abgewiesen.
- **2026-10-28** — in Lauf H, Teil 1 ([Protokoll](../pm-sitzung-2026-10-28-nacht.md)) als `Defekt` unter Freigabe 1 ausformuliert, geschätzt (S) und
  gebaut. **Gemessen:** `Reihe_und_Unit_jenseits_der_ersten_Seite_werden_gefunden` (50 Reihen „aaa …“ vor der Ziel-Reihe
  „zzz …“, die Prämisse „nicht auf Seite 1“ ist zugesichert) → **1 failed** an der ersten Zeile („Es gibt keine Reihe
  mit Unit“, `AgentCommands.cs:473`); `Eine_unbekannte_Reihe_ohne_Unit_wird_mit_ihrer_Id_gemeldet` → **1 failed** (falsche
  Diagnose statt „Reihe 987654“). Fix: eine genannte Reihe wird per Id geholt (`GetSeriesOrUsageErrorAsync`, auch von
  `ResolveSubjectIdAsync` genutzt), ohne `--series` blättert `ListAllSeriesAsync` alle Seiten (Seitengröße 50, damit der
  51-Reihen-Fall die Schleife trägt – bei 200 bliebe die Mutante „eine Seite“ grün, vor dem Review selbst erkannt).
  Mutante „eine Seite“ → **1 failed**, Kontrolle grün. Review (`pugling-reviewer`): korrekt; 🟢 „der Fall prüft nur,
  dass nichts wirft, nicht welche Unit“ → zurückgestellt (ein Abgriff von `Console.Out` ist prozessweit und vertrüge sich
  nicht mit parallelen Testklassen), ohne Zählung. Die Nachschau zu B-307 (`d8e4a0a`) fand denselben Fehler ein zweites
  Mal, darum `entgangen_bei` auch B-307. Suite **1000/1000 `exit=0`**. Rollengang: Agent ohne Oberfläche, Tests sind der
  Weg; Vater und Sohn Regressionszeugen (Suite grün, kein Pfad berührt). **`abgenommen`.**
- **2026-10-28** — Commit `c195ef2` (Lauf H, Teil 1).
