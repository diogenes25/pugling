---
tags: [typ/story, status/geschaetzt, bereich/katalog, rolle/creator, lerntechnik/vokabeln]
aliases: [Zehn Vokabeln, Obergrenze Vokabelübung, Untergrenze Vokabelübung, Mini-Übung]
status: geschaetzt
prio: P2
art: Wunsch
groesse: L
wo: beides
migration: nein
vertragsbruch: nein
quelle: Diskussion Regeln fürs Erstellen von Übungen (Sitzung 2026-11-16, Vater)
grund: ""
ersetzt_durch: []
wartet_auf: ""
---

# B-596 · Eine Vokabelübung hat 5 bis 10 Vokabeln

Regel des Vaters (Sitzung 2026-11-16): eine Vokabelübung trägt **mindestens 5 und höchstens 10 Wörter**. Heute gibt es
keine Grenze, weder oben noch unten. Laut Story haben die zehn Buch-Wortlisten mit Richtung `both` in der echten DB im
Schnitt 25 Wörter, die größte 44; diese Zahlen sind in diesem Lauf **nicht** nachgezählt worden (die DB wird nicht angefasst).
Mini-Übungen mit 2–4 Wörtern entstehen vermutlich an Seitengrenzen beim Fotografieren (Übung 38 „Unité 2 · Wörter
Atelier A – Anfang (S. 175)", 4 Wörter).

## User Story

Als **Creator** möchte ich, dass der Server Vokabelübungen außerhalb von 5 bis 10 Wörtern abweist, damit jede Übung
eine Sitzung lang ist und keine Mini-Übung oder Wortlisten-Wand entsteht.
Als **Schüler** möchte ich kurze, überschaubare Wortgruppen üben, damit ich nicht an einer Liste mit 44 Wörtern hängen
bleibe.

## Ist-Stand am Code

**Es gibt keine Zählgrenze, an keinem der Schreibwege:**

- Anlegen/Ändern über die Typ-Route: `VocabularyController.ValidateConfigAsync` prüft Referenzen, Inline-Felder,
  Sprachcodes und Doppelwörter, **nie die Anzahl**
  (`backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs:38-62`). Der Aufruf geschieht auf POST und PUT
  (`ExerciseControllerBase.cs:427`, `:520`).
- Einzelnes Item: `POST …/vocabulary/{id}/items` (`ExerciseControllers.cs:202`) prüft nur Auflösbarkeit und
  Doppelung, keine Obergrenze.
- Aus Tags: `POST …/refs-from-tags` (`ExerciseControllers.cs:129`) setzt die Items auf **alle** Wörter der Tags – ein
  Weg, der naturgemäß beliebig viele Wörter zieht.
- Fertiger Plan der schwachen Wörter: `WeakWordReviewService.MaxWords = 25`
  (`backend/Pugling.Api/Services/Supervisor/WeakWordReviewService.cs:22`, Prüfung `:38-39`) legt selbst eine
  Vokabelübung mit bis zu 25 Wörtern an. Eine Obergrenze 10 träfe sie (oder sie würde umgangen).
- Bestandsschutz besteht schon in anderer Form: Eine Übung, die ein Plan spielt, darf **nicht kürzer werden und keine
  Wörter umordnen**, nur am Ende wachsen (`ExerciseControllers.cs:111-118` und `ExerciseControllerBase.cs:115-129`,
  `ShiftBlockedProblem` `:135`; Prüfung `ExerciseItemService.WouldShiftAsync`,
  `Services/Creator/ExerciseItemService.cs:90-97`). Das hat Folgen für die Obergrenze, siehe Lücke.
- Zählung: Karten, nicht Wörter, gehen in die Tagesdosis – bei `both` sind es zwei Karten je Wort
  (`ExerciseContentProvider.CardCount`, `Services/Shared/ExerciseContentProvider.cs:109`).

**Die Anlege-Wege teilen laut Story lange Listen selbst auf – das stimmt nicht:**

- `laden.py` legt **eine Übung je Abschnitt der Wortliste** an, ohne jede Aufteilung (`.claude/skills/lehrwerk-aufbereiten/laden.py:443-456`);
  es warnt nur bei **weniger als 3** Wörtern (`:218-219`). Die Länge des Abschnitts entscheidet; das erklärt vermutlich die 25er-Schnitte
  aus der Story.
- KI-Creator: Aufgabenzahl `MinItems = 3`, `MaxItems = 30` (`backend/Pugling.Agent.Creator/Drafting/DraftRules.cs:40-43`),
  gesetzt über `request.ItemCount`; eine Aufteilung einer Unit-Wortliste in mehrere Übungen: nicht gefunden. „Part A (1/3)"
  kommt in `ExerciseStrategy.cs:83` nur als Beispiel für eine Titelkollision vor.
- Vater-Web: die Items laufen über denselben `POST …/items` (`frontend/src/lib/api.ts:518-520`); eine Zählanzeige oder
  Grenze im Editor: nicht gefunden.

**Mini-Übungen:** Eine **Untergrenze** gibt es nirgends in der API. Der Plan prüft nur „nicht leer" (`IsUnfilledAsync`,
`Controllers/Supervisor/PlanPositionsController.cs:36`, `:185`); der KI-Creator nutzt eine Wortliste erst ab 3
(`FixedPairs`, `Drafting/VocabularyStrategy.cs:34`).

