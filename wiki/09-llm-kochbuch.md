# 09 · LLM-Kochbuch: Lernplan aus einem Prompt

← [Zurück zum Wiki-Index](../README.md)

Diese Seite ist ein Rezept für eine AI/LLM, die wie ein Vater aus einem natürlichsprachlichen Auftrag
einen trainierbaren Lernplan über die API baut. Der aktuelle Kern ist: **Plan-Container + Positionen**.
Ein `StudyPlan` gehört einem Kind; jede `PlanPosition` verweist auf eine Katalog-Übung.

**Ziel-Prompt-Beispiel:**
> „Erstelle einen Lernplan für die 9. Klasse in den Fächern Französisch, Englisch und Mathe. Es soll
> zwischen Mo–Fr jeweils 10 Minuten pro Fach gelernt werden."

Voraussetzung: Lies vorher [01 · Architektur](01-ueberblick-architektur.md),
[04 · Lernplan bauen](04-lernplan-bauen.md) und [05 · Punkte](05-punkte-und-bonus.md).

---

## 1. Prompt → Parameter

| Größe | Aus dem Prompt | Standard, wenn unklar |
| --- | --- | --- |
| **Kind** | „mein Sohn", Name | Kind `id=1` (Seed) bzw. einziges Kind des Vaters |
| **Fächer** | „Französisch, Englisch, Mathe" | Pflichtangabe |
| **Klassenstufe** | „9. Klasse" | `grade=9` für die Übungssuche |
| **Schulart** | „Gymnasium" | Filter weglassen, wenn ungenannt |
| **Lernmenge** | „10 Minuten", „20 Vokabeln" | In Positionsziele übersetzen: `cadence`, `itemCount`, `goalThreshold` |
| **Lerntage** | „Mo–Fr" | Es gibt keine harte Wochentagsregel; als Planlaufzeit + Vater-Erwartung dokumentieren |
| **Laufzeit** | „in 10 Tagen", Datum | 14 Tage oder bis zur Klassenarbeit |
| **Übungstyp** | Thema/Fach | Sprachen → `Vocabulary`/`Cloze`; Mathe → `Arithmetic`/`ArithmeticDrill`/`Matching` |

Mehrere Fächer können ein Plan mit mehreren Positionen sein oder mehrere Pläne. Praktisch ist meist:
**ein Plan pro Kind und Lernphase**, darin mehrere fachlich sortierte Positionen. Wenn der Sohn nur einen
aktiven Plan sehen soll, ist das auch serverseitig die sauberste Form.

---

## 2. Algorithmus

```text
1. Als Vater einloggen (POST /auth/father).
2. Kind bestimmen (GET /children → gemeintes Kind wählen oder anlegen).
3. Für jedes Fach/Thema:
   a. Fach/Kapitel im Katalog finden oder anlegen.
   b. Passende Übungen suchen: GET /learn/exercises?subjectId=&grade=&schoolType=&type=
   c. Falls keine gute Übung existiert: neue Katalog-Übung mit typisierter Config anlegen.
4. Study-Plan-Container anlegen: POST /study-plans.
5. Jede ausgewählte Übung als Position anhängen: POST /study-plans/{planId}/positions.
6. Missionen/Auszeichnungen fürs Kind setzen, wenn sie noch fehlen.
7. Verifizieren: GET /study-plans/{planId}/positions und GET /study-plans/{planId}/overview.
```

**Idempotenz:** Nutze stabile Fach-/Kapitel-/Übungstitel und prüfe vorhandene Übungen zuerst über die
Katalogsuche. Bei Vokabeln zuerst den Store pflegen (`lookup`, `batch`, `by-key`) und danach die
Übungsmenge über `ExerciseItem`s bzw. `refs-from-tags` materialisieren. `config.items` beim Übungs-POST
ist nur Authoring-Payload; die dauerhafte Item-Liste liegt unter `.../vocabulary/{exerciseId}/items`.

---

## 3. Inhalte beschaffen

**Weg 1 — Vorhandenes wiederverwenden:**

```http
GET /api/v1/learn/exercises?subjectId=1&grade=9&schoolType=Gymnasium&type=Vocabulary
```

Wenn eine Übung passt, nutze ihre `id` direkt als `exerciseId` in der Position.

**Weg 2 — Neue Katalog-Übung anlegen:**

```http
POST /api/v1/learn/subjects/{subjectId}/chapters/{chapterId}/vocabulary
{ "title":"Unit 1 – Begrüßungen", "orderIndex":1, "rewardPoints":10,
  "config": { "direction":"front-to-back", "sourceLang":"en", "targetLang":"de",
    "items":[{"front":"hello","back":"hallo"},{"front":"goodbye","back":"tschüss"}] },
  "defaultStage":2, "defaultUseLeitner":true, "defaultRequireTypedTest":true,
  "suggestedBonus": { "comboThreshold":5, "comboBonusPoints":5, "speedThresholdSeconds":8,
    "speedBonusPoints":3, "newContentPoints":10 } }
```

  Für größere Vokabelmengen ist robuster: Store-Einträge per `POST /learn/vocabulary/batch` anlegen,
  taggen und anschließend die Übung mit `POST .../vocabulary/{exerciseId}/refs-from-tags` auf den Tag-Snapshot
  setzen. Dadurch kann dieselbe Store-Vokabel in mehreren Übungen auftauchen und der Lernstand später per
  `/children/{childId}/vocabulary-progress/by-word` zusammengeführt werden.

