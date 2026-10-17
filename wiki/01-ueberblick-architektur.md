---
tags: [typ/konzept, bereich/doku, rolle/creator, rolle/supervisor, rolle/student]
aliases: [Architektur-Überblick, Datenmodell-Landkarte]
---

# 01 · Überblick & Architektur

Diese Seite gibt das mentale Modell, das man für alles Weitere braucht. Wer sie versteht, kann sich
in jeder anderen Wiki-Seite schnell orientieren.

← [Zurück zum Wiki-Index](../README.md)

---

## 1. Drei Ebenen, ein Ziel

Das fachliche Grundmodell ([grundprinzip.md](../docs/grundprinzip.md)) kennt **drei Ebenen** – zugleich
**Rollen** (`ProfileRole`), entkoppelt vom Login (ein `Account` kann mehrere tragen):

| Ebene / Rolle | Claim | Darf | Ziel |
| --- | --- | --- | --- |
| **Creator** (`Roles.Creator`) | `fid` | Übungen/Inhalte im Katalog erstellen | Lernstoff liefern |
| **Supervisor** (`Roles.Supervisor`) | `fid` | Lehrpläne/Ziele/Punkte, Shop, Kind-Verwaltung, alles auswerten | Lernerfolg **erzwingen** |
| **Student** (`Roles.Student`) | `cid` | nur die **eigenen** Pläne sehen, üben, testen, kaufen/aktivieren | mit Spaß **lernen** |

Gegated wird direkt auf diese drei Rollen; ein Vater/Sohn-Alias existiert im Code nicht. Die Zeile
hinter jeder Nicht-Kind-Rolle heißt `Adult` (auch ein Lehrer-Konto ohne Kind), die hinter dem Student `Child`.

Rollenbasierter Einstieg in Doku und Wiki: [docs/rollen-doku.md](../docs/rollen-doku.md).

Ein Vater ist heute zugleich Creator **und** Supervisor (ein Token, beide Ebenen). Ein **Student kann
mehrere Supervisor** haben (`SupervisorLink`: Vater/Mutter/Oma) – jeder mit eigenem Shop/Angeboten,
Wallet gemeinsam, Einlösung ausstellergebunden. Jedes Study-Plan-/Missions-/Punkte-Objekt gehört genau
einem Kind — der Zustand ist pro Kind isoliert. Details: [02 · Authentifizierung](02-authentifizierung.md).

---

## 2. Die zwei API-Welten

Pugling hat **zwei getrennte inhaltliche Welten**, die man sauber auseinanderhalten muss:

### A) Der Lern-Katalog (`creator/…`) — globale Übungs-Bibliothek

```text
Publisher ─< TextbookSeries (Lehrwerk-Reihe) ─< SeriesUnit (Band/Unit) ─< Exercise (typisierte Übung, 12 Typen)
                 │ SubjectId (optional)
Subject (Fach) ──┴─< ExerciseCategory (fachabhängige „Art": Grammatik, Vokabeln, …)
```

- **Seit B-106 (2026-08-05) hängt jede Übung an einer `SeriesUnit`**, nicht mehr an einem `Chapter` – die
  Entität ist entfernt. Das Fach ergibt sich transitiv über die Reihe (`SeriesUnit.Series.SubjectId`).
- **Global, kindneutral, Creator-gepflegt.** Wird **einmal** aufgebaut, nicht pro Kind. Lesen darf jeder
  Creator, ändern nur ein Owner (RWX-Grants je Übung, `OwnerAdultId` an Reihe und Fach).
- Jede `Exercise` hat einen **Typ** (String-`Key`, aufgelöst über die `ExerciseTypeRegistry`) und eine **typisierte Config** (als JSON gespeichert,
  im API pro Typ ein eigenes Schema). Dazu **Metadaten**: `GradeMin/GradeMax` (Klassenstufe),
  `SchoolTypes` (Schulart, `[Flags]`), `Source` (Quelle), `CategoryId` (Art) — Basis für die spätere
  automatische Lehrplan-Vorfilterung (`GET api/v1/creator/exercises`).
- Route je Typ: `api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/<typ>`.
  Siehe [03 · Übungstypen](03-uebungstypen.md).

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
   Inhalts-Atom wird materialisiert in `PositionItemProgress`. Stores wie `creator/vocabulary` und
   `creator/cloze-texts` existieren weiterhin als globale Inhaltsbibliotheken und für Verknüpfungen, sind
   aber nicht mehr die direkte Study-Plan-Item-Liste.
- **Vokabelübungen haben eine eigene Item-Ebene:** Die Config enthält Einstellungen und optional
   Authoring-Payload. Die tatsächlichen Vokabelpaare liegen als stabile `ExerciseItem`-Zeilen unter der
   Übung (`.../vocabulary/{exerciseId}/items`) und referenzieren den Vokabel-Store per `VocabularyId`.

> ⚠️ **Wichtige Abgrenzung:** Ein Study-Plan enthält keine kopierten Vokabel-/Lückentext-Items mehr.
> Er verweist über Positionen auf Katalog-Übungen. Änderungen an der Übungs-Config ändern damit den
> künftig gespielten Inhalt; Fortschrittsdaten bleiben positionsbezogen.

### Brücke Katalog → Training: Positionen

Der Vater baut zuerst einen leeren Plan-Container (`POST /api/v1/supervisor/study-plans`) und hängt dann Übungen
als Positionen an (`POST /api/v1/supervisor/study-plans/{planId}/positions`). Leere Overrides erben die Defaults
der Übung (`DefaultStage`, `DefaultUseLeitner`, `SuggestedBonus`). Siehe
[04 · Lernplan bauen](04-lernplan-bauen.md).

