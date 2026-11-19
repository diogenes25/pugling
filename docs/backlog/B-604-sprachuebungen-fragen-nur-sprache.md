---
tags: [typ/story, status/gegrillt, bereich/katalog, rolle/creator, lerntechnik/vokabeln]
aliases: [Keine Sachfragen, Nur Sprache abfragen, Keine Satzantworten]
status: gegrillt
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Vater im Chat 2026-11-16: Fragen nach der Farbe der U-Bahn in England oder nach Städten in Frankreich, Antworten teils ganze Sätze – beim Vokabellernen irrelevant"
grund: ""
ersetzt_durch: []
wartet_auf: Bestätigung der Sonnet-Entscheidungen
---

# B-604 · Sprachübungen fragen nur Sprache ab: keine Sachfragen, keine Sätze als Antwort

Manche Übungen im Sprachbereich stellen Sachfragen aus der Landeskunde des Lehrwerks statt Sprachfragen,
zum Beispiel nach der Farbe der U-Bahn in England oder nach Städten in Frankreich. Die erwarteten Antworten
sind teils ganze Sätze. Wer so eine Frage nicht beantworten kann, scheitert an Sachwissen, nicht an der
Sprache. Eine getippte Satzantwort fällt bei binärer Wertung schon an einem Tippfehler durch, und das trifft
Friedrich mit seiner LRS besonders ([Vokabeltest-Format](B-524-vokabeltest-wie-in-der-schule.md)).

Vorgeschlagene Regel für Creator und KI-Creator: **Die Antwort ist ein Eintrag der Wortliste der Unit**, also
ein Wort oder eine feste Wendung, so wie sie in der Liste steht. Ganze Sätze dürfen nur den Kontext liefern,
etwa im Lückentext, aber nicht die Antwort sein. Eigennamen, die selbst Vokabel sind (*la Manche*,
*l'Angleterre*), bleiben erlaubt; gefragt wird dann nach dem Wort, nicht nach Erdkunde.

## User Story

Als **Lernender** (Friedrich, Theodor) möchte ich in einer Sprachübung nur Sprache gefragt werden – ein Wort
oder eine Wendung der Wortliste –, damit ein Fehler heißt „das Wort kannte ich nicht“ und nicht „das wusste
ich über England nicht“ oder „ich habe in einem langen Satz einen Buchstaben vertippt“.

Als **Creator** (Vater, Lehrer, KI-Creator) möchte ich eine klare Regel, was in eine Sprachübung gehört, damit
meine Übungen sie nicht versehentlich brechen.

## Ist-Stand am Code

Recherchiert am 2026-11-19 gegen `47791819`. Die echte `pugling.db` wird in diesem Lauf nicht angefasst – welche
Bestandsübungen betroffen sind, ist darum **nicht geprüft** (Übung 40 und die U-Bahn-Frage: nicht gefunden, nicht
nachgesehen; `raw/` ist gitignored).

- **Die Landeskunde-Zuordnung wird von einer Anleitung verlangt.** Der Skill `lehrwerk-aufbereiten` sagt für
  `uebungen.json`: „… Wendungen als `translation`, Landeskunde als `matching`“
  (`.claude/skills/lehrwerk-aufbereiten/SKILL.md:169`–`:170`). Das ist die naheliegende Quelle von „Orte in
  Frankreich, Belgien und der Schweiz“. Der Lader prüft bei `matching` nur die Form
  (`.claude/skills/lehrwerk-aufbereiten/laden.py:43`: Felder `instruction`, `pairs`, `left`, `right`); ein Abgleich
  der Paare mit der Wortliste der Unit ist dort **nicht gefunden** (`laden.py` nennt die Wortliste nur in
  Warnungen zu Zeilenzahl und Hinweisen, `:219`).
- **Der KI-Creator erzeugt Zuordnung und Lesetext gar nicht.** Registriert sind vier Strategien
  (`backend/Pugling.Agent.Creator/Program.cs:114`–`:117`: `Vocabulary`, `Cloze`, `Translation`, `Grammar`). Sachfragen
  über `Matching`/`Reading` kommen also von Hand, aus dem Lader oder aus dem Seed, nicht vom Agenten.
- **Wo der Agent die Wortliste erzwingt – und wo nicht.** Nur im Schultest-Lückentext: jede Lücke muss ein Wort der
  Liste tragen, sonst Verstoß (`ClozeStrategy.cs:90`–`:99`, `:145`–`:160`). Im gewöhnlichen Lückentext verlangt
  `CoversRequiredWords` nur, dass Pflichtwörter vorkommen (`DraftRules.cs:94`–`:109`), nie, dass alles aus der Liste
  stammt. Die Übersetzungsübung hat **Sätze als Antwort by design** (`TranslationStrategy.cs:30`–`:34`: „kurze,
  vollständige Sätze“, `target` ist der Satz) und prüft die Wortliste nur als Vorkommen im Ausgangssatz
  (`:50`, `exact: false`). Dass eine Frage eine Sach- und keine Sprachfrage ist, kann keine dieser Prüfungen sehen.
- **Der Server kennt die Regel nicht.** `MatchingExerciseType.Check` vergleicht die rechte Spalte als Text
  (`backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs:453`–`:463`), ohne Länge, Wortzahl oder Bezug zu einer
  Wortliste. Die rechte Spalte einer Zuordnung ist dort frei – auch der Seed nutzt sie für Bundesland → Hauptstadt
  (`backend/Pugling.Api/Data/Seed.cs:1375`), also genau eine Sachfrage, nur auf Deutsch.
- **Verwandt, aber anderes Problem:** [B-626](B-626-antwort-verlangt-mehr-als-die-vokabel.md) (geschätzt) befreit
  die Antwort von Platzhaltern wie „sth.“ – gleiche Wunde, andere Ursache; [B-596](B-596-vokabeluebung-hoechstens-zehn-vokabeln.md)
  (idee) begrenzt die Größe einer Vokabelübung. B-460 (abgenommen) hat den Agenten für Lückentext und Vokabeln
  deterministisch an die Wortliste gebunden; B-604 verlängert das nicht, es betrifft die Typen daneben.
- Ein Hinweis aus dem Briefing steht schon im Prompt: „Wortschatz und Satzbau passen zu Alter und Klassenstufe“
  (`backend/Pugling.Agent.Creator/Drafting/DraftPrompts.cs:51`) – eine Regel gegen Sachfragen **nicht gefunden**.

## Die echte Lücke

Es sind **zwei Regeln**, und nur eine ist maschinell prüfbar:

1. **Die Antwort ist ein Wortlisten-Eintrag** – prüfbar, aber nur dort, wo es eine Wortliste und eine Antwortspalte
   gibt (Zuordnung rechts, Vokabel, Lückentext-Lücke). Heute erzwingt das nur der Schultest.
2. **Keine Sachfragen** – ein Inhaltsurteil, das C# nicht fällen kann (die Farbe der U-Bahn ist grammatikalisch
   unauffällig). Hier hilft nur ein Regeltext an der Stelle, die die Übungen entstehen lässt: der Skill-Text
   (`SKILL.md:169`) und der Prompt-Block des Agenten.

