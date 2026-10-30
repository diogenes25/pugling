---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/a11y, rolle/student]
aliases: [„Weiter versuchen" sagt die alte Frage als neue an]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf K, Teil 2 in Lauf L (docs/pm-sitzung-2026-10-30-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-286]
wartet_auf: ""
nachgeschaut: "2026-10-30"
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

## User Story

Als **Sohn**, der mit Screenreader eine Klausur schreibt, möchte ich nach „Weiter versuchen" nur die Frage hören, die
jetzt dran ist – nicht noch einmal die, die ich schon beantwortet habe.

## Ist-Stand (Lauf L, am Code belegt)

Siehe oben; `resume()` (`SohnTest.tsx`) setzte nur `setError(null)`. **Gemessen:** die Zusicherung „Region leer direkt
nach dem Klick" fällt (rote Probe unten).

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **`setItem(null)` in `resume()` vor dem Nachladen.** Dann zeigt die Seite „wird vorbereitet", die Region bleibt leer
   bis zur neuen Frage, und die alte Karte ist nicht mehr anklickbar. *Kosten:* scheiterte nur `answerTest` (die Frage ist
   noch offen), wird dieselbe Frage nach dem Nachladen erneut angesagt – richtig, sie ist wieder dran.

## Akzeptanzkriterien

1. Nach „Weiter versuchen" ist die Region leer, bis die nächste Frage da ist, und die alte Antwort-Kachel ist weg.
2. Danach steht „Neue Aufgabe: <neue Frage>" in **derselben** Region (bestehende Zusicherung).

## Schätzung

`XS`, Frontend. Testweg: `SohnTest.test.tsx`.

## Verlauf

- **2026-10-30** — angelegt aus der Nachschau zu Lauf K, Teil 2 (`frontend-reviewer` über `448785c`, 🟡). Aus Code
  gelesen, nicht gemessen. Liegt im Increment von Lauf K, nicht in dem von Lauf L.
- **2026-10-30** — Lauf L, Teil 3: ausformuliert, autonom gegrillt (Freigabe 1), geschätzt, gebaut. Rote Probe: der
  erweiterte Fall „sagt die fortgesetzte Frage in derselben Region an" **1 failed** (erwartet 1; Region trug die alte
  Frage). Danach `src/sohn/` **135/135**. Die Mutante „ohne `setItem(null)`" ist der Ausgangsstand der Probe.
- **2026-10-30** — Review `frontend-reviewer`: alle Übergänge geprüft (Antwort gescheitert, nächste Frage gescheitert, `finish` scheitert, zweites Scheitern, Doppelklick-Sperre, Fokus); 🟢 der Kommentar nannte einen Fall, den der Test nicht fuhr → Kommentar berichtigt und den häufigeren Fall (Antwort angenommen, nur `nextTest` scheitert) als eigenen Fall ergänzt; Mutante „ohne `setItem(null)`" → **2 failed** (beide Fälle; ohne Zählung).
- **2026-10-30** — Abnahme Lauf L, Teil 3: `tsc -b` sauber, Vitest **585/585** (danach + 1 Fall in `SohnTest.test.tsx`, **14/14**), volle Backend-Suite siehe Protokoll. E2E-Gesamtlauf **102 passed** (`exit=0`, nach dem letzten Produktcode-Edit gestartet; danach nur Tests). Rollengang als E2E (Browser-Extension nicht genutzt): Vater über `assistent-kinderliste-kommt-spaet`, `assistent` und `uebungen-bewerten` (Kollege mit Schreibrecht), Sohn über die Klausur-Komponententests (Fehler → „Weiter versuchen" → nächste Frage; eine E2E-Spec mit gescheitertem `nextTest` bräuchte einen kaputten Server, das gehört auf Komponentenebene), Creator Regressionszeuge. Review `frontend-reviewer`: kein 🔴/🟡. **Abgenommen**, Commit siehe Protokoll ([Lauf L](../pm-sitzung-2026-10-30-nacht.md)).
- **2026-10-30** — Nachschau in Lauf L, Teil 4 (frischer `frontend-reviewer` über `fce0076`): **sauber** (nur 🟢). Geprüft: jeder Zustand, wenn `item` null wird (Region, Zähler „Frage x von y“, `cursor`, `typedAnswer`/`revealed`, `action.busy`, `planStopped`, Feier/Serie nur in `finish`); Doppelklick; jeder Weg ins Wartebild; `SohnPractice` hat kein gleiches Fortsetzen-Muster. 🟢 im Wartebild fehlt ein Ausweg → [B-350](B-350-wartebild-der-klausur-ohne-ausweg.md); 🟢 der erste Fall lieferte nach gescheiterter Antwort eine andere Frage → realistisch umgebaut (dieselbe Frage kommt wieder und wird erneut angesagt); 🟢 „Rot, wenn“ im zweiten Fall ergänzt (beides ohne Zählung, Mutante weiter **2 failed**).
