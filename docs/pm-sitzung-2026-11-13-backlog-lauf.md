---
tags: [typ/protokoll, bereich/backlog]
---

# PM-Sitzung: Backlog-Lauf 2026-11-13 (PO und Stakeholder in einer Hand)

**Datum:** 2026-11-13  ·  **Moderation:** PM (Claude)
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (~11, Student) · Entwickler
**Ziel:** Den offenen Bestand um mindestens 30 % senken – neu entstehende Stories eingerechnet – mit Sprints, die
zusammen rund 30 % der offenen Stories tragen.

## Freigabe

Auftrag des Nutzers im Dialog am 2026-11-13 (wörtlich): „Arbeite das Backlog als PO durch und übernehme auch die
Position des Stakeholders beim grillen. […] Bei Fragen übernehme auch die Position des Stakeholders und nimm die von
Dir Empfohlenen Vorschläge.“ Damit gilt für diesen Lauf ausdrücklich auch das Grillen von `Wunsch` und `Frage`
(abweichend von README → „Der Backlog-Lauf“, Zusatz 1). Jede so getroffene Entscheidung trägt im `## Verlauf` den
Vermerk **„Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13)“** und ist damit umstoßbar.

Bewusst **nicht** umgestoßen: ausdrückliche frühere Entscheidungen des Nutzers (z. B. B-251/B-459: lokales
Vision-Modell). Halt-Regel: die feinere Nachtlauf-Fassung (behebbare Review-Funde sofort im selben Sprint beheben).

Gearbeitet wird im Worktree `.claude/worktrees/backlog-lauf` (Branch `auto/backlog-lauf-2026-11-13`), weil im
Haupt-Checkout die nicht committete B-534-Arbeit einer anderen Sitzung liegt.

## Ausgangsbestand

**52 offene Stories** (Index nach `b0653edb`; B-534 gehört der parallelen Sitzung und zählt nicht). Ziel: **höchstens
36 offen** am Ende, jede in diesem Lauf neu angelegte Story mitgezählt.

## PM-Synthese & Priorisierung

Beobachtung aus dem Abgleich am selben Tag (`b0653edb`): keine Story ist gegenstandslos, aber drei Sammel-Stories
tragen keine eigene Arbeit, und die meisten offenen `Frage`-Stories warten nur auf eine Entscheidung, die der Code
schon eng macht. Der rote Faden: **Fairness und Verlässlichkeit dort, wo das Kind übt und der Vater anlegt** – erst
das Üben (wo der Sohn Ungerechtigkeit sofort merkt), dann die Integrität des Katalogs (wo stiller Datenverlust droht),
dann Restposten.

| Prio | Story | Größe | wo | Sprint |
| --- | --- | --- | --- | --- |
| P2 | B-531 Fokus nach dem Aufdecken, B-532 Netzfehler beim Bewerten | XS/S | frontend | 1 |
| P3 | B-433 Vorschau verbraucht Einführung, B-435 „nur neue“ fragt Einführung nie, B-437 Artikel-Toleranz nur bei Nomen, B-422 Beinahe-Treffer | S | backend | 1 |
| P2/P3 | B-404, B-407, B-152, B-208, B-328, B-410 (Katalog, Vertrag, Wächter) | S | backend | 2 |
| P2/P3 | B-432, B-445, B-465, B-474, B-384, B-408 | XS–S | gemischt | 3 |
| – | B-217, B-247, B-374 Sammel-Stories | – | – | geschlossen (geteilt) |

Nicht in diesem Lauf: große Wünsche (B-09, B-20, B-39, B-218, B-220, B-248, B-249), Gerätechecks (B-31), Stories,
die Dateien der parallelen B-534-Arbeit berühren (B-16, B-140, B-369, B-380).

## Refinement

Drei Agenten haben die 18 Kandidaten parallel bis `geschaetzt` gebracht (`cf07416f`); jede Entscheidung trägt den
Stakeholder-Vermerk. Zwei Abweichungen von der jeweiligen PM-Empfehlung, beide begründet in der Story:

