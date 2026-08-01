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
- **Anmerkungen** (`api/v1/remarks`) sind der Eingang aus dem laufenden Betrieb: eine Anmerkung mit
  `Category = Idea`, die auf `Planned` geht, wird auf **Vorschlag** zur Story (`quelle: remark #NN`); die
  Anmerkung bekommt einen `Assistant`-Kommentar mit der Story-Id.

## Index

<!-- backlog-index:start -->
<!-- Erzeugt von .claude/scripts/backlog-index.sh — nicht von Hand pflegen. -->

### Offen (48)

| Id | Story | Art | Stufe | Prio | Größe | Wo | Kostet |
| --- | --- | --- | --- | --- | --- | --- | --- |
| [B-37](B-37-uebung-abbruch-unvollendet.md) | Abgebrochene Runden: Pflicht härten, Klausur deckeln | Defekt | `in-arbeit` | P1 | M | beides | — |
| [B-01](B-01-bildwahl-einfrieren.md) | Abschlusstest friert Bildwahlen ein, die er nie zeigt | Defekt | `geschaetzt` | P1 | S | backend | Vertrag |
| [B-07](B-07-db-umbau-restetappen.md) | DB-Struktur-Umbau: der offene Betriebsschritt | Aufräumen | `geschaetzt` | P1 | XS | backend | — |
| [B-53](B-53-wizard-doppelklick.md) | Zwei Klicks im Lehrplan-Assistenten legen zwei Kinder und zwei Pläne an | Defekt | `ausformuliert` | P2 | — | — | — |
| [B-24](B-24-frontend-unknown-field.md) | Frontend gegen `unknown_field` durchspielen | Frage | `idee` | P2 | — | — | — |
| [B-31](B-31-geraete-vorbehalt-klang.md) | Geräte-Vorbehalt: Klang und Haptik am echten Handy gegenhören | Frage | `idee` | P2 | — | — | — |
| [B-48](B-48-anonyme-registrierung-produktion.md) | Anonyme Registrierung ist auch in Produktion offen | Frage | `idee` | P2 | — | — | — |
| [B-10](B-10-zeitfenster-pro-kind.md) | Zeitfenster (Punkte-Faktor) je Pflicht statt global | Wunsch | `geschaetzt` | P2 | M | beides | Migration |
| [B-50](B-50-kind-beschreibt-sich-selbst.md) | Das Kind beschreibt sich selbst: Interessen in einem geführten Prozess | Wunsch | `ausformuliert` | P2 | — | — | — |
| [B-11](B-11-uebungen-veroeffentlichen.md) | Übungen ausdrücklich veröffentlichen | Wunsch | `idee` | P2 | — | — | — |
| [B-13](B-13-fach-kapitel-eigentum.md) | Fach- und Kapitel-Eigentum | Wunsch | `idee` | P2 | — | — | — |
| [B-18](B-18-auto-lehrplan-generator.md) | Lehrplan automatisch aus gefilterten Übungen bauen | Wunsch | `idee` | P2 | — | — | — |
| [B-19](B-19-schuelerprofil-ki-lehrplan.md) | Schülerprofil-getriebener KI-Lehrplan | Wunsch | `idee` | P2 | — | — | — |
| [B-39](B-39-supervisor-dashboard.md) | Supervisor-Dashboard über die Kinder | Wunsch | `idee` | P2 | — | — | — |
| [B-45](B-45-creator-punkte-empfehlung.md) | Die Punkte-Empfehlung des Creators soll der Supervisor übernehmen können | Wunsch | `idee` | P2 | — | — | — |
| [B-46](B-46-interessenbasierte-uebungen.md) | Übungen entstehen für ein Interessenprofil, nicht für ein bestimmtes Kind | Wunsch | `idee` | P2 | — | — | — |
| [B-42](B-42-openapi-typen-generieren.md) | TypeScript-Typen aus dem OpenAPI-Dokument erzeugen statt von Hand pflegen | Aufräumen | `gegrillt` | P2 | — | — | — |
| [B-52](B-52-testabdeckung-paket.md) | Sammel-Story: das Testabdeckungs-Paket | Aufräumen | `gegrillt` | P2 | — | — | — |
| [B-27](B-27-testsuite-grenzfaelle.md) | Die Grenzen des `ScoringService` als Tabelle statt als Flow | Aufräumen | `ausformuliert` | P2 | — | — | — |
| [B-44](B-44-grundprinzip-rollennamen.md) | Grundprinzip auf Supervisor/Student umschreiben — „Vater" ist keine Ebene | Aufräumen | `idee` | P2 | — | — | — |
| [B-43](B-43-frontend-komponententests.md) | Die Doppelklick-Lücke in `useAction` – und die fehlende Ebene für unsichtbare Zusicherungen | Defekt | `gegrillt` | P3 | — | — | — |
| [B-16](B-16-positions-formular-umfang.md) | Prüfauftrag: deckt das Positions-Edit-Formular alle Felder ab? | Frage | `idee` | P3 | — | — | — |
| [B-17](B-17-birkenbihl-sprachcodes.md) | Sprachcode-Normalisierung bei der Vokabel-Dekodierung | Frage | `idee` | P3 | — | — | — |
| [B-29](B-29-altmigration-transaktional.md) | Prüfauftrag: nicht-transaktionale Altmigration | Frage | `idee` | P3 | — | — | — |
| [B-03](B-03-lueckensaetze-mit-bild.md) | Lückensätze mit Bild als Vokabel-Vertiefung | Wunsch | `geschaetzt` | P3 | M | backend | — |
| [B-09](B-09-lehrer-hausaufgaben.md) | Lehrer erteilt Hausaufgaben: zuweisen mit Frist, ohne Betreuungsauftrag | Wunsch | `ausformuliert` | P3 | — | — | — |
| [B-12](B-12-uebungen-kuratieren.md) | Geteilte Übungen bewerten und kuratieren | Wunsch | `idee` | P3 | — | — | — |
| [B-15](B-15-testmodus-weitere-typen.md) | Vorschau für die nicht-prüfbaren Übungstypen | Wunsch | `idee` | P3 | — | — | — |
| [B-20](B-20-ki-supervisor-agent.md) | KI-Supervisor-Agent (Teil D) | Wunsch | `idee` | P3 | — | — | — |
| [B-21](B-21-ki-creator-foerdermodus.md) | KI-Creator: Fördermodus `--mode weakness` | Wunsch | `idee` | P3 | — | — | — |
| [B-22](B-22-unit-stoffnotizen-llm.md) | Unit-Stoffnotizen LLM-gestützt befüllen | Wunsch | `idee` | P3 | — | — | — |
| [B-23](B-23-uebungstyp-plugins-dll.md) | Übungstyp-Plugins als externe DLLs (Stufe 2) | Wunsch | `idee` | P3 | — | — | — |
| [B-28](B-28-login-name-sequenziell.md) | Sequenzielle IDs als Login-Name | Wunsch | `idee` | P3 | — | — | — |
| [B-35](B-35-karten-umdrehen-animation.md) | Karten drehen sich beim Aufdecken um | Wunsch | `idee` | P3 | — | — | — |
| [B-36](B-36-motivations-animationen-teilziele.md) | Motivations-Animationen bei erreichten Teilzielen | Wunsch | `idee` | P3 | — | — | — |
| [B-38](B-38-mehrsprachige-oberflaeche.md) | Mehrsprachige Oberfläche (Deutsch, Englisch, Französisch) | Wunsch | `idee` | P3 | — | — | — |
| [B-08](B-08-xml-docs-englisch.md) | XML-Doc-Kommentare im Backend auf Englisch übersetzen | Aufräumen | `in-arbeit` | P3 | S | doku | — |
| [B-54](B-54-objectivecard-schreib-primitive.md) | `ObjectiveCard` geht an den Schreib-Primitiven vorbei | Aufräumen | `ausformuliert` | P3 | — | — | — |
| [B-55](B-55-wegwerf-dateien-aufraeumen.md) | Die Tests räumen ihre Wegwerf-Dateien nicht weg | Aufräumen | `ausformuliert` | P3 | — | — | — |
| [B-25](B-25-vite-pwa-peer-konflikt.md) | Peer-Konflikt `vite-plugin-pwa` ↔ `vite@8` lösen | Aufräumen | `idee` | P3 | — | — | — |
| [B-30](B-30-i18n-rest.md) | i18n-Rest: Ledger-Texte, Platzhalter, interne Exceptions | Aufräumen | `idee` | P3 | — | — | — |
| [B-32](B-32-father-tabellenname.md) | `Father` heißt noch `Father`, obwohl die Zeile `Adult` ist | Aufräumen | `idee` | P3 | — | — | — |
| [B-47](B-47-deploy-artefakt-smoke.md) | Startet das veröffentlichte Artefakt überhaupt? | Aufräumen | `idee` | P3 | — | — | — |
| [B-49](B-49-sohn-app-schreib-primitive.md) | Die Sohn-App benutzt die geteilten Schreib-Primitive nicht | Aufräumen | `idee` | P3 | — | — | — |
| [B-51](B-51-admin-rolle-dokumentieren.md) | Die Admin-Rolle kommt in keinem Rollen-Dokument vor | Aufräumen | `idee` | P3 | — | — | — |
| [B-04](B-04-adaptiver-vokabel-pool.md) | Adaptiver Vokabel-Pool je Position | Wunsch | `geschaetzt` | P4 | M | backend | Migration? |
| [B-05](B-05-buchstaben-tausch.md) | Buchstaben-Tausch-Eingabe (Anagramm) | Wunsch | `geschaetzt` | P5 | M | beides | — |
| [B-06](B-06-cloze-preview-bild.md) | Cloze-Vorschau kann kein Bild zeigen | Wunsch | `geschaetzt` | P6 | XS | backend | — |

