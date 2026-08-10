---
tags: [typ/story, status/abgenommen, bereich/katalog, bereich/backend, bereich/qualitaet]
aliases: [Topics ohne Längenbegrenzung, JSON-Spalte umgeht die 200-Zeichen-Regel]
status: abgenommen
prio: P3
art: Aufräumen
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: Code-Review 2026-08-07 des Standes gegen `origin/main` (Fund 8)
grund: ""
ersetzt_durch: []
entgangen_bei: [B-63]
wartet_auf: ""
nachgeschaut: 2026-08-10
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

## Entscheidungen

Autonom gegrillt im Nachtlauf am 2026-08-09 (Freigabe 1: `art: Aufräumen`), Protokoll
[pm-sitzung-2026-08-09.md](../pm-sitzung-2026-08-09.md).

0. **Die Prämisse dieser Story stimmt so nicht — und der Fund ist dadurch schärfer, nicht schwächer.**
   Die Story sagt, die JSON-Spalte habe sich der Begründungspflicht „entzogen, ohne dass jemand die
   Entscheidung getroffen hätte". Nachgesehen: `SeriesUnit.Topics` **steht** in
   `PuglingDbContext.UnlimitedByDesign` (`:1008`) mit Grund („Topics of the unit as a JSON list - grows
   with the material"). Die *Spalten*-Ausnahme ist also bewusst und dokumentiert; Tor G-Unbegrenzt hat
   gehalten. Ausgefallen ist etwas anderes: die Grenze für die **Einträge darin**. Solange ein Thema eine
   200-Zeichen-Spalte war, hielt die Datenbank die Linie; seit dem Typwechsel hält sie niemand.
   *Kosten dieser Korrektur*: keine — sie verschiebt nur, wogegen gebaut wird (Anwendungsvalidierung
   statt Spaltenlänge).
1. **200 Zeichen je Thema, 50 Themen je Unit.** *Begründung*: 200 ist keine freie Wahl, sondern die
   `DefaultLength` der Konventionsschleife (`PuglingDbContext.cs:1021`) — genau das Maß, das die Spalte
   vorher trug. 50 ist großzügig gewählt wie die 24 Fenster je Position aus
   [B-10](B-10-zeitfenster-pro-kind.md): eine echte Unit listet eine Handvoll. *Kosten*: eine Unit mit
   mehr als 50 Themen ist künftig nicht anlegbar; wer das braucht, teilt sie — was fachlich ohnehin
   richtiger ist.
2. **Ablehnen mit `400 validation_error`, nicht abschneiden.** *Begründung*: Die Themen sind genau das,
   was der KI-Creator als **gesetzten Stoff** liest (`UnitForm`-Hinweis: „er darf ihn einkleiden, aber
   nicht austauschen"). Ein still gekürztes Thema wäre kein kürzeres, sondern ein **falsches**. Kein
   neuer `ApiErrors`-Code nötig — `ValidationError` trägt den Fall. *Kosten*: ein Aufrufer, der bisher
   201 bekam, bekommt jetzt 400; das ist eine Verhaltensänderung an einem Endpunkt, den außer der
   eigenen Oberfläche niemand bedient.
3. **Beide Schreibwege, ein Wächter.** *Begründung*: `POST` und `PATCH` schreiben `Topics` unabhängig
   (`:77` und `:111`) — die Vorprüfung nur an einen zu hängen wäre dieselbe Fehlerklasse wie
   [B-124](B-124-umbenennen-umgeht-die-eindeutigkeit.md) (Anlegen geschützt, Ändern nicht). *Kosten*:
   eine Hilfsmethode mehr im Controller statt einer Zeile.

## Akzeptanzkriterien

1. `POST`/`PATCH` einer Unit mit einem überlangen Thema wird mit `400` und maschinenlesbarem `code`
   abgewiesen, statt es zu speichern.
2. Dasselbe für eine Themenliste über der Obergrenze.
3. Bestandsdaten und der Normalfall (wenige kurze Themen) bleiben unberührt; die Suite ist so grün wie
   vorher (Abnahmeform `art: Aufräumen`).
4. Die gewählte Grenze steht als Begründung am Code, nicht nur in dieser Story.

## Schätzung

**S** (`wo: backend`, `migration: nein`, `vertragsbruch: nein`) — zwei Konstanten, eine Prüfmethode, zwei
Aufrufstellen, eine neue Testklasse. **Keine Schemaänderung**: die Spalte bleibt unbegrenzt (das ist die
dokumentierte Ausnahme), begrenzt wird der Inhalt in der Anwendung — die Migrationskette wird also nicht
neu gefaltet. Vergleichbar mit dem S-Anker (B-01).

**Testweg:** neue Klasse `backend/Pugling.Api.Tests/SeriesUnitTopicLimitTests.cs` mit vier Fällen —
zu langes Thema beim Anlegen, genau 200 Zeichen als Gegenprobe, mehr als 50 Themen, und dasselbe über
den PATCH-Weg. Kein `/smoke-test`, kein E2E: die Regel sitzt an der API und ist dort direkt prüfbar.

## Verlauf

- **2026-08-07** — angelegt aus dem Code-Review des Standes gegen `origin/main`, am Code nachgeprüft
  (`SeriesUnitsController.cs:163-164`, Migrationsspalte ohne `maxLength`). Als `Aufräumen` eingestuft:
  kein heutiger Fehlbetrieb, sondern eine beim Typwechsel entfallene Begründungspflicht.
  `entgangen_bei: [B-63]`: der Typwechsel ist in jener Story passiert und war `abgenommen`.
- **2026-08-09** — Nachtlauf, Sprint 1: autonom gegrillt (vier Entscheidungen, davon eine, die den
  eigenen Ist-Stand korrigiert — siehe Entscheidung 0), geschätzt (**S**, `backend`) und gebaut
  (`SeriesUnitsController.cs`: `MaxTopicLength`/`MaxTopics` + `ValidateTopics`, an beiden Schreibwegen).
  **Rote Probe mit Zahl, und ehrlich nachgeholt:** ich hatte den Wächter *vor* dem Test gebaut, also
  gegen Auflage 5 des Auftrags verstoßen. Statt die Röte zu behaupten, habe ich den Controller per
  `git stash` zurückgenommen und gemessen: **3 von 4 rot, 1 grün** — der grüne ist der
  200-Zeichen-Grenzfall, der bewusst als Gegenprobe drinsteht und auch vorher grün war. Mit Wächter
  **4/4 grün**.
- **2026-08-10** — **abgenommen.** Commit `c478582`. Verifikation: Suite **788/788** grün, E2E **29/29**
  (Rollengang, nach den letzten Änderungen erneut gefahren), `pugling-reviewer` zweimal gelaufen —
  ohne Fund an dieser Story. Abnahmeform `Aufräumen` erfüllt: kein Verhalten außerhalb der neuen Grenzen
  geändert, alles so grün wie vorher.
- **2026-08-10** — nachgeschaut (Nachtlauf, Retro des Folge-Sprints). Geprüft wurde, ob die Grenze eine
  echte Prüfung ist und nicht nur ein Kommentar: `MaxTopics = 50` wird in `SeriesUnitsController.cs:192-194`
  ausgewertet, und die Fehlermeldung nennt die gesendete Anzahl. Kein durchgekommener Defekt.