- **B-422:** Im Üben wird ein Tippfehler **nicht** verziehen, nur gekennzeichnet („Falsch geschrieben“). Die
  Empfehlung (Abstand 1 als Treffer) widerspräche der Nutzer-Entscheidung 2 aus B-524 („falsch geschrieben ist
  falsch“) – frühere Nutzer-Entscheidungen stößt dieser Lauf nicht um.
- **B-432:** Der Einführungstag einer täglichen Pflicht ist nur **malusfrei**, nicht erfüllt (keine Ziel-Punkte).
  Berührt den „Stick“ – zur Kenntnis des Nutzers.

Bewusst außerhalb der Sprints: **B-407** (Dialog in `ExerciseEditModal.tsx`, das B-534 unkommittiert umbaut), der
Planbau-Teil von **B-408** → neue Story **B-540** (`wartet_auf` B-534). **B-410** (M, neues Tor) kommt als letzte
Story des Laufs, weil ein neues Tor laut README den Lauf anhält.

## Sprint 69 — Ziel & Umfang

**Sprint-Ziel:** Der Sohn erlebt das Üben fair und verlässlich – der Fokus bleibt auf der Karte, ein Netzfehler
schluckt keine Bewertung, die Vorschau des Vaters stiehlt keine Einführung, „nur neue“ fragt die eingeführten Wörter
ab, die Artikel-Toleranz gilt nur für Nomen, und ein Tippfehler ist als solcher erkennbar.
**Umfang:** B-531, B-532 (Arcade) · B-435, B-437, B-433, B-422 (Backend, B-422 mit Anzeige im Blatt).
**Entwickler-Brief:** Quelle der Wahrheit bleibt der Server (`PositionPracticeController.Review`,
`PositionPlayService`, `ExerciseContentResolver`, `AnswerGrader`); die Arcade zeigt nur an. Guards: Rollen-Prüfung
`User.IsStudent()` wie im Test-Controller; kein Vertragsbruch, nur das additive Feld `misspelled`. Keine Migration.
Testweg: Integrationstests je Story, Vitest/RTL für `SohnPractice`, Mutanten-Probe je AK-Fall, volle Suite.

## Iteration 69 — umgesetzt

Commit `699b02a7`. Backend zuerst (B-435 `ScopeMatch` mit Tag, B-437 `MayDropArticle`, B-433 Rollen-Wand und
`noVerdict`, B-422 `ReviewOutcome.Misspelled`), dann die Arcade (B-531 Fokus-Effekt an `[phase, idx]`, B-532
Halt mit „Noch einmal senden“/„Überspringen“, B-422 Etikett im Blatt). Verifikation: Backend **1555/1555**,
Vitest **937/937**, `tsc -b` sauber; Mutanten: 16/16 Backend, 22/22 Frontend rot am benannten Test.

Review: `frontend-reviewer` ohne Blocker, zwei Sollte-Funde zu B-532 (Neuaufbau der Antwortfläche verliert die
Markierung; Feld nicht wirklich gesperrt) **im selben Sprint behoben**. `pugling-reviewer` ohne Blocker; der
Kommentar zur Einführungs-Wand behauptete mehr, als sie hält – eingegrenzt, die Frage dahinter ist **B-542**. Aus dem
Bau: **B-541** (Fokus im Abschlusstest, dazu der `plan_inactive`-Fall aus dem Review).

## Runde 1 — Rollengang / Abnahme (Sprint 69)

Playwright gegen frisch gestartete Server auf Ausweichports (API 5211, Web 5184), nach dem letzten Edit:
`lektion`, `erstbegegnung`, `karten-umdrehen`, `full-flow`, `test-durchgefallen-ueben`, `uebung-abbruch`, `pfad`,
`bonus`, `artikel-pflicht` – **10 passed**.

- **Sohn — abgenommen:** „Nach dem Aufdecken bin ich gleich bei den Knöpfen, und wenn das WLAN hängt, ist meine
  Antwort nicht einfach weg.“ Gefühl (Ton der Meldung) ist nicht maschinell beurteilt.
