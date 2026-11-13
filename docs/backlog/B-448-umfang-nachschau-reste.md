---
tags: [typ/story, status/abgenommen, bereich/katalog, bereich/tests, rolle/supervisor]
aliases: [Umfang Nachschau-Reste]
status: abgenommen
prio: P3
art: Defekt
groesse: "S"
wo: beides
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-09-nacht.md
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-424]
nachgeschaut: 2026-11-13
---

# B-448 · Umfang der Übung: „leer" ist am Server ungetestet, ein Zählfehler bleibt stumm, eine Summary hängt am falschen Helfer

Gefunden in der Nachschau zu Sprint 26 (Commit `453a740`, Lauf AG, frischer `pugling-reviewer`), drei 🟡:

1. **AK 4 („leer") hat am Backend keinen Grenzfall-Test.** Die Mutanten `itemTableRows is 0 ? null : itemTableRows`
   (`VocabularyExerciseType.cs:41`) und `ItemsOf(..).Count is 0 ? null : …` (`ExerciseTypeBase.cs:37`) überleben alle 1178 Tests –
   „leer" erschiene nie mehr. Fix: eine Vokabelübung anlegen, ihre Items löschen, `itemCount == 0` prüfen.
2. **`catch (Exception)` in `ExerciseTypeBase.ItemCount` schluckt ohne Log.** Wirft ein Typ in `ItemsOf` immer (Regression), sieht jede
   seiner Zeilen aus wie ein Aufsatz, und niemand erfährt es. Fix: den Fang in den Controller-Helfer ziehen und mit `ExerciseId`/`Type`
   loggen; der `when (not OperationCanceled)`-Filter ist bei synchronem `ItemsOf` Ballast.
3. **Die `/// <summary>` von `ApplySort` steht jetzt über `ItemCountOf`** (`ExerciseCatalogController.cs:141-149`): der Helfer wurde
   zwischen Doku und Methode geschoben.

Kleiner (🟢): „20 von 100 Karten" wäre ehrlicher, wo `DefaultItemCount` die Runde begrenzt; der Vitest-Kommentar „sagt nichts zum
Umfang" verspricht ein „· 0", das seine Regex nicht fängt; `wiki/08-erweitern.md` sollte sagen, dass ein Typ mit Atomen außerhalb der
Config `ItemCount` überschreiben muss.

## Entscheidungen

Autonom gegrillt (`art: Defekt`, Tageslauf):

1. **Punkt 1:** „leer" bekommt einen Grenzfall-Test am Server – je eine Vokabelübung ohne Item-Zeilen und eine Lese-Übung ohne
   Fragen (direkt in die DB, wie der Test für kaputte Configs) zählen `0`, nicht `null`.
2. **Punkt 2:** der Fang zieht aus `ExerciseTypeBase.ItemCount` in den Controller-Helfer `ItemCountOf` und **loggt** mit
   Übungs-Id und Typ (Warnung, deutsche Laufzeit-Diagnose); der `when (not OperationCanceled)`-Filter entfällt, `ItemsOf`
   ist synchron. Die Typ-Methode darf jetzt werfen – ihr einziger Aufrufer fängt (per `grep` geprüft). Doku an
   `IExerciseType.ItemCount` nachgezogen, inklusive „ein Typ mit Atomen außerhalb der Config überschreibt dies" (🟢).
3. **Punkt 3:** die `/// <summary>` steht wieder über `ApplySort`.
4. 🟢 „20 von 100 Karten" und der Vitest-Kommentar bleiben offen.

## Akzeptanzkriterien

1. Leere Vokabel- und Lese-Übung zeigen `itemCount: 0`.
2. Eine Config mit `null`-Element kostet nur ihre Zeile (Bestandstest, jetzt am neuen Ort).

## Verlauf

- **2026-11-09** — angelegt aus der Nachschau zu Sprint 26 (Retro Sprint 27, Lauf AG). Increment des vorigen Laufs (AF) – kein Limit 2
  (Präzedenz Lauf AD).
- **2026-11-13** — ausformuliert, autonom gegrillt (Defekt), geschätzt S (Tageslauf, Sprint 53).
- **2026-11-13** — gebaut (Sprint 53).
  - AK 1 → `ExerciseItemCountTests.LeereUebung_ZaehltNull` (Vocabulary, Reading); U1 (`ItemsOf(..).Count is 0 ? null`) → rot,
    U2 (Vokabel `itemTableRows is 0 ? null`) → rot.
  - AK 2 → `KaputteConfig_KostetNurDenUmfangDieserZeile` (Bestand, jetzt am Controller-Fang); U3 (Fang nur `JsonException`)
    → rot. Das Loggen selbst trägt kein Test (🟢 des Reviews, bewusst – Warnung, kein Verhalten).
  - Mutanten-Probe nach der letzten Änderung (Release, gefiltert) 3/3 rot; volle Suite 1436/1436.
  - Review (`pugling-reviewer`): Logger in der Projektion wie `registry` (Feld, kein Instanzaufruf); einziger Aufrufer ist
    der Controller, einzige Überschreibung fängt `JsonException` selbst. Drei 🟢, eines behoben (Leerzeile vor `ApplySort`).
  - Rollengang Creator: Katalogsuche gegen den echten In-Process-Server; Vater/Sohn Regressionszeugen. Abgenommen.
- **2026-11-13** — nachgeschaut (Retro Sprint 54/55, frischer `pugling-reviewer` an `14be0fa`, gelesen). Geprüft: einziger
  Aufrufer von `ItemCount` (`ExerciseCatalogController.cs:138`), einzige Überschreibung fängt `JsonException` selbst;
  `ItemsOf`-Nutzer unverändert; `umfangText` liest nur `null` als „kein Umfang"; beide AK-Tests gegen die Mutanten
  nachvollzogen. Kein Befund (zwei 🟢 zum Loggen, bewusst).
