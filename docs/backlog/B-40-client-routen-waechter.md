---
tags: [typ/story, status/gegrillt, bereich/qualitaet, bereich/tests]
aliases: [Client-Routen-Wächter]
status: gegrillt
prio: P3
art: Aufräumen
quelle: docs/testplan.md#nachmessung-2026-07-31-die-drei-unbeobachteten-flächen
---

# B-40 · Routen aus `Pugling.Client` gegen das OpenAPI-Dokument halten

`Pugling.Client` trägt 120 HTTP-Aufrufstellen, gefahren werden davon 18 Tests – Zeilenabdeckung **61,5 %**,
Zweige 56,9 %. Ein Tippfehler im Routen-String einer nicht gefahrenen Methode ist heute unsichtbar: er bricht
erst zur Laufzeit im KI-Agenten, und zwar als 404 statt als Testfehler.

## User Story

Als **Entwickler**, der nach einem neuen Endpunkt die Client-Methode nachzieht, möchte ich, dass ein
**falscher oder veralteter Routen-String beim Testlauf auffällt**, damit ich ihn nicht erst dann finde, wenn
der KI-Creator mitten in einer Pipeline auf 404 läuft.

## Ist-Stand am Code

- **120 Aufrufstellen** in den drei Fassaden: `CreatorApi.cs` 69, `SupervisorApi.cs` 44, `StudentApi.cs` 7.
  Dazu `PuglingTokenStore.cs:14` mit dem einzigen Literal außerhalb der Fassaden
  (`private const string LoginPath = "api/v1/auth/login"`).
- **Routen entstehen als interpolierte Strings** über eine Klassenkonstante:
  `StudentApi.cs:12` `private const string Root = "api/v1/student"`, Aufruf dann
  `$"{Root}/children/{childId}/vocabulary-progress"`. Query-Parameter hängt `PuglingHttp.Query(…)` an, ist
  also vom Pfad getrennt.
- **109 der 120 Aufrufstellen tragen `{Root}` in derselben Zeile**, die restlichen **11** nicht. Nach dem
  Grillen genauer aufgeschlüsselt: **7** sind statisch auflösbar (fünf über `ItemsPath`, das `"vocabulary"`
  fest verdrahtet, zwei mit dem Literal `"birkenbihl"`), **4** nicht – `CreateExerciseAsync`,
  `GetExerciseAsync`, `UpdateExerciseAsync` und `DeleteExerciseAsync` (`CreatorApi.cs:217` ff.) nehmen
  `authoringRoute` als **Laufzeitwert aus dem Server-Manifest**. Das ist kein Schlamperei-Fall, sondern die
  dokumentierte Projektregel „Übungstypen kommen aus dem Server-Manifest".
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
auf `v1` und darf sich bis zur Publikation **frei** ändern) bricht bis zu 120 Aufrufstellen still.

Zweitens – der Fallstrick, an dem ein naiv gebauter Wächter scheitern *würde*: eine zeilenweise Regex über
`Http.<Verb>Async(...{Root}...)` sieht **11 der 120 Stellen nicht** (9 %). Genau diese Sorte Wächter hat das
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
   263 Actions gegen 120 Aufrufstellen sofort rot und bräuchte eine Ausnahmeliste über mehr als hundert
   Einträge – gegen die ausdrückliche Entscheidung, dass der Client ein **Ausschnitt** ist (`StudentApi` deckt
   nur die Lesesichten ab, `Http` ist als „escape hatch" dokumentiert).
   **Kosten:** ein neuer Endpunkt ohne Client-Methode fällt nicht auf; die Regel „erst Backend, dann eine
   einzeilige Methode" bleibt insoweit Disziplin.
3. **Die 4 manifest-getriebenen Pfade werden über das lebende Typ-Manifest ausgezählt.** Statisch aufgelöst
   werden `Root`, `ExercisePath` und `ItemsPath` (116 Pfade); für `CreateExercise`/`Get`/`Update`/`Delete`
   holt der Wächter alle `authoringRoute`-Segmente aus dem Manifest und prüft je Segment einen Pfad. Damit
   ist ein **neuer Übungstyp automatisch mitgeprüft**, und die Abdeckung ist 120/120 statt 116/120.
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
   (inkl. `PuglingTokenStore`) und dynamisch über die `authoringRoute`-Segmente des Typ-Manifests – und findet
   **mindestens 115** statische Literale; sonst fällt er mit „die Auflösung greift nicht" (Selbstschutz gegen
   vakuum-grün, Muster `ConventionGuardTests`).
2. Jeder gefundene Pfad entspricht – nach Normalisierung aller `{…}`-Platzhalter auf ein einheitliches
   Zeichen – einem Pfad in `/openapi/v1.json`.
3. Eine Abweichung nennt in der Meldung **Datei, Methode und den nicht gefundenen Pfad**. „values differ"
   genügt nicht (Meldungsqualität, siehe [testplan.md](../testplan.md)).
4. Jedes Segment aus dem Typ-Manifest ergibt einen gültigen Pfad; ein neuer Übungstyp braucht dafür **keine**
   Änderung am Wächter.
5. **Gegenprobe gefahren und protokolliert:** (a) ein Segment in einer Client-Methode verdreht → rot mit
   Methodennamen; (b) einen der beiden Pfad-Helfer umbenennen → rot an der Untergrenze, nicht still grün.
6. `dotnet test Pugling.sln -c Release` bleibt sonst grün; **kein Produktivcode geändert**.

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
