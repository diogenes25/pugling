---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/creator]
aliases: [UnitForm schliesst mit dem Banner, dritter Fall von B-151]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
quelle: frontend-reviewer zum Sprint 1 des Nachtlaufs 2026-09-01
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
nachgeschaut: ""
---

# B-187 · Auch das Unit-Formular schließt sich mit seiner eigenen Bestätigung

Der dritte Fall derselben Wurzel wie [B-151](B-151-gespeichert-banner-verschwindet-mit-dem-formular.md) —
gefunden vom `frontend-reviewer` beim Review von B-151/B-173, **außerhalb** von deren Diff.

## User Story

Als **Creator** möchte ich nach dem Speichern einer Unit eine Bestätigung sehen, die lange genug steht, um
sie zu lesen, damit ich nicht raten muss, ob die Stoffnotiz angekommen ist.

## Ist-Stand am Code

- `VaterLehrwerke.tsx:634` setzt `"Gespeichert."` beim Ändern einer bestehenden Unit.
- Danach `onDone()` → `UnitPanel` setzt `setEditing(null)`, das Formular hängt aus — und mit ihm der
  `StatusBanner` in `VaterLehrwerke.tsx:732`.
- Genau die Kette, die B-151 für `TextbookForm` und `ProfileForm` behoben hat.

**Warum nicht in B-151 mitgenommen:** Die Zeilen liegen außerhalb des Diffs jener Story
(`UnitPanel` wurde im selben Lauf nur für B-184 an einer Platzhalter-Zeile angefasst). Eine geschätzte
Story während des Bauens wachsen zu lassen ist genau das, was B-151 bei ihrer eigenen Entstehung aus
B-148 vermieden hat.

**`entgangen_bei` bleibt leer und das ist keine Nachlässigkeit:** Der Zustand ist älter als jede Story
dieses Sprints und lag außerhalb ihres Diffs — er ist keiner Abnahme entgangen, sondern nie geprüft
worden.

## Die echte Lücke

Dieselbe wie in B-151: Die Bestätigung lebt in dem Element, dessen Verschwinden sie bestätigt.

## Offene Punkte

1. ~~Meldungsplatz an `UnitPanel` oder an `VaterLehrwerke`?~~ → Entscheidung 1.
2. ~~Trägt das Anlege-Formular dasselbe Problem?~~ → Entscheidung 2 (nachgesehen, nicht vermutet).

## Entscheidungen

1. **An `VaterLehrwerke`, nicht an `UnitPanel`** — gegen die Empfehlung dieser Story, und der Grund kam von
   ihrer Nachbarin: [B-212](B-212-fuenfter-meldungsplatz-in-lehrwerke.md), im selben Sprint gebaut, schafft
   die zweiten Meldungsplätze dieser Datei gerade ab. Ein neuer Platz an `UnitPanel` wäre der **vierte**
   gewesen und hätte denselben Widerspruch erzeugt, den B-212 auflöst. Der Einwand der Empfehlung („am
   Seitenrand weit weg") ist berechtigt und wird dort beantwortet, wo er hingehört: mit
   `scrollIntoView({ block: "nearest" })` im Fehlerfall (B-212, Entscheidung 3). **Kosten:** keine eigenen —
   `UnitForm` bekam `onMeldung` bereits, es fehlte nur die Nutzung.
2. **Das Anlege-Formular hatte das Problem nicht, bekommt aber denselben Weg.** Nachgesehen: Es hängt an
   `{series.isOwn && !units.showPlaceholder && …}` und bleibt montiert, seine Bestätigung ginge also nicht
   verloren. Trotzdem meldet es jetzt ebenfalls nach oben — zwei Plätze für dieselbe Sorte Meldung sind der
   Defekt aus B-212, und eine Ausnahme „nur beim Anlegen" wäre genau die Regel, die niemand im Kopf behält.
   **Kosten:** eine Meldung, die vorher direkt am Formular stand, steht jetzt am Platz der Seite.

## Rote Proben (erwartet / gemessen)

| Probe | erwartet | gemessen |
| --- | --- | --- |
| `UnitForm.submit` meldet wieder lokal (Empfänger weg) | 1 | **1**, an Zeile 68 — der Zeile, die den Fall trägt |

## Verlauf

- 2026-09-01 · Aufgenommen aus dem `frontend-reviewer`-Review zu Sprint 1 des Nachtlaufs. Die drei
  Fundorte sind vom Reviewer benannt und von mir gegengelesen; `unverifiziert` bleibt darum aus. Bewusst
  **nicht** in B-151 hineingezogen: außerhalb ihres Diffs.
- **2026-09-05** — **ausformuliert → abgenommen** im Sprint „Eine Rückmeldung gehört an einen Platz, der sie überlebt — und es darf nur einer
  sein" (B-187/B-200/B-212 gemeinsam). Autonom gegrillt unter Freigabe 1 (`art: Defekt`).
  **Rollengang:** `e2e/ein-meldungsplatz.spec.ts`, Fall 1 — er wartet ausdrücklich, bis das Formular weg
  ist, und prüft die Bestätigung erst danach; vorher wäre er grün geworden, ohne etwas zu belegen. Kein
  Browser-Handgang möglich (keine Chrome-Anbindung), damit greift der dokumentierte Ersatz.
  Belege: `tsc -b` sauber, `vitest run` **368/368** grün, `playwright test` **63/63** grün.
  `frontend-reviewer` gelaufen, **kein Blocker**; seine acht Funde mit Code- oder Testwirkung sind
  eingearbeitet (Protokoll `docs/pm-sitzung-2026-09-05.md`). Commit `HASH`.
