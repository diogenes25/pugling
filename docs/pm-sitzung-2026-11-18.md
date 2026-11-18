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

## Runde 3 — Sprint 87: Truhe, Startseite und Serie über alle Pläne

Zweiter Aufruf `/pm-loop` am selben Tag, ohne eigenes Ziel. Die Agenda ist die Folge der
[Sprintplanung vom 2026-11-17](pm-sitzung-2026-11-17-sprintplanung.md): Sprint 87 mit
[B-613](backlog/B-613-rueckdatierter-plan-bricht-die-serie.md) (L),
[B-611](backlog/B-611-wochenplan-sperrt-die-truhe-aller-plaene.md) (S) und
[B-612](backlog/B-612-startseite-zaehlt-belohnungsziele-anders-als-der-server.md) (M), alle im Refinement vom 17.11.
gegrillt und geschätzt. Kein neuer Befund unter `docs/anmerkungen/`. Der Heimnetz-Server auf `:5200` läuft (echte DB)
und blieb unberührt; Messungen nur an einer Kopie und an Wegwerf-DBs.

Die Rollenstimmen sind die der Grill-Runde vom 17.11., nicht neu erfunden:

- **Sohn:** „Mit zwei Fächern sagt die Startseite 2/2, und die Truhe bleibt zu – und wenn Papa etwas nachträgt, ist
  meine Serie weg."
- **Vater:** „Ich will Pläne anpassen und alte wieder einschalten können, ohne mein Kind rückwirkend zu bestrafen."
- **Creator:** kein Pfad in diesem Sprint.

**Sprint-Ziel (Sicht Sohn):** „Mit zwei Fächern sagen Truhe, Bonus und Startseite dasselbe, und meine Serie bricht nicht,
wenn Papa eine Pflicht nachträgt oder einen alten Plan wieder einschaltet."

**Entwickler-Brief:** wie in der Sprintplanung (Reihenfolge B-613 → B-611 → B-612). Senken dieses Sprints:
`PositionProgressService.IsObligationOn`/`DutyBegins`/`ActiveOn` für jeden vergangenen Tag (B-613) und die eine
Liste `RewardObligations(alle Positionen des Kind-Tags)` für Truhe, Bonus und die neuen Zielfelder (B-611/B-612).

## Iteration 87 — umgesetzt

- **B-613 (Backend, Migration `20261118181817_DutySinceActivatedAt`, Kette verlängert):** `StudyPlan.ActivatedAt` und
  `PlanPosition.DutySince`, Backfill `StartDate` bzw. `date(CreatedAt)`. Eine Pflicht zählt für Serie, Missionen,
  Zeitachse und Historie erst ab `DutyBegins` (`IsObligationOn` in `DutyStateAsync`, Takt-Maske in `ComputeDayAsync`);
  ein Plan gilt an einem Tag nur als eingeschaltet ab `ActivatedAt` (`ActiveOn` in `DutyStateAsync` und der Zeitachse);
  der Malus schuldet nach einem Wiedereinschalten nur Perioden ganz danach (`PlanDueForPeriod`, nur wenn
  `ActivatedAt > StartDate` – sonst bliebe die erste angebrochene Woche jedes Plans straffrei). Schreibpfade:
  Plan anlegen/PATCH (nur aus→an; ein verlegter Start eines aktiven Plans zieht das Einschaltdatum auf
  `max(Start, heute)` vor), Position anlegen/PATCH (nur Kür→Takt), Schwächen-Wiederholung, Seed.
- **B-611 (Backend):** `ComputeChildDayAsync` wählt die Belohnungsziele über die flache Liste aller Pläne; `DutyDone`,
  Ziele und Serie bleiben je Plan summiert.
- **B-612 (beides):** `TodayResponse` trägt `rewardGoalsTotal`/`rewardGoalsMet`/`rewardCadence`, `ExtraTrainingOverview`
  `rewardCadence` – aus derselben Liste. Startseite: Balken und Truhen-/Bonus-Text aus den Feldern (`belohnungsZiele`
  entfällt, `truhenZiele` in `pfad.ts`), die Zeilen „Ziele heute/Woche" zählen weiter je Takt. Bonus-Seite: „Erst die
  Wochenziele…" bei Wochen-Sperre. Die Sperrmeldung `extra_training_locked` (Server und Client) nennt keine feste
  Zielart mehr.
