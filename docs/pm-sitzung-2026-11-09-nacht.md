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

## Lauf AG — Eingang

**Auftrag:** zweiter der zwei Läufe aus „Starte zwei Nachtläufe". Paket NP-27 aus dem Rollengang vom 2026-11-08 (Nacht 3). Die vier
`Defekt`/`Aufräumen`-Stories B-425, B-426, B-427, B-429 stehen auf `ausformuliert` (Freigabe 1, autonom grillbar); B-428 und B-431
sind `geschaetzt` mit Freigabe-Zeile aus `05700b6` (Freigabe 9). **Geteilt in zwei Sprints** statt des einen im Paket: sechs Stories
auf zwei Rollen (Vater, Creator) wären ein Review, den niemand bezahlt – drei je Sprint, je eine Rolle.

## Sprint 27 — Ziel & Umfang (Lauf AG, NP-27 Teil 1)

**Sprint-Ziel:** Der Vater liest im Konto je Buchung die Währung und überall dieselbe deutsche Schreibweise eines Datums, und die
Demo-Familie zeigt ihr Buch als Katalog-Reihe und ihr Startguthaben als Papa-Buchung.
**Umfang:** B-425, B-427, B-429 (nach dem Teilen). **Draußen:** B-445 (Seed-Klasse, filtert den Katalog in mehreren Specs), B-446
(Tipp – Produktfrage im Resolver).
**Entwickler-Brief:** Währung kommt vom Server (`PointKindCurrency`) als additives Feld auf beiden Ledger-DTOs; Datum über ein
Modul mit fester Locale, `DateOnly` als Text; Tor gegen `toLocale…()` ohne Locale. Seed: Buch an `green-line-1`, Startguthaben
`Manual`/`ManualGems`. Testweg: `PointsCurrencyTests`, `SeedFamilyTests`, `labels.test.ts`, `datum.test.ts`, neue Spec
`konto-waehrung-und-datum.spec.ts` (Rollengang Vater + Sohn, Locale en-US fest).

## Iteration 27 — umgesetzt

Backend: `Currency` auf `PointsEntryResponse`/`MyPointsEntryResponse` (drei Projektionen), Seed-Buch an Reihe und Unit,
Startguthaben als Papa-Buchung, Capture neu. Frontend: `signedAmount` in Vater- und Sohn-Konto, Spalte „Betrag"; `lib/datum.ts`,
zwölf Aufrufe und vier ISO-Anzeigen umgestellt, Tor `datum.test.ts`. **Rote Proben mit Zahl:** 3/3 (Währung), 2/2 (Seed), Tor 12 Funde
→ 0. 15 Mutanten, alle rot am benannten Fall (Tabellen in den Stories), inklusive Zeitzonen-Probe unter `TZ=America/New_York`.

## Runde 2 — Review (Sprint 27)

