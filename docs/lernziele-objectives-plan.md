# Umbauplan: Lernziele/Objectives (OKR-Klammer über dem Lernstand)

Status: **Backend umgesetzt** (Etappen 1–6). Stand 2026-07-12. Build grün, 297 Tests grün
(inkl. `ObjectiveTests`: Committed→Münzen + Etappe/Abschluss + Idempotenz, Stretch→Gems ohne Abschluss,
ClassTestGrade-Anker, Validierung/Rollen, Etappen-CRUD), Migration `AddObjectives`. **Offen: Etappe 7 (Frontend).**

## Motivation / Zielbild

Pugling misst heute stark auf zwei Ebenen: **taktisch** (Lehrplan-Positionen mit Rhythmus,
Schwelle, Punkten, Malus) und **Fortschritt** (`ItemProgress`/`MasteryRollup`, `MetricsService`).
Was fehlt, ist die **strategische Klammer**: das *große, terminierte Ergebnis-Ziel*, auf das der
tägliche Grind einzahlt – „Wofür lernen wir dieses Halbjahr eigentlich?".

Wir übernehmen dafür bewusst nur den **Kern** des OKR-Rahmenwerks (Objective + messbare Key Results),
nicht die Corporate-Zeremonie:

- **Kein 0.7-Grading.** Für ein Kind ist „du hast 70 %" Scham, kein Erfolg. Statt Note → Anzahl
  geschaffter Etappen + Fortschrittsbalken.
- **Kein Malus auf Objectives.** Der „Stick" bleibt, wo er hingehört: auf den täglichen/wöchentlichen
  `PenaltyCoins`-Pflichtpositionen. Ein verpasstes großes Ziel wird nicht bestraft, sondern über
  **Etappensieg-Häppchen** motiviert (kurzer Feedback-Loop – repariert die für ein Kind zu lange
  OKR-Kadenz).
- **Committed vs. Stretch** bildet die Zwei-Währungs-Ökonomie ab: Committed → 🪙 Münzen (reale
  Privilegien), Stretch → 💎 Gems (kosmetisch).

## Architektur-Leitentscheidung

Ein **Objective ist ein Container über mehreren Key Results** – genau wie `StudyPlan` ein Container
über `PlanPosition`s ist. Ein **Key Result ist praktisch das vorhandene `LearnGoal`**: eine messbare
Zielgröße auf Katalog-Scope, live ausgewertet über `ChildLearnProgressService.ScopeEvaluator`
(plan-übergreifend, überlebt Umhängen von Übungen). Wir bauen **kein** Parallelsystem, sondern eine
Klammer + genau eine neue, kindsichere Metrik.

## Namen (kindgerecht)

| Framework | Vater-UI / Code | Sohn-UI |
|---|---|---|
| Objective | `Objective` (Lernvorhaben) | „Dein großes Ziel" |
| Key Result | `KeyResult` | „Etappe" (mit Fortschrittsbalken) |
| Grading 0.0–1.0 | *entfällt* | „3 von 4 Etappen geschafft" + Balken, nie eine Note |

## Getroffene Entscheidungen

1. **Objective = Container, KeyResult = LearnGoal-artige Auswertung.** Reuse `ScopeEvaluator`
   (einmal je Kind laden, alle KRs im Speicher rollen), kein materialisierter Zustand.
2. **Belohnungs-Takt: Häppchen + Abschluss.** Jede erreichte Etappe zahlt sofort einen kleinen
   Batzen (`RewardPerKeyResult`), der Voll-Abschluss den großen (`RewardOnComplete`). Kurzer
   Feedback-Loop.
3. **Klassenarbeits-Note als eigener KR-Typ (`ClassTestGrade`).** Der ungameable Realitäts-Anker:
   Note ≤ Ziel je Fach, **read-only** aus `Klassenarbeit.Grade` (vom Vater getippt). Klassenarbeiten
   bleiben von der Punkte-Ökonomie entkoppelt; das KR schaut nur hinein.
