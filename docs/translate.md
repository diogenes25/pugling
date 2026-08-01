---
tags: [typ/plan, bereich/doku, status/abgeschlossen]
aliases: [Übersetzung XML-Docs, Doku auf Englisch, Glossar Übersetzung]
---

# Code-Dokumentation im Backend auf Englisch übersetzen

> **Lebendes Nachschlagewerk, kein Wegwerf-Plan.** Diese Seite trägt Glossar und Fortschritt der gesamten
> Übersetzungsarbeit. Jeder Übersetzungs-Agent bekommt den Abschnitt „Glossar" im Prompt mit; gepflegt wird
> er **nur hier** und **nur von der steuernden Sitzung** (siehe Entscheidung 4).

## Ausführung: eigener Branch, DB-Layer nachgezogen

Der Hauptteil lief auf Branch **`docs/xml-docs-englisch`** in einem eigenen Worktree
(`.claude/worktrees/xml-docs-englisch`), abgezweigt vom damaligen `db-struktur-umbau`-HEAD. Grund: eine
**parallele Sitzung arbeitete im Haupt-Worktree am DB-Layer** – ein Branch-Wechsel dort hätte ihr den Boden
weggezogen.

Daraus folgte der wichtigste Scope-Schnitt jenes Durchgangs: **`backend/Pugling.Api/Models/` und
`backend/Pugling.Api/Data/` blieben unübersetzt** (398 der 1266 `<summary>` in `Pugling.Api`), damit
in den Dateien, die die parallele Sitzung umbaute, keine Merge-Konflikte entstanden. Beide Ordner sind von
`CS1591` ohnehin freigestellt (`.editorconfig`) – sie fließen nicht in Swagger.

**Nachgeholt am 2026-08-01** (Etappe 7), nachdem der DB-Umbau abgeschlossen war: 24 Dateien, 398 `<summary>`,
`Data/Migrations/` bleibt als Generat unangetastet (dort steht ohnehin kein `///`). Damit ist **kein
deutsches XML-Doc mehr im Backend**.

## Warum

