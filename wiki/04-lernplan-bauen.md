# 04 · Einen Lernplan bauen (Vater)

← [Zurück zum Wiki-Index](../README.md)

Diese Seite zeigt den **kompletten Vater-Flow**, um einem Kind einen trainierbaren **Study-Plan**
anzulegen — von den Lerninhalten über die Plan-Einstellungen (Stufen, Leitner, Stundenplan, Punkte,
Bonus) bis zur Kontrolle. Alle Aufrufe brauchen den **Vater-Bearer-Token** ([02 · Auth](02-authentifizierung.md)).

> **Was ist ein Study-Plan?** Ein verfahrensneutrales Trainingsobjekt für **ein Kind**: Zeit, Punkte,
> Fortschritt und Abschlusstest gelten für jedes Lernverfahren gleich; verfahrensspezifisch sind nur
> Inhalt und Test-Stufen. `Method` ∈ `Vocabulary | Cloze | Matching`.

---

## 0. Der Flow in vier Schritten

```text
1. Inhalte in einen STORE legen   (Vokabeln bzw. Lückentexte)
2. Study-Plan anlegen             (Method + contentKeys + Regeln + Bonus)
3. (optional) Stundenplan koppeln (neuer Stoff am Unterrichtstag)
4. Kontrolle                      (today / progress / report / points)
```

---

## 1. Lerninhalte anlegen (Store)

Study-Pläne referenzieren Inhalte **per `Key`** aus einem Store — nicht aus dem Katalog.

### Vokabeln (für `Vocabulary` und `Matching`)

```http
POST /api/v1/learn/vocabulary
{
  "key": "fr_bonjour_de_hallo",
  "sourceLanguage": "fr", "targetLanguage": "de",
  "word": "bonjour", "translation": "hallo",
  "partOfSpeech": "Interjection"
}
```

Optional bei Substantiven/Verben (fürs korrekte Lernen): `noun` (`{ article, genus, plural }`),
`verb` (`{ isBaseForm, infinitive, tense, person, number }`), `baseFormKey`, `pronunciationAudioUrl`
(für Audio-Stufe 5), `version`. `partOfSpeech` ∈ `Noun|Verb|Adjective|Adverb|…` (String).

### Lückentexte (für `Cloze`)

```http
POST /api/v1/learn/cloze-texts
{
  "key": "fr_greet_1", "title": "Salutations",
  "sourceLanguage": "fr", "targetLanguage": "de",
  "text": "A: {{1}}, comment ça va? B: Ça va {{2}}.",
  "translation": "A: Hallo, wie geht's? B: Gut.",
  "gaps": [
    { "index": 1, "answer": "Bonjour", "alternatives": ["Salut"] },
    { "index": 2, "answer": "bien",    "alternatives": ["très bien"] }
  ],
  "wordBank": ["Bonjour","Salut","bien","mal"]
}
```

> Stores sind **Vater-only** und global. Ein Store-Eintrag ist löschgeschützt, solange er in einem
> Plan verwendet wird.

---

## 2. Den Study-Plan anlegen

```http
POST /api/v1/study-plans
Authorization: Bearer <VATER-TOKEN>

{
  "childId": 1,
  "title": "Französisch – Vokabeltest in 10 Tagen",
  "method": "Vocabulary",                       // Vocabulary | Cloze | Matching
  "durationDays": 10,
  "contentKeys": ["fr_bonjour_de_hallo", "fr_merci_de_danke", "…"],

  "dailyMinutesRequired": 20,
  "dailyTestPassPercent": 80,
  "requireTypedTest": true,

  "stageSchedule": [
    { "dayNumber": 1, "stage": 1 }, { "dayNumber": 3, "stage": 2 },
    { "dayNumber": 5, "stage": 3 }, { "dayNumber": 7, "stage": 4 }, { "dayNumber": 9, "stage": 5 }
  ],

  "useLeitner": true,

  "pointsMinutesMet": 10, "pointsTestPassed": 20, "pointsDayCompleteBonus": 10,
  "comboThreshold": 5, "comboBonusPoints": 5,
  "newContentPoints": 10, "speedThresholdSeconds": 8, "speedBonusPoints": 3
}
→ 201 PlanResponse { id, childId, method, items:[…], … alle Regeln … }
```

