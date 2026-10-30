---
tags: [typ/story, status/idee, bereich/backend, bereich/test, rolle/creator]
aliases: [Peek nach gespeichertem Schlüssel ungetötet; Store-Treffer-Kanten]
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
entgangen_bei: []
wartet_auf: ""
---

# B-343 · Dass `PeekDesiredIdsAsync` nach Inhalt auflöst, prüft kein Test – und zwei Kanten des Store-Treffers sind offen

Aus der Nachschau zu `3190f9e` (Store-Treffer nach Inhalt überall):

1. 🟡 **Peek nach Inhalt ungetestet** (`backend/Pugling.Api/Services/Creator/ExerciseItemService.cs:72`). Fall:
   `PUT items=[{vocabularyId: drifted}, {front:"house…", back:"Haus"}]`, wobei `drifted` per `PATCH` von `hus…` auf
   `house…` korrigiert wurde. Mit Peek nach Inhalt → `400`. Mit Peek nach gespeichertem Schlüssel läuft die Validierung
   durch, der Reconcile legt beide auf `drifted` → Unique-Index → `500` mit halb geschriebener Übung (die Klasse von
   B-331). Vorschlag: siebte Form in `UebungsPut_MitDoppeltemWort_Liefert400_UndSchreibtNichts`.
2. 🟢 Kein Test verbindet einen veralteten Schlüssel mit anders geschriebenem Wort (Groß/Klein); eine Mutante mit
   BINARY-Vergleich auf `Word` (`VocabularyStoreService.cs:80`) bliebe grün.
3. 🟢 `FindManyAsync` trimmt die gesuchten Wörter (`:77`), der Store speichert und patcht `Word` aber ungetrimmt
   (`VocabularyStoreController.cs:283`, `:328`). Ein Eintrag „ lake " mit veraltetem Schlüssel ist kein Kandidat.

Nicht Teil davon (🟢, Randfall): andere Stellen filtern Sprachen exakt, etwa die Birkenbihl-Prüfung
(`ExerciseControllers.cs:582-584`), die eine über einen Treffer erreichte „EN"-Karte ablehnen würde.

## Verlauf

- **2026-10-30** — angelegt aus der Nachschau zu Lauf K, Teil 2 (`pugling-reviewer`, 1× 🟡, 2× 🟢). Aus Code gelesen.
  `entgangen_bei` leer: `3190f9e` ist kein abgenommenes Increment einer Story.
