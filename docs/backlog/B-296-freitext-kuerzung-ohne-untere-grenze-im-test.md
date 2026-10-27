---
tags: [typ/story, status/idee, bereich/test, bereich/agent, rolle/creator]
aliases: [Freitext-Kürzung im Test ohne untere Grenze]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: Nachschau zu Lauf E Teil 7 (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-292]
wartet_auf: ""
---

# B-296 · Der Test zur Freitext-Kürzung hat keine untere Grenze – ein viel zu kurzer Schnitt bliebe grün

`Fill_Laesst_Zu_Lange_Themen_Weg_Und_Kuerzt_Freitext_Am_Wortende` (`CreatorAgentTests.cs`) sichert nur
`Length <= 2000 && EndsWith("Wortes")`. Eine Mutante, die am **ersten** Leerzeichen schneidet (sechs Zeichen übrig)
oder bei 1000, bleibt grün – genau die Art Verlust, um die es B-292 ging. Die erwartete Länge ist deterministisch
(285 Wörter × 6 + 284 Leerzeichen = 1994). Dazu ungedeckt: der Zweig „Grenze fällt auf Leerraum“
(`UnitNotesFiller.Clamp`), der ohne Test entfernt werden könnte (Ergebnis bliebe wortgerecht, nur ein Wort kürzer).

**Behebung:** `Assert.Equal(1994, …Length)` (oder `InRange(1990, 2000)`) zusätzlich zu `EndsWith`; ein zweiter
Eingabetext mit Leerzeichen genau an Index 2000 für den Leerraum-Zweig.

## Verlauf

- **2026-10-27** — angelegt aus der Nachschau zu Lauf E Teil 7 (`4a3fa61`) ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)),
  Fund 1 des frischen Prüfers; `entgangen_bei: B-292`.
- **2026-10-27** — Commit `1ee9bf0` (Lauf E, Teil 8; nicht gepusht).
