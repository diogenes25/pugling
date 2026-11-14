---
tags: [typ/story, status/abgenommen, bereich/api, bereich/gamification, rolle/student]
aliases: [Tempo-Bonus-Messung, Antwortzeit je Karte]
status: abgenommen
prio: P3
art: Wunsch
groesse: S
wo: beides
migration: ja
vertragsbruch: nein
quelle: "B-526, Entscheidung 11 und Risiko 1 der Schätzung (2026-11-13)"
grund: ""
ersetzt_durch: []
wartet_auf: ""
---

# B-530 · Antwortzeit ab der sichtbaren Karte messen

Der Schnell-Bonus misst die Antwortzeit als Abstand zur vorigen Antwort der Sitzung. Seit das Blatt aus
[B-526](B-526-lektionsbildschirm-festes-raster.md) auf „Weiter" wartet, zählt die Lesezeit dort in die nächste
Karte – der Bonus belohnt dann das Wegdrücken der Rückmeldung. Dieselbe Wirkung hatte schon
[B-434](B-434-schnellbonus-misst-die-lesezeit-der-loesung-mit.md) für das Anhalten nach einem Fehler (B-420) gemeldet;
sie ist hier aufgegangen.

## User Story

Als *Vater* möchte ich, dass mein Sohn die Rückmeldung in Ruhe lesen kann, ohne dafür den Tempo-Bonus der nächsten Karte
zu verlieren, damit der Bonus schnelles Wissen belohnt und nicht das Überspringen der Lösung.

## Ist-Stand am Code

Gelesen am 2026-11-13 (Worktree `backlog-lauf`, `699b02a7`):

- Messstelle: `backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:430-437` – `lastAt` = jüngste
  `ReviewEvent.At` der Sitzung, `elapsedSeconds = now − lastAt` aus derselben `TimeProvider`-Uhr; `null` für die erste
  Karte einer Sitzung. (Die Zeilenangabe `:418-420` aus B-434 ist verschoben.)
- Bewertung: `ScoringService.IsFastAnswer` (`backend/Pugling.Api/Services/Shared/ScoringService.cs:92-94`) –
  `MinSpeedSeconds ≤ elapsed ≤ SpeedThresholdSeconds`; die Untergrenze (1 s) ist der Anti-Farming-Schutz.
  Der Bonus zahlt in **Gems** (`PointKind.Speed`, `PointKindCurrency.cs:24`) und nur auf gewerteten Karten
  (fällig, einmal je Karte und Tag, `PositionPracticeController.cs:413-420`).
- Die Arcade lädt alle fälligen Karten vorab (`api.cards`, `frontend/src/sohn/SohnPractice.tsx:108`) und schaltet
  lokal weiter; nach einer getippten Antwort steht das Blatt bis „Weiter" (`:240-262`, Phase `blatt`), `next()` (`:296-310`)
  zeigt dann die nächste Karte. Der Server erfährt vom Moment „Karte sichtbar" nichts.
- `PracticeSession` (`backend/Pugling.Api/Models/StudyPlanEntities.cs:88-113`) hat `StartedAt`, `Cursor`, `Reviews`, aber
  keinen Zeitpunkt der zuletzt gezeigten Karte.
- Tests der heutigen Regel: `backend/Pugling.Api.Tests/SpeedBonusTests.cs` mit eingefrorener Uhr (`_factory.Clock`).

## Die echte Lücke

Der Server misst ab der vorigen Antwort, weil er keinen anderen Zeitpunkt kennt. Die Lesezeit auf dem Blatt gehört nicht
zur Antwortzeit, und nur der Client weiß, wann „Weiter" gedrückt wurde.

## Offene Punkte

1. ~~Wie kann der Server einer vom Client gemeldeten Zeit trauen?~~ → Entscheidungen 1 und 2.
2. ~~Ab dem Ausspielen messen oder den Effekt hinnehmen (Frage aus B-434)?~~ → Entscheidung 1.
3. ~~Was gilt ohne Meldung (alter Client, Netzfehler)?~~ → Entscheidung 3.

