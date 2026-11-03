---
tags: [typ/story, status/idee, bereich/frontend]
aliases: [InfoHint CRLF, i/-text]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: Nachschau zu Lauf P, Teil 2 in Lauf Q, Teil 1 (docs/pm-sitzung-2026-11-03-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-273]
wartet_auf: ""
---

# B-362 · `InfoHint.tsx` ist mit CRLF und einem einzelnen CR committet – git hält es nicht mehr für Text

Der Commit `a25a1a6` (B-273) legte `frontend/src/components/InfoHint.tsx` mit CRLF in allen 142 Zeilen und einem `
` in
Zeile 26 ab. Wegen des einzelnen CR meldet `git ls-files --eol` `i/-text`: git normalisiert die Datei nicht mehr, und der
Diff schreibt alle Zeilen um (Blame verloren); ohne Zeilenenden sind es +27/−14. Selbst nachgemessen: 142 × CRLF, 1 × CRCR im
Blob von `HEAD`. Einzige Datei unter `frontend/src` mit CR.

**Ursache:** ein eigenes Python-Skript beim Bau – es ersetzte `
  const remeasure…` durch nichts und ließ das `` der
Vorzeile stehen; ein anderes schrieb die Datei mit CRLF zurück, weil die Arbeitskopie CRLF trug.

## Verlauf

- **2026-11-03** — angelegt aus der Nachschau zu Lauf P, Teil 2 (frischer `frontend-reviewer` über `a25a1a6`, 🟡). Befund
  selbst nachgemessen (Bytezählung am `HEAD`-Blob).
