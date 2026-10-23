---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/doku, bereich/qualitaet]
aliases: [rohe HTML-Tags im Vertragsdokument, b-Tags in Summaries, OpenAPI-Beschreibungen]
status: abgenommen
prio: P3
art: Aufräumen
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: pugling-reviewer zu B-123 (2026-08-10), Fund 2 der zweiten Runde
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
wartet_auf: ""
---

# B-138 · Rohe HTML-Tags stehen in 70 Beschreibungen des Vertragsdokuments

Beim Review von [B-123](B-123-lehrwerk-reihe-bearbeiten.md) an einer Stelle behoben und dabei gemessen,
dass es 70 sind. Ein Einzelfall-Fix lässt die Regel wie Zufall aussehen.

## User Story

Als **Konsument der API** möchte ich Beschreibungen lesen, die nicht mit `<b>` und `<em>` gespickt sind —
das Vertragsdokument ist bei API-First das Produkt, nicht ein Nebenprodukt des Quelltexts.

## Ist-Stand am Code

`ContractDocumentTests` schreibt `docs/openapi/v1.json` bei jedem Lauf; die `description` eines Schemas ist
der XML-Doc-Kommentar des Records. Der Generator übersetzt `<c>` in Backticks und **wirft `<para>` weg**,
lässt aber alles andere als rohen Text stehen.

Gezählt am 2026-08-10 im erzeugten Dokument:

| Tag | Vorkommen |
| --- | --- |
| `<b>` | 62 |
| `<i>` | 6 |
| `<em>` | 2 |
| `<para>` | 0 (wird verworfen) |

Eines der beiden `<em>` steht in der Summary von `POST /api/v1/creator/textbook-series`
(`TextbookSeriesController.cs`) — unmittelbar über der Stelle, an der B-123 dasselbe Problem lokal
behoben hat.

**Der Ersatz für `<para>` existiert und wird schon benutzt:** eine leere `///`-Zeile wird zu `\n\n` und
ergibt in Swagger einen echten Absatz. 38 Schemas machen das bereits (z. B. `UpdateMyAccountDto`); B-123
hat es übernommen, mit belegtem Ergebnis (zwei Absätze, kein Markup).

## Die echte Lücke

Nicht „hässliche Doku": Die Lücke ist, dass die **Zielsprache verwechselt** wird. Ein `///`-Kommentar in
`Pugling.Contracts` hat zwei Leser — den Entwickler in der IDE und den API-Konsumenten im
Vertragsdokument — und nur der erste sieht formatiertes XML. Für den zweiten ist `<b>` reiner Text, der
zusätzlich die Leerzeichen um sich herum schluckt (`SubjectId``<b>and</b>``SubjectName`).

Und es ist die Sorte Regel, die dieses Repo mechanisch hält: eine Konvention ohne Tor stellt sich in drei
Monaten wieder her.

## Offene Punkte

1. **Nur aufräumen oder ein Tor?** Empfehlung: beides, und das Tor ist der eigentliche Wert — eine Regel
   in `ConventionGuardTests`, die in `Pugling.Contracts`-Summaries nur `<c>`, `<see>`, `<paramref>` und
   `<summary>`/`<remarks>` selbst erlaubt. Kosten: eine Ausnahmeliste, falls irgendwo `<list>` oder
   `<code>` bewusst steht (vor dem Bau zählen).
2. **Gilt die Regel nur für `Contracts` oder auch für die Controller?** Die Summaries der **Actions**
   landen ebenfalls im Dokument (`operationId`-Beschreibungen) — das `<em>` aus dem Fund ist genau so
   eines. Empfehlung: dieselbe Regel für beide, weil beide dasselbe Dokument speisen. Kosten: der Wächter
   muss zwei Projekte lesen.
3. **Was passiert mit den Stellen, an denen `<b>` echte Betonung trägt?** 62-mal ist viel; ein Teil davon
   betont bewusst. Empfehlung: beim Aufräumen je Stelle entscheiden, ob die Betonung in die Wortwahl
   wandert („**must**" → „is required to") oder wegfällt — nicht mechanisch löschen.

## Akzeptanzkriterien

1. Im erzeugten `docs/openapi/v1.json` steht kein rohes `<b>`/`<i>`/`<em>`/`<strong>` mehr in einer
   `description` oder `summary`.
