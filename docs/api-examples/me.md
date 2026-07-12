# API-Beispiele – me

_Automatisch erzeugt von `DocsCaptureTests` (Integrationstest). Jedes Beispiel ist verifiziert: Status und – bei Fehlern – der maschinenlesbare `code` wurden im Testlauf geprüft. Nicht von Hand bearbeiten._

## Eigener Kontostand (Wallet)
`GET /api/v1/student/me/points`

Rolle: **child** — `Authorization: Bearer <child-token>`

Response — `HTTP 200`:
```json
{
  "childId": 1,
  "coins": 50,
  "gems": 300
}
```

## Eigene Buchungen (Liste)
`GET /api/v1/student/me/points/entries`

Rolle: **child** — `Authorization: Bearer <child-token>`

Response — `HTTP 200`:
```json
[
  {
    "id": 3,
    "amount": 15,
    "kind": "Base",
    "reason": "Doku-Buchung",
    "createdAt": "2026-07-12T21:09:03.7538574"
  },
  {
    "id": 2,
    "amount": 300,
    "kind": "Achievement",
    "reason": "Willkommens-Gems",
    "createdAt": "2026-07-12T21:08:54.3550135"
  },
  {
    "id": 1,
    "amount": 50,
    "kind": "Base",
    "reason": "Startguthaben (M\u00FCnzen)",
    "createdAt": "2026-07-12T21:08:54.3550128"
  }
]
```

## Einzelne Buchung
`GET /api/v1/student/me/points/entries/3`

Rolle: **child** — `Authorization: Bearer <child-token>`

Response — `HTTP 200`:
```json
{
  "id": 3,
  "amount": 15,
  "kind": "Base",
  "reason": "Doku-Buchung",
  "createdAt": "2026-07-12T21:09:03.7538574"
}
```

## Eigene Missionen (Liste)
`GET /api/v1/student/me/missions`

Rolle: **child** — `Authorization: Bearer <child-token>`

Response — `HTTP 200`:
```json
[
  {
    "id": 1,
    "title": "Tagesziel: 10 richtige Antworten",
    "metric": "CorrectReviews",
    "period": "Daily",
    "target": 10,
    "current": 0,
    "completed": false,
    "rewardPoints": 15
  },
  {
    "id": 2,
    "title": "Tagesziel: 15 Minuten \u00FCben",
    "metric": "MinutesPracticed",
    "period": "Daily",
    "target": 15,
    "current": 0,
    "completed": false,
    "rewardPoints": 10
  },
  {
    "id": 3,
    "title": "Wochenziel: 3 Tests bestehen",
    "metric": "TestsPassed",
    "period": "Weekly",
    "target": 3,
    "current": 0,
    "completed": false,
    "rewardPoints": 30
  },
  {
    "id": 4,
    "title": "Wochenziel: 25 neue W\u00F6rter",
    "metric": "NewWords",
    "period": "Weekly",
    "target": 25,
    "current": 0,
    "completed": false,
    "rewardPoints": 40
  }
]
```

## Einzelne Mission
`GET /api/v1/student/me/missions/1`

Rolle: **child** — `Authorization: Bearer <child-token>`

Response — `HTTP 200`:
```json
{
  "id": 1,
  "title": "Tagesziel: 10 richtige Antworten",
  "metric": "CorrectReviews",
  "period": "Daily",
  "target": 10,
  "current": 0,
  "completed": false,
  "rewardPoints": 15
}
```

## Eigene Auszeichnungen (Liste)
`GET /api/v1/student/me/achievements`

Rolle: **child** — `Authorization: Bearer <child-token>`

Response — `HTTP 200`:
```json
[
  {
    "id": 4,
    "title": "Feuer-Streak",
    "icon": "\uD83D\uDD25",
    "metric": "StreakDays",
    "threshold": 7,
    "current": 0,
    "earned": false,
    "earnedAt": null,
    "rewardPoints": 70
  },
  {
    "id": 3,
    "title": "Test-Ass",
    "icon": "\uD83C\uDFC6",
    "metric": "TestsPassed",
    "threshold": 10,
    "current": 0,
    "earned": false,
    "earnedAt": null,
    "rewardPoints": 40
  },
  {
    "id": 1,
    "title": "Erste Schritte",
    "icon": "\uD83C\uDF31",
    "metric": "CorrectReviews",
    "threshold": 50,
    "current": 0,
    "earned": false,
    "earnedAt": null,
    "rewardPoints": 20
  },
  {
    "id": 2,
    "title": "Wortschatz-Sammler",
    "icon": "\uD83D\uDCDA",
    "metric": "NewWords",
    "threshold": 100,
    "current": 0,
    "earned": false,
    "earnedAt": null,
    "rewardPoints": 50
  },
  {
    "id": 5,
    "title": "Marathon",
    "icon": "\u23F1\uFE0F",
    "metric": "MinutesPracticed",
    "threshold": 300,
    "current": 0,
    "earned": false,
    "earnedAt": null,
    "rewardPoints": 60
  }
]
```

