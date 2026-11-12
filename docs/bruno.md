# Bruno-Export

Der Bruno-Export wird aus der laufenden OpenAPI-Spezifikation erzeugt. Damit bleiben Pfadwerte wie `{{adultId}}`, `{{childId}}`, `{{planId}}` und die Auth-Scripts nach jedem API-Import reproduzierbar.

## Erzeugen

Backend starten:

```powershell
cd backend/Pugling.Api
dotnet run
```

In einem zweiten Terminal aus dem Repo-Root:

```powershell
npm run bruno:generate
```

Die Collection landet unter `tools/bruno/Pugling.Api` und kann in Bruno als Collection-Ordner geöffnet werden.

Alternativ kann eine gespeicherte OpenAPI-Datei genutzt werden:

```powershell
node tools/bruno/generate-bruno.mjs --input .\openapi.json --output tools\bruno\Pugling.Api
```

## Was der Generator einsetzt

- Pfadparameter werden direkt als Bruno-Variablen in die URL geschrieben, z. B. `/api/v1/supervisor/adults/{{adultId}}`.
- Request-Bodies erhalten bekannte IDs und PINs als Variablen, z. B. `{{adultId}}`, `{{childId}}`, `{{adultPin}}`.
- Es gibt **ein** Token: `{{token}}`. Die Collection (`opencollection.yml`) trägt Bearer-Auth mit
  `{{token}}`, Ordner und Requests erben sie (`auth: inherit`).
- `POST /api/v1/auth/adult` bzw. `/api/v1/auth/child` setzen `token` per after-response-Script als
  **Runtime-Variable** (`bru.setVar`, nicht ins Environment – sonst setzt ein Collection-Reload es auf `""`
  zurück). Wer zuletzt eingeloggt hat, dessen Rolle gilt für alle folgenden Requests.
- Antwort-IDs (`adultId`, `childId`, …) werden, soweit erkennbar, per `bru.setEnvVar` ins aktive Environment übernommen.

Die lokale Umgebung wird als `tools/bruno/Pugling.Api/environments/local.yml` erzeugt (OpenCollection-Format). Die Seed-Werte sind aktuell `adultId=1`, `adultPin=0000`, `childId=1`, `childPin=1111`.
