---
tags: [typ/story, status/abgenommen, bereich/qualitaet, bereich/tests]
aliases: [Temp-Ordner aufräumen, Wegwerf-Dateien, Temp-Leck]
status: abgenommen
prio: P3
art: Aufräumen
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: docs/backlog/B-41-produktions-startup-smoke.md
---

# B-55 · Die Tests räumen ihre Wegwerf-Dateien nicht weg

Drei Stellen im Repo legen je Lauf eine SQLite-Datei in den Temp-Ordner und löschen sie nie. Eine davon ist
mit [B-41](B-41-produktions-startup-smoke.md) geschlossen — sie hatte allein **20 880 Waisen mit 16,1 GB**
angesammelt, unbemerkt seit dem 4. Juli. Die anderen zwei laufen weiter.

## User Story

Als **Entwickler**, der die Suite mehrmals täglich fährt, möchte ich, dass ein Testlauf den Rechner so
zurücklässt, wie er ihn vorgefunden hat — damit nicht nach einem Monat zweistellige Gigabyte in `%TEMP%`
liegen, von denen niemand weiß, dass es sie gibt.

## Ist-Stand am Code

Gemessen am **2026-08-01** in `%LOCALAPPDATA%\Temp`: **24 822 Einträge mit 16,9 GB** unter dem Präfix
`pugling`. Nach dem Löschen des B-41-Anteils sind **934 Einträge mit 0,8 GB** übrig — das ist der Rest, um
den es hier geht.

| Erzeuger | Muster | Reste am 2026-08-01 | Zustand |
| --- | --- | --- | --- |
| `PuglingWebAppFactoryBase` ([PuglingWebAppFactory.cs](../../backend/Pugling.Api.Tests/PuglingWebAppFactory.cs)) | `pugling_test_<guid>.db` | 23 865 Dateien, 16,5 GB | **erledigt** (B-41): `DisposeAsync`-Override + `SqliteConnection.ClearPool`; Leck-Delta seither 0 |
| [QueryPlanSmokeTests.cs:28](../../backend/Pugling.Api.Tests/QueryPlanSmokeTests.cs) | `pugling-queryplan-<guid>.db` | 264 Dateien, 201 MB | **offen** — legt an, kein `Delete`, kein `finally`, kein `IDisposable` |
| [playwright.config.ts:11](../../frontend/playwright.config.ts) und `:14` | `pugling-e2e-<ts>.db`, `pugling-e2e-media-<ts>/` | 525 Dateien (604 MB) + 119 Ordner | **offen** — die Konfiguration kennt **kein** `globalTeardown` (nachgesehen: der Bezeichner kommt in `frontend/` nicht vor) |
| — kein Erzeuger im Repo — | `pugling-mig2.db`, `pugling-migrationstest.db`, `pugling-tagcheck.db`, `pugling-vor-umbenennung.db`, `pugling-doc-screenshots.db`, elf `.log`/`.err`/`.txt` | 26 Dateien, 9,6 MB | **Altlast aus früheren Sitzungen** — von Hand angelegt, nicht von Code. Vom Nutzer am 2026-08-01 zum Löschen freigegeben. |

Zwei Belege dazu, damit die Tabelle nicht auf Vermutung steht: `QueryPlanSmokeTests` enthält weder
`Delete` noch `Dispose` noch `finally` (gegrept), und `tmpdir` steht in `frontend/` ausschließlich in den
beiden genannten Zeilen der Playwright-Konfiguration.

## Die echte Lücke

Nicht der belegte Speicherplatz — 0,8 GB tun niemandem weh. Die Lücke ist, dass **kein Tor das bemerkt**.
Das B-41-Leck lief 28 Tage und wurde nicht durch einen roten Test gefunden, sondern weil ein Reviewer
beiläufig Dateien gezählt hat. Ein Aufräumen, das niemand prüft, ist eine Gewohnheit, keine Zusicherung —
und die zwei verbliebenen Stellen zeigen, dass die Gewohnheit nicht trägt.

