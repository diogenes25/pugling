---
tags: [typ/story, status/geschaetzt, bereich/lehrplan, rolle/supervisor, rolle/creator]
aliases: [Plan aus Unit, Kapitel-Plan in einem Schritt, Vier-Wochen-Plan]
status: geschaetzt
prio: P1
art: Wunsch
groesse: L
wo: beides
migration: nein
vertragsbruch: nein
quelle: B-537 (Teil c, Entscheidungen 8–10)
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-580 · Plan aus Unit: ein Kapitel wird in einem Schritt zum Vier-Wochen-Plan

Teil c von [B-537](B-537-lehrplan-als-kapitel-pflicht-und-kuer.md). Begründungen der Nutzer-Entscheidungen stehen
dort. **Braucht [B-539](B-539-ein-aktiver-plan-je-fach.md)** (Fach Pflicht und Unit am Plan, Abschaltung je Fach) –
wird B-539 geteilt, braucht diese Story dessen Teil A.

## User Story

Als *Vater* möchte ich zu Beginn einer neuen Unit mit einem Schritt einen Vier-Wochen-Plan bekommen – tägliche
Vokabeln, wöchentliche Grammatik, der Rest als Bonus –, damit mein Kind das Kapitel verlässlich durcharbeitet, ohne
dass ich jede Übung einzeln plane. Als *Creator* (KI-Agent) möchte ich nur den Katalog der Unit füllen und den
Plan vom Server bauen lassen.

## Ist-Stand am Code

Gemessen am 2026-11-15 (Stand `d41df00a`), Pfade relativ zu `backend/`.

- **Kein `from-unit` irgendwo** (Suche nach `from-unit`/`FromUnit` in Backend, Client, Frontend: kein Treffer).
- **Plan anlegen** nur als leerer Container: `Pugling.Api/Controllers/Supervisor/StudyPlansController.cs:88-114`;
  Positionen einzeln über `Pugling.Api/Controllers/Supervisor/PlanPositionsController.cs:148-230` mit allen Prüfungen
  (Ausführungsrecht `:156-157`, leere Übung `:162-164` → `exercise_empty`, Stufe, getippt, Dosis `:176-178`).
- **Der KI-Agent baut den Plan heute selbst** (`Pugling.Agent.Creator/PlanPlanner.cs`): erzeugt Übungen
  (`:158-196`), legt den Plan **inaktiv** an (`:211-221`), dann je Übung eine Position, **alle `Daily`** (`:230-257`,
  `Cadence: GoalCadence.Daily` `:240`). Für eine Unit mit Buch-Wortliste plant er deren Übungen ganz ein statt Scheiben
  zu erzeugen (`:159-170`, B-463) – Position mit Stufe 4 (getippt), `RequireTypedTest`, Leitner und Tagesdosis
  `max(1, ⌈Karten / Tage⌉)` (`:283-305`). Eine Wortliste ist eine nicht generierte Vokabel-Übung, die mindestens ein
  neues Wort beiträgt (`Pugling.Agent.Creator/Briefing/BriefingBuilder.cs:145-169`, Marke
  `Drafting/DraftPrompts.cs:39` „Vom KI-Creator“). Vorgabe-Laufzeit 14 Tage
  (`Pugling.Agent.Creator/AgentCommands.cs:26`, `:371`); Intensitäten Locker/Normal/Intensiv als Kopie der Wizard-Zahlen
  (`PlanPlanner.cs:110-127` ↔ `frontend/src/vater/VaterWizard.tsx:47`).
- **Der Vater-Wizard** (`frontend/src/vater/VaterWizard.tsx`) wählt Übungen über Fach/Klasse/Schulart aus dem Katalog
  (`:167-178`), „Thema / Kapitel“ ist nur ein Freitext (`:527`); jede gewählte Übung wird ein **Tagesziel** (`:402-415`),
  `wizardFinish.ts` legt die Positionen einzeln an (`:135-158`). Eine Unit als Quelle kennt er nicht.
  `frontend/src/vater/VaterLehrwerke.tsx:103` (`createExerciseHref`) zeigt das Muster „Auswahl reist als Query mit“
  (`?subjectId=&seriesId=&seriesUnitId=`).
