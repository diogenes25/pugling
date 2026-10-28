---
tags: [typ/story, status/idee, bereich/backend, rolle/creator, rolle/student]
aliases: [Reconcile ohne Sperre, refs-from-tags verschiebt Lernstand]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: Nachschau zu Lauf H, Teil 2 in Lauf I (docs/pm-sitzung-2026-10-28-nacht.md)
unverifiziert: true
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

## Verlauf

- **2026-10-28** — angelegt aus der Nachschau (frischer `pugling-reviewer`, `git show f96d358`) in Lauf I
  ([Protokoll](../pm-sitzung-2026-10-28-nacht.md)); aus Code gelesen, nicht gemessen.
