---
tags: [typ/story, status/geschaetzt, bereich/katalog, rolle/creator, rolle/student, lerntechnik/vokabeln]
aliases: [Vokabel-Store nur Wort und Übersetzung, Wortart beim Buchimport, Genus und Stammformen aus dem Lehrwerk]
status: geschaetzt
prio: P2
art: Wunsch
groesse: M
wo: doku
migration: nein
vertragsbruch: nein
quelle: "Chat mit dem Vater, 2026-11-17 (Frage: ist der Store mehr als eine 1:1-Übersetzung?)"
grund: ""
ersetzt_durch: []
---

# B-631 · Der Buchimport füllt vom Vokabel-Store nur Wort und Übersetzung

Der Vokabel-Store ist als Lexikon gebaut: Wortart, `Noun` mit Artikel/Genus/Plural, `Verb` mit Formangaben,
Grundform-Verweis, Aussprache und Tags. Der Weg aus dem Lehrwerk (`lehrwerk-aufbereiten` → `laden.py`) schickt
je Wort aber nur `front`/`back`/`hint`, und der Server legt den Eintrag mit `PartOfSpeech = Other` an. Was das
Buch an Grammatik mitliefert, landet als Freitext im Hinweis oder geht verloren.

## User Story

Als **Vater** möchte ich, dass die Vokabeln aus dem Lehrwerk mit ihrer **Wortart** und den Angaben, die das Buch
dazu druckt, im Store landen, **damit** die Antwortprüfung die Wortart kennt (die Artikel-Toleranz greift dann
nur bei Nomen) und sich später Übungen zu Genus, Plural oder Verbformen aus dem Store bauen lassen, statt dass
ich jedes Wort im Vater-Web von Hand nachtrage.

## Ist-Stand am Code

Recherchiert am 2026-11-17 gegen `1ba66737`. Zahlen aus einer Kopie der echten `pugling.db` vom selben Tag
(per SQLite-Backup-API gezogen, die laufende DB nur gelesen).

**Bestand in der DB:** 387 Einträge (de 25, en 136, fr 138, la 88). Davon stehen 372 auf `Other`, alle 88
lateinischen eingeschlossen. Die 15 anderen stammen aus dem Seed (`backend/Pugling.Api/Data/Seed.cs:721`–`:729`:
„la maison“ Nomen usw.). `Noun` ist 7-mal gesetzt, `Verb` 5-mal, `BaseFormId` einmal. Audio,
`TranslationAlternatives` und Vokabel-Tags sind in keinem Eintrag gesetzt (`VocabTagLinks` leer).

**Der Importweg verliert die Angaben an zwei Stellen:**

- Die TSV kennt nur `abschnitt, wort, uebersetzung, hinweis, notiz, unsicher`
  (`.claude/skills/lehrwerk-aufbereiten/laden.py:26`). Die Skill-Vorgabe legt Genus, Plural und unregelmäßige
  Formen ausdrücklich in den **Freitext** `hinweis` (`.claude/skills/lehrwerk-aufbereiten/SKILL.md:164`).
- `laden.py` baut daraus Inline-Items `{front, back, hint}` (`laden.py:450`–`:452`). Der Vertrag eines
  Inline-Items hat gar kein Feld für eine Wortart (`backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs:58`,
  `VocabItem`). `ExerciseItemService` legt die Store-Einträge über `GetOrCreateManyAsync` an
  (`backend/Pugling.Api/Services/Creator/ExerciseItemService.cs:53`). Dort fällt die Wortart auf `Other`
  (`backend/Pugling.Api/Services/Creator/VocabularyStoreService.cs:46`).
- Ein zweiter Lauf ändert am Bestand nichts: eine Übung gleichen Titels gilt als „schon da“ und wird übersprungen
  (`laden.py:477`–`:480`, außer mit `--ersetzen`). Und `GetOrCreate` ändert einen gefundenen Eintrag bewusst
  nie (`VocabularyStoreService.cs:116`–`:121`).

**Die Store-API kann schon alles, was der Import bräuchte:**

- `POST creator/vocabulary/batch` nimmt `PartOfSpeech`, `Noun`, `Verb`, `BaseFormKey`, `Tags` und
  `TranslationAlternatives` (`backend/Pugling.Contracts/Creator/VocabularyStoreDtos.cs:36`–`:40`;
  `backend/Pugling.Api/Controllers/Creator/VocabularyStoreController.cs:279`–`:294`). Mit `reuseExisting`
  ergänzt es an einem vorhandenen Eintrag aber **nur** Alternativen und Tags, Wortart und Formen nicht
  (`:263`–`:272`).
