---
tags: [typ/story, status/idee, bereich/frontend, bereich/a11y, rolle/student]
aliases: [Kartenwechsel sagt die neue Aufgabe nicht an]
status: idee
prio: P3
art: Aufräumen
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: frontend-reviewer zu B-05 in Lauf E (docs/pm-sitzung-2026-10-27-nacht.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-286 · Nach dem Kartenwechsel hört ein Screenreader die Eingabe, aber nicht die neue Aufgabe

Beim Üben (`SohnPractice.tsx`) und in der Klausur (`SohnTest.tsx`) wechselt die Karte, ohne dass die neue Aufgabe
(`prompt`) angesagt wird: Bei Buchstabenkästchen und dem Freitextfeld behält das `<input>` den Fokus, bei
„Buchstaben ordnen“ steht er seit B-05 auf der ersten Kachel („Buchstabe d, Schaltfläche“). Ein sehender Nutzer liest
die Aufgabe über dem Feld; ein Screenreader-Nutzer muss zurücknavigieren, um zu erfahren, was gefragt ist.

Altlast, nicht durch B-05 verursacht (vorher fiel der Fokus auf `body`, das war schlechter). Der Review nennt zwei
Wege: die Aufgabe beim Kartenwechsel in eine Live-Region spiegeln (Muster B-134, `role="status"` steht dauerhaft im
DOM) oder den Fokus auf die Karte selbst setzen und die Eingabe darunter lassen. Beides ist zu entscheiden, nicht nur
zu bauen.

## Verlauf

- **2026-10-27** — angelegt aus dem `frontend-reviewer` zu B-05 in Lauf E ([Protokoll](../pm-sitzung-2026-10-27-nacht.md)),
  Hinweis 5 (🟢, außerhalb des Diffs).
