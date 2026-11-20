---
tags: [typ/story, status/geschaetzt, bereich/lehrplan, bereich/training, bereich/auswertung, rolle/supervisor, rolle/student]
aliases: [Eingriffe des Vaters, Bonusaufgabe, Begründung in der Sohn-App, Etappe E4b]
status: geschaetzt
prio: P1
art: Wunsch
groesse: L
wo: beides
migration: ja
vertragsbruch: nein
quelle: B-644
grund: ""
ersetzt_durch: []
---

# B-738 · Eingriffe des Vaters, Bonusaufgaben und die Begründung in der Sohn-App (B-644, Etappe E4b)

Neunte Teilstory aus der Teilung von [B-644](B-644-tagesplaner-statt-fester-lauf.md). Der scharfe Planer
([B-736](B-736-pflicht-aus-der-tagesaufgabe.md)) bekommt seine menschliche Notbremse (Vater streicht eine nicht begonnene Aufgabe oder legt eine
eigene dazu, beides vermerkt), seinen Bonus-Weg (ein vorgeschlagenes Bonus-Training, gedeckelt durch den vorhandenen Münzdeckel) und seine
Sohn-Seite (Begründung an der Aufgabe, die eigene Kurve). Termin- und Stundenplan-Regeln liefert
[B-680](B-680-taeglicher-lehrplan-je-fach-just-in-time.md), Förder-Übungen [B-739](B-739-foerder-uebungen-und-zusammenfassung.md).

**Stand:** Eintrittsbedingung `gegrillt` aus B-644 geerbt. Entscheidungen 7 und 13 stammen vom Vater, 15, 20, 22 sind stellvertretend und
unbestätigt.

## User Story

Als *Vater* möchte ich eine Aufgabe, die das Kind noch nicht angefangen hat, streichen oder eine eigene dazulegen können, und später sehen,
dass ich eingegriffen habe, damit der Planer eine menschliche Grenze hat und seine Auswertung ehrlich bleibt. Als *Sohn* möchte ich zu jeder
Aufgabe lesen, warum sie heute dran ist, und meine eigene Entwicklung als Kurve sehen, ohne die Fehlerstatistik oder den Bruder zu sehen.

## Ist-Stand am Code

Gemessen am 2026-11-20 auf `main`. Pfade relativ zu `backend/Pugling.Api/`.

- **Bonus gibt es dreimal, ohne Planer:** Bonus-Training `ExtraTrainingRun` (`Models/GamificationEntities.cs:124`), gespielt über
  `student/children/{childId}/extra-training` (`Controllers/Student/ExtraTrainingController.cs:17`, `:38` Start, `:57` Abgabe); der Münzdeckel ist
  `Child.BonusDailyCoinCap` oder die Voreinstellung (`Services/Student/ExtraTrainingService.cs:272`), die Sperren heißen `no_plan` und `duty_open`
  (`:38`, `:40`); Tagesbox `DailyBoxClaim` (`GamificationEntities.cs:98`); Missionen (`PointKind.Mission`). Alle drei hängen an „erst die Pflicht".
- **Kein Eingriffsweg:** Es gibt keinen Endpunkt, der eine Aufgabe streicht oder ergänzt (es gibt bis B-734 noch keine Aufgabe).
- **Begonnen:** Die Quelle für „zu der Aufgabe gibt es heute eine Antwort oder Sitzung" liegt in `ItemReviewEvent` (`Models/ItemProgressEntities.cs:61`,
  mit Position und Zeit) und `PracticeSession` (`Models/StudyPlanEntities.cs:108`, `:112`).
- **Sohn-App:** `frontend/src/sohn/` (SohnApp, SohnPractice, SohnTest, Lektion …) enthält zurzeit **fremde, ungecommittete Arbeit** (LearningDad:
  `SohnApp.tsx`, `SohnPractice.tsx`, `SohnTest.tsx`, `Lektion.tsx` geändert, `DadShowcase.tsx` neu). Die Tagessicht liest die Sohn-App über
  `frontend/src/lib/api.ts:720`.
- **Die Sohn-Kurve** gibt es nicht; kein Student-Endpunkt liefert einen Verlauf der eigenen Beherrschung.

## Entscheidungen (geerbt)

1. **Der Tagesplan gilt automatisch; der Vater kann einzelne Aufgaben streichen oder eine eigene dazulegen, auch nachdem das Kind den Plan sieht;
   jeder Eingriff wird vermerkt, damit die Auswertung des Folgetags Planer-Menge und Vater-Menge unterscheidet** (B-644 Entscheidung 7, Vater).
2. **Eine Aufgabe ist begonnen, sobald es zu ihr heute eine Antwort oder Sitzung des Kindes gibt; begonnene Aufgaben sind nicht streichbar,
   unbegonnene und selbst hinzugefügte schon** (Entscheidung 20, stellvertretend, unbestätigt).
