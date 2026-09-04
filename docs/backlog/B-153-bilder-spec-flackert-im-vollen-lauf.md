---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/medien]
aliases: [bilder.spec flackert, E2E-Flake Bebilderung, Playwright instabil]
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
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

## Was gebaut wurde

Ein Helfer `verknuepftesBild(vater, description)` schränkt über den „Entfernen"-Knopf der eigenen Kachel
ein (`filter({ has: … })`); die Auswahl des Zuordnen-Knopfs läuft über den vollen `aria-label` statt über
`.first()`. Beide Bezüge tragen `exact: true` — `getByRole(…, { name })` vergleicht als **Teilstring**, und
eine dritte Beschreibung, die eine bestehende enthält, machte die Einschränkung sonst lautlos wieder
zweideutig, mit derselben Meldung und dem Anschein, die Stelle sei repariert.

Dazu zwei Zusicherungen, die die Zweideutigkeit **festhalten** statt sie nur zu umgehen:

- `toHaveCount(2)` auf den rohen Alt-Text-Bezug — die einzige Stelle im Repo, die überhaupt aussagt, dass
  dasselbe Motiv hier zweimal steht. Sie wird rot, sobald jemand den Bezug wieder aufweicht *oder* der
  Picker aufhört, Verknüpftes zu zeigen.
- Der „schon dabei"-Knopf ist **gesperrt**. Das war zuerst ein `toHaveCount(1)` auf den eingeschränkten
  Bezug — und hätte nie vor der Zusicherung in der Schleife fallen können, weil `toBeVisible` bereits
  strikt ist (Review-Fund). Die jetzige Fassung sagt stattdessen, *warum* die zweite Kachel richtig ist:
  Der Picker kennzeichnet Verknüpftes und sperrt es, sonst ordnete ein zweiter Klick dasselbe Bild erneut
  zu.

## Rote Proben (erwartet / gemessen)

| Probe | erwartet | gemessen |
| --- | --- | --- |
| Einschränkung auf die verknüpfte Kachel entfernt | 1 | **1**, mit *exakt* der Meldung aus dem Beleg vom 2026-08-14 |
| Sperre am „schon dabei"-Treffer entfernt (`MediaPickers.tsx`) | 1 | **1** |

Die erste Probe ist der eigentliche Beleg: Aus einem Flackern, das man nur unter der Last des vollen Laufs
sieht, ist ein **deterministisch** reproduzierbarer Fall geworden — dieselben zwei Elemente, dieselben
`hasText`-Marker. Dass ein Flackern *fort* ist, lässt sich durch Laufen nicht beweisen; dass die Ursache
weg ist, schon.

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
- **2026-09-04** — **dritte Sichtung**, beim Verifizieren von B-210: voller Lauf **54 grün, 1 rot**
  (`bilder.spec.ts:77`), Einzellauf danach **1 passed**, zweiter voller Lauf **56/56**. Genau das Muster,
  das der Eintrag vom 2026-08-14 vorhersagt — und genau die Sorte Rot, die man beim zweiten grünen Lauf
  vergisst. Die Sichtung kam zuerst als eigene Story herein (B-213) und ist als Dublette verworfen.
- **2026-09-04** — **ausformuliert → abgenommen**. Gegrillt wurde **nicht**, und das ist hier kein
  übersprungener Schritt: Der Eintrag vom 2026-08-14 hat die Ursache mit erhaltener Fehlermeldung belegt,
  die Fix-Richtung benannt und die frühere Verdachtsrichtung (geteilte DB) ausdrücklich zurückgezogen — es
  gab keinen offenen Punkt, über den zu entscheiden gewesen wäre. Belege: `tsc -b` sauber, `bilder.spec.ts`
  allein grün, voller `playwright test` **56/56** grün. `frontend-reviewer` gelaufen, **kein Blocker**;
  seine sieben Funde sind eingearbeitet, bis auf einen, der als [B-214](B-214-store-zeile-per-teilstring-und-position.md)
  eigenständig weiterläuft. **Rollengang:** die Spec selbst fährt die Creator- und die Student-Rolle im
  echten Browser gegen einen echten Server; ein zusätzlicher Handgang wäre hier gegenstandslos, weil der
  Defekt **im Test** saß und nicht im Produkt. Commit `c5f6dbe`.
