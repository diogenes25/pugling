---
tags: [typ/story, status/ausformuliert, bereich/pm]
aliases: [backlog-index.sh loescht den Index, awk nicht gefunden exit 0]
status: ausformuliert
prio: P2
art: Defekt
groesse: ""
wo: doku
migration: ""
vertragsbruch: ""
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

## Verlauf

- 2026-09-01 · Aufgenommen aus einer **eigenen Beobachtung** im Nachtlauf: Der Aufruf aus
  `docs/backlog/` heraus hat den Index gelöscht und trotzdem „Index geschrieben" gemeldet. Ist-Stand am
  Skript nachgelesen, Fundstelle `:24`. `entgangen_bei` bleibt leer — das Skript wurde im Backlog-Bereich
  nie abgenommen, es entstand als Werkzeug.
