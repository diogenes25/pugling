---
tags: [typ/protokoll, bereich/backlog]
---

# PM-Sitzung: Backlog-Lauf als PO und Stakeholder

**Datum:** 2026-11-14  ·  **Moderation:** PM
**Teilnehmer:** Creator · Vater (Supervisor) · Sohn (Friedrich/Theodor, Student) · Entwickler
**Ziel:** Offene Stories im Index um mindestens 30 % senken (alles im Index gezählt), mindestens 30 % von N in Sprints
eingearbeitet; Stakeholder-Rolle beim Grillen ausdrücklich übertragen, auch für `Wunsch` und `Frage`.

## Auftrag und Ausgangswert

- Branch `auto/backlog-lauf-2026-11-14` im Worktree `.claude/worktrees/backlog-lauf-1114`, abgezweigt von `main`
  (`40dd9bfa`). Der Haupt-Checkout steht auf `auto/fertigstellung` (`701aedba`), das ein Vorfahre von `main` ist –
  `main` trägt also schon alles; abzweigen vom älteren HEAD hätte 13 Commits verloren.
- `bash .claude/scripts/backlog-index.sh` auf `main`: **N = 35 offen** (473 abgenommen, 34 verworfen).
- Zielmarken: offen am Ende **≤ 24** (0,7 × 35 = 24,5); eingearbeitet **≥ 11** (30 % von 35 = 10,5).
- Entscheidungen, die ich als Stakeholder treffe, tragen den Vermerk „Stakeholder-Entscheidung im Auftrag des
  Nutzers". Frühere ausdrückliche Entscheidungen des Nutzers (z. B. B-535, gegrillt im Chat 2026-11-13) bleiben.

## Abgleich gegen den Code

Vier Agenten haben die 32 Stories außerhalb von B-534…B-536 parallel gegen den Code geprüft (B-534…B-536 selbst:
B-534 ist gebaut und reviewt und braucht nur die Abnahme; B-535 ist vom Nutzer gegrillt; B-536 ist groß und wird
gegrillt). Ergebnis: **keine Story erledigt, doppelt oder eine Sammel-Story** – es gab also nichts mit Beleg zu
verwerfen. Vierzehn trugen veraltete Zeilenangaben oder Zahlen und sind berichtigt:

| Urteil | Stories |
| --- | --- |
| gilt | B-39, B-218, B-220, B-248, B-249, B-410, B-545, B-09, B-20, B-219, B-221, B-250, B-251, B-252, B-376, B-377, B-527, B-546 |
| veraltet, Kern gilt (Ist-Stand berichtigt) | B-31, B-469, B-423, B-540, B-407, B-542, B-223, B-16, B-122 (sechs statt sieben Listen), B-140, B-369, B-459 (eingeengt auf `fill --images`), B-47, B-380 |

Folge für das Ziel: die −30 % kommen nicht aus dem Aufräumen, sondern aus gebauter Arbeit. Bei 35 offenen Stories
braucht es netto −11; mit einem Puffer von einer neuen Story je Sprint aus Reviews sind das vier Sprints à 4–6.

### Zurückgestellt (nicht in diesem Lauf)

- **B-31** – wartet auf ein echtes Handy (Klang/Haptik); kein Code.
- **B-47** – wartet auf den Neubau des Deploys (Azure stillgelegt).
- **B-09, B-39, B-221, B-248, B-249, B-251, B-376, B-377** – L/XL, neue Fähigkeiten mit Migration; zu groß für
  diesen Lauf, Teilung erst bei Bedarf.
- **B-218, B-220, B-250** – hängen aneinander bzw. an B-248 (Queue, Stundenplan, Klassenarbeit).
- **B-410** – ein neuer reflexiver Wächter: würde den Lauf nach Regel anhalten; bleibt dem Menschen.

## Refinement

Drei Agenten treiben je vier bis fünf Kandidaten bis `geschaetzt` (nur Doku, getrennte Dateien), mit der
übertragenen Stakeholder-Rolle: Vater-UI (B-540, B-16, B-140, B-380), Sohn/Belohnung (B-545, B-527, B-252, B-542),
Creator/Entwickler (B-122, B-459, B-20, B-469, B-423). B-535 war geschätzt; B-546 trat beim Bau von B-535 erneut auf
und wurde sofort ausformuliert, gegrillt (`Aufräumen`, autonom) und geschätzt (XS).

