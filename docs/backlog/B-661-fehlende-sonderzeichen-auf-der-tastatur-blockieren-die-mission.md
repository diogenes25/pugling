---
tags: [typ/story, status/abgenommen, bereich/training, rolle/student, lerntechnik/vokabeln]
aliases: [î nicht tippbar, Wort überspringen, Tastatur-Blocker]
status: abgenommen
prio: P1
art: Defekt
groesse: M
wo: beides
migration: nein
vertragsbruch: nein
quelle: "Vater am 2026-11-18, Feedback nach der ersten echten Runde mit Friedrich"
grund: ""
ersetzt_durch: []
entgangen_bei: [B-598]
wartet_auf: ""
---

# B-661 · Ein Buchstabe, den die Tastatur nicht hat, blockiert die Tagesmission

Friedrichs Tastatur kann nicht alle französischen Buchstaben schreiben (konkret: **î**, i mit Dach). Ein
Wort, das ihn enthält, kann er nicht richtig eintippen – damit lässt sich eine Tagesmission **nicht
abschließen**. Der Vater will vor allem eins: **kein Blocker**.

Drei Vorschläge des Vaters, einzeln oder zusammen:

1. **Grundbuchstabe gilt**: statt „î" darf „i" geschrieben werden (allgemein: Buchstabe ohne Akzent als
   Ersatz für einen nicht tippbaren).
2. **Knopf „Wort geht nicht / überspringen"**, damit das Kind weiterkommt.
3. **Sonderzeichen-Tastatur im Browser** (nachgereicht): Knöpfe für die besonderen Zeichen, an der
   Geräte-Tastatur vorbei.

## User Story

Als **Lernender** möchte ich ein Wort, dessen Sonderzeichen meine Tastatur nicht hat, trotzdem bearbeiten
können, damit ich meine Tagesmission schaffe und nicht an einer Taste hängen bleibe. Als **Supervisor**
möchte ich, dass daraus kein Schlupfloch wird, durch das schwere Wörter einfach übersprungen werden.

## Ist-Stand am Code

Recherchiert am 2026-11-18 gegen `3f7f0b92` (Arbeitsbaum mit unfertigem Sprint 87, die gelesenen Dateien
sind davon nicht berührt).

- **Der harte Blocker ist der Abschreib-Schritt der Lernkarte** ([B-598](B-598-kombi-lernkarte-aufdecken-abschreiben-erinnern.md)).
  Schritt 3 „Schreib es ab“ (`frontend/src/sohn/Lernkarte.tsx:120`) gibt „OK“ erst frei, wenn die Abschrift
  stimmt: `disabled={gesperrt || !abgeschrieben(kopie, wort)}` (`Lernkarte.tsx:131`). `abgeschrieben`
  (`Lernkarte.tsx:26`) vergleicht über `normalizeAnswer` (`frontend/src/lib/answerNormalize.ts:6`), das
  Groß/klein, Leerraum, Apostroph und Schlusszeichen verzeiht, **aber keine Akzente faltet** – „i“ ist nicht
  „î“. Und dieser Schritt hat **keinen** Ausweg: „Später nochmal“ (`lernkarte.giveUp`) gibt es erst in
  Schritt 4 (`Lernkarte.tsx:148`, `:169`). Wer „î“ nicht tippen kann, kommt an der Karte nicht vorbei.
  Da die Runde ihre Reihenfolge einfriert, steckt damit die ganze Runde fest. Die Pflicht einer Lernkarten-
  Position zählt aber nur eine **gespielte** Runde (`PositionProgressService.PlayedEnough`, über
  `IsGoalMetAsync` `backend/Pugling.Api/Services/Shared/PositionProgressService.cs:182`). Das passt genau
  zu „Tagesmission nicht ausführbar“.
