---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/doku]
aliases: [list item ohne c und b]
status: abgenommen
prio: P4
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Lauf Q, Teil 1 – Rohstrings des OpenAPI-Generators zu B-259/B-260 (docs/pm-sitzung-2026-11-03-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-11-04"
entgangen_bei: []
wartet_auf: ""
---

# B-361 · In Listenpunkten verliert das Vertragsdokument `<c>` und `<b>`

Beim Lesen der Rohstrings des OpenAPI-Generators (Lauf Q, B-259/B-260) gefunden: in einem `<item>` einer `<list>` kommen
Inline-Elemente als nackter Text an. `UsageResponse.otherLearnersCount` (`backend/Pugling.Contracts/Creator/ExerciseCatalogDtos.cs`)
schreibt `<c>409 exercise_in_use</c>` und `<b>creator without own children</b>` – im Dokument steht „fails with 409
exercise_in_use" und „For a creator without own children", ohne Code- bzw. Fettsatz. Bewusst nicht in B-259 mitgenommen: anderes
Verhalten des Generators, und der Text bleibt lesbar.

## User Story

Als **Creator**, der im Vertragsdokument nachliest, warum ein Löschen scheitert, möchte ich den Fehlercode als Code gesetzt sehen,
damit ich ihn als maschinenlesbaren Wert erkenne und nicht als Prosa überlese.

## Ist-Stand am Code (Stand `b79b4a4`)

- `<list>` in Dateien, deren Doku das Dokument erreichen kann (`Pugling.Contracts`, `Pugling.Api/Controllers`): **zwei** –
  `ExerciseCatalogDtos.cs:72` (erreicht `docs/openapi/v1.json`, `components/schemas/UsageResponse/properties/otherLearnersCount`) und
  `ExerciseMediaController.cs:13` (Klassen-Summary eines Controllers; „Item override" steht **nicht** im Dokument).
- Inline-Elemente in Listenpunkten dieser Dateien: vier Zeilen (`ExerciseCatalogDtos.cs:73/75`, `ExerciseMediaController.cs:14/17`).
- Im Dokument: `* Without it … fails with 409 exercise_in_use` – ohne Backticks; außerhalb der Liste kommt `**different children**`
  fett an. Der Normalisierer kann das nicht reparieren: das Markup ist weg, bevor er läuft (wie bei B-259).

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Umformulieren statt Liste:** die zwei Gründe als Prosa, Inline-Elemente bleiben erhalten. Kosten: die Aufzählung fällt im
   Tooltip der IDE weg.
2. **Ein Tor im Quelltext** (`ConventionGuardTests.Listenpunkte_Tragen_Keine_Inline_Elemente`), über dieselben Dateien wie das
   Klebestellen-Tor von B-259: der Generator ändert sein Verhalten nicht, und die Form ist unsichtbar.
3. **Auch die Controller-Klassen-Summary** wird umgeschrieben, obwohl sie das Dokument heute nicht erreicht: eine Regel für alle
   Dateien ist billiger, als zu raten, welchen Block der Generator liest (benannt in den Grenzen des Tors).

## Akzeptanzkriterien

1. Im Dokument steht `` `409 exercise_in_use` `` in Backticks und `**creator without own children**` fett.
2. Das Tor meldet jedes Inline-Element in einem offenen `<item>` mit Datei und Zeile; ein geschlossenes Item, Text nach der Liste
   und ein nie geschlossenes Item im vorigen Kommentarlauf melden nichts.

## Schätzung

`XS`. Testweg: `ConventionGuardTests` (Tor + Selbsttest), `ContractDocumentTests` schreibt das Dokument.

## Verlauf

- **2026-11-03** — angelegt in Lauf Q, Teil 1 aus den Rohstrings zu B-259/B-260. Aus dem rohen Dokument gelesen.
- **2026-11-04** — Lauf S, Teil 1: ausformuliert, gegrillt (Freigabe 1), geschätzt, gebaut. **Rote Probe** (Tor vor der
  Umformulierung): erwartet 4 Fundstellen, gemessen 4 (`ExerciseCatalogDtos.cs:73/75`, `ExerciseMediaController.cs:14/17`). Danach
  0, und das Dokument trägt `` `409 exercise_in_use` `` und `**creator without own children**` (AK 1, `git diff docs/openapi/v1.json`:
  eine Zeile). AK 2 je Fall, Mutante → rot: „`<c>` kein Inline-Tag" → 2 failed; „Schließen des Items ignoriert" → 1 failed;
  „offenes Item leckt in den nächsten Lauf" → 1 failed; „Zeile ohne +1" → 1 failed. Beim Messen selbst gefunden: der erste Fall
  „Zustand leckt" hatte ein **geschlossenes** Item und blieb unter der Leck-Mutante grün (8/8 passed) – auf ein nie geschlossenes
  Item umgeschrieben (zählt, Test geändert; **Zähler Teil 1: 1**).
- **2026-11-04** — Review `pugling-reviewer`: zu B-361 kein 🔴/🟡. Zwei 🟢, ohne Zählung behoben (Freigabe 10): der Kommentar am Fall
  „Text nach der Liste" nannte eine Mutante, für die der Scan keinen Ort hat (er kennt `<list>` nicht) – jetzt „Scan beginnt in einem
  Item"; und `break` nach dem ersten Fund übersprang ein `</item>` auf derselben Zeile (Folgezeile falsch gemeldet) – jetzt ein Merker,
  neuer Fall, Mutante „`break` statt weiterlesen" → 1 failed. Nicht erkannt und heute 0-mal in den Quellen: `<code>`, `<seealso>`,
  Inhalt von `<listheader>`; nachgerechnet vom Prüfer: übrige `<list>` außerhalb von Contracts/Controllers erreichen das Dokument
  nicht (0 Treffer ihrer Texte in `v1.json`).
- **2026-11-04** — Abnahme Lauf S, Teil 1: volle Suite **1068/1068**, Vitest **638/638**, `tsc -b` sauber, E2E-Gesamtlauf **109 passed** (`exit=0`, nach dem letzten Edit). Rollengang: der Entwickler ist die Rolle (Tor-Meldungen, Tor gegen Inline-Elemente im Listenpunkt); Creator über `ContractDocumentTests` und den Dokument-Diff (eine Zeile, Code- und Fettsatz da); Vater und Sohn Regressionszeugen (Suiten grün, nur Kommentare im Frontend). **Abgenommen**, Commit siehe Protokoll ([Lauf S](../pm-sitzung-2026-11-04-nacht.md)).
- **2026-11-04** — Nachschau in Lauf S, Teil 2 (frischer `pugling-reviewer`, `git show cc789ed`, read-only): kein 🔴/🟡, **keine Entgleitung**. Nachgerechnet: rote Probe 4/4 (von Hand gegen die Elterndatei), `v1.json` 1/1 geänderte Zeile, `<list>`/`<item>`/`<code>`/`<seealso>` in Contracts und Controllern 0, je Selbsttest-Fall die benannte Mutante von Hand nachvollzogen (drei davon nur gelesen, nicht gelaufen: `see`, Zeilenumbruch, „Scan beginnt im Item“); die umformulierten Doku-Kommentare inhaltsgleich.
