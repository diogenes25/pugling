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

## Sprint 41 — Ziel & Umfang

**Sprint-Ziel:** *Was Vater und Sohn auf dem Bildschirm lesen, stimmt: keine Sperre als englische Server-Zeile mit Ref,
kein angekündigter Verlust, der nie eintritt, kein Erfolg ohne Messung, kein Häkchen ohne Pflicht.*
**Umfang:** [B-415](backlog/B-415-arcade-sperrcodes-ohne-text.md) (S), [B-418](backlog/B-418-position-entfernen-verspricht-verlust.md)
(XS), [B-444](backlog/B-444-testmodus-fehler-ohne-alert-und-alter-ausweg.md) (XS),
[B-467](backlog/B-467-leerzustaende-melden-erfolg.md) (S) – alle `art: Defekt`, P3, autonom gegrillt.
**Entwickler-Brief:** reine Frontend-Arbeit über bestehende Primitive: Sperrtexte über `problem.*` + `GERMAN_PROBLEM_TEXT`
(Wortgleichheit ist schon ein Test, das Tor `ProblemTextGuardTests` verliert seine Ausnahmen), `StatusBanner` statt eines
eigenen Fehlerkastens, eine zweite kleine Abfrage statt einer neuen API-Größe. Server unverändert (`goalMet: true` für
freie Positionen trägt Tageswertung und Serie).

## Iteration 41 — umgesetzt

- **B-415:** sechs Codes in `PROBLEM_KEYS`, `problem.*` de/en und `GERMAN_PROBLEM_TEXT`; fünf Ausnahmen im Tor entfallen;
  der Sonderfall `media_no_alternative` in `SohnPractice` samt `practice.noMoreImages` fällt weg (Review-🟢).
- **B-418:** `REMOVE_POSITION_QUESTION` – „aus dem Plan nehmen? Die Übung bleibt im Katalog."
- **B-444:** `StatusBanner` im Testmodus; `exercise_empty` = „Diese Übung hat noch keine Inhalte."
- **B-467:** `WeakWords` fragt zusätzlich „gibt es ein Wort?"; freie Positionen ohne Häkchen/Rahmen in Start und Weg.
- Rote Proben: 6/6, 1/1 (nach einer am falschen Ort), 2/2, 1/1 + 1/1 + 1/1 (eine am falschen Ort: fehlender
  `AuthProvider`). Mutanten 10/10 nach der letzten Änderung; F9 war zuerst grün (doppelter Schutz der Pill) – Test ergänzt.
- Review-Zähler: **2** (🟡 „nichts geübt" stimmt nicht bei eingeführten, nie beantworteten Wörtern; roter E2E-Fall, der den
  alten Erfolgs-Satz festschrieb). Reste außerhalb des Diffs → [B-496](backlog/B-496-reste-review-sprint-41.md).
- Commit `1c7b567`.

## Runde 12 — Rollengang / Abnahme (Sprint 41)

`tsc -b` sauber, Vitest **774/774**, Backend **1396/1396**, Playwright **4/4** auf Ausweichports gegen echten Server und
Wegwerf-DB (`PUGLING_E2E_API_PORT=5201`, daneben läuft der Heimnetz-Server).

- **Vater – abgenommen als Maschinengang.** `vater-von-null.spec.ts` öffnet den Lernstand eines Kindes ohne Antwort und
  liest „Noch kein Lernstand"; `full-flow.spec.ts` fährt Vater→Sohn. *„Wenn da steht, es sitzt alles, will ich, dass es
  stimmt."* **Nicht angesehen:** die Entfernen-Frage und der Testmodus-Fehler im Browser (nur Komponententest).
- **Sohn – abgenommen als Maschinengang, mit Gerätevorbehalt.** Komponententests für Start und Weg (kein Häkchen an
  freier Position) und für alle sechs Sperrsätze in beiden Sprachen. **Nicht angesehen:** englische Arcade und der
  Rahmen am Handy.
- **Creator – Regressionszeuge.** Kein Pfad im Diff außer der Kürzung von `exercise_empty`; Suite grün.

## Retrospektive (Sprint 41)

