---
tags: [typ/story, status/geschaetzt, bereich/backend, bereich/qualitaet]
aliases: [Status als String, GoalStatus ohne Werteliste, BatchItemResult ohne Werteliste]
status: geschaetzt
prio: P3
art: Aufräumen
groesse: S
wo: beides
migration: nein
vertragsbruch: ja
quelle: docs/testabdeckung-plan.md
---

# B-59 · Zwei Antwortfelder tragen einen Status als nackten `string`

Gefunden beim Bauen von [B-42](B-42-openapi-typen-generieren.md) Schritt 2 (E6): als die Vertragstypen des
Frontends aus dem Dokument kamen, blieben genau zwei Felder ohne Werteliste übrig. Die Recherche zu dieser
Stufe hat ein **drittes** Feld derselben Bauart nachgezogen (siehe unten).

## User Story

Als **Entwickler eines Clients**, der seine Typen aus dem Vertragsdokument erzeugt oder streng gegen das
Schema validiert, möchte ich, dass `KeyResultResponse.Status`/`.Scope`, `ObjectiveResponse.Status` und
`BatchItemResult.Status` eine dokumentierte Werteliste tragen statt eines nackten `string` – damit mein
generierter Typ die tatsächlich möglichen Werte kennt und ein Tippfehler im Vergleich (z. B. `"acheived"`)
nicht mehr unbemerkt bleibt.

## Ist-Stand am Code

- `KeyResultResponse.Status`/`.Scope` und `ObjectiveResponse.Status` sind `string`
  ([GoalDtos.cs:9-17](../../backend/Pugling.Contracts/Supervisor/GoalDtos.cs)). Die Werte entstehen live:
  `ObjectiveEvaluationService.StatusOf` liefert `"achieved"`/`"overdue"`/`"open"`
  ([ObjectiveEvaluationService.cs:54-55](../../backend/Pugling.Api/Services/Shared/ObjectiveEvaluationService.cs)),
  `ObjectiveService.KrScope` liefert `"exercise"`/`"chapter"`/`"subject"`
  ([ObjectiveService.cs:28-29](../../backend/Pugling.Api/Services/Supervisor/ObjectiveService.cs)). Beide
  Felder sind reine Response-Werte – **weder** `Objective` **noch** `KeyResult` tragen eine `Status`- oder
  `Scope`-Spalte ([ObjectiveEntities.cs](../../backend/Pugling.Api/Models/ObjectiveEntities.cs)); es gibt also
  nichts zu migrieren.
- **Drittes Feld, in der Idee noch nicht erfasst:** `BatchItemResult.Status`
  ([VocabularyStoreDtos.cs:58](../../backend/Pugling.Contracts/Creator/VocabularyStoreDtos.cs)) ist ebenfalls
  ein nackter `string`, mit **zwei unterschiedlichen, teils überlappenden Wertelisten** je nach Endpunkt:
  `POST creator/vocabulary/batch` liefert `"created"`/`"existing"`/`"error"`, `PATCH …/batch` liefert
  `"updated"`/`"not-found"`/`"error"`
  ([VocabularyStoreController.cs:474-483,505-509](../../backend/Pugling.Api/Controllers/Creator/VocabularyStoreController.cs)).
  Der `/// <summary>` des Records nennt keinen Wert; der Doc-Kommentar von `CreateBatch` nennt nur
  `existing`/`created` (nicht `error`), der von `UpdateBatch` gar keinen.
- **Grep-Beleg für „das sind alle"**: eine Suche nach `string Status`/`string State`/`string Scope` über ganz
  `Pugling.Contracts` trifft **genau diese vier Vorkommen in zwei Dateien** – kein weiteres. Alle anderen
  Status-Felder des Vertrags (`ShopPurchaseStatus`, `ActivationRequestStatus`, `KlassenarbeitStatus`,
  `RemarkStatus`) sind bereits echte Enums.
- **Das Gegenstück ist im selben Durchgang schon repariert**: `Metric` und `Kind` waren aus demselben Grund
  `string` und tragen jetzt ihre Enums – wire-identisch, aber mit Werteliste im Dokument.
