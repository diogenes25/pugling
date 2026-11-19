---
tags: [typ/story, status/geschaetzt, bereich/test, rolle/entwickler]
aliases: [Fokus-Vitests flackern]
status: geschaetzt
prio: P3
art: Defekt
groesse: M
wo: frontend
migration: nein
vertragsbruch: nein
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
   `document.activeElement`, in `SohnTest.test.tsx` zehn (davon drei verneinend, 24 positiv); in beiden Dateien kommt `waitFor` kein einziges Mal vor
   (in diesen zwei Dateien kommt `waitFor` nicht vor; im Ordner `src/sohn/` benutzt es nur `MyObjectives.test.tsx` – die Aussage
   der Ausformulierung „nur ausserhalb von `sohn/`“ stimmte nicht). Die
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

1. ~~**Wie weit reicht die Reparatur?**~~ → Entscheidung 1.
2. ~~**Eine Hilfsfunktion oder jede Stelle für sich?**~~ → Entscheidung 2.
3. ~~**Gehört `SohnProgress.test.tsx` hierher?**~~ → Entscheidung 4 (zurückgestellt).
4. ~~**Mit Probe oder ohne?**~~ → Entscheidung 5.
5. ~~**Was geschieht mit den verneinenden Zusicherungen?**~~ → Entscheidung 3 (neu beim Grillen: gezählt sind es drei).

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-19 (Sonnet), unbestätigt.*

1. **Alle positiven Fokus-Zusicherungen beider Dateien werden umgestellt, nicht nur die zwei gemeldeten: 16 in
   `SohnPractice.test.tsx` und 8 in `SohnTest.test.tsx`, zusammen 24.** Gezählt an `activeElement`: 17 + 10 Stellen, davon
   drei verneinend (`SohnPractice.test.tsx:688`, `SohnTest.test.tsx:495` und `:508`, siehe Entscheidung 3). Begründung: Der Fokus
   kommt in allen Fällen aus einem Effekt nach dem Commit (`SohnPractice.tsx:179-226`, `SohnTest.tsx:90-102`), und `waitFor` kommt
   in beiden Dateien kein einziges Mal vor – das Muster „sofort nach dem Fund prüfen“ steckt in jedem der 24 Fälle, die zwei
   gemeldeten sind Stichproben. Stückweises Reparieren hieße, dass 22 Fälle ihr eigenes Flackern erst melden müssen.
   Verworfen: nur die zwei gemeldeten Fälle und ein weiteres `act` (das Problem ist, dass `findBy…` außerhalb von `act`
   auslöst, ein zweites `act` wartet auf nichts Bestimmtes). Kosten: ein Diff über 24 Stellen in zwei Testdateien; die
   Zeilen liegen im Arbeitsbaum, in dem `SohnPractice.tsx` und `SohnTest.tsx` derzeit ungecommittete fremde Änderungen
   tragen – die Zeilennummern der Komponenten (`:179`, `:203`, `:99` …) sind darum vor dem Bau neu zu lesen. Ausgeschlossener
   Fall: Fokus-Tests in anderen Dateien (`KartenMeldung.test.tsx`, `HistoryTab.test.tsx`, `Lektion.test.tsx`,
   `SohnHome.test.tsx` und weitere tragen `activeElement`); gegen die User Story („ein volles Vitest-Rot meint einen Fehler“)
   bleibt dort dasselbe Muster, sobald es dort flackert, ist es eine eigene Story.
2. **Der Helfer heißt `erwarteFokus`, steht lokal in jeder der beiden Dateien und nimmt eine Funktion, die das Ziel
   liefert: `erwarteFokus(() => screen.getByRole("textbox"))`.** Begründung: Die Ziele werden an der Stelle der Zusicherung
   gesucht (`screen.getByRole(…)`), nicht vorher; innerhalb von `waitFor` muss die Suche mitwiederholt werden, sonst
   verglichen die Versuche mit einem Element, das der Effekt längst ersetzt hat. Die Tests haben ihre Hilfen sonst lokal
   (`renderPractice`, `SohnPractice.test.tsx:348`; `renderTest`, `SohnTest.test.tsx:53`), ein gemeinsames Testhelfer-Modul
   wurde nicht gefunden (Suche nach Importen aus Hilfsmodulen in `src/**/*.test.*` ohne Treffer). Gegenfundstelle: `src/test-setup.ts` ist
   gemeinsam, enthält aber Aufräumen und Konfiguration, keine Helfer. Verworfen: der Helfer in `test-setup.ts` (jede Testdatei
   lädt ihn, und er wäre der erste Helfer dort) und ein neues Modul (ein Modul für zwei Dateien). Kosten: der Dreizeiler
   steht zweimal. Ausgeschlossener Fall: eine dritte Datei mit Fokus-Zusicherungen, die den Helfer neu schreibt; gegen die
   User Story ist das ein Schönheitsfehler, kein Flackern.
