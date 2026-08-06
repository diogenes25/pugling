---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/qualitaet]
aliases: [full-flow.spec.ts hängt, Gewusst-Knopf detached]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: eigener Fund beim Bauen von B-62 (2026-08-05) — `git stash` gegen den unveränderten Stand
  bestätigte, dass derselbe Fehler schon vor B-62 bestand, also nicht durch B-62 verursacht
entgangen_bei: []
nachgeschaut: ""
---

# B-109 · `full-flow.spec.ts` hängt reproduzierbar bei „Frage 3/5" der Klausur

`frontend/e2e/full-flow.spec.ts` scheitert reproduzierbar (zweimal identisch beobachtet) im Klausur-Teil:
nach „Aufdecken 🔄" resolved der `Gewusst`-Locator zu einem `disabled`-Button, der dann aus dem DOM
entfernt wird, bevor er wieder anklickbar wird (`element was detached from the DOM, retrying`), bis der
Playwright-Timeout (60 s) greift. Beobachtet exakt bei „Frage 3 / 5" (die dritte `answerOne()`-Iteration
nach dem Wiedereintritt in die Klausur), beide Male mit identischem Fehlerbild.

**Nicht durch B-62 verursacht:** vor dem Bau von B-62 wurden alle betroffenen Dateien
(`SohnPractice.tsx`, `SohnTest.tsx`, `TestResult.tsx`, `full-flow.spec.ts` selbst) per `git stash`
auf den unveränderten Stand zurückgesetzt und der Test erneut gelaufen – derselbe Fehler an derselben
Stelle. Der Defekt ist vorbestehend.

Noch nicht untersucht: ob es ein reiner Timing-Flake ist (Busy-State/Re-Render-Fenster in
`SelfAssessAnswer`, `frontend/src/sohn/SohnTest.tsx:224-248`) oder ein echter Anwendungsfehler, der nur
unter der spezifischen Abfolge „Frage 1 beantworten → Klausur verlassen → wieder betreten → weiter
beantworten" auftritt (die einzige Stelle im Spec, die diese Abfolge testet). Ob der Fehler auch ohne
die Verlassen/Wiederbetreten-Sequenz reproduzierbar ist, ist offen.

## Der Schaden ist größer als die eine Spec (nachgetragen 2026-08-05)

Die Klausur-Sequenz liegt in **Zeile 80–122**, und alles, was danach kommt, läuft seither **nicht mehr**:

| Block | Zeile | Prüft |
| --- | --- | --- |
| Familien-Shop | 124–142 | Kauf und Kaufhistorie des Sohns (B-99) |
| Vater sieht Fortschritt | 143–152 | Tagesverlauf-Tabelle |
| Positions-Report | 153–167 | Vater-only-Sicht |
| Plan-übergreifender Lernstand | 168 ff. | Drilldown |

Damit ist der Ausfall nicht „ein Test rot", sondern **vier stillgelegte Prüfflächen**. Aufgefallen ist es
erst, als B-110 einen Testweg für den Kaufverlauf suchte und den vorhandenen Shop-Block nicht nutzen
konnte.

**Die Zustellung funktioniert dabei** — das ist ausdrücklich *nicht* die Lücke: `.github/workflows/e2e.yml`
fährt die Suite an jedem Pull Request und nachts um 03:00 UTC auf `main` und stellt ein Rot als Issue mit
Zustand zu (aus B-26). Nur `ci.yml` selbst fährt kein E2E, und das ist dort eine begründete Entscheidung.
Was die Meldung **nicht** sagt, ist der eigentliche Punkt: „E2E ist rot" nennt den gescheiterten Schritt,
nicht die vier Flächen, die dahinter aufgehört haben, geprüft zu werden. Der Umfang des Schadens ist aus
dem Signal nicht ablesbar.