- **Im Tipp-Üben und im Test blockiert es nicht, es kostet nur.** Die Antwortfläche
  (`frontend/src/sohn/Lektion.tsx:178`) nimmt jede nicht leere Eingabe an. Der Server wertet „i“ statt „î“
  als falsch, weil `StageMechanics.Normalize` (`backend/Pugling.Api/Services/Shared/StageMechanics.cs:40`)
  keine Akzente faltet. Er markiert sie aber als „falsch geschrieben“ (`AnswerGrader.IsMisspelled`,
  `AnswerGrader.cs:47`), und das Kind kommt weiter. Kosten: Kasten zurück, keine Münze, im Test ein
  Fehler. Eine Prüf-Pflicht ist erfüllt, sobald ein Test bestanden ist
  (`PositionProgressService.cs:191`, Grenze `GoalThreshold`, sonst 80 %). Ein einzelnes î-Wort reißt
  sie also nur, wenn der Test knapp ist.
- **[B-624](B-624-akzentfehler-nicht-ganzer-fehler.md) ist nicht gebaut**, nur `geschaetzt`. Im Code gibt
  es keine Akzent-Faltung in der Wertung (gesucht: `Diacritic`, `NormalizationForm`, `Accent` unter
  `backend/Pugling.Api` und `frontend/src`; einziger Fund ist `Data/InterestSlug.cs:21`, ein Slug, keine
  Wertung). Auch nach B-624 hülfe das der Lernkarte nicht: B-624 ändert die Wertung am Server, nicht den
  lokalen Abschreib-Vergleich im Frontend.
- **Eine Sonderzeichen-Leiste gibt es nicht.** Kein Eingabefeld der Arcade bietet Akzent-Knöpfe an (gesucht
  in `frontend/src/sohn`). Ob die Leiste mit der Kopiersperre verträglich wäre, ist eine Frage: die Sperre
  (`frontend/src/sohn/kopierSperre.ts:39`) verwirft nur `insertText` mit **mehr** als einem Zeichen; ein
  Knopf, der genau ein „î“ einsetzt, fiele nicht darunter, wenn er das Feld über React setzt statt über ein
  `beforeinput`.
- **Welche Tastatur, welches Gerät, welche Wörter**: nicht belegt. Die gängigen Handy-Tastaturen (Gboard,
  iOS) bieten î per langem Druck auf „i“. Dass Friedrich es trotzdem nicht findet, spricht für eine andere
  Tastatur, einen PC ohne französisches Layout oder schlicht eine unbekannte Geste. Die Wörter mit î in
  Plan 15 sind nicht gezählt (die echte DB wird nur an einer Kopie gemessen).

## Die echte Lücke

1. **Der Abschreib-Schritt der Lernkarte hat keinen Ausweg und keine Toleranz.** Das ist der Defekt: ein
   Schritt, der nicht gewertet wird (gewertet wird erst Schritt 4, `Lernkarte.tsx:47`–`:48`), sperrt die
   ganze Runde.
2. **Tippen und Sonderzeichen**: im Tipp-Üben und Test kein Blocker, aber eine Strafe für ein Zeichen,
   das das Kind gar nicht eingeben kann. Das überschneidet sich mit B-624 (Akzent zählt halb, nur im Üben)
   und mit B-422/4 (der Test bleibt streng, weil die Schule streng ist).

## Akzeptanzkriterien

1. **Leiste überall** (E1): Jedes Feld der Arcade, in das ein Wort getippt wird – Tipp-Üben (Freitext),
   Buchstabenkästchen, Lernkarte Schritt 3 und 4, Abschlusstest –, zeigt eine Sonderzeichen-Leiste, wenn die
   getippte Sprache Sonderzeichen hat (Französisch é è ê ë à â ç î ï ô û ù ü œ, Deutsch ä ö ü ß, Englisch
   keine). Nur Kleinbuchstaben. Ein Druck setzt das Zeichen an der Schreibmarke ein, das Feld behält den
   Fokus, die Handy-Tastatur bleibt offen.
2. **Die Kopiersperre lässt die Leiste durch, nichts sonst**: ein Zeichen aus der Leiste wird eingesetzt;
   Einfügen und mehrzeichige Eingaben bleiben gesperrt wie bisher (`kopierSperre.ts`).
