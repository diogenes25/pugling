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
Laufzeit (Start ≤ heute ≤ Ende) und mit **mindestens einer Position**. Je Fach ist **ein** solcher Plan pro
Kind gleichzeitig spielbar (Anti-Cheat-Sperre, B-539).

Der Seed bringt einen **Katalog** mit (Fach → Reihe/Unit → Übung `exerciseId 1`), aber **keinen fertigen
Plan**. Diesen legt zuerst ein **Supervisor (Vater)** an:

```http
POST /api/v1/supervisor/study-plans
{ "childId": 1, "title": "Englisch Woche 1", "subjectId": 1, "durationDays": 10 }
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
  "adultId": null,
  "childId": 1,
  "name": "Sohn"
}
```

`adultId` ist `null` — ein reiner Student-Token trägt keine Supervisor-Identität. `childId` ist die
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
- **`today.rewardsUnlocked`** (im Auszug nicht gezeigt) sagt, ob Tagesbox und Bonus-Training offen sind: es zählt nur
  die **Tagesziele** (hat der Plan keine, alle Ziele; B-535). Ein offenes Wochenziel sperrt die Belohnung nicht.
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
  { "itemIndex": 0, "stage": 1, "type": "Vocabulary", "prompt": "hello", "reveal": "hallo",
    "displayOnly": true, "introduction": true },
  { "itemIndex": 1, "stage": 1, "prompt": "goodbye", "reveal": "auf Wiedersehen", "introduction": true, … },
  { "itemIndex": 2, "stage": 1, "prompt": "please",  "reveal": "bitte", "introduction": true, … }
]
```

Jede Karte hat einen stabilen **`itemIndex`** (Listenposition des Inhalts-Atoms), die **`stage`**
(Abfragestufe **dieser Karte**) und die Aufgabe **`prompt`**. Auf einem frischen Plan sind alle Karten
noch nie gesehen: Sie kommen als **Kennenlern-Karten** (`introduction: true`, B-419) auf der freien
Anzeigestufe 1 (`displayOnly: true`) — egal, was der Fahrplan für den Tag sagt. Erst danach steigt die
Karte auf die Tagesstufe (bei Leitner-Positionen über die Box-Leiter, siehe 4.1). Auf Anzeige- und
Selbsteinschätzungs-Stufen liefert der Server die Lösung **`reveal`** gleich mit. (Getippte Stufen
liefern kein `reveal`; je nach Typ gibt es zusätzlich `hint`, `answerLength`, `choices`, `audioUrl`.)

### 3.3 Eine Antwort abgeben (`review`)

```http
POST /api/v1/student/study-plans/1/positions/1/practice-sessions/1/review
{ "itemIndex": 0, "wasKnown": true }
```

Nicht getippte Stufen (Anzeige, Selbsteinschätzung) wertet der Server **nur über `wasKnown`** — ein
`givenAnswer` auf so einer Karte ergibt `wasCorrect: false`. Für die Kennenlern-Karte antwortet er:

```jsonc
{
  "wasCorrect": true,
  "expected": "hallo",
  "awarded": 0,
  "box": 1,
  "dueOn": "2026-07-09",
  "combo": 0,
  "comboBonus": 0,
  "speedBonus": 0,
  "next": { "itemIndex": 1, "stage": 1, "prompt": "goodbye", "reveal": "auf Wiedersehen", "introduction": true, … },
  "done": false
}
```

- `wasCorrect` — Server-Urteil. `expected` — die erwartete Lösung.
- `awarded` — gutgeschriebene Münzen für diese Antwort (Basis × Zeitfenster). Kennenlernen zahlt nie
  (sonst wäre es der billigste Weg zu Münzen), ebenso wenig eine Karte, die die Box-Leiter auf
  Selbsteinschätzung abgesenkt hat.
- `box` / `dueOn` — Leitner-Kasten der Karte und nächste Fälligkeit (die Kennenlern-Karte bleibt in Box 1,
  eingeführt am Sitzungstag).
- `combo` / `comboBonus` / `speedBonus` — laufende Serie und mögliche Boni (fließen als Gems).
- `next` — die nächste Karte (oder `done: true`, wenn die Runde durch ist).

> **Getippt wird die Zielseite.** Auf einer **getippten Stufe** (z. B. 4) schickst du `givenAnswer`
> statt `wasKnown`. Der `prompt` ist `"hello"`, gefragt ist die **Übersetzung** `"hallo"`. Wer stattdessen
> den Prompt zurückgibt (`givenAnswer: "hello"`), bekommt `"wasCorrect": false` mit `"expected": "hallo"`.

**Nicht jede Antwort bewegt Punkte.** Bei Nicht-Leitner-Positionen, nicht fälligen Karten, heute schon
gewerteten Karten, Kennenlern-/Anzeige-Karten, von der Leiter abgesenkten Selbsteinschätzungen oder
Selbsteinschätzungen unter `requireTypedTest` protokolliert `review` nur und antwortet mit `awarded: 0`.
`204 No Content` kommt ausschließlich im **Info-Modus** (freies Üben ohne Rückmeldung). Das Frontend darf
hier **keinen Punktgewinn vortäuschen**.

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
→ { …, "endedAt": "2026-07-09T16:05:12Z", "activeSeconds": 45, … }
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
  "day": "2026-07-09",
  "stage": 2,
  "totalItems": 1
}
```

