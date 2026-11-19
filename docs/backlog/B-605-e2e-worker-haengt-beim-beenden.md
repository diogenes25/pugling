---
tags: [typ/story, status/geschaetzt, bereich/test, rolle/entwickler]
aliases: [E2E-Worker hängt]
status: geschaetzt
prio: P3
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Sprint 82, Rollengang und volle E2E-Suite (pm-sitzung-2026-11-16-backlog-lauf.md)
grund: ""
ersetzt_durch: []
---

# B-605 · Playwright-Worker hängt beim Beenden, einmal mitten im Lauf

Im Sprint 82 meldete Playwright dreimal „worker-N process did not exit within 300000ms after stop, force-killed it“:
nach dem roten Fall des ersten Rollengang-Laufs (Neustart des Workers) und am Ende der vollen Suite (166 passed,
trotzdem 5 Minuten Nachlauf). Ein voller Lauf blieb zudem bei `bilder.spec.ts:85` (32/166) über zehn Minuten ohne eine
Zeile stehen, weit über dem Test-Timeout von 60 s; nach dem Abbruch lief derselbe Stand in 21 Minuten grün durch.

## User Story

Als Entwickler möchte ich, dass ein hängender E2E-Worker binnen einer Minute auffällt und seine Ursache benannt ist,
damit ein voller Lauf nicht zehn Minuten schweigt und ich nicht raten muss, ob der Fehler im Code oder im Rechner
liegt.

## Ist-Stand am Code

Der Verdacht der Idee („ein offener Handle im Worker hält das Beenden auf“) lässt sich am Code **nicht halten**. Gelesen
in `frontend/node_modules/playwright` (installiert 1.65.1; `frontend/package.json` verlangt `^1.61.1`):

1. **Die Meldung kommt vom Läufer, nicht vom Worker.** `stop()` schickt `__stop__`, wartet auf das Ende des Prozesses
   und erklärt ihn nach der Frist für tot (`node_modules/playwright/lib/runner/index.js:2099-2118`). Die Frist ist
   `PWTEST_CHILD_PROCESS_TIMEOUT` oder 5 Minuten (`:2107`) – die „300000 ms“ der Meldung sind schlicht die Vorgabe.
2. **Die Frist verlängert sich mit jedem Herzschlag.** Solange der Worker `__heartbeat__` schickt, setzt der Läufer die
   Uhr zurück (`runner/index.js:2108-2117`: `lastHeartbeat + timeout`, bei Herzschlag `continue`). Der Worker schickt
   beim Beenden alle 1000 ms einen (`node_modules/playwright/lib/common/index.js:2046` und `:2054`, `unref()` auf
   `:2055`). **Folgerung:** Ein Worker, der wegen eines offenen Handles oder einer hängenden Aufgabe (`gracefullyClose`)
   *lebt*, hielte den Herzschlag und würde **nie** für tot erklärt. Dass die Meldung kam, heißt: **fünf Minuten lang
   kam kein einziger Herzschlag** – die Ereignisschleife des Workers stand, oder der Prozess lief gar nicht.
3. **Ein Handle kann das Beenden nicht blockieren.** Nach `gracefullyClose` ruft der Worker `processExit(0)`
   (`node_modules/playwright/lib/common/index.js:2056-2060`, `import_bootstrap.processExit`); offene Handles stören `process.exit` nicht. Das gilt für Media-Upload,
   Speech-/Audio-Stub und den Vite-Proxy aus der Idee gleichermassen.
4. **Der Hänger ohne Meldung passt dazu:** Der Test-Timeout (60 s, `frontend/playwright.config.ts:44`) läuft als Zeitgeber
   *im Worker*. Steht die Schleife des Workers, feuert er nicht – ein Test, der 10 Minuten „ohne eine Zeile“ steht,
   ist mit einem blockierten Worker vereinbar, mit einer langsamen Seite nicht.