Dazu kommt eine Fehlerklasse, die B-41 teuer bezahlt hat und die hier wieder droht: `Dispose(bool)` lief
für xUnit-Klassen-Fixtures **nie**, weil xUnit über `IAsyncDisposable` entsorgt und
`WebApplicationFactory.DisposeAsync()` nicht durch `Dispose(bool)` führt. Aufräum-Code, der aussieht, als
liefe er, ist schlimmer als keiner.

## Offene Punkte

1. ~~Braucht das ein Tor, oder reichen die zwei Reparaturen?~~ → siehe Entscheidung 1.
2. ~~Playwright: Aufräumen im `globalTeardown` oder gar nicht?~~ → siehe Entscheidung 2.
3. ~~Sollen die vorhandenen 934 Reste im selben Zug weg?~~ → siehe Entscheidung 3.

## Entscheidungen

1. **Kein Tor — Regel als Doku, nicht als Wächter.** Ein Zähl-Wächter koppelt an die Maschine: ein
   frischer CI-Runner hat nie etwas anzusammeln, und lokal stört ihn jeder parallel laufende Testprozess
   (eigener `pugling_test_*`-Bestand). Statt eines Tors bekommt das Testprojekt sein erstes
   `backend/Pugling.Api.Tests/CLAUDE.md` mit der Regel *wer eine Wegwerf-Datei anlegt, löscht sie im
   selben Objekt* plus dem Verweis auf die `DisposeAsync`-Falle (bereits als Doc-Kommentar in
   [PuglingWebAppFactory.cs:86-97](../../backend/Pugling.Api.Tests/PuglingWebAppFactory.cs) belegt: xUnit
   entsorgt Klassen-Fixtures über `IAsyncDisposable`, `Dispose(bool)` läuft dafür **nie**). **Kosten:** eine
   neue, dauerhaft zu pflegende Datei — gerechtfertigt, weil sie beim Arbeiten in diesem Testprojekt
   automatisch lädt (wie die vier bestehenden verschachtelten `CLAUDE.md`), ein Tor das nicht könnte.
2. **Playwright-Teardown läuft unconditional, nicht nur bei Grün.** `trace: "retain-on-failure"` und
   `screenshot: "only-on-failure"` (`playwright.config.ts:29-30`) tragen den Befund eines roten Laufs
   bereits; ein bedingtes Aufräumen bräuchte zusätzlich, den Laufstatus im `globalTeardown` überhaupt zu
   kennen (Playwright reicht ihn dort nicht direkt durch), nur um denselben Fall doppelt abzusichern.
   **Kosten:** bei einem roten Lauf ist die rohe SQLite-Datei nicht mehr per Hand inspizierbar — trace
   deckt das in der Praxis ab; wer die DB doch braucht, kommentiert den Teardown-Aufruf für den einen Lauf
   aus.
3. **Die 934 Altlast-Einträge werden gelöscht — als Teil der Bau-Etappe (`in-arbeit`), nicht in dieser
   Schätz-Sitzung.** Der Nutzer hat die Freigabe am 2026-08-01 bereits erteilt (Ist-Stand-Tabelle). Diese
   Sitzung ändert ausschließlich diese Story-Datei; die Löschung ist eine echte Handlung mit
   Seiteneffekt und gehört in den Bau-Schritt, wo `## Verlauf` das Datum trägt. **Kosten:** keine — reine
   Reihenfolge-Klarstellung, kein Aufschub der Sache selbst.

## Akzeptanzkriterien

1. `QueryPlanSmokeTests` löscht seine Datei am Ende des Tests — auch wenn eine Zusicherung darin fällt
   (`try`/`finally`, plus `SqliteConnection.ClearPool` vor dem `File.Delete`, sonst hält der Pool das
   Handle offen wie beim B-41-Fund).
2. Der Playwright-Lauf lässt weder `pugling-e2e-*.db` noch `pugling-e2e-media-*/` zurück — über ein
   `globalTeardown`, das unconditional (auch bei Rot) läuft.
3. **Gegenprobe je Stelle:** Temp-Einträge des jeweiligen Musters vor und nach einem Lauf zählen, Delta
   **0** — und einmal mit ausgebautem Aufräumen gegengeprüft, dass die Zählung überhaupt anschlägt.