## Einzelne Auszeichnung
`GET /api/v1/student/me/achievements/4`

Rolle: **child** — `Authorization: Bearer <child-token>`

Response — `HTTP 200`:
```json
{
  "id": 4,
  "title": "Feuer-Streak",
  "icon": "\uD83D\uDD25",
  "metric": "StreakDays",
  "threshold": 7,
  "current": 0,
  "earned": false,
  "earnedAt": null,
  "rewardPoints": 70
}
```

## Eigener Skin-Zustand
`GET /api/v1/student/me/skins`

Rolle: **child** — `Authorization: Bearer <child-token>`

Response — `HTTP 200`:
```json
{
  "gems": 300,
  "selected": "pug",
  "owned": [
    "pug"
  ]
}
```

### Vater greift auf Sohn-Route zu — Fehlerfall
`GET /api/v1/student/me/points`

Rolle: **father** — `Authorization: Bearer <father-token>`

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

### Bereits besessenen Skin kaufen — Fehlerfall
`POST /api/v1/student/me/skins/pug/purchase`

Rolle: **child** — `Authorization: Bearer <child-token>`

Request:
```json
{}
```

Response — `HTTP 409`:
```json
{
  "type": "https://pugling.app/errors/skin_already_unlocked",
  "title": "Skin already unlocked.",
  "status": 409,
  "detail": "This skin is already unlocked.",
  "code": "skin_already_unlocked",
  "traceId": "<trace-id>"
}
```

### Skin kaufen ohne Gems — Fehlerfall
`POST /api/v1/student/me/skins/fox/purchase`

Rolle: **child** — `Authorization: Bearer <child-token>`

Request:
```json
{}
```

Response — `HTTP 400`:
```json
{
  "type": "https://pugling.app/errors/insufficient_gems",
  "title": "Not enough gems.",
  "status": 400,
  "detail": "Not enough gems: 0/300 for \u0027fox\u0027.",
  "code": "insufficient_gems",
  "traceId": "<trace-id>"
}
```

### Unbekannten Skin kaufen — Fehlerfall
`POST /api/v1/student/me/skins/banane/purchase`

Rolle: **child** — `Authorization: Bearer <child-token>`

Request:
```json
{}
```

Response — `HTTP 404`:
```json
{
  "type": "https://pugling.app/errors/not_found",
  "title": "Resource not found.",
  "status": 404,
  "detail": "Unknown skin \u0027banane\u0027.",
  "code": "not_found",
  "traceId": "<trace-id>"
}
```

## Skin kaufen (mit Gems)
`POST /api/v1/student/me/skins/ninja/purchase`

Rolle: **child** — `Authorization: Bearer <child-token>`

Request:
```json
{}
```

Response — `HTTP 200`:
```json
{
  "gems": 500,
  "selected": "ninja",
  "owned": [
    "pug",
    "ninja"
  ]
}
```

## Besessenen Skin ausrüsten
`POST /api/v1/student/me/skins/pug/equip`

Rolle: **child** — `Authorization: Bearer <child-token>`

Request:
```json
{}
```

Response — `HTTP 200`:
```json
{
  "gems": 500,
  "selected": "pug",
  "owned": [
    "pug",
    "ninja"
  ]
}
```

### Nicht besessenen Skin ausrüsten — Fehlerfall
`POST /api/v1/student/me/skins/fox/equip`

Rolle: **child** — `Authorization: Bearer <child-token>`

Request:
```json
{}
```

Response — `HTTP 400`:
```json
{
  "type": "https://pugling.app/errors/skin_not_unlocked",
  "title": "Skin not unlocked.",
  "status": 400,
  "detail": "This skin is not unlocked yet.",
  "code": "skin_not_unlocked",
  "traceId": "<trace-id>"
}
```

