# API-Beispiele – catalog

_Automatisch erzeugt von `DocsCaptureTests` (Integrationstest). Jedes Beispiel ist verifiziert: Status und – bei Fehlern – der maschinenlesbare `code` wurden im Testlauf geprüft. Nicht von Hand bearbeiten._

## Fach anlegen
`POST /api/v1/creator/subjects`

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
`POST /api/v1/creator/subjects`

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
`POST /api/v1/creator/subjects/5/chapters`

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
`POST /api/v1/creator/subjects/5/chapters/7/vocabulary`

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
    "targetLang": "de"
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
    "items": []
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
  "defaultRequireTypedTest": false,
  "defaultStage": null,
  "defaultItemCount": null
}
```

## Vokabelpaar hinzufügen
`POST /api/v1/creator/subjects/5/chapters/7/vocabulary/13/items`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "front": "hello",
  "back": "hallo"
}
```

Response — `HTTP 201`:
```json
{
  "id": 15,
  "orderIndex": 0,
  "vocabularyId": 16,
  "front": "hello",
  "back": "hallo",
  "hint": null,
  "_self": "/api/v1/creator/subjects/5/chapters/7/vocabulary/13/items/15",
  "vocabulary": "/api/v1/creator/vocabulary/16"
}
```

### Unbekannte Übung lesen — Fehlerfall
`GET /api/v1/creator/exercises/999999`

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
`POST /api/v1/creator/subjects/5/categories`

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
`POST /api/v1/creator/subjects/5/categories`

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
`DELETE /api/v1/creator/subjects/5/chapters/7/vocabulary/13`

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
`PUT /api/v1/creator/subjects/1/chapters/6/vocabulary/10`

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