- **Die Kür ist schon da.** Der Bonus-Vorrat zieht die Units der Positionen aller spielbaren Pläne plus die aktuelle
  Unit der Lehrbücher (`Pugling.Api/Services/Student/ExtraTrainingService.cs:286-309`); eine Nicht-Vokabel-Übung, die
  Position ist, fällt heraus (`:22-24`). Eine Übung der Unit, die nicht Position wird, landet also von selbst im Vorrat,
  sobald der Plan spielbar ist.
- **Aktuelle Unit am Lehrbuch:** `Pugling.Api/Models/AdminEntities.cs:180` (`Textbook.CurrentUnitId`); Reihenfolge der
  Units `Pugling.Api/Models/CurriculumEntities.cs:73` (`Grade`), `:75` (`OrderIndex`). Niemand setzt sie beim
  Aktivieren eines Plans.
- **Fehlercodes:** `series_without_subject` (`Pugling.Api/Errors/ApiErrors.cs:73`) und `exercise_empty` (`:238`)
  existieren; ein Code für „Unit hat nichts Planbares“ nicht.

**Messung an einer Kopie der echten `pugling.db`** (2026-11-15): Units mit Übungen und ihre Vokabel-Wortlisten
(„trägt neue Wörter bei“ in `OrderIndex`-Reihenfolge):

| Unit | Kind | Vokabel-Übungen | davon Wortliste | Rest |
| --- | --- | --- | --- | --- |
| 7 „Unit 1 This is London“ (Englisch) | Theodor (aktuell) | 8 | 4 (41/44/18/21 Wörter); 4 generierte Scheiben ohne neues Wort | 6 Lückentext, 1 Grammatik, 2 Zuordnung, 1 Übersetzung |
| 13 „Unité 1 Vive la rentrée!“ (Französisch) | Friedrich (aktuell) | 3 | 3 (9/28/33) | 3 Lückentext, 1 Grammatik |
| 15–20 „Unité 3…8“ (Französisch) | – | 0 | – | je 3–4: Lückentext, Grammatik, Übersetzung |
| 3 „Einmaleins“ (Mathe) | – | 0 | – | Arithmetic, ArithmeticDrill |

Ohne Wortlisten-Regel bekäme Theodor 8 tägliche Vokabel-Pflichten (4 davon Dubletten), mit „jede andere prüfbare
Übung wöchentlich“ zusätzlich 10 Wochenziele.

## Die echte Lücke

1. Kein Server-Endpunkt, der aus den Katalog-Übungen einer Unit einen Plan mit Pflicht-Aufteilung baut; die Regel
   „was wird täglich, was wöchentlich, was Kür“ lebt heute nur im Agenten (und dort falsch: alles täglich).
2. Kein Einstieg „Plan aus Unit“ im Vater-Web.
3. Die aktuelle Unit des Lehrbuchs folgt dem Plan nicht.

## Offene Punkte

1. ~~AK 2 sagt „jede andere prüfbare Übung wöchentlich“, Nutzer-Entscheidung 8 „Lückentext/Grammatik wöchentlich;
   der Rest Kür“ – was gilt?~~ → Entscheidung 11.
2. ~~Welche Vokabel-Übungen werden täglich – alle oder nur die Wortlisten?~~ → Entscheidung 12.
3. ~~Route: `supervisor/children/{id}/plans/from-unit` (AK-Entwurf) oder neben den bestehenden Plan-Routen?~~ →
   Entscheidung 13.
4. ~~Was ist „prüfbar“, und was geschieht mit leeren oder nicht ausführbaren Übungen?~~ → Entscheidung 14.
5. ~~Wer hält die Intensitäts-Zahlen?~~ → Entscheidung 15.
6. ~~Aktiv oder inaktiv angelegt?~~ → Entscheidung 16.
7. ~~Hängt „aktuelle Unit nur vorwärts“ am Endpunkt oder an jeder Aktivierung?~~ → Entscheidung 17.
8. ~~Was baut der Agent noch selbst?~~ → Entscheidung 18.

