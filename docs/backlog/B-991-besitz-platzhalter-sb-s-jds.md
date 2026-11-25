---
tags: [typ/story, status/idee, bereich/training, rolle/student, lerntechnik/vokabeln]
aliases: [sb.'s, jds., Besitz-Platzhalter]
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

# B-991 · Der Besitz-Platzhalter „sb.'s“ / „jds.“ ist kein Platzhalter

Beim Bau von [B-626](B-626-antwort-verlangt-mehr-als-die-vokabel.md) an der DB-Kopie vom 2026-11-25 gefunden: Vokabel 825
„(to) attract/catch/grab sb.'s attention“. Die Platzhalter-Liste (`backend/Pugling.Contracts/Common/WordListPlaceholders.cs`,
Entscheidung 1 von B-626: sth., sb., qn, qc, jdn., jdm., etw.) kennt die Genitivform nicht: „sb.'s“ ist für sie ein Wort, also
verlangt der Vergleich es, und die Kästchen machen „sb“ und „s“ zu tippbaren Feldern. Auf der deutschen Seite steht
„jds.“ (Abkürzungsliste von B-462, aber nicht Platzhalter-Liste).

## Offene Punkte

1. Gehören „sb.'s“ und „jds.“ zum Beiwerk? Entscheidung 1 von B-626 hat sie nicht genannt – ob mit Absicht, ist offen.
2. Was soll das Kind dann tippen: „attract attention“ (der Kern ohne Besitzer) liest sich falsch.

## Verlauf

- **2026-11-25** — angelegt beim Bau von B-626 (Datenprüfung AK 7). Bewusst nicht mitgebaut: Entscheidung 1 nennt die Liste
  abschließend.
