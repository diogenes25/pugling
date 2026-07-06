# API-Beispiele – auth

_Automatisch erzeugt von `DocsCaptureTests` (Integrationstest). Jedes Beispiel ist verifiziert: Status und – bei Fehlern – der maschinenlesbare `code` wurden im Testlauf geprüft. Nicht von Hand bearbeiten._

## Vater registrieren (anonym)
`POST /api/v1/fathers`

Rolle: **anonymous** — _(kein Token)_

Request:
```json
{
  "name": "Neuer Papa",
  "pin": "1234"
}
```

Response — `HTTP 201`:
```json
{
  "id": 4,
  "name": "Neuer Papa",
  "email": null,
  "createdAt": "2026-07-06T16:55:31.9987095Z",
  "childrenCount": 0
}
```

## Vater-Login
`POST /api/v1/auth/father`

Rolle: **anonymous** — _(kein Token)_

Request:
```json
{
  "fatherId": 1,
  "pin": "0000"
}
```

Response — `HTTP 200`:
```json
{
  "token": "<redacted-jwt>",
  "role": "Vater",
  "id": 1,
  "name": "Papa",
  "expiresAt": "2026-07-07T04:55:32.0603297Z"
}
```

## Sohn-Login
`POST /api/v1/auth/child`

Rolle: **anonymous** — _(kein Token)_

Request:
```json
{
  "childId": 1,
  "pin": "1111"
}
```

Response — `HTTP 200`:
```json
{
  "token": "<redacted-jwt>",
  "role": "Sohn",
  "id": 1,
  "name": "Sohn",
  "expiresAt": "2026-07-07T04:55:32.0793147Z"
}
```

### Login mit falscher PIN — Fehlerfall
`POST /api/v1/auth/father`

Rolle: **anonymous** — _(kein Token)_

Request:
```json
{
  "fatherId": 1,
  "pin": "9998"
}
```

Response — `HTTP 401`:
```json
{
  "type": "https://pugling.app/errors/invalid_credentials",
  "title": "Invalid credentials.",
  "status": 401,
  "detail": "Invalid father ID or PIN.",
  "code": "invalid_credentials",
  "traceId": "00-93ce2cdcc6af07dea42229ab07ad4ef2-1e0945c98e03649c-00"
}
```

### Login mit nicht-numerischer fatherId — Fehlerfall
`POST /api/v1/auth/father`

Rolle: **anonymous** — _(kein Token)_

Request:
```json
{
  "fatherId": "1a",
  "pin": "0000"
}
```

Response — `HTTP 400`:
```json
{
  "type": "https://pugling.app/errors/validation_error",
  "title": "Invalid request.",
  "status": 400,
  "errors": {
    "fatherId": [
      "The value is not of the expected type."
    ]
  },
  "code": "validation_error",
  "traceId": "00-53cc033dc8f2a75e4d86526a117af9bd-570d400aab207845-00"
}
```

### Selbstauskunft ohne Token — Fehlerfall
`GET /api/v1/auth/me`

Rolle: **anonymous** — _(kein Token)_

Response — `HTTP 401`:
```json
{
  "type": "https://pugling.app/errors/unauthorized",
  "title": "Unauthorized",
  "status": 401,
  "traceId": "00-c7f87e27eb38947dfa0eff197dbc1d22-f11541a50f328fd8-00",
  "code": "unauthorized"
}
```