5. **`bilder.spec.ts:85` trägt nichts Auffälliges.** Der Test legt zwei Kontexte an (`:86`, `:194`) und schliesst sie
   erst am Ende (`:229-230`) – ein Fehler in der Mitte lässt sie offen, was aber nur Chromium betrifft, nicht die
   Schleife des Workers. Die Aufgaben im Worker sind kurz (kleine PNGs aus Base64, `:23-28`; eine Suchschleife mit höchstens
   12 Durchläufen `:205`). Eine Stelle, die den Worker synchron beschäftigen könnte (Endlosschleife, riesige
   Verarbeitung), fand sich in der Spec **nicht**; die Serverseite (Bildverarbeitung) läuft im .NET-Prozess und nicht
   im Worker.
6. **Was im Repo zu dem Vorfall steht:** nur der Verweis in `docs/pm-sitzung-2026-11-16-backlog-lauf.md:260`. Eine
   Stichwortsuche in den Protokollen vom 17. und 18.11. (`worker`, `300000`, `force-killed`) fand **keine weitere
   Meldung**. In `frontend/package.json`, `playwright.config.ts` und `.github/workflows/*.yml` steht weder
   `PWTEST_CHILD_PROCESS_TIMEOUT` noch eine andere Vorkehrung: ein Hänger kostet heute immer die vollen 5 Minuten.
7. **Nicht gefunden:** ein Beleg, dass es Chromium war (kein Absturzbericht im Repo), und ein Zeitstempel des Vorfalls
   (kein Protokoll nennt die Uhrzeit). Ob der Rechner in dieser Zeit schlief oder gedrosselt war (Windows kann
   Hintergrundprozesse bremsen), lässt sich daher aus dem Repo nicht entscheiden.

8. **Ergänzung beim Grillen:** `__stop__` ruft `gracefullyCloseAndExit(false)` (`node_modules/playwright/lib/common/index.js:2027`).
   Ein Worker, der lebt, aber im Aufräumen hängt, schickt Herzschläge und wird nie für tot erklärt; die Frist trifft nur den
   stehenden Prozess. Beobachtet wurde dieser Fall nicht.

## Die echte Lücke

Anders als die Idee sagt, ist nicht „welcher Handle?“ die Frage, sondern **„warum stand der Worker-Prozess fünf Minuten
still?“** Mögliche Erklärungen, nach Passung geordnet:

1. Der Rechner oder der Prozess war ausgesetzt (Ruhezustand, Sperrbildschirm, Energiesparen für Hintergrundprozesse) –
   erklärt beide Beobachtungen (kein Herzschlag, kein Test-Timeout) und das „danach 21 Minuten grün“.
2. Der Worker war durch eine synchrone Arbeit blockiert – in der Spec nicht gefunden (Punkt 5), im Playwright-Code des
   Beendens nicht zu sehen.
3. Der Worker war schon tot, ohne dass Windows das Ende meldete (Prozessbaum mit Chromium) – nicht prüfbar.

Und eine zweite, unabhängig davon **gesicherte** Lücke: Die Erkennung dauert immer 5 Minuten, obwohl der Läufer eine
Umgebungsvariable dafür kennt. Das lässt sich ohne Kenntnis der Ursache beheben.

## Offene Punkte

1. ~~**Diagnose vor Reparatur oder umgekehrt?**~~ → Entscheidung 1 (die billige Hälfte zuerst) und Entscheidung 3.
2. ~~**Wo steht die Variable?**~~ → Entscheidung 2.
3. ~~**Wie viel Aufwand für die Ursache?**~~ → Entscheidung 3 (das Verwerfen bleibt beim Nutzer).
4. ~~**Soll `why-is-node-running` oder `PWDEBUG=0` eingesetzt werden?**~~ → Entscheidung 4.
5. ~~**Sollen die Kontexte in `bilder.spec.ts` in `try/finally` schließen?**~~ → Entscheidung 4.
6. ~~**Deckt die Frist auch den Hänger mitten im Lauf?**~~ → Entscheidung 5 (neu beim Grillen: nein).

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-19 (Sonnet), unbestätigt.*

