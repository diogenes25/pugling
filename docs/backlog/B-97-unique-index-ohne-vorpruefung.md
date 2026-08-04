---
tags: [typ/story, status/ausformuliert, bereich/backend, bereich/api]
aliases: [Unique-Index ohne Vorprüfung, 500 statt 409, duplicate_achievement]
status: ausformuliert
prio: P2
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/api-design-bewertung.md (Vorschlag A1) — Arbeitsrunde PM/API-Designer/Entwickler am 2026-08-04
grund: ""
ersetzt_durch: []
---

# B-97 · Zwei Schreibpfade laufen ungeprüft in einen Unique-Index und antworten mit 500

Ein Kapitel umbenennen und ein Ziel-Abzeichen anlegen laufen ohne Vorprüfung gegen einen Unique-Index der
Datenbank. Ergebnis ist kein `409` mit lesbarem Grund, sondern ein **500 mit halb gespeichertem Zustand** —
und für den Abzeichen-Fall gibt es nicht einmal einen Fehlercode. Beide Stellen verletzen eine Regel, die
dieses Repo sich selbst aufgeschrieben hat (Vorprüfung vor dem Schreiben, damit der Konflikt einen `code`
bekommt).

## User Story

Als **Vater** möchte ich beim Umbenennen eines Kapitels oder beim Anlegen eines Abzeichens eine Meldung
bekommen, die den Grund nennt, damit ich den Namen ändern kann — statt eines Serverfehlers, nach dem ich
nicht weiß, was gespeichert wurde.

## Ist-Stand am Code

- `Controllers/Creator/ChaptersController.cs:80` setzt `chapter.Name` ohne jede Prüfung; der Unique-Index
  `(SubjectId, Name)` steht in `Data/PuglingDbContext.cs:707`. Der Code `duplicate_chapter_name` **existiert**
  und wird auf dem POST-Pfad benutzt — über PATCH ist er damit unerreichbar.
- `Controllers/Supervisor/MissionsController.cs:119` (POST) und `:141` (PATCH) schreiben gegen
  `(ChildId, Metric, Threshold)` (`PuglingDbContext.cs:716`). Für diesen Konflikt gibt es **keinen**
  Fehlercode in `Errors/ApiErrors.cs`.
- Das Muster für die Reparatur steht wenige Zeilen entfernt im Nachbarcontroller:
  `Controllers/Creator/VocabularyTagsController.cs:65` (`AnyAsync`-Vorlauf, dann `ProblemWithCode`).
- Ein globaler `DbUpdateException` → `409`-Handler existiert nicht (geprüft in `Program.cs` und
  `Errors/`); der Fall endet darum im allgemeinen 500-Pfad.
- **Reichweite präzisiert** (Runde, gegen die erste Fassung des Berichts): Über PATCH ist der
  Abzeichen-Konflikt **schmaler** als über POST — `UpdateAchievementDto` (`Contracts/…/GamificationDtos.cs:24`)
  trägt **kein** `Metric`. Erreichbar ist er nur, indem `Threshold` auf den Wert eines anderen Abzeichens
  *derselben* Metrik gesetzt wird. Der POST-Pfad ist der breite.

## Die echte Lücke

Nicht „die API kennt keine Konflikte" — der Katalog ist stark (57 Codes, keiner tot). Die Lücke sind **zwei
Nachzügler**: an zwei von vielen Schreibpfaden fehlt die Vorprüfung, die überall sonst steht. Die Reparatur
ist je Stelle ein `AnyAsync` plus eine `ProblemWithCode`-Zeile; teuer wäre nur der Versuch, daraus eine
mechanische Regel über *alle* Indizes zu machen — siehe „Ergebnis der Runde", Punkt 3.

## Ergebnis der Arbeitsrunde vom 2026-08-04

Vorlage für die Grill-Runde; die Entscheidung gehört dem Menschen, nicht der Runde.

1. **Beide Vorprüfungen bauen** — einig. Vorschätzung **XS**, `wo: backend`, keine Migration, kein
   Vertragsbruch. Der neue Code `duplicate_achievement` ist **additiv** in `ApiErrors`.
2. **Kein globaler `DbUpdateException` → 409-Handler.** Er würde jeden künftigen Index stumm in ein `409`
   ohne fachlichen `code` verwandeln und damit genau die Aussagekraft nehmen, für die der Katalog existiert.
3. **Das im Bericht vorgeschlagene Tor über alle Unique-Indizes ist zurückgezogen** — von beiden Seiten.
   Es sind **47** `IsUnique` (davon 18 mit `HasFilter`; doppelt belegt: `PuglingDbContext.cs` und die eine
   Migration nennen dieselbe Zahl — die „41" im Bericht war falsch). Die Zuordnung Index → Schreibpfad ist
   **nicht reflexiv ableitbar**: viele Indizes liegen auf Tabellen ohne jeden Schreib-Endpunkt
   (`PositionGoalReward`, `MissionAward`, `ItemProgress` …), andere auf Join-Tabellen, deren Schreibpfad
   bewusst ein Link-Insert ohne 409 ist. Ein Tor müsste eine Liste von Paaren pflegen, und die pflegt
   niemand. Die Regel bleibt, wo sie steht: in `backend/Pugling.Api/CLAUDE.md`.

## Akzeptanzkriterien

1. `PATCH creator/subjects/{subjectId}/chapters/{chapterId}` mit einem im Fach bereits vergebenen Namen
   antwortet `409` mit `code: duplicate_chapter_name` — nicht `500`.
2. `POST` **und** `PATCH` auf `supervisor/children/{childId}/achievements` antworten bei einem Konflikt auf
   `(ChildId, Metric, Threshold)` mit einem `ProblemDetails` mit `code: duplicate_achievement`.
3. Nach einem abgelehnten PATCH steht in der Datenbank der **alte** Wert (kein halb geschriebener Zustand).
4. Je Fall ein Integrationstest, der **vor** der Änderung rot war (Abnahmeform für `art: Defekt`).

## Verlauf

- **2026-08-04** — angelegt aus `docs/api-design-bewertung.md` (A1) und der Arbeitsrunde
  PM/API-Designer/Entwickler. Beide Befunde von beiden Seiten unabhängig am Code bestätigt; die
  Tor-Ausweitung und der globale Exception-Handler wurden in der Runde verworfen, die Reichweite des
  PATCH-Falls korrigiert.
