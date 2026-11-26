---
tags: [typ/story, status/abgenommen, bereich/katalog, bereich/training, rolle/creator, rolle/student, lerntechnik/vokabeln]
aliases: [Antwort zu umfangreich, umliegende Wörter abgefragt]
status: abgenommen
prio: P2
art: Wunsch
groesse: M
wo: backend
migration: nein
vertragsbruch: nein
quelle: "Chat mit dem Vater, 2026-11-17 (Erfahrung aus der Testphase)"
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-11-25"
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

Alle in der Grill-Runde vom 2026-11-17 entschieden – siehe Entscheidungen.

1. ~~**Was ist Beiwerk?**~~ → Entscheidung 1 (nur Platzhalter neu).
2. ~~**Wie zeigen die Kästchen das Beiwerk?**~~ → Entscheidung 2 (abweichend vom Entwurf: auch Klammerteile als
   fester Text).
3. ~~**Schrägstrich ohne Leerzeichen.**~~ → Entscheidung 3 (abweichend vom Entwurf: beide Seiten verlangt).
4. ~~**Wo gilt es?**~~ → Entscheidung 4.
5. ~~**Code oder Datenpflege?**~~ → Entscheidung 5.
6. ~~**KI-Creator.**~~ → Entscheidung 6.
7. ~~**Reihenfolge zu B-625.**~~ → Entscheidung 7.

## Entscheidungen

Grill-Runde mit dem Vater, 2026-11-17. Alle nach Empfehlung.

1. **Beiwerk sind die Platzhalter** sth., sb., qn, qc, jdn., jdm., etw. (mit oder ohne Punkt); sie dürfen fehlen
   **oder** stehen. Klammerteile und „...“ dürfen schon heute fehlen (`AnswerVariants.cs:37`, `:91`–`:108`).
   Ein „to“ ohne Klammern und Beispiel-Ergänzungen sind **kein** Beiwerk. *Kosten:* eine Platzhalter-Liste im
   Code (dieselbe wie die Kürzel aus B-625/6), die mit jedem Lehrwerk wachsen kann.
2. **Die Kästchen zeigen den ganzen Eintrag; getippt wird nur der Kern.** Platzhalter und Klammerteile stehen als
   fester Text in der Reihe: „(to) \_\_\_\_\_ sth. \_\_\_ (to sb.)“. Begründung: Das Kind sieht den Eintrag wie im
   Buch, „(to sb.)“ zeigt den Gebrauch. *Kosten:* längere Reihe; die Oberfläche muss fest und tippbar deutlich
   unterscheiden (Frontend-Review).
3. **Ein Schrägstrich ohne Leerzeichen trennt zwei Formen, die beide verlangt werden** – je ein Feld nach
   [B-625](B-625-ein-eingabefeld-je-bedeutung.md) („turn left“, „turn right“). Er **verbindet je ein Wort** links
   und rechts („left/right“, „up/down“); in Klammern bleibt er eine Wahl („stay (at/with)“). Begründung: Wer nur
   „turn up“ kann, kann die Wendung nicht. *Kosten:* Ein Eintrag, dessen Seiten mehr als ein Wort haben
   („towards the station/Mr Bell“), lässt sich so nicht zerlegen – siehe Entscheidung 5; gebaut wird die Trennung
   in B-625.
4. **Überall, wo getippt wird:** Üben, Abschlusstest, Bonus-Training, Vorschau (Regel von B-597/5).
   *Kosten:* keine weiteren.
5. **Gelöst im Code; die Einträge bleiben wie gedruckt.** **Ausnahme:** „towards the station/Mr Bell“ wird im
   Vokabelspeicher von Hand berichtigt (über die API, nicht per Skript); beim Bauen wird geprüft, ob weitere
   Einträge einen Schrägstrich zwischen mehrwortigen Seiten tragen. *Kosten:* eine Datenänderung an der echten
   DB, Heuristik statt Daten für den Rest.
6. **Der KI-Creator bekommt eine Regel:** Neue Übungen fragen nur den Kern eines Wortlisten-Eintrags ab, nie
   Platzhalter; in Lückentexten wird nur der Kern ausgespart. *Kosten:* eine Regel in
   `backend/Pugling.Agent.Creator/Drafting/DraftRules.cs`/`DraftPrompts.cs`, abzustimmen mit
   [B-604](B-604-sprachuebungen-fragen-nur-sprache.md).
7. **Reihenfolge B-624 → B-626 → B-625.** B-626 liefert Kern und Platzhalter-Liste, B-625 die Zerlegung in Felder
   (auch am Schrägstrich ohne Leerzeichen) und nutzt den Ausgang *halb* aus B-624. *Kosten:* B-625 wartet auf zwei
   Stories.

