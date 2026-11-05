---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/creator]
aliases: [Übersetzung prüft VocabularyId nicht]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-05.md#lauf-u
unverifiziert: false
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-11-05"
---

# B-386 · Eine Übersetzungsübung speichert eine unbekannte `vocabularyId` ohne Prüfung

Der `TranslationController` prüft `TranslationItem.VocabularyId` beim Anlegen und Ändern nicht
(`backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs`, `ValidateConfigAsync` der Übersetzung und
`NormalizeConfigAsync`, etwa `:420-436`): eine Id, die es nicht gibt, wird still gespeichert – schlimmer als jeder der beiden
Fehlercodes, um die es in B-329 ging. Zu prüfen beim Ausformulieren: was das Spielen einer solchen Karte liefert, und ob die
Prüfung `invalid_reference` meldet wie die Vokabelübung.

## User Story

Als **Creator** möchte ich, dass eine Übersetzungsübung mit einer Vokabel-Id, die es nicht gibt, abgewiesen wird – und dass ich
eine Vokabel, die eine Übersetzung noch benutzt, nicht aus dem Speicher löschen kann. Sonst zeigt der Link `_self` ins Leere und
das Paar landet nie im Speicher.

## Ist-Stand am Code (Stand `6c00986`)

- `backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs:421-425`: die Übersetzung prüft nur die Sprachcodes. Eine
  gesetzte `vocabularyId` überspringt `NormalizeConfigAsync` (`:430`, nur `is null` wird angelegt), egal ob es sie gibt oder ob sie
  `0` ist.
- **Was das Spielen liefert:** nichts Falsches – `TranslationExerciseType.ItemsOf` (`Exercises/BuiltInExerciseTypes.cs:133-137`)
  liest nur `Source`/`Target`. Der Schaden liegt im Speicher: `_self` zeigt auf eine fehlende Vokabel, und das Paar wird nie
  angelegt.
- **Zweite Tür zum selben Zustand:** `VocabularyStoreController.ReferencingExercisesAsync` (`:411-440`) kennt nur Vokabelübungen
  (Item-Tabelle) und Lückentexte (`VocabKey`), **keine Übersetzungen**. Eine Vokabel, an der eine Übersetzung hängt, lässt sich
  löschen – und hinterlässt dieselbe tote Id.
- Das Frontend schickt beim Bearbeiten die geladene `vocabularyId` zurück, solange Quelle und Ziel unverändert sind
  (`frontend/src/vater/exerciseConfig.tsx:294`).

## Entscheidungen

1. **(Defekt, im Lauf gegrillt)** Die Übersetzung prüft wie die Vokabelübung (`ExerciseControllers.cs:59-67`): `vocabularyId ≤ 0`
   → `validation_error`, unbekannt → `invalid_reference` (B-329).
2. **(Defekt, im Lauf gegrillt)** Übersetzungen zählen als Verwendung: `Usage` listet sie, das Löschen verweigert mit
   `vocabulary_in_use`. **Grund:** ohne das macht Entscheidung 1 aus einem erlaubten Löschen eine Übung, die sich nicht mehr
   speichern lässt – das Frontend schickt die tote Id unverändert zurück (`exerciseConfig.tsx:294`) und bekäme `400`.
3. **Nicht** Teil der Story: ob Quelle/Ziel zur verlinkten Vokabel passen. Das ist eine andere Regel (wer eine Id setzt, behauptet
   die Bindung); dafür gibt es heute keine Meldung und keinen Fund.

## Akzeptanzkriterien

1. Anlegen und Ändern einer Übersetzung mit unbekannter `vocabularyId` → `400 invalid_reference`; mit `0` → `400 validation_error`.
2. Eine bestehende `vocabularyId` speichert weiter (Gegenprobe).
3. `GET …/vocabulary/{id}/usage` listet eine Übersetzung, die die Vokabel verlinkt; `DELETE` → `409 vocabulary_in_use`.

## Schätzung

`S`, `wo: backend`, keine Migration, kein Vertragsbruch (nur neue Ablehnungen eines Zustands, den kein Client absichtlich erzeugt).
Testweg: Integrationsfälle in `UnknownReferenceCodeTests` (AK 1/2) und neben den bestehenden Lösch-Schutz-Tests (AK 3).

