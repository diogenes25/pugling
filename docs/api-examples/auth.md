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
  "createdAt": "2026-07-06T14:37:10.5532478Z",
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
  "expiresAt": "2026-07-07T02:37:10.6814666Z"
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
  "expiresAt": "2026-07-07T02:37:10.7186876Z"
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
  "traceId": "00-188b4f43111583d676e61fbff117406f-73229dc0ba85b772-00"
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
  "traceId": "00-df32c4cc2a5e48aeb5a0371025a13e16-ac8f7370e9e2b23a-00"
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
  "traceId": "00-eea58bca874d3eb4f1b750a75f3ed86c-173ba6af72c0031d-00",
  "code": "unauthorized"
}
```

