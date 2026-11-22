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

## Sprint 112 — Ziel & Umfang

**Sprint-Ziel:** Aus Sicht des Vaters schickt „OK“ im Bearbeiten eines Angebots nur den Termin, den er in diesem Formular
sieht und gewählt hat. Widerlegbar durch einen PATCH mit einem verworfenen oder einem alten, nicht gezeigten Termin.

**Umfang:** [B-786](backlog/B-786-shop-bearbeiten-uebernimmt-alten-termin.md) (aus dem Review von Sprint 111, im Lauf
ausformuliert, gegrillt und geschätzt – Defekt, autonom).

**Entwickler-Brief:** Senke bleibt `planDto`; neu ist die Belegung beim Öffnen (`editForm`/`editPlan`), die einzige Stelle,
an der das Formular Werte aus dem Angebot übernimmt. Keine Server-Änderung.

## Iteration 112 — umgesetzt

- `VaterShop.tsx`: „Bearbeiten“ belegt Formular, Plan und Zielgruppe frisch; das Datum nur bei „einmalig“.
- **Review (`frontend-reviewer`):** ein 🟡 behoben – wählte der Vater nach dem Wechsel auf „einmalig“ den Tag des
  Rest-Termins, ging dessen verborgene Uhrzeit mit; **Zähler 1 von 5**. 🟢: „OK“ schließt vor dem Ergebnis (gewollt, im
  Verlauf vermerkt); geleerter Titel bleibt stehen → [B-789](backlog/B-789-angebotstitel-leeren-bleibt-stehen.md).
- **Mutanten nach der letzten Änderung:** N1–N8 rot, E2E-Probe E3 rot. Vitest `src/vater` 543/543, `tsc -b` sauber.
- **Nummern-Kollision:** B-787 war kurz doppelt vergeben – Lauf B hatte B-787/B-788 auf seinem Branch vergeben, bevor
  dieser Lauf seine Story anlegte; umnummeriert auf B-789 (`d5f344c4`). Die Zählung vorher sah nur den Arbeitsbaum von
  Lauf B, nicht dessen frische Commits. Abgestimmt über die gemeinsamen Notizen.

## Runde 4 — Rollengang Sprint 112

- **Vater — abgenommen** (E2E `shop-einmal-termin-bearbeiten`, zweiter Fall; Server frisch auf 5201/5174): Datum geändert,
  „Abbrechen“, erneut „Bearbeiten“ – das Feld zeigt wieder den gespeicherten Tag, nur der Preis ändert sich, der Termin geht
  unverändert hinaus. „Abbrechen heißt jetzt auch abbrechen.“
- **Sohn — Regressionszeuge:** `shop-verlauf`, `full-flow` grün.
- **Creator — Regressionszeuge:** keine Creator-Fläche im Diff.

## Retrospektive (Sprint 112)

**Nachschau:** Sprint 111 nachgesehen (B-782, `nachgeschaut: 2026-11-22`). Gefunden: der Aufrufer von `planDto` gab den
geladenen Termin auch bei einem wiederkehrenden Angebot weiter – in B-782 harmlos, weil das Datum dort nie leer vorbelegt
war; erst B-786 machte den Fall möglich. Kein Defekt in B-782 selbst, darum kein `entgangen_bei`.

**Was die eigenen Tore durchgelassen haben:** Eine Zählung der nächsten B-Nummer, die nur Dateien beider Checkouts las,
nicht die Commits des anderen Branches (Lauf B hatte zwischen Zählung und Anlage committet). Das Commit-sofort-Verfahren
hielt den Schaden klein (eine Umnummerierung).

**Vorschlag (nur Vorschlag, nicht gelandet):** Beim Parallelbetrieb zählt die Nummer unmittelbar vor dem Anlegen über
`git ls-tree` aller Branches *und* die gemeinsame Notiz, und die Notiz wird **vor** dem Commit geschrieben (Reservierung
statt Bericht). Prosa in `docs/nachtlauf.md`, kein Tor.

Kein Halt-Grund: kein neues Tor; der Fund im Increment ist behoben, Zähler 1 von 5.

## Sprint 113 — Ziel & Umfang

**Sprint-Ziel:** Aus Sicht des Sohns hält die Bonusrunde, was sie zeigt: der Aufwärm-Hinweis verspricht nur die Stufe, die
wirklich mehr zahlt, und wer mit Tastatur spielt, verliert nach „Aufdecken“ und am Ergebnis nicht die Stelle. Widerlegbar
durch „ab der Auswahl gibt es mehr“ oder einen Fokus auf `body` nach „Aufdecken“/am Ergebnis.

