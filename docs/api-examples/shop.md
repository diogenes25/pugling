# API-Beispiele – shop

_Automatisch erzeugt von `DocsCaptureTests` (Integrationstest). Jedes Beispiel ist verifiziert: Status und – bei Fehlern – der maschinenlesbare `code` wurden im Testlauf geprüft. Nicht von Hand bearbeiten._

## Artikel anlegen
`POST /api/v1/supervisor/shop/articles`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "articleNumber": "TV-900",
  "title": "Fernsehzeit",
  "description": "Bildschirmzeit in Minuten",
  "unitType": "Minute",
  "actionType": "TV"
}
```

Response — `HTTP 201`:
```json
{
  "id": 5,
  "articleNumber": "TV-900",
  "title": "Fernsehzeit",
  "description": "Bildschirmzeit in Minuten",
  "unitType": "Minute",
  "actionType": "TV",
  "createdAt": "<timestamp>"
}
```

### Artikel mit doppelter Nummer anlegen — Fehlerfall
`POST /api/v1/supervisor/shop/articles`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "articleNumber": "TV-900",
  "title": "Duplikat",
  "unitType": "Minute",
  "actionType": "TV"
}
```

Response — `HTTP 409`:
```json
{
  "type": "https://pugling.app/errors/duplicate_key",
  "title": "Key already exists.",
  "status": 409,
  "detail": "Article number already exists in this family shop.",
  "code": "duplicate_key",
  "traceId": "<trace-id>"
}
```

## Artikel auflisten
`GET /api/v1/supervisor/shop/articles`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 200`:
```json
[
  {
    "id": 4,
    "articleNumber": "EVENT-001",
    "title": "Kino-Ausflug",
    "description": "Gemeinsam ins Kino \u2013 der Sohn sucht den Film aus.",
    "unitType": "Mal",
    "actionType": "Ausflug",
    "createdAt": "2026-07-08T15:05:45.1057166"
  },
  {
    "id": 2,
    "articleNumber": "GAME-001",
    "title": "Spielzeit",
    "description": "Konsolen- oder PC-Spielzeit; w\u00F6chentliches Budgetmodell.",
    "unitType": "Minute",
    "actionType": "Zocken",
    "createdAt": "2026-07-08T15:05:45.1054648"
  },
  {
    "id": 3,
    "articleNumber": "SWEET-001",
    "title": "S\u00FC\u00DFigkeiten",
    "description": "Kleine Nascherei als Lernanreiz \u2013 z. B. Gummib\u00E4ren oder Schokolade.",
    "unitType": "Gramm",
    "actionType": "Suessigkeit",
    "createdAt": "2026-07-08T15:05:45.1057143"
  },
  {
    "id": 1,
    "articleNumber": "TV-001",
    "title": "Fernsehzeit",
    "description": "Bildschirmzeit nach dem Lernen \u2013 t\u00E4glich abrufbar.",
    "unitType": "Minute",
    "actionType": "TV",
    "createdAt": "2026-07-08T15:05:45.1036798"
  },
  {
    "id": 5,
    "articleNumber": "TV-900",
    "title": "Fernsehzeit",
    "description": "Bildschirmzeit in Minuten",
    "unitType": "Minute",
    "actionType": "TV",
    "createdAt": "2026-07-08T15:06:04.8952651"
  }
]
```

## Artikel auflisten (Suche)
`GET /api/v1/supervisor/shop/articles?search=Fernseh`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 200`:
```json
[
  {
    "id": 1,
    "articleNumber": "TV-001",
    "title": "Fernsehzeit",
    "description": "Bildschirmzeit nach dem Lernen \u2013 t\u00E4glich abrufbar.",
    "unitType": "Minute",
    "actionType": "TV",
    "createdAt": "2026-07-08T15:05:45.1036798"
  },
  {
    "id": 5,
    "articleNumber": "TV-900",
    "title": "Fernsehzeit",
    "description": "Bildschirmzeit in Minuten",
    "unitType": "Minute",
    "actionType": "TV",
    "createdAt": "2026-07-08T15:06:04.8952651"
  }
]
```