3. **Abschreiben bleibt exakt** (E2): „i“ statt „î“ gibt in Schritt 3 „OK“ **nicht** frei. Stimmt die
   Abschrift bis auf Diakritika, steht unter dem Feld „Fast – achte auf die Akzente“.
4. **Kein Blocker beim Abschreiben** (E3): In Schritt 3 bietet die Leiste zusätzlich jedes Nicht-ASCII-
   Zeichen des sichtbaren Wortes an – jedes Wort ist damit abschreibbar. Im Tipp-Üben, in Schritt 4 und
   im Test zeigt die Leiste **nur** die feste Zeichenmenge der Sprache (sie darf nicht verraten, welche
   Akzente die Lösung trägt).
5. **Wertung unverändert** (E4): B-661 ändert nichts an `AnswerGrader`/`StageMechanics.Normalize`.
6. **Kein Überspringen-Knopf** (E5) im Tipp-Üben oder Test.
7. **Hinweis aufs lange Drücken** (E6): an der Leiste ein kurzer Hinweis „Tipp: Taste lange drücken geht
   auch“, nur auf Touch-Geräten.
8. **Tests, die etwas tragen**: (a) Vitest: die Zeichenmenge in Schritt 3 enthält „î“ für „s'abîmer“,
   die feste Menge für Englisch ist leer – rot, wenn die Wort-Ergänzung entfällt; (b) Komponententest
   Lernkarte: „s'abimer“ lässt „OK“ gesperrt und zeigt den Hinweis, „s'abîmer“ über die Leiste gibt
   „OK“ frei – rot, wenn die Leiste das Zeichen nicht einsetzt oder die Toleranz doch kommt; (c) die
   Leiste im Test zeigt kein Zeichen, das nur aus der Lösung stammt – rot, wenn die Wort-Ergänzung in den
   Test durchsickert.

## Offene Punkte

Alle in der Grill-Runde vom 2026-11-18 entschieden – siehe Entscheidungen.

1. ~~**Lernkarte, Schritt 3: Akzente beim Abschreiben verzeihen?**~~ → Entscheidung 2 (nein, mit Hinweis).
2. ~~**Zusätzlich einen Ausweg-Knopf in Schritt 3?**~~ → Entscheidung 3 (kein Knopf, Leiste aus dem Wort).
3. ~~**Eine Sonderzeichen-Leiste über dem Feld?**~~ → Entscheidung 1 (überall, auch im Test).
4. ~~**„i“ statt „î“ im Tipp-Üben und Test als richtig werten?**~~ → Entscheidung 4 (Sache von B-624).
5. ~~**Ein Knopf „Wort geht nicht“ im Tipp-Üben?**~~ → Entscheidung 5 (nein).
6. ~~**Gerät und Tastatur bei Friedrich nachsehen.**~~ → Entscheidung 6 (Handy).

## Entscheidungen

Grill-Runde mit dem Vater, 2026-11-18. Alle sechs nach Empfehlung; Entscheidung 2 weicht von der Empfehlung
der Ausformulierung ab, weil die Leiste aus Entscheidung 1 den Grund für die Toleranz entfernt.

1. **Eine Sonderzeichen-Leiste an jedem Tippfeld der Arcade, auch im Test.** Zeichen je getippter Sprache,
   nur klein, keine Leiste ohne Sonderzeichen. Begründung: sie ersetzt nur eine Taste und verrät nichts;
   ohne sie im Test wäre der Test der einzige Ort, an dem „î“ nicht schreibbar ist – und dort zählt es am
   meisten. *Kosten:* eine Komponente an vier Flächen (Antwortfläche, `LetterBoxes`, Lernkarte 3 und 4),
   eine Zeichentabelle je Sprache, Einsetzen an der Schreibmarke ohne Fokusverlust (Review B-526: Fokus
   darf nicht auf `body` fallen), Abstimmung mit der Kopiersperre; die Sprache der Karte muss beim Feld
   ankommen.
