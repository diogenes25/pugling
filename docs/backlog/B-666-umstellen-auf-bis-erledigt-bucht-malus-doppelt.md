---
tags: [typ/story, status/abgenommen, bereich/gamification, rolle/student, rolle/supervisor]
aliases: [Malus doppelt nach Umstellen, Daily und UntilDone teilen keine Periode]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: "PM-Sitzung 2026-11-18, Runde 4 – Nachschau von Sprint 86 (177027d9)"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-636]
nachgeschaut: 2026-11-19
---

# B-666 · Umstellen einer Nacharbeit von „Tagesziel“ auf „bis erledigt“ bucht den Malus schon abgerechneter Tage noch einmal

## Beobachtung

Gefunden in der Nachschau von [B-636](B-636-pflicht-bis-erledigt-mit-abschlussbonus.md), am Code bestätigt (`fe13531c`):
`SettleClosedPeriodsAsync` prüft „schon belohnt/schon bestraft“ nur gegen `r.Cadence == cadence` mit dem **heutigen**
Takt (`backend/Pugling.Api/Services/Shared/PositionProgressService.cs:776`, `:778`), und der eindeutige Index liegt auf
`(PlanPositionId, Cadence, PeriodStart)` (`backend/Pugling.Api/Data/PuglingDbContext.cs:723`). Eine `Daily`-Zeile schützt
denselben Tag also nicht vor einer zweiten `UntilDone`-Zeile.

**Szenario:** Nacharbeit mit Tagesziel und Malus 5. Montag verpasst ⇒ `PositionGoalPenalty(Daily, Mo)`, −5. Dienstag stellt
der Vater auf „bis erledigt“ (der in B-636 vorgesehene Weg; in der echten DB liegen 11 Nacharbeits-Positionen). Die nächste
Abrechnung findet für Montag keine `UntilDone`-Zeile, prüft mit der neuen Regel (ganze Runde) „nicht erfüllt“ ⇒ Montag
kostet **noch einmal** −5. Betroffen ist jeder Tag im 14-Tage-Fenster vor dem Umstellen, auch belohnte Tage (Schwelle
erreicht, aber keine ganze Runde). Kleiner Doppelgänger bei der Belohnung (`:537`): Tageszielbelohnung mit `Daily` und am
selben Tag eine zweite mit `UntilDone`.

**Dringlich vor dem nächsten Umstellen einer echten Nacharbeit** – das Kind verliert Münzen ohne eigenes Zutun.

## User Story

Als *Sohn* möchte ich, dass ein schon abgerechneter Tag nicht noch einmal Münzen kostet, nur weil Papa meine Nacharbeit
auf „täglich, bis erledigt“ umstellt. Als *Vater* möchte ich umstellen können, ohne mein Kind rückwirkend zu bestrafen.

## Ist-Stand am Code

Gelesen und per Test nachgestellt am 2026-11-18 auf `6b961cb5`. **Die Beobachtung stimmt, und zwar in beide Richtungen.**

- Drei Existenzprüfungen vergleichen `r.Cadence == cadence` mit dem **heutigen** Takt:
  Belohnung in `EvaluateAndAwardAsync` (`backend/Pugling.Api/Services/Shared/PositionProgressService.cs:536-537` vor dem
  Fix), Belohnung und Malus in `SettleClosedPeriodsAsync` (`:775-778`).
- Der eindeutige Index liegt auf `(PlanPositionId, Cadence, PeriodStart)` (`backend/Pugling.Api/Data/PuglingDbContext.cs:723`)
  – er schützt je Takt, nicht je Periode.
- Rote Probe (drei Fälle, `RemediationPositionTests`): Malus-Tage erwartet `[18.11.]`, gemessen `[18.11., 18.11.]`; Malus
  an einem belohnten Tag erwartet leer, gemessen ein Eintrag; Belohnungen erwartet 1, gemessen 2.
