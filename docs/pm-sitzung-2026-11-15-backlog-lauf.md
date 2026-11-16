---
tags: [typ/protokoll, bereich/backlog]
---

# PM-Sitzung: Backlog-Lauf als PO und Stakeholder

**Datum:** 2026-11-15  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (Friedrich/Theodor, Student) · Entwickler
**Ziel:** Offene Stories im Index auf höchstens 0,7 × N senken (alles im Index gezählt), mindestens 30 % von N in
Sprints eingearbeitet; Stakeholder-Rolle beim Grillen ausdrücklich übertragen, auch für `Wunsch` und `Frage`.

## Auftrag und Ausgangswert

- Branch `auto/backlog-lauf-2026-11-15` im Worktree `.claude/worktrees/backlog-lauf-1115`, abgezweigt vom HEAD des
  Haupt-Checkouts (`auto/fertigstellung`, `28a6e99a`).
- Der erste Indexlauf im Haupt-Checkout meldete **62 offen**. Die Zahl ist falsch als Ausgangswert:
  `auto/fertigstellung` lag 3 Commits vor und **31 Commits hinter** `main` – ihm fehlten die Sprints 73–76 des Laufs
  vom 2026-11-14. Der Lauf-Branch muss am Ende per Fast-Forward auf `main`, trägt `main` also ohnehin. Darum zuerst
  `main` hineingemergt (`d39770b1`, Konflikte nur in B-249 und dem Index) und **dann** gezählt:
  **N = 28 offen** (493 abgenommen, 35 verworfen). Mit 62 wäre das Ziel ohne eine Zeile Arbeit erfüllt gewesen.
- Zielmarken: offen am Ende **≤ 19** (0,7 × 28 = 19,6); eingearbeitet **≥ 9** (30 % von 28 = 8,4).
- Entscheidungen, die ich als Stakeholder treffe, tragen den Vermerk „Stakeholder-Entscheidung im Auftrag des
  Nutzers". Frühere ausdrückliche Entscheidungen des Nutzers bleiben.
- Parallele Sitzung: der Haupt-Checkout trägt ungesicherte Änderungen am Skill `lehrwerk-aufbereiten` und an
  `docs/api-examples/catalog.md` – Stories, die diese Dateien berühren, stelle ich zurück.

## Abgleich gegen den Code

Vier Agenten haben die 28 offenen Stories parallel gegen den Code geprüft. Ergebnis:

| Urteil | Stories |
| --- | --- |
| gilt | B-552, B-538, B-536, B-539, B-580 (vorher B-540), B-547, B-219, B-218, B-248, B-249, B-250, B-221, B-376, B-31, B-39, B-410, B-09, B-377, B-582, B-252, B-223 |
| teilweise erledigt, Kern gilt | B-220 (`PenaltyCoins` schon im Vertrag), B-583 (B-529 liefert die Erklärung für Regel-Aufgaben), B-584 (Konsole kann `--use-weak` + `assign`, es fehlt der Griff im Vater-Web), B-369 (Zeilen gewandert) |
| veraltet, neu zuschneiden | B-47 (Azure-Weg tot; der gefahrene Betrieb ist `scripts/heimnetz.ps1`) |
| **verworfen** | **B-581** (geteilt – Sammel-Story ohne eigene Arbeit, Muster B-247) · **B-251** (gegenstandslos durch B-470, gemessen in B-459) |

### Buchhaltungs-Funde beim Abgleich

- **Doppelte Ids.** `auto/fertigstellung` hatte B-540…B-544 neu vergeben, während `main` dieselben Nummern im Lauf
  vom 2026-11-14 abgenommen hatte. Umgezogen: B-540 → **B-580** (Plan aus Unit), B-541 → **B-581**, B-542 → **B-582**,
  B-543 → **B-583**, B-544 → **B-584**; Verweise in B-537, B-248, B-249, B-09, `docs/obsidian.md` und
  `docs/rockettutor-vergleich-plan.md` nachgezogen. Kein Tor sieht doppelte Ids – Vorschlag für die Retro.
- **Konfliktmarken.** Mein Merge-Commit `d39770b1` trug unaufgelöste Marken in B-09 und B-248: die Merge-Ausgabe war
  per `tail` abgeschnitten, und `git add -A` hat die Dateien als gelöst markiert. Behoben in `d41df00a`.