## Artikel ändern
`PATCH /api/v1/supervisor/shop/articles/5`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "title": "Fernsehzeit (30 Min)",
  "description": "30 Minuten freie Bildschirmzeit"
}
```

Response — `HTTP 200`:
```json
{
  "id": 5,
  "articleNumber": "TV-900",
  "title": "Fernsehzeit (30 Min)",
  "description": "30 Minuten freie Bildschirmzeit",
  "unitType": "Minute",
  "actionType": "TV",
  "createdAt": "2026-07-08T15:06:04.8952651"
}
```

## Angebot anlegen
`POST /api/v1/supervisor/shop/articles/5/listings`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "title": "30 Min Fernsehen",
  "description": "Einmalige Halbstunde",
  "coinPrice": 120,
  "gemPrice": 0,
  "unitsPerPurchase": 30,
  "currentStock": 5,
  "maxStock": 5
}
```

Response — `HTTP 201`:
```json
{
  "id": 7,
  "shopArticleId": 5,
  "articleNumber": "TV-900",
  "articleTitle": "Fernsehzeit (30 Min)",
  "title": "30 Min Fernsehen",
  "description": "Einmalige Halbstunde",
  "coinPrice": 120,
  "gemPrice": 0,
  "unitsPerPurchase": 30,
  "active": true,
  "currentStock": 5,
  "maxStock": 5,
  "refillKind": "None",
  "refillAtUtc": null,
  "refillDayOfWeek": null,
  "lastRefilledAtUtc": null,
  "createdAt": "<timestamp>"
}
```

### Angebot anlegen (ungültiger Preis) — Fehlerfall
`POST /api/v1/supervisor/shop/articles/5/listings`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "coinPrice": 0,
  "gemPrice": 0,
  "unitsPerPurchase": 30,
  "currentStock": 5,
  "maxStock": 5
}
```

Response — `HTTP 400`:
```json
{
  "type": "https://pugling.app/errors/validation_error",
  "title": "Invalid request.",
  "status": 400,
  "detail": "At least one price must be positive.",
  "code": "validation_error",
  "traceId": "<trace-id>"
}
```

## Angebote auflisten
`GET /api/v1/supervisor/shop/articles/5/listings`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 200`:
```json
[
  {
    "id": 7,
    "shopArticleId": 5,
    "articleNumber": "TV-900",
    "articleTitle": "Fernsehzeit (30 Min)",
    "title": "30 Min Fernsehen",
    "description": "Einmalige Halbstunde",
    "coinPrice": 120,
    "gemPrice": 0,
    "unitsPerPurchase": 30,
    "active": true,
    "currentStock": 5,
    "maxStock": 5,
    "refillKind": "None",
    "refillAtUtc": null,
    "refillDayOfWeek": null,
    "lastRefilledAtUtc": null,
    "createdAt": "2026-07-08T15:06:05.3147395"
  }
]
```

## Angebot ändern (Bestand auffüllen)
`PATCH /api/v1/supervisor/shop/articles/5/listings/7`

Rolle: **father** — `Authorization: Bearer <father-token>`

Request:
```json
{
  "currentStock": 5,
  "maxStock": 10
}
```

Response — `HTTP 200`:
```json
{
  "id": 7,
  "shopArticleId": 5,
  "articleNumber": "TV-900",
  "articleTitle": "Fernsehzeit (30 Min)",
  "title": "30 Min Fernsehen",
  "description": "Einmalige Halbstunde",
  "coinPrice": 120,
  "gemPrice": 0,
  "unitsPerPurchase": 30,
  "active": true,
  "currentStock": 5,
  "maxStock": 10,
  "refillKind": "None",
  "refillAtUtc": null,
  "refillDayOfWeek": null,
  "lastRefilledAtUtc": null,
  "createdAt": "2026-07-08T15:06:05.3147395"
}
```

## Shop-Sicht (Sohn)
`GET /api/v1/student/me/shop`

Rolle: **child** — `Authorization: Bearer <child-token>`

