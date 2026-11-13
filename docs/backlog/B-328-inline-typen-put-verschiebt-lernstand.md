---
tags: [typ/story, status/geschaetzt, bereich/backend, rolle/creator, rolle/student]
aliases: [Inline-Typen ohne Sperre, Wort-Tausch per PATCH]
status: geschaetzt
prio: P3
art: Frage
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: B-323 (Lauf J, docs/pm-sitzung-2026-10-28-nacht.md)
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-328 · Sollen die Inhaltslisten gespielter Übungen anderer Typen (und der Wort-Tausch) gesperrt sein?

[B-323](B-323-ganzer-uebungs-put-umgeht-woerter-sperre.md) hat die Wörter-Sperre einer **Vokabelübung** auf jeden
schreibenden Weg gezogen. Zwei verwandte Wege bleiben offen, und beide sind eine Produktentscheidung, keine
Reparatur – darum `Frage`:

- **Inline-Typen** (Übersetzung, Grammatik, Lückentext ohne Store-Bezug …): ihr Inhalt ist die Config, und der
  `PUT` ersetzt sie ohne jede Prüfung (`ExerciseControllerBase.UpdateConflictAsync` liefert für sie `null`).
  `PositionItemProgress.ItemIndex` folgt der Listenposition (`ExerciseContentProvider.ItemsOf`), also verschiebt ein
  gestrichener Satz den Lernstand aller späteren. Eine Sperre wie bei Vokabeln würde aber auch das Korrigieren eines
  Tippfehlers in einem gespielten Satz verbieten – bei Vokabeln geschieht das im Store, hier gibt es keinen Store.
- **Wort-Tausch per `PATCH …/items/{id}`** mit neuer `vocabularyId`: der Index bleibt, das Wort wechselt, der
  Lernstand gehört danach zum neuen Wort. Die Sperre nennt nur Löschen und Umsortieren (`ExerciseControllers.cs`
  `PatchItem`), der Test `ItemMutation_BeiInPlanUebung_BlocktIndexVerschiebung_ErlaubtAnhaengen` deckt den Tausch nicht.

Zu entscheiden: Ist ein Tausch/eine Textänderung an gespielter Stelle eine **Korrektur** (erlaubt, Lernstand bleibt)
oder ein **Ersatz** (gesperrt)? Bei den Inline-Typen ggf. nur Länge/Reihenfolge sperren, Text erlauben.

## User Story

Als **Vater** möchte ich, dass das Bearbeiten einer Übung, die mein Sohn schon spielt, seinen Lernstand nicht still auf andere
Sätze oder Wörter verschiebt, damit Leitner-Stufen und Fälligkeiten weiter zu dem gehören, was er wirklich geübt hat.

## Ist-Stand am Code (nachgeprüft 2026-11-13)

