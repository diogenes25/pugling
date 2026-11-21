---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/student]
aliases: [Truhen-Prüfung doppelt, Kind-Tag auf dem Schreibweg]
status: abgenommen
prio: P3
art: Aufräumen
groesse: M
wo: backend
migration: nein
vertragsbruch: nein
quelle: "Backend-Review von B-606 im Nachtlauf 2026-11-17 (docs/pm-sitzung-2026-11-17-nachtlauf.md)"
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-11-22"
entgangen_bei: []
---

# B-609 · Bis die Truhe vergeben ist, rechnet jede Abgabe den Tag zweimal

Jede Abgabe (Runde beenden, Test abschließen) rechnet den Tagesstand des gespielten Plans zweimal voll durch, und die
Verlaufsschleifen laden die Positionen samt Fach-Kette jeden Tag neu, obwohl sie das Fach nie anzeigen. Das Ergebnis
ist richtig, nur nicht sparsam.

## User Story

Als Entwickler möchte ich, dass der Schreibweg nach einer Abgabe den Tag nur so oft rechnet, wie er gebraucht wird,
damit die Abgabe auch bei einem Plan mit vielen Positionen schnell antwortet, ohne dass sich eine Regel ändert.

## Ist-Stand am Code

Gelesen am 2026-11-19; **Zeilen gegenüber der Idee verschoben** (dort „~736" und „~450").

1. **Die Aufrufkette nach einer Abgabe.** `PositionPracticeController.cs:957-961` (Runde beenden) und
   `PositionTestsController.cs:466-471` (Test abschließen) rufen nacheinander
   `progress.EvaluateAndAwardAsync(plan, day)`, `gamification.EvaluateAndAwardAsync(childId, day)` und
   `dailyBox.EvaluateAndAwardAsync(childId, day)`. Beide Controller verwerfen den Rückgabewert der ersten.
2. **Erste Durchrechnung, bedingungslos:** `PositionProgressService.EvaluateAndAwardAsync`
   (`backend/Pugling.Api/Services/Shared/PositionProgressService.cs:540-588`) lädt die Positionen, bucht die
   Ziel-Belohnungen und endet mit `return await ComputeDayAsync(plan, day, ct)` (Zeile 587). Diesen Rückgabewert liest
   nur ein Test (`PositionGoalOverviewTests.cs:266-268`); der zweite Aufrufer in Tests
   (`RemediationPositionTests.cs:595`) verwirft ihn ebenfalls. Der Schreibweg bezahlt die volle Tagessicht also für
   nichts. Das ist **mehr**, als die Idee beschreibt (dort: nur die Wiederholung in der Truhe).
3. **Zweite Durchrechnung, bis die Truhe vergeben ist:** `DailyBoxService.EvaluateAndAwardAsync`
   (`DailyBoxService.cs:35-40`) prüft zuerst `AlreadyClaimedAsync` (Zeile 37) und kehrt dann zurück – danach entfällt
   sie. Davor ruft sie `ComputeChildDayAsync` (`PositionProgressService.cs:407-431`), das je aktivem Plan wieder
   `ComputeDayAsync` rechnet (Zeile 416).
4. **Was `ComputeDayAsync` je Position kostet** (Zeilen 305-360): `play.ItemsOfAsync` (326), bei Leitner
   `play.DueItemIndicesAsync` (328), `IsGoalMetAsync` und, wenn offen, `OpenGoalProgressAsync` (329-330), dazu eine
   Abfrage der eingeführten Items je Plan (312-316). Die Truhe braucht davon nur `GoalMet` und `Cadence` je Position
   (`RewardObligations`, Zeilen 369-374, `RewardsUnlocked` Zeile 428).
5. **Das Fach wird überall mitgeladen:** `LoadPositionsAsync` (Zeilen 284-289) lädt
   `Exercise → SeriesUnit → Series → Subject`. Gelesen wird das Fach nur in `SubjectNameOf`
   (Zeilen 391-392), das nur `ComputeDayAsync` aufruft (Zeile 344). Die Aufrufer ohne Anzeige:
   `ProgressAsync` (Zeilen 869-879, nutzt von `ComputeDayAsync` nur `DutyDone`, `GoalsTotal`, `GoalsMet`,
   `PointsAwarded`), die Zeitachse `ChildrenDashboardService.cs:77-83` (nur Zähler) und
   `EvaluateAndAwardAsync` selbst (Zeile 542, liest nur Übung und Titel für den Ledger-Text, Zeile 568, nie Reihe oder Fach).
