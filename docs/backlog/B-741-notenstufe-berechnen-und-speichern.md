---
tags: [typ/story, status/in-arbeit, bereich/gamification, bereich/lernstand, rolle/student]
aliases: [Notenstufe berechnen, ChildSubjectLevel]
status: in-arbeit
prio: P2
art: Wunsch
groesse: M
wo: backend
migration: ja
vertragsbruch: nein
quelle: B-682
grund: ""
ersetzt_durch: []
---

# B-741 · Die Notenstufe je Kind und Fach berechnen und speichern

Der Kern von [B-682](B-682-notenstufe-je-fach-statt-liga.md): aus dem Anteil beherrschter Wörter am Stoff eines Faches entsteht eine
Stufe 1 bis 6, sie wird je Kind und Fach gespeichert, sinkt höchstens eine Stufe je sieben Tage, und die beste je erreichte Stufe
bleibt stehen. Noch kein Endpunkt und keine Oberfläche ([B-742](B-742-notenstufe-lesen-student-und-supervisor.md),
[B-743](B-743-notenstufe-anzeige-kind-und-vater.md)). Baut auf [B-740](B-740-notenschluessel-percenttograde.md) auf.

*Entscheidungen stellvertretend im Vorbereitungslauf 2026-11-20 (Sonnet) gefällt, unbestätigt; Herkunft B-682.*

## User Story

Als *Vater* möchte ich, dass die Stufe nur aus tatsächlich gewerteten Antworten entsteht und nicht von einem schlechten Tag
umgeworfen wird, damit sich das Kind nicht zur 1 spielt und die Stufe trotzdem ehrlich bleibt.

## Ist-Stand am Code

Gemessen am 2026-11-20 auf `main`; Pfade relativ zu `backend/Pugling.Api/`.

- Eingang je Fach liegt vor: `ChildLearnProgressService.SubjectsAsync` rollt je Fach `TotalItems` und `MasteredItems` auf
  (`Services/Student/ChildLearnProgressService.cs:160-181`). „Beherrscht" heißt dort `Box >= ItemProgress.MaxBox`
  (`:105`), nicht eine Prozentschwelle.
- **Der Rahmen ist weiter als „aktiver Plan":** `LoadRelevantAsync` (`:50-`) nimmt alle Übungen aus **jedem** Plan des Kindes plus alle mit
  Lernstand; `Active` ist nur ein Merkmal je Zeile. Die Entscheidung verlangt den Stoff der Units des aktiven Plans, also einen
  Filter auf `Active`, den es heute im Rollup nicht gibt.
- Nur Vokabeltypen tragen Wortfortschritt (`registry.KeysSupportingItemProgress`, `:69`); Mathe und Grammatik haben keinen
  Eintrag, ihr Fach liefert `TotalItems == 0`.
- Es gibt keinen täglichen Hintergrunddienst für Kinder. Träge, beim ersten Zugriff, laufen heute
  `PositionProgressService.SettleClosedPeriodsAsync` bei der Kind-Anmeldung (`Controllers/AuthController.cs:85`) und vor dem Kauf
  (`Controllers/Student/MeController.cs:296`). Der „Tagesplaner" aus B-644 steht auf `gegrillt` und ist nicht gebaut.
- Tabelle, Dienst oder Spalte für eine Stufe: nicht gefunden.
- Migrationskette: jüngstes Glied `20261119065732_ShopStockPerChild` (`Data/Migrations`); `SchemaGuardTests` G1b erlaubt nur
  Anhängen.

## Entscheidungen

Herkunft in Klammern; Wortlaut verkürzt, die volle Fassung steht in B-682.

1. **Maß: Anteil beherrschter Wörter am gesamten Stoff der Units des aktiven Plans, nur Vokabel-Fächer** (B-682 Entscheidung 1).
   Kosten: Die Stufe steigt langsam bei großem Stoff; Fächer ohne Wortliste bekommen keine Stufe. Schließt aus: Mathe und Grammatik
   im ersten Bau.
2. **Neubewertung nur im täglichen Lauf, nicht je Antwort; Senkung durch den Lernstand höchstens eine Stufe je sieben Tage;
   Anstieg unbegrenzt** (B-682 Entscheidung 4). Der tägliche Lauf ist der träge Aufruf beim ersten Zugriff des Tages, solange der
   Planer fehlt. Kosten: zwei gespeicherte Werte (Stufe, Datum der letzten Senkung).
