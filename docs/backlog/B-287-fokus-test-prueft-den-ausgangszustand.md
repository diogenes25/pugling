---
tags: [typ/story, status/abgenommen, bereich/test, bereich/frontend, rolle/student]
aliases: [Fokus-Test prüft den Ausgangszustand]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf E Teil 1 (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-05]
nachgeschaut: "2026-10-27"
wartet_auf: ""
---

# B-287 · Der Fokus-Test in `LetterTiles.test.tsx` prüft eine Zusicherung, die nie fallen kann

Im Fall „holt den Fokus auf die erste Kachel der nächsten Karte, wenn er im Block stand“
(`frontend/src/components/LetterTiles.test.tsx:103-104`) steht nach dem Fokus-Check die Zeile
`expect(kacheln("d")[0].getAttribute("aria-disabled")).toBe("false")` mit dem Kommentar „Und die gelegten Kacheln der
alten Karte sind weg“. Vor dem `rerender` wird aber keine Kachel gelegt (nur `focus()`), `picked` ist von Anfang an
leer – keine Änderung an der Komponente macht die Zeile rot (CLAUDE.md: „Eine Zusicherung, die den Ausgangszustand
prüft, kann nie fehlschlagen“). Den Reset trägt bereits der Fall „verwirft die gelegten Kacheln, sobald neue Kacheln
kommen“ (`:127-134`).

**Behebung:** vor dem `rerender` eine Kachel legen (`fireEvent.click(kacheln("a")[0])`), dann fällt die Zeile mit
entferntem Render-Reset tatsächlich; oder die zwei Zeilen samt Kommentar streichen und den Fall auf die Fokusfrage
beschränken.

## Verlauf

- **2026-10-27** — angelegt aus der Nachschau zu Lauf E Teil 1 (`4e50370`) ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)),
  Fund 1 des frischen Prüfers; `entgangen_bei: B-05`. Dieselbe Fehlerklasse, die CLAUDE.md bereits als viermal gemessen
  führt – jetzt fünfmal, und wieder von einem Reviewer gefunden, von keinem Tor.
- **2026-10-27** — in Lauf E, Teil 3, autonom gegrillt (Freigabe 1, `Defekt`), geschätzt (XS, nur Test) und gebaut: Der Fall legt vor dem Kartenwechsel Kachel 0 und hält den Wert über einen kleinen Harness (`MitWert`, referenzstabile Karten) – nur der Kartenwechsel darf zurücksetzen. **Rote Probe:** Render-Reset entfernt → **2 failed** (dieser Fall und der Reset-Fall; vorher 1), danach `LetterTiles.test.tsx` **10/10**.
- **2026-10-27** — Review (`frontend-reviewer`, [Protokoll](../pm-sitzung-2026-10-27-nacht.md)): sauber; Ablauf nachgerechnet (der `value === ""`-Effekt greift nicht, weil der Wert „b“ bleibt – nur der Render-Reset leert). 🟢 Kommentar zur Referenzstabilität begründete die Konstanten falsch → umformuliert, ohne Zählung. Vitest **454/454**.
- **2026-10-27** — `abgenommen` in Lauf E, Teil 3; Rollengang als Komponententest (kein sichtbarer Weg ändert sich). Commit `12eab76`.
- **2026-10-27** — Nachschau in Lauf E, Teil 4 ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)), frischer Prüfer an einem `git archive` von `12eab76`: Ablauf `value`/`picked` durch die Komponente verfolgt (der Leer-Effekt greift bei „b“ nicht, nur der Render-Reset), Mutante „kein Render-Reset“ im Export **2 failed** mit der `aria-disabled`-Zeile als Fallstelle. **Kein Befund.**
