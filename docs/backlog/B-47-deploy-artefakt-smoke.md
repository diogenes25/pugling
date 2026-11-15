---
tags: [typ/story, status/abgenommen, bereich/qualitaet, bereich/tests]
aliases: [Deploy-Artefakt-Smoke, Artefakt-Smoke]
status: abgenommen
prio: P3
art: Aufräumen
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: B-41
unverifiziert: false
---

# B-47 · Startet das veröffentlichte Artefakt überhaupt?

Abgespalten von [B-41](B-41-produktions-startup-smoke.md) (Entscheidung 1): Dessen Testklasse deckt die
Produktions-**Konfiguration** in-process ab. Ungeprüft bleibt das Produktions-**Artefakt** – Frontend bauen →
neben die veröffentlichte API legen → `dotnet publish` → starten → liefert Kestrel die PWA aus und antwortet die API
daneben? Genau diese Kette ist 24 Tage lang unbemerkt gescheitert (Peer-Konflikt `vite-plugin-pwa` ↔ `vite@8`), weil
kein Tor sie fuhr. Seit 2026-11-11 fährt sie `scripts/heimnetz.ps1` für die echten Kinder – ohne Probelauf.

## User Story

Als **Vater, der Pugling im Heimnetz betreibt**, möchte ich, dass ein Tor das **veröffentlichte Artefakt** – die
`dotnet publish`-Ausgabe samt gebautem Frontend, gestartet wie im Heimnetz – tatsächlich hochfährt und per HTTP
prüft, damit ein kaputter Stand auf `main` auffällt, bevor ich ihn für meine Söhne starte, statt erst, wenn das Handy
eine leere Seite zeigt.

## Ist-Stand am Code

Nachgemessen am 2026-11-15:

- **Der gefahrene Betrieb ist `scripts/heimnetz.ps1`**, nicht Azure: `npm run build` (`:107`), `dotnet publish … -o
  $appDir` (`:112`), `frontend/dist` als Kopie nach `$appDir/wwwroot` (`:115-116`), danach nur eine **Datei**-Prüfung
  auf `index.html` und `Pugling.Api.dll` (`:118-119`), dann Start im **Vordergrund** gegen die echte `pugling.db` auf
  `:5200` mit `--contentRoot backend/Pugling.Api --webroot $webRoot` (`:167-174`). Kein HTTP-Probelauf.
- **Azure ist stillgelegt** (`docs/deployment-azure.md:7-10`): `deploy-azure.yml` hat nur `workflow_dispatch` (`:35`),
  `workflow_run` auskommentiert (`:28`); dort `dotnet publish` (`:75`) direkt vor `azure/webapps-deploy@v3` (`:78`).
- **`ci.yml`** (Trigger `push: main`, `pull_request`, `workflow_dispatch`, `:18-22`) hat drei Jobs ohne `needs:` –
  `ci` (`:48`, Build/Test/Format, mit ~140 s Format der längste), `frontend` (`:173`, `npm ci` + `npm run build` +
  Vitest), `markdownlint` (`:206`). Keiner publiziert oder startet einen Prozess.
- **`Program.cs`**: `AddHealthChecks().AddDbContextCheck<PuglingDbContext>()` (`:233`), Fail-fast ohne `Jwt:Key`
  außerhalb von Development (`:274-275`), `MigrateAsync` beim Start (`:658`, legt eine leere DB vollständig an),
  `UseDefaultFiles`/`UseStaticFiles` (`:590-591`, liefert `/`), `MapHealthChecks("/health")` (`:701`),
  `MapFallback("/api/{**rest}")` (`:709`), `MapFallbackToFile("index.html")` (`:710`, liefert Tiefenlinks wie
  `/sohn`).
- `frontend/index.html:11` trägt `<div id="root"></div>` – ein stabiles, seltenes Fragment.
- `ProductionStartupTests` (B-41) bleiben in-process (`WebApplicationFactory`), ohne `publish`, ohne gebautes
  `wwwroot`; ihr AK 7 verweist den Out-of-process-Teil ausdrücklich hierher.

## Die echte Lücke

Kein Tor startet das veröffentlichte Artefakt. Der einzige Weg, der es heute startet, ist der echte Heimnetz-Start –
also der Moment, in dem die Söhne es brauchen. Die alte Bauform (ein Schritt in `deploy-azure.yml`) bewachte einen
Weg, den niemand mehr geht; darum lag die Story mit `wartet_auf` still. Der Wartegrund ist entfallen: das zu
bewachende Artefakt ist heute das von `heimnetz.ps1`.

## Offene Punkte

