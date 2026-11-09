---
tags: [typ/protokoll, bereich/pm]
aliases: [Lauf AF, Lauf AG, Nachtlauf 2026-11-09]
---

# PM-Sitzung: Läufe AF und AG – Nachschau-Defekte, Zuweisen mit offenen Augen

**Datum:** 2026-11-09  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Zwei unbeaufsichtigte Läufe nach `pm-loop`, [docs/backlog/README.md](backlog/README.md) und den Freigaben aus
[nachtlauf.md](nachtlauf.md): zuerst die Nachschau-Defekte aus Lauf AE (B-439, B-436), dann Paket NP-26 aus dem
[Rollengang vom 2026-11-08](pm-sitzung-2026-11-08-rollengang.md), danach NP-27.

**Auftrag des Nutzers (wörtlich):** „Starte zwei Nachtläufe"

**Eingangsprüfung:** Nächster Start laut Lauf AE: B-439, B-436, dann NP-26. B-439 und B-436 sind `Defekt` auf `idee` (Freigabe 1,
autonom grillbar). NP-26: B-424 und B-423 stehen auf `geschaetzt` mit Freigabe-Zeile des Nutzers aus Commit `05700b6` (Dialog,
nicht aus einem Lauf) – Freigabe 9. **B-408** steht auf `idee` (`Wunsch`) **ohne** eigene Freigabe-Zeile; die Zeile in B-424
deckt keine Nachbarin → gesperrt, gebrauchte Entscheidung in B-408 notiert.

## Sprint 24 — Ziel & Umfang (Lauf AF)

**Sprint-Ziel:** Der Sohn sieht nach einem Tippfehler die Lösung neben seiner eigenen Antwort in der Karte – und ein Test
fällt, sobald sie dort fehlt; am Rundenende verliert er nie die Stelle.
**Umfang:** B-439 (mit seinen drei 🟢).
**Entwickler-Brief:** Nur Tests und ein Fokus-Zweig. Quelle der Wahrheit: `.card.mistake` in `SohnPractice.tsx`; Zusicherungen
auf die Karte begrenzen, `sr-only` der Region im Halt prüfen; Fokus beim Übergang nach `done` an die Überschrift. Testweg:
Vitest, `erstbegegnung.spec.ts`, `bilder.spec.ts`.

## Iteration 24 — umgesetzt

Vitest AK 1/AK 3 über `within(.card.mistake)` plus `toastRegion()`; E2E über `page.locator(".card.mistake")`. `bilder.spec.ts`
sucht über „Weiter →" (frische Position = Kennenlernkarten) und wartet je Klick auf den Kartenwechsel. Testname
`GemischteRunde_ZeigtErstDieNeuen_DannDasBekannte`. Rundenende: `role="heading"`, `tabIndex={-1}`, Fokus beim Übergang in
`done`, Fokusring `.vtitle[tabindex]:focus-visible`.

**Rote Probe mit Zahl:** Mutante „Lösung aus der Karte" gegen die Tests von `HEAD` → 15 von 15 grün (der Defekt), gegen die neuen
Tests rot; E2E dieselbe Mutante rot. Sechs Mutanten, alle rot am benannten Fall (Tabelle in B-439), nach den 🟢-Fixes neu gemessen.

## Runde 2 — Review (Sprint 24)

`frontend-reviewer`: keine 🔴, keine 🟡; drei 🟢 (Wettlauf in der `bilder.spec`-Schleife, Überschrift als `div`, ungestylter
Fokusring) – alle behoben, **ohne Zählung**. Zähler Sprint 24: 0.

## Runde 3 — Abnahme (Sprint 24)

- **Sohn:** Rollengang als E2E: `erstbegegnung.spec.ts` (frische Position, Tippfehler hält mit Lösung in der Karte) und
  `bilder.spec.ts` (Bildwechsel auf der Kennenlernkarte). Sprint-Ziel gehalten.
- **Vater, Creator:** Regressionszeugen – volle Suite grün, keine ihrer Flächen im Diff.
- **Ausgefallen:** Browser-Rollengang von Hand (Chrome nicht genutzt).
- Zahlen nach dem letzten Edit: Backend 1172/1172, Vitest 687/687, `tsc -b` sauber, Playwright **121 passed**.

