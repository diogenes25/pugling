---
tags: [typ/story, status/ausformuliert, bereich/training, rolle/student, lerntechnik/vokabeln]
aliases: [smb statt sb, smt statt sth, eigene Kürzel beim Tippen]
status: ausformuliert
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

## Akzeptanzkriterien (Entwurf)

1. „turn to smb“ zählt für „(to) turn to sb.“, „turn smt. up/down“ für „(to) turn sth. up/down“, im Üben, im Test und in
   der Vorschau gleich. *Rot, wenn* eine der drei Stellen das Kürzel anders wertet.
2. Ein Kürzel zählt nur dort, wo der Eintrag einen Platzhalter derselben Art hat: „turn to smth“ für „turn to sb.“ bleibt
   falsch (wenn Punkt 2 so entschieden wird). *Rot, wenn* ein Sach-Kürzel einen Personen-Platzhalter ersetzt.
3. Ein Kürzel allein macht keine Antwort richtig: „hold on smth.“ für „(to) hold onto sth.“ bleibt falsch.
4. Der KI-Creator bleibt unberührt: die Kürzel des Kindes werden kein Platzhalter der Wortliste.

## Offene Punkte

1. **Welche Kürzel zählen?** Belegt sind smb, smt, smth. Kandidaten dazu: smbd, sbd, s.o., s.th., sth/sb ohne Punkt gibt es
   schon. *Empfehlung:* nur die belegten plus ihre naheliegenden Geschwister (smb, smbd, sbd für Personen; smth, smt, s.th.
   für Sachen). Nicht „so“/„s.o.“ – „so“ ist ein Wort.
2. **Muss die Art passen** (Person-Kürzel nur für „sb.“, Sach-Kürzel nur für „sth.“)? *Empfehlung:* ja. Sonst zählt
   „turn to smth“, und das ist eine andere Wendung.
3. **Wo gilt die Toleranz – Üben und Test gleich?** Theodors Fälle fielen im Test. Der Grader hat heute eine Regel für alle
   Wege, anders als beim Akzentfehler (B-624), wo nur das Üben milde ist. *Empfehlung:* überall gleich. Ein Kürzel ist
   keine Wissenslücke, und die Schule wertet „smb“ nicht als Fehler.
4. **Toleranz oder Hinweis?** Statt die Kürzel zu erkennen, könnte die Karte sagen „den Platzhalter darfst du weglassen“.
   *Empfehlung:* Toleranz. Der Hinweis ändert die Gewohnheit des Kindes nicht, und auch der Vater tippt „smth.“.
5. **Gilt das auch für Französisch/Latein/Deutsch** (qn/qc, jdn./jdm./etw.)? *Empfehlung:* nur Englisch, solange kein Fall
   belegt ist. Die Liste soll das Beobachtete abbilden, nicht raten.

## Verlauf

- **2026-11-25** — angelegt aus dem Rollengang der PM-Sitzung (Sohn-Sitz, echte Rückmeldungen 3/6/7). Produktfrage, darum
  `Wunsch` und im Dialog zu klären.
- **2026-11-25** — `idee` → `ausformuliert`: Platzhalter-Liste, Varianten und Vergleichsstelle am Code belegt; an der Kopie der
  echten DB vier Kürzel-Antworten gefunden (zwei von Theodor im Test, zwei vom Vater als Testjunge), alle falsch gewertet.
  Fünf offene Punkte mit Empfehlung für die Grill-Runde.
