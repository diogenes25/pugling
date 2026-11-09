---
tags: [typ/story, status/abgenommen, bereich/daten, rolle/supervisor]
aliases: [Seed-Familie, Demo-Daten inkonsistent]
status: abgenommen
prio: P3
art: Aufräumen
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-08-rollengang.md#feedback-vater
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-429 · Seed: die Demo-Familie widerspricht dem eigenen Katalog

Der Seed ist das Erste, was ein neuer Entwickler, ein Tester oder ein Rollengang sieht – und er zeigt die App
schlechter, als sie ist. Im Rollengang vom 2026-11-08 (frische Wegwerf-DB) stand im Kind-Hub von „Sohn":
Buch „Green Line 1 · Klett – **NICHT KATALOGISIERT**", obwohl die Reihe `green-line-1` mit drei Units im
Katalog liegt; „**Kein Profil passt** zu Sohn – prüfe Klasse und Schulart", weil das Kind weder Klasse noch
Schulart trägt; der Kontoverlauf nennt das Startguthaben „**Wiederholung**"; und die französischen Vokabeln
tragen als Tipp den Artikel des **Fragewortes** („💡 la" zu „la ville", Lösung „die Stadt").

## User Story

Als *Vater*, der die App zum ersten Mal öffnet, möchte ich eine Demo-Familie sehen, die den Katalog nutzt, damit
Kind-Hub, Fachlehrer-Treffer und Kontoverlauf das zeigen, was sie können – statt Warnungen über Daten, die der
Seed selbst falsch gesetzt hat.

## Ist-Stand am Code

- `Seed.cs:247-256`: `Textbook { Title = "Green Line 1", Publisher = "Klett", Grade = 5, CurrentChapter =
  "Unit 1 – Greetings" }` – ohne `SeriesId`/`CurrentUnitId`, obwohl `TextbookSeries` „Green Line 1" mit Unit 1
  im selben Seed entsteht.
- `Seed.cs:1047-1052`: Kind „Sohn" ohne `Grade`/`SchoolType` (API: `grade: null, schoolType: "None"`), darum
  schlägt `creator/profiles/match` mit „kein Profil passt" an (Kind-Hub, Rollengang). Der Lehrer-Seed legt
  zudem kein `CreatorProfile` an (`/vater/fachlehrer` als Lehrer: „Noch kein Fachlehrer").
- `Seed.cs:1061`: Startguthaben als `PointKind.Base` („Wiederholung" in `labels.ts:8`); `:1062` Gems als
  `Achievement` („Auszeichnung") – beides irreführend, `Manual`/`ManualGems` („Papa-Buchung") trifft es.
- Französische Items mit `Hint = "la"`/`"le"` (Rollengang: Karte „la ville", Tipp „la"; Config
  `direction: front-to-back, fr→de`): der Tipp wiederholt das Fragewort statt auf die Lösung zu zeigen.

## Die echte Lücke

Vier Stellen im Seed, kein Produktcode. Alles Aufräumen: kein Verhalten ändert sich, nur die Demo-Daten.

## Offene Punkte

Keine Produktfrage; Aufräumen, autonom grillbar. Empfehlung: Buch an Reihe/Unit 1 hängen; Kind Klasse 5,
Gymnasium (passt zur Green-Line-Unit „Klasse 5"); ein Fachlehrer-Profil „Englisch Klasse 5–6, Green Line 1"
für Herrn Schmidt, damit der Match im Kind-Hub trifft; Startguthaben als `Manual`/`ManualGems`; französische
Tipps auf den Anfangsbuchstaben der Lösung („d…") oder leer. **`DocsCaptureTests`** (`docs/api-examples`)
werden sich ändern – neu erzeugen und den Diff lesen (Memory: Capture byte-stabil halten).

## Akzeptanzkriterien (Entwurf)

1. Frische DB → Kind-Hub „Sohn" zeigt das Buch mit Reihe „Green Line 1" und Unit 1 (kein „nicht katalogisiert"),
   und „Passender Fachlehrer" nennt Herrn Schmidt mit Grund `series_match`.
2. Kontoverlauf nennt „Papa-Buchung · Startguthaben" und „Papa-Geschenk (Gems) · Willkommens-Gems".
3. Kein französisches Item trägt als Tipp seinen eigenen Artikel.
4. Seed bleibt idempotent (zweiter Start ändert nichts – bestehender Test der Seed-Idempotenz bleibt grün).

## Entscheidungen

1. **In diesem Sprint: das Buch an Reihe „Green Line 1" und deren erste Unit hängen; das Startguthaben als `Manual`/`ManualGems`
   buchen** (Seed-Kind und Demo-Kind). *Entscheidung im Lauf (Aufräumen), getroffen von Claude.*
2. **Abgeteilt nach [B-445](B-445-seed-kind-ohne-klasse-und-fachlehrer.md): Klasse/Schulart am Seed-Kind und ein Fachlehrer-Profil.**
   Die Klasse filtert den Katalog serverseitig; `e2e/assistent.spec.ts` verlangt dafür ausdrücklich ein Kind ohne Klasse, und viele
   Integrationstests laufen auf Kind 1 – kein Aufräumen mehr, sondern ein Umbau von Testannahmen.
3. **Abgeteilt nach [B-446](B-446-vokabel-tipp-zeigt-den-artikel-des-fragewortes.md): der französische Tipp.** Kein Seed-Datum – der
   Tipp ist abgeleitet (`ExerciseContentResolver.cs:100`, Artikel des Wortes) und die Reparatur eine Produktfrage.
4. Bestehende Datenbanken behalten ihr Freitext-Buch: der Seed legt das Buch nur an, wenn das Kind noch keins hat (Idempotenz bleibt).

## Schätzung

**Größe: XS** (nach dem Teilen) — zwei Seed-Stellen, ein Test, Capture neu. Testweg: `SeedFamilyTests`, `docs/api-examples/me.md`.

## Verlauf

- **2026-11-08** — angelegt aus dem Rollengang ([Protokoll](../pm-sitzung-2026-11-08-rollengang.md)), Ist-Stand
  belegt (`ausformuliert`); Grillen und Schätzen im Nachtlauf (Aufräumen, Freigabe 1).
- **2026-11-09** — gegrillt (Aufräumen, Freigabe 1), dabei geteilt (B-445, B-446), geschätzt und gebaut in Lauf AG, Sprint 27. **Rote
  Probe mit Zahl:** die zwei neuen Seed-Tests gegen den Ausgangsstand → 2 von 2 rot an genau den behaupteten Werten (`Base` statt
  `Manual`; `seriesName` `null`). Capture `docs/api-examples/me.md` neu (Kategorie `Manual`/`ManualGems`, Feld `currency`); der
  Wochentags-Drift von `current` (B-442) zurückgesetzt.
  **Belege je Fall** – `.claude/scripts/mutant-probe.py`, gemessen nach dem letzten Edit an `453a740 + Arbeitsbaum`, 2026-11-09 15:44:

  | Fall | Test | Mutante | gemessen |
  |---|---|---|---|
  | AK 1 Buch → Reihe | `SeedFamilyTests.SeedBuch_HaengtAnReiheUndUnit` | MB5 `SeriesId = null` (`Seed.cs:255`) | red, am Fall |
  | AK 1 Buch → Unit | dieselbe | MB6 `CurrentUnitId = null` (`:256`) | red, am Fall |
  | AK 2 Münzen | `…Startguthaben_IstEinePapaBuchung` | MB7 wieder `Base` (`:1069`) | red, am Fall |
  | AK 2 Gems | dieselbe | MB8 wieder `Achievement` (`:1070`) | red, am Fall |

  Grundlinie nach dem Zurückspielen grün. Nicht gemessen: die Demo-Kind-Zeilen (`Seed.cs:309-310`) – kein Test liest das Demo-Kind.
- **2026-11-09** — Review `pugling-reviewer`: keine 🔴/🟡. Geprüft: Idempotenz (Buch nur ohne vorhandenes Buch), Reihenfolge
  (`SeedCatalog` vor `SeedStudentProfile`), kein Produktcode liest `PointKind.Base` aus dem Ledger (Salden unverändert, Missionen zählen
  Fortschrittstabellen). 🟢 Demo-Kind-Zeilen ungetestet → **bewusst hingenommen** (kein Test liest das Demo-Kind; die Zeilen sind
  dieselbe Umbuchung wie beim Seed-Kind, das gemessen ist). 🟢 positiver Nebeneffekt: `PositionPracticeFlowTests.cs:51`
  (`Sum(Kind == Base) > 0` für Kind 1) war bisher durch die Seed-Buchung immer erfüllt und hängt jetzt wirklich an der Übung.
- **2026-11-09** — abgenommen. Backend 1183/1183, Vitest 715/715, `tsc -b` sauber, Playwright **125 passed** (ganze Suite, Server vom Lauf nach dem letzten Edit gestartet). **Rollengang** Vater ersetzt durch `SeedFamilyTests` gegen eine frische Seed-DB (Buch mit Reihe/Unit, Startguthaben als Papa-Buchung) und die Capture `docs/api-examples/me.md`; die E2E-Suite läuft auf derselben frisch geseedeten Wegwerf-DB und bleibt grün. Ein Klick durch den Kind-Hub von „Sohn“ von Hand ist nicht gelaufen. Commit: siehe `git log -- docs/backlog/B-429-seed-familie-widerspricht-dem-katalog.md` (Lauf AG, Sprint 27).
