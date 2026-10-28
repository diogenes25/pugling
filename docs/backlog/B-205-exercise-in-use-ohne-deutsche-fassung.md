---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/vertrag]
aliases: [exercise_in_use englisch, dritter Geschwisterfall, GERMAN_PROBLEM_TEXT unvollstaendig]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: beides
migration: nein
vertragsbruch: ja
quelle: frontend-reviewer 2026-09-02 zu B-204
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-205 · `exercise_in_use` fehlt in `GERMAN_PROBLEM_TEXT` — die zwei Geschwister stehen drin

Der Vater sieht beim Löschversuch einer benutzten Reihe den englischen Servertext („Content in this series
is still used in a study plan, a class test or an objective milestone; remove it there first."), weil
`exercise_in_use` keine Zeile in `GERMAN_PROBLEM_TEXT` hat (`frontend/src/lib/api.ts:80 ff.`).

Der `frontend-reviewer` hat es beim Review zu [B-204](B-204-vierter-weg-in-den-doppelten-meldungsplatz.md)
gefunden und begründet, warum das kein bewusster Ausschluss ist: Der Code erfüllt beide Hälften des
Aufnahmetests, den die Abbildung sich selbst gibt — der Creator sieht ihn an einem Knopf, den er gedrückt
hat, und kann etwas dagegen tun. Genau darum stehen die zwei Geschwister `subject_in_use` und
`publisher_in_use` bereits drin. Es ist also der dritte Fall derselben Familie, nicht eine i18n-Frage.

**Kopplung, die beim Bauen zählt:** `frontend/e2e/creator-lehrwerk-weg.spec.ts` sichert heute den
**englischen** Text zu (Schritt 6a). Kommt die deutsche Zeile, muss diese Zusicherung mitwandern — der Satz
steht als Kommentar in der Spec.

## User Story

Als Vater, der eine benutzte Reihe, Unit oder Übung löschen will oder die Wörter einer gespielten Übung umsortiert,
möchte ich einen deutschen Satz lesen, der zu dem passt, was ich getan habe.

## Ist-Stand am Code

Siehe den Verlauf vom 2026-10-23: `exercise_in_use` an vier Stellen, eine davon (`ExerciseControllers.cs:269-273`,
Wörter verschieben/entfernen, drei Aufrufer `:187`, `:231`, `:254`) mit anderer Regel; keine Zeile in
`GERMAN_PROBLEM_TEXT` (`frontend/src/lib/api.ts`).

## Akzeptanzkriterien

1. Die Wörter-Sperre antwortet mit einem eigenen Code `exercise_items_locked` – an allen drei Stellen.
2. Die drei Löschfälle behalten `exercise_in_use`.
3. Beide Codes haben eine deutsche Fassung; `creator-lehrwerk-weg.spec.ts` sichert die deutsche zu.

## Entscheidungen

1. Aufspalten (Entscheidung des Nutzers, 2026-10-23). Kosten: kleiner Vertragsbruch für den einen Pfad.
2. Der feste deutsche Text für `exercise_in_use` verliert die Zählung, die der Server beim Löschen einer Übung
   dynamisch nennt (eigene gegen unsichtbare Verwendungen). Er nennt darum die unsichtbaren Verwendungen als Möglichkeit.
   Kosten: im Fall „nur unsichtbare Verwendungen sperren“ ist der Satz unschärfer als der Server.
3. Die drei Item-Routen deklarieren jetzt `409` (Review-Hinweis). Kosten: keine.

## Schätzung

S, `beides`, keine Migration, Vertragsbruch ja (ein Code an einem Pfad). Testweg: `ExerciseItemsAndProgressTests`,
`errorMessage.test.ts`, `creator-lehrwerk-weg.spec.ts`.

## Verlauf

- 2026-09-02 · Aufgenommen aus dem `frontend-reviewer`-Lauf zu B-204. `unverifiziert: true` — der Fundort
  ist belegt, aber ob die Abbildung wirklich alle Geschwister aufnehmen *soll* (statt die Familie anders zu
  schneiden), ist beim Ausformulieren gegen den Code zu prüfen.
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): besteht, **aber anders geschnitten** – `exercise_in_use` steht an vier Stellen mit verschiedener Bedeutung: `ExerciseControllerBase.cs:436` (dynamischer Text über unsichtbare Verwendungen), `ExerciseControllers.cs:269-271` (Items verschieben gesperrt – andere Regel), `SeriesUnitsController.cs:149` (Unit löschen), `TextbookSeriesController.cs:271` (Reihe löschen). Ein fester deutscher Text je Code überdeckte den dynamischen Text und sagte bei `:270` das Falsche. Designwahl: Code aufspalten (Vertragsbruch) oder je Kontext übersetzen.
- **2026-10-23** — **Entscheidung des Nutzers** in der Grill-Runde G-4 der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): der Code wird **aufgespalten**. Die abweichende Regel („Items verschieben gesperrt“, `ExerciseControllers.cs:269-271`) bekommt einen eigenen Code, die Löschfälle behalten `exercise_in_use`, und jeder Code bekommt eine deutsche Fassung in `GERMAN_PROBLEM_TEXT`. Für die eine Stelle, die den Code wechselt, ist das ein kleiner Vertragsbruch. Die Zusicherung in `creator-lehrwerk-weg.spec.ts` wandert mit. Damit ist die Designwahl getroffen, und die Story ist als `Defekt` autonom grill- und baubar (Paket NP-17).
- **2026-10-28** — in Lauf H, Teil 2 ([Protokoll](../pm-sitzung-2026-10-28-nacht.md)) ausformuliert, geschätzt (S) und gebaut. **Gemessen:** die Code-Zusicherung
  in `ItemMutation_BeiInPlanUebung_BlocktIndexVerschiebung_ErlaubtAnhaengen` → **1 failed** („exercise_in_use“) vor dem
  Fix; die zwei neuen Fälle in `errorMessage.test.ts` → **2 failed** vor dem Eintrag. **Review-Fund 🟡 (gezählt):** der
  dritte Sperrpfad (Einfügen mit `orderIndex`) war ungetestet → dritte Anfrage im Fall, Mutante „alter Code nur dort“ →
  **1 failed**, Kontrolle grün. 🟢 genommen: `ProducesResponseType(409)` an den drei Item-Routen, Code im Wiki
  (`03-uebungstypen.md`, `07-api-referenz.md`) und in `endpunkt-beziehungen.md`. Vertrag (`docs/openapi/v1.json`) und
  `contract.ts` neu erzeugt. Backend **1000/1000**, Vitest **555/555**. Rollengang: die volle E2E-Suite gegen frisch gestartete Server (**99 passed**, `exit=0`, nach der letzten Änderung gestartet); Browser-Extension nicht genutzt. Die E2E-Spec
  `creator-lehrwerk-weg.spec.ts` fährt den Löschweg mit der deutschen Zeile. **`abgenommen`.**
- **2026-10-28** — Commit `f96d358` (Lauf H, Teil 2).
