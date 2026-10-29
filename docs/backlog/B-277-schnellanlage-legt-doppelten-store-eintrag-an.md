---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/vokabel, rolle/supervisor]
aliases: [Assistent legt _2-Eintrag für bekanntes Wort an]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Sprint 11 in Lauf B (docs/pm-sitzung-2026-10-26-tag.md)
grund: ""
ersetzt_durch: []
entgangen_bei: [B-71]
wartet_auf: ""
nachgeschaut: "2026-10-29"
---

# B-277 · Der Schnellanleger im Assistenten legt für ein bekanntes Wort einen zweiten Store-Eintrag an

[B-71](B-71-inline-vokabelliste-ohne-varianten.md) AK 5 sagt, eine im Übungseditor eingegebene Variante landet auf **demselben** Store-Eintrag, den
`/vater/vokabeln` zeigt. Der Schnellanleger im Assistenten (`VocabRefPicker`) ruft aber das Store-`POST` ohne Schlüssel
auf. `CreateCoreAsync` → `UniqueKeyAsync` (`VocabularyStoreController.cs`) erzeugt für ein schon vorhandenes Wort einen
`_2`-Eintrag. Die Variante hängt dann an einem Duplikat. Das Verhalten ist älter als B-71, B-71 hat es nur nicht
abgegrenzt. Aus Code gelesen, nicht gemessen.

## User Story

Als **Vater/Creator**, der im Übungs-Assistenten ein Wort schnell anlegt, möchte ich, dass ein Wort, das es im Store
schon gibt, gewählt statt verdoppelt wird – samt der Variante, die ich eintippe –, damit „Auch richtig“ auf dem
Eintrag landet, den ich unter `/vater/vokabeln` sehe und pflege.

## Ist-Stand (Lauf K, am Code belegt)

- `VocabRefPicker.quickAdd` (`frontend/src/vater/VaterExerciseCreate.tsx:396-405`, vor dem Fix) ruft
  `api.createVocabulary` ohne Schlüssel.
- `VocabularyStoreController.CreateCoreAsync` (`:233-236`) erzeugt den Schlüssel per `UniqueKeyAsync` – für ein schon
  vorhandenes Paar `…_2`. Das ist für den Store-`POST` gewollt (Dubletten-Hinweis statt Verbot, B-65 Entscheidung 6 –
  im Store-Formular sieht der Vater den Hinweis).
- **Gemessen** (rote Probe unten): nach dem Schnellanlegen eines vorhandenen Paars stehen zwei Einträge `riesig` im Store.

## Die echte Lücke

Der Assistent hat keinen Dubletten-Hinweis und keinen Weg zum vorhandenen Eintrag; er legt still ein Duplikat an.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. ~~Im Frontend lösen (vorher `vocabulary/lookup`, Treffer per Groß/Klein-Vergleich).~~ **Ersetzt nach dem Review:**
   der Client-Vergleich ist schwächer als der Server-Schlüssel (`VocabKey.Slug` faltet Diakritika, ß, Apostrophe), und
   der Lookup trifft über SQLite-`NOCASE` nur ASCII – „École"/„école" fände nichts. Für Französisch kein Randfall.
   **Stattdessen der Server:** der Store-`POST` bekommt das optionale Feld `reuseExisting`; ohne `key` liefert er dann
   den Eintrag unter dem Schlüssel, den er selbst erzeugen würde (`200`), statt ein `_2` anzulegen. Additiv, kein
   Vertragsbruch: ohne das Feld bleibt alles wie bisher (B-65, Entscheidung 6). *Kosten:* ein Feld mehr im Vertrag, das
   nur der Assistent setzt.
2. **Treffer = derselbe Server-Schlüssel** ~~, also gleiches Wort und gleiche Übersetzung~~ – **berichtigt mit
   [B-337](B-337-reuse-folgt-veraltetem-schluessel.md):** der gespeicherte Schlüssel folgt einem `PATCH` nicht; Treffer ist,
   wessen **aktueller** Inhalt zum erzeugten Schlüssel faltet. Eine andere Übersetzung ist ein Homonym und bekommt einen
   eigenen Eintrag. *Kosten:* eine Abfrage über Schlüssel und Wort.
3. **Neue Varianten ergänzt der Server** zu den vorhandenen (`CleanAlternatives` über beide Listen); die übrigen Felder
   des Treffers bleiben unberührt. Ersetzen würde verwerfen, was im Store gepflegt wurde; die Vereinigung im Server statt
   im Client erspart auch das verlorene Update zwischen Lesen und `PATCH`. *Kosten:* ein Tippfehler in einer Variante ist
   nur im Store oder im Bearbeiten-Dialog wieder zu entfernen.
4. **Der Schnellanleger läuft über `useAction` + `StatusBanner`** (Review): Sperre im Primitiv, Meldung als Live-Region.

## Akzeptanzkriterien

1. Schnellanlage eines vorhandenen Paars wählt den vorhandenen Eintrag (gleiche Id), kein zweiter entsteht – auch in
   anderer Schreibweise, die der Store-Schlüssel faltet (Groß/Klein, Akzente).
2. Eine dabei eingetippte Variante steht danach **zusätzlich** zu den vorhandenen am Eintrag.
3. Dasselbe Wort mit anderer Übersetzung legt einen eigenen Eintrag an.

## Schätzung

