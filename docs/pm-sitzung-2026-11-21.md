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

## Sprint 101 — Ziel & Umfang

**Umfang:** [B-777](backlog/B-777-heute-status-offen-trotz-antworten.md), Defekt, P2, autonom von `idee` bis `geschaetzt`.
Größe XS, die Reparatur liegt entgegen der ersten Notiz im **Backend**.

**Sprint-Ziel:** Aus Sicht des Vaters widerspricht der Status eines Kindes auf der Startseite nicht mehr seiner
Antwortzahl. Wer heute geantwortet hat, steht auf „dran“. Widerlegbar ist das durch ein Kind mit Antworten unter „offen“.

**Entwickler-Brief:**
- Quelle der Wahrheit ist `ChildDay.Practiced`, also die Senke im Vertrag. Der einzige Leser ist `VaterDashboard.tsx:113`,
  und das Frontend bleibt unverändert.
- Keine Migration, kein Vertragsbruch.
- Testweg: Integrationsfall, dazu eine neue Rollengang-Spec.

## Iteration 101 — umgesetzt

- `ChildrenDashboardService`: `Practiced = GoalsMet > 0 || points > 0 || answers > 0`. Dazu die Vertragszeile an
  `ChildDay` (`v1.json` neu erzeugt).
- **Rote Probe:** erwartet `true`, gemessen `false`. Mutanten M8, M9 und M8e (E2E) alle rot, Tabelle in der Story.
- **Volle Suite (Release):** 1938/1938.
- **Review** (`pugling-reviewer`): zwei 🟢, **Zähler 0 von 5**.
  - Die schärfere Vertragszeile ist übernommen.
  - Kein Leser hing an der alten Bedeutung „mindestens ein Ziel“.
  - Der geplante B-592 (Erinnerung „noch nicht geübt“) passt zur neuen Bedeutung besser als zur alten.

## Runde 5 — Rollengang Sprint 101

Der Server wurde nach der letzten Änderung von Playwright frisch gestartet, auf 5201/5174. Gefahren wurden
`heute-dran-nach-antworten` (neu), `vater-schmal` und `full-flow`: **4 passed**.

- **Vater — abgenommen:** „Mein Kind hat vier Antworten gegeben und noch nichts geschafft, und die Startseite sagt ‚dran‘,
  nicht ‚offen‘.“
- **Sohn — Regressionszeuge:** Sein Test läuft in der Spec über die API, keine Sohn-Fläche liegt im Diff, `full-flow`
  ist grün.
- **Creator — Regressionszeuge:** `full-flow` ist grün.

## Retrospektive (Sprint 101)

**Nachschau:** Sprint 100 nachgesehen (B-776, B-772, `nachgeschaut: 2026-11-21`):
- B-776 gegen die einzige schreibende Stelle für `RefillAtUtc` gelesen (`VaterShop.tsx:228`, immer Mitternacht mit `Z`).
  **Befund:** Ein per API mit Uhrzeit angelegter „einmalig“-Termin zählt beim späteren Bearbeiten im Vater-Web als neuer
  Termin und füllt erneut auf → [B-782](backlog/B-782-einmal-termin-mit-uhrzeit-faellt-beim-bearbeiten-erneut.md)
  (`idee`, `entgangen_bei: [B-776]`).
- B-772: kein Befund.

**Was die eigenen Tore durchgelassen haben:**
- B-782 ist derselbe Mechanismus wie der Reviewer-Fund 1 aus Sprint 100. Ein Vergleich „neu gegen alt“ sieht den Wert,
  den der **Client zurückschickt**, und das ist nicht unbedingt der Wert, den er geladen hat. In Sprint 100 war es das
  weggelassene Feld, hier ist es das abgeschnittene.
- Keine Mutante kann das finden, solange kein AK einen Client hat, der den Wert verändert zurückschickt.

**Vorschlag (nur Vorschlag, nicht gelandet):** Der Vorschlag aus Sprint 100 wird erweitert. Löst eine Story am
**Wechsel** eines Feldes eine Wirkung aus, prüft sie zwei weitere Fälle: das weggelassene Feld (`dto.X ?? entity.X`) und
den Rundweg des echten Clients (geladen, umgeformt, zurückgeschickt). Für einen Tag-Termin heißt das: ein Fall mit
Uhrzeit. Weiterhin kein Tor, aus demselben Grund wie in Sprint 100.

**Bewusst ungedeckt:** Übt der Vater im Plan des Kindes, macht das `practiced` wahr. `ReviewEvents` haben keine
Akteur-Spalte, und dieselbe Grenze gilt seit B-770 für `AnswersToday`.

## Stand am Ende des Laufs

