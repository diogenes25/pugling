---
tags: [typ/story, status/abgenommen, bereich/agent, rolle/creator]
aliases: [null-Element Vokabel Übersetzung Grammatik ungetestet]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf F Teil 4 in Lauf H, Teil 1 (docs/pm-sitzung-2026-10-28-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-306]
wartet_auf: ""
---

# B-318 · Ein `null`-Element in Vokabel-, Übersetzungs- und Grammatik-Entwürfen hält kein Test

[B-306](B-306-null-element-in-uebungsstrategien.md) hat die vier Übungsstrategien des KI-Creators `null`-sicher gemacht,
aber nur den Lückentext getestet. In `VocabularyStrategy`, `TranslationStrategy` und `GrammarStrategy` hängt die
Sicherheit an `item?.Front` / `item?.Source` / `task?.Prompt` in der Regelschleife. Die Mutante „`?.` → `.`“ bliebe grün,
und ein Modellentwurf mit einem `null`-Element endete wieder in einer rohen `NullReferenceException` statt in einer
Reparatur-Runde.

## User Story

Als Entwickler möchte ich, dass ein Test fällt, wenn eine Übungsstrategie wieder an einem `null`-Element des
Modellentwurfs abstürzt – damit B-306 nicht nur für den Lückentext belegt ist.

## Ist-Stand am Code

`VocabularyStrategy.cs:44-51`, `TranslationStrategy.cs:42-49`, `GrammarStrategy.cs:43-51`: `item?.…`/`task?.…` in der
Regelschleife, kein Fall mit einem `null`-Element (nur `ClozeStrategy` hat ihn).

## Akzeptanzkriterien

1. Je Typ (Vokabel, Übersetzung, Grammatik) ist ein `null`-Element eine Regelverletzung mit seiner Position; nichts
   landet im Katalog.

## Entscheidungen

1. Eine Theorie über die drei Typen statt drei Fälle – derselbe Weg, drei Daten. Kosten: keine.

## Schätzung

XS, `backend` (Tests), keine Migration, kein Vertragsbruch. Testweg: Mutante je Strategie.

## Verlauf

- **2026-10-28** — angelegt aus der Nachschau zu B-306 (Commit `d8e4a0a`, frischer `pugling-reviewer`, 🟢) in Lauf H,
  Teil 1 ([Protokoll](../pm-sitzung-2026-10-28-nacht.md)); als `Defekt` unter Freigabe 1 im selben Sprint ausformuliert,
  geschätzt (XS) und gebaut. Neuer Fall `Ein_Null_Element_ist_eine_Regelverletzung_statt_eines_Absturzes` (Theorie über
  die drei Typen, je ein `null` an Position 3; erwartet: Entwurf abgelehnt, Verstoß beginnt mit „Vokabel 3“/„Satz 3“/
  „Aufgabe 3“, nichts im Katalog). Rote Probe gegen den Ausgangsstand nicht möglich (der Code war schon richtig, es
  fehlte nur der Fall); stattdessen Mutanten `item!.Front`, `item!.Source`, `task!.Prompt` → je **1 failed**, Kontrolle
  **5 passed**. Suite **1000/1000 `exit=0`**. Rollengang: Agent ohne Oberfläche, Tests sind der Weg. **`abgenommen`.**
- **2026-10-28** — Commit `c195ef2` (Lauf H, Teil 1).
