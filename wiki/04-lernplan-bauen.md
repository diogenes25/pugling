# 04 · Einen Lernplan bauen (Vater)

← [Zurück zum Wiki-Index](../README.md)

Diese Seite zeigt den aktuellen Vater-Flow: Katalog-Übungen auswählen oder anlegen, einen
Study-Plan-Container für ein Kind erstellen, Übungen als Positionen anhängen und anschließend über
Overview/Report kontrollieren. Alle schreibenden Aufrufe brauchen den Vater-Bearer-Token
([02 · Auth](02-authentifizierung.md)).

> **Was ist ein Study-Plan?** Ein Plan ist nur der Rahmen für ein Kind: Titel, Laufzeit, aktiv/inaktiv
> und optional ein Fach. Das eigentliche Training hängt an `PlanPosition`en. Jede Position verweist auf
> eine Katalog-Übung und trägt ihre eigenen Overrides: Stufe, Item-Auswahl, Zielrhythmus,
> Bestehensschwelle, Leitner und Punkte.

---

## 0. Der Flow in vier Schritten

```text
1. Katalog-Übungen anlegen oder suchen       (Subject → Chapter → Exercise)
2. Study-Plan-Container anlegen              (Kind, Titel, Laufzeit)
3. Übungen als PlanPositionen hinzufügen     (Ziele, Leitner, Punkte, Stufen)
4. Kontrolle                                 (overview / progress / position report / points)
```

---

## 1. Katalog-Inhalte anlegen oder suchen

Eine Position verweist auf eine bestehende Katalog-`Exercise`. Du kannst also vorhandene Übungen
wiederverwenden oder neue anlegen.

```http
POST /api/v1/learn/subjects
{ "name": "Französisch" }

POST /api/v1/learn/subjects/{subjectId}/chapters
{ "name": "Unité 1 – Salutations", "orderIndex": 1 }

POST /api/v1/learn/subjects/{subjectId}/chapters/{chapterId}/vocabulary
{
  "title": "Begrüßungen",
  "orderIndex": 1,
  "rewardPoints": 10,
  "config": {
    "direction": "front-to-back",
    "sourceLang": "fr",
    "targetLang": "de",
    "items": [
      { "front": "bonjour", "back": "hallo" },
      { "front": "merci", "back": "danke" }
    ]
  },
  "defaultStage": 2,
  "defaultItemCount": 20,
  "defaultUseLeitner": true,
  "defaultRequireTypedTest": true,
  "suggestedBonus": {
    "comboThreshold": 5,
    "comboBonusPoints": 5,
    "speedThresholdSeconds": 8,
    "speedBonusPoints": 3,
    "newContentPoints": 12
  }
}
```

Der Request ist gültig, obwohl die Response die Vokabeln nicht mehr in `config.items` zurückliefert.
`defaultStage` und `defaultItemCount` werden als Übungs-Defaults gespeichert und in der `ExerciseResponse`
zurückgegeben. Für die tatsächliche Vokabelmenge ist die Item-Subressource maßgeblich.

`rewardPoints` ist aktuell weiterhin ein Pflichtfeld des Katalog-DTOs und taucht in Katalog-/Brief-Responses
auf. Die produktiven Lernpunkte entstehen aber über `PlanPosition` (`newContentPoints`, `pointsGoalMet`,
Combo/Speed) und nicht mehr direkt über dieses Katalogfeld.

Bei Vokabelübungen ist `config.items` nur Authoring-Payload. Der Server legt fehlende Store-Vokabeln an,
materialisiert die Übungsmenge in `ExerciseItem`-Zeilen und gibt die Config danach ohne Items zurück.
Die tatsächlich gespeicherten Vokabelpaare liest und pflegt man über:

```http
GET  /api/v1/learn/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{exerciseId}/items
POST /api/v1/learn/subjects/{subjectId}/chapters/{chapterId}/vocabulary/{exerciseId}/items
{ "vocabularyId": 26, "hint": "die" }
```

Wenn die Übung bereits in einer PlanPosition steckt, ist das Anhängen ans Ende weiter erlaubt;
Löschen, Umsortieren oder Einfügen an fester Position wird blockiert, damit gespeicherter Lernfortschritt
nicht auf ein anderes Wort verschoben wird.

Vorhandene Übungen findest du über die Katalogsuche:

```http
GET /api/v1/learn/exercises?subjectId=1&grade=9&schoolType=Gymnasium&type=Vocabulary
```

Details zu allen Übungstypen stehen in [03 · Übungstypen](03-uebungstypen.md).

---

## 2. Den Study-Plan-Container anlegen

```http
POST /api/v1/study-plans
Authorization: Bearer <VATER-TOKEN>

{
  "childId": 1,
  "title": "Französisch – Vokabeltest in 10 Tagen",
  "subjectId": 4,
  "durationDays": 10,
  "description": "Unité 1, tägliche Wiederholung bis zur Klassenarbeit"
}
→ 201 PlanResponse { id, childId, title, subjectId, startDate, endDate, active, positionCount, description, isPlayable }
```

### Felder (`CreatePlanDto`)

| Feld | Default | Bedeutung |
| --- | --- | --- |
| `childId` *(req)* | — | Eigenes Kind (sonst 404). |
| `title` *(req)* | — | Anzeigename. |
| `subjectId` | null | Optionales Katalog-Fach zur Einordnung/Filterung. |
| `startDate` | heute (UTC) | Beginn. |
| `durationDays` | 10 | Laufzeit; `EndDate = start + duration − 1`. |
| `description` | null | Freie Beschreibung/Zielnotiz. |

Ein neuer aktiver Plan deaktiviert andere aktive Pläne desselben Kindes. Der Sohn sieht damit nur den
einen aktuell spielbaren Plan und kann nicht zwischen leichten Plänen wechseln.

Nachträglich änderbar:

```http
PATCH /api/v1/study-plans/{planId}
{ "title": "Französisch – Klassenarbeit", "active": true, "endDate": "2026-07-15" }
```

---

## 3. Positionen hinzufügen

```http
POST /api/v1/study-plans/{planId}/positions
{
  "exerciseId": 13,
  "cadence": "Daily",
  "orderStrategy": "WeakestFirst",
  "goalThreshold": 80,
  "stageSchedule": [
    { "dayNumber": 1, "stage": 2 },
    { "dayNumber": 5, "stage": 3 },
    { "dayNumber": 8, "stage": 4 }
  ]
}
```

Das ist der sinnvolle Minimal-Request für die oben angelegte Vokabelübung: `defaultUseLeitner`,
`defaultRequireTypedTest` und `suggestedBonus` kommen bereits von der Exercise. `exerciseId` reicht
technisch aus; alles Weitere sind Overrides. Leere Felder erben Defaults der Übung: `stage`/`itemCount`
bleiben dann `null`, `requireTypedTest` und `useLeitner` kommen aus der Exercise, Bonuswerte aus
`SuggestedBonus` bzw. den Modell-Defaults.

Nur echte Abweichungen sendest du zusätzlich, z. B. eine feste Stufe ohne Fahrplan oder eigene Intervalle:

```json
{
  "exerciseId": 13,
  "stage": 4,
  "useLeitner": true,
  "maxBox": 6,
  "boxIntervalDays": [0, 1, 2, 4, 7, 14, 30]
}
```

### Felder (`CreatePositionDto`)

| Feld | Default | Bedeutung |
| --- | --- | --- |
| `exerciseId` *(req)* | — | Katalog-Übung, deren Config gespielt wird. |
| `order` | nächster Index | Reihenfolge im Plan. |
| `stage` | Übungs-Default | Verfahrensabhängige Spiel-/Teststufe. |
| `itemCount` | Übungs-Default/alle | Wie viele Inhaltsatome aus der Übung genutzt werden. |
| `scope` | `All` | `All`, `New` oder `Old`. |
| `cadence` | `None` | `Daily`/`Weekly` macht daraus ein Pflichtziel. |
| `orderStrategy` | `WeakestFirst` | Ausspielreihenfolge: `WeakestFirst`, `Serial`, `Random`, `NewestWeighted`. |
| `goalThreshold` | typabhängig | Bei Tests Prozentgrenze, bei Check-Aufgaben Anzahl/Schwelle. |
| `requireTypedTest` | Übungs-Default | Nur getippte/gewertete Stufen zählen als echt. |
| `useLeitner` | Übungs-Default | Karteikasten-Fälligkeit aktivieren. |
| `maxBox` | 5 | Höchste Leitner-Box. |
| `boxIntervalDays` | `[0,1,2,4,7,14]` | Intervall je Box. |
| `stageSchedule` | null | Tag → Stufe, übersteuert die feste `stage`. |
| `pointsGoalMet` | 20 | Münzen für ein erfülltes Positionsziel je Periode. |
| `newContentPoints` | Bonus-Vorschlag/10 | Basispunkte für erstmals geübten Inhalt. |
| `combo*`, `speed*` | Bonus-Vorschlag | Gem-Boni für Trefferfolgen und schnelle Antworten. |

