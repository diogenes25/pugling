---
tags: [typ/story, status/in-arbeit, bereich/klassenarbeit, bereich/gamification, bereich/lehrplan, bereich/api, rolle/student, rolle/supervisor]
aliases: [Verdichtungs-Pflicht, Zusatzpflicht vor der Klassenarbeit, Vorbereitungs-Pflicht]
status: in-arbeit
prio: P2
art: Wunsch
groesse: L
wo: backend
migration: ja
vertragsbruch: nein
quelle: B-248
unverifiziert: false
grund: ""
ersetzt_durch: []
wartet_auf: "Nutzer: Lauf I endete am Fehlerzähler; erst B-940/B-941 (und B-942/B-943), dann Abnahme"
---

# B-752 · Im Fenster vor der Klassenarbeit entsteht eine tägliche Pflicht über das Bündel

Teil (c), Kern, von [B-248](B-248-klassenarbeit-countdown-treibt-den-plan.md), geteilt beim Schätzen am 2026-11-20 (Teil (c) war als Ganzes XL und ist
in drei Stories geschnitten: dieser Kern, [B-758](B-758-vorbereitungs-pflicht-folgt-der-arbeit.md) für Verschieben, Absagen und Schreiben,
[B-757](B-757-vorbereitungs-pflicht-in-der-vater-oberflaeche.md) für die Vater-Oberfläche). Baut auf [B-659](B-659-pflicht-wird-kuer-bricht-die-serie.md)
(Spalte `DutyUntil`, Helfer, Schließ-Regel) und [B-751](B-751-klassenarbeit-bereitschaft-vokabeln.md) (geteilte Bündel-Auflösung) auf.
**Achtung:** überschneidet sich mit dem Tagesplaner [B-644](B-644-tagesplaner-statt-fester-lauf.md) (siehe Risiken); vor dem Bauen den Stand von B-644 neu lesen.

## User Story

Als *Vater* möchte ich, dass vor dem Termin einer Klassenarbeit eine zusätzliche Pflicht über den Stoff der Arbeit entsteht, damit die App die
Vorbereitung erzwingt, die ich mit dem Termin angekündigt habe.

## Ist-Stand am Code

Übernommen aus B-248 (gemessen 2026-11-19), am 2026-11-20 gegen den Code gelesen.

- Pflicht entsteht aus Positionen: eine Position bindet **eine** Übung an einen Plan mit Takt und Malus (`PlanPosition.ExerciseId`, `Cadence`,
  `PenaltyCoins`, `backend/Pugling.Api/Models/PlanPositionEntities.cs:24,51,91`); ob sie am Tag zählt, sagt `HasDutyOn`/`IsObligationOn`
  (`Services/Shared/PositionProgressService.cs:54-75`). Ein Pflicht-Ende gibt es nur als `UntilDone` mit `CompletedOn` für Nacharbeit
  (`PlanPositionEntities.cs:140-146`).
- Die Abrechnung des Malus läuft lazy beim Kind-Login (`Controllers/AuthController.cs:85`) und an einer zweiten Stelle
  (`Controllers/Student/MeController.cs:296`), höchstens 14 Tage zurück (`MaxSettleLookbackDays`, `PositionProgressService.cs:25`). Einen Zeitgeber
  gibt es nicht.
- Vorläufer „Position aus Bündel erzeugen“: `POST …/positions/from-words` (`Controllers/Supervisor/PlanPositionsController.cs:275`, B-584);
  `review_already_in_plan` verhindert Dubletten.
- Nur ein aktiver Plan je Kind und Fach (`Controllers/Supervisor/StudyPlansController.cs:166`); `Klassenarbeit.SubjectId` ist optional.
- Die Dosis einer Position: `NewItemsPerDay` (nur mit Leitner, nicht bei Mengen-Übungen, `PlanPositionsController.cs:90-98`) oder `ItemCount`
  (**Obergrenze der benutzten Inhalte**, keine Tagesmenge, `PlanPositionEntities.cs:34`).
