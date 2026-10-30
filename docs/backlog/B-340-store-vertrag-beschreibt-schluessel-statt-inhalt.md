---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/creator]
aliases: [Vertragstext zu reuseExisting/BatchItemStatus.Existing nennt den Schlüssel]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf K, Teil 2 in Lauf L (docs/pm-sitzung-2026-10-30-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-337]
wartet_auf: ""
nachgeschaut: "2026-10-30"
---

# B-340 · Der Vertrag beschreibt die Wiederverwendung im Store noch über den Schlüssel, nicht über den Inhalt

[B-337](B-337-reuse-folgt-veraltetem-schluessel.md) hat `reuseExisting` auf den **aktuellen Inhalt** umgestellt (ein
`PATCH` zieht den gespeicherten Schlüssel nicht nach). Die Doku im Vertrag sagt weiter das Gegenteil: „if the key the
server would generate already names an entry, that entry comes back" (`backend/Pugling.Contracts/Creator/VocabularyStoreDtos.cs:27-29`),
ebenso `BatchItemStatus.Existing` (`:73`) und `VocabularyStoreController.cs:206`, `:487-488`. Für einen veralteten
Schlüssel ist das Verhalten heute genau umgekehrt. Weder `448785c` noch `3190f9e` haben den Text angefasst.

Vorschlag: „an entry whose current word and translation fold to the same key", OpenAPI-Datei neu erzeugen.

## User Story

Als **Creator**, der den Store über die API anspricht, möchte ich in der Vertragsdoku lesen, was `reuseExisting` wirklich
tut, damit ich mich nicht auf den gespeicherten Schlüssel verlasse.

## Ist-Stand (Lauf L, am Code belegt)

Vier Doku-Stellen beschreiben den Treffer über den Schlüssel: `VocabularyStoreDtos.cs` (`CreateVocabularyDto`,
`BatchItemStatus.Existing`) und `VocabularyStoreController.cs` (`Create`, `CreateBatch`). Der Code trifft seit B-337/
`3190f9e` über `FindManyAsync` nach aktuellem Inhalt.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Text an den Code angleichen, nicht umgekehrt.** B-337 hat die Richtung entschieden. *Kosten:* keine; die
   OpenAPI-Datei ändert drei Beschreibungen.

## Akzeptanzkriterien

1. Keine der vier Stellen nennt mehr den Schlüssel als Treffergrund für `reuseExisting`; `docs/openapi/v1.json` trägt den
   neuen Text.

## Schätzung

`XS`, nur Doku im Vertrag. Testweg: `ContractDocumentTests` erzeugt die OpenAPI-Datei.

## Verlauf

- **2026-10-30** — angelegt aus der Nachschau zu Lauf K, Teil 2 (`pugling-reviewer`, 🟡). Aus Code gelesen.
- **2026-10-30** — Lauf L, Teil 2: ausformuliert, gegrillt, geschätzt, gebaut. Kein Verhalten, keine rote Probe; die volle
  Suite schrieb `docs/openapi/v1.json` neu (3 Zeilen).
- **2026-10-30** — Review `pugling-reviewer`: 🟡 der neue Text versprach mehr Faltung, als `FindManyAsync` für einen per `PATCH` geänderten Eintrag leistet (Wortseite nur bis ASCII-Groß/Klein) → Text auf das heutige Verhalten verengt, die Lücke nach [B-347](B-347-store-speichert-wort-ungetrimmt.md) (nur Kommentar, ohne Zählung).
- **2026-10-30** — Abnahme Lauf L, Teil 2: volle Suite **1028/1028**, Build ohne Warnung. E2E-Gesamtlauf **100 passed** (`exit=0`, nach dem letzten Produktcode-Edit gestartet; danach nur Tests, Vertragskommentar und die neue Spec), danach die neue Spec und die drei Nachbar-Specs **4 passed**. Rollengang: Vater/Creator über die neue Spec `schlagwort-trotz-fremdem-synonym` (Schlagwort-Verwaltung, Facette ändern → Neuladen → gespeichert; rote Probe ohne den Fix **1 failed**); Sohn Regressionszeuge (seine Specs im Gesamtlauf grün, kein Pfad von ihm im Diff). Kein Delta-Review nach den Nachbesserungen: sie sind Testfälle und Kommentare, je mit gemessener Mutante (Selbst-Check). **Abgenommen**, Commit siehe Protokoll ([Lauf L](../pm-sitzung-2026-10-30-nacht.md)).
- **2026-10-30** — Nachschau in Lauf L, Teil 3 (frischer `pugling-reviewer`): Satz gegen `FindManyAsync`, `VocabKey.Slug`/`Generate`, `CreateCoreAsync`, `CreateBatch` Zeile für Zeile gelesen. 🟢 „whitespace" versprach mehr, als der Schlüssel faltet (nur ein `"__"→"_"`-Durchgang, keine Tabs/NBSP) → „surrounding whitespace" (Kommentar, ohne Zählung). 🟢 im Batch zählen Zeilen, die frühere Einträge geladen haben → [B-349](B-349-batch-treffer-haengt-an-der-reihenfolge.md). Kein 🟡/🔴.
