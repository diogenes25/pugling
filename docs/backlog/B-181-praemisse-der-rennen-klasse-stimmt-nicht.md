---
tags: [typ/story, status/ausformuliert, bereich/backend, bereich/tests]
aliases: [torn content ist nicht beobachtbar, FileShare.Read sperrt den Leser aus, Praemisse von B-57]
status: ausformuliert
prio: P2
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: beim Bauen von B-165 gemessen (Nachtlauf 2026-08-14, Sprint 3)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
nachgeschaut: ""
---

# B-181 · Die Rennen-Klasse behauptet einen Fehler, den kein Leser dieses Repos sehen kann

## Ist-Stand (gemessen, mit zwei unabhängigen Proben)

`backend/Pugling.Api.Tests/OpenApiExampleCatalogConcurrencyTests.cs` beschreibt in ihrem Klassen-Kommentar
den Fehler, den B-57 behoben hat, als **„torn/incomplete JSON content mid-write"** und die geprüfte
Eigenschaft als „can a reader ever observe a partial write of the final path".

**Ein Leser dieses Repos kann einen solchen Zustand nicht beobachten.** Gemessen beim Bauen von
[B-165](B-165-backend-suite-flackert.md):

| Messung | Ergebnis |
|---|---|
| `UnsicheresSchreiben_ErzeugtLeseFehler` nach dem Trennen der Zähler | **0 zerrissen, 1867 gesperrt** |
| eigenständige Probe (`FileStream(Create, Write, FileShare.Read)` gegen `File.OpenRead` im Dauerlauf) | **2351 Ausnahmen, alle identisch:** `IOException: … because it is being used by another process` |

**Der Mechanismus:** `File.OpenRead` fordert `FileShare.Read` — „andere dürfen lesen, **nicht** schreiben".
Ein bereits offenes **Schreib**-Handle widerspricht dieser Forderung, und Windows prüft beide Richtungen.
Das Öffnen wird verweigert, bevor der Leser ein einziges Byte sieht. Zerrissenen Inhalt bekäme er nur zu
Gesicht, wenn der Schreiber Schreibzugriff **teilte** (`FileShare.ReadWrite`) — und das tut weder die
Vor-Fix-Form (`File.WriteAllText`, `DocsCaptureTests.cs:1151`) noch der Fix
(`WriteAllText` in eine Temp-Datei + `MoveWithRetry`, `:1170-1171`).

Der Produktionsleser ist derselbe: `OpenApiExampleCatalog.cs:36` benutzt `File.OpenRead`.

## Die echte Lücke

Nicht die Tests — die messen nach B-165 das Richtige und sind an beiden Enden rot probierbar. Die Lücke ist
die **Begründung**, an drei Stellen: der Klassen-Kommentar, `TryRead`s Beschreibung und die Erzählung von
B-57. Sie benennen einen Fehlermodus („halb geschriebener Inhalt"), der auf dieser Plattform mit diesen
Freigabemodi nicht auftreten kann. Wer das liest und daraus schließt, ein Leser müsse gegen `JsonException`
gewappnet sein, wappnet sich gegen das Falsche — der reale Fehlermodus ist `IOException`, und
`OpenApiExampleCatalog.Load` hat dagegen **keinen** Wiederholungsversuch.

Das ist genau die Fehlerfamilie dieses Repos, angewandt auf eine Begründung statt auf eine Bedingung: der
Text zieht „das Lesen scheitert" und „der Inhalt ist kaputt" zusammen.

## Offene Punkte

1. **Stimmt B-57s Erzählung insgesamt nicht, oder nur ihr Wortlaut?** Empfehlung: erst die Story von B-57
   lesen und prüfen, ob der damals beobachtete Fehler ein `IOException` oder ein `JsonException` war. Wenn er
   ein `IOException` war, ist nur die Beschreibung falsch und der Fix richtig — was nach dem heutigen Wissen
   das Wahrscheinlichste ist. **Nicht raten:** der Unterschied entscheidet, ob hier eine Doku-Korrektur oder
   eine Verhaltensfrage vorliegt.
2. **Braucht `OpenApiExampleCatalog.Load` einen Wiederholungsversuch?** Empfehlung: **wahrscheinlich ja, aber
   erst nach Punkt 1.** Der reale Fehlermodus ist eine kurzlebige Sperre; ein Leser ohne Retry scheitert
   daran, und das Repo hat für genau dieses Muster schon zwei Vorbilder (`OpenForWriteWithRetry`,
   `MoveWithRetry`). Kosten: eine Wiederholung im Produktionspfad, wo bisher keine steht — das ist eine
   bewusste Entscheidung, keine Selbstverständlichkeit.
3. **Soll der Test zerrissenen Inhalt überhaupt beobachtbar machen?** (Schreiber mit `FileShare.ReadWrite`.)
   Empfehlung: **nein.** Das würde einen Fehlermodus herstellen, den das Produkt nicht hat, nur damit ein
   Test ihn zeigen kann. Der `Zerrissen`-Zähler bleibt trotzdem sinnvoll: er ist die Zusicherung, dass dieser
   Modus **nicht** auftritt.

## Verlauf

- 2026-08-14 · Aufgenommen beim Bauen von B-165 im Nachtlauf, direkt auf `ausformuliert`: Der Ist-Stand ist
  mit zwei Messungen belegt (der getrennte Zähler im Test selbst und eine eigenständige Probe), und der
  Mechanismus ist am Code nachvollzogen. **Nicht mitgebaut**, weil hier eine Verhaltensfrage im
  Produktionspfad dranhängt (Punkt 2) — die entscheidet kein unbeaufsichtigter Lauf.
