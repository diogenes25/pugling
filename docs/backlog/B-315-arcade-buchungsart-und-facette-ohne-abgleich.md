---
tags: [typ/story, status/abgenommen, bereich/test, bereich/frontend, rolle/student]
aliases: [pointKind und facet ohne Abgleich mit labels.ts]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf G Teil 1 in Lauf G, Teil 2 (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-304]
wartet_auf: ""
nachgeschaut: "2026-10-28"
---

# B-315 · Deutsche Buchungsarten und Facetten der Arcade sind nicht an die Vater-Helfer gebunden

B-304 hat `pointKind.*` und `interests.facet.*` aus `lib/labels.ts` abgeschrieben, aber nur die Einheiten
(`shop.unit.*` gegen `unitAmount`) per Test gebunden. Mutante (Nachschau, hergeleitet): `de.pointKind.Manual`
umbenennen → alle Vitest-Fälle grün, und dieselbe Buchung hieße in Arcade und Vater-Web verschieden.

## Verlauf

- **2026-10-27** — angelegt aus der Nachschau zu Lauf G, Teil 1 (`95f611b`), Fund 1 (🟡) des frischen
  `frontend-reviewer`; Increment dieses Laufs → **Limit 2**. Als `Defekt` unter Freigabe 1 ausformuliert, geschätzt
  (XS, nur Test) und im laufenden Sprint behoben.
- **2026-10-27** — gebaut: `it.each` über alle `pointKind`-Schlüssel gegen `pointKindLabel` und über
  `INTEREST_FACETS` gegen `interestFacetLabel` (`arcadeSprache.test.tsx`) → am echten Code grün (Testlücke).
  Mutanten `pointKind.Manual` bzw. `facet.Animal` umbenannt → je **1 failed**. **`abgenommen`** mit Teil 2.
- **2026-10-28** — nachgeschaut (Lauf H, Teil 1, frischer `frontend-reviewer` am Commit `43b5eef`): `de.ts:116-130` zeilenweise
  gegen `labels.ts:7-19` gelesen (identisch); Mutante „Wert `de.pointKind.Manual` umbenennen“ → rot hergeleitet. Kein
  Entgleiten; Restlücke 🟢 (die Buchungsarten werden aus `de.ts` iteriert, nicht aus `POINT_KIND_LABELS`, das nicht
  exportiert ist – eine neue Art nur in `labels.ts` bliebe unbemerkt), notiert, nicht angelegt.
