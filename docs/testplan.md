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

**Was heute niemand messt: ob ein Test einen Defekt auch *bemerkt*.** Erreicht ≠ geprüft. Zwei Indizien,
dass die Frage berechtigt ist:

- **96 von 506 Testmethoden treffen ausschließlich Statuscode-Zusicherungen** (maschinell erhoben, s. u.) –
  bei Negativtests (401/403) legitim, bei einem `200`/`204` auf einem Schreibpfad heißt es: der Effekt wurde
  nie nachgesehen.
- Die 88 Tests aus Etappe C entstanden **um eine Abdeckungslücke zu schließen**, nicht um eine Regel zu
  pinnen – genau das Muster, aus dem flache Tests entstehen.

Ziel: ein belegter Befund, **welche Geschäftsregeln bei absichtlicher Verletzung rot werden und welche
nicht**, plus eine benannte Liste nachzuziehender Tests. Kein Umbau der Suite, keine Abdeckungsquote als
Ziel – dieselbe Haltung wie im Gates-Plan.

Umfang: **die ganze Suite**. Verfahren: **Defektinjektion (Gegenproben)** als harte Messung.

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
grün sein, sonst ist jedes „rot" mehrdeutig.

---

## Etappe 1 · Statische Sichtung: taugen die Zusicherungen?

Drei mechanische Erhebungen, jede liefert eine Liste zum Triagieren – nicht jeder Treffer ist ein Mangel.

**1a Zusicherungs-Tiefe.** Die Erhebung ist bereits gefahren: **96/506 Tests prüfen nur Statuscodes.**
Diese Liste je Test einordnen in *legitim* (Negativtest 401/403/404/409 – der Status **ist** die Aussage)
vs. *flach* (2xx auf einem Schreibpfad ohne Nachlesen des Effekts). Kandidaten aus dem ersten Blick:
`AntiCheatTests.Vater_DarfFremdenTagNachtragen`, `AntiCheatTests.Vater_DarfInaktivenPlanTrotzdemDurchspielen`,
`EmptyExerciseGuardTests.*_BleibtZuweisbar`. Ergebnis: Liste „Effekt-Zusicherung fehlt".

**1b Tautologien.** Suchen, wo ein Test den erwarteten Wert **vom Server selbst** bezieht und
zurückspiegelt – dann kann die geprüfte Logik beliebig falsch sein und der Test bleibt grün. Hochriskante
Stelle ist der server-autoritative Antwortpfad: Woher nehmen `ReviewGradingTests`, `PositionTestFlowTests`,
`PositionPracticeFlowTests` die *richtige* Antwort? Aus dem Karten-Payload (tautologisch) oder aus im Test
hart hinterlegten Vokabeln (belastbar)? Gleiche Frage für Punkte-Erwartungen gegen `ScoringService`.

**1c Stumme Wächter.** Ein reflexiver Wächter, dessen Reflexion nicht greift, findet 0 Verstöße und ist
grün – **schlimmer als kein Wächter**, weil er Deckung behauptet. `ConventionGuardTests`,
`OwnershipMatrixTests`, `PatchSemanticsTests`, `TagConventionTests` und der `EndpointCoverageGuard` tragen
solche Selbstschutz-Untergrenzen bereits (`Assert.True(files.Length >= 30)` usw.) – **geprüft, vorhanden.**
Offen bleiben die ohne Untergrenze: `PatchClearFieldTests` (184 Zeilen, kein Mindest-Count),
`UnknownFieldTests`, `ErrorCodeTests`, `OpenApiExampleTests`, `ApiVersioningTests`, `PointKindCurrencyTests`.
Je Datei entscheiden: braucht sie eine Untergrenze (weil sie über eine reflexiv/über Listen ermittelte
Fläche urteilt) oder nicht (weil sie feste Einzelfälle prüft)?

