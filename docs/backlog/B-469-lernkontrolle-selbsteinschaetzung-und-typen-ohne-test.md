---
tags: [typ/story, status/abgenommen, bereich/training, bereich/frontend, rolle/supervisor]
aliases: [Lernkontrolle, Selbstcheck, Grammar ohne Test, Lernkontrolle sichtbar]
status: abgenommen
prio: P2
art: Frage
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: Rollengang am Access-Lehrwerk 2026-11-10 (Kopie der pugling.db, Sohn im Handy-Format 375×812, Vater-Web)
grund: ""
ersetzt_durch: []
---

# B-469 · Prüfauftrag: Der Vokabeltest ist per Vorgabe eine Selbsteinschätzung, Grammatik und Übersetzung haben keinen Test

Der Vater will den Lernfortschritt prüfen. Im Prüfgang zeigte sich, was er dafür tatsächlich bekommt:

- **Vokabel-Position ohne `RequireTypedTest`**: Der Test läuft auf Stufe 2. Das Kind dreht die Karte um und tippt
  „Gewusst!“. Bestanden heißt dann: Das Kind hat es so gesagt.
- **Grammatik und Übersetzung** (`checkMode: None`, Knopf „Durchspielen“): Es gibt keinen Test. Die Pflicht
  gilt als erfüllt, sobald der Bestehens-Prozentsatz der Runde gespielt ist (`PositionProgressService.PlayedEnough`).
  Im Positions-Report sieht der Vater keine Trefferquote.
- Über die API ließ sich für die Grammatik-Position trotzdem ein Testversuch anlegen und mit 100 % bestehen
  (`POST …/positions/25/tests`); er zählt nicht für das Ziel. Die Oberfläche bietet den Weg nicht an.
- **Was gut trägt**: der Positions-Report je Wort (Box, Beherrschung, gesehen/richtig), „Heute“ mit
  Zielen und Punkten, die Kaufhistorie und die Freigabe von Einlösungen.

Die Fragen des Prüfauftrags: Soll ein Plan getippte Tests zur Vorgabe machen? Sollen Grammatik und Übersetzung
prüfbar werden, oder soll die Oberfläche dem Vater sagen, dass diese Positionen nur das Durchspielen messen?

## User Story

Als *Vater* möchte ich je Position sehen, **womit** die Pflicht geprüft wird – getippter Test, Test mit
Selbsteinschätzung oder nur Durchspielen –, damit ich weiß, wie viel ein „erfüllt“ wert ist, und bei Bedarf
„nur getippte Tests zählen“ einschalte.

## Ist-Stand am Code

Nachgeprüft 2026-11-13, berichtigt 2026-11-14:

- Vorgabe-Stufe jedes Typs ist `SelfAssess` (`backend/Pugling.Api/Exercises/ExerciseTypeBase.cs:37`) – ohne
  `RequireTypedTest` bleibt der Test eine Selbsteinschätzung. Das Tagesziel zählt bei `RequireTypedTest` nur
  gewertete Versuche (`Services/Shared/PositionProgressService.cs:150`), gewertet ist ein Versuch auf getippter
  Stufe (`Controllers/Student/PositionTestsController.cs:154,178`, `Graded = typed`).
- **Der Assistent** verlangt getippt **nur beim Ziel „Klassenarbeit"**: `applyDefaults` überschreibt den
  Anfangszustand mit dem Ziel (`frontend/src/vater/VaterWizard.tsx:244`, aufgerufen `:336`); „Aufholen"/
  „Regelmäßig" tragen `typed: false` (`:50-51`); gesendet `:371`.
- **Der KI-Lehrplan** setzt für Vokabelübungen aus der Buch-Wortliste `RequireTypedTest: true`
  (`backend/Pugling.Agent.Creator/PlanPlanner.cs:286-292`); die übrigen bekommen `null` (`:243`).
- **`null` erbt die Übungs-Vorgabe** `Exercise.DefaultRequireTypedTest`
  (`Controllers/Supervisor/PlanPositionsController.cs:169`, `:196`), die der Creator je Übung setzt
  (`frontend/src/vater/ExerciseEditModal.tsx:416-417`); die Plan-Seite übernimmt beim Wählen einer Übung deren
  Vorgabe (`frontend/src/vater/PlanPositions.tsx:645`).
- **Grammatik/Übersetzung ohne Test:** `ExerciseCheckMode.None` (`Exercises/BuiltInExerciseTypes.cs:112`, `:133`;
  ebenso `:23`, `:44`, `:74`, `:150`); Pflicht über `PlayedEnough` (`PositionProgressService.cs:122-127`, Schwelle
  = `GoalThreshold` bzw. Vorgabe).