3. **Eigene Tabelle je (Kind, Fach) mit Stufe, Bestwert, Datum der letzten Senkung und Berechnungstag** (B-682 Entscheidungen 5 und 8).
   Der Bestwert sinkt nie. Kosten: eine Migration; eine bewusste Zeile im Tor G2 (Löschverhalten der neuen Beziehungen).
4. **Eine Stufenänderung bucht nichts aufs Kassenbuch** (B-682 Entscheidung 9, Rahmen F).

## Akzeptanzkriterien

1. Null beherrschte Stoff-Wörter ergeben eine schlechtere Stufe als alle beherrscht; die Antwortzahl zählt nicht. *Rot, wenn* der
   Nenner die eingeführten statt aller Stoff-Wörter ist.
2. Drei perfekt beherrschte Wörter von 200 ergeben eine niedrigere Stufe als 150 beherrschte. *Rot, wenn* `AvgMasteryPercent` das
   Maß ist.
3. Dieselben Daten am selben Tag ergeben dieselbe Stufe. *Rot, wenn* die Berechnung von etwas anderem als Daten und Tagesdatum
   abhängt.
4. Fällt der Lernstand stark, sinkt die Stufe um höchstens eine je sieben Tage. *Rot, wenn* ein Lauf um zwei Stufen senkt oder zweimal
   binnen sieben Tagen senkt.
5. Ein zweiter Lauf am selben Tag ändert nichts. *Rot, wenn* der Lauf je Zugriff neu bewertet.
6. `BestLevel` sinkt nie. *Rot, wenn* eine Senkung der laufenden Stufe den Bestwert überschreibt.
7. Ein Fach ohne Maß (kein Wortschatz im aktiven Plan) bekommt keine Zeile. *Rot, wenn* ein Mathe-Fach eine Stufe bekommt.
8. Zählt nur der aktive Plan: Wörter eines inaktiven Plans heben die Stufe nicht. *Rot, wenn* der Rahmen alle Pläne umfasst.
9. Ein Stufenwechsel erzeugt keinen `ChildPointsEntry`. *Rot, wenn* dabei ein Kassenbucheintrag entsteht.
10. *Absichtserklärung.* Kein Request-Feld beeinflusst die Stufe; der Dienst liest nur serverseitige Daten. *Rot, wenn* ein Endpunkt
    (auch ein späterer) die Stufe aus Client-Angaben setzt; im Ausgangszustand gibt es keinen schreibenden Endpunkt, das Kriterium
    kann also erst durch einen künftigen Fehler fallen.

## Schätzung

**Größe: M**, wie der Batch-Pfad im `MediaSelector` (B-03), mit dem Aufschlag einer Migration: ein neuer Dienst, eine Tabelle, ein
Rollup-Filter und zwei Einhängestellen.

- **`wo: backend`**: kein Vertrag, keine Oberfläche in diesem Stück.
- **`migration: ja`**: neue Tabelle `ChildSubjectLevel` (Kind, Fach, Stufe, Bestwert, Senkungsdatum, Berechnungstag), eindeutiger
  Index auf (Kind, Fach). Eine neue Migration wird an die Kette gehängt; die Tabelle ist leer, nimmt also keine Bestandszeilen
  mit, aber die echte `pugling.db` muss sie ohne Verlust aufnehmen.
- **`vertragsbruch: nein`**: nichts in `Pugling.Contracts`.

### Risiken

- **Der Nenner.** Das bestehende Fach-Rollup mischt alle Pläne; der Filter auf aktive Pläne ist neu und gehört in den Dienst, nicht in
  eine Kopie der Rechnung. Eine zweite Rechnung neben `ChildLearnProgressService` wäre die Drift, die das Repo kennt.
- **Der „tägliche Lauf" ist ein Platzhalter.** Er hängt an Kind-Anmeldung und Lese-Zugriff (siehe oben). Ein Kind, das tagelang
  nicht zugreift, wird nicht neu bewertet; das ist beabsichtigt (keine Senkung ohne Lernen), muss aber in der Doku des Dienstes
  stehen. Kommt der Planer (B-644), zieht der Lauf dorthin um.
- **Nebenläufigkeit.** Zwei gleichzeitige erste Zugriffe schreiben dieselbe Zeile; der eindeutige Index braucht dieselbe
  Verlierer-Behandlung wie `PositionProgressService` (Kommentar bei `:537`). Kein Wallet-Pfad, die Wallet-Invariante gilt nicht.
- **Stufe gegen Note:** Wie aus einer Note mit Dezimalstelle eine ganze Stufe wird, steht nicht fest (Vorschlag: ganze Note laut
  [B-740](B-740-notenschluessel-percenttograde.md)).
