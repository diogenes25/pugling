---
tags: [typ/story, status/ausformuliert, bereich/katalog, bereich/backend, rolle/creator]
aliases: [Suche findet nichts bei Großschreibung, Contains ohne NOCASE, Verlags- und Reihensuche]
status: ausformuliert
prio: P3
art: Defekt
quelle: Code-Review 2026-08-07 des Standes gegen `origin/main` (Fund 4)
grund: ""
ersetzt_durch: []
entgangen_bei: [B-63]
wartet_auf: ""
---

# B-128 · Die Katalogsuche findet „KLETT" nicht, obwohl „Klett" da ist

Die Suchfelder über Verlage und Lehrwerk-Reihen vergleichen mit `Contains`, das EF Core auf SQLites
`instr()` abbildet — und das ist **ohne `NOCASE`-Collation buchstabengenau**. Der Vokabelspeicher hat
dieses Problem längst gelöst; die beiden neuen Katalog-Suchen aus [B-63](B-63-lehrwerk-hierarchie.md)
haben die Lösung nicht mitbekommen.

## User Story

Als **Creator** möchte ich einen Verlag oder ein Lehrwerk finden, ohne die Schreibweise zu treffen, die
jemand anders beim Anlegen gewählt hat.

## Ist-Stand am Code

Drei Vergleiche, alle ohne Collation:

- `Controllers/Creator/PublishersController.cs:45` — `p.Slug.Contains(search) || p.Name.Contains(search)`
- `Controllers/Creator/TextbookSeriesController.cs:76-77` — `s.Name.Contains(search) ||
  s.Slug.Contains(search) || (s.Publisher != null && s.Publisher.Name.Contains(search))`

`Data/PuglingDbContext.cs:274-275` setzt `NOCASE` **nur** auf `Vocabulary.Word` und `.Translation`, mit
einem Kommentar (`:268`), der die Begründung schon trägt: `LOWER(...)` wäre ein Ausdruck, über den kein
Index greift — die Collation ist der richtige Weg.

**Wie kaputt es wirklich ist** (am Code durchgerechnet, nicht aus dem Review übernommen — dessen
Beispiel „klett findet Klett nicht" ist **falsch**): Der Slug ist konstruktionsbedingt kleingeschrieben,
darum fängt er jede rein kleingeschriebene Suche ab. Es scheitern nur Suchbegriffe, die Großbuchstaben
enthalten und nicht exakt so im Namen stehen:

| Bestand | Suche | Slug trifft? | Name trifft? | Ergebnis |
| --- | --- | --- | --- | --- |
| „Klett" (`klett`) | `klett` | ja | – | gefunden |
| „Klett" (`klett`) | `Klett` | nein | ja | gefunden |
| „Klett" (`klett`) | `KLETT` | nein | nein | **nicht gefunden** |
| „Klett" (`klett`) | `Lett` | nein | nein | **nicht gefunden** |

## Die echte Lücke

Nicht „die Suche ist unbrauchbar" — der kleingeschriebene Normalfall funktioniert, zufällig, über den
Slug. Die Lücke ist, dass die Trefferquote von einer Eigenschaft abhängt, die nichts mit dem Suchbegriff
zu tun hat (dass der Slug kleingeschrieben ist), und dass sie genau dann kippt, wenn ein Mensch tippt
wie ein Mensch: mit Großbuchstaben am Wortanfang, mitten im Wort gesucht.

Die Reihensuche trifft es härter als die Verlagssuche: Reihennamen tragen häufiger Ziffern und
Binnengroßschreibung („Green Line 1"), und die Suche über den **Verlagsnamen** (`s.Publisher.Name`) hat
gar keinen Slug als Auffangnetz.

## Offene Punkte

1. **Collation am Modell oder `EF.Functions.Like`?** Empfehlung: Collation, wie beim Vokabelspeicher —
   sie gilt für jeden künftigen Vergleich auf der Spalte, statt an jeder Suchstelle wiederholt zu
   werden. Kosten: eine Schemaänderung, die Migrationskette wird **neu gefaltet** (`SchemaGuardTests`,
   Kettenlänge 1).
2. **Welche Spalten?** Empfehlung: `Publisher.Name` und `TextbookSeries.Name`. Die Slugs brauchen keine
   — sie sind qua Ableitung schon kleingeschrieben, und eine Collation darauf verspräche eine
   Toleranz, die dort nie gebraucht wird.
3. **Weitere Suchen derselben Bauart?** Beim Ausformulieren nicht erschöpfend erhoben. Vor dem Bau
   einmal alle `Contains(search)` im Backend zählen und entscheiden, ob sie zu dieser Story gehören
   oder als eigene Regel/als Tor behandelt werden.

## Akzeptanzkriterien

> Entwurf, siehe Offene Punkte.

1. `GET creator/publishers?search=KLETT` findet den Verlag „Klett".
2. `GET creator/textbook-series?search=GREEN` findet die Reihe „Green Line 1", ebenso die Suche über den
   Verlagsnamen in beliebiger Schreibweise.
3. Ein Integrationstest je Fall, der **vor** der Änderung rot war (Abnahmeform `art: Defekt`).
4. Die Migrationskette bleibt bei Länge 1 (neu gefaltet, nicht verlängert).

## Verlauf

- **2026-08-07** — angelegt aus dem Code-Review des Standes gegen `origin/main`. Am Code nachgeprüft und
  dabei **das Szenario des Reviews korrigiert**: „klett" findet „Klett" sehr wohl, über den Slug — der
  Fund bleibt, seine Begründung war zu weit gefasst. `entgangen_bei: [B-63]`: beide Suchen sind in jener
  Story entstanden und waren `abgenommen`.
