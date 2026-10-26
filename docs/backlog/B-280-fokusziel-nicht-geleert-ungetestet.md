---
tags: [typ/story, status/idee, bereich/frontend, bereich/a11y, bereich/test, rolle/supervisor]
aliases: [Fokusziel bleibt stehen – kein Test]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: Nachschau zum Sprint Lehrbuch-Formular in Lauf C (docs/pm-sitzung-2026-10-26-tag.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-268]
wartet_auf: ""
---

# B-280 · Dass das Fokusziel nach dem Sprung geleert wird, prüft kein Test

[B-268](B-268-fokus-nach-inline-reihe.md) setzt den Fokus über einen Ref, den ein Effekt ohne Abhängigkeiten nach jedem
Rendern liest und **leert** (`frontend/src/vater/ChildMaterialSection.tsx`, `fokusZiel.current = null`). Würde er nicht
geleert, risse nach einem erfolgreichen „Reihe anlegen & wählen“ jedes spätere Rendern (etwa beim Tippen des Titels)
den Fokus zurück auf die Reihen-Auswahl. Für eine Tastaturnutzerin wäre das schlimmer als der ursprüngliche Fehler.

**Gemessen in der Nachschau:** Ohne das Leeren bleibt `TextbookForm.test.tsx` **18/18 grün**. Die Probe aus dem Bau
(„Ref nicht geleert → rot an `:366`“) galt für den Fehlerweg-Fall, bevor Fund 5 ihn umstellte. Seitdem setzt der
Fehlerzweig das Namensfeld ausdrücklich, und dasselbe Ziel liegt ohnehin dort. Die Zusicherung kehrte nicht zurück.

Vorschlag: Nach dem erfolgreichen Anlegen (und nach „Abbrechen“) ein weiteres Rendern auslösen, etwa durch Tippen im
Titel, und prüfen, dass der Fokus dort bleibt, wohin die Nutzerin ihn gesetzt hat.

## Verlauf

- **2026-10-26** — angelegt aus der Nachschau (frischer Prüfer, gemessen an einem `git archive` von `4a25d47`) in
  Lauf C ([Protokoll](../pm-sitzung-2026-10-26-tag.md)).