- **Vater — Regressionszeuge:** Suite grün; seine Ebene ist nur über die Wand von B-433 berührt (seine Vorschau
  verbraucht nichts mehr).
- **Creator — Regressionszeuge:** Suite grün, kein Creator-Pfad im Diff.

## Retrospektive (Sprint 69)

**Nachschau:** B-524, B-525, B-526, B-533 (die zuletzt abgenommenen vor diesem Lauf) mit `pugling-reviewer` gegen ihre
Commits nachgesehen, `nachgeschaut: 2026-11-13` gesetzt. Gefunden: **ein** Defekt in B-533 – nach erreichtem
Tagesdeckel verbrennt eine Einmal-Übung für 0 Münzen, weil der Deckel nur bei der Abgabe greift → **B-543**
(`entgangen_bei: [B-533]`), dazu ein Randfall an derselben Stelle. B-524/B-525/B-526 sauber.

**Was die eigenen Tore durchgelassen haben:** Zwei Funde dieses Laufs haben dieselbe Form wie der aus der Nachschau –
eine Grenze, die nur an einem Ende des Weges wirkt: der B-533-Deckel (nur bei der Abgabe) und die B-532-Sperre
(sperrte die Knöpfe, nicht das Feld). Mutanten konnten das nicht finden: sie mutieren, was gebaut ist, nicht das
fehlende Gegenstück. Gefunden hat es jeweils ein Reviewer, der am anderen Ende gesucht hat.

**Der eine Mechanismus:** eine Prüfregel im Brief des `pugling-reviewer` (`.claude/agents/pugling-reviewer.md`,
„Eine Grenze gilt an beiden Enden des Weges“) – kein neues Tor, darum kein Halt. Bewusst ungedeckt: das Frontend
(die gleiche Regel im `frontend-reviewer` wäre die zweite Zeile; erst messen, ob sie dort trägt).

## Sprint 70 — Ziel & Umfang

**Sprint-Ziel:** Der Vater verliert beim Pflegen von Katalog, Kind und Pflicht nichts still – kein fachloser Katalog,
keine unsinnige Schulart, kein verschobener Lernstand, und eine neue Pflicht kostet nicht schon am ersten Tag.
**Umfang:** B-404, B-152, B-208, B-328, B-445, B-432 (alle Backend).
**Entwickler-Brief:** Quelle der Wahrheit Server; Sperren mit `ApiErrors`-Codes, Meldungen nennen nur die Sperre;
Seed darf eine bestehende DB nie verändern; Malus-Regel nur in `SettleClosedPeriodsAsync`, gebuchte Zeilen bleiben.

## Iteration 70 — umgesetzt

Commit `f88464e8`. Backend **1569/1569**, Mutanten 25/25 + 18/18 nach den Review-Fixes rot. `pugling-reviewer`: keine
Blocker, vier Funde **im selben Sprint behoben** – B-404 nannte gesperrte Auswege; B-328 versprach mehr, als der
Zählvergleich hält; B-432 bestrafte Tage **vor** dem Anlegen der Position (Fenster jetzt ab
`max(CreatedAt, StartDate)`, AK 7); B-445 schrieb den Schutz der echten DB der falschen Sperre zu.

## Runde 2 — Rollengang / Abnahme (Sprint 70)

Playwright auf Ausweichports (API 5213, Web 5186), Server nach dem letzten Edit: **23 von 24 grün**. `lehrwerke`
sicherte den Zustand vor B-445 zu („Kein Profil passt“) und ist angepasst (einzeln grün). `vater-von-null` bleibt rot –
zwei „Speichern“ auf der Kind-Seite seit `f9df2087` (vor diesem Lauf); das ist der Gegenstand der parallelen B-534.

- **Vater — abgenommen:** „Ich kann kein Fach mehr wegwerfen, an dem Übungen hängen, und eine Pflicht, die ich
  mittwochs anlege, kostet nicht rückwirkend Montag und Dienstag.“ Noch offen bei ihm: die Kind-Seite mit zwei
  „Speichern“ (B-534).
