---
tags: [typ/tutorial, bereich/training, bereich/auswertung, rolle/supervisor]
aliases: [Supervisor-Tutorial, Lernplan bauen, Vater-Tutorial]
---

# Tutorial · Supervisor — steuern, belohnen, kontrollieren

Dieses Tutorial führt die **Supervisor-Rolle** der Pugling-API von vorne bis hinten durch: Aus
Katalog-Inhalten werden **verbindliche Aufgaben** für ein Kind — Lehrpläne (Container) mit Positionen
(Ziel, Rhythmus, Punkte), plan-übergreifende Lernziele, der Familien-Shop, Missionen/Auszeichnungen und
Klassenarbeiten — und am Ende liest der Supervisor den Lernstand seines Kindes wieder aus.

> **Technische Rollen vs. Produkt-Metapher.** Pugling schneidet API und Code nach den technischen Rollen
> **Creator** (Inhalte), **Supervisor** (Steuerung) und **Student** (Lernen). Die Produkt-Sprache bleibt
> **Vater/Sohn**. Die Brücke ein für alle Mal: **Im Produkt ist der Vater zugleich Creator und Supervisor**
> (Seed-Konto: *Papa*, `fatherId 1`). Er baut also erst den Katalog (Creator-Seat,
> [tutorial-creator.md](tutorial-creator.md)) und steuert dann als Supervisor sein Kind — mit demselben Login.

