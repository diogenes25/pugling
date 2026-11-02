---
tags: [typ/story, status/idee, bereich/backend, rolle/creator]
aliases: [Sprachkürzel mit Leerraum, Store-Faltung Restkanten]
status: idee
prio: P4
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: Nachschau zu Lauf M, Teil 1 (docs/pm-sitzung-2026-11-02-nacht.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-355 · Restkanten der Store-Faltung: Sprachkürzel mit Leerraum, zwei gleiche Paare in einem Aufruf

Drei 🟢 der Nachschau über `ffbb0b6` (B-346/B-347/B-349), keiner davon eine Entgleitung:

1. **Sprachkürzel sind freie Strings.** `VocabularyStoreService.FindManyAsync` (`:79-82`) filtert die Sprachen per SQL
   `trim(lower())`; SQLite-`trim()` entfernt nur Leerzeichen, `lower()` nur ASCII. Ein Eintrag mit `sourceLanguage = "en\t"`
   ist für `"en"` kein Kandidat mehr (vorher über den Schlüssel erreichbar). Kein Test speichert eine Sprache mit Rand –
   die Mutante „gespeicherte Seite ohne `Trim()`" bliebe grün. Weg: Sprachkürzel beim Anlegen/`PATCH` normalisieren
   (dann Vertragsfrage) oder eine Testzeile `" en "`.
2. **Zwei Paare gleicher Identität in einem `GetOrCreateManyAsync`-Aufruf** (`:46`, `found[identity] = vocab`) trägt kein
   Test; die Vokabel-POST lehnt solche Payloads vorher ab, aber `NormalizeConfigAsync` der Übersetzungsübung
   (`ExerciseControllers.cs:431`) nicht – ohne die Zeile entstünden still zwei Einträge (`key`, `key_2`). Die Zeile ist
   richtig und älter.
3. **Hinweis, keine Lücke:** `VocabKey.Slug` entfernt jedes `NonSpacingMark`, also auch japanische Dakuten (が→か) und
   hebräische/arabische Vokalzeichen. Seit B-347 erreicht die Faltung mehr Einträge. Erst relevant, wenn nichtlateinische
   Sprachen dazukommen.

## Verlauf

- **2026-11-02** — angelegt aus der Nachschau zu Lauf M, Teil 1 (`pugling-reviewer`, 🟢 ×3). Aus Code gelesen.