- Der **Standard-Malus** steht serverseitig nirgends: er lebt als Konstante im Assistenten (`frontend/src/vater/VaterWizard.tsx:48-52`: „Normal“ = 5).

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-20 (Sonnet), unbestätigt; übernommen aus B-248.* Schon entschieden und nicht angefasst: Pflicht
mit Malus, festes Fenster mit Default und Änderung durch den Vater, tägliche Übungsrunde über das Bündel, Bündel-Übungen als Positionen im aktiven Plan
(Nutzer 2026-10-23, Delegation 2026-11-05, siehe Verlauf von B-248).

1. **Auslöser: Abgleich beim Kind-Login, im selben Schritt wie die Abrechnung, nie auf einem GET und nie durch einen Job** (B-248 Entscheidung 2).
   Eine idempotente Abgleich-Funktion erzeugt oder passt die Zusatzpositionen an; Idempotenz je (Klassenarbeit, Übung) über
   `PlanPosition.SourceKlassenarbeitId` (nullbar, FK `SetNull`) und `Klassenarbeit.PrepStartedOn` (nullbar). Begründung: kein Zeitgeber, und eine
   Position, die nach ihrem Anlegetag zählt (`DutyBegins`), schafft keinen rückwirkenden Malus. Kosten: hat das Kind sich mehrere Tage nicht
   eingeloggt, beginnt die Pflicht erst am Anlegetag. Verworfen: Auslösen beim Anlegen durch den Vater; Lesen der Tagessicht. Schließt aus: eine
   Pflicht ab dem ersten Fenstertag unabhängig vom Login.
2. **Pflicht-Ende über die mit B-659 gemeinsame Spalte `DutyUntil`** (B-248 Entscheidung 3, B-659 Entscheidung 2). Eine Zusatzposition trägt
   `Cadence = Daily` und `DutyUntil` = Tag vor dem Termin; danach schuldet sie nichts, auch nicht für spät abgerechnete Tage (die Abrechnung
   überspringt Perioden nach `DutyUntil`). `UntilDone` bleibt Nacharbeit. Kosten: Migration, Eintrag in den Schema-Toren, Obergrenze in
   `SettleClosedPeriodsAsync`. Verworfen: Mission oder angehobenes Ziel bestehender Positionen.
3. **Eine Position je Übung, das Bündel wird beim ersten Auslösen eingefroren** (B-248 Entscheidung 4). Eine später ins Bündel kommende oder
   nachgetaggte Übung erzeugt keine neue Pflicht mitten im Fenster; liegt die Übung schon als Position im Plan, entsteht keine zweite.
   Kosten: ein nachgetaggtes Item erscheint nicht von selbst. Verworfen: Bündel bei jedem Login neu lesen. Schließt aus: ein „lebendes“ Bündel.
4. **Kein passender Plan heißt keine Zusatzpflicht, und die Antwort der Klassenarbeiten-Liste nennt den Grund** (B-248 Entscheidung 5). Kein
   aktiver Plan im Fach oder `SubjectId` leer: nichts entsteht, die App legt keinen Plan an. Kosten: ein additives Feld in der Antwort.
   Schließt aus: dass die Kernidee **still** ausfällt.
5. **Mehrere Arbeiten im Fenster: je Übung höchstens eine Zusatzposition, sie läuft bis zum spätesten Termin** (B-248 Entscheidung 6). Die Position
   behält ihre erste `SourceKlassenarbeitId`; `DutyUntil` ist der späteste Tag vor einem Termin einer geplanten Arbeit des Kindes, deren Bündel die
   Übung enthält. Kosten: der Abgleich liest alle geplanten Arbeiten des Kindes. Verworfen: früheste Ende gewinnt.
6. **Dosis: aufrunden(Karten der Übung / Tage des Fensters), nach oben begrenzt, keine harte Tagessumme** (B-248 Entscheidung 8). Der Deckel ist
   Konfiguration (Startwert beim Bauen an echten Bündeln messen). Welches Feld sie trägt (Entscheidung 8 überließ es dem Schätzen): bei Übungen mit
   Leitner-Karte `NewItemsPerDay`; für Typen, die keine Tagesdosis kennen (Mengen-Übungen, Typen ohne Leitner), gibt es **kein** Tagesfeld – dort ist
   `ItemCount` der Deckel auf die Inhalte, und die Runde spielt diese jeden Tag. Kosten: Last und Rundung; der Vater steuert über Fenster und Bündel.
   Verworfen: Tagessumme hart gedeckelt (machte Übungen still zu Kür).
