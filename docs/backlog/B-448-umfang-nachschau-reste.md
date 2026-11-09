---
tags: [typ/story, status/idee, bereich/katalog, bereich/tests, rolle/supervisor]
aliases: [Umfang Nachschau-Reste]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: beides
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-09-nacht.md
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-424]
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

## Verlauf

- **2026-11-09** — angelegt aus der Nachschau zu Sprint 26 (Retro Sprint 27, Lauf AG). Increment des vorigen Laufs (AF) – kein Limit 2
  (Präzedenz Lauf AD).