Ergebnis (alle bis `geschaetzt`, Entscheidungen als „Stakeholder-Entscheidung im Auftrag des Nutzers“ markiert):
B-540 (S), B-16 (M), B-140 (M), B-380 (M), B-545 (S), B-527 (M), B-252 (S), B-542 (XS), B-122 (S), B-459 (XS),
B-20 (M), B-469 (S), B-423 (M). Ich habe B-536 selbst gegrillt (fünf Empfehlungen übernommen, XL); es geht als Karte
in den Dialog. Zwei Befunde beim Belegen: B-252 braucht ein Vertragsfeld (die Karte kennt ihre Sprache nicht), B-527
findet die Kiste heute in keiner Oberfläche – das Kind sieht sie nur im Kontoauszug.

## Sprint 73 — Ziel & Umfang

**Sprint-Ziel (Sohn):** Friedrich bekommt für ehrliches Üben sichtbar Münzen und Kisten – ohne dass ihn offene
Wochenziele, ein vor seinem ersten Spiel angelegter Plan oder eine während der Runde geänderte Übung um die Belohnung
bringen.
**Umfang:** B-535, B-546, B-545, B-542.
**Entwickler-Brief:** Quelle der Wahrheit ist `PositionProgressService` (eine Senke `RewardObligations` für Kiste und
Bonus-Sperre; das Malus-Fenster in `SettleClosedPeriodsAsync`). Zwei Migrationen, nacheinander angehängt
(`NewBonusPaidAt`, `ExtraRunContentHash`), vorab gegen eine Kopie der echten DB geprobt. Die Arcade rechnet die Sperre
nicht mehr selbst nach (`rewardsUnlocked`). B-542 ändert kein Verhalten (Entscheidung „bleibt“, Regressionstest).

### Messung vor dem Bau (B-535)

An einer Kopie der echten `pugling.db` widerlegt: „Der Neu-Bonus hat nie ausgezahlt“. Theodor hat elf
`Base`-Buchungen zu je 10 Münzen (= `NewContentPoints`). Die freien Sprossen erhöhen `ReviewCount` nicht; der Bonus
ging verloren, wenn der *erste gewertete* Versuch falsch war. Darum der Backfill `ReviewCount > 0 ⇒ bezahlt` (B-535
Entscheidung 6), damit Theodor nichts doppelt bekommt. Entscheidung 4 (nachträglich angelegte Position) war durch
B-432 schon gebaut.

### Migrationsprobe

`dotnet-ef database update` gegen eine Kopie der echten DB: `CardShownAt` (aus dem Lauf 2026-11-13, auf der echten
DB noch nicht angewandt), `NewBonusPaidAt`, `ExtraRunContentHash` laufen durch; 70 Fortschrittszeilen als bezahlt
markiert, 35 offen; 11 Bestandsrunden ohne Fingerabdruck.

## Iteration 73 — umgesetzt

- **B-535** (Backend + Arcade): `RewardsUnlocked` als eine Senke; Malus ab dem ersten eigenen Spieltag; Neu-Bonus über
  `NewBonusPaidAt`; die Ziele-Karte zählt Tages- und Wochenziele getrennt (Review-Fund).
- **B-545**: Fingerabdruck der Bonus-Runde, veraltete Runde wird verworfen (`409 extra_run_stale`), Arcade räumt die
  gespeicherten Antworten.
- **B-546**: `CheckSql` – die zweite Migration des Sprints brauchte keine Handkorrektur mehr.
- **B-542**: entschieden „bleibt“, Regressionstest.
- Verifikation: Suite **1612/1612** (Release), Vitest **1000/1000**, `tsc -b` grün, **24 Mutanten** rot am benannten
  Test (Tabellen im `## Verlauf` der Stories), Migration gegen eine DB-Kopie geprobt. Commits `fa335075` (Code),
  `1f16a98c` (Abnahme).

## Runde 73 — Re-Review / Abnahme

- **Sohn – abgenommen.** Rollengang als Playwright gegen frisch gestartete Server nach dem letzten Edit: 9 passed
  (`belohnung-trotz-wochenziel`, `bonus`, `bonus-deckel*`, `pfad`, `lektion`, `erstbegegnung`, `full-flow`). Mit
  erfülltem Tagesziel und offenem Wochenziel ist das Bonus-Training offen, die Kiste gebucht, und die Karte zeigt
  „Ziele heute 1 / 1“ neben „Ziele diese Woche 0 / 1“.
