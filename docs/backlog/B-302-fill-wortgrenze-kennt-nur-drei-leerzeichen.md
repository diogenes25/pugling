---
tags: [typ/story, status/abgenommen, bereich/agent, rolle/creator]
aliases: [Wortgrenze mit CR oder NBSP, Clamp Leerraum-Mengen]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Review zu B-300/B-301 in Lauf F, Teil 2 (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-292]
wartet_auf: ""
nachgeschaut: "2026-10-27"
---

# B-302 · `fill` schneidet Freitext mitten im Wort, wenn die Wörter nur durch `\r`, NBSP o. Ä. getrennt sind

`UnitNotesFiller.Clamp` prüft die Grenze selbst mit `char.IsWhiteSpace`, sucht die letzte Wortgrenze davor aber nur
nach `' '`, `'\n'`, `'\t'`. Ein Text, dessen Wörter nur durch `'\r'` (alte Mac-Zeilenenden), ein geschütztes
Leerzeichen (U+00A0, häufig aus PDF-Kopien) oder U+2003 getrennt sind, fällt in den harten Schnitt – mitten im Wort,
genau das, was die Wortgrenze verhindern soll. Gefunden vom Prüfer beim Lesen, nicht gemessen (`unverifiziert`).

**Behebung (Vorschlag):** dieselbe Menge an beiden Stellen – die Wortgrenze per Schleife mit `char.IsWhiteSpace`
statt `LastIndexOfAny`; ein Theory-Fall mit NBSP-getrennten Wörtern und exakter Länge.

## Verlauf

- **2026-10-27** — angelegt aus dem Review zu B-300/B-301 (Lauf F, Teil 2, [Protokoll](../pm-sitzung-2026-10-27-nacht.md)),
  Fund 3a (🟢, außerhalb des Diffs): eigene Story, weil das Sprint-Ziel (B-300/B-301) ohne sie erfüllt ist.
- **2026-10-27** — in Lauf F, Teil 3 ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)) als `Defekt` unter Freigabe 1
  ausformuliert, geschätzt (XS) und gebaut. **Gemessen** (nicht mehr `unverifiziert`): `Theory`
  `Fill_Kuerzt_Freitext_An_Jeder_Art_Von_Leerraum_Am_Wortende` mit NBSP, `\r`, U+2003 als Trenner (400 × „Wortes“) →
  rote Probe **3 failed**, je erwartet 1994, gemessen **2000** (Schnitt mitten im Wort). Fix: die Wortgrenze per
  Schleife mit `char.IsWhiteSpace` – dieselbe Menge wie die Grenzprüfung und `TrimEnd()` → grün. Die rote Probe ist
  zugleich die Mutante (alte Zeile zurück). Review (`pugling-reviewer`): Äquivalenz für jede Eingabe des alten Codes
  nachgewiesen (kein Leerraum, Index 0, 1..1999); die B-296/B-300-Mutanten beißen weiter; `\r` tötet zusätzlich die
  Mutante `char.IsSeparator`, NBSP die Mutante „`\r` zur alten Menge“ – die drei Fälle sind nicht redundant. Kein
  🟡/🔴 im Diff; 🟢 Wortlaut des Testkommentars → genommen. Suite **979/979 `exit=0`**. Rollengang: Agent ohne
  Oberfläche, Tests sind der Weg. **`abgenommen`.**
- **2026-10-27** — Nachschau (Lauf F, Teil 5, frischer `pugling-reviewer` am Commit `193f4f6`): 1994 je Trenner
  nachgerechnet (2799 Zeichen, Trenner bei 7k+6, Index 2000 ist ein „s“); Mutanten `LastIndexOfAny` (alle drei rot),
  `char.IsSeparator` (nur `\r` rot), „alte Menge + `\r`“ (NBSP/U+2003 rot) hergeleitet – die drei Fälle sind nicht
  redundant; Äquivalenz zum alten Code für 1..1999 und ohne Leerraum. **Entgangen:** die Grenzprüfung selbst ist nur
  mit Leerzeichen belegt (`== ' '` bliebe grün) → [B-309](B-309-leerraum-auf-der-grenze-nur-mit-leerzeichen-belegt.md).
