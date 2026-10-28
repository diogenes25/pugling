---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/creator, rolle/student]
aliases: [Reconcile ohne Sperre, refs-from-tags verschiebt Lernstand]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf H, Teil 2 in Lauf I (docs/pm-sitzung-2026-10-28-nacht.md)
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-323 · Ein ganzer Übungs-PUT und `refs-from-tags` umgehen die Wörter-Sperre einer gespielten Übung

Die Sperre „Wörter einer Übung, die schon in einem Plan steht, dürfen nicht entfernt oder umsortiert werden“
(`ShiftBlockedProblem`, seit [B-205](B-205-exercise-in-use-ohne-deutsche-fassung.md) mit Code `exercise_items_locked`)
sitzt nur an den drei Einzel-Routen der Item-Unterressource (`ExerciseControllers.cs` AddItem mit `orderIndex`,
PatchItem, DeleteItem). Zwei Wege schreiben dieselbe Liste ohne Prüfung:

- `PUT …/exercises/{id}` mit `items` → `VocabularyController.AfterSaveAsync` → `ExerciseItemService.ReconcileAsync`
  (setzt `OrderIndex = i`, entfernt fehlende Wörter per `RemoveRange`);
- `POST …/refs-from-tags` (`ExerciseControllers.cs:121`), derselbe Pfad.

`PositionItemProgress` hängt an `ItemIndex` (`PlanPositionEntities.cs:199`), und der folgt `OrderBy(OrderIndex)`
(`ExerciseContentResolver.cs:78`). **Szenario** (aus Code gelesen, nicht gemessen): eine Vokabelübung [A, B, C] steht
im Plan, das Kind hat B in Fach 5; ein Creator ruft `refs-from-tags` ohne A auf → B rückt auf Index 0, C auf 1, der
Fach-5-Stand gehört jetzt zu C – ohne 409. Das Frontend meidet diese Wege, die API (das Produkt) und der KI-Creator
nicht. Dieselbe Klasse vermutlich für die Konfigurationslisten anderer Typen (Übersetzung, Grammatik, Lückentext) per PUT.

`wiki/03-uebungstypen.md:144-148` und `docs/endpunkt-beziehungen.md:84-86` behaupten die Sperre ohne Einschränkung.

Zu klären beim Ausformulieren: Sperre auch an `AfterSaveAsync`/`refs-from-tags` (außer reinem Anhängen) – oder die
Doku auf „nur die Einzel-Route“ verengen; ob die anderen Typen dieselbe Lücke haben (messen).

Älter als B-205: B-205 hat nur Code und Text der Sperre geändert, nicht ihren Umfang. Darum `entgangen_bei` leer.

## User Story

Als **Creator**, der eine Vokabelübung über die API pflegt (auch als KI-Creator), möchte ich, dass jeder Weg, der ihre
Wörter schreibt, dieselbe Sperre trägt, damit der Lernstand eines Kindes, das die Übung schon spielt, nie stillschweigend
an ein anderes Wort rutscht.

## Ist-Stand (Lauf J, am Code belegt)

- Sperre nur an den Item-Routen: `ExerciseControllers.cs` `AddItem` (mit `orderIndex`), `PatchItem` (Umsortieren),
  `DeleteItem`, je über `ExerciseInPlanAsync` → `ShiftBlockedProblem`.
- `PUT` → `ExerciseControllerBase.Update` speichert Kopf **und** Config (`db.SaveChangesAsync`) und ruft erst danach
  `AfterSaveAsync` → `ExerciseItemService.SyncFromConfigAsync` → `ReconcileAsync` (setzt `OrderIndex = i`, `RemoveRange`).
  Keine Prüfung, und eine nachträgliche hätte den Kopf schon geschrieben.
- `refs-from-tags` ruft `ReconcileAsync` direkt, ohne Prüfung.
- **Gemessen** (rote Probe unten): `PUT` mit gestrichenem erstem Wort und `refs-from-tags` ohne das erste Wort
  antworten an einer gespielten Übung `200`.

## Die echte Lücke

Die Regel „nur Anhängen“ sitzt an der Route statt an der Liste. Zwei Wege, die dieselbe Liste schreiben, kennen sie nicht.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Sperre erweitern, nicht die Doku verengen.** Die Doku beschreibt die richtige Regel; die API ist das Produkt, und
   der KI-Creator nutzt genau diese Wege. *Kosten:* ein Creator, der eine gespielte Übung per `PUT` neu ordnen will,
   bekommt jetzt `409` statt Erfolg – wie heute schon über `/items`.
