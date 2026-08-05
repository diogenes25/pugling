---
tags: [typ/story, status/abgenommen, bereich/qualitaet, bereich/tests]
aliases: [Beispielkatalog-Rennen, openapi-examples IOException]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/backlog/B-42-openapi-typen-generieren.md
nachgeschaut: 2026-08-05
---

# B-57 · Im Testlauf lesen und schreiben zwei Stellen gleichzeitig dieselbe Katalogdatei

`openapi-examples.generated.json` wird im **selben** Testlauf gelesen und geschrieben:
`OpenApiExampleCatalog.Load` öffnet sie beim ersten Zugriff auf `/openapi/v1.json` mit `File.OpenRead`,
`DocsCaptureTests.WriteOpenApiExamples` überschreibt sie am Ende ihres einzigen, langen Tests mit
`File.WriteAllText`. Beide laufen ohne erzwungene Reihenfolge im selben `dotnet test`-Prozess.

## User Story

Als Entwickler möchte ich, dass der Testlauf die generierte Beispiel-Datei robust liest und schreibt, damit
ein `IOException`/`JsonException`-Flake nicht irgendwann eine grüne Suite unvorhersehbar rot macht und eine
Untersuchung erzwingt, die schon einmal fällig war.

## Ist-Stand am Code

- **Leser**: `OpenApiExampleCatalog.Load` öffnet die Datei mit `File.OpenRead` (implizit `FileShare.Read`)
  und deserialisiert sie direkt aus dem Stream (`OpenApiExampleCatalog.cs:30-39`). Aufgerufen wird das nicht
  beim Hoststart, sondern lazy beim ersten Auflösen von `IOptions<OpenApiOptions>` – de facto beim ersten
  `GET /openapi/v1.json` eines Hosts (`Program.cs:283-286`, `builder.Services.AddOpenApi(o => …)`), gesteuert
  über `OpenApi:ExamplesEnabled` (Default `true`, einzige Ausnahme `ContractDocumentTests.cs:189`).
- **Schreiber**: `DocsCaptureTests.WriteOpenApiExamples` schreibt die Datei mit `File.WriteAllText`
  (`DocsCaptureTests.cs:1106-1114`), einmalig im `finally`-Block des einzigen `[Fact] CaptureAll`
  (`DocsCaptureTests.cs:186-222`) – also erst, nachdem der gesamte API-Durchlauf (Auth, Katalog, Lehrplan,
  Shop, Anmerkungen …) durchgelaufen ist, ganz am Ende eines mehrere Sekunden langen Tests.
- Die Datei ist **eingecheckt** (`backend/Pugling.Api/OpenApi/openapi-examples.generated.json`,
  aktuell 151 158 Byte) – kein Gitignore, kein triviales Umschreiben in Nullzeit: `File.WriteAllText`
  trunkiert zuerst (`FileMode.Create`) und schreibt dann die vollen ~148 KB.
- Von 79 Testdateien mit `IClassFixture<PuglingWebAppFactory>` (`grep`-Zählung) treffen nur die, die
  tatsächlich `GET /openapi/v1.json` aufrufen, überhaupt auf `Load`. Das sind vier Dateien:
  `ContractDocumentTests.cs:124` (schaltet `ExamplesEnabled=false`, `:189` – sicher),
  `OpenApiExampleTests.cs:13` **und** `:28`, `ClientRouteGuardTests.cs:121`, `ErrorCodeTests.cs:155` – die
  letzten drei lassen `ExamplesEnabled` auf dem Default und sind damit exponiert.
- Von diesen dreien prüft nur `OpenApiExampleTests.OpenApi_EnthaeltVerifizierteRequestUndResponseBeispiele`
  (`OpenApiExampleTests.cs:9-22`) tatsächlich **Beispiel-Inhalte** (feste Keys wie `auth-vater-login`).
  `ClientRouteGuardTests.OpenApiPathsAsync` liest nur `paths` (`ClientRouteGuardTests.cs:119-127`),
  `ErrorCodeTests.OpenApi_CodeEnum_DecktSichMitRegistry` nur das `ProblemDetails.code`-Enum aus
  `components.schemas` (`ErrorCodeTests.cs:150-159`) – beide brauchen `Load()` gar nicht, sie lösen es nur
  aus, weil derselbe Endpunkt zufällig denselben Transformer durchläuft.
