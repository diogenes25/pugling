---
tags: [typ/story, status/abgenommen, bereich/frontend]
aliases: [Buch hinterlegt ohne exact, B-189 wiederholt]
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau 2026-09-02 zu Sprint 1 des Nachtlaufs
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-151]
nachgeschaut: 2026-09-02
wartet_auf: ""
---

# B-198 · Die Teilstring-Falle aus B-189 wurde im selben Commit neu gebaut

## User Story

Als **Entwickler** möchte ich, dass eine gerade diagnostizierte Testfalle nicht drei Dateien weiter erneut
entsteht, damit der volle Suite-Lauf nicht wieder aus einem Grund rot wird, der nichts über das Produkt
sagt.

## Ist-Stand am Code

`frontend/e2e/bestaetigung-ueberlebt.spec.ts:83`:

```ts
await expect(page.getByText("Buch hinterlegt.")).toBeVisible();
```

Ohne `{ exact: true }`. `getByText` vergleicht als **Teilstring**, und `ChildMaterialSection.tsx:65`
rendert „Noch kein **Buch hinterlegt.**" — solange `books.reload()` nach dem Anlegen läuft, hält
`useAsync` die alte, leere Seite; der Leer-Text steht also neben dem Banner. Zwei Treffer,
`strict mode violation`.

**Die Klickfolge ist exakt die aus [B-189](B-189-teilstring-zusicherung-bricht-im-vollen-lauf.md)**: frisch
angelegtes Kind → leere Bücherliste → „Buch hinterlegen".

**Was daran mehr ist als ein vergessenes Flag:** Commit `e479d83` hat diese Falle eine Datei weiter
(`kind-lehrbuch-fach.spec.ts:51`) **diagnostiziert und behoben** — und sie im selben Commit hier neu
eingebaut. `6688ff5` hat danach gezielt beide „Gespeichert."-Zusicherungen dieser Datei mit
`{ exact: true }` nachgezogen und Zeile 83 übersehen.

**Warum der 43/43-Beleg der Abnahme das nicht ausschließt:** B-189s eigene Messung sagt, dass genau dieses
Konstrukt **allein grün** ist und erst unter der Last des vollen Laufs rot wird. Der grüne Lauf ist also
kein Gegenbeweis, sondern derselbe Zufall, der B-189 wochenlang verborgen hat.

## Entscheidungen

1. **`{ exact: true }` an der einen Zeile**, wie in B-189. *Begründung:* Der Produkttext ist richtig,
   falsch war die Zusicherung. *Kosten:* keine.
2. **Gezählt wird, und die Zahl steht im `## Verlauf`** (Empfehlung des offenen Punkts bestätigt).
   *Begründung:* Ob daraus ein Wächter wird, ist eine Frage der Häufigkeit — und B-189s Entscheidung 2 hat
   einen Wächter mit Begründung verworfen. Diese Entscheidung ohne neue Zahl zu wiederholen wäre eine
   Meinung. *Kosten:* ein Zählschritt.
3. **Kein Wächter in dieser Story, egal wie die Zahl ausfällt.** *Begründung:* Ein Tor über `getByText`
   ohne `exact` bräuchte sofort eine Ausnahmeliste, denn Teilstring ist der Normalfall. Was die Zahl
   entscheidet, ist nur, ob es eine **eigene** Story wert ist. *Kosten:* Eine vierte Wiederholung ist
   möglich — ausgesprochen.

## Akzeptanzkriterien

1. `bestaetigung-ueberlebt.spec.ts:83` trifft nur den Banner, nicht den Leer-Text.
2. Der **volle** Suite-Lauf ist grün (allein grün beweist bei dieser Falle nichts, siehe B-189).

## Belege je Kriterium

| AK | Beleg |
|---|---|
| 1 | die Zeile selbst — und eine Zählung der übrigen `getByText`-Aufrufe ohne `exact` im `## Verlauf` |
| 2 | voller Playwright-Lauf am Ende des Sprints |

## Schätzung

- **Größe: XS** — eine Zeile plus die Zählung. **Migration/Vertragsbruch: nein.**
- **Testweg:** der volle Playwright-Lauf. Ein eigener Test wäre sinnlos: Die Falle *ist* ein Test.

## Verlauf

- 2026-09-02 · Aufgenommen aus der **Nachschau** zu Sprint 1 (zweiter Nachtlauf).
- 2026-09-02 · **`ausformuliert` → `gegrillt` → `geschaetzt`** (dritter Sprint, Freigabe 1). XS.
- 2026-09-02 · **`geschaetzt` → `in-arbeit` → `abgenommen`** (Sprint 3). Belege:
  - **Reviewer:** `frontend-reviewer`, zwei Runden. Runde 1 setzte **einen Blocker** (siehe unten),
    Runde 2 steht aus bzw. ist in den Verlauf eingearbeitet.
  - **Rollengang:** maschinell — voller Playwright-Lauf, echter Chromium gegen echten Server.
  - **Zahlen:** `npx tsc -b` sauber · `npm run test` **327/327**.
  - **Commits:** `6843d42` (Bau) und der Abnahme-Commit dieses Sprints.
  - **Die Zählung, die Entscheidung 2 verlangt** (am 2026-09-02 erhoben): **118** `getByText`-Aufrufe in
    17 Spec-Dateien; davon **29** mit einem Literal und ohne `{ exact: true }`. Behoben wurden die, deren
    Text nachweislich in einem längeren gerenderten Text steckt: die drei „Buch hinterlegt."-Fälle
    (`bestaetigung-ueberlebt`, `lehrwerke`, plus der bereits in B-189 behobene) und vier
    „Gespeichert."-Fälle in `lehrwerk-bearbeiten` und `lehrwerke`.
  - **Die Messmethode selbst musste zweimal geschärft werden, und das gehört dazu:** Der erste Anlauf
    zählte **Quelltextzeilen** als gerenderten Text und meldete 24 Mehrdeutigkeiten — eine Zahl, die
    nichts bedeutete. Erst die Beschränkung auf echte String-Literale brachte die belastbare Klasse
    heraus, und mit ihr `lehrwerke.spec.ts` als **dritten** Fall, den die Story nicht kannte.
  - **Entscheidung 3 bleibt: kein Wächter.** Bei 29 Kandidaten und einer Handvoll echter Fälle bliebe ein
    Tor eine Ausnahmeliste. Die Zahl ist jetzt da; wer sie später anders bewertet, hat eine Grundlage.
- 2026-09-02 · **Nachgeschaut** (Nachtlauf, Retro Sprint 1): kein Befund.
  `bestaetigung-ueberlebt.spec.ts:83` trägt `{ exact: true }` samt Begründung, warum es tragend ist (der
  Leer-Text enthält die Erfolgsmeldung als Teilstring). AK 2 hängt am vollen Suite-Lauf und wird von
  diesem Sprint mitgetragen.
