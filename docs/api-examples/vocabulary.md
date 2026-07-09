# API-Beispiele – vocabulary

_Automatisch erzeugt von `DocsCaptureTests` (Integrationstest). Jedes Beispiel ist verifiziert: Status und – bei Fehlern – der maschinenlesbare `code` wurden im Testlauf geprüft. Nicht von Hand bearbeiten._

## Vokabel anlegen
`POST /api/v1/creator/vocabulary`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "key": "en_doku_de_beispiel",
  "sourceLanguage": "en",
  "targetLanguage": "de",
  "word": "example",
  "translation": "Beispiel",
  "partOfSpeech": "Noun"
}
```

Response — `HTTP 201`:
```json
{
  "id": 26,
  "key": "en_doku_de_beispiel",
  "version": "1.0",
  "sourceLanguage": "en",
  "targetLanguage": "de",
  "word": "example",
  "translation": "Beispiel",
  "partOfSpeech": "Noun",
  "noun": null,
  "verb": null,
  "baseFormId": null,
  "baseFormKey": null,
  "baseFormRelation": null,
  "pronunciationAudioUrl": null,
  "tags": [],
  "createdAt": "<timestamp>"
}
```

### Vokabel mit doppeltem Key — Fehlerfall
`POST /api/v1/creator/vocabulary`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "key": "en_doku_de_beispiel",
  "sourceLanguage": "en",
  "targetLanguage": "de",
  "word": "example",
  "translation": "Beispiel",
  "partOfSpeech": "Noun"
}
```

Response — `HTTP 409`:
```json
{
  "type": "https://pugling.app/errors/duplicate_key",
  "title": "Key already exists.",
  "status": 409,
  "detail": "Key \u0027en_doku_de_beispiel\u0027 already exists.",
  "code": "duplicate_key",
  "traceId": "<trace-id>"
}
```

## Grundform-Vokabel lesen
`GET /api/v1/creator/vocabulary/by-key/en_go_de_gehen`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 200`:
```json
{
  "id": 2,
  "key": "en_go_de_gehen",
  "version": "1.0",
  "sourceLanguage": "en",
  "targetLanguage": "de",
  "word": "go",
  "translation": "gehen",
  "partOfSpeech": "Verb",
  "noun": null,
  "verb": {
    "isBaseForm": true,
    "infinitive": "gehen",
    "tense": null,
    "person": null,
    "number": null
  },
  "baseFormId": null,
  "baseFormKey": null,
  "baseFormRelation": null,
  "pronunciationAudioUrl": null,
  "tags": [],
  "createdAt": "2026-07-09T15:25:16.4004807"
}
```

### Verwendete Grundform löschen — Fehlerfall
`DELETE /api/v1/creator/vocabulary/2`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 409`:
```json
{
  "type": "https://pugling.app/errors/vocabulary_in_use",
  "title": "Vocabulary item is in use.",
  "status": 409,
  "detail": "The vocabulary item is the base form of other entries and cannot be deleted.",
  "code": "vocabulary_in_use",
  "traceId": "<trace-id>"
}
```

