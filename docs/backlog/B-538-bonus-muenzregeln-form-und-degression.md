---
tags: [typ/story, status/geschaetzt, bereich/gamification, rolle/student, rolle/supervisor]
aliases: [Bonus-Münzregeln, Degression je Wiederholung, getippt zahlt drei]
status: geschaetzt
prio: P1
art: Wunsch
groesse: M
wo: beides
migration: nein
vertragsbruch: nein
quelle: B-537 (Teil a, Entscheidungen 4–7)
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-538 · Bonus-Münzregeln: Form bestimmt den Wert, Wiederholung halbiert ihn

Teil a von [B-537](B-537-lehrplan-als-kapitel-pflicht-und-kuer.md). Ist-Stand am Code (Punkte 2, 5, 6, 7) und
Begründungen stehen dort; diese Story trägt nur ihren Ausschnitt.

## User Story

Als *Sohn* möchte ich nach der Pflicht im Bonus-Training für schwerere Abfragen mehr Münzen bekommen und Übungen
wiederholen dürfen, auch wenn jede Wiederholung am selben Tag weniger bringt, damit sich zusätzliches Üben lohnt.
Als *Vater* möchte ich, dass es außer dem Bonus-Training keinen zweiten freiwilligen Münzweg gibt, damit die Pflicht
immer zuerst kommt.

## Ist-Stand am Code

