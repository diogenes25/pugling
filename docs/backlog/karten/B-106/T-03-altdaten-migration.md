# T-03 · Wie wird der Chapter-Altbestand migriert?

Status: entschieden     <!-- offen | beansprucht | entschieden -->
Typ: task                <!-- research | prototype | grilling | task -->
Blockiert durch: T-01

## Frage

Die EF-Migrationskette wird bei Schemaänderungen neu gefaltet (nicht verlängert), Altdaten sind laut
Root-`CLAUDE.md` bis zur Veröffentlichung verzichtbar. Trotzdem hängt an Chapter heute Seed-Content
(`Seed.cs`, mehrere hundert Zeilen Subject/Chapter/Exercise) und die gesamte Testsuite
(`CatalogManagementTests`, `ExerciseGrantsTests`, etc.), die über `chapterId`-Routen läuft. Wird der
Seed komplett auf `SeriesUnit`-Anker umgeschrieben (erfordert dort zuerst katalogisierte Reihen/Units im
Seed), oder bekommt jedes Seed-Fach eine synthetische 1:1-`SeriesUnit`-Entsprechung? Wie viele
Testdateien sind betroffen (Umfang vorab abschätzen, bevor der Code-Slice beginnt)?

## Antwort

**Echte Reihen/Units statt synthetischer 1:1-Stubs — mit dem bereits vorhandenen Beleg als Vorlage.**
Live geprüft (PM-Loop-Runde 2026-08-04): über die gesamte Seed-Landschaft existiert heute **keine**
katalogisierte `TextbookSeries` (`creator/textbook-series` → `[]` bei zwei verschiedenen Creator-Konten),
obwohl der Übungskatalog reich ist (Englisch: 3 Kapitel mit Übungen, u. a. „Unit 1 – Greetings",
„Unit 2 – Family", „Unit 5 – Global challenges"). Der Seed trägt bereits ein passendes Vorbild
(`SeedStudentProfile`, `Seed.cs:203-212`: `Textbook { Title = "Green Line 1", Publisher = "Klett",
SubjectName = "Englisch" }`, heute nur Freitext). Der Seed wird umgeschrieben, um für Englisch eine
echte `TextbookSeries` „Green Line 1" (Klett) mit `SeriesUnit`s anzulegen, die 1:1 den heutigen
Kapiteln entsprechen (Greetings/Family/Global challenges) — kein austauschbarer Platzhaltername, weil
der Seed dann konsistent mit dem bereits erzählten Freitext-Beispiel bleibt. Die übrigen drei Fächer
(Mathe, Erdkunde, Französisch, je ein Kapitel) bekommen je eine einzige pauschale Reihe/Unit, damit ihre
bestehenden Übungen nicht ersatzlos verwaisen.

**Laufende Pläne sind strukturell bereits sicher, nicht nur versprochen:** `PlanPosition` referenziert
ausschließlich `ExerciseId` (`PlanPositionEntities.cs:17-129`), nie `ChapterId`/`SubjectId` direkt — ein
bestehender Plan bleibt beim Umhängen der Exercise-FK unberührt, solange die `ExerciseId` selbst stabil
bleibt (reine Spalten-Umbenennung der Fremdreferenz, keine neuen/gelöschten `Exercise`-Zeilen). Das
entkräftet die in dieser Runde von Vater und Sohn geäußerte Sorge strukturell.

**Umfang vorab sizen, nicht raten:** Vor dem eigentlichen Code-Slice zählt
`grep -rn "chapterId\|ChapterId" backend/` die betroffenen Stellen (erwartungsgemäß: alle zwölf
Typ-Controller, `ExerciseControllerBase`, `Seed.cs`, `CatalogManagementTests`, `ExerciseGrantsTests`,
und jeder typspezifische Controller-Test, der über die Chapter-Route anlegt) — das Ergebnis geht direkt
in den Angriffsplan des Schema-Slice-Sprints, nicht in dieses Ticket.

**Kosten:** Der Seed wächst um eine echte Lehrwerk-Hierarchie für Englisch (mehr Zeilen als der
heutige Freitext-`Textbook`-Eintrag), die übrigen drei Fächer bekommen nur eine pauschale Reihe/Unit
(kein Anspruch auf lehrwerktypische Differenzierung). Jeder Test, der heute über eine
`chapterId`-Route anlegt, muss auf die neue Route umgestellt werden — Umfang erst beim Bauen final,
über den Grep-Befund oben.

**Verlauf:** 2026-08-04 — gegrillt, autonom entschieden (Nutzerauftrag 2026-08-04, PM-Loop Runde
„Lehrwerkgetriebener Katalog"), grundiert durch dieselbe Live-Prüfung wie T-01.

