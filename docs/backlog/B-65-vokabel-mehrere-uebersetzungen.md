---
tags: [typ/story, status/abgenommen, bereich/katalog, bereich/training, bereich/frontend, lerntechnik/vokabeln, rolle/student, rolle/creator]
aliases: [Vokabel 1:n Übersetzung, Mehrfachdeutung, TranslationAlternatives]
status: abgenommen
prio: P1
art: Defekt
groesse: M
wo: beides
migration: ja
vertragsbruch: nein
quelle: remark #11, #12, #13 (Punkt 2)
nachgeschaut: "2026-08-07"
---

# B-65 · Eine Vokabel mit zwei richtigen Übersetzungen wertet eine davon falsch

## User Story

Als Sohn möchte ich für eine Vokabel mit mehreren gültigen Übersetzungen **jede** davon als richtig gewertet
bekommen, damit ich nicht für Wissen bestraft werde, das ich habe.

## Ist-Stand am Code

- Eine `Vocabulary`-Zeile ist genau **ein** Paar `Word → Translation`
  ([VocabEntities.cs:19-22](../../backend/Pugling.Api/Models/VocabEntities.cs)); der Eintrag hat **keinen
  Eigentümer**, und der Store ist für jeden Creator schreibbar
  ([VocabularyStoreController.cs:24](../../backend/Pugling.Api/Controllers/Creator/VocabularyStoreController.cs)).
- Ein `Alternatives`-Feld gibt es **nur an der Lücke**, nicht an der Vokabel:
  `AnswerGrader.MatchesGap` prüft `gap.Alternatives`
  ([AnswerGrader.cs:19-24](../../backend/Pugling.Api/Services/Shared/AnswerGrader.cs)),
  `AnswerGrader.Matches` vergleicht auf exakte Gleichheit nach Normalisierung gegen die eine gespeicherte
  Lösung ([AnswerGrader.cs:12-16](../../backend/Pugling.Api/Services/Shared/AnswerGrader.cs)).
- Der Richtungstausch vertauscht Frage und Antwort je Item
  ([ExerciseContentProvider.cs:50-55](../../backend/Pugling.Api/Services/Shared/ExerciseContentProvider.cs))
  und **verwirft Alternativen ausdrücklich**, weil sie zur alten Antwort gehörten (`:57`).
- Multiple-Choice zieht Distraktoren aus den übrigen Items und dedupliziert über `Normalize`
  ([VocabularyExerciseType.cs:52-57](../../backend/Pugling.Api/Exercises/VocabularyExerciseType.cs)) —
  zwei Zeilen mit derselben Frage liefern hier zwei *unterschiedliche* Antworten als Auswahl, von denen
  eine als falsch gilt.
- Beim Buchstabenkästchen kommt die Kästchenzahl aus `item.Answer.Length`
  ([VocabularyExerciseType.cs:80](../../backend/Pugling.Api/Exercises/VocabularyExerciseType.cs)).
- **Zwei Zeilen mit gleichem `Word` bedeuten im Projekt bereits etwas**, nämlich ein **Homonym**: der
  Birkenbihl-Dekoder liefert genau darum Kandidaten zur Auswahl
  ([BirkenbihlDecodingService.cs:56](../../backend/Pugling.Api/Services/Creator/BirkenbihlDecodingService.cs),
  [ExerciseAuthoringDtos.cs:61](../../backend/Pugling.Contracts/Creator/ExerciseAuthoringDtos.cs)).
- Alternativen sind im Vertrag durchgehend `List<string>?`
  ([ExerciseConfigs.cs:80,154,218](../../backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs)) — im UI
  aber ein **einzelnes Komma-Feld**, das beim Senden zerlegt und beim Laden wieder zusammengefügt wird
  ([exerciseConfig.tsx:67-74,493,501,521](../../frontend/src/vater/exerciseConfig.tsx)).

## Die echte Lücke

Enger als die Anmerkung vermutete — und asymmetrisch:

