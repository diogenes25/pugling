---
tags: [typ/story, status/abgenommen, bereich/test, bereich/agent, rolle/creator]
aliases: [Grenzprüfung nur mit Leerzeichen belegt, B-302 Gegenrichtung]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf F Teil 3 (docs/pm-sitzung-2026-10-27-nacht.md, Lauf F Teil 5)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-302]
wartet_auf: ""
nachgeschaut: "2026-10-27"
---

# B-309 · „Dieselbe Leerraum-Menge an jeder Stelle“ ist nur in einer Richtung belegt

[B-302](B-302-fill-wortgrenze-kennt-nur-drei-leerzeichen.md) hat die Wortgrenzen-Suche auf `char.IsWhiteSpace`
gezogen und mit NBSP/`\r`/U+2003 **vor** der Grenze belegt. Die andere Stelle – Leerraum genau **auf** der Grenze
(`UnitNotesFiller.Clamp`, `char.IsWhiteSpace(trimmed[2000])`) – prüfen alle Tests nur mit einem Leerzeichen. Mutante
(in der Nachschau hergeleitet, hier gemessen): `trimmed[2000] == ' '` → alle `Fill_*` grün, und ein NBSP genau auf
der Grenze kostet ein ganzes Wort ohne Not.

**Behebung:** ein `Theory` mit den drei Trennern auf Index 1990 und 2000 → Länge 2000; die Mutante muss je Fall rot
werden (1990).

## Verlauf

- **2026-10-27** — angelegt aus der Nachschau zu Lauf F Teil 3 (`193f4f6`) in Lauf F, Teil 5
  ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)), Fund 1 (🟢) des frischen Prüfers; `entgangen_bei: B-302` – das
  Sprint-Ziel versprach „jede Leerraum-Art“, belegt war eine Richtung. Als `Defekt` unter Freigabe 1 ausformuliert,
  geschätzt (XS, nur Test) und in den laufenden Sprint genommen.
- **2026-10-27** — gebaut: `Fill_Behaelt_Das_Letzte_Wort_Wenn_Jede_Art_Von_Leerraum_Auf_Der_Grenze_Liegt` (1990 × x,
  Trenner, 9 × y, Trenner auf 2000, 10 × z) → **3 passed** am echten Code (die Lücke war der Test). Mutante
  `trimmed[2000] == ' '` → **3 failed**, je gemessen **1990** statt 2000. Suite **990/990 `exit=0`**.
- **2026-10-27** — Review (`pugling-reviewer`, zusammen mit B-308): Arithmetik nachgerechnet (2011 Zeichen, Trenner
  auf 1990 und 2000; richtig 2000, Mutante 1990); `char.IsSeparator` als Mutante träfe nur den `\r`-Fall, das Streichen
  der Grenzprüfung alle drei. Kein Fund. Rollengang: Agent ohne Oberfläche, Tests sind der Weg. **`abgenommen`.**
- **2026-10-27** — nachgeschaut (Lauf G, Teil 1, frischer `pugling-reviewer` am Commit `45cbe7b`): 1990 + 1 + 9 + 1
  + 10 = 2011 nachgerechnet, Trenner auf 1990/2000; Mutanten `== ' '` (rot ×3, 1990), `char.IsSeparator` (rot nur
  für `\r`), `[2000 ± 1]` (rot, 1990) hergeleitet; Geschwister `vocabularyNotes` (selbe `Text()`-Funktion),
  `TrimEnd` im Grenzzweig (Test bei 1999) und `Trim()` in `UnitNotesFiller.cs:173` gedeckt. Kein Befund.
