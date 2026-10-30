---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/creator]
aliases: [reuseExisting verglich Sprache exakt und Wort ungetrimmt]
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
---

# B-344 · `reuseExisting` verglich die Sprache exakt und das Wort ungetrimmt

Nachgetragen fürs Wirkungsmaß: `ReuseCandidateAsync` aus [B-337](B-337-reuse-folgt-veraltetem-schluessel.md)
(`448785c`) verglich `v.Word == dto.Word` ohne Trim und `SourceLanguage`/`TargetLanguage` exakt (BINARY-Kollation;
nur `Word`/`Translation` sind NOCASE, `Data/PuglingDbContext.cs:301-302`). Ein Eintrag mit „EN"/„DE" oder gepolstertem
Wort und veraltetem Schlüssel wurde verfehlt, ein zweiter angelegt.

**Behoben vom Nutzer außerhalb der Schleife** in `3190f9e` (geteilter Treffer `FindManyAsync`, faltet Sprache und
Leerraum wie der Schlüssel); belegt durch `VocabularyStoreTests.ReuseExisting_FaltetSpracheUndLeerraumWieDerSchluessel`.

## User Story

Als **Creator**, der ein Wort mit `reuseExisting` anlegt, möchte ich den vorhandenen Eintrag zurückbekommen, auch wenn er
mit „EN" oder Leerraum gespeichert ist, damit kein zweiter entsteht.

## Ist-Stand (am Code belegt)

Siehe oben: `ReuseCandidateAsync` in `448785c`; heute `VocabularyStoreService.FindManyAsync` (faltet Sprache und Leerraum
im Speicher wie `VocabKey`).

## Entscheidungen

Keine eigenen – vom Nutzer in `3190f9e` gebaut; hier nur nachgetragen.

## Akzeptanzkriterien

1. Ein Eintrag mit veraltetem Schlüssel und abweichender Sprach-Schreibweise oder Leerraum wird wiederverwendet –
   gehalten von `VocabularyStoreTests.ReuseExisting_FaltetSpracheUndLeerraumWieDerSchluessel`.

## Schätzung

`XS`, Backend, keine Migration, kein Vertragsbruch (gebaut in `3190f9e`).

## Verlauf

- **2026-10-30** — angelegt in der Nachschau zu Lauf K, Teil 2 (`pugling-reviewer`): Fluchtweg von B-337, gefunden
  durch ein Review außerhalb der Schleife und dort behoben. Hier nur nachgetragen, damit die Entgleitung im Index zählt;
  `abgenommen` steht für den Commit `3190f9e` des Nutzers, kein eigener Rollengang.
