---
tags: [typ/story, status/idee, bereich/backend, rolle/creator]
aliases: [VocabKey ohne eindeutigen Trenner, zwei Paare ein Schlüssel]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: Review zu B-336 in Lauf L (docs/pm-sitzung-2026-10-30-nacht.md)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-346 · Zwei verschiedene Wortpaare können denselben Vokabel-Schlüssel ergeben

`VocabKey.Generate` (`backend/Pugling.Contracts/Common/VocabKey.cs`) setzt `{src}_{wort}_{tgt}_{übersetzung}` mit
Unterstrich als Trenner zusammen, und Leerzeichen im Wort werden ebenfalls zu Unterstrichen. Der Trenner ist darum
nicht eindeutig. Beispiel des Prüfers mit `fr→de`:

- A = „pomme de terre" → „Kartoffel"
- B = „pomme" → „terre de Kartoffel"

Beide ergeben `fr_pomme_de_terre_de_kartoffel`. Der Server lehnt B neben A in einer Übung mit `400` ab
(`RepeatedWordAsync`), und `VocabularyStoreService.FindManyAsync`/`GetOrCreateManyAsync` legen beide Paare auf
**denselben** Store-Eintrag – B bekäme die Übersetzung von A.

Praktisch selten (braucht das Sprachkürzel der Zielsprache als Wort an der Nahtstelle), aber ein echter Fehler im
Begriff „dasselbe Wort". Zu klären beim Ausformulieren: ob der Treffer nach Inhalt (`FindManyAsync`) statt nach dem
zusammengesetzten Schlüssel vergleichen kann (Wort und Übersetzung getrennt gefaltet), ohne die gespeicherten Schlüssel
zu ändern; sonst `migration`/`vertragsbruch` prüfen. Erst messen.

## Verlauf

- **2026-10-30** — angelegt aus dem `pugling-reviewer`-Hinweis 🟢 zu B-336 in Lauf L (ohne Zählung, älter als der
  Sprint-Diff). Aus Code gelesen, nicht gemessen.
