# Pugling – Tutorial (API)

> 📖 **Teil des Wikis.** Dieser kompakte Walkthrough hat eine ausführliche Entsprechung im Wiki:
> [04 · Lernplan bauen (Vater)](../wiki/04-lernplan-bauen.md) und [06 · Sohn-App](../wiki/06-sohn-app.md),
> mit allen DTO-Feldern, dem [Punkte- & Bonus-System](../wiki/05-punkte-und-bonus.md) und der
> [API-Referenz](../wiki/07-api-referenz.md). Einstieg: [README](../README.md).

Wie **Vater (Klaus)** das Tool einrichtet/steuert und wie **Sohn (Peter)** damit lernt.
Alle Beispiele gehen von `http://localhost:5200` aus. Geschützte Aufrufe brauchen den JWT im Header
`Authorization: Bearer <token>`. Swagger-UI unter `/swagger` (dort „Authorize"-Button nutzen).

Startdaten aus dem Seed: Vater `id=1` PIN `0000`, Sohn (Kind) `id=1` PIN `1111`.

---

## Teil 1 – Vater (Einrichtung & Kontrolle)

### 1.1 Anmelden

```http
POST /api/v1/auth/father   { "fatherId": 1, "pin": "0000" }
→ { token, role:"Vater", ... }
```

Den `token` in allen weiteren Aufrufen als Bearer mitgeben.

### 1.2 (Optional) Kind anlegen / PIN vergeben

```http
POST /api/v1/children        { "name":"Peter", "birthYear":2015, "pin":"1111" }
GET  /api/v1/children        → Liste inkl. Punktestand
```

### 1.3 Lerngrundlagen anlegen

**Vokabeln** (Basis für Vokabel- und Matching-Verfahren):

```http
POST /api/v1/learn/vocabulary
{ "key":"vt_dog","sourceLanguage":"en","targetLanguage":"de",
  "word":"dog","translation":"Hund","partOfSpeech":"Noun" }
```

**Lückentexte** (für das Lückentext-Verfahren):

```http
POST /api/v1/learn/cloze-texts
{ "key":"cz_greet_1","title":"Greeting","sourceLanguage":"en","targetLanguage":"de",
  "text":"A: {{1}}, how are you? B: I am {{2}}.",
  "translation":"A: Hallo, … B: Mir geht es gut.",
  "gaps":[{"index":1,"answer":"Hello","alternatives":["Hi"]},
          {"index":2,"answer":"fine","alternatives":["good"]}],
  "wordBank":["Hello","Hi","fine","good","bad"] }
```

### 1.4 Lehrplan erstellen (drei Verfahren – gleicher Rahmen)

Gemeinsame Stellschrauben: `dailyMinutesRequired`, `dailyTestPassPercent`, `requireTypedTest`,
`stageSchedule` (Schwierigkeit steigt über die Tage), Punkte (`pointsMinutesMet`, `pointsTestPassed`, `pointsDayCompleteBonus`).

**Vokabel-Plan** (mit Stufen-Fahrplan + Anti-Schummel):

```http
POST /api/v1/study-plans
{ "childId":1, "title":"Vokabeltest in 10 Tagen", "method":"Vocabulary",
  "durationDays":10, "dailyMinutesRequired":20, "dailyTestPassPercent":80,
  "requireTypedTest":true,
  "contentKeys":["vt_dog","vt_cat","vt_sun", …],
  "stageSchedule":[{"dayNumber":1,"stage":1},{"dayNumber":3,"stage":2},
                   {"dayNumber":5,"stage":3},{"dayNumber":7,"stage":4},{"dayNumber":9,"stage":5}] }
```

*Vokabel-Stufen:* 1 = Vokabel+Übersetzung zeigen · 2 = Selbsteinschätzung · 3 = Buchstabenfelder (getippt) · 4 = Freitext · 5 = Vorlesen+Freitext.

**Lückentext-Plan:** `"method":"Cloze"`, `contentKeys` = Cloze-Keys.
*Stufen:* 1 = Wortpool · 2 = Übersetzung+Wortpool · 3 = Übersetzung+Freitext · 4 = Freitext.

**Matching-Plan:** `"method":"Matching"`, `contentKeys` = Vokabel-Keys.
*Stufen:* 1 = Wort→Übersetzung · 2 = + Ablenker · 3 = Übersetzung→Wort · 4 = + Ablenker.

Weitere Vokabel/Text nachträglich: `POST /api/v1/study-plans/{id}/items { "contentKey":"…" }`.
Regeln ändern: `PATCH /api/v1/study-plans/{id}`.

### 1.4b Nach Stundenplan lernen (Vorbereitung vs. neuer Stoff)

Damit der Plan sich am Schul-Stundenplan ausrichtet:

1. **Plan mit Fach koppeln:** beim Erstellen `"subjectId": <Katalog-Fach>` und `"newItemsPerLesson": 5` setzen.
2. **Stundenplan pflegen:**

```http
POST /api/v1/children/{childId}/timetable   { "subjectId": 1, "dayOfWeek":"Tuesday", "timeOfDay":"Nachmittag" }
GET  /api/v1/children/{childId}/timetable
```

Wirkung (automatisch): Am **Unterrichtstag** (Di) führt der Test **neuen Stoff** ein (nächste `newItemsPerLesson` Inhalte); an **allen anderen Tagen** wird der bereits eingeführte Stoff **wiederholt** – der Tag direkt davor (Mo) ist der Vorbereitungstag. Tests ziehen automatisch den passenden Tages-Pool. Ohne Fach/Stundenplan verhält sich der Plan wie bisher (alle Inhalte).

### 1.5 Fortschritt & Kontrolle

```http
GET /api/v1/study-plans/{id}/today     → heute Pflicht erfüllt? empfohlene Stufe, offene Punkte, Streak, schwache Inhalte
GET /api/v1/study-plans/{id}/progress  → Tag-für-Tag: Minuten, bester Test, komplett?, Punkte
GET /api/v1/study-plans/{id}/report    → pro Inhalt: Wiederholungen, Test-Treffer, Mastery% + Testhistorie
GET /api/v1/children/1/points → Punkte-Saldo + Buchungen (Ledger)
```

Manuelle Punktekorrektur / Belohnung:

```http
POST /api/v1/children/1/points   { "amount": 30, "reason":"Extra fürs Dranbleiben" }
```

Nur der Vater darf zudem einen Tag **nachtragen** (`day` im Practice/Test-Start) – Sohn nicht.

---

## Teil 2 – Sohn (Lernen)

### 2.1 Anmelden

```http
POST /api/v1/auth/child   { "childId": 1, "pin": "1111" }   → token (Rolle "Sohn")
```

### 2.2 Was ist heute zu tun?

```http
GET /api/v1/study-plans?childId=1        → meine Pläne
GET /api/v1/study-plans/{id}/today       → dutyDone, offene Pflichten, empfohlene Stufe, schwache Inhalte
```

Ziel jeden Tag: **20 min üben** UND den **Abschlusstest ≥ 80 %** bestehen. Beides zählt getrennt.

### 2.3 Üben (Zeit sammeln)

```http
POST /api/v1/study-plans/{id}/practice-sessions            {}                → sessionId
GET  /api/v1/study-plans/{id}/practice-sessions/{sid}/cards → fällige Übungskarten (OHNE Lösung)
POST /api/v1/study-plans/{id}/practice-sessions/{sid}/review
      { "contentId":<vokabel/cloze-id>, "stage":4, "givenAnswer":"Haus" }   // getippte Stufe
      { "contentId":<cloze-id>, "stage":3, "gaps":[{"gapIndex":1,"givenAnswer":"…"}] }  // Lückentext
      { "contentId":<vokabel-id>, "stage":2, "wasKnown":true }              // Selbsteinschätzung
   → { wasCorrect, expected, awarded, box, dueOn, combo, comboBonus }
POST /api/v1/study-plans/{id}/practice-sessions/{sid}/heartbeat
      { "seconds":1200, "active":true }   → Antwort zeigt live den Tagesfortschritt
POST /api/v1/study-plans/{id}/practice-sessions/{sid}/end
```

Der **Server bewertet** die Antwort (nicht das Frontend) und gibt richtig/falsch, die Lösung und die
vergebenen Punkte/Combo zurück – das Frontend würdigt das per Animation. Nur „aktive" Sekunden zählen;
ist die Tagesübungszeit erreicht, gibt es Punkte.

### 2.4 Abschlusstest machen

Je nach Verfahren des Plans der passende Endpunkt. Ohne `stage` wird die **Fahrplan-Stufe des Tages** genommen.

**Vokabeltest:**

```http
POST /api/v1/study-plans/{id}/tests            { }             → attemptId + Karten (ohne Lösung)
POST /api/v1/study-plans/{id}/tests/{aid}/hint { "vocabularyId":X }   (Stufe 3+: deckt einen Buchstaben auf)
POST /api/v1/study-plans/{id}/tests/{aid}/submit
   { "answers":[ {"vocabularyId":X,"wasKnown":true},              // Stufe 2
                 {"vocabularyId":Y,"givenAnswer":"Hund"} ] }      // Stufe 3–5
→ score, passed, dayProgress (inkl. vergebener Punkte)
```

**Lückentext-Test:**

```http
POST /api/v1/study-plans/{id}/cloze-tests             { }   → Texte mit Lücken + (je Stufe) Übersetzung/Wortpool
POST /api/v1/study-plans/{id}/cloze-tests/{aid}/hint  { "clozeTextId":X,"gapIndex":1 }   (nur Freitext-Stufen)
POST /api/v1/study-plans/{id}/cloze-tests/{aid}/submit
   { "answers":[ {"clozeTextId":X,"gapIndex":1,"givenAnswer":"Hello"} ] }
```

**Matching-Test:**

```http
POST /api/v1/study-plans/{id}/matching-tests             { }   → items (Prompts) + options (gemischter Pool)
POST /api/v1/study-plans/{id}/matching-tests/{aid}/submit
   { "answers":[ {"vocabularyId":X,"chosenAnswer":"Hund"} ] }
```

### 2.5 Belohnung

Punktestand: `GET /api/v1/children/1/points` (oder im `today`/Report). Beim Bestehen und beim
Erreichen der Übungszeit fließen automatisch Punkte; ein kompletter Tag (beides) gibt zusätzlich einen Bonus
und verlängert den **Streak**.

### 2.6b Heutiger Modus & Übungen bewerten

`today` zeigt (bei Plan mit Stundenplan) den **Modus** des Tages (`New` = neuer Stoff / `Review` = Wiederholung), ob es ein Vorbereitungstag ist, und die **fälligen Inhalte** (`dueItems`). Die Tests ziehen automatisch genau diesen Pool.

Der Sohn kann jeden Inhalt bewerten – das hilft dem Vater, den Plan an den echten Unterricht anzupassen:

```http
POST /api/v1/study-plans/{id}/ratings
  { "contentId": <vokabel/cloze-id>, "feedback": "SehrGut|Gut|Neutral|Schlecht|Fehler", "comment": "optional" }
```

Bedeutung: **SehrGut** = genau unser aktuelles Thema · **Gut** = unser Stoff, aber Wiederholung · **Neutral** = passt zu meinem Stand · **Schlecht** = haben wir noch nicht · **Fehler** = Übung ist fehlerhaft. Der Vater sieht die Bewertungen im `report`.

### 2.6 Was der Sohn NICHT kann (by design)

- Keine Lehrpläne/Inhalte anlegen oder ändern (nur Vater).
- Keine fremden Pläne sehen/bedienen (nur die eigenen).
- Keinen anderen Tag „nachtragen" – der Test von heute muss heute gemacht werden.
- Bei `requireTypedTest`-Plänen zählt Selbsteinschätzung nicht – es muss wirklich getippt werden.
