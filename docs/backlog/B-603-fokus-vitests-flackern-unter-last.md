---
tags: [typ/story, status/ausformuliert, bereich/test, rolle/entwickler]
aliases: [Fokus-Vitests flackern]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Sprint 82, Verifikation von B-598 (pm-sitzung-2026-11-16-backlog-lauf.md)
grund: ""
ersetzt_durch: []
---

# B-603 · Fokus-Vitests flackern im vollen Lauf

Im Sprint 82 fiel im vollen Vitest-Lauf je einmal ein Fokus-Fall rot, der allein grün ist:
`SohnPractice.test.tsx` („setzt den Fokus nach Weiter auf einer Kennenlernkarte …“, einmal, während parallel die
E2E-Suite lief) und `SohnTest.test.tsx` („steht nach „Nochmal versuchen“ im ersten Bedienbaren der neuen Frage“, in einem
von drei vollen Läufen ohne andere Last). Seither kamen zwei weitere Meldungen dazu (siehe Ist-Stand, Punkt 5).

## User Story

Als Entwickler möchte ich, dass ein Fokus-Test erst dann urteilt, wenn React den Fokus-Effekt tatsächlich ausgeführt
hat, damit ein volles Vitest-Rot einen Fehler im Code meint und keinen Zufall der Zeitplanung.

## Ist-Stand am Code

Die Idee („ein einziger `await act(async () => {})` nimmt einen Takt an“) trifft einen der beiden Fälle nicht und
trifft die Ursache nur halb. Gelesen, nicht gemessen:

1. **Der Fall in `SohnPractice.test.tsx` hat gar kein `act`.** Er klickt „Weiter“, wartet mit `findByText("zwei")`
   und prüft sofort `document.activeElement`
   (`frontend/src/sohn/SohnPractice.test.tsx:669-679`). Der Fokus kommt aus einem Effekt, der erst nach dem Render der
   neuen Karte läuft (`frontend/src/sohn/SohnPractice.tsx:191-214`, Aufruf `.focus()` auf `:203`). Zwischen DOM-Änderung
   und Effekt liegt Zeit.
2. **Der Fall in `SohnTest.test.tsx` hat genau ein `act`.** Er klickt „Nochmal versuchen“, wartet mit
   `findByRole("button", { name: "das Pferd" })` und gibt dann einmal `await act(async () => {})`
   (`frontend/src/sohn/SohnTest.test.tsx:474-486`). Der Fokus kommt aus dem Effekt in `SohnTest.tsx:90-102`, der
   die Absicht erst „verbraucht“, wenn ein Ziel da ist (`:100-101`) und `action.busy` nicht mehr sperrt (`:97`).
3. **Warum die Zeit schwankt:** `findBy…` löst über einen `MutationObserver` aus, sobald der Knopf im DOM steht –
   ausserhalb von `act` (RTL schaltet die `act`-Umgebung für die Dauer von `findBy` ab,
   `node_modules/@testing-library/react/dist/pure.js:84-87`). Ein Zustandswechsel aus einem aufgelösten Versprechen
   rendert dann über den Scheduler, und der Effekt (`useEffect`) läuft **nach** dem Commit, in einer späteren Aufgabe.
   RTL hängt nach dem Fund zwar ein `setTimeout(0)` an (`pure.js:92-99`), und in einem leisen Lauf reicht das fast
   immer; wer zuerst drankommt, der Zeitgeber oder der Scheduler, hängt aber von der Last des Prozesses ab. Das
   erklärt „allein grün, im vollen Lauf selten rot“. **Nicht gemessen** – die Zeitreihenfolge im Lauf unter Last ist
   keine Aussage, die sich am Code beweisen lässt; Akzeptanzkriterium 3 liefert den Beleg.
4. **Der Rest der Datei hat dasselbe Muster:** In `SohnPractice.test.tsx` stehen 17 Zusicherungen auf
   `document.activeElement`, in `SohnTest.test.tsx` zehn; in beiden Dateien kommt `waitFor` kein einziges Mal vor
   (`grep` über `src/sohn/*.test.tsx`: `waitFor` nur ausserhalb von `sohn/`, z. B. `src/lib/useAsync.test.tsx`). Die
   beiden gemeldeten Fälle sind also Stichproben, keine Ausnahmen.
5. **Es betrifft nicht nur Fokus.** Zwei weitere Meldungen im Protokoll: `SohnProgress.test.tsx` fiel im Teillauf
   von B-673 einmal („allein 9/9“, `docs/pm-sitzung-2026-11-18.md:95` und `:128`) und ein volles Vitest-Rot bei
   B-539 „nicht zuzuordnen – Muster B-603“ (`docs/backlog/B-539-ein-aktiver-plan-je-fach.md:304`). Die erste Datei
   prüft keinen Fokus (`SohnProgress.test.tsx` greift nirgends auf `activeElement`, nur `findBy…` und ein
   `act` auf `:167`), die Ursache ist dort also eine andere oder breitere.
6. **Die Teststrecke trägt schon eine Last-Antwort:** `frontend/src/test-setup.ts` erhöht `asyncUtilTimeout` auf 4 s
   (Begründung: einzelne Fälle brauchten unter Last bis 2,4 s). Das hilft `findBy…`/`waitFor`, nicht einer
   Zusicherung, die sofort nach einem Fund prüft – sie wartet gar nicht.
