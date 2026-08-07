---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/api, bereich/doku]
aliases: [401 nicht deklariert, 24 Operationen ohne summary, X-Total-Count im Dokument, Dokument-Bündel]
status: abgenommen
prio: P3
art: Aufräumen
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/api-design-bewertung.md (Vorschläge B4, B5, B6) — Arbeitsrunde PM/API-Designer/Entwickler am 2026-08-04
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-08-07"
---

# B-100 · Das Vertragsdokument verschweigt 401, `X-Total-Count` und 24 Operationsnamen

Das OpenAPI-Dokument beschreibt die Leitung an drei Stellen unvollständig: `401` steht an 5 von 323
Operationen (obwohl fast alle Authentifizierung verlangen), **keine** Antwort deklariert einen Kopf — auch die
31 paginierten nicht, die `X-Total-Count` senden — und **24 Operationen tragen keinen Namen**: genau die 12
POST und 12 PUT, mit denen Übungen angelegt und ersetzt werden. Dazu zwei fehlende Laufzeit-Köpfe an den
Login-Türen.

## User Story

Als **Mensch oder KI-Creator, der die API über ihr Dokument liest**, möchte ich, dass jede Operation einen
Namen trägt und ihre möglichen Antworten nennt, damit ich nicht im Quelltext nachsehen muss, was das Scalar-UI
verschweigt.

## Ist-Stand am Code

- **401/403:** an 5 von 323 Operationen deklariert (aus `docs/openapi/v1.json` ausgezählt, beide Rollen
  unabhängig auf dieselbe Zahl gekommen).
- **Antwort-Köpfe:** 0 Operationen mit `headers`, obwohl `httpPaged` im Frontend `X-Total-Count` von Hand
  liest.
- **24 Operationen ohne `summary`:** exakt die 12 POST + 12 PUT der Übungs-Controller. Die Doku-Kommentare
  **existieren** an `Controllers/Creator/ExerciseControllerBase.cs:224` und `:297` und kommen bei
  `List`/`Get`/`Delete` derselben Basisklasse durch — nur bei `Create`/`Update` nicht.
- **Die Infrastruktur ist da:** vier Operation-/Schema-Transformer stehen in `Program.cs:283-408`; der
  Fehlercode-`enum`-Transformer ist das exakte Vorbild für beide neuen.
- **Laufzeit-Köpfe:** `Program.cs:250-258` setzt nur `RejectionStatusCode`, kein `OnRejected` (also kein
  `Retry-After`); `Controllers/AuthController.cs:48-99` (die drei Login-Türen) und `GET auth/me` setzen kein
  `Cache-Control: no-store`.

## Die echte Lücke

Keine Falschaussage, sondern eine **Auslassung** — und das ist der Grund für `art: Aufräumen` statt `Defekt`
(siehe Entscheidung 1). Betroffen ist, wer die API über das Dokument liest: der Mensch im Scalar-UI und der
KI-Creator. Der *generierte* Vertrag leidet nicht, und das war in der Runde die entscheidende Messung.

## Entscheidungen

Erarbeitet in der Arbeitsrunde PM/API-Designer/Entwickler am 2026-08-04.

