---
tags: [typ/story, status/gegrillt, bereich/training, rolle/student, lerntechnik/vokabeln]
aliases: [smb statt sb, smt statt sth, eigene Kürzel beim Tippen]
status: gegrillt
prio: P2
art: Wunsch
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-25.md#runde-1--vorstellung--feedback
grund: ""
ersetzt_durch: []
---

# B-990 · Theodor tippt den Platzhalter mit – in seinem eigenen Kürzel („smb“, „smt“)

Beobachtet an Theodors offenen Rückmeldungen in der echten DB (Kopie vom 2026-11-25): zu „(to) turn sth. up/down“ tippte er
zweimal „turn smt. up/down“ (Rückmeldung 3 und 7, „zu viel verlangt“ bzw. „zu streng“), zu „(to) turn to sb.“ tippte er
„turn to smb“ (Rückmeldung 6, „zu streng“, 24.11. 19:59).

Seit [B-626](B-626-antwort-verlangt-mehr-als-die-vokabel.md) darf der Platzhalter fehlen („turn to“ ist richtig) oder so
stehen, wie die Wortliste ihn druckt („turn to sb.“). Theodor tut keins von beiden: er schreibt den Platzhalter mit, aber in
der Abkürzung, die er aus der Schule kennt („smb.“/„smth.“/„smt.“ für somebody/something). Das bleibt falsch – er wusste die
Vokabel und verliert die Karte am Kürzel.

## User Story

Als Lernender möchte ich, dass „turn to smb“ für „(to) turn to sb.“ zählt, damit ich eine Wendung, die ich kann, nicht am
Kürzel verliere, das ich aus der Schule anders kenne als das Buch.

## Ist-Stand am Code

- **Die Liste der Platzhalter** ist `WordListPlaceholders.Slots` (`backend/Pugling.Contracts/Common/WordListPlaceholders.cs:12`):
  `sth`, `sb`, `qn`, `qc`, `jdn`, `jdm`, `etw`, je mit oder ohne Punkt, auch als `qn/qc` (`:21`). Sie liegt im Vertrag,
  weil der KI-Creator dieselbe Liste braucht: er darf nie einen Platzhalter zum Teil einer Antwort machen
  (`backend/Pugling.Agent.Creator/Drafting/DraftRules.cs:141`).
- **Was zählt**, baut `AnswerVariants.Of` aus dem Eintrag (`backend/Pugling.Api/Services/Shared/AnswerVariants.cs:59`): den
  Eintrag selbst, jede Bedeutung mit und ohne Klammerteile, und seit B-626 jede Form ohne ihre Platzhalter
  (`WordListPlaceholders.Core`, `:70`). Für „(to) turn to sb.“ zählen also u. a. „turn to sb“ und „turn to“.
- **Verglichen wird** an der einen Senke `AnswerGrader.Matches` (`backend/Pugling.Api/Services/Shared/AnswerGrader.cs:18`)
  über `ContentItem.GradedAnswers()` (`ExerciseContentProvider.cs:72`) – für Üben, Abschlusstest, Vorschau und
  Mischrunde gleich („the rule cannot differ between practice and test“, `AnswerGrader.cs:15`). Die Varianten kommen nur
  aus dem **erwarteten** Eintrag; die getippte Antwort wird bloß normalisiert (`StageMechanics.Normalize`). Ein Kürzel
  des Kindes wird darum nie als Platzhalter erkannt.
- **Auf den Buchstaben-Kästchen** steht der Platzhalter fest vorgegeben (`AnswerVariants.AccessoryMask`, `:33`, genutzt in
  `StageMechanics.cs:108`). Das Problem gibt es also nur bei freier Eingabe.
- **Die Spiegel** des Vergleichs (Vater-Web `frontend/src/lib/answerNormalize.ts`, Lehrwerk-Lader `graded()` in
  `.claude/skills/lehrwerk-aufbereiten/laden.py:106`, Fallliste `docs/fallliste-antwortvergleich.json`) kennen nur
  `Normalize`, keine Platzhalter; eine Regel in `AnswerVariants` berührt sie nicht.
