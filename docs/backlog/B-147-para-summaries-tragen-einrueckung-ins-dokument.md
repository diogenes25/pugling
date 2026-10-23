---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/doku, rolle/creator]
aliases: [Einrückung im OpenAPI-Summary, para im Summary, Swagger-Codeblock]
status: abgenommen
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

## Akzeptanzkriterien

1. Kein Endpunkt-`summary` in `docs/openapi/v1.json` beginnt mit Leerzeichen, und keine Folgezeile trägt
   die Quelltext-Einrückung (Zählung vorher/nachher im Verlauf).
2. Ein Absatz bleibt als Absatz erkennbar – der Workaround „einzeilig ohne Satzgrenze" wird unnötig.
   Getragen von einer leeren `///`-Zeile statt `<para>` (Entscheidung 3, gemeinsam mit B-138 AK 3).
3. Die Normalisierung sitzt an **einer** Stelle (Transformer), nicht in jedem Kommentar, und ein Test hält
   sie fest.

## Entscheidungen

Autonom gegrillt im vierten Nachtlauf (`art: Defekt`, Freigabe 1), am Code und an der laufenden Swagger UI belegt.

1. **Punkt 1 – gemessen, nicht nachgelesen:** Swagger UI zeigt die Operation-`summary` als **Klartext**
   (`div.opblock-summary-description`, `white-space: normal`). Die führenden Leerzeichen sind dort unsichtbar,
   ein `<b>all</b>` steht aber wörtlich da (`POST /api/v1/auth/login`, Bildschirmfoto im Rollengang). Schema-
   und Feld-`description`s werden als Markdown gerendert; dort sind vier Leerzeichen ein Codeblock. Ergebnis:
   ein **Defekt** im Vertragsdokument, sichtbar je nach Feld als Codeblock oder als rohes Tag.
   *Kosten:* keine; die Messung ersetzt die Annahme der Story.
2. **Behebung an einer Stelle:** `OpenApi/OpenApiProseNormalizer.cs`, als **letzter** Dokument-Transformer
   (`Program.cs`), über jede `summary`/`description` im Dokument (Operationen, Parameter, Bodies, Antworten,
   Header, Schemas samt Eigenschaften, Tags). Je Zeile `Trim`, Leerzeilen-Läufe zu einem Absatz. Der Generator
   rückt nur die **erste** Zeile eines `<para>` ein; „eingerückt nach bündig“ wird darum zum Absatz.
   *Kosten:* das Signal ist eine Eigenheit von Microsoft.AspNetCore.OpenApi 10.0.10; ändert der Generator
   sein Verhalten, findet die Regel nur nichts mehr.
