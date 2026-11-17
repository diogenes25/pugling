---
tags: [typ/story, status/verworfen]
aliases: []
status: verworfen
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Chat mit dem Vater, 2026-11-17 (Messung auf einer Kopie der echten pugling.db)"
grund: "geteilt (Entscheidung 8)"
ersetzt_durch: [B-647, B-648, B-649]
---

# B-646 · Vokabel-Schema nachfüllen: Wortart, Nomen-/Verb-Angaben, jedes Birkenbihl-Wort eine Vokabel

Gemessen am 2026-11-17 auf einer Kopie der echten DB (1.119 `Vocabularies`): die Pflichtfelder (`Key`, `Word`,
`Translation`, Sprachen) sind vollständig, die strukturierten Felder fast leer. `PartOfSpeech` steht bei 1.104
Einträgen auf `Other`; `Noun` ist bei 7, `Verb` bei 5 gesetzt (Handproben); `TranslationAlternatives`,
`PronunciationAudioUrl` und Tags sind nirgends gesetzt, `BaseFormId` einmal. Die Information steckt teils
unstrukturiert im Wort: 68 französische Einträge tragen den Artikel im `Word` (`la ville`), 192 englische das
`to`/`(to)`, 10 ein `the`/`a`. Latein (88, VIVA 1 L1–3) hat weder Genitiv noch Genus (`pater` statt
`pater, patris m.`).

**Anforderung des Vaters (2026-11-17):** Die Felder sollen **vollständig** gefüllt sein, weil weitere
Übungen auf den Vokabeln aufbauen. Konkret soll die Birkenbihl-Methode ausgebaut werden, und dafür braucht
**jedes Wort, das in einem Birkenbihl-Text vorkommt, eine Vokabel**.

## User Story

Als **Creator** möchte ich, dass jede Vokabel im Speicher ihre Wortart und – je nach Wortart – Artikel/Genus/
Plural bzw. Grundform und Beugung strukturiert trägt, und dass jedes Wort eines Birkenbihl-Textes mit einer
Vokabel verknüpft ist, **damit** neue Übungen (Birkenbihl-Ausbau, Artikel-/Konjugationsübungen, Filter nach
Wortart) auf diesen Daten aufbauen können, statt sie aus dem Wortlaut zu raten.

## Ist-Stand am Code

### Das Schema kann fast alles, befüllt wird es nicht

- `Vocabulary` trägt `PartOfSpeech`, `Noun`, `Verb`, `BaseFormId`/`BaseFormRelation`
  (`backend/Pugling.Api/Models/VocabEntities.cs:41-56`); gebeugte Formen sind als eigene Einträge mit Zeiger auf
  die Grundform gedacht (`VocabEntities.cs:3-5`).
- `NounInfo` kennt `Article`, `Genus`, `Plural` – **keinen Genitiv** (`backend/Pugling.Contracts/Common/VocabBaseTypes.cs:48-56`).
  Für Latein (`pater, patris m.`) fehlt damit ein Feld; `Genus` (m/f/n) passt.
- Die Creator-API nimmt alle Felder an: Anlegen `Controllers/Creator/VocabularyStoreController.cs:279-291`,
  PATCH `VocabularyStoreController.cs:342-361`.
- „Vollständig" ist schon einmal definiert, als Filter `?incomplete=true`: keine Übersetzung, Wortart `Other`,
  Nomen ohne `Noun`, Verb ohne `Verb` (`VocabularyStoreController.cs:116-119`). Gebeugte Formen ohne Grundform
  zählt er nicht.

### Jeder automatische Anlegeweg setzt die Wortart auf `Other`

- Der eine Anlegeweg aller Übungen ist `VocabularyStoreService.GetOrCreateManyAsync`; er setzt nur Wort,
  Übersetzung und `PartOfSpeech = partOfSpeech ?? Other` (`Services/Creator/VocabularyStoreService.cs:37-46`).
  Keine Übung reicht eine Wortart durch (Aufrufe `Controllers/Creator/ExerciseControllers.cs:336`, `:441`, `:617`).
