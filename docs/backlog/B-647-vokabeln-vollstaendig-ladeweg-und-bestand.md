---
tags: [typ/story, status/geschaetzt]
aliases: []
status: geschaetzt
prio: P2
art: Wunsch
groesse: L
wo: beides
migration: nein
vertragsbruch: ja
quelle: B-646
grund: ""
ersetzt_durch: []
---

# B-647 · Vokabeln vollständig: Definition, Ladeweg und Bestand

Teil (a) aus der Teilung von [B-646](B-646-vokabel-schema-nachfuellen.md). Ist-Stand, echte Lücke und die
Begründung jeder Entscheidung stehen dort.

## User Story

Als **Creator** möchte ich, dass jede Vokabel im Speicher ihre Wortart und, je nach Wortart, Artikel, Genus,
Plural oder Genitiv bzw. die Verbangaben strukturiert trägt, **und dass das auch für jede neu geladene Unit
gilt**. **Damit** können Übungen, die auf Vokabeln aufbauen (Birkenbihl, Artikel, Konjugation, Filter nach
Wortart), auf diese Daten zählen.

## Ist-Stand am Code

Ausführlich in B-646. Das Wichtigste:

- Gemessen am 2026-11-17 auf der DB-Kopie: 1.104 von 1.119 Vokabeln haben die Wortart `Other`, nur 12 Einträge
  tragen `Noun`/`Verb`, Latein hat weder Genitiv noch Genus.
- Alle Übungen legen Vokabeln über denselben Weg an, und der setzt nur Wort und Übersetzung
  (`backend/Pugling.Api/Services/Creator/VocabularyStoreService.cs:37-46`).
- Die TSV kennt nur `abschnitt`, `wort`, `uebersetzung`, `hinweis`, `notiz`, `unsicher`
  (`.claude/skills/lehrwerk-aufbereiten/laden.py:26`).
- `NounInfo` hat kein Feld für den Genitiv (`backend/Pugling.Contracts/Common/VocabBaseTypes.cs:48-56`).
- Der Filter `?incomplete=true` prüft heute Wortart `Other` und Nomen/Verb ohne Angaben, aber keine Grundform
  (`backend/Pugling.Api/Controllers/Creator/VocabularyStoreController.cs:116-119`).
- Der Artikel im `Word` trägt die Wertung von B-421
  (`backend/Pugling.Api/Services/Shared/ExerciseContentResolver.cs:132`, `:146-147`).

## Entscheidungen

