---
tags: [typ/story, status/geschaetzt, bereich/training, bereich/belohnung, rolle/student, rolle/supervisor]
aliases: [Hausaufgabe Backend, Hausaufgaben-Kern]
status: geschaetzt
prio: P2
art: Wunsch
groesse: L
wo: backend
migration: ja
vertragsbruch: nein
quelle: "B-687"
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-760 · Hausaufgaben-Kern: Ressource am Kind, Abgabe, Abnahme, Münzen und Malus beim Login (Backend)

Der Backend-Teil von [B-687](B-687-hausaufgabe-mit-foto-nachweis.md): die Hausaufgabe als eigene Ressource am Kind mit Frist, Abgabe,
Abnahme durch den Vater, Münzen und Malus. **Ohne Foto-Datei:** eine Abgabe ist hier ein Datensatz mit Zeitstempel; die Datei
hängt B-761 an. Baubar vor dem Ablageweg, weil die Datei nur eine nullbare Spalte später ist.

## User Story

Als **Supervisor** möchte ich für mein Kind eine Hausaufgabe mit Frist anlegen und eine Abgabe annehmen oder zurückgeben, damit
ich die Schulpflicht wie die übrigen Pflichten belohne und bestrafe. Als **Student** möchte ich meine Hausaufgaben sehen und
abgeben, damit ich Münzen bekomme und keinen Malus kassiere.

## Ist-Stand am Code

Pfade relativ zu `backend/Pugling.Api/`; gemessen am 2026-11-20.

- Hausaufgabe als Fachbegriff: **nicht gefunden** (siehe B-687, Ist-Stand).
- Muster der Abrechnung: `PositionProgressService.SettleClosedPeriodsAsync` (`Services/Shared/PositionProgressService.cs:754`), Rückblick
  höchstens 14 Tage über die **private** Konstante `MaxSettleLookbackDays` (`:25`), Ledger-Eintrag `PointKind.GoalPenalty` und
  `child.ConcurrencyStamp = Guid.NewGuid()` vor dem Speichern (`:827`), Idempotenz über einen eindeutigen Schlüssel
  (`PositionGoalPenalty`, `Models/PlanPositionEntities.cs:223`, Schlüssel verlangt `PlanPositionId`).
- Auslöser ist der Kind-Login (`Controllers/AuthController.cs:85`) **und** der Kauf **vor** der Kontoprüfung
  (`Controllers/Student/MeController.cs:296`, Kommentar: ein Kind soll der Strafe nicht durch schnelles Kaufen entkommen).
- Kontoart: `PointKindCurrency.Of` (`Services/Shared/PointKindCurrency.cs:21`) wirft für eine Art ohne Zuordnung; `PointKind` endet bei
  `RemediationBonus = 17` (`Pugling.Contracts/Common/AdminBaseTypes.cs:79`). Verdient-Summe: `EarnedCoinKinds` nimmt alle Münzarten
  außer drei (`Services/Shared/WalletService.cs:47-48`), eine neue Münzart zählt also von selbst.
- Einstellung je Kind mit „leer = Vorgabe aus der Konfiguration“: `Child.BonusDailyCoinCap` (`Models/AdminEntities.cs:96`).
- Eigentum: `ChildOwnershipFilter` am Supervisor-Controller (`Controllers/Supervisor/ChildrenController.cs:20-21`); Student-Wege lesen
  `User.ChildId()` (`MeController.cs`).

## Die echte Lücke

Eine neue Ressource, drei Routen-Gruppen und ein zweiter, kleiner Abrechnungspfad, der das Muster der Pflicht übernimmt, ohne ihre Tabelle.

## Entscheidungen

*Entscheidungen stammen aus [B-687](B-687-hausaufgabe-mit-foto-nachweis.md): stellvertretend entschieden im Vorbereitungslauf 2026-11-20 (Sonnet), unbestätigt. Die Nummern der Entscheidungen verweisen dorthin.*

1. **Eigene Ressource am Kind** (B-687 Entscheidung 1): Entität `Homework` mit Fach, Text, Frist als Tag, Anlegezeit, Zustand und
   Nacharbeits-Notiz; Routen `supervisor/children/{id}/homework` und `student/homework`. Wirkt nicht auf Serie, Tageskiste, Missionen und die
   Sperre des Bonus-Trainings. *Kosten:* Entität, Migration, zweiter Abrechnungspfad. *Schließt aus:* „Hausaufgabe macht den Tag vollständig“.
2. **Erledigt für die Frist heißt abgegeben; Malus nur ohne jede Abgabe; die Annahme bringt die Münzen** (B-687 Entscheidung 2). Ein Foto
   nach dem Malus nimmt ihn nicht zurück; das geht nur über die `Manual`-Gutschrift des Vaters.