- **Vertragskonvention bereits etabliert**: `Program.cs:299-305` dokumentiert jedes Enum automatisch mit
  „Allowed values: …" im Schema, sobald es ein echtes C#-Enum ist (globaler `JsonStringEnumConverter`,
  `Program.cs:35`) – die drei betroffenen Felder bekämen diese Behandlung **kostenlos**, sie ist heute nur
  wegen des nackten `string`-Typs nicht ausgelöst.
- **Wire-Format aller bestehenden Enums ist PascalCase** (Beleg: `docs/api-examples/shop.md:584` `"status":
  "Pending"`, `class-tests.md:33` `"Planned"`, `remarks.md:35` `"Open"`) – es gibt **kein** Präzedenzfall für
  `[JsonStringEnumMemberName]` (0 Treffer im ganzen Backend), der eine abweichende Kleinschreibung trüge.
- **Frontend behilft sich schon**: `GoalStatus` bleibt ein Hand-Typ in
  [uiTypes.ts:35](../../frontend/src/lib/uiTypes.ts), ausdrücklich nur als Whitelist des **Filters**
  (`api.objectives({ status })`), nicht als Zusage über die Antwort. `StatusPill` in
  [VaterZiele.tsx:68-70](../../frontend/src/vater/VaterZiele.tsx) nimmt `string` und fällt bei Unbekanntem
  auf „offen" zurück – der im Ist-Stand behauptete Tippfehler-Blindflug ist damit real belegt, nicht nur
  vermutet.
- **Blast radius geprüft, nicht geschätzt**: `kr.scope` wird im Frontend nur **angezeigt**
  (`VaterZiele.tsx:330`), nirgends verglichen. `status` wird an zwei Stellen mit `===` auf `"achieved"`/
  `"overdue"` geprüft ([MyObjectives.tsx:44,49](../../frontend/src/sohn/MyObjectives.tsx)) und einmal als
  `Record<GoalStatus, …>`-Schlüssel benutzt (`VaterZiele.tsx:62`). Kein Playwright-E2E-Spec referenziert
  `achieved`/`overdue`/`GoalStatus`/`kr.scope` (0 Treffer). Im Backend hängen acht **wörtliche**
  String-Assertions an der heutigen Kleinschreibung: `ObjectiveTests.cs:47,68,98,113,143,230,236` (`"open"`/
  `"achieved"`) und `VocabAgentApiTests.cs:127,130` (`"created"`/`"existing"`).

## Die echte Lücke

Größer als die Idee-Notiz behauptete (zwei Felder), aber immer noch klein und rein technisch: **drei**
Antwortfelder in **zwei** Dateien lassen eine Oberfläche nicht vollständig fallunterscheiden und liefern
einem generierten Client `string` statt einer Werteliste. Kein Persistenz-Feld ist betroffen (beide Ist-Stand
belegten Quellen sind live berechnet), also keine Migration. Und obwohl die Umstellung das **Vertrags**format
bricht (Kleinschreibung → PascalCase), ändert sich für Vater und Sohn **nichts sichtbar**, sofern Backend und
Frontend im selben Zug landen: `GOAL_PILL` bildet weiterhin auf dieselben deutschen Texte ab, nur der
Schlüssel wechselt die Schreibweise. Das rechtfertigt `art: Aufräumen` trotz des Vertragsbruchs.

## Offene Punkte

1. ~~Kleinschreibung beibehalten (`[JsonStringEnumMemberName]`) oder auf die Hausform `Open`/`Achieved`/
   `Overdue` gehen?~~ → siehe Entscheidung 1.
2. ~~Gilt dasselbe für `Scope`?~~ → siehe Entscheidung 2.
3. ~~Gibt es weitere `string`-Felder mit fester Werteliste?~~ → beantwortet im Ist-Stand: ja, genau eines
   (`BatchItemResult.Status`), gezählt per Grep über den ganzen Vertrag, nicht mehr offen.
4. ~~Wie geht `BatchItemResult.Status` mit den zwei unterschiedlichen Wertelisten je Endpunkt um?~~ → siehe
   Entscheidung 3.
5. ~~Was passiert mit dem Hand-Typ `GoalStatus` in `uiTypes.ts`, sobald der Vertrag die Werteliste selbst
   trägt?~~ → siehe Entscheidung 4.

## Entscheidungen

