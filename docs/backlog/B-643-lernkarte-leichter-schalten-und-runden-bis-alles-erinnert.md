---
tags: [typ/story, status/abgenommen, bereich/training, bereich/frontend, rolle/student, lerntechnik/vokabeln]
aliases: [Lernkarte leichter, Mach's mir leichter in der Lernkarte]
status: abgenommen
prio: P2
art: Wunsch
groesse: L
wo: beides
migration: ja
vertragsbruch: nein
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

## Schätzung

**L.** Gleich groß wie B-598, an dem sie ansetzt: ein neuer Zustand je Wort mit Migration über die echte DB, eine
Änderung im Kern der Kartenwahl (wann ist ein Wort neu?), eine Rundenfolge ohne Grenze, drei Abfrageformen in der
Lernkarte und der Ring. Das Fundament (Stufe je Karte, `easier`) kommt aus B-635 und ist hier nicht mitgezählt; ohne
B-635 wäre es mehr als L und müsste geteilt werden.

- **`wo: beides`** – Backend zuerst (Zustand, Kartenwahl, Dosis, Wiederholung, Ring-Zahl), dann
  `frontend/src/sohn/Lernkarte.tsx` und der Pfad.
- **`migration: ja`:**
  - Neue Spalte `ImprintedAt` (`DateOnly?`) an `PositionItemProgress`
    (`backend/Pugling.Api/Models/PlanPositionEntities.cs:204-232`, neben `IntroducedAt` `:222`): „eingeprägt“.
  - **Bestandszeilen:** Jede schon eingeführte Zeile bekommt `ImprintedAt = IntroducedAt`. Sonst kämen am Tag nach dem
    Deploy alle Wörter, die Friedrich und Theodor schon lernen, wieder als Lernkarte, und ihre goldenen Ringe fielen auf
    null. Die Lernkarte gibt es erst seit 2026-11-16 (`17808c27`); fast alle Bestandswörter sind vor ihr eingeführt.
    Ein Wort, das seitdem aufgegeben wurde, gilt damit als eingeprägt. Das ist der Preis dafür, dass niemand Stoff
    wiederholen muss, den er schon abgefragt bekommt.
- **`vertragsbruch: nein`:**
  - additiv: `PositionStatus.ImprintedCount` neben `IntroducedCount`
    (`backend/Pugling.Contracts/Shared/ProgressDtos.cs:42`). Der Server füllt es für Übungen ohne Lernkarte mit der
    Zahl eingeführter Wörter (Entscheidung 1). Das Frontend muss diese Regel dann nicht kennen.
  - Die Kennzeichen „leichter möglich“ und die Stufe der Karte bringt B-635 an `PracticeCard` mit.
- **Risiken:**
  1. **„Neu“ wird an 42 Stellen über `IntroducedAt` gelesen** (Services und Controller, gezählt am 2026-11-17). Nur die
     Kartenwahl darf auf „eingeprägt“ umstellen: `IntroducedCardsAsync` (`PositionPlayService.cs:165-168`) liefert
     `card is not null` an `IsIntroduction` (`:156`, `:187`; `PositionPracticeController.cs:121`, `:219`). Neu-Bonus
     (B-535), Kennzahlen (`MetricsService.cs:26`), Plan-Start (`PositionProgressService.cs:677`) und die Tagesdosis
     müssen bei „eingeführt“ bleiben, sonst zahlt ein Wort seinen Neu-Bonus zweimal oder der Tagesbeginn verschiebt
     sich.
  2. **Tagesdosis doppelt gezählt.** Ein offenes Wort ist schon eingeführt; `DailyTestPortion`
     (`PositionPlayService.cs:310-315`) zählt Neues über `IntroducedAt == day`. Die Rundenzusammenstellung muss offene
     Lernkarten zuerst einplanen und nur den Rest mit neuen Wörtern füllen (Entscheidung 2, AK 5).
  3. **Wiederholung ohne Grenze in der eingefrorenen Folge.** `IsRepeat` (`PositionPracticeController.cs:141-142`)
     erkennt das zweite Vorkommen; das Anhängen (`:694-695`) hat heute die Grenze `< 2`. Ohne Grenze wächst `Order`
     mit jedem Fehlversuch. Der Lernrunden-Teil der Pflicht (`PlayedEnough`, `PositionProgressService.cs:124-129`)
     misst Cursor gegen `Order.Count`. Bei Vokabeln zählt dafür der Test (Entscheidung 7), aber an einem reinen
     Anzeigetag läse dieselbe Regel eine wachsende Folge.
  4. **Anti-Cheat.** Eine leichter geschaltete Karte darf nie `ImprintedAt` setzen, auch nicht über eine Antwort im
     zweiten Tab oder ein doppelt geschicktes Review. Der Schutz aus B-598 gegen den zweiten Tab
     (`PositionPracticeController.cs:455-461`) gilt für die Stufe, nicht für „eingeprägt“.
  5. **Echte DB.** Die Migration läuft auf der `pugling.db`, mit der die Söhne lernen. Probe zuerst gegen eine Kopie.
- **Angriffsplan** (nach B-635):
  1. Migration `ImprintedAt` mit Backfill aus `IntroducedAt`; Probe gegen eine Kopie der echten DB (Ringe vorher und
     nachher gleich).
  2. `LearnCardReviewAsync` (`PositionPracticeController.cs:663-711`) setzt `ImprintedAt` nur bei richtiger Antwort im
     freien Feld auf der von B-635 geführten Freitext-Stufe.
  3. Die Kartenwahl spielt ein eingeführtes, nicht eingeprägtes Wort wieder als Lernkarte: `IntroducedCardsAsync`
     oder ein eigener Abruf liefert „eingeprägt“ an `IsIntroduction`.
  4. Wiederholung ohne Grenze: Leichter geschaltet, „Später nochmal“ und falsch hängen das Wort ans Rundenende;
     `StageAt` (`:148-156`) spielt jede Wiederholung als ganze Lernkarte.
  5. Tagesdosis: offene Lernkarten zuerst, neue Wörter nur bis zur Dosis.
  6. `ImprintedCount` in `PositionStatus` (`PositionProgressService.cs:244` daneben), Client, OpenAPI.
  7. Frontend: `Lernkarte.tsx` Schritt 4 mit „Mach's mir leichter“, den drei Formen (`LetterBoxes.tsx`,
     `LetterTiles.tsx`, Auswahl) und „Später nochmal“; der goldene Ring liest `imprintedCount`
     (`frontend/src/sohn/pfad.ts:47-50`), Beschriftung „N von M eingeprägt“ (`frontend/src/i18n/de.ts:39`, `en.ts`).