- **Creator — abgenommen:** „Ich sehe beim Bearbeiten, wenn eine Kürzung den Lernstand verschieben würde – und die
  Meldung sagt ehrlich, was sie nicht erkennt.“
- **Sohn — Regressionszeuge:** Suite und seine Specs grün.

## Retrospektive (Sprint 70)

**Nachschau:** Sprint 69 (B-531, B-532, B-433, B-435, B-437, B-422) ist durch seine Reviews und den Rollengang frisch
belegt; die eigentliche Nachschau dieses Laufs folgt am Ende für alle drei Sprints gemeinsam (Index-Zähler).

**Was durchkam:** Drei der vier Review-Funde betreffen **Text, der mehr verspricht als der Code** (Meldung B-404, Meldung
B-328, `<summary>` B-432 „first due day“, Testkommentar B-445). Mutanten können das nicht sehen – sie messen Verhalten,
nicht Versprechen. **Der eine Mechanismus:** keiner neu; die bestehende Regel „Meldungen nennen die Sperre“ (pm-loop
Step 5) hat B-404 gefangen, aber erst im Review. Bewusst ungedeckt: `<summary>`- und Kommentar-Wahrheit hat kein Tor;
ein Tor dafür ist nicht denkbar ohne Sprachverständnis – das bleibt Reviewer-Arbeit.

## Sprint 71 — Ziel & Umfang

**Sprint-Ziel:** An den Rändern geht nichts verloren – der Sohn behält im Abschlusstest den Fokus, eine Einmal-Übung
verbrennt nicht am vollen Deckel, der Vater wählt im Assistenten nach Bewertung, und der KI-Creator verwirft keinen
gültigen Lückentext.
**Umfang:** B-541, B-543, B-408 (nur Assistent), B-465, B-474; B-384 nach Messung verworfen.
**Entwickler-Brief:** Frontend zuerst parallel zum Backend-Bau von Sprint 70 (keine geteilten Dateien, kein `dotnet`);
B-543 serverseitig an **beiden** Enden des Weges (Start und Abgabe), die Arcade zeigt nur an.

## Iteration 71 — umgesetzt

Commit `45365c66`. Backend **1577/1577**, Vitest **960/960**, `tsc -b` sauber; Mutanten 23/23 (Frontend), 11/11 + 5/5
(B-543), 4/4 (B-465), 7/7 (B-474) rot. **B-384** nach Messung verworfen (Meldungsplatz bei 393 × 830 im Bild,
507,75 px). Reviews ohne Blocker; behoben im Sprint: B-543-Meldungen versprachen bei Deckel 0 ein „morgen“, das es
nicht gibt; `IsOneShot` als eine Definition; `raw/`-Pflicht in `kacheln.ps1` erzwungen; ein Nebenfund aus B-533
(drei Bonus-Codes fehlten in `PROBLEM_KEYS`). Neu: **B-545** (Randfall aus B-543).

## Runde 3 — Rollengang / Abnahme (Sprint 71)

Playwright auf Ausweichports (API 5214, Web 5187), Server nach dem letzten Edit: **13 passed**.

- **Sohn — abgenommen, mit einem Handblick offen:** „Im Test springe ich nach dem Aufdecken gleich zu den Knöpfen; und
  wenn ich meine 30 Münzen schon habe, steht bei der Mathe-Übung ‚morgen wieder‘ statt dass sie einfach weg ist.“ Den
  vollen Deckel fährt keine Spec – Handblick am Gerät bleibt.
- **Vater — abgenommen:** „Im Assistenten sehe ich die Sterne und kann die besten nach oben holen, ohne meine Auswahl
  zu verlieren.“
- **Creator — abgenommen, mit Live-Lauf offen:** B-465 per Tests belegt; der Lauf mit dem echten Modell (AK 5, „kein
  Tor“) ist Handschritt.

## Retrospektive (Sprint 71)