3. **Die drei verneinenden Zusicherungen („beim ersten Aufbau steht der Fokus auf `body`“) bleiben ohne `waitFor` und tragen
   einen Kommentar, warum.** Begründung: Sie belegen eine Abwesenheit (B-443, ein ungefragt aufspringender Fokus);
   `waitFor` bestünde sie im ersten Versuch und könnte nie rot werden. Sie behalten den einen Takt (`await act(async () => {})`),
   der dem Effekt Zeit zum Feuern gibt. Das Risiko ist benannt: unter Last kann ein spät feuernder Fehlfokus nach dem Takt
   noch nicht da sein – die Zusicherung wäre dann **grün zu Unrecht**, nie flackernd rot. Verworfen: ein fester Warte-
   abstand (Zeitraten, nicht beweisbar). Kosten: Diese drei Zusicherungen bleiben schwächer als die positiven. Ausgeschlossener
   Fall: ein Fehlfokus, der erst nach dem einen Takt aufspringt; gegen die User Story („Rot meint einen Fehler“) trägt das,
   weil ein falsches Grün hier kein Flackern ist.
4. **Zurückgestellt: `SohnProgress.test.tsx` und die nicht zugeordnete Meldung bei B-539.** Die Datei prüft keinen Fokus
   (`activeElement` kommt nicht vor), die Ursache ist ungeklärt; die beiden Protokollstellen nennen **keinen Fall**
   (`pm-sitzung-2026-11-18.md:95`: „Ausreißer `SohnProgress.test.tsx` im Teillauf“; `B-539-…md:304`: „nicht zuzuordnen – Muster
   B-603“). Eine Reparatur ohne Fallnamen wäre geraten. Zurückgestellt bis ein Rot mit Fallnamen und Fehlermeldung vorliegt;
   dann eine eigene Story. Ausgeschlossener Fall: dieses Flackern; gegen die User Story (Fokus-Tests) trägt das, weil es
   kein Fokus-Fall ist.
5. **Die Probe verzögert den Fokus-Effekt, und zwar je Fokus-Aufruf: sieben in `SohnPractice.tsx` (`:179`, `:203`, `:207`, `:210`,
   `:213`, `:226`, `:370`), einer in `SohnTest.tsx` (`:99`).** Begründung: Eine rote Probe je Datei fragt nur, ob irgendein Fall
   fällt; sie belegt nicht, dass jede umgestellte Zusicherung etwas trägt (gemessen in B-154, B-161, B-157). Je Aufruf zwei
   Proben: (a) Aufruf in `setTimeout(…, 50)` – die alte Form muss mindestens einen Fall rot zeigen, die neue alle grün;
   (b) Aufruf entfernt – mindestens ein Fall wird rot (sonst trägt keine Zusicherung diesen Aufruf, und das steht im
   Verlauf). Weil die Komponenten im Arbeitsbaum fremde, ungecommittete Änderungen tragen, wird **nicht** per `git checkout`
   zurückgesetzt, sondern der Stand vor der Probe als Kopie gesichert und danach zurückgespielt (Blob-Hash vor und nach
   gleich); das Zurückspielen braucht keinen Build, Vitest liest die Quelle. Verworfen: eine Probe je Datei (zeigt nur,
   dass etwas hängt) und ein Verzicht auf die Probe („im vollen Lauf grün“ belegt bei einem Flackern nichts, B-153). Kosten:
   16 Probeläufe einer Datei, jeder Sekunden. Ausgeschlossener Fall: ein Fokus-Aufruf, den keine Zusicherung trägt – er wird
   dabei sichtbar und im Verlauf genannt, nicht still ergänzt.

## Akzeptanzkriterien

Je Kriterium: die Änderung, die es rot macht.

