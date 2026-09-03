---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/tests, rolle/supervisor]
aliases: [rechtmaessiges null zeigt Lade, showPlaceholder haengt am Versuch, Unit-Formular verschwindet]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
nachgeschaut: 2026-09-03
quelle: Code-Review 2026-09-02 zu den 19 Commits vor origin/main
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-184, B-63]
wartet_auf: ""
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
3. **Die sechs genannten Aufrufstellen werden hier *nicht* umgestellt — zurückgestellt.**
   ~~Begründung: sie lesen `data === null` selbst und tragen dieselbe Anfälligkeit.~~ **Diese Begründung
   ist falsch, vom `frontend-reviewer` nachgesehen:** `VaterPlanDetail.tsx:29` liest
   `plan.loading && plan.data === null`, `VaterProfil.tsx:54` liest `loading && !hasData`,
   `SohnProgress.tsx:17` ein blankes `loading` — **keine** dieser Stellen kann die dauerhafte Lüge
   erzeugen. Zurückstellen bleibt richtig, aber aus dem anderen Grund: es gibt dort **keinen Defekt**, den
   diese Story zu beheben hätte, und `SohnProgress` gehört ohnehin zu einer anderen Familie (B-116-Flackern,
   Sohn-Arcade). *Kosten:* keine — die Annahme, es liege dort Arbeit, war der Fehler.

## Schätzung

**Größe: S** — vier Zeilen Produktivcode in zwei Dateien, dazu zwei Komponententests.

- **`wo: frontend`** — `lib/useAsync.ts` und `vater/VaterLehrwerke.tsx`. Kein Backend berührt.
- **`migration: nein`**, **`vertragsbruch: nein`** — kein Schema, kein DTO.

**Risiko:** ~~Das Primitiv trägt 41 Platzhalter im Vater-Web.~~ **Überzeichnet, vom `frontend-reviewer`
nachgezählt:** `showPlaceholder` wird an **fünf** Stellen gelesen (`VaterLehrwerke.tsx:563,638`,
`VaterKind.tsx:94`, `ChildMaterialSection.tsx:64,132`, `VaterWizard.tsx:547` als Prop). Die 41 sind
Platzhalter *überhaupt*, die meisten in der Form `loading && data === null` — die diese Änderung nicht
berührt. Der Beleg liegt trotzdem an den **drei bestehenden** Fällen von B-184 (Kriterium 3), denn fünf
Stellen sind noch immer fünf mehr als die zwei geänderten.

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
- 2026-09-02 · **`frontend-reviewer` gelaufen** (Freigabe des Nutzers). **Kein Blocker**, aber ein
  Korrektheitsfund am **Beleg** — und er trifft die Regel, die ich in derselben Sitzung notiert habe:
  **Der zweite neue Testfall konnte aus seinem eigenen Grund nicht fallen.** `act` entleert die
  Microtask-Kette, `Promise.resolve(null)` ist danach durch, `loading` wieder `false` — das Ladefenster,
  um das es geht, wurde nie beobachtet. Selbst nachgemessen statt geglaubt: mit der alten Bedingung fiel
  der Fall an **Zeile 88** (dem `waitFor` der *ersten* Ladung), nicht an Zeile 91. Er belegte damit
  denselben Satz wie der Fall vor ihm; Kriterium 2 war **implementiert, aber nicht gemessen**.
  Neu gefasst mit **zwei `deferred`**, die zweite Ladung bleibt offen, und `expect(loading).toBe(true)`
  belegt, dass das Fenster wirklich offen ist.
  **Die passende rote Probe ist jetzt eine andere:** `setSettled(false)` im Effekt (also „bei jedem
  Ladeversuch zurücksetzen") — gemessen, **beide** Neulade-Fälle fallen an ihrer gemeinten Zusicherung
  (`useAsync.test.tsx:105` und `:132`), und `loading` auf der Zeile darüber hält. Die vorige Probe (alte
  Bedingung) belegte diesen Fall nicht.
  **Der bestehende B-184-Fall daneben trug dieselbe Schwäche** (er war die Vorlage, von der mein Fall den
  Fehler geerbt hat) und ist mitgezogen, statt als leere Zusicherung neben der reparierten zu bleiben.
  Zwei weitere Funde eingearbeitet: die **Prop-Doku** in `VaterWizard.tsx:646` beschrieb noch die
  *entfernte* Bedingung (jetzt Verweis statt Nachformulierung, wie `frontend/CLAUDE.md` es verlangt), und
  die **dünner gewordene Marge** bei `VaterWizard.tsx:547` steht als Kommentar dort: heute wirkungsgleich,
  aber sobald der Assistent ein Fach per Query vorbelegt, ergibt „erste Suche gescheitert, dann Filter
  gewechselt" wieder den falschen Satz aus B-162.
  **Zwei Behauptungen dieser Story hat der Reviewer widerlegt** und sie sind oben durchgestrichen: die
  „41 Platzhalter" (es sind fünf Leser) und die Begründung von Entscheidung 3.
  **Ausdrücklich geprüft und ohne Änderung gelassen:** `settled` wird bei einem Dep-Wechsel nicht
  zurückgesetzt. Der Reviewer hat nachgewiesen, dass die neue Bedingung eine **Abschwächung** ist — der
  Platzhalter kann nirgends neu *auftauchen*, nur ausbleiben — und dass **B-164 unberührt** bleibt (dort ist
  `data` nicht-`null`, für nicht-`null` waren beide Bedingungen `false`). Empfehlung „nichts ändern"
  übernommen; die Kopplung ist in B-164 vermerkt. Der **401-Pfad** ist richtig und mit der neuen Bedingung
  sogar besser (die alte hinterließ dort ein „Lade…").
  **Vom Reviewer unabhängig nachgefahren:** `npm run build` sauber, 23/23 in den beiden berührten
  Testdateien.
- 2026-09-02 · `in-arbeit` → **`abgenommen`**. **329/329** vitest grün, `tsc --noEmit` sauber, Reviewer
  gelaufen und eingearbeitet, rote Probe an der gemeinten Zusicherung gemessen. Commit `81eb60c` plus der
  Nachtrag dieser Runde. Der **Rollengang** fehlt weiter und die Lücke bleibt benannt: Kriterium 4 braucht
  ein *gescheitertes* Lesen der Units, also einen künstlich kaputten Server — die argumentierte Ausnahme
  des Bereichs-READMEs. Gedeckt ist die **Bedingung** am Primitiv, nicht ihr Einbau.