Aus B-646 übernommen: **1** (Artikel/`to` bleiben im `Word`, zusätzlich strukturiert), **2** (Definition von
„vollständig"), **3** (Opus liest den Bestand aus den Seiten-Markdowns, Vater prüft eine Stichprobe), **6**
(`NounInfo.Genitive`), **7** (Übersetzungssätze bis B-649 als `Phrase`), **9** (`NounInfo` beschreibt das Wort
der Lernsprache, den deutschen Tipp-Artikel liest der Code aus der Übersetzung).

## Akzeptanzkriterien

- `?incomplete=true` folgt Entscheidung 2: Nomen ohne Artikel/Genus (sprachabhängig) bzw. ohne Plural, im
  Latein ohne Genitiv/Genus. Verben ohne festgelegtes `IsBaseForm`, gebeugte Formen ohne `BaseFormId`. Jede
  andere Wortart, solange sie `Other` ist. Tests in `Pugling.Api.Tests` decken je Wortart einen vollständigen
  und einen unvollständigen Fall ab.
- `NounInfo.Genitive` existiert im Vertrag, im Client und im Vokabel-Editor des Frontends (anlegen **und**
  zurücklesen).
- Speichern weist einen Widerspruch ab: Beginnt `Word` mit einem Artikel der Lernsprache, muss `Noun.Article`
  derselbe sein.
- `laden.py` und das TSV-Format kennen Spalten für Wortart, Artikel, Genus, Plural, Genitiv und
  Infinitiv/Grundform. Eine neu geladene Unit hat 0 Treffer in `?incomplete=true`.
- Auf einer Kopie der echten DB: Nach dem Nachfüll-Lauf liefert `?incomplete=true` für en, fr und la **0**
  Treffer. Die 29 Übersetzungssätze tragen `Phrase`. `ItemProgress` und `PositionItemProgress` sind
  zeilengleich vor und nach dem Lauf.
- `NounInfo` beschreibt das Lernwort (Entscheidung 9). Der Vertragstext sagt das, Seed und Editor-Beschriftung
  folgen. Der B-446-Tipp nimmt den deutschen Artikel aus der Übersetzung. `VokabelTippTests` wird
  angepasst; je Fall ist benannt, welche Änderung ihn rot macht.
- Auf der Kopie wird gezählt, bei wie vielen Einträgen sich durch die neue Wortart das Weglassen des Artikels
  (B-421, nur `Noun`/`Other`) oder der Tipp ändert. Jede solche Änderung ist dem Vater vorgelegt, bevor die echte
  DB angefasst wird.
- Der Vater hat eine Stichprobe je Buch geprüft. Erst danach läuft der Nachfüll-Lauf gegen die echte DB.

## Schätzung

**Größe: L.** Der Anker ist die DB-Umbau-Etappe E6 („Löschverhalten explizit + bezahltes Inventar retten"):
Wie dort gibt es einen Eingriff am Code **und** einen Datenlauf gegen die echte DB, deren Lernstände überleben
müssen. Größer als M (B-03, ein neuer Pfad), weil hier fünf Teile zusammenkommen: Vertrag/Backend,
Editor, Ladeweg, Datenerhebung für vier Bücher (rund 1.100 Wörter) und der überwachte Nachfüll-Lauf. Nicht XL:
Alle Teile hängen an einer Definition von „vollständig" und werden zusammen abgenommen. B-648 und B-649
sind schon abgetrennt.

- **`migration: nein`.** `Noun`/`Verb` sind Text-Spalten mit JSON-Konverter
  (`backend/Pugling.Api/Data/PuglingDbContext.cs:307-315`). Ein neues Feld `Genitive` ändert nur das JSON, nicht
  das EF-Modell. `PartOfSpeech.Phrase` gibt es schon (`backend/Pugling.Contracts/Common/VocabBaseTypes.cs:30-31`).
  Die Daten zieht ein Skript über die API nach, keine Migration. `SchemaGuardTests` (kein Modell-Drift) bestätigt
  das beim Bauen.
- **`vertragsbruch: ja`.** In der Form ist die Änderung additiv (`NounInfo.Genitive`), in der **Bedeutung**
  bricht sie (Entscheidung 9): Wer bisher den deutschen Artikel in `Noun.Article` schrieb, schreibt jetzt den
  falschen. Betroffen sind der Seed (`backend/Pugling.Api/Data/Seed.cs:717-728`), der Vokabel-Editor
  (`frontend/src/vater/VaterVocab.tsx:424`, `:539-542`) und der B-446-Tipp. Der KI-Creator setzt `NounInfo` nicht
  (kein Fund in `backend/Pugling.Agent.Creator`).
- **`wo: beides`.** Das Backend kommt zuerst (Vertrag, Prüfung, Filter, Tipp). Das Frontend zieht im Editor das
  Feld Genitiv und die Beschriftung nach. Dazu kommen das Ladeskript im Skill `lehrwerk-aufbereiten` und der
  Datenlauf.

### Risiken

1. **Die Wortart wirkt auf die Wertung.** B-421 lässt den Artikel nur bei `Noun`/`Other` weg
   (`backend/Pugling.Api/Services/Shared/ExerciseContentResolver.cs:146-147`). Wird ein Eintrag von `Other` zu
   `Adjective` oder `Phrase`, muss das Kind womöglich einen führenden Artikel mittippen, den es bisher weglassen
   durfte. Deshalb wird auf der Kopie gezählt und dem Vater vorgelegt (AK).
2. **Der Tipp ändert sich sichtbar.** Liest B-446 den Artikel künftig aus der Übersetzung, kann auf Karten der
   laufenden Pläne ein Tipp erscheinen, der heute fehlt. Grund: Die echte DB trägt bei den französischen Einträgen
   „la", was den Tipp bisher unterdrückt. Beim Bauen wird festgelegt und getestet, dass der sichtbare Tipp in den
   laufenden Plänen gleich bleibt, außer der Vater will es anders.
3. **Der Filter `incomplete` ist nicht direkt in SQL übersetzbar.** Die neuen Bedingungen liegen im JSON
   (`Genus`, `Genitive`), und EF übersetzt Felder hinter einem Konverter nicht. Ausweg wie im
   `VocabularyStoreService`: je Sprachpaar eine schmale Projektion in den Speicher laden und dort prüfen. Dabei
   müssen Seitenaufteilung und `X-Total-Count` stimmen.
4. **Die Datenerhebung ist ungenau.** Opus liest Wortart und Formen aus den Seiten-Markdowns. Einträge ohne
   Buchquelle (Seed, KI-Übungen, Handanlagen) brauchen einen eigenen Durchgang. Die Stichprobe des Vaters ist der
   Beleg, nicht die Ausgabe des Modells.
5. **Der Lauf gegen die echte DB.** Der Nachfüll-Lauf ändert nur `PartOfSpeech`, `Noun`, `Verb` und
   `BaseFormId`, nie `Word`/`Translation`, also bleibt jeder Schlüssel und jeder Lernstand stehen. Er läuft
   erst auf einer Kopie (`scripts/heimnetz.ps1 -Database <kopie> -Port <anderer>`), dann mit Freigabe gegen
   `127.0.0.1:5200`. Das Startskript legt die Sicherung an.

### Angriffsplan

1. **Backend:**
   - Vertragstext von `NounInfo` auf das Lernwort umstellen und `Genitive` ergänzen.
   - Beim Speichern prüfen, dass ein `Word` mit führendem Artikel denselben Artikel in `Noun.Article` trägt, mit
     der Artikelliste aus `StageMechanics` und `validation_error`.
   - Den Filter `incomplete` nach Entscheidung 2 neu fassen.
   - Den B-446-Tipp auf den Artikel der Übersetzung umstellen.
   - Den Seed nachziehen.
   - Tests dazu.
2. **Frontend:** im Vokabel-Editor das Feld Genitiv und die Beschriftung „Artikel/Genus/Plural des Lernworts";
   anlegen und zurücklesen.
3. **Ladeweg:**
   - TSV-Spalten `wortart`, `artikel`, `genus`, `plural`, `genitiv`, `grundform` in `laden.py` und in der
     Skill-Doku.
   - Das Ladeskript legt jede Vokabel zuerst vollständig über `POST creator/vocabulary` an und verweist dann per
     `refs`, statt Inline-Items mit nur `front`/`back` zu schicken.
4. **Datenerhebung:** Opus füllt die neuen Spalten für Access 3, Access 5, VIVA 1 und Découvertes 2 aus den
   Seiten-Markdowns. Die Einträge ohne Buch werden als Liste ausgegeben und in einem eigenen Durchgang gefüllt.
5. **Probe auf der Kopie:**
   - `?incomplete=true` liefert 0.
   - Die Lernstände sind zeilengleich.
   - Die Zählung aus Risiko 1 und 2 liegt vor.
   - Dann folgt die Stichprobe des Vaters.
6. **Lauf gegen die echte DB** mit Freigabe des Vaters.

### Testweg

- `VocabularyStoreTests`: `incomplete` je Wortart vollständig/unvollständig, `Genitive` hin und zurück,
  Artikel-Widerspruch → `400 validation_error`.
- `VokabelTippTests`: Der Tipp kommt aus der Übersetzung, kein Tipp bei Wiederholung der Frage.
- `SchemaGuardTests` (keine Migration nötig) und `ContractDocumentTests` (zieht `docs/openapi/v1.json` nach).
- Frontend: E2E im Vokabel-Editor (Nachbar `frontend/e2e/schnellanlage-ohne-dublette.spec.ts`): Genitiv anlegen,
  neu laden, Feld trägt den Wert.
- `/smoke-test` für den Ladeweg mit einer kleinen Probe-TSV gegen die Wegwerf-DB.
- Datenlauf: Messung auf der Kopie (SQL-Zählung `incomplete`, `ItemProgress`/`PositionItemProgress` vor und
  nach dem Lauf), dann der Rollengang als Sohn an einer Karte mit französischem Nomen.

## Verlauf

- **2026-11-17** — aus B-646 geteilt (Entscheidung 8), Stufe `gegrillt` mit den Entscheidungen 1–3, 6, 7 aus B-646.
- **2026-11-17** — geschätzt: L, beides, keine Migration, Vertragsbruch in der Bedeutung (Entscheidung 9 aus B-646, beim Schätzen mit dem Vater nachgeholt).
