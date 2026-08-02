# Anmerkungen – Export

Stand: 2026-08-02 15:43 UTC · 13 Einträge · Filter: alle

> Erzeugt von `GET api/v1/remarks/export`. **Nicht von Hand bearbeiten** – die Quelle ist
> die Datenbank. Status und Antworten ändert der Skill `anmerkungen` über die API.

## #1 · Ui · eingeplant

- **Erfasst:** 2026-08-02 13:54 UTC von Konto 6 (Supervisor)
- **Wo:** `/vater/kind/3` (vater)
- **Bezug:** Kind 3

Das VAterdesign könnte als responsive website einen "polish" vertragen.

**Letzte Fehler:**

```json
[{"kind":"http","method":"POST","path":"/api/v1/auth/adult","status":401,"code":"invalid_credentials","at":"2026-08-02T13:48:33.750Z"}]
```

**Antwort** (claude-code, 2026-08-02 15:29 UTC):

Trifft zu, belegt: das Vater-Web hat KEINEN eigenen Breakpoint. Die einzige max-width-Media-Query in
frontend/src/index.css:399 gilt der Produktseite (.lp-*). .vater-main ist fest max-width 1000px (index.css:227),
.form-grid fliesst immerhin per auto-fit minmax(180px,1fr) (index.css:259) - aber .table ist width:100% ohne
overflow-Container (index.css:237), breite Listen wie /vater/lehrwerke (5 Spalten) laufen auf schmalen Schirmen
also ueber. Wunsch, kein Defekt.

**Verlauf** (1):

> **claude-code** · 2026-08-02 15:41 UTC, Konto 1
>
> Eingeplant als Story B-68 (Vater-Web responsive, art: Wunsch, P3). Analyse ist als Ist-Stand mitgewandert.

## #2 · Content · eingeplant

- **Erfasst:** 2026-08-02 14:09 UTC von Konto 6 (Supervisor)
- **Wo:** `/vater/lehrwerke` (vater)

Die Reihenfolge beim hinzufügen von Lehrwerke:
Fach, Schulart, Verlag, Reihe
Wenn FAch ein Sprachfach ist, soll die Lernsprache ausgewählt werden.
Muttersprache und Lernsprache sollen Pull-Down-Auswahl sein, kein Freitext.
REihe und Verlag sollten ebenfalls Pulldown-Felder sein die auseinander aufbauen. Wird also das FAch ausgewählt, erscheinen nur Verläge die auch Englisch als Fach anbieten. Es gibt immer die Auswahl "default".
Die Suche nach Lehrwerke soll über div. PArameter möglich sein.
Das Thema geht über die UI hinaus und ist eine change des gesamten Models, da viele Freitexte durch feste Werte ersetzt wird. Dies macht aber die wiederverwendung von Lernwerken einfacher.

**Antwort** (claude-code, 2026-08-02 15:29 UTC):

Trifft zu, und deine Einschaetzung "geht ueber die UI hinaus" stimmt.
Ist-Stand Formular (frontend/src/vater/VaterLehrwerke.tsx:352-384): Reihenfolge ist Reihe, Verlag, Fach, Schulart,
Lernsprache, Muttersprache, Notiz. Fach (:361) und Schulart (:368) sind bereits Pulldowns; Verlag (:357),
Lernsprache (:375) und Muttersprache (:379) sind Freitext.
Ist-Stand Modell: TextbookSeries.Publisher/SourceLanguage/TargetLanguage sind string?
(backend/Pugling.Api/Models/CurriculumEntities.cs:22,31,33) - es gibt weder eine Verlag-Entitaet noch eine
Sprachcode-Liste, also nichts, worauf abhaengige Pulldowns aufsetzen koennten.
Ist-Stand Suche: der Endpunkt kennt nur search (Name/Verlag), subjectId und mineOnly
(Controllers/Creator/TextbookSeriesController.cs:48-50). Schulart, Verlag oder Band sind nicht filterbar.

**Verlauf** (1):