- **Was der Vater heute sieht:** die Positionszeile (`frontend/src/vater/PlanPositions.tsx:750-762`) zeigt nur
  `· getippt`, wenn `requireTypedTest` gesetzt ist; ohne das Häkchen und bei `checkMode: None` steht **nichts**.
  Das Typ-Manifest mit `checkMode` liegt in der Zeile schon vor (`useExerciseTypes()` in `PositionRow`, `:723`;
  `ExerciseTypeLookup.byType`, `frontend/src/lib/exerciseTypes.ts:37-39`).
- Feldhilfe `requireTypedTest` (`frontend/src/lib/fieldHelp.ts:103-108`) erklärt das Häkchen, nicht die drei Arten.

## Die echte Lücke

Nicht die fehlende Strenge – die gibt es (Häkchen, Klassenarbeits-Ziel, KI-Wortlisten) –, sondern dass der
Vater am Plan nicht sieht, welche Position wie viel prüft. Ein „erfüllt“ bei Grammatik und ein „erfüllt“ beim
getippten Vokabeltest sehen gleich aus.

## Offene Punkte

1. ~~Werden getippte Tests Pflicht bzw. Vorgabe?~~ → Entscheidung 1.
2. ~~Bekommen Grammatik und Übersetzung einen Test?~~ → Entscheidung 3 (zurückgestellt).
3. ~~Sagt die Oberfläche dem Vater, was eine Position misst?~~ → Entscheidung 2 (der baubare Kern).
4. ~~Der über die API mögliche Testversuch an einer `None`-Position?~~ → Entscheidung 4 (zurückgestellt).

## Entscheidungen

1. **Getippte Tests werden nicht zur Pflicht und nicht zur neuen Vorgabe.** Stakeholder-Entscheidung im
   Auftrag des Nutzers. Begründung: Wo der Vater Strenge will, gibt es sie schon (Ziel „Klassenarbeit“,
   Häkchen je Position, Übungs-Vorgabe des Creators, KI-Wortlisten); ein Zwang für „Aufholen“/„Regelmäßig“
   träfe genau das Kind, dem neue Wörter beim Tippen noch nichts einbringen (B-535 Ist-Stand 2: 45 Antworten,
   0 Münzen) und das mit LRS tippt. Die Fairness-Bedingung aus B-462 (eine Bedeutung genügt) ist inzwischen
   erfüllt, ändert aber nichts an diesem Abwägen. B-423 Entscheidung 6 (die anderen Ziele bleiben unverändert)
   bleibt gültig. Kosten: Selbsteinschätzung bleibt möglich – Entscheidung 2 macht sie wenigstens sichtbar.
2. **Der baubare Kern: die Positionszeile nennt die Art der Lernkontrolle.** Stakeholder-Entscheidung im
   Auftrag des Nutzers. Eine reine Funktion `lernkontrolleText(checkMode, requireTypedTest)` ersetzt das heutige
   `· getippt`:
   - `StudyPlanTest` + `requireTypedTest` ⇒ „Test getippt“,
   - `StudyPlanTest` ohne ⇒ „Test, Selbsteinschätzung zählt“ (Hervorhebung `pill amber` wie der Malus),
   - `None` ⇒ „kein Test – zählt Durchspielen“ (ebenfalls `pill amber`),
   - `CatalogCheck`/`CatalogGenerateCheck` bzw. unbekannter Typ (Manifest lädt noch) ⇒ nichts (wie heute).
   Dazu ein Feldhilfe-Eintrag `lernkontrolle` (InfoHint an der Zeile), der die drei Arten in drei Sätzen erklärt.
   Begründung: Der Vater erzwingt Lernerfolg nur, wenn er sieht, wo seine Pflicht schwach ist; die Information
   liegt im Client schon vor. Kosten: eine Funktion, eine Zeile, ein Hilfetext; kein Server, kein Vertrag.
3. **Zurückgestellt: ein Test für Grammatik und Übersetzung.** Stakeholder-Entscheidung im Auftrag des Nutzers.
   Begründung: das hieße je Typ einen neuen `CheckMode` samt Prüfpfad, Bewertung (Übersetzung mit
   Alternativen) und Sohn-Oberfläche – mindestens L und mit Vertragsbruch, nicht in einer Sitzung baubar. Kosten:
   diese Positionen messen weiter nur das Durchspielen; nach Entscheidung 2 steht das sichtbar am Plan. Wird
   später gebraucht: eigene Story je Typ, Übersetzung zuerst (eindeutige Lösung + `alternatives`).
