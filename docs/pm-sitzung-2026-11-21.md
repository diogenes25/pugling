---
tags: [typ/protokoll, bereich/pm]
---

# PM-Sitzung: Nachtlauf 2026-11-21 (Sprint 98 ff.)

**Datum:** 2026-11-21  ·  **Moderation:** PM (Opus 5.5, Nachtlauf nach [nachtlauf.md](nachtlauf.md), Freigaben 1–10)
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** `main` wieder grün machen und danach baubare Defekte der Sohn-Arcade abarbeiten.

## Runde 1 — Ausgangslage

- **Fremde Arbeit im Checkout:** Die Sitzung „Learning Dead Animationen integrieren“ hatte rund 30 Frontend-Dateien
  nicht committet. Auf Anweisung des Nutzers („Erst Dad-Arbeit committen“) vorher geprüft und committet: `tsc -b` grün,
  Vitest **1 failed / 1295 passed**. Der Fehler kam aus dem Tor `liveRegions.test.ts` mit zwei Funden: einer in der
  Dad-Arbeit (`LearningDad.tsx:250`, vor dem Commit behoben), einer **auf `main`** (`VaterApp.tsx:200`, seit Sprint 97,
  `5f024e2b`) → B-774.
  - `8a9812a4` Learning Dad. Die Rohquellen `frontend/character/` (17 MB: Original-Clips mit Wasserzeichen,
    Charakter-Bibel als `.docx`) sind **nicht** im öffentlichen Repo, sondern per `.gitignore` lokal, nach dem Vorbild der
    Buchfotos in `raw/`.
  - `c84fbedb` Vorbereitungslauf-Anleitung und B-748 aus den Sitzungen vom 2026-11-20.
- **Roadmap-Punkt 3 vom 2026-11-20** (Celebration gegen `full-flow`, `shop-verlauf`, `taschengeld-monatlich`):
  gemessen **3 failed / 0 passed** → B-775.
- **Nebenbefund ohne Befund:** Im Server-Log steht eine UNIQUE-Verletzung an `ShopListingStocks`. Nachgeschaut: Zwei
  gleichzeitige `GET student/me/shop` legen denselben Bestand an. `ShopService.cs:657` fängt das als verlorenes Rennen ab,
  die Antwort ist 200. EF loggt es trotzdem als ERR.
- E2E läuft auf den Ausweichports 5201/5174, weil auf :5200 der Heimnetz-Server mit der echten DB läuft.

## Sprint 98 — Ziel & Umfang

**Sprint-Ziel:** Aus Sicht des Entwicklers belegt ein grüner Lauf auf `main` wieder, dass die Feier beim Sohn ankommt
und der Vater am Handy den Hinweis „nur am Rechner“ angesagt bekommt. Widerlegbar: Vitest oder eine der vier Specs ist
rot.
**Umfang:** [B-774](backlog/B-774-am-rechner-hinweis-live-region-bedingt.md),
[B-775](backlog/B-775-e2e-feier-titel-nach-learning-dad.md), beide `Defekt`, XS, frontend.
**Entwickler-Brief:** Quelle der Wahrheit ist das gerenderte DOM. B-774 folgt dem Live-Region-Muster aus
`ListControls.tsx:71`. B-775 zieht nur Selektoren nach, das neue Aussehen bleibt. Keine Migration, kein Vertrag.

## Iteration 98 — umgesetzt

- `VaterApp.tsx`: Die Region `role="status"` steht immer da, die Klassen `banner info` und der Text hängen an der
  Bedingung. `vater-schmal.spec.ts` sichert zusätzlich die Banner-Klasse.
- `full-flow`, `shop-verlauf`, `taschengeld-monatlich`: `.cel-title` → `.cel-layer .dad-bubble-text`.
- **Rote Proben:** `liveRegions.test.ts` erwartet 0 Funde, gemessen 2, nach der Dad-Korrektur 1, nach B-774 0 (29/29
  grün). Die drei Specs: vorher 3 failed, nachher mit `vater-schmal` **5 passed**.
- **Mutanten** (von Hand, E2E):

| Mutante | Stelle | erwartet | gemessen |
| --- | --- | --- | --- |
| Banner ohne `caption` | `Celebration.tsx`, Banner-Bühne | `shop-verlauf` rot | rot (`.dad-bubble-text` „GEKAUFT!“ nicht gefunden) |
| Region ohne `banner info` | `VaterApp.tsx` | `vater-schmal` rot | rot (`Expected pattern: /\bbanner\b/`) |

