---
tags: [typ/protokoll, bereich/backlog]
---

# PM-Sitzung: Backlog-Lauf 2026-11-13 (PO und Stakeholder in einer Hand)

**Datum:** 2026-11-13  ·  **Moderation:** PM (Claude)
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Den offenen Bestand um mindestens 30 % senken – neu entstehende Stories eingerechnet – mit Sprints, die
zusammen rund 30 % der offenen Stories tragen.

## Freigabe

Auftrag des Nutzers im Dialog am 2026-11-13 (wörtlich): „Arbeite das Backlog als PO durch und übernehme auch die
Position des Stakeholders beim grillen. […] Bei Fragen übernehme auch die Position des Stakeholders und nimm die von
Dir Empfohlenen Vorschläge.“ Damit gilt für diesen Lauf ausdrücklich auch das Grillen von `Wunsch` und `Frage`
(abweichend von README → „Der Backlog-Lauf“, Zusatz 1). Jede so getroffene Entscheidung trägt im `## Verlauf` den
Vermerk **„Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13)“** und ist damit umstoßbar.

Bewusst **nicht** umgestoßen: ausdrückliche frühere Entscheidungen des Nutzers (z. B. B-251/B-459: lokales
Vision-Modell). Halt-Regel: die feinere Nachtlauf-Fassung (behebbare Review-Funde sofort im selben Sprint beheben).

Gearbeitet wird im Worktree `.claude/worktrees/backlog-lauf` (Branch `auto/backlog-lauf-2026-11-13`), weil im
Haupt-Checkout die nicht committete B-534-Arbeit einer anderen Sitzung liegt.

## Ausgangsbestand

**52 offene Stories** (Index nach `b0653edb`; B-534 gehört der parallelen Sitzung und zählt nicht). Ziel: **höchstens
36 offen** am Ende, jede in diesem Lauf neu angelegte Story mitgezählt.

## PM-Synthese & Priorisierung

Beobachtung aus dem Abgleich am selben Tag (`b0653edb`): keine Story ist gegenstandslos, aber drei Sammel-Stories
tragen keine eigene Arbeit, und die meisten offenen `Frage`-Stories warten nur auf eine Entscheidung, die der Code
schon eng macht. Der rote Faden: **Fairness und Verlässlichkeit dort, wo das Kind übt und der Vater anlegt** – erst
das Üben (wo der Sohn Ungerechtigkeit sofort merkt), dann die Integrität des Katalogs (wo stiller Datenverlust droht),
dann Restposten.

| Prio | Story | Größe | wo | Sprint |
| --- | --- | --- | --- | --- |
| P2 | B-531 Fokus nach dem Aufdecken, B-532 Netzfehler beim Bewerten | XS/S | frontend | 1 |
| P3 | B-433 Vorschau verbraucht Einführung, B-435 „nur neue“ fragt Einführung nie, B-437 Artikel-Toleranz nur bei Nomen, B-422 Beinahe-Treffer | S | backend | 1 |
| P2/P3 | B-404, B-407, B-152, B-208, B-328, B-410 (Katalog, Vertrag, Wächter) | S | backend | 2 |
| P2/P3 | B-432, B-445, B-465, B-474, B-384, B-408 | XS–S | gemischt | 3 |
| – | B-217, B-247, B-374 Sammel-Stories | – | – | geschlossen (geteilt) |

Nicht in diesem Lauf: große Wünsche (B-09, B-20, B-39, B-218, B-220, B-248, B-249), Gerätechecks (B-31), Stories,
die Dateien der parallelen B-534-Arbeit berühren (B-16, B-140, B-369, B-380).

## Refinement

Drei Agenten haben die 18 Kandidaten parallel bis `geschaetzt` gebracht (`cf07416f`); jede Entscheidung trägt den
Stakeholder-Vermerk. Zwei Abweichungen von der jeweiligen PM-Empfehlung, beide begründet in der Story:

- **B-422:** Im Üben wird ein Tippfehler **nicht** verziehen, nur gekennzeichnet („Falsch geschrieben“). Die
  Empfehlung (Abstand 1 als Treffer) widerspräche der Nutzer-Entscheidung 2 aus B-524 („falsch geschrieben ist
  falsch“) – frühere Nutzer-Entscheidungen stößt dieser Lauf nicht um.
- **B-432:** Der Einführungstag einer täglichen Pflicht ist nur **malusfrei**, nicht erfüllt (keine Ziel-Punkte).
  Berührt den „Stick“ – zur Kenntnis des Nutzers.

Bewusst außerhalb der Sprints: **B-407** (Dialog in `ExerciseEditModal.tsx`, das B-534 unkommittiert umbaut), der
Planbau-Teil von **B-408** → neue Story **B-540** (`wartet_auf` B-534). **B-410** (M, neues Tor) kommt als letzte
Story des Laufs, weil ein neues Tor laut README den Lauf anhält.

## Sprint 69 — Ziel & Umfang

**Sprint-Ziel:** Der Sohn erlebt das Üben fair und verlässlich – der Fokus bleibt auf der Karte, ein Netzfehler
schluckt keine Bewertung, die Vorschau des Vaters stiehlt keine Einführung, „nur neue“ fragt die eingeführten Wörter
ab, die Artikel-Toleranz gilt nur für Nomen, und ein Tippfehler ist als solcher erkennbar.
**Umfang:** B-531, B-532 (Arcade) · B-435, B-437, B-433, B-422 (Backend, B-422 mit Anzeige im Blatt).
**Entwickler-Brief:** Quelle der Wahrheit bleibt der Server (`PositionPracticeController.Review`,
`PositionPlayService`, `ExerciseContentResolver`, `AnswerGrader`); die Arcade zeigt nur an. Guards: Rollen-Prüfung
`User.IsStudent()` wie im Test-Controller; kein Vertragsbruch, nur das additive Feld `misspelled`. Keine Migration.
Testweg: Integrationstests je Story, Vitest/RTL für `SohnPractice`, Mutanten-Probe je AK-Fall, volle Suite.

## Stakeholder-Entscheidungen ohne Sprint

- **B-217, B-247, B-374 verworfen (geteilt):** Eine Sammel-Story trägt keine eigene Arbeit; der Index führt jede
  Teil-Story einzeln. Kosten: der Vergleichsplan ist nur noch über `docs/` und die Teil-Stories erreichbar.
