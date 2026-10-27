---
tags: [typ/story, status/abgenommen, bereich/test, bereich/agent, rolle/creator]
aliases: [Harter Schnitt ohne Leerraum ungetestet, Clamp dritter Zweig]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf E Teil 9 (docs/pm-sitzung-2026-10-27-nacht.md, Lauf F Teil 1)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-292, B-296]
wartet_auf: ""
nachgeschaut: "2026-10-27"
---

# B-300 · Der harte Schnitt eines Freitexts ohne Leerraum ist ungetestet – ein leerer Kopf bliebe grün

`UnitNotesFiller.Clamp` kennt drei Schnitte: Grenze auf Leerraum, letzte Wortgrenze davor, und – ohne jeden Leerraum –
den harten Schnitt bei 2000 (`boundary > 0 ? head[..boundary] : head`). B-296 hat die ersten beiden mit exakten
Längen belegt, den dritten nicht. Mutante (aus dem Code hergeleitet in der Nachschau): `head` → `""` – dann nennt
`Shortened` das Feld, `Decide` sieht aber keinen Wert und schreibt es **nicht**: genau der stille Verlust, um den es
B-292 ging, und die Suite bleibt grün. Dazu (🟢) das abschließende `TrimEnd()` im Wortgrenzen-Zweig: zwei Leerzeichen
vor der letzten Grenze ließen den Entwurf auf ein Leerzeichen enden, ohne dass ein Test fällt.

**Behebung:** ein `Theory` mit exakten Längen: 2500 × „x“ → 2000; 1990 × „x“ + 2 Leerzeichen + 20 × „y“ → 1990.

## Verlauf

- **2026-10-27** — angelegt aus der Nachschau zu Lauf E Teil 9 (`aab2ef9`) in Lauf F, Teil 1
  ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)), Fund 1 (🟡) und Fund 5 (🟢) des frischen Prüfers;
  `entgangen_bei: B-292` (der Zweig) und `B-296` (deren Sprint-Ziel versprach die Grenzen „nach unten bewiesen“).
  Als `Defekt` unter Freigabe 1 ausformuliert und geschätzt (XS, nur Test).
- **2026-10-27** — gebaut in Lauf F, Teil 2: `Theory` `Fill_Kuerzt_Freitext_Ohne_Oder_Mit_Doppeltem_Leerraum_Exakt`
  mit (2500 × x → **2000**) und (1990 × x + 2 Leerzeichen + 20 × y → **1990**). Vorher wie nachher grün (der Code
  stimmte, die Lücke war der Test) – getragen über Mutanten: harter Schnitt gibt `""` zurück → **1 failed** (Zeile
  2500); abschließendes `TrimEnd()` des Wortgrenzen-Zweigs gestrichen → **1 failed** (Zeile 1990, gemessen 1991).
  Review (`pugling-reviewer`): Arithmetik beider Zeilen nachgerechnet, kein 🟡/🔴; 🟢 Umbruch zweier Kommentarzeilen
  → genommen; 🟢 außerhalb des Diffs → [B-302](B-302-fill-wortgrenze-kennt-nur-drei-leerzeichen.md). Suite
  **975/975 `exit=0`**. Rollengang: der Agent hat keine Oberfläche, die Tests sind der Weg. **`abgenommen`.**
- **2026-10-27** — Nachschau (Lauf F, Teil 4, frischer `pugling-reviewer` am Commit `26e731a`): beide `Theory`-Zeilen
  Index für Index nachgerechnet (2500 → 2000 über `boundary = -1`; 2012 Zeichen, Leerzeichen an 1990/1991 → 1990,
  ohne `TrimEnd` 1991); drei Mutanten des harten Schnitts hergeleitet (`""` → 0, `trimmed` → 2500, `>= 0` → wirft) –
  alle rot; die Wirkung von `193f4f6` (Schleife statt `LastIndexOfAny`) auf beide Zeilen geprüft – Erwartungen und
  Mutanten halten. `Fill_*` 17/17 gemessen. Kein Fund.