- **Gebaut und abgenommen:** B-776 und B-772 (`a0b73724`), B-777 (`6c128f3a`). Nichts gepusht.
- **Kein Halt erzwungen:** Kein neues Tor, und der Review-Zähler stand in beiden Sprints bei 0. Der Lauf endet nach
  Sprint 101, weil der nächste Kandidat ein anderes Themengebiet aufmacht.
- **Nächste Kandidaten:**
  - [B-782](backlog/B-782-einmal-termin-mit-uhrzeit-faellt-beim-bearbeiten-erneut.md) (Shop, `idee`, die Reparatur ist zu
    entscheiden);
  - B-672 und B-639 (geschätzte Defekte im Backend).
- **Offen beim Nutzer:**
  - die Retro-Vorschläge aus Sprint 99, 100 und 101;
  - die Geräte-Prüfung von B-671.

## Runde 6 — Dritter Start am Abend (Auftrag „starte einen Nachtlauf“)

Freie Auswahl, Freigaben nach [nachtlauf.md](nachtlauf.md). Kein neuer Befund unter `docs/anmerkungen/`. Die Rollen
wurden nicht neu durchgespielt: die Kandidaten stehen aus der Nachschau und dem Vorbereitungslauf fest (B-672, B-639,
B-782). Reihenfolge: erst B-672 (geschätzt, Malus und Serie, also der Kern „verpasste Pflicht kostet“), dann B-639
(Mathe, eigenes Themengebiet, eigener Sprint).

## Sprint 102 — Ziel & Umfang

**Umfang:** [B-672](backlog/B-672-startverlegung-zurueck-holt-unspielbare-tage-zurueck.md), Defekt, P2, S, `geschaetzt`.

**Sprint-Ziel:** Aus Sicht des Sohns kostet ihn eine Startkorrektur des Vaters weder Münzen noch die Serie für Tage, an
denen er den Plan nicht spielen konnte. Widerlegbar durch einen Malus oder einen Serienbruch an einem solchen Tag.

**Entwickler-Brief:**
- Quelle der Wahrheit ist `StudyPlan.ActivatedAt`; die einzige Stelle, die beim Verlegen schreibt, ist
  `StudyPlansController.Update` (die Senke). Die Verbraucher `PlanDueForPeriod` und `ActiveOn` bleiben unverändert.
- Alten Start vor dem Überschreiben festhalten, dann anheben auf `min(alter Start, heute)`, dann das bestehende Absenken.
- Keine Migration, kein Vertragsbruch. Testweg: `RueckwirkendePflichtTests` mit eingefrorener Uhr.

## Iteration 102 — umgesetzt

- `StudyPlansController.Update`: `oldStart` vor dem Überschreiben, Anhebung im Block `startMoved && plan.Active`;
  Spalten-Doku von `ActivatedAt` ergänzt.
- Vier neue Fälle. **Rote Proben:** AK 1 erwartet `[T+5, T+6]`, gemessen T+1..T+6; AK 2 erwartet Serie 11, gemessen 1.
- Mutanten M1–M7 alle rot am erwarteten Test (Tabelle in der Story).
- **Volle Suite (Release):** 1942/1942.
- **Review** (`pugling-reviewer`): kein 🔴; ein 🟡 außerhalb des Diffs →
  [B-783](backlog/B-783-verlaengerter-plan-holt-abgelaufene-tage-zurueck.md) (Verlängern eines abgelaufenen Plans, das
  gespiegelte Ende); drei 🟢 nachgezogen bzw. in der Story vermerkt. **Zähler 0 von 5.**

## Runde 7 — Rollengang Sprint 102

Die Wirkung braucht vergehende Tage; die laufende App hat keine verstellbare Uhr. Ein Gang im Browser kann den Fall nicht
zeigen. Ersatzbeleg: die Integrationsfälle fahren den PATCH des Vaters über die API mit eingefrorener Uhr. Regressions-E2E
nach der letzten Änderung frisch gestartet (5201/5174): `full-flow`, `vater-schmal`, `heute-dran-nach-antworten` – **4 passed**.

- **Vater — abgenommen (auf dem Ersatzbeleg):** „Wenn ich nach den Ferien den Start zurücksetze, bekommt mein Sohn keinen
  Malus für die Tage, an denen der Plan gar nicht lief.“
- **Sohn — Regressionszeuge:** keine Sohn-Fläche im Diff, `full-flow` grün.
- **Creator — Regressionszeuge:** `full-flow` grün.

## Retrospektive (Sprint 102)

**Nachschau:** Sprint 101 nachgesehen (B-777, `nachgeschaut: 2026-11-21`), kein Befund. Geprüft: alle Leser von
`practiced` gegrept (einziger Leser `VaterDashboard.tsx:113`), die neue Bedingung gegen `CountAnswersAsync` gelesen –
abgebrochene Tests und Vater-Tests zählen nicht.