3. **Eine Bonusaufgabe ist ein vom Planer vorgeschlagenes Bonus-Training mit Begründung, gedeckelt durch `BonusDailyCoinCap`; Tagesbox und
   Missionen bleiben unverändert** (Entscheidung 15, stellvertretend, unbestätigt). Schließt aus: Bonus ohne Münzdeckel.
4. **Die Sohn-App ist bewusst schmaler: die eigene Kurve und die Begründung an der Aufgabe; keine Fehlerstatistik, kein Vergleich mit dem Bruder**
   (Entscheidung 13, Vater).
5. **Der Vater setzt Leitplanken und hat eine Notbremse** (Entscheidung 22): Streichen und Hinzufügen sind die Notbremse, keine Tagesmenge.
6. **Der Vater kann durch Streichen den Boden unterlaufen** (Entscheidung 7, Kosten): bewusste Ausnahme, kein Fehler.

### Schätz-Annahmen (stellvertretend, unbestätigt)

1. **Der Eingriff steht an der Aufgabe (`DailyTask.Herkunft`, `Streichzeit`, aus [B-734](B-734-planer-kern-und-schattenmodus.md)) und wird in der
   Auswertung des Folgetags als drei Zähler festgehalten** (`DailyEvaluation`: Planer-Aufgaben, vom Vater hinzugefügt, vom Vater gestrichen).
   Daher eine eigene kleine Migration an `DailyEvaluation`. Verworfen: die Zähler beim Lesen aus den Aufgaben zählen (die Auswertung soll stabil
   sein, [B-733](B-733-tagesauswertung-und-leistungsprofil.md) Kriterium 3).
2. **Die Bonusaufgabe ist ein `DailyTask` der Art Bonus mit einer Übung und ohne eigene Münze;** das Kind startet sie über den vorhandenen
   `POST extra-training/{exerciseId}/runs`. Die Sperre „erst die Pflicht" bleibt; ist der Deckel voll, erscheint der Vorschlag nicht mehr
   (`bonus-deckel`-Verhalten). Verworfen: eine Bonusaufgabe als eigene Entität mit eigener Münzregel.
3. **Die Sohn-Kurve zeigt die gefestigten Wörter je Tag** aus der Auswertung des Kindes (B-733), nicht aus dem Bruder und ohne Fehlerarten.
   `GET student/children/{childId}/progress-curve` liefert nur Tag und Zahl; die Eigentumsprüfung trennt die Kinder.
4. **Die Begründung an der Aufgabe kommt additiv in den Tagesplan der Sohn-Sicht** (`PositionStatus` bzw. Aufgabenliste in `TodayResponse`): ein
   Textfeld, kein Zahlenwert.

## Akzeptanzkriterien

1. `DELETE supervisor/children/{childId}/daily-tasks/{taskId}` streicht eine **nicht begonnene** Aufgabe; eine begonnene (heute Antwort oder
   Sitzung dazu) liefert `409` mit `code: task_started`. *Rot, wenn* das Streichen einer begonnenen Aufgabe gelingt.
2. `POST supervisor/children/{childId}/daily-plans/{day}/tasks` legt eine eigene Aufgabe (`Herkunft` Vater) dazu; sie zählt zur Pflicht des Tages
   und ist streichbar. *Rot, wenn* eine Vater-Aufgabe nicht gezählt wird oder ohne Vermerk erscheint.
3. Jeder Eingriff steht in der Auswertung des Folgetags als Zähler (Planer, hinzugefügt, gestrichen), und das Vater-Web zeigt ihn. *Rot, wenn*
   ein Eingriff ohne Vermerk bleibt.
4. Beide Endpunkte verlangen Supervisor-Claim und Eigentum: ein Konto nur mit Creator-Profil `403`, ein fremder Vater `404`. *Rot, wenn* ein Gate fehlt.
5. Das Streichen und Hinzufügen ändert den Tagesplan, aber nicht die gebuchten Münzen und nicht den Malus einer erledigten Aufgabe; eine gestrichene
   Aufgabe kostet nichts ([B-736](B-736-pflicht-aus-der-tagesaufgabe.md) Kriterium 6). *Rot, wenn* ein Streichen nachträglich gebuchten Malus verändert.
6. Eine Bonusaufgabe ist ein vorgeschlagenes Bonus-Training mit Begründung und bucht nur im Rahmen von `BonusDailyCoinCap`; ist der Deckel voll,
   wird sie nicht vorgeschlagen. *Rot, wenn* eine Bonusaufgabe Münzen außerhalb des Deckels bucht.
7. Die Sohn-App zeigt an jeder Aufgabe die Begründung und eine Kurve der eigenen gefestigten Wörter; sie zeigt weder Fehlerarten noch Zahlen des
   Bruders. *Rot, wenn* ein Student-Endpunkt Fehlerarten oder Zahlen eines anderen Kindes liefert (Eigentumsprüfung, Reflexionsprüfung nach
   `Actions_Geben_Nur_Vertragstypen_Zurueck`).