### Review Sprint 98

`frontend-reviewer`: zwei 🟡 und ein 🟢, dazu ein Fund des vollen Vitest-Laufs. **Zähler Sprint 98: 3 von 5.**

1. 🟡 Der Untertitel einer kleinen Feier („+N 🪙“) war unsichtbar, weil die Sprechblase nur am Titel hing → behoben,
   neuer Test (gezählt).
2. 🟡 Die Klassen-Zusicherung in `vater-schmal` fing ein bedingungsloses Banner nicht → Gegenfall ergänzt (gezählt).
3. Der volle Vitest-Lauf: Meine Live-Region in der Sprechblase ließ `Lektion.test.tsx` („Blatt ist selbst keine
   Live-Region“) rot werden → die Blase ist dekorativ, ohne `role` (gezählt).
4. 🟢 Der Kommentar versprach eine Ansage, die kein Aufrufer bekommt → mit 3 erledigt (ohne Zählung).

Der erste volle E2E-Lauf: 178 passed, 1 failed (`feldhilfe.spec.ts:72`). Mit `--repeat-each=3` 9/9 grün, also Flackern
der Familie B-602 (dort vermerkt), nicht im Diff, ohne Zählung.

## Runde 2 — Rollengang Sprint 98

Server nach dem letzten Edit von Playwright gestartet (Ausweichports). **E2E 179/179**, Vitest 1297/1297.

- **Vater — abgenommen:** „Am Handy sehe ich, warum Zuweisen nicht geht, und ohne den Hinweis steht da kein leerer
  Kasten.“ (`vater-schmal`, 390 px). Offen bleibt, ob ein echter Screenreader den Hinweis ansagt.
- **Sohn — abgenommen:** „Combo und Kauf feiern wieder, und bei der kleinen Feier sehe ich jetzt auch meine Münzen.“
  (`full-flow`, `shop-verlauf`, Vitest der kleinen Feier). Wie sich die Feier anfühlt, ist nicht geprüft.
- **Creator — Regressionszeuge:** Kein Pfad im Diff, seine Specs sind grün.

Commit `9ab66ecc`, B-774 und B-775 `abgenommen`.

## Retrospektive (Sprint 98)

**Nachschau:** Sprint 97 und B-771 nachgesehen (B-747, B-770, B-663, B-749, B-771; `nachgeschaut: 2026-11-21` an allen
fünf, Prüfpunkte je Story im `## Verlauf`), gelesen von `pugling-reviewer` und `frontend-reviewer`.

- Drei Entgänge: B-663 → B-774 (schon behoben), B-749/B-770 → B-777 (Status „offen“ trotz Antworten), B-771 → B-776
  (wiederkehrend → einmalig feuert nie).
- Der Index meldete vor der Nachschau „507 von 541“.

**Was die eigenen Tore durchgelassen haben:** Sprint 97 wurde mit **roter Vitest-Suite** committet und abgenommen. Das
Tor `liveRegions.test.ts` hätte es gemeldet, aber niemand hat es laufen lassen.

- Geprüft am Repo: Das Stop-Tor `.claude/hooks/test-gate.sh` reagiert nur auf `.cs` (Zeile 39). Vitest läuft nur in CI
  (`ci.yml:202`).
- Seit dem 2026-11-19 ist nichts gepusht, also hat auch CI den Stand nie gesehen.
- Die zweite Lücke dieses Laufs gehört in dieselbe Familie: Zwei gezielte Vitest-Dateien waren grün, erst der volle Lauf
  fand `Lektion.test.tsx`.

**Vorgeschlagener Mechanismus (nur Vorschlag, Freigabe 3 — nicht gelandet):** Das Stop-Tor ruft zusätzlich
`vitest run` auf, wenn `frontend/src/**/*.ts(x)` von `HEAD` abweicht. Das kostet rund 45 s, und genau wie beim Backend
läuft es nur bei Abweichung und nur bis zum ersten Grün. Rot blockt dann wie beim Backend.
**Bewusst ungedeckt:** E2E bleibt außerhalb des Tors (13 min, Ausweichports).

## Sprint 99 — Ziel & Umfang

