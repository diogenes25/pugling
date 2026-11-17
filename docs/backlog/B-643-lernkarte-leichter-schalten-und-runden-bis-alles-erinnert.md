---
tags: [typ/story, status/gegrillt, bereich/training, bereich/frontend, rolle/student, lerntechnik/vokabeln]
aliases: [Lernkarte leichter, Mach's mir leichter in der Lernkarte]
status: gegrillt
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
wartet_auf: B-635
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

**Die Ringe im Pfad** (Stand `a28358d3`, „zweiter Ring für den Stand der Pflicht“, am 2026-11-17 aus einer
parallelen Sitzung):

- Außen, gold: der Anteil eingeführter Wörter am Pool, `kennenlernAnteil = introducedCount / poolSize`
  (`frontend/src/sohn/pfad.ts:47-50`, gezeichnet `frontend/src/sohn/SohnHome.tsx:277`), beschriftet „N von M schon
  kennengelernt“ (`frontend/src/i18n/de.ts:39`).
- `introducedCount` zählt die Fortschrittszeilen mit `IntroducedAt != null`
  (`backend/Pugling.Api/Services/Shared/PositionProgressService.cs:244`). Ein aufgegebenes Wort füllt den goldenen
  Ring also heute schon genauso wie ein erinnertes.
- Innen, grün: der Stand der Pflicht, `zielAnteil` (`pfad.ts:57`, `SohnHome.tsx:278`), voll genau mit `goalMet`. Für
  eine prüfbare Übung wie Vokabeln ist das Ziel ein **bestandener Test** im Zeitraum, nicht die Lernrunde
  (`PositionProgressService.cs:147-151`); bis dahin bleibt der grüne Ring leer. Nur Übungen ohne Prüfung oder an einem
  reinen Anzeigetag messen die gespielte Runde (`PlayedEnough`, `:124-129`, `DefaultPassPercent` `:18`).

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
3. Der goldene Ring zählt „gesehen“, nicht „einmal aus dem Gedächtnis geschrieben“. Ein Kind, das jede Karte aufgibt, hat
   denselben vollen Kreis wie eines, das alle erinnert.
4. Kein Zustand hält fest, dass ein Wort aus dem Gedächtnis „geschafft“ ist.

## Offene Punkte

1. ~~**Welche Stufen hat „leichter“ in Schritt 4?**~~ → Entscheidung 3.
2. ~~**Wann kommt ein leichter geschaltetes Wort wieder?**~~ → Entscheidung 4.
3. ~~**Bleibt [Aufgeben] je Karte?**~~ → Entscheidung 5.
4. ~~**Was heißt [Abbrechen]?**~~ → Entscheidung 6.
5. ~~**Was misst der gelbe Kreis künftig?**~~ → Entscheidung 1.
6. ~~**Kommt ein offenes Wort am nächsten Tag wieder als Lernkarte?**~~ → Entscheidung 2.
7. ~~**Was heißt „Übung nicht geschafft“?**~~ → Entscheidung 7.
8. ~~**Gibt es eine Notbremse gegen eine endlose Runde?**~~ → Entscheidung 8.
9. ~~**Reihenfolge zu B-635.**~~ → Entscheidung 9.

## Entscheidungen

Gegrillt mit dem Vater am 2026-11-17.

1. **Zwei Begriffe statt eines: „eingeführt“ und „eingeprägt“. Der goldene Ring zählt eingeprägte Wörter, beschriftet
   „N von M eingeprägt“.** *Eingeführt* heißt wie heute: Die Lernkarte wurde beantwortet (`IntroducedAt`), das steuert
   weiter die Fälligkeit. *Eingeprägt* heißt: einmal frei aus dem Gedächtnis richtig geschrieben, ohne leichter
   geschaltet zu haben. Übungen ohne Lernkarte (Erkennen, andere Typen) zählen im Ring weiter „eingeführt“.
   *Begründung:* Der Ring soll zeigen, was sitzt, nicht was vorbeigezogen ist; nach einem Abbruch ist er nicht voll.
   *Kosten:* Der goldene Ring bedeutet je nach Übung leicht Verschiedenes; ein neuer Zustand je Wort und Position,
   `PositionStatus` braucht eine zweite Zahl neben `IntroducedCount`, und die Beschriftung wandert mit.
2. **Ein noch nicht eingeprägtes Wort kommt am nächsten Tag wieder als Lernkarte, bis es eingeprägt ist; offene
   Lernkarten zählen auf die Tagesdosis neuer Wörter.** Bei 6 neuen am Tag und 2 offenen von gestern kommen 2 offene
   und 4 neue. *Begründung:* Sonst wandert ein abgebrochenes Wort in die Abfrage, ohne je erinnert worden zu sein, und
   der Ring bliebe für immer offen. *Kosten:* `CardStage`/`IsIntroduction` (`PositionPlayService.cs:187`) fragen künftig
   „eingeprägt?“ statt „eingeführt?“; die Tagesdosis (`DailyTestPortion`, `:310-315`, und die Rundenzusammenstellung)
   rechnet offene Lernkarten an.
3. **Die Leiter im letzten Schritt ist die aus B-635: Buchstabenkästen, Buchstabensalat, Auswahl.** Ein Druck auf
   „Mach's mir leichter“ = eine Stufe, nie zurück. Wo der Buchstabensalat nicht geht (mehr als ein Wort, mehr als
   12 Buchstaben; `TestStage.LetterScramble`, `StudyPlanEntities.cs:52-55`), wird er übersprungen. *Begründung:* eine
   Leiter für Kind und Code; alle drei Formen gibt es bei Vokabeln schon. *Kosten:* Die Lernkarte rendert in Schritt 4
   drei Abfrageformen, die sie heute nicht kennt.