Response — `HTTP 200`:
```json
{
  "coins": 300,
  "gems": 0,
  "available": [
    {
      "id": 6,
      "shopArticleId": 4,
      "articleNumber": "EVENT-001",
      "articleTitle": "Kino-Ausflug",
      "unitType": "Mal",
      "actionType": "Ausflug",
      "title": "1 Kinoabend",
      "description": "",
      "coinPrice": 1500,
      "gemPrice": 0,
      "unitsPerPurchase": 1,
      "currentStock": 1,
      "affordable": false
    },
    {
      "id": 3,
      "shopArticleId": 2,
      "articleNumber": "GAME-001",
      "articleTitle": "Spielzeit",
      "unitType": "Minute",
      "actionType": "Zocken",
      "title": "30 Minuten Zocken",
      "description": "",
      "coinPrice": 200,
      "gemPrice": 0,
      "unitsPerPurchase": 30,
      "currentStock": 3,
      "affordable": true
    },
    {
      "id": 4,
      "shopArticleId": 2,
      "articleNumber": "GAME-001",
      "articleTitle": "Spielzeit",
      "unitType": "Minute",
      "actionType": "Zocken",
      "title": "60 Minuten Zocken",
      "description": "",
      "coinPrice": 350,
      "gemPrice": 0,
      "unitsPerPurchase": 60,
      "currentStock": 1,
      "affordable": false
    },
    {
      "id": 5,
      "shopArticleId": 3,
      "articleNumber": "SWEET-001",
      "articleTitle": "S\u00FC\u00DFigkeiten",
      "unitType": "Gramm",
      "actionType": "Suessigkeit",
      "title": "50 g Naschpaket",
      "description": "",
      "coinPrice": 300,
      "gemPrice"
… (gekürzt)
```

## Shop-Angebot kaufen
`POST /api/v1/student/me/shop/listings/7/purchase`

Rolle: **child** — `Authorization: Bearer <child-token>`

Request:
```json
{}
```

Response — `HTTP 200`:
```json
{
  "coins": 180,
  "gems": 0,
  "available": [
    {
      "id": 6,
      "shopArticleId": 4,
      "articleNumber": "EVENT-001",
      "articleTitle": "Kino-Ausflug",
      "unitType": "Mal",
      "actionType": "Ausflug",
      "title": "1 Kinoabend",
      "description": "",
      "coinPrice": 1500,
      "gemPrice": 0,
      "unitsPerPurchase": 1,
      "currentStock": 1,
      "affordable": false
    },
    {
      "id": 3,
      "shopArticleId": 2,
      "articleNumber": "GAME-001",
      "articleTitle": "Spielzeit",
      "unitType": "Minute",
      "actionType": "Zocken",
      "title": "30 Minuten Zocken",
      "description": "",
      "coinPrice": 200,
      "gemPrice": 0,
      "unitsPerPurchase": 30,
      "currentStock": 3,
      "affordable": false
    },
    {
      "id": 4,
      "shopArticleId": 2,
      "articleNumber": "GAME-001",
      "articleTitle": "Spielzeit",
      "unitType": "Minute",
      "actionType": "Zocken",
      "title": "60 Minuten Zocken",
      "description": "",
      "coinPrice": 350,
      "gemPrice": 0,
      "unitsPerPurchase": 60,
      "currentStock": 1,
      "affordable": false
    },
    {
      "id": 5,
      "shopArticleId": 3,
      "articleNumber": "SWEET-001",
      "articleTitle": "S\u00FC\u00DFigkeiten",
      "unitType": "Gramm",
      "actionType": "Suessigkeit",
      "title": "50 g Naschpaket",
      "description": "",
      "coinPrice": 300,
      "gemPrice
… (gekürzt)
```

### Shop-Angebot kaufen (ausverkauft) — Fehlerfall
`POST /api/v1/student/me/shop/listings/8/purchase`

Rolle: **child** — `Authorization: Bearer <child-token>`

Request:
```json
{}
```

