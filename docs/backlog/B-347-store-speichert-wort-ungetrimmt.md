---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/creator]
aliases: [Store-Wort mit Leerraum ist kein Treffer-Kandidat]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf K, Teil 2 in Lauf L (docs/pm-sitzung-2026-10-30-nacht.md), abgetrennt aus B-343
unverifiziert: false
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

## User Story

Als **Creator** möchte ich, dass „dieses Wort gibt es schon" auch einen Eintrag findet, dessen Wort ich per `PATCH`
korrigiert habe – mit Akzent, Umlaut-Großschreibung oder Leerraum –, damit kein zweiter Eintrag gleichen Inhalts entsteht.

## Ist-Stand (Lauf M, am Code belegt, Stand `d0afbc0`)

- `FindManyAsync` (`VocabularyStoreService.cs:77-80`) lud als Kandidaten Schlüssel-Treffer und Einträge mit
  `words.Contains(v.Word)` – die Spalte trägt `NOCASE` (`PuglingDbContext.cs:301`), SQLite faltet damit nur ASCII-Groß/Klein.
- Ein `PATCH` zieht den Schlüssel nicht nach (B-337); ein solcher Eintrag war nur über die Wortspalte erreichbar.
- **Gemessen:** Eintrag per `PATCH` auf „café", „Über" bzw. „ lake " gesetzt; `reuseExisting` mit „cafe", „über",
  „lake" → je `201` mit zweitem Eintrag (3 Fälle).

## Die echte Lücke

Die Kandidatensuche faltete weniger als der Vergleich danach; was SQL nicht fand, kam nie an den Vergleich.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Kandidat ist jeder gespeicherte Eintrag des Sprachpaars**, als schmale Projektion (Id, Sprachen, Wort, Übersetzung)
   geladen und im Speicher vollständig gefaltet; nur die Treffer werden getrackt nachgeladen. Die Sprachen filtert SQL
   per `trim(lower())` (Sprachkürzel sind ASCII). *Kosten:* ein Scan des Sprachpaars je Aufruf – ein Familien-Store hat
   Hunderte Wörter je Paar; im Batch einmal je Zeile. Die Alternative (gefaltete, indizierte Spalte) bräuchte eine
   Migration, und eine DB aus der alten Kette weist der Start ab – für einen P3 zu teuer.
2. **Kein Trimmen beim Speichern.** Mit der vollständigen Faltung im Speicher ist der Rand auf der gespeicherten Seite
   gleichgültig, und Altdaten heilen ohne Umschreiben. *Kosten:* „ lake " bleibt so gespeichert, wie es geschickt wurde.
3. **Der älteste Treffer gewinnt** (wie bisher), ungespeicherte Einträge derselben Arbeitseinheit zählen nach den
   gespeicherten (wie bisher) – beides jetzt mit eigener Zusicherung.

## Akzeptanzkriterien

1. Ein per `PATCH` auf „café", „Über" oder „ lake " gesetztes Wort ist Treffer für „cafe", „über", „lake".
2. Der Vertragstext nennt die Faltung ohne die alte ASCII-Grenze.
3. Bei zwei Einträgen gleichen Inhalts kommt der ältere zurück; ein ungespeicherter Eintrag wird vor dem Speichern gefunden.

## Schätzung

`S`, Backend, keine Migration, kein Vertragsbruch (die Antwort wird großzügiger, nie strenger). Testweg: `VocabularyStoreTests`.

## Verlauf

- **2026-10-30** — aus [B-343](B-343-store-treffer-kanten-ungetestet.md) Punkt 3 abgetrennt (🟢 der Nachschau): anders
  als die beiden übrigen Punkte keine Testlücke, sondern eine Verhaltensfrage mit zwei Wegen. Aus Code gelesen.
- **2026-10-30** — erweitert um die übrige Wortseite der Faltung (`pugling-reviewer` zu B-340, 🟡; der Vertragstext wurde
  auf das heutige Verhalten verengt statt die Suche zu erweitern).
- **2026-11-02** — Lauf M, Teil 1: ausformuliert, autonom gegrillt (Freigabe 1), geschätzt, gebaut. Rote Probe
  **3 failed** (erwartet 3): Theorie `ReuseExisting_FindetGepatchtesWortUeberDieGanzeFaltung`, je `201` statt `200`.
  AK 1 → diese Theorie, Mutante „Kandidaten nur über die NOCASE-Wortspalte" → **5 failed** (die drei Fälle, B-349 und der
  ältere B-277-Fall). AK 3 → `ReuseExisting_BeiZweiGleichenEintraegenGewinntDerAeltere` (Mutante „neuester gewinnt"
  **1 failed**) und `GetOrCreate_VorDemSpeichern_FindetDenEbenAngelegtenEintrag` (Mutante „ohne ungespeicherte" **1 failed**);
  beide Teile waren vorher ohne Zusicherung (beide Mutanten zunächst grün). Sprachfaltung: Mutante „Sprachen exakt" →
  **1 failed** (`ReuseExisting_FaltetSpracheUndLeerraumWieDerSchluessel`). AK 2 → Text in `VocabularyStoreDtos.cs`.
- **2026-11-02** — Review `pugling-reviewer` (Teil 1, 1041/1041 am Review-Stand): kein 🔴. 🟡 die Klassen-Doku von `VocabKey` nannte den Schlüssel noch „den Begriff von dasselbe Wort" → berichtigt (Doku, ohne Zählung); 🟡 der B-349-Test behauptete „rot, wenn der ganze Tracker zählt" – im neuen Aufbau nicht erzeugbar → Beschriftung auf den Fall, der ihn wirklich rot macht (Kandidaten wieder über Schlüssel/NOCASE), und den Service-Satz entsprechend gekürzt (nur Kommentare, ohne Zählung). 🟢 behoben ohne Zählung: veraltete Testkommentare („key"/„NOCASE-Kandidat") in drei Testdateien, SQL-`trim()` faltet nur Leerzeichen (Kommentar präzisiert), Theorie-Zeile „`_` gegen Leerraum-Folge" ergänzt und im Vertragstext genannt. 🟢 bewusst stehen gelassen: ein Scan des Sprachpaars je Batch-Zeile (Entscheidung 1 von B-347, Kosten benannt). **Zähler Teil 1: 0.**
- **2026-11-02** — Abnahme Lauf M, Teil 1: volle Suite **1045/1045**, Build ohne Warnung. E2E **102 passed** (`exit=0`, nach dem letzten Produktcode-Edit gestartet). Rollengang: Creator maschinell über die neuen Integrationstests gegen die In-Process-API; Vater und Sohn Regressionszeugen (ihre Specs im E2E-Lauf grün, kein Pfad von ihnen im Diff). **Abgenommen**, Commit siehe Protokoll ([Lauf M](../pm-sitzung-2026-11-02-nacht.md)).
