# Pugling API Bruno Collection

Diese Collection ist generiert. Manuelle Änderungen an Requests gehen beim nächsten Export verloren.

## Aktualisieren

`npm run bruno:generate`

Standardquelle ist `http://localhost:5200/openapi/v1.json`. Alternativ kann eine gespeicherte OpenAPI-Datei genutzt werden:

`node tools/bruno/generate-bruno.mjs --input ./openapi.json --output tools/bruno/Pugling.Api`

## Variablen

Pfad- und Body-Werte werden als Bruno-Variablen gesetzt, z. B. `{{fatherId}}`, `{{childId}}`, `{{planId}}`, `{{positionId}}`. Die Login-Requests speichern Tokens und IDs per Post-Response-Script persistent ins aktive Environment.

Vater-Endpunkte senden `Authorization: Bearer {{fatherToken}}`; Sohn-Endpunkte unter `/api/v1/me` senden `Authorization: Bearer {{childToken}}`.