- Keine `[Collection]`/`CollectionBehavior`-Klammer bindet `DocsCaptureTests` an eine der drei Klassen oder
  schaltet die Parallelisierung ab (projektweite Suche: kein Treffer). Das Projekt läuft auf **xunit.v3**
  (`Pugling.Api.Tests.csproj:29`); ohne explizites `[Collection(...)]` ist jede Testklasse ihre eigene
  implizite Collection, und xunit.v3 parallelisiert Collections standardmäßig – `DocsCaptureTests`' einziger
  Test kann also zeitgleich mit jeder der drei anderen Klassen laufen.
- **Reproduziert** (Wegwerf-Skript außerhalb des Repos, nicht Teil dieser Story): paralleles
  `File.WriteAllText` gegen dieselbe Datei vs. `File.OpenRead` + `JsonDocument.Parse`, 5 Sekunden Dauerlast,
  ergab **954 `IOException`** („being used by another process") **und 419 `JsonReaderException`**
  (abgeschnittener/unvollständiger Inhalt) bei rund 1 374 Leseversuchen. Das Rennen ist real – **und**
  breiter als die Notiz vermutete: nicht nur `IOException` in beiden Richtungen, auch ein sauber geöffneter,
  aber inhaltlich zerrissener Read (der Schreiber trunkiert sofort und füllt danach schrittweise).

## Die echte Lücke

Nicht alle 79 Hosts sind exponiert, sondern genau drei – und von denen brauchen zwei (`ClientRouteGuardTests`,
`ErrorCodeTests`) die Beispiele überhaupt nicht, sie laden sie nur als Nebenwirkung desselben Endpunkts. Echt
betroffen ist genau `OpenApiExampleTests`, dessen einziger Zweck es ist, die echten Beispiele im Dokument zu
sehen – der kann `Load()` mit realem Inhalt nicht umgehen. Und das Fehlerbild ist nicht nur „`IOException` in
beide Richtungen" (so die ursprüngliche Notiz), sondern schließt einen sauberen, aber inhaltlich kaputten
Read ein – relevant für die Wahl der Antwort: ein toleranteres `FileShare` löst nur den Öffnen-Konflikt, nicht
den zerrissenen Inhalt.

## Offene Punkte

- ~~Ist das Rennen überhaupt beobachtbar – gezielt provoziert, nicht auf Glück gewartet?~~ → siehe
  Entscheidung 1 (reproduziert: 954 `IOException` + 419 `JsonReaderException` in 5 s).
- ~~Tolerantes Öffnen (`FileShare.ReadWrite`), atomares Schreiben (Temp + `File.Move`) oder den Katalog gar
  nicht mehr aus dem Quellbaum lesen?~~ → siehe Entscheidung 2.
- ~~Muss `OpenApiExampleCatalog.Load` (Produktionscode in `Pugling.Api`) selbst angefasst werden?~~ → siehe
  Entscheidung 3.
- ~~Sollen `ClientRouteGuardTests`/`ErrorCodeTests` weiterhin unnötig Beispiele laden, obwohl sie nur Pfade
  bzw. das Fehlercode-Enum brauchen?~~ → siehe Entscheidung 4.
- ~~Wie wird der Fix als Regressionstest bewiesen, ohne den echten Testlauf selbst zusätzlich
  timing-abhängig zu machen?~~ → siehe Entscheidung 5.

## Entscheidungen

1. **Das Rennen ist real und gehört als Beleg in den Ist-Stand, nicht als offene Frage.** Ein gezieltes,
   nicht eingechecktes Wegwerf-Skript (paralleles `File.WriteAllText` vs. `File.OpenRead`+`JsonDocument.Parse`
   über 5 s) erzeugte 954 `IOException` und 419 `JsonReaderException`. Begründung: „selten beobachtet" war
   bisher nur „nie gezielt provoziert" – die Häufigkeit im echten Testlauf hängt am Timing zweier konkreter
   Testklassen, das Muster selbst ist nicht spekulativ. Kosten: keine, reine Erkenntnis.
2. **Fix ist atomares Schreiben (Temp-Datei im selben Verzeichnis + `File.Move(tmp, final, overwrite: true)`)
   – nicht `FileShare.ReadWrite` und nicht „Katalog nicht mehr aus dem Quellbaum lesen".** Begründung:
   `FileShare.ReadWrite` verhindert nur den Öffnen-Konflikt, nicht den zerrissenen Inhalt (siehe Repro: die
   419 `JsonReaderException` entstehen unabhängig vom Sharing-Modus, weil `FileMode.Create` sofort trunkiert
   und der Leser mitten in den neuen Bytes landet). „Nicht mehr aus dem Quellbaum lesen" schließt die Lücke
   nicht für `OpenApiExampleTests`, dessen Zweck genau der reale Inhalt ist. Ein atomarer Rename lässt jeden
   Leser entweder die vollständige alte oder die vollständige neue Datei sehen, nie einen Zwischenzustand.
   Kosten: eine kurzlebige Temp-Datei im selben Verzeichnis während des Schreibens.
3. **`OpenApiExampleCatalog.Load` (Produktionscode) bleibt unverändert; nur `DocsCaptureTests.WriteOpenApiExamples`
   (Testcode) wird umgestellt.** Begründung: Ist der Ziel-Dateiname immer entweder vollständig alt oder
   vollständig neu, braucht ein einfacher `File.OpenRead` kein besonderes Sharing – die Lücke schließt sich
   an der Quelle (dem einzigen Schreiber), nicht durch Abwehr beim Leser. Ein Eingriff in die
   Produktionsklasse träfe zudem den echten Serverstart (`Program.cs`) ohne zusätzlichen Nutzen. Kosten:
   keine zusätzlichen.
4. **`ClientRouteGuardTests` und `ErrorCodeTests` bekommen `builder.UseSetting("OpenApi:ExamplesEnabled", "false")`**
   auf ihrer jeweiligen Factory-Konfiguration, analog zu `ContractDocumentTests.cs:189`. Begründung: Beide
   brauchen nur `paths` bzw. das `ProblemDetails.code`-Enum aus `components.schemas` – Beispiele haben auf
   diese Assertions keinen Einfluss (Grep bestätigt: keiner der beiden Tests liest `examples`). Das
   Ausschalten reduziert die reale Exposition von drei auf eine Testklasse, unabhängig vom Fix in
   Entscheidung 2 – zwei Zeilen, kein Verhaltensrisiko. Kosten: keine (reine Verhaltens-Verengung ohne
   Assertion-Änderung).
5. **Testweg ist ein neuer, fokussierter Concurrency-Test gegen eine Wegwerf-Kopie, nicht der Versuch, den
   echten `dotnet test`-Lauf verlässlich rot zu bekommen.** Begründung: Ein Test, der auf das Timing
   zwischen `DocsCaptureTests` und einer der drei anderen Klassen im echten Lauf wettet, wäre selbst wieder
   ein Flake – das Gegenteil vom Ziel. Ein deterministischer Test gegen eine temporäre Datei, der die exakten
   Lese-/Schreib-Aufrufe (`File.WriteAllText`-Form vs. `File.OpenRead`+`JsonSerializer.Deserialize`-Form)
   eine kurze Zeit im Dauerlauf gegeneinander laufen lässt, ist schnell, reproduzierbar und beweist vor dem
   Fix rot / nach dem Fix grün, ohne den Rest der Suite zu verlangsamen. Kosten: ein zusätzlicher Testfile.

## Akzeptanzkriterien

1. Ein neuer Concurrency-Test (Arbeitstitel `OpenApiExampleCatalogConcurrencyTests`, gegen eine
   Wegwerf-Kopie, nicht die echte generierte Datei) ist **vor** der Umstellung reproduzierbar rot
   (mindestens eine `IOException`/`JsonException` über mehrere Wiederholungen) und **nach** ihr über
   mehrere Wiederholungen grün.
2. `DocsCaptureTests.WriteOpenApiExamples` schreibt über eine Temp-Datei im selben Verzeichnis
   (`backend/Pugling.Api/OpenApi/`) und `File.Move(tmp, final, overwrite: true)`; ein fehlgeschlagener Move
   hinterlässt keine Temp-Datei im Quellbaum (Aufräumen im `finally`/`catch`).
3. `ClientRouteGuardTests` und `ErrorCodeTests` setzen `OpenApi:ExamplesEnabled=false`; ihre bestehenden
   Assertions (Pfade bzw. `ProblemDetails.code`-Enum) bleiben unverändert grün.
4. `OpenApiExampleCatalog.Load` und `OpenApiExamplesOperationTransformer` bleiben unverändert – kein
   Produktionsverhalten ändert sich, nur der Schreibpfad im Testprojekt wird robust.
5. `dotnet test Pugling.sln -c Release` bleibt vollständig grün, insbesondere `DocsCaptureTests`,
   `OpenApiExampleTests`, `ClientRouteGuardTests`, `ErrorCodeTests`, `ContractDocumentTests`.

## Schätzung

**Größe: S** – ein Schreibpfad wird von `File.WriteAllText` auf Temp-Datei + `File.Move` umgestellt (wenige
Zeilen in `DocsCaptureTests.cs`), zwei `UseSetting`-Zeilen in zwei fremden Testklassen, ein neuer fokussierter
Unit-Test. Vergleichbar mit dem S-Anker B-01 (`childId` aus dem Test-Pfad ziehen), deutlich kleiner als die
M-Anker B-03/B-10.

- **wo**: backend – die Änderung liegt vollständig im Backend-Testprojekt (`Pugling.Api.Tests`); die
  Produktionsklasse `OpenApiExampleCatalog` in `Pugling.Api` bleibt unberührt (Entscheidung 3). Kein
  Frontend-Anteil.
- **migration**: nein – keine Schemaänderung.
- **vertragsbruch**: nein – keine Änderung an `Pugling.Contracts`; reiner Test-/Schreibpfad-Umbau.
- **Risiken**: `File.Move(overwrite: true)` verlangt keine Volume-Gleichheit-Sorge, weil Temp- und Zieldatei
  bewusst im selben Verzeichnis liegen. Ein Leser, der exakt während der Rename-Operation öffnet, ist auf
  NTFS praktisch nicht beobachtbar (Rename ist ein Metadaten-Vorgang auf dem Verzeichniseintrag, kein
  Stream-Schreiben) – der neue Concurrency-Test (Akzeptanzkriterium 1) ist die Absicherung, falls diese
  Annahme doch nicht trägt.
- **Angriffsplan**: Backend/Testprojekt zuerst und einzig, in dieser Reihenfolge: 1) neuen
  Concurrency-Test rot bekommen (belegt das Muster deterministisch), 2) `WriteOpenApiExamples` auf
  Temp+`File.Move` umstellen, 3) dass derselbe Test grün wird, 4) `ExamplesEnabled=false` in
  `ClientRouteGuardTests`/`ErrorCodeTests` ergänzen, 5) volle Suite laufen lassen.