1. **Die billige Hälfte zuerst: die Erkennungsfrist des Beendens wird auf 45 Sekunden gesetzt; die Ursache bleibt eine
   eigene, später folgende Hälfte.** Begründung: Die Frist ist `PWTEST_CHILD_PROCESS_TIMEOUT` mit 5 Minuten als Vorgabe
   (`node_modules/playwright/lib/runner/index.js:2107`) und rechnet ab dem **letzten Herzschlag**
   (`:2108-2117`); ein lebender Worker schickt beim Beenden jede Sekunde einen (`lib/common/index.js:2046`, `:2054-2055`). Eine
   Frist von 45 s tötet also nur einen Prozess, dessen Ereignisschleife stillsteht, und liegt unter der Minute, die die
   User Story verlangt (Herzschlag eine Sekunde plus Frist). 60 s wären die Grenze selbst; 45 s lassen Luft. Die Variable
   ist am Code der installierten Version 1.65.1 gelesen; `package.json` verlangt `^1.61.1`, ein Playwright-Update kann sie
   umbenennen – Akzeptanzkriterium 1 fängt das (die Meldung zeigt wieder 300000 ms). Verworfen: erst die Ursache suchen (die
   Erkennung dauert in jedem Fall 5 Minuten, auch wenn die Ursache offen bleibt) und `PWTEST_FORCE_EXIT_TIMEOUT` (das ist eine
   Frist **im Worker** nach einem Trennen, `lib/common/index.js:2011`, `:2047`, und trifft den stehenden Worker nicht).
   Kosten: Ein Rechner, der mitten im Beenden länger als 45 s ruht, verliert die Nacharbeit dieses Workers (Spuren eines
   roten Falls); die Testergebnisse sind da schon gemeldet. Ausgeschlossener Fall: ein **lebender** Worker mit hängendem
   Aufräumen. `__stop__` ruft `gracefullyCloseAndExit(false)` (`lib/common/index.js:2027`), solange die Schleife lebt, kommen
   Herzschläge, und der Läufer wartet für immer – diese Frist erwischt ihn nie. Beobachtet wurde er nicht; gegen die User
   Story („ein hängender Worker fällt binnen einer Minute auf“) ist er der Teil, den sie nicht deckt.
2. **Die Variable steht oben in `frontend/playwright.config.ts` als `process.env.PWTEST_CHILD_PROCESS_TIMEOUT ??= "45000"`.**
   Begründung: Der Läufer liest sie erst im Beenden (`runner/index.js:2107`), die Konfiguration ist dann geladen; so gilt sie
   für jeden Aufruf, auch ein blankes `playwright test`, und unter Windows ohne `VAR=x cmd`. `??=` lässt eine von außen gesetzte
   Frist gewinnen (die CI-Workflow-Datei `e2e.yml` setzt keine, und braucht keine Änderung). Dass der Läufer die Konfiguration
   im selben Prozess auswertet, in dem später `stop()` läuft, ist am Code nicht belegt – das klärt die Probe aus
   Akzeptanzkriterium 1. Verworfen: ein Skript in `package.json` mit der Variablen (ein Aufruf ohne das Skript hätte wieder
   fünf Minuten) und ein Eintrag in `.github/workflows/e2e.yml` (nur CI, nicht lokal; dort war der Hänger nicht). Kosten: eine
   Zeile mit einem Kommentar, der Grund und Herkunft nennt. Ausgeschlossener Fall: ein Aufruf eines anderen Läufers (etwa
   `npx playwright` aus einem anderen Ordner); gegen die User Story (derselbe Lauf, den der Entwickler startet) trägt es,
   weil jeder Aufruf aus `frontend/` die Konfiguration lädt.
3. **Die Ursache bekommt ein Zeitfenster, keine Dauerinstrumentierung; „nicht reproduzierbar, verworfen“ bleibt eine
   Entscheidung des Nutzers.** Beim nächsten Hänger werden (a) Uhrzeit und Rechnerzustand notiert, (b) `powercfg /lastwake`
   bzw. das Ereignisprotokoll nach Ruhezustand befragt, (c) der Worker mit `node --cpu-prof` oder einem Herzschlag-Protokoll
   gefahren. Belegt am Code: Die Meldung bedeutet fünf Minuten **ohne einen einzigen Herzschlag** (`runner/index.js:2108-2117`),
   ein offener Handle blockiert das Beenden nicht (`processExit(0)` nach `gracefullyClose`,
   `lib/common/index.js:2056-2060`); die Ursachenliste der „echten Lücke“ gilt. Es fand sich im Repo kein weiterer Fall
   (Stichwortsuche in den Protokollen vom 17. und 18.11. ohne Treffer, ein Zeitstempel des Vorfalls steht nirgends).
   Vorschlag, nicht Beschluss: Taucht er in fünf vollen Läufen nach der Reparatur nicht wieder auf, schlägt die Story selbst
   die Verwerfung „nicht reproduzierbar“ vor; die Zahl fünf ist eine Setzung (ein voller Lauf dauert gemessen 21 Minuten),
   nicht aus Daten gerechnet. Verworfen: die Story jetzt verwerfen (ein Einzelfall ist keine Messreihe, und die Frist aus
   Entscheidung 1 ist unabhängig davon wert) und Dauer-Logging im Worker (kostet in jedem Lauf für einen seltenen Fall).
   Kosten: Die Ursache bleibt vorerst offen. Ausgeschlossener Fall: ein Hänger, der nie wiederkommt – er kostet nichts mehr
   als diese Story schon kostet.
