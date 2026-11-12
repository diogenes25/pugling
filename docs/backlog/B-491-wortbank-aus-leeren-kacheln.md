---
tags: [typ/story, status/abgenommen, bereich/lernen, rolle/student]
aliases: [leere Kachel in der Wortbank, wordBank aus Leerzeichen verrät die Lösung]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: "backend, .claude/skills/lehrwerk-aufbereiten"
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-12-backlog-lauf.md (Nachschau Sprint 38)
unverifiziert: false
entgangen_bei: [B-487]
nachgeschaut: 2026-11-12
grund: ""
ersetzt_durch: []
---

# B-491 · Eine Wortbank aus leeren Einträgen wird zum Pool aus reinen Lösungen

Nachschau von Sprint 38 (frischer `pugling-reviewer` an `9d648bb`; gelesen, `ClozePlayTests` 10/10, der Fehlerfall
gelesen, nicht gefahren). B-487 ergänzt fehlende Lösungen in `ClozeExerciseType.Choices` – leere Einträge der Bank
zählen dort aber als Bank und bleiben als Kachel stehen.

1. **`wordBank: [""]` oder `["  "]` bei einer Lücke `went`** (`BuiltInExerciseTypes.cs:231-232`): vor B-487 war die
   Lücke unlösbar, jetzt sieht das Kind einen leeren Knopf und `went` – die Lösung ist verraten; bei mehreren Lücken
   ist der Pool genau die Lösungen, der Fall, den B-487 für leere `distractors` im Lader ausdrücklich verbietet.
   Das Vater-Web filtert leere Einträge (`RepeatedTextFields.nonEmpty`), `AnswerChecking.ChoicesOf` ebenso – die
   Lückentext-Bank nicht, und der Lader prüft eine handgeschriebene `wordBank` gar nicht (auch `[]`/`null` gehen
   still durch; das Kind tippt dann auf der „Wortbank"-Stufe frei).

## Reste aus derselben Nachschau (🟢)

- Der KI-Creator vergleicht Ablenker mit Lösung und untereinander nur nach Groß/Klein
  (`Drafting/ClozeStrategy.cs:55-60`, `:115-117`), nicht nach der Bewertungsregel: `[went]` + `went.` oder
  `[qu'il]` + `qu’il` geben zwei richtige Kacheln.
- Die Tests aus B-487 laufen nur über die Übungskarte; Klausur und Testmodus sind nur über `CardFacets` mitgedeckt.
- Der Platzhalter einer gelöschten Speichervokabel bleibt ungetestet.

## Akzeptanzkriterien

1. **Eine Bank aus leeren Einträgen ist keine Bank:** die Wortbank-Stufe bleibt Freitext, wie bei einer weggelassenen
   Bank – keine Kachel ohne Text, kein Pool aus reinen Lösungen.
2. **Ein leerer Eintrag neben echten kommt nicht als Kachel an.**
3. **Einträge, die die Bewertung nicht unterscheidet** (Groß/Klein, Schlusszeichen, Apostroph), sind eine Kachel – die
   zuerst geschriebene. Damit ist auch der 🟢-Rest des KI-Creators gedeckt (`went` + `Went.`, `qu'il` + `qu’il`).
4. **Der Lader** meldet eine handgeschriebene `wordBank`, die keine Liste nicht-leerer Texte ist (`[]`, `null`,
   `[""]`, Nicht-Text), statt sie still durchzulassen.

## Entscheidungen (autonom gegrillt, `art: Defekt`, Nachtlauf 2026-11-12)

1. **Die Bereinigung sitzt an der Senke** (`ClozeExerciseType.Choices`), *vor* der Ergänzung aus B-487, und ist eine
   geteilte Funktion `AnswerChecking.Tiles`, die auch `ChoicesOf` (B-490) benutzt – die Retro von Sprint 39 hat genau
   das verlangt: die Eingangsbereinigung der Nachbarn übernehmen, nicht neu erfinden. Kein zweiter Vergleich: Dubletten
   entscheidet `StageMechanics.Normalize`, dieselbe Faltung, über die der `AnswerGrader` vergleicht.
2. **Leer zählt als „keine Bank"**, nicht als Fehler beim Lesen: Altbestand bleibt spielbar (Freitext), und der Vater
   sieht im Editor ohnehin nur nicht-leere Zeilen (`RepeatedTextFields.nonEmpty`).
3. **Die erste Schreibweise gewinnt** – der Autor hat sie zuerst geschrieben; alphabetisch sortiert wird danach wie
   bisher.
4. **Der KI-Creator wird nicht angefasst:** seine doppelten Ablenker legt jetzt die Senke zusammen. Eine zweite Regel
   in `ClozeStrategy` wäre genau das Muster „Regel je Weg", das sechsmal durchgerutscht ist.
5. **Lader:** dieselbe Formprüfung wie für `distractors` (B-487), Fehler statt Warnung – eine Bank aus Leerzeichen will
   niemand absichtlich.
6. **Die Kopie der echten DB** (2026-11-12 14:55): sieben Lückentexte mit Bank, keiner mit leerem Eintrag oder Dublette –
   für die Söhne ändert sich nichts.

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 38 im Backlog-Lauf).
- **2026-11-12** — Nachtlauf, Sprint 40: autonom gegrillt (`art: Defekt`), geschätzt S, umgesetzt
  (`AnswerChecking.Tiles`, `ClozeExerciseType.Choices`, Formprüfung im Lader).
