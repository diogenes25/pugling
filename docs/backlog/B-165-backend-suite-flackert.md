---
tags: [typ/story, status/idee, bereich/backend, bereich/tests]
aliases: [Suite flackert, ein Lauf rot zwei gruen]
status: idee
prio: P1
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: beim Bauen von B-163 beobachtet (docs/backlog/B-163-art-und-typ-tragen-dieselben-woerter.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-165 · Die Backend-Suite flackert – ein Lauf rot, zwei grün, Name unbekannt

## Beobachtung

Beim Verifizieren von B-163 lief `dotnet test Pugling.sln -c Release` dreimal hintereinander auf
**demselben** Arbeitsstand:

| Lauf | Ergebnis |
|---|---|
| 1 | `Failed: 1, Passed: 827, Total: 828` |
| 2 | `Failed: 0, Passed: 828` |
| 3 | `Failed: 0, Passed: 828` |

Der Name des gefallenen Tests ist **nicht bekannt**: der erste Aufruf schnitt die Ausgabe auf die
letzten drei Zeilen ab, und darin stand nur ein Stapel-Fragment
(`at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs`). Damit ist die Beobachtung echt, ihre
Identität aber offen — genau darum steht diese Story auf `idee` mit `unverifiziert: true`.

## Warum das mehr wiegt als ein flackernder E2E

Das **Test-Tor** (`.claude/hooks/test-gate.sh`, Stop-Hook) und die CI hängen an dieser Suite. Ein Test,
der einen von drei Läufen ohne Zutun fällt, blockt also gelegentlich eine **korrekte** Änderung — und
weil ein rotes Tor per Konvention „die verletzte Regel und den Fundort selbst benennt", kostet jeder
solche Fehlalarm eine Fehlersuche an einer Stelle, an der nichts kaputt ist. Das ist die teuerste Sorte
Rauschen: sie untergräbt das Vertrauen in das Tor, das die Disziplin ersetzen sollte.

`B-153` (flackernde `bilder.spec.ts`) ist **nicht** derselbe Befund: das ist ein Playwright-E2E, dieser
hier ein Integrationstest.

## Erster Schritt (das ist die Arbeit, nicht die Analyse)

Den Namen **einfangen**, bevor irgendetwas vermutet wird:

```bash
dotnet test Pugling.sln -c Release --logger "trx;LogFileName=flake.trx"
```

Die `.trx` hält das Ergebnis je Test samt Meldung, überlebt den Lauf und ist unabhängig davon, wie die
Konsolenausgabe abgeschnitten wird. Mehrere Läufe in einer Schleife, bis einer rot ist.

Erst dann lohnt eine Ursachenfrage. Die naheliegenden Verdächtigen sind **nicht** zu raten, sondern an
der Meldung abzulesen; als Suchraum notiert, nicht als Behauptung:

- ein Test, der auf `DateTime.UtcNow` rechnet und über eine Tagesgrenze läuft (`CLAUDE.md`, „Zeit/UTC");
- ein Test, der eine in `xUnit` parallel laufende Nachbarklasse an geteiltem Zustand trifft;
- ein Test, der auf einer Reihenfolge sitzt, die die DB nicht zusichert (SQLite gibt ohne `OrderBy`
  keine stabile Sortierung).

## Offene Punkte

1. Welcher Test ist es? — **muss** eingefangen werden, siehe oben.
2. Ist er länger flackernd oder erst seit einer der Änderungen dieser Woche? Empfehlung: nach dem
   Einfangen `git log -- <Testdatei>` und den Lauf auf einem älteren Commit wiederholen; solange der
   Name fehlt, ist die Frage nicht beantwortbar.
3. Braucht das Test-Tor eine Wiederholung bei genau einem roten Test? Empfehlung: **nein** — ein
   automatischer Wiederholungslauf macht aus einem sichtbaren Flackern ein unsichtbares. Erst die
   Ursache, dann die Frage.

## Verlauf

- 2026-08-13 · Aufgenommen. Beim Verifizieren von B-163 beobachtet: derselbe Stand, drei Läufe,
  ein Rot. Name nicht mitgeschnitten — das Einfangen per `.trx` ist der erste Schritt, nicht die
  Ursachenanalyse.