- **Vater – Regressionszeuge:** Suite grün, `full-flow` grün; kein Vater-Pfad im Diff außer dem Tutorial-Satz (B-542).
- **Creator – Regressionszeuge:** Suite grün; kein Creator-Pfad im Diff.
- **Am Gerät offen:** keiner. Handgriff: Friedrichs −60 Malus vom 13.11. per „Verschenken“ ausgleichen (B-535,
  Entscheidung 4).

## Retrospektive 73

**Nachschau:** Sprint 72 (B-544, B-522, B-528, B-529, B-253, B-530) nachgesehen – ohne Fund, `nachgeschaut` gesetzt.
Nebenbefund: 20 Story-Dateien des Laufs vom 2026-11-13 trugen doppelt kodierte Umlaute (`Ã¼`, `â€“`) in den
Abnahme-Zeilen; mechanisch repariert (Zeichenfolgen zurückdekodiert, sonst nichts geändert).

- **Was die eigenen Tore durchgelassen haben:** Der Reviewer fand, dass die Einführungstag-Ausnahme (B-432) noch am
  alten Fensterbeginn hing – genau an Friedrichs erstem Spieltag hätte sie nie gegriffen. Mein eigener Test seedete
  nur einen Test ohne Einführung und hielt das falsche Verhalten fest. Dazu Texte „today's duty“ an der neuen Regel.
- **Der eine Mechanismus:** eine Zeile in `pm-loop` (Step 4): Wer eine Grenze verschiebt, greppt den Helfer der alten
  Grenze und liest jeden Aufrufer neu. Prosa, weil sich „Ausnahme an einer verschobenen Grenze“ nicht statisch
  erkennen lässt.
- **Tor-Vorschlag (nicht gebaut, hält sonst an):** ein Wächter, der in `docs/**/*.md` doppelt kodiertes UTF-8 (`Ã`,
  `â€`) ablehnt – die Umlaute des Vorlaufs sind zwei Tage unbemerkt geblieben. Liegt in der Liste für den Menschen.
- **Bewusst ungedeckt:** die Doppel-Abgabe einer veralteten Bonus-Runde (500 → 409) ist nur gelesen, nicht gemessen.

## Sprint 74 — Ziel & Umfang

**Sprint-Ziel (Vater):** Der Vater baut einen Plan, ohne dass ihm die Auswahl verloren geht, ohne Übungen zu wählen,
die er nicht zuweisen darf, und kann jede Positions-Einstellung im Formular setzen und zurücknehmen.
**Umfang:** B-540, B-16, B-140, B-469 (Kern), dazu die Abnahme von B-534 (gebaut und reviewt im Lauf 2026-11-13).
**Entwickler-Brief:** Backend zuerst und nacheinander: `ExerciseTypeManifest.Stages` über eine Senke
(`ExerciseTypeRegistry.Published`, beide Endpunkte), `UpdatePositionDto.ClearStage` (Wert, dann Schalter),
`ExerciseSummary.CanAssign` in der SQL-Projektion – gegen `CanExecuteAsync` durch einen Gleichheitstest gehalten. Danach
das Frontend in einer Hand, weil drei Stories `PlanPositions.tsx` ändern (B-540 → B-16 → B-140 → B-469). Keine Migration.

## Iteration 74 — umgesetzt

- Backend: Manifest-`stages` und `selfAssessedTest` (Review-Fund, s. u.), `clearStage`, `canAssign`. Tests:
  `ExerciseTypeManifestTests` (+2), `PatchSemanticsTests` (Schalter-Zeile), `ExerciseSharingTests` (+6 Fälle gegen
  echtes Zuweisen; der Server antwortet 403, nicht 400 wie in der Story notiert).
- Frontend (von einem Agenten gebaut, ich habe Reviews und Nachbesserungen gemacht): Auswahl-Schlüssel ohne Sortierung
  (B-540); Stufe/Inhalte/Tempo im Positionsformular, Stufe aus dem Manifest, Reset beim Übungswechsel (B-16); gesperrte
  Zeilen mit Grund in Planbau, Assistent **und Klassenarbeit** (B-140; die Klassenarbeit war zurückgestellt, das
  Review zeigte die offene Grenze – nachgezogen); „Alle wählen“ übernimmt nur Zuweisbare; die Positionszeile nennt die
  Art der Lernkontrolle (B-469).
