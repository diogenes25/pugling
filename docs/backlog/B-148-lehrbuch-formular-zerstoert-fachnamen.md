---
tags: [typ/story, status/idee, bereich/frontend, bereich/katalog, rolle/supervisor]
aliases: [ChildMaterialSection clearSubject, Lehrbuch verliert Fachnamen, B-143 am Kind]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: frontend-reviewer im Nachtlauf Sprint 3 (2026-08-10), Fund neben dem Diff von B-143
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
wartet_auf: ""
---

# B-148 · Das Lehrbuch-Formular am Kind zerstört den Fachnamen bei jedem Speichern

Dieselbe Fehlerklasse wie [B-143](B-143-formular-kennt-zustaende-des-modells-nicht.md), eine Datei weiter
— und **ohne** den Schutz, der dort gratis abfiel. Vom `frontend-reviewer` gefunden, während er B-143
prüfte.

## User Story

Als **Supervisor** möchte ich am Lehrbuch meines Kindes eine Notiz ändern können, ohne dabei die
Fachangabe zu verlieren, die ich nie angefasst habe.

## Ist-Stand am Code

`frontend/src/vater/ChildMaterialSection.tsx:154-173`. Der Update-Rumpf baut den Lösch-Schalter aus dem
**aktuellen Formularwert** statt aus einem Vergleich gegen den Ladezustand:

```ts
clearSubject: dto.subjectId == null,
```

Dazu kennt das Fach-`<select>` nur Katalog-Fächer. Die Kette:

1. Ein Lehrbuch, dessen Fach gelöscht wurde, hat `subjectId: null` und `subjectName: "Englisch"`
   (`SetNull` räumt nur die Id — die Ursache ist [B-144](B-144-fach-loeschen-trifft-reihen-lautlos.md)).
2. Das Formular startet damit auf `subjectId: ""`, weil es den Zustand nicht darstellen kann.
3. **Jedes** Speichern eines beliebigen anderen Feldes schickt `clearSubject: true`.
4. Der Name ist weg, und daneben steht „Gespeichert.".

## Die echte Lücke

Der Unterschied zu B-143 ist der entscheidende: Dort **schützte der Diff-Vergleich** — `form` blieb gleich
`loaded`, also ging nichts mit, und der Defekt war „man kommt nicht heran". Hier gibt es keinen Vergleich,
sondern eine Ableitung aus dem Momentanwert. Der Defekt ist damit nicht „man kommt nicht heran", sondern
**aktive Zerstörung bei einer unbeteiligten Handlung** — deshalb `P2` statt `P3`.

## Warum das niemandem aufgefallen ist

Die Verfolgung ist **verwaist**: [B-137](B-137-freitext-fach-unerreichbar.md) hielt unter Punkt 3 fest,
dass `CreatorProfile` und `Textbook` dieselbe Frage stellen, und vermerkte „reist mit B-144". B-144 nennt
sie in der gebauten Fassung in **keinem** Akzeptanzkriterium — beim Grillen wurde die Frage auf das
Löschverhalten verengt, und der Rest fiel zwischen die Stories.

**Kein `entgangen_bei`:** Der Zustand ist älter als B-143/B-144 und wurde von keiner Abnahme
durchgelassen — er wurde in einer Notiz verfolgt und dort vergessen.

## Offene Punkte

1. **Trägt `CreatorProfile` dasselbe?** B-137 nannte beide in einem Atemzug; gemessen ist bisher nur
   `Textbook`. Vor dem Ausformulieren nachsehen — und beim Ergebnis benennen, welche Datei geprüft wurde.
2. **Übernimmt diese Story den Sentinel aus B-143 oder reicht der Diff-Vergleich?** Empfehlung: erst den
   Vergleich (er behebt die Zerstörung), den Sentinel nur, wenn der Zustand am Kind auch *angezeigt*
   werden soll. Die zwei Hälften sind trennbar, und die erste ist die dringende.

## Verlauf

- **2026-08-10** — angelegt aus dem Frontend-Review des Nachtlauf-Sprints 3. **Bewusst nicht im Sprint
  behoben:** der Fund liegt außerhalb seines Diffs, das Sprint-Ziel ist ohne ihn erreicht, und B-143 zu
  erweitern hieße, eine geschätzte Story während des Bauens wachsen zu lassen.
