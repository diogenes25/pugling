---
tags: [typ/story, status/abgenommen, bereich/training, bereich/belohnung, rolle/student, rolle/supervisor]
aliases: [Hausaufgabe Backend, Hausaufgaben-Kern]
status: abgenommen
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
- **2026-11-24** — in Arbeit (Nachtlauf J, Sprint 141, Worktree `nachtlauf-j`). Gebaut nach Freigabe 8 auf den Entscheidungen 1–5.
  Am Code vorher geprüft: `MaxSettleLookbackDays` privat (jetzt `internal`, geteilt), `PointKind` endet bei `TowerGameCoins = 18`
  (neu `HomeworkCoins = 19`), Nähte Login/Kauf wie beschrieben. Migration `20261124201850_Homework` (angehängt: zwei nullbare
  Spalten am Kind, drei Tabellen, kein `UPDATE`, kein Default). Startwerte `Gamification:Homework` 20/2 (= Beträge einer
  Vokabel-Tagespflicht der echten Pläne; Risiko 6). Zusätzlich gebaut, weil die Entscheidungen es tragen und ein Vater sonst
  nicht korrigieren kann: `DELETE` einer Hausaufgabe, `take` an beiden Listen (Tor „unpaginierte Array-GETs“).
- Entscheidungs-Auslegungen ohne neue Produktfrage (im Protokoll benannt): Annahme nur aus `Submitted` (Review-Fund 1);
  „zur Frist“ = Abgabe bis Ende des Fälligkeitstags im Lerntag; eine Abgabe nach der Frist ist erlaubt, wendet aber nichts ab;
  Betrag 0 bucht nichts und schreibt keinen Schlüssel (wie die Pflicht mit `PenaltyCoins = 0`).
- AK 1, fremde Hausaufgabe abgeben → `HausaufgabenTests.Kind_Sieht_Und_Abgibt_Nur_Eigene_Hausaufgaben` (404, keine Zeile;
  Vater-Token 403), M1 → rot.
- AK 2, Malus einmal, Betrag des Kindes, Stamp → `Ohne_Abgabe_Kostet_Den_Malus_Des_Kindes_Genau_Einmal` (−7 statt Vorgabe −2),
  M3 (Stamp) / M4 (Betrag) / M19b (Login-Naht) → rot; Abgabe am Fälligkeitstag 23:30 und Nacharbeit kosten nichts →
  `Abgabe_Bis_Zur_Frist_Und_Nacharbeit_Kosten_Nichts`, M6 → rot; Abgabe 00:30 am Folgetag wendet nichts ab →
  `Spaete_Abgabe_Wendet_Den_Malus_Nicht_Ab`, M5 → rot. Doppelbuchung: der eindeutige Schlüssel ist die Garantie; ein Mutant
  auf die Existenzprüfung bliebe grün (der Index fängt ihn), darum keine Zeile dafür.
- AK 3, Nachtrag nach der Frist / Frist −15 / Grenze −14 → `Nachtrag_Und_Alte_Frist_Kosten_Nichts` (genau die Grenze wird
  belastet), M7 (Anlegezeit) / M8 (Fenster zu weit) / M9 (Grenze ausgeschlossen) → rot.
- AK 4, Annahme einmal, Betrag des Kindes (25 statt 20), zählt als verdient → `Annahme_Bucht_Einmal_Und_Zaehlt_Als_Verdient`,
  M10 (zweite Annahme) / M11 (Betrag) / M12 (Stamp) / M13 (ohne Abgabe) / M14 (aus `EarnedCoinKinds` gefallen) → rot.
- AK 5, Notiz lesbar, Frist bleibt, leere Notiz 400, Annahme nach Rückgabe ohne neue Abgabe 409 →
  `Nacharbeit_Ist_Lesbar_Und_Verschiebt_Die_Frist_Nicht`, M13b / M15 / M16 / M17 → rot.
- AK 6 → `Offene_Hausaufgabe_Aendert_Den_Tag_Nicht`, **Absichtserklärung** (kein Mutant: nichts liest Hausaufgaben im Tag).
- AK 7, Liste je Kind → M2 → rot (zwei Kinder im Fall); `take` und Löschen → `Vater_Begrenzt_Die_Liste_Und_Loescht`, M20 / M21 → rot.
- AK 8 → `HausaufgabenMigrationTests.Bestand_Hat_Leere_Betraege_Und_Unveraenderten_Kontostand`, M23 (Default 20) → rot.
- AK 9, **Absichtserklärung** (wie in der Story beschriftet): `Spaete_Abgabe_…` zeigt, dass eine spätere Abgabe nichts zurücknimmt.
- AK 10, Kauf rechnet vor der Kontoprüfung ab → `Kauf_Rechnet_Den_Malus_Vor_Der_Kontopruefung_Ab` (10 Münzen, Preis 10,
  Malus 5 → `insufficient_coins`), M18b → rot.