**Was die eigenen Tore durchgelassen haben:** Der Fund B-783 kam vom Reviewer, nicht aus der Story. Die Ausformulierung
von B-672 hat den Anfang des Spielfensters untersucht (`StartDate`) und das Ende (`EndDate`) nicht, obwohl
`PlanPlayableForChild` beide Grenzen gleich behandelt. Dieselbe Lehre („eine Grenze gilt an beiden Enden“) steht schon
in der Retro von Sprint 69.

**Vorschlag (nur Vorschlag, nicht gelandet):** Beim Ausformulieren einer Story über ein Zeitfenster
(`StartDate`/`EndDate`, `from`/`to`) nennt der Ist-Stand ausdrücklich, was am anderen Ende passiert. Kein Tor: die Stelle
ist Prosa im Vorbereitungslauf, und ein Mechanismus müsste Fenster erkennen.

**Bewusst ungedeckt:** Malus, der vor dem Fix durch eine Zurückverlegung schon gebucht wurde (Entscheidung 3).

## Sprint 103 — Ziel & Umfang

**Umfang:** [B-783](backlog/B-783-verlaengerter-plan-holt-abgelaufene-tage-zurueck.md), Defekt, P2, im Sprint von `idee`
bis `geschaetzt` gebracht (autonom grillbar), XS. Dasselbe Themengebiet wie Sprint 102, das gespiegelte Ende.

**Sprint-Ziel:** Aus Sicht des Sohns kostet ihn das Verlängern eines abgelaufenen Plans weder Münzen noch die Serie für
die Tage, an denen der Plan schon zu Ende war. Widerlegbar durch einen Malus oder Serienbruch in dieser Lücke.

**Entwickler-Brief:** Senke wie in Sprint 102 (`StudyPlansController.Update`); ein abgelaufener, aktiver Plan, der
verlängert wird, gilt wie beim Wiedereinschalten (B-613) ab heute als eingeschaltet. Keine Migration, kein Vertragsbruch.

## Iteration 103 — umgesetzt

- `StudyPlansController.Update`: `oldEnd` festhalten, Regel `Active && neues Ende > altes Ende && altes Ende + 1 < heute`
  → `ActivatedAt = heute`. Eine unerreichbare Zusatzbedingung (`today > ActivatedAt`) aus dem ersten Entwurf gestrichen.
- **Rote Proben:** AK 1 erwartet kein Malus, gemessen T+6..T+9 (vier); AK 2 erwartet Serie 14, gemessen 1.
- Fünf neue Fälle, Mutanten N1–N6 rot (Tabelle in der Story). **Volle Suite (Release):** 1947/1947.
- **Review** (`pugling-reviewer`): kein 🔴; 🟡 Untergrenze der Lücke ohne Fall → Fall + N6 ergänzt (**Zähler 1 von 5**);
  🟡 Kosten der Entscheidung zu knapp → in der Story ergänzt (ohne Zählung); 🟢 Code-Kommentar ergänzt.

## Runde 8 — Rollengang Sprint 103

Wie in Sprint 102: Die Wirkung braucht vergehende Tage, ein Gang im Browser kann sie nicht zeigen; Ersatzbeleg sind die
Integrationsfälle über den PATCH des Vaters. Regressions-E2E nach der letzten Änderung frisch gestartet (5201/5174):
**4 passed**.

- **Vater — abgenommen (auf dem Ersatzbeleg):** „Ist der Plan übers Wochenende ausgelaufen und ich verlängere ihn am
  Montag, zahlt mein Sohn nichts für Samstag und Sonntag.“
- **Sohn, Creator — Regressionszeugen:** keine ihrer Flächen im Diff, `full-flow` grün.

## Retrospektive (Sprint 103)

**Nachschau:** Sprint 102 nachgesehen (B-672, `nachgeschaut: 2026-11-21`). Der Review-Befund war schon B-783. Zusätzlich
geprüft: der Rundweg des echten Clients (`VaterPlanDetail.tsx:159-160` schickt die Daten nur bei Änderung, im selben
Format) – kein weiterer Befund.

**Was die eigenen Tore durchgelassen haben:** Die eigene Mutantenliste prüfte die Grenze `altes Ende + 1 < heute` nur von
einer Seite (N2 lockert sie). Die Gegenseite (verschärfen) fand der Reviewer. Bei B-672 war es dieselbe Form: eine Grenze
mit Konstante hat zwei Richtungen.

**Vorschlag (nur Vorschlag, nicht gelandet):** `mutant-probe.py` könnte zu jeder Mutante, die eine Vergleichskonstante
um eins verschiebt, die Gegenrichtung anmahnen (Warnung, wenn nur eine der beiden Richtungen in der Liste steht). Das
wäre ein neues Tor, darum hier nur vorgeschlagen.

