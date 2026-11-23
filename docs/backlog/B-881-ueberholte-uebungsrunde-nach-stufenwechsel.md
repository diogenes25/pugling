---
tags: [typ/story, status/idee, bereich/api, bereich/training, rolle/student]
aliases: [Überholte Übungsrunde, Runde nach Stufenwechsel]
status: idee
prio: P3
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: "Nachschau von B-848 im Nachtlauf 2026-11-23-e (docs/pm-sitzung-2026-11-23-e.md)"
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-848]
wartet_auf: ""
---

# B-881 · Eine offene Übungsrunde von gestern spielt nach einem Stufenwechsel die falsche Übung

Schwester von [B-880](B-880-ueberholter-testversuch-nach-stufenwechsel.md): Eine Übungsrunde (`PracticeSession`) friert
ihre Reihenfolge als Item-Indizes ein und löst ihre Übung über `session.Day` auf. Wechselt der Vater heute die Stufe, sperrt
`InPlayTodayAsync` nur Runden von heute; eine offene Runde von gestern läuft über ihre Id weiter und wertet ihre Karten gegen
die Übung der neuen Stufe (Punkte, Kasten, Lernstand der falschen Übung). Anders als beim Test trägt die Runde keine Stufe,
gegen die man prüfen könnte. Nicht am laufenden Produkt nachgestellt (`unverifiziert`); nur über eine von Hand gebaute
Anfrage erreichbar.

## Verlauf

- **2026-11-23** — Bei der Nachschau von B-848 gefunden (Nachtlauf E). Nicht im Sprint gebaut: das Sprint-Ziel hält ohne
  sie, und die Lösung braucht eine Wahl (Stufe an der Runde stempeln wie beim Versuch, oder Runden früherer Tage
  schließen) – als Defekt autonom grillbar, aber eine eigene Story.
