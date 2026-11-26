---
tags: [typ/story, status/abgenommen, bereich/training, rolle/student]
aliases: []
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: "backend"
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-26.md#retrospektive
grund: ""
ersetzt_durch: []
entgangen_bei: [B-712]
nachgeschaut: ""
---

# B-1025 · „keine Lösung“ wurde nur in einer Schreibweise erkannt

Fund im Merge-Review der Läufe M/N/O (2026-11-26) über die abgenommene Arbeit von B-712: die leere Lösungsmenge wurde nur als
genau „keine Lösung“ in einer Unicode-Form erkannt; „keine Losung“, „keine Loesung“ und eine zerlegte Schreibweise (NFD) galten
als falsch. Behoben in `61a11f55` (Vergleich nach NFC, beide Ersatzschreibweisen). Nachträglich als Story abgelegt in der
Retrospektive von Sprint 192, weil ein Defekt in abgenommener Arbeit eine eigene Story bekommt (README).

## Verlauf

- **2026-11-26** — angelegt und `abgenommen` mit Commit `61a11f55` (Test in `61a11f55`, Suite am Endstand `4e70c583`
  2609/2609); Rollengang entfällt, Bewertungsregel ohne eigene Oberfläche.
