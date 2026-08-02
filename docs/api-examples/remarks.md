# API-Beispiele – remarks

_Automatisch erzeugt von `DocsCaptureTests` (Integrationstest). Jedes Beispiel ist verifiziert: Status und – bei Fehlern – der maschinenlesbare `code` wurden im Testlauf geprüft. Nicht von Hand bearbeiten._

## Anmerkung erfassen (mit Kontext)

`POST /api/v1/remarks`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:

```json
{
  "text": "Ich will meine E-Mail-Adresse \u00E4ndern und finde keine Stelle daf\u00FCr.",
  "category": "Question",
  "context": {
    "route": "/vater/kind/1",
    "appArea": "vater",
    "childId": 1,
    "exerciseId": 13,
    "contextJson": "{\u0022tab\u0022:\u0022stammdaten\u0022}",
    "recentErrorsJson": "[{\u0022method\u0022:\u0022GET\u0022,\u0022path\u0022:\u0022/api/v1/supervisor/adults/1\u0022,\u0022status\u0022:404,\u0022code\u0022:\u0022not_found\u0022,\u0022at\u0022:\u00222026-07-27T09:12:44Z\u0022}]"
  }
}
```

Response — `HTTP 201`:

```json
{
  "id": 1,
  "text": "Ich will meine E-Mail-Adresse \u00E4ndern und finde keine Stelle daf\u00FCr.",
  "category": "Question",
  "status": "Open",
  "answer": null,
  "answeredAt": null,
  "answeredBy": null,
  "parentRemarkId": null,
  "accountId": 1,
  "authorRole": "Supervisor",
  "isOwn": true,
  "context": {
    "route": "/vater/kind/1",
    "appArea": "vater",
    "childId": 1,
    "exerciseId": 13,
    "studyPlanId": null,
    "planPositionId": null,
    "contextJson": "{\u0022tab\u0022:\u0022stammdaten\u0022}",
    "recentErrorsJson": "[{\u0022method\u0022:\u0022GET\u0022,\u0022path\u0022:\u0022/api/v1/supervisor/adults/1\u0022,\u0022status\u0022:404,\u0022code\u0022:\u0022not_found\u0022,\u0022at\u0022:\u00222026-07-27T09:12:44Z\u0022}]"
  },
  "userAgent": null,
  "createdAt": "<timestamp>",
  "commentCount": 0
}
```

### Anmerkung ohne Text erfassen — Fehlerfall

`POST /api/v1/remarks`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:

```json
{
  "text": "   "
}
```

Response — `HTTP 400`:

```json
{
  "type": "https://pugling.app/errors/validation_error",
  "title": "Invalid request.",
  "status": 400,
  "detail": "Text is required.",
  "code": "validation_error",
  "traceId": "<trace-id>"
}
```

## Anmerkung zur Log-Id lesen

`GET /api/v1/remarks/1`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 200`:

```json
{
  "id": 1,
  "text": "Ich will meine E-Mail-Adresse \u00E4ndern und finde keine Stelle daf\u00FCr.",
  "category": "Question",
  "status": "Open",
  "answer": null,
  "answeredAt": null,
  "answeredBy": null,
  "parentRemarkId": null,
  "accountId": 1,
  "authorRole": "Supervisor",
  "isOwn": true,
  "context": {
    "route": "/vater/kind/1",
    "appArea": "vater",
    "childId": 1,
    "exerciseId": 13,
    "studyPlanId": null,
    "planPositionId": null,
    "contextJson": "{\u0022tab\u0022:\u0022stammdaten\u0022}",
    "recentErrorsJson": "[{\u0022method\u0022:\u0022GET\u0022,\u0022path\u0022:\u0022/api/v1/supervisor/adults/1\u0022,\u0022status\u0022:404,\u0022code\u0022:\u0022not_found\u0022,\u0022at\u0022:\u00222026-07-27T09:12:44Z\u0022}]"
  },
  "userAgent": null,
  "createdAt": "<timestamp>",
  "commentCount": 0
}
```

## Eigene Anmerkungen (Liste im Widget)

`GET /api/v1/remarks?mine=true&take=5`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 200`:

