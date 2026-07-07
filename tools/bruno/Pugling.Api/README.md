# Pugling API Bruno Collection

Generiert im OpenCollection-`.yml`-Format. Manuelle Änderungen gehen beim nächsten Export verloren.

## Aktualisieren

`npm run bruno:generate`

Standardquelle ist `http://localhost:5200/openapi/v1.json`. Alternativ eine gespeicherte OpenAPI-Datei:

`node tools/bruno/generate-bruno.mjs --input ./openapi.json --output tools/bruno/Pugling.Api`

## Auth

Die Collection (`opencollection.yml`) trägt Bearer-Auth mit `{{token}}`; Ordner und Requests erben sie
per `auth: inherit`. Die Login-Requests (`auth: none`) setzen `token` per after-response-Script
automatisch ins aktive Environment.

## Variablen & Beispiele

Pfad-/Query-Werte sind als `{{variable}}` vorbelegt (Environment `environments/local.yml`).
Jeder Request bringt die von `DocsCaptureTests` verifizierten `examples` (Request-Eingabe + Response) mit.
