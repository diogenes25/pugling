---
tags: [typ/story, status/ausformuliert, bereich/katalog, bereich/training, rolle/creator, rolle/student, lerntechnik/vokabeln]
aliases: [Antwort zu umfangreich, umliegende Wörter abgefragt]
status: ausformuliert
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Chat mit dem Vater, 2026-11-17 (Erfahrung aus der Testphase)"
grund: ""
ersetzt_durch: []
---

# B-626 · Eine Vokabel verlangt nur das Wort, nicht die Platzhalter und Ergänzungen der Wortliste

Bei manchen Vokabeltests ist die erwartete Antwort zu umfangreich: abgefragt wird nicht nur die eigentliche
Vokabel, sondern auch die umliegenden Wörter. Jedes zusätzliche Wort ist eine weitere Chance auf einen
Fehler, der dann als ganzer Fehler zählt – ohne dass das Kind die Vokabel nicht gewusst hätte.

## User Story

Als **Lernender** möchte ich bei einer Vokabel nur das Wort bzw. die Wendung tippen müssen, nicht die
Platzhalter und Ergänzungen, mit denen die Wortliste sie druckt („sth.“, „qc“, „(to)“, „...“), damit ein
Fehler nur dann zählt, wenn ich das Wort nicht weiß.

## Ist-Stand am Code

Recherchiert am 2026-11-17 gegen `4ec275e7`.

- **Der Speicher hält den Eintrag, wie das Buch ihn druckt** ([B-462](B-462-mehrdeutige-uebersetzung-verlangt-alle-bedeutungen.md)/1,
  `backend/Pugling.Api/Services/Shared/AnswerVariants.cs:5`–`:9`), und getippt wird seit
  [B-597](B-597-tippstufen-fragen-die-zielsprache-ab.md) die Fremdsprache – also genau diese Seite.
- **Gemessen** an einer Kopie der Sicherung `pugling.db.bak-auto-20261117-124225` (nur lesend, danach gelöscht):
  Von 265 Vokabeln in Übungen hat die getippte Seite bei **50** drei oder mehr Wörter und bei **53** einen
  Platzhalter, eine Auslassung oder einen Schrägstrich. Beispiele: „(to) point sth. out (to sb.)“,
  „(to) be home to sth.“, „marcher sur qc“, „plaire à qn“, „imaginer qn/qc“, „au bord de qc“,
  „towards the station/Mr Bell“, „(to) turn left/right“, „It was a pity that ...“, „the best thing about ...“.
- **Die Bewertung verzeiht nur einen Teil des Beiwerks.** Klammerteile dürfen fehlen
  (`AnswerVariants.cs:91`–`:108`, `OptionalParts`), „...“ fällt weg (`:37`). **Platzhalter nicht:** „sth.“,
  „sb.“, „qn“, „qc“ kennt der Code nur als Abkürzung, nach der ein Punkt kein Satzende ist (`:19`–`:22`), nicht
  als verzichtbar. „marcher sur“ ist für „marcher sur qc“ falsch; „point out“ für „(to) point sth. out (to sb.)“
  ebenso. Ein Schrägstrich **ohne** Leerzeichen („left/right“, „qn/qc“) trennt nichts (`:68`–`:69`).
- **Die Buchstabenkästchen verlangen alles.** Die Maske entsteht aus dem ganzen Eintrag; Klammern, Punkte und
  Schrägstriche stehen fest, aber jeder Buchstabe darin ist ein Kästchen
  (`backend/Pugling.Api/Services/Shared/StageMechanics.cs:96`–`:97`). Bei „(to) point sth. out (to sb.)“ tippt
  das Kind „to“, „sth“, „to“ und „sb“ mit – das ist das „Drumherum“, das der Vater meint.
- **Lückentexte sind nicht betroffen:** 141 Lücken in 21 Lückentexten, keine mit drei oder mehr Wörtern
  (gleiche Messung).
- **Abgrenzung:** [B-604](B-604-sprachuebungen-fragen-nur-sprache.md) betrifft Sachfragen und ganze Sätze als
  Antwort (Übersetzungs- und Grammatik-Übungen), B-626 die Einträge der Wortliste.
  [B-625](B-625-ein-eingabefeld-je-bedeutung.md)/6 nimmt Kürzel-Teile schon von der Zerlegung aus
  („connaître qn / qc“ bleibt ein Feld) – dieselbe Kürzel-Liste.

