---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/creator]
aliases: [Wort-PUT meldet Id 0 als unbekannte Referenz]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-05.md#lauf-w
unverifiziert: false
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-11-05"
entgangen_bei: [B-387]
---

# B-394 · Der Birkenbihl-Wort-`PUT` meldet `vocabularyId = 0` als `invalid_reference`, der CRUD-Weg als `validation_error`

B-387 entstand aus „zwei Wege zum selben Feld, zwei Antworten" und hat sie für die unbekannte Id angeglichen – für `0` und negative Ids
nicht: `SetWord` (`backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs`, `PUT …/birkenbihl/{exerciseId}/words/{wordId}`,
um `:586-592`) schlägt die Id einfach nach und antwortet `invalid_reference`, `POST`/`PUT` der Übung seit B-387 `validation_error`
(Regel aus B-329: `≤ 0` ist Formfehler). Kein Test deckt `SetWord` mit `0`. Die Summary am Birkenbihl-Validator behauptete
„wie der Wort-Endpunkt" – seit Lauf W auf die unbekannte Id eingeschränkt.

Zu klären beim Ausformulieren: `≤ 0` → `validation_error` auch in `SetWord` (vermutlich), mit einem Fall in `UnknownReferenceCodeTests`.

## User Story

Als **Creator** (oder KI-Agent) möchte ich für dasselbe Feld überall dieselbe Antwort: eine Vokabel-Id `≤ 0` ist ein Formfehler, eine
unbekannte eine fehlende Zeile – am Wort-Endpunkt wie beim Anlegen und Ersetzen der Übung.

## Entscheidungen

1. **(Defekt, im Lauf gegrillt)** `SetWord` prüft `vocabularyId ≤ 0` vor dem Nachschlagen → `validation_error` (Regel aus B-329).

## Akzeptanzkriterien (je Fall)

| Weg | `0` | negativ | unbekannt |
|---|---|---|---|
| Wort-`PUT` | `validation_error` | `validation_error` | `invalid_reference` (Gegenprobe) |
| Übung `POST`/`PUT` | seit B-387 | – | seit B-387 |

## Schätzung

`XS`, `wo: backend`, eine Zeile plus Summary.

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu Lauf W, Sprint 8 (Retro Sprint 9; frischer `pugling-reviewer`, `git show cae15a1`,
  🟢). **Entgangen bei B-387:** das Verhalten von `SetWord` ist älter, aber B-387 versprach die Angleichung und ihre Summary behauptete
  sie. Increment **dieses** Laufs: **Limit 2**, Lauf W endet nach Sprint 9.
- **2026-11-05** — ausformuliert, gegrillt, geschätzt und gebaut in Lauf X, Sprint 10. Fälle nach Vorschlag (BN)/(BO) als Tabelle, mit
  dem Vergleichsweg. `0` → `UnknownReferenceCodeTests.cs:200`, negativ → `:201`, unbekannt → `:202`. Rote Probe: der Ausgangszustand –
  der Test lief **vor** dem Fix und fiel in `:200` (`invalid_reference` statt `validation_error`).
- **2026-11-05** — Review `pugling-reviewer`: kein Fund; die Prüfung sitzt nach 404/403 (keine fremden Daten), vor dem Nachschlagen, mit demselben Text wie der geteilte Validator.
- **2026-11-05** — **abgenommen.** Volle Suite 1098/1098 nach dem letzten Edit; Rollengang Creator als E2E: volle Playwright-Suite 115 passed gegen ein frisch gestartetes Backend mit der Änderung (danach nur Test und DTO-Summary). **Grenze:** kein Klickweg – der Vokabelspeicher trimmt die Eingabe vorher (B-389), und der Wort-Endpunkt hat im Dialog keine Id-Eingabe (B-394); belegt per Integrationsfall. Vater und Sohn unberührt. Commit: siehe nächster Eintrag.
- **2026-11-05** — Commit `2312aaf`.
- **2026-11-05** — **Nachschau** (Lauf X, Retro Sprint 11): nichts entgangen; `:200`/`:201` fallen allein, `:202` ist als Gegenprobe beschriftet.