- Review-Funde behoben: Zuordnung warnte grundlos „Selbsteinschätzung zählt“ (der Server wertet dort jeden Test
  getippt) – das Manifest trägt jetzt `selfAssessedTest` aus der Stufenregel des Typs; Grund-Satz unterstellte ein
  „Zurückziehen“, das es bei einer nie freigegebenen Übung nicht gab; Hinweis „nach dem Umsortieren“ stand auch nach
  einem Neuladen; DTO-Doku behauptete, die übrigen Felder trügen „erben“ als eigenen Wert (falsch für vier Felder →
  neue Story **B-547**); Testverweis im Kommentar zeigte auf eine nicht existierende Datei.
- Bewusst offen: der doppelt vorgelesene Grund (Span im `<label>` und per `aria-describedby`) – Kosmetik, nicht behoben.
- Verifikation: Suite **1621/1621** (Release), Vitest **1023/1023** (drei volle Läufe in Folge), `tsc -b` grün,
  **17 Mutanten** rot (N1–N16, N8b; N8 war äquivalent und zählt nicht). Commit `defa3ea0`.

## Runde 74 — Re-Review / Abnahme

- **Vater – abgenommen.** Rollengang als Playwright gegen frisch gestartete Server: 19 passed. Gesehen: eine
  zurückgezogene Übung steht im Planbau, im Assistenten und in der Klassenarbeit gesperrt mit Grund; „Alle wählen“
  überspringt sie und sagt es; die Positionszeile nennt die Lernkontrolle (getippt / Selbsteinschätzung / kein Test).
  B-534 (Reiter im Kind-Hub, ein Speichern je Fläche) mit abgenommen.
- **Creator – Regressionszeuge:** Suite grün; das Manifest ist additiv (`stages`, `selfAssessedTest`), der KI-Creator
  liest es unverändert.
- **Sohn – Regressionszeuge:** `full-flow` grün, kein Sohn-Pfad im Diff.

## Retrospektive 74

**Nachschau:** Sprint 73 läuft als Agent (Ergebnis folgt in Retro 75).

- **Was die eigenen Tore durchgelassen haben:** Der Bau-Agent hat eine Server-Regel im Frontend nachgebaut
  („ungetippter Test ⇒ Selbsteinschätzung zählt“) und für Zuordnung damit falsch gewarnt; die Klassenarbeit war per
  Entscheidung zurückgestellt, das Sprint-Ziel sprach aber von „Übungen, die er nicht zuweisen darf“ – die Grenze war
  offen. Dazu ein Vitest-Lauf, der unter Last 2–3 Fälle verlor (Wartezeit 1 s, gemessen bis 2,4 s).
- **Der eine Mechanismus:** `frontend/src/test-setup.ts` setzt `asyncUtilTimeout` auf 4 s – ein Lauf, der nur unter
  Last fällt, misst nichts und kostet Vertrauen in jede rote Zeile.
- **Bewusst ungedeckt:** der doppelt vorgelesene Grund; der Mehr-Seiten-Zweig von „Alle wählen“.

## Sprint 75 — Ziel & Umfang

**Sprint-Ziel (Sohn):** Friedrich sieht seine verdiente Kiste als Truhe auf dem Pfad, öffnet sie selbst und bekommt
den Inhalt genau einmal gutgeschrieben.
**Umfang:** B-527, dazu B-548 und B-549 (beide aus der Nachschau zu Sprint 73: Texte, die noch die alte Regel nennen).
B-252 (Vorlesen) aus dem Plan genommen: es teilt `PositionPracticeController` und die Arcade-Karte mit B-527 und
gehört nicht zum Sprint-Ziel – es läuft im nächsten Sprint.
**Entwickler-Brief:** Ziehen beim Verdienen, Buchen beim Öffnen (Streak vom Verdienen); Migration mit Bestands-`UPDATE`;
Öffnen nur durch das Kind selbst, Wallet-Invariante per bedingtem Update + Stamp in einer Transaktion.

## Iteration 75 — umgesetzt

