---
tags: [typ/story, status/idee, bereich/backend, bereich/test]
aliases: [Load ohne Test, Beispielkatalog-Retry nur in der Hilfsmethode belegt]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Nachschau zu Sprint 9 (fünfter Nachtlauf, docs/pm-sitzung-2026-10-24-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-181]
wartet_auf: ""
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

## Verlauf

- **2026-10-23** — angelegt in der Nachschau zu Sprint 9 (fünfter Nachtlauf); `Load`-Aufrufer selbst nachgelesen
  (`grep`: einziger Aufruf `Program.cs:290`, kein Test ruft `Load`), Zeilenzeiger selbst gegen `git show 90387d5:` gelesen.