4. **Weder `why-is-node-running` noch `PWDEBUG=0` noch ein `try/finally` um die Kontexte in `bilder.spec.ts`.** Begründung:
   `why-is-node-running` benennt offene Handles, und die sind nach Entscheidung 3 nicht die Ursache; `PWDEBUG` setzt hier
   niemand (in `playwright.config.ts` und `package.json` nicht gefunden), eine Null davor ändert nichts; die Kontexte
   schließen erst am Ende (`bilder.spec.ts:229-230`), ein Fehler in der Mitte lässt sie offen – das betrifft Chromium, nicht die
   Schleife des Workers und nicht diese Story. Verworfen: alle drei. Kosten: Ein sauberes Aufräumen in `bilder.spec.ts`
   bleibt liegen. Ausgeschlossener Fall: ein Rot in der Mitte von `bilder.spec.ts`, das einen Kontext offen lässt; ein
   anderer Fehler, den dieses Rot selbst meldet.
5. **Zurückgestellt: der Hänger mitten im Lauf („über zehn Minuten ohne eine Zeile“).** Die Frist aus Entscheidung 1 greift
   erst im Beenden; mitten im Lauf gibt es im Läufer keine Wache für einen stehenden Worker – der Test-Timeout (60 s) läuft als
   Zeitgeber **im** Worker und feuert nicht, wenn seine Schleife steht (Ist-Stand 4). Einzig `globalTimeout` beendet so einen
   Lauf von außen, müsste aber über der längsten Suite liegen (21 Minuten gemessen) und meldete den Hänger damit nach über
   21 Minuten. Zurückgestellt: erst die Ursache (Entscheidung 3), dann entscheidet sich, ob ein Wächter außerhalb des Läufers
   (ein Skript, das auf Zeilen der Ausgabe achtet) lohnt. Gegen die User Story gerechnet ist das die Lücke: „ein voller Lauf
   schweigt zehn Minuten“ bleibt, bis die Ursache bekannt ist.

## Akzeptanzkriterien

Je Kriterium: die Änderung, die es rot macht.

1. Mit der Zeile in `frontend/playwright.config.ts` endet ein blockierter Worker nach etwa 45 s mit „did not exit within
   45000ms after stop, force-killed it“. Probe **außerhalb des Repos**: eine Wegwerf-Konfiguration importiert die echte
   Konfiguration (damit deren Zeile läuft), nimmt `globalSetup`/`webServer` heraus und hat eine Wegwerf-Spec, deren worker-skopiertes Fixture im Abbau
   mit `while (Date.now() < …)` länger als die Frist blockiert (nicht im Testkörper, siehe Schätzung, Risiko 1); Start mit der lokalen Binary
   (`./node_modules/.bin/playwright`, nicht `npx`). Rot, wenn die Zeile entfernt wird: nach 90 s steht keine Meldung da
   (sie käme erst bei 300000 ms).
2. Deutung belegt: dieselbe Wegwerf-Probe, aber mit einem offenen Handle statt einer Blockierung (`setInterval` ohne
   `unref` in der Spec): der Worker beendet sich ohne die Meldung. Rot, wenn der Handle dieselbe Meldung erzeugt – dann ist
   die Deutung des Ist-Stands falsch und die Ursachenliste neu zu ordnen. Das Ergebnis beider Proben steht im `## Verlauf`.
