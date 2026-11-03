---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/doku]
aliases: [falscher Absatz im OpenAPI-Text, bündig dann eingerückt]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Sprint 8 (vierter Nachtlauf 2026-10-23)
unverifiziert: false
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-11-03"
entgangen_bei: [B-147]
---

# B-260 · Der OpenAPI-Normalisierer setzt einen Absatz mitten in einen Satz

Gefunden in der Nachschau zu Sprint 8. `OpenApiProseNormalizer` (`backend/Pugling.Api/OpenApi/OpenApiProseNormalizer.cs`,
die Regel „eingerückte Zeile nach bündiger = neuer Absatz") greift auch, wenn ein mehrzeiliges `<summary>`/`<param>`
seinen Text **auf der Zeile des Tags** beginnt: der Generator liefert dann die erste Zeile bündig und die Folgezeilen
eingerückt – umgekehrt wie bei `<para>`. Selbst nachgemessen am geparsten `docs/openapi/v1.json`:
`UpdateMyAccountDto.pin` = „…so an account can\n\nbe deliberately deactivated…" (Quelle `AuthDtos.cs:42-43`). Laut
Prüfer drei weitere Fälle: `Gender`, `SetExerciseSharingDto.executePublic`, `UsageResponse.otherLearnersCount`
(dort reißt der Umbruch zusätzlich einen Listenpunkt). Kein Fall in `OpenApiProseNormalizerTests` deckt die Form
„bündig, dann eingerückt", und das Dokument-Tor sieht sie nicht (weder Einrückung noch Tag).

Vorschlag für die rote Probe: ein Fall `"a later\n            study plan"` → erwartet weicher Umbruch. Zu entscheiden beim
Ausformulieren: die Regel auf „vorige Zeile ebenfalls eingerückt oder erste Zeile eingerückt" schärfen, oder die
Quellen so umformulieren, dass der Text nicht auf der Tag-Zeile beginnt (dann gehört ein Quelltext-Tor dazu, siehe
Retro-Vorschlag von Sprint 8). **Nicht im laufenden Sprint behoben:** der Fund liegt im Increment von Sprint 8; nach
Limit 2 endet der Lauf an diesem Sprint-Ende.

## User Story

Als **Creator**, der die API-Doku liest, möchte ich, dass ein Satz nicht mitten drin in zwei Absätze zerfällt, damit ich
Feldbeschreibungen wie die der PIN am Stück lesen kann.

## Ist-Stand am Code (Lauf Q, Stand `a25a1a6`)

- `OpenApiProseNormalizer.Dedent` (`backend/Pugling.Api/OpenApi/OpenApiProseNormalizer.cs:133-141`): „eingerückte Zeile
  nach bündiger = neuer Absatz".
- **Rohstrings gemessen** (Normalisierer abgeschaltet): eine bündige Zeile gefolgt von eingerückten kommt in drei Formen vor
  – (a) erste Zeile eines Texts, der auf der Tag-Zeile beginnt (`New login PIN … can\n            be deliberately…`),
  (b) Folgezeile eines `<para>` (die Regel), (c) jeder Listenpunkt nach dem ersten (`\n* For a creator … both\n
  lists`). Nur (b) ist ein Absatz.
- **Zehn** falsche Absätze im Dokument (u. a. `UpdateMyAccountDto.pin`, `UsageResponse.otherLearnersCount`,
  `SetExerciseSharingDto.executePublic`, `Gender`, drei Sortier-Parameter).

## Die echte Lücke

Die Regel kannte nur Form (b) und deutete (a) und (c) als Absatz.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Ein Absatz beginnt nur nach einer bündigen Zeile, die selbst Folgezeile ist** – weder die erste Zeile des Texts noch
   ein Listenpunkt (`* `). *Kosten:* ein einzeiliger `<para>` direkt nach Text auf der Tag-Zeile wird ein weicher Umbruch
   (im Dokument heute nirgends; die Gegenrichtung stand schon als bekannte Grenze im Kommentar).
2. **Kein Quelltext-Tor**, anders als der Vorschlag der Idee: die Regel lässt sich im Normalisierer richtig herstellen, und die
   Quellform „Text auf der Tag-Zeile" ist im Repo der Normalfall.

## Akzeptanzkriterien

1. Text auf der Tag-Zeile mit Folgezeile bleibt ein Absatz.
2. Die Folgezeile eines späteren Listenpunkts bleibt im Punkt.
3. Ein `<para>` nach einer `<para>`-Folgezeile beginnt weiter einen Absatz.

## Schätzung

`S`, backend. Testweg: `OpenApiProseNormalizerTests`, Diff von `docs/openapi/v1.json` je geänderter Zeile nachgelesen.

## Verlauf

- **2026-10-23** — angelegt in der Nachschau zu Sprint 8 (vierter Nachtlauf); ein Fall selbst nachgemessen, drei vom Prüfer.
- **2026-11-03** — Lauf Q, Teil 1: ausformuliert, autonom gegrillt (Freigabe 1), geschätzt, gebaut. Rote Probe: zwei neue
  Fälle **2 failed** (erwartet 2). Nach dem Fix `OpenApiProseNormalizerTests` + `ContractDocumentTests` **13/13**; im
  Dokument genau **10** Zeilen geändert, jede nachgelesen: alle zehn ein Umbruch mitten im Satz, zwei davon nach einem
  Satzende („(substring).", „`created`.") – in der Quelle ohne `<para>` und ohne Leerzeile, also kein gewollter Absatz.
  Mutanten: „ohne Listenpunkt-Ausnahme" **1 failed** (AK 2), „ohne Erste-Zeile-Ausnahme" **1 failed** (AK 1; eine erste
  Fassung übersetzte nicht – unbenutzte Variable unter `TreatWarningsAsErrors` – und zählt nicht), „nie ein Absatz" **1
  failed** (AK 3, bestehender Fall).
- **2026-11-03** — Review `pugling-reviewer`: kein 🔴/🟡 (Prüfpunkte im Protokoll); 🟢 ohne Zählung behoben bzw. benannt. Abnahme Lauf Q, Teil 1: volle Suite **1056/1056**, Solution-Build ohne Warnung, E2E-Gesamtlauf **107 passed** (`exit=0`, nach dem letzten Edit). Rollengang: Creator – sein Weg ist das Vertragsdokument, jede geänderte Zeile von `docs/openapi/v1.json` nachgelesen; Vater und Sohn Regressionszeugen (nur Doku-Text und ein Normalisierer, kein Pfad von ihnen; ihre Specs grün). **Abgenommen**, Commit siehe Protokoll ([Lauf Q](../pm-sitzung-2026-11-03-nacht.md)).
- **2026-11-03** — Nachschau in Lauf Q, Teil 2 (frischer `pugling-reviewer`, `git show 1067b55`, read-only): **sauber.** Geprüft: AK 1/3 und Entscheidung 1 je mit rotem Fall; kein anderer String geändert als die zehn; Text nach `</list>` kommt als weicher Umbruch. 🟢 Mutante „jedes `*`" war unsichtbar → Fall ergänzt, überflüssiger Term `previousIndented` gestrichen (in B-366 gemessen).