3. **Zwei feste Beträge je Kind** (B-687 Entscheidung 3, 8): `Child.HomeworkCoins` und `Child.HomeworkPenaltyCoins`, beide nullbar, leer =
   Konfigurationsvorgabe; Gutschrift als neue Art `PointKind.HomeworkCoins` (Münzen), Malus als `PointKind.GoalPenalty`. Der Malus gilt nur für
   eine Hausaufgabe, die **angelegt wurde, bevor ihre Frist verstrich**, höchstens 14 Tage zurück.
4. **Teilweise erledigt: kein Teilpunkte-System** (B-687 Entscheidung 6): „Nacharbeit“ mit Notiz, die Frist verschiebt sich nicht.
5. **Beide Söhne, von Hand angelegt** (B-687 Entscheidung 7); WebUntis ist eine spätere Quelle derselben Ressource (B-654).

*Verschoben (nicht in diesem Teil):* die Datei und ihre Auslieferung (B-761), alle Oberflächen (B-762, B-763).

## Akzeptanzkriterien

Je Kriterium die Änderung, die es rot macht. Nummerierung folgt B-687; AK 1 und 3 liegen bei B-761, die Oberflächen-Kriterien bei B-762 und B-763.

1. (B-687 AK 2) Ein Student kann nur für **seine** Hausaufgaben abgeben, nie für ein anderes Kind. *Rot, wenn die Eigentumsprüfung fehlt.*
2. (B-687 AK 4) Eine Hausaufgabe **ohne Abgabe** zur Frist kostet beim nächsten Login **genau einmal** den Malus (`PointKind.GoalPenalty`, Betrag des
   Kindes); mit mindestens einer Abgabe kostet sie nichts, auch nicht bei Nachlade oder „Nacharbeit“. *Rot, wenn zweimaliges Abrechnen doppelt bucht,
   `ConcurrencyStamp` unbewegt bleibt oder „Nacharbeit“ einen Malus auslöst.*
3. (B-687 AK 5) Eine nach Ablauf der Frist **angelegte** Hausaufgabe und eine, deren Frist über 14 Tage zurückliegt, kosten nichts. *Rot, wenn
   die Abrechnung `CreatedAt` oder das Zeitfenster nicht liest.*
4. (B-687 AK 6) Eine Abnahme schreibt einmal Münzen gut (`PointKind.HomeworkCoins`); ein zweites „Annehmen“ nicht. *Rot, wenn der Endpunkt nicht
   idempotent ist oder die Münzen aus `WeekCoinsAsync` fallen.*
5. (B-687 AK 7) „Nacharbeit“ mit Begründung ist an der Hausaufgabe lesbar und verschiebt die Frist nicht. *Rot, wenn die Notiz fehlt oder die Frist sich
   verschiebt.*
6. (B-687 AK 8) Eine offene Hausaufgabe ändert Serie, Tageskiste, Missionen und die Sperre des Bonus-Trainings nicht. *Rot, wenn `GateAsync` sie sperrt
   oder `DutyStateAsync` sie als Pflicht liest.*
7. (B-687 AK 9) Die Liste ist je Kind gefiltert. *Rot, wenn die Abfrage nicht nach `ChildId` filtert (Test mit zwei Kindern).*
8. (B-687 AK 10) Nach der Migration sind die zwei Beträge am Kind leer, und an einer Kopie der echten DB ändert sich kein Kontostand. *Rot, wenn die
   Migration etwas vorbelegt.*
9. (B-687 AK 11, **Absichtserklärung**) Ein nachgereichtes Foto nimmt einen gebuchten Malus nicht zurück. Der Ausgangszustand erfüllt das schon; rot
   würde nur, wer eine automatische Rücknahme einbaut.
10. (**neu, aus der Konvention des Kaufs**) Die Abrechnung läuft auch **vor** der Kontoprüfung eines Kaufs. *Rot, wenn sie nur im Login steht: ein Kind
    kauft dann vor dem Malus und entkommt ihm (`MeController.cs:293-296` ist das Vorbild).*

## Schätzung

**Größe: L** – wie eine DB-Umbau-Etappe (E6): neue Entität mit Eigentum, Geld auf beiden Seiten, zwei Auslöser, eine Migration. Nicht M, weil Geld
bewegt wird und der Malus lazy mit einer 14-Tage-Grenze abrechnet.

Nachgesehen am 2026-11-20:

- **`wo: backend`.** Eine Zeile Oberfläche hängt dran: `PointKind` wächst, und `frontend/src/lib/labels.ts` (`Record` über die Arten, Zeile 14-22
  beschriftet sie) bekommt einen Eintrag samt Übersetzung, sonst bricht `tsc` nach dem Neuerzeugen des Vertrags. Das gehört zu diesem Teil.
