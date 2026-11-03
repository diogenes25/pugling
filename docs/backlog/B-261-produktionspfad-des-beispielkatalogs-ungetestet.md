---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/test]
aliases: [Load ohne Test, Beispielkatalog-Retry nur in der Hilfsmethode belegt]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Sprint 9 (fünfter Nachtlauf, docs/pm-sitzung-2026-10-24-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-181]
wartet_auf: ""
nachgeschaut: "2026-11-03"
---

# B-261 · Der Produktionspfad des Beispielkatalogs hängt an keinem Test

B-181 hat einen begrenzten Wiederholungsversuch gebaut und mit fünf Fällen belegt – alle gegen
`OpenApiExampleCatalog.LoadWithRetry`. Der Pfad, den die App wirklich nimmt, ist
`OpenApiExampleCatalog.Load` (`backend/Pugling.Api/OpenApi/OpenApiExampleCatalog.cs:33-40`, aufgerufen in
`Program.cs:290`). Er prüft kein Test: fiele `Load` auf den früheren Direktlesepfad zurück
(`File.OpenRead` + `Deserialize`) oder riefe es `LoadWithRetry` mit einem Versuch auf, blieben alle fünf
Fälle grün. Auch die Pause (`Thread.Sleep(pause)`) prüft kein Fall; alle laufen mit `TimeSpan.Zero`. AK 3
von B-181 („ein Test hält den Retry für beide Typen fest“) gilt damit nur für die Hilfsmethode.

Dazu eine falsche Doku-Behauptung im Verlauf von B-181: vier von sieben Mutanten-Zeilen sind um eine Zeile
verschoben (`:43/:55/:58/:59` statt `:44/:56/:59/:60` in `git show 90387d5:`) – die Mutanten liefen auf einer
früheren Fassung der Testdatei.

Vorschlag für die rote Probe: ein Test, der über `Load` (oder eine dünne Naht wie eine `internal`-Überladung mit
Öffner) einen gesperrten bzw. zerrissenen ersten Versuch einspielt; rot, wenn `Load` nicht wiederholt.
**Nicht in diesem Lauf behoben:** der Fund liegt im Increment von Sprint 9 (vierter Lauf), nicht im Increment des
fünften Laufs; der Auftrag nimmt Nachschau-Defekte früherer Läufe ausdrücklich aus.

## User Story

Als **Entwickler** möchte ich, dass der Pfad, den die App beim Start wirklich nimmt (`OpenApiExampleCatalog.Load` mit dem
Inhaltspfad), samt Wiederholung und Pause von einem Test gehalten wird, damit ein Rückbau nicht nur an der Hilfsmethode
auffällt.

## Ist-Stand (Lauf O, am Code belegt, Stand `cc19020`)

- `Load(string)` (`OpenApi/OpenApiExampleCatalog.cs:33-40`) sucht die Datei und ruft `LoadWithRetry(path, File.OpenRead,
  ReadAttempts, 50 ms)`; aufgerufen nur in `Program.cs:295`. Kein Test ruft `Load`; alle fünf Retry-Fälle rufen
  `LoadWithRetry` mit `TimeSpan.Zero`.

## Die echte Lücke