Nicht im Request enthalten sind interne/abgeleitete Felder wie `positionId`, `exerciseTitle` oder
`exerciseType`; sie kommen aus der Response bzw. aus Serverlogik.
Ebenfalls weglassen solltest du Felder, die nur Defaults wiederholen: `order: 0`, `scope: "All"`,
`itemCount: null`, `orderStrategy: "WeakestFirst"`, `maxBox: 5`, `boxIntervalDays: [0,1,2,4,7,14]`,
`pointsGoalMet: 20` sowie Bonuswerte, die identisch zum `suggestedBonus` der Übung sind. `stage` ist
wirkungslos, wenn ein `stageSchedule` schon ab Tag 1 greift; dann liefert der Fahrplan immer die Stufe.

Positionen ändern oder löschen:

```http
GET    /api/v1/study-plans/{planId}/positions
PATCH  /api/v1/study-plans/{planId}/positions/{positionId}
DELETE /api/v1/study-plans/{planId}/positions/{positionId}
```

Eine Position mit vorhandenen Übungs-/Testdaten kann nicht gelöscht werden (`position_has_data`).

---

## 4. Stufen und Leitner

Die Stufe kommt für den Sohn serverseitig aus `stageSchedule` bzw. `stage`; nur der Vater darf beim
Teststart eine Stufe frei übersteuern. Getippte Stufen werden pro Übungstyp durch
`PositionPlayService.IsTypedStage` entschieden. Beispiele:

| Typ | Typische Stufen |
| --- | --- |
| `Vocabulary` | `TestStage`: Anzeigen, Selbsteinschätzung, Buchstabenfelder, Freitext, Audio, Multiple Choice |
| `Cloze` | Wortbank, Übersetzung+Wortbank, Übersetzung+Freitext, Freitext |
| `Matching` | Direkt, mit Ablenkern, Rückrichtung, Rückrichtung mit Ablenkern |

`useLeitner=true` aktiviert Box/Fälligkeit pro Inhalts-Atom der Position (`PositionItemProgress`):
richtig → Box hoch und neue Fälligkeit, falsch → Box 1 und sofort fällig. Gewertet wird nur eine
fällige Karte und höchstens einmal pro Tag; bei `requireTypedTest=true` zählen reine
Selbsteinschätzungs-Stufen nicht für Punkte/Box.

---

## 5. Üben, Testen und Kontrolle

Der Sohn spielt immer eine konkrete Position:

```http
POST /api/v1/study-plans/{planId}/positions/{positionId}/practice-sessions
{ "mode": "Lern" }

GET  /api/v1/study-plans/{planId}/positions/{positionId}/practice-sessions/{sessionId}/cards
GET  /api/v1/study-plans/{planId}/positions/{positionId}/practice-sessions/{sessionId}/next

POST /api/v1/study-plans/{planId}/positions/{positionId}/practice-sessions/{sessionId}/review
{ "itemIndex": 0, "givenAnswer": "hallo" }

POST /api/v1/study-plans/{planId}/positions/{positionId}/practice-sessions/{sessionId}/review
{ "itemIndex": 1, "wasKnown": true }

POST /api/v1/study-plans/{planId}/positions/{positionId}/practice-sessions/{sessionId}/heartbeat
{ "seconds": 60, "active": true }

POST /api/v1/study-plans/{planId}/positions/{positionId}/practice-sessions/{sessionId}/end

POST /api/v1/study-plans/{planId}/positions/{positionId}/tests
{ }

GET  /api/v1/study-plans/{planId}/positions/{positionId}/tests/{attemptId}

GET  /api/v1/study-plans/{planId}/positions/{positionId}/tests/{attemptId}/next

POST /api/v1/study-plans/{planId}/positions/{positionId}/tests/{attemptId}/answer
{ "itemIndex": 0, "givenAnswer": "hallo" }

POST /api/v1/study-plans/{planId}/positions/{positionId}/tests/{attemptId}/answer
{ "itemIndex": 1, "wasKnown": true }

POST /api/v1/study-plans/{planId}/positions/{positionId}/tests/{attemptId}/submit
{ }
```
Im Lernmodus ist `next` der normale Weg: Der Server führt den Cursor und liefert nach `review` direkt die
nächste Karte mit. `cards` bleibt für Info-Modus und Offline-Fallback. Tests laufen ebenfalls
servergeführt: `next` liefert die aktuelle Frage, `answer` speichert die Antwort ohne Feedback, `submit`
schließt den Versuch ab und liefert erst dann Ergebnis und Lösungen. Für den Bulk-/Fallback-Modus kann
`submit` weiterhin eine `answers`-Liste enthalten; im normalen Klausurmodus reicht `{}`.

