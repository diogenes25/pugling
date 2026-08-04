---
tags: [typ/story, status/abgenommen, bereich/training, lerntechnik/vokabeln, rolle/student]
aliases: [Selbsteinschätzung Alternativen, Reveal zeigt nur eine Lösung]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: docs/backlog/B-65-vokabel-mehrere-uebersetzungen.md (Review-Nebenbefund)
---

# B-70 · Die Selbsteinschätzung zeigt nur die primäre Übersetzung

Seit [B-65](B-65-vokabel-mehrere-uebersetzungen.md) kann eine Vokabel mehrere gleichwertige Übersetzungen
tragen, und bei den getippten Stufen zählt jede. Die Stufe **Selbsteinschätzung** deckt aber nur die
primäre auf: `PositionPracticeController.BuildCard` und `PositionTestsController.ToItem` reichen
`f.Reveal` (= `item.Answer`) durch. Wer „sehr groß" gedacht hat und „riesig" aufgedeckt bekommt, wertet
sich selbst als falsch — derselbe Schaden wie im ursprünglichen Defekt, nur diesmal vom Kind selbst
verursacht. Genau die Ecke, aus der die Anmerkungen #11/#12 kamen.

## User Story

Als Sohn möchte ich beim Aufdecken einer Karte **jede** gleichwertige Übersetzung sehen, damit ich mich
nicht selbst als falsch einschätze, obwohl ich eine ebenso richtige Antwort im Kopf hatte.

## Ist-Stand am Code

- Die Aufdeck-Logik sitzt an **genau einer** Stelle: `PositionPlayService.CardFacets` liefert
  `typed ? null : item.Answer` als `Reveal`
  ([PositionPlayService.cs:150](../../backend/Pugling.Api/Services/Shared/PositionPlayService.cs)) — nur
  die primäre `ContentItem.Answer`, nie `AcceptedAnswers`. Der XML-Kommentar direkt darüber
  ([PositionPlayService.cs:135](../../backend/Pugling.Api/Services/Shared/PositionPlayService.cs)) sagt es
  selbst: „typed stages withhold the solution (`Reveal`), display/self-assessment reveals it" — ohne ein
  Wort zu Alternativen.
- Genau **drei** Aufrufer teilen sich diese eine Funktion — der Fix ist darum an einer Stelle, nicht an
  drei: `PositionPracticeController.BuildCard`
  ([PositionPracticeController.cs:103-111](../../backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs),
  `f.Reveal` bei `:109`), `PositionTestsController.ToItem`
  ([PositionTestsController.cs:68-77](../../backend/Pugling.Api/Controllers/Student/PositionTestsController.cs),
  `f.Reveal` bei `:75`) und `ExercisePreviewService`
  ([ExercisePreviewService.cs:114](../../backend/Pugling.Api/Services/Creator/ExercisePreviewService.cs)).
- `ContentItem.AcceptedAnswers` trägt die primäre Antwort **plus** jede erklärte Alternative bereits seit
  B-65 ([ExerciseContentProvider.cs:21-28](../../backend/Pugling.Api/Services/Shared/ExerciseContentProvider.cs)):
  gebaut über den Helfer `Accepted(answer, alternatives) => [answer, .. alternatives]`
  ([ExerciseContentResolver.cs:132-133](../../backend/Pugling.Api/Services/Shared/ExerciseContentResolver.cs)),
  gefüttert aus `Vocabulary.TranslationAlternatives` bzw. `Gap.Alternatives`
  ([ExerciseContentResolver.cs:99,123,125](../../backend/Pugling.Api/Services/Shared/ExerciseContentResolver.cs)).
  Die Daten liegen also längst da — nur die Ausspielung an dieser einen Stelle ignoriert sie.
- Der Richtungstausch setzt `AcceptedAnswers = [it.Prompt]`, ein Einzelelement
  ([ExerciseContentProvider.cs:69](../../backend/Pugling.Api/Services/Shared/ExerciseContentProvider.cs)) —
  „keine Alternative nach dem Tausch" (B-65 Entscheidung 3) ist damit für den neuen Reveal-Zusatz **bereits
  erfüllt**, ohne eigenen Code.
- Der Vertrag trägt `Reveal` als **einzelnes** `string?`-Feld an drei Stellen: `PracticeCard`
  ([PracticeDtos.cs:48-51](../../backend/Pugling.Contracts/Student/PracticeDtos.cs)), `TestItem`
  ([TestDtos.cs:26-28](../../backend/Pugling.Contracts/Student/TestDtos.cs)) und `PreviewItem`
  ([ExercisePreviewDtos.cs:17-18](../../backend/Pugling.Contracts/Creator/ExercisePreviewDtos.cs)).
