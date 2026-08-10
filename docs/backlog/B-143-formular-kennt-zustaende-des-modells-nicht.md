---
tags: [typ/story, status/ausformuliert, bereich/frontend, bereich/katalog, rolle/creator]
aliases: [Freitext-Fach nicht wegzubekommen, Schulart-Kombination geht verloren, Formular kennt Zustand nicht]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: B-137
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
wartet_auf: ""
---

# B-143 · Das Reihen-Formular kennt zwei Zustände nicht, die das Modell erlaubt

Abgespalten von [B-137](B-137-freitext-fach-unerreichbar.md) (dessen Punkte 1 und 5). Zweimal dieselbe
Fehlerklasse, ein Feld auseinander — und beide Male entscheidet **die Oberfläche**, nicht der Code, wie
die Lösung aussieht.

## User Story

Als **Creator** möchte ich, dass das Bearbeiten-Formular jeden Zustand ausdrücken kann, in dem eine Reihe
tatsächlich sein darf — sonst zerstöre ich beim Speichern etwas, das ich gar nicht angefasst habe.

## Ist-Stand am Code

**Zustand A — Freitext-Fach ohne Fach-Id.** Entsteht ohne Zutun (siehe
[B-144](B-144-fach-loeschen-trifft-reihen-lautlos.md)): die Zeile zeigt „Englisch" (Rückfall auf
`subjectName`, `VaterLehrwerke.tsx:135-137`), das Formular zeigt „– keine Angabe –", weil es nur das
Katalog-`<select>` hat. Und der Freitext ist nicht wegzubekommen:
`loaded.subjectId === form.subjectId === ""` ⇒ kein Diff ⇒ weder `clearSubject` noch `subjectName` gehen
mit (`seriesPatch.ts`). Der Nutzer liest „Gespeichert." und „Englisch" steht weiter da.

**Zustand B — Schulart-Kombination.** `SchoolTypes` ist ein `[Flags]`-Enum, eine Kombination
(„Realschule, Gymnasium") reist als freier String (B-60). Das Formular hat dafür keine Option: der
`<select>` zeigt leer, der Nutzer greift zu „– für alle –", und `"None"` **löscht** die Kombination. Vor
B-123 gab es diesen Weg für Reihen nicht.

Beides ist dieselbe Aussage: *das Formular kennt einen Zustand nicht, den das Modell erlaubt* — und in
beiden Fällen ist die Folge nicht „man kann etwas nicht", sondern „man zerstört etwas, ohne es zu merken".

## Die echte Lücke

Nicht das fehlende Bedienelement, sondern die **stille Zerstörung**: In beiden Fällen meldet die App
Erfolg. Das ist die Fehlerklasse, gegen die die PATCH-Regel der Root-`CLAUDE.md` geschrieben ist („ohne
den Schalter meldet eine Oberfläche mit ‚– keine Angabe –' fröhlich ‚Gespeichert.'") — hier eine Ebene
höher, weil nicht der Schalter fehlt, sondern die Anzeige des Zustands, für den er gedacht ist.

## Offene Punkte — hier hält der Nachtlauf an

Beide Punkte sind **Produktentscheidungen**: nicht „wie stellt man es richtig her", sondern „wie soll die
Oberfläche aussehen". Ein Agent entscheidet das nicht (Freigabe 1 sinngemäß, ausdrücklich in B-137
vermerkt).

1. **Wie drückt das Formular den Freitext aus?** Empfehlung: eine `.sub`-Zeile unter dem Fach-Select
   („Freitext-Fach ‚Englisch'") plus ein Knopf „entfernen", der `clearSubject` schickt — den Schalter hat
   der Server seit B-123, und er räumt Id und Namen gemeinsam. Alternative (billiger, unschärfer): eine
   dritte Select-Option. Kosten der Empfehlung: ein Bedienelement mehr in einem Formular mit sieben
   Feldern.
2. **Wie überlebt die Schulart-Kombination?** Empfehlung: dieselbe Runde wie Punkt 1 lösen. Zwei Formen:
   eine echte Mehrfachauswahl, oder eine sichtbare Anzeige „Kombination — hier nicht änderbar", die den
   Wert schützt statt ihn bedienbar zu machen. Kosten: die Mehrfachauswahl ist das größere Stück und
   ändert ein Feld, das an mehreren Stellen gleich aussehen sollte.

## Akzeptanzkriterien

> Entwurf — Kriterium 1 und 3 hängen an den offenen Punkten und werden erst beim Grillen final.

1. Trägt eine Reihe einen Fachnamen ohne Fach-Id, sagen Zeile und Formular dasselbe.
2. Der Freitext lässt sich über die Oberfläche entfernen, ohne Umweg über ein zugewiesenes Fach.
3. Eine Reihe mit einer Schulart-**Kombination** verliert sie nicht, wenn jemand das Formular öffnet und
   ohne Absicht auf die Schulart speichert.
4. Ein Vitest über `seriesPatch` und ein E2E-Fall, der Zustand A herstellt und auflöst.

## Verlauf

- **2026-08-10** — abgespalten von [B-137](B-137-freitext-fach-unerreichbar.md) im Nachtlauf (Sprint 2),
  weil B-137 faktisch XL war. Hier liegen die Punkte, die **nur im Dialog** fallen können; der Lauf hält
  an ihnen an und entscheidet sie nicht.