7. **Fenster: Standard 7 Tage, je Klassenarbeit änderbar, 0 schaltet die Zusatzpflicht für diese Arbeit ab** (B-248 Entscheidung 10). Neues nullbares
   Feld `Klassenarbeit.PrepWindowDays` (leer = Standard aus der Konfiguration). Kosten: ein Feld, Migration (die eine mit den Spalten aus 1 und 2).
   Verworfen: ein fester Wert.
8. **Malus und Belohnung: ein Standard-Malus je Klassenarbeit und Tag, ganzzahlig auf die Zusatzpositionen verteilt (Rest auf die erste); keine
   eigene Ziel-Belohnung** (B-248 Entscheidung 11). `PenaltyCoins` je Position aus der Verteilung, `PointsGoalMet` = 0, der Vater kann `PenaltyCoins`
   danach von Hand ändern, der Abgleich überschreibt es nicht. Kosten: Rundung. **Hier würde der Nutzer am ehesten anders entscheiden** (Malus-Höhe,
   keine Belohnung). Verworfen: voller Malus je Übung und eine Ziel-Belohnung. Schließt aus: eine Vorbereitungs-Pflicht, die Münzen **bringt**.
9. **Teil (c) steht für sich, B-680 darf es später ersetzen** (B-248 Entscheidung 12). Die Zusatzpositionen sind gewöhnliche Positionen im aktiven
   Plan. Kosten: (c) kann verworfen werden, wenn der Tagesplaner es übernimmt.

## Akzeptanzkriterien

Final. Je Kriterium die Änderung, die es rot macht (B-248 AK 5, 6, 7, 8, 10, 12, 13, 14, dazu zwei beim Schätzen ergänzte).

1. Beim ersten Login im Fenster trägt jede Bündel-Übung eine tägliche Pflicht im aktiven Plan des Fachs, ab dem Anlegetag, mit dem verteilten
   Standard-Malus; vor dem Fenster nicht. *Rot, wenn* die Pflicht vor dem Fenster entsteht oder rückwirkend Malus bucht (`DutyBegins` nicht gelesen).
2. Am Termintag und danach schuldet die Zusatzposition nichts mehr, auch nicht für spät abgerechnete Tage. *Rot, wenn* `DutyUntil` fehlt oder
   `SettleClosedPeriodsAsync` es nicht liest (Test: Login nach dem Termin, kein Malus für den Termintag).
3. Das zweimalige Abgleichen derselben Arbeit legt keine zweite Position an, und ein später nachgetaggtes Item legt keine neue Pflicht an. *Rot,
   wenn* die Idempotenz (`SourceKlassenarbeitId`, `PrepStartedOn`) fehlt oder das Bündel bei jedem Login neu gelesen wird.
4. Eine Übung, die schon als Position im Plan liegt, bekommt keine zweite Position und bleibt, wie sie ist. *Rot, wenn* eine Dublette entsteht.
5. Der Malus **eines Tages** einer Arbeit ist der Standard-Malus, auf die Zusatzpositionen verteilt, nicht je Übung vervielfacht; die Zusatzposition
   zahlt keine Ziel-Belohnung. *Rot, wenn* acht Übungen am verpassten Tag acht Mal den Standard-Malus buchen oder `PointsGoalMet` über 0 gesetzt wird.
6. Kein aktiver Plan im Fach oder keine `SubjectId`: keine Zusatzpflicht, die Klassenarbeiten-Liste nennt den Grund. *Rot, wenn* die App einen Plan
   anlegt oder der Grund fehlt.
7. Zwei Arbeiten im Fenster, dieselbe Übung: eine Position bis zum späteren Termin. *Rot, wenn* die zweite Arbeit eine zweite Position anlegt oder die
   erste das Ende festlegt.
8. Bestand: nach der Migration sind alle neuen Spalten `NULL`; an einer Kopie der echten DB ändern sich Plan, Serie und Malus-Stand nicht (Vorbild
   `ActivatedAtMigrationTests`). *Rot, wenn* die Migration etwas vorbelegt.