- B-527 von einem Agenten gebaut (Backend, Migration, Endpunkt, Arcade, E2E `truhe`); Reviews fanden zehn Punkte,
  alle im Sprint behoben – darunter: ein Konto mit Kind- und Vater-Profil hätte die Truhe eines betreuten Kindes
  öffnen können (jetzt `User.ChildId() == childId`), `daily_box_none` hatte keinen Arcade-Satz und ließ die Truhe
  weiter leuchten, der Fokus fiel nach dem Öffnen auf `body`, eine gestern verdiente Truhe hieß „heute geöffnet“.
- B-548/B-549: Swagger, Kommentare, `en.ts`, Vater-Absatz.
- Verifikation: Suite **1628/1628**, Vitest **1033/1033** (zwei Läufe), `tsc -b` grün, **11 Mutanten** rot (P2 nicht
  kompilierbar, durch P2b ersetzt), Migration gegen eine DB-Kopie geprobt. Commit `f96ec5c6`.

## Runde 75 — Re-Review / Abnahme

- **Sohn – abgenommen, mit Gerätevorbehalt.** Rollengang als Playwright: 8 passed; die Truhe leuchtet nach
  erfüllten Tageszielen, öffnet auf Tipp, der Münzstand im HUD steigt um den gezeigten Betrag, nach Neuladen steht sie
  offen. Ob die Animation sich gut anfühlt, entscheidet ein Mensch am Gerät.
- **Vater – Regressionszeuge:** Suite grün; der Bonus-Absatz im Kind-Hub nennt die neue Regel.
- **Creator – Regressionszeuge:** Suite grün, kein Creator-Pfad im Diff.

## Retrospektive 75

**Nachschau:** Sprint 73 – zwei Text-Durchrutscher (B-548, B-549), beide in diesem Sprint behoben; Doku in `wiki/` und
den Tutorials direkt berichtigt. Sprint 74 läuft als Agent.

- **Was die eigenen Tore durchgelassen haben:** In Sprint 73 habe ich die Texte an der neuen Regel nur an den Stellen
  nachgezogen, die der Reviewer nannte; der Rest (Swagger-DTO, zwei Kommentare, `en.ts`, Vater-Absatz) blieb.
- **Der eine Mechanismus:** keiner neu – die Regel „eine Story über einen Satz greppt jeden Satz dieser Art“ steht
  schon in `pm-loop` Step 5; sie galt hier für eine **Regel**, nicht nur für einen Satz. Ich habe sie in diesem Sprint
  angewandt (grep nach „duty“ über Backend, Frontend und Doku).
- **Bewusst ungedeckt:** AK 3 der Truhe (paralleles Öffnen) ist ein Rennen; grün ist deterministisch, rot nicht.

## Sprint 76 — Ziel & Umfang

**Sprint-Ziel (Vater + Creator):** Der Vater bereitet eine Klassenarbeit im Assistenten mit Termin und Stufenleiter
vor, kein Formular lässt ihn ein Pflichtfeld leer speichern, und der Creator weist per Agent eine Übungsliste einem
Plan zu, ohne die API von Hand zu treiben.
**Umfang:** B-122, B-407, B-20 (Backend-Kette, ein Agent), B-423, B-380, B-459 (Frontend/Doku, ein Agent ohne dotnet).

## Iteration 76 — umgesetzt

- B-122 Paging für sechs Listen; B-407 Ausweg „Art entfernen“ im Übungsdialog; B-20 Agent-Verb `assign`; B-423
  Klassenarbeit im Assistenten; B-380 Pflichtfelder in acht Formularen (`<Pflicht />`); B-459 Warnung in Agent-Doku.
- Review-Funde behoben: zweiter Anlauf mit neuem Termin baute auf den alten Plan; Leiter versprach „getippt“ bei
  nicht getippten Stufen (jetzt: keine Leiter bei Stufe ≤ 2, Entscheidung 7); Schwellentag nicht zweistellig
  eintippbar; Exit 2 für „nichts neu“ kollidierte mit „falsch aufgerufen“ (jetzt 3); gekürztes Lesefenster endete mit
  0 (jetzt Abbruch vor dem Schreiben); mehrdeutige Tag-Schreibweise; leeres `--exercise-id`. Ein Vitest-Fall brach,
  weil das neue Art-Feld ungemockt ins Netz ging (zweiter „Erneut laden“-Knopf) – Mock ergänzt.
- Verifikation: Suite **1650/1650**, Vitest **1079/1079**, `tsc -b` grün, **14 Mutanten** rot. Commit `8e970493`.

## Runde 76 — Re-Review / Abnahme