`frontend-reviewer`: keine 🔴. Vier 🟡, alle behoben: drei rohe ISO-Daten übrig (Klassenarbeit, Ziel-Frist, „Fällig") (**1**); Server-
Zeitstempel ohne Offset würden als Ortszeit gelesen – `formatDate`/`formatDateTime` ergänzen ein `Z` (hergeleitet aus EF/SQLite, nicht
am Endpunkt gemessen) (**2**); Tagesverlauf, Planliste und Assistent-Überblick ungetestet → E2E-Zusicherungen (**3**); das Minus in
`signedAmount` widersprach Doku und Assistent → U+2212 (**4**). 🟢 ohne Zählung: Tor um drei Formen erweitert, Arcade-Hinweis in der
Meldung, Tag-Grenze als Entscheidung dokumentiert, Spec in drei Fälle geteilt; 🟢 → [B-447](backlog/B-447-arcade-zeigt-iso-daten.md).
`pugling-reviewer`: keine 🔴/🟡; 🟢 Gegenrichtung „fest Gems" getestet, Tutorial ergänzt, Demo-Kind-Zeilen bewusst hingenommen.
**Zähler Sprint 27: 4 von 5.** 24 Mutanten nach dem letzten Edit, alle rot (Tabellen in den Stories).

## Runde 3 — Abnahme (Sprint 27)

- **Vater:** Rollengang als E2E `konto-waehrung-und-datum.spec.ts` (drei Fälle je Fläche, Browser-Locale en-US fest): Konto mit „Betrag“
  und Währung je Buchung, deutsche Daten in Konto, Plan, Tagesverlauf, Planliste, Klassenarbeit, Ziel; dazu `assistent.spec.ts` (Überblick).
- **Sohn:** derselbe Fall 1 – „Mein Konto“ zeigt „+9 💎“.
- **Creator:** Regressionszeuge – Suite grün, keine Creator-Fläche im Diff.
- **Seed (B-429):** `SeedFamilyTests` gegen frische Seed-DB statt eines Klicks durch den Kind-Hub (ausdrücklich benannt in der Story).
- Zahlen nach dem letzten Edit: Backend 1183/1183, Vitest 715/715, `tsc -b` sauber, Playwright **125 passed**.

B-425, B-427, B-429 → `abgenommen`. Neu: B-445, B-446 (aus dem Teilen von B-429), B-447 (Arcade-Daten), B-448 (Nachschau).

## Retrospektive (Sprint 27)

**Nachschau:** Sprint 26 (`453a740`, B-424), frischer `pugling-reviewer`: kein 🔴, **drei 🟡** → [B-448](backlog/B-448-umfang-nachschau-reste.md)
(`entgangen_bei: [B-424]`): „leer“ (0 statt `null`) am Server ungetestet – zwei Mutanten überleben alle 1178 Tests; der breite `catch`
schluckt ohne Log; eine Summary hängt am falschen Helfer. Prüfpunkte in B-424 notiert. Increment des vorigen Laufs (AF) – nach der
Präzedenz aus Lauf AD **kein Limit 2**; der Lauf geht mit Sprint 28 weiter.

**Was die eigenen Tore durchgelassen haben:** (1) Der Ist-Stand von B-427 zählte die Fundstellen nach **Funktionsname**
(`toLocaleDateString`) und übersah drei rohe ISO-Anzeigen; der Reviewer fand sie, indem er die Felder mit `format: date` aus dem
OpenAPI-Dokument nahm und ihre Anzeige suchte. Mein Tor sieht dieselbe Lücke: es kennt nur `toLocale…()`. (2) B-424 trug einen
Grenzfall („leer“) nur im Frontend – dieselbe Klasse wie (CJ) aus Sprint 25: die Fall-Tabelle hatte „zählt“, nicht „zählt null“.

**Vorschlag (nicht gelandet, Freigabe 3):**

- **(CL)** Eine Story, die eine **Darstellung** vereinheitlicht (Datum, Betrag, Typname), erhebt ihren Ist-Stand über die **Daten**, nicht
  über die alte Funktion: die Vertragsfelder des Typs (`format: date|date-time` bzw. das DTO-Feld) aus `docs/openapi/v1.json`, dann je Feld
  seine Anzeige. Ort: `pm-loop` Step 5 neben „Fälle Weg × Wert“ oder die Eintrittsbedingung `ausformuliert`. Ein Tor über rohe ISO-Strings
  in JSX wäre denkbar, fände aber nur das Muster, nicht das Feld.

## Sprint 28 — Ziel & Umfang (Lauf AG, NP-27 Teil 2)

**Sprint-Ziel:** Der Creator sieht in den Lehrwerken je Unit die Zahl ihrer Übungen, auf „Übungen verwalten" zuerst seine eigenen
Übungen über alle Fächer und im Fachlehrer-Profil die deutschen Typnamen aller Typen.
**Umfang:** B-426, B-428, B-431.
**Entwickler-Brief:** B-428 – additives `ExerciseCount` auf `SeriesUnitResponse` (DB-Count, gruppiert für die Liste), Spalte mit
Link auf die gefilterte Liste. B-426 – Typ-Knöpfe aus dem Manifest (als Prop, das Formular bleibt netzfrei), Server weist
unbekannte Schlüssel mit `validation_error` ab, vor jeder Änderung. B-431 – ohne Fach lädt die Liste `mineOnly` über alle
Fächer, jede Zeile nennt dann ihr Fach. Testweg: `SeriesUnitExerciseCountTests`, `CreatorProfileTypeKeyTests`, Vitest
`VaterFachlehrer.test.tsx`, neue Spec `creator-katalog-ueberblick.spec.ts` (Rollengang Creator, drei Fälle je Fläche).
**Abweichung von B-431 Entscheidung 1:** der Endpunkt kennt `mineOnly` – also serverseitig, gepagt, mit echter Gesamtzahl; die
Überschrift „Meine Übungen (Seite)" aus der Rückfall-Variante entfällt.

## Iteration 28 — umgesetzt

Backend: `ExerciseCount` auf `SeriesUnitResponse` (gruppiert, Get/PATCH per Count), `UnknownTypesProblem` im Fachlehrer-Profil (Create und
PATCH, vor dem Speichern). Frontend: Spalte „Übungen" mit Link, Typ-Knöpfe aus dem Manifest, „Meine Übungen" aller Fächer mit
„Fach: …" je Zeile. **Rote Proben mit Zahl:** 1/1 (Zählung), 2/3 (Typ-Prüfung; der dritte Fall ist Gegenprobe). 19 Mutanten nach
dem letzten Edit, alle rot (MB1/MB2 im Gesamtlauf zuerst „kompiliert nicht", einzeln nachgemessen: rot).

## Runde 2 — Review (Sprint 28)

`pugling-reviewer`: 🟡 Entscheidung 3 von B-428 (fremde Creator sehen dieselbe Zahl) ungetestet → zweites Konto im Test (**1**); 🟢 drei
Kommentar- bzw. Doku-Punkte behoben. `frontend-reviewer`: **🔴 die Unit-Zahl zählt alle Übungen, ihr Link öffnete „nur meine"** – „3",
dahinter „Übungen (0)" → Schalter „geteilte Übungen" in die Adresse, Link setzt `shared=1` (**2**); 🟡 „ohne Fach", solange die Fächer
fehlen → `fachDerZeile` (**3**); 🟡 gescheiterte Liste sah aus wie leerer Bestand → `ListenAusfall` (**4**); 🟡 Manifest-Ausfall auf der
Fachlehrer-Seite stumm → `ListenAusfall` (**5**). 🟢 ohne Zählung: Singular im Linknamen, `title` am Altlast-Knopf, Export zurückgenommen.
**Zähler 5 von 5.**

## Ende Lauf AG — an Freigabe 3

Der volle E2E-Lauf nach dem letzten Edit: **1 failed, 129 passed** (Backend 1187/1187, Vitest 723/723, `tsc -b` sauber). Rot
`typ-manifest-faellt-aus.spec.ts:40`: der Schalter „geteilte Übungen" springt nach dem Klick zurück – eine Folge des Fixes zu Fund 2
(veraltete Closure in `setFilter`, verzögertes Rendern der Adresse; Analyse in [B-450](backlog/B-450-schalter-geteilte-uebungen-springt-zurueck.md)).
Seine Behebung würde Code ändern → **Fund 6, der Zähler überschreitet 5 → der gesamte Lauf endet sofort** (Freigabe 3). Nicht repariert.
B-426, B-428, B-431 bleiben **`in-arbeit`** mit `wartet_auf`; der Stand ist committet, aber nicht abgenommen (Präzedenz: NP-2 und
Sprint 12). Ein Rollengang hat darum nicht als Abnahme stattgefunden; die Spec `creator-katalog-ueberblick.spec.ts` ist grün (5/5),
die Abnahme scheitert an der Nachbar-Spec.

**Korrektur im Lauf, ehrlich vermerkt:** beim Schreiben der Story-Dateien setzte ein Skript die drei Stories zunächst auf
`abgenommen` (eine Ersetzung im Skript griff nicht); noch vor dem Commit auf `in-arbeit` zurückgesetzt.

## Retrospektive (Sprint 28)

**Nachschau:** Sprint 27 (`a14961d`, B-425/B-427/B-429), frischer `frontend-reviewer`: kein 🔴, **ein 🟡** →
[B-449](backlog/B-449-zeitzonen-tests-greifen-nur-in-new-york.md) (`entgangen_bei: [B-427]`): die Zeitzonen-Tests greifen weder lokal
(Berlin) noch in der CI (UTC), die Mutanten wurden nur einmal unter `TZ=America/New_York` bzw. Berlin gemessen; Nachkommastellen
ungetestet. Increment **dieses** Laufs → für sich schon **Limit 2**. `nachgeschaut` in B-425/B-427/B-429 mit Prüfpunkten gesetzt.

**Was die eigenen Tore durchgelassen haben:** (1) Ein Review-Fix (Fund 2) erzeugte Fund 6: ich habe den Schalter in die Adresse gelegt,
die eigene Spec dafür grün gesehen und die Nachbar-Spec, die denselben Schalter direkt nach einem Fach-Wechsel klickt, erst im
Volllauf. (2) Eine Mutante, die nur unter einer bestimmten Zeitzone rot wird, ist ein Beleg für genau diese Zone – die Tabelle in B-427
sagte das nicht deutlich genug, und CI/lokal laufen in anderen.

**Vorschläge (nicht gelandet, Freigabe 3):**

- **(CM)** Nach einem Review-Fix, der einen **geteilten** Zustand umbaut (Adresse, Kontext, Cache), läuft vor der nächsten Zählung die
  E2E-Teilmenge aller Specs, die diese Seite besuchen (`grep -l "<route>" e2e/`), nicht nur die eigene. Ort: `pm-loop` Step 5.
- **(CN)** Eine Mutanten-Zeile, die nur unter gesetzter Umgebung (`TZ`, Locale) rot wird, nennt die Umgebung in der Spalte „gemessen",
  und der zugehörige Test setzt sie selbst (oder ist zonenfrei formuliert) – sonst ist er im Alltag blind. Ort: Docstring von
  `mutant-probe.py`.

## Offene Roadmap

Stand im Backlog ([docs/backlog/](backlog/README.md)). **Nächster Start:** B-450 (P2, blockiert die Abnahme von B-426/B-428/B-431),
danach B-449 und B-448. **Für den Dialog:** B-423 Offener Punkt 5 (Stufenleiter vs. `requireTypedTest`), B-408 (Bewertung im
Assistenten – keine Freigabe), B-445 (Seed-Kind mit Klasse), B-446 (Tipp-Inhalt). Retro-Vorschläge (CI)–(CN) unentschieden. Nichts gepusht.