4. **Zurückgestellt: der über die API mögliche Testversuch an einer `None`-Position.** Stakeholder-Entscheidung
   im Auftrag des Nutzers. Begründung: er zählt nicht für das Ziel und bucht nichts (Befund 2026-11-10), schadet
   also weder Vater noch Kind; ein `409` dort ist sauberer, aber ein eigener Defekt-Schnitt. Kosten: eine
   Unschärfe im Vertrag bleibt; beim Bau dieser Story als eigene Story aufnehmen, wenn der Entwickler sie für
   wirksam hält (Regel „Ein Fund beim Bauen wird eine eigene Story“).

## Akzeptanzkriterien

1. `lernkontrolleText` liefert für die vier Fälle aus Entscheidung 2 genau die genannten Texte (Vitest je Fall).
   *Rot, wenn* der `None`-Zweig fehlt (dann stünde wie heute nichts) bzw. wenn `requireTypedTest` bei
   `StudyPlanTest` ignoriert wird.
2. Die Positionszeile im Plan-Detail zeigt den Text; `· getippt` als eigene Angabe entfällt (kein doppelter
   Hinweis). Bei `None` und bei „Selbsteinschätzung zählt“ ist der Text als `pill amber` hervorgehoben.
3. Während das Manifest lädt oder sein Abruf scheiterte, zeigt die Zeile **keinen** Kontrolltext (kein falsches
   „kein Test“ für einen unbekannten Typ). *Rot, wenn* ein unbekannter Typ wie `None` behandelt wird.
4. Feldhilfe `lernkontrolle` existiert und hängt als InfoHint an der Angabe.
5. E2E: Der Vater öffnet einen Plan mit einer getippten Vokabel-, einer ungetippten Vokabel- und einer
   Grammatik-Position und liest die drei Texte in den drei Zeilen. *Rot, wenn* die Zeile den Text nicht rendert.
6. Unverändert: Assistent (alle Ziele), Positions-Formular, Server-Regeln (Regression über die bestehenden
   Vitest-/E2E-Läufe des Assistenten).

## Schätzung

**Größe: S** – eine reine Funktion, eine Zeile in `PositionRow`, ein Feldhilfe-Eintrag, Vitest und eine kleine
E2E. Vergleich: etwas mehr als zwei Sätze in `lib/fieldHelp.ts` samt E2E (B-02, XS), weniger als `childId` aus dem
Test-Pfad ziehen (B-01, S) an Risiko, aber mit derselben Teile-Zahl.

- **`migration: nein`** – kein Schema.
- **`vertragsbruch: nein`** – nutzt `checkMode` aus dem Typ-Manifest und `requireTypedTest` aus
  `PositionResponse`, beide schon im Vertrag.

**Risiken:**

- Das Manifest kommt asynchron; der Text darf erst erscheinen, wenn `byType` den Typ kennt (AK 3).
- `old-phrases.py` und bestehende Specs, die `· getippt` lesen (vor dem Bau greppen: `grep -rn "getippt" frontend/e2e frontend/src --include=*.ts*`).
- Ein ungetippter Vokabel-Test kann trotzdem auf einer Tipp-Stufe laufen (Stufe aus Plan/Position); „Selbsteinschätzung
  zählt“ ist dann die obere Schranke des Risikos, nicht der Normalfall – der Hilfetext sagt „zählt mit“, nicht „ist immer“.

**Angriffsplan** (kein Backend nötig, Entscheidung 3 und 4 zurückgestellt)

1. `lernkontrolleText` exportiert aus `frontend/src/vater/PlanPositions.tsx` (neben `dosisText`) + Vitest in
   `PlanPositions.test.ts`.
2. `PositionRow`: Text aus `types.byType.get(pos.exerciseType)?.checkMode` und `pos.requireTypedTest`; `· getippt`
   ersetzen; InfoHint.
3. `fieldHelp.ts`: Eintrag `lernkontrolle`.
4. E2E `frontend/e2e/lernkontrolle-sichtbar.spec.ts` (Positionen per API anlegen, dann Plan-Detail lesen).

**Testweg:**

- Vitest `frontend/src/vater/PlanPositions.test.ts` (AK 1, AK 3 über die Funktion mit `undefined`-Typ).
- Playwright `frontend/e2e/lernkontrolle-sichtbar.spec.ts` (AK 2, AK 5) – zählt als Rollengang Vater; Ergebnis am
  „N passed“ messen, nicht am Exit-Code.
