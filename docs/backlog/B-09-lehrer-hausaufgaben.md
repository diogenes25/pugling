---
tags: [typ/story, status/geschaetzt, bereich/katalog, bereich/auth, rolle/creator, rolle/supervisor]
aliases: [Lehrer-Hausaufgaben, Klassen, Beitrittscode]
status: geschaetzt
prio: P3
art: Wunsch
groesse: L
wo: beides
migration: ja
vertragsbruch: nein
quelle: docs/lehrer-konto-plan.md
---

# B-09 · Lehrer verteilt Hausaufgaben: Klassen, Beitrittscode, Ownership-Umkehr

**Sammel-Story, keine Kopie.** Der Entwurf liegt in [lehrer-konto-plan.md](../lehrer-konto-plan.md),
Abschnitt „Offen", und im dort referenzierten älteren Hausaufgaben-Entwurf.

## User Story

Als Lehrer möchte ich einer Klasse Aufgaben mit Fälligkeit zuweisen, damit meine Schüler zu Hause
verbindlich üben, ohne dass ich für jedes Kind einen Betreuungsauftrag brauche.

## Ist-Stand (am 2026-07-30 geprüft)

Die **Identität** ist gebaut, aber **anders als der Entwurf sagte**: statt einer `Teacher`-Entität gibt es
das Creator-only-Konto auf der `Adult`-Zeile (siehe `CLAUDE.md`, „`Adult` statt `Father`"). Offen sind
darum genau die Teile, die darüber hinausgehen: **Klassen**, **Beitrittscode**, **Ownership-Umkehr** und
**Fälligkeit** (`DueDate` gibt es nicht).

## Entscheidungen

→ [lehrer-konto-plan.md](../lehrer-konto-plan.md) und der dort referenzierte ältere Hausaufgaben-Entwurf
(2026-07-05 ausgearbeitet und abgesegnet, Design + 6 Etappen).

**Eine Entscheidung ist seither überholt** und gehört beim Ausformulieren neu gestellt: Der Entwurf sah eine
`Teacher`-Entität vor; gebaut wurde stattdessen das Creator-only-Konto auf der `Adult`-Zeile. Alles, was der
Entwurf an `Teacher` hängt, muss auf `Adult` umgedacht werden.

## Akzeptanzkriterien

1. Ein Lehrer-Konto kann eine **Klasse** anlegen und einen **Beitrittscode** ausgeben.
2. Ein Kind/Elternteil tritt per Code bei, ohne dem Lehrer einen Betreuungsauftrag zu geben.
3. Der Lehrer kann der Klasse eine Übung **mit Fälligkeit** zuweisen; die Rechteprüfung erlaubt das ohne
   `SupervisorLink`.
4. Der Supervisor sieht, was von außen zugewiesen wurde, und behält die Kontrolle über Punkte und Malus.

## Schätzung

**Größe: L** — neue Entitäten (Klasse, Mitgliedschaft, Beitrittscode), Rechteumkehr an den bestehenden
Ownership-Filtern, dazu UI auf beiden Seiten.

- **`migration: ja`** — neue Tabellen.
- **Risiko und eigentlicher Kern:** die **Ownership-Umkehr**. Heute hängt Zugriff am Betreuungsauftrag
  (`AuthAccess`, `ChildOwnershipFilter`); ein Lehrer soll ohne ihn zuweisen dürfen. Das ist die
  Entscheidung, die vor jeder Zeile Code fallen muss.
- **Testweg:** Rollen-/Ownership-Tests in `Pugling.Api.Tests` (Lehrer darf zuweisen, nicht betreuen) +
  E2E für den Beitrittscode.

## Verlauf

- **2026-07-30** — als Sammel-Story geerntet; Abweichung Entwurf ↔ gebaute Identität festgehalten.
