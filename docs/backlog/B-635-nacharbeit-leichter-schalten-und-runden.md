---
tags: [typ/story, status/geschaetzt, bereich/klassenarbeit, rolle/student, rolle/supervisor, lerntechnik/vokabeln]
aliases: [Nacharbeit-Mechanik, leichter schalten, erledigt erst frei getippt]
status: geschaetzt
prio: P1
art: Wunsch
groesse: M
wo: beides
migration: ja
vertragsbruch: nein
quelle: B-632
grund: ""
ersetzt_durch: []
---

# B-635 · Nacharbeit: selbst leichter schalten, erledigt erst frei getippt, Runden bis alles erledigt

Teil C aus [B-632](B-632-schultest-nacharbeiten-mit-leichter-schalten.md), der Kern der Nacharbeit. Eine Position
mit der Einstellung „Nacharbeit“ spielt ihre Aufgaben in Runden. Jede Aufgabe beginnt als Freitext. Das Kind darf
sie Stufe für Stufe leichter schalten. Erledigt ist sie erst, wenn es sie frei getippt richtig hat. Offene Aufgaben
kommen in der nächsten Runde wieder.

## User Story

Als *Vater* möchte ich eine Übung als Nacharbeit in den Plan legen, damit mein Sohn jede Aufgabe so lange
wiederholt, bis er sie allein schreiben kann.

Als *Sohn* möchte ich bei einer Aufgabe, an der ich festhänge, auf eine leichtere Stufe wechseln können.

## Ist-Stand am Code

Gelesen am 2026-11-17 (`b1bf80c8`), aus B-632:

- Die Stufe wählt nur der Server: „Enforce the stage server-side (not selectable by the client)“
  (`backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:437-442`). `PositionPlayService.CardStage`
  (`backend/Pugling.Api/Services/Shared/PositionPlayService.cs:180-193`) kennt keine Anfrage des Kindes.
- Die Leiter des Lückentexts: `TranslationFreeText`, `LetterBoxes`, `LetterScramble`, `Choice`
  (`backend/Pugling.Api/Models/ClozeEntities.cs:16`, `:25`, `:30`, `:35`).
- Wiederholt wird nur die Lernkarte, und nur einmal (`PositionPracticeController.cs:694-695`, `IsRepeat` `:141`).
- Die Sitzung friert ihre Reihenfolge ein (`PracticeSession.Order`/`Cursor`,
  `backend/Pugling.Api/Models/StudyPlanEntities.cs:100-122`).

## Die echte Lücke

Das Kind kann keine Stufe wählen. Kein Zustand hält fest, dass eine Aufgabe „frei getippt erledigt“ ist, und keine
Runde wiederholt offene Aufgaben, bis sie erledigt sind.

## Entscheidungen

Aus B-632:

- **Entscheidung 3:** Leichter geht nur eine Stufe pro Klick, und nie zurück.
- **Entscheidung 4:** Leichter schalten kostet nichts.
- **Entscheidung 6:** Die Mechanik ist eine Einstellung an der Position, getragen zuerst vom Lückentext.

Zusätzlich aus dem Risiko-Abschnitt: Eine Aufgabe, die bis zur Auswahl leichter geschaltet wurde, kommt erst in
der **nächsten** Runde wieder, nicht in derselben.

## Akzeptanzkriterien

1. Eine Position hat die Einstellung „Nacharbeit“. Der Vater setzt sie im Positionsformular.
2. Jede offene Aufgabe beginnt als Freitext. `POST …/practice/{sessionId}/easier` schaltet die aktuelle Karte genau
   eine Stufe tiefer: Buchstabenkästen, Buchstabensalat, Auswahl. Wieder schwerer schalten geht nicht.
3. Eine Antwort auf einer leichteren Stufe zeigt, ob sie richtig war, und die Lösung. Die Aufgabe bleibt offen.
4. Eine Runde geht der Reihe nach durch alle offenen Aufgaben. Die nächste Runde enthält nur die Aufgaben, die dann
   noch offen sind.
5. Der Server führt die Stufe jeder Karte. Eine Antwort zu einer leichter geschalteten Karte erledigt die Aufgabe
   nie, auch wenn der Client sie als Freitext schickt. Ein Integrationstest belegt das, und er war vor dem Bau rot.
6. Wie oft jede Aufgabe auf welcher Stufe gelöst wurde und ob sie erledigt ist, bleibt über Tage gespeichert. Diese
   Daten liest B-637.
7. Leichter schalten kostet keine Münzen.
8. Die Arcade zeigt den Knopf „leichter“, die Runde und den Erledigt-Zustand.

## Schätzung

**M.** Ein neuer Zustand mit Anti-Cheat, ein neuer Endpunkt, Runden-Logik und die Arcade.

- **`migration: ja`:**
  - Spalte „Nacharbeit“ an `PlanPosition` (`backend/Pugling.Api/Models/PlanPositionEntities.cs:17`).
  - Die Stufe je Karte als JSON-Spalte an `PracticeSession`, mit `ValueComparer` (Tor G7).
  - „Erledigt“ und die Zähler je Stufe, entweder an `PositionItemProgress` (`PlanPositionEntities.cs:204`) oder als
    eigene Tabelle. Welche Form es wird, entscheidet der Bau.
  - Bestandszeilen bekommen neutrale Werte.
- **`vertragsbruch: nein`:**
  - additiv: das Positionsfeld, an `PracticeCard` die Kennzeichen „leichter möglich“ und „erledigt“
  - neu: der Endpunkt `easier` neben `review` (`PositionPracticeController.cs:410`)
  - in `Pugling.Client` eine Zeile für den neuen Endpunkt
- **Risiken:**
  - Anti-Cheat (Akzeptanzkriterium 5).
  - Die Auswahl verrät die Lösung, darum kommt die Aufgabe erst in der nächsten Runde wieder.
  - Die Pflicht-Regel anderer Positionen darf sich nicht ändern.
- **Angriffsplan:**
  1. Migration und Probe gegen eine Kopie der echten DB.
  2. Stufe je Karte und `easier`.
  3. `Review` prüft gegen die geführte Stufe und setzt „erledigt“.
  4. Rundenaufbau aus den offenen Aufgaben.
  5. Arcade: `frontend/src/sohn/SohnPractice.tsx` und `frontend/src/components/ClozePrompt.tsx`.
  6. Die Einstellung im Positionsformular `frontend/src/vater/PlanPositions.tsx`.
- **Testweg:**
  - Neue Klasse `RemediationPositionTests` in `backend/Pugling.Api.Tests`. Sie prüft:
    - Leichter geht nur stufenweise und nie zurück.
    - Eine falsch deklarierte Antwort erledigt nichts (vorher rot).
    - Die nächste Runde enthält nur offene Aufgaben.
    - Nach der Auswahl kommt die Aufgabe nicht in derselben Runde wieder.
  - Vitest `SohnPractice.test.tsx`.
  - E2E `frontend/e2e/nacharbeit.spec.ts`: Leichter schalten, eine andere Aufgabe frei tippen, die erste kommt in
    der nächsten Runde wieder. Gezählt wird nach „N passed“.

## Verlauf

- **2026-11-17** — aus B-632 geteilt (Teil C), auf `geschaetzt` übernommen: Ist-Stand, Entscheidungen und Schätzung
  stammen aus B-632.