1. ~~Ist B-47 durch B-41 abgedeckt?~~ → nein, eigenständig (siehe „Die echte Lücke“).
2. ~~Wann darf gebaut werden?~~ → Entscheidung 6 (ersetzt Entscheidung 1).
3. ~~Wo sitzt der Smoke?~~ → Entscheidungen 7 und 8 (ersetzen Entscheidung 2).
4. ~~xUnit-Test oder CI-Schritt?~~ → Entscheidung 3 (gilt weiter).
5. ~~Welche Routen?~~ → Entscheidung 9 (erweitert Entscheidung 4).
6. ~~`Jwt:Key` und Wegwerf-DB?~~ → Entscheidung 5 (gilt weiter, präzisiert in 10).
7. ~~Prüft `heimnetz.ps1` selbst nach dem Start?~~ → Entscheidung 11.
8. ~~Woran sieht man, dass der Smoke selbst etwas fängt?~~ → Entscheidung 12.

## Entscheidungen

Die Entscheidungen 1–5 vom 2026-08-03 waren autonom getroffen (Nutzerauftrag 2026-08-04), keine **Entscheidung des
Nutzers**; sie werden darum hier neu geschnitten statt verteidigt.

1. ~~**Gebaut wird erst, wenn der `workflow_run`-Block in `deploy-azure.yml` wieder scharf ist.**~~ **Abgelöst durch
   Entscheidung 6** (2026-11-15): das Gate setzte voraus, dass Azure der Betrieb ist; seit 2026-11-11 ist es
   `heimnetz.ps1`.
2. ~~**Schritt im bestehenden `deploy-azure.yml`, kein eigener Workflow.**~~ **Abgelöst durch Entscheidungen 7 und 8**
   (2026-11-15): ein Schritt dort läuft nie automatisch und prüft nicht den Start, den `heimnetz.ps1` macht.
3. **CI-Skript (Shell), kein xUnit-Test in `Pugling.sln`.** Gilt weiter. Begründung: Vite-Build + `publish` +
   Kestrel-Start liegen im Minutenbereich und sprängen dem Test-Tor-Budget (~63 s) davon. Kosten: keine
   Testklasse mit Assertion-Historie – ausgeglichen durch Entscheidung 12.
4. **Beide prüfen: `/health` UND die Seite.** Gilt weiter, erweitert durch Entscheidung 9.
5. **Eigene, kurzlebige SQLite-Datei plus generierter `Jwt:Key`, keine Secrets.** Gilt weiter, präzisiert durch
   Entscheidung 10.
6. **Gebaut wird jetzt; `wartet_auf` entfällt.** Stakeholder-Entscheidung im Auftrag des Nutzers. Begründung: der
   Artefakt-Weg wird seit 2026-11-11 für echte Kinder gefahren; ein Fehler dort trifft die Söhne, nicht einen
   stillgelegten Azure-Dienst. Kosten: ein CI-Job mehr, der gepflegt werden will.
7. **Ein wiederverwendbares Skript `scripts/artefakt-smoke.ps1` (PowerShell 7, plattformneutral).**
   Stakeholder-Entscheidung im Auftrag des Nutzers. Es baut das Frontend (`npm ci` + `npm run build`), publiziert die
   API in einen Wegwerf-Ordner, kopiert `frontend/dist` nach `<ordner>/wwwroot` und startet
   `dotnet <ordner>/Pugling.Api.dll --webroot <ordner>/wwwroot` – **dieselbe Startform wie `heimnetz.ps1:174`**, nur
   mit dem Veröffentlichungs-Ordner als Inhaltswurzel (nichts im Repo wird beschrieben). Schalter: `-Port` (Vorgabe
   ein unüblicher, z. B. `5299`), `-SkipBuild`, `-EmptyWebRoot` (Gegenprobe, Entscheidung 12). Begründung: PowerShell,
   weil der Betrieb PowerShell ist und `pwsh` auf `ubuntu-latest` vorinstalliert ist – **ein** Skript lokal und in CI
   statt eines Bash-Zwillings. Windows-only-Cmdlets aus `heimnetz.ps1` (`Get-NetTCPConnection`, `Get-NetIPConfiguration`)
   sind darin verboten. Kosten: lokal Windows, CI Linux – die Pfadbehandlung muss beides tragen (siehe Risiken).