Plan-Übersicht und Fortschritt:

```http
GET /api/v1/study-plans/{planId}/overview
GET /api/v1/study-plans/{planId}/overview/progress[?from=&to=&dutyDone=&sort=&skip=&take=]
GET /api/v1/study-plans/{planId}/positions/{positionId}/report
GET /api/v1/children/{childId}/vocabulary-progress[?exerciseId=&maxBox=&onlyWeak=&skip=&take=]
GET /api/v1/children/{childId}/vocabulary-progress/by-word[?onlyWeak=&skip=&take=]
GET /api/v1/children/{childId}/points
```

Nur der **Vater** darf einen Tag nachtragen (`day` beim Practice-/Test-Start ungleich heute).

---

## 6. Vollständiges Beispiel

Ziel: Kind 1 übt französische Begrüßungen zehn Tage lang, täglich als Pflichtposition, mit Leitner,
getippten Tests und kleinen Bonus-Anreizen.

```http
# 1) Login
POST /api/v1/auth/father   { "fatherId": 1, "pin": "0000" }

# 2) Katalog-Übung anlegen oder vorhandene Übung suchen
POST /api/v1/learn/subjects/{subjectId}/chapters/{chapterId}/vocabulary
{ "title":"Begrüßungen", "orderIndex":1, "rewardPoints":10,
  "config": { "direction":"front-to-back", "sourceLang":"fr", "targetLang":"de",
    "items":[{"front":"bonjour","back":"hallo"},{"front":"merci","back":"danke"}] },
  "defaultStage":2, "defaultItemCount":20, "defaultUseLeitner":true, "defaultRequireTypedTest":true }
→ 201 { "id": 13, … }

# 3) Plan-Container
POST /api/v1/study-plans
{ "childId":1, "title":"Französisch – Vokabeltest in 10 Tagen", "subjectId":4, "durationDays":10 }
→ 201 { "id": 42, "positionCount": 0, … }

# 4) Position
POST /api/v1/study-plans/42/positions
{ "exerciseId":13, "cadence":"Daily", "goalThreshold":80,
  "stageSchedule":[{"dayNumber":1,"stage":2},{"dayNumber":5,"stage":3},{"dayNumber":8,"stage":4}] }
→ 201 { "id": 7, "exerciseTitle":"Begrüßungen", … }

# 5) Kontrolle
GET /api/v1/study-plans/42/overview
GET /api/v1/study-plans/42/positions/7/report
GET /api/v1/children/1/points
```

Der Sohn kann danach über [06 · Sohn-App](06-sohn-app.md) loslegen.

---

## 7. Missionen & Auszeichnungen

Zusätzlich zum laufenden Punktesystem definierst du pro Kind **Missionen** (Tages-/Wochenziele) und
**Auszeichnungen** (Badges). Sie messen serverseitige Metriken und belohnen idempotent — voll erklärt
in [05 · Punkte & Bonus §5](05-punkte-und-bonus.md#5-missionen--auszeichnungen).

```http
POST /api/v1/children/1/missions
{ "title": "Tagesziel: 10 richtige Antworten", "metric": "CorrectReviews", "target": 10, "period": "Daily", "rewardPoints": 15 }

POST /api/v1/children/1/achievements
{ "title": "Feuer-Streak", "icon": "🔥", "metric": "StreakDays", "threshold": 7, "rewardPoints": 70 }
```