- Frontend rendert exakt dieses eine Feld als eine Zeile, an drei Stellen: `SohnPractice.tsx:252`
  (`card.reveal`), `SohnTest.tsx:196` (`item.reveal`) und `ExercisePreviewModal.tsx:206` (`it.reveal`) —
  nirgends eine zweite Zeile für Alternativen. Die Typen kommen generiert aus dem OpenAPI-Dokument
  (`frontend/CLAUDE.md`, `npm run gen:contract`), nicht von Hand.
- Der Namens-Wächter `ConventionGuardTests.Actions_Mit_Loesungsfeld_Sind_Vor_Dem_Studenten_Gegated` prüft
  **exakte** Property-Namen (`SolutionPropertyNames = ["Answer", "Solution", "CorrectAnswer",
  "Translation"]`, `StringComparer.Ordinal`-Gleichheit,
  [ConventionGuardTests.cs:300,355](../../backend/Pugling.Api.Tests/ConventionGuardTests.cs)); `Reveal`
  selbst steht laut Kommentar bewusst außerhalb ([ConventionGuardTests.cs:291-297](../../backend/Pugling.Api.Tests/ConventionGuardTests.cs)) —
  ein neu benanntes Zusatzfeld trifft die Liste nicht.
- Keine bestehende Testklasse prüft den **Inhalt** eines Reveals auf einer nicht-getippten Stufe — geprüft
  in `ReviewGradingTests.cs:112-114`, `PositionPracticeFlowTests.cs:36`, `ExercisePreviewTests.cs:34-40`,
  `ClozePlayTests.cs:93`, `PositionPlayModesTests.cs:308`: alle fragen nur „ist `reveal` `null`" auf der
  getippten Stufe ab, keine „was steht in `reveal`" auf der aufdeckenden.
- Testwerkzeug liegt schon bereit: `TestApi.CreateStoreVocabAsync(father, word, translation,
  translationAlternatives: [...])` und `TestApi.CreateVocabRefExerciseAsync`
  ([PositionTestFlowTests.cs:214-216](../../backend/Pugling.Api.Tests/PositionTestFlowTests.cs)) aus dem
  B-65-Bau.

## Die echte Lücke

