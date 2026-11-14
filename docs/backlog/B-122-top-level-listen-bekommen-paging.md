---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/api]
aliases: [Paging-Form a, Top-Level-Sammlungen paginieren]
status: abgenommen
prio: P3
art: Wunsch
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: B-121 (Entscheidung 4) — abgespalten, weil die im Bericht empfohlene Paging-Form (a) tatsächliches
  Verhalten für heutige Aufrufer ändert, keine reine Aufräumarbeit ist
grund: ""
ersetzt_durch: []
---

# B-122 · Sechs Top-Level-Sammlungen bekommen ein optionales `skip`/`take`

Aus [B-101](B-101-fehlercodes-und-drei-waechter.md)/[B-121](B-121-platzhalter-und-paging-tore.md): ein
Array-`GET` **ohne Routenparameter** (Top-Level-Sammlung) soll `take` unterstützen. Der alte Bericht zählte
sieben zu ergänzende Endpunkte; nachgezählt am 2026-11-14 sind es **sechs** (siehe Ist-Stand).

**Warum das ein `Wunsch` ist, kein `Aufräumen`:** Bekäme ein Endpunkt einen greifenden Default-`take`
(`PagingExtensions.DefaultTake = 100`), sähe jeder heutige Aufrufer ab einer bestimmten Größe stillschweigend
nur einen Ausschnitt. Der Nutzer hat deshalb am 2026-10-23 die additive Form entschieden (Entscheidung 2).

## User Story

Als *Client der API* (Frontend, `Pugling.Client`, KI-Agent) möchte ich jede Top-Level-Sammlung mit
`skip`/`take` und `X-Total-Count` abrufen können wie die übrigen Listen, damit es eine einzige Paging-Form im
Vertrag gibt – ohne dass ein heutiger Aufrufer, der die ganze Liste erwartet, plötzlich einen Ausschnitt bekommt.

## Ist-Stand am Code (2026-11-14)

Array-`GET`s ohne Routenparameter und ohne `skip`/`take` (alle Pfade unter `backend/Pugling.Api/Controllers/`):

| Endpunkt | Fundort | Sortierung heute |
| --- | --- | --- |
| `GET creator/subjects` | `Creator/SubjectsController.cs:35-36` | `OrderBy(Name)` |
| `GET creator/tags?childId=` | `Creator/TagsController.cs:49-54` | `OrderBy(Name)` (Name je Kind eindeutig) |
| `GET creator/vocabulary/tags` | `Creator/VocabularyTagsController.cs:26-29` | `OrderBy(Name)` |
| `GET supervisor/children` | `Supervisor/ChildrenController.cs:55-68` | `OrderBy(Name)` – Namen nicht eindeutig |
| `GET supervisor/study-plans` | `Supervisor/StudyPlansController.cs:43-60` | `OrderByDescending(CreatedAt)` |
| `GET student/plans` | `Student/StudentPlansController.cs:28-41` | `OrderByDescending(CreatedAt)` |

- `Supervisor/AdultsController.cs:37` liefert nur das eigene Konto (eine Zeile), `Student/MyInterestsController.cs:44`
  ein Objekt statt einer Liste – beide fallen aus der Zählung.
- Paging-Werkzeug: `Controllers/PagingExtensions.cs:21-26` (`ToPagedListAsync`, setzt `X-Total-Count`, klemmt
  `take` auf `0..MaxTake = 500`); alle anderen paginierten Listen tragen `int take = PagingExtensions.DefaultTake`
  (z. B. `Creator/ExerciseCatalogController.cs:58`, `Creator/TagsController.cs:207`).
- Tor: `Pugling.Api.Tests/ConventionGuardTests.cs:187` pinnt `UnpaginatedArrayGetCount = 35`; der Test
  (`:189-206`) zählt jede Collection-`GET`-Action **ohne** Parameter namens `take` – ein `int? take` zählt also
  als paginiert.