## Akzeptanzkriterien

1. „marcher sur“ und „marcher sur qc“ sind beide richtig für „marcher sur qc“; „point out“, „point sth. out“ und
   „to point sth. out to sb.“ für „(to) point sth. out (to sb.)“.
2. Die Kästchen zu „(to) point sth. out (to sb.)“ zeigen „point“ und „out“ als Kästchen, „(to)“, „sth.“ und
   „(to sb.)“ als festen Text.
3. „(to) be home to sth.“ verlangt „be home to“ – das „to“ ohne Klammern bleibt Pflicht.
4. Ein Eintrag ohne Beiwerk verhält sich wie heute.
5. Üben, Abschlusstest, Bonus-Training und Vorschau werten gleich (Integrationstest je Pfad).
6. Der KI-Creator erzeugt keine Antwort mit Platzhaltern und spart in Lückentexten nur den Kern aus
   (Prompt-Test).
7. „towards the station/Mr Bell“ ist im Vokabelspeicher berichtigt; die Liste weiterer Einträge mit
   mehrwortigen Schrägstrich-Seiten ist geprüft und im Verlauf vermerkt.

Die Zerlegung „turn left/right“ → zwei Felder ist ein Akzeptanzkriterium von B-625, nicht von hier.

## Schätzung

**Größe: M** — wie der Batch-Pfad im `MediaSelector`: zwei kleine Eingriffe an geteilten Stellen (Vergleich und
Maske), eine Regel im Agenten, eine Datenkorrektur. Kein Schema, kein neues Feld.

- **`migration: nein`** — die Berichtigung von „towards the station/Mr Bell“ (Entscheidung 5) ist eine
  Datenänderung über die API (`creator/…/vocabulary`-Speicher), keine Schemaänderung.
- **`vertragsbruch: nein`** — die Maske bleibt ein String im bisherigen Format (`_` = tippen, alles andere fest,
  `backend/Pugling.Contracts/Student/PracticeDtos.cs:67`); sie markiert nur mehr Zeichen als fest.
- **`wo: backend`** — die Sohn-App rendert feste Maskenzeichen schon als eigene, nicht fokussierbare Kästchen
  (`frontend/src/components/LetterBoxes.tsx:77`–`:81`, Klasse `lbox-fixed`) und reicht sie im Wert mit
  (`:36`–`:41`). Ob „(to)“ und „sth.“ dort deutlich genug als fest erkennbar sind (Entscheidung 2), klärt der
  Rollengang; braucht es mehr als CSS, wird das eine eigene Frontend-Story.

### Risiken

1. **Maske und Vergleich müssen dasselbe Beiwerk kennen.** Die Maske setzt Platzhalter und Klammerteile fest
   (`backend/Pugling.Api/Services/Shared/StageMechanics.cs:96`–`:97`), der Vergleich lässt sie weg oder zu
   (`backend/Pugling.Api/Services/Shared/AnswerVariants.cs`). Eine gemeinsame Funktion „Beiwerk-Spannen eines
   Eintrags“ für beide, sonst zeigt die Karte ein festes „sth.“, das der Vergleich verlangt.
2. **Der Kästchen-Wert enthält das Beiwerk.** Die Sohn-App schickt die festen Zeichen mit
   („(to) point sth. out (to sb.)“) – das ist der ganze Eintrag und damit schon heute richtig. Kein Pfad darf das
   feste Beiwerk vor dem Vergleich abschneiden.
3. **Platzhalter mitten im Wort.** „sth.“ steht auch als Teil echter Wörter? Nein – verglichen wird je Wort
   (Wortgrenzen), sonst würde aus „something“ das „so“ (B-462-Liste enthält „so“) herausgeschnitten. Die
   Platzhalter-Liste ist **nicht** die Abkürzungsliste von B-462 (`AnswerVariants.cs:19`–`:22`, mit „so“, „usw“,
   „etc“) – zwei Listen mit verschiedenem Zweck, deutlich benannt.
4. **Bildschirmleser.** Feste Kästchen tragen ein `aria-label` je Zeichen (`LetterBoxes.tsx:81`); „s“, „t“, „h“
   einzeln vorgelesen ist ungeschickt. Im Rollengang mit prüfen, nicht hier bauen.
5. **Echte Daten.** Die Korrektur trifft die laufende DB der Söhne; vorher an einer Kopie prüfen, welche
   Einträge mehrwortige Schrägstrich-Seiten haben (AK7).

### Angriffsplan

