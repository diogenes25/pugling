---
tags: [typ/story, status/gegrillt]
aliases: [Vokabel-Türme, Tower-Defense-Spiel]
status: gegrillt
prio: P3
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Nutzer im Dialog 2026-11-22; Plan: ../turm-spiel-plan.md"
grund: ""
ersetzt_durch: []
---

# B-822 · Vokabel-Türme: ein natives Tower-Defense-Spiel als eigener Übungsbereich

Ein eigenständiges Handyspiel (Android, später iOS) auf dem unveränderten Pugling-Backend: fest
aufgebaute Türme schießen Monster ab, besiegte Monster lassen Vokabeln fallen, die Spielfigur sammelt sie
ein und weist sie dem Turm mit der passenden Übersetzung zu — das ist dessen Munition. Der Server liefert
den Stoff, bewertet jede Zuweisung und schreibt den Lernstand fort. Spielablauf, Technik und Etappen:
[turm-spiel-plan.md](../turm-spiel-plan.md); die zehn Vorab-Entscheidungen vom 2026-11-22 stehen dort
unter „Entscheidungen" und werden hier nicht wiederholt.

## User Story

Als **Student** (Friedrich, Theodor) möchte ich meine Vokabeln in einem Handyspiel festigen, in dem ich
besiegten Monstern Vokabeln abnehme und sie dem Turm mit der richtigen Übersetzung bringe, damit
Wiederholen sich wie Spielen anfühlt und trotzdem in meinen Lernstand und meine Pflicht einzahlt.

Als **Supervisor** (Vater) möchte ich festlegen, ob und wie stark das Spiel auf eine Pflicht anrechnet und
wie viele Minuten am Tag gespielt werden darf, damit das Spiel das Üben ergänzt und nicht ersetzt.

## Ist-Stand am Code

**Lernstand je Vokabel — trägt sofort.**
`ItemProgressService.RecordAsync` schreibt Historie (`ItemReviewEvent`) und Leitner-Box je (Kind, Item)
fort und kennt schon den Anti-Farming-Schalter `countsForMastery`
([ItemProgressService.cs:29](../../backend/Pugling.Api/Services/Shared/ItemProgressService.cs:29), Abbruch
vor der Box bei `false`: [:51](../../backend/Pugling.Api/Services/Shared/ItemProgressService.cs:51)).
Die Quelle ist heute `Practice`/`Test`
([ItemProgressEntities.cs:4](../../backend/Pugling.Api/Models/ItemProgressEntities.cs:4)) — eine weitere
Quelle `Game` ist ein Enum-Wert, kein Umbau. Entscheidung 3 (Box gedämpft) ist damit direkt abbildbar.

**Pflicht — trägt nicht so, wie Entscheidung 1 sie beschreibt.** Eine Pflicht-Position einer prüfbaren
Übung gilt als erfüllt, sobald im Zeitraum ein **bestandener Positionstest** vorliegt:
`IsGoalMetAsync` fragt `TestAttempts … t.Passed && (!pos.RequireTypedTest || t.Graded)`
([PositionProgressService.cs:208](../../backend/Pugling.Api/Services/Shared/PositionProgressService.cs:208),
Abfrage [:216](../../backend/Pugling.Api/Services/Shared/PositionProgressService.cs:216)). Die Schwelle ist
ein **Prozentsatz richtiger Antworten im Test** (`GoalThreshold`,
[PlanPositionEntities.cs:75](../../backend/Pugling.Api/Models/PlanPositionEntities.cs:75)), der Test ist
auf zwei Versuche je Tag gedeckelt
([PositionProgressService.cs:155](../../backend/Pugling.Api/Services/Shared/PositionProgressService.cs:155)),
und `RequireTypedTest` ([PlanPositionEntities.cs:80](../../backend/Pugling.Api/Models/PlanPositionEntities.cs:80))
verlangt ausdrücklich getippte Antworten. Der Code selbst sagt: ein am Test gemessenes Ziel **hat keinen
Teilstand** ([PositionProgressService.cs:287](../../backend/Pugling.Api/Services/Shared/PositionProgressService.cs:287)).
Es gibt also **keinen Zähler richtiger Antworten**, auf den eine Spiel-Zuweisung „mit Faktor 0,5"
aufaddieren könnte — siehe Offener Punkt 1.