### Alle Felder (`CreatePlanDto`) und ihre Defaults

| Feld | Default | Bedeutung |
| --- | --- | --- |
| `childId` *(req)* | — | Eigenes Kind (sonst 404). |
| `title` *(req)* | — | Anzeigename. |
| `method` | `Vocabulary` | Lernverfahren (nach dem Anlegen **nicht** änderbar). |
| `subjectId` | null | Katalog-Fach koppeln → Stundenplan-Steuerung (§3). |
| `newItemsPerLesson` | 5 | Wie viele neue Inhalte an einem Unterrichtstag eingeführt werden. |
| `startDate` | heute (UTC) | Beginn. |
| `durationDays` | 10 | Laufzeit; `EndDate = start + duration − 1`. |
| `contentKeys` | – | Store-Keys der Inhalte (Reihenfolge = `order`). |
| `dailyMinutesRequired` | 20 | Pflicht-Übungszeit/Tag (Minuten). |
| `dailyTestPassPercent` | 80 | Bestehensgrenze des Abschlusstests. |
| `defaultStage` | verfahrensabh. | Stufe, wenn Fahrplan/Angabe fehlen (Vocab 2, Cloze 2, Match 1). |
| `requireTypedTest` | false | Test/Review zählt nur getippt (Anti-Schummel). |
| `stageSchedule` | null | Stufen-Fahrplan `{dayNumber, stage}` (Schwierigkeits-Rampe). |
| `useLeitner` | false | Karteikasten-Terminierung aktivieren (§4). |
| `maxBox` | 5 | Höchste Leitner-Box. |
| `boxIntervalDays` | `[0,1,2,4,7,14]` | Intervall je Box (Index = Box). |
| `pointsMinutesMet` | 10 | Punkte fürs Erreichen der Tages-Übungszeit. |
| `pointsTestPassed` | 20 | Punkte fürs Bestehen des Tests. |
| `pointsDayCompleteBonus` | 10 | Bonus, wenn Zeit **und** Test an einem Tag erfüllt. |
| `comboThreshold` | 5 | Alle N Treffer in Folge → Combo-Bonus (0 = aus). |
| `comboBonusPoints` | 5 | Basis-Bonus je Combo-Meilenstein (eskaliert). |
| `newContentPoints` | 10 | Basispunkte für einen erstmals geübten Inhalt. |
| `speedThresholdSeconds` | 0 | Schnelle-Antwort-Fenster in s (0 = aus). |
| `speedBonusPoints` | 0 | Bonus für schnelle Antwort. |

Punkte-/Bonus-Details: [05 · Punkte & Bonus](05-punkte-und-bonus.md).

### Nachträglich ändern

```http
PATCH  /api/v1/study-plans/{id}        // partiell; jedes Feld optional. Method NICHT änderbar.
                                       // zusätzlich: dailyTestRequired, active
POST   /api/v1/study-plans/{id}/items  { "contentKey": "fr_au_revoir_de_tschuess" }
DELETE /api/v1/study-plans/{id}/items/{itemId}
```

---

## 3. Stufen pro Verfahren

Ohne `stageSchedule` gilt `defaultStage`; mit Fahrplan gilt „letzter passender Schritt bis heute".
`GET …/today.recommendedStage` zeigt die für heute geltende Stufe.

**Vocabulary** (`TestStage`)

| Stufe | Name | Ablauf | gewertet? |
| --- | --- | --- | --- |
| 1 | ShowBoth | Vokabel + Übersetzung zeigen | Anzeige |
| 2 | SelfAssess | aufdecken → „gewusst? Ja/Nein" | Selbsteinschätzung |
| 3 | LetterBoxes | Übersetzung tippen (Länge bekannt, Buchstaben-Tipps) | ✅ getippt |
| 4 | FreeText | Übersetzung frei tippen | ✅ getippt |
| 5 | Audio | Vokabel vorlesen → frei tippen | ✅ getippt |

