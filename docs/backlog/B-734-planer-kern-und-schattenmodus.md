---
tags: [typ/story, status/geschaetzt, bereich/lehrplan, bereich/training, rolle/supervisor, rolle/student]
aliases: [Planer-Kern, Tagesplan, Schattenmodus, 6-Uhr-Lauf, Etappe E2]
status: geschaetzt
prio: P1
art: Wunsch
groesse: L
wo: beides
migration: ja
vertragsbruch: nein
quelle: B-644
grund: ""
ersetzt_durch: []
---

# B-734 · Der Planer erzeugt täglich einen eingefrorenen Tagesplan, im Schattenmodus ohne Wirkung (B-644, Etappe E2)

Fünfte Teilstory aus der Teilung von [B-644](B-644-tagesplaner-statt-fester-lauf.md). Der Kern des Planers: ein
deterministischer Dienst in C#, der je Kind und Lerntag einen **Tagesplan** aus Tagesaufgaben mit Begründung erzeugt,
idempotent, ausgelöst beim ersten Zugriff nach 6 Uhr Ortszeit und von einem kleinen Hintergrunddienst. Er rechnet mit den
Leitplanken ([B-731](B-731-lerntage-pause-und-leitplanken-je-kind.md)) und dem Profil
([B-733](B-733-tagesauswertung-und-leistungsprofil.md)) und läuft eine Woche im **Schattenmodus**: Der Vater sieht die
Pläne, sie wirken auf Pflicht, Malus und Sohn-App nicht.

**Stand:** Eintrittsbedingung `gegrillt` aus B-644 und B-680 geerbt. Entscheidungen 9, 10, 14 stammen vom Vater, 19, 21, 24
hat der Nutzer am 2026-11-26 bestätigt.

## User Story

Als *Vater* möchte ich jeden Morgen sehen, was der Planer für jedes Kind vorgesehen hat und warum, eine Woche lang ohne
Folgen, damit ich dem Planer erst traue, wenn er mir sieben Tage lang plausibel vorkam. Als *Sohn* möchte ich später
Aufgaben, die zu meinem Stand passen; dafür muss der Plan eindeutig und nachrechenbar entstehen.

## Ist-Stand am Code

Gemessen am 2026-11-20 auf `main`. Pfade relativ zu `backend/Pugling.Api/`.

- **Kein Zeitgeber:** Suche nach `BackgroundService`, `IHostedService`, `PeriodicTimer` im ganzen Backend ohne Treffer. Der Malus wird träge
  beim Kind-Login abgerechnet (`Controllers/AuthController.cs:85`); die Tagessicht rechnet nur, sie legt nichts an
  (`PositionProgressService.ComputeChildDayAsync`, `Services/Shared/PositionProgressService.cs:407`; Leser
  `Controllers/Student/ChildTodayController.cs:19`, `:27`).
- **Die Pflicht steht an der Position:** `PlanPosition.NewItemsPerDay`, `Cadence`, `GoalThreshold`, `PenaltyCoins`
  (`Models/PlanPositionEntities.cs:40`, `:51`, `:75`, `:86`). Ein aktiver Plan je Kind und Fach (B-539) ist gebaut.
- **Kein Plan- oder Aufgabenmodell für den Tag:** Eine Tagesplan-Entität, eine Tagesaufgabe und ein Modus
  (Schatten/scharf) gibt es nicht (keine Fundstelle).
- **Test-Host:** Hintergrunddienste liefen im `WebApplicationFactory` mit; ein bestehendes Muster zum Abschalten ist
  `RateLimiting:LoginEnabled` (`CLAUDE.md`, Fallstricke PIN-Login). `ProductionStartupTests` prüft den Produktionsstart.
- **Kein LLM im Backend** und keine Chat-Abhängigkeit in der API (die Chat-Clients liegen in `Pugling.Agent.Creator`).
- **Wallet-Invariante:** Jeder abbuchende Pfad bumpt `child.ConcurrencyStamp` (`PositionProgressService.cs:827`,
  `DailyBoxService.cs:142`). Diese Story bucht nichts; die Invariante greift ab [B-736](B-736-pflicht-aus-der-tagesaufgabe.md).

## Entscheidungen (geerbt)

