---
tags: [typ/story, status/verworfen, bereich/beides, bereich/katalog, rolle/creator]
aliases: [Freitext-Fach an der Reihe, Fach löschen lässt den Namen stehen, zwei Aussagen auf einem Bildschirm]
status: verworfen
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: frontend-reviewer zu B-123 (2026-08-10), Fund 5
unverifiziert: false
grund: "geteilt — faktisch XL (sechs Akzeptanzkriterien, drei Controller, Backend und Frontend, dazu eine Flags-Enum-Mehrfachauswahl); Freigabe 3 des Nachtlaufs verlangt dafür Teilen statt Bauen"
ersetzt_durch: [B-142, B-143, B-144]
entgangen_bei: []
nachgeschaut: ""
wartet_auf: ""
---

# B-137 · Ein Freitext-Fach an der Reihe ist sichtbar, aber nicht wegzubekommen

Abgespalten von [B-123](B-123-lehrwerk-reihe-bearbeiten.md): dessen Ziel — Metadaten über die Oberfläche
korrigieren — ist erfüllt, aber es gibt einen Zustand, den das neue Formular nicht ausdrücken kann. Die
Lösung braucht eine Produktentscheidung, die B-123s Akzeptanzkriterien nicht abdecken.

## User Story

Als **Creator** möchte ich, dass Zeile und Bearbeiten-Formular dasselbe über das Fach einer Reihe sagen —
und dass ich einen Fachnamen, der ohne Katalog-Fach dasteht, auch loswerden kann.

## Ist-Stand am Code

Der Zustand entsteht **ohne Zutun**, über die gewöhnliche Oberfläche:

- `Data/PuglingDbContext.cs:241-242` — `Subject → TextbookSeries.SubjectId` ist `SetNull`.
- `Controllers/Creator/SubjectsController.cs:79-86` — `Delete` prüft **nichts**: ein Fach lässt sich
  löschen, während Reihen darauf zeigen. `frontend/src/vater/CatalogAdmin.tsx:89` bietet genau das an.
- Danach steht die Reihe auf `subjectId = null`, `subjectName = "Englisch"` — der Name ist eine
  **gespeicherte** Spalte, kein Join (`TextbookSeriesController.Project`).

Was der Nutzer dann sieht, auf einem Bildschirm:

- Die Zeile zeigt „Englisch" (Rückfall auf `subjectName`, `VaterLehrwerke.tsx:135-137`).
- Das Bearbeiten-Formular zeigt „– keine Angabe –", weil es nur das Katalog-`<select>` hat.

Und der Freitext ist nicht wegzubekommen: `loaded.subjectId === form.subjectId === ""` ⇒ kein Diff ⇒
weder `clearSubject` noch `subjectName` gehen mit (`seriesPatch.ts`). Der Nutzer liest „Gespeichert." und
„Englisch" steht weiter da — genau die Fehlerklasse, gegen die die PATCH-Regel der Root-`CLAUDE.md`
geschrieben ist, eine Ebene über dem, was B-123 geschlossen hat.

Verloren ist er nicht: Fach zuweisen → speichern → „keine Angabe" → speichern räumt ihn ab. Aber niemand
errät das.

## Die echte Lücke

Zwei Ursachen, und die zweite ist die eigentliche:

1. Das Formular kennt einen Zustand nicht, den das Modell erlaubt (Fachname ohne Fach-Id).
2. **Ein Fach lässt sich löschen, während Reihen darauf zeigen.** `SetNull` ist als Löschverhalten
   vertretbar, aber ohne Vorprüfung und ohne Hinweis entsteht der verwaiste Name lautlos.

## Offene Punkte

1. **Wie drückt das Formular den Freitext aus?** Empfehlung: eine `.sub`-Zeile unter dem Fach-Select
   („Freitext-Fach „Englisch"") plus ein Knopf „entfernen", der `clearSubject` schickt — der Server hat
   den Schalter seit B-123, und er räumt Id und Namen gemeinsam. Alternative (billiger, unschärfer):
   eine dritte Select-Option. Kosten der Empfehlung: ein Bedienelement mehr in einem Formular mit sieben
   Feldern.
2. **Soll das Löschen eines Fachs vorher warnen oder verweigern?** Empfehlung: warnen, nicht verweigern —
   `SetNull` ist gewollt, und ein blockiertes Löschen wäre schlimmer als ein verwaister Name. Der
   Endpunkt könnte die Zahl betroffener Reihen zurückmelden, damit `confirmAction` sie nennen kann.
   Kosten: eine zusätzliche Abfrage im Delete-Pfad.
3. **Gilt dasselbe für `CreatorProfile` und `Textbook`?** Nicht erhoben. Beide tragen dasselbe Paar und
   haben ihren `ClearSubject`-Schalter schon länger — ob ihre Oberflächen den Freitext ausdrücken können,
   ist vor dem Bau zu prüfen.
