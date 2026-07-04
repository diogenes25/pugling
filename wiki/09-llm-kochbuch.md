# 09 · LLM-Kochbuch: Lernplan aus einem Prompt

← [Zurück zum Wiki-Index](../README.md)

Diese Seite ist ein **Rezept für eine AI/LLM**, die sich wie ein Vater verhalten und aus einem
natürlichsprachlichen Auftrag einen **fertigen, trainierbaren Lernplan über die API** bauen soll —
ohne Rückfragen, mit sinnvollen Standards.

**Ziel-Prompt-Beispiel:**
> „Erstelle einen Lernplan für die 9. Klasse in den Fächern Französisch, Englisch und Mathe. Es soll
> zwischen Mo–Fr jeweils 10 Minuten pro Fach gelernt werden."

Voraussetzung: Lies vorher [01 · Architektur](01-ueberblick-architektur.md), [04 · Lernplan bauen](04-lernplan-bauen.md)
und [05 · Punkte](05-punkte-und-bonus.md). Die Kernregel: **ein `StudyPlan` = ein Kind × ein Fach ×
ein Verfahren.** Mehrere Fächer → mehrere Pläne.

---

## 1. Prompt → Parameter (Extraktion)

Ziehe aus dem Auftrag diese Größen; fehlt etwas, setze den Standard:

| Größe | Aus dem Prompt | Standard, wenn unklar |
| --- | --- | --- |
| **Kind** | „mein Sohn", Name | Kind `id=1` (Seed) bzw. das einzige Kind des Vaters |
| **Fächer** | „Französisch, Englisch, Mathe" | — (Pflichtangabe) |
| **Klassenstufe** | „9. Klasse" | → `grade=9` für die Übungssuche |
| **Schulart** | „Gymnasium" | wenn ungenannt: Filter weglassen (`None` passt immer) |
| **Minuten/Tag pro Fach** | „10 Minuten" | 15 |
| **Lerntage** | „Mo–Fr" | siehe §4 (Study-Plan hat keine Wochentags-Minuten) |
| **Laufzeit** | „in 10 Tagen" / bis Datum | 14 Tage (oder bis zur nächsten Klassenarbeit) |
| **Verfahren je Fach** | selten genannt | Sprachen → `Vocabulary`, Mathe → `Matching`/Katalog-Drill (§3) |

Für „9. Klasse, drei Fächer, 10 min" ergibt das: **3 Study-Pläne** für dasselbe Kind,
`dailyMinutesRequired=10`, je Fach eins.

---

## 2. Der Algorithmus (Pseudocode)

```text
1. Als Vater einloggen (POST /auth/father).
2. Kind bestimmen (GET /children → nimm das gemeinte; sonst anlegen).
3. FÜR JEDES Fach:
   a. Fach im Katalog finden/anlegen (GET/POST /learn/subjects).
   b. Passende Inhalte beschaffen (§3):
        - Übungssuche: GET /learn/exercises?subjectId=&grade=9[&schoolType=]
        - existiert eine Matching-Übung → to-study-plan (fertig!)  ODER
        - lege Vokabeln/Lückentexte im Store an und sammle ihre Keys.
   c. Study-Plan anlegen (POST /study-plans) mit:
        method, contentKeys, dailyMinutesRequired=10, dailyTestPassPercent=80,
        useLeitner=true, requireTypedTest=true (Sprachen),
        stageSchedule (Schwierigkeits-Rampe), Bonus-Felder.
   d. (optional) Fach + Stundenplan koppeln (subjectId, POST /timetable) für Mo–Fr-Steuerung.
4. Motivation je Kind: Missionen/Auszeichnungen setzen (einmal pro Kind, nicht pro Plan).
5. Zusammenfassung ausgeben: pro Fach planId + Titel + itemCount.
```

**Idempotenz:** Nutze stabile `key`s (z. B. `fr_bonjour_de_hallo`). Ein erneuter Lauf soll Inhalte
aktualisieren, nicht duplizieren. Prüfe vorher `GET /learn/vocabulary/by-key/{key}`.

---

## 3. Inhalte beschaffen — zwei Wege

**Weg 1 — Vorhandenes wiederverwenden (bevorzugt):** Erst die Übungssuche fragen.

```http
GET /api/v1/learn/exercises?subjectId=1&grade=9&schoolType=Gymnasium
```

