---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/qualitaet]
aliases: [E2E-Nightly rot, Login bricht ab Test 2, Issue 3]
status: abgenommen
prio: P1
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: CI-Sichtung beim Push am 2026-08-10 (`gh run list`), GitHub-Issue #3
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-55]
nachgeschaut: ""
wartet_auf: ""
---

# B-139 · Der Aufräum-Sweep der E2E löschte das Journal seiner eigenen Datenbank

Beim Push am 2026-08-10 gesehen, nicht gesucht: der E2E-Nachtlauf war **sechs Nächte** rot (05.–10.08.),
letzter grüner Lauf am 04.08. Die Zustellung aus [B-26](B-26-e2e-in-ci.md) hat funktioniert — Issue #3
war offen und wurde jede Nacht kommentiert. Nur hat niemand hineingesehen.

Warum P1: die E2E-Suite ist der einzige automatische Wächter über die *gesamte* Oberfläche, und solange
sie rot ist, sagt ein grüner Lauf nichts mehr. Der Rollengang mehrerer Abnahmen dieser Woche stützt sich
auf genau diese Suite.

## User Story

Als **Entwickler** möchte ich, dass ein roter Nachtlauf eine Aussage über das Produkt ist und nicht über
die Testinfrastruktur — sonst ist der einzige automatische Blick auf die ganze Oberfläche wertlos.

## Was gemessen war

- **Rot in sechs aufeinanderfolgenden Nächten**, die späteren nach ~6,5 min, die vom 05./06.08. nach
  ~4 min (also mit anderer Gestalt).
- **Test 1 grün, danach 30 Fehlschläge** mit gleichmäßig ~11,6 s — eine konstante Dauer über verschiedene
  Specs ist ein Timeout, kein Bündel Einzelfehler.
- **Lokal grün**: dieselbe Suite lief am 10.08. mehrfach mit 31/31 durch.

Zwei Hypothesen sind unterwegs **widerlegt** worden, und beide stehen hier, weil eine widerlegte Vermutung
mehr wert ist als eine offene:

1. *Der Ratenbegrenzer drosselt die Logins* — `frontend/playwright.config.ts:60` setzt
   `RateLimiting__LoginEnabled: "false"`, und CI nutzt dieselbe Konfiguration.
2. *Ab Test 2 scheitert der Login* (so hieß diese Story zuerst) — das Artefakt zeigt das Gegenteil: in
   Test 2 ist der Vater angemeldet („👤 Papa (#1)"), `POST auth/adult` antwortete mit 200.

## Ursache und Behebung (2026-08-10)

**Zwei meiner ersten Aussagen waren falsch und sind hier richtiggestellt:**

- Rot war der Lauf **seit sechs Nächten** (05.–10.08.), nicht seit drei — ich hatte nur die ersten vier
  Einträge von `gh run list` angesehen. Letzter grüner Lauf: 04.08.
- Der Titel behauptete, der **Login** bricht ab Test 2. Das Artefakt zeigt das Gegenteil: in Test 2 ist
  der Vater angemeldet („👤 Papa (#1)"), und `POST auth/adult` antwortete mit 200. Der Login trug.

**Die Ursache**, belegt aus Lauf `31377842041` mit durchgereichter Server-Ausgabe: **156-mal**
`SQLite Error 10: 'disk I/O error'`, verteilt über sieben Tabellen (`Adults`, `Children`,
`TextbookSeries`, `PlanPositions`, `ChildPointsEntries`, `Remarks`, `StudyPlans`) und **sporadisch** —
Migration und Login gingen, dann kippten Aufrufe und erholten sich zwischendurch wieder.

`e2e/global-setup.ts` fegt alles mit dem Präfix `pugling-e2e-` aus dem Temp-Verzeichnis und nahm dabei
genau **zwei exakte Namen** aus: die eigene `.db` und den eigenen Medienordner. SQLite legt aber
Beidateien daneben (`.db-journal`, bei WAL `-wal`/`-shm`), und die tragen dasselbe Präfix. Da der Sweep
laut seinem eigenen Kommentar **nach** dem Start des eigenen Backends läuft, löschte er das **lebende
Journal seiner eigenen Datenbank**.

**Warum es nur in CI weh tat** — und warum es sich sechs Nächte lang der lokalen Suite entzog: Unter
Linux gelingt `unlink` auf eine offene Datei; SQLite verliert sein Journal mitten in der Transaktion und
antwortet fortan sporadisch mit `SQLITE_IOERR`. Unter Windows scheitert dasselbe `rm` mit `EBUSY`, und
`Promise.allSettled` schluckt den Fehler. Dieselbe Suite lief hier 31/31 grün, während der Nachtlauf rot
war. Das ist die reinste Form des Fallstricks „Hooks/Umgebung messen etwas anderes als du".

**Behebung:** Der Ausschluss läuft über das **Präfix** statt über zwei exakte Namen
(`frontend/e2e/global-setup.ts`).

## Akzeptanzkriterien

1. ~~Ursache belegt statt vermutet~~ — Stacktrace aus dem Lauf, nicht aus einer Hypothese.
2. ~~Der Nachtlauf ist wieder grün~~ — Lauf `31378866913`: **31 passed in 1,5 min**, **0** disk-I/O-Fehler
   (vorher 6,5 min mit 156). Das Issue #3 hat der Gegenmechanismus aus B-26 selbst geschlossen.
3. ~~Die Diagnose kostet künftig einen Lauf, keine Nacht~~ — `stdout`/`stderr: "pipe"` am webServer.

## Verlauf

- **2026-08-10** — angelegt aus der CI-Sichtung nach dem Push. Symptome gemessen, Ursache ausdrücklich
  offen gelassen; eine Hypothese am Code widerlegt und als widerlegt notiert.
- **2026-08-10** — **abgenommen.** Commits `08c176a` (Server-Ausgabe durchreichen — ohne sie war die
  Ausnahme in keinem Protokoll) und der Fix am Sweep. Verifikation: Lauf `31378866913` grün mit
  **31/31** und **0** disk-I/O-Fehlern, gegenüber 156 im Lauf davor. Kein Rollengang nötig und keiner
  möglich: der Defekt lebt ausschließlich in der Testinfrastruktur, und die grüne Suite **ist** der
  Beleg. `entgangen_bei: [B-55]` — der Sweep ist in jener Story entstanden und war `abgenommen`.
- **2026-08-10** — **Offen und ausdrücklich nicht behauptet:** der erste rote Lauf war am **05.08.**,
  `global-setup.ts` landete erst am **06.08. um 01:23**. Diese Änderung erklärt den heutigen Fehler,
  nicht zwingend den ersten — die Läufe vom 05. und 06.08. brachen nach ~4 min ab, die späteren nach
  ~6,5 min, also mit anderer Gestalt. Wer das nachprüfen will, hat bis zum **19.08.** Zeit, dann
  verfallen die Artefakte jener Läufe. Ein grüner Nachtlauf am 11.08. wäre das billigere Gegenargument.
