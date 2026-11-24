---
tags: [typ/story, status/abgenommen, bereich/qualitaet, rolle/entwickler]
aliases: [Umhäng-Spec trifft zwei Bearbeiten]
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
nachgeschaut: "2026-11-22"
quelle: "Nachtlauf B 2026-11-22, volle E2E-Suite des Sprints B1 (docs/pm-sitzung-2026-11-22-b.md)"
grund: ""
ersetzt_durch: []
entgangen_bei: [B-673]
---

# B-790 · `plan-umhaengen-gesperrt.spec.ts` trifft zwei „Bearbeiten“-Knöpfe

Die volle E2E-Suite von Sprint B1 lief auf 5202/5175 mit Basis `9abb6aa6`. Im Diff des Branches gibt es weder
Produktcode noch diese Spec. Trotzdem fiel `plan-umhaengen-gesperrt.spec.ts:23` mit einer strict-mode-Verletzung:
`getByRole('button', { name: 'Bearbeiten' })` fand zwei Elemente, und zwar

- `btn inline-btn`: den Plan im Kopf von `VaterPlanDetail.tsx:63`,
- `btn ghost inline-btn`: die Position in `PlanPositions.tsx:993`.

Einzeln lief die Spec zweimal und fiel beide Male rot. Das Rot ist also fest, kein Flackern.

## User Story

Als Entwickler möchte ich, dass die Rollengang-Spec von B-673 den Plan-Knopf trifft. Dann meint ihr Rot die
Umhäng-Sperre und nicht die Ladereihenfolge der Seite.

## Ist-Stand am Code

- Die Spec legt per API einen Plan **mit einer Position** an (`:42-43`). Dann öffnet sie die Plan-Seite und klickt
  ungezielt „Bearbeiten“ (`:60`).
- Der Kopf rendert mit dem Plan. Die Positionsliste lädt eigenständig nach. Jede Zeile bringt ein eigenes „Bearbeiten“ mit.
- Am 2026-11-21 (Sprint 105) war die Spec grün: Der Klick kam, bevor die Liste stand. Seither hat sich unter
  `frontend/src/vater` nichts geändert (`git log 1ac42f4a..9abb6aa6 -- frontend/src/vater` ist leer). Die Liste ist
  heute schlicht schneller da. Grün war die Spec von Anfang an nur durch den Ablauf.
- Die beiden übrigen Specs mit blankem „Bearbeiten“ auf der Plan-Seite (`plan-kinderliste-faellt-aus`,
  `plan-kinderliste-laedt-noch`) legen über `/vater/plan/new` einen leeren Plan an. Bei ihnen gibt es keine Positionszeile.

## Entscheidungen

*Stellvertretend entschieden im Nachtlauf B (art: Defekt, README „Der Backlog-Lauf“), unbestätigt.*

1. **Der Knopf wird über die Kopfzeile des Plans adressiert, nicht mit `.first()`.** `.first()` hinge an der
   DOM-Reihenfolge und träfe still die Position, sobald der Kopf einmal unter der Liste stünde. Die Kopfzeile ist die
   `.row`, die die Überschrift mit dem Plantitel trägt. Verworfen:
   - Warten auf die Positionsliste und dann `.first()`: zwei Annahmen statt einer.
   - Ein `aria-label` im Produkt: Das wäre ein Produktumbau für einen Testfehler. Und „Bearbeiten“ neben der Überschrift ist
     für Menschen eindeutig.
2. **Kein Dauerfall gegen das Rennen.** Seit heute ist die alte Form stabil rot, die Messung ist also schon
   deterministisch. Die neue Form ist durch ihren Bereich unabhängig von der Ladereihenfolge.

## Akzeptanzkriterien

1. `plan-umhaengen-gesperrt.spec.ts:60` klickt den „Bearbeiten“-Knopf innerhalb der Kopfzeile des Plans. Rot, wenn er die
   Positionszeile träfe: Dann erschiene kein Feld „Kind“ (`:61-62`).
2. Die alte Zeile ist rot (strict mode), die neue grün, je dreimal einzeln. Rot, wenn die alte Zeile grün läuft: Dann
   belegt die Messung nichts.

## Schätzung

**XS**: eine Zeile in einer Spec. `migration: nein`, `vertragsbruch: nein` (nachgesehen: nur `frontend/e2e/`).

## Verlauf

- **2026-11-22**: im Nachtlauf B in einem Zug angelegt und bis `in-arbeit` getrieben. Der Defekt war fest rot, die
  Ursache am Code gelesen, und als Defekt XS durfte der Lauf das allein.
- **2026-11-22**: gebaut. `:60` adressiert jetzt die `.row` mit der Überschrift `Bruder-Plan ${RUN}`.
  - **AK 2, alte Form:** im vollen Lauf rot, einzeln `--repeat-each=2` zweimal rot. Zusammen 3 von 3 rot, immer mit der
    Meldung „strict mode violation … resolved to 2 elements“.
  - **AK 2, neue Form:** `--repeat-each=3` gibt 3 von 3 grün.
  - **AK 1, Mutante:** Der Locator trifft statt der Kopfzeile eine Tabellenzeile (`page.locator("tr")`). Ergebnis: rot an
    `:65`, die Option „Jünger …“ fehlt, weil das Feld „Kind“ nicht erscheint. Der Knopf der Kopfzeile ist also wirklich
    der, der die Spec trägt.
  - Kein Review-Agent: Die Änderung ist eine Zeile in einer Spec, und beide Richtungen sind gemessen. Ein Rollengang
    entfällt, die Spec ist selbst der Rollengang Vater von B-673.
- **2026-11-22**: **abgenommen** (`761af289`). Die volle E2E-Suite auf dem Endstand läuft 185/185 grün, mit der Spec.
- **2026-11-22** — Nachschau (Nachtlauf B 2026-11-23, Retro Sprint B1, Agent `frontend-reviewer`): Selektor gegen `VaterPlanDetail.tsx:51-69` geprüft: die `.row` umschließt genau einen „Bearbeiten“, `PlanPositions` steht als Geschwister darunter; kein Befund.