- **Testweg**: neuer Test `OpenApiExampleCatalogConcurrencyTests.cs` in `Pugling.Api.Tests` (deterministisch,
  gegen eine Wegwerf-Kopie); zusätzlich als Regressionsschutz im vollen Lauf `DocsCaptureTests`,
  `OpenApiExampleTests`, `ClientRouteGuardTests`, `ErrorCodeTests`, `ContractDocumentTests`.

## Verlauf

- **2026-08-01** — geerntet aus dem Review zu [B-42](B-42-openapi-typen-generieren.md) Schritt 1 (E3),
  ungeprüft: die `IOException` ist beobachtet, ihre Häufigkeit und die richtige Antwort sind offen.
- **2026-08-03** — ausformuliert: Ist-Stand mit `Datei:Zeile` belegt (`OpenApiExampleCatalog.cs:30-39`,
  `Program.cs:283-286`, `DocsCaptureTests.cs:1106-1114`/`186-222`, `ContractDocumentTests.cs:189`); die
  Exposition auf drei von 79 Testklassen präzisiert (`OpenApiExampleTests`, `ClientRouteGuardTests`,
  `ErrorCodeTests`), das Rennen per Wegwerf-Skript reproduziert (954 `IOException` + 419
  `JsonReaderException` in 5 s) und damit als real statt spekulativ belegt.
