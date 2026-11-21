---
tags: [typ/story, status/abgenommen, bereich/gamification, rolle/student, rolle/supervisor]
aliases: [Verlängerter Plan kostet Malus für abgelaufene Tage]
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: "Review von Sprint 102 (B-672, pugling-reviewer) im Nachtlauf 2026-11-21, docs/pm-sitzung-2026-11-21.md"
grund: ""
ersetzt_durch: []
entgangen_bei: [B-672]
nachgeschaut: "2026-11-21"
---

# B-783 · Ein verlängerter, schon abgelaufener Plan holt die Tage dazwischen als Pflicht zurück

Das Gegenstück zu B-672 am anderen Ende des Plans. Läuft ein Plan von T bis T+5 und setzt der Vater am Tag T+10 das
Ende auf T+30, waren die Tage T+6 bis T+9 nicht spielbar (`PositionPlayService.PlanPlayableForChild` verlangt
`today <= EndDate`). `ActivatedAt` bleibt aber bei T, also `<= StartDate`, und in `PlanDueForPeriod`
(`backend/Pugling.Api/Services/Shared/PositionProgressService.cs:677-679`) greift die reine Überlappungsregel: Die
Abrechnung bucht Malus für T+6 bis T+9, und `ChildStreakAsync` (`:493`) zählt diese Tage als offene Pflicht und bricht
die Serie.

Per Test nachgestellt (Nachtlauf 2026-11-21): Plan T..T+5 an jedem Tag erfüllt, Uhr auf T+10, PATCH `endDate` T+30,
Abrechnung – gemessen vier Malus-Buchungen T+6..T+9; mit einem zweiten, täglich erfüllten Plan bricht die Serie auf 1.
Einzige Schreibstelle ist `StudyPlansController.Update` (`if (dto.EndDate is not null) plan.EndDate = …`); Plan-Kopie,
Agent und Skripte legen Pläne nur an (Review von Sprint 102).

## Entscheidungen

*Stellvertretend entschieden im Nachtlauf 2026-11-21 (Defekt, autonom grillbar).*

1. **Wird ein aktiver, schon abgelaufener Plan verlängert, gilt er ab heute als eingeschaltet: `ActivatedAt = heute`.**
   Bedingung: `plan.Active && neues Ende > altes Ende && altes Ende + 1 < heute`. *Begründung:* Zwischen altem Ende und
   heute war der Plan nicht spielbar (`PlanPlayableForChild`), nach der Verlängerung ist er es ab heute – das ist genau
   der Fall „wieder eingeschaltet“ von B-613, dessen Regel `ActivatedAt = heute` ist. *Kosten:* Wie bei B-613 verwischt
   ein einziges Datum die Vorgeschichte: noch nicht abgerechnete, verpasste Tage im alten Fenster werden mit
   freigestellt (zugunsten des Kindes, Vertrag der Spalte). Das schreibt auch die Sicht auf das alte Fenster um (Review
   Sprint 103): Die Zeitachse des Vaters zeigt einen verpassten Tag dort ohne Pflicht (`TimelineDutyDone` = `null`), und
   ein Tag, an dem ein anderer Plan erfüllt war, wird in `CompleteChildDaysAsync` nachträglich vollständig – die
   Metriken `DaysComplete`/`StreakDays` können steigen und eine Mission auslösen. Ein schon gebuchter Malus bleibt.
   Weil eine Verlängerung alltäglicher ist als Aus- und Wiedereinschalten (Plan lief übers Wochenende aus), ist das
   eine **Entscheidung zum Umstoßen** für den Nutzer; die Alternative wäre ein Startverlauf als Tabelle (Migration),
   wie in B-672 Entscheidung 2 verworfen. *Verworfen:* der Vorschlag aus dem Review,
   `min(altes Ende + 1, heute)` – nachgerechnet lässt er die Lücke stehen, weil ihre Tage nach diesem Datum liegen und
   `from >= ActivatedAt` sie fällig macht (Mutante N5).
2. **Ein Plan, der gestern endete, hat keine Lücke und bleibt unverändert** (`altes Ende + 1 < heute`, nicht `<`).
   *Begründung:* Sonst stellte jede rechtzeitige Verlängerung den ganzen bisherigen Plan frei. *Kosten:* keine.
3. **Ein inaktiver Plan wird nicht angehoben, ein Titel-PATCH oder ein nicht späteres Ende auch nicht.** Das spätere
   Einschalten setzt `ActivatedAt` ohnehin. Keine Migration, keine rückwirkende Heilung schon gebuchter Malusse (wie
   B-672 Entscheidung 3).

## Akzeptanzkriterien

1. Plan T..T+5 erfüllt, bei T+10 bis T+30 verlängert: die Abrechnung bucht nichts. Rot ohne die Regel (T+6..T+9) und
   mit `min(altes Ende + 1, heute)`.
2. Dazu Plan A täglich erfüllt, B heute erfüllt: Serie 14 (T−3..T+10). Rot ohne die Regel (1).
3. Plan T..T+9, T+8 verpasst, bei T+10 verlängert: T+8 bleibt gebucht. Rot, wenn die Regel auch bei Ende gestern greift.
4. Datenzusicherung (beschriftet): Titel-PATCH, nicht späteres Ende und verlängerter inaktiver Plan lassen
   `ActivatedAt` stehen. Rot ohne „neues Ende > altes Ende“ bzw. ohne `plan.Active`.

