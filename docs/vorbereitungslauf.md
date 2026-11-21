---
tags: [typ/referenz, bereich/pm]
aliases: [Vorbereitungslauf, Backlog vorbereiten mit Sonnet]
---

# Vorbereitungslauf: das Backlog über Nacht bis `geschaetzt` bringen

Ein **Vorbereitungslauf** baut nichts. Er treibt offene Stories über die Stufen `idee → ausformuliert →
gegrillt → geschaetzt`, damit ein späterer Bau-Lauf ([nachtlauf.md](nachtlauf.md)) genug Material hat.
Gedacht ist er für **Sonnet**: Die Stufen davor sind Recherche am Code und Schreibarbeit, und dafür
reicht das günstigere Modell.

## Die eine Abweichung vom Standard und ihr Preis

Laut [README](backlog/README.md#der-backlog-lauf-dieselbe-freigabe-aber-offen-statt-je-vorhaben) grillt
der Agent einen `Wunsch` oder eine `Frage` nie allein. Dieser Lauf **darf das trotzdem**, aber nur als
**Vorschlag**:

- Jede so gefällte Entscheidung trägt die Marke *„stellvertretend, unbestätigt“*.
- Die Story bekommt `wartet_auf: Bestätigung der Sonnet-Entscheidungen`. Damit steht sie im Index unter
  „Wartet auf Zutun von außen“, und ein Bau-Lauf hält an ihr an ([nachtlauf.md](nachtlauf.md) → „Wo er
  anhält“), bis du das Feld leerst.

**Kosten:** Am Morgen liegt eine Bestätigungsrunde vor dir. Die ist aber kürzer als ein Grill-Dialog,
weil du nur widersprechen musst, wo du anders entscheidest. **Was es nicht kostet:** Eine falsche
Produktentscheidung kann nicht still in den Code wandern.

## Der Auftragstext

Starte eine **neue Sitzung**, stelle das Modell auf **Sonnet 5.5** und die Denktiefe auf **high**, und
füge den Text wörtlich ein.

```text
Vorbereitungslauf, unbeaufsichtigt. Du BAUST NICHTS. Du treibst offene Stories des Backlogs über
die Stufen idee → ausformuliert → gegrillt → geschaetzt, nach dem Skill `backlog` und
docs/backlog/README.md (lies beides zuerst, dazu docs/vorbereitungslauf.md).

Freigaben für diesen Lauf (erteilt vom Nutzer, gelten nur für diesen Lauf):
1. Du darfst JEDE `art` selbst grillen, auch `Wunsch` und `Frage`. Ich bin nicht da und
   beantworte keine Rückfragen. Wo der Skill eine Grill-Runde mit mir verlangt, entscheidest du
   stellvertretend nach deiner eigenen Empfehlung. Dafür gelten drei Auflagen:
   a) Jede Entscheidung bekommt eine Nummer, eine Begründung und Kosten wie immer. Dazu kommen
      die verworfene Alternative und der Fall, den die Entscheidung ausschließt, gerechnet
      gegen die USER STORY, nicht nur gegen die AKs.
   b) Über `## Entscheidungen` steht die Zeile
      „_Stellvertretend entschieden im Vorbereitungslauf <Datum> (Sonnet), unbestätigt._“
   c) Im Frontmatter steht `wartet_auf: Bestätigung der Sonnet-Entscheidungen`. Bei `Defekt`
      und `Aufräumen` entfällt das, dort ist autonomes Grillen ohnehin erlaubt.
   Eine Freigabe-Zeile für den Bau („Autonomer Modus, Opt-in“) schreibst du NIE, die gibt
   nur der Nutzer.
2. Ausgenommen vom Grillen, nur bis `ausformuliert`: B-680, B-681, B-682, B-683, B-684 und
   B-644. Sie hängen am Richtungswechsel „Leitbild Privatlehrer“ vom 2026-11-19, und den
   entscheidet der Nutzer. Dasselbe gilt für jede Story, deren Entscheidung die
   Rollenaufteilung Vater/Lehrer, die Münz-Ökonomie oder die Malus-Regeln ändern würde:
   ausformulieren, die Frage samt Empfehlung notieren, nicht entscheiden.