4. Die 934 Altlast-Einträge (inkl. der 26 handbenannten) sind gelöscht, das Datum steht im Verlauf.
5. Die Regel „wer eine Wegwerf-Datei anlegt, löscht sie im selben Objekt" steht in
   `backend/Pugling.Api.Tests/CLAUDE.md`, **mit** dem `IAsyncDisposable`-Fallstrick — sonst tritt der
   nächste hinein.

## Schätzung

- **Größe:** S — drei kleine, voneinander unabhängige Reparaturen (ein `try`/`finally` in einem
  Backend-Test, ein `globalTeardown`-Modul im Frontend-E2E, eine neue kurze `CLAUDE.md`) plus eine
  einmalige Löschaktion ohne Code. Kein Einzelteil erreicht die Substanz des M-Ankers (vokabel-basierter
  Batch-Pfad im `MediaSelector`, B-03).
- **Wo:** `beides` — Backend (`Pugling.Api.Tests`) **und** Frontend-E2E-Infrastruktur
  (`playwright.config.ts` + neues `frontend/e2e/global-teardown.ts`). Backend zuerst.
- **Migration:** nein — keine Schemaänderung.
- **Vertragsbruch:** nein — `Pugling.Contracts` ist nicht betroffen.

### Risiken

- **`globalTeardown` kennt `dbFile`/`mediaDir` nicht von selbst.** Playwright lädt Config und Teardown als
  getrennte Module; die Pfade müssen aus `playwright.config.ts` in ein gemeinsames, einmal ausgewertetes
  Modul wandern (z. B. `frontend/e2e/temp-paths.ts`), sonst berechnet der Teardown mit einem neuen
  `Date.now()` andere Dateinamen und löscht nichts.
- **Derselbe Pool-Fallstrick wie bei B-41** kann sich in `QueryPlanSmokeTests` wiederholen, wenn
  `SqliteConnection.ClearPool` vor dem `File.Delete` vergessen wird — `File.Delete` schlägt dann
  still im `catch` fehl, nicht sichtbar im Testergebnis.
- **Manuelles Löschen der 934 Altlast-Dateien** darf keinen laufenden Testprozess treffen — vor dem
  Löschen prüfen, dass kein `dotnet test`/`npm run test:e2e` gerade läuft.

### Angriffsplan (Backend zuerst)

1. **Backend:** `QueryPlanSmokeTests.cs` — `dbPath`-Handling in `try`/`finally` fassen, `con` schließen,
   `SqliteConnection.ClearPool` vor `File.Delete`, kurzer Doc-Kommentar mit Verweis auf den B-41-Fund.
2. **Backend:** `backend/Pugling.Api.Tests/CLAUDE.md` neu anlegen mit der Regel aus Entscheidung 1 und dem
   `IAsyncDisposable`-Fallstrick-Verweis.
3. **Frontend:** `frontend/e2e/global-teardown.ts` neu, exportiert aus einem gemeinsamen Modul mit
   `playwright.config.ts` berechnete `dbFile`/`mediaDir`-Pfade; `globalTeardown` in der Config eintragen.
4. **Einmalig, danach:** die 934 Altlast-Einträge (Ist-Stand-Tabelle) löschen, Datum in `## Verlauf`.

### Testweg

- Backend: `dotnet test --filter FullyQualifiedName~QueryPlanSmokeTests` zweimal laufen lassen, dabei
  `%TEMP%` auf `pugling-queryplan-*.db` zählen (vorher/nachher, Delta 0); einmal mit auskommentiertem
  `finally` gegenprüfen, dass die Zählung dann tatsächlich einen Rest zeigt (AC3).
  `Pugling.Api.Tests` insgesamt bleibt Teil des bestehenden Test-Gates (`dotnet test Pugling.sln -c
  Release`).
- Frontend: `npm run test:e2e` einmal laufen lassen, danach `%TEMP%` auf `pugling-e2e-*.db` und
  `pugling-e2e-media-*` prüfen (leer). Kein neuer Vitest-Test nötig — der Beleg ist die Zählung, kein
  Unit-Test einer Config-Datei.