Die eigentliche Quelle der Beispiele ist wahrscheinlich eine **Anleitung** (Landeskunde als Zuordnung), kein Defekt des
Agenten – und die Regel „Satz als Antwort“ widerspricht dem Zweck des Typs `Translation`, wenn man sie wörtlich nimmt.

## Offene Punkte

1. ~~**Wo gilt die Regel: als Regeltext, als Server-Prüfung, oder beides?**~~ → Entscheidung 1.
2. ~~**Gilt „kein Satz als Antwort“ auch für `Translation`?**~~ → Entscheidung 2.
3. ~~**Was wird aus der Landeskunde-Zuordnung?**~~ → Entscheidung 3.
4. ~~**Bestand.**~~ → Entscheidung 5.
5. ~~**Soll der Lader warnen?**~~ → Entscheidung 4.
6. ~~**Abgrenzung zu B-626.**~~ → Entscheidung 6.
7. ~~**Erreicht die Regel den Vater im Web-Editor?**~~ (beim Grillen gegen die zweite User Story gerechnet) → Entscheidung 1.

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-19 (Sonnet), unbestätigt.*

1. **Die Regel steht als Text an den Stellen, an denen Übungen entstehen – nicht als Server-Prüfung.** Das sind der
   Skill (`SKILL.md:169`), der Systemprompt des Agenten (`DraftPrompts.System`, `DraftPrompts.cs:40`–`:55`, bisher acht
   Regeln) und **eine Hilfezeile im Vater-Editor** (`frontend/src/lib/fieldHelp.ts`, `FIELD_HELP`, die zentrale Stelle
   aller „ⓘ“-Texte). Begründung: Eine Server-Prüfung der rechten Zuordnungsspalte bräche legitime Übungen – der Seed
   fragt „Bundesland → Landeshauptstadt“ (`Seed.cs` um `:1375`, Typ `Matching`) –, und C# kann „Wendung“ nicht von
   „Satz“ und „Sachfrage“ nicht von „Sprachfrage“ unterscheiden (nur die erste der beiden Regeln ist maschinell
   prüfbar, siehe „Die echte Lücke“). Die Hilfezeile kommt dazu, weil die zweite User Story den Vater als Creator
   nennt: ohne sie erreichte die Regel nur den Agenten und die Anleitung, nicht den Vater, der im Web-Editor von Hand
   anlegt. **Verworfen:** Server-Prüfung (bricht den Seed-Fall) und „nur Skill und Prompt“ (lässt den Vater ohne
   Regel). **Ausgeschlossen:** dass eine Hand-Übung des Vaters mit Sachfrage abgelehnt wird – sie bleibt möglich, die
   Regel ist ein Hinweis, kein Tor. **Kosten:** drei Textstellen, die auseinanderlaufen können; an welchem Feld des
   Editors die Hilfezeile hängt, wird beim Schätzen gezählt (die Editor-Felder sind in diesem Lauf nicht gelesen).