> **claude-code** · 2026-08-02 15:41 UTC, Konto 1
>
> Eingeplant als Story B-63 (Lehrwerk-Hierarchie, art: Wunsch, P2) - gemeinsam mit #3, #4, #5, #6 und #7, weil alle sechs denselben Schnitt betreffen. Dieser Punkt steckt dort in den offenen Punkten 2 und 5 (Verlag als Entitaet, Sprachen als Liste) und im Akzeptanzkriterium 5 (Suchparameter).

## #3 · Content · eingeplant

- **Erfasst:** 2026-08-02 14:22 UTC von Konto 6 (Supervisor)
- **Wo:** `/vater/lehrwerke` (vater)

"Themen der Unit" soll ein Array sein.
Entsprechend können
Beispiel für Englisch, Unit 1:
- Talking about holidays
- Reading in a street map
- Reading a Tube map

**Antwort** (claude-code, 2026-08-02 15:29 UTC):

Trifft zu: SeriesUnit.Topics ist ein einzelnes string? (backend/Pugling.Api/Models/CurriculumEntities.cs:63),
im UI ein einzeiliges Textfeld (frontend/src/vater/VaterLehrwerke.tsx:273-277). Mehrere Themen gehen heute nur
als Komma-Liste in einem Freitext - such- und verknuepfbar ist das nicht.

**Verlauf** (1):

> **claude-code** · 2026-08-02 15:41 UTC, Konto 1
>
> Eingeplant als Story B-63 (Lehrwerk-Hierarchie, art: Wunsch, P2), offener Punkt 4 (Themen als Freitext-Array) und Akzeptanzkriterium 3.

## #4 · Content · eingeplant

- **Erfasst:** 2026-08-02 14:24 UTC von Konto 6 (Supervisor)
- **Wo:** `/vater/lehrwerke` (vater)

Grammatik der Unit soll ebenfalls ein Array sein. Am besten verknüpft um diese Werte Buchübergreifend zu nutzen, da Grammatik ein Sprachübergreifendes Thema ist.

**Antwort** (claude-code, 2026-08-02 15:29 UTC):

Trifft zu: SeriesUnit.Grammar ist string? (backend/Pugling.Api/Models/CurriculumEntities.cs:65), im UI ein
einzeiliges Feld (frontend/src/vater/VaterLehrwerke.tsx:280-284). Eine Grammatik-Entitaet gibt es NICHT - die
Suche nach "class Grammar*" im Backend trifft nur den Uebungstyp (Exercises/BuiltInExerciseTypes.cs:52,
Contracts/Exercise/ExerciseConfigs.cs:112, Controllers/Creator/ExerciseControllers.cs:343), nicht den Stoff.
Vorbild fuer eine geteilte, slug-idempotente Taxonomie waere VocabTag bzw. InterestTag.

**Verlauf** (1):

> **claude-code** · 2026-08-02 15:41 UTC, Konto 1
>
> Eingeplant als Story B-63 (Lehrwerk-Hierarchie, art: Wunsch, P2), offener Punkt 3 (Grammatik als geteilte, slug-idempotente Taxonomie) und Akzeptanzkriterium 3.

## #5 · Content · eingeplant

- **Erfasst:** 2026-08-02 14:27 UTC von Konto 6 (Supervisor)
- **Wo:** `/vater/lehrwerke` (vater)

Bei der Grammatik soll entsprechend ein Such oder auswahl-berech sein um bereits vorhandene Grammatik-Werte zu übernehmen.
Es gibt oft auch weitere Unterlagen aus der gleichen Buchreihe die gleiche Themen unterschiedlich aufbereiten. Es gibt neben dem Hauptbuch, noch Lernbücher und Übungsbücher die zusätzliche Aufgben zum selben Bereich anbieten. Damit diese Verknüpfung zweifelsfrei erfolgt müssen die Krammatik-Werte eindeutig sein (id).
Evtl. kann mach diese Querverseise zu Übungen und/oder Büchern ebenfalls festhalten/ermöglichen

**Antwort** (claude-code, 2026-08-02 15:29 UTC):

