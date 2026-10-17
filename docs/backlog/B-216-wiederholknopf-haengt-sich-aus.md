---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor, rolle/student]
aliases: [Erneut suchen verschwindet unter dem Finger, Fehlerbanner haengt aus, useGemerkterFehler]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: frontend-reviewer zu Sprint 1 des Nachtlaufs 2026-09-04 (zu B-188)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
nachgeschaut: "2026-10-17: Zustandsuebergaenge des Hooks (Fehler→reload→gleicher Fehler, Erfolg, nie-laedt) und Fokus-Reihenfolge nachgerechnet; Grenze bei Dep-Wechsel gefunden → B-227; gefuellte Shop-Karte ohne Fokusregel → B-228; AK 3 berichtigt"
---

# B-216 · Zwei Wiederhol-Knöpfe verschwinden im Moment des Klicks unter dem Finger

`useAsync.reload()` setzt `error` auf `null` und `loading` auf `true` **im selben Schritt**
(`frontend/src/lib/useAsync.ts:47-48`). Ein Fehlerbanner, das allein an `error` hängt, hängt sich damit in
genau dem Moment aus, in dem sein eigener Knopf gedrückt wird.

## User Story

Als **Vater** (Übungssuche im Assistenten und im Planbau) und als **Sohn** (Kaufverlauf im Shop) möchte ich,
dass der Knopf „Erneut suchen" bzw. „Nochmal versuchen" nach dem Klick stehen bleibt und „Lädt…" sagt,
damit ich sehe, dass die Wiederholung läuft, und der Fokus nicht ins Leere fällt.

## Ist-Stand am Code

Nachgesehen am 2026-10-17. Es sind **drei** Stellen, nicht zwei — die dritte ist der ausdrücklich so
benannte „Zwilling" des Assistenten:

1. **`frontend/src/vater/VaterWizard.tsx:759-765`** (`WizardTrefferbereich`): `{fehler && (<div
   className="banner err" …><button disabled={laedt} onClick={onRetry}>Erneut suchen</button></div>)}`.
   Das Banner hängt an `fehler` allein; `disabled={laedt}` ist unerreichbar.
2. **`frontend/src/vater/PlanPositions.tsx:537-543`** (`AddPosition`): wörtlich dieselbe Bauform
   (`{exercises.error && (… disabled={exercises.loading} onClick={exercises.reload}>Erneut suchen …)}`),
   der Kommentar davor nennt sie „Derselbe Aufbau wie im Zwilling (`VaterWizard.tsx`)". In der Story vom
   2026-09-04 nicht gezählt.
3. **`frontend/src/sohn/SohnShop.tsx:323-334`** (`HistoryTab`, leere Liste): der Fehlerzweig
   `if (error != null) return (<… <button disabled={loading} onClick={onLoadMore}>{loading ? "Lädt…" :
   "Nochmal versuchen"}</button>)` steht hinter `purchases.length === 0`; danach `if (loading) return null`.
   Der Lader `loadMoreHistory` (`SohnShop.tsx:78-79`) setzt `setHistoryLoading(true)` und
   `setHistoryError(null)` im selben Tick — kein `useAsync`, dieselbe Choreographie. Nach dem Klick fällt
   die Karte in den `loading`-Zweig und rendert **nichts**: Knopf weg, Fokus auf `<body>`. Die Beschriftung
   „Lädt…" an Zeile 332 ist damit toter Code — im Zustand, den sie beschreibt, existiert der Knopf nicht.