- Das Ladeskript schreibt Wortlisten als Inline-Items mit `front`/`back`/`hint`
  (`.claude/skills/lehrwerk-aufbereiten/laden.py:26`, `:451`); die TSV hat keine Spalte für Wortart, Genus,
  Plural oder Genitiv. Die Längen im Latein stehen nur im `hinweis` („lang: māter").

### Der Artikel im Wort ist heute tragend

- B-421 lässt beim Tippen einen führenden Artikel weg, **sofern die Wortart `Noun` oder `Other` ist**
  (`Services/Shared/ExerciseContentResolver.cs:132`, `:146-147`); `ArticleRequired` schaltet das je Übung ab
  (`backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs:35`). Die Mechanik setzt voraus, dass der Artikel **im**
  `Word` steht (`la ville`).
- Der abgeleitete Hinweis zeigt `NounInfo.Article` nur, wenn das gefragte Wort nicht schon damit beginnt (B-446,
  `ExerciseContentResolver.cs:149-153`).
- Folge: Wer den Artikel aus `Word` nach `Noun.Article` zieht, ändert, was das Kind sieht und tippen muss – in allen
  laufenden Plänen.

### Birkenbihl: nichts erzwingt, dass ein Wort eine Vokabel hat

- Messung: die einzige Birkenbihl-Übung (#3 „Getting to know each other", angelegt 2026-11-10) hat 8 Wörter,
  **0** verknüpft, **1** im Speicher auffindbar.
- Die Dekodierung sucht die exakte Oberfläche, case-insensitiv, ohne Grundform, ohne Artikel-/`to`-Faltung
  (`Services/Creator/BirkenbihlDecodingService.cs:48-62`). `is` findet `be` nicht, `live` nicht `(to) live`,
  `ville` nicht `la ville`.
- `POST …/sentences` speichert ein nicht gefundenes Wort ohne Glosse und ohne Vokabel
  (`ExerciseControllers.cs:551-562`).
- Anlegen/Ersetzen der ganzen Übung (CRUD) normalisiert nur die Ids (`ExerciseControllers.cs:500-512`), verankert
  also eine mitgeschickte Glosse **nicht** im Speicher – anders als die Übersetzungsübung, die jedes Paar anlegt
  (`ExerciseControllers.cs:437-445`). So entstand #3: Glossen vorhanden, `vocabularyId: null`.
- Nur der Einzeltausch mit freier Glosse legt eine Vokabel an (`ExerciseControllers.cs:613-619`).

### Die 29 deutschen Einträge sind Übersetzungssätze, keine vertauschten Vokabeln

- Sie stammen aus sechs Übersetzungsübungen mit `sourceLang: de` (#12, #23, #52, #62, #65, #101), die jedes
  Satzpaar als Vokabel verankern (`ExerciseControllers.cs:437-445`). Für die Übung ist `de` als Quellsprache
  richtig; für den Speicher widerspricht es der Festlegung „`SourceLanguage` = Lernsprache" (B-597,
  `VocabEntities.cs:17-23`). Ganze Sätze sind zudem keine Wörter mit Wortart.

## Die echte Lücke

1. **Kein Anlegeweg füllt die Grammatikfelder**, und die Quelle (TSV) hat sie nicht – das Nachfüllen eines
   Bestands allein hält nicht: jede neue Unit legt wieder `Other` an.
2. **Birkenbihl kennt keine Pflicht „jedes Wort hat eine Vokabel"**: weder beim Anlegen noch beim Satz-Hinzufügen,
   und die Suche findet gebeugte Formen und Wörter mit Artikel/`to` nicht.
3. **Gebeugte Formen fehlen als Einträge** – ohne sie kann ein Text (`is`, `goes`, `went`) nie vollständig
   verknüpft sein.
4. **Latein hat kein Feld für den Genitiv.**
5. **Die Darstellung des Artikels hängt am Wortlaut** – eine Bereinigung des `Word` berührt B-421/B-446 und die
   laufenden Pläne der Kinder.

## Akzeptanzkriterien (Entwurf)

- Für jede Vokabel einer Lernsprache (en, fr, la) gilt `?incomplete=true` liefert **0** Treffer; gebeugte Formen
  tragen `BaseFormId`.
- Jedes Wort jeder Birkenbihl-Übung hat eine `vocabularyId`; ein neuer Satz oder eine neue Übung ohne vollständige
  Verknüpfung wird [abgewiesen | markiert – siehe Offene Punkte].
- Die Dekodierung findet die Vokabel zu einer gebeugten Form (`is` → `is`, Grundform `be`) und zu einem Wort, das im
  Speicher mit Artikel bzw. `to` steht.
- Eine neue Unit aus `laden.py` kommt vollständig an (Wortart und Nomen-/Verb-Angaben aus der TSV).
- Latein-Nomen tragen Genitiv und Genus.
- Die Lernstände der Söhne (`ItemProgress`, `PositionItemProgress`) überleben die Bereinigung unverändert;
  Probe auf einer Kopie der echten DB vorher.
- Integrationstests in `Pugling.Api.Tests` für Dekodierung (Grundform, Artikel) und die Birkenbihl-Pflicht.

## Offene Punkte

1. ~~**Artikel und `to` im `Word` – bleiben oder herausziehen?**~~ (→ Entscheidung 1) Herausziehen ist sauberer und macht die Birkenbihl-
   Suche einfach, bricht aber B-421/B-446 und ändert, was die Kinder in laufenden Plänen sehen und tippen.
   *Empfehlung:* **bleiben lassen**, zusätzlich strukturiert in `Noun.Article`/`Verb.Infinitive` ablegen, und die
   Dekodierung faltet Artikel/`to` beim Suchen weg. Kein Bruch an laufenden Plänen.
2. ~~**Was heißt „vollständig" je Wortart?**~~ (→ Entscheidung 2) *Empfehlung:* Nomen = Artikel/Genus (+ Plural; Latein + Genitiv), Verb =
   `IsBaseForm` und bei Formen `BaseFormId` + Zeit/Person/Zahl, sonst nur die Wortart. Kein Audio, keine Tags,
   keine `TranslationAlternatives` (die Wertung zerlegt `,`/`;`, B-462). Den Filter `incomplete` entsprechend
   schärfen.
3. ~~**Woher kommen die Werte für den Bestand (1.104 Einträge)?**~~ (→ Entscheidung 3) Von Hand ist das zu viel. *Empfehlung:* Opus
   füllt sie aus den Seiten-Markdowns der Bücher (Vokabelseiten tragen Wortart/Genus/Plural, im Latein den
   Genitiv) in neue TSV-Spalten; ein Skript zieht sie per PATCH nach; der Vater prüft eine Stichprobe. Nicht
   `gemma4:e2b` (B-459).
4. ~~**Birkenbihl-Pflicht: hart abweisen oder weich markieren?**~~ (→ Entscheidung 4) Hart heißt: Satz ohne vollständige Verknüpfung →
   `400`. Weich heißt: speichern, aber als unvollständig ausweisen. *Empfehlung:* **anlegen statt abweisen** –
   ein nicht gefundenes Wort mit Glosse wird wie bei der Übersetzungsübung im Speicher verankert; ein Wort ohne
   Glosse lässt die Übung als unvollständig gelten (sichtbar im Editor), nicht spielbar-gesperrt.
5. ~~**Gebeugte Formen: wer legt sie an?**~~ (→ Entscheidung 5) *Empfehlung:* beim Birkenbihl-Satz bei Bedarf – die Dekodierung schlägt
   die Grundform vor, der Creator bestätigt, die Form entsteht mit `BaseFormId`. Kein vorab erzeugtes
   Konjugationsparadigma.
6. ~~**Latein-Genitiv: neues Feld in `NounInfo` oder in `Plural` zweckentfremden?**~~ (→ Entscheidung 6) *Empfehlung:* neues Feld
   `Genitive` (JSON-Spalte, also **keine Migration**, aber Vertragsänderung).
7. ~~**Die 29 Übersetzungssätze im Speicher – so lassen, umdrehen oder aus dem Speicher nehmen?**~~ (→ Entscheidung 7) *Empfehlung:*
   eigene Story; Sätze gehören nicht in den Wort-Speicher. Für B-646 nur aus der Vollständigkeitsprüfung
   ausnehmen (Wortart `Phrase`).
8. ~~**Schnitt:**~~ (→ Entscheidung 8) Die Story ist groß (Bestand, Ladeweg, Birkenbihl, Latein). *Empfehlung:* teilen in
   (a) Vollständigkeit definieren + Ladeweg + Bestand nachfüllen, (b) Birkenbihl-Pflicht + Dekodierung,
   (c) Übersetzungssätze. Reihenfolge a → b.

## Entscheidungen

Am 2026-11-17 vom Vater im Dialog getroffen: Er hat alle acht Empfehlungen der Offenen Punkte übernommen
(„Nimm immer deine Empfehlung"). Hier stehen sie mit Begründung und Kosten.

1. **Artikel und `to` bleiben im `Word`, zusätzlich strukturiert.** `la ville` behält seinen Wortlaut und trägt
   außerdem `Noun.Article = "la"` und `Genus`; `(to) live` trägt `Verb.IsBaseForm = true`. Die Birkenbihl-Suche
   lässt Artikel und `to` beim Vergleichen weg. Begründung: B-421/B-446 und die laufenden Pläne der Söhne bleiben
   unberührt, kein Lernstand wandert. Kosten: die Angabe steht doppelt. Eine Prüfung beim Speichern muss
   sicherstellen, dass ein `Word` mit führendem Artikel denselben Artikel in `Noun.Article` trägt. Dazu kommt
   eine sprachabhängige Artikelliste für die Suche (en/fr/la).
2. **„Vollständig" je Wortart.** Ein Nomen braucht Artikel und Genus, wo die Sprache sie hat, dazu den Plural; im
   Latein Genitiv und Genus. Bei einem Verb steht `IsBaseForm` fest; eine gebeugte Form trägt `BaseFormId` und
   Zeit/Person/Zahl. Jede andere Wortart braucht nur die Wortart selbst (`≠ Other`). Audio, Tags und
   `TranslationAlternatives` zählen nicht (die Wertung zerlegt `,`/`;`, B-462). Der Filter `?incomplete=true`
   wird darauf geschärft. Begründung: ein messbares Ziel statt „alles gefüllt". Kosten: Der Filter wird
   sprachabhängig (Englisch hat kein Genus), und der Vertrag beschreibt das.
3. **Werte für den Bestand liest Opus aus den Seiten-Markdowns.** Sie kommen in neue TSV-Spalten, ein Skript zieht
   sie per PATCH nach, und der Vater prüft eine Stichprobe. Begründung: 1.104 Einträge sind von Hand zu viel,
   und `gemma4:e2b` liest zu ungenau (B-459). Kosten: Opus-Tokens je Buch. Einträge ohne Buchquelle (Seed,
   KI-Übungen) brauchen einen eigenen Durchgang, und die Stichprobe ist Handarbeit des Vaters.
4. **Birkenbihl: anlegen statt abweisen.** Ein nicht gefundenes Wort mit Glosse wird im Speicher verankert, wie
   bei der Übersetzungsübung, beim Satz-Hinzufügen **und** beim Anlegen/Ersetzen der ganzen Übung. Ein Wort ohne
   Glosse markiert die Übung im Editor als unvollständig; sie bleibt spielbar. Begründung: Ein `400` würde das
   Anlegen eines Textes blockieren, solange der Speicher lückenhaft ist. Kosten: Die Pflicht ist weich, eine
   unvollständige Übung kann bestehen bleiben, und der Editor braucht eine Anzeige.
5. **Gebeugte Formen entstehen bei Bedarf.** Die Dekodierung schlägt zu `is` die Grundform `be` vor, der Creator
   bestätigt, und die Form wird mit `BaseFormId` angelegt. Kein vorab erzeugtes Paradigma. Begründung: Nur
   Formen, die in Texten vorkommen, kosten Pflege. Kosten: Die Grundform-Erkennung braucht eine Regel je Sprache
   oder eine Liste unregelmäßiger Formen, und der Creator hat einen Bestätigungsschritt mehr.
6. **Latein-Genitiv als neues Feld `NounInfo.Genitive`.** Begründung: `Plural` zweckzuentfremden würde jede
   Sprache verwirren. Kosten: Vertragsänderung (`Pugling.Contracts`, Client, Frontend-Editor). Keine Migration,
   weil `Noun` eine JSON-Spalte ist.
7. **Die 29 Übersetzungssätze sind eine eigene Story (B-649).** Bis dahin tragen sie die Wortart `Phrase` und
   fallen damit aus der Vollständigkeitsprüfung. Begründung: Sätze im Wortspeicher sind ein eigenes Thema und
   nicht nötig, damit B-647/B-648 ihr Ziel erreichen. Kosten: Der Speicher bleibt bis dahin gemischt.
8. **Geteilt in drei Stories, Reihenfolge B-647 → B-648.**
   - [B-647](B-647-vokabeln-vollstaendig-ladeweg-und-bestand.md): Vollständigkeit, Ladeweg, Bestand, Latein-Genitiv
     (Entscheidungen 1–3, 6, 7).
   - [B-648](B-648-birkenbihl-jedes-wort-eine-vokabel.md): Birkenbihl-Pflicht und Dekodierung (Entscheidungen 1, 4, 5).
   - [B-649](B-649-uebersetzungssaetze-im-vokabelspeicher.md): Übersetzungssätze im Wortspeicher (Entscheidung 7).

   Begründung: Jeder Teil ist für sich abnehmbar. B-648 braucht die Grundform-Daten aus B-647. Kosten: drei
   Durchläufe durch die Kette statt einem.

9. **`NounInfo` beschreibt das Wort der Lernsprache (`Word`), nicht die deutsche Übersetzung.** Diese
   Entscheidung präzisiert 1 und 6 und wurde beim Schätzen von B-647 nachgeholt (Vater, 2026-11-17).
   Hintergrund: Der Vertrag definiert Artikel, Genus und Plural bisher als die der Zielsprache, also Deutsch
   (`backend/Pugling.Contracts/Common/VocabBaseTypes.cs:47-56`). Der Tipp aus B-446 liest den Artikel so
   (`backend/Pugling.Api/Services/Shared/ExerciseContentResolver.cs:148-163`), und der Seed setzt „die" zu
   `la ville`. Für die Grammatik der Lernsprache gab es damit keinen Platz. Künftig gilt: `la ville` →
   `Article = "la"`, `Genus = Feminine`; `pater` → `Genus = Masculine`, `Genitive = "patris"`. Den deutschen
   Artikel für den Tipp liest der Code aus der Übersetzung („die Stadt"), über die vorhandene Artikelliste
   (`backend/Pugling.Api/Services/Shared/StageMechanics.cs:62-67`). Begründung: Birkenbihl, Artikel- und
   Beugungsübungen brauchen die Grammatik des Lernworts, und der deutsche Artikel steht schon in der
   Übersetzung. Kosten: Die Bedeutung des Vertrags ändert sich (die Form nicht). Der B-446-Tipp wird umgebaut,
   Seed und Editor-Beschriftung ziehen nach, der Eintrag `house` („das"/„Häuser") wird umgedreht.

## Verlauf

- **2026-11-17** — angelegt (Quelle: Füllgrad-Messung des Vokabel-Schemas auf einer DB-Kopie, im Chat mit dem Vater).
- **2026-11-17** — Anforderung des Vaters ergänzt (Felder vollständig, jedes Birkenbihl-Wort braucht eine Vokabel), Birkenbihl-Abdeckung auf der DB-Kopie gemessen; P3→P2, Aufräumen→Wunsch.
- **2026-11-17** — ausformuliert: Ist-Stand am Code belegt. Korrektur der Idee: die 29 deutschen Einträge sind Übersetzungssätze (sechs Übungen mit `sourceLang: de`), keine vertauschten Vokabeln. Neu gefunden: CRUD-Anlage einer Birkenbihl-Übung verankert Glossen nicht im Speicher; der Artikel im `Word` trägt B-421.
- **2026-11-17** — gegrillt im Dialog (Vater übernimmt alle acht Empfehlungen) und nach Entscheidung 8 geteilt: verworfen, ersetzt durch B-647, B-648, B-649.
- **2026-11-17** — Entscheidung 9 nachgetragen (beim Schätzen von B-647 gefunden: `NounInfo` war im Vertrag deutsch); vom Vater im Dialog entschieden.
