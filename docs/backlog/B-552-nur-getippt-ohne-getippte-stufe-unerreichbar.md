---
tags: [typ/story, status/idee, bereich/api, rolle/supervisor, rolle/student]
aliases: [Unerreichbare Pflicht, nur getippt ohne Stufe]
status: idee
prio: P1
art: Defekt
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: "Gesamt-Review und volle Playwright-Suite des Backlog-Laufs 2026-11-14 (docs/pm-sitzung-2026-11-14-backlog-lauf.md, Abschluss)"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-469, B-16]
---

# B-552 · „Nur getippte Tests“ ohne getippte Stufe macht die Pflicht unerreichbar – und kostet täglich Malus

Seit B-550 rechnet der Server die Lernkontrolle je Position (`PositionPlayService.LearningCheckOf`). Die volle
Playwright-Suite zeigte damit, was die alte Frontend-Ableitung verdeckte: Eine Vokabel-Position mit
`requireTypedTest: true` und **ohne** Stufe prüft auf der Vorgabestufe des Typs (`ExerciseTypeBase.DefaultStage` =
Selbsteinschätzung), ein solcher Versuch wird nie gewertet (`PositionTestsController`: `Graded = IsTypedStage`), das
Tagesziel ist also nie erreichbar und der Malus bucht jeden Tag. Bis B-550 stand dort in der Plan-Zeile „Test getippt“;
jetzt steht „Ziel nicht erreichbar …“ (`LearningCheckKind.Unreachable`). Der Server **lehnt** die Kombination aber nur
bei einem Stufenplan ab (`RequireTypedTestValidation`), nicht bei einer selbst eingeschätzten Basisstufe. Das ist genau
die Konfiguration „Häkchen an, Stufe auf Standard“, die das Positionsformular (B-16) mit einem Klick erzeugt.

Vorschlag zum Grillen: dieselbe Ablehnung (`validation_error`) auch für die Basisstufe beim Anlegen und beim PATCH, der
eine der Hälften setzt; Bestandspositionen bleiben (Meldung `Unreachable`), der Vater sieht sie rot. Vorher an einer
Kopie der echten DB zählen, wie viele Positionen heute so stehen (Friedrichs sechs Vokabel-Positionen tragen
`RequireTypedTest = true` – laut B-535 bestanden seine Tests, ihre Stufe ist also getippt; nachmessen).

## Verlauf

- **2026-11-15** — angelegt im Abschluss des Backlog-Laufs 2026-11-14: die E2E `lernkontrolle-sichtbar` erwartete für
  genau diese Position „Test getippt“ und fiel, nachdem der Server die Lernkontrolle selbst rechnete.