- **Echte Antworten** (Kopie der echten DB vom 2026-11-25, `ItemReviewEvents.GivenAnswer`), alle als falsch gewertet:

  | Kind | Quelle | getippt | Eintrag |
  | --- | --- | --- | --- |
  | Theodor | Test 24.11. | turn smt. up/down | (to) turn sth. up/down |
  | Theodor | Test 24.11. | turn to smb | (to) turn to sb. |
  | Testjunge (der Vater) | Üben 17.11. | (to) hold on smth. | (to) hold onto sth. |
  | Testjunge (der Vater) | Üben 21.11. | (to) dig (for smth.), dug, dug | (to) dig (for sth.), dug, dug |

  Theodors Fälle fielen im **Abschlusstest**, nicht im Üben. Im Store tragen 75 Wörter ein „sb“/„sth“.

## Die echte Lücke

Der Grader kennt Platzhalter nur in der Schreibweise des Buchs. Wer den Platzhalter mittippt, aber in einer anderen
gängigen Abkürzung (smb., smth., smt.), bekommt die Karte falsch, obwohl er die Wendung kann. Das trifft nicht nur
Theodor: der Vater selbst tippt „smth.“. Beim zweiten Fall des Testjungen („hold on“ statt „hold onto“) wäre die Antwort auch
mit Toleranz falsch – die Toleranz darf nur das Kürzel schlucken, nicht das Wort davor.

## Akzeptanzkriterien

1. „turn to smb“ zählt für „(to) turn to sb.“, „turn smt. up/down“ für „(to) turn sth. up/down“, im Üben, im Test, in der
   Mischrunde und in der Vorschau gleich (Entscheidung 4). *Rot, wenn* eine dieser Stellen das Kürzel anders wertet.
2. Ein Kürzel zählt nur dort, wo der Eintrag einen Platzhalter derselben Art hat: „turn to smth“ für „turn to sb.“ bleibt
   falsch (Entscheidung 3). *Rot, wenn* ein Sach-Kürzel einen Personen-Platzhalter ersetzt oder umgekehrt.
3. Ein Kürzel allein macht keine Antwort richtig: „hold on smth.“ für „(to) hold onto sth.“ bleibt falsch.
4. Der KI-Creator bleibt unberührt: die Kürzel des Kindes werden kein Platzhalter der Wortliste
   (`WordListPlaceholders` unverändert).
5. Es zählen genau smb, smbd, sbd für „sb.“ und smth, smt, s.th. für „sth.“, je mit oder ohne Punkt, ohne Rücksicht auf
   Groß-/Kleinschreibung (Entscheidung 2); „so“ und „s.o.“ nicht. *Rot, wenn* eine der sechs fehlt oder „so“ zählt.
6. Französische und deutsche Platzhalter (qn/qc, jdn./jdm./etw.) bleiben wie heute (Entscheidung 5).

## Offene Punkte

1. ~~**Welche Kürzel zählen?** Belegt sind smb, smt, smth. Kandidaten dazu: smbd, sbd, s.o., s.th., sth/sb ohne Punkt gibt es
   schon. *Empfehlung:* nur die belegten plus ihre naheliegenden Geschwister (smb, smbd, sbd für Personen; smth, smt, s.th.
   für Sachen). Nicht „so“/„s.o.“ – „so“ ist ein Wort.~~ → Entscheidung 2
2. ~~**Muss die Art passen** (Person-Kürzel nur für „sb.“, Sach-Kürzel nur für „sth.“)? *Empfehlung:* ja. Sonst zählt
   „turn to smth“, und das ist eine andere Wendung.~~ → Entscheidung 3
3. ~~**Wo gilt die Toleranz – Üben und Test gleich?** Theodors Fälle fielen im Test. Der Grader hat heute eine Regel für alle
   Wege, anders als beim Akzentfehler (B-624), wo nur das Üben milde ist. *Empfehlung:* überall gleich. Ein Kürzel ist
   keine Wissenslücke, und die Schule wertet „smb“ nicht als Fehler.~~ → Entscheidung 4
4. ~~**Toleranz oder Hinweis?** Statt die Kürzel zu erkennen, könnte die Karte sagen „den Platzhalter darfst du weglassen“.
   *Empfehlung:* Toleranz. Der Hinweis ändert die Gewohnheit des Kindes nicht, und auch der Vater tippt „smth.“.~~ → Entscheidung 1
5. ~~**Gilt das auch für Französisch/Latein/Deutsch** (qn/qc, jdn./jdm./etw.)? *Empfehlung:* nur Englisch, solange kein Fall
   belegt ist. Die Liste soll das Beobachtete abbilden, nicht raten.~~ → Entscheidung 5

## Entscheidungen

