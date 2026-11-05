---
tags: [typ/story, status/idee, bereich/frontend, rolle/creator]
aliases: [series_subject_in_use ohne deutschen Text]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-05.md#lauf-z
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-291]
---

# B-403 · Die Sperre beim Fachwechsel einer Reihe erscheint im deutschen Vater-Web als englischer Satz mit Ref-Nummer

`frontend/src/lib/api.ts` (`GERMAN_PROBLEM_TEXT`) übersetzt Fehlercodes, auf die der Nutzer selbst handeln kann – die Geschwister
`subject_in_use`, `publisher_in_use`, `interest_tag_in_use` stehen dort, gehalten von `errorMessage.test.ts`. Der neue Code
`series_subject_in_use` aus [B-291](B-291-fachwechsel-der-reihe-laesst-themen-stehen.md) (und B-401) fehlt: der Creator liest den
englischen Server-Satz plus `(Ref: …)`. Zu klären beim Ausformulieren: wie die Anzahlen (Übungen/Units) in den deutschen Text kommen.
Die E2E-Spec `reihe-fachwechsel-gesperrt.spec.ts` prüft heute den englischen Wortlaut und muss mitziehen.

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu Lauf Z, Sprint 14 (Retro Sprint 15; frischer `pugling-reviewer`, 🟡). Am Code gelesen.
