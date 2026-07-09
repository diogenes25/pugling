---
tags: [typ/tutorial, bereich/training, rolle/creator, rolle/supervisor, rolle/student]
---

# Pugling – Tutorial (API)

> Teil des Wikis. Die ausführliche Fassung steht in
> [04 · Lernplan bauen (Vater)](../wiki/04-lernplan-bauen.md) und
> [06 · Sohn-App](../wiki/06-sohn-app.md). Einstieg: [README](../README.md).

Wie **Vater (Klaus)** das Tool einrichtet/steuert und wie **Sohn (Peter)** damit lernt. Alle Beispiele
gehen von `http://localhost:5200` aus. Geschützte Aufrufe brauchen den JWT im Header
`Authorization: Bearer <token>`. Swagger-UI liegt unter `/swagger`.

Seed-Konten: Vater `id=1` PIN `0000`, Sohn (Kind) `id=1` PIN `1111`.

---

## Teil 1 – Vater (Einrichtung & Kontrolle)

### 1.1 Anmelden

```http
POST /api/v1/auth/father
{ "fatherId": 1, "pin": "0000" }
→ { token, role:"Supervisor", ... }
```

### 1.2 Kind prüfen oder anlegen

```http
GET  /api/v1/supervisor/children
POST /api/v1/supervisor/children
{ "name":"Peter", "birthYear":2015, "pin":"1111" }
```

### 1.3 Katalog-Übung anlegen

Der Plan enthält später keine kopierten Items. Er verweist über Positionen auf Katalog-Übungen.
Bei Vokabelübungen werden inline übergebene `items` beim Speichern in stabile `ExerciseItem`-Zeilen
materialisiert. Die Config bleibt danach nur für Einstellungen wie Richtung und Sprachen zuständig.

```http
POST /api/v1/creator/subjects
{ "name":"Englisch" }

POST /api/v1/creator/subjects/{subjectId}/chapters
{ "name":"Unit 1", "orderIndex":1 }

POST /api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary
{ "title":"Unit 1 – Basics", "orderIndex":1, "rewardPoints":10,
  "config": { "direction":"front-to-back", "sourceLang":"en", "targetLang":"de",
    "items":[{"front":"dog","back":"Hund"},{"front":"cat","back":"Katze"}] },
  "defaultStage":2, "defaultItemCount":20, "defaultUseLeitner":true, "defaultRequireTypedTest":true }
→ { "id": 13, ... }
```

Die gespeicherten Vokabelpaare der Übung liest oder ergänzt du danach über die Item-Subressource:

```http
GET  /api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary/13/items
POST /api/v1/creator/subjects/{subjectId}/chapters/{chapterId}/vocabulary/13/items
{ "front":"bird", "back":"Vogel" }
```

Vorhandene Übungen findest du mit:

```http
GET /api/v1/creator/exercises?subjectId={subjectId}&grade=5&type=Vocabulary
```

### 1.4 Plan-Container und Position erstellen

```http
POST /api/v1/supervisor/study-plans
{ "childId":1, "title":"Vokabeltest in 10 Tagen", "subjectId":1, "durationDays":10 }
→ { "id": 42, "positionCount":0, ... }

POST /api/v1/supervisor/study-plans/42/positions
{ "exerciseId":13, "cadence":"Daily", "useLeitner":true, "requireTypedTest":true,
  "goalThreshold":80,
  "stageSchedule":[{"dayNumber":1,"stage":2},{"dayNumber":5,"stage":3},{"dayNumber":8,"stage":4}],
  "pointsGoalMet":20,
  "comboThreshold":5, "comboBonusPoints":5,
  "speedThresholdSeconds":8, "speedBonusPoints":3 }
→ { "id": 7, "exerciseTitle":"Unit 1 – Basics", ... }
```

### 1.5 Fortschritt & Kontrolle

```http
GET /api/v1/student/study-plans/42/overview
GET /api/v1/student/study-plans/42/overview/progress
GET /api/v1/student/study-plans/42/positions/7/report
GET /api/v1/student/children/1/vocabulary-progress?onlyWeak=true
GET /api/v1/student/children/1/vocabulary-progress/by-word?onlyWeak=true
GET /api/v1/supervisor/children/1/points
```

Manuelle Punktekorrektur:

```http
POST /api/v1/supervisor/children/1/points
{ "amount": 30, "reason":"Extra fürs Dranbleiben" }
```

Nur der Vater darf beim Start von Practice/Test einen anderen `day` als heute setzen.

---

## Teil 2 – Sohn (Lernen)

### 2.1 Anmelden

```http
POST /api/v1/auth/child
{ "childId": 1, "pin": "1111" }
→ token (Rolle "Student")
```

### 2.2 Tagesmission lesen

```http
GET /api/v1/supervisor/study-plans
GET /api/v1/supervisor/study-plans/42/positions
GET /api/v1/student/study-plans/42/overview
```

Der Sohn sieht nur eigene, aktive, heute laufende Pläne. `overview` zeigt, welche Positionen heute
Pflicht sind und ob der Tag schon erledigt ist.

### 2.3 Üben

```http
POST /api/v1/student/study-plans/42/positions/7/practice-sessions
{} → { "id": 1, "planId":42, "positionId":7, ... }

GET /api/v1/student/study-plans/42/positions/7/practice-sessions/1/cards

POST /api/v1/student/study-plans/42/positions/7/practice-sessions/1/review
{ "itemIndex":0, "givenAnswer":"Hund" }
→ { wasCorrect, expected, awarded, box, dueOn, combo, comboBonus, speedBonus }

POST /api/v1/student/study-plans/42/positions/7/practice-sessions/1/heartbeat
{ "seconds":60, "active":true }

POST /api/v1/student/study-plans/42/positions/7/practice-sessions/1/end
```

Der Server erzwingt die Stufe und bewertet die Antwort; das Frontend entscheidet nie selbst über
richtig/falsch. Bei nicht gewerteten Reviews kommt `204 No Content` zurück.

### 2.4 Abschlusstest

```http
POST /api/v1/student/study-plans/42/positions/7/tests
{} → attemptId + Aufgaben ohne Lösung

POST /api/v1/student/study-plans/42/positions/7/tests/{attemptId}/submit
{ "answers":[
  { "itemIndex":0, "givenAnswer":"Hund" },
  { "itemIndex":1, "givenAnswer":"Katze" }
] }
→ { scorePercent, passed, passPercent, items:[...] }
```

Die Bestehensgrenze kommt aus `goalThreshold` der Position, sonst aus dem Standard (80 %). Ein bereits
abgeschlossener Versuch kann nicht erneut submitted werden.

### 2.5 Belohnung und Grenzen

```http
GET /api/v1/student/me/points
GET /api/v1/student/me/missions
GET /api/v1/student/me/achievements
GET /api/v1/student/me/skins
GET /api/v1/student/me/shop
```

Der Sohn kann keine Pläne/Inhalte ändern, keine fremden Pläne sehen und keine fremden Tage nachtragen.
Bei `requireTypedTest=true` zählen Anzeige- oder Selbsteinschätzungsstufen nicht als echte Leistung.
