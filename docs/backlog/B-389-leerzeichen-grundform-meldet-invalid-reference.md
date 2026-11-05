---
tags: [typ/story, status/idee, bereich/backend, rolle/creator]
aliases: [Leerzeichen-baseFormKey ist unbekannte Referenz]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-05.md#lauf-v
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-389 · Ein `PATCH` im Vokabelspeicher mit `baseFormKey: "  "` meldet `invalid_reference`

`VocabularyStoreController.cs:345-359` prüft `Length == 0`; ein Schlüssel aus Leerzeichen sucht also eine Zeile und findet keine.
Der Anlege-Weg (`:250`, `IsNullOrWhiteSpace`) behandelt denselben Wert als „nicht angegeben". Das Verhalten ist alt, seit B-329
heißt nur der Code anders. Zu entscheiden beim Ausformulieren: „nicht angegeben" wie beim Anlegen oder `validation_error`.

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu Lauf U, Sprint 4 (Lauf V, Retro Sprint 5; frischer `pugling-reviewer`, 🟢).
