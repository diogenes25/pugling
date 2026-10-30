---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/test, rolle/supervisor]
aliases: [Assistent-Vorwahl ohne Spec, AK 2 von B-177 nur nebenbei]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Sprint 9 (fünfter Nachtlauf, docs/pm-sitzung-2026-10-24-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-177]
wartet_auf: ""
nachgeschaut: "2026-10-30"
---

# B-262 · „Ohne Eingabe bleibt die Vorwahl ‚bestehendes Kind‘“ hält keine Spec gezielt

B-177 hat den stillen Tausch eines eingetippten neuen Kindes gegen ein bestehendes behoben (`touchedMode` per
`onChange` am Neues-Kind-Formular, `VaterWizard.tsx`). Sein AK 2 – ohne Eingabe wählt der Assistent weiter das
bestehende Kind vor – hält keine Spec gezielt: alle Assistent-Specs klicken die Pille ausdrücklich
(`assistent.spec.ts:26` u. a.). Nur nebenbei trägt es `feldhilfe.spec.ts:101` („Weiter“ ohne Eingabe), und diese
Zeile wartet selbst nicht darauf, dass die Kinderliste angekommen ist (vorbestehend). Ein Fix, der `touchedMode`
zu früh setzt (etwa schon beim Rendern des Formulars), bliebe damit wahrscheinlich grün – belegt war die Vorwahl
nur im Rollengang.

Vorschlag: ein Fall in `assistent-kinderliste-kommt-spaet.spec.ts` ohne Eingabe – Liste spät freigeben, dann
erwarten, dass „Bestehendes Kind“ gewählt ist; rote Probe: `touchedMode` initial auf `true`.
**Nicht in diesem Lauf behoben:** Increment von Sprint 9 (vierter Lauf), vom Auftrag ausgenommen.

## User Story

Als **Vater** ohne neues Kind möchte ich, dass der Assistent mein bestehendes Kind vorwählt, auch wenn die Kinderliste
spät kommt.

## Ist-Stand (Lauf L, am Code belegt)

Siehe oben. Gemessen beim Bau: `ChoicePill` trägt kein `aria-pressed` → [B-348](B-348-assistent-pillen-ohne-aria-pressed.md).

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Zweiter Fall in `assistent-kinderliste-kommt-spaet.spec.ts`:** Liste anhalten, Ausgangslage „Namensfeld sichtbar"
   feststellen, freigeben, dann Kind-Auswahl sichtbar und kein Namensfeld. Die Pille selbst ist nicht abfragbar (B-348).
   *Kosten:* ~20 s Laufzeit.

## Akzeptanzkriterien

1. Mutante `touchedMode` beginnt mit `true` → der neue Fall fällt, der alte bleibt grün.

## Schätzung

`XS`, nur Test.

## Verlauf

- **2026-10-23** — angelegt in der Nachschau zu Sprint 9 (fünfter Nachtlauf); Grep über `frontend/e2e` nach Assistent-Specs
  ohne Pillenklick: einziger Treffer `feldhilfe.spec.ts:101`.
- **2026-10-30** — Lauf L, Teil 3: gebaut. Beide Fälle **2 passed**; Mutante `useState(true)` für `touchedMode` →
  **1 failed, 1 passed** (erwartet: der neue fällt, der alte nicht).
- **2026-10-30** — Review `frontend-reviewer`: 🟢 der Titel sagt „wählt vor", geprüft war nur die sichtbare Auswahl → `not.toHaveValue("")` ergänzt, Spec **2 passed** (ohne Zählung).
- **2026-10-30** — Abnahme Lauf L, Teil 3: `tsc -b` sauber, Vitest **585/585** (danach + 1 Fall in `SohnTest.test.tsx`, **14/14**), volle Backend-Suite siehe Protokoll. E2E-Gesamtlauf **102 passed** (`exit=0`, nach dem letzten Produktcode-Edit gestartet; danach nur Tests). Rollengang als E2E (Browser-Extension nicht genutzt): Vater über `assistent-kinderliste-kommt-spaet`, `assistent` und `uebungen-bewerten` (Kollege mit Schreibrecht), Sohn über die Klausur-Komponententests (Fehler → „Weiter versuchen" → nächste Frage; eine E2E-Spec mit gescheitertem `nextTest` bräuchte einen kaputten Server, das gehört auf Komponentenebene), Creator Regressionszeuge. Review `frontend-reviewer`: kein 🔴/🟡. **Abgenommen**, Commit siehe Protokoll ([Lauf L](../pm-sitzung-2026-10-30-nacht.md)).
- **2026-10-30** — Nachschau in Lauf L, Teil 4: **sauber**. Geprüft: Ausgangslage im Fenster der angehaltenen Route; Mutanten `touchedMode` = `true`, Effekt entfernt, Wechsel ohne `childId` (letztere fängt `not.toHaveValue("")`); vorausgesetzt ist nur, dass Erwachsener 1 im Seed ein Kind hat.