1. Platzhalter-Liste und „Beiwerk-Spannen“ neben `AnswerVariants`, mit Unit-Tests über die 53 gemessenen
   Einträge.
2. Vergleich: Platzhalter dürfen fehlen (Varianten ergänzen).
3. Maske: Beiwerk-Spannen fest (`StageMechanics.LetterBoxPattern`, Aufrufer
   `backend/Pugling.Api/Exercises/VocabularyExerciseType.cs:156`, `backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs:204`).
4. KI-Creator: Regel in `DraftRules`/`DraftPrompts`, abgestimmt mit B-604.
5. Datenkorrektur über die API nach der Prüfung an der Kopie.

### Testweg

- Unit-Tests (`Pugling.Api.Tests`): Varianten und Maske je Beispiel aus den AKs; „something“ bleibt unberührt.
- Integrationstests je Pfad (Üben, Test, Bonus, Vorschau-Check) für AK1/AK3/AK5.
- Prompt-Test des Agenten für AK6.
- Rollengang: Sohn spielt „(to) point sth. out (to sb.)“ auf der Kästchenstufe (E2E, etwa Erweiterung von
  `frontend/e2e/artikel-pflicht.spec.ts`) – prüft auch die Erkennbarkeit des festen Texts.
- Reviewer: `pugling-reviewer`; `frontend-reviewer` nur, falls CSS nötig wird.

## Verlauf

- **2026-11-17** — angelegt aus dem Chat mit dem Vater.
- **2026-11-17** — ausformuliert: Ursache sind Platzhalter (sth., qc …), die der Vergleich nicht verzeiht und
  die Kästchen-Maske mitzählt; an der DB-Kopie 53 von 265 Einträgen betroffen, Lücken nicht; sieben offene
  Punkte mit Empfehlung.
- **2026-11-17** — gegrillt mit dem Vater: sieben Entscheidungen, alle nach Empfehlung; Schrägstrich ohne
  Leerzeichen trennt zwei verlangte Formen (gebaut in B-625), Reihenfolge B-624 → B-626 → B-625.
- **2026-11-17** — geschätzt: M, backend, keine Migration, kein Vertragsbruch; die Sohn-App zeigt feste
  Maskenzeichen schon, Erkennbarkeit klärt der Rollengang.
- **2026-11-21** — Beleg aus Theodors Rückmeldungen (Übung 15): „(to) turn sth. up/down“ meldete er als „zu viel verlangt“ (er tippte „turn sth. up/down“ ohne „(to)“, gewertet falsch), „towards the station/Mr Bell“ als „zu streng“. „(to) turn“ und „(to) turn sth. up/down“ liegen als zwei Karten nebeneinander und werden verwechselt. Nicht neu bewertet, nur als Fall für Entscheidung und Akzeptanzkriterien festgehalten.
- **2026-11-22** — (nachgetragen 2026-11-25) Teil gebaut in `1171f1f4`: Vergleich (Platzhalter dürfen fehlen) und
  Kästchen-Maske teilen `AnswerVariants.AccessoryMask`; die Story blieb auf `geschaetzt`, ohne Verlaufszeile. Offen damals:
  Creator-Regel, Pfad-Tests, Mutanten, Review, Rollengang, AK 7.
- **2026-11-25** — `in-arbeit` (Sprint 162, [PM-Sitzung](../pm-sitzung-2026-11-25.md)). Die Platzhalter-Liste zieht nach
  `Pugling.Contracts/Common/WordListPlaceholders.cs` (Server und KI-Creator teilen sie, Muster `VocabKey`).