9. Hat die Arbeit `PrepWindowDays = 0`, entsteht beim Abgleich keine Zusatzpflicht (B-248 AK 9, zweite Hälfte). *Rot, wenn* das Feld nicht gelesen wird.
10. Die Dosis einer Zusatzposition mit Leitner-Karte ist aufrunden(Karten / Fenstertage), höchstens der konfigurierte Deckel; bei Typen ohne Tagesdosis
    trägt `ItemCount` den Deckel. *Rot, wenn* die Dosis fehlt (alle Karten am ersten Tag) oder der Deckel nicht gelesen wird. (B-248 hatte zu Entscheidung 8
    kein eigenes Kriterium.)

## Schätzung

**Größe: L** – wie eine DB-Umbau-Etappe (E6): drei Spalten an zwei Tabellen mit Bestandsdaten, ein neuer idempotenter Abgleich mit Auslöser im
Login, Dosis-, Dubletten- und Malus-Verteilungsregeln sowie ein Eingriff im Kern der Abrechnung. Als Ganzes mit Verschieben/Absagen und Oberfläche
wäre es XL gewesen, darum der Schnitt.

- **`migration: ja`** – nachgesehen: `PlanPositions.SourceKlassenarbeitId` (FK, `SetNull`, G2), `Klassenarbeiten.PrepStartedOn` und
  `Klassenarbeiten.PrepWindowDays` (nullbar); `DutyUntil` legt [B-659](B-659-pflicht-wird-kuer-bricht-die-serie.md) an. Die Migration hängt an die
  Kette hinter dessen Glied, ohne Backfill, Probe an einer Kopie der echten DB.
- **`vertragsbruch: nein`** – nachgesehen: `KlassenarbeitResponse`, `CreateClassTestDto`, `UpdateClassTestDto` (`Contracts/Supervisor/ClassTestDtos.cs`)
  bekommen **trailing optionale** Felder (`PrepWindowDays`, `PrepCardsPerDay`, ein Grund-Kennzeichen, dazu ein Schalter `ClearPrepWindow` nach dem Muster `ClearGrade`, damit ein gesetztes Fenster wieder auf „Standard“ zurückgeht); konstruiert ein Test die Antwort positional,
  zieht er nach, das ist kein Bruch.
- **`wo: backend`** – die Oberfläche steht in [B-757](B-757-vorbereitungs-pflicht-in-der-vater-oberflaeche.md).
- **Reihenfolge:** B-659 und B-751 zuerst; danach dieser Kern; danach B-758 und B-757.

### Risiken

- **Überschneidung mit B-644, und sie ist groß.** B-644 nennt B-248 ausdrücklich als von ihm übernommen („Menge aus … dem Abstand zum Termin … der
  Klassenarbeit (übernimmt B-248)“), plant mit E3 den Wegfall der festen Pflicht an der Position (`Cadence`/`PenaltyCoins`) und mit E4 die Terminregeln.
  Die Zusatzpositionen dieser Story hängen genau an dem, was B-644 auflöst. Baut man (c) vor E3, entsteht Code, den der Stichtag wieder abräumt; baut
  man ihn danach, ist er überflüssig. **Zu entscheiden ist vor dem Bau, ob (c) überhaupt noch gebraucht wird** (B-248 Entscheidung 12 hielt es offen).
  Dasselbe gilt für B-758 und B-757.
- **Die Dosis hat für Nicht-Leitner-Typen kein Tagesfeld** (Entscheidung 6): dort spielt die Runde alle Inhalte unter dem Deckel jeden Tag; für ein
  großes Grammatik-Bündel ist das Last, die das Verfahren nicht verteilt.
- **Rest der Malus-Verteilung ist schief:** „der Rest auf die erste“ heißt bei Standard 5 und acht Übungen: eine Position trägt 5, sieben tragen 0. Wer
  nur die erste Übung verpasst, zahlt 5; wer jede andere verpasst, zahlt nichts. Die Summe bleibt der Standard-Malus (AK 5). Eine Verteilung auf mehrere
  Positionen (je 1) wäre gleichmäßiger; das ist eine Änderung der Entscheidung und gehört dem Nutzer.
