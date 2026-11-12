---
tags: [typ/story, status/idee, bereich/frontend, rolle/supervisor, rolle/student]
aliases: [Leerfall an items.length, leere Seite jenseits des Bestands]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: "frontend"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-nachtlauf.md (Review Sprint 46, B-504)
unverifiziert: false
entgangen_bei: []
grund: ""
ersetzt_durch: []
---

# B-505 · Paged-Listen sagen „noch keine …" für eine leere Seite, nicht für einen leeren Bestand

Gefunden im `frontend-reviewer` zu B-504 (🟢, außerhalb des Diffs, bewusst nicht in Sprint 46 – das Sprint-Ziel hielt
ohne). B-504 hat die Regel im Lernstand an die Senke gelegt (`total === 0`); acht weitere Listen hängen ihren Leerfall noch
an die Länge der Seite (`items.length === 0`):

- `frontend/src/vater/VaterClassTests.tsx:139` „Noch keine Klassenarbeiten."
- `frontend/src/vater/VaterKonto.tsx:79` „Noch keine Buchungen."
- `frontend/src/vater/VaterShop.tsx:476` „Noch keine Käufe."
- `frontend/src/vater/VaterAnmerkungen.tsx:155`
- `frontend/src/vater/ClozeTexts.tsx:111`
- `frontend/src/vater/VaterVocab.tsx:324` „Keine Vokabeln in dieser Sprach-Kombination."
- `frontend/src/vater/VaterZiele.tsx:229` „Noch keine großen Ziele."
- `frontend/src/sohn/SohnKonto.tsx:41` (`account.noEntries`)

Erreichbar, wenn die letzte Zeile der letzten Seite gelöscht wird oder ein Filter den Bestand unter `skip` drückt. Die
Sätze sagen „nichts da", keinen Erfolg – darum P3. Frage fürs Ausformulieren: eine gemeinsame Senke (Leerzeile im
`Pager`-Umfeld bzw. ein Helfer, der `total` liest) statt acht Einzelstellen; und ob die leere Seite auf die letzte
gültige zurückspringen soll.

## Verlauf

- **2026-11-12** — angelegt (Review Sprint 46 zu B-504).