**Korrektur beim Grillen (2026-11-19):** Der KI-Creator **schneidet** eine Unit-Wortliste schon auf `ItemCount` zu
(Vorgabe 10, `AgentCommands.cs:19`, `:573`): `UnitVocabulary.Slice(pool, weakWords, count, part)` nimmt `count` Wörter ab
`part × count`, rotierend (`backend/Pugling.Agent.Creator/Briefing/UnitVocabulary.cs:33-45`, Aufruf `BriefingBuilder.cs:93`),
und `Normalize` kürzt den Entwurf auf `ItemCount` (`Drafting/VocabularyStrategy.cs`). Über 10 kommt er nur mit `--count`
(Grenze `DraftRules.MaxItems = 30`, `Drafting/DraftRules.cs:43`). Die Aussage „Aufteilung einer Unit-Wortliste: nicht gefunden“
oben gilt also nur für `laden.py`; beim KI-Creator ist es ein Teilen **mit Überlappung** (die Scheibe läuft am Listenende
um), kein sauberes Zerlegen.

## Die echte Lücke

Die Regel ist nicht „eine Prüfung einbauen", sondern drei Dinge, die gegeneinander laufen:

1. **Oben deckeln und gespielte Übungen nicht kürzen dürfen** vertragen sich nicht: Eine Bestandsübung mit 44 Wörtern in
   einem Plan ist weder verkürzbar (Verschiebe-Sperre) noch änderbar, wenn jede Speicherung die Obergrenze verlangt. Die
   einzig stimmige Regel für Bestand ist „darf nicht wachsen", nicht „muss ≤ 10 sein".
2. **Unten mindestens 5** trifft das Anlegen in Schritten: Das Anlegen mit leeren Refs, dann `/items` oder
   `/refs-from-tags` ist ein ausdrücklich gewollter Weg (`PlanPositionsController.cs:181-184`). Eine Untergrenze auf POST
   würde ihn verbieten; sie kann nur beim **Zuweisen** greifen, wo heute schon „leer" abgewiesen wird.
3. **Die vier Erzeuger** (Typ-Route, Item-Route, Tag-Weg, Weak-Word-Review) müssen dieselbe Regel kennen, sonst ist
   sie über den vierten Weg umgehbar.

Offen bleibt auch, ob die Grenze wirklich im **Server** liegt oder bei den Erzeugern: Der Server-Weg ist die einzige
Stelle, an der die Regel für alle gilt, ist aber auch die einzige, die den Vertrag bricht (neue Fehlerfälle für bestehende
Clients) und die echten Daten der Söhne nicht berühren darf.

## Offene Punkte

1. ~~**Wörter oder Karten?**~~ → Entscheidung 1.
2. ~~**Wo greift die Obergrenze?**~~ → Entscheidung 2.
3. ~~**Bestandsübungen über 10:**~~ → Entscheidung 3.
4. ~~**Untergrenze 5: wo?**~~ → Entscheidung 4.
5. ~~**Rest an der Seitengrenze:**~~ → Entscheidung 5.
6. ~~**Weak-Word-Review (bis 25):**~~ → Entscheidung 6.
7. ~~**Bestehende Daten bei Friedrich:**~~ → Entscheidung 3 (nicht Teil dieser Story).
8. ~~**Wo steht die Regel für den Creator?**~~ → Entscheidung 7.
9. ~~**Vertragsbruch?**~~ → Entscheidung 8 (gemessen erst bei `geschaetzt`).
10. ~~**Berührt die Leitbild-Stories (B-680):**~~ → Entscheidung 9.

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-19 (Sonnet), unbestätigt.*

