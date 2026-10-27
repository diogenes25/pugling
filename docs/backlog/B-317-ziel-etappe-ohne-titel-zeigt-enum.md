---
tags: [typ/story, status/idee, bereich/frontend, rolle/student]
aliases: [k.title ?? k.scope, Datum folgt Browser-Locale]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf G Teil 1 in Lauf G, Teil 2 (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-317 · Eine Ziel-Etappe ohne Titel zeigt dem Kind den rohen Enum-Wert

`MyObjectives.tsx` zeigt `k.title ?? k.scope` – in der Etappenliste und in der Feier. Ohne Titel liest das Kind
`Exercise`, `SeriesUnit` oder `Subject`, in beiden Sprachen. Nebenbei (selbe Nachschau): Daten im Shop-Verlauf und in
den Anfragen (`toLocaleDateString()`) folgen der Browser-Locale, nicht der Arcade-Sprache.

`unverifiziert`: aus dem Code gelesen, nicht im Bild gesehen. Schon vor B-304 so; außerhalb dessen Umfang notiert.

## Verlauf

- **2026-10-27** — angelegt aus der Nachschau zu Lauf G, Teil 1 (Randbefunde, außerhalb des Umfangs).