Haengt an #4 und trifft zu: ohne eigene Entitaet gibt es keine Auswahlliste, keine Suche und vor allem keine
stabile Id, an der ein Querverweis haengen koennte (SeriesUnit.Grammar ist Freitext,
backend/Pugling.Api/Models/CurriculumEntities.cs:65).
Zweiter Punkt ebenfalls offen: weitere Buecher derselben Reihe (Lernbuch, Uebungsbuch) kennt das Modell nicht -
TextbookSeries -> SeriesUnit ist genau zweistufig (CurriculumEntities.cs:41,51), ein Buchtyp-Feld existiert nicht.

**Verlauf** (1):

> **claude-code** · 2026-08-02 15:41 UTC, Konto 1
>
> Eingeplant als Story B-63 (Lehrwerk-Hierarchie, art: Wunsch, P2), offene Punkte 3 (Grammatik-Ids) und 6 (weitere Buchtypen derselben Reihe).

## #6 · Ui · eingeplant

- **Erfasst:** 2026-08-02 14:29 UTC von Konto 6 (Supervisor)
- **Wo:** `/vater/lehrwerke` (vater)

In der Übersicht der Lehrwerke muss auch das Band erscheinen.

**Antwort** (claude-code, 2026-08-02 15:29 UTC):

Trifft zu: die Uebersicht zeigt Reihe, Fach, Schulart, Units (frontend/src/vater/VaterLehrwerke.tsx:56); der
Band steht nur in der aufgeklappten Unit-Tabelle (:152 Spaltenkopf, :158 Wert).
Abhaengigkeit, die vor dem Bauen zu klaeren ist: der Band haengt heute an der Unit
(SeriesUnit.Grade, backend/Pugling.Api/Models/CurriculumEntities.cs:57), eine Reihe kann also mehrere Baende
tragen. Eine Spalte muesste heute aggregieren ("Baende 7-9"). Eindeutig wird sie erst nach dem Umbau aus #7.

**Verlauf** (1):

> **claude-code** · 2026-08-02 15:41 UTC, Konto 1
>
> Eingeplant als Story B-63 (Lehrwerk-Hierarchie, art: Wunsch, P2), Akzeptanzkriterium 4. Bewusst dort und nicht als Einzelfix: solange der Band an der Unit haengt, muesste die Spalte aggregieren - eindeutig wird sie erst nach dem Umbau aus #7.

## #7 · Content · eingeplant

- **Erfasst:** 2026-08-02 14:32 UTC von Konto 6 (Supervisor)
- **Wo:** `/vater/lehrwerke` (vater)

Band muss aus Unit raus und in Lehrwerk ziehen. 
Verbindung:
Verlag 1:n Fach 1:n Reihe 1:n Band 1:n Units

**Antwort** (claude-code, 2026-08-02 15:29 UTC):

Der Ist-Stand ist hier eine bewusste Entscheidung, keine Luecke: "Band und Unit liegen bewusst in EINER
Ebene (Grade = Band)" - backend/Pugling.Api/Models/CurriculumEntities.cs:44-47, dieselbe Aussage in
backend/Pugling.Api/CLAUDE.md unter "Unterrichtsmaterial & Creator-Profile". Der Verlag ist heute gar keine Ebene,
sondern ein string? an der Reihe (CurriculumEntities.cs:22).
Was an deinem Umbau haengt (Verlag 1:n Fach 1:n Reihe 1:n Band 1:n Unit):
- Textbook.SeriesId/CurrentUnitId am Kind zeigt heute auf Reihe + Unit (Models/AdminEntities.cs:168-175) -
  eine Band-Ebene dazwischen bricht diesen Vertrag.
- CreatorProfile.SeriesId (CurriculumEntities.cs:98) und das Matching-Gewicht "Reihe 8" haengen ebenfalls an
  der Reihe.
- Fach 1:n Reihe ist heute nur eine optionale Verknuepfung (SubjectId nullable, CurriculumEntities.cs:26).
Bemerkenswert: den Band gibt es bereits - aber auf der KIND-Seite (Textbook.Grade, AdminEntities.cs:161), nicht
im geteilten Katalog.

**Verlauf** (1):

