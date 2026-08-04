---
tags: [typ/story, status/idee, bereich/frontend, rolle/supervisor]
aliases: [Positionsliste Ladezustand, aufgeklappter Report schließt]
status: idee
prio: P3
art: Defekt
quelle: docs/backlog/B-10-zeitfenster-pro-kind.md
unverifiziert: true
---

# B-89 · Die Positionsliste hängt bei jeder Änderung den aufgeklappten Report aus

`PlanPositions.tsx` prüft `positions.loading ? "Lade Positionen…" : <tabelle>` und trifft damit genau die in
[frontend/CLAUDE.md](../../frontend/CLAUDE.md) dokumentierte `useAsync`-Falle: `reload` setzt `loading`
erneut, behält aber `data` — die Tabelle wird also bei **jeder** Änderung ausgehängt und neu gebaut. Alles,
was an einer Zeile aufgeklappt war (der 📊-Report), ist danach zu.

Regelkonform wäre `positions.loading && positions.data === null`; die Regel steht schon dort, nur diese
Datei folgt ihr nicht. Zu prüfen ist, ob weitere Listen im Vater-Web dieselbe Zeile tragen — dann ist es
eine kleine Sammelaufgabe statt einer Zeile.

Befund des `frontend-reviewer` beim Review zu [B-10](B-10-zeitfenster-pro-kind.md); vorbestehend, nicht von
B-10 verursacht.

## Verlauf

- **2026-08-04** — aus dem B-10-Review aufgenommen (ungeprüft: die Zahl der betroffenen Listen ist nicht
  gezählt).
