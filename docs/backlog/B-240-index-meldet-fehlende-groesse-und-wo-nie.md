---
tags: [typ/story, status/ausformuliert, bereich/pm, bereich/doku]
aliases: [belege nach den Platzhaltern, Warnzeile bei nur leeren Dateien]
status: ausformuliert
prio: P2
art: Defekt
groesse: ""
wo: doku
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-10-23-nacht.md (Nachschau zu Sprint 2)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-131]
wartet_auf: ""
---

# B-240 · Der Index meldet fehlende `groesse`/`wo` nie – und verschluckt die Warnzeile, wenn nur leere Dateien da sind

Zwei Lücken im selben Wächter, beide in der Nachschau zu Sprint 2 gefunden – an der Story, die die ⚠-Tabelle
gerade repariert hatte: B-131 stand nach ihrer Abnahme **selbst** darin (README:842), aber nur mit zwei der
vier Felder, die ihr fehlten.

## User Story

Als **jemand, der den Backlog über den Index liest**, möchte ich, dass die ⚠-Tabelle *alle* fehlenden
Pflichtfelder nennt und auch dann erscheint, wenn nur kaputte Dateien da sind – sonst sagt „zwei Felder fehlen"
wörtlich die Unwahrheit, und ein leerer Bereich sieht sauber aus.

## Ist-Stand am Code

- `.claude/scripts/backlog-index.sh:192-193` setzt `groesse`/`wo` auf `—`, **bevor** `belege` (`:212`) sie
  prüft; `[ -n "$groesse" ]`/`[ -n "$wo" ]` (`:118-119`) sind damit immer wahr. Gemessen (Nachschau, Harness mit
  einer B-131-Kopie ohne die vier Felder): gemeldet werden nur `migration` und `vertragsbruch`. Vorbestehend
  seit `41f21eb` (awk-Umbau, 2026-08-06) – die alte Fassung prüfte vor dem Platzhalter.
- `:265`: die Bedingung, ob überhaupt ein Index-Block geschrieben wird, prüft `unbelegt` nicht. Ein
  Wegwerf-Root mit **nur** einer leeren Datei liefert „*Noch keine Story angelegt.*", Exit 0 – die ⚠-Zeile aus
  B-131 ist dann weg. Degenerierter Fall, aber B-131s AK 1 („nicht im Nichts") gilt damit nur, solange eine
  lesbare Story existiert.

## Die echte Lücke

Ein Wächter, dessen Meldung unvollständig ist, wirkt vollständig: niemand zählt die vier Felder nach, wenn
zwei gemeldet sind. Genau so ist B-131 mit ⚠ abgenommen worden – der Prüfende (der Lauf selbst) las „migration,
vertragsbruch", trug zwei Felder nach, und der Index sagte beim nächsten Lauf weiter nichts von `groesse`/`wo`,
weil er es nie gesagt hatte.

## Akzeptanzkriterien

1. Eine Story auf `abgenommen` ohne `groesse`, `wo`, `migration`, `vertragsbruch` erscheint in der ⚠-Tabelle
   mit **allen vier** Feldern (Harness aus B-190, Kopie ohne die Felder – vorher zwei gemeldet, nachher vier).
2. Ein Root, dessen einzige Story-Datei leer ist, schreibt die ⚠-Tabelle mit der Zeile „Datei ist leer" statt
   „Noch keine Story angelegt".
3. Der echte Index bleibt idempotent (`git diff` leer nach dem Lauf), Laufzeit im Sekundenbereich.

## Offene Punkte

1. `belege` vor die Platzhalter ziehen, oder `belege` die Rohwerte geben? *Empfehlung: die Rohwerte – die
   Platzhalter sind nur für die Tabellenzeile da.* Eine Zeile Umstellung.
2. Für `:265`: `unbelegt` in die Bedingung aufnehmen, damit die ⚠-Tabelle auch ohne lesbare Story erscheint.
3. Rote Probe im Harness aus B-190 (Kopie einer Story ohne die vier Felder → ⚠ nennt vier; Root mit nur
   leerer Datei → ⚠ erscheint).

## Verlauf

- **2026-10-23** — angelegt aus der **Nachschau** zu Sprint 2 des Nachtlaufs (Commit `95b0624`),
  `entgangen_bei: B-131` – die Abnahme von B-131 trug den Defekt, den dieser Bug verdeckt hat; B-131s
  Frontmatter ist in Sprint 3 nachgetragen. Direkt `ausformuliert`: beide Stellen mit Zeile und Messung belegt.
  **Der Fund beendet den Lauf nach Sprint 3** (Limit 2 des Backlog-Laufs: Defekt im Increment eines Sprints
  derselben Nacht).