3. Die Ursache des Hängers vom 2026-11-16 ist **entweder** benannt (Zeitstempel, Rechnerzustand, Profil) **oder** die Story
   endet mit dem Befund „nicht reproduzierbar“ samt Zähler („in N vollen Läufen nicht wieder aufgetreten“) und der
   Verwerfung durch den Nutzer. **Absichtserklärung**, kein Test: Rot wäre eine Ursache ohne Beleg aus Kriterium 2 oder einem Profil;
   Durchsicht bei der Abnahme.
4. Der Diff berührt genau `frontend/playwright.config.ts`, keine Spec und keinen Workflow (`git diff --stat`). Rot, wenn eine
   zweite Datei im Diff steht.
5. Die volle Suite läuft danach wie vorher durch; gezählt wird „N passed“ in der Ausgabe, nicht der Exit-Code. Rot, wenn die
   Konfiguration nicht mehr lädt oder die Zahl der bestandenen Fälle sinkt.

## Schätzung

**Größe: S** — der Bau ist eine Zeile mit Kommentar (XS), die Beweisführung aber ein Wegwerf-Aufbau außerhalb des Repos mit zwei
Proben; zusammen etwas über dem XS-Anker (zwei Sätze plus ein prüfender E2E).

- **`migration: nein`** — nachgesehen: Konfiguration des Testläufers, kein Schema.
- **`vertragsbruch: nein`** — nachgesehen: nur `frontend/playwright.config.ts`; `Pugling.Contracts` unberührt.
- **`wo: frontend`** — Playwright-Konfiguration im Frontend; Reviewer `frontend-reviewer`.

### Risiken

1. **Der Probe-Aufbau aus Akzeptanzkriterium 1 trägt so nicht: Ein Block im Testkörper erzeugt die Meldung nicht.**
   Die Frist gilt erst ab `stop()` (`node_modules/playwright/lib/runner/index.js:2099-2118`); blockiert der Worker mitten im
   Test, ist `stop()` noch nicht gerufen, und nach dem Ende der Schleife läuft der Worker normal aus. Die Blockade muss
   **nach** dem `__stop__` liegen: im Abbau eines **worker-skopierten Fixtures** (`gracefullyClose` ruft
   `teardownScope("worker", …)`, `lib/worker/workerProcessEntry.js:1406-1420`, und der Herzschlag-Zeitgeber läuft schon,
   `lib/common/index.js:2046-2055`). Eine synchrone Schleife dort legt auch den Herzschlag lahm – genau die Lage, die die
   Meldung beschreibt. Die Schleife muss die Frist deutlich überdauern (etwa 120 s bei 45 s Frist).
2. **Die Variable gilt nur, wenn die Konfiguration im selben Prozess geladen wird, der später `stop()` ruft.** Der Läufer lädt
   sie im Haupt-Prozess (die Worker sind Kinder), `stop()` ist dort; dass es in 1.65.1 genau so läuft, belegt erst die Probe.
   Eine Aktualisierung von Playwright (`^1.61.1`) kann den Namen ändern; Akzeptanzkriterium 1 zeigt es.
3. **Die Wegwerf-Konfiguration muss die echte importieren, ohne ihren Start mitzubringen.** `webServer`, `globalSetup` und
   `globalTeardown` wären sonst aktiv (`dotnet run`, echte Ports) – die Probe verbietet das. Die Konfiguration wirft außerdem,
   solange eine Mutanten-Probe läuft (`.claude/.mutant-probe.lock`, `playwright.config.ts:34-39`).
4. **Die 45 s sind eine Setzung**: Der Herzschlag kommt jede Sekunde, die Frist rechnet ab dem letzten; ein Rechner, der im
   Beenden länger ruht, verliert die Nacharbeit dieses Workers (Entscheidung 1, Kosten).
5. **Der Hänger mitten im Lauf und der lebende Worker mit hängendem Aufräumen bleiben ungedeckt** (Entscheidungen 1 und 5); die
   Story endet nach dem Bau ohne Wächter dafür.

### Angriffsplan

1. `process.env.PWTEST_CHILD_PROCESS_TIMEOUT ??= "45000";` oben in `frontend/playwright.config.ts` (nach den Importen), mit
   Kommentar zu Grund und Herkunft (Version 1.65.1, `runner/index.js:2107`).