**Basis für alle Beispiele:** `http://localhost:5200`, geschützte Aufrufe mit `Authorization: Bearer <token>`,
Swagger-UI unter [`/swagger`](http://localhost:5200/swagger). Die JSON-Bodies unten sind **verifiziert** —
so kommen sie wirklich von der laufenden API zurück (gekürzte Felder mit `…` markiert).

---

## Der Flow in vier Schritten

Der Supervisor-Loop ist derselbe wie im [Endpunkt-Datenfluss](endpunkt-beziehungen.md): der Vater pflegt
**einmal** globale Katalog-Übungen; ein **Lehrplan** gehört einem **Kind** und *referenziert* diese Übungen
über **Positionen** (er kopiert sie nicht). Beim Üben schreibt der Server den Fortschritt; der Supervisor
liest ihn aus.

```text
1. Katalog wählen        → welche Übung wird zur Aufgabe?      (Creator-Seat, hier nur referenziert)
2. Plan-Container        → Rahmen fürs Kind: Titel, Laufzeit    POST /supervisor/study-plans
3. Positionen            → Ziel, Rhythmus, Punkte, Leitner      POST …/study-plans/{id}/positions
4. Kontrolle             → Overview / Report / Lernstand        GET  /student/study-plans/{id}/overview …
```

Dazu kommen die **Steuer- und Belohnungshebel**, die alle am Kind hängen: **Lernziele** (plan-übergreifend),
der **Familien-Shop** (der einzige Münz-Ausgabeweg), **Missionen/Auszeichnungen**, **manuelle Punkte /
Verschenken** und **Klassenarbeiten**. Der einzige **„Stick"** (Konsequenz fürs *Nicht*-Lernen) ist der
**Münz-Malus** einer Pflichtposition (Schritt 3) — alle anderen Hebel sind Belohnung.

---

## 0. Anmelden

Der Vater meldet sich per PIN an und erhält ein JWT. Weil ein Vater zugleich Creator und Supervisor ist,
trägt das Token beide Rollen (plus den Alias `Vater`); die `fatherId` steckt als `fid`-Claim drin.

```http
POST /api/v1/auth/father
{ "fatherId": 1, "pin": "0000" }
→ { "token": "…", "role": "Supervisor", "fatherId": 1, … }
```

Alle folgenden `POST/PATCH/DELETE`-Aufrufe brauchen `Authorization: Bearer <token>`.

---

## 1. Kinder ansehen

Der Supervisor steuert seine eigenen Kinder (Eigentum erzwingt der `ChildOwnershipFilter`). Der Seed
liefert genau ein Kind:

```http
GET /api/v1/supervisor/children
→ [
  { "id": 1, "name": "Sohn", "birthYear": 2015, "grade": null,
    "schoolType": "None", "coins": 50, "gems": 300 }
]
```

`coins` (🪙) und `gems` (💎) sind die beiden Salden aus dem gemeinsamen Wallet des Kindes. Ein neues Kind
legst du mit `POST /api/v1/supervisor/children { "name": "…", "birthYear": 2015, "pin": "1111" }` an;
kindbezogene Ressourcen leben danach unter `api/v1/supervisor/children/{childId}/…`. Verifizierte Bodies:
[api-examples/children.md](api-examples/children.md).

---

## 2. Den Plan-Container anlegen

Ein `StudyPlan` ist ein **reiner Container**: Kind, Titel, Laufzeit, aktiv/inaktiv, optional ein Fach. Das
eigentliche Training hängt an den Positionen (Schritt 3).

```http
POST /api/v1/supervisor/study-plans
{ "childId": 1, "title": "Vokabel-Sprint", "durationDays": 10 }
→ {
  "id": 1,
  "childId": 1,
  "title": "Vokabel-Sprint",
  "subjectId": null,
  "startDate": "2026-07-09",
  "endDate": "2026-07-18",
  "active": true,
  "positionCount": 0,
  "isPlayable": true
}
```

**Was hier passiert:**

- `endDate = startDate + durationDays − 1` (10 Tage → 09.–18.07.). Ohne `startDate` beginnt der Plan heute
  (UTC — nahe Mitternacht lokal ggf. ein anderer Kalendertag).
- `isPlayable` = **`active` UND heute innerhalb der Laufzeit**. Nur ein spielbarer Plan taucht in der
  Sohn-Sicht auf.
- **Anti-Cheat:** Es ist **genau ein aktiver + laufender Plan je Kind** spielbar. Ein neuer aktiver Plan
  deaktiviert automatisch andere aktive Pläne desselben Kindes — der Sohn kann nicht zwischen leichten
  Plänen hin- und herwechseln. Deaktivierte Pläne bleiben zur Auswertung erhalten.

Optionale Felder: `subjectId` (Katalog-Fach zur Einordnung/Filterung), `description`, `startDate`.
Nachträglich ändern:

```http
PATCH /api/v1/supervisor/study-plans/1
{ "title": "Vokabel-Sprint (verlängert)", "active": true, "endDate": "2026-07-25" }
```

Verifizierte Plan-Bodies: [api-examples/study-plans.md](api-examples/study-plans.md).

---

## 3. Positionen anhängen — hier steckt die ganze Steuerung

Eine `PlanPosition` verweist auf **eine Katalog-Übung** (per `exerciseId`) und trägt ihr **eigenes** Ziel,
ihre Stufe, ihre Punkte und ihren Leitner-Zustand. Der Inhalt bleibt in der Übungs-Config; die Position ist
die Steuer-Schicht darüber.

Der Seed hat Übung `1` = **„Begrüßungen"** (`Vocabulary`). Ein reichhaltig konfigurierter Request:

```http
POST /api/v1/supervisor/study-plans/1/positions
{
  "exerciseId": 1,
  "cadence": "Daily",
  "useLeitner": true,
  "requireTypedTest": true,
  "goalThreshold": 80,
  "pointsGoalMet": 20,
  "comboThreshold": 5,
  "comboBonusPoints": 5,
  "speedThresholdSeconds": 8,
  "speedBonusPoints": 3
}
→ {
  "id": 1,
  "studyPlanId": 1,
  "exerciseId": 1,
  "exerciseTitle": "Begrüßungen",
  "exerciseType": "Vocabulary",
  "order": 0,
  "stage": null,
  "scope": "All",
  "cadence": "Daily",
  "orderStrategy": "WeakestFirst",
  "goalThreshold": 80,
  "requireTypedTest": true,
  "useLeitner": true,
  "maxBox": 5,
  "stageSchedule": null,
  "pointsGoalMet": 20,
  "penaltyCoins": 0,
  "newContentPoints": 10,
  "comboThreshold": 5,
  "comboBonusPoints": 5,
  "speedThresholdSeconds": 8,
  "speedBonusPoints": 3
}
```

`exerciseId` reicht technisch aus — alles andere sind **Overrides**. Leere Felder erben die Defaults der
Übung (`stage`/`itemCount` bleiben dann `null`, Bonuswerte kommen aus dem `suggestedBonus` der Exercise).

### Die wichtigsten Positions-Felder

| Feld | Bedeutung |
| --- | --- |
| `exerciseId` *(req)* | Katalog-Übung, deren Config gespielt wird. |
| `cadence` | `Daily`/`Weekly` macht die Position zur **Pflicht** (Tagesziel); `None` = freies Üben. |
| `goalThreshold` | Bei Tests die Prozent-Bestehensgrenze, bei Check-Aufgaben die Stück-/Schwellenzahl. |
| `useLeitner` | Karteikasten-Fälligkeit je Inhalts-Atom (`PositionItemProgress`). |
| `requireTypedTest` | Nur getippte/gewertete Stufen zählen als echte Leistung. Siehe **Fallstrick** unten. |
| `stage` / `stageSchedule` | Feste Spiel-/Teststufe bzw. Tag→Stufe-Fahrplan (übersteuert `stage`). |
| `scope` | `All` / `New` / `Old` — welche Inhalts-Teilmenge gespielt wird. |
| `orderStrategy` | Ausspielreihenfolge: `WeakestFirst` (Default), `Serial`, `Random`, `NewestWeighted`. |
| `pointsGoalMet` | 🪙 Münzen für ein **erfülltes Positionsziel** je Periode (idempotent). |
| `penaltyCoins` | 🪙 **Münz-Malus**, wenn die Pflicht-Periode **gerissen** wird (0 = aus). Der einzige „Stick". |
| `newContentPoints` | Basispunkte für **erstmals** geübten Inhalt. |
| `combo*` / `speed*` | 💎 Gem-Boni für Trefferfolgen bzw. schnelle Antworten. |

Vollständige Feldliste inkl. `maxBox`/`boxIntervalDays`: die frühere Referenz lebt jetzt in diesem Tutorial
und im [PlanPositionsController](../backend/Pugling.Api/Controllers/Supervisor/PlanPositionsController.cs).

### Der Münz-Malus (`penaltyCoins`) — Lernen erzwingen

Alle bisherigen Hebel belohnen. `penaltyCoins` ist die **Konsequenz fürs Nicht-Lernen**: Reißt eine
Pflichtposition (`cadence: Daily|Weekly`) ihre **abgeschlossene** Periode (Ziel nicht erreicht), zieht der
Server `penaltyCoins` 🪙 wieder ab (`PointKind.GoalPenalty`).

- **Schulden erlaubt** — der Münzsaldo darf negativ werden und **blockt dann den Shop-Kauf**, bis das Kind
  wieder verdient oder du Münzen **verschenkst** (Schritt 7). So koppelt der Malus reale Privilegien ans Lernen.
- **Automatisch & idempotent:** Es gibt keinen Scheduler — der Malus wird **faul** beim **Kind-Login** und
  beim **Shop-Kauf** nachgerechnet, je Periode höchstens einmal (Rückblick max. 14 Tage).
- **Fair:** Kein Malus für Tage/Wochen, in denen der Plan **inaktiv** oder außerhalb der Laufzeit war.
- Default `0` = reines Belohnen (bisheriges Verhalten). Setzen/ändern via `POST`/`PATCH` der Position, z. B.
  `{ "penaltyCoins": 50 }`. Hintergrund: [wiki/05 · Punkte & Bonus §2](../wiki/05-punkte-und-bonus.md).

### Stufen-Fahrplan (`stageSchedule`)

Statt einer festen `stage` kann die Schwierigkeit über die Laufzeit steigen — Tag→Stufe:

```json
{ "exerciseId": 1, "cadence": "Daily", "requireTypedTest": true,
  "stageSchedule": [ { "dayNumber": 1, "stage": 2 }, { "dayNumber": 5, "stage": 4 } ] }
```

Ab Tag 1 spielt der Sohn Stufe 2, ab Tag 5 Stufe 4. Der Server erzwingt die Stufe aus dem Fahrplan; nur der
**Vater** darf beim Teststart eine Stufe frei übersteuern.

> ⚠️ **Fallstrick — `requireTypedTest: true` ohne `stageSchedule`.** In dieser Kombination fällt die
> **Prüfung des Kindes auf Stufe 2 (Selbsteinschätzung) zurück** — sie wird **nicht** automatisch zum
> getippten Test. Wer eine **getippte** Klausur erzwingen will, muss einen `stageSchedule` mit **Stufe 4**
> setzen (z. B. `[{ "dayNumber": 1, "stage": 4 }]`). `requireTypedTest` allein steuert nur, dass reine
> Anzeige-/Selbsteinschätzungs-Reviews **nicht für Punkte/Leitner-Box** zählen — es hebt die Teststufe nicht an.

### Positionen ändern / löschen

```http
GET    /api/v1/supervisor/study-plans/1/positions
PATCH  /api/v1/supervisor/study-plans/1/positions/1
DELETE /api/v1/supervisor/study-plans/1/positions/1
```

Eine Position mit bereits vorhandenen Übungs-/Testdaten kann **nicht** gelöscht werden
(`409 position_has_data`) — gespeicherter Lernfortschritt geht nicht verloren.

---

## 4. Lernziele — plan-übergreifende Ergebnis-Ziele

Positionen messen **Aktivität** (heute geübt?). **Lernziele** messen den **Lernstand** (wie gut sitzt der
Stoff?) — und zwar **plan-übergreifend**: Das Ziel hängt am Kind + einem Katalog-Scope, nicht an einer
Position. Es überlebt das Abhängen einer Übung und wird beim Lesen **live** ausgewertet.

```http
POST /api/v1/supervisor/children/1/learn-goals
{ "subjectId": 1, "metric": "MasteredPercent", "targetValue": 80, "title": "80% Englisch sicher" }
→ {
  "id": 1,
  "childId": 1,
  "subjectId": 1,
  "chapterId": null,
  "exerciseId": null,
  "scope": "subject",
  "metric": "MasteredPercent",
  "targetValue": 80,
  "currentValue": 0,
  "progressPercent": 0,
  "status": "open",
  "title": "80% Englisch sicher"
}
```

**Feld-Feinheiten (leicht zu verwechseln):**

- **Scope** = `subjectId` (Pflicht) + optional `chapterId`/`exerciseId` — **nicht** `scopeType`/`scopeId`.
  Der `scope`-String in der Antwort (`subject`/`chapter`/`exercise`) leitet sich aus dem gesetzten Feld ab.
- Der Zielwert heißt **`targetValue`** — **nicht** `target` (das ist das Missions-Feld, Schritt 6).
- **Metrik-Enum:** `AvgMastery` | `Coverage` | `MasteredPercent` (jeweils „≥ Zielwert") | `MaxWeakItems`
  („≤ Zielwert"). Sie bilden direkt Felder des `MasteryRollup` aus der Auswertung ab.
- **Status** wird live berechnet: `open` / `achieved` / `overdue` (bei gesetztem `dueDate`).
- **Lesen** dürfen Vater **und** Kind (Motivation); **Schreiben** nur der Vater. Filter: `?subjectId=`, `?status=`.

```http
GET    /api/v1/supervisor/children/1/learn-goals?status=open
PATCH  /api/v1/supervisor/children/1/learn-goals/1
DELETE /api/v1/supervisor/children/1/learn-goals/1
```

Abgrenzung: Lernziele ≠ das plan-gebundene Pflichtziel der Position (`cadence`) ≠ die aktivitätsbasierten
Missionen. Drei getrennte Konzepte — siehe [endpunkt-beziehungen.md §4](endpunkt-beziehungen.md#4-lernziele-ergebnis-ziele-auf-der-auswertung).

---

## 5. Familien-Shop — der einzige Münz-Ausgabeweg

🪙 Münzen fürs Lernen fließen ausschließlich über den **Familien-Shop** in reale Belohnungen. Der Shop hat
zwei Ebenen: **Artikel** (der Katalog: *was* gibt es) und **Angebote/Listings** (Preis + Bestand: *zu
welchen Konditionen*).

```http
POST /api/v1/supervisor/shop/articles
{ "articleNumber": "BOOK-001", "title": "Comic-Heft",
  "description": "Ein Comic nach Wahl", "unitType": "Mal", "actionType": "Ausflug" }
→ { "id": 5, … }
```

```http
POST /api/v1/supervisor/shop/articles/5/listings
{ "title": "1 Comic", "coinPrice": 200, "gemPrice": 0,
  "unitsPerPurchase": 1, "currentStock": 3, "maxStock": 3 }
→ { "id": 7, "shopArticleId": 5, "active": true, "refillKind": "None", … }
```

**Was hier zusammenhängt:**

- `unitType`/`actionType` beschreiben die Einlöse-Art des Artikels; `refillKind` steuert automatisches
  Auffüllen des Bestands (`None` = kein Nachfüllen).
- Der Kauf durch den Sohn (`POST /api/v1/student/me/shop/listings/7/purchase`) bucht 🪙 `ShopCoins` (und ggf.
  💎 `ShopGems`) ab, senkt `currentStock` und erhöht das aggregierte Inventar des Kindes (`ChildInventory`).
- Käufe/Aktivierungen sind **ausstellergebunden**: Bei Multi-Supervisor (Vater/Mutter/Oma) merkt sich der
  Snapshot `supervisorId`, wer das Angebot ausgestellt hat — das Wallet ist gemeinsam, die Einlösung
  aussteller-gebunden.

Das frühere separate „Angebots"-System (`Reward`/`OfferService`) wurde entfernt; der Shop ist der einzige
Münz-Ausgabeweg. Konzept + Gem-Ökonomie (💎 → Skins): [wiki/05 · Punkte & Bonus](../wiki/05-punkte-und-bonus.md).
Verifizierte Shop-Bodies: [api-examples/shop.md](api-examples/shop.md).

### Aktivierung genehmigen (die Kehrseite der Sohn-Anfrage)

Kauft der Sohn Inventar und will es einlösen, stellt er eine **Aktivierungsanfrage**
(`POST /api/v1/student/me/shop/inventory/{articleId}/activate`). Der Vater entscheidet:

```http
POST /api/v1/supervisor/children/1/shop/activations/{requestId}/approve
POST /api/v1/supervisor/children/1/shop/activations/{requestId}/reject
```

---

## 6. Missionen & Auszeichnungen

Zusätzlich zum laufenden Punktesystem definierst du pro Kind **Missionen** (Tages-/Wochenziele) und —
analog — **Auszeichnungen** (Badges). Beide messen serverseitige Metriken und belohnen **idempotent**.

```http
POST /api/v1/supervisor/children/1/missions
{ "title": "Täglich üben", "metric": "CorrectReviews", "target": 10,
  "period": "Daily", "rewardPoints": 15 }
→ { "id": 5, "active": true, … }
```

- **Metrik-Enum:** `NewWords` | `CorrectReviews` | `TestsPassed` | `MinutesPracticed` | `DaysComplete` | `StreakDays`.
- **Zeitraum:** `Daily` | `Weekly` | `OneOff`.
- Das Zielfeld heißt hier **`target`** (nicht `targetValue` wie beim Lernziel).

Der Sohn sieht seine eigene Projektion — nicht die fremde Kinder-ID — unter `GET /api/v1/student/me/missions`
bzw. `…/me/achievements`; `current` speist sich aus denselben Review-/Testdaten wie Overview und Auswertung.
Vollständige Semantik: [wiki/05 · Punkte & Bonus §5](../wiki/05-punkte-und-bonus.md).

---

## 7. Kontrolle — den Lernstand auslesen

Die Auswertungs-Endpunkte liegen technisch unter `api/v1/student/…`, sind aber **dual lesbar**: Ein
Vater-Token darf sie für seine eigenen Kinder lesen (Ownership OR-verknüpft). Es gibt drei Blickwinkel.

### a) Tagesmission / Plan-Overview

```http
GET /api/v1/student/study-plans/1/overview
→ {
  "planId": 1,
  "title": "Vokabel-Sprint",
  "currentStreak": 0,
  "today": {
    "dutyDone": false,
    "goalsTotal": 1,
    "goalsMet": 0,
    "outstanding": ["Begrüßungen (Tagesziel) offen"],
    "positions": [
      {
        "positionId": 1,
        "exerciseId": 1,
        "exerciseTitle": "Begrüßungen",
        "renderer": "flashcards",
        "checkMode": "StudyPlanTest",
        …
      }
    ]
  }
}
```

`overview` verdichtet die Positionen zur Tagesmission: `cadence: Daily` wird zur Pflicht, `dutyDone`/`goalsMet`
zeigen den Tagesstand, `outstanding` nennt die offenen Punkte. Verlauf: `GET …/overview/progress`.

### b) Pro Position (Leitner-/Teststand im Plan-Kontext)

```http
GET /api/v1/student/study-plans/1/positions/1/report
```

Beantwortet „wie steht **diese eine Position** da?" — liest die positionsgebundene Spur `PositionItemProgress`.

### c) Kind-zentrisch, plan-übergreifend („schlecht gelernte Wörter")

```http
GET /api/v1/student/children/1/vocabulary-progress?onlyWeak=true
GET /api/v1/student/children/1/vocabulary-progress/by-word?onlyWeak=true
```

Beantwortet „welche **Wörter** sitzen bei diesem Kind schlecht — egal in welchem Plan?" — liest die
plan-übergreifende Spur `ItemProgress`/`ItemReviewEvent`. Genau hier setzt der **nächste** Plan an: schwache
Wörter finden, passende Übung suchen/anlegen, als neue Position anhängen. Die hierarchische Drill-down-Sicht
(`…/children/1/learn/subjects/…`) spiegelt zusätzlich den Katalog. Datenfluss und alle Filter:
[endpunkt-beziehungen.md §3](endpunkt-beziehungen.md#3-übung--auswertung-des-kindes).

### Manuelle Punkte & Verschenken (🪙 Münzen **oder** 💎 Gems)

Der Supervisor kann jederzeit direkt ins Ledger buchen — für Extra-Motivation, Korrekturen oder als
**Geschenk**. `amount` positiv = gutschreiben/verschenken, negativ = abziehen; über **`currency`** wählst
du die Währung (Default `Coins`):

```http
POST /api/v1/supervisor/children/1/points
{ "amount": 30, "reason": "Extra fürs Dranbleiben" }                       # Münzen (Default)
→ { "id": 3, "childId": 1, "amount": 30, "kind": "Manual", "reason": "Extra fürs Dranbleiben" }

POST /api/v1/supervisor/children/1/points
{ "amount": 20, "reason": "Belohnung fürs Zimmeraufräumen", "currency": "Gems" }   # Gems verschenken
→ { "id": 4, "childId": 1, "amount": 20, "kind": "ManualGems", … }
```

**Gems verschenken** ging vorher nicht (Münzen konnte der Vater immer manuell buchen). Zweck: Belohnung
**außerhalb der App** *und* das **Druckventil gegen Malus-Schulden** — wird der Münzsaldo durch den Malus
(Schritt 3) zu negativ und erstickt die Motivation, gleicht der Vater ihn mit einem Münz-Geschenk aus. Im
Frontend sitzt das als „Verschenken"-Dialog unter `/vater/konto`.

Nur der **Vater** darf beim Start von Practice/Test einen anderen `day` als heute setzen (fremde Tage
nachtragen) — der Sohn ist serverseitig auf heute geklemmt.

---

## 8. Klassenarbeiten

Über die Auswertung hinaus plant und benotet der Supervisor **Klassenarbeiten**: Übungen taggen, eine Arbeit
terminieren, gezielt darauf üben und sie am Ende benoten. Route: `api/v1/supervisor/class-tests` (die
internen Typnamen heißen weiterhin `Klassenarbeit`).

```http
POST /api/v1/supervisor/class-tests
{ "childId": 1, "title": "Englisch-Vokabeltest", "date": "2026-07-18", … }
```

Tagging-Konzept, gezieltes Üben/Wiederholen und Benoten: [docs/klassenarbeiten-tagging.md](klassenarbeiten-tagging.md).
Verifizierte Bodies: [api-examples/class-tests.md](api-examples/class-tests.md).

---

## Copy-&-paste-Spickzettel

Kompletter Supervisor-Loop für Kind 1 (Vater eingeloggt, Katalog-Übung `1` = „Begrüßungen" vorhanden):

```http
# 0) Login
POST /api/v1/auth/father          { "fatherId": 1, "pin": "0000" }

# 1) Kind prüfen
GET  /api/v1/supervisor/children

# 2) Plan-Container
POST /api/v1/supervisor/study-plans
{ "childId": 1, "title": "Vokabel-Sprint", "durationDays": 10 }
→ { "id": 1, "positionCount": 0, "isPlayable": true, … }

# 3) Position (getippte Klausur → stageSchedule mit Stufe 4!; penaltyCoins = Münz-Malus bei gerissener Pflicht)
POST /api/v1/supervisor/study-plans/1/positions
{ "exerciseId": 1, "cadence": "Daily", "useLeitner": true, "requireTypedTest": true,
  "goalThreshold": 80, "pointsGoalMet": 20, "penaltyCoins": 50,
  "stageSchedule": [ { "dayNumber": 1, "stage": 2 }, { "dayNumber": 5, "stage": 4 } ],
  "comboThreshold": 5, "comboBonusPoints": 5,
  "speedThresholdSeconds": 8, "speedBonusPoints": 3 }
→ { "id": 1, "exerciseTitle": "Begrüßungen", … }

# 4) Plan-übergreifendes Lernziel
POST /api/v1/supervisor/children/1/learn-goals
{ "subjectId": 1, "metric": "MasteredPercent", "targetValue": 80, "title": "80% Englisch sicher" }

# 5) Familien-Shop (Artikel + Angebot)
POST /api/v1/supervisor/shop/articles
{ "articleNumber": "BOOK-001", "title": "Comic-Heft", "unitType": "Mal", "actionType": "Ausflug" }
POST /api/v1/supervisor/shop/articles/5/listings
{ "title": "1 Comic", "coinPrice": 200, "gemPrice": 0, "unitsPerPurchase": 1, "currentStock": 3, "maxStock": 3 }

# 6) Mission
POST /api/v1/supervisor/children/1/missions
{ "title": "Täglich üben", "metric": "CorrectReviews", "target": 10, "period": "Daily", "rewardPoints": 15 }

# 7) Kontrolle (Vater-Token liest die student/-Routen)
GET  /api/v1/student/study-plans/1/overview
GET  /api/v1/student/study-plans/1/positions/1/report
GET  /api/v1/student/children/1/vocabulary-progress?onlyWeak=true

# 8) Manuelle Punkte / Verschenken (Münzen bzw. Gems)
POST /api/v1/supervisor/children/1/points
{ "amount": 30, "reason": "Extra fürs Dranbleiben" }
POST /api/v1/supervisor/children/1/points
{ "amount": 20, "reason": "Malus-Ausgleich", "currency": "Gems" }
```

Danach kann der Sohn loslegen — weiter in [tutorial-student.md](tutorial-student.md).

---

**Verwandt:** [tutorial-creator.md](tutorial-creator.md) (vorher: Katalog bauen) ·
[tutorial-student.md](tutorial-student.md) (danach: das Kind spielt) · [tutorial.md](tutorial.md) (Index) ·
[endpunkt-beziehungen.md](endpunkt-beziehungen.md) · [wiki/05 · Punkte & Bonus](../wiki/05-punkte-und-bonus.md) ·
[klassenarbeiten-tagging.md](klassenarbeiten-tagging.md) · [rollen-doku.md](rollen-doku.md) ·
[api-examples/study-plans.md](api-examples/study-plans.md) · [api-examples/children.md](api-examples/children.md) ·
[api-examples/shop.md](api-examples/shop.md) · [api-examples/class-tests.md](api-examples/class-tests.md)
