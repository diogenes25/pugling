---
tags: [typ/story, status/idee, bereich/training, rolle/student, lerntechnik/vokabeln]
aliases: [Salat mit qc, Buchstabensalat Platzhalter]
status: idee
prio: P3
art: Wunsch
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-25.md#iteration-162--umgesetzt
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-992 · Der Buchstabensalat legt die Buchstaben eines Platzhalters mit

Beim Bau von [B-626](B-626-antwort-verlangt-mehr-als-die-vokabel.md) am Code gefunden: Die Kästchen setzen Platzhalter fest
(`StageMechanics.EntryLetterBoxPattern`), der Buchstabensalat nicht. Ein Eintrag aus Buchstaben und Leerzeichen
(„marcher sur qc“, `StageMechanics.IsScramblable`) wird als Salat gespielt, und `VocabularyExerciseType.LetterTileSource`
gibt den ganzen Eintrag als Plättchen heraus – das Kind legt „q“ und „c“ mit. Ein Eintrag mit Punkt oder Klammer
(„point sth. out“) ist nicht salatfähig und fällt ohnehin auf die Kästchen zurück.

Entscheidung 2 von B-626 spricht nur von den Kästchen; Entscheidung 4 („überall, wo getippt wird“) lässt offen, ob Plättchen
legen „tippen“ ist.

## Offene Punkte

1. Soll der Salat den Platzhalter als festen Text zeigen (Wortgrenzen-Maske `ScrambleWordPattern` mit festem „qc“) oder ihn
   weglassen?

## Verlauf

- **2026-11-25** — angelegt beim Bau von B-626; bewusst nicht mitgebaut, weil Entscheidung 2 nur die Kästchen nennt.