## Entscheidungen

Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13, [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).

1. **Messen ab „Karte sichtbar", gemeldet als Zeitpunkt, gestempelt mit der Server-Uhr – nicht als Dauer.** Neuer
   Endpunkt `POST …/sessions/{sessionId}/shown` (204); der Server setzt `PracticeSession.CardShownAt = now`. Die Arcade
   ruft ihn in `next()` beim Verlassen des Blatts. `Review` misst dann `now − max(lastAt, CardShownAt)`.
   *Begründung:* Die Dauer kommt weiter aus der Server-Uhr; der Client sagt nur *wann*, nicht *wie lange*. Hinnehmen hieße,
   dass der Bonus das Lesen der Lösung bestraft – gegen den Zweck des Blatts und gegen das Interesse des Vaters.
   *Kosten:* ein Endpunkt, ein Aufruf je Karte nach dem Blatt.
2. **Schutz gegen Schummeln: ein Stempel je Karte, nie vor der vorigen Antwort, Untergrenze bleibt.** `shown` wird nur
   angenommen, solange seit der letzten Antwort noch kein Stempel gesetzt ist (`CardShownAt ≤ lastAt`); ein zweiter Aufruf
   ändert nichts. Die 1-Sekunden-Untergrenze gilt unverändert. Der Supervisor stempelt nicht (Vorschau zahlt nichts).
   *Begründung:* Mit der normalen App kann das Kind nichts erschwindeln – „Weiter" drücken *ist* der Moment, ab dem die
   Karte sichtbar ist. Wer Anfragen von Hand fälscht, gewinnt höchstens den Tempo-Bonus auf eine langsam, aber
   **richtig** beantwortete, fällige Karte – einmal je Karte und Tag, in Gems. Das ist dieselbe Klasse wie heute (der
   Fälscher kennt mit `api.cards` ohnehin alle Fragen vorab). *Kosten:* Restrisiko bewusst angenommen.
3. **Ohne Stempel gilt die heutige Regel** (Abstand zur vorigen Antwort). *Begründung:* Ein alter Client, ein
   verlorener Aufruf oder eine Selbsteinschätzung ohne Blatt messen weiter wie bisher – nie großzügiger. *Kosten:* keine.
4. **`shown` ist fire-and-forget im Frontend.** *Begründung:* Ein Fehlschlag kostet höchstens den Bonus (Entscheidung 3),
   er darf die Runde nicht anhalten (anders als die Bewertung, B-532). *Kosten:* keine Meldung bei Fehlschlag.

## Akzeptanzkriterien

1. Uhr eingefroren: Antwort 1, dann +10 s `shown`, dann +2 s Antwort 2 (Schwelle 5 s) → Tempo-Bonus auf Antwort 2.
   *Rot, wenn* `Review` weiter ab `lastAt` misst.
2. Antwort 1, +2 s `shown`, +0,5 s Antwort 2 → kein Bonus. *Rot, wenn* die Untergrenze nach dem Stempel nicht mehr greift.
3. Antwort 1, +10 s `shown`, +5 s `shown` (zweiter Aufruf), +2 s Antwort 2 → Bonus aus dem **ersten** Stempel gemessen
   (7 s > 5 s → kein Bonus). *Rot, wenn* ein späterer Stempel den früheren überschreibt.
4. Antwort 1, +3 s Antwort 2 ohne `shown` → Bonus wie heute. *Rot, wenn* das Fehlen des Stempels die Messung ändert.
5. `shown` mit Supervisor-Token setzt keinen Stempel. *Rot, wenn* die Rollenprüfung fehlt.
6. Bestehende Datenbank: Migration hängt eine nullable Spalte an; offene Sitzungen behalten ihre Reviews, `CardShownAt`
   ist `null` (`SchemaGuardTests` grün). *Rot, wenn* die Spalte nicht nullable ist oder die Kette neu gefaltet wird.
7. Arcade: „Weiter" auf dem Blatt ruft `shown` genau einmal; ein Fehlschlag hält die Runde nicht an. *Rot, wenn* `next()`
   den Aufruf nicht macht oder auf ihn wartet.