**Nachschau:** Sprint 40 (B-491, B-490) von einem frischen `pugling-reviewer` an `b521cd9`, per `git archive` exportiert
(1396/1396). Geprüft: die Dubletten-Regel gegen jede Bewertungsstelle (Üben, Klausur, Testmodus – alle über
`grader.Matches`), die Nachsicht für Artikel und Bedeutungen (nur Vokabeln), `graded()` im Lader gegen `Normalize`, die
Editor-Warnung gegen das Serververhalten, Doku nach dem alten Verhalten. **Kein 🔴, kein 🟡 – die erste Nachschau ohne
Durchrutscher dieser Stärke nach sieben in Folge.** Vier 🟢 → [B-497](backlog/B-497-nachschau-reste-sprint-40.md)
(`entgangen_bei: B-491, B-490`): die ergänzte Lösung wird nicht getrimmt und verrät sich beim Sortieren; NBSP macht zwei
gleich aussehende Knöpfe; zwei Doku-Stellen. `nachgeschaut` an B-490 und B-491.

**Was die eigenen Tore in Sprint 41 durchgelassen haben:** zweimal dieselbe Form wie in Sprint 40 – eine Aussage über
die geänderte Sache an einer anderen Stelle: der E2E-Fall schrieb den alten Erfolgs-Satz fest (das Tor hat gehalten,
rot), und der neue Satz „nichts geübt" behauptete mehr als gemessen (Review). Dazu zweimal eine rote Probe am falschen
Ort (fehlender `AuthProvider`, fehlende Konstante) und eine Zusicherung, die nur am Namen der Testdaten hing
(„gebunden" im Profilnamen, Sprint 42) – alle drei vor dem Fix bemerkt, weil die Probe die *Meldung* gelesen hat, nicht
nur „rot".

**Vorschläge (nicht gelandet, Freigabe 3):**

- **(CW)** Ein Satz, der einen Leerzustand deutet („nichts geübt", „sitzt alles"), nennt die gemessene Größe
  (Antworten, eingeführte Wörter), nicht eine abgeleitete. Ort: `frontend/CLAUDE.md` neben der Platzhalter-Regel.
- **(CX)** `mutant-probe.py` warnt, wenn eine Zusicherung auf einem Text besteht, der auch in den Testdaten vorkommt –
  zu schwer mechanisch; stattdessen in pm-loop Step 5: *die rote Probe liest die Fehlermeldung, nicht nur die Zahl*.
  (CU) aus Sprint 40 bleibt offen.

**Weiterlaufen:** Review-Zähler 2 ≤ 5, kein neues Tor.

## Sprint 42 — Ziel & Umfang

**Sprint-Ziel:** *Wer dem KI-Creator ein Fachlehrer-Profil gibt, das nicht zum Ort passt, bekommt eine Meldung, deren
Ausweg funktioniert – und ein Profil ohne Fach-Id, eine Reihe ohne Fach oder eine zweite Schreibweise eines Typs rutschen
nicht mehr durch.*
**Umfang:** [B-495](backlog/B-495-profil-an-reihe-gebunden-fach-falsch-zirkulaere-meldung.md) (S, Durchrutscher aus
B-488), [B-455](backlog/B-455-fachlehrer-typen-pruefung-nach-dem-entdoppeln.md) (XS) – beide `art: Defekt`.
**Entwickler-Brief:** Senke bleibt `ProfileFit` im Briefing; sie vergleicht jetzt mit dem Fach der Anfrage (das Briefing
hält es gleich dem der Reihe, wo diese eins hat) und unterscheidet in der Meldung, ob das Profil an die Reihe gebunden
ist. B-455 im Controller: Typprüfung vor dem Entdoppeln. Kein Vertrag, keine Migration.

## Iteration 42 — umgesetzt

- **B-495:** `ProfileFit.Problem(profile, series, subject)` mit `TeachesAnother` (Fach-Id oder Freitext-Name); zwei
  Meldungen nach Bindung; `ResolveProfileAsync` reicht das Fach durch, Summary neu.
- **B-455:** `UnknownTypesProblem` prüft die getrimmten Rohwerte.
- Rote Proben: 3/3 (B-495; eine Zusicherung hing am Profilnamen der Testdaten – umbenannt), 1/1 (B-455).
- Mutanten 4/4 rot (Debug).
- Review-Zähler: **0**. 🟢 ohne Zählung behoben (Meldung rät zuerst zum Fach, zwei Test-Beschriftungen); Reste →
  [B-498](backlog/B-498-reste-review-sprint-42.md).
- Kopie der echten DB: zwei Profile mit passender Fach-Id, kein Freitext-Profil.

## Runde 13 — Rollengang / Abnahme (Sprint 42)

Volle Suite **1405/1405** (Debug), `CreatorAgentTests` + `CreatorProfile*` **148/148** nach der letzten Änderung.

- **Creator – abgenommen als Maschinengang.** Ein an die Reihe gebundenes Profil mit fremdem Fach nennt sich selbst als
  Ursache; ein Freitext-Profil und eine Reihe ohne Fach werden geprüft; `["Cloze","cloze"]` ist ein 400.
  *„Endlich sagt er mir, dass mein Profil falsch ist – und nicht, ich soll eine Latein-Reihe nehmen, die er dann auch
  ablehnt."* **Nicht gefahren:** Ollama, echte DB (Kopie: kein betroffenes Profil).
- **Vater, Sohn – Regressionszeugen.** Kein Pfad im Diff; Suite grün.

## Sprint 43 — Ziel & Umfang

**Sprint-Ziel:** *Auch was die Senke selbst ergänzt oder vergleicht, verrät nichts und sieht nie doppelt aus: die
ergänzte Lösung sortiert sich ein, und ein geschütztes Leerzeichen macht keinen zweiten Knopf.*
**Umfang:** [B-497](backlog/B-497-nachschau-reste-sprint-40.md) (S, Nachschau-Reste aus Sprint 40) – `art: Defekt`.
**Entwickler-Brief:** die Senke des Vergleichs ist `StageMechanics.Normalize` (Bewertung, Zusammenlegen und Ergänzen
gehen alle darüber): Leerraum statt Leerzeichen. Getrimmt wird beim Ergänzen, nicht beim Speichern (Altbestand heilt
beim Lesen). Lader zieht `graded()` nach und prüft die eigene `wordBank` gegen Alternativen wie den `markup`-Weg.

## Iteration 43 — umgesetzt

- Rote Proben: 4/4 (Server), 4 von 6 Lader-Fällen. Mutanten 3/3 rot. Volle Suite 1405/1405 (Debug, vor dem Review).
- Gebaut, während der Review von Sprint 42 lief (getrennte Dateien, getrennte Commits); der Reviewer von Sprint 42 sah die
  vier roten Proben im Arbeitsbaum und hat sie richtig als fremd eingeordnet.

## Retrospektive (Sprint 42)

**Nachschau:** Sprint 41 (B-415, B-418, B-444, B-467) von einem frischen `frontend-reviewer` an `1c7b567` (Vitest des
Commits 55/55). Geprüft: alle Leser von `goalMet`/`cadence`, alle 31 `confirmAction`-Texte gegen das Serververhalten,
Erfolgssätze auf leeren Daten, de/en-Schlüssel, Doku. **Zwei 🟡 durchgerutscht** →
[B-499](backlog/B-499-nachschau-reste-sprint-41.md) (P2, `entgangen_bei: B-467, B-418`): der korrigierte Leerfall-Satz
misst „gewertete Antwort" (Selbsteinschätzungs-Stufen erzeugen keinen Lernstand), und „Übung löschen" kündigt wie B-418
eine Wirkung an, die der Server ablehnt. `nachgeschaut` an allen vier.

**Was die eigenen Tore durchgelassen haben:** beide Funde haben die Form, die die Retro von Sprint 40 schon benannt hat
(CT): eine Aussage über eine Regel an einer Stelle, die die Regel nicht kennt. Bei B-467 hat das Review den ersten Satz
korrigiert und der Ersatz hat dieselbe Lücke eine Ebene tiefer – „beantwortet" statt „gewertet"; die Bedingung für
„gewertet" steht in einem Controller, den keiner der Frontend-Leser öffnet. Bei B-418 lag die Schwester-Stelle in einer
anderen Datei, und ich habe nicht nach allen `confirmAction` gesucht. Sprint 42 selbst: Review-Zähler 0.

**Vorschläge (nicht gelandet, Freigabe 3):**

- **(CY)** pm-loop Step 5: *Für eine Story über einen Satz (Bestätigung, Leerfall, Sperre) alle Sätze derselben Art
  greppen* (`confirmAction(`, `colSpan`-Leerzeilen, `problem.*`) – (CR) für Texte. Ein Tor könnte `confirmAction` gegen
  die 409-Codes der gerufenen Route halten, aber die Abbildung Knopf → Route ist nicht mechanisch lesbar.
- (CW) aus Sprint 41 präzisiert: der Leerfall nennt die Größe *so, wie der Server sie zählt* – Quelle zitieren
  (`countsForMastery`), nicht umschreiben.

**Weiterlaufen:** Review-Zähler 0, kein neues Tor – Sprint 43 war schon gebaut (siehe dort).
