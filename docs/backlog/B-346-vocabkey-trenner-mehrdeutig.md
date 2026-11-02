---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/creator]
aliases: [VocabKey ohne eindeutigen Trenner, zwei Paare ein Schlüssel]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: Review zu B-336 in Lauf L (docs/pm-sitzung-2026-10-30-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
nachgeschaut: "2026-11-02"
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

## User Story

Als **Creator** möchte ich zwei verschiedene Wortpaare in den Store und in eine Übung legen können, auch wenn ihr
Schlüssel zufällig gleich zusammengesetzt wird, damit kein Paar die Übersetzung eines anderen erbt.

## Ist-Stand (Lauf M, am Code belegt, Stand `d0afbc0`)

- `VocabKey.Generate` (`Pugling.Contracts/Common/VocabKey.cs:22-29`) fügt `{src}_{wort}_{tgt}_{übersetzung}` mit `_`
  zusammen; Leerzeichen im Wort werden ebenfalls `_`. Der Trenner ist mehrdeutig.
- „Dasselbe Wort" hing an diesem Verbundschlüssel an drei Stellen: `VocabularyStoreService.FindManyAsync` (`:71-88`,
  Treffer-Dictionary nach Schlüssel), `ExerciseItemService.PeekDesiredIdsAsync` (`:74`) und
  `RepeatedWordAsync` (`ExerciseControllers.cs:77-91`).
- **Gemessen:** „pomme de terre" → „Kartoffel" und „pomme" → „terre de Kartoffel" (fr→de): `reuseExisting` liefert `200`
  mit dem ersten Eintrag; eine Übung mit beiden Paaren (en→de, gleiche Naht „de") liefert `400 validation_error`.

## Die echte Lücke

Der Schlüssel ist ein Bezeichner, kein Vergleichsbegriff – er wurde als beides benutzt.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **„Dasselbe Wort" ist das Tupel der vier einzeln gefalteten Teile** (`VocabKey.Identity` → `VocabIdentity`,
   `record struct`), nicht der Verbundschlüssel. Jeder Teil faltet wie `Generate` innerhalb eines Teils (`Slug`, `_` und
   Leerraum-Folgen als ein Leerzeichen). *Kosten:* ein zweiter öffentlicher Begriff neben `Generate` im Vertrag;
   `Generate` bleibt, was es ist – der Erzeuger des Bezeichners.
2. **Gespeicherte Schlüssel bleiben unverändert.** Ein zweites Paar mit gleichem Verbundschlüssel bekommt `_2` wie jede
   andere Kollision. *Kosten:* keine Migration, kein Vertragsbruch; die Mehrdeutigkeit bleibt im Bezeichner sichtbar.
3. **Der KI-Creator faltet die Vorderseite über `VocabKey.Part`** statt über `Generate(src, front, tgt, "")` – dieselbe
   Faltung, ohne den Umweg über den Schlüssel.

## Akzeptanzkriterien

1. `reuseExisting` mit „pomme" → „terre de Kartoffel" neben „pomme de terre" → „Kartoffel" legt einen neuen Eintrag an.
2. Eine Übung mit beiden Paaren ist gültig und verlinkt zwei Store-Einträge.

## Schätzung

`S`, Backend, keine Migration, kein Vertragsbruch (additive Methode). Testweg: `VocabularyStoreTests`,
`ExerciseItemsAndProgressTests`; Mutante „Identität = Verbundschlüssel".

## Verlauf

- **2026-10-30** — angelegt aus dem `pugling-reviewer`-Hinweis 🟢 zu B-336 in Lauf L (ohne Zählung, älter als der
  Sprint-Diff). Aus Code gelesen, nicht gemessen.
- **2026-11-02** — Lauf M, Teil 1: ausformuliert, autonom gegrillt (Freigabe 1), geschätzt, gebaut. Rote Probe
  **2 failed** (erwartet 2): `ReuseExisting_UnterscheidetPaareMitGleichemVerbundschluessel` (`200` statt `201`),
  `UebungsPost_ZweiPaareMitGleichemVerbundschluessel_SindZweiWoerter` (`400` beim Anlegen). AK 1 → erster Test,
  AK 2 → zweiter; Mutante „`Identity` = `Generate(…)`" → **2 failed** (erwartet 2). Entscheidung 3 hält
  `CreatorAgentTests` (die B-336-Theorie) unverändert grün.
- **2026-11-02** — Review `pugling-reviewer` (Teil 1, 1041/1041 am Review-Stand): kein 🔴. 🟡 die Klassen-Doku von `VocabKey` nannte den Schlüssel noch „den Begriff von dasselbe Wort" → berichtigt (Doku, ohne Zählung); 🟡 der B-349-Test behauptete „rot, wenn der ganze Tracker zählt" – im neuen Aufbau nicht erzeugbar → Beschriftung auf den Fall, der ihn wirklich rot macht (Kandidaten wieder über Schlüssel/NOCASE), und den Service-Satz entsprechend gekürzt (nur Kommentare, ohne Zählung). 🟢 behoben ohne Zählung: veraltete Testkommentare („key"/„NOCASE-Kandidat") in drei Testdateien, SQL-`trim()` faltet nur Leerzeichen (Kommentar präzisiert), Theorie-Zeile „`_` gegen Leerraum-Folge" ergänzt und im Vertragstext genannt. 🟢 bewusst stehen gelassen: ein Scan des Sprachpaars je Batch-Zeile (Entscheidung 1 von B-347, Kosten benannt). **Zähler Teil 1: 0.**
- **2026-11-02** — Abnahme Lauf M, Teil 1: volle Suite **1045/1045**, Build ohne Warnung. E2E **102 passed** (`exit=0`, nach dem letzten Produktcode-Edit gestartet). Rollengang: Creator maschinell über die neuen Integrationstests gegen die In-Process-API; Vater und Sohn Regressionszeugen (ihre Specs im E2E-Lauf grün, kein Pfad von ihnen im Diff). **Abgenommen**, Commit siehe Protokoll ([Lauf M](../pm-sitzung-2026-11-02-nacht.md)).
- **2026-11-02** — Nachschau in Lauf M, Teil 2 (frischer `pugling-reviewer`, `git show ffbb0b6`, 117/117 in den drei Testklassen): **sauber**. Geprüft: AK 1 → `ReuseExisting_UnterscheidetPaare…`, Mutante „`Identity` = `Generate`" rot; AK 2 → `UebungsPost_ZweiPaare…`, Mutanten „`RepeatedWordAsync` per `Generate`" und „`found` per `Generate`" rot; Entscheidung 3 → B-336-Theorie (Mutante „`Part` = `Slug`" rot); Peek kann wegen des `Dictionary<VocabIdentity,…>`-Typs nicht vom Reconcile abweichen; `Part` faltet außer an der Naht mindestens so viel wie `Generate` (kein alter Treffer wird strenger).
