---
tags: [typ/story, status/ausformuliert, bereich/backend, bereich/doku, rolle/creator]
aliases: [Einrückung im OpenAPI-Summary, para im Summary, Swagger-Codeblock]
status: ausformuliert
prio: P3
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachtlauf Sprint 3 (2026-08-10), gefunden beim Prüfen des eigenen OpenAPI-Diffs
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
wartet_auf: ""
---

# B-147 · Ein `<para>` im `<summary>` trägt seine Quelltext-Einrückung ins OpenAPI-Dokument

## User Story

Als **Konsument der API** möchte ich die Beschreibung eines Endpunkts als Fließtext lesen — nicht als
eingerückten Block, der in einer Markdown-Anzeige womöglich als Code erscheint.

## Ist-Stand am Code

Gemessen am 2026-08-10 über `docs/openapi/v1.json`: von **323** Endpunkt-`summary`s beginnen **14** mit
Leerzeichen. Sie sind genau die, deren `/// <summary>` ein `<para>` enthält — der Generator übernimmt die
Einrückung des Quelltexts, statt sie zu normalisieren.

Beispiel (aus diesem Lauf, `SubjectsController.Delete`):

```text
"summary": "    Deletes a subject along with its exercise categories, unless a child's data points at it.\n    Two groups, split by what the delete would cost (B-144). ..."
```

Ohne `<para>` steht der Text bündig — die 309 übrigen Zusammenfassungen belegen das.

## Die echte Lücke

**Vier führende Leerzeichen sind in Markdown ein Codeblock.** Ob das sichtbar wird, hängt davon ab, ob die
anzeigende Oberfläche `summary` als Markdown rendert — das ist der Teil, der **noch nicht gemessen ist**
und beim Ausformulieren zuerst drankommt. Swagger UI rendert `description` als Markdown; ob es das mit
`summary` auch tut, wurde hier nicht geprüft.

Zwei mögliche Ausgänge, und sie führen zu ganz verschiedenen Stories:

- **Wird es gerendert**, sind 14 Endpunkt-Beschreibungen als Codeblock zu lesen — ein echter Defekt an der
  Oberfläche, die das Produkt *ist* (API-First).
- **Wird es nicht gerendert**, bleibt eine Unsauberkeit im Vertragsdokument: `docs/openapi/v1.json` ist
  eingecheckt und wird gelesen, und die Einrückung ist Rauschen im Diff.

## Verwandtschaft

Nachbar von `ConventionGuardTests.Vertrags_Dokumentation_Erreicht_Das_Dokument` (gelandet 2026-08-10):
das Tor prüft, dass ein `<para>` **innerhalb** eines Containers steht, der das Dokument erreicht. Hier
erreicht es das Dokument — nur in schlechterer Form. Dieselbe Fläche, eine Frage weiter.

**Kein `entgangen_bei`:** Der Zustand ist älter als jede der beteiligten Stories und gehört keiner
bestimmten Abnahme an; er entstand mit dem ersten `<para>` in einem `<summary>`.

## Offene Punkte

1. **Rendert die anzeigende Oberfläche `summary` als Markdown?** Zuerst messen — davon hängt ab, ob das ein
   Defekt oder eine Unsauberkeit ist. Empfehlung: an einem der 14 Endpunkte in der laufenden Swagger-UI
   nachsehen, nicht in der Dokumentation von Swagger UI nachlesen.
2. **Wo gehört die Behebung hin?** Drei Kandidaten, mit steigenden Kosten: die 14 Kommentare von Hand
   umformatieren (verrottet, der fünfzehnte kommt); ein Transformer am OpenAPI-Generator, der
   `summary` trimmt (eine Stelle, wirkt auf alle künftigen); oder `<para>` in `<summary>` ganz aufgeben und
   die Prosa nach `<remarks>` ziehen — was aber erst zu klären hätte, ob `<remarks>` das Dokument
   überhaupt erreicht. Empfehlung: der Transformer, wenn Punkt 1 „Defekt" ergibt.

## Akzeptanzkriterien (Entwurf)

1. Kein Endpunkt-`summary` in `docs/openapi/v1.json` beginnt mit Leerzeichen, und keine Folgezeile trägt
   die Quelltext-Einrückung (Zählung vorher/nachher im Verlauf).
2. Ein Absatz aus `<para>` bleibt als Absatz erkennbar – der Workaround „einzeilig ohne Satzgrenze" wird
   unnötig.
