---
tags: [typ/story, status/ausformuliert, bereich/backend, bereich/tests]
aliases: [Suite flackert, AtomaresSchreiben_KeineLeseFehler, Dateirennen unter Volllast]
status: ausformuliert
prio: P1
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: beim Bauen von B-163 beobachtet (docs/backlog/B-163-art-und-typ-tragen-dieselben-woerter.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-165 · `AtomaresSchreiben_KeineLeseFehler` fällt unter Volllast — und färbt das Test-Tor rot

## Beobachtung

Beim Verifizieren von B-163 lief `dotnet test Pugling.sln -c Release` dreimal hintereinander auf
**demselben** Arbeitsstand:

| Lauf | Ergebnis |
|---|---|
| 1 | `Failed: 1, Passed: 827, Total: 828` |
| 2 | `Failed: 0, Passed: 828` |
| 3 | `Failed: 0, Passed: 828` |

Der Name fehlte zunächst: der erste Aufruf schnitt die Ausgabe auf die letzten drei Zeilen ab, und darin
stand nur ein Stapel-Fragment (`at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs`).

## Der Name — gefunden, nicht geraten

Die **Nachschau zu B-157** am selben Tag hat ihn unabhängig eingefangen, mit Zahl:

> `dotnet test Pugling.sln -c Release` → 827 grün, **1 rot:
> `OpenApiExampleCatalogConcurrencyTests.AtomaresSchreiben_KeineLeseFehler`**
> (`backend/Pugling.Api.Tests/OpenApiExampleCatalogConcurrencyTests.cs:231`, erwartet 0 Lesefehler,
> gemessen 1). Einzeln nachgefahren (`--filter ~OpenApiExampleCatalogConcurrencyTests`) → **grün.**

Das passt an einer Stelle zur ersten Beobachtung, die keine Vermutung ist: der Test ist `public void` —
**synchron** —, und `MethodBaseInvoker.InvokeWithNoArgs` ist der Aufrufweg einer parameterlosen synchronen
Methode. Die meisten Fälle dieser Suite sind `async Task`. Zwei unabhängige Beobachtungen, dasselbe Bild;
`unverifiziert` ist damit weg.

## Warum das mehr wiegt als ein flackernder E2E

Das **Test-Tor** (`.claude/hooks/test-gate.sh`, Stop-Hook) und die CI hängen an dieser Suite. Ein Test,
der einen von drei Läufen ohne Zutun fällt, blockt also gelegentlich eine **korrekte** Änderung — und
weil ein rotes Tor per Konvention „die verletzte Regel und den Fundort selbst benennt", kostet jeder
solche Fehlalarm eine Fehlersuche an einer Stelle, an der nichts kaputt ist. Das ist die teuerste Sorte
Rauschen: sie untergräbt das Vertrauen in das Tor, das die Disziplin ersetzen sollte.

`B-153` (flackernde `bilder.spec.ts`) ist **nicht** derselbe Befund: das ist ein Playwright-E2E, dieser
hier ein Integrationstest.

## Was der Test tut, und warum die Volllast ihn kippt

Er belegt das Rennen, das B-57 behoben hat: Ein Leser-Thread pollt eine Wegwerf-Datei, während ein Schreiber
sie mitten im Schreiben pausiert; `AtomaresSchreiben_KeineLeseFehler` fordert **0** Lesefehler, und zwar
**zehn Mal hintereinander** („Several independent repetitions, not one lucky pass").

Die Klasse ist an dieser Empfindlichkeit ausdrücklich gebaut und **kennt die Umgebung schon**: ihr
Klassen-Kommentar beschreibt einen „real-time-antivirus-driven exclusive-access blip on **any** fresh write
or rename" auf genau dieser Windows-Maschine und begründet damit, warum das Schreiben von Hand pausiert
statt gegen echte Plattengeschwindigkeit gefahren wird. `CountFailedReadsDuring` nimmt zusätzlich einen
**eigenen** `Thread` statt `Task.Run`, mit dieser Begründung im Code:

> when the full suite runs hundreds of test classes in parallel (xunit.v3 parallelizes collections by
> default, and this project has no `[Collection]` grouping), the ThreadPool queue can be under enough real
> pressure that a queued work item sits waiting well past a 300ms pause

Die Vorkehrungen greifen also gegen ThreadPool-Aushungern — der beobachtete Fehlschlag ist ein **Lesefehler**,
nicht ein ausgehungerter Leser. Was die Volllast zusätzlich verbreitert, ist damit noch nicht belegt und
gehört gemessen, nicht behauptet.

## Offene Punkte

1. **Was genau schlägt fehl?** Empfehlung: den Zähler durch die *Ausnahme* ersetzen, die `TryRead`
   verschluckt, und sie in die Meldung nehmen. Ein `IOException` (Sharing Violation) und ein
   `JsonException` (zerrissener Inhalt) sind zwei völlig verschiedene Befunde — der erste ist ein
   Umgebungsartefakt, der zweite wäre die Rückkehr des Fehlers, den B-57 behoben hat. Heute zieht
   `failures` beide zusammen: **genau die Fehlerfamilie dieses Repos, hier im Messinstrument selbst.**
2. **Zehn Wiederholungen — sind sie das wert?** Empfehlung: erst Punkt 1, dann entscheiden. Zehn
   Versuche multiplizieren die Wahrscheinlichkeit eines Umgebungs-Blips mit zehn; wenn der Fehler ein
   `IOException` ist, ist die Wiederholungszahl die Ursache des Flackerns und nicht der Beweis der Güte.
3. **Ein `[Collection]` für diese Klasse?** Empfehlung: **nicht als Erstes.** Es würde die Suite
   verlangsamen und die Ursache verdecken; nach Punkt 1 ist vielleicht klar, dass es gar nicht die
   Parallelität ist.
4. **Kein automatischer Wiederholungslauf im Test-Tor.** Empfehlung: **nein**, wie schon zuvor notiert —
   das macht aus einem sichtbaren Flackern ein unsichtbares.

## Testweg

Kein neuer Test, sondern eine **schärfere Meldung** im bestehenden. Die Probe dafür ist ungewöhnlich, aber
machbar: eine Datei mit halb geschriebenem Inhalt und eine mit exklusiver Sperre erzeugen und belegen, dass
die Meldung die beiden Fälle **verschieden** benennt. `UnsicheresSchreiben_ErzeugtLeseFehler` (der
Nachbar-Fall, `:206`) liefert den ersten davon schon frei Haus.

## Verlauf

- 2026-08-13 · Aufgenommen. Beim Verifizieren von B-163 beobachtet: derselbe Stand, drei Läufe, ein Rot.
  Name nicht mitgeschnitten, weil der Aufruf die Ausgabe abschnitt — das Einfangen per `.trx` war darum
  als erster Schritt notiert, nicht die Ursachenanalyse.
- 2026-08-13 · `idee → ausformuliert`, ohne dass der `.trx`-Schritt nötig war: Die **Nachschau zu B-157**
  hat denselben Fehlschlag am selben Tag unabhängig erwischt und den Namen mitgeliefert
  (`AtomaresSchreiben_KeineLeseFehler`, im Einzellauf grün). Die synchrone Signatur des Tests passt zum
  Stapel-Fragment der ersten Beobachtung. Dabei verschoben: Der interessante Fund ist **nicht** die
  Parallelität, sondern dass das Messinstrument selbst „Sperre" und „zerrissener Inhalt" in einem Zähler
  zusammenzieht — und damit nicht sagen kann, ob hier ein Umgebungsartefakt flackert oder der von B-57
  behobene Fehler zurück ist.
