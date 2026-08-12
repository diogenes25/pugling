---
tags: [typ/story, status/ausformuliert, bereich/frontend, bereich/lehrplan, rolle/supervisor]
aliases: [Gescheiterte Katalogsuche liest sich als leerer Katalog]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Nachschau im Nachtlauf 2026-08-12 (Nebenfund zu B-18)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-162 · Scheitert die Übungssuche im Assistenten, behauptet er einen leeren Katalog

Dieselbe Klasse wie [B-111](B-111-verlauf-luegt-im-fehlerfall.md), eine Fläche weiter: ein Fehler beim Laden
wird als „nichts da" ausgegeben — und hier mit einer ausdrücklichen, falschen Handlungsanweisung.

## User Story

Als *Vater* möchte ich bei einer gescheiterten Suche erfahren, dass sie gescheitert ist, damit ich nicht
Übungen anlege, die längst existieren.

## Ist-Stand am Code

- `frontend/src/vater/VaterWizard.tsx:434-437`: Der Zweig `filteredExercises.length === 0` zeigt „**Keine
  passenden Übungen im Katalog. Lege welche unter ‚Übungen' an**".
- `exercises.error` wird in der ganzen Datei **nirgends** gerendert (nachgezählt über `VaterWizard.tsx`) —
  anders als `setError`/`error` für die Assistenten-eigenen Fehler, die es gibt.
- `useAsync` (`frontend/src/lib/useAsync.ts:29-36`) lässt `data` bei einem Fehler auf `null` und setzt
  `loading` auf `false`. Eine gescheiterte Suche landet damit zwangsläufig im Leer-Zweig.
- Der Satz ist nicht bloß unpräzise, sondern **handlungsleitend falsch**: er schickt den Vater ins
  Anlege-Formular, während der Katalog voll ist und nur die Abfrage scheiterte.

## Die echte Lücke

`filteredExercises.length === 0` trägt zwei Bedeutungen: „die Suche lief und fand nichts" und „die Suche lief
nicht". Genau die Fehlerfamilie, die dieses Repo mehrfach bezahlt hat (B-111, B-116). Der Unterschied zu
B-111: dort log die App über einen Verlauf, hier über den **Katalog** — und gibt eine Anweisung dazu.

## Offene Punkte

1. **Nur den Fehler anzeigen, oder auch den Leer-Satz schärfen?** Empfehlung: beides. `exercises.error` in
   ein `banner err` (das Muster steht direkt daneben, `:434`), und der Leer-Satz greift erst, wenn
   `exercises.data !== null`.
2. **Gilt dasselbe an weiteren `useAsync`-Leerzweigen?** Zu messen, bevor eine Regel daraus wird — B-116 hat
   dieselbe Frage für den Ladezustand gestellt und drei Stellen gefunden. Empfehlung: erst zählen, dann
   entscheiden, ob ein Wächter trennscharf formulierbar ist.
3. **Ist es ein `Defekt` oder `Aufräumen`?** Empfehlung: `Defekt` — die Meldung ist nicht unschön, sie ist
   unwahr und führt zu einer Handlung.

## Akzeptanzkriterien (Entwurf)

1. Scheitert die Suche, sieht der Vater den Fehler und **nicht** „Keine passenden Übungen im Katalog".
2. Findet die Suche wirklich nichts, bleibt der bisherige Satz mit seiner Anweisung.
3. Ein Test deckt beide Fälle; die rote Probe belegt, dass er den heutigen Stand fängt.

## Verlauf

- **2026-08-12** — angelegt aus der **Nachschau** des Nachtlaufs (Nebenfund neben
  [B-161](B-161-alle-waehlen-macht-die-auswahl-unsichtbar.md)). **`entgangen_bei` bleibt leer, und das ist
  keine Nachlässigkeit:** der Block liegt außerhalb des Diffs von B-18 (der fasst nur die Zeile darüber und
  `TruncationHint` an) und außerhalb der drei anderen nachgeschauten Stories — er ist also **keiner**
  Abnahme entgangen, sondern älter. **Bewusst nicht mit B-161 zusammengelegt**, obwohl beide in derselben
  Datei liegen: B-161 trägt ein `entgangen_bei`, dieses hier nicht, und ein gemeinsamer Eintrag würde die
  Wirkungszahl des Bereichs verfälschen.
