---
tags: [typ/story, status/abgenommen, bereich/qualitaet, rolle/entwickler]
aliases: [Feldhilfe-Spec flackert]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
nachgeschaut: "2026-11-22"
quelle: Sprint 82, Verifikation von B-597 (pm-sitzung-2026-11-16-backlog-lauf.md)
grund: ""
ersetzt_durch: []
---

# B-602 · `feldhilfe.spec.ts` flackert beim „Ziel-Rhythmus“

Im Sprint 82 fiel `frontend/e2e/feldhilfe.spec.ts` („Positions-Formular: jedes erklärungsbedürftige Feld hat den
passenden Hinweis“) in einem von drei Einzelläufen rot: Nach dem Klick auf das „ⓘ“ am Feld „Ziel-Rhythmus“ erschien
kein `note` mit „Wochenziel“ einmal pro Woche“ (10 s Wartezeit). Im vollen Lauf fiel einmal stattdessen ein Fall mit dem
Münz-Malus (ebenfalls `getByRole("note")`). Der Diff des Sprints berührt das Positionsformular nicht.

## User Story

Als Entwickler möchte ich, dass die Feldhilfe-Spezifikation nur dann rot wird, wenn ein Hinweis am falschen Feld hängt
oder nicht aufgeht, damit ein Rot dieser Spec nie mehr „einfach nochmal laufen lassen“ heißt.

## Ist-Stand am Code

Die Vermutung der Idee („der Klick kommt, während das Formular noch nachlädt“) stimmt, und der Mechanismus ist
**gewollt**, nicht zufällig:

1. **Das Popover schließt, sobald sich sein Knopf bewegt.** `InfoHint` merkt sich beim Öffnen, wo der Knopf stand
   (`frontend/src/components/InfoHint.tsx:80`), und prüft je Animationsframe, ob er dort noch steht
   (`InfoHint.tsx:38-48`, Schwelle 1 px). Eine Bewegung ruft `close()`. Das ist Absicht (B-273, Akzeptanzkriterium 1:
   „Inhalt, der über dem Knopf aufgeht, schließt ein offenes ⓘ“, `docs/backlog/B-273-feste-feldhilfe-resize-und-layoutverschiebung.md:74`).
2. **Über dem „Ziel-Rhythmus“-Feld lädt Inhalt nach.** Das Feld sitzt in `PositionFields`, und das steht im Planbau
   *unterhalb* der Filterleiste und der Trefferliste der Katalog-Übungen (`frontend/src/vater/PlanPositions.tsx:883`,
   Liste ab `:794`). Die Liste kommt aus einer eigenen Abfrage (`useAsync(... api.searchExercises ...)`,
   `PlanPositions.tsx:642`). Bis sie antwortet, steht dort ein einzeiliges „Lade…“ (`:794`); danach eine Liste mit
   bis zu 240 px Höhe (`:799`). Das Formular selbst erscheint dagegen schon mit dem Bestand der Positionen
   (`PlanPositions.tsx:106`), also früher. Antwortet die Suche *nach* dem Klick auf das „ⓘ“, springt der Knopf nach
   unten, `moved()` greift, das Popover schließt – und `getByRole("note")` findet nichts.
3. **Die Spec wartet nur auf den Knopf, nicht auf die Liste.** Sie legt den Plan an und wartet auf die Überschrift
   (`frontend/e2e/feldhilfe.spec.ts:30`), dann auf die Sichtbarkeit des ersten Knopfs (`:63`) und klickt
   (`:64`). Die Trefferzahl „(N Treffer)“ als Zeichen, dass die Liste steht (`PlanPositions.tsx:756`), kommt in der
   Spec nicht vor. *Korrektur beim Grillen: Die Zahl taugt nicht als Zeichen – beim Erstladen steht schon „(0 Treffer)“
   (Entscheidung 2).* Im zweiten Fall der Datei (Escape, `:71-88`) wartet nicht einmal eine `toBeVisible`-Zusicherung vor
   dem Klick (`:77`); Playwrights Stabilitätsprüfung deckt nur zwei Frames ab.
4. **Dieselbe Lage in zwei weiteren Fällen derselben Datei:** der Gleichlaut-Fall klickt am Planbau
   (`feldhilfe.spec.ts:94-96`) und im Assistenten (`:112-115`) ohne Warten auf die Trefferliste. Die im Backlog
   genannte Zeile `:89` ist durch Commits seit Sprint 82 verrutscht; der Münz-Malus steht heute auf `:47`, `:75`
   und `:94`.