**Entscheidung zum Umstoßen:** B-783 Entscheidung 1 (die Verlängerung verwischt das alte Fenster wie das Wiedereinschalten,
Zeitachse und Metriken können sich nachträglich verbessern). Alternative wäre ein Startverlauf mit Migration.

## Sprint 104 — Ziel & Umfang

**Umfang:** [B-639](backlog/B-639-dezimalkomma-im-zahlenvergleich.md), Defekt, P2, M, `geschaetzt` (im Dialog gegrillt).

**Sprint-Ziel:** Aus Sicht des Sohns zählt eine richtig gerechnete Dezimalzahl, egal ob er sie mit Komma oder Punkt
tippt – im Test wie beim Üben, und so, wie der Vater es im Testmodus gesehen hat. Widerlegbar durch „2,5“, das als falsch
gewertet wird.

**Entwickler-Brief:** Senke ist `AnswerGrader.Matches(given, item)` (alle Spielpfade) plus `AnswerChecking.NumericMatch`
(Katalog-Check, Drill). Ein Parser an einer Stelle; Zahlen-Atome tragen ihr Ziel als `ContentItem.Numeric`, Text-Atome
bleiben unberührt. Keine Migration, kein Vertragsbruch (`ContentItem` ist intern).

## Iteration 104 — umgesetzt

- `NumericAnswer` (neu), `ContentItem.Numeric`, `AnswerGrader` (Zahl-Zweig, Tippfehler-Sperre), `NumericMatch` auf dem
  Parser, `Swap` räumt `Numeric` ab.
- **Rote Probe:** 14 von 28 Fällen der neuen Theorie rot vor dem Fix (u. a. „2,5“ auf beiden Pfaden, Toleranz beim Spielen).
- Mutanten P1–P11 und P1e (E2E) rot am erwarteten Fall. **Volle Suite (Release):** 1978/1978.
- **Review** (`pugling-reviewer`): kein 🔴; zwei 🟡 nur Testkommentare (falsch benannte rote Proben) → korrigiert, ohne
  Zählung; drei 🟢 übernommen. **Zähler 0 von 5.**

## Runde 9 — Rollengang Sprint 104

Server und Vite frisch von Playwright gestartet (5201/5174), nach der letzten Änderung.

- **Sohn — abgenommen:** neue Spec `rechnen-mit-komma`: „Ich tippe 2,5 wie in der Schule, und es zählt. 2,3 bei 7 : 3
  zählt auch. 2,5 bei 5 · 5 natürlich nicht.“ (2 / 3, mit P1 rot).
- **Vater — Regressionszeuge:** sein Testmodus nutzt denselben Parser (Katalog-Check, im Unit-Test belegt); keine
  Vater-Fläche im Diff.
- **Creator — Regressionszeuge:** `full-flow` grün.
- **Geräte-Prüfung offen:** am Handy, ob die Tastatur der Arcade ein Komma bequem anbietet (kein `inputMode="decimal"`).

## Retrospektive (Sprint 104)

**Nachschau:** Sprint 103 nachgesehen (B-783, `nachgeschaut: 2026-11-21`), kein Befund. Geprüft: die Aktivierungsregel
(`DeactivateSameSubjectPlansAsync` filtert ohne Datum, ein verlängerter Plan schaltet keinen laufenden ab) und der
Client-Rundweg (`VaterPlanDetail.tsx:160`).

**Was die eigenen Tore durchgelassen haben:** Der Reviewer fand Testkommentare, die rote Proben behaupteten, die es nicht
gab („25“ für 2,5; „1234“ für 1,234). Das ist genau die Fehlerklasse aus dem Root-`CLAUDE.md` („eine Zusicherung, die den
Ausgangszustand prüft, kann nie fehlschlagen“), wieder von einem Reviewer und von keinem Tor gefunden. Die rote Probe lief
diesmal je Theorie-Fall (14 von 28 rot gezählt), aber niemand hat die 14 grünen gegen die Beschriftung abgeglichen.

**Vorschlag (nur Vorschlag, nicht gelandet):** `mutant-probe.py` (oder ein Geschwister-Skript) fährt die neuen Tests
einmal gegen den Stand **vor** dem Fix und listet je Fall „grün vor dem Fix“. Jeder so gelistete Fall muss im Kommentar
als Absichtserklärung stehen, sonst bricht das Skript ab. Das wäre ein neues Tor.

**Bewusst ungedeckt:** der Gedankenstrich „–“ als Minus (nur vermerkt), die Ziffern-Tastatur der Arcade (Geräte-Prüfung).

## Sprint 105 — Ziel & Umfang

**Umfang:** [B-673](backlog/B-673-plan-auf-anderes-kind-umgehaengt-erbt-dessen-vergangenheit.md), Defekt, P3, M,
`geschaetzt` (Entscheidungen stellvertretend aus dem Vorbereitungslauf 2026-11-19). Dasselbe Themengebiet wie Sprint 102
und 103: wem ein Plan gehört und ab wann er Pflicht ist.

