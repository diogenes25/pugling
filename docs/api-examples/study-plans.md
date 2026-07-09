# API-Beispiele – study-plans

_Automatisch erzeugt von `DocsCaptureTests` (Integrationstest). Jedes Beispiel ist verifiziert: Status und – bei Fehlern – der maschinenlesbare `code` wurden im Testlauf geprüft. Nicht von Hand bearbeiten._

## Lehrplan anlegen
`POST /api/v1/supervisor/study-plans`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "childId": 1,
  "title": "Doku-Lehrplan",
  "durationDays": 10
}
```

Response — `HTTP 201`:
```json
{
  "id": 2,
  "childId": 1,
  "title": "Doku-Lehrplan",
  "subjectId": null,
  "startDate": "2026-07-09",
  "endDate": "2026-07-18",
  "active": true,
  "positionCount": 0,
  "description": null,
  "isPlayable": true
}
```

## Position anlegen
`POST /api/v1/supervisor/study-plans/2/positions`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "exerciseId": 13,
  "useLeitner": true,
  "stage": 4,
  "cadence": "Daily"
}
```

Response — `HTTP 201`:
```json
{
  "id": 2,
  "studyPlanId": 2,
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
  "useLeitner": true,
  "maxBox": 5,
  "boxIntervalDays": null,
  "stageSchedule": null,
  "pointsGoalMet": 20,
  "newContentPoints": 10,
  "comboThreshold": 5,
  "comboBonusPoints": 5,
  "speedThresholdSeconds": 0,
  "speedBonusPoints": 0
}
```

### Position mit unbekannter Übung — Fehlerfall
`POST /api/v1/supervisor/study-plans/2/positions`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "exerciseId": 999999
}
```

Response — `HTTP 400`:
```json
{
  "type": "https://pugling.app/errors/invalid_reference",
  "title": "Invalid request.",
  "status": 400,
  "detail": "Exercise 999999 not found.",
  "code": "invalid_reference",
  "traceId": "<trace-id>"
}
```

### Unbekannten Lehrplan lesen — Fehlerfall
`GET /api/v1/supervisor/study-plans/999999`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 404`:
```json
{
  "type": "https://pugling.app/errors/not_found",
  "title": "Resource not found.",
  "status": 404,
  "detail": "Study plan not found.",
  "code": "not_found",
  "traceId": "<trace-id>"
}
```

## Übungssitzung starten (Lern-Modus)
`POST /api/v1/student/study-plans/2/positions/2/practice-sessions`

Rolle: **child** — `Authorization: Bearer <child-token>`

Request:
```json
{
  "mode": "Lern"
}
```

Response — `HTTP 201`:
```json
{
  "id": 1,
  "planId": 2,
  "positionId": 2,
  "day": "2026-07-09",
  "startedAt": "<timestamp>",
  "endedAt": null,
  "activeSeconds": 0,
  "reviewCount": 0,
  "mode": "Lern",
  "cursor": 0,
  "total": 2
}
```

## Nächste Karte (server-geführter Cursor)
`GET /api/v1/student/study-plans/2/positions/2/practice-sessions/1/next`

Rolle: **child** — `Authorization: Bearer <child-token>`

Response — `HTTP 200`:
```json
{
  "card": {
    "itemIndex": 0,
    "stage": 4,
    "type": "Vocabulary",
    "prompt": "hello",
    "hint": null,
    "answerLength": null,
    "reveal": null,
    "choices": null,
    "audioUrl": null
  },
  "done": false,
  "cursor": 0,
  "total": 2
}
```

## Karte bewerten (Review, mit nächster Karte)
`POST /api/v1/student/study-plans/2/positions/2/practice-sessions/1/review`

Rolle: **child** — `Authorization: Bearer <child-token>`

Request:
```json
{
  "itemIndex": 0,
  "givenAnswer": "hallo"
}
```

Response — `HTTP 200`:
```json
{
  "wasCorrect": true,
  "expected": "hallo",
  "awarded": 10,
  "box": 2,
  "dueOn": "2026-07-11",
  "combo": 1,
  "comboBonus": 0,
  "speedBonus": 0,
  "next": {
    "itemIndex": 1,
    "stage": 4,
    "type": "Vocabulary",
    "prompt": "goodbye",
    "hint": null,
    "answerLength": null,
    "reveal": null,
    "choices": null,
    "audioUrl": null
  },
  "done": false
}
```