1. **Gezählt werden Wörter, nicht Karten; Grenzen `MinWords = 5`, `MaxWords = 10`.** Begründung: Ein Wort ist genau eine
   `ExerciseItem`-Zeile (eindeutig je Übung: Vorprüfung `ExerciseControllers.cs:224`, `RepeatedWordAsync` `:70-84`); Karten sind
   eine abgeleitete Größe je Richtung (`ExerciseContentProvider.CardCount`, `Services/Shared/ExerciseContentProvider.cs:109`).
   Beide Größen haben getrennte Schreiber, die Regel gleicht sie bewusst nicht gleich. Verworfen: Karten zählen (`both`
   hieße 5 Wörter = 10 Karten Untergrenze und 5 Wörter Obergrenze). Kosten: 10 Wörter bei `both` sind 20 Karten in einem
   Üben; die Tagesdosis (zählt Karten) deckelt den Umfang je Tag ohnehin. Ausgeschlossener Fall: ein Kind, dem 20 Karten
   am Stück zu viel sind. Gegen die User Story gerechnet („eine Sitzung lang“): trägt, weil die Dosis den Tag regelt, die
   Übung nur die Einheit.
2. **Ein Prädikat an allen Schreibwegen: abgewiesen wird, wenn `nachher > 10` und zugleich `nachher > vorher`.** Wege:
   Neuanlage (POST, vorher = 0), PUT mit Items (`UpdateConflictAsync`, `ExerciseControllers.cs:111`, hat `exercise` und damit
   `vorher`), `POST …/items` (`:202`), `refs-from-tags` (`:129`; die Fehlermeldung nennt die Trefferzahl, gespeichert wird nichts).
   Fehler `validation_error`. Ein PUT **ohne** Items bleibt unberührt (`HasPayloadItems`, `:115`). Die Regel sitzt in den
   Creator-Routen, **nicht** im geteilten `ExerciseItemService.ReconcileAsync` (Begründung Entscheidung 6). Verworfen: die
   Obergrenze nur auf POST. Kosten: vier Stellen, ein gemeinsamer Helfer nötig; ob POST über `ValidateConfigAsync`
   (`:38`, läuft auch bei PUT) oder einen eigenen Haken verdrahtet wird, entscheidet der Bau, die Akzeptanzkriterien prüfen
   nur das Verhalten. Ausgeschlossener Fall: ein Weg an der Regel vorbei – gegen die User Story („keine Wortlisten-Wand“)
   ist das der Fall, der sie leerlaufen ließe.
3. **Bestand über 10 wird nicht abgewiesen und nicht zum Teilen gezwungen; er darf nur nicht wachsen.** Zusammenspiel mit der
   Verschiebe-Sperre, geprüft an `WouldShiftAsync` (`ExerciseItemService.cs:90-97`: `current.Count > desired.Count` sperrt
   das Kürzen): eine **gespielte** Übung ist damit in der Länge eingefroren (nicht kürzer wegen der Sperre, nicht länger
   wegen Entscheidung 2); eine **ungespielte** Übung über 10 darf kürzer werden, auch auf einen Wert über 10. Automatisches
   Aufteilen bleibt verboten (zerreißt den Leitner-Stand an den Karten). Der Umbau echter Pläne (Friedrich, Plan 8) ist **nicht**
   Teil dieser Story, sondern ein Skript an einer DB-Kopie. Verworfen: jede Speicherung über 10 abweisen (die gespielte
   44er-Übung wäre nie wieder speicherbar). Kosten: die Wortlisten-Wand des Bestands bleibt, bis jemand sie per Skript
   teilt. Ausgeschlossener Fall: Bestand per API auf ≤ 10 zwingen; gegen die Schüler-Story heißt das, dass der Sohn die
   vorhandenen langen Listen weiter bekommt.