## Entscheidungen

Nutzer-Entscheidungen aus B-537 (im Dialog, nicht umzustoßen): **8** Vokabeln täglich mit Tagesdosis,
Lückentext/Grammatik wöchentlich, der Rest Kür; ein Fach ohne Vokabeln bekommt eine tägliche Übung. **9** ein
Server-Endpunkt baut den Plan (28 Tage), Wizard und Agent rufen ihn; der Agent füllt nur den Katalog. **10** wird ein
Plan mit Unit aktiv, setzt der Server die aktuelle Unit des Lehrbuchs – nur vorwärts. Die folgenden setzen sie um
(Nummern fortlaufend nach B-537).

11. **Wöchentlich werden nur Lückentext (`Cloze`) und Grammatik (`Grammar`); jede andere prüfbare Übung wird Kür.**
    *Stakeholder-Entscheidung im Auftrag des Nutzers*, die den AK-Entwurf an Nutzer-Entscheidung 8 angleicht – dort
    steht wörtlich „der Rest Kür“, der Entwurf („jede andere prüfbare Übung wöchentlich“) widersprach ihr. Begründung:
    die Nutzer-Entscheidung gewinnt; Zuordnung und Übersetzung bleiben im Bonus-Vorrat spielbar. Kosten: Theodors
    Unit 7 bekommt trotzdem 7 Wochenziele (6 Lückentexte, 1 Grammatik) – rund eines je Tag.
12. **Täglich werden nur die Vokabel-Übungen, die ein neues Wort beitragen** (in `OrderIndex`-Reihenfolge, Wort
    gefaltet wie im Briefing); Dubletten-Scheiben werden Kür. Tagesdosis je Position `max(1, ⌈Karten / Laufzeit⌉)`,
    Stufe 4 (getippt), `requireTypedTest`, Leitner – die Regel von B-463, jetzt im Server. *Stakeholder-Entscheidung
    im Auftrag des Nutzers.* Begründung: Nutzer-Entscheidung 8 sagt „Vokabeln mit Tagesdosis“ – gemeint ist die
    Wortliste der Unit, nicht jede Kopie davon; an der echten DB hätte Theodor sonst 4 Dubletten-Pflichten. Die Regel
    existiert schon im Agenten und wandert nur. Kosten: der Server braucht den Wort-Vergleich (Vokabel-Id bzw.
    gefaltete Vorderseite); eine generierte Scheibe mit wirklich neuen Wörtern wird Pflicht – richtig so.
13. **Route `POST supervisor/study-plans/from-unit`, `childId` im Rumpf** – neben `POST supervisor/study-plans`, wie
    `CreatePlanDto`. *Stakeholder-Entscheidung im Auftrag des Nutzers*; ändert den AK-Entwurf (`children/{id}/plans/…`).
    Begründung: alle Plan-Routen hängen unter `study-plans`; eine zweite Wurzel für dieselbe Ressource wäre eine zweite
    Stelle für Ownership und Doku. Kosten: keine.
14. **Prüfbar = Typ mit Test (`CheckMode != None`), mit Inhalt und für den Vater ausführbar.** Leere und nicht
    ausführbare Übungen werden übersprungen und in der Antwort mit Grund genannt (nicht `400`); Typen ohne Test
    (Lesen, Hören …) werden Kür. *Stakeholder-Entscheidung im Auftrag des Nutzers.* Begründung: dieselben Prüfungen wie
    beim Einzel-Anlegen (`PlanPositionsController.cs:156-164`) – eine Pflicht ohne Inhalt wäre unspielbar, ein ganzer
    Plan darf aber nicht an einer fremden Übung scheitern. Kosten: die Antwort braucht eine Liste `Skipped` und eine
    Liste `Bonus`.
