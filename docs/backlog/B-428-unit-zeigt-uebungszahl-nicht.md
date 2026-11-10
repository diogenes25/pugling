---
tags: [typ/story, status/abgenommen, bereich/katalog, bereich/frontend, rolle/creator]
aliases: [Übungen je Unit, Unit ohne Zähler]
status: abgenommen
prio: P3
art: Wunsch
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-08-rollengang.md#feedback-creator
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-428 · Lehrwerke: eine Unit zeigt nicht, wie viele Übungen sie schon trägt

Im Rollengang vom 2026-11-08 klappte der Lehrer „Green Line 1" auf: drei Units, je „kein Stoff hinterlegt"
und „+ Übung". Ob Unit 1 schon drei Übungen hat und Unit 2 keine, steht nirgends – dafür muss er unter
Übungen Fach, Reihe und Unit filtern. Wer Material plant, sieht die Lücken im Katalog nicht.

## User Story

Als *Creator* möchte ich in der Unit-Liste je Unit sehen, **wie viele Übungen** sie trägt (und mit einem Klick
dorthin), damit ich erkenne, wo Stoff fehlt, ohne drei Filter zu setzen.

## Ist-Stand am Code

- `SeriesUnitResponse(Id, SeriesId, Grade, OrderIndex, Label, BookType, Topics, Grammar, VocabularyNotes,
  CreatedAt, GrammarTopicIds)` (`TextbookSeriesDtos.cs:51-53`) – kein Zähler.
- `SeriesUnitsController.List` (`:36-46`) materialisiert die Units und mappt sie; `Exercise.SeriesUnitId`
  ist die Pflicht-Beziehung (B-106), ein Count je Unit ist eine Zeile in der Projektion.
