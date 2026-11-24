# API-Beispiele – feedback

_Automatisch erzeugt von `DocsCaptureTests` (Integrationstest). Jedes Beispiel ist verifiziert: Status und – bei Fehlern – der maschinenlesbare `code` wurden im Testlauf geprüft. Nicht von Hand bearbeiten._

## Lehrplan für die Rückmeldung

`POST /api/v1/supervisor/study-plans`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:

```json
{
  "childId": 1,
  "title": "R\u00FCckmelde-Plan",
  "seriesUnitId": 7,
  "durationDays": 10
}
```

Response — `HTTP 201`:

```json
{
  "id": 5,
  "childId": 1,
  "title": "R\u00FCckmelde-Plan",
  "subjectId": 5,
  "startDate": "<date>",
  "endDate": "<date>",
  "active": true,
  "positionCount": 0,
  "description": null,
  "seriesUnitId": 7,
  "seriesUnitLabel": "Unit 1",
  "isPlayable": true
}
```

## Position für die Rückmeldung

`POST /api/v1/supervisor/study-plans/5/positions`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:

```json
{
  "exerciseId": 13,
  "stage": 4,
  "cadence": "Daily"
}
```

Response — `HTTP 201`:

```json
{
  "id": 20,
  "studyPlanId": 5,
  "exerciseId": 13,
  "exerciseTitle": "Begr\u00FC\u00DFungen",
  "exerciseType": "Vocabulary",
  "order": 0,
  "stage": 4,
  "itemCount": null,
  "scope": "All",
  "cadence": "Daily",
  "orderStrategy": "WeakestFirst",
  "goalThreshold": null,
  "requireTypedTest": false,
  "useLeitner": false,
  "maxBox": 5,
  "boxIntervalDays": null,
  "stageSchedule": null,
  "pointsGoalMet": 10,
  "penaltyCoins": 0,
  "newContentPoints": 10,
  "comboThreshold": 5,
  "comboBonusPoints": 5,
  "speedThresholdSeconds": 0,
  "speedBonusPoints": 0,
  "timeSlots": null,
  "newItemsPerDay": 0,
  "dailyDoseInForce": 0,
  "learningCheck": {
    "kind": "Typed",
    "typedFromDay": null
  },
  "forgivingDemotion": false,
  "remediation": false,
  "completionBonus": 0,
  "completedOn": null,
  "currentBuildLevel": null,
  "build": null
}
```

## Gründe mit Reichweite lesen

`GET /api/v1/student/feedback-reasons`

Rolle: **child** — `Authorization: Bearer <child-token>`

Response — `HTTP 200`:

```json
[
  {
    "reason": "TooHard",
    "reach": "ThisChild"
  },
  {
    "reason": "TooEasy",
    "reach": "ThisChild"
  },
  {
    "reason": "AheadOfClass",
    "reach": "ThisChild"
  },
  {
    "reason": "WrongTranslation",
    "reach": "Everyone"
  },
  {
    "reason": "TooMuchAsked",
    "reach": "Everyone"
  },
  {
    "reason": "TooStrict",
    "reach": "Everyone"
  },
  {
    "reason": "SeveralMeanings",
    "reach": "Everyone"
  },
  {
    "reason": "Liked",
    "reach": "ThisChild"
  }
]
```

## Karte melden (zwei Gründe, Text)

`POST /api/v1/student/study-plans/5/positions/20/feedback`

Rolle: **child** — `Authorization: Bearer <child-token>`

Request:

```json
{
  "reasons": [
    "WrongTranslation",
    "TooHard"
  ],
  "text": "Das hei\u00DFt doch anders.",
  "itemIndex": 0
}
```

Response — `HTTP 201`:

```json
{
  "ids": [
    1,
    2
  ]
}
```

### Meldung ohne Grund und Text — Fehlerfall

`POST /api/v1/student/study-plans/5/positions/20/feedback`

Rolle: **child** — `Authorization: Bearer <child-token>`

Request:

```json
{
  "reasons": []
}
```

Response — `HTTP 400`:

```json
{
  "type": "https://pugling.app/errors/validation_error",
  "title": "Invalid request.",
  "status": 400,
  "detail": "Give at least one reason or a text.",
  "code": "validation_error",
  "traceId": "<trace-id>"
}
```

## Rückmeldungen der Kinder (offen)

