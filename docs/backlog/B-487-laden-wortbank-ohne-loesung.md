---
tags: [typ/story, status/abgenommen, bereich/werkzeug, rolle/creator]
aliases: [handgeschriebene wordBank ohne Lösung, Wortbank im Selbsttest ungeprüft]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: "backend, .claude/skills/lehrwerk-aufbereiten"
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-12-backlog-lauf.md (Nachschau Sprint 36)
unverifiziert: false
entgangen_bei: [B-483]
nachgeschaut: ""
grund: ""
ersetzt_durch: []
---

# B-487 · Eine handgeschriebene Wortbank ohne die Lösung geht durch Prüfung und Selbsttest

Nachschau von Sprint 36 (frischer `pugling-reviewer` an `4a2e170`; `check()`/`build_config()` offline aufgerufen,
kein Server). B-483 hat die Wortbank neben `markup` verboten, weil ihr eine Lösung fehlen könnte – den zweiten Weg,
den `FORMAT.md` und die Fehlermeldung ausdrücklich empfehlen (`text`/`gaps`/`wordBank` von Hand), aber nicht.

1. **`{"text":"I {{1}}.","gaps":[{"index":1,"answer":"went"}],"wordBank":["go","gone"]}` besteht `check()`**
   (gemessen). Der Server prüft nicht, dass die Bank jede Lösung enthält (`ClozeConfig.WordBank` ist eine Liste), und
   der Selbsttest sieht es auf keiner Stufe: die Bank ist reine Anzeige (`Choices`), gewertet wird nur der Text. Das
   Kind spielt Lückentexte standardmäßig auf der Wortbank-Stufe (`DefaultStage => TranslationWordBank`) und bekommt
   eine Lücke, die es nicht lösen kann, während der Lader grün meldet.

## Akzeptanzkriterien

1. **Jede Lücke ist auf der Wortbank-Stufe lösbar**, gleich auf welchem Weg die Bank entstand (Vater-Web, Lader,
   KI-Creator, API) und auch wenn sich ein Speicherwort nach dem Schreiben der Bank ändert.
2. **Die Bank verrät die Lücke nicht:** auf jeder Karte einer Übung derselbe Pool.
3. **Eine passende Kachel genügt:** was die Bewertung für eine Lücke annimmt (andere Groß/Klein-Schreibung,
   Schlusszeichen, eine Alternative), lässt den Pool unverändert.
4. **Der Lader lässt keine still leere oder falsche Bank durch** (die 🟢-Reste unten).

## Entscheidungen (autonom gegrillt, `art: Defekt`)

1. **Die Regel sitzt dort, wo jede Bank zum Kind kommt** – `ClozeExerciseType.Choices`, aufgerufen von
   `PositionPlayService.CardFacets` für Üben, Klausur und Testmodus –, nicht in einem Schreibweg. Vier Nachschauen in
   Folge fanden eine Regel an *einem* Weg zum Ergebnis; die Wortbank hat mindestens vier Schreibwege und mit dem
   Vokabelspeicher einen fünften, der die Lösung *nach* dem Schreiben ändert. Eine Ablehnung beim Schreiben (400)
   hätte den letzten nicht gedeckt.
2. **Ergänzen statt ablehnen:** fehlt für eine Lücke jede passende Kachel, kommt ihre Lösung in den Pool. Eine Bank
   ohne Lösung ist nie gewollt; den Rest der Bank lässt das unberührt (E4 „nicht kürzen" gilt weiter). Kein
   Vertragsbruch, keine Migration; Altbestand wird beim Lesen heil.
3. **„Passend" entscheidet der `AnswerGrader`**, dieselbe Prüfung, die die Wahl des Kindes bewertet – kein zweiter
   Vergleich.
4. **Ergänzt wird für alle Lücken der Übung**, nicht nur für die gefragte: ein Pool, der je Karte anders aussieht,
   nennt die Lösung der gefragten Lücke.
5. **Die Kopie der echten DB** (2026-11-12) hat keine Lückentext-Übung mit Wortbank – für die Söhne war nichts
   kaputt, eine Datenreparatur entfällt.
6. **Lader:** `distractors` vorhanden heißt nicht-leere Liste; ein Ablenker, den die Bewertung als *andere
   Schreibweise* einer Lösung annimmt, ist ein Fehler (eine zweite richtige Kachel); einer gleich einer Hauptlösung
   verschmilzt mit deren Kachel. Kacheln werden nach der Bewertungsregel zusammengelegt (`graded()`, Spiegel von
   `StageMechanics.Normalize`).
7. **Nicht angefasst:** die Lückentext-Bibliothek (`ClozeText`, `creator/cloze-texts`) – sie erreicht kein Kind
   (kein Leser in `Services/`, `Controllers/Student`).

## Reste aus derselben Nachschau (🟢, in AK 4 aufgenommen)

- `"distractors": null` neben `markup` geht still durch (die Formprüfung läuft nur bei `is not None`); das Kind tippt
  dann frei, obwohl `FORMAT.md` „Liste von Wörtern" verspricht.
- `"distractors": []` baut eine Bank nur aus Lösungen (bei einer Lücke eine einzige Kachel).
- Die Doppel-Kachel-Bereinigung in `build_config` ignoriert nur Groß/Klein; der Vergleich faltet auch Apostrophe und
  Schlusszeichen (`[qu'il]` + Ablenker `qu’il` = zwei richtige Kacheln); ein Ablenker gleich einer Alternative einer
  Lücke (`[went|did go]` + `did go`) ist ein richtiger „Ablenker".

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 36 im Backlog-Lauf).
- **2026-11-12** — gegrillt (autonom, `art: Defekt`), geschätzt S, umgesetzt: `ClozeExerciseType.Choices` ergänzt
  fehlende Lösungen; Lader prüft `distractors` und legt Kacheln nach `graded()` zusammen; `FORMAT.md` nachgezogen.