1. **Der Planer entscheidet mit festen Regeln im Programm, ohne Sprachmodell** (B-644 Entscheidung 10; Entscheidung 21).
   Dieselben Daten ergeben denselben Plan.
2. **Der Plan entsteht beim ersten Zugriff nach 6 Uhr Ortszeit, derselbe idempotente Aufruf wie der Hintergrunddienst**
   (Entscheidung 9, Vater; B-680 Entscheidung 7).
3. **Eine Woche Schattenmodus vor dem Stichtag; der Vater sieht die Tagespläne und setzt den Stichtag** (Entscheidung 14,
   Vater). Die Anzeige ist die Vorstufe der späteren Planer-Ansicht, kein Wegwerfwerk.
4. **Der Tagesplan ist nach dem Erzeugen eingefroren** (B-644 Selbstbetrug; B-680 Entscheidung 4, Rahmen E).
5. **Tag ohne erzeugten Plan zählt wie eine Pause und wird dem Vater gemeldet** (Entscheidung 19, stellvertretend,
   unbestätigt).
6. **Der Tagesplan ist eindeutig je Kind und Lerntag; das Fach ist Eigenschaft der Aufgabe** (B-680 Entscheidung 12).
7. **Der Planer wählt nur aus Positionen des aktiven Plans** (Entscheidung 25, Rahmen G): kein Modellinhalt wird selbst zur
   Pflicht.
8. **In diesem Zuschnitt gelten die Regeln 1 (fällige Wiederholungen), 2 (Gescheitertes kommt eine Stufe leichter wieder) und
   6 (Boden und Deckel)** aus B-644. Termin, Stundenplan, Fächerverteilung und Rückstand sind
   [B-680](B-680-taeglicher-lehrplan-je-fach-just-in-time.md), Bonus und Eingriffe
   [B-738](B-738-eingriffe-bonus-und-sohn-begruendung.md).

### Schätz-Annahmen (stellvertretend, unbestätigt)

1. **Der Modus liegt in einer Ein-Zeilen-Tabelle `PlannerSettings` (Modus, Stichtag), nicht in `appsettings.json`.** Begründung:
   [B-737](B-737-reset-zum-stichtag.md) kippt Reset und Modus in **einer** Transaktion; eine Konfigurationsdatei ließe einen Neustart
   dazwischen. Kosten: eine weitere Tabelle. Verworfen: `Planner:Mode` als Konfiguration. Ohne Zeile gilt Schatten.
2. **Der Hintergrunddienst ist abschaltbar (`Planner:BackgroundEnabled`, im Test-Host aus)** und ruft nur die Methode des trägen
   Aufrufs. Begründung: ein Codepfad, wie bei `RateLimiting:LoginEnabled`.
3. **Das Tempo neuer Wörter kommt hier aus dem Profil ([B-733](B-733-tagesauswertung-und-leistungsprofil.md)), geklemmt auf Boden und
   Deckel;** ohne Profil (die ersten Wochen) gilt der Boden. Eine Rückwärtsrechnung vom Termin kommt mit B-680.
4. **Die Begründung ist ein Text aus Bausteinen je Regel** („6 fällige Wiederholungen", „gestern 3 Fehler bei …"), keine
   feste Zeichenkette (Akzeptanzkriterium 4 aus B-644).

## Akzeptanzkriterien

1. Je Kind und Lerntag entsteht genau ein Tagesplan, um 6 Uhr durch den Zeitgeber oder beim ersten Zugriff danach; zweimaliges
   Auslösen und gleichzeitiges Auslösen legen keinen zweiten an. *Rot, wenn* der eindeutige Index auf (Kind, Tag) fehlt oder ein
   paralleler Lauf an der Verletzung des Index scheitert statt den vorhandenen Plan zu liefern.
2. Der Plan enthält mindestens den Boden und höchstens den Deckel an Pflichtaufgaben und neuen Wörtern und ist an einem Lerntag
   nie leer; Wiederholungen zählen nicht gegen den Wörter-Deckel. *Rot, wenn* Boden oder Deckel nicht angewandt werden oder
   Wiederholungen den Wörter-Deckel verbrauchen.
