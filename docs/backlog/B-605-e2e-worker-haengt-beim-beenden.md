---
tags: [typ/story, status/ausformuliert, bereich/test, rolle/entwickler]
aliases: [E2E-Worker hängt]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
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

1. **Diagnose vor Reparatur oder umgekehrt?** Empfehlung: **beides getrennt, die billige Hälfte zuerst:** die
   Erkennungsfrist verkürzen (`PWTEST_CHILD_PROCESS_TIMEOUT`, etwa 60000), dann beim nächsten
   Auftreten die Ursache eingrenzen. Eine Frist unter dem Herzschlag-Takt ist unkritisch: ein lebender Worker meldet
   sich jede Sekunde.
2. **Wo steht die Variable?** Empfehlung: oben in `frontend/playwright.config.ts` als
   `process.env.PWTEST_CHILD_PROCESS_TIMEOUT ??= "60000"`. Der Läufer liest sie erst beim Beenden
   (`runner/index.js:2107`), die Konfiguration ist dann längst geladen; so gilt sie für jeden Aufruf, auch ein
   blankes `playwright test`, und ist Windows-fest (kein `VAR=x cmd`). Ob der Läufer die Konfiguration im selben
   Prozess auswertet, in dem `stop()` später läuft, ist nicht geprüft – das klärt die Probe aus Kriterium 2.
3. **Wie viel Aufwand für die Ursache?** Empfehlung: **ein Zeitfenster**: beim nächsten Hänger (a) Uhrzeit und
   Rechnerzustand notieren, (b) `powercfg /lastwake` bzw. das Ereignisprotokoll nach Ruhezustand befragen, (c) den
   Worker mit `node --cpu-prof` oder einem Herzschlag-Logger fahren. Taucht der Fall in fünf vollen Läufen nicht mehr
   auf, wird die Story mit Grund „nicht reproduzierbar“ **verworfen** – das entscheidet der Nutzer, nicht der Lauf.
4. **Soll `why-is-node-running` (Idee) oder `PWDEBUG=0` (Idee) eingesetzt werden?** Empfehlung: **nein.** `why-is-node-running`
   benennt offene Handles, und die sind nach Punkt 3 nicht die Ursache; `PWDEBUG` setzt hier niemand (in `playwright.config.ts`
   und `package.json` nicht gefunden), eine Null davor ändert nichts.
5. **Sollen die Kontexte in `bilder.spec.ts` in `try/finally` schliessen?** Empfehlung: **nein, nicht hier.** Das
   ist ein Aufräum-Gedanke ohne Bezug zur Ursache; ein Rot in der Mitte ist ohnehin ein anderer Fehler. Wenn überhaupt,
   gehört er in eine eigene Aufräum-Story.

## Entwurf der Akzeptanzkriterien

1. Ein Hänger des Workers endet innerhalb von etwa 60 Sekunden mit der bekannten Meldung, nicht nach 5 Minuten.
   *Rot, wenn* die Variable aus der Konfiguration entfernt wird: die Probe aus Kriterium 2 braucht dann 5 Minuten statt 1.
2. Eine **Wegwerf-Probe** außerhalb von `frontend/e2e` (nicht ins Repo) belegt die Deutung aus Punkt 2 des Ist-Stands:
   ein Test blockiert den Worker synchron länger als der Test-Timeout (`while (Date.now() < …)`), der Lauf trägt
   `PWTEST_CHILD_PROCESS_TIMEOUT=10000`. Erwartet: dieselbe Meldung („did not exit within 10000ms after stop,
   force-killed it“) – damit ist belegt, dass die Meldung einen stehenden Worker meint und nicht einen offenen Handle.
   *Rot, wenn* stattdessen ein Handle ohne Blockierung dieselbe Meldung erzeugt (dann ist Punkt 2 falsch und die
   Ursachenliste neu zu ordnen).
3. Die Ursache des Hängers vom 2026-11-16 ist **entweder** benannt (mit Beleg: Zeitstempel, Rechnerzustand, Profil)
   **oder** die Story endet mit dem Befund „nicht reproduzierbar“ und einem Zähler („in N vollen Läufen nicht wieder
   aufgetreten“). *Rot, wenn* eine Ursache behauptet wird, ohne dass Kriterium 2 oder ein Profil sie trägt – als
   Durchsicht bei der Abnahme, kein Tor.
4. `bilder.spec.ts` und die übrige Suite bleiben unverändert grün (166/168 je nach Stand); die Änderung berührt keine Spec.
   *Rot, wenn* der Diff eine Spec berührt – Durchsicht.

## Verlauf

- **2026-11-16** — angelegt beim Rollengang von Sprint 82.
- **2026-11-19** — ausformuliert (Vorbereitungslauf, Sonnet): der Handle-Verdacht der Idee widerspricht dem
  Playwright-Code (Herzschlag während des Beendens, `processExit(0)`); die Meldung bedeutet „fünf Minuten kein
  Herzschlag“. Nichts gemessen oder gestartet; Vorschlag an den Nutzer: nach dem Bau mit Verwerfen „nicht
  reproduzierbar“ rechnen, falls der Fall nicht wiederkehrt.