1. Die 24 positiven Fokus-Zusicherungen (16 in `SohnPractice.test.tsx`, 8 in `SohnTest.test.tsx`) laufen über `erwarteFokus` mit
   einer Zielfunktion und prüfen in `waitFor`. Rot, wenn ein Fokus-Effekt um 50 ms verzögert läuft (Probe aus
   Entscheidung 5a): die alte Form fällt, die neue besteht. Rot, wenn die Umstellung ein weiteres `act` ist: die Probe
   bleibt rot.
2. Jeder der acht Fokus-Aufrufe (Entscheidung 5) hat mindestens einen Fall, der rot wird, wenn der Aufruf entfällt (Probe 5b).
   Rot, wenn ein Aufruf von keinem Fall getragen wird; das steht dann im `## Verlauf` als Befund, kein stilles Weglassen.
3. Die drei verneinenden Zusicherungen bleiben ohne `waitFor` und tragen einen Kommentar mit dem Grund. Rot, wenn jemand sie
   auf `waitFor` umstellt – das lässt sich nicht prüfen, darum eine **Absichtserklärung** mit Durchsicht bei der Abnahme, kein
   automatisches Kriterium.
4. Nach den Proben sind `SohnPractice.tsx` und `SohnTest.tsx` byte-gleich zum Stand vor der Probe (Blob-Hash vor und nach
   verglichen; der Arbeitsbaum trägt fremde Änderungen, ein Vergleich mit `HEAD` taugt nicht). Rot, wenn die Hashes
   abweichen: eine Probe ist stehen geblieben.
5. `SohnProgress.test.tsx` wird nicht berührt (Durchsicht, kein Tor).

## Schätzung

**Größe: M** — wie der Batch-Pfad im `MediaSelector`: kein Entwurfsproblem, aber breit. Ein lokaler Helfer, 24 Stellen in
zwei Testdateien und 16 Probeläufe an acht Fokus-Aufrufen; jede Stelle ist klein, die Summe kostet.

- **`migration: nein`** — nachgesehen: nur Testdateien; kein Schema.
- **`vertragsbruch: nein`** — nachgesehen: `Pugling.Contracts` und `Pugling.Client` bleiben unberührt, Produktcode wird
  nur während der Proben verändert und danach byte-gleich zurückgespielt (Akzeptanzkriterium 4).
- **`wo: frontend`** — Vitest-Tests im Frontend; Reviewer `frontend-reviewer`.

### Risiken

1. **Fremde Änderungen im Arbeitsbaum.** `SohnPractice.tsx` und `SohnTest.tsx` tragen ungecommittete Arbeit (`git status`
   zeigt beide als geändert, dazu `Lektion.tsx`, `SohnApp.tsx`). Die Zahl acht der Fokus-Aufrufe und die Zeilen
   (`SohnPractice.tsx:179`, `:203`, `:207`, `:210`, `:213`, `:226`, `:370`, `SohnTest.tsx:99`) sind am 2026-11-19 gezählt
   und können driften; **vor Schritt 1 neu zählen**. Die Testdateien selbst sind unverändert
   (`SohnPractice.test.tsx` 17, `SohnTest.test.tsx` 10 `activeElement`-Stellen, davon 1 bzw. 2 verneinend).
2. **Eine Probe, die den Arbeitsbaum anfasst, ist heikel.** Entscheidung 5 verlangt Sicherungskopie und Blob-Hash vor und nach,
   kein `git checkout`. Das Zurückspielen per Kopie ist hier ungefährlich: Vitest liest die Quelle bei jedem Lauf frisch, es
   gibt keine Assembly, die die Probe weitertrüge (anders als beim Backend-Build).
3. **`waitFor` und Zielfunktion:** Entscheidung 2 verlangt, dass die Suche im Wiederholungsblock steht. Ein Ziel mit zwei
   Treffern wirft im Block und wird wiederholt, bis das Timeout (4 s, `test-setup.ts`) die Mehrdeutigkeit meldet statt des
   Fokus – gewollt, aber die Fehlermeldung zeigt dann nicht auf den Fokus.
4. **Die drei verneinenden Zusicherungen bleiben schwächer** (Entscheidung 3, dort benannt): unter Last ein falsches Grün
   möglich, nie ein falsches Rot.
5. **Der Befund kann sich bei Probe 5b ändern:** trägt ein Fokus-Aufruf keinen Fall, steht das als Befund im `## Verlauf`
   (Akzeptanzkriterium 2), und es entsteht kein stilles Ergänzen. Das kann eine Folge-Story erzeugen.

