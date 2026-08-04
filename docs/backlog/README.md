---
tags: [typ/referenz, bereich/doku]
aliases: [Backlog, Story-Bereich, Ideen-Backlog]
---

# Backlog: der definierte Bereich für Ideen und User-Stories

Hier liegt **eine Story je Datei** (`B-<nn>-<slug>.md`) mit einem maschinell lesbaren `status` — von der
rohen Idee bis zur belegten Abnahme. Der Bereich existiert, weil offene Ideen vorher verstreut in
`docs/*-plan.md`, in PM-Roadmaps und als „offen:"-Vermerk in Memory-Notizen lagen: man konnte an keiner
Stelle sehen, *was* offen ist, und schon gar nicht, *wie weit* eine Idee gereift ist.

**Die Id ist die stabile Referenz** („mach B-07 weiter") — dieselbe Rolle wie die Log-Id bei den
[Anmerkungen](../anmerkungen-plan.md). Sie wird nie neu vergeben und wechselt nie ihre Bedeutung.

Getrieben wird der Bereich vom Skill **`/backlog`**: `/backlog` zeigt den Stand, `/backlog B-07` erarbeitet
**eine** Stufe weiter, `/backlog B-07 grillen` holt die Entscheidungen beim Menschen ab,
`/backlog neu "…"` nimmt eine Idee auf, `/backlog ernten` zieht neue Ideen aus den Quellen.

## Die Kette: sechs Stufen mit Eintrittsbedingung

Eine Stufe ohne Eintrittsbedingung ist Dekoration. Darum trägt jede eine, und `/backlog` **weigert sich**
weiterzuschieben, solange sie nicht erfüllt ist — statt die Lücke stillschweigend zu übergehen.

| Stufe | Eintrittsbedingung (was vorliegen muss) | Wer treibt |
| --- | --- | --- |
| `idee` | Titel, ein Absatz, `quelle:`-Link, `prio`, `art`. `unverifiziert: true`. Sonst nichts. | Ernte · Anmerkungs-Widget · Mensch |
| `ausformuliert` | User Story („Als … möchte ich … damit …"), **Ist-Stand am Code mit `Datei:Zeile`**, „Die echte Lücke", Entwurf der Akzeptanzkriterien, **Offene Punkte** als Fragenliste. `unverifiziert` ist weg. | PM (Skill) |
| `gegrillt` | Jeder offene Punkt ist **entweder** eine nummerierte Entscheidung (Begründung **und** Kosten) **oder** ausdrücklich zurückgestellt; erledigte durchgestrichen statt gelöscht. Akzeptanzkriterien final. | **Mensch**, im Dialog |
| `geschaetzt` | `groesse`, `wo`, `migration`, `vertragsbruch` gesetzt; Risiken; Angriffsplan (Reihenfolge, Backend zuerst); Testweg (welcher Integrationstest / E2E / `/smoke-test`). | Entwickler (Skill) |
| `in-arbeit` | Es wird gebaut; `## Verlauf` wächst mit. | Entwickler |
| `abgenommen` | Verifikation **belegt**: echte Testzahl, gelaufener `/smoke-test` bzw. E2E, bei Backend-Änderungen zusätzlich `pugling-reviewer`, und **Commit(s) genannt**. | Entwickler |
| `verworfen` | Begründung im Feld `grund`. Bei „geteilt" zusätzlich `ersetzt_durch`. | Mensch |

`verworfen` ist ein **Ziel, kein Scheitern.** Mehrere Punkte im Projekt sind bewusste Nicht-Ziele; als
`verworfen` mit Grund tauchen sie nie wieder als offene Frage auf, statt in jeder Sichtung erneut
Aufmerksamkeit zu kosten.

**Gegenstandslos heißt `verworfen`, nicht `abgenommen`.** Fällt eine Story weg, weil ein anderer Umbau ihr
den Boden entzogen hat, ist nichts gebaut und nichts verifiziert — `abgenommen` behauptete beides und würde
in einem halben Jahr wie eine gelieferte Funktion gelesen. Der Grund gehört ins Feld `grund` („gegenstandslos
durch E13"). Das ist passiert, bevor dieser Satz hier stand: der Wächter hat es an neun fehlenden Belegen
gemeldet.

### Die Eintrittsbedingung darf in einem verlinkten Protokoll stehen

Ist-Stand und Entscheidungen dürfen **statt in der Story** in einem verlinkten Abschnitt eines *datierten
Protokolls* liegen (`docs/backlog-vokabellernen.md`, `docs/pm-sitzung-*.md`). Grund: Das Grill-Protokoll vom
2026-07-30 nummeriert seine zehn Entscheidungen **durchlaufend über drei Ideen hinweg** und verweist im
Text darauf („siehe Entscheidung 8") — deshalb ist dort sogar MD029 abgeschaltet. Ein Schnitt in sechs
Story-Dateien bräche genau diese Querverweise. Die Story trägt dann Frontmatter, User Story und
Akzeptanzkriterien (die sind je Idee ohnehin eigenständig) und verlinkt den Rest.

### Ausformulieren heißt gegen den Code belegen

Der Schritt `idee → ausformuliert` ist der **teure**, und er ist keine Abschrift der Notiz: Ein Ist-Stand
ohne `Datei:Zeile` ist keine Ausformulierung. Im Protokoll vom 2026-07-30 hat genau diese Disziplin fünf
vermeintlich offene Punkte als längst erledigt entlarvt und eine Vorab-Entscheidung revidiert — das ist
der Wertbeitrag der Stufe. Notizen und „offen"-Vermerke verrotten; der Code nicht.

Weil dieser Schritt teuer ist, trägt **schon `idee` eine `prio`**. Sonst müsste `/backlog` raten, welche
von zwei Dutzend Ideen es als Nächstes recherchiert.

Die Prios der Erst-Ernte sind **am 2026-07-30 vom Nutzer im Triage-Durchgang bestätigt** worden (Vorschlag
vorgelegt, Abweichungen übernommen) — sie sind keine Maschinen-Schätzung. Der Weg bleibt so: `/backlog`
schlägt vor, der Mensch entscheidet in einer Runde.

## Frontmatter

```yaml
---
tags: [typ/story, status/gegrillt, bereich/medien, rolle/student]
aliases: [Bildwahl einfrieren]
status: gegrillt          # idee | ausformuliert | gegrillt | geschaetzt | in-arbeit | abgenommen | verworfen
prio: P1                  # P0…P3 — ab 'idee' gesetzt, darf wandern
art: Defekt               # Defekt | Wunsch | Frage | Aufräumen — ab 'idee', geschlossene Liste
groesse: S                # XS…L — erst ab 'geschaetzt'
wo: backend               # backend | frontend | beides | doku
migration: nein           # ab 'geschaetzt': ja | nein | offen
vertragsbruch: ja         # ab 'geschaetzt': ja | nein | offen
quelle: docs/backlog-vokabellernen.md#fund-1   # Pflicht — woher die Idee kam
unverifiziert: true       # nur auf 'idee'
grund: ""                 # nur bei 'verworfen'
ersetzt_durch: []         # nur bei 'verworfen: geteilt'
---
```

`status` ist die *eine* maschinell gelesene Angabe; das Tag `status/…` trägt dasselbe für den
Obsidian-Graphen (Konvention siehe [obsidian.md](../obsidian.md)). Weichen beide voneinander ab, gilt
`status:` — das Tag ist die Kopie.

**Rumpf-Abschnitte** in dieser Reihenfolge (Format aus dem Protokoll vom 2026-07-30, es hat sich bewährt):
`## User Story` · `## Ist-Stand am Code` · `## Die echte Lücke` · `## Offene Punkte` ·
`## Entscheidungen` · `## Akzeptanzkriterien` · `## Schätzung` · `## Verlauf`. Vorlage:
[\_vorlage.md](_vorlage.md).

`## Verlauf` ist der Zeitstempel-Träger: eine Zeile je Bewegung, Datum **aus der Shell** (`date -u`), nie
aus dem Gedächtnis. Auf dem letzten Eintrag rechnet die Alterung (siehe unten) — die Datei-Mtime taugt
dafür nicht, ein Reformat ist keine Bewegung.

## `art`: Defekt, Wunsch, Frage, Aufräumen

Zwei Dinge hängen daran, sonst wäre das Feld Dekoration: **die Reihenfolge** und **die Form der Abnahme.**

| `art` | Was es ist | Wie „fertig" aussieht |
| --- | --- | --- |
| `Defekt` | Etwas verhält sich falsch, und zwar **jetzt** | ein **Regressionstest**, der vorher rot war |
| `Wunsch` | Eine neue Fähigkeit | die Akzeptanzkriterien |
| `Frage` | Prüfauftrag: „gilt das noch?" | eine **belegte Antwort** — `verworfen` ist ein Erfolg |
| `Aufräumen` | Kein Verhalten ändert sich | alles so grün wie vorher |

Bei gleicher `prio` sortiert der Index nach dieser Ordnung: **Defekt vor Frage vor Wunsch vor Aufräumen.**
Ein Defekt wirkt jetzt; ein Prüfauftrag ist billig und kann Arbeit *streichen* (B-16 und B-29 sind
Kandidaten, nach dem Prüfen zu verschwinden); ein Wunsch ist das Produkt; Aufräumen ändert für niemanden
etwas. So wurde B-01 zum P1 — nicht weil es groß ist, sondern weil es heute an echten Kindern wirkt.

**Warum nicht die sechs `RemarkCategory`-Werte** (`Bug, Ui, Code, Content, Idea, Question`)? Weil die zwei
Fragen mischen: „Defekt oder Wunsch?" **und** „welche Fläche?". Die zweite trägt der Bereich schon über `wo`
und die `bereich/…`-Tags. Zwei überlappende Achsen bedeuten, dass eine davon veraltet — und die veraltete
gewinnt. Abbildung beim Befördern einer Anmerkung: `Bug → Defekt`, `Idea`/`Content → Wunsch`, `Ui →` je
nachdem (falsche Beschriftung ist ein Defekt, hässliches Layout ein Wunsch), `Code`/`Question →` meist gar
keine Story, sondern eine Antwort.

`art` bleibt ein **reines Feld ohne Tag-Kopie** — Obsidian liest Frontmatter-Felder direkt. Das
`status/…`-Tag existiert nur, weil im Graphen schon darauf gefiltert wird; ein zweiter kopierter Wert wäre
eine zweite Stelle zum Vergessen.

## Größen-Anker

Eine Größe ohne Bezugsbeispiel ist ein Gefühl. Personentage taugen nicht, wenn der Entwickler eine
Claude-Sitzung ist — darum sind die Stufen an **echten Vorgängen dieses Repos** verankert:

| Größe | Anker |
| --- | --- |
| XS | zwei Sätze in `lib/fieldHelp.ts` plus der E2E, der sie prüft (B-02) |
| S | `childId` aus dem Test-Pfad ziehen (B-01) |
| M | vokabel-basierter Batch-Pfad im `MediaSelector` (B-03) |
| L | eine DB-Umbau-Etappe wie E6 (Löschverhalten explizit + bezahltes Inventar retten) |
| XL | gibt es nicht — dann wird geteilt |

Die Größe allein verschweigt aber, was in *diesem* Repo wirklich kostet. Darum zwei ja/nein-Flags:

- **`migration`** — die Migrationskette wird **neu gefaltet**, nicht verlängert (`SchemaGuardTests` hält
  Länge 1). Jede Schemaänderung zieht das nach sich.
- **`vertragsbruch`** — `Pugling.Contracts` ändert sich **brechend**, also ziehen `Pugling.Client`, das
  Frontend und die `unknown_field`-Guards nach. Ein *additives* Feld ist kein Bruch. In einem „M" ist das
  sonst unsichtbar.

Beide Flags kennen einen dritten Wert: **`offen`** — zulässig genau dann, wenn eine ausdrücklich
zurückgestellte Entscheidung darüber bestimmt (Beispiel B-04: neues Feld für den Tagesdeckel *oder*
Umdeutung von `ItemCount`). Der Index zeigt das als „Migration?" und macht die Unbekannte damit sichtbar,
statt sie als „nein" zu tarnen.

## Teilen und Zusammenlegen

Wird eine Story zu groß (XL) oder stellt sich als Bündel heraus, geht die **alte Id auf `verworfen`** mit
`grund: geteilt` und `ersetzt_durch: [B-41, B-42]`; die neuen Stories tragen `quelle: B-19`. Keine Sub-Ids
(`B-19a`) — sonst bleibt eine leere Hülle stehen, und beim zweiten Teilen entsteht `B-19a1`. Die Spur ist
in beide Richtungen lesbar, wie `Remark.ParentRemarkId`.

## Karten: Vorhaben, die nicht in eine Sitzung passen

`/backlog <id> grillen` holt die Entscheidungen einer Story in **einer** Sitzung ab. Manche Vorhaben
tragen das nicht: die Runde wirft mehr Fragen auf, als sie schließt, weil erst die Antwort auf Frage 3
sichtbar macht, dass es Frage 7 überhaupt gibt. [Teilen](#teilen-und-zusammenlegen) hilft dann noch
nicht — man sieht die Teile ja noch nicht.

Für diesen Fall wird die Story als **Karte** gefahren (Skill `/wayfinder`). Die Karte benennt das Ziel
und hält den Weg dorthin als einzelne **Entscheidungs-Tickets**, die über mehrere Sitzungen eines nach
dem anderen fallen. Sie ist **Planung, nicht Bau**: fertig ist sie, wenn nichts mehr zu entscheiden ist.

**Die Karte ist die Story, nicht ihr Ersatz.** Sie ist eine gewöhnliche `B-<nn>`-Datei nach dem Muster
„Sammel-Story mit Link" (siehe [Kein zweiter Ablageort](#kein-zweiter-ablageort)) und läuft dieselbe
Kette. Ihr Nutzen liegt auf genau einer Stufe: **`ausformuliert → gegrillt`**. Erreicht die Karte
`gegrillt`, steht jeder Ticket-Beschluss als nummerierte Entscheidung in der Story — dieselbe
Eintrittsbedingung wie sonst, nur über mehrere Sitzungen erarbeitet statt in einem Gespräch.

**Wann eine Karte statt einer Grill-Runde?** Nicht nach Größe, sondern nach Sicht: Eine Karte lohnt,
wenn die offenen Punkte voneinander abhängen und man die Kette nicht zu Ende sehen kann. Der Skill hat
den Abbruch selbst eingebaut — findet er beim Kartieren keinen Nebel, ist keine Karte nötig, und die
normale Runde genügt. Im Zweifel erst grillen; die Karte entsteht dann aus dem, was die Runde nicht
geschlossen hat.

Die Tickets stehen **nicht** im Index. Sie sind Zwischenstände einer Entscheidungsfindung, keine offene
Arbeit — im Index wären sie Rauschen vor dem Signal, und keine der sechs Stufen passt auf ein Ticket
(es wird nie `geschaetzt` und nie `abgenommen`). Sichtbar bleibt die Karte, und die trägt den Zustand.

### Wayfinding operations

Diese Abbildung liest `/wayfinder`: Der Skill ist tracker-agnostisch und erwartet unter dieser
Überschrift, wie *dieses* Repo seine Karten ausdrückt. Sein mitgelieferter Vorgabe-Tracker
(`.scratch/<effort>/`) gilt hier **nicht** — er wäre der zweite Ablageort.

| Beim Skill | Hier |
| --- | --- |
| Map | die Story selbst, `docs/backlog/B-<nn>-<slug>.md` |
| Destination | `## Karte` → `### Ziel` |
| Notes | `## Karte` → `### Notizen` |
| Decisions so far | `## Entscheidungen` — der bestehende Abschnitt, nummeriert, mit Begründung und Kosten |
| Not yet specified | `## Offene Punkte` — was noch zu unscharf für ein Ticket ist |
| Out of scope | `## Karte` → `### Außerhalb des Ziels` |
| Child ticket | `docs/backlog/karten/B-<nn>/T-<nn>-<slug>.md`, ab `T-01` |
| Blocking | Zeile `Blockiert durch: T-01, T-04` im Ticket-Kopf; frei, sobald alle genannten `entschieden` sind |
| Frontier | die offenen, unblockierten, nicht beanspruchten Tickets; die kleinste Nummer gewinnt |
| Claim | `Status: beansprucht` setzen und **speichern, bevor** die Arbeit beginnt |
| Resolve | `## Antwort` ins Ticket, `Status: entschieden`, dann die Entscheidung in die Karte |

Ticket-Format:

```markdown
# T-03 · Wie wird der Tagesdeckel getragen?

Status: offen           <!-- offen | beansprucht | entschieden -->
Typ: grilling           <!-- research | prototype | grilling | task -->
Blockiert durch: T-01

## Frage

<die eine Entscheidung, die dieses Ticket schließt — auf eine Sitzung zugeschnitten>

## Antwort

<erst beim Entscheiden; der Kern wandert danach in die `## Entscheidungen` der Karte>
```

Zwei Abweichungen von der Vorgabe des Skills, beide bewusst:

- **Die Karte gistet nicht, sie trägt die Entscheidung ganz.** Der Skill will die Entscheidung nur an
  einer Stelle — im Ticket — und auf der Karte bloß angerissen. Hier gewinnt die Eintrittsbedingung von
  `gegrillt`: sie verlangt Begründung **und** Kosten in der Story. Die Story muss allein lesbar bleiben,
  wenn die Tickets längst kalt sind; das Ticket behält den Verlauf (Alternativen, Verworfenes) und wird
  von der Entscheidung verlinkt.
- **Ein Ticket je Sitzung**, Recherche-Tickets ausgenommen — die Regel kommt vom Skill und bleibt. Zwei
  Entscheidungen in einem Rutsch heißt, dass eine davon nicht erarbeitet wurde; dieselbe Regel gilt für
  die Stufen (`/backlog <id>` schiebt genau eine).

Die vier Ticket-Typen greifen je einen Skill: `research`, `prototype`, `grilling` (der Normalfall,
zusammen mit `domain-modeling`) und `task` — Handarbeit, die eine Entscheidung erst möglich macht. Ein
`grilling`- und ein `prototype`-Ticket fallen **nur im Gespräch**; der Agent beantwortet sie nicht
selbst.

## Hygiene: der Bereich darf nicht nur wachsen

Ein Backlog, der nur wächst, ist der Zettelberg, den er ersetzen sollte — nur mit YAML davor. Darum meldet
`/backlog` ungefragt jede `idee`-Story, die **über 90 Tage unbewegt** ist, und legt je eine Entscheidung
vor: Prio bestätigen oder verwerfen.

Das **warnt und blockt nicht**, wie das Kontext-Budget-Tor. Eine WIP-Grenze auf `idee` wäre die strengere
Variante und ist bewusst verworfen: sie bremst das Erfassen, und eine Idee, die wegen eines Zählers nicht
notiert wird, ist verloren. (Aus demselben Grund steht `RemarkCategory` beim Erfassen per Vorgabe auf
`Unspecified`.)

## Kein zweiter Ablageort

- Ein **gepflegtes Plandokument bleibt Quelle der Wahrheit.** Für Vorhaben mit eigenem Plan
  ([db-struktur-umbau-plan.md](../db-struktur-umbau-plan.md), [translate.md](../translate.md),
  [lehrer-konto-plan.md](../lehrer-konto-plan.md)) trägt der Bereich **eine Sammel-Story mit Link**, keine
  Kopie der Etappen. Sie führt keine Etappen-Zustände und wird erst `abgenommen`, wenn im Plandokument
  keine offene Etappe mehr steht.
- **`pm-loop`** erzeugt Ideen am laufenden Produkt (Rollen-Feedback) und legt sie hier als `idee` ab; seine
  Prio-Tabelle ist eine *datierte Momentaufnahme* mit Story-Ids. Die dauerhafte Liste ist dieser Bereich —
  Sitzungslogs bleiben Protokolle. Rollen-Feedback und Abnahme-Gate bleiben bei `pm-loop`.
- **Ticket-Dateien einer [Karte](#karten-vorhaben-die-nicht-in-eine-sitzung-passen)** sind kein zweiter
  Ablageort, obwohl sie eigene Dateien sind: Sie tragen keinen Story-Zustand, sondern die Arbeitsblätter
  **einer** Stufe. Der Zustand steht in der Karte, und mit ihr endet die Kette. Fällt die letzte
  Entscheidung, ist der Ordner Archiv — nichts, was noch gepflegt werden müsste.
- **Anmerkungen** (`api/v1/remarks`) sind der Eingang aus dem laufenden Betrieb: eine Anmerkung mit
  `Category = Idea`, die auf `Planned` geht, wird auf **Vorschlag** zur Story (`quelle: remark #NN`); die
  Anmerkung bekommt einen `Assistant`-Kommentar mit der Story-Id.

## Index

<!-- backlog-index:start -->
<!-- Erzeugt von .claude/scripts/backlog-index.sh — nicht von Hand pflegen. -->

### Offen (69)

| Id | Story | Art | Stufe | Prio | Größe | Wo | Kostet |
| --- | --- | --- | --- | --- | --- | --- | --- |
| [B-07](B-07-db-umbau-restetappen.md) | DB-Struktur-Umbau: der offene Betriebsschritt | Aufräumen | `geschaetzt` | P1 | XS | backend | — |
| [B-60](B-60-flags-enum-im-dokument.md) | Das Vertragsdokument verbietet einen `SchoolTypes`-Wert, den Server und Frontend täglich austauschen | Defekt | `geschaetzt` | P2 | S | backend | — |
| [B-66](B-66-buchstabenkaestchen-trennzeichen.md) | Das Buchstabenkästchen lässt Leer- und Satzzeichen tippen, die schon feststehen | Defekt | `geschaetzt` | P2 | M | beides | — |
| [B-96](B-96-showboth-stufe-ohne-mechanik.md) | „Beide zeigen (Kennenlernen)" ist eine Beschriftung ohne eigene Stufe | Defekt | `ausformuliert` | P2 | — | — | — |
| [B-97](B-97-unique-index-ohne-vorpruefung.md) | Zwei Schreibpfade laufen ungeprüft in einen Unique-Index und antworten mit 500 | Defekt | `ausformuliert` | P2 | — | — | — |
| [B-98](B-98-idempotenter-link-post-luegt.md) | Drei idempotente Schreibpfade antworten mit erfundenen Werten, zwei davon mit `201 Created` | Defekt | `ausformuliert` | P2 | — | — | — |
| [B-99](B-99-kaufhistorie-endet-lautlos.md) | Die Kaufhistorie des Kindes endet lautlos bei 50 Zeilen | Defekt | `ausformuliert` | P2 | — | — | — |
| [B-31](B-31-geraete-vorbehalt-klang.md) | Geräte-Vorbehalt: Klang und Haptik am echten Handy gegenhören | Frage | `geschaetzt` | P2 | XS | frontend | — |
| [B-48](B-48-anonyme-registrierung-produktion.md) | Anonyme Registrierung ist auch in Produktion offen | Frage | `geschaetzt` | P2 | S | backend | — |
| [B-11](B-11-uebungen-veroeffentlichen.md) | Übungen ausdrücklich veröffentlichen | Wunsch | `geschaetzt` | P2 | S | frontend | — |
| [B-13](B-13-fach-kapitel-eigentum.md) | Fach- und Kapitel-Eigentum | Wunsch | `geschaetzt` | P2 | M | backend | Migration |
| [B-18](B-18-auto-lehrplan-generator.md) | Lehrplan automatisch aus gefilterten Übungen bauen | Wunsch | `geschaetzt` | P2 | S | beides | — |
| [B-19](B-19-schuelerprofil-ki-lehrplan.md) | Schülerprofil-getriebener KI-Lehrplan | Wunsch | `geschaetzt` | P2 | M | backend | — |
| [B-39](B-39-supervisor-dashboard.md) | Supervisor-Dashboard über die Kinder | Wunsch | `geschaetzt` | P2 | L | beides | — |
| [B-45](B-45-creator-punkte-empfehlung.md) | Die Punkte-Empfehlung des Creators soll der Supervisor übernehmen können | Wunsch | `geschaetzt` | P2 | S | beides | — |
| [B-46](B-46-interessenbasierte-uebungen.md) | Übungen entstehen für ein Interessenprofil, nicht für ein bestimmtes Kind | Wunsch | `geschaetzt` | P2 | L | backend | Migration |
| [B-50](B-50-kind-beschreibt-sich-selbst.md) | Das Kind beschreibt sich selbst: Interessen in einem geführten Prozess | Wunsch | `geschaetzt` | P2 | L | beides | — |
| [B-63](B-63-lehrwerk-hierarchie.md) | Das Lehrwerk ist eine Ebene aus Freitext, gebraucht wird eine Hierarchie mit Listen | Wunsch | `geschaetzt` | P2 | L | beides | Migration + Vertrag |
| [B-67](B-67-fachlehrer-aus-lehrwerk.md) | Der Fachlehrer fragt nach Fach und Sprachen, die im gewählten Lehrwerk längst stehen | Wunsch | `geschaetzt` | P2 | S | frontend | — |
| [B-27](B-27-testsuite-grenzfaelle.md) | Die Grenzen des `ScoringService` als Tabelle statt als Flow | Aufräumen | `geschaetzt` | P2 | S | backend | — |
| [B-44](B-44-grundprinzip-rollennamen.md) | Grundprinzip auf Supervisor/Student umschreiben — „Vater" ist keine Ebene | Aufräumen | `geschaetzt` | P2 | XS | doku | — |
| [B-58](B-58-assistent-e2e.md) | Der Lehrplan-Assistent hat keinen Durchstich | Aufräumen | `geschaetzt` | P2 | S | frontend | — |
| [B-56](B-56-problemdetails-required-extensions.md) | `ProblemDetails` fordert im Schema ein Feld, das es nicht beschreibt | Defekt | `geschaetzt` | P3 | S | backend | — |
| [B-57](B-57-beispielkatalog-schreib-lese-rennen.md) | Im Testlauf lesen und schreiben zwei Stellen gleichzeitig dieselbe Katalogdatei | Defekt | `geschaetzt` | P3 | S | backend | — |
| [B-61](B-61-reste-der-schreib-primitiven-runde.md) | Zwei Reste aus der Schreib-Primitiven-Runde | Defekt | `geschaetzt` | P3 | S | frontend | — |
| [B-62](B-62-reste-aus-dem-b37-review.md) | Drei Reste aus dem B-37-Review (Sohn-Arcade) | Defekt | `geschaetzt` | P3 | S | beides | — |
| [B-72](B-72-birkenbihl-dekodierung-paarfelder.md) | Die Birkenbihl-Dekodierung trägt zwei Trennzeichen in einem Feld | Defekt | `geschaetzt` | P3 | S | frontend | — |
| [B-84](B-84-api-beispiele-behaupten-unerreichbarkeit.md) | Die API-Beispiele behaupten Unerreichbarkeit, wo nur nichts mitgeschnitten wurde | Defekt | `geschaetzt` | P3 | S | doku | — |
| [B-89](B-89-positionsliste-haengt-report-aus.md) | Die Positionsliste hängt bei jeder Änderung den aufgeklappten Report aus | Defekt | `geschaetzt` | P3 | L | frontend | — |
| [B-93](B-93-birkenbihl-einstellungen-ohne-wirkung.md) | Zwei Birkenbihl-Einstellungen, die lautlos nichts tun | Defekt | `idee` | P3 | — | — | — |
| [B-17](B-17-birkenbihl-sprachcodes.md) | Sprachcode-Normalisierung bei der Vokabel-Dekodierung | Frage | `geschaetzt` | P3 | XS | frontend | — |
| [B-103](B-103-idempotenzschluessel-und-etag.md) | Prüfauftrag: Brauchen `Idempotency-Key` und ETag/`If-Match` in dieser App einen Platz? | Frage | `ausformuliert` | P3 | — | — | — |
| [B-03](B-03-lueckensaetze-mit-bild.md) | Lückensätze mit Bild als Vokabel-Vertiefung | Wunsch | `geschaetzt` | P3 | M | backend | — |
| [B-09](B-09-lehrer-hausaufgaben.md) | Lehrer erteilt Hausaufgaben: zuweisen mit Frist, ohne Betreuungsauftrag | Wunsch | `geschaetzt` | P3 | L | backend | Migration |
| [B-12](B-12-uebungen-kuratieren.md) | Geteilte Übungen bewerten und kuratieren | Wunsch | `geschaetzt` | P3 | M | beides | Migration |
| [B-15](B-15-testmodus-weitere-typen.md) | Vorschau für die nicht-prüfbaren Übungstypen | Wunsch | `geschaetzt` | P3 | S | beides | — |
| [B-16](B-16-positions-formular-umfang.md) | Prüfauftrag: deckt das Positions-Edit-Formular alle Felder ab? | Wunsch | `geschaetzt` | P3 | S | frontend | — |
| [B-20](B-20-ki-supervisor-agent.md) | KI-Supervisor-Agent (Teil D) — erster Schritt: Übungen einem Plan zuweisen | Wunsch | `geschaetzt` | P3 | L | backend | — |
| [B-21](B-21-ki-creator-foerdermodus.md) | KI-Creator: Fördermodus (`--use-weak`) sprachrein und mit Tests absichern | Wunsch | `geschaetzt` | P3 | S | backend | — |
| [B-22](B-22-unit-stoffnotizen-llm.md) | Unit-Stoffnotizen LLM-gestützt befüllen | Wunsch | `geschaetzt` | P3 | M | backend | Migration |
| [B-23](B-23-uebungstyp-plugins-dll.md) | Übungstyp-Plugins als externe DLLs (Stufe 2) | Wunsch | `geschaetzt` | P3 | L | backend | — |
| [B-28](B-28-login-name-sequenziell.md) | Sequenzielle IDs als Login-Name | Wunsch | `geschaetzt` | P3 | S | backend | — |
| [B-35](B-35-karten-umdrehen-animation.md) | Karten drehen sich beim Aufdecken um | Wunsch | `geschaetzt` | P3 | S | frontend | — |
| [B-36](B-36-motivations-animationen-teilziele.md) | Motivations-Animationen bei erreichten Teilzielen | Wunsch | `geschaetzt` | P3 | S | frontend | — |
| [B-64](B-64-textbook-vs-series.md) | Das Lehrwerk gibt es zweimal: einmal als Freitext am Kind, einmal katalogisiert | Wunsch | `geschaetzt` | P3 | M | beides | — |
| [B-68](B-68-vater-web-responsive.md) | Das Vater-Web hat keinen einzigen eigenen Breakpoint | Wunsch | `geschaetzt` | P3 | M | frontend | — |
| [B-71](B-71-inline-vokabelliste-ohne-varianten.md) | Die Inline-Vokabelliste im Übungs-Editor kann keine gleichwertigen Übersetzungen anlegen | Wunsch | `geschaetzt` | P3 | M | beides | — |
| [B-85](B-85-i18n-infrastruktur-sohn-arcade-englisch.md) | i18n-Infrastruktur + Sohn-Arcade auf Englisch (erste Teilstufe der Mehrsprachigkeit) | Wunsch | `geschaetzt` | P3 | L | frontend | — |
| [B-86](B-86-uebungstyp-manifest-anzeigenamen-schluessel.md) | Das Übungstyp-Manifest liefert Anzeigenamen als Daten, nicht als Schlüssel | Wunsch | `geschaetzt` | P3 | M | beides | Vertrag |
| [B-25](B-25-vite-pwa-peer-konflikt.md) | Peer-Konflikt `vite-plugin-pwa` ↔ `vite@8` lösen | Aufräumen | `geschaetzt` | P3 | XS | frontend | — |
| [B-32](B-32-father-tabellenname.md) | `Father` heißt noch `Father`, obwohl die Zeile `Adult` ist | Aufräumen | `geschaetzt` | P3 | S | backend | — |
| [B-47](B-47-deploy-artefakt-smoke.md) | Startet das veröffentlichte Artefakt überhaupt? | Aufräumen | `geschaetzt` | P3 | S | backend | — |
| [B-49](B-49-sohn-app-schreib-primitive.md) | Die Sohn-App benutzt die geteilten Schreib-Primitive nicht | Aufräumen | `geschaetzt` | P3 | S | frontend | — |
| [B-51](B-51-admin-rolle-dokumentieren.md) | Die Admin-Rolle kommt in keinem Rollen-Dokument vor | Aufräumen | `geschaetzt` | P3 | XS | doku | — |
| [B-55](B-55-wegwerf-dateien-aufraeumen.md) | Die Tests räumen ihre Wegwerf-Dateien nicht weg | Aufräumen | `geschaetzt` | P3 | S | beides | — |
| [B-59](B-59-status-strings-ohne-werteliste.md) | Zwei Antwortfelder tragen einen Status als nackten `string` | Aufräumen | `geschaetzt` | P3 | S | beides | Vertrag |
| [B-74](B-74-editor-zeilen-typisieren.md) | Die Zeilen des Übungs-Editors sind `Record<string, any>` | Aufräumen | `geschaetzt` | P3 | M | frontend | — |
| [B-83](B-83-loesungsfeld-regel-residenter-kontext.md) | Die Lösungsfeld-Regel steht nur als Kommentar am Wächter | Aufräumen | `geschaetzt` | P3 | XS | doku | — |
| [B-88](B-88-scoring-uhrzeit-am-timeprovider.md) | Die Punkte-Uhrzeit kommt von der Wanduhr, nicht vom `TimeProvider` | Aufräumen | `geschaetzt` | P3 | XS | backend | — |
| [B-100](B-100-vertragsdokument-unterdeklariert.md) | Das Vertragsdokument verschweigt 401, `X-Total-Count` und 24 Operationsnamen | Aufräumen | `ausformuliert` | P3 | — | — | — |
| [B-101](B-101-fehlercodes-und-drei-waechter.md) | Drei generische Fehlercodes ersetzen — und die drei Wächter, die daraus reif geworden sind | Aufräumen | `ausformuliert` | P3 | — | — | — |
| [B-102](B-102-token-vorgabewert-regel-schaerfen.md) | Die Token-Regel im Startkontext ist zu weit formuliert — 55 Signaturen „verstoßen" gegen eine Compilerregel | Aufräumen | `ausformuliert` | P3 | — | — | — |
| [B-95](B-95-stufenwaechter-haengt-am-include.md) | Die Stufenprüfung beim PATCH einer Position hängt an einem `Include`, das niemand einfordert | Aufräumen | `ausformuliert` | P3 | — | — | — |
| [B-04](B-04-adaptiver-vokabel-pool.md) | Adaptiver Vokabel-Pool je Position | Wunsch | `geschaetzt` | P4 | M | backend | Migration? |
| [B-90](B-90-server-sprachfeld.md) | Server-Sprachfeld an `Adult`/`Child` | Wunsch | `idee` | P4 | — | — | — |
| [B-91](B-91-vater-web-extraktion-englisch.md) | Vater-Web-Textkorpus auf Übersetzungsschlüssel umstellen (Englisch) | Wunsch | `idee` | P4 | — | — | — |
| [B-92](B-92-franzoesisch-zweite-zielsprache.md) | Französisch als zweite Zielsprache (Sohn-Arcade + Vater-Web) | Wunsch | `idee` | P4 | — | — | — |
| [B-05](B-05-buchstaben-tausch.md) | Buchstaben-Tausch-Eingabe (Anagramm) | Wunsch | `geschaetzt` | P5 | M | beides | — |
| [B-06](B-06-cloze-preview-bild.md) | Cloze-Vorschau kann kein Bild zeigen | Wunsch | `geschaetzt` | P6 | XS | backend | — |

<details>
<summary>Abgenommen (25)</summary>

| Id | Story | Art | Stufe | Prio | Größe | Wo | Kostet |
| --- | --- | --- | --- | --- | --- | --- | --- |
| [B-01](B-01-bildwahl-einfrieren.md) | Abschlusstest friert Bildwahlen ein, die er nie zeigt | Defekt | `abgenommen` | P1 | S | backend | Vertrag |
| [B-02](B-02-itemcount-hilfetext.md) | Der Hilfetext erklärt `ItemCount` falsch herum | Defekt | `abgenommen` | P2 | XS | frontend | — |
| [B-08](B-08-xml-docs-englisch.md) | XML-Doc-Kommentare im Backend auf Englisch übersetzen | Aufräumen | `abgenommen` | P3 | S | doku | — |
| [B-10](B-10-zeitfenster-pro-kind.md) | Zeitfenster (Punkte-Faktor) je Pflicht statt global | Wunsch | `abgenommen` | P2 | M | beides | Migration |
| [B-26](B-26-e2e-in-ci.md) | Der E2E-Nachtlauf ist rot – und niemand erfährt es | Defekt | `abgenommen` | P1 | S | frontend | — |
| [B-37](B-37-uebung-abbruch-unvollendet.md) | Abgebrochene Runden: Pflicht härten, Klausur deckeln | Defekt | `abgenommen` | P1 | M | beides | — |
| [B-40](B-40-client-routen-waechter.md) | Routen aus `Pugling.Client` gegen das OpenAPI-Dokument halten | Aufräumen | `abgenommen` | P3 | XS | backend | — |
| [B-41](B-41-produktions-startup-smoke.md) | Der Produktionspfad des Starts ist der einzige ohne Test | Aufräumen | `abgenommen` | P2 | S | backend | — |
| [B-42](B-42-openapi-typen-generieren.md) | TypeScript-Typen aus dem OpenAPI-Dokument erzeugen statt von Hand pflegen | Aufräumen | `abgenommen` | P2 | M | beides | — |
| [B-43](B-43-frontend-komponententests.md) | Die Doppelklick-Lücke in `useAction` – und die fehlende Ebene für unsichtbare Zusicherungen | Defekt | `abgenommen` | P3 | M | frontend | — |
| [B-52](B-52-testabdeckung-paket.md) | Sammel-Story: das Testabdeckungs-Paket | Aufräumen | `abgenommen` | P2 | L | beides | — |
| [B-53](B-53-wizard-doppelklick.md) | Zwei Klicks im Lehrplan-Assistenten legen zwei Kinder und zwei Pläne an | Defekt | `abgenommen` | P2 | S | frontend | — |
| [B-54](B-54-objectivecard-schreib-primitive.md) | Fünf Knöpfe im Vater-Web gehen an den Schreib-Primitiven vorbei | Defekt | `abgenommen` | P2 | S | frontend | — |
| [B-65](B-65-vokabel-mehrere-uebersetzungen.md) | Eine Vokabel mit zwei richtigen Übersetzungen wertet eine davon falsch | Defekt | `abgenommen` | P1 | M | beides | Migration |
| [B-69](B-69-wiederhol-felder-alternativen.md) | Kommagetrennte Sammelfelder: einer davon nimmt gar keine zweite Alternative an | Defekt | `abgenommen` | P2 | M | frontend | — |
| [B-70](B-70-selbsteinschaetzung-nur-primaerloesung.md) | Die Selbsteinschätzung zeigt nur die primäre Übersetzung | Defekt | `abgenommen` | P2 | S | beides | — |
| [B-73](B-73-auswahl-feld-ohne-wirkung.md) | Das Auswahl-Feld verspricht Multiple-Choice, das Kind bekommt Freitext | Defekt | `abgenommen` | P2 | S | beides | — |
| [B-75](B-75-lese-hoerverstehen-ohne-inhalt.md) | Lese- und Hörverstehen kommen ohne ihren Inhalt beim Kind an | Defekt | `abgenommen` | P1 | M | beides | Vertrag |
| [B-76](B-76-lueckentext-karte-ohne-luecke.md) | Der Lückentext sagt dem Kind nicht, welche Lücke gemeint ist | Defekt | `abgenommen` | P1 | M | beides | — |
| [B-77](B-77-liste-menge-als-folge.md) | Beim Spielen wird eine ungeordnete Liste als Folge bewertet | Defekt | `abgenommen` | P1 | M | beides | Vertrag |
| [B-78](B-78-birkenbihl-dekodierung-erreicht-kind-nicht.md) | Die Birkenbihl-Dekodierung erreicht das Kind nicht | Defekt | `abgenommen` | P2 | M | beides | — |
| [B-79](B-79-position-stufe-unvalidiert.md) | Die Stufe einer Position wird gegen nichts geprüft | Defekt | `abgenommen` | P2 | S | backend | — |
| [B-80](B-80-tags-geben-fremde-konfiguration-preis.md) | Das Kind kann die Lösungen jeder Übung lesen | Defekt | `abgenommen` | P1 | S | backend | Vertrag |
| [B-81](B-81-vokabel-tags-geben-uebersetzungen-preis.md) | Über die Vokabel-Tags kann ein Kind jede Übersetzung des Stores lesen | Defekt | `abgenommen` | P1 | S | backend | — |
| [B-82](B-82-positions-report-gibt-loesungen-preis.md) | Über den Positions-Report kann ein Kind die Lösung jeder Karte lesen | Defekt | `abgenommen` | P1 | M | beides | Vertrag |

</details>

### ⚠ Stufe behauptet, Datei belegt nicht

Diese Stories tragen einen `status`, dessen Eintrittsbedingung in der Datei nicht
vollständig steht. Entweder nachtragen oder die Stufe zurücknehmen.

| Id | Stufe | Fehlt |
| --- | --- | --- |
| [B-93](B-93-birkenbihl-einstellungen-ohne-wirkung.md) | `idee` | `unverifiziert: true` |

<details>
<summary>Verworfen (8)</summary>

| Id | Story | Grund |
| --- | --- | --- |
| [B-14](B-14-learngoal-belohnung.md) | Idempotente Belohnung, wenn ein Lernziel erreicht ist — **gegenstandslos** | erfüllt durch KeyResult/ObjectiveRewardService (DB-Umbau E13) |
| [B-24](B-24-frontend-unknown-field.md) | Frontend gegen `unknown_field` durchspielen | "Alle 34 untypisierten Schreib-Rümpfe in api.ts sind Feld für Feld gegen die Contracts-DTOs |
| [B-29](B-29-altmigration-transaktional.md) | Prüfauftrag: nicht-transaktionale Altmigration | gegenstandslos seit der Neufaltung auf InitialCreate — die Migrationskette besteht (SchemaGuardTests, Tor G1b) aus genau einer Migration (Data/Migrations/20260803223259_InitialCreate.cs), keine Altmigration ist mehr vorhanden, die nicht-transaktional laufen könnte. Program.cs (Zeilen 471–484) behandelt eine DB der alten Kette ohnehin nicht als Migrationsfall, sondern wirft eine handlungsfähige Fehlermeldung — ein Upgrade-Pfad existiert bewusst nicht. |
| [B-30](B-30-i18n-rest.md) | i18n-Rest: Ledger-Texte, Platzhalter, interne Exceptions | "Der vermutete Rest existiert nicht: kein einziger `///`- oder `//`-Doku-Kommentar im Backend ist |
| [B-33](B-33-azure-publish-profile.md) | Azure-Secret `AZURE_WEBAPP_PUBLISH_PROFILE` fehlt | bewusste Nicht-Aufgabe für eine Code-Sitzung (Nutzer-Entscheidung) |
| [B-34](B-34-sitzungsbonus-dauer.md) | „Dauer durchgehend gelernt" als eskalierender Sitzungs-Bonus | durch MinutesPracticed-Missionen abgelöst |
| [B-38](B-38-mehrsprachige-oberflaeche.md) | Mehrsprachige Oberfläche (Deutsch, Englisch, Französisch) | "geteilt — ein Programm, keine Story (Entscheidung 8); siehe die recherchierte Grundlage unten und → [B-85, B-86, B-87] |
| [B-87](B-87-vater-web-franzoesisch-server-sprachfeld.md) | Rest des Mehrsprachigkeits-Programms: Vater-Web, Französisch, Server-Sprachfeld | "geteilt — selbst noch ein Programm (Entscheidung 1); siehe die recherchierte Grundlage oben und die → [B-91, B-92, B-90] |

</details>

<!-- backlog-index:end -->

---

**Verwandt:** [obsidian.md](../obsidian.md) · [backlog-vokabellernen.md](../backlog-vokabellernen.md) ·
[anmerkungen-plan.md](../anmerkungen-plan.md) · [endpunkt-beziehungen.md](../endpunkt-beziehungen.md)