Nach dem Abgleich: **26 offen**.

### Zurückgestellt (nicht in diesem Lauf)

- **B-31** – wartet auf ein echtes Handy.
- **B-536** – XL, Karte; geht in den Dialog.
- **B-09, B-248, B-249, B-582** – L/XL mit Migration, hängen aneinander (Klassenarbeit-Kette) bzw. an der Teilung.
- **B-218, B-220, B-250** – Queue-Kette, hängt an B-248.
- **B-221, B-376** – Gems/Auszeichnungen, Migration; nachrangig zu den P1.
- **B-223** – Vertragsbruch an `PracticeCard`; nachrangig.
- **B-369** – L, Teilung nötig (Vater-Web/Arcade); würde erst eine Story mehr erzeugen.
- Parallele Sitzung im Haupt-Checkout arbeitet am Skill `lehrwerk-aufbereiten` – keine der gewählten Stories berührt ihn.

## Refinement

Drei Agenten treiben je drei Kandidaten bis `geschaetzt` (nur Doku, getrennte Dateien): Vater-Plan (B-552, B-539,
B-580), Vater-Sicht (B-584, B-39, B-377), Sohn/Entwickler (B-219, B-47, B-583). B-538, B-547, B-252 und B-410 waren
schon geschätzt.

Ergebnis Refinement (alle Entscheidungen als „Stakeholder-Entscheidung im Auftrag des Nutzers“ markiert): B-552 (S),
B-584 (M), B-39 (L → M, Kind-Hub-Reiter gestrichen, höchstens 92 Tage je Abfrage), B-377 (M, Migration), B-219 (M,
Migration), B-47 (S, neu zugeschnitten auf ein wiederverwendbares `scripts/artefakt-smoke.ps1` plus CI-Job; die
Azure-gebundenen Entscheidungen 1–2 durchgestrichen), B-583 (S, eingeengt auf Vokabel-Items, Migration). An der Kopie
der echten DB gemessen: **0** Positionen und **0** Übungen in der Lage von B-552.

## Sprint 77 — Ziel & Umfang

**Sprint-Ziel (Vater):** Der Vater kann jede Positions-Einstellung speichern und wieder auf „Standard“ setzen, und keine
Einstellung, die er (oder der Creator als Vorgabe) setzen kann, macht eine Pflicht unerreichbar.
**Umfang:** B-547, B-552.
**Entwickler-Brief:** Senke ist **eine** Regel: `PositionPlayService.LearningCheckOf(type, requireTypedTest, baseStage,
schedule)` – dieselbe Rechnung zeigt dem Vater „unerreichbar“ und lehnt jetzt Anlegen, PATCH (sobald er
`requireTypedTest`/`stage`/`clearStage`/`stageSchedule` berührt) und die Creator-Vorgabe ab. Die alte Leiterprüfung
geht darin auf. B-547 nach dem Muster `ClearStage` (Wert, dann Schalter); leere Listen heißen „Methoden-Vorgabe“.
Keine Migration (0 Bestandsfälle).

## Iteration 77 — umgesetzt

- Backend: `UpdatePositionDto.ClearItemCount`/`ClearGoalThreshold`; leere Listen = Methoden-Vorgabe (PATCH und Anlegen);
  `LearningCheckOf` über lose Werte, `RequireTypedTestValidation.UnreachableProblemText` als einzige Prüfung (Anlegen,
  PATCH bei Änderung einer Hälfte, Creator-Vorgabe über `DefaultPairProblem`). Ein bestehender Test
  (`PlanPositionCrudTests`) legte genau die jetzt abgelehnte Kombination an und wurde auf Stufe 4 gesetzt.
- Frontend: Merkwerte `itemCountStored`/`goalThresholdStored`, Schalter im Update-DTO, „Bearbeiten“ übernimmt frisch;
  Assistent: eine Regel für Häkchen-Wegfall und Satz an den Vater; Feldhilfe ergänzt.