**Cloze** (`ClozeStage`)

| Stufe | Name | Übersetzung? | Wortpool? | gewertet? |
| --- | --- | --- | --- | --- |
| 1 | WordBank | – | ✓ | – |
| 2 | TranslationWordBank | ✓ | ✓ | – |
| 3 | TranslationFreeText | ✓ | – | ✅ |
| 4 | FreeText | – | – | ✅ |

**Matching** (`MatchStage`, immer objektiv/gewertet)

| Stufe | Name | Richtung | Ablenker? |
| --- | --- | --- | --- |
| 1 | Direct | Wort → Übersetzung | – |
| 2 | Distractors | Wort → Übersetzung | ✓ |
| 3 | Reverse | Übersetzung → Wort | – |
| 4 | ReverseDistractors | Übersetzung → Wort | ✓ |

> Mit `requireTypedTest=true` zählen nur die **gewerteten** Stufen als bestandener Test — bloßes
> „gewusst"-Klicken bringt dann nichts.

---

## 4. Leitner-Karteikasten

`useLeitner=true` schaltet echte Karteikasten-Terminierung ein:

- Jede Karte hat eine **Box** (1 = neu/schwer … `maxBox` = sicher) und eine **Fälligkeit** (`dueOn`).
- **Richtig** → eine Box höher, nächste Fälligkeit = heute + `boxIntervalDays[neueBox]`.
- **Falsch** → zurück in Box 1, sofort wieder fällig.
- Die tägliche Wiederholung zieht nur die **fälligen** Karten (sortiert nach Box/Fälligkeit).
- Box/Fälligkeit sind **pro Kind** (ein Plan gehört einem Kind).

Ohne Leitner werden Übungs-Reviews nur protokolliert (kein Box-Aufstieg, keine Review-Punkte —
`POST …/review` liefert dann `204`). Punkte gibt es dann über Zeit-/Test-/Tagesbonus.

---

## 5. Nach Stundenplan lernen (neuer Stoff vs. Wiederholung)

Damit der Plan sich am Schul-Stundenplan ausrichtet:

1. **Plan mit Fach koppeln** — beim Anlegen `"subjectId": <Fach>` und `"newItemsPerLesson": 5`.
2. **Stundenplan pflegen** (Kind × Fach × Wochentag):

```http
POST /api/v1/children/{childId}/timetable
{ "subjectId": 1, "dayOfWeek": "Tuesday", "timeOfDay": "Nachmittag" }

GET  /api/v1/children/{childId}/timetable
```

**Wirkung (automatisch):** Am **Unterrichtstag** (Di) führt der Test die nächsten
`newItemsPerLesson` **neuen** Inhalte ein; an allen anderen Tagen wird der bereits eingeführte Stoff
**wiederholt** (der Tag davor ist der Vorbereitungstag). Tests und Übungskarten ziehen automatisch den
passenden Tages-Pool. Ohne Fach/Stundenplan verhält sich der Plan wie bisher (alle Inhalte).

`GET …/today` liefert dann `mode` (`New`/`Review`), `isPreparationDay`, `scheduleReason` und `dueItems`.

---

## 6. Kontrolle & Auswertung

```http
GET /api/v1/study-plans/{id}/today
  → { dutyDone, recommendedStage, mode, isPreparationDay, currentStreak,
      progress:{ minutesPracticed, minutesMet, bestScorePercent, testPassed, dayComplete, pointsAwarded, outstanding[] },
      dueItems[], weakItems[] }

GET /api/v1/study-plans/{id}/progress
  → { daysComplete, totalDays, totalPoints, currentStreak, days:[ DayProgress … ] }

GET /api/v1/study-plans/{id}/report
  → { items:[ ItemStat{ ref,label,timesReviewed,reviewCorrect,timesTested,testCorrect,masteryPercent,box,dueOn } ],
      testHistory:[…], ratings:[…] }     // ratings = Inhalts-Feedback des Sohns

GET /api/v1/children/{childId}/points
  → { balance, entries:[ { amount, kind, reason, createdAt } ] }   // kind = PointKind (Base/Combo/…)
```