Response — `HTTP 409`:
```json
{
  "type": "https://pugling.app/errors/shop_insufficient_stock",
  "title": "Shop listing is out of stock.",
  "status": 409,
  "detail": "This shop listing is out of stock.",
  "code": "shop_insufficient_stock",
  "traceId": "<trace-id>"
}
```

### Shop-Angebot kaufen (deaktiviert) — Fehlerfall
`POST /api/v1/student/me/shop/listings/8/purchase`

Rolle: **child** — `Authorization: Bearer <child-token>`

Request:
```json
{}
```

Response — `HTTP 400`:
```json
{
  "type": "https://pugling.app/errors/shop_listing_inactive",
  "title": "Shop listing no longer available.",
  "status": 400,
  "detail": "This shop listing is no longer available.",
  "code": "shop_listing_inactive",
  "traceId": "<trace-id>"
}
```

## Aktivierungsanfrage stellen
`POST /api/v1/student/me/shop/inventory/5/activate`

Rolle: **child** — `Authorization: Bearer <child-token>`

Request:
```json
{
  "quantity": 30
}
```

Response — `HTTP 200`:
```json
{
  "id": 1,
  "shopArticleId": 5,
  "articleTitle": "Fernsehzeit (30 Min)",
  "unitType": "Minute",
  "actionType": "TV",
  "requestedQuantity": 30,
  "status": "Pending",
  "requestedAt": "<timestamp>",
  "closedAt": null
}
```

### Aktivierungsanfrage (Inventar erschöpft) — Fehlerfall
`POST /api/v1/student/me/shop/inventory/5/activate`

Rolle: **child** — `Authorization: Bearer <child-token>`

Request:
```json
{
  "quantity": 999
}
```

Response — `HTTP 400`:
```json
{
  "type": "https://pugling.app/errors/insufficient_inventory",
  "title": "Not enough units in inventory.",
  "status": 400,
  "detail": "Not enough units in your inventory.",
  "code": "insufficient_inventory",
  "traceId": "<trace-id>"
}
```

## Eigenes Inventar (Sohn)
`GET /api/v1/student/me/shop/inventory`

Rolle: **child** — `Authorization: Bearer <child-token>`

Response — `HTTP 200`:
```json
[
  {
    "shopArticleId": 5,
    "articleNumber": "TV-900",
    "title": "Fernsehzeit (30 Min)",
    "unitType": "Minute",
    "actionType": "TV",
    "quantity": 30
  }
]
```

## Eigene Aktivierungen (Sohn)
`GET /api/v1/student/me/shop/activations`

Rolle: **child** — `Authorization: Bearer <child-token>`

Response — `HTTP 200`:
```json
[
  {
    "id": 2,
    "shopArticleId": 5,
    "articleTitle": "Fernsehzeit (30 Min)",
    "unitType": "Minute",
    "actionType": "TV",
    "requestedQuantity": 10,
    "status": "Pending",
    "requestedAt": "2026-07-08T15:06:06.1749324",
    "closedAt": null
  },
  {
    "id": 1,
    "shopArticleId": 5,
    "articleTitle": "Fernsehzeit (30 Min)",
    "unitType": "Minute",
    "actionType": "TV",
    "requestedQuantity": 30,
    "status": "Pending",
    "requestedAt": "2026-07-08T15:06:06.1349591",
    "closedAt": null
  }
]
```

## Kind-Inventar
`GET /api/v1/supervisor/children/6/shop/inventory`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 200`:
```json
[
  {
    "shopArticleId": 5,
    "articleNumber": "TV-900",
    "title": "Fernsehzeit (30 Min)",
    "unitType": "Minute",
    "actionType": "TV",
    "quantity": 30
  }
]
```

## Kind-Käufe
`GET /api/v1/supervisor/children/6/shop/purchases`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 200`:
```json
[
  {
    "id": 1,
    "childId": 6,
    "shopListingId": 7,
    "articleNumber": "TV-900",
    "title": "30 Min Fernsehen",
    "description": "Einmalige Halbstunde",
    "coinPrice": 120,
    "gemPrice": 0,
    "unitsPerPurchase": 30,
    "status": "Owned",
    "purchasedAt": "2026-07-08T15:06:05.9024381",
    "closedAt": null,
    "canCancel": true
  }
]
```

