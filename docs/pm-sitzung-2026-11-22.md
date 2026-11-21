# PM-Sitzung: Nachtlauf A – Hinweis-Reichweite und Einmal-Termin

**Datum:** 2026-11-22  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Autonomer Backlog-Lauf im Haupt-Checkout auf `main`, parallel zu Lauf B (Worktree, B-602/B-605).

Rahmen: Auftrag aus der startenden Sitzung, Nachtlauf nach [nachtlauf.md](nachtlauf.md) mit den Freigaben 1–10. Lauf B
arbeitet zugleich in `.claude/worktrees/` an den flackernden Tests B-602/B-605; dieser Lauf fasst sie nicht an. Die fremden
uncommitteten Dateien (B-626, B-784, `INDEX.md`, Theodor-Skript) bleiben unberührt, `INDEX.md` wird nicht neu erzeugt.
E2E auf 5201/5174; das Heimnetz auf :5200 bleibt unberührt.

## Runde 1 — Stand und Auswahl

Kein neuer Befund unter `docs/anmerkungen/`. Gesetzt sind B-607 (Defekt S, `geschaetzt`) und B-782 (Defekt, `idee`, aus
der Nachschau von Sprint 100). Beide sind eigene Themen (Creator-Editor bzw. Vater-Shop) und laufen darum als zwei Sprints.

| Prio | Story | Größe | wo | Rolle |
|---|---|---|---|---|
| P3 | [B-607](backlog/B-607-hinweis-verschwindet-auf-schreibstufen.md) Hinweis-Reichweite im Editor | S | frontend (+ Festschreibungen) | Creator |
| P3 | [B-782](backlog/B-782-einmal-termin-mit-uhrzeit-faellt-beim-bearbeiten-erneut.md) Einmal-Termin füllt beim Bearbeiten erneut | S | frontend (+ Festschreibung) | Vater |

**Roter Faden:** Was eine Oberfläche dem Erwachsenen anbietet, muss stimmen – ein Feld, dessen Wirkung verschwiegen wird
(B-607), und ein Feld, das einen nicht geänderten Wert verändert zurückschickt (B-782).

## Refinement

B-782 von `idee` bis `geschaetzt` (Defekt, autonom grillbar, Freigabe 1). Entscheidung 1 (stellvertretend, unbestätigt):
der Client schickt den geladenen Termin unverändert zurück, solange das Datum im Feld gleich ist; verworfen wurde die
Normalisierung am Server (verschöbe den Termin eines API-Aufrufers still) und der reine Datumsvergleich am Server (eine
absichtliche Uhrzeit-Änderung wäre keine Änderung mehr).

## Sprint 110 — Ziel & Umfang

**Sprint-Ziel:** Aus Sicht des Creators erfährt, wer im Editor einen Hinweis zu einem Wort schreibt, wo das Kind ihn zu
sehen bekommt – und der Text stimmt mit dem Server überein. Widerlegbar durch einen Editor ohne „ⓘ“ am Hinweis oder einen
Satz, den eine Festschreibung am Server widerlegt.

**Umfang:** B-607.

**Entwickler-Brief:** Senke ist der eine Text `FIELD_HELP.vocabularyHint`; beide Eingabeorte (Editor-Spalte, Zeilenfeld
im Anlegen-Formular) hängen ihn nur an. Backend zuerst als Festschreibung über die Creator-Vorschau (AK 3–5, grün im
Ausgangszustand). Keine Migration, kein Vertrag.

## Iteration 110 — umgesetzt

- `fieldHelp.ts`: Thema `vocabularyHint`; `ExerciseEditModal.tsx` (Spalte) und `exerciseConfig.tsx` (Zeilenfeld) tragen das
  „ⓘ“.