5. **Die Last-Abhängigkeit passt dazu:** Der Playwright-Lauf nutzt einen Worker (`workers: 1`) und eine echte Wegwerf-DB
   (`frontend/playwright.config.ts:46-47`), die Suchantwort kommt also je nach Last früh oder spät. Einzeln und ohne
   Last gewinnt die Liste das Rennen meistens – 2 von 3 Läufen grün entspricht dem.

Nicht nachgestellt: Messen und Starten sind in diesem Lauf nicht erlaubt. Der Mechanismus ist am Code gelesen; dass er
den beobachteten Fall erzeugt, belegt erst die rote Probe unten (Akzeptanzkriterium 3).

## Die echte Lücke

Kein Fehler im Produkt, sondern eine **Spec ohne Wartebedingung** für etwas, das der Produktcode absichtlich nicht
toleriert: Die Spec klickt in ein Layout, das sich noch bewegt. Zwei Dinge sind schmaler als die Idee dachte:

- Es ist nicht der erste Klick allein. Jeder Klick auf ein „ⓘ“ vor dem Ende der Suche ist betroffen, in drei der
  drei Fälle der Datei.
- Die Ursache ist deterministisch herstellbar (Suchantwort verzögern), also lässt sich die Reparatur *beweisen*
  statt durch mehrere grüne Läufe zu „belegen“.

Ein Randbefund für den Nutzer, kein Teil dieser Story: Ein Vater, der in diesen Sekunden auf ein „ⓘ“ tippt, sieht
genauso ein sich schließendes Popover. Die Feldhilfe-Position ändert sich nicht, die Liste darüber springt.

## Offene Punkte

1. ~~**Nur die Spec reparieren oder auch die Layoutverschiebung beseitigen?**~~ → Entscheidung 1.
2. ~~**Worauf wartet die Spec?**~~ → Entscheidung 2 (die Empfehlung „(N Treffer)“ trägt nicht).
3. ~~**Eine Hilfsfunktion für alle drei Fälle?**~~ → Entscheidung 3.
4. ~~**Gehört die Probe mit verzögerter Suchantwort in die Spec?**~~ → Entscheidung 4 (anders als im Entwurf).
5. ~~**Braucht der Assistent im Gleichlaut-Fall eine Wartebedingung?**~~ → Entscheidung 5 (neu beim Grillen).

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-19 (Sonnet), unbestätigt.*

1. **Nur die Spec wird repariert; das Schließen bei Bewegung und die springende Liste bleiben.** Begründung: Das Schließen
   ist durch B-273 beschlossen (Akzeptanzkriterium 1, `B-273-feste-feldhilfe-resize-und-layoutverschiebung.md:74`) und
   hat eine eigene Spec, `feldhilfe-lage.spec.ts:213` („ein Inhalt, der über dem Knopf aufgeht, schließt ein offenes ⓘ“).
   Die Höhe der Trefferliste zu reservieren wäre ein Produktumbau für Handy-Nutzer. Verworfen: die Liste mit fester Höhe
   rendern (ein eigenes Vorhaben, das auch die Spec grün machte, aber an der Ursache im Produkt dreht, nicht am
   Defekt „Spec ohne Wartebedingung“). Kosten: Ein Vater, der in den ersten Sekunden auf ein ⓘ tippt, erlebt das
   schließende Popover weiter (siehe „Die echte Lücke“, Randbefund). Ausgeschlossener Fall: dieser Vater; gegen die User
   Story (Entwickler, „Rot nur bei falschem Feld oder nicht aufgehendem Hinweis“) trägt das, weil sie nur das Rot der Spec
   meint.
