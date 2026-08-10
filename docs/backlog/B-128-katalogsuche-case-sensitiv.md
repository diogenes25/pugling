---
tags: [typ/story, status/abgenommen, bereich/katalog, bereich/backend, rolle/creator]
aliases: [Suche findet nichts bei Großschreibung, Contains ohne NOCASE, Verlags- und Reihensuche]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: backend
migration: ja
vertragsbruch: nein
quelle: Code-Review 2026-08-07 des Standes gegen `origin/main` (Fund 4)
grund: ""
ersetzt_durch: []
entgangen_bei: [B-63]
wartet_auf: ""
nachgeschaut: 2026-08-10
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

## Entscheidungen

Autonom gegrillt im Nachtlauf am 2026-08-09 (Freigabe 1: `art: Defekt`), Protokoll
[pm-sitzung-2026-08-09.md](../pm-sitzung-2026-08-09.md).

1. **Die Empfehlung von Offenem Punkt 1 ist widerlegt — `LIKE` statt Collation.** *Gemessen, nicht
   überlegt*: mit `NOCASE` auf `Publisher.Name` und `TextbookSeries.Name` waren **3 von 4** Testfällen
   **weiterhin rot**. Grund: EF bildet `string.Contains` auf SQLites `instr()` ab, und diese Funktion ist
   byte-genau — sie zieht die Spalten-Collation gar nicht heran. Eine Collation wirkt auf
   *Gleichheitsvergleiche* (`==`, Index-Zugriffe), nie auf eine Teilstring-Suche. Der Vokabelspeicher,
   den die Story als gelöstes Vorbild nennt, ist deshalb **ebenfalls** case-sensitiv in seiner Suche;
   gelöst hat `NOCASE` dort die Dublettenprüfung beim Anlegen. Gebaut wird darum mit
   `EF.Functions.Like` über `SearchPattern` (`Services/Shared/SearchPattern.cs`). *Kosten*: SQLites
   eingebautes `LIKE` faltet **nur ASCII** — „STRASSE" findet weiterhin nicht „Straße". Das steht als
   benannte Grenze am Code, statt später neu entdeckt zu werden.
2. **Die Collation bleibt trotzdem — sie zahlt auf [B-133](B-133-zwei-reihen-ein-anzeigename.md) ein.**
   *Begründung*: Für die Namens-Eindeutigkeit beim Umbenennen ist der Vergleich eine **Gleichheit**, und
   genau dort wirkt `NOCASE`: „Access" und „ACCESS" zählen dadurch als derselbe Anzeigename. Sie ohne
   diesen Zweck zu behalten wäre Vorrat; mit ihm ist sie die halbe Miete der Nachbarstory. *Kosten*: die
   Migrationskette wird neu gefaltet (`migration: ja`) — für eine Änderung, die **nicht** das Problem
   dieser Story löst. Das ist ein bewusster Vorgriff, kein Versehen.