- Bestehende Specs/Vitest des Assistenten unverändert grün (AK 6).

## Verlauf

- **2026-11-10** — angelegt (Prüfgang am Access-Lehrwerk: Übungen, Vater-Sicht, Handy).
- **2026-11-13** — nachgeprüft (Tageslauf, Restzeit): Assistent und KI-Wortlisten-Plan verlangen schon getippt; offen bleiben Handanlage und Typen ohne Test.
- **2026-11-14** — Abgleich Backlog-Lauf: Kern gilt, Ist-Stand berichtigt – der Assistent verlangt getippt nur beim Ziel „Klassenarbeit" (`VaterWizard.tsx:49-51`, `:244`), `null` erbt die Übungs-Vorgabe (`PlanPositionsController.cs:169`), die Plan-Seite übernimmt beim Wählen die Übungs-Vorgabe (`PlanPositions.tsx:645`); Grammatik/Übersetzung weiter ohne Test (`BuiltInExerciseTypes.cs:112,133`).
- **2026-11-14** — Refinement Backlog-Lauf: ausformuliert, gegrillt, geschätzt – Stakeholder im Auftrag des Nutzers: getippt wird keine Pflicht (Entscheidung 1), Kern ist die sichtbare Lernkontrolle je Positionszeile (Entscheidung 2), Test für Grammatik/Übersetzung und der API-Testversuch an `None`-Positionen zurückgestellt (3, 4); S, frontend, keine Migration, kein Vertragsbruch.
- **2026-11-14** — Sprint 74, Frontend gebaut (Kern nach Entscheidung 2): `lernkontrolleText(checkMode, requireTypedTest)` und die Angabe in `PositionRow` statt „· getippt“, schwache Arten als `pill amber`, InfoHint `lernkontrolle` (`frontend/src/vater/PlanPositions.tsx`, `lib/fieldHelp.ts`). Tests: `PlanPositions.test.ts` „Positionszeile – Lernkontrolle (B-469)“ (AK 1 vier Fälle, AK 3 „unbekannter Typ: nichts“); `PlanPositionsStufe.test.tsx` „zeigt je Position die Art der Prüfung, die schwachen hervorgehoben“ (AK 2, AK 4). Offen: die E2E `e2e/lernkontrolle-sichtbar.spec.ts` (AK 5) ist nicht gebaut.
- **2026-11-14** — Sprint 74, E2E nachgezogen: `frontend/e2e/lernkontrolle-sichtbar.spec.ts` „Die Positionszeilen nennen die Art der Lernkontrolle“ (AK 5; drei Positionen per API, Plan-Detail im UI; getippt/ungetippt/Grammatik je Zeile, `pill amber` an den schwachen, kein „· getippt“). Grün (Lauf „3 passed“ zusammen mit `auswahl-nicht-zuweisbar`); rote Probe: Zeile auf `· getippt` zurückgedreht → Spec rot an „Expected substring: Test getippt“.
- **2026-11-14** — **abgenommen** (Sprint 74, Kern nach Entscheidung 2). volle Suite 1621/1621 (Release), Vitest 1023/1023 (drei volle Läufe in Folge), `tsc -b` grün; Rollengang Vater als Playwright gegen frisch gestartete Server nach dem letzten Edit: 19 passed (`auswahl-nicht-zuweisbar` 3 Fälle, `lernkontrolle-sichtbar`, `planbau-bewertung`, `vater-von-null`, `assistent`, `uebung-teilspeichern`, `interessen`, `bonus-deckel`, `kind-lehrbuch-fach`, `responsive-vater`, `full-flow`); Reviews `pugling-reviewer` + `frontend-reviewer`, Funde im Sprint behoben; Commit `defa3ea0`. Mutanten rot: N15 Manifest-Flag ignoriert;
  N3 `selfAssessedTest` immer falsch. Review-Fund behoben: Zuordnung warnte „Selbsteinschätzung zählt“, obwohl der
  Server dort jeden Test getippt wertet – das Manifest trägt jetzt `selfAssessedTest` aus der Stufenregel des Typs
  (`ExerciseTypeRegistry.Published`), Test `Manifest_Sagt_Ob_Ein_Test_Selbsteinschaetzung_Zaehlt`. AK 5 hält die E2E
  `lernkontrolle-sichtbar` (Rot-Probe vom Bau-Agenten gemessen). Entscheidungen 3/4 bleiben zurückgestellt.
