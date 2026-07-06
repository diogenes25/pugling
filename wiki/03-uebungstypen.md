# 03 · Übungstypen (Lern-Katalog)

← [Zurück zum Wiki-Index](../README.md)

Der **Katalog** ist die globale Übungs-Bibliothek: `Subject → Chapter → Exercise`. Es gibt **12
Übungstypen**, jeder mit eigener typisierter Config und eigenem Swagger-Schema. Diese Seite listet
alle Typen mit Config-Schema und Beispiel-Requests.

> **Rolle:** Katalog-CRUD ist **Vater-only** (`[Authorize(Roles = Vater)]`). Alle Beispiele setzen den
> Vater-Bearer-Token voraus.
>
> **Abgrenzung:** Katalog-Übungen sind **nicht** dasselbe wie das Study-Plan-Training. Sie sind eine
> kindneutrale Bibliothek mit Metadaten (für Suche/Vorfilterung). Ein Study-Plan verweist über
> `PlanPosition` auf Katalog-Übungen; dafür nutzt der Vater `POST /api/v1/study-plans/{planId}/positions`.
> Siehe [01 · Architektur](01-ueberblick-architektur.md#2-die-zwei-api-welten).

---

## 1. Fach & Kapitel anlegen (Voraussetzung)

Jede Übung lebt in einem Kapitel eines Fachs.

```http
POST /api/v1/learn/subjects                 { "name": "Französisch" }
→ { "id": 4, "name": "Französisch", "chaptersCount": 0 }

POST /api/v1/learn/subjects/4/chapters      { "name": "Unité 1 – Salutations", "orderIndex": 1 }
→ { "id": 12, "subjectId": 4, "name": "…", "orderIndex": 1, "exercisesCount": 0 }
```

Optional pro Fach **fachabhängige Arten** (kontrolliertes Vokabular für die Vorfilterung):

```http
POST /api/v1/learn/subjects/4/categories    { "name": "Vokabeln" }
→ { "id": 7, "subjectId": 4, "name": "Vokabeln" }
```

---

## 2. Gemeinsames CRUD aller Übungstypen

Jeder Typ erbt dasselbe CRUD aus `ExerciseControllerBase<TConfig>`. Route-Präfix:

```text
api/v1/learn/subjects/{subjectId}/chapters/{chapterId}/<typ-pfad>
```

| Methode | Route | Zweck |
| --- | --- | --- |
| `GET` | `…/<typ>` | Übungen dieses Typs im Kapitel |
| `GET` | `…/<typ>/{exerciseId}` | Eine Übung |
| `POST` | `…/<typ>` | Anlegen |
| `PUT` | `…/<typ>/{exerciseId}` | Vollständig ersetzen (inkl. Config) |
| `DELETE`| `…/<typ>/{exerciseId}` | Löschen |

**Request-Body beim Anlegen/Ersetzen** (`ExercisePayload<TConfig>`) — die gemeinsamen Felder sind für
**alle** Typen gleich, nur `config` ist typ-spezifisch:

```jsonc
{
  "title": "Begrüßungen",
  "orderIndex": 1,
  "rewardPoints": 10,
  "config": { /* typ-spezifisch, siehe unten */ },

  // --- alles ab hier optional ---
  "suggestedBonus": {                 // Bonus-Vorlage, wird beim Anlegen einer PlanPosition übernommen
    "comboThreshold": 3, "comboBonusPoints": 5,
    "speedThresholdSeconds": 8, "speedBonusPoints": 3, "newContentPoints": 12
  },
  "gradeMin": 5, "gradeMax": 7,       // geeignete Klassenstufe (inkl.); null = keine Grenze
  "schoolTypes": "Realschule, Gymnasium",  // [Flags]; "None" = für alle. Kombinationen kommasepariert angeben
  "source": "Green Line 1, Unit 1",   // Quelle (Schulbuch o. Ä.)
  "categoryId": 7                     // fachabhängige Art (muss zum Fach gehören)
}
```

**Metadaten** (`gradeMin/gradeMax/schoolTypes/source/categoryId`) sind das Fundament der
fachübergreifenden Übungssuche → [Abschnitt 5](#5-übungssuche-vorfilterung-für-den-auto-generator).

**Enums werden als String übertragen** (`JsonStringEnumConverter`): `"schoolTypes": "Gymnasium"`,
Flag-Kombis als `"Realschule, Gymnasium"`.

---

## 3. Die 12 Übungstypen im Detail

Legende: **/check** = hat einen Auswertungs-Endpunkt · **gen** = erzeugt Aufgaben on demand.

| Typ | Pfad | Config | Extra |
| --- | --- | --- | --- |
| Vocabulary | `/vocabulary` | `VocabularyConfig` | — |
| Reading | `/reading` | `ReadingConfig` | — |
| Cloze | `/cloze` | `ClozeConfig` | — |
| Essay | `/essays` | `EssayConfig` | — |
| Listening | `/listening` | `ListeningConfig` | — |
| Grammar | `/grammar` | `GrammarConfig` | — |
| Matching | `/matching` | `MatchingConfig` | **/check** |
| Translation | `/translation` | `TranslationConfig` | — |
| Arithmetic | `/arithmetic` | `ArithmeticConfig` | **/check** |
| ArithmeticDrill | `/arithmetic-drill` | `ArithmeticDrillConfig` | **/generate** (gen), **/check** |
| List | `/list` | `ListConfig` | **/check** |
| Birkenbihl | `/birkenbihl` | `BirkenbihlConfig` | — (reine Inhaltsübung, kein Abfragen) |

### 3.1 Vocabulary — Vokabelübung

```jsonc
"config": {
  "direction": "front-to-back",     // front-to-back | back-to-front | both
  "items": [
    { "front": "hello", "back": "hallo" },
    { "front": "please", "back": "bitte", "hint": "Höflichkeit" }
  ]
}
```

### 3.2 Reading — Leseverständnis

```jsonc
"config": {
  "text": "Tom lives in London. He has a dog.",
  "questions": [
    { "prompt": "Where does Tom live?", "choices": ["London","Paris"], "answer": "London" },
    { "prompt": "What pet does Tom have?", "choices": null, "answer": "a dog" }  // choices null = Freitext
  ]
}
```

### 3.3 Cloze — Lückentext (Katalog)

`{{1}}`, `{{2}}` … sind die Lücken. `gaps[].index` verweist darauf.

```jsonc
"config": {
  "text": "A: {{1}}, how are you? B: I'm {{2}}, thank you.",
  "gaps": [
    { "index": 1, "answer": "Hello", "alternatives": ["Hi"] },
    { "index": 2, "answer": "fine",  "alternatives": ["good","well"] }
  ],
  "wordBank": ["Hello","Hi","fine","good","well"]   // optional
}
```

### 3.4 Essay — Aufsatz

```jsonc
"config": {
  "prompt": "Schreibe über deine Ferien.",
  "minWords": 80, "maxWords": 150,
  "rubric": [ { "criterion": "Wortschatz", "maxScore": 5 }, { "criterion": "Grammatik", "maxScore": 5 } ]
}
```

### 3.5 Listening — Hörverständnis

```jsonc
"config": {
  "audioUrl": "https://cdn/…/dialog1.mp3",
  "transcript": "…",                 // optional
  "questions": [ { "prompt": "Who is speaking?", "choices": null, "answer": "the teacher" } ]
}
```

### 3.6 Grammar — Grammatik

```jsonc
"config": {
  "instruction": "Setze das Verb ins Simple Past.",
  "tasks": [ { "prompt": "I (go) to school.", "answer": "went", "ruleHint": "unregelmäßig" } ]
}
```

### 3.7 Matching — Zuordnung (Paare) · /check

```jsonc
"config": {
  "instruction": "Ordne jedem Bundesland seine Hauptstadt zu.",
  "pairs": [ { "left": "Bayern", "right": "München" }, { "left": "Hessen", "right": "Wiesbaden" } ]
}
```

**Auswerten** — positionsbezogen (Index = Position in `pairs`):

```http
POST …/matching/{exerciseId}/check
{ "answers": [ { "index": 0, "value": "München" }, { "index": 1, "value": "Wiesbaden" } ] }
→ CheckResult { total, correct, scorePercent, details[…] }
```

**Als Study-Plan trainieren:** Lege zuerst einen Plan-Container an und füge die Matching-Übung dann als
Position hinzu. `useLeitner`, `stage`, Zielrhythmus und Bonuswerte hängen an der Position:

```http
POST /api/v1/study-plans
{ "childId": 1, "title": "Bundesländer üben", "durationDays": 14 }

POST /api/v1/study-plans/{planId}/positions
{ "exerciseId": 42, "useLeitner": true, "stage": 1, "cadence": "Daily" }
```

### 3.8 Translation — Übersetzung

```jsonc
"config": {
  "sourceLang": "de", "targetLang": "en",
  "items": [ { "source": "Guten Morgen", "target": "Good morning", "alternatives": ["Morning"] } ]
}
```

### 3.9 Arithmetic — feste Rechenaufgaben · /check

Manuell gepflegte Liste. `tolerance` erlaubt Rundungsspielraum (0 = exakt).

```jsonc
"config": {
  "problems": [
    { "prompt": "7 × 6", "answer": 42 },
    { "prompt": "20 ÷ 3", "answer": 6.67, "tolerance": 0.01 }
  ]
}
```

```http
POST …/arithmetic/{exerciseId}/check
{ "answers": [ { "index": 0, "value": "42" }, { "index": 1, "value": "6.67" } ] }
```

### 3.10 ArithmeticDrill — Zufalls-Rechenaufgaben · /generate · /check

Gespeichert werden nur die **Regeln**; die konkreten Aufgaben erzeugt der Server aus einem **Seed**
(reproduzierbar). Ablauf: erst `generate` (liefert Aufgaben + Seed), dann `check` mit demselben Seed.

```jsonc
"config": {
  "operations": ["Addition","Subtraction"],   // Addition|Subtraction|Multiplication|Division
  "minOperand": 1, "maxOperand": 20,
  "problemCount": 10,
  "allowNegativeResults": false,
  "divisionMustBeWhole": true,
  "seed": null                                 // fester Seed = reproduzierbar; null = echter Zufall
}
```

```http
POST …/arithmetic-drill/{exerciseId}/generate
→ { "exerciseId": 9, "title": "Kopfrechnen bis 20", "seed": 123456, "problems": [ { "prompt": "7 + 8", … } ] }

POST …/arithmetic-drill/{exerciseId}/check
{ "seed": 123456, "answers": [ { "index": 0, "value": "15" } ] }
```

### 3.11 List — auswendig zu lernende Liste · /check

```jsonc
"config": {
  "instruction": "Nenne alle 16 Bundesländer.",
  "ordered": false,                            // true = Reihenfolge zählt
  "items": [ { "value": "Bayern" }, { "value": "Hessen", "alternatives": ["Land Hessen"] } ]
}
```

```http
POST …/list/{exerciseId}/check
{ "answers": ["Bayern","Hessen","…"] }         // genannte Einträge (bei ordered: in Reihenfolge)
```

### 3.12 Birkenbihl — Wort-für-Wort-Dekodierung (kein /check)

Bewusst **ohne** aktives Abfragen: gelernt wird durch Lesen/Hören der Dekodierung. Punkte gibt es fürs
Durcharbeiten.

```jsonc
"config": {
  "learningLang": "Englisch", "nativeLang": "Deutsch",
  "sentences": [
    {
      "text": "What is your name?",
      "decoding": [
        { "word": "What", "literal": "Was" }, { "word": "is", "literal": "ist" },
        { "word": "your", "literal": "dein" }, { "word": "name", "literal": "Name" }
      ],
      "naturalTranslation": "Wie heißt du?"
    }
  ]
}
```

---

## 4. Vollständiges Beispiel: Grammatik-Übung anlegen

```http
POST /api/v1/learn/subjects/1/chapters/1/grammar
Authorization: Bearer <VATER-TOKEN>
Content-Type: application/json

{
  "title": "Simple Past – unregelmäßige Verben",
  "orderIndex": 3,
  "rewardPoints": 15,
  "gradeMin": 6, "gradeMax": 8,
  "schoolTypes": "Realschule, Gymnasium",
  "source": "Green Line 2, Unit 3",
  "categoryId": 2,
  "config": {
    "instruction": "Setze das Verb ins Simple Past.",
    "tasks": [
      { "prompt": "I (go) home.",  "answer": "went",  "ruleHint": "go→went" },
      { "prompt": "She (see) it.", "answer": "saw",   "ruleHint": "see→saw" }
    ]
  }
}
→ 201 ExerciseResponse { id, chapterId, type:"Grammar", title, config, gradeMin, …, categoryName }
```

---

## 5. Übungssuche (Vorfilterung für den Auto-Generator)

Über die Metadaten lassen sich Übungskandidaten fachübergreifend finden — die Grundlage für die
**automatische Lehrplan-Erstellung** ([09 · LLM-Kochbuch](09-llm-kochbuch.md)):

```http
GET /api/v1/learn/exercises?subjectId=1&grade=9&schoolType=Gymnasium&categoryId=2&type=Grammar&search=Past
```

Alle Parameter sind optional und werden **UND-verknüpft**. Nullbare Grenzen und `schoolType=None`
bedeuten „passt immer". Antwort: schlanke `ExerciseSummary`-Zeilen (`id, chapterId, subjectId, type,
title, gradeMin, gradeMax, schoolTypes, source, categoryId, categoryName`).

---

## 6. `CheckResult` (Auswertungs-Antwort)

Die `/check`-Endpunkte (Matching, Arithmetic, ArithmeticDrill, List) liefern ein einheitliches
Ergebnis mit Gesamt-/Trefferzahl, Prozent und Detail je Position. Genaue Feldnamen im Swagger-Schema
(`CheckResult`) bzw. in [Services/ExerciseAnswerChecker.cs](../backend/Pugling.Api/Services/ExerciseAnswerChecker.cs).

---

Weiter: **[04 · Einen Lernplan bauen](04-lernplan-bauen.md)** — wie aus Inhalten ein trainierbarer
Study-Plan wird.
