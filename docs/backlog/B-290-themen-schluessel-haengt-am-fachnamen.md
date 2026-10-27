---
tags: [typ/story, status/idee, bereich/katalog, rolle/creator]
aliases: [Themen-Idempotenz hängt am Fachnamen statt am Fach, Grammatik-Thema in gleichnamigem Fach nicht anlegbar]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: Nachschau zu Lauf E Teil 4 (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-155]
wartet_auf: ""
---

# B-290 · Der Idempotenz-Schlüssel eines Grammatik-Themas hängt am Fach**namen**, nicht am Fach

[B-155](B-155-grammatik-themen-als-tags.md) bildet den Slug eines Themas als `<fach-slug>-<label-slug>` und sucht beim
Anlegen **nur über den Slug** (`GrammarTopicsController.Create`): findet er ihn, kommt das bestehende Thema zurück
(200). Fachnamen sind aber bewusst **nicht eindeutig** (`PuglingDbContext`, Kommentar am `Subject`-Index: ein
„Englisch“ je Creator ist eine akzeptierte Produktentscheidung). Zwei Folgen, beide vom Prüfer am Code hergeleitet:

1. Ein zweiter Creator mit eigenem Fach „Englisch“ legt „Present perfect“ an → er bekommt **200 mit dem geseedeten
   Thema des anderen Fachs** (`subjectId` weicht vom Request ab, kein Fehler); der Picker nimmt es auf, das Speichern
   scheitert mit `validation_error` „belong to another subject“ – und es gibt **keinen Weg**, „Present perfect“ in
   seinem Fach anzulegen.
2. Wird ein Fach **umbenannt** (`SubjectsController` zieht den Namen in die Reihen nach, nicht in die Themen-Slugs),
   ändert sich der Präfix: „Present perfect“ im selben Fach erzeugt danach ein **zweites** Thema
   (`english-present-perfect` neben `englisch-present-perfect`) – AK 1 („gleiches Label im selben Fach ist ein
   Thema“) gilt nicht mehr.

Die Tests sehen beides nicht: `GrammatikThemenTests` nutzt nur `UniqueName`-Fächer.

**Behebung (Vorschlag des Prüfers):** Idempotenz-Schlüssel `(SubjectId, label-slug)` – entweder Unique-Index über
`(SubjectId, Slug)` mit dem unqualifizierten Slug, oder den Slug mit der Fach-**Id** statt dem Namen qualifizieren
(`<fach-id>-<label-slug>`); Suche beim Anlegen über `t.SubjectId == subject.Id && …`. Dazu je ein Test: zwei Fächer
gleichen Namens, und Umbenennen-dann-Anlegen.

## Verlauf

- **2026-10-27** — angelegt aus der Nachschau zu Lauf E Teil 4 (`1c1fe62`) ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)),
  🔴-Fund des frischen Prüfers; `entgangen_bei: B-155`.
