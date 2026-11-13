---
tags: [typ/story, status/idee, bereich/training, rolle/supervisor]
aliases: [Lernkontrolle, Selbstcheck, Grammar ohne Test]
status: idee
prio: P2
art: Frage
groesse: ""
wo: "beides"
migration: ""
vertragsbruch: ""
quelle: Rollengang am Access-Lehrwerk 2026-11-10 (Kopie der pugling.db, Sohn im Handy-Format 375×812, Vater-Web)
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-469 · Prüfauftrag: Der Vokabeltest ist per Vorgabe eine Selbsteinschätzung, Grammatik und Übersetzung haben keinen Test

Der Vater will den Lernfortschritt prüfen. Im Prüfgang zeigte sich, was er dafür tatsächlich bekommt:

- **Vokabel-Position ohne `RequireTypedTest`** (Vorgabe, auch im KI-Lehrplan): Der Test läuft auf Stufe 2. Das
  Kind dreht die Karte um und tippt „Gewusst!“. Bestanden heißt dann: Das Kind hat es so gesagt.
- **Grammatik und Übersetzung** (`checkMode: None`, Knopf „Durchspielen“): Es gibt keinen Test. Die Pflicht
  gilt als erfüllt, sobald 80 % der Runde gespielt sind (`PositionProgressService.PlayedEnough`). Im
  Positions-Report sieht der Vater keine Trefferquote.
- Über die API ließ sich für die Grammatik-Position trotzdem ein Testversuch anlegen und mit 100 % bestehen
  (`POST …/positions/25/tests`); er zählt nicht für das Ziel. Die Oberfläche bietet den Weg nicht an.
- **Was gut trägt**: der Positions-Report je Wort (Box, Beherrschung, gesehen/richtig), „Heute“ mit
  Zielen und Punkten, die Kaufhistorie und die Freigabe von Einlösungen.

Fragen: Soll ein Plan, der auf eine Klassenarbeit zielt, getippte Tests zur Vorgabe machen? Sollen Grammatik und
Übersetzung prüfbar werden (die Lösungen sind eindeutig bzw. tragen Alternativen), oder soll die Oberfläche dem
Vater sagen, dass diese Positionen nur Anwesenheit messen? Abhängig von
[B-462](B-462-mehrdeutige-uebersetzung-verlangt-alle-bedeutungen.md): Getippt ist erst fair, wenn eine
Bedeutung genügt.

## Nachgeprüft am Code (2026-11-13)

- Vorgabe-Stufe jedes Typs ist `SelfAssess` (`ExerciseTypeBase.cs:37`) – ohne `RequireTypedTest` bleibt der Test
  eine Selbsteinschätzung, wie beschrieben.
- **Der Assistent** setzt `requireTypedTest` per Vorgabe auf `true` (`VaterWizard.tsx:101`, `:367`).
- **Der KI-Lehrplan** setzt seit B-463 für Vokabelübungen aus der Buch-Wortliste `RequireTypedTest: true` mit Stufe
  Freitext (`PlanPlanner.cs:286-292`); nur die übrigen, vom Modell erzeugten Positionen bekommen `null`
  (`PlanPlanner.cs:243`), also die Vorgabe. Die Behauptung „auch im KI-Lehrplan" gilt damit nur noch für diese.
- Offen bleibt der Kern: wer eine Position **von Hand** anlegt (Plan-Seite, API), bekommt die Selbsteinschätzung, und
  Grammatik/Übersetzung haben weiter keinen Test.

## Verlauf

- **2026-11-10** — angelegt (Prüfgang am Access-Lehrwerk: Übungen, Vater-Sicht, Handy).
- **2026-11-13** — nachgeprüft (Tageslauf, Restzeit): Assistent und KI-Wortlisten-Plan verlangen schon getippt; offen bleiben Handanlage und Typen ohne Test.