2. Ein Tor meldet ein neu hinzukommendes Tag; seine Ausnahmeliste trägt je Eintrag einen Grund.
3. Wo ein Absatz gewollt ist, steht eine leere `///`-Zeile — nicht `<para>` (der Generator verwirft es).
4. Die Suite bleibt grün; kein Verhalten ändert sich (Abnahmeform `Aufräumen`).

## Entscheidungen

Autonom gegrillt im vierten Nachtlauf (`art: Aufräumen`, Freigabe 1), zusammen mit [B-147](B-147-para-summaries-tragen-einrueckung-ins-dokument.md)
– dessen Normalisierer entscheidet, was hier noch von Hand bleibt (Reihenfolge laut Paket NP-4).

1. **Punkt 1 – übersetzen statt aufräumen, und ein Tor:** die Tags bleiben im Quelltext (der IDE-Leser
   behält seine Betonung), `OpenApiProseNormalizer` übersetzt sie beim Erzeugen des Dokuments. Das Tor ist die
   zweite Zusicherung von `ContractDocumentTests.Vertragsdokument_TraegtKeinQuelltextFormat`: kein Text im
   Dokument trägt ein rohes Element-Tag. *Kosten:* ein künftiges unbekanntes Tag (`<u>`, `<list>`) macht den
   Test rot, bis der Normalisierer es lernt oder der Kommentar umformuliert ist – gewollt, die Meldung sagt beides.
2. **Punkt 2 – Contracts und Controller:** erledigt sich durch den Ort des Tors. Es misst das Dokument, also
   alles, was es speist, ohne zwei Projekte zu lesen. *Kosten:* keine Fundstelle in `Datei:Zeile` – die Meldung
   nennt den Ort im Dokument.
3. **Punkt 3 – Betonung nach Feldart, nicht je Stelle:** in einer `description` (Markdown) wird `<b>`/`<strong>`
   zu `**…**`, `<i>`/`<em>` zu `*…*`; in einer `summary` (Swagger UI zeigt sie als Klartext, gemessen) fällt
   das Tag weg und der Text bleibt. *Kosten:* in rund achtzig Summaries geht die Betonung für den Vertragsleser
   verloren; ein `**all**` wäre dort genauso sichtbar wie `<b>all</b>`. Keine Entscheidung je Stelle nötig,
   weil nichts gelöscht wird.
4. **AK 3 gilt jetzt für Schemas und Operationen:** die Richtigstellung aus der Sichtung (leere Zeile trennt nur
   in Schemas) war richtig für `<para>` **mit** leerer Zeile; ohne `<para>` trennt die leere Zeile überall
   (Messung und Quelltext-Tausch in B-147, Entscheidung 3).

## Schätzung

- **groesse:** S · **wo:** backend · **migration:** nein · **vertragsbruch:** nein.
- **Risiko:** der Generator verschluckt Leerzeichen neben `<c>`/`<b>` (``**PATCH semantics:**`null` ``) –
  vorbestehend, nicht durch Übersetzen behebbar (das Leerzeichen ist schon weg), bewusst ungedeckt.
- **Testweg:** wie B-147; die Fälle `Summary_Laesst_Betonung_Fallen`, `Description_Uebersetzt_Betonung_Nach_Markdown`,
  `Leerraum_Im_Tag_Wandert_Nach_Aussen`.

## Verlauf

