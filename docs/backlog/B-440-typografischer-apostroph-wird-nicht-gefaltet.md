---
tags: [typ/story, status/idee, bereich/training, rolle/student]
aliases: [Typografischer Apostroph]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-09-nacht.md
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-440 · Ein typografischer Apostroph („l’école") macht die vollständige Antwort falsch, die verkürzte richtig

`StageMechanics.Normalize` (`backend/Pugling.Api/Services/Shared/StageMechanics.cs:31-32`) faltet `’` nicht zu `'`. Steht im Store
„l'école" und tippt das Kind auf einem iPad mit Smart Punctuation „l’école", ist die Antwort falsch – „école" ohne Artikel gilt seit
[B-421](B-421-antwort-toleranz-artikel.md) dagegen als richtig. Die vollständige Antwort wird strenger bewertet als die verkürzte. Der
Fehler ist älter als B-421, wird durch B-421 aber sichtbar. Vorschlag des Reviewers: `Normalize` faltet `’` zu `'`; die zweite
Präfix-Zeile `["l'", "l’"]` (`:39`) entfällt dann.

## Verlauf

- **2026-11-09** — angelegt aus der Nachschau zu Sprint 23 (Retro Sprint 24, Lauf AF, frischer `pugling-reviewer`), 🟢.
