---
tags: [typ/story, status/idee, bereich/gamification, rolle/student]
aliases: [CoinsPerHit übertreibt]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Nachschau Sprint 82 im Nachtlauf 2026-11-17 (docs/pm-sitzung-2026-11-17-nachtlauf.md)"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-597]
---

# B-608 · Der Kopf eines Bonus-Laufs verspricht mehr Münzen je Treffer, als manche Karten zahlen

`ExtraRunResponse.CoinsPerHit` nennt für eine Freitext-Runde über eine Liste mit Richtung „beides" den getippten Satz
(3 je Treffer, `backend/Pugling.Api/Services/Student/ExtraTrainingService.cs:444`). Seit B-597 zahlen die vorwärts
gestellten Karten dieser Runde aber nur den Auswahl-Satz 1 (`CardRatesAsync`, `:400-414`). Die Abrechnung am Ende
stimmt (`CoinsForHits`), nur die Ansage vorab übertreibt – der Sohn merkt so etwas sofort.

## Verlauf

- **2026-11-17** — angelegt aus der Nachschau von Sprint 82 (🟢 des `pugling-reviewer`), `entgangen_bei: [B-597]`.