- `PATCH creator/vocabulary/batch` setzt Wortart, `Noun` und `Verb` an Bestandseinträgen nach Id
  (`:553`–`:556`, Logik `:342`–`:344`).
- Eine Übung kann statt Inline-Items auch `refs` auf Store-Ids tragen (`ExerciseItemService.cs:26`–`:40`).
  `laden.py` kennt das Feld schon (`laden.py:39`–`:40`), nutzt es aber nicht.
- `GET creator/vocabulary?incomplete=true` liefert genau die Einträge mit `Other` oder fehlenden Nomen- bzw.
  Verbdaten (`VocabularyStoreController.cs:116`–`:119`). Das Vater-Web kann Wortart und Nomendaten je Wort
  bearbeiten (`frontend/src/vater/VaterVocab.tsx:396`–`:424`). Den `incomplete`-Filter zeigt es nicht an.

**Wer die Angaben heute liest:**

- Nur die Wortart wirkt beim Spielen. `MayDropArticle` lässt den Artikel bei `Noun` **und** `Other` weg
  (`backend/Pugling.Api/Services/Shared/ExerciseContentResolver.cs:146`–`:147`). Die Begründung im Kommentar
  (`:143`–`:144`) ist genau dieser Bestand ohne Wortart. Damit bleibt B-437 für jede Buchvokabel wirkungslos.
- `Noun.Article` wird nur als Hinweis auf der Karte gezeigt (`ExerciseContentResolver.cs:155`–`:164`). `Genus`,
  `Plural` und `Verb` liest beim Spielen niemand. Der KI-Creator gibt die Wortart nur beim Bildermotiv weiter
  (`backend/Pugling.Agent.Creator/Images/MotifPlanner.cs:70`).
- **`NounInfo` beschreibt die deutsche Seite**, nicht das Lernwort: Artikel, Genus und Plural „in the target
  language“ (`backend/Pugling.Contracts/Common/VocabBaseTypes.cs:50`–`:55`), und `TargetLanguage` ist die
  Muttersprache (`backend/Pugling.Api/Models/VocabEntities.cs:19`–`:23`). Seed-Kommentar dazu: „Article: the
  German one“ (`Seed.cs:717`), Beispiel `house` → `das`, Neutrum, `Häuser` (`Seed.cs:932`). Für „la mer – das
  Meer, im Französischen feminin“ hat das Schema also **keinen Platz**.
- `VerbInfo` beschreibt **eine** gebeugte Form (Tempus, Person, Numerus; `VocabBaseTypes.cs:59`–`:71`). Stammformen
  bildet das Modell nur als eigene Einträge mit `BaseFormId` ab, nicht als Felder.

**Was die Bücher hergeben** (TSV-Hinweise gezählt, Fotos nicht neu gelesen):

- **Latein (VIVA 1, 88 Wörter):** 70 Zeilen mit Hinweis, meist Längen („lang: māter“). Genus einmal
  („· Neutrum“), Wortart vereinzelt („· Adverb“). Das Buch druckt in den ersten Lektionen **keinen Genitiv und
  kein Genus** zu den Nomen, nur Kürzel wie *(Adv.)* oder einzelne Formen („currere (*3. Pers. Pl.* currunt)“,
  `raw/latin/viva-1-pugling/md/s120-121.md`). Ein Genus für „servus“ stünde also nicht auf dem Foto, sondern käme
  aus dem Wissen des Modells.
- **Französisch (Découvertes 2, 127 Wörter):** Genus steht entweder im Wort (`(f.)`/`(m.)` nach `l'…`) oder im
  Hinweis („im Französischen feminin“), zehnmal. Bei „le/la/un/une“ steckt es im Artikel des Worts selbst.
- **Englisch (Access 5, 732 TSV-Zeilen):** kein Genus. Die Wortart ist am Wort ablesbar
  („(to) add“ = Verb). Unregelmäßige Formen stehen nicht im Hinweis.

## Die echte Lücke

Die Store-API ist fertig, gefüllt wird sie nicht. Der Import kennt keinen Kanal für strukturierte Angaben (keine
TSV-Spalte, kein Inline-Item-Feld, kein Store-Aufruf), und der Bestand wird von keinem Lauf mehr angefasst.
Daneben fehlt dem Schema ein Ort für das, was beim Sprachenlernen am meisten zählt: das Genus des **Lernworts**.
`NounInfo` hält nur das deutsche.

## Offene Punkte