- **2026-11-12** — Rote Probe vor dem Fix (`ClozePlayTests`, Release): erwartet 3 rot, gemessen 3 rot
  (`Wortbank_aus_leeren_Eintraegen_ist_keine_Wortbank`: `choices` Array statt null; `Wortbank_laesst_leere_Eintraege_weg`:
  Index 0 `""`; `Wortbank_legt_ununterscheidbare_Eintraege_zusammen`: `go` statt `qu’il` an Index 1). Lader-Probe
  `probe491.py` (Scratchpad, ruft `check()` direkt): vor dem Fix 5 von 5 Fehlerfällen unbemerkt (W1–W5), danach 5/5
  gemeldet und 2/2 gültige Fälle still (W6, W7).
- **2026-11-12** — Belege je AK: AK 1 → `Wortbank_aus_leeren_Eintraegen_ist_keine_Wortbank` (C1, C2); AK 2 →
  `Wortbank_laesst_leere_Eintraege_weg` (C1, T1); AK 3, Schlusszeichen + Groß/Klein und Apostroph →
  `Wortbank_legt_ununterscheidbare_Eintraege_zusammen` (T2, T3); AK 4 → `probe491.py` W1–W7.
- **2026-11-12** — `pugling-reviewer`: kein 🔴; **ein 🟡** – die Warnung im Vater-Editor (`exerciseConfig.tsx`,
  `choicesMissAnswer`) behauptete noch „kann nicht richtig antworten"; jetzt sagt sie, dass die Antwort als letzter Knopf
  dazukommt (Doku-Kommentar nennt die Schreibweisen-Falle „York"/„in York"). **Review-Zähler Sprint 40: 1.** 🟢 behoben
  ohne Zählung: behaltene Einträge werden getrimmt (Test `Wortbank_laesst_leere_Eintraege_weg` um `" go "` erweitert,
  Mutante T4), Lader-Kommentar. 🟢 offen, bewusst: die ergänzte Antwort steht immer zuletzt (Entscheidung 2 in B-490);
  eine leere Antwort bleibt unlösbar (nicht neu); Editor- und Server-Normalisierung unterscheiden sich bei NBSP (älter).
  Der Reviewer bestätigt die Senke: Üben, Klausur, Testmodus und „anderes Bild" gehen über `CardFacets`; die Arcade filtert
  nicht nach; der Lader legt Kacheln nach derselben Regel zusammen.
- **2026-11-12** — Mutanten nach der letzten Code-Änderung (`mutant-probe.py`, 15:16, **Debug**: fremde Testläufe
  sperrten zweimal die Release-DLL, beide Läufe verworfen – 14:59 und 15:03, Grundlinie danach „kompiliert nicht"):

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | C1 Bank roh | BuiltInExerciseTypes.cs:235 | red | red |
  | C2 leere Bank zaehlt | BuiltInExerciseTypes.cs:236 | red | red |
  | T1 Leere bleiben | BuiltInExerciseTypes.cs:467 | red | red |
  | T2 Dubletten bleiben | BuiltInExerciseTypes.cs:467 | red | red |
  | T3 nur Gross/Klein | BuiltInExerciseTypes.cs:467 | red | red |
  | T4 nicht getrimmt | BuiltInExerciseTypes.cs:467 | red | red |
  | A1 keine Ergaenzung | BuiltInExerciseTypes.cs:494 | red | red |
  | A2 exakter Vergleich | BuiltInExerciseTypes.cs:494 | red | red |

- **2026-11-12** — **abgenommen.** Volle Suite nach der letzten Änderung 1396/1396 (Release), `tsc -b` sauber,
  `exerciseConfig.test.ts` 22/22. **Rollengang (Sohn) als Maschinengang:** die Tests spielen Wortbank- und Auswahlkarten
  über die Karten-Route des Kindes gegen die In-Process-API; kein Browser. Vater: die geänderte Warnzeile ist nur
  gelesen, nicht im Browser angesehen.
- **2026-11-12** — Nachschau (Retro Sprint 41, Nachtlauf): frischer `pugling-reviewer` an `b521cd9` (per `git archive`,
  1396/1396). Geprüft: Dubletten-Regel gegen jede Bewertungsstelle (Üben `PositionPracticeController.cs:383`, Klausur
  `PositionTestsController.cs:303/:417`, Testmodus `ExercisePreviewService.cs:87` – alle `grader.Matches`), Artikel-/
  Bedeutungs-Nachsicht nur bei Vokabeln (`ExerciseContentResolver.cs:115-117`), Lader-`graded()` gegen `Normalize`,
  Editor-Warnung gegen Serververhalten, Doku nach altem Verhalten (nur Historie in B-73/B-76/B-487). Kein 🔴/🟡; vier 🟢 →
  [B-497](B-497-nachschau-reste-sprint-40.md) (die angehängte Lösung ist ungetrimmt und verrät sich beim Sortieren).
