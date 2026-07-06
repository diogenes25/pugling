# Bruno-Export

Der Bruno-Export wird aus der laufenden OpenAPI-Spezifikation erzeugt. Damit bleiben Pfadwerte wie `{{fatherId}}`, `{{childId}}`, `{{planId}}` und die Auth-Scripts nach jedem API-Import reproduzierbar.

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

- Pfadparameter werden direkt als Bruno-Variablen in die URL geschrieben, z. B. `/api/v1/fathers/{{fatherId}}`.
- Request-Bodies erhalten bekannte IDs und PINs als Variablen, z. B. `{{fatherId}}`, `{{childId}}`, `{{fatherPin}}`.
- `POST /api/v1/auth/father` speichert `fatherToken`, `fatherId` und `authToken` ins aktive Environment.
- `POST /api/v1/auth/child` speichert `childToken`, `childId` und `authToken` ins aktive Environment.
- Vater-Endpunkte senden `Authorization: Bearer {{fatherToken}}`.
- Sohn-Endpunkte unter `/api/v1/me` senden `Authorization: Bearer {{childToken}}`.
- Antwort-IDs werden, soweit erkennbar, ebenfalls persistent ins Environment übernommen.

Die lokale Umgebung wird als `tools/bruno/Pugling.Api/environments/local.bru` erzeugt. Die Seed-Werte sind aktuell `fatherId=1`, `fatherPin=0000`, `childId=1`, `childPin=1111`.
