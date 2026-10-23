---
tags: [typ/story, status/abgenommen, bereich/doku]
aliases: [Kapitel-Route im Wiki, tote Route in der Wissenskarte]
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: doku
migration: nein
vertragsbruch: nein
quelle: beim Bauen von B-163 gefunden (docs/backlog/B-163-art-und-typ-tragen-dieselben-woerter.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-166 · Wiki und Endpunkt-Karte nennen die entfernte Kapitel-Route

## Ist-Stand (belegt)

Die `Chapter`-Entität ist **entfernt** — `grep -rn "class Chapter" backend/Pugling.Api/Models/` findet
nichts, und seit B-106 hängt jede Übung zwingend an einer Lehrwerk-`SeriesUnit`
(`backend/Pugling.Api/CLAUDE.md` → „Lern-Katalog"). Die lebende Route lautet:

```text
api/v1/creator/textbook-series/{seriesId}/units/{seriesUnitId}/<typ>
```

Drei Dateien führen dagegen weiter `subjects/{id}/chapters/{id}/…`:

| Datei | Vorkommen |
|---|---|
| `wiki/03-uebungstypen.md` | 2 (u. a. das „Vollständige Beispiel" in Abschnitt 4) |
| `docs/endpunkt-beziehungen.md` | mindestens 1 |
| `docs/backlog/B-80-tags-geben-fremde-konfiguration-preis.md` | mindestens 1 |

## Warum das mehr wiegt als ein Tippfehler

`docs/endpunkt-beziehungen.md` ist die Datei, die die Root-`CLAUDE.md` als **Einstieg** benennt („Erst die
Wissenskarte, dann breit suchen"). Eine tote Route dort führt jede Sitzung, die dem Rat folgt, an eine
Adresse, die 404 liefert — und das ausgerechnet an der Stelle, die Tokens sparen soll. Das Wiki-Beispiel
ist zusätzlich zum Kopieren gedacht.

Der Backlog-Eintrag B-80 ist ein **Zeitzeuge** und bleibt, wie er ist: dort war die Route zum Zeitpunkt
der Aufnahme richtig, und `## Verlauf` ist append-only.

## Offene Punkte

1. Wie weit reicht es wirklich? Empfehlung: nicht nur auf `chapters` greppen, sondern auch auf
   `chapterId` und `Kapitel` — der Begriff kann als Prosa überlebt haben, wo die Route schon stimmt.
   (Ein solcher Fall ist beim Finden schon aufgefallen und in B-163 mitkorrigiert: `README.md` nannte
   „Fächer, Kapitel" in der Aufzählung dessen, was die App kann.)
2. Ersetzen oder umschreiben? Empfehlung: **umschreiben**. Ein Beispiel braucht eine `seriesId` und eine
   `seriesUnitId`, die es vorher irgendwo herbekommt — eine reine Pfad-Ersetzung erzeugt ein Beispiel,
   das niemand nachvollziehen kann, weil die beiden Ids aus der Luft fallen.
3. Sollte ein Tor das halten? Empfehlung: **prüfen, nicht annehmen** — es gibt bereits einen
   Client-Routen-Wächter (`docs/codequalitaet-gates-plan.md`), und dessen Erfahrung war, dass halbe
   Parser eine Rot-Liste brauchen. Ein Wächter, der Markdown nach Routen absucht und gegen das
   OpenAPI-Dokument hält, ist attraktiv und genau darum vor dem Bauen zu vermessen.

## User Story

Als **jemand, der dem Rat der Root-`CLAUDE.md` folgt** („erst die Wissenskarte, dann breit suchen"), möchte
ich in `docs/endpunkt-beziehungen.md`, im Wiki und in den Tutorials nur Routen und Katalogebenen lesen, die es
gibt – damit ich nicht an eine 404-Adresse geschickt werde und nicht nach einer Entität suche, die B-106
entfernt hat.

## Akzeptanzkriterien

1. `grep -rn "chapters/\|chapterId" wiki docs --include=*.md` trifft außerhalb von `docs/backlog/`,
   `docs/pm-sitzung-*`, `docs/anmerkungen/` (Zeitzeugen) und `docs/api-design-bewertung.md` (historische
   Messung) **keine** Zeile mehr.
2. Die Prosa-Hierarchie „Fach → Kapitel → Übung" steht in keiner lebenden Doku und keinem Code-Kommentar
   mehr; `Kapitel` bleibt nur, wo es das Freitext-Feld `currentChapter` des Lehrbuchs oder einen Unit-Namen
   („Kapitel 1 – Die Zelle") meint. Fundstellen mit `Datei:Zeile` in Entscheidung 2.
3. Die Swagger-sichtbare Beschreibung von `AuthoringRoute` (`ExerciseTypeManifest.cs`) nennt die lebende
   Route; `docs/openapi/v1.json` ist aus dem Testlauf nachgezogen.
4. `dotnet build` sauber (Kommentare), Markdown-Lint grün.

## Entscheidungen

Gegrillt im Nachtlauf 2026-10-23 (Freigabe 1, `art: Defekt`).

1. **Der Ist-Stand der Story war zu zwei Dritteln überholt** – die drei genannten Dateien tragen die Route
   nicht mehr: `wiki/03-uebungstypen.md` und `docs/endpunkt-beziehungen.md` wurden am 2026-10-17 korrigiert
   (`9787c66` „Doku gegen den Code: Katalog ohne Chapter", `cc3ac19` „Doku ohne Chapter-Routen"), B-80 bleibt
   als Zeitzeuge. Begründung: gegrept, nicht geglaubt (Punkt 1 der Story). Übrig waren **andere** Stellen,
   die der breitere Grep (`chapterId`, `Kapitel`) findet. **Kosten:** keine.
2. **Fundstellen, einzeln angesehen und umgeschrieben** (nicht pauschal ersetzt, Punkt 2):
   `wiki/08-erweitern.md:135-136` (Code-Beispiel `Check(int subjectId, int chapterId, …)` → die Signatur aus
   `ExerciseControllers.cs:361`, mit `seriesId`/`seriesUnitId`/`ct`), `:213` („Fächer/Kapitel/Übungen");
   `docs/tutorial-supervisor.md:307` (`chapterId` im Beispiel-JSON → `seriesUnitId`, so heißt das Feld in
   `KeyResultResponse`), `:316-317` (Scope-Felder und die Enum-Namen `Subject`/`SeriesUnit`/`Exercise` statt
   `subject`/`chapter`/`exercise`); `docs/endpunkt-beziehungen.md:419`, `:431`, `:450-452`
   (Fach/Kapitel/Übung → Fach/Unit/Übung; die Restrict-Regel gilt für `SeriesUnit`, `PuglingDbContext.cs:683`);
   `docs/grundprinzip.md:21`, `:34` (nannte `Chapter` als Entität), `:102`; `docs/rollen-doku.md:18`, `:23`;
   `wiki/09-llm-kochbuch.md:56`; `frontend/CLAUDE.md:82` und `docs/vater-informationsarchitektur-plan.md:120`
   (Query `?subjectId=&chapterId=` → die lebenden Parameter aus `createExerciseHref`,
   `VaterLehrwerke.tsx:100`: `seriesId`/`seriesUnitId`; `frontend/src` kennt `chapterId` nur noch als
   „ex-" in einem Kommentar, `VaterZiele.tsx:121`). Code-Kommentare (englisch): `ExerciseTypeManifest.cs:37` (Route im
   Swagger-Text), `AuthAccess.cs:15`, `ApiRoutes.cs:20`, `ExerciseGrantsController.cs:12`,
   `ExerciseCatalogController.cs:131` (Standard-Sortierung). **Stehen gelassen:** `docs/tutorial-creator.md:41`
   und `:99` (sagen ausdrücklich, dass es kein `Chapter` mehr gibt), `:165/175` (Unit-Label „Kapitel 1 – Die
   Zelle" ist Inhalt), `docs/db-struktur-umbau-plan.md` und `docs/codequalitaet-gates-plan.md` (Historie).
   **Kosten:** sichtbar nur in Doku und Swagger-Text, kein Verhalten.
3. **Zwei Funde werden eigene Stories, nicht Anhang** (README: „ein Fund beim Bauen"): `docs/REST/Creator.http`
   fährt die tote Route in **ausführbaren** Requests (`:62`, `:85`, `:109`) – das ist ein Umbau des Tutorials
   mit Reihe/Unit-Anlage, nicht eine Wortersetzung → **B-238**; die Konsolenhilfe des KI-Creators bietet
   `--chapter <id>` an (`AgentCommands.cs:459`) – dahinter hängt Verhalten, das erst zu lesen ist → **B-237**.
   `docs/REST/README.md:15` („Fach → Kapitel → Vokabelübung") beschreibt genau diese Datei und wird mit B-238
   wahr, nicht hier. **Kosten:** B-166 bleibt kleiner als sein Grep; das Ziel (Wissenskarte, Wiki, Tutorials
   ohne tote Route) ist ohne beide erfüllt.
4. **Kein Wächter** (Punkt 3). Begründung: die zwei verbliebenen Routen-Stellen fanden sich mit einem Grep;
   ein Markdown-Routen-Parser gegen OpenAPI wäre ein halber Parser mit Rot-Liste (B-40) für einen Fall, der
   seit dem 2026-10-17 kaum noch Masse hat. **Kosten:** der nächste Umbau einer Route muss die Doku wieder
   per Grep finden.

## Schätzung

**Größe XS** (Anker B-02: Sätze plus Beleg). `wo: doku`, keine Migration, kein Vertrag (ein Doc-Kommentar
im Contracts-Projekt ändert kein Feld).

**Angriffsplan:** Grep-Sweep → Doku-Stellen umschreiben → fünf Code-Kommentare → `dotnet test` (zieht
`docs/openapi/v1.json` nach) → Lint → Reviewer → Commit.

**Risiko:** keines im Verhalten; das Wiki-Beispiel könnte in einem anderen Detail veraltet sein – nur die
Signatur ist gegen `ExerciseControllers.cs:361` geprüft.

**Testweg:** Grep-Probe aus AK 1 (vorher 2 Treffer in lebender Doku: `wiki/08:135`, `tutorial-supervisor:307`;
nachher 0), Build, Lint. Rollengang: der PM liest die Wissenskarte – kein Browser nötig.

## Verlauf

- **2026-10-23** — **abgenommen** (Nachtlauf, Sprint 2). Reviewer (allgemeiner Agent): Wiki-Signatur gegen
  `ExerciseControllers.cs:350-363`, `seriesUnitId` und die Enum-Namen gegen `GoalDtos.cs`/`ObjectiveBaseTypes.cs`
  und `JsonStringEnumConverter` ohne Naming-Policy (`Program.cs:37`), Restrict gegen `PuglingDbContext.cs:683`,
  Routen-Text gegen `ExerciseRoutes.Base`, Standard-Sortierung gegen den `_ =>`-Fall – alles bestätigt.
  **Zwei Funde mit Codewirkung (Zähler 1 und 2 des Sprints):** (1) **AK 2 war behauptet, nicht erfüllt** –
  mein Sweep über Backend-Kommentare war mit `head -8` abgeschnitten; elf weitere Stellen in acht Dateien
  (`Program.cs:211`, `ChildLearnProgressService.cs:208/209/241/242/275/360`, `KeyResultsController.cs:10`,
  `ObjectiveEntities.cs:47`, `PuglingDbContext.cs:941`, `AdultLifecycleTests.cs:15`, `Seed.cs:430/558`,
  `DraftRules.cs:45`) umgeschrieben; stehen gelassen, weil wahr: `TextbooksController.cs:13` u. a.
  (Freitext-Kapitel des Lehrbuchs), die Tag-Beispiele „chapter 5", `ObjectiveBaseTypes.cs:60` („series unit
  (chapter)" als Glosse), `Seed.cs:1016` und `ConventionGuardTests.cs:78` (Historie), `AgentCommands.cs`
  (→ B-237). (2) **Hohle Probe** `ObjectiveTests.cs:167-173`: „ClassTestGrade mit Kapitel-Scope → 400"
  schickte `chapterId = 1` – kein Vertragsfeld, also `unknown_field` vor der Scope-Regel; jetzt echte Unit
  (`TestApi.CreateSeriesAndUnitAsync`), Feld `seriesUnitId`, Zusicherung auf `code == validation_error`.
  **Rote Probe (erwartet/gemessen):** Mutante ohne `seriesUnitId is not null` in `ObjectiveService.cs:79` →
  **1 rot von 1**; Original 1/1; Suite danach **877/877**. Grep AK 1: **0** (vorher `wiki/08:135-136`,
  `tutorial-supervisor:307/316`, `vater-informationsarchitektur-plan:120`, `frontend/CLAUDE.md:82`).
  Reviewer-Nit → **B-239** (deutsche `/// <summary>` im Wiki-Beispiel). **Rollengang:** der Leser der
  Wissenskarte – Grep und Lesen, kein Browser. Commit: siehe Nachtrag.
- **2026-10-23** — gegrillt, geschätzt und gebaut im Nachtlauf (Sprint 2, Freigabe 1). Ist-Stand berichtigt
  (Entscheidung 1), zwei Folge-Stories B-237/B-238.
- 2026-08-13 · Aufgenommen. Beim Nachziehen der Typ-Label-Prosa für B-163 gefunden: dieselbe Datei trug
  neben dem alten Label auch eine Route auf eine Ebene, die es nicht mehr gibt. Das Label war in Scope,
  die Route nicht.