Die Qualität der Übungs-Config ist entscheidend. Lieber wenige gute Items mit passenden Hinweisen,
Alternativen und Stufen als viel Füllmaterial.

---

## 4. „Mo–Fr" korrekt abbilden

Der Server kennt derzeit keine harte „nur Mo–Fr zählt"-Regel. Bilde solche Wünsche als Erwartung in
der Planbeschreibung und über Positionen ab:

- Laufzeit setzen (`durationDays`) und in `description` notieren: „Schultage Mo–Fr, 10 Minuten/Fach".
- Pro Fach/Thema eine `Daily`-Position mit passender Item-Menge oder Testschwelle anlegen.
- Wochenziele mit `cadence=Weekly` nutzen, wenn ein Thema nicht täglich dran sein soll.
- Missionen (`MinutesPracticed`, `CorrectReviews`, `TestsPassed`) setzen, wenn du zusätzliche Motivation
  über Zeit oder Anzahl brauchst.

---

## 5. Beispiel: Französisch, Englisch, Mathe

```http
### Login
POST /api/v1/auth/father   { "fatherId": 1, "pin": "0000" }

### Plan-Container
POST /api/v1/study-plans
{ "childId":1, "title":"9. Klasse – Wochenplan", "durationDays":14,
  "description":"Mo–Fr je Fach ca. 10 Minuten; täglich die fälligen Positionen erledigen." }
→ planId

### Französisch-Position
POST /api/v1/study-plans/{planId}/positions
{ "exerciseId": 101, "order":0, "cadence":"Daily", "useLeitner":true,
  "requireTypedTest":true, "itemCount":10, "goalThreshold":80,
  "stageSchedule":[{"dayNumber":1,"stage":2},{"dayNumber":5,"stage":3},{"dayNumber":9,"stage":4}],
  "comboThreshold":5, "comboBonusPoints":5, "speedThresholdSeconds":8, "speedBonusPoints":3 }

### Englisch-Position
POST /api/v1/study-plans/{planId}/positions
{ "exerciseId": 102, "order":1, "cadence":"Daily", "useLeitner":true,
  "requireTypedTest":true, "itemCount":10, "goalThreshold":80 }

### Mathe-Position
POST /api/v1/study-plans/{planId}/positions
{ "exerciseId": 201, "order":2, "cadence":"Daily", "stage":1,
  "itemCount":12, "goalThreshold":10, "pointsGoalMet":20 }

### Motivation einmal fürs Kind
POST /api/v1/children/1/missions
{ "title":"Tagesziel: 15 richtige Antworten", "metric":"CorrectReviews", "target":15,
  "period":"Daily", "rewardPoints":10 }

POST /api/v1/children/1/achievements
{ "title":"Feuer-Streak", "icon":"🔥", "metric":"StreakDays", "threshold":7, "rewardPoints":70 }

### Kontrolle
GET /api/v1/study-plans/{planId}/positions
GET /api/v1/study-plans/{planId}/overview
```

Der Sohn startet über `GET /study-plans`, wählt den spielbaren Plan und arbeitet die Positionen aus
`overview` bzw. der Positionsliste ab. Details: [06 · Sohn-App](06-sohn-app.md).

---

## 6. Gute Standards

| Entscheidung | Vernünftiger Default | Warum |
| --- | --- | --- |
| Sprachen | `Vocabulary` oder `Cloze`, `requireTypedTest=true` | echtes Wissen statt bloßem Klick |
| Faktenpaare | `Matching`-Übung als Position | einfach prüfbar und leitnerfähig |
| Rechnen | `Arithmetic` oder `ArithmeticDrill` | serverseitig prüfbar/generierbar |
| Leitner | `useLeitner=true` bei drillfähigen Positionen | Fälligkeit/Boxen statt blindem Wiederholen |
| Bestehensgrenze | `goalThreshold=80` bei Tests | fair und fordernd |
| Stufen-Fahrplan | leichte Stufe → getippt/frei über die Laufzeit | steigende Härte ohne Fruststart |
| Combo/Speed | moderat aktivieren | Motivation ohne Hauptwährung aufzublähen |

---

## 7. Fallstricke für Agenten

- **Plan ist Container:** Lernregeln gehören an `PlanPosition`, nicht an `StudyPlan`.
- **Kein `method/contentKeys` mehr:** Positionen referenzieren Katalog-Übungen per `exerciseId`.
- **Kein `to-study-plan`:** Matching wird wie jede andere Übung als Position angehängt.
- **Nur ein aktiver Plan je Kind:** Ein neuer aktiver Plan deaktiviert ältere aktive Pläne desselben Kindes.
- **Enums als String:** z. B. `"cadence":"Daily"`, `"scope":"All"`, `"metric":"CorrectReviews"`.
- **„Mo–Fr" nicht als harte Serverregel erfinden:** siehe §4.
- **Nach dem Bauen verifizieren:** `GET /positions`, `GET /overview`, bei Bedarf `GET /overview/progress`.
