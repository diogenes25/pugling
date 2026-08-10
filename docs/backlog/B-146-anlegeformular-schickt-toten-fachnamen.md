---
tags: [typ/story, status/ausformuliert, bereich/frontend, bereich/katalog, rolle/creator]
aliases: [NewSeries schickt subjectName, toter Fachname beim Anlegen, Rest aus B-142]
status: ausformuliert
prio: P3
art: Aufräumen
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: B-142 (beim Grillen von B-143 am 2026-08-10 gefunden)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
wartet_auf: ""
---

# B-146 · Das Anlege-Formular schickt einen Fachnamen, den der Server ignoriert

Ein Rest aus [B-142](B-142-fachname-driftet-gegen-fach-id.md). Dort wurde `seriesPatch.ts` bereinigt —
das **Anlege**-Formular daneben blieb stehen.

## User Story

Als **Entwickler** möchte ich, dass keine Stelle im Frontend eine Regel behauptet, die der Server nicht
mehr kennt — sonst lese ich beim nächsten Mal den Kommentar und nicht den Code.

## Ist-Stand am Code

`frontend/src/vater/VaterLehrwerke.tsx:590,597-598` in `NewSeries.submit`:

```tsx
const subject = subjects.find((s) => String(s.id) === form.subjectId);
…
// Den Fachnamen mitschicken: er trägt die Reihe auch dort, wo kein Katalog-Fach gewählt ist.
subjectName: subject?.name ?? null,
```

Beides ist seit B-142 gegenstandslos, und der Kommentar war es sogar schon vorher:

- **Ist ein Fach gewählt**, leitet der Server den Namen selbst ab und ignoriert den mitgeschickten
  (`SubjectNaming.ResolveNameAsync`, angewandt im `Create`). Totes Feld.
- **Ist keines gewählt**, liefert der Ausdruck `null`. Das Formular hat **kein Freitext-Feld fürs Fach** —
  der Kommentar verspricht also eine Fähigkeit, die es an dieser Stelle nie gab.

Es ist **kein Fehlverhalten**: Die angelegte Reihe ist in beiden Fällen korrekt. Nur die Zeile und ihr
Kommentar behaupten eine Bringschuld, die es nicht mehr gibt.

## Die echte Lücke

Dieselbe Klasse wie Fund 3 des `pugling-reviewer` zu B-142 (`seriesPatch.ts` begründete seine
Kompensation mit einer Server-Aussage, die falsch geworden war) — eine Datei weiter und im Anlege- statt
im Änderungspfad. Der Reviewer hat den einen Pfad geprüft, nicht den anderen; ich beim Beheben ebenso
wenig.

**Kein `entgangen_bei`:** Es ist kein durchgekommener Defekt, sondern toter Code mit falschem Kommentar —
`art: Aufräumen`. Der Vollständigkeit halber gehört aber notiert, dass B-142 seine eigene
Frontend-Bereinigung nicht zu Ende geführt hat.

## Akzeptanzkriterien

1. `NewSeries.submit` schickt kein `subjectName` mehr; der `subjects`-Nachschlag entfällt, sofern er
   nirgends sonst gebraucht wird.
2. Kein Kommentar im Frontend behauptet mehr, der Server leite den Fachnamen nicht ab.
3. Eine neu angelegte Reihe mit gewähltem Fach trägt danach denselben Fachnamen wie vorher —
   abgesichert durch den bestehenden E2E-Weg über `/vater/lehrwerke`.

## Verlauf

- **2026-08-10** — angelegt beim Messen für [B-143](B-143-formular-kennt-zustaende-des-modells-nicht.md).
  **Bewusst nicht in B-143 mitgenommen:** dessen Ziel — das Formular soll Zustände ausdrücken können, die
  das Modell erlaubt — ist ohne diesen Rest erfüllt, und hier geht es um das Gegenteil, nämlich ein Feld
  zu *entfernen*.
