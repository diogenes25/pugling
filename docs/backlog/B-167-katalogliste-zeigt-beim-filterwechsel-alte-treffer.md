---
tags: [typ/story, status/idee, bereich/frontend]
aliases: [alte Treffer nach Filterwechsel, Trefferzahl passt nicht zum Filter]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: frontend-reviewer beim Review von B-163 (docs/backlog/B-163-art-und-typ-tragen-dieselben-woerter.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-167 · Katalogliste im Planbau zeigt beim Filterwechsel die alten Treffer weiter

## Behauptung (ungeprüft — das ist der Punkt)

`frontend/src/vater/PlanPositions.tsx:409` soll beim **Filterwechsel** über die `ExerciseFilterBar` die
Zeilen des vorigen Filters samt der alten „(N Treffer)"-Zahl weiter anzeigen, ohne ein Ladesignal. Damit
läse der Vater eine Trefferzahl, die zu seinem gerade eingestellten Filter nicht gehört.

Das wäre die **andere Hälfte** der bekannten `loading && data === null`-Regel
(`frontend/CLAUDE.md`): Die Regel wurde eingeführt, damit ein `reload` nicht alle aufgeklappten Zeilen
aushängt — sie hat aber den Fall „eine **andere** Abfrage läuft" nicht mitgedacht, und da ist das
Weiterzeigen kein Vorteil, sondern eine falsche Aussage.

**Ausdrücklich nicht belegt.** Der Fund kommt aus dem Review von B-163, der Reviewer hat ihn selbst als
nicht vermessen gekennzeichnet, und B-163 hat ihn nicht verursacht. Er steht hier, weil Wissen, das beim
genauen Hinsehen anfällt, sonst verdampft — nicht, weil er feststeht. Erster Schritt ist reproduzieren,
nicht reparieren ([Backlog-Regel](README.md): der Ist-Stand braucht Belege aus dem echten Code).

## Offene Punkte

1. Tritt es überhaupt auf? Empfehlung: an der laufenden App im Planbau einen Typ-Filter umstellen und
   beobachten, ob Zeilen und Trefferzahl stehen bleiben. Erst wenn ja, weiterlesen.
2. Ist es dieselbe Stelle wie B-116 oder eine zweite? Empfehlung: B-116 lesen, bevor eine neue Lösung
   entworfen wird — es könnte ein Duplikat sein, und dann gehört diese Story verworfen mit Verweis.
3. Wenn es auftritt: Ladesignal oder Zeilen leeren? Empfehlung: **Signal**, nicht leeren — das Leeren war
   genau der Schaden, den die bestehende Regel abgestellt hat. Die Trefferzahl darf allerdings nicht
   stehen bleiben, denn sie ist die falsche Aussage, nicht die Zeilen.

## Verlauf

- 2026-08-13 · Aufgenommen. Vom `frontend-reviewer` beim Review von B-163 als Umfeld-Beobachtung
  gemeldet, von ihm selbst als nicht vermessen gekennzeichnet. Bleibt `unverifiziert`, bis es reproduziert
  ist.