**Was durchkam:** Wieder **Text, der mehr verspricht als der Code** (B-543: „morgen“ bei Deckel 0) – das dritte Mal in
diesem Lauf, und wieder hat es erst der Review gefunden. Dazu ein Defekt aus B-533, der seit der Abnahme bestand: tote
englische `problem.*`-Schlüssel, weil `PROBLEM_KEYS` von Hand gepflegt wird.
**Der eine Mechanismus:** Der Reviewer schlug ein Tor vor („jeder Schlüssel aus `en.sohn.problem` steht in
`PROBLEM_KEYS`“). Es ist **gemessen** sinnvoll (drei tote Schlüssel gefunden), aber ein neues Tor hält den Lauf an –
darum hier **nicht** gebaut, sondern als Vorschlag an den Menschen: `PROBLEM_KEYS` exportieren und in
`arcadeSpielweg.test.tsx` gegen `Object.keys(en.sohn.problem)` prüfen (~10 Zeilen). Bewusst ungedeckt bis dahin.

## Sprint 72 — Ziel & Umfang

**Sprint-Ziel:** Der Sohn bekommt ehrliche Rückmeldung – der Test misst ohne Tipp, nach dem Test landet „Üben“ nicht im
Nichts, Missionen sagen ihre Frist, eine Regelaufgabe erklärt sich nach der Antwort, die Woche zählt gegen die
eigene Vorwoche, und der Tempo-Bonus misst ab der sichtbaren Karte.
**Umfang:** B-544, B-522, B-528, B-529, B-253, B-530. Nicht dabei: **B-223** (M, Migration und Vertragsbruch; die
Auslegung von G-5 soll der Nutzer sehen, bevor gebaut wird).
**Entwickler-Brief:** ein Entwickler nacheinander (alle berühren den Student-Vertrag); B-530 als letzte, weil sie die
Migrationskette verlängert (nullable Spalte, nur angehängt). Anti-Cheat: der Sichtbar-Stempel setzt die Server-Uhr,
der Client meldet nur das Ereignis.

## Iteration 72 — umgesetzt

Commit `ff6a08fb`, Review-Fixes `fbd78e04`. Migration `20261113224441_CardShownAt` (nullable, nur angehängt).
Backend **1599/1599**, Vitest **998/998**, `tsc -b` sauber; Mutanten je Story rot (3/3, 8/8, 7/7, 8/8, 7/7, 6/6;
Review-Fixes 6/6 + 3/3 + 9/9).

## Merge und Code-Review über den ganzen Lauf

Auf Wunsch des Nutzers (2026-11-13, spät): „Merge am Ende sämtliche Branches zum Main. Führe ein Code-Review durch,
fixe sämtliche Findings und commite ein vollständiges, aktuelles Programm.“

- `auto/fertigstellung` (B-534 Vater-Web aufräumen, Heimnetz-Startskript, B-535/B-536) in den Lauf gemergt
  (`ae9fb08a`); Konflikte nur in `lehrwerke.spec.ts` (B-445 + B-534 kombiniert) und im generierten Index.
- **`prototyp/lernpfad-b525` bewusst nicht gemergt:** sein einziger Commit heißt „PROTOTYP (Wegwerf) … Nicht nach main
  mergen“ und hängt eine Route in `App.tsx` an.
- Review (`pugling-reviewer` + `frontend-reviewer`) über `b0653edb..HEAD`: **keine Blocker**, alle Funde behoben in
  `fbd78e04` – B-253 zählte Stornierungen als verdient, B-522 Doppelrolle/Heartbeat, B-530 verspäteter Stempel,
  B-534 (Enter im Übungsdialog, verworfene Eingaben beim Reiterwechsel, Meldung fern vom AddPanel, Ladefehler ohne
  „Erneut laden“), dazu die veraltete Fachlehrer-Suche nach dem Speichern der Klasse (im E2E-Lauf gefunden) und ein
  Login-Flake im E2E-Helfer. Ein Fund war gemessen keiner (405 kommt schon vom Router; nur der Kommentar war falsch).

## Runde 4 — Rollengang / Abnahme (Sprint 72 und Gesamtstand)

