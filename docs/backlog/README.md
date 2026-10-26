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
| `abgenommen` | Verifikation **belegt**: echte Testzahl, gelaufener `/smoke-test` bzw. E2E, bei Backend-Änderungen zusätzlich `pugling-reviewer`, **Commit(s) genannt** — und der **Rollengang** an der laufenden App (`pm-loop` Step 6; eine E2E, die den Weg fährt, zählt als Rollengang) oder, wenn er ausfiel, eine Zeile im `## Verlauf`, die das ausdrücklich benennt. | Entwickler |
| `verworfen` | Begründung im Feld `grund`. Bei „geteilt" zusätzlich `ersetzt_durch`. | Mensch |

`verworfen` ist ein **Ziel, kein Scheitern.** Mehrere Punkte im Projekt sind bewusste Nicht-Ziele; als
`verworfen` mit Grund tauchen sie nie wieder als offene Frage auf, statt in jeder Sichtung erneut
Aufmerksamkeit zu kosten.

**Gegenstandslos heißt `verworfen`, nicht `abgenommen`.** Fällt eine Story weg, weil ein anderer Umbau ihr
den Boden entzogen hat, ist nichts gebaut und nichts verifiziert — `abgenommen` behauptete beides und würde
in einem halben Jahr wie eine gelieferte Funktion gelesen. Der Grund gehört ins Feld `grund` („gegenstandslos
durch E13"). Das ist passiert, bevor dieser Satz hier stand: der Wächter hat es an neun fehlenden Belegen
gemeldet.

### `wartet_auf`: fertig ist nicht dasselbe wie in Arbeit

`in-arbeit` trug zwei Bedeutungen, und die zweite war unsichtbar: „wird gerade gebaut" und „fertig
gebaut, belegt, hängt nur noch an einem Schritt außerhalb des Repos". Am 2026-08-05 lagen **vier**
Stories in der zweiten (B-110, B-111, B-114, B-115 — die Reviewer waren an sechs serverseitigen `529`
gescheitert), und keine Liste sagte es. Wer die Sitzung danach aufnahm, hätte sie für unfertige Arbeit
gehalten.

```yaml
wartet_auf: frontend-reviewer      # kurz und konkret; leer, sobald der Schritt getan ist
```

Das Feld ist **stufenunabhängig** — es sammelt alles, was ohne Zutun von außen nicht weitergeht, und
genau das macht es nützlich: der Index führt es als eigenen Abschnitt, und dort steht seit dem ersten
Lauf, warum das einzige **P1** des Bereichs nie wandert (B-07 wartet auf einen Handgriff an der
Azure-Instanz) und dass B-31 ohne ein echtes Handy nicht zu beantworten ist. Beides war vorher nur
Prosa in den Stories.

Es ist **keine** Eintrittsbedingung und ändert keine Stufe. Es beantwortet die eine Frage, die eine
Stufe nicht beantworten kann: *liegt das an mir oder an jemand anderem?*

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

### Der Rollengang fällt am leichtesten weg — und kostet am meisten

Von den Belegen, die `abgenommen` verlangt, ist der Rollengang der einzige, den **kein Tor** erzwingt:
Testzahl, Reviewer und Commit stehen schwarz auf weiß da, „hat sich jemand als Sohn hingesetzt?" nicht.
Genau deshalb steht er seit dem 2026-08-05 in der Eintrittsbedingung, und zwar mit einer *sichtbaren*
Ausnahme statt eines stillen Weglassens.

Der Anlass ist gemessen, nicht befürchtet. Die autonome Bau-Runde vom 2026-08-05 hat vierzehn Stories
abgenommen: **13 von 14** mit Reviewer, **8 von 14** mit `/smoke-test` oder E2E, **0 von 14** mit einem
Rollengang. Der Code-Review am Tag danach fand zwei echte Defekte — beide in Stories dieser Runde
([B-96](B-96-showboth-stufe-ohne-mechanik.md), [B-66](B-66-buchstabenkaestchen-trennzeichen.md)). B-96
trägt vier Reviewer-Erwähnungen, eine rote Probe und eine grüne Suite; die neue Anzeigenurstufe machte
die Position für das Kind trotzdem unspielbar (kein Knopf) und buchte ihm nachts Malus-Münzen ab. Ein
halbminütiger Gang als Sohn hätte das gefunden. Der Reviewer nicht — er beantwortet „ist der Code
richtig?", nicht „kann das Kind spielen?".

Im Gegenbeispiel derselben Woche hat der Rollengang genau das geleistet: B-106s Sprint 1 war grün und
ohne Reviewer-Blocker, und *dann* konnte der Creator keine Übung mehr anlegen
([Protokoll](../pm-sitzung-2026-08-04.md), „Re-Review gegen die echte laufende App"). Der Unterschied
zwischen den beiden Runden ist nicht die Sorgfalt beim Bauen, sondern dieser eine Schritt.

**Eine E2E, die den Weg fährt, ist der Rollengang** — und wo das geht, ist sie die bessere Wahl: sie
treibt eine echte Oberfläche in einem echten Browser gegen einen echten Server, nur wiederholt sie sich,
statt einmal stattzufinden. Vorbild `frontend/e2e/shop-verlauf.spec.ts` (B-110): sie fährt „Verlauf
ansehen → kaufen → Verlauf ansehen", also genau die Reihenfolge, in der der Fehler auftrat. Zwei Dinge
bleiben beim Menschen bzw. beim Komponententest, und sie zu benennen ist eine *argumentierte* Ausnahme,
keine Lücke: das sinnliche Urteil („fühlt sich das richtig an", Klang, Animation) und Fehlerzustände, die
einen künstlich kaputten Server bräuchten.

**Wenn der Reviewer nicht laufen darf, bleibt die Story auf `in-arbeit`.** Die Eintrittsbedingung nennt
ihn, und eine Sitzung kann die Regel tragen, keine Agenten unaufgefordert zu starten. Dann ist der
ehrliche Ausgang: alles andere belegt, Stufe `in-arbeit`, eine `## Verlauf`-Zeile mit dem Grund — und die
Freigabe beim Nutzer erfragen. Nicht erlaubt ist, den eigenen Blick auf den Diff „das Review" zu nennen;
ein Selbst-Check ist der schwächere Beleg und muss, wenn er überhaupt zählt, als solcher beschriftet sein.
So gelaufen am 2026-08-05 bei B-110/B-111.

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
entgangen_bei: []         # nur bei 'art: Defekt' — welche ABGENOMMENE Story ihn durchgelassen hat
nachgeschaut: ""          # nur bei 'abgenommen' — Datum des Blicks NACH der Abnahme (auch ohne Fund!)
wartet_auf: ""            # haengt an einem Schritt ausserhalb des Repos (Mensch, Geraet, Werkzeug)
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

### Ein Fund beim Bauen wird eine eigene Story, nicht ein Anhang an die laufende

Beim Verifizieren fällt regelmäßig etwas auf, das *nebenan* liegt — dieselbe Fehlerklasse eine Ebene höher,
dieselbe Zeile in einem zweiten Controller. Die Regel: **eigene Story, wenn das Ziel der laufenden ohne sie
erfüllt ist**, mit einem Satz in der neuen Story, warum sie nicht mitgenommen wurde. Belege: B-97 → B-104
(dieselbe Unique-Index-Klasse in einem anderen Dienst) und B-110 → B-113 (dieselbe veränderliche Sortierung
in der Vater-Sicht, gefunden bei der Frage „wo steht diese Zeile noch?").

Beide Reflexe daneben kosten: **mitschlucken** heißt, dass die laufende Story kein Ende hat und ihre
Akzeptanzkriterien nichts mehr bedeuten; **liegenlassen** verliert die teuerste Sorte Wissen — das, was man
nur findet, während man ohnehin genau hinsieht.

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

### Autonomer Modus (Opt-in je Vorhaben)

Die Regeln „nur im Gespräch" und „ein Ticket je Sitzung" sind der **Standard**, kein Naturgesetz. Auf
**ausdrücklichen Wunsch des Nutzers, je Vorhaben neu erteilt**, kann eine Karte den gesamten Zyklus
ohne Dialog-Gate durchlaufen: der PM (`pm-loop`) grillt jedes Ticket selbst — mit Begründung und Kosten
wie sonst auch, nur ohne Rückfrage —, schätzt, baut, lässt `pugling-reviewer`/`frontend-reviewer`
laufen, prüft gegen die drei Rollen (`creator`/`supervisor`/`student`-Skills gegen die echte laufende
App) und wiederholt das, bis alle drei Rollen zufrieden sind oder auf einem benannten
Mensch-/Geräte-Check ruhen (dieselbe Abnahme-Regel wie in `pm-loop` Step 6). Mehrere kurze,
risikoarme Tickets (insbesondere solche, die sich als „bleibt wie es ist, kein Code nötig"
entscheiden) dürfen dabei in einer Sitzung fallen — die Ein-Ticket-Regel schützt vor unbedachten
Entscheidungen, nicht vor schnellen.

Vorbild/Muster: [B-106](B-106-lehrwerkgetriebener-katalog.md) — Protokoll des vollständigen Ablaufs
(Story-Findung mit allen drei Rollen → autonomes Grillen → Entwickler-Brief → Bauen → Review →
Rollen-Re-Review → Rückfluss → Commit → nächste Runde) steht in
`docs/pm-sitzung-2026-08-04.md` ab dem Abschnitt „Runde: Lehrwerkgetriebener Katalog".

**Was der autonome Modus nicht ändert:** die Eintrittsbedingungen der Stufenkette bleiben gleich
scharf (Begründung **und** Kosten je Entscheidung, echte Testzahlen, genannte Commits für
`abgenommen`); nur *wer* die Entscheidung trifft und *wie oft* pro Sitzung ändert sich. Ohne diese
ausdrückliche Freigabe gilt wieder der Standard — die Erlaubnis gehört zum Vorhaben, nicht zum Repo.

#### Der Backlog-Lauf: dieselbe Freigabe, aber offen statt je Vorhaben

„Arbeite das Backlog ab" ist eine **größere** Freigabe als die für B-106: sie gilt nicht für ein
Vorhaben, dessen Ende man sieht, sondern für eine Liste, die weiterwächst. Damit sie trotzdem eine
Grenze hat, gelten für den Backlog-Lauf drei Zusätze — sie sind der Preis dafür, dass niemand je Story
gefragt wird.

**1. Was der Agent selbst grillen darf, entscheidet `art`.**

| `art` | Grillen | Warum |
| --- | --- | --- |
| `Defekt` | **autonom** | Der Code verengt die Antwort: „richtig herstellen" ist keine Produktentscheidung, und ein Regressionstest belegt sie. |
| `Aufräumen` | **autonom** | Kein Verhalten ändert sich; die Suite ist der Beleg. |
| `Frage` | **nur im Dialog** | Ein Prüfauftrag endet oft in `verworfen` — also darin, Arbeit zu *streichen*. Das ist eine Wertentscheidung. |
| `Wunsch` | **nur im Dialog** | Neue Fähigkeiten sind Produktrichtung und Geschmack. Dafür hat der Agent keine Legitimation, auch mit guter Begründung nicht. |

Der Schnitt läuft an einem Feld, das es ohnehin gibt, statt an einer zweiten Liste. **Kosten:** ein
Lauf bleibt an jedem `Wunsch` stehen und liefert weniger, als das Backlog hergibt — gewollt. Die
35 offenen P3 sind überwiegend Wünsche; ein Lauf, der sie mitentscheidet, baut das Produkt eines
Agenten, nicht deines.

**2. Der Lauf hält an — und zwar an einem Ergebnis, nicht an einer leeren Liste.** Halt am Sprint-Ende,
wenn eines von beiden zutrifft: die Retrospektive hat ein **neues Tor** erzeugt (das willst du sehen,
bevor es weitere Arbeit prägt), oder ein Review hat einen Defekt **im Increment dieses Sprints** gefunden
(dann ist die Qualitätsschwelle gerutscht, und Weiterlaufen ist die falsche Reaktion). „Backlog leer" ist
keine Abbruchbedingung — es ist kein erreichbarer Zustand.
**Für den Nachtlauf speziell gilt seit 2026-08-06 eine feinere Fassung dieses zweiten Punkts**
(behebbare Review-Funde werden sofort behoben bzw. im selben Sprint als Defekt bearbeitet; erst mehr als
fünf davon in einem Sprint gelten als Endlosschleife und beenden die Nacht) — siehe
[nachtlauf.md](../nachtlauf.md), Freigabe 3.

**3. Der Rollengang bleibt Pflicht**, und das ist die Bedingung, an der die 14er-Runde vom 2026-08-05
gescheitert ist (siehe [oben](#der-rollengang-fällt-am-leichtesten-weg--und-kostet-am-meisten)). Er wird
**je Sprint** geführt, nicht je Story — deshalb hat ein Sprint eine Obergrenze
(`pm-loop`, „The Sprint"). Fällt er aus, steht das als Zeile im `## Verlauf`, nicht im Nichts.

## Die eine Zahl über die Wirkung: was die Abnahme durchgelassen hat

Alles andere in diesem Bereich misst **Einhaltung** — gesetzte Felder, vorhandene Abschnitte, echte
Testzahlen, gelaufene Schritte. Keine davon beantwortet „ist die App besser als vorige Woche". Genau
eine Zahl kann das, und sie steht im Index: **Defekte, die in schon abgenommener Arbeit gefunden wurden.**

Getragen wird sie von einem Feld auf der Defekt-Story:

```yaml
entgangen_bei: [B-99]     # dieser Defekt saß in Arbeit, die bereits `abgenommen` war
```

### Was zählt — und was ausdrücklich nicht

Die Zahl misst, was die **Abnahme durchgelassen** hat. Sie ist kein Zähler für „Folgearbeit". Die
Unterscheidung ist der ganze Wert des Felds, darum an echten Fällen dieses Bereichs:

| Fall | Beispiel | Zählt? |
| --- | --- | --- |
| Defekt in Code, der beim Fund schon `abgenommen` war | B-110/B-111 in der Kaufhistorie aus B-99 | **ja** |
| Gefunden bei der Verifikation der Story selbst, vor ihrer Abnahme | B-113 während B-110 `in-arbeit` war | nein — das Tor hat gehalten |
| Reviewer-Befund vor dem Sign-off | B-93 (zum B-78-Bau), B-104 (zum B-97-Bau) | nein — das ist Step 5 bei der Arbeit |
| „Befund außerhalb des Diffs" | B-80, B-82, B-84 | nein — der Fehler liegt in älterem Code, nicht in der geprüften Arbeit |
| Abspaltung einer Grill-Entscheidung | B-72, B-77, B-78 (aus B-69/B-76) | nein — nie ausgeliefert, nichts entgangen |
| Nicht zuordenbar (Altbestand vor diesem Bereich) | B-109 | Feld bleibt leer, und das ist keine Nachlässigkeit |

### Der Nenner ist die Falle — darum ist er ein Feld

**Die Quote läuft nie über alle abgenommenen Stories.** „4 von 42" läse sich wie zehn Prozent Fehlerrate
und wäre eine Lüge über die, die **nie nachgeprüft** wurden. Ein fehlender Eintrag heißt „nicht
beobachtet", niemals „sauber".

Damit der Unterschied überhaupt existiert, trägt jede abgenommene Story das Gegenstück:

```yaml
nachgeschaut: 2026-08-05   # nach der Abnahme hat noch einmal jemand hingesehen
```

**Ein Blick, der nichts findet, wird genauso eingetragen** — das ist die tragende Hälfte der Regel. Sonst
ist „sauber geprüft" von „nie angesehen" nicht zu unterscheiden, und genau diese Verwechslung macht jede
Qualitätszahl wertlos. Der Index rechnet daraus die einzige ehrliche Quote (*„Nachgeschaut: X von Y — und
in Z davon steckte ein durchgekommener Defekt"*) und führt die nie geprüften als **Arbeitsvorrat der
Nachschau** auf.

### Der Auslöser: die Nachschau ist die Pflichthandlung der Retro

Ohne Auslöser bleibt die Zahl bei null, weil niemand hinsieht — sie sinkt dann nicht, sie verschwindet aus
dem Blick. Der Auslöser sitzt darum in `pm-loop` **Step 8** und ist die *einzige Pflichthandlung* der
Retrospektive: sie liest die Zeile „Nachgeschaut: X von Y" aus diesem Index, und ist die Arbeit des
**vorigen** Sprints nicht unter den X, wird sie jetzt angesehen. Das Ergebnis steht als erste Zeile unter
`## Retrospektive` im Protokoll — **ein Sprint ohne Nachschau-Zeile ist nicht geschlossen.**

Das ist kein Tor im harten Sinn, und der Grund dafür ist ehrlich zu benennen: die Handlung „hinsehen" ist
nicht mechanisch prüfbar, nur ihre *Aufzeichnung*. Was der Mechanismus leistet, ist, das Weglassen
**sichtbar** zu machen — eine fehlende Zeile in einem datierten Protokoll und ein Nenner, der nicht wächst.
Was er nicht leisten kann, ist, einen flüchtigen Blick von einem sorgfältigen zu unterscheiden.

**Der Stand** steht im Index (er rechnet ihn), die Herkunft in den Protokollen. Bewusst **nicht**
nachgetragen ist der Durchgang vom 2026-08-04: welche Stories er abdeckte, ist nicht belegt — und ein
geratener Nenner wäre schlimmer als ein kleiner.

**Unbeaufsichtigt** (über Nacht) braucht dieser Lauf mehrere Vorab-Freigaben; seit der Erprobung vom
2026-08-05 darf er mehrere thematisch verwandte Sprints hintereinander fahren statt nur einen.
Auftragstext und ehrliche Erwartung stehen in [nachtlauf.md](../nachtlauf.md).

### Warum der Defekt eine eigene Story braucht

Ein Defekt in abgenommener Arbeit bekommt eine **eigene Story**, auch wenn er in fünf Minuten behoben ist.
Sonst steht er als Zeile im `## Verlauf` der alten Story und fehlt in der Messung — genau so waren die
zwei wichtigsten Fälle vom 2026-08-05 (B-114, B-115) zuerst unsichtbar, obwohl sie der Anlass für diese
Zahl sind. Die Story darf dünn sein; sie muss nur existieren und das Feld tragen.

### „Je Sprint" braucht kein eigenes Feld

Das Feld zeigt auf die **abgenommene Story**, nicht auf einen Sprint — Sprints haben in diesem Bereich
keine Id, und eine zweite einzuführen wäre ein Feld, das bei jeder Story gepflegt werden müsste, damit es
in seltenen Fällen etwas aggregiert. Die Zuordnung läuft stattdessen über die Story: ihr `## Verlauf` nennt
das Protokoll, in dem sie gebaut wurde. Aus `entgangen_bei: [B-99]` wird so „Sprint/Runde, in der B-99
abgenommen wurde".

**Der Stand heute, so gelesen:** alle **vier** erfassten Entgleitungen zeigen auf B-99, B-96 und B-66 —
also auf **dieselbe** Runde, die autonome Bau-Runde vom 2026-08-05 mit ihren vierzehn Abnahmen. Genau die
Runde, die **keinen einzigen Rollengang** geführt hat. Das ist die Grundlinie, an der sich künftige Sprints
messen.

Und die notwendige Einschränkung dazu, damit die Grundlinie nicht mehr behauptet als sie kann: **Sprint 1
(2026-08-05, B-110/B-111) steht bei null — aber niemand hat ihn nachgeprüft.** Diese Null ist kein Erfolg,
sie ist eine Nichtbeobachtung. Erst der nächste nachträgliche Blick auf abgenommene Arbeit macht aus ihr
eine Aussage.

### Was man mit der Zahl macht

Nach dem dritten oder vierten Sprint sagt sie, ob das Rollengang-Tor wirkt. **Steigt sie, ist die Abnahme
zu weich.** Und wenn sie steigt, ist die richtige Reaktion, das Tor zu **verwerfen oder umzubauen** — nicht
es zu verschärfen: eine Bedingung, die nicht hält, wird durch mehr Text nicht besser.

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

### Offen (62)

| Id | Story | Art | Stufe | Prio | Größe | Wo | Kostet |
| --- | --- | --- | --- | --- | --- | --- | --- |
| [B-254](B-254-weg-zeigt-englischen-planfehler.md) | Der Trophäenweg zeigt dem Kind „Study plan not found." statt einer Erklärung | Defekt | `idee` | P2 | — | — | — |
| [B-31](B-31-geraete-vorbehalt-klang.md) | Geräte-Vorbehalt: Klang und Haptik am echten Handy gegenhören | Frage | `geschaetzt` | P2 | XS | frontend | — |
| [B-208](B-208-put-ohne-einkleidung-loescht-sie.md) | Ein Übungs-PUT ohne `interestTagIds` löscht die Einkleidung lautlos | Frage | `gegrillt` | P2 | — | backend | Vertrag? |
| [B-39](B-39-supervisor-dashboard.md) | Supervisor-Dashboard über die Kinder | Wunsch | `geschaetzt` | P2 | L | beides | — |
| [B-207](B-207-kind-auswahlliste-ist-unkuratiert.md) | Die Auswahlliste des Kindes zeigt jedes Schlagwort, das je irgendwer getippt hat | Wunsch | `gegrillt` | P2 | — | beides | Migration? |
| [B-217](B-217-khan-academy-vergleich-sammel-story.md) | Khan-Academy-Vergleich: die Verbesserungen übernehmen (Sammel-Story) | Wunsch | `idee` | P2 | — | — | — |
| [B-218](B-218-mischwiederholung-ueber-alle-positionen.md) | Eine Mischwiederholung über alle Positionen des Kindes | Wunsch | `idee` | P2 | — | — | — |
| [B-220](B-220-learning-queue-im-sohn-dashboard.md) | Eine Learning Queue in der Sohn-Arcade: heute, diese Woche, überfällig | Wunsch | `idee` | P2 | — | — | — |
| [B-247](B-247-knowunity-vergleich-sammel-story.md) | Knowunity-Vergleich: die Verbesserungen übernehmen (Sammel-Story) | Wunsch | `idee` | P2 | — | — | — |
| [B-248](B-248-klassenarbeit-countdown-treibt-den-plan.md) | Die Klassenarbeit wird beim Kind sichtbar und verdichtet den Plan vor dem Termin | Wunsch | `idee` | P2 | — | — | — |
| [B-249](B-249-probeklausur-mit-notenprognose.md) | Eine Probeklausur über das Bündel einer Klassenarbeit, mit Notenprognose | Wunsch | `idee` | P2 | — | — | — |
| [B-134](B-134-bedingte-live-regionen.md) | Dreizehn Live-Regionen entstehen zusammen mit ihrem Text — und schweigen darum | Defekt | `in-arbeit` | P3 | M | frontend | — |
| [B-236](B-236-checkbox-gruppe-ohne-zugaenglichen-gruppennamen.md) | Eine Gruppen-Überschrift über Kontrollkästchen ist sichtbar, aber für den Screenreader an nichts gebunden | Defekt | `in-arbeit` | P3 | S | frontend | — |
| [B-263](B-263-info-popover-steht-am-rechten-rand-ueber.md) | Ein aufgeklapptes ⓘ am rechten Rand lässt die Seite seitlich scrollen | Defekt | `in-arbeit` | P3 | XS | frontend | — |
| [B-164](B-164-useasync-paart-frischen-zustand-mit-alten-daten.md) | Nach einem Wechsel zeigt der Bildschirm für einen Moment frischen Zustand mit alten Daten | Defekt | `ausformuliert` | P3 | — | frontend | — |
| [B-141](B-141-interest-tag-label-dublette.md) | Zwei Interessen-Tags dürfen dasselbe Label tragen | Defekt | `idee` | P3 | — | — | — |
| [B-205](B-205-exercise-in-use-ohne-deutsche-fassung.md) | `exercise_in_use` fehlt in `GERMAN_PROBLEM_TEXT` — die zwei Geschwister stehen drin | Defekt | `idee` | P3 | — | — | — |
| [B-245](B-245-token-mit-fid-auf-geloeschten-adult.md) | Ein Token, das vor `DELETE adults/{id}` ausgestellt wurde, trägt ein `fid` auf eine gelöschte Zeile – jeder `OwnerAdultId`-Schreiber liefe in die FK | Defekt | `idee` | P3 | — | backend | — |
| [B-255](B-255-kinderzeile-per-namen-zweideutig.md) | E2E-Specs suchen die Kinderzeile per Namen, den zwei Tabellen tragen | Defekt | `idee` | P3 | — | — | — |
| [B-257](B-257-zielfeier-verspaetet-jenseits-der-ersten-fuenf.md) | Ein schon erreichtes Ziel jenseits der ersten fünf wird später verspätet gefeiert | Defekt | `idee` | P3 | — | — | — |
| [B-258](B-258-unbekannte-fach-id-zwei-fehlercodes.md) | Eine unbekannte `SubjectId` meldet je nach Ressource zwei verschiedene Fehlercodes | Defekt | `idee` | P3 | — | — | — |
| [B-259](B-259-generator-verschluckt-leerzeichen-neben-xml-elementen.md) | Der XML-Doc-Generator verschluckt Leerzeichen zwischen zwei Elementen | Defekt | `idee` | P3 | — | — | — |
| [B-260](B-260-normalisierer-setzt-absatz-mitten-im-satz.md) | Der OpenAPI-Normalisierer setzt einen Absatz mitten in einen Satz | Defekt | `idee` | P3 | — | — | — |
| [B-261](B-261-produktionspfad-des-beispielkatalogs-ungetestet.md) | Der Produktionspfad des Beispielkatalogs hängt an keinem Test | Defekt | `idee` | P3 | — | — | — |
| [B-262](B-262-vorwahl-bestehendes-kind-ohne-eigene-spec.md) | „Ohne Eingabe bleibt die Vorwahl ‚bestehendes Kind‘“ hält keine Spec gezielt | Defekt | `idee` | P3 | — | — | — |
| [B-264](B-264-interessen-ohne-kind-kleiden-nicht-ein.md) | `--interests` ohne Kind setzt die Tags an die Übung, kleidet ihren Inhalt aber nicht ein | Defekt | `idee` | P3 | — | backend | — |
| [B-265](B-265-sperr-zaehler-zusicherung-kann-nie-fallen.md) | Die „Spur“ der gesperrten Lesevorgänge ist eine Zusicherung, die nie fallen kann | Defekt | `idee` | P3 | — | backend | — |
| [B-266](B-266-wartepunkt-netzwerk-statt-app-zustand.md) | Der Wartepunkt aus B-180 wartet aufs Netzwerk, nicht auf die App | Defekt | `idee` | P3 | — | frontend | — |
| [B-268](B-268-fokus-nach-inline-reihe.md) | Nach „Reihe anlegen & wählen“ fällt der Fokus auf `body` | Defekt | `idee` | P3 | — | frontend | — |
| [B-17](B-17-birkenbihl-sprachcodes.md) | Sprachcode-Normalisierung bei der Vokabel-Dekodierung | Frage | `in-arbeit` | P3 | XS | frontend | — |
| [B-152](B-152-schoolTypes-filter-und-attribut.md) | `SchoolTypes` ist Filter und Attribut zugleich — vier Stellen leiten daraus drei Antworten ab | Frage | `idee` | P3 | — | — | — |
| [B-231](B-231-fokus-ohne-fehlerkasten.md) | Wohin gehört der Fokus, wenn ein Knopf ohne Fehlerkasten gesperrt wird? | Frage | `idee` | P3 | — | frontend | — |
| [B-232](B-232-bestaetigung-ausserhalb-des-sichtfelds.md) | Soll eine Bestätigung zum Meldungsplatz scrollen, wenn er weit vom Knopf entfernt liegt? | Frage | `idee` | P3 | — | frontend | — |
| [B-233](B-233-zwei-sperrmuster-auf-der-lehrwerke-seite.md) | Eine Seite, zwei Sperrmuster: Soll „Pflichtfeld leer" überall gleich aussehen? | Frage | `idee` | P3 | — | frontend | — |
| [B-234](B-234-dritter-zustand-der-arten-liste.md) | Soll die Arten-Liste einen dritten Zustand bekommen – oder B-174s Entscheidung 3 zurückgenommen werden? | Frage | `idee` | P3 | — | frontend | — |
| [B-253](B-253-wochenvergleich-familie-statt-liga.md) | Prüfauftrag: ein Wochenvergleich gegen sich selbst oder Geschwister statt einer Liga? | Frage | `idee` | P3 | — | — | — |
| [B-267](B-267-entkoppeltes-lehrbuch-zeigt-katalogwert-im-freitextfeld.md) | Nach dem Entkoppeln zeigt das Freitextfeld den Katalogwert – gespeichert wird er nicht | Frage | `idee` | P3 | — | beides | — |
| [B-12](B-12-uebungen-kuratieren.md) | Geteilte Übungen bewerten und kuratieren | Wunsch | `in-arbeit` | P3 | M | beides | Migration |
| [B-15](B-15-testmodus-weitere-typen.md) | Vorschau für die nicht-prüfbaren Übungstypen | Wunsch | `in-arbeit` | P3 | S | beides | — |
| [B-21](B-21-ki-creator-foerdermodus.md) | KI-Creator: Fördermodus (`--use-weak`) sprachrein und mit Tests absichern | Wunsch | `in-arbeit` | P3 | S | backend | — |
| [B-68](B-68-vater-web-responsive.md) | Das Vater-Web hat keinen einzigen eigenen Breakpoint | Wunsch | `in-arbeit` | P3 | M | frontend | — |
| [B-155](B-155-grammatik-themen-als-tags.md) | Grammatik als Thema, nicht als Freitext | Wunsch | `geschaetzt` | P3 | L | beides | Migration |
| [B-22](B-22-unit-stoffnotizen-llm.md) | Unit-Stoffnotizen LLM-gestützt befüllen | Wunsch | `geschaetzt` | P3 | M | backend | Migration |
| [B-28](B-28-login-name-sequenziell.md) | Sequenzielle IDs als Login-Name | Wunsch | `geschaetzt` | P3 | S | backend | — |
| [B-85](B-85-i18n-infrastruktur-sohn-arcade-englisch.md) | i18n-Infrastruktur + Sohn-Arcade auf Englisch (erste Teilstufe der Mehrsprachigkeit) | Wunsch | `geschaetzt` | P3 | L | frontend | — |
| [B-09](B-09-lehrer-hausaufgaben.md) | Lehrer erteilt Hausaufgaben: zuweisen mit Frist, ohne Betreuungsauftrag | Wunsch | `ausformuliert` | P3 | L | backend | Migration |
| [B-16](B-16-positions-formular-umfang.md) | Prüfauftrag: deckt das Positions-Edit-Formular alle Felder ab? | Wunsch | `ausformuliert` | P3 | S | frontend | — |
| [B-20](B-20-ki-supervisor-agent.md) | KI-Supervisor-Agent (Teil D) — erster Schritt: Übungen einem Plan zuweisen | Wunsch | `ausformuliert` | P3 | L | backend | — |
| [B-122](B-122-top-level-listen-bekommen-paging.md) | Sieben Top-Level-Sammlungen bekommen `skip`/`take` | Wunsch | `idee` | P3 | — | — | — |
| [B-140](B-140-freigabe-kennzeichen-in-der-uebungsauswahl.md) | Die Übungsauswahl verschweigt, dass eine Übung zurückgezogen ist | Wunsch | `idee` | P3 | — | — | — |
| [B-183](B-183-gescheitertes-lesen-ohne-erneuten-versuch.md) | Ein gescheitertes Lesen ist ohne F5 nicht zu wiederholen | Wunsch | `idee` | P3 | — | — | — |
| [B-219](B-219-verzeihende-abstufung-je-position.md) | Verzeihende Abstufung als Positions-Option | Wunsch | `idee` | P3 | — | — | — |
| [B-221](B-221-level-leiter-fuer-auszeichnungen.md) | Eine Level-Leiter für Auszeichnungen statt einzelner Badges | Wunsch | `idee` | P3 | — | — | — |
| [B-223](B-223-gestufte-hinweise-vor-der-antwort.md) | Gestufte Hinweise vor der Antwort, die Punkte kosten | Wunsch | `idee` | P3 | — | — | — |
| [B-250](B-250-stundenplan-in-der-tagesmission.md) | Der Stundenplan gewichtet die Tagesmission | Wunsch | `idee` | P3 | — | — | — |
| [B-251](B-251-foto-der-vokabelliste-in-den-store.md) | Ein Foto der Vokabelliste aus dem Buch landet im Vokabelspeicher | Wunsch | `idee` | P3 | — | — | — |
| [B-252](B-252-vorlesen-und-sprechen-bei-vokabeln.md) | Vokabeln vorlesen lassen und optional per Stimme beantworten | Wunsch | `idee` | P3 | — | — | — |
| [B-47](B-47-deploy-artefakt-smoke.md) | Startet das veröffentlichte Artefakt überhaupt? | Aufräumen | `geschaetzt` | P3 | S | backend | — |
| [B-230](B-230-pin-hash-doppelt-gehalten.md) | Der PIN-Hash liegt doppelt: Profil und Konto halten denselben Wert | Aufräumen | `idee` | P3 | — | — | — |
| [B-269](B-269-bewertungen-reihenfolge-und-updatedat-ungetestet.md) | Bewertungen: Reihenfolge der Liste und `UpdatedAt` beim Überschreiben sind ungetestet | Aufräumen | `idee` | P3 | — | backend | — |
| [B-04](B-04-adaptiver-vokabel-pool.md) | Adaptiver Vokabel-Pool je Position | Wunsch | `geschaetzt` | P4 | M | backend | Migration |
| [B-05](B-05-buchstaben-tausch.md) | Buchstaben-Tausch-Eingabe (Anagramm) | Wunsch | `geschaetzt` | P5 | M | beides | — |

<details>
<summary>Abgenommen (184)</summary>

| Id | Story | Art | Stufe | Prio | Größe | Wo | Kostet |
| --- | --- | --- | --- | --- | --- | --- | --- |
| [B-01](B-01-bildwahl-einfrieren.md) | Abschlusstest friert Bildwahlen ein, die er nie zeigt | Defekt | `abgenommen` | P1 | S | backend | Vertrag |
| [B-02](B-02-itemcount-hilfetext.md) | Der Hilfetext erklärt `ItemCount` falsch herum | Defekt | `abgenommen` | P2 | XS | frontend | — |
| [B-08](B-08-xml-docs-englisch.md) | XML-Doc-Kommentare im Backend auf Englisch übersetzen | Aufräumen | `abgenommen` | P3 | S | doku | — |
| [B-10](B-10-zeitfenster-pro-kind.md) | Zeitfenster (Punkte-Faktor) je Pflicht statt global | Wunsch | `abgenommen` | P2 | M | beides | Migration |
| [B-100](B-100-vertragsdokument-unterdeklariert.md) | Das Vertragsdokument verschweigt 401, `X-Total-Count` und 24 Operationsnamen | Aufräumen | `abgenommen` | P3 | S | backend | — |
| [B-101](B-101-fehlercodes-und-drei-waechter.md) | Drei generische Fehlercodes ersetzen — und die drei Wächter, die daraus reif geworden sind | Aufräumen | `abgenommen` | P3 | S | backend | — |
| [B-102](B-102-token-vorgabewert-regel-schaerfen.md) | Die Token-Regel im Startkontext ist zu weit formuliert — 55 Signaturen „verstoßen" gegen eine Compilerregel | Aufräumen | `abgenommen` | P3 | XS | doku | — |
| [B-104](B-104-keyresult-dublette-zahlt-doppelt.md) | Derselbe Meilenstein zweimal: drei Schreibpfade laufen ungeprüft in einen Unique-Index, und dort hängt Geld | Defekt | `abgenommen` | P2 | S | backend | — |
| [B-105](B-105-taegliche-belohnungsbox.md) | Tägliche Belohnungsbox: Loot-Box + Streak als positives Gegenstück zum Stick | Wunsch | `abgenommen` | P4 | S | backend | Migration |
| [B-106](B-106-lehrwerkgetriebener-katalog.md) | Übungen hängen künftig am Lehrwerk, nicht am Kapitel | Wunsch | `abgenommen` | P1 | L | beides | — |
| [B-107](B-107-dailybox-zufallswert-in-docs-capture.md) | `DailyBoxService` würfelt ohne Seed – der Doku-Capture-Snapshot ist dadurch nicht byte-stabil | Aufräumen | `abgenommen` | P3 | XS | backend | — |
| [B-108](B-108-requiretypedtest-default-am-uebungstyp.md) | `DefaultRequireTypedTest` am Übungstyp selbst ungeprüft — dieselbe Fehlerklasse eine Ebene höher als B-93 | Defekt | `abgenommen` | P3 | S | backend | — |
| [B-109](B-109-full-flow-spec-flackert-bei-frage-3.md) | `full-flow.spec.ts` hängt reproduzierbar bei „Frage 3/5" der Klausur | Defekt | `abgenommen` | P3 | S | frontend | — |
| [B-11](B-11-uebungen-veroeffentlichen.md) | Übungen ausdrücklich veröffentlichen | Wunsch | `abgenommen` | P2 | S | frontend | — |
| [B-110](B-110-kaufverlauf-ueberspringt-zeilen.md) | Der Kaufverlauf überspringt Zeilen und verpasst den eigenen Kauf | Defekt | `abgenommen` | P2 | S | beides | — |
| [B-111](B-111-verlauf-luegt-im-fehlerfall.md) | Scheitert das Laden des Verlaufs, sagt die App „Noch nichts gekauft" | Defekt | `abgenommen` | P2 | XS | frontend | — |
| [B-112](B-112-kommentar-begruendet-das-gegenteil.md) | Ein Kommentar begründet das Gegenteil der Bedingung unter ihm | Aufräumen | `abgenommen` | P3 | XS | frontend | — |
| [B-113](B-113-vater-kaufhistorie-endet-still.md) | Drei geblätterte Listen mit veränderlicher Sortierung — der Vater erreicht keine von ihnen vollständig | Defekt | `abgenommen` | P2 | M | beides | — |
| [B-114](B-114-showboth-position-unspielbar.md) | Eine Kennenlern-Position hatte für das Kind keinen einzigen Knopf — und kostete Münzen | Defekt | `abgenommen` | P1 | S | beides | — |
| [B-115](B-115-buchstabenkaestchen-index-drift.md) | Übersprang das Kind ein Buchstabenkästchen, rutschten alle folgenden Zeichen | Defekt | `abgenommen` | P2 | XS | frontend | — |
| [B-116](B-116-blaettern-ohne-rueckmeldung.md) | Beim Blättern gibt es keine Rückmeldung mehr — und der Pager meldet eine Seite, die noch nicht da ist | Defekt | `abgenommen` | P3 | S | frontend | — |
| [B-117](B-117-uebungsbildschirm-bietet-test-trotz-anzeigestufe.md) | Nach der Übungsrunde bietet der Bildschirm einen Test an, den es für diese Stufe nicht gibt | Defekt | `abgenommen` | P2 | S | beides | — |
| [B-118](B-118-dailybox-spanne-ohne-zusicherung.md) | Keine Zusicherung sieht die Ziehungsspanne der Tagesbox mehr | Aufräumen | `abgenommen` | P3 | XS | backend | — |
| [B-119](B-119-ratenbegrenzer-hinter-proxy.md) | Hinter einem Reverse Proxy partitioniert der Ratenbegrenzer alle Nutzer in einen Topf | Defekt | `abgenommen` | P2 | XS | backend | — |
| [B-120](B-120-waechter-anonym-heisst-gedrosselt.md) | „Anonym heißt gedrosselt" hängt an Disziplin, nicht an einem Tor | Aufräumen | `abgenommen` | P3 | XS | backend | — |
| [B-121](B-121-platzhalter-und-paging-tore.md) | Platzhalter-Rot-Liste und Paging-Tor aus B-101 | Aufräumen | `abgenommen` | P3 | S | backend | — |
| [B-123](B-123-lehrwerk-reihe-bearbeiten.md) | Lehrwerk-Reihe im Vater-Web bearbeiten | Wunsch | `abgenommen` | P1 | M | beides | — |
| [B-124](B-124-umbenennen-umgeht-die-eindeutigkeit.md) | Anlegen schützt die Eindeutigkeit, Umbenennen umgeht sie | Defekt | `abgenommen` | P2 | S | backend | — |
| [B-125](B-125-forwarded-proto-fehlt.md) | Die App weiß hinter dem Proxy, wer ruft — aber nicht, dass er über HTTPS ruft | Defekt | `abgenommen` | P2 | XS | backend | — |
| [B-126](B-126-ableitung-behauptet-falsche-herkunft.md) | „aus dem Lehrwerk übernommen" steht auch unter Werten, die nicht von dort kommen | Defekt | `abgenommen` | P2 | S | frontend | — |
| [B-127](B-127-verlag-loeschen-trifft-fremde.md) | Jeder Creator darf einen Verlag löschen, den alle benutzen | Frage | `abgenommen` | P3 | S | backend | — |
| [B-128](B-128-katalogsuche-case-sensitiv.md) | Die Katalogsuche findet „KLETT" nicht, obwohl „Klett" da ist | Defekt | `abgenommen` | P3 | S | backend | Migration |
| [B-129](B-129-themenfeld-committet-beim-verlassen.md) | Das Themenfeld legt an, was beim Wegklicken gerade dasteht | Defekt | `abgenommen` | P3 | XS | frontend | — |
| [B-13](B-13-fach-kapitel-eigentum.md) | Fach- und Kapitel-Eigentum | Wunsch | `abgenommen` | P2 | M | backend | Migration |
| [B-130](B-130-unit-themen-ohne-grenze.md) | Aus einem 200-Zeichen-Feld wurde eine unbegrenzte Liste, ohne dass eine Grenze nachrückte | Aufräumen | `abgenommen` | P3 | S | backend | — |
| [B-131](B-131-leere-story-faellt-aus-dem-index.md) | Eine leere Story-Datei verschwindet aus dem Index — auch aus der Mängelliste | Defekt | `abgenommen` | P3 | XS | doku | — |
| [B-132](B-132-hinweis-live-region-haengt-aus.md) | Der Hinweis „aus dem Lehrwerk übernommen" wird angesagt, indem seine Live-Region entsteht | Defekt | `abgenommen` | P3 | XS | frontend | — |
| [B-133](B-133-zwei-reihen-ein-anzeigename.md) | Nach einer Umbenennung können zwei Reihen denselben Anzeigenamen tragen | Defekt | `abgenommen` | P3 | S | backend | — |
| [B-135](B-135-freitextsuchen-case-sensitiv.md) | Sieben weitere Freitextsuchen sind buchstabengenau | Defekt | `abgenommen` | P3 | M | backend | — |
| [B-136](B-136-verlag-umbenennen-erzeugt-namensdublette.md) | Beim Verlag steht dieselbe Dublettenlücke wie bei der Reihe | Defekt | `abgenommen` | P3 | S | backend | — |
| [B-138](B-138-markup-sickert-in-openapi.md) | Rohe HTML-Tags stehen in 70 Beschreibungen des Vertragsdokuments | Aufräumen | `abgenommen` | P3 | S | backend | — |
| [B-139](B-139-e2e-nachtlauf-login-bricht-ab-test-2.md) | Der Aufräum-Sweep der E2E löschte das Journal seiner eigenen Datenbank | Defekt | `abgenommen` | P1 | XS | frontend | — |
| [B-142](B-142-fachname-driftet-gegen-fach-id.md) | Ein Fachwechsel hinterlässt den alten Fachnamen | Defekt | `abgenommen` | P3 | M | beides | — |
| [B-143](B-143-formular-kennt-zustaende-des-modells-nicht.md) | Das Reihen-Formular kennt zwei Zustände nicht, die das Modell erlaubt | Defekt | `abgenommen` | P3 | S | frontend | — |
| [B-144](B-144-fach-loeschen-trifft-reihen-lautlos.md) | Ein Fach zu löschen löscht Meilensteine und Stundenpläne des Kindes | Defekt | `abgenommen` | P3 | M | beides | Migration |
| [B-145](B-145-fach-umbenennen-laesst-namen-stehen.md) | Ein umbenanntes Fach lässt seinen Namen in drei Tabellen stehen | Defekt | `abgenommen` | P3 | S | backend | — |
| [B-146](B-146-anlegeformular-schickt-toten-fachnamen.md) | Das Anlege-Formular schickt einen Fachnamen, den der Server ignoriert | Aufräumen | `abgenommen` | P3 | XS | frontend | — |
| [B-147](B-147-para-summaries-tragen-einrueckung-ins-dokument.md) | Ein `<para>` im `<summary>` trägt seine Quelltext-Einrückung ins OpenAPI-Dokument | Defekt | `abgenommen` | P3 | S | backend | — |
| [B-148](B-148-lehrbuch-formular-zerstoert-fachnamen.md) | Das Lehrbuch-Formular am Kind zerstört den Fachnamen bei jedem Speichern | Defekt | `abgenommen` | P2 | M | frontend | — |
| [B-149](B-149-schularten-tabelle-statt-manifest.md) | Die Schularten-Liste ist eine handgepflegte Kopie eines Server-Enums | Aufräumen | `abgenommen` | P3 | S | beides | — |
| [B-150](B-150-verlagssperre-unsichtbar-dialog-verspricht-gegenteil.md) | Die Verlags-Löschsperre war für den Vater unsichtbar — der Dialog versprach das Gegenteil | Defekt | `abgenommen` | P2 | S | beides | — |
| [B-151](B-151-gespeichert-banner-verschwindet-mit-dem-formular.md) | „Gespeichert." ist im Lehrbuch- und Fachlehrer-Formular nie zu sehen | Defekt | `abgenommen` | P3 | S | frontend | — |
| [B-153](B-153-bilder-spec-flackert-im-vollen-lauf.md) | `bilder.spec.ts` fällt im vollen Lauf gelegentlich aus, allein ist sie grün | Defekt | `abgenommen` | P2 | XS | frontend | — |
| [B-154](B-154-katalogseite-bietet-fremde-faecher-zum-umbenennen.md) | Die Katalogseite bietet „Umbenennen" und „Löschen" an jedem Fach an — auch an fremden | Defekt | `abgenommen` | P2 | S | frontend | — |
| [B-156](B-156-ismine-heisst-anderswo-isown.md) | Dasselbe Eigentums-Flag heißt im Vertrag einmal `isMine` und siebenmal `isOwn` | Aufräumen | `abgenommen` | P3 | S | beides | Vertrag |
| [B-157](B-157-kategorien-unter-fremdem-fach-ungeschuetzt.md) | Das Fach ist geschützt, seine „Arten" sind es nicht | Defekt | `abgenommen` | P2 | S | beides | — |
| [B-158](B-158-subjectscontroller-drei-kleine-reste.md) | Drei kleine Reste im `SubjectsController` | Aufräumen | `abgenommen` | P3 | S | backend | — |
| [B-159](B-159-reihe-ohne-owner-behauptet-fremden-ersteller.md) | Eine Reihe ohne Eigentümer behauptet, jemand anderes habe sie angelegt | Defekt | `abgenommen` | P3 | XS | frontend | — |
| [B-160](B-160-gesperrter-knopf-nennt-den-grund-nie.md) | Der gesperrte Löschen-Knopf nennt seinen Grund nie — der `title` erscheint nicht | Defekt | `abgenommen` | P2 | S | frontend | — |
| [B-161](B-161-alle-waehlen-macht-die-auswahl-unsichtbar.md) | „Alle wählen" wählt bis zu 400 Übungen, die der Vater nie sieht und nicht abwählen kann | Defekt | `abgenommen` | P1 | S | frontend | — |
| [B-162](B-162-assistent-nennt-den-leeren-katalog-als-ursache.md) | Scheitert die Übungssuche im Assistenten, behauptet er einen leeren Katalog | Defekt | `abgenommen` | P2 | M | frontend | — |
| [B-163](B-163-art-und-typ-tragen-dieselben-woerter.md) | „Art" und „Typ" tragen dieselben Wörter — in einer Zeile sogar zweimal hintereinander | Defekt | `abgenommen` | P2 | S | beides | — |
| [B-165](B-165-backend-suite-flackert.md) | `AtomaresSchreiben_KeineLeseFehler` fällt unter Volllast — und färbt das Test-Tor rot | Defekt | `abgenommen` | P1 | XS | backend | — |
| [B-166](B-166-wiki-nennt-die-entfernte-kapitel-route.md) | Wiki und Endpunkt-Karte nennen die entfernte Kapitel-Route | Defekt | `abgenommen` | P2 | XS | doku | — |
| [B-167](B-167-katalogliste-zeigt-beim-filterwechsel-alte-treffer.md) | Katalogliste im Planbau zeigt beim Filterwechsel die alten Treffer weiter | Defekt | `abgenommen` | P3 | S | frontend | — |
| [B-168](B-168-eigentuemer-zusicherung-prueft-eine-konstante.md) | „Anlegen macht den Aufrufer zum Eigentümer" prüft eine Konstante, keine Identität | Defekt | `abgenommen` | P1 | XS | backend | — |
| [B-169](B-169-ladefenster-macht-die-alten-zeilen-anklickbar.md) | Im Ladefenster sind die alten Zeilen anklickbar — und die Auswahl überlebt | Defekt | `abgenommen` | P1 | XS | frontend | — |
| [B-170](B-170-selbst-angelegte-art-im-grundbestand-ist-unloeschbar.md) | Eine selbst angelegte Art im Grundbestands-Fach ist unlöschbar — und der Seitentext verspricht das Gegenteil | Defekt | `abgenommen` | P1 | M | beides | Migration |
| [B-171](B-171-zwei-zusicherungen-pruefen-den-ausgangszustand.md) | Zwei Zusicherungen prüfen den Ausgangszustand mit — und können darum nicht fallen | Defekt | `abgenommen` | P2 | XS | beides | — |
| [B-172](B-172-fachauswahl-loescht-wenn-ihre-optionen-fehlen.md) | Die Fachauswahl löscht das Fach, wenn ihre eigenen Optionen nicht geladen sind | Defekt | `abgenommen` | P1 | M | frontend | — |
| [B-173](B-173-speichern-vernichtet-seine-eigene-bestaetigung.md) | Ein erfolgreiches Speichern vernichtet seine eigene Bestätigung | Defekt | `abgenommen` | P2 | M | frontend | — |
| [B-174](B-174-arten-liste-verschweigt-ihren-fehler.md) | Die Arten-Liste verschweigt, dass ihr Laden gescheitert ist | Defekt | `abgenommen` | P2 | S | frontend | — |
| [B-175](B-175-zwei-gerottete-zahlen-in-kommentaren.md) | Zwei Zahlen in Kommentaren widersprechen dem, was gemessen wurde | Defekt | `abgenommen` | P3 | XS | doku | — |
| [B-176](B-176-gruppen-ueberschrift-verliert-die-feld-optik.md) | Eine Gruppen-Überschrift verliert ihre Optik, sobald sie kein `<label>` mehr ist | Defekt | `abgenommen` | P3 | XS | frontend | — |
| [B-177](B-177-seitenschluessel-haengt-an-einer-batching-annahme.md) | Der Seitenschlüssel hängt an einer Batching-Annahme, die kein Tor hält | Defekt | `abgenommen` | P3 | S | frontend | — |
| [B-178](B-178-katalogtext-verspricht-mehr-als-der-server-erlaubt.md) | Der Katalogtext verspricht mehr als der Server erlaubt | Defekt | `abgenommen` | P1 | XS | beides | — |
| [B-179](B-179-drei-fassungen-des-admin-testhelfers.md) | Drei Fassungen desselben Admin-Testhelfers, und die tragende Regel steht in jeder neu | Aufräumen | `abgenommen` | P3 | XS | backend | — |
| [B-18](B-18-auto-lehrplan-generator.md) | Lehrplan automatisch aus gefilterten Übungen bauen | Wunsch | `abgenommen` | P2 | S | beides | — |
| [B-180](B-180-zusicherung-haengt-an-scheduling-statt-am-gate.md) | Eine Zusicherung hing an 20 ms Scheduling statt am Gate | Defekt | `abgenommen` | P2 | XS | frontend | — |
| [B-181](B-181-praemisse-der-rennen-klasse-stimmt-nicht.md) | Die Rennen-Klasse nennt nur einen von zwei Fehlermodi — welcher auftritt, entscheidet die Plattform | Defekt | `abgenommen` | P2 | S | backend | — |
| [B-182](B-182-reichweite-nur-am-patch-festgenagelt.md) | Die Reichweite des Ventils war nur am PATCH festgenagelt | Defekt | `abgenommen` | P2 | XS | backend | — |
| [B-184](B-184-vier-platzhalter-zeigen-fehler-als-laedt-noch.md) | Vier Platzhalter zeigen einen Fehler dauerhaft als „Lade…" | Defekt | `abgenommen` | P3 | S | frontend | — |
| [B-185](B-185-anlegemodus-behauptet-uebernommenes-fach.md) | Beim Anlegen behauptet der Hinweis ein übernommenes Fach, das das Feld nicht zeigt | Defekt | `abgenommen` | P3 | XS | frontend | — |
| [B-187](B-187-unitform-verliert-seine-bestaetigung.md) | Auch das Unit-Formular schließt sich mit seiner eigenen Bestätigung | Defekt | `abgenommen` | P3 | XS | frontend | — |
| [B-188](B-188-wizard-verschweigt-drei-weitere-ausfaelle.md) | Der Assistent verschweigt drei weitere Ausfälle — einer schickt den Vater ins Kind-Anlegen | Defekt | `abgenommen` | P2 | S | frontend | — |
| [B-189](B-189-teilstring-zusicherung-bricht-im-vollen-lauf.md) | Eine Teilstring-Zusicherung bricht, sobald der Leer-Text daneben stehen bleibt | Defekt | `abgenommen` | P3 | XS | frontend | — |
| [B-19](B-19-schuelerprofil-ki-lehrplan.md) | Schülerprofil-getriebener KI-Lehrplan | Wunsch | `abgenommen` | P2 | M | backend | Vertrag |
| [B-190](B-190-backlog-index-schreibt-leeren-index.md) | Aus einem Unterverzeichnis aufgerufen, schreibt der Index-Generator einen leeren Index | Defekt | `abgenommen` | P2 | XS | doku | — |
| [B-191](B-191-punktevorbelegung-traegt-den-wert-der-vorigen-uebung.md) | Die Punkte-Vorbelegung trägt den Wert der zuvor angeklickten Übung mit | Defekt | `abgenommen` | P2 | S | frontend | — |
| [B-192](B-192-obergrenze-schuetzt-nur-den-api-pfad.md) | Die Obergrenze der Punkte-Empfehlung schützt den Weg nicht, den der Vater geht | Defekt | `abgenommen` | P3 | M | beides | Vertrag |
| [B-193](B-193-vertragsdoku-nennt-die-alte-null-bedeutung.md) | Das Vertragsdokument nennt für `PointsGoalMet` noch die alte `null`-Bedeutung | Defekt | `abgenommen` | P3 | XS | backend | — |
| [B-194](B-194-plandetail-kinderauswahl-ohne-rueckfall.md) | Die Kind-Auswahl im Plan-Detail hat die Regel von B-172 nicht geerbt | Defekt | `abgenommen` | P3 | S | frontend | — |
| [B-195](B-195-unit-liste-bleibt-waehrend-des-reihenwechsels-waehlbar.md) | Beim Reihenwechsel bleiben die Units der alten Reihe wählbar | Defekt | `abgenommen` | P3 | XS | frontend | — |
| [B-196](B-196-loeschen-meldet-nichts-und-raeumt-nichts.md) | Das Löschen einer Reihe meldet nichts — und lässt die Bestätigung von vorhin stehen | Defekt | `abgenommen` | P2 | S | frontend | — |
| [B-197](B-197-meldungsplatz-wird-beim-absenden-nicht-geraeumt.md) | Der Meldungsplatz wird beim Absenden nicht geräumt — und zeigt dann zwei Wahrheiten | Defekt | `abgenommen` | P2 | M | frontend | — |
| [B-198](B-198-teilstring-falle-im-selben-commit-neu-gebaut.md) | Die Teilstring-Falle aus B-189 wurde im selben Commit neu gebaut | Defekt | `abgenommen` | P2 | XS | frontend | — |
| [B-199](B-199-drei-behauptungen-die-ihr-eigener-sprint-aufgehoben-hat.md) | Drei Behauptungen, die ihr eigener Sprint aufgehoben hat | Aufräumen | `abgenommen` | P3 | XS | frontend | — |
| [B-200](B-200-drei-stumme-erfolgspfade.md) | Drei Löschwege melden gar nichts | Defekt | `abgenommen` | P3 | S | frontend | — |
| [B-201](B-201-feldhilfe-argumentiert-in-beide-richtungen.md) | Die Feldhilfe zu den Zielpunkten argumentiert seit Sprint 2 in beide Richtungen | Defekt | `abgenommen` | P3 | XS | frontend | — |
| [B-202](B-202-agent-vorgabewert-halbiert-die-zielbelohnung.md) | Der KI-Creator halbiert die Zielbelohnung jeder erzeugten Übung | Defekt | `abgenommen` | P2 | XS | backend | — |
| [B-203](B-203-platzhalter-luegt-auch-ohne-fehler.md) | Der Platzhalter lügt auch ohne Fehler — und ein misslungenes Lesen nahm dem Vater eine Fähigkeit | Defekt | `abgenommen` | P2 | S | frontend | — |
| [B-204](B-204-vierter-weg-in-den-doppelten-meldungsplatz.md) | Der vierte Weg in denselben Widerspruch: die Reihen-Zeile hat zwei Meldungsplätze | Defekt | `abgenommen` | P2 | XS | frontend | — |
| [B-206](B-206-einkleidung-ist-unsichtbar-und-nicht-korrigierbar.md) | Die Einkleidung einer Übung ist im Vater-Web unsichtbar — und ein gelöschtes Stichwort macht das Speichern unmöglich | Defekt | `abgenommen` | P2 | M | beides | — |
| [B-209](B-209-savednotice-raeumt-nie.md) | „Gespeichert" bleibt über jedem folgenden Bildschirm der Arcade stehen | Defekt | `abgenommen` | P2 | S | frontend | — |
| [B-210](B-210-dialog-haelt-zwei-wahrheiten-fuer-die-config.md) | „Beschreibung speichern" dreht den gerade gespeicherten Inhalt lautlos zurück | Defekt | `abgenommen` | P1 | M | frontend | — |
| [B-211](B-211-interestchips-zusicherung-kann-nicht-fallen.md) | Eine Zusicherung prüft den Ausgangszustand und kann darum nie fallen | Defekt | `abgenommen` | P3 | XS | frontend | — |
| [B-212](B-212-fuenfter-meldungsplatz-in-lehrwerke.md) | Auf der Lehrwerke-Seite stehen rot und grün gleichzeitig — der Weg ist nur ein anderer | Defekt | `abgenommen` | P3 | M | frontend | — |
| [B-214](B-214-store-zeile-per-teilstring-und-position.md) | Die Store-Zeile wird per Teilstring **und** Position gegriffen — dieselbe Bauart wie B-153 | Defekt | `abgenommen` | P3 | XS | frontend | — |
| [B-215](B-215-typ-manifest-ohne-fehlerkanal.md) | Das Typ-Manifest schluckt seinen Ausfall — und niemand kann ihn anzeigen | Defekt | `abgenommen` | P3 | S | frontend | — |
| [B-216](B-216-wiederholknopf-haengt-sich-aus.md) | Zwei Wiederhol-Knöpfe verschwinden im Moment des Klicks unter dem Finger | Defekt | `abgenommen` | P3 | S | frontend | — |
| [B-224](B-224-verlagsbaustein-traegt-zweiten-meldungsplatz.md) | Der Verlags-Baustein trägt auf der Lehrwerke-Seite einen zweiten Meldungsplatz | Defekt | `abgenommen` | P3 | S | frontend | — |
| [B-225](B-225-reihe-anlegen-schweigt-bei-leerem-namen.md) | „Reihe anlegen“ ist bei leerem Namen klickbar und schweigt | Defekt | `abgenommen` | P3 | XS | frontend | — |
| [B-227](B-227-fehlergedaechtnis-ueberlebt-den-filterwechsel.md) | Das Fehler-Gedächtnis überlebt auch den Start einer *anderen* Abfrage | Defekt | `abgenommen` | P3 | S | frontend | — |
| [B-228](B-228-mehr-laden-verliert-den-fokus.md) | „Mehr laden“ in der gefüllten Verlaufskarte verliert den Fokus | Defekt | `abgenommen` | P3 | XS | frontend | — |
| [B-229](B-229-nicht-geladen-waehrend-des-ladens.md) | Das Kind-Feld im Plan-Formular sagt „(nicht geladen)“, solange die Liste erst lädt | Defekt | `abgenommen` | P3 | S | frontend | — |
| [B-235](B-235-unit-feld-sagt-nicht-geladen-waehrend-des-ladens.md) | Das Unit-Feld im Lehrbuch-Formular sagt „(nicht geladen)“, solange die Units erst laden | Defekt | `abgenommen` | P3 | XS | frontend | — |
| [B-237](B-237-ki-creator-hilfe-nennt-ein-kapitel.md) | Die Hilfe des KI-Creators bietet `--chapter <id>` an – ein Kapitel gibt es seit B-106 nicht mehr | Defekt | `abgenommen` | P3 | S | backend | — |
| [B-238](B-238-rest-tutorial-des-creators-faehrt-die-tote-kapitel-route.md) | Das ausführbare REST-Tutorial des Creators fährt die Kapitel-Route, die es seit B-106 nicht gibt | Defekt | `abgenommen` | P2 | S | doku | — |
| [B-239](B-239-wiki-beispiele-tragen-deutsche-xml-docs.md) | Die Erweitern-Anleitung im Wiki zeigt deutsche `/// <summary>` – entgegen der Regel „Code-Doku auf Englisch" | Aufräumen | `abgenommen` | P3 | XS | doku | — |
| [B-240](B-240-index-meldet-fehlende-groesse-und-wo-nie.md) | Der Index meldet fehlende `groesse`/`wo` nie – und verschluckt die Warnzeile, wenn nur leere Dateien da sind | Defekt | `abgenommen` | P2 | XS | doku | — |
| [B-241](B-241-kapitel-ebene-in-tutorials-und-einem-kommentar-uebrig.md) | B-166 hat die Kapitel-Ebene aus der Wissenskarte entfernt – in drei Tutorials und einem Plan steht sie noch, und ein Kommentar wurde dabei falsch | Defekt | `abgenommen` | P3 | XS | doku | — |
| [B-242](B-242-zweite-rest-datei-faehrt-die-tote-kapitel-route.md) | Eine zweite `.http`-Datei unter `docs/REST/Creator/` fährt die tote Kapitel-Route – mit eingebranntem Token | Defekt | `abgenommen` | P3 | XS | doku | — |
| [B-243](B-243-testapi-kommentar-und-vierte-admin-helfer-fassung.md) | B-179 hat den Admin-Helfer geteilt – die Summary behauptet die Gegenrichtung des gemessenen Effekts, und eine vierte Fassung blieb stehen | Defekt | `abgenommen` | P3 | XS | backend | — |
| [B-244](B-244-publishers-create-baut-die-antwort-von-hand.md) | `PublishersController.Create` baut die Antwort von Hand, `Update` liest über `Project` zurück | Aufräumen | `abgenommen` | P3 | XS | backend | — |
| [B-246](B-246-b-241-zwei-saetze-falsch-und-sweep-zu-frueh.md) | B-241 hat zwei Sätze beim Wortwechsel falsch gemacht und den Sweep vor der letzten Änderung gemessen | Defekt | `abgenommen` | P3 | XS | doku | — |
| [B-25](B-25-vite-pwa-peer-konflikt.md) | Peer-Konflikt `vite-plugin-pwa` ↔ `vite@8` lösen | Aufräumen | `abgenommen` | P3 | XS | frontend | — |
| [B-256](B-256-sprint-5-drei-saetze-nach-der-abnahme.md) | Drei Sätze aus Sprint 5, die nach der Abnahme nicht stimmen | Defekt | `abgenommen` | P3 | XS | beides | — |
| [B-26](B-26-e2e-in-ci.md) | Der E2E-Nachtlauf ist rot – und niemand erfährt es | Defekt | `abgenommen` | P1 | S | frontend | — |
| [B-27](B-27-testsuite-grenzfaelle.md) | Die Grenzen des `ScoringService` als Tabelle statt als Flow | Aufräumen | `abgenommen` | P2 | S | backend | — |
| [B-32](B-32-father-tabellenname.md) | `Father` heißt noch `Father`, obwohl die Zeile `Adult` ist | Aufräumen | `abgenommen` | P3 | S | backend | — |
| [B-35](B-35-karten-umdrehen-animation.md) | Karten drehen sich beim Aufdecken um | Wunsch | `abgenommen` | P3 | S | frontend | — |
| [B-36](B-36-motivations-animationen-teilziele.md) | Motivations-Animationen bei erreichten Teilzielen | Wunsch | `abgenommen` | P3 | S | frontend | — |
| [B-37](B-37-uebung-abbruch-unvollendet.md) | Abgebrochene Runden: Pflicht härten, Klausur deckeln | Defekt | `abgenommen` | P1 | M | beides | — |
| [B-40](B-40-client-routen-waechter.md) | Routen aus `Pugling.Client` gegen das OpenAPI-Dokument halten | Aufräumen | `abgenommen` | P3 | XS | backend | — |
| [B-41](B-41-produktions-startup-smoke.md) | Der Produktionspfad des Starts ist der einzige ohne Test | Aufräumen | `abgenommen` | P2 | S | backend | — |
| [B-42](B-42-openapi-typen-generieren.md) | TypeScript-Typen aus dem OpenAPI-Dokument erzeugen statt von Hand pflegen | Aufräumen | `abgenommen` | P2 | M | beides | — |
| [B-43](B-43-frontend-komponententests.md) | Die Doppelklick-Lücke in `useAction` – und die fehlende Ebene für unsichtbare Zusicherungen | Defekt | `abgenommen` | P3 | M | frontend | — |
| [B-44](B-44-grundprinzip-rollennamen.md) | Grundprinzip auf Supervisor/Student umschreiben — „Vater" ist keine Ebene | Aufräumen | `abgenommen` | P2 | XS | doku | — |
| [B-45](B-45-creator-punkte-empfehlung.md) | Die Punkte-Empfehlung des Creators soll der Supervisor übernehmen können | Wunsch | `abgenommen` | P2 | S | beides | — |
| [B-46](B-46-interessenbasierte-uebungen.md) | Übungen entstehen für ein Interessenprofil, nicht für ein bestimmtes Kind | Wunsch | `abgenommen` | P2 | L | beides | Migration |
| [B-48](B-48-anonyme-registrierung-produktion.md) | Anonyme Registrierung ist auch in Produktion offen | Frage | `abgenommen` | P2 | S | backend | — |
| [B-49](B-49-sohn-app-schreib-primitive.md) | Die Sohn-App benutzt die geteilten Schreib-Primitive nicht | Aufräumen | `abgenommen` | P3 | S | frontend | — |
| [B-50](B-50-kind-beschreibt-sich-selbst.md) | Das Kind beschreibt sich selbst: Interessen in einem geführten Prozess | Wunsch | `abgenommen` | P2 | L | beides | — |
| [B-51](B-51-admin-rolle-dokumentieren.md) | Die Admin-Rolle kommt in keinem Rollen-Dokument vor | Aufräumen | `abgenommen` | P3 | XS | doku | — |
| [B-52](B-52-testabdeckung-paket.md) | Sammel-Story: das Testabdeckungs-Paket | Aufräumen | `abgenommen` | P2 | L | beides | — |
| [B-53](B-53-wizard-doppelklick.md) | Zwei Klicks im Lehrplan-Assistenten legen zwei Kinder und zwei Pläne an | Defekt | `abgenommen` | P2 | S | frontend | — |
| [B-54](B-54-objectivecard-schreib-primitive.md) | Fünf Knöpfe im Vater-Web gehen an den Schreib-Primitiven vorbei | Defekt | `abgenommen` | P2 | S | frontend | — |
| [B-55](B-55-wegwerf-dateien-aufraeumen.md) | Die Tests räumen ihre Wegwerf-Dateien nicht weg | Aufräumen | `abgenommen` | P3 | S | beides | — |
| [B-56](B-56-problemdetails-required-extensions.md) | `ProblemDetails` fordert im Schema ein Feld, das es nicht beschreibt | Defekt | `abgenommen` | P3 | S | backend | — |
| [B-57](B-57-beispielkatalog-schreib-lese-rennen.md) | Im Testlauf lesen und schreiben zwei Stellen gleichzeitig dieselbe Katalogdatei | Defekt | `abgenommen` | P3 | S | backend | — |
| [B-58](B-58-assistent-e2e.md) | Der Lehrplan-Assistent hat keinen Durchstich | Aufräumen | `abgenommen` | P2 | S | frontend | — |
| [B-59](B-59-status-strings-ohne-werteliste.md) | Zwei Antwortfelder tragen einen Status als nackten `string` | Aufräumen | `abgenommen` | P3 | S | beides | Vertrag |
| [B-60](B-60-flags-enum-im-dokument.md) | Das Vertragsdokument verbietet einen `SchoolTypes`-Wert, den Server und Frontend täglich austauschen | Defekt | `abgenommen` | P2 | S | backend | — |
| [B-61](B-61-reste-der-schreib-primitiven-runde.md) | Zwei Reste aus der Schreib-Primitiven-Runde | Defekt | `abgenommen` | P3 | S | frontend | — |
| [B-62](B-62-reste-aus-dem-b37-review.md) | Drei Reste aus dem B-37-Review (Sohn-Arcade) | Defekt | `abgenommen` | P3 | S | beides | — |
| [B-63](B-63-lehrwerk-hierarchie.md) | Das Lehrwerk ist eine Ebene aus Freitext, gebraucht wird eine Hierarchie mit Listen | Wunsch | `abgenommen` | P2 | L | beides | Migration + Vertrag |
| [B-64](B-64-textbook-vs-series.md) | Das Lehrwerk gibt es zweimal: einmal als Freitext am Kind, einmal katalogisiert | Wunsch | `abgenommen` | P3 | M | beides | — |
| [B-65](B-65-vokabel-mehrere-uebersetzungen.md) | Eine Vokabel mit zwei richtigen Übersetzungen wertet eine davon falsch | Defekt | `abgenommen` | P1 | M | beides | Migration |
| [B-66](B-66-buchstabenkaestchen-trennzeichen.md) | Das Buchstabenkästchen lässt Leer- und Satzzeichen tippen, die schon feststehen | Defekt | `abgenommen` | P2 | M | beides | — |
| [B-67](B-67-fachlehrer-aus-lehrwerk.md) | Der Fachlehrer fragt nach Fach und Sprachen, die im gewählten Lehrwerk längst stehen | Wunsch | `abgenommen` | P2 | S | frontend | — |
| [B-69](B-69-wiederhol-felder-alternativen.md) | Kommagetrennte Sammelfelder: einer davon nimmt gar keine zweite Alternative an | Defekt | `abgenommen` | P2 | M | frontend | — |
| [B-70](B-70-selbsteinschaetzung-nur-primaerloesung.md) | Die Selbsteinschätzung zeigt nur die primäre Übersetzung | Defekt | `abgenommen` | P2 | S | beides | — |
| [B-71](B-71-inline-vokabelliste-ohne-varianten.md) | Die Inline-Vokabelliste im Übungs-Editor kann keine gleichwertigen Übersetzungen anlegen | Wunsch | `abgenommen` | P3 | M | beides | — |
| [B-72](B-72-birkenbihl-dekodierung-paarfelder.md) | Die Birkenbihl-Dekodierung trägt zwei Trennzeichen in einem Feld | Defekt | `abgenommen` | P3 | S | frontend | — |
| [B-73](B-73-auswahl-feld-ohne-wirkung.md) | Das Auswahl-Feld verspricht Multiple-Choice, das Kind bekommt Freitext | Defekt | `abgenommen` | P2 | S | beides | — |
| [B-74](B-74-editor-zeilen-typisieren.md) | Die Zeilen des Übungs-Editors sind `Record<string, any>` | Aufräumen | `abgenommen` | P3 | M | frontend | — |
| [B-75](B-75-lese-hoerverstehen-ohne-inhalt.md) | Lese- und Hörverstehen kommen ohne ihren Inhalt beim Kind an | Defekt | `abgenommen` | P1 | M | beides | Vertrag |
| [B-76](B-76-lueckentext-karte-ohne-luecke.md) | Der Lückentext sagt dem Kind nicht, welche Lücke gemeint ist | Defekt | `abgenommen` | P1 | M | beides | — |
| [B-77](B-77-liste-menge-als-folge.md) | Beim Spielen wird eine ungeordnete Liste als Folge bewertet | Defekt | `abgenommen` | P1 | M | beides | Vertrag |
| [B-78](B-78-birkenbihl-dekodierung-erreicht-kind-nicht.md) | Die Birkenbihl-Dekodierung erreicht das Kind nicht | Defekt | `abgenommen` | P2 | M | beides | — |
| [B-79](B-79-position-stufe-unvalidiert.md) | Die Stufe einer Position wird gegen nichts geprüft | Defekt | `abgenommen` | P2 | S | backend | — |
| [B-80](B-80-tags-geben-fremde-konfiguration-preis.md) | Das Kind kann die Lösungen jeder Übung lesen | Defekt | `abgenommen` | P1 | S | backend | Vertrag |
| [B-81](B-81-vokabel-tags-geben-uebersetzungen-preis.md) | Über die Vokabel-Tags kann ein Kind jede Übersetzung des Stores lesen | Defekt | `abgenommen` | P1 | S | backend | — |
| [B-82](B-82-positions-report-gibt-loesungen-preis.md) | Über den Positions-Report kann ein Kind die Lösung jeder Karte lesen | Defekt | `abgenommen` | P1 | M | beides | Vertrag |
| [B-83](B-83-loesungsfeld-regel-residenter-kontext.md) | Die Lösungsfeld-Regel steht nur als Kommentar am Wächter | Aufräumen | `abgenommen` | P3 | XS | doku | — |
| [B-84](B-84-api-beispiele-behaupten-unerreichbarkeit.md) | Die API-Beispiele behaupten Unerreichbarkeit, wo nur nichts mitgeschnitten wurde | Defekt | `abgenommen` | P3 | S | doku | — |
| [B-88](B-88-scoring-uhrzeit-am-timeprovider.md) | Die Punkte-Uhrzeit kommt von der Wanduhr, nicht vom `TimeProvider` | Aufräumen | `abgenommen` | P3 | XS | backend | — |
| [B-89](B-89-positionsliste-haengt-report-aus.md) | Die Positionsliste hängt bei jeder Änderung den aufgeklappten Report aus | Defekt | `abgenommen` | P3 | L | frontend | — |
| [B-93](B-93-birkenbihl-einstellungen-ohne-wirkung.md) | Zwei Birkenbihl-Einstellungen, die lautlos nichts tun | Defekt | `abgenommen` | P3 | S | beides | — |
| [B-95](B-95-stufenwaechter-haengt-am-include.md) | Die Stufenprüfung beim PATCH einer Position hängt an einem `Include`, das niemand einfordert | Aufräumen | `abgenommen` | P3 | XS | backend | — |
| [B-96](B-96-showboth-stufe-ohne-mechanik.md) | „Beide zeigen (Kennenlernen)" ist eine Beschriftung ohne eigene Stufe | Defekt | `abgenommen` | P2 | M | beides | — |
| [B-97](B-97-unique-index-ohne-vorpruefung.md) | Zwei Schreibpfade laufen ungeprüft in einen Unique-Index und antworten mit 500 | Defekt | `abgenommen` | P2 | XS | backend | — |
| [B-98](B-98-idempotenter-link-post-luegt.md) | Drei idempotente Schreibpfade antworten mit erfundenen Werten, zwei davon mit `201 Created` | Defekt | `abgenommen` | P2 | S | backend | Vertrag |
| [B-99](B-99-kaufhistorie-endet-lautlos.md) | Die Kaufhistorie des Kindes endet lautlos bei 50 Zeilen | Defekt | `abgenommen` | P2 | S | beides | — |

</details>

### Wartet auf Zutun von außen (11)

Diese Stories kommen **im Repo nicht weiter** — es fehlt ein Schritt, den nur ein Mensch
oder ein Werkzeug außerhalb tun kann. Nicht „in Arbeit" im Sinne von „wird gerade gebaut".

| Id | Story | Stufe | Wartet auf |
| --- | --- | --- | --- |
| [B-12](B-12-uebungen-kuratieren.md) | Geteilte Übungen bewerten und kuratieren | `in-arbeit` | Nutzer: Zähler Sprint 12 überschritten (Freigabe 3), Funde 6 und 7 offen |
| [B-134](B-134-bedingte-live-regionen.md) | Dreizehn Live-Regionen entstehen zusammen mit ihrem Text — und schweigen darum | `in-arbeit` | Nutzer: Nachtlauf an Freigabe 3 beendet (Sprint 10) – frontend-reviewer für B-134/B-236 nicht gelaufen |
| [B-15](B-15-testmodus-weitere-typen.md) | Vorschau für die nicht-prüfbaren Übungstypen | `in-arbeit` | Nutzer: Nachtlauf an Freigabe 3 beendet (6 Review-Funde in Sprint 7) – offene Funde im Verlauf |
| [B-17](B-17-birkenbihl-sprachcodes.md) | Sprachcode-Normalisierung bei der Vokabel-Dekodierung | `in-arbeit` | Nutzer: Nachtlauf an Freigabe 3 beendet (6 Review-Funde in Sprint 7) – offene Funde im Verlauf |
| [B-21](B-21-ki-creator-foerdermodus.md) | KI-Creator: Fördermodus (`--use-weak`) sprachrein und mit Tests absichern | `in-arbeit` | Nutzer: Nachtlauf an Freigabe 3 beendet (6 Review-Funde in Sprint 7) – offene Funde im Verlauf |
| [B-236](B-236-checkbox-gruppe-ohne-zugaenglichen-gruppennamen.md) | Eine Gruppen-Überschrift über Kontrollkästchen ist sichtbar, aber für den Screenreader an nichts gebunden | `in-arbeit` | Nutzer: Nachtlauf an Freigabe 3 beendet (Sprint 10) – frontend-reviewer für B-134/B-236 nicht gelaufen |
| [B-263](B-263-info-popover-steht-am-rechten-rand-ueber.md) | Ein aufgeklapptes ⓘ am rechten Rand lässt die Seite seitlich scrollen | `in-arbeit` | Nutzer: Nachtlauf an Freigabe 3 beendet (Sprint 10, Fund 6 aus dem Delta-Review) – offene Funde im Verlauf |
| [B-267](B-267-entkoppeltes-lehrbuch-zeigt-katalogwert-im-freitextfeld.md) | Nach dem Entkoppeln zeigt das Freitextfeld den Katalogwert – gespeichert wird er nicht | `idee` | Nutzer: Entscheidung, ob die Antwort den gespeicherten Freitext getrennt mitliefert |
| [B-31](B-31-geraete-vorbehalt-klang.md) | Geräte-Vorbehalt: Klang und Haptik am echten Handy gegenhören | `geschaetzt` | ein echtes Handy — Klang und Haptik sind nicht maschinell zu beurteilen |
| [B-47](B-47-deploy-artefakt-smoke.md) | Startet das veröffentlichte Artefakt überhaupt? | `geschaetzt` | den Neubau des Azure-Deploys (B-07 ist verworfen, es gibt keine Alt-Instanz) |
| [B-68](B-68-vater-web-responsive.md) | Das Vater-Web hat keinen einzigen eigenen Breakpoint | `in-arbeit` | Nutzer: Nachtlauf an Freigabe 3 beendet (Sprint 10, Fund 6 aus dem Delta-Review) – offene Funde im Verlauf |

### Nach der Abnahme entgangen (61)

**Nachgeschaut: 182 von 184 abgenommenen** — und in 50 davon steckte ein Defekt, der bei der Abnahme durchgekommen war. Der Nenner ist die Zahl der *geprüften*, nicht der abgenommenen Stories; die übrigen 2 sind **unbeobachtet**, nicht sauber.

| Defekt | Titel | Entgangen bei | Stufe |
| --- | --- | --- | --- |
| [B-110](B-110-kaufverlauf-ueberspringt-zeilen.md) | Der Kaufverlauf überspringt Zeilen und verpasst den eigenen Kauf | [B-99] | `abgenommen` |
| [B-111](B-111-verlauf-luegt-im-fehlerfall.md) | Scheitert das Laden des Verlaufs, sagt die App „Noch nichts gekauft" | [B-99] | `abgenommen` |
| [B-114](B-114-showboth-position-unspielbar.md) | Eine Kennenlern-Position hatte für das Kind keinen einzigen Knopf — und kostete Münzen | [B-96] | `abgenommen` |
| [B-115](B-115-buchstabenkaestchen-index-drift.md) | Übersprang das Kind ein Buchstabenkästchen, rutschten alle folgenden Zeichen | [B-66] | `abgenommen` |
| [B-116](B-116-blaettern-ohne-rueckmeldung.md) | Beim Blättern gibt es keine Rückmeldung mehr — und der Pager meldet eine Seite, die noch nicht da ist | [B-89] | `abgenommen` |
| [B-117](B-117-uebungsbildschirm-bietet-test-trotz-anzeigestufe.md) | Nach der Übungsrunde bietet der Bildschirm einen Test an, den es für diese Stufe nicht gibt | [B-114] | `abgenommen` |
| [B-124](B-124-umbenennen-umgeht-die-eindeutigkeit.md) | Anlegen schützt die Eindeutigkeit, Umbenennen umgeht sie | [B-63] | `abgenommen` |
| [B-125](B-125-forwarded-proto-fehlt.md) | Die App weiß hinter dem Proxy, wer ruft — aber nicht, dass er über HTTPS ruft | [B-119] | `abgenommen` |
| [B-126](B-126-ableitung-behauptet-falsche-herkunft.md) | „aus dem Lehrwerk übernommen" steht auch unter Werten, die nicht von dort kommen | [B-67] | `abgenommen` |
| [B-128](B-128-katalogsuche-case-sensitiv.md) | Die Katalogsuche findet „KLETT" nicht, obwohl „Klett" da ist | [B-63] | `abgenommen` |
| [B-129](B-129-themenfeld-committet-beim-verlassen.md) | Das Themenfeld legt an, was beim Wegklicken gerade dasteht | [B-63] | `abgenommen` |
| [B-132](B-132-hinweis-live-region-haengt-aus.md) | Der Hinweis „aus dem Lehrwerk übernommen" wird angesagt, indem seine Live-Region entsteht | [B-67] | `abgenommen` |
| [B-133](B-133-zwei-reihen-ein-anzeigename.md) | Nach einer Umbenennung können zwei Reihen denselben Anzeigenamen tragen | [B-124] | `abgenommen` |
| [B-139](B-139-e2e-nachtlauf-login-bricht-ab-test-2.md) | Der Aufräum-Sweep der E2E löschte das Journal seiner eigenen Datenbank | [B-55] | `abgenommen` |
| [B-150](B-150-verlagssperre-unsichtbar-dialog-verspricht-gegenteil.md) | Die Verlags-Löschsperre war für den Vater unsichtbar — der Dialog versprach das Gegenteil | [B-127] | `abgenommen` |
| [B-160](B-160-gesperrter-knopf-nennt-den-grund-nie.md) | Der gesperrte Löschen-Knopf nennt seinen Grund nie — der `title` erscheint nicht | [B-150] | `abgenommen` |
| [B-161](B-161-alle-waehlen-macht-die-auswahl-unsichtbar.md) | „Alle wählen" wählt bis zu 400 Übungen, die der Vater nie sieht und nicht abwählen kann | [B-18] | `abgenommen` |
| [B-168](B-168-eigentuemer-zusicherung-prueft-eine-konstante.md) | „Anlegen macht den Aufrufer zum Eigentümer" prüft eine Konstante, keine Identität | [B-13] | `abgenommen` |
| [B-169](B-169-ladefenster-macht-die-alten-zeilen-anklickbar.md) | Im Ladefenster sind die alten Zeilen anklickbar — und die Auswahl überlebt | [B-161] | `abgenommen` |
| [B-170](B-170-selbst-angelegte-art-im-grundbestand-ist-unloeschbar.md) | Eine selbst angelegte Art im Grundbestands-Fach ist unlöschbar — und der Seitentext verspricht das Gegenteil | [B-157] | `abgenommen` |
| [B-171](B-171-zwei-zusicherungen-pruefen-den-ausgangszustand.md) | Zwei Zusicherungen prüfen den Ausgangszustand mit — und können darum nicht fallen | [B-161, B-157] | `abgenommen` |
| [B-172](B-172-fachauswahl-loescht-wenn-ihre-optionen-fehlen.md) | Die Fachauswahl löscht das Fach, wenn ihre eigenen Optionen nicht geladen sind | [B-123, B-148] | `abgenommen` |
| [B-173](B-173-speichern-vernichtet-seine-eigene-bestaetigung.md) | Ein erfolgreiches Speichern vernichtet seine eigene Bestätigung | [B-123] | `abgenommen` |
| [B-174](B-174-arten-liste-verschweigt-ihren-fehler.md) | Die Arten-Liste verschweigt, dass ihr Laden gescheitert ist | [B-154] | `abgenommen` |
| [B-175](B-175-zwei-gerottete-zahlen-in-kommentaren.md) | Zwei Zahlen in Kommentaren widersprechen dem, was gemessen wurde | [B-139] | `abgenommen` |
| [B-176](B-176-gruppen-ueberschrift-verliert-die-feld-optik.md) | Eine Gruppen-Überschrift verliert ihre Optik, sobald sie kein `<label>` mehr ist | [B-11] | `abgenommen` |
| [B-178](B-178-katalogtext-verspricht-mehr-als-der-server-erlaubt.md) | Der Katalogtext verspricht mehr als der Server erlaubt | [B-157] | `abgenommen` |
| [B-180](B-180-zusicherung-haengt-an-scheduling-statt-am-gate.md) | Eine Zusicherung hing an 20 ms Scheduling statt am Gate | [B-169] | `abgenommen` |
| [B-182](B-182-reichweite-nur-am-patch-festgenagelt.md) | Die Reichweite des Ventils war nur am PATCH festgenagelt | [B-178] | `abgenommen` |
| [B-189](B-189-teilstring-zusicherung-bricht-im-vollen-lauf.md) | Eine Teilstring-Zusicherung bricht, sobald der Leer-Text daneben stehen bleibt | [B-148] | `abgenommen` |
| [B-191](B-191-punktevorbelegung-traegt-den-wert-der-vorigen-uebung.md) | Die Punkte-Vorbelegung trägt den Wert der zuvor angeklickten Übung mit | [B-45] | `abgenommen` |
| [B-192](B-192-obergrenze-schuetzt-nur-den-api-pfad.md) | Die Obergrenze der Punkte-Empfehlung schützt den Weg nicht, den der Vater geht | [B-45] | `abgenommen` |
| [B-193](B-193-vertragsdoku-nennt-die-alte-null-bedeutung.md) | Das Vertragsdokument nennt für `PointsGoalMet` noch die alte `null`-Bedeutung | [B-45] | `abgenommen` |
| [B-195](B-195-unit-liste-bleibt-waehrend-des-reihenwechsels-waehlbar.md) | Beim Reihenwechsel bleiben die Units der alten Reihe wählbar | [B-172] | `abgenommen` |
| [B-196](B-196-loeschen-meldet-nichts-und-raeumt-nichts.md) | Das Löschen einer Reihe meldet nichts — und lässt die Bestätigung von vorhin stehen | [B-173] | `abgenommen` |
| [B-197](B-197-meldungsplatz-wird-beim-absenden-nicht-geraeumt.md) | Der Meldungsplatz wird beim Absenden nicht geräumt — und zeigt dann zwei Wahrheiten | [B-173, B-151] | `abgenommen` |
| [B-198](B-198-teilstring-falle-im-selben-commit-neu-gebaut.md) | Die Teilstring-Falle aus B-189 wurde im selben Commit neu gebaut | [B-151] | `abgenommen` |
| [B-201](B-201-feldhilfe-argumentiert-in-beide-richtungen.md) | Die Feldhilfe zu den Zielpunkten argumentiert seit Sprint 2 in beide Richtungen | [B-191] | `abgenommen` |
| [B-202](B-202-agent-vorgabewert-halbiert-die-zielbelohnung.md) | Der KI-Creator halbiert die Zielbelohnung jeder erzeugten Übung | [B-45] | `abgenommen` |
| [B-203](B-203-platzhalter-luegt-auch-ohne-fehler.md) | Der Platzhalter lügt auch ohne Fehler — und ein misslungenes Lesen nahm dem Vater eine Fähigkeit | [B-184, B-63] | `abgenommen` |
| [B-204](B-204-vierter-weg-in-den-doppelten-meldungsplatz.md) | Der vierte Weg in denselben Widerspruch: die Reihen-Zeile hat zwei Meldungsplätze | [B-197] | `abgenommen` |
| [B-209](B-209-savednotice-raeumt-nie.md) | „Gespeichert" bleibt über jedem folgenden Bildschirm der Arcade stehen | [B-50] | `abgenommen` |
| [B-211](B-211-interestchips-zusicherung-kann-nicht-fallen.md) | Eine Zusicherung prüft den Ausgangszustand und kann darum nie fallen | [B-206] | `abgenommen` |
| [B-224](B-224-verlagsbaustein-traegt-zweiten-meldungsplatz.md) | Der Verlags-Baustein trägt auf der Lehrwerke-Seite einen zweiten Meldungsplatz | [B-212] | `abgenommen` |
| [B-225](B-225-reihe-anlegen-schweigt-bei-leerem-namen.md) | „Reihe anlegen“ ist bei leerem Namen klickbar und schweigt | [B-212] | `abgenommen` |
| [B-227](B-227-fehlergedaechtnis-ueberlebt-den-filterwechsel.md) | Das Fehler-Gedächtnis überlebt auch den Start einer *anderen* Abfrage | [B-216] | `abgenommen` |
| [B-228](B-228-mehr-laden-verliert-den-fokus.md) | „Mehr laden“ in der gefüllten Verlaufskarte verliert den Fokus | [B-216] | `abgenommen` |
| [B-229](B-229-nicht-geladen-waehrend-des-ladens.md) | Das Kind-Feld im Plan-Formular sagt „(nicht geladen)“, solange die Liste erst lädt | [B-194] | `abgenommen` |
| [B-235](B-235-unit-feld-sagt-nicht-geladen-waehrend-des-ladens.md) | Das Unit-Feld im Lehrbuch-Formular sagt „(nicht geladen)“, solange die Units erst laden | [B-229] | `abgenommen` |
| [B-240](B-240-index-meldet-fehlende-groesse-und-wo-nie.md) | Der Index meldet fehlende `groesse`/`wo` nie – und verschluckt die Warnzeile, wenn nur leere Dateien da sind | [B-131] | `abgenommen` |
| [B-241](B-241-kapitel-ebene-in-tutorials-und-einem-kommentar-uebrig.md) | B-166 hat die Kapitel-Ebene aus der Wissenskarte entfernt – in drei Tutorials und einem Plan steht sie noch, und ein Kommentar wurde dabei falsch | [B-166] | `abgenommen` |
| [B-243](B-243-testapi-kommentar-und-vierte-admin-helfer-fassung.md) | B-179 hat den Admin-Helfer geteilt – die Summary behauptet die Gegenrichtung des gemessenen Effekts, und eine vierte Fassung blieb stehen | [B-179] | `abgenommen` |
| [B-246](B-246-b-241-zwei-saetze-falsch-und-sweep-zu-frueh.md) | B-241 hat zwei Sätze beim Wortwechsel falsch gemacht und den Sweep vor der letzten Änderung gemessen | [B-241] | `abgenommen` |
| [B-256](B-256-sprint-5-drei-saetze-nach-der-abnahme.md) | Drei Sätze aus Sprint 5, die nach der Abnahme nicht stimmen | [B-237, B-244, B-246] | `abgenommen` |
| [B-257](B-257-zielfeier-verspaetet-jenseits-der-ersten-fuenf.md) | Ein schon erreichtes Ziel jenseits der ersten fünf wird später verspätet gefeiert | [B-36] | `idee` |
| [B-260](B-260-normalisierer-setzt-absatz-mitten-im-satz.md) | Der OpenAPI-Normalisierer setzt einen Absatz mitten in einen Satz | [B-147] | `idee` |
| [B-261](B-261-produktionspfad-des-beispielkatalogs-ungetestet.md) | Der Produktionspfad des Beispielkatalogs hängt an keinem Test | [B-181] | `idee` |
| [B-262](B-262-vorwahl-bestehendes-kind-ohne-eigene-spec.md) | „Ohne Eingabe bleibt die Vorwahl ‚bestehendes Kind‘“ hält keine Spec gezielt | [B-177] | `idee` |
| [B-264](B-264-interessen-ohne-kind-kleiden-nicht-ein.md) | `--interests` ohne Kind setzt die Tags an die Übung, kleidet ihren Inhalt aber nicht ein | [B-46] | `idee` |
| [B-265](B-265-sperr-zaehler-zusicherung-kann-nie-fallen.md) | Die „Spur“ der gesperrten Lesevorgänge ist eine Zusicherung, die nie fallen kann | [B-165] | `idee` |
| [B-266](B-266-wartepunkt-netzwerk-statt-app-zustand.md) | Der Wartepunkt aus B-180 wartet aufs Netzwerk, nicht auf die App | [B-180] | `idee` |

<details>
<summary>Nie nachgeschaut (2) — Arbeitsvorrat der Nachschau</summary>

Abgenommen, aber nach der Abnahme nie wieder angesehen. Wer hier einen Blick tut, setzt
danach `nachgeschaut: <Datum>` — **auch wenn er nichts gefunden hat**, sonst zählt der Blick nicht.

| Id | Story |
| --- | --- |
| [B-64](B-64-textbook-vs-series.md) | Das Lehrwerk gibt es zweimal: einmal als Freitext am Kind, einmal katalogisiert |
| [B-71](B-71-inline-vokabelliste-ohne-varianten.md) | Die Inline-Vokabelliste im Übungs-Editor kann keine gleichwertigen Übersetzungen anlegen |

</details>

### ⚠ Stufe behauptet, Datei belegt nicht

Diese Stories tragen einen `status`, dessen Eintrittsbedingung in der Datei nicht
vollständig steht. Entweder nachtragen oder die Stufe zurücknehmen.

| Id | Stufe | Fehlt |
| --- | --- | --- |
| [B-139](B-139-e2e-nachtlauf-login-bricht-ab-test-2.md) | `abgenommen` | Abschnitt „Ist-Stand…", Abschnitt „Entscheidungen", Abschnitt „Schätzung" |
| [B-146](B-146-anlegeformular-schickt-toten-fachnamen.md) | `abgenommen` | Abschnitt „Entscheidungen" |
| [B-153](B-153-bilder-spec-flackert-im-vollen-lauf.md) | `abgenommen` | Abschnitt „User Story", Abschnitt „Ist-Stand…", Abschnitt „Akzeptanzkriterien", Abschnitt „Entscheidungen", Abschnitt „Schätzung" |
| [B-160](B-160-gesperrter-knopf-nennt-den-grund-nie.md) | `abgenommen` | Abschnitt „Schätzung" |
| [B-165](B-165-backend-suite-flackert.md) | `abgenommen` | Abschnitt „User Story", Abschnitt „Ist-Stand…" |
| [B-168](B-168-eigentuemer-zusicherung-prueft-eine-konstante.md) | `abgenommen` | Abschnitt „User Story", Abschnitt „Schätzung" |
| [B-169](B-169-ladefenster-macht-die-alten-zeilen-anklickbar.md) | `abgenommen` | Abschnitt „User Story" |
| [B-171](B-171-zwei-zusicherungen-pruefen-den-ausgangszustand.md) | `abgenommen` | Abschnitt „User Story", Abschnitt „Ist-Stand…" |
| [B-178](B-178-katalogtext-verspricht-mehr-als-der-server-erlaubt.md) | `abgenommen` | Abschnitt „User Story", Abschnitt „Ist-Stand…" |
| [B-180](B-180-zusicherung-haengt-an-scheduling-statt-am-gate.md) | `abgenommen` | Abschnitt „User Story", Abschnitt „Akzeptanzkriterien", Abschnitt „Entscheidungen", Abschnitt „Schätzung" |
| [B-182](B-182-reichweite-nur-am-patch-festgenagelt.md) | `abgenommen` | Abschnitt „User Story", Abschnitt „Akzeptanzkriterien", Abschnitt „Entscheidungen", Abschnitt „Schätzung" |
| [B-187](B-187-unitform-verliert-seine-bestaetigung.md) | `abgenommen` | Abschnitt „Akzeptanzkriterien", Abschnitt „Schätzung" |
| [B-188](B-188-wizard-verschweigt-drei-weitere-ausfaelle.md) | `abgenommen` | Abschnitt „Akzeptanzkriterien", Abschnitt „Schätzung" |
| [B-200](B-200-drei-stumme-erfolgspfade.md) | `abgenommen` | Abschnitt „Akzeptanzkriterien", Abschnitt „Schätzung" |
| [B-209](B-209-savednotice-raeumt-nie.md) | `abgenommen` | Abschnitt „Akzeptanzkriterien", Abschnitt „Schätzung" |
| [B-210](B-210-dialog-haelt-zwei-wahrheiten-fuer-die-config.md) | `abgenommen` | Abschnitt „Akzeptanzkriterien", Abschnitt „Schätzung" |
| [B-212](B-212-fuenfter-meldungsplatz-in-lehrwerke.md) | `abgenommen` | Abschnitt „Akzeptanzkriterien", Abschnitt „Schätzung" |
| [B-236](B-236-checkbox-gruppe-ohne-zugaenglichen-gruppennamen.md) | `in-arbeit` | Abschnitt „User Story", Abschnitt „Ist-Stand…" |
| [B-260](B-260-normalisierer-setzt-absatz-mitten-im-satz.md) | `idee` | `unverifiziert: true` |
| [B-261](B-261-produktionspfad-des-beispielkatalogs-ungetestet.md) | `idee` | `unverifiziert: true` |
| [B-262](B-262-vorwahl-bestehendes-kind-ohne-eigene-spec.md) | `idee` | `unverifiziert: true` |
| [B-264](B-264-interessen-ohne-kind-kleiden-nicht-ein.md) | `idee` | `unverifiziert: true` |
| [B-265](B-265-sperr-zaehler-zusicherung-kann-nie-fallen.md) | `idee` | `unverifiziert: true` |
| [B-267](B-267-entkoppeltes-lehrbuch-zeigt-katalogwert-im-freitextfeld.md) | `idee` | `unverifiziert: true` |
| [B-268](B-268-fokus-nach-inline-reihe.md) | `idee` | `unverifiziert: true` |
| [B-269](B-269-bewertungen-reihenfolge-und-updatedat-ungetestet.md) | `idee` | `unverifiziert: true` |

<details>
<summary>Verworfen (22)</summary>

| Id | Story | Grund |
| --- | --- | --- |
| [B-03](B-03-lueckensaetze-mit-bild.md) | Lückensätze mit Bild als Vokabel-Vertiefung | > |
| [B-06](B-06-cloze-preview-bild.md) | Cloze-Vorschau kann kein Bild zeigen | > |
| [B-07](B-07-db-umbau-restetappen.md) | DB-Struktur-Umbau: der offene Betriebsschritt | gegenstandslos – Entscheidung des Nutzers am 2026-10-23: das Azure-Deployment wird später frisch neu gebaut, es gibt keine Alt-Datenbank, an der der Betriebsschritt nachzuholen wäre |
| [B-103](B-103-idempotenzschluessel-und-etag.md) | Prüfauftrag: Brauchen `Idempotency-Key` und ETag/`If-Match` in dieser App einen Platz? | gemessen in der Arbeitsrunde 2026-08-04 — beim Idempotenz-Schlüssel bleibt ein betroffener Endpunkt von vier übrig und der Rückweg (POST children/{}/points) existiert als Produktverhalten; der ETag-Vorschlag ist gegen den Code unausführbar (StudyPlan/PlanPosition tragen keinen ConcurrencyStamp) und mit der Wallet-Invariante unverträglich. Kein Bau, Entscheidung dokumentiert in backend/Pugling.Api/CLAUDE.md. |
| [B-137](B-137-freitext-fach-unerreichbar.md) | Ein Freitext-Fach an der Reihe ist sichtbar, aber nicht wegzubekommen | geteilt — faktisch XL (sechs Akzeptanzkriterien, drei Controller, Backend und Frontend, dazu eine Flags-Enum-Mehrfachauswahl); Freigabe 3 des Nachtlaufs verlangt dafür Teilen statt Bauen → [B-142, B-143, B-144] |
| [B-14](B-14-learngoal-belohnung.md) | Idempotente Belohnung, wenn ein Lernziel erreicht ist — **gegenstandslos** | erfüllt durch KeyResult/ObjectiveRewardService (DB-Umbau E13) |
| [B-186](B-186-assistent-und-planseite-setzen-zielpunkte-verschieden.md) | Assistent und Plan-Seite setzen die Zielpunkte nach verschiedenen Regeln | bleibt wie es ist – Entscheidung des Nutzers am 2026-10-23 (G-1): im Assistenten gewinnt die gewählte Intensität, weil sie eine ausdrückliche, angezeigte und in der Zusammenfassung bestätigte Aussage des Vaters ist; die Creator-Empfehlung gilt nur auf der Plan-Seite |
| [B-213](B-213-bilder-spec-flackert-im-vollen-lauf.md) | `bilder.spec.ts` fällt im vollen Lauf gelegentlich, allein nie | Dublette zu B-153, das denselben Fall seit 2026-08-11 fuehrt und die Ursache belegt hat → [B-153] |
| [B-222](B-222-wochen-streak-als-default.md) | Prüfauftrag: ist der Tages-Streak der richtige Default? | bleibt wie es ist – Entscheidung des Nutzers am 2026-10-23 (G-6): der Tages-Streak bleibt. Bei Pugling kommt der Druck aus der Pflicht (PenaltyCoins), der Streak ist reines Zuckerbrot (DailyBoxOptions.cs:25, PositionProgressService.cs:405). Khans Argument gegen Tagesdruck trifft darum nicht |
| [B-226](B-226-zwei-muenzstaende-im-shop-bildschirm.md) | Der Shop-Bildschirm zeigt zwei verschiedene Münzstände | gegenstandslos – beide Chips lesen dieselbe Context-Variable `coins` aus `api.wallet()` (SohnApp.tsx:52,64,96; SohnShop.tsx:29,152); zwei verschiedene Werte im selben Render sind unmöglich, abgelesen wurde vermutlich ein anderes Element |
| [B-23](B-23-uebungstyp-plugins-dll.md) | Übungstyp-Plugins als externe DLLs (Stufe 2) | Entscheidung des Nutzers am 2026-10-23: eine Familien-App hat keine externen Plugin-Autoren; Stufe 1 (IExerciseType + Registry) genügt. Die Entscheidungen 2 und 3 schlossen sich zudem aus. Kommt der Bedarf, ist das eine neue Story |
| [B-24](B-24-frontend-unknown-field.md) | Frontend gegen `unknown_field` durchspielen | "Alle 34 untypisierten Schreib-Rümpfe in api.ts sind Feld für Feld gegen die Contracts-DTOs |
| [B-29](B-29-altmigration-transaktional.md) | Prüfauftrag: nicht-transaktionale Altmigration | gegenstandslos seit der Neufaltung auf InitialCreate — die Migrationskette besteht (SchemaGuardTests, Tor G1b) aus genau einer Migration (Data/Migrations/20260803223259_InitialCreate.cs), keine Altmigration ist mehr vorhanden, die nicht-transaktional laufen könnte. Program.cs (Zeilen 471–484) behandelt eine DB der alten Kette ohnehin nicht als Migrationsfall, sondern wirft eine handlungsfähige Fehlermeldung — ein Upgrade-Pfad existiert bewusst nicht. |
| [B-30](B-30-i18n-rest.md) | i18n-Rest: Ledger-Texte, Platzhalter, interne Exceptions | "Der vermutete Rest existiert nicht: kein einziger `///`- oder `//`-Doku-Kommentar im Backend ist |
| [B-33](B-33-azure-publish-profile.md) | Azure-Secret `AZURE_WEBAPP_PUBLISH_PROFILE` fehlt | bewusste Nicht-Aufgabe für eine Code-Sitzung (Nutzer-Entscheidung) |
| [B-34](B-34-sitzungsbonus-dauer.md) | „Dauer durchgehend gelernt" als eskalierender Sitzungs-Bonus | durch MinutesPracticed-Missionen abgelöst |
| [B-38](B-38-mehrsprachige-oberflaeche.md) | Mehrsprachige Oberfläche (Deutsch, Englisch, Französisch) | "geteilt — ein Programm, keine Story (Entscheidung 8); siehe die recherchierte Grundlage unten und → [B-85, B-86, B-87] |
| [B-86](B-86-uebungstyp-manifest-anzeigenamen-schluessel.md) | Das Übungstyp-Manifest liefert Anzeigenamen als Daten, nicht als Schlüssel | gegenstandslos im Backend – das Manifest trägt mit `type` (= IExerciseType.Key, ExerciseTypeManifest.cs:33,43) bereits einen stabilen, sprachneutralen Schlüssel; `Label` ist der deutsche Anzeigename daneben. Der Rest – eine Frontend-Übersetzungstabelle mit `type` als Schlüssel – gehört in B-85 |
| [B-87](B-87-vater-web-franzoesisch-server-sprachfeld.md) | Rest des Mehrsprachigkeits-Programms: Vater-Web, Französisch, Server-Sprachfeld | "geteilt — selbst noch ein Programm (Entscheidung 1); siehe die recherchierte Grundlage oben und die → [B-91, B-92, B-90] |
| [B-90](B-90-server-sprachfeld.md) | Server-Sprachfeld an `Adult`/`Child` | Entscheidung des Nutzers am 2026-10-23 (G-9): Mehrsprachigkeit ist über die Sohn-Arcade auf Englisch (B-85) hinaus kein Ziel; ein Server-Sprachfeld hat keinen Konsumenten |
| [B-91](B-91-vater-web-extraktion-englisch.md) | Vater-Web-Textkorpus auf Übersetzungsschlüssel umstellen (Englisch) | Entscheidung des Nutzers am 2026-10-23 (G-9): Mehrsprachigkeit ist über die Sohn-Arcade auf Englisch (B-85) hinaus kein Ziel; das Vater-Web bleibt deutsch |
| [B-92](B-92-franzoesisch-zweite-zielsprache.md) | Französisch als zweite Zielsprache (Sohn-Arcade + Vater-Web) | Entscheidung des Nutzers am 2026-10-23 (G-9): Mehrsprachigkeit ist über die Sohn-Arcade auf Englisch (B-85) hinaus kein Ziel; keine zweite Zielsprache |

</details>

<!-- backlog-index:end -->

---

**Verwandt:** [obsidian.md](../obsidian.md) · [backlog-vokabellernen.md](../backlog-vokabellernen.md) ·
[anmerkungen-plan.md](../anmerkungen-plan.md) · [endpunkt-beziehungen.md](../endpunkt-beziehungen.md)
