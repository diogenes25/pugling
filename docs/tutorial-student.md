---
tags: [typ/tutorial, bereich/training, rolle/student, lerntechnik/vokabeln]
---

# Tutorial · Student — lernen, verdienen, einlösen

Dieses Tutorial führt die **Student**-Rolle der Pugling-API vollständig durch: anmelden,
Tagesmission lesen, **üben** (serverbewertet, Anti-Cheat), den **Abschlusstest** schreiben,
den eigenen **Fortschritt** ansehen und **Münzen im Familien-Shop einlösen**.

> **Rollen ↔ Produkt.** Pugling schneidet API und Code technisch nach drei Rollen — *Creator*
> (Inhalte), *Supervisor* (Steuerung), *Student* (Lernen). Die Produkt-Metapher bleibt aber
> **Vater/Sohn**: Im Produkt ist der **Student der Sohn** (Seed: *Sohn*, `childId 1`, PIN `1111`).
> Wer den Plan eingerichtet hat, ist der Supervisor/Vater. Mehr dazu:
> [rollen-doku.md](rollen-doku.md).

Alle Beispiele gehen von `http://localhost:5200` aus; geschützte Aufrufe brauchen den JWT im Header
`Authorization: Bearer <token>`. Die Swagger-UI liegt unter `/swagger`. Die Antwort-Bodies unten sind
gegen die laufende API verifiziert — kürzere Vollfassungen stehen in
[api-examples/me.md](api-examples/me.md) und [api-examples/vocabulary.md](api-examples/vocabulary.md).

---

## Voraussetzung: ein spielbarer Plan

Der Student **legt keine Inhalte an** und erstellt keine Pläne — er *spielt* nur, was für ihn
bereitsteht. Damit überhaupt etwas zu tun ist, braucht es einen **spielbaren Plan**: aktiv, heute in
Laufzeit (Start ≤ heute ≤ Ende) und mit **mindestens einer Position**. Genau **ein** solcher Plan pro
Kind ist gleichzeitig spielbar (Anti-Cheat-Sperre).

Der Seed bringt einen **Katalog** mit (Fach → Kapitel → Übung `exerciseId 1`), aber **keinen fertigen
Plan**. Diesen legt zuerst ein **Supervisor (Vater)** an:

```http
POST /api/v1/supervisor/study-plans
{ "childId": 1, "title": "Englisch Woche 1", "durationDays": 10 }
→ { "id": 1, "positionCount": 0, ... }

POST /api/v1/supervisor/study-plans/1/positions
{ "exerciseId": 1 }
→ { "id": 1, "exerciseTitle": "Begrüßungen", ... }
```

