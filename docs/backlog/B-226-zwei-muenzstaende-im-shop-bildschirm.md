---
tags: [typ/story, status/idee, bereich/frontend, bereich/shop, rolle/student]
aliases: [Zwei Münzstände, Kopfzeile vs. Shop-Chip]
status: idee
prio: P3
art: Frage
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-10-17-nacht.md (Browser-Rollengang Sohn)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-226 · Der Shop-Bildschirm zeigt zwei verschiedene Münzstände

Beim Browser-Rollengang des Nachtlaufs 2026-10-17 (Sohn „1" in einer frisch geseedeten Wegwerf-DB) stand in
der oberen Leiste der Arcade „🪙 60 · 💎 300", im Kopf des Shop-Bildschirms direkt darunter „🪙 50 · 💎 300".
Zwei Zahlen für dieselbe Währung im selben Blickfeld, ohne dass eine von beiden sagt, warum sie anders ist.
Zu prüfen: aus welchen zwei Quellen die Zahlen kommen (Punkte-Endpunkt der Arcade vs. `shopView`), ob die
Differenz eine fachliche Bedeutung hat (reservierte Münzen? Malus-Schulden? Zeitfenster-Bonus?) oder ein
Darstellungsfehler ist – und ob der Sohn das als „Fake" liest. Nicht in dieser Sitzung untersucht, weil
außerhalb des Sprint-Umfangs; aufgefallen ist es nur, weil ein Mensch (bzw. der Agent im Browser) auf den
Bildschirm sah – keine E2E zählt die beiden Chips gegeneinander.

## Verlauf

- **2026-10-17** — angelegt aus dem Browser-Rollengang des Nachtlaufs (Sprint 1), außerhalb des Diffs.