### Angriffsplan

1. Fokus-Aufrufe und Zusicherungen neu zählen (Risiko 1); Abweichung im Verlauf vermerken.
2. `erwarteFokus(ziel: () => HTMLElement)` lokal in `SohnPractice.test.tsx` und `SohnTest.test.tsx`
   (`await waitFor(() => expect(document.activeElement).toBe(ziel()))`).
3. Die 16 bzw. 8 positiven Stellen umstellen; die drei verneinenden (`SohnPractice.test.tsx:688`,
   `SohnTest.test.tsx:495` und `:508`) behalten den Takt und bekommen den Kommentar mit dem Grund.
4. **Messschritt M1 (Probe 5a, je Fokus-Aufruf):** Stand sichern, Blob-Hash notieren; je Aufruf in `setTimeout(…, 50)` hüllen;
   Dateien mit **alter** Form zeigen mindestens einen roten Fall, mit **neuer** Form alle grün.
5. **Messschritt M2 (Probe 5b):** je Aufruf entfernen; mindestens ein Fall rot, sonst Befund. Danach Stand zurückspielen und
   Blob-Hash gegenprüfen (Akzeptanzkriterium 4).
6. `tsc -b` und Vitest für beide Dateien, danach der volle Lauf.

### Testweg

- **Vitest:** `frontend/src/sohn/SohnPractice.test.tsx` und `frontend/src/sohn/SohnTest.test.tsx` (die Zusicherungen selbst).
  Rote Probe: Aufruf um 50 ms verzögert → alte Form rot, neue grün (Akzeptanzkriterium 1); Aufruf entfernt →
  mindestens ein Fall rot (Akzeptanzkriterium 2). Die Proben liegen in den **Komponenten**, nicht in den Tests, und das
  Zurückspielen ist Teil der Probe.
- **Messbefehl** (aus `frontend/`, lokale Binary): `./node_modules/.bin/vitest run src/sohn/SohnPractice.test.tsx src/sohn/SohnTest.test.tsx`;
  gezählt wird die Zahl der bestandenen Fälle, nicht der Exit-Code. Das Flackern selbst misst kein Lauf; die Beweiskraft liegt
  in der verzögerten Probe (deterministisch).
- Akzeptanzkriterien 3 und 5 sind Absichtserklärungen mit Durchsicht bei der Abnahme.
- Vor der Abnahme: `frontend-reviewer`; ein Rollengang ist nicht nötig (kein Verhalten ändert sich), das Fehlen steht dann
  als Zeile im `## Verlauf`.

## Verlauf

- **2026-11-16** — angelegt bei der Verifikation von B-598 (Sprint 82).
- **2026-11-19** — ausformuliert (Vorbereitungslauf, Sonnet): Der Fall in `SohnPractice` hat kein `act` (die Idee
  behauptete eines); Ursache am Code gelesen (Effekt nach Commit, `findBy` löst vorher aus); zwei weitere Meldungen im
  Protokoll nachgetragen; nichts gemessen oder gestartet.
- **2026-11-19** — gegrillt (Vorbereitungslauf, Sonnet, stellvertretend, unbestätigt): Entscheidungen 1 bis 5. Gezählt: 24 positive und 3 verneinende Fokus-Zusicherungen; alle positiven über einen lokalen Helfer mit `waitFor`, die verneinenden bleiben. Probe je Fokus-Aufruf (acht), nicht je Datei. `SohnProgress.test.tsx` zurückgestellt (keine Protokollstelle nennt einen Fall). Die Komponenten tragen im Arbeitsbaum fremde, ungecommittete Änderungen: Zeilen neu lesen, Proben per Kopie zurückspielen. Akzeptanzkriterien final, je mit roter Änderung.
- **2026-11-19** — geschaetzt (Vorbereitungslauf, Sonnet): M, frontend, keine Migration, kein Vertragsbruch (nachgesehen: nur Testdateien; Komponenten nur kurz durch Proben verändert). Nachgezählt: 17 und 10 `activeElement`-Stellen, 8 Fokus-Aufrufe, `waitFor` kommt in beiden Dateien nicht vor. Die Komponenten tragen fremde ungecommittete Änderungen: vor dem Bau neu zählen. Nichts gemessen oder gestartet.
