---
tags: [typ/story, status/abgenommen, bereich/qualitaet, bereich/api]
aliases: [ProblemDetails required extensions]
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

# B-56 · `ProblemDetails` fordert im Schema ein Feld, das es nicht beschreibt

Im OpenAPI-Dokument trägt das Schema `ProblemDetails` genau **ein** Pflichtfeld – `extensions` –, und dieses
Feld steht **nicht** unter `properties` (dort liegen `type`, `title`, `status`, `detail`, `instance`, `code`).
`ProblemDetails` ist der Fehlertyp **jedes** Endpunkts – das Vertragsdokument ist an dieser einen Stelle
also in sich widersprüchlich.

## User Story

Als Entwickler, der das eingecheckte `docs/openapi/v1.json` als Vertrag liest (Swagger/Scalar-UI, ein
künftiger strenger Generator/Linter), möchte ich, dass jedes als `required` markierte Feld eines Schemas
tatsächlich unter `properties` beschrieben ist, damit das Dokument nicht ein Feld verlangt, das es selbst
nicht kennt.

## Ist-Stand am Code

- Der eigene Schema-Transformer berechnet `required` **neu** statt den generatorseitigen Wert zu
  übernehmen, weil der .NET-OpenAPI-Generator sonst jeden Record-Konstruktor-Parameter als `required`
  markiert – auch nullable/optionale
  ([Program.cs:309-335](../../backend/Pugling.Api/Program.cs)). Er ruft dafür
  `EnumSchemaHelp.RequiredJsonPropertyNames(context.JsonTypeInfo)`
  ([Program.cs:333-334](../../backend/Pugling.Api/Program.cs)).
- `RequiredJsonPropertyNames` iteriert **alle** `typeInfo.Properties` und nimmt jede mit `IsRequired` oder
  einer nicht-nullablen Nullability auf
  ([EnumSchemaHelp.cs:26-36](../../backend/Pugling.Api/Errors/EnumSchemaHelp.cs)) – ohne eine
  JSON-Extension-Data-Property (`[JsonExtensionData]`) auszunehmen.
- `Microsoft.AspNetCore.Mvc.ProblemDetails` trägt genau so eine Property:
  `IDictionary<string, object?> Extensions { get; }` – ein get-only-Referenztyp, also nicht-nullabel, also
  von `IsNonNullable` als „required" eingestuft. Der .NET-Generator selbst schreibt eine
  Extension-Data-Property **nicht** in `schema.Properties` (das ist ihr Sinn: sie ist kein benanntes
  Feld, sondern der Auffangbehälter für beliebige zusätzliche Schlüssel) – das erzeugt genau die
  Lücke: in `required`, aber nicht in `properties`.
- Nachgemessen (Skript gegen `docs/openapi/v1.json`, alle Schemas verglichen): **`ProblemDetails` ist das
  einzige** Schema im gesamten Dokument mit einem `required`-Namen ohne zugehörige `properties`-Property.
  Der ursprüngliche Verdacht „betrifft es alle Nicht-Record-Typen" trifft also nicht zu – es ist speziell
  die `Extensions`-Property, die einzige Extension-Data-Property, die irgendwo im Vertrag auftaucht.
- Das benachbarte `code`-Feld (im Befund ursprünglich mitverdächtigt) ist **nicht** betroffen und läuft
  über einen ganz anderen Mechanismus: zur Laufzeit landet `code` in
  `problem.Extensions["code"]` ([ProblemDetailsStamping.cs:29](../../backend/Pugling.Api/Errors/ProblemDetailsStamping.cs),
  `:40`) – ein separater **Dokument**-Transformer schreibt danach von Hand eine typisierte `code`-Property
  mit vollständigem Enum in `schema.Properties`
  ([Program.cs:389-408](../../backend/Pugling.Api/Program.cs)). Dieser Transformer läuft nach der
  `required`-Berechnung und rührt sie nicht an – `code` bleibt korrekt optional (es steht nicht in
  `required`) und ist vollständig beschrieben. Derselbe Transformer zielt zusätzlich auf ein Schema
  `HttpValidationProblemDetails`, das im aktuellen Dokument gar nicht existiert (`TryGetValue` schlägt
  fehl und der Zweig wird übersprungen) – folgenlos, aber ein Hinweis, dass Validierungsfehler heute
  offenbar dasselbe `ProblemDetails`-Schema teilen statt ein eigenes zu erzeugen.
- Auswirkung auf den generierten Frontend-Vertrag geprüft: `frontend/src/lib/contract.ts` (erzeugt von
  `openapi-typescript`, siehe [B-42](B-42-openapi-typen-generieren.md)) listet `ProblemDetails` mit **allen**
  Feldern optional (`type?`, `title?`, …, `code?`) und **ohne** ein `extensions`-Feld überhaupt zu
  erzeugen – der Generator ignoriert einen `required`-Namen stillschweigend, wenn keine passende Property
  existiert. Der Frontend-Vertrag ist also **nicht** kaputt; das Dokument ist nur in sich unwahr.