8. **Aufgerufen von einem vierten Job `artefakt` in `ci.yml`, ohne `needs:`, auf allen bestehenden Triggern.**
   Stakeholder-Entscheidung im Auftrag des Nutzers. Begründung: geschätzt ~3 min (setup-dotnet, `npm ci` mit Cache,
   Vite-Build, `publish`, zwei Starts) und damit kürzer als der parallele `ci`-Job (Test + ~140 s Format) – die
   Wandzeit eines Laufs wächst nicht. Auf PRs, weil dort ein kaputter Build am billigsten zurückgeht. Kosten: Runner-
   Minuten und ein doppeltes `npm ci` (der `frontend`-Job baut ebenfalls). **Fallback**: verlängert der Job die
   Wandzeit messbar, wird er auf `push: main` + `workflow_dispatch` beschränkt – ohne neue Entscheidung, nur mit
   einer Verlauf-Zeile.
9. **Drei Prüfungen**: `GET /health` → `200`; `GET /` → `200` und enthält `<div id="root">`; `GET /sohn` → `200` und
   enthält `<div id="root">`. Stakeholder-Entscheidung im Auftrag des Nutzers. Begründung: `/` geht über
   `UseDefaultFiles`, `/sohn` über `MapFallbackToFile` (`Program.cs:590/710`) – zwei verschiedene Wege, und `/sohn`
   ist die Adresse, die das Handy des Sohns öffnet. Kosten: eine Anfrage mehr.
10. **Laufzeitumgebung des Probestarts**: `ASPNETCORE_ENVIRONMENT=Production`, `ASPNETCORE_URLS=http://127.0.0.1:<Port>`
    (nur Loopback), `ConnectionStrings__Default` auf eine Datei im Wegwerf-Ordner, `Jwt__Key` je Lauf zufällig
    erzeugt, `Seed__Enabled=false` – wie `heimnetz.ps1:167-171`. Stakeholder-Entscheidung im Auftrag des Nutzers.
    Begründung: genau die Konfiguration des echten Betriebs, nur mit fremder DB; die Migration läuft dabei einmal
    über eine leere DB (`Program.cs:658`). Kosten: keine Prüfung der Migration gegen den **Bestand** der echten DB –
    das ist eine andere Frage und gehört nicht in diesen Schnitt.
11. **`heimnetz.ps1` bekommt keinen Probelauf gegen die laufende Instanz.** Stakeholder-Entscheidung im Auftrag des
    Nutzers. Begründung: dort läuft die **echte** `pugling.db` auf `:5200` im Vordergrund; ein Probelauf müsste den
    Start in den Hintergrund verlagern und gegen echte Kinderdaten fragen – auch ein lesendes `/health` öffnet die
    echte DB. Der Schutz kommt vorher: das Tor auf `main` (Entscheidung 8), und von Hand ist `scripts/artefakt-smoke.ps1`
    vor einem Heimnetz-Neustart aufrufbar (ein Satz in `docs/heimnetz.md`). Kosten: lokal ungepushte Stände sind nur
    geschützt, wenn jemand das Skript von Hand aufruft.
12. **Die Gegenprobe ist dauerhaft: ein zweiter Lauf mit `-SkipBuild -EmptyWebRoot` muss rot enden.**
    Stakeholder-Entscheidung im Auftrag des Nutzers. Der CI-Job ruft ihn nach dem grünen Lauf auf und schlägt fehl,
    wenn der Exit-Code `0` ist. Begründung: ein Smoke ohne gesehene rote Probe ist eine Behauptung (Muster B-41 AK 6);
    eine einmalige Handprobe verrottet, ein Job-Schritt nicht. Kosten: ~15 s je Lauf.

## Akzeptanzkriterien

Die früheren AKs (Schritt in `deploy-azure.yml` vor `azure/webapps-deploy@v3`, unveränderter `if:`/`ref:`-Block)
sind mit den Entscheidungen 1 und 2 abgelöst; ihr Kern (Retry statt Sleep, Abbruch bei Nicht-Antwort, Prozess immer
beenden) steht unten.

1. `scripts/artefakt-smoke.ps1` endet mit Exit `0`, wenn `/health`, `/` und `/sohn` wie in Entscheidung 9 antworten.
   **Rot**, wenn das Frontend nicht gebaut oder nicht kopiert wird (dann fehlt `<div id="root">`).
2. Bereitschaft per Retry-Schleife gegen `/health` mit Frist (z. B. 60 s), kein fester Sleep. Beendet sich der Prozess
   vorher (etwa Fail-fast auf `Jwt:Key`), meldet das Skript dessen Exit-Code und die letzten Zeilen seiner Ausgabe
   statt nur „Timeout“. **Rot**, wenn `Jwt__Key` weggelassen wird.
