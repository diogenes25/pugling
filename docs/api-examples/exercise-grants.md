# API-Beispiele – exercise-grants

_Automatisch erzeugt von `DocsCaptureTests` (Integrationstest). Jedes Beispiel ist verifiziert: Status und – bei Fehlern – der maschinenlesbare `code` wurden im Testlauf geprüft. Nicht von Hand bearbeiten._

## Rechte einer Übung auflisten (nur Owner)
`GET /api/v1/creator/exercises/13/grants`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 200`:
```json
[
  {
    "creatorId": 1,
    "creatorName": "Papa",
    "permission": "Owner",
    "grantedByAdultId": 1,
    "createdAt": "<timestamp>"
  }
]
```

### Rechte einer fremden Übung auflisten — Fehlerfall
`GET /api/v1/creator/exercises/13/grants`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 403`:
```json
{
  "type": "https://pugling.app/errors/not_owner",
  "title": "Access denied.",
  "status": 403,
  "detail": "Only an owner can view or manage the permissions of this exercise.",
  "code": "not_owner",
  "traceId": "<trace-id>"
}
```

## Write-Recht an anderen Creator vergeben
`POST /api/v1/creator/exercises/13/grants`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "creatorId": 4,
  "permission": "Write"
}
```

Response — `HTTP 201`:
```json
{
  "creatorId": 4,
  "creatorName": "Zweiter Papa",
  "permission": "Write",
  "grantedByAdultId": 1,
  "createdAt": "<timestamp>"
}
```

### Letzten Owner entfernen — Fehlerfall
`DELETE /api/v1/creator/exercises/13/grants/1/Owner`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 409`:
```json
{
  "type": "https://pugling.app/errors/last_owner",
  "title": "Cannot remove the last owner.",
  "status": 409,
  "detail": "Cannot remove the last owner of an exercise.",
  "code": "last_owner",
  "traceId": "<trace-id>"
}
```

## Nicht öffentlich ausführbare Übung anlegen
`POST /api/v1/creator/subjects/5/chapters/7/vocabulary`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "title": "Nur intern",
  "orderIndex": 2,
  "rewardPoints": 10,
  "executePublic": false,
  "config": {
    "direction": "front-to-back",
    "sourceLang": "en",
    "targetLang": "de"
  }
}
```

Response — `HTTP 201`:
```json
{
  "id": 25,
  "chapterId": 7,
  "type": "Vocabulary",
  "title": "Nur intern",
  "orderIndex": 2,
  "rewardPoints": 10,
  "createdAt": "<timestamp>",
  "config": {
    "direction": "front-to-back",
    "sourceLang": "en",
    "targetLang": "de",
    "refs": null,
    "items": []
  },
  "suggestedBonus": null,
  "gradeMin": null,
  "gradeMax": null,
  "schoolTypes": "None",
  "source": null,
  "categoryId": null,
  "categoryName": null,
  "authorAdultId": 1,
  "isOwn": true,
  "isOwner": true,
  "executePublic": false,
  "grantCount": 1,
  "description": null,
  "defaultUseLeitner": false,
  "defaultRequireTypedTest": false,
  "defaultStage": null,
  "defaultItemCount": null
}
```

## Lehrplan für eigenes Kind anlegen
`POST /api/v1/supervisor/study-plans`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "childId": 3,
  "title": "Plan (fremd)",
  "durationDays": 5
}
```

Response — `HTTP 201`:
```json
{
  "id": 3,
  "childId": 3,
  "title": "Plan (fremd)",
  "subjectId": null,
  "startDate": "<date>",
  "endDate": "<date>",
  "active": true,
  "positionCount": 0,
  "description": null,
  "isPlayable": true
}
```

### Nicht ausführbare Übung zuweisen — Fehlerfall
`POST /api/v1/supervisor/study-plans/3/positions`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "exerciseId": 25
}
```

Response — `HTTP 403`:
```json
{
  "type": "https://pugling.app/errors/exercise_not_executable",
  "title": "Exercise cannot be assigned.",
  "status": 403,
  "detail": "This exercise is not publicly assignable; you need execute permission from its owner.",
  "code": "exercise_not_executable",
  "traceId": "<trace-id>"
}
```

