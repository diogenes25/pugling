---
tags: [typ/story, status/abgenommen, bereich/medien, bereich/training, rolle/student]
aliases: [Bildwahl einfrieren, Fund 1]
status: abgenommen
prio: P1
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: ja
quelle: docs/backlog-vokabellernen.md#fund-1--defekt-der-abschlusstest-friert-bildwahlen-ein-die-er-nie-zeigt
---

# B-01 · Abschlusstest friert Bildwahlen ein, die er nie zeigt

**Wirkt heute.** Jeder Abschlusstestlauf schreibt die Motivwahl des Kindes fest — für Bilder, die der Test
selbst nicht rendert. Damit entscheidet der Test still darüber, welches Bild das Kind später in der
Übungsschleife sieht.

## User Story

Als Vater möchte ich, dass ein Abschlusstest die Bildzuordnung meines Sohnes nicht verändert, damit die
Bildkonstanz den Merkeffekt trägt und nicht ein Nebeneffekt des Tests.

## Ist-Stand am Code · Entscheidungen

→ Grill-Protokoll vom 2026-07-30, Abschnitt **„Fund 1"** und **Entscheidung 3**:
[backlog-vokabellernen.md](../backlog-vokabellernen.md#fund-1--defekt-der-abschlusstest-friert-bildwahlen-ein-die-er-nie-zeigt).

Kern, belegt: `MediaSelector.SelectForItemsAsync` schreibt die Wahl fest (`MediaSelector.cs:79-90`,
`AddRange` + `SaveFreezeAsync` → `SaveChangesAsync`) und entfernt dabei überholte Wahlen
(`context.Superseded`). `PositionTestsController` reicht `childId` durch, obwohl `SohnTest.tsx` nur
`audioUrl` liest.

## Akzeptanzkriterien

1. `childId` fliegt aus dem Test-Pfad (vier `ItemsOfAsync`-Aufrufe in `PositionTestsController`).
2. `imageUrl`/`imageAlt` fliegen aus `TestItem` und dem Contract-Record — ein Feld, das immer `null` ist,
   ist genau die stille Lüge, gegen die das Projekt sonst mit `unknown_field` kämpft.
3. Ein Testlauf verändert keine `ChildMediaPick`-Zeile mehr.
4. Eine Batch-Abfrage weniger je Testfrage.

## Schätzung

**Größe: S** — eine Durchreichung entfernen, zwei Vertragsfelder streichen.

- **`vertragsbruch: ja`** — `TestItem` verliert zwei Felder; `Pugling.Contracts`, `Pugling.Client` und die
  Frontend-Typen ziehen nach.
- **`migration: nein`** — kein Schema betroffen.
- **Risiko:** `SohnTest.tsx` darf die Felder nicht doch irgendwo lesen (geprüft: liest nur `audioUrl`);
  die Vorschau-Pfade des Vaters gehen über `ExercisePreview`, nicht über `TestItem`.
- **Testweg:** Regressionstest, der nach einem Testlauf die Unverändertheit der `ChildMediaPick`-Zeilen
  prüft (`Pugling.Api.Tests`, bei den Medien-Tests); dazu `/smoke-test` für die HTTP-Schicht.

## Verlauf

- **2026-07-30** — geerntet aus dem Grill-Protokoll vom selben Tag; Stufe `geschaetzt` übernommen, weil
  dort schon gegrillt und mit Größe versehen.
- **2026-08-02** — umgesetzt und abgenommen. Die Schätzung hat getragen: vier Durchreichungen entfernt,
  zwei Vertragsfelder gestrichen, alle vier Akzeptanzkriterien erfüllt.

  **Erst rot.** Der Regressionstest `Abschlusstest_SchreibtKeineBildwahlFest` (in `MediaSelectionTests`,
  wie im Testweg vorgesehen) fiel vor der Reparatur mit
  `Assert.Empty() Failure: Collection was not empty · [ChildMediaPick { ChildId = 3, Id = 1, … }]` —
  ein Testlauf schrieb also belegbar eine Bildwahl fest.

  **Ein Fund über die Schätzung hinaus:** `pos.StudyPlan` wurde ausschließlich für dieses `childId`
  gebraucht, also fiel auch das `Include(p => p.StudyPlan)` in `GetPosition` weg — ein Join weniger auf
  allen vier Test-Endpunkten. Geprüft, dass kein Dienst am `PlanPosition` hängend darauf zugreift: die
  Auswertungen nehmen `plan.ChildId` aus `GetPlan`, und `PositionProgressService` lädt seinen eigenen
  `Include`.

  Der Grund für den Defekt steht jetzt dort, wo er wirkt: Am `childId`-Parameter von
  `PositionPlayService.ItemsOfAsync` erklärt der Kommentar, dass die Auswahl **einfriert** — dass sie also
  nicht bloß eine Abfrage kostet, sondern entscheidet. Vorher stand da nur „braucht kein Bild".

  Belege: **638 Tests** grün (`dotnet test Pugling.sln -c Release`), `/smoke-test` 13 von 13 grün (inkl.
  Positions-Test starten und einreichen), `dotnet format --verify-no-changes` sauber, `tsc --noEmit` im
  Frontend sauber nach neu erzeugtem `contract.ts`. `SohnTest.tsx` liest die gestrichenen Felder
  nachweislich nicht (`grep imageUrl|imageAlt` leer) — das benannte Risiko der Schätzung hielt.
  Der Review lief in dieser Sitzung **von Hand statt über `pugling-reviewer`** (Agenten waren
  abgeschaltet); geprüft wurden gezielt die zwei Stellen, an denen dieser Umbau schiefgehen konnte: die
  entfallene Navigation und der Frontend-Lesezugriff.

  Commits: `e8cbe47` (Reparatur samt Regressionstest und neu erzeugtem Vertragsdokument), dazu dieser
  Nachtrag mit der Abnahme.