**Sprint-Ziel:** Aus Sicht des Sohnes stellt ihm die Arcade keine Falle mehr. In der Bonusrunde landet das Sonderzeichen
in der Karte, die er gerade tippt. Und eine Meldung aus dem Üben kommt an, auch wenn er einen Test vorher mit „Später
weiter“ verlassen hat. Widerlegbar: `rueckmeldung.spec.ts` 5b oder die Fokus-Tests der Bonusrunde sind rot.
**Umfang:** [B-685](backlog/B-685-abgebrochener-test-sperrt-rueckmeldung-im-ueben.md) (S, beides),
[B-671](backlog/B-671-bonus-sonderzeichen-landet-im-falschen-kaestchen.md) (S, frontend), beide `Defekt`, geschätzt im
Vorbereitungslauf.
**Entwickler-Brief:**
- B-685 baut das Backend zuerst: Sperre und Fehlercode entfernen, Vertrag und Beispiele neu erzeugen. Danach kommen die
  Frontend-Texte und die E2E.
- B-671 folgt dem Fokus-Muster aus `SohnTest.tsx:89-102`, ohne `LetterBoxes` anzufassen.
- Keine Migration. Der Vertrag schrumpft nur um einen Fehlercode, das ist kein Bruch.

## Iteration 99 — umgesetzt

Einzelheiten, Proben und Mutanten stehen im `## Verlauf` der beiden Stories.

- **Rote Proben:**
  - B-685 Integrationstest: erwartet 201, gemessen 409.
  - B-685 E2E: 5b und 6 je rot an der richtigen Zeile.
  - B-685 AK-2-Mutante: rot.
  - B-671: 2 failed gegen den Ausgangsstand.
- **Review:**
  - `pugling-reviewer` (B-685): 🟡 Summary (Kommentar, ohne Zählung), 🟡 409-Attribut (gezählt), 🟢 Schnappschuss
    erweitert, 🟢 Vermerk in B-586.
  - `frontend-reviewer` (B-671): 🟡 Absicht nach der letzten Karte (gezählt), 🟡 Fortsetzungstest (gezählt), 🟢 → B-778.
  - Das Stop-Tor fing einen Compilefehler in meinem erweiterten Test (`TestItemResults` gibt es nicht als `DbSet`),
    behoben vor dem Commit. Er zählt nicht als Review-Fund, weil kein Reviewer ihn meldete; er steht hier als Messung.
  - **Zähler Sprint 99: 3 von 5.**
- **Verifikation:** Backend 1931/1931, Vitest 1301/1301, E2E 179/179. `dotnet build Pugling.sln` hat keine Warnungen.
  `dotnet format --verify-no-changes` ist auf den geänderten Dateien sauber; die übrigen WHITESPACE-Meldungen liegen in
  unberührten Dateien und sind das bekannte lokale CRLF-Artefakt.

## Runde 3 — Rollengang Sprint 99

- **Sohn — abgenommen:** „Ich hab den Test auf später gelegt, beim Üben trotzdem gemeldet, und es kam an. Am Ende vom Test
  konnte ich auch melden.“ (`rueckmeldung.spec.ts`, 5b und 6.)
  **Geliefert, Geräte-Prüfung offen** für B-671: Am echten Handy einmal eine französische Bonusrunde tippen und prüfen, ob
  das „î“ in der neuen Karte landet.
- **Vater — Regressionszeuge:** Er sieht und erledigt die Meldung (Schritt 2), seine Specs sind grün.
- **Creator — Regressionszeuge:** Die Zählung an der eigenen Übung (Schritt 3) ist grün.

Commit `1793475e`, B-671 und B-685 `abgenommen`.

## Retrospektive (Sprint 99)

**Nachschau:** Sprint 98 nachgesehen (B-774, B-775, `nachgeschaut: 2026-11-21`):
- Diff `9ab66ecc` gegen die Akzeptanzkriterien gelesen.
- Die leere Region ist außerhalb des Flusses der Flex-Spalte.
- Keine ungefilterte `getByRole("status")`-Spec trifft die neue Region.
- Die Selektoren tragen erneut.
- Kein Befund.

**Was die eigenen Tore durchgelassen haben:**
- Beide 🟡 aus dem Frontend-Review sind „Zusicherung, die den Ausgangszustand prüft“: Der Lade-Test war im
  Ausgangszustand grün und blieb es auch ohne Wächter. Diese Regel steht schon im Root-`CLAUDE.md`, und ich habe sie beim
  Schreiben trotzdem verfehlt. Die Story hatte den Fall sogar als Festschreibung beschriftet, ehrlich, aber schwach.
- Das zweite 🟡 (eine Absicht, die ohne Ziel liegen bleibt) stand als Risiko 1 in der Schätzung, und ich habe es beim
  Bauen übergangen.

