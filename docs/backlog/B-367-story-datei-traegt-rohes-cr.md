---
tags: [typ/story, status/abgenommen, bereich/doku]
aliases: [B-362 Story-Datei mit CR]
status: abgenommen
prio: P4
art: Defekt
groesse: XS
wo: doku
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf Q, Teil 2 in Lauf R, Teil 1 (docs/pm-sitzung-2026-11-03-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-362]
wartet_auf: ""
---

# B-367 · Die Story-Datei von B-362 trägt selbst rohe CR-Bytes

Ausgerechnet die Story über eine Datei mit einzelnem CR (B-362) trug zwei rohe CR-Bytes: wo der Text `\r\r\n` bzw. `\r`
zitieren sollte, schrieb das Heredoc-Skript echte Steuerzeichen (an einer dritten Stelle wurde aus `\r\n` ein echter
Zeilenumbruch im Code-Span). `git ls-files --eol` meldete `i/-text`.

## User Story

Als **Entwickler** möchte ich, dass Diff und Blame der Story lesbar sind und sie sagt, was sie meint.

## Ist-Stand am Code (Stand `617df82`)

- `docs/backlog/B-362-infohint-mit-crlf-und-einzelnem-cr-committet.md`: zwei CR-Bytes (selbst gezählt: an Byte 603 und 1039),
  `i/-text`.

## Die echte Lücke

Wie beschrieben; dieselbe Ursache wie B-362 und derselbe fehlende Wächter (Retro-Vorschlag (AY)).

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Die Bytes durch den gemeinten Text ersetzen** (`\r\r\n`, `\r`), den Satz mit dem verunglückten Code-Span umformulieren.

## Akzeptanzkriterien

1. Die Datei trägt kein CR und liegt als `i/lf` im Index.

## Schätzung

`XS`, Doku. Testweg: Bytezählung, `git ls-files --eol` nach dem Commit.

## Verlauf

- **2026-11-03** — angelegt aus der Nachschau zu Lauf Q, Teil 2 (`frontend-reviewer` über `617df82`, 🟡). Gebaut: beide Bytes
  ersetzt (Zählung danach 0), der Satz umformuliert; `git add --renormalize` zeigte `i/lf`.
- **2026-11-03** — Abnahme Lauf R, Teil 1: Vitest **627/627**, `tsc -b` sauber, E2E-Gesamtlauf **108 passed** (`exit=0`, nach dem letzten Code-Edit; darin der neue Zwilling). Rollengang: der Entwickler ist die Rolle (Diff und Blame der Story); kein Produktpfad berührt. **Abgenommen**, Commit siehe Protokoll ([Lauf R](../pm-sitzung-2026-11-03-nacht.md)).
