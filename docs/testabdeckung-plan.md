---
tags: [bereich/qualitaet, bereich/tests, status/gegrillt]
---

# Testabdeckung: das Paket aus sieben Stories

Status: **Gegrillt am 2026-08-01, acht Entscheidungen** (unten). Dieses Dokument ist die Quelle der Wahrheit für
**Reihenfolge, Schnitt und die spurübergreifenden Entscheidungen**. Die einzelnen Stories bleiben bestehen und
behalten ihre Stufe – hier steht **keine Kopie** ihrer Akzeptanzkriterien, sondern nur, was sich aus dem
Zusammenlegen ergibt (Backlog-Regel „ein gepflegtes Plandokument bleibt Quelle der Wahrheit, der Bereich trägt
eine Sammel-Story mit Link", [backlog/README.md](backlog/README.md)). Sammel-Story:
[B-52](backlog/B-52-testabdeckung-paket.md).

## Warum überhaupt ein Paket

Die sieben Stories stammen aus zwei Messungen ([testplan.md](testplan.md): Defektinjektion 2026-07-30,
Nachmessung 2026-07-31) und wurden einzeln ausformuliert. Einzeln gebaut kollidieren sie an drei Stellen, die
in keiner der Stories steht: sie teilen **eine Konstante** (`EndpointCoverageGuard.FullRunTouchedActions`),
**ein Artefakt** (`/openapi/v1.json`) und **ein Lockfile** (zwei devDependencies, ein Peer-Konflikt). Genau
diese drei Nähte sind der Grund für dieses Dokument.

## Was die Dev-Runde am 2026-08-01 geändert hat

Backend- und Frontend-Sicht wurden getrennt befragt (Agenten `pugling-reviewer` / `frontend-reviewer`,
Auftrag: Machbarkeit und Reihenfolge, nicht Review). Beide haben unabhängig voneinander dieselben zwei Stories
aus dem Paket geschoben – und **drei Story-Aussagen widerlegt**. Die drei folgenreichsten sind nachgeprüft und
nicht übernommen:

1. **B-26s Prämisse ist falsch, und dahinter liegt ein Defekt.** „In CI nie gelaufen" stimmt nicht:
   `.github/workflows/e2e.yml` hat zwei geplante Läufe, `30517281632` (2026-07-30) **grün** und
   `30608976657` (2026-07-31 06:12 UTC) **rot** (`gh run list --workflow=e2e.yml`, selbst nachgesehen).
   Ursache belegt: `frontend/e2e/vater-von-null.spec.ts:265-272` fährt den Abschnitt „Lernziel auf
   Fach-Ebene" und klickt „Lernziel anlegen" – in `frontend/src` hat dieser Text **0 Treffer**, seit
   `6471e1d` die Ebene gelöscht hat (siehe [B-14](backlog/B-14-learngoal-belohnung.md)). Die E2E-Suite hat
   also genau getan, wofür sie da ist; erreicht hat es niemanden.
2. **B-42s großer Diff ist keiner.** `frontend/src/lib/types.ts` hat **null Laufzeit-Exporte**, und alle 54
   importierenden Dateien nutzen `import type` (gezählt, nicht geschätzt). Die Datei kann **Barrel** bleiben
   und die Deklarationen scheibenweise durch `S["…"]`-Aliase ersetzt werden – 54 Konsumenten bleiben
   unangetastet.
3. **B-43 rechnet den Defekt zu klein.** Die Story sagt „alle 24 Aufrufer haben `disabled={busy}`". Nachgezählt
   und selbst nachgesehen: **fünf Knöpfe haben keins** – `VaterRewards.tsx:131,133,214,216` und
   `VaterShop.tsx:443` („Stornieren", ein **Geldpfad auf der Vater-Seite**, womit das Argument „der Geldpfad
   des Sohnes läuft nicht über das Primitiv" hier nicht greift). Bei `toggle` ist der Doppelschuss zudem ein
   **Flip-Flop** (`active: !m.active` zweimal = Ausgangszustand, Banner meldet Erfolg).

## Die zwei Spuren

Es ist **keine Kette**. Backend (E1–E3) und Frontend (E4–E6) berühren sich nur im Artefakt aus E3, und das
braucht E6 erst am Ende. E0 steht vor beiden, weil es ein offener Defekt ist.

| Etappe | Inhalt | Story | Spur | Vorbedingung |
| --- | --- | --- | --- | --- |
| **E0** | Roter E2E-Nachtlauf: toter Lernziel-Abschnitt + die Frage, wie ein rotes Nightly jemanden erreicht | [B-26](backlog/B-26-e2e-in-ci.md) (neu zuschneiden) | – | keine |
| **E1** | Produktions-Konfiguration in-process | [B-41](backlog/B-41-produktions-startup-smoke.md) | Backend | keine |
| **E2** | Client-Routen gegen das lebende OpenAPI-Dokument | [B-40](backlog/B-40-client-routen-waechter.md) | Backend | E1 (nur wegen der Konstante) |
| **E3** | Vertragsreines `/openapi/v1.json` eingecheckt + Diff-Tor | [B-42](backlog/B-42-openapi-typen-generieren.md), Schritt 1 | Backend | keine |
| **E4** | Werkzeugkette Frontend: beide devDependencies, ein Lockfile-Schritt | – (neu) | Frontend | keine |
| **E5** | Wiedereintritts-Sperre + Tests auf die geteilten Primitive | [B-43](backlog/B-43-frontend-komponententests.md) | Frontend | E4 |
| **E5'** | Doppelklick im Lehrplan-Assistenten (zwei Kinder, zwei Pläne) | [B-53](backlog/B-53-wizard-doppelklick.md) | Frontend | keine (mit E5 gebaut) |
| **E6** | Vertragstypen generieren, `tsc` als Tor | [B-42](backlog/B-42-openapi-typen-generieren.md), Schritt 2 | Frontend | E3, E4, E5 |

## Die drei geteilten Nähte

Sie sind der eigentliche Inhalt dieses Dokuments – in keiner Einzelstory stehen sie.

### Naht 1 · `EndpointCoverageGuard.FullRunTouchedActions` gehört E1

`EndpointCoverageGuard.cs:30` pinnt `263` als **obere** Schranke: Wer als Erster eine bisher unberührte Action
erfolgreich aufruft, muss die Zahl ziehen (`:75`, „Erfreulich: … Bitte FullRunTouchedActions auf diesen Wert
setzen"). E1 und E2 fahren beide neue Hosts. **Regel: E1 besitzt die Konstante**, E2 zieht nach, falls nötig.
Ohne diese Zuweisung ist es bei parallelen Zweigen ein garantierter Merge-Konflikt an einer Zeile – und beim
Auflösen gewinnt leicht die kleinere Zahl, was den Wächter stumpf macht.

### Naht 2 · Das eingecheckte OpenAPI-Dokument ist heute nicht byte-stabil

Der schwerste Fund der Runde, und er kippt eine Entscheidung aus B-42. Das Dokument ist **nicht
selbsttragend**: `Program.cs:279` lädt beim Hoststart `OpenApiExampleCatalog.Load(ContentRootPath)`, das
(`OpenApiExampleCatalog.cs:20`) `backend/Pugling.Api/OpenApi/openapi-examples.generated.json` **aus dem
Quellbaum** liest; der Operations-Transformer hängt die Beispiele in jede Operation. Dieselbe Datei schreibt
`DocsCaptureTests` im selben Lauf neu (`DocsCaptureTests.cs:1105-1113`). Da xUnit über Collections
parallelisiert, sieht ein Host den alten Katalog und ein anderer den neuen – **B-42s Akzeptanzkriterium 1
(„zwei Läufe erzeugen denselben Inhalt") ist heute nicht erfüllbar**, und das Tor würde flappen.

**Entscheidung: E3 checkt eine vertragsreine Fassung ein** (Beispielkatalog beim Erzeugen übersprungen – der
Transformer steigt bei leerem Katalog selbst aus, `OpenApiExamplesOperationTransformer.cs:16-17`). Damit ist
das Dokument klein, stabil und unabhängig vom Beispiel-Schreibpfad. **Kosten:** das eingecheckte Dokument
zeigt nicht mehr, was Swagger zur Laufzeit ausliefert; die Beispiele bleiben bei D4 und beim Katalog-Diff.
Zusatz aus derselben Naht: `openapi-examples.generated.json` ist eingecheckt, wird zur Laufzeit gelesen und
beim Publish mitkopiert (`Pugling.Api.csproj:44`), aber **vom D4-Tor nicht gedeckt** – eine Zeile in E3, und
zwar in den D4-Schritt, nicht in ein drittes Tor.

E2 bleibt davon unberührt und liest bewusst **lebend** (B-40, Entscheidung 1): Der Wächter startet für das
Typ-Manifest ohnehin einen Host, das Dokument kostet ihn nichts. Positivbefund dazu: `Program.cs:129` setzt
`SubstituteApiVersionInUrl = true`, das Dokument trägt also `/api/v1/…` – die naive Falle „Client schreibt
`v1`, Doku schreibt `{version}`" gibt es nicht.

### Naht 3 · Ein Lockfile, zwei devDependencies, ein Peer-Konflikt

E5 und E6 bringen je eine devDependency mit. Beide zusammen in **E4**, einmal `npm ci --legacy-peer-deps`
gegengeprüft, statt zweimal am Frontend-Job zu wackeln. Nachgesehen statt vermutet:

- **`openapi-typescript` ist unkritisch** – Peer ist nur `typescript ^5.x` (installiert `^5.6.3`), es fasst
  die `vite`-Kante nicht an. **[B-25](backlog/B-25-vite-pwa-peer-konflikt.md) blockiert das Paket nicht.**
- **`@testing-library/react` braucht `@testing-library/dom@^10` als eigene devDependency** – es ist ein Peer
  und wird nicht gebündelt, und unter `--legacy-peer-deps` installiert npm fehlende Peers **nicht**. Sonst:
  „Cannot find module" zur Laufzeit.
- **`vitest.config.ts` setzt kein `globals: true`** – ohne globales `afterEach` registriert RTL sein
  automatisches `cleanup()` nicht, das DOM bleibt zwischen Fällen stehen und die Reihenfolge entscheidet.
  Entweder `globals: true` oder ein Setup-File mit explizitem `cleanup()`. **Das ist die Flake-Quelle der
  Etappe, nicht die Laufzeit** (Baseline gemessen: 21 Tests, 1,97 s).
- Nebenbei korrigiert: das Frontend läuft auf **React 18.3.1**, nicht 19.

## Die Etappen

### E0 · Der rote Nachtlauf (vorgezogen, Defekt)

Zwei Teile, und nur der erste ist Code: (a) `vater-von-null.spec.ts:265-275` wird **zur Etappe am Objective
umgeschrieben** (Entscheidung 2), nicht gestrichen; (b) die eigentliche offene Frage aus `e2e.yml:7-15`: Wie
erreicht ein rotes Nightly jemanden, wenn es bewusst **kein** Freigabe-Tor ist? Das Tor zu verschieben ist
ausdrücklich **nicht** gemeint – `ci.yml:142-144` hält Playwright bewusst draußen, und der Lauf kostet
~2,3 min Testphase.

Der Grund für „umschreiben statt streichen" steht im Nachbarabschnitt: 8a legt zwar ein **Objective** an
(`+ Großes Ziel anlegen`), aber **nie eine Etappe** – im ganzen Spec kommt kein `KeyResult` vor, während
`VaterZiele.tsx:244` selbst sagt „Noch keine Etappen – ohne sie kann das Ziel nicht erreicht werden". Streichen
hieße also: das Rot verschwindet, und mit ihm die Abdeckung des Scope-Wählers, der Messlatte und der ganzen
Ebene, die die gelöschte Lernziel-Ebene fachlich beerbt hat.

B-26 muss dafür neu zugeschnitten werden: aus „einen CI-Lauf einrichten" (erledigt, nur nicht bemerkt) wird
„einen roten Lauf auswerten und ihn zustellbar machen".

### E1 · Produktions-Konfiguration ([B-41](backlog/B-41-produktions-startup-smoke.md), `geschaetzt`, S)

Unverändert im Inhalt, mit zwei Auflagen aus der Runde:

- **Basisklasse statt zweiter Fabrik-Kopie.** Fehlt in einer Kopie
  `UseSetting("ConnectionStrings:Default", …)`, greift `appsettings.json` mit
  `Data Source=pugling.db` relativ zum ContentRoot – der Test migriert dann die **echte**
  `backend/Pugling.Api/pugling.db`. Das ist kein sichtbarer Testfehler, sondern eine kaputte
  Entwicklerdatenbank. Dasselbe gilt für `Media:RootPath`. **Umgesetzt:** beide stehen einmal in
  `PuglingWebAppFactoryBase`, und zwar **nach** dem Ableitungs-Hook (das spätere `UseSetting` gewinnt);
  `ConfigureWebHost` ist `sealed`, sonst könnte eine neue Fabrik es überschreiben und den `base`-Aufruf
  vergessen.
- **Akzeptanzkriterium 4 (Alt-Ketten-Probe) bleibt** (Entscheidung 8), trägt aber im Kommentar sein
  Ablaufdatum: es stirbt mit der Regel, also mit der ersten Veröffentlichung. Zusatzbefund, der beim Bauen
  Zeit spart: der Fail-fast (`Program.cs:260`) wirft **vor** `builder.Build()`, der Alt-Ketten-Abbruch
  (`:442-452`) **danach** – zwei verschiedene Wege durch `WebApplicationFactory`, nicht ein Risiko.

### E2 · Client-Routen-Wächter ([B-40](backlog/B-40-client-routen-waechter.md), `gegrillt`)

Inhaltlich unverändert; die Reihenfolge nach E1 ist **nur** der Konstante aus Naht 1 geschuldet, nicht einer
fachlichen Abhängigkeit. Aus Backend-Sicht die risikoärmste Etappe des Pakets: ein Ort, kein Produktivcode.

### E3 · Vertragsreines Dokument + Tor (B-42 Schritt 1)

Siehe Naht 2. Rein backendseitig, berührt `frontend/` mit keiner Zeile – kann parallel zu allem laufen.

### E4 · Werkzeugkette Frontend (neu, nicht aus einer Story)

Siehe Naht 3. Eine Etappe, ein Lockfile-Commit, ein Nachweis: `npm ci --legacy-peer-deps` fehlerfrei,
`npm test` weiterhin grün.

### E5 · Sperre und Primitive-Tests ([B-43](backlog/B-43-frontend-komponententests.md), `gegrillt`, Defekt)

Der Umfang **wächst** gegenüber der Story – ohne die vier folgenden Punkte schließt sie die Fehlerklasse nicht,
sondern nur ihren gut abgesicherten Teil:

1. **Die fünf Knöpfe ohne `disabled`** (oben belegt) bekommen es dazu. Die Sperre ist **additiv**:
   `disabled={busy}` bleibt überall – es ist heute der Serialisierungspunkt, auf den Playwrights Actionability
   wartet. Wer es nach der Sperre als „überflüssig" entfernt, nimmt der E2E den Wartepunkt, und Klicks werden
   **still geschluckt**.
2. **`useAction` trägt auch einen Lesevorgang:** `MediaPickers.tsx:77-82` ist die Bibliothekssuche. Eine
   Sperre, die die zweite Aktion still verwirft, macht aus „ich suche neu" ein „nichts passiert". Diese
   Stelle wird **vor** der Sperre auf `useAsync` umgestellt (Entscheidung 5) – das Primitiv heißt selbst
   „Zustand einer **schreibenden** Aktion" (`useAction.ts:10`), die Suche gehörte nie hinein.
3. **Eine `ActionState`-Instanz wird über Listen geteilt** (`PlanPositions.tsx:38→:48/:58`, ebenso
   `VaterShop.tsx:47→:105`, `VaterZiele.tsx:164→:178`). Der Ref-Lock ist damit **listenweit**, nicht
   knopfweit – bewusst so (Entscheidung 5: keine Schlüssel-Signatur am Primitiv). Heute macht
   `disabled={busy}` das sichtbar (alle Knöpfe grau); nach der Sperre ist es unsichtbar, **deshalb** gehört
   es als Satz in die Doku von `useAction`.
4. **Die teuerste Doppelklick-Stelle liegt außerhalb von `useAction`** und wird als eigene Story gebaut
   (Entscheidung 6, [B-53](backlog/B-53-wizard-doppelklick.md)): `VaterWizard.tsx:172-188` hat ein
   handgebautes `busy` (State) und setzt `done.childId` erst **nach** dem `await` – zwei Klicks im selben Tick
   legen **zwei Kinder und zwei Lehrpläne** an.

Gestrichen wird dagegen die Ausnahme aus B-43/Entscheidung 3 („einen Bildschirm stellvertretend rendern"):
Der Defekt sitzt in `useAction`, nicht im Knopf; zwei synchrone `run()`-Aufrufe auf derselben Hook-Instanz
(`renderHook`) zeigen ihn genauso rot – ohne `api.ts` und Router hereinzuziehen. Die Regel in
`frontend/CLAUDE.md` wird damit sauber: **nur `components/` und `lib/`, ohne Sternchen.**

### E6 · Generierte Vertragstypen (B-42 Schritt 2)

Über den **Barrel** und in Scheiben (Katalog, Plan, Shop, Wallet, Medien …), jede Scheibe für sich
`tsc -b`-grün – nicht „einmal am Stück". Drei Hand-Ausnahmen sind vorab bekannt, statt sie zu entdecken:

- **Die Generika** `ExercisePayload<TConfig>`/`ExerciseResponse<TConfig>`: kein Benennungsproblem, sondern
  Absicht – die Oberfläche kollabiert sie bewusst zu einem `CreateExercisePayload` mit `config: unknown`
  (`types.ts:1063-1090`), weil der Typ zur Laufzeit aus dem Server-Manifest kommt.
- **`[Flags] SchoolTypes`** (`Contracts/Common/LearnBaseTypes.cs:8-9`): serialisiert als
  `"Gymnasium, Realschule"`, im Schema stehen aber die Einzelnamen. Das Frontend deklariert deshalb bewusst
  `schoolTypes: string` und baut den String von Hand (`ExerciseEditModal.tsx:102`/`:142`). Generiert entstünde
  hier ein **falsches Rot an einer korrekten Stelle**.
- **`required`/`nullable` ist die unvermessene Größe**, nicht die Generika: `Program.cs:297-303` rechnet
  `schema.Required` aus der Nullability neu. Jedes zusätzliche `?` in den generierten Typen ist ein
  `strictNullChecks`-Fehler an jeder Lesestelle. **Vor der Schätzung von E6 einmal `openapi-typescript` über
  das Dokument laufen lassen und `tsc -b` die Fehler zählen lassen** – das ist billig und macht die Etappe
  erst belastbar.

Gute Nachricht, belegt: Enums überleben. `Program.cs:284-295` setzt `schema.Type = String` **und**
`schema.Enum = [names]`; daraus werden String-Literal-Unions, kein Typverlust gegenüber heute.

## Nicht im Paket

- **[B-47](backlog/B-47-deploy-artefakt-smoke.md) (Deploy-Artefakt-Smoke)** – bleibt `idee`, außerhalb.
  `deploy-azure.yml:27-33` ist stillgelegt (`on:` auskommentiert), das Secret als
  [B-33](backlog/B-33-azure-publish-profile.md) bewusst verworfen. Der Job bewachte einen Weg, den niemand
  geht. **Eintrittsbedingung:** wird gebaut, wenn der `workflow_run`-Block wieder scharf ist – und dann als
  **CI-Job**, nie als Test in `Pugling.sln`: `dotnet publish` + Vite-Build + Kestrel liegen im Minutenbereich
  und liefen sonst bei jeder `.cs`-Änderung im Stop-Hook mit.
- **[B-27](backlog/B-27-testsuite-grenzfaelle.md) in der heutigen Form** – die Story hat **kein Arbeitsblatt
  mehr.** In [testplan.md](testplan.md) ist jeder benannte Punkt geschlossen: Klasse (a) im zweiten Commit
  (`:585-594`), die Restliste (c) „Stand 2026-07-30: abgearbeitet" mit Gegenprobe je Rang (`:416-426`), der
  `SpeedBonusTests`-Flake (`:510-518`), die vier Grenzfall-Regeln D01/D07/D11/D15 (`:487-490`). Wer sie so
  annimmt, erhebt neu (ausgeschlossen) oder erfindet Tests. Was der Testplan **offen** lässt, steht in
  `:660-666`: Unit zu Integration ist 5 % zu 95 %, „jeder Grenzfall kostet einen vollen Flow". Der
  vorgeschlagene Zuschnitt ist deshalb eine einzige `[Theory]`-Klasse gegen `ScoringService` (unit-fähig, nimmt
  nur `IOptions<ScoringOptions>`, `ScoringService.cs:15`), die **auf** den Grenzen prüft statt daneben:
  `MinSpeedSeconds` exakt 1,0 (`:37`), die Combo-Schwelle exakt erreicht/um eins verfehlt, die
  Zeitfenster-Kante exakt 12:00, die höchste Leitner-Stufe. Abnahme ist nicht die Zahl der Tests, sondern **je
  Grenze eine Gegenprobe**. Entscheidung 7: **so zugeschnitten angenommen, aber außerhalb des Pakets** – es
  ist Testtiefe, nicht Abdeckung, und teilt mit dem Paket keine der drei Nähte.

## Laufzeit: was die Tore kosten

Gemessen bzw. abgeschätzt, weil das Argument gegen jedes neue Tor die Laufzeit ist:

- **Stop-Hook** (`dotnet test Pugling.sln -c Release`, ~63 s): E1 drei zusätzliche Host-Starts, davon zwei
  Fehlstarts (billig) und einer **ohne** Seed (billiger als die bestehenden Dev-Hosts); E2 ein Host mit Seed
  (1–2 s); E3 ein weiterer Start. Zusammen im Sekundenbereich, das „< 5 s" aus B-41/AK 5 ist plausibel.
- **E5/E6 lösen den Stop-Hook gar nicht aus** – er greift bei abweichenden `.cs`-Dateien.
- **CI-Frontend-Job:** E6 addiert die Generierung (Sekunden) und `tsc -b` über eine maschinengenerierte
  Typdatei – der einzige messbare Zuwachs im Haupttor, und der Grund, das Dokument aus Naht 2 klein zu halten.
  E5 addiert Vitest+RTL auf 1,97 s Baseline.

## Entscheidungen

Getroffen am 2026-08-01 im Dialog. Jede mit Begründung **und** Kosten – eine Entscheidung ohne Kosten ist
eine Meinung.

1. **Die sieben Stories bleiben bestehen; dieses Dokument führt.** Es besitzt Reihenfolge, Nähte und die
   spurübergreifenden Entscheidungen, die Stories bleiben die Arbeitseinheiten und behalten ihre Stufe;
   [B-52](backlog/B-52-testabdeckung-paket.md) ist die Sammel-Story. Begründung: vier von ihnen sind gegrillt
   oder geschätzt – beim Verschmelzen ginge dieser erarbeitete Zustand ein, und eine Story über alle sieben
   wäre XL („gibt es nicht, dann wird geteilt", [backlog/README.md](backlog/README.md)).
   **Kosten:** der Zustand steht an zwei Orten – Etappe hier, Stufe dort. Die Regel dagegen: **der Zustand
   einer Etappe steht in ihrer Story**, hier steht nur die Reihenfolge.
2. **E0 wird vorgezogen, und der tote Abschnitt wird auf die Etappe umgeschrieben statt gestrichen.**
   Begründung: die KeyResult-Ebene, die die Lernziel-Ebene beerbt hat, ist im E2E heute **gar nicht**
   abgedeckt; ein Objective ohne Etappe nennt die Oberfläche selbst unerreichbar (`VaterZiele.tsx:244`).
   Streichen brächte Grün und weniger Abdeckung als vorher. **Kosten:** eine echte Spec-Änderung statt fünf
   gelöschter Zeilen, und E0 verzögert das Paket um diese Arbeit.
3. **Das eingecheckte Dokument ist vertragsrein** (Naht 2). Begründung dort. **Kosten:** es zeigt nicht mehr,
   was Swagger ausliefert – es ist der Vertrag, nicht die Dokumentation. Damit kippt B-42/Entscheidung 3 auf
   **zwei** Tore statt drei: D4 (inklusive `openapi-examples.generated.json`) und das Vertragsdokument.
4. **E6 arbeitet über den Barrel, in Scheiben** – damit ist B-42/AK 5 Pflicht, aber teilbar. Begründung:
   `types.ts` hat null Laufzeit-Exporte und alle 54 Importe sind `import type`; die Konsumenten bleiben
   unberührt, jede Scheibe ist für sich `tsc -b`-grün. **Kosten:** eine Übergangszeit, in der die Datei
   gemischt ist (generierte Aliase neben Handarbeit) – lesbar nur, weil jede verbleibende Handdeklaration
   einen Satz Begründung trägt.
5. **Die Sperre wirkt je Hook-Instanz; die Bibliothekssuche wird vorher aus dem Primitiv gelöst.** Kein
   Schlüssel-Parameter an `useAction`. Begründung: eine Schlüssel-Signatur nützt erst, wenn 24 Aufrufer sie
   nachziehen, und der einzige belegte Schaden am instanzweiten Verhalten ist die Suche – die im
   **Schreib**-Primitiv ohnehin falsch sitzt. **Kosten:** in den drei Listen-Bildschirmen blockiert Zeile 3
   die Zeile 7; heute ist das sichtbar (`disabled`), danach stumm. Deshalb der Satz in der Doku.
6. **`VaterWizard` wird eine eigene Defekt-Story** ([B-53](backlog/B-53-wizard-doppelklick.md), P2), gebaut im
   selben Durchgang wie E5. Begründung: andere Bauform – der Wizard hat bereits einen `progress`-Ref, die
   Sperre gehört dorthin, nicht ins Primitiv. **Kosten:** zwei Akten für eine Fehlerklasse; dafür bleibt die
   Abnahme je Bauform sauber.
7. **B-27 wird neu zugeschnitten und bleibt außerhalb** (siehe „Nicht im Paket"). **Kosten:** die
   Testtiefe-Frage wandert aus dem Paket heraus und konkurriert wieder mit allem anderen um Aufmerksamkeit.
8. **E1/AK 4 (Alt-Ketten-Probe) bleibt, mit Ablaufvermerk im Test.** Begründung: es ist der einzige Pfad im
   Repo, der eine **bestehende** Datenbank betrifft, und die Meldung, die er bewacht, ist genau die Sorte, aus
   der eine Handlung folgt. **Kosten:** eine Kopplung an `__EFMigrationsHistory`, die ein EF-Versionswechsel
   brechen kann – und ein Test, der planmäßig mit der ersten Veröffentlichung stirbt.

## Verlauf

- **2026-08-01** — angelegt: sieben Stories gebündelt, Backend- und Frontend-Sicht getrennt eingeholt, drei
  Story-Aussagen widerlegt und nachgeprüft (roter E2E-Nachtlauf, `import type`-Barrel, fünf Knöpfe ohne
  `disabled`). Zwei Stories aus dem Paket geschoben, eine Etappe (E4) neu erfunden, eine Entscheidung aus
  B-42 gekippt (Naht 2).
- **2026-08-01** — gegrillt: acht Entscheidungen. Zwei haben den Zuschnitt verändert – E0 **schreibt um statt
  zu streichen** (die KeyResult-Ebene ist im E2E gar nicht abgedeckt, das kam erst beim Grillen heraus), und
  die Sperre aus E5 bekommt keinen Schlüssel-Parameter, dafür wandert die Bibliothekssuche aus dem
  Schreib-Primitiv. Neu abgespalten: [B-53](backlog/B-53-wizard-doppelklick.md).