- `ContractDocumentTests.Vertragsdokument_BeschreibtDieLeitungWahrheitsgemaess`
  ([ContractDocumentTests.cs:65-119](../../backend/Pugling.Api.Tests/ContractDocumentTests.cs)) prüft
  bereits eine verwandte Lüge – „required trotz Default" (Punkt 3) – aber nicht diese: „required ohne
  passende Property" hat noch keine Assertion.

## Die echte Lücke

Schmaler als vermutet: Es betrifft **nur** `ProblemDetails`, **nur** die eine geerbte `Extensions`-Property,
und **nur** die selbstgebaute `required`-Berechnung in `EnumSchemaHelp` – nicht den generatorseitigen
Properties-Aufbau (der macht es an dieser Stelle bereits richtig) und nicht `code`/`type`/`title` (die sind
korrekt beschrieben). Es beißt **heute** auch niemanden: `openapi-typescript` ignoriert den verwaisten
`required`-Eintrag klaglos, der Frontend-Vertrag bleibt unverändert. Der Schaden ist, dass das eingecheckte,
byte-diffte Vertragsdokument – „das Produkt" laut CLAUDE.md – an genau einer Stelle etwas behauptet, das
nicht stimmt: Swagger/Scalar würden `extensions*` als Pflichtfeld anzeigen, das nirgends erklärt ist, und ein
künftiger strengerer Konsument (ein anderer Generator, ein Schema-Validator) könnte daran gültige Antworten
ablehnen, die niemals ein `extensions`-Feld senden.

## Offene Punkte

- ~~Betrifft es nur `ProblemDetails` oder alle Nicht-Record-Typen?~~ → siehe Entscheidung 1 (gemessen: nur
  `ProblemDetails`/nur Extension-Data-Properties).
- ~~Ist `extensions` überhaupt gewollt im Vertrag?~~ → siehe Entscheidung 2 (nein – die einzelnen Schlüssel
  wie `code` sind der Vertrag, nicht der rohe Sammelbehälter).
- ~~Beißt es wirklich, oder ignoriert `openapi-typescript` ein `required` ohne Property – und läuft das vor
  oder nach B-42 Schritt 2?~~ → siehe Entscheidung 3 (gemessen: ignoriert es, unabhängig von B-42, keine
  Reihenfolge-Abhängigkeit).

## Entscheidungen

1. **Der Fix greift generisch an der Ursache (Extension-Data-Properties), nicht speziell am Namen
   `ProblemDetails`.** `EnumSchemaHelp.RequiredJsonPropertyNames` schließt jede Property mit
   `JsonPropertyInfo.IsExtensionData == true` aus der Berechnung aus. Begründung: Der Fehler ist kein
   `ProblemDetails`-Spezifikum, sondern eine Lücke im Vorgehen „required aus Nullability ableiten" – das
   trifft strukturell jede künftige DTO mit `[JsonExtensionData]`. Kosten: eine zusätzliche Zeile in einer
   gemeinsam genutzten Hilfsfunktion, die auch andere Schemas berechnet – dafür sichert die neue Assertion
   aus Entscheidung 4 ab, dass nichts anderes kaputtgeht.
2. **`extensions` wird nicht nachträglich als Property dokumentiert – es bleibt vollständig abwesend.**
   Begründung: `Extensions` ist ASP.NET Cores interner Auffangbehälter; der eigentliche Vertrag sind die
   einzelnen, benannten Schlüssel (`code`, `traceId`), die der Dokument-Transformer gezielt als typisierte
   Properties nachträgt (Muster: `code` in `Program.cs:389-408`). Ein rohes `"extensions": {"type":
   "object"}` wäre keine Information für einen Konsumenten. Kosten: keine – das ist der Status quo, nur
   ohne den falschen `required`-Eintrag.
3. **Kein Zusammenhang mit B-42 Schritt 2, keine Reihenfolge-Abhängigkeit.** Gemessen: `openapi-typescript`
   erzeugt für `ProblemDetails` bereits heute alle Felder optional und gar kein `extensions`-Feld – der
   verwaiste `required`-Eintrag hat keine Wirkung auf den generierten Frontend-Vertrag. Diese Story kann
   unabhängig und in beliebiger Reihenfolge zu B-42 gebaut werden. Kosten: keine.
