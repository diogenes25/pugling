---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/creator]
aliases: [useExerciseTypes schluckt den Fehler, Typ-Manifest ohne error]
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
---

# B-215 · Das Typ-Manifest schluckt seinen Ausfall — und niemand kann ihn anzeigen

[B-188](B-188-wizard-verschweigt-drei-weitere-ausfaelle.md) hat die drei Optionslisten des Assistenten
wahrheitsfähig gemacht. Die **vierte** kam nicht mit, weil sie strukturell anders gebaut ist.

## User Story

Als **Creator** möchte ich, wenn das Typ-Manifest nicht geladen werden konnte, an der Übungsliste und beim
Anlegen lesen, *warum* Bearbeiten, Löschen und Anlegen gesperrt sind — und es erneut versuchen können —
statt vor grauen Knöpfen ohne Grund zu sitzen.

## Ist-Stand am Code

Nachgesehen am 2026-10-17.

- **Der Hook** (`frontend/src/lib/exerciseTypes.ts:37-56`): `useExerciseTypes()` liefert `null`, solange
  geladen wird, und fängt den Fehlschlag mit `.catch(() => { if (!cancelled) setAll([]); })` (Z. 44) —
  danach ist der Ausfall von „Server kennt null Typen" nicht zu unterscheiden. `loadExerciseTypes()`
  (Z. 21-25) leert bei Fehlschlag seinen Modul-Cache, ein erneuter Aufruf würde also neu laden — nur ruft
  ihn niemand erneut: der Effekt hängt an `[]`.
- **Sieben Konsumenten**, gezählt mit `grep useExerciseTypes src/`:
  `ExerciseEditModal.tsx:46`, `ExerciseFilterBar.tsx:38`, `PlanPositions.tsx:407` und `:649`,
  `VaterExerciseCreate.tsx:51`, `VaterExercises.tsx:58`, `VaterWizard.tsx:135`.
