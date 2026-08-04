# T-05 · Wie verschmelzen `/vater/katalog` und `/vater/lehrwerke` im Frontend?

Status: offen           <!-- offen | beansprucht | entschieden -->
Typ: grilling           <!-- research | prototype | grilling | task -->
Blockiert durch: T-01

## Frage

Beide Bereiche sind heute getrennte, unverlinkte Routen. Nach der Verschmelzung ist der Katalog-Baum
strukturell `TextbookSeries → SeriesUnit → Exercise` statt `Subject → Chapter → Exercise` —
verschmelzen die beiden Frontend-Bereiche zu einer Route, bleibt `/vater/katalog` bestehen und zeigt
jetzt Lehrwerk-Struktur, oder bleiben es zwei verlinkte, aber getrennte Ansichten (Lehrwerk-Verwaltung
vs. Übungs-Anlage innerhalb einer gewählten Unit)?

## Antwort

**Noch nicht gegrillt** — dies ist nur eine Randnotiz aus dem Bau-Sprint, kein Beschluss. Der
Schema-Slice hat das Frontend komplett zerbrochen (Übungs-Anlage und Ziel-Etappen unbedienbar, siehe
`docs/pm-sitzung-2026-08-04.md` Runde „Re-Review"); ein Notfall-Fix war darum unausweichlich, nicht
optional. Der Fix folgt de facto der Option „zwei verlinkte, getrennte Ansichten": `/vater/katalog`
(`CatalogAdmin.tsx`) verwaltet weiterhin nur Fach+Art und verlinkt jetzt auf `/vater/lehrwerke` für
Reihe/Unit; die Übungs-Anlage (`VaterExerciseCreate.tsx`) bekam einen Fach→Reihe→Unit-Kaskaden-Picker.
Das entscheidet die eigentliche Frage dieses Tickets nicht — ob eine tiefere Verschmelzung (z. B. Units
direkt aus der Übungs-Anlage heraus anlegen, ohne zu `/vater/lehrwerke` zu wechseln) lohnt, bleibt offen.

