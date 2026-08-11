---
tags: [typ/story, status/idee, bereich/frontend, bereich/medien]
aliases: [bilder.spec flackert, E2E-Flake Bebilderung, Playwright instabil]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Verifikation von B-149 am 2026-08-11 (voller Playwright-Lauf)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
wartet_auf: ""
---

# B-153 · `bilder.spec.ts` fällt im vollen Lauf gelegentlich aus, allein ist sie grün

Beim Verifizieren von [B-149](B-149-schularten-tabelle-statt-manifest.md) fiel
`e2e/bilder.spec.ts:77` („Vater bebildert eine Vokabel, der Sohn sieht sein Bild und kann es wechseln")
in einem vollen Playwright-Lauf aus — **33 von 34**. Allein nachgefahren: grün. Voller Lauf wiederholt:
grün, 34/34.

**Mit B-149 hat das nichts zu tun.** Die Story fasst `labels.ts`, `types.ts` und einen
OpenAPI-Dokument-Transformer an; Medien, Vokabeln und die Sohn-Arcade berührt sie nicht, und der
Laufzeitwert von `SCHOOL_TYPES` ist Element für Element derselbe geblieben.

## Was fehlt: der Beleg

**Der Fehlergrund ist nicht erfasst.** Der grüne Einzellauf hat `test-results/` überschrieben, bevor die
`error-context.md` des roten Laufs gelesen war — ein Fehler in meinem Vorgehen, nicht des Tests. Damit
steht hier nur „einmal rot, zweimal grün" und keine Ursache. Wer das aufnimmt, braucht zuerst einen
reproduzierten roten Lauf mit erhaltenem Artefakt (`--retries=0`, `test-results/` vorher sichern).

Verdachtsrichtung, ausdrücklich **unbelegt**: Der Lauf teilt sich eine Datenbank, und die Bebilderung
hängt an der deterministischen Bildwahl je Kind (`ChildMediaPick`, „Bildkonstanz *ist* der Merkeffekt").
Ein von einer anderen Spec angelegtes Kind oder Motiv könnte die Auswahl verschieben. Das ist eine
Hypothese, keine Diagnose.

## Warum eine eigene Story

Ein flackernder E2E ist die Sorte Rot, die man beim zweiten grünen Lauf gerne vergisst — und dann steht
im nächsten Abnahmeprotokoll „34/34", ohne dass jemand erfährt, dass es beim ersten Mal 33 waren.
[B-109](B-109-full-flow-spec-flackert-bei-frage-3.md) war derselbe Fall und hat vier Flächen stillgelegt,
bevor er behoben wurde.

**Kein `entgangen_bei`:** Es ist ein Testlauf-Problem, kein Defekt in abgenommener Produktarbeit.

## Verlauf

- **2026-08-11** — angelegt bei der Verifikation von B-149. Belege: erster voller Lauf 33/34 (rot:
  `bilder.spec.ts:77`), Einzellauf grün, zweiter voller Lauf 34/34.