B-439 → `abgenommen`.

## Retrospektive (Sprint 24)

**Nachschau:** Sprint 23 (`481c973`, B-421), frischer `pugling-reviewer`: kein 🔴, kein 🟡. Prüfpunkte (in B-421 notiert): die fünf
Grader-Aufrufstellen je gegen ihren Test, acht Mutanten-Zeilen gegen ihre Testzeile, die rote Probe „11/11" nachgezählt, vier
Grenzwörter am Code. 🟢 → [B-440](backlog/B-440-typografischer-apostroph-wird-nicht-gefaltet.md) (Apostroph),
[B-441](backlog/B-441-editor-liest-articlerequired-nicht-zurueck.md) (Editor-Rücklese), eine Zeilenangabe berichtigt. Increment
eines früheren Laufs, nur 🟢 → kein Limit 2.

**Was die eigenen Tore durchgelassen haben:** nichts von außen. Gekostet hat die Mutantenprobe drei Anläufe: `mutant-probe.py` ruft
die Befehle über `shell=True`, unter Windows also `cmd.exe` – `./node_modules/.bin/vitest` läuft dort nicht, und das Skript meldet
nur „Grundlinie nicht grün". Danach brach das JSON an einem unmaskierten Backslash.

**Vorschlag (nicht gelandet, Freigabe 3):**

- **(CI)** `mutant-probe.py` gibt bei roter Grundlinie die letzten Zeilen der Ausgabe mit aus, und sein Docstring zeigt die
  Windows-Form (`node_modules\.bin\vitest.cmd`). Ort: das Skript selbst. Mechanik statt Prosa: die Ausgabe hätte den Grund sofort
  gezeigt.

## Sprint 25 — Ziel & Umfang (Lauf AF)

**Sprint-Ziel:** Der Creator bekommt beim Löschen einer Reihe genau dann `unit_content_in_use`, wenn ein Ziel auf eine ihrer Units
zeigt – und nie wegen einer Etappe auf fremdem Inhalt; beides hält ein Test.
**Umfang:** B-436.
**Entwickler-Brief:** Nur Tests. Quelle der Wahrheit: `ExerciseUsageQueries.AnyBlockingAsync` mit dem Unit-Umfang aus
`TextbookSeriesController.Delete` bzw. `SeriesUnitsController.Delete`. Testweg: `CatalogManagementTests`, Mutanten je Prädikat.

## Iteration 25 — umgesetzt

`Delete_Unit_MitZielEtappeAufDerUnit_Liefert409` → Theory über `wholeSeries`; aus dem Review die Gegenrichtung
`Delete_UnitOderReihe_ZielEtappeAufEinerAnderenUnit_SperrtNicht` (Nachbar-Unit bzw. fremde Reihe → 204), gemeinsamer Helfer
`AddUnitMilestoneAsync`. Kommentar in `ExerciseUsageQueries.cs`, warum der Übungs-Zweig über die API nicht trennbar ist.
Drei Mutanten (Reihen-Umfang leer, Reihenfilter weg, Unit-Pfad auf die Reihe verbreitert), alle rot am benannten Fall.

## Runde 2 — Review (Sprint 25)

`pugling-reviewer`: keine 🔴; 🟡 zu breites Sperren in beiden Pfaden nur zufällig gedeckt (die Klassen-DB seedet keine Etappen) →
behoben (**Zähler 1**); 🟢 Übungs-Zweig nicht trennbar → Kommentar statt Test; 🟢 Fortbestand nach 409 → bewusst nicht.

## Runde 3 — Abnahme (Sprint 25)

- **Creator:** Integrationstests gegen die echte API als Ersatz-Rollengang (die Fläche ist das Löschen; `e2e/lehrwerke.spec.ts` fährt
  den UI-Weg der Reihen und bleibt grün).
- **Vater, Sohn:** Regressionszeugen – Suite grün, kein Pfad berührt.
- Zahlen: Backend 1172/1172, Playwright 121 passed (gemeinsamer Lauf mit Sprint 24, nach dem letzten Edit).

B-436 → `abgenommen`.

