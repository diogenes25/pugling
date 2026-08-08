---
tags: [typ/story, status/ausformuliert, bereich/doku, bereich/qualitaet]
aliases: [Leere Story-Datei unsichtbar, awk FNR==1 öffnet keinen Datensatz]
status: ausformuliert
prio: P3
art: Defekt
quelle: Code-Review 2026-08-07 des Standes gegen `origin/main` (Fund 9)
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-131 · Eine leere Story-Datei verschwindet aus dem Index — auch aus der Mängelliste

Der Backlog-Index ist der Ort, an dem eine Story mit fehlender Eintrittsbedingung auffällt („⚠ Stufe
behauptet, Datei belegt nicht"). Seit der Umstellung auf einen einzigen awk-Durchlauf gilt das für jede
Datei — **außer für die leere**: sie erzeugt gar keinen Datensatz und fehlt damit in beiden Tabellen.
Der Wächter schweigt genau bei der Datei, die am offensichtlichsten kaputt ist.

## User Story

Als **jemand, der den Backlog über den Index liest**, möchte ich, dass eine unvollständige Story-Datei
gemeldet wird und nicht verschwindet — sonst kann ich der Vollzähligkeit des Index nicht trauen.

## Ist-Stand am Code

`.claude/scripts/backlog-index.awk:33-38`:

```awk
FNR == 1 {
  if (NR > 1) emit()
  reset()
  fname = FILENAME
}
```

`FNR == 1` ist die **einzige** Stelle, an der ein Datensatz geöffnet wird. Eine Datei mit null Bytes hat
keine erste Zeile, also feuert keine Regel, also wird weder `fname` gesetzt noch am Ende etwas emittiert.
Ein `touch docs/backlog/B-132-neue-idee.md` erzeugt eine Story, die im Index schlicht nicht existiert —
weder unter „Offen" noch unter der ⚠-Liste, die sie melden müsste.

Die frühere Fassung (`for`-Schleife in `backlog-index.sh`, vor Commit `41f21eb`) hat für jede Datei eine
Zeile ausgegeben, notfalls mit `status: ?` — sie hätte den Fall gezeigt.

## Die echte Lücke

Nicht „der awk-Umbau war falsch" — er hat den Index von 2–3 Minuten auf ~1 Sekunde gebracht, und die
Datenextraktion selbst ist korrekt. Die Lücke ist eine Verschiebung der **Fehlerform**: vorher war eine
kaputte Datei laut (eine Zeile mit Fragezeichen), jetzt ist sie still.

Reichweite: klein und real. Eine leere Datei entsteht bei einem abgebrochenen Schreibvorgang oder einem
`touch` vor dem Befüllen — selten, aber genau dann, wenn jemand mitten in der Arbeit unterbrochen wurde
und sich später auf den Index verlässt.

## Offene Punkte

1. **Wo prüfen — in awk oder in der Shell?** Empfehlung: in der Shell, vor dem awk-Aufruf. Die
   Dateiliste steht dort ohnehin bereit; awk kann über eine Datei, die es nie sieht, nichts sagen. Ein
   `[ -s "$f" ]` je Datei ist billig (kein Fork).
2. **Melden oder scheitern?** Empfehlung: melden — als Zeile in der ⚠-Tabelle mit „Datei ist leer",
   nicht als Abbruch. Der Index ist ein Wächter, der warnt; ein Abbruch nähme den übrigen 120 Stories
   ihre Auflistung wegen einer kaputten.
3. **Gilt dasselbe für eine Datei ohne Frontmatter** (Inhalt, aber kein `---`-Block)? Beim
   Ausformulieren nicht geprüft. Vermutung: die wird korrekt als „`status` fehlt" gemeldet, weil
   `FNR == 1` feuert — vor dem Bau einmal nachstellen statt annehmen.

## Akzeptanzkriterien

> Entwurf, siehe Offene Punkte.

1. Eine leere `B-nnn-*.md` unter `docs/backlog/` erscheint im Index in der ⚠-Tabelle, nicht im Nichts.
2. Die übrigen Stories werden davon unberührt weiter aufgelistet; die Zählungen stimmen.
3. Ein nachgestellter Fall belegt Punkt 1 **vor** der Änderung als rot (Abnahmeform `art: Defekt`) —
   nachstellbar mit einer Wegwerf-Datei in einem Temp-Ordner, nicht im echten Bereich.
4. Die Laufzeit des Index bleibt im Sekundenbereich.

## Verlauf

- **2026-08-07** — angelegt aus dem Code-Review des Standes gegen `origin/main`, am Skript nachgeprüft
  (`backlog-index.awk:33-38`, `FNR == 1` als einzige öffnende Regel). `entgangen_bei` bleibt **leer**:
  der awk-Umbau lief als direkter Commit (`41f21eb`) ohne eigene Story — es gibt keine abgenommene
  Story, an der er hätte vorbeikommen können. Das ist der „nicht zuordenbar"-Fall aus
  [README.md](README.md), keine Nachlässigkeit.
