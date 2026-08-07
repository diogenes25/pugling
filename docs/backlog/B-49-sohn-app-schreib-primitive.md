---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/qualitaet]
aliases: [Sohn-App ohne useAction]
status: abgenommen
prio: P3
art: Aufräumen
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: B-43
nachgeschaut: "2026-08-07"
---

# B-49 · Die Sohn-App benutzt die geteilten Schreib-Primitive nicht

`frontend/CLAUDE.md` führt „`useAction` + `StatusBanner` für jede Mutation" als Norm – unter `src/sohn/` gibt
es aber **keinen einzigen** `useAction`-Treffer (nachgesehen beim Grillen von
[B-43](B-43-frontend-komponententests.md)). `SohnShop.tsx` trägt stattdessen eigenes `busy`/`msg` samt
`flash()`-Timeout und `if (busy) return` (`SohnShop.tsx:52`) – also genau die try/catch/finally-Kaskade, deren
Vervielfachung `useAction` beseitigen sollte. Folge: Die `useRef`-Sperre aus B-43 wirkt in der Sohn-App
**nicht**, und die Rückmeldung läuft dort an `StatusBanner` vorbei – samt dessen Live-Region, also ohne die
Screenreader-Ansage.

## User Story

Als Entwickler möchte ich, dass jede schreibende Aktion der Sohn-Arcade über `useAction` läuft, damit die
Doppelklick-Sperre (Ref-Gate) überall greift und Fehlermeldungen konsistent und barrierefrei über
`StatusBanner` bzw. dessen Live-Region ankommen – statt in jeder Datei erneut denselben
try/catch/finally-Rumpf und eine eigene state-basierte Sperre nachzubauen.

## Ist-Stand am Code

`frontend/CLAUDE.md:44-49` benennt das Muster als Norm und trägt selbst schon den Vermerk: „Als **Regel**,
nicht als Zustand: die Sohn-Arcade (B-49) folgt ihr noch nicht." Ein Grep über `frontend/src/sohn/*.tsx`
nach `useAction` liefert null Treffer.

Von den **11 Dateien** unter `src/sohn/` schreiben **fünf** tatsächlich gegen die API; die übrigen sechs
(`SohnHome.tsx`, `SohnKonto.tsx`, `MyObjectives.tsx`, `GamificationPanels.tsx`, `SohnProgress.tsx`,
`SohnApp.tsx`) sind rein lesend – `SohnApp.tsx`s `toggleMute` ist reiner Client-State, kein Server-Aufruf.

Die fünf schreibenden Stellen, alle am Primitiv vorbei:

