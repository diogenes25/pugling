---
tags: [typ/story, status/ausformuliert, bereich/katalog, bereich/backend, bereich/qualitaet]
aliases: [Topics ohne Längenbegrenzung, JSON-Spalte umgeht die 200-Zeichen-Regel]
status: ausformuliert
prio: P3
art: Aufräumen
quelle: Code-Review 2026-08-07 des Standes gegen `origin/main` (Fund 8)
grund: ""
ersetzt_durch: []
entgangen_bei: [B-63]
wartet_auf: ""
---

# B-130 · Aus einem 200-Zeichen-Feld wurde eine unbegrenzte Liste, ohne dass eine Grenze nachrückte

Vor [B-63](B-63-lehrwerk-hierarchie.md) waren die Themen einer Unit ein `string?` mit `maxLength: 200` —
gedeckelt durch die Konventionsschleife, die im Repo jede String-Spalte kappt, die nicht mit Begründung
in `UnlimitedByDesign` steht. Als daraus eine `List<string>` in einer JSON-Spalte wurde, fiel dieser
Deckel weg, und keiner trat an seine Stelle: weder für die Länge eines Themas noch für ihre Anzahl.

## User Story

Als **Betreiber** möchte ich, dass ein Schreibpfad nicht beliebig viel Text annimmt und dauerhaft
speichert — auch dann nicht, wenn der Aufrufer authentifiziert ist.

## Ist-Stand am Code

- `Controllers/Creator/SeriesUnitsController.cs:163-164`:
  `CleanTopics(values) => [.. (values ?? []).Select(t => t.Trim()).Where(t => t.Length > 0)]` — trimmt
  und wirft Leeres weg. **Keine** Prüfung auf Einzellänge, **keine** auf Listenlänge.
- Aufgerufen an beiden Schreibpfaden: `:77` (`Create`) und `:111` (`Update`).
- Die Spalte ist in der Migration `Topics = table.Column<string>(type: "TEXT", nullable: false)` — also
  ohne `maxLength`, wie jede JSON-Spalte des Projekts.
- Zum Vergleich, wie das Repo es sonst hält: `Data/PuglingDbContext.cs` kappt jede nicht ausgenommene
  String-Spalte auf 200 Zeichen, und `UnlimitedByDesign` verlangt für jede Ausnahme eine Begründung
  (Tor **G7**/Etappe E11 des DB-Umbaus).

Die Werte gehen von dort in das Briefing des KI-Creators — sie werden also gelesen, nicht nur abgelegt.

## Die echte Lücke

Nicht „hier fehlt eine Validierung, also ist es unsicher": Der Endpunkt ist Creator-gegated und
eigentümergebunden, es gibt keinen anonymen Weg dorthin, und niemand hat je zu viele Themen eingetippt —
die Oberfläche legt sie einzeln als Chips an.

Die Lücke ist eine **Regel, die beim Typwechsel verlorenging**. Das Projekt hat eine ausdrückliche
Haltung zu unbegrenzten Spalten (jede braucht eine begründete Ausnahme), und die JSON-Spalte hat sich
dieser Haltung entzogen, ohne dass jemand die Entscheidung getroffen hätte. Genau deshalb `Aufräumen`
und nicht `Defekt`: heute verhält sich nichts falsch, aber die Begründungspflicht ist still ausgefallen.

## Offene Punkte

1. **Welche Grenzen?** Empfehlung: 200 Zeichen je Thema (dasselbe Maß, das die Spalte vorher trug) und
   eine Obergrenze für die Anzahl — Vorbild ist die Obergrenze von 24 Fenstern je Position aus
   [B-10](B-10-zeitfenster-pro-kind.md), die aus demselben Grund eingezogen wurde („die JSON-Spalte ist
   bewusst unbegrenzt"). Konkrete Zahl beim Grillen; 50 Themen je Unit wären großzügig.
2. **Ablehnen oder abschneiden?** Empfehlung: ablehnen mit `400 validation_error`. Stilles Abschneiden
   ist genau die Sorte lautloser Datenverlust, gegen die das Projekt sonst `unknown_field` stellt.
3. **Gilt das auch für die anderen JSON-Listen?** `Gaps`, `WordBank`, `Interests`, `OwnedSkins`,
   `StageSchedule` sind ebenso unbegrenzt. Beim Ausformulieren **nicht** erhoben. Empfehlung: erst
   messen, dann entscheiden, ob daraus eine Regel oder ein Tor wird — nicht ungeprüft mitziehen.

## Akzeptanzkriterien

> Entwurf, hängt an den Offenen Punkten.

1. `POST`/`PATCH` einer Unit mit einem überlangen Thema wird mit `400` und maschinenlesbarem `code`
   abgewiesen, statt es zu speichern.
2. Dasselbe für eine Themenliste über der Obergrenze.
3. Bestandsdaten und der Normalfall (wenige kurze Themen) bleiben unberührt; die Suite ist so grün wie
   vorher (Abnahmeform `art: Aufräumen`).
4. Die gewählte Grenze steht als Begründung am Code, nicht nur in dieser Story.

## Verlauf

- **2026-08-07** — angelegt aus dem Code-Review des Standes gegen `origin/main`, am Code nachgeprüft
  (`SeriesUnitsController.cs:163-164`, Migrationsspalte ohne `maxLength`). Als `Aufräumen` eingestuft:
  kein heutiger Fehlbetrieb, sondern eine beim Typwechsel entfallene Begründungspflicht.
  `entgangen_bei: [B-63]`: der Typwechsel ist in jener Story passiert und war `abgenommen`.