15. **Der Server hält die Intensitäts-Zahlen für diesen Endpunkt** (Enum `Locker|Normal|Intensiv` im Vertrag, Vorgabe
    `Normal`; dieselben Zahlen wie `VaterWizard.INTENSITY`). Der Agent verliert seine Kopie (`PlanPlanner.Intensities`),
    der freie Wizard-Weg behält seine. *Stakeholder-Entscheidung im Auftrag des Nutzers.* Begründung: Geschäftsregel
    gehört in den Server (API-First); die Anzahl der Kopien bleibt zwei. Kosten: ein Enum im Vertrag; ein Kommentar an
    beiden Kopien nennt die andere.
16. **Vorgabe `active: true` wie beim Plan-Anlegen; der Agent schickt `false`.** *Stakeholder-Entscheidung im Auftrag
    des Nutzers.* Begründung: der Vater, der im Wizard „Plan aus Unit“ wählt, hat geprüft; ungeprüfter KI-Inhalt bleibt
    aus (Agent-Regel). Mit B-539 schaltet ein aktiver Plan nur den laufenden Plan **desselben Fachs** ab. Kosten: keine.
17. **„Aktuelle Unit nur vorwärts“ hängt an jeder Aktivierung eines Plans mit Unit** (Anlegen aktiv, PATCH
    `active: true`), nicht nur am Endpunkt: der Server setzt `CurrentUnitId` jedes Lehrbuchs des Kindes **derselben
    Reihe**, wenn die Plan-Unit nach (`Grade` ?? 0, `OrderIndex`) hinter der bisherigen liegt oder keine gesetzt ist.
    *Stakeholder-Entscheidung im Auftrag des Nutzers.* Begründung: auch der Agent-Plan wird später per PATCH aktiv;
    am Endpunkt allein griffe die Regel dort nie. Kosten: die Aktivierungsstelle aus B-539 bekommt einen zweiten
    Nebeneffekt; ein Kind ohne Lehrbuch dieser Reihe bleibt unberührt.
18. **Der Agent `plan` erzeugt nur noch Übungen in die Unit und ruft danach den Endpunkt** (`--days` Vorgabe 28,
    `active: false`); `PlanPlanner` verliert Plan-Anlegen, Positions-Schleife, `BookListPosition` und die
    Wortlisten-Zuweisung (die Wortlisten **erzeugt** er weiter nicht – B-463 bleibt: keine Scheiben, wo die Unit eine
    Liste hat). *Stakeholder-Entscheidung im Auftrag des Nutzers.* Begründung: Nutzer-Entscheidung 9. Kosten: der
    Plan enthält alle Übungen der Unit, nicht nur die dieses Laufs – gewollt, der Plan ist das Kapitel; die Agent-Tests
    zu B-463 (`CreatorAgentTests.cs:1439-1450`) ziehen auf den Endpunkt um.

## Akzeptanzkriterien

Final. Je Fall die Änderung, die seinen Test rot macht.

1. `POST supervisor/study-plans/from-unit` mit `childId`, `seriesUnitId` (optional `startDate`, `durationDays`
   Vorgabe 28, `intensity` Vorgabe `Normal`, `active` Vorgabe `true`) legt einen Plan mit Fach (aus der Reihe) und Unit
   an, Laufzeit 28 Tage, in **einer** Transaktion. Fremdes Kind → `404`; Reihe ohne Fach → `series_without_subject`.
   *Rot, wenn eine Position scheitert und der Plan trotzdem stehen bleibt (Transaktion).*
2. Jede Vokabel-Übung, die ein neues Wort beiträgt, wird `Daily` mit Stufe 4, `requireTypedTest`, Leitner und
   Tagesdosis `max(1, ⌈Karten / Laufzeit⌉)`; eine Vokabel-Übung ohne neues Wort wird nicht Position. Jeder Lückentext
   und jede Grammatik-Übung wird `Weekly`. *Rot ohne Dubletten-Regel (vierte Position im Testfall) bzw. mit der
   Dosis des Agenten auf 14 statt der Laufzeit.*
3. Hat die Unit keine Vokabel-Übung, wird ihre erste prüfbare Übung (nach `OrderIndex`, dann Id) `Daily`, die übrigen
   Lückentexte/Grammatik `Weekly`. *Rot, wenn die erste Übung zusätzlich wöchentlich auftaucht oder gar keine täglich
   wird.*
