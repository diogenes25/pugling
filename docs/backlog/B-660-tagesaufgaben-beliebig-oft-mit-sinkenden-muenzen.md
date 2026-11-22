---
tags: [typ/story, status/in-arbeit, bereich/gamification, rolle/student, rolle/supervisor]
aliases: [Tagesaufgabe wiederholen, Wiederholung mit sinkenden Münzen]
status: in-arbeit
prio: P2
art: Wunsch
groesse: M
wo: frontend
migration: nein
vertragsbruch: nein
quelle: "Vater am 2026-11-18, Feedback nach der ersten echten Runde mit Friedrich"
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-660 · Tagesaufgaben beliebig oft spielen, die Münzen sinken je Wiederholung bis 0

Friedrich möchte eine Tagesaufgabe nach dem Erledigen **noch einmal** machen, statt dass sie für den Tag
abgeschlossen ist. Der Vater ist einverstanden, solange sich das nicht ausnutzen lässt: jede Wiederholung
zahlt weniger Münzen als die vorige, bis sie am Ende **0** zahlt – üben darf er dann weiter, so oft er will.

## User Story

Als **Schüler** möchte ich eine erledigte Tagesaufgabe am selben Tag noch einmal spielen, **damit** ich mehr üben kann,
wenn ich Lust habe. Als **Vater** möchte ich, dass jede Wiederholung weniger Münzen zahlt als die vorige und
irgendwann nichts mehr, **damit** sich das Wiederholen nicht als Münzquelle ausnutzen lässt.

## Ist-Stand am Code

**Die erledigte Pflicht heute: sie ist nicht gesperrt, aber leer.**

- **1.** Der Pfad-Knoten einer erfüllten Pflicht bleibt ein Link (`frontend/src/sohn/SohnHome.tsx:366-371`, nur die Klasse
   `erfuellt` kommt hinzu; keine Sperre). Ein Tippen startet eine Üben-Sitzung.
- **2.** Üben (`PlayMode.Lern`) spielt **nur die heute fälligen Karten**
   (`backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:121-123`, `dueOnly: dto.Mode != PlayMode.Info`).
   Ist alles gespielt, ist nichts mehr fällig, und der Bildschirm zeigt den Leerzustand
   (`frontend/src/sohn/SohnPractice.tsx:139`, `due.length === 0 ? "empty"`, Phase `empty` bei `:434`). Das ist die
   „Abgeschlossenheit", die Friedrich erlebt.
- **3.** Gezahlt wird je Karte nur, wenn sie **fällig und heute noch nicht gewertet** ist
   (`PositionPracticeController.cs:674-680`, `due`, `alreadyScoredToday`, `scored`); eine Position ohne Kasten stempelt den
   Tag und zahlt je Wort einmal am Tag (`:750-752`). Eine zweite Runde derselben Karte zahlt also **0**, nicht weniger.
- **4.** Den freien Modus `PlayMode.Info` (ganzer Pool, ohne Fälligkeit) gibt es am Server
   (`PositionPracticeController.cs:121-123`) und im Vertrag (`frontend/src/lib/contract.ts:28280-28285`). Eine
   Verwendung in der Sohn-App: **nicht gefunden** (`grep "Info"` unter `frontend/src/sohn` ohne Treffer). Er zahlt auf
   einer Position mit Kasten nichts (`due` ist falsch).

**Was „noch einmal gegen sinkende Münzen" heute schon gibt: das Bonus-Training (nicht für jede Aufgabe).**

- **5.** `ExtraTrainingService` zahlt je abgeschlossenem Durchgang **derselben Übung am selben Tag** 100, 50, 25, dann 0 %
   (`backend/Pugling.Api/Services/Student/ExtraTrainingService.cs:282-289`, `RunPercent`; Zählung `:191-193`); am
   nächsten Tag wieder voll. Das ist genau die verlangte Abnahme, in Stufen. Dazu ein Tagesdeckel
   (`ExtraTrainingOptions.DailyCoinCap = 90`, `backend/Pugling.Api/Services/Student/ExtraTrainingOptions.cs:14`).
