---
tags: [typ/story, status/gegrillt, bereich/qualitaet, bereich/tests]
aliases: [Produktions-Startup-Smoke]
status: gegrillt
prio: P2
art: Aufräumen
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
- **Es gibt keinen `/health`-Endpunkt**; gegen leeres `wwwroot` antwortet `MapFallbackToFile` mit 404. Das
  Deploy hängt am **stillgelegten** `deploy-azure.yml` (`on:`-Block auskommentiert, Zeile 27 f.); der
  Azure-Schlüssel ist als [B-33](B-33-azure-publish-profile.md) **bewusst verworfen**.

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

## Verlauf

- **2026-07-31** — angelegt (Quelle: Nachmessung der Test-Abdeckung, [testplan.md](../testplan.md)).
- **2026-07-31** — ausformuliert: sechs Produktions-Zweige mit Zeilen belegt; die Idee „out-of-process nötig"
  hat die Recherche **halbiert** – vier der sechs sind in-process erreichbar.
- **2026-07-31** — gegrillt: vier Entscheidungen, `/health` zurückgestellt. Der Zuschnitt hat sich beim
  Nachsehen noch einmal verbessert: weil die Registrierung `[AllowAnonymous]` ist, braucht der Test **keinen**
  zweiten Host mit Seed, sondern fährt den echten Inbetriebnahme-Weg einer frischen Instanz.