- Kein automatisches Tor (Entscheidung 1) — die Gegenprobe ist eine einmalige, in dieser Story
  dokumentierte Handlung, keine dauerhafte CI-Prüfung.

## Verlauf

- **2026-08-01** — angelegt beim Abschluss von [B-41](B-41-produktions-startup-smoke.md)/E1, nachdem das
  Aufräumen des dortigen Lecks die zwei verbliebenen Erzeuger sichtbar gemacht hat. Alle drei am Code
  belegt, die Mengen gezählt statt geschätzt. Der B-41-Anteil (23 888 Dateien, 16,1 GB) ist am selben Tag
  gelöscht worden.
- **2026-08-04** — gegrillt: alle drei offenen Punkte in Entscheidungen überführt — kein Tor (Regel als
  neues `backend/Pugling.Api.Tests/CLAUDE.md` statt Wächter), Playwright-Teardown unconditional statt nur
  bei Grün, Löschung der 934 Altlast-Dateien verschoben in den Bau-Schritt (autonom getroffen,
  Nutzerauftrag). Beide `Datei:Zeile`-Belege im Ist-Stand (`QueryPlanSmokeTests.cs:28`,
  `playwright.config.ts:11`/`:14`) gegen den heutigen Code geprüft — unverändert, keine Korrektur nötig.
- **2026-08-04** — geschätzt: `groesse: S`, `wo: beides`, `migration: nein`, `vertragsbruch: nein`; Risiken,
  Angriffsplan (Backend zuerst) und Testweg ergänzt (autonom getroffen, Nutzerauftrag).
- **2026-08-06** — gebaut (Nachtlauf 2, Sprint 2). **Backend:** `QueryPlanSmokeTests.cs` in `try`/`finally`
  gefasst, `SqliteConnection.ClearPool` vor `File.Delete`. **Gegenprobe:** vor dem Fix 424→425 Dateien
  (`pugling-queryplan-*.db` in `%TEMP%`) nach einem Lauf (Leck bestätigt), nach dem Fix zweimal
  hintereinander Delta **0**. Neues `backend/Pugling.Api.Tests/CLAUDE.md` mit der Aufräum-Regel und dem
  `IAsyncDisposable`-Fallstrick. **Frontend:** `e2e/temp-paths.ts` (geteiltes Modul), `e2e/global-setup.ts`
  (räumt fremde `pugling-e2e-*`-Leichen vor jedem Lauf, schließt die eigenen Pfade explizit aus) und
  `e2e/global-teardown.ts` (bestmögliches Löschen der eigenen Dateien, `Promise.allSettled`, nie werfend)
  in `playwright.config.ts` verdrahtet. **Abweichung vom ursprünglichen Plan, gemessen statt vermutet:**
  ein reines `globalTeardown` genügt nicht – das Backend dieses Laufs läuft zum Teardown-Zeitpunkt noch
  (Playwright stoppt `webServer` erst danach), sein SQLite-Verbindungspool hält `dbFile` deterministisch
  offen; `EBUSY` blieb über zwei Minuten linearen Backoffs bestehen. Der `global-setup`-Sweep im
  **nächsten** Lauf ist die tatsächliche Garantie, nicht der Teardown-Versuch – gemessen: 951→3 Einträge
  nach einem Lauf, stabil bei 3 nach einem zweiten. Einmalig 447 Alt-Dateien gelöscht (425 `queryplan`,
  22 benannte Reste). `frontend-reviewer` fand einen echten, aber unterhalb der Blocker-Schwelle
  liegenden Fund: die erste Kommentar-Fassung schrieb die Ursache fälschlich einem Virenscanner zu,
  tatsächlich ist es Playwrights Task-Reihenfolge (Setup läuft **nach** dem eigenen `webServer`-Start,
  Teardown **vor** dessen Stop) – Kommentare korrigiert, Sweep härtet zusätzlich die eigenen Pfade aus.
  `dotnet test Pugling.sln -c Release` → **746/746 grün**, `npm run build` clean, `npm run test:e2e` →
  **27/28 grün** (der eine Ausfall ist der vorbestehende, dokumentierte B-109-Flake in
  `full-flow.spec.ts`, unverändert).
