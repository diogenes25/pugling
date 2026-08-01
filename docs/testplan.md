---
tags: [bereich/qualitaet, bereich/tests]
---

# Kontrolle: Erfüllen die Integrationstests ihren Zweck?

Status: **Abgeschlossen am 2026-07-30** auf `3cd7aae`, gemessen in vier Wegwerf-Worktrees.
30 Injektionen komplett gefahren, Kalibrierung nachweislich rot; die vier Lücken mit Geldwirkung sind
geschlossen und per Gegenprobe belegt. Ergebnis unter
[Befund](#befund-gemessen-2026-07-30) – **Konformität 60 %, Sensitivität 57 %**.
Der Plan darüber bleibt als Beschreibung des Verfahrens stehen (nachfahrbar), die Zahlen darin sind
Vorab-Schätzungen; wo die Messung sie korrigiert, steht es im Befund.

## Warum dieser Vorgang

Das Projekt hat eine große Integrationstest-Suite: **85 Dateien, 518 Testmethoden (= 587 Testfälle mit den
`Theory`-Zeilen), 14.105 Zeilen** in
[backend/Pugling.Api.Tests](../backend/Pugling.Api.Tests), gefahren in-process über
[PuglingWebAppFactory](../backend/Pugling.Api.Tests/PuglingWebAppFactory.cs) gegen eine Wegwerf-SQLite je
Testklasse. Dazu sieben reflexive Wächter und der
[EndpointCoverageGuard](../backend/Pugling.Api.Tests/EndpointCoverageGuard.cs).

**Was heute gemessen wird:** *dass* jede Controller-Action von einem Test mit Status < 400 erreicht wird
(268/268), Zweigabdeckung 69,2 %, Zeilenabdeckung 98,2 % (in
[codequalitaet-gates-plan.md](codequalitaet-gates-plan.md) selbst als „irreführend hoch" markiert).

**Was heute niemand messt: ob ein Test einen Defekt auch *bemerkt*.** Erreicht ≠ geprüft.

Die Begründung trägt **ein** Indiz, nicht zwei: die 88 Tests aus Etappe C entstanden, **um eine
Abdeckungslücke zu schließen**, nicht um eine Regel zu pinnen – genau das Muster, aus dem flache Tests
entstehen. Ein zweites Indiz („96 Tests prüfen nur Statuscodes") hat die Nachprüfung nicht überlebt, siehe
den nächsten Abschnitt.

Ziel: ein belegter Befund, **welche Geschäftsregeln bei absichtlicher Verletzung rot werden und welche
nicht**, plus die Schließung der Lücken mit Geldwirkung. Kein Umbau der Suite, keine Abdeckungsquote als
Ziel – dieselbe Haltung wie im Gates-Plan.

### Drei Zahlen, die bei der Nachprüfung geschrumpft sind

Alle drei ließen den Mangel größer erscheinen, als er ist. Sie stehen hier, damit der Plan nicht mit
Zahlen argumentiert, die er selbst nicht hält:

| erste Behauptung | nachgeprüft |
|---|---|
| 96 Testmethoden prüfen nur Statuscodes | **66** sind reine Negativtests (der Status *ist* die Aussage), 3 Lesepfade, **27** auf Schreibpfaden – und die meisten davon belegen den Effekt sehr wohl über einen Status (`DELETE → 204`, dann `GET → 404`). Echte Verdächtige: **~8** |
| Sechs Wächter ohne Selbstschutz gegen falsch-grün | **zwei** urteilen über eine reflexiv ermittelte Fläche (`ErrorCodeTests:159`, `OpenApiExampleTests` vergleichen gegen Listen aus dem OpenAPI-Dokument). `PointKindCurrencyTests` iteriert `Enum.GetValues<PointKind>()` (nie leer), `PatchClearFieldTests`/`UnknownFieldTests`/`ApiVersioningTests` prüfen feste Einzelfälle |
| „Prüfen, ob das Tor wirklich blockt" | **schon belegt** in Etappe A4 des Gates-Plans (absichtlich gebrochener Test → `exit 2` mit Namen; Fingerprint-Treffer → `exit 0`), CI über den D0-Nachweis. Fällt hier weg |

## Entschiedene Rahmenbedingungen

Ergebnis der Durchsprache – diese acht Punkte sind gesetzt, nicht mehr offen:

1. **Einmalige Messung.** Keine versionierte Patch-Serie, kein CI-Job. Der Wert steckt im Befund.
2. **Zwei Commits.** Erst Messung + Bericht, dann die fehlenden Tests.
3. **50/50-Ziehung.** Die Hälfte der Injektionen aus dokumentierten Regeln (*Konformität*: hält die Suite,
   was `CLAUDE.md` behauptet?), die andere Hälfte blind gezogen (*Sensitivität*: bemerkt die Suite einen
   beliebigen Fehler?). **Beide Quoten getrennt berichten** – der Plan hatte die zwei Fragen vermischt.
4. **„Blind" heißt mechanisch.** Jeder k-te *ausgeführte* Zweigpunkt der Risiko-Services, sortiert nach
   Datei:Zeile. Unverletzbare Stelle → überspringen **und mit Grund protokollieren** (sonst kehrt die
   Verzerrung durch die Hintertür zurück).
5. **Urteil in drei Klassen plus Streubreite** (s. Etappe 3).
6. **E2E zählt nicht mit.** Bewertet wird nur, was ein Tor fahren kann. Dass die 10 Playwright-Specs unter
   `frontend/e2e/` in keiner CI laufen und darum nichts verhindern, wird als Satz im Bericht vermerkt.
7. **Etappe 4 auf zwei Punkte gekürzt** (Wiederholbarkeit, Reihenfolge).
8. **Fester Nenner: 30 Injektionen**, komplett durchgefahren – nur bei festem Nenner ist das Ergebnis eine
   Quote und keine Anekdote. Kalibrier-Injektion zuerst. Nachzug im zweiten Commit: **alles mit
   Geldwirkung**, der Rest bleibt benannte Restliste.

---

## Etappe 0 · Arbeitsplatz sichern (zuerst, nicht nachher)

Die Injektionen ändern Produktivcode. Drei Fallen im Arbeitsbaum:

1. **Der Baum ist unsauber.** `git status` zeigt uncommittete Arbeit (CancellationToken-Nacharbeit,
   `ClientAbortExceptionHandler.cs`, `docs/backlog-vokabellernen.md`). Vor der ersten Injektion committen
   oder stashen – sonst ist `git restore` nach einer Injektion nicht mehr risikofrei.
2. **Deshalb: eigener Worktree.** `git worktree add ../pugling-testaudit <commit>` und dort injizieren.
   Ein `git restore .` betrifft dann nie die Hauptarbeit. (Fallstrick aus dem Gates-Plan mitnehmen: die
   Worktree-Basis bewusst wählen, nicht `main` annehmen.)
3. **`PUGLING_SKIP_TEST_GATE=1` setzen.** Der Stop-Hook fährt sonst nach jedem Edit die Suite und kämpft
   gegen die absichtlich roten Zwischenstände. Außerdem: **`DocsCaptureTests` überschreibt
   `docs/api-examples/` bei jedem Lauf** – nach jeder Injektion mit `git restore` wegräumen, sonst wandern
   falsche Beispiele in den Diff.

Referenzlauf zum Vergleich: `dotnet test Pugling.sln -c Release` (~63 s) muss **vor** der ersten Injektion
grün sein, sonst ist jedes „rot" mehrdeutig. Läufe mit `--logger trx`, weil die Streubreite die Namen der
gefallenen Tests braucht.

---

## Etappe 1 · Statische Sichtung + Ziehungsgrundlage

**1a Zusicherungs-Tiefe (klein geworden).** Zu prüfen sind die **~8** Tests, die einen Erfolg auf einem
Schreibpfad zusichern, ohne den Effekt nachzulesen – nicht 96. Namentlich u. a.
`AntiCheatTests.Vater_DarfFremdenTagNachtragen` (200, aber wurde der Tag nachgetragen?),
`AntiCheatTests.Vater_DarfInaktivenPlanTrotzdemDurchspielen`,
`EmptyExerciseGuardTests.Aufsatz_OhneItems_BleibtZuweisbar`. Die 27er-Liste je Test durchsehen und die
aussortieren, die ihren Effekt über einen Folgestatus belegen (`204` → `404`) – das ist eine gültige
Zusicherung, kein Mangel.

**1b Tautologien.** Suchen, wo ein Test den erwarteten Wert **vom Server selbst** bezieht und
zurückspiegelt – dann kann die geprüfte Logik beliebig falsch sein und der Test bleibt grün. Hochriskante
Stelle ist der server-autoritative Antwortpfad: Woher nehmen `ReviewGradingTests`, `PositionTestFlowTests`,
`PositionPracticeFlowTests` die *richtige* Antwort? Aus dem Karten-Payload (tautologisch) oder aus im Test
hart hinterlegten Vokabeln (belastbar)? Gleiche Frage für Punkte-Erwartungen gegen `ScoringService`.

**1c Stumme Wächter (zwei Kandidaten).** Ein reflexiver Wächter, dessen Reflexion nicht greift, findet 0
Verstöße und ist grün – **schlimmer als kein Wächter**, weil er Deckung behauptet. Fünf Wächter tragen die
Untergrenze schon (`Assert.True(files.Length >= 30)` usw., geprüft). Zu entscheiden bleibt sie für
`ErrorCodeTests` und `OpenApiExampleTests`: dort ist zu prüfen, ob eine **leere** (nicht: fehlende) Liste aus
dem OpenAPI-Dokument die Zusicherung vakuum-grün werden lässt – das hängt an der Assert-Richtung. Eine
nötige Untergrenze gehört in den **zweiten** Commit.

**1d Coverage-Lauf – Vorbedingung für Etappe 3, nicht Nebenstrang.** Aus dem Cobertura-Bericht die
ausgeführten Zweigpunkte ziehen; sie sind die **Ziehungsgrundlage** der blinden Hälfte. Der Grund ist
zwingend: **eine Injektion in nie genommenen Code ist zwangsläufig grün** und messt nichts – sie wiederholt
nur den Coverage-Bericht. Zusätzlich die 30,8 % ungenommenen Zweige nach Art klassifizieren (Guard Clause /
Fehlerpfad / toter Code). Kommando steht schon in der CI ([ci.yml](../.github/workflows/ci.yml), Schritt
„Zweigabdeckung ausgeben") – wiederverwenden, nicht neu bauen:
`dotnet test Pugling.sln --collect:"XPlat Code Coverage" --results-directory ./TestResults`.

---

## Etappe 2 · Regel → Test-Landkarte (die dokumentierte Hälfte)

Der Zweck der Tests ist nicht „Zeilen ausführen", sondern **die Geschäftsregeln festhalten**. Die Regeln
stehen bereits geschrieben in [CLAUDE.md](../CLAUDE.md) – daraus eine Tabelle *Regel → pinnender Test →
Injektion*. Bewusst gesagt: diese Auswahl ist **verzerrt**. Was dokumentiert wurde, wurde bewusst gebaut und
ist überproportional wahrscheinlich getestet; darum steht daneben die blinde Ziehung.

| Regel | Fundstelle | vermutet pinnender Test |
|---|---|---|
| Wallet: jeder abbuchende Pfad bumpt `child.ConcurrencyStamp` | [WalletService.cs](../backend/Pugling.Api/Services/Shared/WalletService.cs), [ShopService.cs](../backend/Pugling.Api/Services/Shared/ShopService.cs) | `ShopFlowTests`, `SkinPurchaseTests` |
| Malus ist idempotent über `PositionGoalPenalty`, Schuld erlaubt | [PositionProgressService.cs](../backend/Pugling.Api/Services/Shared/PositionProgressService.cs) | `PflichtMalusTests` |
| Ziel-Punkte idempotent über `PositionGoalReward` | dito | `PositionGoalOverviewTests` |
| Währung = reine Funktion des `PointKind` | [PointKindCurrency.cs](../backend/Pugling.Api/Services/Shared/PointKindCurrency.cs) | `PointKindCurrencyTests` |
| `GoalThreshold` ist **Prozent**, 1–100, `null` = 80 % | `PlanPositionsController` | `PlanPositionCrudTests` |
| Anti-Cheat Bild: nur auf `ShowBoth`/`SelfAssess`, „anderes Bild" mit gleicher Schranke | [MediaSelector.cs](../backend/Pugling.Api/Services/Shared/MediaSelector.cs) | `MediaSelectionTests`, `AntiCheatTests` |
| Genau **ein** aktiver+laufender Plan ist spielbar | [PositionPlayService.cs](../backend/Pugling.Api/Services/Shared/PositionPlayService.cs) | `AntiCheatTests` |
| Stufe/Heartbeat server-autoritativ, Sohn kann nicht wählen | `PositionPlayService.cs`, `ScoringService.cs` | `PositionPlayModesTests`, `SpeedBonusTests` |
| PATCH: `null` ändert nichts, `Clear…` gewinnt | alle `Update…Dto` | `PatchSemanticsTests`, `PatchClearFieldTests` |
| Unbekanntes Feld → 400 `unknown_field` | `Program.cs` | `UnknownFieldTests` |
| Eigentum über die geteilten Filter | `Auth/*OwnershipFilter.cs` | `OwnershipMatrixTests` |
| Einlösung ausstellergebunden (`SupervisorId`-Snapshot) | `ShopService.cs` | `MultiSupervisorTests`, `ShopFlowTests` |
| Unit muss zur Reihe des Buchs gehören | `TextbookSeries`-Pfad | `CreatorProfileTests` |
| Profil-Matching deterministisch: Reihe 8 > Fach 4 > Stufe 2 > Schulart 1, Gleichstand über `Id` | [CreatorProfileService.cs](../backend/Pugling.Api/Services/Creator/CreatorProfileService.cs) | `CreatorProfileTests` |
| Leere Übung wird beim Zuweisen abgewiesen | `EmptyExerciseGuard`-Pfad | `EmptyExerciseGuardTests` |

**Regel ohne zugeordneten Test = Befund, ohne dass eine Injektion nötig wäre.** Das ist der billigste Teil
der Kontrolle.

---

## Etappe 3 · Defektinjektion: die harte Messung

**30 Injektionen, feste Zahl:** 1 Kalibrierung + ~15 aus Etappe 2 + ~14 blind gezogen. Jede ist eine
**minimale, fachlich plausible Verletzung** (kein Syntaxmüll, kein `throw` – sie muss aussehen wie ein
Programmierfehler, den ein Mensch macht).

**Reihenfolge ist Zwang, nicht Geschmack:** Die **Kalibrier-Injektion läuft zuerst** – ein
`[ServiceFilter(typeof(ChildOwnershipFilter))]` entfernen, im Gates-Plan als Gegenprobe belegt („ein fremdes
Kind bekam einen Stundenplan-Eintrag"). Bleibt sie grün, ist das Verfahren kaputt und der Vorgang **bricht
sofort ab**; jede weitere Zahl wäre wertlos.

Zuschnitt der dokumentierten Hälfte (Beispiele):

- Vergleichsrichtung drehen: `>=` → `>` bei der Bestehensgrenze.
- Prozent-Schranke aufweichen: die 1–100-Prüfung entfernen.
- Idempotenz-Riegel entfernen: die `PositionGoalPenalty`-Existenzprüfung überspringen (→ Malus doppelt).
- `ConcurrencyStamp`-Bump in **einem** abbuchenden Pfad weglassen.
- `SupervisorId`-Snapshot beim Kauf nicht setzen.
- Bild-Schranke lockern: Bild auch auf getippten Stufen ausspielen.
- `MediaSelector`-Determinismus brechen: Gleichstand nicht über die `Id` auflösen.
- Reflexion eines Wächters ins Leere laufen lassen (falscher Ordnername) – prüft den Selbstschutz aus 1c.

Blinde Hälfte: jeder k-te ausgeführte Zweigpunkt aus 1d in `WalletService`, `ShopService`,
`PositionProgressService`, `ScoringService`, `MediaSelector`, sortiert nach Datei:Zeile. Lässt sich eine
gezogene Stelle nicht plausibel verletzen (Null-Prüfung ohne erreichbaren Null-Fall, Logging,
`switch`-Default), wird sie **übersprungen und mit Grund protokolliert** – die nächste Stelle rückt nach.

**Protokoll je Injektion:** Regel · Datei:Zeile · Änderung · Ausgang · **Anzahl und Namen der gefallenen
Tests** · war die Meldung verständlich? Die letzte Spalte ist nicht Kosmetik: ein Test, der rot wird, aber
nicht sagt *warum*, kostet beim nächsten echten Regress eine halbe Stunde.

**Urteil in drei Klassen:**

- **gepinnt** – ein Test fällt, **dessen Name die Regel benennt**. Nichts zu tun.
- **zufällig mitgeprüft** – rot, aber kein fallender Test benennt die Regel. Befund: die Deckung ist ein
  Nebeneffekt und verschwindet beim nächsten Test-Umbau.
- **unbewacht** – grün. Der eigentliche Ertrag dieses Vorgangs.

Dazu die **Streubreite** als eigene Achse: fallen bei *einem* Defekt mehr als **10** Tests, ist die Regel
zwar bewacht, aber beim echten Regress geht das Signal im Lärm unter – eigener Befund („Signal im Lärm").

Aufwand: Build + Vollauf je Injektion, ~45 min Rechenzeit für 30 Stück; der Rest ist Lesen. Seriell fahren
(der `EndpointCoverageGuard` urteilt erst bei ≥ 60 % Vollbestand – für die Injektionen egal, der Referenzlauf
muss aber vollständig sein).

---

## Etappe 4 · Struktur-Robustheit (auf zwei Punkte gekürzt)

1. **Wiederholbarkeit.** Zweimal hintereinander laufen lassen (Fingerprint des Stop-Hooks umgehen). Innerhalb
   einer Testklasse teilen alle Tests **eine** SQLite-Datei – Tests, die absolute Anzahlen zusichern
   (`Assert.Equal(1, …GetArrayLength())`, 47 × `Assert.Single`, 38 × `Assert.Empty`) sind genau dann stabil,
   wenn sie auf frisch angelegten Elternobjekten arbeiten. Gegenprobe fahren, statt es anzunehmen.
2. **Reihenfolge-Unabhängigkeit.** Ein Lauf mit `maxParallelThreads=1`. Kein hypothetisches Risiko: der
   Gates-Plan nennt einen **Reihenfolge-Fix in `CreatorAgentTests`** und ein Isolationsproblem derselben
   Klasse – in dieser Suite hat die Kopplung schon einmal zugeschlagen.

Gestrichen, weil belegt: „prüfen, ob das Tor blockt" (A4) und die Zeitzonen-Gegenprobe (CI fährt `TZ: UTC`,
D4 prüft Byte-Stabilität). Zusammen ~3 min statt eines halben Nachmittags.

---

## Ergebnis

Der Befund wird **in dieses Dokument** zurückgeschrieben, im Stil von
[codequalitaet-gates-plan.md](codequalitaet-gates-plan.md) – mit Zahlen, nicht mit Adjektiven:

**Commit 1 – Messung und Bericht:**

- Die Injektions-Tabelle vollständig, **30 Zeilen**, samt Streubreite und den protokollierten Übersprüngen
  (auch die grünen, besonders die grünen).
- **Zwei getrennte Quoten:** Konformität (dokumentierte Hälfte) und Sensitivität (blinde Hälfte). Sie zu
  einer Zahl zu verrühren wäre der Fehler, den der Plan zuerst gemacht hat.
- Die ~8 flachen Tests aus 1a und die Tautologien aus 1b.
- Die Regeln aus Etappe 2 ohne pinnenden Test.
- Der Satz zu den E2E-Specs: sie laufen in keiner CI und verhindern darum nichts.
- Wo ein Muster mehrfach auftrat: Vorschlag für einen **weiteren reflexiven Wächter** statt Einzeltests –
  die im Projekt etablierte Antwort („mechanische Tore statt Disziplin").

**Commit 2 – Lücken schließen, begrenzt:**

- Nachgezogen wird **jede unbewachte Regel mit Geldwirkung** (Münzen/Gems/Wallet/Shop/Malus) – dort kostet
  ein Fehler den Sohn echtes Guthaben.
- Dazu, falls 1c es ergibt, die Selbstschutz-Untergrenzen in `ErrorCodeTests`/`OpenApiExampleTests`.
- Alles andere bleibt **benannte Restliste** im Bericht, nach Schadenshöhe sortiert – nicht stillschweigend
  fallen gelassen.

Kein Produktivcode wird geändert; alle Injektionen werden zurückgenommen. Gefundene **Defekte** werden
gemeldet, nicht nebenbei gefixt (dieselbe Trennung wie beim CancellationToken-Umbau, wo der
`ExerciseControllerBase.Update`-Fund bewusst offen blieb).

## Verifikation

- Die **Kalibrier-Injektion war nachweislich rot.** Ohne diesen Punkt ist jedes „grün" im Protokoll wertlos –
  darum läuft sie zuerst und bricht den Vorgang ab, wenn sie hält.
- `git status` im Hauptbaum am Ende **identisch** zum Stand vor Etappe 0 (Worktree entfernt:
  `git worktree remove`), insbesondere kein Diff in `docs/api-examples/`.
- `dotnet test Pugling.sln -c Release` grün, und `TestResults/endpoint-coverage.txt` meldet weiter
  **0 offene Actions**.
- Jede Zeile der Injektions-Tabelle nennt Datei und Zeile, ist also nachfahrbar; jeder Übersprung nennt
  seinen Grund.

---

## Befund (gemessen 2026-07-30)

### Aufbau der Messung

Vier Worktrees auf `3cd7aae` (`git worktree add`), `PUGLING_SKIP_TEST_GATE=1`, nach jeder Injektion
`git checkout -- .` **plus** `git clean` auf `docs/api-examples` (die `DocsCaptureTests` schreiben dort bei
jedem Lauf). Jede Injektion war eine **exakt-eindeutige** Textersetzung – der Treiber bricht ab, wenn der
Anker nicht genau einmal passt, damit keine unkontrollierte Zweitänderung mitläuft. Drei Worktrees fuhren
die Injektionen parallel, der vierte Etappe 4.

**Referenzlauf vor der ersten Injektion:** `dotnet test Pugling.sln -c Release` → **587/587 grün**,
`TestResults/endpoint-coverage.txt` → **268/268 Actions, 0 offen**. Zeilenabdeckung **98,15 %**,
Zweigabdeckung **69,08 %** (bestätigt die Zahlen des Gates-Plans).

**Kalibrierung (K1) lief zuerst und war rot** – ohne diesen Beleg wäre jedes „grün" unten wertlos.
`[ServiceFilter(typeof(ChildOwnershipFilter))]` aus `ChildrenController` entfernt → 5 gefallene Tests,
davon drei, die die Regel benennen (`ConventionGuardTests.Actions_Unter_ChildId_Oder_PlanId_Tragen_Den_Ownership_Filter`,
`OwnershipMatrixTests.Fremder_Supervisor_…`, `OwnershipTests.Vater_KannFremdesKind_NichtSehen_404`).

**Ziehungsgrundlage der blinden Hälfte** (Etappe 1d): aus dem Cobertura-Bericht die **ausgeführten**
Zweigpunkte der fünf Risiko-Services, sortiert nach Datei:Zeile – `MediaSelector` 36, `PositionProgressService` 35,
`ScoringService` 8, `ShopService` 44, `WalletService` 0 (die Klasse hat gar keinen Zweigpunkt) = **123**.
Gezogen wurde jeder 8. (k = ⌊123/14⌋), also die Indizes 8, 16, … 112.

### Zwei Quoten, getrennt berichtet

| Hälfte | Injektionen | rot | grün | bemerkt |
|---|---|---|---|---|
| **Konformität** (dokumentierte Regeln aus `CLAUDE.md`) | 15 | 9 | 6 | **60 %** |
| **Sensitivität** (blind gezogene Zweigpunkte) | 14 | 8 | 6 | **57 %** |
| Kalibrierung | 1 | 1 | – | Verfahren belegt |

**Das ist das eigentliche Ergebnis:** die beiden Quoten liegen praktisch gleich. Die Vermutung des Plans –
dokumentierte Regeln seien überproportional getestet, die Auswahl also verzerrt – **trägt nicht.** Die Suite
ist gegenüber einem beliebigen Fehler ungefähr so empfindlich wie gegenüber einem, den die Doku ankündigt.
Das ist gleichzeitig die gute und die schlechte Nachricht: keine Selbstbestätigung, aber auch etwa **vier von
zehn plausiblen Programmierfehlern bleiben unbemerkt**.

### Die 30 Injektionen

`Streu` = Anzahl gefallener Tests. „gepinnt" = ein fallender Test **benennt** die Regel; „mitgeprüft" = rot,
aber kein Name trifft die Regel; „unbewacht" = grün.

| # | Hälfte | Stelle | Änderung | Streu | Urteil | erster benennender Test |
|---|---|---|---|---|---|---|
| K1 | kalib. | `ChildrenController.cs:21` | Ownership-Filter entfernt | 5 | gepinnt | `ConventionGuardTests.Actions_Unter_ChildId_…_Ownership_Filter` |
| D01 | dok. | `PositionTestsController.cs:282` | `>= passPercent` → `>` | 0 | **unbewacht** | – |
| D02 | dok. | `PlanPositionsController.cs:70` | Schranke `1–100` → `0–1000` | 2 | gepinnt | `PlanPositionCrudTests.Position_SchwelleAusserhalbProzent_WirdAbgewiesen` |
| D03 | dok. | `PositionProgressService.cs:211` | Malus-Idempotenz: `pos.Id` → `pos.StudyPlanId` | 0 | **unbewacht** (redundant, s. u.) | – |
| D04 | dok. | `ShopService.cs:214` | `child.ConcurrencyStamp`-Bump entfernt | 1 | gepinnt | `ShopFlowTests.Kauf_BumptChildConcurrencyStamp_SchuetztWalletVorParallelemDoppelkauf` |
| D05 | dok. | `ShopService.cs:182` | `SupervisorId = article.AdultId` → `childId` | 7 | gepinnt | `MultiSupervisorTests.ZweiSupervisor_GemeinsamesWallet_AberEinloesungAusstellergebunden` |
| D06 | dok. | `VocabularyExerciseType.cs:75` | Bild auch auf getippten Stufen | 2 | gepinnt | `MediaSelectionTests.GetippteStufen_ZeigenKeinBild_DennEinMotivVerraetDieBedeutung` |
| D07 | dok. | `MediaSelector.cs:275` | Tiebreak `return hash` → `Random.Shared` | 0 | **unbewacht** | – |
| D08 | dok. | `ApiErrors.cs:154` | Reflexion ins Leere (`Public` → `NonPublic`) | 2 | gepinnt | `ErrorCodeTests.HttpError_FallbackCode_IstImKatalog` |
| D09 | dok. | `PositionPlayService.cs:48` | `plan.Active &&` entfernt | 6 | gepinnt | `AntiCheatTests.Sohn_KannInaktivenPlanNichtUeben_403` |
| D10 | dok. | `PositionTestsController.cs:86` | Sohn darf Teststufe wählen | 1 | gepinnt | `AntiCheatTests.Sohn_KannTeststufeNichtWaehlen_FahrplanStufeErzwungen` |
| D11 | dok. | `PositionTestsController.cs:82` | `dto.Day ?? today` → `today` | 0 | **unbewacht** | – |
| D12 | dok. | `PointKindCurrency.cs:24` | `ShopCoins` zählt auf Gems | 6 | gepinnt | `PointKindCurrencyTests.Zuordnung_EntsprichtDerFachlichenTrennung(ShopCoins)` |
| D13 | dok. | `PositionProgressService.cs:133` | Reward-Idempotenz: `pos.Id` → `pos.StudyPlanId` | 0 | **unbewacht** (redundant, s. u.) | – |
| D14 | dok. | `TextbooksController.cs:140` | `unit.SeriesId != seriesId` → `==` | 10 | gepinnt | `CreatorProfileTests.Eine_Unit_aus_fremder_Reihe_am_Lehrbuch_wird_abgewiesen` |
| D15 | dok. | `CreatorProfileService.cs:18` | `WeightSeries` 8 → 4 | 0 | **unbewacht** | – |
| B01 | blind | `MediaSelector.cs:88` | `RemoveRange(Superseded)` entfernt | 0 | **unbewacht** | – |
| B02 | blind | `MediaSelector.cs:120` | Alternativen-Guard zu breit | 0 | **unbewacht** (redundant, s. u. – 2026-07-30 umgestuft) | – |
| B03 | blind | `MediaSelector.cs:207` | `==` → `!=` bei der eingefrorenen Wahl | 3 | gepinnt | `MediaSelectionTests.DieWahlBleibtStabil_AuchWennSpaeterEinBesseresBildDazukommt` |
| B04 | blind | `MediaSelector.cs:282` | Träger-Ternäre in `NewPick` vertauscht | 3 | gepinnt | `MediaSelectionTests.OhneAlternative_BleibtDasBildStehen_StattZuVerschwinden` |
| B05 | blind | `PositionProgressService.cs:59` | Prüfmodus-Weiche invertiert | 3 | gepinnt | `PositionGoalOverviewTests.BestandenerPositionsTest_ErfuelltTagesziel_…` |
| B06 | blind | `PositionProgressService.cs:129` | `&& p.PointsGoalMet > 0` entfernt | 0 | **unbewacht** | – |
| B07 | blind | `PositionProgressService.cs:204` | Fenster-Ternär gedreht | 0 | **unbewacht** (wirkungslos, s. u.) | – |
| B08 | blind | `PositionProgressService.cs:228` | Buchungstext Tag/Woche vertauscht | 0 | **unbewacht** | – |
| B09 | blind | `ScoringService.cs:56` | `if (!wasCorrect)` → `if (wasCorrect)` | 10 | gepinnt | `ReviewGradingTests.RichtigeAntwort_WirdServerseitigGewertet_UndBringtPunkte` |
| B10 | blind | `ShopService.cs:57` | `InsufficientCoins` → `InsufficientGems` | 3 | gepinnt | `ShopFlowTests.Kauf_OhneCoins_400_KeineAbbuchung` |
| B11 | blind | `ShopService.cs:143` | unbekanntes Angebot → `ListingInactive` | 1 | **mitgeprüft** | – (nur `ShopFlowTests.Sohn_SiehtKeineAngeboteAusFremderFamilie`) |
| B12 | blind | `ShopService.cs:177` | Kauf-Titel prüft falsche Variable | 0 | **unbewacht** | – |
| B13 | blind | `ShopService.cs:265` | Storno-Rückbuchung nur bei Überschuss | 1 | gepinnt | `ShopFlowTests.Storno_ErstattetCoinsUndGems_ReduziertInventar` |
| B14 | blind | `ShopService.cs:335` | `ShopArticleId is not null` → `is null` | 3 | gepinnt | `ShopFlowTests.Aktivierung_SohnStellt_VaterGenehmigt_InventarSinkt` |

**Urteil in Zahlen:** 17 gepinnt, 1 zufällig mitgeprüft, 12 unbewacht.

#### Protokollierte Übersprünge (drei, mit Grund)

Der Plan verlangt, unverletzbare Ziehungen zu überspringen **und den Grund zu nennen**, damit die Verzerrung
nicht durch die Hintertür zurückkehrt. Für jeden rückte die nächste Stelle der sortierten Liste nach:

| gezogen | Grund des Übersprungs | Ersatz |
|---|---|---|
| `MediaSelector.cs:270` (`foreach` über die Hash-Bytes) | Reine Hash-Schleife: jede Änderung bleibt **deterministisch**, und Determinismus ist die einzige zugesicherte Eigenschaft. Nicht plausibel verletzbar. (Die Regel selbst wurde stattdessen als D07 gezielt gebrochen – und war grün.) | `:282` (B04) |
| `PositionProgressService.cs:195` (`if (positions.Count == 0) return 0;`) | Reiner Früh-Ausstieg; die Schleife darunter läuft über eine leere Liste ohnehin nicht. Ohne beobachtbare Wirkung. | `:204` (B07) |
| `ShopService.cs:136` (`if (child is null) …`) | Null-Fall auf diesem Pfad nicht erreichbar (`childId` kommt aus einer Route hinter dem `ChildOwnershipFilter`); der Zweig ist im Bericht nur halb abgedeckt. Entfernen erzeugte zudem `CS8602` → Build-Fehler, keine Messung. | `:143` (B11) |

#### Streubreite: unauffällig

Der Plan setzt die Grenze bei **mehr als 10** gefallenen Tests je Defekt („Signal im Lärm"). **Kein einziger
Fall überschreitet sie**; das Maximum ist 10 (B09 und D14). Zwei Beobachtungen trotzdem:

- **D14 (10 Tests)** ist der lauteste Fall, und der Lärm ist hausgemacht: 6 der 10 kommen aus den
  **reflexiven** PATCH-Wächtern (`PatchSemanticsTests`/`PatchClearFieldTests` × `UpdateTextbookDto`), die jede
  Störung an einem DTO in mehrere Zeilen vervielfachen. Der eine Test, der die Regel *benennt*
  (`CreatorProfileTests.Eine_Unit_aus_fremder_Reihe_am_Lehrbuch_wird_abgewiesen`), geht dazwischen unter.
- **`DocsCaptureTests.CaptureAll`** fiel bei 6 von 18 roten Injektionen mit. Er ist der breiteste Einzeltest
  der Suite und liefert dabei die **beste** Meldung von allen (er zitiert Route, erwarteten und tatsächlichen
  Status samt Body) – Lärm mit hohem Informationsgehalt, kein Streichkandidat.

#### Meldungsqualität

Verständlich waren die Meldungen dort, wo der Testname die Regel trägt (D04, D10, B13: je **ein** Test, Name
sagt alles). Unbrauchbar allein aus der Meldung: `Assert.Equal() Failure: Values differ` ohne Kontext – das
betrifft D09, D10, B05 und die meisten `AntiCheatTests`. Der Name rettet es jeweils; der reine
Assert-Text tut es nicht.

### Was grün war – und warum (die zwölf unbewachten Stellen)

Nicht jedes „grün" ist eine Lücke. Drei Klassen:

#### (a) Echte Lücke mit Geldwirkung – im zweiten Commit geschlossen

1. **D01 · `PositionTestsController.cs:282` – die Bestehensgrenze ist an der Grenze nicht geprüft.**
   `>=` zu `>` zu machen fällt niemandem auf, weil **kein Test genau auf der Schwelle sitzt**: die Suite
   prüft 100 % vs. 80 %, 50 % vs. 80 %, 50 % vs. 40 %, 50 % vs. 3 % und 50 % vs. 90 % – jedes Mal echt
   darüber oder echt darunter. Das ist die teuerste Lücke des ganzen Vorgangs: `TestAttempt.Passed` steuert
   über `IsGoalMetAsync` **beides**, die Ziel-Punkte (`PointKind.Goal`) *und* das Ausbleiben des Malus. Ein
   Kind mit exakt 80 % verlöre die Belohnung **und** bekäme den Abzug.
2. **D11 · `PositionTestsController.cs:82` – der Nachtrag-Tag des Vaters wird nicht belegt.**
   `var day = dto.Day ?? today` zu `var day = today` zu verkürzen bleibt grün, weil
   `AntiCheatTests.Vater_DarfFremdenTagNachtragen` nur `201 Created` zusichert und den gebuchten Tag nie
   zurückliest (siehe 1a). Geldwirkung indirekt aber echt: der Nachtrag ist der Weg, eine gerissene Periode
   zu heilen; landet er auf „heute", bleibt der Malus für gestern stehen.

#### (b) Grün, weil eine zweite Schranke hält – kein Loch, sondern Tiefenverteidigung

1. **D03 / D13 · die Idempotenz-Prüfungen in `PositionProgressService`.** Beide Existenz-Checks auf die
   falsche Spalte zu setzen bleibt unbemerkt, weil die **echte** Garantie im Schema steht: `PositionGoalReward`
   und `PositionGoalPenalty` tragen je einen Unique-Index auf `(PlanPositionId, PeriodKey)`, und den pinnen
   `PflichtMalusTests` und `PositionGoalOverviewTests` mit exakten Anzahlen. Beim Malus fängt
   `catch (DbUpdateException)` den Verstoß sauber ab. **Aber:** beim Reward gibt es kein `catch` – dort wird
   der zweite Abschluss zum **500**, und niemand merkt es, weil
   `PositionGoalOverviewTests` (Zeile 60) die Antwort des zweiten Submits **verwirft**. Diese eine Zeile
   gehört nachgezogen (Commit 2), die Existenz-Checks selbst brauchen keinen eigenen Test.
2. **B07 · `PositionProgressService.cs:204`.** Das gedrehte Ternär ist **wirkungslos**: nachgelagert filtert
   `PlanDueForPeriod` alle Perioden außerhalb der Plan-Laufzeit weg. Grün ist hier die richtige Antwort.
3. **B01 · `MediaSelector.cs:88`.** Das nicht gelöschte `Superseded` hinterlässt eine Waisen-Zeile; der
   nächste Abruf zieht sie erneut zurück und `SaveFreezeAsync` verschluckt den Index-Konflikt bewusst. Kein
   beobachtbarer Effekt – genau das, was der Kommentar dort behauptet.
4. **B02 · `MediaSelector.cs:120` – nachträglich hierher umgestuft (2026-07-30).** Stand ursprünglich als
   Rang 1 der Restliste unten, und zwar falsch. Beim Versuch, den vorgeschlagenen Test zu schreiben, fiel auf:
   der Guard ist **nicht beobachtbar**. Fällt er weg, wird die aktuelle Wahl zwar auf `Rejected` gesetzt, aber
   die anschließende Neuwahl findet nichts mehr und `ReshuffleAsync` steigt bei `chosen is null` **vor**
   `SaveFreezeAsync` aus – die Ablehnung wird nie geschrieben. Beide Endpunkte
   (`ChildMediaPicksController`, `PositionPracticeController.ReshuffleImage`) antworten danach `409` ohne
   eigenes `SaveChanges`. Der Fall „genau ein Kandidat" ist von
   `MediaSelectionTests.OhneAlternative_BleibtDasBildStehen_StattZuVerschwinden` bereits vollständig
   abgedeckt – der Test war unter der Injektion grün, weil es **nichts zu sehen** gab, nicht weil er
   wegsah. Der Guard bleibt trotzdem stehen (er ist Voraussetzung, sobald jemand ein `SaveChanges` vorzieht);
   der Grund steht jetzt im Kommentar an der Stelle. Nachgemessen, nicht vermutet: Injektion erneut gesetzt,
   alle 48 Medien-Tests grün.

#### (c) Echte Lücke ohne Geldwirkung – benannte Restliste, nach Schadenshöhe

**Stand 2026-07-30: abgearbeitet.** Alle fünf verbliebenen Ränge sind geschlossen, jeder mit Gegenprobe
(Injektion erneut gesetzt, zuständiger Test rot). B02 ist nach (b) umgestuft – siehe dort.

| Rang | Stelle | Was unbemerkt durchging | jetzt gepinnt von |
|---|---|---|---|
| ~~1~~ | **B02** `MediaSelector.cs:120` | – *umgestuft nach (b): nicht beobachtbar, kein Loch* | – |
| 2 | **D07** `MediaSelector.cs:275` | Der Tiebreak darf zufällig werden. Kein Test erzeugt einen Punktgleichstand, also ist die dokumentierte Determinismus-Zusage („kein `Random`, kein `GetHashCode`") unbewacht – und Bildkonstanz *ist* laut `CLAUDE.md` der Merkeffekt. | `MediaSelectionTests.Punktgleichstand_WirdDeterministischGebrochen_NichtZufaellig` (+ `Tiebreak_LiefertFestgeschriebeneGoldwerte` für die **prozessunabhängige** Zusage, die ein Vergleich im selben Prozess nicht zeigen kann) |
| 3 | **D15** `CreatorProfileService.cs:18` | Die Gewichtung `Reihe 8 > Fach 4` darf zum Gleichstand flachgedrückt werden. Die dokumentierte Rangfolge ist nirgends festgenagelt; nur „ein Profil gewinnt" ist geprüft. | `CreatorProfileTests.Das_Matching_haelt_die_Rangfolge_der_Gewichte_ein` – pinnt die tragende Aussage: **Reihe allein schlägt Fach + Stufe + Schulart zusammen** (8 > 7) |
| 4 | **B12** `ShopService.cs:177` | Der Kauf-Beleg (`ShopPurchase.Title`) kann leer werden, wenn das Angebot keinen eigenen Titel trägt – der Vater sieht in der Kaufhistorie eine namenlose Zeile. | `ShopFlowTests.KaufBeleg_NimmtDenAngebotsTitel_UndSonstDenDesArtikels` (beide Zweige, sonst lässt sich die Weiche zur Konstante flachdrücken) |
| 5 | **B08** `PositionProgressService.cs:228` | Der Ledger-Text verwechselt „Tagesziel" und „Wochenziel". Sichtbar im Punkte-Verlauf des Kindes, keine Buchung falsch. | `PflichtMalusTests.MalusBuchungstext_BenenntTagesZielUndWochenziel_JeweilsRichtig` – Zuordnung über den Plan-Titel im Text; zwei getrennte „irgendwo steht Tagesziel"-Prüfungen wären gegen die Vertauschung blind |
| 6 | **B06** `PositionProgressService.cs:129` | Positionen mit `PointsGoalMet == 0` bekommen eine Reward-Zeile und eine Ledger-Buchung über **0** Münzen. Saldo unverändert, aber Rauschen in Verlauf und Auswertung. | `PositionGoalOverviewTests.ZielOhnePunkte_ErfuelltDiePflicht_BuchtAberNichts` |

### Etappe 1a – die flachen Zusicherungen: **8**, wie geschätzt

Mechanisch gesucht (Schreibpfad + keine Zusicherung, die einen Wert nachliest): **81** Rohtreffer. Davon
fallen 56 als **gültige** Negativtests oder Folgestatus-Belege heraus (`204` → `404`, `201` → `409`), 25
bleiben, und von denen sichern 17 ihren Effekt doch über einen zweiten Aufruf oder einen Id-Vergleich. Es
bleiben **8** Tests, die einen Erfolg zusichern, ohne ihn irgendwo nachzulesen:

| Test | zugesichert | nicht geprüft |
|---|---|---|
| `AntiCheatTests.Vater_DarfFremdenTagNachtragen` | `201` | ob der Versuch **auf dem gewünschten Tag** liegt → **von D11 bestätigt** |
| `AntiCheatTests.Vater_DarfInaktivenPlanTrotzdemDurchspielen` | `201` | ob die Sitzung benutzbar ist |
| `EmptyExerciseGuardTests.GefuellteVokabeluebung_LaesstSichWeiterZuweisen` | `201` | ob die Position entstand |
| `EmptyExerciseGuardTests.ErstAnlegenDannFuellen_BleibtMoeglich` | `201`, `201` | dito |
| `EmptyExerciseGuardTests.Aufsatz_OhneItems_BleibtZuweisbar` | `201` | dito |
| `ExerciseGrantsTests.OeffentlicheUebung_BleibtFuerFremdeZuweisbar` | `201` | dito |
| `ExerciseGrantsTests.Admin_DarfFremdeUebungAendernUndLoeschen` | `200`, `204` | ob Änderung und Löschung wirkten |
| `ExerciseGrantsTests.Admin_KannVerwaisteUebungBearbeiten_AutorNichtMehr` | `200` | ob die Änderung wirkte |

Die Zahl **96 aus dem Plan-Entwurf war zu hoch, die Korrektur auf „~8" trifft**: gemessen genau 8.

**Stand 2026-07-30: alle acht behoben** (Nr. 1 im zweiten Commit, die übrigen sieben im dritten). Jeder liest
seinen Effekt jetzt nach – die Sitzung liefert wirklich eine Karte, die Position hängt wirklich am Plan, der
`PUT` schreibt einen **geänderten** Titel und der wird zurückgelesen. Letzteres war die eigentliche Lücke bei
den beiden Admin-Tests: sie schickten die Werte, die schon drinstanden, ein wirkungsloser `PUT` hätte
ebenfalls `200` geliefert.

### Etappe 1b – Tautologien: **keine gefunden**

Der Hochrisiko-Pfad hält. `ReviewGradingTests`, `PositionTestFlowTests` und `PositionPracticeFlowTests`
nehmen die richtige Antwort **hart aus dem Test** (`"hallo"`, `"tschüss"`, `"2"`, `"4"`), nicht aus dem
Karten-Payload – die Karten werden im Gegenteil daraufhin geprüft, dass sie **keine** Lösung mitgeben
(`Cards_LiefernKeineLoesung_FuerGetippteStufe`). Genauso die Punkte-Erwartungen gegen den `ScoringService`:
`ComboTests` und `SpeedBonusTests` nennen `7`, `4`, `0` als feste Zahlen. Mechanische Gegenprobe (eine
`Assert.Equal`, deren *beide* Seiten aus einer Server-Antwort stammen) → **0 Treffer**. Dass B09 (Richtig-
Weiche invertiert) 10 Tests umlegt, ist der positive Beleg dafür.

### Etappe 1c – stumme Wächter: **einer ohne Untergrenze, Wirkung aber vorhanden**

Sechs der sieben reflexiven Wächter tragen einen Selbstschutz: `ConventionGuardTests` viermal
(`files.Length >= 30`, `blessedHits >= 100`, `types.Count >= 200`, `checkedActions >= 100/150/50`),
`TagConventionTests` (`checkedTags >= 25`), `OwnershipMatrixTests` (`checkedActions >= 60`);
`PointKindCurrencyTests` iteriert `Enum.GetValues<PointKind>()` (nie leer) **und** hat eine
`InlineData`-Tabelle. `OpenApiExampleTests` prüft mit `Assert.Contains`/`Assert.True(found)` – eine leere
Liste lässt ihn fallen, kein Vakuum.

Übrig bleibt **`ErrorCodeTests.OpenApi_CodeEnum_DecktSichMitRegistry`**: er vergleicht zwei Mengen, die
**beide** aus `ApiErrors.AllCodes` stammen. Wird die Reflexion dort blind (D08), müsste er nach Papierform
`leer == leer` vergleichen und grün bleiben. **Er wurde rot** – aber aus einem Grund, auf den man sich nicht
verlassen sollte: bei leerer Liste lässt der OpenAPI-Transformer die `enum`-Eigenschaft ganz weg, also fliegt
`GetProperty("enum")` mit `KeyNotFoundException`. Zusätzlich fiel
`ErrorCodeTests.HttpError_FallbackCode_IstImKatalog` (`Assert.Contains("http_error", AllCodes)`) – **der** ist
der eigentliche Schutz. Der Selbstschutz ist damit **zufällig, nicht gebaut**; eine Untergrenze in einer Zeile
gehört nachgezogen (Commit 2).

### Etappe 2 – Regeln ohne pinnenden Test

**Keine der 15 Regeln steht ganz ohne Test da** – jede hat mindestens die im Plan vermutete Testklasse, und
die Injektionen belegen es: 9 der 15 dokumentierten Regeln wurden von einem Test mit passendem Namen
gefangen. Was fehlt, ist feiner: bei **vier** Regeln existiert ein Test *zur Regel*, aber nicht *zu ihrer
Grenze bzw. ihrer Zusage im Detail* – D01 (Schwelle geprüft, aber nie **auf** der Schwelle), D07
(`MediaSelector` breit geprüft, aber nie mit Gleichstand), D11 (Nachtrag geprüft, aber nur der Statuscode),
D15 (Matching geprüft, aber nie die Rangfolge der Gewichte). Das ist die Fehlerklasse, die dieser Vorgang
sichtbar machen sollte: **Regel bekannt, Test vorhanden, Grenzfall offen.**

### Etappe 4 – Struktur-Robustheit: beide Punkte grün

1. **Wiederholbarkeit.** Zwei Läufe hintereinander auf identischem Stand: **587/587** und **587/587**. Die
   47 × `Assert.Single` / 38 × `Assert.Empty` arbeiten also tatsächlich auf frisch angelegten Elternobjekten,
   obwohl alle Tests einer Klasse **eine** SQLite-Datei teilen – angenommen war das, jetzt ist es geprüft.
2. **Reihenfolge-Unabhängigkeit.** Ein Lauf mit `parallelizeTestCollections: false` + `maxParallelThreads: 1`:
   **587/587**. Dass die Konfiguration wirklich griff, zeigt die Laufzeit: **12 min** seriell gegen ~5 min
   parallel. Das im Gates-Plan genannte `CreatorAgentTests`-Isolationsproblem ist damit auch von dieser Seite
   bestätigt erledigt.

**Nebenfund (nicht geplant, aber belegt): ein wanduhr-abhängiger Test ist unter Last unzuverlässig.**
`SpeedBonusTests.ZuSchnelleAntwort_UnterAntiCheatUntergrenze_BringtKeinenBonus` fiel bei zwei Injektionen
(D05, B10) mit, obwohl beide `ShopService` betreffen und mit Bonuspunkten nichts zu tun haben. Der Test
setzt voraus, dass zwei aufeinanderfolgende Antworten **unter** 1 s auseinanderliegen (Anti-Farming-Grenze);
bei drei parallel laufenden Suiten auf 20 Kernen reißt das. Das ist kein Produktfehler, sondern eine
Fragilität des Tests – auf einem langsamen oder ausgelasteten CI-Runner wird sie zum Flake. Der saubere Weg
wäre, die gemessene Zeit einzuspeisen statt sie zu erwarten; hier nur gemeldet, nicht behoben.

**Behoben 2026-07-30 (dritter Commit).** Die Naht ist ein injizierter `TimeProvider`: `Program.cs` registriert
`TimeProvider.System`, `PositionPracticeController` nimmt ihn und stempelt damit **sowohl** `ReviewEvent.At`
**als auch** die gemessene Antwortzeit – beides muss aus derselben Uhr kommen, sonst vergleicht man Äpfel mit
Birnen. Im Testhost ersetzt `PuglingWebAppFactory.Clock` (`TestClock`) ihn; die Uhr ist standardmäßig
**durchreichend**, nur `SpeedBonusTests` friert sie ein und rückt sie selbst vor. Bewusst so klein: die
Tageslogik bleibt bei `DateTime.UtcNow`, denn sie ist mit Kalendertagen prüfbar – die Antwortzeit nicht.
Nebeneffekt: die beiden `Task.Delay(1200)` sind weg, und die Untergrenze ist jetzt **an** ihrer Grenze
gepinnt (900 ms → kein Bonus, 1000 ms → Bonus) statt nur einseitig. Gegenprobe: `MinSpeedSeconds` auf 0,5
gesetzt → der 900-ms-Fall fällt.

### Die E2E-Specs – der Satz, präziser als im Plan

Der Plan schreibt, die 10 Playwright-Specs unter `frontend/e2e/` „laufen in keiner CI und verhindern darum
nichts". Das ist zu scharf und in einem Punkt falsch: **`.github/workflows/e2e.yml` existiert** und ist auf
`pull_request`, nightly (03:00 UTC) und Handbetrieb verdrahtet.

**Diese Frage hat sich noch während der Messung geändert – darum mit Zeitstempel:** um 04:2x UTC meldete
`gh api .../workflows/e2e.yml/runs` noch `total_count: 0` (deckt sich mit
[codequalitaet-gates-plan.md](codequalitaet-gates-plan.md), Stand 2026-07-29 – bis dahin ging alles per
direktem Push auf `main`, und darauf triggert der Workflow bewusst nicht). Um **05:40 UTC lief die erste
Nightly – und war grün** (`event: schedule`, `conclusion: success`, auf `5a09417`). Der lokale
„25/25 grün"-Beleg aus der vorherigen Übergabe hat damit endlich seine CI-Bestätigung.

Was **bleibt**: das E2E-Tor ist kein Freigabe-Tor. `deploy-azure.yml` hing per `workflow_run` an `CI`, nicht
an diesem Workflow (und ist seit 2026-07-30 ganz stillgelegt, siehe
[codequalitaet-gates-plan.md](codequalitaet-gates-plan.md), A2) – rote E2E blocken also weiterhin kein
Deploy, sie melden sich am PR und nachts. Genau so
ist es im Kopf von `e2e.yml` als Entscheidung dokumentiert. Der Satz des Plans stimmt also im Ergebnis
(„verhindert nichts"), aber aus einem anderen Grund als angenommen: nicht weil der Workflow fehlt oder nie
läuft, sondern weil er absichtlich nicht am Release hängt.

### Vorschlag: ein weiterer reflexiver Wächter

Ein Muster trat mehrfach auf und lässt sich mechanisch fassen statt mit Einzeltests
(„mechanische Tore statt Disziplin"): **ein Test, der auf einem Schreibpfad einen Erfolgsstatus zusichert und
den Effekt nirgends nachliest.** Genau das ist die Fehlerklasse hinter D11, und die 1a-Liste zeigt acht
Vertreter. Ein Wächter über den Testquellen (Methode enthält `PostAsJsonAsync`/`Patch…`/`Delete…`, prüft
`HttpStatusCode.OK|Created|NoContent`, enthält aber kein `GetProperty`/`ReadFromJsonAsync<`/`JsonAssert.`/
DB-Scope) fände sie – mit einer Baseline-Liste wie beim `CancellationToken`-Wächter, damit die acht Altlasten
nicht sofort blocken. Das Skript der Messung liegt bereit; **bewusst nicht** in diesem Vorgang scharf
gestellt, weil der Plan „kein Umbau der Suite" sagt und die Heuristik (25 Rohtreffer → 8 echte) noch zu
grob für ein hartes Tor ist.

#### Entscheidung 2026-07-30: **kein Wächter.** Nachgemessen statt geschätzt

Die offene Frage lautete nicht „bauen oder nicht", sondern: **lässt sich die Heuristik auf die echten Fälle
verengen, ohne eine Ausnahmeliste zu pflegen?** Antwort: nein. Gemessen auf dem Stand vor der Reparatur:

| Verengung | Treffer | davon echt | davon verloren |
|---|---|---|---|
| Roh (Schreibpfad + Erfolgsstatus + kein Nachlesen) | 24 | 7 | – |
| + Folgestatus als Beleg ausgenommen (`204`→`404`, `201`→`409`) | **8** | 6 | 1 |
| + Id-/`GetFromJsonAsync`-Vergleich ausgenommen | 5 | 3 | 4 |
| + Fehlercode-/Header-/Textprüfung ausgenommen | 3 | 3 | 4 |

Keine Stufe erreicht gleichzeitig brauchbare Genauigkeit **und** Vollständigkeit: bei 8 Treffern sind 2 falsch
und einer fehlt; wer die beiden Falschen wegfiltert, verliert vier echte. Ein Tor mit dieser Kennlinie
produziert Ausnahmen statt Qualität – genau das, was [codequalitaet-gates-plan.md](codequalitaet-gates-plan.md)
vermeiden will.

Der zweite, härtere Grund kam erst nach der Reparatur heraus: zwei der reparierten Tests **bleiben** in der
Trefferliste, weil ihr Nachlesen in einen gemeinsamen Helfer gewandert ist (`AssertPositionOnPlanAsync`). Die
Heuristik sieht nur den Methodenrumpf und kann „liest über einen Helfer nach" nicht von „liest nie nach"
unterscheiden – **dieselbe** Blindheit, die im Projekt schon für CA2016 und tokenlose Helfer dokumentiert ist
(`CLAUDE.md`, „Mechanische Tore statt Disziplin"). Ein Tor, das die saubere Refaktorierung bestraft und die
Lücke durchlässt, ist das falsche Werkzeug.

Also: die sieben Altlasten **einzeln repariert** (eine bis drei Zeilen je Test), Skript verworfen. Die
Fehlerklasse bleibt als Prüffrage im Review, nicht als Tor.

### Was der zweite Commit nachgezogen hat

Bewusst begrenzt auf **Geldwirkung** plus den 1c-Selbstschutz; alles andere steht oben als benannte Restliste.
Vier Änderungen, alle in `Pugling.Api.Tests` – **kein Produktivcode**:

| # | Test | pinnt |
|---|---|---|
| 1 | `PositionTestFlowTests.Test_ErgebnisGenauAufDerSchwelle_IstBestanden` (neu) | **D01** – Ergebnis genau auf der Schwelle gilt als bestanden, und die Ziel-Punkte sind gebucht |
| 2 | `AntiCheatTests.Vater_DarfFremdenTagNachtragen` (erweitert) | **D11** – der gebuchte `day` ist der nachgetragene, nicht „heute" |
| 3 | `PositionGoalOverviewTests.BestandenerPositionsTest_…` (erweitert) | **D13** – der zweite Abschluss wird auf seinen **Status** geprüft, nicht nur auf die Reward-Anzahl (macht den 500 sichtbar) |
| 4 | `ErrorCodeTests.OpenApi_CodeEnum_DecktSichMitRegistry` (erweitert) | **1c** – Untergrenze `AllCodes.Count >= 40`, damit der Drift-Wächter nicht vom Zufall lebt |

**Gegenprobe gefahren, nicht angenommen:** jede der vier Injektionen wurde in einem eigenen Worktree erneut
eingesetzt und der zuständige Test einzeln gefahren – **alle vier wurden rot**. Ohne diesen Schritt wären es
Tests, von denen niemand weiß, ob sie etwas festhalten. Danach die volle Suite grün.

### Was der dritte Commit nachgezogen hat (2026-07-30)

Die Nacharbeit nach [testaudit-nacharbeit-plan.md](testaudit-nacharbeit-plan.md) – die dort benannte Restliste
ist damit leer. **Ein** Produktivcode-Fund, der Rest Tests. Stand danach: **597/597 grün, 268/268 Actions,
Build warnungsfrei, `dotnet format` sauber.**

| # | Änderung | pinnt / behebt |
|---|---|---|
| 1 | `PositionProgressService.EvaluateAndAwardAsync`: `catch (DbUpdateException)` + `ChangeTracker.Clear()` (**Produktivcode**) | der nebenläufige Verlierer eines Zielabschlusses bekam einen **500** auf einen gelungenen Abschluss – beim Malus war derselbe Fall längst als „gutartig" ausformuliert |
| 2 | `PositionGoalOverviewTests.NebenlaeufigeZielbuchung_VerliertDasRennen_…` (neu) | Nr. 1, **deterministisch** – siehe unten |
| 3 | `MediaSelectionTests` × 2 (neu) | **D07** Determinismus des Tiebreaks, in-process *und* prozessunabhängig |
| 4 | `CreatorProfileTests.Das_Matching_haelt_die_Rangfolge_der_Gewichte_ein` (neu) | **D15** |
| 5 | `ShopFlowTests.KaufBeleg_…` (neu) | **B12** |
| 6 | `PflichtMalusTests.MalusBuchungstext_…` (neu) | **B08** |
| 7 | `PositionGoalOverviewTests.ZielOhnePunkte_…` (neu) | **B06** |
| 8 | `TimeProvider`-Naht + `TestClock`, `SpeedBonusTests` neu geschrieben | der wanduhr-abhängige Test (Etappe 4, Nebenfund) |
| 9 | sieben Tests in `AntiCheatTests`/`EmptyExerciseGuardTests`/`ExerciseGrantsTests` erweitert | die flachen Zusicherungen aus Etappe 1a |
| 10 | Kommentar an `MediaSelector.cs:120` | hält fest, **warum** B02 nicht testbar ist – damit niemand denselben Weg noch einmal geht |

**Zwei Abweichungen von der Vorlage, beide bewusst:**

1. **B02 wurde nicht getestet, sondern umgestuft.** Der vorgeschlagene Test existiert längst und kann die
   Injektion prinzipiell nicht fangen (Begründung unter (b)). Ein Test zu schreiben, der nicht fallen *kann*,
   wäre schlechter als keiner: er behauptet eine Absicherung.
2. **Der Nebenläufigkeits-Test schickt keine zwei parallelen Submits.** Das Fenster zwischen Existenzprüfung
   und `SaveChanges` ist Bruchteile einer Millisekunde breit; zwei parallele HTTP-Requests treffen es fast nie,
   der Test wäre grün geblieben, **ohne den Pfad je zu betreten** – dieselbe Sorte Scheinsicherheit wie eine
   flache Zusicherung. Stattdessen wird der Zustand des Verlierers hergestellt: die Belohnung ist vom echten
   Gewinner (einem bestandenen Test über HTTP) festgeschrieben, ein zweiter Kontext hält die Buchung noch
   ungespeichert vor. Der Konflikt tritt damit **immer** ein. Gegenprobe: ohne den `catch` wird der Test rot
   (`SqliteException: UNIQUE constraint failed`).

**Offene Frage der Vorlage beantwortet:** „Ist der 500 je real aufgetreten?" – **nein.** In den 14
Logtagen unter `backend/Pugling.Api/logs/` steht kein einziger 500 auf einem Test-/Overview-Pfad. Die 108
gefundenen 500 sind `supervisor/children/daily-overview` und alle vom selben Typ: `TaskCanceledException`
durch Client-Abbruch, sämtlich **vor** `179cc06` („Client-Abbruch ist kein Serverfehler") – heute wären es
499 ohne Fehler-Log. Also blieb die Reihenfolge der Vorlage richtig: erst der Fix, dann die Tests.

### Verifikation dieses Vorgangs

- Kalibrierung war **nachweislich rot** (K1, 5 Tests) – erst damit sind die 12 „grün" oben etwas wert.
- Kein Produktivcode wurde geändert; alle 30 Injektionen sind zurückgenommen. `git status` im Hauptbaum war
  vor Etappe 0 und nach Etappe 4 leer, **insbesondere ohne Diff in `docs/api-examples/`** (die Worktrees
  wurden nach jeder Injektion auch dort bereinigt). Die vier Worktrees sind entfernt.
- Jede Tabellenzeile nennt Datei **und** Zeile, ist also nachfahrbar; jeder der drei Übersprünge nennt seinen
  Grund.
- Gefundene Defekte wurden **gemeldet, nicht nebenbei gefixt** – die vier Punkte oben sind Tests, kein
  Produktivcode. Der Flake in `SpeedBonusTests` bleibt bewusst offen.

## Nachmessung 2026-07-31: die drei unbeobachteten Flächen

Der Vorgang oben bewertet die **Backend**-Suite, und zwar bewusst (Rahmenbedingung 6: „E2E zählt nicht mit").
Am 2026-07-31 ist auf `3b63d1d` eine Bewertung der *gesamten* Test-Abdeckung nachgezogen worden – ein voller
Lauf mit `--collect:"XPlat Code Coverage"`, **keine** Injektionen, kein Produktivcode geändert. Sie bestätigt
den Befund oben und findet drei Flächen, über die bis heute **keine** Zahl vorlag.

| Größe | 2026-07-30 (Audit) | 2026-07-31 |
| --- | --- | --- |
| Backend-Tests | 597/597 | **615/615** grün, 52 s (Release) |
| Endpunkt-Abdeckung | 268/268, 0 offen | **263/263, 0 offen** – fünf Actions weniger, Ursache nicht nachgesehen |
| Zweigabdeckung `Pugling.Api` | 69,1 % | **72,2 %** |
| Zeilenabdeckung `Pugling.Api` | 98,2 % | 97,0 % |
| `git status` nach dem Lauf | leer | leer – D4 (Doku-Byte-Stabilität) hält |

**Unit gegen Integration: rund 5 % zu 95 %.** Von 80 Testklassen fahren **71 über HTTP** gegen die in-process
gestartete App; die 9 übrigen tragen 46 Testfälle, davon 15 reflexive Wächter – bleiben ~31 klassische
Unit-Tests plus `QueryPlanSmokeTests`. Für ein API-First-Produkt mit dünnen Controllern ist das die passende
Form: eine dort gepinnte Regel prüft Auth, Ownership, EF-Mapping und Vertrag mit. Der Preis steht in den
Zahlen oben – **jeder Grenzfall kostet einen vollen Flow**, und genau daran hängt die gemessene Fehlerklasse
„Regel bekannt, Grenzfall offen". Kombinatorisch sind zwei Ecken: `ScoringService`/`StageMechanics`
(Combo × Speed × Zeitfenster × Leitner-Stufe) und der `MediaSelector` (4 der 12 unbewachten Injektionen).

Die drei Flächen, die dieser Befund nicht abdeckte:

1. **`Pugling.Client`: Zeilen 61,5 %, Zweige 56,9 %** – **122** öffentliche Methoden gegen 18 Tests
   ([PuglingClientTests](../backend/Pugling.Api.Tests/PuglingClientTests.cs)). Kein Wächter hält die
   Routen-Strings gegen das OpenAPI-Dokument; ein Tippfehler in einer der nicht gefahrenen Methoden fällt erst
   dem Agenten zur Laufzeit auf. (`pugling-creator`: Zeilen 67,0 %, Zweige 51,5 %.)
2. **Der Produktionspfad ist zu 0 % ausgeführt.**
   [PuglingWebAppFactory.cs:26](../backend/Pugling.Api.Tests/PuglingWebAppFactory.cs) setzt
   `UseEnvironment("Development")` und ist die **einzige** `UseEnvironment`-Stelle im Repo. Nie ausgeführt: der
   Fail-Fast auf fehlenden `Jwt:Key` ([Program.cs:260](../backend/Pugling.Api/Program.cs)),
   `RemarkOptions.GlobalRead = false` (:198), `Seed:Enabled` aus (:464), der Login-Rate-Limiter und
   `Migrate()` gegen eine echte Datei.
3. **Frontend: 21 Vitest-Fälle über 83 Quelldateien**, ausschließlich für `lib/remarks.ts` und
   `vater/navigation.ts` – **keine** Komponententests (die DOM-Umgebung ist eingerichtet,
   `vitest.config.ts:13` setzt `environment: "happy-dom"`; es rendert nur niemand), und
   [types.ts](../frontend/src/lib/types.ts) trägt **1950 handgeschriebene Zeilen** Vertrag ohne Generator. Ein
   Feldumbau im Backend fällt `tsc` nur auf, wenn jemand die TS-Zeile mitzieht; sonst fängt es erst
   Playwright – und der läuft an PR und nachts, ist also kein Freigabe-Tor.

**Zur Frage „brauchen wir System-Tests?": als Ebene nein, die gibt es zweimal** – 25 Playwright-Tests durch
zwei echte Server und einen echten Browser, dazu `/smoke-test` out-of-process von Hand. Was fehlt, sind
punktuelle Prüfungen an den Nähten oben; sie liegen als Stories im Backlog:
[B-40](backlog/B-40-client-routen-waechter.md) · [B-41](backlog/B-41-produktions-startup-smoke.md) ·
[B-42](backlog/B-42-openapi-typen-generieren.md) · [B-43](backlog/B-43-frontend-komponententests.md).

## Verwandt

- [testaudit-nacharbeit-plan.md](testaudit-nacharbeit-plan.md) – **die Reste dieses Befunds als Arbeitsplan**:
  die sechs unbewachten Regeln ohne Geldwirkung, der `SpeedBonusTests`-Flake, der Produktivcode-Fund im
  Reward-Pfad und die Entscheidung über den vorgeschlagenen Wächter
- [codequalitaet-gates-plan.md](codequalitaet-gates-plan.md) – die Tore, deren Wirksamkeit hier geprüft wird
- [code-review.md](code-review.md) – der Review, der die Integrationstests angestoßen hat
- [endpunkt-beziehungen.md](endpunkt-beziehungen.md) – die Wissenskarte für die Regel→Test-Landkarte