3. **Absätze in der Quelle: leere `///`-Zeile statt `<para>`** – gemessen, dass der Generator `<para>` in zwei
   Pfaden verschieden behandelt: in eingerückten Deklarationen bleibt die Einrückung, in Typen auf oberster
   Ebene klebt er aufeinanderfolgende `<para>` **ohne Trenner** zusammen (17 Stellen wie
   `…in the list.The SuggestedGoalPoints…`) und wirft dabei `<b>`/`<c>` weg. Eine leere `///`-Zeile ergibt in
   beiden Fällen `\n\n` und behält Betonung und Backticks. **Die Behauptung vom 2026-09-02 („eine leere
   ///-Zeile hilft nicht“) stimmt nur zusammen mit `<para>`**: leere Zeile **plus** `<para>` klebt weiter.
   Darum hat der Lauf alle 204 eigenständigen `<para>`/`</para>`-Zeilen in `<summary>`-Blöcken von
   `Pugling.Contracts` und `Pugling.Api/Controllers` (39 Dateien) gegen eine leere `///`-Zeile getauscht
   (Skript, Fundstellenliste im Protokoll).
   *Kosten:* in der IDE-Schnellinfo verschwimmen diese Absätze (VS fasst Leerraum zusammen) – der erste Leser
   des Kommentars verliert die Gliederung, der Vertragsleser gewinnt sie. API-First entscheidet für den zweiten.
   Drei Inline-`<para>` in `<param>` (`CreatorProfileDtos.cs:18`, `TextbookSeriesDtos.cs:21`,
   `ProfileDtos.cs:27`) bleiben unberührt – sie stehen nicht in einem `<summary>`.
4. **Kein Quelltext-Tor für `<para>`:** ein neues `<para>` richtet keinen Schaden mehr an (der Normalisierer
   entfernt die Einrückung), es verliert nur den Absatz. Das Tor sitzt am Dokument
   (`ContractDocumentTests.Vertragsdokument_TraegtKeinQuelltextFormat`), wo der Leser ist.
   *Kosten:* ein künftiges `<para>` in einem Typ auf oberster Ebene klebt wieder an seinen Vorgänger, und
   kein Tor meldet es.

## Schätzung

- **groesse:** S · **wo:** backend · **migration:** nein · **vertragsbruch:** nein (nur Beschreibungstexte).
- **Risiko:** ein Transformer, der Referenz-Beschreibungen über den Getter liest, kopiert die Typbeschreibung
  an jede `$ref` (gemessen: 920 zusätzliche Texte) – darum liest er nur `Reference.Description`.
- **Angriffsplan:** Messung (Swagger UI, Skript über `v1.json`) → Dokument-Test rot → Normalisierer →
  Quelltext-Tausch `<para>` → Leerzeile → Einheitstests mit Mutanten.
- **Testweg:** `ContractDocumentTests.Vertragsdokument_TraegtKeinQuelltextFormat`, `OpenApiProseNormalizerTests`
  (7 Fälle), Rollengang in Swagger UI.

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
- **2026-10-23** — vierter Nachtlauf, Sprint 8 (NP-4, [Protokoll](../pm-sitzung-2026-10-23-nacht.md)): autonom gegrillt (`Defekt`), geschätzt, gebaut. Messung vorher am geparsten `v1.json`: **16 von 327** Operation-Summaries und **26** Texte insgesamt beginnen mit Leerraum, **31** tragen `\n` + ≥ 4 Leerzeichen, **41** Absatzumbrüche, **17** zusammengeklebte Absätze. Rote Probe (Auflage 5): `Vertragsdokument_TraegtKeinQuelltextFormat` gegen den Ausgangsstand – erwartet 31 eingerückte Texte, gemessen **31** (`ContractDocumentTests.cs`, erste Zusicherung). Nachher: 0 eingerückt, 0 Tags, **147** Absatzumbrüche, 0 geklebte Absätze (ein Treffer des Prüfskripts ist `request.Keys`, kein Absatz), Anzahl Texte unverändert **2487**. Je Fall von `OpenApiProseNormalizerTests` eine Mutante gefahren: TrimStart weg → 3 rot (`:17`, `:23`, `:68`); nie Absatz → `:23`; jede Einrückung ein Absatz → `:17`, `:23`; Leerzeilen nicht zusammengefasst → `:31`; Summary mit Markdown → `:37`; kursiv als fett → `:43`; Leerraum bleibt im Tag → `:49`; Referenz-Getter zurückgeschrieben → zuerst **grün** (der Test hatte keinen registrierten Workspace, die Referenz löste nichts auf – eine leere Zusicherung), nach `doc.RegisterComponents()` plus Vorbedingung rot an `:71`. Backend **894/894**.
- **2026-10-23** — `pugling-reviewer` (Backend, zuerst) und danach `frontend-reviewer` (nacheinander, Freigabe 3): kein Blocker. Gezählt (Behebung ändert Code oder Tests): **3** – (1) vier Inline-`<para>` in Contracts (`CreatorProfileDtos.cs:17`, `TextbookSeriesDtos.cs:20`, `ProfileDtos.cs:26`, `ExerciseGrantDtos.cs:18` im `<param>`) verloren weiter Absatz und Backticks, ohne dass ein Tor es sah → auf leere `///`-Zeile umgestellt; (2) Betonung über einen Absatz würde zu `**a` + Leerzeile + `b**` → Übersetzung nur innerhalb eines Absatzes, sonst bleibt das Tag roh und das Tor meldet es, neuer Fall `Betonung_Ueber_Einen_Absatz_Bleibt_Roh` (Mutante: rot an `OpenApiProseNormalizerTests.cs:55`); (3) die Aussage „Zeilen fremder Eigentümer werden mit umgeschrieben" war ungetestet → Reihe eines zweiten Creators im B-145-Fall (Mutante „nur eigene Reihen": rot an `FachnameFolgtDerFachIdTests.cs:130`). Ohne Zähler: Kommentar zum zweiten Mehrdeutigkeitsfall im Normalisierer; Messung „Transformer raus" am Endstand → die Einrückungs-Zusicherung bleibt rot (**7** Texte an `ContractDocumentTests.cs:213`), trägt also weiter; die vorbestehenden Generator-Eigenheiten → **B-259**; Nit `frontend/CLAUDE.md:112` (historischer Satz) belassen. Nachher: Absatzumbrüche **152**, Backend **895/895**, Vitest **406/406**, `tsc -b` sauber. **Rollengang im Browser** (Freigabe 6, Wegwerf-DB `pugling_walk4.db`, Server nach dem letzten Edit gestartet): Swagger UI zeigt die Summary von `POST /api/v1/auth/login` ohne `<b>`, keine Summary beginnt mit Leerraum, `DELETE …/subjects/{id}` mit Absätzen. Vater-Web `/vater/katalog`: neues Fach „Franz Walk" angelegt → die Zeile bietet „Fach umbenennen"/„Löschen" (`isOwn` auf dem Draht), Seed-Fächer nicht; Reihe und Lehrbuch des Kindes per API an das Fach gehängt, im UI umbenannt in „Französisch Walk" → `/vater/lehrwerke` zeigt die Reihe sofort mit dem neuen Fachnamen, das Lehrbuch des Kindes trägt ihn ebenfalls. **Einschränkung, benannt:** nach dem Wiederverbinden der Extension kamen Klicks und Tastatur nicht mehr an; Eingaben liefen darum über die echten Handler der Seite (nativer Value-Setter + `input`/`change`, `button.click()`), nicht über `computer.type`. Sohn: Regressionszeuge (Startseite geladen, kein Student-Pfad im Diff außer Kommentaren, Suite grün). Korrektur zur Zahl „0 geklebte Absätze" weiter oben: sie galt nur für eigenständige `<para>`; die vier Inline-Fälle hat der Reviewer gefunden (siehe oben). Abgenommen; Commit: `e5b67ec`.