- **Probe an einer Kopie der echten DB** (Sicherungs-API, nur lesend geöffnet; alter Code in einem Wegwerf-Worktree
  gegen neuen): eine nachgetragene Pflicht-Position im Bestand (Position 49, Plan 7, Testjunge, Plan inaktiv); Serie aller
  vier Kinder unverändert, vollständige Tage nur beim Testjungen 1 → 3 (Risiko 8: darf nur steigen), kein neuer Malus.

## Review Sprint 87

Beide Reviewer liefen nur lesend. **Keine 🔴.**

| Fund | Quelle | Behandlung |
| --- | --- | --- |
| 🟡 Ein Zukunftsplan, dessen Start der Vater auf heute vorzieht, behielt den alten Start als Einschalttag – verpasste Tage danach kosteten keinen Malus, die Zeitachse zeigte nichts | pugling-reviewer | Behoben: ein verlegter Start eines aktiven Plans zieht `ActivatedAt` auf `max(Start, heute)` vor; Fall `Vorgezogener_Start_Gilt_Ab_Heute_Als_Eingeschaltet` |
| 🟡 B-613 AK 9, letzter Satz: Pflicht → Kür (die letzte Pflicht eines Plans) bricht die Serie rückwirkend | pugling-reviewer | Bestand schon vor dem Sprint, braucht eine Pflicht-Historie (zweite Migration) → [B-659](backlog/B-659-pflicht-wird-kuer-bricht-die-serie.md), bewusst nicht in diesem Sprint |
| 🟡 Startseiten-Fall prüfte den Bonus-Text nicht · Sperrmeldung `extra_training_locked` sagte fest „Tagesziele" · E2E-Kommentar zum Balken versprach mehr, als er hält | frontend-reviewer | Zusicherung ergänzt; Meldung am Server und im Client ohne feste Zielart; Kommentar ehrlich |
| 🟢 Laufzeit-Kreis `SohnBonus` ↔ `SohnHome`, `UntilDone` → „keine", Kommentare, Testtexte | beide | `truhenZiele` nach `pfad.ts` mit `istTagespflicht`, Texte nachgezogen |

**Mutanten:** Endmessung nach der letzten Bearbeitung – Backend **26/26** rot am richtigen Ort, Frontend **6/6**
(Tabellen im Verlauf von B-613). Erster Lauf: U6/W2 „abweichend" (Tippfehler im erwarteten Testnamen, nicht im Test),
U7 grün – eine echte Lücke: kein Fall hielt, dass die erste angebrochene Woche eines Plans fällig bleibt. Fall
`Erste_Angebrochene_Woche_Bleibt_Faellig` ergänzt, im zweiten Lauf rot.

**Verifikation:** `dotnet test Pugling.sln -c Release` **1872/1872**, `npm run build` sauber, Vitest **1228/1228**,
E2E **11 passed** (`belohnung-ueber-faecher` neu, dazu `belohnung-trotz-wochenziel`, `zwei-faecher`, `truhe`, `bonus`,
`pfad`, `vater-zeitachse`, `schwache-woerter-zuweisen`, `nacharbeit-bis-erledigt`, `full-flow`, `sprint-83-rollengang`).
Commit `fe13531c`.

## Runde 4 — Abnahme

Der Server des Rollengangs wurde von Playwright **nach der letzten Bearbeitung** gestartet (Ausweichports 5211/5181,
Release, Wegwerf-DB); der Heimnetz-Server auf `:5200` blieb unberührt.

- **Sohn — abgenommen (B-611, B-612).** Maschineller Rollengang `belohnung-ueber-faecher.spec.ts`: Französisch mit
  Tagesziel, Mathe nur mit Wochenziel; vorher „Schaffe deine Tagesziele…" und Bonus gesperrt, nach dem Französisch-Test
  wartet die Truhe, der Bonus ist offen, der Balken steht auf 100 % (1/1), die Zeilen zählen „heute 1/1, Woche 0/1". In
  seinen Worten: „Wenn ich Französisch geschafft habe, krieg ich meine Truhe – auch wenn Mathe noch offen ist."
