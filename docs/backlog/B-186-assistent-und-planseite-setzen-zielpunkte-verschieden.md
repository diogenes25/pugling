---
tags: [typ/story, status/idee, bereich/frontend, rolle/supervisor]
aliases: [Assistent ignoriert die Punkte-Empfehlung, Intensität vs. Empfehlung]
status: idee
prio: P3
art: Frage
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: B-45, Fund des frontend-reviewer
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-186 · Assistent und Plan-Seite setzen die Zielpunkte nach verschiedenen Regeln

Dieselbe Übung mit `rewardPoints = 15` ergibt über `/vater/plan/{id}` eine Position mit **15** und über den
Assistenten `/vater/wizard` eine mit **20**. Der Assistent schickt `pointsGoalMet` immer explizit mit
(`VaterWizard.tsx:362`, Wert aus `INTENSITY.points` 10/20/30, `:34-38`), und „Position override zuerst"
lässt den Server-Fallback aus [B-45](B-45-creator-punkte-empfehlung.md) damit nie greifen.

**Das ist womöglich richtig so** — und darum eine `Frage`, kein `Defekt`: Im Assistenten ist der Wert aus
der gewählten Intensität abgeleitet, wird im Intensitäts-Hinweis und in der Zusammenfassung (`:640`)
angezeigt und ist damit eine **ausdrückliche Aussage des Vaters**, wie `penaltyCoins`. Eine Empfehlung des
Creators zu übernehmen, die der Vater gar nicht gesehen hat, wäre dort möglicherweise die schlechtere
Antwort.

Was fehlt, ist nicht die Einheitlichkeit, sondern die **Festlegung**: B-45s Akzeptanzkriterium 4 und
Entscheidung 2 lesen sich universell, und der nächste Leser wird eine der beiden Seiten „reparieren". Der
Hilfetext ist im Rahmen von B-45 schon relativiert worden („auf der Plan-Seite … der Assistent setzt die
Punkte stattdessen aus der gewählten Intensität"), damit die Oberfläche in der Zwischenzeit nicht lügt.

Zu klären: Gewinnt die Intensität oder die Empfehlung? Falls die Empfehlung — es gibt dafür schon ein
Muster eine Zeile weiter (`wizardFinish.ts:109-112` lässt `stage` weg, „weggelassen entscheidet der
Server"), es kostet dann aber die Feinschliff-Anzeige und die Überblick-Zeile. Ein „bleibt wie es ist" ist
ein vollwertiges Ergebnis; dann wird diese Story `verworfen` mit Grund.

## Verlauf

- 2026-09-01 · Aufgenommen aus dem `frontend-reviewer`-Review zu B-45. Die Fundorte sind vom Reviewer am
  Code belegt, die Wirkung habe ich nicht selbst gefahren — darum `unverifiziert`.
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): besteht, Belege stimmen (Intensität 10/20/30 `VaterWizard.tsx:37-39`, immer mitgeschickt `:366`). Die Antwort entscheidet zugleich B-201 (Feldhilfe).