- **Vater – abgenommen.** Rollengang als Playwright: 38 passed, darunter `assistent-klassenarbeit` (Termin, Leiter,
  eine Klassenarbeit, kein zweiter Plan beim Wiederholen) und die Formular-Specs mit „(Pflicht)“.
- **Creator – abgenommen (Agent):** `AssignAgentTests` 12/12 gegen einen In-Process-Server; ein Handlauf gegen eine
  laufende Instanz steht aus (Liste für den Menschen).
- **Sohn – Regressionszeuge:** `erstbegegnung` grün, kein Sohn-Pfad im Diff.

## Retrospektive 76

**Nachschau:** Sprint 74 – zwei Durchrutscher (B-550 Lernkontroll-Anzeige ignoriert eine feste Stufe, B-551
Feldhilfe zum Stufenplan falsch), `wiki/08` nachgezogen. Sprint 75 und 76 sieht der Gesamt-Review im Abschluss nach.

- **Was die eigenen Tore durchgelassen haben:** In zwei Sprints hintereinander hat das Frontend eine Server-Regel
  vereinfacht nachgebaut (B-469 → B-550; B-423 „getippt“). Beide Male fand es erst ein Review.
- **Der eine Mechanismus:** keiner neu gebaut – die Regel „Übungstypen und ihre Eigenschaften kommen aus dem
  Server-Manifest“ steht in `frontend/CLAUDE.md`; B-550 wird sie für die Positions-Stufe ebenso vom Server holen.
- **Bewusst ungedeckt:** der Handlauf des Agenten gegen eine laufende Instanz.

## Abschluss

- **Merge:** Eine parallele Sitzung hatte die Sprints 73–75 schon nach `main` gemergt (lokal `489bc8b4`, gepusht
  bis `99f5cec2` samt eigener CI-Fixes `cfe2331b`, `99f5cec2` und dem Merge von `prototyp/lernpfad-b525` „als
  erledigt“ mit Strategie `ours` – kein Inhalt übernommen). `auto/fertigstellung` ist ein Vorfahre von `main`. Ich habe
  `main` in den Lauf-Branch gemergt (`2ef0ec8a`, konfliktfrei).
- **Gesamt-Review** (Backend + Frontend über `40dd9bfa..HEAD`, inklusive der fremden Commits): kein Blocker, neun
  „sollte“-Funde aus dem Zusammenspiel, alle behoben. Der schwerste: Die Lernkontroll-Anzeige rechnete eine
  Server-Regel nach und riet dem Vater bei den Leiter-Positionen des Assistenten zu „nur getippte Tests“ – das hätte
  das Ziel unerreichbar gemacht. Jetzt liefert der Server `PositionResponse.learningCheck`; damit sind B-550 und B-551
  erledigt. Ferner: Truhe/Bonus lesen `rewardsUnlocked` statt nachzurechnen, Bonus-Satz für Pläne nur mit
  Wochenzielen, Balken nur über die Belohnungsziele, sichtbarer Neuladen-Fehler, Assistent mit Manifest-Stufennamen
  und Fach als Pflichtfeld, Agent prüft `canAssign` vor dem ersten Schreiben und lehnt `--cadence None` ab,
  `requireTypedTest` + Leiter mit Selbsteinschätzung → `validation_error`.
- **Messung im Abschluss:** Die volle Playwright-Suite fand mit der Server-Lernkontrolle einen echten, vorher
  verdeckten Defekt: „nur getippte Tests“ auf einer Vokabel-Position **ohne** Stufe prüft auf der Vorgabestufe
  Selbsteinschätzung und ist nie erfüllbar (täglicher Malus). Die alte Anzeige sagte dort „Test getippt“ → **B-552**
  (P1, Defekt), nicht mehr in diesem Lauf.
- **Belege nach dem letzten Code-Edit:** Suite **1659/1659** (Release), Vitest **1088/1088**, `tsc -b` grün, volle
  Playwright-Suite **153 passed**, 2 rot mit veralteter Erwartung (beide Specs angepasst, danach 3 passed). Mutanten
  der Abschluss-Fixes: G1–G4, G5b, G6–G8, G10 rot; G9 (Balkenbreite nutzt die Belohnungsziele) bewusst ungedeckt – der
  Test hält die Rechnung, nicht die Aufrufstelle.

### Zähltabelle