**Münzen mit Tagesdeckel — Vorbild vorhanden.** Das Bonus-Training bucht Münzen als
`ChildPointsEntry` (`PointKind.ExtraTrainingCoins`) und bumpt dabei `child.ConcurrencyStamp`
([ExtraTrainingService.cs:210](../../backend/Pugling.Api/Services/Student/ExtraTrainingService.cs:210),
[:220](../../backend/Pugling.Api/Services/Student/ExtraTrainingService.cs:220)); Deckel und Sätze sind
Konfiguration (`DailyCoinCap = 90`, [ExtraTrainingOptions.cs:14](../../backend/Pugling.Api/Services/Student/ExtraTrainingOptions.cs:14)).
Sein Run-Modell (`ExtraTrainingRun`,
[GamificationEntities.cs:124](../../backend/Pugling.Api/Models/GamificationEntities.cs:124); Start/Submit
[ExtraTrainingService.cs:67](../../backend/Pugling.Api/Services/Student/ExtraTrainingService.cs:67),
[:140](../../backend/Pugling.Api/Services/Student/ExtraTrainingService.cs:140)) ist die nächste
Verwandtschaft eines Spiel-Runs — allerdings mit **einem** Submit am Ende, während das Spiel je Zuweisung
eine Bewertung braucht.

**Eingefrorene Reihenfolge — Muster vorhanden.** Runden frieren ihren Stoff als `Order` + `Cursor` ein
([StudyPlanEntities.cs:128](../../backend/Pugling.Api/Models/StudyPlanEntities.cs:128)); ein Spiel-Run
kann dasselbe Muster nutzen, damit ein App-Neustart die Runde nicht neu würfelt.

**Login — vorhanden, aber gebremst.** `POST auth/child`
([AuthController.cs:68](../../backend/Pugling.Api/Controllers/AuthController.cs:68)) liefert das JWT; der
Login liegt hinter dem Rate-Limiter (Policy `login`). Eine native App kann das unverändert nutzen.

**Erreichbarkeit — HTTPS existiert.** Das Heimnetz-Skript serviert die echte Instanz über Tailscale als
HTTPS (`tailscale serve`, [heimnetz.ps1:172](../../scripts/heimnetz.ps1:172)); im LAN nur `http://IP:5200`
([:155](../../scripts/heimnetz.ps1:155)). Android blockt Klartext-HTTP standardmäßig — die Tailscale-Adresse
ist der Weg ohne Ausnahme im Manifest.

**Spielzeit-Limit — nicht vorhanden.** Kein Feld, keine Konfiguration für Minuten je Tag gefunden
(`grep MinutesPerDay|ScreenTime|DailyMinutes` über `Models`/`Services`: leer). Entscheidung 6 ist
vollständig neu.

**Native App / `game/` — nicht vorhanden.** Kein Godot-Projekt im Repo. Werkzeuge auf dem Entwickler-PC
(geprüft 2026-11-22): Android-SDK mit Build-Tools 35/36 und Plattformen 34–36, JDK 21, `adb` — **Godot
selbst ist nicht installiert**. `Pugling.Contracts`/`Pugling.Client` zielen auf `net10.0`; ob Godots C#
das direkt referenzieren kann, ist ungeprüft (Spike).

## Die echte Lücke

1. **Ein serverseitiger Spiel-Run mit Bewertung je Zuweisung**: Runde anlegen (Türme + Tokens, Lösung
   bleibt am Server), `assign` bewerten und Lernstand mit Quelle `Game` schreiben, Fehlzuweisung als
   zurückgelegtes Token führen (Entscheidung 2), Abschluss mit gedeckelter Münzbuchung (Entscheidung 4).
2. **Ein Weg, wie ein Spiel-Ergebnis eine Pflicht erfüllt** — den gibt es nicht, und der naheliegende
   (Teilzähler) widerspricht dem Pflichtmodell (Offener Punkt 1).
3. **Ein Minutenlimit je Kind** mit Vater-Einstellung und serverseitiger Durchsetzung.
4. **Die App selbst**: Godot-Projekt unter `game/`, Zwei-Daumen-Steuerung, Spiellogik, Netz-Client.

## Akzeptanzkriterien

Verteilt auf die Teilstories (Entscheidung 8); hier die Gesamtsicht.

- [ ] **B-850 Spike:** Ein Godot-4-C#-Projekt unter `game/` baut eine APK, die auf Friedrichs Android-Handy
  startet, per `auth/child` gegen die echte Instanz (Tailscale-HTTPS) ein JWT holt und den Namen des Kindes
  anzeigt. Gemessen und notiert: ob `Pugling.Contracts` referenzierbar ist (Entscheidung 7).
