# 01 · Überblick & Architektur

Diese Seite gibt das mentale Modell, das man für alles Weitere braucht. Wer sie versteht, kann sich
in jeder anderen Wiki-Seite schnell orientieren.

← [Zurück zum Wiki-Index](../README.md)

---

## 1. Zwei Rollen, ein Ziel

| Rolle | Claim | Darf | Ziel |
| --- | --- | --- | --- |
| **Vater** (`Roles.Vater`) | `fid` | Katalog & Inhalte pflegen, Study-Pläne/Missionen/Auszeichnungen anlegen, Punkte manuell buchen, alles auswerten | Lernerfolg **erzwingen** und kontrollieren |
| **Sohn** (`Roles.Sohn`) | `cid` + `fid` | nur die **eigenen** Pläne sehen, üben, testen, Inhalte bewerten, eigenen Punktestand lesen | mit Spaß und Punkten **lernen** |

Ein `Father` hat viele `Child`ren. Jedes Study-Plan-, Missions- und Punkte-Objekt gehört genau einem
Kind — dadurch ist der Zustand automatisch pro Kind isoliert. Details: [02 · Authentifizierung](02-authentifizierung.md).

---

## 2. Die zwei API-Welten

Pugling hat **zwei getrennte inhaltliche Welten**, die man sauber auseinanderhalten muss:

### A) Der Lern-Katalog (`learn`) — globale Übungs-Bibliothek

```text
Subject (Fach)  ─┬─ Chapter (Kapitel) ─┬─ Exercise (typisierte Übung, 12 Typen)
                 │                      └─ …
                 └─ ExerciseCategory (fachabhängige „Art": Grammatik, Vokabeln, …)
```

- **Global, kindneutral, Vater-gepflegt.** Wird **einmal** aufgebaut, nicht pro Kind.
- Jede `Exercise` hat einen **Typ** (`ExerciseType`) und eine **typisierte Config** (als JSON gespeichert,
  im API pro Typ ein eigenes Schema). Dazu **Metadaten**: `GradeMin/GradeMax` (Klassenstufe),
  `SchoolTypes` (Schulart, `[Flags]`), `Source` (Quelle), `CategoryId` (Art) — Basis für die spätere
  automatische Lehrplan-Vorfilterung (`GET api/v1/learn/exercises`).
- Route je Typ: `api/v1/learn/subjects/{s}/chapters/{c}/<typ>`. Siehe [03 · Übungstypen](03-uebungstypen.md).

### B) Der Study-Plan (`study-plans`) — das produktive Training pro Kind

```text
StudyPlan (gehört einem Kind, verfahrensneutraler Rahmen)
 ├─ Method: Vocabulary | Cloze | Matching
 ├─ StudyPlanItem[]  → referenziert Inhalte aus einem STORE (nicht aus dem Katalog!)
 ├─ PracticeSession[] + ReviewEvent[]   (Übungszeit + Wiederholungen)
 ├─ TestAttempt[] + TestItemResult[]    (Abschlusstests)
 └─ StudyDayReward[]                    (idempotente Tages-Belohnungen)
```

- **Der Study-Plan ist verfahrensneutral:** Zeit, Punkte, Fortschritt und Abschlusstest gelten für
  **jedes** Lernverfahren gleich. Verfahrensspezifisch sind nur der **Inhalt** und die **Test-Mechanik/Stufen**.
- **Inhalte kommen aus eigenen Stores**, nicht aus dem Katalog:
  - **Vokabel-Store** (`api/v1/learn/vocabulary`) — die „Single Source of Truth" für Wörter.
  - **Lückentext-Store** (`api/v1/learn/cloze-texts`).
  - Ein `StudyPlanItem` referenziert einen Store-Eintrag über dessen **`Key`**.

> ⚠️ **Wichtige Abgrenzung:** Der Katalog-Übungstyp `Vocabulary` (`VocabularyConfig` mit `Items`) ist
> **nicht dasselbe** wie der **Vokabel-Store** (`Vocabulary`-Entity mit `Key`). Ein Study-Plan zieht
> seine Vokabeln aus dem **Store** (per Key), nicht aus einer Katalog-Übung.

### Die einzige Brücke: `to-study-plan`

