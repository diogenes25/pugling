# Umbauplan: Lehrplan = Zusammenstellung von Übungen

Status: **freigegeben** (Design), Bau noch nicht begonnen. Stand 2026-07-05.

## Motivation / Zielbild

Ein **Lehrplan** ist die **Zusammenstellung mehrerer Übungen** mit individuellen
Zielen (Tag/Woche) und Punkten – gemischte Verfahren erlaubt. Beispiel:

```
Lehrplan für Sohn 1
 - Englisch
   - Vokabeln (10 Vokabeln, neu, Zeigen)   → Tagesziel
   - Vokabeln (20 Vokabeln, alt, Tippen)    → kein Ziel (freies Üben)
 - Französisch
   - Birkenbihl                             → Tagesziel
 - Mathe
   - 10 Aufgaben Bruchrechnen               → Wochenziel
```

**Übungen** existieren unabhängig, kindneutral, wiederverwendbar und (später) zwischen
Vätern teilbar. **Lehrpläne** existieren unabhängig und referenzieren Übungen.

## Ausgangsbefund (Ist-Zustand)

Es existieren heute **zwei getrennte, duplizierte Inhaltswelten**:
- **Katalog** (`Subject → Chapter → Exercise`): Inhalt liegt **inline in `Exercise.ConfigJson`**
  (z. B. die 30 Vokabeln in `VocabularyConfig.Items`). Vom Lern-Motor **abgekoppelt**.
- **Lehrplan-Welt** (`StudyPlan → StudyPlanItem`): `StudyPlanItem` verweist direkt auf
  separate Stores `Vocabulary`/`ClozeText`. Der Leitner-/Test-Apparat hängt an
  `ContentId = VocabularyId/ClozeTextId`. `StudyPlan.Method` kennt nur 3 Verfahren.
- Einzige Brücke: `MatchingController.ToStudyPlan` **kopiert** Config-Inhalte in den Store.
- Vorbild für Referenz-statt-Kopie existiert: `KlassenarbeitExercise` referenziert `Exercise` per Id.

## Getroffene Entscheidungen

1. **Inhaltsquelle:** Die Übungs-`ConfigJson` wird die **einzige Wahrheit**. Eine Position
   verweist per `ExerciseId`; der Motor liest Items aus der Config. Preis: Motor von
   `ContentId=Store-Id` auf `(PositionId, ItemIndex)` umschlüsseln. Kein Kopieren mehr.
2. **Leitner-Zustand** pro Content-Atom in einer neuen Tabelle `PositionItemProgress`
   (faul angelegt). Nur *Fortschritt* wird materialisiert, nicht *Inhalt*.
3. **Heterogene „Erledigt"-Semantik** je `ExerciseTypeManifest.CheckMode`:
   - Leitner-Drill (Vokabeln/Cloze/Matching): Tagestest bestanden / fällige Karten geübt
   - Inhaltsübung (Birkenbihl, Reading): durchgespielt
   - Katalog-Check (Bruchrechnen/Liste): N Aufgaben korrekt
   Tagesziel = alle Positionen mit Rhythmus *Tag* nach ihrer Typ-Regel erledigt (Woche analog).
4. **Migration:** dev-only, `pugling.db` löschen + neu migrieren/seeden (v1 darf vor
   Publikation brechen). Kein datenerhaltender Umstieg.
5. **Autorschaft/geteilte Bibliothek:** eigene **spätere Etappe 7** (siehe unten), orthogonal.

## Ziel-Datenmodell

- **`Exercise`** (Katalog, bleibt) – `SuggestedBonus` → volle **Defaults** (Stufe, Menge, Punkte).
  Metadaten für Entdeckung existieren bereits: `Source` (Lehrbuch, Freitext), `Chapter`,
  `ExerciseCategory` (Thema/Art), `SchoolTypes`, `GradeMin/Max`.