2. **Der Typ `Translation` behält Sätze als Antwort.** Dort heißt die Regel „Sprachfrage statt Sachfrage“: der
   Ausgangssatz enthält ein Wort der Unit (das prüft `DraftRules.CoversRequiredWords` bereits,
   `TranslationStrategy.cs:50`). „Antwort = Wortlisten-Eintrag“ gilt für `Vocabulary`, `Matching` und
   Lückentext-Lücken. Begründung: Der Typ **ist** die Satzübersetzung (`TranslationStrategy.cs:30`–`:34`: „kurze,
   vollständige Sätze“); eine Regel „kein Satz“ löschte ihn. **Verworfen:** die Regel auf alle Typen ausdehnen.
   **Ausgeschlossen – und das trifft die User Story:** der Satz „ich habe in einem langen Satz einen Buchstaben
   vertippt“ bleibt für Übersetzungsübungen möglich, denn die Wertung bleibt binär; `AnswerGrader.IsMisspelled`
   (`AnswerGrader.cs:47`) ist nur Anzeige und ändert nichts an der Wertung. Dass das Tippfehler-Problem dort bleibt, löst
   diese Story nicht; das ist die Sache von B-624 (Akzentfehler zählt halb), nicht dieser Regel. **Kosten:** die User
   Story ist für einen Typ nur teilweise erfüllt; Theodors Schulformat („deutsche Vorgabe ganz übersetzen“) braucht den
   Satz aber genau so.
3. **Die Anleitung ändert die Landeskunde-Zeile, statt Landeskunde zu verbieten.** `SKILL.md:169`–`:170` lautet künftig
   „… Wendungen als `translation`, feste Wendungen und Eigennamen **aus der Wortliste** als `matching`“. Landeskunde
   bleibt als Stoff (`topics`, Lesetext) erhalten, wird aber nicht abgefragt. Begründung: Der Lader prüft bei `matching`
   nur die Form (`laden.py:43`), und die Zeile im Skill ist die naheliegende Quelle der Beispiele. **Verworfen:** die
   Landeskunde ganz aus dem Skill streichen (verliert Kontext, den das Buch trägt). **Ausgeschlossen:** eine
   Landeskunde-Abfrage, die der Vater ausdrücklich will – dann legt er sie von Hand an (Entscheidung 1). **Kosten:**
   `lehrwerk-aufbereiten` liefert für künftige Bücher keine Landeskunde-Übungen mehr.
4. **Der Lader warnt als `Hinweis`, nicht als `FEHLER`.** `check(book, words, exercises)` (`laden.py:157`) kennt die
   Wortliste je Unit (`words[nr]`, Spalten `wort`/`uebersetzung`, `laden.py:193`–`:200`). Steht eine Seite eines
   `matching`-Paares weder als Wort noch als Übersetzung noch als eine Bedeutung der Übersetzung in der Liste der Unit
   (verglichen über `graded()`, `laden.py:106`), kommt ein Hinweis. Begründung: Die Regel bleibt sichtbar, ohne die
   legitime Ausnahme zu sperren (Eigennamen, Formen in Grammatik-Zuordnungen). **Verworfen:** `FEHLER` (Grammatik-
   Zuordnungen wie Infinitiv ↔ Form stehen nicht in der Wortliste). **Kosten:** Hinweis-Rauschen bei Grammatik-
   Zuordnungen; der Vater liest die Hinweise ohnehin (`laden.py:576`).
