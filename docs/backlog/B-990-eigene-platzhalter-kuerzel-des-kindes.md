---
tags: [typ/story, status/abgenommen, bereich/training, rolle/student, lerntechnik/vokabeln]
aliases: [smb statt sb, smt statt sth, eigene Kürzel beim Tippen]
status: abgenommen
prio: P2
art: Wunsch
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-25.md#runde-1--vorstellung--feedback
grund: ""
ersetzt_durch: []
nachgeschaut: 2026-11-25
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

## Schätzung

**Größe S, `wo: backend`, keine Migration, kein Vertragsbruch.** Eine Regel an einer Senke, keine Datenänderung, kein
Feld im Vertrag.

**Senke:** `AnswerGrader.Matches(given, expected, articleOptionalIn)` (`backend/Pugling.Api/Services/Shared/AnswerGrader.cs:29`).
Jeder Weg, der eine getippte Antwort wertet, landet dort: Üben (`PositionPracticeController.cs:662`), Lernkarte (`:893`),
Abschlusstest (`PositionTestsController.cs:306`), Satz-Wertung (`PositionPlayService.cs:677`), Vorschau
(`ExercisePreviewService.cs:94`), Mischrunde (über das Üben-Urteil), dazu die Ablenker-Filter der Typen
(`BuiltInExerciseTypes.cs:290/334/670`) und die beiden Fehlerkategorien `IsMisspelled`/`IsAccentSlip`, die zuerst
`Matches` fragen.

**Angriffsplan:**

1. Fälle zuerst: Unit-Fälle in `AnswerGraderTests` (AK 2, 3, 5, 6) und Pfad-Tests `KuerzelPfadTests` nach dem Muster von
   `SchraegstrichPfadTests` (Üben, Abschlusstest, Vorschau; AK 1). Vor der Regel rot.
2. Regel: `AnswerVariants.CanonicalSlots` bildet nach `Normalize` jedes Kind-Kürzel auf seinen Platzhalter ohne Punkt ab
   (smb/smbd/sbd/sb → sb, smth/smt/s.th/sth → sth, nur als ganzes Wort); `Matches` wendet sie auf **beide** Seiten an. Die
   Art passt damit von selbst (AK 2), und „hold on“ bleibt „hold on“ (AK 3).
3. `WordListPlaceholders` bleibt unverändert (AK 4); die Spiegel (Vater-Web, Lader, Fallliste) betrifft die Regel nicht, weil
   sie nicht in `Normalize` liegt.

**Risiken:** (1) Die Abbildung gilt auch für „sth“ ohne Punkt gegen „sth.“ im Satzinneren – das ist Entscheidung 2 („mit
oder ohne Punkt“), heute aber falsch; ein Fall hält es. (2) Die Ablenker-Filter sehen dieselbe Regel: ein Ablenker „turn to
smb“ fiele aus der Auswahl – gewollt, er wäre eine richtige Antwort. (3) Ein echtes Wort „smt“/„sbd“ gibt es in keiner der
Sprachen; eine falsche Abbildung in Französisch/Deutsch ist darum nicht zu erwarten (AK 6 hält qn/jdn).

**Testweg:** `AnswerGraderTests` (Unit), `KuerzelPfadTests` (Integration, drei Wege), volle Suite `-c Release`; Mutanten
mit `.claude/scripts/mutant-probe.py`; Rollengang an der Kopie mit Theodors echter Position 100/193 bzw. der
Vorschau von Übung 15.

## Verlauf

- **2026-11-25** — angelegt aus dem Rollengang der PM-Sitzung (Sohn-Sitz, echte Rückmeldungen 3/6/7). Produktfrage, darum
  `Wunsch` und im Dialog zu klären.
- **2026-11-25** — `idee` → `ausformuliert`: Platzhalter-Liste, Varianten und Vergleichsstelle am Code belegt; an der Kopie der
  echten DB vier Kürzel-Antworten gefunden (zwei von Theodor im Test, zwei vom Vater als Testjunge), alle falsch gewertet.
  Fünf offene Punkte mit Empfehlung für die Grill-Runde.
- **2026-11-25** — `ausformuliert` → `gegrillt`: Grill-Runde mit dem Vater, fünf Entscheidungen (erkennen statt Hinweis,
  sechs Kürzel, Art muss passen, überall gleich, nur Englisch); Akzeptanzkriterien final.
- **2026-11-25** — `gegrillt` → `geschaetzt`: S, Senke `AnswerGrader.Matches`, alle Aufrufer belegt, Testweg benannt.
- **2026-11-25** — `geschaetzt` → `in-arbeit` (Sprint 165): `AnswerVariants.CanonicalSlots` bildet die sechs Kürzel auf
  „sb“/„sth“ ab (ganzes Wort, Punkt optional); `AnswerGrader.Comparable` wendet sie mit `Normalize` auf beide Seiten an, in
  `Matches`, `IsMisspelled`, `IsAccentSlip` und den Lösungs-Schreibweisen. Vor der Regel 14 neue Fälle rot, alle
  „soll richtig sein“-Fälle; die „bleibt falsch“-Fälle grün (Absicht, im Test beschriftet).