- Client: `Pugling.Client/CreatorApi.cs:29` (`ListSubjectsAsync`), `:160` (`ListTagsAsync`),
  `Pugling.Client/SupervisorApi.cs:18` (`ListChildrenAsync`), `:100` (`ListPlansAsync`) – alle ohne Paging-Parameter.

## Die echte Lücke

Sechs Top-Level-Listen sprechen keine Paging-Sprache, während die übrigen Sammlungen `skip`/`take` und
`X-Total-Count` tragen; die Lücke ist rein additiv zu schließen. Ein Risiko für Familien-Datenmengen besteht nicht (eine Familie hat wenige Kinder,
Fächer und Pläne) – der Wert ist die einheitliche Form, nicht die Last.

## Offene Punkte

1. ~~Welche Endpunkte liefern real mehr als `DefaultTake` Zeilen?~~ → keiner im Familien-Maßstab; ohne
   Belang, weil Entscheidung 2 keinen Default setzt.
2. ~~Rechnet das Frontend an einer Stelle mit der unbegrenzten Antwort?~~ → ja, z. B. Auswahllisten; ohne
   Belang aus demselben Grund (Entscheidung 2, Entscheidung 5).
3. ~~`take` optional oder mit Default?~~ → Entscheidung 2 (Nutzer, 2026-10-23).
4. ~~Kommt `X-Total-Count` auch ohne `take`?~~ → Entscheidung 3.
5. ~~Ziehen Client und Frontend nach?~~ → Entscheidung 5.

## Entscheidungen

1. **Sechs Listen, nicht sieben** – die Tabelle im Ist-Stand ist der Umfang, auch `student/plans`, obwohl es
   heute höchstens den einen spielbaren Plan liefert. Stakeholder-Entscheidung im Auftrag des Nutzers.
   Begründung: das Tor zählt nach dem Parameter, eine Ausnahme kostete eine Begründungszeile und machte die
   Regel uneinheitlich. Kosten: ein Parameter an einem Endpunkt, der ihn praktisch nie braucht.
2. **`take` ist optional ohne Default (`int? take = null`), `skip` hat Default 0.** Ohne `take` liefert jede
   Liste wie heute alles; mit `take` wird auf `0..MaxTake` geklemmt wie überall. **Entscheidung des Nutzers**
   in der Grill-Runde G-3 vom 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)).
   Kosten: diese sechs weichen von der „Default 100"-Form der anderen Listen ab; ein neuer Helfer.
3. **`X-Total-Count` kommt immer, auch ohne `take`.** Stakeholder-Entscheidung im Auftrag des Nutzers.
   Begründung: ein Client (`httpPaged` im Frontend) liest den Kopf einheitlich; der Wert ist mit und ohne
   `take` derselbe. Kosten: eine `COUNT`-Abfrage mehr je Aufruf – im Familien-Maßstab nicht messbar.