Enger als der Nebenbefund vermuten ließ: Es fehlt **kein neuer Mechanismus** — `AcceptedAnswers` trägt die
Alternativen längst, und der Richtungstausch verwirft sie bereits korrekt. Die Lücke ist buchstäblich
**eine Zeile** (`PositionPlayService.cs:150`), die nur die primäre Antwort statt aller akzeptierten
Antworten weiterreicht, plus deren Durchreichen durch drei Vertrags-Records und drei Frontend-Stellen. Der
Fund ist eng mit der Vorlage von B-65 (dort war die Ausspielung ebenfalls „eine Zeile") — nur diesmal in der
umgekehrten Richtung: dort fehlte die Alternative in der *Bewertung*, hier fehlt sie in der *Anzeige* dessen,
was das Kind zur Selbsteinschätzung sieht.

## Offene Punkte

1. ~~Kommen die Alternativen beim Aufdecken immer mit, oder nur als „auch richtig:"-Zeile?~~ → Entscheidung 1
2. ~~Berührt das die Bildregel des Anti-Cheats?~~ → Entscheidung 2
3. ~~Gilt der Fix für alle Übungstypen oder nur Vokabeln?~~ → Entscheidung 3
4. ~~Zieht das den Namens-Wächter (`SolutionPropertyNames`) auf den Plan?~~ → Entscheidung 4

## Entscheidungen

1. **Additives Feld `RevealAlternatives` neben `Reveal`, keine Verschmelzung.** `Reveal` bleibt die eine
   kanonische Antwort (Vorgabe der bestehenden XML-Doku,
   [PositionPlayService.cs:135](../../backend/Pugling.Api/Services/Shared/PositionPlayService.cs)); die
   zusätzlichen, gleichwertigen Antworten kommen als eigenes `IReadOnlyList<string>?` dazu, gefüllt aus
   `item.AcceptedAnswers` abzüglich der primären. Begründung: `Reveal` bricht so für keinen bestehenden
   Konsumenten (kein Vertragsbruch), und die Oberfläche kann „die Antwort" von „auch richtig" optisch
   trennen — eine verschmolzene Kommaliste sähe für den Regelfall ohne Alternative identisch zum heutigen
   Bug aus. *Kosten:* drei Contract-Records wachsen um je einen optionalen Trailing-Parameter (Muster aus
   B-65, Angriffsplan #4: „mit Vorgabewert anhängen") — additiv, darum `vertragsbruch: nein`.
2. **Alternativen kommen nur dort mit, wo die primäre Lösung ohnehin aufgedeckt wird** (`!typed` in
   `CardFacets`), keine eigene Schranke. Begründung: es ist dieselbe Anti-Cheat-Grenze — eine Alternative
   ist auf der aufdeckenden Stufe kein größeres Geheimnis als die primäre Antwort; eine eigene Schranke
   würde einen inkonsistenten Zwischenzustand schaffen (Hauptantwort sichtbar, gleichwertige Antwort nicht).
   Die Bildregel bleibt unberührt: Bilder hängen an `ImageUrl`/`typed`
   ([PositionPlayService.cs:153-156](../../backend/Pugling.Api/Services/Shared/PositionPlayService.cs)),
   komplett orthogonal zu Textalternativen. *Kosten:* keine — die `typed`-Prüfung existiert bereits an der
   Stelle, an der `Reveal` gesetzt wird.
3. **Gilt uniform für jeden Übungstyp, nicht nur Vokabeln.** `CardFacets`/`AcceptedAnswers` sind seit B-65
   bereits typ-neutral (Lücke, Liste und Vokabel laufen alle über denselben `Accepted(...)`-Helfer,
   [ExerciseContentResolver.cs:99,123,125](../../backend/Pugling.Api/Services/Shared/ExerciseContentResolver.cs)).
   Eine Sonderregel „nur Vokabel" bräuchte eine zusätzliche Typprüfung, die es heute nicht gibt, und würde
   nichts gewinnen. *Kosten:* keine zusätzliche — es ist die natürliche Folge, den einen geteilten Helfer
   zu reparieren, keine separate Arbeit.
4. **Namens-Wächter braucht keine neue Ausnahme.** Nachgesehen, nicht vermutet: `SolutionPropertyNames`
   vergleicht exakte Property-Namen
   ([ConventionGuardTests.cs:300,355](../../backend/Pugling.Api.Tests/ConventionGuardTests.cs)); ein Feld
   `RevealAlternatives` trifft `Answer`/`Solution`/`CorrectAnswer`/`Translation` nicht. *Kosten:* keine —
   ausdrücklich festgehalten, damit niemand reflexhaft einen Eintrag in `SolutionFieldExceptions` nachträgt.

**Zwingende Folge, keine eigene Entscheidung:** Nach dem Richtungstausch bleibt `RevealAlternatives` immer
`null` — `AcceptedAnswers` ist dort bereits ein Einzelelement
([ExerciseContentProvider.cs:69](../../backend/Pugling.Api/Services/Shared/ExerciseContentProvider.cs)),
ohne dass der Fix das eigens behandeln muss.

## Akzeptanzkriterien

1. Auf jeder nicht-getippten Stufe (Anzeige/Selbsteinschätzung) trägt die Karte zusätzlich zu `reveal` ein
   Feld `revealAlternatives` mit jeder weiteren, gleichwertigen Antwort — `null`, wenn es keine gibt.
2. Auf getippten Stufen bleiben `reveal` **und** `revealAlternatives` `null` — kein neuer Leak.
3. Nach einem Richtungstausch (`back-to-front`) ist `revealAlternatives` immer `null` (Alternativen sind
   zielseitig, B-65 Entscheidung 3).
4. `PracticeCard`, `TestItem` und `PreviewItem` tragen das neue Feld additiv (Vorgabewert `null`) — bestehende
   Aufrufe ohne das Feld bleiben gültig.
5. Frontend zeigt in `SohnPractice`, `SohnTest` und `ExercisePreviewModal` eine zweite Zeile („auch richtig:
   …"), sobald `revealAlternatives` nicht leer ist; ohne Alternativen ändert sich die Anzeige nicht.
6. Ein **Regressionstest, der vorher rot ist**: eine Vokabel mit `TranslationAlternatives`, gespielt auf der
   Selbsteinschätzungs-Stufe, liefert die Alternative in `revealAlternatives`.
7. `ConventionGuardTests.Actions_Mit_Loesungsfeld_Sind_Vor_Dem_Studenten_Gegated` bleibt grün, ohne einen
   neuen Eintrag in `SolutionFieldExceptions`.

## Schätzung

**Größe: S** — vergleichbar mit [B-01](B-01-bildwahl-einfrieren.md) (`childId` aus dem Test-Pfad ziehen):
eine konkrete, mechanische Änderung an einer geteilten Stelle, ohne Migration und ohne neue Komponente.
Kleiner als [B-65](B-65-vokabel-mehrere-uebersetzungen.md) (M), weil dort Schema, zwei Schema-Tore und ein
neuer Editor-Baustein dazukamen — hier liegen die Daten bereits vor, es fehlt nur ihre Ausspielung an einer
einzigen Funktion plus das Durchreichen in drei Verträge und drei Render-Stellen.

**`migration: nein`** — keine neue Spalte, `Vocabulary.TranslationAlternatives`/`Gap.Alternatives` existieren
bereits und fließen schon in `ContentItem.AcceptedAnswers`. **`vertragsbruch: nein`** — die drei neuen Felder
sind additive Trailing-Parameter mit Vorgabewert `null`.

### Angriffsplan — Backend zuerst

1. **Kernfix.** `PositionPlayService.CardFacets` um ein Tupel-Element `RevealAlternatives` erweitern:
   `typed || item.AcceptedAnswers.Count <= 1 ? null : item.AcceptedAnswers.Skip(1).ToList()` — Index 0 ist
   laut `Accepted(...)` immer die primäre Antwort
   ([ExerciseContentResolver.cs:132-133](../../backend/Pugling.Api/Services/Shared/ExerciseContentResolver.cs)).
2. **Vertrag.** `RevealAlternatives` (`IReadOnlyList<string>?`, Vorgabewert `null`) an `PracticeCard`,
   `TestItem`, `PreviewItem` als **letzten** Parameter anhängen.
3. **Drei Aufrufer verdrahten.** `PositionPracticeController.BuildCard` (`:109`), `PositionTestsController.ToItem`
   (`:75`), `ExercisePreviewService` (`:114`) je um `f.RevealAlternatives` ergänzen.
4. **Frontend.** `npm run gen:contract` (Typen kommen aus dem aktualisierten OpenAPI-Dokument), danach je
   eine bedingte zweite Zeile in `SohnPractice.tsx:252`, `SohnTest.tsx:196`, `ExercisePreviewModal.tsx:206`.
5. **Doku-Capture.** `docs/api-examples/study-plans.md` enthält `reveal`-Beispiele und schreibt sich beim
   nächsten Testlauf automatisch um (`DocsCaptureTests`) — kein manueller Eingriff, aber im Diff erwartet.

### Risiken

- **Keins der bestehenden Tests prüft den Reveal-Inhalt** (nur „ist `null`") — die Rot-Probe für den neuen
  Regressionstest muss also wirklich vorher fehlschlagen, sonst beweist er nichts.
- **`AcceptedAnswers.Skip(1)`** setzt voraus, dass Index 0 stets die primäre Antwort ist — das hält der
  `Accepted(...)`-Helfer ein, aber ein künftiger vierter Aufrufer, der `ContentItem` anders befüllt, könnte
  die Annahme brechen. Kein akutes Risiko, nur ein Kommentar wert im Code.
- Kein Bestandsrisiko: alte Items ohne Alternativen liefern weiterhin `null`, keine Verhaltensänderung.

### Testweg

- **Regression (muss vorher rot sein):** neuer Test in `ReviewGradingTests.cs` (Muster
  `Selbsteinschaetzung_BeiRequireTypedTest_BringtKeinePunkte`, `:67-81`) oder `PositionPracticeFlowTests.cs`:
  Vokabel mit `TestApi.CreateStoreVocabAsync(..., translationAlternatives: [...])`, Position auf
  `TestStage.SelfAssess`, Karte abrufen, `revealAlternatives` enthält die Alternative.
- **Richtungstausch:** Erweiterung von `Test_Rueckwaerts_AkzeptiertDieAlternativeNicht`
  (`PositionTestFlowTests.cs:210-221`) um die Prüfung `revealAlternatives == null`.
- **Namens-Wächter:** bestehender Lauf von `ConventionGuardTests` genügt (kein neuer Fall nötig, siehe
  Entscheidung 4) — grün bleibt der Beleg.
- **Frontend:** Vitest/RTL-Ergänzung an den drei Render-Stellen (eine Karte mit Alternativen zeigt die
  zweite Zeile, eine ohne nicht).
- Zum Schluss `/smoke-test` und **`pugling-reviewer` plus `frontend-reviewer`** (`wo: beides`).

## Verlauf

- **2026-08-02** — aufgenommen als Nebenbefund des `pugling-reviewer` beim Bau von B-65; am Code
  angesehen, aber nicht selbst reproduziert.
- **2026-08-03** — ausformuliert: Ist-Stand belegt — der Defekt sitzt an einer einzigen Stelle
  (`PositionPlayService.cs:150`, `typed ? null : item.Answer`), geteilt von drei Aufrufern; die Daten
  (`ContentItem.AcceptedAnswers`) liegen dank B-65 bereits vollständig vor, der Richtungstausch verwirft sie
  schon korrekt (`ExerciseContentProvider.cs:69`); der Namens-Wächter (`ConventionGuardTests.cs:300,355`)
  ist durch exakten Namensvergleich unberührt.
- **2026-08-03** — gegrillt: alle Offenen Punkte in nummerierte Entscheidungen überführt (autonom
  getroffen, Nutzerauftrag 2026-08-04).
- **2026-08-03** — geschätzt: Größe **S**, `wo: beides`, kein Migrations- und kein Vertragsbruch-Risiko;
  Testweg ist eine Erweiterung an bestehenden Tests (`ReviewGradingTests`/`PositionPracticeFlowTests`,
  `PositionTestFlowTests`) plus `/smoke-test` (autonom getroffen, Nutzerauftrag 2026-08-04).
- **2026-08-04** — **gebaut**, genau nach Angriffsplan: `CardFacets` liefert `RevealAlternatives`
  (`typed || AcceptedAnswers.Count <= 1 ? null : Skip(1)`), additive Trailing-Parameter an `PracticeCard`,
  `TestItem` und `PreviewItem`, alle drei Aufrufer verdrahtet.
  - **Abweichung vom Plan (Schritt 4), bewusst:** statt drei Render-Stellen einzeln zu ergänzen gibt es **eine**
    Komponente `RevealAlternatives` (Muster `ListRule`) — dieselbe Zeile in Übung, Klausur und Vater-Vorschau
    wäre sonst dreimal gepflegt worden.
  - **Tests:** `PositionPracticeFlowTests.Selbsteinschaetzung_DecktJedeGleichwertigeUebersetzungAuf`
    (Alternative auf der Selbsteinschätzung da, auf der getippten Stufe `reveal` **und**
    `revealAlternatives` `null`), `PositionTestFlowTests.Rueckwaerts_DecktKeineAlternativeAuf`
    (Akzeptanzkriterium 3), `RevealAlternatives.test.tsx` (Anzeige + die drei leeren Vertragsformen).
    Die Rot-Probe ist trivial erfüllt: das Feld existierte vorher nicht, der Test hätte es nicht lesen können.
  - **Verifikation:** Backend **708/708 grün**, Frontend **116/116** (Vitest) und `tsc -b`/Vite-Build sauber.
    `ConventionGuardTests` grün **ohne** neuen Eintrag in `SolutionFieldExceptions` (Akzeptanzkriterium 7,
    Entscheidung 4 bestätigt). **Nicht** live nachgespielt: der Demo-Datenstand trägt keine Vokabel mit
    erklärten Alternativen, und dafür welche anzulegen hätte Testdaten in den Demo-Bestand geschrieben — der
    Beleg ist stattdessen der Integrationstest an der echten Card-Antwort. Offen für die Abnahme: Commit.
- **2026-08-04** — **Reviews (`pugling-reviewer` + `frontend-reviewer`), Befunde eingearbeitet:** beide ohne
  Blocker. Der Backend-Reviewer bestätigt die Anti-Cheat-Grenze ausdrücklich nachgeprüft (Index 0 ist in allen
  drei Bauwegen die Primärantwort, alle Cloze-Stufen und MC/Buchstaben/Audio sind getippt → kein neues
  Preisgeben, auch nicht in der Klausur) und `ConventionGuardTests` grün ohne neue Ausnahme. Aus dem
  Frontend-Review übernommen: der Abstand steht jetzt in `.reveal-alternatives` (index.css) statt als
  `marginBottom`-Prop an zwei Aufrufern — die UA-Vorgabe eines `<p>` hätte sonst ein Loch zwischen Lösung und
  „auch richtig" gerissen; dazu der `card.reveal`-Guard in `SohnPractice`, damit „auch richtig" nie ohne die
  Antwort steht, zu der es gehört.
  **Verifikation nach dem Review:** Backend **709/709 grün**, Frontend **118/118** (Vitest), Build sauber.
- **2026-08-04** — **abgenommen.** Verifikation belegt: Backend **709/709 grün**, Frontend **118/118**
  (Vitest) und `tsc -b`/Vite-Build sauber; `pugling-reviewer` **und** `frontend-reviewer` ohne Blocker, ihre
  „Sollte"-Befunde eingearbeitet. Kein `/smoke-test`: der Beleg ist der Integrationstest an der echten
  Card-Antwort (im Demo-Datenstand trägt keine Vokabel erklärte Alternativen, siehe Eintrag oben).
  Commit `88ca9e8`.
