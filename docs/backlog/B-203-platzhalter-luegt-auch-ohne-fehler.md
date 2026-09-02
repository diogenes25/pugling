---
tags: [typ/story, status/in-arbeit, bereich/frontend, bereich/tests, rolle/supervisor]
aliases: [rechtmaessiges null zeigt Lade, showPlaceholder haengt am Versuch, Unit-Formular verschwindet]
status: in-arbeit
prio: P2
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Code-Review 2026-09-02 zu den 19 Commits vor origin/main
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-184, B-63]
wartet_auf: frontend-reviewer
---

# B-203 · Der Platzhalter lügt auch ohne Fehler — und ein misslungenes Lesen nahm dem Vater eine Fähigkeit

Zwei Fälle, **eine** Wurzel und darum eine Story (Vorbild
[B-171](B-171-zwei-zusicherungen-pruefen-den-ausgangszustand.md)): die Bedingung von `showPlaceholder`.
B-184 hat sie eingeführt, um „Lade…" nach einem **Fehler** zu beenden. Sie beendet es aber nur dann — und
genau daran hängt der zweite Fall.

## User Story

Als *Vater* möchte ich, dass ein Bildschirm entweder Daten, einen Fehler oder ein ehrliches „Lade…" zeigt
und dass ein misslungenes Lesen mir keine Bedienelemente wegnimmt, damit ich nicht F5 drücken muss, um
weiterarbeiten zu können.

## Ist-Stand am Code (vor der Reparatur)

**Fall 1 — die Bedingung** (`useAsync.ts:53`):

```ts
showPlaceholder: data === null && error === null
```

Ein `fn`, das **rechtmäßig** `null` liefert, setzt keins von beidem — der Platzhalter bleibt für immer
wahr. Anders als im Fehlerfall von B-184 steht daneben nicht einmal ein Banner, das die Lüge verrät.
Der Review nennt sechs Aufrufstellen, deren `fn` genau das kann: `SohnProgress.tsx:12-13`,
`MediaPickers.tsx:86`, `VaterPlanDetail.tsx:18`, `VaterProfil.tsx:27,29`. Sie lesen heute `data === null`
direkt — was die Doku des Feldes selbst „nie richtig" nennt und zur Übernahme einlädt.

**Fall 2 — der erste Verbraucher** (`VaterLehrwerke.tsx:620`):

```tsx
{series.isOwn && units.data !== null && ( … „Unit hinzufügen" … )}
```

Das Tor stammt aus B-63 und ist begründet: `UnitForm` liest seine Vorbelegung im `useState`-Anfangswert,
also genau einmal beim Einhängen. Scheitert das Lesen der Units, bleibt `data` aber **für immer** `null` —
und mit ihm verschwindet das ganze Anlegen-Formular, nicht bloß die Liste. Einen zweiten Weg zurück gibt es
nicht.

## Die echte Lücke

B-184 hat die Bedingung an *zwei Zustände* gehängt (`data`, `error`), obwohl die Frage, die sie beantwortet,
**einen** hat: *ist der erste Versuch durch?* Solange „durch" nur über seine beiden Ergebnisse erschlossen
wird, fehlt jedes Ergebnis, das keins von beidem setzt.

Fall 2 zeigt, was das kostet: Das Tor war nie als Fehlerbehandlung gedacht, wurde aber zu einer — und zwar
zu der schlechtesten Sorte, die eine Fähigkeit entfernt, die vom gelesenen Wert nicht abhängt. Die
Vorbelegung ist bloß Bequemlichkeit (Klasse und Buchart der letzten Unit); `newUnitDefaults([])` ist ein
seit B-63 **getesteter** Eingabewert (`VaterLehrwerke.test.tsx:72`).

## Akzeptanzkriterien

1. Ein `fn`, das rechtmäßig `null` liefert, beendet den Platzhalter, sobald der Versuch durch ist — ohne
   Daten und ohne Fehler.
2. Ein `reload` bringt den Platzhalter **nicht** zurück, auch nicht nach einer rechtmäßigen
   `null`-Ladung — sonst nimmt er das Formular darunter mitsamt seinem Zustand aus dem DOM, und das ist die
   Regel, die dieses Feld überhaupt begründet.
3. Die drei bestehenden Fälle von B-184 bleiben grün: der Fix ist **additiv**, nicht ein Austausch.
4. Das Anlegen-Formular der Units steht nach einem **gescheiterten** Lesen zur Verfügung und startet mit der
   neutralen Vorbelegung.
5. Es steht während der **ersten** Ladung weiterhin **nicht** da — sonst startete es mit falschen
   Vorbelegungen, und die nachgelieferten kämen nie an.

## Entscheidungen