Heute verbindet **ein** Endpunkt die beiden Welten: Eine **Matching**-Katalogübung kann per
`POST api/v1/learn/subjects/{s}/chapters/{c}/matching/{id}/to-study-plan` in einen fertigen
Leitner-Study-Plan umgewandelt werden (jedes Paar wird zu einer Vokabel im Store). Siehe
[04 · Lernplan bauen](04-lernplan-bauen.md#8-abkürzung-matching-übung--fertiger-leitner-plan).

---

## 3. Datenmodell-Landkarte

```text
Father ──< Child ──< ChildPointsEntry  (Punkte-Ledger, jede Buchung trägt einen PointKind)
              │
              ├──< StudyPlan ──< StudyPlanItem ── VocabularyId? / ClozeTextId?
              │        │
              │        ├──< PracticeSession ──< ReviewEvent
              │        ├──< TestAttempt ──< TestItemResult
              │        └──< StudyDayReward   (idempotenz je Plan/Tag/Art)
              │
              ├──< Mission ──< MissionAward         (Tages-/Wochenziele)
              ├──< Achievement ──< AchievementAward (Badges)
              ├──< TimetableEntry                   (Stundenplan: Fach × Wochentag)
              ├──< Tag ──< ExerciseTag              (Tagging von Katalog-Übungen)
              └──< Klassenarbeit                    (geplant/geschrieben + Note)

Katalog (global):
Subject ──< Chapter ──< Exercise (Type, ConfigJson, Metadaten, SuggestedBonus?)
   └──< ExerciseCategory

Stores (global):
Vocabulary (Key, Word, Translation, Noun/Verb-Info, BaseForm)
ClozeText  (Key, Text mit {{n}}, Gaps, WordBank)

Legacy (nur erhalten):
TimeSlotRule (Zeitfenster-Multiplikator für Punkte)
```

Die Entities liegen in [backend/Pugling.Api/Models/](../backend/Pugling.Api/Models/):
`AdminEntities.cs` (Father/Child/Points), `LearnEntities.cs` (Katalog), `StudyPlanEntities.cs`,
`GamificationEntities.cs`, `VocabEntities.cs`, `ClozeEntities.cs`, `TimetableEntities.cs`,
`KlassenarbeitEntities.cs`, `RatingEntities.cs`, `TimeSlotRule.cs`.

---

## 4. Services (wo die Logik wohnt)

Controller sind dünn; die Geschäftsregeln stecken in [backend/Pugling.Api/Services/](../backend/Pugling.Api/Services/):

| Service | Aufgabe |
| --- | --- |
| **`ScoringService`** | **Die eine Stelle** für Review-Punkte: Basis (Neuheit/Box × Zeitfenster) + Ereignis-Boni (Combo, Speed). Jede Buchung trägt einen `PointKind`. → [05 · Punkte](05-punkte-und-bonus.md) |
| **`StudyProgressService`** | Tages-Auswertung (`ComputeDayAsync`) + idempotente Tages-Punkte (`EvaluateAndAwardAsync`); Stufe-des-Tages (`StageForDay`), getippt? (`IsTyped`), Antwort-Normalisierung. |
| **`ScheduleService`** | Auswahl des Tages-Pools (Stundenplan: neu vs. Wiederholung) + Leitner-Mathematik (`ApplyReview`). |
| **`AnswerGrader`** | Normalisierter Antwortvergleich (Vokabel/Lücke), Alternativen erlaubt. |
| **`ExerciseAnswerChecker`** | Auswertung der Katalog-Übungen mit `/check` (Matching, Arithmetic, List). |
| **`ArithmeticProblemGenerator`** | Erzeugt Zufalls-Rechenaufgaben aus Regeln + Seed (reproduzierbar). |
| **`TestAttemptService`** | Gemeinsamer Lebenszyklus der Abschlusstests. |
| **`GamificationService`** | Missionen & Auszeichnungen auswerten + idempotent belohnen. |
| **`MetricsService`** | Fortschritts-Metriken (`ProgressMetric`) aus den Tabellen. |

**Kernprinzip: server-autoritativ.** Das Frontend schickt nur die *Antwort* des Kindes — nie ein
„richtig"-Flag und nie die gewählte Stufe. Der Server bestimmt die Stufe aus dem Fahrplan, bewertet
gegen die hinterlegte Lösung und bucht die Punkte. Das verhindert Selbstbetrug.

---

## 5. Ein typischer End-to-End-Loop

1. **Vater** legt Inhalte an (Vokabeln/Lückentexte im Store) und daraus einen **Study-Plan** fürs Kind.
2. **Sohn** loggt sich ein, sieht via `GET …/today` seine Pflichten, **übt** (Practice-Session →
   Karten → Review) und macht den **Abschlusstest**.
3. Der **Server** bewertet jede Antwort, bucht Punkte (Basis + Boni), bewegt Leitner-Boxen, wertet
   Missionen/Auszeichnungen aus.
4. **Vater** kontrolliert über `progress`/`report`/`points`, dreht bei Bedarf an den Bonus-Knöpfen
   (z. B. Grammatik-Bonus für ein lustloses Kind).

Konkrete Requests: [04 · Lernplan bauen](04-lernplan-bauen.md) (Vater) und [06 · Sohn-App](06-sohn-app.md) (Sohn).

---

## 6. Konventionen & Fallstricke (Kurzform)

- **C# 14 / .NET 10**, `Nullable` an, file-scoped Namespaces, Primary Constructors, `record`-DTOs.
- **Doku auf Deutsch** (`/// <summary>` fließt in Swagger). Kommentare erklären das *Warum*.
- **Fehler** einheitlich als `ProblemDetails` (`return Problem(statusCode:…, detail:…)`).
- **Zeit/UTC:** Tageslogik nutzt `DateTime.UtcNow`/`DateOnly` — nahe Mitternacht lokal ggf. anderer Kalendertag.
- **EF-Migrationen** bei jeder Schemaänderung (kein `EnsureCreated`). `db.Database.Migrate()` läuft beim Start.
- **JSON-Spalten** (`Gaps`, `WordBank`, `StageSchedule`, `Noun`/`Verb`) neu **zuweisen**, nicht in-place mutieren.

Voll ausformuliert in [CLAUDE.md](../CLAUDE.md).
