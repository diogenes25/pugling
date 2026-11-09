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