**Umfang:** [B-785](backlog/B-785-aufwaermhinweis-verspricht-mehr-ab-der-auswahl.md),
[B-778](backlog/B-778-bonus-fokus-ergebnis-und-aufdecken.md) – beide von `idee` bis `geschaetzt` im Lauf (Defekt, autonom).

**Entwickler-Brief:** B-785: fester Text, Voraussetzung am Server festgeschrieben (keine Vertragsänderung für einen Satz).
B-778: Muster des Tests (`SohnTest.tsx`) übernehmen – eine Absicht, ein Effekt; nach der letzten Karte wartet sie aufs
Ergebnis. Keine Migration, kein Vertrag.

## Iteration 113 — umgesetzt

- `SohnBonus.tsx`: Absicht in `onReveal`, Effekt auf `[cursor, revealed, result, error, run]`, `resultTitleRef` am
  Ergebnis. `de.ts`/`en.ts`: `bonus.hintSelf` „beim Tippen gibt es mehr“. `ExtraTrainingRatesTests` (2 Fälle).
- **Review (`frontend-reviewer`):** ein 🟡 behoben – der getippte Weg zum Ergebnis war ungetestet; **Zähler 1 von 5**.
  🟢: Wortlaut „Tippen“ statt „Schreiben“ übernommen (gleiche Seite nennt die Form so); Fokus, den das Kind während der
  Auswertung verschiebt, wird überholt – wie im Test, belassen.
- **Mutanten nach der letzten Änderung:** Q1–Q6, R1–R3 rot; E2E-Probe E4 rot.
- **Volle Suite:** Backend 2023/2023 (Release); Vitest 1323/1324 im Gesamtlauf – der eine rote Fall
  (`SohnPractice.test.tsx`, Fokus beim Kartenwechsel) liegt in einer Datei ohne Diff dieses Laufs und war einzeln zweimal
  grün (52/52): das Last-Flackern aus B-603, das Lauf B auf seinem Branch bearbeitet. Nicht repariert (fremder Bereich).

## Runde 5 — Rollengang Sprint 113

- **Sohn — abgenommen** (E2E `bonus`, erweitert; Server frisch auf 5201/5174): Vokabel-Aufwärmen – oben steht „jedes
  gewusste Wort bringt 1 🪙, beim Tippen gibt es mehr“; nach „Aufdecken“ steht der Fokus auf „Nicht gewusst“, am Ende auf
  „4 / 4 richtig“. „Mit Enter und Tab komm ich jetzt durch, ohne zu suchen.“ Zusätzlich grün: `bonus-satz-je-karte`.
- **Vater — Regressionszeuge:** `bonus-deckel`, `full-flow` grün.
- **Creator — Regressionszeuge:** `full-flow`; keine Creator-Fläche im Diff.

## Retrospektive (Sprint 113)

**Nachschau:** Sprint 112 nachgesehen (B-786, `nachgeschaut: 2026-11-22`). Geprüft: jeder Wert, den `ListingRow` beim Öffnen
übernimmt (`editForm`, `editPlan`, Zielgruppe), gegen `UpdateShopListingDto`; die Zeile ist `key={l.id}`, `startEditing`
liest bei jedem Klick die aktuelle Prop. Kein Befund.

**Was die eigenen Tore durchgelassen haben:** Ein Test-Helfer mit einem Parameter, den kein Fall nutzte (`starte("Typed")`) –
er sah wie Abdeckung aus. Gefunden hat es der Reviewer. Die Mutante Q6 hätte es auch gezeigt, war aber nicht in der ersten
Liste: die Mutanten folgten den Zeilen des Diffs, nicht den Wegen in die neue Bedingung.

**Vorschlag (nur Vorschlag, nicht gelandet):** Für eine neue Warte-Bedingung (hier `waitingForResult`) je Weg, der sie
erreicht, eine Mutante am *Eingang* des Wegs (`answer()`, `onReveal`), nicht nur an der Bedingung selbst. Zeile in
`pm-loop` Step 5. Prosa, kein Tor.

Kein Halt-Grund: kein neues Tor; der Fund im Increment ist behoben, Zähler 1 von 5.

## Sprint 114 — Ziel & Umfang

**Sprint-Ziel:** Aus Sicht des Vaters ist ein Shop-Feld, das er leert und speichert, danach wirklich leer. Widerlegbar durch
einen Titel oder eine Beschreibung, die nach „gespeichert“ und Neuladen wieder dasteht.

**Umfang:** [B-789](backlog/B-789-angebotstitel-leeren-bleibt-stehen.md) (aus dem Review von Sprint 112, im Lauf
ausformuliert – die Artikel-Beschreibung hat denselben Fehler –, gegrillt und geschätzt; Defekt, autonom).

**Entwickler-Brief:** Der Server leert mit `""` schon; der Fehler sitzt im Client (`|| null`). Keine Vertragsänderung, eine
Festschreibung am Server. Keine Migration.

