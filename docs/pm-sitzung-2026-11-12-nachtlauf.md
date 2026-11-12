---
tags: [typ/protokoll, bereich/pm]
---

# PM-Sitzung: Nachtlauf mit vier Sprints

**Datum:** 2026-11-12  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Vier Läufe (Sprints) nach [docs/nachtlauf.md](nachtlauf.md) – Auftrag des Nutzers: „Führe 4 Nachtläufe durch."

Gelesen als: vier Sprints nach `pm-loop`, je mit Rollengang und Retrospektive, unter den Freigaben 1–10 aus
`nachtlauf.md` (Wunsch/Frage nur auf `geschaetzt` oder mit Freigabe-Zeile; Retro schlägt nur vor; Fünfer-Zähler je
Sprint). Branch `auto/fertigstellung`, nichts gepusht. Ausgangsstand `2c6187f`.
Keine parallele Sitzung lief beim Start; trotzdem sperrten fremde Testläufe zeitweise die Release-Ausgabe (siehe
Sprint 40) – Mutanten laufen darum in `Debug`.

Vorlauf aus dem Backlog-Lauf vom selben Tag ([Protokoll](pm-sitzung-2026-11-12-backlog-lauf.md)): nächster Kandidat
B-491 (P2), daneben B-490; B-492/B-493 sind neue Tore und laufen, wenn überhaupt, als letzter Sprint der Nacht.

## Sprint 40 — Ziel & Umfang

**Sprint-Ziel:** *Jede Auswahl, die das Kind antippt – Wortbank-Kachel oder Antwortknopf einer Verstehensfrage –, hat
einen Text, enthält eine richtige Lösung und keine zweite, gleich aus welchem Schreibweg sie kommt.*
**Umfang:** [B-491](backlog/B-491-wortbank-aus-leeren-kacheln.md) (S), [B-490](backlog/B-490-auswahl-ohne-richtige-antwort.md)
(XS) – beide `art: Defekt`, autonom gegrillt.
**Entwickler-Brief:** Senke sind `ClozeExerciseType.Choices` und `AnswerChecking.ChoicesOf` (jede Karte in Üben, Klausur
und Testmodus geht über `CardFacets` dorthin). Eine geteilte Eingangsbereinigung `AnswerChecking.Tiles` (leer raus,
`Normalize`-Dubletten zusammen), danach die Ergänzung der Lösung über den `AnswerGrader`. Lader: Formprüfung der
handgeschriebenen `wordBank` wie bei `distractors`. Keine Migration, kein Vertrag; die Kopie der echten DB hat keinen
betroffenen Datensatz.

## Iteration 40 — umgesetzt

- **B-491/B-490:** `AnswerChecking.Tiles` (leer raus, getrimmt, `Normalize`-Dubletten zusammen, erste Schreibweise
  gewinnt) vor der Wortbank-Ergänzung aus B-487 und in `ChoicesOf`; `ChoicesOf` hängt eine fehlende Antwort hinter die
  Möglichkeiten des Autors. Lader: Formprüfung der handgeschriebenen `wordBank`. Vater-Editor: die Warnung
  `choicesMissAnswer` sagt jetzt, was passiert, statt „kann nicht richtig antworten".
- Rote Probe vor dem Fix: 6 erwartet, 6 gemessen (einer zuerst am falschen Ort – 400 beim Anlegen der Hörübung, Test
  korrigiert); Lader 5/5 Fehlerfälle vorher unbemerkt.
