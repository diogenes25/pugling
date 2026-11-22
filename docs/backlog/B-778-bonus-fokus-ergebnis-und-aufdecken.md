---
tags: [typ/story, status/abgenommen, bereich/a11y, rolle/student]
aliases: [Bonus-Fokus Ergebnis und Aufdecken]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: "Review von B-671 im Nachtlauf 2026-11-21 (frontend-reviewer), docs/pm-sitzung-2026-11-21.md"
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-778 · Die Bonusrunde lässt den Fokus nach „Aufdecken“ und auf dem Ergebnisblatt auf `body` fallen

In der Bonusrunde (`frontend/src/sohn/SohnBonus.tsx`) ersetzt „Aufdecken“ einer Karteikarte den Knopf durch die
Urteils-Knöpfe, und nach der letzten Antwort ersetzt das Ergebnisblatt die Lektion. In beiden Fällen verschwindet das
fokussierte Element, und der Fokus fällt auf `body`. Ein Tastatur- oder Screenreader-Kind muss neu suchen. Der Test
macht beides schon richtig: Er setzt die Absicht in `onReveal` (`SohnTest.tsx:279`, `revealed` in den Abhängigkeiten)
und fokussiert die Überschrift des Ergebnisses (`resultTitleRef`, `SohnTest.tsx:95`). B-671 Entscheidung 4 („wie im
Üben“) ist damit nur für den Kartenwechsel eingelöst. Das war schon vor B-671 so. Bewusst nicht in B-671 mitgenommen,
weil dessen Ziel (das Sonderzeichen landet in der neuen Karte) ohne das erfüllt ist.

## User Story

Als Sohn, der mit Tastatur oder Screenreader spielt, möchte ich nach „Aufdecken“ und am Ende der Bonusrunde nicht neu
suchen müssen, wo ich bin.

## Entscheidungen

*Stellvertretend entschieden im Nachtlauf A 2026-11-22 (Defekt, autonom grillbar), unbestätigt.*

1. **Wie im Test:** „Aufdecken“ setzt die Fokus-Absicht, der Effekt hört auf `revealed` und fokussiert das erste
   freie Bedienelement der Antwortzone („Nicht gewusst“). Das Ergebnisblatt fokussiert seine Überschrift (`tabIndex={-1}`).
2. **Nach der letzten Karte wartet die Absicht auf das Ergebnis**, statt zu verfallen; ein Fehler verbraucht sie (die
   Fehlerbox fokussiert sich selbst). Verworfen: eine zweite Absicht nur fürs Ergebnis – ein Wächter weniger.
3. **Der erste Aufbau fokussiert weiter nichts** (B-443), auch mit den neuen Abhängigkeiten.

## Akzeptanzkriterien

1. Nach „Aufdecken“ steht der Fokus auf „Nicht gewusst“. Rot: `onReveal` ohne Absicht oder `revealed` nicht in den
   Abhängigkeiten.
2. Nach der letzten Karte steht der Fokus auf der Überschrift des Ergebnisses. Rot: die Absicht verfällt vor dem Ergebnis
   (Ausgangszustand) oder die Überschrift trägt keinen `ref`.
3. Festschreibung: das Laden der Runde fokussiert nichts. Rot: der Wächter `if (!focusIntent.current)` fehlt.

## Verlauf

- **2026-11-21** — angelegt aus dem Review von B-671 im Nachtlauf (`idee`).
- **2026-11-22** — ausformuliert, gegrillt (stellvertretend, Defekt) und geschätzt (S, frontend) im Nachtlauf A; in-arbeit in Sprint 113.
  - AK 1 → `SohnBonus.test.tsx` „nach „Aufdecken“ steht der Fokus auf dem ersten Urteil“, Q1 (ohne Absicht) → rot, Q4 (`revealed` nicht in den Abhängigkeiten) → rot.
  - AK 2 → „Überschrift des Ergebnisses“, Q2 (Absicht verfällt) → rot, Q3 (ohne `ref`) → rot.
  - AK 3 → „fokussiert beim Laden der Runde nichts“, Q5 (ohne Wächter) → rot. Messung 00:56 nach der letzten Änderung, Grundlinie grün.
- **2026-11-22** — Review `frontend-reviewer`: 🟡 behoben – der getippte Weg zum Ergebnis war nicht getestet (`starte("Typed")` ungenutzt); neuer Fall „nach der letzten getippten Karte“, Q6 (`answer()` ohne Absicht) → rot. **Zähler 1 von 5.** 🟢 ohne Zählung: ein Fokus, den das Kind während „Wird ausgewertet …“ selbst verschiebt, wird vom Ergebnis überholt (wie im Test, Fenster kurz, belassen).
- **2026-11-22** — abgenommen (`b2081837`, Sprint 113): Rollengang Sohn per E2E `bonus` (Fokus nach „Aufdecken“ auf „Nicht gewusst“, Ergebnisüberschrift fokussiert; Probe E4 → rot) gegen frisch gestarteten Server; Vater/Creator Regressionszeugen. Backend 2023/2023 (Release).
