---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/creator]
aliases: [B-173 AK 2 nicht gebaut, Loeschen ohne Meldung]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau 2026-09-02 zu Sprint 1 des Nachtlaufs
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-173]
wartet_auf: ""
---

# B-196 · Das Löschen einer Reihe meldet nichts — und lässt die Bestätigung von vorhin stehen

**Das ist ein Akzeptanzkriterium, das abgenommen wurde, ohne gebaut zu sein.** B-173s AK 2 nennt das
Löschen ausdrücklich, und ihre Entscheidung 1 begründet den Meldungsplatz auf Seitenebene sogar damit,
dass er „auch die Sortier-Variante (Umbenennen) und das Löschen abdeckt".

## User Story

Als **Creator** möchte ich nach dem Löschen einer Reihe eine Bestätigung sehen, die zum Löschen gehört,
damit ich nicht die Erfolgsmeldung von vorhin für sie halte.

## Ist-Stand am Code

`VaterLehrwerke.tsx:251-256`:

```ts
if (await action.run(() => api.deleteTextbookSeries(series.id))) onChanged();
```

Kein `okText`, kein `onMessage(…)`. Ein geglücktes Löschen erzeugt **gar keine** Meldung — weder in der
Zeile (die ohnehin verschwindet) noch am Seitenplatz.

**Und der zweite Teil ist der schlimmere:** `remove()` **räumt** den Seitenplatz auch nicht. Nach „Reihe A
speichern → Reihe B löschen" steht das grüne „Gespeichert." von A über der gerade verschwundenen Zeile B
und liest sich als Bestätigung des Löschens. Das ist die Fehlerfamilie von B-173 selbst, nur in die
andere Richtung: **eine Meldung, die zu einer anderen Handlung gehört als der, die man gerade getan hat.**

## Die echte Lücke

Der Seitenplatz ist zustandsbehaftet, die Regel „der Erfolg reist, Fehler bleiben lokal" ist
zustandsfrei formuliert. Wo eine Handlung gar keine Meldung erzeugt, bleibt die vorige stehen.

## Entscheidungen

1. **Das Löschen meldet, und es nennt die Reihe.** *Begründung:* Der Seitenplatz überlebt die Zeile — er
   ist der einzige Ort, an dem eine Löschbestätigung überhaupt stehen kann. Und er muss den **Namen**
   tragen: Die Zeile, auf die sich die Meldung bezieht, ist ja gerade verschwunden; „Gelöscht." allein
   ließe offen, was. *Kosten:* Der Text ist länger als die übrigen Meldungen dieser Fläche.
2. **Gebaut wird zusammen mit [B-197](B-197-meldungsplatz-wird-beim-absenden-nicht-geraeumt.md)**
   (Empfehlung des offenen Punkts bestätigt). *Begründung:* Beide brauchen dasselbe Prop; getrennt
   entstünden zwei Muster für einen Platz. *Kosten:* keine — derselbe Sprint.
3. **Die übrigen stummen Erfolgspfade bleiben draußen.** *Begründung:* Nur der Reihen-Löschpfad ist von
   einem Akzeptanzkriterium **beansprucht** (B-173, AK 2); die anderen waren vor wie nach Sprint 1 stumm
   und gehören keiner Story. Sie mitzunehmen hieße, eine geschätzte Story während des Bauens wachsen zu
   lassen. *Kosten:* Bekannte stumme Stellen bleiben stehen — ausgesprochen, nicht kaschiert, und als
   [B-200](B-200-drei-stumme-erfolgspfade.md) abgelegt.

   **Korrektur an der eigenen Zählung (2026-09-02, Fund des `frontend-reviewer`):** Diese Entscheidung
   nannte ursprünglich **zwei** stumme Pfade und wies das als gemessen aus. Es sind **vier**:
   `VaterLehrwerke.tsx` (Reihe löschen — hier behoben), `ChildMaterialSection.tsx` (Buch entfernen),
   `VaterFachlehrer.tsx` (Fachlehrer löschen) und `VaterLehrwerke.tsx` (Unit löschen). Die *Entscheidung*
   trägt unverändert — die als Beleg ausgewiesene **Zahl** trug nicht, und eine falsche Zahl in einer
   Begründung ist schlimmer als gar keine.

