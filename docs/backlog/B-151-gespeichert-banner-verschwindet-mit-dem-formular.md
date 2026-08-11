---
tags: [typ/story, status/idee, bereich/frontend, rolle/supervisor, rolle/creator]
aliases: [Gespeichert-Banner unsichtbar, onDone schließt das Formular, StatusBanner im geschlossenen Formular]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Fund beim Bauen von B-148 (2026-08-11)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
wartet_auf: ""
---

# B-151 · „Gespeichert." ist im Lehrbuch- und Fachlehrer-Formular nie zu sehen

Beide Bearbeiten-Formulare setzen ihre Erfolgsmeldung und schließen sich im selben Atemzug: `onDone`
ruft `setEditing(null)`, das Formular wird ausgehängt — und mit ihm der `StatusBanner`, der die Meldung
trägt. Der Nutzer sieht die Bestätigung nie, nur die geänderte Zeile.

Genau das ist bei der Lehrwerk-Reihe schon aufgefallen und behoben: `VaterLehrwerke.tsx:263-266` hält
das Formular nach dem Speichern absichtlich offen, mit dem Vermerk „im Rollengang aufgefallen —
„Gespeichert." war nie zu sehen". Die beiden anderen Formulare haben diese Runde nie gehabt.

Gefunden beim Bauen von [B-148](B-148-lehrbuch-formular-zerstoert-fachnamen.md) und **bewusst nicht dort
mitgenommen**: Das Ziel jener Story (der Fachname überlebt) ist ohne diesen Punkt erfüllt, und ihn
mitzuschlucken hieße, eine geschätzte Story während des Bauens wachsen zu lassen. Er hat außerdem eine
eigene Entscheidung im Rücken — offen bleiben *oder* schließen sind beides vertretbare Muster, und die
Reihe hat sich für „offen bleiben" plus nachgezogenen Bezugspunkt entschieden.

**Kein `entgangen_bei`:** Der Zustand ist älter als B-148 und war in keiner abgenommenen Arbeit dieser
Woche enthalten.

## Verlauf

- **2026-08-11** — angelegt aus dem Bau von B-148. Belegstellen: `ChildMaterialSection.tsx` (`onDone`
  schließt beim Speichern), `VaterFachlehrer.tsx` (dieselbe Zeile), Gegenbeispiel
  `VaterLehrwerke.tsx:263-266`.
