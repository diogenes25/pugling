---
tags: [bereich/qualitaet, bereich/tests]
---

# Kontrolle: Erfüllen die Integrationstests ihren Zweck?

## Warum dieser Vorgang

Das Projekt hat eine große Integrationstest-Suite: **88 Dateien, 518 Testmethoden, ~16.500 Zeilen** in
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

## Verwandt

- [codequalitaet-gates-plan.md](codequalitaet-gates-plan.md) – die Tore, deren Wirksamkeit hier geprüft wird
- [code-review.md](code-review.md) – der Review, der die Integrationstests angestoßen hat
- [endpunkt-beziehungen.md](endpunkt-beziehungen.md) – die Wissenskarte für die Regel→Test-Landkarte
