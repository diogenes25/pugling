---
tags: [typ/story, status/idee, bereich/gamification, rolle/student]
aliases: [Wochenziel-Plan sperrt Truhe]
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

# B-611 · Ein Plan nur mit Wochenzielen sperrt die Tagestruhe und den Bonus über alle Pläne

`PositionProgressService.ComputeChildDayAsync` wendet `RewardObligations` **je Plan** an: ein Plan ohne Tagesziele gibt
seine Wochenziele als Belohnungsziele ab (B-535: „sonst gäbe es die Truhe für nichts"). Seit B-606 gilt das über alle
Pläne des Kindes, seit B-539 hat ein Kind real mehrere: Französisch mit zwei Tageszielen, Mathe mit einem Wochenziel ⇒
am Montag ist Französisch erledigt, Truhe und Bonus bleiben trotzdem bis zum Mathe-Wochenziel zu – womöglich die ganze
Woche. Genau diese Wirkung wollte B-535 vermeiden (`PositionProgressService.cs` ~:265, Doku von `RewardObligations`);
deren Grund („Truhe für nichts") gilt nicht mehr, sobald ein anderer Plan Tagesziele hat.

Naheliegend: die Auswahl über den **Kind-Tag** treffen – Tagesziele aller Pläne, wenn es irgendwo welche gibt, sonst
alle Ziele. Kein Test hält heute gemischten Takt über Pläne (`KindTagessichtTests`).

## Verlauf

- **2026-11-17** — angelegt aus der Nachschau von Sprint 83 (🟡, `pugling-reviewer`), `entgangen_bei: [B-606]`.
