---
tags: [typ/protokoll, bereich/backlog]
---

# PM-Sitzung: Sprintplanung nach dem Refinement vom 2026-11-17

**Datum:** 2026-11-17  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (Student) · Entwickler
**Ziel:** Die am 2026-11-17 geschätzten Stories zu Sprints schneiden, die nacheinander gebaut werden können.

## Auftrag und Ausgangswert

„Bereite entsprechende Sprints vor" – nach der Backlog-Sichtung vom selben Tag. Das ist **nur Planung**: kein
Bau, kein Rollengang, keine Statusänderung an Stories. Die Rollen-Runde (Step 2) entfällt, weil kein Stand der
App seit Sprint 84 neu zu bewerten ist; die Reibung steckt schon in den Stories (Grill-Runden mit dem Vater vom
selben Tag).

**Ausgangswert:** 58 offen (Index vom 2026-11-17, Stand `8fb3b69b`). Letzter gebauter Sprint: 84
([Nachtlauf 2026-11-17](pm-sitzung-2026-11-17-nachtlauf.md)). Der dort genannte „Sprint 85" (B-611, B-612, B-614)
ist nie gestartet; die Nummer wird hier neu vergeben, die drei Defekte stehen unter „Refinement".

## Schnitt-Regeln

- Höchstens sechs Stories je Sprint, alle auf `geschaetzt` (pm-loop, „The Sprint").
- **Feste Reihenfolgen aus den Stories** (nicht verhandelbar ohne neue Entscheidung):
  - B-635 vor B-636, B-637 und B-643 (Zustand „erledigt" und `easier` kommen aus B-635).
  - B-628 vor B-629 und B-630 (API-First).
  - B-621 und B-622 **zusammen ausliefern** (B-622 Risiko 1: die Sohn-Startseite verzweigt hart nach Taktung).
  - B-624 → B-626 → B-625 (B-626 Entscheidung 7; B-625 „erst bauen, wenn beide abgenommen sind").
- Höchstens ein L je Sprint; zwei Migrationen in einem Sprint nur sequenziell (die Kette wird verlängert, nie
  gefaltet).
- Reihenfolge der Sprints nach Prio; bei Gleichstand der, der eine frisch gebaute Grundlage weiterbenutzt.

## Sprint-Folge

| Sprint | Roter Faden | Umfang | Größen | Migration / Vertrag | Prio |
| --- | --- | --- | --- | --- | --- |
| 85 | Schultest nacharbeiten I: das Kind spielt die Nacharbeit | B-635, B-633, B-634 | M + S + S | B-635 Migration | P1 |
| 86 | Schultest nacharbeiten II: Pflicht bis erledigt, Vater sieht den Stand | B-636, B-637 | M + S | B-636 Migration | P1 |
| 87 | Rückmeldung zur Karte: Sohn meldet, Vater erledigt | B-628, B-629 | L + M | B-628 Migration | P1 |
| 88 | Der Stundenplan erzeugt die Pflicht | B-621, B-622 | L + M | Migration **und** Vertragsbruch | P1 |
| 89 | Plan aus Unit in einem Schritt | B-580 | L | — | P1 |
| 90 | Der KI-Creator bekommt bessere Eingaben | B-630, B-631 | M + M | — | P1 / P2 |
| 91 | Lernkarte: Runden, bis jedes Wort aus dem Gedächtnis sitzt | B-643 | L | Migration | P2 |
| 92 | Antwortvergleich I: Akzent halb, Platzhalter dürfen fehlen | B-624, B-626 | L + M | B-624 Migration | P2 |
| 93 | Antwortvergleich II: ein Feld je Bedeutung, Dezimalkomma | B-625, B-639 | L + M | — | P2 |
| 94 | Fächer teilen ein Tagesbudget | B-618 (+ B-619 nach Schätzung) | L (+ ?) | Migration | P2 |

**Begründung der Reihenfolge:** Die Nacharbeit (85–86) zuerst, weil sie vier P1-Stories trägt und Theodors
verpatzten Unit-1-Test (9/20; Ursprung [B-632](backlog/B-632-schultest-nacharbeiten-mit-leichter-schalten.md))
sofort nutzbar macht. Die Rückmeldung (87) vor dem Stundenplan (88), weil sie keinen Vertragsbruch trägt und die
Kinder ab dem ersten Tag schlechte Karten melden können – das speist später B-630. B-643 (91) zieht vor die beiden
anderen P2-Sprints, weil sie auf B-635 aufsetzt und sonst deren Mechanik neu gelesen werden müsste. B-618 (94)
zuletzt: ihre Schätzung ist nach B-621 nachzusehen (B-618, Risiko 6), und B-619 ist noch nicht geschätzt.

## Sprint 85 — Ziel & Umfang

**Sprint-Ziel (Sicht Sohn):** „Ich arbeite meinen verpatzten Vokabeltest als Nacharbeit ab: Bei jeder Lücke steht
das deutsche Wort, mehrteilige Lösungen kommen als Buchstabensalat je Wort, ich darf selbst leichter schalten – aber
erledigt ist eine Aufgabe erst, wenn ich sie frei getippt habe, und es geht Runde um Runde weiter, bis alles erledigt
ist."

**Umfang:** [B-635](backlog/B-635-nacharbeit-leichter-schalten-und-runden.md) (M),
[B-633](backlog/B-633-lueckentext-deutsche-vorgabe-je-luecke.md) (S),
[B-634](backlog/B-634-buchstabensalat-je-wort.md) (S).

**Entwickler-Brief:**

- **Reihenfolge:** B-633 → B-634 → B-635. Die beiden kleinen zuerst, weil B-635s Rundenlogik sonst gegen eine
  Karte gebaut wird, die sich danach noch ändert (Vorgabe je Lücke, Kacheln je Wort).
- **Quelle der Wahrheit:** Die Stufe wählt nur der Server (B-632 Entscheidungen). `easier` neben `review` in
  `PositionPracticeController`; `Review` prüft gegen die **geführte** Stufe, nicht gegen eine vom Client behauptete.
  B-633: die Vorgabe schlägt den Vokabel-Speicher in `ResolveClozeRefsAsync`/`ClozeExerciseType.ItemsOf`.
  B-634: die Regel sitzt in `StageMechanics` (Senke für alle Typen).
- **Guards:** Anti-Cheat B-635 AK 5 (eine falsch deklarierte Antwort erledigt nichts); die Pflicht-Regel anderer
  Positionen bleibt unverändert; einwortige Karten aller Typen ändern sich durch B-634 nicht.
- **Migration:** nur B-635 (Positionsschalter, Stufe je Karte als JSON-Spalte **mit `ValueComparer`**, Tor G7,
  Erledigt-Zähler). Probe gegen eine **Kopie** der echten `pugling.db`.
- **Vertrag:** additiv (`Gap.Prompt`, Kennzeichen an `PracticeCard`, Wortgrenzen an der Kachelliste) – Client und
  Editor ziehen mit, sonst greift `unknown_field`.
- **Testweg:** `RemediationPositionTests` (neu), `StageMechanicsTests`, `ClozePlayTests`, Vitest `SohnPractice`,
  E2E `nacharbeit.spec.ts` (zugleich der Rollengang Sohn; gezählt nach „N passed").
- **Reviewer:** `pugling-reviewer` und `frontend-reviewer` (alle drei `wo: beides`).

## Sprint 86 — Ziel & Umfang

**Sprint-Ziel (Sicht Vater):** „Ich stelle die Nacharbeit als Pflicht ‚täglich, bis erledigt' ein; sie endet von
selbst mit einem Abschlussbonus, und ich sehe je Aufgabe, auf welcher Stufe mein Kind sie gelöst hat und ob sie
erledigt ist."

**Umfang:** [B-636](backlog/B-636-pflicht-bis-erledigt-mit-abschlussbonus.md) (M),
[B-637](backlog/B-637-nacharbeit-vater-sicht-je-aufgabe.md) (S).

**Entwickler-Brief:**

- **Reihenfolge:** B-636 → B-637; beide lesen den Erledigt-Stand aus B-635.
- **Quelle der Wahrheit:** Die neue Taktung zieht durch Pflicht-Regel, Tagesmission, Malus-Abrechnung und Buchung –
  **Regel an der Senke**: per `grep` jede Verzweigung nach `GoalCadence` finden und je Aufrufer entscheiden
  (pm-loop Step 4, geteilte Helfer).
- **Guards:** kein Malus nach dem Ende, auch nicht rückwirkend; der Bonus bucht genau einmal (idempotent nach dem
  Vorbild `PositionGoalReward`); Wallet-Pfade bumpen `ConcurrencyStamp`. B-637: fremdes Kind sieht nichts.
- **Migration:** B-636 (Bonushöhe an `PlanPosition`, ggf. Bonus-Tabelle). Probe gegen Kopie.
- **Testweg:** Integrationstests in `RemediationPositionTests`, Ownership-Test, Vitest der Vater-Tabelle; Rollengang
  Vater per E2E (Position anlegen → Kind erledigt → Bonus und Tabelle).
- **Reviewer:** `pugling-reviewer`; für B-637 zusätzlich `frontend-reviewer`.

## Sprint 87 — Ziel & Umfang

**Sprint-Ziel (Sicht Sohn → Vater):** „Ist eine Karte falsch oder unklar, melde ich das mit einem Knopf und Grund;
mein Vater sieht die Meldung, erledigt sie, und ich sehe danach einen Hinweis, dass sie bearbeitet wurde."

**Umfang:** [B-628](backlog/B-628-rueckmeldung-backend.md) (L), [B-629](backlog/B-629-rueckmeldung-frontend.md) (M).

**Entwickler-Brief:**

- **Reihenfolge:** B-628 vollständig (inkl. Doku-Capture) vor B-629 – API-First.
- **Quelle der Wahrheit:** Student-`POST` mit Eigentums-Check nach `RemarksController`; Supervisor-Liste und
  `PATCH` *erledigt*; Creator-Zählung.
- **Guards:** je Ebene „fremd sieht nichts"; eine Meldung ändert **nichts** an Kasten, Wallet und Pflicht-Report
  (B-628 AK 2, Vorher/Nachher). Das Nachbuchen ist bewusst ausgelagert ([B-627](backlog/B-627-berechtigte-meldung-bucht-nach.md)).
- **Migration:** B-628 (Meldungs-Entität, `SetNull` an der Karte); `SchemaGuardTests`.
- **Testweg:** `ExerciseFeedbackTests` (neu), `/smoke-test`, E2E `rueckmeldung.spec.ts` (zugleich Rollengang).
- **Reviewer:** `pugling-reviewer`, dann `frontend-reviewer`.

## Sprint 88 — Ziel & Umfang

**Sprint-Ziel (Sicht Vater):** „Ich trage den Stundenplan ein, und an Schultagen des Fachs entsteht die Pflicht von
selbst – nie mehr als die Obergrenze je Tag; mein Sohn sieht in der App, warum heute diese Pflicht gilt."

**Umfang:** [B-621](backlog/B-621-stundenplan-pflicht-backend.md) (L),
[B-622](backlog/B-622-stundenplan-pflicht-vater-web-und-sohn-app.md) (M) – **gemeinsam ausliefern**.

**Entwickler-Brief:**

- **Reihenfolge:** B-621 → B-622, ein Commit-Paar ohne Zwischenstand auf `main`, der die Sohn-App mit einem
  unbekannten Taktungswert zeigt.
- **Quelle der Wahrheit:** Die Taktung ist Teil des Periodenschlüssels und verzweigt an 16 Stellen
  (B-621 Schätzung) – jede per `grep` gefunden und im `## Verlauf` je Aktion belegt.
- **Guards:** eine Stundenplan-Änderung wirkt nur ab heute (Entscheidung 8), kein Malus für vergangene freie Tage;
  die Position nimmt täglich an der Budget-Verteilung teil (Entscheidung 9 – bis B-618 steht, ist das ein No-op,
  im Verlauf benennen).
- **Migration und Vertragsbruch:** Obergrenze am Kind; `GoalCadence` bekommt einen vierten Wert → Client, Agent
  (`PlanPlanner`, `PlanAssigner`), alle handgeführten Listen im Frontend.
- **Testweg:** wie in den Stories; Rollengang Vater **und** Sohn (beide Ebenen ändern sich).
- **Reviewer:** beide.

## Sprint 89 — Ziel & Umfang

**Sprint-Ziel (Sicht Vater):** „Ich wähle eine Unit aus dem Lehrwerk, und in einem Schritt steht ein aktiver
Vier-Wochen-Plan im richtigen Fach, ohne dass meine Pläne in anderen Fächern abgeschaltet werden."

**Umfang:** [B-580](backlog/B-580-plan-aus-unit.md) (L). Voraussetzung B-539 Teil A ist seit Sprint 84 abgenommen.

**Entwickler-Brief:** Neuer Endpunkt mit Transaktion; die Positions-Prüfungen aus `PlanPositionsController.Create`
**in einen gemeinsamen Dienst ziehen** statt doppeln (B-580 Risiko „Prüfungen doppelt"); Wizard-Weg und das
Agenten-Verb ziehen nach. Kein Schema, kein Bruch. Reviewer: beide.

## Sprint 90 — Ziel & Umfang

**Sprint-Ziel (Sicht Creator):** „Wenn ich für ein Kind Übungen erzeugen lasse, kennt der Agent dessen gemeldete
Probleme, und die Wortlisten aus dem Buchimport tragen die Wortart."

**Umfang:** [B-630](backlog/B-630-rueckmeldung-im-ki-creator.md) (M, nach B-628),
[B-631](backlog/B-631-buchimport-fuellt-vokabel-store-nur-flach.md) (M, `wo: doku`).

**Entwickler-Brief:** B-630 liest nur die Endpunkte aus B-628 über `Pugling.Client`; Regeln am Briefing
deterministisch prüfen, das Modell nur stichprobenhaft (Risiko 1), Obergrenze des Faktenblocks (AK 3). B-631 ändert
nur den Skill `lehrwerk-aufbereiten` und die TSVs unter `raw/` (gitignored), Nachtrag per Batch-PATCH gegen die
API – an der echten DB nur mit Sicherung und nach Freigabe des Vaters. Reviewer: `pugling-reviewer` für B-630;
B-631 braucht grünen Markdown-Lint und `test_laden.py`.

## Sprint 91 — Ziel & Umfang

**Sprint-Ziel (Sicht Sohn):** „Neue Wörter zählen erst, wenn ich sie aus dem Gedächtnis weiß; bis dahin darf ich
mir helfen lassen, und die Lernkarte holt sie Runde um Runde zurück."

**Umfang:** [B-643](backlog/B-643-lernkarte-leichter-schalten-und-runden-bis-alles-erinnert.md) (L).

**Entwickler-Brief:** Fundament (Stufe je Karte, `easier`) aus B-635 wiederverwenden, nicht nachbauen. Migration
`ImprintedAt` an `PositionItemProgress` mit **Backfill aus `IntroducedAt`** (Bestand der Söhne!). Kernänderung an
der Kartenwahl („wann ist ein Wort neu?") – jeder Aufrufer einzeln. Ring zählt „eingeprägt". Reviewer: beide.

## Sprint 92 — Ziel & Umfang

**Sprint-Ziel (Sicht Sohn):** „Ein vergessener Akzent kostet mich im Üben nur einen halben Fehler, und ‚sth.' oder
‚jdn.' muss ich nicht mittippen."

**Umfang:** [B-624](backlog/B-624-akzentfehler-nicht-ganzer-fehler.md) (L),
[B-626](backlog/B-626-antwort-verlangt-mehr-als-die-vokabel.md) (M).

**Entwickler-Brief:** B-624 → B-626. Der dritte Review-Ausgang *halb* läuft durch Combo, Punkte, Kasten,
Lernstand und Report; der **Test bleibt binär** (Friedrichs Schulformat). Migration B-624: Schalter an
`PlanPosition` (Bestand **an** per `UPDATE`, kein DB-Default, Tor G9) und Kennzeichen an `ReviewEvent`. B-626:
Vergleich und Maske an der geteilten Stelle, Datenkorrektur über die API. Reviewer: beide bzw. `pugling-reviewer`.

## Sprint 93 — Ziel & Umfang

**Sprint-Ziel (Sicht Sohn):** „Hat ein Wort zwei Bedeutungen, tippe ich jede in ihr eigenes Feld; und ‚2,5' ist bei
Mathe richtig."

**Umfang:** [B-625](backlog/B-625-ein-eingabefeld-je-bedeutung.md) (L, nach B-624 und B-626),
[B-639](backlog/B-639-dezimalkomma-im-zahlenvergleich.md) (M).

**Entwickler-Brief:** Gemeinsame Senke ist `AnswerGrader` – beide Stories greifen dort ein, darum ein Sprint und
**sequenziell** (B-639 zuerst, klein und reiner Backend-Eingriff; danach B-625 quer durch alle vier Tipp-Pfade über
`Lektion.tsx`). Reviewer: beide.

## Sprint 94 — Ziel & Umfang (vorläufig)

**Sprint-Ziel (Sicht Vater):** „Ich verteile das Tagesbudget neuer Wörter in Prozent auf die Fächer meines Kindes."

**Umfang:** [B-618](backlog/B-618-faecher-verteilung-backend.md) (L); [B-619](backlog/B-619-faecher-verteilung-vater-web-und-sohn-app.md)
kommt dazu, sobald es geschätzt ist. **Vor dem Start** B-618s Schätzung gegen den Stand nach Sprint 88 nachsehen
(ihr Risiko 6).

## Nicht eingeplant – mit Grund

| Id | Grund | Was es freigibt |
| --- | --- | --- |
| [B-620](backlog/B-620-friedrich-englisch-lehrwerk-und-plan.md) | kein Code, läuft **sofort** neben Sprint 85: Access 5 ist fotografiert, aufbereitet und gegen eine Wegwerf-DB geprüft (27 Übungen, je 100 %) | Freigabe und PIN des Vaters fürs echte Laden, dann der Englisch-Plan neben Plan 8 |
| [B-31](backlog/B-31-geraete-vorbehalt-klang.md) | Gerätecheck durch einen Menschen am Handy | Gegenhören, dann abnehmen |
| [B-619](backlog/B-619-faecher-verteilung-vater-web-und-sohn-app.md) | erst `gegrillt` | `/backlog B-619` (schätzen) |
| [B-536](backlog/B-536-aufbau-uebungen-statt-versagen.md) | `gegrillt`, XL – muss vor dem Schätzen geteilt werden | Teilung im Dialog |
| B-223, B-377 | P3 | nach Sprint 94 |
| Ideen B-638, B-640–B-642 (Mathe) | `idee` | Ausformulieren; B-639 ist der Defekt davon |

## Refinement (eigene Phase, nicht im Sprint gezählt)

Die P2-Defekte aus der Nachschau von Sprint 83 sind noch `idee`, betreffen aber den Kernkreis Truhe/Serie/Startseite:
[B-611](backlog/B-611-wochenplan-sperrt-die-truhe-aller-plaene.md),
[B-612](backlog/B-612-startseite-zaehlt-belohnungsziele-anders-als-der-server.md),
[B-613](backlog/B-613-rueckdatierter-plan-bricht-die-serie.md), dazu P3
[B-614](backlog/B-614-startseite-gruppiert-nach-uebungsfach-statt-planfach.md) und B-607–B-609. Als `Defekt` sind
sie unter Freigabe autonom grillbar. **Empfehlung:** sie bis `geschaetzt` bringen, solange Sprint 85 läuft; stehen
sie, zieht ein Defekt-Sprint „Truhe und Serie stimmen über alle Pläne" (B-611, B-612, B-613, ggf. B-614) **vor**
Sprint 87 – Korrektheit im Kernkreis schlägt neue Funktion (pm-loop Step 3).

## Offene Roadmap

Quelle ist [docs/backlog/](backlog/INDEX.md); diese Tabelle ist eine datierte Momentaufnahme. Der nächste Schritt ist
Sprint 85.
