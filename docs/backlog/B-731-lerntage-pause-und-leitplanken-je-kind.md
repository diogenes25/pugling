---
tags: [typ/story, status/geschaetzt, bereich/lehrplan, bereich/training, rolle/supervisor, rolle/student]
aliases: [Lerntage, Sonntag frei, Pause je Kind, Leitplanken, Boden und Deckel, Etappe E0b]
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

# B-731 · Lerntage Montag bis Samstag, Pause je Kind und die Leitplanken des Vaters (B-644, Etappe E0b)

Zweite Teilstory aus der Teilung von [B-644](B-644-tagesplaner-statt-fester-lauf.md). Serie und Malus kennen heute
keinen freien Tag: Ein Sonntag ohne Lernen reißt die Serie und kostet Malus. Diese Story führt den **Lerntag** ein
(Montag bis Samstag, nicht in einer Pause), die **Pause von Hand** und die **Einstellungen je Kind** (Boden und Deckel
für Pflichtaufgaben und neue Wörter, Malus-Betrag). Die Zahlen wirken erst mit dem Planer
([B-734](B-734-planer-kern-und-schattenmodus.md), [B-736](B-736-pflicht-aus-der-tagesaufgabe.md)); Sonntag und Pause
wirken sofort.

**Stand:** Eintrittsbedingung `gegrillt` aus B-644 geerbt. Die Entscheidungen 2 bis 5 und 8 stammen vom Vater, 22 ist
stellvertretend und unbestätigt.

## User Story

Als *Vater* möchte ich je Kind eine Pause von Datum bis Datum eintragen und die Leitplanken des Planers (Boden und
Deckel für Aufgaben und neue Wörter, Malus-Betrag) einmal setzen, damit Krankheit und Sonntag weder Serie noch Münzen
kosten und der Planer eine menschliche Grenze hat. Als *Sohn* möchte ich, dass ein freier Sonntag meine Serie nicht
bricht.

## Ist-Stand am Code

Gemessen am 2026-11-20 auf `main`. Pfade relativ zu `backend/Pugling.Api/`.

- **Serie:** `ChildStreakAsync` (`Services/Shared/PositionProgressService.cs:479`) läuft Kalendertag für Kalendertag
  rückwärts (Schleife `:490`) und bricht am ersten Tag, für den `IsCompleteDay` (`:439`) falsch ist; ein Tag ohne Plan
  liefert eine leere Zustandsliste und bricht ebenfalls. Einen Wochentag fragt sie nicht ab.
- **Malus:** `ClosedPeriods` (`:686`) zerlegt nur nach Taktung (Tage einzeln, Wochen Montag bis Sonntag);
  `SettleClosedPeriodsAsync` (`:754`) läuft träge beim Kind-Login (`Controllers/AuthController.cs:85`) und in
  `Controllers/Student/MeController.cs`, höchstens `MaxSettleLookbackDays = 14` zurück (`:25`).
- **`DayOfWeek`** benutzt das Backend nur im Shop-Auffülltag (`ShopController.cs`) und im Stundenplan
  (`TimetableController.cs:25`). Eine Pause, Ferien oder Krank existiert in Modell, Diensten und Vertrag **nicht**.
- **Taktung** kennt außer `None`, `Daily`, `Weekly` auch `UntilDone` (`PositionProgressService.cs:85`, `IsDailyDuty`);
  B-644 nennt `UntilDone` nicht.
- **Einstellungen je Kind:** `Child` trägt als einzige Lern-Stellgröße `BonusDailyCoinCap` (`Models/AdminEntities.cs:96`);
  der Vertrag trägt sie in `ChildResponse`, `CreateChildDto` und `UpdateChildDto` (`Pugling.Contracts/Supervisor/ChildDtos.cs:15`,
  `:23`, `:36`, dort mit `ClearBonusDailyCoinCap`). Boden, Deckel, Malus-Betrag je Kind: nicht gefunden.
- **Vater-Web:** Betreuungs-Panels je Kind liegen in `frontend/src/vater/ChildCarePanels.tsx`, die Maske
  `VaterKind.tsx`.

## Entscheidungen (geerbt)

1. **Jeden Tag von Montag bis Samstag eine Pflicht, der Sonntag ist frei; die Serie läuft über diese sechs Tage**
   (B-644 Entscheidung 2, Vater). Der Sonntag zählt weder für noch gegen die Serie.
