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
| gilt | B-552, B-538, B-536, B-539, B-553 (vorher B-540), B-547, B-219, B-218, B-248, B-249, B-250, B-221, B-376, B-31, B-39, B-410, B-09, B-377, B-555, B-252, B-223 |
| teilweise erledigt, Kern gilt | B-220 (`PenaltyCoins` schon im Vertrag), B-556 (B-529 liefert die Erklärung für Regel-Aufgaben), B-557 (Konsole kann `--use-weak` + `assign`, es fehlt der Griff im Vater-Web), B-369 (Zeilen gewandert) |
| veraltet, neu zuschneiden | B-47 (Azure-Weg tot; der gefahrene Betrieb ist `scripts/heimnetz.ps1`) |
| **verworfen** | **B-554** (geteilt – Sammel-Story ohne eigene Arbeit, Muster B-247) · **B-251** (gegenstandslos durch B-470, gemessen in B-459) |

### Buchhaltungs-Funde beim Abgleich

- **Doppelte Ids.** `auto/fertigstellung` hatte B-540…B-544 neu vergeben, während `main` dieselben Nummern im Lauf
  vom 2026-11-14 abgenommen hatte. Umgezogen: B-540 → **B-553** (Plan aus Unit), B-541 → **B-554**, B-542 → **B-555**,
  B-543 → **B-556**, B-544 → **B-557**; Verweise in B-537, B-248, B-249, B-09, `docs/obsidian.md` und
  `docs/rockettutor-vergleich-plan.md` nachgezogen. Kein Tor sieht doppelte Ids – Vorschlag für die Retro.
- **Konfliktmarken.** Mein Merge-Commit `d39770b1` trug unaufgelöste Marken in B-09 und B-248: die Merge-Ausgabe war
  per `tail` abgeschnitten, und `git add -A` hat die Dateien als gelöst markiert. Behoben in `d41df00a`.

Nach dem Abgleich: **26 offen**.

### Zurückgestellt (nicht in diesem Lauf)

- **B-31** – wartet auf ein echtes Handy.
- **B-536** – XL, Karte; geht in den Dialog.
- **B-09, B-248, B-249, B-555** – L/XL mit Migration, hängen aneinander (Klassenarbeit-Kette) bzw. an der Teilung.
- **B-218, B-220, B-250** – Queue-Kette, hängt an B-248.
- **B-221, B-376** – Gems/Auszeichnungen, Migration; nachrangig zu den P1.
- **B-223** – Vertragsbruch an `PracticeCard`; nachrangig.
- **B-369** – L, Teilung nötig (Vater-Web/Arcade); würde erst eine Story mehr erzeugen.
- Parallele Sitzung im Haupt-Checkout arbeitet am Skill `lehrwerk-aufbereiten` – keine der gewählten Stories berührt ihn.

## Refinement

Drei Agenten treiben je drei Kandidaten bis `geschaetzt` (nur Doku, getrennte Dateien): Vater-Plan (B-552, B-539,
B-553), Vater-Sicht (B-557, B-39, B-377), Sohn/Entwickler (B-219, B-47, B-556). B-538, B-547, B-252 und B-410 waren
schon geschätzt.

Ergebnis Refinement (alle Entscheidungen als „Stakeholder-Entscheidung im Auftrag des Nutzers“ markiert): B-552 (S),
B-557 (M), B-39 (L → M, Kind-Hub-Reiter gestrichen, höchstens 92 Tage je Abfrage), B-377 (M, Migration), B-219 (M,
Migration), B-47 (S, neu zugeschnitten auf ein wiederverwendbares `scripts/artefakt-smoke.ps1` plus CI-Job; die
Azure-gebundenen Entscheidungen 1–2 durchgestrichen), B-556 (S, eingeengt auf Vokabel-Items, Migration). An der Kopie
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