> **Zwei wichtige Fallstricke:**
>
> 1. **Der Student wählt die Stufe nicht.** Nur der Vater darf beim Start eine `stage` vorgeben. Sonst
>    gilt der Reihe nach: `stageSchedule` der Position → `stage` der Position → `defaultStage` der Übung
>    → Standard des Typs (Vokabeln: **Stufe 2 = Selbsteinschätzung**). `requireTypedTest: true` hebt die
>    Teststufe **nicht** an — es zählt nur einen getippten (gewerteten) Versuch fürs Ziel. Auf einer
>    Leitner-Position mit getippter Zielstufe fragt der Test jedes Item außerdem auf **seiner** Sprosse
>    der Box-Leiter (B-476): bis 7 Tage nach der Einführung ggf. noch als Auswahl, danach getippt.
> 2. **`totalItems` zählt nicht zwingend alle Inhalte.** Mit Tagesdosis (`newItemsPerDay`) ist es die
>    Portion des Tages; sonst die schon *eingeführten* Items — und wenn noch keins eingeführt ist, der
>    ganze Pool der Position. Wer wenig geübt hat, schreibt also einen kurzen Test.

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

## 5. Eigener Lernstand — was sitzt, was nicht

Der **Positionsreport** gehört nicht hierher: er liegt unter
`GET /api/v1/supervisor/study-plans/{planId}/positions/{positionId}/report` und ist **Vater-only**, weil er
je Zeile die `answer` des Inhalts trägt — auch für Karten, die noch nie gezeigt wurden. Ein Sohn-Token
bekommt dort `403`. Siehe [tutorial-supervisor.md](tutorial-supervisor.md).

Der eigene Lernstand kommt stattdessen aus den kindzentrischen Sichten — sie nennen **keine** Lösung, nur
Kasten und Beherrschung:

```http
GET /api/v1/student/children/1/vocabulary-progress
GET /api/v1/student/children/1/vocabulary-progress?onlyWeak=true
GET /api/v1/student/children/1/vocabulary-progress/by-word
```

Plan-übergreifend (nur Vokabeln), über alle Pläne hinweg; `/by-word` ist der Rollup für „schlecht gelernte
Wörter". Hierarchisch (Fach→Unit→Übung→Item, Routen `…/series-units/…`) liegt derselbe Stand unter
`GET /api/v1/student/children/1/learn/subjects`.

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

### 6.2 Die Tagestruhe öffnen

Sind die **Tagesziele** geschafft (`today.rewardsUnlocked`), verdient der Student am Ende der Runde bzw. des Tests
eine **Truhe** (B-527). Ihr Inhalt – zufällige Münzen und Gems, mit der Serie des Tages skaliert – ist dann schon
gezogen, aber **noch nicht gebucht** und **geheim**. Die Übersicht zeigt nur, dass sie da ist:

```jsonc
// GET /api/v1/student/study-plans/1/overview → dailyBox
{ "claimedToday": true, "openedToday": false, "waiting": 1,
  "coinsAwarded": null, "gemsAwarded": null, "streakAtClaim": 1 }
```

Öffnen darf nur der Student selbst:

```http
POST /api/v1/student/children/1/daily-box/open
→ { "coins": 20, "gems": 2, "streakAtClaim": 1, "day": "2026-07-09", "waitingAfter": 0 }
```

- Erst jetzt landen `DailyBoxCoins`/`DailyBoxGems` im Kontoauszug, und `dailyBox` zeigt `openedToday: true` samt
  Inhalt.
- **Eine Truhe verfällt nie.** Warten mehrere (`waiting`), öffnet der Aufruf die **älteste**. Es zahlt der Wert vom
  Tag des Verdienens – Truhen aufzusparen bringt nichts.
- Keine Truhe wartet (oder ein Doppeltipp war schneller) → `409 daily_box_none`; ein Supervisor bekommt `403`. Beim
  Doppeltipp verliert der zweite Aufruf auch dann, wenn noch eine Truhe wartet – geöffnet wird immer die älteste
  wartende Truhe, ein erneuter Aufruf öffnet also die nächste.

### 6.3 Missionen und Auszeichnungen

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

### 6.4 Skins

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
    { "id": 1, "shopArticleId": 1, "title": "Fernsehzeit", "coinPrice": 50, "gemPrice": 0,
      "unitsPerPurchase": 10, "currentStock": …, "affordable": true, … },
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
  "inventory": [ { "shopArticleId": 1, "title": "Fernsehzeit", "quantity": 10, … } ], … }
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