4. Übungen der Unit, die nicht Position werden (Zuordnung, Übersetzung, Typen ohne Test, Dubletten-Scheiben), stehen
   in der Antwort unter `bonus` und erscheinen im Bonus-Training des Kindes, sobald der Plan spielbar ist; leere bzw.
   nicht ausführbare Übungen stehen unter `skipped` mit Grund. *Rot, wenn der Bonus-Vorrat sie nicht zeigt* (Test über
   `student/extra-training`, nach B-538).
5. Eine Unit ohne prüfbare Übung liefert `400` mit eigenem Code `unit_not_plannable`, und es entsteht **kein** Plan.
   *Rot ohne die Vorprüfung (leerer Plan, `201`).*
6. Wird ein Plan mit Unit aktiv (Anlegen aktiv, PATCH `active: true`), steht `CurrentUnitId` des Lehrbuchs derselben
   Reihe auf dieser Unit – nur, wenn sie nach (`Grade`, `OrderIndex`) hinter der bisherigen liegt oder keine gesetzt
   ist. Testfälle: vorwärts ⇒ gesetzt; rückwärts (Wiederholungsplan) ⇒ unverändert; anderes Lehrbuch ⇒ unverändert.
   *Rot ohne die Vorwärts-Prüfung (rückwärts überschrieben).*
7. Der Vater-Wizard bietet „Plan aus Unit“ (Kind, Fach → Reihe → Unit, Start, Intensität), ruft den Endpunkt und
   zeigt danach den Plan mit den Listen `bonus`/`skipped`. *Rot (E2E), wenn der Weg Positionen einzeln anlegt.*
8. Der KI-Agent `plan` erzeugt Übungen in den Katalog der Unit und ruft danach den Endpunkt; Vorgabe-Laufzeit 28 Tage,
   Plan inaktiv. *Rot, wenn der Agent noch selbst `AddPositionAsync` ruft* (Agent-Test mit Fake-Server zählt die
   Aufrufe).

## Schätzung

**Größe L**, `wo: beides`, `migration: nein` (die Unit-Spalte bringt B-539), `vertragsbruch: nein` (neuer Endpunkt,
neues DTO, neuer Fehlercode – additiv). Vergleich: breiter als B-538 (M: Regeln in zwei Diensten), weil zum Regelwerk
ein neuer Endpunkt mit Transaktion, ein neuer Wizard-Weg **und** der Umbau eines Agenten-Verbs kommen; kleiner als
B-539 (keine Migration, kein Bruch).

**Risiken**

- **Abhängigkeit B-539.** Ohne Fach-Pflicht und Abschaltung je Fach schaltet ein aktiv angelegter Unit-Plan heute
  **alle** anderen Pläne des Kindes ab. Nicht vor B-539 (Teil A) bauen.
- **Prüfungen doppelt.** Der Endpunkt muss dieselben Positions-Prüfungen fahren wie `PlanPositionsController.Create`
  (Ausführungsrecht, leer, Stufe, getippt inkl. B-552, Dosis). Die Prüfungen in einen gemeinsamen Dienst ziehen,
  statt sie zu kopieren – sonst läuft der Unit-Weg an der nächsten Regel vorbei.
- **Wort-Vergleich im Server.** Die Faltung muss die des Servers sein (`VocabKey`, B-336), nicht die des Agenten –
  sonst zählt „horse“/„the horse“ als neues Wort. Bei verlinkten Vokabeln über `VocabularyId`.
- **Karten statt Wörter.** Die Dosis rechnet mit den gespielten Karten (zwei je Wort bei beidseitigen Listen, B-472) –
  dieselbe Quelle wie `PositionPlayService.PoolSize`, nicht die Wortzahl.
- **Viele Wochenziele.** Unit 7 ergibt 7 Wochenziele; ob das trägt, zeigt erst der Rollengang mit Theodor – nicht im
  Code lösen, im Verlauf festhalten.
