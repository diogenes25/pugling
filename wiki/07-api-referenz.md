---
tags: [typ/referenz, bereich/doku, rolle/creator, rolle/supervisor, rolle/student]
aliases: [API-Referenz, Endpunkt-Index]
---

# 07 · API-Referenz (Endpunkt-Index)

← [Zurück zum Wiki-Index](../README.md)

Kompakter Überblick über alle Routen. **Autoritative Quelle bleibt Swagger** (`/swagger` bzw.
`/openapi/v1.json`) — dort stehen die vollständigen Request/Response-Schemas. Alle Routen unter
`api/v1/…`. `V` = Vater-only, `S` = Sohn, `A` = beide (authentifiziert), `∅` = anonym.

> 🔗 **Wie die Endpunkte inhaltlich zusammenhängen** (Übung → Lehrplan → Kind → Auswertung), mit
> Datenfluss-Diagramm: [docs/endpunkt-beziehungen.md](../docs/endpunkt-beziehungen.md).

## Rollen-Schnelleinstieg

| Rolle | Route-Präfix | Doku-Einstieg |
| --- | --- | --- |
| **Creator** | `api/v1/creator/learn/...` | [docs/rollen-doku.md · Creator](../docs/rollen-doku.md#creator--inhalte-und-katalog) |
| **Supervisor** | `api/v1/supervisor/...` | [docs/rollen-doku.md · Supervisor](../docs/rollen-doku.md#supervisor--steuerung-kontrolle-belohnung) |
| **Student** | `api/v1/student/...` | [docs/rollen-doku.md · Student](../docs/rollen-doku.md#student--lernen-fortschritt-einlösen) |

---

## Auth & Selbstauskunft

| Rolle | Methode & Route | Zweck |
| --- | --- | --- |
| ∅ | `POST /auth/father` | Vater-Login (fatherId + pin) → JWT |
| ∅ | `POST /auth/child` | Sohn-Login (childId + pin) → JWT |
| A | `GET /auth/me` | Identität aus dem Token |
| S | `GET /me/points` · `GET …/entries` · `GET …/entries/{entryId}` | Kontostand (Salden) + Buchungen (Liste, paginiert, + Einzeln) |
| S | `GET /me/missions` · `GET …/{missionId}` | eigene Missionen mit Fortschritt (Liste, paginiert, + Einzeln) |
| S | `GET /me/achievements` · `GET …/{achievementId}` | eigene Auszeichnungen (Liste, paginiert, + Einzeln) |
| S | `GET /me/shop` · `…/inventory[?skip=&take=]` · `…/activations[?status=]` | Familien-Shop-Sicht (`coins/gems/available/inventory/purchases`) + eigener Bestand + eigene Aktivierungen |
| S | `POST /me/shop/listings/{listingId}/purchase` | Shop-Angebot kaufen (bucht Münzen ab → Inventar) |
| S | `POST /me/shop/inventory/{articleId}/activate` | Aktivierungsanfrage stellen `{ quantity }` |

## Admin — Personen & Punkte

| Rolle | Methode & Route | Zweck |
| --- | --- | --- |
| ∅ | `POST /fathers` | Vater registrieren |
| V | `GET/PATCH/DELETE /fathers/{fatherId}` · `GET /fathers` | eigener Vater-Datensatz |
| V | `GET/POST /children` · `GET/PATCH/DELETE /children/{childId}` | Kinder verwalten (inkl. `pointsBalance`) |
| V | `GET /children/{childId}/points` | Punkte-Ledger des Kindes |
| V | `POST /children/{childId}/points` | manuelle Buchung `{ amount, reason }` |
| A | `GET /children/{childId}/vocabulary-progress` · `…/{itemId}` · `…/{itemId}/history` · `…/by-word` | Vokabel-Lernstand des Kindes, **flach** (Item + Wort-Rollup + Historie) |
| A | `GET /children/{childId}/learn/subjects[/{s}]` · `…/{s}/chapters` · `…/{s}/chapters/{c}/vocabulary[/{exerciseId}/items]` | Vokabel-Lernstand **hierarchisch** (Fach→Kapitel→Übung→Item, Aggregate + `active`-Flag; `search`/`sort`/`active`/Paging) |
| A/V | `GET /children/{childId}/learn-goals[/{goalId}]` (lesen: Vater/Kind) · `POST/PATCH/DELETE` (nur Vater) | **Lernziele** (Beherrschung/Abdeckung je Scope), live ausgewertet (`open`/`achieved`/`overdue`); Filter `?subjectId=&status=` |
| V | `GET/POST /children/{childId}/missions` · `PATCH/DELETE …/{missionId}` | Missionen definieren |
| V | `GET/POST /children/{childId}/achievements` · `PATCH/DELETE …/{achievementId}` | Auszeichnungen definieren |
| V | `GET/POST /children/{childId}/timetable` | Stundenplan (Fach × Wochentag) |

## Lern-Katalog (global, Vater-only)

| Methode & Route | Zweck |
| --- | --- |
| `GET/POST /learn/subjects` · `GET/PATCH/DELETE /learn/subjects/{subjectId}` | Fächer |
| `GET/POST /learn/subjects/{s}/chapters` · `…/{chapterId}` | Kapitel |
| `GET/POST /learn/subjects/{s}/categories` · `…/{categoryId}` | fachabhängige Arten |
| `GET /learn/exercises?subjectId=&grade=&schoolType=&categoryId=&type=&search=` | **Übungssuche** (Vorfilterung) |
| `GET/POST/PUT/DELETE /learn/subjects/{s}/chapters/{c}/<typ>[/{id}]` | Übungs-CRUD je Typ (12 Typen, siehe [03](03-uebungstypen.md)) |
| `GET/POST /learn/subjects/{s}/chapters/{c}/vocabulary/{id}/items` · `GET/PATCH/DELETE …/items/{itemId}` | Vokabel-Items einer Übung |
| `POST /learn/subjects/{s}/chapters/{c}/vocabulary/{id}/refs-from-tags` | Vokabel-Items per Tag-Snapshot setzen |
| `POST …/matching/{id}/check` · `POST …/arithmetic/{id}/check` · `POST …/list/{id}/check` | Auswertung |
| `POST …/arithmetic-drill/{id}/generate` · `POST …/arithmetic-drill/{id}/check` | Zufallsaufgaben erzeugen/prüfen |

**Übungstyp-Pfade:** `/vocabulary` `/reading` `/cloze` `/essays` `/listening` `/grammar` `/matching`
`/translation` `/arithmetic` `/arithmetic-drill` `/list` `/birkenbihl`.

## Stores (global, Vater-only)

| Methode & Route | Zweck |
| --- | --- |
| `GET/POST /learn/vocabulary` · `GET …/{id}` · `GET …/by-key/{key}` · `PATCH/DELETE …/{id}` | Vokabel-Store |
| `GET/POST /learn/cloze-texts` · `GET …/{id}` · `GET …/by-key/{key}` · `PATCH/DELETE …/{id}` | Lückentext-Store |

## Study-Plans (Training)

| Rolle | Methode & Route | Zweck |
| --- | --- | --- |
| A | `GET /study-plans?childId=` · `GET /study-plans/{id}` | Pläne lesen (Sohn nur eigene) |
| V | `POST /study-plans` · `PATCH /study-plans/{id}` | Plan anlegen/ändern |
| V | `GET/POST /study-plans/{id}/positions` · `GET/PATCH/DELETE …/{positionId}` | Übungen als PlanPositionen verwalten |
| A | `GET /study-plans/{id}/overview` | Tagesmission und aktueller Status |
| A | `GET /study-plans/{id}/overview/progress?from=&to=&dutyDone=&sort=&skip=&take=` | Tag-für-Tag-Fortschritt (Filter/Sort/Paging) |
| A | `GET /study-plans/{id}/positions/{positionId}/report` | Mastery und Testhistorie pro Position |

### Üben (Practice)

| Rolle | Methode & Route | Zweck |
| --- | --- | --- |
| A | `POST /study-plans/{id}/positions/{positionId}/practice-sessions` | Sitzung starten |
| A | `GET …/practice-sessions/{sid}/cards` | fällige Karten (ohne Lösung) |
| A | `POST …/practice-sessions/{sid}/review` | Antwort abgeben (server-autoritativ) |
| A | `POST …/practice-sessions/{sid}/heartbeat` | aktive Sekunden zählen |
| A | `POST …/practice-sessions/{sid}/end` | Sitzung beenden |

### Abschlusstests

| Rolle | Methode & Route | Zweck |
| --- | --- | --- |
| A | `POST /study-plans/{id}/positions/{positionId}/tests` | Testversuch starten |
| A | `GET …/tests/{attemptId}` | Testversuch lesen |
| A | `POST …/tests/{attemptId}/submit` | Antworten serverseitig bewerten |

## Tags & Klassenarbeiten

| Methode & Route | Zweck |
| --- | --- |
| `…/tags` | Tags verwalten (Vater/Sohn), Übungen taggen |
| `…/class-tests` | Klassenarbeiten planen/benoten, gezielt üben/wiederholen |

Details: [docs/klassenarbeiten-tagging.md](../docs/klassenarbeiten-tagging.md).

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

**Generische, status-getriebene Codes:** `validation_error`/`bad_request`/`invalid_reference` (400),
`unauthorized`/`invalid_credentials` (401), `forbidden`/`not_author` (403), `not_found` (404),
`conflict`/`concurrency_conflict` (409), `rate_limited` (429), `internal_error` (500).

**Fachliche Codes (Auswahl):** `skin_already_unlocked`, `skin_not_unlocked`, `insufficient_gems`,
`insufficient_coins`, `quota_exhausted`, `offer_inactive`, `purchase_not_open`, `duplicate_key`,
`duplicate_tag_name`, `exercise_in_use`, `vocabulary_in_use`, `position_has_data`, `plan_inactive`,
`test_already_submitted`, `no_checkable_content`, `timetable_slot_taken`.

Verifizierte Beispiel-Requests/-Responses (Erfolg **und** jeder erreichbare Fehler-Code, erzeugt gegen
die geseedete API) liegen unter [docs/api-examples/](../docs/api-examples/index.md); sie werden vom
Integrationstest `DocsCaptureTests` erzeugt und dabei auf Status + `code` geprüft.