- **Zwei Abrechnungsstellen:** der Abgleich gehört an beide Stellen, an denen die Abrechnung läuft (`AuthController.cs:85`, `MeController.cs:296`), und
  muss dort **vor** der Abrechnung laufen, sonst fehlt der ersten Periode die Position. Ein Kind, das sich über den konto-zentrischen Login anmeldet,
  löst heute keine Abrechnung aus (nur `LoginChild` ruft sie); ob das ein eigener Defekt ist, ist nicht geprüft.
- **Standard-Malus ist neu auf dem Server:** der Wert lebt nur im Assistenten (`VaterWizard.tsx:48-52`); (c) braucht eine Konfiguration `Startwert 5`,
  die mit der Konstante des Assistenten auseinanderlaufen kann.
- **Wallet-Invariante:** die Zusatzposition bucht nur über den bestehenden Malus-Pfad; kein neuer Pfad, der den Kontostand ändert, entsteht (die
  `ConcurrencyStamp`-Regel gilt weiter, nichts zu tun).
- **Lesestellen von `DutyUntil`:** die der Helfer aus B-659 liefert; hier kommt die Obergrenze in `SettleClosedPeriodsAsync` (`:761`) hinzu, die B-659 bewusst
  nicht anfasst (AK 4 dort ist die Gegenprobe).

### Angriffsplan

1. B-659 und B-751 gebaut; Entity-Felder und Migration
   (`dotnet dotnet-ef migrations add VorbereitungsPflicht --project backend/Pugling.Api --output-dir Data/Migrations`).
2. Dienst `KlassenarbeitPrepService` (Name vorläufig) unter `Services/Shared`: Fenster, Bündel einfrieren (`PrepStartedOn`), Zusatzpositionen anlegen
   (Plan des Fachs suchen, Dublette prüfen, Dosis, `DutyUntil`, verteilter Malus, `PointsGoalMet = 0`), `DutyUntil` bei mehreren Arbeiten neu rechnen,
   Grund liefern.
3. Aufruf an beiden Abrechnungsstellen vor `SettleClosedPeriodsAsync`.
4. Obergrenze `DutyUntil` in `SettleClosedPeriodsAsync`.
5. Antwortfelder an `KlassenarbeitResponse` und Konfigurationswerte (Fenster-Standard, Malus, Deckel).
6. `dotnet build Pugling.sln`, `dotnet format Pugling.sln`.

### Testweg

- Neue Klasse `VorbereitungsPflichtTests` in `backend/Pugling.Api.Tests` (Aufbau über `TestApi`, Muster `PflichtMalusTests` und
  `RueckwirkendePflichtTests`): AK 1 bis 7, 9, 10, je Fall ein eigener Test; AK 5 mit acht Übungen.
- Neue Klasse `VorbereitungsPflichtMigrationTests` nach dem Muster `ActivatedAtMigrationTests`: AK 8.
- `PflichtMalusTests`, `RueckwirkendePflichtTests`, `KlassenarbeitenTests`, `ConventionGuardTests`, `SchemaGuardTests` und `EndpointCoverageGuard`
  bleiben grün.
- Vor der Abnahme: Agent `pugling-reviewer`; `/smoke-test`; Rollengang: Vater plant Arbeit, Kind loggt ein, Tagessicht zeigt die Pflicht (E2E mit B-757,
  `klassenarbeit-vorbereitung.spec.ts`).

## Verlauf

- **2026-11-20** — angelegt beim Teilen von [B-248](B-248-klassenarbeit-countdown-treibt-den-plan.md) und geschätzt (Vorbereitungslauf, Sonnet): L,
  backend, Migration ja, Vertrag additiv. Größtes Risiko: der Tagesplaner B-644 übernimmt die Terminpflicht und löst die Positions-Pflicht auf.