Manuelle Punkte (Belohnung/Einlösung; positiv = gutschreiben, negativ = abziehen):

```http
POST /api/v1/children/{childId}/points   { "amount": 30, "reason": "Extra fürs Dranbleiben" }
```

Nur der **Vater** darf einen Tag **nachtragen** (`day` im Practice/Test-Start ≠ heute).

---

## 7. Vollständiges Beispiel (kopierfertig)

Ziel: *Sohn (Kind 1) schreibt in 10 Tagen einen Französisch-Vokabeltest; 20 min/Tag, getippt, ≥80 %,
Schwierigkeit steigt, Leitner an, kleiner Grammatik-Motivations-Bonus.*

```http
# 1) Login
POST /api/v1/auth/father   { "fatherId": 1, "pin": "0000" }

# 2) Inhalte (Auszug – so für alle Vokabeln)
POST /api/v1/learn/vocabulary  { "key":"fr_bonjour_de_hallo","sourceLanguage":"fr","targetLanguage":"de","word":"bonjour","translation":"hallo","partOfSpeech":"Interjection" }
POST /api/v1/learn/vocabulary  { "key":"fr_merci_de_danke","sourceLanguage":"fr","targetLanguage":"de","word":"merci","translation":"danke","partOfSpeech":"Interjection" }
# … weitere …

# 3) Plan
POST /api/v1/study-plans
{
  "childId": 1,
  "title": "Französisch – Vokabeltest in 10 Tagen",
  "method": "Vocabulary",
  "durationDays": 10,
  "contentKeys": ["fr_bonjour_de_hallo","fr_merci_de_danke","fr_oui_de_ja","fr_non_de_nein","fr_au_revoir_de_tschuess"],
  "dailyMinutesRequired": 20,
  "dailyTestPassPercent": 80,
  "requireTypedTest": true,
  "stageSchedule": [ {"dayNumber":1,"stage":1},{"dayNumber":3,"stage":2},{"dayNumber":5,"stage":3},{"dayNumber":7,"stage":4},{"dayNumber":9,"stage":5} ],
  "useLeitner": true,
  "comboThreshold": 5, "comboBonusPoints": 5,
  "speedThresholdSeconds": 8, "speedBonusPoints": 3,
  "newContentPoints": 12
}
→ 201 { "id": 42, … }

# 4) Kontrolle
GET /api/v1/study-plans/42/today
GET /api/v1/children/1/points
```

Der Sohn kann sofort loslegen → **[06 · Sohn-App](06-sohn-app.md)**.

---

## 8. Abkürzung: Matching-Übung → fertiger Leitner-Plan

Hast du bereits eine **Matching-Katalogübung** (z. B. „Bundesland → Hauptstadt"), sparst du dir Store
und Plan-Aufbau:

```http
POST /api/v1/learn/subjects/{s}/chapters/{c}/matching/{exerciseId}/to-study-plan
{ "childId": 1, "title": "Bundesländer üben", "durationDays": 14 }
→ 201 { "planId": 43, "itemCount": 16 }
```

Erzeugt Vokabeln (je Paar) im Store **und** einen Plan (`Method=Matching`, `UseLeitner=true`), ans Fach
gekoppelt. Danach ganz normaler Study-Plan.

---

## 9. Missionen & Auszeichnungen (Motivation je Kind)

Zusätzlich zum laufenden Punktesystem definierst du pro Kind **Missionen** (Tages-/Wochenziele) und
**Auszeichnungen** (Badges). Sie messen serverseitige Metriken und belohnen idempotent — voll erklärt
in [05 · Punkte & Bonus §5](05-punkte-und-bonus.md#5-missionen--auszeichnungen).

```http
POST /api/v1/children/1/missions
{ "title": "Tagesziel: 10 richtige Antworten", "metric": "CorrectReviews", "target": 10, "period": "Daily", "rewardPoints": 15 }

POST /api/v1/children/1/achievements
{ "title": "Feuer-Streak", "icon": "🔥", "metric": "StreakDays", "threshold": 7, "rewardPoints": 70 }
```