**Sprint-Ziel:** Aus Sicht des Vaters nimmt ein Plan, den er einem anderen Kind gibt, weder Serie noch Malus-Beginn des
ersten Kindes mit; einen schon gespielten Plan lässt der Server gar nicht umhängen und sagt warum. Widerlegbar durch ein
Umhängen, nach dem das neue Kind eine gebrochene Serie hat, oder durch eine englische Rohzeile.

**Entwickler-Brief:** Senke `StudyPlansController.Update`, Zweig „anderes Kind“: 404 zuerst, dann die Sperre (drei
Tabellen), dann die Zuweisung; `ActivatedAt = heute` erst, wenn feststeht, ob der Plan nach dem PATCH aktiv ist.
Fehlercode, deutscher Satz und Vitest in einem Schritt (`ProblemTextGuardTests` koppelt sie). Keine Migration.

## Iteration 105 — umgesetzt

- `ApiErrors.PlanHasData`, Sperre und Einschaltdatum im Controller, `[ProducesResponseType(409)]`, `v1.json` neu,
  deutscher Satz und Vitest.
- **Rote Proben:** AK 1 (drei Fälle) und AK 2 erwartet 409, gemessen 200; AK 3 erwartet Serie 4, gemessen 1.
- Mutanten Q1–Q8 und Q0e (E2E) rot; eine erste Gang-Probe Q2e war kein Beleg und ist ersetzt (Story).
- **Volle Suite (Release):** 1985/1985; Vitest 1302/1302.
- **Review** (`pugling-reviewer`): kein 🔴; 🟡 verrutschte Helfer-Doku (Code, **Zähler 1 von 5**), 🟡 Testkommentar
  (ohne Zählung), 🟢 zwei Satz-Hinweise übernommen.

## Runde 10 — Rollengang Sprint 105

Server und Vite frisch von Playwright gestartet (5201/5174), nach der letzten Änderung. 3 passed.

- **Vater — abgenommen:** „Ich wollte den Plan meines Großen dem Kleinen geben. Die Seite sagt mir auf Deutsch, dass
  damit schon gelernt wurde, und der Plan bleibt beim Großen.“ (Spec `plan-umhaengen-gesperrt`, mit Q0e rot).
- **Sohn — Regressionszeuge:** AK 3 (seine Serie nach dem Umhängen) läuft über die API im Integrationsfall; keine
  Sohn-Fläche im Diff, `full-flow` grün.
- **Creator — Regressionszeuge:** `full-flow` grün.

## Retrospektive (Sprint 105)

**Nachschau:** Sprint 104 nachgesehen (B-639, `nachgeschaut: 2026-11-21`), kein Befund. Geprüft: Üben und Test teilen die
Eingabekomponente; keine Spielfläche hat ein Zahlenfeld, das ein Komma verschluckt.

**Was die eigenen Tore durchgelassen haben:** Meine erste rote Probe für den Gang (Q2e) war keine: `mutant-probe.py`
meldete sie richtig als „abweichend“, von Hand blieb die Spec grün. Ursache: ein Test-Submit schreibt auch
Fortschrittszeilen, sodass eine zweite Bedingung derselben Sperre den Fall trägt. Das Werkzeug hat hier funktioniert –
die Lehre ist, für einen Gang die ganze Sperre zu mutieren, nicht eine ihrer ODER-Bedingungen.

**Vorschlag (nur Vorschlag, nicht gelandet):** In `pm-loop` Step 5 ein Halbsatz: „Für eine E2E-Gang-Probe die ganze Regel
abschalten, nicht eine Bedingung einer ODER-Kette – der Gang erzeugt oft mehr Daten als der Integrationsfall.“ Prosa,
kein Tor.

**Halt nach diesem Sprint:** Während Sprint 105 hat eine **parallele Sitzung** im selben Checkout Backlog-Dateien
geschrieben (`B-626` geändert, `B-784` neu, `INDEX.md`). Zwei schreibende Läufe im selben Checkout stören sich (DLL-Sperren, doppelte Story-Nummern); darum startet dieser Lauf
keinen weiteren Sprint; der Index wurde **nicht** neu erzeugt und die fremden Dateien sind nicht mitcommittet.

## Stand am Ende des dritten Starts

- **Gebaut und abgenommen:** B-672 (`095b6e01`), B-783 (`6ddf9b2c`), B-639 (`7ff47fde`), B-673 (`1ac42f4a`). Nichts
  gepusht.
