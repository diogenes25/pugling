---
tags: [typ/story, status/idee, bereich/ki-agent, rolle/creator, lerntechnik/lueckentext]
aliases: [KI-Ablenker je Lücke]
status: idee
prio: P3
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: B-599 Entscheidung 10 (Grill-Runde 2026-11-16)
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-601 · Der KI-Creator schreibt Ablenker je Lücke

Mit [B-599](B-599-lueckentext-eingabearten-wie-vokabeln.md) kann eine Lücke eigene Ablenker tragen, die auf der
Stufe „Auswahl je Lücke“ Vorrang vor den Antworten anderer Lücken haben. Der KI-Creator schreibt heute nur Ablenker
für eine gemeinsame Wortbank (`ClozeStrategy`, mindestens 2). Idee: Er liefert je Lücke bis zu 4 ähnliche, falsche
Formen (*doesn't she* → *don't she*, *isn't she*, *does she*, *doesn't he*). Nach B-460 entscheidet das Programm,
was nur eine richtige Antwort hat: Die Regelprüfung muss sicherstellen, dass kein Ablenker als Lösung oder
Alternative der Lücke durchgeht (vgl. [B-461](B-461-lueckentext-ablenker-zaehlen-als-loesung.md)). Zu klären:
Prompt-Form, Anzahl, Verhältnis zur gemeinsamen Bank und ob `laden.py` dasselbe Feld lernt.

## Verlauf

- **2026-11-16** — angelegt aus B-599, Entscheidung 10.