Der getestete Teil ist die Hilfsmethode, nicht der Pfad, den die App geht – und die Pause, die einem Schreiber Zeit gibt,
prüft kein Fall.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`, Testlücke)

1. **Eine Methode mit optionalen Nähten** `Load(contentRoot, open = null, pause = null)` statt einer zweiten Überladung:
   die App ruft sie mit dem Inhaltspfad allein, der Test mit geskriptetem Öffner. So ist der Produktionspfad genau die
   getestete Methode. *Kosten:* die Vorgabewerte (`File.OpenRead`, 50 ms) tragen keine eigene Zusicherung; ein Fall gegen
   eine echte Sperre hätte sie gehalten, war aber zeitabhängig (Verlauf) und ist darum verworfen.
2. **Drei Fälle:** Wiederholung über den Inhaltspfad mit `ReadAttempts` (vier Sperren, fünfter Versuch gelingt); fehlender
   Katalog → `Empty`; die Pause als Untergrenze (2 × 40 ms ≥ 50 ms – Windows kann einen Takt früher wecken).

## Akzeptanzkriterien

1. Ignoriert `Load` den Öffner oder liest es mit weniger Versuchen, fällt ein Test.
2. Schläft die Wiederholung nicht mehr, fällt ein Test.
3. Fehlt die Dateiprüfung, fällt ein Test.

## Schätzung

`S`, Backend (Tests + eine Naht), keine Migration, kein Vertragsbruch. Testweg: `OpenApiExampleCatalogRetryTests`.

## Verlauf

- **2026-10-23** — angelegt in der Nachschau zu Sprint 9 (fünfter Nachtlauf); `Load`-Aufrufer selbst nachgelesen
  (`grep`: einziger Aufruf `Program.cs:290`, kein Test ruft `Load`), Zeilenzeiger selbst gegen `git show 90387d5:` gelesen.
- **2026-11-03** — Lauf O, Teil 1: ausformuliert, autonom gegrillt (Freigabe 1), gebaut. Die neuen Fälle sind vor jeder
  Mutante grün (Testlücke; das Verhalten stimmte). AK 1 → `Load_Wiederholt_Ueber_Den_Inhaltspfad` und
  `Load_Ueberwindet_Eine_Echte_Sperre`: Mutante „`ReadAttempts` → 1" **2 failed**, „öffentliches `Load` liest direkt" **1
  failed** (der Sperr-Fall); AK 2 → `Zwischen_Den_Versuchen_Wird_Gewartet`, Mutante „ohne `Thread.Sleep`" **2 failed** (dazu
  der Sperr-Fall); AK 3 → `Load_Ohne_Katalog_Liefert_Den_Leeren`, Mutante „ohne `File.Exists`" **1 failed**.
  `OpenApiExampleCatalogRetryTests` **9/9**. Zur Doku-Behauptung aus der Idee: die verschobenen Zeilenzeiger berichtigt schon die
  Nachschau-Zeile im Verlauf von B-181 (2026-10-23); nichts weiter nachzuziehen.
- **2026-11-03** — Test-Tor und Review: 🟡 der Fall „echte Sperre" war zeitabhängig – das Fenster ist real ~200 ms (Pause nach jedem Fehlversuch), die Freigabe hing am Thread-Pool; unter Volllast im Test-Tor **1 failed** (`Load_Ueberwindet_Eine_Echte_Sperre`, 252 ms). Behoben, indem die zwei Überladungen zu **einer** Methode mit optionalen Nähten werden (`Load(contentRoot, open = null, pause = null)`): der Produktionspfad ist jetzt genau die getestete Methode, der zeitabhängige Fall entfällt (zählt). *Kosten:* der Vorgabewert 50 ms trägt keine Zusicherung mehr. 🟡 die Untergrenze `>= 80 ms` ist unter Windows nicht garantiert (`Sleep` kann einen Takt früher wecken) → `>= 50 ms`, Kommentar nennt den Grund (zählt). 🟢 fünfter Versuch aus dem Speicher statt von der Platte, leerer `catch` begründet (ohne Zählung). Mutanten danach: „ohne Pause", „`ReadAttempts` → 1", „`open` ignoriert" je **1 failed**; Klasse **10/10** mit der Nebenläufigkeitsprobe, volle Suite **1048/1048**.
- **2026-11-03** — Abnahme Lauf O, Teil 1: volle Suite **1048/1048**, Build ohne Warnung, E2E **103 passed** (`exit=0`, nach dem letzten Edit; der Dev-Server lädt den Beispielkatalog über `Load`). Rollengang: der Entwickler ist die Rolle – die Proben selbst sind sein Weg, ihre Mutanten je benannt; Creator, Vater und Sohn Regressionszeugen (kein Pfad von ihnen im Diff, ihre Specs grün). **Abgenommen**, Commit siehe Protokoll ([Lauf O](../pm-sitzung-2026-11-02-nacht.md)).
- **2026-11-03** — Nachschau in Lauf P, Teil 1 (frischer `pugling-reviewer`, `git show 299d216 -- '*.cs'`, read-only): **sauber** (🟢 → [B-358](B-358-beispielkatalog-proben-restkanten.md)). Geprüft: AK 1 → Mutanten „`open` ignoriert" (rot an `calls == 0`, RetryTests `:99`), „`ReadAttempts` → 4" und „ohne Wiederholung" (rot an `:96`); AK 2 → „ohne `Thread.Sleep`" rot an `:82`, kürzeste Zeit unter Windows 2 × 31,25 ms über der 50-ms-Grenze; AK 3 → „ohne `File.Exists`" rot an `:108`; `Program.cs:295` ruft genau die getestete Methode.
