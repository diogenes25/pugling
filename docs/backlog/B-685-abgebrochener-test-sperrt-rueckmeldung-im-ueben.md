---
tags: [typ/story, status/idee, bereich/rueckmeldung, rolle/student]
aliases: [Rückmeldung nach „Später“ gesperrt]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: beides
migration: ""
vertragsbruch: ""
quelle: "Nachschau Sprint 88 in der PM-Sitzung 2026-11-19"
grund: ""
ersetzt_durch: []
entgangen_bei: [B-628]
---

# B-685 · Ein mit „Später“ verlassener Test sperrt die Rückmeldung im Üben für den ganzen Tag

Entscheidung 5 von B-623 erlaubt die Rückmeldung „im Üben jederzeit“; nur **im** Test wartet sie bis zum Ergebnisblatt,
weil ein Knopf mitten im Test ablenkt. Die Sperre am Server prüft aber nicht, ob gerade getestet wird, sondern ob es
heute einen offenen Versuch zur Position gibt.

## Belegt am Code (2026-11-19)

- `ExerciseFeedbackService.TestRunningAsync`
  ([ExerciseFeedbackService.cs:24](../../backend/Pugling.Api/Services/Shared/ExerciseFeedbackService.cs)) ist wahr,
  sobald zur Position heute ein Versuch mit `CompletedAt == null` existiert. Benutzt wird sie in
  [ExerciseFeedbackController.cs:67](../../backend/Pugling.Api/Controllers/Student/ExerciseFeedbackController.cs).
- Der Testbildschirm legt den Versuch beim Öffnen an
  ([SohnTest.tsx:121](../../frontend/src/sohn/SohnTest.tsx)), und „Später“ führt nur zur Startseite. Der Versuch bleibt
  offen, mit Absicht, damit er fortgesetzt werden kann.
- Das Üben hat keine Sperre gegen einen offenen Test und zeigt den Rückmelde-Knopf
  ([SohnPractice.tsx:632](../../frontend/src/sohn/SohnPractice.tsx)).

## Szenario

1. Das Kind öffnet den Test zu Position P und tippt „Später“.
2. Es übt P und meldet eine Karte mit einem Grund.
3. Der Server antwortet `409 feedback_during_test` („Dein Test zu dieser Übung läuft noch …“).

Bis das Kind den Test abgibt, ist jede Meldung aus dem Üben dieser Position gesperrt, obwohl der Knopf angeboten wird.

## Offen (Entscheidung nötig)

AK 3 von B-628 („eine Meldung zu dessen Karten“) lässt sich positionsweit lesen. Drei Wege:

- **(a)** nur sperren, während der Testbildschirm aktiv ist; das braucht ein Signal vom Client und ist darum schwach
  für eine Server-Regel;
- **(b)** die Meldung aus dem Üben nie sperren und nur die Meldung aus dem Test (Quelle im Request) bis zum
  Ergebnisblatt aufschieben;
- **(c)** den Knopf im Üben verbergen, solange ein Test offen ist, mit einem Satz dazu.

## Verlauf

- 2026-11-19 · idee · aufgenommen aus der Nachschau von Sprint 88 (B-628/B-629); am Code bestätigt. Bewusst nicht in
  Sprint 96 behoben, weil das Sprint-Ziel (Shop je Kind) ohne den Fix erfüllt ist.