2. **Zwei Zahlenpaare je Kind: Pflichtaufgaben am Tag und neue Wörter am Tag, je mit Boden und Deckel; der Boden ist nie
   null** (Entscheidungen 3 und 4, Vater). Feste Mengen, keine Minuten.
3. **Malus je verpasster Pflichtaufgabe mit einem Betrag je Kind** (Entscheidung 5, Vater).
4. **Pause von Hand je Kind von Datum bis Datum, auch rückwirkend für heute; keine Schulferien-Automatik**
   (Entscheidung 8, Vater). In der Pause kein Plan, kein Malus, die Serie steht.
5. **Der Vater setzt Leitplanken und hat eine Notbremse, er legt keine Tagesmengen fest** (Entscheidung 22,
   stellvertretend, unbestätigt; Rahmen B). Die fünf Zahlen und die Pause sind seine Stellgrößen.

### Schätz-Annahmen (stellvertretend, unbestätigt)

1. **Die fünf Zahlen sind nullable Spalten an `Child`**, nicht eine eigene Tabelle; „nicht gesetzt" heißt „Planer
   nicht eingerichtet". Begründung: ein Datensatz je Kind, wie `BonusDailyCoinCap`. Kosten: fünf Spalten am breiten
   `Child`. Verworfen: Tabelle `ChildPlannerSettings`.
2. **Die Pause ist eine eigene Tabelle `ChildPause`** (Kind, Von, Bis, Notiz, Anlagezeit). Begründung: mehrere Pausen je
   Kind, Verlauf bleibt lesbar. Verworfen: ein einziges Von/Bis-Paar am Kind (verliert die Historie, die Serie und
   Auswertung brauchen).
3. **Ein bereits gebuchter Malus wird durch eine rückwirkende Pause nicht erstattet** (B-644 Entscheidung 8, Kosten: der
   Vater gibt ihn von Hand zurück, `POST children/{id}/points`).

## Akzeptanzkriterien

1. Ein Sonntag ohne Lernen bricht die Serie nicht: Samstag erfüllt, Sonntag leer, Montag erfüllt ergibt Serie 2.
   *Rot, wenn* `ChildStreakAsync` am Sonntag abbricht oder den Sonntag mitzählt.
2. Eine Tagespflicht, die am Sonntag offen bleibt, kostet keinen Malus; am Samstag offen kostet sie. *Rot, wenn*
   `SettleClosedPeriodsAsync` für den Sonntag bucht.
3. Eine Wochenpflicht (Montag bis Sonntag) wird unverändert abgerechnet, genau einmal nach Sonntag. *Rot, wenn* die
   Lerntag-Regel die Wochenabrechnung entfallen lässt oder verdoppelt.
4. Tage in einer Pause kosten keinen Malus, zählen nicht für die Serie und brechen sie nicht, auch wenn die Pause für
   heute rückwirkend eingetragen wird. *Rot, wenn* in einer Pause Malus gebucht wird oder die Serie bricht.
5. `GET/POST/DELETE supervisor/children/{childId}/pauses` verlangt Supervisor-Claim und Eigentum: ein Konto nur mit
   Creator-Profil bekommt `403`, ein fremder Vater `404`; `Von` nach `Bis` ist `400` mit `code: validation_error`.
   *Rot, wenn* eines der Gates oder die Prüfung fehlt (B-681 Akzeptanzkriterium 4).
6. `UpdateChildDto` nimmt Boden, Deckel (Aufgaben und neue Wörter) und Malus-Betrag an und lehnt Boden 0, Boden über
   Deckel und einen negativen Malus mit `400` ab. *Rot, wenn* ein solcher Wert gespeichert wird.
7. Die Migration lässt alle Bestandszeilen von `Child` unverändert (neue Spalten leer); `ChildPause` entsteht leer.
   *Rot, wenn* die Migration fehlt (`SchemaGuardTests`, G1b) oder eine Bestandszeile ihre Werte verliert.
8. Das Vater-Web trägt eine Pause und die fünf Zahlen ein und liest sie danach vom Server zurück (build + read-back). *Rot,
   wenn* die Maske nach dem Speichern den eingegebenen statt den gelesenen Wert zeigt.

## Schätzung

**Größe: L** — Anker: eine DB-Umbau-Etappe wie E6. Eine Schemaänderung (Spalten und Tabelle in einer Migration), ein
Eingriff in Serie und Malus (der empfindlichste Teil, er trifft die Münzen echter Kinder), drei Endpunkte, zwei
Formulare im Vater-Web.