2. **Beim Abschreiben keine Akzent-Toleranz, aber ein Hinweis.** Stimmt die Abschrift bis auf Diakritika,
   steht „Fast – achte auf die Akzente“ unter dem Feld; „OK“ bleibt gesperrt. Begründung: Abschreiben ist
   der billigste Moment, das richtige Wortbild einzuprägen, und der Test wertet den Akzent streng.
   *Kosten:* eine Diakritika-Faltung im Frontend, die nur den Hinweis steuert (nicht die Freigabe).
3. **Kein Ausweg-Knopf; die Leiste ergänzt in Schritt 3 die Sonderzeichen des sichtbaren Wortes.**
   Begründung: schließt den Blocker konstruktiv aus statt per Notausgang, und kein Knopf kürzt das
   Abschreiben ab. *Kosten:* die Leiste kennt zwei Modi (feste Menge / feste Menge + Wort); der Modus mit
   Wort darf nur dort stehen, wo das Wort ohnehin zu sehen ist – sonst verrät sie die Lösung.
4. **Die Wertung ändert B-661 nicht.** „i“ statt „î“ bleibt im Üben und Test, was es heute ist; die mildere
   Wertung im Üben regelt [B-624](B-624-akzentfehler-nicht-ganzer-fehler.md) (entschieden, nicht gebaut).
   Begründung: mit der Leiste ist ein fehlender Akzent keine Tastenfrage mehr, sondern eine Wissenslücke.
   *Kosten:* bis B-624 gebaut ist, kostet ein Akzentfehler im Üben weiter einen ganzen Kasten.
5. **Kein Überspringen-Knopf im Tipp-Üben.** Begründung: dort blockiert nichts; der Knopf wäre gratis ein
   Schlupfloch oder, mit Kosten, dasselbe wie eine falsche Antwort. *Kosten:* zeigt sich doch ein Wort,
   das nicht geht, braucht es eine neue Story.
6. **Friedrich lernt auf dem Handy** – darum zusätzlich ein Hinweis aufs lange Drücken an der Leiste, nur
   auf Touch-Geräten. *Kosten:* eine Zeile, Erkennung über `pointer: coarse`.

## Schätzung

**Größe M** – am Anker „vokabel-basierter Batch-Pfad im `MediaSelector`“ (B-03): ein kleiner Backend-Schritt
(ein additives Feld durch die Kartenbildung) und eine neue Frontend-Komponente an vier Flächen samt drei
Testebenen. Kleiner als L, weil nichts am Schema und nichts an der Wertung.

- **`wo: beides`** – das Frontend kennt die getippte Sprache heute nicht (siehe unten), also Backend zuerst.
- **`migration: nein`** – kein Schema. Die Sprache liegt schon in der DB: `TextbookSeries.SourceLanguage`
  ist „die gelernte Sprache“ (`backend/Pugling.Api/Models/CurriculumEntities.cs:46`–`:47`), und jede Übung
  hängt zwingend an einer Unit dieser Reihe (B-106).
- **`vertragsbruch: nein`** – ein **additives** Feld an den Kartentypen (`PracticeCard`,
  `backend/Pugling.Contracts/Student/PracticeDtos.cs:116`; `TestItem`, `TestDtos.cs:43`; `ExtraCard` des
  Bonus-Trainings, `ExtraTrainingDtos.cs:63`). Bestehende Clients ignorieren es.

### Was der Vertrag heute nicht trägt

Die Karte nennt die Sprache der Antwort nicht. `SpeechLang` ist die Sprache des **Prompts** und nur gesetzt,
wenn das Wort keine Aufnahme hat (`ExerciseContentResolver.cs:108`); beim Umdrehen fällt es weg
(`ExerciseContentProvider.cs:132`). `DictationLang` gibt es nur auf der Diktatstufe, `RevealSpeechLang` nur
auf der Lernkarte. Die Testkarte trägt gar keine Vorlese-Sprache (`frontend/src/sohn/Lektion.tsx:29`).
Keines davon taugt als Quelle für die Leiste.

