---
tags: [typ/story, status/geschaetzt, bereich/qualitaet, bereich/tests]
aliases: [Deploy-Artefakt-Smoke]
status: geschaetzt
prio: P3
art: Aufräumen
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: B-41
---

# B-47 · Startet das veröffentlichte Artefakt überhaupt?

Abgespalten von [B-41](B-41-produktions-startup-smoke.md) (Entscheidung 1): Dessen Testklasse deckt die
Produktions-**Konfiguration** in-process ab. Ungeprüft bleibt das Produktions-**Artefakt** – der Weg, den
`deploy-azure.yml` geht: Frontend bauen → nach `wwwroot` kopieren → `dotnet publish` → starten → liefert
Kestrel die PWA über `MapFallbackToFile("index.html")` aus und antwortet die API daneben? Genau diese Kette
ist 24 Tage lang unbemerkt gescheitert (Peer-Konflikt `vite-plugin-pwa` ↔ `vite@8`), weil kein Tor sie fuhr.

## User Story

Als **Betreiber**, der einen Deploy tatsächlich fahren will (sobald die Azure-Reaktivierung aus B-33
nachgeholt ist), möchte ich, dass ein CI-Schritt das **veröffentlichte Artefakt** – die `dotnet publish`-
Ausgabe samt dem hineinkopierten Frontend-Build – tatsächlich hochfährt und per HTTP prüft, damit ein
stiller Ausfall wie der 24-tägige Peer-Konflikt beim nächsten Deploy sofort auffällt statt erneut unbemerkt
zu bleiben.

## Ist-Stand am Code

- `ProductionWebAppFactory`/`ProductionStartupTests` (B-41, abgenommen) starten die Produktions-
  **Konfiguration** ausschließlich in-process über `WebApplicationFactory<Program>`
  (`backend/Pugling.Api.Tests/ProductionWebAppFactory.cs`) – kein `dotnet publish`, kein eigener
  Kestrel-Prozess, kein gebautes `wwwroot`. B-41s eigenes AK 7 hält das ausdrücklich fest: „Der
  Out-of-process-Teil ist nicht enthalten, sondern liegt als B-47 vor."
- `.github/workflows/ci.yml` hat drei Jobs (`ci`, `frontend`, `markdownlint`) – keiner ruft `dotnet publish`
  auf oder startet einen Prozess, gegen den `curl` liefe (gegen die Datei nachgelesen, nicht vermutet).
- `.github/workflows/deploy-azure.yml:80-81` ruft `dotnet publish ${{ env.API_PROJECT }} -c Release -o
  publish` und geht **direkt weiter** zu `azure/webapps-deploy@v3` (Zeilen 84-88) – dazwischen liegt kein
  Start, kein Health-Check, kein `curl`. Der Workflow ist seit 2026-07-30 stillgelegt: der `workflow_run`-
  Block ist auskommentiert (Zeilen 27-31), es bleibt nur `workflow_dispatch`.
- `/health` existiert: `Program.cs:221` registriert `AddHealthChecks().AddDbContextCheck<PuglingDbContext>()`,
  `Program.cs:531` mappt `app.MapHealthChecks("/health")` – anonym, laut Kommentar direkt darüber „for load
  balancers/monitoring". Kein Test und kein CI-Schritt ruft ihn auf; der `EndpointCoverageGuard`
  (`backend/Pugling.Api.Tests/EndpointCoverageGuard.cs`) zählt ausschließlich Controller-Actions – ein
  `MapHealthChecks`-Endpunkt ist kein Controller und steht in keinem Inventar.
- `Program.cs:536` (`app.MapFallbackToFile("index.html")`) liefert die PWA aus `wwwroot` aus. Lokal und in
  jedem bestehenden Testlauf ist `wwwroot` leer – der Zweig läuft also nie mit echtem Frontend-Inhalt durch.

## Die echte Lücke

