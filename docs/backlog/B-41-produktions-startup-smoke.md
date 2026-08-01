---
tags: [typ/story, status/abgenommen, bereich/qualitaet, bereich/tests]
aliases: [Produktions-Startup-Smoke]
status: abgenommen
prio: P2
art: Aufräumen
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/testplan.md#nachmessung-2026-07-31-die-drei-unbeobachteten-flächen
---

# B-41 · Der Produktionspfad des Starts ist der einzige ohne Test

Alle 615 Tests laufen als **Entwicklungsumgebung**. Jeder Zweig, der nur außerhalb der Entwicklung gilt, ist
damit zu 0 % ausgeführt – und das ist genau die Klasse Fehler, die hier schon zugeschlagen hat: der
Peer-Konflikt `vite-plugin-pwa` ↔ `vite@8` ließ das Azure-Deploy **24 Tage unbemerkt** scheitern, weil kein
Tor die Deploy-Umgebung prüfte.

## User Story

Als **Betreiber**, der die App auf einer echten Instanz hochfährt, möchte ich, dass die Suite die
**Nicht-Entwicklungs-Konfiguration mindestens einmal durchfährt**, damit ein fehlender Schlüssel, ein
unerwartet laufender Seed oder ein offener Anmerkungs-Blick beim Testlauf auffällt und nicht beim Deploy.

## Ist-Stand am Code

`PuglingWebAppFactory.cs:26` setzt `builder.UseEnvironment("Development")` und ist – nachgesehen, nicht
vermutet – die **einzige** `UseEnvironment`-Stelle im ganzen Repo. Damit steht auf der falschen Seite jeder
dieser Zweige:

| Stelle | Was nur außerhalb der Entwicklung gilt | heute ausgeführt |
| --- | --- | --- |
| `Program.cs:197` | `RemarkOptions.GlobalRead = IsDevelopment()` – der kontenübergreifende Anmerkungs-Blick (`?scope=all`) ist in Produktion **zu** | nur der offene Zweig |
| `Program.cs:260` | Fail-fast: ohne `Jwt:Key` wird der Start abgebrochen | nein |
| `Auth/TokenService.cs:15` | Dev-Fallback-Schlüssel `"pugling-dev-signing-key-change-me-please-0123456789"`; in Produktion **muss** der konfigurierte greifen | nur der Fallback |
| `Program.cs:464` | `Seed:Enabled` fällt außerhalb der Entwicklung auf `false` | nur der säende Zweig |
| `Program.cs:389`/`:505` | `UseStaticFiles()` + `MapFallbackToFile("index.html")` – der Single-Host-Deploy (React-PWA aus `wwwroot`) | nein, `wwwroot` ist im Test leer |
| `Program.cs:445` | Abbruch mit Klartext-Meldung, wenn die DB aus der **alten** Migrationskette stammt | nein (Testdatenbanken sind immer frisch, `applied` enthält die bekannte `InitialCreate`) |

Drei Randbedingungen, die die Sache erst zur Lücke machen:

- **`appsettings.json` enthält gar keinen `Jwt`-Abschnitt** (nachgesehen). Der Fail-fast auf `Jwt:Key` ist
  also in Produktion tatsächlich erreichbar und keine tote Zeile.
- **Eine ungeseedete Instanz ist trotzdem in Betrieb zu nehmen:** `POST api/v1/supervisor/adults` trägt
  `[AllowAnonymous]` („Creates a new father (registration, reachable without login)",
  `AdultsController.cs:49-51`), ebenso das Lehrer-Konto. Das ist der Bootstrap-Weg einer frischen Instanz.
- ~~**Es gibt keinen `/health`-Endpunkt**~~ — **falsch, beim Schätzen am 2026-08-01 korrigiert:**
  `Program.cs:498` mappt `/health` (`AddHealthChecks().AddDbContextCheck<PuglingDbContext>()`,
  `Program.cs:219`), anonym und seit dem 2026-07-04 im Baum (`105e2e1`). Kein Test ruft ihn auf — der
  Abdeckungs-Wächter zählt nur Controller-Actions, ein `MapHealthChecks` steht in keinem Inventar. Gegen
  leeres `wwwroot` antwortet `MapFallbackToFile` weiterhin mit 404. Das Deploy hängt am **stillgelegten**
  `deploy-azure.yml` (`on:`-Block auskommentiert, Zeile 27 f.); der Azure-Schlüssel ist als
  [B-33](B-33-azure-publish-profile.md) **bewusst verworfen**.