- **Inline-Typen ohne Sperre:** [ExerciseControllerBase.cs:107-108](../../backend/Pugling.Api/Controllers/Creator/ExerciseControllerBase.cs#L107)
  (`UpdateConflictAsync` → `null`), aufgerufen im `PUT` bei `:456`. Überschrieben wird es nur von der Vokabelübung
  ([ExerciseControllers.cs:111-120](../../backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs#L111)).
- **Der Lernstand hängt am Index:** `PositionItemProgress.ItemIndex` – „index of the content in the item list"
  ([PlanPositionEntities.cs:204-205](../../backend/Pugling.Api/Models/PlanPositionEntities.cs#L204)); die Liste liefert je Typ
  `ItemsOf` ([BuiltInExerciseTypes.cs](../../backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs), z. B. `:25`, `:46`, `:114`;
  `ExerciseContentProvider.cs:65-69`). Die Vokabelübung liefert hier `[]` (`VocabularyExerciseType.cs:38`), ihre Items liegen in
  einer eigenen Tabelle.
- **Wort-Tausch ohne Sperre:** [ExerciseControllers.cs:252-283](../../backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs#L252)
  (`PatchItem`) setzt `item.Vocabulary = vocab` (`:270`) ohne Plan-Prüfung; gesperrt ist dort nur `OrderIndex` (`:273-278`). Auch
  `front`/`back` tauschen das Wort, weil `ResolveVocabularyAsync` per `GetOrCreateAsync` einen anderen Store-Eintrag liefert (`:330-338`).
- **Die Sperr-Bausteine gibt es:** `ExerciseInPlanAsync` und `ShiftBlockedProblem` (`exercise_items_locked`)
  ([ExerciseControllers.cs:313-319](../../backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs#L313)), heute `private` im
  Vokabel-Controller.
- **Test:** `ExerciseItemsAndProgressTests.cs:252` (`ItemMutation_BeiInPlanUebung_BlocktIndexVerschiebung_ErlaubtAnhaengen`) prüft
  Löschen, Umsortieren, Anhängen – nicht den Tausch, nicht die Inline-Typen.

## Die echte Lücke

Zwei Schreibwege verschieben den Lernstand einer gespielten Übung ohne Rückfrage: der `PUT` eines Inline-Typs mit kürzerer Liste und
der `PATCH` eines Vokabel-Items auf ein anderes Wort.

## Offene Punkte

1. ~~Inline-Typen: Korrektur oder Ersatz?~~ → Entscheidung 1 (2026-11-05)
2. ~~Wort-Tausch per `PATCH`?~~ → Entscheidung 2 (2026-11-05)
3. ~~Woran wird „andere Reihenfolge" ohne stabile Ids erkannt?~~ → Entscheidung 3
4. ~~Zählt der Tausch über `front`/`back` wie der über `vocabularyId`?~~ → Entscheidung 4

## Entscheidungen

Entscheidungen 1–2 stellvertretend am 2026-11-05 (Delegation, [Protokoll](../pm-sitzung-2026-11-05-delegation.md)); 3–4
Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13, [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).

1. **Inline-Typen:** gesperrt wird, was den Lernstand **verschiebt** – weniger Einträge oder eine andere Reihenfolge in
   einer Übung, die in einem Plan steht. Erlaubt bleiben Ändern an Ort und Stelle (Tippfehler im Satz) und Anhängen.
   Das ist dieselbe Linie wie bei Vokabeln (Löschen/Umsortieren gesperrt, Anhängen frei), nur ohne Store: die Korrektur
   geschieht im Eintrag selbst. *Kosten:* ein Satz lässt sich aus einer gespielten Übung nicht mehr streichen.
2. **Wort-Tausch per `PATCH …/items/{id}` mit neuer `vocabularyId`:** in einer Übung, die in einem Plan steht, gesperrt
   wie Löschen – der Lernstand gehört sonst einem Wort, das das Kind nie geübt hat. Die Korrektur eines Wortes geschieht
   im Store (dort bleibt der Bezug). *Kosten:* ein falsch gewähltes Wort bleibt in der gespielten Übung, bis man es im Store
   berichtigt.
3. **Erkannt wird nur „weniger Einträge"** (`ItemsOf(neu).Count < ItemsOf(alt).Count`). Ein Umsortieren gleich langer Listen ist von
   einer Text-Korrektur an mehreren Stellen ohne stabile Ids nicht zu unterscheiden; es bleibt eine **bekannte Grenze**, im
   `<summary>` benannt. Begründung: eine Sperre, die Korrekturen mitträfe, nähme Entscheidung 1 die erlaubte Hälfte. *Kosten:*
   wer gleich lange Listen umsortiert, verschiebt den Lernstand weiter still.
4. **Jeder `PATCH`, der das Item auf einen anderen Store-Eintrag zeigen lässt, ist ein Tausch** – gleich ob per `vocabularyId` oder
   `front`/`back`. Gleicher Eintrag (z. B. nur `hint` oder Übersetzungs-Alternativen) bleibt frei. Begründung: das Kind merkt keinen
   Unterschied zwischen den beiden Wegen. *Kosten:* auch ein per `front`/`back` „berichtigtes" Wort wird gesperrt; berichtigt wird im
   Store.

## Akzeptanzkriterien

1. `PUT` eines Inline-Typs (z. B. Übersetzung) mit **weniger** Einträgen auf eine Übung, die in einem Plan steht → `409
   exercise_items_locked`; die Übung ist unverändert.
2. Derselbe `PUT` mit gleich vielen Einträgen und geändertem Text → `200`; mit mehr Einträgen (angehängt) → `200`.
3. Ohne Plan-Position geht auch der kürzere `PUT` durch (`200`).
4. `PATCH …/vocabulary/{id}/items/{itemId}` mit einer anderen `vocabularyId` oder mit `front`/`back`, die einen anderen Store-Eintrag
   ergeben, auf eine Übung in einem Plan → `409 exercise_items_locked`; Item unverändert.
5. `PATCH` nur mit `hint` auf dieselbe Übung → `200`.

## Schätzung

**Größe: S** – eine Zählung im Basis-`UpdateConflictAsync`, eine Bedingung in `PatchItem`, zwei Testfälle. `migration: nein`,
`vertragsbruch: nein` (bestehender Fehlercode, `PUT` und `PATCH` deklarieren `409` schon).

**Risiken**

- `ExerciseInPlanAsync`/`ShiftBlockedProblem` müssen aus dem Vokabel-Controller in die Basis wandern (oder dort dupliziert werden);
  die Meldung nennt heute „items" und „adding to the end" – sie muss für Inline-Typen ebenso stimmen (B-414-Muster).
- Typen ohne Liste (`ItemsOf` → `[]`, `BuiltInExerciseTypes.cs:76`, `:412`) dürfen nie sperren: 0 < 0 ist falsch, also sicher.
- Die Vokabelübung überschreibt `UpdateConflictAsync`; die neue Basis-Logik darf dort nicht doppelt laufen.
- Ein Altbestand gespielter Übungen, deren Liste schon früher gekürzt wurde, bleibt verschoben; die Story heilt nicht rückwirkend.

**Angriffsplan (Backend zuerst)**

1. Rote Tests für AK 1 und AK 4.
2. Plan-Prüfung in die Basis ziehen, Basis-`UpdateConflictAsync` zählt `ItemsOf` alt/neu; `PatchItem` vergleicht `vocab.Id` mit
   `item.VocabularyId` und sperrt bei Plan-Bezug. `<summary>` mit der bekannten Grenze (Entscheidung 3).
3. Frontend: keine Änderung; der Editor zeigt `409` über das vorhandene `StatusBanner`.

**Testweg**

- `backend/Pugling.Api.Tests/ExerciseItemsAndProgressTests.cs`, neuer Fall `InlineTyp_InPlan_Sperrt_Kuerzere_Liste_Erlaubt_Korrektur`
  (AK 1–3). Rot, wenn die Basis weiter `null` liefert (AK 1), wenn sie jede Textänderung sperrt (AK 2) oder die Plan-Prüfung fehlt
  (AK 3).
- Neuer Fall `ItemPatch_InPlan_Sperrt_Wort_Tausch_Erlaubt_Hinweis` (AK 4–5). Rot, wenn `PatchItem` den Tausch nicht prüft, nur
  `vocabularyId` statt des aufgelösten Eintrags vergleicht (dann fällt die `front`/`back`-Zeile) oder auch `hint` sperrt.

## Verlauf

- **2026-10-28** — angelegt beim Bau von B-323 in Lauf J ([Protokoll](../pm-sitzung-2026-10-28-nacht.md)); aus Code
  gelesen, nicht gemessen. Bewusst nicht in B-323 genommen: `Frage`, im Nachtlauf nicht grillbar (Freigabe 1).
- **2026-11-05** — **Entscheidung im Auftrag des Nutzers** (getroffen von Claude; Delegation im Dialog am 2026-11-05: „Wenn es etwas zu entscheiden gibt, übernimm meine Position und entscheide nach bestem Wissen und Gewissen“; [Protokoll](../pm-sitzung-2026-11-05-delegation.md)): (1) Inline-Typen: Länge und Reihenfolge gespielter Inhaltslisten gesperrt, Textänderung an einer Stelle erlaubt (Korrektur); (2) Wort-Tausch per `PATCH …/items/{id}` mit neuer `vocabularyId` ist Ersatz und gesperrt, korrigiert wird im Store. **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt im Dialog am 2026-11-05 mit derselben Delegation):** ein Lauf darf diese Story auf Grundlage der Entscheidungen selbst ausformulieren, schätzen und bauen; trifft er auf eine Produktfrage, die sie nicht hergeben, hält er an.
- **2026-11-13** — ausformuliert mit zwei Entscheidungen, stellvertretend im Tageslauf (Restzeit); Schätzen und Bauen offen.
- **2026-11-13** — Ist-Stand am Code nachgeprüft (Backlog-Lauf): `UpdateConflictAsync` bei `:107-108`, `PatchItem` `:252-283`;
  User Story, Lücke und AKs ergänzt, `unverifiziert` entfernt.
- **2026-11-13** — **gegrillt**: Entscheidungen 3–4 (Reihenfolge als bekannte Grenze; `front`/`back` zählt als Tausch).
  Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13, [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).
- **2026-11-13** — **geschaetzt**: S, backend, keine Migration, kein Vertragsbruch.