2. **Messschritt M1 (Akzeptanzkriterium 1), außerhalb des Repos** (Ablage im Scratchpad): Wegwerf-Konfiguration, die die echte
   importiert und `webServer`, `globalSetup`, `globalTeardown` entfernt; Wegwerf-Spec mit einem worker-skopierten Fixture,
   dessen Abbau ca. 120 s blockiert. Lauf mit Zeile: Meldung „did not exit within 45000ms“ bei etwa 45 s. Lauf ohne Zeile
   (zweite Wegwerf-Konfiguration): bis 90 s keine Meldung.
3. **Messschritt M2 (Akzeptanzkriterium 2):** dieselbe Probe, aber im Abbau nur ein `setInterval` ohne `unref`; der Worker
   beendet sich ohne die Meldung. Beide Ergebnisse in den `## Verlauf`.
4. Volle Suite (Akzeptanzkriterium 5) und Diff-Prüfung (`git diff --stat`, genau eine Datei).

### Testweg

- **Kein Repo-Test:** der Nutzen der Zeile ist die Erkennungsfrist; sie lässt sich nur über einen blockierten Worker messen, und
  ein solcher Fall gehört nicht in die Suite (er kostete jedes Mal 45 s und 120 s Blockade). Darum die Wegwerf-Probe aus dem
  Angriffsplan; ihr Aufbau steht im `## Verlauf`, damit sie wiederholbar ist.
- **Rote Probe:** Zeile entfernen → die Meldung kommt nicht bei 45 s (Akzeptanzkriterium 1); offener Handle statt Blockade →
  keine Meldung (Akzeptanzkriterium 2). Beides deterministisch, kein Flackern.
- **Messbefehl** (aus `frontend/`, lokale Binary, nicht `npx`; Konfiguration per `--config` auf die Wegwerf-Datei):
  `./node_modules/.bin/playwright test --config <Scratchpad>/probe.config.ts`. Die volle Suite danach auf Ausweichports
  (`PUGLING_E2E_API_PORT=5201 PUGLING_E2E_WEB_PORT=5174 PUGLING_E2E_CONFIGURATION=Release`); gezählt wird „N passed“, nicht der
  Exit-Code.
- Vor der Abnahme: `frontend-reviewer`; ein Rollengang entfällt (Testwerkzeug, keine Oberfläche), das steht dann im Verlauf.

## Verlauf

- **2026-11-16** — angelegt beim Rollengang von Sprint 82.
- **2026-11-19** — ausformuliert (Vorbereitungslauf, Sonnet): der Handle-Verdacht der Idee widerspricht dem
  Playwright-Code (Herzschlag während des Beendens, `processExit(0)`); die Meldung bedeutet „fünf Minuten kein
  Herzschlag“. Nichts gemessen oder gestartet; Vorschlag an den Nutzer: nach dem Bau mit Verwerfen „nicht
  reproduzierbar“ rechnen, falls der Fall nicht wiederkehrt.
- **2026-11-19** — gegrillt (Vorbereitungslauf, Sonnet, stellvertretend, unbestätigt): Entscheidungen 1 bis 5. Billige Hälfte zuerst: `PWTEST_CHILD_PROCESS_TIMEOUT ??= "45000"` in `playwright.config.ts`, geprüft über eine Wegwerf-Konfiguration, die die echte importiert. Die Frist deckt nur den Hänger im Beenden, nicht den mitten im Lauf (zurückgestellt) und nicht einen lebenden Worker mit hängendem Aufräumen. Die Ursache bleibt ein Zeitfenster; „nicht reproduzierbar, verworfen“ ist ein Vorschlag, die Entscheidung bleibt beim Nutzer. Akzeptanzkriterien final, je mit roter Änderung.
- **2026-11-19** — geschaetzt (Vorbereitungslauf, Sonnet): S, frontend, keine Migration, kein Vertragsbruch (nachgesehen: nur `playwright.config.ts`). Beim Schätzen am Läufer-Code korrigiert: der Probe-Aufbau aus Akzeptanzkriterium 1 muss die Blockade im Abbau eines worker-skopierten Fixtures setzen, nicht im Testkörper (die Frist gilt erst ab `stop()`); sonst bestünde die Probe ohne die Zeile ebenso. Nichts gemessen oder gestartet.
