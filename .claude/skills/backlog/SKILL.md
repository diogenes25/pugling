---
name: backlog
description: >-
  Den Ideen-/Story-Backlog unter docs/backlog/ führen und eine Story eine Stufe weiter treiben
  (idee → ausformuliert → gegrillt → geschaetzt → in-arbeit → abgenommen). Nutze dies, wenn der Nutzer
  wissen will, was offen ist ("was ist offen", "Backlog", "was koennen wir als Naechstes bauen",
  "Stand der Ideen"), wenn er eine Story weiterschieben will ("mach B-07 weiter", "B-12 ausformulieren",
  "B-03 schaetzen", "B-10 grillen"), wenn er eine neue Idee ablegen will ("nimm die Idee auf",
  "das gehoert in den Backlog") oder wenn Ideen aus Anmerkungen, Notizen und Plandokumenten eingesammelt
  werden sollen ("Ideen ernten"). Die Zustandsmaschine steht in docs/backlog/README.md — dieser Skill
  führt sie aus, er definiert sie nicht.
---

# backlog — die Kette von der Idee zur Abnahme

Der Bereich ist `docs/backlog/`, eine Story je Datei, `B-<nn>-<slug>.md`. Die Id ist die stabile
Referenz.

## Erst lesen, dann handeln

**`docs/backlog/README.md` ist die Quelle der Wahrheit für die Stufen, die Eintrittsbedingungen, die
Frontmatter-Felder und die Größen-Anker.** Lies sie zu Beginn jedes Aufrufs. Wiederhole ihren Inhalt
nicht hier und nicht in einer Story — zwei Fassungen einer Zustandsmaschine driften, und dann gilt die
falsche.

## Die eine Regel, die diesen Bereich wertvoll oder wertlos macht

**Eine Stufe wird erarbeitet, nicht gesetzt.** `status:` ist die Behauptung, die Eintrittsbedingung ist
der Beleg. Erfüllt die Datei sie nicht, **weigere dich** und benenne genau, was fehlt — schiebe nicht
weiter und trage die Lücke nicht als „offener Punkt" nach, um sie loszuwerden.

Der häufigste Weg, das zu verletzen, ist die Stufe `ausformuliert`: Die Notiz sagt „offen: X fehlt", und
es ist verlockend, das in eine User Story umzuformulieren und fertig. Das ist **keine** Ausformulierung.
Der Ist-Stand braucht Belege (`Datei.cs:Zeile`) aus dem echten Code. Beim ersten Durchgang dieses
Verfahrens (Protokoll `docs/backlog-vokabellernen.md`, 2026-07-30) hat genau das fünf vermeintlich offene
Punkte als längst erledigt entlarvt und eine Vorab-Entscheidung revidiert. Ideen und „offen"-Vermerke
verrotten; der Code nicht.

Wenn du etwas nicht belegen kannst, schreib das hin („nicht gefunden", „nicht sicher"). Das ist ein
brauchbarer Ist-Stand; ein geratener nicht.

## Aufrufe

### `/backlog` — den Stand zeigen

1. Index frisch machen: `bash .claude/scripts/backlog-index.sh`.
2. Tabelle nach Stufe zeigen, und **eine** Empfehlung „als Nächstes": höchste `prio`, bei Gleichstand die
   reifere Stufe. Begründe sie in einem Halbsatz.
3. **Überfällig-Meldung**: `idee`-Stories, deren letzter `## Verlauf`-Eintrag über 90 Tage zurückliegt
   (Tagesdatum via `date -u +%F` holen, nicht schätzen). Je Eintrag eine Entscheidung anbieten: Prio
   bestätigen oder verwerfen. Das **warnt und blockt nicht** — niemals von selbst verwerfen.

### `/backlog <id>` — eine Stufe weiter

Genau **eine** Stufe je Aufruf. Zwei Stufen in einem Rutsch heißt, dass eine davon nicht erarbeitet wurde.

1. Story lesen, aktuelle Stufe und die Eintrittsbedingung der **Ziel**stufe aus dem README holen.
2. Fehlt der Zielstufe etwas, das nur der Mensch liefern kann (das ist bei `gegrillt` der Normalfall):
   nicht raten — die Grill-Runde fahren (siehe unten).
3. Arbeit tun, Datei schreiben, `status` **und** das `status/…`-Tag setzen, `## Verlauf` um eine Zeile mit
   dem Shell-Datum ergänzen, Index neu erzeugen.
