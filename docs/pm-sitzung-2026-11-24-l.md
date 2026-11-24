---
tags: [typ/protokoll, bereich/pm]
---

# PM-Sitzung: Nachtlauf L – Klausur-Prognose und Notenstufe

**Datum:** 2026-11-24  ·  **Moderation:** PM (Nachtlauf, unbeaufsichtigt)
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Die Klausur-Prognose aus B-683 bauen: Schnappschuss am Klausurtag, Wirkung der Note, Abweichung für den Vater
(B-744, B-745, B-746).

Auftrag: Nachtlauf L, einer von drei parallelen Läufen (J, K bauen Hausaufgaben B-760–763 und Meldungen B-767–769), gelesen
als der Auftragstext aus [nachtlauf.md](nachtlauf.md) mit den Freigaben 1–11. Worktree `.claude/worktrees/nachtlauf-l`,
Branch `nachtlauf/2026-11-24-l` ab `5993c86b`. Story-Ids nur B-980 bis B-989, Sprints ab 161. Kopie-Server auf :5232, E2E auf
:5231/:5195; :5200 (Heimnetz, echte `pugling.db`) bleibt unberührt. Hooks messen den Haupt-Checkout; jede Messung hier lief
mit ausdrücklichem Pfad im Worktree.

## Runde 1 — Auswahl

Kein neuer Befund unter `docs/anmerkungen/`. Alle drei Stories stehen auf `geschaetzt`, `art: Wunsch` – baubar nach
Freigabe 8. Ihre Entscheidungen tragen die Marke „stellvertretend im Vorbereitungslauf 2026-11-20 (Sonnet) gefällt,
unbestätigt“; Lauf G hat die Nachbarstories B-740–B-743 mit derselben Marke gebaut, und der Auftraggeber hat dieses Paket
ausdrücklich zugeteilt. Ich baue darum, halte die Marke im Abschlussbericht fest.

- **B-744** (M, backend, Migration): baubar. Die „Zeitzone“-Risikozeile der Story ist überholt: der Klausurtag ist seit
  B-730 der deutsche Lerntag (`LearningDay`), nicht das UTC-Datum.
- **B-745** (M, backend, `migration: offen`): **angehalten (Freigabe 8).** Die Story nennt selbst eine Frage, die keine ihrer
  fünf Entscheidungen beantwortet und die „vor dem Bau zu klären“ ist: Hat die Klausurwirkung neben dem täglichen
  Stufen-Lauf (B-741) Bestand? Gegen den Code geprüft: `SubjectLevelService.Apply` hebt die Stufe beim nächsten Lerntag
  sofort und unbegrenzt auf das gemessene Niveau – ein Abstieg durch eine Klausur wäre am Folgetag zurückgenommen, solange
  der Lernstand die bessere Stufe hergibt; umgekehrt senkt der Lauf eine durch die Klausur gehobene Stufe binnen sieben
  Tagen wieder um eins. Die Antwort entscheidet über eine Migration (Anker-Spalte an `ChildSubjectLevel`) und über die
  Regel im Lauf. Dazu zwei weitere Lücken, die die Story als „Vorschlag“ führt statt als Entscheidung: Umrechnung einer
  Kommanote in eine Stufe (4,5 → 4 oder 5?) und `ClearGrade` (Stufe zurückdrehen oder nicht). Offene Punkte unten.
- **B-746** (S, beides): baubar, braucht nur B-744 (`PredictedLevel`); die Abweichung ist eine Anzeige, keine Wirkung.

| Sprint | Stories | Thema |
| --- | --- | --- |
| 161 | B-744, B-746 | Prognose am Klausurtag festhalten und dem Vater neben der Note zeigen |

## Sprint 161 — Ziel & Umfang

