---
tags: [typ/story, status/abgenommen, bereich/training, rolle/supervisor, rolle/student]
aliases: [Umfang Nur neue nach B-419, Scope New fragt nie]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-08-nacht.md
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
nachgeschaut: 2026-11-14
---

# B-435 · Eine Position mit Umfang „nur neue" zeigt ihre Wörter seit B-419 nur noch – abgefragt werden sie nie

Der Umfang `ItemScope.New` filtert die Runde auf Atome ohne `IntroducedAt` (`PositionPlayService.ScopeMatch`). Bis B-419
wurde ein neues Wort dort einmal getippt abgefragt und fiel danach aus dem Umfang. Seit B-419 ist die erste Begegnung eine
Kennenlernkarte, die `IntroducedAt` setzt – die Abfrage-Runde danach findet das Wort nicht mehr im Umfang, „Jetzt
abfragen" führt in „Nichts fällig".

## User Story

Als **Vater**, der eine Position auf „nur neue Wörter“ stellt, möchte ich, dass mein Sohn jedes neue Wort kennenlernt
**und** am selben Tag abgefragt wird, **damit** „nur neue“ Lernen bedeutet und nicht bloß Anschauen – und die Pflicht der
Position nicht mit Durchklicken erfüllt ist.

## Ist-Stand am Code

Gelesen am 2026-11-13 (Branch `auto/backlog-lauf-2026-11-13`, Stand `b0653edb`):

- `backend/Pugling.Api/Services/Shared/PositionPlayService.cs:421-426`: `ItemScope.New => prog?.IntroducedAt is null`,
  `ItemScope.Old => prog?.IntroducedAt is not null`. Angewendet in `DueItemIndicesAsync` (`:190`).
- Die Kennenlernkarte setzt `IntroducedAt` beim ersten Kontakt (`backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs:388-394`),
  zahlt nichts und bewegt keinen Kasten (`:402-404`, `displayOnly`); `ReviewCount` bleibt 0, weil `ApplyReview`
  (`PositionPlayService.cs:450-465`) nicht läuft.
- Folge: nach dem Kennenlernen fällt das Atom aus dem Umfang `New`; die nächste Runde („Jetzt abfragen“) ist leer.
- **Erreichbarkeit (abweichend von der Idee):** der Umfang ist **nur über die API** setzbar
  (`backend/Pugling.Api/Controllers/Supervisor/PlanPositionsController.cs:191`, `:264`; Vertrag
  `backend/Pugling.Contracts/Supervisor/StudyPlanDtos.cs:52`). Das Vater-Web setzt ihn nirgends (kein Treffer für
  `ItemScope`/„Umfang“ als Positionsfeld unter `frontend/src/vater/`), der Assistent auch nicht; Standard ist `All`
  (`backend/Pugling.Api/Models/PlanPositionEntities.cs:42`). Ob die echte `pugling.db` eine Position mit `New` trägt,
  ist nicht gemessen (Messungen nur auf einer Kopie).
- Kein Integrationstest deckt `ItemScope.New` oder `Old` ab (Suche in `backend/Pugling.Api.Tests` ohne Treffer).

## Die echte Lücke

„Neu“ hieß bis B-419 „noch nie gespielt“ und fiel mit „noch nie eingeführt“ zusammen. Seit die Einführung eine eigene,
urteilsfreie Begegnung ist, fehlt dem Umfang `New` der Tag, an dem das Wort gezeigt **und** gefragt wird.

## Offene Punkte

1. ~~Heißt „neu“ künftig „noch nie abgefragt“ (`ReviewCount == 0`) oder bleibt „nur neue“ reines Kennenlernen?~~ →
   Entscheidung 1.
2. ~~Ändert sich `Old` mit?~~ → Entscheidung 2.
3. ~~`art`: Frage oder Defekt?~~ → Entscheidung 3.

## Entscheidungen

Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13, [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).

1. **„Neu“ heißt: noch nicht eingeführt oder heute eingeführt** (`IntroducedAt is null || IntroducedAt == day`). Das
   Wort wird am Einführungstag gezeigt und in der Folgerunde abgefragt, ab dem nächsten Tag ist es Wiederholungsstoff.
   *Begründung:* stellt das Verhalten vor B-419 wieder her (einmal abgefragt, dann draußen) und gilt für Positionen mit
   und ohne Leitner gleich. `ReviewCount == 0` taugt nicht: ohne Leitner läuft `ApplyReview` nie, „neu“ endete dort
   nie. Reines Kennenlernen widerspräche dem Kern – eine Pflicht, die sich mit „Weiter“-Klicken erfüllt, erzwingt kein
   Lernen. *Kosten:* `ScopeMatch` braucht den Tag; wer am Einführungstag nicht mehr abfragt, wird unter „nur neue“
   später nicht mehr gefragt (wie vor B-419, wer nicht weiterspielte).
2. **`Old` bleibt unverändert** (`IntroducedAt is not null`); am Einführungstag liegt ein Wort damit in beiden Umfängen.
   *Begründung:* `Old` war schon vor B-419 so, und die Überschneidung schadet nicht – eine `Old`-Position zeigt nie ein
   ungezeigtes Wort. *Kosten:* keine.