7. **Die Vermutung zum Stimmen-Hook aus B-597** (Idee: „speechSynthesis fehlt in happy-dom, also still“) ist am Code
   nicht gegengeprüft worden; für die beiden Fälle ist sie nicht nötig, weil Punkt 1–3 die Lücke bereits erklären.

## Die echte Lücke

Fokus-Zusicherungen prüfen **einen Zeitpunkt statt eines Zustands**: „jetzt, kurz nach dem Fund“ statt „bald, sobald der
Effekt gelaufen ist“. Die Reparatur ist nicht „ein weiteres `act`“, sondern auf den Fokus selbst zu warten – bei den
Zusicherungen, die einen *Wechsel des Fokus* belegen. Zwei Dinge sind enger oder anders als die Idee dachte:

- **Verneinende Zusicherungen dürfen nicht auf `waitFor` umgestellt werden.** „Beim ersten Laden steht der Fokus auf
  `body`“ (`SohnPractice.test.tsx:683-689`, `SohnTest.test.tsx` „fokussiert beim ersten Aufbau nichts“) beweisen eine
  Abwesenheit; `waitFor` bestünde sie sofort im ersten Versuch und könnte nie mehr rot werden, auch nicht für den
  Fehler, den sie bewachen (ein ungefragt aufspringender Fokus, B-443). Sie brauchen einen Takt zum *Abwarten*,
  nicht zum *Wiederholen*.
- **Die Reihenfolge der Fälle ist schon einmal Ursache gewesen** (`src/test-setup.ts`: „die Reihenfolge der Fälle
  entscheidet über Grün“). Das ist ein anderer Fehler als dieser und steht hier nur, damit er nicht als Erklärung
  eines roten Falls herhalten muss.

## Offene Punkte

1. **Wie weit reicht die Reparatur?** Empfehlung: **nur die positiven Fokus-Zusicherungen** der beiden Dateien
   (17 + 10 Stellen, abzüglich der verneinenden) auf `waitFor(() => expect(document.activeElement).toBe(…))`
   umstellen – nicht stückweise bei jedem Flackern. Die Ausnahme mit Begründung (Punkt „verneinende Zusicherungen“)
   gehört als Kommentar an die Stellen.
2. **Eine Hilfsfunktion oder jede Stelle für sich?** Empfehlung: ein kleiner Helfer in der Testdatei
   (`erwarteFokus(element)`), damit die Form einheitlich ist und eine künftige Zusicherung sie nicht vergisst. Ob ein
   gemeinsamer Helfer unter `src/` (z. B. `test-setup.ts`) Sinn hat, entscheidet sich beim Schätzen: dort liegt er
   neben dem `cleanup` und wäre damit für alle Tests sichtbar, belastet sie aber auch.
3. **Gehört `SohnProgress.test.tsx` hierher?** Empfehlung: **nein, erst einzeln ansehen.** Dort geht es nicht um
   Fokus; die Ursache ist ungeklärt. Als eigene Story ablegen, falls es ein zweites Mal rot wird. Diese Story deckt nur
   die Fokus-Fälle.
4. **Mit Probe oder ohne?** Empfehlung: **mit**, denn „im vollen Lauf grün“ belegt bei einem Flackern nichts (B-153).
   Die Probe: den Effekt für die Dauer des Falls künstlich verzögern (z. B. `await new Promise(r => setTimeout(r, 50))`
   *vor* dem Effekt-Aufruf, nur in der Probe) – dann muss der Fall in der alten Form rot und in der neuen grün sein.

## Entwurf der Akzeptanzkriterien

1. Die positiven Fokus-Zusicherungen in `SohnPractice.test.tsx` und `SohnTest.test.tsx` warten auf den Fokus
   (`waitFor` oder ein Helfer darum) statt nach einem einzelnen `await act(async () => {})` bzw. ohne Warten zu prüfen.
   *Rot, wenn* ein Fokus-Effekt um Zeit verzögert läuft (Probe aus Kriterium 3): die alte Form fällt, die neue besteht.
2. Die verneinenden Zusicherungen („nach dem ersten Laden steht kein Fokus in der Karte“) bleiben ohne `waitFor`
   und tragen einen Kommentar, warum. *Rot, wenn* jemand sie auf `waitFor` umstellt: Das lässt sich nicht prüfen,
   also als Durchsicht-Punkt der Abnahme, nicht als automatisches Kriterium (ausdrücklich so beschriftet).
3. Eine rote Probe je Datei belegt, dass die Reparatur wirkt: den Effekt in `SohnPractice.tsx:203` bzw.
   `SohnTest.tsx:100` in einem `setTimeout(..., 50)` ausführen lassen. Vorher fällt der Fall, nachher bleibt er grün.
   *Rot, wenn* die Reparatur ein weiteres `act` ist: das fängt die Verzögerung nicht ab, die Probe bleibt rot.
4. `SohnProgress.test.tsx` wird nicht angefasst. *Rot, wenn* der Diff die Datei berührt – Durchsicht, kein Tor.

## Verlauf

- **2026-11-16** — angelegt bei der Verifikation von B-598 (Sprint 82).
- **2026-11-19** — ausformuliert (Vorbereitungslauf, Sonnet): Der Fall in `SohnPractice` hat kein `act` (die Idee
  behauptete eines); Ursache am Code gelesen (Effekt nach Commit, `findBy` löst vorher aus); zwei weitere Meldungen im
  Protokoll nachgetragen; nichts gemessen oder gestartet.
