---
tags: [typ/story, status/idee, bereich/frontend, rolle/student]
aliases: [Ziele 2/2, Truhe zu]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Nachschau Sprint 83 im Nachtlauf 2026-11-17 (docs/pm-sitzung-2026-11-17-nachtlauf.md)"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-606]
---

# B-612 · Die Startseite zählt die Belohnungsziele anders als der Server – „2/2" neben einer geschlossenen Truhe

`SohnHome.tsx` rechnet mit `belohnungsZiele` (~:334) über die **flache** Positionsliste aller Pläne nach („gibt es
irgendwo Tagesziele, zählen nur Tagesziele"); der Server wählt **je Plan** (`RewardObligations`, B-611). Mit einem
Tagesziel-Plan und einem Wochenziel-Plan zeigt die Startseite „Ziele heute 2/2" und einen vollen Balken, während die
Truhe „Schaffe deine Tagesziele…" sagt und der Bonus gesperrt ist. Der Kommentar „dieselbe Auswahl wie
`RewardObligations`" (~:331) stimmt bei mehreren Plänen nicht mehr. Das Kind merkt so etwas sofort.

Naheliegend: Belohnungsziel-Zahlen und -Art vom Server in `TodayResponse` liefern (Regel an der Senke) statt im Client
nachzurechnen; hängt an der Regel aus B-611.

## Verlauf

- **2026-11-17** — angelegt aus der Nachschau von Sprint 83 (🟡, `pugling-reviewer`), `entgangen_bei: [B-606]`.
