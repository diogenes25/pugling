---
tags: [typ/story, status/idee]
aliases: []
status: idee
prio: P2
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Chat mit dem Vater, 2026-11-17 (Mathe in die App; Material raw/Math/lambacher-schweizer-9-sh-pugling/md)"
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-639 · Dezimalkomma im Zahlenvergleich der Rechenaufgaben

`AnswerChecking.NumericMatch` (`backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs`) parst die Antwort mit
`NumberStyles.Number` und `CultureInfo.InvariantCulture`. Dort ist das Komma ein Tausendertrenner – „2,5“ würde
vermutlich als 25 gelesen und als falsch gewertet, obwohl ein deutsches Kind genau so schreibt. **Nicht reproduziert:**
ob das Frontend vor dem Absenden umwandelt, ist ungeprüft; erst am laufenden Stand nachstellen. Fällt heute kaum auf,
weil die Seed-Rechenaufgaben ganzzahlig sind; mit Mathe Klasse 9 (gerundete Lösungen wie −0,4 und 2,3) wird es zur Regel.

## Verlauf

- **2026-11-17** — angelegt (Quelle: Konzept „Mathe in die App" aus dem Chat, Lambacher Schweizer 9, Kapitel IV).
