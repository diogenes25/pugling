---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/a11y, bereich/test, rolle/supervisor]
aliases: [Fokusziel bleibt stehen – kein Test]
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
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

## User Story

Als **Vater** mit Tastatur möchte ich, dass der Fokus nach „Reihe anlegen & wählen" oder „Abbrechen" dort bleibt, wohin
ich ihn danach setze – auch wenn ich tippe.

## Ist-Stand (Lauf L, am Code belegt)

Siehe oben. `ChildMaterialSection.tsx` leert `fokusZiel.current` im Effekt; kein Fall rendert nach dem Sprung erneut.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Zwei Zusicherungen, kein Code:** nach dem Anlegen (im bestehenden Fokus-Fall) und nach „Abbrechen" (eigener Fall)
   ins Titelfeld wechseln, tippen, der Fokus bleibt dort. *Kosten:* keine.

## Akzeptanzkriterien

1. Mutante „Ziel nicht geleert" → beide Fälle fallen, und zwar an der neuen Zusicherung.

## Schätzung

`XS`, nur Test. Testweg: `TextbookForm.test.tsx`.

## Verlauf

- **2026-10-26** — angelegt aus der Nachschau (frischer Prüfer, gemessen an einem `git archive` von `4a25d47`) in
  Lauf C ([Protokoll](../pm-sitzung-2026-10-26-tag.md)).
- **2026-10-30** — Lauf L, Teil 3: gebaut. Mutante „`fokusZiel.current = null` auskommentiert" → **2 failed** (erwartet 2),
  an `TextbookForm.test.tsx:359` und `:376` – den beiden neuen Zusicherungen, nicht früher. Danach **19/19**.
- **2026-10-30** — Review `frontend-reviewer`: sauber; beide Fälle setzen den Fokus bewusst weg und rendern dann neu, sind also nicht im Ausgangszustand wahr.
- **2026-10-30** — Abnahme Lauf L, Teil 3: `tsc -b` sauber, Vitest **585/585** (danach + 1 Fall in `SohnTest.test.tsx`, **14/14**), volle Backend-Suite siehe Protokoll. E2E-Gesamtlauf **102 passed** (`exit=0`, nach dem letzten Produktcode-Edit gestartet; danach nur Tests). Rollengang als E2E (Browser-Extension nicht genutzt): Vater über `assistent-kinderliste-kommt-spaet`, `assistent` und `uebungen-bewerten` (Kollege mit Schreibrecht), Sohn über die Klausur-Komponententests (Fehler → „Weiter versuchen" → nächste Frage; eine E2E-Spec mit gescheitertem `nextTest` bräuchte einen kaputten Server, das gehört auf Komponentenebene), Creator Regressionszeuge. Review `frontend-reviewer`: kein 🔴/🟡. **Abgenommen**, Commit siehe Protokoll ([Lauf L](../pm-sitzung-2026-10-30-nacht.md)).
