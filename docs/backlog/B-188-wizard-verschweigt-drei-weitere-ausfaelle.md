---
tags: [typ/story, status/ausformuliert, bereich/frontend, rolle/supervisor]
aliases: [children.error im Assistenten, Assistent behauptet du hast keine Kinder]
status: ausformuliert
prio: P2
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: frontend-reviewer zum Sprint 1 des Nachtlaufs 2026-09-01
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-188 · Der Assistent verschweigt drei weitere Ausfälle — einer schickt den Vater ins Kind-Anlegen

[B-162](B-162-assistent-nennt-den-leeren-katalog-als-ursache.md) hat die **Übungssuche** von Schritt 3
wahrheitsfähig gemacht. Im selben Bildschirm stehen drei Geschwister derselben Klasse, die sie nicht
angefasst hat — und einer davon ist schlimmer als der behobene.

## User Story

Als **Vater** möchte ich im Assistenten erfahren, wenn eine Liste nicht geladen werden konnte, damit ich
nicht ein Kind ein zweites Mal anlege, das es längst gibt.

## Ist-Stand am Code

`VaterWizard.tsx:53`, `:54`, `:132` laden `children`, `subjects` und `categories`. **Keiner der drei
`.error` wird in der ganzen Datei gerendert** (nachgezählt).

**Der schwerste Fall ist `children`:** Scheitert `api.children()`, bleibt `children.data` auf `null` — und
der Knopf „Bestehendes Kind" ist dann `disabled` (`:399`). Der Assistent behauptet damit stumm *„du hast
keine Kinder"* und lässt nur noch den Weg „neues Kind anlegen" offen. Das ist dieselbe falsche
Handlungsanweisung, gegen die B-162 angetreten ist — nur zwei Schritte früher, und mit einem **doppelt
angelegten Kind** als Folge statt einer doppelten Übung.

**Die Regel ist im Repo längst etabliert**, `VaterWizard` ist die Ausnahme: `VaterLehrwerke.tsx:186-187`,
`VaterFachlehrer.tsx:68-69` und `ChildMaterialSection.tsx:63` zeigen ihre Optionslisten-Fehler seit B-172
ausdrücklich an.

**Warum nicht in B-162 mitgenommen:** außerhalb ihres Diffs — B-162 hat den Trefferbereich von Schritt 3
herausgezogen, die drei Abrufe liegen im Seitenkopf. Und B-162 war `geschaetzt`; sie während des Bauens
wachsen zu lassen hätte ihre Schätzung wertlos gemacht.

**Bezug zur gemessenen Breite:** Diese drei sind Teil der 41 von 105 `useAsync`-Aufrufstellen ohne
Fehleranzeige, die im selben Lauf für [B-174](B-174-arten-liste-verschweigt-ihren-fehler.md) erhoben
wurden. Sie werden hier herausgezogen, weil `children` **nicht** bloß eine Auswahlmöglichkeit verliert,
sondern eine falsche Handlung auslöst.

## Offene Punkte

1. Nur die drei hier, oder gleich der ganze Sammel-Durchgang über die 41 Stellen? Empfehlung: **nur die
   drei** — `children` ist handlungsleitend, die anderen 38 sind es nicht, und ein Sammel-Fix über beide
   Klassen wäre eine Regel ohne Gefälle (dieselbe Begründung wie B-174, Entscheidung 2).
2. Reicht ein Banner, oder braucht „Bestehendes Kind" auch hier einen Wiederholweg wie in B-162?
   Empfehlung: nachsehen, ob der Stepper-Rückweg eine neue Abfrage auslöst — bei der Übungssuche war
   ausgerechnet das die wirkungslose Bewegung.

## Verlauf

- 2026-09-01 · Aufgenommen aus dem `frontend-reviewer`-Review zu Sprint 1 des Nachtlaufs. Fundorte vom
  Reviewer benannt und von mir gegengelesen. `prio: P2`, weil der `children`-Fall zu einem **doppelt
  angelegten Kind** führt und damit teurer ist als der in B-162 behobene Fall.