- **2026-11-25** — Verifikation je AK-Fall:
  - AK 1, Üben/Test/Vorschau → `KuerzelPfadTests` (drei Theorien, je mit einer Zeile falscher Art), M1 → rot an allen drei.
  - AK 2, falsche Art → `AnswerGraderTests.Falsches_Oder_Fremdes_Kuerzel_Bleibt_Falsch` Zeilen 1–2, M3 → rot.
  - AK 3, „hold on smth.“ → dieselbe Theorie Zeile 3, Absichtserklärung (keine plausible Regel macht sie rot).
  - AK 4, `WordListPlaceholders` → unverändert (`git diff` leer an der Datei).
  - AK 5, je Kürzel → `Kuerzel_Des_Kindes_Zaehlt_Fuer_Seinen_Platzhalter`, M4–M7, M10 → rot; Punkt optional M8 → rot; „so“ M9 → rot.
  - AK 6, qn/jdm → `Andere_Sprachen_Bleiben_Wie_Heute`, Absichtserklärung.
  - Buch-Seite der Abbildung M2 → rot; Wortgrenzen M11 → rot („Gasmthaus“); „falsch geschrieben“ Kind-Seite M12, Lösungs-Seite M13 → rot.
- **2026-11-25** — Mutanten nach der letzten Änderung (`.claude/scripts/mutant-probe.py`, Debug), gemessen an `4fcb5618 +
  Arbeitsbaum`, 16:15:

  | Mutante | Stelle | erwartet | gemessen |
  | --- | --- | --- | --- |
  | M1 Kind-Seite ohne Abbildung | `AnswerGrader.cs:32` | red | red |
  | M2 Buch-Seite ohne Abbildung | `AnswerGrader.cs:34` | red | red |
  | M3 Art egal (alles sth) | `AnswerVariants.cs:163` | red | red |
  | M4 ohne smbd | `AnswerVariants.cs:166` | red | red |
  | M5 ohne sbd | `AnswerVariants.cs:166` | red | red |
  | M6 ohne s.th | `AnswerVariants.cs:166` | red | red |
  | M7 ohne smth | `AnswerVariants.cs:166` | red | red |
  | M8 Punkt nicht optional | `AnswerVariants.cs:166` | red | red |
  | M9 so zählt (Absicht) | `AnswerVariants.cs:166` | red | red |
  | M10 ohne blankes sb | `AnswerVariants.cs:166` | red | red |
  | M11 ohne Wortgrenzen | `AnswerVariants.cs:166` | red | red |
  | M12 falsch geschrieben ohne Abbildung | `AnswerGrader.cs:53` | red | red |
  | M13 Lösungsseite ohne Abbildung | `AnswerGrader.cs:106` | red | red |

  Grundlinie nach dem Zurückspielen grün. Suite .NET **2469/2469** (Release, Ausgangsstand 2440).
- **2026-11-25** — Review `pugling-reviewer`: kein 🔴; drei 🟡 übernommen (Fehlerkategorien maßen ohne die Abbildung →
  gemeinsamer Helfer `Comparable`; blankes „sb“ mitten in der Wendung ungetestet → „give sb. a hand“; Wortgrenzen
  ungetestet → „Gasmthaus“). 🟢 nicht übernommen: doppelte Ablenker „for smb“/„for sb“ (kosmetisch), Kommentar in
  `Lernkarte.tsx:34` (das Abschreiben-Signal vergleicht bewusst mit dem gedruckten Wort). Fehlerzähler Sprint 165: 0 (die
  🟡 sind Lücken in Tests bzw. Anzeige, kein falsches Urteil im Increment).
- **2026-11-25** — Rollengang an einer frischen Kopie (:5201, nach der letzten Änderung gestartet): Vater-Vorschau an
  Theodors Übung 15 (Freitext, Deutsch→Englisch) – „turn smt. up/down“, „turn smth up/down“, „turn to smb“, „turn to smbd.“
  richtig; „turn smb. up/down“, „turn to smth“, „turn to so“ falsch. Testjunge im Abschlusstest an Position 220: Theodors
  wörtliche Antworten „turn to smb“ und „turn smt. up/down“ richtig. Theodors eigener Test an Position 102 fragt die beiden
  Karten nicht ab (feste Auswahl 1–7, 16, 17), darum am Testjungen. Nebenbefund: eine Antwort falscher Art erscheint jetzt
  als „falsch geschrieben“ (sth/sb liegen zwei Zeichen auseinander) – nur Anzeige, als [B-994](B-994-falsche-platzhalter-art-als-falsch-geschrieben.md)
  abgelegt.
- **2026-11-25** — `in-arbeit` → `abgenommen`: Commit `fc3c37f4`; Suite 2469/2469, Mutanten 13/13, Review ohne 🔴, Rollengang an
  der Kopie (Sohn maschinell im Test des Testjungen, Vater in der Vorschau von Übung 15). Creator Regressionszeuge.
- **2026-11-25** — nachgeschaut (Nachtlauf N, Retro Sprint 181): `AnswerGrader.Comparable`/`AnswerVariants.CanonicalSlots` aus `fc3c37f4` gelesen – alle Vergleichsstellen des Graders gehen durch dieselbe Form, die Art-Trennung hält (Gruppe `thing`), Wortgrenzen beidseitig; kein Befund.