```json
[
  {
    "id": 1,
    "text": "Ich will meine E-Mail-Adresse \u00E4ndern und finde keine Stelle daf\u00FCr.",
    "category": "Question",
    "status": "Open",
    "answer": null,
    "answeredAt": null,
    "answeredBy": null,
    "parentRemarkId": null,
    "accountId": 1,
    "authorRole": "Supervisor",
    "isOwn": true,
    "context": {
      "route": "/vater/kind/1",
      "appArea": "vater",
      "childId": 1,
      "exerciseId": 13,
      "studyPlanId": null,
      "planPositionId": null,
      "contextJson": "{\u0022tab\u0022:\u0022stammdaten\u0022}",
      "recentErrorsJson": "[{\u0022method\u0022:\u0022GET\u0022,\u0022path\u0022:\u0022/api/v1/supervisor/adults/1\u0022,\u0022status\u0022:404,\u0022code\u0022:\u0022not_found\u0022,\u0022at\u0022:\u00222026-07-27T09:12:44Z\u0022}]"
    },
    "userAgent": null,
    "createdAt": "<timestamp>",
    "commentCount": 0
  }
]
```

## Antwort zurückschreiben und abschließen

`PATCH /api/v1/remarks/1`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:

```json
{
  "answer": "Die API kann das \u00FCber PATCH api/v1/supervisor/adults/{id} (AdultsController.Update); im Vater-Web gibt es daf\u00FCr kein Formular.",
  "answeredBy": "claude-code",
  "status": "Done"
}
```

Response — `HTTP 200`:

```json
{
  "id": 1,
  "text": "Ich will meine E-Mail-Adresse \u00E4ndern und finde keine Stelle daf\u00FCr.",
  "category": "Question",
  "status": "Done",
  "answer": "Die API kann das \u00FCber PATCH api/v1/supervisor/adults/{id} (AdultsController.Update); im Vater-Web gibt es daf\u00FCr kein Formular.",
  "answeredAt": "<timestamp>",
  "answeredBy": "claude-code",
  "parentRemarkId": null,
  "accountId": 1,
  "authorRole": "Supervisor",
  "isOwn": true,
  "context": {
    "route": "/vater/kind/1",
    "appArea": "vater",
    "childId": 1,
    "exerciseId": 13,
    "studyPlanId": null,
    "planPositionId": null,
    "contextJson": "{\u0022tab\u0022:\u0022stammdaten\u0022}",
    "recentErrorsJson": "[{\u0022method\u0022:\u0022GET\u0022,\u0022path\u0022:\u0022/api/v1/supervisor/adults/1\u0022,\u0022status\u0022:404,\u0022code\u0022:\u0022not_found\u0022,\u0022at\u0022:\u00222026-07-27T09:12:44Z\u0022}]"
  },
  "userAgent": null,
  "createdAt": "<timestamp>",
  "commentCount": 0
}
```

## Umsetzungsnotiz in den Verlauf schreiben

`POST /api/v1/remarks/1/comments`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:

```json
{
  "body": "Gebaut: Formular unter /vater/profil erg\u00E4nzt (VaterProfil.tsx), PATCH \u00FCber api.updateAdult.",
  "author": "Assistant",
  "authorLabel": "claude-code"
}
```

Response — `HTTP 201`:

```json
{
  "id": 1,
  "remarkId": 1,
  "body": "Gebaut: Formular unter /vater/profil erg\u00E4nzt (VaterProfil.tsx), PATCH \u00FCber api.updateAdult.",
  "author": "Assistant",
  "authorLabel": "claude-code",
  "authorAccountId": 1,
  "isOwn": true,
  "createdAt": "<timestamp>"
}
```

## Verlauf einer Anmerkung lesen