- Weitere Leser der Zeilen (Review): nur `PointsAwardedAsync` (`:297-300`), filtert auf `Day`, nicht auf den Takt –
  unberührt. Zeitachse, Serie und Missionen rechnen live über `IsGoalMetAsync`.

## Die echte Lücke

„Schon abgerechnet?“ ist eine Frage nach der **Periode**, wird aber nach dem **Takt** gestellt. Tagesziel und „bis
erledigt“ haben dieselbe Periode (einen Tag).

## Offene Punkte

1. ~~Familie beim Nachsehen oder `UntilDone` unter `Daily` buchen?~~ → Entscheidung 1.
2. ~~Gilt das auch für Daily ↔ Weekly?~~ → Entscheidung 2.
3. ~~Ein erfüllter Tag ohne Belohnungszeile (Ziel ohne Punkte)?~~ → Entscheidung 3.

## Entscheidungen

Autonom gegrillt im Nachtlauf 2026-11-18 (`art: Defekt`, Freigabe 1).

1. **Die Existenzprüfungen lesen die Perioden-Familie** (`SamePeriodCadences`: `Daily` und `UntilDone` gemeinsam), die
   Zeilen behalten den Takt ihrer Buchung als Momentaufnahme. *Begründung:* die Momentaufnahme ist Schema-Konvention
   (`Pugling.Api/CLAUDE.md`, „Kein Zeitpunkt und keine Periode als Text“); seit Sprint 86 gebuchte `UntilDone`-Zeilen
   müssten bei „unter `Daily` buchen“ ohnehin mitgelesen werden. *Kosten:* der Index schützt nur noch je Takt; ein
   Wettlauf braucht Umstellen und Buchung im selben Augenblick – hingenommen und an der Methode dokumentiert.
2. **Daily ↔ Weekly bleibt getrennt.** *Begründung:* verschiedene Perioden; B-613 hat entschieden, dass ein Wechsel
   Tag↔Woche das Pflicht-Datum behält. *Kosten:* ein Wechsel mitten in der Woche kann einen Tag und die Woche getrennt
   werten (Review-Hinweis 🟢3) – bewusst nicht in dieser Story.
3. **Ein erfüllter Tag ohne Belohnungszeile bleibt ungeschützt.** Ein Ziel ohne Punkte bucht absichtlich keine Zeile
   (`PositionGoalOverviewTests.ZielOhnePunkte_ErfuelltDiePflicht_BuchtAberNichts`, Injektion B06). Der Reparaturvorschlag
   des Reviewers (Nullzeile als Markierung) bricht genau das – gemessen: der Test fällt. *Kosten:* bei Ziel-Punkten 0,
   Malus > 0 und Umstellen vor der nächsten Abrechnung wird ein erfüllter Tag nach der strengeren Regel gewertet.
   Dokumentiert an `SamePeriodCadences`.

## Akzeptanzkriterien

1. Malus unter `Daily` gebucht, auf „bis erledigt“ umgestellt, abgerechnet ⇒ genau eine Malus-Zeile für den Tag. *Rot,
   wenn die Malus-Prüfung nur den heutigen Takt liest.*
2. Belohnung unter `Daily` gebucht, umgestellt, eine Karte gespielt, abgerechnet ⇒ kein Malus. *Rot, wenn die
   Belohnungsprüfung der Abrechnung nur den heutigen Takt liest.*
3. Belohnung unter `Daily` gebucht, umgestellt, ganze Runde gespielt ⇒ genau eine Belohnungszeile. *Rot, wenn die
   Prüfung im Belohnungspfad nur den heutigen Takt liest.*
4. Gegenrichtung: Malus unter `UntilDone`, zurück auf Tagesziel ⇒ genau eine Malus-Zeile. *Rot, wenn die Familie
   einseitig ist.*

## Schätzung

**Größe: S** — wie B-01: drei Vergleiche an einer Stelle, deren Wirkung vorher klar ist. **`migration: nein`** (keine
Spalte, der Index bleibt), **`vertragsbruch: nein`** (kein Feld), **`wo: backend`**.

### Risiken

