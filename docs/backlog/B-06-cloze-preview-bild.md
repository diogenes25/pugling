---
tags: [typ/story, status/verworfen, bereich/medien, bereich/katalog, lerntechnik/lueckentext, rolle/creator]
aliases: [Cloze-Vorschau mit Bild, Fund 3]
status: verworfen
prio: P6
art: Wunsch
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/backlog-vokabellernen.md#fund-3--kleinigkeit-cloze-vorschau-zeigt-nie-ein-bild
grund: >
  Reine Sichtbarmachung von B-03 für den Vater — mit B-03s Verwerfen (dieselbe Anti-Cheat-Prämisse,
  siehe dort) entfällt der Anlass vollständig. Kein eigenständiger Nutzen ohne B-03.
ersetzt_durch: []
---

# B-06 · Cloze-Vorschau kann kein Bild zeigen

## User Story

Als Creator möchte ich in der Vorschau sehen, was das Kind sieht — sonst halte ich ein funktionierendes
Feature für kaputt.

## Ist-Stand am Code · Entscheidungen

→ Grill-Protokoll vom 2026-07-30, Abschnitt **„Fund 3"**:
[backlog-vokabellernen.md](../backlog-vokabellernen.md#fund-3--kleinigkeit-cloze-vorschau-zeigt-nie-ein-bild).

Kern: `PreviewStage` des Cloze-Typs ist `TranslationFreeText`, also eine **getippte** Stufe — und auf
getippten Stufen gibt es nach der Anti-Cheat-Regel bewusst kein Bild. Die Vater-Vorschau bekäme das in
B-03 ergänzte Bild also nie zu sehen.

## Akzeptanzkriterien

1. Die Cloze-Vorschau läuft auf einer nicht-getippten Stufe (`TranslationWordBank`) und zeigt das Bild,
   wenn eines hinterlegt ist.
2. Die Anti-Cheat-Regel bleibt unangetastet: auf getippten Stufen weiterhin kein Bild.

## Schätzung

**Größe: XS** — eine Stufenangabe in `BuiltInExerciseTypes.cs`.

- **Reihenfolge:** zusammen mit **B-03** erledigen, sonst wirkt B-03 für den Vater kaputt.
- **Testweg:** der Vorschau-Test des Cloze-Typs; sichtbar im `ExercisePreviewModal`.

## Verlauf

- **2026-07-30** — geerntet aus dem Grill-Protokoll vom selben Tag, Stufe `geschaetzt` übernommen.
- **2026-08-02** — **Akzeptanzkriterium 1 ist unerfüllbar geworden.** Es verlangt eine „nicht-getippte Stufe
  (`TranslationWordBank`)"; [B-76](B-76-lueckentext-karte-ohne-luecke.md) hat genau die getippt gemacht
  (Entscheidung E6, Commit `1125ee6`), damit die Wortbank ankommt. Der Lückentext hat jetzt **keine**
  untypisierte Stufe mehr. Die Story hängt damit an derselben Entscheidung wie
  [B-03](B-03-lueckensaetze-mit-bild.md) und wird mit ihr zusammen neu bewertet — sie war ohnehin nur deren
  Sichtbarmachung für den Vater. Gefunden vom `pugling-reviewer` beim Review zu B-76.
- **2026-08-07** — **verworfen** (Grillen im Dialog, siehe `grund`): mit B-03 entfällt der Anlass.
