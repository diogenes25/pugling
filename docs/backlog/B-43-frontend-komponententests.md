---
tags: [typ/story, status/gegrillt, bereich/qualitaet, bereich/frontend, bereich/tests]
aliases: [Frontend-Komponententests, useAction-Sperre]
status: gegrillt
prio: P3
art: Defekt
quelle: docs/testplan.md#nachmessung-2026-07-31-die-drei-unbeobachteten-flächen
---

# B-43 · Die Doppelklick-Lücke in `useAction` – und die fehlende Ebene für unsichtbare Zusicherungen

Das Frontend hat **21 Vitest-Fälle über 83 Quelldateien**, und sie prüfen zwei reine Logikdateien. Zwischen
„Logik unter `src/lib`" und dem Playwright-Durchstich liegt nichts – auch nicht für die Primitive, durch die
*jede* Mutation läuft. Beim Grillen kam dabei ein **Defekt** heraus, siehe „Die echte Lücke".

## User Story

Als **Vater**, der auf „Speichern" klickt und dabei zweimal erwischt, möchte ich, dass **nur eine** Mutation
abgeschickt wird – und als **Entwickler**, der an einem geteilten Primitiv etwas ändert, möchte ich einen
Test, der dessen **unsichtbare Zusicherungen** festhält, damit ein Umbau sie nicht still bricht.

## Ist-Stand am Code

- **Die DOM-Umgebung ist eingerichtet, nicht nur installiert:** `vitest.config.ts:13` setzt
  `environment: "happy-dom"`. Was fehlt, ist `@testing-library/react` – und ein Test, der überhaupt rendert.
  (Die Idee behauptete „installiert und ungenutzt"; die Konfiguration widerspricht dem, die Nutzung nicht.)
- **Die vorhandenen 21 Fälle sind gut, aber keine Komponenten:** `lib/remarks.test.ts` (9 Fälle) fährt den
  **echten** API-Client gegen einen abweisenden `fetch` und durchsucht den Fehler-Ringpuffer nach der PIN.
  `vater/navigation.test.ts` (12 Fälle) pinnt die Pfad→Perspektive-Zuordnung samt der Präfix-Falle `/vater`.
- **Reichweite:** `useAction` wird in **26** Dateien benutzt (24 Bildschirme plus die beiden
  Definitionsdateien); pro Panel eine Instanz (`VaterShop.tsx` z. B. drei Instanzen für zehn Aktionen).
  **Kein** Aufrufer ruft `run` nebenläufig oder in einer Schleife (nachgesehen).
- **`useAction.run`/`runFor` sperren ihren eigenen Wiedereintritt nicht:** sie setzen `busy` per `useState`,
  und die Doku sagt ausdrücklich „damit Knöpfe sperren". Nachgesehen: **alle 24 Aufrufer haben heute
  `disabled={…busy}`** – die Regel wird also befolgt, aber `disabled` greift erst **nach** dem Re-Render.
- **`StatusBanner.tsx` trägt eine Invariante, die man beim Umbauen verliert:** die Live-Region
  (`role="status"`, `aria-live="polite"`) steht **immer** im DOM, auch ohne Meldung – „viele Screenreader
  sagen nur an, was in eine *bereits vorhandene* Region hineinwächst". Ein `return null` im Leerfall sieht
  richtig aus, ist sichtbar identisch und macht die Ansage stumm.
- **Die Sohn-App benutzt die Primitive gar nicht:** unter `src/sohn/` gibt es **keinen** `useAction`-Treffer;
  `SohnShop.tsx:52` trägt ein eigenes `if (busy) return`. Dort ist der Kaufpfad aber dreifach abgesichert –
  eigenes `busy`, ein **synchrones** `confirmAction` zwischen Klick und POST, und serverseitig der
  `ConcurrencyStamp`-Bump (von `ShopFlowTests` gepinnt). Die Abweichung von der Primitive-Norm ist als
  [B-49](B-49-sohn-app-schreib-primitive.md) eigens erfasst.
- **Weitere ungetestete Primitive:** `components/ListControls.tsx` (`PAGE_SIZE`, `Pager`, `TruncationHint`,
  `SortableTh`, `SortControl`), `vater/MediaPickers.tsx` (`AssetThumb`, `MediaSearch`), `lib/ui.ts`
  (`confirmAction`, `prefersReducedMotion`).
- **Was der E2E schon trägt** (25 Tests): Feldhilfe-Zuordnung, Perspektivwechsel, Freigabe, Lehrer-Konto,
  Bilder-Durchstich, „jeder Übungstyp des Manifests lässt sich anlegen", der Vater→Sohn-Loop. Also die
  **Wege**, nicht die Bausteine.

## Die echte Lücke

Zwei Dinge, und das Grillen hat die Reihenfolge umgedreht.

**Erstens ein Defekt:** `disabled={busy}` schützt erst nach dem Re-Render. Zwei Klicks im selben Tick laufen
beide durch `run` und schicken **zwei** Mutationen ab. Das ist exakt die im Projekt belegte Fehlerklasse – beim
doppelten Sohn-Test-Versuch schützte das `alive`-Flag den Zustand, nicht den abgeschickten POST. Die Schwere
ist gemessen niedrig (der Geldpfad des Sohnes läuft nicht über `useAction` und ist dreifach abgesichert), die
Fläche aber groß: 24 Masken.

**Zweitens:** Es gibt keine Ebene für Zusicherungen, die man am Bildschirm **nicht sehen** kann. Die
Live-Region von `StatusBanner`, die `busy`-Sperre, `aria-sort` – ein Bruch verändert das Bild nicht, also
findet ihn kein Klick-Test und ein Screenshot erst recht nicht. Ohne diese Ebene lässt sich der Defekt oben
auch nicht **rot zeigen**, und ein Defekt ohne vorher roten Test ist im Projekt nicht abgenommen.

Damit ist der Zuschnitt anders als in der Idee: **nicht** „Komponententests fürs Frontend einführen" (das
endet bei nachgebauten Bildschirmen mit gefälschtem `fetch`, teurer als der vorhandene E2E), sondern **ein
Defekt plus fünf bis acht Tests auf die geteilten Primitive**.