4. **Kein Malus, kein Scheduler.** Belohnung wird per **Lazy Settlement** an denselben
   POST-Nahtstellen wie der Pflicht-Malus abgerechnet (Login + Student-Me), idempotent über
   Unique-Key.

## Tricksicherheit der Key-Result-Typen (Auswahlbegründung)

Metriken zerfallen in **outcome-basiert** (Leitner-Box, Anti-Farming über `countsForMastery`) und
**aktivitätsbasiert** (Zähler). Für ein *Erfolgs*-Ziel taugen nur die ersten – ein Zähler belohnt
Wiederholen, nicht Können.

| KR-Typ | Quelle | Tricksicher? | Verwendung |
|---|---|---|---|
| `MasteredPercent` (% in Box 5) | `MasteryRollup.MasteredItems` | ✅ Box 5 nur durch wiederholt korrekt; Fehler → zurück auf Box 1 | **Primär-KR** |
| `AvgMastery` (⌀ Box) | `MasteryRollup.AvgMasteryPercent` | ✅ weichere Variante | 2. KR |
| `MaxWeakItems` (≤ N schwach) | `MasteryRollup.WeakItems` | ✅ „keine Baustellen mehr" | „Aufräum"-KR |
| `ClassTestGrade` (Note ≤ Ziel) | `Klassenarbeit.Grade` | ✅✅ vom Vater getippt, für den Sohn nicht manipulierbar | **Anker** |
| ~~`Coverage`~~ (eingeführt/gesamt) | Rollup | ❌ steigt durch bloßes *Sehen* | weggelassen |
| ~~`MinutesPracticed`/`CorrectReviews`~~ | `MetricsService` | ❌ Zähler = Farming-Anreiz | nicht als KR |
| `StreakDays ≥ N` | `MetricsService` | ⚠️ mäßig, aber motivierend | optional (Effort-KR im Stretch) |

**Standard-Set eines Objectives:** `MasteredPercent` + `MaxWeakItems` + `ClassTestGrade`.

## Datenmodell

```csharp
public sealed class Objective
{
    public int Id { get; set; }
    public int ChildId { get; set; }
    public Child Child { get; set; } = null!;

    public string Title { get; set; } = "";       // konkret: "Englisch Unit 3 sicher können"
    public string? Motivation { get; set; }         // das "Warum", 1 Satz, wird dem Sohn gezeigt
    public ObjectiveKind Kind { get; set; }         // Committed | Stretch
    public DateOnly? Start { get; set; }
    public DateOnly? DueDate { get; set; }
    public bool Active { get; set; } = true;

    public int RewardOnComplete { get; set; }       // beim Erreichen ALLER KRs
    public int RewardPerKeyResult { get; set; }      // Etappensieg-Häppchen

    public DateTime CreatedAt { get; set; }
    public List<KeyResult> KeyResults { get; set; } = [];
}

public enum ObjectiveKind { Committed = 0, Stretch = 1 }

public sealed class KeyResult
{
    public int Id { get; set; }
    public int ObjectiveId { get; set; }
    public Objective Objective { get; set; } = null!;

    public int SubjectId { get; set; }              // Katalog-Scope wie LearnGoal
    public int? ChapterId { get; set; }
    public int? ExerciseId { get; set; }

    public KeyResultMetric Metric { get; set; }
    public int TargetValue { get; set; }            // % / Anzahl / Note×10 (ClassTestGrade)
    public string? Title { get; set; }
}

public enum KeyResultMetric
{
    AvgMastery = 0,       // vorhanden (LearnGoalMetric)
    MasteredPercent = 2,  // vorhanden
    MaxWeakItems = 3,     // vorhanden ("≤"-Ziel)
    ClassTestGrade = 4,   // NEU: beste Note ≤ Ziel je Fach, "≤"-Ziel
}
```