3. Prozess **und** Wegwerf-Ordner (DB, Publish, `wwwroot`) werden in jedem Fall entfernt (`try/finally`), auch bei
   rotem Ausgang. Prüfbar: nach einem roten Lauf ist der Port frei und der Ordner weg.
4. `-EmptyWebRoot` lässt `wwwroot` leer; der Lauf endet **rot** an der Seitenprüfung, nicht am `/health`
   (Meldung nennt `/`). Das ist die Gegenprobe; sie belegt, dass AK 1 etwas trägt.
5. Job `artefakt` in `ci.yml` ruft AK 1 und danach AK 4 auf und ist nur grün, wenn AK 1 grün **und** AK 4 rot ist.
   **Rot**, wenn das Skript die Seitenprüfung verliert (dann wäre AK 4 grün).
6. Das Skript nutzt keine Windows-only-Cmdlets und keinen festen Pfad unter `/tmp`; es läuft unter `pwsh` auf
   Windows und `ubuntu-latest`.
7. `docs/heimnetz.md` nennt den Aufruf vor einem Neustart in einem Satz; `heimnetz.ps1` bleibt unverändert.

## Schätzung

**Größe S** (Anker B-01; nächstliegend B-41) – kein Produktivcode, keine Abhängigkeit, kein Vertrag. Ein Skript, ein
Job, ein Satz Doku. `wo: backend` (CI/Skript), `migration: nein` (die Wegwerf-DB entsteht und verschwindet im Lauf),
`vertragsbruch: nein`.

### Risiken

| Risiko | Warum es hier greift | Gegenmittel |
| --- | --- | --- |
| Lokal Windows, CI Linux | Pfade, `dotnet`-Aufruf, Prozess-Beenden unterscheiden sich; eine Messung nur auf Windows ist für Linux blind | `Join-Path`/`[IO.Path]`, `Start-Process -PassThru` + `Stop-Process`; einmal lokal **und** einmal in CI grün und rot sehen |
| Port belegt | ein Dev-Server oder Heimnetz läuft lokal | unüblicher Vorgabe-Port, nur `127.0.0.1`; belegter Port → sofort rot mit klarer Meldung, nie auf `:5200` |
| Fail-fast frisst den Prozess | falscher `Jwt__Key`/Connection-String | Exit-Code des Prozesses in der Retry-Schleife prüfen (AK 2) |
| Verwaister Prozess bei Job-Abbruch | ein Timeout überspringt `failure()`-Aufräumen | `try/finally` im Skript, Runner wird ohnehin verworfen; lokal ist das `finally` der Schutz |
| Laufzeit größer als geschätzt | `publish` + Vite auf dem Runner | Fallback aus Entscheidung 8 |
| Prüfung zu strikt/zu lasch | Titel ändert sich, leeres `wwwroot` liefert 404 | auf `<div id="root">` prüfen, nicht auf den Titel; Gegenprobe AK 4 |

### Angriffsplan

1. `scripts/artefakt-smoke.ps1` schreiben (Bauen, Publish in Wegwerf-Ordner, Kopie nach `wwwroot`, Start mit
   Entscheidung-10-Umgebung, Retry gegen `/health`, drei Prüfungen, `finally`-Aufräumen, `-EmptyWebRoot`).
2. Lokal unter Windows: einmal grün, einmal mit `-EmptyWebRoot` rot, einmal ohne `Jwt__Key` rot (AK 2).
3. Job `artefakt` in `ci.yml` (setup-dotnet, setup-node mit derselben `NODE_VERSION`, `pwsh`-Schritte für AK 1 und
   AK 4 mit invertierter Erwartung).
4. Satz in `docs/heimnetz.md`.
5. CI-Lauf per `workflow_dispatch` auf dem Branch: grün, Laufzeit des Jobs gegen den `ci`-Job notieren
   (Fallback-Entscheidung 8).

### Testweg

Kein Eintrag in `Pugling.sln`. Der Testweg ist das Skript selbst: lokal grün/rot (Angriffsplan 2) und der CI-Job mit
eingebauter Gegenprobe (AK 5). Abgenommen ist es erst mit einem CI-Lauf, dessen Log die rote Gegenprobe zeigt –
nicht am Exit-Code des Jobs allein.

## Verlauf

- **2026-07-31** — angelegt bei der Teilung von B-41 (Grillen der vier Test-Stories).
- **2026-08-01** — Ist-Stand korrigiert: die Annahme „es gibt keinen `/health`-Endpunkt" war **falsch**;
  gefunden beim Schätzen von B-41. Der Endpunkt ist da, nur ungetestet.
