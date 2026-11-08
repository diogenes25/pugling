---
tags: [typ/story, status/idee, bereich/frontend, bereich/tests, rolle/student]
aliases: [Lösung in der angehaltenen Karte ungetestet]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-08-nacht.md
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-420]
---

# B-439 · Die sichtbare Lösung in der angehaltenen Karte prüft kein Test

Gefunden in der Nachschau zu Sprint 22 (Commit `652e5d3`, Lauf AE, frischer `frontend-reviewer`, nur gelesen). Im
Haltezustand von B-420 steht die Lösung zweimal: sichtbar in der Karte (`SohnPractice.tsx`, Block `.card.mistake`, Zeile
`.rev`) und in der Live-Region, die dann nur `sr-only` wird. Die Zusicherungen treffen beide:

- Vitest `SohnPractice.test.tsx` (AK 1 und AK 3) prüfen `getAllByText(…).length > 0` – grün auch ohne die Zeile in der Karte.
- E2E `erstbegegnung.spec.ts` prüft `getByText("Lösung: die Straße").first()` mit `toBeVisible()` – ohne die Kartenzeile
  landet `.first()` auf der 1×1-px-Region, die Playwright als sichtbar zählt.

Damit ist der Kern der Story („die Lösung **neben meiner eigenen Antwort**") durch keinen Test gehalten; die gemessenen
Mutanten F1–F3/R5 fassen die Zeile nicht an. Reparatur des Reviewers: die Zusicherungen auf die Karte begrenzen
(`within(…closest(".card"))` bzw. `page.locator(".card.mistake")`), dazu prüfen, dass die Region im Haltezustand `sr-only`
trägt; eine Mutante „`.rev`-Zeile entfernt".

Kleinere Funde derselben Nachschau, hier mitgeführt (🟢):

1. `e2e/bilder.spec.ts:199-204` sucht „la ville" mit „Umdrehen"/„Gewusst!" – auf einer frischen Position gibt es diese Knöpfe
   nicht mehr (Kennenlernkarte); die Schleife läuft nur zufällig nie (Wort an Index 0). Die Spec prüft „anderes Bild" jetzt
   auf einer Kennenlernkarte statt auf einer Selbsteinschätzung – unbenannt.
2. `ErstbegegnungTests.GemischteRunde_…_OhneDieComboZuBrechen` misst die Reihenfolge, nicht die Combo – umbenennen oder die
   Runde spielen und `combo` prüfen.
3. Nach einer falschen Antwort auf der **letzten** Karte fällt der Fokus nach „Weiter" auf `body` (das Rundenende nimmt
   ihn nicht).

## Verlauf

- **2026-11-08** — angelegt aus der Nachschau zu Sprint 22 (Lauf AE), 🟡. Löst Limit 2 aus: der Lauf endet nach Sprint 23.