2. **Die Spec wartet auf die Radiogruppe der Trefferliste, nicht auf „(N Treffer)“:
   `page.getByRole("radiogroup", { name: "Übung wählen" })` sichtbar.** Die Empfehlung der Ausformulierung trägt nicht, am Code
   geprüft: Während der **ersten** Abfrage steht die Beschriftung bereits bei „(0 Treffer)“ (`PlanPositions.tsx:756`:
   weder `sucheGescheitert` noch `zeilenVeraltet`; `zeilenVeraltet` ist `exercises.stale`, und `stale` ist beim Erstladen
   falsch, weil `dataDeps` mit den Abhängigkeiten des ersten Renders beginnt, `useAsync.ts:59` und `:80`; `results` ist
   `[]`, solange `data` fehlt, `PlanPositions.tsx:734`). Eine Wartebedingung `(\d+ Treffer)` wäre also vor der Antwort schon
   erfüllt und könnte nie rot werden. Die Radiogruppe ersetzt das „Lade…“ erst, wenn die Antwort da ist
   (`PlanPositions.tsx:794-798`: `exercises.loading && exercises.data === null`). Verworfen: der Treffer-Text (steht
   vorher), `waitForTimeout` (Zeitraten) und ein Wiederholen des Klicks mit `toPass` (verdeckt genau die Bewegung, die
   B-273 meldet). Kosten: Die Bedingung hängt an einer Rolle und einem Namen der Oberfläche; wird die Gruppe umbenannt,
   fällt die Spec laut (Timeout) statt still. Bei einer gescheiterten Suche erscheint die Gruppe ebenfalls – die Spec
   wartet dann nicht auf Treffer, sondern auf das Ende des Ladens, und das ist hier gemeint. Ausgeschlossener Fall: eine Seite,
   deren Liste ausfällt; gegen die User Story gerechnet bleibt das Rot dort ein Rot der Seite, nicht ein Flackern der Spec.
3. **Die Wartebedingung steht einmal, am Ende von `planAnlegen` in `feldhilfe.spec.ts:25-31`.** Begründung: Alle drei Fälle der Datei
   (`:35`, `:73`, `:92`) legen den Plan darüber an, und der Escape-Fall hat vor seinem ersten Klick nicht einmal eine
   `toBeVisible`-Zusicherung (`:75-77`). Die Datei hat ihre Hilfsfunktionen schon lokal (`planAnlegen`, `vaterLogin`);
   `e2e/helpers.ts` führt `vaterLogin` zwar ebenfalls, zehn Specs definieren aber ihre eigene (Suche nach
   `async function vaterLogin`), und `planbau-ladefenster.spec.ts:29` hat `planbauOeffnen` ebenso lokal. Verworfen: je ein Warten
   vor jedem Klick (drei Stellen, eine wird vergessen) und der Umzug in `helpers.ts` (nur diese Datei braucht ihn, und ein
   gemeinsamer Helfer für „Planbau steht“ wäre eine zweite Stelle neben `planbauOeffnen`). Kosten: `planAnlegen` wartet
   künftig auf die Liste, auch wo ein Fall sie nicht braucht (der Escape-Fall); es kostet Millisekunden. Ausgeschlossener
   Fall: eine vierte Spec, die später `planAnlegen` kopiert und die Wartebedingung vergisst; das fängt der Spät-Fall aus
   Entscheidung 4 nicht (er prüft nur diese Datei) – dort steht der Kommentar mit dem Grund.
4. **Ein Dauerfall hält die Wartebedingung ehrlich: die Suchantwort wird um 1,5 s verzögert, nach `planAnlegen` wird das ⓘ am
   „Ziel-Rhythmus“ geöffnet, zwei Frames nach dem Eintreffen der Liste muss es noch offen sein.** Begründung: Ohne diesen Fall
   ist die Wartebedingung eine Zusicherung, die niemand rot sehen kann – ein späteres Aufräumen („wartet ohne Grund“) brächte
   das Flackern zurück, unbemerkt. Das Schließen bei Bewegung selbst bleibt bei `feldhilfe-lage.spec.ts:213`; der neue Fall
   prüft das Gegenstück: **mit** der Wartebedingung bleibt das Popover offen. Das Muster der Verzögerung steht in
   `planbau-ladefenster.spec.ts:57` (`page.route("**/api/v1/creator/exercises?**", …)` mit `route.fallback()`); die Route wird hier
   **vor** `planAnlegen` gesetzt, weil die Liste beim Öffnen der Seite lädt. Der Fall muss rot werden, wenn die Bedingung
   fehlt: dann öffnet das ⓘ vor der Antwort, die Liste springt, das Popover schließt. Verworfen: eine Wegwerf-Probe nur beim
   Bauen (belegt die Reparatur einmal, nicht dauerhaft) und ein Fall, der das Schließen des Popovers erwartet (das
   dupliziert `feldhilfe-lage.spec.ts:213`). Kosten: rund 1,5 s mehr in einer Suite mit 166 Fällen. Ausgeschlossener
   Fall: ein Flackern, das nicht von der Trefferliste kommt (etwa ein späterer Ladevorgang über dem Formular); gegen die User
   Story („Rot heißt nie mehr einfach nochmal laufen lassen“) ist das die Grenze: ein neuer Auslöser braucht seinen
   eigenen Fall.
