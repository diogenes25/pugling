---
tags: [typ/story, status/idee, bereich/backend, rolle/creator]
aliases: [FindManyAsync filtert den ganzen Local-Cache]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: Nachschau zu Lauf L, Teil 2 (docs/pm-sitzung-2026-10-30-nacht.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-349 · Im Vokabel-Batch hängt ein Treffer davon ab, was frühere Einträge desselben Batches geladen haben

`VocabularyStoreService.FindManyAsync` lädt Kandidaten (Schlüssel passt oder Wort gleicht unter NOCASE), filtert danach
aber den ganzen `db.Vocabularies.Local` – nicht nur die eben geladenen Zeilen. Im `CreateBatch`
(`VocabularyStoreController.cs`) bedient ein Kontext alle Einträge; was ein früherer Eintrag geladen hat (eigene
`FindAsync`-Kandidaten, `BaseForm` über `LoadGraphAsync`), zählt darum für spätere mit.

Repro aus dem Code (Nachschau-Prüfer): Eintrag E als `hus`/`Haus` angelegt, per `PATCH` zu `Hõuse`. Batch
[`hus`/`Haus`, `house`/`Haus`]: Eintrag 1 lädt E über den Schlüssel (kein Treffer, E faltet jetzt zu `house`), Eintrag 2
meldet `existing` → E, obwohl E für ihn weder Schlüssel- noch Wortkandidat ist (NOCASE faltet õ nicht). Allein geschickt
gibt Eintrag 2 `201`. Das Ergebnis hängt an der Reihenfolge, und zwar in der großzügigeren Richtung. Ein einzelnes
`Create` ist exakt.

Vorschlag: `Local` auf die Zeilen dieses Aufrufs (geladene Kandidaten plus ungespeichert hinzugefügte) einschränken –
oder die Wortseite ohnehin weiter falten (dann fällt der Unterschied mit [B-347](B-347-store-speichert-wort-ungetrimmt.md)
zusammen). Erst messen.

## Verlauf

- **2026-10-30** — angelegt aus der Nachschau zu Lauf L, Teil 2 (`pugling-reviewer` über `d2be0a3`, 🟢). Aus Code gelesen.