- Backend: `VokabelHinweisReichweiteTests` (15 Fälle) – je Richtung und Stufe über die Vorschau. Abweichung von der
  Schätzung: die Vorschau wendet die Kartendecke an, die Vorwärtskarte von „beides“ kommt also auch an einem Schreibtag als
  Auswahl. Der Test prüft darum, dass jede Karte mit Hinweis eine Auswahl ist.
- Vitest: `VokabelHinweisFeld.test.tsx` (neu), Fall in `ExerciseItemErklaerung.test.tsx`, vier Fälle in `fieldHelp.test.ts`.
- **Mutanten nach der letzten Änderung:** F1–F6 (Frontend) und B1–B3 (Backend) rot, Grundlinie grün; Tabelle im Verlauf der
  Story.
- **Review (`frontend-reviewer`):** zwei 🟡 im Text behoben – die Klammer „immer als Auswahl“ war falsch (die Vorwärtskarte
  kommt auch als Selbsteinschätzung oder Lernkarte, dann ohne Hinweis), und der Abschlusstest zeigt nie einen Hinweis (B-544);
  **Zähler 2 von 5**. 🟢 ohne Zählung: „meist der Artikel“ übernommen, Lernkarte nicht festgeschrieben, „ⓘ“ am Artikel im
  Vokabel-Store wäre eine eigene Idee.

## Runde 2 — Rollengang Sprint 110

Server frisch durch Playwright auf 5201/5174 gestartet, nach der letzten Änderung.

- **Creator — abgenommen** (E2E `vokabel-hinweis-erklaerung`, neu; rote Probe E1 → rot): Übung mit Hinweis anlegen, Dialog
  öffnen, „ⓘ“ am Hinweis anklicken – der Text nennt Auswahlstufe, Abschlusstest und „Deutsch → Fremdwort“. „Jetzt sehe ich
  vor dem Schreiben, dass mein ‚beginnt mit S‘ nur bei der Auswahl kommt.“
- **Vater — Regressionszeuge:** `full-flow`, `auch-richtig-im-uebungseditor` grün.
- **Sohn — Regressionszeuge:** `full-flow` grün; kein Sohn-Pfad im Diff.

## Retrospektive (Sprint 110)

**Nachschau:** Sprint 109 nachgesehen (B-609, `nachgeschaut: 2026-11-22`), kein Befund. Geprüft: alle Aufrufer von
`PositionProgressService.EvaluateAndAwardAsync` (`PositionPracticeController.cs:957`, `PositionTestsController.cs:466`, zwei
Tests) verwerfen das Ergebnis; nach dem `ChangeTracker.Clear()` im Konfliktfall lesen die folgenden
`gamification`/`dailyBox`-Aufrufe frisch aus der DB. Index nicht neu erzeugt (fremde Änderungen an `INDEX.md`).

**Was die eigenen Tore durchgelassen haben:** Der Hilfetext wurde zuerst gegen die *Vorschau* geprüft und dann über alle
Wege verallgemeinert („immer als Auswahl“, „nur auf der Auswahlstufe“). Die Vorschau kennt weder Leitner-Kasten noch
Abschlusstest. Gefunden hat es der Reviewer, nicht ein Test: die Festschreibungen laufen alle über die Vorschau.

**Vorschlag (nur Vorschlag, nicht gelandet):** Ein Hilfetext, der Verhalten über Stufen verspricht, nennt in seiner Story je
Satz den Weg, über den er geprüft ist (Vorschau, Üben, Abschlusstest); ein Satz ohne Weg wird im Bau festgeschrieben oder
gestrichen. Prosa im `pm-loop` Step 5, kein Tor.

Kein Halt-Grund: kein neues Tor; die Funde im Increment sind behoben, Zähler 2 von 5.

## Sprint 111 — Ziel & Umfang

**Sprint-Ziel:** Aus Sicht des Vaters füllt ein „einmalig“-Angebot nicht noch einmal auf, wenn er im Vater-Web nur den Preis
ändert – auch wenn ein anderer Aufrufer den Termin mit Uhrzeit gesetzt hat. Widerlegbar durch einen PATCH, der einen
unveränderten Termin anders zurückschickt, als die Liste ihn geliefert hat.

