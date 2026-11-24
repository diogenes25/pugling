---
tags: [typ/story, status/abgenommen, bereich/klassenarbeit, rolle/student]
aliases: []
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: "backend"
migration: "nein"
vertragsbruch: "nein"
quelle: "Nachschau Sprint 133 in der Retro von Sprint 135 (docs/pm-sitzung-2026-11-24.md)"
grund: ""
ersetzt_durch: []
entgangen_bei: [B-751]
wartet_auf: ""
nachgeschaut: "2026-11-24"
---

# B-945 · Die Bereitschaft „x von y Wörtern sicher“ zählt ein Wort doppelt, das in zwei Übungen des Bündels steht

`ChildClassTestsController.Readiness` (Commit 65f4d2a7) zählt als Nenner alle `ExerciseItem`s der Vokabel-Übungen im Bündel und als
Zähler die `ItemProgress`-Zeilen je `ItemId`. Die Arcade-Karte beschriftet das als „x von y **Wörtern** sicher“ (`classTest.secureWords`).
Steht dieselbe Store-Vokabel in zwei Übungen des Bündels (Unit-Übung plus Wiederholung, oder Tag-Übung neben direkter), zählt sie im
Nenner zweimal, und ihr Lernstand je Item kann sich unterscheiden: das Kind sieht mehr Wörter, als die Arbeit hat. AK 2 von B-751
spricht von „Items“, die Oberfläche von „Wörtern“ – die Abnahme prüfte gegen den AK, nicht gegen den Satz, den das Kind liest.
Unverifiziert: ob echte Bündel überlappen, ist an der echten DB nicht gemessen. Vorschlag zum Prüfen: nach `VocabularyId` falten
(sicher = sicher in mindestens bzw. in allen Items des Worts – zu entscheiden) oder die Beschriftung auf „Karten“ ändern.

## User Story

Als Sohn möchte ich auf der Klassenarbeits-Karte so viele Wörter sehen, wie die Arbeit wirklich hat, damit „x von y Wörtern
sicher“ stimmt.

## Ist-Stand am Code (2026-11-24, `cfebfe60`)

- Nenner und Zähler zählen `ExerciseItem`s bzw. `ItemProgress`-Zeilen
  (`backend/Pugling.Api/Controllers/Student/ChildClassTestsController.cs:66-71`); die Karte beschriftet „Wörter“
  (`frontend/src/sohn/KlassenarbeitKarte.tsx:62`).
- Gemessen an einer Kopie der echten DB (2026-11-24): In keinem der sechs Bündel steht ein Wort doppelt (Items = verschiedene
  Wörter in allen sechs). Heute wirkt der Fehler also nicht. Er entsteht, sobald eine Wiederholung oder eine Tag-Übung dasselbe
  Wort trägt.
- Zwei bestehende Tests bauten zufällig genau diesen Fall: Beide Übungen nutzten die Vorgabewörter „hello/goodbye“.

## Die echte Lücke

Die Zählung kannte nur Zeilen, die Oberfläche spricht von Wörtern.

## Entscheidungen

1. **Nutzer, 2026-11-24:** Der Nenner zählt verschiedene Wörter (`VocabularyId`). Ein Wort ist sicher, wenn der Mittelwert seiner
   Beherrschung über die Bündel-Zeilen mindestens `WeakBelowPercent` erreicht. Eine ungeübte Zeile zählt als 0. Verworfen wurden
   „sicher in allen Zeilen“ (zu streng: eine ungeübte Wiederholung macht ein gekonntes Wort unsicher), „in mindestens einer“
   (eine verlernte Zeile bleibt unsichtbar) und „nur umbeschriften“.
2. Die Feldnamen `TotalItems`/`SecureItems` bleiben, damit der Vertrag nicht bricht. Die Doku im Vertrag sagt jetzt „Wörter“.

## Akzeptanzkriterien

