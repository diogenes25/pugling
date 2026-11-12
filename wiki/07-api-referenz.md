---
tags: [typ/referenz, bereich/doku, rolle/creator, rolle/supervisor, rolle/student]
aliases: [API-Referenz, Endpunkt-Index]
---

# 07 · API-Referenz (Endpunkt-Index)

← [Zurück zum Wiki-Index](../README.md)

Kompakter Überblick über alle Routen. **Autoritative Quelle bleibt Swagger** (`/swagger` bzw.
`/openapi/v1.json`) — dort stehen die vollständigen Request/Response-Schemas. Alle Routen unter
`api/v1/…`. `V` = nur Erwachsene (Rolle Supervisor bzw. Creator), `S` = Sohn (Student), `A` = beide
(authentifiziert), `∅` = anonym.

> 🔗 **Wie die Endpunkte inhaltlich zusammenhängen** (Übung → Lehrplan → Kind → Auswertung), mit
> Datenfluss-Diagramm: [docs/endpunkt-beziehungen.md](../docs/endpunkt-beziehungen.md).

## Rollen-Schnelleinstieg

| Rolle | Route-Präfix | Doku-Einstieg |
| --- | --- | --- |
| **Creator** | `api/v1/creator/...` | [docs/rollen-doku.md · Creator](../docs/rollen-doku.md#creator--inhalte-und-katalog) |
| **Supervisor** | `api/v1/supervisor/...` | [docs/rollen-doku.md · Supervisor](../docs/rollen-doku.md#supervisor--steuerung-kontrolle-belohnung) |
| **Student** | `api/v1/student/...` | [docs/rollen-doku.md · Student](../docs/rollen-doku.md#student--lernen-fortschritt-einlösen) |

---

## Auth & Selbstauskunft (`api/v1/auth/…`, ohne Ebenen-Präfix)

| Rolle | Methode & Route | Zweck |
| --- | --- | --- |
| ∅ | `POST /auth/adult` | Login eines Erwachsenen – Vater wie Lehrer (adultId + pin) → JWT |
| ∅ | `POST /auth/child` | Sohn-Login (childId + pin) → JWT |
| ∅ | `POST /auth/login` | konto-zentrischer Login (accountId + pin) → ein Token mit **allen** Rollen des Kontos |
| A | `GET /auth/me` | Identität aus dem Token (`accountId, role, roles[], adultId, childId, name`) |
| V | `PATCH /auth/me` | eigenes Konto pflegen (Name, E-Mail, PIN) – jedes Erwachsenen-Konto, auch Lehrer |

## Supervisor — Personen, Punkte, Steuerung (`api/v1/supervisor/…`)

| Rolle | Methode & Route | Zweck |
| --- | --- | --- |
| ∅ | `POST /adults` | Vater registrieren |
| V | `GET /adults` · `GET/PATCH/DELETE /adults/{adultId}` | eigener Erwachsenen-Datensatz |
| V | `GET/POST /children` · `GET/PATCH/DELETE /children/{childId}` | Kinder verwalten (inkl. Salden `coins`/`gems`) |
| V | `GET /children/daily-overview[?date=]` | Tagesstatus aller eigenen Kinder |
| V | `GET/POST /children/{childId}/supervisors` · `DELETE …/{supervisorId}` | **Multi-Supervisor**: weitere Betreuer (Mutter/Oma) verknüpfen; der letzte bleibt |
| V | `GET/POST /children/{childId}/points` | Punkte-Ledger lesen bzw. manuell buchen `{ amount, reason, currency }` |
| V | `GET/PUT /children/{childId}/interests` · `PUT/DELETE …/{tagId}` | gewichtete Interessen des Kindes |
| V | `GET/POST /children/{childId}/textbooks` · `GET/PATCH/DELETE …/{textbookId}` | Schulbücher des Kindes |
| V | `GET/POST /children/{childId}/timetable` · `DELETE …/{entryId}` | Stundenplan (Fach × Wochentag) |
| V | `GET/POST /children/{childId}/missions` · `PATCH/DELETE …/{missionId}` | Missionen definieren |
| V | `GET/POST /children/{childId}/achievements` · `PATCH/DELETE …/{achievementId}` | Auszeichnungen definieren |
| A/V | `GET/POST /children/{childId}/objectives` · `GET/PATCH/DELETE …/{objectiveId}` | große Lernziele (Objectives), live ausgewertet; schreiben nur Supervisor |
| V | `POST …/objectives/{objectiveId}/key-results` · `PATCH/DELETE …/key-results/{keyResultId}` | Key Results (Meilensteine) eines Lernziels |
| V | `GET/POST /shop/articles` · `GET/PATCH/DELETE …/{articleId}` · `GET/POST …/{articleId}/listings` · `GET/PATCH/DELETE …/listings/{listingId}` | Familien-Shop: Artikel und Angebote |
| V | `GET /children/{childId}/shop/inventory` · `…/purchases` · `POST …/purchases/{purchaseId}/cancel` | Bestand und Käufe des Kindes |
| V | `GET /children/{childId}/shop/activations` · `POST …/{requestId}/approve` · `…/reject` | Aktivierungsanfragen bearbeiten |

## Student — eigene Sicht (`api/v1/student/…`)

| Rolle | Methode & Route | Zweck |
| --- | --- | --- |
| S | `GET /me/points` · `GET …/entries` · `GET …/entries/{entryId}` | Kontostand (Salden) + Buchungen (Liste, paginiert, + Einzeln) |
| S | `GET /me/missions` · `GET …/{missionId}` | eigene Missionen mit Fortschritt |
| S | `GET /me/achievements` · `GET …/{achievementId}` | eigene Auszeichnungen |
| S | `GET /me/objectives` · `GET …/{objectiveId}` | eigene aktive Lernziele mit Key-Result-Fortschritt |
| S | `GET /me/skins` · `POST …/{skinId}/purchase` · `POST …/{skinId}/equip` | Skins (Gems) |
| S | `GET /me/shop` · `…/inventory[?skip=&take=]` · `…/activations[?status=]` | Familien-Shop-Sicht (`coins/gems/available/inventory/purchases`) + eigener Bestand + eigene Aktivierungen |
| S | `POST /me/shop/listings/{listingId}/purchase` | Shop-Angebot kaufen (bucht Münzen ab → Inventar) |
| S | `POST /me/shop/inventory/{articleId}/activate` | Aktivierungsanfrage stellen `{ quantity }` |
| S | `GET/PUT /me/interests` · `POST /me/interests/wishes` · `GET /interest-tags` | eigene Interessen, freie Wünsche, für Kinder freigegebene Schlagworte |
| A | `POST /children/{childId}/media-picks/reshuffle` | anderes Bild ziehen |
| A | `GET /children/{childId}/vocabulary-progress` · `…/{itemId}` · `…/{itemId}/history` · `…/by-word` | Vokabel-Lernstand des Kindes, **flach** (Item + Wort-Rollup + Historie) |
| A | `GET /children/{childId}/learn/subjects[/{s}]` · `…/{s}/series-units` · `…/{s}/series-units/{u}/vocabulary[/{exerciseId}/items]` | Vokabel-Lernstand **hierarchisch** (Fach→Unit→Übung→Item, Aggregate + `active`-Flag; `search`/`sort`/`active`/Paging) |

## Lern-Katalog (global, Rolle Creator; alle Routen unter `api/v1/creator/…`)

| Methode & Route | Zweck |
| --- | --- |
| `GET/POST /subjects` · `GET/PATCH/DELETE /subjects/{subjectId}` | Fächer |
| `GET/POST /subjects/{s}/categories` · `…/{categoryId}` | fachabhängige Arten |
| `GET/POST /publishers` · `…/{id}` | Verlage |
| `GET/POST /textbook-series` · `GET/PATCH/DELETE …/{seriesId}` | Lehrwerk-Reihen (slug-idempotent) |
| `GET/POST /textbook-series/{seriesId}/units` · `…/{unitId}` | Units (Band/Unit, Topics/Grammar/VocabularyNotes) |
| `GET/POST /grammar-topics` · `GET/PATCH/DELETE …/{id}` | Grammatik-Themen (slug-idempotent je Fach) |
| `GET /exercise-types` · `…/{type}` | **Typ-Manifest** (einzige Quelle für Typ, Anzeigename, Routen-Segment) |
| `GET /exercises?subjectId=&grade=&schoolType=&categoryId=&type=&search=` · `GET …/{id}` · `GET …/{id}/usage` | **Übungssuche** (Vorfilterung), typübergreifende Sicht, Verwendung (Pläne/Klassenarbeiten/Lernziele) |
| `GET/POST/PUT/DELETE /textbook-series/{s}/units/{u}/<typ>[/{exerciseId}]` | Übungs-CRUD je Typ (12 Typen, siehe [03](03-uebungstypen.md)) |
| `GET/POST …/vocabulary/{id}/items` · `GET/PATCH/DELETE …/items/{itemId}` | Vokabel-Items einer Übung |
| `POST …/vocabulary/{id}/refs-from-tags` | Vokabel-Items per Tag-Snapshot setzen |
| `POST …/matching/{id}/check` · `POST …/arithmetic/{id}/check` · `POST …/list/{id}/check` | Auswertung |
| `POST …/arithmetic-drill/{id}/generate` · `POST …/arithmetic-drill/{id}/check` | Zufallsaufgaben erzeugen/prüfen |
| `POST/DELETE …/birkenbihl/{id}/sentences[/{sentenceId}]` · `PUT …/words/{wordId}` · `GET …/words/{wordId}/candidates` · `POST /birkenbihl/decode` | Birkenbihl: Sätze dekodieren, Wort-Bedeutung tauschen, Vorschau |
| `GET /exercises/{id}/preview` · `POST …/preview/check` | Testmodus ohne Nebenwirkung |
| `PATCH /exercises/{id}/sharing` · `GET/POST/DELETE /exercises/{id}/grants[/{creatorId}/{permission}]` | RWX-Freigaben; den aktuellen Stand (`executePublic`) liest `GET /exercises/{id}`, einen GET auf `…/sharing` gibt es nicht |
| `GET /exercises/{id}/ratings` · `PUT/DELETE /exercises/{id}/rating` | Bewertungen durch andere Creator (die eigene Übung ist nicht bewertbar) |
| `GET/POST /profiles` · `…/{profileId}` · `GET /profiles/match?childId=` | Creator-Profile („Fachlehrer") und Treffer je Kind |
| `GET/POST /interest-tags` · `…/{id}` | Interessen-Taxonomie (slug-idempotent; ein Label, das schon ein anderes Tag trägt, wird beim Anlegen und Umbenennen abgewiesen – exakter Vergleich, Prüfung ohne Index; `visibleToChildren` gibt ein Tag für die Kinder-Auswahl frei) |
| `POST /teacher-accounts` (anonym) · `GET …/{creatorId}` | Lehrer-Konto (nur Creator) anlegen/lesen |

**Übungstyp-Pfade:** `/vocabulary` `/reading` `/cloze` `/essays` `/listening` `/grammar` `/matching`
`/translation` `/arithmetic` `/arithmetic-drill` `/list` `/birkenbihl`.

## Stores & Medien (global, Rolle Creator)

| Methode & Route | Zweck |
| --- | --- |
| `GET/POST /creator/vocabulary` · `GET/PATCH/DELETE …/{id}` · `GET …/by-key/{key}` | Vokabel-Store (`POST` mit `reuseExisting`: der vorhandene Eintrag unter dem erzeugten Schlüssel statt einer `_2`-Kopie) |
| `POST/PATCH /creator/vocabulary/batch` · `POST …/lookup` | Massen-Anlage (idempotent) bzw. Massen-Ergänzung; Existenzprüfung vor dem Anlegen |
| `GET …/vocabulary/{id}/forms` · `GET …/{id}/usage` | Formen-Familie (go → went → gone) und Verwendung in Übungen |
| `GET/POST /creator/vocabulary/tags` · `PATCH/DELETE …/tags/{id}` · `POST …/{vocabularyId}/tags` · `DELETE …/{vocabularyId}/tags/{tagId}` | Vokabel-Tags |
| `GET/POST /creator/cloze-texts` · `GET …/{id}` · `GET …/by-key/{key}` · `PATCH/DELETE …/{id}` | Lückentext-Store |
| `GET/POST /creator/media` · `GET/PATCH/DELETE …/{id}` · `GET …/by-key/{key}` · `POST …/upload` · `GET …/{id}/usage` · `GET …/demand` | Bild-Assets, Verwendung, Vokabeln ohne Bild |
| `GET/POST /creator/media/{assetId}/variants` · `PATCH/DELETE …/{variantId}` · `POST/DELETE …/{id}/tags[/{tagId}]` | Varianten, Interessen-Tags |
| `…/vocabulary/{id}/media` · `…/exercises/{id}/media` · `…/exercises/{id}/items/{itemId}/media` | Bild-Verknüpfungen |

## Study-Plans (Training)

Verwaltung unter `api/v1/supervisor/…`, Spielen unter `api/v1/student/…`:

| Rolle | Methode & Route | Zweck |
| --- | --- | --- |
| A | `GET /supervisor/study-plans?childId=` · `GET /supervisor/study-plans/{planId}` | Pläne lesen (Sohn nur eigene) |
| V | `POST /supervisor/study-plans` · `PATCH/DELETE /supervisor/study-plans/{planId}` | Plan anlegen/ändern/löschen |
| V | `GET/POST /supervisor/study-plans/{planId}/positions` · `GET/PATCH/DELETE …/{positionId}` | Übungen als PlanPositionen verwalten |
| V | `GET /supervisor/study-plans/{planId}/positions/{positionId}/report` | Mastery und Testhistorie pro Position — **Vater-only**, jede Zeile trägt die Lösung |
| S | `GET /student/study-plans` | die spielbaren Pläne des Kindes (praktisch genau einer) |
| A | `GET /student/study-plans/{planId}/overview` | Tagesmission und aktueller Status |
| A | `GET /student/study-plans/{planId}/overview/progress?from=&to=&dutyDone=&sort=&skip=&take=` | Tag-für-Tag-Fortschritt (Filter/Sort/Paging) |

### Üben (Practice) — `api/v1/student/study-plans/{planId}/positions/{positionId}/…`

| Rolle | Methode & Route | Zweck |
| --- | --- | --- |
| A | `POST …/practice-sessions` · `GET …/practice-sessions/{sid}` | Sitzung starten bzw. lesen |
| A | `GET …/practice-sessions/{sid}/cards` | alle Karten in eingefrorener Reihenfolge (Info-Modus, ohne Lösung) |
| A | `GET …/practice-sessions/{sid}/next` | aktuelle Karte am Cursor (Lern-Modus, one-at-a-time) |
| A | `POST …/practice-sessions/{sid}/review` | Antwort abgeben (server-autoritativ) |
| A | `POST …/practice-sessions/{sid}/cards/{itemIndex}/image/reshuffle` | anderes Bild für eine Karte |
| A | `POST …/practice-sessions/{sid}/heartbeat` | aktive Sekunden zählen |
| A | `POST …/practice-sessions/{sid}/end` | Sitzung beenden |

### Abschlusstests — gleiches Präfix

| Rolle | Methode & Route | Zweck |
| --- | --- | --- |
| A | `POST …/tests` | Testversuch starten |
| A | `GET …/tests/{attemptId}` | Testversuch lesen |
| A | `GET …/tests/{attemptId}/next` · `POST …/tests/{attemptId}/answer` | Klausur-Modus: eine Frage nach der anderen, ohne Rückmeldung zur Richtigkeit |
| A | `POST …/tests/{attemptId}/submit` | Abschließen, serverseitig bewerten |

## Tags & Klassenarbeiten

| Rolle | Methode & Route | Zweck |
| --- | --- | --- |
| A | `GET/POST /creator/tags` · `PATCH/DELETE …/{tagId}` | Tags je Kind (Sohn nur eigene) |
| A | `GET/POST /creator/tags/{tagId}/exercises` · `DELETE …/{exerciseId}` · `GET …/for-exercise/{exerciseId}` | Übungen taggen |
| A | `GET/POST /creator/tags/{tagId}/vocabulary` · `DELETE …/{vocabularyId}` · `GET …/for-vocabulary/{vocabularyId}` | Vokabeln taggen (die Liste nur für Erwachsene) |
| A/V | `GET/POST /supervisor/class-tests` · `GET/PATCH/DELETE …/{id}` | Klassenarbeiten planen/benoten (schreiben nur Supervisor) |
| V | `POST …/{id}/exercises` · `DELETE …/{id}/exercises/{exerciseId}` · `POST/DELETE …/{id}/tags/{tagId}` | Übungen direkt bzw. per Tag zuordnen |
| A | `GET …/{id}/practice` · `GET /supervisor/class-tests/repeat?minBadGrade=` | gezielt üben / schlecht benotete wiederholen |

Details: [docs/klassenarbeiten-tagging.md](../docs/klassenarbeiten-tagging.md).

## Anmerkungen beim Testen (`api/v1/remarks`)

**Dev-Werkzeug, kein Produktfeature** – und die einzige Ressource neben `auth/…` **ohne** Ebenen-Präfix:
Sie gehört keiner Ebene, weil derselbe Mensch sie aus Vater-Web und Sohn-Arcade erfasst.

| Methode & Route | Rolle | Zweck |
| --- | --- | --- |
| `POST remarks` | A | Beobachtung erfassen (Widget, Alt+A); Antwort trägt die **Log-Id** |
| `GET remarks` | A | Liste; `?mine=true` nur eigene, dazu `status`/`category`/`childId`/`appArea` |
| `GET remarks/{id}` | A | Einzelne Anmerkung samt Kontext-Schnappschuss |
| `PATCH remarks/{id}` | A | Text/Kategorie/Status und die Antwort (`answer`) |
| `DELETE remarks/{id}` | A | Löschen |
| `GET/POST remarks/{id}/comments` · `DELETE …/{commentId}` | A | Verlauf einer Anmerkung (ein Eintrag des Menschen öffnet eine erledigte wieder) |
| `GET remarks/export` | V | Markdown-Schnappschuss (`text/markdown`) |

**Sichtbarkeit** (inline getrennt, nicht per `[Authorize]`): Ein Sohn sieht **ausschließlich eigene**
Anmerkungen – Antworten tragen Datei-/Zeilenverweise. Ein Vater sieht zusätzlich die der betreuten Kinder.

Details: [docs/anmerkungen-plan.md](../docs/anmerkungen-plan.md) ·
Exporte: [docs/anmerkungen/](../docs/anmerkungen/README.md).

---

## Fehlerformat

Alle Fehler sind `ProblemDetails` (RFC 7807) mit einem zusätzlichen, **maschinenlesbaren
`code`** und einem stabilen `type`-URI (`https://pugling.app/errors/{code}`):

```json
{
  "type": "https://pugling.app/errors/insufficient_gems",
  "title": "Not enough gems.",
  "status": 400,
  "detail": "Not enough gems: 120/2000 for 'ninja'.",
  "code": "insufficient_gems",
  "traceId": "00-…"
}
```

Der `code` ist **stabiler Vertragsbestandteil** – der Client verzweigt/lokalisiert darauf, nicht auf
dem englischen `detail`-Freitext. Die Codes stammen aus der zentralen Registry
[`Errors/ApiErrors.cs`](../backend/Pugling.Api/Errors/ApiErrors.cs); das OpenAPI-Dokument führt sie als
`enum` der `code`-Property (sichtbar in Swagger `/swagger`). Neue Codes werden nur additiv ergänzt.

**Generische, status-getriebene Codes:** `validation_error`/`bad_request`/`invalid_reference`/`unknown_field` (400),
`unauthorized`/`invalid_credentials` (401), `forbidden`/`not_author`/`not_owner` (403), `not_found` (404),
`conflict`/`concurrency_conflict` (409), `rate_limited` (429), `internal_error` (500).

**Fachliche Codes (Auswahl):** `skin_already_unlocked`, `skin_not_unlocked`, `insufficient_gems`,
`insufficient_coins`, `shop_insufficient_stock`, `insufficient_inventory`, `purchase_not_open`, `duplicate_key`,
`duplicate_tag_name`, `exercise_in_use`, `exercise_items_locked`, `vocabulary_in_use`, `position_has_data`, `plan_inactive`,
`test_already_submitted`, `no_checkable_content`, `timetable_slot_taken`.

Verifizierte Beispiel-Requests/-Responses (Erfolg **und** jeder erreichbare Fehler-Code, erzeugt gegen
die geseedete API) liegen unter [docs/api-examples/](../docs/api-examples/index.md); sie werden vom
Integrationstest `DocsCaptureTests` erzeugt und dabei auf Status + `code` geprüft.