## Die echte Lücke

Die Idee sagte „out-of-process nötig". Nach der Recherche ist das **zu grob**: der weitaus größte Teil ist
in-process erreichbar, indem eine zweite Fabrik `UseEnvironment("Production")` setzt und `Jwt:Key` als
Einstellung mitgibt – dann laufen `:197`, `:260`, `:464` und `TokenService` auf ihrer Produktionsseite, und
sogar der Fail-fast ist prüfbar (`Assert.Throws` beim Host-Bau ohne Schlüssel). Das ist eine Testklasse, kein
Vorhaben.

Was **wirklich** einen Prozess braucht, ist nur der Rest: ob das *veröffentlichte* Artefakt startet
(`dotnet publish` → `wwwroot` mit dem Vite-Build → Kestrel) und die PWA über `MapFallbackToFile` ausliefert.
Das ist derselbe Weg, den `deploy-azure.yml` geht, und damit eher ein **CI-Job** als ein Test – und solange
das Deploy stillliegt, prüft er einen Weg, den niemand geht.

## Offene Punkte

Alle im Grillen vom 2026-07-31 entschieden bzw. ausdrücklich zurückgestellt.

1. ~~Teilen oder zusammen bauen?~~ → Entscheidung 1
2. ~~Gehört ein `/health`-Endpunkt dazu?~~ → **zurückgestellt**, wandert zu [B-47](B-47-deploy-artefakt-smoke.md)
3. ~~Darf der Produktions-Smoke seeden?~~ → Entscheidung 2
4. ~~Der Alt-Ketten-Abbruch – mitprüfen?~~ → Entscheidung 3

## Entscheidungen

1. **Die Story wird geteilt; hier entsteht nur Teil 1** – die Produktions-**Konfiguration** in-process. Der
   Artefakt-Teil (publish → Kestrel → `wwwroot/index.html`) liegt als [B-47](B-47-deploy-artefakt-smoke.md)
   auf `idee`. Begründung: Teil 1 ist eine Testklasse, kostet Sekunden und macht heute sechs ungeprüfte Zweige
   scharf; Teil 2 kostet Minuten je Lauf und prüft eine Deploy-Form, die derzeit stillliegt.
   **Kosten:** `wwwroot`, Kestrel und `MapFallbackToFile` bleiben ungeprüft, bis es wieder ein Deploy gibt.
2. **Der Test fährt den echten Bootstrap-Weg**, statt den Seed anzuschalten: **eine** Konfiguration
   (Production, `Seed:Enabled` = aus), der Test registriert über den anonymen Endpunkt selbst einen
   Erwachsenen und meldet sich damit an. Begründung: das deckt „ohne Seed ist die DB leer" **und** „ein Token
   gegen den konfigurierten Schlüssel gilt" in einem Ablauf – und pinnt zusätzlich, dass eine frische
   Produktionsinstanz überhaupt in Betrieb zu nehmen ist. Die Alternative (zweiter Host mit `Seed:Enabled` =
   an) hätte die Azure-**Sonderstellung** geprüft, nicht den Normalfall.
   **Kosten:** der Test hängt an der anonymen Registrierung; ändert die sich, fällt er mit.
3. **Der Alt-Ketten-Abbruch (`Program.cs:445`) wird mitgeprüft:** Wegwerf-DB anlegen, in
   `__EFMigrationsHistory` einen Phantasienamen schreiben, Host bauen, auf die Klartext-Meldung prüfen.
   Begründung: der einzige Pfad im Repo, der eine **bestehende** Datenbank betrifft, und er ist bisher nie
   ausgeführt worden. **Kosten:** der Test greift in eine EF-interne Tabelle – eine Kopplung, die bei einem
   EF-Versionswechsel brechen kann. Zusatz: die Regel hat laut `CLAUDE.md` ein Ablaufdatum (sie endet mit der
   ersten Veröffentlichung), der Test also auch.
4. **Reihenfolge:** B-41 wird als **erste** der vier Test-Stories gebaut. Begründung: eine Testklasse, kein
   Produktivcode, keine neue Abhängigkeit – der billigste Ertrag der vier.

## Akzeptanzkriterien