3. **`art: Defekt`** statt `Frage`: die Kombination B-419 + `New` liefert jetzt eine leere Abfrage-Runde. `entgangen_bei`
   bleibt leer: beim Bau von B-419 bemerkt und bewusst zurückgestellt. *Kosten:* keine.

## Akzeptanzkriterien

1. Position mit `Scope = New` (Leitner): das Kind spielt die Kennenlern-Runde; die Folgerunde am selben Tag liefert die
   eben eingeführten Atome als Abfrage-Karten (nicht leer). *Rot, wenn* `ScopeMatch` wieder nur `IntroducedAt is null`
   prüft.
2. Dieselbe Position am nächsten Tag (gesteuerte Uhr): die gestern eingeführten Atome sind nicht mehr im Umfang, die
   noch nicht eingeführten schon. *Rot, wenn* die Bedingung auf „irgendwann eingeführt“ ausgeweitet wird.
3. Position mit `Scope = New` ohne Leitner: Folgerunde am Einführungstag nicht leer, am Folgetag ohne die eingeführten
   Atome. *Rot, wenn* stattdessen `ReviewCount == 0` geprüft wird (dann bleiben sie am Folgetag drin).
4. Position mit `Scope = Old`: unverändert, ein nie eingeführtes Atom erscheint nicht. *Rot, wenn* `Old` versehentlich
   mitgeändert wird.

## Schätzung

**Größe: XS** – eine Bedingung in `PositionPlayService.ScopeMatch` (Tag als Parameter, Aufrufer `:190` hat ihn) und ein
neuer Integrationstest mit vier Fällen. `wo: backend`, `migration: nein`, `vertragsbruch: nein`.

**Risiken:** gering. Die Tagesdosis (`WithinDailyDose`, `PositionPlayService.cs:204-215`) lässt eingeführte Atome
ungehindert durch – heute eingeführte bleiben also auch unter Dosis in der Runde. Weitere Leser von `ScopeMatch` gibt es
nicht.

**Angriffsplan:** (1) Integrationstest `backend/Pugling.Api.Tests/UmfangNeuTests.cs` (AK 1–4, gesteuerte Uhr wie in
`ErstbegegnungTests`), rot gegen den Ist-Code (AK 1, 3); (2) `ScopeMatch` um den Tag erweitern; (3) rote Probe je AK;
(4) `pugling-reviewer`.

**Testweg:** `backend/Pugling.Api.Tests/UmfangNeuTests.cs` (neu, AK 1–4). Kein E2E: das Vater-Web setzt den Umfang nicht;
Rollengang per `curl` (Position über die API auf `New` stellen, als Sohn zwei Runden).

## Verlauf

- **2026-11-08** — angelegt beim Bau von B-419 (Lauf AE, Sprint 22).
- **2026-11-13** — `ausformuliert`: am Code belegt; der Umfang ist nur über die API setzbar, nicht über die Plan-Seite,
  und ohne Test.
- **2026-11-13** — `gegrillt`: Entscheidungen 1–3, `art` von `Frage` auf `Defekt`; Stakeholder-Entscheidung im Auftrag
  des Nutzers (Backlog-Lauf 2026-11-13).
- **2026-11-13** — `geschaetzt`: XS, backend, ohne Migration und Vertragsbruch.
- **2026-11-13** — `in-arbeit`: gebaut. `PositionPlayService.ScopeMatch` nimmt den Tag; `New` = nicht eingeführt oder heute eingeführt, `Old` unverändert. Test `backend/Pugling.Api.Tests/UmfangNeuTests.cs` (AK 1–4, AK 2/3 mit gesteuerter Uhr). Keine Migration, kein Vertragsbruch.
  Rote Probe, gemessen an `cf07416f + Arbeitsbaum geändert`, 2026-11-13 21:38 (`mutant-probe.py`, Grundlinie nach dem Zurückspielen grün):

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | B435-AK1 | backend/Pugling.Api/Services/Shared/PositionPlayService.cs:428 | red | red |
  | B435-AK2 | backend/Pugling.Api/Services/Shared/PositionPlayService.cs:428 | red | red |
  | B435-AK3 | backend/Pugling.Api/Services/Shared/PositionPlayService.cs:428 | red | red |
  | B435-AK4 | backend/Pugling.Api/Services/Shared/PositionPlayService.cs:429 | red | red |

- **2026-11-13** — **abgenommen** mit Commit `699b02a7` (Sprint 69, Backlog-Lauf 2026-11-13). Verifikation: Backend 1555/1555 grün (dotnet test Pugling.sln -c Release), Vitest 937/937, tsc -b sauber, Mutanten-Probe je AK-Fall rot (Tabelle oben). Review: `pugling-reviewer` ohne Befund (ein Aufrufer `DueItemIndicesAsync`, Runde und `DueCount` zählen gleich). Rollengang: Playwright gegen frisch gestartete Server auf Ausweichports (API 5211, Web 5184) nach dem letzten Edit – lektion, erstbegegnung, karten-umdrehen, full-flow, test-durchgefallen-ueben, uebung-abbruch, pfad, bonus, artikel-pflicht: **10 passed**. Vater und Creator sind Regressionszeugen (Suite grün, kein Pfad ihrer Ebene im Diff außer der Vater-Wand von B-433).