2. **Regel: die bisherigen Items bleiben der Anfang der neuen Liste, in derselben Reihenfolge** – Anhängen erlaubt,
   alles andere `409 exercise_items_locked`. Dieselbe Regel wie an den Einzel-Routen. *Kosten:* keine.
3. **Prüfung vor jedem Schreiben** (neuer Haken `UpdateConflictAsync` in der Basisklasse, nach Validierung), damit ein
   abgewiesener `PUT` auch den Titel nicht ändert. Die Prüfung legt dafür nichts im Store an (`PeekDesiredIdsAsync`
   sucht ein Inline-Wort nur per Schlüssel). *Kosten:* ein virtueller Haken mehr in der Basisklasse; `409` steht jetzt
   im Vertrag des `PUT` aller Typen, obwohl nur Vokabeln ihn liefern.
4. **Inline-Typen und Wort-Tausch per `PATCH` bleiben draußen** – Produktfrage, abgelegt als
   [B-328](B-328-inline-typen-put-verschiebt-lernstand.md) (`Frage`). *Kosten:* die Lücke bleibt dort offen und ist in
   `docs/endpunkt-beziehungen.md` benannt.

## Akzeptanzkriterien

1. `PUT` einer gespielten Vokabelübung, der ein Wort streicht, umsortiert oder in der Mitte einfügt → `409
   exercise_items_locked`; Items **und** Titel unverändert.
2. `PUT`, der nur anhängt → `200`, die bisherigen Items behalten ihre Id.
3. `refs-from-tags`, dessen Treffer ein gespieltes Wort verlieren → `409`, Items unverändert; ein Schnappschuss, der nur
   anhängt → `200`.
4. Doku (`wiki/03-uebungstypen.md`, `docs/endpunkt-beziehungen.md`) nennt den Umfang der Sperre genau.

## Schätzung

`S`, Backend. Angriffsplan: rote Probe → Dienst-Methoden → Haken in der Basis → `refs-from-tags` → Doku. Testweg:
Integrationstests in `ExerciseItemsAndProgressTests`; Live-Probe gegen die laufende API.

## Verlauf

- **2026-10-28** — angelegt aus der Nachschau (frischer `pugling-reviewer`, `git show f96d358`) in Lauf I
  ([Protokoll](../pm-sitzung-2026-10-28-nacht.md)); aus Code gelesen, nicht gemessen.
- **2026-10-28** — Lauf J: ausformuliert, autonom gegrillt (Freigabe 1), geschätzt, gebaut. Rote Probe: zwei neue Fälle
  **2 failed** (erwartet 2), beide an `Assert.Equal(Conflict, …)` (Z. 309, 348; gemessen `OK`). Mutante „Obermenge statt
  Präfix“ **1 failed** (Z. 309, Tausch/Einfügen); Mutante „Prüfung erst nach dem Speichern“ **1 failed** (Z. 314, Titel
  `Umbenannt`). Danach betroffene Klassen **27/27**.
- **2026-10-28** — Review `pugling-reviewer`: 🟡 Refs-Zweig der Prüfung ohne Fall → zwei Fälle ergänzt (Mutante „Items vor Refs“ **1 failed** Z. 332, „Refs-Zeile gestrichen“ **1 failed** Z. 332, „Refs nur neben Items“ **1 failed** Z. 354), zählt; 🟡 doppeltes Wort → 500 (älter, außerhalb des Diffs) → [B-331](B-331-doppeltes-wort-im-uebungs-payload-500.md), Wiki-Satz auf `409` verengt; 🟢 Kommentar zu Schlüssel-Refs, Szenario-Schritt 7 sichert den `409` zu. Volle Suite **1004/1004**, E2E **99 passed** (nach der letzten Änderung gestartet). Rollengang Creator: Live-Probe gegen frisch gestarteten Server (Wegwerf-DB, Port 5280) – Übung anlegen, über die Supervisor-API in einen Plan nehmen, `PUT` ohne erstes Wort → `409 exercise_items_locked`, Titel unverändert; Anhängen → `200`. Vater und Sohn Regressionszeugen (E2E). **Abgenommen**, Commit `1a3a73f`.
