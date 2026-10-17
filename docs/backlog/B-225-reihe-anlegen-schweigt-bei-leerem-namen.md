---
tags: [typ/story, status/ausformuliert, bereich/frontend, rolle/creator]
aliases: [NewSeries stummer Pfad, Reihe anlegen ohne Namen]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-10-17-nacht.md (Nachschau zu Sprint 2026-09-05)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-212]
wartet_auf: ""
---

# B-225 · „Reihe anlegen“ ist bei leerem Namen klickbar und schweigt

Der Kommentar, den Sprint 2026-09-05 unter das Anlege-Formular der Reihe schrieb, behauptete: „dieses
Formular hat keine Eingabeprüfung, es sperrt den Knopf stattdessen." Gesperrt ist aber nur der
Verlags-Knopf. Der stumme Pfad ist älter als der Sprint; der Kommentar war neu – und falsch.

## User Story

Als **Creator** möchte ich, dass „Reihe anlegen" bei leerem Namen entweder gesperrt ist oder mir sagt, was
fehlt – statt dass der Klick nichts tut.

## Ist-Stand am Code

Nachgesehen am 2026-10-17 (Nachschau, Reviewer-Agent; von mir gegengelesen).

- `frontend/src/vater/VaterLehrwerke.tsx:1023`: `<button type="submit" … disabled={action.busy}>` – nur die
  Sperre während der Aktion.
- `:951` (`NewSeries.submit`): bei leerem Namen `return` ohne `fail(...)` – kein Banner, und seit B-212 auch
  kein lokaler Meldungsplatz mehr, der etwas sagen könnte.
- `:1045`: der Verlags-Knopf trägt `disabled={… || !form.newPublisher.trim()}` – das Muster, das der
  Kommentar für beide Knöpfe behauptete.
- Der Kommentar (`:1031-1034`) ist in diesem Nachtlauf wahrheitsgemäß nachgezogen und verweist hierher.

## Die echte Lücke

Ein Klick ohne Wirkung und ohne Wort. Die Reparatur ist eine Bedingung am Knopf
(`disabled={action.busy || !form.name.trim()}`) **oder** ein `action.fail("Der Name der Reihe fehlt.")` wie
in `SeriesForm` `:490` – eines von beiden, konsistent mit dem Verlags-Knopf daneben.

## Offene Punkte

1. Sperren (wie der Verlags-Knopf) oder melden (wie `SeriesForm`)? *Empfehlung: sperren* – derselbe
   Baustein, dasselbe Muster, und der Grund („der gesperrte Knopf nennt seinen Grund", B-160) steht als
   Pflichtfeld-Markierung am Namensfeld.

## Verlauf

- **2026-10-17** — angelegt aus der **Nachschau** zu Sprint 2026-09-05 (Nachtlauf, Protokoll
  `docs/pm-sitzung-2026-10-17-nacht.md`), `entgangen_bei: B-212` (der Kommentar ging durch die Abnahme; der
  Pfad selbst ist älter). Direkt `ausformuliert`, Ist-Stand mit `Datei:Zeile` belegt.
