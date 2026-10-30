---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/test, rolle/creator]
aliases: [Peek nach gespeichertem Schlüssel ungetötet; Store-Treffer-Kanten]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf K, Teil 2 in Lauf L (docs/pm-sitzung-2026-10-30-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
nachgeschaut: "2026-10-30"
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

## User Story

Als **Entwickler** möchte ich, dass ein Rückfall des Store-Treffers auf den gespeicherten Schlüssel oder einen
Groß/Klein-genauen Vergleich einen Test rot macht.

## Ist-Stand (Lauf L, am Code belegt)

Punkte 1 und 2 oben; kein Test hat einen per `PATCH` korrigierten Eintrag neben demselben Wort inline bzw. einen veralteten
Schlüssel mit anders geschriebenem Wort.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Punkt 1** als siebte Form in `UebungsPut_MitDoppeltemWort_Liefert400_UndSchreibtNichts` (ein Store-Eintrag „hus…",
   per `PATCH` zu „house…", daneben „house…" inline).
2. **Punkt 2** im bestehenden Fall `ReuseExisting_FolgtDemInhaltNichtDemVeraltetenSchluessel`: der Treffer wird mit dem
   Wort in Großbuchstaben gesucht.
3. **Punkt 3 abgetrennt** → [B-347](B-347-store-speichert-wort-ungetrimmt.md): keine Testlücke, sondern eine
   Verhaltensfrage mit zwei Wegen. *Kosten:* der Randfall bleibt offen.

## Akzeptanzkriterien

1. Mutante „Peek nach gespeichertem Schlüssel" → die siebte Form fällt (`500`).
2. Mutante „`Word` mit BINARY-Kollation" in `FindManyAsync` → der Reuse-Fall fällt.

## Schätzung

`S`, nur Tests. Testweg: `ExerciseItemsAndProgressTests`, `VocabularyStoreTests`.

## Verlauf

- **2026-10-30** — angelegt aus der Nachschau zu Lauf K, Teil 2 (`pugling-reviewer`, 1× 🟡, 2× 🟢). Aus Code gelesen.
  `entgangen_bei` leer: `3190f9e` ist kein abgenommenes Increment einer Story.
- **2026-10-30** — Lauf L, Teil 2: gebaut. Mutante „Peek nach gespeichertem Schlüssel" (`ExerciseItemService.PeekDesiredIdsAsync`
  sucht per `v.Key`) → **1 failed** (erwartet 1, „id after a word fix and the same word inline: 500"). Mutante
  `EF.Functions.Collate(v.Word, "BINARY")` in `FindManyAsync` → **1 failed** (erwartet 1,
  `ReuseExisting_FolgtDemInhaltNichtDemVeraltetenSchluessel`). Punkt 3 → B-347.
- **2026-10-30** — Review `pugling-reviewer`: 🟢 beide Fälle hingen an der unbelegten Prämisse „der `PATCH` lässt den Schlüssel stehen" → je eine Zusicherung auf den Schlüssel nach dem `PATCH` (ohne Zählung).
- **2026-10-30** — Abnahme Lauf L, Teil 2: volle Suite **1028/1028**, Build ohne Warnung. E2E-Gesamtlauf **100 passed** (`exit=0`, nach dem letzten Produktcode-Edit gestartet; danach nur Tests, Vertragskommentar und die neue Spec), danach die neue Spec und die drei Nachbar-Specs **4 passed**. Rollengang: Vater/Creator über die neue Spec `schlagwort-trotz-fremdem-synonym` (Schlagwort-Verwaltung, Facette ändern → Neuladen → gespeichert; rote Probe ohne den Fix **1 failed**); Sohn Regressionszeuge (seine Specs im Gesamtlauf grün, kein Pfad von ihm im Diff). Kein Delta-Review nach den Nachbesserungen: sie sind Testfälle und Kommentare, je mit gemessener Mutante (Selbst-Check). **Abgenommen**, Commit siehe Protokoll ([Lauf L](../pm-sitzung-2026-10-30-nacht.md)).
- **2026-10-30** — Nachschau in Lauf L, Teil 3: **sauber**. Geprüft: die siebte Form erreicht den Peek mit veraltetem Schlüssel; der Reuse-Fall trifft nur über den NOCASE-Wortkandidaten; beide Prämissen-Zusicherungen stehen.