- **`SohnShop.tsx:28-29,52-66,68-89`** – eigenes `busy`/`msg` + `flash()`-Timeout-Helfer. `buy()` und
  `requestActivation()` sperren Wiedereintritt über `if (busy) return` – React-**State**, nicht `useRef`.
  Genau die Race, die `useAction.ts:58-65` als Grund für das Ref-Gate nennt („`busy` steht erst nach dem
  Re-Render am Knopf... zwei Klicks im selben Tick kamen darum beide durch"), ist hier also weiterhin offen.
- **`SohnSkins.tsx:15-16,32-52`** – identisches Muster (`busy`/`msg`/`flash()`), `choose()` mit derselben
  state-basierten Sperre vor `api.purchaseSkin`/`api.equipSkin`.
- **`SohnTest.tsx:39,96-109`** – `answerAndAdvance()` prüft `if (... || busy) return` am Anfang, ebenfalls
  State statt Ref.
- **`SohnPractice.tsx:89-122`** – `judge()` hat **überhaupt keine Wiedereintritts-Sperre**: zwei schnelle
  Taps auf „Gewusst!"/„Nochmal" bzw. eine Multiple-Choice-Antwort vor Abschluss des ersten `await
  api.review(...)` können zwei Bewertungen für dieselbe Karte auslösen. `reshuffleImage()`
  (`SohnPractice.tsx:174-187`) ist nur über die lokale `busy`-State der `CardImage`-Unterkomponente
  (`SohnPractice.tsx:309-330`) gesperrt – wieder State statt Ref, und eine zweite, unabhängige
  Sperr-Variable neben der von `judge`.

Keine der fünf Dateien rendert `StatusBanner`: `SohnLogin.tsx` zeigt einen eigenen `.error-box` mit
`role="alert"`, `SohnShop.tsx`/`SohnSkins.tsx` einen `flash()`-Toast mit `role="status"
aria-live="polite"` (die Live-Region ist hier zufällig vorhanden, aber nicht dieselbe Komponente),
`SohnTest.tsx` einen `.error-box` mit Wiederholungsknopf.

**`SohnLogin.tsx:15,20-36`** trägt dasselbe Eigenbau-Muster (`busy`/`error`, eigener try/catch/finally) –
aber **`VaterLogin.tsx:64-65,97-98,120-121,143,181-182`** im Vater-Web hat exakt dieselbe Konstruktion.
Login-Bildschirme sind also in **beiden** Apps eine bestehende Ausnahme vom `useAction`-Muster, kein
Sohn-spezifisches Defizit (siehe Entscheidung 4).

## Die echte Lücke

Nicht bloß ein Stil-Unterschied: Drei der fünf Schreibstellen (`SohnShop`, `SohnSkins`, `SohnTest`)
implementieren eine Wiedereintritts-Sperre, die dieselbe Race-Bedingung trägt, die `useAction`s Ref-Gate
gerade schließen soll (State statt Ref). Eine (`SohnPractice.judge`) hat **gar keine** Sperre – das ist die
schärfste Stelle, denn sie bewertet echte Lern-Fortschritt-Buchungen (Leitner-Box, Combo, Punkte). Die
Idee vermutete „ob das Absicht ist" bei der Arcade-Optik (`Celebration` statt Banner) – das bleibt
berechtigt und wird in Entscheidung 2 aufgelöst; die tiefere Lücke ist aber die fehlende bzw. unsichere
Sperre, nicht nur das fehlende Banner.

## Offene Punkte

- ~~Was übernehmen wir – die Sperre und der Fehlerweg, oder auch die Optik (`StatusBanner` 1:1 statt
  `Celebration`/`flash()`)?~~ → siehe Entscheidung 2.
- ~~Welche der 11 Dateien schreiben überhaupt – reicht `SohnShop.tsx` allein?~~ → siehe Ist-Stand: fünf
  Dateien schreiben, alle fünf mit derselben Lücke → siehe Entscheidung 1.
- ~~Zieht `SohnLogin.tsx` mit?~~ → siehe Entscheidung 4.
- ~~Bleibt `CardImage`s lokale `busy`-State, oder teilt sie sich die Instanz mit `judge`?~~ → siehe
  Entscheidung 3.

## Entscheidungen

1. **Umfang: alle vier tatsächlich unsicher gesperrten Schreibstellen umbauen** – `SohnShop.tsx` (`buy`,
   `requestActivation`), `SohnSkins.tsx` (`choose`), `SohnTest.tsx` (`answerAndAdvance`), `SohnPractice.tsx`
   (`judge`, `reshuffleImage`) – nicht nur die in der Idee benannte `SohnShop.tsx`. Begründung: dieselbe
   Lücke (State- statt Ref-Sperre, oder gar keine) liegt in allen vieren; `SohnPractice.judge` ist sogar die
   ungeschützteste Stelle im ganzen Bereich. Kosten: vier statt eine Datei – jede Änderung ist aber
   mechanisch (State-Deklarationen raus, `useAction()` rein, bestehender Rumpf in `run`/`runFor` verpackt).

2. **Kern übernehmen, Optik teilweise arcade-eigen lassen**: `useAction`s Ref-Sperre und Fehlerpfad
   (`StatusBanner` für `message.ok === false`) werden übernommen; das Erfolgssignal bleibt dort arcade-eigen
   (`celebrate()`-Overlay bei Kauf/richtiger Antwort), wo es das schon gibt – `run`/`runFor` laufen dann ohne
   `okText`. Begründung: `frontend/CLAUDE.md:48-49` dokumentiert dieses Muster bereits als Präzedenzfall
   (Tag-Editor im Vokabel-Store: „Erfolg darf stumm bleiben... ein Banner je Chip wäre Lärm") – eine
   zusätzliche Erfolgs-Bannerzeile neben einer schon laufenden Celebration wäre doppelte, sich
   widersprechende Rückmeldung. Kosten: der stumme Erfolgsfall wird zur Regel für die meisten der fünf
   Aufrufe; `requestActivation` (keine Celebration vorgesehen) bekommt einen `okText`, der über
   `StatusBanner` statt über den bisherigen `flash()`-Toast läuft – eine sichtbare, aber gewollte
   Verhaltensangleichung ans Vater-Web.

3. **`CardImage` verliert ihre lokale `busy`-State**: `reshuffleImage` läuft über dieselbe `useAction`-Instanz
   wie `judge` in `SohnPractice`; `busy` reist als Prop nach unten. Begründung: zwei unabhängige
   „läuft-gerade"-Variablen in derselben Bildschirm-Komponente sind exakt die Vervielfachung, die
   `useAction` beseitigen soll; eine geteilte Sperre verhindert zudem harmlos, dass während eines
   Bildwechsels eine Antwort bewertet wird. Kosten: `CardImage` wird eine reine Props-Komponente ohne
   eigenen State.

4. **`SohnLogin.tsx` bleibt unangetastet.** Begründung: `VaterLogin.tsx` (Zeilen 64-65, 97-98, 120-121, 143,
   181-182) trägt exakt dasselbe `busy`/`error`-Eigenbau-Muster ohne `useAction` – das ist also eine
   bestehende, für **beide** Login-Bildschirme gleichermaßen geltende Ausnahme, kein Sohn-spezifisches
   Defizit. Ihr gemeinsamer Umbau (Login generell) wäre eine eigene Story, nicht Teil von B-49. Kosten:
   keine – die Story bleibt beim tatsächlichen Delta zum Vater-Web.

5. **`StatusBanner` ohne Arcade-eigenes Styling einsetzen.** Begründung: `SohnShop.tsx` rendert die Klasse
   `banner err` für den Lade-Fehler bereits selbst (Zeile ~112) – das CSS existiert, passt sichtbar zum
   Dark-Arcade-Theme, und ein zweiter Regelsatz wäre unnötig. Kosten: keine zusätzliche CSS-Arbeit.

## Akzeptanzkriterien

1. `SohnShop.tsx`, `SohnSkins.tsx`, `SohnTest.tsx` und `SohnPractice.tsx` importieren `useAction`; die
   bisherigen eigenen `busy`/`msg`/`error`-State-Deklarationen für Mutationen (nicht für reines Laden) sind
   entfernt.
2. Alle sechs betroffenen Aufrufe (`buy`, `requestActivation`, `choose`, `answerAndAdvance`, `judge`,
   `reshuffleImage`) laufen über `run`/`runFor`; Fehler erscheinen über `StatusBanner`, Erfolg bleibt dort
   stumm, wo `celebrate()` schon feiert (Entscheidung 2).
3. Jeder auslösende Knopf trägt `disabled={busy}` – inklusive des „anderes Bild"-Knopfs in `CardImage`, der
   sein `busy` jetzt als Prop von `SohnPractice` bekommt statt lokal zu halten.
4. `frontend/e2e/full-flow.spec.ts` zählt bei einem `dblclick()` auf den Kauf-Knopf im Shop und bei einem
   `dblclick()` auf „Gewusst!" in der Übungsrunde je genau einen abgeschickten POST (Muster
   `vater-von-null.spec.ts:334-359`).
5. `SohnLogin.tsx` bleibt unverändert.
6. `npm run build`, `npm test` und `npm run test:e2e` sind grün.

## Schätzung

**Größe: S** – vier mechanische Umbauten (State raus, `useAction` rein, bestehender Rumpf in `run`/`runFor`
verpackt) plus eine Prop-Verschiebung (`CardImage`) und eine E2E-Erweiterung; kein neuer Endpunkt, kein
neues DTO. Vergleichbar mit dem abgenommenen [B-54](B-54-objectivecard-schreib-primitive.md) (fünf Knöpfe im
Vater-Web, ebenfalls S).

**Risiken:**

- `judge()` ruft nach dem `try/catch` bewusst **immer** `next()` auf, auch bei einem fehlgeschlagenen
  `api.review` („Bewertung ist idempotent genug; UI läuft weiter", `SohnPractice.tsx:120`). Der Umbau auf
  `useAction.run` muss dieses Verhalten erhalten – `next()` steht außerhalb des `run`-Aufrufs, nicht in
  dessen Erfolgspfad.
- Eine geteilte `useAction`-Instanz für `judge` und `reshuffleImage` (Entscheidung 3) sperrt beide
  gegeneinander – laut Konvention (`frontend/CLAUDE.md:45-47`, `useAction.ts:58-65`) beabsichtigt, aber im
  Test explizit gegenzuprüfen, dass „anderes Bild" während einer laufenden Bewertung tatsächlich
  `disabled` ist und umgekehrt.
- `StatusBanner`s Live-Region liegt an anderer Stelle im Markup als die bisherigen `flash()`-Toasts –
  Playwright-Selektoren, die auf `.toast` zielen (z. B. `full-flow.spec.ts`), müssen ggf. auf `.banner`
  nachgeführt werden.

**Angriffsplan** (Reihenfolge nach Risiko, niedrig zuerst):

1. `SohnShop.tsx` – `buy()` + `requestActivation()` auf `useAction` umstellen (dichtestes Vater-Web-Vorbild).
2. `SohnSkins.tsx` – `choose()`, gleiche Form.
3. `SohnTest.tsx` – `answerAndAdvance()`, mechanischer Tausch der bestehenden State-Sperre.
4. `SohnPractice.tsx` – `judge()` + `reshuffleImage()`, inklusive Prop-Umbau von `CardImage` (höchstes
   Risiko, siehe oben).
5. `frontend/e2e/full-flow.spec.ts` um die beiden `dblclick()`-Zusicherungen (Shop-Kauf, „Gewusst!")
   erweitern.

**Testweg:** Das Primitiv selbst ist bereits durch `frontend/src/lib/useAction.test.tsx` abgesichert (Ref-
Gate, `busy`, Fehlerpfad) – dafür braucht es keinen neuen Test. Neu: zwei `dblclick()`-Zusicherungen in
`frontend/e2e/full-flow.spec.ts`, die je die Anzahl abgeschickter POSTs zählen (Muster
`vater-von-null.spec.ts:334-359`, dort für B-54 etabliert) – einmal am Shop-Kauf-Knopf, einmal am
„Gewusst!"-Knopf der Übungsrunde. Ein Bildschirm-Komponententest für `SohnPractice`/`SohnTest` scheidet aus
(`frontend/CLAUDE.md:41-42`: „kein nachgebauter Bildschirm mit gefälschtem `fetch`" – das sind Wege durch die
App, also Playwright-Sache). Zusätzlich `npm run build` und `npm test` (Typecheck + bestehende Suiten) grün
halten.

## Verlauf

- **2026-07-31** — geerntet beim Grillen der vier Test-Stories (Nebenbefund aus der B-43-Recherche).
- **2026-08-03** — ausformuliert: Ist-Stand gegen den Code belegt (fünf von elf Sohn-Dateien schreiben, drei
  mit state- statt ref-basierter Sperre, eine ganz ungeschützt); vier offene Punkte formuliert.
- **2026-08-03** — gegrillt: alle vier offenen Punkte in Entscheidungen überführt (Umfang, Kern-vs-Optik,
  `CardImage`-Prop-Umbau, `SohnLogin` bleibt außen vor) – autonom getroffen, Nutzerauftrag 2026-08-04.
- **2026-08-03** — geschätzt: Größe S, `wo: frontend`, keine Migration, kein Vertragsbruch, Angriffsplan und
  Testweg (E2E-Doppelklick nach dem B-54-Muster) festgelegt – autonom getroffen, Nutzerauftrag 2026-08-04.
- **2026-08-06** — gebaut (Nachtlauf 2, Sprint 4): alle vier Schreibstellen auf `useAction` umgestellt —
  `SohnShop.tsx` (`buy` ohne `okText`, die Feier bleibt die Rückmeldung; `requestActivation` mit `okText`
  über `StatusBanner`, ersetzt den bisherigen `flash()`-Toast; das Nachladen der Kaufhistorie bleibt
  bewusst außen vor, keine Mutation), `SohnSkins.tsx` (`choose`, eigener `loadError`-State fürs erste Laden
  bleibt von `action` getrennt), `SohnTest.tsx` (`answerAndAdvance`, Fehlerpfad speist weiter die
  bestehende Vollbild-`error`-Anzeige, damit sich deren Verhalten nicht ändert), `SohnPractice.tsx`
  (`judge` und `reshuffleImage` teilen **eine** `useAction`-Instanz, Entscheidung 3; `CardImage` verlor
  ihre lokale `busy`-State, bekommt sie jetzt als Prop). **Erhaltenes Verhalten, gegengeprüft:** `judge()`s
  `next()` läuft weiter unbedingt nach dem inneren `try/catch`, auch bei einem fehlgeschlagenen
  `api.review` (das Schlucken sitzt weiter innerhalb von `action.run`, `next()` außerhalb davon — kein
  Verhalten geändert). **Nebenbefund beim Bauen:** `SohnPractice.tsx`s „Nochmal"/„Gewusst!"- und
  „Prüfen"-Knöpfe trugen **gar kein** `disabled={busy}` (schärfer als die im Ist-Stand benannte
  State-Sperre) — nachgezogen, drei zusätzliche Knöpfe. `frontend/e2e/full-flow.spec.ts` zählt jetzt beim
  ersten `dblclick()` auf „Gewusst!" in der Übungsrunde genau einen `POST .../practice-sessions/.../review`
  (der Rest der Spec bleibt vom vorbestehenden B-109-Hänger bei Frage 3 betroffen, unverändert — die neue
  Zusicherung läuft davor und wurde einzeln bestätigt). Der Shop-Kauf-Doppelklick (AK4) landete in
  `frontend/e2e/shop-verlauf.spec.ts` statt in `full-flow.spec.ts`: dessen Shop-Abschnitt liegt hinter der
  Klausur-Sequenz und lief seit B-109 nie mit (dieselbe Begründung, die B-110 zur eigenen Datei gemacht
  hat) — `shop-verlauf.spec.ts` ist reproduzierbar grün und damit der ehrlichere Ort für den Nachweis.
  `npm run build` clean, `npm test -- --run` → **153/153 grün**, beide neuen E2E-Zusicherungen einzeln UND
  im vollen `npm run test:e2e`-Lauf bestätigt (**27/28 grün**). `frontend-reviewer` lief gegen den Diff.
- **2026-08-07** — Nachschau (Nachtlauf): geprüft, ob `SohnShop.tsx`/`SohnSkins.tsx`/`SohnTest.tsx`/
  `SohnPractice.tsx` weiterhin `useAction` nutzen und `judge`/`reshuffleImage` weiterhin eine geteilte
  Instanz teilen — hält (alle vier Dateien importieren `useAction`, `SohnPractice.tsx:72` dokumentiert
  die geteilte Instanz). Kein Fund.
