---
tags: [typ/protokoll, bereich/backlog]
---

# PM-Sitzung: Schultest nacharbeiten II (Sprint 86)

**Datum:** 2026-11-18  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Die Nacharbeit aus Sprint 85 wird zur Pflicht, die von selbst endet, und der Vater sieht je Aufgabe, wie viel
Hilfe sein Sohn gebraucht hat.

## Runde 1 — Ausgangslage

Aufruf `/pm-loop` ohne eigenes Ziel. Die Agenda kommt aus der
[Sprintplanung vom 2026-11-17](pm-sitzung-2026-11-17-sprintplanung.md): Sprint 85 und Sprint 92 sind abgenommen
([Lernkarten-Lauf](pm-sitzung-2026-11-17-nachtlauf-lernkarte.md)), der nächste Sprint der Folge ist **86**. Ein neuer
Befund unter `docs/anmerkungen/` liegt nicht vor (Export vom 2026-08-14, beide Einträge erledigt). Der Heimnetz-Server
auf `:5200` läuft nicht; er wird nicht gestartet – Rollengang gegen Wegwerf-DBs.

Die Rollenstimmen dieser Runde sind die der Sprintplanung (beobachtet an Theodors verpatztem Unit-1-Test, B-632) und
werden hier nicht neu erfunden:

- **Vater:** „Die Nacharbeit läuft, aber ich muss sie heute als normale Tagespflicht einstellen – sie hört nie auf, und
  ich sehe nicht, wie oft er sich helfen ließ."
- **Sohn:** „Wenn ich alles kann, will ich, dass das Ding weg ist – und dass es sich gelohnt hat."
- **Creator:** kein Pfad in diesem Sprint.

## Sprint 86 — Ziel & Umfang

**Sprint-Ziel (Sicht Vater):** „Ich stelle die Nacharbeit als Pflicht ‚täglich, bis erledigt' ein; sie endet von
selbst mit einem Abschlussbonus, und ich sehe je Aufgabe, auf welcher Stufe mein Kind sie gelöst hat und ob sie
erledigt ist."

**Umfang:** [B-636](backlog/B-636-pflicht-bis-erledigt-mit-abschlussbonus.md) (M),
[B-637](backlog/B-637-nacharbeit-vater-sicht-je-aufgabe.md) (S).

**Entwickler-Brief:** wie in der Sprintplanung. Dazu die Senke dieses Sprints: **eine** Funktion entscheidet, ob eine
Position an einem Tag eine Pflicht trägt (`PositionProgressService.HasDutyOn`); jede Verzweigung nach `GoalCadence`
wird per `grep` gefunden und je Aufrufer entschieden.

## Iteration 86 — umgesetzt

- **Backend (API-First):** `GoalCadence.UntilDone`, `PointKind.RemediationBonus`; Migration
  `UntilDoneCompletionBonus` (Kette verlängert: `PlanPosition.CompletionBonus`, `PlanPosition.CompletedOn`, Tabelle
  `PositionCompletionBonuses` mit eindeutigem Index je Position). Senke `HasDutyOn`/`IsDailyDuty` – je Aufrufer
  entschieden:

  | Verzweigung nach `GoalCadence` | Entscheidung |
  | --- | --- |
  | `IsGoalMetAsync` | eigener Zweig `RemediationDayMetAsync`: ganz gespielte Runde; ab dem Endtag erfüllt |
  | `OpenGoalProgressAsync` (Balken) | Runde gegen 100 %, nicht gegen die Schwelle |
  | `ComputeDayAsync` (Tagesmission) | nach dem Endtag nicht mehr gelistet, am Endtag „erfüllt“ |
  | `RewardObligations` (Truhe, Bonus) | „bis erledigt“ zählt wie ein Tagesziel |
  | `DutyStateAsync` (Serie, Zeitleiste) | nach dem Ende keine Pflicht – trägt die Serie nicht weiter |
  | `EvaluateAndAwardAsync` (Ziel-Punkte) | nach dem Ende keine Tagesbelohnung |
  | `SettleClosedPeriodsAsync` (Malus) | täglich; nach dem Ende über `IsGoalMetAsync` straffrei |
  | `IsIntroductionDaySparedAsync` | bleibt `Daily`: eine Nacharbeit hat keinen Einführungstag |
  | Ledger-Texte „Tagesziel/Wochenziel“ | „bis erledigt“ heißt „Tagesziel“ |
  | `paysCoins` im Üben | unverändert (`!= None`) |

  Abschluss in `CompleteRemediationIfDoneAsync` (nach der Antwort, die die letzte Aufgabe erledigt; Bonus genau einmal,
  `ReviewOutcome.CompletionBonus`). Report (B-637) um `RemediationTaskReport` je Aufgabe und `Remediation`/`CompletedOn`
  erweitert; die Auswertung ist schon Supervisor-only (Lösungsfeld-Regel) und hinter `PlanOwnershipFilter`.
