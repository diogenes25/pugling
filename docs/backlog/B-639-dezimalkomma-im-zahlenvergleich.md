---
tags: [typ/story, status/abgenommen]
aliases: []
status: abgenommen
prio: P2
art: Defekt
groesse: M
wo: backend
migration: nein
vertragsbruch: nein
quelle: "Chat mit dem Vater, 2026-11-17 (Mathe in die App; Material raw/Math/lambacher-schweizer-9-sh-pugling/md)"
entgangen_bei: []
nachgeschaut: "2026-11-21"
grund: ""
ersetzt_durch: []
---

# B-639 · Dezimalkomma im Zahlenvergleich der Rechenaufgaben

Eine Rechenaufgabe mit der Lösung 2,5 wertet die Antwort „2,5“ als falsch – auf beiden Wertungspfaden, aus zwei
verschiedenen Gründen. Fällt heute nicht auf, weil die Seed-Rechenaufgaben ganzzahlig sind
(`backend/Pugling.Api/Data/Seed.cs:1292`); mit Mathe Klasse 9 (gerundete Lösungen wie −0,4 und 2,3) wird es zur Regel.

## User Story

Als Schüler möchte ich eine Dezimalzahl so eintippen, wie ich sie in der Schule schreibe (mit Komma), damit eine
richtige Rechnung nicht an der Schreibweise scheitert.

## Ist-Stand am Code

Zwei Pfade werten eine Zahlenantwort, und sie tun es verschieden:

1. **Katalog-Check** (`Check`, Testmodus des Vaters und Rechen-Drill): `AnswerChecking.NumericMatch`
   (`backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs:669`) parst mit `NumberStyles.Number` und
   `CultureInfo.InvariantCulture`. Dort ist das Komma ein Tausendertrenner. Nachgestellt am 2026-11-17 mit genau
   diesem Aufruf: „2,5“ → 25, „0,6“ → 6, „−0,4“ → −4 – die Eingabe wird nicht abgelehnt, sondern **als andere Zahl
   gelesen**. Aufrufer: `ArithmeticExerciseType.Check` (Zeile 484) und `CheckGenerated` (Zeile 694).
2. **Spielen in einer Position** (Üben/Test): `ArithmeticExerciseType.ItemsOf` legt die Lösung als Text in
   Invariant-Schreibweise ab (`BuiltInExerciseTypes.cs:471`, also „2.5“), und gewertet wird über
   `AnswerGrader.Matches` (`backend/Pugling.Api/Services/Shared/AnswerGrader.cs:26`) per **Textvergleich** nach
   `StageMechanics.Normalize`. „2,5“ ≠ „2.5“. Am Code gelesen, nicht per Test nachgestellt.

Im Frontend wandelt nichts ein Komma um (keine Fundstelle unter `frontend/src`).

## Die echte Lücke

Breiter als die Idee vermutete – das Komma ist nur das sichtbarste Symptom:

- Pfad 1 liest das Komma als Tausendertrenner und vergleicht gegen eine falsche Zahl.
- Pfad 2 vergleicht gar nicht numerisch. Damit scheitert dort nicht nur „2,5“, sondern auch „2.50“ oder „+3“, und
  die **`Tolerance` der Aufgabe wirkt beim Spielen nicht** – sie gilt nur im Katalog-Check. Eine Aufgabe, die im
  Testmodus des Vaters als richtig durchgeht, kann beim Kind falsch sein.

## Offene Punkte

1. ~~**Was ist „1.234“?**~~ → Entscheidung 1.
2. ~~**Ein numerischer Vergleich für beide Pfade?**~~ → Entscheidung 2.
3. ~~**Gehört die Toleranz beim Spielen dazu oder in eine eigene Story?**~~ Gegenstandslos: sie folgt aus
   Entscheidung 2 – dieselbe Regel auf beiden Pfaden schließt die Toleranz ein.

## Entscheidungen

1. **Punkt und Komma sind beide Dezimaltrenner; Tausendertrenner gibt es nicht.** „1.234“ ist 1,234. Begründung:
   Schulaufgaben kommen ohne Tausendertrenner aus, und jede Mehrdeutigkeit entfällt – auch die vom Taschenrechner
   abgetippte „2.5“ stimmt. Kosten: wer „1.000“ für tausend tippt, liegt falsch.