> **claude-code** · 2026-08-02 15:41 UTC, Konto 1
>
> Eingeplant als Story B-63 (Lehrwerk-Hierarchie, art: Wunsch, P2). Diese Anmerkung ist der TRAEGER der Story - offener Punkt 1 ist die Frage, ob die dokumentierte Ein-Ebenen-Entscheidung umgekehrt wird; faellt sie negativ aus, sind die uebrigen sechs Punkte gegenstandslos. Empfehlung in der Story: ja.

## #8 · Content · eingeplant

- **Erfasst:** 2026-08-02 14:36 UTC von Konto 6 (Supervisor)
- **Wo:** `/vater/fachlehrer` (vater)

Wenn beim anlegen des Fachlehrer ein Lehrwerk angegeben wird, ist das Fach schon bekannt. Die Lern- und Muttersprache eigentlich auch.

**Antwort** (claude-code, 2026-08-02 15:29 UTC):

Trifft zu, und es ist reine UI-Arbeit. Im Formular sind Fach (frontend/src/vater/VaterFachlehrer.tsx:221),
Lehrwerk (:249), Lernsprache (:258) und Muttersprache (:262) vier voneinander unabhaengige Eingaben; die beiden
Sprachfelder sind Freitext mit Vorgabe en/de (:140-141).
Die Daten zum Ableiten liegen bereits vor: TextbookSeries traegt SubjectId/SubjectName und
SourceLanguage/TargetLanguage (backend/Pugling.Api/Models/CurriculumEntities.cs:24-33). Das Modell muss also
nicht geaendert werden - es fehlt nur das Vorbelegen beim Waehlen der Reihe. Die Felder muessen dabei
beschreibbar bleiben: ein Profil ohne Reihe (SeriesId ist nullable) braucht sein Fach weiter selbst.

**Verlauf** (1):

> **claude-code** · 2026-08-02 15:41 UTC, Konto 1
>
> Eingeplant als Story B-67 (Fachlehrer aus Lehrwerk vorbelegen, art: Wunsch, P2, wo: frontend). Analyse ist als Ist-Stand mitgewandert; das Modell muss nicht geaendert werden.

## #9 · Content · eingeplant

- **Erfasst:** 2026-08-02 14:40 UTC von Konto 6 (Supervisor)
- **Wo:** `/vater/katalog` (vater)

Fächer und Lehrwerke scheint mir doppelt zu sein. Eigentlich kann ein Vater ein indivuelles Lehrwerk erstellen und somit seinen Katalog erstellen. Hier sind die Arten auch schon als Liste enthalten die ich im Lehrwerk vermisse.

**Antwort** (claude-code, 2026-08-02 15:29 UTC):

Teils - und die echte Dopplung liegt woanders als vermutet.
Im Modell sind Fach und Lehrwerk zwei verschiedene Dinge: Subject -> Chapter -> Exercise traegt die UEBUNGEN
(Exercise.ChapterId, backend/Pugling.Api/Models/LearnEntities.cs:54), TextbookSeries -> SeriesUnit traegt den
STOFF (Topics/Grammar/VocabularyNotes, Models/CurriculumEntities.cs:62-67) und keine einzige Uebung. Verbunden
sind beide ueber TextbookSeries.SubjectId (CurriculumEntities.cs:26). Ein Vater kann also NICHT ueber ein
Lehrwerk seinen Katalog erstellen - an einer Unit haengt kein Inhalt, nur eine Notiz.
Doppelt ist dagegen: Textbook (das Buch AM KIND, Models/AdminEntities.cs:146-175 mit Title, Publisher, Grade,
CurrentChapter als Freitext) neben TextbookSeries/SeriesUnit (katalogisiert, geteilt). Dieselbe Sache zweimal.
Die vermissten "Arten" sind ExerciseCategory je Fach (LearnEntities.cs:25-32), verwaltet unter /vater/katalog -
sie sind eine kontrollierte Liste, genau das, was du in #3/#4 fuer Themen und Grammatik willst.

**Verlauf** (1):

> **claude-code** · 2026-08-02 15:41 UTC, Konto 1
>
> Eingeplant als Story B-64 (Textbook vs. TextbookSeries aufloesen, art: Wunsch, P3). Die Story haelt fest, dass Fach und Lehrwerk NICHT doppelt sind - die Dopplung liegt zwischen dem Freitext-Buch am Kind und der katalogisierten Reihe.

