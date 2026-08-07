---
tags: [typ/story, status/abgenommen, bereich/qualitaet, bereich/tests]
aliases: [Client-Routen-Wächter]
status: abgenommen
prio: P3
art: Aufräumen
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/testplan.md#nachmessung-2026-07-31-die-drei-unbeobachteten-flächen
nachgeschaut: "2026-08-07"
---

# B-40 · Routen aus `Pugling.Client` gegen das OpenAPI-Dokument halten

`Pugling.Client` trägt **138** HTTP-Aufrufstellen, gefahren werden davon 18 Tests – Zeilenabdeckung
**61,5 %**, Zweige 56,9 %. Ein Tippfehler im Routen-String einer nicht gefahrenen Methode ist heute
unsichtbar: er bricht erst zur Laufzeit im KI-Agenten, und zwar als 404 statt als Testfehler.

> **Zahlenkorrektur beim Bauen (2026-08-01).** Diese Story hat durchgehend mit **120** Aufrufstellen
> gerechnet; nachgezählt sind es **138** (+ das Login-Literal = 139 Pfade). Die alte Zahl steht unten nur
> noch dort, wo sie eine damalige Überlegung trägt – überall sonst ist sie ersetzt. Die Korrektur ist nicht
> kosmetisch: die daraus abgeleitete Untergrenze von „mindestens 115" hätte das Tor **stumpf** gemacht
> (siehe Verifikation, Gegenprobe b).

## User Story

Als **Entwickler**, der nach einem neuen Endpunkt die Client-Methode nachzieht, möchte ich, dass ein
**falscher oder veralteter Routen-String beim Testlauf auffällt**, damit ich ihn nicht erst dann finde, wenn
der KI-Creator mitten in einer Pipeline auf 404 läuft.

## Ist-Stand am Code

- **138 Aufrufstellen** in den drei Fassaden: `CreatorApi.cs` 84, `SupervisorApi.cs` 47, `StudentApi.cs` 7
  (gezählt über `Http.(Get|Post|Patch|Put|Send|PostContent)Async`, beim Bauen am 2026-08-01 unabhängig
  vom Reviewer nachgezählt). Dazu `PuglingTokenStore.cs:14` mit dem einzigen Literal außerhalb der Fassaden
  (`private const string LoginPath = "api/v1/auth/login"`) – zusammen **139 Pfade**.
- **Routen entstehen als interpolierte Strings** über eine Klassenkonstante:
  `StudentApi.cs:12` `private const string Root = "api/v1/student"`, Aufruf dann
  `$"{Root}/children/{childId}/vocabulary-progress"`. Query-Parameter hängt `PuglingHttp.Query(…)` an, ist
  also vom Pfad getrennt.
- **Die meisten Aufrufstellen tragen `{Root}` in derselben Zeile**, eine Handvoll nicht. Aufgeschlüsselt:
  statisch auflösbar sind sie über `ItemsPath` (das `"vocabulary"` fest verdrahtet) bzw. das Literal
  `"birkenbihl"`; **4** sind es nicht – `CreateExerciseAsync`, `GetExerciseAsync`, `UpdateExerciseAsync` und
  `DeleteExerciseAsync` (`CreatorApi.cs:217` ff.) nehmen `authoringRoute` als **Laufzeitwert aus dem
  Server-Manifest**. Das ist kein Schlamperei-Fall, sondern die dokumentierte Projektregel „Übungstypen
  kommen aus dem Server-Manifest".
- **Beim Bauen dazugekommen** (die Story kannte es nicht): `CreateExerciseAsync` (`CreatorApi.cs:219`)
  übergibt den Pfad-Helfer **nackt** als Argument – `Http.PostAsync(ExercisePath(…), …)`, nicht in einem
  `$"…"`. Ein Scanner, der nur interpolierte Zeichenketten liest, verliert genau diese eine Aufrufstelle,
  und zwar lautlos.