- Review-Funde (Backend 1 + 5 Aufräumpunkte, Frontend 2 + 3) im Sprint behoben, Einzelheiten in B-547/B-552. Bewusst offen
  und ohne eigene Story: englische `validation_error`-Meldung im Formular (wie schon die Leiterregel), Stufe 2 als Kopie
  der Vokabel-Skala im Assistenten (das Manifest trägt keine Stufenart).
- Mutanten (nach dem letzten Edit, Debug): 18 + 8 Zeilen, **alle rot am benannten Test**; Tabellen in den Stories.
- Volle Suite **1670/1670**, Vitest **1093/1093**, `tsc -b` grün.

## Runde 77 — Abnahme

- **Vater – abgenommen:** Playwright gegen frisch gestartete Server (5251/5191): neue Spec
  `position-zurueck-auf-standard.spec.ts` – „Inhalte“ auf 5, gespeichert, im zweiten Durchgang geleert, Zeile ohne
  „Inhalte“; Häkchen an bei „Standard der Übung“ → Ablehnung sichtbar, Zeile unverändert. Dazu sechs Nachbar-Specs
  (Assistent, Klassenarbeit-Assistent, Planbau, Feldhilfe, Lernkontrolle, Vier-Wochen-Ziel): **13 passed**.
- **Creator – Regressionszeuge:** Creator-Vorgabe-Prüfung im Integrationstest; `feldhilfe`, `ziel-vier-wochen` grün.
- **Sohn – Regressionszeuge:** kein Sohn-Pfad im Diff; Suite grün.

## Retrospektive 77

**Nachschau:** B-550, B-551 (Abschluss des Laufs vom 2026-11-14) – sauber; die Fluchtstelle von B-550 lag schon als
B-552 vor und ist jetzt geschlossen. `nachgeschaut: 2026-11-15` gesetzt.

- **Was die eigenen Tore durchgelassen haben:** (1) mein eigener Merge-Commit mit Konfliktmarken (Ausgabe per `tail`
  gekürzt), (2) doppelte Story-Ids zwischen zwei Branches – beides vor dem Sprint, beides von Agenten gefunden, nicht von
  einem Tor; (3) im Sprint selbst „Text verspricht mehr als der Code“ zweimal (Bestandsschutz nur für API-Aufrufer;
  Assistent nimmt das Häkchen still weg) – beide vom Review gefunden.
- **Der eine Mechanismus:** Prosa im treibenden Skill (`pm-loop`, Step 4): nach jedem Merge `git diff --cached --check`,
  Merge-Ausgabe nie kürzen, doppelte Ids prüfen.
- **Vorschlag für ein Tor (nicht gebaut, hält sonst an):** `backlog-index.sh` bricht bei doppelter Id ab bzw. meldet sie
  im Index – heute zählt es zwei Dateien mit derselben Id stumm als zwei Stories.
- **Bewusst ungedeckt:** dass ein PATCH-Auslöser „gesendet“ statt „geändert“ prüft, findet weiter nur ein Review.

## Sprint 78 — Ziel & Umfang

**Sprint-Ziel (Sohn):** Zusätzliches Üben lohnt sich fair – schwerer bringt mehr, Wiederholen am selben Tag weniger, freie
Positionen bringen nichts –, und der Sohn kann sich ein fremdes Wort vorlesen lassen, ohne dass das je die Lösung verrät.
**Umfang:** B-538, B-252.
**Entwickler-Brief:** B-538 nach den Entscheidungen 4–7 des Nutzers (B-537): Münzsperre an der einen Buchungsstelle der
Übung (`PositionPracticeController`), Sätze in Optionen **und** `appsettings.json`, Halbierung bei der Abgabe aus den heute
abgeschlossenen Läufen, Wert je Übung im Vorrat und in der Runde. B-252: die Regel am Inhalt (`ContentItem.SpeechLang`),
an der Karte nur außerhalb der Hörstufe; im Frontend ein Knopf nur mit passender Stimme. Keine Migration.

## Iteration 78 — umgesetzt

- Backend und Frontend wie in B-538/B-252 beschrieben; drei Bonus-Specs an die neuen Regeln angepasst (Rechen-Übung
  bleibt im Vorrat, 3 🪙 je Treffer, Deckel 90).