6. **Die Schleifen laden je Tag neu:** `ComputeDayAsync` ruft `LoadPositionsAsync` bei **jedem** Aufruf (Zeile 307);
   `ProgressAsync` ruft es je Tag der Plandauer, die Zeitachse je Kind, Tag und Plan. Der Kommentar an der Funktion
   (Zeilen 282-283) benennt das („runs once per day of a history"), löst es aber nicht. Die Streak-Schleifen
   sind anders gebaut und laden einmal je Plan (Wörterbuch in `ChildStreakAsync`, Zeilen 487-496; `CompleteChildDaysAsync`
   Zeile 521).
7. **Messung:** keine gefunden (Suche nach `Stopwatch`/`BenchmarkDotNet` im Backend ohne einschlägigen Treffer; ein
   Messwert zur Abgabe-Latenz bei 4-15 Positionen steht nirgends). Die Idee vermutet „vermutlich nicht spürbar".
8. **Korrektur beim Grillen:** Das Vorbild `QueryPlanSmokeTests` zählt nichts – es prüft per `EXPLAIN QUERY PLAN` Indexe
   (`QueryPlanSmokeTests.cs:23-60`); ein `DbCommandInterceptor` oder `AddInterceptors` ist im Backend nicht gefunden. Die Messung
   braucht einen neuen Zähler (Entscheidung 1).

## Die echte Lücke

- **Echt, klein:** Die erste Durchrechnung auf dem Schreibweg (Beleg 2) hat keinen Leser. Ein Rückgabewert, den nur ein
  Test liest, treibt die volle Tagessicht bei jeder Abgabe. Das ist die billigste Einsparung und die einzige, die auch
  **nach** der Truhe weiterläuft.
- **Echt, mittel:** Die Verlaufsschleifen laden Positionen und Fach je Tag neu (Beleg 5-6) – ein Vielfaches der nötigen
  Abfragen, an der Planübersicht des Vaters (`…/overview/progress`) und der Zeitachse. *Beim Grillen relativiert (Entscheidung 3): je Tag kommen mehr als vier Befehle je Position
  hinzu, die Positions-Abfrage ist ein Befehl von mehr als 60; geschätzt, nicht gemessen.*
- **Fraglich:** Die zweite Durchrechnung (Truhe) entfällt, sobald die Truhe vergeben ist; bis dahin ist sie bei **jeder** Abgabe zu viel
  (nicht einmal je Tag, Entscheidung 4). Eine eigene schlanke Truhen-Prüfung (Vorschlag der Idee) wäre eine zweite Wahrheit für
  `RewardsUnlocked` und widerspricht B-611/B-612 („alle Zahlen kommen aus derselben Liste").

## Offene Punkte

1. ~~**Erst messen?**~~ → Entscheidung 1.
2. ~~**Rückgabewert von `EvaluateAndAwardAsync`:**~~ → Entscheidung 2.
3. ~~**Fach nur in der Tagessicht laden?**~~ → Entscheidung 3 (die Verlaufsschleifen gehören nicht zu dieser Story) und
   Entscheidung 5 (zurückgestellt).
4. ~~**Truhe:**~~ → Entscheidung 4.
5. ~~**Berührt das Regeln oder Münzen?**~~ → Entscheidung 6.

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-19 (Sonnet), unbestätigt.*

1. **Zuerst messen; die Messung ist Schritt 1 des Baus und Akzeptanzkriterium 1.** Der in der Story genannte Weg
   („EF-Befehlszähler, Vorbild `QueryPlanSmokeTests`“) trägt nicht: `QueryPlanSmokeTests` prüft per `EXPLAIN QUERY PLAN`
   Indexe und zählt nichts, und im Backend gibt es keinen `DbCommandInterceptor` (Suche über `backend/**/*.cs` ohne Treffer, auch
   `AddInterceptors` fehlt, `Program.cs:156-157` registriert den Kontext nackt). Die Messung braucht darum einen neuen,
   zählenden Interceptor im Testprojekt, eingehängt über `ConfigureServices` der Test-Fabrik
   (`PuglingWebAppFactory.cs:65`). Gemessen wird (a) eine Abgabe „Runde beenden“ bei einem Plan mit 5 und mit 15 Positionen,
   einmal mit offener, einmal mit vergebener Truhe, (b) `ProgressAsync` über 30 Tage bei 15 Positionen, getrennt nach
   Befehlen, die die Kette Position → Reihe → Fach laden (Befehlstext mit dem Join auf `TextbookSeries`), und allen übrigen.
   Verworfen: Stoppuhr-Zeiten (flackern unter Last) und Bauen ohne Zahl (die Story selbst vermutet „vermutlich nicht
   spürbar“). Kosten: ein Hilfsbau im Testprojekt vor dem eigentlichen Aufräumen; er bleibt als Zähler für spätere
   Messungen stehen. Ausgeschlossener Fall: ein Plan mit mehr als 15 Positionen; gegen die User Story („auch bei vielen
   Positionen“) deckt die lineare Zeile 5 → 15 das Wachstum ab, ein dritter Messpunkt ändert die Steigung nicht.
2. **Die unbenutzte Schluss-Rechnung in `EvaluateAndAwardAsync` entfällt, ohne eine Schwelle abzuwarten; die Methode
   liefert kein `DayOverview` mehr.** Begründung: Beide Controller verwerfen den Rückgabewert
   (`PositionPracticeController.cs:957`, `PositionTestsController.cs:466`), und Tests lesen ihn nur an einer Stelle
   (`PositionGoalOverviewTests.cs:266-268`; `RemediationPositionTests.cs:595` verwirft ihn). Die Rechnung hat keinen Leser
   im Produkt und ändert keine Ausgabe; sie hängt an keiner Buchung (`ComputeDayAsync` schreibt nichts). Die zwei Test-Aufrufer
   lesen künftig `ComputeDayAsync` nach der Buchung. Der Kommentar im `catch` („the day status below is read fresh from the
   database anyway“, `PositionProgressService.cs:584`) wird angepasst, `db.ChangeTracker.Clear()` (`:585`) bleibt. Verworfen: den Rückgabewert behalten und einen
   zweiten Lesepfad bauen (Pflege ohne Nutzen). Kosten: ein Signaturwechsel mit zwei Testanpassungen. Ausgeschlossener Fall:
   ein künftiger Aufrufer, der die Tagessicht direkt nach der Buchung braucht – er ruft `ComputeDayAsync` selbst; gegen die
   User Story trägt das, weil sie nur verlangt, dass der Schreibweg nicht öfter rechnet, als er braucht.
3. **Die Verlaufsschleifen (`ProgressAsync`, Zeitachse) gehören nicht zu dieser Story; die Messung (b) entscheidet, ob eine
   eigene entsteht.** Begründung, gegen die User Story gerechnet: Sie handelt von der **Abgabe** („der Schreibweg nach einer
   Abgabe“); `ProgressAsync` (`PositionProgressService.cs:869-879`) und `ChildrenDashboardService.cs:77-83` sind Lesewege des
   Vaters. Dazu die Rechnung am Code: je Tag lädt `ComputeDayAsync` die Positionen einmal (`:307`), dann je Position
   mindestens `ItemsOfAsync`, `DueItemIndicesAsync` (mit eigener Item- und Fortschrittsabfrage,
   `PositionPlayService.cs:273-282`) und `IsGoalMetAsync` (`:326-330`), also mehr als vier Befehle je Position. Bei 15
   Positionen ist die Positions-Abfrage mit ihrem Fach-Join ein Befehl von mehr als 60 je Tag, unter zwei Prozent
   (geschätzt, nicht gemessen). Wer sie aus der Schleife hebt, spart fast nichts und ändert drei Aufrufer. Schwelle: macht
   die Positions-Fach-Abfrage in Messung (b) mindestens 20 Prozent aller Befehle aus, entsteht eine eigene Aufräumen-Story
   (Id vorher neu zählen); sonst steht im Verlauf „Verlustposten, nicht gebaut“. Die Zahl 20 ist eine Setzung ohne Messung:
   darunter ist die Ersparnis im Verlauf einer Planübersicht nicht spürbar. Verworfen: Verlauf mitbauen (kostet drei
   Aufrufer, trägt die Story nicht) und Streichen ohne Messung (die Schätzung ist eine Rechnung, kein Befund). Kosten: die
   Fach-Kette bleibt in `LoadPositionsAsync` (`:284-289`), sodass [B-614](B-614-startseite-gruppiert-nach-uebungsfach-statt-planfach.md)
   (Rückfall auf das Reihen-Fach) unberührt bleibt. Ausgeschlossener Fall: ein Vater mit sehr langem Verlauf und vielen
   Positionen, dem die Planübersicht zu langsam ist – seine Zeit sinkt hier nicht, siehe Entscheidung 5.
4. **Die Truhen-Rechnung wird nicht angefasst: `RewardObligations` bleibt die einzige Auswahl der Belohnungsziele.**
   Begründung: Sie wird an genau zwei Stellen gerufen (je Plan `PositionProgressService.cs:355`, über das ganze Kind `:421`);
   eine schlanke Truhen-Prüfung wäre eine dritte Regel für `RewardsUnlocked` und widerspräche
   [B-612](B-612-startseite-zaehlt-belohnungsziele-anders-als-der-server.md) („aus derselben Liste“). Verworfen: eine eigene
   Prüfung mit nur `GoalMet` und `Cadence`. Kosten: bis die Truhe vergeben ist, bezahlt **jede** Abgabe eine Tagessicht über
   alle aktiven Pläne – nicht „einmal je Tag und Kind“, wie die Idee es sagt, sondern bei einem Tag mit vier Pflichten für
   die ersten drei Abgaben (`DailyBoxService.cs:35-40`: erst `AlreadyClaimedAsync`, dann `ComputeChildDayAsync`). Nach
   Entscheidung 2 ist das die einzige Tagessicht je Abgabe. Ausgeschlossener Fall: eine Abgabe, die früh erkennen könnte, dass
   die Truhe noch zu ist, ohne zu rechnen; gegen die User Story (nicht öfter rechnen als nötig) ist das der Preis, weil die
   frühe Erkennung selbst die Regel wäre.
5. **Zurückgestellt: die Verlaufsansicht in Stapelabfragen statt Tag mal Position.** Der eigentliche Hebel der Verlaufsschleifen sind
   die Abfragen je Position und Tag (Entscheidung 3), nicht das Laden der Positionen. Das ist ein Umbau der Rechenform
   (Sitzungen und Versuche eines Plans einmal laden, Tage im Speicher rechnen) mit eigenem Risiko für Regeln wie
   `ActiveOn`. Wann: erst wenn Messung (b) eine absolute Befehlszahl zeigt, die der Nutzer für zu viel hält; eine Zahl
   dafür setzt dieser Lauf nicht.
6. **Keine Regel und keine Münze ändert sich.** Die Reihenfolge Ziel-Belohnung → Gamification → Truhe bleibt
   (`PositionPracticeController.cs:957-961`, `PositionTestsController.cs:466-471`); entfernt wird nur eine Rechnung, deren
   Ergebnis niemand liest. Der Beleg ist, dass die bestehenden Tests unverändert grün bleiben (Akzeptanzkriterium 3).

## Akzeptanzkriterien

Je Kriterium: die Änderung, die es rot macht. Kriterium 1 liegt vor jedem Umbau.

1. **Messung, vor dem Umbau:** Ein Test mit zählendem Interceptor misst die Befehle einer Abgabe bei 5 und 15 Positionen
   (Truhe offen und vergeben) und die Befehle von `ProgressAsync` über 30 Tage bei 15 Positionen, mit dem Anteil der
   Positions-Fach-Abfrage. Die Zahlen stehen im `## Verlauf`. Der Test prüft zuerst, dass der Zähler für eine bekannte
   Abfrage mehr als null zählt. Rot, wenn der Interceptor nicht eingehängt ist (alle Zahlen null, die Messung bewiese
   „kostet nichts“).
2. **Eine Tagessicht weniger je Abgabe nach vergebener Truhe:** Die Befehlszahl einer Abgabe bei vergebener Truhe liegt
   höchstens bei der Messzahl aus Kriterium 1 abzüglich der Kosten einer direkten Tagessicht desselben Plans (beide
   Konstanten stehen nach Schritt 1 fest und im Test, mit dem Messdatum im Kommentar). Rot heute (die Zahl ist die
   Messzahl selbst); rot, wenn die Schluss-Rechnung in `EvaluateAndAwardAsync` stehen bleibt.
3. **Verhalten unverändert:** `DailyBoxTests`, `PositionGoalOverviewTests`, `ChildrenOverviewTests`, `KindTagessichtTests` bleiben
   bis auf die zwei Aufrufer aus Entscheidung 2 unverändert grün. Rot, wenn der Umbau die Reihenfolge Ziel-Belohnung →
   Truhe verschiebt (`DailyBoxTests`: Truhe ohne gebuchte Ziel-Belohnung) oder `RewardsUnlocked` anders rechnet.
4. **Der Schreibweg liefert nichts mehr zurück:** `PositionGoalOverviewTests.cs:266-268` liest die Tagessicht mit
   `ComputeDayAsync` nach der Buchung. Rot, wenn die Buchung in `EvaluateAndAwardAsync` entfällt (dann fehlen die Punkte
   in der gelesenen Tagessicht).
5. **Keine zweite Wahrheit (Durchsicht, kein Tor):** `RewardObligations(` hat im Quelltext genau die zwei Aufrufstellen aus
   Entscheidung 4, `RewardsUnlocked` keine dritte Berechnung. Rot, wenn eine eigene Truhen-Prüfung die Regel nachbaut;
   als `grep` bei der Abnahme beschriftet, weil ein Zähltor auf Quelltext dieselbe Regel nur an einer anderen Stelle
   festhielte.
6. **Die Entscheidung über den Verlauf ist belegt (Absichtserklärung, kein Test):** Im `## Verlauf` steht der Anteil der
   Positions-Fach-Abfrage aus Messung (b) und daraus „eigene Story (Id)“ oder „Verlustposten, nicht gebaut“. Rot, wenn
   die Zeile fehlt, weil die Abnahme dann die Messung nicht benutzt hat.

## Schätzung

**Größe: M** — wie der Batch-Pfad im `MediaSelector`: ein kleiner Eingriff an einer Stelle (die Signatur von
`EvaluateAndAwardAsync`), aber mit eigenem Hilfsbau davor (ein zählender Interceptor samt Messtest), den kein
Vorgänger im Repo liefert. Ohne die Messung wäre es ein S; sie ist Akzeptanzkriterium 1 und gehört dazu.

- **`migration: nein`** — nachgesehen: Die Story ändert weder eine Entität noch eine Spalte; `ComputeDayAsync` schreibt
  nichts (`PositionProgressService.cs:305-360`), und die Buchung in `EvaluateAndAwardAsync` bleibt, wie sie ist.
- **`vertragsbruch: nein`** — nachgesehen: `DayOverview` liegt zwar in `Pugling.Contracts`
  (`backend/Pugling.Contracts/Shared/ProgressDtos.cs:57`), bleibt aber Rückgabetyp von `ComputeDayAsync`
  (`PlanOverviewController.cs:40` liest ihn weiter). Geändert wird nur die Signatur des Dienstes
  `PositionProgressService.EvaluateAndAwardAsync` – er ist kein Endpunkt, und kein Controller gibt den Wert heraus (beide
  verwerfen ihn, `PositionPracticeController.cs:957`, `PositionTestsController.cs:466`). `Pugling.Client` und das
  Frontend sind nicht betroffen.
- **`wo: backend`** — Dienst und Testprojekt; im Frontend steht nichts.

### Risiken

1. **Der Zähler muss sich überhaupt einhängen lassen.** `Program.cs:156-157` registriert `AddDbContext` ohne Interceptor,
   und `PuglingWebAppFactoryBase.ConfigureWebHost` ist versiegelt; der Haken ist `ConfigureFactory` (Muster:
   `DailyBoxRangeFactory`, `DailyBoxRangeTests.cs:112-132`). Ob EF einen als `IInterceptor` im Anwendungs-Container
   registrierten Zähler von allein aufnimmt, ist hier **nicht belegt** (Paket 10.0.15). Fällt der Weg aus, bleibt, den
   Kontext in der Factory-Unterklasse mit `AddInterceptors` neu zu registrieren (Vorlage für den Einhängeweg pro Kontext:
   `ShopStockPerChildTests.cs:436`, dort ein `SaveChangesInterceptor`, kein Befehlszähler – die Aussage der Entscheidung
   1 „`AddInterceptors` fehlt“ stimmt nur für `backend/Pugling.Api`). Die Vorprobe aus Akzeptanzkriterium 1 (Zähler
   größer null) fängt beide Wege.
2. **Pläne mit 5 und 15 Positionen gibt es im Testbau nicht.** `TestApi.SeedLeitnerPosition` (`TestApi.cs:260`) legt je
   Aufruf einen **eigenen** Plan mit einer Position an (`TestApi.cs:307-345`). Für „ein Plan, N Positionen“ braucht der
   Messtest einen lokalen Aufbau (`StudyPlan` mit `Positions`), der die Regel „ein aktiver Plan je Fach“ (B-539) umgeht,
   weil er direkt in die DB schreibt – gewollt, aber im Kommentar zu benennen.
3. **Die Zahlen in Akzeptanzkriterium 2 sind fest verdrahtet** und werden rot, sobald eine fremde Änderung eine Abfrage in
   der Tagessicht hinzufügt – aus einem anderen Grund als diese Story. Besser, sie im selben Lauf **relativ** zu messen:
   Befehle der Abgabe gegen Befehle einer direkten `ComputeDayAsync`-Sicht desselben Plans, beide aus derselben Messung.
   Das ändert keine Entscheidung, nur die Form der Konstanten; beim Bau zu wählen.
4. **Reihenfolge Ziel-Belohnung → Gamification → Truhe darf sich nicht verschieben** (Entscheidung 6): Die Buchung steht
   in `EvaluateAndAwardAsync` vor der entfallenden Zeile 587 und bleibt unberührt; `DailyBoxTests` ist das Netz.
5. **Anzahl der Testanpassungen:** Entscheidung 2 spricht von zwei; nachgesehen braucht nur
   `PositionGoalOverviewTests.cs:266-268` eine Änderung (liest `overview.DutyDone`). `RemediationPositionTests.cs:595`
   verwirft den Wert und kompiliert unverändert.

### Angriffsplan

1. **Messschritt M1 (Akzeptanzkriterium 1, vor jedem Umbau):** `CommandCounter : DbCommandInterceptor` im Testprojekt, eine
   Factory-Unterklasse über `PuglingWebAppFactoryBase.ConfigureFactory`, der Messtest `AbgabeBefehlszaehlerTests`. Erst die
   Vorprobe (bekannte Abfrage zählt mehr als null), dann Messung (a) Abgabe bei 5 und 15 Positionen mit offener und
   vergebener Truhe, (b) `ProgressAsync` über 30 Tage bei 15 Positionen, getrennt nach Befehlen mit dem Join auf
   `TextbookSeries`. Zahlen in den `## Verlauf`.
2. `EvaluateAndAwardAsync` liefert `Task`; die Schluss-Rechnung und der veraltete Kommentar im `catch`
   (`PositionProgressService.cs:577-586`) entfallen bzw. werden angepasst; `PositionGoalOverviewTests.cs:266-268` liest
   `ComputeDayAsync` nach der Buchung.
3. Die Konstanten aus Schritt 1 in den Test schreiben (Akzeptanzkriterium 2), mit Messdatum im Kommentar.
4. Durchsicht Akzeptanzkriterium 5 (`grep` auf `RewardObligations(`) und Eintrag der Entscheidung zu Akzeptanzkriterium 6
   im `## Verlauf`: Anteil der Positions-Fach-Abfrage, daraus „eigene Story“ oder „Verlustposten“.
5. `dotnet build Pugling.sln` und `dotnet format Pugling.sln` vor dem Commit.

### Testweg

- **Neu:** `AbgabeBefehlszaehlerTests` (Akzeptanzkriterien 1 und 2) mit eigener Factory. Zählen ist deterministisch (keine
  Zeiten), also kein Flacker. Rot heute: Kriterium 2 (die Zahl ist die Messzahl selbst); rot, wenn der Zähler nicht
  eingehängt ist (Vorprobe).
- **Unverändert grün:** `DailyBoxTests`, `PositionGoalOverviewTests` (bis auf die eine Stelle), `ChildrenOverviewTests`,
  `KindTagessichtTests`, `RemediationPositionTests` (Akzeptanzkriterien 3 und 4); Lauf über das Test-Tor
  (`dotnet test Pugling.sln -c Release`).
- **Messschritt als eigener Befehl:** `dotnet test backend/Pugling.Api.Tests -c Release --filter AbgabeBefehlszaehlerTests`;
  die Zahlen kommen über `ITestOutputHelper` und werden von Hand in den Verlauf übertragen (gezählt wird „N passed“, nicht
  der Exit-Code).
- Vor der Abnahme: Agent `pugling-reviewer`; ein Rollengang als Sohn (Runde beenden mit Truhe) genügt, weil sich kein
  sichtbares Verhalten ändert.

## Verlauf

- **2026-11-17** — angelegt aus dem Backend-Review von B-606 (🟢, ohne Zählung).
- **2026-11-19** — ausformuliert (Vorbereitungslauf): Aufrufkette neu belegt (Zeilen gegenüber der Idee verschoben);
  größerer Posten gefunden – die erste Durchrechnung in `EvaluateAndAwardAsync` (Zeile 587) hat keinen Leser auf dem
  Schreibweg; die schlanke Truhen-Prüfung der Idee würde eine zweite Wahrheit schaffen; Messung als erster Schritt
  empfohlen.
- **2026-11-19** — gegrillt (Vorbereitungslauf, Sonnet, stellvertretend, unbestätigt): Entscheidungen 1 bis 6. Zuerst messen (Messung ist Kriterium 1); Vorbild `QueryPlanSmokeTests` trägt nicht (EXPLAIN, kein Zähler), im Backend gibt es keinen Befehlszähler. Die tote Schluss-Rechnung in `EvaluateAndAwardAsync` fällt ohne Schwelle; die Verlaufsschleifen gehören nicht zur User Story (Abgabe) und werden nur bei mindestens 20 Prozent Anteil eigene Story; Truhe bleibt unangetastet. Akzeptanzkriterien final, je mit roter Änderung.
- **2026-11-19** — geschaetzt (Vorbereitungslauf, Sonnet): M, backend, keine Migration, kein Vertragsbruch (nachgesehen: `DayOverview` bleibt Rückgabetyp von `ComputeDayAsync`, `EvaluateAndAwardAsync` ist ein Dienst ohne Endpunkt). Größer als der Eingriff selbst ist der Hilfsbau (Zähler, Plan mit N Positionen); beides benannt. Nebenfund: nur eine statt zwei Testanpassungen nötig; `AddInterceptors` fehlt nur in `backend/Pugling.Api`, im Testprojekt gibt es ein Vorbild (`ShopStockPerChildTests.cs:436`). Nichts gemessen oder gestartet.
- **2026-11-21** — in-arbeit (Nachtlauf, Sprint 109). **Messung vor jedem Umbau** (AK 1, `SchreibwegMessungTests`, zählender `DbCommandInterceptor` in eigener Test-Fabrik `CommandCountingWebAppFactory`): Tagessicht eines Plans 34 Befehle (5 Positionen) bzw. 94 (15); Abgabe (Test bestanden) mit offener Truhe 100 bzw. 230, mit vergebener Truhe 66 bzw. 136. Verlauf (`…/overview/progress`) über 30 Tage bei 15 Positionen: 3261 Befehle, davon 36 mit der Positions-Fach-Kette (Join `PlanPositions` × `TextbookSeries`) = **1,1 %**.
- **2026-11-21** — AK 6 (Entscheidung 3): 1,1 % liegt unter der Schwelle von 20 % → **Verlustposten, nicht gebaut**, keine eigene Story. Der Hebel des Verlaufs sind die Abfragen je Position und Tag (Entscheidung 5, zurückgestellt; 3261 Befehle für eine Planübersicht über 30 Tage ist die Zahl, an der der Nutzer das entscheiden kann).
- **2026-11-21** — Nach dem Umbau (Entscheidung 2, `EvaluateAndAwardAsync` liefert nichts mehr): Abgabe mit vergebener Truhe 66 → **32** (−34 = genau eine Tagessicht), 136 → **42** (−94); mit offener Truhe 100 → 66, 230 → 136. Fälle → Tests: AK 1 → `Messung_Abgabe`, `Messung_Verlauf` (`[Trait("Category", "Measurement")]`, prüfen nur, dass der Zähler zählt); AK 2 → `Nach_Vergebener_Truhe_Rechnet_Die_Abgabe_Keine_Tagessicht` (5 und 15 Positionen), R1 (Tagessicht wieder auf dem Schreibweg) → rot; AK 3 → `DailyBox*`, `PositionGoalOverviewTests`, `ChildrenOverviewTests`, `KindTagessichtTests` grün (64/64), nur der eine Leser aus Entscheidung 2 angepasst; AK 4 → `NebenlaeufigeZielbuchung_VerliertDasRennen_OhneFehlerUndOhneDoppelbuchung` liest die Tagessicht mit `ComputeDayAsync` nach der Buchung, R2 (Buchung entfällt) → rot; AK 5 → `grep "RewardObligations(" backend/Pugling.Api` (ohne Doku-Zeilen): `PositionProgressService.cs:356` und `:432`, keine dritte Stelle (dazu ein Testaufruf `RemediationPositionTests.cs:469`).
- **2026-11-21** — Mutanten nach der letzten Änderung (`mutant-probe.py`, Debug, Filter `SchreibwegMessungTests|PositionGoalOverviewTests`):

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | R1 Tagessicht auf dem Schreibweg | `PositionProgressService.cs:554` | red | red |
  | R2 Buchung entfällt | `PositionProgressService.cs:558` | red | red |
  | R3 `ChangeTracker.Clear()` entfällt | `PositionProgressService.cs:597` | red | red |

- **2026-11-21** — Review (`pugling-reviewer`): ein 🟡 (zwei Seiten): die Obergrenze aus AK 2 als **Gesamtzahl** (≤ 66 − Tagessicht) hatte keinen Spielraum – jede fremde Abfrage auf dem Abgabeweg oder in der Tagessicht hätte sie rot gemacht, eine billigere Tagessicht sie still gelockert. Behoben: AK 2 prüft jetzt **strukturell** – die eine Abfrage, die nur eine Tagessicht absetzt (eingeführte Inhalte eines ganzen Plans, `PositionItemProgress` × `PlanPositions` über `StudyPlanId`), kommt bei vergebener Truhe 0-mal vor, bei offener 1-mal; die Tagessicht selbst muss sie genau einmal zeigen (prüft den Marker). Die Messzahlen stehen im Doc-Kommentar. Die Wortlaut-Abweichung vom Kriterium („höchstens die Messzahl abzüglich …“) ist gewollt: dieselbe Absicht, ohne Fehlalarm. **Zähler 1 von 5.** 🟢 ohne Zählung übernommen: `SaveChangesAsync` nach dem Verlierer im Renn-Test (hält, wofür `ChangeTracker.Clear()` da ist – R3), Doc-Satz an `EvaluateAndAwardAsync`, `Trait` an den Messfällen, „Befehle, nicht Rundreisen“ am Zähler, Zeit der Test-Truhe aus der Test-Uhr. Volle Suite (Release) 2004/2004.
- **2026-11-21** — abgenommen (Sprint 109). Kein UI-Diff; Rollengang Sohn über die laufende App: E2E `full-flow` (Abgabe bucht Punkte), `belohnung-ueber-faecher` (Truhe nach erfüllten Tageszielen), `bonus` (Sperre nach Pflicht) grün, Server frisch von Playwright auf 5201/5174 nach der letzten Änderung. Vater und Creator: Regressionszeugen, keine ihrer Flächen im Diff. Commit `9482ec75`.
- **2026-11-22** — nachgeschaut (Nachschau Sprint 109 im Nachtlauf A): alle Aufrufer von `EvaluateAndAwardAsync` verwerfen das Ergebnis (`PositionPracticeController.cs:957`, `PositionTestsController.cs:466`, zwei Tests); nach dem `ChangeTracker.Clear()` lesen `gamification`/`dailyBox` frisch. Kein Befund.
