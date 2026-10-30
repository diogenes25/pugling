---
tags: [typ/story, status/idee, bereich/frontend, bereich/a11y, rolle/student]
aliases: [„Weiter versuchen" sagt die alte Frage als neue an]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: Nachschau zu Lauf K, Teil 2 in Lauf L (docs/pm-sitzung-2026-10-30-nacht.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-286]
wartet_auf: ""
---

# B-339 · „Weiter versuchen" in der Klausur sagt die schon beantwortete Frage noch einmal als neue an

[B-286](B-286-kartenwechsel-sagt-die-aufgabe-nicht-an.md) sagt jede neue Aufgabe in einer Live-Region über allen
Ansichten an. In der Klausur (`frontend/src/sohn/SohnTest.tsx`) setzt `resume()` nur `setError(null)` und lässt `item`
stehen. Bis `nextTest` antwortet, zeigt `asked = !error && !result ? item : null` wieder auf die alte Frage. Die Region
läuft darum `""` → „Neue Aufgabe: <alte Frage>" → „Neue Aufgabe: <neue Frage>".

Das trifft genau den Fall, für den „Weiter versuchen" da ist: `answerTest` ist durchgegangen, nur `nextTest` ist
gescheitert. Das Kind hört dann die schon beantwortete Frage als „neue Aufgabe" und danach die richtige. Die alte Karte
ist außerdem kurz sichtbar und anklickbar, weil `resume` nicht über `action` läuft (älter als B-286).

Der Test „sagt die fortgesetzte Frage in derselben Region an" (`SohnTest.test.tsx`) prüft nur den Endzustand.

Vorschlag des Prüfers: `setItem(null)` in `resume()` direkt nach `setError(null)`; im Test nach dem Klick auf „Weiter
versuchen" `region.textContent === ""` erwarten, bevor die neue Frage kommt.

## Verlauf

- **2026-10-30** — angelegt aus der Nachschau zu Lauf K, Teil 2 (`frontend-reviewer` über `448785c`, 🟡). Aus Code
  gelesen, nicht gemessen. Liegt im Increment von Lauf K, nicht in dem von Lauf L.
