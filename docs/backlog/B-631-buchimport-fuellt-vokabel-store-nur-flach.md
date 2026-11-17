---
tags: [typ/story, status/ausformuliert, bereich/katalog, rolle/creator, rolle/student, lerntechnik/vokabeln]
aliases: [Vokabel-Store nur Wort und Übersetzung, Wortart beim Buchimport, Genus und Stammformen aus dem Lehrwerk]
status: ausformuliert
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
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

## Akzeptanzkriterien (Entwurf)

1. Nach einem Lauf von `laden.py` trägt jede Vokabel des Ordners eine Wortart ≠ `Other`, außer der Aufbereiter
   hat sie ausdrücklich offen gelassen. `GET creator/vocabulary?incomplete=true&sourceLanguage=<sprache>` liefert
   für den geladenen Ordner nur noch diese ausdrücklich offenen Wörter.
2. Ein zweiter Lauf über einen schon geladenen Ordner ergänzt Wortart und Formangaben an den **vorhandenen**
   Store-Einträgen (gleiche Ids, Lernstand und `ItemProgress` bleiben). Er legt keine Doppel an.
3. Eine Buchvokabel, die kein Nomen ist („a few“, „un peu“), bekommt die Artikel-Toleranz nicht mehr: „few“ für
   „a few“ ist falsch. Die Prüfung läuft durch `MayDropArticle`, ohne Änderung an der Regel.
4. Angaben, die nur das Modell weiß und nicht das Foto (lateinisches Genus in VIVA 1), sind von gedruckten
   unterscheidbar oder bleiben leer, je nach Entscheidung zu Punkt 3.
5. `laden.py --pruefen` meldet eine Zeile ohne Wortart bzw. einen Wert außerhalb der erlaubten
   Liste als Fehler, bevor etwas geladen wird.

## Offene Punkte

1. **Woher kommt die Wortart:** neue TSV-Spalte `wortart`, die Opus beim Aufbereiten füllt, oder abgeleitet
   (`(to) …` → Verb, Artikel vorne → Nomen)? *Empfehlung:* Spalte, von Opus gefüllt. Ableiten trifft Englisch
   und Französisch halbwegs, Latein gar nicht („puer“). Fehler sind sichtbar und billig zu korrigieren, weil
   sie in der TSV stehen.
2. **Wie kommt sie in den Store:** (a) `laden.py` legt erst per `POST vocabulary/batch` an und verweist die
   Übungen über `refs`; (b) `VocabItem` bekommt ein Feld `partOfSpeech` (Vertragsänderung); (c) `laden.py`
   lädt wie bisher und patcht danach per `PATCH vocabulary/batch` nach Id. *Empfehlung:* (c). Ohne
   Backend-Änderung, und derselbe Schritt heilt auch den Bestand (Akzeptanzkriterium 2). (a) bräche die
   Hinweise am Item, die `refs` nicht tragen (`ExerciseItemService.cs:23`).
3. **Genus des Lernworts:** `NounInfo` um `SourceGenus` (o. ä.) erweitern (Migration + Vertragsbruch), oder im
   Hinweis-Freitext lassen? Und gilt Modellwissen dafür als Quelle, wenn das Buch es nicht druckt? *Empfehlung:*
   Erst die Wortart (Punkte 1, 2) liefern, das Genus als eigene Story abspalten, sobald es eine Übung gibt, die
   es liest. Heute liest niemand `Noun.Genus` (siehe Ist-Stand). Ein Feld ohne Leser ist Disziplin ohne Tor.
4. **Bestand:** Gelten die 372 `Other`-Einträge als Teil dieser Story, auch die ohne TSV (Seed, KI-Creator,
   Vater-Web)? *Empfehlung:* nur die aus den vier Buch-Ordnern unter `raw/` (dort liegt die TSV als Quelle). Den
   Rest zeigt der `incomplete`-Filter, sobald er im Vater-Web sichtbar ist. Das wäre eine kleine eigene Story.
5. **Homonyme und geteilte Einträge:** Ein Store-Eintrag kann von Übungen aus zwei Büchern genutzt werden. Wer
   gewinnt, wenn zwei TSVs ihm verschiedene Wortarten geben? *Empfehlung:* Der erste gesetzte Wert ≠ `Other`
   bleibt. Ein Widerspruch ist eine Warnung in `laden.py`, kein Überschreiben.
6. **Verbformen und Stammformen:** einbeziehen (Latein-Perfekt, englische *irregular verbs* als eigene Einträge mit
   `BaseFormKey`) oder zurückstellen? *Empfehlung:* zurückstellen. Die Bücher drucken sie in den erfassten Units
   kaum, und kein Übungstyp liest `BaseFormId` beim Spielen.

## Verlauf

- **2026-11-17** — angelegt aus dem Chat mit dem Vater; Zahlen an einer DB-Kopie gezählt.
- **2026-11-17** — ausformuliert: Importweg, Store-API und Leser gegen `1ba66737` belegt, Buch-TSVs und
  VIVA-Seitenmarkdown auf Grammatikangaben gezählt. Neu dabei: `NounInfo` beschreibt die deutsche Seite, nicht
  das Lernwort.
