---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/a11y, bereich/test]
aliases: [Kommentar-Mutante an ungültigem TSX]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf P, Teil 1 in Lauf P, Teil 2 (docs/pm-sitzung-2026-11-03-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-272]
wartet_auf: ""
---

# B-359 · Der Live-Region-Wächter übersieht die Kommentare, die zwischen `&&` und Tag wirklich vorkommen

B-272 hat die Mutante „ohne Kommentar-Entfernung" an der Eingabe `{ok && (\n  {/* warum */}\n  <div role="status">…` gemessen.
Das ist kein gültiges TSX (`( {} <div` liest der Parser als Objekt-Literal mit Kleiner-Zeichen). Zwischen `&&` und Tag
kompilieren nur `// …` und `/* … */` ohne Klammern – und beide entfernte `davor` nicht (`liveRegions.test.ts:79`): ein
`{ok && ( // warum` vor einer Region blieb unerkannt.

## User Story

Als **Entwickler** möchte ich, dass der Wächter eine bedingte Live-Region auch dann meldet, wenn ein Kommentar vor ihr steht,
damit eine Begründung im Code die Regel nicht aushebelt.

## Ist-Stand am Code (Stand `bffb583`)

- `frontend/src/liveRegions.test.ts:79` entfernt nur `\{\/\*…\*\/\}`; `wurzelRegionen` (`:108`) entfernt dagegen `//` und
  `{?/*…*/}?` – zwei Stellen, zwei Regeln.
- Der Selbsttest `:174-177` benutzt die ungültige Form.

## Die echte Lücke

Die einzige Kommentar-Form, die die Erkennung kannte, kommt an der Stelle in kompilierendem Code nicht vor.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **`davor` entfernt `/* … */` mit oder ohne Klammern und `//` bis zum Zeilenende.** *Kosten:* ein `//` in einem String vor
   dem `&&` derselben Zeile (`"https://…" && <p role="status">`) nähme das `&&` mit – ein falsches Nein, heute nirgends.
2. **Die zwei Selbsttest-Fälle benutzen die zwei gültigen Formen**; der ungültige Fall entfällt.

## Akzeptanzkriterien

1. `{ok && ( // warum\n  <div role="status">` und `{ok && /* warum */ <div role="status">` werden je gemeldet.

## Schätzung

`XS`, nur Tests. Testweg: `liveRegions.test.ts`.

## Verlauf

- **2026-11-03** — angelegt aus der Nachschau zu Lauf P, Teil 1 (frischer `frontend-reviewer` über `bffb583`, 🟡). Im selben
  Sprint bearbeitet (Freigabe 3). Rote Probe (neue Fälle gegen die alte Entfernung): **2 failed** (erwartet 2). Mit dem Fix
  `liveRegions.test.ts` **27/27**, auch über die echten Quellen (kein neuer Fund). Die alte Entfernung ist zugleich die
  Mutante „nur Klammer-Kommentar".
- **2026-11-03** — Review `frontend-reviewer`: sauber – Regex-Reihenfolge erkennt einen Block-Kommentar vor einem `//` darin; der String-Grenzfall steht in Entscheidung 1.
- **2026-11-03** — Abnahme Lauf P, Teil 2: Vitest **620/620**, `tsc -b` sauber, E2E-Gesamtlauf **107 passed** (`exit=0`, nach dem letzten Code-Edit gestartet). Rollengang: Der Entwickler ist die Rolle – die Probe ist sein Weg; Creator, Vater, Sohn Regressionszeugen. **Abgenommen**, Commit siehe Protokoll ([Lauf P](../pm-sitzung-2026-11-03-nacht.md)).
