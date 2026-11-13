---
tags: [typ/story, status/ausformuliert, bereich/api, bereich/frontend, rolle/supervisor, rolle/creator, rolle/student]
aliases: [Aufbau-Übungen, Vorstufen, Lernweg zum Stoff, Scaffolding]
status: ausformuliert
prio: P1
art: Wunsch
groesse: ""
wo: beides
migration: ""
vertragsbruch: ""
quelle: "Nutzer im Chat 2026-11-13: Friedrichs Stoff ist zu schwer – es muss einen Weg geben, Übungen auszuwählen, die zum gewünschten Level aufbauen; statt zu versagen ein Weg mit Übungen zum Erlernen des aktuellen Stoffes"
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-536 · Aufbau-Übungen: ein Weg zum aktuellen Stoff statt Versagen daran

Friedrichs Plan stellt ihm den Stoff der Unités 1–2 direkt in Zielschwierigkeit. An der Grammatik scheitert er
(Position 44 „quel, quelle, quels, quelles": 8 Antworten, 0 richtig, 219 s). Der Vater will Übungen auswählen
können, die **zum gewünschten Level hinführen** – erst leichtere Vorstufen, dann der eigentliche Stoff –, damit
das Kind auf dem Weg Erfolge (und Münzen, siehe [B-535](B-535-belohnungen-erreichen-friedrich-nicht.md)) hat statt
nur Misserfolge.

## User Story

Als *Vater* möchte ich zu einer zu schweren Übung leichtere Aufbau-Übungen auswählen (oder erzeugen lassen), die
mein Kind zum Ziel führen, damit es den aktuellen Stoff lernt statt daran zu scheitern. Als *Sohn* möchte ich
Aufgaben bekommen, die ich gerade so schaffe.

## Ist-Stand am Code

- **Vokabeln haben schon einen Aufbau**: die Leiter Kennenlernen → Karteikarte → Auswahl → Tippen je Wort
  (B-96, B-476; `PositionPlayService.cs:118-158`, `LadderDays = 7`). Bei Friedrich trägt sie: Tests 11 × 100 %.
- **Grammatik/Cloze/Matching haben keinen**: eine Übung hat genau eine Schwierigkeit. Das Modell `Exercise` kennt
  `GradeMin/GradeMax`, `SchoolTypes`, `DefaultStage` – aber kein Feld für Schwierigkeit, Vorstufe oder
  „baut auf auf". Ein Lehrplan ist eine flache Liste von Positionen ohne Reihenfolge-Abhängigkeit.
- In Plan 6 hängen neun Grammatik-Positionen als **Wochenziele** gleichzeitig offen; keine ist gegenüber einer
  anderen vor- oder nachgelagert.
- Der KI-Creator kann Übungen zu einer Unit erzeugen (`plan`, `create --child 3`), kennt aber keine Stufung
  „leichter als Übung X".
- Verwandt: [B-529](B-529-erklaerung-nach-der-antwort.md) (Erklärung nach der Antwort – zeigt, *warum* falsch),
  [B-524](B-524-vokabeltest-wie-in-der-schule.md) (Testformat der Schule = das Ziel-Level bei Vokabeln),
  [B-533](B-533-bonus-training-extra-muenzen.md) (Bonus-Training: Pool aus Übungen der aktuellen Units).

## Die echte Lücke

Es fehlt eine **Stufung innerhalb eines Themas** jenseits der Vokabel-Leiter: Welche Übung ist eine Vorstufe welcher
anderen, und wann schaltet der Plan von der Vorstufe auf die Ziel-Übung um? Heute kann der Vater nur Übungen
hinzufügen oder entfernen – den Weg dazwischen kann er nicht ausdrücken.

## Offene Punkte

1. **Wie wird „baut auf" ausgedrückt?** (a) Positionen eines Plans bekommen eine Reihenfolge-Sperre („Position B
   erst, wenn A bestanden") – ein Lernpfad je Thema; (b) eine Übung trägt im Katalog eine Stufe (z. B. 1 = erkennen,
   2 = einsetzen mit Auswahl, 3 = frei bilden) und der Plan führt eine Gruppe von Stufen als *eine* Position mit
   eigener Leiter, wie bei Vokabeln; (c) beides. *Empfehlung:* (b) – es überträgt die bewährte Vokabel-Leiter auf
   Grammatik und hält den Plan flach; der Vater wählt das Thema und das Ziel-Level, nicht jede Einzelübung.
2. **Wer liefert die Vorstufen?** Der Vater per Hand aus dem Katalog, oder der KI-Creator erzeugt zu einer Ziel-Übung
   2–3 leichtere Varianten (Auswahl statt frei, eine Form statt vier). *Empfehlung:* KI-Creator erzeugt, Vater wählt
   aus – sonst bleibt der Katalog leer.
3. **Wann steigt das Kind auf, wann ab?** Aufstieg nach bestandenem Test der Stufe; Abstieg nach N Fehlversuchen
   (z. B. 2 Tests unter 50 %) zurück auf die Vorstufe statt weiter Malus. *Empfehlung:* beides, Abstieg ohne Malus.
4. **Zählt eine Vorstufe fürs Pflichtziel?** *Empfehlung:* ja – das Ziel der Position ist die aktuelle Stufe, nicht
   die Endstufe. Sonst bleibt die Woche gesperrt (B-535, Punkt 1).
5. Soll die Selbsteinschätzung „zu schwer" vom Kind kommen dürfen (Knopf), oder entscheidet nur der Vater/die
   Messung? *Empfehlung:* nur Messung + Vater – ein Knopf „zu schwer" ist ein Ausweg aus der Pflicht.

## Verlauf

- **2026-11-13** — angelegt und ausformuliert aus Friedrichs erstem Feedback; Ist-Stand am Code und an einer
  Kopie der echten DB (Plan 6) belegt. `art: Wunsch` – die offenen Punkte gehören ins Grillen.