- **Fund der Migrationsprobe:** in der echten DB stehen 11 Nacharbeits-Positionen. Eine schon fertige, die der Vater
  später auf „bis erledigt“ stellt, hätte nie geendet (kein Abschluss-Ereignis mehr) und jeden Tag Malus gekostet. Behoben:
  `StampFinishedRemediationAsync` stempelt das Ende beim Umstellen, ohne Bonus.
- **Frontend:** Rhythmus „täglich, bis erledigt“ nur mit Nacharbeit (`cadencesFor`), Abwählen fällt aufs Tagesziel zurück
  (`withRemediation`), Feld „Abschlussbonus 🪙“, Positionszeile mit Bonus und „geschafft am“, Nacharbeits-Report (Vorgabe,
  Lösung, Stand, „Gelöst je Stufe“); Sohn-Arcade: Rhythmus-Pille, „bis erledigt“ zählt als Tagespflicht (Pfad, Zähler,
  Truhen-Balken), Jubel „Nacharbeit geschafft!“ mit Bonus.

## Review Sprint 86

Beide Reviewer liefen nur lesend (eine Mutanten-Probe lief im selben Checkout). **Keine 🔴.**

| Fund | Quelle | Behandlung |
| --- | --- | --- |
| Y1 Pool schrumpft hinter die letzte offene Aufgabe → Pflicht endet nie, Malus jeden Tag | pugling-reviewer | Vorschlag „Tag gilt dann als erfüllt“ **verworfen**: eine nie endende erfüllte Pflicht zahlte täglich Ziel-Punkte ohne Spielen. Stattdessen stempelt der Rundenstart das Ende, wenn nichts mehr offen ist (ohne Bonus); der PATCH stempelte schon. Test `Geschrumpfter_Pool_beendet_die_Nacharbeit_beim_Start` |
| Y2 beendete Position zahlt bei Leitner noch Münzen; Ende nicht umkehrbar | pugling-reviewer | `paysCoins` an `HasDutyOn`; „Ende ist endgültig“ dokumentiert. Test `Nach_dem_Ende_zahlt_die_Position_keine_Muenzen` |
| Y3 positiver Münzpfad der Runde ungetestet | pugling-reviewer | Zusicherung `PositionGoalReward(UntilDone, heute)` nach `…/end` ergänzt |
| G2 Report-Zeilen außerhalb des Pools „ewig offen“ | pugling-reviewer | behoben (Pool-Grenze), im Test oben |
| G4 Kommentar „Index ist die Garantie“ gilt nur mit Bonus | pugling-reviewer | Kommentar präzisiert |
| G1 Alt-Nacharbeiten ohne `completedOn` (Anzeige) · G3 Vorschau des Vaters erfüllt die Tagespflicht (wie B-542 für alle Positionen) | pugling-reviewer | G1 → [B-652](backlog/B-652-abschlussbonus-feld-nachschliff.md); G3 bewusst so (gleiche Regel wie überall) |
| 🟡 Bonus nur im 2-s-Jubel (aria-hidden, bei reduzierter Bewegung unsichtbar) | frontend-reviewer | Bonus in Ansage, Blatt und Rundensumme; E2E prüft die dauerhafte Ansage statt des Jubels |
| 🟡 Stufennamen und Sohn-Pille „bis erledigt“ ohne Test | frontend-reviewer | E2E liest den Freitext-Namen aus dem Manifest; `cadenceKey` exportiert und getestet |
| 🟢 Bonus in der falschen Spalte | frontend-reviewer | in die Punkte-Spalte verschoben |
| 🟢 Bonus bleibt beim Rhythmuswechsel, Emoji im Feldnamen, keine Client-Grenze | frontend-reviewer | → [B-652](backlog/B-652-abschlussbonus-feld-nachschliff.md) |

**Mutanten:** Endmessung nach der letzten Bearbeitung – Backend 27/27 rot (U1–U22, R1–R5), Frontend 13/13 rot (F1–F10,
E1, E3, E4); Tabellen im Verlauf von B-636. Erster Lauf: U13 grün (der Test stellte den Altbestand ohne Endstempel nicht
her – nachgezogen), U19/F5/E1 kompilierten nicht (ungenutzter Parameter unter `TreatWarningsAsErrors` bzw.
TS-Einengung) – in kompilierbarer Form wiederholt.

