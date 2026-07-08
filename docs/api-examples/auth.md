# API-Beispiele – auth

_Automatisch erzeugt von `DocsCaptureTests` (Integrationstest). Jedes Beispiel ist verifiziert: Status und – bei Fehlern – der maschinenlesbare `code` wurden im Testlauf geprüft. Nicht von Hand bearbeiten._

## Vater registrieren (anonym)
`POST /api/v1/supervisor/fathers`

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
  "createdAt": "<timestamp>",
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
  "expiresAt": "<timestamp>"
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
  "expiresAt": "<timestamp>"
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
  "traceId": "<trace-id>"
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
  "traceId": "<trace-id>"
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
  "traceId": "<trace-id>",
  "code": "unauthorized"
}
```