2. **Eine Zahlenantwort wird auf beiden Pfaden numerisch verglichen, nach einer Regel, die der Übungstyp vorgibt.**
   Begründung: was im Testmodus des Vaters richtig ist, muss beim Kind richtig sein; „2.50“, „+3“ und „2,5“ sind
   dieselbe Zahl. Folge: die `Tolerance` der Aufgabe wirkt auch beim Spielen. Kosten: Eingriff in den geteilten
   `AnswerGrader`, den alle Typen nutzen – größere Änderung als eine Komma-Ersetzung, und die Regressionstests der
   Text-Typen müssen unverändert grün bleiben.

## Akzeptanzkriterien

1. Eine Rechenaufgabe mit Lösung 2,5 wertet „2,5“, „2.5“ und „2.50“ als richtig – im Katalog-Check und beim Spielen
   in einer Position.
2. „25“ ist für dieselbe Aufgabe auf beiden Pfaden falsch (die rote Probe: heute ist „2,5“ im Katalog-Check genau
   diese 25).
3. Eine Aufgabe mit Lösung 1,234 wertet „1.234“ als richtig und „1234“ als falsch.
4. Eine Aufgabe mit Toleranz 0,05 und Lösung 2,33 wertet „2,3“ beim Spielen als richtig, „2,2“ als falsch.
5. Eine Aufgabe mit Lösung 3 wertet „+3“ als richtig; eine leere Eingabe und „drei“ bleiben falsch.
6. Der Rechen-Drill wertet eine gerundete Lösung mit Komma wie mit Punkt.
7. Die Text-Typen (Vokabel, Lückentext, Liste) vergleichen unverändert – „2,5“ als Vokabel-Antwort wird nicht zur Zahl.

## Schätzung

**Größe: M** — wie der Batch-Pfad im `MediaSelector`: ein Eingriff an einer geteilten Stelle (`AnswerGrader`), ein
gemeinsamer Zahlen-Parser, kein Schema, kein neues Vertragsfeld.

- **`migration: nein`** — die Lösung steht weiter als `decimal` im `ConfigJson`; nichts am Schema.
- **`vertragsbruch: nein`** — das Atom `ContentItem` ist intern (`backend/Pugling.Api/Services/Shared/ExerciseContentProvider.cs:43`),
  in `Pugling.Contracts` kommt es nicht vor. Ein Zahlen-Kennzeichen daran ändert keinen Vertrag.
- **`wo: backend`** — die Sohn-App schickt die Eingabe als Text; es gibt dort kein Zahlenfeld, das ein Komma
  verschluckt (`type="number"` nur im Shop, `frontend/src/sohn/SohnShop.tsx:297`).

### Risiken

- **Alle Spielpfade laufen durch `AnswerGrader.Matches(given, item)`** – Üben
  (`PositionPracticeController.cs:484`, `:669`), Test (`PositionTestsController.cs:288`, `:402`), Vorschau
  (`ExercisePreviewService.cs:94`), Mengen-Zuordnung (`PositionPlayService.cs:379`). Das ist der Hebel (eine Stelle
  genügt) und die Gefahr (ein Fehler trifft jeden Typ). AK 7 schützt die Text-Typen.
- **`IsMisspelled` misst Buchstabenabstand** (`AnswerGrader.cs:47`): „2,4“ statt „2,5“ gälte als „richtig, nur
  verschrieben“. Für Zahlen-Atome muss das ausgeschaltet sein – eine falsche Zahl ist kein Tippfehler.
- **Minuszeichen:** eine Tablet-Tastatur kann das typografische „−“ (U+2212) liefern. Der Parser nimmt es an, wie
  `Normalize` es für den Apostroph tut (B-440). Kein eigenes Akzeptanzkriterium, aber ein Testfall.
- **Nicht enthalten:** eine Zifferntastatur in der Sohn-App (`inputMode="decimal"`). Zeigt der Rollengang, dass sie
  fehlt, wird das eine eigene Frontend-Story.

### Angriffsplan

1. Ein Zahlen-Parser an einer Stelle (Punkt und Komma dezimal, kein Tausendertrenner, Vorzeichen, U+2212);
   `NumericMatch` nutzt ihn – damit sind Katalog-Check und Rechen-Drill erledigt (AK 1–3, 5, 6 für Pfad 1).
2. `ContentItem` bekommt die Zahl und ihre Toleranz als optionales Kennzeichen; `ArithmeticExerciseType.ItemsOf`
   setzt es.
3. `AnswerGrader.Matches(given, item)` vergleicht ein Zahlen-Atom über denselben Parser; `IsMisspelled` gibt für
   Zahlen-Atome `false` zurück.
