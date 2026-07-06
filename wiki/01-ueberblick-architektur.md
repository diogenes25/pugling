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
StudyPlan (gehört einem Kind, reiner Container)
 ├─ PlanPosition[] → referenziert globale Katalog-Übungen (`Exercise`)
 ├─ PracticeSession[] + ReviewEvent[]   (Übungszeit + Wiederholungen)
 ├─ TestAttempt[] + TestItemResult[]    (Abschlusstests)
 └─ PositionGoalReward[]                (idempotente Ziel-Belohnungen je Position/Periode)
```

- **Der Study-Plan ist nur der Rahmen:** Kind, Titel, Laufzeit, aktiv/inaktiv und optionales Fach.
- **Die einzelne `PlanPosition` entscheidet das Training:** referenzierte Katalog-Übung, Reihenfolge,
   Stufe/Fahrplan, Item-Auswahl, Zielrhythmus (`None|Daily|Weekly`), Bestehensschwelle, Punkte und
   Leitner-Einstellungen.
- **Inhalte kommen aus der Übungs-Config** der referenzierten Katalog-`Exercise`; der Fortschritt pro
   Inhalts-Atom wird materialisiert in `PositionItemProgress`. Stores wie `learn/vocabulary` und
   `learn/cloze-texts` existieren weiterhin als globale Inhaltsbibliotheken und für Verknüpfungen, sind
   aber nicht mehr die direkte Study-Plan-Item-Liste.

> ⚠️ **Wichtige Abgrenzung:** Ein Study-Plan enthält keine kopierten Vokabel-/Lückentext-Items mehr.
> Er verweist über Positionen auf Katalog-Übungen. Änderungen an der Übungs-Config ändern damit den
> künftig gespielten Inhalt; Fortschrittsdaten bleiben positionsbezogen.

### Brücke Katalog → Training: Positionen

Der Vater baut zuerst einen leeren Plan-Container (`POST /api/v1/study-plans`) und hängt dann Übungen
als Positionen an (`POST /api/v1/study-plans/{planId}/positions`). Leere Overrides erben die Defaults
der Übung (`DefaultStage`, `DefaultUseLeitner`, `SuggestedBonus`). Siehe
[04 · Lernplan bauen](04-lernplan-bauen.md).

---

## 3. Datenmodell-Landkarte

```text
Father ──< Child ──< ChildPointsEntry  (Punkte-Ledger, jede Buchung trägt einen PointKind)
              │
              ├──< StudyPlan ──< PlanPosition ── Exercise
              │        │
              │        ├──< PracticeSession ──< ReviewEvent
              │        ├──< TestAttempt ──< TestItemResult
              │        └──< PositionGoalReward + PositionItemProgress
              │
              ├──< Mission ──< MissionAward         (Tages-/Wochenziele)
              ├──< Achievement ──< AchievementAward (Badges)
              ├──< TimetableEntry                   (Stundenplan: Fach × Wochentag)
              ├──< Tag ──< ExerciseTag              (Tagging von Katalog-Übungen)
              └──< Klassenarbeit                    (geplant/geschrieben + Note)

Katalog (global):
Subject ──< Chapter ──< Exercise (Type, ConfigJson, Metadaten, SuggestedBonus?)
   └──< ExerciseCategory

Stores (global / optionale Inhaltsbibliotheken):
Vocabulary (Key, Word, Translation, Noun/Verb-Info, BaseForm)
ClozeText  (Key, Text mit {{n}}, Gaps, WordBank)

