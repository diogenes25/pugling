---
tags: [typ/story, status/geschaetzt, bereich/training, bereich/backend, rolle/student, rolle/creator]
aliases: [Gestufte Hinweise, Hint-Stufen, Hilfe vor der Antwort, Tipp kostet]
status: geschaetzt
prio: P3
art: Wunsch
groesse: M
wo: beides
migration: ja
vertragsbruch: ja
quelle: docs/khan-academy-vergleich-plan.md#wo-khan-weiter-ist
grund: ""
ersetzt_durch: []
---

# B-223 · Gestufte Hinweise vor der Antwort, die Punkte kosten

Ein Vokabel-Item trägt einen statischen `Hint`, der mit der Karte ausgespielt wird; danach kennt der
Student-Pfad nur Antwort und Reveal. Khan Academy gibt über Khanmigo Strategiehinweise, ohne die Lösung
zu verraten. Die Idee: ein gestufter Hinweisweg am Item, den das Kind aktiv anfordert und der die Review-Punkte der
Karte mindert. Diese Story liefert die **erste Stufe**: der vorhandene Tipp kommt vom Server, wird dort gezählt und
kostet nachvollziehbar Punkte. Die Buchstaben-Stufen folgen danach.

## User Story

Als *Vater* möchte ich, dass ein Tipp beim Üben etwas kostet, damit mein Sohn erst selbst nachdenkt und die Münzen für
eine Karte nur voll bekommt, wenn er sie ohne Hilfe wusste. Als *Sohn* möchte ich den Tipp trotzdem holen können, wenn
ich festhänge, und sehen, was er mich gekostet hat.

## Ist-Stand am Code

Gelesen am 2026-11-13 (Worktree `backlog-lauf`, `699b02a7`), Zeilen nachgemessen am 2026-11-14:

- Der Tipp reist **mit der Karte** und ist kostenlos: `PositionPlayService.CardFacets` gibt `item.Hint` auf getippten
  Stufen heraus (`backend/Pugling.Api/Services/Shared/PositionPlayService.cs:314`, `:328`; nicht neben einem Wortkasten,
  B-524), `PracticeCard.Hint` (`backend/Pugling.Contracts/Student/PracticeDtos.cs:81-82`), gebaut in
  `PositionPracticeController.cs:196-199`. Die Arcade deckt ihn nur auf Wunsch auf – rein clientseitig
  (`frontend/src/sohn/SohnPractice.tsx:89`, `:477-482`). Der Server erfährt nicht, ob er gelesen wurde.