Daraus folgt eine zweite, allgemeinere Frage für die Ausformulierung: **eine lange Spec ist ein einziger
Ausfallpunkt.** Ob der Vater→Sohn-Durchstich weiter als *eine* Datei laufen soll oder in Abschnitte
zerfällt, die einzeln scheitern können, ist zu entscheiden (Empfehlung: der Durchstich bleibt als *ein*
Weg erhalten — sein Wert ist ja die Kette —, aber neue Flächen bekommen eigene, kurze Specs, damit sie
nicht hinter fremden Schritten liegen; B-110 legt `e2e/shop-verlauf.spec.ts` schon so an).

## User Story

Als Entwickler möchte ich, dass der Vater→Sohn-Durchstich die Klausur ohne Hänger durchspielt, damit die
vier Prüfflächen dahinter (Shop, Tagesverlauf, Positions-Report, Lernstand) wieder tatsächlich geprüft
werden und ein rotes E2E wieder eine Aussage über das Produkt ist statt über die Spec.

## Ist-Stand am Code

Belegt am 2026-08-06 gegen den damaligen `HEAD` (`0ffaaad`), nicht aus der Notiz übernommen:

- **`frontend/src/sohn/SohnTest.tsx:100-110`** — `answerAndAdvance` fährt in **einem** `action.run` zwei
  Rundreisen (`api.answerTest`, dann `api.nextTest`). Erst danach fallen `setItem`/`setCursor`/
  `setRevealed(false)` gemeinsam an.
- **`frontend/src/sohn/SohnTest.tsx:242-243`** — währenddessen steht die **vorige** Karte unverändert im
  DOM: „Nicht gewusst"/„Gewusst" nur `disabled={busy}`, kein „Aufdecken".
- **`frontend/src/sohn/SohnTest.tsx:133`** — die Frage-Nummer rendert aus `cursor`, also aus demselben
  Zustandssprung wie die Karte.
- **`frontend/e2e/full-flow.spec.ts`** (Fassung `c7100bc`, Zeilen 86-112) — `answerOne()` ohne
  Wartepunkt; der erste Aufruf war durch die Zusicherungen davor gedeckt, die Schleife dahinter nicht.

## Die echte Lücke

**Eine Wettlaufsituation in der Spec, kein Fehler der App** — damit ist die oben offene Frage beantwortet.
`answerOne()` lief in das Zwei-Rundreisen-Fenster hinein: die Prüfung auf „Aufdecken" traf die alte Karte
(kein Treffer → Schritt übersprungen), der Klick griff das alte, deaktivierte „Gewusst", das beim
Kartenwechsel aus dem DOM fiel — und die neue Karte zeigt vor dem Aufdecken kein „Gewusst". Danach wartete
der Locator 60 s auf etwas, das ohne den übersprungenen Schritt nie erscheint. Für ein Kind ist der Ablauf
unauffällig: Knopf sperrt, zwei Rundreisen, neue Karte.

## Offene Punkte

1. ~~Timing-Flake oder echter Anwendungsfehler?~~ → beantwortet, siehe „Die echte Lücke".
2. ~~Tritt der Fehler auch ohne die Verlassen/Wiederbetreten-Sequenz auf?~~ → ja; die Sequenz ist nicht
   die Ursache, sie verschiebt nur, welche Iteration als erste ungedeckt läuft.
