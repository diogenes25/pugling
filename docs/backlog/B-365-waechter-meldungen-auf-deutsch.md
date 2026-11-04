---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/test]
aliases: [Vertrags-Doku-Wächter deutsch]
status: abgenommen
prio: P4
art: Aufräumen
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: pugling-reviewer zu B-259 in Lauf Q, Teil 1 (docs/pm-sitzung-2026-11-03-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-365 · Die Meldungen der reflexiven Wächter sind teils deutsch

`ConventionGuardTests.Vertrags_Dokumentation_Erreicht_Das_Dokument` meldet „<para> ausserhalb von <summary>/<param>/<remarks> –
der Text erscheint nicht im Dokument." und „zweites <summary> im selben Doku-Block …" auf Deutsch. Die Root-`CLAUDE.md` verlangt
englische Meldungen roter Tore. Außerhalb des Diffs von B-259 gefunden; beim Ausformulieren gezählt: es sind nicht zwei.

## User Story

Als **Entwickler**, den ein Tor rot meldet, möchte ich jede Meldung in derselben Sprache lesen wie Code und Kommentare, damit die
Regel aus der Root-`CLAUDE.md` auch für die Tore selbst gilt.

## Ist-Stand am Code (Stand `b79b4a4`)

Deutsche Meldungstexte reflexiver Wächter (`Assert`-Meldung oder Fund-Zeile), per `grep` über die Wächter-Dateien gezählt:

- `ConventionGuardTests.cs`: **14** – zweimal „Zu wenige Controller-Dateien" (`:63`, `:100`), Konflikt-Code (`:102`), Rot-Liste
  (`:157`, `:160`, `:162`), unpaginierte GETs (`:204`), Vertragstypen (`:226`, `:228`), Antworttypen ins Vertrags-Projekt (`:257`,
  erst im Review gefunden), Lösungsfeld (`:454`, `:458`), Doku-Blöcke (`:724`, `:732`).
- `SchemaGuardTests.cs`: **4** Selbstschutz-Meldungen („Zu wenige Tabellen/Enum-Properties/String-Spalten/Entity-Typen").
- `PatchSemanticsTests.cs:426`: eine, `SeedContractTests.cs:80/81`: zwei Selbstschutz-Meldungen (`:81` erst im Review gefunden).
- `ClientRouteGuardTests`, `EndpointCoverageGuard`, `SlugRenameGuardTests`, `EmptyExerciseGuardTests`: keine (dort nur deutsche
  Testdaten wie `gibt-es-nicht`, die bleiben).

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Aufräumen`)

1. **Alle 21 übersetzen**, nicht nur die zwei genannten – die Regel gilt für jedes Tor, und ein halber Umbau ließe die nächste
   Nachschau dieselbe Story wieder anlegen.
2. **Nicht angefasst:** gewöhnliche Testmeldungen außerhalb der Wächter (`MediaSelectionTests:361`, `CreatorAgentTests:1282`) und
   Produkt-/Testdaten-Strings – die Regel nennt die Meldungen roter **Tore**. Ein Zitat eines deutschen Doku-Abschnitts
   („Der Nenner ist die Falle") bleibt als Eigenname stehen.

## Akzeptanzkriterien

1. Kein Meldungstext in den Wächter-Dateien ist deutsch – geprüft über **Umlaute in String-Literalen** der Wächter-Dateien, nicht
   über die Liste der bekannten Sätze (die erste Fassung suchte nur, was sie schon kannte, und konnte darum nichts Übersehenes finden).
2. Die Suite bleibt grün; kein Verhalten ändert sich.

## Schätzung

`S` (21 Stellen in vier Dateien). Testweg: volle Suite.

## Verlauf

- **2026-11-03** — angelegt aus dem `pugling-reviewer` zu B-259 (🟢, außerhalb des Diffs).
- **2026-11-04** — Lauf S, Teil 1: ausformuliert (19 statt 2 Stellen gezählt), gegrillt (Freigabe 1), geschätzt, gebaut – per
  Skript mit Zählprüfung je Ersetzung (18 Einträge, einer zweifach). AK 1: `grep` → 0 in allen vier Dateien. Die Wächter-Klassen
  danach grün (`ConventionGuardTests|ContractDocument|SchemaGuard`: **40/40**). Rote Probe entfällt: `Aufräumen` ohne Verhaltensänderung.
- **2026-11-04** — Review `pugling-reviewer` (🟡): zwei deutsche Meldungen übersehen – `ConventionGuardTests.cs:257` und
  `SeedContractTests.cs:81`; die erste AK suchte nur die bekannten Sätze und hätte das nie gemeldet. Beide übersetzt, AK auf
  Umlaute in String-Literalen umgestellt: über die Wächter-Dateien erwartet 0, gemessen 0 (übrig nur deutsche Testdaten in
  `PatchSemanticsTests`, die bleiben). Zählt (Code geändert, 🟡): **Zähler Teil 1: 2.**
- **2026-11-04** — Abnahme Lauf S, Teil 1: volle Suite **1068/1068**, Vitest **638/638**, `tsc -b` sauber, E2E-Gesamtlauf **109 passed** (`exit=0`, nach dem letzten Edit). Rollengang: der Entwickler ist die Rolle (Tor-Meldungen, Tor gegen Inline-Elemente im Listenpunkt); Creator über `ContractDocumentTests` und den Dokument-Diff (eine Zeile, Code- und Fettsatz da); Vater und Sohn Regressionszeugen (Suiten grün, nur Kommentare im Frontend). **Abgenommen**, Commit siehe Protokoll ([Lauf S](../pm-sitzung-2026-11-04-nacht.md)).