3. **Suchbegriffe werden escaped.** `%` und `_` sind in `LIKE` Platzhalter; ein Creator, der „50%" sucht,
   bekäme sonst alles. `SearchPattern.Contains` neutralisiert sie mit `\`. *Kosten*: eine Hilfsklasse
   statt einer Inline-Zeile — dafür an einer Stelle statt an acht.
4. **Nur die beiden Suchen dieser Story; die übrigen sechs werden eine eigene.** Gemessen (`grep` über
   `Controllers/` und `Services/`): **acht** Freitext-Suchen im Backend, und außer dem Vokabelspeicher
   (`Word`/`Translation`) trägt keine eine Collation — betroffen sind zusätzlich `ClozeTextsController`,
   `ExerciseCatalogController`, `InterestTagsController`, `MediaAssetsController`, `ShopController` und
   `ChildLearnProgressService`. *Begründung*: Jede braucht eine eigene Abwägung (ist ein `Key` oder
   `Slug` überhaupt schreibweisen-tolerant zu suchen?), und B-128s Ziel ist ohne sie erfüllt. Abgelegt
   als [B-135](B-135-freitextsuchen-case-sensitiv.md). *Kosten* — und der ist echt: B-135 braucht keine
   zweite Faltung (es sind reine Query-Änderungen), aber die sechs Suchen bleiben bis dahin
   buchstabengenau.

## Akzeptanzkriterien

1. `GET creator/publishers?search=KLETT` findet den Verlag „Klett".
2. `GET creator/textbook-series?search=GREEN` findet die Reihe „Green Line 1", ebenso die Suche über den
   Verlagsnamen in beliebiger Schreibweise.
3. Ein Integrationstest je Fall, der **vor** der Änderung rot war (Abnahmeform `art: Defekt`).
4. Die Migrationskette bleibt bei Länge 1 (neu gefaltet, nicht verlängert).

## Schätzung

**S** (`wo: backend`, **`migration: ja`**, `vertragsbruch: nein`) — zwei Query-Umstellungen, eine
Hilfsklasse, zwei Collations, eine neu gefaltete Kette, eine neue Testklasse. Kein Vertragsbruch: weder
Route noch DTO ändern sich, das OpenAPI-Dokument bleibt gleich.

**Testweg:** neue Klasse `backend/Pugling.Api.Tests/KatalogSucheCaseTests.cs` mit vier Fällen —
Verlagssuche wie angelegt (Gegenprobe, war vorher grün), durchgehend groß, mitten im Wort mit
Großbuchstaben, und dieselbe Frage für die Reihensuche. Dazu `SchemaGuardTests` (Kettenlänge 1, kein
Modell-Drift) als Abnahme der Faltung, und der `git diff` am `PuglingDbContextModelSnapshot.cs`, der
**genau** die beiden `UseCollation("NOCASE")`-Zeilen zeigen muss.

## Verlauf

- **2026-08-07** — angelegt aus dem Code-Review des Standes gegen `origin/main`. Am Code nachgeprüft und
  dabei **das Szenario des Reviews korrigiert**: „klett" findet „Klett" sehr wohl, über den Slug — der
  Fund bleibt, seine Begründung war zu weit gefasst. `entgangen_bei: [B-63]`: beide Suchen sind in jener
  Story entstanden und waren `abgenommen`.
- **2026-08-09** — Nachtlauf, Sprint 1: autonom gegrillt (vier Entscheidungen), geschätzt (**S**,
  `backend`, `migration: ja`) und gebaut. **Die eigene Empfehlung fiel bei der Messung durch**: mit der
  Collation allein blieben **3 von 4** Fällen rot (der eine grüne ist die Suche in Anlege-Schreibweise,
  die der kleingeschriebene Slug ohnehin abfing). Erst der Umbau auf `EF.Functions.Like` über die neue
  `SearchPattern`-Hilfsklasse macht **4/4 grün**. Die Röte wurde nach dem Umbau noch einmal mit den
  *finalen* Zusicherungen nachgemessen (`git stash` beider Controller): wieder **3 von 4 rot**.
  Zwischendurch war ein Fall aus dem falschen Grund rot — meine erste Fassung zählte Treffer (`erwartet 1,
  gemessen 2`), was über die geteilte Fixture-DB von den Geschwistertests abhängt; die Zusicherung prüft
  jetzt, dass der eigene Name **enthalten** ist. Kette neu gefaltet, `git diff` am Snapshot zeigt genau
  die zwei `UseCollation`-Zeilen, Suite **780/780 grün**. Beim Messen gefunden und abgespalten:
  [B-135](B-135-freitextsuchen-case-sensitiv.md) — sechs weitere Freitextsuchen derselben Bauart.
- **2026-08-10** — `pugling-reviewer`, Re-Review: keine Korrektheitsfunde am Suchumbau; die
  Escaping-Reihenfolge und die Übersetzbarkeit von `EF.Functions.Like(..., escape)` hat der Reviewer
  gegen eine echte SQLite-Instanz **gemessen** und bestätigt. Zwei Test-Lücken geschlossen: der
  `_`-Zweig von `SearchPattern` war unbeobachtet (die Zeile zu löschen ließ alles grün) — der Fall ist
  jetzt eine `[Theory]` über `%` **und** `_`; dazu `EnsureSuccessStatusCode()` auf den Vorbereitungs-POSTs
  und `take=500`, weil bei gebrochenem Escaping die Treffermenge die ganze Tabelle ist und die
  Vorgabe-Seitengröße das Gegenbeispiel verstecken könnte. Kommentar-Nits behoben: der Querverweis in
  `SearchPattern.cs` zeigte auf `Publisher.Name` (die Spalte ohne heutigen Vergleich) statt auf
  `TextbookSeries.Name`, „already" suggerierte Bestand für eine im selben Diff entstandene Collation, und
  die dritte messbare Wirkung (`ORDER BY` wird case-insensitiv) fehlte in der Abgrenzung.
- **2026-08-10** — **abgenommen.** Commit `0663aa8` (gemeinsam mit B-133, weil beide
  `TextbookSeriesController` und `PuglingDbContext` anfassen). Verifikation: **788/788**, E2E **29/29**
  als Rollengang, `pugling-reviewer` zweimal — der zweite Lauf hat Escaping-Reihenfolge und
  `LIKE ... ESCAPE`-Übersetzbarkeit gegen eine echte SQLite-Instanz gemessen. Migrationskette bei
  Länge 1, Snapshot-Diff genau die zwei beabsichtigten Zeilen.
- **2026-08-10** — nachgeschaut (Nachtlauf, Retro des Folge-Sprints). Geprüft wurde nicht „ist es noch da",
  sondern zwei benannte Punkte: (a) beide Aufrufstellen dieser Story übergeben `EF.Functions.Like` das
  dritte Argument `SearchPattern.Escape` — ohne das wäre ein getipptes `%` ein Treffer auf alles; (b) die
  an der Klasse dokumentierte ASCII-Grenze stimmt weiter. **Ein Befund, aber kein Defekt:** die von dieser
  Story ergänzte `NOCASE`-Collation auf `Publisher.Name` war bis heute **wirkungslos** — es gab keinen
  einzigen Gleichheitsvergleich, auf den sie hätte wirken können. [B-136](B-136-verlag-umbenennen-erzeugt-namensdublette.md)
  macht sie tragend. Kein durchgekommener Defekt.