- **Testweg:**
  - **`LernkarteTests`** (`backend/Pugling.Api.Tests/LernkarteTests.cs`) erweitern, je Fall mit der Änderung, die ihn
    rot macht:
    - leichter geschaltet, dann Freitext geschickt → nicht eingeprägt (rot, wenn `Review` die geführte Stufe nicht
      prüft);
    - dritte Wiederholung desselben Worts in einer Runde (rot mit der heutigen Grenze `< 2`);
    - abgebrochene Runde → am nächsten Tag kommt das Wort wieder als Lernkarte (rot mit dem heutigen
      `IntroducedAt`-Gate);
    - 6 neue pro Tag, 2 offene von gestern → 2 offene und 4 neue (rot, wenn offene nicht angerechnet werden);
    - Neu-Bonus wird für ein wiederkehrendes offenes Wort nicht zweimal gezahlt (rot, wenn Risiko 1 eintritt);
    - `ImprintedCount` zählt nur eingeprägte, bei einer Übung ohne Lernkarte die eingeführten.
  - **Migrationsprobe** gegen eine Kopie der echten `pugling.db`: `ImprintedCount` gleich `IntroducedCount` je
    Position direkt nach der Migration.
  - **`DosisResteTests`** grün lassen (die Dosis-Regel für Abfragen bleibt).
  - **Vitest:** `Lernkarte.test.tsx` (Knopf schaltet, „Später nochmal“, keine Wahl der Stufe im Client),
    `SohnHome.test.tsx` (goldener Ring aus `imprintedCount`, Beschriftung).
  - **E2E:** `frontend/e2e/erstbegegnung.spec.ts` erweitern: neue Karte, bis zur Auswahl leichter schalten, nächste
    Karte, das erste Wort kommt am Rundenende als ganze Lernkarte wieder, frei erinnern, Ring „1 von N eingeprägt“.
    Gezählt wird nach „N passed“, nicht nach dem Exit-Code.
  - Reviewer: `pugling-reviewer`, dann `frontend-reviewer`; Rollengang als Sohn an der laufenden App.

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
- **2026-11-17** — geschätzt: L, beides, Migration ja (`ImprintedAt`, Backfill aus `IntroducedAt`), kein
  Vertragsbruch (`ImprintedCount` additiv). Größtes Risiko: Nur die Kartenwahl darf „neu“ auf „eingeprägt“ umstellen,
  die übrigen 42 Leser von `IntroducedAt` nicht.
