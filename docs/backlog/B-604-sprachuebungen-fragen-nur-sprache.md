---
tags: [typ/story, status/ausformuliert, bereich/katalog, rolle/creator, lerntechnik/vokabeln]
aliases: [Keine Sachfragen, Nur Sprache abfragen, Keine Satzantworten]
status: ausformuliert
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Vater im Chat 2026-11-16: Fragen nach der Farbe der U-Bahn in England oder nach Städten in Frankreich, Antworten teils ganze Sätze – beim Vokabellernen irrelevant"
grund: ""
ersetzt_durch: []
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

Alle ohne Entscheidung; Empfehlung steht dabei.

1. **Wo gilt die Regel: als Regeltext, als Server-Prüfung, oder beides?** Empfehlung: **nur als Regeltext** (Skill
   und Agenten-Prompt). Eine Server-Prüfung der rechten Spalte bräche legitime Zuordnungen (Bundesland → Hauptstadt,
   Seed `:1375`) und kann „Wendung“ nicht von „Satz“ unterscheiden.
2. **Gilt „kein Satz als Antwort“ auch für `Translation`?** Empfehlung: **nein.** Der Typ übersetzt Sätze; die Regel
   heißt dort „Sprachfrage statt Sachfrage“, also ein Ausgangssatz, der ein Wort der Unit enthält (das verlangt
   `DraftRules.CoversRequiredWords` bereits). Die Regel „Antwort = Wortlisten-Eintrag“ gilt für `Vocabulary`,
   `Matching` und Lückentext-Lücken.
3. **Was wird aus der Landeskunde-Zuordnung?** Empfehlung: die Zeile in `SKILL.md:169`–`:170` ersetzen („Landeskunde als
   `matching`“ → „feste Wendungen und Eigennamen **aus der Wortliste** als `matching`“) statt die Landeskunde ganz zu
   verbieten – sie bleibt als Lesetext/Hintergrund im Stoff (`topics`), nicht als Abfrage.
4. **Bestand.** Empfehlung: nicht in dieser Story ändern, sondern **read-only auflisten** (Zuordnungen und Übersetzungen,
   deren Antwort in keiner Wortliste der Unit steht) und den Vater entscheiden lassen. Automatisches Umschreiben risse
   Item-Ids und Leitner-Stand an den Karten.
5. **Soll der Lader warnen?** Empfehlung: ja, als `Hinweis` (nicht `FEHLER`), wenn eine `matching`-Seite eines Paares in
   keiner Wortliste der Unit steht – die Regel bliebe sichtbar, ohne eine legitime Ausnahme zu blockieren.
6. **Abgrenzung zu B-626:** B-626 räumt Platzhalter aus der Antwort, B-604 begrenzt, *was gefragt wird*. Empfehlung: getrennt
   lassen, in beiden Verlaufszeilen verlinken.

## Akzeptanzkriterien (Entwurf)

Je Kriterium die Änderung, die es rot macht.

1. Der Skill `lehrwerk-aufbereiten` verlangt keine Landeskunde-Abfrage mehr als `matching` und nennt die neue Regel.
   *Rot, wenn:* die Zeile `SKILL.md:169`–`:170` unverändert bleibt (Prüfung als Textvergleich in einem
   Skill-Test; gibt es dafür kein Tor, ist das AK ausdrücklich eine **Absichtserklärung**).
2. Der Regelblock des KI-Creators nennt „Sprachfrage statt Sachfrage“ und „Antwort ist ein Wort oder eine Wendung der
   Wortliste“ für `Vocabulary`/Lückentext. *Rot, wenn:* ein Test in `CreatorAgentTests` (mit `FakeChatClient`) den
   Prompt liest und der Satz fehlt, weil jemand den Block aus `DraftPrompts` entfernt.
3. `laden.py --pruefen` meldet einen `Hinweis`, wenn eine Seite eines `matching`-Paares in keiner Wortliste der Unit
   steht, und schweigt, wenn alle Paare in der Liste stehen. *Rot, wenn:* `test_laden.py` ein Paar „Orte“ ohne
   Wortlisten-Treffer lädt und keinen Hinweis sieht.
4. Die Übersetzungsübung des Agenten erzeugt weiter Sätze als Antwort. *Rot, wenn:* jemand die Regel auf `Translation`
   ausdehnt und `TranslationStrategy` Satz-Antworten ablehnt – **Absichtserklärung** (der Ist-Stand ist das Soll).
5. Eine Liste der Bestandsübungen, deren Antwort in keiner Wortliste der Unit steht, liegt dem Vater vor
   (read-only). *Rot, wenn:* die Auswertung eine bekannte Landeskunde-Zuordnung (Test-DB) nicht nennt.

## Verlauf

- **2026-11-16** — angelegt aus dem Chat mit dem Vater (Regeln für den Creator).
- **2026-11-19** — ausformuliert (Vorbereitungslauf, Sonnet): Quelle der Landeskunde-Zuordnung ist die Anleitung
  `SKILL.md:169`; der Agent erzeugt `Matching`/`Reading` nicht; zwei Regeln, nur eine maschinell prüfbar; Bestand in
  der echten DB nicht geprüft.
