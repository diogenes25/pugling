---
tags: [typ/story, status/idee, bereich/katalog, rolle/creator]
aliases: [DefaultTypes Reihenfolge]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-10-fertigstellung.md
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-426]
---

# B-455 · Ob ein unbekannter Übungstyp im Fachlehrer-Profil abgelehnt wird, hängt an der Reihenfolge

`UnknownTypesProblem` prüft erst nach `Clean()`, und `Clean()` entdoppelt ohne Rücksicht auf Groß/Klein, die Registry
vergleicht exakt (`backend/Pugling.Api/Controllers/Creator/CreatorProfilesController.cs:229-238`):
`defaultTypes: ["Cloze","cloze"]` wird angenommen und das unbekannte `cloze` still verworfen, `["cloze","Cloze"]` ist
ein 400. Gespeichert wird nie ein falscher Schlüssel. Vorschlag des Reviewers: die getrimmten Rohwerte prüfen, dann
entdoppeln. Für das Ziel des Fertigstellungs-Laufs nicht tragend, darum `idee`.

## Verlauf

- **2026-11-10** — angelegt aus der Nachschau zu Sprint 28 (Retro Sprint 29).
