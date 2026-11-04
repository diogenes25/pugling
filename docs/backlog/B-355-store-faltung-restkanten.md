---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/creator]
aliases: [Sprachkürzel mit Leerraum, Store-Faltung Restkanten]
status: abgenommen
prio: P4
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf M, Teil 1 (docs/pm-sitzung-2026-11-02-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-355 · Restkanten der Store-Faltung: Sprachkürzel mit Leerraum, zwei gleiche Paare in einem Aufruf

Drei 🟢 der Nachschau über `ffbb0b6` (B-346/B-347/B-349), keiner davon eine Entgleitung:

1. **Sprachkürzel sind freie Strings.** `VocabularyStoreService.FindManyAsync` (`:79-82`) filtert die Sprachen per SQL
   `trim(lower())`; SQLite-`trim()` entfernt nur Leerzeichen, `lower()` nur ASCII. Ein Eintrag mit `sourceLanguage = "en\t"`
   ist für `"en"` kein Kandidat mehr (vorher über den Schlüssel erreichbar). Kein Test speichert eine Sprache mit Rand –
   die Mutante „gespeicherte Seite ohne `Trim()`" bliebe grün. Weg: Sprachkürzel beim Anlegen/`PATCH` normalisieren
   (dann Vertragsfrage) oder eine Testzeile `" en "`.
2. **Zwei Paare gleicher Identität in einem `GetOrCreateManyAsync`-Aufruf** (`:49`, `found[identity] = vocab`) trägt kein
   Test; die Vokabel-POST lehnt solche Payloads vorher ab, aber `NormalizeConfigAsync` der Übersetzungsübung
   (`ExerciseControllers.cs:431`) nicht – ohne die Zeile entstünden still zwei Einträge (`key`, `key_2`). Die Zeile ist
   richtig und älter.
3. **Hinweis, keine Lücke:** `VocabKey.Slug` entfernt jedes `NonSpacingMark`, also auch japanische Dakuten (が→か) und
   hebräische/arabische Vokalzeichen. Seit B-347 erreicht die Faltung mehr Einträge. Erst relevant, wenn nichtlateinische
   Sprachen dazukommen.

## User Story

Als **Creator**, der eine Übersetzungsübung mit Wörtern füllt, möchte ich, dass jedes Wort genau einmal im Vokabelspeicher landet –
auch wenn es zweimal in derselben Übung steht oder ein älterer Eintrag sein Sprachkürzel mit Leerzeichen trägt.

## Ist-Stand am Code (Stand `cc789ed`)

Wie oben beschrieben; nachgelesen: `VocabularyStoreService.cs:49` (`found[identity] = vocab`) und `:79-82` (SQL-Filter
`Trim().ToLower()` auf beiden Seiten). Beide Zeilen sind richtig, nur ungetestet.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Punkt 1 als Testzeile, nicht als Normalisierung beim Schreiben** – die Normalisierung wäre eine Vertragsänderung (ein
   gespeicherter Wert sähe anders aus als geschickt). Festgenagelt werden Leerzeichen; Tab o. Ä. bleibt ausdrücklich ungedeckt
   (SQLite-`trim()` kennt nur Leerzeichen, und kein Schreiber legt ein Tab in ein Sprachkürzel).
2. **Punkt 2 als Service-Test** im Muster von `GetOrCreate_VorDemSpeichern_FindetDenEbenAngelegtenEintrag`.
3. **Punkt 3 bleibt Hinweis** – kein Defekt, solange nur lateinische Sprachen gepflegt werden.

## Akzeptanzkriterien

1. Ein Eintrag, gespeichert mit `" en "`/`" de "`, ist für `reuseExisting` mit `en`/`de` ein Treffer (200, dieselbe Id).
2. Zwei Paare gleicher Identität in einem `GetOrCreateManyAsync`-Aufruf liefern dieselbe Instanz und genau eine neue Zeile.

## Schätzung

`XS`, nur Tests. Testweg: `VocabularyStoreTests`.

## Verlauf

- **2026-11-02** — angelegt aus der Nachschau zu Lauf M, Teil 1 (`pugling-reviewer`, 🟢 ×3). Aus Code gelesen.
- **2026-11-04** — Lauf S, Teil 2: ausformuliert, gegrillt (Freigabe 1), geschätzt, gebaut. AK 1 →
  `ReuseExisting_FaltetDieGespeicherteSpracheMitLeerraum` (`VocabularyStoreTests.cs`), Mutante „gespeicherte Sprache ohne
  `Trim()`" (SQL-Filter) → **1 failed**. AK 2 → `GetOrCreateMany_ZweiGleichePaareImSelbenAufruf_LegenEinenEintragAn`, Mutante
  „`found[identity] = vocab` gestrichen" → **1 failed**. Beide Tests im Ausgangszustand grün (der Code war richtig, es fehlte die
  Zusicherung); die Mutanten per Skript gesetzt und byte-gleich zurückgespielt.
- **2026-11-04** — Review `pugling-reviewer`: kein 🔴/🟡. Ein 🟢 hier, ohne Zählung behoben: die Zeilennummer von `found[identity] = vocab` ist `:49`, nicht `:46`. Zum Test vom Prüfer nachgerechnet: `" en "` bleibt beim POST unverändert gespeichert, die Mutante scheitert an der Status-Zusicherung (201 statt 200); der Service-Test läuft in eigenem Scope, die Mutante scheitert an `Assert.Same`. Ein 🟢 am Test, ohne Zählung behoben: der PATCH vor dem Wiederfinden trug nichts (`reuseExisting` ohne `key` sucht nur über den Inhalt) – entfernt, Mutante „gespeicherte Sprache ohne `Trim()`" danach erneut **1 failed**.
- **2026-11-04** — Abnahme Lauf S, Teil 2: volle Suite **1071/1071**, Build ohne Warnung, Vitest **638/638**, E2E-Gesamtlauf **109 passed** (`exit=0`, nach dem letzten Code-Edit). Rollengang: der Entwickler ist die Rolle (jede richtige Zeile trägt jetzt eine Zusicherung, Mutanten je rot); Creator maschinell über die beiden Store-Fälle im echten HTTP-Pfad; Vater und Sohn Regressionszeugen. **Abgenommen**, Commit siehe Protokoll ([Lauf S](../pm-sitzung-2026-11-04-nacht.md)).
