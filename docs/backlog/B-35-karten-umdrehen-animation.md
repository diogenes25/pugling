---
tags: [typ/story, status/geschaetzt, bereich/frontend, rolle/student]
aliases: [Karten umdrehen, Flip-Animation, Aufdecken-Animation]
status: geschaetzt
prio: P3
art: Wunsch
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nutzer, Sitzung 2026-07-30
grund: ""
ersetzt_durch: []
---

# B-35 · Karten drehen sich beim Aufdecken um

Beim Aufdecken einer Karte soll sie sich **umdrehen**, sodass man die Rückseite sieht — wie eine echte
Karteikarte, statt dass die Lösung einfach erscheint. Reine Politur der Sohn-Arcade, aber genau die Art
Politur, die das „TADAA" trägt.

## User Story

Als *Sohn (Student)* möchte ich, dass sich die Karte beim Aufdecken der Lösung **umdreht**, damit das
Umblättern sich wie eine echte Karteikarte anfühlt und nicht wie ein plötzlicher Textwechsel.

## Ist-Stand am Code

**Zwei getrennte Orte** decken heute eine Lösung auf, kein gemeinsamer Reveal-Mechanismus:

- **Übungsschleife (Leitner)**: `frontend/src/sohn/SohnPractice.tsx:15` führt
  `type Phase = "loading" | "front" | "back" | "done" | "empty" | "error"`. Der Knopf „Umdrehen 🔄"
  (`SohnPractice.tsx:284`) ruft `onClick={() => setPhase("back")}`; die Lösung erscheint als zusätzliche
  Zeile unterhalb der Aufgabe (`SohnPractice.tsx:252`):
  `{phase === "back" && card.reveal && <div className="rev">→ {card.reveal}</div>}`. Ob eine Karte
  überhaupt eine Rückseite hat, entscheidet `SohnPractice.tsx:166`:
  `const typed = card.reveal === null;` — nur wenn `reveal` nicht `null` ist, zeigt die Karte den
  „Umdrehen"-Knopf statt eines Eingabefelds.
- **Abschlusstest/Klausur**: `frontend/src/sohn/SohnTest.tsx:36` hält einen eigenen
  `const [revealed, setRevealed] = useState(false)`. `SohnTest.tsx:126`:
  `const showSolution = revealed || item.reveal !== null;`. Der Knopf „Aufdecken 🔄" (`SohnTest.tsx:198`)
  ruft `onClick={() => setRevealed(true)}`; die Lösung erscheint in `SohnTest.tsx:196`:
  `<div className="rev" ...>→ {item.reveal ?? "(aufgedeckt)"}</div>`.

Beide Stellen sind unabhängige `useState`-Flags mit eigenem JSX-Zweig — keine geteilte Komponente/Hook.

**Visuell ist das Aufdecken heute ein hartes Umschalten ohne Übergang.** Die „Karte" selbst
(`.fcard`, `frontend/src/index.css:124`) bleibt bestehen; darunter erscheint nur ein zusätzlicher Absatz
(`.rev`, `frontend/src/index.css:127`: `color: var(--cyan); font-size: 20px; font-weight: 800;` — keine
`transition`/`animation`-Deklaration). In `SohnTest.tsx` sitzt die Lösungszeile sogar in einem generischen
`.card` (`SohnTest.tsx:148`), nicht in `.fcard` — die beiden Orte teilen sich nicht einmal denselben
Karten-Container.

**Wo hat „Aufdecken" fachlich überhaupt eine Vorder-/Rückseite-Bedeutung?** Das Feld `reveal` wird zentral
in `backend/Pugling.Api/Services/Shared/PositionPlayService.cs:135` dokumentiert: „typed stages withhold
the solution (`Reveal`), display/self-assessment reveals it". Ob eine Stufe „typed" ist, entscheidet
`IExerciseType.IsTypedStage`; Default in `backend/Pugling.Api/Exercises/ExerciseTypeBase.cs:38`:
`public virtual bool IsTypedStage(int stage) => true;`. Von den elf registrierten Übungstypen
(`backend/Pugling.Api/Exercises/IExerciseType.cs:117-142`) überschreiben nur zwei:

- **Vocabulary** (`VocabularyExerciseType.cs:42`): `IsTypedStage` nutzt
  `StageMechanics.IsTyped(TestStage)`, und `StageMechanics.cs:17-18` listet nur `LetterBoxes, FreeText,
  Audio, MultipleChoice` als typed. Die beiden **nicht** gelisteten Stufen —
  `TestStage.ShowBoth = 1` und `TestStage.SelfAssess = 2`
  (`backend/Pugling.Api/Models/StudyPlanEntities.cs:33,35`) — sind die einzigen, wo `reveal` gesetzt wird.
