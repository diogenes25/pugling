---
tags: [typ/story, status/idee, bereich/frontend]
aliases: [kein Retry am Fehlerbanner, gescheitertes Lesen nur per F5, useAsync.reload ungenutzt]
status: idee
prio: P3
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: B-172, Entscheidung 5
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-183 · Ein gescheitertes Lesen ist ohne F5 nicht zu wiederholen

Scheitert ein Abruf, zeigt die Oberfläche einen Satz — und lässt den Nutzer damit stehen. `useAsync` hält
mit `reload` längst den Weg zurück bereit (`frontend/src/lib/useAsync.ts:23`), aber **von 68 `banner err`
im Frontend bietet keines einen erneuten Versuch** (gezählt am 2026-09-01); die 75 `reload`-Aufrufe hängen
ausnahmslos an einer geglückten Mutation, nie an einem Fehler. Der Nutzer hilft sich mit F5 und verliert
dabei jede Eingabe, die im Formular daneben schon stand.

Aufgekommen in der Grill-Runde zu [B-172](B-172-fachauswahl-loescht-wenn-ihre-optionen-fehlen.md): Dort
wird ein Auswahlfeld während eines Ausfalls unbrauchbar, weil seine Optionen fehlen — und genau dann wäre
ein zweiter Versuch die einzige Handlung, die weiterhilft. B-172 baut ihn bewusst **nicht** ein: fünf
Banner mit Knopf und 68 ohne wären eine Inkonsistenz nach dem Muster von B-178 und B-126, und das Ziel von
B-172 (das Feld lügt nicht mehr) ist ohne ihn erfüllt.

Zu klären beim Ausformulieren ist vor allem, wie viele der 68 Stellen überhaupt ein eindeutiges `reload`
haben: mehrere Seiten führen drei und mehr `useAsync`-Instanzen, und welche zu welchem Banner gehört, ist
nicht überall aus dem Code abzulesen.

## Verlauf

- 2026-09-01 · Aufgenommen aus der Grill-Runde zu B-172 (Entscheidung 5). Die beiden Zahlen (68 Banner
  ohne Retry, 75 `reload`-Aufrufe) sind an diesem Tag gezählt; welche Stelle *welches* `reload` bräuchte,
  ist nicht geprüft — darum `unverifiziert`.
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): besteht, **der Ist-Text ist überholt** – es gibt inzwischen Retry-Knöpfe: `ListenAusfall` (`components/ListenAusfall.tsx:36`, 5 Einsätze), `ExerciseEditModal.tsx:170`, `PlanPositions.tsx:549`, `VaterWizard.tsx:735`. Die in B-172 befürchtete Inkonsistenz (wenige Banner mit Knopf, die Mehrheit ohne) ist damit real. Neu gezählt: 81× `banner err`, 55× `.reload()`. Frage: wird `ListenAusfall` Hausregel für jeden Lesefehler?