- Review-Zähler: **1** (🟡 Editor-Warnung) – behoben; 🟢 Trimmen ohne Zählung behoben.
- Mutanten 8/8 rot nach der letzten Änderung – in `Debug`, weil fremde Testläufe zweimal die Release-DLL sperrten (zwei
  Läufe verworfen). Ursache: die Sitzung [Doku](#local_345fb4f6-e737-444a-ae47-c98889edcff4) arbeitete parallel im
  selben Checkout (Doku-/Skill-Dateien, uncommittet) und ihr Test-Tor lief mit. Committet habe ich nur eigene Dateien.
- Kopie der echten DB: kein betroffener Datensatz (sieben Wortbanken ohne leeren Eintrag oder Dublette, keine
  Verstehensfrage ohne Antwort).
- Commit `b521cd9`.

## Runde 11 — Rollengang / Abnahme (Sprint 40)

Volle Suite **1396/1396** nach der letzten Änderung, `tsc -b` sauber, `exerciseConfig.test.ts` 22/22.

- **Sohn – abgenommen als Maschinengang.** `ClozePlayTests` und `ExerciseChoicesTests` spielen Wortbank- und
  Auswahlkarten über die Karten-Route des Kindes (In-Process-API): eine Bank aus Leerzeichen bleibt Freitext, keine
  Kachel ohne Text, `went`/`Went.` eine Kachel, eine Frage ohne die Antwort unter den Knöpfen bekommt sie dazu.
  *„Kein leerer Knopf mehr, und jede Frage kann ich richtig machen."* **Nicht gefahren:** Browser, echte DB (deren Kopie
  hat keinen betroffenen Fall).
- **Vater – Regressionszeuge plus eine Zeile.** Die Editor-Warnung ist gelesen, nicht im Browser angesehen.
- **Creator – Regressionszeuge.** Lader-Probe 7/7, beide echten Lehrwerke bestehen `--pruefen`; KI-Creator nicht
  angefasst (seine doppelten Ablenker legt jetzt die Senke zusammen).

## Retrospektive (Sprint 40)

**Nachschau:** Sprint 39 (B-488) von einem frischen `pugling-reviewer` an `ed43705` – der Commit per `git archive`
exportiert und dort gebaut und getestet (1385/1385), weil der Checkout gesperrt war. Geprüft: kein Weg am Briefing
vorbei, benanntes und unbenanntes Profil unter einer Regel, AK 1 und AK 4 samt Mutante M7. **Ein Durchrutscher, der
siebte in Folge:** ein an eine Reihe gebundenes Profil mit fremdem Fach schickt den Creator mit seinem Ratschlag im Kreis
(„eine Reihe in Latein" → „gehört zur Reihe Y") → [B-495](backlog/B-495-profil-an-reihe-gebunden-fach-falsch-zirkulaere-meldung.md)
(P2, `entgangen_bei: B-488`), vier 🟢 dort. `nachgeschaut` an B-488. Index danach: siehe Close-out.

**Was die eigenen Tore in Sprint 40 durchgelassen haben:** nichts bis zum Review; dort ein 🟡, und zwar dieselbe Form wie
die Nachschau: eine **Aussage auf einer anderen Fläche** über die geänderte Regel (die Editor-Warnung beschrieb das alte
Serververhalten). Die Senke war richtig, die Beschreibung der Regel lag woanders. B-495 ist die Gegenrichtung: eine
Meldung, deren Ausweg nur gegen *eine* der zwei Sperren geprüft war.

**Vorschläge (nicht gelandet, Freigabe 3):**

- **(CT)** pm-loop Step 5: *Ändert sich das Verhalten einer Regel, die Oberfläche nach Sätzen absuchen, die das alte
  Verhalten beschreiben* (`frontend/src`, `fieldHelp.ts`, `docs/`) – nicht nur Tests mit altem Text (`old-phrases.py`).
  Ein Tor wüsste nicht, welcher Satz eine Regel beschreibt; das bleibt Lesen. Kosten: ein `grep` je Regeländerung.
- **(CU)** `mutant-probe.py` per Vorgabe in `Debug` bauen: das Test-Tor (`test-gate.sh`) läuft in `Release`, und jede
  parallele Sitzung sperrt damit genau die Ausgabe, die die Probe braucht (zweimal gemessen, zwei Läufe verworfen; der
  Heimnetz-Server läuft aus einer Kopie und sperrt `Debug` nicht mehr).
- **(CV)** Für B-495 die Regel des Sprints 39 noch einmal: eine Meldung, die einen Ausweg nennt, prüft ihn gegen *alle*
  Sperren derselben Senke (hier Reihe *und* Fach in `ProfileFit`), nicht nur gegen die, die sie meldet.

**Weiterlaufen:** kein neues Tor gelandet, Review-Zähler 1 ≤ 5 – der Nachtlauf geht weiter. Anders als im Backlog-Lauf
vom Tag halte ich nicht aus Ermessen: der Auftrag ist ausdrücklich vier Läufe, und der Durchrutscher hat eine Story.