- Die Wortlänge gibt es schon ohne Kosten (`AnswerLength`/Buchstabenkästen, `CardFacets` `letterBoxLength`).
- **`HintsUsed` liegt nicht am Üben, sondern an der Testzeile** `TestItemResult` (`backend/Pugling.Api/Models/StudyPlanEntities.cs:183`,
  `:199-207`, „Set by no path and therefore always 0"), im Vertrag `ItemResultDto` (`backend/Pugling.Contracts/Student/TestDtos.cs:59`),
  gelesen in `PositionTestsController.cs:329`. Ein Üben-Review schreibt nur `ReviewEvent(WasCorrect, At)` und die
  Positions-Fortschrittszeile `PositionItemProgress` (`backend/Pugling.Api/Models/PlanPositionEntities.cs:198-217`).
- Punkte: `ScoringService.ScoreReview` (`backend/Pugling.Api/Services/Shared/ScoringService.cs:63-90`) – Basis in Münzen,
  Combo/Tempo in Gems; Buchungstext je Beitrag.
- **Befund am Rand (erledigt durch [B-544](B-544-abschlusstest-zeigt-den-tipp.md), abgenommen):** `TestItem` trägt keinen Tipp mehr (`TestDtos.cs:37`), `SohnTest.tsx` zeigt keinen. Damals: `TestItem.Hint` (`TestDtos.cs:34`,
  `PositionTestsController.cs:72`), `frontend/src/sohn/SohnTest.tsx:215` – entgegen der Rest-Entscheidung vom 2026-11-05
  („im Abschlusstest gibt es keine Hinweise").

## Die echte Lücke

Der Tipp ist ein Feld der Karte, nicht eine Anfrage an den Server. Solange er mitreist, kann der Server weder zählen noch
mindern – jede Kostenregel stünde nur im Client.

## Offene Punkte

1. ~~Wie zählt der Server, ohne dass ein gefälschter Client den Tipp kostenlos liest?~~ → Entscheidung 1.
2. ~~Wo wird gezählt, wenn `HintsUsed` am Test hängt?~~ → Entscheidung 2.
3. ~~Was kostet der Tipp genau?~~ → Entscheidung 3.
4. ~~Welche Stufen in dieser Story?~~ → Entscheidung 4.
5. ~~Hinweise im Abschlusstest?~~ → beantwortet 2026-11-05 (nein); die Abweichung im Code → „Nicht in dieser Story".

## Entscheidungen

Grundlage bleiben die **Entscheidung des Nutzers G-5 (2026-10-23)** – Hinweise kosten Punkte, gestuft, der Server zählt,
der Tipp wird die erste Stufe, kein Parallel-Mechanismus, Lösungsfeld-Regel unverletzt – und die Rest-Entscheidung vom
2026-11-05 (kein Hinweis im Test). Die folgenden sind Stakeholder-Entscheidungen im Auftrag des Nutzers (Backlog-Lauf
2026-11-13, [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).

1. **Der Tipp wird angefordert, nicht mitgeliefert.** `PracticeCard.Hint` (Text) weicht `HasHint` (bool); neuer Endpunkt
   `POST …/sessions/{sessionId}/cards/{itemIndex}/hint` → `{ hint }`. Er gilt nur für Karten der Runde (`session.Order`,
   wie `Review`) und nur, wo `CardFacets` heute einen Tipp gäbe. *Begründung:* „der Server zählt" (G-5) setzt voraus, dass
   der Text nur über ihn zu haben ist. *Kosten:* Vertragsbruch an `PracticeCard` (Frontend, `Pugling.Client`, auch
   `ReviewOutcome.Next`).
2. **Gezählt wird an `PositionItemProgress` als `HintedOn` (DateOnly?, neue nullable Spalte)**, gesetzt beim ersten Abruf
   des Kindes an diesem Tag; der Supervisor bekommt den Text ohne Stempel (Vorschau, Wand aus B-433).
   *Begründung:* G-5 nennt „die vorhandene Spalte `HintsUsed`" – die liegt aber an der **Testzeile**, und im Test gibt es
   nach 2026-11-05 keine Hinweise; am Üben-Pfad existiert keine solche Spalte. Das ist eine **Auslegung, kein Umstoßen**:
   gewahrt bleiben die Absicht (Server zählt, kein zweiter Mechanismus neben dem Tipp) und der Ort der Wertung
   (`ScoringService`). Der Tag genügt, weil eine Karte ohnehin höchstens einmal je Tag gewertet wird.
   `TestItemResult.HintsUsed` bleibt unberührt. *Kosten:* eine Migration an der Kette; dem Nutzer bei Gelegenheit
   vorlegen, dass die Spalte nicht die genannte ist.
3. **Kosten: halbe Basis-Münzen (abgerundet), kein Tempo-Bonus; Combo und Kasten wie sonst.** Buchungstext
   „… richtig (mit Tipp, ½) → Box n". *Begründung:* Nachvollziehbar für den Vater im Konto, fühlbar, aber nicht
   vernichtend für den Sohn – er soll den Tipp lieber nehmen als aufgeben. Den Kasten nicht anzuhalten hält die Story
   klein; die Karte kommt ohnehin wieder. *Kosten:* ein Parameter in `ScoreReview`, eine Regel mehr im Buchungstext.
4. **Nur Stufe 1 (der vorhandene Tipp).** *Begründung:* Kleinste Scheibe, die die Kostenregel liefert; Karten ohne Tipp
   ändern sich nicht. *Kosten:* „gestuft" ist mit dieser Story noch nicht erreicht – siehe „Nicht in dieser Story".

## Akzeptanzkriterien

1. Eine Karte mit Tipp liefert `hasHint = true` und keinen Tipp-Text; ohne Tipp `false`. *Rot, wenn* `PracticeCard` den
   Text weiter trägt.
2. `hint` als Kind: liefert den Text und setzt `HintedOn = heute`. *Rot, wenn* kein Stempel entsteht.
3. Danach richtige getippte Antwort, fällig: Basis = ⌊Basis/2⌋, kein Tempo-Bonus, Buchungstext enthält „mit Tipp".
   *Rot, wenn* die volle Basis gebucht wird.
4. Ohne Tipp-Abruf: volle Basis (bestehende Fälle bleiben grün). *Rot, wenn* die Minderung ohne Stempel greift.
5. `hint` als Supervisor: Text ja, Stempel nein; ein anschließendes Kind-Review zahlt voll. *Rot, wenn* die Vorschau stempelt.
6. `hint` auf einen Index außerhalb der Runde → 404; auf eine Karte ohne Tipp → 404. *Rot, wenn* der Endpunkt beliebige
   Indizes bedient.
7. Bestehende Datenbank: nullable Spalte angehängt, Bestandszeilen `HintedOn = null` (`SchemaGuardTests` grün).
   *Rot, wenn* die Spalte nicht nullable ist.
8. Arcade: „Tipp" ruft den Endpunkt, zeigt den Text und darunter „Kostet die Hälfte der Münzen"; ein Fehlschlag zeigt eine
   Meldung statt eines leeren Tipps. *Rot, wenn* der Knopf ohne Abruf aus der Karte liest.

## Schätzung

**Größe: M** – Endpunkt, Spalte samt Migration, Kostenregel im `ScoringService`, Vertragsbruch an `PracticeCard`, Umbau
des Tipp-Knopfs. Schon auf Stufe 1 geschnitten; kleiner geht es nicht, ohne dass wieder nur der Client zählt.
`wo: beides`, `migration: ja`, `vertragsbruch: ja`.

**Risiken:** `PracticeCard` ist auch `ReviewOutcome.Next` – beide Pfade gleich umstellen. Der Wortkasten (B-524) unterdrückt
den Tipp schon heute; `HasHint` muss dieselbe Bedingung tragen (`CardFacets`), sonst zeigt die Arcade einen Knopf ohne
Inhalt. `unknown_field`-Fallen bei `Pugling.Client` und Frontend-Typen. Ein Tipp-Abruf ohne anschließende Antwort zählt
nur für diesen Tag – gewollt.

**Angriffsplan:** (1) neue Datei `backend/Pugling.Api.Tests/TippKostetTests.cs` (AK 1–6) und Fall in
`ScoringServiceBoundaryTests.cs` (Halbierung, Abrundung), rot gegen den Ist-Code; (2) Spalte + Migration, Endpunkt,
`HasHint` in `CardFacets`/`BuildCard`, Parameter `hinted` in `ScoreReview`; (3) `pugling-reviewer`; (4) Client und
Frontend-Typen, Tipp-Knopf in `SohnPractice.tsx:477-482` über `useAction`; (5) Vitest AK 8; (6) rote Probe je AK;
`frontend-reviewer`; `DocsCaptureTests` neu schreiben lassen.

**Testweg:** `backend/Pugling.Api.Tests/TippKostetTests.cs` (AK 1–6), `ScoringServiceBoundaryTests.cs` (AK 3, Rechnung),
`SchemaGuardTests` (AK 7), `frontend/src/sohn/SohnPractice.test.tsx` (AK 8). Rollengang: Kind holt einen Tipp, Vater
sieht die halbe Buchung im Konto.

**Nicht in dieser Story:** Stufe 2 und 3 (Anfangsbuchstabe, weiterer Buchstabe – serverseitig aus der Lösung abgeleitet,
mit eigener Kostenstufe); Tipps im Bonus-Training (`ExtraCard.Hint`). **Als eigene Defekt-Story erledigt ([B-544](B-544-abschlusstest-zeigt-den-tipp.md)):** der Test
lieferte und zeigte den Tipp (`TestItem.Hint`, `SohnTest.tsx:215`) entgegen der Entscheidung vom 2026-11-05.

## Verlauf

- **2026-10-17** — angelegt (Quelle: Khan-Academy-Vergleich, Etappe 6 des Übernahmeplans).
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): **teilweise vorhanden** – ein Tipp auf Wunsch (`SohnPractice.tsx:60,305`, kostenlos, clientseitig) und die Wortlänge über `answerLength`/`LetterBoxes` (`:324`). Die Spalte `HintsUsed` existiert, wird aber von keinem Pfad beschrieben (`StudyPlanEntities.cs:188-194`). Beim Ausformulieren: auf diesen Bestand aufsetzen statt einen neuen Mechanismus zu bauen.
- **2026-10-23** — **Entscheidung des Nutzers** in der Grill-Runde G-5 der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): Hinweise **kosten Punkte, über die vorhandene Spalte `HintsUsed`**. Die Hinweise sind gestuft (Anfangsbuchstabe, dann ein weiterer Buchstabe); der Server zählt sie und mindert die Punkte der Karte nachvollziehbar im `ScoringService`. Ein neuer Mechanismus neben dem vorhandenen Tipp entsteht nicht, der Tipp wird zur ersten Stufe. Die Lösungsfeld-Regel bleibt unverletzt. Offen beim Ausformulieren: ob es im Abschlusstest Hinweise gibt. **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt am 2026-10-23 für alle an diesem Tag im Dialog entschiedenen Stories):** ein Nachtlauf darf diese Story auf Grundlage der Entscheidungen selbst ausformulieren, schätzen und bauen (Paket NP-18); trifft er auf eine Produktfrage, die die Entscheidungen nicht hergeben, hält er an.
- **2026-11-05** — **Rest-Entscheidung im Auftrag des Nutzers** (getroffen von Claude; Delegation im Dialog am 2026-11-05, [Protokoll](../pm-sitzung-2026-11-05-delegation.md)): im Abschlusstest gibt es keine Hinweise – er misst.
- **2026-11-13** — `ausformuliert`: am Code belegt – der Tipp reist mit der Karte (`CardFacets`), `HintsUsed` hängt an der Testzeile, nicht am Üben; Befund: der Test zeigt den Tipp entgegen 2026-11-05.
- **2026-11-13** — `gegrillt`: Entscheidungen 1–4 (Tipp per Anfrage, Zählung als `HintedOn` an `PositionItemProgress` – Auslegung von G-5, weil `HintsUsed` am Test liegt –, halbe Basis ohne Tempo-Bonus, nur Stufe 1); Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13, [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)); G-5 und 2026-11-05 unverändert.
- **2026-11-13** — `geschaetzt`: M (schon auf Stufe 1 geschnitten), beides, Migration ja, Vertragsbruch ja; Buchstaben-Stufen und der Test-Befund ausdrücklich nicht in dieser Story.
- **2026-11-14** — Abgleich Backlog-Lauf: Kern gilt – der Tipp reist weiter mit der Karte (`PositionPlayService.cs:328`, `PracticeDtos.cs:82`), kein `hint`-Endpunkt, kein `HintedOn`. Ist-Stand-Zeilen berichtigt; der Rand-Befund (Tipp im Abschlusstest) ist durch B-544 erledigt (`TestDtos.cs:37` ohne `Hint`).