- **Cloze** (`BuiltInExerciseTypes.cs:148` mit `StageMechanics.cs:26-27`): alle drei `ClozeStage`-Werte
  gelten als typed → `reveal` ist bei Cloze **immer** `null`.

Die restlichen neun Typen (Reading, Essay, Listening, Grammar, Matching, Translation, Arithmetic,
ArithmeticDrill, List) sowie Cloze liefern nie ein `reveal` — dort ist „Aufdecken" nur getippte Antwort →
richtig/falsch-Feedback, keine Karten-Metapher.

**`prefersReducedMotion` und bestehende Animationsmuster:** `frontend/src/lib/ui.ts:22-25` definiert die
JS-Prüfung (`window.matchMedia("(prefers-reduced-motion: reduce)").matches`), genutzt z. B. in
`Celebration.tsx:46` (verhindert den Aufbau der Konfetti-Daten) und `lib/feedback.ts:104` (gatet
Vibration). Zusätzlich gibt es ein **globales CSS-Gate** in `frontend/src/index.css:337-340`:
`@media (prefers-reduced-motion: reduce) { * { animation: none !important; transition: none !important; }
.cel-layer { display: none; } }` — das schaltet jede neue `transition`/`animation` automatisch ab, ohne
dass eine Komponente selbst etwas tun muss. Bestehende Animationen sind durchweg reines CSS: Toast-Popup
(`index.css:190-191`, `@keyframes pop`), Konfetti/Feier (`index.css:303-334`,
`frontend/src/components/Celebration.tsx`), Tempo-Leiste (`index.css:349-350`,
`SohnPractice.tsx:213`, `key={idx}` erzwingt Remount = Animation-Neustart je Karte). Keine
Animationsbibliothek ist installiert: `frontend/package.json:18-22` führt unter `dependencies` nur
`react`, `react-dom`, `react-router-dom` — kein `framer-motion`/`react-spring`.

**Playwright-E2E, die den Reveal-Moment durchlaufen** (keine wartet auf eine CSS-Klasse, alle klicken
direkt nacheinander):

- `frontend/e2e/full-flow.spec.ts:85-86`: „Umdrehen 🔄" klicken, direkt danach „Gewusst!" klicken.
- `frontend/e2e/full-flow.spec.ts:104-106`: „Aufdecken 🔄" bedingt klicken (bei `SelfAssess` ist laut
  Kommentar Zeile 97 die Lösung „automatisch aufgedeckt", der Knopf erscheint dann nicht), danach „Gewusst".
- `frontend/e2e/bilder.spec.ts:169-170`: dieselbe Umdrehen→Gewusst-Sequenz in einer Schleife.

## Die echte Lücke

Nicht „eine Animation fehlt überall", sondern: **zwei unabhängige State-Maschinen** (`SohnPractice.tsx`
`phase`, `SohnTest.tsx` `revealed`) schalten je einen JSX-Zweig ohne Übergang um, und nur an genau zwei
Vokabel-Stufen (`ShowBoth`, `SelfAssess`) trägt `reveal` überhaupt Information, die eine
Vorderseite/Rückseite-Metapher rechtfertigt. Die Lücke ist damit rein **visuell und lokal** (zwei
Komponenten, ein CSS-Muster, keine Server-Änderung) — nicht strukturell.

## Offene Punkte

1. ~~Wo findet das Aufdecken statt — ein Ort oder mehrere?~~ → siehe Entscheidung 1.
2. ~~Wie verhält sich die Animation zu `prefersReducedMotion`?~~ → siehe Entscheidung 2.
3. ~~Trägt die Rückseite fachlich immer dieselbe Information — gibt es Typen, wo es nichts umzudrehen
   gibt?~~ → siehe Entscheidung 3.
4. ~~Soll die Animation beide Loci (Übungsschleife **und** Klausur) betreffen, oder zunächst nur die
   Übungsschleife?~~ → siehe Entscheidung 4.
5. ~~Wie wird das Playwright-Timing-Risiko behandelt (Klick auf „Umdrehen" gefolgt vom sofortigen Klick
   auf „Gewusst!"/„Aufdecken")?~~ → siehe Entscheidung 5.
6. ~~Neue Animationsbibliothek oder reines CSS?~~ → siehe Entscheidung 6.

## Entscheidungen