1. Ein Wort in zwei Bündel-Übungen zählt im Nenner einmal.
2. Gekonnt in A (100) und ungeübt in B ergibt einen Schnitt von 50: sicher.
3. 60 in A und 10 in B ergeben 35: unsicher.
4. 80 in A und ungeübt in B ergeben 40: unsicher. Die ungeübte Zeile verschwindet nicht.
5. Die Schwelle bleibt inklusiv (genau 50 ist sicher).

## Angriffsplan und Testweg

Die Zählung im Endpunkt nach `VocabularyId` falten. Drei neue Fälle in `KlassenarbeitBereitschaftTests`, die zwei alten auf
eigene Wörter umstellen. Rollengang über `e2e/klassenarbeit-countdown.spec.ts`.

## Schätzung

- **Größe:** XS – eine Faltung im Endpunkt, keine Migration, kein Vertragsbruch.
- **Testweg:** drei neue Fälle in `KlassenarbeitBereitschaftTests`, Mutanten je AK.

## Verlauf

- **2026-11-24** — angelegt in der Nachschau der Retro von Sprint 135, nicht behoben.
- **2026-11-24** — verifiziert (Code und DB-Kopie), mit dem Nutzer entschieden, in Sprint 137 gebaut.
- AK 1, Wort in zwei Übungen → `KlassenarbeitBereitschaftTests.Wort_In_Zwei_Uebungen_Zaehlt_Einmal…`, Mutante B945-1
  (`GroupBy(i => i.Id)`) → rot.
- AK 2, Schnitt statt „in allen“ → derselbe Test, Mutante B945-2 (`Min`) → rot.
- AK 3, Schnitt statt „in einer“ → `Wort_Ist_Erst_Sicher_Wenn_Sein_Schnitt…`, Mutante B945-3 (`Max`) → rot.
- AK 4, ungeübt = 0 → `Ungeuebtes_Item_Zaehlt_Im_Schnitt_Als_Null…`, Mutante B945-4 (nur geübte Zeilen mitteln) → rot.
- AK 5, Schwelle inklusiv → `Anteil_Zaehlt_Nicht_Schwache…`, Mutante B945-5 (`>`) → rot.

| Mutante | Stelle | erwartet | gemessen |
|---|---|---|---|
| B945-1 Wort statt Item | backend/Pugling.Api/Controllers/Student/ChildClassTestsController.cs:78 | red | red |
| B945-2 Schnitt statt alle | backend/Pugling.Api/Controllers/Student/ChildClassTestsController.cs:79 | red | red |
| B945-3 Schnitt statt eines | backend/Pugling.Api/Controllers/Student/ChildClassTestsController.cs:79 | red | red |
| B945-4 ungeuebt = 0 | backend/Pugling.Api/Controllers/Student/ChildClassTestsController.cs:79 | red | red |
| B945-5 Schwelle inklusiv | backend/Pugling.Api/Controllers/Student/ChildClassTestsController.cs:82 | red | red |

Gemessen mit `mutant-probe.py` an `cfebfe60 + Arbeitsbaum`, je Zeile am genannten Test (`fails`); die Grundlinie nach dem
Zurückspielen war grün. Die Zeilen 78–82 hat die spätere Kommentar-Korrektur nicht verschoben.
- **2026-11-24** — abgenommen in Sprint 137, Commit `262bd5ae`. Volle Suite 2315/2315 grün (Release), Build ohne Warnung.
  `pugling-reviewer`: für B-945 kein Fund, für B-946 ein 🟡 (eigene Rechnung statt `PoolSize`, Deckel-Fall ungetestet), beide
  eingearbeitet. Rollengang: `klassenarbeit-countdown.spec.ts` und `klassenarbeit-vorbereitung.spec.ts` gegen einen frisch
  gestarteten Server auf Ausweichports (5201/5174, Release), 2 passed. Die Specs fahren die Anzeige, nicht den Überlappungs- bzw.
  Deckelfall; den tragen die Integrationstests.
- **2026-11-24** — nachgeschaut in der Retro von Sprint 138 (`262bd5ae`): ohne Befund.