4. **Untergrenze 5 greift nur beim Zuweisen** (`PlanPositionsController.cs:185`, neben `IsUnfilledAsync`), als `validation_error`.
   Ausnahme nur durch ein neues, additives Feld `AllowShortList` (bool, Vorgabe `false`) an `CreatePositionDto`, das das
   Vater-Web nach einer Rückfrage setzt. Begründung: „anlegen, dann füllen“ ist ausdrücklich gewollt
   (`PlanPositionsController.cs:181-184`), eine Untergrenze auf POST verböte ihn; ob eine Liste die ganze Unit ist, kann
   der Server nicht wissen. Verworfen: der Server leitet „ganze Unit“ aus den Wörtern der Unit ab (fragil, ein
   Zweitfall je Unit). Kosten: ein Vertragsfeld und eine Rückfrage im Vater-Web. Ausgeschlossener Fall: eine kurze Liste,
   die ohne bewusste Bestätigung zugewiesen wird; gegen die User Story („keine Mini-Übung“) ist das der Zweck.
5. **Die Erzeuger entscheiden über Reste und Überlängen, der Server prüft nur.** `laden.py` (eine Übung je Abschnitt ohne
   Aufteilung, `laden.py:443-456`): Abschnitte unter 5 Wörtern werden an den Nachbarabschnitt derselben Unit gehängt, danach wird
   jeder Abschnitt über 10 **gleichmäßig** in `ceil(n/10)` Teile mit dem Titelzusatz „(Teil i/k)“ zerlegt (mindestens 5 Wörter
   je Teil für jedes n ≥ 11, gerechnet: 11 = 6+5, 21 = 7+7+7). Bleibt eine ganze Unit unter 5, bleibt sie und wird mit
   `AllowShortList` zugewiesen. KI-Creator: `MinItems`/`MaxItems` gelten für die Vokabeltypen als 5/10 statt 3/30
   (`DraftRules.cs:40-43`); `Slice` bleibt. Verworfen: `laden.py` bricht nur ab und überlässt dem Vater das Zerlegen
   (die zehn Buch-Wortlisten wären dann nicht mehr ladbar). Kosten: Abschnittsgrenzen des Buchs (Seiten) verschwimmen im
   Titel; zwei Erzeuger müssen vor dem Server nachziehen. Ausgeschlossener Fall: ein Abschnitt, der als eine Übung
   erhalten bleibt, weil er „zusammengehört“; gegen die Schüler-Story richtig, weil die 44er-Liste genau das Problem ist.
6. **Die Weak-Word-Review bleibt bei 25 Wörtern und ist von der Katalog-Obergrenze ausgenommen.** Sie legt ihre Übung über
   `ExerciseItemService` an, nicht über die Creator-Routen (`WeakWordReviewService.cs`, Konstruktor `items`, `MaxWords = 25`,
   `:22`), und ist ein Pflicht-Päckchen für einen Tag, keine Lerneinheit; ihr Umfang ist die Wahl des Vaters aus seiner
   Wortliste. Das ist **kein** Eingriff in Malus oder Münzen. Verworfen: auf 10 senken (würde die Wiederholung eines
   schwachen Tages zerhacken). Kosten: der Katalog enthält Übungen bis 25 Wörter, und die Regel „Katalog-Obergrenze“ hat eine
   benannte Ausnahme. Ausgeschlossener Fall: eine Wiederholung mit mehr als 10 Wörtern ist nicht „überschaubar“; gegen die
   Schüler-Story ist das der schwächste Punkt der Entscheidung.
7. **Die Grenzen stehen als benannte Konstanten im Vertragsprojekt neben `VocabularyConfig`**
   (`Pugling.Contracts/Exercise/ExerciseConfigs.cs:23`), `laden.py` dupliziert sie mit Verweis. Ein Typ-Manifest-Feld dafür
   habe ich im Vertragsprojekt nicht gefunden (Suche nach `Manifest` ohne Treffer); ob es eines gibt, klärt `geschaetzt`.
   Begründung: Wird die Zahl in Python dupliziert, ist Drift **laut** (der Server antwortet `400`), nicht still. Verworfen:
   Grenzen aus einem Endpunkt lesen. Kosten: eine doppelte Zahl. Ausgeschlossener Fall: ein Python-Lader, der sich selbst
   an eine Servergrenze anpasst.
8. **Reihenfolge des Baus: Erzeuger vor Server.** `laden.py` und der KI-Creator rufen die API auf und scheitern sonst an
   Listen über 10; sie ziehen im selben Schritt nach, bevor die Server-Regel scharf wird. Der Vertrag wächst additiv
   (`AllowShortList`, Konstanten) – `vertragsbruch` wird bei `geschaetzt` nachgesehen. Kosten: ein Bau in drei Teilen
   (Server, `laden.py`, Agent). Ausgeschlossener Fall: ein Server, der vor den Erzeugern scharf wird.