<details>
<summary>Abgenommen (4)</summary>

| Id | Story | Art | Stufe | Prio | Größe | Wo | Kostet |
| --- | --- | --- | --- | --- | --- | --- | --- |
| [B-02](B-02-itemcount-hilfetext.md) | Der Hilfetext erklärt `ItemCount` falsch herum | Defekt | `abgenommen` | P2 | XS | frontend | — |
| [B-26](B-26-e2e-in-ci.md) | Der E2E-Nachtlauf ist rot – und niemand erfährt es | Defekt | `abgenommen` | P1 | S | frontend | — |
| [B-40](B-40-client-routen-waechter.md) | Routen aus `Pugling.Client` gegen das OpenAPI-Dokument halten | Aufräumen | `abgenommen` | P3 | XS | backend | — |
| [B-41](B-41-produktions-startup-smoke.md) | Der Produktionspfad des Starts ist der einzige ohne Test | Aufräumen | `abgenommen` | P2 | S | backend | — |

</details>

<details>
<summary>Verworfen (3)</summary>

| Id | Story | Grund |
| --- | --- | --- |
| [B-14](B-14-learngoal-belohnung.md) | Idempotente Belohnung, wenn ein Lernziel erreicht ist — **gegenstandslos** | erfüllt durch KeyResult/ObjectiveRewardService (DB-Umbau E13) |
| [B-33](B-33-azure-publish-profile.md) | Azure-Secret `AZURE_WEBAPP_PUBLISH_PROFILE` fehlt | bewusste Nicht-Aufgabe für eine Code-Sitzung (Nutzer-Entscheidung) |
| [B-34](B-34-sitzungsbonus-dauer.md) | „Dauer durchgehend gelernt" als eskalierender Sitzungs-Bonus | durch MinutesPracticed-Missionen abgelöst |

</details>

<!-- backlog-index:end -->

---

**Verwandt:** [obsidian.md](../obsidian.md) · [backlog-vokabellernen.md](../backlog-vokabellernen.md) ·
[anmerkungen-plan.md](../anmerkungen-plan.md) · [endpunkt-beziehungen.md](../endpunkt-beziehungen.md)