`GET /api/v1/remarks/1/comments`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 200`:

```json
[
  {
    "id": 1,
    "remarkId": 1,
    "body": "Gebaut: Formular unter /vater/profil erg\u00E4nzt (VaterProfil.tsx), PATCH \u00FCber api.updateAdult.",
    "author": "Assistant",
    "authorLabel": "claude-code",
    "authorAccountId": 1,
    "isOwn": true,
    "createdAt": "<timestamp>"
  }
]
```

### Leeren Beitrag schreiben — Fehlerfall

`POST /api/v1/remarks/1/comments`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:

```json
{
  "body": "   "
}
```

Response — `HTTP 400`:

```json
{
  "type": "https://pugling.app/errors/validation_error",
  "title": "Invalid request.",
  "status": 400,
  "detail": "Body is required.",
  "code": "validation_error",
  "traceId": "<trace-id>"
}
```

## Folgeanmerkung mit Verweis anlegen

`POST /api/v1/remarks`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:

```json
{
  "text": "Formular f\u00FCr die E-Mail-Adresse im Vater-Web nachziehen.",
  "category": "Idea",
  "parentRemarkId": 1
}
```

Response — `HTTP 201`:

```json
{
  "id": 2,
  "text": "Formular f\u00FCr die E-Mail-Adresse im Vater-Web nachziehen.",
  "category": "Idea",
  "status": "Open",
  "answer": null,
  "answeredAt": null,
  "answeredBy": null,
  "parentRemarkId": 1,
  "accountId": 1,
  "authorRole": "Supervisor",
  "isOwn": true,
  "context": {
    "route": "",
    "appArea": "",
    "childId": null,
    "exerciseId": null,
    "studyPlanId": null,
    "planPositionId": null,
    "contextJson": null,
    "recentErrorsJson": null
  },
  "userAgent": null,
  "createdAt": "<timestamp>",
  "commentCount": 0
}
```

### Verweis auf unbekannte Vorgänger-Anmerkung — Fehlerfall

`POST /api/v1/remarks`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:

```json
{
  "text": "Bezug ins Leere",
  "parentRemarkId": 999999
}
```

Response — `HTTP 400`:

```json
{
  "type": "https://pugling.app/errors/invalid_reference",
  "title": "Invalid request.",
  "status": 400,
  "detail": "Parent remark not found.",
  "code": "invalid_reference",
  "traceId": "<trace-id>"
}
```

### Fremde Anmerkung lesen (Sohn) — Fehlerfall

`GET /api/v1/remarks/1`

Rolle: **child** — `Authorization: Bearer <child-token>`

Response — `HTTP 404`:

```json
{
  "type": "https://pugling.app/errors/remark_not_found",
  "title": "Remark not found.",
  "status": 404,
  "detail": "Remark not found.",
  "code": "remark_not_found",
  "traceId": "<trace-id>"
}
```

### Unbekannte Anmerkung lesen — Fehlerfall

`GET /api/v1/remarks/999999`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 404`:

```json
{
  "type": "https://pugling.app/errors/remark_not_found",
  "title": "Remark not found.",
  "status": 404,
  "detail": "Remark not found.",
  "code": "remark_not_found",
  "traceId": "<trace-id>"
}
```

## Anmerkungen als Markdown exportieren

`GET /api/v1/remarks/export?status=Done`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 200` (`text/markdown`):

````markdown
# Anmerkungen – Export

Stand: <timestamp> · 1 Eintrag · Filter: status=Done

> Erzeugt von `GET api/v1/remarks/export`. **Nicht von Hand bearbeiten** – die Quelle ist
> die Datenbank. Status und Antworten ändert der Skill `anmerkungen` über die API.

## #1 · Question · erledigt

- **Erfasst:** <timestamp> von Konto 1 (Supervisor)
- **Wo:** `/vater/kind/1` (vater)
- **Bezug:** Kind 1, Übung 13

Ich will meine E-Mail-Adresse ändern und finde keine Stelle dafür.

**Zustand:**

```json
{"tab":"stammdaten"}
```

**Letzte Fehler:**

```json
[{"method":"GET","path":"/api/v1/supervisor/adults/1","status":404,"code":"not_found","at":"<timestamp>"}]
```

**Antwort** (claude-code, <timestamp>):

Die API kann das über PATCH api/v1/supervisor/adults/{id} (AdultsController.Update); im Vater-Web gibt es dafür kein Formular.

**Verlauf** (1):

> **claude-code** · <timestamp>
>
> Gebaut: Formular unter /vater/profil ergänzt (VaterProfil.tsx), PATCH über api.updateAdult.

````

### Export als Sohn abrufen — Fehlerfall

`GET /api/v1/remarks/export`

Rolle: **child** — `Authorization: Bearer <child-token>`

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

## Nachhaken (holt die Anmerkung zurück auf offen)

`POST /api/v1/remarks/1/comments`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:

```json
{
  "body": "Und wie \u00E4ndere ich die Adresse des Kindes?"
}
```

Response — `HTTP 201`:

```json
{
  "id": 2,
  "remarkId": 1,
  "body": "Und wie \u00E4ndere ich die Adresse des Kindes?",
  "author": "Human",
  "authorLabel": "Papa",
  "authorAccountId": 1,
  "isOwn": true,
  "createdAt": "<timestamp>"
}
```

## Anmerkung nach dem Nachhaken lesen

`GET /api/v1/remarks/1`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 200`:

```json
{
  "id": 1,
  "text": "Ich will meine E-Mail-Adresse \u00E4ndern und finde keine Stelle daf\u00FCr.",
  "category": "Question",
  "status": "Open",
  "answer": "Die API kann das \u00FCber PATCH api/v1/supervisor/adults/{id} (AdultsController.Update); im Vater-Web gibt es daf\u00FCr kein Formular.",
  "answeredAt": "<timestamp>",
  "answeredBy": "claude-code",
  "parentRemarkId": null,
  "accountId": 1,
  "authorRole": "Supervisor",
  "isOwn": true,
  "context": {
    "route": "/vater/kind/1",
    "appArea": "vater",
    "childId": 1,
    "exerciseId": 13,
    "studyPlanId": null,
    "planPositionId": null,
    "contextJson": "{\u0022tab\u0022:\u0022stammdaten\u0022}",
    "recentErrorsJson": "[{\u0022method\u0022:\u0022GET\u0022,\u0022path\u0022:\u0022/api/v1/supervisor/adults/1\u0022,\u0022status\u0022:404,\u0022code\u0022:\u0022not_found\u0022,\u0022at\u0022:\u00222026-07-27T09:12:44Z\u0022}]"
  },
  "userAgent": null,
  "createdAt": "<timestamp>",
  "commentCount": 2
}
```

## Anmerkungen aller Konten lesen (scope=all)

`GET /api/v1/remarks?scope=all&take=5`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 200`:

```json
[
  {
    "id": 2,
    "text": "Formular f\u00FCr die E-Mail-Adresse im Vater-Web nachziehen.",
    "category": "Idea",
    "status": "Open",
    "answer": null,
    "answeredAt": null,
    "answeredBy": null,
    "parentRemarkId": 1,
    "accountId": 1,
    "authorRole": "Supervisor",
    "isOwn": true,
    "context": {
      "route": "",
      "appArea": "",
      "childId": null,
      "exerciseId": null,
      "studyPlanId": null,
      "planPositionId": null,
      "contextJson": null,
      "recentErrorsJson": null
    },
    "userAgent": null,
    "createdAt": "<timestamp>",
    "commentCount": 0
  },
  {
    "id": 1,
    "text": "Ich will meine E-Mail-Adresse \u00E4ndern und finde keine Stelle daf\u00FCr.",
    "category": "Question",
    "status": "Open",
    "answer": "Die API kann das \u00FCber PATCH api/v1/supervisor/adults/{id} (AdultsController.Update); im Vater-Web gibt es daf\u00FCr kein Formular.",
    "answeredAt": "<timestamp>",
    "answeredBy": "claude-code",
    "parentRemarkId": null,
    "accountId": 1,
    "authorRole": "Supervisor",
    "isOwn": true,
    "context": {
      "route": "/vater/kind/1",
      "appArea": "vater",
      "childId": 1,
      "exerciseId": 13,
      "studyPlanId": null,
      "planPositionId": null,
      "contextJson": "{\u0022tab\u0022:\u0022stammdaten\u0022}",
      "recentErrorsJson": "[{\u0022method\u0022:\u0022GET\u0022,\u0022path\u0022:\u0022/api/v1/supervisor/adults/1\u0022,\u0022status\u0022:404,\u0022code\u0022:\u00
… (gekürzt)
```

### Alle Konten lesen als Sohn — Fehlerfall

`GET /api/v1/remarks?scope=all`

Rolle: **child** — `Authorization: Bearer <child-token>`

Response — `HTTP 403`:

```json
{
  "type": "https://pugling.app/errors/remark_scope_forbidden",
  "title": "Reading across accounts is disabled on this instance.",
  "status": 403,
  "detail": "Reading across accounts is disabled on this instance.",
  "code": "remark_scope_forbidden",
  "traceId": "<trace-id>"
}
```

## Anmerkung löschen

`DELETE /api/v1/remarks/1`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 204`:

```json
(kein Inhalt)
```