**Sprint-Ziel:** Der Vater sieht in der Klassenarbeiten-Liste bei einer Arbeit, an deren Tag das Kind die App benutzt
hat, die Notenstufe, die die App an diesem Tag für das Fach prognostiziert hat, neben der eingetragenen Note samt
Differenz – bei jeder anderen Arbeit „keine Prognose“, nie eine erfundene Zahl.
**Umfang:** B-744 (M, Migration), B-746 (S, beides).
**Entwickler-Brief:** Quelle der Wahrheit ist `ChildSubjectLevel.Level` nach der Tagesbewertung; der Schnappschuss ist
eine nullable Spalte `Klassenarbeit.PredictedLevel`, geschrieben von `SubjectLevelService` am Ende jeder Bewertung
(Kind-Anmeldung, Lesepfad Kind und Vater) – bedingtes UPDATE (`ScheduledDate == Lerntag`, Fach gesetzt und gemessen,
`PredictedLevel IS NULL`), damit ein Rennen und ein späterer Tag nichts überschreiben. Keine Buchung. Migration
`KlassenarbeitPredictedLevel` (nur die Spalte, kein Rückfüllen). Danach `KlassenarbeitResponse.PredictedLevel` additiv am
Ende, Spalte „Prognose“ in `VaterClassTests.tsx`; die Differenz rechnet eine reine Funktion im Frontend als Note minus
Stufe, ungerundet (2,5 gegen Stufe 3 = −0,5) – so braucht die Anzeige die offene Umrechnung Kommanote → Stufe aus B-745
nicht. Kein Student-Endpunkt liefert das Feld. Testweg: `KlassenarbeitPrognoseTests`,
`PredictedLevelMigrationTests`, `KlassenarbeitenTests`, Vitest `VaterClassTests.test.tsx`, E2E
`klassenarbeit-abweichung.spec.ts`.

## Iteration 161 — umgesetzt

**Backend (API-First):** Spalte `Klassenarbeit.PredictedLevel`, Migration **`20261124201304_KlassenarbeitPredictedLevel`**
(nur `AddColumn` nullable `INTEGER`, kein Rückfüllen; beim Merge ggf. neu zu erzeugen, sie hängt hinter
`20261124131013_ClassTestPrepLeftOut`). `SubjectLevelService.SnapshotTestDayAsync` am Ende jeder Bewertung (Leseprobe, dann
bedingtes `ExecuteUpdate`). `KlassenarbeitenController.Update` verwirft die Prognose bei einem Fachwechsel (Review-Fund).
Vertrag: `KlassenarbeitResponse.PredictedLevel` additiv; der Schülervertrag (`UpcomingClassTestResponse`) trägt es nicht.

**Frontend:** Spalte „Prognose“ in der Klassenarbeiten-Liste (`PrognoseZelle`, `prognoseAbstand`, `abstandText`), E2E
`klassenarbeit-abweichung.spec.ts`.

**Review:** `pugling-reviewer` – 🟡 Fachwechsel (behoben, Test), zwei 🟢 (behoben ohne Zählung). `frontend-reviewer` – 🟡
Vorzeichen nur im `title` (behoben: Worte), 🟡 Rundung entgegen der Doku (behoben: roh + symmetrische Anzeige), 🟢 Wortlaut
vor dem Klausurtag → B-980. Dazu ein eigener Fund bei der Mutanten-Probe: nach der Leseprobe standen die Filter doppelt, M1–M4
am `UPDATE` blieben grün – ein geteilter Filter `due` behebt es. **Fehlerzähler Sprint 161: 4** (drei 🟡 aus dem Review, ein
eigener Fund nach Freigabe 11; alle mit Code-/Teständerung).

## Runde 161 — Re-Review / Abnahme

Zwei Wege, beide nach der letzten Änderung am jeweiligen Teil gestartet:

- **Maschinell:** E2E `klassenarbeit-abweichung.spec.ts` auf :5231/:5195 (Wegwerf-DB): frisches Kind mit drei Wörtern,
  Arbeit heute mit Fach und eine ohne, Kind meldet sich an, Vater trägt die 4 ein und liest in der Liste „Stufe 6 · 2
  besser“ bzw. „keine Prognose“. Grün zusammen mit den Zeugen `klassenarbeit-countdown`, `klassenarbeit-vorbereitung`,
  `notenstufe` (4 passed). `assistent-klassenarbeit` fiel im selben Lauf zweimal am 60-s-Testlimit (Rechner mit drei
  parallelen Läufen, Testdauer 1,1 min); allein wiederholt mit `PUGLING_E2E_CONFIGURATION=Release`: 2 passed.