4. **Das Paar driftet auch beim Wechsel, nicht nur beim Löschen.** Vom `pugling-reviewer` beim
   B-123-Review nachgetragen: `TextbookSeriesController.Update` setzt bei `subjectId` nur die Id und
   lässt `SubjectName` stehen. Ein `PATCH {"subjectId": 2}` hinterlässt eine Reihe, die per Id
   Französisch und per Name „Englisch" behauptet — dieselbe Fehlerklasse wie oben, nur ohne Schalter.
   Kompensiert wird das **allein im Frontend** (`seriesPatch.ts` schickt Id und Namen als Paar);
   `Pugling.Client.UpdateSeriesAsync`, der Creator-Agent und `docs/REST` haben kein Gegenstück, und die
   beiden Präzedenz-Controller machen es genauso. Empfehlung: der Server leitet den Namen nach, wenn eine
   Id ohne Namen kommt — `ValidateReferencesAsync` fragt `db.Subjects` ohnehin ab, es kostet ein
   `Select(s => s.Name)` statt `AnyAsync`. Kosten: es sind **drei** Controller, und es ändert bestehendes
   Verhalten — darum nicht still in B-123 mitgenommen, sondern hier. In B-123 steht seither ein Satz im
   Vertrag, der die Bringschuld des Aufrufers benennt.

5. **Zweite Fundstelle derselben Klasse: die Schulart-Kombination.** Vom `frontend-reviewer` beim
   B-123-Review nachgetragen. `SchoolTypes` ist ein `[Flags]`-Enum, und eine Kombination
   („Realschule, Gymnasium") reist als freier String (B-60). Das Bearbeiten-Formular hat dafür keine
   Option: der `<select>` zeigt leer, der Nutzer greift zu „– für alle –", und `"None"` **löscht** die
   Kombination. Vor B-123 gab es diesen Weg für Reihen nicht. Empfehlung: zusammen mit Punkt 1 lösen —
   es ist dieselbe Frage („das Formular kennt einen Zustand nicht, den das Modell erlaubt"), nur ein
   Feld weiter. Kosten: die Schulart braucht dann eine Mehrfachauswahl oder eine sichtbare
   „Kombination, hier nicht änderbar"-Anzeige.

## Akzeptanzkriterien

1. Trägt eine Reihe einen Fachnamen ohne Fach-Id, sagen Zeile und Formular dasselbe.
2. Der Freitext lässt sich über die Oberfläche entfernen, ohne Umweg über ein zugewiesenes Fach.
3. Wer ein Fach löscht, auf das Reihen zeigen, erfährt vorher davon.
4. Ein Vitest über `seriesPatch` und ein E2E-Fall, der den Zustand herstellt und auflöst.
5. Ein `PATCH`, der nur `subjectId` ändert, hinterlässt keinen widersprüchlichen Fachnamen — oder der
   Vertrag sagt ausdrücklich, dass der Aufrufer beide Felder schickt (heute nur Letzteres).
6. Eine Reihe mit einer Schulart-**Kombination** verliert sie nicht, wenn jemand das Formular öffnet und
   ohne Absicht auf die Schulart speichert.

## Verlauf

- **2026-08-10** — angelegt aus dem `frontend-reviewer`-Befund zu B-123 (Fund 5), Ist-Stand vom Reviewer
  Zeile für Zeile belegt. **Bewusst nicht in B-123 mitgenommen:** dessen Akzeptanzkriterien deckten den
  Zustand nicht ab, und Punkt 1 wie Punkt 2 sind Produktentscheidungen — in B-123 wären sie unbemerkte
  Nebenentscheidungen geworden.
- **2026-08-10** — Punkt 4 und Akzeptanzkriterium 5 ergänzt: der `pugling-reviewer` hat beim
  B-123-Review gezeigt, dass das Paar **auch beim Wechsel** driftet, nicht nur beim Löschen
  eines Fachs. Damit besitzt diese Story beide Richtungen. In B-123 blieb es bei einem Satz im Vertrag,
  weil die Nachleitung drei Controller betrifft und bestehendes Verhalten ändert. Dazu Punkt 5 und
  Akzeptanzkriterium 6 vom `frontend-reviewer`: die Schulart-Kombination ist dieselbe Fehlerklasse ein
  Feld weiter, und das neue Formular kann sie zerstören.
- **2026-08-10** — **geteilt** im Nachtlauf (Sprint 2) statt gebaut. Der Umfang war faktisch XL, und die
  fünf offenen Punkte zerfallen sauber entlang der Frage, *wer* sie beantworten kann:
  [B-142](B-142-fachname-driftet-gegen-fach-id.md) (Punkt 4) bestimmt der Code — eine Zeile darf sich
  nicht selbst widersprechen; [B-143](B-143-formular-kennt-zustaende-des-modells-nicht.md) (Punkte 1 und
  5) und [B-144](B-144-fach-loeschen-trifft-reihen-lautlos.md) (Punkt 2) sind Produktentscheidungen und
  fallen nur im Dialog. Punkt 3 (gilt es auch für `CreatorProfile`/`Textbook`?) reist mit B-144, wo er
  hingehört. Die Ist-Stände wurden beim Teilen an den Controllern **nachgezählt**, nicht abgeschrieben.