- [ ] **B-851 Backend-Run:** Der Run-Start liefert Türme und Tokens mit undurchsichtigen Ids und **ohne**
  Zuordnung, aus **einer** Position (Entscheidung 1); ein Integrationstest prüft, dass kein Payload die
  Paarung verrät, die Lösungsfeld-Regel bleibt grün.
- [ ] **B-851:** `assign` bewertet serverseitig, antwortet mit `correct`, `expected` und der neuen
  Turm-Übersetzung und schreibt ein `ItemReviewEvent` mit Quelle `Game`; die Box steigt höchstens einmal je
  Vokabel und Tag. Eine Fehlzuweisung legt das Token zurück, es erscheint später im selben Run erneut.
- [ ] **B-851:** Fremdes Kind bzw. fremder Run → `404`/`403` wie die übrigen Student-Endpunkte.
- [ ] **B-851:** Ein gewonnenes Level bucht Münzen über den Wallet-Pfad mit `ConcurrencyStamp`-Bump; der
  eigene Deckel (30/Tag, `Gamification:TowerGame`) hält auch bei parallelen `end`-Aufrufen (Entscheidung 4).
- [ ] **B-851:** Spielzeit per Heartbeat, serverseitig geclampt; ist das Tageslimit erreicht (Vorgabe 20
  Minuten), verweigert der Run-Start mit eigenem Fehlercode, ein laufendes Level darf noch höchstens 5
  Minuten weiterlaufen und wird normal gewertet (Entscheidung 5).
- [ ] **B-852 Pflicht-Anrechnung:** `PlanPosition` trägt „Level je Test" (Vorgabe 0 = aus). Bei N > 0 und
  ohne `RequireTypedTest` erfüllen N **bestandene Level** (gewonnen **und** Trefferquote ≥ Testschwelle) im
  Zeitraum die Pflicht wie ein bestandener Test; Integrationstest, der ohne die Anrechnung rot wäre, und
  einer, der bei `RequireTypedTest` die Anrechnung verweigert (Entscheidungen 1–2).
- [ ] **B-852:** Der Vater stellt „Level je Test" an der Position und das Minutenlimit je Kind im Vater-Web
  ein (Editor beidseitig: schreiben und zurücklesen).
- [ ] **B-851/B-852:** Jede Schemaänderung hängt eine Migration an die Kette an und nimmt die Bestandsdaten
  der echten DB mit.
- [ ] **B-853 Prototyp:** Eine Karte, 5 Türme, eine Monsterart, Sammeln mit Auswahlleiste, Zuweisen per
  Ja/Nein, Level-Ende nach 10 richtigen Zuweisungen — Friedrich oder Theodor spielt ein Level an der echten
  Instanz durch (Rollengang).

## Offene Punkte

1. ~~**Wie erfüllt das Spiel eine Pflicht?**~~ → Entscheidungen 1 und 2.
2. ~~**Wo sitzt der Faktor?**~~ → Entscheidung 3.
3. ~~**Eigenes Run-Modell oder `ExtraTrainingRun` erweitern?**~~ → Entscheidung 4.
4. ~~**Gemeinsamer oder eigener Münzdeckel?**~~ → Entscheidung 5.
5. ~~**Wie wird die Spielzeit gemessen?**~~ → Entscheidungen 6 und 7.
6. ~~**Schnitt in Teilstories.**~~ → Entscheidung 8.
7. ~~**Godot und `net10.0`.**~~ → Entscheidung 9.

## Entscheidungen

Gegrillt mit dem Nutzer am 2026-11-22. Sie verfeinern die zehn Vorab-Entscheidungen im
[Plan](../turm-spiel-plan.md); wo sie abweichen, gilt diese Liste.

1. **Ein Level zählt als Test, nicht als Teil-Fortschritt.** Ein Level zieht seinen Stoff aus **einer**
   Planposition. Es ist **bestanden**, wenn es gewonnen ist (die Monster sind nicht durchgebrochen) **und**
   die Trefferquote (richtige ÷ alle Zuweisungen) die Testschwelle der Position (`GoalThreshold`) erreicht.
   *Begründung:* Das Pflichtmodell kennt nur den bestandenen Test, keinen Teilzähler
   ([PositionProgressService.cs:287](../../backend/Pugling.Api/Services/Shared/PositionProgressService.cs:287));
   das Spiel dockt an genau dieser Stelle an, statt ein zweites Pflichtmaß einzuführen. *Kosten:* Kein
   Level quer über alle Pläne; `IsGoalMetAsync` bekommt einen zweiten Belegweg neben `TestAttempts`.
