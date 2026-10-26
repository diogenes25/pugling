---
tags: [typ/story, status/idee, bereich/frontend, bereich/vokabel, rolle/supervisor]
aliases: [Assistent legt _2-Eintrag für bekanntes Wort an]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: beides
migration: ""
vertragsbruch: ""
quelle: Nachschau zu Sprint 11 in Lauf B (docs/pm-sitzung-2026-10-26-tag.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-71]
wartet_auf: ""
---

# B-277 · Der Schnellanleger im Assistenten legt für ein bekanntes Wort einen zweiten Store-Eintrag an

[B-71](B-71-inline-vokabelliste-ohne-varianten.md) AK 5 sagt, eine im Übungseditor eingegebene Variante landet auf **demselben** Store-Eintrag, den
`/vater/vokabeln` zeigt. Der Schnellanleger im Assistenten (`VocabRefPicker`) ruft aber das Store-`POST` ohne Schlüssel
auf. `CreateCoreAsync` → `UniqueKeyAsync` (`VocabularyStoreController.cs`) erzeugt für ein schon vorhandenes Wort einen
`_2`-Eintrag. Die Variante hängt dann an einem Duplikat. Das Verhalten ist älter als B-71, B-71 hat es nur nicht
abgegrenzt. Aus Code gelesen, nicht gemessen.

## Verlauf

- **2026-10-26** — angelegt aus der Nachschau zu Sprint 11 (frischer Prüfer, read-only) in Lauf B
  ([Protokoll](../pm-sitzung-2026-10-26-tag.md)).