5. **Der Assistent im Gleichlaut-Fall bekommt keine Wartebedingung.** Das ⓘ steht im Schritt „Feinschliff“
   (`VaterWizard.tsx:667-668`, `step === 3`), und die Trefferliste gehört zu Schritt 3 (`step === 2`, `:564`); beim Klick auf
   „Weiter“ ist sie nicht mehr gerendert, über dem ⓘ lädt nichts nach (nicht gefunden: weitere Abfrage in Schritt 4 im Code, die
   Prüfung ist gelesen, nicht gemessen). Verworfen: eine Wartebedingung „auf Verdacht“ (ein Warten ohne Ursache ist eine
   zweite Zusicherung, die nie rot werden kann). Kosten: Flackert dieser Teil doch, entsteht eine eigene Story. Ausgeschlossener
   Fall: ein Flackern im Assistenten; gegen die User Story ist es nicht belegt, also nicht Teil des Defekts.

## Akzeptanzkriterien

Je Kriterium: die Änderung, die es rot macht.

1. `planAnlegen` wartet am Ende auf `getByRole("radiogroup", { name: "Übung wählen" })`, und alle drei bestehenden Fälle der
   Datei laufen über sie. Rot, wenn die Zeile fehlt: der Spät-Fall aus Kriterium 2 fällt.
2. Ein neuer Fall verzögert die Antwort auf `**/api/v1/creator/exercises?**` um 1,5 s (Route **vor** `planAnlegen`), öffnet nach
   `planAnlegen` das ⓘ „Ziel-Rhythmus“, wartet auf die Trefferliste und zwei Frames und erwartet `aria-expanded="true"` und
   den sichtbaren Hinweis „Wochenziel“ einmal pro Woche“. Rot, wenn `planAnlegen` die Wartebedingung verliert: das ⓘ öffnet vor
   der Antwort, die Liste springt, das Popover ist zu.
3. Die Wartebedingung ist nicht auf „(N Treffer)“ gegründet. Probe bei der Abnahme: ersetzt man die Zeile in `planAnlegen`
   durch `getByText(/\(\d+ Treffer\)/)`, fällt der Spät-Fall (die Beschriftung steht vor der Antwort schon bei „(0 Treffer)“).
   Rot, wenn er unter dieser Ersetzung grün bleibt: dann wartet die Zeile nicht auf die Antwort, und die Probe hat nichts
   gemessen.
4. Keine Stelle der Datei verwendet `waitForTimeout` oder eine Wiederholungsschleife um einen Klick. Rot, wenn ein Wartebefehl
   diese Form hat; Durchsicht bei der Abnahme (Suche in der Datei), kein Tor.
5. Die übrigen Fälle der Datei (Escape-Fall, Gleichlaut-Fall mit Assistent) bleiben unverändert bis auf den Aufruf von
   `planAnlegen`. Rot, wenn der Diff den Assistenten-Teil (`:101-116`) berührt – Durchsicht, kein Tor.

## Schätzung

**Größe: S** — größer als der XS-Anker (zwei Sätze plus ein E2E): eine Zeile in einer Hilfsfunktion, aber dazu ein neuer
Dauerfall mit verzögerter Antwort nach dem Muster von `planbau-ladefenster.spec.ts`. Kein Produktcode, daher klein.

- **`migration: nein`** — nachgesehen: Es ändert sich nur `frontend/e2e/feldhilfe.spec.ts`; kein Schema, kein Backend.
- **`vertragsbruch: nein`** — nachgesehen: `Pugling.Contracts` bleibt unberührt, die Spec ruft nur die Oberfläche.
- **`wo: frontend`** — E2E-Spezifikation im Frontend; `frontend/CLAUDE.md` lädt dort selbst, Reviewer
  `frontend-reviewer`.

### Risiken

1. **Die Verzögerung muss wirklich greifen.** Der Glob `**/api/v1/creator/exercises?**` stammt aus
   `planbau-ladefenster.spec.ts:57`; `api.searchExercises` hängt Paging an (`api.ts:538`, `appendPaging`), die Adresse
   trägt also in der Regel ein `?`. Greift die Route doch nicht, besteht der neue Fall auch ohne Wartebedingung und beweist
   nichts – genau das fängt die rote Probe (Schritt 3 unten). Nicht gemessen.
2. **Die Radiogruppe erscheint nur, wenn die Antwort da ist:** `exercises.loading && exercises.data === null` ersetzt
   „Lade…“ (`PlanPositions.tsx:794`); `loading` startet mit `true` (`useAsync.ts:51`), die Bedingung ist also schon im
   ersten Render erfüllt. Gelesen, nicht gemessen.
