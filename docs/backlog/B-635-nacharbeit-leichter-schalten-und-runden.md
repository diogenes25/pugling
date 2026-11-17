---
tags: [typ/story, status/abgenommen, bereich/klassenarbeit, rolle/student, rolle/supervisor, lerntechnik/vokabeln]
aliases: [Nacharbeit-Mechanik, leichter schalten, erledigt erst frei getippt]
status: abgenommen
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
- **2026-11-17** — gebaut (Nachtlauf, Sprint 85). Migration `Remediation` (Kette verlängert): `PlanPosition.Remediation`,
  `PracticeSession.EasedStages` und `PositionItemProgress.SolvedByStage` als JSON mit `ValueComparer`,
  `PositionItemProgress.RemediationDoneAt`; Bestand neutral (`false`/`null`). Leiter am Typ (`IExerciseType.EaseLadder`,
  Lückentext: Freitext mit Übersetzung → Kästchen → Salat → Auswahl je Lücke); eine Stufe ohne eigene Form wird
  übersprungen (`PositionPlayService.EasierStage`). `POST …/easier` nur für die aktuelle Karte, `409 cannot_ease` sonst.
  `Review` liest die Stufe aus der Sitzung; „erledigt“ nur frei getippt, nur durch das Kind selbst. Eine leichter
  geschaltete Karte zahlt nichts (auch mit Leitner). Die Runde ist die Liste der offenen Aufgaben in Reihenfolge
  (`RemediationRoundAsync`); `SessionResponse.Remediation` trägt Runde/erledigt/gesamt. Der Schalter im Vater-Formular
  kommt aus der Manifest-Fähigkeit `remediation`. Arcade: „Mach's mir leichter“, Runden-Pille, Blatt „noch nicht erledigt“.
  - AK 1 → `RemediationPositionTests.Vater_schaltet_Nacharbeit_ein_nur_fuer_einen_Typ_mit_Leiter`, M15 → rot; Vitest `PlanPositionsStufe` „zeigt den Schalter nur beim Lückentext und schickt ihn“.
  - AK 2 → `Leichter_geht_eine_Stufe_je_Druck_bis_zur_Auswahl` (Start Freitext, drei Drücke, dann 409), M10 → rot; `Eine_Stufe_ohne_eigene_Form_wird_uebersprungen`, M13 → rot; `Nur_die_aktuelle_Karte_wird_leichter`, M14 → rot.
  - AK 3 + AK 5 → `Leichter_geschaltet_erledigt_nie_frei_getippt_schon` (richtig mit Lösung, `taskDone` false, kein Datum), M8/M9 → rot. Vor dem Bau rot: weder `easier` noch `taskDone` existierten (Kompilierfehler bzw. 404 am ersten Druck).
  - AK 4 + AK 6 → `Naechste_Runde_bringt_nur_die_offenen_Aufgaben` (2 von 3 offen, Runde 2, Zähler je Stufe), M11/M12/M16 → rot.
  - AK 7 → derselbe Test zählt die Buchungen vor/nach `easier` und nach der Antwort (gleich), M9 → rot.
  - AK 8 → E2E `nacharbeit.spec.ts` (leichter, Blatt „Noch nicht erledigt“, Zähler, nächste Runde Karte 1/1).
  - Wächter Nachbarn → `Ohne_Nacharbeit_bleibt_alles_wie_es_war`; `Manifest_nennt_Nacharbeit_genau_wo_eine_Leiter_ist`.
- **2026-11-17** — Review (Nachtlauf): Fünfer-Zähler von Sprint 85 überschritten (8 Funde 🟡/🔴), Lauf beendet; Funde offen, siehe `docs/pm-sitzung-2026-11-17-nachtlauf-lernkarte.md` → „Review Sprint 85 und Halt des Laufs“. Bleibt `in-arbeit`, nicht committet.
- **2026-11-17** — Review-Funde behoben (Fortsetzung nach „Ja, weitermachen“ des Vaters): Fokus und Ruhe am Leichter-Knopf, Fehleransage, Fokus nach dem Schalten, Nacharbeit fällt beim Typwechsel, toter Tastatur-Zweig, Wort-Gruppen für Screenreader, offene Nacharbeits-Runde wird fortgesetzt und `Review` nimmt nur die Karte am Cursor, `EasierStage` nie zurück auf eine gezeigte Form. Belege und Mutanten-Tabelle im Protokoll `docs/pm-sitzung-2026-11-17-nachtlauf-lernkarte.md`. Endstand: `dotnet test` 1820/1820, Vitest 1168/1168 am gestagten Stand, E2E `nacharbeit.spec.ts` + `buchstaben-ordnen.spec.ts` **2 passed**.
- **2026-11-17** — abgenommen (`29df59d4`). Rollengang: **Sohn** maschinell per E2E `nacharbeit.spec.ts` gegen frisch gestarteten Server (Ausweichports 5211/5181; leichter → Kästchen, Fokus im Feld, „Noch nicht erledigt“, Zähler, nächste Runde 1/1) und `buchstaben-ordnen.spec.ts` (Salat je Wort); **Vater** über `RemediationPositionTests` (Schalter per PATCH) und Vitest `PlanPositionsStufe` (Schalter nur beim Lückentext, fällt beim Typwechsel); **Creator** über Vitest `exerciseConfig` (Vorgabe hin und zurück) und `ClozePlayTests`. Ein Gerätecheck am Handy durch einen Menschen steht aus (Gefühl der Leiter, Lesbarkeit langer Vorgaben).