3. Die Normalisierung sitzt an **einer** Stelle (Transformer), nicht in jedem Kommentar, und ein Test hält
   sie fest.

## Verlauf

- **2026-08-10** — angelegt im Nachtlauf (Sprint 3) beim Lesen des eigenen OpenAPI-Diffs. **Bewusst nicht
  im Sprint behoben:** 12 der 14 Fälle liegen außerhalb seines Diffs, und die Frage, *ob* es sich sichtbar
  auswirkt, ist ungemessen — sie zu beantworten ist der nächste Schritt, nicht das Umformatieren.
  Zwei Fälle, die der Sprint selbst erzeugt hatte, tragen die Einrückung weiter; behoben wurde dort nur,
  was er **allein** verursacht hatte (zwei `<see cref>`, die als volle Methodensignatur im Fließtext
  landeten).
- **2026-09-01** — **Drei weitere Fälle**, aus B-45: die neuen `<para>`-Blöcke an `ExerciseSummary`
  (`ExerciseCatalogDtos.cs`), `ExercisePayload` (`ExerciseAuthoringDtos.cs`) und
  `Exercise.RewardPoints` (`LearnEntities.cs`) tragen ihre Quelltext-Einrückung ins Dokument, ebenso die
  Konstanten-Doku in `PlanPositionsController.cs`. Der `frontend-reviewer` hat es beim Lesen des
  OpenAPI-Diffs gemeldet — sichtbar als 12 Leerzeichen am Zeilenanfang und fehlender Absatzumbruch.
  **Bewusst nicht dort behoben:** B-45s Ziel ist ohne die Formatfrage erfüllt, und die Antwort auf „womit
  formatiert man das richtig" ist genau diese Story. Zahl im Text nicht nachgezogen — sie war schon beim
  Anlegen eine Momentaufnahme; wer den Sweep fährt, zählt neu.
- 2026-09-02 · **Gemessen im zweiten Nachtlauf — und der Befund ist größer als gedacht: Einrückung und
  Absatztrennung sind DASSELBE.** Beim Schreiben eines neuen `<para>` in `ExerciseCatalogDtos.cs` sind
  beide Formen gefahren worden:
  - **mehrzeilig** → der Absatz wird getrennt, trägt aber 12 führende Leerzeichen in jede Folgezeile.
    In Swagger UI ist das ein **Codeblock** (Markdown: vier Leerzeichen genügen) — der Ausgang ist also
    nicht offen, sondern absehbar hässlich.
  - **einzeilig** → keine Einrückung, aber auch **keine Trennung**: der Text klebt am Vorsatz
    (`…per keystroke in the list.The SuggestedGoalPoints figure is…`).

  Eine eingeschobene leere `///`-Zeile hilft **nicht** (probiert, kein Effekt im erzeugten Dokument).
  Der `\n` im Dokument stammt also nicht von `<para>`, sondern **ist** der Zeilenumbruch der Quelle —
  darum kommt er nur zusammen mit der Einrückung. Wer das behebt, bekommt beides auf einmal; wer
  einzeilig ausweicht (der bisherige Workaround), bezahlt mit der Satzgrenze. **Für eine `<param>`-Zeile
  ist das der bessere Handel und gemessen sauber** — für einen `<para>` im `<summary>` nicht.
  Der neue Absatz in `ExerciseCatalogDtos.cs` steht darum einzeilig und **ohne** Satzgrenze; das ist eine
  bewusste Wahl zwischen zwei Fehlern, keine übersehene Stelle.
- **2026-10-23** — Stufe in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)) auf `ausformuliert` gezogen: die Datei trug User Story, Ist-Stand und Offene Punkte längst, nur der `status` stand auf `idee` (Index-Warnung). Neu gezählt: **16 von 327** Endpunkt-Summaries beginnen mit Leerzeichen, 31 Texte tragen `\n` + ≥4 Leerzeichen. Ansatzpunkt: `Program.cs:288-520` hat fünf OpenAPI-Transformer, ein Trimm-Transformer gehört dorthin. **Widerspruch zu B-138** (dessen Annahme „eine leere `///`-Zeile ergibt `\n\n`“): stimmt für Schema-Beschreibungen (39 Schemas tragen `\n\n`), nicht für Operation-Summaries – beide zusammen in einem Sprint bauen.
