# API-Beispiele – timetable

_Automatisch erzeugt von `DocsCaptureTests` (Integrationstest). Jedes Beispiel ist verifiziert: Status und – bei Fehlern – der maschinenlesbare `code` wurden im Testlauf geprüft. Nicht von Hand bearbeiten._

## Stundenplan-Eintrag anlegen
`POST /api/v1/supervisor/children/1/timetable`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "subjectId": 5,
  "dayOfWeek": "Tuesday",
  "timeOfDay": "Nachmittag"
}
```

Response — `HTTP 201`:
```json
{
  "id": 1,
  "childId": 1,
  "subjectId": 5,
  "subjectName": "Doku-Fach",
  "dayOfWeek": "Tuesday",
  "timeOfDay": "Nachmittag"
}
```

### Gleiches Fach am selben Wochentag — Fehlerfall
`POST /api/v1/supervisor/children/1/timetable`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "subjectId": 5,
  "dayOfWeek": "Tuesday",
  "timeOfDay": "Vormittag"
}
```

Response — `HTTP 409`:
```json
{
  "type": "https://pugling.app/errors/timetable_slot_taken",
  "title": "Timetable slot already taken.",
  "status": 409,
  "detail": "This subject is already scheduled on this weekday.",
  "code": "timetable_slot_taken",
  "traceId": "<trace-id>"
}
```

