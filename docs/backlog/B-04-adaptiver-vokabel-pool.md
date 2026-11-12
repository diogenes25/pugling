---
tags: [typ/story, status/abgenommen, bereich/training, bereich/auswertung, lerntechnik/vokabeln, rolle/supervisor, rolle/student]
aliases: [Adaptiver Pool, Idee 3]
status: abgenommen
nachgeschaut: 2026-11-12
prio: P0
art: Wunsch
groesse: M
wo: beides
migration: ja
vertragsbruch: nein
quelle: docs/backlog-vokabellernen.md#runde-1--idee-3-adaptiver-vokabel-pool-je-position
---

# B-04 · Adaptiver Vokabel-Pool je Position

## User Story

Als Vater möchte ich an einer Vokabel-Position einen größeren Pool (z. B. 100 Vokabeln) mit einer täglichen
Teilmenge (z. B. 20/Tag) hinterlegen, wobei im Abschlusstest nicht gekonnte Vokabeln in den nächsten Tag
nachrücken — sodass über die Laufzeit möglichst jede Pool-Vokabel einmal korrekt im Abschlusstest vorkommt.

## Ist-Stand am Code · Entscheidungen

→ Grill-Protokoll vom 2026-07-30, **Idee 3**, Entscheidungen 8–10:
[backlog-vokabellernen.md](../backlog-vokabellernen.md#runde-1--idee-3-adaptiver-vokabel-pool-je-position).

Kern, belegt: `ItemCount` ist **kein Tageskontingent, sondern ein Abschneiden**
(`PositionPlayService.DueItemIndicesAsync:75-89`, `Enumerable.Range(0, poolSize)` plus Fortschrittsfilter
`p.ItemIndex < poolSize`) — Vokabel 21 bis 100 einer Übung mit `ItemCount = 20` sind **nie** dran.
Umgekehrt macht „Durchgefallenes kommt wieder" **Leitner heute schon**. Es fehlen genau zwei Dinge: ein
Deckel auf die Tagesmenge und ein Pool, der nicht abgeschnitten wird — beides in *dieser einen Methode*.
Die ursprüngliche L/XL-Schätzung kam nicht von der Mechanik, sondern von der ungeklärten Semantik.

## Akzeptanzkriterien

1. `ItemCount` (oder ein Nachfolgefeld) begrenzt den **Pool** und schneidet die Item-Liste nicht mehr ab;
   ein Tagesdeckel begrenzt die **täglich ausgespielte** Menge aus den fälligen Items.
2. Zuvor durchgefallene vor noch nie gezeigten (`WeakestFirst` deckt das vermutlich ab — zu prüfen).
3. Eine im Abschlusstest korrekt beantwortete Pool-Vokabel wird nicht mehr ausgespielt; fällt sie später
   durch, kehrt sie zurück.
4. Fortschrittsanzeige „X von Y geschafft" über die Laufzeit.
5. Ein **`KeyResult`** auf Übungs-Scope kann „Pool durch bis Datum" verbindlich machen.

## Entscheidungen (Fertigstellungs-Lauf 2026-11-10, PM unter Vollmacht des Nutzers)

Der Zuschnitt folgt dem Ziel „vier Wochen täglich Französisch-Vokabeln“ ([Protokoll](../pm-sitzung-2026-11-10-fertigstellung.md)).
Die zwei Nutzer-Entscheidungen vom 2026-10-23 (eigenes Feld; Mischwiederholung zählt nicht) gelten weiter.

1. **Ein Feld `NewItemsPerDay` an der Position (`0` = keine Dosis, wie bisher).** Die Lernrunde spielt die fälligen,
   schon eingeführten Wörter und dazu höchstens so viele nie gezeigte, wie die Tagesdosis nach den heute schon
   eingeführten noch hergibt – in Buchreihenfolge. Kosten: eine Migration (neu gefaltet), ein Feld mehr im Formular.
2. **Mit Tagesdosis prüft der Test die Tagesportion, nicht mehr alle eingeführten Wörter:** eingeführt und
   (heute eingeführt, heute wiederholt oder heute fällig). Ist die Portion leer (nichts fällig), prüft er die
   schwächsten eingeführten Wörter, höchstens zweimal die Tagesdosis; ist noch nichts eingeführt, die ersten
   Wörter in Höhe der Dosis. Begründung: sonst wächst der Pflichttest in vier Wochen auf 150 getippte Antworten
   täglich. Kosten: ein bestandener Test bescheinigt nur noch den Tag, nicht den ganzen Bestand – die
   Gesamtprüfung bleibt Sache der Klassenarbeit/Probeklausur (B-249).
3. **AK 3 („im Test gekonnt → nie wieder ausgespielt“) ist verworfen.** Leitner streckt Gekonntes ohnehin bis
   auf 14 Tage; ein endgültiger Ausschluss nähme dem Vier-Wochen-Plan die Wiederholung, von der er lebt. Kosten: keine.
4. **AK 5 (`KeyResult` „Pool durch bis Datum“) ist zurückgestellt** – dient dem Ziel nicht; bleibt als eigener
   Wunsch offen, falls er gebraucht wird.
5. **AK 4 wird ein additives `IntroducedCount` am Positionsstatus** („37 von 150 eingeführt“), gezeigt in der
   Tagesübersicht des Vaters und beim Kind. Kosten: ein Feld im Vertrag (additiv).
6. **Der Assistent belegt „Neue Wörter je Tag“ mit 10 vor**, änderbar im Feinschliff; bei Übungen mit höchstens
   10 Wörtern ändert das nichts. Kosten: ein Plan aus dem Assistenten verhält sich bei großen Übungen anders
   als vor diesem Lauf – gewollt.
7. **Der Info-Modus bleibt ungedeckelt** (er schreibt nichts, führt nichts ein, zahlt nichts).
8. **Der Test nimmt den offenen Rest der Tagesdosis kalt dazu und führt ihn ein** (Fund des `pugling-reviewer`:
   sonst prüfte ein Kind, das nie übt, vier Wochen lang dieselben ersten Wörter und erfüllte damit die Pflicht).
   Was der Test eingeführt hat, bleibt fällig, bis es geübt ist – wer das Üben auslässt, bekommt jeden Tag einen
   längeren Test (5, 10, 15 …), nie einen kürzeren. Die Vorschau des Vaters führt nichts ein. Kosten: ein Kind,
   das zuerst auf „TEST“ tippt, sieht die neuen Wörter des Tages nicht mehr als Kennenlernkarte.
9. **Eine Dosis verlangt den Leitner-Kasten und passt nicht auf ungeordnete Listen** (`validation_error`). Ohne
   Kasten ist alles Bekannte täglich fällig, die Runde wüchse trotzdem auf den ganzen Pool; bei einer Liste „in
   beliebiger Reihenfolge“ gälte eine richtige Nennung außerhalb der verdeckten Auswahl als falsch.
10. **Unter einer Dosis nimmt die Lernrunde nur Karten der Runde an** – sonst wäre die Dosis per API umgehbar.

## Schätzung

**Größe: M** (vorher L/XL) — zwei Änderungen in `DueItemIndicesAsync`, eine Test-Treffer-Abfrage, ein
additiver Metrik-Wert, eine Fortschrittsanzeige. Die separate Design-Etappe ist nach dem Grillen entfallen.

- **`migration: offen`** — die eine bewusst zurückgestellte Frage: braucht der Tagesdeckel ein **neues
  Feld** (⇒ Migration) oder wird `ItemCount` **umgedeutet** (⇒ keine)? Das ist die erste Entscheidung beim
  Anfassen, nicht ein Versäumnis der Schätzung.
- **Nachbarschaft, am 2026-07-30 aufgelöst:** Kriterium 5 hing ursprünglich an `LearnGoal` — **E13 hat die
  Ebene gelöscht** (`6471e1d`), ihre Rolle trägt jetzt das `KeyResult` eines Objectives. Das ist für diese
  Story eine *Verbesserung*: `KeyResult` belohnt schon idempotent per Lazy Settlement
  (`ObjectiveRewardService`), Kriterium 5 braucht also keine eigene Belohnungsmechanik mehr. Die im
  Grill-Protokoll erwogene Erweiterung „ein additiver `LearnGoalMetric`-Wert" heißt jetzt
  `KeyResultMetric` — und dort ist zu prüfen, ob „im Abschlusstest einmal korrekt" ein neuer Wert ist oder
  auf einen bestehenden abbildet.
- **Nebeneffekt:** Macht B-02 nachträglich gegenstandslos, weil der heutige Hilfetext dann *stimmt*.
- **Testweg:** Integrationstest über mehrere simulierte Tage (Pool 5, Deckel 2): jede Vokabel kommt dran,
  Durchgefallenes kehrt zurück, Gekonntes fällt raus.

## Verlauf

- **2026-07-30** — geerntet aus dem Grill-Protokoll vom selben Tag, Stufe `geschaetzt` übernommen. Beim
  Ernten zusätzlich gefunden: die Abhängigkeit von `LearnGoal` kollidiert mit E13 des DB-Umbaus.
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): Kern gilt (`PositionPlayService.cs:67-91`), aber (1) `PoolSize` wird an vier weiteren Stellen gelesen (`PositionTestsController.cs:135`, `PositionPracticeController.cs:305`, `PositionProgressService.cs:147,185-186`) – nicht „beides in dieser einen Methode“. (2) Der Nebeneffekt zu B-02 ist falsch: B-02 hat den Hilfetext auf „dauerhaft begrenzen“ geändert (`fieldHelp.ts:37-38`), B-04 muss ihn samt `feldhilfe.spec.ts` wieder umschreiben. (3) AK 5 braucht eine neue Datenquelle (Metriken laufen über die kind-weite Mastery, `ObjectiveEvaluationService.cs:105-107`). Neue Überschneidungen: B-218 (Tagesdeckel muss die Mischwiederholung mitzählen), B-219 (ändert die Box-Rücksetzung, auf die AK „Durchgefallenes kehrt zurück“ baut). Eher M–L. **Nicht nachtlauffähig**, solange `migration: offen` – zusammen mit B-218/B-219 grillen. Teilungsvorschlag: (a) Pool + Tagesdeckel + Hilfetext, (b) Ausschluss „im Test geschafft“, (c) `KeyResult`-Metrik.
- **2026-10-23** — **Zwei Entscheidungen des Nutzers** in der Grill-Runde G-5 der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)), sie schließen die zurückgestellte Frage zu `migration: offen`: (1) Der Tagesdeckel bekommt ein **eigenes Feld** an der Position (etwa `NewItemsPerDay`). `ItemCount` behält seine Bedeutung, der Hilfetext aus B-02 bleibt richtig, und deshalb ist `migration: ja`. Der in der Nachprüfung vermutete Umbau von Hilfetext und `feldhilfe.spec.ts` entfällt damit. (2) Die Mischwiederholung (B-218) bringt **nur fällige, bekannte Items** und zählt nicht auf den Tagesdeckel. Die Story ist damit nachtlauffähig (Paket NP-19, allein wegen der Migration); den Teilungsvorschlag (a/b/c) prüft der Lauf beim Schätzen.
- **2026-11-10** — Fertigstellungs-Lauf: P0 für das Ziel des Nutzers, sieben Entscheidungen oben, `in-arbeit` (Sprint 29).
- **2026-11-10** — gebaut (Sprint 29). Backend: `PlanPosition.NewItemsPerDay`, Migration neu gefaltet (Snapshot-Diff: genau die eine Spalte), `PositionPlayService.WithinDailyDose`/`DailyTestPortion`/`IntroduceAsync`, `PositionStatus.IntroducedCount`. Frontend: Feld „Neue Inhalte je Tag“ in Positions-Formular und Assistent (Vorgabe 10), Zeile „x von y schon kennengelernt“ beim Kind.
- **2026-11-10** — Belege je Regel, Mutanten mit `.claude/scripts/mutant-probe.py` nach dem letzten Edit gemessen (alle rot am genannten Test): Dosis greift → `VierWochenTests.VierWochen_…` (M1); heute Eingeführte zählen → ebd. (M2); Test prüft Portion statt Bestand → `Dosis_TestPrueftDieTagesportion…` (M3, exakt 10); erste Dosis kalt → `TagesportionTests.NichtsEingefuehrt…` (M4); Portion vor Rückfall → `Tagesportion_Ist…` (M5); Rückfall gedeckelt → `NichtsFaellig…` (M6); Grenzen 0..200 → `Dosis_AusserhalbDerGrenzen` (M7, M8); Test führt ein → `Dosis_WerNieUebt…` (N1); Vorschau führt nichts ein → `Dosis_VorschauDesVaters…` (N2); Review nur in der Runde → `Dosis_Review_…` (N3); keine Dosis auf Liste → `Dosis_AufUngeordneterListe…` (N4); offener Rest im Test → `OffenerRestDerDosis…` (N5); Dosis nur mit Kasten → `Dosis_OhneLeitner…` (N6).
- **2026-11-10** — `pugling-reviewer` und `frontend-reviewer` gelaufen. Backend: ein fachlicher Blocker (Entscheidung 8), drei „sollte“ (N+1 in `ComputeDayAsync` → eine gruppierte Abfrage; Entscheidungen 9 und 10) – alle eingearbeitet. Frontend: Kopplung Dosis↔Kasten als `withDose`/`withLeitner` herausgezogen und getestet, Assistent sperrt das Feld ohne Kasten. Nicht eingearbeitet: `IntroducedCount` für vergangene Tage zeigt den heutigen Stand (wie `DueCount`), Scope „nur alte“ mit Dosis ist eine nicht abgewiesene Fehlkonfiguration.
- **2026-11-10** — abgenommen. Backend 1211/1211, Vitest 741/741, E2E 134 passed; beide Reviewer gelaufen, Befunde eingearbeitet; Rollengang Sohn und Vater: `VierWochenTests` (28 Tage) und `e2e/ziel-vier-wochen.spec.ts` (Oberfläche, Server nach dem letzten Edit von Playwright gestartet). Commits `595b429`, `1079a96`.
- **2026-11-12** — Nachschau 2026-11-12 (Sprint 31): `WithinDailyDose`/`DailyTestPortion` gegen Entscheidungen 1, 2, 7, 8 nachgerechnet; vier Umgehungen gespielt (nie üben, Test wiederholen, über Mitternacht, alte Sitzung) – halten. **Entgangen:** der Assistent schickt die Dosis auch an ungeordnete Listen → 400 mitten im Anlegen ([B-477](B-477-assistent-dosis-auf-ungeordneter-liste.md)); Reste (Entscheidung 5 nur halb, `IntroducedCount` nach Verkleinern, Umgehung von 9 über die Übung, zwei Kommentare) in [B-478](B-478-nachschau-reste-sprint-29.md).
