---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/creator]
aliases: [Id 0 an Inline-Items und Item-PATCH ungetestet]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-05.md#lauf-v
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-329]
---

# B-388 · Zwei Fälle „Id 0 bleibt `validation_error`“ aus B-329 haben keinen Test

B-329 AK 1 sagt, eine Id `0` bleibe Formfehler (`validation_error`). Belegt ist das nur für `refs` und den Item-`POST`. Für
Inline-`items` (`ExerciseControllers.cs:59-60`) und den Item-`PATCH` (`:262`) fehlt der Fall: würde dort `0` zu
`invalid_reference` umgestellt, bliebe jede Suite grün. Je eine Zusicherung in `UnknownReferenceCodeTests` schließt das.

## User Story

Als **Entwickler** möchte ich, dass jede Stelle, an der B-329 „Id 0 bleibt Formfehler" behauptet, einen Test hat, der fällt, wenn
die Stelle `0` als unbekannte Referenz meldet.

## Ist-Stand am Code (Stand `f78ff5d`)

- `backend/Pugling.Api.Tests/UnknownReferenceCodeTests.cs`: `0` ist belegt für `refs` (`:49-50`) und den Item-`POST` (`:88`), nicht für
  Inline-`items` (Validator `ExerciseControllers.cs:59-60`) und den Item-`PATCH` (Route `:262`).

## Entscheidungen

1. **(Bau)** Je eine Zusicherung in den bestehenden Fällen, kein neuer Fall: die Unterscheidung gehört neben ihr Gegenstück.

## Akzeptanzkriterien

1. Inline-Item mit `vocabularyId = 0` → `validation_error`, getestet.
2. Item-`PATCH` mit `vocabularyId = 0` → `validation_error`, getestet.

## Schätzung

`XS`, `wo: backend`, nur Tests. Rote Probe je Stelle: `0` auf `invalid_reference` umstellen.

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu Lauf U, Sprint 4 (Lauf V, Retro Sprint 5; frischer `pugling-reviewer`,
  `git show f4cf656`, 🟢). Entgangen bei B-329: das AK behauptete die Unterscheidung, der Test trug sie nur an zwei von vier Stellen.
- **2026-11-05** — ausformuliert und geschätzt in Lauf V.
- **2026-11-05** — gebaut in Lauf W, Sprint 8 (mit B-387). AK 1 → `UnknownReferenceCodeTests.cs:67` (Inline-Item `0` →
  `validation_error`), AK 2 → `:101` (Item-`PATCH` `0`). Rote Proben: Inline-Validator meldet `0` als `invalid_reference` → rot nur in
  `:67`; der `PATCH` wählt den Code mit `>= 0` → rot nur in `:101`; je 1 von 18 im gefilterten Lauf, beide Mutanten kompiliert
  (Build-Prüfung im Proben-Skript, Vorschlag (BL) angewandt).
- **2026-11-05** — Review `pugling-reviewer` (gemeinsam mit B-387): kein Fund zu B-388.
- **2026-11-05** — **abgenommen.** Volle Suite 1089/1089 (danach nur Kommentartext geändert); Rollengang Creator als E2E: volle Playwright-Suite 115 passed gegen ein Backend, das nach dem letzten Code-Edit startete. Birkenbihl- und Übersetzungs-Anlage im Browser (`uebungstypen.spec.ts`) und der Vokabelspeicher grün; Vater und Sohn unberührt (nur Creator-Schreibwege), ihre Specs in derselben Suite grün. Commit: siehe nächster Eintrag.