1. Der Index ist nicht mehr „die Garantie“ über Takte hinweg – Doku von `EvaluateAndAwardAsync` korrigiert.
2. EF-Übersetzung von `array.Contains(enum als String)` – läuft schon in `WalletService.cs:26`; die Fälle laufen auf SQLite.

### Angriffsplan

Rote Fälle zuerst (AK 1–3), dann `SamePeriodCadences` und die drei Prüfungen, dann AK 4.

### Testweg

`RemediationPositionTests` (vier Fälle), Mutanten je Prüfung, `pugling-reviewer`. Rollengang: kein UI-Pfad ändert sich;
Ersatz ist der Integrationstest über die echten Endpunkte (Vater-PATCH, Kind-Antworten) – siehe `## Verlauf`.

## Verlauf

- **2026-11-18** — angelegt aus der Nachschau von Sprint 86.
- **2026-11-18** — Nachtlauf: ausformuliert, am Code nachgestellt (rote Probe 3/3, Zahlen im Ist-Stand), autonom gegrillt
  (Entscheidungen 1–3), geschätzt S/backend/keine Migration/kein Bruch; in Arbeit.
- **2026-11-18** — AK 1 → `backend/Pugling.Api.Tests/RemediationPositionTests.cs:770`, U3 (Malus-Prüfung nur heutiger Takt, `PositionProgressService.cs:794`) → rot · AK 2 → `:787`, U2 (Belohnungsprüfung der Abrechnung, `:792`) → rot · AK 3 → `:803`, U1 (Belohnungspfad, `:551`) → rot · AK 4 → `:823`, U4 (Familie einseitig, `:98`) → rot. Gemessen an `6b961cb5 + Arbeitsbaum geändert`, 2026-11-18 22:37 (Skriptkopie mit Schreib-Wiederholung, siehe Retro): alle vier rot am richtigen Ort.
- **2026-11-18** — Review `pugling-reviewer`: kein 🔴. 🟡1 (erfüllter Tag ohne Belohnungszeile bei Ziel-Punkten 0) – Vorschlag Nullzeile **verworfen**: bricht `PositionGoalOverviewTests.ZielOhnePunkte_ErfuelltDiePflicht_BuchtAberNichts` (gemessen: 1 failed von 1881), Lücke als Entscheidung 3 dokumentiert (ohne Zählung, kein Code bleibt). 🟡2 außerhalb des Diffs → [B-669](B-669-beendete-nacharbeit-zurueck-auf-tagesziel-bestraft-tage-davor.md). 🟢 Doku des Index korrigiert, Gegenrichtung als AK 4 ergänzt (ohne Zählung).
- **2026-11-18** — Verifikation nach der letzten Bearbeitung: `dotnet test Pugling.sln -c Release` **1882/1882**, Vitest **1249/1249**, `tsc -b` sauber, `dotnet format --verify-no-changes` ohne Fund außer Zeilenende-Artefakten an nicht berührten Zeilen; E2E `nacharbeit-bis-erledigt`, `nacharbeit`, `full-flow` **3 passed** (Ausweichports 5211/5181, Server von Playwright nach der letzten Bearbeitung gestartet).
- **2026-11-18** — Rollengang: kein UI-Pfad ändert sich (das Formular schickt denselben PATCH). Ersatz: die Fälle laufen über die echten Endpunkte (Vater-PATCH, Kind-Antworten, Abrechnung); die Rollengang-E2E `nacharbeit-bis-erledigt` (Vater stellt um, Kind spielt) ist grün.
- **2026-11-18** — abgenommen: `f5b04d26` (Sprint 93, Nachtlauf).
- **2026-11-19** — nachgeschaut (PM-Sitzung 2026-11-19, Nachschau über `f5b04d26` bzw. `796314db`/`8f3d12db`): ohne Befund: alle drei Existenzprüfungen über `SamePeriodCadences` (`PositionProgressService.cs:549-551, :791, :793`), kein weiterer Leser filtert nach der Taktung.
