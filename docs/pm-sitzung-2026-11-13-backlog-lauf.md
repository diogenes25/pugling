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

## Stakeholder-Entscheidungen ohne Sprint

- **B-217, B-247, B-374 verworfen (geteilt):** Eine Sammel-Story trägt keine eigene Arbeit; der Index führt jede
  Teil-Story einzeln. Kosten: der Vergleichsplan ist nur noch über `docs/` und die Teil-Stories erreichbar.