## Schätzung

**Größe: XS** — wie B-672 an derselben Stelle: eine Variable und eine Bedingung im Controller, vier Testfälle in
`RueckwirkendePflichtTests`. `migration: nein`, `vertragsbruch: nein`, `wo: backend`.

## Verlauf

- **2026-11-21** — angelegt aus dem Review von Sprint 102 (🟡, außerhalb des Sprint-Diffs, darum eigene Story und ohne
  Zählung; `entgangen_bei: [B-672]`).
- **2026-11-21** — ausformuliert, gegrillt (stellvertretend, Defekt), geschätzt XS und in-arbeit im selben Sprint 103.
  Rote Proben ohne Fix: AK 1 erwartet leer, gemessen `[T+6, T+7, T+8, T+9]`; AK 2 erwartet 14, gemessen 1. AK 3 und
  AK 4 im Ausgangszustand grün (Gegenprobe bzw. Datenzusicherung). Je AK-Fall Test und Mutante:
  - AK 1 → `Verlaengerter_Abgelaufener_Plan_Bucht_Keinen_Malus_Fuer_Die_Luecke`; N1 (Regel weg), N5 (`altes Ende + 1`) → rot.
  - AK 2 → `Verlaengerter_Abgelaufener_Plan_Bricht_Die_Serie_Nicht`; N1 → rot.
  - AK 3 → `Gestern_Abgelaufener_Plan_Bleibt_Pflicht`; N2 (`oldEnd < today`) → rot.
  - AK 4 → `Ohne_Verlaengerung_Wird_Nichts_Angehoben`; N3 (ohne `Active`), N4 (ohne „späteres Ende“) → rot.
  - Eine Bedingung `today > ActivatedAt` stand im ersten Entwurf; sie ist unerreichbar (ein abgelaufener Plan hat
    `ActivatedAt` ≤ Ende < heute) und wurde gestrichen statt ungetestet behalten.

  Gemessen an `6a79b54e + Arbeitsbaum geändert` mit `mutant-probe.py` (Debug), Grundlinie danach grün:

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | N1 | `StudyPlansController.cs:239` Regel weg | red | red |
  | N2 | `:238` `oldEnd < today` | red | red |
  | N3 | `:238` ohne `plan.Active` | red | red |
  | N4 | `:238` ohne `EndDate > oldEnd` | red | red |
  | N5 | `:239` `oldEnd.AddDays(1)` | red | red |

  Review (`pugling-reviewer`): kein 🔴. 🟡 1: die Untergrenze der Lücke hatte keinen Fall (`oldEnd + k < today` mit
  k = 2..4 blieb grün) → neuer Fall `Eintaegige_Luecke_Bucht_Keinen_Malus` (AK 3b, Plan T..T+8, bei T+10 verlängert,
  T+9 frei), Mutante N6 (`oldEnd.AddDays(2)`); ändert Tests, **Zähler 1 von 5**. 🟡 2: die Kosten der Entscheidung 1
  waren zu knapp benannt → oben ergänzt (nur Story, ohne Zählung). 🟢: Kommentar zum Fall „erst verkürzt, dann wieder
  verlängert“ im Code ergänzt; Wochenpflicht, Start+Ende und `active:true`+Verlängerung im selben PATCH sind
  nachgerechnet richtig und ohne trennende Mutante, darum ohne eigenen Fall.

  Neu gemessen nach der letzten Änderung an `6a79b54e + Arbeitsbaum geändert`, Grundlinie danach grün: N1–N5 wie oben
  (Zeilen jetzt `:239/:240`) rot, **N6** `:239` `oldEnd.AddDays(2) < today` → rot an `Eintaegige_Luecke_Bucht_Keinen_Malus`.
- **2026-11-21** — abgenommen in Commit `6ddf9b2c` (Sprint 103). Backend 1947/1947 (Release).
  **Rollengang:** wie bei B-672 braucht die Wirkung vergehende Tage, die laufende App hat keine verstellbare Uhr. Ersatz:
  die Integrationsfälle fahren den PATCH des Vaters über die API. Regressions-E2E nach der letzten Änderung frisch
  gestartet (5201/5174): `full-flow`, `vater-schmal`, `heute-dran-nach-antworten` – 4 passed. Vater abgenommen auf diesem
  Beleg, Sohn und Creator als Regressionszeugen.
- **2026-11-21** — nachgeschaut (Nachschau Sprint 103 in der Retro von Sprint 104), kein Befund. Geprüft:
  `DeactivateSameSubjectPlansAsync` (`StudyPlansController.cs:162-168`) filtert nach `Active` ohne Datum – ein
  verlängerter abgelaufener Plan war also schon der einzige aktive seines Fachs und schaltet keinen laufenden ab; der
  Client (`VaterPlanDetail.tsx:160`) schickt `endDate` nur bei Änderung, ein Speichern ohne Datumsänderung löst die
  Regel nicht aus.