## Übungssitzung starten (Info-Modus, freies Üben)
`POST /api/v1/student/study-plans/2/positions/2/practice-sessions`

Rolle: **child** — `Authorization: Bearer <child-token>`

Request:
```json
{
  "mode": "Info"
}
```

Response — `HTTP 201`:
```json
{
  "id": 2,
  "planId": 2,
  "positionId": 2,
  "day": "2026-07-09",
  "startedAt": "<timestamp>",
  "endedAt": null,
  "activeSeconds": 0,
  "reviewCount": 0,
  "mode": "Info",
  "cursor": 0,
  "total": 2
}
```

## Karten am Stück (Info-Modus/Offline-Batch)
`GET /api/v1/student/study-plans/2/positions/2/practice-sessions/2/cards`

Rolle: **child** — `Authorization: Bearer <child-token>`

Response — `HTTP 200`:
```json
[
  {
    "itemIndex": 1,
    "stage": 4,
    "type": "Vocabulary",
    "prompt": "goodbye",
    "hint": null,
    "answerLength": null,
    "reveal": null,
    "choices": null,
    "audioUrl": null
  },
  {
    "itemIndex": 0,
    "stage": 4,
    "type": "Vocabulary",
    "prompt": "hello",
    "hint": null,
    "answerLength": null,
    "reveal": null,
    "choices": null,
    "audioUrl": null
  }
]
```

## Review im Info-Modus (kein Feedback → 204)
`POST /api/v1/student/study-plans/2/positions/2/practice-sessions/2/review`

Rolle: **child** — `Authorization: Bearer <child-token>`

Request:
```json
{
  "itemIndex": 0,
  "givenAnswer": "hallo"
}
```

Response — `HTTP 204`:
```json
(kein Inhalt)
```

## Test starten (Klausur, ohne Aufgaben-Bulk)
`POST /api/v1/student/study-plans/2/positions/2/tests`

Rolle: **child** — `Authorization: Bearer <child-token>`

Request:
```json
{}
```

Response — `HTTP 201`:
```json
{
  "attemptId": 1,
  "planId": 2,
  "positionId": 2,
  "day": "2026-07-09",
  "stage": 4,
  "totalItems": 1
}
```

## Nächste Prüfungsfrage (One-at-a-time)
`GET /api/v1/student/study-plans/2/positions/2/tests/1/next`

Rolle: **child** — `Authorization: Bearer <child-token>`

Response — `HTTP 200`:
```json
{
  "item": {
    "itemIndex": 0,
    "prompt": "hello",
    "stage": 4,
    "reveal": null,
    "answerLength": null,
    "hint": null,
    "choices": null,
    "audioUrl": null
  },
  "done": false,
  "cursor": 0,
  "total": 1
}
```

## Prüfungsantwort abgeben (ohne Korrektheit)
`POST /api/v1/student/study-plans/2/positions/2/tests/1/answer`

Rolle: **child** — `Authorization: Bearer <child-token>`

Request:
```json
{
  "givenAnswer": "hallo"
}
```

Response — `HTTP 200`:
```json
{
  "done": true,
  "cursor": 1,
  "total": 1
}
```

## Test abgeben (auswerten)
`POST /api/v1/student/study-plans/2/positions/2/tests/1/submit`

Rolle: **child** — `Authorization: Bearer <child-token>`

Request:
```json
{}
```

Response — `HTTP 200`:
```json
{
  "attemptId": 1,
  "stage": 4,
  "totalItems": 1,
  "correctItems": 1,
  "scorePercent": 100,
  "passed": true,
  "passPercent": 80,
  "items": [
    {
      "itemIndex": 0,
      "prompt": "hello",
      "expected": "hallo",
      "givenAnswer": "hallo",
      "wasCorrect": true
    }
  ]
}
```

### Test erneut abgeben — Fehlerfall
`POST /api/v1/student/study-plans/2/positions/2/tests/1/submit`

Rolle: **child** — `Authorization: Bearer <child-token>`

Request:
```json
{}
```

Response — `HTTP 400`:
```json
{
  "type": "https://pugling.app/errors/test_already_submitted",
  "title": "Test already submitted.",
  "status": 400,
  "detail": "The test has already been submitted.",
  "code": "test_already_submitted",
  "traceId": "<trace-id>"
}
```