- **An einer Kopie der echten DB** (`kopie-server.sh`, :5232, nach dem letzten Backend-Edit gebaut; spielte dabei die
  Migration `KlassenarbeitPredictedLevel` ein): Vater liest Friedrichs Notenstufen (Englisch 6, Französisch 6). Danach
  trägt seine Arbeit „Französisch: Petit contrôle Unité 5“ vom 24.11. die Prognose **6**; Theodors Englisch-Arbeit vom
  23.11. bleibt leer (Tag vorbei, kein Rückfüllen), die künftigen Arbeiten ebenso. Server danach beendet.

- **Vater – abgenommen:** „Bei Friedrichs Vokabeltest von heute steht jetzt, was die App erwartet hat – Stufe 6. Wenn ich
  morgen die Note eintrage, sehe ich daneben, ob er besser oder schlechter war, in Worten. Wo es nichts zu vergleichen
  gibt, steht ‚keine Prognose‘ und keine erfundene Zahl.“ Offen für einen Menschen: ob „keine Prognose“ an geplanten
  Arbeiten stört (B-980).
- **Sohn – Regressionszeuge:** Der Schülervertrag trägt das Feld nicht (`KlassenarbeitCountdownTests`, M13 rot);
  `klassenarbeit-countdown` und `notenstufe` grün; kein Sohn-Pfad im Frontend-Diff.
- **Creator – Regressionszeuge:** kein Creator-Pfad im Diff; Suite grün.

**Stand:** B-744 und B-746 `abgenommen` (Code `2886acf6`). B-745 angehalten (Freigabe 8). Verifikation: .NET 2353/2353
(Release; Ausgangsstand 2343), Vitest 130/1445, `tsc -b` und `dotnet format --verify-no-changes` sauber, E2E 4 passed +
2 passed (Wiederholung `assistent-klassenarbeit`). Mutanten-Probe nach der letzten Änderung: Backend 14/14 rot am benannten
Test, Frontend 6/6 (Tabellen in B-744 und B-746).

Hinweis für die echte DB: Der Schnappschuss entsteht nur an einem Zugriff am Klausurtag. Friedrichs Arbeit vom 24.11.
bekommt in der echten `pugling.db` keinen, weil die Migration erst nach dem Tag eingespielt wird.

## Retrospektive (Sprint 161)

**Nachschau:** Sprint 140 (B-954, `ddf6d1d6`) – Behauptung „dieselbe Definition wie ‚diese Woche‘“ nachgerechnet:
`EarnedCoinsByDayAsync` und `WeekCoinsAsync` filtern über dasselbe `EarnedCoins` (`WalletService.cs:97-99`) und schneiden
am Lerntag; einziger Unterschied ist die Obergrenze `<= now` der Woche, die nur Einträge nach der Serveruhr träfe. Beide
Aufrufer in `ChildrenDashboardService` (`:32`, `:98`) gelesen. Kein Befund, `nachgeschaut: 2026-11-24` an B-954. (Die
Läufe J und K schauen womöglich dieselbe Story nach; beim Merge gewinnt eine der gleichlautenden Frontmatter-Zeilen.)
Index danach: Nachgeschaut 576 von 615 abgenommenen.

**Was die eigenen Tore durchgelassen haben:** Drei 🟡 kamen vom Review, keiner von einem Test oder Mutanten.