**Verifikation:** `dotnet test Pugling.sln -c Release` **1843/1843**, `tsc` sauber, Vitest **1211/1211**
(ein Ausreißer `SohnProgress.test.tsx` im Teillauf, allein 9/9 und ohne Änderung von mir – Flackern wie B-603), E2E
`nacharbeit-bis-erledigt.spec.ts` + `nacharbeit.spec.ts` **2 passed**. Migrationsprobe an einer Kopie der echten DB
(Sicherungs-API, nur lesend geöffnet): angewandt, 155 Positionen, 11 Nacharbeiten erhalten.

## Runde 2 — Abnahme

Der Server des Rollengangs wurde von Playwright **nach der letzten Bearbeitung** gestartet (Ausweichports 5211/5181,
Wegwerf-DB); der Heimnetz-Server auf `:5200` blieb unberührt.

- **Vater — abgenommen.** Maschineller Rollengang `nacharbeit-bis-erledigt.spec.ts`: im Positionsformular „täglich, bis
  erledigt“ gewählt, Abschlussbonus 30 eingetragen, gespeichert, die Zeile nennt Rhythmus und Bonus; nach dem Spiel des
  Kindes „geschafft am“ in der Zeile; im Report „Nacharbeit: 2/2 erledigt“, je Aufgabe „erledigt“ und der Stufenname aus
  dem Manifest mit „×1“. In seinen Worten: „Die Nacharbeit hört von selbst auf, und ich sehe, wo er Hilfe brauchte.“
  Malus nach dem Ende und doppelter Bonus sind am Server belegt, nicht im Rollengang.
- **Sohn — geliefert, Gerätecheck offen.** Im selben Rollengang: beide Aufgaben frei getippt, Ansage „Nacharbeit
  geschafft! +30 🪙 Abschlussbonus“, Bonus auf dem Blatt. Offen für einen Menschen am Handy: ob der große Jubel (🏆, 2 s)
  sich wie ein „TADAA“ anfühlt.
- **Creator — Regressionszeuge.** Kein Pfad des Creators im Diff; Suite grün.

## Retrospektive

**Nachschau:** B-643 (Sprint 92, `e10cdd34`) nur über die Git-Historie gelesen – ein Durchrutscher,
[B-653](backlog/B-653-nur-neue-inhalte-verliert-offene-lernkarte.md) (P3, `entgangen_bei: B-643`), am Code bestätigt.
`nachgeschaut: 2026-11-18` an B-643 gesetzt. Index: 493 von 518 nachgeschaut.

- **Von außerhalb der Schleife:** `86edb1d3` (Nutzer, nach dem Lauf vom 17.11.): die Lernkarte prüfte beim Abschreiben
  strenger als der Server (`abgeschrieben` ohne Klammerteile) – Code aus B-598, eine Vergleichsregel je Pfad statt an
  der Senke. Schon behoben; keine neue Story.
- **Was die eigenen Tore durchließen:** eine **No-op-Mutante** (eine fehlgeschlagene Ersetzung baute `new == old`) lief
  durch und erschien als „green (abweichend)“ – sie sah wie eine Testlücke aus und kostete einen vollen Lauf.
- **Der eine Mechanismus (neues Tor):** `.claude/scripts/mutant-probe.py` bricht vor dem ersten Bau ab, wenn eine
  Mutante ihre Zeile nicht ändert. Probe: dieselbe Datei mit `new == old` endet mit Exit 1 und der Meldung, nichts wird
  geschrieben. **Weil es ein neues Tor ist, endet der Lauf hier.**
- **Bewusst ungedeckt:** der Jubel selbst (sensorisch, Gerätecheck); `SohnProgress.test.tsx` flackert unter Last (B-603).
- **Bestätigt, kein neuer Mechanismus:** der Reparaturvorschlag Y1 des Reviewers brach eine Invariante (Ziel-Punkte für
  nichts) – [[review-vorschlag-gegen-invariante]] hat gegriffen.

## Offene Roadmap (priorisiert)

Quelle ist [docs/backlog/](backlog/INDEX.md). Nach der Sprintplanung vom 2026-11-17 folgt **Sprint 87** (B-611, B-612,
B-613 – Truhe, Startseite und Serie über alle Pläne; Defekte vor neuer Funktion). Neu aus dieser Sitzung:
[B-652](backlog/B-652-abschlussbonus-feld-nachschliff.md) (P3, Aufräumen), [B-653](backlog/B-653-nur-neue-inhalte-verliert-offene-lernkarte.md)
(P3, Defekt).