- **2026-11-25** — Belege je AK-Fall:
  - AK 1, „marcher sur“/„marcher sur qc“, „point out“/„point sth. out“/„to point sth. out to sb.“ →
    `AnswerVariantsTests.Platzhalter_Duerfen_Fehlen` (AnswerVariantsTests.cs:23), Mutant M1 → rot.
  - AK 2, Maske „(to) _____ sth. ___ (to sb.)“ → `StageMechanicsTests.Eintrag_Maske_Setzt_Beiwerk_Fest` (:66) und an der
    Karte `PlatzhalterPfadTests.Kaestchen_Zeigen_Beiwerk_Fest_Und_Der_Ganze_Wert_Zaehlt` (:127), M4/M5 → rot.
  - AK 2, Risiko 2 (der Kästchen-Wert ist der ganze Eintrag und zählt) → dasselbe Kästchen-Test, zweite Zusicherung.
  - AK 3, „be home“ falsch für „(to) be home to sth.“ → `AnswerVariantsTests.Kern_Und_Echte_Woerter_Bleiben_Pflicht` (:31),
    M12 → rot (siehe Tabelle).
  - AK 4, Eintrag ohne Beiwerk wie bisher („something“, „to grow up“) → `Eintrag_Maske_Setzt_Beiwerk_Fest`, M12 → rot;
    die B-462-Tabelle in `AnswerVariantsTests` läuft unverändert grün.
  - AK 5, je Pfad Kern richtig / zu kurzer Kern („point“) falsch → `PlatzhalterPfadTests` Üben (:37), Abschlusstest (:54),
    Vorschau (:75), Bonus-Training getippte Form (:95); M3 (Resolver-Markierung aus) macht alle vier rot, M1 alle
    Kern-Zeilen, M2 (Kern = erstes Wort) alle „point“-Zeilen. Die vier Pfade teilen eine Senke (`AnswerGrader` über
    `GradedAnswers`), darum gibt es keinen Mutanten, der nur einen Pfad trifft.
  - AK 6, Lücke mit Platzhalter (Antwort und Alternative) abgelehnt → `CreatorAgentTests.Eine_Luecke_mit_Platzhalter_der_Wortliste_wird_abgelehnt`
    (CreatorAgentTests.cs:1213), M6/M7/M10 → rot; Grammatik-Lösung → `…Grammatikloesung…` (:1233), M8 → rot; Pflichtwort
    „marcher sur qc“ von „marcher sur“ gedeckt (sonst Endlos-Reparatur) → `…Pflichtwort_mit_Platzhalter…` (:1256), M9 → rot;
    Anfrage (Systemprompt-Regel 9) → `Der_Systemprompt_verbietet_Platzhalter_in_Loesungen` (:1279), M11 → rot.
  - AK 7, Prüfung an der DB-Kopie vom 2026-11-25 (1587 Vokabeln in Übungen): Schrägstrich ohne Leerzeichen zwischen
    **mehrwortigen** Seiten tragen 122 „towards the station/Mr Bell“ und 953 „(to) pose a risk/a problem“ (744 „a hotel/…“
    steht in einer Aufzählung mit Leer-Schrägstrichen). Die Berichtigung an der echten DB steht aus (braucht den Vater).
- **2026-11-25** — Review `pugling-reviewer`: drei 🟡 im KI-Creator, alle behoben mit Test – Pflichtwort mit Klammern
  und Platzhalter (Kern ließ Klammern stehen, M16), Kern-Deckung lockerte die Vokabel-Strategie (jetzt Pflichtschalter
  `slotFree`, M15), Übersetzung prüfte Ziel und Alternativen nicht (M13/M14); dazu 🟢 „(to sb.)“ (M17). Bewusst offen:
  Salat (B-992), eine Lücke mit Store-Bezug ohne `Form` wertet das rohe Store-Wort samt Platzhalter
  (`ExerciseContentResolver.cs:192`, von Hand angelegte Lückentexte; die Agenten-Schultests schreiben `Form`).
- **2026-11-25** — Mutanten-Probe nach der letzten Änderung (M12 einzeln nachgemessen, die erste Fassung traf AK 3 nicht):

| Mutante | Stelle | erwartet | gemessen | Anmerkung |
|---|---|---|---|---|
| M1 | backend/Pugling.Api/Services/Shared/AnswerVariants.cs:71 | red | red |  |
| M2 | backend/Pugling.Api/Services/Shared/AnswerVariants.cs:70 | red | red |  |
| M3 | backend/Pugling.Api/Services/Shared/ExerciseContentResolver.cs:135 | red | red |  |
| M4 | backend/Pugling.Api/Exercises/VocabularyExerciseType.cs:163 | red | red |  |
| M5 | backend/Pugling.Api/Services/Shared/AnswerVariants.cs:48 | red | red |  |
| M6 | backend/Pugling.Agent.Creator/Drafting/ClozeStrategy.cs:130 | red | red |  |
| M7 | backend/Pugling.Agent.Creator/Drafting/ClozeStrategy.cs:129 | red | red |  |
| M8 | backend/Pugling.Agent.Creator/Drafting/GrammarStrategy.cs:61 | red | red |  |
| M9 | backend/Pugling.Agent.Creator/Drafting/DraftRules.cs:107 | red | red |  |
| M10 | backend/Pugling.Agent.Creator/Drafting/DraftRules.cs:146 | red | red |  |
| M11 | backend/Pugling.Agent.Creator/Drafting/DraftPrompts.cs:57 | red | red |  |
| M13 | backend/Pugling.Agent.Creator/Drafting/TranslationStrategy.cs:58 | red | red |  |
| M14 | backend/Pugling.Agent.Creator/Drafting/TranslationStrategy.cs:59 | red | red |  |
| M15 | backend/Pugling.Agent.Creator/Drafting/VocabularyStrategy.cs:112 | red | red |  |
| M16 | backend/Pugling.Agent.Creator/Drafting/DraftRules.cs:116 | red | red |  |
| M17 | backend/Pugling.Contracts/Common/WordListPlaceholders.cs:32 | red | red |  |
| M12 | backend/Pugling.Contracts/Common/WordListPlaceholders.cs:24 | red | red |  |

  Grundlinie nach dem Zurückspielen jeweils grün. M12: `WordListPlaceholders.cs:24` „to“ als Platzhalter → rot an
  `Kern_Und_Echte_Woerter_Bleiben_Pflicht` und `Eintrag_Maske_Setzt_Beiwerk_Fest`.

