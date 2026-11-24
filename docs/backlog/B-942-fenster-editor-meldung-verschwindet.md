---
tags: [typ/story, status/idee, bereich/klassenarbeit]
aliases: []
status: idee
prio: P3
art: Defekt
groesse: ""
wo: "frontend"
migration: ""
vertragsbruch: ""
quelle: "Review von B-757 im Nachtlauf I 2026-11-23 (docs/pm-sitzung-2026-11-23-i.md, frontend-reviewer 🟡)"
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-942 · Die Erfolgsmeldung des Fenster-Editors verschwindet sofort nach dem Speichern

`FensterEditor` hängt in `VaterClassTests.tsx` an `key={`${id}-${prepWindowDays}`}`; nach dem Speichern lädt die Detailkarte
neu, der Schlüssel wechselt, der Editor wird neu montiert und `useAction` verliert die Meldung („Vorbereitung: 3 Tage.“).
Fehler bleiben stehen. Der Unit-Test übergibt `onSaved={() => {}}` und sieht es darum nicht. Vorschlag: Schlüssel nur `id`,
oder die Meldung bewusst stumm lassen („Erfolg darf stumm bleiben“) – die Spalte zeigt die Änderung ohnehin.

## Verlauf

- **2026-11-24** — angelegt im Nachtlauf I (Sprint 134) aus dem Review, nicht behoben: der Fehlerzähler des Sprints stand
  bei 5, ein weiterer Fund hätte ihn überschritten (Freigabe 3/11) – der Lauf endet hier. Protokoll
  [pm-sitzung-2026-11-23-i.md](../pm-sitzung-2026-11-23-i.md).
