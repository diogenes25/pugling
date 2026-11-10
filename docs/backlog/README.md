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

Zwei Prüfungen gehören dazu, beide in Lauf I (2026-10-28) gemessen: Eine **Entscheidung, die einen Fall
ausschließt** („zählt nur beim ersten Besuch“), wird gegen die User Story gerechnet, nicht nur gegen die AKs –
B-257 bestand seine AKs und verfehlte seine Story (B-327). Und **ändert ein Fix, wie gespeicherter Client-Zustand
entsteht** (`localStorage`, Cache), sagt die Story, was mit dem schon gespeicherten geschieht – sonst heilt er nur
neue Geräte.

Setzt eine Entscheidung zwei Größen gleich („Schlüssel = Inhalt") oder beruft sie sich auf eine **Konvention des
Repos**, belegt sie das am Code: je Größe den Schreiber mit `Datei:Zeile`, bei einer Konvention Fund- **und**
Gegenfundstellen (B-277, B-348).

Weil dieser Schritt teuer ist, trägt **schon `idee` eine `prio`**. Sonst müsste `/backlog` raten, welche
von zwei Dutzend Ideen es als Nächstes recherchiert.

`/backlog` schlägt die Prio vor, der Mensch entscheidet in einer Runde (Triage vom 2026-07-30).

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
Qualitätszahl wertlos. Der [Index](INDEX.md) rechnet daraus die einzige ehrliche Quote (*„Nachgeschaut: X von Y — und
in Z davon steckte ein durchgekommener Defekt"*) und führt die nie geprüften als **Arbeitsvorrat der
Nachschau** auf.

### Der Auslöser: die Nachschau ist die Pflichthandlung der Retro

Ohne Auslöser bleibt die Zahl bei null, weil niemand hinsieht — sie sinkt dann nicht, sie verschwindet aus
dem Blick. Der Auslöser sitzt darum in `pm-loop` **Step 8** und ist die *einzige Pflichthandlung* der
Retrospektive: sie liest die Zeile „Nachgeschaut: X von Y" aus dem Index, und ist die Arbeit des
**vorigen** Sprints nicht unter den X, wird sie jetzt angesehen. Das Ergebnis steht als erste Zeile unter
`## Retrospektive` im Protokoll — **ein Sprint ohne Nachschau-Zeile ist nicht geschlossen.**

Das ist kein Tor im harten Sinn, und der Grund dafür ist ehrlich zu benennen: die Handlung „hinsehen" ist
nicht mechanisch prüfbar, nur ihre *Aufzeichnung*. Was der Mechanismus leistet, ist, das Weglassen
**sichtbar** zu machen — eine fehlende Zeile in einem datierten Protokoll und ein Nenner, der nicht wächst.
Was er nicht leisten kann, ist, einen flüchtigen Blick von einem sorgfältigen zu unterscheiden.

**Der Stand** steht im Index (er rechnet ihn), die Herkunft in den Protokollen.

**Unbeaufsichtigt** (über Nacht) braucht dieser Lauf Vorab-Freigaben: [nachtlauf.md](../nachtlauf.md).

**Verschiebt der Diff Prüfungen gegeneinander**, liest die Nachschau die Reihenfolge vorher und nachher
(`git show <commit>^:<datei>`) und nennt sie im Prüfpunkt (B-345: Umordnung, als „sauber" nachgeschaut).

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

Generiert in [INDEX.md](INDEX.md) (`.claude/scripts/backlog-index.sh`) – eigene Datei, damit dieser Regeltext klein bleibt.

---

**Verwandt:** [obsidian.md](../obsidian.md) · [backlog-vokabellernen.md](../backlog-vokabellernen.md) ·
[anmerkungen-plan.md](../anmerkungen-plan.md) · [endpunkt-beziehungen.md](../endpunkt-beziehungen.md)
