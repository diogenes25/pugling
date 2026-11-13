---
tags: [typ/story, status/in-arbeit, bereich/backend, rolle/creator]
aliases: [Fach löschen macht Reihe fachlos]
status: in-arbeit
prio: P3
art: Frage
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-05.md#lauf-z
grund: ""
ersetzt_durch: []
---

# B-404 · Ein gelöschtes Fach lässt Reihen mit Übungen fachlos zurück

`TextbookSeries.Subject` ist `OnDelete(SetNull)` (`PuglingDbContext.cs` um `:250`), und `SubjectsController.Delete` sperrt nur bei
Schlüsselergebnissen und Stundenplan-Einträgen (B-144). Ein gelöschtes Fach erzeugt damit genau den Zustand, den
[B-401](B-401-fach-leeren-laesst-uebungen-ohne-fach.md) beim Leeren verweigert: eine Reihe ohne Fach, die Übungen trägt. Die E2E
„Ein gelöschtes Fach bleibt als Freitext sichtbar…" in `lehrwerk-bearbeiten.spec.ts` erzeugt ihn sogar absichtlich.

**Frage:** soll das Löschen eines Fachs mit 409 verweigert werden, solange eine Reihe dieses Fachs Übungen trägt – oder ist „Fach weg,
Name bleibt als Freitext" (B-142) der gewollte Rückweg, und die Sperre aus B-106 T-01 gilt nur fürs Anlegen?

## User Story

Als **Creator** möchte ich, dass das Löschen eines Fachs keine Reihe zurücklässt, deren Übungen an keinem Fach mehr hängen,
damit der Katalog nicht still in einen Zustand gerät, den jeder andere Weg (Reihe anlegen, Fach leeren) verweigert.

## Ist-Stand am Code

