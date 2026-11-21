---
tags: [typ/story, status/abgenommen, bereich/test, rolle/student]
aliases: [cel-title nach Learning Dad]
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: "Offene Roadmap docs/pm-sitzung-2026-11-20.md, Punkt 3; gemessen im Nachtlauf 2026-11-21"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: "2026-11-21"
---

# B-775 · Drei E2E-Specs suchen den Feier-Titel noch unter `.cel-title`

Die Learning-Dad-Arbeit (`8a9812a4`) rendert Titel und Untertitel einer Feier in der Sprechblase von `LearningDad`
(`.dad-bubble-text`), nicht mehr in `.cel-title` (`frontend/src/components/Celebration.tsx`, Banner-Bühne).
`full-flow`, `shop-verlauf` und `taschengeld-monatlich` warten darum zehn Sekunden auf ein Element, das es nicht mehr
gibt, und scheitern. Damit trägt der Vater→Sohn-Durchstich nichts mehr. `entgangen_bei` bleibt leer, weil die
Dad-Arbeit keine Story ist, sondern eine Sitzung ohne Abnahme.

## User Story

Als *Entwickler* möchte ich, dass der Durchstich den Feier-Titel dort sucht, wo die Arcade ihn zeigt, damit ein grüner
E2E-Lauf wieder belegt, dass die Combo-Feier und die Kauf-Feier beim Sohn ankommen.

## Ist-Stand am Code

Gemessen am 2026-11-21 auf `c84fbedb`.

- `full-flow.spec.ts:99` (`COMBO ×5`), `:172`, `shop-verlauf.spec.ts:68` und `taschengeld-monatlich.spec.ts:133`
  (`GEKAUFT!`) suchen `.cel-title`.
- `Celebration.tsx`: Die Stufen `medium` und `big` geben den Titel als `caption` an `LearningDad`. Die Stufe `small`
  zeigt **keinen** Titel. Combo ×5 ist `medium` (`SohnPractice.tsx:285`), die Kauf-Feier ebenfalls eine Stufe mit Titel.
- `LearningDad.tsx` rendert die `caption` in `.dad-bubble-text`.

## Entscheidungen

1. **Die Specs suchen `.cel-layer .dad-bubble-text`.** Begründung: Das ist die Stelle, die der Sohn sieht. Der Bezug
   auf `.cel-layer` hält eine Sprechblase außerhalb der Feier (Dad-Showcase, spätere Dad-Auftritte) aus dem Treffer.
   Kosten: Der Selektor hängt an der Klasse der Komponente. Verworfen: ein Rollen-Selektor, weil die Feier-Ebene
   `aria-hidden` ist und für die Barrierefreiheit unsichtbar bleiben soll (die Ansage kommt aus dem Ton und dem Inhalt
   der Seite); ebenso verworfen: `.cel-title` zurückzubauen, denn das änderte das neue Aussehen.

## Akzeptanzkriterien

1. `full-flow`, `shop-verlauf` und `taschengeld-monatlich` laufen grün. *Rot, wenn* der Titel der Feier nicht mehr in
   der Sprechblase steht (Probe: `caption` im Banner weglassen).

## Schätzung

**Größe: XS**: vier Selektoren. Testweg: die drei Specs auf den Ausweichports (`PUGLING_E2E_API_PORT=5201`,
`PUGLING_E2E_WEB_PORT=5174`), weil auf :5200 der Heimnetz-Server läuft.

## Verlauf

- **2026-11-21** — angelegt im Nachtlauf aus der Roadmap des 2026-11-20 und in einem Zug bis `in-arbeit` gebracht
  (Defekt, Freigabe 1). Rote Probe auf `c84fbedb`: erwartet 3 passed, gemessen **3 failed / 0 passed**, je
  `locator('.cel-title')… element(s) not found`. Daneben im Server-Log eine UNIQUE-Verletzung an `ShopListingStocks`:
  nachgeschaut, kein Befund. Zwei gleichzeitige `GET student/me/shop` legen denselben Bestand an, `ShopService.cs:657`
  fängt das als verlorenes Rennen ab und liest neu, die Antwort ist 200 (Log-Zeile 405 des Laufs). EF loggt es trotzdem
  als ERR.
- **2026-11-21** — gebaut: vier Selektoren auf `.cel-layer .dad-bubble-text`. AK 1 → die drei Specs, nach dem Fix 3 passed
  (zusammen mit `vater-schmal` 5 passed), nach dem letzten Edit an `LearningDad.tsx` erneut 3 passed. Mutante
  „Banner ohne `caption`“ (`Celebration.tsx`): erwartet `shop-verlauf` rot, gemessen rot. Review `frontend-reviewer`: die
  Selektoren sind robust (die kleine Feier hat keinen Titel, also keine `.dad-bubble-text`; nur eine `.cel-layer` zur
  Zeit); kein Fund zu dieser Story.
- **2026-11-21** — `abgenommen` mit `9ab66ecc`. Rollengang: volle E2E-Suite **179/179** nach dem letzten Edit (Ausweichports),
  darin `full-flow` (Combo ×5 und Kauf-Feier beim Sohn) und `shop-verlauf`/`taschengeld-monatlich` (Kauf-Feier). Vater und
  Creator: Regressionszeugen, ihre Specs grün.
- **2026-11-21** — Nachschau (Retro Sprint 99): Die vier Selektoren tragen im vollen Lauf von Sprint 99 erneut (179/179), und `.cel-layer .dad-bubble-text` trifft nach der Änderung an `LearningDad` (Untertitel ohne Titel) keine kleine Feier: Sie hat keinen Titel, also keine `.dad-bubble-text`. Kein Befund.
