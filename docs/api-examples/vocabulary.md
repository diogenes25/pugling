# API-Beispiele – vocabulary

_Automatisch erzeugt von `DocsCaptureTests` (Integrationstest). Jedes Beispiel ist verifiziert: Status und – bei Fehlern – der maschinenlesbare `code` wurden im Testlauf geprüft. Nicht von Hand bearbeiten._

## Vokabel anlegen
`POST /api/v1/learn/vocabulary`

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
  "id": 18,
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
  "createdAt": "2026-07-06T16:55:34.6333256Z"
}
```

### Vokabel mit doppeltem Key — Fehlerfall
`POST /api/v1/learn/vocabulary`

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
  "traceId": "00-fe3eae8f6207cac599eae02ed3ce43cc-7564dcf40cd7ebc6-00"
}
```

## Grundform-Vokabel lesen
`GET /api/v1/learn/vocabulary/by-key/en_go_de_gehen`

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
  "createdAt": "2026-07-06T16:55:30.3225691"
}
```

### Verwendete Grundform löschen — Fehlerfall
`DELETE /api/v1/learn/vocabulary/2`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 409`:
```json
{
  "type": "https://pugling.app/errors/vocabulary_in_use",
  "title": "Vocabulary item is in use.",
  "status": 409,
  "detail": "The vocabulary item is the base form of other entries and cannot be deleted.",
  "code": "vocabulary_in_use",
  "traceId": "00-60a80a9671196c005001c663e1617262-256941675e54b34d-00"
}
```