**Die Reparatur ist vorgeführt:** `ListenAusfall` (`VaterWizard.tsx:661-693`, B-188) hält den letzten Fehler
in einem `useState` + `useEffect` fest, solange geladen wird, und beschriftet den Knopf zu „Lädt…" um.
Bewacht von `VaterWizard.test.tsx:115-141` („bleibt während der Wiederholung stehen", „räumt sich ab, wenn
die Wiederholung geklappt hat").

**Bestehende Zusicherungen an den drei Stellen:** `VaterWizard.test.tsx:45` klickt „Erneut suchen" und prüft
nur den Aufruf von `onRetry`; `HistoryTab.test.tsx:58-68` ebenso für „Nochmal versuchen"; die E2E
`ausfall-wird-genannt.spec.ts:60-71` klickt den Knopf und wartet auf das Ergebnis. **Keine** prüft den
Zustand *während* der Wiederholung — darum ist der Defekt an allen drei Stellen grün.

## Die echte Lücke

Dieselbe Regel steht an einer Stelle richtig (`ListenAusfall`) und fehlt an dreien. Es ist keine Frage des
`useAsync` (die dritte Stelle benutzt ihn nicht), sondern der Darstellung: wer ein Banner allein an `fehler`
hängt, verliert es beim Start der Wiederholung.

## Offene Punkte

1. ~~Beide Stellen auf `ListenAusfall` umstellen, oder nur das Gedächtnis-Muster übernehmen?~~ →
   Entscheidung 1.
2. ~~Gehört das Gedächtnis stattdessen in `useAsync` selbst?~~ → Entscheidung 2.

## Entscheidungen

Autonom gegrillt am 2026-10-17 unter Freigabe 1 (`art: Defekt`).

1. **Das Gedächtnis wird ein geteilter Hook `useGemerkterFehler(fehler, laedt)` unter `src/lib/`, und
   `ListenAusfall` benutzt ihn ebenfalls.** Nicht der ganze Baustein, nur die Regel: Der Trefferbereich
   trägt mehr als ein Banner (Zählzeile, Leer-Satz, Platzhalter), und die Shop-Karte hat eine andere Form
   (Knopf *unter* dem Banner, kein Satzanfang). *Begründung:* dasselbe Messargument wie bei `ReferenceSelect`
   (B-172, Entscheidung 2) — vier von vier Stellen brauchen dieselbe Regel, und dreimal von drei hat die
   Kopie hier verloren. Ein Hook ist die kleinste Einheit, die die Regel trägt, ohne die Darstellung zu
   diktieren. *Kosten:* ein neuer Hook mit Vitest; `ListenAusfall` wird umgebaut, seine vier bestehenden
   Tests bleiben das Netz. Wer künftig ein Banner an `fehler` allein hängt, bleibt ungefangen — bewusst,
   kein Quelltext-Tor (B-172, Entscheidung 2, dieselbe Abwägung).
2. **`useAsync` bleibt unangetastet.** Ein querschnittliches Feld dort wäre genau die Frage, die
   [B-164](B-164-useasync-paart-frischen-zustand-mit-alten-daten.md) offen hat („Feld im Hook oder Muster je
   Aufrufer") und die ausdrücklich beim Nutzer liegt. Der Hook aus Entscheidung 1 ist *Muster je Aufrufer*,
   nur ohne Kopie — er greift der Architekturfrage nicht vor, weil er an keiner der 105 `useAsync`-Stellen
   etwas ändert, die ihn nicht aufrufen. *Kosten:* vier Aufrufstellen statt null; und fällt B-164 später
   für „Feld im Hook", ist dieser Hook der Kandidat, der dort aufgeht.
3. **Alle drei Knöpfe sagen während der Wiederholung „Lädt…"**, wie `ListenAusfall` und wie der Verlauf
   schon wollte. *Begründung:* Rückmeldung während der Wiederholung ist Folge 2 des Defekts; ein
   unveränderter Knopf, der nur gesperrt ist, sagt nicht, *dass* etwas läuft. *Kosten:*
   `ausfall-wird-genannt.spec.ts` greift den Knopf vor dem Klick per Name — das bleibt gültig; wer ihn
   *während* des Ladens greift, muss „Lädt…" nehmen.
4. **Die dritte Stelle (PlanPositions) gehört in diese Story**, kein Split. *Begründung:* ein Fund beim
   Ausformulieren, nicht beim Bauen; dieselbe Zeile Code. *Kosten:* eine E2E-Fläche mehr im Rollengang.
5. **Der Fokus wandert beim Klick auf den Kasten (`tabIndex={-1}`), nicht auf den Knopf** – aus der
   Bau-Runde, im Browser gemessen: Ein Knopf, der `disabled` wird, **verliert in Chromium den Fokus**
   (Playwright meldete `inactive`, obwohl der Knopf im DOM stand). Folge 1 der Story wäre mit dem bloßen
   Stehenlassen also nur halb behoben gewesen. *Begründung:* Der Kasten ist das Element, das gerade sagt,
   was passiert; scheitert die Wiederholung erneut, ist der Knopf von dort per Tab erreichbar. Die
   Alternative – den Knopf nicht zu sperren (`aria-disabled`) – bräche die Hausregel `disabled={busy}`
   (`frontend/CLAUDE.md`) an vier Stellen und wäre eine Stilentscheidung, keine Reparatur. *Kosten:* ein
   `useRef` je Stelle; gelingt die Wiederholung, verschwindet der Kasten und der Fokus fällt auf das
   Dokument – ein Zustand ohne Fehler, in dem es nichts mehr zu fokussieren gibt. Gilt für alle vier
   Stellen, auch `ListenAusfall`, das denselben Verlust schon vorher hatte.

## Akzeptanzkriterien

1. An allen drei Stellen bleibt nach dem Klick auf den Wiederhol-Knopf das Banner samt Knopf im DOM,
   solange die Wiederholung läuft; der Knopf ist `disabled` und heißt „Lädt…"; der Fokus liegt auf dem
   Banner, nicht auf `<body>` (Entscheidung 5). *Beleg:* Vitest `useGemerkterFehler.test.ts` (Regel), `VaterWizard.test.tsx` (Trefferbereich),
   `HistoryTab.test.tsx` (Shop-Karte); E2E `wiederholknopf-bleibt.spec.ts` (Assistent + Planbau, mit
   verzögerter Antwort) und `shop-verlauf-wiederholen.spec.ts` (Sohn).
2. Gelingt die Wiederholung, verschwindet das Banner; scheitert sie erneut, zeigt es den neuen Fehler.
   *Beleg:* Vitest wie oben; die E2E lassen den Abruf nach der Verzögerung gelingen.
3. `ListenAusfall` verhält sich in der Sache unverändert – seine vier Tests ziehen unverändert nach
   `ListenAusfall.test.tsx` um und bleiben grün. Neu ist nur der Fokus-Umzug auf den Kasten (Entscheidung 5),
   den kein Test misst (Nachschau 2026-10-17: die erste Fassung dieses Kriteriums widersprach Entscheidung 5).
4. `useAsync.ts` ist im Diff nicht enthalten.

## Schätzung

**Größe: S** — ein Hook (~15 Zeilen), drei Aufrufstellen mit je zwei bis vier geänderten Zeilen, der Umzug
von `ListenAusfall` nach `src/components/` (Import-Pfad in `VaterWizard.tsx`, `VaterWizard.test.tsx`,
`uebung-teilspeichern` nutzt es nur über die Seite). Anker: B-01 („`childId` aus dem Test-Pfad ziehen").

**Risiken:** `HistoryTab` ruft Hooks heute nicht; der Hook muss *vor* den frühen `return`s stehen
(Hook-Regel). Die E2E müssen den Ladezustand *im Fenster* messen — Muster `planbau-ladefenster.spec.ts`
(verzögerte Antwort per `route.fallback()` nach 2 s).

**Angriffsplan:** (1) `src/lib/useGemerkterFehler.ts` + Test, (2) `ListenAusfall` → `src/components/`
mit dem Hook, Tests nachziehen, (3) Trefferbereich, (4) Planbau, (5) Shop-Karte, (6) zwei E2E-Specs.

**Testweg:** `vitest run` (neue Fälle rot vor dem Fix: Knopf nach `rerender(… fehler={null} laedt …)`
nicht mehr im DOM — erwartet rot je Stelle), `playwright test e2e/wiederholknopf-bleibt.spec.ts
e2e/shop-verlauf-wiederholen.spec.ts`, danach die ganze Suite.

## Verlauf

- **2026-09-04** — vom `frontend-reviewer` beim Review von B-188 benannt, am Code gegengelesen. Die
  **dritte** Stelle (`ListenAusfall`) ist im selben Sprint behoben, weil sie im Diff lag; diese zwei liegen
  außerhalb und laufen darum als eigene Story (README, „Ein Fund beim Bauen…"). Kein `entgangen_bei`: Beide
  sind älter als der Sprint.
- **2026-10-17** — **idee → ausformuliert** im Nachtlauf (Sprint 1): Ist-Stand an drei Stellen belegt, die
  dritte (`PlanPositions.tsx:537-543`) ist neu — der Kommentar dort nennt sich selbst „Zwilling" des
  Assistenten. Keine bestehende Zusicherung misst den Zustand während der Wiederholung.
- **2026-10-17** — **ausformuliert → gegrillt**, autonom unter Freigabe 1: vier Entscheidungen, `useAsync`
  bewusst unberührt (B-164 bleibt beim Nutzer).
- **2026-10-17** — **gegrillt → geschaetzt**: S, frontend, keine Migration, kein Vertragsbruch; Testweg
  Vitest + zwei E2E-Specs.
- **2026-10-17** — **geschaetzt → abgenommen** im Nachtlauf, Sprint 1. Rollengang: Vater im Planbau (Suche abgeschossen → „Erneut suchen" → im Fenster „Lädt…", gesperrt, Fokusring auf dem Kasten, Zählzeile „(Suche gescheitert)", kein Leer-Satz → danach „(12 Treffer)"), Sohn im Shop-Verlauf (Meldung + „Nochmal versuchen" → „Lädt…" mit Fokus auf der Meldung → „Noch nichts gekauft"). Assistent nur per E2E `wiederholknopf-bleibt.spec.ts` (derselbe Baustein wie der Planbau-Zwilling). Rote Proben (erwartet/gemessen): VaterWizard.test 1/1, HistoryTab.test 1/1, useGemerkterFehler.test Importfehler (Hook existierte nicht); E2E vor dem Fokus-Fix 3/3 rot an `toBeFocused` – daraus Entscheidung 5. Belege: tsc -b sauber · vitest run 380/380 (47 Dateien) · playwright test 69/70 im vollen Lauf – der eine rote Fall (`lehrwerke.spec.ts`) war eine Nebenwirkung der neuen `fachlehrer-faecher-fallen-aus.spec.ts` in der geteilten DB (aktives Englisch-Profil ohne Grenzen passt zu jedem Kind), behoben (Profil wird stillgelegt angelegt), danach die betroffenen Specs gemeinsam grün · `frontend-reviewer` gelaufen, **kein Blocker**, ein Fund mit Codewirkung (Leer-Satz und Zählzeile des Trefferbereichs hingen noch an `fehler` statt am Gedächtnis – behoben, Vitest ergänzt) · Skill `web-design-guidelines` (Freigabe 7): ein Fund (fokussierter Kasten ohne projektweiten Fokusring – `.banner[tabindex]:focus-visible` ergänzt) · **Browser-Rollengang in Chrome (Freigabe 6)** gegen Backend mit Wegwerf-DB und `npm run dev`, Abrufe über einen Fetch-Wrapper in der Seite abgeschossen bzw. verzögert, siehe Protokoll `docs/pm-sitzung-2026-10-17-nacht.md`. Commit `d834a8e`.
- **2026-10-17** — Nachschau (Nachtlauf, Protokoll `docs/pm-sitzung-2026-10-17-nacht.md`): Hook-Übergänge und Fokus-Reihenfolge halten. **Zwei Funde:** das Gedächtnis überlebt auch einen Filterwechsel ([B-227](B-227-fehlergedaechtnis-ueberlebt-den-filterwechsel.md)); die gefüllte Shop-Karte hat die Fokusregel nicht bekommen ([B-228](B-228-mehr-laden-verliert-den-fokus.md)). AK 3 widersprach Entscheidung 5 – berichtigt.