**Vorschlag (nur Vorschlag, nicht gelandet):** Kein neues Tor. Die Mutanten-Probe je Fall (`mutant-probe.py`) verlangt
`pm-loop` Step 5 schon; ich habe für B-671 von Hand gemessen und nur die Ausgangsstand-Probe gefahren. Der Vorschlag ist
eine Verfahrenszeile: Ein Fall mit der Beschriftung „Festschreibung“ bekommt trotzdem eine Mutante, die ihn rot macht,
sonst wird er gestrichen. Das ist der B-157-Befund aus dem Root-`CLAUDE.md` in einem fünften Fall.
**Bewusst ungedeckt:** Fokus am echten Handy (Geräte-Prüfung oben).

## Sprint 100 — Ziel & Umfang

Zweiter Start am Nachmittag mit dem Auftrag „nachtlauf“ und freier Auswahl. Es gelten die Freigaben 1–10.

**Auswahl:** Kandidaten aus der Nachschau von Sprint 97 waren B-776 und B-777. Gewählt wurde der Shop als Themengebiet:
- [B-776](backlog/B-776-wechsel-auf-einmalig-loest-nie-aus.md), Defekt, P2;
- [B-772](backlog/B-772-bestand-setzen-antwortet-mit-altem-geschwisterbestand.md), Defekt, P3.

B-777 (Vater-Startseite) gehört zu einem anderen Gebiet und bleibt Kandidat für den nächsten Sprint.

**Verfeinerung:** Beide Stories gingen autonom von `idee` bis `geschaetzt` (`art: Defekt`). Ist-Stand und Szenario sind am
Code nachgerechnet. Größen: B-776 S, B-772 XS. Beide ohne Migration und ohne Vertragsbruch.

**Sprint-Ziel:** Aus Sicht des Vaters gilt, was er im Shop einstellt, für jedes Kind gleich. Ein „einmalig zum Termin“
füllt zum Termin auf, und die Antwort auf „Bestand setzen“ zeigt die Geschwister so, wie sie sind. Widerlegbar ist das
durch einen Bestand, der nach dem Termin leer bleibt, oder durch ein Geschwister mit altem Wert in der Antwort.

**Entwickler-Brief:**
- Quelle der Wahrheit ist der Bestand je Kind (`ShopListingStock.LastRefilledAtUtc`), gelesen über
  `ShopRefillSchedule.DueRefills`.
- Senken: der PATCH für alles, was die Regel ändert, und `ToViewAsync` für die Vater-Sicht eines geladenen Angebots.
- Guard: Die alte Regel wird gebucht, bevor ihr Zeitpunkt geleert wird (Muster B-771).
- Testweg: Integrationsfälle in `ShopStockPerChildTests`, Rollengang über die Shop-E2E-Specs.

## Iteration 100 — umgesetzt

- **B-776:** `ShopController.UpdateListing` leert über die neue `ShopService.ClearRefillInstant` die Zeitpunkte aller
  Bestände. Das geschieht bei einem Wechsel auf „einmalig“ oder bei einem neuen Termin, nachdem die alte Regel gebucht ist.
  Ein unverändert mitgeschickter Termin leert nichts.
- **B-772:** `SetStockAsync` antwortet nach dem Speichern über `ToViewAsync`. Geschrieben wird weiter nur der Bestand des
  einen Kindes.
- **Rote Probe:** Der Shop-Filter stand vorher bei 4 failed / 22 passed, nachher bei 81/81. Die Zahlen je AK stehen im
  `## Verlauf` der Stories.
- **Mutanten:** M1–M7, alle rot am benannten Test, gemessen nach der letzten Änderung. Die Tabellen stehen in den Stories.
- **Volle Suite (Release):** 1936/1936 vor den beiden 🟢-Korrekturen, auf dem Endstand nach `a0b73724` **1937/1937**.

### Review Sprint 100

`pugling-reviewer`: kein 🔴, kein 🟡, vier 🟢. **Zähler Sprint 100: 0 von 5** (Freigabe 10: 🟢 zählt nicht).

1. 🟢 Ein Wechsel auf „einmalig“ ohne Datum verwendete den gespeicherten alten Termin weiter. Durch diesen Fix hätte er
   sofort ausgelöst. → **behoben, ohne Zählung:** `400 validation_error` mit Test `B776_Wechsel_Auf_Einmalig_Verlangt_Ein_Datum`
   und Mutante M7. Das Vater-Web trifft es nicht, denn `planIncomplete` sperrt „einmalig“ ohne Datum.
