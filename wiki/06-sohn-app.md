# 06 · Anleitung für die Sohn-App

← [Zurück zum Wiki-Index](../README.md)

Der **Sohn** lernt — er legt keine Inhalte an, sondern übt, testet und sammelt Punkte. Alle Aufrufe
brauchen den **Sohn-Bearer-Token**. Der Server bewertet jede Antwort selbst; das Frontend würdigt das
per Animation.

---

## 1. Anmelden

```http
POST /api/v1/auth/child   { "childId": 1, "pin": "1111" }
→ { "token": "eyJ…", "role": "Sohn", … }
```

Token in allen weiteren Aufrufen als `Authorization: Bearer <token>` mitgeben.

---

## 2. Was ist heute zu tun?

```http
GET /api/v1/study-plans?childId=1        // meine Pläne (Sohn sieht nur eigene)
GET /api/v1/study-plans/{id}/today
```

`today` liefert alles für den Tag auf einen Blick:

```jsonc
{
  "dutyDone": false,
  "recommendedStage": 3,               // die heute geltende Stufe (aus dem Fahrplan)
  "mode": "Review",                    // New | Review (bei Stundenplan-Kopplung)
  "isPreparationDay": true,
  "currentStreak": 2,
  "progress": {
    "minutesPracticed": 8, "minutesMet": false,
    "bestScorePercent": 0, "testPassed": false, "dayComplete": false,
    "pointsAwarded": 0,
    "outstanding": ["Noch 12 min üben", "Test noch nicht bestanden"]   // offene Pflichten im Klartext
  },
  "dueItems": [ … ],                   // die heute fälligen Inhalte
  "weakItems": [ … ]                   // Inhalte mit Mastery < Bestehensgrenze (gezielt üben)
}
```

**Tagesziel:** die geforderten Minuten üben **und** den Abschlusstest bestehen — beides zählt getrennt.

---

## 3. Üben (Zeit sammeln + Karten wiederholen)

```http
POST /api/v1/study-plans/{id}/practice-sessions            {}            → { id (=sessionId), … }
GET  /api/v1/study-plans/{id}/practice-sessions/{sid}/cards               → fällige Karten (OHNE Lösung)
```

Eine Karte (`PracticeCard`) enthält je nach Stufe/Verfahren: `prompt`, `stage`, `method`,
`answerLength` (Stufe LetterBoxes), `audioUrl` (Audio), `translation` und `wordBank` (Cloze-Hilfen),
`gapIndexes` (Cloze). Getippte Stufen liefern **keine** Lösung — der Server bewertet.

### Eine Antwort abgeben (`review`) — server-autoritativ

Die Stufe bestimmt der Server aus dem Fahrplan; das Frontend schickt nur die passende Antwortform:

```jsonc
// getippte Vokabel-/Matching-Stufe
POST …/practice-sessions/{sid}/review   { "contentId": 101, "givenAnswer": "hallo" }

// Lückentext (pro Lücke)
POST …/practice-sessions/{sid}/review   { "contentId": 55, "gaps": [ { "gapIndex": 1, "givenAnswer": "Bonjour" } ] }

// reine Anzeige-/Selbsteinschätzungs-Stufe
POST …/practice-sessions/{sid}/review   { "contentId": 101, "wasKnown": true }
```

Antwort bei gewerteten (Leitner-)Reviews — `ReviewOutcome`:

```jsonc
{ "wasCorrect": true, "expected": "hallo", "awarded": 18, "box": 2,
  "dueOn": "2026-07-06", "combo": 5, "comboBonus": 5, "speedBonus": 3 }
```

> Bei Nicht-Leitner-Plänen oder bei Selbsteinschätzung unter `requireTypedTest` liefert `review` ein
> **`204`** (nur protokolliert, keine Punkte/Box-Bewegung). Punkte-Details: [05 · Punkte](05-punkte-und-bonus.md).

### Zeit zählen & beenden

```http
POST …/practice-sessions/{sid}/heartbeat   { "seconds": 60, "active": true }   → aktueller Tagesfortschritt
POST …/practice-sessions/{sid}/end                                            → Tagesfortschritt
```