- **Agent-Umbau.** `CreatorAgentTests.cs` prüft B-463 und die Intensitäten (`:1450`, `:2912`) am Agenten; beide
  wandern an den Endpunkt. Die Agent-`CLAUDE.md` (Abschnitt `PlanPlanner`) und der Skill `ki-creator` beschreiben das
  alte Verhalten.

**Angriffsplan (Backend zuerst, nach B-539)**

1. `ApiErrors.UnitNotPlannable`; Vertrag `CreatePlanFromUnitDto`, `PlanFromUnitResponse` (Plan, Positionen, `Bonus`,
   `Skipped`), Enum `PlanIntensity`.
2. Positions-Prüfungen aus `PlanPositionsController` in einen geteilten Dienst ziehen (verhaltensneutral, Tests grün).
3. `StudyPlanFromUnitService`: Übungen der Unit laden, klassifizieren (Entscheidungen 11, 12, 14, AK 3), Plan +
   Positionen in einer Transaktion; Endpunkt im `StudyPlansController`. Tests AK 1–5.
4. Aktivierungs-Nebeneffekt „aktuelle Unit vorwärts“ an der B-539-Aktivierungsstelle. Tests AK 6.
5. `Pugling.Client`, `contract.ts`; Wizard-Einstieg + Vitest; E2E.
6. Agent: `PlanPlanner` auf „erzeugen, dann Endpunkt“; `AgentCommands` `--days` 28; Agent-Tests, Agent-`CLAUDE.md`,
   Skill-Doku `ki-creator`.

**Testweg**

- Neue Datei `Pugling.Api.Tests/PlanAusUnitTests.cs`: AK 1–5 (eigene Unit je Fall, Muster `FreshUnitAsync` aus
  `CreatorAgentTests.cs:1453`), AK 4 über den Bonus-Vorrat.
- `Pugling.Api.Tests/AntiCheatTests.cs` bzw. die Plan-CRUD-Tests von B-539: AK 6 (vorwärts, rückwärts, fremde Reihe).
- `Pugling.Api.Tests/CreatorAgentTests.cs`: AK 8 (Fake-Server zählt `from-unit` = 1, `positions` = 0).
- Vitest am Wizard-Baustein (Rumpf des Requests); E2E neue Spec `frontend/e2e/plan-aus-unit.spec.ts`: Vater wählt
  Unit, Plan entsteht, Plan-Seite zeigt Tagesziele/Wochenziele, Sohn sieht die Pflicht – das ist der Rollengang.
- `/smoke-test`; einmal gegen eine **Kopie** der echten DB mit Theodors Unit 7 (erwartet: 4 täglich, 7 wöchentlich).
- Reviewer: `pugling-reviewer` und `frontend-reviewer`.

## Verlauf

- 2026-11-15 · `gegrillt` angelegt durch Teilung von B-537 (Entscheidung 11); Entscheidungen dort.
- 2026-11-15 · Backlog-Lauf 2026-11-15: Ist-Stand am Code nachgetragen (kein `from-unit`; Agent plant alles täglich,
  B-463-Wortlistenregel und Intensitäten nur im Agenten; Wizard kennt keine Unit; Bonus-Vorrat nimmt Nicht-Positionen
  schon auf), an der DB-Kopie Units gemessen (Unit 7: 4 Wortlisten + 4 Dubletten-Scheiben). Entscheidungen 11–18 als
  Stakeholder im Auftrag des Nutzers; AK 1 (Route) und AK 2 (wöchentlich nur Lückentext/Grammatik, Rest Kür – an
  Nutzer-Entscheidung 8 angeglichen) geändert.
- 2026-11-15 · `geschaetzt` im Backlog-Lauf 2026-11-15: L, beides, keine Migration, kein Vertragsbruch; nach B-539
  (Teil A) bauen.
- **2026-11-15** — umnummeriert von B-540 auf B-580: die Id war auf `origin/main` schon vergeben (main belegt bis B-552, der Backlog-Lauf vom 2026-11-15 bis B-584). Inhalt unverändert.