### Risiken

1. **Reihe ohne Sprache.** `SourceLanguage` ist nullable. Fehlt sie an Friedrichs Französisch-Reihe in der
   echten DB, erscheint keine Leiste und der Blocker bleibt. Vor dem Bau an einer **DB-Kopie** nachsehen;
   als Rückfall die `SourceLanguage` der Store-Vokabel (dieselbe Quelle wie `SpeechLang`).
2. **Einsetzen ohne Fokusverlust.** Ein Leisten-Knopf nimmt den Fokus, am Handy klappt dann die Tastatur
   zu (dieselbe Falle wie Review B-526). Der Knopf muss `onPointerDown` mit `preventDefault` abfangen und
   das Zeichen an der Schreibmarke (`selectionStart`) einsetzen.
3. **Kopiersperre.** Setzt die Leiste über React (`onWert`) statt über ein `beforeinput`, greift
   `kopierSperre.ts:39` nicht – gewollt, aber durch einen Test festzuhalten, damit niemand später „die
   Leiste über `insertText` vereinfacht“ und sie damit stumm sperrt.
4. **`LetterBoxes`** verteilt Buchstaben auf Kästchen und verwaltet die Schreibmarke selbst
   (`frontend/src/components/LetterBoxes.tsx`). Das Einsetzen dort braucht einen eigenen Weg.
5. **Wort-Ergänzung leckt in den Test** (E3). Die Leiste bekommt die Wortzeichen nur als ausdrückliche
   Eigenschaft, die allein die Lernkarte in Schritt 3 setzt – nie aus `reveal` der Karte abgeleitet.

### Angriffsplan

1. **Backend:** `ContentItem` bzw. die Kartenbildung bekommt die Lernsprache der Reihe
   (`Exercise.SeriesUnit.Series.SourceLanguage`, Rückfall Store-Vokabel); neues Feld `KeyboardLang` an
   `PracticeCard`, `TestItem` und `ExtraCard`. Gesetzt auf jeder Karte, deren Antwort getippt wird;
   es verrät nichts (eine Zeichenmenge, keine Lösung). Integrationstest, OpenAPI-Dokument neu
   (Testlauf), `npm run gen:contract`.
2. **Frontend, Baustein:** `lib/sonderzeichen.ts` (Zeichenmenge je Sprache, Diakritika-Faltung für den
   Hinweis) und `components/Sonderzeichen.tsx` (Leiste, Einsetzen an der Schreibmarke, Touch-Hinweis über
   `matchMedia("(pointer: coarse)")`).
3. **Frontend, Flächen:** Antwortfläche (`Lektion.tsx`, dient Üben, Test und Bonus), `LetterBoxes`,
   Lernkarte Schritt 3 (mit Wortzeichen und Akzent-Hinweis) und Schritt 4. Texte über `t()` in `de.ts`/`en.ts`.
4. **Reviewer:** `pugling-reviewer` für Schritt 1, `frontend-reviewer` für 2–3.

### Testweg

- **Backend:** `LernkarteTests` und ein Fall im Positions-Test: `keyboardLang` ist `fr` auf einer
  französischen Karte, auch in umgedrehter Richtung – rot, wenn das Feld nur bei `SpeechLang` mitkommt.
- **Vitest:** `sonderzeichen.test.ts` (Menge je Sprache, Ergänzung aus dem Wort, Faltung für den Hinweis);
  `Lernkarte.test.tsx` und `Lektion.test.tsx` nach AK 8 (a)–(c); `LetterBoxes.test.tsx` für das Einsetzen.
- **E2E:** neue Spec `e2e/sonderzeichen.spec.ts` (eine Fläche je Spec): Lernkarte mit einem Wort mit „î“,
  Abschrift über die Leiste, Runde läuft durch – das ist zugleich der Rollengang. Felder per `tippen()`.
- **Vor dem Bau:** `SourceLanguage` der Reihen in einer Kopie der echten DB prüfen (Risiko 1).