Siehe [B-537 → Ist-Stand](B-537-lehrplan-als-kapitel-pflicht-und-kuer.md#ist-stand-am-code), Punkte 2, 5, 6 und 7.

## Entscheidungen

Übernommen aus B-537: **4** (Positionen ohne Pflicht zahlen 0), **5** (Auswahl 1, getippt 3), **6** (Halbierung
je Lauf am selben Tag, alle Übungsarten), **7** (Deckel-Vorgabe 90).

## Akzeptanzkriterien

1. Eine richtige Antwort in einer Plan-Position mit `Cadence.None` bucht **keine** Münzen (weder Basis noch Combo
   noch Tempo); Leitner-Stand und Lernstand bewegen sich weiter wie bisher.
2. Bonus-Training zahlt je Treffer: Auswahl 1, getippt 3, Selbsteinschätzung 0 (Vorgaben in
   `Gamification:ExtraTraining`).
3. Derselbe Übungs-Lauf am selben Tag zahlt beim 1. Mal 100 %, beim 2. 50 %, beim 3. 25 %, ab dem 4. 0 % – der
   Lauf-Wert wird abgerundet. Am nächsten Tag zahlt der erste Lauf wieder 100 %.
4. Nicht-Vokabel-Übungen bleiben nach einem Lauf im Bonus-Vorrat und sind wiederholbar (Ausnahme wie heute: was
   schon Plan-Position des Kindes ist, bleibt draußen). Die Formleiter der Vokabel-Übungen bleibt.
5. Tagesdeckel-Vorgabe 90; ein am Kind gesetzter Deckel gewinnt weiter.
6. Der Bonus-Vorrat zeigt je Übung, was der nächste Lauf heute wert ist (z. B. „50 %").
7. Die Arcade zeigt bei einer Position ohne Pflicht „ohne Münzen"; der Positions-Editor im Vater-Web blendet die
   Punkte-Felder für `Cadence.None` aus.

## Schätzung

**Größe: M** – vier kleine Regeländerungen in zwei Diensten, ein additives Vertragsfeld, zwei Stellen im
Frontend. Vergleich: in der Breite wie B-535 (M, oberer Rand), aber ohne Migration und ohne den Umbau des
Pflicht-Begriffs; mehr als `childId` aus dem Test-Pfad (B-01, S), weil Backend **und** Frontend nachziehen.

- **`migration: nein`** – die Halbierung lässt sich aus vorhandenen Zeilen rechnen: `ExtraTrainingRun` trägt
  `ChildId`, `ExerciseId`, `Day` und `CompletedAt` (`Models/GamificationEntities.cs:118-138`); „abgeschlossene Läufe
  dieser Übung heute" ist eine Zählung. Deckel und Sätze sind Konfiguration.
- **`vertragsbruch: nein`** – `ExtraExercise` (`Pugling.Contracts/Student/ExtraTrainingDtos.cs`) bekommt ein
  additives Feld für den Wert des nächsten Laufs (z. B. `NextRunPercent`); `Repeatable`/`Done` bleiben im Vertrag
  (alle Übungen werden wiederholbar, `Done` heißt künftig „heute nichts mehr wert"). Additiv ist laut README kein
  Bruch; `contract.ts` wird trotzdem neu erzeugt.

**Risiken**

- **Die Konfiguration übersteuert die Vorgaben.** `appsettings.json:35-39` setzt `DailyCoinCap: 30` und
  `CoinsPerTypedHit: 2` ausdrücklich – nur die Vorgaben in `ExtraTrainingOptions.cs` zu ändern, wirkt nicht. Beide
  Stellen ändern; der Test prüft gegen die geladene Konfiguration, nicht gegen die Klasse.
- **Zusammenspiel mit B-535.** B-535 (vorher gebaut) zahlt `NewContentPoints` beim ersten getippten Treffer eines
  neuen Worts im `PositionPracticeController`. Die Sperre für `Cadence.None` muss **vor** jeder Buchung greifen,
  auch vor dem Neu-Bonus – sonst bleibt über den Neu-Bonus ein Münzweg offen. Leitner-Stand (`ApplyReview`,
  `PositionPracticeController.cs:465`) läuft weiter.
- **Missionen/Auszeichnungen** werden durch Übung an einer Position ohne Pflicht weiter gespeist
  (`PositionTestsController.cs:42-43`). Ihre Münzen sind ein eigener, gedeckelter Weg und bleiben **bewusst**
  unberührt – im Test festhalten, damit es nicht wie ein Versehen aussieht.
- **Einmal-Übungen werden wiederholbar.** Heute fallen Nicht-Vokabel-Übungen nach einem Lauf heraus und solche, die
  Plan-Position sind, ganz (`ExtraTrainingService.cs:247-248`, `:255-256`). Die zweite Regel bleibt (AK 4); die
  erste entfällt. `FormFor` (`:269-286`) muss für wiederholte Nicht-Vokabel-Läufe dieselbe geprüfte Stufe liefern.
- **Halbierung über den Tageswechsel.** Ein Lauf wird auf den **Abgabe**-Tag gebucht (`GamificationEntities.cs:125`).
  Der Faktor wird darum bei der Abgabe aus den heute abgeschlossenen Läufen berechnet, nicht beim Start eingefroren
  – sonst zahlt ein gestern gestarteter, heute abgegebener Lauf nach dem Stand von gestern.

**Angriffsplan (Backend zuerst)**

1. `PositionPracticeController`: bei `pos.Cadence == GoalCadence.None` keine `ChildPointsEntry` (Basis, Combo,
   Tempo, Neu-Bonus); Antwort meldet 0.
2. `appsettings.json` + `ExtraTrainingOptions`: `CoinsPerTypedHit` 3, `DailyCoinCap` 90.
3. `ExtraTrainingService.SubmitAsync`: Faktor = 0,5^(heute abgeschlossene Läufe dieser Übung), ab dem 4. Lauf 0;
   `coins = min(floor(Treffer × Satz × Faktor), Rest-Deckel)`.
4. `PoolAsync`: Nicht-Vokabel-Übungen nicht mehr nach einem Lauf ausblenden; `NextRunPercent` je Eintrag; Vertrag,
   Client, `contract.ts` nachziehen.
5. Frontend: `SohnBonus.tsx` zeigt den Wert des nächsten Laufs; Arcade-Position ohne Pflicht zeigt „ohne Münzen";
   der Positions-Editor im Vater-Web blendet die Punkte-Felder bei `Cadence.None` aus.
6. Doku: `docs/tutorial-student.md` / `docs/api-examples` (Doku-Capture) prüfen; `appsettings`-Kommentar anpassen.

**Testweg**

- `Pugling.Api.Tests/ExtraTrainingTests.cs`: getippt zahlt 3/Treffer; Läufe 1–4 derselben Übung am selben Tag
  zahlen 100/50/25/0 % (abgerundet); am Folgetag wieder 100 % (`TimeProvider` vorstellen); Deckel 90 und Deckel am
  Kind gewinnt; Nicht-Vokabel-Übung nach einem Lauf weiter im Vorrat, Plan-Position weiter nicht.
- `Pugling.Api.Tests/PositionPracticeFlowTests.cs`: richtige Antwort an einer `Cadence.None`-Position bucht 0 Münzen
  und bewegt die Leitner-Box; an einer `Daily`-Position weiter Münzen (Gegenprobe, damit der Test nicht leer prüft).
- Rote Probe je Fall: Faktor fest auf 1 ⇒ Degressionstest rot; `None`-Sperre entfernt ⇒ Positionstest rot.
- E2E `frontend/e2e/bonus.spec.ts` erweitern: Übung zweimal spielen, zweiter Lauf zeigt und bucht 50 %;
  `bonus-deckel.spec.ts` auf 90 nachziehen.
- Rollengang als Sohn an der laufenden App (Kopie der DB): Pflicht erfüllen, dieselbe Übung dreimal, Münzen ablesen.
- Reviewer: `pugling-reviewer` und `frontend-reviewer` (`wo: beides`).

## Verlauf

- 2026-11-15 · `gegrillt` angelegt durch Teilung von B-537 (Entscheidung 11); Entscheidungen dort.
- 2026-11-15 · `geschaetzt`: M, beides, keine Migration, kein Vertragsbruch (additives Feld); Risiko
  `appsettings.json` übersteuert die Vorgaben, Abhängigkeit zu B-535 (Neu-Bonus).