## #10 · Content · eingeplant

- **Erfasst:** 2026-08-02 14:45 UTC von Konto 6 (Supervisor)
- **Wo:** `/vater/exercises/neu?subjectId=1` (vater)

Übungen sollen zu einem Kapitel hinzugefügt werden. Damit gehören Übungen immer zu einem Kapitel. Diese Abhängigkeit kann gut im REST abgebildet werden.
Damit sind viele Werte der Übung hinfällig (Klasse von, Klasse bis, Quelle, Schulart, etc.)

**Antwort** (claude-code, 2026-08-02 15:29 UTC):

Der erste Satz ist bereits umgesetzt: Exercise.ChapterId ist nicht nullable
(backend/Pugling.Api/Models/LearnEntities.cs:54), die Route lautet api/v1/creator/subjects/{}/chapters/{}/<typ>,
und das Formular verlangt beides, bevor es sendet (frontend/src/vater/VaterExerciseCreate.tsx:103-104,
Anlegen :135). Eine Uebung ohne Kapitel kann nicht entstehen.
Der zweite Satz trifft heute NICHT zu: Klasse von/bis, Schulart und Quelle sind nicht redundant, weil das
Kapitel nur Name und OrderIndex traegt (LearnEntities.cs:35-44) und das Fach nur den Namen (:8-18) - es gibt
oben nichts, was diese Angaben ersetzen koennte. Sie liegen an der Uebung (LearnEntities.cs:86-93). Redundant
wuerden sie erst, wenn Klassenstufe/Schulart ans Kapitel wandern; das waere der eigentliche Vorschlag und ein
Modellumbau.

**Verlauf** (1):

> **claude-code** · 2026-08-02 15:41 UTC, Konto 1
>
> Erste Haelfte ist beantwortet (Uebungen haengen bereits zwingend am Kapitel) - dafuer entsteht keine Story. Die zweite Haelfte (Klassenstufe/Schulart ans Kapitel ziehen) steht als offener Punkt 7 in Story B-63 (Lehrwerk-Hierarchie), mit der Empfehlung, sie zurueckzustellen: es ist ein eigener Umbau am Uebungs-Katalog.

## #11 · Content · eingeplant

- **Erfasst:** 2026-08-02 15:05 UTC von Konto 6 (Supervisor)
- **Wo:** `/vater/exercises/neu?subjectId=1` (vater)

Bei Vokabeln gibt es oft mehrfachdeutungen. Bei einer Rückwärtssuche würden die Werte nicht passen.
Beispiel:
en: huge -> de: riesig
en: huge -> de: riesig

**Antwort** (claude-code, 2026-08-02 15:29 UTC):

Trifft zu. Eine Vocabulary-Zeile ist genau EIN Paar Word -> Translation
(backend/Pugling.Api/Models/VocabEntities.cs:19-22); ein Alternatives-Feld gibt es nur an der Luecke, nicht an
der Vokabel (Services/Shared/AnswerGrader.cs:19-24 gegenueber :12-16). Bewertet wird auf exakte Gleichheit nach
Normalisierung gegen die EINE gespeicherte Loesung (AnswerGrader.cs:12-16).
Ausfuehrliche Antwort samt Richtungsanalyse steht an #12.

**Verlauf** (1):

> **claude-code** · 2026-08-02 15:41 UTC, Konto 1
>
> Eingeplant als Story B-65 (Vokabel 1:n Uebersetzung, art: DEFEKT, P1) - zusammen mit #12. Hoechste Prio im Stapel, weil hier eine RICHTIGE Antwort als falsch gewertet wird und der Muenz-Malus daran haengt.

## #12 · Content · eingeplant

- **Erfasst:** 2026-08-02 15:08 UTC von Konto 6 (Supervisor)
- **Wo:** `/vater/exercises/neu?subjectId=1` (vater)

zu Frage 11:
en: huge -> de: riesig
en: huge -> de: sehr groß
Gegenprobe müsste dann sein:
de: riesig -> en: huge
de: sehr groß -> en: huge

Es müsste entweder ein Entrag pro Kombination geben oder die Übersetzung müsste ein Array sein: en 1:n de
bzw:
src 1:n target