1. **`KeyResultResponse.Status`/`ObjectiveResponse.Status` werden ein echtes C#-Enum `GoalStatus { Open,
   Achieved, Overdue }`** in `Pugling.Contracts.Common` (neben `ObjectiveKind`/`KeyResultMetric` in
   `ObjectiveBaseTypes.cs`), serialisiert über den bestehenden globalen `JsonStringEnumConverter` – **keine**
   `[JsonStringEnumMemberName]`-Ausnahme für die heutige Kleinschreibung. Begründung: alle vier bereits
   vorhandenen Status-Enums des Vertrags (`ShopPurchaseStatus`, `ActivationRequestStatus`,
   `KlassenarbeitStatus`, `RemarkStatus`) serialisieren PascalCase, und es gibt keinen einzigen Präzedenzfall
   für die Ausnahme im Backend – sie neu einzuführen, nur um einen heute billigen Fix zu vermeiden, wäre ein
   zweites Muster für dieselbe Sache. Kosten: **Vertragsbruch** (Wire-Wert wechselt `"achieved"` →
   `"Achieved"`); betrifft geprüft genau 8 Backend-Test-Assertions (`ObjectiveTests.cs`) und 3 Frontend-Dateien
   (`uiTypes.ts`, `VaterZiele.tsx`, `MyObjectives.tsx`) – kein E2E-Spec.
2. **`KeyResultResponse.Scope` wird ein Enum `KeyResultScope { Exercise, Chapter, Subject }`**, gleicher Ort,
   gleiche Begründung wie Entscheidung 1. Kosten: geringer als Entscheidung 1 – das Frontend vergleicht `scope`
   nirgends, es zeigt den Wert nur an (`VaterZiele.tsx:330`); die Änderung ist dort rein kosmetisch
   (Groß-/Kleinschreibung der Anzeige).
3. **`BatchItemResult.Status` wird ein gemeinsames Enum `BatchItemStatus { Created, Existing, Updated,
   NotFound, Error }`** statt zweier getrennter Typen für Create- und Update-Batch. Begründung: ein Konzept
   („wie ist dieses Batch-Element ausgegangen"), eine DTO, ein Typ – dieselbe Regel, nach der ein Enum mehrere
   Aufrufer haben darf, ohne dass jeder Aufrufer alle Werte benutzt (Vorbild: `KeyResultMetric` wird auch nicht
   von jedem Metrik-Typ gleich genutzt). Kosten: `error` ist der einzige über beide Endpunkte geteilte Wert,
   die anderen vier sind je endpunktspezifisch – die XML-Docs von `CreateBatch`/`UpdateBatch` müssen das
   **explizit** je Endpunkt sagen (heute lückenhaft, siehe Ist-Stand), sonst suggeriert das gemeinsame Enum
   fälschlich, jeder Wert sei überall möglich.
4. **Der Hand-Typ `GoalStatus` in `uiTypes.ts` fällt weg**, sobald `npm run gen:contract` die echte
   Werteliste aus dem Dokument zieht – dieselbe Bewegung, die `Metric`/`Kind` in E6 schon gemacht haben.
   Begründung: `frontend/CLAUDE.md`s eigene Regel verlangt für jeden Hand-Typ in `uiTypes.ts` einen Grund;
   der bisherige Grund („der Vertrag sagt nur `string`") entfällt mit dieser Story. Kosten: `VaterZiele.tsx`
   (`GOAL_PILL`-Schlüssel, `StatusPill`) und `MyObjectives.tsx` (zwei `===`-Vergleiche) wechseln auf die
   generierten PascalCase-Werte; `api.ts`s Import von `GoalStatus` wechselt von `uiTypes.ts` auf den
   generierten Typ-Barrel.
5. **Kein genereller mechanischer Wächter gegen künftige nackte Status-`string`s.** Begründung: die
   Grep-Zählung im Ist-Stand zeigt ein **geschlossenes, kleines** Vorkommen (vier Felder in zwei Dateien) –
   ein generischer Test müsste zwischen einem echten Status/einer echten Scope und beliebigem Freitext
   unterscheiden und hätte damit ein hohes Falsch-Positiv-Risiko (anders als B-60s `[Flags]`-Wächter, der an
   einem strukturellen Merkmal, nicht an einem Feldnamen hängt). Kosten: keine – dafür bleibt die Grep-Probe
   aus diesem Ist-Stand die Referenz, falls die Frage in einem Jahr wieder auftaucht.

## Akzeptanzkriterien

1. `KeyResultResponse.Status` und `ObjectiveResponse.Status` sind das gemeinsame Enum `GoalStatus`, `Scope` ist
   `KeyResultScope`, `BatchItemResult.Status` ist `BatchItemStatus` – alle drei in `Pugling.Contracts`, mit
   `///`-Summary je Member.
