---
tags: [typ/story, status/idee, bereich/klassenarbeit]
aliases: []
status: idee
prio: P2
art: Defekt
groesse: ""
wo: "backend"
migration: ""
vertragsbruch: ""
quelle: "Review von B-752 im Nachtlauf I 2026-11-23 (docs/pm-sitzung-2026-11-23-i.md, pugling-reviewer 🟡)"
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-941 · Die Vorbereitungs-Pflicht legt Schlagwort-Übungen ohne Ausführ-Recht als Position an

`ClassTestPrepService.StartAsync` legt jede Bündel-Übung als Position an, ohne `ExercisePermissionService.CanExecuteAsync` bzw.
`ExecutePublic` zu prüfen. Geprüft wird nur das direkte Zuweisen (`KlassenarbeitenController.BuildExerciseLinksAsync`);
Schlagwörter dürfen fremdes Material tragen (`TagsController.cs:163-167`). So wird zurückgezogenes Material („stoppt neue
Zuweisungen“) über ein Schlagwort doch zur neuen Pflicht. Vorschlag: Kandidaten auf ausführbare Übungen (öffentlich oder mit
Freigabe eines Betreuers des Kindes) beschränken.

## Verlauf

- **2026-11-24** — angelegt im Nachtlauf I (Sprint 134) aus dem Review, nicht behoben: der Fehlerzähler des Sprints stand
  bei 5, ein weiterer Fund hätte ihn überschritten (Freigabe 3/11) – der Lauf endet hier. Protokoll
  [pm-sitzung-2026-11-23-i.md](../pm-sitzung-2026-11-23-i.md).
