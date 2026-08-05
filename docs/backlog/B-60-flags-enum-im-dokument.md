---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/qualitaet]
aliases: [SchoolTypes im Dokument, Flags-Enum als Werteliste]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/testabdeckung-plan.md
nachgeschaut: 2026-08-05
---

# B-60 · Das Vertragsdokument verbietet einen `SchoolTypes`-Wert, den Server und Frontend täglich austauschen

Gefunden vom `pugling-reviewer` beim Review von [B-42](B-42-openapi-typen-generieren.md) Schritt 2 (E6). Kein
Mangel dieser Etappe, aber E6 macht ihn **scharf**: seit die Frontend-Typen aus dem Dokument kommen, steht die
falsche Aussage als TypeScript-Union im Code.

## User Story

Als **Entwickler eines Clients**, der seine Typen aus dem Vertragsdokument erzeugt oder streng gegen das
Schema validiert, möchte ich, dass das Dokument keinen Wert verbietet, den der Server täglich sendet – damit
mein Generator keinen Typ baut, der gültige Antworten zurückweist.

## Ist-Stand am Code, belegt

Gegen den Stand vom 2026-08-04 nachgeprüft – alle ursprünglichen Belege stehen unverändert, ein Fund kam
dazu (letzter Punkt).

- `SchoolTypes` ist ein `[Flags]`-Enum
  ([LearnBaseTypes.cs:8-25](../../backend/Pugling.Contracts/Common/LearnBaseTypes.cs)). Der Schema-Transformer
  ([Program.cs:291-306](../../backend/Pugling.Api/Program.cs), „Allowed values") behandelt es wie jedes andere
  Enum: `type: string` plus eine `enum`-Liste der **sieben Einzelnamen** – bestätigt im generierten Dokument
  (`docs/openapi/v1.json:33163-33175`).
- Über die Leitung geht aber eine **Kombination**: der Seed legt mehrere Übungen mit `Realschule | Gymnasium`
  an ([Seed.cs:624,649,1003,1026](../../backend/Pugling.Api/Data/Seed.cs)), `GET /api/v1/creator/exercises`
  liefert also `"schoolTypes": "Realschule, Gymnasium"` – einen Wert, den das Schema ausschließt.
- Umgekehrt **sendet** das Frontend genau diesen String
  ([ExerciseEditModal.tsx:102](../../frontend/src/vater/ExerciseEditModal.tsx) – Zeile stimmt noch),
  und der Server nimmt ihn an.
- Folge in E6: `contract.ts` führt `SchoolTypes` als Union der sieben Einzelnamen
  (`contract.ts:19787`: `"None" | "Grundschule" | … | "Berufsschule"`) – zur Laufzeit falsch, sobald eine
  Kombination durch die Leitung geht.
- **Neu gegenüber der Ausformulierung, Korrektur:** Die in Offenem Punkt 3 vermutete „Hand-Ausnahme
  `schoolTypes?: string` in `uiTypes.ts`" als eigener Alias existiert **nicht mehr** – die Datei trägt
  inzwischen selbst einen Kommentar, der auf diese Story vorausweist
  ([uiTypes.ts:8-11](../../frontend/src/lib/uiTypes.ts)): „`SchoolType` lag hier einmal mit der Begründung
  `[Flags]`-Enum – falsch, das Schema listet die Einzelnamen und ist zeichengleich mit der Handliste gewesen.
  Nicht ausdrückbar ist nur die Kombination … und die reist als freier String (B-60)." Was bleibt, sind zwei
  **bestehende** Felder in ohnehin von Hand geführten Interfaces aus anderen Gründen: `schoolType?:
  components["schemas"]["SchoolTypes"]` in `ExerciseSearchParams` (Zeile 71) und `schoolTypes?: string` in
  `CreateExercisePayload` (Zeile 128) – keines davon ist die im Offenen Punkt vermutete Sonderregel, siehe
  Entscheidung 3.
- **Gezählt statt geschätzt** (beantwortet Offenen Punkt 1): eine Suche nach `[Flags]` über das gesamte
  Backend (`Pugling.Api`, `Pugling.Contracts`, `Pugling.Client`, `Pugling.Agent.Creator`,
  `Pugling.Api.Tests`) trifft genau **eine** Enum-Deklaration – `SchoolTypes` selbst. Alle weiteren Treffer
  sind Doku-/Kommentar-Erwähnungen des Konzepts (`Pugling.Api/CLAUDE.md`, `PuglingDbContext.cs:943-946`,
  `SchemaGuardTests.cs:129-131`), keine zweite Deklaration.
- **DB-Speicherung unberührt:** `SchoolTypes` liegt in allen drei betroffenen Spalten als `INTEGER`
  ([20260803223259_InitialCreate.cs:607,899,965](../../backend/Pugling.Api/Data/Migrations/20260803223259_InitialCreate.cs))
  – die bewusste `[Flags]`-Ausnahme von „Enum als String in der DB" (`PuglingDbContext.cs:943-946`). Diese
  Story ändert nur die OpenAPI-Schema-Erzeugung, nicht die Persistenz.

## Die echte Lücke

Das Dokument ist der Vertrag (E3). Hier behauptet es etwas, was die API widerlegt – und zwar in der
schlimmeren Richtung: ein Generator erzeugt einen Typ, der gültige Antworten **zurückweist**. Ein Client, der
streng gegen das Schema validiert, verwirft heute korrekte Daten.

Das ist dieselbe Familie wie die vier Mängel, die E6 behoben hat (Dokument beschreibt die Leitung nicht
wahrheitsgemäß), aber die einzige, bei der die Aussage **falsch** statt nur **fehlend** ist. Darum `Defekt`,
nicht `Aufräumen`, und P2 statt P3.

## Offene Punkte

1. ~~Gibt es weitere `[Flags]`-Enums im Vertrag, oder ist `SchoolTypes` das einzige?~~ → siehe Entscheidung 1.
2. ~~Soll die Ausnahme im Transformer stehen oder soll `SchoolTypes` einen eigenen Schema-Eintrag mit `pattern`
   bekommen?~~ → siehe Entscheidung 2.
3. ~~Nach der Reparatur: fällt die Hand-Ausnahme `schoolTypes?: string` in `uiTypes.ts` weg?~~ → beantwortet im
   Ist-Stand: die vermutete Ausnahme existiert schon heute nicht mehr, siehe Entscheidung 3.

## Entscheidungen

1. **`SchoolTypes` ist der einzige `[Flags]`-Enum im gesamten Backend** – gezählt per Grep über
   `Pugling.Api`, `Pugling.Contracts`, `Pugling.Client`, `Pugling.Agent.Creator` und die Tests (siehe
   Ist-Stand), nicht geschätzt. Begründung: macht Entscheidung 2 tragbar – ein genereller Mechanismus lohnt
   sich schon bei einem Treffer, weil er jeden künftigen `[Flags]`-Typ kostenlos mitnimmt. Kosten: keine,
   reine Feststellung.
2. **Die Ausnahme steht generisch im bestehenden Schema-Transformer** ([Program.cs:291-306](../../backend/Pugling.Api/Program.cs)),
   nicht als eigener Schema-Eintrag mit `pattern` für `SchoolTypes`. Begründung: dieselbe Regel wie bei
   `PuglingDbContext.ApplyEnumConvention` („eine Regel statt 32 Einzelfälle") – ein struktureller Zweig auf
   `enumType.IsDefined(typeof(FlagsAttribute), false)` deckt jeden künftigen `[Flags]`-Typ automatisch ab,
   ohne dass ihn jemand einzeln nachträgt. Ein `pattern` wäre präziser (Kombination statt „irgendein String"),
   bringt aber keinen praktischen Gewinn: `openapi-typescript` kollabiert beides zu `string`, und ein Regex,
   der Reihenfolge/Duplikate der Flags-Namen sauber abbildet, wäre selbst Pflegeaufwand für einen Fall, den
   heute niemand braucht. Kosten: ein zusätzlicher Zweig im Transformer (~10 Zeilen), keine neue Datei.
3. **Kein Frontend-Code ändert sich.** Die im Offenen Punkt vermutete Hand-Ausnahme `schoolTypes?: string`
   als eigener Typ-Alias in `uiTypes.ts` existiert bereits heute nicht mehr (siehe Ist-Stand, Kommentar
   verweist selbst auf diese Story). Die zwei verbleibenden Felder (`ExerciseSearchParams.schoolType`,
   `CreateExercisePayload.schoolTypes`) bleiben unverändert stehen – sie sind Teil größerer Hand-Typen aus
   anderen, unabhängigen Gründen (Query-Parameter-Bündel bzw. kollabierte Generik). Nach der Reparatur löst
   `components["schemas"]["SchoolTypes"]` in `ExerciseSearchParams` einfach zu `string` auf statt zur
   (falschen) Union – identisch mit dem, was `CreateExercisePayload` schon von Hand deklariert. Die Zahl der
   elf Hand-Typen in `uiTypes.ts` ändert sich **nicht** (kein Rückgang, weil dort nie eine eigene
   `SchoolType`-Zeile stand, die wegfallen könnte). Kosten: keine.

## Akzeptanzkriterien

1. `GET /api/v1/creator/exercises` liefert für eine Übung mit `Realschule | Gymnasium` einen Wert, den das
   Schema **zulässt** – heute schließt es ihn aus.
2. Das Schema von `SchoolTypes` führt keine `enum`-Liste der Einzelnamen mehr, sondern `type: string` mit einer
   Beschreibung der zulässigen Kombination.
3. Eine Zusicherung in `ContractDocumentTests` hält das fest: kein `[Flags]`-Enum im Dokument trägt eine
   `enum`-Liste – reflektiv über die `[Flags]`-Typen der Contracts-Assembly geprüft (Idiom
   `typeof(PointKind).Assembly.GetTypes()`, siehe `ConventionGuardTests.cs:77`), nicht über den Feldnamen
   `SchoolTypes`. Sie ist vor der Reparatur rot.
4. `contract.ts` führt `SchoolTypes` nach dem nächsten `npm run gen:contract` als `string`; an `uiTypes.ts`
   ändert sich nichts (Entscheidung 3).
5. Offener Punkt 1 ist beantwortet: die `[Flags]`-Typen im Vertrag sind **gezählt** (genau einer), nicht
   geschätzt.

## Schätzung

**Größe: S** – eine gezielte Änderung am bestehenden Schema-Transformer (~10-15 Zeilen, ein struktureller
Zweig) plus eine neue reflektive Zusicherung in `ContractDocumentTests.cs`. Keine Migration, keine
Contracts-DTO ändert ihre Form, kein Frontend-Handcode nötig (Entscheidung 3) – vergleichbar mit dem
S-Anker `childId` aus dem Test-Pfad ziehen (B-01): eine mechanische, klar umrissene Änderung ohne offene
Design-Frage mehr.

**wo: backend** – abweichend hätte man „beides" vermutet (B-42/E6 macht den Mangel im Frontend sichtbar),
aber die Recherche zeigt: kein Frontend-Quelltext muss angefasst werden, nur `contract.ts` regeneriert sich
automatisch. Nur `Program.cs` und `Pugling.Api.Tests` sind betroffen.

**migration: nein** – `SchoolTypes` bleibt in der DB `INTEGER` (die bestehende `[Flags]`-Ausnahme von
„Enum als String", siehe Ist-Stand); die Story ändert ausschließlich die OpenAPI-Schema-Erzeugung, keine
EF-Konfiguration und keine Spalte.

**vertragsbruch: nein** – die Änderung **lockert** eine heute falsch restriktive Schema-Aussage (verbotene
Werte werden erlaubt), sie benennt keine DTO-Form um und entfernt kein Feld. Der generierte TS-Typ wird von
einer Literal-Union zu `string` **weiter**, nicht enger – bestehender Code, der gegen einzelne Enum-Namen
vergleicht, bleibt gültig (ein `string`-Vergleich typprüft weiterhin). Einzige Nebenwirkung ist der Verlust
der TS-Autovervollständigung für die Einzelnamen, kein Laufzeit-Bruch.

**Risiken:**

- Die neue Zusicherung muss strukturell auf `[FlagsAttribute]` reflektieren, nicht auf den Namen
  `SchoolTypes` – sonst bleibt sie blind für jeden künftigen zweiten `[Flags]`-Typ (genau die Lücke, die
  diese Story schließt).
- Die 400-Fehlermeldung „allowed values: …" für ungültige **Einzel**werte (`Program.cs:91-97`,
  `EnumSchemaHelp.AllowedValues`) bleibt bewusst unverändert – sie ist laufzeitkorrekt für einen einzelnen
  falschen Namen und kein Fall dieser Story (die betrifft nur die Schema-**Beschreibung** einer gültigen
  Kombination).
- Verlust der TS-Autovervollständigung für `SchoolTypes`-Einzelnamen im generierten Client – rein kosmetisch,
  kein Funktionsverlust.

**Angriffsplan** (Backend zuerst):

1. `Program.cs:291-306`: Zweig ergänzen – ist `enumType.IsDefined(typeof(FlagsAttribute), false)`, dann
   `schema.Enum` **nicht** setzen (Typ bleibt `string`), Beschreibungs-Hinweis auf „comma-separated
   combination of: …" statt „Allowed values: …" anpassen.
2. `ContractDocumentTests.cs`: neue Zusicherung in `Vertragsdokument_BeschreibtDieLeitungWahrheitsgemaess`
   (Punkt 5) – `[Flags]`-Enums der Contracts-Assembly reflektiv sammeln, je Namen das Schema im Dokument
   holen und sicherstellen, dass kein `enum`-Array darin steht.
3. `dotnet test` grün; `docs/openapi/v1.json`-Diff auf die `SchoolTypes`-Schemazeilen prüfen (nichts sonst
   sollte sich bewegen).
4. Frontend: `npm run gen:contract` laufen lassen (regeneriert das gitignorte `contract.ts`) – kein
   Handcode laut Entscheidung 3; `npm run build` + `npm test` zur Kontrolle, dass nichts bricht.

**Testweg:** `dotnet test backend/Pugling.Api.Tests` – die neue Assertion in `ContractDocumentTests.cs` muss
vor der Reparatur rot sein, danach grün (Regressionstest, `art: Defekt`). Ergänzend `npm run build` im
Frontend nach `gen:contract` als Kontrolle. Kein `/smoke-test` nötig – reines Schema-/Dokument-Thema ohne
Verhaltensänderung zur Laufzeit.

## Verlauf

- **2026-08-01** — angelegt aus dem Review zu E6. Der Befund ist **verifiziert**: Seed-Daten, Antwortwert,
  Sende-Stelle im Frontend und die generierte Union sind je am Code belegt.
- **2026-08-01** — **ausformuliert.** Der Backlog-Wächter hat die Stufe `idee` angemahnt, weil dort
  `unverifiziert: true` stehen muss – hier stand `false`, und das war richtig: der Befund war schon bei der
  Anlage am Code belegt. Statt die Eintrittsbedingung mit einer falschen Angabe zu erfüllen, ist die Stufe
  nachgezogen; ergänzt wurden nur die zwei Abschnitte, die ihr noch fehlten (User Story, Entwurf der
  Akzeptanzkriterien). Kein Code berührt.
- **2026-08-04** — **gegrillt** (autonom getroffen, Nutzerauftrag): alle drei offenen Punkte in nummerierte
  Entscheidungen überführt (kein Punkt zurückgestellt). Belege gegen den heutigen Code nachgeprüft: alle
  Zeilenangaben stimmten noch, ergänzt wurde ein Fund – die im Offenen Punkt 3 vermutete Hand-Ausnahme
  `schoolTypes?: string` in `uiTypes.ts` existiert dort bereits nicht mehr als eigener Alias (der Kommentar an
  der Stelle verweist selbst schon auf diese Story).
- **2026-08-04** — **geschätzt** (autonom getroffen, Nutzerauftrag): Größe S, `wo: backend` (kein
  Frontend-Handcode nötig), `migration: nein`, `vertragsbruch: nein`; kein XL-Split nötig.
- **2026-08-04** — **gehört in ein Bündel und läuft darin zuerst**, Ergebnis der Arbeitsrunde
  PM/API-Designer/Entwickler zu `docs/api-design-bewertung.md`: mit
  [B-56](B-56-problemdetails-required-extensions.md) und den beiden neuen Punkten aus
  [B-100](B-100-vertragsdokument-unterdeklariert.md) in **einen** Branch, Reihenfolge **B-60** → B-56 → B4 →
  B5, dann **eine** Regenerierung von `docs/openapi/v1.json` als letzter, eigener Hunk. B-60 zuerst, weil es
  auf der Schema-Ebene arbeitet (`schema.Enum` je Typ) und B-56 gegen ein Dokument prüfen muss, das es schon
  enthält. Die Begründung dieser Story („die einzige, bei der die Aussage **falsch** statt nur **fehlend**
  ist — darum `Defekt`") hat in der Runde zusätzlich die Einordnung von B4 entschieden: eine fehlende
  `responses`-Deklaration bleibt `Aufräumen`, sonst wird die Kategorie wertlos.
  **Kollision:** B-60 (Akzeptanzkriterium 3) und B-56 (Entscheidung 4) beanspruchen beide „Punkt 5"
  derselben Testmethode `Vertragsdokument_BeschreibtDieLeitungWahrheitsgemaess`; im Bündel werden es Punkt 5
  bis 8 in einer Bearbeitung. **Auflage:** die rote Probe **vor** der ersten Codezeile einzeln gegen `HEAD`
  fahren und hier protokollieren.
- **2026-08-05** — im Autonomen Modus gebaut, **einzeln** committet statt im ursprünglich geplanten
  gemeinsamen Branch mit B-56 (Projekt-Konvention: ein Commit je Story) — die Reihenfolge B-60 zuerst
  bleibt aber eingehalten, B-56 baut als Punkt 6 auf demselben Testkörper auf. Rote Probe **vor** der
  Codeänderung protokolliert (Auflage aus der Kollisionsnotiz): `git stash` von `Program.cs`,
  `Vertragsdokument_BeschreibtDieLeitungWahrheitsgemaess` scheiterte mit „[Flags] enums with an `enum`
  value list … SchoolTypes" — danach implementiert, grün. `dotnet test Pugling.sln -c Release` →
  **716/716 grün**. `docs/openapi/v1.json`-Diff exakt wie vorhergesagt: nur die `SchoolTypes`-Schema-Node
  (Enum-Liste entfernt, Beschreibungstext umformuliert). `pugling-reviewer` fand keinen Blocker am Fix
  selbst, aber einen Nebenbefund: `docs/api-examples/study-plans.md` und
  `openapi-examples.generated.json` trugen zusätzlich einen nicht-deterministischen Diff
  (`dailyBox.coinsAwarded`, `Random.Shared` in `DailyBoxService` ohne Seed) — zurückgesetzt, damit der
  Commit ausschließlich B-60 zeigt, und als [B-107](B-107-dailybox-zufallswert-in-docs-capture.md) neu
  aufgenommen statt hier mitgelöst. Frontend unverändert (Entscheidung 3): `npm run build` regeneriert
  `SchoolTypes` korrekt als `string`, `npm test` weiter 131/131. Commit: siehe Repo-Verlauf
  (B-60-Commit). Status → `abgenommen`.
- **2026-08-05** — Nachtrag zur neuen Eintrittsbedingung (README → „Der Rollengang fällt am leichtesten
  weg"): **kein Rollengang geführt.** Belegt waren die Suite und der Reviewer, nicht aber ein Gang als
  Creator an der laufenden App. Kein Schaden bekannt — die Lücke steht hier, statt still zu bleiben.
