---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/test]
aliases: [Beispielkatalog Restkanten, TempRoot UnauthorizedAccess]
status: abgenommen
prio: P4
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf O in Lauf P, Teil 1 (docs/pm-sitzung-2026-11-03-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-11-04"
entgangen_bei: []
wartet_auf: ""
---

# B-358 · Restkanten der Beispielkatalog-Proben (Nachschau über `299d216`)

Drei 🟢 der Nachschau über B-261/B-265/B-274, keiner eine Entgleitung:

1. **`TempRoot.Dispose` fängt nur `IOException`** (`backend/Pugling.Api.Tests/OpenApiExampleCatalogRetryTests.cs:125`). Ein
   Virenscanner kann unter Windows beim `Directory.Delete` `UnauthorizedAccessException` werfen; der Test fiele dann im
   Aufräumen. Weg: `catch (Exception e) when (e is IOException or UnauthorizedAccessException)`.
2. **Unter Linux nennt die rote Meldung nur das aktuelle Fenster** (`OpenApiExampleCatalogConcurrencyTests.cs:282`, die
   Zerrissen-Regel je Fenster). B-265 AK 2 („eine rote Probe nennt alle zehn Fenster") ist damit weiter gefasst, als der Code
   hält – die AK auf die Summen-Zusicherung einengen oder die Meldung um die bisherigen Fenster ergänzen.
3. **Der Vorgabewert der Pause trägt keine Zusicherung** (Mutante „`pause` ignoriert, immer 50 ms" bliebe grün; B-261
   Entscheidung 1 nimmt das an). Dazu eine Zeile von ~127 Zeichen (`ConcurrencyTests.cs:293`).

## User Story

Als **Entwickler**, dessen Suite unter Windows mit Virenscanner läuft, möchte ich, dass ein Aufräumschritt keinen Test rot färbt
und dass die Meldung einer roten Probe hält, was ihre Story verspricht.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Punkt 1:** Ausnahmefilter auf `IOException or UnauthorizedAccessException` (welche der beiden Windows wirft, hängt am
   Zeitpunkt). Keine rote Probe, weil sie den Aufwand nicht lohnt: der Scanner-Griff selbst ist nicht herstellbar, die Ausnahme
   schon (eine schreibgeschützte Datei im Ordner lässt `Directory.Delete` unter Windows mit `UnauthorizedAccessException`
   scheitern) – aber nur unter Windows, und der Test ließe den Ordner liegen. Benannte Lücke.
2. **Punkt 2:** die **AK einengen**, nicht die Meldung erweitern. Ein einziger zerrissener Lesevorgang ist das ganze Urteil; die
   Zahlen früherer Fenster sagen darüber nichts. Ein Satz am Code sagt das, eine Zeile im Verlauf von B-265.
3. **Punkt 3:** bleibt angenommen (B-261, Entscheidung 1); nur die lange Zeile wird umbrochen.

## Akzeptanzkriterien

1. `TempRoot.Dispose` schluckt `UnauthorizedAccessException` wie `IOException`, sonst nichts.
2. Am Code und in B-265 steht, dass die Zerrissen-Regel nur ihr eigenes Fenster nennt.

## Schätzung

`XS`. Testweg: volle Suite; keine Verhaltensänderung einer Zusicherung.

## Verlauf

- **2026-11-03** — angelegt aus der Nachschau zu Lauf O (`pugling-reviewer` über `299d216`, 🟢 ×3). Aus Code gelesen.
- **2026-11-04** — Lauf S, Teil 2: ausformuliert, gegrillt (Freigabe 1), geschätzt, gebaut. Keine rote Probe (Punkt 1: herstellbar, aber
  nicht lohnend, siehe Entscheidung 1 – Punkt 2/3 ändern keine Zusicherung). Lange Zeile umbrochen
  (max. 118 Zeichen).
- **2026-11-04** — Review `pugling-reviewer`: kein 🔴/🟡. Zwei 🟢, ohne Zählung behoben: „keine rote Probe möglich" war zu stark – die Ausnahme ist herstellbar, nur nicht lohnend (Entscheidung 1 umformuliert); und die AK-Zeile von B-265 selbst nennt jetzt die Summen-Zusicherung, nicht nur ihr Verlauf. Vom Prüfer nachgerechnet: der Filter fängt `IOException` (samt `DirectoryNotFoundException`) und `UnauthorizedAccessException`, sonst nichts; „max. 118 Zeichen" gilt für den umbrochenen Block (`:287-296`).
- **2026-11-04** — Abnahme Lauf S, Teil 2: volle Suite **1071/1071**, Build ohne Warnung, Vitest **638/638**, E2E-Gesamtlauf **109 passed** (`exit=0`, nach dem letzten Code-Edit). Rollengang: der Entwickler ist die Rolle (Aufräumschritt, Meldung); Creator, Vater, Sohn Regressionszeugen. **Abgenommen**, Commit siehe Protokoll ([Lauf S](../pm-sitzung-2026-11-04-nacht.md)).
- **2026-11-04** — Nachschau in Lauf S, Teil 3 (frischer `pugling-reviewer`, `git show 764f7b5`, read-only). **Entgangen:** die Begründung widersprach sich (Verlauf und Protokoll „nicht möglich", Entscheidung 1 „nicht lohnend") – [B-373](B-373-inventar-und-begruendung-aus-lauf-s.md). Ohne Befund nachgerechnet: Filter minimal; „max. 118 Zeichen" erwartet 118, gemessen 118 (`:295`); die eingeengte AK von B-265 stimmt mit `:283` (eigenes Fenster) und `:302-303` (alle zehn).
