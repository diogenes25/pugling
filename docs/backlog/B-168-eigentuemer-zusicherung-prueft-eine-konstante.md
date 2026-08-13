---
tags: [typ/story, status/ausformuliert, bereich/backend, bereich/tests, rolle/creator]
aliases: [Eigentuemer ist Adult 1, Anlegen macht den Aufrufer zum Eigentuemer prueft eine Zahl]
status: ausformuliert
prio: P1
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Nachschau 2026-08-13 zu B-13
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-13]
---

# B-168 · „Anlegen macht den Aufrufer zum Eigentümer" prüft eine Konstante, keine Identität

Die Zusicherung, die B-13s wichtigstes Akzeptanzkriterium tragen soll, vergleicht gegen die Zahl `1`.
Im Seed **ist** das die Id des Vaters — die Zusicherung hält also „Eigentümer ist Adult 1" und nicht
„Eigentümer ist der Aufrufer". Solange kein anderer Creator dasselbe tut, ist der Unterschied unsichtbar,
und niemand tut es.

## Ist-Stand am Code (belegt und selbst nachgeprüft)

```csharp
// backend/Pugling.Api.Tests/FachEigentumTests.cs:52 (im Test Anlegen_MachtDenAufrufer_ZumEigentuemer)
Assert.Equal(1, created.GetProperty("ownerAdultId").GetInt32());
```

`TestApi.AdultAsync(factory)` steht per Vorgabe auf dem geseedeten Vater, `id = 1`
(`backend/Pugling.Api.Tests/TestApi.cs:32`). Dieselbe Verwechslung ein zweites Mal in `:102`.

**Der Grund, warum das trägt, ist die Lücke daneben.** Alle vier `PATCH`-Aufrufe auf ein Fach in der
gesamten Suite (`FachEigentumTests.cs:62,78,300,320`) sind entweder Adult 1 auf eigenem Fach, ein Fremder
auf **fremdem** Fach (403 erwartet) oder ein Seed-Fach (403 erwartet). **Kein Test lässt einen anderen
Creator als den Seed-Vater sein eigenes Fach erfolgreich umbenennen oder löschen.**

Fremde Creator legen durchaus Fächer an (`TeacherAccountTests.cs:85`, `SharedLibraryScenarioTests.cs:29`,
`ExerciseUsageScopeTests.cs:114`), aber keiner liest danach `ownerAdultId` oder schreibt auf das Fach.
Auch `TeacherAccountTests.Lehrer_DarfInhalteAnlegenUndBesitzenWieBisher` — der Fall mit „…UndBesitzen" im
Namen — prüft `isOwner` an der **Übung** (`:126`), nie am Fach.

## Fehlerszenario

`SubjectsController.cs:60` von `OwnerAdultId = fid` auf `OwnerAdultId = 1` ändern (oder auf
`User.AccountId()`, was für Adult 1 dieselbe `1` liefert):

1. Zweit-Creator (Adult 2) → `POST creator/subjects {name:"Latein"}` → `201`, `ownerAdultId: 1`.
2. Derselbe Creator → `PATCH creator/subjects/{id}` → **`403 not_owner` auf sein eigenes, gerade
   angelegtes Fach.** Umbenennen und Löschen sind für jeden Creator außer dem Seed-Vater dauerhaft zu —
   und der Seed-Vater darf dafür jedes fremde Fach umbenennen.
3. Suite: **828/828 grün.** Alle sechs B-13-Fälle bleiben grün: Fall 1 sieht die erwartete `1`, Fall 2
   arbeitet als Adult 1, die Fälle 3–6 erwarten ohnehin `403`.

Das ist genau der Schaden, gegen den B-13 gebaut wurde, nur in der anderen Richtung. **Der
Produktionscode ist heute richtig** — an allen vier Stellen. Was fehlt, ist das Netz: B-13s AK 1 ist zur
Hälfte belegt, AK 3 nur für den einen Eigentümer, dessen Id mit der Konstante zusammenfällt.

## Fehlerfamilie

Die zweite der beiden gemessenen: *eine Zusicherung, die nicht fehlschlagen kann.* Hier in ihrer
tückischsten Form — sie prüft nicht den Ausgangszustand, sondern eine Zahl, die im Ausgangszustand
**zufällig** stimmt. Ein Testname („…MachtDenAufrufer…") behauptet dabei mehr als der Rumpf hält, was die
Verwechslung beim Lesen zudeckt.

## Angriffsplan

Das Material liegt schon da: `ZweiterCreatorAsync` (`FachEigentumTests.cs:16-24`) liest die Adult-Id des
Fremden bereits und **wirft sie weg**.

1. `ZweiterCreatorAsync` gibt die Id mit zurück (Tupel oder ein kleines `record`).
2. Beide Konstanten-Vergleiche (`:52`, `:102`) gegen die Id des tatsächlichen Aufrufers stellen.
3. **Den fehlenden Fall ergänzen** — er ist der tragende: Der Fremde legt sein **eigenes** Fach an →
   `ownerAdultId == seineId`, `isMine == true`; sein `PATCH` darauf → `200`.
4. Gegenrichtung, heute ebenfalls ungeprüft: Adult 1 auf das Fach des Fremden → `403 not_owner`.
5. Rote Probe **mit Zahl**: `OwnerAdultId = 1` hartkodieren und belegen, dass jetzt genau die neuen Fälle
   fallen — nicht „irgendetwas wird rot".

**Testweg**: `FachEigentumTests` erweitern, keine neue Datei. Kein Frontend-Anteil, kein Schema, kein
Vertrag.

## Verlauf

- 2026-08-13 · Aufgenommen aus der **Nachschau** zu B-13, dort belegt und von mir gegengeprüft
  (`TestApi.cs:32`, die vier `PATCH`-Fundstellen, `ZweiterCreatorAsync`). `ausformuliert` direkt, weil der
  Ist-Stand aus dem Code belegt ist und der offene Punkt keiner ist: der fehlende Fall steht fest.