- **Neu im Backlog:** B-783 (aus dem Review von Sprint 102, im selben Lauf gebaut).
- **Entscheidungen zum Umstoßen:** B-783 Entscheidung 1 (die Verlängerung verwischt das alte Fenster wie das
  Wiedereinschalten); B-673 Entscheidung 1 (ein gespielter Plan lässt sich nicht mehr umhängen – ein heute möglicher
  Vater-Weg fällt weg).
- **Offen beim Nutzer:** die Retro-Vorschläge der Sprints 102–105; Geräte-Prüfung Komma-Tastatur am Handy (B-639).
- **Nächste Kandidaten:** B-782 (Shop, Reparaturweg zu entscheiden), B-653, B-614.

## Runde 11 — vierter Start (Nachtlauf am Abend)

Vor dem Start geprüft: keine andere Sitzung läuft (`list_sessions`, alle `isRunning: false`). Die fremden, nicht
committeten Dateien (`B-626`, `B-784`, `INDEX.md`, `scripts/theodor-part-c-aufholen-2026-11-19.ps1`) bleiben unangetastet
und werden nicht mitcommittet.

## Sprint 106 — Ziel & Umfang

**Umfang:** [B-653](backlog/B-653-nur-neue-inhalte-verliert-offene-lernkarte.md), Defekt, P3, S, `geschaetzt`
(Entscheidungen stellvertretend aus dem Vorbereitungslauf 2026-11-19).

**Sprint-Ziel:** Aus Sicht des Sohns kommt ein Wort, das er bei der Einführung nicht aus dem Kopf schreiben konnte, auch
unter „nur neue Inhalte“ an jedem Folgetag wieder als Lernkarte, bis es sitzt – und nimmt dabei einen Platz der Tagesdosis
ein. Widerlegbar durch eine Runde am Folgetag ohne dieses Wort oder durch sechs neue Wörter neben zwei offenen.

**Entwickler-Brief:** Senke ist `PositionPlayService.DueItemIndicesAsync`, der einzige Weg zu Lernrunde und Tagesstatus.
Das Prädikat „offene Lernkarte“ wird aus `OpenLearnCards` herausgezogen und von `ScopeMatch` mitbenutzt (eine Stelle,
Entscheidung 1). `Old`/`All` unverändert, Tagestest unberührt. Keine Migration, kein Vertrag.

## Iteration 106 — umgesetzt

- `PositionPlayService`: Prädikat `IsOpenLearnCard` herausgezogen; `OpenLearnCards` und `ScopeMatch` (`ItemScope.New`)
  nutzen es gemeinsam, `DueItemIndicesAsync` reicht Typ und Inhalte durch. Fünf neue Fälle in `UmfangNeuTests` und
  `LernkarteTests`.
- **Rote Proben:** vor dem Fix 5 von 9 rot (AK 1 erwartet `[0, 2]`, gemessen `[2]`; AK 6 erwartet `dueCount` 2, gemessen 1;
  AK 4 erwartet Karten 0..5, gemessen 2..7). Mutanten Q1–Q6 und Q3b nach der letzten Änderung alle rot, jeweils am
  benannten Fall.
- **Volle Suite (Release):** 1990/1990. **Review** (`pugling-reviewer`): kein 🔴/🟡, drei 🟢 übernommen (ohne Zählung).
  **Zähler 0 von 5.**

## Runde 12 — Rollengang Sprint 106

- **Sohn — abgenommen** (über die API, In-Process-Server aus dem Stand nach der letzten Änderung): „Das Wort, das ich gestern
  nicht hinbekommen hab, kommt heute wieder als Lernkarte, auch morgen noch, bis ich es kann. Und ich krieg nicht einfach
  sechs neue obendrauf.“ Keine UI-Änderung, darum keine eigene Spec.
- **Vater — Regressionszeuge:** `full-flow` E2E 1 passed (Server frisch von Playwright auf 5201/5174); keine Vater-Fläche
  im Diff.
- **Creator — Regressionszeuge:** derselbe `full-flow`-Lauf; keine Creator-Fläche im Diff.

## Retrospektive (Sprint 106)

**Nachschau:** Sprint 105 nachgesehen (B-673, `nachgeschaut: 2026-11-21`), kein Befund. Geprüft: kein zweiter Weg setzt
`plan.ChildId` (Grep, einziger Treffer `StudyPlansController.cs:202`); Umhängen plus Startverschiebung im selben PATCH legt
das Einschaltdatum nie vor heute.

**Was die eigenen Tore durchgelassen haben:** Der Reviewer fand eine falsche Doku-Zusage aus B-643: `OpenLearnCards`
versprach, offene Karten belegten auch im Testanteil Plätze – der Testanteil ruft die Methode gar nicht auf. Kein Tor prüft
eine Doku-Behauptung über Aufrufer; die Nachschau von B-643 hat sie auch nicht gesehen.