**1d Nie genommene Zweige.** Aus dem Cobertura-Bericht die 30,8 % ungenommenen Zweige ziehen und nach Art
klassifizieren (Guard Clause / Fehlerpfad / toter Code). Kommando steht schon in der CI
([ci.yml](../.github/workflows/ci.yml), Schritt „Zweigabdeckung ausgeben") – wiederverwenden, nicht neu
bauen: `dotnet test Pugling.sln --collect:"XPlat Code Coverage" --results-directory ./TestResults`.
Ergebnis: die ungenommenen Zweige in den Services mit Geldwirkung (`WalletService`, `ShopService`,
`PositionProgressService`, `ScoringService`) sind die Prioritätsliste.

---

## Etappe 2 · Regel → Test-Landkarte

Der Zweck der Tests ist nicht „Zeilen ausführen", sondern **die Geschäftsregeln festhalten**. Die Regeln
stehen bereits geschrieben in [CLAUDE.md](../CLAUDE.md) – daraus eine Tabelle *Regel → pinnender Test →
Injektion*. Die Regeln, um die es geht (Auswahl nach Schadenshöhe, nicht vollständig):

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
der Kontrolle und liefert vermutlich schon Treffer.

---

## Etappe 3 · Defektinjektion: die harte Messung

Für jede Zeile aus Etappe 2 **eine minimale, fachlich sinnvolle Verletzung** einbauen (kein Syntaxmüll,
kein `throw` – die Injektion muss aussehen wie ein plausibler Programmierfehler), Suite fahren, Ergebnis
protokollieren. Beispiele für den Zuschnitt:

- Vergleichsrichtung drehen: `>=` → `>` bei der Bestehensgrenze.
- Prozent-Schranke aufweichen: die 1–100-Prüfung entfernen.
- Idempotenz-Riegel entfernen: die `PositionGoalPenalty`-Existenzprüfung überspringen (→ Malus doppelt).
- `ConcurrencyStamp`-Bump in **einem** abbuchenden Pfad weglassen.
- `SupervisorId`-Snapshot beim Kauf nicht setzen.
- Bild-Schranke lockern: Bild auch auf getippten Stufen ausspielen.
- `MediaSelector`-Determinismus brechen: Gleichstand nicht über die `Id` auflösen.
- Einen `[ServiceFilter(typeof(ChildOwnershipFilter))]` entfernen (**Referenz-Injektion** – diese muss rot
  werden, sie ist im Gates-Plan als Gegenprobe belegt und dient hier als Kalibrierung des Verfahrens).
- Reflexion eines Wächters ins Leere laufen lassen (falscher Ordnername) – prüft, ob der Selbstschutz aus 1c
  greift.

Protokoll je Injektion: **Regel · Datei:Zeile · Änderung · rot? · welcher Test hat gemeldet · Meldung
verständlich?** Die letzte Spalte ist nicht Kosmetik: ein Test, der rot wird, aber nicht sagt *warum*,
kostet beim nächsten echten Regress eine halbe Stunde.

Drei Ausgänge, drei Bedeutungen:

- **rot, richtiger Test, klare Meldung** → Regel ist gepinnt. Nichts zu tun.
- **rot, aber irgendein weit entfernter Test** → Regel ist nur *zufällig* mitgeprüft. Befund: fehlender
  gezielter Test (der entfernte Test kann jederzeit umgebaut werden und nimmt die Deckung mit).
- **grün** → **die Regel ist unbewacht.** Der eigentliche Ertrag dieses Vorgangs.

Aufwand: ~60 s je Lauf, 20–30 Injektionen ⇒ **eine halbe Stunde Rechenzeit**, der Rest ist Lesen. Läufe
seriell fahren (der `EndpointCoverageGuard` urteilt erst bei ≥ 60 % Vollbestand, ein `--filter`-Teillauf
verschweigt sein Urteil – für die Injektionen ist das egal, aber der Referenzlauf muss vollständig sein).

---

## Etappe 4 · Struktur-Robustheit

Vier Eigenschaften, ohne die eine grüne Suite nichts aussagt:

1. **Wiederholbarkeit.** Zweimal hintereinander laufen lassen (Fingerprint des Stop-Hooks umgehen). Innerhalb
   einer Testklasse teilen alle Tests **eine** SQLite-Datei – Tests, die absolute Anzahlen zusichern
   (`Assert.Equal(1, …GetArrayLength())`, 47 × `Assert.Single`, 38 × `Assert.Empty`) sind genau dann stabil,
   wenn sie auf frisch angelegten Elternobjekten arbeiten. Gegenprobe fahren, statt es anzunehmen.
2. **Reihenfolge-Unabhängigkeit.** xUnit parallelisiert über Collections; Reihenfolge innerhalb einer Klasse
   ist nicht garantiert. Wiederholte Läufe mit veränderter Parallelität zeigen versteckte Kopplung.
3. **Das Tor blockt wirklich.** Stichprobe: einen Test absichtlich brechen und prüfen, dass Stop-Hook **und**
   CI rot melden – inkl. der Eigenheit des Assembly-Fixtures („Passed!" trotz Exit-Code 1), die der
   Gates-Plan beschreibt. Das ist der Unterschied zwischen „Test existiert" und „Test verhindert etwas".
4. **Zeit/Uhrzeit-Unabhängigkeit.** Die Factory löscht die geseedeten `TimeSlots` (Wanduhr-Neutralisierung).
   Gegenprobe: Lauf mit anderer `TZ` bzw. nahe Mitternacht – die `DateTime.UtcNow`/`DateOnly`-Logik ist der
   bekannte Fallstrick aus CLAUDE.md.

---

## Ergebnis

Der Befund wird **in dieses Dokument** zurückgeschrieben, im Stil von
[codequalitaet-gates-plan.md](codequalitaet-gates-plan.md) – also mit Zahlen, nicht mit Adjektiven:

- Die Injektions-Tabelle vollständig (auch die grünen, besonders die grünen).
- Die triagierte Liste der flachen Tests aus 1a und der Tautologien aus 1b.
- Die Regeln aus Etappe 2 ohne pinnenden Test.
- Eine **priorisierte Liste nachzuziehender Tests** – nach Schadenshöhe, nicht nach Bequemlichkeit
  (Geldwirkung > Anti-Cheat > CRUD-Schwanz).
- Wo ein Muster mehrfach auftrat: Vorschlag für einen **weiteren reflexiven Wächter** statt Einzeltests –
  die im Projekt etablierte Antwort („mechanische Tore statt Disziplin").

Kein Produktivcode wird in diesem Vorgang geändert; alle Injektionen werden zurückgenommen. Gefundene
Defekte werden **gemeldet, nicht nebenbei gefixt** (dieselbe Trennung wie beim CancellationToken-Umbau, wo
der `ExerciseControllerBase.Update`-Fund bewusst offen blieb).

## Verifikation

- `git status` im Hauptbaum am Ende **identisch** zum Stand vor Etappe 0 (Injektions-Worktree entfernt:
  `git worktree remove`), insbesondere kein Diff in `docs/api-examples/`.
- `dotnet test Pugling.sln -c Release` grün: 518+ Tests, und `TestResults/endpoint-coverage.txt` meldet
  weiter **0 offene Actions**.
- Die Referenz-Injektion (`ChildOwnershipFilter` entfernen) wurde nachweislich **rot** – belegt, dass das
  Verfahren überhaupt greift. Ohne diesen Kalibrierpunkt ist jedes „grün" im Protokoll wertlos.
- Jede Zeile der Injektions-Tabelle nennt Datei und Zeile, ist also nachfahrbar.

## Verwandt

- [codequalitaet-gates-plan.md](codequalitaet-gates-plan.md) – die Tore, deren Wirksamkeit hier geprüft wird
- [code-review.md](code-review.md) – der Review, der die Integrationstests angestoßen hat
- [endpunkt-beziehungen.md](endpunkt-beziehungen.md) – die Wissenskarte für die Regel→Test-Landkarte
