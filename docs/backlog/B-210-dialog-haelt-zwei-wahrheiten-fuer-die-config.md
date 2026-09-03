---
tags: [typ/story, status/ausformuliert, bereich/frontend, rolle/creator]
aliases: [Inhalt wird lautlos zurueckgedreht, config vom Oeffnen]
status: ausformuliert
prio: P1
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: Nachschau 2026-09-03 zu B-206 (Nachtlauf)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-210 · „Beschreibung speichern" dreht den gerade gespeicherten Inhalt lautlos zurück

**P1, weil es Arbeit des Creators vernichtet und er es nicht merkt** — kein Fehler, keine Meldung, und
der Bildschirm zeigt weiter die Fassung, die es nicht mehr gibt.

## User Story

Als **Creator** möchte ich, dass ein gespeicherter Inhalt gespeichert bleibt, auch wenn ich danach im
selben Dialog noch den Titel ändere.

## Ist-Stand am Code

`ExerciseEditModal.tsx:203` schickt beim Speichern der Metadaten `config: detail.config` mit — den Stand
**vom Öffnen des Dialogs**. `detail` wird nie neu geladen: `onSaved` ist `existing.reload`
(`VaterExercises.tsx:187`) und frischt nur die **Liste** auf.

Der Weg: Übung öffnen → Inhalt ändern → „Inhalt speichern" (der Server hat jetzt den neuen Inhalt) →
Titel ändern → „Beschreibung speichern" (`:205`). Der PUT ist ein **Vollersatz** und trägt die alte
`config` — der gerade gespeicherte Inhalt ist weg. Der `ContentEditor` zeigt weiter die geänderten
Zeilen, es gibt also nicht einmal einen sichtbaren Widerspruch.

**Älter als B-206, aber von dessen Review-Commit ausdrücklich für behoben erklärt.** Die Review-Runde zu
B-206 hat die *Gegenrichtung* geschlossen (der `ContentEditor` las `initialForm(detail)`) und im Kommentar
`:53` „Ein State, zwei Leser" notiert. Für die **Config** sind es weiter zwei Zustände. `e2e/einkleidung.spec.ts:118→126`
fährt genau die Reihenfolge, die den Fall **nicht** auslöst.

**Nebenbefund aus demselben Umbau:** `ContentEditor.save` (`:321-327`) prüft den Titel **nicht**,
`MetaEditor.submit` (`:197`) schon. Seit dem Hochziehen des `MetaForm` kann „Inhalt speichern" also einen
leergeräumten Titel abschicken und fängt sich einen Server-`400`.

## Offene Punkte

1. Nach jedem Teil-Speichern `detail` neu laden, oder die `config` aus dem lebenden Editor-Zustand
   nehmen? *Empfehlung: neu laden* — der Editor-Zustand wäre eine dritte Quelle derselben Wahrheit.
2. Den Nebenbefund (Titelprüfung) hier mitnehmen oder trennen? *Empfehlung: mitnehmen* — dieselbe Datei,
   dieselbe Ursache (hochgezogenes Formular), und getrennt bliebe er liegen.

## Verlauf

- **2026-09-03** — aus der **Nachschau** zu B-206 (Nachtlauf). `entgangen_bei` bleibt **leer**: der Defekt
  ist älter als B-206s Diff, und die Nachschau hat das ausdrücklich geprüft. Was B-206 zuzurechnen wäre,
  ist die *Behauptung* der Review-Runde, das Problem sei damit erledigt.
