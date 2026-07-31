---
tags: [typ/story, status/in-arbeit, bereich/backend, bereich/frontend, rolle/student]
aliases: [Übung abbrechen, Unvollendete Übung, Inaktivitäts-Abbruch, Wann wird eine Übung abgebrochen]
status: in-arbeit
prio: P1
art: Defekt
groesse: M
wo: beides
migration: nein
vertragsbruch: nein
quelle: Nutzer, Sitzung 2026-07-31
---

# B-37 · Abgebrochene Runden: Pflicht härten, Klausur deckeln

> Angetreten als Prüfauftrag „wann wird eine Übung abgebrochen?". Die Antwort steht unter
> [Ist-Stand](#ist-stand-am-code); weil sie zwei echte Defekte freigelegt hat, ist `art` beim Grillen von
> `Frage` auf `Defekt` gewechselt und die Story trägt jetzt die Reparatur.

## User Story

> Als **Vater** möchte ich wissen, was passiert, wenn mein Sohn eine begonnene Übung nicht zu Ende spielt —
> damit ich weiß, ob eine verlassene Runde als Pflicht zählt, ob sie Münzen bringt und ob er eine
> schlecht laufende Klausur einfach neu anfangen kann.

Der Prüfauftrag ist beantwortet; der Ist-Stand unten ist die Antwort. Zwei der vier Fragen ergeben ein
harmloses Bild, zwei legen eine echte Lücke offen.

## Ist-Stand am Code

### 1. Es gibt keinen Abbruch-Zustand — nur „beendet" und „offen"

Zwei unabhängige Ausspiel-Objekte, beide mit eingefrorener Reihenfolge und Server-Cursor:

| Objekt | Beleg | Ende markiert durch | Abbruch-Zustand |
| --- | --- | --- | --- |
| `PracticeSession` (Üben) | [StudyPlanEntities.cs:77-102](../../backend/Pugling.Api/Models/StudyPlanEntities.cs) | `EndedAt` | **keiner** |
| `TestAttempt` (Klausur) | [StudyPlanEntities.cs:125-154](../../backend/Pugling.Api/Models/StudyPlanEntities.cs) | `CompletedAt` | **keiner** |

„Unvollendet" ist also kein Zustand, sondern das **Fehlen** eines Zeitstempels — nicht unterscheidbar von
„läuft gerade noch".

Endpunkte: Das Üben hat `POST …/practice-sessions/{sessionId}/end`
([PositionPracticeController.cs:357-372](../../backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs)),
aber das ist das **normale** Ende (setzt `EndedAt`, bucht Ziel-Punkte und wertet Missionen), kein Abbruch.
Der Test hat **kein Gegenstück**: nur `submit`
([PositionTestsController.cs:221-286](../../backend/Pugling.Api/Controllers/Student/PositionTestsController.cs))
schließt einen Versuch, und der bewertet. Ein verlassener Versuch bleibt für immer offen.

### 2. Inaktivität bricht nichts ab

Es gibt keine Zeitgrenze, keinen Scheduler und keinen Aufräumlauf. `EndedAt`/`ActiveSeconds` werden von
genau zwei Stellen gelesen — [MetricsService.cs:40-42](../../backend/Pugling.Api/Services/Shared/MetricsService.cs)
(Summe für `MinutesPracticed`) und [PositionProgressService.cs:63-67](../../backend/Pugling.Api/Services/Shared/PositionProgressService.cs)
(Erledigt-Regel) — und **keine** behandelt „alt und offen" besonders. Der Heartbeat
([PositionPracticeController.cs:87-96](../../backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs))
deckelt nur die gutgeschriebenen Sekunden je Schlag auf 120 (`MaxHeartbeatSeconds`, Zeile 31); er lässt
nichts verfallen. Das passt zur bestehenden Linie „es gibt keinen Scheduler" (Malus wird lazy abgerechnet).

### 3. Punkte: pro Antwort — aber der naheliegende Farm-Weg ist zu

- **Üben:** `Review` bucht `ChildPointsEntry` **sofort je Antwort**, aber nur innerhalb
  `if (leitnerScored)` ([Zeile 316-331](../../backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs)).
  `leitnerScored = pos.UseLeitner && scored` (315), und `scored` verlangt `due && !alreadyScoredToday`
  (277-279). `alreadyScoredToday` vergleicht `prog.LastReviewedAt` mit dem Sitzungstag (278), gesetzt wird
  es in [PositionPlayService.cs:208](../../backend/Pugling.Api/Services/Shared/PositionPlayService.cs).
  **Folge: „bis zur letzten Karte spielen, abbrechen, neu beginnen" bringt 0 Punkte** — dieselben Karten
  sind heute schon gewertet. Die Farm-Sorge aus der Idee ist unbegründet.
- **Ziel-Punkte** (`PointsGoalMet`) sind idempotent je Periode über `PositionGoalReward`
  ([PositionProgressService.cs:145-156](../../backend/Pugling.Api/Services/Shared/PositionProgressService.cs)).
- **Test:** `Answer` bucht **nichts** ([PositionTestsController.cs:160-197](../../backend/Pugling.Api/Controllers/Student/PositionTestsController.cs));
  erst `Submit` schreibt Ergebnis und Lernstand. Der Kommentar dort (190-191) sagt es ausdrücklich: „erst
  beim Abschluss EINMAL geschrieben, damit abgebrochene/wiederholte Versuche den Lernstand nicht
  verfälschen". Ein abgebrochener Test ist damit **völlig folgenlos**.

### 4. UI: kein Abbruch-Knopf, und Weglaufen wirkt unterschiedlich

- **Kein Knopf während der Runde.** In [SohnPractice.tsx](../../frontend/src/sohn/SohnPractice.tsx) gibt es
  „Zur Basis" nur in den Phasen error/empty/done (126, 135, 148); die Kopfzeile der laufenden Runde trägt
  nur Kartenzähler, Combo-Pille und Tempo-Schalter (179-185). [SohnTest.tsx](../../frontend/src/sohn/SohnTest.tsx)
  genauso: „Zur Basis" nur im Fehler- und Ergebnisbildschirm (111, 221).
- **Router-Navigation beendet die Übungsrunde ordentlich:** das Cleanup des Heartbeat-Effekts schickt die
  Rest-Sekunden und ruft `endSession` (SohnPractice.tsx:70-77).
- **Tab schließen oder neu laden nicht:** in `frontend/src` gibt es kein `beforeunload`, `pagehide`,
  `visibilitychange` und kein `sendBeacon` (Grep: kein Treffer). Die Sitzung bleibt ohne `EndedAt` liegen,
  die letzten <12 s sind verloren.
- **Der Test hat gar kein Cleanup.** Beim erneuten Betreten legt `start()` einen **neuen** Versuch an
  (SohnTest.tsx:70-77; die `startedFor`-Ref schützt nur gegen den Doppellauf des Effekts). Offene Versuche
  sammeln sich also an.
- **Fortsetzen kann nur der Fehlerfall:** `resume()` (SohnTest.tsx:80-88) holt die Cursor-Frage des
  laufenden Versuchs erneut — aber nur aus dem Fehlerbildschirm, mit der `attemptId` im State. Nach einem
  Reload ist die weg.

### 5. Und die Klausur ist beliebig oft wiederholbar

`Start` prüft **nicht**, ob heute schon ein Versuch läuft oder abgeschlossen ist
([PositionTestsController.cs:66-117](../../backend/Pugling.Api/Controllers/Student/PositionTestsController.cs));
eine Versuchsobergrenze existiert nirgends (Grep `MaxAttempts`/`AttemptsPerDay`: kein Treffer). Das
Ergebnisbild bietet den Neustart sogar an (`onRetry={start}`, SohnTest.tsx:114). Die Erledigt-Regel verlangt
nur **irgendeinen** bestandenen Versuch in der Periode
([PositionProgressService.cs:69-71](../../backend/Pugling.Api/Services/Shared/PositionProgressService.cs)).

## Die echte Lücke

Nicht der fehlende Knopf. Drei Dinge, in dieser Reihenfolge:

1. **Zwölf Sekunden Anwesenheit erfüllen die Pflicht.** Für Übungstypen mit `ExerciseCheckMode.None`
   (Inhalts-/Leseübungen) gilt das Ziel als erreicht, sobald eine `Lern`-Sitzung mit
   `EndedAt != null || ActiveSeconds > 0` in der Periode existiert (PositionProgressService.cs:63-67).
   **Ein einziger Heartbeat genügt** — wer die Runde nach 12 s verlässt, hat die Pflicht erfüllt und
   bekommt `PointsGoalMet`. Das ist die Stelle, an der „nicht abgeschlossen" heute doch bezahlt wird, und
   sie steht gegen die Kernidee des Produkts (Pflicht erzwingen, verpasste Pflicht kostet).
2. **Eine schlecht laufende Klausur kostet nichts.** Verlassen ist folgenlos (Befund 3), ein neuer Versuch
   jederzeit möglich (Befund 5). Kein Punkte-Farming — aber **Noten-Farming**: der Sohn kann so lange neu
   anfangen, bis es sitzt, und jeden misslungenen Lauf lautlos wegwerfen.
3. **Der Server könnte fortsetzen, wird aber nicht gefragt.** `Cursor` ist persistiert und `Order`
   eingefroren; es fehlt nur ein Endpunkt „mein offener Versuch / meine offene Sitzung zu dieser Position".
   Deshalb legt die Oberfläche nach einem Reload neu an, statt fortzusetzen — und verlassene Versuche
   wachsen unbegrenzt, ohne von einem laufenden unterscheidbar zu sein.

**Nebenfund (klein, gehört zur Abbruch-Mechanik):** `End` trägt **keinen**
`PlanPlayableForChild`-Wächter (PositionPracticeController.cs:357-372), obwohl `Cards` (126), `Next` (208)
und `Review` (246) ihn alle tragen — und `End` bucht über `EvaluateAndAwardAsync` Ziel-Punkte (369), die
selbst nicht auf `Active` prüfen (PositionProgressService.cs:137-164). Ein mitten in der Runde
deaktivierter Plan lässt sich also über `end` noch zu den Ziel-Punkten der laufenden Periode bringen.
Einmalig (idempotent je Periode), kein Farm — aber ein Loch in einer ansonsten geschlossenen Wand.

## Offene Punkte

Alle sieben sind in der Grill-Runde vom 2026-07-31 entschieden; die Nummern in Klammern zeigen auf die
Entscheidung. Durchgestrichen statt gelöscht, damit nachvollziehbar bleibt, was gefragt war.

1. ~~**Bleibt das eine Story?** Vorschlag war teilen (`grund: geteilt`, zwei Nachfolger).~~ → **E8**;
   der Vorschlag wurde im Grillen **verworfen**, weil die Entscheidungen sich als klein und verzahnt
   erwiesen.
2. ~~**Ist die Zwölf-Sekunden-Pflicht ein Defekt oder Absicht?**~~ → **E1**
3. ~~**Darf eine Klausur beliebig oft wiederholt werden?**~~ → **E3**
4. ~~**Was ist ein Abbruch fachlich — verwerfen oder werten?**~~ → **E4/E5**; die Frage hat sich zur Hälfte
   aufgelöst: mit idempotentem `Start` gibt es keinen Test-Abbruch mehr, nur ein Unterbrechen.
5. ~~**Soll Inaktivität abbrechen?**~~ → **E6**
6. ~~**Fortsetzen oder neu beginnen?**~~ → **E4**
7. ~~**Bleibt `prio: P2`?**~~ → **E8**; nein, `P1`.

Der Nebenfund aus [Die echte Lücke](#die-echte-lücke) (`End` ohne Playable-Wächter) ist als **E7**
entschieden.

## Entscheidungen

Aus der Grill-Runde vom **2026-07-31**. Jede mit Begründung **und** Kosten.

### E1 · Die Pflicht verlangt eine gespielte Runde, nicht Anwesenheit

Für `ExerciseCheckMode.None` gilt das Positionsziel künftig erst als erledigt, wenn die eingefrorene
Reihenfolge zu einem Mindestanteil **ausgespielt** wurde (`Cursor`), statt sobald `ActiveSeconds > 0`.

*Begründung:* Anwesenheit ist mit einem offenen Tab erzeugbar, gesehene Karten nicht. Die halbe Bibliothek
hängt daran (6 von 12 Typen, darunter Grammatik und Übersetzung), und „12 Sekunden = Pflicht erfüllt +
`PointsGoalMet`" steht gegen die Kernidee des Produkts.
*Kosten:* eine zweite Bedingung in `IsGoalMetAsync`. `EndedAt`/`ActiveSeconds` verlieren damit ihre Rolle in
der Erledigt-Regel — `ActiveSeconds` bleibt nur noch Futter für `MinutesPracticed`.

### E2 · Das Maß ist `GoalThreshold`, umgedeutet

Kein neues Feld: der vorhandene Prozentwert (`null` = 80 %) wird bei `CheckMode.None` als „Prozent der
Runde gespielt" gelesen, bei prüfbaren Typen bleibt er „Prozent richtig".

*Begründung:* Der Wert liegt schon an der Position und ist dort laut eigener XML-Doku
(`PlanPositionEntities.cs:59-61`) **nachweislich tot**. Der Satz für den Vater bleibt derselbe — „wie viel
Prozent musst du schaffen" —, also ist es eine Lesart, nicht eine zweite Bedeutung.
*Kosten:* **keine Migration, kein Vertragsbruch.** Dafür ein Feld mit zwei Lesarten: der Hilfetext in
`fieldHelp.ts` muss beide nennen, sonst entsteht genau der Defekt von B-02 (Hilfetext erklärt das Feld
falsch herum). Und die XML-Doku an `GoalThreshold` muss mitgezogen werden, sonst behauptet sie weiter,
der Wert sei hier ungenutzt.

### E3 · Zwei Klausur-Versuche je Periode, jeder gestartete zählt

`Start` weist einen dritten Versuch mit `ApiErrors.TestAttemptsExhausted` ab. Die Zahl ist eine Konstante
im Controller, kein Feld.

*Begründung:* Eine zweite Chance ist pädagogisch richtig, die fünfte ist Noten-Farming. Gezählt wird der
**Start**, nicht die Abgabe — sonst bliebe Weglaufen gratis und der Deckel wirkungslos. Reicht der Sohn
beide Versuche nicht bestanden ein, ist die Pflicht gerissen und der Malus greift; das ist der „Stick" des
Produkts, kein Unfall.
*Kosten:* Guard in `Start` + ein additiver Code in `ApiErrors`. Konfigurierbar wäre ein Feld und damit eine
Migration — bewusst nicht. Preis der Konstante: der Vater kann den Deckel nicht je Position lockern.

### E4 · `Start` wird idempotent (nur für den Sohn)

Findet `Start` einen offenen Versuch derselben Position in derselben Periode, gibt es **diesen** zurück
statt einen neuen anzulegen. Für `IsSupervisor()` bleibt es beim Neuanlegen, damit die Vorschau mit eigener
Stufe weiter funktioniert.

*Begründung:* Erst das macht E3 fair — sonst kostet ein versehentlicher Reload die Hälfte des Budgets. Der
Server hat den Zustand längst: `Cursor` ist persistiert, `Order` eingefroren.
*Kosten:* **null im Frontend und null im Vertrag.** `SohnTest.start()` ruft direkt nach `startTest` das
`nextTest` auf und übernimmt dessen `cursor` (`SohnTest.tsx:53-57`), landet also automatisch auf der
richtigen Frage. Preis: „abbrechen und eine neue Reihenfolge würfeln" ist zu — eine unglücklich
eingefrorene Reihenfolge muss ausgesessen werden.

### E5 · Beide Rundenarten bekommen einen sichtbaren Ausweg

Üben: **„Runde beenden"** (ruft `end`, also genau das, was das Cleanup heute schon tut). Klausur:
**„Später weiter"** (verlässt die Seite, der Versuch bleibt am Cursor stehen). Beide **ohne**
`confirmAction`-Rückfrage.

*Begründung:* Heute gibt es während einer laufenden Runde keinen Ausgang auf dem Schirm — für ein Kind
fühlt sich das wie eine Falle an, und genau das erzeugt die Panik-Reloads, die E4 abfängt. Keine Rückfrage,
weil nach E4 beides folgenlos ist: das Wort auf dem Knopf sagt die Wahrheit, und eine Rückfrage über eine
harmlose Aktion erzieht zum Wegklicken.
*Kosten:* zwei Knöpfe plus je eine E2E-Zeile. Kein Backend.

### E6 · Inaktivität bricht nichts ab

Kein Timeout, kein Aufräumlauf. Offene Zeilen aus abgelaufenen Perioden bleiben liegen.

*Begründung:* Das Projekt hat bewusst keinen Scheduler (der Malus wird lazy abgerechnet), und ein Timeout
hätte keinen Leser: weder die Erledigt-Regel noch `MinutesPracticed` fragen nach „noch offen". Nach E4 ist
ein offener Versuch ohnehin kein Müll, sondern der Fortsetzungspunkt.
*Kosten:* keine Arbeit — dafür wachsen offene Zeilen alter Perioden unbegrenzt an, für niemanden sichtbar.
Bewusst in Kauf genommen.

### E7 · `End` bekommt den Playable-Wächter, `pagehide` bleibt ein Nicht-Ziel

`POST …/practice-sessions/{sessionId}/end` prüft künftig wie `Cards`, `Next` und `Review`, ob der Plan für
den Sohn spielbar ist. Ein `pagehide`/`sendBeacon`-Listener fürs Tab-Schließen wird **nicht** gebaut.

*Begründung Wächter:* `End` bucht über `EvaluateAndAwardAsync` Ziel-Punkte, und die prüfen selbst nicht auf
`Active` — ein mitten in der Runde deaktivierter Plan lässt sich so noch zu den Punkten der laufenden
Periode bringen. Einmalig und kein Farm, aber ein Loch in einer ansonsten geschlossenen Wand.
*Begründung Nicht-Ziel:* Nach E1 hängt die Erledigt-Regel am `Cursor`; der verlorene Rest beim
Tab-Schließen sind damit **unter 12 Sekunden** in `MinutesPracticed`. Ein Listener, der bei jedem Reload
zwei Requests abfeuert, ist dafür zu teuer.
*Kosten:* drei Zeilen — aber `End` setzt `EndedAt` heute **vor** dem Laden des Plans, die Reihenfolge im
Rumpf dreht sich also mit. Kein bestehender Test ruft `End` mit einem nicht spielbaren Plan auf.

### E8 · Eine Story, `Frage` → `Defekt`, `P2` → `P1`

Kein Teilen. `art: Defekt`, weil repariert wird; damit ist die Abnahme ein **Regressionstest, der vorher
rot war**. `prio: P1`, weil E1 heute an einem echten Kind wirkt.

*Begründung:* Die vor dem Grillen empfohlene Teilung wurde verworfen: alle Entscheidungen fassen dieselben
vier Dateien an, und E3 ohne E4 wäre sogar schädlich — sie gehören in einen Commit.
*Kosten:* eine Story trägt sechs Änderungen, die Schätzung muss also die Summe abdecken statt einer
kleinsten Einheit. Dafür kein Frontmatter-Zwilling und keine zweite Schätzung für eine Arbeit, die in einem
Zug erledigt ist.

### Detail, das aus E1/E2 folgt

Bei **leerem Fälligkeits-Pool** ist `Order` leer, also ist `Cursor >= ceil(0.8 × 0)` sofort wahr — die
Pflicht gilt als erfüllt, ohne dass eine Karte kam. Das ist dasselbe Verhalten wie heute und fachlich
richtig (es gab nichts zu tun), gehört aber ausdrücklich in die Akzeptanzkriterien, damit niemand später
einen Test dagegen schreibt.

## Akzeptanzkriterien

- [x] Die vier Fragen des Nutzers sind mit `Datei:Zeile` beantwortet (siehe Ist-Stand).
- [x] **E1/E2:** Bei einem `CheckMode.None`-Typ erfüllt eine Lern-Sitzung mit einem Heartbeat und ohne
      gespielte Karte das Positionsziel **nicht** mehr; ab `GoalThreshold` Prozent gespielter Runde schon.
      Ein Regressionstest deckt beide Seiten ab und ist vor der Änderung rot.
      → `LernModus_ReineInhaltsuebung_BlosseAnwesenheit_ErfuelltDieTagespflichtNicht` /
      `…_GespielteRunde_ErfuelltDieTagespflicht`; die Rot-Probe ist gelaufen (siehe Verlauf).
- [x] **E1/E2:** Bei **leerer** eingefrorener Reihenfolge (nichts fällig) gilt das Ziel weiter als erfüllt.
      → `LernModus_ReineInhaltsuebung_LeererPool_ErfuelltDieTagespflicht`.
- [x] **E2:** Der Hilfetext zu `GoalThreshold` nennt beide Lesarten; die XML-Doku am Feld behauptet nicht
      mehr, der Wert sei bei `CheckMode.None` ungenutzt.
- [x] **E3:** Ein dritter Klausur-Start in derselben Periode antwortet mit `TestAttemptsExhausted`;
      verlassene Versuche zählen mit. Der Vater ist nicht gedeckelt.
      → `KlausurModus_DritterVersuchDerPeriode_WirdAbgewiesen_VaterNicht`.
- [x] **E4:** Zwei `Start`-Aufrufe des Sohns hintereinander liefern **denselben** `attemptId` samt Cursor;
      ein Reload mitten in der Klausur setzt an derselben Frage fort und verbraucht keinen Versuch.
      Die Vater-Vorschau bekommt weiter einen frischen Versuch.
      → `KlausurModus_VerlassenerVersuch_SchreibtKeinenLernstand_UndWirdFortgesetzt` **und** im Browser
      (`full-flow.spec.ts`: Klausur verlassen, wieder betreten, steht auf derselben Frage).
- [x] **E5:** Beide Rundenarten zeigen während des Spielens einen Ausweg; der Übungs-Knopf beendet die
      Sitzung serverseitig, der Klausur-Knopf verliert keinen Fortschritt.
- [x] **E7:** `End` weist einen nicht spielbaren Plan für den Sohn ab und bucht dafür keine Ziel-Punkte.
      → `Sohn_KannLaufendeSitzungAufInaktivemPlanNichtAbschliessen_403`.
- [x] Nichts von dieser Story endet als „offen:"-Vermerk irgendwo sonst — die beim Bauen entstandenen
      Grenzen stehen unten.

### Bekannte Grenzen (beim Bauen entstanden, bewusst)

- **Ein Versuch des Vaters auf derselben Position und Periode ist von dem des Sohns nicht
  unterscheidbar.** Der Sohn würde ihn fortsetzen, und er zählt gegen den Deckel. Ein Unterscheidungsmerkmal
  wäre eine Spalte am `TestAttempt` und damit eine Migration — für einen seltenen Vorschau-/Nachtragsfall zu
  teuer. Der Vater selbst ist nicht gedeckelt.
- **Der `Mode == Lern`-Filter in der Erledigt-Regel ist jetzt doppelt gesichert**, weil eine Info-Sitzung
  ihren Cursor nie vorrückt (`Review` antwortet im Info-Modus vor dem Cursor-Schritt mit 204). Er bleibt als
  Tiefenverteidigung stehen; geprüft wird er über das Paar aus Info- und Lern-Test mit leerem Pool.
- **`/smoke-test` wurde nicht zusätzlich gefahren.** Die Playwright-E2E deckt dieselbe Fläche ab (echter
  Server, Wegwerf-DB, ganzer Vater→Sohn-Loop) und prüft zusätzlich den Fortsetzen-Pfad im Browser. Sie nutzt
  aber eine **Vokabel**-Übung: der `CheckMode.None`-Pfad aus E1 ist nur durch die Integrationstests belegt,
  nicht durch einen Browser-Lauf.

## Schätzung

**M · `wo: beides` (Backend zuerst) · `migration: nein` · `vertragsbruch: nein`**

Die beiden Flags sind **nachgesehen, nicht vermutet**: Es entsteht keine persistierte Spalte (E2 deutet ein
vorhandenes Feld um, E3 hält die Zahl als Konstante), also kein Falten der Migrationskette. Und
`Pugling.Contracts` wird **nicht angefasst** — `AttemptResponse` bleibt wie sie ist, weil `SohnTest`
den Cursor ohnehin aus `nextTest` bezieht; der neue `ApiErrors`-Code liegt in `Pugling.Api/Errors`, nicht im
Vertrag.

Zur Größe: über dem S-Anker (B-01, „`childId` aus dem Test-Pfad ziehen" — eine Stelle, ein Gedanke), auf
Höhe des M-Ankers (B-03, ein neuer Pfad im `MediaSelector`). Sechs Änderungen in zwei Schichten, davon
zwei mit eigener Testarbeit, plus eine Vorarbeit. Kein L: keine Etappe, kein Schema, keine neue Ressource.

### Risiken

| # | Risiko | Umgang |
| --- | --- | --- |
| R1 | `PeriodRange` ist `private static` in `PositionProgressService:34` — der Deckel in `PositionTestsController` braucht sie. | Als eigener, verhaltensneutraler erster Schritt teilen (Grün halten), nicht nebenbei im Feature-Commit. |
| R2 | `PositionPlayModesTests.KlausurModus_AbgebrochenerVersuch_…` (Zeile 187-225) startet nach dem Abbruch bewusst einen **zweiten** Versuch. E4 liefert dort denselben zurück, der `next`-Dreierlauf läuft ins `done` und der Test bricht. | **Absicht, kein Kollateralschaden:** der Test dokumentiert die alte Entscheidung. Die eine Hälfte (abgebrochener Versuch schreibt keinen Lernstand) bleibt, die andere wird von „Wiederholung" auf „Fortsetzung" umgeschrieben. |
| R3 | Den **positiven** `CheckMode.None`-Fall (Lern-Sitzung ⇒ Pflicht erfüllt) deckt heute **kein** Test ab — geprüft über alle `dutyDone`-Vorkommen. E1 könnte also lautlos durchgehen. | Der Regressionstest muss **neu** geschrieben werden; „vorher rot" ist hier kein Automatismus, sondern eine Handlung. |
| R4 | `InfoModus_ErfuelltDasTagesziel_Nicht` (Zeile 101-122) bleibt nach E1 grün aus dem **falschen** Grund: der Cursor steht dort ohnehin auf 0. Die Info-Ausschluss-Regel verliert damit ihren Wächter. | Ein Lern-Gegenstück mit gespielter Runde ergänzen. Genau die Fehlerklasse „Regel getestet, Grenzfall offen" aus [docs/testplan.md](../testplan.md). |
| R5 | Der Deckel könnte bestehende Tests brechen. | Geprüft: höchstens **zwei** Starts je Position und Periode (`PositionGoalOverviewTests:33+57`, `PositionPlayModesTests:197+210`). Deckel 2 bricht keinen — aber ein künftiger dritter Start ist eine Falle, darum gehört die Konstante in eine benannte Stelle, nicht in einen Literal. |
| R6 | `End` setzt `EndedAt`, **bevor** der Plan geladen wird. | Rumpf-Reihenfolge mitdrehen: erst Plan + Wächter, dann schreiben. |
| R7 | `DocsCaptureTests` überschreibt `docs/api-examples` bei jedem Lauf. | Kippt ein Beispiel, wird die Datei mit committet — kein Fehler, aber im Diff zu prüfen. |

### Angriffsplan

**Backend zuerst** — API-First ist hier nicht Stil: die Erledigt-Regel und der Deckel entscheiden, was die
Oberfläche überhaupt anzeigen kann.

1. **Vorarbeit (verhaltensneutral):** `PeriodRange`/`WeekMonday` teilbar machen. Suite bleibt grün. (R1)
2. **E1 + E2** — Regressionstests zuerst (rot), dann `IsGoalMetAsync` auf `Cursor >= ceil(GoalThreshold %
   × Order.Count)` umstellen, XML-Doku an `GoalThreshold` nachziehen. Inklusive Leer-Pool-Kriterium und
   dem Lern-Gegenstück aus R4.
3. **E3 + E4 in einem Schritt** — Deckel und Idempotenz gehören zusammen, E3 allein wäre schädlich.
   `ApiErrors.TestAttemptsExhausted` additiv ergänzen, `KlausurModus_AbgebrochenerVersuch_…` umschreiben (R2).
4. **E7** — `End`-Wächter samt Rumpf-Reihenfolge (R6).
5. **Frontend:** `fieldHelp.ts` → `goalThreshold` um die zweite Lesart erweitern (der Eintrag existiert,
   Titel „Bestehen ab %", Text nennt heute nur „richtiger Antworten"). Dann die zwei Knöpfe in
   `SohnPractice.tsx`/`SohnTest.tsx`.
6. **Verifikation:** `/smoke-test` für den Durchstich, dann `pugling-reviewer` **und** `frontend-reviewer`.

### Testweg

Benannt, nicht behauptet:

- `PositionPlayModesTests` — trägt schon den Reading-Aufbau (`InfoModus_ErfuelltDasTagesziel_Nicht`), also
  der Ort für das E1-Paar; dort auch das Umschreiben aus R2.
- `PositionGoalOverviewTests` — Deckel und Idempotenz rund um die bestehenden Zwei-Versuchs-Fälle.
- `PositionTestFlowTests` — dritter Start in derselben Periode → `TestAttemptsExhausted`.
- `AntiCheatTests` — `End` auf einem nicht spielbaren Plan (die Klasse trägt die Playable-Wand; heute ruft
  sie `End` gar nicht auf).
- `frontend/e2e/full-flow.spec.ts` — die zwei Knöpfe im Sohn-Durchstich.
- `/smoke-test` gegen eine Wegwerf-DB, weil die Änderung Laufzeit-Verhalten hat (Pflicht + Punkte).

## Verlauf

- **2026-07-31** — vom Nutzer direkt aufgenommen (ungeprüft, keine Recherche — das ist der nächste Schritt).
- **2026-07-31** — `idee` → `ausformuliert`: gegen den Code belegt. Farm-Sorge widerlegt (Tageswertung je
  Karte greift), dafür drei echte Lücken gefunden: Pflicht schon nach einem Heartbeat erfüllt, Klausur
  unbegrenzt wiederholbar, kein Fortsetzen-Pfad. Plus Nebenfund: `End` ohne Playable-Wächter.
- **2026-07-31** — `ausformuliert` → `gegrillt`: acht Entscheidungen (E1–E8). Kern: die Pflicht misst
  künftig gespielte Karten statt Anwesenheit, getragen vom umgedeuteten `GoalThreshold` (**keine
  Migration**); die Klausur bekommt einen Deckel von zwei Versuchen je Periode, den ein idempotenter
  `Start` erst fair macht (**Frontend und Vertrag unverändert**). Die vorab empfohlene Teilung wurde
  verworfen, `art` von `Frage` auf `Defekt` und `prio` von P2 auf P1 gedreht.
- **2026-07-31** — `gegrillt` → `geschaetzt`: **M**, `wo: beides`, `migration: nein`, `vertragsbruch: nein`
  (beides nachgesehen: kein Schema, `Contracts` unberührt). Sieben Risiken, das teuerste ist die Testarbeit
  statt der Code: der positive `CheckMode.None`-Fall ist heute **ungetestet** (R3) und
  `KlausurModus_AbgebrochenerVersuch_…` muss durch E4 umgeschrieben werden (R2). Vorarbeit: `PeriodRange`
  teilbar machen.
- **2026-07-31** — `geschaetzt` → `in-arbeit`: E1–E7 gebaut. **615 Tests grün** (610 vorher, +5 neue),
  `full-flow.spec.ts` grün inkl. Klausur verlassen/fortsetzen im Browser, `dotnet build Pugling.sln` und
  `dotnet format --verify-no-changes` sauber. **Rot-Probe gefahren:** mit der alten Regel fällt
  `LernModus_ReineInhaltsuebung_BlosseAnwesenheit_…` (1 failed) — der Defekt ist also belegt, nicht behauptet;
  die beiden anderen neuen Tests bleiben unter beiden Regeln grün, wie es sein soll.
  Zwei Abweichungen von der Schätzung: **R4 löste sich günstiger** (der bestehende Info-Test prüft den
  Mode-Filter durch den leeren Pool jetzt wirklich — ein Lern-Gegenstück macht das Paar explizit, statt
  White-Box-Seeding), und ein EF-Fallstrick kam dazu: `Order` ist eine JSON-Spalte, `Order.Count` ist nicht
  übersetzbar — die Menge wird in der DB gefiltert, der Vergleich läuft im Speicher. Offen für die Abnahme:
  beide Reviewer und der Commit.