Nur **aktive** Sekunden zählen; pro Heartbeat max. 120 s anrechenbar. Ist die Tages-Übungszeit
erreicht, fließen Minuten-Punkte.

---

## 4. Abschlusstest machen

Je nach `method` des Plans der passende Endpunkt. **Ohne `stage`** nimmt der Server automatisch die
Fahrplan-Stufe des Tages. Der Start liefert die Karten **ohne Lösung**; Bewertung + Punkte beim Submit.

### Vokabeltest

```http
POST /api/v1/study-plans/{id}/tests                     {}                    → attemptId + Karten
POST /api/v1/study-plans/{id}/tests/{aid}/hint          { "vocabularyId": X } // Stufe 3+: deckt einen Buchstaben auf
POST /api/v1/study-plans/{id}/tests/{aid}/submit
{
  "answers": [
    { "vocabularyId": X, "wasKnown": true },              // Stufe 2 (SelfAssess)
    { "vocabularyId": Y, "givenAnswer": "Hund" }          // Stufe 3–5 (getippt)
  ]
}
→ { score, passed, dayProgress }
```

### Lückentext-Test

```http
POST /api/v1/study-plans/{id}/cloze-tests               {}   → Texte mit Lücken (+ je Stufe Übersetzung/Wortpool)
POST /api/v1/study-plans/{id}/cloze-tests/{aid}/hint    { "clozeTextId": X, "gapIndex": 1 }   // nur Freitext-Stufen
POST /api/v1/study-plans/{id}/cloze-tests/{aid}/submit
{ "answers": [ { "clozeTextId": X, "gapIndex": 1, "givenAnswer": "Bonjour" } ] }
```

### Matching-Test

```http
POST /api/v1/study-plans/{id}/matching-tests            {}   → items (Prompts) + options (gemischter Pool)
POST /api/v1/study-plans/{id}/matching-tests/{aid}/submit
{ "answers": [ { "vocabularyId": X, "chosenAnswer": "Hund" } ] }
```

Beliebig viele Versuche erlaubt; es zählt der **beste**. Bei `requireTypedTest` zählt ein Test nur auf
einer getippten/gewerteten Stufe als bestanden.

---

## 5. Inhalte bewerten (Feedback an den Vater)

Der Sohn bewertet jeden Plan-Inhalt 5-stufig — hilft dem Vater, den Plan an den echten Unterricht
anzupassen (erscheint im `report`):

```http
POST /api/v1/study-plans/{id}/ratings
{ "contentId": <vokabel/cloze-id>, "feedback": "SehrGut", "comment": "optional" }
```

Bedeutung: **SehrGut** = genau unser aktuelles Thema · **Gut** = unser Stoff, Wiederholung ·
**Neutral** = passt zu meinem Stand · **Schlecht** = haben wir noch nicht · **Fehler** = Übung ist fehlerhaft.

---

## 6. Punktestand, Missionen & Auszeichnungen

```http
GET /api/v1/me/points          → { balance, entries:[ { amount, kind, reason, createdAt } ] }
GET /api/v1/me/missions        → Tages-/Wochenziele mit Fortschritt { title, target, current, completed, rewardPoints }
GET /api/v1/me/achievements    → Badges { title, icon, threshold, current, earned, earnedAt }
```

Beim Bestehen, beim Erreichen der Übungszeit und bei vollständigen Tagen fließen automatisch Punkte;
ein kompletter Tag verlängert den **Streak**. Missionen/Auszeichnungen werden beim Üben laufend
ausgewertet und belohnt.

---

## 7. Was der Sohn NICHT kann (by design)

- Keine Lehrpläne/Inhalte anlegen oder ändern (nur Vater) → **403**.
- Keine fremden Pläne sehen/bedienen — nur die eigenen.
- Keinen anderen Tag „nachtragen" (`day` ≠ heute) → **403**. Der Test von heute muss heute gemacht werden.
- Bei `requireTypedTest`-Plänen zählt Selbsteinschätzung nicht — es muss wirklich getippt werden.
- Nichts „richtig" melden: der Server bewertet jede Antwort selbst.
