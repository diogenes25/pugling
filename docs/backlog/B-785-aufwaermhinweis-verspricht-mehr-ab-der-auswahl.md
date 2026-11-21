---
tags: [typ/story, status/idee, bereich/gamification, rolle/student]
aliases: [Aufwärmhinweis verspricht mehr]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: "Review Sprint 108 (B-608, frontend-reviewer 🟢) im Nachtlauf 2026-11-21, docs/pm-sitzung-2026-11-21.md"
grund: ""
ersetzt_durch: []
entgangen_bei: [B-657]
---

# B-785 · Der Aufwärm-Hinweis verspricht „ab der Auswahl gibt es mehr“, die Auswahl zahlt aber gleich viel

Der Hinweis über einer Karteikarten-Runde im Bonus-Training (`bonus.hintSelf`, `frontend/src/i18n/de.ts`) sagt „jedes
gewusste Wort bringt {{coins}} 🪙, ab der Auswahl gibt es mehr“. Seit B-657 zahlt die Karteikarte 1, die Auswahl aber
ebenfalls 1 (`ExtraTrainingOptions.cs`: `CoinsPerSelfAssessHit = 1`, `CoinsPerChoiceHit = 1`, `CoinsPerTypedHit = 3`;
`appsettings.json:38-40`). Mehr gibt es erst beim Tippen. Dieselbe Art Übertreibung wie [B-608](B-608-bonus-kopf-nennt-zu-hohen-satz.md),
nur im festen Text statt in einer Zahl.

Am Code nachgelesen, nicht im Browser gesehen. Gefunden vom `frontend-reviewer` in Sprint 108 als 🟢 außerhalb des Diffs;
bewusst nicht dort behoben (anderer Satz, fremder Text).

Mögliche Reparatur (zu entscheiden): den Text auf „ab dem Tippen“ ändern, oder ihn aus den Sätzen des Servers ableiten,
damit eine geänderte Konfiguration ihn nicht wieder falsch macht. Rote Probe: Vitest, der den Hinweis gegen die Sätze
des Vorrats hält.

## Verlauf

- **2026-11-21** — angelegt aus dem Review von Sprint 108 (`idee`, `entgangen_bei: [B-657]`).