Sämtliche Code-Dokumentation im Backend ist auf Deutsch (Konvention in `CLAUDE.md`: „Doku auf Deutsch.").
Stattdessen soll internationales Englisch stehen – u. a. weil einige Fachbegriffe in der Doku
uneinheitlich/seltsam übersetzt wurden (z. B. deutsche Prosa, die einen Typ beschreibt, der im Code längst
einen anderen, englischen Namen trägt – der `Adult`/`Vater`-Fall ist in `CLAUDE.md` selbst dokumentiert).

**Scope dieser Etappe: nur `///`-XML-Doc-Kommentare** (`<summary>`, `<param>`, `<returns>`, `<remarks>`) in
den fünf Backend-Projekten. Inline-`//`-Warum-Kommentare (~1900 Zeilen) und Markdown-Docs (`docs/`, `wiki/`,
restliche `CLAUDE.md`-Prosa, ~16k Zeilen) sind bewusst **nicht** Teil dieser Etappe – das wäre ein eigener
Plan.

## Ausgangsbefund (Momentaufnahme, read-only erhoben)

~2303 `<summary>` + ~441 `<param>` + 5 `<returns>` + 2 `<remarks>`:

| Projekt | `<summary>` |
|---|---|
| `Pugling.Api` | 1260 |
| `Pugling.Contracts` | 492 |
| `Pugling.Api.Tests` | 243 |
| `Pugling.Client` | 181 |
| `Pugling.Agent.Creator` | 127 |

**Keine Tooling-Kopplung an den Wortlaut** – Übersetzen ist build-/testgefahrlos:

- `CS1591` prüft nur *Vorhandensein*, nicht Sprache.
- `DocsCaptureTests` generiert `docs/api-examples/*.md` aus eigenen deutschen Titel-Strings **im Testcode**,
  nicht aus `/// <summary>`-Text.
- Keine `IncludeXmlComments`-Anbindung an Swagger gefunden.

## Glossar

Bedeutung am Code ausrichten, nicht Wort für Wort übersetzen.

| Deutsch (in der Doku) | Englisch | Warum |
|---|---|---|
| „Stick" / Malus | **penalty** | Deckt sich mit `PenaltyCoins`, `GoalPenalty` – die Bild-Metapher nicht wörtlich übersetzen. |
| „Sohn" (Rolle/Person) | **child** | Entität heißt `Child`. |
| „Sohn" (Lern-Ebene gemeint) | **student** | Nur wo die Tier gemeint ist (`api/v1/student/…`), analog zur Adult/Father-Unterscheidung. |
| „Vater" (Nicht-Kind-Zeile) | **adult** | `CLAUDE.md`: im Vertrag heißt es durchgehend `Adult`. |
| „Vater" (wirklich der Vater) | **father** | Bleibt richtig, wo ein Vater gemeint ist (`SupervisorRelation.Father`). |
| „Fahrplan" / „Lehrplan" | **study plan** | Beide Wörter stehen im Fließtext für `StudyPlan` – einheitlich auflösen. |
| „Klassenarbeit" | **class test** | Deckt sich mit `api/v1/supervisor/class-tests`, `ClassTestGrade`. |
| „nur Vater" / „als Vater" (Berechtigung) | **supervisor only** / **as the supervisor** | Gegated wird auf `Roles.Supervisor`, nicht auf die Verwandtschaft. |
| „Buchung" (Punkte/Wallet) | **ledger entry** | `PointLedger`; „booking" wäre im Shop-Kontext irreführend. |
| „Beherrschung" | **mastery** | |
| „Leitner-Stufe" / „Box" | **Leitner box** | Ein Begriff für beides, `BoxIntervalDays`. |
| „Pflichtziel" | **mandatory goal** | Der „Stick" hängt daran (`PenaltyCoins`). |
| „Übersteuerung" (Bild) | **override** | Positions-Bild schlägt Store-Bild. |
| „Reihe" (Buchreihe) | **series** | `Textbook`/Units. |
| „Einlösung" | **redemption** | Ausstellergebunden (`SupervisorId`). |
| „Wächter" (Konventionstest) | **guard** | `ConventionGuardTests`. |
| „Tor" (Qualitätstor) | **gate** | Test-Gate/CI. |
| Anführungszeichen „…" | **"…"** | Deutsche Zitatzeichen werden gerade. |

Im Verlauf entschieden (gemeldet von den Übersetzungs-Agenten, hier zentral festgelegt):

| Deutsch | Englisch | Anmerkung |
|---|---|---|
| „Etappe" / `KeyResult` im Fließtext | **milestone** | Der **Codetyp bleibt `KeyResult`**; „milestone" ist die Prosa-Form und im Baum durchgängig. |
| Schularten („Hauptschule", „Realschule", „Gymnasium") | **Umschreibung + deutscher Begriff in Klammern**, z. B. „Lower secondary school (Hauptschule)" | Es gibt kein 1:1-Äquivalent; die **Enum-Werte bleiben deutsch**. |
| „Fachlehrer" | **subject teacher** | Creator-Profil. |
| „Ausspiel-Modus" / „Ausspiel-Reihenfolge" | **playback mode** / **play-out order** | `PracticeOrder`. |
| „Klausur-Modus" | **class-test mode** | |
| „Kaufbuchung" | **purchase entry** | Ledger-Sinn, passend zu ledger entry. |
| „Auffüll-Regel" | **refill rule** | |
| „Wiedervorlage" | **review** / **review scheduling** | Leitner. |
| „redaktioneller Rang" | **editorial rank** | Medien-Verknüpfung. |
| „Existenzprüfung" | **existence check** | Vokabelspeicher. |
| „Rechenart" | **operation type** | |
| „Glosse" | **gloss** | Deckt sich mit der Property `Gloss` (Birkenbihl). |
| „Wort-Oberfläche" | **surface form** | Birkenbihl-Token. |
| „Freigeben" / „Zurückziehen" (Übung teilen) | **publish** / **withdraw** | |
| „Träger" (Medien-Verknüpfung) | **carrier** | |
| „Notnagel" | **stopgap** | |
| „Merkeffekt" | **retention effect** | |
| „Bildkonstanz" | **image constancy** | Anti-Cheat bei Bildwahlen. |
| „Blatt-Guard" | **leaf guard** | |
| „Zielstatus" | **goal status** | |
| „Genauigkeits-Kaskade" | **specificity cascade** | |
| „verfahrensneutral" | **type-agnostic** | |
| „Vater-Konto" (im Kontrast zum Lehrer-Konto) | **father account** | Hier **bewusst wörtlich**: die Stelle stellt ein reines Lehrer-Konto einem echten Vater-Konto gegenüber – „supervisor" würde den Gegensatz einziehen. |
| Schulnoten („mindestens 2,0") | **at least 2.0** | Die deutsche 1–6-Skala bleibt als Konvention stehen. |
| „Darstellung" (Medien) | **asset** | Passt zu `MediaAssetResponse`. |
| „Ergebnisziel" / „Beherrschungsziel" | **outcome goal** / **mastery goal** | Lernziele. |
| „Regelzuordnung" (Bild↔Vokabel) | **default assignment** | |
| „übungslokale Übersteuerung" | **exercise-local override** | |
| „Wackelkandidat" | **shaky candidate** | |
| „Sicherheitsabstand" (Token-Refresh) | **safety margin** | |
| „Ablenker" (Wortbank/MC) | **distractor** | |
| „Lücke" (einzelne) | **gap** | Der Übungstyp bleibt **cloze**. |
| „Herkunftsnotiz" | **provenance note** | |
| „Reparatur-Runde" | **repair round** | KI-Creator-Pipeline. |
| „Trockenlauf" | **dry run** | |
| „Selbsttest" | **self-test** | |
| „Merkhinweis" / „Eselsbrücke" | **memory aid** / **mnemonic** | |
| „Regelverstoß" | **rule violation** | |
| „Fachlogik" | **domain logic** | |
| „Katalog-Ort" | **catalog location** | |
| „Grundform" | **base form** | |
| „Break-Glass" | **break-glass** | Etablierter Begriff, bleibt. |
| „Sichtbarkeitstrennung" | **visibility separation** | |
| „Wegwerf-Konto" / „Wegwerf-Ordner" | **throwaway account** / **disposable folder** | Tests. |
| „Ausspielung" | **delivery** | Ergänzt „playback mode". |
| „Sammelbecken" | **catch-all** | |
| „Selbstauskunft" (`/auth/me`) | **self-info lookup** | |

Aus Etappe 7 (DB-Layer) hinzugekommen:

| Deutsch | Englisch | Anmerkung |
|---|---|---|
| „Momentaufnahme" | **snapshot** | Durchgängig für die eingefrorenen Anzeige-/Aussteller-Felder (`ShopPurchase`, `ChildInventory`, `MissionAward`). |
| „Nebenläufigkeits-Marke" | **concurrency stamp** | Deckt sich mit `ConcurrencyStamp`. |
| „Deckungs-Check" | **funds check** | Wallet. |
| „Wert-Vergleicher" | **value comparer** | Deckt sich mit EFs `ValueComparer`. |
| „Nachlauf" (Seed) | **follow-up** | Der zweite Seed-Block, der Services braucht. |
| „Zusicherungs-Tabelle" | **assurance table** | Die gepinnte Liste in `SchemaGuardTests`. |
| „Kappungsgrenze" | **cut-off** | String-Längen-Konvention. |
| „Ausspiel-Historie" | **delivery history** | Ergänzt „delivery". |
| „ordnend verglichen" | **compared by order** | Begründung der Int-Enum-Ausnahmen. |
| „Wortpool"/„Wortbank" | **word pool** / **word bank** | `WordBank`. |
| „Lehrwerk-Reihe" | **textbook series** | Deckt sich mit `TextbookSeries`; „Reihe" allein bleibt **series**. |
| „Inhalts-Atom" | **content atom** | Die kleinste spielbare Einheit einer Übung. |
| „Verwandtschaftsangabe" | **kinship** | `SupervisorRelation`. |
| „Betreuungs-Beziehung" | **supervision relation** | `SupervisorLink`. |
| „Bestehensgrenze" | **pass threshold** | `GoalThreshold`. |
| „Bruch"/„Vertragsbruch" | **break the contract** | |

Weitere Begriffe entstehen im Verlauf und werden hier nachgetragen (Entscheidung 4).

## Entscheidungen (geklärt)

1. **Mechanischer mehrsprachiger Regex-Vorlauf, zuerst, über alle fünf Projekte in einem Lauf.**
   Ursprünglich verworfen, aber auf expliziten Wunsch nach maximaler Token-Sparsamkeit wieder aufgenommen:
   ein einmaliges Such-/Ersetzen-Skript (PowerShell) ersetzt exakte, wiederkehrende Textbausteine
   („Erstellt eine(n) neue(n) X" → „Creates a new X", „Liefert Y zurück" → „Returns Y",
   „Eindeutige(r) ID/Bezeichner" → „Unique ID/identifier" …) **ohne jeden Agenten-Aufruf** – kostet null
   LLM-Tokens und schrumpft den Rest-Korpus, den die Agenten noch anfassen müssen, spürbar (Nebeneffekt:
   diese Fälle sind danach auch garantiert konsistent, kein Agent muss sie individuell entscheiden).
   Muster vorher an 15–20 Beispielen aus der Recherche verifizieren, dann breit anwenden, danach `grep`
   auf verbliebene deutsche Signalwörter, um zu sehen, was übrig bleibt.
2. **Straffungs-Lizenz eng begrenzt.** Beim Übersetzen dürfen Agenten **ausschließlich** klar erkennbare
   Änderungshistorie/Datums-Prosa entfernen (z. B. „Hieß bis 2026-07-29 `Adult`…", „seit dem Umbau…").
   Keine sonstige eigenständige Kürzung/Umformulierung – sonst driftet der Ton zwischen parallelen
   Agenten auseinander. Ansonsten treue vollständige Übersetzung.
3. **Glossar-Festlegungen** für mehrdeutige Fachbegriffe: siehe Abschnitt „Glossar".
4. **Glossar-Pflege zentral, nicht durch die Übersetzungs-Agenten.** Jeder Modul-Agent übersetzt nur seine
   zugewiesenen `.cs`-Dateien und meldet neue/uneindeutige Begriffe **in seiner Textantwort** – die
   steuernde Sitzung pflegt `docs/translate.md` danach selbst nach (eine Schreibstelle, keine
   Merge-Konflikte bei parallelen Agenten).
5. **Nur vorhandenen Text übersetzen, keine Docs ergänzen.** Fehlt z. B. nach dem jüngsten
   CancellationToken-Rollout ein `<param name="ct">`-Eintrag, wird er **nicht** neu angelegt – das ist eine
   Übersetzungs-, keine Vervollständigungs-Etappe.
6. **`Pugling.Api.Tests` bleibt reguläre fünfte Etappe** (trotz fehlender CS1591-Pflicht dort) – sonst
   bleibt dauerhaft ein deutscher Rest übrig.
7. **Commit pro Etappe, nur nach expliziter Bestätigung.** Nach jeder erfolgreich reviewten Etappe wird
   nachgefragt, ob committet werden soll – kein automatisches Committen (CLAUDE.md-Konvention).

## Strategie: wenig Tokens pro übersetztem Kommentar

1. **Mechanischer Vorlauf zuerst** (Entscheidung 1) – über alle fünf Projekte in einem Skript-Lauf,
   kostet keine Agenten-Tokens, schrumpft den Rest-Korpus vor dem ersten Agenten-Aufruf.
2. **Glossar zweitens, einmalig** (Abschnitt „Glossar") – die Festlegungen aus Entscheidung 3, plus die
   Adult/Father-Regel aus `CLAUDE.md` übernommen. Jeder Übersetzungs-Agent bekommt dieses kompakte Glossar
   im Prompt mitgegeben statt es jedes Mal neu herzuleiten.
3. **Grobe statt feine Agenten-Granularität** – pro Projekt-Etappe möglichst **ein** `Agent`-Aufruf statt
   vieler kleiner pro Unterordner, um den pro Call wiederholten Overhead (System-Prompt, Tool-Schemas,
   Glossar-Präambel) so oft wie möglich auf einmal abzuschreiben. Nur wenn ein Projekt für einen einzelnen
   Agenten zu groß würde (voraussichtlich nur `Pugling.Api`, 1260 summaries), in 2–3 große Blöcke statt
   8 kleine teilen (z. B. Controllers+Auth / Models+Data / Services+Errors+Exercises+OpenApi). Jeder Agent
   liest seine zugewiesenen Dateien **einmal**, übersetzt darin **nur** die `///`-Kommentare (Code/Signaturen
   unverändert) und wendet seine Edits **im selben Turn** an (kein Round-Trip pro Datei), statt bei jedem
   einzelnen Kommentar zurückzufragen. `opts.effort: "low"` für diese Aufrufe – Übersetzen mit festem
   Glossar ist mechanisches Handwerk, kein tiefes Reasoning, das spart Reasoning-Tokens. Rückmeldung knapp:
   Anzahl übersetzter Blöcke + neue Begriffsvorschläge (Entscheidung 4), keine Erzähl-Prosa.
   Unabhängige Blöcke parallel (mehrere `Agent`-Aufrufe in einer Nachricht), abhängige (Api nach Contracts)
   sequenziell. Kein `Workflow`-Einsatz – einzelne `Agent`-Aufrufe reichen, keine Multi-Agent-Orchestrierung
   angefragt.
4. **Nach jeder Etappe**: `dotnet build` fürs jeweils besitzende Projekt (Hook übernimmt das nach Edits
   ohnehin), zusätzlich `dotnet build Pugling.sln` nach der Contracts-Etappe (Api/Client/Agent.Creator
   hängen daran – das deckt der Datei-Hook nicht ab), stichprobenartig `grep` nach verbliebenen deutschen
   Signalwörtern (z. B. `Gibt|Liefert|Erstellt|Eindeutige`) im übersetzten Bereich.
   Danach kurze Rückfrage, ob committet werden soll (Entscheidung 7).

## Fortschritt

Jede Etappe = eigener Commit-Kandidat, nach Review stoppbar. Reihenfolge ist bewusst so gewählt:
`Contracts` prägt das Glossar, `Api` hängt daran.

| # | Etappe | Umfang | Stand | Belege |
|---|---|---|---|---|
| 0 | Mechanischer Regex-Vorlauf (alle 5 Projekte) | 24 Regeln | **durch** | 320 Ersetzungen in 110 Dateien; Diff enthält ausschließlich `///`-Zeilen (geprüft) |
| 1 | `Pugling.Contracts` – prägt das Glossar | 487 summaries | **durch** | 47 Dateien, 881 Zeilen; `<summary>`/`<param>` je Datei unverändert |
| 2 | `Pugling.Api` **ohne `Models/` + `Data/`** – 4 Blöcke | 868 summaries | **durch** | Controllers/Creator 24 Dat., Supervisor 14, Student 10, Controllers-Wurzel 5, Services 34, Auth 8, Errors 7, Exercises 5, OpenApi 3 |
| 3 | `Pugling.Client` | 181 summaries | **durch** | 11 Dateien, 258 Zeilen, 181→181 `<summary>` |
| 4 | `Pugling.Agent.Creator` | 127 summaries | **durch** | 21 Dateien; deutsche LLM-Prompt-Strings unangetastet |
| 5 | `Pugling.Api.Tests` | 272 summaries | **durch** | 88 Dateien; −7 Zeilen = gekürzte Änderungshistorie in `TestApi.cs` (Entscheidung 2) |
| 6 | Konventionszeile in den `CLAUDE.md` umstellen | 2 Stellen | **durch** | `CLAUDE.md` („XML-Doku auf Englisch") + `Pugling.Contracts/CLAUDE.md`; `Client`/`Agent.Creator` nennen keine Doku-Sprache |
| 7 | **Nachetappe:** `Pugling.Api/Models/` + `Data/` | 398 summaries | **durch** | 18 Modell- + 6 Data-Dateien; `<summary>`-Zahl je Datei unverändert, `Migrations/` (Generat) nicht angefasst |
| 8 | **`//`-Kommentare, alle fünf Projekte** | ~2650 Voll-Zeilen + ~240 am Zeilenende | **durch** | 239 Dateien; zeilengenau gepatcht, Code beweisbar unverändert (siehe unten) |
| 9 | **Meldungstexte der Wächter/Asserts** | 72 Zeilen | **durch** | 13 Test-Dateien; strenges Zeilen-Ersetzen mit zeichengenauem Alt-Abgleich |

**Gesamtstand:** Die Code-Dokumentation des Backends ist **vollständig englisch** – alle 2303 `<summary>`
*und* jeder `//`-Kommentar in `Pugling.Api`, `Pugling.Contracts`, `Pugling.Client`, `Pugling.Agent.Creator`
und `Pugling.Api.Tests`. Verifiziert: `dotnet build Pugling.sln -c Release` grün (0 Warnungen),
`dotnet test Pugling.sln -c Release` **615/615 grün**, `dotnet format Pugling.sln --verify-no-changes` ohne
Befund.

Dazu sind in Etappe 9 die **Meldungstexte der reflexiven Wächter** englisch geworden – das ist der Text,
den man liest, wenn ein Tor rot ist (`SchemaGuardTests` G1–G9, `ConventionGuardTests`,
`EndpointCoverageGuard`, `OwnershipMatrixTests`, `PatchSemanticsTests`, `TagConventionTests`), samt der
Begründungs-Einträge in ihren Ausnahmelisten.

**Deutsch bleibt bewusst** (kein Versäumnis, sondern die Grenze der Umstellung):

- die **Markdown-Doku** (`docs/`, `wiki/`, `CLAUDE.md`-Prosa) – eigener Plan;
- **Produktinhalt in Strings**: Seed-Vokabeln und -Ledger-Texte, der Markdown-Kopf des
  `RemarkExportService` (er landet als deutsches Dokument im Repo), Enum-Werte wie `Gymnasium`;
- **die `Capture(…)`-Titel in `DocsCaptureTests`** – sie *sind* die Überschriften der eingecheckten
  `docs/api-examples/*.md`; sie zu übersetzen hieße, die deutsche Doku umzuschreiben;
- **Testdaten**: Namen, Anmerkungstexte, Vokabelpaare und Bildbeschreibungen, die der Test hin- und
  zurückprüft (`RemarkTests`, `PuglingClientTests`, `MediaStoreTests` …);
- **Laufzeit-Diagnose**: Exception- und Log-Meldungen im Produktivcode (`Program.cs`,
  `ArithmeticProblemGenerator`, `MediaSelector`, `MediaStorage`, `PointKindCurrency`) – die gehen an den
  Betreiber, nicht an den Leser des Codes;
- **deutsche Bezeichner** (`vorher`, `ziel`, `ohneGrant`, Testmethodennamen): Umbenennen ist eine
  Code-Änderung, keine Übersetzung – und hätte ohne Sprachregel für Namen nur halben Wert.

## Etappen 8/9: zeilengenau statt per Agent

Der Hauptteil (Etappen 1–7) lief über Übersetzungs-Agenten, die ganze Dateien anfassten. Für die
`//`-Kommentare war das der falsche Schnitt: sie stehen **zwischen** Code, und genau dort ist ein
versehentlich mitgeänderter Ausdruck teuer und im Diff schwer zu sehen. Stattdessen ein
**zeilengenaues Patch-Werkzeug** (JSONL: Datei, Zeile, neuer Text), das per Zusicherung nur Zeilen
anfasst, die getrimmt mit `//` beginnen; ein Kommentar am Zeilenende darf nur ersetzt werden, wenn der
**Code-Teil vor dem ersten `//` zeichengenau gleich bleibt**. Bei jedem Verstoß bricht es ab, ohne zu
schreiben.

Daraus folgt der Beweis, den ein Diff-Blick nicht liefert: streicht man aus beiden Ständen alle
Voll-Zeilen-Kommentare und schneidet jede übrige Zeile am ersten `//` ab, ist das Ergebnis über alle 239
Dateien **byte-identisch zu `HEAD`** – mit genau einer beabsichtigten Ausnahme, den
Begründungs-Strings in `PuglingDbContext.IntEnumsByDesign`/`UnlimitedByDesign` (Entwickler-Doku, die
zufällig als String im Code steht; kein Test liest sie).

Zwei Kosmetik-Fallstricke bei den Trenner-Zeilen (`// ──── Label ────`): der Strichlauf muss erhalten
bleiben (das Werkzeug ersetzt nur das Label und rechnet die Länge nach), und ein im Label mitgelieferter
Strichlauf verdoppelt ihn. Beides fiel erst beim Nachlesen des Diffs auf, nicht beim Build.

Für die **Meldungs-Strings** (Etappe 9) greift dieselbe Haltung mit einem zweiten Werkzeug: dort steht die
Übersetzung in einer echten Code-Zeile, also verlangt es die **alte Zeile zeichengenau** mit und bricht
sonst ab. Damit ist auch dieser Schritt kein Suchen-und-Ersetzen über Textmuster, das versehentlich einen
Payload-String trifft. Die Abgrenzung ist dabei die eigentliche Arbeit, nicht die Übersetzung: eine
Assert-Meldung darf umgestellt werden, ein Payload, den derselbe Test zurückvergleicht, nicht.

## Fallstricke (beim Umsetzen aufgelaufen)

- **`>` ist in XML-Docs erlaubt, `<` nicht.** Ein „(goal: >= target value)" ging durch, das passende
  „<= " brach den Build mit **CS1570** („badly formed XML"). In Doku-Text also `&lt;`/`&gt;` oder die
  Zeichen `≥`/`≤` – letztere standen im deutschen Original ohnehin. Das Tor greift: `TreatWarningsAsErrors`
  macht CS1570 zum Fehler, ein Übersetzungsfehler dieser Art kommt nie unbemerkt durch.
- **PowerShells `-ne` vergleicht Strings case-insensitiv.** Im Vorlauf-Skript wurden dadurch die rein
  groß-/kleinschreibenden Regeln (`(Paging)` → `(paging)`, `(Default)` → `(default)`) still verworfen –
  Trefferzähler 0, obwohl `grep` die Stellen fand. `-cne` erzwingt den zeichengenauen Vergleich.
- **Übersetzungs-Agenten rutschen in `//`-Kommentare.** Drei von acht Blöcken haben benachbarte
  Inline-Kommentare mitübersetzt (zwei fielen es selbst auf und nahmen es zurück; in `Auth/`, `Errors/`
  und `Controllers/ApiRoutes.cs` blieben 35 Zeilen stehen und wurden zentral zurückgesetzt). Die
  Gegenprobe ist billig und **gehört nach jedem Block** gefahren – sie zählt je Datei die geänderten
  Zeilen, die *kein* `///` tragen:
  `for f in $(git diff --name-only -- '*.cs'); do git diff -U0 -- "$f" | grep '^[+-]' | grep -v '^\(+++\|---\)' | grep -vc '///'; done`
- **Zwei Blöcke erfinden zwei Wörter für denselben Begriff.** „Genauigkeits-Kaskade" wurde einmal
  *accuracy*, einmal *specificity cascade*; „Fachlehrer" einmal *subject-matter teacher*. Darum meldet
  jeder Agent neue Begriffe nur, und die **Entscheidung fällt zentral** (Entscheidung 4) – mit einem
  `grep`-Abgleich der konkurrierenden Varianten am Ende, nicht im Vertrauen auf das Glossar allein.
- **Ein Pfad-Ausschluss muss auf vereinheitlichten Trennern vergleichen.** Die Skip-Liste des Vorlaufs
  („DB-Layer nicht anfassen") lief still ins Leere: `-Root` kam mit `/`, `[IO.Path]::Combine` machte daraus
  einen Prefix mit **gemischten** Trennern, und `StartsWith` gegen die reinen Backslash-Pfade von
  `Get-ChildItem` traf nie. Ergebnis: 52 Ersetzungen im ausgeschlossenen `Models/`+`Data/`, zurückgenommen
  per `git checkout --`. Lehre: nach dem Lauf **prüfen, dass der Ausschluss wirklich leer ist**
  (`git status -- <ausgeschlossener Pfad>`), nicht nur, dass das Skript ohne Fehler lief.
- **Der Test-/Build-Hook baut `$CLAUDE_PROJECT_DIR`, nicht den aktuellen Worktree.** Aus einem Worktree
  heraus prüfen die Hooks also den **Haupt-Worktree** – Meldungen daraus können komplett fremd sein (hier:
  der rote Zwischenstand der parallelen DB-Sitzung). In dieser Lage jede Verifikation **selbst** im Worktree
  fahren (`dotnet build`/`dotnet test` mit explizitem Pfad) und Agenten vorab sagen, dass sie
  Hook-Build-Fehler ignorieren und **niemals** Code „reparieren" sollen.
- **Das Vorlauf-Skript fasst nur Zeilen an, die auf `^\s*///` passen** (Regex-Callback über den ganzen
  Dateitext). Ohne diese Einschränkung träfen Bausteine wie `(partiell)` auch String-Literale. BOM und
  Zeilenenden werden dabei je Datei erhalten (BOM-Erkennung über die ersten drei Bytes).

## CLAUDE.md-Konvention umstellen

In `CLAUDE.md` unter „Konventionen" die Zeile

> **Doku auf Deutsch.** Öffentliche Typen/Members tragen `/// <summary>` (fließt in Swagger).

auf Englisch umstellen (nur diese eine Konventions-Zeile – der Rest von `CLAUDE.md` bleibt auf Deutsch,
das ist eine separate spätere Etappe). Gleiche Prüfung für die kurzen Konventionshinweise in
`backend/Pugling.Contracts/CLAUDE.md`, `backend/Pugling.Client/CLAUDE.md`,
`backend/Pugling.Agent.Creator/CLAUDE.md` (je 14–24 Zeilen).

## Kritische Dateien

- `docs/translate.md` – **diese Seite**: Glossar + Fortschrittstabelle je Etappe.
- `CLAUDE.md` (+ die drei Unterprojekt-`CLAUDE.md`) – Konventionszeile.
- `backend/Pugling.Contracts/**/*.cs`, `backend/Pugling.Api/**/*.cs`,
  `backend/Pugling.Client/**/*.cs`, `backend/Pugling.Agent.Creator/**/*.cs`,
  `backend/Pugling.Api.Tests/**/*.cs` – je Etappe die `///`-Blöcke, Musterbeispiele aus der Recherche:
  `Pugling.Contracts/Common/AdminBaseTypes.cs:3-24`, `Pugling.Api/Models/AdminEntities.cs:12-143`,
  `Pugling.Api/Controllers/Supervisor/AdultsController.cs:49`, `Pugling.Client/CreatorApi.cs:13`.

## Verifikation

- Nach jeder Projekt-Etappe: `dotnet build` (bzw. `dotnet build Pugling.sln` nach Contracts) – muss grün
  bleiben (CS1591 prüft nur Präsenz, nicht Sprache).
- `dotnet format` läuft automatisch über den Hook nach `.cs`-Edits.
- Stichproben-Grep auf deutsche Signalwörter im gerade übersetzten Bereich als Restefund.
- Test-Gate (`dotnet test Pugling.sln -c Release`) greift automatisch am Ende der Antwort/vor Push –
  sollte unverändert grün bleiben, da kein Test auf `///`-Wortlaut prüft.
- Swagger einmal am Ende der letzten Etappe stichprobenartig ansehen (`/swagger`), um zu bestätigen,
  dass keine gemischt-sprachige Ausgabe übrig bleibt, falls doch irgendwo XML-Docs einfließen.