1. ~~**Woher kommt die Wortart:** neue TSV-Spalte `wortart`, die Opus beim Aufbereiten füllt, oder abgeleitet
   (`(to) …` → Verb, Artikel vorne → Nomen)?~~ → Entscheidungen 2 und 7.
2. ~~**Wie kommt sie in den Store:** (a) erst `POST vocabulary/batch`, Übungen über `refs`; (b) Feld
   `partOfSpeech` am `VocabItem` (Vertragsänderung); (c) nach dem Laden per `PATCH vocabulary/batch`?~~ →
   Entscheidung 4.
3. ~~**Genus des Lernworts:** neues Feld (Migration + Vertragsbruch) oder Hinweis-Freitext?~~ → Entscheidung 1
   (eigene Story, sobald eine Übung es liest).
4. ~~**Bestand:** Welche der 372 `Other`-Einträge bekommen die Wortart nachgetragen?~~ → Entscheidung 6.
5. ~~**Homonyme und geteilte Einträge:** Wer gewinnt, wenn zwei TSVs einem Eintrag verschiedene Wortarten
   geben?~~ → Entscheidung 5.
6. ~~**Verbformen und Stammformen:** einbeziehen oder zurückstellen?~~ → Entscheidung 1 (zurückgestellt).
7. ~~**Mehrwort-Einträge** (beim Grillen aufgekommen): Wie ordnet Opus „le professeur principal“, „a few“,
   „avoir l'air“, „necesse est“ ein?~~ → Entscheidung 3.

## Entscheidungen

Grill-Runde mit dem Vater, 2026-11-17. Alle nach Empfehlung.

1. **Nur die Wortart.** Das Genus des Lernworts wird eine eigene Story, sobald eine Übung es liest. Verb- und
   Stammformen sind zurückgestellt. Heute liest beim Spielen nur die Wortart etwas aus dem Store, und das Genus
   des Lernworts bräuchte ein neues Feld (`NounInfo` beschreibt die deutsche Seite), also Migration und
   Vertragsbruch für einen Wert ohne Leser. *Kosten:* Das Genus aus dem Buch bleibt vorerst Freitext im
   `hinweis`. Für die spätere Story: Das Genus des Lernworts braucht projektweit einen eigenen Namen, getrennt
   von `Noun.Genus` (deutsche Seite).
2. **Neue Pflichtspalte `wortart` in `vokabeln-unit<N>.tsv`, Opus füllt sie beim Aufbereiten.** Ableiten am Wort
   trifft Latein gar nicht („puer“) und Adjektive oder Adverbien nie. In der TSV ist jeder Fehler sichtbar und
   dort korrigierbar. *Kosten:* Nachtrag in den vorhandenen TSVs (siehe 6); `SKILL.md`, `FORMAT.md` und
   `TSV_COLUMNS` (`laden.py:26`) ändern sich.