## Offene Punkte

Alle im Grillen vom 2026-07-31 entschieden.

1. ~~`@testing-library/react` aufnehmen?~~ → Entscheidung 2 (ja, als Teil des Angriffsplans)
2. ~~Wo ist die Grenze zu Playwright?~~ → Entscheidung 3
3. ~~Die `busy`-Regel: Test oder Wächter?~~ → Entscheidung 1 (weder – die Sperre kommt ins Primitiv)
4. ~~Läuft das im CI-Job mit?~~ → Entscheidung 4

## Entscheidungen

1. **`run`/`runFor` bekommen eine Wiedereintritts-Sperre per `useRef`** – nicht über den `busy`-State.
   Begründung: State greift erst nach dem Re-Render; zwei Klicks im selben Tick kommen heute beide durch. Ein
   `useRef` sperrt synchron. **Kosten:** Produktivcode in einem Baustein, an dem 24 Bildschirme hängen – und
   eine zweite Aktion im selben Panel wird während der ersten **still verworfen** statt eingereiht. Eine
   Warteschlange wurde erwogen und verworfen: sie brächte Fragen mit (Reihenfolge, Abbruch beim Unmount,
   welche Meldung gewinnt), die niemand gestellt hat.
2. **Die Sperre bleibt in dieser Story, und `art` wird `Defekt`.** Begründung: Die Projektregel lautet
   „Defekt = ein Regressionstest, der vorher rot war" – und diesen Test rot zu sehen braucht genau die
   Testinfrastruktur, die diese Story einführt. Eine getrennte Defekt-Story würde auf diese warten: zwei Akten
   für einen Vorgang. Angriffsplan daher: `@testing-library/react` aufnehmen → Test mit zwei Klicks im selben
   Tick, **rot** → `useRef`-Sperre → grün → die übrigen Primitive-Tests. **Kosten:** B-43 rückt in der
   Sortierung nach vorn (Defekt vor Aufräumen), obwohl es die teuerste der vier Test-Stories ist.
3. **Harte Grenze: nur `components/` und `lib/`.** Kein nachgebauter Bildschirm, kein gefälschter `fetch` über
   eine ganze Maske – Bildschirme und Abläufe bleiben bei Playwright. **Eine** benannte Ausnahme: der
   Defekt-Test aus Entscheidung 2 rendert stellvertretend **einen** Aufrufer, weil die Lücke nur an einem
   echten Knopf sichtbar ist. Die Regel kommt als Satz in `frontend/CLAUDE.md`. **Kosten:** eine Ausnahme, die
   jemand später als Einladung lesen könnte – deshalb steht sie als Ausnahme dort, nicht als Beispiel.
4. **Kein neuer CI-Job.** `ci.yml` fährt `npm test` bereits im Frontend-Job. **Kosten:** keine; die Grenze
   dafür steht in Akzeptanzkriterium 6.