2. Das OpenAPI-Dokument trägt für alle drei Felder eine `enum`-Liste plus „Allowed values"-Beschreibung
   (automatisch durch den bestehenden Schema-Transformer, keine Sonderbehandlung nötig).
3. `CreateBatch`/`UpdateBatch` dokumentieren in ihrem `/// <summary>` **vollständig und je Endpunkt getrennt**,
   welche `BatchItemStatus`-Werte tatsächlich auftreten können (heute lückenhaft, siehe Ist-Stand).
4. `dotnet test` ist grün, insbesondere die acht angepassten Assertions in `ObjectiveTests.cs` und die zwei in
   `VocabAgentApiTests.cs` (neue Groß-/Kleinschreibung).
5. `npm run gen:contract` erzeugt `contract.ts` mit Literal-Union-Typen für alle drei Felder statt `string`;
   der Hand-Typ `GoalStatus` in `uiTypes.ts` ist entfernt.
6. `VaterZiele.tsx` (`GOAL_PILL`, `StatusPill`, `kr.scope`-Anzeige) und `MyObjectives.tsx`
   (Status-Vergleiche) sind auf die neuen PascalCase-Werte gehoben; `npm run build` (tsc) und `npm test` sind
   grün.
7. Kein Verhalten ändert sich für Vater oder Sohn – `GOAL_PILL` zeigt dieselben deutschen Texte wie vorher,
   nur der Schlüssel hat sich geändert.

## Schätzung

**Größe: S** – drei Enum-Umstellungen ohne neue Fachlogik und ohne Migration (beide Quellfelder sind live
berechnet, keine Spalte betroffen), aber über zwei Ebenen verteilt: zwei Contracts-Dateien, zwei
Service-/Controller-Stellen (`ObjectiveEvaluationService.StatusOf`, `ObjectiveService.KrScope`,
`VocabularyStoreController`), zwei Backend-Testdateien (10 Assertions gesamt) und vier Frontend-Dateien
(`contract.ts` generiert, `uiTypes.ts`, `VaterZiele.tsx`, `MyObjectives.tsx`). Jede einzelne Änderung ist
mechanisch; nichts davon ist eine offene Design-Entscheidung mehr.

**wo: beides** – abweichend von der Erstannahme „backend": die Recherche zeigt echten Handarbeitsbedarf im
Frontend (drei Dateien mit String-Vergleichen/Enum-Schlüsseln, nicht nur der generierte Barrel). Ein reiner
Backend-Review würde genau die Stelle übersehen, an der B-10s Schätzung schon einmal am schwächsten war.

**migration: nein** – `Objective`/`KeyResult` tragen weder `Status` noch `Scope` als Spalte
([ObjectiveEntities.cs](../../backend/Pugling.Api/Models/ObjectiveEntities.cs)); beide Werte sind reine
Laufzeit-Berechnung. `BatchItemResult` ist ohnehin nie persistiert.

**vertragsbruch: ja** – die Wire-Werte wechseln die Schreibweise (`"achieved"` → `"Achieved"` usw.); ein
Client, der heute auf die Kleinschreibung vergleicht, bricht. Blast radius ist aber **gemessen, nicht
geschätzt**: 0 E2E-Specs, 3 Frontend-Dateien, 10 Backend-Test-Assertions.

**Risiken:**

- Wird eine der acht `ObjectiveTests.cs`-Assertions oder der zwei `VocabAgentApiTests.cs`-Assertions
  übersehen, bleibt sie **stumm rot** (Test schlägt fehl) – kein stiller Datenverlust, aber ein vermeidbarer
  Rücksprung. Gegenmittel: die Zeilen sind oben einzeln benannt.
- Das gemeinsame `BatchItemStatus`-Enum kann fälschlich suggerieren, jeder der fünf Werte sei an jedem
  Endpunkt möglich – Gegenmittel ist Akzeptanzkriterium 3 (Doku je Endpunkt).
- `contract.ts` ist gitignored und wird nur durch `npm run gen:contract`/`postinstall`/`predev`/`prebuild`
  neu erzeugt – wer die Frontend-Dateien vor einem frischen Generieren anfasst, editiert gegen einen
  veralteten Typ.

