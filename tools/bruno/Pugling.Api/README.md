# Pugling API Bruno Collection

Diese Collection ist generiert. Manuelle Änderungen an Requests gehen beim nächsten Export verloren.

## Aktualisieren

`npm run bruno:generate`

Standardquelle ist `http://localhost:5200/openapi/v1.json`. Alternativ kann eine gespeicherte OpenAPI-Datei genutzt werden:

`node tools/bruno/generate-bruno.mjs --input ./openapi.json --output tools/bruno/Pugling.Api`

## Auth

Die Collection ist mit Bearer-Token-Auth konfiguriert (`{{token}}`). Vor dem Testen einen Login-Request ausführen – der Post-Response-Script setzt `token` automatisch ins aktive Environment. Alle anderen Requests erben die Auth von der Collection.

## Variablen

Pfad- und Body-Werte werden als Bruno-Variablen gesetzt, z. B. `{{fatherId}}`, `{{childId}}`, `{{planId}}`, `{{positionId}}`.
