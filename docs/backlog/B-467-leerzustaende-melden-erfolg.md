---
tags: [typ/story, status/idee, bereich/frontend, rolle/supervisor, rolle/student]
aliases: [Lernstand leer, frei mit Häkchen]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: "frontend"
migration: ""
vertragsbruch: ""
quelle: Rollengang am Access-Lehrwerk 2026-11-10 (Kopie der pugling.db, Sohn im Handy-Format 375×812, Vater-Web)
unverifiziert: true
entgangen_bei: []
grund: ""
ersetzt_durch: []
---

# B-467 · Ohne eine einzige Antwort meldet der Lernstand „es sitzt alles 👍“, und freie Übungen tragen ein Häkchen

Zwei Stellen lesen „nichts gemessen“ als „alles gut“:

- **Vater, `/vater/kind/{id}/lernstand`:** Bei einem Kind ohne jede Antwort steht „Wörter (0)“ und darunter
  „Kein Wort unter 50 % – es sitzt alles. 👍“. Der Vater, der den Lernfortschritt prüfen will, liest Erfolg, wo
  noch nicht gelernt wurde. Auch nach acht eingeführten Wörtern bleibt die Liste leer.
- **Sohn, `/sohn/progress` („Meine Übungen“):** Positionen ohne Pflicht („frei“) tragen ✅, obwohl sie nie
  gespielt wurden; die Pflicht-Positionen darüber tragen ⬜. Die API liefert für `cadence: None` `goalMet: true`.

Erwartet: Der Lernstand unterscheidet „noch nichts gelernt“ von „nichts Schwaches“; eine freie Position zeigt
ihr Häkchen erst nach einer gespielten Runde oder gar keins.

## Verlauf

- **2026-11-10** — angelegt (Prüfgang am Access-Lehrwerk: Übungen, Vater-Sicht, Handy).