## Leseübung anlegen
`POST /api/v1/creator/subjects/5/chapters/7/reading`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "title": "Der Wetterbericht",
  "orderIndex": 3,
  "rewardPoints": 10,
  "config": {
    "text": "Today it is sunny with a light breeze.",
    "questions": [
      {
        "prompt": "How is the weather?",
        "choices": [
          "sunny",
          "rainy",
          "snowy"
        ],
        "answer": "sunny"
      }
    ]
  }
}
```

Response — `HTTP 201`:
```json
{
  "id": 14,
  "chapterId": 7,
  "type": "Reading",
  "title": "Der Wetterbericht",
  "orderIndex": 3,
  "rewardPoints": 10,
  "createdAt": "<timestamp>",
  "config": {
    "text": "Today it is sunny with a light breeze.",
    "questions": [
      {
        "prompt": "How is the weather?",
        "choices": [
          "sunny",
          "rainy",
          "snowy"
        ],
        "answer": "sunny"
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
  "defaultRequireTypedTest": false,
  "defaultStage": null,
  "defaultItemCount": null
}
```

## Lückentext anlegen
`POST /api/v1/creator/subjects/5/chapters/7/cloze`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "title": "Present Simple",
  "orderIndex": 4,
  "rewardPoints": 10,
  "config": {
    "text": "She {{1}} to school every day and {{2}} her friends.",
    "gaps": [
      {
        "index": 1,
        "answer": "goes"
      },
      {
        "index": 2,
        "answer": "meets"
      }
    ],
    "wordBank": [
      "goes",
      "meets",
      "go",
      "meet"
    ]
  }
}
```

Response — `HTTP 201`:
```json
{
  "id": 15,
  "chapterId": 7,
  "type": "Cloze",
  "title": "Present Simple",
  "orderIndex": 4,
  "rewardPoints": 10,
  "createdAt": "<timestamp>",
  "config": {
    "text": "She {{1}} to school every day and {{2}} her friends.",
    "gaps": [
      {
        "index": 1,
        "answer": "goes",
        "alternatives": null,
        "vocabKey": null
      },
      {
        "index": 2,
        "answer": "meets",
        "alternatives": null,
        "vocabKey": null
      }
    ],
    "wordBank": [
      "goes",
      "meets",
      "go",
      "meet"
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
  "defaultRequireTypedTest": false,
  "defaultStage": null,
  "defaultItemCount": null
}
```

## Aufsatz anlegen
`POST /api/v1/creator/subjects/5/chapters/7/essays`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "title": "My last holiday",
  "orderIndex": 5,
  "rewardPoints": 20,
  "config": {
    "prompt": "Write about your last holiday.",
    "minWords": 80,
    "maxWords": 200,
    "rubric": [
      {
        "criterion": "Content",
        "maxScore": 5
      },
      {
        "criterion": "Grammar",
        "maxScore": 5
      }
    ]
  }
}
```

Response — `HTTP 201`:
```json
{
  "id": 16,
  "chapterId": 7,
  "type": "Essay",
  "title": "My last holiday",
  "orderIndex": 5,
  "rewardPoints": 20,
  "createdAt": "<timestamp>",
  "config": {
    "prompt": "Write about your last holiday.",
    "minWords": 80,
    "maxWords": 200,
    "rubric": [
      {
        "criterion": "Content",
        "maxScore": 5
      },
      {
        "criterion": "Grammar",
        "maxScore": 5
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
  "defaultRequireTypedTest": false,
  "defaultStage": null,
  "defaultItemCount": null
}
```

## Hörübung anlegen
`POST /api/v1/creator/subjects/5/chapters/7/listening`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "title": "At the station",
  "orderIndex": 6,
  "rewardPoints": 10,
  "config": {
    "audioUrl": "https://example.com/audio/at-the-station.mp3",
    "transcript": "The train to London leaves at nine o\u0027clock.",
    "questions": [
      {
        "prompt": "When does the train leave?",
        "choices": [
          "at nine",
          "at ten",
          "at noon"
        ],
        "answer": "at nine"
      }
    ]
  }
}
```

Response — `HTTP 201`:
```json
{
  "id": 17,
  "chapterId": 7,
  "type": "Listening",
  "title": "At the station",
  "orderIndex": 6,
  "rewardPoints": 10,
  "createdAt": "<timestamp>",
  "config": {
    "audioUrl": "https://example.com/audio/at-the-station.mp3",
    "transcript": "The train to London leaves at nine o\u0027clock.",
    "questions": [
      {
        "prompt": "When does the train leave?",
        "choices": [
          "at nine",
          "at ten",
          "at noon"
        ],
        "answer": "at nine"
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
  "defaultRequireTypedTest": false,
  "defaultStage": null,
  "defaultItemCount": null
}
```

## Grammatikübung anlegen
`POST /api/v1/creator/subjects/5/chapters/7/grammar`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "title": "Simple Past",
  "orderIndex": 7,
  "rewardPoints": 10,
  "config": {
    "instruction": "Put the verb in brackets into the simple past.",
    "tasks": [
      {
        "prompt": "I (go) to school.",
        "answer": "went",
        "ruleHint": "irregular verb"
      },
      {
        "prompt": "She (play) tennis.",
        "answer": "played",
        "ruleHint": "regular: \u002B ed"
      }
    ]
  }
}
```

Response — `HTTP 201`:
```json
{
  "id": 18,
  "chapterId": 7,
  "type": "Grammar",
  "title": "Simple Past",
  "orderIndex": 7,
  "rewardPoints": 10,
  "createdAt": "<timestamp>",
  "config": {
    "instruction": "Put the verb in brackets into the simple past.",
    "tasks": [
      {
        "prompt": "I (go) to school.",
        "answer": "went",
        "ruleHint": "irregular verb"
      },
      {
        "prompt": "She (play) tennis.",
        "answer": "played",
        "ruleHint": "regular: \u002B ed"
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
  "defaultRequireTypedTest": false,
  "defaultStage": null,
  "defaultItemCount": null
}
```

## Zuordnungsübung anlegen
`POST /api/v1/creator/subjects/5/chapters/7/matching`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "title": "Countries \u0026 capitals",
  "orderIndex": 8,
  "rewardPoints": 10,
  "config": {
    "instruction": "Match each country to its capital.",
    "pairs": [
      {
        "left": "France",
        "right": "Paris"
      },
      {
        "left": "Spain",
        "right": "Madrid"
      }
    ]
  }
}
```

Response — `HTTP 201`:
```json
{
  "id": 19,
  "chapterId": 7,
  "type": "Matching",
  "title": "Countries \u0026 capitals",
  "orderIndex": 8,
  "rewardPoints": 10,
  "createdAt": "<timestamp>",
  "config": {
    "instruction": "Match each country to its capital.",
    "pairs": [
      {
        "left": "France",
        "right": "Paris"
      },
      {
        "left": "Spain",
        "right": "Madrid"
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
  "defaultRequireTypedTest": false,
  "defaultStage": null,
  "defaultItemCount": null
}
```

## Übersetzungsübung anlegen
`POST /api/v1/creator/subjects/5/chapters/7/translation`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "title": "Everyday phrases",
  "orderIndex": 9,
  "rewardPoints": 10,
  "config": {
    "sourceLang": "en",
    "targetLang": "de",
    "items": [
      {
        "source": "Good morning",
        "target": "Guten Morgen",
        "alternatives": [
          "Guten Tag"
        ]
      },
      {
        "source": "Thank you",
        "target": "Danke",
        "alternatives": [
          "Vielen Dank"
        ]
      }
    ]
  }
}
```

Response — `HTTP 201`:
```json
{
  "id": 20,
  "chapterId": 7,
  "type": "Translation",
  "title": "Everyday phrases",
  "orderIndex": 9,
  "rewardPoints": 10,
  "createdAt": "<timestamp>",
  "config": {
    "sourceLang": "en",
    "targetLang": "de",
    "items": [
      {
        "source": "Good morning",
        "target": "Guten Morgen",
        "alternatives": [
          "Guten Tag"
        ],
        "vocabularyId": 26,
        "_self": "/api/v1/creator/vocabulary/26"
      },
      {
        "source": "Thank you",
        "target": "Danke",
        "alternatives": [
          "Vielen Dank"
        ],
        "vocabularyId": 27,
        "_self": "/api/v1/creator/vocabulary/27"
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
  "defaultRequireTypedTest": false,
  "defaultStage": null,
  "defaultItemCount": null
}
```

## Feste Rechenaufgaben anlegen
`POST /api/v1/creator/subjects/5/chapters/7/arithmetic`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "title": "Kopfrechnen gemischt",
  "orderIndex": 10,
  "rewardPoints": 10,
  "config": {
    "problems": [
      {
        "prompt": "7 \u002B 8",
        "answer": 15,
        "tolerance": 0
      },
      {
        "prompt": "12 / 5",
        "answer": 2.4,
        "tolerance": 0.1
      }
    ]
  }
}
```

Response — `HTTP 201`:
```json
{
  "id": 21,
  "chapterId": 7,
  "type": "Arithmetic",
  "title": "Kopfrechnen gemischt",
  "orderIndex": 10,
  "rewardPoints": 10,
  "createdAt": "<timestamp>",
  "config": {
    "problems": [
      {
        "prompt": "7 \u002B 8",
        "answer": 15,
        "tolerance": 0
      },
      {
        "prompt": "12 / 5",
        "answer": 2.4,
        "tolerance": 0.1
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
  "defaultRequireTypedTest": false,
  "defaultStage": null,
  "defaultItemCount": null
}
```

## Rechen-Drill (Regeln) anlegen
`POST /api/v1/creator/subjects/5/chapters/7/arithmetic-drill`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "title": "Einmaleins-Drill",
  "orderIndex": 11,
  "rewardPoints": 10,
  "config": {
    "operations": [
      "Multiplication"
    ],
    "minOperand": 2,
    "maxOperand": 10,
    "problemCount": 10,
    "allowNegativeResults": false,
    "divisionMustBeWhole": true
  }
}
```

Response — `HTTP 201`:
```json
{
  "id": 22,
  "chapterId": 7,
  "type": "ArithmeticDrill",
  "title": "Einmaleins-Drill",
  "orderIndex": 11,
  "rewardPoints": 10,
  "createdAt": "<timestamp>",
  "config": {
    "operations": [
      "Multiplication"
    ],
    "minOperand": 2,
    "maxOperand": 10,
    "problemCount": 10,
    "allowNegativeResults": false,
    "divisionMustBeWhole": true,
    "seed": null
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
  "defaultRequireTypedTest": false,
  "defaultStage": null,
  "defaultItemCount": null
}
```

## Merkliste anlegen
`POST /api/v1/creator/subjects/5/chapters/7/list`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "title": "Die vier Himmelsrichtungen",
  "orderIndex": 12,
  "rewardPoints": 15,
  "config": {
    "instruction": "Nenne die vier Himmelsrichtungen.",
    "ordered": false,
    "items": [
      {
        "value": "Norden",
        "alternatives": [
          "Nord"
        ]
      },
      {
        "value": "Osten",
        "alternatives": [
          "Ost"
        ]
      }
    ]
  }
}
```

Response — `HTTP 201`:
```json
{
  "id": 23,
  "chapterId": 7,
  "type": "List",
  "title": "Die vier Himmelsrichtungen",
  "orderIndex": 12,
  "rewardPoints": 15,
  "createdAt": "<timestamp>",
  "config": {
    "instruction": "Nenne die vier Himmelsrichtungen.",
    "ordered": false,
    "items": [
      {
        "value": "Norden",
        "alternatives": [
          "Nord"
        ]
      },
      {
        "value": "Osten",
        "alternatives": [
          "Ost"
        ]
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
  "defaultRequireTypedTest": false,
  "defaultStage": null,
  "defaultItemCount": null
}
```

## Birkenbihl-Übung anlegen
`POST /api/v1/creator/subjects/5/chapters/7/birkenbihl`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "title": "Birkenbihl: Small talk",
  "orderIndex": 13,
  "rewardPoints": 10,
  "config": {
    "learningLang": "en",
    "nativeLang": "de",
    "sentences": []
  }
}
```

Response — `HTTP 201`:
```json
{
  "id": 24,
  "chapterId": 7,
  "type": "Birkenbihl",
  "title": "Birkenbihl: Small talk",
  "orderIndex": 13,
  "rewardPoints": 10,
  "createdAt": "<timestamp>",
  "config": {
    "learningLang": "en",
    "nativeLang": "de",
    "nextSentenceId": 1,
    "nextWordId": 1,
    "sentences": []
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
  "defaultRequireTypedTest": false,
  "defaultStage": null,
  "defaultItemCount": null
}
```

