---
tags: [typ/story, status/idee, bereich/frontend]
aliases: [Lade… bleibt nach Fehler stehen, data === null ohne loading, Platzhalter widerspricht Banner]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: B-172, Fund des frontend-reviewer
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-184 · Vier Platzhalter zeigen einen Fehler dauerhaft als „Lade…"

`frontend/CLAUDE.md` nennt die Falle in einer Richtung: wer `{loading ? "Lade…" : rows}` schreibt, hängt bei
jedem `reload` alle Zeilen aus. Die Antwort darauf ist `loading && data === null` — und **37 von 41**
Platzhaltern im Vater-Web halten sich daran. Vier tun es nicht und prüfen allein `data === null`:

| Datei:Zeile | Was dauerhaft „Lade…" zeigt |
| --- | --- |
| `ChildMaterialSection.tsx:54` | die Bücherliste des Kindes |
| `ChildMaterialSection.tsx:114-116` | das Fachlehrer-Treffer-Panel |
| `VaterKind.tsx:94` | der Interessen-Editor |
| `VaterLehrwerke.tsx:461` | die Unit-Liste einer Reihe |

Das ist an allen vier Stellen eine **bewusste** Wahl mit Begründung im Kommentar („Auf `data` prüfen, nicht
auf `loading` – nach dem Speichern lädt neu, und ein Platzhalter nähme das Formular für einen Moment aus dem
DOM; es verlöre seinen Zustand samt der Bestätigung"). Unbedacht ist nur die Nebenwirkung: scheitert der
Abruf, wird `loading` falsch, `data` bleibt aber `null` — und der Platzhalter steht für immer.

Dass daneben das Fehlerbanner steht, macht es nicht besser, sondern widersprüchlich: die Seite sagt
gleichzeitig „das ging schief" und „ich lade noch". Aufgefallen ist es dem `frontend-reviewer` beim Review
von [B-172](B-172-fachauswahl-loescht-wenn-ihre-optionen-fehlen.md), weil dort für die Units genau dieser
Fall gerade behoben wurde — mit `units.error ? [] : units.data ?? []` an der Optionsliste.

Zu klären beim Ausformulieren: ob ein dritter Zustand die richtige Antwort ist (`data === null && !error`)
oder ob die vier Stellen ihre Begründung überhaupt noch brauchen — zwei von ihnen tragen inzwischen einen
`useRef`-Bezugspunkt, der das Argument „verliert seinen Zustand" möglicherweise erledigt hat.

## Verlauf

- 2026-09-01 · Aufgenommen aus dem `frontend-reviewer`-Review zu B-172. Die Zählung (37 von 41 richtig,
  vier nicht) ist an diesem Tag gemessen; nicht geprüft ist, ob die Begründung der vier Stellen noch
  gilt — darum `unverifiziert`.