**Antwort** (claude-code, 2026-08-02 15:29 UTC):

Trifft zu, und deine Gegenprobe zeigt genau die Asymmetrie.
"Ein Eintrag pro Kombination" ist der IST-Zustand: zwei Vocabulary-Zeilen huge->riesig und huge->sehr gross
(Models/VocabEntities.cs:19-22).
Rueckwaerts funktioniert das: der Richtungstausch vertauscht Frage und Antwort je Item
(Services/Shared/ExerciseContentProvider.cs:50-55), riesig->huge und "sehr gross"->huge sind beide richtig.
Vorwaerts nicht: das Kind sieht "huge" zweimal, und der Vergleich laeuft auf exakte Gleichheit gegen die eine
Loesung des jeweiligen Items (Services/Shared/AnswerGrader.cs:12-16) - eine der beiden korrekten Antworten wird
als falsch gewertet. Es gibt kein Alternatives an der Vokabel (nur an der Luecke, AnswerGrader.cs:19-24).
Der Fix ist damit deine zweite Variante (1:n Uebersetzung bzw. Alternatives an der Vokabel), nicht mehr Zeilen.
Nebenwirkung, die dabei zu klaeren ist: der Richtungstausch verwirft Alternativen ausdruecklich, weil sie zur
alten Antwort gehoerten (ExerciseContentProvider.cs:57).

**Verlauf** (1):

> **claude-code** · 2026-08-02 15:41 UTC, Konto 1
>
> Eingeplant als Story B-65 (Vokabel 1:n Uebersetzung, art: DEFEKT, P1) - zusammen mit #11. Deine Gegenprobe steht dort in der Lueckenbeschreibung; die Nebenwirkung beim Richtungstausch ist als offener Punkt 3 festgehalten.

## #13 · Content · eingeplant

- **Erfasst:** 2026-08-02 15:16 UTC von Konto 6 (Supervisor)
- **Wo:** `/vater/exercises?subjectId=1` (vater)

Beim Vokabel Buchstabeneingabe sollen Leer- und Satzzeichen nicht eingegeben werden. Die sind schon da. Wenn eine Vokabel mehrere Übersetzungen hat und diese die gleiche Anzahl an Buchstaben, soll die ÜBerprüfung dies erkennen und die richtige Übersetzung bewerten auch wenn die Worte in einer anderen Reihenfolge eingegeben wurde

**Antwort** (claude-code, 2026-08-02 15:29 UTC):

Drei Punkte, alle drei bestaetigt.
1. Leer- und Satzzeichen: die Zahl der Kaestchen ist item.Answer.Length
   (backend/Pugling.Api/Exercises/VocabularyExerciseType.cs:80), und LetterBoxes rendert genau so viele
   gleichartige Einzelfelder (frontend/src/components/LetterBoxes.tsx:15,32-48). Die Komponente kennt keine
   Trennzeichen - das Kind muss das Leerzeichen selbst in ein Kaestchen tippen.
2. Mehrere Uebersetzungen gleicher Laenge: haengt an #11/#12. Solange nur eine Loesung gespeichert ist, wird die
   andere richtige als falsch gewertet - die Laenge aendert daran nichts.
3. Wortreihenfolge: Normalize trimmt, senkt Gross-/Kleinschreibung und faltet Mehrfach-Leerzeichen, sortiert die
   Woerter aber NICHT (Services/Shared/StageMechanics.cs:25-26). "gross sehr" gilt als falsch.

**Verlauf** (1):

> **claude-code** · 2026-08-02 15:41 UTC, Konto 1
>
> Eingeplant, aufgeteilt auf zwei Stories: Punkt 1 (Leer-/Satzzeichen) und Punkt 3 (Wortreihenfolge) als B-66 (Buchstabenkaestchen Trennzeichen, art: Defekt, P2); Punkt 2 (mehrere Uebersetzungen) gehoert zu B-65, weil er an der fehlenden Gleichwertigkeit haengt, nicht an der Laenge. Punkt 3 ist in B-66 als offene Entscheidung notiert, nicht als Defekt - bei einem Satz darf die Reihenfolge nicht egal sein.

