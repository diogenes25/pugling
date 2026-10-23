---
tags: [typ/story, status/abgenommen, bereich/pm, bereich/doku]
aliases: [belege nach den Platzhaltern, Warnzeile bei nur leeren Dateien]
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: doku
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-10-23-nacht.md (Nachschau zu Sprint 2)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-131]
wartet_auf: ""
nachgeschaut: "2026-10-23 (zweiter Lauf, Sprint 5, frischer Agent, gegengelesen): belege :195-197 vor den Platzhaltern :199-202, grund erst :247, kein Pfad setzt einen Platzhalter vorher; AK 1-3 im Wegwerf-Root nachgestellt (vier Felder gemeldet, Nur-leere-Datei-Root mit Tabelle, Idempotenz byte-gleich); eigene Faelle verworfen ohne grund, art Tippfehler, groesse leerer String (awk strippt Quotes, backlog-index.awk:54) alle gemeldet; kein Defekt. Nit: Vergleiche gegen '""' in :230/:239 sind seit dem Quote-Strip unerreichbar"
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

## Entscheidungen

Gegrillt im zweiten Nachtlauf 2026-10-23 (Freigabe 1, `art: Defekt`).

1. **`belege` bekommt die Rohwerte** (Punkt 1): der Aufruf wandert **vor** die Platzhalter-Zeilen, die
   Platzhalter bleiben für die Tabellenzeile. Begründung: die Platzhalter sind Darstellung, die Prüfung ist
   Inhalt – zwei Zwecke, eine Variable, und die Reihenfolge entschied, welcher gewann. **Kosten:** eine
   Zeilenverschiebung; `row` entsteht weiter nach den Platzhaltern.
2. **Ein drittes Feld derselben Familie wird mitgenommen:** `:194` setzt auch `art` auf `—` vor `belege`. Ein
   fehlendes `art` wird dadurch nie als „Feld `art`" gemeldet, sondern als „`art` ist kein bekannter Wert" –
   gemessen am Code (`:135-139`: der Fall `""` ist unerreichbar). Dieselbe Umstellung heilt es. Begründung:
   Freigabe 3 – ein Fund im Diff des Sprints wird behoben, nicht notiert. **Kosten:** keine zusätzliche.
3. **`:265` prüft `unbelegt` mit** (Punkt 2): ist die ⚠-Liste nicht leer, wird der volle Block geschrieben
   („Offen (0)" + „*Keine.*" + ⚠-Tabelle) statt „*Noch keine Story angelegt.*". Begründung: B-131s AK 1 gilt
   sonst nur, solange eine lesbare Story existiert. **Kosten:** im degenerierten Fall eine leere Offen-Tabelle
   mehr – ehrlicher als ein leerer Bereich.
4. **Beleg als Wegwerf-Harness, kein Tor im Repo** (wie B-190, Entscheidung 5): der Harness des ersten Laufs
   (`index-probe.sh`) bekommt drei Proben P4–P6 (Story ohne die vier Felder, Root mit nur leerer Datei, Story
   ohne `art`), rote Zahl vor dem Fix im Verlauf. Das vorgeschlagene **Tor für den Wächter** (Retro Sprint 3)
   wird in diesem Lauf nicht gelandet – Freigabe 3. **Kosten:** kein Rückfallschutz; der Vorschlag liegt beim
   Nutzer.

## Schätzung

**Größe XS** (Anker B-02: eine Datei, wenige Zeilen). `wo: doku` (Werkzeug des Backlog-Bereichs), keine
Migration, kein Vertrag.

**Angriffsplan:** Harness erweitern (P4–P6) → rote Proben mit Zahl → `belege` vor die Platzhalter, `unbelegt`
in die Bedingung `:265` → Proben grün → echter Index zweimal (Idempotenz-Diff leer) → Reviewer → Commit.

**Risiko:** die ⚠-Tabelle des echten Index wird länger, weil jetzt auch `groesse`/`wo` gemeldet werden – das ist
der Zweck, aber die Zeilen gehören nachgezogen oder stehen bewusst als Schuld da.

**Testweg:** Harness-Proben P4–P6 (erwartet/gemessen), Idempotenz des echten Index.

## Verlauf

- **2026-10-23** — **abgenommen** (zweiter Nachtlauf, Sprint 4). Reviewer (allgemeiner Agent, Brief: Skript +
  Doku + REST-Datei): Reihenfolge der Rohwerte nachvollzogen (`:195` vor `:200-202`, `grund` erst `:247`),
  `set -u`-Pfade, Markdown im Nur-⚠-Fall (MD055/56/60), Harness P4–P6 selbst gefahren plus fünf eigene
  Fälle (E1 `geschaetzt` ohne `wo` → „Feld `wo`", in der HEAD-Fassung **nicht gemeldet**; E2–E5 wie erwartet),
  Idempotenz; **ohne Blocker, kein Fund mit Codewirkung** am Skript. Ein Fund mit Doku-Wirkung (Index nicht
  regeneriert, B-242-Zeile fehlte) – behoben durch den Lauf vor dem Commit. **Rollengang:** der PM selbst
  (Harness + echter Index zweimal, byte-gleich); die ⚠-Tabelle des echten Index ist **nicht** gewachsen – keine
  abgenommene Story fehlt heute `groesse`/`wo`. Das vorgeschlagene Tor (Retro Sprint 3) bleibt Vorschlag.
  Commit `7c33765` (Protokoll `docs/pm-sitzung-2026-10-23-nacht.md`, „Sprint 4").
- **2026-10-23** — **gebaut** (zweiter Nachtlauf, Sprint 4). **Rote Proben (erwartet/gemessen)** im Harness
  `scratchpad/index-probe.sh <repo-root>` (Fortschreibung des B-190-Harness): **P4** B-131-Kopie auf
  `abgenommen` ohne die vier Felder – erwartet vier gemeldete Felder, gemessen **zwei** („Feld `migration`,
  Feld `vertragsbruch`"); **P6** B-190-Kopie ohne `art` – erwartet „Feld `art`", gemessen „`art` ist kein
  bekannter Wert"; **P5** Root mit nur einer leeren Datei – erwartet ⚠-Tabelle „Datei ist leer", gemessen
  „*Noch keine Story angelegt.*", Exit 0. **Nach dem Fix:** P4 „Feld `groesse`, Feld `wo`, Feld `migration`,
  Feld `vertragsbruch`"; P6 „Feld `art`"; P5 voller Block (Offen (0) · *Keine.* · Entgangen (0) · ⚠ mit
  B-900 „Datei ist leer"), Exit 0. Echter Index: „65 offen, 162 abgenommen, 13 verworfen" (Stand vor den vier neuen Stories; im Commit
  66/165/13), zweiter Lauf byte-gleich (`cmp`), Laufzeit unverändert im Sekundenbereich.

- **2026-10-23** — gegrillt und geschätzt im zweiten Nachtlauf desselben Tages (Freigabe 1); ein drittes Feld derselben Familie (`art`) beim Grillen am Code gefunden (Entscheidung 2).

- **2026-10-23** — angelegt aus der **Nachschau** zu Sprint 2 des Nachtlaufs (Commit `95b0624`),
  `entgangen_bei: B-131` – die Abnahme von B-131 trug den Defekt, den dieser Bug verdeckt hat; B-131s
  Frontmatter ist in Sprint 3 nachgetragen. Direkt `ausformuliert`: beide Stellen mit Zeile und Messung belegt.
  **Der Fund beendet den Lauf nach Sprint 3** (Limit 2 des Backlog-Laufs: Defekt im Increment eines Sprints
  derselben Nacht).