3. **Zurückgestellt:** ob der Durchstich in mehrere Specs zerfällt. Der Fix nimmt der Frage die
   Dringlichkeit, nicht die Berechtigung — sie gehört in eine eigene Story, nicht als Anhang hierher
   (README → „Ein Fund beim Bauen wird eine eigene Story").

## Entscheidungen

1. **Der Wartepunkt ist die Frage-Nummer, nicht der Knopf.** Begründung: sie entsteht im selben
   Zustandssprung wie die Karte, ist also genau dann richtig, wenn die Karte es ist. Kosten: jeder Aufruf
   von `answerOne` muss seine erwartete Nummer kennen — eine Zeile je Aufrufstelle.
2. **Kein `waitForTimeout`.** Begründung: eine Wartezeit verdeckt den Wettlauf, statt ihn zu schließen,
   und wäre auf einem langsameren CI-Runner wieder zu kurz. Kosten: keine.
3. **Kein Eingriff in die App.** Begründung: `disabled` während zweier Rundreisen ist das richtige
   Verhalten; der Fehler lag in der Erwartung der Spec. Kosten: die Spec trägt das Wissen über das
   Zwei-Rundreisen-Fenster nun als Kommentar — sie muss nachgezogen werden, wenn `answerAndAdvance`
   einmal nur noch eine Rundreise fährt.
4. **`shop-verlauf.spec.ts` zählt Zeilen, statt „leer" zu verlangen.** Begründung: ihre bisherige
   Vorbedingung war eine Nebenwirkung genau dieses Ausfalls. Kosten: eine Zusicherung mehr — die dafür
   auch schärfer ist als der Titelvergleich.

## Akzeptanzkriterien

- `full-flow.spec.ts` läuft bis „SIEG!" durch, die vier Blöcke dahinter laufen mit. ✅
- Kein `waitForTimeout` im Fix. ✅
- Die ganze E2E-Suite ist grün, nicht nur die reparierte Spec. ✅ (28/28)

## Schätzung

`S` — die Änderung ist klein (ein Parameter, eine Wartezeile, eine Nachbar-Spec), teuer war die Diagnose.
`migration: nein`, `vertragsbruch: nein`: keine Zeile Produktionscode geändert.

**Testweg:** die Spec selbst (`frontend/e2e/full-flow.spec.ts`) plus die volle Playwright-Suite — bei einem
E2E-Fix ist die *ganze* Suite der Testweg, weil Specs eine Wegwerf-DB teilen (genau das hat Entscheidung 4
zutage gefördert).

## Verlauf

- **2026-08-05** — angelegt aus dem eigenen Fund beim Bauen von B-62; die Vorbestehen-Prüfung per
  `git stash` steht oben.
- **2026-08-05** — Schadensbild nachgetragen (bleibt `idee`, die Ursache ist weiter unbekannt): der
  Ausfall stellt vier weitere Prüfflächen derselben Spec still, darunter den Shop-Verlauf. Der Fund kam
  aus der Testweg-Suche für B-110, nicht aus einer neuen Messung an der Ursache selbst.
- **2026-08-06** — **Ursache gefunden und behoben**, angestoßen vom roten Nachtlauf des E2E-Workflows
  (Lauf `31075202977` auf `c7100bc`). Die oben offene Frage „Timing-Flake oder echter
  Anwendungsfehler" ist damit beantwortet: **eine Wettlaufsituation in der Spec, kein Fehler der App.**

  `answerAndAdvance` ([frontend/src/sohn/SohnTest.tsx:100-110](../../frontend/src/sohn/SohnTest.tsx))
  fährt in **einem** `action.run` zwei Rundreisen — `answerTest`, dann `nextTest`. Solange die laufen, steht
  die **vorige** Karte unverändert im DOM: „Gewusst" nur `disabled` (Zeile 243), „Aufdecken" gar nicht.
  `answerOne()` besaß keinen Wartepunkt und lief in genau dieses Fenster: `revealBtn.count()` traf die alte
  Karte (0 Treffer → Aufdecken übersprungen), der anschließende Klick griff das alte, deaktivierte „Gewusst",
  das beim Kartenwechsel aus dem DOM fiel — und die neue Karte zeigt vor dem Aufdecken kein „Gewusst" mehr.
  Der Locator wartete danach auf etwas, das ohne den übersprungenen Schritt nie erscheint: 60 s Timeout.

  Das erklärt auch die Stelle: der **erste** `answerOne()`-Aufruf war durch die Zusicherungen davor gedeckt,
  die Schleife `for (…) await answerOne()` nicht — darum traf es die dritte Frage, die zweite Iteration nach
  dem Wiedereintritt. Behoben, indem der Wartepunkt die **Frage-Nummer** wird (`Frage n / total`): sie kommt
  aus demselben Zustandssprung wie die Karte (`setCursor`/`setItem`/`setRevealed(false)`), ist also genau
  dann richtig, wenn die Karte es ist. Kein `waitForTimeout`.

  Belegt: derselbe Fehler an derselben Stelle am aktuellen `HEAD` reproduziert (also nicht vom Code-Split
  `0ffaaad` verursacht), nach der Änderung `1 passed (53.6s)`; damit laufen die vier stillgelegten Blöcke
  (Shop, Vater-Fortschritt, Positions-Report, Lernstand) wieder mit. Commit: siehe unten.

  **Der Fix legte eine zweite, stille Kopplung frei.** Mit wieder laufendem Shop-Block kauft der Durchstich
  in derselben Wegwerf-DB für dasselbe geseedete Kind ein — und `shop-verlauf.spec.ts` (alphabetisch danach,
  `workers: 1`) verlangte in Zeile 48 „Noch nichts gekauft". Diese Vorbedingung war nie eine Eigenschaft
  der Spec, sondern eine **Nebenwirkung genau dieses Ausfalls**; ihr eigener Kopfkommentar benennt die
  Abhängigkeit sogar („läuft, solange B-109 offen ist, überhaupt nicht mit"). Belegt durch drei Läufe:
  allein grün, im Paar mit `full-flow` rot an Zeile 48, nach der Änderung im Paar grün. Sie zählt jetzt die
  Verlaufszeilen vor und nach dem Kauf (`+1`) statt auf „leer" zu prüfen — das ist auch die **schärfere**
  Zusicherung, weil beide Specs dieselbe erste Karte greifen und der Titel allein einen ausgebliebenen
  Nachschlag nicht bemerkt hätte.

  **Offen bleibt der zweite Teil der Story** — „eine lange Spec ist ein einziger Ausfallpunkt". Die
  Aufteilungsfrage ist mit diesem Fix *nicht* entschieden, nur ihre Dringlichkeit gesunken. Der Fund oben
  fügt ihr eine Frage hinzu: eine Spec, die auf dem Ausfall einer anderen ruht, sieht grün aus und ist es
  nicht.
- **2026-08-06** — `idee` → **`abgenommen`**, auf ausdrückliche Entscheidung des Nutzers **unter Auslassung
  der Zwischenstufen**: die Arbeit war bereits gebaut und belegt, als die Story gesichtet wurde
  (Einstieg war das rote CI-Tor, nicht der Backlog). `ausformuliert`/`gegrillt`/`geschaetzt` sind darum
  nachgetragen statt erarbeitet — das ist hier festgehalten, weil die Stufen sonst mehr behaupten, als
  der Weg hergibt. Belege: Fehler am `HEAD` reproduziert; `shop-verlauf` allein grün, im Paar mit
  `full-flow` rot an Zeile 48, nach der Änderung im Paar grün; **volle E2E-Suite 28/28 grün (2,0 min)**;
  `tsc --noEmit` und `markdownlint-cli2` sauber. Commit `58a9cdb`. **Rollengang:** der Durchstich selbst
  fährt den Weg Vater→Sohn an der laufenden App — laut README zählt eine E2E, die den Weg fährt, als
  Rollengang. **Reviewer:** keiner — die Änderung liegt vollständig in `frontend/e2e/`, also weder im
  Backend (`pugling-reviewer`) noch in der Anwendungsschicht, für die `frontend-reviewer` gedacht ist;
  das ist eine bewusste Auslassung, keine vergessene.
- **2026-08-06** — `entgangen_bei` bleibt leer: der Defekt ist Altbestand ohne zuordenbare abgenommene
  Story — genau der Fall, den `README.md` in seiner Tabelle **an dieser Story** als Beispiel führt.
