# API-Beispiele – class-tests

_Automatisch erzeugt von `DocsCaptureTests` (Integrationstest). Jedes Beispiel ist verifiziert: Status und – bei Fehlern – der maschinenlesbare `code` wurden im Testlauf geprüft. Nicht von Hand bearbeiten._

## Klassenarbeit planen
`POST /api/v1/class-tests`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "childId": 1,
  "title": "Vokabeltest Unit 5",
  "scheduledDate": "2099-03-01"
}
```

Response — `HTTP 201`:
```json
{
  "klassenarbeit": {
    "id": 3,
    "childId": 1,
    "subjectId": null,
    "subjectName": null,
    "title": "Vokabeltest Unit 5",
    "topic": null,
    "scheduledDate": "2099-03-01",
    "status": "Planned",
    "grade": null,
    "gradeComment": null,
    "directExerciseCount": 0,
    "tags": [],
    "createdAt": "2026-07-06T14:37:16.6914022Z"
  },
  "assignedExercises": []
}
```

### Note außerhalb des Bereichs — Fehlerfall
`POST /api/v1/class-tests`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "childId": 1,
  "title": "Ung\u00FCltige Note",
  "scheduledDate": "2099-03-01",
  "grade": 9
}
```

Response — `HTTP 400`:
```json
{
  "type": "https://pugling.app/errors/validation_error",
  "title": "Invalid request.",
  "status": 400,
  "detail": "Grade must be between 1.0 and 6.0.",
  "code": "validation_error",
  "traceId": "00-bda0bf0a7ab0d1b4e412b2482953f2e1-2d0b0e35bc2ea125-00"
}
```

### Unbekannte Übung zuweisen — Fehlerfall
`POST /api/v1/class-tests`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "childId": 1,
  "title": "Unbekannte \u00DCbung",
  "scheduledDate": "2099-03-01",
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
  "traceId": "00-6c1dcfadd6bf8ea59e1c46773a8eb14d-ca66c2d2192b6b38-00"
}
```

