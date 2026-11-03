---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [Feste Feldhilfe schließt bei Tastatur-Resize, Popover zeigt nach Layout-Verschiebung aufs falsche Feld]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: frontend-reviewer zu B-263 in Lauf A (docs/pm-sitzung-2026-10-26-tag.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-11-03"
entgangen_bei: []
wartet_auf: ""
---

# B-273 · Das feste ⓘ-Popover: Resize ohne Prüfung, Layout-Verschiebung ohne Schließen

Seit [B-263](B-263-info-popover-steht-am-rechten-rand-ueber.md) steht das ⓘ-Popover fest zum Fenster
(`position: fixed`). Der `frontend-reviewer` hat in Lauf A drei Restpunkte genannt, die dort nicht behoben wurden:

1. **`resize` schließt ohne Prüfung** (`frontend/src/components/InfoHint.tsx`, anders als `scroll`). Browser, die
   für die Bildschirmtastatur noch das Layout-Viewport verkleinern (Firefox Android, ältere Chromium-Versionen),
   lösen beim Wegklappen der Tastatur `resize` aus. Ein Tipp aufs ⓘ neben einem gerade bearbeiteten Feld könnte das
   Popover darum sofort wieder schließen. Chrome Android ab 108 und iOS Safari tun das nicht. **Unverifiziert.**
   Vorschlag: wie bei `scroll` nur schließen, wenn sich der Knopf bewegt oder `clientWidth` geändert hat.
2. **Layout-Verschiebung ohne Scrollen**: Verschiebt sich der Inhalt über dem Knopf (eine Liste lädt nach, ein Banner
   erscheint), bleibt das Popover stehen und zeigt auf das falsche Feld. Selten, weil Öffnen ein bewusster Klick ist.
   Vorschlag: `ResizeObserver` oder eine rAF-Prüfung des Ankers, solange es offen ist.
3. **Testlücken, benannt statt geschlossen**: Die Regel „nie über den linken Rand hinaus“ (`Math.max(EDGE, …)`)
   greift bei keiner realen Breite und ist darum nicht prüfbar. Ein Scrollen *nur waagrecht* (Vergleich nur über
   `top`) bleibt ungeprüft. Der Wächter „oben nur, wenn oben Platz ist“ wirkt seit dem senkrechten Klemmen nur noch
   auf die Klasse `.up`.

**In Lauf A bewusst nicht behoben:** Punkt 1 ist ungemessen, und die Zielbrowser lösen den Fall nicht aus. Jede
Behebung hätte als Fund gezählt (Freigabe 3), der Zähler stand bei 4.

## User Story

Als **Vater** möchte ich, dass ein aufgeklapptes ⓘ immer am Feld steht, zu dem es gehört – es schließt, sobald der Knopf
wandert, und bleibt offen, wenn sich nur das Fenster ändert –, damit ich keine Erklärung neben dem falschen Feld lese.

## Ist-Stand am Code (Lauf P, Stand `bffb583`)

- `frontend/src/components/InfoHint.tsx:31-53`: `scroll` (capture) schließt nur, wenn der Knopf sich bewegt hat
  (Vergleich `left` und `top`); `resize` ruft `close` ohne Prüfung (`:46`).
- Nichts reagiert auf Inhalt, der über dem Knopf aufgeht, ohne dass gescrollt wird.
- `e2e/feldhilfe-lage.spec.ts` prüft senkrechtes Scrollen (`:166-183`) und eine Breitenänderung, kein waagrechtes Scrollen.
- **Gemessen** (rote Proben, neue Fälle gegen den Ausgang): Layout-Verschiebung **1 failed**, Größenänderung nur der Höhe
  **1 failed**, waagrechtes Scrollen **1 passed** (Testlücke).

## Die echte Lücke

Punkt 1 und 2 wie beschrieben; Punkt 1 ist in Chromium nicht über die Tastatur, aber über eine reine Höhenänderung
reproduzierbar. Von Punkt 3 bleibt das waagrechte Scrollen; „nie über den linken Rand" greift bei keiner realen Breite.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Eine Anker-Prüfung je Frame (`requestAnimationFrame`) ersetzt den `scroll`-Listener.** Sie fängt Scrollen in jedem
   Rahmen, auch nur waagrecht, und Inhalt, der darüber aufgeht. *Kosten:* ein `getBoundingClientRect` je Frame, solange
   ein Popover offen ist (höchstens eins); ein Schließen kommt bis zu einen Frame später als mit `scroll`.
2. **`resize` schließt nur, wenn der Knopf sich bewegt hat; sonst wird neu ausgerichtet** (Zähler `measureTick` am
   Mess-Effekt). Eine zusätzliche Breiten-Bedingung entfällt: gemessen grün als Mutante, und das Neumessen richtet die
   Breite ohnehin neu aus. *Kosten:* keine bekannten.
3. **„Nie über den linken Rand" bleibt ungeprüft**, begründet: das Popover ist höchstens 320 px breit, unter 336 px Fenster
   gibt es kein Vater-Web-Layout, das getestet wird; eine Zusicherung dort wäre ein künstliches Fenster. Der Wächter für
   `.up` bleibt, wie er ist.

## Akzeptanzkriterien

1. Inhalt, der über dem Knopf aufgeht, schließt ein offenes ⓘ.
2. Waagrechtes Scrollen allein schließt es.
3. Eine Größenänderung, die den Knopf nicht bewegt, lässt es offen und richtet es so aus, dass es im Fenster steht.
4. Die bestehenden Fälle (Scrollen von vor dem Öffnen, Scrollen und Breitenänderung schließen) bleiben grün.

## Schätzung

`S`, frontend. Testweg: drei neue Fälle in `feldhilfe-lage.spec.ts`, Mutanten je AK und Entscheidungsteil.

## Verlauf

- **2026-10-26** — angelegt aus dem `frontend-reviewer` zu B-263 in Lauf A
  ([Protokoll](../pm-sitzung-2026-10-26-tag.md)), Funde 2–4 und die Restzeilen der Mutanten-Tabelle.
- **2026-11-03** — Lauf P, Teil 2: ausformuliert, autonom gegrillt (Freigabe 1), geschätzt, gebaut. Rote Proben: AK 1 **1
  failed**, AK 3 **1 failed** (erwartet je 1); AK 2 **1 passed** (Testlücke). Nach dem Bau `feldhilfe-lage.spec.ts` **9
  passed**. Mutanten: „nur `top` verglichen" **1 failed** (AK 2, `:204`); „ohne Anker-Wächter je Frame" rot an AK 1 (`:220`),
  AK 2 und den Scroll-Fällen; „resize schließt immer" **1 failed** (AK 3, `:240`); „ohne Neuausrichtung" **1 failed** (AK
  3, `:242`, erst nachdem der Fall bei 1000 px Höhe startet – bei 700 px klappte das Popover nach oben, und die Vorbedingung
  „stünde unten über" hielt nicht); „Breite nicht geprüft" grün → Bedingung gestrichen (Entscheidung 2). Vitest **620/620**,
  `tsc -b` sauber.
- **2026-11-03** — Review `frontend-reviewer`: 🟡 die Bewegungsprüfung in `onResize` sei vermutlich ungetragen (rAF schließe vorher) → **gemessen statt vermutet:** Mutante „`onResize` ohne `moved()`" **1 failed** am Breitenfall – getragen, kein Code geändert (ohne Zählung). 🟡 der Breitenfall prüfte das Layout statt der Regel → Vorbedingung „der Knopf wandert" (zählt). 🟡 Banner- und Waagrecht-Fall ohne Vorbedingung (Scroll-Anchoring könnte das `prepend` ausgleichen) → Vorbedingungen „der Knopf rutscht" bzw. „nur waagrecht gewandert" (zählt). 🟢 Dateikopf und Wartezeit-Kommentar nach dem entfernten `scroll`-Listener nachgezogen, `imFenster` per `expect.poll` (ohne Zählung). Danach `feldhilfe-lage.spec.ts` **9 passed**.
- **2026-11-03** — Abnahme Lauf P, Teil 2: Vitest **620/620**, `tsc -b` sauber, E2E-Gesamtlauf **107 passed** (`exit=0`, nach dem letzten Code-Edit gestartet). Rollengang: Vater maschinell – `feldhilfe-lage.spec.ts` (neun Fälle) öffnet das ⓘ im echten Browser und bewegt Knopf, Rahmen und Fenster; Creator und Sohn Regressionszeugen. Gerätecheck offen: die Bildschirmtastatur eines echten Handys (Firefox Android) einmal gegen ein offenes ⓘ. **Abgenommen**, Commit siehe Protokoll ([Lauf P](../pm-sitzung-2026-11-03-nacht.md)).
- **2026-11-03** — Nachschau in Lauf Q, Teil 1 (frischer `frontend-reviewer`, `git show a25a1a6 -- frontend`, read-only): **Entgleitung** → [B-362](B-362-infohint-mit-crlf-und-einzelnem-cr-committet.md) (🟡: die Datei ging mit CRLF und einem einzelnen CR in den Index, `i/-text`). Fachlich sauber; geprüft: jede AK mit Vorbedingung gegen den falschen Grund (bewegt / nur waagrecht / steht still), Start bei 1000 px trägt „stünde unten über", Effekt-Reihenfolge (Anker vor dem ersten Frame), Aufräumen bricht den Frame ab, Scrollen im Popover bewegt den Knopf nicht, ein per `display:none` versteckter Knopf zählt als bewegt.
