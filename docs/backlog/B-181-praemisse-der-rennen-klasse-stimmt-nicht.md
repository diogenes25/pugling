---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/tests]
aliases: [Fehlermodus haengt an der Plattform, FileShare.Read sperrt den Leser nur unter Windows aus, Praemisse von B-57]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: beim Bauen von B-165 gemessen (Nachtlauf 2026-08-14, Sprint 3)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
nachgeschaut: 2026-10-23
---

# B-181 · Die Rennen-Klasse nennt nur einen von zwei Fehlermodi — welcher auftritt, entscheidet die Plattform

## User Story

Als **Entwickler, der die Rennen-Klasse liest**, möchte ich, dass ihre Begründung beide Fehlermodi nennt,
die ein Leser je nach Plattform sieht, damit ich einen Leser nicht gegen den falschen wappne.

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

## Akzeptanzkriterien

1. Der Klassen-Kommentar von `OpenApiExampleCatalogConcurrencyTests` nennt beide Modi (gesperrt unter
   Windows, zerrissen unter Linux) und dass die Plattform entscheidet.
2. Ob `OpenApiExampleCatalog.Load` einen Wiederholungsversuch bekommt, steht als nummerierte Entscheidung
   fest – mit Begründung und Kosten (Punkt 2; Verhaltensänderung im Produktionspfad, beim Menschen).
3. Wird der Wiederholungsversuch gebaut, hält ein Test ihn für **beide** Ausnahmetypen fest.

## Entscheidungen

1. **Wiederholungsversuch für beide Ausnahmetypen** – Entscheidung des Nutzers (G-8, Dialog-Sitzung): wenige Versuche, kurze
   Pause, bei dauerhaftem Scheitern den letzten Fehler mit Diagnose werfen. Gebaut: `ReadAttempts = 5`, 50 ms Pause,
   `InvalidOperationException` mit Pfad, Versuchszahl und Ursache.
2. **Zusätzlich `UnauthorizedAccessException`** (autonom, `art: Defekt`, auf Reviewer-Fund): Windows meldet sie für eine Datei im
   „delete pending"-Moment des atomaren Ersetzens – genau das Fenster, das der Schreiber in `DocsCaptureTests` auf seiner Seite
   wiederholt. *Kosten:* ein dritter Typ im Filter; andere Ausnahmen (Programmierfehler) schlagen weiter sofort durch (Test).
3. **Öffentliche Naht `LoadWithRetry(path, open, attempts, pause)`** statt Zeitrennen im Test – `Pugling.Api` hat kein
   `InternalsVisibleTo`. *Kosten:* eine öffentliche Methode, die nur Tests anders aufrufen.

## Schätzung

- **groesse:** S · **wo:** backend · **migration:** nein · **vertragsbruch:** nein. **Testweg:** `OpenApiExampleCatalogRetryTests` (5 Fälle) mit Mutanten.

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
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)): **Offener Punkt 1 ist durch B-57 selbst beantwortet** – dort wurden auf Windows beide Modi beobachtet („954 `IOException` und 419 `JsonReaderException`“, B-57 `:60,:78`). Die Erzählung von B-57 stimmt also; falsch ist nur, dass der Klassen-Kommentar (`OpenApiExampleCatalogConcurrencyTests.cs:17-20`) einen Modus weglässt. Damit zerfällt die Story: (a) Doku-Korrektur XS, nachtlauffähig; (b) Wiederholungsversuch in `OpenApiExampleCatalog.Load` (`:36-37`) auch bei `JsonException` – Verhaltensänderung im Produktionspfad, beim Menschen.
- **2026-10-23** — **Entscheidung des Nutzers** in der Grill-Runde der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)) (G-8) zu offenem Punkt 2: `OpenApiExampleCatalog.Load` bekommt einen **begrenzten Wiederholungsversuch für beide Ausnahmetypen** (`IOException` und `JsonException`): wenige Versuche mit kurzer Pause, und bei dauerhaftem Scheitern wird der letzte Fehler mit Diagnose geworfen, nicht verschluckt. Ein Test hält beide Typen fest (AK 3). Damit ist die Story als `Defekt` vollständig autonom grill- und baubar (Paket NP-12).
- **2026-10-23** — vierter Nachtlauf, Sprint 9 (NP-12, [Protokoll](../pm-sitzung-2026-10-23-nacht.md)): gegrillt (Entscheidung des Nutzers aus G-8), gebaut. AK 1: Klassen-Kommentar von `OpenApiExampleCatalogConcurrencyTests` nennt beide Modi, den Retry und dass er eine Leseversuch-Nachbildung ist. Rote Proben (Auflage 5, Mutanten): nur `IOException` → rot `Zerrissener_Inhalt…` (`:43`) und `Dauerhaft…`; nur `JsonException` → rot `Gesperrte_Datei…` (`:36`); Scheitern verschluckt → `:55`; Ursache fehlt → `:59`; ein Versuch zu viel → `:58`; ohne `UnauthorizedAccessException` → `:69`; jede Ausnahme wiederholt → `:80`. `pugling-reviewer`: zwei gezählte Funde (der dritte Ausnahmetyp, der Fall „Programmierfehler schlägt durch") behoben; ein Kommentar-Nit („unbounded" hängt statt rot) berichtigt. Delta der Behebung: **Selbst-Check**, ausdrücklich so benannt (Mutanten oben), kein zweiter Reviewer-Lauf – der Sprint-Zähler stand bei 5. Backend **900/900**. Rollengang: `GET /openapi/v1.json` trägt Beispiele an 87 Operationen (Ladepfad live). Abgenommen; Commit: `90387d5`.
- **2026-10-23** — Nachschau zu Sprint 9 (fünfter Nachtlauf, [Protokoll](../pm-sitzung-2026-10-24-nacht.md)), frischer Prüfer, Prämisse selbst nachgelesen: Ausnahmefilter, Grenze und Meldung in `LoadWithRetry` halten, jeder der fünf Fälle fällt unter einer benannten Mutante. **Eine Entgleitung → [B-261](B-261-produktionspfad-des-beispielkatalogs-ungetestet.md)**: der Produktionspfad `OpenApiExampleCatalog.Load` (`:33-40`, Aufruf `Program.cs:290`) und die Pause hängen an keinem Test – fiele `Load` auf den Direktlesepfad zurück, blieben alle fünf Fälle grün. Dazu falsch: vier von sieben Mutanten-Zeilen im Eintrag oben (`:43/:55/:58/:59` statt `:44/:56/:59/:60` in `git show 90387d5:`), die Mutanten liefen auf einer früheren Fassung.
