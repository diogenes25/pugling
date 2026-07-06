# API-Beispiele – tags

_Automatisch erzeugt von `DocsCaptureTests` (Integrationstest). Jedes Beispiel ist verifiziert: Status und – bei Fehlern – der maschinenlesbare `code` wurden im Testlauf geprüft. Nicht von Hand bearbeiten._

## Tag anlegen (Vater)
`POST /api/v1/tags`

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
  "createdAt": "2026-07-06T16:55:34.8045927Z"
}
```

## Tag anlegen (Sohn)
`POST /api/v1/tags`

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
  "createdAt": "2026-07-06T16:55:34.8249507Z"
}
```

### Tag mit doppeltem Namen — Fehlerfall
`POST /api/v1/tags`

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
  "traceId": "00-97da2eb72e16e7d331c4ce05bcebecf7-5e0240ce908decff-00"
}
```

### Tag für fremdes Kind anlegen — Fehlerfall
`POST /api/v1/tags`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "childId": 2,
  "name": "Fremd"
}
```

Response — `HTTP 403`:
```json
{
  "type": "https://pugling.app/errors/forbidden",
  "title": "Forbidden",
  "status": 403,
  "traceId": "00-7ca11c03712a7973488b40f4822e42a8-13686e64258390c2-00",
  "code": "forbidden"
}
```

### Unbekannte Übungen taggen — Fehlerfall
`POST /api/v1/tags/3/exercises`

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
  "traceId": "00-e6b7c532cb14ebc9602b6ae595312147-da97914c09b4365f-00"
}
```