- Anlegen ohne `dueOn` → 400 (Review-Fund 2) → `Anlegen_Prueft_Text_Und_Fach`, M22b → rot.
- Mutanten-Probe nach dem letzten Edit (Debug, `python -I .claude/scripts/mutant-probe.py`), 24 Zeilen; die drei
  `ungemessen` (M18/M19/M22: Mutant kompilierte nicht) neu gemessen als M18b/M19b/M22b:

  Grundlinie: dotnet test backend/Pugling.Api.Tests -c Debug --no-build --filter FullyQualifiedName~Hausaufgaben
  Gemessen an `5993c86b + Arbeitsbaum geändert`, 2026-11-24 23:01.

  | Mutante | Stelle | erwartet | gemessen | Anmerkung |
  |---|---|---|---|---|
  | M1 | backend/Pugling.Api/Services/Shared/HomeworkService.cs:70 | red | red |  |
  | M2 | backend/Pugling.Api/Services/Shared/HomeworkService.cs:45 | red | red |  |
  | M3 | backend/Pugling.Api/Services/Shared/HomeworkService.cs:186 | red | red |  |
  | M4 | backend/Pugling.Api/Services/Shared/HomeworkService.cs:169 | red | red |  |
  | M5 | backend/Pugling.Api/Services/Shared/HomeworkService.cs:165 | red | red |  |
  | M6 | backend/Pugling.Api/Services/Shared/HomeworkService.cs:165 | red | red |  |
  | M7 | backend/Pugling.Api/Services/Shared/HomeworkService.cs:164 | red | red |  |
  | M8 | backend/Pugling.Api/Services/Shared/HomeworkService.cs:159 | red | red |  |
  | M9 | backend/Pugling.Api/Services/Shared/HomeworkService.cs:159 | red | red |  |
  | M10 | backend/Pugling.Api/Services/Shared/HomeworkService.cs:91 | red | red |  |
  | M11 | backend/Pugling.Api/Services/Shared/HomeworkService.cs:96 | red | red |  |
  | M12 | backend/Pugling.Api/Services/Shared/HomeworkService.cs:111 | red | red |  |
  | M13 | backend/Pugling.Api/Services/Shared/HomeworkService.cs:92 | red | red |  |
  | M13b | backend/Pugling.Api/Services/Shared/HomeworkService.cs:92 | red | red |  |
  | M14 | backend/Pugling.Api/Services/Shared/WalletService.cs:50 | red | red |  |
  | M15 | backend/Pugling.Api/Services/Shared/HomeworkService.cs:137 | red | red |  |
  | M16 | backend/Pugling.Api/Services/Shared/HomeworkService.cs:136 | red | red |  |
  | M17 | backend/Pugling.Api/Controllers/Supervisor/HomeworkController.cs:74 | red | red |  |
  | M18 | backend/Pugling.Api/Controllers/Student/MeController.cs:299 | red | ungemessen | kompiliert nicht |
  | M19 | backend/Pugling.Api/Controllers/AuthController.cs:91 | red | ungemessen | kompiliert nicht |
  | M20 | backend/Pugling.Api/Services/Shared/HomeworkService.cs:145 | red | red |  |
  | M21 | backend/Pugling.Api/Services/Shared/HomeworkService.cs:45 | red | red |  |
  | M22 | backend/Pugling.Api/Controllers/Supervisor/HomeworkController.cs:47 | red | ungemessen | kompiliert nicht |
  | M23 | backend/Pugling.Api/Data/Migrations/20261124201850_Homework.cs:18 | red | red |  |

  Grundlinie nach dem Zurückspielen: green.

  Gemessen an `5993c86b + Arbeitsbaum geändert`, 2026-11-24 23:05.

  | Mutante | Stelle | erwartet | gemessen | Anmerkung |
  |---|---|---|---|---|
  | M18b | backend/Pugling.Api/Controllers/Student/MeController.cs:299 | red | red |  |
  | M19b | backend/Pugling.Api/Controllers/AuthController.cs:91 | red | red |  |
  | M22b | backend/Pugling.Api/Controllers/Supervisor/HomeworkController.cs:47 | red | red |  |

  Grundlinie nach dem Zurückspielen: green.
- **2026-11-24** — Review `pugling-reviewer`: 🟡 Annahme aus `Rework` + gleichzeitige Abgabe → zweite Buchung möglich; behoben
  (Annahme nur aus `Submitted`, M13b). 🟢 behoben: `dueOn` Pflicht (M22b), gelöschtes Kind → leere Liste statt 500. 🟢
  zurückgestellt: Betrag 0 schiebt auf statt zu erlassen (Frage an den Nutzer im Protokoll), Obergrenze der Abgaben (→ B-761).
  Verifikation: .NET 2357/2357 Release, Vitest 1441/1441, `tsc` sauber. Fehlerzähler Sprint 141: 4.
- **2026-11-24** — Rollengang an der API einer Kopie der echten DB (`:5212`, Migration lief dort ein): Vater legt an, gibt
  zurück, nimmt an (Testjunge 2640 → 2660), zweites Annehmen 409; Sohn sieht, gibt ab, liest die Notiz; fremdes Kind 404;
  Kontostände nach der Migration unverändert (275 / 2138 / 2640). Der Malus selbst lief nicht an der Kopie (braucht einen
  abgelaufenen Tag) – belegt durch die Integrationstests. Kein Browser: es gibt keine Oberfläche.
- **2026-11-24** — abgenommen, Commit `788921d6` (Branch `nachtlauf/2026-11-24-j`, ungemergt).
