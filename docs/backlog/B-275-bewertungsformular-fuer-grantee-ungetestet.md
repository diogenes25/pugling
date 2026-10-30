---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/test, rolle/supervisor]
aliases: [Grantee sieht das Bewertungsformular – ungetestet an der Aufrufstelle]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: frontend-reviewer zu B-12 in Lauf B (docs/pm-sitzung-2026-10-26-tag.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-275 · Ob ein Schreib-Grantee das Bewertungsformular sieht, prüft kein Test an der Aufrufstelle

[B-12](B-12-uebungen-kuratieren.md) AK 7 verlangt, dass das Formular dieselbe Grenze zieht wie der Server: Bewerten
darf, wer eine Übung nicht verwaltet (`isOwner`), auch wenn er sie bearbeiten darf (`isOwn`, Schreib-Grant). Seit Lauf B
steht die Regel im Helfer `canRate`, und Vitest belegt den Grantee-Fall. Die **Aufrufstelle**
(`frontend/src/vater/ExerciseRatingPanel.tsx`, `!canRate(exercise)`) prüft aber nichts. Eine Vertauschung dort
(`exercise.isOwn ? … : …` oder `canRate({ isOwner: exercise.isOwn })`) bleibt grün, weil die E2E-Spec keinen Grantee
fährt: Autorin und Vater haben jeweils beide Felder gleich belegt.

Vorschlag des Reviewers: In `e2e/uebungen-bewerten.spec.ts` gibt die Autorin dem Kollegen einen Schreib-Grant, der
Kollege meldet sich an, und das Formular „Bewertung speichern“ muss sichtbar sein.

**In Lauf B bewusst nicht behoben:** Der geplante Teil von Fund 6 (Helfer, Vitest, rote Probe `isOwn`) ist gebaut. Die
Aufrufstelle ist ein neuer 🟢-Hinweis, und der Zähler stand bei 4 vor dem Delta-Review.

## User Story

Als **Lehrkraft mit Schreibrecht** an einer fremden Übung möchte ich sie bewerten können – ich verwalte sie nicht.

## Ist-Stand (Lauf L, am Code belegt)

Siehe oben; die E2E-Spec fuhr keinen Grantee, `isOwn` und `isOwner` fielen bei keiner Person auseinander.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Ein vierter Abschnitt in `uebungen-bewerten.spec.ts`:** die Autorin gibt dem Kollegen `Write`, der Kollege sieht
   „Bewertung ändern" (er hat oben schon bewertet) und keinen Hinweis „eigene Übung". *Kosten:* ~10 s Laufzeit.

## Akzeptanzkriterien

1. Mutante `canRate({ isOwner: exercise.isOwn })` an der Aufrufstelle → die Spec fällt an der neuen Zusicherung.

## Schätzung

`XS`, nur Test.

## Verlauf

- **2026-10-26** — angelegt aus dem `frontend-reviewer` zu B-12 in Lauf B ([Protokoll](../pm-sitzung-2026-10-26-tag.md)), Hinweis 2.
- **2026-10-30** — Lauf L, Teil 3: gebaut. Spec grün; Mutante an der Aufrufstelle (`ExerciseRatingPanel.tsx`) → **1 failed**
  (erwartet 1) an `uebungen-bewerten.spec.ts:158`, der neuen Zusicherung.
- **2026-10-30** — Review `frontend-reviewer`: sauber; „Bewertung ändern" stammt aus der Bewertung des Kollegen per API, nicht aus dem Vater-Teil.
- **2026-10-30** — Abnahme Lauf L, Teil 3: `tsc -b` sauber, Vitest **585/585** (danach + 1 Fall in `SohnTest.test.tsx`, **14/14**), volle Backend-Suite siehe Protokoll. E2E-Gesamtlauf **102 passed** (`exit=0`, nach dem letzten Produktcode-Edit gestartet; danach nur Tests). Rollengang als E2E (Browser-Extension nicht genutzt): Vater über `assistent-kinderliste-kommt-spaet`, `assistent` und `uebungen-bewerten` (Kollege mit Schreibrecht), Sohn über die Klausur-Komponententests (Fehler → „Weiter versuchen" → nächste Frage; eine E2E-Spec mit gescheitertem `nextTest` bräuchte einen kaputten Server, das gehört auf Komponentenebene), Creator Regressionszeuge. Review `frontend-reviewer`: kein 🔴/🟡. **Abgenommen**, Commit siehe Protokoll ([Lauf L](../pm-sitzung-2026-10-30-nacht.md)).