- Review-Funde: Backend 6 + Aufräumen, Frontend 1 blockierend (Hinweis versprach den vollen Satz) + 6 – alle im Sprint
  behoben. Zwei Stakeholder-Entscheidungen beim Bau (in B-538): Vokabeln bei 0 % weiter spielbar, Selbsteinschätzungs-Läufe
  zählen für die Halbierung (wörtlich Entscheidung 6 des Nutzers).
- Mutanten (nach dem letzten Edit): **25 Zeilen, alle rot am benannten Test**.
- Volle Suite **1678/1678**, Vitest **1102/1102**, `tsc -b` grün.

## Runde 78 — Abnahme

- **Sohn – abgenommen:** Playwright gegen frisch gestartete Server (5252/5192): `bonus` (Rechen-Übung bringt 6 🪙, steht
  danach mit „heute noch 50 % wert“ weiter im Vorrat), `bonus-deckel`, `bonus-deckel-voll`, `lektion`, `pfad`,
  `erstbegegnung`, `belohnung-trotz-wochenziel`, `truhe` und `position-zurueck-auf-standard` – **10 passed**.
  **Am Gerät offen:** das Vorlesen (Chromium in Playwright hat keine Stimmen) – Klang und Erscheinen des Knopfs am Handy
  gehören zum Gerätecheck B-31.
- **Vater – Regressionszeuge:** Positions-Editor blendet bei „frei“ die Punkte aus (Vitest), `position-zurueck-auf-standard`
  grün.
- **Creator – Regressionszeuge:** kein Creator-Pfad im Diff; Suite grün.

## Retrospektive 78

**Nachschau:** B-547, B-552 (Sprint 77) – das Frontend-Review dieses Sprints las `PlanPositions.tsx` (Positionszeile,
Editor) mit; kein Durchrutscher. `nachgeschaut: 2026-11-15` gesetzt.

- **Was die eigenen Tore durchgelassen haben:** „Text verspricht mehr als der Code“ in Serie – neun Texte über das alte
  „zahlt nur einmal“ an Stellen, die der Diff nicht berührte (API-Fehler, Swagger, Vater-Seite, Feldhilfe). Das Skript
  `old-phrases.py` fand nur Tests, die alte Wendungen **erwarten**, nicht Doku, die eine geänderte **Regel** beschreibt.
- **Der eine Mechanismus:** keiner gebaut. Argument: eine Regeländerung hat keinen maschinenlesbaren Namen, nach dem sich
  greppen ließe („einmal“ ist überall). Der Entwickler-Brief nennt künftig die alte Regel als Suchwort – das habe ich hier
  zu spät getan.
- **Bewusst ungedeckt:** Missionen an freien Positionen (gewollt, nicht getestet).

## Sprint 79 — Ziel & Umfang

**Sprint-Ziel (Vater):** Der Vater sieht seine Kinder über die Zeit und macht aus den schwachen Wörtern eines Kindes mit
einem Griff eine tägliche Pflicht.
**Umfang:** B-584, B-39.
**Entwickler-Brief:** Zwei neue, additive Endpunkte. B-584: ein Service als einzige Stelle, die aus Wörtern eine Übung
baut – nur Wort-Ids im Rumpf, nie eine Config (sonst ein Hintereingang am Creator-Pfad vorbei); alle Prüfungen vor dem
ersten Schreiben, Schreiben in einer Transaktion. B-39: die vorhandene Tagesrechnung je Tag, nur gruppiert; höchstens
92 Tage. Keine Migration.

## Iteration 79 — umgesetzt

- B-584 und B-39 wie in den Stories beschrieben; neue Fehlercodes `review_already_in_plan`, `review_subject_mismatch`;
  `WordMasteryResponse` trägt das Sprachpaar.
- **Am laufenden Produkt gefunden:** die E2E zeigte, dass die Herkunftsübung selbst als „schon im Plan“ zählt – richtig
  vom Server, aber der deutsche Satz behauptete „schon als Wiederholung“. Berichtigt, Integrationstest dazu.
- Review-Funde: Backend 1 Blocker (Endpunkt-Zähler 282 → 284, schlägt nur im Dispose an – „Passed!“ täuschte) + 6,
  Frontend 1 Blocker (zwei Sprachen eines Kindes) + 9 – alle behoben bis auf einen echten Rest: **B-558** (abgeschaltete
  Pläne fallen aus der Zeitachse).