| Größe | Wert |
| --- | --- |
| N (offen zu Beginn, Index auf `main` `40dd9bfa`) | 35 |
| neu angelegt im Lauf | 6 (B-547 Review 74, B-548/B-549 Nachschau 73, B-550/B-551 Nachschau 74, B-552 Abschluss) |
| abgenommen | 20 (Sprint 73: 4, Sprint 74: 5, Sprint 75: 3, Sprint 76: 6, Abschluss: 2) |
| davon aus N | 16 = **46 %** von N (Ziel ≥ 30 %) |
| verworfen | 0 – der Abgleich fand keine erledigte, doppelte oder Sammel-Story |
| offen am Ende (Index) | **21** = 60 % von N (Ziel ≤ 24,5) |

Rechnung: 35 + 6 − 20 = 21.

### Liste für den Menschen

1. **Echte DB vor dem nächsten Start:** vier angehängte Migrationen – `CardShownAt` (Lauf 2026-11-13) sowie
   `NewBonusPaidAt`, `ExtraRunContentHash`, `DailyBoxOpenedAt` aus diesem Lauf. Alle vier sind gegen eine Kopie der
   echten `pugling.db` geprobt (laufen durch; 70 Fortschrittszeilen als „Neu-Bonus bezahlt“ markiert, 2 Bestandstruhen
   als geöffnet). Das Startskript legt vor dem Start eine Sicherung an.
2. **Friedrichs −60 Malus vom 11./12.11.** von Hand per „Verschenken“ ausgleichen (B-535, Entscheidung 4 des Nutzers).
3. **Gerät:** die Truhen-Animation (B-527) und weiter Klang/Haptik (B-31) am echten Handy beurteilen.
4. **B-552 (P1)** grillen: soll der Server „nur getippt“ ohne getippte Stufe ablehnen? Vorher an einer DB-Kopie zählen,
   welche echten Positionen so stehen.
5. **B-536** (Aufbau-Übungen, XL) ist gegrillt (fünf Empfehlungen übernommen) und braucht eine Karte im Dialog.
6. **Agent `assign`:** ein Handlauf gegen die laufende Instanz steht aus (Tests laufen gegen einen In-Process-Server).
7. **Tor-Vorschlag** (nicht gebaut, Regel „neues Tor nur vorschlagen“): ein Wächter gegen doppelt kodiertes UTF-8
   (`Ã`, `â€`) in `docs/**/*.md` – im Vorlauf blieb so etwas in 20 Stories zwei Tage unbemerkt.
8. **Stakeholder-Entscheidungen zum Umstoßen** (alle in den Stories markiert): B-535 Entscheidung 6 (Bestandszeilen mit
   `ReviewCount > 0` gelten als bezahlt – gemessen widerlegte Prämisse), B-545 (veraltete Bonus-Runde wird verworfen),
   B-527 (Truhe verfällt nie, nur das Kind öffnet), B-16/B-140/B-469-Entscheidungen, B-423 Entscheidung 7 (keine
   Leiter bei Stufe ≤ 2), B-20 (Exit 3 = nichts neu), B-550 (Server lehnt „nur getippt“ + Leiter ab), B-536.
9. Das Manifest-Feld `selfAssessedTest` (Sprint 74) hat seit B-550 keinen Leser im Frontend mehr; es ist additiv und
   schadet nicht – bei Gelegenheit entfernen oder für den Agenten nutzen.
10. `prototyp/lernpfad-b525` hat die parallele Sitzung nach `main` gemergt – mit Strategie `ours`, also ohne Inhalt;
    der Commit sagt „Nicht nach main mergen“, sein Inhalt steht weiter nur im Branch.

### CI nach dem Push

Der erste CI-Lauf auf `9e02510f` war rot, obwohl alle 1659 Tests grün waren: `EndpointCoverageGuard` meldete beim
Aufräumen der Assembly 282 statt der gepinnten 281 abgedeckten Actions (neuer Truhen-Endpunkt aus Sprint 75). Die
Konsole zeigt dabei „Passed!“ – ich hatte lokal nur diese Zeile gelesen, nicht den Exit-Code. Konstante auf 282
nachgezogen; lokal danach Exit-Code 0. **Lehre für die Liste der Werkzeug-Fallstricke:** bei `dotnet test` den
Exit-Code prüfen, nicht die „Passed!“-Zeile – dieselbe Klasse wie „E2E nie am Exit-Code messen“, nur umgekehrt.
