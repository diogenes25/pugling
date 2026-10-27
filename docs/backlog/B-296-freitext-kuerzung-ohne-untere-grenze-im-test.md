---
tags: [typ/story, status/abgenommen, bereich/test, bereich/agent, rolle/creator]
aliases: [Freitext-Kürzung im Test ohne untere Grenze]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
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
- **2026-10-27** — in Lauf E, Teil 9 ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)) als `Defekt` unter Freigabe 1
  ausformuliert, geschätzt (XS, nur Tests) und gebaut: `Assert.Equal(1994, Grammar.Length)` + `EndsWith` statt
  `<= 2000`; zweiter Text (1000 × „ab“, Index 2000 ist ein Leerzeichen) in `vocabularyNotes` → Länge **2000**,
  `Shortened == ["grammar", "vocabularyNotes"]`, Rücklese gegen die Unit. Mutanten: `LastIndexOfAny` → `IndexOfAny`
  (Schnitt am ersten Leerzeichen) → **1 failed**; Leerraum-Zweig gelöscht (Ergebnis 1997 statt 2000) → **1 failed**.
  `Fill_*` **9/9**, Suite **971/971 `exit=0`**.
- **2026-10-27** — Review (`pugling-reviewer`, [Protokoll](../pm-sitzung-2026-10-27-nacht.md)): kein 🟡/🔴, Zähler
  bleibt 0. 🟢 ohne Zählung (Freigabe 10), beide genommen: `EndsWith("ab")` gestrichen (keine Mutante, die nur sie
  rot macht – die exakte Länge deckt sie); dritter Eingabetext (Leerzeichen an Index 1999 **und** 2000) als eigener
  Fall `Fill_Kuerzt_Freitext_Mit_Leerraum_Vor_Der_Grenze_Ohne_Nachlaufendes_Leerzeichen` → Länge **1999**, Mutante
  `TrimEnd()` im Leerraum-Zweig entfernt → **1 failed**. `Fill_*` **10/10**, Suite **972/972 `exit=0`**. Rollengang:
  der Agent hat keine Oberfläche, die Tests sind der Weg. **`abgenommen`.**
- **2026-10-27** — Commit `aab2ef9` (Lauf E, Teil 9; nicht gepusht).
