---
tags: [typ/story, status/idee, bereich/frontend, bereich/katalog, rolle/supervisor]
aliases: [Bewertung beim Zuweisen sehen, Positions-Formular zeigt Sterne]
status: idee
prio: P3
art: Wunsch
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: Morgen-Entscheidung 2026-10-26 zu B-12 (docs/pm-sitzung-2026-10-26-nacht.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-270 · Die Bewertung einer Übung beim Zusammenstellen des Plans sehen

## User Story

Als Vater, der einem Kind eine Position anlegt, möchte ich bei der Übungsauswahl sehen, wie gut andere die Übung
bewertet haben, und nach der Bewertung sortieren können. So finde ich gute Übungen dort, wo ich sie auswähle, und
nicht erst in der Verwaltungsliste.

## Ist-Stand (unverifiziert)

[B-12](B-12-uebungen-kuratieren.md) liefert `AverageRating`/`RatingCount` an `ExerciseSummary` und `sort=rating`. Im
Vater-Web zeigt sie die Bewertung nur in der Verwaltungsliste (`VaterExercises.tsx`). Das Paket NP-8 („gute Übungen
beim Zusammenstellen finden“) meinte eher die Übungsauswahl im Positions-Formular (`PlanPositions.tsx`). Die
Morgen-Entscheidung vom 2026-10-26 hat B-12 mit der Verwaltungsliste abgenommen und diesen Teil abgetrennt.

## Verlauf

- **2026-10-26** — angelegt aus der Morgen-Entscheidung zu B-12 ([Protokoll](../pm-sitzung-2026-10-26-nacht.md#morgen-entscheidung-2026-10-26)).
  Ausdrücklich **nicht** Teil der Nachtläufe vom 2026-10-26, darum keine Freigabe-Zeile.
- **2026-11-05** — **Entscheidung im Auftrag des Nutzers** (getroffen von Claude; Delegation im Dialog am 2026-11-05: „Wenn es etwas zu entscheiden gibt, übernimm meine Position und entscheide nach bestem Wissen und Gewissen“; [Protokoll](../pm-sitzung-2026-11-05-delegation.md)): Ja – die Übungsauswahl im Positions-Formular zeigt die Bewertung und sortiert nach ihr (`sort=rating`). **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt im Dialog am 2026-11-05 mit derselben Delegation):** ein Lauf darf diese Story auf Grundlage der Entscheidungen selbst ausformulieren, schätzen und bauen; trifft er auf eine Produktfrage, die sie nicht hergeben, hält er an.