3. Fällige Wiederholungen kommen zuerst; Gescheitertes von gestern kommt wieder, eine Stufe leichter. *Rot, wenn* die
   Reihenfolge der Regeln vertauscht oder die Stufe gleich bleibt.
4. Jede Aufgabe trägt eine Begründung aus den Eingangsdaten; bei verschiedenen Daten lautet sie verschieden. *Rot, wenn* der Text
   konstant ist.
5. Dieselben Daten und dieselbe Uhr ergeben denselben Plan; der Dienst hängt weder von einem Chat-Client noch von Zufall ab.
   *Rot, wenn* der Konstruktor eine solche Abhängigkeit erhält oder zwei Läufe mit `FakeTimeProvider` verschiedene Pläne liefern.
6. Ein zweiter Abruf am selben Tag liefert dieselbe Zusammenstellung, auch wenn sich der Lernstand dazwischen ändert. *Rot, wenn*
   der Plan beim Lesen neu gerechnet wird.
7. Im Schattenmodus bucht ein Tagesplan keine Münzen, kein Malus entsteht, und er erscheint in keinem Student-Endpunkt und nicht in
   der Sohn-App. *Rot, wenn* ein Schattenplan Münzen bucht oder `student/children/{childId}/today` ihn liefert.
8. Ein Tag, an dem nie ein Plan entstand (Server aus), kostet keinen Malus, lässt die Serie stehen und steht beim Vater als
   Meldung. *Rot, wenn* Malus gebucht wird oder die Meldung fehlt.
9. Der Planer nimmt keine Menge aus einem Request an und hebt eine eingetragene Pause nie auf (B-681 Akzeptanzkriterium 5).
   *Rot, wenn* ein Endpunkt die Menge übernimmt oder der Planer an einem Pausentag einen Plan erzeugt.
10. `GET supervisor/children/{childId}/daily-plans` (Zeitraum, seitenweise) liefert Pläne mit Aufgaben, Begründung und Modus; ein
    Konto nur mit Creator-Profil bekommt `403`, ein fremder Vater `404`. *Rot, wenn* ein Gate fehlt.
11. Das Vater-Web zeigt je Kind und Tag den Tagesplan mit Begründung je Aufgabe und die Meldung zu fehlenden Plänen. *Rot, wenn* der
    Plan ohne Begründung erscheint oder ein Schattenplan als wirksam beschriftet ist.

## Schätzung

**Größe: L** — Anker: eine DB-Umbau-Etappe wie E6, am oberen Rand: drei neue Tabellen mit Migration, der erste Zeitgeber des
Backends, ein Regeldienst, ein Endpunkt, eine Vater-Ansicht. Reißt der Bau die L-Marke, wird das Vater-Web als eigene Story
abgespalten (neue Id nach Nachzählen, `quelle: B-734`).

- **`migration: ja`** — `DailyPlan` (Kind, Tag eindeutig, Erzeugt-Zeit, Warnungen als JSON, Modus beim Erzeugen), `DailyTask` (Plan,
  Position, Übung, Fach, Art Pflicht oder Bonus, Wortauswahl als JSON, Ziel, Belohnung, Malus-Momentaufnahme, Begründung,
  Herkunft Planer oder Vater, Streichzeit), `PlannerSettings` (eine Zeile). JSON-Spalten brauchen `ValueComparer` (Tor G7). An
  die Kette hängen; die Tabellen starten leer.
- **`vertragsbruch: nein`** — nur neue DTOs und ein Endpunkt; kein bestehender Vertragstyp ändert sich.
- **`wo: beides`** — Backend zuerst (Dienst, Zeitgeber, Endpunkt), dann Vater-Web.

### Risiken

- **Parallele Auslösung:** Zeitgeber und erster Zugriff laufen gleichzeitig; der eindeutige Index ist die Sicherung, der Verlierer
  liest den Gewinner (dieselbe Klasse wie B-97/B-104, Unique-Index-Verlierer).
- **Hintergrunddienst im Test-Host:** muss aus sein, sonst läuft er in jedem Integrationstest mit.
- **Zeitzone:** Ohne [B-730](B-730-tagesbegriff-in-deutscher-zeit.md) ist „6 Uhr" nicht definiert.
- **Plan B beim Heimserver:** Er läuft nicht durch; Tage ohne Plan müssen beim nächsten Start erkannt werden
  (Meldung), nicht verschwinden.