Wie der Supervisor Plan, Positionen, Ziele und Punkte einrichtet, steht in
[tutorial-supervisor.md](tutorial-supervisor.md). Ab hier sitzt du auf dem **Student-Stuhl** und
gehst davon aus, dass `planId 1` mit `positionId 1` (Übung „Begrüßungen") bereitsteht.

---

## 1. Anmelden

```http
POST /api/v1/auth/child
{ "childId": 1, "pin": "1111" }
→ { "token": "eyJ…", "role": "Student", … }
```

Den `token` in allen weiteren Aufrufen als `Authorization: Bearer <token>` mitgeben. Wer hinter dem
Token steckt, verrät `auth/me`:

```http
GET /api/v1/auth/me
→ {
  "accountId": 3,
  "role": "Student",
  "roles": ["Student"],
  "fatherId": null,
  "childId": 1,
  "name": "Sohn"
}
```

`fatherId` ist `null` — ein reiner Student-Token trägt keine Supervisor-Identität. `childId` ist die
eigene Kind-ID; sämtliche `me/…`-Routen und die eigenen Pläne hängen daran. Ein Student sieht **nur
seine eigenen** Pläne, Punkte und Käufe; fremde Ressourcen liefern `403 forbidden`.

---

## 2. Tagesmission lesen

Der Einstieg in den Tag ist die **Übersicht** eines Plans. Sie fasst alle Positionen zur
Tagesmission zusammen:

```http
GET /api/v1/student/study-plans/1/overview
```

Antwort (Auszug): Der Block `today` trägt die offenen Pflichten des Tages, jede Position ihren
Renderer und ihren Prüfmodus:

```jsonc
{
  "planId": 1,
  "title": "Englisch Woche 1",
  "today": {
    "day": "2026-07-09",
    "dutyDone": false,
    "outstanding": ["Begrüßungen (Tagesziel) offen"],
    "positions": [
      {
        "positionId": 1,
        "exerciseId": 1,
        "exerciseTitle": "Begrüßungen",
        "renderer": "flashcards",
        "checkMode": "…",
        "cadence": "Daily",
        "goalMet": false
      }
    ]
  }
}
```

Lies das so:

- **`today.outstanding`** ist die konkrete To-do-Liste des Tages — hier `"Begrüßungen (Tagesziel)
  offen"`. Ist sie leer und `dutyDone: true`, ist die Tagesmission geschafft.
- **`cadence`** ist der Rhythmus einer Position: `Daily` (jeden Tag Pflicht), `Weekly` (x-mal pro
  Woche) oder `None` (frei — darf geübt werden, zählt aber nicht zur Pflicht).
- **`renderer`** / **`checkMode`** sagen dem Frontend, wie die Position gespielt und geprüft wird.
  Voll ausgebaut ist heute die **Vokabelübung** (`renderer: "flashcards"`).

Der Tagesverlauf über die gesamte Planlaufzeit (filter-/sortier-/paginierbar) liegt unter
`GET /api/v1/student/study-plans/1/overview/progress`.

---

## 3. Üben — die Leitner-Runde (serverbewertet)

Geübt wird **immer eine konkrete Position**. Der Ablauf ist: Sitzung starten → Karten holen → Antwort
für Antwort abgeben → Zeit melden → beenden. Wichtig fürs Verständnis: **Der Server bewertet jede
Antwort selbst.** Das Frontend entscheidet nie über richtig/falsch — es schickt nur die Antwort und
zeigt die Server-Antwort an.

### 3.1 Sitzung starten

```http
POST /api/v1/student/study-plans/1/positions/1/practice-sessions
{}
→ {
  "id": 1,
  "planId": 1,
  "positionId": 1,
  "day": "2026-07-09",
  "mode": "Lern",
  "cursor": 0,
  "total": 3
}
```

Der Modus ist hier **`Lern`** (Üben/Leitner). Mit dem Start friert der Server die Kartenreihenfolge
ein (`cursor`/`total`).

### 3.2 Karten holen

```http
GET /api/v1/student/study-plans/1/positions/1/practice-sessions/1/cards
→ [
  { "itemIndex": 0, "stage": 2, "type": "Vocabulary", "prompt": "hello",   "reveal": "hallo" },
  { "itemIndex": 1, "prompt": "goodbye", "reveal": "auf Wiedersehen" },
  { "itemIndex": 2, "prompt": "please",  "reveal": "bitte" }
]
```

Jede Karte hat einen stabilen **`itemIndex`** (Listenposition des Inhalts-Atoms), die **`stage`**
(Leitner-/Abfragestufe) und die Aufgabe **`prompt`**. Weil hier eine **Anzeige-/Selbsteinschätzungs-
Stufe** (Stufe 2) läuft, liefert der Server die Lösung **`reveal`** gleich mit — im **Lern-Modus wird
die Antwort aufgedeckt**, damit man lernen kann. (Getippte Stufen liefern kein `reveal`; je nach Typ
gibt es zusätzlich `hint`, `answerLength`, `choices`, `audioUrl`.)

### 3.3 Eine Antwort abgeben (`review`)

```http
POST /api/v1/student/study-plans/1/positions/1/practice-sessions/1/review
{ "itemIndex": 0, "givenAnswer": "hallo" }
```

Der Server prüft und antwortet mit dem vollständigen Ergebnis der Leitner-Wiederholung:

```jsonc
{
  "wasCorrect": true,
  "expected": "hallo",
  "awarded": 10,
  "box": 2,
  "dueOn": "2026-07-11",
  "combo": 1,
  "comboBonus": 0,
  "speedBonus": 0,
  "next": { "itemIndex": 1, "prompt": "goodbye", "reveal": "auf Wiedersehen" },
  "done": false
}
```

- `wasCorrect` — Server-Urteil. `expected` — die erwartete Lösung.
- `awarded` — gutgeschriebene Münzen für diese Antwort (Basis × Zeitfenster).
- `box` / `dueOn` — neuer Leitner-Kasten der Karte und nächste Fälligkeit.
- `combo` / `comboBonus` / `speedBonus` — laufende Serie und mögliche Boni (fließen als Gems).
- `next` — die nächste Karte (oder `done: true`, wenn die Runde durch ist).

> **Die Antwort ist die Zielseite.** Der `prompt` ist `"hello"`, gefragt ist die **Übersetzung**
> `"hallo"`. Wer stattdessen den Prompt zurückgibt (`givenAnswer: "hello"`), bekommt
> `"wasCorrect": false` mit `"expected": "hallo"`. Also immer die **Ziel-/Rückseite** eintippen.

**Nicht jede Antwort bewegt Punkte.** Bei Nicht-Leitner-Positionen, nicht fälligen Karten, heute schon
gewerteten Karten oder nicht gewerteten Selbsteinschätzungen unter `requireTypedTest` protokolliert
`review` nur (`204 No Content` bzw. `awarded: 0`) — keine Box, keine Münzen. Das Frontend darf hier
**keinen Punktgewinn vortäuschen**.

### 3.4 Zeit zählen (Heartbeat) und beenden

Während des Übens läuft ein Zeitzähler mit; er wird periodisch gemeldet:

```http
POST /api/v1/student/study-plans/1/positions/1/practice-sessions/1/heartbeat
{ "seconds": 45, "active": true }
→ { …, "activeSeconds": 45 }
```

Nur **aktive** Sekunden zählen, und der Server **clampt** sie (Anti-Cheat: pro Heartbeat sind maximal
120 s anrechenbar — man kann sich also keine Minutenmissionen „erschummeln"). Zum Schluss:

```http
POST /api/v1/student/study-plans/1/positions/1/practice-sessions/1/end
{}
→ { …, "ended": true }
```

Beim Beenden wertet der Server Positionsziele und Missionen erneut aus.

---

## 4. Abschlusstest (Klausur, strikt einzeln)

Der Test misst ernst, ob das Positionsziel erreicht ist. Fragen kommen **einzeln nacheinander, ohne
Zwischenfeedback**, und ein Versuch lässt sich **nur einmal** einreichen.

### 4.1 Versuch starten

```http
POST /api/v1/student/study-plans/1/positions/1/tests
{}
→ {
  "attemptId": 1,
  "planId": 1,
  "positionId": 1,
  "stage": 2,
  "totalItems": 1
}
```

> **Zwei wichtige Fallstricke:**
>
> 1. **Der Student wählt die Stufe nicht.** Nur der Vater darf beim Start eine `stage` vorgeben. Ohne
>    `stageSchedule` der Position fällt der Server auf **Stufe 2 = Selbsteinschätzung** zurück — und
>    zwar **auch dann, wenn die Position `requireTypedTest: true` hat**. Die getippte Prüfung erzwingt
>    also der Fahrplan des Vaters, nicht der Testaufruf.
> 2. **`totalItems` zählt nur die *eingeführten* Items** — also das, was du beim Üben schon gesehen
>    hast, nicht zwingend alle Inhalte der Übung. Wer wenig geübt hat, schreibt einen kurzen Test.

### 4.2 Frage für Frage holen

```http
GET /api/v1/student/study-plans/1/positions/1/tests/1/next
→ {
  "item": { "itemIndex": 0, "prompt": "hello", "stage": 2, "reveal": "hallo" },
  "done": false,
  "cursor": 0,
  "total": 1
}
```

Bei `done: true` ist der Test durch und wird ausgewertet.

### 4.3 Einreichen — Selbsteinschätzung vs. getippt

Weil hier **Stufe 2 (Selbsteinschätzung)** läuft, bewertet sich der Student selbst mit `wasKnown` —
**kein `givenAnswer`**:

```http
POST /api/v1/student/study-plans/1/positions/1/tests/1/submit
{ "answers": [ { "itemIndex": 0, "wasKnown": true } ] }
→ {
  "scorePercent": 100,
  "passed": true,
  "passPercent": 80
}
```

- `scorePercent` — erreichter Anteil, `passPercent` — Bestehensgrenze (aus `goalThreshold` der
  Position, sonst Standard 80 %), `passed` — bestanden ja/nein.

> **Modus nicht verwechseln.** Bei einer **getippten Stufe** läuft der Test anders: Frage holen mit
> `…/tests/1/next`, tippen mit `POST …/tests/1/answer { "itemIndex", "givenAnswer" }`, am Ende
> `…/submit`. In einer **Selbsteinschätzung** schickst du **`wasKnown`** und **niemals `givenAnswer`**
> — ein getippter Wert würde hier mit **0** gewertet.

Ein zweiter `submit` auf denselben Versuch liefert `test_already_submitted`. Ein Verbindungsabbruch
setzt denselben Versuch fort (kein Neustart).

---

## 5. Positionsreport — was sitzt, was nicht

Nach dem Üben/Testen zeigt der Report je Inhalts-Atom Mastery, Leitner-Zustand und Testhistorie:

```http
GET /api/v1/student/study-plans/1/positions/1/report
→ {
  "totalItems": 3,
  "introducedItems": 1,
  "masteredItems": 0,
  "items": [
    {
      "itemIndex": 0,
      "prompt": "hello",
      "answer": "hallo",
      "introduced": true,
      "box": 1,
      "masteryPercent": 0,
      "testsSeen": 2,
      "testsCorrect": 1
    }
    // … weitere Items
  ]
}
```

- `totalItems` — alle Inhalte der Position; `introducedItems` — davon schon eingeführt (deckt sich mit
  `totalItems` des Tests, siehe 4.1); `masteredItems` — als beherrscht gewertet.
- Je Item: aktueller `box`-Kasten, `masteryPercent` und die Testbilanz (`testsSeen` / `testsCorrect`).

Plan-übergreifend (nur Vokabeln) liegt der Wortschatz-Lernstand unter
`GET /api/v1/student/children/1/vocabulary-progress` (mit `?onlyWeak=true`, `/by-word`-Rollup für
„schlecht gelernte Wörter" usw.) — die kindzentrische Sicht über alle Pläne hinweg.

---

## 6. Wallet, Missionen, Auszeichnungen, Skins

Alle persönlichen Stände hängen an den `me/…`-Routen.

### 6.1 Kontostand (zwei Währungen)

```http
GET /api/v1/student/me/points
→ { "childId": 1, "coins": 80, "gems": 300 }
```

- **🪙 Münzen (`coins`)** verdient man durch **Lernleistung** (richtige Antworten, erfüllte Ziele) und
  gibt sie im **Familien-Shop** für echte Vater-Belohnungen aus.
- **💎 Gems (`gems`)** kommen aus **Boni** (Combo, Tempo, Missionen, Auszeichnungen) und kaufen
  kosmetische **Skins** (bzw. als Gem-Anteil eines Shop-Artikels).

> **Vorsicht, Münzen können ins Minus rutschen.** Hat eine Pflichtübung einen **Malus** (`penaltyCoins`,
> vom Vater gesetzt) und lässt der Student sein **Tages-/Wochenziel platzen**, zieht der Server die Münzen
> beim nächsten **Login** wieder ab — auch **unter 0**. Ein negativer Münzstand **sperrt den Shop-Kauf**
> (`insufficient_coins`), bis wieder genug verdient (oder vom Vater geschenkt) ist. Nur Lernen füllt das
> Konto zurück — genau das ist der Sinn.

Die einzelnen Buchungen liegen paginiert unter `GET /api/v1/student/me/points/entries`
(mit `X-Total-Count`); eine einzelne unter `…/entries/{entryId}`.

### 6.2 Missionen und Auszeichnungen

```http
GET /api/v1/student/me/missions
→ [ { "id": 1, "title": "Tagesziel: 10 richtige Antworten",
      "metric": "CorrectReviews", "period": "Daily", "target": 10,
      "current": 0, "completed": false, "rewardPoints": 15 }, … ]   // 5 Missionen

GET /api/v1/student/me/achievements
→ [ { "id": 4, "title": "Feuer-Streak", "icon": "🔥",
      "metric": "StreakDays", "threshold": 7, "current": 0,
      "earned": false, "rewardPoints": 70 }, … ]                     // 5 Auszeichnungen
```

**Missionen** sind Tages-/Wochenziele mit Fortschritt (`current`/`target`) und Belohnung
(`rewardPoints`, meist Gems). **Auszeichnungen** sind Badges mit Schwelle (`threshold`) und
`earned`-Status. Beide werden serverseitig **idempotent** belohnt (jedes nur einmal). Einzelsicht:
`…/missions/{id}` bzw. `…/achievements/{id}`.

### 6.3 Skins

```http
GET /api/v1/student/me/skins
→ { "gems": 300, "selected": "pug", "owned": ["pug"] }

POST /api/v1/student/me/skins/{skinId}/purchase   // kaufen (kostet Gems), danach ausgerüstet
POST /api/v1/student/me/skins/{skinId}/equip      // besessenen Skin ausrüsten
```

Preise und Besitz kommen vom Server. Typische Fehler: `skin_already_unlocked` (schon besessen),
`insufficient_gems` (zu wenig Gems), `skin_not_unlocked` (nicht besessenen Skin ausrüsten) — Details
in [api-examples/me.md](api-examples/me.md).

---

## 7. Familien-Shop — der einzige Münz-Ausgabeweg

Münzen gibt der Student **ausschließlich hier** aus. Der Ablauf ist zweistufig: **kaufen** (Münzen/Gems
weg, Inventar rauf) → **Einlösung beantragen** (der Vater genehmigt).

### 7.1 Angebote ansehen

```http
GET /api/v1/student/me/shop
→ {
  "coins": 80,
  "gems": 300,
  "available": [
    { "listingId": 1, "title": "Fernsehzeit", "coinPrice": 50, "gemPrice": 0,
      "unitsPerPurchase": 10, "stock": …, "affordable": true },
    …
  ],
  "inventory": [],
  "purchases": []
}
```

Jedes Angebot trägt ein **`affordable`**-Flag (reicht das Guthaben?) plus Preis, Einheiten-pro-Kauf und
Bestand.

### 7.2 Kaufen

```http
POST /api/v1/student/me/shop/listings/1/purchase
{}
```

Der Server bucht Münzen ab (hier **80 → 30**) und erhöht das aggregierte Inventar:

```jsonc
{ "coins": 30, "gems": 300,
  "inventory": [ { "articleId": 1, "title": "Fernsehzeit", "quantity": 10 } ] }
```

Der Kauf ist **ausstellergebunden**: Wer den Artikel genehmigen darf, wird als `SupervisorId`
festgehalten. Zu wenig Guthaben/Bestand blockiert der Server (Knopf im UI entsprechend deaktivieren,
nicht selbst „durchwinken").

### 7.3 Einlösung beantragen

Das Inventar ist noch keine eingelöste Belohnung — dafür stellt der Student eine
**Aktivierungsanfrage**:

```http
POST /api/v1/student/me/shop/inventory/1/activate
{ "quantity": 10 }
→ { "id": 1, "status": "Pending", … }
```

Die Menge wird **erst bei Genehmigung** durch den Vater abgezogen; bis dahin bleibt der Status
`Pending`. Eigene Anfragen verfolgt man über `GET /api/v1/student/me/shop/activations[?status=Pending]`.
Genehmigen/ablehnen tut der **Supervisor** — siehe [tutorial-supervisor.md](tutorial-supervisor.md).

---

## Was der Student nicht darf (Anti-Cheat & Rollen)

- **Keine Inhalte/Pläne ändern**, keine fremden Pläne sehen, keine fremden Tage nachtragen — das prüft
  die Ownership serverseitig (fremde Ressource → `403 forbidden`).
- **Stufe im Test** wählt der Vater, nicht der Student (siehe 4.1).
- **Übungszeit** ist geclampt (Heartbeat, siehe 3.4); nur **fällige** Karten geben Punkte, jede Karte
  höchstens **einmal pro Tag**.
- **Bewertung ist Server-Sache** — das UI täuscht nie Punkte, Lösungen oder gedeckte Käufe vor.

---

**Verwandt:** [tutorial-supervisor.md](tutorial-supervisor.md) (wer den Plan einrichtet) ·
[tutorial.md](tutorial.md) (Tutorial-Index) · [rollen-doku.md](rollen-doku.md) ·
[wiki/05 · Punkte & Bonus](../wiki/05-punkte-und-bonus.md) ·
[sohn-app-funktionsbeschreibung.md](sohn-app-funktionsbeschreibung.md) ·
[api-examples/me.md](api-examples/me.md) · [api-examples/vocabulary.md](api-examples/vocabulary.md)