## Akzeptanzkriterien

1. Eine Reihe löschen → am Seitenplatz steht eine Bestätigung, die den Namen der Reihe nennt.
2. Vor dem Löschen wird der Platz geräumt: eine Bestätigung von vorhin kann nicht als Löschbestätigung
   stehenbleiben.
3. Bricht der Nutzer den Bestätigungsdialog ab, ändert sich am Platz nichts.

## Belege je Kriterium

| AK | Beleg |
|---|---|
| 1 | neuer E2E-Fall in `e2e/bestaetigung-ueberlebt.spec.ts` — **die rote Probe dieser Story** |
| 2 | derselbe Fall: er speichert **zuerst** eine andere Reihe, damit eine alte Meldung dasteht |
| 3 | **nicht geprüft** — `confirmAction` ist `window.confirm`; ein Abbruch verlässt `remove()` vor jedem `action`-Aufruf (`VaterLehrwerke.tsx:252-254`, am Code gelesen). Ein E2E dafür bräuchte einen abgelehnten Dialog und beliefe die Spec um einen Fall, der nichts messen kann, was der frühe `return` nicht schon garantiert |

## Schätzung

- **Größe: S.** Anker: zwei Zeilen in `remove()` plus ein E2E-Fall; das tragende Prop entsteht in B-197.
- **Migration/Vertragsbruch: nein.**
- **Testweg:** `frontend/e2e/bestaetigung-ueberlebt.spec.ts` (dieselbe Fläche, dieselbe Datei).

## Verlauf

- 2026-09-02 · Aufgenommen aus der **Nachschau** zu Sprint 1 (zweiter Nachtlauf). Der Fund ist ernster als
  ein gewöhnlicher Defekt: Er belegt, dass die Abnahme von B-173 ein eigenes Akzeptanzkriterium nicht
  geprüft hat. Der Rollengang (43/43 E2E) hat es nicht gefangen, weil keine Spec eine Reihe löscht.
- 2026-09-02 · **`ausformuliert` → `gegrillt` → `geschaetzt`** (dritter Sprint, Freigabe 1). Der offene
  Punkt 1 ist **gezählt statt vermutet**: zwei stumme Erfolgspfade, nur einer von einem
  Akzeptanzkriterium beansprucht. Akzeptanzkriterium 3 trägt ausdrücklich ein „nicht geprüft, weil …"
  statt eines behaupteten Belegs.
- 2026-09-02 · **`geschaetzt` → `in-arbeit` → `abgenommen`** (Sprint 3). Belege:
  - **Reviewer:** `frontend-reviewer`, zwei Runden. Runde 1 setzte **einen Blocker** (siehe unten),
    Runde 2 steht aus bzw. ist in den Verlauf eingearbeitet.
  - **Rollengang:** maschinell — voller Playwright-Lauf, echter Chromium gegen echten Server.
  - **Zahlen:** `npx tsc -b` sauber · `npm run test` **327/327**.
  - **Commits:** `6843d42` (Bau) und der Abnahme-Commit dieses Sprints.
  - **Rote Probe, gezielt:** `Locator: getByText('Reihe „…" gelöscht.')` — element not found. Eine eigene
    Gegenprobe war **nötig**, weil der gemeinsame E2E-Fall schon an B-197s Zusicherung abbricht; „der
    Lauf war rot" wäre für diese Story kein Beleg gewesen.
  - **Akzeptanzkriterium 3 (Dialog-Abbruch) trägt am Code**, vom Reviewer gegengelesen: `confirmAction`
    steht **vor** `onMeldung(null)`, ein Abbruch lässt den Platz unberührt. Die Reihenfolge ist gewählt,
    nicht zufällig.
  - **Die eigene Zählung war falsch** — Korrektur samt Begründung steht oben in Entscheidung 3, die drei
    übrigen Pfade als [B-200](B-200-drei-stumme-erfolgspfade.md).
