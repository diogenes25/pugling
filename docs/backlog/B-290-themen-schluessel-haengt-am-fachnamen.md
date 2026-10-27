---
tags: [typ/story, status/abgenommen, bereich/katalog, rolle/creator]
aliases: [Themen-Idempotenz hängt am Fachnamen statt am Fach, Grammatik-Thema in gleichnamigem Fach nicht anlegbar]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: backend
migration: ja
vertragsbruch: nein
quelle: Nachschau zu Lauf E Teil 4 (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-155]
nachgeschaut: "2026-10-27"
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
- **2026-10-27** — in Lauf E, Teil 6, autonom gegrillt (Freigabe 1, `Defekt`), geschätzt (S, Migration) und gebaut: der Slug ist jetzt der reine Label-Schlüssel (`present-perfect`), eindeutig **je Fach** über einen zusammengesetzten Unique-Index `(SubjectId, Slug)`; das Anlegen sucht über `SubjectId && Slug`; der Seed schreibt den reinen Schlüssel. Entscheidung 4 von B-155 („fachqualifiziert“) ist damit strukturell erfüllt – durch die Fach-Id, nicht durch den Namen; die Story-Beispiele (`en-present-perfect`) waren eine textliche Qualifikation, die an nicht-eindeutigen Namen scheitert. Kette neu gefaltet, Snapshot-Diff: Index `Slug` → `(SubjectId, Slug)`, Einzelindex `SubjectId` entfällt (der zusammengesetzte deckt ihn). **Rote Probe:** `Gleichnamige_Faecher_Und_Umbenennen_Brechen_Die_Idempotenz_Nicht` (zwei Creator, zwei gleichnamige Fächer, danach Umbenennen) – Mutante „Suche nur über den Slug“ → **4 failed** (dieser Fall und drei bestehende, deren Themen jetzt in fremden Fächern getroffen würden), danach `GrammatikThemenTests` + Schema-Tore **17/17 `exit=0`**. Dazu ohne Zählung die zwei 🟢 der Nachschau: `key={subjectId}` am Picker, `toBeUndefined()` im Assistenten-Test.
- **2026-10-27** — Review (`pugling-reviewer`, [Protokoll](../pm-sitzung-2026-10-27-nacht.md)), Zähler 0: kein Code-Fund. 🟡 veraltete `Create`-Summary (fließt in Swagger) und 🟡 `docs/openapi/v1.json` muss den neuen Vertragstext tragen (sonst CI rot) → Summary und Client-Kommentar nachgezogen, Dokument per `ContractDocumentTests` neu erzeugt (nur Kommentar/Doku, ohne Zählung). Geprüft und sauber: kein Verbraucher setzt globale Slug-Eindeutigkeit voraus (Suche per LIKE, Client über Ids, Frontend je Fach, E2E über Labels), alte Dev-DBs werden mit klarer Meldung abgewiesen. **Rollengang:** kein Browser; Integrationstests sind der Weg (zwei Creator, zwei gleichnamige Fächer, Umbenennen), `grammatik-themen.spec` + `assistent.spec` **4 passed** als Zeugen, Suite **969/969 `exit=0`**. `abgenommen`; Commit `6d20ddd`.
- **2026-10-27** — Nachschau in Lauf E, Teil 7 ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)), frischer Prüfer an einem `git archive` von `6d20ddd` (969/969, Vitest 20/20): Cascade am Fach, LIKE-Suche je Fach, Sortierung ohne Label-Index (24 Zeilen je Fach, akzeptabel), beide Testhälften gegen Namens- und Slug-Schlüssel, Eindeutigkeit des Seed-Falls im geteilten Testlauf, `key={subjectId}`, `Slug = slug!` – je Zeile geprüft. **Kein Code-Entgleiten.** Drei Doku-Entgleiten hier berichtigt: `backend/Pugling.Api/CLAUDE.md` nannte noch den Namenspräfix, die Klassen-Summary von `GrammarTopic` widersprach ihrer Property-Doku, ein Testname sagte „fachqualifizierter Slug“. Notiert, nicht neu: zwei gleichzeitige POSTs desselben Labels enden als 500 (Unique-Verletzung) – gilt ebenso für Interessen und Verlage.