**Umfang:** B-782.

**Entwickler-Brief:** Senke ist `planDto`, der eine Ort, an dem das Vater-Web `refillAtUtc` baut (Anlegen und Bearbeiten).
Der Server bleibt, wie er ist; eine Festschreibung hält, dass der unverändert zurückgeschickte Termin dort nichts auslöst.

## Iteration 111 — umgesetzt

- `VaterShop.tsx`: `planDto(plan, loadedRefillAtUtc)`; `ListingRow` reicht `listing.refillAtUtc` durch (für den Test
  exportiert). Vitest `VaterShopEinmalTermin.test.tsx` (3 Fälle), Theorie `B782_…` in `ShopStockPerChildTests` (2 Fälle).
- **Mutanten:** M1–M5 rot, Grundlinie grün.
- **Review (`frontend-reviewer`):** kein 🟡/🔴, **Zähler 0**. Zwei 🟢 sind ältere Fehler im selben Formular (Rest-Termin beim
  Wechsel auf „einmalig“, „Abbrechen“ setzt nicht zurück) → [B-786](backlog/B-786-shop-bearbeiten-uebernimmt-alten-termin.md),
  bewusst nicht in B-782.
- **Volle Suite (Release):** Backend 2021/2021; Vitest 1313/1313; `tsc -b` sauber.

## Runde 3 — Rollengang Sprint 111

- **Vater — abgenommen** (E2E `shop-einmal-termin-bearbeiten`, neu; rote Probe E2 → rot; Server frisch auf 5201/5174):
  Angebot mit Termin 10:00 Uhr per API, im Vater-Web nur den Preis auf 12 gesetzt – der PATCH trägt den Termin, wie die Liste
  ihn zeigte. „Ich ändere den Preis, und sonst passiert nichts. So soll es sein.“ Dabei gemessen: die Antwort des Anlegens
  trägt den Termin mit `Z`, die Liste ohne – der Client schickt den Wert der Liste zurück, der Server liest dieselben Ticks.
- **Sohn — Regressionszeuge:** `shop-verlauf`, `full-flow` grün.
- **Creator — Regressionszeuge:** keine Creator-Fläche im Diff; derselbe `full-flow`-Lauf.

## Retrospektive (Sprint 111)

**Nachschau:** Sprint 110 nachgesehen (B-607, `nachgeschaut: 2026-11-22`). Geprüft: jeder Satz von `FIELD_HELP.vocabularyHint`
gegen einen Weg – Auswahl/Schreiben/Richtung über die Festschreibungen, „im Abschlusstest nie“ gegen
`PositionTestFlowTests.Abschlusstest_Liefert_Keinen_Tipp_Das_Ueben_Schon`. Kein weiterer Befund.

**Was die eigenen Tore durchgelassen haben:** nichts im Increment. Der Reviewer fand zwei ältere Fehler im selben Formular –
beide entstehen aus demselben Muster wie B-782 (der Client schickt einen Wert, den der Vater in diesem Formular nicht gewählt
hat). Die Retro-Frage aus Sprint 100 („eine Story am Wechsel eines Feldes prüft den Rundweg des Clients“) hätte sie gefunden,
wäre sie auf *alle* Felder des Formulars ausgedehnt worden.

**Vorschlag (nur Vorschlag, nicht gelandet):** Ein Defekt im Rundweg eines Formulars prüft alle Wege, auf denen das Formular
einen Wert zum Server bringt, den der Nutzer nicht berührt hat (Vorbelegung, Abbrechen, Artwechsel) – als Zeile in
`pm-loop` Step 5. Prosa, kein Tor.

Kein Halt-Grund: kein neues Tor, kein Defekt im Increment (Zähler 0).