- Mutanten (nach dem letzten Edit): **27 Zeilen, alle rot am benannten Test**.
- Volle Suite **1693/1693** (Exit-Code 0 geprüft), Vitest **1113/1113**, `tsc -b` grün.

## Runde 79 — Abnahme

- **Vater – abgenommen:** Playwright gegen frisch gestartete Server (5253/5193): `vater-zeitachse` (Tagespflicht heute
  erfüllt → Balken „1 von 1 Zielen – Pflicht geschafft“, Tag/Monat, Link), `schwache-woerter-zuweisen` (Test falsch →
  Lernstand → Knopf → zweiter Klick 409 → Kind sieht die Pflicht), dazu `vater-von-null`, `responsive-vater`,
  `perspektiven`, `kind-anlegen-id`, `position-zurueck-auf-standard` – **13 passed**.
- **Sohn – Regressionszeuge:** sieht die neue Wiederholung als Tagespflicht auf dem Pfad (in der Spec); keine
  Arcade-Datei im Diff.
- **Creator – Regressionszeuge:** die Wiederholungs-Übung erscheint im Katalog als private Übung des Vaters; Suite grün.

## Retrospektive 79

**Nachschau:** Sprint 78 (B-538, B-252) übernimmt der Gesamt-Review im Abschluss (er liest alles seit Laufbeginn).

- **Was die eigenen Tore durchgelassen haben:** (1) Ich habe zwei Sprints lang nur „Passed!“ gelesen – der
  `EndpointCoverageGuard` meldet im Dispose und setzt nur den Exit-Code; das Review fand es am Zählerstand der Datei.
  (2) Der Blocker „Kind mit zwei Sprachen“ war im Datenmodell sichtbar (Friedrich lernt Französisch **und** Englisch),
  aber weder Story noch E2E hatten zwei Sprachpaare.
- **Der eine Mechanismus:** Prosa im treibenden Skill wäre zu schwach; die wirksame Änderung ist meine eigene Praxis seit
  diesem Sprint: Suiten-Läufe mit `echo exit=$?` – im Protokoll belegt. Vorschlag für ein **Tor** (nicht gebaut): das
  Test-Gate-Hook-Skript wertet den Exit-Code von `dotnet test` aus, nicht die Zeile „Passed!“ – vorher nachsehen, ob es
  das schon tut.
- **Bewusst ungedeckt:** gleichzeitige identische Zuweisungen (zwei Übungen möglich), dokumentiert.

## Sprint 80 — Ziel, Umfang, Abnahme

**Sprint-Ziel (Creator/Entwickler):** Der Creator kann jedem Vokabel-Wort eine Erklärung geben, die der Sohn erst nach
seiner Antwort sieht, und jedes veröffentlichte Artefakt wird vor dem Betrieb mechanisch gestartet und befragt.
**Umfang:** B-583 (vorher B-556), B-47. B-410 (neues Tor) bleibt draußen – auf Wunsch des Nutzers endet der Lauf mit
diesem Sprint.

- B-583: Spalte `ExerciseItems.Explanation` (Migration angehängt, gegen eine Kopie der echten DB angewendet, 305 Items
  erhalten), Vertrag additiv, Feld im Bearbeiten-Dialog; der Lader-Feldabgleich (`laden.py`) kennt das Feld.
- B-47: `scripts/artefakt-smoke.ps1` + CI-Job `artefakt` mit dauerhafter Gegenprobe (Exit 2 = rot an der Seitenprüfung).
- Mutanten: 5/5 rot. Review: durch den Gesamt-Review (unten) mit abgedeckt.

## Abschluss

1. **Merge `auto/fertigstellung`** (`c3411038`): die parallele Sitzung hatte dieselben Kollisionen B-540…B-547 bemerkt
   und auf **B-580…B-588** ausgewichen, während dieser Lauf auf B-553…B-557 umgezogen war. Übernommen sind **ihre**
   Nummern (B-553→B-580 … B-557→B-584), repo-weit in Code, Tests und Doku; Verlaufszeilen beider Seiten zusammengeführt,
   die Skill-Dateien `lehrwerk-aufbereiten` von ihrer Seite. Neu aus ihr: B-585 (Sammel-Story, hier geteilt), B-586,
   B-587, B-588.
