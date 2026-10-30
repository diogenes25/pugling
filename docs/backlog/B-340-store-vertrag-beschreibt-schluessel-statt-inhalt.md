---
tags: [typ/story, status/idee, bereich/backend, rolle/creator]
aliases: [Vertragstext zu reuseExisting/BatchItemStatus.Existing nennt den Schlüssel]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: Nachschau zu Lauf K, Teil 2 in Lauf L (docs/pm-sitzung-2026-10-30-nacht.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-337]
wartet_auf: ""
---

# B-340 · Der Vertrag beschreibt die Wiederverwendung im Store noch über den Schlüssel, nicht über den Inhalt

[B-337](B-337-reuse-folgt-veraltetem-schluessel.md) hat `reuseExisting` auf den **aktuellen Inhalt** umgestellt (ein
`PATCH` zieht den gespeicherten Schlüssel nicht nach). Die Doku im Vertrag sagt weiter das Gegenteil: „if the key the
server would generate already names an entry, that entry comes back" (`backend/Pugling.Contracts/Creator/VocabularyStoreDtos.cs:27-29`),
ebenso `BatchItemStatus.Existing` (`:73`) und `VocabularyStoreController.cs:206`, `:487-488`. Für einen veralteten
Schlüssel ist das Verhalten heute genau umgekehrt. Weder `448785c` noch `3190f9e` haben den Text angefasst.

Vorschlag: „an entry whose current word and translation fold to the same key", OpenAPI-Datei neu erzeugen.

## Verlauf

- **2026-10-30** — angelegt aus der Nachschau zu Lauf K, Teil 2 (`pugling-reviewer`, 🟡). Aus Code gelesen.