- **2026-08-01** — bleibt `idee`, aber nicht mehr unbestimmt: Eintrittsbedingung und Bauform sind entschieden
  (Paket-Grillen). Damit kostet die Story in der nächsten Sichtung keine Aufmerksamkeit mehr – sie wartet auf
  ein Ereignis, nicht auf eine Entscheidung.
- **2026-08-03** — ausformuliert: gegen den Code geprüft, dass B-47 keine Überschneidung mit dem
  abgenommenen B-41 hat (dessen AK 7 schließt den Out-of-process-Teil ausdrücklich aus) – `deploy-azure.yml`
  ruft `dotnet publish` und geht ohne Zwischenschritt zu `azure/webapps-deploy@v3`, `ci.yml` hat gar keinen
  Publish-Schritt.
- **2026-08-03** — gegrillt: autonom getroffen, Nutzerauftrag 2026-08-04 – fünf offene Punkte in
  Entscheidungen überführt (Reaktivierungs-Gate, Schritt statt eigenem Workflow, CI-Skript statt xUnit-Test,
  `/health` plus `index.html`, Wegwerf-DB/`Jwt:Key`-Handhabung).
- **2026-08-03** — geschätzt: autonom getroffen, Nutzerauftrag 2026-08-04 – Größe **S**, `backend`, keine
  Migration, kein Vertragsbruch; fünf Risiken benannt, Angriffsplan in fünf Schritten, Testweg über
  `workflow_dispatch` plus Gegenprobe.
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): gilt inhaltlich, Zeilen verrutscht (`deploy-azure.yml:74-82`, `Program.cs:226/681/686`). Die User Story nennt als Gate „die Azure-Reaktivierung aus B-33“ – **B-33 ist verworfen**; das Gate ist der Neubau des Deploys (`docs/deployment-azure.md:7-10`). Entscheidung 1 verbietet den Bau davor, darum `wartet_auf`. Wird das Deployment neu gebaut, ist die Bauform (Schritt in genau dieser YAML) eventuell hinfällig.
- **2026-10-23** — In der Grill-Runde G-0 der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): B-07 ist verworfen (Neubau statt Reaktivierung). Das Gate dieser Story ist damit allein der Neubau; beim Neubau prüfen, ob der Smoke-Schritt in die neue Pipeline gehört statt in `deploy-azure.yml`.
- **2026-11-14** — Abgleich Backlog-Lauf: veraltet, Kern gilt, `wartet_auf` unverändert. Zeilen nachgemessen (Kasten unter „Ist-Stand am Code"); neu ist `scripts/heimnetz.ps1` als gefahrener Betrieb mit eigenem `dotnet publish` – beim Bau prüfen, ob der Smoke dorthin gehört statt in `deploy-azure.yml`.
- **2026-11-15** — Backlog-Lauf 2026-11-15: neu geschnitten auf den gefahrenen Betrieb `heimnetz.ps1`. Entscheidungen 1 und 2 (Azure-gebunden, autonom) abgelöst und durchgestrichen; sieben Stakeholder-Entscheidungen im Auftrag des Nutzers (6–12): Bau jetzt, Skript `scripts/artefakt-smoke.ps1`, CI-Job `artefakt` ohne `needs:`, `/health` + `/` + `/sohn`, Heimnetz-Startform mit Wegwerf-DB, kein Probelauf gegen die echte Instanz, dauerhafte Gegenprobe. `wartet_auf` entfernt. Bleibt `geschaetzt`: **S**, `backend`, keine Migration, kein Vertragsbruch.
- **2026-11-15** — `geschaetzt` → **`abgenommen`** in Sprint 80 des Backlog-Laufs 2026-11-15, Code-Commit `73c73a6a`.
  `scripts/artefakt-smoke.ps1` und Job `artefakt` in `ci.yml` nach den Entscheidungen 6–12; Satz in `docs/heimnetz.md`.
  - AK 1 → lokal (Windows) grün: `/health` 200, `/` und `/sohn` mit dem gebauten Frontend.
  - AK 4 → lokal `-SkipBuild -EmptyWebRoot` rot an `/` (404), nicht an `/health`.
  - AK 3 → nach dem roten Lauf kein Wegwerf-Ordner mehr unter dem Temp-Pfad, Port 5299 frei (gemessen).
  - AK 2 → Retry gegen `/health` mit Prozess-Exit-Prüfung im Skript; ein eigener Rotlauf ohne `Jwt__Key` ist **nicht**
    gemessen (das Skript setzt den Schlüssel immer).
  - AK 5/6 → CI-Lauf auf Linux mit dem Push nach `main` (Ergebnis im Protokoll, Abschluss).
