# API-Beispiele – tags

_Automatisch erzeugt von `DocsCaptureTests` (Integrationstest). Jedes Beispiel ist verifiziert: Status und – bei Fehlern – der maschinenlesbare `code` wurden im Testlauf geprüft. Nicht von Hand bearbeiten._

## Tag anlegen (Vater)
`POST /api/v1/creator/tags`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "childId": 1,
  "name": "Doku-Tag",
  "color": "#3b82f6"
}
```

Response — `HTTP 201`:
```json
{
  "id": 3,
  "childId": 1,
  "name": "Doku-Tag",
  "color": "#3b82f6",
  "createdBy": "Vater",
  "exerciseCount": 0,
  "vocabularyCount": 0,
  "createdAt": "<timestamp>"
}
```

## Tag anlegen (Sohn)
`POST /api/v1/creator/tags`

Rolle: **child** — `Authorization: Bearer <child-token>`

Request:
```json
{
  "childId": 1,
  "name": "Sohn-Tag",
  "color": "#22c55e"
}
```

Response — `HTTP 201`:
```json
{
  "id": 4,
  "childId": 1,
  "name": "Sohn-Tag",
  "color": "#22c55e",
  "createdBy": "Sohn",
  "exerciseCount": 0,
  "vocabularyCount": 0,
  "createdAt": "<timestamp>"
}
```

### Tag mit doppeltem Namen — Fehlerfall
`POST /api/v1/creator/tags`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "childId": 1,
  "name": "Doku-Tag"
}
```

Response — `HTTP 400`:
```json
{
  "type": "https://pugling.app/errors/duplicate_tag_name",
  "title": "Tag name already exists.",
  "status": 400,
  "detail": "A tag with this name already exists for this child.",
  "code": "duplicate_tag_name",
  "traceId": "<trace-id>"
}
```

### Tag für fremdes Kind anlegen — Fehlerfall
`POST /api/v1/creator/tags`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "childId": 3,
  "name": "Fremd"
}
```

Response — `HTTP 403`:
```json
{
  "type": "https://pugling.app/errors/forbidden",
  "title": "Forbidden",
  "status": 403,
  "traceId": "<trace-id>",
  "code": "forbidden"
}
```

### Unbekannte Übungen taggen — Fehlerfall
`POST /api/v1/creator/tags/3/exercises`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "exerciseIds": [
    999999
  ]
}
```

Response — `HTTP 400`:
```json
{
  "type": "https://pugling.app/errors/invalid_reference",
  "title": "Invalid request.",
  "status": 400,
  "detail": "Unknown exercise IDs: 999999",
  "code": "invalid_reference",
  "traceId": "<trace-id>"
}
```