## Verlauf

- 2026-11-18 · idee · aufgenommen aus dem Feedback des Vaters
- 2026-11-18 · ausformuliert · Ist-Stand gegen den Code: der Blocker ist der Abschreib-Schritt der
  Lernkarte (B-598) ohne Ausweg und ohne Akzent-Toleranz; Tipp-Üben und Test blockieren nicht. B-624 ist
  nicht gebaut. Sechs offene Punkte mit Empfehlung.
- 2026-11-18 · gegrillt · sechs Entscheidungen mit dem Vater: Sonderzeichen-Leiste überall (auch Test),
  Abschreiben exakt mit Hinweis, Leiste ergänzt in Schritt 3 die Zeichen des Wortes, Wertung bleibt bei
  B-624, kein Überspringen, Handy → Hinweis aufs lange Drücken.
- 2026-11-18 · geschaetzt · M, beides, keine Migration, kein Vertragsbruch (additives `KeyboardLang` aus
  `TextbookSeries.SourceLanguage`); Hauptrisiken Reihe ohne Sprache in der echten DB und Fokusverlust am Handy.
- 2026-11-18 · in-arbeit · Risiko 1 an einer Kopie der echten DB geprüft: alle Sprachreihen tragen ihre Lernsprache
  (Friedrichs „Découvertes Série bleue 2“ = `fr`), sechs seiner Wörter haben î/ï/œ (u. a. „connaître“, „gîte“,
  „dîner“). Darum kein Rückfall auf die Sprache der Store-Vokabel; keine der 1629 Vokabeln ist zerlegt (NFD) gespeichert.
- 2026-11-18 · abgenommen · Commit `96a3491d`. Backend: `keyboardLang` an `PracticeCard`/`TestItem`/`ExtraCard` aus
  `TextbookSeries.SourceLanguage`; Frontend: `SonderzeichenLeiste` an Antwortfläche, `LetterBoxes`, Lernkarte 3 und 4,
  Akzent-Hinweis beim Abschreiben. Belege: `dotnet test` 1876/1876 grün (neu `TastaturSpracheTests` ×3,
  `Bonuskarten_Nennen_Die_Tastatursprache_Der_Reihe`); Vitest 1244/1244; E2E `sonderzeichen.spec.ts` und
  `lernkarte-leichter.spec.ts` „2 passed“ (Ausweichports, Wegwerf-DB) – die neue Spec ist der Rollengang Sohn.
  Rote Proben am richtigen Ort: Include im Test-Controller weg → nur `Testfrage_…` rot; Sprache aus `SpeechLang` →
  nur `Uebungskarte_…_Umgedreht` rot; Wortzeichen auch in Schritt 4 → nur Lernkarte-Fall 3 rot; Leiste setzt
  doppelt ein → die zwei Einsetz-Fälle rot; NFC weg → nur der NFD-Fall rot. Reviewer: `pugling-reviewer` keine 🔴
  (🟢 überflüssiger Include in `GetSession` entfernt; 🟢 `PreviewItem` ohne Feld bleibt so, die Vater-Vorschau zeigt
  keine Leiste), `frontend-reviewer` keine 🔴: 🟡 Fokus-Schutz war von keiner Probe gedeckt → Komponententest
  `pointerDown`/`mouseDown` + `onMouseDown`; 🟡 gemerktes Kästchen überlebte den Kartenwechsel → Rückfall aufs erste
  leere + Test; 🟢 leere Zusicherung entfernt; 🟢 NFC für zerlegt gespeicherte Wörter; 🟢 `disabled` an den
  Leistenknöpfen bewusst gelassen (der Fokus steht dort praktisch nie). **Offen** (`wartet_auf`): Android-
  Komposition ist nur am echten Gerät prüfbar – die E2E läuft in Desktop-Chromium.
- 2026-11-18 · abgenommen · Heimnetz-Server vom Vater neu gestartet; Rückmeldung nach der Probe auf dem Handy:
  „funktioniert“. `wartet_auf` geleert.