5. **Der Bestand wird nicht umgeschrieben, sondern read-only aufgelistet – als einmalige Handarbeit, nicht als Bau der
   Story.** Der Entwickler listet per lesendem API-Abruf (kein Schreibzugriff) die Zuordnungen und Übersetzungen, deren
   Antwort in keiner Wortliste der Unit steht, und legt die Liste dem Vater vor. Begründung: Umschreiben risse
   Item-Ids und Leitner-Stand an den Karten. **Verworfen:** automatisches Umschreiben. **Ausgeschlossen – gegen die
   User Story gerechnet:** Friedrich und Theodor bekommen die betroffenen Bestandsfragen weiter, bis der Vater die Liste
   abgearbeitet hat; die Story ist für **neu** angelegte Übungen erfüllt, für den Bestand erst nach der Entscheidung des
   Vaters. **Kosten:** eine Handarbeit ohne Test; sie wird im `## Verlauf` belegt, nicht durch ein Tor.
6. **B-604 und B-626 bleiben getrennt.** B-626 räumt Platzhalter („sth.“) aus der Antwort, B-604 begrenzt, *was gefragt
   wird*. Beide Verlaufszeilen verlinken einander. **Verworfen:** zusammenlegen (andere Ursache, andere Stelle, B-626 ist
   schon geschätzt). **Kosten:** zwei Stories am selben Satz „die Antwort ist die Vokabel“; wer eine ändert, liest die
   andere mit.

## Akzeptanzkriterien

Je Kriterium die Änderung, die es rot macht.

1. `SKILL.md` verlangt keine Landeskunde-Abfrage mehr als `matching` und nennt die Wortlisten-Regel. *Rot, wenn:* die
   Zeile „Landeskunde als `matching`“ wieder in `SKILL.md` steht – ein neuer Fall in
   `.claude/skills/lehrwerk-aufbereiten/test_laden.py` liest die Datei und prüft beides (der alte Text fehlt, „aus der
   Wortliste“ steht da).
2. `DraftPrompts.System` enthält die Regel „Sprachfrage statt Sachfrage“ und „die Antwort einer Vokabel- oder
   Lückentext-Aufgabe ist ein Wort oder eine feste Wendung der Wortliste“, mit dem Zusatz, dass Übersetzungsaufgaben
   Sätze verlangen dürfen. *Rot, wenn:* jemand die Regel aus `DraftPrompts` entfernt – ein Fall in `CreatorAgentTests`
   liest `chat.LastMessages[0].Text` (wie `CreatorAgentTests.cs:95`) und sucht beide Satzteile.
3. `laden.py --pruefen` meldet einen `Hinweis`, wenn eine Seite eines `matching`-Paares in der Wortliste der Unit fehlt,
   und schweigt, wenn alle Paare dort stehen. *Rot, wenn:* `test_laden.py` ein Paar „Orte“ ohne Wortlisten-Treffer
   prüft (`laden.check(book, words, [e])`) und keinen Hinweis findet, **oder** ein Paar aus der Liste einen Hinweis
   auslöst.
4. Die Übersetzungsübung des Agenten verlangt weiter Sätze als Antwort. *Rot, wenn:* `TranslationStrategy` einen
   mehrwortigen `target` als Verstoß meldet – **Absichtserklärung**, soweit kein vorhandener Test in
   `CreatorAgentTests` einen mehrwortigen `target` annimmt (beim Schätzen nachsehen; findet sich einer, entfällt die
   Beschriftung).
5. Der Vater-Editor zeigt die Hilfezeile „Sprachfrage statt Sachfrage“. *Rot, wenn:* `FIELD_HELP` den Eintrag nicht
   führt – ein Fall in `frontend/src/lib/fieldHelp.test.ts` prüft den Text (Vorbild dort: `FIELD_HELP.stage.text`),
   und ein Komponententest des Editors prüft, dass die Hilfe am Feld hängt.
6. Eine Liste der Bestandsübungen, deren Antwort in keiner Wortliste der Unit steht, liegt dem Vater vor (read-only).
   **Absichtserklärung:** kein Code, der rot werden könnte – der Beleg ist die Verlaufszeile mit der Liste.

## Verlauf

- **2026-11-16** — angelegt aus dem Chat mit dem Vater (Regeln für den Creator).
- **2026-11-19** — ausformuliert (Vorbereitungslauf, Sonnet): Quelle der Landeskunde-Zuordnung ist die Anleitung
  `SKILL.md:169`; der Agent erzeugt `Matching`/`Reading` nicht; zwei Regeln, nur eine maschinell prüfbar; Bestand in
  der echten DB nicht geprüft.
- **2026-11-19** — gegrillt (Vorbereitungslauf, Sonnet, stellvertretend, unbestätigt): Regel als Text an Skill,
  Agent-Prompt und Vater-Editor statt als Server-Prüfung; `Translation` behält Sätze; Lader warnt nur; Bestand nur
  gelistet. Verwandt: [B-626](B-626-antwort-verlangt-mehr-als-die-vokabel.md).