- **2026-11-24** — `geschaetzt` → `in-arbeit` (Nachtlauf I, Sprint 134, Freigabe 8; B-248 trägt die Freigabe des Nutzers vom
  2026-10-23). Vor dem Bau geprüft: B-644 ist verworfen/geteilt, die Positions-Pflicht besteht; Entscheidung 9 trägt den Bau.
  Gebaut (`f1be5aba`, Review-Fixes `d0fe6477`): `Services/Shared/ClassTestPrepService.cs` (Abgleich beim Kind-Login und vor dem Kauf,
  jeweils vor `SettleClosedPeriodsAsync`), Migration `20261123232647_ClassTestPrepDuty` (drei nullable Spalten, FK `SetNull`, kein
  Backfill), Obergrenze `DutyUntil` in der Abrechnung, Felder an `KlassenarbeitResponse`/`Create…`/`UpdateClassTestDto` (additiv),
  Konfiguration `ClassTestPrep` (Fenster 7, Malus 5, Deckel 10 – der Deckel ist **nicht** an echten Bündeln gemessen, die echte DB
  ist für den Nachtlauf tabu). Lesarten: „Fenstertage“ der Dosis = Resttage bis zum Termin beim Start (am ersten Fenstertag = Fenster);
  „Leitner-Karte“ = `Exercise.DefaultUseLeitner` und kein Mengen-Typ (`GradesAsSet`); eine Übung ohne Inhalt wird keine Pflicht.
- **2026-11-24** — Belege je AK (je AK der tragende Test; **die Mutanten-Probe über diese Fälle ist nicht gelaufen** – der Lauf
  endete vorher am Fehlerzähler; gemessen sind nur die Review-Fixes R1/R2/R3, Tabelle im Protokoll): AK 1 Fenster → `Pflicht_Entsteht_Erst_Im_Fenster…`, kein Rückwirken → `Spaeter_Erster_Login_Im_Fenster…` (R2,
  vom Review gefunden: vorher kein Test); AK 2 → `Termintag_Und_Danach…`; AK 3 → `Zweiter_Abgleich…`; AK 4 →
  `Uebung_Schon_Im_Plan…`; AK 5 → `Malus_Eines_Tages…`; AK 6 → `Ohne_Fach_Oder_Ohne_Aktiven_Plan…`,
  `Inaktiver_Plan_Im_Fach…`; AK 7 → `Zwei_Arbeiten…`, `Spaetere_Arbeit_Verlaengert…`; AK 8 →
  `VorbereitungsPflichtMigrationTests` (kein Backfill; nach dem Review auch Malus-Zeilen, Lernstand, Versuche überleben den
  Tabellen-Neubau – ohne rote Probe, eine Kaskade lässt sich nicht als Zeilen-Mutante erzeugen); **an einer Kopie der echten DB nicht
  geprüft** (tabu); AK 9 → `Fenster_Null_Schaltet…`; AK 10 → `Dosis_Ist_Aufgerundet…`. Zweiter Aufrufort →
  `Kauf_Gleicht_Ab_Wie_Der_Login`.
- **2026-11-24** — Review `pugling-reviewer`: 🔴 ein Speichern der Position (das Formular schickt die Taktung immer mit) löschte das Ende
  der Vorbereitung → endlose Tagespflicht mit Malus; behoben in `PlanPositionsController` (nur ein echter Taktwechsel räumt die Spanne)
  und im Abgleich (Vorbereitung ohne Ende schließt), Test `Speichern_Der_Position_Nach_Dem_Termin…`, R1 rot. 🟡 gleichzeitige Abgleiche
  legten doppelt an → Start wird per `ExecuteUpdateAsync` beansprucht (ohne deterministischen Test; R3 bleibt grün, in B-944 benannt).
  🟡 Rückwirken ungetestet → Test ergänzt. 🟡 Migrationstest sah keine Kaskade → Abhängige ergänzt. **Nicht behoben**, weil der
  Fehlerzähler bei 5 stand: 🟡 Kind verkleinert sein Bündel über Schlagwörter → [B-940](B-940-kind-verkleinert-sein-vorbereitungs-buendel.md),
  🟡 Ausführ-Recht umgangen → [B-941](B-941-vorbereitung-umgeht-das-ausfuehr-recht.md); 🟢 gesammelt in
  [B-944](B-944-vorbereitungs-pflicht-restpunkte.md).
- **2026-11-24** — Rollengang (maschinell): E2E `e2e/klassenarbeit-vorbereitung.spec.ts` 1 passed (Ports 5231/5204) – der Vater plant mit
  Fenster 12, sieht „etwa 2 Karten je Tag“, das Kind findet nach dem Login die Pflicht als Knoten auf seinem Pfad. Bleibt `in-arbeit`:
  zwei 🟡 offen (B-940, B-941), der Lauf endete am Fehlerzähler.