DTOs als `record` (Projektion, nie EF-Entities): `ObjectiveResponse` (mit `AchievedCount`,
`TotalCount`, `ProgressPercent`, `Status`, `Rewarded`, `KeyResults[]`), `KeyResultResponse`
(mit `CurrentValue`, `ProgressPercent`, `Status`), `Create/Update`-Requests.

## Belohnung — idempotent & wallet-sicher

Nach dem `PositionGoalReward`-Muster:

1. **Neue `PointKind`** + zwingendes Mapping in `PointKindCurrency.Of()` (sonst wirft `Of()`,
   Test schlägt an):
   - `ObjectiveCoins = 17` → `Currency.Coins` (Committed)
   - `ObjectiveGems = 18` → `Currency.Gems` (Stretch)
2. **Award-Tabelle `ObjectiveReward (ObjectiveId, PeriodKey)`** mit Unique-Index. `PeriodKey`:
   - `"kr:{keyResultId}"` → Etappensieg-Häppchen, einmalig je KR
   - `"done"` → Voll-Abschluss, einmalig je Objective
3. Buchung: positive `ChildPointsEntry` (`ObjectiveCoins`/`ObjectiveGems`); `child.ConcurrencyStamp
   = Guid.NewGuid()` vor `SaveChanges`, `DbUpdateException` schlucken (identisch zu
   `SettleClosedPeriodsAsync`).
4. **Trigger:** `ObjectiveRewardService.SettleAsync(childId, today)` an `AuthController` (Login)
   und `MeController` (Student-Me) – re-evaluiert und bucht offene Häppchen/Abschlüsse.

## Endpunkte (Supervisor-Tier, spiegelt LearnGoals/StudyPlans)

```text
api/v1/supervisor/children/{childId}/objectives
  GET / · POST / · GET /{id} · PATCH /{id} · DELETE /{id}
api/v1/supervisor/children/{childId}/objectives/{objectiveId}/key-results
  POST / · PATCH /{krId} · DELETE /{krId}   (Scope fix, wie LearnGoal)
api/v1/student/me/objectives                 (Sohn-Sicht, read-only)
```

- Ownership via `[ServiceFilter(typeof(ChildOwnershipFilter))]`; Schreiben
  `[Authorize(Roles = Roles.Supervisor)]`, Lesen Vater + Kind.
- Fehler als `ProblemWithCode(ApiErrors.…)`; neue fachliche Codes additiv in `ApiErrors` ergänzen.

## Etappen (API-First, jede lauffähig + testbar)

1. **Modell:** Entities `Objective`/`KeyResult`/`ObjectiveReward` + `KeyResultMetric` +
   `PointKind`-Erweiterung + `PointKindCurrency`-Mapping; EF-Migration `AddObjectives`.
2. **Auswertung:** `ObjectiveEvaluationService` – reuse `ScopeEvaluator`, plus `ClassTestGrade`-Query.
3. **API:** `ObjectivesController` + `KeyResultsController` (CRUD, `record`-DTOs, deutsche `/// <summary>`).
4. **Belohnung:** `ObjectiveRewardService.SettleAsync` (idempotent, ConcurrencyStamp) + Einhängen
   an Login/Me.
5. **Sohn:** `GET student/me/objectives`.
6. **Test** (`Pugling.Api.Tests`): KR erreicht → Häppchen einmal; alle KRs → Abschluss einmal;
   erneuter Login bucht **nicht** doppelt; `ClassTestGrade` via Vater-Grade; Committed→Coins,
   Stretch→Gems.
7. **Frontend:** `/vater` Objective-Editor, `/sohn` „großes Ziel"-Karte mit Etappen-Balken +
   Belohnungs-Vorschau.

## Verwandte Doku

- Positions-Pflichtziel + Malus (der „Stick"): [pflicht-malus-schenkung-plan.md](pflicht-malus-schenkung-plan.md)
- Lehrplan-Container-Modell: [lehrplan-umbau-plan.md](lehrplan-umbau-plan.md)
- Endpunkt-Zusammenhänge: [endpunkt-beziehungen.md](endpunkt-beziehungen.md)
