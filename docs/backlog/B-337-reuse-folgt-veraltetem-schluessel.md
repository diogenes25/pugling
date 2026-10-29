---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/vokabel, rolle/creator]
aliases: [reuseExisting trifft verdrifteten Eintrag]
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf K, Teil 1 (docs/pm-sitzung-2026-10-29-nacht.md)
grund: ""
ersetzt_durch: []
entgangen_bei: [B-277]
wartet_auf: ""
---

# B-337 · `reuseExisting` folgt dem Schlüssel, auch wenn der Eintrag inzwischen etwas anderes sagt

## Ist-Stand (Lauf K, am Code belegt)

[B-277](B-277-schnellanlage-legt-doppelten-store-eintrag-an.md) sucht den Treffer per `Key == generated`
(`VocabularyStoreController.cs`, Stand `0fcff19`). Der Store-`PATCH` ändert `Word` und `Translation`, aber nicht den
Schlüssel (`:340-341`) – Schlüssel und Inhalt laufen auseinander, erreichbar über den Bearbeiten-Dialog in `VaterVocab`:

- `huge→riesig` wird auf `huge→gewaltig` geändert; eine Schnellanlage `huge/riesig` bekommt den `gewaltig`-Eintrag zurück.
  Das getippte „riesig" geht verloren, das Kind wird dafür falsch bewertet. **Gemessen** (rote Probe): `OK` statt `Created`.
- Ein Tippfehler wird behoben (`hgue` → `huge`, Schlüssel bleibt `en_hgue_…`); die Schnellanlage `huge/riesig` verfehlt
  den Eintrag und legt genau die Dublette an, die B-277 verhindern soll.

B-277 Entscheidung 2 („Treffer = derselbe Server-Schlüssel, also gleiches Wort und gleiche Übersetzung") behauptete das
„also", ohne es zu prüfen.

## Entscheidungen (autonom, Freigabe 1 – `Defekt`)

1. **Der Inhalt entscheidet, nicht der gespeicherte Schlüssel**: Kandidaten sind der Schlüssel-Treffer und die Einträge mit
   gleichem Wort (NOCASE, über den Index) im Sprachpaar; Treffer ist, wessen **aktueller** Inhalt zu `generated` faltet,
   der älteste zuerst. *Kosten:* eine Abfrage mit `OR`; ein Wort, das sich nur in einem Nicht-ASCII-Großbuchstaben unterscheidet
   **und** dessen Schlüssel verdriftet ist, findet der Wort-Kandidat nicht (NOCASE faltet nur ASCII) – benannt.
2. `VocabularyStoreService.GetOrCreateAsync` (Inline-Wörter der Übungen) hat dieselbe Schwäche, ist aber älter und nicht im
   Diff – **zurückgestellt**, hier benannt.

## Akzeptanzkriterien

1. Ein per `PATCH` von der Übersetzung weg geänderter Eintrag ist kein Treffer mehr: `201`, neuer Eintrag mit der getippten
   Übersetzung.
2. Ein Eintrag, dessen Wort per `PATCH` berichtigt wurde, ist ein Treffer (`200`, seine Id), obwohl sein Schlüssel noch den
   alten Stand trägt.

## Schätzung

`XS`, Backend; Integrationstest in `VocabularyStoreTests`.

## Verlauf

- **2026-10-29** — angelegt aus der Nachschau (frischer `pugling-reviewer`, `git show 0fcff19`) in Lauf K, Teil 2; liegt im
  Increment dieses Laufs → **Limit 2**, der Lauf endet nach Teil 2. Im Sprint gebaut (Freigabe 3). Rote Probe **1 failed**
  (erwartet 1) an Z. 166 – `OK` statt `Created`. Mutanten: „ohne Inhaltsprüfung" **1 failed** (Z. 166); „nur
  Schlüssel-Kandidat" **1 failed** (Z. 172, der berichtigte Eintrag wird nicht gefunden).
- **2026-10-29** — Delta-Review `pugling-reviewer`: keine 🔴/🟡. 🟢 „Schlüssel-Kandidat ungetestet" nachgemessen: die Mutante
  „`v.Key == generated` gestrichen" fällt am älteren Fall `ReuseExisting_TrifftDenEintragDesServerSchluessels_…` (Z. 85,
  „École"/„école" findet nur der Schlüssel, NOCASE faltet `É` nicht) – getragen. 🟢 Ein verdrifteter Eintrag plus sein
  `_2`-Nachfolger mit dem ursprünglichen Inhalt: eine Schnellanlage in anderer Nicht-ASCII-Schreibweise verfehlt den `_2` und
  legt `_3` an – selten, besser als der frühere falsche Treffer; benannt.
- **2026-10-29** — Abnahme Lauf K, Teil 2: volle Suite **1017/1017**, Vitest **584/584**, `tsc -b` sauber, E2E **100 passed** (`exit=0`, nach der letzten Code-Änderung gestartet). Rollengang als E2E-Gesamtlauf (Browser-Extension nicht genutzt): Sohn über seine Arcade-Specs (Übung, Klausur, Buchstaben ordnen) plus die Komponentenfälle mit Knoten-Identität; Creator über `schnellanlage-ohne-dublette`; Vater Regressionszeuge. Was ein echter Screenreader ansagt, prüft keine Maschine – **Gerätecheck offen**: einmal mit NVDA/VoiceOver eine Übungsrunde und eine fortgesetzte Klausurfrage anhören. Reviews: `frontend-reviewer`, Delta-Review (ohne 🔴/🟡). **Abgenommen**, Commit siehe Protokoll ([Lauf K](../pm-sitzung-2026-10-29-nacht.md)).