1. **Der Grader erkennt die Kürzel des Kindes; ein bloßer Hinweis auf der Karte reicht nicht.** (Vater, 2026-11-25, auf
   Punkt 4.) Begründung: Ein Hinweis ändert die Schulgewohnheit nicht, auch der Vater tippt „smth.“, und die Schule wertet
   „smb“ nicht als Fehler. Kosten: Die App nimmt mehr Schreibweisen an, als das Buch druckt. Es braucht eine zweite Liste
   nur für die Antwortseite (die Liste der Wortliste und der KI-Creator bleiben unberührt), und jede Schreibweise braucht
   einen Fall, der zeigt, dass sie nicht mehr durchlässt als das Kürzel (AK 3).
2. **Es zählen die belegten Kürzel und ihre naheliegenden Geschwister:** für „sb.“ smb, smbd, sbd; für „sth.“ smth, smt,
   s.th. – je mit oder ohne Punkt, Groß-/Kleinschreibung egal. „so“ und „s.o.“ zählen nicht. (Vater, 2026-11-25, auf
   Punkt 1.) Begründung: Belegt sind smb, smt, smth (Theodor und der Vater); nur auf Belege zu warten hieße, dass jede neue
   Schreibweise erst ein Kind eine Karte kostet. „so“ ist ein englisches Wort und ließe andere Wendungen durch. Kosten: sechs
   Schreibweisen in der neuen Liste, je ein Testfall; eine, die nie jemand tippt, ist Ballast.
3. **Die Art muss passen:** ein Personen-Kürzel zählt nur für „sb.“, ein Sach-Kürzel nur für „sth.“ – „turn to smth“ für
   „(to) turn to sb.“ bleibt falsch (AK 2 gilt). (Vater, 2026-11-25, auf Punkt 2.) Begründung: Person und Sache machen
   verschiedene Wendungen; wer sie verwechselt, kennt die Vokabel nicht ganz. In allen belegten Antworten passt die Art.
   Kosten: Jedes Kürzel braucht die Zuordnung zu seinem Platzhalter statt einer flachen Liste, und ein Kind, das aus
   Gewohnheit überall „smth“ tippt, verliert die Karte bei einer Person weiter.
4. **Die Toleranz gilt überall gleich – Üben, Abschlusstest, Vorschau, Mischrunde.** (Vater, 2026-11-25, auf Punkt 3,
   „nimm deine Empfehlung“.) Begründung: Theodors Fälle fielen im Test; die Schule wertet ein anderes Kürzel nicht als
   Fehler, anders als einen Akzent (B-624, dort nur das Üben milde); und `AnswerGrader.Matches` ist heute die eine
   Vergleichsstelle aller Wege, die Regel braucht keinen Sonderzweig. Kosten: Der Test nimmt mehr Schreibweisen an als das
   Buch; bei einer Lehrerin, die auf „sb.“ besteht, ist er milder als die Klassenarbeit.
5. **Vorerst nur Englisch.** (Vater, 2026-11-25, auf Punkt 5, „nimm deine Empfehlung“.) Begründung: Belegt sind nur
   englische Kürzel; für qn/qc und jdn./jdm./etw. ist in den echten Antworten kein eigenes Kürzel aufgetaucht, und geratene
   Schreibweisen („qqn“, „jmdn.“) nähmen an, was niemand tippt. Ein späterer Fall ist eine Zeile und ein Testfall. Kosten:
   Tippt Friedrich „qqn“, verliert er die Karte, bis es gemeldet wird.

## Verlauf

- **2026-11-25** — angelegt aus dem Rollengang der PM-Sitzung (Sohn-Sitz, echte Rückmeldungen 3/6/7). Produktfrage, darum
  `Wunsch` und im Dialog zu klären.
- **2026-11-25** — `idee` → `ausformuliert`: Platzhalter-Liste, Varianten und Vergleichsstelle am Code belegt; an der Kopie der
  echten DB vier Kürzel-Antworten gefunden (zwei von Theodor im Test, zwei vom Vater als Testjunge), alle falsch gewertet.
  Fünf offene Punkte mit Empfehlung für die Grill-Runde.
- **2026-11-25** — `ausformuliert` → `gegrillt`: Grill-Runde mit dem Vater, fünf Entscheidungen (erkennen statt Hinweis,
  sechs Kürzel, Art muss passen, überall gleich, nur Englisch); Akzeptanzkriterien final.