## Iteration 114 — umgesetzt

- `VaterShop.tsx`: `ListingRow` und `ArticleRow` schicken das getrimmte Feld, auch leer. Vitest `VaterShopLeeren.test.tsx`
  (2), Fakt `B789_…` in `ShopStockPerChildTests`, E2E `shop-felder-leeren` (neu).
- **Mutanten:** T1–T4 rot, E2E-Probe E5 rot.
- **Review (`frontend-reviewer`):** kein 🟡/🔴, **Zähler 0**. 🟢: dieselbe Falle an drei Stellen außerhalb des Shops →
  [B-791](backlog/B-791-geleerte-felder-ausserhalb-des-shops.md) (Nummer vor dem Anlegen in der gemeinsamen Notiz reserviert).
- **Volle Suite:** Backend 2024/2024 (Release); Vitest `src/vater` 545/545. Das Stop-Tor lief einmal in meinen eigenen
  Release-Lauf (MSB3027, gesperrte DLLs) – kein Testergebnis, der Lauf selbst war grün.

## Runde 6 — Rollengang Sprint 114

- **Vater — abgenommen** (E2E `shop-felder-leeren`, Server frisch auf 5201/5174): Angebotstitel geleert → die Zeile zeigt
  „(ohne Titel)“; Artikel-Beschreibung geleert → nach dem Neuladen leer. „Was ich lösche, ist weg.“
- **Sohn — Regressionszeuge:** `shop-verlauf`, `full-flow` grün; der Sohn-Shop zeigt bei leerem Angebotstitel den Artikeltitel
  (Server, `MeController.cs:414`).
- **Creator — Regressionszeuge:** keine Creator-Fläche im Diff.

## Retrospektive (Sprint 114)

**Nachschau:** Sprint 113 nachgesehen (B-778, B-785, `nachgeschaut: 2026-11-22`). Geprüft: alle Texte der Sohn-App, die einen
Münzsatz versprechen (`bonus.*` in `de.ts`/`en.ts`), gegen `ExtraTrainingOptions` (1/1/3) – stimmig; der Fokus-Effekt verbraucht
die Absicht in jedem Endzustand (Ergebnis, Fehler, Abbruch durch Verlassen). Kein Befund.

**Was die eigenen Tore durchgelassen haben:** nichts im Increment. Der Fehler selbst ist eine Klasse (`|| null` vor einem
PATCH), die schon an sechs Stellen saß; kein Tor sucht sie.

**Vorschlag (nur Vorschlag, nicht gelandet):** Ein Vitest-Tor in der Art von `src/lesefehler.test.ts`, das in
`frontend/src/vater` jedes `.trim() || null` in einem Update-Aufruf meldet, mit Ausnahmeliste für Anlege-Formulare. Das wäre
ein **neues Tor** – darum hier nur als Vorschlag; es würde B-791 vorab rot machen.

Kein Halt-Grund im Sinne der Freigaben: kein Tor gelandet, kein Fund im Increment (Zähler 0).

## Stand am Ende von Lauf A

- **Abgenommen:** B-607 (`dd06a3dd`), B-782 (`fbf1bd94`), B-786 (`4013ead9`), B-785 + B-778 (`b2081837`), B-789 (`28fce14d`).
  Nichts gepusht.
- **Neu:** B-786 und B-789 (im Lauf gebaut), [B-791](backlog/B-791-geleerte-felder-ausserhalb-des-shops.md) (`idee`).
- **Parallelbetrieb:** Lauf B (Worktree, Branch `claude/serene-bhabha-1b1ff6`) vergab B-787, B-788, B-790. Eine kurze
  Doppelvergabe von B-787 ist auf B-789 umnummeriert. Beim Merge von Lauf B nach `main`: `docs/backlog` auf Doppel-Ids prüfen.
  Lauf B meldet außerdem, dass `plan-umhaengen-gesperrt.spec.ts` auf `main` fest rot ist (B-790, sein Bereich).
- **Entscheidungen zum Umstoßen** (alle stellvertretend, Defekte): B-782 E1 (Rundweg im Client statt Normalisierung am
  Server), B-786 E2 (Datum bei wiederkehrenden Angeboten leer), B-785 E1 („beim Tippen“, Voraussetzung als Server-Test),
  B-789 E1 (`""` statt `Clear…`-Schalter).
- **Offen beim Nutzer:** die Retro-Vorschläge der Sprints 110–114; der Tor-Vorschlag aus Sprint 114.
- **Nächste Kandidaten:** B-791 (Defekt, `idee`), B-670 (Defekt, `ausformuliert`), B-659 (Defekt L).
- `INDEX.md` nicht neu erzeugt (fremde Änderungen im Arbeitsbaum).