`S`, Backend (ein additives Feld) + Frontend, keine Migration, kein Vertragsbruch. Testweg: Integrationstest
`VocabularyStoreTests.ReuseExisting_…` (Faltung, Vereinigung, ohne Feld bleibt `_2`) und E2E
`e2e/schnellanlage-ohne-dublette.spec.ts` (der Rundweg Assistent → Store, also kein Komponententest mit gefälschtem
`fetch`, `frontend/CLAUDE.md`).

## Verlauf

- **2026-10-26** — angelegt aus der Nachschau zu Sprint 11 (frischer Prüfer, read-only) in Lauf B
  ([Protokoll](../pm-sitzung-2026-10-26-tag.md)).
- **2026-10-29** — Lauf K, Teil 1: ausformuliert, autonom gegrillt (Freigabe 1), geschätzt, gebaut. Rote Probe: neue Spec
  **1 failed** (erwartet 1) an Z. 65 – gemessen `["riesig", "riesig", "ungeheuer"]` statt `["riesig", "ungeheuer"]`.
  Danach neue Spec und `auch-richtig-im-uebungseditor.spec.ts` **2 passed**. Mutanten: „Treffer nur nach dem Wort“
  **1 failed** (Z. 60, das Homonym erscheint nicht); „Varianten ersetzen statt ergänzen“ **1 failed** (Z. 69, „sehr groß“
  fehlt).
- **2026-10-29** — `frontend-reviewer`: 🟡 Client-Vergleich schwächer als der Server-Schlüssel (`_2` bei Akzent-Großbuchstaben,
  ß, Diakritika) → Entscheidungen 1–3 ersetzt, Server-Feld `reuseExisting` (**zählt**); 🟡 neue Mutation an `useAction`
  vorbei → umgestellt, `StatusBanner` statt `div` ohne Rolle (**zählt**); 🟢 Groß/Klein ungetestet → die Spec tippt
  „Riesig" (ohne Zählung); 🟢 Token hängt an der ersten Store-Seite → die Spec filtert vorher (ohne Zählung); 🟢 verlorenes
  Update zwischen Lookup und `PATCH` → entfällt mit der Vereinigung im Server. Rote Probe des neuen Integrationstests
  **1 failed** (erwartet 1) – `BadRequest` (`unknown_field`) statt `OK`. Mutanten: „ersetzt statt ergänzt" **1 failed**
  (Z. 82); „Feld ignoriert" **1 failed** (Z. 78). Die beiden Frontend-Mutanten vom ersten Bau sind mit dem Server-Weg
  gegenstandslos; die Spec hält jetzt den Rundweg, der Integrationstest die Regel.
- **2026-10-29** — Delta-Review `pugling-reviewer` auf den Server-Weg: 🟡 der Reuse-Zweig übersprang die
  `BaseFormKey`-Prüfung (`200` statt `400`) und verwarf gesendete Tags still → Prüfung vor den Zweig, Tags additiv wie
  beim `PATCH` (**zählt**); 🟡 `key` + `reuseExisting` wurde stumm ignoriert → `400 validation_error` (**zählt**); 🟡 die
  Batch-Abbildung `Reused → Existing` ohne Fall, Batch-Doku nannte das Feld nicht → Fall und Satz (**zählt**). 🟢 Übersetzung
  im Reuse-Fall nicht zugesichert → zugesichert; 🟢 Inline-`PATCH` bei B-334 ohne Fall → ergänzt (beide ohne Zählung).
  Die Fixes standen vor den Fällen, darum je Fall die Mutante „alter Zustand“: Flag mit Schlüssel erlaubt **1 failed**
  (Z. 117); `BaseFormKey` im Reuse übersprungen **1 failed** (Z. 125); Batch `Created` statt `Existing` **1 failed**
  (Z. 145); Tags verworfen **1 failed** (Z. 87); B-334-Prüfung nur im Id-Zweig **1 failed** (Z. 496). **Zähler des Sprints: 5.**
- **2026-10-29** — Abnahme Lauf K, Teil 1: volle Suite **1015/1015**, Vitest **576/576**, `tsc -b` sauber, E2E **100 passed** (`exit=0`, nach der letzten Code-Änderung gestartet; danach nur ein Testkommentar berichtigt). Rollengang als E2E-Gesamtlauf (Browser-Extension nicht genutzt): Creator/Vater über `schnellanlage-ohne-dublette` und `auch-richtig-im-uebungseditor`, Sohn als Regressionszeuge über seine Specs. Reviews: `pugling-reviewer`, `frontend-reviewer`, zwei Delta-Reviews (das letzte ohne 🔴/🟡). **Abgenommen**, Commit siehe Protokoll ([Lauf K](../pm-sitzung-2026-10-29-nacht.md)).
- **2026-10-29** — Nachschau in Lauf K, Teil 2 (frischer `pugling-reviewer`, `git show 0fcff19`): **entgangen** → [B-337](B-337-reuse-folgt-veraltetem-schluessel.md) (🟡, Treffer per gespeichertem Schlüssel, den ein `PATCH` nicht nachzieht – Entscheidung 2 behauptete „also gleiches Wort und gleiche Übersetzung", ohne es zu prüfen; im Sprint behoben). Geprüft: übrige Aufrufer von `CreateVocabularyDto` (`VaterVocab.tsx:162-169` Batch ohne Feld, gewollt; `CreatorApi.cs:196-202`; KI-Creator ohne Feld), Prüfungen vor dem Abkürzungsweg (`:240-250`), Batch-Abbildung `:518-520`.
