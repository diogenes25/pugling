---
tags: [typ/story, status/abgenommen, bereich/pm]
aliases: [backlog-index.sh loescht den Index, awk nicht gefunden exit 0]
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: doku
migration: nein
vertragsbruch: nein
quelle: eigene Beobachtung im Nachtlauf 2026-09-01
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-190 · Aus einem Unterverzeichnis aufgerufen, schreibt der Index-Generator einen leeren Index

Dieselbe Familie wie der ganze Sprint 1 dieses Laufs: **ein Fehlschlag gibt sich als gültiges Ergebnis
aus.** Nur trifft es hier nicht die Oberfläche, sondern die Quelle der Wahrheit des Backlog-Bereichs.

## User Story

Als **PM** möchte ich, dass ein gescheiterter Indexlauf abbricht statt den Index zu überschreiben, damit
ein Aufruf aus dem falschen Verzeichnis nicht 61 offene Stories aus der Übersicht löscht.

## Ist-Stand am Code

`.claude/scripts/backlog-index.sh:24` bildet den Pfad zum `awk`-Programm über
`$(dirname "${BASH_SOURCE[0]}")` — also **relativ zum Aufruf**. Wird das Skript mit relativem Pfad aus
einem Unterverzeichnis gestartet, zeigt das ins Leere:

```text
awk: fatal: cannot open source file `../../.claude/scripts/backlog-index.awk' for reading
Index geschrieben: 0 offen, 0 abgenommen, 0 verworfen.
```

**Beide Zeilen zusammen sind der Defekt.** `awk` meldet `fatal`, das Skript läuft weiter, schreibt den
Index aus einer leeren Eingabe — und meldet Erfolg. Am 2026-09-01 im Nachtlauf ausgelöst
(`cd docs/backlog && bash ../../.claude/scripts/backlog-index.sh`); der Index war danach leer und wurde
nur deshalb wiederhergestellt, weil derselbe Lauf ihn eine Minute später aus dem Repo-Wurzelverzeichnis
neu erzeugte.

Der Schaden ist still: Ein leerer Index sieht aus wie ein leerer Backlog, und `git diff` an einer
generierten Datei liest niemand Zeile für Zeile.

## Die echte Lücke

Kein `set -e` bzw. keine Prüfung des `awk`-Ergebnisses, und keine Plausibilitätsschwelle vor dem
Überschreiben. Die Erfolgsmeldung wird gedruckt, ohne dass jemand geprüft hat, ob etwas gelesen wurde.

## Offene Punkte

1. **Abbrechen oder heilen?** Empfehlung: **abbrechen** — das Skript soll nicht raten, wo es liegt, wenn
   der Aufrufer es schon falsch adressiert hat. Ein `[ -f "$awk_script" ] || { echo …; exit 1; }` genügt.
2. **Zusätzlich eine Untergrenze?** („0 Stories gelesen → nicht schreiben.") Empfehlung: **ja, aber als
   Null-Prüfung, nicht als feste Zahl.** Feste Untergrenzen verrotten — das ist im Bereich schon einmal
   bezahlt worden (Client-Routen-Wächter, siehe Memory „Codequalität-Gates"). „Null gelesen" verrottet nie.
3. Gilt dasselbe für andere Skripte unter `.claude/scripts/`? Empfehlung: greppen, nicht vermuten.

## Akzeptanzkriterien

1. Ein relativer Aufruf aus einem Unterverzeichnis (`cd docs/backlog && bash ../../.claude/scripts/backlog-index.sh`)
   erzeugt denselben Index wie der Aufruf aus dem Wurzelverzeichnis (`git diff` leer).
2. Scheitert `awk` (Exit ≠ 0) oder liest es trotz vorhandener Story-Dateien keinen Datensatz, endet das Skript
   mit Exit 1 und einer Meldung – der Index wird **nicht** geschrieben.
3. Beides ist in einem Wegwerf-Root nachgestellt, mit Zahl vor und nach dem Fix (Auflage 5).

## Entscheidungen

Gegrillt im Nachtlauf 2026-10-23 (Freigabe 1, `art: Defekt`).

1. **Die Lücke ist präziser als „keine Prüfung des awk-Ergebnisses":** die Existenzprüfung
   `[ -f "$awk_script" ]` gibt es seit `41f21eb` (2026-08-06, also **vor** der Beobachtung vom 2026-09-01) –
   sie läuft nur **vor** dem `cd "$root"` mit einem relativen Pfad, der vom Aufrufer aus existiert, und awk
   läuft **danach** vom Root aus. Die Prüfung prüfte also die falsche Lage. Begründung: nachgelesen
   (`backlog-index.sh:21-27`) und im Wegwerf-Root reproduziert (P1 unten). **Kosten:** keine; der Ist-Stand der
   Story wird hier berichtigt, nicht umgeschrieben.
2. **Abbrechen, nicht heilen** (Punkt 1): der awk-Pfad wird **absolut** gebildet (`cd … && pwd`), vor dem `cd`;
   die Existenzprüfung bleibt. Begründung wie in der Story – das Skript rät nicht, wo es liegt. **Kosten:** eine
   Subshell beim Start, einmalig.
3. **Null-Prüfung statt fester Untergrenze** (Punkt 2), **und** der awk-Exit-Status: awk schreibt in eine
   Datei statt per Prozess-Substitution in die Schleife – `done < <(awk …)` verschluckt den Status. Exit ≠ 0
   → Abbruch; null Datensätze trotz nicht-leerer Dateien → Abbruch. Begründung: beides sind Fehlschläge, die
   sich vorher als „0 offen" ausgaben; „null gelesen" verrottet nie, eine Zahl schon. **Kosten:** eine
   Temp-Datei (`README.md.records.tmp`), die am Ende gelöscht wird; bei Abbruch ebenso.
4. **Andere Skripte** (Punkt 3): gegrept (`BASH_SOURCE` in `.claude/scripts/*.sh`, `.claude/hooks/*.sh`) –
   `context-budget.sh:29` nutzt `dirname` nur, um die Git-Wurzel zu finden (kein Begleitdatei-Pfad); die
   Hooks bilden ihren Root per `cd … && pwd` absolut. Kein zweiter Fall. **Kosten:** keine.
5. **Beleg als Wegwerf-Harness, nicht als Test im Repo.** Begründung: AK 3 von B-131 verlangt ausdrücklich
   einen Temp-Ordner; ein Dauer-Test für ein Bash-Skript hätte keinen Läufer in CI (die Suite ist `dotnet test`
   + Vitest + Playwright). Die Kommandos stehen im Verlauf, nachstellbar. **Kosten:** kein Tor – ein Rückfall
   fiele erst beim nächsten Fehlaufruf auf; der ist aber jetzt laut, nicht still.

## Schätzung

**Größe XS** (Anker B-02). `wo: doku` (Werkzeug des Backlog-Bereichs), keine Migration, kein Vertrag.

**Angriffsplan:** Harness im Scratchpad (Wegwerf-Root mit Marker-README, zwei echten Stories, kaputter
awk-Kopie) → rote Proben P1 (relativer Aufruf) und P2 (awk-Syntaxfehler) → Fix in `backlog-index.sh` →
Proben grün → echter Index aus Root **und** Unterverzeichnis, Diff leer → Reviewer → Commit.

**Risiko:** Die Temp-Datei liegt neben der README; bricht das Skript zwischen Schreiben und Löschen hart ab,
bleibt sie liegen (untracked, harmlos).

**Testweg:** Harness-Proben P1/P2 (Zahl im Verlauf), Idempotenz-Diff des echten Index.

## Verlauf

- **2026-10-23** — **abgenommen** (Nachtlauf, Sprint 2). Reviewer (allgemeiner Agent, Brief: Skript + Doku):
  Skript-Verhalten im eigenen Wegwerf-Root nachgestellt (leere Datei, Datei ohne Frontmatter, relativer
  Aufruf, kaputtes awk, alle Dateien leer, keine Dateien), `set -u`-Pfade, Temp-Datei auf jedem Ausgang,
  nur Builtins in der neuen Schleife – ohne Blocker; **ein Fund mit Codewirkung (Zähler 3 des Sprints):**
  der Zeilenumbruch der ⚠-Zeile stand als literaler Umbruch in `$'…'` → `$'\n'` wie die Nachbarn, Proben
  erneut grün. Nits benannt (Guard für leeres `files` nur bash < 4.4; ⚠-Reihenfolge; kein `trap`).
  **Rollengang:** der PM selbst – Aufruf aus Root und Unterverzeichnis, Harness P1/P2; kein Browser nötig.
  Commit: siehe Nachtrag.
- **2026-10-23** — **gebaut** (Nachtlauf, Sprint 2). **Rote Proben (erwartet/gemessen), im Wegwerf-Root:**
  P1 (relativer Aufruf aus `docs/backlog`): erwartet Index mit 2 offen/1 abgenommen – gemessen
  `awk: fatal: cannot open source file`, dann „Index geschrieben: 0 offen, 0 abgenommen", **Exit 0**, README
  mit leerem Index überschrieben. P2 (awk-Programm mit Syntaxfehler): erwartet Abbruch – gemessen awk-Fehler,
  „Index geschrieben: 0 offen", **Exit 0**. **Nach dem Fix:** P1 „2 offen, 1 abgenommen", Exit 0, Index
  vollständig; P2 „awk ist gescheitert – Index nicht geschrieben.", **Exit 1**, README unverändert. Echter
  Index aus dem Root und per `cd docs/backlog && bash ../../.claude/scripts/backlog-index.sh`: beide
  „65 offen, 157 abgenommen, 13 verworfen", die beiden Ausgaben byte-gleich (Root gegen Unterverzeichnis; der
  Index selbst wird vor dem Commit regeneriert, wie immer), Laufzeit unverändert ~1,6 s.
  Harness: `scratchpad/index-probe.sh <repo-root>` (baut den Wegwerf-Root selbst; Kommandos in der Datei).
- **2026-10-23** — gegrillt und geschätzt im Nachtlauf (Freigabe 1); Ist-Stand präzisiert (Entscheidung 1).
- 2026-09-01 · Aufgenommen aus einer **eigenen Beobachtung** im Nachtlauf: Der Aufruf aus
  `docs/backlog/` heraus hat den Index gelöscht und trotzdem „Index geschrieben" gemeldet. Ist-Stand am
  Skript nachgelesen, Fundstelle `:24`. `entgangen_bei` bleibt leer — das Skript wurde im Backlog-Bereich
  nie abgenommen, es entstand als Werkzeug.
