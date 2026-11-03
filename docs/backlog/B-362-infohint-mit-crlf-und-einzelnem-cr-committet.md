---
tags: [typ/story, status/abgenommen, bereich/frontend]
aliases: [InfoHint CRLF, i/-text]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf P, Teil 2 in Lauf Q, Teil 1 (docs/pm-sitzung-2026-11-03-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-11-03"
entgangen_bei: [B-273]
wartet_auf: ""
---

# B-362 · `InfoHint.tsx` ist mit CRLF und einem einzelnen CR committet – git hält es nicht mehr für Text

Der Commit `a25a1a6` (B-273) legte `frontend/src/components/InfoHint.tsx` mit CRLF in allen 142 Zeilen und einem `\r\r\n` in
Zeile 26 ab. Wegen des einzelnen CR meldet `git ls-files --eol` `i/-text`: git normalisiert die Datei nicht mehr, und der
Diff schreibt alle Zeilen um (Blame verloren); ohne Zeilenenden sind es +27/−14. Selbst nachgemessen: 142 × CRLF, 1 × CRCR im
Blob von `HEAD`. Einzige Datei unter `frontend/src` mit CR.

**Ursache:** ein eigenes Python-Skript beim Bau – es entfernte die Zeile `const remeasure…` samt dem Zeilenumbruch davor und ließ das `\r` der
Vorzeile stehen; ein anderes schrieb die Datei mit CRLF zurück, weil die Arbeitskopie CRLF trug.

## User Story

Als **Entwickler** möchte ich, dass jede Textdatei mit LF im Index liegt, damit Diff und Blame zeigen, was sich wirklich
geändert hat.

## Ist-Stand am Code (Stand `a25a1a6`)

- `git ls-files --eol frontend/src/components/InfoHint.tsx` → `i/-text`; Blob: 142 × CRLF, 1 × `\r\r\n` (Zeile 26).

## Die echte Lücke

Wie beschrieben; kein Tor sieht CR im Index (Retro-Vorschlag (AY)).

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Die Datei auf LF bringen und das einzelne CR entfernen**, dann `git add --renormalize`. *Kosten:* der Commit schreibt
   noch einmal alle Zeilen um – danach ist die Datei wieder `i/lf`.

## Akzeptanzkriterien

1. `git ls-files --eol frontend/src/components/InfoHint.tsx` meldet `i/lf`, der Blob trägt kein CR.

## Schätzung

`XS`, frontend. Testweg: `git ls-files --eol`, Bytezählung am Blob; Vitest und die ⓘ-Specs als Regression.

## Verlauf

- **2026-11-03** — angelegt aus der Nachschau zu Lauf P, Teil 2 (frischer `frontend-reviewer` über `a25a1a6`, 🟡). Befund
  selbst nachgemessen (Bytezählung am `HEAD`-Blob).
- **2026-11-03** — Lauf Q, Teil 2: autonom gegrillt (Freigabe 1), gebaut. Datei auf LF, CR entfernt (Bytezählung 0); ohne
  Zeilenenden ändert sich genau eine Zeile (das `\r\r` in Zeile 26). Die Tor-Frage (AY) bleibt Vorschlag.
- **2026-11-03** — Review `frontend-reviewer` und `pugling-reviewer`: kein 🔴; ein 🟡 (InfoHint noch nicht gestaged – AK 1 von B-362 gilt erst nach dem Commit, dort per `git ls-files --eol` geprüft, siehe Protokoll; kein Code geändert, ohne Zählung); drei 🟢 (Umbruch, zwei Kommentar-Formulierungen) ohne Zählung behoben. Abnahme Lauf Q, Teil 2: volle Suite **1059/1059**, Vitest **621/621**, E2E-Gesamtlauf **107 passed** (`exit=0`, nach dem letzten Edit). Rollengang: der Entwickler ist die Rolle (Diff und Blame); Vater über die ⓘ-Specs im Gesamtlauf; Creator, Sohn Regressionszeugen. **Abgenommen**, Commit siehe Protokoll ([Lauf Q](../pm-sitzung-2026-11-03-nacht.md)).
- **2026-11-03** — Nachschau in Lauf R, Teil 1 (frischer `frontend-reviewer`, `git show 617df82`, read-only): **Entgleitung** → [B-367](B-367-story-datei-traegt-rohes-cr.md) (🟡: diese Story-Datei selbst trug zwei rohe CR-Bytes und lag als `i/-text` im Index – dieselbe Ursache, ein Heredoc-Skript). AK 1 an `InfoHint.tsx` erfüllt (Blob 0 CR).