---

## 3. Datenmodell-Landkarte

```text
Account ──< AccountProfile ── Adult | Child     (Login/PIN-Hash; ein Konto trägt mehrere Rollen)
Adult ──< SupervisorLink >── Child ──< ChildPointsEntry  (Punkte-Ledger, jede Buchung trägt einen PointKind)
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
Publisher ──< TextbookSeries ──< SeriesUnit ──< Exercise (Type, ConfigJson, Metadaten, SuggestedBonus?)
                   │ SubjectId?                   ├──< ExerciseItem (nur Vocabulary: stabile Store-Referenzen)
Subject ───────────┴──< ExerciseCategory          └──< ExerciseGrant (RWX je Creator)

Stores (global / optionale Inhaltsbibliotheken):
Vocabulary (Key, Word, Translation, Noun/Verb-Info, BaseForm)
ClozeText  (Key, Text mit {{n}}, Gaps, WordBank)
MediaAsset ──< MediaVariant, MediaLink (Bilder an Vokabeln/Übungen; Auswahl je Kind über InterestTag)
```

Der Zeitfenster-Multiplikator für Punkte ist **Konfiguration** (`Scoring:TimeSlots` in `appsettings.json`),
keine Tabelle mehr.

Die Entities liegen in [backend/Pugling.Api/Models/](../backend/Pugling.Api/Models/):
`IdentityEntities.cs` (Account/AccountProfile), `AdminEntities.cs` (Adult/Child/SupervisorLink/Points),
`CurriculumEntities.cs` (Publisher/TextbookSeries/SeriesUnit/CreatorProfile), `LearnEntities.cs` (Katalog),
`ExerciseItemEntities.cs`, `StudyPlanEntities.cs`, `PlanPositionEntities.cs`, `ItemProgressEntities.cs`,
`GamificationEntities.cs`, `ObjectiveEntities.cs`, `VocabEntities.cs`, `ClozeEntities.cs`, `MediaEntities.cs`,
`InterestEntities.cs`, `TimetableEntities.cs`, `KlassenarbeitEntities.cs`, `RemarkEntities.cs`. Die achtzehnte Datei
dort, `VocabLink.cs`, ist kein Entity, sondern der Helfer für den `_self`-Link auf Vokabelspeicher-Einträge.

---

## 4. Services (wo die Logik wohnt)

Controller sind dünn; die Geschäftsregeln stecken in [backend/Pugling.Api/Services/](../backend/Pugling.Api/Services/):

| Service | Aufgabe |
| --- | --- |
| **`ScoringService`** | **Die eine Stelle** für Review-Punkte: Basis (Neuheit/Box × Zeitfenster) + Ereignis-Boni (Combo, Speed). Jede Buchung trägt einen `PointKind`. → [05 · Punkte](05-punkte-und-bonus.md) |
| **`PositionProgressService`** | Tages-/Verlaufs-Auswertung über Positionen, Ziel-erledigt-Regeln und idempotente Ziel-Punkte. |
| **`PositionPlayService`** | Inhaltsauswahl, Stufen-Fahrplan, getippte Stufen, Leitner-Fälligkeit und Box-Bewegung je Position. |
| **`AnswerGrader`** | Normalisierter Antwortvergleich (Vokabel/Lücke), Alternativen erlaubt. |
| **`ExerciseTypeRegistry`** | Löst `Exercise.Type` (String-`Key`) auf die Typklasse (`IExerciseType`) auf; trägt Manifeste + Katalog-`/check` (je Typ `IExerciseType.Check`). |
| **`ExerciseItemService`** | Materialisiert Vokabel-Authoring-Payload (`items`/`refs`) in stabile `ExerciseItem`-Zeilen und bewahrt `ItemId`s beim Abgleich. |
| **`ItemProgressService`** | Schreibt planübergreifenden Vokabel-Lernstand je Kind und `ItemId` plus Antwort-Historie. |
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
Wie die Endpunkte dieses Loops **aufeinander aufbauen** (Übung → Lehrplan → Kind → Auswertung), mit
Datenfluss-Diagramm: [docs/endpunkt-beziehungen.md](../docs/endpunkt-beziehungen.md).

---

## 6. Konventionen & Fallstricke (Kurzform)

- **C# 14 / .NET 10**, `Nullable` an, file-scoped Namespaces, Primary Constructors, `record`-DTOs.
- **Code-Doku auf Englisch** (`/// <summary>` fließt in Swagger, ebenso jeder `//`-Kommentar); die
   Markdown-Doku bleibt deutsch. Kommentare erklären das *Warum*.
- **Fehler** einheitlich als `ProblemDetails` mit maschinenlesbarem `code`
   (`return this.ProblemWithCode(ApiErrors.X, "…")`).
- **Zeit/UTC:** Tageslogik nutzt `DateTime.UtcNow`/`DateOnly` — nahe Mitternacht lokal ggf. anderer Kalendertag.
- **EF-Migrationen:** die Kette ist genau **eine** Migration und wird bei jeder Schemaänderung neu gefaltet
   (kein `EnsureCreated`). `db.Database.Migrate()` läuft beim Start.
- **JSON-Spalten** tragen einen `ValueComparer` (Tor G7); eine neue JSON-Spalte braucht ihn ebenfalls.

Voll ausformuliert in [CLAUDE.md](../CLAUDE.md).