**Vorschlag (nur Vorschlag, nicht gelandet):** keiner als Mechanismus. Begründung: eine Behauptung „gilt auch für X“ in
einer `<summary>` ist nur per Lesen prüfbar; ein Tor müsste Prosa gegen den Aufrufgraphen halten. Billiger bleibt der
Reviewer-Auftrag, der die Senke benennt und nach Pfaden darum greppen lässt – er hat es hier gefunden.

Kein Halt-Grund: kein neues Tor, kein Defekt im Increment.

## Runde 13 — fünfter Start (Nachtlauf am späten Abend)

Vor dem Start geprüft: keine andere Sitzung läuft (`list_sessions`, alle `isRunning: false`). Die fremden, nicht
committeten Dateien (`B-626`, `B-784`, `INDEX.md`, `scripts/theodor-part-c-aufholen-2026-11-19.ps1`) bleiben unangetastet;
`INDEX.md` wird darum nicht neu erzeugt.

## Sprint 107 — Ziel & Umfang

**Umfang:** [B-614](backlog/B-614-startseite-gruppiert-nach-uebungsfach-statt-planfach.md), Defekt, P3, M, `geschaetzt`
(Entscheidungen stellvertretend aus dem Vorbereitungslauf 2026-11-19).

**Sprint-Ziel:** Aus Sicht des Sohns steht jede Aufgabe eines Plans auf der Startseite unter dem Fach dieses Plans – eine
Übung aus einem fremden Lehrwerk öffnet keine zweite Fach-Karte. Widerlegbar durch eine zweite Karte für einen
Englisch-Plan mit einer Mathe-Übung.

**Entwickler-Brief:** Senke ist `PositionProgressService.ComputeDayAsync`, die einzige Stelle, die `PositionStatus` baut.
Name = Fach des Plans; fehlt die Navigation, holt die Senke ihn selbst (Entscheidung 3); Plan ohne Fach → Name der Reihe
wie bisher (Entscheidung 2). Frontend unverändert. Keine Migration, Vertragsform unverändert (nur Doc-Satz).

## Iteration 107 — umgesetzt

- `PositionProgressService`: `PlanSubjectNameAsync` (geladene Navigation, sonst Abfrage nach `SubjectId`), Rückfall
  `SeriesSubjectNameOf`; `ComputeChildDayAsync`, Plan-Übersicht und Dashboard-Zeitachse laden `Subject` mit.
  `ProgressDtos.cs` Doc-Satz. Neue Klasse `PlanFachTagessichtTests` (4 Fälle), `KindTagessichtTests.cs:143` umgeschrieben,
  E2E `startseite-plan-fach.spec.ts`.
- **Rote Probe:** vor dem Fix 2 von 3 rot (AK 1 erwartet `Englisch`, gemessen `Mathe-x 16`; AK 4 erwartet `Englisch`,
  gemessen `Mathe-x 24`). Mutanten Q1–Q4 rot, Q5/Q6 (nur Ladeoptimierung) grün wie erwartet; E2E-Probe E1 rot.
- **Volle Suite (Release):** 1994/1994. **Review** (`pugling-reviewer`): kein 🔴/🟡, zwei 🟢 übernommen (ohne Zählung).
  **Zähler 0 von 5.**

## Runde 14 — Rollengang Sprint 107

- **Sohn — abgenommen** (E2E `startseite-plan-fach`, Server frisch auf 5201/5174 nach der letzten Änderung): „Ich hab
  Englisch, und da steht auch alles von Englisch drin – auch die Wörter, die aus dem anderen Buch kommen. Keine komische
  Mathe-Karte mehr.“
- **Vater — Regressionszeuge:** `full-flow` und `belohnung-ueber-faecher` E2E grün; im Diff nur die Ladezeile der
  Dashboard-Zeitachse (`Include`), deren Zahlen den Namen nicht lesen.
- **Creator — Regressionszeuge:** derselbe `full-flow`-Lauf; keine Creator-Fläche im Diff.

## Retrospektive (Sprint 107)

**Nachschau:** Sprint 106 nachgesehen (B-653, `nachgeschaut: 2026-11-21`), kein Befund. Geprüft: `pos.Scope` hat einen
einzigen Leser (`PositionPlayService.cs:287`); Umfang und Dosis teilen `IsOpenLearnCard`. Index nicht neu erzeugt
(fremde Änderungen an `INDEX.md`).

**Was die eigenen Tore durchgelassen haben:** nichts nach außen. Innen: der übernommene 🟢-Vorschlag (`Include` in der
Plan-Übersicht) nahm AK 4 die Last, das Nachladen zu tragen – der Fall wäre danach ohne Zweck grün geblieben. Gefangen hat
es die Pflicht, nach der letzten Änderung neu zu messen (Q3 hätte sonst „grün statt rot“ gezeigt); ergänzt wurde ein
direkter Service-Fall.