5. **Reihenfolge:** B-43 wird als **letzte** der vier Test-Stories gebaut, trotz `art: Defekt`. Begründung:
   die Schwere des Defekts ist belegt niedrig (siehe „Die echte Lücke"), und es ist die einzige Story mit
   neuer devDependency **und** Verhaltensänderung in 24 Masken. **Kosten:** der Defekt liegt am längsten
   offen – bewusst.

## Akzeptanzkriterien

1. Ein Test fährt zwei Klicks im selben Tick gegen einen stellvertretend gerenderten Aufrufer und weist nach,
   dass **genau eine** Mutation abgeschickt wird. Er war **vor** der Sperre rot (zwei Aufrufe) – belegt, nicht
   behauptet.
2. `useAction` hat Tests für die vier Zusicherungen seiner Dokumentation: `busy` während des Laufs,
   Rückgabewert `true`/`false` bzw. Ergebnis/`null`, Fehlermeldung aus `errorMessage(err)`, und `okText`
   weggelassen ⇒ Banner bleibt leer.
3. `StatusBanner` hat einen Test, der die Live-Region **im Leerfall** nachweist (`role="status"` vorhanden,
   kein Meldungstext) und im Erfolgs-/Fehlerfall Text und Einfärbung. **Gegenprobe:** `return null` im
   Leerfall macht ihn rot.
4. `ListControls` ist abgedeckt: `Pager`-Grenzen (erste/letzte Seite) und `SortableTh`-Umschaltung inklusive
   `aria-sort`.
5. Alle 24 Aufrufer verhalten sich unverändert; die 25 Playwright-Tests bleiben grün (die Sperre darf keinen
   bestehenden Ablauf brechen).
6. `npm test` läuft im vorhandenen CI-Job mit, **unter fünf Sekunden** zusätzlich, und `npm ci
   --legacy-peer-deps` bleibt fehlerfrei.
7. Kein Test baut einen ganzen Bildschirm mit gefälschtem `fetch` nach; die Grenze aus Entscheidung 3 steht
   als Satz in `frontend/CLAUDE.md`.

## Verlauf

- **2026-07-31** — angelegt (Quelle: Nachmessung der Test-Abdeckung, [testplan.md](../testplan.md)).
- **2026-07-31** — ausformuliert: die Idee war in zwei Punkten falsch – die DOM-Umgebung **ist** eingerichtet
  (`vitest.config.ts:13`), und alle 24 `useAction`-Aufrufer sperren ihren Knopf korrekt.
- **2026-07-31** — gegrillt: fünf Entscheidungen, und die Story hat sich gedreht. Aus einer Aufräum-Story
  wurde ein **Defekt**: `disabled={busy}` greift erst nach dem Re-Render, zwei Klicks im selben Tick schicken
  zwei Mutationen. Schwere belegt niedrig (Geldpfad des Sohnes läuft nicht über das Primitiv und ist dreifach
  abgesichert), Fläche 24 Masken.
- **2026-08-01** — ins [Testabdeckungs-Paket](../testabdeckung-plan.md) als **E5** aufgenommen (hinter E4,
  der gemeinsamen Werkzeugkette – und **vor** dem Typ-Umbau E6, nicht als Letzte: erst das Netz, dann der
  Umbau, der durch dieselben Bausteine geht). Vier Änderungen aus der Dev-Runde:
  1. **Der Ist-Stand war falsch:** „alle 24 Aufrufer haben `disabled={busy}`" stimmt nicht – **fünf Knöpfe
     haben keins** (`VaterRewards.tsx:131,133,214,216`, `VaterShop.tsx:443` „Stornieren", ein Geldpfad auf
     der **Vater**-Seite). Bei `toggle` ist der Doppelschuss zudem ein Flip-Flop: zweimal `active: !m.active`
     endet im Ausgangszustand, das Banner meldet Erfolg. Die fünf bekommen `disabled` dazu; die Sperre ist
     **additiv** – `disabled={busy}` bleibt überall, weil Playwrights Actionability daran ihren
     Serialisierungspunkt hat.
  2. **Die Bibliothekssuche verlässt das Primitiv vorher** (`MediaPickers.tsx:77-82` → `useAsync`): ein
     Lesevorgang gehört nicht in den „Zustand einer **schreibenden** Aktion" (`useAction.ts:10`), und eine
     still verworfene zweite Suche wäre für den Nutzer schlicht „es passiert nichts".
  3. **Kein Schlüssel-Parameter am Primitiv.** Die Sperre wirkt je Hook-Instanz – in drei Bildschirmen ist
     das listenweit (`PlanPositions.tsx:38→:58`, `VaterShop.tsx:47→:105`, `VaterZiele.tsx:164→:178`). Das ist
     bewusst so und kommt als Satz in die Doku von `useAction`.
  4. **Entscheidung 3 verliert ihre Ausnahme:** kein stellvertretend gerenderter Bildschirm. Der Defekt sitzt
     in `useAction`, zwei synchrone `run()` auf derselben Hook-Instanz (`renderHook`) zeigen ihn genauso rot.
     Die Regel in `frontend/CLAUDE.md` wird damit sauber – nur `components/` und `lib/`, ohne Sternchen.
  Abgespalten: [B-53](B-53-wizard-doppelklick.md) (`VaterWizard`, zwei Kinder und zwei Pläne) – andere
  Bauform, eigene Story, im selben Durchgang gebaut.
