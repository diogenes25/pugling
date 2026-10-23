---
tags: [typ/story, status/ausformuliert, bereich/doku]
aliases: [B-166 Rest, Kapitel in Tutorials, Cascade-Kommentar AdultLifecycleTests]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: doku
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-10-23-nacht.md (Nachschau zu Sprint 2)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-166]
wartet_auf: ""
---

# B-241 · B-166 hat die Kapitel-Ebene aus der Wissenskarte entfernt – in drei Tutorials und einem Plan steht sie noch, und ein Kommentar wurde dabei falsch

B-166s AK 2 („Fach → Kapitel → Übung steht in keiner lebenden Doku mehr") war nach dem Reviewer-Fund (elf
Kommentare, `head -8`) ein zweites Mal behauptet und ein zweites Mal nicht erfüllt: der Grep lief über
`chapters/|chapterId`, nicht über das deutsche Wort in den Tutorials, die die User Story ausdrücklich nennt.

## Ist-Stand am Code

**Ein Satz, der durch B-166 falsch wurde** – in Sprint 3 bereits korrigiert, hier als Beleg:
`backend/Pugling.Api.Tests/AdultLifecycleTests.cs:14-15` sagte nach dem Umschreiben „deleting the adult takes
their children, subjects and exercises with it". Wahr ist `SetNull` für `Subject.OwnerAdultId`
(`PuglingDbContext.cs:471-472`), `Exercise.AuthorAdultId` (`:512-516`), `TextbookSeries.OwnerAdultId`
(`:245-246`); `AdultsController.Delete` (`:118-152`) nimmt nur allein betreute Kinder und das Konto mit – so
heißt auch der Test (`Loeschen_Nimmt_Das_Allein_Betreute_Kind_Und_Das_Konto_Mit`). Das Wort „chapters" wurde
ersetzt, die falsche Prämisse (Katalog-Cascade) übernommen.

**Lebende Doku, die die Ebene noch nennt:**

| Datei:Zeile | Wortlaut | Richtig wäre |
| --- | --- | --- |
| `docs/tutorial-supervisor.md:340-341` | „Etappe auf einem Kapitel … ein Kapitel-Ziel" | Unit (Löschen einer Unit antwortet `exercise_in_use`, `SeriesUnitsController.cs:146-151`) |
| `docs/tutorial-student.md:31` | „Katalog (Fach → Kapitel → Übung `exerciseId 1`)" | Fach → Reihe/Unit → Übung |
| `docs/tutorial-student.md:320` | „Hierarchisch (Fach→Kapitel→Übung→Item)" | Fach→Unit→Übung→Item (`…/series-units/…`) |
| `docs/tutorial.md:20` | Creator-Zeile „Fächer, Kapitel, typisierte Übungen" | wie `rollen-doku.md:18` nach B-166 |
| `docs/vater-informationsarchitektur-plan.md:121-125` (und `:89/:94/:111/:112/:151`) | „Fach und Kapitel", Filter „Fach/Kapitel", „Neues Kapitel" | halb editierter Absatz – Zeile 120 wurde geändert, der Rest nicht; entweder ganz nachziehen oder als Plan-Historie kennzeichnen |
| `docs/uebergabe-vokabeln.md:59` | „Kapitel-Liste mit Verwendung + Löschen" | Unit-Liste (UI-Beschreibung; `:87-91` sind Tag-Beispiele und bleiben) |

**Nits aus derselben Nachschau:** `frontend/CLAUDE.md:82` nennt `?seriesId=&seriesUnitId=`, der Code reicht
auch `subjectId` durch (`createExerciseHref`, `VaterLehrwerke.tsx:100-106`); `docs/endpunkt-beziehungen.md:418`
verlinkt `Services/ChildLearnProgressService.cs`, die Datei liegt unter `Services/Student/` (tot seit
`57c724c`). `DraftRules.cs:50` und `CreatorBriefing.cs:133` sagen „Kapitel" in deutschen Laufzeit-Strings des
KI-Creators – gehört zu **B-237**.

## Die echte Lücke

Dieselbe wie in B-166s eigenem Review: ein Grep, der nicht alles fasst, gilt als Sweep. Zweimal im selben
Sprint (`head -8`, dann nur die englischen Routen-Wörter). Und einmal die Fehlerklasse B-112: ein Wort
ersetzt, die Behauptung drumherum nicht gelesen.

## Offene Punkte

1. `docs/vater-informationsarchitektur-plan.md`: Plan-Historie oder lebende Beschreibung? Die Datei
   beschreibt die Umstellung von 2026-08 in der Vergangenheitsform; `frontend/CLAUDE.md` verlinkt sie als
   Quelle der Informationsarchitektur. *Empfehlung: die Begriffe nachziehen, die Erzählung lassen.*
2. Beleg: der Grep aus B-166 AK 1 **plus** `Kapitel` über `docs/*.md wiki/*.md` mit der Ausschlussliste
   (Freitext-Kapitel, Unit-Labels, Zeitzeugen) → 0.

## Verlauf

- **2026-10-23** — angelegt aus der **Nachschau** zu Sprint 2 des Nachtlaufs (Commit `95b0624`),
  `entgangen_bei: B-166`. Der falsche Cascade-Kommentar ist im selben Lauf (Sprint 3) korrigiert – ein Satz,
  den ich selbst am selben Tag falsch geschrieben hatte; die sechs Doku-Stellen bleiben hier offen, weil sie
  eine eigene Grep-Runde mit Ausschlussliste brauchen und der Lauf nach Sprint 3 endet (Limit 2).