Keine Überschneidung mit B-41: dessen Testklasse prüft *Konfigurationszweige* (Jwt-Fail-fast, Seed-Schalter,
Anmerkungs-Sichtbarkeit) in einem In-Process-Host, dessen `wwwroot` nie das gebaute Frontend enthält. B-47
prüft etwas kategorisch anderes – **ob der tatsächlich veröffentlichte Prozess überhaupt hochkommt und
antwortet**: `dotnet publish` mit dem echten `frontend/dist`-Inhalt in `wwwroot`, als eigener Kestrel-Prozess
gestartet, gegen `/health` **und** `index.html` per HTTP abgefragt. Genau diese Kette scheiterte 24 Tage lang
am Peer-Konflikt, ohne dass ein Tor es bemerkte – kein In-Process-Test hätte das gefangen, weil `npm ci` dort
gar nicht läuft.

Die Lücke ist real und eigenständig, aber **aktuell folgenlos**: Ohne reaktivierten `workflow_run`-Trigger
(das fehlende Azure-Secret ist als [B-33](B-33-azure-publish-profile.md) bewusst verworfen) läuft
`deploy-azure.yml` nur noch per Handauslösung. Ein Smoke-Schritt dort wachte über einen Weg, den zurzeit
niemand automatisch geht.

## Offene Punkte

1. ~~Ist B-47 durch B-41 abgedeckt?~~ → siehe „Die echte Lücke" oben – nein, eigenständig.
2. ~~Wann darf gebaut werden – sofort oder erst nach Reaktivierung?~~ → siehe Entscheidung 1.
3. ~~Eigener Workflow oder Schritt in einem bestehenden?~~ → siehe Entscheidung 2.
4. ~~xUnit-Test oder CI-Schritt?~~ → siehe Entscheidung 3.
5. ~~Welche Route(n) genau prüfen – reicht `/health` oder auch `index.html`?~~ → siehe Entscheidung 4.
6. ~~Woher `Jwt:Key` und eine harmlose Wegwerf-DB für den Probestart nehmen?~~ → siehe Entscheidung 5.

## Entscheidungen

1. **Gebaut wird erst, wenn der `workflow_run`-Block in `deploy-azure.yml` wieder scharf ist** (heute
   auskommentiert, Zeilen 27-31). Begründung: Solange nur `workflow_dispatch` triggert, bewachte ein
   Smoke-Schritt einen Weg, den niemand automatisch geht – die Story bleibt `geschaetzt`, ihr `in-arbeit`
   wartet auf dieses Ereignis. Kosten: Die Lücke bleibt bis dahin offen, trifft aber nicht zu, solange kein
   automatischer Deploy läuft. Übernommen aus dem Grillen des Testabdeckungs-Pakets (2026-08-01), hier nur
   bestätigt statt neu verhandelt.