- Findet sie eine **Matching**-Übung, die zum Thema passt → direkt in einen Plan gießen:
  `POST /api/v1/learn/subjects/{s}/chapters/{c}/matching/{id}/to-study-plan` (fertig, inkl. Leitner).
- Findet sie andere Typen, kannst du deren Inhalt als Vorlage nehmen, musst aber für Vokabel-/
  Cloze-Training Store-Einträge erzeugen (Weg 2), weil Study-Pläne aus dem **Store** ziehen.

**Weg 2 — Neu anlegen:** Vokabeln bzw. Lückentexte in den Store schreiben ([04 §1](04-lernplan-bauen.md#1-lerninhalte-anlegen-store))
und die `key`s sammeln. Wähle didaktisch sinnvolle, klassenstufengerechte Inhalte (du bist der Autor —
wie der Vater im [Markdown-Lehrplan](../docs/lehrplan-erstellen.md): lieber echte Qualität als Füllmaterial).

---

## 4. „Mo–Fr" korrekt abbilden

Ein `StudyPlan` kennt **keine** wochentagsabhängige Minutenpflicht — er verlangt an **jedem** Tag der
Laufzeit `dailyMinutesRequired`. „Mo–Fr" bildest du so ab:

- **Einfach & empfohlen:** Laufzeit `durationDays` setzen, `dailyMinutesRequired=10`. Der Streak
  bricht am Wochenende, wenn nicht geübt wird — das ist okay; der Vater sieht es im `progress`. Kläre in
  der Zusammenfassung, dass Üben an Schultagen erwartet wird.
- **Mit Stundenplan-Kopplung:** Fach koppeln (`subjectId`) und einen `TimetableEntry` für den
  Unterrichtstag setzen. Dann führt der Unterrichtstag neuen Stoff ein, andere (Schul-)Tage wiederholen —
  fachlich näher am echten Rhythmus. Wochenende bleibt „Wiederholung" bzw. wird ausgelassen.

Eine harte „nur Mo–Fr zählt"-Regel gibt es serverseitig **nicht** — nicht erfinden.

---

## 5. Vollständiger Durchlauf für das Beispiel

*„9. Klasse, Französisch + Englisch + Mathe, 10 min/Fach, Mo–Fr", Kind 1, Laufzeit 14 Tage.*

```http
### 0) Vater-Login
POST /api/v1/auth/father   { "fatherId": 1, "pin": "0000" }      # → token (in allen Calls als Bearer)

### 1) Kind bestätigen
GET  /api/v1/children                                            # → Kind id=1 „Sohn"

### 2) FRANZÖSISCH ────────────────────────────────────────────
# Fach anlegen (falls noch nicht vorhanden)
POST /api/v1/learn/subjects            { "name": "Französisch" }          # → subjectId 4
# Vokabeln in den Store (Auszug – idempotente Keys)
POST /api/v1/learn/vocabulary  { "key":"fr_bonjour_de_hallo","sourceLanguage":"fr","targetLanguage":"de","word":"bonjour","translation":"hallo","partOfSpeech":"Interjection" }
POST /api/v1/learn/vocabulary  { "key":"fr_merci_de_danke","sourceLanguage":"fr","targetLanguage":"de","word":"merci","translation":"danke","partOfSpeech":"Interjection" }
# … weitere klassenstufengerechte Vokabeln …
# Plan
POST /api/v1/study-plans
{
  "childId": 1, "title": "Französisch – 9. Klasse", "method": "Vocabulary",
  "subjectId": 4, "durationDays": 14,
  "contentKeys": ["fr_bonjour_de_hallo","fr_merci_de_danke", "…"],
  "dailyMinutesRequired": 10, "dailyTestPassPercent": 80, "requireTypedTest": true,
  "useLeitner": true,
  "stageSchedule": [ {"dayNumber":1,"stage":2},{"dayNumber":5,"stage":3},{"dayNumber":9,"stage":4} ],
  "comboThreshold": 5, "comboBonusPoints": 5, "speedThresholdSeconds": 8, "speedBonusPoints": 3
}                                                               # → planId (Französisch)

### 3) ENGLISCH ───────────────────────────────────────────────
# Fach existiert im Seed (subjectId 1). Vorhandene Übungen suchen:
GET  /api/v1/learn/exercises?subjectId=1&grade=9
# Vokabeln in den Store + Plan (analog Französisch, method "Vocabulary", subjectId 1, 10 min)
POST /api/v1/learn/vocabulary  { "key":"en_…","sourceLanguage":"en","targetLanguage":"de", … }
POST /api/v1/study-plans       { "childId":1,"title":"Englisch – 9. Klasse","method":"Vocabulary","subjectId":1,"durationDays":14,"contentKeys":["en_…"],"dailyMinutesRequired":10,"dailyTestPassPercent":80,"requireTypedTest":true,"useLeitner":true }

### 4) MATHE ──────────────────────────────────────────────────
# Mathe existiert im Seed (subjectId 2). Wenn eine Matching-Übung passt → direkt gießen:
GET  /api/v1/learn/exercises?subjectId=2&grade=9&type=Matching
POST /api/v1/learn/subjects/2/chapters/{c}/matching/{id}/to-study-plan
     { "childId": 1, "title": "Mathe – 9. Klasse", "durationDays": 14 }   # → planId (Mathe), Leitner an
# (Der Plan erbt 10-min-Pflicht nicht automatisch → danach anpassen:)
PATCH /api/v1/study-plans/{mathePlanId}   { "dailyMinutesRequired": 10 }

### 5) MOTIVATION (einmal fürs Kind)
POST /api/v1/children/1/missions      { "title":"Tagesziel: 15 Minuten üben","metric":"MinutesPracticed","target":15,"period":"Daily","rewardPoints":10 }
POST /api/v1/children/1/achievements  { "title":"Feuer-Streak","icon":"🔥","metric":"StreakDays","threshold":7,"rewardPoints":70 }

### 6) Kontrolle
GET  /api/v1/study-plans?childId=1     # → drei Pläne
```

Ergebnis: **drei Study-Pläne** (Französisch, Englisch, Mathe) für Kind 1, je 10 min/Tag, 14 Tage,
Leitner an, Sprachen getippt, mit Schwierigkeits-Rampe und Motivations-Boni. Der Sohn kann sofort über
`GET /study-plans/{id}/today` loslegen ([06 · Sohn-App](06-sohn-app.md)).

---

## 6. Gute Standards, wenn der Prompt schweigt

| Entscheidung | Vernünftiger Default | Warum |
| --- | --- | --- |
| Verfahren Sprachen | `Vocabulary`, `requireTypedTest=true` | echtes Wissen statt „gewusst"-Klick |
| Verfahren Faktenpaare/Mathe-Reihen | `Matching` via `to-study-plan` | schnellster Weg, Leitner inklusive |
| Leitner | `useLeitner=true` | Fälligkeit/Boxen = echtes Karteikasten-Lernen |
| Bestehensgrenze | `dailyTestPassPercent=80` | fair und fordernd |
| Stufen-Fahrplan | Rampe über die Laufzeit (leicht → getippt → frei) | Motivation + steigende Härte |
| Combo/Speed | an (z. B. 5/5 bzw. 8 s/3) | Motivation ohne Schummel-Risiko |
| Laufzeit | 14 Tage oder bis zur Klassenarbeit | überschaubar |

---

## 7. Fallstricke für den Agenten

- **Store ≠ Katalog:** Study-Pläne ziehen Inhalte aus dem **Store** (per `key`), nicht aus
  Katalog-Übungen. Nur `to-study-plan` (Matching) überbrückt das.
- **`method` ist nach dem Anlegen fix** — richtig wählen.
- **`to-study-plan` setzt eigene Defaults** (14 Tage, Bonus aus `suggestedBonus`); Minuten/Regeln
  danach per `PATCH` anpassen.
- **Ein Plan = ein Kind + ein Fach.** Drei Fächer = drei Pläne. Missionen/Auszeichnungen dagegen
  gehören dem **Kind** (nicht dem Plan) — nur einmal setzen.
- **Enums als String** (`"method":"Vocabulary"`, `"metric":"MinutesPracticed"`, `"period":"Daily"`).
- **Idempotenz über `key`s** — vor dem Anlegen prüfen, um Duplikate zu vermeiden.
- **„Mo–Fr" nicht als harte Serverregel erfinden** — siehe §4.
- Nach dem Bauen **verifizieren** (`GET …/today`, `GET /study-plans?childId=`), nicht blind melden.