## Schätzung

**Größe: S** – eine nullable Spalte samt Migration, ein kleiner Endpunkt, eine geänderte Zeile in der Messung, ein Aufruf
im Frontend. `wo: beides`, `migration: ja` (additiv, nimmt alle Zeilen mit), `vertragsbruch: nein` (neuer Endpunkt).

**Risiken:** Migration gegen die echte `pugling.db` – nur anhängen, nullable, kein Default nötig. Ein Doppel-Weiter
(B-284) darf nicht zweimal stempeln; Entscheidung 2 macht das harmlos. `next()` läuft auch beim Überspringen
(`SohnPractice.tsx:507`) – dort ist der Stempel ebenso richtig (die neue Karte wird sichtbar).

**Angriffsplan:** (1) AK 1–5 in `SpeedBonusTests.cs`, AK 1 und 3 rot gegen den Ist-Code; (2) Spalte + Migration
(`dotnet dotnet-ef migrations add CardShownAt …`), Endpunkt, Messung an `:436-437`; (3) `pugling-reviewer`;
(4) Client-Methode, Aufruf in `next()`; (5) Vitest AK 7; (6) rote Probe je AK; `frontend-reviewer`.

**Testweg:** `backend/Pugling.Api.Tests/SpeedBonusTests.cs` (AK 1–5), `SchemaGuardTests` (AK 6), `frontend/src/sohn/SohnPractice.test.tsx` (AK 7).
Rollengang: eine getippte Runde, Blatt 10 s lesen, nächste Karte schnell → Tempo-Bonus auf dem Blatt.

**Nicht in dieser Story:** Tempo-Bonus auf der ersten Karte einer Sitzung (heute `null`, bleibt so); Messung im Test
oder im Bonus-Training.

## Verlauf