Der Nutzer schlug vor, entweder „ein Eintrag pro Kombination" oder ein Übersetzungs-Array zu bauen. **Ein
Eintrag pro Kombination ist der Ist-Zustand**, und er trägt in genau eine Richtung:

- **Rückwärts (de → en) funktioniert er.** Zwei Zeilen `huge→riesig` und `huge→sehr groß` liefern nach dem
  Tausch `riesig→huge` und `sehr groß→huge`; beide sind richtig.
- **Vorwärts (en → de) nicht.** Das Kind sieht `huge` zweimal; jedes Item hat genau eine erwartete Lösung,
  und die jeweils andere korrekte Antwort wird als falsch gewertet.

Die Lücke ist damit nicht „das Modell kennt keine Mehrfachdeutung", sondern: **es fehlt die
Gleichwertigkeit mehrerer Lösungen zu einer Frage.** Das wirkt auf drei Stufen (Buchstabenkästchen,
Freitext, Hören) und über Multiple-Choice hinaus bis in den Malus — `PenaltyCoins` hängt an der
Zielerreichung.

**Die Falle dabei** (Grill-Runde, Entscheidung 1): Am Datenbestand sieht ein Synonympaar (`huge → riesig` /
`huge → sehr groß`, austauschbar) **identisch** aus wie ein Homonympaar (`bank → Bank` / `bank → Ufer`,
nicht austauschbar). Eine Regel „mehrere Übersetzungen desselben Worts gelten alle" hätte aus dem Defekt
„richtige Antwort wird falsch gewertet" den umgekehrten gemacht — und der ist schlimmer, weil er unsichtbar
ist.

## Offene Punkte

> Grill-Runde vom 2026-08-02 — alle geschlossen, siehe Entscheidungen.

1. ~~Alternativen an der Vokabel oder am Übungs-Item?~~ → Entscheidung 2
2. ~~Neues Feld `Alternatives` oder Beziehung `Vocabulary ↔ Vocabulary`?~~ → Entscheidung 2
3. ~~Was passiert beim Richtungstausch?~~ → Entscheidung 3
4. ~~Was passiert mit doppelten Fragen im Bestand?~~ → Entscheidung 6, Zusammenführen zurückgestellt
5. ~~Multiple-Choice: Alternative nie als Distraktor~~ → zwingende Folge aus Entscheidung 1, siehe unten

Neu aufgeworfen und ebenfalls geschlossen: die Kästchen-Stufe (Entscheidung 4), der Zuschnitt
(Entscheidung 5) und die Eingabeform im Editor (Entscheidung 7).

## Entscheidungen

1. **Gleichwertigkeit wird ausdrücklich erklärt, nicht aus gleichem `Word` abgeleitet.** Zwei Zeilen mit
   demselben Wort bleiben per Vorgabe verschiedene Bedeutungen — genau wie der Birkenbihl-Dekoder sie heute
   behandelt (`BirkenbihlDecodingService.cs:56`). *Kosten:* der Creator muss die Gleichwertigkeit pflegen;
   ungepflegte Altdaten verhalten sich unverändert falsch. Das ist der Preis dafür, dass der Fix keinen
   stillen Fehler in die Gegenrichtung einbaut (`Ufer` als gültige Antwort auf `bank` im Geld-Kapitel).
2. **Träger ist der Vokabeleintrag: `Vocabulary.TranslationAlternatives` als JSON-Liste**, Muster
   `Gap.Alternatives` (`ExerciseConfigs.cs:80`). Begründung: dass `sehr groß` für `huge` gilt, ist eine
   Eigenschaft des Wortes, nicht der Übung — einmal gepflegt, wirkt es überall. *Kosten:* der Store ist
   eigentümerlos und global schreibbar (`VocabularyStoreController.cs:24`); wer eine Alternative einträgt,
   lockert die Bewertung für alle Familien. Dasselbe Vertrauensmodell gilt aber schon für `Word` und
   `Translation` selbst — der Umbau macht es nicht schlechter. Eine echte Beziehung
   `Vocabulary ↔ Vocabulary` wäre sauberer für die Rückrichtung, kostet aber eine Join-Tabelle und die
   Abgrenzung „Synonym gegenüber eigener Vokabel", die niemand gestellt hat.
