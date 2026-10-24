---
tags: [typ/story, status/idee, bereich/frontend, bereich/test, rolle/supervisor]
aliases: [Assistent-Vorwahl ohne Spec, AK 2 von B-177 nur nebenbei]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Nachschau zu Sprint 9 (fünfter Nachtlauf, docs/pm-sitzung-2026-10-24-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-177]
wartet_auf: ""
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

## Verlauf

- **2026-10-23** — angelegt in der Nachschau zu Sprint 9 (fünfter Nachtlauf); Grep über `frontend/e2e` nach Assistent-Specs
  ohne Pillenklick: einziger Treffer `feldhilfe.spec.ts:101`.