- 2026-11-13 · `idee` – abgespalten aus B-526; Prio P3 ist ein Vorschlag.
- 2026-11-13 · Backlog-Abgleich: B-434 hier aufgenommen (dieselbe Messstelle); Code-Behauptung bestätigt, `unverifiziert` entfällt.
- **2026-11-13** — `ausformuliert`: Messstelle neu belegt (jetzt `PositionPracticeController.cs:430-437`); der Server kennt den Moment „Karte sichtbar" nicht, weil die Arcade alle Karten vorab lädt.
- **2026-11-13** — `gegrillt`: Entscheidungen 1–4 (Zeitpunkt statt Dauer melden, Server-Uhr stempelt, ein Stempel je Karte, ohne Stempel alte Regel); beantwortet auch die Frage aus B-434. Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13, [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).
- **2026-11-13** — `geschaetzt`: S, beides, Migration ja (nullable Spalte), kein Vertragsbruch.
- **2026-11-13** — `in-arbeit` (Sprint 72): nullable Spalte `PracticeSession.CardShownAt`, Migration `20261113224441_CardShownAt` hängt die Kette an (nur `AddColumn … nullable: true`, Snapshot-Diff genau diese Spalte; die von EF auf Windows erzeugten `\r\n` in drei Check-Constraint-Strings auf das `\n` der Kette zurückgesetzt). Neuer Endpunkt `POST …/practice-sessions/{sessionId}/shown` (204): stempelt mit der Server-Uhr, nur für den Studenten, nur nach mindestens einer Antwort und nur, solange kein Stempel später als die letzte Antwort steht; `Review` misst ab `max(lastAt, CardShownAt)`, ohne Stempel wie bisher, Untergrenze unverändert. Arcade: `api.cardShown` fire-and-forget in `next()`, sobald die nächste Karte sichtbar wird (auch beim Überspringen). Die Grenze hält an beiden Enden: der Client meldet nur das Wann (Server-Uhr), ein Stempel je Karte, nie vor der vorigen Antwort, 1-s-Untergrenze bleibt. Tests: `SpeedBonusTests` (AK 1–5), `SchemaGuardTests` grün (AK 6), `SohnPractice.test.tsx` „meldet mit Weiter die nächste Karte genau einmal als sichtbar"/„schaltet weiter, ohne auf die Meldung zu warten, auch wenn sie scheitert" (AK 7; das Nicht-Warten ist ohne einzeiligen Mutanten, der zweite Fall darum Absichtserklärung für „hält die Runde nicht an"). Rote Probe (M4 im zweiten Lauf, im ersten kompilierte er nicht):

  Gemessen an `36ebffe5 + Arbeitsbaum geändert`, 2026-11-13 23:49 / 23:50.

  | Mutante | Stelle | erwartet | gemessen | Anmerkung |
  |---|---|---|---|---|
  | M1 | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:467 | red | red | misst weiter ab `lastAt` (AK 1) |
  | M2 | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:468 | red | red | Untergrenze nach dem Stempel ausgehebelt (AK 2) |
  | M3 | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:151 | red | red | späterer Stempel überschreibt (AK 3) |
  | M4 | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:467 | red | red | ohne Stempel keine Messung (AK 4) |
  | M5 | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:148 | red | red | Supervisor stempelt (AK 5) |
  | M6 | frontend/src/sohn/SohnPractice.tsx:317 | red | red | `next()` meldet nichts (AK 7) |

  Grundlinie nach dem Zurückspielen: green (beide Läufe).
- **2026-11-14** — Code-Review-Fund behoben (Restrisiko 1 des Frontend-Reviews): `api.cardShown` ist fire-and-forget; überholte die Antwort auf Karte B die Meldung „B sichtbar", wurde der Stempel zur Startzeit der übernächsten Karte und maß deren Antwort zu kurz. Die Meldung nennt jetzt ihre Karte (`CardShownDto(ItemIndex)`, Pflicht-Rumpf von `POST …/shown`); der Server stempelt nur, wenn sie `Order[Cursor]` der Sitzung ist, sonst 204 ohne Wirkung (für das Kind nichts zu tun). Die Arcade schickt `cards[idx + 1].itemIndex`. Tests: `SpeedBonusTests.Verspaeteter_Stempel_Der_Vorigen_Karte_Zaehlt_Nicht`, `SohnPractice.test.tsx` „meldet mit Weiter die nächste Karte genau einmal als sichtbar" (jetzt mit Indizes 7/4/9 und erwartetem `itemIndex`). Rote Probe:

  Gemessen an `ae9fb08a + Arbeitsbaum geändert`, 2026-11-14 00:35 / 00:40.

  | Mutante | Stelle | erwartet | gemessen | Anmerkung |
  |---|---|---|---|---|
  | BE6 | backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:159 | red | red | Stempel nicht an die Karte gebunden |
  | FE1 | frontend/src/sohn/SohnPractice.tsx:327 | red | red | Rundenposition statt `itemIndex` |

  Grundlinie nach dem Zurückspielen: green (beide Läufe).
- **2026-11-13** — **abgenommen** mit Commit `ff6a08fb + fbd78e04` (Sprint 72, Backlog-Lauf 2026-11-13). Verifikation: Backend 1599/1599 grün (dotnet test Pugling.sln -c Release), Vitest 998/998, tsc -b sauber, Mutanten-Probe je AK-Fall rot (Tabelle oben). Review: Code-Review â€“ Fund behoben in `fbd78e04`: ein verspÃ¤tet eintreffender Stempel konnte zur Basis der Ã¼bernÃ¤chsten Karte werden; jetzt trÃ¤gt `POST â€¦/shown` den Index der Karte und wirkt nur auf die aktuelle. Das bewusst hingenommene Restrisiko (Stempel von Hand kurz vor der Antwort, begrenzt auf Gems) gilt weiter. Rollengang: volle Playwright-Suite gegen frisch gestartete Server auf Ausweichports (API 5216, Web 5189) nach dem letzten Edit: **145 passed** (alle Specs). Den Tab „Erfolge“ (Woche, Frist) fährt keine Spec eigens; er ist über RTL-Tests belegt – ein Handblick am Gerät bleibt für den Menschen.