3. **Folge-Layoutwechsel nach der Liste** (etwa ein weiterer Ladevorgang über dem Formular) wäre durch die Wartebedingung
   nicht gedeckt – so entschieden (Entscheidung 4, ausgeschlossener Fall). Der Dauerfall prüft zwei Frames **nach** der
   Liste und wäre dann der erste, der fällt.
4. **Die Spec öffnet im ersten Fall ein eingeklapptes `summary`** (`feldhilfe.spec.ts:57`): auch das verschiebt Knöpfe,
   aber unterhalb der Ziel-Felder und vor der Schleife – nicht Teil des Befunds, hier nur gesehen.
5. **Der Lauf teilt sich die Maschine mit dem Heimnetz-Server.** Die Messung läuft auf Ausweichports
   (`PUGLING_E2E_API_PORT`, `PUGLING_E2E_WEB_PORT`, `playwright.config.ts:21-22`), nie auf `:5200` (echte DB der Söhne).

### Angriffsplan

1. `planAnlegen` (`feldhilfe.spec.ts:25-31`) bekommt als letzte Zeile
   `await expect(page.getByRole("radiogroup", { name: "Übung wählen" })).toBeVisible();` samt Kommentar (Grund: B-273
   schließt ein offenes ⓘ, sobald sein Knopf wandert; die Liste lädt unter dem Feld nach).
2. Neuer Fall am Ende der Datei (Entscheidung 4): Route mit 1,5 s Verzögerung **vor** `planAnlegen`, danach ⓘ
   „Ziel-Rhythmus“ öffnen, auf die Trefferliste und zwei Frames warten, `aria-expanded="true"` und Hinweistext prüfen.
3. **Messschritt M1 (rote Probe, Akzeptanzkriterien 1 und 3):** Wartezeile aus `planAnlegen` entfernen, nur den neuen Fall
   laufen lassen – er muss rot sein; dann die Zeile durch `getByText(/\(\d+ Treffer\)/)` ersetzen – er muss ebenfalls rot
   sein; dann Zeile wiederherstellen – grün. Danach alle drei alten Fälle einzeln je drei Mal und die ganze Datei einmal.
4. Durchsicht Akzeptanzkriterien 4 und 5 (kein `waitForTimeout`, Diff berührt `:101-116` nicht).

### Testweg

- **E2E-Spec:** `frontend/e2e/feldhilfe.spec.ts` selbst (der neue Fall trägt die Wartebedingung). Rote Probe liegt in der
  **Spec**, nicht im Produkt: Entfernen der Wartezeile in `planAnlegen` macht den neuen Fall rot, weil das ⓘ vor der
  Antwort öffnet und die Liste es anschließend schließt (`InfoHint.tsx:38-48`). Das Gegenstück zum Produkt bleibt
  `feldhilfe-lage.spec.ts:213`, unverändert.
- **Messbefehl** (aus `frontend/`, lokale Binary, nicht `npx`; Ausweichports, Release-Build, weil auf `:5200` der
  Heimnetz-Server die Debug-Ausgabe sperrt):
  `PUGLING_E2E_API_PORT=5201 PUGLING_E2E_WEB_PORT=5174 PUGLING_E2E_CONFIGURATION=Release ./node_modules/.bin/playwright test e2e/feldhilfe.spec.ts`;
  gezählt wird „N passed“, nicht der Exit-Code. Ein Lauf misst bei einem Flackern wenig: die Beweiskraft liegt in der
  verzögerten Antwort (deterministisch), nicht in grünen Wiederholungen.
- Vor der Abnahme: `frontend-reviewer`; ein Rollengang als Vater an der Plan-Seite (ⓘ „Ziel-Rhythmus“ gleich nach dem Öffnen
  antippen) – dort bleibt das schließende Popover als Randbefund sichtbar, bewusst.

## Verlauf

- **2026-11-16** — angelegt bei der Verifikation von B-597 (Sprint 82): 2 von 3 Einzelläufen grün.
- **2026-11-19** — ausformuliert (Vorbereitungslauf, Sonnet): Ursache am Code gelesen (`InfoHint.tsx` schließt bei
  Bewegung des Knopfs, B-273; die Trefferliste unter dem Formular lädt nach), Zeilennummern der Idee waren verrutscht;
  nichts gemessen oder gestartet.
