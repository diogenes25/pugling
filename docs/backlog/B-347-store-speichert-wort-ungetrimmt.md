---
tags: [typ/story, status/idee, bereich/backend, rolle/creator]
aliases: [Store-Wort mit Leerraum ist kein Treffer-Kandidat]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: Nachschau zu Lauf K, Teil 2 in Lauf L (docs/pm-sitzung-2026-10-30-nacht.md), abgetrennt aus B-343
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-347 · Ein per PATCH geändertes Store-Wort trifft nur bis auf ASCII-Groß/Klein

`VocabularyStoreService.FindManyAsync` lädt als Kandidaten die Einträge, deren gespeicherter Schlüssel passt **oder**
deren `Word` einem der gesuchten Wörter gleicht – die gesuchten Wörter sind getrimmt. Der Store speichert und patcht
`Word` aber ungetrimmt (`VocabularyStoreController.cs`, Anlegen und `PATCH`). Ein Eintrag „ lake " mit veraltetem
Schlüssel (ein `PATCH` zog ihn nicht nach) ist darum kein Kandidat, obwohl sein Inhalt zum gesuchten Wort faltet; ein
zweiter Eintrag entsteht.

Allgemeiner (Review zu B-340 in Lauf L): Für einen Eintrag mit veraltetem Schlüssel faltet die Wortseite nur, was die
NOCASE-Kollation faltet – ASCII-Groß/Klein. Akzente, ß, Apostrophe, Nicht-ASCII-Groß/Klein (Ü/ü) und Leerraum auf der
gespeicherten Seite nicht. Repro aus dem Code: `kaffe`/`x` anlegen, Wort per `PATCH` zu `café`, dann `reuseExisting` mit
`cafe`/`x` → `201`, zweiter Eintrag gleichen Inhalts. Der Vertragstext (`CreateVocabularyDto`) nennt diese Grenze seit
Lauf L ausdrücklich.

Zu klären beim Ausformulieren: den Store beim Anlegen und Ändern trimmen lassen (dann bleiben Altdaten offen), oder die
Kandidatensuche den Rand ignorieren lassen (SQLite: `trim()` verliert die NOCASE-Kollation der Spalte). Erst messen.

## Verlauf

- **2026-10-30** — aus [B-343](B-343-store-treffer-kanten-ungetestet.md) Punkt 3 abgetrennt (🟢 der Nachschau): anders
  als die beiden übrigen Punkte keine Testlücke, sondern eine Verhaltensfrage mit zwei Wegen. Aus Code gelesen.
- **2026-10-30** — erweitert um die übrige Wortseite der Faltung (`pugling-reviewer` zu B-340, 🟡; der Vertragstext wurde
  auf das heutige Verhalten verengt statt die Suche zu erweitern).
