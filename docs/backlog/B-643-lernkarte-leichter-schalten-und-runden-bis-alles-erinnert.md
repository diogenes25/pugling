---
tags: [typ/story, status/ausformuliert, bereich/training, bereich/frontend, rolle/student, lerntechnik/vokabeln]
aliases: [Lernkarte leichter, Mach's mir leichter in der Lernkarte]
status: ausformuliert
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Chat mit dem Vater, 2026-11-17 (Kombi-Lernkarte B-598, Leichter-Knopf aus B-635)"
grund: ""
ersetzt_durch: []
---

# B-643 · Lernkarte: „Mach's mir leichter“ beim Erinnern, Runden bis jedes Wort aus dem Gedächtnis sitzt

Die Kombi-Lernkarte aus [B-598](B-598-kombi-lernkarte-aufdecken-abschreiben-erinnern.md) zeigt das Wort, lässt es
abschreiben und dann aus dem Gedächtnis schreiben – das bleibt so. Neu: Im letzten Schritt (aus dem Gedächtnis) gibt
es den Knopf „Mach's mir leichter“ wie in der Nacharbeit
([B-635](B-635-nacharbeit-leichter-schalten-und-runden.md)). Er lässt sich mehrfach drücken und schaltet Stufe für
Stufe herunter, bis das Kind es kann. Leichter geschaltet zählt nicht als geschafft: Geschafft ist eine Karte erst,
wenn das Wort frei aus dem Gedächtnis richtig geschrieben ist. Danach kommt die nächste Vokabel; nicht geschaffte
Vokabeln kommen wieder, und zwar so lange, bis jede einmal korrekt aus dem Gedächtnis geschrieben ist. Das ersetzt
B-598 Entscheidung 7 („höchstens einmal am Ende derselben Runde wieder“). [Abbrechen] beendet die Runde; in der
Pfadübersicht ist der gelbe Kreis der Station dann nicht geschlossen, und die Übung gilt als nicht geschafft.

## User Story

Als **Vater** möchte ich, dass mein Kind ein neues Wort erst dann als gelernt abhakt, wenn es dieses Wort einmal frei
aus dem Gedächtnis richtig geschrieben hat, und dass es sich bis dahin selbst Hilfe holen darf, damit der Kreis im
Pfad zeigt, was wirklich sitzt, und nicht, was nur einmal vorbeigezogen ist.

Als **Sohn** möchte ich bei einem Wort, das mir nicht einfällt, „Mach's mir leichter“ drücken können, statt nur
aufzugeben.

## Ist-Stand am Code

Gelesen am 2026-11-17 (`135957b6`).

**Die Lernkarte heute (B-598, abgenommen):**

- Vokabeln spielen ein neues Wort als Lernkarte: `IntroductionStage => LearnCard`
  (`backend/Pugling.Api/Exercises/VocabularyExerciseType.cs:79`, Stufe `TestStage.LearnCard = 8`,
  `backend/Pugling.Api/Models/StudyPlanEntities.cs:60`). Eine Karte, deren Wort nur erkannt werden soll, sieht statt
  der Lernkarte „Beide zeigen“ (`CardCeiling`, `VocabularyExerciseType.cs:99-102`).
- Schritt 4 im Frontend: ein Freitextfeld mit [OK] und [Aufgeben]; Aufgeben schickt `givenAnswer: null`
  (`frontend/src/sohn/Lernkarte.tsx:109-121`, Knopf `:119-120`). Einen Weg zu einer leichteren Form gibt es nicht.
- Der Server wertet Schritt 4 in `LearnCardReviewAsync`
  (`backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:663-711`): richtig erinnert oder nicht,
  zwei Historien-Zeilen ohne Münze und ohne Box (`:686-691`).
- **Wiederholung höchstens einmal:** gibt das Kind auf oder schreibt es falsch, hängt der Server den Index ein
  einziges Mal ans Ende der eingefrorenen Folge (`:694-695`). Ein zweites Vorkommen des Index ist die Wiederholung
  (`IsRepeat`, `:141-142`) und wird wieder als Lernkarte gespielt (`StageAt`, `:148-156`).
- **„Kennengelernt“ heißt heute „einmal gesehen“:** Schon die erste Antwort auf die Lernkarte setzt
  `IntroducedAt`, ob erinnert oder aufgegeben (`:673-677`). Ab dann gilt das Wort als eingeführt und spielt in der
  nächsten Runde seine Leiter-Stufe, nicht mehr die Lernkarte (`PositionPlayService.cs:187`).

**Der gelbe Kreis im Pfad:**

- Der Ring ist der Anteil eingeführter Wörter am Pool: `kennenlernAnteil = introducedCount / poolSize`
  (`frontend/src/sohn/pfad.ts:47-50`), gezeichnet in Gold (`frontend/src/sohn/SohnHome.tsx:250-259`).
- `introducedCount` zählt die Fortschrittszeilen mit `IntroducedAt != null`
  (`backend/Pugling.Api/Services/Shared/PositionProgressService.cs:222`). Ein aufgegebenes Wort füllt den Ring also
  heute schon genauso wie ein erinnertes.
- Der Haken „geschafft“ am Knoten ist etwas anderes: `goalMet` (`pfad.ts:34`, `SohnHome.tsx:263`). Für eine prüfbare
  Übung wie Vokabeln ist das Ziel ein **bestandener Test** im Zeitraum, nicht die Lernrunde
  (`PositionProgressService.cs:147-151`). Nur Übungen ohne Prüfung oder an einem reinen Anzeigetag messen die
  gespielte Runde, und zwar über 80 % der eingefrorenen Folge (`PlayedEnough`, `:124-129`, `DefaultPassPercent` `:18`).

**Leichter schalten:**

- Das gibt es noch nicht. B-635 (`geschaetzt`, nicht gebaut) plant `POST …/practice/{sessionId}/easier`, die Stufe je
  Karte als JSON-Spalte an `PracticeSession` und die Leiter Freitext → Buchstabenkästen → Buchstabensalat → Auswahl.
  Ein Endpunkt `easier` existiert im Controller nicht (Suche am 2026-11-17 ohne Treffer).
- Die Stufen dieser Leiter gibt es bei Vokabeln schon als eigene Abfrageformen: `LetterBoxes = 3`, `FreeText = 4`,
  `MultipleChoice = 6`, `LetterScramble = 7` (`StudyPlanEntities.cs:43-56`). Die Lernkarte spielt sie aber nicht; ihr
  Schritt 4 ist immer das freie Feld.
- Die Stufe einer Karte setzt nur der Server (`PositionPracticeController.cs:437-442`, zitiert in B-635); eine
  leichtere Form wählt das Frontend heute nicht selbst.

## Die echte Lücke

1. In Schritt 4 gibt es nur „schreib es“ oder „gib auf“, keine Zwischenhilfe.
2. Die Runde gibt ein nicht erinnertes Wort nach einer Wiederholung frei. Danach gilt es als eingeführt und kommt
   nicht mehr als Lernkarte.
3. Der Kreis zählt „gesehen“, nicht „einmal aus dem Gedächtnis geschrieben“. Ein Kind, das jede Karte aufgibt, hat
   denselben vollen Kreis wie eines, das alle erinnert.
4. Kein Zustand hält fest, dass ein Wort aus dem Gedächtnis „geschafft“ ist.

## Akzeptanzkriterien (Entwurf)

1. In Schritt 4 der Lernkarte gibt es „Mach's mir leichter“. Jeder Druck schaltet genau eine Stufe tiefer, wieder
   schwerer geht nicht. Der Server führt die Stufe; das Frontend kann sie nicht selbst wählen.
2. Eine Antwort auf einer leichteren Stufe zeigt, ob sie richtig war, und die Lösung. Das Wort ist damit nicht
   geschafft.
3. Geschafft ist ein Wort erst mit einer richtigen Antwort im freien Feld von Schritt 4. Ein Integrationstest belegt,
   dass eine leichter geschaltete Karte nie „geschafft“ setzt, auch wenn der Client sie als Freitext schickt; er war
   vor dem Bau rot.
4. Nicht geschaffte Wörter kommen in derselben Runde wieder, bis jedes Wort der Runde geschafft ist. Die Grenze
   „höchstens einmal“ aus B-598 fällt.
5. Bricht das Kind die Runde ab, bleiben die nicht geschafften Wörter offen. Der gelbe Kreis zählt nur geschaffte
   Wörter und ist dann nicht voll.
6. Test, Klausur, Bonus und Info-Modus spielen weiter keine Lernkarte (B-598, Entscheidung 9).
7. Leichter schalten kostet keine Münzen, und die Lernkarte zahlt weiter nichts.

## Offene Punkte

1. **Welche Stufen hat „leichter“ in Schritt 4?** Empfehlung: dieselbe Leiter wie B-635 – Buchstabenkästen,
   Buchstabensalat, Auswahl –, damit Kind und Code nur eine Leiter kennen. Alternative: zuerst „Wort noch einmal
   zeigen“ (zurück zu Schritt 3, abschreiben). Das ist näher an der Lernkarte, aber es verrät die Lösung ganz.
2. **Wann kommt ein leichter geschaltetes Wort wieder?** Empfehlung: ans Ende der Runde, nicht sofort. Nach der
   Auswahl hat das Kind die Lösung eben gesehen; sofort noch einmal abgefragt, wäre das Abschreiben aus dem
   Kurzzeitgedächtnis. B-635 entscheidet das genauso.
3. **Bleibt [Aufgeben] je Karte?** Empfehlung: ja, als „später nochmal“. Das Wort geht ans Rundenende, die nächste
   Vokabel kommt. Ohne den Knopf kann das Kind nur noch leichter schalten oder die ganze Runde abbrechen.
4. **Was heißt [Abbrechen]?** Du hast vom Abbrechen der Übung gesprochen. Gemeint ist vermutlich das Schließen der
   Runde (✕ in der Lektion), nicht der Knopf je Karte. Empfehlung: ja, so lesen. Schließt das Kind, bleiben die
   offenen Wörter offen, ohne Malus über die Pflicht-Regel hinaus.
5. **Was misst der gelbe Kreis künftig?** Empfehlung: „geschafft“ (einmal frei erinnert) statt „gesehen“. Folge:
   Ein aufgegebenes Wort ist zwar eingeführt, füllt den Kreis aber nicht. Kosten: Der Kreis bedeutet dann bei
   Vokabeln etwas anderes als bei Übungen ohne Lernkarte, wo er weiter „gesehen“ zählt. Die Beschriftung
   (`home.introduced`, „N von M kennengelernt“) müsste mitwandern.
6. **Kommt ein offenes Wort am nächsten Tag wieder als Lernkarte?** Heute nicht: Die erste Antwort setzt
   `IntroducedAt`, danach spielt das Wort seine Leiter-Stufe (Selbsteinschätzung in Box 1). Empfehlung: ja, bis es
   geschafft ist. Sonst wandert ein abgebrochenes Wort in die Abfrage, ohne je erinnert worden zu sein, und der
   Kreis bliebe dauerhaft offen. Kosten: Der Tageszähler neuer Wörter (`DailyTestPortion`,
   `PositionPlayService.cs:310-315`) muss offene Lernkarten mitzählen, sonst wächst die Dosis an Tagen mit
   Abbrüchen.
7. **Was heißt „Übung nicht geschafft“?** Der Haken am Knoten hängt bei Vokabeln am bestandenen Test, nicht an der
   Lernrunde (`PositionProgressService.cs:147-151`). Empfehlung: das so lassen. „Nicht geschafft“ zeigt der offene
   Kreis; der Test bleibt die Pflicht. Alternative: Die Pflicht verlangt zusätzlich, dass alle neuen Wörter des
   Tages geschafft sind. Das ist strenger, aber ein Malus für ein Wort, das das Kind tapfer leichter geschaltet hat.
8. **Gibt es eine Notbremse gegen eine endlose Runde?** Mit unbegrenzter Wiederholung und einem Kind, das ein Wort
   einfach nicht schreiben kann, endet die Runde nur über Abbrechen. Empfehlung: keine Grenze, denn Abbrechen ist
   die Notbremse und der Kreis zeigt das Ergebnis ehrlich. Alternative: nach drei Durchgängen ohne Erfolg geht das
   Wort für heute aus der Runde.
9. **Reihenfolge zu B-635.** Beide brauchen die Stufe je Karte am Server und den Endpunkt `easier`. Empfehlung: B-635
   zuerst bauen (P1) und B-643 auf dessen Mechanik setzen, nicht umgekehrt und nicht doppelt.

## Verlauf

- **2026-11-17** — angelegt (Quelle: Wunsch des Vaters im Chat; ändert B-598 Entscheidung 7, nutzt die Mechanik
  aus B-635).
- **2026-11-17** — ausformuliert: Ist-Stand am Code (Lernkarte, Wiederholung, `IntroducedAt`, Ring, Ziel-Regel),
  neun offene Punkte mit Empfehlung. Kern: Der Kreis zählt heute „gesehen“, und ein Wort ist nach der ersten Antwort
  eingeführt, auch aufgegeben.