2. **Schritt im bestehenden `deploy-azure.yml`, kein eigener Workflow.** Begründung: Der Smoke-Schritt muss
   exakt das Artefakt prüfen, das im selben Job zwei Schritte vorher mit `dotnet publish` entstand
   (`deploy-azure.yml:80-81`) – ein zweiter Workflow müsste Build und Publish redundant wiederholen und liefe
   Gefahr, in einer **anderen** Umgebung zu laufen als die, die deployt wird (dieselbe Falle, die `ci.yml`
   bei `NODE_VERSION` schon benennt: „ein Tor, das eine andere Umgebung prüft als die, in der gebaut und
   deployt wird, bewacht nichts"). Kosten: Der Schritt läuft nur, wenn `deploy-azure.yml` überhaupt anläuft
   (Entscheidung 1) – kein eigenständiges Tor für PRs.
3. **CI-Schritt (Shell), kein xUnit-Test in `Pugling.sln`.** Begründung: bereits in der Eintrittsbedingung
   vom 2026-08-01 festgelegt – `dotnet publish` + Vite-Build + Kestrel-Start liegen im Minutenbereich; als
   Test im Haupttor liefe das bei **jeder** `.cs`-Änderung mit und spränge dem 63-Sekunden-Stop-Hook-Budget
   davon. Kosten: kein `EndpointCoverageGuard`-Schutz, keine Gegenprobe-Disziplin wie bei B-41 – der Schritt
   ist ein CI-Skript, keine versionierte Testklasse mit Assertion-Historie.
4. **Beide prüfen: `/health` UND `index.html`.** Begründung: `/health` beweist nur, dass Kestrel läuft und
   die DB erreichbar ist – nicht, dass `wwwroot` das gebaute Frontend enthält. Genau der Peer-Konflikt, der
   diese Story ausgelöst hat, hätte einen laufenden Server mit leerem oder veraltetem `wwwroot` erzeugt (der
   Vite-Build bricht, `cp -r frontend/dist/.` kopiert dann nichts Neues) – ein reiner `/health`-Check hätte
   das **nicht** gefangen. Der zweite Aufruf gegen `/` prüft, dass `MapFallbackToFile("index.html")` etwas
   anderes als 404 liefert und die Antwort nach echtem HTML aussieht. Kosten: ein zweiter Curl-Aufruf plus
   eine Inhaltsprüfung statt eines reinen Status-Codes.
5. **Eigene, kurzlebige SQLite-Datei plus ein im Workflow-Schritt gesetzter `Jwt:Key`, kein Zugriff auf
   Azure-Secrets.** Begründung: Der Probestart läuft standardmäßig als „Production" (kein
   `ASPNETCORE_ENVIRONMENT` nötig, das ist bereits der Default), darum greift der Fail-fast auf `Jwt:Key`
   (`Program.cs:262-263`, siehe auch B-41) ohne einen gesetzten Wert – ein beliebiger, im Workflow generierter
   Wert genügt, da nichts über den Prozess hinaus gilt. Die Connection-String zeigt auf eine Wegwerf-Datei im
   Runner-Workspace (Muster aus dem Skill `smoke-test`: relativer Pfad, kein `/tmp`), `Seed:Enabled` bleibt
   auf seinem Produktions-Vorgabewert `false` – geprüft wird der Start, nicht der Lernstand. Kosten: ein
   weiterer Satz Umgebungsvariablen im Workflow-Schritt, gepflegt neben den bestehenden aus
   `deploy-azure.yml`.

## Akzeptanzkriterien

1. Ein neuer Schritt in `deploy-azure.yml` startet nach „API publish" (vor `azure/webapps-deploy@v3`) den
   veröffentlichten Prozess im Hintergrund, mit einer Wegwerf-SQLite-Datei und einem im Schritt gesetzten
   `Jwt:Key`.
2. Der Schritt wartet mit Zeitlimit auf Bereitschaft (Retry-Schleife gegen `/health`, kein Sleep ins Blaue)
   und schlägt **rot** fehl, wenn der Prozess innerhalb der Frist nicht antwortet.