- **6.** Bedingungen dort: das Bonus-Training **öffnet erst nach den Tageszielen** (`ExtraTrainingService.cs:12-18`,
   `GateAsync` `:258-270`, Sperrgrund `duty_open`). **Nicht-Vokabel-Übungen, die eine Position eines Plans des Kindes
   sind, sind ausgenommen** („the child knows it from the path", Kommentar `:21-24`, `knownFromPath` `:318-322`); eine
   Übung mit fester Form und 0 % wird nicht mehr gestartet (`:94-97`, `extra_exercise_done`). Eine
   Vokabelübung darf weiter, auch bei 0 Münzen (`:94-97`).
- **7.** Der Durchgang ist **vom Plan unabhängig**: er bewegt keinen Kasten und kein Plan-Ziel
   (`backend/Pugling.Api/Models/GamificationEntities.cs:121-122`). Zählt eine Wiederholung für den Leitner-Kasten? Im
   Bonus-Training: nein.
- **8.** [B-657](B-657-kuer-uebungen-zahlen-muenzen.md) (abgenommen) lässt jede Übung Münzen zahlen, auch freie Positionen;
   [B-538](B-538-bonus-muenzregeln-form-und-degression.md) (abgenommen) hat die Halbierung eingeführt.

## Die echte Lücke

Schmaler als die Idee. Die **Abnahme-Form** („Halbierung, feste Stufen") und der **Münzweg danach** gibt es als
Bonus-Training. Offen bleibt genau der Teil, der Friedrichs Erlebnis ausmacht:

1. **Eine erledigte Pflicht-Position, die keine Vokabelliste ist** (Lückentext, Birkenbihl, Rechnen, Liste), ist im
   Bonus-Training ausgespart (Punkt 6) und im Üben leer (Punkt 2). Sie lässt sich also nicht noch einmal spielen.
2. **Eine erledigte Vokabel-Pflicht** lässt sich über das Bonus-Training wiederholen, aber nur **nach allen Tageszielen**
   und über einen eigenen Einstieg, nicht vom Knoten der Aufgabe aus. Friedrich tippt den Knoten an und findet den
   Leerzustand (Punkt 2).
3. Der Leerzustand sagt dem Kind nicht, dass es *weiterspielen* darf und was das bringt (Text nicht geprüft:
   Übersetzungsschlüssel für `practice.empty*` nicht gelesen).

Es ist möglich, dass die Story zu großen Teilen durch eine **Verbindung** vom Knoten zum Bonus-Training erledigt ist
(Frage 1). Ob sie dann als eigene Story stehen bleibt, ist ein Vorschlag, keine Entscheidung dieses Laufs.

## Offene Punkte

Alle Punkte sind unten entschieden (durchgestrichen, mit Verweis auf die Entscheidung).

1. ~~**Neuer Weg oder Bonus-Training erweitern?** Ein zweiter Münzweg neben dem Bonus-Training verdoppelt Deckel,
   Halbierung und Sperre.
   *Empfehlung:* kein zweiter Weg. Das Bonus-Training erweitern: dieselbe Übung am Knoten einer erledigten Pflicht als
   Einstieg ins Bonus-Training anbieten, und die Ausnahme „Nicht-Vokabel-Position" (Punkt 6) überprüfen.~~ → Entscheidung 1 und 2.
2. ~~**Form der Abnahme:** Halbierung, feste Stufen oder ein Wert?
   *Empfehlung:* die vorhandene Stufung 100/50/25/0 beibehalten (`RunPercent`). Ein zweites Schema für dieselbe Sache
   wäre die Kosten nicht wert.~~ → Entscheidung 5.
3. ~~**Je Position oder je Tag?** Heute zählt die Halbierung je **Übung und Tag**
   (`ExtraTrainingService.cs:191-193`), nicht je Position.
   *Empfehlung:* je Übung und Tag beibehalten. Eine Übung ist selten in mehr als einer Position desselben Kindes, und
   „je Position" erlaubte, dieselbe Übung über einen zweiten Plan zu melken.~~ → Entscheidung 5.
4. ~~**Zählt eine Wiederholung für den Leitner-Kasten?**
   *Empfehlung:* nein, wie im Bonus-Training (Punkt 7). Die Karte soll an ihrem Fälligkeitstag wieder drankommen; eine
   freiwillige Zusatzrunde darf den Abstand nicht verlängern.~~ → Entscheidung 5.
5. ~~**Der Deckel (90 Münzen am Tag) und die Sperre „erst nach den Tageszielen" bleiben?**
   *Empfehlung:* ja. Der Vater hat „solange sich das nicht ausnutzen lässt" gesagt; beide Regeln sind sein Schutz.~~ → Entscheidung 5.
6. ~~**Vorschlag: Duplikat?** B-660 deckt sich zum großen Teil mit dem Bonus-Training (B-538/B-657/B-535).
   *Empfehlung:* nicht verwerfen, aber **kürzen** auf Punkt 1 der Lücke (Erreichbarkeit vom Knoten, Ausnahme für
   Nicht-Vokabel-Positionen). Das entscheidet der Vater beim Grillen.~~ → Entscheidung 6.

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-20 (Sonnet) auf Anweisung des Nutzers (Empfehlung übernommen), unbestätigt.*

**Querfrage mit [B-218](B-218-mischwiederholung-ueber-alle-positionen.md) (dort Entscheidungen 1, 2 und 7, gleiche Antwort):**
Es gibt **einen** Münzweg für Zusatzrunden, das Bonus-Training (`ExtraTrainingService`), und **einen** Tagesdeckel
(`Child.BonusDailyCoinCap`, sonst `DailyCoinCap` = 90). Eine **Wiederholung** (diese Story) bewegt keinen Kasten; eine
**Mischwiederholung** fälliger Karten (B-218) bewegt den Kasten der Herkunftsposition, weil sie die Leitner-Wiederholung
selbst ist. **Überschneidung mit abgenommenen Entscheidungen, nicht geändert:** B-533 Entscheidung 6 („Bekanntes fliegt raus“),
7 („unabhängig vom Plan“), 1 und 3 (Sperre, Deckel) sowie B-538 Entscheidung 6 und 7 (Halbierung je Übung und Tag, Deckel 90).

1. **Kein zweiter Münzweg: die Wiederholung ist das Bonus-Training.** *Begründung:* Das Bonus-Training zahlt die verlangte
   Abnahme schon (`RunPercent` 100/50/25/0 je Übung und Tag, `ExtraTrainingService.cs:282-289`), mit Deckel und Sperre. Ein
   zweites Schema für dieselbe Sache wäre die Kosten nicht wert. Die Position zahlt eine Karte ohnehin nur einmal je Tag
   (`PositionPracticeController.cs:674-680`), eine zweite Runde im Üben zahlt also 0. *Kosten:* Wiederholen mit Münzen
   verlangt, dass die Tagesziele erfüllt sind (`duty_open`). *Verworfen:* Münzen je Wiederholung im Üben (zwei Deckel, zwei
   Halbierungen). *Schließt aus:* Ein Kind, das erst eine von vier Pflichten gemacht hat und diese Aufgabe sofort noch einmal
   für Münzen spielen will; es kann üben (Entscheidung 3), aber nicht kassieren. Das ist gegen die User Story gerechnet gewollt:
   „solange sich das nicht ausnutzen lässt“ ist der Grund der Sperre.
2. **Der Knoten einer erledigten Pflicht führt weiter, statt in den Leerzustand.** Der Leerzustand der Üben-Sicht
   (`SohnPractice.tsx:139`, Phase `empty` `:434`) sagt, dass es weitergeht, und bietet je nach Lage: das **Bonus-Training mit
   dieser Übung** (nur wenn sie im Pool liegt, die Sperre offen ist; sonst der Satz, wie viele Tagesziele noch fehlen) und das
   **Weiter üben ohne Münzen** (Entscheidung 3). *Begründung:* Friedrich tippt den Knoten an und findet heute „nichts fällig“
   (Ist-Stand 2); das ist die erlebte Abgeschlossenheit. *Kosten:* ein Einstieg ins Bonus-Training aus der Üben-Sicht und
   neuer Text (Übersetzungsschlüssel `practice.empty*`, nicht gelesen). *Verworfen:* den Knoten direkt ins Bonus-Training
   leiten: ein Tippen soll nie ungefragt eine bezahlte Runde starten.
3. **„Weiter üben, so oft er will“ ist der vorhandene freie Modus `PlayMode.Info`.** Er spielt den ganzen Pool ohne
   Fälligkeit (`PositionPracticeController.cs:121-129`), ohne Rückmeldung (`:569`, 204), zahlt nichts, bewegt keinen Kasten,
   und zählt nicht zur Pflicht (nur `PlayMode.Lern` zählt, `PositionProgressService.cs`, `PlayedRoundMeetsGoalAsync`).
   *Begründung:* Es ist das einzige „noch einmal“, das für **jeden** Übungstyp geht, auch für die ausgesparten
   Nicht-Vokabel-Positionen. *Kosten:* Die Sohn-App benutzt den Modus bisher nirgends (Ist-Stand 4: nicht gefunden), das ist neue
   Oberfläche; wird sie größer als M, wird sie beim Schätzen eine eigene Story. *Verworfen:* gar nicht anbieten: dann hätte
   Friedrich für Lückentext und Co. keine Wiederholung. *Schließt aus:* Münzen für das Wiederholen einer
   Nicht-Vokabel-Pflicht (siehe 4).
4. **Die Ausnahme „Nicht-Vokabel-Position“ bleibt (B-533 Entscheidung 6 unverändert).** Eine Übung ohne Vokabeln, die Position
   eines Plans des Kindes ist, steht nicht im Bonus-Pool (`knownFromPath`, `ExtraTrainingService.cs:318-322`). *Begründung:*
   Eine feste Übung fragt dieselben Antworten, und die erste Wiederholung würde mit 100 % „für das Gedächtnis“ zahlen
   (Kommentar `:21-24`); genau das ist der Missbrauch, den der Vater nennt. *Kosten:* Für Lückentext, Rechnen und Co. gibt es
   **keine sinkende Münzleiter** aus der Pflicht heraus, nur das freie Üben. **Hier würde der Nutzer am ehesten anders
   entscheiden.** *Verworfen:* die Ausnahme aufheben: sie erlaubte, eine gelernte Pflicht mit 100/50/25 Prozent zu melken.
   *Gegen die User Story gerechnet:* Der Vater will „bis 0 sinken“; für diese Typen ist es von Anfang an 0. Das ist der
   ausgeschlossene Fall.
5. **Form, Zähler, Deckel, Sperre und Kasten bleiben, wie sie sind.** Stufung 100/50/25/0; Zähler **je Übung und Tag** (B-538
   Entscheidung 6; „je Position“ erlaubte, dieselbe Übung über einen zweiten Plan zu melken); Deckel je Kind übersteuerbar
   (Vorgabe 90, `ExtraTrainingOptions.cs:14`; die Story sprach von einem festen Wert); Sperre „erst nach den Tageszielen“;
   die Wiederholung bewegt weder Kasten noch Fälligkeit (B-533 Entscheidung 7), damit die Karte an ihrem Fälligkeitstag wieder
   drankommt. *Kosten:* keine neue Regel, kein neuer Code. *Schließt aus:* eine freiwillige Zusatzrunde, die den Abstand der
   Karte verlängert.
6. **Die Story wird auf die Erreichbarkeit gekürzt, nicht verworfen.** Sie deckt sich mit B-533/B-538/B-657 bis auf
   Entscheidung 2 und 3 (Knoten, Leerzustand, freies Üben). *Kosten:* kleinere Story (voraussichtlich S bis M). *Verworfen:*
   als Duplikat verwerfen: der erlebte Fall „Knoten führt ins Leere“ bleibt sonst ohne Eigentümer.

## Akzeptanzkriterien

Je Kriterium die Änderung, die es rot macht. „Bestehendes Verhalten“ heißt: der Ausgangszustand erfüllt es schon; das
Kriterium ist eine Absichtserklärung und fällt nur, wenn jemand die genannte Regel aufhebt.

1. Der Leerzustand einer **erledigten Vokabel-Pflicht** bietet bei offener Sperre das Bonus-Training mit **dieser** Übung
   und das freie Üben an, bei geschlossener Sperre den Satz, wie viele Tagesziele fehlen. *Rot, wenn der Leerzustand wie
   heute nur den Text zeigt (`SohnPractice.tsx:139`). Vitest an `SohnPractice` mit einer Position ohne Fälliges.*
2. Der Text des Leerzustands nennt, dass es weitergeht und was es bringt (Münzen im Bonus-Training, keine beim freien Üben).
   *Rot, wenn der Text unverändert „nichts fällig“ meint.*
3. Das **freie Üben** nach einer erledigten Pflicht spielt den ganzen Pool, ohne Münzen, ohne Kasten, ohne Wirkung auf die
   Pflicht. *Rot, wenn der Durchgang `PositionItemProgress`, einen `ChildPointsEntry` oder eine zählende `PracticeSession`
   (`Mode = Lern`) schreibt. API-Test: Info-Sitzung, Antwort `204`, Kontostand und `DueOn` vor und nach gleich.*
4. **Bestehendes Verhalten:** Eine Nicht-Vokabel-Übung, die Position eines Plans des Kindes ist, bleibt aus dem Bonus-Pool
   (B-533 Entscheidung 6). *Rot, wenn jemand `knownFromPath` (`ExtraTrainingService.cs:318-322`) entfernt.*
5. **Bestehendes Verhalten:** Stufung 100/50/25/0 je Übung und Tag, danach spielt eine Vokabelübung weiter ohne Münzen.
   *Rot, wenn `RunPercent` (`:282-289`) geändert oder der Start bei 0 % für eine Vokabelübung abgewiesen wird (`:94-97`).*
6. **Bestehendes Verhalten:** Die Tagessumme bleibt unter dem Deckel des Kindes. *Rot, wenn ein Weg am Deckel vorbei bucht
   (`CoinsTodayAsync`, `:299`).*
7. Eine Bonus-Runde auf einer Übung, die auch Position ist, ändert weder Kasten noch Fälligkeit dieser Position (B-533
   Entscheidung 7). *Rot, wenn der Durchgang `PositionItemProgress` schreibt.*

## Schätzung

**Größe: M** – wie der Batch-Pfad im `MediaSelector` (B-03): eine Oberfläche mit mehreren Zweigen, kein Schema. Nicht S,
weil der Leerzustand drei Lagen unterscheiden muss (Sperre offen und Übung im Pool, Sperre zu, Übung nicht im Pool) und das
freie Üben (`PlayMode.Info`) in der Sohn-App bisher keine Oberfläche hat.

Nachgesehen am 2026-11-20:

- **`wo: frontend`.** Die Stelle, an der heute nichts steht, ist `SohnPractice.tsx` (Phase `empty`, `:434-448`). Produktiver
  Backend-Code ändert sich nicht (Entscheidungen 1, 4 und 5 sagen „bleibt, wie es ist“). Das Backend bekommt nur Tests, die
  vorhandenes Verhalten festnageln (AK 3 bis 7); sie kommen **zuerst**, weil sie den Boden für die Oberfläche zeigen.
- **`migration: nein`.** Die Info-Sitzung schreibt nur eine `PracticeSession` mit `Mode = Info`, die Bonus-Runde nur
  `ExtraTrainingRun`; beide Tabellen gibt es. Kein neues Feld.
- **`vertragsbruch: nein`.** `StartPracticeDto(Day, Mode)` kennt `Mode` schon (`Pugling.Contracts/Student/PracticeDtos.cs:44`),
  und `ExtraTrainingOverview` liefert `Unlocked`, `Outstanding` und `Exercises` (`ExtraTrainingDtos.cs:28`). Der Client
  schickt heute einen leeren Rumpf (`frontend/src/lib/api.ts:695-696`); ein zusätzlicher Parameter ändert den Vertrag nicht.
- Der Weg ins Bonus-Training steht: Route `bonus/:exerciseId` (`frontend/src/sohn/SohnApp.tsx:184`), Start über
  `api.startExtraRun` (`SohnBonus.tsx:189`).

### Risiken

1. **Info-Modus ohne Oberfläche.** Server: Info liefert den ganzen Pool (`PositionPracticeController.cs:129`), auf einer
   Lernkarten-Stufe wird die Anzeigestufe gewählt (`PositionPlayService.cs:206`), `review` antwortet `204` ohne Rückmeldung
   (`:569`). Offen: Wie verhält sich eine **getippte** Stufe im Info-Modus, wenn nichts zurückkommt? Zeigt der Bildschirm dann
   die Lösung, oder tippt das Kind ins Leere? Das ist die Stelle, an der M zu L werden kann; dann wird das freie Üben eine
   eigene Story (Zerlegen beim Bauen, nicht jetzt).
2. **Die `exerciseId` der Position kennt `SohnPractice` nicht** (keine Fundstelle in der Datei); sie kommt aus der
   Plan-Übersicht oder muss mitgeliefert werden. Im zweiten Fall wäre es ein **additives** Vertragsfeld.
3. **Fremde, ungecommittete Arbeit:** `SohnPractice.tsx` und `SohnApp.tsx` tragen am 2026-11-20 uncommittete Änderungen
   (LearningDad). Erst bauen, wenn diese Arbeit committet ist, sonst Konflikt im selben Bildschirm.
4. **Zusammenspiel mit der Pflicht:** Das freie Üben darf die Tagespflicht nicht erfüllen (nur `Mode = Lern` zählt,
   `PositionProgressService.cs`, `PlayedRoundMeetsGoalAsync`); der Test `InfoModus_ErfuelltDasTagesziel_Nicht` deckt das.
5. **Teilung mit B-218:** beide hängen am Bonus-Training; Reihenfolge siehe unten.

### Angriffsplan

1. Backend-Tests zuerst (kein Produktivcode): AK 3 (Info bucht weder `ChildPointsEntry` noch `PositionItemProgress` noch eine
   Lern-Sitzung, Kontostand und `DueOn` gleich), AK 7 (Bonus-Runde auf einer Übung, die auch Position ist, lässt
   `PositionItemProgress` unberührt).
2. Client: `startSession` bekommt den Modus als Parameter (Standard `Lern`).
3. `SohnPractice`: im Leerzustand `GET extra-training` lesen und drei Zweige zeigen (AK 1, 2); „Weiter üben ohne Münzen“ startet
   eine Info-Sitzung und spielt den Pool.
4. i18n-Schlüssel `practice.empty*` in Deutsch und Englisch.
5. Vitest, dann E2E, dann `frontend-reviewer`.

### Reihenfolge mit B-218

**B-660 zuerst, B-218 danach, nicht parallel.** Beide hängen am Bonus-Training. B-660 ändert dort keinen Produktivcode,
liest nur `ExtraTrainingOverview`; B-218 (jetzt B-764 bis B-766) ändert `ExtraTrainingService.CoinsTodayAsync`, die Übersicht und
`SohnBonus.tsx`. Läuft B-660 zuerst, ist die Leitung vom Knoten zum Bonus-Training bewiesen, bevor eine zweite Spielart
daneben tritt.

### Testweg

- `PositionPlayModesTests` (Info-Modus): AK 3 als neuer Fall neben `InfoModus_LiefertAlleKartenAlsBatch_UndSchreibtKeinFeedback`;
  der vorhandene Test prüft heute nur `PositionItemProgress` und `ReviewEvents`, nicht `ChildPointsEntries` und nicht den
  Kontostand. Rote Probe: ein Pfad, der im Info-Modus Münzen bucht, lässt ihn fallen.
- `ExtraTrainingTests`: AK 5 bis 7 liegen dort schon (Stufung, Deckel, `knownFromPath`); je einen Fall um AK 7 ergänzen. AK 4 bis 6 sind
  Absichtserklärungen und als solche beschriftet.
- `SohnPractice.test.tsx`: neue Fälle für den Leerzustand (Sperre offen, Sperre zu, Übung nicht im Pool). Rote Probe: der
  heutige Leerzustand zeigt in allen drei Fällen nur den Text.
- E2E: `frontend/e2e/bonus.spec.ts` um den Weg „erledigte Pflicht, Knoten antippen, Bonus-Training starten“ erweitern oder
  eine Spec `wiederholen-nach-pflicht.spec.ts` anlegen; Vorbild ist `wiederholknopf-bleibt.spec.ts`. Das ist der Rollengang als Sohn.
- Vor der Abnahme: `frontend-reviewer`.

## Verlauf

- 2026-11-18 · idee · aufgenommen aus dem Feedback des Vaters
- 2026-11-19 · ausformuliert (Vorbereitungslauf): am Code belegt, dass eine erledigte Pflicht-Position im Üben leer
  ist (nur Fälliges, 0 Münzen) und das Bonus-Training die verlangte Abnahme 100/50/25/0 je Übung und Tag schon hat – mit
  Ausnahme der Nicht-Vokabel-Positionen. Teil-Überschneidung mit B-538/B-657; Entscheidungen der Münz-Ökonomie stehen
  nur als offene Punkte.
- 2026-11-20 · gegrillt (Vorbereitungslauf, Sonnet, auf Anweisung des Nutzers „Entscheide die offenen Fragen nach deiner Empfehlung“; unbestätigt): kein zweiter Münzweg, Wiederholung = Bonus-Training; Knoten und Leerzustand führen weiter, freies Üben über den vorhandenen `PlayMode.Info`; B-533 Entscheidung 6 und 7 bleiben (Nicht-Vokabel-Positionen ohne Münzleiter, keine Kastenbewegung); Story auf die Erreichbarkeit gekürzt. Querfrage mit B-218 einheitlich beantwortet.
- 2026-11-20 · geschaetzt (Vorbereitungslauf, Sonnet): M, frontend (Backend nur Tests), keine Migration, kein Vertragsbruch. Risiko: das freie Üben (`PlayMode.Info`) hat in der Sohn-App keine Oberfläche, bei getippten Stufen ohne Rückmeldung kann es L werden. Reihenfolge: vor B-218 (jetzt B-764 bis B-766). Fremde uncommittete Arbeit an `SohnPractice.tsx` und `SohnApp.tsx` abwarten.
- **2026-11-22** — in-arbeit (Nachtlauf B 2026-11-23, Sprint B3; baubar nach Freigabe 8, `geschaetzt` + Wunsch).
  **Geteilt beim Bauen, wie Risiko 1 es vorsieht:** Das freie Üben (Entscheidung 3, AK 3 und der Satz „freies Üben“ in AK 1/2)
  ist [B-821](B-821-freies-ueben-info-modus-in-der-sohn-app.md). Grund, am Code gelesen: der Info-Modus antwortet auf
  `review` mit `204`, auf einer getippten Stufe tippte das Kind ins Leere – was es dort sehen soll, geben die
  Entscheidungen nicht her (Freigabe 8: anhalten und notieren, nicht selbst entscheiden). Gebaut ist der Rest:
  `frontend/src/sohn/NochEinmal.tsx` im Leerzustand von `SohnPractice` mit drei Lagen (Sperre offen und Übung im Pool →
  Knopf in **diese** Übung mit ihrem heutigen Wert; Sperre zu → wie viele Tages- bzw. Wochenziele fehlen; nicht im Pool
  oder heute erledigte feste Übung → nichts). Die Übung der Position kommt aus der Tagesübersicht (`overview`), kein
  Vertragsfeld (Risiko 2 erledigt ohne Vertrag). Kein Backend-Produktcode.
  **AK, wie gebaut:** AK 1 ohne „freies Üben“ (B-821); AK 2 ohne „keine beim freien Üben“; AK 3 → B-821; AK 4 bis 6
  bestehendes Verhalten, von `ExtraTrainingTests` getragen (`knownFromPath`, Stufung, Deckel); AK 7 neuer Fall
  `ExtraTrainingTests.Bonus_Runde_Laesst_Den_Kasten_Der_Position_Unberuehrt` (Absichtserklärung: grün vor und nach dem Bau,
  rot, wenn die Bonus-Abgabe `PositionItemProgress` schreibt).
  **Tests:** `NochEinmal.test.tsx` (8 Fälle, je Lage), ein Fall in `SohnPractice.test.tsx` (der Block steht im Leerzustand);
  `SohnPractice.test.tsx` brauchte dazu Attrappen für `extraTraining` und `useAuth` (der Block lädt über `useAsync`). Neue
  Spec `e2e/nochmal-nach-pflicht.spec.ts` (Rollengang als Sohn: erst gesperrt mit „Noch 1 Tagesziel offen“, nach der Pflicht
  in die Bonus-Runde dieser Übung; nur die Kartenliste ist per `page.route` leer, Treffer gezählt).
  **Mutanten-Probe nach der letzten Änderung:**

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | N1 Position nicht auf ihre Übung abgebildet | NochEinmal.tsx:30 | red | red |
  | N2 heute erledigte feste Übung angeboten | :45 | red | red |
  | N3 Wert-Satz auch bei 0 % | :49 | red | red |
  | N4 Wochenziele als Tagesziele | :38 | red | red |
  | N5 Sperre ohne offene Ziele verspricht „noch 0“ | :35 | red | red |
  | N6 Block fehlt im Leerzustand | SohnPractice.tsx:446 | red | red |
  | N7 Pool ignoriert (erste Übung) | NochEinmal.tsx:43 | red | red |
  | N8 Sperre ignoriert | :32 | red | red |
  | E2 Block fehlt (E2E) | SohnPractice.tsx:446 | red | red |
- **2026-11-22** — Review (`frontend-reviewer`) und Selbst-Check, Zähler Sprint B3: 2 von 5. **Selbst-Check vor dem Review,
  gezählt:** ein voller Tagesdeckel versprach „noch 100 % wert“ – jetzt wie die Bonus-Liste (`capReached`: Vokabeln ohne
  Münzen weiter, feste Übung wartet), Satz ohne „morgen“ (galt bei Deckel 0 nie). Der Reviewer fand dasselbe als 🟡1 (einmal
  gezählt). **🟡2, gezählt:** die Sperre wurde vor dem Pool geprüft und versprach einer Nicht-Vokabel-Pflicht eine bezahlte
  Wiederholung, die nie kommt (der Server liefert den Pool auch bei Sperre) – jetzt erst der Pool. 🟢 eingearbeitet: eine
  angefangene Runde heißt „Weiterspielen“ (`openRunId`), der Takt-Satz liest `truhenZiele` wie die Bonus-Liste. 🟢5 (der
  Bonus-Knopf steht farbig über dem Test-Knopf) bleibt als Geschmacksfrage offen, ohne Story.
  **Mutanten-Probe nach der letzten Änderung** (ersetzt die Tabelle oben):

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | N1 Position nicht auf ihre Übung abgebildet | NochEinmal.tsx:31 | red | red |
  | N2 heute erledigte feste Übung angeboten | :52 | red | red |
  | N3 Wert-Satz auch bei 0 % | :56 | red | red |
  | N4 Wochenziele als Tagesziele | :43 | red | red |
  | N5 Sperre ohne offene Ziele verspricht „noch 0“ | :40 | red | red |
  | N6 Block fehlt im Leerzustand | SohnPractice.tsx:446 | red | red |
  | N7 Pool ignoriert (erste Übung) | NochEinmal.tsx:34 | red | red |
  | N8 Sperre ignoriert | :37 | red | red |
  | N9 Tagesdeckel ignoriert | :50 | red | red |
  | N10 Sperre vor dem Pool | :35 | red | red |
  | N11 angefangene Runde als neue | :60 | red | red |
  | E2 Block fehlt (E2E) | SohnPractice.tsx:446 | red | red |

  **Verifikation nach der letzten Änderung:** Vitest voll 1340/1340, `tsc -b` sauber, `ExtraTrainingTests` (Release)
  36/36.