## Tagesmission (Overview)
`GET /api/v1/student/study-plans/2/overview`

Rolle: **child** — `Authorization: Bearer <child-token>`

Response — `HTTP 200`:
```json
{
  "planId": 2,
  "title": "Doku-Lehrplan",
  "startDate": "2026-07-09",
  "endDate": "2026-07-18",
  "active": true,
  "currentStreak": 1,
  "today": {
    "day": "2026-07-09",
    "dutyDone": true,
    "goalsTotal": 1,
    "goalsMet": 1,
    "pointsAwarded": 20,
    "outstanding": [],
    "positions": [
      {
        "positionId": 2,
        "exerciseId": 13,
        "exerciseTitle": "Begr\u00FC\u00DFungen",
        "exerciseType": "Vocabulary",
        "renderer": "flashcards",
        "order": 0,
        "cadence": "Daily",
        "checkMode": "StudyPlanTest",
        "useLeitner": true,
        "testable": true,
        "goalMet": true,
        "dueCount": 1,
        "poolSize": 2,
        "pointsGoalMet": 20
      }
    ]
  }
}
```

## Verlauf – Paging & Sortierung (neueste zuerst)
`GET /api/v1/student/study-plans/2/overview/progress?take=3&sort=-day`

Rolle: **child** — `Authorization: Bearer <child-token>`

Response — `HTTP 200`:
```json
{
  "planId": 2,
  "startDate": "2026-07-09",
  "endDate": "2026-07-18",
  "daysComplete": 1,
  "totalDays": 10,
  "totalPoints": 20,
  "currentStreak": 1,
  "days": [
    {
      "day": "2026-07-18",
      "dutyDone": false,
      "goalsTotal": 1,
      "goalsMet": 0,
      "pointsAwarded": 0
    },
    {
      "day": "2026-07-17",
      "dutyDone": false,
      "goalsTotal": 1,
      "goalsMet": 0,
      "pointsAwarded": 0
    },
    {
      "day": "2026-07-16",
      "dutyDone": false,
      "goalsTotal": 1,
      "goalsMet": 0,
      "pointsAwarded": 0
    }
  ]
}
```

## Verlauf – nur erledigte Tage
`GET /api/v1/student/study-plans/2/overview/progress?dutyDone=true`

Rolle: **child** — `Authorization: Bearer <child-token>`

Response — `HTTP 200`:
```json
{
  "planId": 2,
  "startDate": "2026-07-09",
  "endDate": "2026-07-18",
  "daysComplete": 1,
  "totalDays": 10,
  "totalPoints": 20,
  "currentStreak": 1,
  "days": [
    {
      "day": "2026-07-09",
      "dutyDone": true,
      "goalsTotal": 1,
      "goalsMet": 1,
      "pointsAwarded": 20
    }
  ]
}
```

### Test auf Übung ohne prüfbaren Inhalt — Fehlerfall
`POST /api/v1/student/study-plans/2/positions/3/tests`

Rolle: **child** — `Authorization: Bearer <child-token>`

Request:
```json
{}
```

Response — `HTTP 400`:
```json
{
  "type": "https://pugling.app/errors/no_checkable_content",
  "title": "No checkable content.",
  "status": 400,
  "detail": "The exercise contains no checkable content.",
  "code": "no_checkable_content",
  "traceId": "<trace-id>"
}
```

### Bespielte Position löschen — Fehlerfall
`DELETE /api/v1/supervisor/study-plans/2/positions/2`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 409`:
```json
{
  "type": "https://pugling.app/errors/position_has_data",
  "title": "Position has practice/test data.",
  "status": 409,
  "detail": "This position already has practice/test data and cannot be deleted.",
  "code": "position_has_data",
  "traceId": "<trace-id>"
}
```

### Deaktivierten Plan spielen — Fehlerfall
`POST /api/v1/student/study-plans/2/positions/2/practice-sessions`

Rolle: **child** — `Authorization: Bearer <child-token>`

Request:
```json
{}
```

Response — `HTTP 403`:
```json
{
  "type": "https://pugling.app/errors/plan_inactive",
  "title": "Study plan is not active.",
  "status": 403,
  "detail": "This study plan is not currently active. Ask your parent.",
  "code": "plan_inactive",
  "traceId": "<trace-id>"
}
```