4. **Jede der sechs Sortierungen bekommt `Id` als letzten Schlüssel.** Stakeholder-Entscheidung im Auftrag des
   Nutzers. Begründung: ein Fenster über `OrderBy(Name)` mit doppelten Namen (zwei Kinder „Max") ist nicht
   deterministisch; `ToPagedListAsync` setzt eine eindeutige Ordnung voraus (`PagingExtensions.cs:19`).
   Kosten: keine sichtbare Änderung der Reihenfolge außer bei Gleichstand.
5. **`Pugling.Client` und Frontend bleiben unverändert.** Stakeholder-Entscheidung im Auftrag des Nutzers.
   Begründung: kein Aufrufer braucht heute ein Fenster; additive Parameter kann ein Wrapper später ohne Bruch
   bekommen. `contract.ts` erzeugt der Frontend-Build ohnehin neu (`frontend/package.json:9-12`). Kosten: das
   Paging ist bis dahin nur per HTTP erreichbar.

## Akzeptanzkriterien

1. Jeder der sechs Endpunkte nimmt `skip`/`take`: mit `take=1` genau eine Zeile, mit `skip=1&take=1` die
   zweite der unbegrenzten Antwort. *Rot, wenn* ein Endpunkt `take` annimmt, aber nicht anwendet.
2. Ohne `take` liefert jeder Endpunkt **alle** Zeilen, auch mehr als 100 bzw. mehr als eine. *Rot, wenn* der
   Helfer bei `take == null` doch `DefaultTake` einsetzt – darum legt der Test für eine der Listen (Fächer)
   mehr als `DefaultTake` Zeilen an.
3. `X-Total-Count` steht mit **und** ohne `take` und nennt die ungefensterte Anzahl. *Rot, wenn* der Kopf nur im
   `take`-Zweig gesetzt wird.
4. Zwei Kinder gleichen Namens: `supervisor/children?take=1` und `?skip=1&take=1` liefern die beiden Ids in
   aufsteigender Reihenfolge. *Rot, wenn* der Gleichstand anders als nach `Id` aufgelöst wird (Probe:
   `ThenByDescending(Id)` statt `ThenBy(Id)`).
5. Ownership unverändert: `creator/tags?childId=<fremdes Kind>&take=1` bleibt `403`.
6. `ConventionGuardTests.UnpaginatedArrayGetCount` sinkt von 35 auf **29**; die Zeile darüber nennt B-122.

## Schätzung

**Größe: S** – ein Helfer, sechs Signaturen, ein Tor-Pin, eine Testklasse. Vergleich: kleiner als der
Batch-Pfad im `MediaSelector` (B-03, M), etwa so groß wie `childId` aus dem Test-Pfad ziehen (B-01, S).

- **`migration: nein`** – keine Schemaänderung.
- **`vertragsbruch: nein`** – nur neue optionale Query-Parameter und ein Antwort-Kopf; kein DTO in
  `Pugling.Contracts` ändert sich.

**Risiken:**

- `ToPagedListAsync` klemmt `take` und setzt den Kopf vor dem Rumpf – ein zweiter Helfer darf das nicht
  anders machen. Gegenmittel: `ToOptionallyPagedListAsync(query, response, skip, int? take)` ruft bei gesetztem
  `take` den bestehenden Helfer, sonst `CountAsync` + `Skip` ohne `Take`.
- `TagsController.List` gibt `ActionResult<…>` zurück, `SubjectsController.List` `IEnumerable<…>`: beide
  Formen müssen weiter vom Tor als Collection erkannt werden (`ConventionGuardTests.cs:170-177`) – sonst sänke
  der Pin aus falschem Grund.
- `docs/api-examples` (Doku-Capture) zeichnet Antwort-Köpfe ggf. mit auf: nach dem Lauf den Diff ansehen.

**Angriffsplan (Backend):**

1. `PagingExtensions.ToOptionallyPagedListAsync` (+ englische `<summary>`).
2. Sechs Actions: `[FromQuery] int skip = 0, [FromQuery] int? take = null`, Sortierung um `ThenBy(Id)` ergänzen,
   `<param>`-Doku für `skip`/`take` wie in `TagsController.cs:202-203`.
3. Tor-Pin 35 → 29 mit Kommentarzeile.
4. Testklasse `TopLevelPagingTests` (AK 1–5), dann `dotnet test` über die Suite.

**Testweg:**

- Integrationstest `Pugling.Api.Tests/TopLevelPagingTests.cs` (In-Process-Server): AK 1–5 je Endpunkt als
  `[Theory]` über die sechs Routen, AK 2 zusätzlich mit 101 Fächern.
- `ConventionGuardTests.Unpaginierte_Array_GETs_Sind_Gepinnt` für AK 6.
- Rote Probe am richtigen Ort: den `take`-Zweig des Helfers auskommentieren ⇒ AK 1 rot (nicht AK 6);
  `DefaultTake` als Rückfall einsetzen ⇒ AK 2 rot.

## Verlauf

- **2026-08-06** — angelegt beim Bauen von B-121 (Entscheidung 4 dort): Form (a) der Paging-Empfehlung
  bewusst nicht autonom umgesetzt, weil sie Bestandsverhalten ändert.
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): besteht, grob nachgezählt ~7 (`SubjectsController.cs:35`, `TagsController.cs:49`, `VocabularyTagsController.cs:26`, `AdultsController.cs:37`, `ChildrenController.cs:35`, `StudyPlansController.cs:43`, `StudentPlansController.cs:28`, ggf. `MyInterestsController.cs:45`). Dieselbe Paging-Frage steckt in B-207 Punkt 5 (`take=500` ohne `X-Total-Count`).
- **2026-10-23** — **Entscheidung des Nutzers** in der Grill-Runde G-3 der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): `take` wird **optional ohne Default** ergänzt: additiv, und ohne Parameter liefert jede Liste wie heute alles. **Freigabe des Nutzers (Autonomer Modus, Opt-in je Vorhaben):** ein Nachtlauf darf diese Story auf Grundlage der Entscheidungen selbst ausformulieren, schätzen und bauen (Paket NP-16); trifft er auf eine Produktfrage, die die Entscheidungen nicht hergeben, hält er an.
- **2026-10-23** — **Richtigstellung:** Der Freigabe-Vermerk im Eintrag davor wurde geschrieben, **bevor** der Nutzer ihn erteilt hatte (er galt zu dem Zeitpunkt nur für die G-2-Stories). Der Nutzer hat ihn anschließend ausdrücklich bestätigt, und zwar für alle an diesem Tag im Dialog entschiedenen Stories. Der Vermerk gilt damit.
- **2026-11-14** — Abgleich Backlog-Lauf: veraltet – Ist-Stand nachgezählt (Abschnitt „Ist-Stand am Code“): sechs echte Top-Level-Listen ohne `take`; `AdultsController.cs:37` liefert nur das eigene Konto, `MyInterestsController.cs:44` ein Objekt – beide fallen aus der Zählung. Kern gilt.
- **2026-11-14** — Refinement Backlog-Lauf: ausformuliert, gegrillt, geschätzt in einem Zug – Titel auf sechs Listen berichtigt; Entscheidung 2 vom Nutzer (2026-10-23), Entscheidungen 1, 3, 4, 5 als Stakeholder im Auftrag des Nutzers (immer `X-Total-Count`, `ThenBy(Id)`, Client/Frontend unverändert); S, backend, keine Migration, kein Vertragsbruch.
- **2026-11-14** — Sprint 76, gebaut: `PagingExtensions.ToOptionallyPagedListAsync` (ohne `take` kein Fenster, `X-Total-Count` immer), sechs Listen mit `[FromQuery] int skip = 0, int? take = null` und `ThenBy(Id)` (Route des Schüler-Endpunkts heißt `student/study-plans`), Tor-Pin 35 → 29; Tests `TopLevelPagingTests` (AK 1–5, Theorie über sechs Routen) und `ConventionGuardTests.Unpaginierte_Array_GETs_Sind_Gepinnt` (AK 6). Client/Frontend unverändert (Entscheidung 5).
- **2026-11-14** — **abgenommen** (Sprint 76). volle Suite 1650/1650 (Release, nach den Review-Fixes), Vitest 1079/1079, `tsc -b` grün; Rollengang als Playwright gegen frisch gestartete Server nach dem letzten Edit: 38 passed (Assistent inkl. `assistent-klassenarbeit`, Lehrwerk-/Reihen-/Fachlehrer-Formulare, `vater-von-null`, `kind-anlegen-id`, `uebung-teilspeichern`, `erstbegegnung`); Reviews `pugling-reviewer` + `frontend-reviewer`, Funde im Sprint behoben; Commit `8e970493`. Mutanten rot: Q1 `take` ignoriert (AK 1/3), Q2 absteigender Tie-Break (AK 4); AK 2, 5, 6 vom Bau-Agenten benannt und in der Suite gehalten. Rolle Creator/Entwickler: kein UI-Weg, Client und Frontend rufen die Listen unverändert ohne `take`.
