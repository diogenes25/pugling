---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/test]
aliases: [waitForResponse statt App-Zustand, AK 3 von B-169 hängt weiter am Timing]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau Sprint 11 (sechster Nachtlauf, docs/pm-sitzung-2026-10-26-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-180]
wartet_auf: ""
nachgeschaut: "2026-10-30"
---

# B-266 · Der Wartepunkt aus B-180 wartet aufs Netzwerk, nicht auf die App

B-180 sollte eine Zusicherung vom Scheduling lösen. `frontend/e2e/assistent.spec.ts:183-186` wartet jetzt mit
`page.waitForResponse` – das löst aus, sobald die Antwort ankommt, nicht wenn die App sie verarbeitet hat (Body
lesen, `setState`, Commit liegen dazwischen). Die erste Auswertung von `toBeDisabled()` (`:191`) kann darum weiter
in das Fenster fallen, in dem `laedt` den Knopf allein sperrt (`VaterWizard.tsx:723`:
`disabled={selectAllBusy || laedt || zeilenVeraltet}`); ohne `zeilenVeraltet` bliebe der Fall dann grün. Nach dem
Fix lief keine rote Probe, nur grüne Läufe.

`unverifiziert: true`: nur strukturell belegt, der Nachschau-Prüfer durfte kein Playwright fahren. Beweisweg:
`zeilenVeraltet` aus `VaterWizard.tsx:723` entfernen, den Fall mit `--repeat-each 20` fahren – jeder grüne Lauf
belegt den Defekt. Vorschlag: vorher auf einen App-Zustand warten (z. B. „Suche gescheitert“, `VaterWizard.tsx:714`),
gegen `useAsync` gegenprüfen. **Nicht in diesem Lauf behoben:** außerhalb des Sprint-11-Diffs.

## User Story

Als **Entwickler** möchte ich, dass der Fall „Scheitert die Suche, ist auch »Alle wählen« gesperrt" an der App hängt, nicht
am Netzwerk-Timing.

## Ist-Stand (Lauf L, gemessen)

Der Beweisweg der Story wurde gefahren: `zeilenVeraltet` am Knopf entfernt, Fall `--repeat-each 20` → **20 failed**. Der
behauptete Fehlgrün ist lokal (Windows) **nicht reproduziert**. Das Fenster zwischen Antwort und Commit gibt es
strukturell trotzdem; ob es auf Linux in CI aufgeht, ist hier nicht messbar.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Billig härten statt verwerfen:** nach `waitForResponse` zusätzlich auf „Suche gescheitert" warten – das steht erst
   im Render, in dem `loading` falsch ist. *Kosten:* eine Zeile; der Fall wird nicht langsamer.

## Akzeptanzkriterien

1. Mit der Härtung: Mutante „ohne `zeilenVeraltet`" fällt jedes Mal (10×), ohne Mutante grün (5×).

## Schätzung

`XS`, nur Test.

## Verlauf

- **2026-10-26** — angelegt aus der Nachschau zu B-180 (sechster Nachtlauf, Sprint 11); strukturell belegt.
- **2026-10-30** — Lauf L, Teil 3: Beweisweg gefahren, **20 failed von 20** (Defekt lokal nicht reproduziert); Härtung
  trotzdem gebaut (Entscheidung 1). Danach Mutante **10 failed von 10**, ohne Mutante **5 passed von 5**.
- **2026-10-30** — Review `frontend-reviewer`: sauber; „Suche gescheitert" erscheint erst im Render mit `loading = false` (`useGemerkterFehler`), `exact` trennt es von „(Suche gescheitert)" in `PlanPositions`.
- **2026-10-30** — Abnahme Lauf L, Teil 3: `tsc -b` sauber, Vitest **585/585** (danach + 1 Fall in `SohnTest.test.tsx`, **14/14**), volle Backend-Suite siehe Protokoll. E2E-Gesamtlauf **102 passed** (`exit=0`, nach dem letzten Produktcode-Edit gestartet; danach nur Tests). Rollengang als E2E (Browser-Extension nicht genutzt): Vater über `assistent-kinderliste-kommt-spaet`, `assistent` und `uebungen-bewerten` (Kollege mit Schreibrecht), Sohn über die Klausur-Komponententests (Fehler → „Weiter versuchen" → nächste Frage; eine E2E-Spec mit gescheitertem `nextTest` bräuchte einen kaputten Server, das gehört auf Komponentenebene), Creator Regressionszeuge. Review `frontend-reviewer`: kein 🔴/🟡. **Abgenommen**, Commit siehe Protokoll ([Lauf L](../pm-sitzung-2026-10-30-nacht.md)).
- **2026-10-30** — Nachschau in Lauf L, Teil 4: **sauber**. Geprüft: `useGemerkterFehler` liefert den Fehler erst im Render mit `loading = false`; Mutante „ohne `zeilenVeraltet`“ damit deterministisch rot; `exact` fiele bei doppeltem Text laut.