- **Sohn/Vater — B-613 abgenommen, Rollengang an der Oberfläche ausgefallen.** Die Fälle brauchen vergangene Tage, die
  nur die Datenbank herstellt. Belegt über die echten Endpunkte (`RueckwirkendePflichtTests`, 15 Fälle: Vater trägt nach,
  datiert zurück, schaltet wieder ein, macht Kür zur Pflicht; das Kind liest seine Serie über `today`) und an einer Kopie
  der echten DB. **Offen für einen Menschen:** beim nächsten Unit-Wechsel in einem echten Plan nachsehen, dass die Serie
  stehen bleibt. **Vor dem nächsten Heimnetz-Start:** der Neustart spielt die Migration `DutySinceActivatedAt` ein.
- **Creator — Regressionszeuge.** Kein Pfad des Creators im Diff; Suite grün.

## Retrospektive (Sprint 87)

**Nachschau:** Sprint 86 (`177027d9`, B-636/B-637) von einem frischen `pugling-reviewer` gelesen – drei Durchrutscher,
zwei am Code bestätigt: [B-666](backlog/B-666-umstellen-auf-bis-erledigt-bucht-malus-doppelt.md) (P2: das Umstellen
einer Nacharbeit von „Tagesziel“ auf „bis erledigt“ bucht den Malus schon abgerechneter Tage noch einmal – die
Doppelbuchungs-Sperre hängt am Takt), [B-667](backlog/B-667-nacharbeit-report-zaehlt-aufgaben-ausserhalb-des-pools.md)
(P3, Report „2/3 erledigt“ bei fertiger Nacharbeit), [B-668](backlog/B-668-leerer-pool-beendet-nacharbeit-nie.md) (P3,
ungeprüft: leerer Pool endet nie). `nachgeschaut: 2026-11-18` an B-636 und B-637. Index: 495 von 525.

- **Was die eigenen Tore durchließen:** ein Tippfehler im erwarteten Testnamen einer Mutante (`Bricht` statt `Bucht`). Die
  Probe meldete U6/W2 „abweichend" – das sah wie eine Testlücke aus und kostete einen zweiten vollen Lauf (~25 Min.).
  Dieselbe Fehlerklasse wie die No-op-Mutante aus Sprint 86: ein Fehler **in der Mutanten-Liste**, den erst das Ergebnis
  verrät. Nicht durchgelassen hat die Probe dagegen U7 – die echte Lücke (erste angebrochene Woche) kam dadurch ans Licht.
- **Von außerhalb der Schleife:** nichts vom Nutzer. Eine parallele Sitzung committete während der Probe (`3f7f0b92`, nur
  Plan-Skripte); die Probe nennt den Stand, an dem sie maß – so fiel es auf. Kein Schaden.
- **Der eine Mechanismus (neues Tor):** `.claude/scripts/mutant-probe.py` bricht vor dem ersten Bau ab, wenn ein
  `fails`-Name in keiner Datei des Repos steht (bei `Klasse.Methode` die Methode, sonst der Text). Probe: dieselbe Liste
  mit dem Tippfehler endet mit Exit 1 und der Meldung, nichts wird gebaut; mit richtigen Namen läuft sie bis zur
  Grundlinie. **Weil es ein neues Tor ist, endet der Lauf hier.**
- **Bewusst ungedeckt:** der Rollengang von B-613 an der Oberfläche (vergangene Tage); die Prüfung sieht nur, ob ein Name
  *existiert*, nicht, ob er zum richtigen Fall gehört.

## Offene Roadmap (priorisiert)

Quelle ist [docs/backlog/](backlog/INDEX.md). **Vor allem anderen [B-666](backlog/B-666-umstellen-auf-bis-erledigt-bucht-malus-doppelt.md)**
(P2, Defekt): es kostet echte Kinder Münzen, sobald der Vater eine laufende Nacharbeit auf „bis erledigt“ stellt – bis
dahin eine Nacharbeit lieber neu als „bis erledigt“ anlegen statt umzustellen. Danach die Folge der Sprintplanung
(Sprint 88). Neu aus dieser Runde: B-659 (P3), B-667 (P3), B-668 (P3).