- **2026-08-10** — angelegt aus dem `pugling-reviewer`-Befund zur zweiten B-123-Runde.
  Zahlen sind gemessen, und zwar **am geparsten JSON**, nicht per `grep`: `System.Text.Json`
  escapt `<` als `\u003C`, ein `grep` auf das Tag liefert darum immer 0 Treffer. Diese Falle
  hält die Story ausdrücklich fest, weil sie den Fund sonst unsichtbar macht.
  **Bewusst nicht in B-123 mitgenommen:** dort war es eine Datei, hier sind es 70 Stellen
  mit einer Abwägung je Stelle (Punkt 3) und einem Tor als eigentlichem Ziel.
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): besteht und ist gewachsen – `<b>` 82, `<i>` 7, `<em>` 4, zusammen **93** statt 70 (geparstes `docs/openapi/v1.json`). **AK 3 gilt nur für Schemas:** die Annahme „eine leere `///`-Zeile ergibt einen Absatz“ stimmt für Schema-Beschreibungen, für Operation-Summaries hat B-147 am 2026-09-02 das Gegenteil gemessen – vor dem Bau richtigstellen, mit B-147 zusammen bauen.
- **2026-10-23** — vierter Nachtlauf, Sprint 8 (NP-4, [Protokoll](../pm-sitzung-2026-10-23-nacht.md)): autonom gegrillt (`Aufräumen`), geschätzt, gebaut mit B-147. Gemessen vorher (geparstes `v1.json`): `<b>` 82, `<i>` 7, `<em>` 4 Paare in **71** Texten, keine anderen Tags; in Swagger UI steht `<b>all</b>` wörtlich in der Summary von `POST /api/v1/auth/login`. Rote Probe (Auflage 5): Tag-Zusicherung mit abgeschalteter Übersetzung (Einrückung weiter normalisiert) – erwartet ≥ 71, gemessen **104** Texte an `ContractDocumentTests.cs:217`; die Mehrung um 33 Texte stammt vermutlich aus dem `<para>`-Tausch (B-147): `<b>` in einem `<para>` verwarf der Generator still (gesehen an `ScoringTimeSlot`, `UpdateMyAccountDto`), ohne `<para>` reicht er es roh durch – nicht je Text nachgezählt. Nachher **0**; die Ausnahmeliste des Tors ist leer, weil nach der Übersetzung kein anderes Tag übrig ist. Mutanten der drei Betonungsfälle siehe B-147 (`:37`, `:43`, `:49`).
- **2026-10-23** — `pugling-reviewer` (Backend, zuerst) und danach `frontend-reviewer` (nacheinander, Freigabe 3): kein Blocker. Gezählt (Behebung ändert Code oder Tests): **3** – (1) vier Inline-`<para>` in Contracts (`CreatorProfileDtos.cs:17`, `TextbookSeriesDtos.cs:20`, `ProfileDtos.cs:26`, `ExerciseGrantDtos.cs:18` im `<param>`) verloren weiter Absatz und Backticks, ohne dass ein Tor es sah → auf leere `///`-Zeile umgestellt; (2) Betonung über einen Absatz würde zu `**a` + Leerzeile + `b**` → Übersetzung nur innerhalb eines Absatzes, sonst bleibt das Tag roh und das Tor meldet es, neuer Fall `Betonung_Ueber_Einen_Absatz_Bleibt_Roh` (Mutante: rot an `OpenApiProseNormalizerTests.cs:55`); (3) die Aussage „Zeilen fremder Eigentümer werden mit umgeschrieben" war ungetestet → Reihe eines zweiten Creators im B-145-Fall (Mutante „nur eigene Reihen": rot an `FachnameFolgtDerFachIdTests.cs:130`). Ohne Zähler: Kommentar zum zweiten Mehrdeutigkeitsfall im Normalisierer; Messung „Transformer raus" am Endstand → die Einrückungs-Zusicherung bleibt rot (**7** Texte an `ContractDocumentTests.cs:213`), trägt also weiter; die vorbestehenden Generator-Eigenheiten → **B-259**; Nit `frontend/CLAUDE.md:112` (historischer Satz) belassen. Nachher: Absatzumbrüche **152**, Backend **895/895**, Vitest **406/406**, `tsc -b` sauber. **Rollengang im Browser** (Freigabe 6, Wegwerf-DB `pugling_walk4.db`, Server nach dem letzten Edit gestartet): Swagger UI zeigt die Summary von `POST /api/v1/auth/login` ohne `<b>`, keine Summary beginnt mit Leerraum, `DELETE …/subjects/{id}` mit Absätzen. Vater-Web `/vater/katalog`: neues Fach „Franz Walk" angelegt → die Zeile bietet „Fach umbenennen"/„Löschen" (`isOwn` auf dem Draht), Seed-Fächer nicht; Reihe und Lehrbuch des Kindes per API an das Fach gehängt, im UI umbenannt in „Französisch Walk" → `/vater/lehrwerke` zeigt die Reihe sofort mit dem neuen Fachnamen, das Lehrbuch des Kindes trägt ihn ebenfalls. **Einschränkung, benannt:** nach dem Wiederverbinden der Extension kamen Klicks und Tastatur nicht mehr an; Eingaben liefen darum über die echten Handler der Seite (nativer Value-Setter + `input`/`change`, `button.click()`), nicht über `computer.type`. Sohn: Regressionszeuge (Startseite geladen, kein Student-Pfad im Diff außer Kommentaren, Suite grün). Abgenommen; Commit: siehe Nachtrag.
