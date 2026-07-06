# API-Beispiele – me

_Automatisch erzeugt von `DocsCaptureTests` (Integrationstest). Jedes Beispiel ist verifiziert: Status und – bei Fehlern – der maschinenlesbare `code` wurden im Testlauf geprüft. Nicht von Hand bearbeiten._

## Eigener Punktestand (Wallet)
`GET /api/v1/me/points`

Rolle: **child** — `Authorization: Bearer <child-token>`

Response — `HTTP 200`:
```json
{
  "childId": 1,
  "coins": 50,
  "gems": 300,
  "entries": [
    {
      "id": 2,
      "amount": 300,
      "kind": "Achievement",
      "reason": "Willkommens-Gems",
      "createdAt": "2026-07-06T14:37:03.2303726"
    },
    {
      "id": 1,
      "amount": 50,
      "kind": "Base",
      "reason": "Startguthaben (M\u00FCnzen)",
      "createdAt": "2026-07-06T14:37:03.2302628"
    }
  ]
}
```

## Eigene Missionen
`GET /api/v1/me/missions`

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

## Eigene Auszeichnungen
`GET /api/v1/me/achievements`

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

## Eigener Skin-Zustand
`GET /api/v1/me/skins`

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

## Eigene Angebote & Käufe
`GET /api/v1/me/rewards`

Rolle: **child** — `Authorization: Bearer <child-token>`

Response — `HTTP 200`:
```json
{
  "coins": 50,
  "available": [
    {
      "id": 1,
      "title": "30 Min Fernsehen",
      "cost": 200,
      "period": "Daily",
      "quantity": 2,
      "remainingThisPeriod": 2,
      "affordable": false,
      "planTitle": null,
      "exerciseTitle": null
    },
    {
      "id": 2,
      "title": "1 Stunde Zocken",
      "cost": 400,
      "period": "Weekly",
      "quantity": 5,
      "remainingThisPeriod": 5,
      "affordable": false,
      "planTitle": null,
      "exerciseTitle": null
    },
    {
      "id": 3,
      "title": "Taschengeld 5 \u20AC",
      "cost": 500,
      "period": "Weekly",
      "quantity": 1,
      "remainingThisPeriod": 1,
      "affordable": false,
      "planTitle": null,
      "exerciseTitle": null
    },
    {
      "id": 4,
      "title": "Kinoabend aussuchen",
      "cost": 1500,
      "period": "OneOff",
      "quantity": 1,
      "remainingThisPeriod": 1,
      "affordable": false,
      "planTitle": null,
      "exerciseTitle": null
    }
  ],
  "redemptions": []
}
```

### Vater greift auf Sohn-Route zu — Fehlerfall
`GET /api/v1/me/points`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 403`:
```json
{
  "type": "https://pugling.app/errors/forbidden",
  "title": "Forbidden",
  "status": 403,
  "traceId": "00-2bf3dddc0da7f86f4871a6800e044e3e-6a3a497a158d51ac-00",
  "code": "forbidden"
}
```

### Bereits besessenen Skin kaufen — Fehlerfall
`POST /api/v1/me/skins/pug/purchase`

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
  "traceId": "00-8798682ec4371817ca20a53e036e06aa-0bd29c5f24a9bddc-00"
}
```

### Skin kaufen ohne Gems — Fehlerfall
`POST /api/v1/me/skins/fox/purchase`

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
  "traceId": "00-bf71d90b530642954011829e4c638556-e15a203a1e96f0f4-00"
}
```

### Unbekannten Skin kaufen — Fehlerfall
`POST /api/v1/me/skins/banane/purchase`

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
  "traceId": "00-3a25562ec3f44f4203c756d31c48caec-155b29f9c57c2781-00"
}
```

