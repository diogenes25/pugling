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

# B-940 · Das Kind kann das Bündel seiner Vorbereitungs-Pflicht über Schlagwörter verkleinern

Schlagwörter sind kindbezogen, und `DELETE creator/tags/{tagId}/exercises/{exerciseId}` lässt das Kind selbst durch
(`TagsController.cs:191-204`, `FindOwnedAsync` → `OwnsChildAsync`). Vor dem Fenster kann das Kind eine Übung aus einem Schlagwort
nehmen, das an seiner Klassenarbeit hängt – sie wird dann nie Pflicht (B-752). Nach dem Start verkürzt dasselbe die
übergreifende Spanne einer zweiten Arbeit, weil `ClassTestPrepService.AlignSpansAsync` die Bündel live liest. Eine Oberfläche
dafür gibt es nicht, ein Kind-Token erreicht den Endpunkt aber. Vorschlag des Reviewers: Entfernen durch das Kind an
Schlagwörtern einer geplanten Arbeit sperren, oder für die Vorbereitung nur Verknüpfungen des Vaters zählen.

## Verlauf

- **2026-11-24** — angelegt im Nachtlauf I (Sprint 134) aus dem Review, nicht behoben: der Fehlerzähler des Sprints stand
  bei 5, ein weiterer Fund hätte ihn überschritten (Freigabe 3/11) – der Lauf endet hier. Protokoll
  [pm-sitzung-2026-11-23-i.md](../pm-sitzung-2026-11-23-i.md).