4. **Ein leichter geschaltetes Wort geht ans Rundenende und kommt dort als ganze Lernkarte wieder** (aufdecken,
   abschreiben, erinnern). *Begründung:* Sofort nach der Auswahl getippt, wäre es Abschreiben aus dem Kurzzeitgedächtnis;
   ein gescheiterter Versuch profitiert vom Abschreiben. B-635 entscheidet die Rundenfolge genauso. *Kosten:* Runden
   werden länger.
5. **[Aufgeben] bleibt und heißt „Später nochmal“:** das Wort geht ohne Hilfe ans Rundenende. Die Historie speichert es
   weiter als „aufgegeben“ (B-598, Entscheidung 8). *Begründung:* ein Ausweg ohne Durchklicken bis zur Auswahl, wenn
   gerade nichts kommt. *Kosten:* ein Knopf mehr neben „leichter“ und [OK].
6. **„Abbrechen“ ist das ✕ der Lektion: ohne Rückfrage, ohne eigene Strafe.** Eingeprägte Wörter bleiben eingeprägt,
   die übrigen bleiben offen und kommen morgen wieder (Entscheidung 2). *Begründung:* „noch nicht drin“ ist ein Grund
   für Wiederholung, nicht für Strafe (B-598, Entscheidung 7); den Preis zahlt nur eine verpasste Pflicht. *Kosten:*
   kein Schubs zum Weitermachen.
7. **Die Pflicht bleibt der bestandene Test;** grüner Ring und Haken ändern sich nicht. *Begründung:* Die Lernkarte
   bleibt strafenfrei, und die Pflicht-Regel aller laufenden Vokabel-Positionen (Pläne 8, 9, 10) bleibt stehen.
   *Kosten:* Ein Kind kann die Pflicht erfüllen, ohne alle neuen Wörter eingeprägt zu haben; das zeigt nur der
   goldene Ring.