4. Am Ende in zwei Sätzen berichten: von welcher Stufe auf welche, und was der belegte Kern war.

Stufenspezifisch:

- **→ `ausformuliert`**: gegen den Code recherchieren. Einstieg über
  [docs/endpunkt-beziehungen.md](../../../docs/endpunkt-beziehungen.md) und
  [docs/obsidian.md](../../../docs/obsidian.md), nicht über einen Voll-Scan. `unverifiziert` entfernen.
  Offene Punkte als Fragenliste formulieren, **je mit deiner Empfehlung** — das ist das Material der
  Grill-Runde.
- **→ `gegrillt`**: siehe Grill-Runde. Erledigte offene Punkte ~~durchstreichen~~, nicht löschen.
  Selbst grillen darfst du **nur** unter einer ausdrücklichen Freigabe und **nur** bei `art: Defekt`
  oder `Aufräumen` — bei `Wunsch` und `Frage` bleibt es beim Dialog, auch mit guter Begründung
  (Regel und Kosten: README → „Der Backlog-Lauf").
- **→ `geschaetzt`**: Größe an den Ankern im README ausrichten, nicht frei schätzen. `migration` und
  `vertragsbruch` sind ja/nein und **nachzusehen**, nicht zu vermuten: Schemaänderung ⇒ `migration: ja`
  (die Kette wird neu gefaltet, `SchemaGuardTests` hält Länge 1); Änderung an `Pugling.Contracts` ⇒
  `vertragsbruch: ja` (Client, Frontend und die `unknown_field`-Guards ziehen nach). Den Testweg **benennen**
  (welche Testklasse, welcher E2E, `/smoke-test`) — „wird getestet" ist kein Testweg.
- **→ `in-arbeit` / `abgenommen`**: bauen nach den Konventionen der [CLAUDE.md](../../../CLAUDE.md).
  `abgenommen` verlangt **belegte** Verifikation: echte Testzahl, gelaufener E2E/`/smoke-test`, den
  passenden Reviewer, Commit — **und den Rollengang an der laufenden App** (`pm-loop` Step 6) oder eine
  `## Verlauf`-Zeile, die seinen Ausfall benennt. Der Reviewer beantwortet „ist der Code richtig?", nicht
  „kann das Kind spielen?"; woran das gemessen wurde, steht im README. Eine hoffnungsvolle Formulierung
  ist hier eine Lüge mit Haltbarkeitsdatum.

### Die Fachbrille kommt aus `wo` — nicht aus dem Gedächtnis

Ab `geschaetzt` entscheidet das Feld `wo`, **wen der PM fragt**. Der Grund, das hier festzuschreiben: Eine
Schätzung ohne die richtige Brille ist an genau der Stelle am dünnsten, wo sie wehtut — B-10s Schätzung war
beim eingeklappten Formularblock am schwächsten, weil sie ohne Frontend-Brille entstand.

| `wo` | Beim Schätzen und Bauen | Vor der Abnahme |
| --- | --- | --- |
| `backend` | Skill `csharp-senior-dev`; `backend/Pugling.Api/CLAUDE.md` lädt beim Arbeiten dort selbst, dazu `Contracts`/`Client` bei Vertragsarbeit | Agent **`pugling-reviewer`** |
| `frontend` | `frontend/CLAUDE.md` (lädt selbst); Skill `web-design-guidelines` für UI/A11y-Fragen | Agent **`frontend-reviewer`** |
| `beides` | **Backend zuerst** — API-First ist keine Stilfrage, das Frontend hängt an der API. Beide Brillen, in dieser Reihenfolge | beide Reviewer |
| `doku` | Obsidian-Konventionen aus `docs/obsidian.md`; `markdownlint-cli2` (CI-Job „Markdown-Lint") | kein Agent, aber Lint muss grün sein |

**Keine Discipline-Skills erfinden.** Die Konventionen laden in diesem Repo **nach Verzeichnis** (fünf
verschachtelte `CLAUDE.md`). Ein „UI-Dev-Skill", der `frontend/CLAUDE.md` nacherzählt, wäre eine zweite
Fassung derselben Regel — und nach der Erfahrung dieses Projekts gewinnt dann die veraltete. Ein Skill lohnt
nur, wo er ein **Verfahren** beiträgt, keine Fakten. (`dotnet-ui` ist übrigens Blazor/MAUI und für dieses
React-Frontend nutzlos — nicht ziehen.)

### `/backlog <id> grillen` — die Entscheidungen beim Menschen abholen

Ruf den Skill `grilling` **zusammen mit `domain-modeling`** und halte dich an die Regel von `grilling`:
**eine Frage, dann warten.** Fakten selbst nachsehen, nur Entscheidungen vorlegen, je mit Empfehlung.

`domain-modeling` trägt dabei genau seine *aktive* Hälfte bei: einen Begriff sofort anfechten, wenn er
mit der Sprache des Projekts kollidiert („du sagst Vater — meinst du die Rolle `Supervisor` oder die
Verwandtschaft?"), unscharfe Wörter durch den kanonischen ersetzen und die Behauptung gegen den Code
halten. Das ist billig und fängt genau die Drift ab, die dieses Repo teuer bezahlt hat (`Father`→`Adult`,
Lernziel→`KeyResult`, Vater/Sohn vs. Supervisor/Student).

**Aber: keine neuen Dateien.** Der Skill legt von sich aus `CONTEXT.md` und `docs/adr/` an, sobald ein
Begriff oder eine Entscheidung fällt — das ist hier **nicht** gewollt, solange darüber nicht entschieden
ist. Das Ergebnis der Runde geht in die `## Entscheidungen` der Story; ein geschärfter Begriff, der über
die Story hinaus gilt, wird als solcher benannt und dem Nutzer vorgelegt, nicht abgelegt.

Danach in die Story schreiben: `## Entscheidungen`, nummeriert, jede mit **Begründung und Kosten**
(„Träger der Bildwahl ist die Vokabel. Folge: dasselbe Motiv wie auf der Karteikarte — gewollt. Kosten:
ein vokabel-basierter Batch-Pfad im `MediaSelector`."). Eine Entscheidung ohne Kosten ist eine Meinung.

**Wenn die Runde nicht trägt:** Merkst du beim Grillen, dass die offenen Punkte voneinander abhängen und
die Kette nicht in eine Sitzung passt — jede Antwort legt zwei neue Fragen frei —, dann **schlage die
Karte vor** (`/wayfinder`, Abschnitt „Karten" im README) und warte auf die Entscheidung des Nutzers.
Nicht von selbst umschwenken: eine Karte kostet eine eigene Sitzung fürs Kartieren. Umgekehrt gilt
dasselbe — eine Story, deren Punkte unabhängig sind, wird gegrillt, nicht kartiert, egal wie viele es
sind.

### `/backlog neu "…"` — Idee aufnehmen

Reibungsfrei: Titel, ein Absatz, `quelle:`, `prio` (fragen, wenn nicht klar), `unverifiziert: true`,
nächste freie Id. Keine Recherche — das ist der nächste Schritt, nicht dieser.

### `/backlog ernten` — Ideen einsammeln

Quellen, in dieser Reihenfolge:

1. **Anmerkungen** aus dem laufenden Betrieb — und zwar **jede auf `Planned`**, nicht nur `Category = Idea`:
   `Planned` heißt „es ist etwas zu tun, aber nicht jetzt", und ohne Story ist das eine Sackgasse (die
   Anmerkung liegt in der DB einer laufenden Instanz und ist ohne Server unsichtbar). Zugriff wie im Skill
   `anmerkungen` beschrieben (echte Instanz, `scope=all`), oder über den Export `docs/anmerkungen/aktuell.md`,
   wenn kein Server läuft.
   - **Vorschlagen, nicht automatisch anlegen.**
   - Die schon geschriebene **Analyse wandert als Ist-Stand mit** — sie ist belegt und entstand, während der
     Fehler sichtbar war; damit ist `ausformuliert` für diese Story fast geschenkt.
   - `art` aus der Kategorie: `Bug → Defekt`, `Idea`/`Content → Wunsch`, `Ui →` je nachdem, `Code`/`Question`
     → meist keine Story, sondern eine Antwort im Skill `anmerkungen`.
   - Danach `quelle: remark #NN` setzen und die Anmerkung mit einem `Assistant`-Kommentar auf die Story-Id
     verweisen. Die Anmerkung bleibt der **Beleg**, die Story trägt den **Zustand**.
2. **„offen:"-Vermerke** in den Memory-Notizen und in `docs/*-plan.md`.
3. **Roadmap-Reste** in `docs/pm-sitzung-*.md`.

Regeln der Ernte:

- **Ungeprüft ist erlaubt, unmarkiert nicht.** Alles kommt auf `idee` mit `unverifiziert: true` und
  `quelle:`-Link. Die Prüfung passiert beim Ausformulieren.
- **Den Stand nicht abschreiben, sondern nachsehen.** Notizen verrotten schnell: Am 2026-07-30 behaupteten
  `MEMORY.md` und der Plan „E7 und E9–E14 offen", während E7 und E9 bereits committet waren. Eine Zahl oder
  Etappenliste aus einer Notiz wird gegen die Quelle geprüft, bevor sie in eine Story wandert.
- **Ein gepflegtes Plandokument bleibt Quelle der Wahrheit** → eine Sammel-Story mit Link, keine Kopie der
  Etappen.
- **Bewusste Nicht-Ziele** kommen als `verworfen` **mit Grund** herein, nicht als `idee`. Sonst kosten sie
  in jeder Sichtung erneut Aufmerksamkeit.
- Am Ende **eine** Triage-Tabelle vorlegen (Id · Titel · Prio-Vorschlag · Halbsatz Begründung); der Nutzer
  antwortet mit den Abweichungen, danach die Prios schreiben.

## Grenzen

- **Den von Hand gesetzten `status` niemals zurückschreiben.** Hat der Nutzer eine Stufe selbst gesetzt,
  ist das seine Entscheidung; erfüllt die Datei die Bedingung nicht, wird das *gemeldet* — die Datei bleibt
  wie sie ist.
- **Nicht bauen, wenn nur weitergeschoben werden soll**, und nicht weiterschieben, wenn gebaut werden soll.
- **`## Verlauf` ist append-only.** Alte Einträge werden nicht umgeschrieben; auf dem letzten rechnet die
  Alterung.
- **Datum aus der Shell** (`date -u +%F`). Nahe Mitternacht weicht das lokale Datum ab — das Projekt rechnet
  in UTC (`CLAUDE.md`, „Zeit/UTC"), also auch hier.
- Nach jeder Änderung an einer Story: `bash .claude/scripts/backlog-index.sh`. Der Index ist generiert;
  von Hand gepflegt driftet er.
- Story- **und Ticket**-Dateien liegen unter `docs/` und werden von `markdownlint-cli2` geprüft (CI-Job „Markdown-Lint"):
  Leerzeilen um Listen/Überschriften/Codeblöcke, Tabellen mit führendem und schließendem Pipe.

## Verhältnis zu den Nachbar-Werkzeugen

- **`pm-loop`** ist die periodische Runde am laufenden Produkt (Creator/Vater/Sohn) mit Abnahme-Gate. Es
  *erzeugt* Ideen und legt sie hier als `idee` ab; seine Prio-Tabelle ist eine datierte Momentaufnahme mit
  Story-Ids. Die dauerhafte Liste ist dieser Bereich.
- **`anmerkungen`** beantwortet Beobachtungen aus dem Widget. Wo aus einer Beobachtung *Arbeit* wird, endet
  sie hier als Story.
- **`grilling`** liefert die Entscheidungen für die Stufe `gegrillt`, **`domain-modeling`** dabei die
  Begriffsschärfe. Die beiden zusammen sind das, was anderswo `grill-with-docs` heißt — ohne dessen
  Nebenprodukt-Dateien.
- **`wayfinder`** ist dieselbe Stufe für ein Vorhaben, das nicht in eine Sitzung passt: Die Karte **ist**
  die Story, ihre Entscheidungs-Tickets liegen unter `docs/backlog/karten/B-<nn>/` und stehen nicht im
  Index. Er **ersetzt `grilling` nicht**, er ruft ihn — `grilling` ist sein Normalfall-Ticket-Typ. Die
  Abbildung auf diesen Bereich steht im README unter „Wayfinding operations"; sein mitgelieferter
  `.scratch/`-Tracker gilt hier nicht.
- **`pugling-reviewer`** (Backend) und **`frontend-reviewer`** (React) prüfen vor der Abnahme Korrektheit
  und Konventionen — welcher, sagt `wo`.

**Bewusst nicht übernommen** (aus demselben Paket wie `wayfinder`, geprüft am 2026-08-01): `to-spec` und
`to-tickets` schreiben Spec und Tickets in einen *externen* Tracker — das wäre der zweite Ablageort neben
diesem Bereich, aus demselben Grund abgelehnt wie der `.scratch/`-Tracker des Wayfinders; ihre Arbeit
leisten hier die Stufen `ausformuliert`/`geschaetzt` in der Story selbst. `implement` ist inhaltlich die
Stufe `in-arbeit` plus die beiden Reviewer, nur ohne Eintrittsbedingung und ohne `## Verlauf`.