- **2026-11-12** — Belege je AK (`ClozePlayTests`, über die echte Übungs-/Kartenroute des Kindes):
  AK 1, Bank ohne Lösung → `Wortbank_ohne_Loesung_bekommt_sie_auf_jeder_Karte` (C1); AK 1, Speicherwort nach der Bank
  geändert → `Wortbank_folgt_einem_umbenannten_Wort_aus_dem_Speicher` (C5); AK 2, gleicher Pool je Karte → derselbe
  erste Test (C2); AK 3, Groß/Klein + Schlusszeichen und Alternative → `Wortbank_mit_einer_gueltigen_Schreibweise_…`
  (C3, C4); Review: leere Antwort mit Alternative und geteilte fehlende Lösung →
  `Wortbank_ergaenzt_eine_angenommene_Loesung_und_jede_nur_einmal` (C6, C7). AK 4 → Offline-Probe `probe487.py`
  (Scratchpad, ruft `check`/`build_config`/`graded` direkt; D1–D7, K1–K2).
- **2026-11-12** — `pugling-reviewer`: kein 🔴/🟡 – **Review-Zähler Sprint 38: 0**. Drei 🟢 behoben (leere bzw.
  Leerzeichen-Antwort, Abhängigkeit von verzögerter Auswertung, `graded()` trennte an jedem Leerraum statt nur am
  Leerzeichen), zwei Tests dazu. 🟢 offen: kein Test für den Platzhalter einer gelöschten Speichervokabel (gelesen:
  `GradedAnswers` ist dann `[""]`, nichts wird ergänzt).
- **2026-11-12** — Mutanten nach der letzten Code-Änderung (`mutant-probe.py`, 12:02). Zwei frühere Läufe (11:48,
  11:50) **verworfen**: das Test-Tor einer parallelen Sitzung (`test-gate.sh`, `testhost` 79264) sperrte die
  Release-DLL, die Grundlinie kompilierte nicht.

  | Mutante | Stelle | erwartet | gemessen | Anmerkung |
  |---|---|---|---|---|
  | C1 Bank wie geschrieben | BuiltInExerciseTypes.cs:240 | red | red |  |
  | C2 nur die gefragte Luecke | BuiltInExerciseTypes.cs:234 | red | red |  |
  | C3 exakte Schreibweise | BuiltInExerciseTypes.cs:237 | red | red |  |
  | C4 nur Hauptloesung | BuiltInExerciseTypes.cs:237 | red | red |  |
  | C5 Inline-Loesung statt Speicher | BuiltInExerciseTypes.cs:234 | red | red |  |
  | C6 Antwort wie geschrieben | BuiltInExerciseTypes.cs:239 | red | red |  |
  | C7 Pool nicht nachgeprueft | BuiltInExerciseTypes.cs:237 | red | red |  |

  | Mutante (`laden.py`) | erwartet rot | gemessen |
  |---|---|---|
  | P1 leere Liste erlaubt | D2 | rot: D2 |
  | P2 null wie weggelassen | D1 | rot: D1 |
  | P3 Ablenker roh verglichen | D5 | rot: D5 |
  | P4 Lösungen nicht abgezogen | D7 | rot: D7 |
  | P5 Kacheln nur nach Groß/Klein | K1 | rot: K1 |
  | P6 Apostroph nicht gefaltet | K1 | rot: K1 |
  | P7 Schlusszeichen nicht gestrichen | K1 | rot: D5, K1 |
  | P8 trennt an jedem Leerraum | K2 | rot: K2 |

- **2026-11-12** — **abgenommen.** Volle Suite nach der letzten Änderung: 1380/1380, Build ohne Warnung. Review-Zähler
  0. **Rollengang (Sohn) als Maschinengang**: `ClozePlayTests` spielt die Wortbank-Stufe über die Karten-Route des
  Kindes gegen die In-Process-API; kein Browser, keine echte DB (deren Kopie hat keine Übung mit Wortbank).