4. `dotnet build Pugling.sln` und `dotnet format Pugling.sln` vor dem Commit.

### Testweg

- `AnswerGraderTests` – Theorie über die Schreibweisen („2,5“, „2.5“, „2.50“, „+3“, „−0,4“, „1.234“, „25“, leer,
  „drei“) gegen ein Zahlen-Atom, dazu Toleranz-Grenzfälle und `IsMisspelled`. Rote Probe: jeder Fall fällt heute
  einzeln, außer „2.5“ und „25“ – die beiden sind als Absichtserklärung zu beschriften.
- `ExerciseCheckEndpointTests` – Katalog-Check mit Komma (AK 1, 2) und Rechen-Drill (AK 6).
- `PositionPracticeFlowTests` und `PositionTestFlowTests` – eine Rechen-Position mit Lösung 2,5 und mit Toleranz
  wird mit Komma gespielt (AK 1, 4); Aufbau über `TestApi.CreateArithmeticExerciseAsync`.
- AK 7: die vorhandenen Vokabel-, Lückentext- und Listen-Tests bleiben unverändert grün; dazu ein Fall, der „2,5“
  als Vokabel-Antwort gegen die Lösung „2.5“ weiter ablehnt.
- Vor der Abnahme: Agent `pugling-reviewer` und der Rollengang als Sohn an einer Rechen-Position.

## Verlauf