1. **Beide Orte bekommen die Flip-Animation, mit derselben CSS-Technik.** `SohnPractice.tsx` und
   `SohnTest.tsx` haben zwar getrennte State-Maschinen, aber denselben Bedarf (Rückseite-Zeile ohne
   Übergang). Eine gemeinsame Komponente/Hook zu extrahieren wäre ein größerer Umbau, den die Story nicht
   rechtfertigt — die Animation hängt sich stattdessen an beide bestehenden Bedingungen
   (`phase === "back"` bzw. `showSolution`) an, ohne die State-Maschinen selbst anzufassen. Kosten: zwei
   Stellen statt einer werden geändert, aber beide sind kleine, gleichartige JSX/CSS-Ergänzungen.
2. **Die Animation ist reines CSS (`transform`/`transition`, kein `setTimeout`-Gate) und verlässt sich auf
   das bestehende globale Reduce-Motion-Gate** (`index.css:337-340`). Begründung: Der Flip verzögert
   keinen Zustandswechsel — der Judge-Button-Block wird weiterhin synchron zum State gerendert, nur die
   Optik der Lösungszeile ändert sich. Damit greift `* { transition: none !important }` automatisch und
   ohne zusätzlichen JS-Check. Kosten: keine — es ist dieselbe Absicherung, die Toast/Konfetti/Tempo-Leiste
   schon nutzen, kein neuer Mechanismus.
3. **Die Flip-Animation gilt nur, wo `reveal !== null` ist — also nur an den Vokabel-Stufen `ShowBoth` und
   `SelfAssess`.** Das ist bereits die bestehende Bedingung in beiden Komponenten
   (`card.reveal === null` → getippte Stufe, sonst Umdrehen-Knopf); es braucht keinen neuen
   Unterscheidungs-Code. Alle anderen Übungstypen und Cloze bleiben unverändert (kein Flip, weil dort kein
   Rückseite-Zweig existiert). Kosten: keine zusätzliche Verzweigung nötig, die Animation hängt sich an den
   vorhandenen Zweig.
4. Siehe Entscheidung 1 — beide Orte.
5. **Die Animation bleibt kurz (Größenordnung des bestehenden `pop`-Keyframes, ~200–300ms) und verzögert
   kein Element-Mounting.** Der „Gewusst!"/„Nicht gewusst"-Button-Block wird weiterhin sofort mit dem
   State-Wechsel gerendert, nicht erst nach Animationsende — nur die Lösungszeile bekommt eine
   `transform`-Transition. Damit bleibt `full-flow.spec.ts:85-86,104-106` und `bilder.spec.ts:169-170`
   ohne Änderung grün, weil Playwright weiterhin sofort ein aktionsfähiges Element vorfindet. Kosten: die
   Animation wirkt rein dekorativ und „fertig", bevor der Nutzer sie bewusst wahrnimmt — akzeptiert, weil
   Spielbarkeit vor Optik geht (kein Feature darf die Klick-Kette bremsen).
6. **Reines CSS, keine neue Abhängigkeit.** `package.json` führt keine Animationsbibliothek, und alle
   bestehenden Effekte (Toast, Konfetti, Tempo-Leiste) sind CSS-`@keyframes`/`transition`. Ein echter
   Karten-Flip braucht `perspective`, `transform-style: preserve-3d` und `backface-visibility: hidden` auf
   einem neuen Wrapper (`.flip-card > .flip-inner > .flip-front/.flip-back`) — Standard-CSS, keine Library
   nötig. Kosten: die Vorderseite (Wort/Bild/Audio) muss in der Flip-Inner-Struktur bleiben, während die
   Rückseite nur die `reveal`-Zeile zusätzlich zeigt — eine kleine JSX-Umstrukturierung in beiden
   Komponenten, kein neues Datenmodell.

## Akzeptanzkriterien

1. Klickt man in der Übungsschleife auf „Umdrehen 🔄" (`SohnPractice.tsx:284`), dreht sich die Karte
   sichtbar um (CSS-`transform: rotateY`), bevor `card.reveal` erscheint — statt dass die Zeile ohne
   Übergang eingeblendet wird.
2. Klickt man in der Klausur auf „Aufdecken 🔄" (`SohnTest.tsx:198`), verhält sich die Karte analog.
3. Bei `prefers-reduced-motion: reduce` läuft kein Flip: Die Rückseite erscheint sofort, weil das
   bestehende globale CSS-Gate (`index.css:337-340`) jede `transition`/`animation` abschaltet — keine
   zusätzliche JS-Sonderbehandlung nötig.
4. Übungstypen und Stufen ohne `reveal` (alle getippten Vokabel-Stufen, alle Cloze-Stufen, alle neun
   übrigen Übungstypen) bleiben unverändert — kein Flip, da dort kein Rückseite-Zweig existiert.