- **Wo der Ausfall eine Handlung sperrt** (handlungsleitend im Sinn von B-188, Entscheidung 1):
  - `VaterExerciseCreate.tsx:55-57`: `route = types?.route(type) ?? null` — „dann bleibt das Anlegen
    gesperrt, statt gegen eine geratene Route zu posten". Der Creator kann **keine** Übung anlegen und
    sieht keinen Grund.
  - `VaterExercises.tsx:263-275`: `ExerciseManageRow` setzt `known = isKnownType(exercise.type) && route
    !== null` — Bearbeiten und Löschen sind je Zeile gesperrt, ohne Grund (die Regel aus B-160, „der
    gesperrte Knopf nennt seinen Grund", wird hier nicht eingelöst).
- **Wo er nur eine Auswahlmöglichkeit kostet:** `ExerciseFilterBar.tsx:94-100` (Typ-Filter bietet nur
  „– alle –"), `VaterWizard.tsx:554` (dasselbe Pulldown im Assistenten), die `typeLabel`-Rückfälle auf den
  Schlüssel in `PlanPositions.tsx:408,650`, `VaterExercises.tsx:59`, `VaterWizard.tsx:573`.
  `ExerciseEditModal` wird nur aus einer Zeile mit `known === true` geöffnet (`VaterExercises.tsx:275`),
  sein `route` kann also im Ausfall gar nicht `null` sein.

## Die echte Lücke

Der Hook kann den Ausfall nicht aussprechen (kein `error`), und er kann ihn nicht wiederholen (kein
`reload`). Beides fehlt an **einer** Stelle; die Konsumenten sind davon abhängig, nicht schuld.

## Offene Punkte

1. ~~Bekommt der Hook ein `error` neben seiner leeren Liste, oder bleibt die Schluck-Entscheidung?~~ →
   Entscheidung 1.
2. ~~Wer zeigt es dann an?~~ → Entscheidung 2 (gezählt, siehe Ist-Stand).

## Entscheidungen

Autonom gegrillt am 2026-10-17 unter Freigabe 1 (`art: Defekt`).

1. **Der Hook liefert immer ein Lookup — mit `loading`, `error` und `reload` — und behält die leere Liste
   im Fehlerfall.** `label()` fällt weiter auf den Schlüssel zurück, `route()` weiter auf `null`; die
   Zusage „lesbar statt kaputt" bleibt für jeden Konsumenten gültig, der nichts tut. *Begründung:* Die
   Konsumenten brauchen drei Dinge, die ein `null` nicht tragen kann: ob geladen wird, ob es scheiterte, und
   einen Weg zurück. *Kosten:* sieben Aufrufstellen verlieren ihr `?.` (der Compiler führt), und das Lookup
   trägt zwei Felder, die nur zwei Konsumenten lesen.
2. **Angezeigt wird der Ausfall genau dort, wo er eine Handlung sperrt:** `VaterExercises` (Liste, über den
   Zeilen) und `VaterExerciseCreate` (über dem Formular) — als `ListenAusfall was="Die Übungstypen"` mit
   `onRetry={types.reload}`. Der Typ-Filter und die Beschriftungen bleiben stumm. *Begründung:* dieselbe
   Gefälle-Regel wie B-188, Entscheidung 1 („`children` ist handlungsleitend, die anderen 38 nicht") — ein
   Banner über einem Filter, der nur eine Facette verliert, wäre eine Regel ohne Gefälle. *Kosten:* im
   Assistenten und Planbau fällt der Typ-Filter weiter still auf „– alle –"; wer das je als Symptom
   meldet, bekommt eine eigene Story.
3. **`ListenAusfall` zieht nach `src/components/`** (bisher in `VaterWizard.tsx`), weil zwei weitere Seiten
   ihn brauchen und ein Import aus einer fremden Seite die falsche Abhängigkeitsrichtung wäre. *Kosten:*
   Import-Pfade in `VaterWizard.tsx` und `VaterWizard.test.tsx`; der Test zieht mit um (`ListenAusfall.test.tsx`).

## Akzeptanzkriterien

1. Scheitert `GET creator/exercise-types`, steht auf `/vater/exercises` und `/vater/exercises/neu` ein
   Banner „Die Übungstypen konnten nicht geladen werden: …" mit „Erneut laden". *Beleg:* E2E
   `typ-manifest-faellt-aus.spec.ts` (beide Seiten, mit `page.route(…).abort()`).
2. „Erneut laden" lädt wirklich neu: gelingt der Abruf danach, verschwindet das Banner, und die Zeilen
   zeigen Anzeigenamen statt Schlüssel; Bearbeiten ist wieder bedienbar. *Beleg:* dieselbe E2E nach
   `unroute`.
3. Während der Wiederholung bleibt das Banner stehen und der Knopf sagt „Lädt…" (B-216). *Beleg:*
   `ListenAusfall.test.tsx` (umgezogen, unverändert in der Sache).
4. Der Hook selbst: im Fehlerfall `all = []`, `error` gesetzt, `loading = false`; nach `reload()` erst
   `loading = true`, dann je nach Antwort gefüllte Liste oder neuer Fehler. *Beleg:* Vitest
   `exerciseTypes.test.ts` mit `vi.mock("./api")` (Hook-Test per `renderHook`, kein nachgebauter Bildschirm).
5. Kein Konsument verhält sich im **Erfolgsfall** anders als vorher. *Beleg:* `tsc -b` ohne Fehler,
   bestehende Vitest- und E2E-Suiten grün (`uebungstypen.spec.ts`, `assistent.spec.ts`).

## Schätzung

**Größe: S** — Hook-Umbau (~20 Zeilen), sieben mechanische Aufrufstellen, zwei Banner, ein Umzug, ein
Hook-Test, eine E2E-Spec. Anker: B-01.

**Risiken:** Der Zustand lebt **je Hook-Instanz**; geteilt ist nur das `pending`-Promise des Moduls.
`reload()` erhöht den Zähler der eigenen Instanz – eine Schwester-Instanz auf derselben Seite bliebe bei
`all = []`/`error` stehen, bis sie neu montiert wird (Reviewer-Fund; die erste Fassung dieses Satzes
behauptete das Gegenteil). **Heute folgenlos:** Auf keiner Seite steht ein Wiederhol-Knopf neben einer
zweiten Instanz (`/vater/exercises`: eine Instanz, `ExerciseEditModal` montiert später und trifft den
gefüllten Cache; `/vater/exercises/neu`: eine Instanz). Latent wird es, sobald jemand `ListenAusfall` neben
`ExerciseFilterBar` stellt – dann zeigten die Zeilen Namen und der Typ-Filter bliebe leer. Die saubere
Lösung wäre ein Modul-Store (`useSyncExternalStore`); hier nicht gebaut, weil der Fall nicht eintritt.
Im Test ist der Cache je Fall frisch (`vi.resetModules()`). `renderHook` braucht `useAuth` nicht — der Hook
ruft `useAsync` nicht.

**Angriffsplan:** (1) Hook, (2) Konsumenten (Compiler-geführt), (3) `ListenAusfall` nach `components/`
(mit B-216s Hook), (4) zwei Banner, (5) Vitest, (6) E2E.

**Testweg:** `vitest run src/lib/exerciseTypes.test.ts src/components/ListenAusfall.test.tsx`, dann
`playwright test e2e/typ-manifest-faellt-aus.spec.ts`, dann die ganze Suite.

## Verlauf

- **2026-09-04** — vom `frontend-reviewer` beim Review von B-188 benannt und von mir am Code gegengelesen.
  Bewusst **nicht** in B-188 mitgebaut: Der Hook ist geteilt, ein `error` dort trifft jeden Konsumenten und
  wäre damit ein Umbau außerhalb des Sprint-Diffs. `unverifiziert: true`, weil der sichtbare Schaden
  hergeleitet und nicht vorgeführt ist.
- **2026-10-17** — **idee → ausformuliert** im Nachtlauf (Sprint 1): sieben Konsumenten gezählt, zwei
  davon sperren eine Handlung ohne Grund (Anlegen, Bearbeiten/Löschen); `ExerciseEditModal` ist im Ausfall
  unerreichbar.
- **2026-10-17** — **ausformuliert → gegrillt**, autonom unter Freigabe 1: drei Entscheidungen.
- **2026-10-17** — **gegrillt → geschaetzt**: S, frontend, keine Migration, kein Vertragsbruch.
- **2026-10-17** — **geschaetzt → abgenommen** im Nachtlauf, Sprint 1. Rollengang: Creator auf `/vater/exercises` (Manifest abgeschossen → Banner „Die Übungstypen konnten nicht geladen werden", Zeilen „· Vocabulary" → „Erneut laden" → „Lädt…" mit Fokusring → Banner weg, „· Vokabelkarten"/„· Lückentext"). Rote Probe: exerciseTypes.test 3/3 (`.loading` undefined, `error` null). E2E `typ-manifest-faellt-aus.spec.ts` 2/2 grün (zweiter Fall brauchte den Schalter „geteilte Übungen", weil die geseedeten Englisch-Übungen dem Lehrer-Konto gehören). Ist-Stand-Korrektur aus dem Review: `reload()` wirkt je Hook-Instanz, nicht geteilt – Risiko-Absatz berichtigt. Belege: tsc -b sauber · vitest run 380/380 (47 Dateien) · playwright test 69/70 im vollen Lauf – der eine rote Fall (`lehrwerke.spec.ts`) war eine Nebenwirkung der neuen `fachlehrer-faecher-fallen-aus.spec.ts` in der geteilten DB (aktives Englisch-Profil ohne Grenzen passt zu jedem Kind), behoben (Profil wird stillgelegt angelegt), danach die betroffenen Specs gemeinsam grün · `frontend-reviewer` gelaufen, **kein Blocker**, ein Fund mit Codewirkung (Leer-Satz und Zählzeile des Trefferbereichs hingen noch an `fehler` statt am Gedächtnis – behoben, Vitest ergänzt) · Skill `web-design-guidelines` (Freigabe 7): ein Fund (fokussierter Kasten ohne projektweiten Fokusring – `.banner[tabindex]:focus-visible` ergänzt) · **Browser-Rollengang in Chrome (Freigabe 6)** gegen Backend mit Wegwerf-DB und `npm run dev`, Abrufe über einen Fetch-Wrapper in der Seite abgeschossen bzw. verzögert, siehe Protokoll `docs/pm-sitzung-2026-10-17-nacht.md`. Commit: siehe Nachtrag.