- **2026-11-25** — Rollengang an der DB-Kopie (Server nach der letzten Änderung gestartet): Vater-Vorschau von Übung 15
  (Theodors Part C) auf Freitext: „turn to“ → richtig, „turn up/down“ → richtig, „(to) turn sth. up/down“ → richtig,
  „turn to smb“ und „turn smt. up/down“ → falsch (B-990). Kästchen-Vorschau: `(to) ____ __ sb.`,
  `(to) ____ sth. __/____`. Theodors eigene Sitzung an Position 102 zeigte heute keine dieser Karten (Tagesdosis 9 Karten);
  der Kindpfad ist über `PlatzhalterPfadTests` belegt. Wartet auf den Vater (`wartet_auf`).
- **2026-11-25** — AK 7 vorbereitet: `scripts/vokabel-schraegstrich-2026-11-25.ps1` ändert genau die zwei benannten Einträge
  (Vorprüfung auf den alten Text, PIN verdeckt) – das ist die Handkorrektur aus Entscheidung 5, kein heuristischer Umbau.
  122 → „towards the station / towards Mr Bell“, 953 → „(to) pose a risk / (to) pose a problem“ (Leer-Schrägstrich = zwei
  Bedeutungen, jede zählt allein). An einer frischen Kopie geprobt: beide Bedeutungen einzeln richtig, „Mr Bell“ allein
  falsch, zweiter Lauf bricht ab. Kosten: die Kästchen verlangen jetzt beide Bedeutungen
  („_______ ___ _______ / _______ __ ____“). Der Lauf am echten Server steht beim Vater.
- **2026-11-25** — AK 7 erledigt: der Vater hat das Skript am echten Server laufen lassen (Log 08:01:41–08:01:43 UTC:
  2× GET, 2× PATCH, je 200). In `pugling.db` lesend geprüft: 122 „towards the station / towards Mr Bell“, 953
  „(to) pose a risk / (to) pose a problem“. Offen bleibt nur der Blick am Gerät (`wartet_auf`).
- **2026-11-25** — Für den Blick am Gerät: `scripts/testjunge-platzhalter-2026-11-25.ps1` legt die Übung „Test: Platzhalter
  (B-626)“ (fünf Store-Einträge mit sth./sb.) in Access Unit 1 an und hängt sie als Kür ohne Leitner auf Stufe 3 und 4 in
  Plan 17 des Testjungen. An einer frischen Kopie geprobt: Runde 1 fünf Lernkarten (Rückruf „turn to“, „point out“ …
  richtig), Runde 2 Kästchen „(to) ____ __ sb.“, „(to) _____ sth. ___ (to sb.)“; zweiter Lauf bricht ab.
- **2026-11-25** — `abgenommen` (Code `459cb60b`, AK 7 `143ff649`). Blick am Gerät durch den Vater als Testjunge an der Testübung
  (Positionen 220/221 in Plan 17): fester Text und Kästchen sind deutlich getrennt – „passt so“. Alle sieben AK belegt
  (Tabelle oben); offen und als eigene Stories geführt: B-990 (eigene Kürzel des Kindes), B-991 („sb.'s“), B-992 (Salat).
- **2026-11-25** — nachgeschaut (Runde 2 der PM-Sitzung): ein Befund, [B-993](B-993-leerzeichen-um-den-schraegstrich.md). AK 7
  (`0d88e81d`) schrieb Vokabel 122 auf „towards the station / towards Mr Bell“ um; der ganze Eintrag ohne Leerzeichen
  getippt war danach falsch. Der Vater fand die Gegenrichtung („up / down“) am Gerät. Gelesen: Skript `0d88e81d`, die
  Bedeutungsteilung in `AnswerVariants`; sonst kein Fund.