5. Die bestehenden Playwright-Suiten (`frontend/e2e/full-flow.spec.ts`, `frontend/e2e/bilder.spec.ts`)
   laufen weiterhin grün, ohne dass ein Klick auf ein noch nicht aktionsfähiges Element trifft.
6. `frontend/package.json` bleibt ohne neue Abhängigkeit — die Animation ist reines CSS.

## Schätzung

**Größe: S** — reine CSS-/JSX-Politur an zwei bekannten Stellen (`SohnPractice.tsx`, `SohnTest.tsx` plus
`index.css`), kein Backend, kein neues Datenfeld, keine Migration, kein Vertragsbruch. Etwas mehr als eine
XS-Politur (B-02-Anker), weil zwei Komponenten eine kleine Struktur-Umstrukturierung (Flip-Wrapper mit
Vorder-/Rückseite) statt nur einer CSS-Zeile brauchen — bleibt aber deutlich unter dem M-Anker
(vokabel-basierter Batch-Pfad, B-03).

**Risiken:**

- Playwright-Timing (siehe Entscheidung 5): Ein zu langsamer oder verzögert gemounteter Judge-Button-Block
  würde `full-flow.spec.ts` und `bilder.spec.ts` rot machen. Gegenmaßnahme ist Teil der Entscheidung, nicht
  nur ein Risiko-Vermerk.
- 3D-`transform` auf schwächeren mobilen Geräten (PWA-Zielgruppe) kann ruckeln — geringes Risiko bei einer
  einzelnen, kurzen Transition; kein bekannter Präzedenzfall im Repo, der das widerlegt oder bestätigt.
- Die beiden Loci teilen sich heute keinen Container (`.fcard` vs. generisches `.card`,
  `SohnTest.tsx:148`) — die Flip-CSS muss an beiden Stellen konsistent angewendet werden, sonst wirkt die
  Klausur uneinheitlich zur Übungsschleife.

**Angriffsplan** (nur Frontend, Backend unverändert — `reveal` existiert bereits):

1. CSS-Grundgerüst in `frontend/src/index.css` ergänzen: `.flip-card`/`.flip-inner`/`.flip-front`/
   `.flip-back` mit `perspective`, `transform-style: preserve-3d`, `backface-visibility: hidden`, plus eine
   Zustandsklasse (z. B. `.is-flipped { transform: rotateY(180deg); }`) — das bestehende Reduce-Motion-Gate
   greift automatisch.
2. `SohnPractice.tsx`: den `.fcard`-Block in den Flip-Wrapper packen, Vorderseite unverändert, Rückseite
   mit `card.reveal`; Zustandsklasse an `phase === "back"` hängen.
3. `SohnTest.tsx`: dieselbe Umstrukturierung, Zustandsklasse an `showSolution`/`revealed` hängen.
4. Visuell im laufenden Dev-Server prüfen (`npm run dev`) — reine Optik, kein `/smoke-test`-Fall.
5. `npm run test:e2e` laufen lassen; bleibt `full-flow.spec.ts`/`bilder.spec.ts` rot, eine
   `await expect(...).toBeVisible()`-Zeile vor dem betroffenen Klick ergänzen (kein neuer Testfall nötig,
   da kein neues Verhalten geprüft wird außer „Judge-Buttons bleiben erreichbar").

**Testweg:** Kein neuer Integrationstest (rein visuelle CSS-Änderung, kein Backend-Verhalten). Bestehende
Playwright-Suiten `frontend/e2e/full-flow.spec.ts` und `frontend/e2e/bilder.spec.ts` müssen grün bleiben
(`npm run test:e2e`); visuelle Abnahme über den laufenden Dev-Server.

## Verlauf

- **2026-07-30** — vom Nutzer direkt aufgenommen (ungeprüft, keine Recherche — das ist der nächste Schritt).
- **2026-08-03** — ausformuliert: Ist-Stand gegen den Code belegt (`SohnPractice.tsx`, `SohnTest.tsx`,
  `StageMechanics.cs`, `PositionPlayService.cs`, `index.css`, `lib/ui.ts`); zwei getrennte Reveal-Orte ohne
  Übergang, `reveal` nur bei `TestStage.ShowBoth`/`SelfAssess` gesetzt, keine Animationsbibliothek
  installiert, globales Reduce-Motion-CSS-Gate vorhanden.
- **2026-08-03** — gegrillt: alle sechs offenen Punkte als nummerierte Entscheidungen beantwortet (autonom
  getroffen, Nutzerauftrag 2026-08-04).
- **2026-08-03** — geschätzt: Größe S, `wo: frontend`, keine Migration, kein Vertragsbruch, Angriffsplan
  und Testweg festgelegt (autonom getroffen, Nutzerauftrag 2026-08-04).
