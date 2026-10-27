---
tags: [typ/story, status/idee, bereich/agent, rolle/creator]
aliases: [Wortgrenze mit CR oder NBSP, Clamp Leerraum-Mengen]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: Review zu B-300/B-301 in Lauf F, Teil 2 (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-292]
wartet_auf: ""
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