1. **Die Bedingung hängt an einem eigenen Zustand `settled`, nicht an einem vorangestellten `loading &&`.**
   Der Review schlug letzteres vor. Es hätte Fall 1 behoben und Kriterium 2 gebrochen: nach einer
   rechtmäßigen `null`-Ladung wäre bei jedem `reload` wieder `loading && data === null && error === null`
   wahr — der Platzhalter käme zurück und hängte das Formular aus. *Kosten:* Ein `useState` mehr im
   Primitiv. Dafür beantwortet die Bedingung genau die eine Frage, die sie stellt, statt sie aus zwei
   anderen zu erschließen.
2. **Fall 2 wird auf `!units.showPlaceholder` gestellt, nicht auf `units.data ?? []` allein.** Der Review
   schlug letzteres vor. Es hätte das Formular schon **während** der ersten Ladung gerendert — genau das,
   was Kriterium 5 und der Kommentar an der Stelle verhindern. `showPlaceholder` trifft beide Hälften und
   ist laut eigener Doku für „Fläche mit Formular" gedacht. *Kosten:* Fall 2 hängt jetzt an Fall 1; ohne
   Entscheidung 1 wäre `showPlaceholder` an dieser Stelle das falsche Tor. Die Reihenfolge ist damit
   zwingend und steht im Angriffsplan.
3. **Die sechs genannten Aufrufstellen werden hier *nicht* umgestellt — zurückgestellt.** Begründung: Der
   Review verankert den Fund am Primitiv, und die sechs sind heute nicht defekt, sondern **einladend**: sie
   lesen `data === null` selbst und tragen dieselbe Anfälligkeit. Sie umzustellen ist eine eigene Handlung
   an sechs Seitenkomponenten, die am Netz hängen und darum ohne gefälschtes `fetch` nicht testbar sind.
   *Kosten:* Die Fehlerklasse bleibt an sechs Stellen erreichbar, und das gehört benannt statt zugedeckt —
   wenn es zählt, ist es eine eigene Story.

## Schätzung

**Größe: S** — vier Zeilen Produktivcode in zwei Dateien, dazu zwei Komponententests.

- **`wo: frontend`** — `lib/useAsync.ts` und `vater/VaterLehrwerke.tsx`. Kein Backend berührt.
- **`migration: nein`**, **`vertragsbruch: nein`** — kein Schema, kein DTO.

**Risiko:** Das Primitiv trägt 41 Platzhalter im Vater-Web. Eine Änderung an seiner Bedingung wirkt überall,
und genau darum liegt der Beleg an den **drei bestehenden** Fällen von B-184 (Kriterium 3) und nicht nur an
den neuen.

**Angriffsplan:** Erst das Primitiv (Entscheidung 1), dann der Verbraucher (Entscheidung 2) — die
umgekehrte Reihenfolge ergibt an der zweiten Stelle ein falsches Tor.

**Testweg**: `frontend/src/lib/useAsync.test.tsx` (zwei neue Fälle neben den drei von B-184);
`newUnitDefaults([])` ist in `VaterLehrwerke.test.tsx:72` bereits gepinnt.

## Verlauf

- 2026-09-02 · Aufgenommen aus dem **Code-Review** der 19 Commits vor `origin/main` und im selben Zug
  **gebaut**. Zuordnung aus dem Git belegt: `git log -S showPlaceholder` nennt `e479d83` (Sprint 1, B-184),
  `git log -S 'units.data !== null'` nennt `6978fdc` (B-63) — daher `entgangen_bei: [B-184, B-63]`.
  **Verifikation:** 329/329 vitest grün (+2 neu) in 38 Dateien, `tsc --noEmit` sauber. Commit `81eb60c`.
  **Rote Probe:** alte Bedingung `data === null && error === null` wieder eingesetzt → **beide neuen Fälle
  fallen** (`expected true to be false`), die **drei bestehenden bleiben grün**. Damit ist Kriterium 3 nicht
  behauptet, sondern gemessen.
- 2026-09-02 · Bleibt auf `in-arbeit`: Der `frontend-reviewer` ist **nicht gelaufen** (diese Sitzung startet
  keine Agenten unaufgefordert). Alles andere ist belegt, `wartet_auf` benennt es.
  **Der Rollengang fehlt und die Lücke ist benannt, nicht zugedeckt:** Kriterium 4 verlangt ein
  *gescheitertes* Lesen der Units, und das braucht einen künstlich kaputten Server — genau die Ausnahme, die
  das Bereichs-README als argumentiert zulässt. Gedeckt ist damit die **Bedingung** (am Primitiv, im
  Komponententest), nicht ihr Einbau in `VaterLehrwerke` — dieselbe ausgesprochene Grenze, die schon B-184
  gezogen hat.