**Nebenbefund:** Der volle Testlauf am Montag schrieb `docs/api-examples/me.md` um (Wochenmission „NewWords" `current` 2 → 0) –
die Capture hängt am Wochentag → [B-442](backlog/B-442-capture-wochenmission-haengt-am-wochentag.md); Dateien zurückgesetzt,
außerhalb des Sprint-Diffs, nicht gezählt.

## Retrospektive (Sprint 25)

**Nachschau:** Sprint 24 (`62bb35d`, B-439), frischer `frontend-reviewer`: kein 🔴, kein 🟡. Prüfpunkte: sechs Mutanten-Zeilen gegen
Code und Testzeilen nachgerechnet, doppelte h1 gesucht, Eindeutigkeit der Test-Helfer. Fünf 🟢 →
[B-443](backlog/B-443-fokus-wege-der-sohn-runde.md) (darunter ein älterer Fokusverlust nach „Jetzt abfragen" aus B-419).
`nachgeschaut` in B-439 gesetzt. Increment dieses Laufs, aber ohne 🟡/🔴 – kein Limit 2; der Lauf geht mit NP-26 weiter.

**Was die eigenen Tore durchgelassen haben:** Der Review von B-436 fand die Gegenrichtung (zu breites Sperren) – meine Fall-Tabelle
hatte nur „sperrt, wo sie soll" und nicht „sperrt nicht, wo sie nicht soll". Die Klassen-DB seedet keine Ziel-Etappen; eine
Verbreiterung wäre nur je nach Testreihenfolge aufgefallen. Und die Nachschau zu B-439 fand dasselbe Muster: Entscheidung 2 sagt
„aus jeder Phase", getestet sind zwei von fünf Herkünften.

**Vorschlag (nicht gelandet, Freigabe 3):**

- **(CJ)** Ein AK der Form „X sperrt/greift, wenn Y" bekommt in der Fall-Tabelle automatisch die Gegenzeile „Y' (Nachbar, fremder
  Elternteil) → greift nicht", und eine Aussage „aus jeder Phase/jedem Weg" zählt ihre Herkünfte als Zeilen auf. Ort: `pm-loop`
  Step 5 neben „Fälle Weg × Wert". Prosa, weil es eine Regel beim Schreiben der Tabelle ist.

## Sprint 26 — Ziel & Umfang (Lauf AF, NP-26)

**Sprint-Ziel (aus NP-26, verkürzt):** Der Vater sieht in der Übungsauswahl – im Assistenten wie im Planbau – den Umfang jeder
Übung und kann sie ausprobieren, bevor er sein Kind darauf verpflichtet.
**Umfang:** B-424. **Draußen, mit Namen:** B-408 (gesperrt, keine Freigabe), B-423 (angehalten, siehe unten).
**Entwickler-Brief:** Quelle der Wahrheit für den Umfang ist der Server (`ExerciseSummary.ItemCount`, Wort aus dem Manifest
`ItemNoun`); Vokabel zählt Item-Zeilen, alle übrigen Typen ihre Atome aus der Config, Typen ohne zählbare Atome `null`. Keine
Migration, Vertrag additiv. Frontend: zwei Zeilen mit Umfang und „Ausprobieren" (vorhandener Testmodus). Testweg:
`ExerciseItemCountTests`, Vitest, neue Spec `uebungsauswahl-umfang.spec.ts` als Rollengang Vater.

**B-423 angehalten (Freigabe 8):** Entscheidung 2 (erstes Drittel Selbsteinschätzung) kollidiert mit der Vorgabe des Ziels
„Klassenarbeit" (`requireTypedTest: true`): ein ungewerteter Test zählt dann nicht fürs Tagesziel (`PositionProgressService.cs:116`),
im ersten Drittel liefe der Malus gegen das Kind. Kein Code geschrieben, Story zurück auf `ausformuliert` mit Offenem Punkt 5 und
vier Optionen. Entscheidungen 1, 3, 4 bleiben gültig; Nebenbefund: `CreateClassTestDto` nimmt die Übungen gleich mit.

## Iteration 26 — umgesetzt

Backend: Manifest-Wörter je Typ (Einzahl/Mehrzahl), `IExerciseType.ItemCount`, `ExerciseSummary.ItemCount` per korrelierter
Unterabfrage; kaputte Configs kosten nur ihre eigene Zahl. Frontend: `umfangText`, Zeilen mit „· 3 Karten" und „🧪 Ausprobieren"
neben dem Label, Testmodus im Planbau außerhalb des Formulars. Doku: `wiki/08-erweitern.md`, `/neuer-uebungstyp`.
**Rote Probe mit Zahl:** 4 von 4 neuen Integrationstests rot an `KeyNotFoundException` vor dem Bau. 14 Mutanten, alle rot am
benannten Fall (Tabelle in B-424), nach dem letzten Edit gemessen.

## Runde 2 — Review (Sprint 26)

`frontend-reviewer`: 🟡 Planbau-Hälfte der E2E prüfte nur den Ausgangszustand → behoben (**1**). `pugling-reviewer`: 🟡 `catch` nur
`JsonException` (eine `null`-Element-Config hätte die Suche auf 500 gesetzt) (**2**), 🟡 Fehlerpfad ungetestet (**3**) → behoben.
🟢 ohne Zählung behoben: Sperre auf veralteten Zeilen, Manifest-Anzahl, Summary. 🟢 → [B-444](backlog/B-444-testmodus-fehler-ohne-alert-und-alter-ausweg.md).
**Zähler Sprint 26: 3 von 5.**

## Runde 3 — Abnahme (Sprint 26)

- **Vater:** Rollengang als E2E `uebungsauswahl-umfang.spec.ts`: Assistent und Planbau nennen „3 Karten", „Ausprobieren" öffnet den
  Testmodus genau dieser Übung, Schließen lässt die Wahl stehen. Sprint-Ziel gehalten – für den Teil, den NP-26 noch trägt; „Plan,
  Klassenarbeit und Stufenleiter aus einer Hand" (B-423) ist **nicht geliefert** und wartet auf Offenen Punkt 5.
- **Creator, Sohn:** Regressionszeugen – Suite grün, Vertrag additiv, kein Spielweg im Diff.
- Zahlen nach dem letzten Edit: Backend 1178/1178, Vitest 696/696, `tsc -b` sauber, Playwright **122 passed**.

B-424 → `abgenommen`; B-423 → `ausformuliert` (`wartet_auf`); B-408 unverändert `idee`.

## Retrospektive (Sprint 26)

**Nachschau:** Sprint 25 (`b2da41d`, B-436), frischer `pugling-reviewer`: kein 🔴, kein 🟡. Prüfpunkte: drei Mutanten-Zeilen gegen Code
und Fall, Determinismus gegen die geteilte Klassen-DB, die neue Kommentar-Behauptung gegen beide KeyResult-Schreibwege. Zwei 🟢
(Zeilenangaben im Fundtext berichtigt, Parametername bewusst gelassen). `nachgeschaut` in B-436 gesetzt. Kein Limit 2.

**Was die eigenen Tore durchgelassen haben:** B-423 kam mit `geschaetzt` und Freigabe in den Lauf und widersprach sich doch selbst –
die Schätzung nannte die Anzeigestufe als Risiko („kein Tag auf ShowBoth"), folgte dem Wert aber nicht bis zur Regel, die ihn liest
(`Graded` → Tagesziel → Malus). Gefunden habe ich es beim Lesen von `PositionTestsController` vor dem Bau, nicht über ein Tor. Und
im Bau von B-424 fing ich zuerst nur `JsonException` – die Summary versprach mehr, als der Code hielt; der Reviewer fand es.

**Vorschlag (nicht gelandet, Freigabe 3):**

- **(CK)** Setzt eine Entscheidung einen Wert, den der Server später liest (Stufe, Pflicht-Schalter, Datum), nennt der Angriffsplan
  in `geschaetzt` jede **lesende** Regel mit `Datei:Zeile` (per `grep` auf das Feld gefunden) und sagt je Regel, was der neue Wert dort
  bewirkt. Ort: Eintrittsbedingung `geschaetzt` im Backlog-README. Verwandt mit (CD) aus Lauf AE (Schreib- und Lesestelle derselben
  Spalte); zusammen landen oder zusammen verwerfen.

**Ende Lauf AF.** Kein Limit gegriffen (Zähler 0 / 1 / 3, keine Nachschau mit 🟡, kein Tor gelandet). Der Lauf endet mit dem Paket,
das ihm der Rollengang-Plan zugedacht hat (Nacht 2 = NP-26); Lauf AG übernimmt NP-27 (Nacht 3).
