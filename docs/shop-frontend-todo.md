---
tags: [typ/aufgabe, bereich/frontend, rolle/student, rolle/supervisor]
---

# TODO: Familien-Shop im Frontend nachziehen — ✅ ERLEDIGT (2026-07-09)

← [Zurück zum Wiki-Index](../README.md) · Verwandt: [05 · Punkte & Bonus](../wiki/05-punkte-und-bonus.md),
[Sohn-App-Funktionsbeschreibung](sohn-app-funktionsbeschreibung.md)

> **Status: umgesetzt.** Alles unten ist gebaut: Sohn-Shop [`frontend/src/sohn/SohnShop.tsx`](../frontend/src/sohn/SohnShop.tsx)
> (`/sohn/shop`, Nav 🛒 – Kaufen/Sachen/Anfragen), Vater-Verwaltung [`frontend/src/vater/VaterShop.tsx`](../frontend/src/vater/VaterShop.tsx)
> (`/vater/shop`, Nav 🛒 – Artikel/Angebote + Käufe/Aktivierungen je Kind), Shop-Methoden in
> [`api.ts`](../frontend/src/lib/api.ts) + Typen in [`types.ts`](../frontend/src/lib/types.ts) + geteilte
> Anzeige-Helfer in [`lib/shop.ts`](../frontend/src/lib/shop.ts). Mislabel 🪙→💎 in `VaterRewards.tsx` behoben.
> Der E2E ([`full-flow.spec.ts`](../frontend/e2e/full-flow.spec.ts)) fährt jetzt auch einen Shop-Kauf (grün).
> Das Dokument bleibt als Beschreibung der Anforderung stehen.

**Kontext:** Das redundante „Angebots"-System (`Reward`/`OfferService`/`…/rewards`) wurde entfernt – der
**Familien-Shop** ist der **einzige Münz-Ausgabeweg** (Backend + API vollständig vorhanden). Das
React-Frontend hatte dafür zunächst **keine Oberfläche**; diese Lücke ist mit dieser Session geschlossen.

## Ist-Zustand (nach dem Reward-Removal)

- **Sohn** [`frontend/src/sohn/SohnKonto.tsx`](../frontend/src/sohn/SohnKonto.tsx): nur noch Salden + Verlauf
  (kein Kauf). Der Erklärtext verweist auf einen **🛒 Shop**, den es in der Nav/Route **nicht gibt**
  ([`SohnApp.tsx`](../frontend/src/sohn/SohnApp.tsx): nur Basis/Weg/Konto/Skins).
- **Vater** [`frontend/src/vater/VaterKonto.tsx`](../frontend/src/vater/VaterKonto.tsx): nur noch Salden + Verlauf.
  Text verweist auf „Familien-Shop", den es in [`VaterApp.tsx`](../frontend/src/vater/VaterApp.tsx) nicht gibt.
- **API-Client** [`frontend/src/lib/api.ts`](../frontend/src/lib/api.ts): **keine** Shop-Methoden. Die Shop-DTOs
  (`ShopListingResponse` etc.) existieren serverseitig; im Frontend sind sie noch **nicht** typisiert.

## Zu bauen

### 1. Sohn-Shop-Screen (`/sohn/shop`, Nav 🛒)
Gegenstück zu `SohnSkins`. Drei Bereiche (siehe [Sohn-App-Funktionsbeschreibung §4.7](sohn-app-funktionsbeschreibung.md)):
- **Kaufen:** `GET /api/v1/student/me/shop` → `{ coins, gems, available[], inventory[], purchases[] }`;
  Kauf `POST /api/v1/student/me/shop/listings/{listingId}/purchase`.
- **Inventar + Einlösen beantragen:** `GET …/me/shop/inventory`, `POST …/me/shop/inventory/{articleId}/activate { quantity }`.
- **Anfragen-Status:** `GET …/me/shop/activations`.
- Nav-Eintrag in `SohnApp.tsx` ergänzen; Konto-Text (`SohnKonto.tsx`) zeigt dann korrekt auf den echten Tab.

### 2. Vater-Shop-Verwaltung (`/vater/shop`)
- **Artikel-Katalog:** `GET/POST /api/v1/supervisor/shop/articles`, `PATCH/DELETE …/articles/{id}`.
- **Angebote je Artikel:** `GET/POST …/articles/{id}/listings`, `PATCH/DELETE …/listings/{lid}` (Coin/Gem-Preis,
  `unitsPerPurchase`, Bestand, `ShopRefillKind`).
- **Käufe/Aktivierungen je Kind:** `GET …/children/{childId}/shop/purchases`, `POST …/purchases/{id}/cancel`;
  `GET …/children/{childId}/shop/activations`, `POST …/activations/{id}/approve|reject`.
- Nav-Eintrag in `VaterApp.tsx`; Konto-Text (`VaterKonto.tsx`) auf den echten Tab zeigen lassen.

### 3. API-Client + Typen ergänzen
Shop-Methoden in `api.ts` und Typen in `types.ts` (analog zu den entfernten Reward-Typen). Verifizierte
Request/Response-Beispiele: [`docs/api-examples/shop.md`](api-examples/shop.md).

## Aus dem Code-Review offen (in dieser Session mit erledigen)

- **Mislabel 🪙 vs 💎:** [`VaterRewards.tsx`](../frontend/src/vater/VaterRewards.tsx) (Z. 117/133/205/221) zeigt
  die Missions-/Auszeichnungs-Belohnung als **🪙 Münzen**, obwohl `PointKind.Mission`/`Achievement` laut
  [`PointKindCurrency`](../backend/Pugling.Api/Services/Shared/PointKindCurrency.cs) **💎 Gems** sind → auf 💎 korrigieren.
- Optional: `VaterRewards.tsx` in `VaterGamification`/`VaterBelohnungen` umbenennen (verwaltet nur noch
  Missionen + Auszeichnungen; „Rewards" ist ein Namensfossil).

## Verweise
- Ökonomie-/Endpunkt-Details: [wiki/05](../wiki/05-punkte-und-bonus.md), [wiki/06](../wiki/06-sohn-app.md),
  [wiki/07 API-Referenz](../wiki/07-api-referenz.md).
- E2E ([`frontend/e2e/full-flow.spec.ts`](../frontend/e2e/full-flow.spec.ts)) fährt den Shop bislang **nicht** –
  nach dem UI-Bau einen Shop-Kauf-Schritt ergänzen.
