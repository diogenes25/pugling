---
tags: [typ/story, status/idee, bereich/training, rolle/student]
aliases: [Übersetzung ohne Punkt falsch]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: B-375
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-457 · Eine richtige Übersetzung ohne Schlusspunkt wird als falsch gewertet

Gemessen beim Laden des Access-Lehrwerks (B-375, 2026-11-10) im Testmodus (`preview/check`) einer
Translation-Übung: `Excuse me, how can I get to the station` gegen die Lösung `… station?` und
`the museum is on the corner` gegen `The museum is on the corner.` wurden beide als falsch gewertet. Groß-
und Kleinschreibung verzeiht der Vergleich (`StageMechanics.Normalize`), Satzzeichen am Ende nicht. Ein Kind,
das den Punkt weglässt, verliert also Punkte für eine richtige Übersetzung. Umgangen ist es bisher nur in
den Start-Übungen dieses Lehrwerks, mit zusätzlichen Lösungsvarianten ohne Schlusszeichen. KI-erzeugte und
handgeschriebene Übungen haben diese Varianten nicht. Zu klären: ob Schlusszeichen (`.?!`) beim Vergleich
grundsätzlich ignoriert werden und ob das für Cloze und Grammar genauso gilt.

## Verlauf

- **2026-11-10** — angelegt (Fund beim Laden des Lehrwerks in B-375).