- **2026-11-17** — gebaut (Sprint 92, nach „Ja, weitermachen“ des Vaters). Migration `ImprintedAt` mit Backfill
  `ImprintedAt = IntroducedAt`; Probe an einer **Kopie** der echten `pugling.db` (Stand `StudyPlanSeriesUnit`): beide neuen
  Migrationen liefen, 30 Positionen mit 216 eingeführten Wörtern, je Position eingeprägt = eingeführt, keine Abweichung.
  Kartenwahl über `PositionPlayService.Known` (eingeprägt – oder die Karte braucht keine Lernkarte, `NeedsImprint`, etwa die
  Erkennen-Karte einer „beide“-Vokabel); alle übrigen Leser von `IntroducedAt` unverändert (Reviewer: 44 Treffer
  geprüft). Tagesdosis zählt offene Lernkarten der Runde, auch heute eingeprägte. Vokabel-`EaseLadder`, eigene
  Typ-Eigenschaft `SupportsRemediation` (Vokabel-Positionen können nicht nebenbei Nacharbeit). Leichter geschaltet bleibt
  die Lernkarte in ihrer Form (Auswahl bietet Wörter der Lernsprache), ohne Vorlese-Ton. Wiederholung ohne Grenze.
  `PositionStatus.ImprintedCount`; Ring und Beschriftung „N von M eingeprägt“; „Später nochmal“.
  - **Abweichung, hergeleitet aus Entscheidung 1:** ein im Test frei getipptes, richtiges Wort ist „einmal frei aus dem
    Gedächtnis richtig geschrieben“ und gilt als eingeprägt; eine richtige Auswahl im Test nicht. Ohne diese Regel käme
    jedes im Test kalt getroffene Wort für immer als Lernkarte. **Vom Vater bestätigt (2026-11-17).**
  - **Verworfen (Review-Fund 3):** den Tagestest um offene Lernkarten zu kürzen brach B-04 – wer nie übt, muss im Test
    jeden Tag mehr Wörter treffen (`VierWochenTests.Dosis_WerNieUebt…` fiel: 5/5/5 statt 5/10/15). Kalt getestete Wörter
    kommen am Folgetag als Lernkarte und nehmen dort ihren Dosis-Platz; das Tempo bleibt die Dosis.
  - AK 1 → `Lernkarte_Wird_Im_Erinnern_Stufe_Fuer_Stufe_Leichter`; Vitest `Lernkarte` (Knopf nur in Schritt 4 mit `canEase`).
  - AK 2/3 → `Leichter_Erinnert_Praegt_Nicht_Ein_Frei_Erinnert_Schon`, `Doppelt_Geschickte_Hilfe_Antwort_Praegt_Nicht_Ein`,
    `Antwort_Fuer_Eine_Vergangene_Stelle_Praegt_Nicht_Ein` (Risiko 4). Vor dem Bau rot: weder `easier` für die Lernkarte
    noch `ImprintedAt` existierten.
  - AK 4 → `Nicht_Erinnertes_Wort_Kommt_Wieder_Bis_Es_Sitzt` (dritte und vierte Wiederholung; mit der alten Grenze `< 2` rot).
  - AK 5 → `Nicht_Eingepraegtes_Wort_Kommt_Am_Naechsten_Tag_Als_Lernkarte`, `Offene_Lernkarten_Zaehlen_Auf_Die_Tagesdosis`
    (2 offen + 4 neu), `Zweite_Runde_Am_Selben_Tag_Bringt_Keine_Neuen_Ueber_Die_Dosis`.
  - AK 6 → `Ring_Zaehlt_Eingepraegte_Woerter`, Vitest `pfad` und `SohnHome` (Blatt und Vorlesename; Gegenprobe von Hand:
    mit `introducedCount` 1 failed, sonst 27/27).
  - AK 7/8 → Bestandstests Test/Bonus/Info grün; `Wiederkehrende_Lernkarte_Zahlt_Keinen_Neu_Bonus`; Test-Pfad:
    `Frei_Getippt_Im_Test_Praegt_Ein`, `Gewaehlt_Im_Test_Praegt_Nicht_Ein`.
  - Nachgezogen (setzten „bekannt = gesehen“ voraus): `TestApi.MarkIntroduced`, Erstbegegnung-, UmfangNeu-, Kartenstufe-,
    VierWochen-Tests; `KartenstufeTests.Test_Fuehrt_Ungesehene…` erwartet jetzt die Lernkarte nach einem kalten Test.
  - E2E `lernkarte-leichter.spec.ts` (Rollengang Sohn) **1 passed**; volle Suite 169 passed, 1 failed
    (`vater-von-null.spec.ts`, einzeln **1 passed** – Last durch einen parallelen Testlauf).
- **2026-11-17** — abgenommen (`e10cdd34`). Der Vater bestätigte die hergeleitete Regel „im Test frei getippt richtig = eingeprägt“. Gerätecheck am Handy (Gefühl der Leiter in Schritt 4) steht aus; bei den Söhnen wirksam erst nach einem neuen Publish des Heimnetz-Servers.
