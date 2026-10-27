---
tags: [typ/story, status/idee, bereich/agent, rolle/creator]
aliases: [Agent kürzt Themen still auf 200 Zeichen]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: Nachschau zu Lauf E Teil 5 (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-22]
wartet_auf: ""
---

# B-292 · Der `fill`-Agent kürzt ein zu langes Thema still auf 200 Zeichen – die API lehnt genau das bewusst ab

`UnitNotesFiller.Clamp` (`UnitNotesFiller.cs:138`) schneidet einen Themen-Eintrag des Entwurfs auf 200 Zeichen, während
`SeriesUnitsController.ValidateTopics` (`:229-230`) seit B-130 ausdrücklich **ablehnt statt zu kürzen**: „ein still
gekürztes Thema ist genau das, was der KI-Creator als gesetzten Stoff liest – ein halbes Thema würde still zu einem
falschen.“ Der Agent führt die stille Kürzung für dieselben Daten wieder ein, und im Schreib-Lauf steht der gekürzte
Entwurf erst **nach** dem PATCH auf der Konsole (`AgentCommands.FillAsync`). Dazu: `Distinct` läuft vor dem Schnitt
(zwei Themen, die sich erst nach Zeichen 200 unterscheiden, werden nach dem Kürzen Dubletten), und ein Schnitt bei
2000 kann ein Surrogat-Paar oder ein Wort teilen.

**Behebung (Vorschlag des Prüfers):** zu lange Themen aus dem Entwurf **streichen und melden** (Liste `Dropped` im
`UnitFillOutcome`, unter „Entwurf“ gedruckt) oder mit `AgentException` abbrechen und das Modell erneut fragen; den
Freitext-Schnitt bei 2000 (Spaltenlänge) behalten, aber ebenso melden und wortgerecht schneiden.

## Verlauf

- **2026-10-27** — angelegt aus der Nachschau zu Lauf E Teil 5 (`f033f70`) ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)),
  Fund 2 des frischen Prüfers; `entgangen_bei: B-22`.