2. **Gesamt-Review** (Backend + Frontend, alles seit `d41df00a`): kein Blocker. Behoben: die B-552-Ablehnung kam beim
   Vater englisch an (eigener Code `typed_test_unreachable` + deutscher Satz, E2E prüft ihn); veraltete
   `ReviewOutcome`-Doku; Umgebungsvariablen des Smoke-Skripts blieben in einer offenen Shell stehen; die Gegenprobe prüfte
   nur „nicht 0“ statt „rot an der Seitenprüfung“ (jetzt Exit 2, eigener Port); feste Münzzahlen im Vater-Text;
   veraltete Summaries/Kommentare. Bewusst offen (Entscheidung 6 von B-552): eine spätere Änderung der Übungs-Vorgabe
   wird nicht gegen bestehende Positionen geprüft – die Plan-Seite zeigt sie rot; Doku sagt es jetzt so.
3. **Belege nach dem letzten Edit:** volle Suite **1697/1697** (Exit-Code 0), Vitest **1114/1114**, `tsc -b` grün,
   Gegenprobe lokal Exit 2; volle Playwright-Suite **158 passed** (Ports 5254/5194, frische Server).

### Zähltabelle

| Größe | Wert |
| --- | --- |
| N (offen zu Beginn, nach Merge von `main`) | 28 |
| neu im Index (Lauf: B-558; parallele Sitzung: B-585, B-586, B-587, B-588) | 5 |
| abgenommen | 8 (B-547, B-552, B-538, B-252, B-584, B-39, B-583, B-47) |
| verworfen | 3 (B-581 geteilt, B-251 gemessen/gegenstandslos, B-585 geteilt) |
| **offen am Ende (Index)** | **22** = 79 % von N – **Ziel ≤ 19 (0,7 × N) nicht erreicht** |
| eingearbeitet (abgenommen + nach Messung verworfen) | 9 = 32 % von N – Ziel ≥ 30 % erreicht |

Warum das Ziel verfehlt ist: der Lauf endete auf Wunsch des Nutzers nach Sprint 80 (Kontextgrenze), und die parallele
Sitzung legte vier Stories an, die nach Auftrag mitzählen. Ohne sie stünde der Index bei 18.

### Liste für den Menschen

- **B-251 verworfen** (gegenstandslos durch B-470, gemessen in B-459) – Deine Entscheidung G-8 (lokales Modell) ist
  damit beantwortet, nicht umgestoßen; eine Wortliste **ohne** Buch wäre eine neue Story.
- **B-538, Entscheidung 6 wörtlich umgesetzt:** auch Selbsteinschätzungs-Läufe zählen für die Halbierung – am ersten Tag
  einer neuen Liste zahlt die erste Auswahl-Runde darum nur 25 %. Bitte bestätigen oder auf „nur geprüfte Läufe“ ändern.
- **Shop-Preise prüfen** (B-537 Entscheidung 7): Bonus getippt 3 statt 2, Deckel 90 statt 30.
- **Am Gerät:** Vorlesen (B-252) und Klang/Haptik (B-31) – Knopf erscheint nur mit passender Stimme.
- **B-539/B-580 (P1, ein Plan je Fach, Plan aus Unit):** geschätzt, aber L+L mit Vertragsbruch; B-539 sollte vorher in
  Teil A/B geteilt werden (Vorschlag in der Story). Nicht autonom begonnen.
- **B-410 (neues Tor):** bewusst nicht gebaut.
- **Tor-Vorschläge aus den Retros** (nicht gebaut): `backlog-index.sh` meldet doppelte Ids; das Test-Gate wertet den
  Exit-Code statt „Passed!“ (der `EndpointCoverageGuard` schlägt nur im Dispose an).
- **Branch `auto/fertigstellung`** im Haupt-Checkout: ist mit diesem Lauf in `main`; der Haupt-Checkout trägt weiter
  ungesicherte Änderungen (`docs/api-examples/catalog.md`, `.claude/launch.json`) der parallelen Sitzung.