1. Eine Testklasse fährt die App mit `UseEnvironment("Production")` und gesetztem `Jwt:Key` hoch und sichert
   in **einem** Ablauf zu: die DB ist leer (kein Seed) → Registrierung über den anonymen Endpunkt gelingt →
   Login liefert ein Token → `auth/me` akzeptiert es mit den erwarteten Claims.
2. `?scope=all` auf den Anmerkungen ist in dieser Umgebung **zu** (`RemarkOptions.GlobalRead = false`).
3. Ohne `Jwt:Key` bricht der Host-Bau mit der dokumentierten `InvalidOperationException` ab – geprüft, nicht
   nur behauptet.
4. Eine Datenbank mit einem unbekannten Eintrag in `__EFMigrationsHistory` führt zur **Klartext-Meldung** aus
   `Program.cs:445`, nicht zu `table "Adults" already exists`.
5. Die Endpunkt-Abdeckung bleibt bei **0 offenen Actions**, die Suite grün, und die Gesamtlaufzeit steigt um
   **unter fünf Sekunden** (die 52 s sind das Argument, dass niemand das Haupttor umgeht).
6. **Gegenprobe gefahren:** `Seed:Enabled` in der Produktionskonfiguration auf `true` gesetzt → der
   „DB ist leer"-Teil wird rot. Ohne diese Probe ist der Test eine Behauptung.
7. Der Out-of-process-Teil ist **nicht** enthalten, sondern liegt als
   [B-47](B-47-deploy-artefakt-smoke.md) vor – samt der zurückgestellten `/health`-Frage.

## Schätzung