9. **Die Regel ist eine Katalog-Obergrenze, keine pädagogische Größe.** Ein Lehrer-Agent (B-680) schneidet Tagespakete über
   Positionen und `NewItemsPerDay`, nicht über die Wortzahl der Katalogübung. Diese Story ändert weder Rollenaufteilung noch
   Münzen oder Malus. Kosten: ein Paket von 12 Wörtern ist eine Übung mit 10 plus eine zweite. Ausgeschlossener Fall: eine
   Katalogübung, die ein Agent frei nach Tagesbedarf dimensioniert.

## Akzeptanzkriterien

Je Kriterium: welche Änderung macht es rot.

1. `POST …/vocabulary` mit 11 Wörtern liefert `validation_error`, mit 10 `201`. Rot, wenn die Grenze fehlt oder bei `> 11` liegt.
2. `PUT` mit Items auf eine in einem Plan gespielte Übung mit 44 Wörtern: **gleiche Länge** → `200`; 45 Wörter → `400
   validation_error`; 43 Wörter → weiter `409 exercise_items_locked` (Verschiebe-Sperre). Rot, wenn die Regel nur `nachher > 10`
   prüft (44 würde abgewiesen) oder das Wachstum zulässt.
3. `PUT` mit 30 Items auf eine **ungespielte** 44er-Übung liefert `200`. Rot, wenn die Obergrenze auf PUT absolut gilt.
4. `PUT` ohne Items und ohne Richtungswechsel (nur Titel/Einstellungen) auf die 44er-Übung liefert `200`. Rot, wenn die Prüfung die
   Zeilen der Tabelle zählt, obwohl der Payload keine Items trägt.
5. `POST …/items` auf eine Übung mit 10 Items liefert `validation_error`, mit 9 `201`, auf die 44er-Übung `validation_error`. Rot,
   wenn der Item-Weg die Grenze nicht kennt.
6. `refs-from-tags` mit 11 Treffern liefert `validation_error`, dessen Text „11“ nennt, und lässt die Items unverändert; auf
   einer 44er-Übung sind 40 Treffer erlaubt. Rot, wenn gekürzt oder gespeichert wird oder der Weg die Grenze nicht kennt.
7. Zuweisen einer Vokabelübung mit 4 Wörtern liefert `validation_error`, mit `AllowShortList = true` `201`, mit 5 Wörtern ohne
   das Feld `201`; Anlegen einer leeren Übung und späteres `…/items` bleibt möglich. Rot, wenn die Untergrenze beim Anlegen
   statt beim Zuweisen greift oder das Feld ignoriert wird.
8. Zehn Wörter mit `direction = both` (20 Karten) werden angenommen, fünf (10 Karten) auch. Rot, wenn `CardCount` statt der
   Wortzahl zählt (zehn Wörter `both` würden abgewiesen).
9. Die Weak-Word-Review mit 25 Wörtern entsteht weiter (`201`). Rot, wenn die Obergrenze im geteilten `ExerciseItemService`
   statt in den Creator-Routen sitzt.
10. `laden.py` macht aus einem Abschnitt mit 25 Wörtern drei Übungen mit 9/8/8 Wörtern und Titel „(Teil i/3)“ und hängt einen
    Abschnitt mit 4 Wörtern an den Nachbarn; geprüft an einer reinen Funktion (Testweg bei `geschaetzt`). Rot, wenn der
    Abschnitt unverändert als Übung mit 25 Wörtern in den Plan geht.
11. Der KI-Creator liefert für einen Vokabelentwurf mit `--count 25` keine Übung mit 25 Wörtern (Verstoß oder Kürzung auf 10).
    Rot, wenn `DraftRules.MaxItems = 30` für Vokabeltypen gilt.

## Schätzung

*Geschätzt im Vorbereitungslauf 2026-11-19 (Sonnet); die Entscheidungen oben sind weiter unbestätigt, `wartet_auf` bleibt.*

