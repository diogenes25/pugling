---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor]
aliases: [children.error im Assistenten, Assistent behauptet du hast keine Kinder]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
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

1. ~~Nur die drei hier, oder gleich der ganze Sammel-Durchgang über die 41 Stellen?~~ → Entscheidung 1.
2. ~~Reicht ein Banner, oder braucht „Bestehendes Kind" auch hier einen Wiederholweg?~~ → Entscheidung 2.

## Entscheidungen

1. **Nur die drei.** `children` ist handlungsleitend, die anderen 38 sind es nicht; ein Sammel-Fix über
   beide Klassen wäre eine Regel ohne Gefälle (dieselbe Begründung wie B-174, Entscheidung 2).
   **Kosten:** die Zahl aus B-174 sinkt nur um drei, die Frage bleibt offen.
2. **Banner *und* Wiederholweg** — und das ist keine Ermessensfrage, sondern **nachgesehen**: `onGo` des
   Steppers ruft nur `setStep`, und `children` hängt an `[session!.id]`. Über den Stepper zurück und wieder
   vor ändert also **keine** `useAsync`-Abhängigkeit, es fliegt keine neue Abfrage. Genau das steht als
   Kommentar schon in derselben Datei (B-162, Trefferbereich). Ohne den Knopf wäre die neue, wahre Meldung
   eine Verschlechterung: Sie stünde über einem Bildschirm, aus dem nur F5 herausführt — und der kostet den
   ganzen Assistenten, es persistiert kein Zustand. **Kosten:** ein geteilter Baustein `ListenAusfall`
   statt dreier Zeilen.
3. **Der Knopf „Bestehendes Kind" bleibt gesperrt**, auch mit Banner. Es gibt ja keine Liste; ihn zu öffnen
   führte in ein leeres Pulldown. Was fehlte, war nicht die Bedienbarkeit, sondern der Unterschied zwischen
   „keine da" und „nicht geladen". **Kosten:** keine.
4. **Das Banner bleibt während der Wiederholung stehen** (aus der Review-Runde). `useAsync.reload()` setzt
   `error` auf `null` und `loading` auf `true` im **selben** Schritt — an `fehler` allein hinge das Banner
   samt Knopf im Moment des Klicks aus: Der gedrückte Knopf verschwände unter dem Finger, der Fokus fiele
   auf `<body>`, und während der Wiederholung stünde gar nichts da. `ListenAusfall` hält den letzten Fehler
   darum fest, solange geladen wird. **Kosten:** ein Zustand mehr im Baustein — und erst dadurch ist
   `disabled={laedt}` überhaupt erreichbar; vorher wäre es toter Code gewesen.

## Bewusst nicht mitgenommen: die vierte Liste

`useExerciseTypes` hat **gar keinen Fehlerkanal** — `exerciseTypes.ts` schluckt den Fehlschlag absichtlich
(`.catch(() => setAll([]))`, begründet als „lesbar statt kaputt"). Fällt das Manifest aus, zeigt „Typ" nur
noch „– alle Typen –" und die Beschriftungen fallen auf die rohen Schlüssel zurück: dieselbe Klasse wie
`categories`, nur eine Ebene tiefer. **Innerhalb dieses Diffs nicht reparierbar** — der Hook ist geteilt,
ein `error` dort trifft jeden Konsumenten. Läuft als [B-215](B-215-typ-manifest-ohne-fehlerkanal.md)
weiter, damit der nächste Leser es nicht erneut herleitet.

## Rote Proben (erwartet / gemessen)

| Probe | erwartet | gemessen |
| --- | --- | --- |
| Ausgangsstand, `e2e/assistent-listen-fallen-aus.spec.ts` | 1 | **1** |
| Kinder-Ausfall wird nicht mehr gezeigt (`fehler={null}`) | 1 | **1** |

## Verlauf

- 2026-09-01 · Aufgenommen aus dem `frontend-reviewer`-Review zu Sprint 1 des Nachtlaufs. Fundorte vom
  Reviewer benannt und von mir gegengelesen. `prio: P2`, weil der `children`-Fall zu einem **doppelt
  angelegten Kind** führt und damit teurer ist als der in B-162 behobene Fall.
- **2026-09-04** — **ausformuliert → abgenommen** im Nachtlauf, Sprint 1. Autonom gegrillt unter Freigabe 1
  (`art: Defekt`); der zweite offene Punkt war eine **Frage an den Code**, keine Entscheidung, und ist dort
  beantwortet (Entscheidung 2). **Rollengang:** `e2e/assistent-listen-fallen-aus.spec.ts` fährt die
  Supervisor-Rolle im echten Browser gegen einen echten Server, mit künstlich abgeschossener Kinderliste —
  und einmal wieder zurück, damit belegt ist, dass der Riegel eine Tür hat. Kein Browser-Handgang möglich
  (keine Chrome-Anbindung), damit greift der dokumentierte Ersatz.
  Belege: `tsc -b` sauber, `vitest run` **368/368** grün, `playwright test` **59/59** grün.
  `frontend-reviewer` gelaufen, **kein Blocker**; seine sechs Funde mit Code- oder Testwirkung sind
  eingearbeitet (Protokoll `docs/pm-sitzung-2026-09-04-nacht.md`). Commit `6f7fc60`.