- **Regeln finden nur, wonach sie suchen** (B-644, Entscheidung 10, Kosten): Die Schattenwoche ist die Messung.
- **Fremde Sohn-Dateien:** `frontend/src/sohn/*` trägt ungecommittete LearningDad-Arbeit; diese Story fasst die Sohn-App nicht an.

### Angriffsplan

1. Modell, Migration und Migrationstest (`DailyPlan`, `DailyTask`, `PlannerSettings`).
2. Regeldienst als reine Funktion (Eingang: Positionen, fällige Wörter, Profil, Leitplanken, Tag) und seine Tests ohne
   Datenbank.
3. `EnsureDailyPlanAsync(childId, day)` (idempotent, Index-Verlierer liest den Gewinner), Aufruf im Tagessicht-Controller und
   beim Kind-Login.
4. Hintergrunddienst mit Schalter; Test-Host aus.
5. Endpunkt `daily-plans`, Meldung für Tage ohne Plan.
6. Vater-Web: Tagesplan-Ansicht je Kind.

### Testweg

- Neue Klasse `TagesplanerTests` (Integration mit `TestClock`): Idempotenz, parallele Auslösung, Boden und Deckel,
  Reihenfolge der Regeln, eingefrorener Plan, Schatten ohne Buchung. Rote Probe je AK: Index entfernen lässt AK 1 fallen, den
  Deckel entfernen AK 2, den Neuaufbau beim Lesen AK 6.
- `TagesplanerRegelnTests` (Unit, ohne Datenbank): Determinismus mit `FakeTimeProvider`, Konstruktor ohne Chat-Client.
- `TagesplanMeldungTests` (Tag ohne Plan, Pause).
- `ConventionGuardTests`: Student-Aktionen geben keinen `DailyPlan`-Typ zurück (nur Student-Lesepfad `…/today` bleibt unverändert,
  Rot-Probe: Schattenaufgabe in `ChildDayRollup` mischen).
- Migration: `TagesplanerMigrationTests` nach `ActivatedAtMigrationTests`; `ProductionStartupTests` für den Dienst.
- Frontend: Vitest und E2E neu `frontend/e2e/tagesplan-schatten.spec.ts` (Vater sieht den Plan, Sohn-Tag bleibt unverändert).
- Vor der Abnahme: `pugling-reviewer`, `frontend-reviewer`, Rollengang als Vater **und** Sohn (Sohn darf nichts sehen).

## Reihenfolge und Abhängigkeiten

Nach [B-733](B-733-tagesauswertung-und-leistungsprofil.md) (Profil) und [B-731](B-731-lerntage-pause-und-leitplanken-je-kind.md)
(Leitplanken, Pause). Danach läuft die **Schattenwoche**. [B-735](B-735-stoffrahmen-ohne-termin.md) und
[B-680](B-680-taeglicher-lehrplan-je-fach-just-in-time.md) liefern Eingänge, die der Planer in der Schattenwoche mitrechnen sollte
(siehe dort); [B-736](B-736-pflicht-aus-der-tagesaufgabe.md) schaltet scharf.

## Verlauf

- **2026-11-26** — Der Nutzer bestätigt die stellvertretenden Entscheidungen 19, 21 und 24 aus B-644 unverändert (PM-Sitzung 2026-11-26). Damit ist B-734 baubar.

- **2026-11-20** — angelegt durch Teilung von B-644 (Etappe E2) und `gegrillt` → `geschaetzt` im Vorbereitungslauf (Sonnet):
  Eintrittsbedingung `gegrillt` aus B-644 und B-680 geerbt. Nachgesehen: kein Zeitgeber, keine Tagesaufgabe im Modell; Modus als
  Tabellenzeile statt Konfiguration (Schätz-Annahme). L am oberen Rand, beides, Migration, kein Vertragsbruch.
- **2026-11-20** — Reihenfolge entschieden (siehe B-680, stellvertretend auf Anweisung des Nutzers): Der Schattenmodus wird erst eingeschaltet, wenn B-735 und die Regeln aus B-680 gebaut sind. B-734 liefert den Kern und den abgeschalteten Modus.
