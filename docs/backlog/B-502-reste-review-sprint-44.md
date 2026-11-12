---
tags: [typ/story, status/idee, bereich/frontend, rolle/supervisor]
aliases: [Lernstand nach Kinderwechsel, Vokabel aus Übung entfernen kostet Lernstand]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: "frontend"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-nachtlauf.md (Review Sprint 44)
unverifiziert: false
entgangen_bei: []
grund: ""
ersetzt_durch: []
---

# B-502 · Reste aus dem Review von Sprint 44: die Wortliste nach einem Kinderwechsel und das Entfernen einer Vokabel

Gefunden im `frontend-reviewer` zu B-499 (🟢, außerhalb des Diffs bzw. älter, ohne Zählung zurückgestellt).

1. `frontend/src/vater/VaterLernstand.tsx:104-113` liest `words.stale` nicht: nach einem Kinderwechsel steht die (leere)
   Liste des vorigen Kindes, bis die neue kommt – mit „nur schwache" aus harmlos, ohne kurz „Noch kein Lernstand" für das
   neue Kind aus dem Stand des alten. Fix: Leerzeile nur bei `!words.stale`, oder `WeakWords` mit `key={childId}`.
2. `frontend/src/vater/ExerciseEditModal.tsx:570`: eine Vokabel aus einer *unbenutzten* Übung zu entfernen nimmt den
   Lernstand der Kinder daran mit (`ExerciseItem` → `ItemProgress` Cascade); die Frage sagt nur „Vokabel bleibt im Store".
   Dazu die Produktfrage, ob `ItemProgress.ItemId` wie die Historie `SetNull` werden soll – eine Entscheidung, kein Fix.

## Verlauf

- **2026-11-12** — angelegt (Review Sprint 44).