3. **Nur die Ziel-Seite; das Feld heißt darum `TranslationAlternatives`, nicht `Alternatives`.** Beim
   Richtungstausch wird es verworfen — das ist das heutige Verhalten (`ExerciseContentProvider.cs:57`),
   jetzt aber als benannte Regel statt als Nebenwirkung. Begründung: ein Feld ohne Seitenangabe ist genau
   die Unschärfe, die den Wegwerf-Kommentar dort nötig gemacht hat. *Kosten:* rückwärts (`riesig → ?`)
   bleibt es bei einer akzeptierten Antwort; wer dort mehr will, legt wie heute eine zweite Zeile an.
4. **Buchstabenkästchen: die Kästchenzahl kommt weiter aus der primären `Translation`, der Grader
   akzeptiert jede Alternative.** Eine gleich lange Alternative wird damit richtig gewertet — genau der in
   Anmerkung #13 gemeldete Fall; eine anderslange ist an dieser Stufe nicht tippbar, zählt aber bei
   Freitext und Hören. Begründung: die Kästchenzahl ist Teil der *Frage*, nicht der Bewertung. *Kosten:*
   sie verrät, welche der Antworten gemeint ist. Die Gegenvariante („längste Alternative bestimmt die
   Zahl") wurde verworfen: leere Endfelder sind ein noch stärkerer Hinweis, und `Normalize` müsste
   nachlaufende Leerzeichen tolerieren — die Stufe würde leichter statt genauer.
5. **Zuschnitt `wo: beides`, Backend zuerst, Editor-Feld gehört dazu.** Begründung: ein
   `TranslationAlternatives`, das niemand befüllen kann, behebt den Defekt nicht — die Anmerkung kam vom
   Creator-Platz. *Kosten:* die Story wächst von S auf M. Ein Bestands-Werkzeug zum Zusammenführen
   heutiger Dubletten ist **ausdrücklich nicht** dabei: es träfe fremde Daten im eigentümerlosen Store und
   müsste Item-Referenzen und Lernstand umhängen — ein eigener Umbau, größer als der Defekt.
6. **Der Editor weist beim Anlegen auf eine bestehende Zeile mit gleichem Wort hin, ohne sie zu
   verbieten.** Nachschlagen über den vorhandenen `vocabulary/lookup`
   (`VocabularyStoreController.cs:408`). Begründung: die Dublette ist heute der bequemste Weg und erzeugt
   den Defekt immer wieder neu; eine harte Sperre scheidet aus, weil Homonyme zwei Zeilen **brauchen**
   (Entscheidung 1). *Kosten:* eine zusätzliche Abfrage beim Anlegen. Eine „als Alternative übernehmen"-
   Schaltfläche wurde verworfen: sie schriebe mit einem Klick in einen fremden Eintrag, ohne dass sichtbar
   ist, wessen Übungen das trifft.
7. **Im Editor bekommt jede Variante ein eigenes Feld — kein kommagetrenntes Sammelfeld.** Gebaut als
   **wiederverwendbare Komponente** (Feld je Variante, „+ Variante", Entfernen). Begründung: das Komma-Feld
   der drei bestehenden Editoren (`exerciseConfig.tsx:493,501,521`) hat einen echten Fehler — eine
   Übersetzung, die selbst ein Komma enthält, ist nicht eintragbar, und `splitList` (`:67-71`) zerreißt sie
   stillschweigend. *Kosten:* das Komma-Muster lebt vorübergehend neben dem neuen; die drei bestehenden
   Stellen zieht [B-69](B-69-wiederhol-felder-alternativen.md) nach. Sie sofort mitzunehmen hätte die
   Defekt-Story auf L getrieben und drei Übungstypen samt Rückweg (`joinList`) und E2E berührt.

**Zwingende Folgen, keine eigenen Entscheidungen:**

- Die Alternativen wandern in die `seen`-Menge der Multiple-Choice-Distraktoren
  (`VocabularyExerciseType.cs:52-57`) — eine als gleichwertig erklärte Antwort darf nie als falsche Option
  erscheinen.
- Die neue JSON-Spalte braucht einen `ValueComparer` (Tor **G7**, `Data/JsonValueComparer.cs`).
- Die Schemaänderung faltet die Migrationskette neu (`migration: ja`). `Pugling.Contracts` bekommt ein
  **additives** Feld — das ist kein Vertragsbruch.

## Akzeptanzkriterien

1. Ein Vokabeleintrag trägt beliebig viele gleichwertige Übersetzungen; jede wird bei Freitext, Hören und
   Buchstabenkästchen als richtig gewertet.
2. Zwei Einträge mit gleichem `Word` gelten **nicht** automatisch als gleichwertig — der Homonymfall
   verhält sich unverändert.
3. Der Richtungstausch verwirft die Alternativen; die Rückrichtung verhält sich wie heute.
4. Eine gleichwertige Alternative erscheint nie als Multiple-Choice-Distraktor derselben Frage.
5. Beim Buchstabenkästchen kommt die Kästchenzahl aus der primären Übersetzung; eine gleich lange
   Alternative wird angenommen.
6. Im Vokabel-Editor hat jede Variante ein eigenes Eingabefeld, mit „+ Variante" und Entfernen; eine
   Übersetzung mit Komma lässt sich eintragen und kommt unverändert zurück.
7. Der Editor weist beim Anlegen auf einen bestehenden Eintrag mit gleichem Wort hin und lässt das Anlegen
   trotzdem zu.
8. Ein **Regressionstest, der vorher rot ist**: zwei gleichwertige Übersetzungen, die zweite wird
   angenommen. Dazu ein Test, der belegt, dass ein Homonym-Paar sich **nicht** gegenseitig akzeptiert.
9. Der Lernstand je Item (`ItemProgress`) bleibt stabil — die Item-Ids ändern sich nicht.

## Schätzung

**Größe: M** — vergleichbar mit [B-03](B-03-lueckensaetze-mit-bild.md) (vokabel-basierter Batch-Pfad im
`MediaSelector`). Nicht S, weil Schema, Vertrag, vier Controller-Stellen und eine neue Frontend-Komponente
zusammenkommen; nicht L, weil an keiner Stelle Bestandsdaten umgezogen werden.

**Beim Messen kam der Ausschlag nach unten:** die Bewertungsstrecke kann längst mehrere Antworten.
`ContentItem` trägt bereits `AcceptedAnswers` als Liste
([ExerciseContentProvider.cs:17-28](../../backend/Pugling.Api/Services/Shared/ExerciseContentProvider.cs)),
und **alle vier** Auswertungsstellen fragen sie schon so ab
(`PositionPracticeController.cs:265`, `PositionTestsController.cs:229,301`,
`ExercisePreviewService.cs:63`). Für Lückentext, Liste und Übersetzung wird sie über den Helfer
`Accepted(answer, alternatives)` befüllt — nur die Vokabel setzt hart `[v.Translation]`
([ExerciseContentResolver.cs:97](../../backend/Pugling.Api/Services/Shared/ExerciseContentResolver.cs)).

**Zwei Entscheidungen kosten dadurch nichts:**

- Entscheidung 3 (Alternativen fallen beim Richtungstausch weg) ist bereits so gebaut:
  `Swap` setzt `AcceptedAnswers = [it.Prompt]` (`ExerciseContentProvider.cs:64`).
- Entscheidung 4 (Kästchenzahl aus der primären Übersetzung) ebenfalls: `StageFacets` rechnet auf
  `item.Answer.Length` (`VocabularyExerciseType.cs:80`), und `Answer` bleibt die primäre.

### Angriffsplan — Backend zuerst

1. **Modell.** `Vocabulary.TranslationAlternatives` als `List<string>` (JSON-Spalte). Dabei **beide Tore
   bedienen**: Eintrag in `UnlimitedByDesign` (**G3**, `PuglingDbContext.cs:964-981` — sonst greift die
   Längen-Konvention und schneidet die Liste bei 200 Zeichen ab) und ein `ValueComparer` (**G7**,
   `Data/JsonValueComparer.cs`). Danach die Migrationskette **neu falten**.
2. **Ausspielung.** Eine Zeile: `ExerciseContentResolver.cs:97` von `[v.Translation]` auf
   `Accepted(v.Translation, v.TranslationAlternatives)` (der Helfer steht direkt darunter, `:130`).
3. **Multiple-Choice.** `VocabularyExerciseType.Choices` dedupliziert über `other.Answer`
   (`:52-57`) — auf `other.AcceptedAnswers` umstellen und die Alternativen des *eigenen* Items in `seen`
   aufnehmen, damit keine gleichwertige Antwort als Distraktor erscheint.
4. **Vertrag.** Drei Records in `Creator/VocabularyStoreDtos.cs`: `VocabularyResponse` (Feld ergänzen),
   `CreateVocabularyDto` (optionaler Parameter), `UpdateVocabularyDto` (Feld **plus**
   `ClearTranslationAlternatives`). Die neuen Parameter **mit Vorgabewert** anhängen — `UpdateVocabularyDto`
   hat heute keine Vorgaben, ohne sie bräche jeder positionelle Aufruf in Client und Tests.
5. **Controller.** `VocabularyStoreController`: Projektion plus vier Schreibstellen — `POST` (`:188`),
   `PATCH` (`:272`), `POST batch` (`:451`), `PATCH batch` (`:480`).
6. **Frontend.** Wiederhol-Feld-Komponente (ein Feld je Variante, „+ Variante", Entfernen), eingebaut an
   beiden Stellen in `VaterVocab.tsx`: Anlege-Zeilen (`:122`, `createVocabularyBatch`) und Bearbeiten
   (`:323`, `updateVocabulary`). Dazu der Dublettenhinweis über den vorhandenen
   `vocabulary/lookup`. Vorher `npm run gen:contract` — die Typen kommen aus dem Dokument, nicht aus der Hand.

### Risiken

- **G3 und G7 werden beim ersten Lauf rot** — das ist ihr Zweck, nicht ein Fehler. Wer G3 übersieht,
  bekommt eine stillschweigend abgeschnittene Liste; wer G7 übersieht, verliert Änderungen, solange
  niemand die Liste neu zuweist.
- **`ClearTranslationAlternatives` zieht einen Test nach.** Der reflexive Wächter in
  `PatchSemanticsTests.cs:425-449` verlangt zu **jedem** Clear-Schalter im Vertrag einen Fall in seiner
  Tabelle und wird sonst rot („Clear switch without a test"). Vorbild: `ClearWordBank`.
- **Der Store ist eigentümerlos** (Entscheidung 2) — es gibt keine Rechteprüfung nachzuziehen, aber auch
  keine, die einen Missbrauch bremst. Bewusst so.
- Kein Bestandsrisiko: alte Zeilen bekommen eine leere Liste und verhalten sich exakt wie heute.

### Testweg

- **Regression (muss vorher rot sein):** `PositionTestFlowTests` — Vokabel mit
  `TranslationAlternatives`, die zweite Antwort wird angenommen. Dazu der Gegen-Test, dass zwei Zeilen mit
  gleichem `Word` sich **nicht** gegenseitig akzeptieren (Homonym, Entscheidung 1).
- **Multiple-Choice:** `PositionPlayChoicesTests` — eine gleichwertige Alternative taucht nie als Option
  derselben Frage auf.
- **Store-CRUD und Batch:** `VocabularyStoreTests` (Anlegen, PATCH, beide Batch-Wege) und
  `VocabAgentApiTests` für den Client-Pfad.
- **PATCH-Semantik:** Fall in der Tabelle von `PatchSemanticsTests` (siehe Risiken).
- **Frontend:** Vitest/RTL für die Wiederhol-Feld-Komponente — Variante hinzufügen, entfernen, und eine
  Übersetzung **mit Komma** kommt unverändert zurück (das ist der Fehler, den das Komma-Feld hat).
- Zum Schluss `/smoke-test` und `pugling-reviewer` **plus** `frontend-reviewer` (`wo: beides`).

## Verlauf

- **2026-08-02** — angelegt aus den Anmerkungen #11, #12 und #13 (Punkt 2); Ist-Stand am Code belegt,
  Befund: [befund-2026-08-02.md](../anmerkungen/befund-2026-08-02.md#c--vokabel-mehrdeutigkeit-11-12-13-punkt-2).
- **2026-08-02** — `gegrillt → geschaetzt`. **M**, `migration: ja`, `vertragsbruch: nein`. Das Messen hat
  die Story kleiner gemacht als gedacht: `ContentItem.AcceptedAnswers` ist längst eine Liste und alle vier
  Auswertungsstellen fragen sie so ab — die Ausspielung ist **eine Zeile**
  (`ExerciseContentResolver.cs:97`), und die Entscheidungen 3 und 4 sind bereits gebaut. Teuer sind
  stattdessen die Ränder: zwei Schema-Tore (G3, G7), der Clear-Schalter samt Wächter-Fall und die
  Frontend-Komponente.
- **2026-08-02** — `ausformuliert → gegrillt`. Sieben Entscheidungen; tragend ist Entscheidung 1
  (Gleichwertigkeit wird erklärt, nicht aus gleichem Wort abgeleitet) — sie verhindert, dass der Fix
  Homonyme gegenseitig als richtig wertet. Entscheidung 7 (ein Feld je Variante) hat
  [B-69](B-69-wiederhol-felder-alternativen.md) abgeworfen.
- **2026-08-02** — `geschaetzt → in-arbeit`. Backend zuerst, nach dem Angriffsplan; der Kern war die
  vorhergesagte **eine Zeile** (`ExerciseContentResolver.cs:97`), teuer waren wie geschätzt die Ränder.
  **Rot-Probe gefahren:** mit der alten Zeile fällt `Test_GleichwertigeUebersetzung_WirdAlsRichtigGewertet`
  (`correctItems 0` statt 1) — der Defekt ist belegt, nicht behauptet; der Homonym-Gegentest bleibt unter
  beiden Regeln grün, wie es sein soll. Zwei Abweichungen vom Angriffsplan, beide klein:
  1. **`TranslationAlternatives` ist nullable** (`List<string>?`, Muster `ClozeText.WordBank` statt einer
     stets vorhandenen Liste). Grund: nur so hat „keine erklärt" **eine** Schreibweise — `[]` und `null`
     wären zwei —, und nur so passt der Clear-Schalter in die Tabelle von `PatchSemanticsTests`, die nach
     dem Leeren einen `null`-Wert liest. Der Controller normalisiert eine leer gewordene Liste darum auf
     `null` (`CleanAlternatives`).
  2. **Die Multiple-Choice-Entdopplung prüft beide Seiten**, nicht nur die eigenen Alternativen: erklärt
     allein die Gegenkarte die Gleichwertigkeit, wäre ihre Primärantwort sonst weiter ein Ablenker. Ein
     Kandidat fällt darum, sobald **irgendeine** seiner akzeptierten Antworten schon gesehen ist.
- **2026-08-02** — `in-arbeit → abgenommen`. **636 Tests grün** (Basis 628, +8), Vitest **54 grün** in
  8 Dateien, `dotnet build Pugling.sln` und `npm run build` sauber, `markdownlint-cli2` 0 Befunde,
  `/smoke-test` vollständig grün (13 Checks) und das neue Feld zusätzlich per `curl` gegen die laufende
  Instanz geprüft: `null` lässt stehen, `clearTranslationAlternatives` leert, `lookup` findet die Dublette
  groß-/kleinschreibungsunabhängig. Alle neun Akzeptanzkriterien erfüllt. Zwei davon ruhten zunächst nur
  auf bestehendem Verhalten und haben jetzt einen eigenen Test: **AK5** (Buchstabenkästchen, auf Anstoß des
  Reviews — `TestStage.LetterBoxes` mit gleich langer Alternative, `nice → nett` gegen `lieb`) und **AK3**
  (rückwärts wird die Alternative *nicht* angenommen). Der AK3-Test ist zugleich nicht leer zu bekommen:
  vorwärts *wäre* dieselbe Antwort richtig, er beweist den Richtungstausch also mit.

  **`pugling-reviewer`**: Kern bestätigt (G3, G7, Kettenlänge 1, Snapshot-Diff genau drei Zeilen,
  Entscheidung 1 gehalten, Alternativen verlassen den Server nirgends). Behoben: `CleanAlternatives`
  entfernt jetzt auch die **primäre** Übersetzung aus der Liste (sonst „auch: riesig" neben „riesig"), und
  der Vertrag sagt, dass eine ausdrücklich leere Liste ebenfalls leert. Stehen geblieben, bewusst: dass
  `BatchUpdateItem` nicht ins Raster von `PatchSemanticsTests.UpdateDtos()` fällt (`Update…Dto`/`…Request`)
  — heute durch `VocabularyStoreTests` ausdrücklich abgedeckt, das Raster zu weiten ist eine eigene
  Aufräum-Entscheidung.

  **`frontend-reviewer`**: Vertragstreue, PATCH-Semantik, Schreib-Primitive und der Rückweg bestätigt.
  Behoben: gleichnamige Felder bei zwei offenen Store-Editoren (`scope` durchgereicht), der zugängliche
  Name des Hinzufügen-Knopfs enthält jetzt seinen sichtbaren Text (WCAG 2.5.3, und `addLabel` schlägt
  wieder durch), der Dublettenhinweis trägt das **nachgeschlagene** Wort mit sich (er behauptete sonst
  beim Weitertippen etwas Ungeprüftes) und schreibt nicht mehr auf eine inzwischen verschobene Zeile, die
  Live-Region steht dauerhaft im DOM, Enter setzt den Fokus ins neue Feld und stapelt keine leeren mehr,
  und die doppelte Formulierung ist zu einem `HelpTopic` (`translationAlternatives`) zusammengezogen.
  Dazu ein Knopf „Eintrag unten zeigen", der die Store-Suche auf das Wort stellt.

  Zwei Nebenbefunde sind **nicht** hier eingebaut, sondern als Ideen abgelegt:
  [B-70](B-70-selbsteinschaetzung-nur-primaerloesung.md) (die Selbsteinschätzung deckt weiter nur die
  primäre Übersetzung auf) und [B-71](B-71-inline-vokabelliste-ohne-varianten.md) (die Inline-Liste im
  Übungs-Editor kann keine Varianten anlegen).

  Commits: `189fdbe` (Backend inkl. Review-Nachlauf), `0ca8e7b` (Frontend inkl. Review-Nachlauf), dazu
  dieser Doku-Commit mit der Abnahme und den zwei abgeleiteten Ideen.

- 2026-08-02 – **Nachlauf aus einem zweiten Review-Durchgang.** Ein Befund war echt: `CleanAlternatives`
  lief nur, wenn der PATCH die Varianten *mitschickte*. Ein PATCH, der allein die Übersetzung auf ein Wort
  setzt, das schon als Variante steht (`riesig` → `sehr groß`), ließ sie doppelt stehen – über die
  Oberfläche unsichtbar, weil der Editor die Liste immer mitsendet, über API und Agent aber nicht. Die
  Reinigung läuft jetzt **unbedingt** und gegen die Übersetzung *nach* dem PATCH; der Wert-vor-Schalter-
  Ablauf bleibt. Der Regressionstest dazu ist vorher rot (`["sehr groß"]` statt `null`).
  Suite 637 grün, Vitest 54, `npm run build` und markdownlint sauber.
- **2026-08-07** — Nachschau (Nachtlauf): geprüft, ob `Vocabulary.TranslationAlternatives` weiterhin
  existiert und die Multiple-Choice-Entdopplung weiterhin beide Seiten (eigene und fremde
  `AcceptedAnswers`) prüft — hält (`VocabEntities.cs:34`, Logik jetzt in `StageMechanics.cs:65-85`, reine
  Extraktion ohne Verhaltensänderung seit B-106). Kein Fund.
