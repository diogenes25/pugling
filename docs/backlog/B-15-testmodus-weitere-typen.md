---
tags: [typ/story, status/idee, bereich/katalog, rolle/creator]
aliases: [Vorschau für nicht-prüfbare Typen]
status: idee
prio: P3
art: Wunsch
quelle: memory/uebungs-testmodus.md
unverifiziert: true
---

# B-15 · Vorschau für die nicht-prüfbaren Übungstypen

Der Vater kann eine Übung vor dem Zuweisen durchspielen — aber nur, wo Inhalts-Atome entstehen. Ohne
Vorschau bleibt **`Essay`**: `EssayExerciseType.ItemsOf` gibt `[]` zurück
([BuiltInExerciseTypes.cs:48](../../backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs)), und
`ExercisePreviewService` steigt bei leerer Liste aus
([:30, :49](../../backend/Pugling.Api/Services/Creator/ExercisePreviewService.cs)). Für den Aufsatz fehlt
also eine reine **Ansicht** ohne Bewertung.

> **Korrigiert am 2026-08-02.** Ursprünglich standen hier fünf Typen — „Reading, Essay, Listening, Grammar
> und Birkenbihl". Bei vieren stimmt das nicht: Alle vier bauen Inhalts-Atome und haben damit eine
> Vorschau; Reading und Listening werden dort sogar **bewertet** (`scorePercent: 100` im Lauf). Belegt in
> der Grill-Runde zu [B-75](B-75-lese-hoerverstehen-ohne-inhalt.md), Entscheidung 4. `ArithmeticDrill`
> liefert ebenfalls keine Atome, geht aber bewusst den eigenen Weg über `generate`/`check`
> (`ExerciseCheckMode.CatalogGenerateCheck`) und gehört nicht hierher.

**Was Reading und Listening angeht, ist der Mangel ein anderer** — ihre Vorschau zeigt zwar etwas, aber
nicht den Trägertext bzw. die Aufnahme. Das repariert B-75 mit, es ist nicht diese Story.

**Ungeprüft:** ob eine „nur anzeigen"-Variante ohne Bewertung ins bestehende Preview-Modell passt, und
was der Vater beim Aufsatz überhaupt sehen soll (die Schreibaufgabe plus die Bewertungskriterien?).

## Verlauf

- **2026-07-30** — geerntet (ungeprüft).
- **2026-08-02** — Ist-Stand richtiggestellt: von fünf genannten Typen bleibt einer. Der Rest der Story
  ist unberührt, die Stufe bleibt `idee` — ausformuliert ist damit nichts, nur eine falsche Behauptung
  weniger im Bestand.