- **`migration: ja`.** Neue Tabellen (`Homework`, `HomeworkSubmission`, ein Abrechnungsschlüssel je Hausaufgabe) und zwei nullbare Spalten an `Child`;
  FKs auf `Child` mit `Cascade`; Einträge in `SchemaGuardTests` (FK-Verhalten, Spaltenlängen, Enums als Strings).
- **`vertragsbruch: nein`.** Alles ist neu und additiv; der neue Enum-Wert `HomeworkCoins` bricht nichts am Server, das Frontend zieht über `labels.ts` nach.
- **Wallet-Invariante geprüft:** (1) der **Malus** bucht ab und bumpt `child.ConcurrencyStamp` vor dem Speichern (AK 2); (2) die **Gutschrift** der
  Annahme bucht nur zu und braucht den Bump nicht, wird aber wie in B-627 Entscheidung 6 **ohne Ausnahme** mitgebumpt; (3) der vorhandene Weg
  `ChildrenController.AddPoints` (`Controllers/Supervisor/ChildrenController.cs:279-290`) lässt auch **negative** Beträge zu und bumpt nicht – siehe Risiko 5.

### Risiken

1. **Idempotenz des Malus.** Der eindeutige Schlüssel ist die Hausaufgaben-Id; ein paralleler Login darf nicht doppelt buchen. Wie bei der Pflicht den
   `DbUpdateException` fangen (`PositionProgressService.cs:827-838`).
2. **Rückblick teilen.** `MaxSettleLookbackDays` ist privat; entweder öffentlich machen oder in eine gemeinsame Stelle ziehen, nicht kopieren.
3. **Zwei Auslöser.** Login und Kauf; fehlt einer, gibt es ein Schlupfloch (AK 10).
4. **Zeitzone.** Die Frist ist ein Tag in UTC; „angelegt, bevor sie verstrich“ vergleicht `CreatedAt` mit dem Ende dieses Tages. Nahe Mitternacht lokal liegt der Tag anders (CLAUDE.md, Zeit/UTC).
5. **Nebenfund außerhalb des Zuschnitts:** `AddPoints` bucht negative Beträge ohne `ConcurrencyStamp`-Bump; das ist ein abbuchender Weg, der gegen die
   Wallet-Invariante verstößt. Eigene Story, nicht hier mitgenommen (das Ziel dieser Story ist ohne sie erfüllt).
6. **Entscheidung 3 braucht Startwerte** (B-687: „aus den echten Positions-Beträgen der Kinder abgeleitet“); sie stehen in der Konfiguration, nicht in der Migration.

### Angriffsplan

1. Modell, Migration (an die Kette), Tor-Einträge, `PointKind.HomeworkCoins` samt `PointKindCurrency`, `labels.ts`.
2. `HomeworkService`: anlegen, abgeben, annehmen, nacharbeiten, abrechnen; Wallet-Bump an jedem buchenden Weg.
3. Controller `supervisor/children/{id}/homework` (Eigentum über `ChildOwnershipFilter`) und `student/homework` (Eigentum über `User.ChildId()`).
4. Aufruf der Abrechnung im Login und vor dem Kauf.
5. Contract-Typen, Client-Methoden; `dotnet build Pugling.sln`, `dotnet format Pugling.sln`; `DocsCaptureTests` neu erzeugen lassen.

### Testweg

- Neue Klasse `HausaufgabenTests` (Vorbild `PflichtMalusTests`, dort `GerissenePflicht_ZiehtMuenzMalusAb_ErlaubtSchuld_UndIstIdempotent`): AK 1 bis 7 und 10.
  Rote Proben stehen in den Kriterien.
- `HausaufgabenMigrationTests` (neu, nach `ActivatedAtMigrationTests`): AK 8.
- `PointKindCurrencyTests` und `SchemaGuardTests` laufen mit; die erste hält die Zuordnung, die zweite FK, Länge und Enum-Form.
- `OwnershipMatrixTests` bekommt die neuen Routen (der `EndpointCoverageGuard` verlangt sie ohnehin).
- Vor der Abnahme: `pugling-reviewer`; `/smoke-test` für den Weg anlegen, abgeben, Login nach der Frist.

## Verlauf

- 2026-11-20 · aus B-687 geteilt und geschaetzt (Vorbereitungslauf, Sonnet): L, backend, Migration ja, kein Vertragsbruch. Baubar ohne den Ablageweg der Fotos. Reihenfolge: B-760, dann B-761, dann B-762 und B-763.