## Die echte Lücke

Es fehlt die Unterscheidung zwischen **Kern** (was gelernt wird) und **Beiwerk** (Platzhalter, Infinitiv-„to“,
Ergänzungen in Klammern, Auslassungspunkte) – im Vergleich nur halb vorhanden (Klammern, „...“), in der
Kästchen-Maske gar nicht. Der KI-Creator kennt die Regel ebenfalls nicht.

## Offene Punkte

1. **Was ist Beiwerk?** Empfehlung: **Platzhalter** (sth., sb., qn, qc, jdn., jdm., etw.), **Klammerteile**
   (schon heute) und **Auslassungspunkte** (schon heute). Alles darf fehlen **oder** stehen. Kosten: eine
   Platzhalter-Liste im Code, dieselbe wie die Kürzel aus B-625/6.
2. **Wie zeigen die Kästchen das Beiwerk?** Empfehlung: **als festen Text** in der Reihe, wie heute Leerzeichen
   und Satzzeichen – „____ sth. ___“ für „point sth. out“; Klammerteile fallen aus der Reihe weg. Das Kind sieht
   so die Stellung des Objekts, tippt aber nur den Kern. Kosten: Die Maske braucht ein zweites festes Zeichen-
   Kriterium neben „kein Buchstabe“.
3. **Schrägstrich ohne Leerzeichen** („left/right“, „qn/qc“, „the station/Mr Bell“). Empfehlung: **jede Seite
   gilt**, wie die Wahl in einer Klammer („stay (at/with)“); in den Kästchen steht die erste. Kosten: „turn left“
   und „turn right“ zählen je allein – dass beides gelernt wird, sichert nur B-625, und das trennt an „/“ ohne
   Leerzeichen bewusst nicht.
4. **Wo gilt es?** Empfehlung: **überall, wo getippt wird** (Üben, Test, Bonus, Vorschau), die Regel von B-597/5.
   Kosten: keine weiteren.
5. **Code oder Datenpflege?** Empfehlung: **Code**, wie B-462/1 – die 53 Einträge bleiben wie gedruckt. Kosten:
   Heuristik statt Daten.
6. **KI-Creator.** Empfehlung: **ja, eine Regel**: Antworten neuer Übungen sind der Kern eines
   Wortlisten-Eintrags; in Lückentexten wird nur der Kern ausgespart. Kosten: eine Regel in
   `backend/Pugling.Agent.Creator/Drafting/DraftRules.cs`/`DraftPrompts.cs`; B-604 will dort Ähnliches.
7. **Reihenfolge zu B-625.** Empfehlung: **B-626 vor B-625** – B-625 zerlegt in Teile und braucht je Teil den
   Kern; die Kürzel-Liste entsteht hier und wird dort wiederverwendet. Kosten: B-625 wartet auf B-624 **und**
   B-626.

## Akzeptanzkriterien (Entwurf)

1. „marcher sur“ und „marcher sur qc“ sind beide richtig für „marcher sur qc“; „point out“, „point sth. out“
   und „to point sth. out to sb.“ für „(to) point sth. out (to sb.)“.
2. Die Kästchen zu „(to) point sth. out (to sb.)“ zeigen „point“ und „out“ als Kästchen, „sth.“ als festen Text,
   keine Klammerteile.
3. „turn left“ und „turn right“ sind richtig für „(to) turn left/right“.
4. Ein Eintrag ohne Beiwerk verhält sich wie heute.
5. Üben, Test, Bonus und Vorschau werten gleich.
6. Der KI-Creator erzeugt keine Antwort mit Platzhaltern (Briefing-/Prompt-Test).

## Verlauf

- **2026-11-17** — angelegt aus dem Chat mit dem Vater.
- **2026-11-17** — ausformuliert: Ursache sind Platzhalter (sth., qc …), die der Vergleich nicht verzeiht und
  die Kästchen-Maske mitzählt; an der DB-Kopie 53 von 265 Einträgen betroffen, Lücken nicht; sieben offene
  Punkte mit Empfehlung.