**Größe: L** – drei Erzeuger plus Server plus Vater-Web in einem Vorhaben, jeder Teil für sich M oder kleiner, zusammen eine
Etappe wie E6 (ein geteilter Eingriff, der Bestand und Test-Fixtures mitzieht). Nicht XL: die Teile hängen an einer Regel und
einem Prädikat und lassen sich als Commits in Reihenfolge bauen (Entscheidung 8), eine Zerlegung in Stories brächte nur
Zwischenstände, in denen der Server scharf und ein Erzeuger noch alt ist.

- **`wo: beides`** – Backend zuerst: Prädikat an vier Creator-Routen und die Untergrenze beim Zuweisen
  (`ExerciseControllers.cs:38`, `:111`, `:129`, `:202`; `PlanPositionsController.cs:185`); danach Erzeuger (`laden.py`, KI-Creator),
  zuletzt die Rückfrage im Vater-Web (`frontend/src/vater/PlanPositionsAdd.tsx`).
- **`migration: nein`** – nachgesehen: Die Zählung liest `ExerciseItem`-Zeilen bzw. den Payload, `AllowShortList` ist ein
  Request-Feld, kein Spaltenwert (`CreatePositionDto`, `Pugling.Contracts/Supervisor/StudyPlanDtos.cs:221`); kein Eintrag an der
  Migrationskette.
- **`vertragsbruch: nein`** – nachgesehen: `AllowShortList` kommt als **letzter** optionaler Parameter mit Vorgabe an
  `CreatePositionDto` (wie `CompletionBonus = null`, `:226`), die Konstanten sind neu; kein bestehendes Feld ändert Form oder Sinn.
  Client, Frontend-Typen (`S["CreatePositionDto"]`, `frontend/src/lib/types.ts:508`) werden neu erzeugt, aber nicht gebrochen.
  Der Bruch liegt im **Verhalten** (neue `validation_error`-Fälle für alte Aufrufer), nicht in der Form des Vertrags.

### Risiken

1. **Die Test-Fixtures laufen gegen die Untergrenze.** `TestApi.CreateVocabExerciseAsync` legt ohne Argumente **zwei** Wörter an
   (`TestApi.cs:178`); es hat 185 Aufrufe in 62 Testdateien, und 52 Dateien weisen über `/positions` zu. Jede davon trifft die
   Untergrenze 5 beim Zuweisen. Der Bau muss zuerst die Fixture-Vorgabe auf mindestens 5 Wörter anheben oder die Aufrufe
   `AllowShortList` setzen lassen und danach messen, welche Zusicherungen an „zwei Karten“ hingen – das ist der eigentliche
   Aufwand dieser Story, nicht das Prädikat.
2. **Das Frontend-E2E weist ebenfalls kurze Listen zu** (43 Treffer auf `positions` in `frontend/e2e/*.ts`); die Helfer in
   `frontend/e2e/helpers.ts` legen Übungen mit wenigen Wörtern an und müssen mitziehen.
3. **Die Weak-Word-Review (1 bis 25 Wörter) geht nicht über `PlanPositionsController`**, sondern über `WeakWordReviewService`
   (`:35`). Die Untergrenze darf sie nicht treffen: eine Wiederholung von 3 schwachen Wörtern muss weiter gehen (Testfall bei
   AK 9 ergänzen).
4. **Reihenfolge ist Pflicht (Entscheidung 8):** wird die Server-Regel vor `laden.py` und dem KI-Creator scharf, scheitert jeder
   Ladevorgang mit 25er-Abschnitten mit `400`.
5. **`laden.py` hat eine Kopie der Config-Felder** (`LadenFieldListTests`); die neuen Konstanten sind keine Felder, die Liste
   bleibt gleich, der Test muss unverändert grün sein.
6. **Gleichmäßiges Teilen** (Entscheidung 5) und das Anhängen von Resten verschieben die Abschnittsgrenzen des Buchs; die
   Titel „(Teil i/k)“ kollidieren nicht mit der Titeleindeutigkeit je Unit, wenn `laden.py` sie vor dem Anlegen prüft
   (nicht nachgemessen).

### Angriffsplan

1. Konstanten `MinWords = 5`, `MaxWords = 10` im Vertragsprojekt neben `VocabularyConfig`
   (`Pugling.Contracts/Exercise/ExerciseConfigs.cs:23`).
2. Ein Helfer für das Prädikat `nachher > 10 && nachher > vorher` in `ExerciseControllers.cs`; Verdrahtung auf POST/PUT
   (Payload mit Items), `POST …/items` und `refs-from-tags` (AK 1 bis 6, 8). `ExerciseItemService` bleibt unberührt (AK 9).
