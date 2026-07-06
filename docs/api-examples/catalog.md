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
  "createdAt": "2026-07-06T14:37:12.0085062Z",
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
  "traceId": "00-4aa3eb590d491c5da95b645670ddef66-3511afeb98d6c2ab-00"
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
  "createdAt": "2026-07-06T14:37:12.1986893Z",
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
  "traceId": "00-4ba19ef31653f6639bfb59a23424aa52-41cf0f4e4673f71a-00",
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
  "createdAt": "2026-07-06T14:37:12.6259505Z"
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
  "traceId": "00-bda39a1878f08197099131ee2e7832d9-9ad193b7cd298765-00"
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
  "traceId": "00-38338a4bb9f04501daa8bd80398538d1-bddcbe140fde020d-00"
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
  "traceId": "00-ee56ed8a6b346aa34fe435b340e2320f-b4c711786da1217c-00"
}
```