## Kind-Aktivierungen
`GET /api/v1/supervisor/children/6/shop/activations`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 200`:
```json
[
  {
    "id": 2,
    "childId": 6,
    "shopArticleId": 5,
    "articleTitle": "Fernsehzeit (30 Min)",
    "unitType": "Minute",
    "actionType": "TV",
    "requestedQuantity": 10,
    "status": "Pending",
    "requestedAt": "2026-07-08T15:06:06.1749324",
    "closedAt": null,
    "canApprove": true,
    "canReject": true
  },
  {
    "id": 1,
    "childId": 6,
    "shopArticleId": 5,
    "articleTitle": "Fernsehzeit (30 Min)",
    "unitType": "Minute",
    "actionType": "TV",
    "requestedQuantity": 30,
    "status": "Pending",
    "requestedAt": "2026-07-08T15:06:06.1349591",
    "closedAt": null,
    "canApprove": true,
    "canReject": true
  }
]
```

## Aktivierung genehmigen
`POST /api/v1/supervisor/children/6/shop/activations/1/approve`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 200`:
```json
{
  "id": 1,
  "childId": 6,
  "shopArticleId": 5,
  "articleTitle": "Fernsehzeit (30 Min)",
  "unitType": "Minute",
  "actionType": "TV",
  "requestedQuantity": 30,
  "status": "Approved",
  "requestedAt": "2026-07-08T15:06:06.1349591",
  "closedAt": "<timestamp>",
  "canApprove": false,
  "canReject": false
}
```

### Aktivierung erneut genehmigen — Fehlerfall
`POST /api/v1/supervisor/children/6/shop/activations/1/approve`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 409`:
```json
{
  "type": "https://pugling.app/errors/activation_not_pending",
  "title": "Activation request is not pending.",
  "status": 409,
  "detail": "This activation request is not pending.",
  "code": "activation_not_pending",
  "traceId": "<trace-id>"
}
```

### Aktivierung genehmigen (Inventar erschöpft) — Fehlerfall
`POST /api/v1/supervisor/children/6/shop/activations/2/approve`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 400`:
```json
{
  "type": "https://pugling.app/errors/insufficient_inventory",
  "title": "Not enough units in inventory.",
  "status": 400,
  "detail": "Not enough units left in the child\u0027s inventory to approve this request.",
  "code": "insufficient_inventory",
  "traceId": "<trace-id>"
}
```

## Aktivierung ablehnen
`POST /api/v1/supervisor/children/6/shop/activations/2/reject`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 200`:
```json
{
  "id": 2,
  "childId": 6,
  "shopArticleId": 5,
  "articleTitle": "Fernsehzeit (30 Min)",
  "unitType": "Minute",
  "actionType": "TV",
  "requestedQuantity": 10,
  "status": "Rejected",
  "requestedAt": "2026-07-08T15:06:06.1749324",
  "closedAt": "<timestamp>",
  "canApprove": false,
  "canReject": false
}
```

## Kauf stornieren (Vater)
`POST /api/v1/supervisor/children/6/shop/purchases/1/cancel`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 200`:
```json
{
  "id": 1,
  "childId": 6,
  "shopListingId": 7,
  "articleNumber": "TV-900",
  "title": "30 Min Fernsehen",
  "description": "Einmalige Halbstunde",
  "coinPrice": 120,
  "gemPrice": 0,
  "unitsPerPurchase": 30,
  "status": "Cancelled",
  "purchasedAt": "2026-07-08T15:06:05.9024381",
  "closedAt": "<timestamp>",
  "canCancel": false
}
```

## Angebot löschen
`DELETE /api/v1/supervisor/shop/articles/5/listings/7`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 204`:
```json
(kein Inhalt)
```

## Artikel löschen
`DELETE /api/v1/supervisor/shop/articles/5`

Rolle: **father** — `Authorization: Bearer <father-token>`

Response — `HTTP 204`:
```json
(kein Inhalt)
```

