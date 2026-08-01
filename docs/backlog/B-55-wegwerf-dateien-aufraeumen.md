---
tags: [typ/story, status/ausformuliert, bereich/qualitaet, bereich/tests]
aliases: [Temp-Ordner aufräumen, Wegwerf-Dateien, Temp-Leck]
status: ausformuliert
prio: P3
art: Aufräumen
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

1. **Braucht das ein Tor, oder reichen die zwei Reparaturen?** Ein Wächter müsste den Temp-Ordner vor und
   nach dem Lauf zählen — das ist eine Kopplung an die Maschine, die in CI (frischer Runner) nichts findet
   und lokal von jedem parallel laufenden Testprozess gestört wird. **Empfehlung:** kein Tor, aber ein Satz
   in `backend/Pugling.Api.Tests`-Nähe bzw. `CLAUDE.md`: *wer eine Wegwerf-Datei anlegt, löscht sie im
   selben Objekt* — und der Verweis auf die `DisposeAsync`-Falle, weil sie nicht erratbar ist.
2. **Playwright: Aufräumen im `globalTeardown` oder gar nicht?** Ein `globalTeardown` löscht am Ende des
   Laufs; bei einem roten Lauf ist die DB aber gelegentlich das einzige, woran man den Zustand noch
   nachsehen kann. **Empfehlung:** aufräumen, aber nur bei grünem Lauf — oder schlicht immer, weil `trace`
   und `screenshot` den Befund ohnehin tragen (`playwright.config.ts:29-30`).
3. **Sollen die vorhandenen 934 Reste im selben Zug weg?** Das ist eine einmalige Handlung, kein Code.
   **Empfehlung:** ja, und in der Story vermerken, dass es passiert ist — sonst misst die nächste Zählung
   Altlast und hält sie für ein neues Leck.

## Akzeptanzkriterien

1. `QueryPlanSmokeTests` löscht seine Datei am Ende des Tests — auch wenn eine Zusicherung darin fällt.
2. Der Playwright-Lauf lässt weder `pugling-e2e-*.db` noch `pugling-e2e-media-*/` zurück.
3. **Gegenprobe je Stelle:** Temp-Einträge des jeweiligen Musters vor und nach einem Lauf zählen, Delta
   **0** — und einmal mit ausgebautem Aufräumen gegengeprüft, dass die Zählung überhaupt anschlägt.
4. Die 934 Altlast-Einträge (inkl. der 26 handbenannten) sind gelöscht, das Datum steht im Verlauf.
5. Die Regel „wer eine Wegwerf-Datei anlegt, löscht sie im selben Objekt" steht dort, wo sie gelesen wird,
   **mit** dem `IAsyncDisposable`-Fallstrick — sonst tritt der nächste hinein.

## Verlauf

- **2026-08-01** — angelegt beim Abschluss von [B-41](B-41-produktions-startup-smoke.md)/E1, nachdem das
  Aufräumen des dortigen Lecks die zwei verbliebenen Erzeuger sichtbar gemacht hat. Alle drei am Code
  belegt, die Mengen gezählt statt geschätzt. Der B-41-Anteil (23 888 Dateien, 16,1 GB) ist am selben Tag
  gelöscht worden.