- **Tore:** `SchemaGuardTests` G2 (neue FKs brauchen ein bestätigtes Löschverhalten: Vorschlag Cascade zum Kind, Restrict zum Fach,
  wie die übrigen Kind-Tabellen), G6 (Datum als `DateOnly`, nicht als Text), G1 (Modell gleich Snapshot).

### Angriffsplan

1. Entität `ChildSubjectLevel` und Konfiguration im `PuglingDbContext`; Migration anhängen (`dotnet dotnet-ef migrations add
   ChildSubjectLevel`); G2-Zeile ergänzen.
2. Rollup-Zugriff: eine interne Methode in `ChildLearnProgressService`, die je Fach `Mastered` und `Total` nur für aktive Pläne
   liefert; kein neuer Aufrufer der privaten Rechnung.
3. Dienst `SubjectLevelService` in `Services/Student` (oder `Shared`): Berechnen, Senkungsfrist, Bestwert, idempotent je Tag.
4. Einhängen an `AuthController` (Kind-Anmeldung, neben `:85`) und an die Lese-Endpunkte aus
   [B-742](B-742-notenstufe-lesen-student-und-supervisor.md); in diesem Stück nur der Dienstaufruf an der Anmeldung.
5. `dotnet build Pugling.sln`, `dotnet format Pugling.sln`.

### Testweg

- Neue Testklasse `ChildSubjectLevelTests` (Integrationstest über `PuglingWebAppFactory`, Aufbau wie `ChildLearnProgressTests`):
  Daten über die Spiel-Endpunkte erzeugen, nicht per Direktschreibung (AK 1, 2, 7, 8), Senkungsfrist mit der steuerbaren Uhr der Fabrik
  (`TestClock`, `PuglingWebAppFactory.cs:68`; AK 4, 5, 6).
- Neue Testklasse `ChildSubjectLevelMigrationTests` nach dem Muster von `ShopStockPerChildMigrationTests` (die Migration läuft
  gegen eine Datenbank mit Bestand durch).
- `SchemaGuardTests` (G1, G1b, G2, G6) laufen mit; AK 9 über einen Zahlungsweg-Test (kein neuer `ChildPointsEntry`).
- Vor der Abnahme: Agent `pugling-reviewer`; ein Rollengang entfällt, solange nichts sichtbar ist, und wird in
  [B-743](B-743-notenstufe-anzeige-kind-und-vater.md) nachgeholt (Verlauf-Zeile).

## Verlauf

- **2026-11-20** — angelegt aus [B-682](B-682-notenstufe-je-fach-statt-liga.md) (geteilt); `geschaetzt`: M, backend, Migration ja,
  kein Vertragsbruch (Vorbereitungslauf, Sonnet).
- **2026-11-23** — `in-arbeit` (Nachtlauf G, Sprint 127). Gebaut: Entität `ChildSubjectLevel` (`Models/SubjectLevelEntities.cs`:
  Stufe, Bestwert, `LastLoweredOn`, `EvaluatedOn`, eindeutiger Index (Kind, Fach)), Migration
  `20261123220126_NotenstufeChildSubjectLevel` (nur `CreateTable` + zwei Indizes, gelesen), G2-Zeilen in
  `SchemaGuardTests`. Maß: neue interne Methode `ChildLearnProgressService.ActiveSubjectMeasuresAsync` (dieselbe
  Rechnung `LoadRelevantAsync`/`LoadAggAsync`/`Combine`, gefiltert auf `Active`, Fächer mit `TotalItems == 0` fehlen).
  Dienst `Services/Student/SubjectLevelService.cs` (`EvaluateAsync(childId, day)`, `Apply`), träge an der
  Kind-Anmeldung (`AuthController.LoginChild`, nach `objectiveRewards.SettleAsync`). Abweichungen ohne neue
  Entscheidung: (a) **Fach-FK `Cascade` statt `Restrict`** (Risiken nannten Restrict nur als Vorschlag): die Stufe ist
  abgeleitet, ein Fach ist erst löschbar, wenn keine Übung es trägt (`SubjectsController.Delete`), und `Restrict`
  ohne Vorprüfung machte aus dem Löschen eine nackte 500; (b) **„aktiver Plan“ heißt alle aktiven Pläne** des Kindes
  (seit B-539 einer je Fach), Rahmen = Übungen, die ein aktiver Plan referenziert, wie das `Active`-Merkmal des
  Rollups; (c) Senkung: die erste Senkung darf sofort kommen (kein Datum), jede weitere erst sieben Tage nach der
  vorigen; ein Anstieg setzt die Frist nicht zurück; (d) der Verlierer zweier gleichzeitiger Erst-Anlagen löst seine
  Zeilen vom Tracker (Unique-Verletzung, Muster `ShopService.TrySaveAsync`) – nicht gemessen, kein Test (Rennen ist im
  In-Process-Host nicht deterministisch herstellbar).