4. **Die neue Testsicherung ist generisch** (jedes Schema, nicht nur `ProblemDetails`): eine Erweiterung von
   `ContractDocumentTests.Vertragsdokument_BeschreibtDieLeitungWahrheitsgemaess` um eine fünfte Aussage –
   kein `required`-Name ohne passende `properties`-Property, über **alle** Schemas. Begründung: dieselbe
   Klasse Lüge wie die schon vorhandene Prüfung 3 („required trotz Default"), also derselbe Testort statt
   einer neuen Datei. Kosten: ein zusätzlicher Assert-Block in einer bestehenden, bereits vertrauten
   Testdatei.

## Akzeptanzkriterien

1. Im regenerierten `docs/openapi/v1.json` trägt das Schema `ProblemDetails` **kein** `required` mehr, das
   `extensions` nennt (der Schlüssel `required` fällt für `ProblemDetails` ganz weg, sofern kein anderes
   Feld ihn füllt).
2. `properties.extensions` wird **nicht** neu ergänzt – das Feld bleibt so unsichtbar wie heute, nur ohne
   den falschen Pflicht-Eintrag.
3. `code`, `type`, `title`, `status`, `detail`, `instance` bleiben unverändert beschrieben (Regressionsfreiheit
   gegenüber dem heutigen Dokument).
4. Eine neue, generische Assertion in `ContractDocumentTests` schlägt fehl, sobald irgendein Schema im
   Dokument einen `required`-Namen ohne zugehörige `properties`-Property trägt – nicht nur hartkodiert für
   `ProblemDetails`.
5. Der bestehende Test-/Doku-Regenerierungs-Lauf (`ContractDocumentTests`) bleibt grün und das neu
   geschriebene `docs/openapi/v1.json` zeigt im Diff **ausschließlich** die erwartete `required`-Änderung.

## Schätzung

**Größe: S** – ein Filter in einer bestehenden, gemeinsam genutzten Hilfsfunktion
(`EnumSchemaHelp.RequiredJsonPropertyNames`), eine automatisch regenerierte, eingecheckte Vertragsdatei und
ein neuer Assert-Block in einer bereits existierenden Testklasse; kein neues DTO, kein neuer `ApiErrors`-Code,
keine neue Datei. Vergleichbar mit dem S-Anker B-01 (Wert aus dem Test-Pfad ziehen), kleiner als die
M-Anker B-03/B-10.

- **wo**: backend – reine Vertragsdokument-/Generator-Korrektur, kein Frontend-Anteil (der generierte
  `contract.ts` ändert sich laut Messung nicht, siehe „Ist-Stand am Code").
- **migration**: nein – keine Schemaänderung an einer EF-Entity, nur an der generierten OpenAPI-Beschreibung.
- **vertragsbruch**: nein – kein Feld wird umbenannt, entfernt oder neu Pflicht für einen Konsumenten; die
  Korrektur macht `required` nur **kleiner**, nie größer, und der einzige heute existierende Generator
  (`openapi-typescript`) reagiert nachweislich nicht auf den Fehlbestand.
- **Risiken**: `JsonPropertyInfo.IsExtensionData` muss sich auf `net10.0` so verhalten wie erwartet (die
  Extension-Data-Property als solche markieren) – abgesichert durch einen Build- und Testlauf, der das
  regenerierte Dokument tatsächlich zeigt statt es zu vermuten. Restrisiko: die neue generische Assertion
  könnte einen zweiten, heute unbekannten Fall in einem *künftig* hinzukommenden Schema aufdecken – das wäre
  kein Rückschritt, sondern genau der Zweck der Prüfung.
- **Angriffsplan**: Backend zuerst und einzig.
  1. `EnumSchemaHelp.RequiredJsonPropertyNames` um den Ausschluss von `property.IsExtensionData` erweitern.
  2. `docs/openapi/v1.json` durch einen Testlauf von `ContractDocumentTests` neu schreiben lassen (die
     Testklasse schreibt die Datei selbst) und den Diff auf genau die erwartete Zeile prüfen.
  3. Die neue generische Assertion in
     `Vertragsdokument_BeschreibtDieLeitungWahrheitsgemaess` ergänzen.
  4. Volle Testsuite laufen lassen (Stop-Hook/`dotnet test Pugling.sln -c Release`).
- **Testweg**: `dotnet test backend/Pugling.Api.Tests --filter ContractDocumentTests` (regeneriert und
  diff-prüft `docs/openapi/v1.json`, führt die neue Assertion aus); danach der volle
  `dotnet test Pugling.sln -c Release`-Lauf vor dem Commit.

## Verlauf

- **2026-08-01** — geerntet aus dem Review zu [B-42](B-42-openapi-typen-generieren.md) Schritt 1 (E3),
  ungeprüft: der Befund am Schema steht, die Wirkung auf den Generator ist die offene Frage.
- **2026-08-03** — ausformuliert: Ursache am Code belegt (`Program.cs:309-335`, `EnumSchemaHelp.cs:26-36`) –
  `ProblemDetails.Extensions` ist eine `[JsonExtensionData]`-Property, die die eigene `required`-Berechnung
  nicht ausnimmt, während der generatorseitige `properties`-Aufbau sie schon korrekt ausschließt. Gemessen
  gegen das gesamte Dokument: einziges betroffenes Schema, `code` unbetroffen (eigener Mechanismus,
  `ProblemDetailsStamping.cs:29`/`:40`, `Program.cs:389-408`), kein Effekt auf den generierten
  `frontend/src/lib/contract.ts`.
- **2026-08-03** — gegrillt: alle Offenen Punkte in nummerierte Entscheidungen überführt (autonom
  getroffen, Nutzerauftrag 2026-08-04).
- **2026-08-03** — geschätzt: Größe S, Angriffsplan ein Filter in `EnumSchemaHelp` plus eine generische
  Assertion in `ContractDocumentTests`, Testweg `ContractDocumentTests` gefolgt vom vollen Suite-Lauf
  (autonom getroffen, Nutzerauftrag 2026-08-04).
- **2026-08-04** — **gehört in ein Bündel**, Ergebnis der Arbeitsrunde PM/API-Designer/Entwickler zu
  `docs/api-design-bewertung.md`: Diese Story, [B-60](B-60-flags-enum-im-dokument.md) und die beiden neuen
  Punkte aus [B-100](B-100-vertragsdokument-unterdeklariert.md) greifen alle in dieselbe
  Transformer-Kette (`Program.cs:283-408`) und lassen alle `ContractDocumentTests` das 900-KB-Dokument neu
  schreiben — getrennt gebaut sind es vier Commits mit je einem unlesbaren Riesendiff. **Reihenfolge:**
  B-60 → **B-56** → B4 → B5, dann **eine** Regenerierung als letzter, eigener Hunk; B-56 nach B-60, weil
  die generische `required`-Assertion gegen ein Dokument prüfen muss, das B-60 schon enthält.
  **Kollision, die beim getrennten Bau Arbeit vernichtet:** B-56 (Entscheidung 4) und B-60
  (Akzeptanzkriterium 3) beanspruchen **beide** „Punkt 5" derselben Testmethode
  `Vertragsdokument_BeschreibtDieLeitungWahrheitsgemaess` — die zweite Story müsste die erste umnummerieren
  und ihr eigener Text veraltet dabei; im Bündel werden es Punkt 5 bis 8 in einer Bearbeitung.
  **Auflage für die Abnahme:** die rote Probe dieser Story **vor** der ersten Codezeile einzeln gegen `HEAD`
  fahren (`--filter ContractDocumentTests`) und hier protokollieren — nach der Regenerierung ist alles
  gleichzeitig grün und „vorher rot" nicht mehr zu belegen.
- **2026-08-05** — im Autonomen Modus gebaut, nach B-60 wie in der Bündel-Notiz vorgesehen (Punkt 6 statt
  des kollidierenden Punkt 5). **Auflage erfüllt:** rote Probe einzeln gegen `HEAD` gefahren (`git stash`
  von `EnumSchemaHelp.cs`, neue Testklasse behalten) — `--filter ContractDocumentTests` scheiterte mit
  genau einem Fund: „Required fields without a matching property: ProblemDetails.extensions", exakt wie
  vorhergesagt. Danach implementiert: `dotnet test Pugling.sln -c Release` → **724/724 grün**.
  `docs/openapi/v1.json`-Diff exakt 3 Zeilen (das `required: ["extensions"]`-Feld unter `ProblemDetails`
  entfernt) — nichts sonst bewegt sich. `pugling-reviewer` fand keinen Blocker; bestätigt, dass
  `JsonPropertyInfo.IsExtensionData` sich wie erwartet verhält (das war das im Risiko-Abschnitt benannte
  Restrisiko) und dass `properties.extensions` **nicht** versehentlich ergänzt wurde (Akzeptanzkriterium 2).
  Kein Frontend-Anteil, `npm run build` als Kontrolle weiter grün. Commit: siehe Repo-Verlauf
  (B-56-Commit). Status → `abgenommen`.
- **2026-08-05** — Nachtrag zur neuen Eintrittsbedingung (README → „Der Rollengang fällt am leichtesten
  weg"): **kein Rollengang geführt, und keiner möglich** — die Änderung wirkt nicht zur Laufzeit für
  Creator, Vater oder Sohn (sie betrifft das Vertragsdokument, nicht das Verhalten). Belegt bleiben Suite und Reviewer; das ist hier die
  vollständige Verifikation, keine Lücke.
