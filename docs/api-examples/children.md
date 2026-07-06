# API-Beispiele – children

_Automatisch erzeugt von `DocsCaptureTests` (Integrationstest). Jedes Beispiel ist verifiziert: Status und – bei Fehlern – der maschinenlesbare `code` wurden im Testlauf geprüft. Nicht von Hand bearbeiten._

## Eigene Kinder auflisten
`GET /api/v1/children`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 200`:
```json
[
  {
    "id": 1,
    "fatherId": 1,
    "name": "Sohn",
    "birthYear": 2015,
    "grade": null,
    "schoolType": "None",
    "createdAt": "2026-07-06T20:22:27.2414619",
    "coins": 50,
    "gems": 300
  }
]
```

## Kind anlegen
`POST /api/v1/children`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "name": "Doku-Kind",
  "pin": "4242"
}
```

Response — `HTTP 201`:
```json
{
  "id": 3,
  "fatherId": 1,
  "name": "Doku-Kind",
  "birthYear": null,
  "grade": null,
  "schoolType": "None",
  "createdAt": "<timestamp>",
  "coins": 0,
  "gems": 0
}
```

### Kind ohne Namen anlegen — Fehlerfall
`POST /api/v1/children`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "name": "",
  "pin": "0000"
}
```

Response — `HTTP 400`:
```json
{
  "type": "https://pugling.app/errors/validation_error",
  "title": "Invalid request.",
  "status": 400,
  "detail": "Name is required.",
  "code": "validation_error",
  "traceId": "<trace-id>"
}
```

## Einzelnes Kind lesen
`GET /api/v1/children/3`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 200`:
```json
{
  "id": 3,
  "fatherId": 1,
  "name": "Doku-Kind",
  "birthYear": null,
  "grade": null,
  "schoolType": "None",
  "createdAt": "2026-07-06T20:22:29.7246659",
  "coins": 0,
  "gems": 0
}
```

## Kind ändern (Klassenstufe)
`PATCH /api/v1/children/3`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "grade": 4
}
```

Response — `HTTP 200`:
```json
{
  "id": 3,
  "fatherId": 1,
  "name": "Doku-Kind",
  "birthYear": null,
  "grade": 4,
  "schoolType": "None",
  "createdAt": "2026-07-06T20:22:29.7246659",
  "coins": 0,
  "gems": 0
}
```

### Fremdes Kind lesen — Fehlerfall
`GET /api/v1/children/2`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 404`:
```json
{
  "type": "https://pugling.app/errors/not_found",
  "title": "Resource not found.",
  "status": 404,
  "detail": "Child not found.",
  "code": "not_found",
  "traceId": "<trace-id>"
}
```

## Kind löschen
`DELETE /api/v1/children/3`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 204`:
```json
(kein Inhalt)
```