- **Die Sperre beim Löschen kennt nur Kind-Daten:**
  [SubjectsController.cs:150-153](../../backend/Pugling.Api/Controllers/Creator/SubjectsController.cs#L150) prüft
  `KeyResults` und `TimetableEntries`, sonst `204`.
- **Die Reihe verliert die Fach-Id still:** [PuglingDbContext.cs:251-252](../../backend/Pugling.Api/Data/PuglingDbContext.cs#L251)
  (`TextbookSeries.Subject` → `SetNull`). Mit dem Fach gehen per Kaskade seine Arten (`ExerciseCategory`,
  [PuglingDbContext.cs:532-535](../../backend/Pugling.Api/Data/PuglingDbContext.cs#L532)) und Grammatik-Themen
  ([PuglingDbContext.cs:419](../../backend/Pugling.Api/Data/PuglingDbContext.cs#L419)); die Übung verliert ihre Art per `SetNull`
  ([PuglingDbContext.cs:544-548](../../backend/Pugling.Api/Data/PuglingDbContext.cs#L544)).
- **Jeder andere Weg verweigert den Zustand:** Leeren des Fachs einer Reihe mit Übungen ist seit B-401 gesperrt
  ([TextbookSeriesController.cs:189-198](../../backend/Pugling.Api/Controllers/Creator/TextbookSeriesController.cs#L189),
  „a series without a subject may not host exercises"), und eine Übung in einer fachlosen Reihe lässt sich gar nicht anlegen
  ([ExerciseControllerBase.cs:121-127](../../backend/Pugling.Api/Controllers/Creator/ExerciseControllerBase.cs#L121), B-106 T-01).
- **Die E2E erzeugt den Freitext-Zustand mit einer Reihe ohne Übungen:**
  [lehrwerk-bearbeiten.spec.ts:109](../../frontend/e2e/lehrwerk-bearbeiten.spec.ts#L109) legt Fach und Reihe an und löscht das Fach,
  ohne eine Übung anzulegen. Sie bleibt von einer Sperre „Reihe mit Übungen" unberührt.
- **Tests:** [FachLoeschenSperreTests.cs](../../backend/Pugling.Api.Tests/FachLoeschenSperreTests.cs) hält Meilenstein (`:26`),
  Stundenplan (`:71`) und „nur eine Reihe bleibt löschbar" (`:88`); eine Reihe **mit Übungen** prüft keiner.

## Die echte Lücke

Die Regel „eine Reihe ohne Fach trägt keine Übung" gilt an zwei Türen (Anlegen, Leeren), nicht an der dritten (Fach löschen).
Dazu verliert jede Übung beim Löschen ihre Art und ihre Grammatik-Themen ohne Rückfrage.

## Offene Punkte

1. ~~Sperre beim Löschen, solange eine Reihe des Fachs Übungen trägt, oder ist der Freitext-Rückweg (B-142) gewollt?~~ → Entscheidung 1
2. ~~Bricht die Sperre die bewusst gebaute E2E?~~ → Entscheidung 2
3. ~~Zählen auch Units mit Grammatik-Themen oder Übungen fremder Reihen mit einer Art des Fachs?~~ → Entscheidung 3

## Entscheidungen

Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13, [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).

1. **Löschen wird mit `409 subject_in_use` verweigert, solange eine Reihe dieses Fachs Übungen trägt.** Begründung: dieselbe Regel
   wie beim Leeren (B-401) und beim Anlegen (B-106 T-01); eine dritte Tür, die sie umgeht, ist ein stiller Datenverlust (Arten und
   Themen der Übungen gehen per Kaskade mit). Der Ausweg ist derselbe wie bei B-401: Reihe auf ein anderes Fach stellen oder ihre
   Übungen entfernen. *Kosten:* ein Fach mit benutzter Reihe lässt sich erst nach dem Umhängen löschen; das ist gewollt.
2. **Der Freitext-Rückweg (B-142/B-143) bleibt für Reihen ohne Übungen.** Die E2E `lehrwerk-bearbeiten.spec.ts:109` bleibt
   unverändert grün, weil ihre Reihe keine Übung trägt. *Kosten:* keine.
3. **Gezählt werden nur Übungen in Reihen des Fachs** (Kriterium `Exercise.SeriesUnit.Series.SubjectId == subjectId`). Units mit
   Grammatik-Themen ohne Übungen und Übungen fremder Reihen, die eine Art dieses Fachs tragen, sperren nicht. Begründung: klein
   halten; beides ist Katalog-Information ohne Kind-Bezug. *Kosten:* bekannte Grenze – diese Zuordnungen gehen beim Löschen
   weiterhin still verloren.

## Akzeptanzkriterien

1. `DELETE /api/v1/creator/subjects/{id}` auf ein Fach, an dem eine Reihe mit mindestens einer Übung hängt, antwortet
   `409 subject_in_use`; Fach, Reihe und Übung (samt Art) sind danach unverändert.
2. Ein Fach, an dem nur eine Reihe **ohne** Übungen hängt, bleibt löschbar (`204`), die Reihe behält den Fachnamen als Freitext
   (bestehender Fall `Fach_MitNurEinerReihe_BleibtLoeschbar`, E2E `lehrwerk-bearbeiten.spec.ts:109`).
3. Die Meldung nennt die Art der Nutzung ohne Ausweg, den es nicht gibt (B-414-Muster).

## Schätzung

**Größe: XS** – eine weitere `AnyAsync`-Bedingung neben `:150-151` und ein Test. `migration: nein` (keine Schemaänderung),
`vertragsbruch: nein` (bestehender Fehlercode, kein DTO-Feld).

**Risiken**

- Die Reihenfolge Eigentum vor Nutzung (`SubjectsController.cs:141-146`) muss bleiben; die neue Bedingung kommt in denselben
  Nutzungsblock.
- Der `<summary>` am `Delete` warnt ausdrücklich davor, die Regel als „Kind-Daten sperren" zu formulieren
  (`SubjectsController.cs:123-125`). Er wird um den Katalog-Fall ergänzt, nicht umgedeutet.

**Angriffsplan (Backend zuerst)**

1. Roter Test in `FachLoeschenSperreTests.cs`, dann die Bedingung in `SubjectsController.Delete`, `<summary>` nachziehen.
2. Frontend: keine Änderung; die Katalog-Seite zeigt den 409 über den vorhandenen `StatusBanner`.

**Testweg**

- `backend/Pugling.Api.Tests/FachLoeschenSperreTests.cs`, neuer Fall `Fach_MitReiheDieUebungenTraegt_LaesstSichNichtLoeschen`
  (AK 1). Rot wird er, wenn die neue `AnyAsync`-Bedingung fehlt oder die Reihe statt über `SeriesUnit.Series` gar nicht geprüft
  wird. Er prüft nach dem 409 zusätzlich, dass die Art der Übung noch dran ist – rot, wenn die Prüfung erst nach `Remove` läuft.
- AK 2: `Fach_MitNurEinerReihe_BleibtLoeschbar` (`:88`) und die E2E `lehrwerk-bearbeiten.spec.ts:109` – rot, wenn die Bedingung
  jede Reihe statt nur Reihen mit Übungen zählt.

## Verlauf

- **2026-11-05** — angelegt aus dem Review zu B-401 (Lauf Z, Sprint 15; `pugling-reviewer`, 🟡). Als `Frage`, nicht im Lauf entschieden:
  die Antwort widerspricht einer bestehenden, absichtlich gebauten E2E und ist darum eine Produktentscheidung, kein Herstellen.
- **2026-11-13** — **ausformuliert** (Backlog-Lauf): Ist-Stand am Code belegt; die E2E trägt keine Übung und widerspricht der Sperre
  nicht.
- **2026-11-13** — **gegrillt**: Entscheidungen 1–3. Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13,
  [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).
- **2026-11-13** — **geschaetzt**: XS, backend, keine Migration, kein Vertragsbruch.
- **2026-11-13** — `in-arbeit`: `SubjectsController.Delete` verweigert mit `409 subject_in_use`, solange eine Reihe des Fachs Übungen trägt (Kriterium `Exercise.SeriesUnit.Series.SubjectId`, nach der Eigentumsprüfung, vor `Remove`); die Meldung nennt die Reihe mit Übungen und die zwei Auswege (Reihe umhängen, Übungen entfernen); `<summary>` um den Katalog-Fall samt bekannter Grenze (Entscheidung 3) ergänzt. Test `FachLoeschenSperreTests.Fach_MitReiheDieUebungenTraegt_LaesstSichNichtLoeschen` (AK 1, 3; prüft danach Fach, Fach der Reihe und Art der Übung). AK 2 hält der bestehende Fall `Fach_MitNurEinerReihe_BleibtLoeschbar` (Absichtserklärung, im Ausgangszustand grün; rot unter B404-AK2). Rote Probe 3/3 rot.
  Rote Probe (Gemessen an `afadf18d + Arbeitsbaum geändert`, 2026-11-13 22:15, `mutant-probe.py`, Grundlinie nach dem Zurückspielen grün):

  | Mutante | Stelle | erwartet | gemessen | Anmerkung |
  |---|---|---|---|---|
  | B404-AK1 | backend/Pugling.Api/Controllers/Creator/SubjectsController.cs:165 | red | red |  |
  | B404-AK2 | backend/Pugling.Api/Controllers/Creator/SubjectsController.cs:165 | red | red |  |
  | B404-AK3 | backend/Pugling.Api/Controllers/Creator/SubjectsController.cs:167 | red | red |  |
- **2026-11-13** — Review-Fund (`pugling-reviewer`) behoben: die 409-Meldung nannte zwei Auswege, die selbst gesperrt sein können (Reihe umhängen → `series_subject_in_use`/nur Besitzer; Übungen entfernen → `exercise_in_use`/`position_has_data`). Sie nennt jetzt nur die Sperre („This subject carries a textbook series with exercises.“, wie B-401); der Test prüft zusätzlich, dass kein Ausweg mit „first“ genannt wird. `old-phrases.py`: keine Fundstelle erwartet den alten Text. Rote Probe 4/4 rot.
  Rote Probe nach den Review-Fixes (Gemessen an `afadf18d + Arbeitsbaum geändert`, 2026-11-13 22:33, `mutant-probe.py`, Grundlinie nach dem Zurückspielen grün):

  | Mutante | Stelle | erwartet | gemessen | Anmerkung |
  |---|---|---|---|---|
  | B404-AK1 | backend/Pugling.Api/Controllers/Creator/SubjectsController.cs:165 | red | red |  |
  | B404-AK2 | backend/Pugling.Api/Controllers/Creator/SubjectsController.cs:165 | red | red |  |
  | B404-AK3 | backend/Pugling.Api/Controllers/Creator/SubjectsController.cs:168 | red | red |  |
  | B404-AK3-way-out | backend/Pugling.Api/Controllers/Creator/SubjectsController.cs:168 | red | red |  |