`GET /api/v1/supervisor/feedback?status=Open`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 200`:

```json
[
  {
    "id": 2,
    "childId": 1,
    "childName": "Sohn",
    "studyPlanId": 5,
    "planPositionId": 20,
    "exerciseId": 13,
    "exerciseTitle": "Begr\u00FC\u00DFungen",
    "itemIndex": 0,
    "cue": "hello",
    "solution": "hallo",
    "childAnswer": "hallo",
    "reason": "TooHard",
    "reach": "ThisChild",
    "count": 1,
    "text": "Das hei\u00DFt doch anders.",
    "status": "Open",
    "createdAt": "<timestamp>",
    "lastReportedAt": "<timestamp>",
    "doneAt": null,
    "testItemResultId": null,
    "testAnswerCorrect": null,
    "outcome": null
  },
  {
    "id": 1,
    "childId": 1,
    "childName": "Sohn",
    "studyPlanId": 5,
    "planPositionId": 20,
    "exerciseId": 13,
    "exerciseTitle": "Begr\u00FC\u00DFungen",
    "itemIndex": 0,
    "cue": "hello",
    "solution": "hallo",
    "childAnswer": "hallo",
    "reason": "WrongTranslation",
    "reach": "Everyone",
    "count": 1,
    "text": "Das hei\u00DFt doch anders.",
    "status": "Open",
    "createdAt": "<timestamp>",
    "lastReportedAt": "<timestamp>",
    "doneAt": null,
    "testItemResultId": null,
    "testAnswerCorrect": null,
    "outcome": null
  }
]
```

### Fremder Betreuer erledigt — Fehlerfall

`PATCH /api/v1/supervisor/feedback/1`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:

```json
{
  "status": "Done"
}
```

Response — `HTTP 404`:

```json
{
  "type": "https://pugling.app/errors/not_found",
  "title": "Resource not found.",
  "status": 404,
  "detail": "Feedback not found.",
  "code": "not_found",
  "traceId": "<trace-id>"
}
```

## Rückmeldung erledigen

`PATCH /api/v1/supervisor/feedback/1`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:

```json
{
  "status": "Done"
}
```

Response — `HTTP 200`:

```json
{
  "id": 1,
  "childId": 1,
  "childName": "Sohn",
  "studyPlanId": 5,
  "planPositionId": 20,
  "exerciseId": 13,
  "exerciseTitle": "Begr\u00FC\u00DFungen",
  "itemIndex": 0,
  "cue": "hello",
  "solution": "hallo",
  "childAnswer": "hallo",
  "reason": "WrongTranslation",
  "reach": "Everyone",
  "count": 1,
  "text": "Das hei\u00DFt doch anders.",
  "status": "Done",
  "createdAt": "<timestamp>",
  "lastReportedAt": "<timestamp>",
  "doneAt": "<timestamp>",
  "testItemResultId": null,
  "testAnswerCorrect": null,
  "outcome": null
}
```

## Gezählte Gründe an der Übung (Ersteller)

`GET /api/v1/creator/exercises/13/feedback`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 200`:

```json
{
  "exerciseId": 13,
  "total": 2,
  "byReason": [
    {
      "reason": "TooHard",
      "reach": "ThisChild",
      "count": 1
    },
    {
      "reason": "WrongTranslation",
      "reach": "Everyone",
      "count": 1
    }
  ],
  "byCard": [
    {
      "itemIndex": 0,
      "reasons": [
        {
          "reason": "TooHard",
          "reach": "ThisChild",
          "count": 1
        },
        {
          "reason": "WrongTranslation",
          "reach": "Everyone",
          "count": 1
        }
      ]
    }
  ]
}
```

## Hinweise des Kindes

`GET /api/v1/student/children/1/feedback-notices`

Rolle: **child** — `Authorization: Bearer <child-token>`

Response — `HTTP 200`:

```json
[
  {
    "id": 1,
    "exerciseId": 13,
    "exerciseTitle": "Begr\u00FC\u00DFungen",
    "itemIndex": 0,
    "cue": "hello",
    "reason": "WrongTranslation",
    "doneAt": "<timestamp>",
    "outcome": null
  }
]
```

## Hinweis gesehen

`POST /api/v1/student/children/1/feedback-notices/1/seen`

Rolle: **child** — `Authorization: Bearer <child-token>`

Response — `HTTP 204`:

```json
(kein Inhalt)
```