**Größe S** (Anker: „`childId` aus dem Test-Pfad ziehen", B-01) — kein Produktivcode, keine neue
Abhängigkeit, keine Vertragsänderung. Über XS liegt es, weil nicht *eine* Testklasse entsteht, sondern
**drei verschiedene Host-Konfigurationen**: Produktion mit Schlüssel, Produktion ohne Schlüssel (muss
werfen), Produktion gegen eine vorbereitete Alt-Ketten-DB (muss werfen).

`migration: nein` — nachgesehen: es ändert sich kein Entity und kein `DbContext`; die Alt-Ketten-Probe
schreibt von Hand in die `__EFMigrationsHistory` einer **Wegwerf**-Datei, die Kette selbst bleibt bei
`InitialCreate`. `vertragsbruch: nein` — `Pugling.Contracts` wird nicht angefasst.

### Die Produktionsseite ist schmaler als gedacht

Nachgezählt in `Program.cs` gibt es genau **drei** `IsDevelopment()`-Verzweigungen (`:198` `Remarks:GlobalRead`,
`:260` Fail-fast auf `Jwt:Key`, `:462` `Seed:Enabled`) plus den Dev-Fallback in `TokenService.cs:15`. Die
Zeilen `:387`/`:503` (`UseStaticFiles`/`MapFallbackToFile`) und `:445` (Alt-Kette) sind **nicht**
umgebungsabhängig registriert — sie greifen nur nicht, weil `wwwroot` leer bzw. die Test-DB frisch ist. Das
verkleinert Teil 1 gegenüber der Ausformulierung nicht, macht aber die Zusicherung ehrlich: geprüft wird die
Produktions-**Konfiguration**, nicht ein anderer Ablauf.

Ebenfalls nachgesehen und **entlastend**: es gibt kein `UseHttpsRedirection`/`UseHsts` außerhalb der
Entwicklung. Der In-Process-Client redet weiter über HTTP, die Fabrik braucht keinen TLS-Umweg.

### Angriffsplan

Nur Backend, in dieser Reihenfolge (jede Stufe ist für sich grün):

1. **`ProductionWebAppFactory`** neben `PuglingWebAppFactory` — dieselbe Wegwerf-DB-/Medienordner-Mechanik,
   aber `UseEnvironment("Production")` und `Jwt:Key` als `UseSetting`. Den `EndpointCoverageStartupFilter`
   mitnehmen, sonst zählt der Abdeckungs-Wächter die dort bedienten Actions nicht. `Seed:Enabled` **nicht**
   setzen (Entscheidung 2: der Vorgabewert `false` ist der Prüfgegenstand).
2. **`ProductionStartupTests`** — der Bootstrap-Ablauf in einem Test (AK 1) und `?scope=all` → 403 (AK 2).
   Die 403 ist belegt, keine Vermutung: `RemarksController.cs:113`/`:231` hängt sie an
   `RemarkOptions.GlobalRead`.
3. **Fail-fast ohne `Jwt:Key`** (AK 3) — eigene Fabrik ohne die Einstellung, `Record.Exception(…)` beim
   ersten `CreateClient()`.
4. **Alt-Ketten-Probe** (AK 4) — SQLite-Datei anlegen, `__EFMigrationsHistory` mit einem Phantasienamen
   füllen, Host darauf zeigen lassen, auf die Klartextmeldung aus `Program.cs:445` prüfen.
5. **Gegenprobe und Messung** (AK 5, AK 6) — `Seed:Enabled=true` einschalten, rot sehen, zurücknehmen;
   danach Gesamtlaufzeit und `TestResults/endpoint-coverage.txt` gegen den Stand davor halten.

### Testweg

Der Testweg **ist** hier das Ergebnis, deshalb konkret: neue Datei
`backend/Pugling.Api.Tests/ProductionStartupTests.cs` plus `ProductionWebAppFactory.cs` im selben Projekt,
gefahren vom Haupttor `dotnet test Pugling.sln -c Release` (Stop-Hook und CI). Kein E2E, kein
`/smoke-test` — beide fahren die Entwicklungsumgebung und können die Zusicherung gar nicht treffen.
Nachweis der Wirksamkeit ist die Gegenprobe aus AK 6, nicht der grüne Lauf.

### Risiken

| Risiko | Warum es hier greift | Gegenmittel |
| --- | --- | --- |
| Der Fail-fast kommt nicht als `InvalidOperationException` an | `WebApplicationFactory` fängt die Ausnahme des Einstiegspunkts ab und wirft sie verzögert – teils gekapselt, teils als „entry point exited without ever building an IHost" | Auf die **Meldung** prüfen (`Jwt:Key`), die Kette der `InnerException` mitnehmen, nicht auf den exakten Typ pinnen |
| Der Abdeckungs-Wächter geht rot, obwohl nichts fehlt | `EndpointCoverageGuard.FullRunTouchedActions = 263` ist eine **obere** Schranke: berührt der neue Test eine bisher unberührte Action, verlangt der Wächter das Nachziehen der Zahl | Registrierung/Login/`auth/me` sind bereits abgedeckt, also erwartet unverändert – nach dem ersten Voll-Lauf den Bericht lesen und die Konstante ggf. setzen |
| Login-Rate-Limit (10/min je IP) | Die Produktionsfabrik lässt `RateLimiting:LoginEnabled` bewusst an; alle Tests der Klasse teilen eine IP-Partition | Bei wenigen Logins unkritisch; wächst die Klasse, die Anmeldungen zählen statt den Schalter blind auszuschalten |
| Kopplung an EF-Interna | Die Alt-Ketten-Probe schreibt in `__EFMigrationsHistory` (Kosten aus Entscheidung 3) | Bewusst getragen; die Regel hat ohnehin ein Ablaufdatum (erste Veröffentlichung) |
| Laufzeit-Budget (< 5 s) | Drei zusätzliche Host-Starts, jeder mit `Migrate()` auf frischer SQLite-Datei | Messen statt hoffen; notfalls die beiden werfenden Fälle in **eine** Klasse legen und die Fabriken nicht cachen |

### Fund beim Schätzen

`/health` existiert (`Program.cs:498`, seit `105e2e1` vom 2026-07-04) — die im Grillen **zurückgestellte**
Frage aus offenem Punkt 2 ist damit gegenstandslos. Für B-41 ändert das nichts (AK 1 fährt den fachlichen
Weg), wohl aber für [B-47](B-47-deploy-artefakt-smoke.md): dessen Ist-Stand-Annahme „ohne `/health` muss sich
ein Smoke an einer fachlichen Route festmachen" trägt nicht mehr. Nachzuziehen beim Ausformulieren von B-47.

## Verifikation

Gebaut als [ProductionStartupTests](../../backend/Pugling.Api.Tests/ProductionStartupTests.cs) +
[ProductionWebAppFactory](../../backend/Pugling.Api.Tests/ProductionWebAppFactory.cs); die geteilte
Mechanik liegt in `PuglingWebAppFactoryBase`
([PuglingWebAppFactory.cs](../../backend/Pugling.Api.Tests/PuglingWebAppFactory.cs)).

| AK | Beleg |
| --- | --- |
| 1 | `FrischeProduktionsinstanz_IstOhneSeedInBetriebZuNehmen` – kein Seed-Login, leerer Katalog, anonyme Registrierung, Id 1, Login, `auth/me` mit Creator **und** Supervisor. |
| 2 | `AnmerkungsBlick_UeberAlleKonten_IstInProduktionZu` – `?scope=all` → 403 `remark_scope_forbidden`, und im selben Test die Gegenrichtung (ohne `scope` → 200). |
| 3 | `OhneJwtSchluessel_BrichtDerStartAb` – geprüft auf die **Meldung** in `thrown.ToString()`, nicht auf den Typ. |
| 4 | `DatenbankAusDerAltenKette_MeldetKlartextStattEfFehler` – inkl. der Zusicherung, dass `already exists` **nicht** durchschlägt. |
| 5 | Volle Suite **620/620 grün** (48 s bzw. 51 s in zwei Läufen); Abdeckung **263/263, 0 offen**; `FullRunTouchedActions` bleibt bei **263**. Zur Laufzeit siehe unten. |
| 6 | Fünf Gegenproben, jede rot gesehen – Tabelle unten. |
| 7 | Out-of-process nicht enthalten; liegt als [B-47](B-47-deploy-artefakt-smoke.md). |

### Die Gegenproben

| Probe | Manipulation | Was fiel |
| --- | --- | --- |
| A | `Seed:Enabled=true` | `#1/0000` meldet sich an → `Expected: Unauthorized, Actual: OK` |
| B | `Remarks:GlobalRead=true` | `?scope=all` → `Expected: Forbidden, Actual: OK` |
| C | `Jwt:Key` gesetzt | kein Abbruch → `Assert.NotNull() Failure` |
| D | bekannte Migrations-Id statt fremder | Alt-Ketten-Zweig feuert nicht → `Assert.NotNull() Failure` |
| E | Dev-Fallback **als** `Jwt:Key` | gefälschtes Token wird angenommen → `Expected: Unauthorized, Actual: OK` |

### AK 5 ist präziser, als die Wanduhr hergibt

Die Suite schwankt bei **identischem** Stand zwischen 41 s und 56 s; in paarweisen Läufen (mit/ohne die
neue Klasse, direkt hintereinander, viermal) war die Variante *mit* den Tests zweimal die schnellere. Ein
Fünf-Test-Effekt ist darin nicht auflösbar. Belastbar ist die **serielle Mehrarbeit** aus dem
Testprotokoll: **6,85 s von 920 s** (0,7 %), längster neuer Test 3,42 s gegen 14,78 s für den längsten
Test der Suite – die Klasse liegt also nicht auf dem kritischen Pfad. Die Zusicherung „unter fünf
Sekunden" gilt damit **dem Sinn nach belegt, der Zahl nach unterhalb der Messgrenze**.

### Zwei Funde beim Bauen, beide nicht in der Story vorhergesehen

1. **Der Bootstrap-Test war reihenfolgeabhängig.** Beide Tests der Klasse teilten über `IClassFixture`
   **eine** Datenbank und registrierten je einen Erwachsenen – lief der Anmerkungs-Test zuerst, bekam der
   Bootstrap-Test Id 2. In Release grün, in Debug und unter jedem `--filter` rot; das Haupttor fährt
   `-c Release` und hätte es verdeckt. Der Test hat jetzt seinen **eigenen** Host, und „kein Seed" steht auf
   zwei Beinen (Login **und** leerer Katalog), weil ein 401 allein auch von einer falschen PIN käme.
2. **Der ursprüngliche Schlüssel-Nachweis bewies nichts.** „Token wird akzeptiert" zeigt nur, dass
   Ausstellen und Prüfen übereinstimmen – und das tun sie immer: beide Seiten hängen an derselben
   `TokenService`-Instanz. Käme `Jwt:Key` nirgends an, fielen beide gemeinsam auf den Fallback zurück und
   `auth/me` bliebe 200. Es gibt darum einen fünften Test: ein mit dem **Dev-Fallback** signiertes Token
   muss 401 bekommen. Das ist die Aussage, um die es geht – der Fallback steht im Klartext im Quellbaum.

### Nebenbefund: 14 GB Wegwerf-Datenbanken

`PuglingWebAppFactoryBase.Dispose(bool)` lief für Klassen-Fixtures **nie**: xUnit entsorgt über
`IAsyncDisposable`, und `WebApplicationFactory.DisposeAsync()` führt nicht durch `Dispose(bool)`. Zusätzlich
hielt der SQLite-Verbindungspool die Datei offen, sodass `File.Delete` still im `catch` scheiterte. Stand am
2026-08-01: **20 880 verwaiste `pugling_test_*.db`, 14 GB**, angesammelt seit dem 4. Juli. Behoben durch ein
`DisposeAsync()`-Override plus `SqliteConnection.ClearPool` **auf die eigene** Verbindung; danach gemessen:
Leck-Delta **0** je Lauf. Zwei Irrwege sind dabei ausgeschlossen worden und stehen als Begründung im Code:
`Pooling=False` schob die Suite von ~1 auf **3 Minuten**, und das prozessweite `ClearAllPools` warf je Lauf
drei bis vier fremde Tests um, weil xUnit Klassen parallel fährt.

Die Altlast (23 888 Dateien, 16,1 GB) ist am 2026-08-01 gelöscht. Beim Aufräumen kamen **zwei weitere
Erzeuger** zum Vorschein, die dieselbe Sorgfalt vermissen lassen – `QueryPlanSmokeTests` und die
Playwright-Konfiguration; sie liegen als [B-55](B-55-wegwerf-dateien-aufraeumen.md).

## Verlauf

- **2026-07-31** — angelegt (Quelle: Nachmessung der Test-Abdeckung, [testplan.md](../testplan.md)).
- **2026-07-31** — ausformuliert: sechs Produktions-Zweige mit Zeilen belegt; die Idee „out-of-process nötig"
  hat die Recherche **halbiert** – vier der sechs sind in-process erreichbar.
- **2026-07-31** — gegrillt: vier Entscheidungen, `/health` zurückgestellt. Der Zuschnitt hat sich beim
  Nachsehen noch einmal verbessert: weil die Registrierung `[AllowAnonymous]` ist, braucht der Test **keinen**
  zweiten Host mit Seed, sondern fährt den echten Inbetriebnahme-Weg einer frischen Instanz.
- **2026-08-01** — ins [Testabdeckungs-Paket](../testabdeckung-plan.md) als **E1** aufgenommen (erste
  Etappe). Zwei Auflagen aus der Dev-Runde: **Basisklasse statt zweiter Fabrik-Kopie** – fehlt in der Kopie
  `UseSetting("ConnectionStrings:Default", …)`, migriert der Test die **echte** `pugling.db`; und die Etappe
  **besitzt** `EndpointCoverageGuard.FullRunTouchedActions` (Naht 1). Akzeptanzkriterium 4 (Alt-Kette)
  **bleibt**, bekommt aber den Ablaufvermerk „stirbt mit der ersten Veröffentlichung" in den Test.
- **2026-08-01** — geschätzt: **S**, backend, keine Migration, kein Vertragsbruch; Angriffsplan in fünf
  Schritten, fünf benannte Risiken. Zwei Belege haben den Ist-Stand berichtigt: es gibt nur **drei**
  `IsDevelopment()`-Zweige (die Static-Files-Zeilen sind umgebungsunabhängig registriert), und `/health`
  existiert seit dem 2026-07-04 – die zurückgestellte Frage aus Punkt 2 ist gegenstandslos und wandert als
  Korrektur zu [B-47](B-47-deploy-artefakt-smoke.md).
- **2026-08-01** — gebaut als Etappe **E1**. `pugling-reviewer` hat einen Blocker und fünf Befunde
  gebracht, alle übernommen: die Reihenfolge-Abhängigkeit (in Release unsichtbar), der Schlüssel-Nachweis
  ohne Beweiskraft, `ConfigureWebHost` jetzt `sealed`, `MessageChain` durch `ToString()` ersetzt, die
  Doku-Drift auf verschobene Zeilen, und das 14-GB-Leck in `Dispose`.
- **2026-08-01** — **abgenommen**: 620/620 grün, Abdeckung 263/263 (0 offen), fünf Gegenproben je einmal
  rot gesehen. `FullRunTouchedActions` bleibt bei 263 – **Naht 1 aus dem
  [Paket-Plan](../testabdeckung-plan.md) ist damit erledigt, E2 muss nichts nachziehen.**