2. **Der Abschlag ist eine Anzahl: N bestandene Level ersetzen einen Test.** Die Vorab-Entscheidung
   „Faktor 0,5 auf die Trefferquote" ist verworfen — Quote × 0,5 erreicht nie 80 %, das Spiel hätte die
   Pflicht nie erfüllen können. Nie bei `RequireTypedTest`: dort hat der Vater Tippen verlangt.
   *Kosten:* Zählung bestandener Level je Position und Zeitraum (Tag/Woche wie `PeriodRange`).
3. **„Level je Test" ist eine Ganzzahl an der `PlanPosition`, Vorgabe 0 = Spiel zählt nicht.**
   Pflicht-Anrechnung ist eine bewusste Freigabe des Vaters, kein Default. *Kosten:* Feld + Migration,
   DTO-Feld (Vertragsbruch für Schreiber des Positions-DTOs wegen `unknown_field`), Formularfeld im Vater-Web.
4. **Eigenes Run-Modell** (`TowerGameRun`, `TowerGameRunItem`); Münzbuchung und Deckel-Logik aus dem
   Bonus-Training in einen gemeinsamen Baustein ziehen statt kopieren. *Begründung:* Bewertung je Zuweisung
   und zurückgelegte Tokens passen nicht in das Ein-Submit-Modell von `ExtraTrainingRun`.
   *Kosten:* zwei Tabellen, ein Umbau in `ExtraTrainingService` (Münzbuchung herauslösen).
5. **Eigener Münzdeckel, 30 je Tag**, konfigurierbar unter `Gamification:TowerGame` (Start: 10 Münzen je
   gewonnenem Level). *Begründung:* Spiel und Bonus-Training sollen sich nicht gegenseitig verdrängen; die
   Münzen bleiben der Hebel des Vaters. *Kosten:* eigener `PointKind`, eigene Options-Klasse.
6. **Spielzeit per Heartbeat**, serverseitig geclampt wie im Üben. *Begründung:* Die Wanduhr zählt eine
   vergessene App mit. *Kosten:* Heartbeat-Endpunkt, App meldet im Vordergrund alle ~15 s.
7. **Minutenlimit je Kind, Vorgabe 20 Minuten.** Ist es erreicht, startet kein neues Level; ein laufendes
   darf noch höchstens 5 Minuten weiterlaufen und wird normal gewertet. *Begründung:* Ein harter Stopp
   mitten im Level frustriert und bringt keinen Lerngewinn. *Kosten:* Feld am Kind + Migration,
   Vater-Web-Einstellung, Fehlercode beim Run-Start.
8. **Vier Teilstories, Spike zuerst:** B-850 Spike (App + Login aufs Handy), B-851 Backend-Run (Run,
   `assign`, Lernstand, Münzen, Minutenlimit), B-852 Pflicht-Anrechnung + Vater-Web, B-853 spielbarer
   Prototyp. Progression/Turm-Upgrades kommen später als eigene Idee. *Begründung:* Der Spike hat keine
   Abhängigkeit und entscheidet, ob der Technikweg trägt, bevor Backend-Arbeit dafür entsteht.
9. **Vertrags-Typen: im Spike messen, sonst spiegeln.** Kann Godots C# `Pugling.Contracts` nicht
   referenzieren, hält das Spiel eigene schlanke DTOs für die wenigen Spiel-Endpunkte. *Kosten:* im
   Fallback eine zweite Fassung weniger DTOs, gesichert über den Integrationstest des Backends und
   `UnmappedMemberHandling.Disallow`.

## Verlauf

- 2026-11-22 · `idee` angelegt aus dem Dialog; zehn Vorab-Entscheidungen im Plan gegrillt.
- 2026-11-22 · `ausformuliert`: Ist-Stand am Code belegt. Kernfund: das Pflichtmodell kennt nur den
  bestandenen Test, keinen Teilzähler — Entscheidung 1 braucht eine neue Form (Offener Punkt 1).
- 2026-11-22 · `gegrillt`: sieben offene Punkte mit dem Nutzer entschieden (Entscheidungen 1–9); Faktor
  auf die Trefferquote verworfen (rechnerisch unerreichbar), stattdessen „Level je Test". Geteilt in
  B-850..B-853.