1. **`art: Aufräumen`, nicht `Defekt` — hier hat der API-Designer seine eigene Hochstufung widerlegt.**
   Er hatte angekündigt, B4 werde zum Defekt, falls `frontend/src/lib/contract.ts` am Dokument hängt.
   Es hängt daran (`frontend/scripts/gen-contract.mjs:10` liest `docs/openapi/v1.json`, an
   `postinstall`/`predev`/`prebuild`) — **aber der generierte Typ wird für Antworten nie gelesen**: das
   401-Handling ist ein untypisierter globaler Check auf dem rohen `Response`
   (`frontend/src/lib/api.ts:170`), und `X-Total-Count` liest `httpPaged` ebenfalls von Hand (0 Treffer auf
   `responses[…]` außerhalb von `contract.ts`). Dazu das begriffliche Argument: eine **fehlende**
   `responses`-Deklaration ist in OpenAPI keine Falschaussage — undeklarierte Status sind nicht
   ausgeschlossen. Genau diese Linie zieht [B-60](B-60-flags-enum-im-dokument.md) für sich selbst
   („die einzige, bei der die Aussage falsch statt nur fehlend ist"). Würde eine Auslassung `Defekt`
   heißen, wäre die Kategorie wertlos.
   *Was das kippen würde:* sobald irgendwo `paths[…]["get"]["responses"]["401"]` typisiert gelesen wird, ist
   es ein Defekt. Heute tut es niemand.
2. **`[EndpointSummary]` direkt, ohne Ursachenforschung.** Der Bericht wollte erst herausfinden, *warum* die
   Auflösung bei `Create`/`Update` scheitert (unverifizierter Verdacht: der generische Parametertyp im
   Doc-Schlüssel). Beide Rollen sind sich einig, dass das eine Recherche mit offenem Ende in einem
   Fremdgenerator ist — und dass der „Notausgang" das **bessere Ergebnis** liefert: „Legt eine Vokabelübung
   an" statt zwölfmal derselben generischen Zeile.
3. **`Retry-After` fällt weg, `Cache-Control: no-store` bleibt.** Ein `Retry-After` hat kein Publikum (die
   429 trifft nur die eigene Login-Maske, und der Vater versucht es in fünf Sekunden erneut). Ein Token in
   einem Proxy-Cache ist dagegen ein echtes kleines Loch. Der `no-store`-Teil gehört **nicht** in den
   Dokument-Commit (er ändert das Dokument nicht) — eigener Commit in derselben Story.
4. **Das Bündel — vier Stories, ein Branch, EINE Regenerierung.** B4, B5 und die schon geschätzten
   [B-56](B-56-problemdetails-required-extensions.md) und [B-60](B-60-flags-enum-im-dokument.md) greifen alle
   in dieselbe Transformer-Kette und lassen alle `ContractDocumentTests` das 900-KB-Dokument neu schreiben.
   Getrennt gebaut sind das vier Commits mit je einem unlesbaren Riesendiff, in dem die eigentliche Änderung
   untergeht. **Reihenfolge:** B-60 (Schema-Ebene) → B-56 (`required` über alle Schemas, muss gegen ein
   Dokument prüfen, das B-60 schon enthält) → B4 (Operation-Ebene, additiv) → B5 (reiner Text) → **eine**
   Regenerierung als letzter, eigener Hunk.
   **Zusätzlicher Fund:** B-56 und B-60 beanspruchen **beide** „Punkt 5" derselben Testmethode
   `Vertragsdokument_BeschreibtDieLeitungWahrheitsgemaess`. Getrennt gebaut muss die zweite Story die erste
   umnummerieren, und ihr eigener Text veraltet dabei. Im Bündel werden es Punkt 5 bis 8 in einer
   Bearbeitung.
   **Auflage (sonst ist der Commit nicht abnahmefähig):** ein Branch mit **je einem Commit pro Story**, und
   **vor** der ersten Codezeile werden die Assertions der beiden `Defekt`-Stories einzeln gegen `HEAD`
   gefahren (`--filter ContractDocumentTests`) und die roten Läufe in deren `## Verlauf` protokolliert —
   nach der Regenerierung ist alles gleichzeitig grün und der Einzelnachweis „vorher rot" nicht mehr zu
   führen.
5. Vorschätzung für diese Story (B4+B5+`no-store`): **S**, `wo: backend`, keine Migration, kein
   Vertragsbruch.

## Schätzung

`groesse: S`, `wo: backend`, `migration: nein`, `vertragsbruch: nein` (additive Header-/Summary-
Deklarationen im OpenAPI-Dokument, kein bestehendes Feld ändert Form). Angriffsplan: drei neue
`AddOperationTransformer`-Registrierungen in `Program.cs` (401/403, `X-Total-Count`, 24 typspezifische
Summaries aus `ExerciseTypeManifest`) plus `[ResponseCache(NoStore = true, Location =
ResponseCacheLocation.None)]` an den drei Login-Actions und `GET auth/me`. Testweg: die bestehende
`ContractDocumentTests.Vertragsdokument_BeschreibtDieLeitungWahrheitsgemaess` um drei Punkte erweitert,
ein neuer `SecurityHardeningTests`-Fall für den Laufzeit-Header — siehe „Verlauf" 2026-08-07 für die
tatsächliche Umsetzung und die Messzahlen.

## Akzeptanzkriterien

1. Jede nicht-`[AllowAnonymous]`-Operation im Dokument deklariert `401` (und bei rollen-gegateten `403`) mit
   `ProblemDetails`; die Zahl steigt von 5 auf die tatsächliche Menge.
2. Jede Operation mit `skip`/`take` deklariert den Antwort-Kopf `X-Total-Count`.
3. Keine Operation im Dokument ohne `summary` — die 24 Übungs-Operationen tragen **typ-spezifische** Namen
   („Legt eine Vokabelübung an"), nicht zwölfmal denselben Satz.
4. Die drei Login-Actions und `GET auth/me` antworten mit `Cache-Control: no-store`.
5. Das Tor „keine Operation ohne `summary`" (Ausnahmeliste **leer**) steht — **nach** der Reparatur, siehe
   [B-101](B-101-fehlercodes-und-drei-waechter.md).
6. `ContractDocumentTests` bleibt grün, und das eingecheckte `v1.json` ist in **einem** Hunk gewachsen.

## Verlauf

- **2026-08-04** — angelegt aus `docs/api-design-bewertung.md` (B4, B5, B6) und der Arbeitsrunde. Die
  Herabstufung auf `Aufräumen` ist eine Selbstkorrektur des API-Designers gegen seine eigene Ankündigung;
  die Bündel-Auflage und der Fund zur doppelt beanspruchten Testnummer stammen aus Runde 2.
- **2026-08-07** — Grillen/Schätzen: die Bündel-Auflage (Punkt 4 oben) ist **gegenstandslos** — B-56 und
  B-60 sind seit 2026-08-06 beide `abgenommen`, B-100 läuft solo. Geschätzt **S**, `wo: backend`, keine
  Migration, kein Vertragsbruch — wie in der Arbeitsrunde vorgeschätzt.
- **2026-08-07** — umgesetzt, drei neue `AddOperationTransformer`-Registrierungen in `Program.cs` (Vorbild
  im Stil: der bestehende Fehlercode-Schema-Transformer):
  1. **401/403** (AC1): jede Operation ohne `[AllowAnonymous]` in `EndpointMetadata` bekommt `401`
     (`ProblemDetails`-Referenz); trägt zusätzlich ein `[Authorize(Roles=…)]` eine nicht-leere `Roles`, auch
     `403`. `EndpointMetadata` fasst Klassen- **und** Methodenattribute zusammen (MVC-Standardverhalten) —
     eine einzige Prüfung deckt beide Ebenen.
  2. **`X-Total-Count`** (AC2): jede Operation mit `skip`/`take`-Parameter bekommt den Header auf **jeder**
     2xx-Antwort (nicht auf Fehlerantworten).
  3. **24 Übungs-Summaries** (AC3, Entscheidung 2 — kein Ursachenforschung, direkt der „Notausgang"): für
     `POST`/`PUT` ohne `Summary`, deren Action von der generischen `ExerciseControllerBase<>` **deklariert**
     wird (nicht nur „letztes Routensegment trifft zufällig einen Namen" — nachträglich gehärtet, siehe
     unten), liest der Transformer das typ-spezifische `Label` aus `ExerciseTypeManifest` (dieselbe Quelle,
     die das Frontend für Anzeigenamen nutzt) und setzt „Legt eine neue {Label}-Übung an."/"Aktualisiert
     eine {Label}-Übung." — 12 unterschiedliche Texte statt einer generischen Zeile.
  4. **`Cache-Control: no-store`** (AC4, eigener Commit-Gedanke wie in Entscheidung 3 vorgesehen — hier im
     selben Commit belassen, da die Story ohnehin als Ganzes committet wird): `[ResponseCache(NoStore =
     true, Location = ResponseCacheLocation.None)]` an den drei Login-Actions und `GET auth/me`.
  Neue mechanische Tore (AC5/AC6) in `ContractDocumentTests.Vertragsdokument_BeschreibtDieLeitungWahrheitsgemaess`
  als Punkte 7–9 (kein Summary fehlt / jede paginierte Operation trägt den Header / 401 überall außer den
  zwei benannten Registrierungs-Ausnahmen), plus ein neuer Integrationstest
  `SecurityHardeningTests.Login_Und_Me_Tragen_CacheControlNoStore` für AC4 (nicht im JSON-Dokument sichtbar,
  reiner Laufzeit-Header).
  **Rote Proben** (vier gezielte Fehler-Injektionen, alle vor dem jeweiligen Fix zurückgesetzt):
  Ausnahme `POST creator/teacher-accounts` aus `Unauthorized401Exceptions` entfernt →
  `1 operations without 401: POST /api/v1/creator/teacher-accounts`; `[ResponseCache]` von `GetMe` entfernt
  → `GET auth/me must send Cache-Control: no-store.` Beide danach wiederhergestellt: grün.
  **Gemessen statt aus dem zwei Tage alten Bericht übernommen** (dieselbe Lehre wie B-121 am 2026-08-06):
  321/323 Operationen mit 401 (nicht „5 von 323" wie im Ist-Stand von 2026-08-04 — das war die Zahl VOR
  dieser Story), 278/323 mit 403, 0/323 ohne `summary`, 44 paginierte Operationen (nicht 31) alle mit
  `X-Total-Count`. `docs/openapi/v1.json` in einem Hunk gewachsen (5825 Einfügungen/149 Löschungen).
  Volle Suite: **761/761 grün** (758 vor diesem Sprint + 3 neue Tests: 2 neue Assertions zählen nicht als
  eigene Tests, sondern erweitern eine bestehende Methode).
- **2026-08-07** — `pugling-reviewer` gefahren: **kein Blocker**, ein 🟡-Fund (Segment-Matching in
  Transformer 3 war fragil: „letztes Segment == `AuthoringRoute`" ohne Prüfung, dass die Action wirklich
  von `ExerciseControllerBase<>` stammt — ein künftiger Nicht-Übungs-Endpunkt mit gleichnamigem Segment und
  ohne eigenes XML-Doc hätte still eine falsche, aber plausibel klingende Beschriftung bekommen). **Sofort
  behoben** (Freigabe 3): zusätzliche Prüfung auf `ControllerActionDescriptor.MethodInfo.DeclaringType`
  gegen die offene generische `ExerciseControllerBase<>`. Erneut verifiziert: weiterhin 0 fehlende
  Summaries, volle Suite weiterhin 761/761 grün. Review-Fund-Zähler dieses Sprints: **1** (behoben), weit
  unter der Fünf-Fehlversuche-Schwelle aus `docs/nachtlauf.md`.
- **2026-08-07** — Rollengang-Ersatz: kein UI-Kandidat (reine Dokument-/Header-Vervollständigung, additiv,
  kein Vertragsbruch für bestehende Clients). Ersatz nach `docs/nachtlauf.md`: die vier gezielten
  rot→grün-Belege, die volle Suite und der Reviewer; zusätzlich `npm run build` im Frontend gegen das
  regenerierte Dokument gefahren (grün) — der einzige tatsächliche Konsument des Dokuments außerhalb der
  Suite selbst.
- **2026-08-07** — `abgenommen`.
