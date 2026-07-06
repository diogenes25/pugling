# 06 · Anleitung für die Sohn-App

← [Zurück zum Wiki-Index](../README.md)

Der **Sohn** lernt: Er legt keine Inhalte an, sondern sieht seinen aktuell spielbaren Plan, übt
Positionen, macht Tests und sammelt Münzen/Gems. Alle Aufrufe brauchen den Sohn-Bearer-Token. Der
Server bewertet jede Antwort selbst; das Frontend schickt nur Antworten und Interaktionen.

---

## 1. Anmelden

```http
POST /api/v1/auth/child
{ "childId": 1, "pin": "1111" }
→ { "token": "eyJ…", "role": "Sohn", … }
```

Token in allen weiteren Aufrufen als `Authorization: Bearer <token>` mitgeben.

---

## 2. Was ist heute zu tun?

```http
GET /api/v1/study-plans
GET /api/v1/study-plans/{planId}/positions
GET /api/v1/study-plans/{planId}/overview
```

Der Sohn sieht nur eigene, aktive und heute laufende Pläne. `overview` liefert die Tagesmission über
alle Positionen des Plans:

```jsonc
{
  "planId": 2,
  "title": "Doku-Lehrplan",
  "startDate": "2026-07-06",
  "endDate": "2026-07-15",
  "active": true,
  "currentStreak": 1,
  "today": {
    "day": "2026-07-06",
    "dutyDone": false,
    "positions": [
      {
        "positionId": 2,
        "exerciseId": 13,
        "exerciseTitle": "Begrüßungen",
        "cadence": "Daily",
        "goalMet": false,
        "pointsAwarded": 0
      }
    ]
  }
}
```

**Tagesziel:** Alle fälligen Pflichtpositionen (`Daily` bzw. die passende `Weekly`-Periode) müssen ihre
Zielregel erfüllen. Freie Positionen (`cadence=None`) dürfen geübt werden, zählen aber nicht zur Pflicht.

---

## 3. Üben (Zeit sammeln + Karten wiederholen)

Der Sohn übt immer eine konkrete Position:

```http
POST /api/v1/study-plans/{planId}/positions/{positionId}/practice-sessions
{} → { id, planId, positionId, day, activeSeconds, reviewCount }

GET /api/v1/study-plans/{planId}/positions/{positionId}/practice-sessions/{sessionId}/cards
```

Eine `PracticeCard` enthält je nach Übungstyp und Stufe: `itemIndex`, `stage`, `type`, `prompt`,
optional `hint`, `answerLength`, `reveal`, `choices` und `audioUrl`. Getippte Stufen liefern keine
Lösung; Anzeige-/Selbsteinschätzungs-Stufen dürfen `reveal` enthalten.

### Eine Antwort abgeben (`review`)

```http
POST /api/v1/study-plans/{planId}/positions/{positionId}/practice-sessions/{sessionId}/review
```

```jsonc
// getippte Stufe
{ "itemIndex": 0, "givenAnswer": "hallo" }

// Anzeige-/Selbsteinschätzungs-Stufe
{ "itemIndex": 0, "wasKnown": true }
```

Antwort bei einer gewerteten Leitner-Wiederholung:

```jsonc
{
  "wasCorrect": true,
  "expected": "hallo",
  "awarded": 10,
  "box": 2,
  "dueOn": "2026-07-08",
  "combo": 1,
  "comboBonus": 0,
  "speedBonus": 0
}
```

Bei Nicht-Leitner-Positionen, nicht fälligen Karten, bereits heute gewerteten Karten oder nicht
gewerteten Selbsteinschätzungen unter `requireTypedTest` liefert `review` `204 No Content`: Es wird
protokolliert, aber keine Box/Punkte werden bewegt.

### Zeit zählen & beenden

```http
POST /api/v1/study-plans/{planId}/positions/{positionId}/practice-sessions/{sessionId}/heartbeat
{ "seconds": 60, "active": true }

POST /api/v1/study-plans/{planId}/positions/{positionId}/practice-sessions/{sessionId}/end
```

Nur aktive Sekunden zählen; pro Heartbeat sind maximal 120 s anrechenbar. Beim Beenden werden
Positionsziele und Missionen erneut ausgewertet.

---

## 4. Abschlusstest machen

```http
POST /api/v1/study-plans/{planId}/positions/{positionId}/tests
{} → attemptId + Aufgaben ohne Lösung

GET /api/v1/study-plans/{planId}/positions/{positionId}/tests/{attemptId}

POST /api/v1/study-plans/{planId}/positions/{positionId}/tests/{attemptId}/submit
{
  "answers": [
    { "itemIndex": 0, "givenAnswer": "hallo" },
    { "itemIndex": 1, "wasKnown": true }
  ]
}
```

Ohne `stage` nimmt der Server automatisch die Positions-/Fahrplan-Stufe des Tages. Nur der Vater darf
beim Start eine Stufe explizit vorgeben. Die Bestehensgrenze kommt aus `goalThreshold` der Position,
sonst aus dem Standard (80 %). Ein Test kann nur einmal submitted werden; ein zweiter Submit liefert
`test_already_submitted`.

---

## 5. Fortschritt, Report und Wallet

```http
GET /api/v1/study-plans/{planId}/overview/progress
GET /api/v1/study-plans/{planId}/positions/{positionId}/report
GET /api/v1/me/points
```

`overview/progress` zeigt den Verlauf über die Planlaufzeit. Der Positionsreport zeigt Mastery,
Wiederholungen, Testhistorie und Leitner-Zustand der Inhalts-Atoms dieser Position. `me/points` liefert
beide Währungen:

```jsonc
{ "childId": 1, "coins": 50, "gems": 300, "entries": [ … ] }
```

---

## 6. Missionen, Auszeichnungen, Skins und Angebote

```http
GET /api/v1/me/missions        → Tages-/Wochenziele mit Fortschritt
GET /api/v1/me/achievements    → Badges mit Fortschritt/Earned-Status
GET /api/v1/me/skins           → { gems, selected, owned }
POST /api/v1/me/skins/{skinId}/purchase
POST /api/v1/me/skins/{skinId}/equip

GET /api/v1/me/rewards         → { coins, available, redemptions }
POST /api/v1/me/rewards/{rewardId}/purchase

GET /api/v1/me/shop            → { coins, gems, available[], inventory[], purchases[] }
POST /api/v1/me/shop/listings/{listingId}/purchase
POST /api/v1/me/shop/inventory/{articleId}/activate   { "quantity": 30 }
GET /api/v1/me/shop/activations[?status=Pending]
```

Münzen kommen aus Lernleistung und kaufen reale Vater-Angebote (`/me/rewards`) **oder** Familien-Shop-Artikel
(`/me/shop`). Gems kommen aus Boni, Missionen und Auszeichnungen und kaufen kosmetische Skins — können aber
auch als Gem-Anteil eines Shop-Angebots anfallen. Details: [05 · Punkte & Bonus](05-punkte-und-bonus.md).
