---
tags: [typ/story, status/idee, bereich/frontend, bereich/a11y, rolle/supervisor]
aliases: [Assistent-Pillen ohne aria-pressed]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: Bau von B-262 in Lauf L (docs/pm-sitzung-2026-10-30-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-348 · Die Auswahl-Pillen im Assistenten sagen nicht, welche gewählt ist

`ChoicePill` (`frontend/src/vater/VaterWizard.tsx`, „Bestehendes Kind" / „Neues Kind anlegen" u. a.) zeigt die Wahl nur
über die Klasse `ghost` – also über Farbe. Ein Screenreader hört zwei gleich klingende Schaltflächen ohne Zustand. Die
Konvention des Vater-Webs ist `aria-pressed` (Accessibility-Pass, „aria-pressed statt role=\"tab\"").

Gemessen beim Bau von B-262: `toHaveAttribute("aria-pressed", "true")` an „Bestehendes Kind" fällt, das Attribut fehlt.
Vorschlag: `aria-pressed={active}` an `ChoicePill`, eine Zusicherung in `assistent-kinderliste-kommt-spaet.spec.ts`.

## Verlauf

- **2026-10-30** — angelegt beim Bau von B-262 (Lauf L, Teil 3); außerhalb des Sprint-Ziels, darum eigene Story.
