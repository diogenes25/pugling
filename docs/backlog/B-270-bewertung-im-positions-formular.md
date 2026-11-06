---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/katalog, rolle/supervisor]
aliases: [Bewertung beim Zuweisen sehen, Positions-Formular zeigt Sterne]
status: abgenommen
prio: P3
art: Wunsch
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Morgen-Entscheidung 2026-10-26 zu B-12 (docs/pm-sitzung-2026-10-26-nacht.md)
unverifiziert: false
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

## Ist-Stand (geprüft 2026-11-05)

[B-12](B-12-uebungen-kuratieren.md) liefert `AverageRating`/`RatingCount` an `ExerciseSummary` und `sort=rating`. Im
Vater-Web zeigt sie die Bewertung nur in der Verwaltungsliste (`VaterExercises.tsx`). Das Paket NP-8 („gute Übungen
beim Zusammenstellen finden“) meinte eher die Übungsauswahl im Positions-Formular (`PlanPositions.tsx`). Die
Morgen-Entscheidung vom 2026-10-26 hat B-12 mit der Verwaltungsliste abgenommen und diesen Teil abgetrennt.

## Entscheidungen

1. (aus dem Verlauf, im Auftrag) Die Übungsauswahl im Positions-Formular zeigt die Bewertung und sortiert nach ihr.
2. **(im Lauf ausformuliert)** Je Zeile `ratingSummary` wie in der Verwaltungsliste („★ 3,5 (2 Bewertungen)" / „noch unbewertet"). Zur Wahl
   stehen nur zwei Reihenfolgen – „wie im Katalog" (Server-Vorgabe) und „beste Bewertung zuerst" (`sort=rating&dir=desc`, unbewertete
   hinten, B-12) –, keine Richtung zum Umdrehen. Das Feld heißt „Trefferliste sortieren", weil „Reihenfolge" im selben Formular schon die
   Übungsreihenfolge der Position meint. Die Wahl gehört zu den Suchkriterien (eine Seite, andere Zeilen) und läuft darum durch dieselbe
   Maschine wie ein Filterwechsel (Sperre, Auswahl-Rücksetzen) – das Rücksetzen ist nicht eigens getestet. Der Zwilling im Assistenten (`VaterWizard`) bleibt unberührt – die Story nennt nur den Planbau.

## Akzeptanzkriterien

| Fall | Beleg | Mutante |
|---|---|---|
| Zeile zeigt Bewertung bzw. „noch unbewertet" | `e2e/planbau-bewertung.spec.ts:65-66` (an die Zeile gebunden) | Bewertung nicht angezeigt – gemessen nach dem letzten Test-Edit: rot `:65` |
| „beste Bewertung zuerst" stellt die bewertete vor die unbewertete | `:71`, gegen den Ausgangszustand `:68` (im Katalog steht sie hinten) | Sortierung nicht gesendet – gemessen: rot `:71` |

## Verlauf

- **2026-10-26** — angelegt aus der Morgen-Entscheidung zu B-12 ([Protokoll](../pm-sitzung-2026-10-26-nacht.md#morgen-entscheidung-2026-10-26)).
  Ausdrücklich **nicht** Teil der Nachtläufe vom 2026-10-26, darum keine Freigabe-Zeile.
- **2026-11-05** — **Entscheidung im Auftrag des Nutzers** (getroffen von Claude; Delegation im Dialog am 2026-11-05: „Wenn es etwas zu entscheiden gibt, übernimm meine Position und entscheide nach bestem Wissen und Gewissen“; [Protokoll](../pm-sitzung-2026-11-05-delegation.md)): Ja – die Übungsauswahl im Positions-Formular zeigt die Bewertung und sortiert nach ihr (`sort=rating`). **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt im Dialog am 2026-11-05 mit derselben Delegation):** ein Lauf darf diese Story auf Grundlage der Entscheidungen selbst ausformulieren, schätzen und bauen; trifft er auf eine Produktfrage, die sie nicht hergeben, hält er an.
- **2026-11-05** — ausformuliert, geschätzt und gebaut in Lauf AB, Sprint 19. Ist-Stand am Code bestätigt (`PlanPositions.tsx` zeigte keine
  Bewertung, Server liefert `averageRating`/`ratingCount` und `sort=rating`). Klickweg-Spec 1 passed; je Mutante rot an ihrer Zeile (Tabelle).
  Erster Lauf scheiterte an einem doppelten Label „Reihenfolge" – umbenannt. `tsc` sauber.
- **2026-11-05** — Review `frontend-reviewer`: kein 🔴; Stale-/Auswahl-Maschine durch `kriterien` konsistent, A11y in Ordnung. 🟡 der Zwilling
  im Assistenten zeigt keine Bewertung → [B-408](B-408-assistent-zeigt-keine-bewertung.md) (Wunsch, kein Code hier). 🟢 die Bewertungs-
  Zusicherung war nicht an die Zeile gebunden → gebunden (erster Versuch falsch maskiert, `\(` im Template-String – berichtigt), beide
  Mutanten danach erneut gemessen: unverändert. 🟢 Rücksetzen beim Umsortieren war behauptet, nicht getestet → Entscheidung 2 so beschriftet.
  **Zähler 0 von 5** (keine 🟡-Behebung im Code).
- **2026-11-05** — **abgenommen.** Rollengang Vater als E2E: `e2e/planbau-bewertung.spec.ts` (zweites Konto bewertet, Planbau zeigt und
  sortiert) 1 passed nach dem letzten Edit. Vitest 653/653, `tsc` sauber. Creator und Sohn unberührt (nur `PlanPositions.tsx`). Commit:
  siehe nächster Eintrag.