- **2026-08-03** — gegrillt: alle Offenen Punkte in fünf nummerierte Entscheidungen überführt, u. a. für
  atomares Schreiben statt toleranterem Öffnen und für einen deterministischen Concurrency-Test statt eines
  Versuchs, den echten Testlauf verlässlich rot zu bekommen (autonom getroffen, Nutzerauftrag 2026-08-04).
- **2026-08-03** — geschätzt: Größe S, `wo: backend`, keine Migration, kein Vertragsbruch, Angriffsplan in
  fünf Schritten, Testweg `OpenApiExampleCatalogConcurrencyTests.cs` plus fünf bestehende Regressionstests
  (autonom getroffen, Nutzerauftrag 2026-08-04).
- **2026-08-05** — im Autonomen Modus gebaut, ohne Rückfrage je Ticket: `DocsCaptureTests.WriteOpenApiExamples`
  schreibt jetzt über eine Temp-Datei im selben Verzeichnis + `File.Move(overwrite: true)` (mit kurzer
  Retry-Schleife, `MoveWithRetry`); `ClientRouteGuardTests`/`ErrorCodeTests` laufen gegen eine neue
  `SchemaOnlyWebAppFactory` mit `OpenApi:ExamplesEnabled=false` (Entscheidung 4). Der erste Entwurf des
  Concurrency-Tests (Entscheidung 5) hat mehrfach die Marschrichtung geändert, weil die reale Racing-Probe
  gegen echte Disk-/OS-Zeitwerte auf dieser Windows-Maschine einen fremden Störfaktor maß: **jede** frische
  Datei-Schreib-/Umbenennungsoperation — unsicher oder atomar — kann kurz (20-65 ms) exklusiv gesperrt sein,
  fast sicher Windows-Echtzeit-Virenschutz beim Scannen der berührten Datei, unabhängig vom eigentlichen
  Fehlerbild (zerrissener JSON-Inhalt). Ein kontinuierlich pollender Reader auf einem `Task.Run` wurde unter
  dem vollen `dotnet test Pugling.sln`-Lauf (726 Tests, xunit.v3 parallelisiert jede Testklasse mangels
  `[Collection]`) zudem gelegentlich durch Thread-Pool-Druck verhungert und lieferte falsch-grüne Ergebnisse.
  Endgültiger Testaufbau: der Schreiber pausiert **selbst erzwungen** mitten im Schreiben (kein Wetten auf
  reale I/O-Geschwindigkeit mehr), ein Reader auf einem dedizierten `Thread` (nicht Thread-Pool) pollt
  währenddessen; das beweist deterministisch „kein Reader sieht je den halbgeschriebenen Temp-Inhalt" — die
  eigentliche Story-Fehlerklasse — nicht „ein Reader während des Renames selbst ist sicher" (bleibt
  akzeptierte, im Risiken-Abschnitt schon benannte Annahme, siehe Klassenkommentar in
  `OpenApiExampleCatalogConcurrencyTests.cs`). Rote Probe vorab bestätigt (`UnsicheresSchreiben_ErzeugtLeseFehler`
  scheiterte gegen den ursprünglichen `File.WriteAllText`-Schreiber), danach grün, 25+ Wiederholungen isoliert
  sowie zweimal der volle `dotnet test Pugling.sln -c Release` → **726/726 grün**. `pugling-reviewer` fand
  einen echten (wenn auch seltenen) Hänger-Fall — der Reader-Thread lief bei einem Writer-Fehler vor dem
  Pause-Callback endlos weiter — behoben mit `try`/`finally` ums Schreiben; außerdem die Doku im
  Klassenkommentar auf die engere, tatsächlich bewiesene Eigenschaft geschärft. Die vom `DailyBoxService`
  verursachte Zufallsrauschen in `docs/api-examples/study-plans.md`/`openapi-examples.generated.json`
  (unabhängig von dieser Story, siehe [B-107](B-107-dailybox-zufallswert-in-docs-capture.md)) trat bei jedem
  Testlauf neu auf und wurde vor dem Commit auf die eingecheckten Werte zurückgesetzt. Commit `a19a702`,
  dazu dieser. Status → `abgenommen`.
- **2026-08-05** — Nachtrag zur neuen Eintrittsbedingung (README → „Der Rollengang fällt am leichtesten
  weg"): **kein Rollengang geführt, und keiner möglich** — die Änderung wirkt nicht zur Laufzeit für
  Creator, Vater oder Sohn (sie betrifft den Testlauf selbst). Belegt bleiben Suite und Reviewer; das ist hier die
  vollständige Verifikation, keine Lücke.
- **2026-08-05** — **Nachschau** (Selbst-Check, kein Reviewer-Lauf; Protokoll `docs/pm-sitzung-2026-08-05.md`):
  **kein Befund.** Die Änderung berührt keinen Produktionscode — sie führt `SchemaOnlyWebAppFactory` ein,
  die für zwei Testklassen `OpenApi:ExamplesEnabled` abschaltet, damit sie nicht mehr gegen die Datei
  lesen, die `DocsCaptureTests` im selben parallelen Lauf schreibt. Geprüft: die beiden Klassen lesen
  wirklich nur `paths`/`components.schemas`, und die Suite ist grün (730/730), das Verhalten der übrigen
  Tests also unverändert.