2. 🟢 `ClearRefillInstant` bumpte auch Zeilen ohne Zeitpunkt. → **behoben, ohne Zählung** (die Zeile wird übersprungen).
3. 🟢 Eine Bestandszeile, die zwischen dem Laden und dem Speichern des PATCH entsteht, bekommt das Leeren nicht ab. Dieselbe
   Lücke hat `RestartRefillClock` (B-771), das Fenster ist winzig. → **bewusst offen**, hier vermerkt.
4. 🟢 Ein `RefillAtUtc` mit einem anderen Offset als `Z` gilt als neuer Termin, weil das Feld nicht auf UTC normalisiert
   wird. Das liegt außerhalb des Diffs. Dazu kommt der Nebenbefund: Ein aus SQLite gelesener Termin geht ohne `Z` über den
   Draht. → **beides offen, als Notiz**. Eine Story lohnt erst, wenn ein Client daran falsch rechnet. Das Vater-Web schneidet
   auf das Datum zu und rechnet deshalb nicht falsch.

## Runde 4 — Rollengang Sprint 100

Der Server wurde nach der letzten Änderung von Playwright frisch gestartet, auf den Ausweichports 5201/5174 (Heimnetz auf
:5200). Gefahren wurden `taschengeld-monatlich`, `shop-verlauf`, `shop-verlauf-wiederholen` und `full-flow`: **4 passed**.

- **Vater — abgenommen:** „Ich stelle um und gebe einen Termin, und jedes meiner Kinder kriegt es zum Termin. Setze ich den
  Bestand des einen, zeigt mir die Antwort die anderen richtig.“
  - Belegt durch AK 1–4 von B-776 und AK 1–3 von B-772 (Integrationsfälle gegen die API).
  - Im Browser bearbeitet und „füllt“ er Angebote (`taschengeld-monatlich`).
  - Den Wechsel auf „einmalig“ fährt keine E2E-Spec am Bestand. Der Beleg dafür ist der Integrationstest.
- **Sohn — Regressionszeuge:** Er kauft leer und sieht „ausverkauft, wieder da:“ (`taschengeld-monatlich`, `shop-verlauf`).
  Keine Sohn-Fläche liegt im Diff.
- **Creator — Regressionszeuge:** `full-flow` ist grün, keine Creator-Fläche liegt im Diff.

## Retrospektive (Sprint 100)

**Nachschau:** Sprint 99 nachgesehen (B-685, B-671, `nachgeschaut: 2026-11-21`):
- B-685: Nach Resten der entfernten Sperre über Backend, Frontend, Doku und Wiki gegrept: keine.
- B-671: Der Fokus-Effekt in `SohnBonus.tsx:222-240` verbraucht die Absicht auch nach der letzten Karte.
- Kein Befund.

**Was die eigenen Tore durchgelassen haben:**
- Der Reviewer-Fund 1 entstand aus der eigenen Entscheidung 4 („ein Termin in der Vergangenheit feuert sofort“). Gedacht
  war sie für einen Termin, den der Vater ausdrücklich setzt. Der PATCH führt aber `dto.X ?? listing.X` zusammen, und dadurch
  kann ein **weggelassenes** Feld einen Altwert tragen, den niemand gesehen hat. Keine meiner Mutanten konnte das finden, weil
  kein AK den Fall „Feld weggelassen, Altwert vorhanden“ hatte.

**Vorschlag (nur Vorschlag, nicht gelandet):** Kein neues Tor. Eine Verfahrenszeile in `backend/CLAUDE.md` unter
„PATCH-Semantik“: Eine Story, die am Wechsel eines Feldes eine Wirkung auslöst, nimmt den Fall „Feld weggelassen,
gespeicherter Altwert“ als eigenes AK auf. Der zusammengeführte Wert (`dto.X ?? entity.X`) ist dann nicht das, was der
Nutzer gerade gesehen hat. Ein Tor wäre hier nicht mechanisch formulierbar, denn welche Felder eine Wirkung auslösen, ist
Fachlogik.

**Bewusst ungedeckt:** die Hinweise 3 und 4 aus dem Review.

## Offene Roadmap (Stand nach Sprint 100)

Nächster Kandidat ist [B-777](backlog/B-777-heute-status-offen-trotz-antworten.md) (Defekt, `idee`, Vater-Startseite).
Danach kommen die geschätzten Defekte B-672 und B-639. Alles Weitere steht in [docs/backlog/](backlog/INDEX.md).