## Skin kaufen (mit Gems)
`POST /api/v1/me/skins/ninja/purchase`

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
`POST /api/v1/me/skins/pug/equip`

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
`POST /api/v1/me/skins/fox/equip`

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
  "traceId": "00-e947cc8dd05e6de31e320dc2f7b99e8b-de5795bc951e0654-00"
}
```

## Angebot kaufen
`POST /api/v1/me/rewards/5/purchase`

Rolle: **child** — `Authorization: Bearer <child-token>`

Request:
```json
{}
```

Response — `HTTP 200`:
```json
{
  "coins": 450,
  "available": [
    {
      "id": 5,
      "title": "30 Min Fernsehen",
      "cost": 50,
      "period": "Weekly",
      "quantity": 3,
      "remainingThisPeriod": 2,
      "affordable": true,
      "planTitle": null,
      "exerciseTitle": null
    }
  ],
  "redemptions": [
    {
      "id": 1,
      "rewardId": 5,
      "title": "30 Min Fernsehen",
      "cost": 50,
      "status": "Purchased",
      "purchasedAt": "2026-07-06T14:37:15.0118453",
      "fulfilledAt": null
    }
  ]
}
```

## Kauf erfüllen (Vater)
`POST /api/v1/children/4/rewards/redemptions/1/fulfill`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{}
```

Response — `HTTP 200`:
```json
{
  "id": 1,
  "childId": 4,
  "rewardId": 5,
  "title": "30 Min Fernsehen",
  "cost": 50,
  "status": "Fulfilled",
  "purchasedAt": "2026-07-06T14:37:15.0118453",
  "fulfilledAt": "2026-07-06T14:37:15.0561522Z",
  "canFulfill": false,
  "canCancel": false
}
```

### Bereits erfüllten Kauf erneut erfüllen — Fehlerfall
`POST /api/v1/children/4/rewards/redemptions/1/fulfill`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{}
```

Response — `HTTP 409`:
```json
{
  "type": "https://pugling.app/errors/purchase_not_open",
  "title": "Purchase not open.",
  "status": 409,
  "detail": "This purchase is no longer open.",
  "code": "purchase_not_open",
  "traceId": "00-a97e4e089830555420aec3c1817c75d0-be95db07adca46f5-00"
}
```

### Angebot über Kontingent kaufen — Fehlerfall
`POST /api/v1/me/rewards/6/purchase`

Rolle: **child** — `Authorization: Bearer <child-token>`

Request:
```json
{}
```

Response — `HTTP 409`:
```json
{
  "type": "https://pugling.app/errors/quota_exhausted",
  "title": "Quota exhausted.",
  "status": 409,
  "detail": "The quota for this period is exhausted.",
  "code": "quota_exhausted",
  "traceId": "00-a44bbf3335b677dbfbeccbc301c6b862-319daaba2d765caf-00"
}
```

### Deaktiviertes Angebot kaufen — Fehlerfall
`POST /api/v1/me/rewards/7/purchase`

Rolle: **child** — `Authorization: Bearer <child-token>`

Request:
```json
{}
```

Response — `HTTP 400`:
```json
{
  "type": "https://pugling.app/errors/offer_inactive",
  "title": "Offer no longer available.",
  "status": 400,
  "detail": "This offer is no longer available.",
  "code": "offer_inactive",
  "traceId": "00-f42a333340fd0ae08c124a1e774e7d2a-560a77464247bea4-00"
}
```

### Angebot ohne Deckung kaufen — Fehlerfall
`POST /api/v1/me/rewards/8/purchase`

Rolle: **child** — `Authorization: Bearer <child-token>`

Request:
```json
{}
```

Response — `HTTP 400`:
```json
{
  "type": "https://pugling.app/errors/insufficient_coins",
  "title": "Not enough coins.",
  "status": 400,
  "detail": "Not enough coins for this offer.",
  "code": "insufficient_coins",
  "traceId": "00-89f3641ee8faa018cdb897c235ae8e07-2b6bde33e0deea63-00"
}
```