- **2026-11-17** — angelegt (Quelle: Konzept „Mathe in die App" aus dem Chat, Lambacher Schweizer 9, Kapitel IV).
- **2026-11-17** — ausformuliert: Parse-Verhalten nachgestellt („2,5“ → 25), zweiter Pfad über `AnswerGrader` am Code
  gelesen; Lücke ist breiter (Textvergleich und wirkungslose Toleranz beim Spielen).
- **2026-11-17** — gegrillt (Dialog): beide Trennzeichen dezimal, kein Tausendertrenner; ein numerischer Vergleich
  für beide Pfade, Toleranz damit auch beim Spielen. Akzeptanzkriterien final (7).
- **2026-11-17** — geschaetzt: M, backend, keine Migration, kein Vertragsbruch; ein Eingriff im `AnswerGrader`, der alle
  Spielpfade trägt.
- **2026-11-21** — in-arbeit (Nachtlauf 2026-11-21, Sprint 104). Gebaut nach Angriffsplan: `NumericAnswer`
  (`Services/Shared/NumericAnswer.cs`) als einziger Zahlen-Parser, `ContentItem.Numeric` (`NumericTarget`), gesetzt in
  `ArithmeticExerciseType.ItemsOf`; `AnswerGrader.Matches` vergleicht Zahlen-Atome numerisch mit Toleranz,
  `IsMisspelled` gibt für sie `false`; `AnswerChecking.NumericMatch` (Katalog-Check, Drill) nutzt denselben Parser.
  **Rote Probe** (`ZahlenantwortTests`, vor dem Fix): 14 von 28 Fällen rot – u. a. „2,5“ für 2,5 auf beiden Pfaden
  (der Check-Pfad las 25), „2,3“ bei Toleranz 0,05 beim Spielen, „3,33“ im Drill, „2,4“ als Tippfehler für 2,5.
  Je AK-Fall Test und Mutante:
  - AK 1 (Komma/Punkt/2.50, beide Pfade) → `Dieselbe_Zahl_Zaehlt_Auf_Beiden_Pfaden`; P2 (Komma nicht ersetzt) → rot.
    Spielen in der Position: `PositionTestFlowTests.Test_Rechenaufgabe_Mit_Komma_Und_Toleranz` (erwartet 2 von 3); P1
    (`Numeric` in `ItemsOf` weg) → rot.
  - AK 2 („2,5“ ist nicht 25) → derselbe Integrationsfall (dritte Aufgabe 25 mit „2,5“) und „2,5“ in der Theorie; P3
    (Komma als Tausender gelesen) → rot. „25“ für 2,5 in `Andere_Oder_Keine_Zahl_Ist_Falsch` ist Absichtserklärung.
  - AK 3 (1.234) → „1,234“ in der ersten Theorie (P2 → rot); „1234“ ist Absichtserklärung.
  - AK 4 (Toleranz beim Spielen) → `Toleranz_Gilt_Beim_Spielen`; P1, P5 (Grenze exklusiv) → rot.
  - AK 5 (+3, leer, „drei“) → „+3“, P8 (Vorzeichen weg) → rot; leer und „drei“ sind Absichtserklärung (vorher schon falsch).
  - AK 6 (Drill) → `Drill_Wertet_Komma_Wie_Punkt`; P2 → rot („3.33“ ist Absichtserklärung).
  - AK 7 (Text-Typen) → `Text_Atom_Bleibt_Textvergleich`; P7 (Zahl-Regel für jedes Atom) → rot.
  - Risiko Minuszeichen → „−0,4“, P4 (U+2212 weg) → rot. Tippfehler → `Falsche_Zahl_Ist_Kein_Tippfehler`, P6 → rot.
  - Aus dem Review (🟢, ohne Zählung): `Swap` setzt `Numeric = null` → `Vertauschte_Karte_Ist_Kein_Zahlen_Atom`, P10 → rot;
    der Schlusspunkt „2,5.“ wird wie früher beim Textvergleich toleriert → P11 → rot; Rand-Leerraum über `Trim()` → P9 → rot.
  - **Abweichung vom Testweg:** statt `ExerciseCheckEndpointTests`/`PositionPracticeFlowTests` prüft ein Unit-Test beide
    Pfade direkt (`Check` und `AnswerGrader.Matches` auf dem Atom aus `ItemsOf`), dazu ein Integrationsfall über die
    Test-Route. Üben läuft durch dieselbe `Matches`-Zeile (`PositionPracticeController.cs:643/:858`, vom Review gelesen).

  Gemessen nach der letzten Änderung an `ed922687 + Arbeitsbaum geändert` mit `mutant-probe.py` (Debug), Grundlinie
  danach grün:

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | P1 | `BuiltInExerciseTypes.cs:483` ohne `Numeric` | red | red |
  | P2 | `NumericAnswer.cs:21` Komma nicht ersetzt | red | red |
  | P3 | `:21` Komma entfernt (= Tausender) | red | red |
  | P4 | `:21` U+2212 nicht ersetzt | red | red |
  | P5 | `:27` Toleranz exklusiv | red | red |
  | P6 | `AnswerGrader.cs:54` Tippfehler-Sperre weg | red | red |
  | P7 | `AnswerGrader.cs:19` Zahl-Regel für jedes Atom | red | red |
  | P8 | `NumericAnswer.cs:12` ohne Vorzeichen | red | red |
  | P9 | `:21` ohne `Trim()` | red | red |
  | P10 | `ExerciseContentProvider.cs:139` `Swap` behält `Numeric` | red | red |
  | P11 | `:21` ohne Schlusspunkt-Toleranz | red | red |
  | P1e | `BuiltInExerciseTypes.cs:483`, E2E `rechnen-mit-komma` | red | red |

  Review (`pugling-reviewer`): kein 🔴. Zwei 🟡, beide falsch beschriftete Testkommentare (benannte rote Proben, die
  keine waren) → Kommentare korrigiert, **ohne Zählung** (nur Kommentar). Vier 🟢: drei übernommen (oben), einer nur
  vermerkt (der Gedankenstrich „–“ als Minus wird nicht gelesen – erst, wenn ein Tablet ihn wirklich schickt).
  **Zähler 0 von 5.**
- **2026-11-21** — abgenommen in Commit `7ff47fde` (Sprint 104). Backend 1978/1978 (Release).
  **Rollengang Sohn:** neue Spec `rechnen-mit-komma` gegen die frisch gestartete App (5201/5174): der Sohn tippt im
  Tagestest „2,5“, „2,3“, „2,5“ und sieht „2 / 3“ – 1 passed, mit P1 rot. `full-flow` grün.
  Nicht enthalten, wie geplant: eine Zifferntastatur (`inputMode="decimal"`) in der Arcade. Das Textfeld nimmt das Komma
  an; ob die Handy-Tastatur es bequem anbietet, ist eine offene Geräte-Prüfung.
- **2026-11-21** — nachgeschaut (Nachschau Sprint 104 in der Retro von Sprint 105), kein Befund. Geprüft: der Gang lief
  nur über den Test – das Üben nutzt dieselbe Eingabekomponente (`Lektion`, Feld „Deine Antwort“), und in den
  Spielflächen der Arcade gibt es kein `type="number"`, das ein Komma verschlucken könnte (gegrept: nur
  `SohnLogin.tsx:73` `inputMode="numeric"` und `SohnShop.tsx:300`).
