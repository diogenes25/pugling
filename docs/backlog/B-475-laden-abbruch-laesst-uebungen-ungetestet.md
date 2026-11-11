---
tags: [typ/story, status/idee, bereich/werkzeug, rolle/creator]
aliases: [laden.py Abbruch, Selbsttest nur für neue Übungen, grammar alternatives]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: ".claude/skills/lehrwerk-aufbereiten"
migration: ""
vertragsbruch: ""
quelle: Lauf des Skills lehrwerk-aufbereiten an Découvertes Série bleue 2 (2026-11-11)
unverifiziert: true
entgangen_bei: []
grund: ""
ersetzt_durch: []
---

# B-475 · Bricht `laden.py` mitten im Anlegen ab, bleiben die schon angelegten Übungen für immer ungetestet

Beobachtet gegen die Wegwerf-DB: Übung 23 von 28 scheiterte mit `400 unknown_field`
(`config.tasks[2].alternatives`). Die 22 Übungen davor standen da schon auf dem Server, der Selbsttest läuft aber
erst am Ende. Der nächste Lauf meldete sie als „schon da“ und testete nur die 12 neuen – der Lauf sah grün aus,
obwohl 22 Übungen nie durch den Selbsttest gegangen waren. Bemerkt nur, weil die Zahl der Testzeilen nicht zur
Zahl der Übungen passte; behoben durch eine frische DB. Gegen die echte Instanz gäbe es diesen Ausweg nicht.

Die Ursache des Abbruchs ist ein zweiter, kleinerer Fund: `--pruefen` kennt die Felder der Typen nicht. Eine
`grammar`-Aufgabe hat nur `prompt`, `answer`, `ruleHint` (`GrammarTask` in `ExerciseConfigs.cs`), also keine
`alternatives` – anders als Lückentext und Übersetzung. `FORMAT.md` sagt das nicht, und die Prüfung fängt es
nicht; der Fehler kommt erst vom Server, mitten im Lauf.

Offen und nicht gemessen: ob der Grammatik-Vergleich einen typografischen Apostroph (`qu’`) wie den geraden
(`qu'`) wertet. Ohne `alternatives` lässt sich das in der Übung nicht absichern.

Naheliegende Abhilfe: (1) der Selbsttest läuft über **alle** Übungen des Ordners, nicht nur über die neuen;
(2) `--pruefen` lehnt unbekannte Felder je Typ ab (Feldlisten aus dem Vertrag); (3) ein Satz in `FORMAT.md`.

## Verlauf

- **2026-11-11** — angelegt (Lauf an Découvertes Série bleue 2).
