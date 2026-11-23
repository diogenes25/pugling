---
tags: [typ/story, status/idee, bereich/katalog, rolle/creator]
aliases: [Aufbau-Weg Wettlauf]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: offen
vertragsbruch: offen
quelle: "Nachschau von B-700 im Nachtlauf 2026-11-23-c (docs/pm-sitzung-2026-11-23-c.md, pugling-reviewer 🟢)"
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-700]
wartet_auf: ""
---

# B-860 · Zwei gleichzeitige Bearbeitungen bauen am Aufbau-Weg eine Kette oder einen Kreis

Die Ein-Ebenen-Regel von [B-700](B-700-aufbau-stufe-am-katalog.md) prüft `BuildPathProblemAsync`
(`backend/Pugling.Api/Controllers/Creator/ExerciseControllerBase.cs:285-305`) vor dem Schreiben, die Datenbank hält sie
nicht. Laufen `PUT X {target: T}` und `PUT T {target: Y}` gleichzeitig, bestehen beide die Prüfung, und gespeichert ist
die Kette X→T→Y; ebenso ein Kreis A→B, B→A. Kein 500 – der Verlauf von B-700 behauptet das Gegenteil. In einer Familie
unwahrscheinlich; ein PUT ohne die Felder repariert es. Gelesen am Code, nicht nachgestellt.

## Verlauf

- **2026-11-23** — angelegt aus der Nachschau von B-700 (Nachtlauf 2026-11-23-c).