- **`StudyPlan`** → reiner Container: `ChildId, Title, StartDate, EndDate, Active`.
  **Entfällt:** `Method` + plan-weite Tages-/Punkte-/Leitner-Felder.
- **`PlanPosition`** (umgebautes `StudyPlanItem`): `ExerciseId`, `Order`; Overrides
  (leer = Übungs-Default): `Stage`, `ItemCount`, `Scope` (Neu/Alt/Alle); **Ziel** `Cadence`
  (Kein/Tag/Woche) + Schwelle; **Punkte** (Default aus Übung); Leitner-Config je Position.
- **`PositionItemProgress`** (neu): `PositionId, ItemIndex, Box, DueOn, ReviewCount, IntroducedAt`.
- **`PracticeSession`/`ReviewEvent`/`TestAttempt`/`TestItemResult`**: `ContentId` → `(PositionId, ItemIndex)`.

## Etappen (Backend-First, jede einzeln verifizierbar)

1. **Datenmodell + Migration** – neue Entities, DbContext, EF-Migration, Reseed. **✅ ERLEDIGT
   (2026-07-05):** `PlanPosition` + `PositionItemProgress` + `GoalCadence`/`ItemScope`-Enums
   ([Models/PlanPositionEntities.cs](../backend/Pugling.Api/Models/PlanPositionEntities.cs)),
   `StudyPlan.Positions`-Nav, `Exercise.DefaultStage`/`DefaultItemCount`, DbContext-Config,
   Migration `LehrplanPositionen`. **Additiv** (kein Wipe nötig) – Alt-Modell läuft parallel weiter.
   Verifiziert: Build grün, Migration angewendet, Tabellen physisch da, App startet.
2. **Inhaltsquelle** – `ExerciseContentProvider` liefert je Typ die übbaren/testbaren Items
   aus `ConfigJson`. **✅ ERLEDIGT (2026-07-05):**
   [Services/ExerciseContentProvider.cs](../backend/Pugling.Api/Services/ExerciseContentProvider.cs)
   projiziert alle Typen verfahrensneutral in `ContentItem` (Index/Prompt/Answer/AcceptedAnswers/Hint/GapIndex);
   Essay + ArithmeticDrill bewusst leer (frei/generiert). Nur Extraktion – Bewertung bleibt bei
   `AnswerGrader`/`ExerciseAnswerChecker`. Als Singleton registriert. 13 Unit-Tests, Gesamt-Suite 131 grün.
3. **Motor umschlüsseln** – PracticeSessions + 3 Test-Controller lesen aus Position→Übung+Index;
   Re-Key Review/TestItemResult. Verifikation: `/smoke-test` Drill+Test end-to-end. **(größter Brocken)**
4. **Ziel-/Punkte-Engine** – `StudyProgressService`: Abschluss pro Position nach `CheckMode`,
   Rollup Tag/Woche; Punkte pro Position. Verifikation: `today`/`progress` gegen gemischten Plan.
5. **Lehrplan-CRUD** – `StudyPlansController`: Position per `ExerciseId` + Overrides + Ziel + Punkte.
   Verifikation: gemischten Plan per API bauen.
6. **Frontend** – Types/API; Vater (Übungen wählen, Ziel/Punkte je Position); Sohn (heterogene
   Typen spielen). Verifikation: Playwright-E2E-Loop.
7. **Autorschaft & geteilte Bibliothek** (später, orthogonal) – `FatherId`/Autor am Katalog,
   Sichtbarkeit (privat/veröffentlicht), Edit-/Lösch-Schutz gegen fremde Übungen. Optional:
   strukturiertes Lehrbuch-Objekt statt Freitext-`Source`.

## Offene Detailfragen

- `Scope` Neu/Alt genau definieren (Alt = bereits eingeführt?), plan- vs. positionsweit.
- `ArithmeticDrill` erzeugt Aufgaben on-the-fly → Leitner n/a, Abschluss = N gelöst.
