---
tags: [typ/story, status/ausformuliert, bereich/beides, bereich/katalog, rolle/creator]
aliases: [Fach löschen ohne Vorwarnung, SetNull auf Reihen, verwaister Fachname]
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

# B-144 · Ein Fach lässt sich löschen, während Reihen darauf zeigen

Abgespalten von [B-137](B-137-freitext-fach-unerreichbar.md) (dessen Punkt 2). Das ist die **Ursache**
des Zustands, den [B-143](B-143-formular-kennt-zustaende-des-modells-nicht.md) nicht anzeigen kann.

## User Story

Als **Creator** möchte ich beim Löschen eines Fachs erfahren, dass Lehrwerk-Reihen daran hängen — sonst
zerreiße ich eine Zuordnung, von der ich nichts weiß, und der Fachname bleibt als verwaister Freitext
stehen.

## Ist-Stand am Code

- `Data/PuglingDbContext.cs:241-242` — `Subject → TextbookSeries.SubjectId` ist `SetNull`.
- `Controllers/Creator/SubjectsController.cs:79-86` — `Delete` prüft **nichts**: ein Fach lässt sich
  löschen, während Reihen darauf zeigen.
- `frontend/src/vater/CatalogAdmin.tsx:89` bietet genau das an, ohne Zahl und ohne Warnung.
- Danach steht die Reihe auf `subjectId = null`, `subjectName = "Englisch"` — der Name ist eine
  **gespeicherte** Spalte, kein Join (`TextbookSeriesController.Project`).

Zum Vergleich: `TextbookSeriesController.Delete` (`:196-200`) trägt eine Nutzungssperre (`409`, solange
eine Übung daran in einem Plan hängt). Beim Fach gibt es keine.

Verwandt, aber **nicht dasselbe**: [B-127](B-127-verlag-loeschen-trifft-fremde.md) fragt, ob das Löschen
eines *Verlags* fremde Konten treffen darf. Dort ist die Frage die Reichweite über Eigentumsgrenzen
hinweg; hier ist sie, ob der Löschende überhaupt erfährt, was er anfasst.

## Die echte Lücke

`SetNull` ist als Löschverhalten **vertretbar und gewollt** — ein Fach zu löschen soll nicht daran
scheitern, dass irgendwo eine Reihe darauf zeigt. Die Lücke ist, dass der Vorgang **lautlos** ist: keine
Zahl, keine Warnung, kein Weg zurück. Der verwaiste Name entsteht als Nebenwirkung einer Handlung, deren
Folgen niemand angezeigt bekommt.

## Offene Punkte — hier hält der Nachtlauf an

1. **Warnen oder verweigern?** Empfehlung: **warnen, nicht verweigern.** `SetNull` ist gewollt, und ein
   blockiertes Löschen wäre schlimmer als ein verwaister Name — es gäbe keinen Weg mehr, ein Fach
   loszuwerden, ohne vorher jede Reihe umzuhängen. Das ist eine Produktentscheidung, kein Herstellen:
   der Code lässt beides zu, und die Nachbar-Ressource (Reihe) hat sich für das Gegenteil entschieden.
2. **Woher kommt die Zahl?** Empfehlung: der `DELETE`-Endpunkt meldet die Zahl betroffener Reihen zurück,
   damit `confirmAction` sie nennen kann. Kosten: eine zusätzliche Abfrage im Delete-Pfad — oder eine
   eigene Vorschau-Route, wenn die Bestätigung *vor* dem Aufruf stehen soll (was sie sollte).
3. **Gilt dasselbe für `CreatorProfile` und `Textbook`?** Nicht erhoben. Beide tragen dasselbe Paar
   (nachgezählt in [B-142](B-142-fachname-driftet-gegen-fach-id.md)); ob ihre Fremdschlüssel ebenfalls
   `SetNull` sind, ist vor dem Bau zu prüfen.

## Akzeptanzkriterien

> Entwurf — hängen an Punkt 1 und 2, final erst beim Grillen.

1. Wer ein Fach löscht, auf das Reihen zeigen, erfährt es vorher und mit Zahl.
2. Das Löschen eines Fachs ohne Reihen bleibt so einfach wie heute.
3. Ein Integrationstest über die gemeldete Zahl.

## Verlauf

- **2026-08-10** — abgespalten von [B-137](B-137-freitext-fach-unerreichbar.md) im Nachtlauf (Sprint 2),
  weil B-137 faktisch XL war. Als eigene Story und nicht als Teil von
  [B-143](B-143-formular-kennt-zustaende-des-modells-nicht.md), obwohl sie die Ursache desselben Zustands
  ist: B-143 ist reine Oberfläche, diese hier braucht einen Endpunkt. Und B-143 bleibt nötig, selbst wenn
  diese Story nie gebaut wird — verwaiste Namen gibt es im Bestand bereits.