8. Das Vater-Web zeigt Streichen und Hinzufügen am Tagesplan mit Bestätigung und liest das Ergebnis vom Server zurück. *Rot, wenn* die Maske eine
   begonnene Aufgabe als streichbar anbietet.
9. Die Migration lässt `DailyEvaluation` mit allen Zeilen bestehen (neue Zähler leer oder null). *Rot, wenn* die Migration fehlt oder Zeilen verloren gehen.

## Schätzung

**Größe: L** — Anker: eine DB-Umbau-Etappe wie E6, am unteren Rand: zwei Schreib-Endpunkte mit der Regel „begonnen", eine Bonus-Anbindung an
vorhandene Mechanik, ein Student-Lese-Endpunkt, Vater-Web und Sohn-App. Die Sohn-App liegt im selben Arbeitsbaum wie fremde Änderungen.

- **`migration: ja`** — drei Zähler an `DailyEvaluation`. An die Kette hängen.
- **`vertragsbruch: nein`** — neue Endpunkte und DTOs; die Begründung kommt als zusätzliches Textfeld in die Sohn-Sicht (additiv, Probe:
  `UnknownFieldTests`, `ContractDocumentTests`).
- **`wo: beides`** — Backend zuerst (Eingriffe, Bonus, Kurve), dann Vater-Web und Sohn-App.

### Risiken

- **„Begonnen" ist eine Race-Bedingung:** Das Kind beginnt, während der Vater streicht. Die Prüfung und das Streichen gehören in **eine** Transaktion,
  der Verlierer bekommt `409`, nie einen Halbzustand.
- **Sohn-App im Arbeitsbaum:** `sohn/*` trägt ungecommittete LearningDad-Arbeit; vor dem Bau klären, ob sie zuerst committet wird, sonst vermischen sich
  die Commits.
- **Bonus hängt an `no_plan` und `duty_open`:** Im scharfen Modus muss die Sperre „Pflicht offen" die Tagesaufgaben lesen, nicht die Positionen
  (Zusammenspiel mit B-736).
- **Nicht in B-736 mitgezogen:** Die Tagesbox- und Missions-Sperre liest dieselbe Tagessicht; ein Leser, der noch auf Positionen rechnet, würde den
  Bonus freigeben.
- **Vater kann den Boden unterlaufen** (Entscheidung 7): Das ist gewollt, die Auswertung zeigt es.

### Angriffsplan

1. Migration (Zähler) und Auswertung des Folgetags um die Zähler erweitern.
2. Endpunkte Streichen und Hinzufügen mit der Begonnen-Regel und Transaktion.
3. Bonus: Planer-Regel für den Vorschlag, Anbindung an `extra-training`, Deckelprüfung.
4. `progress-curve` für Student; Begründungsfeld in der Sohn-Sicht.
5. Vater-Web (Eingriffe), danach Sohn-App (Begründung, Kurve).

### Testweg

- Neue Klasse `TagesplanEingriffeTests` (Integration): Streichen unbegonnen, begonnen `409`, Hinzufügen, Vermerk in der Auswertung des Folgetags,
  Gates. Rote Probe je AK: die Begonnen-Prüfung entfernen lässt AK 1 fallen; das Zählen der Vater-Aufgaben entfernen AK 2.
- `BonusAufgabeTests` nach dem Muster `ExtraTrainingTests` (Deckel voll, Vorschlag fehlt, keine Münzen außerhalb des Deckels).
- `SohnKurveTests` (nur eigenes Kind, keine Fehlerarten); `ConventionGuardTests`-Reflexionsprüfung.
- Frontend: Vitest je Maske; E2E neu `frontend/e2e/tagesplan-eingriffe.spec.ts` (Vater streicht, Sohn sieht die Begründung), Regression
  `frontend/e2e/bonus.spec.ts`, `bonus-deckel.spec.ts`, `bonus-deckel-voll.spec.ts`.
- Vor der Abnahme: `pugling-reviewer`, `frontend-reviewer`, Rollengang als Vater und Sohn.

## Reihenfolge und Abhängigkeiten

Nach [B-736](B-736-pflicht-aus-der-tagesaufgabe.md) und [B-737](B-737-reset-zum-stichtag.md), nach [B-733](B-733-tagesauswertung-und-leistungsprofil.md)
(Zähler) und [B-680](B-680-taeglicher-lehrplan-je-fach-just-in-time.md) (Bonus ist die letzte Regel nach dem Rest). Der Sohn-Teil sollte warten, bis die
fremde `sohn/*`-Arbeit committet ist.

## Verlauf

- **2026-11-20** — angelegt durch Teilung von B-644 (Etappe E4b) und `gegrillt` → `geschaetzt` im Vorbereitungslauf (Sonnet):
  Eintrittsbedingung `gegrillt` aus B-644 geerbt. Nachgesehen: Bonus-Training und Deckel sind gebaut (`ExtraTrainingService`), ein Eingriffsweg und eine
  Sohn-Kurve fehlen; die Sohn-App trägt fremde, ungecommittete Änderungen. L, beides, Migration, kein Vertragsbruch.