**Vorschlag (nur Vorschlag, nicht gelandet):** keiner als Mechanismus. Begründung: die bestehende Regel „Mutanten nach
der letzten Änderung“ deckt genau diesen Fall; ein zweites Tor würde dasselbe messen.

Kein Halt-Grund: kein neues Tor, kein Defekt im Increment.

## Sprint 108 — Ziel & Umfang

**Umfang:** [B-608](backlog/B-608-bonus-kopf-nennt-zu-hohen-satz.md), Defekt, P3, M, `geschaetzt` (Entscheidungen
stellvertretend aus dem Vorbereitungslauf 2026-11-19).

**Sprint-Ziel:** Aus Sicht des Sohns verspricht das Bonus-Training nie mehr, als eine Karte zahlt – der Hinweis über jeder
Karte nennt ihren Satz, die Übersicht sagt „bis zu“, wo die Karten verschieden zahlen. Widerlegbar durch eine Auswahlkarte,
über der „3 🪙“ steht.

**Entwickler-Brief:** Senke ist `ExtraTrainingService.CardRate` – die eine Stelle, aus der Abrechnung (`CardRatesAsync`),
Karten (`BuildRun`) und Vorrat (`CoinsPerHit` = höchster Satz, `MixedRates`) lesen. Hier liegt Geld: die Abrechnung bleibt
wertgleich (Netz: `Getippte_Bonusrunde_Fragt_Die_Vorwaertskarte_Bei_Beiden_Als_Auswahl`). Zwei additive Antwortfelder,
keine Migration. Frontend liest den Satz der Karte und `mixedRates`.

## Iteration 108 — umgesetzt

- Backend: `CardRate` aus `CardRatesAsync` gelöst, `CardRates` je Übung; `ExtraCard.CoinsPerHit`, `ExtraExercise.MixedRates`.
  Frontend: Hinweis je Karte, „bis zu“ je Form (`MIXED_KEY`), drei neue Texte de/en. Tests: drei Fälle plus Summenzeile in der
  Lückentext-Theorie (`ExtraTrainingTests`), zwei Vitest-Fälle, E2E `bonus-satz-je-karte.spec.ts`.
- **Rote Probe:** 9 von 33 rot, nur am fehlenden Feld (`KeyNotFoundException`) – trägt nichts; die Last tragen M1–M6,
  F1–F3 und E1, alle nach der letzten Änderung rot.
- **Volle Suite (Release):** 1999/1999, Vitest 1304/1304. **Review:** ein 🟡 (von beiden Reviewern) behoben, **Zähler 1
  von 5**; 🟢 ohne Zählung behoben bzw. als [B-785](backlog/B-785-aufwaermhinweis-verspricht-mehr-ab-der-auswahl.md)
  zurückgestellt.

## Runde 15 — Rollengang Sprint 108

- **Sohn — abgenommen** (E2E `bonus-satz-je-karte`, Server frisch auf 5201/5174 nach der letzten Änderung): „Oben steht
  jetzt ‚bis zu 3‘, und bei der Buchstabenkarte steht 1, bei der zum Schreiben 3. Das passt dann auch zu dem, was ich am
  Ende kriege.“
- **Vater — Regressionszeuge:** `bonus-deckel` und `full-flow` E2E grün; keine Vater-Fläche im Diff.
- **Creator — Regressionszeuge:** derselbe `full-flow`-Lauf; keine Creator-Fläche im Diff.

## Retrospektive (Sprint 108)

**Nachschau:** Sprint 107 nachgesehen (B-614, `nachgeschaut: 2026-11-21`), kein Befund. Geprüft: `subjectName` hat einen
Leser im Sohn-Frontend (`tagessicht.ts:25`); `PositionStatus` entsteht nur in `PositionProgressService.cs:336`. Index nicht
neu erzeugt (fremde Änderungen an `INDEX.md`).

**Was die eigenen Tore durchgelassen haben:** Eine Annahme der Schätzung („gemischt kann nur Tippen sein“, mit „nicht
gefunden“ belegt) ging ungeprüft in den Bau; die Tests wählten ihre Fälle aus derselben Annahme. Gefunden haben es beide
Reviewer unabhängig. Außerdem: die erste rote Probe fiel nur am fehlenden Feld – sie sah gültig aus und trug nichts.

**Vorschlag (nur Vorschlag, nicht gelandet):** Eine „nur X ist möglich“-Annahme in `## Entscheidungen` bekommt im Bau
eine Mutante, die die Annahme selbst bricht (hier: „bis zu“ nur beim Tippen) – fällt sie nicht, fehlt der Gegenfall als
Test. Als Zeile im Verfahren (`pm-loop` Step 5), kein Tor.

Kein Halt-Grund: kein neues Tor; der Defekt im Increment ist behoben, Zähler 1 von 5 (Nachtlauf-Freigabe 3/10).