- **Pfad-Helfer:** `ExercisePath` (`CreatorApi.cs:488`), `ItemsPath` (`CreatorApi.cs:491`).
- **Das Muster für einen solchen Wächter ist etabliert.** `ConventionGuardTests` scannt für zwei seiner fünf
  Regeln den **Quelltext** statt der Reflexion (`ConventionGuardTests.cs:33`, Begründung im Kommentar:
  „die Konvention betrifft den Methoden*körper*, den Reflexion nicht sieht"), findet die Wurzel über
  `RepoRoot()` (`:243`) und trägt **je Test eine Untergrenze** gegen falsch-grün.
- **Das OpenAPI-Dokument liegt im Test bereit**: `ErrorCodeTests.cs:155` holt es mit
  `client.GetFromJsonAsync<JsonElement>("/openapi/v1.json")`.
- **Kein Test prüft heute Client-Routen.** `PuglingClientTests` (18 Fälle) fährt fachliche Abläufe; welche
  Methoden dabei nicht vorkommen, sagt niemand.

## Die echte Lücke

Nicht „der Client ist zu wenig getestet" – 18 Abläufe über echte HTTP-Aufrufe sind für eine Hilfsbibliothek
ordentlich. Die Lücke ist schmaler und mechanisch: **es gibt keinen Abgleich zwischen den Routen, die der
Client schreibt, und denen, die die API anbietet.** Jede Umbenennung eines Segments im Backend (die API steht
auf `v1` und darf sich bis zur Publikation **frei** ändern) bricht bis zu 138 Aufrufstellen still.

Zweitens – der Fallstrick, an dem ein naiv gebauter Wächter scheitern *würde*: eine zeilenweise Regex über
`Http.<Verb>Async(...{Root}...)` sieht rund ein Dutzend Stellen nicht. Genau diese Sorte Wächter hat das
Projekt schon einmal verworfen, weil ihre Kennlinie kein Tor trug
([testaudit-nacharbeit-plan.md](../testaudit-nacharbeit-plan.md), „Kein Wächter – die Kennlinie trägt kein
Tor"). Hier ist der Ausweg aber vorhanden und billig, siehe Entscheidung 3.

Die Namensfrage der Platzhalter löst sich von selbst: der Client schreibt `{childId}`, die Route vielleicht
`{id}`. Werden beide Seiten vor dem Vergleich auf `{}` normalisiert, ist der Vergleich gegen Umbenennungen von
Parameternamen **absichtlich** blind und gegen falsche *Segmente* scharf – und nur die sind der Fehler.

## Offene Punkte

Alle im Grillen vom 2026-07-31 entschieden, siehe unten.

1. ~~Nur Tippfehler fangen oder Vollständigkeit fordern?~~ → Entscheidung 2
2. ~~Wo lebt der Wächter?~~ → Entscheidung 4
3. ~~Die 11 Helfer-/Umbruch-Fälle: auflösen oder Client umbauen?~~ → Entscheidung 3
4. ~~Ist der Agent-Ausschnitt (`pugling-creator`) mitgemeint?~~ → Entscheidung 5

## Entscheidungen

1. **Der Wächter liest das OpenAPI-Dokument lebend aus dem Testhost** (`/openapi/v1.json`), nicht aus dem
   eingecheckten Artefakt, das [B-42](B-42-openapi-typen-generieren.md) anlegt. Begründung: ein Wächter, der
   gegen ein Abbild prüft, kann grün bleiben, während die echte API abweicht – das Abbild ist nur so frisch
   wie der letzte Lauf. Außerdem hängt B-40 damit an nichts und ist sofort baubar.
   **Kosten:** das Dokument erscheint an zwei Stellen im Repo (lebend hier, eingecheckt in B-42).
2. **Nur die Richtung Client → API.** Die Gegenrichtung („jeder Endpunkt hat eine Client-Methode") wäre bei
   263 Actions gegen 138 Aufrufstellen sofort rot und bräuchte eine Ausnahmeliste über mehr als hundert
   Einträge – gegen die ausdrückliche Entscheidung, dass der Client ein **Ausschnitt** ist (`StudentApi` deckt
   nur die Lesesichten ab, `Http` ist als „escape hatch" dokumentiert).
   **Kosten:** ein neuer Endpunkt ohne Client-Methode fällt nicht auf; die Regel „erst Backend, dann eine
   einzeilige Methode" bleibt insoweit Disziplin.
3. **Die 4 manifest-getriebenen Pfade werden über das lebende Typ-Manifest ausgezählt.** Statisch aufgelöst
   werden `Root`, `ExercisePath` und `ItemsPath` (135 Pfade); für `CreateExercise`/`Get`/`Update`/`Delete`
   holt der Wächter alle `authoringRoute`-Segmente aus dem Manifest und prüft je Segment einen Pfad. Damit
   ist ein **neuer Übungstyp automatisch mitgeprüft**, und die Abdeckung ist 139/139 statt 135/139
   (gebaut: 12 Typsegmente × 4 Aufrufstellen = 48 zusätzlich geprüfte Pfade).
   **Kosten:** der Wächter kennt drei Interna des Clients (`Root` und die zwei Helfer) und bricht, wenn jemand
   einen vierten Helfer einführt – deshalb die Untergrenze, damit er dabei **laut** scheitert statt still
   weniger zu prüfen.
4. **Eigene Testdatei** (`ClientRouteGuardTests`), nicht in `ConventionGuardTests`. Begründung: die urteilt
   über `Pugling.Api/Controllers`; ein Wächter über ein anderes Projekt wäre eine zweite Zuständigkeit in
   einer Datei. **Kosten:** eine Datei mehr, `RepoRoot()` wird dupliziert oder gemeinsam genutzt.
5. **`pugling-creator` (Agent, Zeilen 67,0 %) ist nicht im Umfang.** Dort fehlen fachliche Abläufe, keine
   Routen. **Kosten:** die Agent-Abdeckung bleibt, wie sie ist – bewusst, nicht übersehen.
6. **Reihenfolge:** B-40 wird als **zweite** der vier Test-Stories gebaut, nach
   [B-41](B-41-produktions-startup-smoke.md). Begründung: kleinste Story, ein Ort, kein Produktivcode.

## Akzeptanzkriterien

1. Ein Test sammelt die Routen aus `Pugling.Client` – statisch über `Root`, `ExercisePath`, `ItemsPath`
   (inkl. `PuglingTokenStore`) und dynamisch über die `authoringRoute`-Segmente des Typ-Manifests – und
   **löst je HTTP-Aufrufstelle mindestens einen Pfad auf**; sonst fällt er mit „n Aufrufstellen sind still
   ungeprüft" (Selbstschutz gegen vakuum-grün, Muster `ConventionGuardTests`).
   ~~und findet **mindestens 115** statische Literale~~ — **beim Bauen ersetzt.** Eine feste Zahl ist nur an
   dem Tag dicht, an dem sie geschrieben wird, und jede neue Client-Methode erkauft danach ein Stück
   Blindheit. Konkret: mit 115 wäre Gegenprobe (b) **grün geblieben** (129 aufgelöste Pfade > 115) — die
   Story hätte ihr eigenes Tor entschärft. Der Wächter zählt die Aufrufstellen jetzt selbst
   (`Http.(Get|Post|Patch|Put|Send|PostContent)Async`, zweites und bewusst *dummes* Maß, unabhängig vom
   Auflöser) und fordert `Routen ≥ Aufrufstellen`. Das bleibt dicht, während der Client wächst. Die
   verbliebene feste Zahl (`MinimumCallSites = 130`) sichert nur noch den Zähler selbst gegen Vakuum.
2. Jeder gefundene Pfad entspricht – nach Normalisierung aller `{…}`-Platzhalter auf ein einheitliches
   Zeichen – einem Pfad in `/openapi/v1.json`.
3. Eine Abweichung nennt in der Meldung **Datei, Methode und den nicht gefundenen Pfad**. „values differ"
   genügt nicht (Meldungsqualität, siehe [testplan.md](../testplan.md)).
4. Jedes Segment aus dem Typ-Manifest ergibt einen gültigen Pfad; ein neuer Übungstyp braucht dafür **keine**
   Änderung am Wächter.
5. **Gegenprobe gefahren und protokolliert:** (a) ein Segment in einer Client-Methode verdreht → rot mit
   Methodennamen; (b) einen der beiden Pfad-Helfer umbenennen → rot an der Untergrenze, nicht still grün.
6. `dotnet test Pugling.sln -c Release` bleibt sonst grün; **kein Produktivcode geändert**.
7. **Nachgetragen beim Review** (die drei Formen, an denen der Scanner still weniger prüft, statt rot zu
   werden): ein aus zwei Literalen mit `+` zusammengesetzter Pfad, ein Verbatim-/Raw-String mit Route und
   ein Bindungszyklus durch vertauschte Helfer-Argumente. Je eine Gegenprobe.

## Schätzung

**XS**, `wo: backend`, `migration: nein`, `vertragsbruch: nein` — eine neue Testdatei, **kein Produktivcode**
(AK 6). Das Paket nennt E2 die risikoärmste Etappe: ein Ort, ein Host.

**Angriffsplan** (Backend zuerst gilt trivial – es gibt nur Backend):

1. `ClientRouteGuardTests` anlegen, `IClassFixture<PuglingWebAppFactory>`. Kein Login nötig:
   `/openapi/v1.json` ist anonym (belegt: `ErrorCodeTests.cs:155` ruft es mit einem nackten
   `factory.CreateClient()`), und das Typ-Manifest steht als `ExerciseTypeRegistry` im DI-Container
   (`ExerciseTypeManifestTests.cs:19`) – also **ohne** HTTP-Aufruf und damit ohne Wirkung auf
   `FullRunTouchedActions` (Naht 1: die Konstante bleibt bei 263, E1 besitzt sie).
2. Quell-Scanner über `backend/Pugling.Client/*.cs`: interpolierte Zeichenketten einsammeln, Löcher
   auflösen (`Root`, die zwei Pfad-Helfer), Rest auf `{}` normalisieren.
3. Die vier manifest-getriebenen Pfade je `AuthoringRoute`-Segment vervielfachen (Entscheidung 3).
4. Abgleich gegen die Pfade des Dokuments; Untergrenze als Selbstschutz.

**Risiken:**

- **Der Scanner ist ein halber Parser.** Ein `$"…"` mit einer verschachtelten Zeichenkette im Loch
  (`ExercisePath(subjectId, chapterId, "birkenbihl")`, `CreatorApi.cs:270`) bricht jede zeilenweise Regex.
  Gegenmittel: ein Zeichen-Scanner mit Klammertiefe statt einer Regex – und die Untergrenze, die zuschlägt,
  wenn er weniger findet als erwartet.
- **Vakuum-Grün** ist die eigentliche Gefahr dieser Bauform (die Regel steht im Klassenkommentar von
  `ConventionGuardTests`): ein Wächter, der nichts findet, ist grün. Deshalb AK 1 und die Gegenprobe aus
  AK 5(b).

**Testweg:** der Wächter *ist* der Test. Abnahme über die zwei Gegenproben aus AK 5 (je einmal rot gesehen)
plus `dotnet test Pugling.sln -c Release` grün.

## Verifikation

Gebaut am **2026-08-01**. Ein Ort: [ClientRouteGuardTests.cs](../../backend/Pugling.Api.Tests/ClientRouteGuardTests.cs),
zwei Tests, **kein Produktivcode** (`git status` zeigt außer der neuen Testdatei nur Doku).

| Beleg | Ergebnis |
| --- | --- |
| `dotnet test Pugling.sln -c Release` | **622/622 grün**, 48 s (vorher 620) |
| Endpunkt-Abdeckung | **263/263, 0 offen** – unverändert, `FullRunTouchedActions` **nicht** angefasst (Naht 1) |
| `dotnet format Pugling.sln --verify-no-changes` | sauber |
| Geprüfte Pfade | 135 statisch + 4 manifest-getrieben × **12** Typsegmente = **183** Vergleiche |
| Laufzeit der Klasse | ~10 s inkl. eigenem Host; der Host ist einer von 70 `IClassFixture`-Hosts, kein neuer Kostenblock |

### Akzeptanzkriterien

| AK | Beleg |
| --- | --- |
| 1 Sammeln + Selbstschutz | `Routen ≥ Aufrufstellen` (139 ≥ 138), plus `MinimumCallSites = 130` für den Zähler selbst. Fassung revidiert, Begründung oben. |
| 2 Jeder Pfad im Dokument | grün über alle 183 Vergleiche; Normalisierung beidseitig auf `{}` |
| 3 Meldungsqualität | `CreatorApi.cs:52 ListChaptersAsync → /api/v1/creator/subjects/{}/chapterz` – Datei, Zeile, Methode, Pfad |
| 4 Neuer Übungstyp ohne Änderung am Wächter | eigener Test `Jedes_Typ_Segment_Des_Manifests_Ergibt_Einen_Gueltigen_Pfad`; Gegenprobe (d) |
| 5 Gegenproben | acht statt zwei, siehe unten |
| 6 Sonst grün, kein Produktivcode | 622/622; nur `ClientRouteGuardTests.cs` neu |
| 7 Die drei Scanner-Blindstellen | Gegenproben (e), (f), (g) |

### Gegenproben – jede einmal rot gesehen

| # | Eingriff | Reaktion |
| --- | --- | --- |
| a | `chapters` → `chapterz` in `ListChaptersAsync` | rot, 1 Treffer mit Datei:Zeile:Methode |
| b | `ExercisePath` umbenannt | rot: „138 Aufrufstellen, aber nur 130 Routen – **8 still ungeprüft**" |
| b2 | `ItemsPath` umbenannt | rot: 136 Routen, 2 still ungeprüft |
| c | Vervielfachung entschärft | rot mit genau den **4** manifest-getriebenen Aufrufstellen – belegt zugleich, dass die nackte Helfer-Übergabe in `CreateExerciseAsync` gefunden wird |
| d | Manifest-Segment `haikus` erfunden | **beide** Tests rot |
| e | Pfad aus zwei Literalen (`$"…" + "/points"`) | rot: „path composed from two string literals" |
| f | Verbatim-Interpolation `$@"…"` | rot: „verbatim or raw string literal carrying a route" |
| g | Vertauschte Helfer-Argumente (Bindungszyklus) | läuft durch; **ohne** die Tiefenbremse: `Catastrophic failure: Test process crashed with exit code -1073741571` (`0xC00000FD`, Stack Overflow) |

Die Gegenproben **e**, **f** und **g** kommen aus dem `pugling-reviewer`-Durchgang und haben je einen echten
Mangel der ersten Fassung freigelegt – **e** und **f** waren still grün, **g** hätte bei genau der
Fehlerklasse, für die der Wächter gebaut ist, den kompletten Testlauf ohne Ursachenmeldung abgeschossen.

### Was der Review sonst geändert hat

- **Feste Untergrenze → relatives Maß** (siehe AK 1). Der schwerste Befund: die Zahl aus der Story hätte
  Gegenprobe (b) grün bleiben lassen.
- **`api/v1/` → `api/v`** im Präfixfilter. CLAUDE.md plant den Bruch als parallele `v2`; auf `v1` verankert
  wäre die erste v2-Client-Methode lautlos aus der Prüfung gefallen.
- **`obj/`/`bin/` ausgeschlossen**; die Datei-Untergrenze hing sonst am Build-Zustand statt am Client.
- **`RepoRoot()` geteilt** statt kopiert – `ApiSurface.RepoRoot()` ist bereits `public` und wird vom
  `EndpointCoverageGuard` genutzt. Damit ist die in Entscheidung 4 offen gelassene Frage entschieden.

### Bewusst nicht gemacht

`Jedes_Typ_Segment_…` verdrahtet die Autorenpfad-Form (`…/subjects/{}/chapters/{}/{segment}`) von Hand,
statt sie aus dem schon aufgelösten dynamischen Client-Pfad zu ziehen. Das ist Absicht: der Test prüft
**Manifest ↔ API**. Leitete er die Form aus dem Client ab, würde ein Client-Fehler ihn stillschweigend
mit-entschärfen – zwei Prüfungen an derselben Quelle sind eine.

## Verlauf

- **2026-07-31** — angelegt (Quelle: Nachmessung der Test-Abdeckung, [testplan.md](../testplan.md)).
- **2026-07-31** — ausformuliert: 120 Aufrufstellen belegt, Quell-Scan-Muster und `/openapi/v1.json` als
  vorhandene Bausteine gefunden; die eigentliche Falle sind die **11** Stellen ohne `{Root}` in der Zeile.
- **2026-07-31** — gegrillt: sechs Entscheidungen. Der Kern kam beim Nachsehen heraus – von den 11 Stellen
  sind **4 grundsätzlich nicht statisch** (Typ-Segment aus dem Manifest), und genau dafür gibt es mit dem
  Manifest eine mechanische Antwort statt eines protokollierten Übersprungs.
- **2026-08-01** — ins [Testabdeckungs-Paket](../testabdeckung-plan.md) als **E2** aufgenommen, inhaltlich
  unverändert. Zwei Zusätze aus der Dev-Runde: Die Reihenfolge nach E1 ist **nur** der geteilten Konstante
  `EndpointCoverageGuard.FullRunTouchedActions` geschuldet, nicht einer fachlichen Abhängigkeit – und
  Entscheidung 1 (lebend lesen) bleibt auch nach E3 richtig, wo dasselbe Dokument eingecheckt wird.
  Positivbefund: `Program.cs:129` setzt `SubstituteApiVersionInUrl = true`, das Dokument trägt also
  `/api/v1/…`; die naive Falle „Client schreibt `v1`, Doku schreibt `{version}`" gibt es nicht.
- **2026-08-01** — geschätzt (XS) und in Arbeit: Angriffsplan und Risiken oben. Beim Planen kam heraus, dass
  der Wächter **gar keinen HTTP-Aufruf** braucht – das Manifest steht als `ExerciseTypeRegistry` im
  Container, `/openapi/v1.json` ist anonym. Damit rührt E2 die Konstante aus Naht 1 nicht an.
- **2026-08-01** — **abgenommen**, Commit `b229b1c`. 622/622 grün, Abdeckung unverändert 263/263, acht
  Gegenproben je rot gesehen. Zwei Story-Aussagen sind beim Bauen gefallen: die **Zahl** (120 → 138
  Aufrufstellen, und die daraus abgeleitete Untergrenze hätte das Tor stumpf gemacht) und die **Bauform**
  (`CreateExerciseAsync` übergibt den Pfad-Helfer nackt, nicht in einem `$"…"`). Der `pugling-reviewer` hat
  danach drei Blindstellen gefunden, von denen zwei still grün waren und eine den Testlauf mit einem
  Stack Overflow abgeschossen hätte – alle drei geschlossen und mit eigener Gegenprobe belegt.
- **2026-08-07** — Nachschau (Nachtlauf): geprüft, ob die relative Zusicherung (`MinimumCallSites = 130`)
  nach dem B-106-Umbau von `CreateApi.cs` weiterhin hält — hält (aktuelle Aufrufstellenzahl 136, war 138
  bei Abnahme, weiterhin ≥130). Kein Fund.