## Verlauf

- **2026-11-05** — angelegt aus dem Review zu B-329 (Lauf U, Sprint 4, `pugling-reviewer`, 🟢); außerhalb des Diffs, darum
  eigene Story.
- **2026-11-05** — ausformuliert, gegrillt und geschätzt in Lauf V, Sprint 6 (`art: Defekt` – der Lauf darf selbst grillen). Am Code
  gefunden: die zweite Tür über das Löschen im Speicher, ohne die Entscheidung 1 das Bearbeiten bricht.
- **2026-11-05** — gebaut in Lauf V, Sprint 6. AK 1 → `UnknownReferenceCodeTests.cs:119` (unbekannt), `:120` (`0`), Ersetzen per PUT
  in derselben Methode; AK 2 → die bestehende Id als Gegenprobe (`201`); AK 3 → `CatalogReadDeleteTests.Store_Vokabel_An_Einer_Uebersetzung_Ist_Benutzt`
  (Usage `:160`, `409`, und eine fremde Vokabel, deren Id im Satztext steht, bleibt löschbar `:167`). Rote Proben: unbekannte Id
  ungeprüft → rot in `:119`; `≤ 0` ungeprüft → rot in `:120`; Usage ohne Übersetzungen → rot in `:160`; Vorfilter allein entscheidet
  → rot in `:167`; je 1 von 16 im gefilterten Lauf. Zwei Proben kompilierten im ersten Anlauf nicht (`CS0162`) und liefen gegen
  die vorige Assembly – verworfen und mit kompilierbaren Mutanten wiederholt. Volle Suite 1087/1087.
- **2026-11-05** — Review `pugling-reviewer`: kein 🔴, keine Verhaltens-Lücke. 🟡 Vertragskommentar `VocabUsage` nannte keine
  Übersetzungen (nur Text, ohne Zählung – **Zähler Sprint 6: 0 von 5**), dazu 🟢 DELETE-Summary und `docs/uebergabe-vokabeln.md`
  nachgezogen. **Bekanntes Verhalten, kein Defekt:** eine Vokabel, die eine Übersetzung angelegt oder wiederverwendet hat, ist
  danach löschgeschützt, bis Satz oder Übung sich ändern – wie bei der Vokabelübung; `usage` nennt die sperrende Übung.
- **2026-11-05** — **abgenommen.** Volle Suite 1087/1087; Rollengang Creator: die sechs E2E-Specs, die Übersetzung und Vokabelspeicher
  im Browser fahren, 8 passed – davon legt `uebungstypen.spec.ts:105` eine Übersetzung an. **Berichtigt:** zuerst stand hier,
  `auch-richtig-im-uebungseditor.spec.ts` speichere eine Übersetzung mit geladener Id erneut; die Spec bearbeitet aber eine
  Vokabelübung. Das erneute Speichern mit bestehender Id deckt nur der Integrationsfall (Anlegen mit bestehender Id → `201`, derselbe
  Validator wie beim PUT), keine Spec im Browser. Vater und Sohn unberührt (Spielweg liest nur `Source`/`Target`). Commit: siehe nächster Eintrag.
- **2026-11-05** — Commit `28cb072`.
- **2026-11-05** — **Nachschau** (Lauf W, Retro Sprint 8; frischer `pugling-reviewer`, `git show 28cb072`, Stand per
  `git archive` gebaut: 1087/1087). **Entgangen (🟢):** AK 1 „Ändern mit `0`" trägt kein Test – der `PUT` ist nur mit unbekannter
  Id belegt → [B-393](B-393-uebersetzung-put-id-null-ohne-test.md). Daneben, **älter** als dieses Increment (🟡): der `PUT` legt neue
  Paare nie im Speicher an, weil `Update` `NormalizeConfigAsync` nicht ruft → [B-392](B-392-uebersetzung-put-legt-paare-nicht-an.md).
  Der Satz oben „löschgeschützt, bis Satz oder Übung sich ändern" verschweigt darum: ein bearbeitetes Paar bleibt danach
  unverlinkt. 🟢 `docs/uebergabe-vokabeln.md` nannte `learn/…/usage` statt `creator/…/usage` – berichtigt (nur Text).