- **2026-11-23** — Testweg-Abweichung: Die Beherrschung wird in `ChildSubjectLevelTests` als `ItemProgress`-Zeile
  geschrieben statt über die Spiel-Endpunkte erspielt (Box 5 braucht Wochen von Leitner-Tagen je Wort). Die Stufe liest
  nur, was die Wertungsstellen schreiben; deren Weg in `ItemProgress` hat eigene Tests (`ChildLearnProgressTests`). Den
  Tag übergibt der Test ausdrücklich, die Sieben-Tage-Regel braucht darum keine Uhr.
- **2026-11-23** — AK → Test (`backend/Pugling.Api.Tests/ChildSubjectLevelTests.cs`), Mutante → rot (Tabellen aus
  `.claude/scripts/mutant-probe.py`, gemessen an `a782b4b2` + Arbeitsbaum, 2026-11-23 23:49 und 2026-11-24 00:10):
  AK 1 (Nenner/Antwortzahl) → `Null_beherrscht_steht_unter_alles_beherrscht`, M6 (Maß = `Correct/Seen`,
  `ChildLearnProgressService.cs:152`) → rot · AK 2 (`AvgMastery`-artig) → `Drei_von_200_ist_schlechter_als_150_von_200`,
  M5 (Nenner = eingeführte Wörter, `:152`) → rot · AK 3/5 (einmal je Tag) → `Zweiter_Lauf_am_selben_Tag_aendert_nichts`,
  M8 (`else if (row.EvaluatedOn < day)` → `else`, `SubjectLevelService.cs:58`) → rot · Anstieg auch um genau eine
  Stufe → `Absturz_senkt_eine_Stufe_je_sieben_Tage_Bestwert_bleibt` (letzter Abschnitt), M12 (`measured < row.Level - 1`,
  `:86`) → rot · AK 4 (Frist) → derselbe Test, M9 (Frist 0 statt 7, `:89`) → rot; (eine Stufe) M10
  (`row.Level = measured`, `:91`) → rot · AK 6 (Bestwert) → derselbe Test, M11 (`BestLevel = row.Level`, `:94`) → rot ·
  AK 7 (Fach ohne Maß) → `Fach_ohne_Wortschatz_bekommt_keine_Zeile`: **zwei redundante Wächter**, keiner allein rot zu
  machen – M7 (`TotalItems > 0` → `>= 0`, `:151`) grün/abweichend und M7b (Typfilter `KeysSupportingItemProgress` in
  `LoadRelevantAsync` entfernt, `:74`) grün wie erwartet; die Rechenübung fällt am jeweils anderen. Der Test belegt das
  Verhalten, nicht eine einzelne Zeile · AK 8 (nur aktiver Plan) → `Nur_der_aktive_Plan_zaehlt`, M4 (`.Where(r =>
  r.Active)` entfernt, `:145`) → rot · AK 9 (keine Buchung) → `Stufenwechsel_bucht_nichts` (Ledger-Zählung vor/nach;
  nicht als Mutante gemessen – der Dienst hat keinen Buchungsweg, eine Mutante müsste ihn erst erfinden) · Anmeldung
  bewertet → `Kind_Anmeldung_bewertet_den_Tag`, M13 (`if (child.Id < 0)` vor dem Aufruf, `AuthController.cs:91`) → rot
  (erste Fassung `await Task.CompletedTask` kompilierte nicht: ungenutzter Primärkonstruktor-Parameter) ·
  AK 10 Absichtserklärung (kein schreibender Endpunkt) · Migration → `NotenstufeMigrationTests` (Bestand bleibt,
  Eindeutigkeit (Kind, Fach)).
- **2026-11-23** — Eigener Fund der Mutanten-Probe (Freigabe 11, als 🟡 gezählt, Sprint-Zähler **1**): M12 blieb grün –
  kein Test stieg um genau eine Stufe. Behoben mit einem Abschnitt im Sturz-Test (9 von 10 = Note 2 am Tag nach der
  zweiten Senkung), danach M12 rot.