1. *Fachwechsel nach dem Schnappschuss.* Die Story zählt in AK 5 nur den verschobenen Termin; die zweite Eingabe des
   Schnappschusses – das Fach – kann der Vater genauso per `PATCH` ändern. Ich habe die Aufrufer der **Senke** gegrept
   (wer bewertet die Stufe), nicht die Schreiber ihrer **Eingaben** (wer ändert `ScheduledDate` und `SubjectId`). Die
   Regel „je Aufrufer entscheiden“ aus pm-loop Step 4 deckt das nur halb: sie spricht von Aufrufern eines geänderten
   Helfers, nicht von den Schreibern der Felder, aus denen ein festgehaltener Wert entsteht.
2. *Vorzeichen nur im `title`* und 3. *„ungerundet“ gegen `Math.round`* – zwei Sätze in meiner eigenen Doku bzw.
   Darstellung, die der Test nicht prüfen konnte, weil er das falsche Verständnis teilte (der Test rundete mit).

Von außerhalb der Schleife kam nichts.

**Vorschlag (nicht gelandet, Freigabe 3):** pm-loop Step 4, ein Satz an der Aufrufer-Regel: *„Ein festgehaltener Wert
(Schnappschuss, Momentaufnahme) bekommt je Schreiber jedes Eingabefelds eine AK-Zeile – `grep` nach den Zuweisungen der
Felder, nicht nach den Aufrufern der Senke.“* Ein Tor dafür sehe ich nicht: welche Felder ein Wert „einfriert“, steht
nirgends maschinenlesbar.

**Bewusst ungedeckt:** AK 3 von B-744 (ohne Fach) ist eine Absichtserklärung – NULL-Semantik in SQL hält sie, kein
einzeiliger Mutant macht sie rot. Die Tagesgrenze des Schnappschusses ist nur über explizite Tage im Test gemessen, nicht
über die Uhr.

## Offene Entscheidungen für den Nutzer

1. **B-745** (angehalten): (a) Anker gegen den Tageslauf – soll eine Klausurnote die Stufe dauerhaft setzen, bis neuer
   Lernstand sie überholt (dann Spalte an `ChildSubjectLevel`, Migration), oder nur bis zur nächsten Tagesbewertung?
   (b) Kommanote → Stufe: 4,5 → 4 (Vorschlag) oder → 5? (c) `ClearGrade` dreht die Stufe nicht zurück – bestätigen?
2. **B-744/B-746**: die Entscheidungen stammen stellvertretend aus dem Vorbereitungslauf 2026-11-20 und sind
   unbestätigt. Neu und vom Review getrieben: ein Fachwechsel verwirft die Prognose (Ableitung aus B-744 Entscheidung 2).
3. **B-980** (`idee`): Wortlaut „keine Prognose“ an geplanten Arbeiten.
4. **Retro-Vorschlag** (nicht gelandet): Satz in pm-loop Step 4 zu den Schreibern der Eingabefelder eines festgehaltenen
   Werts.
5. **B-743 AK 2** wartet weiter auf den Nutzer (nicht angefasst).

## Offene Roadmap (priorisiert)

Die Liste, die gilt, ist `docs/backlog/`. Für dieses Paket: zuerst B-745 entscheiden (oben), dann bauen – es ist das
einzige Stück, das die Note auf die Stufe wirken lässt. B-748 (Klausurchance in der Vater-Übersicht) kann die Prognose aus
B-744 lesen.

## Konkreter Änderungsstand (für Review)

Branch `nachtlauf/2026-11-24-l`, ungepusht, nicht gemergt.

- `2886acf6` – Code: Spalte + Migration `20261124201304_KlassenarbeitPredictedLevel`, Schnappschuss in
  `SubjectLevelService`, Fachwechsel im `KlassenarbeitenController`, Vertrag, Frontend-Spalte, Tests, E2E, Doku-Capture.
- Danach ein Doku-Commit: B-744, B-745, B-746, B-954 (Nachschau), B-980, dieses Protokoll, `INDEX.md`.
- Beim Merge: die Migration hängt hinter `20261124131013_ClassTestPrepLeftOut`; bringt ein anderer Lauf eine eigene
  Migration mit, wird diese hier neu erzeugt (Kette verlängern, nicht falten).
