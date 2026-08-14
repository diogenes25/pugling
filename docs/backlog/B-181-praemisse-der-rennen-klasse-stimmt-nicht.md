---
tags: [typ/story, status/ausformuliert, bereich/backend, bereich/tests]
aliases: [Fehlermodus haengt an der Plattform, FileShare.Read sperrt den Leser nur unter Windows aus, Praemisse von B-57]
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

# B-181 · Die Rennen-Klasse nennt nur einen von zwei Fehlermodi — welcher auftritt, entscheidet die Plattform

## Ist-Stand (gemessen, auf zwei Plattformen — der Fehlermodus hängt an ihr)

`backend/Pugling.Api.Tests/OpenApiExampleCatalogConcurrencyTests.cs` beschreibt in ihrem Klassen-Kommentar
den Fehler, den B-57 behoben hat, als **„torn/incomplete JSON content mid-write"** und die geprüfte
Eigenschaft als „can a reader ever observe a partial write of the final path".

**Welchen der beiden Fehlermodi ein Leser sieht, entscheidet die Plattform** — und die ursprüngliche
Aufnahme dieser Story hat das übersehen, weil beide ihrer Proben auf Windows liefen:

| Messung | Plattform | Ergebnis |
|---|---|---|
| `UnsicheresSchreiben_ErzeugtLeseFehler` nach dem Trennen der Zähler | Windows | **0 zerrissen, 1867 gesperrt** |
| eigenständige Probe (`FileStream(Create, Write, FileShare.Read)` gegen `File.OpenRead` im Dauerlauf) | Windows | **2351 Ausnahmen, alle identisch:** `IOException: … because it is being used by another process` |
| derselbe Testfall, CI-Lauf [31810420330](https://github.com/diogenes25/pugling/actions/runs/31810420330) | Ubuntu 24.04 | **126 zerrissen, 0 gesperrt** |

**Der Mechanismus auf Windows:** `File.OpenRead` fordert `FileShare.Read` — „andere dürfen lesen, **nicht**
schreiben". Ein bereits offenes **Schreib**-Handle widerspricht dieser Forderung, und Windows prüft beide
Richtungen. Das Öffnen wird verweigert, bevor der Leser ein einziges Byte sieht.

**Unter Linux greift diese Prüfung nicht** — der Leser kommt herein und deserialisiert eine halb
geschriebene Datei, also genau den Zustand, den der Klassen-Kommentar beschreibt. Damit ist die ursprüngliche
Kernaussage dieser Story („ein Leser dieses Repos kann zerrissenen Inhalt nicht beobachten") **auf der
Plattform falsch, auf der die Suite in CI tatsächlich läuft**. Warum die Freigabemodi dort nicht gleich
durchgesetzt werden, ist **nicht** nachgemessen, nur das Ergebnis ist es.

Der Produktionsleser ist derselbe: `OpenApiExampleCatalog.cs:36` benutzt `File.OpenRead`.

## Die echte Lücke

Nicht die Tests — die messen nach B-165 das Richtige und sind an beiden Enden rot probierbar. Die Lücke ist
die **Begründung**, an drei Stellen: der Klassen-Kommentar, `TryRead`s Beschreibung und die Erzählung von
B-57. Keine von ihnen sagt, dass der beschriebene Fehlermodus von der Plattform abhängt — sie benennen
**einen** und lassen den anderen weg. Wer daraus schließt, ein Leser müsse nur gegen `IOException` gewappnet
sein (oder nur gegen `JsonException`), wappnet sich je nach Laufumgebung gegen das Falsche;
`OpenApiExampleCatalog.Load` hat gegen **keins** von beidem einen Wiederholungsversuch.

Das ist genau die Fehlerfamilie dieses Repos, angewandt auf eine Begründung statt auf eine Bedingung: der
Text zieht „das Lesen scheitert" und „der Inhalt ist kaputt" zusammen.

**Zwei Kommentare sind am 2026-08-14 bereits korrigiert** (Zusicherung plattformfest gemacht, siehe Verlauf):
`TryRead`s Enum-Beschreibung und die Zwei-Zusicherungen-Begründung in `AtomaresSchreiben_KeineLeseFehler`.
Offen bleiben der Klassen-Kommentar und die Erzählung von B-57.

## Offene Punkte

1. **Stimmt B-57s Erzählung insgesamt nicht, oder nur ihr Wortlaut?** Empfehlung: erst die Story von B-57
   lesen und prüfen, ob der damals beobachtete Fehler ein `IOException` oder ein `JsonException` war — und
   **auf welcher Plattform** er beobachtet wurde, was seit der Linux-Messung die eigentlich entscheidende
   Frage ist. War es ein `IOException` auf Windows, ist nur die Beschreibung unvollständig und der Fix
   richtig. **Nicht raten:** der Unterschied entscheidet, ob hier eine Doku-Korrektur oder eine
   Verhaltensfrage vorliegt.
2. **Braucht `OpenApiExampleCatalog.Load` einen Wiederholungsversuch?** Empfehlung: **wahrscheinlich ja, aber
   erst nach Punkt 1** — und **gegen beide Ausnahmetypen**. Das ist die Verschärfung aus der Linux-Messung:
   ein Retry, der nur `IOException` fängt, wäre unter Linux das falsche Netz, denn dort scheitert der Leser
   an `JsonException`. Ein Retry auf `JsonException` ist allerdings nicht harmlos — er würde eine dauerhaft
   kaputte Katalogdatei genauso behandeln wie eine kurzlebig halb geschriebene und die Diagnose verschlucken.
   Das Repo hat für das Muster zwei Vorbilder (`OpenForWriteWithRetry`, `MoveWithRetry`), beide nur für
   `IOException`. Kosten: eine Wiederholung im Produktionspfad, wo bisher keine steht — bewusste
   Entscheidung, keine Selbstverständlichkeit.
3. **Soll der Test zerrissenen Inhalt überhaupt beobachtbar machen?** (Schreiber mit `FileShare.ReadWrite`.)
   Empfehlung: **nein — und die Frage hat sich weitgehend erledigt.** Unter Linux ist der Modus ohne jeden
   Eingriff beobachtbar, dort misst der `Zerrissen`-Zähler ihn also echt. Unter Windows bliebe er die
   Zusicherung, dass der Modus **nicht** auftritt. Einen Fehlermodus künstlich herstellen, den das Produkt
   dort nicht hat, nur damit ein Test ihn zeigt, lohnt in beiden Fällen nicht.

## Verlauf

- 2026-08-14 · Aufgenommen beim Bauen von B-165 im Nachtlauf, direkt auf `ausformuliert`: Der Ist-Stand ist
  mit zwei Messungen belegt (der getrennte Zähler im Test selbst und eine eigenständige Probe), und der
  Mechanismus ist am Code nachvollzogen. **Nicht mitgebaut**, weil hier eine Verhaltensfrage im
  Produktionspfad dranhängt (Punkt 2) — die entscheidet kein unbeaufsichtigter Lauf.
- 2026-08-14 · **Die Prämisse dieser Story fiel selbst** — durch den ersten CI-Lauf, der B-165 sah
  ([31810420330](https://github.com/diogenes25/pugling/actions/runs/31810420330), rot). Unter Ubuntu misst
  derselbe Testfall **126 zerrissen, 0 gesperrt**, also das Gegenteil der beiden Windows-Proben. Beide
  Aufnahme-Messungen liefen auf Windows; der Fehlermodus hängt an der Plattform. Sofort behoben, weil `main`
  daran rot stand: die Zusicherung prüft jetzt `gesperrt + zerrissen > 0` (beides ist derselbe Defekt aus
  Sicht des Aufrufers) und nennt die Aufteilung in der Meldung weiter — B-165s Gewinn bleibt, die Bindung an
  eine Plattform fällt. Rote Probe belegt: mit dem atomaren Schreiber wird der Fall rot (`0/0/422`).
  Mitkorrigiert wurden die zwei Kommentare, die die widerlegte Behauptung wörtlich trugen. **Offen bleibt
  die Story** — Punkt 1 und 2 sind unbeantwortet, Punkt 2 jetzt schärfer.