8. **Keine Grenze für Wiederholungen in einer Runde.** B-598, Entscheidung 7 („höchstens einmal“) entfällt. Das ✕ ist
   die Notbremse. *Begründung:* Eine Grenze nähme ein Wort heraus, das beim nächsten Versuch vielleicht sitzt; der
   Ring zeigt ehrlich, was fehlt. *Kosten:* Eine Runde endet ohne Abbruch erst, wenn alles eingeprägt ist; kein
   „fertig“-Erlebnis für ein Kind, das an einem Wort hängt. `IsRepeat` (`PositionPracticeController.cs:141-142`) erkennt
   die Wiederholung über das zweite Vorkommen und muss beliebig viele tragen.
9. **B-635 zuerst; B-643 setzt auf dessen Stufe je Karte und `easier` auf** (`wartet_auf: B-635`). *Begründung:* B-635
   ist P1 und geschätzt, das Fundament wird nur einmal gebaut. *Kosten:* Die Lernkarte wartet auf die
   Lückentext-Nacharbeit.

## Akzeptanzkriterien

1. In Schritt 4 der Lernkarte gibt es „Mach's mir leichter“. Jeder Druck schaltet genau eine Stufe tiefer –
   Buchstabenkästen, Buchstabensalat (übersprungen, wo er nicht geht), Auswahl –, wieder schwerer geht nicht. Der
   Server führt die Stufe; das Frontend kann sie nicht selbst wählen.
2. Eine Antwort auf einer leichteren Stufe zeigt, ob sie richtig war, und die Lösung. Das Wort ist damit nicht
   eingeprägt und geht ans Rundenende, wo es als ganze Lernkarte wiederkommt.
3. Eingeprägt ist ein Wort erst mit einer richtigen Antwort im freien Feld von Schritt 4, ohne leichter geschaltet zu
   haben. Ein Integrationstest belegt, dass eine leichter geschaltete Karte nie „eingeprägt“ setzt, auch wenn der
   Client sie als Freitext schickt; er war vor dem Bau rot.
4. „Später nochmal“ (vorher [Aufgeben]) und ein falsches Wort schicken das Wort ans Rundenende. Es gibt keine Grenze:
   Die Runde endet von selbst erst, wenn jedes Wort eingeprägt ist. Ein Test belegt eine dritte Wiederholung desselben
   Worts (heute rot: höchstens eine).
5. Das ✕ schließt die Runde ohne Rückfrage und ohne Münzabzug. Nicht eingeprägte Wörter kommen am nächsten Tag wieder
   als Lernkarte und zählen auf die Tagesdosis neuer Wörter (Test: 6 am Tag, 2 offene → 2 offene + 4 neue).
6. Der goldene Ring zählt eingeprägte Wörter und ist beschriftet „N von M eingeprägt“; nach einem Abbruch ist er nicht
   voll. Übungen ohne Lernkarte zählen weiter eingeführte Wörter.
7. Grüner Ring und Haken bleiben unverändert am bestandenen Test.
8. Test, Klausur, Bonus und Info-Modus spielen weiter keine Lernkarte (B-598, Entscheidung 9). Leichter schalten kostet
   keine Münzen, und die Lernkarte zahlt weiter nichts.

## Verlauf

- **2026-11-17** — angelegt (Quelle: Wunsch des Vaters im Chat; ändert B-598 Entscheidung 7, nutzt die Mechanik
  aus B-635).
- **2026-11-17** — ausformuliert: Ist-Stand am Code (Lernkarte, Wiederholung, `IntroducedAt`, Ring, Ziel-Regel),
  neun offene Punkte mit Empfehlung. Kern: Der Kreis zählt heute „gesehen“, und ein Wort ist nach der ersten Antwort
  eingeführt, auch aufgegeben.
- **2026-11-17** — gegrillt mit dem Vater: neun Entscheidungen (Ring zählt „eingeprägt“, offene Lernkarte kommt am
  Folgetag wieder und zählt auf die Tagesdosis, Leiter wie B-635, ans Rundenende als ganze Lernkarte, „Später
  nochmal“, ✕ ohne Rückfrage und Strafe, Pflicht bleibt der Test, keine Grenze, B-635 zuerst). Ist-Stand an
  `a28358d3` (zweiter Ring) nachgezogen; `wartet_auf: B-635`.