3. Fixtures: `TestApi.CreateVocabExerciseAsync` auf mindestens 5 Wörter, Suite messen, gebrochene Zusicherungen einzeln
   nachziehen. Erst danach Schritt 4.
4. `AllowShortList` an `CreatePositionDto`; Untergrenze beim Zuweisen neben `IsUnfilledAsync`
   (`PlanPositionsController.cs:185`), nur für Vokabelübungen (AK 7).
5. `laden.py`: Reste anhängen, über 10 gleichmäßig teilen, Titelzusatz; reine Funktion (AK 10).
6. KI-Creator: 5/10 für die Vokabeltypen in `DraftRules` (AK 11).
7. Vater-Web: Rückfrage vor dem Zuweisen einer kurzen Liste, setzt `AllowShortList`.
8. `dotnet build Pugling.sln`, `dotnet format Pugling.sln`, Reviewer `pugling-reviewer` und `frontend-reviewer`.

### Testweg

- Neue Klasse `VocabWordLimitTests` (Vorbild `VocabExerciseAuthoringTests`): AK 1 bis 6 und 8, je Weg ein Fall; die gespielte
  44er-Übung per `TestApi.SeedLeitnerPosition` und 44 Items als Setup. Rote Probe je AK: die Spalte „rot, wenn …“ oben.
- `PlanPositionCrudTests` (Vorbild `EmptyExerciseGuardTests`, `:156`): AK 7 mit 4 Wörtern ohne und mit `AllowShortList`, mit 5
  Wörtern ohne das Feld, leere Übung anlegen und später füllen.
- `WeakWordReviewTests`: AK 9 mit 25 Wörtern und der Fall mit 3 Wörtern.
- `.claude/skills/lehrwerk-aufbereiten/test_laden.py`: AK 10 an der reinen Teilen-Funktion (25 → 9/8/8, 4 → angehängt, 11 → 6+5).
- `CreatorAgentTests`: AK 11.
- `frontend/src/vater/PlanPositionsAdd.test.tsx`: Rückfrage setzt das Feld (Komponententest); Rollengang als Vater beim
  Zuweisen einer 4-Wörter-Übung vor der Abnahme.

**Bau-Sperre:** die Story bleibt bis zur Bestätigung der Entscheidungen 1 bis 9 durch den Nutzer nicht baubar.

## Verlauf

- **2026-11-16** — angelegt aus der Diskussion über Regeln fürs Erstellen von Übungen.
- **2026-11-16** — offene Frage „Wörter oder Karten?“ aus dem Grillen von B-597 übernommen.
- **2026-11-16** — um die Untergrenze 5 erweitert (Vater im Chat), Titel auf „5 bis 10“ gezogen.
- **2026-11-19** — ausformuliert (Vorbereitungslauf): keine Zählgrenze an vier Schreibwegen; die Behauptung „`laden.py` und
  KI-Creator teilen selbst auf“ ist nicht belegbar; die Obergrenze kollidiert mit der Verschiebe-Sperre für gespielte
  Übungen; zehn offene Punkte mit Empfehlung.
- **2026-11-19** — gegrillt (Vorbereitungslauf, Sonnet, stellvertretend, unbestätigt, `wartet_auf` gesetzt): Entscheidungen 1 bis 9.
  Gegen den Code korrigiert: Der KI-Creator schneidet Unit-Wortlisten schon auf `ItemCount` (Vorgabe 10). Untergrenze nur beim
  Zuweisen mit additivem `AllowShortList`; Bestand darf nicht wachsen (Zusammenspiel mit der Verschiebe-Sperre belegt).
  Weak-Word-Review bleibt bei 25. Akzeptanzkriterien final, je mit roter Änderung.
- **2026-11-19** — geschaetzt (Vorbereitungslauf, Sonnet; `wartet_auf` bleibt): L, beides (Backend zuerst), keine Migration, kein Vertragsbruch
  (additives `AllowShortList`). Größter Posten: die Test-Fixtures (zwei Wörter als Vorgabe, 185 Aufrufe in 62 Dateien) laufen gegen die Untergrenze.
- **2026-11-19** — Entscheidungen 1 bis 9 bestätigt. Stellvertretend auf Anweisung des Nutzers („nimm für alle Fragen deine Empfehlung“): die Empfehlung der Sonnet-Entscheidungen wurde übernommen.