Volle Playwright-Suite auf Ausweichports (API 5216, Web 5189), Server nach dem letzten Edit: **145/145 passed** – auch
`vater-von-null`, das seit `f9df2087` rot war und durch B-534 wieder grün ist.

- **Sohn — abgenommen, Handblick offen:** „Im Test gibt es keinen Tipp mehr, nach dem Test sagt mir das Üben, was noch
  geht, und die Regel steht nach der Antwort da.“ Woche und Frist im Tab „Erfolge“ sind per RTL belegt – Handblick.
- **Vater — abgenommen:** Kind-Hub verliert beim Reiterwechsel nichts, Meldungen stehen dort, wo er klickt.
- **Creator — Regressionszeuge:** Suite und Specs grün.

## Retrospektive (Sprint 72 und Lauf)

**Nachschau:** Das Gesamt-Review über `b0653edb..HEAD` war die Nachschau für Sprint 69–71: an deren 17 Stories kein
weiterer Defekt, `nachgeschaut: 2026-11-14` gesetzt. Index danach: **446 von 473** nachgesehen.

**Was durchkam:** Sprint 72 hatte vor dem Gesamt-Review **kein** Einzel-Review – und genau dort lagen vier der Funde
(B-253, B-522 ×2, B-530). Die Reviews je Sprint in 69–71 haben ihre Sprints sauber gehalten; das Auslassen kostet
messbar. Durchgehend in diesem Lauf: **Text, der mehr verspricht als der Code** (Meldungen B-404, B-328, B-543,
Kommentare B-432, B-445, Program.cs) – sechs Fälle, alle nur vom Review gefunden.

**Der eine Mechanismus:** keiner neu (ein neues Tor hielte laut README an); der Vorschlag aus Sprint 71
(`PROBLEM_KEYS` gegen `en.sohn.problem`) bleibt beim Menschen. Bewusst ungedeckt: Wahrheit von Meldungen und
Kommentaren – das bleibt Reviewer-Arbeit, und ein Sprint ohne Review ist ein Sprint mit ungeprüften Versprechen.

## Abschluss des Laufs

| Zahl | Wert |
| --- | --- |
| offen zu Beginn (`b0653edb`) | 52 |
| neu entstanden im Lauf | 7 (B-540 … B-546) |
| abgenommen | 23 (Sprint 69: 6, 70: 6, 71: 5, 72: 6) |
| verworfen | 4 (B-217, B-247, B-374 geteilt; B-384 nach Messung) |
| **offen am Ende (Scope des Laufs)** | **32 = −38 %** |
| offen im Index gesamt | 35 (inkl. B-534/B-535/B-536 der parallelen Sitzung) = −33 % |
| in Sprints eingearbeitet | 24 von 52 = 46 % |

### Liste für den Menschen

- **Handblicke am Gerät:** voller Bonus-Deckel (B-543), Tab „Erfolge“ mit Woche und Frist (B-253, B-528).
- **Live-Lauf:** B-465 AK 5 – `ki-creator plan` gegen eine Kopie der `pugling.db` mit dem lokalen Modell.
- **Zur Kenntnis (Stakeholder-Entscheidungen):** B-422 (Tippfehler nur gekennzeichnet, nicht verziehen – wegen
  B-524), B-432 (Einführungstag nur malusfrei, nicht erfüllt), B-223 (Auslegung von G-5 – vor dem Bau ansehen).
- **Tor-Vorschlag:** `PROBLEM_KEYS` gegen `en.sohn.problem` prüfen (Sprint-71-Retro).
- **Neue Migration:** `CardShownAt` – vor dem ersten Start gegen die echte DB per Kopie proben (Startskript sichert).
- **Nicht gemergt:** `prototyp/lernpfad-b525` (Wegwerf-Prototyp).

## Stakeholder-Entscheidungen ohne Sprint

- **B-217, B-247, B-374 verworfen (geteilt):** Eine Sammel-Story trägt keine eigene Arbeit; der Index führt jede
  Teil-Story einzeln. Kosten: der Vergleichsplan ist nur noch über `docs/` und die Teil-Stories erreichbar.