- **`migration: ja`** — fünf nullable Spalten an `Child`, neue Tabelle `ChildPause` (Fremdschlüssel auf `Child`, Index
  auf Kind und Von). An die Kette hängen (`SchemaGuardTests` G1b), Bestandszeilen mitnehmen.
- **`vertragsbruch: nein`** — nur Felder kommen hinzu (`ChildResponse`, `UpdateChildDto`) und neue DTOs für Pausen;
  das ist **additiv**. Positionale Records ziehen `Pugling.Client` und Tests im Quelltext nach, ohne die Wire-Form zu
  brechen (Probe: `UnknownFieldTests`, `ContractDocumentTests`).
- **`wo: beides`** — Backend zuerst (Serie, Malus, Endpunkte), dann `ChildCarePanels.tsx`.

### Risiken

- **Serie und Malus an echten Daten:** Der Eingriff ändert die Abrechnung der laufenden Pläne beider Söhne. Vorab eine
  Messung auf einer Kopie der echten DB (nie auf `pugling.db`).
- **`IsCompleteDay` und leere Tage:** Ein Tag ohne Plan bricht die Serie heute; der Sonntag muss als „überspringen"
  behandelt werden, nicht als „vollständig". Die Wochenpflicht endet weiter am Sonntag (`ClosedPeriods`), auch wenn der
  Sonntag frei ist.
- **N+1 in der Schleife:** Die Pausen des Kindes einmal laden, nicht je Tag.
- **Endpunkt-Abdeckung:** `EndpointCoverageGuard` verlangt, dass jeder neue Endpunkt im Test aufgerufen wird.
- **Wallet-Invariante** gilt erst, wenn der Malus-Betrag in B-736 abbucht; hier wird nichts gebucht.

### Angriffsplan

1. Migration und Modell (`Child`-Spalten, `ChildPause`), Migrationstest nach dem Muster `ActivatedAtMigrationTests`.
2. Hilfsdienst „Lerntag?" (Wochentag und Pausen, einmal je Kind geladen) neben `PositionProgressService`.
3. `ChildStreakAsync`, `ClosedPeriods` und `SettleClosedPeriodsAsync` darauf umstellen; Sonntag und Pause überspringen.
4. Endpunkte (Pausen, `UpdateChildDto`) mit Gates und Prüfungen.
5. Vater-Web: zwei Formulare in `ChildCarePanels.tsx`, Lesen zurück.

### Testweg

- Neue Klasse `LerntageTests` (Integration, `TestClock`): Sonntag ohne Serienbruch, Sonntag ohne Malus, Pause, Wochenpflicht
  unverändert. Rote Probe je AK: den Wochentagstest in `ChildStreakAsync` entfernen lässt AK 1 und 2 fallen; die
  Pausenabfrage entfernen lässt AK 4 fallen.
- Neue Klasse `LeitplankenTests` (Wertebereiche, Gates `403`/`404`) und `LeitplankenMigrationTests` (Muster
  `ActivatedAtMigrationTests`, `ShopStockPerChildMigrationTests`).
- Regressionsnetz: `PflichtMalusTests`, `RueckwirkendePflichtTests`, `VierWochenTests`, `OwnershipMatrixTests`,
  `TeacherAccountTests` (Creator-Konto bekommt `403`).
- Frontend: Vitest für die beiden Formulare; E2E neu `frontend/e2e/lerntage-und-pause.spec.ts` (Vater trägt Pause ein,
  Sohn-Tag zeigt keinen offenen Malus), Regression `frontend/e2e/vater-zeitachse.spec.ts`.
- Vor der Abnahme: `pugling-reviewer`, `frontend-reviewer`, Rollengang als Vater und Sohn.

## Reihenfolge und Abhängigkeiten

Nach [B-730](B-730-tagesbegriff-in-deutscher-zeit.md) (derselbe Tagesbegriff). Vor [B-733](B-733-tagesauswertung-und-leistungsprofil.md),
[B-734](B-734-planer-kern-und-schattenmodus.md) und [B-736](B-736-pflicht-aus-der-tagesaufgabe.md), die die Zahlen
und die Pause lesen.

## Verlauf

- **2026-11-20** — angelegt durch Teilung von B-644 (Etappe E0b) und `gegrillt` → `geschaetzt` im Vorbereitungslauf (Sonnet):
  Eintrittsbedingung `gegrillt` aus B-644 geerbt. Nachgesehen: Sonntag bricht die Serie (`:479`) und kostet Malus (`:686`),
  keine Pause im Modell, `UntilDone` als vierte Taktung. L, beides, Migration, additiver Vertrag.
