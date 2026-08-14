---
tags: [typ/story, status/ausformuliert, bereich/frontend, bereich/medien]
aliases: [bilder.spec flackert, E2E-Flake Bebilderung, Playwright instabil]
status: ausformuliert
prio: P2
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Verifikation von B-149 am 2026-08-11 (voller Playwright-Lauf)
unverifiziert: false
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

## Der Beleg — nachgeliefert am 2026-08-14

Die Ursache ist **keine** Zeitfrage. Der volle Lauf im Nachtlauf vom 2026-08-14 (35 grün, 1 rot) hat die
Meldung erhalten:

```text
Error: expect(locator).toBeVisible() failed
Locator: getByRole('img', { name: 'Stadt als Foto' })
Error: strict mode violation: ... resolved to 2 elements:
    1) <img alt="Stadt als Foto" src="/media/2/thumb.webp"/>  aka figure … hasText: 'Stadt als FotoEntfernen'
    2) <img alt="Stadt als Foto" src="/media/2/thumb.webp"/>  aka figure … hasText: 'Stadt als Fotoschon dabei'
```

**Derselbe Bezug trifft zwei Elemente**: dasselbe Motiv (Asset 2) steht einmal als *verknüpftes* Bild
(`figure` mit „Entfernen") und einmal im *Auswahlfeld* (`figure` mit „schon dabei"). Beides ist richtig — die
Oberfläche kennzeichnet im Picker korrekt, was schon dabei ist. **Falsch ist der Locator**: er ist nicht auf
das verknüpfte `figure` eingeschränkt und trifft darum beide, sobald das Auswahlfeld geladen ist.

Damit erklärt sich auch das „Flackern": Der Fall ist grün, solange das Auswahlfeld beim Zeitpunkt der
Zusicherung noch nicht steht — im Einzellauf praktisch immer, unter der Last des vollen Laufs nicht
zuverlässig. **Die frühere Verdachtsrichtung (geteilte Datenbank, `ChildMediaPick`) ist damit erledigt** und
war falsch: es liegt kein Fremdzustand vor, sondern eine Zweideutigkeit im Test selbst.

**Fix-Richtung:** Die Zusicherung auf das verknüpfte `figure` einschränken (Muster: `locator("figure")
.filter({ hasText: "Entfernen" }).getByRole("img", { name: … })`), statt global nach dem `alt`-Text zu
greifen. Rote Probe: Der Fall muss im **vollen** Lauf grün werden und im Einzellauf grün bleiben; zusätzlich
belegt eine Zusicherung, dass es *zwei* Vorkommen gibt, dass der eingeschränkte Locator wirklich das
verknüpfte trifft und nicht zufällig das erste.

## Warum eine eigene Story

Ein flackernder E2E ist die Sorte Rot, die man beim zweiten grünen Lauf gerne vergisst — und dann steht
im nächsten Abnahmeprotokoll „34/34", ohne dass jemand erfährt, dass es beim ersten Mal 33 waren.
[B-109](B-109-full-flow-spec-flackert-bei-frage-3.md) war derselbe Fall und hat vier Flächen stillgelegt,
bevor er behoben wurde.

**Kein `entgangen_bei`:** Es ist ein Testlauf-Problem, kein Defekt in abgenommener Produktarbeit.

## Verlauf

- **2026-08-11** — angelegt bei der Verifikation von B-149. Belege: erster voller Lauf 33/34 (rot:
  `bilder.spec.ts:77`), Einzellauf grün, zweiter voller Lauf 34/34.
- **2026-08-14** — `idee → ausformuliert`, `prio` P3 → P2, `unverifiziert` weg. Im Nachtlauf-Sprint 1 fiel der
  Fall erneut (voller Lauf **35 grün, 1 rot**; Einzellauf danach **1 passed**) — diesmal **mit** erhaltener
  Meldung. Sie nennt die Ursache: eine `strict mode violation`, der Bildbezug trifft zwei Elemente. Der
  Befund liegt **außerhalb** des Sprint-Diffs (nur `VaterWizard.tsx`, `assistent.spec.ts`,
  `FachEigentumTests.cs`) und zählt darum nach Freigabe 3 nicht in den Fehlerzähler des Sprints; er wird hier
  aufgeschrieben statt dort mitgebaut. P2, weil ein flackernder Fall das Test-Tor und die CI gelegentlich rot
  färbt und die Ursache jetzt in Minuten behebbar ist.
