---
tags: [typ/konzept, bereich/katalog, rolle/creator]
---

# Design: Übungs-Meta-Beschreibung & Versionierung

Stand: 2026-07-04. Grundlage für die Entscheidung, wie sich neue Lernmethoden anbinden lassen
und wie das (neu zu bauende) Frontend Übungen darstellt, ohne für jeden Typ manuell erweitert
werden zu müssen. **Der Manifest-Schritt ist umgesetzt** (siehe [Umgesetzt](#umgesetzt)).

## Problem

Zwei zusammenhängende Wünsche:

1. **Versionierung einzelner Übungen** – Übungstypen entwickeln sich weiter (neue Felder,
   geändertes Verhalten). Der Client soll wissen, mit welcher Schema-Variante er es zu tun hat,
   und die Kommunikation soll cachebar sein.
2. **Selbstbeschreibende Übungen** – Idee: Das Backend liefert zu jeder Übung eine generische
   „Frontend-Beschreibung" (Template/Meta-JSON), sodass das Frontend die Übungsseite
   **automatisch** rendern kann. Hintergrund: das System soll sich um beliebige Lernmethoden
   erweitern lassen (heute: Vokabeln, Listen, Karteikarten/Matching, Cloze, Birkenbihl …),
   ohne dass jeder neue Typ zwingend Frontend-Handarbeit bedeutet.

Beide Muster haben etablierte Namen: **Server-Driven UI (SDUI)** für den selbstbeschreibenden
Teil, **Schema-Versionierung** für den Evolutions-Teil.

## Ist-Zustand

- Ein Controller je Übungstyp, jeder erbt CRUD aus
  [`ExerciseControllerBase<TConfig>`](../backend/Pugling.Api/Controllers/Creator/ExerciseControllerBase.cs);
  Route + `Type` + Tag sind das Einzige, was ein konkreter Controller setzt
  ([ExerciseControllers.cs](../backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs)).
- Die typ-spezifische Config ist als JSON gespeichert, im API aber voll typisiert
  (`ExercisePayload<TConfig>` / `ExerciseResponse<TConfig>`); jeder Typ bekommt so ein eigenes
  Swagger-Schema ([ExerciseConfigs.cs](../backend/Pugling.Api/Models/ExerciseConfigs.cs)).
- Auswertung ist **server-autoritativ** über `/check`-Endpunkte (`CheckResult`); nicht jeder Typ
  hat eine Prüfung (Birkenbihl bewusst ohne), und `ArithmeticDrill` erzeugt Aufgaben erst per
  `/generate` und prüft über denselben `Seed` beim `/check`.
- `ExerciseControllerBase` ist `[Authorize(Roles = Roles.Vater)]` – das ist die **Autoren-Sicht**;
  sie liefert die **volle Config inkl. Lösungen** (`Gap.Answer`, `Question.Answer`, `MatchPair.Right` …).
- Die **Sohn-Sicht ist getrennt und bereits antwortfrei**: Gespielt wird über die positionsbezogenen
  Controller unter `study-plans/{planId}/positions/{positionId}/…`. `PositionPracticeController` und
  `PositionTestsController` geben Lösungen **stufenabhängig** frei (die Position/Fahrplan-Stufe entscheidet,
  ob Reveal/Choices/AnswerLength/Hint sichtbar sind). Das ist server-autoritativ: Der Client sendet nur
  Antworten, der Server bewertet.

Beobachtung: Heute kostet ein neuer Typ = neuer Controller **plus** neue Frontend-Komponente.
Das Wissen über einen Typ ist zudem verstreut: `ExerciseType` (Katalog) ↔ `LearningMethod` (Lehrplan)
sind zwei Enums ohne explizite Brücke; welcher Play-Controller, welches Submit-Format, ob `/check`
oder `/generate` oder gar keine Prüfung existiert – all das ist implizit über ~4 Stellen verteilt.

## Verworfene Option: „Volles UI aus JSON" (reines SDUI)

Ein generisches Meta-Schema, aus dem das Frontend *jede* Übung vollständig rendert, funktioniert
gut für die **formular-artigen** Typen (Vocabulary, Cloze, List, Matching, Grammar, Arithmetic,
Translation, Reading) – und scheitert an genau den Typen, die das System interessant machen:

- **Birkenbihl** – Wort-für-Wort-Dekodierung unter jedem Wort, Hover, kein `/check`.
- **Listening** – Audio-Player (ggf. Transkript-Toggle).
- **ArithmeticDrill** – kein gespeicherter Aufgabensatz; Client muss `/generate` rufen, den `Seed`
  halten und beim `/check` zurückgeben.

Sobald diese ins generische Schema gezwungen werden, wächst das „Widget-Vokabular"
(`word-hover`, `audio-player`, `keypad`, `generate-then-check` …), bis das Frontend **doch jeden
Typ einzeln implementiert** – nur verkleidet als Schema-Interpreter. Die Komplexität wird
verschoben, nicht entfernt. Zusätzlich verwässert der server-autoritative Anti-Cheat-Gedanke,
weil ein generischer Renderer beginnt, „richtig/falsch"-Logik im Client zu tragen.

→ **Nicht verfolgen.** Das Meta muss nicht *jedes Pixel* beschreiben.

## Entscheidung (greenfield): genau ein Manifest, sonst nichts

Da nichts ausgeliefert ist und **keine Abwärtskompatibilität** gewahrt werden muss, ist die
optimale Lösung *minimal*: eine einzige Ergänzung, die die heute verstreute Verdrahtung bündelt.
Der eigentliche Schmerz ist nicht die Darstellung (die Play-Controller sind bereits sauber), sondern
dass das Wissen über einen Typ implizit über mehrere Stellen verteilt ist.

### Das Manifest als Single Source of Truth

Ein Endpunkt liefert *einmal pro Typ* die Brücke zwischen Katalog, Lehrplan, Play-Route und Renderer:

```text
GET /api/v1/creator/exercise-types
```

```jsonc
{
  "type": "Cloze",               // ExerciseType (Katalog)
  "method": "Cloze",             // LearningMethod (Lehrplan) – die explizite Brücke
  "authoringRoute": "cloze",     // .../chapters/{}/cloze  (Vater-CRUD)
  "playRoute": "tests",          // study-plans/{}/positions/{}/tests (Sohn-Play)
  "renderer": "cloze",           // welche Frontend-Komponente rendert
  "checkMode": "server",         // server | generated | none
  "submitShape": "positional",   // Aufbau des Submit-/Check-Bodys
  "capabilities": ["wordbank", "hints"],
  "label": { "de": "Lückentext" },
  "schemaVersion": 1             // nur hier, nicht in den Entities (s. u.)
}
```

Nutzen: Das Frontend erhält Routing, Submit-Format und Discovery aus **einer** Quelle; ein neuer
Lernmethoden-Typ wird an *einer* Stelle deklariert statt an ~4 implizit konsistent gehalten. Der
konkrete Renderer bleibt **handgebaut pro `renderer`-Id** – das muss er, weil die Play-Sicht
stufenabhängig unterschiedlich viel aufdeckt (siehe `TestItem`/`ClozeTestText`). Das Manifest
beschreibt also *welchen* Renderer + *welche* Datenform, nicht das UI.

Umsetzung: so viel wie möglich aus vorhandenen Enums/Attributen ableiten, damit keine zweite
Wahrheit driftet; ein Konsistenz-Test erzwingt „jeder `ExerciseType` → genau ein Manifest-Eintrag
(+ Controller)".

### Bewusst *nicht* gebaut

- **Voll-SDUI / UI-aus-JSON** – verschiebt Komplexität, kollidiert mit der stufenabhängigen
  Play-Logik. Verworfen (siehe oben).
- **Antwortfreie Play-Projektion** – **existiert bereits** (`TestItem`, `ClozeTestText`, …) und ist
  besser als eine generische Schicht. Keine Baustelle.
- **Instanz-Caching (`updatedAt`/ETag)** – für eine Ein-Familien-App der geringste Nutzen. Später.
- **Per-Entity-Versionsspalten + Migrations-Maschinerie** – reine Zeremonie, solange nichts
  ausgeliefert ist. Bewusste Abweichung vom ursprünglichen Wunsch „Versionssystem".

### Zur Versionierung (bewusste Entscheidung)

Ohne Kompatibilitätszwang ist echte Schema-Versionierung verfrüht: Config-Schemas dürfen frei
geändert werden. Der ehrliche Kompromiss ist **ein einziges `schemaVersion`-Feld im Manifest**
(startet bei 1, lebt nur dort) als Verzweigungspunkt für später – **keine** Version an den Entities,
**keine** Migrationslogik. Die echte Versionierung kommt am Tag der ersten inkompatiblen Änderung
*mit* einem Client, der veralten könnte. Verhältnis zur [API-Versionierung](architektur-resumee.md):
`api/v1` bleibt der Makro-Bruchmechanismus (v2 parallel); `schemaVersion` ist ein späterer Mikro-Hebel.

## Ehrliche Einordnung

Für eine Ein-Familien-App ist der Manifest-Nutzen **saubere interne Architektur + Erweiterbarkeit**
(„das System soll um Lernmethoden wachsen"), *nicht* Deployment-Entkopplung. Das ist der konkrete
Ausdruck des API-First-Prinzips für Lernmethoden – aber nicht mehr als das.

## Vorgehen

Ein einziger, kleiner Schritt: Registry-Record + Endpunkt + Konsistenz-Test. Rückwärtskompatibel
(rein additiv), unblockt das Wiring des Frontend-Rebuilds sofort, ohne Verhaltensänderung.

## Umgesetzt

Stand 2026-07-04, rein additiv (kein bestehendes Verhalten geändert):

- **Modell** ([Models/ExerciseTypeManifest.cs](../backend/Pugling.Api/Models/ExerciseTypeManifest.cs)):
  `ExerciseCheckMode` (`None | StudyPlanTest | CatalogCheck | CatalogGenerateCheck`) und der Record
  `ExerciseTypeManifest`. Felder: `type`, `label`, `renderer`, `schemaVersion` (überall 1), `authoringRoute`,
  `checkMode`, `playRoute`, `method`, `capabilities`. `schemaVersion` lebt **nur** hier.
  > **Seit dem Registry-Umbau:** Das Manifest wird **nicht** mehr zentral hartkodiert (früher
  > `ExerciseManifests.All` je `ExerciseType`-Enum-Wert), sondern liegt an jeder Typklasse
  > (`IExerciseType.Manifest`); die `ExerciseTypeRegistry` aggregiert sie (`registry.Manifests`).
  > `type` ist der String-`Key` der Typklasse.
- **Endpunkt** ([Controllers/Creator/ExerciseTypesController.cs](../backend/Pugling.Api/Controllers/Creator/ExerciseTypesController.cs)):
  `GET api/v1/creator/exercise-types` (Liste) und `.../{type}` (Einzel). `[Authorize]` ohne Rollen-
  einschränkung – kindneutrales Manifest, das beide Rollen lesen dürfen.
- **Tests** ([ExerciseTypeManifestTests.cs](../backend/Pugling.Api.Tests/ExerciseTypeManifestTests.cs)):
  Vollständigkeit (jeder registrierte Typ → genau ein Manifest, `Key == Manifest.Type`) + Prüfmodus-Invarianten
  (StudyPlanTest ⇔ `playRoute`+`method` gesetzt, sonst beide null); Erreichbarkeit für Vater und
  Sohn; Einzelabruf; unbekannter Typ-Schlüssel → 404 (Controller-Guard, kein Enum-Model-Binding mehr).

Beispiele der gewählten Zuordnung: Vocabulary→`flashcards`/StudyPlanTest/`tests`,
Cloze→`cloze`/StudyPlanTest/`tests`, Matching→`matching`/StudyPlanTest/`tests`,
Arithmetic+ArithmeticDrill teilen `renderer:"arithmetic"` (CatalogCheck bzw. CatalogGenerateCheck),
Grammar+Translation teilen `renderer:"prompts"`, Birkenbihl/Reading/Essay/Listening = `None`.

Noch offen (bewusst nicht in diesem Schritt): echte Schema-Versionierung, ETag/Caching, Voll-SDUI.

## Offene Fragen

- **`checkMode`/`submitShape`-Taxonomie**: Reichen `server | generated | none` und
  `positional | set | free`? An den bestehenden Submit-/Check-DTOs (`CheckAnswersDto`, `CheckDrillDto`,
  `CheckListDto`, `PositionTestsController.SubmitDto`) verifizieren.
- **Manifest-Quelle**: aus Attributen/Enums ableiten (eine Wahrheit) vs. explizite Registry (mehr
  Kontrolle, Drift-Risiko) – tendenziell abgeleitet, per Test abgesichert.
- **`ExerciseType` ↔ `LearningMethod`**: nicht jeder Katalog-Typ hat (heute) einen Play-Weg
  (z. B. Birkenbihl ohne `/check`). Das Manifest muss „kein Play-Weg" ausdrücken können.
- **i18n der Labels**: statisch im Manifest (`label.de`) vs. eigener Übersetzungsweg.
- **Verhältnis zum Lehrplan-Generator** (siehe [[uebungs-metadaten]]): Das Manifest ist
  kindneutral wie der Katalog – passt zur Vorfilter-Logik im
  [ExerciseCatalogController](../backend/Pugling.Api/Controllers/Creator/ExerciseCatalogController.cs).
