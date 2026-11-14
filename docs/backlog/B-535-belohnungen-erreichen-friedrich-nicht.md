---
tags: [typ/story, status/abgenommen, bereich/gamification, rolle/student, rolle/supervisor]
aliases: [Friedrich bekommt keine Münzen, Box gesperrt durch Wochenziele]
status: abgenommen
prio: P1
art: Wunsch
groesse: M
wo: beides
migration: ja
vertragsbruch: ja
quelle: "Nutzer im Chat 2026-11-13: erstes Feedback von Friedrich – Münzen, Belohnung und Zufalls-Kisten motivieren ihn am meisten; im aktuellen Lehrplan erhält er keine Münzen"
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-535 · Die Belohnungen, die Friedrich motivieren, erreichen ihn nicht

Friedrich (Child 3) wird vor allem durch Münzen, Belohnungen und Zufalls-Kisten zum Üben gebracht – genau der
Effekt, den der Vater will. In seinem Plan 6 („Französisch: Au début bis Unité 2") kommt davon fast nichts an.
Gemessen an einer Kopie der echten `pugling.db` vom 2026-11-13 abends:

| Quelle | Friedrich | Zum Vergleich: Testjunge (Child 4) |
|---|---|---|
| Übungs-Münzen (`Base`, `Combo`) | **0** aus 45 Antworten | 465 + 30 |
| Tagesziele (`Goal`) | +140 (7 × 20, alle am 13.11.) | +160 |
| Malus (`GoalPenalty`) | **−60** (12 × 5, für den 11. und 12.11., gebucht beim ersten Öffnen am 13.11. um 19:53) | −20 |
| Tägliche Belohnungsbox | **nie** | 2 × |
| Bonus-Training (B-533) | **nie** – gesperrt | 4 Runden, 26 Münzen |

Sein erster Eindruck war also: zwölf Abzüge, keine Kiste, kein Bonus.

## User Story

Als *Sohn* möchte ich für jede ehrliche Übungsrunde sichtbar Münzen und an guten Tagen eine Kiste bekommen, damit
sich Üben lohnt. Als *Vater* möchte ich, dass gerade die Belohnungen, die mein Kind antreiben, nicht an dem Stoff
hängen, an dem es gerade scheitert.

## Ist-Stand am Code

1. **Die Kiste und das Bonus-Training hängen an der vollen Pflicht – Wochenziele eingeschlossen.**
   `PositionProgressService.cs:214-216` zählt jede Position mit Ziel (täglich *und* wöchentlich) zur Tagespflicht;
   `DailyBoxService.cs:35` vergibt die Box nur bei `DutyDone`, `ExtraTrainingService.cs:194` sperrt das
   Bonus-Training mit derselben Regel (B-533, Entscheidung 1). Friedrich hat am 13.11. alle sechs Tagesziele
   (Vokabeln) erfüllt, aber nur eins von neun Wochenzielen (Grammatik). Solange ein Wochenziel offen ist, gibt es an
   **keinem** Tag der Woche eine Kiste – und offen sind genau die Grammatik-Übungen, an denen er scheitert (Position
   44 „quel, quelle…": 8 Antworten, 0 richtig, 219 s).
2. **Neue Wörter zahlen tagelang nichts.** Alle sechs Vokabel-Positionen haben `RequireTypedTest = true`. Gezahlt
   wird nur, was getippt *und* fällig *und* heute noch nicht gewertet ist
   (`PositionPracticeController.cs:402`). Die ersten Sprossen der Leiter (Kennenlernen, Karteikarte) zahlen bewusst
   nichts (B-96, B-476); danach liegen die Wörter in Box 2, fällig erst am 15.11. Ergebnis: 45 Antworten, 0 Münzen.
   Bezahlt wurde allein das Tagesziel über den Test (11 bestandene Tests, alle 100 %).
3. **Der Malus kam vor dem ersten Erfolg.** Der Plan startet am 11.11., dem Tag, an dem er angelegt wurde; Friedrich
   hat am 11.11. nur angeschaut (getippt „le courtier", „la betange" – LRS) und am 12.11. nicht gespielt. Die
   Abrechnung läuft rückwirkend ab `plan.StartDate` (`PositionProgressService.cs:344`) und buchte beim ersten Öffnen
   am 13.11. zwölf Abzüge in einem Rutsch.

## Die echte Lücke

Keine der drei Regeln ist für sich falsch – sie sind gegen Münz-Farmen gebaut. Zusammen sorgen sie dafür, dass ein
Kind, das den neuen Stoff noch nicht kann, **nur** die Strafe sieht. Der Hebel ist (1): die Kiste ist die
Zufalls-Belohnung, die Friedrich am stärksten motiviert, und sie hängt heute an der Grammatik der ganzen Woche.

## Offene Punkte

1. ~~Woran hängt die Kiste?~~ → Entscheidung 1.
2. ~~Sollen neue Wörter beim ersten Treffer etwas zahlen?~~ → Entscheidung 2.
3. ~~Malus am Starttag / vor dem ersten Login?~~ → Entscheidung 3 und 4.
4. ~~Verhältnis zu B-527?~~ → Entscheidung 5.

## Entscheidungen

Gegrillt im Chat am 2026-11-13 (Nutzer, fünf Fragen, jeweils die Empfehlung gewählt).

1. **Kiste und Bonus-Training hängen nur an den Tageszielen.** Die Tagesbox (`DailyBoxService`) und die Sperre des
   Bonus-Trainings (`ExtraTrainingService`, B-533 Entscheidung 1) prüfen „alle *täglichen* Ziele des Tages
   erfüllt" statt `DutyDone`. Wochenziele behalten ihren eigenen Malus am Ende der Woche. Hat ein Plan **keine**
   Tagesziele, bleibt die heutige Regel (alle Ziele) – sonst gäbe es dort die Kiste geschenkt. `DutyDone` selbst,
   die Serie (Streak) und die Auswertung des Vaters bleiben unverändert. Begründung: Eine Woche schwere Grammatik
   darf nicht sieben Tage Kisten sperren; die Kiste ist Friedrichs stärkster Antrieb. Kosten: Ein Kind kann die
   Wochenziele liegen lassen und trotzdem täglich Kisten holen – einzige Bremse ist der Wochen-Malus; zwei Begriffe
   von „Pflicht erfüllt" im Code (Tagespflicht für Belohnungen, Gesamtpflicht für Serie/Auswertung).
2. **Der erste getippte Treffer eines neuen Wortes zahlt einmal `NewContentPoints`.** Die Regel steht schon in
   `ScoringService.cs:112-113` (`reviewCount == 0`), greift aber nie: die freien Sprossen (Kennenlernen,
   Karteikarte) erhöhen `ReviewCount`, bevor das Wort je getippt wird. Künftig zählt „neu" bis zum ersten
   **gewerteten** Treffer, nicht bis zur ersten Berührung. Kennenlernen, Karteikarte und Auswahl bleiben gratis
   (B-96, B-476 unberührt). Kosten: Die Münze kommt erst auf der Tipp-Sprosse, nicht am ersten Tag; braucht je
   Wort eine Markierung „Neu-Bonus bezahlt" (Spalte an `PositionItemProgress` ⇒ Migration, Bestandszeilen gelten
   als *nicht* bezahlt, weil die Regel bisher nie ausgezahlt hat).
3. **Der Malus läuft ab dem ersten Spielen im Plan an.** Fenster-Beginn in `SettleClosedPeriodsAsync`
   (`PositionProgressService.cs:344`) = der spätere von `plan.StartDate` und dem ersten Tag, an dem das Kind
   irgendeine Position dieses Plans gespielt hat (Übungsrunde oder Test). Vorher: kein Malus. Danach gilt die
   Pflicht für **alle** Positionen, nicht je Position – sonst entginge eine nie geöffnete Position dem Malus.
   Kosten: Ein Kind, das den Plan nie öffnet, bekommt nie Malus; das sieht der Vater nur an der Auswertung
   (Tage ohne Pflicht).
4. **Eine nachträglich hinzugefügte Position zählt erst ab ihrem Anlegen.** Beim Nachsehen gefunden: das Fenster
   beginnt heute bei `plan.StartDate` für jede Position, auch für eine, die erst Wochen später in den Plan kam –
   sie würde rückwirkend bestraft. Fenster-Beginn zusätzlich ≥ `PlanPosition.CreatedAt` (Tag). Kosten: keine
   nennenswerten. Friedrichs schon gebuchte −60 schreibt der Vater per Hand gut (`Manual`); keine Datenmigration.
5. **B-527 bleibt getrennt, B-535 zuerst.** B-535 macht die Kiste erreichbar (Backend), B-527 baut danach das
   Öffnen-Erlebnis darauf. Kosten: die Vergaberegel der Kiste wird zweimal angefasst.

6. **Präzisierung zu Entscheidung 2 nach Messung** (Stakeholder-Entscheidung im Auftrag des Nutzers, 2026-11-14):
   Die Prämisse „die Regel hat bisher nie ausgezahlt" hält nicht – an einer Kopie der echten DB trägt Theodor
   (Child 1) elf `Base`-Buchungen zu je 10 Münzen = `NewContentPoints`. Freie Sprossen erhöhen `ReviewCount` schon
   heute nicht (`climbs`-Zweig setzt ihn zurück); der Neu-Bonus ging verloren, wenn der *erste gewertete* Versuch
   falsch war. Darum setzt die Migration `NewBonusPaidAt` für Bestandszeilen mit `ReviewCount > 0` (ihre eine Chance
   nach alter Regel ist vorbei), Zeilen ohne gewertete Antwort bleiben `null`. Kosten: ein Wort, dessen erster Versuch
   früher falsch war, bekommt den Neu-Bonus nicht nachgezahlt (Friedrich: höchstens vier Wörter); dafür zahlt nichts
   doppelt. Entscheidung 2 selbst (Neu-Bonus beim ersten gewerteten Treffer, genau einmal) gilt unverändert.
7. **Entscheidung 4 war schon gebaut** (B-432, `FirstDueDay` = max(`CreatedAt`, `StartDate`) in
   `PositionProgressService.SettleClosedPeriodsAsync`); AK 6 hält `PflichtMalusTests.Position_SpaeterAngelegt_…`.

## Akzeptanzkriterien

1. Plan mit Tages- und Wochenzielen: alle Tagesziele erfüllt, ein Wochenziel offen ⇒ die Kiste wird gebucht
   (`DailyBoxClaim` + `DailyBoxCoins`) und `GET …/extra-training` meldet `unlocked: true`. Ein Tagesziel offen ⇒
   keine Kiste, Bonus gesperrt.
2. Plan nur mit Wochenzielen: Kiste erst, wenn alle Wochenziele erfüllt sind (heutige Regel).
3. `DutyDone`, Streak und die Auswertung des Vaters zählen weiter alle Ziele (Regressionstest).
4. Ein neues Wort: Kennenlernen und Karteikarte zahlen 0; der erste richtig getippte Treffer bucht
   `NewContentPoints` genau einmal; ein zweiter getippter Treffer (an einem späteren Fälligkeitstag) zahlt den
   normalen Wiederholungswert, nicht erneut den Neu-Bonus.
5. Plan mit `StartDate` vor dem ersten Spielen: für Tage vor dem ersten Spielen im Plan wird kein Malus gebucht;
   ab dem ersten Spieltag für jede verpasste Pflicht jeder Position wie bisher – auch für Positionen, die das Kind
   nie geöffnet hat.
6. Eine Position, die nach dem Startdatum angelegt wurde, wird für Perioden vor ihrem Anlegen nicht bestraft.

## Schätzung

**Größe: M** (oberer Rand) – drei kleine, voneinander unabhängige Regeländerungen im Backend, je mit eigenem Test,
dazu eine Spalte und ein Vertragsfeld. Vergleich: mehr als der Batch-Pfad im `MediaSelector` (B-03, M), weniger als
eine DB-Umbau-Etappe (E6, L) – die Migration ist reines Anhängen einer nullbaren Spalte.

- **`migration: ja`** – Entscheidung 2 braucht je Wort die Markierung „Neu-Bonus bezahlt": nullbare Spalte
  `PositionItemProgress.NewBonusPaidAt` (`DateTime?`), neue Migration an die Kette; Bestandszeilen bleiben `null`
  (= nicht bezahlt, wie entschieden) – ~~überholt durch Entscheidung 6~~: die Migration markiert Zeilen mit
  `ReviewCount > 0` als bezahlt. Kein Umschreiben vorhandener Zeilen.
- **`vertragsbruch: ja`** – die Arcade rechnet die Bonus-Sperre heute selbst nach
  (`frontend/src/sohn/SohnHome.tsx:125`: `today.dutyDone || today.goalsTotal === 0`). Nach Entscheidung 1 wiche sie
  vom Server ab. Darum bekommt `DayOverview` (`Pugling.Contracts/Shared/ProgressDtos.cs:32`) ein Feld
  `RewardsUnlocked` (Tagespflicht für Belohnungen), das Server, Kiste und Bonus-Sperre aus **einer** Methode lesen;
  `contract.ts` neu erzeugen, `SohnHome` liest das Feld statt nachzurechnen. Additiv in der Antwort – kein Client
  bricht, aber `Contracts` ändert sich.

**Risiken**

- Zwei Begriffe „Pflicht erfüllt" im Code (Entscheidung 1). Gegenmittel: genau eine Methode berechnet
  `RewardsUnlocked`, `DailyBoxService` und `ExtraTrainingService.cs:194` rufen nur sie; Regressionstest, dass
  `DutyDone`/Streak unverändert alle Ziele zählen.
- „Erster Spieltag im Plan" (Entscheidung 3) kostet eine Abfrage über `PracticeSessions` und `TestAttempts` je
  Plan in `SettleClosedPeriodsAsync` – einmal je Plan, nicht je Position und Periode, sonst wächst es mit beiden.
- Neu-Bonus im Mengen-Modus (`GradesAsSet`): gutgeschrieben wird der *gutgeschriebene* Eintrag, nicht die Karte –
  die Markierung muss an `prog` des gutgeschriebenen Index hängen (wie `ApplyReview`).
- Die Kiste wird pro Kind, aber aus der Übersicht *eines* Plans vergeben (`DailyBoxService.cs:35`); bei mehreren
  spielbaren Plänen bliebe das so – heute ist genau einer spielbar (Anti-Cheat), keine Änderung.

**Angriffsplan (Backend zuerst)**

1. `RewardsUnlocked` in `PositionProgressService.ComputeDayAsync` (Tagesziele bzw. alle Ziele, wenn es keine
   Tagesziele gibt) + Feld in `DayOverview`; `DailyBoxService` und `ExtraTrainingService` darauf umstellen.
2. Malus-Fenster in `SettleClosedPeriodsAsync`: Beginn = max(`plan.StartDate`, erster Spieltag im Plan, Tag von
   `pos.CreatedAt`); ohne Spieltag kein Malus.
3. Spalte + Migration; `PositionPracticeController` zahlt `NewContentPoints` beim ersten gewerteten Treffer
   (`scored && wasCorrect && NewBonusPaidAt is null`) und setzt die Markierung; `ScoringService` bekommt „neu" als
   Eingabe statt `reviewCount == 0`.
4. Vertrag/Client/`contract.ts` nachziehen, `SohnHome` auf `rewardsUnlocked`.
5. Tutorials/Doku-Capture prüfen (`DayOverview` steht in `docs/api-examples`).

**Testweg**

- `DailyBoxTests`: AK 1 (Wochenziel offen ⇒ Kiste), AK 2 (nur Wochenziele ⇒ alte Regel).
- `ExtraTrainingTests`: AK 1 (Bonus `unlocked: true` bei offenem Wochenziel).
- `PflichtMalusTests`: AK 3-Regression (Streak/`DutyDone`), AK 5 (kein Malus vor erstem Spieltag; danach auch für
  nie geöffnete Position), AK 6 (nachträglich angelegte Position).
- `ScoringServiceBoundaryTests` bzw. ein Integrationstest am Übungs-Endpunkt: AK 4 (Kennenlernen/Karteikarte 0,
  erster getippter Treffer genau einmal `NewContentPoints`, zweiter Treffer Wiederholungswert).
- `SchemaGuardTests` (Migration ohne Drift), E2E `bonus.spec.ts` (Einstieg offen bei offenem Wochenziel).
- Je Fall die rote Probe am richtigen Ort (welche Änderung macht ihn rot) – siehe CLAUDE.md.

## Verlauf

- **2026-11-13** — angelegt und ausformuliert aus Friedrichs erstem Feedback; Ist-Stand an einer Kopie der echten
  DB gemessen (Ledger, Box-Claims, Review-Events, Positionen von Plan 6).
- **2026-11-13** — gegrillt im Chat (fünf Entscheidungen); beim Nachsehen gefunden und als Entscheidung 4
  aufgenommen: nachträglich hinzugefügte Positionen werden rückwirkend ab Planstart bestraft.
- **2026-11-13** — geschätzt: M, Migration ja (nullbare Spalte), Vertrag ja (`DayOverview.RewardsUnlocked`,
  weil `SohnHome.tsx:125` die Bonus-Sperre selbst nachrechnet).
- **2026-11-14** — Backlog-Lauf, Sprint 73: gebaut. `DayOverview.RewardsUnlocked` (eine Stelle,
  `PositionProgressService.RewardObligations`), Kiste und Bonus-Sperre lesen nur sie, die Bonus-Sperre nennt nur die
  Tagesziele; Malus-Fenster beginnt beim ersten eigenen Spieltag des Kindes im Plan (`FirstPlayDaysAsync`:
  `IntroducedAt` oder eigener Test, Vater-Test zählt nicht); `PositionItemProgress.NewBonusPaidAt` + Migration
  `20261114090616_NewBonusPaidAt` (Bestandszeilen siehe Entscheidung 6); `SohnHome` liest `rewardsUnlocked`.
  Tests: `BelohnungenTagespflichtTests` (3), `PflichtMalusTests` (+2, Bestandsfälle spielen ab Tag 1), E2E
  `belohnung-trotz-wochenziel.spec.ts`.
- **2026-11-14** — Review Sprint 73 (`pugling-reviewer`, `frontend-reviewer`), im Sprint behoben: (1) die
  Einführungstag-Regel (B-432) hing noch am Planstart und konnte am ersten Spieltag – dem ersten abgerechneten Tag –
  nie greifen: Einführungstag = max(erster Fälligkeitstag, erster Spieltag), Test
  `Einfuehrungstag_Ist_Der_Erste_Spieltag_Wenn_Der_Plan_Frueher_Begann`; (2) `FirstPlayDaysAsync`-Doku auf das
  eingeengt, was gilt (erste eigene, einem Atom zugeordnete Antwort oder eigener Test – eine Runde ohne Antwort oder nur
  Fehlversuche in einer Mengen-Übung starten die Uhr nicht); (3) Texte „today's duty“ → „today's daily goals“ an
  Kiste/Bonus-Sperre; (4) die Ziele-Karte der Arcade zählt Tages- und Wochenziele getrennt („Ziele heute 1 / 1“,
  „Ziele diese Woche 0 / 1“), sonst stand „1 / 2“ neben offenem Bonus. Bewusst so: eine Nachtrag-Runde des Vaters
  verbraucht den Neu-Bonus des Kindes (B-542: sie zählt fürs Kind), startet aber die Malus-Uhr nicht (Vater-Tests
  zählen nicht als Spiel des Kindes).
- **2026-11-14** — **abgenommen** (Sprint 73). volle Suite 1612/1612 (Release), Vitest 1000/1000, `tsc -b` grün, Rollengang Sohn als Playwright gegen frisch gestartete Server nach dem letzten Edit: 9 passed (`belohnung-trotz-wochenziel`, `bonus`, `bonus-deckel*`, `pfad`, `lektion`, `erstbegegnung`, `full-flow`); Reviews `pugling-reviewer` + `frontend-reviewer` (Funde im Sprint behoben); Commit `fa335075`. Mutanten (mutant-probe, je AK-Fall, alle rot am benannten Test):
  M1 `RewardsUnlocked`=`DutyDone`, M2/M3 `RewardObligations` immer alle/immer täglich, M4 `DutyDone` nur Tagesziele (AK 3),
  M5 Kiste liest `DutyDone`, M6 Bonus-Sperre ungefiltert, M7 ohne Spieltag kein Abbruch, M8 Fenster ohne Spieltag,
  M9 Vater-Test zählt, M10 „neu“ = `ReviewCount == 0`, M11 Markierung fehlt, M12 Neu-Bonus ohne Markierung,
  M14 Arcade liest `dutyDone` (E2E), M15 Einführungstag am Planstart (Review-Fund), M24 Ziele-Karte gemischt (E2E).
  Migration gegen eine Kopie der echten DB geprobt (läuft durch, Backfill 70/35). Ausgefallen/Gerät: keiner; Friedrichs
  −60 schreibt der Vater per Hand gut (Entscheidung 4), das ist ein Handgriff am echten Konto.
