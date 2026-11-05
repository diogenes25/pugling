---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/creator]
aliases: [null-Element in Config-Liste 500]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-05.md#lauf-x
unverifiziert: false
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-11-05"
entgangen_bei: [B-391]
---

# B-397 · Ein `null`-Element in einer Config-Liste endet in einer 500

`[Required]` (implizit für nicht-nullbare Member) prüft keine **Elemente** einer Liste, und System.Text.Json nimmt `[null]` an.
Gemessen am 2026-11-05 (temporäre Fälle im Theory-Test von B-391): `POST …/translation` mit `"items":[null]`, `…/birkenbihl` mit
`"sentences":[null]` und `…/cloze` mit `"gaps":[null]` → **500** (`NullReferenceException` in den Validatoren, etwa
`ExerciseControllers.cs:350`, `:420`, `:473`). Erwartet: `400 validation_error` mit dem Pfad.

Zu entscheiden beim Ausformulieren: ein Wächter im Basis-Controller vor `ValidateConfigAsync` (eine Regel für alle Configs) oder
`RespectNullableAnnotations` global (breiter, erst messen). Die Summary des Regressionstests aus B-391 (`UnknownReferenceCodeTests.cs`
um `:283`) wird mit berichtigt; die `[null]`-Fälle kommen in denselben Theory.

## User Story

Als **Creator** (oder KI-Agent) möchte ich auf einen Payload mit `null` als Listenelement eine `400 validation_error` mit dem Pfad
bekommen – wie für ein `null` als Member –, nicht eine 500 und nicht ein stilles `201`.

## Entscheidungen

1. **(Defekt, im Lauf gegrillt)** Ein **globaler** Aktionsfilter (`NullElementGuardFilter`, Order −2001, vor dem
   `ModelStateInvalidFilter`) statt eines Wächters nur im Übungs-Basis-Controller. Begründung: die Lücke ist die der Modellvalidierung,
   nicht der Configs – gemessen auch an `POST …/vocabulary/{id}/tags` (`"tags":["Tiere",null]` → 500 aus `Trim()`). Er läuft über alle
   `[FromBody]`-Argumente, deren Typen im Vertragsprojekt liegen, und meldet jedes `null`-Element einer Liste, deren Elementtyp
   nicht-nullbar deklariert ist (`NullabilityInfoContext`). `List<string?>`/`List<int?>` bleiben erlaubt. *Kosten:* ein Vertragstyp, der
   `null`-Elemente meint, aber nicht-nullbar deklariert, antwortet jetzt `400` – das ist die richtige Folge (der Vertrag lügt sonst).
2. `RespectNullableAnnotations` wurde **nicht** genommen: es prüft Member, nicht die Typargumente einer Liste.

## Akzeptanzkriterien (je Fall)

| Weg | Liste | Form | Erwartet |
|---|---|---|---|
| Übung `POST` translation | `items` | `[null]` | `400`, `errors["config.items[0]"]` |
| Übung `POST` birkenbihl | `sentences` | `[null]` | `400`, `config.sentences[0]` |
| Übung `POST` cloze | `gaps` | `[null]` | `400`, `config.gaps[0]` |
| Übung `POST` cloze | `wordBank` (Liste selbst nullbar) | `["bin",null]` | `400`, `config.wordBank[1]` (vorher **201**, still gespeichert) |
| Vokabel-Tags `POST` (kein Config) | `tags` | `["Tiere",null]` | `400`, `tags[1]` |
| Vokabel-Batch `POST` (Body ist die Liste) | Body | `[null]` | `400`, `[0]` |
| Vokabel-Batch `PATCH` (Body ist die Liste) | Body | `[null]` | `400`, `[0]` |
| Vokabel-Batch `POST` | `[0].tags` | `[null]` | `400`, `[0].tags[0]` |

## Schätzung