3. **Mehrwort-Einträge nach dem Kern:** Artikel + Nomengruppe = `Noun` („le professeur principal“, „the Tube“;
   die Artikel-Toleranz aus B-421 bleibt). Verb mit Ergänzung = `Verb` („avoir l'air“). `Phrase` nur für
   Wendungen und Sätze („necesse est“, „And anyway, …“). „a few“ = `Pronoun` bzw. `Adjective`, also ohne
   Toleranz. *Kosten:* eine kurze Einordnungsregel im Skill; Grenzfälle bleiben Urteil des Aufbereiters.
4. **`laden.py` setzt die Wortart nach dem Laden per `PATCH creator/vocabulary/batch`.** Es liest nach dem
   Anlegen die Items jeder Vokabelübung (mit Store-Id) und ordnet sie den TSV-Zeilen zu. Das läuft auch für
   Übungen, die „schon da“ sind, und heilt so den Bestand ohne `--ersetzen`. *Kosten:* ein zusätzlicher
   Aufruf je Übung; keine Backend-Änderung, keine Migration, kein Vertragsbruch.
5. **Eine vorhandene Wortart ≠ `Other` bleibt.** `laden.py` setzt nur, wo `Other` steht. Widerspricht die TSV
   einem gesetzten Wert, gibt es eine Warnung mit Wort und beiden Werten. Das schützt eine Korrektur des Vaters
   im Vater-Web und verhindert, dass zwei Ordner sich gegenseitig umschreiben. *Kosten:* Eine falsche Wortart im
   Store korrigiert die TSV nicht, nur das Vater-Web.
6. **Nachtrag für die drei geladenen Ordner:** `raw/english/access-pugling` (Access Band 3, 124 Zeilen),
   `raw/france/decouvertes-serie-bleue-2-pugling` (127), `raw/latin/viva-1-pugling` (88). Access 5 (732 Zeilen,
   nicht geladen) bekommt die Spalte beim ersten Laden; die Pflichtspalte erzwingt das. Seed-, KI-Creator- und
   Vater-Web-Vokabeln bleiben `Other`. Den `incomplete`-Filter im Vater-Web sichtbar zu machen ist eine eigene
   kleine Story. *Kosten:* Dieser Restbestand bleibt beim Artikel so tolerant wie heute.
7. **Werte sind die englischen Enum-Namen** (`Noun`, `Verb`, `Adjective`, `Adverb`, `Pronoun`, `Preposition`,
   `Conjunction`, `Article`, `Numeral`, `Interjection`, `Phrase`, `Other`). Leer ist ein Fehler. `Other` ist die
   bewusste Entscheidung „nicht einzuordnen“ und bleibt tolerant wie heute (`MayDropArticle` unverändert).
   *Kosten:* englische Wörter in einer sonst deutschen TSV; dafür keine Übersetzungstabelle neben dem Enum.

## Akzeptanzkriterien

1. `laden.py --pruefen` meldet eine leere `wortart` und einen Wert außerhalb der Enum-Namen als Fehler, bevor
   etwas geladen wird.
2. Nach einem Lauf über die drei Ordner aus Entscheidung 6 liefert `GET creator/vocabulary?incomplete=true` für
   deren Wörter nur noch die, die in der TSV ausdrücklich `Other` tragen.
3. Ein zweiter Lauf über denselben Ordner ändert keine Store-Id und keinen `ItemProgress` und legt keine Vokabel
   doppelt an.
4. Eine Buchvokabel, die kein Nomen ist, bekommt keine Artikel-Toleranz mehr: „fois“ für „une fois“ (einmal,
   Découvertes 2, als `Adverb` eingeordnet) ist falsch. Eine Nomengruppe („le professeur principal“) behält sie:
   „professeur principal“ ist richtig. („a few“ aus der Grill-Runde steht in keinem geladenen Ordner; „une fois“
   ist derselbe Fall aus dem echten Bestand.)
5. Hat eine Store-Vokabel schon eine Wortart ≠ `Other` und sagt die TSV etwas anderes, bleibt der Wert, und
   `laden.py` gibt eine Warnung mit Wort und beiden Werten aus.
6. Der Skill `lehrwerk-aufbereiten` beschreibt die Spalte samt Einordnungsregel aus Entscheidung 3.

## Schätzung

Geschätzt am 2026-11-17 gegen `cc0ec770`.

- **Größe M.** Code ist wenig (eine Spalte, eine Prüfung, ein Nachlauf in `laden.py`, Tests), dazu kommt der
  Datennachtrag über drei Ordner mit rund 340 Zeilen, je Zeile ein Urteil. Größer als S („`childId` aus dem
  Test-Pfad ziehen“), kleiner als L: keine Migration, kein Backend.
- **wo: doku.** Geändert werden nur Skill-Dateien unter `.claude/skills/lehrwerk-aufbereiten/` (`laden.py`,
  `test_laden.py`, `SKILL.md`, `FORMAT.md`) und die TSVs unter `raw/` (gitignored). Kein C#, kein Frontend,
  darum kein Reviewer-Agent. Die Markdown-Dateien müssen durch den Markdown-Lint.
- **migration: nein.** Die Wortart ist eine vorhandene Spalte (`Vocabulary.PartOfSpeech`).
- **vertragsbruch: nein.** Genutzt werden nur vorhandene Endpunkte: `GET …/vocabulary/{exerciseId}/items`
  (`backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs:175`, liefert `VocabularyId` je Item,
  `backend/Pugling.Contracts/Creator/ExerciseAuthoringDtos.cs:78`) und `PATCH creator/vocabulary/batch`
  (`VocabularyStoreController.cs:554`).

### Risiken

1. **Zuordnung Item → TSV-Zeile.** Die Items liefern `Front`/`Back` **live aus dem Store**. Hat der Vater ein
   Wort im Vater-Web umbenannt, trifft ein Textvergleich nicht mehr. *Gegenmittel:* über die Position zuordnen
   (je Abschnitt eine Übung, Items in TSV-Reihenfolge, `laden.py:447`–`:452`) und den Text nur gegenprüfen.
   Bei Abweichung: Warnung, kein PATCH.
2. **Spaltenprüfung ist exakt** (`laden.py:197`). Ab dem Umbau schlägt `--pruefen` für jeden Ordner ohne
   Nachtrag fehl, auch für Access 5 und `access-pugling/alt`. Gewollt nach Entscheidung 6; `alt/` ist Archiv
   und wird nicht geladen.
3. **Falsche Einordnung durch Opus.** Ein Nomen als `Adjective` verliert still die Artikel-Toleranz und kostet
   das Kind Punkte. *Gegenmittel:* Vor dem Lauf gegen die echte DB eine Liste je Wortart zum Durchsehen
   ausgeben (`laden.py --pruefen` druckt die Verteilung und alle Nicht-Nomen mit Artikel vorne).
4. **Lauf gegen die echte DB.** Die Söhne lernen darauf. Der PATCH ändert nur `PartOfSpeech`, sonst nichts;
   trotzdem zuerst gegen eine Kopie (`scripts/heimnetz.ps1 -Database <kopie> -Port <anderer>`), das Startskript
   legt zusätzlich bei jedem Start eine Sicherung an.

### Angriffsplan

1. `laden.py`: `wortart` in `TSV_COLUMNS`, Prüfung auf leer und auf die Enum-Namen, Verteilungsausgabe für
   `--pruefen` (AK 1, Risiko 3).
2. `laden.py`: Nachlauf nach dem Anlegen bzw. „schon da“: Items je Vokabelübung lesen, positionsweise zuordnen,
   nur `Other` patchen, Widerspruch und Textabweichung warnen (AK 2, 3, 5; Risiko 1).
3. `test_laden.py`: Fälle offline mit gemocktem `api.call` (siehe Testweg).
4. `SKILL.md` und `FORMAT.md`: Spalte, Werte und Einordnungsregel aus Entscheidung 3 (AK 6).
5. Nachtrag der Spalte in `raw/english/access-pugling`, `raw/france/decouvertes-serie-bleue-2-pugling`,
   `raw/latin/viva-1-pugling`; Liste aus Schritt 1 mit dem Vater durchsehen.
6. Lauf gegen eine DB-Kopie, dann gegen die echte DB.

### Testweg

- **Offline, in CI:** `test_laden.py` (läuft in `.github/workflows/ci.yml:93`). Neue Fälle: leere und unbekannte
  `wortart` → Fehler; PATCH nur für `Other`; vorhandener abweichender Wert → kein PATCH, Warnung; umbenanntes
  Store-Wort → kein PATCH, Warnung; zweiter Lauf schickt keinen PATCH mehr. Je Fall benennen, welche Änderung
  ihn rot macht (Regel aus der Root-`CLAUDE.md`).
- **Gegen eine DB-Kopie:** `laden.py` über die drei Ordner, danach
  `GET creator/vocabulary?incomplete=true&sourceLanguage=<la|fr|en>` → nur die bewusst als `Other` markierten
  Wörter (AK 2). Zweiter Lauf: keine Änderung an Ids, `ItemProgress`-Zeilenzahl gleich (AK 3).
- **AK 4 am Server:** `POST creator/exercises/{id}/preview/check` auf der Kopie, auf einer getippten Stufe in
  Richtung Französisch, mit „fois“ für „une fois“ (falsch) und „professeur principal“ für „le professeur
  principal“ (richtig). Die Regel selbst deckt
  `ArtikelToleranzTests` schon ab; geprüft wird hier nur, dass die Daten sie erreichen.
- **Rollengang:** Friedrich bzw. Theodor spielen danach ohnehin weiter; vor der Abnahme eine Lektion als Sohn
  auf der Kopie mit einer Nicht-Nomen-Vokabel.

## Verlauf

- **2026-11-17** — angelegt aus dem Chat mit dem Vater; Zahlen an einer DB-Kopie gezählt.
- **2026-11-17** — ausformuliert: Importweg, Store-API und Leser gegen `1ba66737` belegt, Buch-TSVs und
  VIVA-Seitenmarkdown auf Grammatikangaben gezählt. Neu dabei: `NounInfo` beschreibt die deutsche Seite, nicht
  das Lernwort.
- **2026-11-17** — gegrillt mit dem Vater: sieben Entscheidungen, alle nach Empfehlung. Umfang auf die Wortart
  verengt (keine Migration, kein Vertragsbruch), Genus des Lernworts als spätere eigene Story.
- **2026-11-17** — geschätzt: M, doku, keine Migration, kein Vertragsbruch. Beispiel in AK 4 von „a few“ auf
  „une fois“ getauscht (gleicher Fall, steht im geladenen Bestand; „a few“ in keinem Ordner).
