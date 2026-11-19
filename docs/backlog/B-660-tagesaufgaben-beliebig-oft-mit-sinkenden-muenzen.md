---
tags: [typ/story, status/ausformuliert, bereich/gamification, rolle/student, rolle/supervisor]
aliases: [Tagesaufgabe wiederholen, Wiederholung mit sinkenden Münzen]
status: ausformuliert
prio: P2
art: Wunsch
groesse: ""
wo: beides
migration: ""
vertragsbruch: ""
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

Die Münz-Ökonomie ist Sache des Vaters. Je Frage nur eine Empfehlung, keine Entscheidung.

1. **Neuer Weg oder Bonus-Training erweitern?** Ein zweiter Münzweg neben dem Bonus-Training verdoppelt Deckel,
   Halbierung und Sperre.
   *Empfehlung:* kein zweiter Weg. Das Bonus-Training erweitern: dieselbe Übung am Knoten einer erledigten Pflicht als
   Einstieg ins Bonus-Training anbieten, und die Ausnahme „Nicht-Vokabel-Position" (Punkt 6) überprüfen.
2. **Form der Abnahme:** Halbierung, feste Stufen oder ein Wert?
   *Empfehlung:* die vorhandene Stufung 100/50/25/0 beibehalten (`RunPercent`). Ein zweites Schema für dieselbe Sache
   wäre die Kosten nicht wert.
3. **Je Position oder je Tag?** Heute zählt die Halbierung je **Übung und Tag**
   (`ExtraTrainingService.cs:191-193`), nicht je Position.
   *Empfehlung:* je Übung und Tag beibehalten. Eine Übung ist selten in mehr als einer Position desselben Kindes, und
   „je Position" erlaubte, dieselbe Übung über einen zweiten Plan zu melken.
4. **Zählt eine Wiederholung für den Leitner-Kasten?**
   *Empfehlung:* nein, wie im Bonus-Training (Punkt 7). Die Karte soll an ihrem Fälligkeitstag wieder drankommen; eine
   freiwillige Zusatzrunde darf den Abstand nicht verlängern.
5. **Der Deckel (90 Münzen am Tag) und die Sperre „erst nach den Tageszielen" bleiben?**
   *Empfehlung:* ja. Der Vater hat „solange sich das nicht ausnutzen lässt" gesagt; beide Regeln sind sein Schutz.
6. **Vorschlag: Duplikat?** B-660 deckt sich zum großen Teil mit dem Bonus-Training (B-538/B-657/B-535).
   *Empfehlung:* nicht verwerfen, aber **kürzen** auf Punkt 1 der Lücke (Erreichbarkeit vom Knoten, Ausnahme für
   Nicht-Vokabel-Positionen). Das entscheidet der Vater beim Grillen.

## Akzeptanzkriterien (Entwurf)

Je Kriterium die Änderung, die es rot macht. Entwurf; endgültig erst nach der Grill-Runde.

1. Vom Knoten einer **erledigten** Pflicht aus lässt sich dieselbe Übung noch einmal spielen, sobald die Tagesziele
   erfüllt sind. *Rot, wenn* der Knoten weiter nur in den Leerzustand führt
   (`SohnPractice.tsx:139`).
2. Eine Nicht-Vokabel-Übung, die Position eines Plans des Kindes ist, ist im Bonus-Training spielbar. *Rot, wenn*
   `knownFromPath` sie weiter ausschließt (`ExtraTrainingService.cs:318-322`).
3. Der erste Wiederholungsdurchgang zahlt 100 %, der zweite 50 %, der dritte 25 %, der vierte 0 % der Treffer-Münzen.
   *Rot, wenn* `RunPercent` (`:282-289`) geändert oder umgangen wird.
4. Nach dem vierten Durchgang lässt sich dieselbe Übung weiter spielen, ohne Münzen. *Rot, wenn* der Start bei 0 %
   abgewiesen wird (`:94-97`) – das gilt heute nur für die Vokabel.
5. Eine Wiederholung ändert weder Kasten noch Fälligkeit der Position. *Rot, wenn* der Durchgang `PositionItemProgress`
   schreibt.
6. Die Tagessumme der Wiederholungs-Münzen bleibt unter dem Deckel. *Rot, wenn* ein Weg am Deckel vorbei bucht.
7. Der Leerzustand der Üben-Sicht nennt, dass es weitergeht und was es bringt. *Rot, wenn* der Text unverändert
   „nichts fällig" meint.

## Verlauf

- 2026-11-18 · idee · aufgenommen aus dem Feedback des Vaters
- 2026-11-19 · ausformuliert (Vorbereitungslauf): am Code belegt, dass eine erledigte Pflicht-Position im Üben leer
  ist (nur Fälliges, 0 Münzen) und das Bonus-Training die verlangte Abnahme 100/50/25/0 je Übung und Tag schon hat – mit
  Ausnahme der Nicht-Vokabel-Positionen. Teil-Überschneidung mit B-538/B-657; Entscheidungen der Münz-Ökonomie stehen
  nur als offene Punkte.