`S`, `wo: backend`, ein Filter plus Registrierung, Fälle in bestehenden Tests.

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu Lauf X, Sprint 10 (Retro Sprint 11; frischer `pugling-reviewer`, `git show 2312aaf`, 🟡)
  und **gemessen** (drei 500er). Entgangen bei B-391: dessen Verwerfung im Increment **dieses** Laufs galt nur für `null`-Member –
  **Limit 2**, Lauf X endet nach Sprint 11.
- **2026-11-05** — ausformuliert, gegrillt, geschätzt und gebaut in Lauf Y, Sprint 12 (Auftrag im Dialog: „Starte 4 Nachtläufe … kläre
  Fragen selber"). **Rote Probe** gegen den Ausgangszustand: die vier neuen Theory-Fälle (`UnknownReferenceCodeTests.cs:296-299`) –
  erwartet 4 rot, gemessen 4 rot (drei `500`, `wordBank` **`201`**). Nach dem Fix 10/10. **Mutante** Filter-Registrierung in
  `Program.cs` auskommentiert: erwartet 5 rot (vier Config-Fälle + Tags-Fall `CatalogReadDeleteTests.cs:361`), gemessen 5 rot; zurück →
  grün. Volle Suite 1103/1103 nach B-397, 1105/1105 mit B-398.
- **2026-11-05** — Review `pugling-reviewer`: 🟡 ein Body, der selbst eine Liste ist (`POST`/`PATCH …/vocabulary/batch`, vom KI-Agenten
  benutzt), lief am Filter vorbei → `[null]` gab weiter 500. **Zähler 1 von 5.** Behoben: Listen werden an jeder Stelle gleich besucht,
  die Element-Nullbarkeit des Bodys kommt aus dem Aktionsparameter. **Rote Probe** `CatalogReadDeleteTests.cs:379-381` (Theory, drei
  Fälle): erwartet 3 rot, gemessen 3 rot (alle `500`); danach grün. 🟢 Kommentar in `Program.cs` verrutscht (berichtigt), 🟢 verschachtelte
  Listen (`List<List<T>>`) nicht durchsucht – keine im Vertrag, im Remarks benannt, 🟢 kein Test gegen Fehlalarm bei `List<string?>` – kein
  solcher Vertragstyp, ohne Zählung zurückgestellt. Delta-Review: kein 🔴/🟡; 🟢 effektive Tiefe über Listen halbiert – im Kommentar
  benannt. Volle Suite 1108/1108 nach dem letzten Code-Edit.
- **2026-11-05** — **abgenommen.** Rollengang Creator als Integrationsfälle gegen den echten Host (jeder Fall ist ein Payload, wie ihn
  Agent oder Client schicken; die Oberfläche schickt keine `null`-Elemente, darum kein Klickweg). Vater und Sohn als Regressionszeugen:
  volle Playwright-Suite **115 passed** gegen ein frisch gestartetes Backend nach dem letzten Code-Edit (danach nur Kommentare). Commit:
  siehe nächster Eintrag.
- **2026-11-05** — Commit `49e773d`.
- **2026-11-05** — **Nachschau** (Lauf Y, Retro Sprint 13; frischer `pugling-reviewer`, `git show 49e773d`): geprüft (1) je Belegzeile
  Fundort und Mutante, (2) „eine Regel für jeden Body" gegen alle `[FromBody]`-Parameter (alle Vertragstypen oder `List<Vertragstyp>`,
  kein `Dictionary`, keine eigenen Binder, Agent und Frontend schicken keine `null`-Elemente), (3) `NaturalTranslation`-`null` auf allen
  Wegen (`?? ""` in beiden Handlern, Birkenbihl wird nie bewertet) und die `Gap.Answer`-Summary gegen Config- und Lückentext-Weg.
  **Entgangen:** eine Mutanten-Zeile in B-398 (kompiliert nicht) und eine verrutschte Zeilenangabe in B-397 →
  [B-399](B-399-b398-mutantenzeile-ungemessen-und-falsch.md).