**Angriffsplan** (Backend zuerst):

1. `Pugling.Contracts.Common`: `GoalStatus`, `KeyResultScope` in `ObjectiveBaseTypes.cs` ergänzen;
   `BatchItemStatus` neben `BatchItemResult` in `VocabularyStoreDtos.cs`. `GoalDtos.cs`/`VocabularyStoreDtos.cs`
   auf die Enum-Typen umstellen.
2. `ObjectiveEvaluationService.StatusOf` und `ObjectiveService.KrScope` auf Enum-Rückgabe heben.
3. `VocabularyStoreController.CreateBatch`/`UpdateBatch`: Enum-Werte statt String-Literale, XML-Docs je
   Endpunkt um die tatsächlichen Werte ergänzen.
4. `ObjectiveTests.cs` (Zeilen 47, 68, 98, 113, 143, 230, 236) und `VocabAgentApiTests.cs` (Zeilen 127, 130)
   auf die neue Schreibweise heben; `dotnet test` grün.
5. Frontend: `npm run gen:contract`, `uiTypes.ts` (`GoalStatus` entfernen), `VaterZiele.tsx`, `MyObjectives.tsx`
   nachziehen; `npm run build` + `npm test` grün.
6. `/smoke-test` als Rauchprobe für Ziele- und Vokabel-Batch-Endpunkte.

**Testweg:** `backend/Pugling.Api.Tests/ObjectiveTests.cs` (angepasste Assertions) und
`VocabAgentApiTests.cs` (dito) laufen unter `dotnet test`; Frontend über `npm run build` (Typecheck deckt
jede verbliebene String-Stelle auf) und `npm test`. Kein neuer Integrationstest nötig – die Umstellung ändert
keine Fachlogik, nur den Wire-Typ, und die bestehenden Assertions sind bereits die schärfste Probe dafür.

## Verlauf

- **2026-08-01** — angelegt beim Bauen von E6. Der Befund ist am Code belegt; unverifiziert ist nur, wie
  viele weitere Felder dieselbe Form haben (offener Punkt 3).
- **2026-08-03** — **ausformuliert.** Gegen den echten Code recherchiert: Grep über `Pugling.Contracts`
  bestätigt genau vier Vorkommen in zwei Dateien (drei Felder), davon eines (`BatchItemResult.Status`) in der
  Idee-Notiz noch nicht erfasst. Blast radius für Frontend/E2E/Backend-Tests gemessen statt geschätzt.
- **2026-08-03** — **gegrillt** (autonom getroffen, Nutzerauftrag 2026-08-04): alle fünf offenen Punkte in
  nummerierte Entscheidungen mit Begründung und Kosten überführt, kein Punkt zurückgestellt.
- **2026-08-03** — **geschätzt** (autonom getroffen, Nutzerauftrag 2026-08-04): Größe S, `wo: beides`
  (abweichend von der Erstannahme, siehe Schätzung), `migration: nein`, `vertragsbruch: ja`; kein XL-Split
  nötig.
- **2026-08-04** — **unabhängig bestätigt** durch `docs/api-design-bewertung.md` (Vorschlag A4) und die
  Arbeitsrunde PM/API-Designer/Entwickler: der Bericht fand `BatchItemResult.Status` selbständig und schlug
  **wortgleich** dasselbe Enum vor (`BatchItemStatus { Created, Existing, Updated, NotFound, Error }`), ohne
  `GoalStatus`/`KeyResultScope` zu sehen. Er wurde als Dublette zurückgezogen, diese Story bleibt die
  Quelle. Zwei Anmerkungen aus der Runde: (a) Der Bericht schlug an anderer Stelle erneut einen generischen
  Wächter gegen nackte Status-Strings vor — **Entscheidung 5 hat das begründet abgelehnt**, die Frage ist
  nicht neu offen. (b) Eine echte Ergänzung wäre ein Zähl-Kopf oder Summen-Objekt an den Batch-Antworten,
  damit ein Aufrufer nicht 500 Elemente durchzählt; bewusst **nicht** als eigene Story angelegt, weil kein
  Aufrufer gemessen ist, der das braucht — sie steht hier, damit sie beim Bau von B-59 auf dem Tisch liegt.