- **2026-11-19** — gegrillt (Vorbereitungslauf, Sonnet, stellvertretend, unbestätigt): Entscheidungen 1 bis 5. Gegen den Code korrigiert: die empfohlene Wartebedingung „(N Treffer)“ trüge nicht, die Beschriftung steht beim Erstladen schon bei „(0 Treffer)“; gewartet wird auf die Radiogruppe der Trefferliste, einmal in `planAnlegen`. Ein Dauerfall mit verzögerter Suchantwort hält die Bedingung ehrlich. Der Assistent bleibt unberührt. Akzeptanzkriterien final, je mit roter Änderung.
- **2026-11-19** — geschaetzt (Vorbereitungslauf, Sonnet): S, frontend, keine Migration, kein Vertragsbruch (nachgesehen: nur `feldhilfe.spec.ts`, kein Produktcode). Rote Probe benannt (Wartezeile entfernen; zweite Probe mit „(N Treffer)“); Messbefehl auf Ausweichports. Offen bleibt, ob die Route-Verzögerung greift – genau das zeigt die erste Probe. Nichts gemessen oder gestartet.
- **2026-11-21** — weiterer Fall im Nachtlauf (Sprint 98, voller E2E-Lauf auf 5201/5174): diesmal
  `feldhilfe.spec.ts:72` („Hinweis schließt per Escape und per Klick daneben“), `getByRole('note')` nach dem Klick auf
  den Knopf nicht sichtbar. Einzeln wiederholt `--repeat-each=3`: 9/9 grün. Dieselbe Spec, ein anderer Fall als der
  „Ziel-Rhythmus“; die Ursache aus der Ausformulierung (`InfoHint` schließt bei Bewegung des Knopfs) passt auch hier.
- **2026-11-22** — in Arbeit (Nachtlauf B, Worktree `claude/serene-bhabha-1b1ff6`, E2E auf 5202/5175). `planAnlegen` wartet
  auf `radiogroup "Übung wählen"`; neuer Fall „Ein ⓘ, das nach dem Anlegen geöffnet wird, bleibt auch bei langsamer Suche
  offen“.
  - **Review-Fund (frontend-reviewer), eingearbeitet:** Der Glob `**/api/v1/creator/exercises?**` aus
    `planbau-ladefenster.spec.ts` griff hier nie – das Erstladen sucht ohne Filter, die Adresse hat kein `?`
    (`api.ts:527-542`). Risiko 1 der Schätzung war damit real; die erste rote Probe war Latenz, kein Beweis. Jetzt
    Pfad-Prädikat `u.pathname === "/api/v1/creator/exercises"` und ein Trefferzähler mit `expect(verzoegert).toBeGreaterThan(0)`.
  - **AK 1/2, Wartezeile → Spät-Fall:** ohne Zeile in `planAnlegen` rot an `aria-expanded` (`feldhilfe.spec.ts:142`); mit
    Zeile grün; Route getroffen (Zähler). Gemessen mit `scratchpad/b602probe.py` nach der letzten Änderung.
  - **AK 3, „(N Treffer)“ statt Radiogruppe:** rot an `aria-expanded` (`:143`) – die Beschriftung steht vor der Antwort.
  - **AK 4:** `grep -nE "waitForTimeout|toPass" feldhilfe.spec.ts` ohne Treffer. **AK 5:** `git diff -U0` zeigt nur zwei
    Anfügungen (`planAnlegen`-Ende, neuer Fall am Dateiende); der Assistenten-Teil ist unberührt.
  - Wiederholung `--repeat-each=3` über die Datei: 12/12 grün (die drei alten Fälle je dreimal).
- **2026-11-22**: **abgenommen** (`f79b8fed`, Branch `claude/serene-bhabha-1b1ff6`).
  - Rollengang Entwickler: volle E2E-Suite auf dem Endstand, 185/185 grün (5202/5175, frisch gestartet, 12,5 min). Die
    Datei lief dabei mit.
  - Vater, Sohn und Creator sind Regressionszeugen: dieselbe Suite, kein Produktcode im Diff.
  - Entscheidungen 1–5 bleiben stellvertretend und unbestätigt.
- **2026-11-22** — Nachschau (Nachtlauf B 2026-11-23, Retro Sprint B1, Agent `frontend-reviewer`): Route-Prädikat gegen `api.ts:527-542` nachgerechnet (Erstladen ohne `?`), Wartebedingung `planAnlegen:35` gegen `PlanPositions.tsx:756/794-797` (Radiogruppe erst nach der Antwort) – kann rot werden; kein Befund.
