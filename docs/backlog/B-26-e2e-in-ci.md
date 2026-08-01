---
tags: [typ/story, status/in-arbeit, bereich/frontend, bereich/qualitaet]
aliases: [E2E in CI, roter Nachtlauf]
status: in-arbeit
prio: P1
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: memory/codequalitaet-gates.md
---

# B-26 · Der E2E-Nachtlauf ist rot – und niemand erfährt es

**Neu zugeschnitten am 2026-08-01.** Die ursprüngliche Idee („lokal 25/25, in CI nie gelaufen") ist
**widerlegt**: Der Lauf existiert, ist zweimal gefahren – und beim zweiten Mal rot. Aus „einen CI-Lauf
einrichten" wird damit „einen roten Lauf auswerten und ihn zustellbar machen". Etappe **E0** des
Testabdeckungs-Pakets ([testabdeckung-plan.md](../testabdeckung-plan.md)).

## User Story

Als **Entwickler**, dessen nächtliche E2E-Suite einen echten Bruch findet, möchte ich davon **erfahren**,
bevor ich zwei Tage später zufällig in die Actions-Übersicht sehe – und ich möchte, dass die Suite prüft,
was es **heute** gibt, nicht was es einmal gab.

## Ist-Stand am Code

- **Der Lauf existiert.** `.github/workflows/e2e.yml` mit `pull_request` und nächtlichem Cron; bewusst
  **kein** Freigabe-Tor (`e2e.yml:7-15`), `ci.yml:142-144` hält Playwright ausdrücklich draußen.
- **Zwei geplante Läufe, der zweite rot** (`gh run list --workflow=e2e.yml`): `30517281632` vom 2026-07-30
  **grün**, `30608976657` vom 2026-07-31 06:12 UTC **rot** – 24 grün, 1 rot, Laufzeit 3m42s.
- **Die Ursache ist eine tote Ebene.** `vater-von-null.spec.ts:265-272` fährt Abschnitt „8. Lernziel auf
  Fach-Ebene", wählt ein Fach und klickt „Lernziel anlegen". In `frontend/src` hat dieser Text **0 Treffer** –
  `6471e1d` („DB-Struktur-Umbau E13: LearnGoal geloescht") hat die Ebene entfernt, siehe
  [B-14](B-14-learngoal-belohnung.md). Der Test läuft in den 60-Sekunden-Timeout beim Warten auf
  `getByLabel("Fach")`.
- **Der Nachfolger ist nur halb abgedeckt.** Abschnitt 8a legt ein **Objective** an („+ Großes Ziel
  anlegen"), aber im ganzen Spec kommt **keine Etappe (KeyResult)** vor – während `VaterZiele.tsx:244` selbst
  sagt: „Noch keine Etappen – ohne sie kann das Ziel nicht erreicht werden." Der Scope-Wähler und die
  Messlatte, die Abschnitt 8 geprüft hat, sind damit heute **ungeprüft**.

## Die echte Lücke

Zwei Dinge, und das zweite ist das eigentliche.

**Erstens:** Die Suite prüft eine gelöschte Ebene. Das ist billig zu reparieren – aber nicht durch Streichen:
dann verschwindet mit dem Rot auch die Abdeckung der Ebene, die die Lernziel-Ebene **beerbt** hat.

**Zweitens, und das ist der Kern:** Die E2E hat genau getan, wofür sie da ist – sie hat einen echten Bruch
gefunden. Erreicht hat es niemanden. Ein Tor ohne Zustellung ist kein Tor, sondern ein Protokoll, das jemand
lesen müsste. Die Entscheidung, Playwright **nicht** ins Freigabe-Tor zu nehmen, ist richtig (2,3 min
Testphase) – sie verlangt nur ihr Gegenstück.

## Entscheidungen

1. **Der tote Abschnitt wird auf die Etappe umgeschrieben, nicht gestrichen** (Paket-Entscheidung 2). Der
   Scope-Wähler, die Messlatte und der Status „offen" wandern an das Objective aus 8a. Begründung und Kosten
   stehen in [testabdeckung-plan.md](../testabdeckung-plan.md).
2. **Zugestellt wird über ein Issue**, das der Workflow selbst führt – die Empfehlung aus Offenem Punkt 1
   unverändert übernommen. Ein fester Titel plus Label `e2e-nightly`: bei Rot wird es angelegt oder
   kommentiert, bei Grün geschlossen. Begründung: es ist die einzige Variante mit einem **Zustand** – ein
   Badge zeigt nur den letzten Lauf und braucht jemanden, der hinsieht; eine Mail hängt an persönlichen
   Einstellungen. **Kosten:** der Workflow braucht `issues: write` (bisher nur `contents: read`), und ein
   Issue-Faden wächst über die Zeit — deshalb ein *wiederverwendetes* Issue mit Kommentaren statt eines
   neuen je Nacht.
3. **Zugestellt wird nur bei `schedule` und `workflow_dispatch`, nicht am Pull Request.** Begründung: am PR
   ist der rote Check schon die Zustellung, ein Issue dazu wäre Lärm — und ein Fork-PR hat ohnehin keinen
   Token mit Schreibrecht. **Kosten:** ein `if`-Ausdruck mehr, und ein roter PR-Lauf schließt das offene
   Nacht-Issue nicht mit.

## Akzeptanzkriterien

1. `vater-von-null.spec.ts` legt am Objective aus 8a eine **Etappe** an – Bereich (Fach), Messlatte, Status –
   und prüft sie in der Zeile; der Lernziel-Abschnitt ist weg.
2. Der Nachtlauf ist wieder grün, belegt durch einen **echten Lauf** (manuell ausgelöst oder die nächste
   Nacht), nicht durch einen lokalen.
3. Ein roter Nachtlauf **erreicht jemanden**. Der Weg ist offen (siehe unten), das Ergebnis ist zu belegen:
   ein absichtlich rot gemachter Lauf muss die Meldung auslösen.
4. Playwright bleibt **draußen** aus dem Freigabe-Tor; `ci.yml:142-144` bleibt, wie es ist.

## Offene Punkte

1. ~~**Wie wird zugestellt?** Bei fehlgeschlagenen *scheduled* Workflows benachrichtigt GitHub bestenfalls per
   E-Mail-Einstellung – verlässlich ist das nicht. Kandidaten: ein Issue, das der Workflow bei Rot selbst
   anlegt bzw. wiederverwendet; ein Badge in der README; ein Schritt, der die Anmerkungs-API bedient.
   **Empfehlung:** das Issue – die einzige Variante mit einem Zustand (offen/geschlossen), und sie braucht
   keine zweite Infrastruktur.~~ → Entscheidungen 2 und 3.

## Schätzung

**Größe: S** — Anker: „`childId` aus dem Test-Pfad ziehen" (B-01). Eine Spec-Passage und ein Workflow-Job,
kein Produktivcode und kein Vertrag. Der Grill-Teil liegt bereits im Paket-Dokument
([testabdeckung-plan.md](../testabdeckung-plan.md), Entscheidung 2), darum ging die Story von
`ausformuliert` direkt in die Arbeit.

**Risiken.** Zwei, beide beim Bauen eingetreten und behandelt:

1. *Die neuen Selektoren hängen daran, dass es zufällig nur ein Ziel gibt.* Der Scope-Wähler steht je
   Karte einmal; ohne Eingrenzung auf die Karte wäre jeder Label-Zugriff bei einem zweiten Objective eine
   Strict-Mode-Verletzung. → alles unter einem `.card`-Locator.
2. *Die Zustellung kann das Urteil des Laufs umdrehen.* Als Schritte im Testjob färbte ein gh-Fehler den
   grünen Lauf rot, und ein `timeout-minutes`-**Abbruch** löste weder `failure()` noch `success()` aus –
   genau der Fall, für den die Reißleine da ist, hätte als Einziger nichts zugestellt. → eigener Job mit
   `needs: e2e` und `always()`.

**Angriffsplan.** Kein Backend beteiligt. (1) Spec-Abschnitt umschreiben und lokal rot→grün belegen;
(2) Zustellung als eigener Job; (3) echter CI-Lauf für AK 2, absichtliches Rot für AK 3.

**Testweg.** `npx playwright test vater-von-null.spec.ts` lokal (Gegenprobe: derselbe Lauf auf dem alten
Stand muss rot sein), danach die volle Suite, danach ein echter Lauf des Workflows – AK 2 und 3 sind
ausdrücklich **nicht** lokal belegbar.

## Verlauf

- **2026-07-30** — geerntet (ungeprüft).
- **2026-08-01** — ausformuliert **und neu zugeschnitten**: die Prämisse „in CI nie gelaufen" ist widerlegt
  (zwei Läufe, der zweite rot). `art` wird `Defekt`, `prio` steigt auf P1 – ein rotes Tor, das niemanden
  erreicht, entwertet jedes weitere Tor, das das Paket danach baut.
