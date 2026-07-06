# API-Beispiele – catalog

_Automatisch erzeugt von `DocsCaptureTests` (Integrationstest). Jedes Beispiel ist verifiziert: Status und – bei Fehlern – der maschinenlesbare `code` wurden im Testlauf geprüft. Nicht von Hand bearbeiten._

## Fach anlegen
`POST /api/v1/learn/subjects`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "name": "Doku-Fach"
}
```

Response — `HTTP 201`:
```json
{
  "id": 5,
  "name": "Doku-Fach",
  "createdAt": "<timestamp>",
  "chaptersCount": 0
}
```

### Fach ohne Namen anlegen — Fehlerfall
`POST /api/v1/learn/subjects`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "name": ""
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

## Kapitel anlegen
`POST /api/v1/learn/subjects/5/chapters`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "name": "Kapitel 1",
  "orderIndex": 1
}
```

Response — `HTTP 201`:
```json
{
  "id": 7,
  "subjectId": 5,
  "name": "Kapitel 1",
  "orderIndex": 1,
  "exercisesCount": 0
}
```

## Vokabel-Übung anlegen
`POST /api/v1/learn/subjects/5/chapters/7/vocabulary`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "title": "Begr\u00FC\u00DFungen",
  "orderIndex": 1,
  "rewardPoints": 10,
  "config": {
    "direction": "front-to-back",
    "sourceLang": "en",
    "targetLang": "de",
    "items": [
      {
        "front": "hello",
        "back": "hallo"
      },
      {
        "front": "goodbye",
        "back": "tsch\u00FCss"
      }
    ]
  }
}
```

Response — `HTTP 201`:
```json
{
  "id": 13,
  "chapterId": 7,
  "type": "Vocabulary",
  "title": "Begr\u00FC\u00DFungen",
  "orderIndex": 1,
  "rewardPoints": 10,
  "createdAt": "<timestamp>",
  "config": {
    "direction": "front-to-back",
    "sourceLang": "en",
    "targetLang": "de",
    "refs": null,
    "items": [
      {
        "front": "hello",
        "back": "hallo",
        "hint": null,
        "vocabularyId": 16,
        "_self": "/api/v1/learn/vocabulary/16"
      },
      {
        "front": "goodbye",
        "back": "tsch\u00FCss",
        "hint": null,
        "vocabularyId": 17,
        "_self": "/api/v1/learn/vocabulary/17"
      }
    ]
  },
  "suggestedBonus": null,
  "gradeMin": null,
  "gradeMax": null,
  "schoolTypes": "None",
  "source": null,
  "categoryId": null,
  "categoryName": null,
  "authorFatherId": 1,
  "isOwn": true,
  "description": null,
  "defaultUseLeitner": false,
  "defaultRequireTypedTest": false
}
```

### Unbekannte Übung lesen — Fehlerfall
`GET /api/v1/learn/exercises/999999`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 404`:
```json
{
  "type": "https://pugling.app/errors/not_found",
  "title": "Not Found",
  "status": 404,
  "traceId": "<trace-id>",
  "code": "not_found"
}
```

## Art (Kategorie) anlegen
`POST /api/v1/learn/subjects/5/categories`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "name": "Vokabeln"
}
```

Response — `HTTP 201`:
```json
{
  "id": 8,
  "subjectId": 5,
  "name": "Vokabeln",
  "createdAt": "<timestamp>"
}
```

### Doppelte Art anlegen — Fehlerfall
`POST /api/v1/learn/subjects/5/categories`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "name": "Vokabeln"
}
```

Response — `HTTP 409`:
```json
{
  "type": "https://pugling.app/errors/conflict",
  "title": "Conflict.",
  "status": 409,
  "detail": "This category already exists in the subject.",
  "code": "conflict",
  "traceId": "<trace-id>"
}
```

### Verwendete Übung löschen — Fehlerfall
`DELETE /api/v1/learn/subjects/5/chapters/7/vocabulary/13`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 409`:
```json
{
  "type": "https://pugling.app/errors/exercise_in_use",
  "title": "Exercise is in use.",
  "status": 409,
  "detail": "The exercise is used in a study plan or a class test and cannot be deleted.",
  "code": "exercise_in_use",
  "traceId": "<trace-id>"
}
```

### Fremd-Autor-Übung bearbeiten — Fehlerfall
`PUT /api/v1/learn/subjects/1/chapters/6/vocabulary/10`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "title": "\u00DCbernahmeversuch",
  "orderIndex": 1,
  "rewardPoints": 1,
  "config": {}
}
```

Response — `HTTP 403`:
```json
{
  "type": "https://pugling.app/errors/not_author",
  "title": "Access denied.",
  "status": 403,
  "detail": "This exercise belongs to another father and can only be modified or deleted by its author.",
  "code": "not_author",
  "traceId": "<trace-id>"
}
```