3. Eine Stufe je Story und Durchgang, und jede Stufe wird erarbeitet:
   - → ausformuliert: Ist-Stand mit `Datei:Zeile` aus dem echten Code. Was du nicht
     findest, steht dort als „nicht gefunden“. Notizen und Memory sind keine Belege. Stellt
     sich die Story als erledigt oder gegenstandslos heraus, setzt du sie NICHT auf
     verworfen, sondern notierst es als Vorschlag im Protokoll.
   - → gegrillt: wie in Freigabe 1. Jedes Akzeptanzkriterium nennt die Änderung, die es rot
     macht. Lautet die Antwort „keine“, wird das Kriterium umgeschrieben.
   - → geschaetzt: Größe an den Größen-Ankern im README. `migration` und `vertragsbruch`
     NACHGESEHEN, nicht vermutet: Schema → Migration an die Kette, Pugling.Contracts →
     Vertragsbruch. Testweg mit Testklasse/E2E-Spec beim Namen. Bei `wo: beides` gilt
     Backend zuerst.
   - Faktisch XL: nach „Teilen und Zusammenlegen“ im README in kleinere Stories zerlegen.
     Vor jeder neuen Id den höchsten vorhandenen Wert in docs/backlog/ neu zählen, denn eine
     andere Sitzung kann parallel Ids vergeben.
4. Reihenfolge: zuerst `Defekt`, dann P1, dann P2, P3 zuletzt und nur, wenn noch Zeit ist.
   Innerhalb einer Prio geht die reifere Stufe vor. Erst alle bis ausformuliert bringen, die
   du schaffst, dann grillen, dann schätzen. Breite vor Tiefe.

Grenzen (hart):
- Du änderst NUR Dateien unter docs/backlog/ und das Protokoll
  docs/pm-sitzung-<Datum>-vorbereitungslauf.md. Kein Code, keine Tests, keine Skills, keine
  CLAUDE.md, keine Memory-Dateien.
- Du startest keinen Server und rührst pugling.db nicht an. Den Server auf :5200 benutzen
  die Kinder.
- Commits: je Story oder je kleinem Bündel selbst committen, IMMER mit expliziten Pfaden
  (`git add docs/backlog/B-…md docs/backlog/INDEX.md …`), NIE `git add -A` oder `git add .`.
  Im Arbeitsbaum liegt fremde, ungecommittete Frontend-Arbeit, die nicht in deine Commits
  gehört. Nicht pushen.
- Nach jeder Story-Änderung `bash .claude/scripts/backlog-index.sh`. Das Datum für
  `## Verlauf` kommt aus `date -u +%F`. Vor jedem Commit muss
  `npx markdownlint-cli2 "docs/backlog/**/*.md"` grün sein (bzw. die lokale Binary unter
  frontend/node_modules/.bin, falls vorhanden).

Halt: wenn keine Story mehr eine Stufe weiterkommt, oder wenn du merkst, dass du dieselbe
Frage in mehreren Stories unterschiedlich entscheiden würdest. Dann hältst du an und
notierst den Widerspruch.

Am Ende schreibst du das Protokoll docs/pm-sitzung-<Datum>-vorbereitungslauf.md mit:
- einer Tabelle Id | Titel | Stufe vorher → nachher | Kern der Entscheidungen in einem
  Halbsatz | deine Unsicherheit (niedrig/mittel/hoch, mit Grund),
- einem Abschnitt „Zum Bestätigen“, sortiert nach Unsicherheit (hoch zuerst). Je Story
  stehen dort die Entscheidungen, bei denen ich am ehesten anders entscheiden würde, und
  warum,
- einem Abschnitt „Nicht entschieden“: die Fragen aus Freigabe 2 samt Empfehlung,
- einem Abschnitt „Vorschläge“: verworfen/erledigt-Kandidaten und Widersprüche.
Das Protokoll committest du ebenfalls.
```

## Am Morgen

1. Im Protokoll zuerst den Abschnitt **„Zum Bestätigen“** lesen, hohe Unsicherheit zuerst.
2. Je Story entweder **bestätigen** (`wartet_auf` leeren und unter `## Verlauf` die Zeile „Entscheidungen
   bestätigt durch Nutzer“ ergänzen) oder **widersprechen**. Bei einem Widerspruch geht die Story mit
   `/backlog <id> grillen` in den Dialog. Das kann ruhig eine Opus-Sitzung erledigen: „bestätige B-x, B-y;
   bei B-z gilt Entscheidung 2 andersherum“.
3. Erst danach sind die `Wunsch`-Stories für einen Bau-Lauf nach [nachtlauf.md](nachtlauf.md) baubar.
   Den Bau-Lauf fährt besser **Opus**, die Begründung dafür steht dort unter „Wenn der Lauf mit Sonnet
   fährt“.