Legacy (nur erhalten):
TimeSlotRule (Zeitfenster-Multiplikator für Punkte)
```

Die Entities liegen in [backend/Pugling.Api/Models/](../backend/Pugling.Api/Models/):
`AdminEntities.cs` (Father/Child/Points), `LearnEntities.cs` (Katalog), `StudyPlanEntities.cs`,
`PlanPositionEntities.cs`, `GamificationEntities.cs`, `VocabEntities.cs`, `ClozeEntities.cs`,
`TimetableEntities.cs`, `KlassenarbeitEntities.cs`, `TimeSlotRule.cs`.

---

## 4. Services (wo die Logik wohnt)

Controller sind dünn; die Geschäftsregeln stecken in [backend/Pugling.Api/Services/](../backend/Pugling.Api/Services/):

| Service | Aufgabe |
| --- | --- |
| **`ScoringService`** | **Die eine Stelle** für Review-Punkte: Basis (Neuheit/Box × Zeitfenster) + Ereignis-Boni (Combo, Speed). Jede Buchung trägt einen `PointKind`. → [05 · Punkte](05-punkte-und-bonus.md) |
| **`PositionProgressService`** | Tages-/Verlaufs-Auswertung über Positionen, Ziel-erledigt-Regeln und idempotente Ziel-Punkte. |
| **`PositionPlayService`** | Inhaltsauswahl, Stufen-Fahrplan, getippte Stufen, Leitner-Fälligkeit und Box-Bewegung je Position. |
| **`AnswerGrader`** | Normalisierter Antwortvergleich (Vokabel/Lücke), Alternativen erlaubt. |
| **`ExerciseAnswerChecker`** | Auswertung der Katalog-Übungen mit `/check` (Matching, Arithmetic, List). |
| **`ArithmeticProblemGenerator`** | Erzeugt Zufalls-Rechenaufgaben aus Regeln + Seed (reproduzierbar). |
| **`PositionReportService`** | Mastery-/Report-Sicht pro Position. |
| **`GamificationService`** | Missionen & Auszeichnungen auswerten + idempotent belohnen. |
| **`MetricsService`** | Fortschritts-Metriken (`ProgressMetric`) aus den Tabellen. |

**Kernprinzip: server-autoritativ.** Das Frontend schickt nur die *Antwort* des Kindes — nie ein
„richtig"-Flag und nie die gewählte Stufe. Der Server bestimmt die Stufe aus dem Fahrplan, bewertet
gegen die hinterlegte Lösung und bucht die Punkte. Das verhindert Selbstbetrug.

---

## 5. Ein typischer End-to-End-Loop

1. **Vater** legt Katalog-Übungen an und erstellt daraus einen **Study-Plan** mit Positionen fürs Kind.
2. **Sohn** loggt sich ein, sieht via `GET …/overview` seine Pflichten, **übt** (positionsbezogene
   Practice-Session → Karten → Review) und macht den **Abschlusstest** der Position.
3. Der **Server** bewertet jede Antwort, bucht Punkte (Basis + Boni), bewegt Leitner-Boxen, wertet
   Missionen/Auszeichnungen aus.
4. **Vater** kontrolliert über `progress`/`report`/`points`, dreht bei Bedarf an den Bonus-Knöpfen
   (z. B. Grammatik-Bonus für ein lustloses Kind).

Konkrete Requests: [04 · Lernplan bauen](04-lernplan-bauen.md) (Vater) und [06 · Sohn-App](06-sohn-app.md) (Sohn).

---

## 6. Konventionen & Fallstricke (Kurzform)

- **C# 14 / .NET 10**, `Nullable` an, file-scoped Namespaces, Primary Constructors, `record`-DTOs.
- **Doku auf Deutsch** (`/// <summary>` fließt in Swagger). Kommentare erklären das *Warum*.
- **Fehler** einheitlich als `ProblemDetails` mit maschinenlesbarem `code`
   (`return this.ProblemWithCode(ApiErrors.X, "…")`).
- **Zeit/UTC:** Tageslogik nutzt `DateTime.UtcNow`/`DateOnly` — nahe Mitternacht lokal ggf. anderer Kalendertag.
- **EF-Migrationen** bei jeder Schemaänderung (kein `EnsureCreated`). `db.Database.Migrate()` läuft beim Start.
- **JSON-Spalten** (`Gaps`, `WordBank`, `StageSchedule`, `Noun`/`Verb`) neu **zuweisen**, nicht in-place mutieren.

Voll ausformuliert in [CLAUDE.md](../CLAUDE.md).