- `VaterLehrwerke.tsx:641` zeigt nur die Pille „kein Stoff hinterlegt", `:661` den Knopf „+ Übung"
  (`createExerciseHref`); die Übungsliste nimmt `?subjectId=&seriesId=&seriesUnitId=` (Hausregel „eine Auswahl
  reist als Query mit").

## Die echte Lücke

Ein additives Feld und eine Spalte.

## Entscheidungen

Getroffen vom PM in der Fable-Sitzung vom 2026-11-08 unter dem Auftrag des Nutzers, das Backlog
nachtlauffähig zu machen ([Protokoll](../pm-sitzung-2026-11-08-rollengang.md)). Vetorecht beim Nutzer.

1. **Additives Feld `ExerciseCount` auf `SeriesUnitResponse`**, als DB-Count in der Projektion (kein Laden der
   Übungen). Kosten: ein Feld im Vertrag (additiv), Capture neu.
2. **Spalte „Übungen" in der Unit-Tabelle** mit der Zahl als Link auf `/vater/exercises?subjectId=&seriesId=&seriesUnitId=`;
   `0` steht als Zahl, nicht als Pille – die Pille „kein Stoff hinterlegt" meint die Stoffnotizen, nicht die
   Übungen. Kosten: eine Spalte, ein Link.
3. **Zählung ohne Rollenfilter** (alle Übungen der Unit, auch fremde): die Unit ist geteilter Katalog, die Frage
   ist „ist die Unit versorgt", nicht „habe ich hier etwas". Kosten: keine.

## Akzeptanzkriterien

1. `GET creator/textbook-series/{s}/units` liefert je Unit `exerciseCount`; eine Unit mit drei Übungen → 3, eine
   leere → 0 (Integrationstest, Mutante: Count durch `0` ersetzen → rot).
2. Die Unit-Tabelle zeigt die Spalte „Übungen" mit der Zahl als Link; der Link öffnet die Übungsliste mit den
   drei Filtern gesetzt.
3. Zurückgezogene Übungen zählen mit (Zuweisbarkeit ist eine andere Frage, B-140).

## Schätzung

**Größe: S** — Projektion + Feld + Capture im Backend, Spalte + Test im Frontend. Kein Schema, kein
Vertragsbruch.

**Risiken:** `DocsCaptureTests` ändern sich; `VaterLehrwerke.test.tsx` hat Fixtures ohne das Feld (ergänzen).

**Angriffsplan:** (1) Projektion in `SeriesUnitsController.List`/`Get` + DTO; (2) Test in
`CatalogManagementTests` oder der Units-Testklasse; (3) `gen:contract`; (4) Spalte + Link, Vitest;
(5) Rollengang Creator als Fall in `lehrwerke.spec.ts` („Zahl steht, Link filtert").

**Testweg:** Integrationstest der Units-Liste, `VaterLehrwerke.test.tsx`, `lehrwerke.spec.ts`.

## Verlauf

- **2026-11-08** — angelegt aus dem Rollengang Creator ([Protokoll](../pm-sitzung-2026-11-08-rollengang.md)),
  Ist-Stand belegt, Entscheidungen vom PM unter dem Auftrag „nachtlauffähig machen" (Vetorecht beim Nutzer),
  geschätzt.
- **2026-11-08** — **Freigabe des Nutzers (Autonomer Modus, Opt-in):** die sieben vorgelegten Entscheidungen der Fable-Sitzung hat der Nutzer im Dialog bestätigt („Ja, so fahren“, Antwort auf die Frage nach den Entscheidungen für die erste Nacht, [Protokoll](../pm-sitzung-2026-11-08-rollengang.md)). Ein Nachtlauf darf diese Story auf Grundlage der `## Entscheidungen` bauen; trifft er auf eine Produktfrage, die sie nicht hergeben, hält er an.
- **2026-11-09** — gebaut (Lauf AG, Sprint 28); vor dem Bau die Freigabe-Zeile gelesen (`05700b6`, Dialog). **Rote Probe mit Zahl:** der
  neue Integrationstest gegen den Ausgangsstand → 1 von 1 rot an `KeyNotFoundException` (Aufbau samt Zurückziehen lief durch).
  Backend: `ExerciseCount` (gruppierter Count in der Liste, `CountAsync` in Get/PATCH, 0 beim Anlegen); Frontend: Spalte „Übungen"
  mit Link `exercisesOfUnitHref` (drei Filter + `shared=1`), Singular „1 Übung" im Linknamen. Capture `catalog.md` neu.
  **Belege je Fall** – `.claude/scripts/mutant-probe.py`, gemessen nach dem letzten Edit an `a14961d + Arbeitsbaum`, 2026-11-09 17:17 (MB1/MB2 im Gesamtlauf zuerst „kompiliert nicht" – ein Build-Aussetzer; einzeln nachgemessen: red):

  | Fall | Test | Mutante | gemessen |
  |---|---|---|---|
  | AK 1 Liste | `SeriesUnitExerciseCountTests.Units_NennenIhreUebungszahl` | MB1 Liste liest falschen Schlüssel (`SeriesUnitsController.cs:55`) | red, am Fall |
  | AK 1 Einzel-Unit | dieselbe | MB2 Get liefert 0 (`:65`) | red, am Fall |
  | PATCH-Antwort | dieselbe | MB3 (`:142`) | red, am Fall |
  | AK 3 zurückgezogene | dieselbe | MB4 nur `ExecutePublic` (`:51`) | red, am Fall |
  | Entscheidung 3 fremde Creator | dieselbe (zweites Konto liest) | MB7 Autorfilter (`:32`) | red, am Fall |
  | AK 2 Link | `e2e/creator-katalog-ueberblick.spec.ts` Fall 1 | ME1 Zahl ohne Link (`VaterLehrwerke.tsx:673`) | red, am Fall |
  | Review: fremde Übungen hinter dem Link | dieselbe, Fall „Fremde Übungen" | ME5 Link ohne `shared=1` (`:118`) | red, am Fall |

  Grundlinie nach dem Zurückspielen grün.
- **2026-11-09** — Review: `pugling-reviewer` 🟡 Entscheidung 3 (fremde Creator) ungetestet → zweites Konto im Test (**zählt**); 🟢
  Tutorial → `docs/tutorial-creator.md` zeigt `exerciseCount`. `frontend-reviewer` **🔴 die Zahl zählt alle Übungen, der Link öffnete
  „nur meine"** – „3", dahinter „Übungen (0)" → der Schalter „geteilte Übungen" lebt jetzt in der Adresse (`?shared=1`), der Link setzt
  ihn (**zählt**); 🟢 Singular im Linknamen behoben.
- **2026-11-09** — **nicht abgenommen: Lauf AG an Freigabe 3 beendet.** Volle Suite nach dem letzten Edit: Backend 1187/1187, Vitest 723/723, `tsc -b` sauber, Playwright **1 failed, 129 passed** – rot `e2e/typ-manifest-faellt-aus.spec.ts:40`: der Schalter „geteilte Übungen anderer Väter anzeigen" ändert beim Klick seinen Zustand nicht. Ursache (analysiert, nicht behoben): seit dem Review-Fix zu B-428 hängt der Schalter an der Adresse (`?shared=1`, `VaterExercises.tsx` `setFilter`); Fach-Wechsel und Klick gehen beide über `setSearchParams`, der zweite liest den Fach-Wert aus dem alten Render (veraltete Closure), und React Router rendert die Adress-Änderung verzögert – der kontrollierte Haken springt zurück. Reparaturweg: `setParams(prev => …)` (aktuelle Adresse statt Render-Werte). Das ist Fund 6 in Sprint 28 und überschreitet den Zähler (5); darum gestoppt, nicht repariert. Der Rest der Story ist gebaut und belegt (Tabelle oben); die Abnahme gehört dem Nutzer bzw. dem nächsten Lauf nach der Reparatur.
- **2026-11-10** — abgenommen nach der Reparatur von [B-450](B-450-schalter-geteilte-uebungen-springt-zurueck.md) (im Dialog). Backend 1187/1187, Vitest 723/723, `tsc -b` sauber, Playwright **131 passed** (ganze Suite, Server vom Lauf nach dem letzten Edit gestartet). **Rollengang** Creator = `e2e/creator-katalog-ueberblick.spec.ts` (sechs Fälle, grün); Vater/Sohn als Regressionszeugen. Commit: siehe `git log -- docs/backlog/B-428-unit-zeigt-uebungszahl-nicht.md`.
