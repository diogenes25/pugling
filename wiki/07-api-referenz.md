# 07 · API-Referenz (Endpunkt-Index)

← [Zurück zum Wiki-Index](../README.md)

Kompakter Überblick über alle Routen. **Autoritative Quelle bleibt Swagger** (`/swagger` bzw.
`/openapi/v1.json`) — dort stehen die vollständigen Request/Response-Schemas. Alle Routen unter
`api/v1/…`. `V` = Vater-only, `S` = Sohn, `A` = beide (authentifiziert), `∅` = anonym.

---

## Auth & Selbstauskunft

| Rolle | Methode & Route | Zweck |
| --- | --- | --- |
| ∅ | `POST /auth/father` | Vater-Login (fatherId + pin) → JWT |
| ∅ | `POST /auth/child` | Sohn-Login (childId + pin) → JWT |
| A | `GET /auth/me` | Identität aus dem Token |
| S | `GET /me/points` | eigener Punktestand (Wallet) |
| S | `GET /me/missions` | eigene Missionen mit Fortschritt |
| S | `GET /me/achievements` | eigene Auszeichnungen |

## Admin — Personen & Punkte

| Rolle | Methode & Route | Zweck |
| --- | --- | --- |
| ∅ | `POST /fathers` | Vater registrieren |
| V | `GET/PATCH/DELETE /fathers/{fatherId}` · `GET /fathers` | eigener Vater-Datensatz |
| V | `GET/POST /children` · `GET/PATCH/DELETE /children/{childId}` | Kinder verwalten (inkl. `pointsBalance`) |
| V | `GET /children/{childId}/points` | Punkte-Ledger des Kindes |
| V | `POST /children/{childId}/points` | manuelle Buchung `{ amount, reason }` |
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
| `POST …/matching/{id}/check` · `POST …/arithmetic/{id}/check` · `POST …/list/{id}/check` | Auswertung |
| `POST …/arithmetic-drill/{id}/generate` · `POST …/arithmetic-drill/{id}/check` | Zufallsaufgaben erzeugen/prüfen |
| `POST …/matching/{id}/to-study-plan` | Matching-Übung → Leitner-Study-Plan |

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
| V | `POST /study-plans/{id}/items` · `DELETE /study-plans/{id}/items/{itemId}` | Inhalte im Plan |
| A | `GET /study-plans/{id}/today` | Ein-Blick-Status heute |
| A | `GET /study-plans/{id}/progress` | Tag-für-Tag-Fortschritt |
| A | `GET /study-plans/{id}/report` | Mastery pro Inhalt + Testhistorie + Ratings |

### Üben (Practice)

| Rolle | Methode & Route | Zweck |
| --- | --- | --- |
| A | `POST /study-plans/{id}/practice-sessions` | Sitzung starten |
| A | `GET …/practice-sessions/{sid}/cards` | fällige Karten (ohne Lösung) |
| A | `POST …/practice-sessions/{sid}/review` | Antwort abgeben (server-autoritativ) |
| A | `POST …/practice-sessions/{sid}/heartbeat` | aktive Sekunden zählen |
| A | `POST …/practice-sessions/{sid}/end` | Sitzung beenden |

### Abschlusstests

| Rolle | Methode & Route | Verfahren |
| --- | --- | --- |
| A | `POST /study-plans/{id}/tests` · `…/{aid}/hint` · `…/{aid}/submit` · `GET …/{aid}` | Vocabulary |
| A | `POST /study-plans/{id}/cloze-tests` · `…/{aid}/hint` · `…/{aid}/submit` | Cloze |
| A | `POST /study-plans/{id}/matching-tests` · `…/{aid}/submit` | Matching |
| S | `POST /study-plans/{id}/ratings` | Inhalt bewerten |

## Tags & Klassenarbeiten

| Methode & Route | Zweck |
| --- | --- |
| `…/tags` | Tags verwalten (Vater/Sohn), Übungen taggen |
| `…/class-tests` | Klassenarbeiten planen/benoten, gezielt üben/wiederholen |

Details: [docs/klassenarbeiten-tagging.md](../docs/klassenarbeiten-tagging.md).

---

## Fehlerformat

Alle Fehler sind `ProblemDetails` (RFC 7807): `{ "type", "title", "status", "detail" }`. Typische
Codes: `400` (Validierung), `401` (kein/falscher Token), `403` (falsche Rolle / fremde Ressource),
`404` (nicht gefunden / nicht eigenes Kind), `409` (Konflikt, z. B. Key existiert / Löschschutz).