3. `GET /health` liefert `200`.
4. `GET /` liefert **kein** `404` und enthält erkennbar den gebauten Frontend-Inhalt (nicht bloß „irgendein
   200").
5. Der Prozess wird **in jedem Fall** beendet (Erfolg wie Fehlschlag), bevor der Job weiterläuft oder endet –
   kein verwaister Hintergrundprozess, keine liegen gebliebene Wegwerf-Datei.
6. Schlägt der Smoke fehl, bricht der Job **vor** `azure/webapps-deploy@v3` ab – ein kaputtes Artefakt
   erreicht Azure nicht.
7. Der bestehende `if:`-Bedingungsblock und die `ref:`-Fallstrick-Behandlung aus `deploy-azure.yml`
   (Fallstrick 2, [docs/deployment-azure.md](../deployment-azure.md)) bleiben unverändert.

## Schätzung

**Größe S** (Anker: „`childId` aus dem Test-Pfad ziehen", B-01; nächstliegender Vorgang ist B-41s
Produktions-Startup-Smoke) – kein Produktivcode, keine neue Abhängigkeit, keine Vertragsänderung. Der Umfang
ist ein Shell-Block in einer bestehenden YAML-Datei, aber mit mehreren Teilen, die einzeln leicht zu
vergessen sind (Wegwerf-DB, `Jwt:Key`, Retry-Schleife, Cleanup, zwei Curl-Prüfungen) – vergleichbar im
Zuschnitt mit B-41, nur als CI-Skript statt als C#-Testklasse.

`migration: nein` – kein Entity, kein `DbContext` ändert sich; die Wegwerf-Datei entsteht und verschwindet
im selben Job-Lauf. `vertragsbruch: nein` – `Pugling.Contracts` bleibt unberührt.

### Risiken

| Risiko | Warum es hier greift | Gegenmittel |
| --- | --- | --- |
| Runner-Firewall/Port-Konflikt | Der Ubuntu-Runner startet den Prozess selbst; ein falscher Port oder eine belegte Adresse ließe die Retry-Schleife nur ins Timeout laufen | Festen, unwahrscheinlichen Port über `ASPNETCORE_URLS` setzen, Timeout eng genug für schnelles Feedback |
| Fail-fast frisst den Prozess sofort | Fehlt `Jwt:Key` oder ist die Wegwerf-DB-Zeichenkette falsch escaped, bricht der Start ab, bevor `curl` überhaupt fragt | Exit-Code des Hintergrundprozesses nach der Wartezeit mitprüfen, nicht nur den Curl-Status – sonst zeigt ein rotes `curl` nicht die eigentliche Ursache |
| `index.html`-Prüfung zu strikt/zu lasch | Ein zu enger Textvergleich bricht bei jeder Frontend-Änderung, ein zu weiter lässt eine leere `wwwroot` durchgehen | Auf ein stabiles, seltenes Fragment prüfen (z. B. `<div id="root">`), nicht auf den vollen Seitentitel |
| Verwaister Hintergrundprozess bei Job-Abbruch | Ein Timeout-Abbruch (wie in `e2e.yml`) überspringt ein reines `if: failure()`-Cleanup | Cleanup über `if: always()` führen, nicht über `failure()` – das Muster steht bereits im `zustellung`-Job von `e2e.yml` |
| Deploy bleibt stillgelegt | Ohne Reaktivierung (Entscheidung 1) läuft der neue Schritt nie automatisch, nur per `workflow_dispatch` | Bewusst getragen; der Schritt kostet nichts, solange niemand ihn auslöst, und ist bereit, sobald B-33 fällt |

### Angriffsplan

Nur Backend/CI-Brille (Workflow-Datei, kein Frontend-Code), in dieser Reihenfolge:

1. Neuen Schritt „Artefakt-Smoke" zwischen „API publish" und „Nach Azure deployen" in `deploy-azure.yml`
   einfügen: Hintergrundstart, `ASPNETCORE_URLS`, `Jwt__Key`, `ConnectionStrings__Default` auf eine
   Wegwerf-Datei im Runner-Workspace.
2. Retry-Schleife gegen `/health` (Muster aus dem Skill `smoke-test`: `for … curl -s -m 3 …; sleep 1`),
   Zeitlimit z. B. 30 s.
3. Zweiter `curl` gegen `/`, Textprüfung auf ein stabiles Fragment.
4. Cleanup über einen eigenen `if: always()`-Schritt, der Prozess und Wegwerf-DB entfernt.
5. Einmal per `workflow_dispatch` **gegen einen absichtlich kaputten Stand fahren** (z. B. `wwwroot` lokal
   leeren) und rot sehen – ohne diese Gegenprobe ist der Schritt eine Behauptung (Muster B-41, AK 6).

### Testweg

Es entsteht **kein** neuer Eintrag in `Pugling.sln` (Entscheidung 3) und kein Playwright-Spec – der Testweg
**ist** der neue CI-Schritt selbst, gefahren über `workflow_dispatch` auf `deploy-azure.yml` (manuell, da der
automatische Trigger stillgelegt bleibt, Entscheidung 1). Nachweis der Wirksamkeit ist die Gegenprobe aus
Angriffsplan-Schritt 5, nicht der grüne Lauf allein – dasselbe Prinzip wie bei B-41s Gegenproben-Tabelle.

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
