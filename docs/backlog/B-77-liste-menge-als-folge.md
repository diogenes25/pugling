---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/frontend, rolle/student]
aliases: [Liste als Menge, 16 gleiche Karten, Ungeordnete Liste im Übungs-Pfad,
  Der Übungs-Pfad bewertet eine ungeordnete Liste als Folge]
status: abgenommen
prio: P1
art: Defekt
groesse: M
wo: beides
migration: nein
vertragsbruch: ja
quelle: B-76 (Grill-Runde, Entscheidung 1)
---

# B-77 · Beim Spielen wird eine ungeordnete Liste als Folge bewertet

## User Story

Als **Kind** möchte ich bei einer ungeordneten Liste eine Karte bekommen, die ich beantworten kann — eine,
auf der **jede noch nicht genannte** Antwort zählt —, damit ich meine Wochenpflicht „Nenne alle 16
Bundesländer" erfüllen kann, statt zu raten, welches der sechzehn gerade gemeint ist.

## Ist-Stand am Code

### 1 · Es gibt zwei Bewertungen, und die richtige erreicht kein Kind

`ListExerciseType` trägt beide Sichten in einer Klasse
([BuiltInExerciseTypes.cs:297-341](../../backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs)):

- **`ItemsOf` (`:308-312`)** baut je Eintrag eine Karte. `AcceptedAnswers` enthält genau *diesen* Eintrag
  (plus seine Alternativen), der `Prompt` ist für alle Karten dasselbe `Instruction`-Feld. Das ist eine
  Folge.
- **`Check` (`:314-340`)** unterscheidet: `Ordered` wertet positionsgenau über den Index, ungeordnet als
  **Menge** — jede Antwort darf auf jeden noch offenen Eintrag passen, und ein Treffer wird verbraucht
  (`remaining[hit] = " "`, `:336`), damit dieselbe Nennung nicht zweimal zählt.

`Check` hängt aber allein am Katalog-Endpunkt `POST creator/subjects/{}/chapters/{}/list/{id}/check`
([ExerciseControllers.cs:703-716](../../backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs) →
[ExerciseControllerBase.cs:38-44](../../backend/Pugling.Api/Controllers/Creator/ExerciseControllerBase.cs)).
Jeder Weg, auf dem ein Kind antwortet, geht an ihr vorbei und vergleicht gegen `item.AcceptedAnswers`:

| Weg | Stelle |
|---|---|
| Üben | [PositionPracticeController.cs:264-266](../../backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs) |
| Klausur, Frage für Frage | [PositionTestsController.cs:230-232](../../backend/Pugling.Api/Controllers/Student/PositionTestsController.cs) |
| Klausur, Sammelabgabe | `PositionTestsController.cs:302-304` |
| Testmodus des Vaters | [ExercisePreviewService.cs:60-64](../../backend/Pugling.Api/Services/Creator/ExercisePreviewService.cs) |

Am laufenden System gemessen (drei Einträge, Freitext-Stufe):

```json
[{"itemIndex":0,"prompt":"Nenne die Bundeslaender.","reveal":null,"choices":null},
 {"itemIndex":1,"prompt":"Nenne die Bundeslaender.","reveal":null,"choices":null},
 {"itemIndex":2,"prompt":"Nenne die Bundeslaender.","reveal":null,"choices":null}]
```

**Die Mengen-Logik ist getestet — und trotzdem blind.**
[CatalogExerciseTests.cs:32-71](../../backend/Pugling.Api.Tests/CatalogExerciseTests.cs) prüft grün, dass
drei Bundesländer in beliebiger Reihenfolge 100 % ergeben; `:73-108` prüft den geordneten Fall. Beide
gehen über den Creator-Endpunkt, an dem kein Kind sitzt. Das ist genau die Fehlerklasse „Regel getestet,
Grenzfall offen" aus [docs/testplan.md](../testplan.md).

### 2 · Die Klausur ist nicht ausgeschlossen — sie ist der einzige Weg

Hier korrigiert sich die Idee. Sie vermutete, `List` sei als `ExerciseCheckMode.CatalogCheck` vom
Abschlusstest ausgenommen. Das Gegenteil trifft zu, in beide Richtungen:

- **`PositionTestsController` kennt keine Check-Mode-Schranke.** `Start` blockt nur bei leerem Pool
  (`:97-99`); jede Position mit prüfbarem Inhalt ist testbar.
- **Für alles außer `None` ist die Klausur die einzige Erfüllung.**
  `PositionProgressService.IsGoalMetAsync` misst gespielte Runden nur bei `ExerciseCheckMode.None`
  ([:91-115](../../backend/Pugling.Api/Services/Shared/PositionProgressService.cs)); sonst zählt
  ausschließlich ein bestandener `TestAttempt` (`:117-119`).

Das Sohn-Web schließt den Kreis: `canPractice = pos.useLeitner || (!pos.testable && pos.checkMode ===
"None")` ([SohnHome.tsx:120](../../frontend/src/sohn/SohnHome.tsx)). Die geseedete Position hat kein
Leitner und ist `testable` — es gibt also **nur den TEST-Knopf** (`:141-145`). Und `ListExerciseType`
erbt `IsTypedStage => true`
([ExerciseTypeBase.cs:35](../../backend/Pugling.Api/Exercises/ExerciseTypeBase.cs)): getippt, ohne
`reveal`, ohne Auswahl.

**Was das Kind heute bekommt:** sechzehn zeichengleiche Freitext-Karten
([Seed.cs:1134-1142](../../backend/Pugling.Api/Data/Seed.cs) sind genau 16 Länder), Bestehensgrenze 90 %
(`Seed.cs:401-402`), zwei Versuche pro Tag (`PositionTestsController.cs:52`). Die Prüfungsreihenfolge
friert der Server nach `OrderStrategy = WeakestFirst` ein
([PlanPositionEntities.cs:41](../../backend/Pugling.Api/Models/PlanPositionEntities.cs)) — auch eine
gedachte Ordnung wäre also nicht ablesbar. Wer alle 16 Länder kennt und sie der Reihe nach eintippt,
trifft im Mittel **eine** Karte.

Weil `DayOverview.DutyDone` *jede* Pflicht verlangt (`PositionProgressService.cs:164-167`), hängt die
Tagespflicht der geseedeten Familie die ganze Woche an dieser einen unbestehbaren Klausur. Ein Münz-Malus
entsteht nicht — `PenaltyCoins` bleibt 0 (`PlanPositionEntities.cs:85`) —, „Pflicht erfüllt" aber ebenso
wenig.

### 3 · Der leere Prompt

`ItemsOf` setzt `c.Instruction ?? ""` (`:311`); die Anweisung ist optional im Vertrag
([ExerciseConfigs.cs:210-211](../../backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs)) und im
Editor ([exerciseConfig.tsx:346-349](../../frontend/src/vater/exerciseConfig.tsx)). `List` ist der
**einzige** Typ, dessen Karte ihren Prompt aus einem übungsweiten Feld zieht: `Matching` nimmt `p.Left`
(`:201`), `Arithmetic` den Aufgaben-Prompt (`:235`), `Grammar` den der Einzelaufgabe. Die geseedete Übung
setzt eine Anweisung (`Seed.cs:1164`), der leere Prompt trifft also nur selbst angelegte Listen.

Beachtenswert für die Reparatur: `ItemsOf(string configJson)` sieht **nur die Config**, nicht den
Übungstitel — ein Rückfall auf „Die 16 Bundesländer" ist an dieser Naht nicht zu haben.

### 4 · Die Testlage

- [ExerciseContentProviderTests.cs:94-108](../../backend/Pugling.Api.Tests/ExerciseContentProviderTests.cs)
  (`List_NutztInstructionAlsPromptUndUebernimmtAlternativen`) nagelt das defekte Verhalten ausdrücklich
  fest. Der Test ist grün **wegen** des Fehlers und muss mit umgeschrieben werden.
- **Kein Test spielt eine List-Position** — weder Übungsrunde noch Klausur. Die Abdeckung endet beim
  Anlegen und beim Auflösen, wie bei B-76 (dort R4).

## Die echte Lücke

Der Katalog-Check bewertet eine **Antwortmenge**, der Spielpfad bewertet **Karten**. Dazwischen steht
`ContentItem`, und das kennt nur „eine Karte = ein Atom = eine erwartete Antwort". In dieses Modell lässt
sich eine Menge nur auf eine Weise zerlegen — und diese Weise erfindet eine Ordnung, die es nicht gibt.

Deshalb ist es **nicht** der Fehler von [B-76](B-76-lueckentext-karte-ohne-luecke.md). Dort war die Karte
richtig gestellt und nur nicht adressiert; ein „3 von 16" reparierte sie. Hier stellt die Karte die
falsche Frage: sie fragt „welcher Eintrag steht an Position 3?", während die Übung „nenne einen, den du
noch nicht genannt hast" meint. Ein Etikett ändert daran nichts.

Zwei Dinge fehlen dafür in derselben Naht:

1. **Eine Bewertung, die über die Karte hinaussieht.** „Noch nicht genannt" ist eine Aussage über den
   ganzen Versuch, nicht über die Karte. Das Material liegt schon vor: `TestAttempt.Results` trägt jede
   Antwort des Versuchs (`PositionTestsController.cs:233-237`), `PracticeSession.Reviews` die der Runde.
2. **Ein Prompt, der zur Frage passt.** Solange 16 Karten denselben Satz tragen, bliebe selbst die
   richtige Bewertung unsichtbar — das Kind wüsste nicht, dass es „irgendeines" nennen darf.

## Offene Punkte

1. ~~**Wie wird eine Menge kartenweise gespielt?**~~ → **E1**, und **E2** für die Verbuchung
2. ~~**Zieht die Klausur mit?**~~ → **E3**. Die Empfehlung, die hier stand („Hauptschauplatz, die
   Übungsrunde ist gar nicht erreichbar"), war nur zur Hälfte richtig: die geseedete Position ist wirklich
   nur testbar, eine Position mit `UseLeitner = true` aber nicht — also gehören beide Wege hinein.
3. ~~**Was ist mit `Ordered = true`?**~~ → **E6**, und die Empfehlung („getrennt behandeln") ist
   **verworfen**: die Adressierung liegt mit `itemIndex` schon auf der Karte, ein eigenes Feld braucht es
   nicht. Damit erledigt sich auch B-76/R2 für diesen Typ — `GapIndex` wird *nicht* mitbenutzt.
4. ~~**Was passiert mit der geseedeten Pflicht?**~~ → **E8**
5. ~~**Wird `Instruction` für `List` zur Pflicht?**~~ → **E7**
6. ~~**Was bedeutet der Umbau für den Lernstand?**~~ → durch **E3** miterledigt: `PositionItemProgress`
   bleibt je Eintrag geführt, weil E2 auf den getroffenen Eintrag verbucht — die Leitner-Kiste behält also
   ihre Bedeutung („dieses Bundesland fällt mir schwer"). Der plan-übergreifende Stand war ohnehin nicht
   betroffen (`SupportsItemProgress` ist `false`, `ExerciseTypeBase.cs:50`).

## Entscheidungen

Aus der Grill-Runde vom 2026-08-03. Zwei Empfehlungen der Ausformulierung hat sie widerlegt (siehe E3 und
E5), eine dritte billiger gemacht (E6).

### E1 · Die Menge wird über den Versuch geführt

Karte N verlangt **einen noch offenen Eintrag**, nicht den Eintrag Nr. N. Die Zahl der Karten bleibt die
Zahl der Einträge.

*Begründung.* Es ist die einzige Form, die beides behält: die fachliche Bedeutung („nenne alle") und das
Kartenmodell, an dem eingefrorene Reihenfolge, one-at-a-time, Punkte und Missionen hängen. Die Alternative
„eine Karte mit N Feldern" ist fachlich am nächsten, bricht aber `Order`/`Cursor`/`TestItemResult` je Index
und nimmt Leitner je Eintrag. Die Alternative „nur geordnete Listen sind spielbar" wäre die billigste
Reparatur und die schlechteste: der Creator müsste eine Reihenfolge erfinden, die es nicht gibt, und das
Kind sie mitlernen.

*Kosten.* Die Bewertung ist nicht mehr rein index-basiert — `ContentItem.AcceptedAnswers` allein reicht
nicht, es braucht einen typ-eigenen Haken neben `Check`/`Choices` in `IExerciseType`. Wie der heißt und wo
er sitzt, ist Bau und gehört in die Schätzung.

### E2 · Verbucht wird auf den getroffenen Eintrag, nicht auf den Kartenplatz

Die 16 Ergebniszeilen sind die 16 **Einträge**. Eine richtige Antwort belegt die Zeile ihres Eintrags; eine
Antwort ohne Treffer ist eine Fehlnennung und bekommt eine Zeile ohne Atom (`TestItemResult.ItemIndex` ist
schon nullable, [StudyPlanEntities.cs:169](../../backend/Pugling.Api/Models/StudyPlanEntities.cs)). Die
Punktzahl bleibt „belegte Einträge / alle Einträge" — identisch zu `Check`.

*Begründung.* Nur so trägt der Auswertungsschirm die Lehre der Übung: „Saarland — nicht genannt". Bliebe die
Zeile am Kartenplatz, wäre `ItemOutcome`s „Erwartet" entweder sinnlos („Erwartet: Bayern" auf Platz 3, wo
jede Antwort recht gewesen wäre) oder müsste unterdrückt werden — und das Kind erfährt nicht, was es
vergessen hat.

*Kosten.* `Answer` sucht die passende Zeile statt der Cursor-Zeile; die Gleichung Karte = Ergebniszeile
gilt nicht mehr. Das ist die Stelle, an der ein Leser stolpert, und sie braucht einen Kommentar.

### E3 · Beide Wege, mit unterschiedlicher Periode

Klausur: „schon genannt" gilt **je Versuch** (`TestItemResult`). Übungsrunde: **je Tag**, über die
vorhandene Tagessperre `PositionItemProgress.LastReviewedAt`.

*Begründung.* Die Ausformulierung hielt die Übungsrunde für teuer, weil ein `ReviewEvent` nur `WasCorrect`
und `At` trägt ([StudyPlanEntities.cs:114-121](../../backend/Pugling.Api/Models/StudyPlanEntities.cs)) —
ein „schon genannt" hätte also neuen Zustand gebraucht, samt neu gefalteter Migrationskette. Falsch: die
Anti-Farming-Sperre `scored = due && !alreadyScoredToday`
([PositionPracticeController.cs:277-279](../../backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs))
**ist** der Marker. Damit kostet der zweite Weg kein Schema. Und ihn auszulassen hieße, eine List-Position
mit `UseLeitner = true` kaputt zu lassen, die ein Vater heute anlegen kann — die Story wäre `abgenommen`
und der Defekt am Leben.

*Kosten.* Ein Begriff mit zwei Perioden (Versuch vs. Tag). Das gehört als Kommentar an die Bewertung,
sonst liest es sich wie eine Ungenauigkeit statt wie eine Entscheidung. Fachlich passt es: der Tag ist im
Übungspfad ohnehin die Einheit, in der nichts zweimal zählt.

### E4 · Eine Wiederholung ist eine Fehlnennung

Nennt das Kind einen Eintrag zum zweiten Mal, ist die Karte verbraucht — in beiden Wegen.

*Begründung.* Die treue Übersetzung von `Check`: dort verbraucht ein Treffer die Nennung
(`remaining[hit] = " "`, `BuiltInExerciseTypes.cs:336`), ein zweites „Bayern" trifft nichts mehr. Die
freundlichere Variante („Karte bleibt stehen, das hattest du schon") wäre in der Klausur ein Ausweg aus der
one-at-a-time-Strenge: unbegrenztes Weiterprobieren, bis dem Kind etwas Neues einfällt. Und zwei Verhalten
für dieselbe Eingabe — beim Üben milde, in der Prüfung streng — hieße, das Kind lernt eine Regel, die in
der Prüfung nicht gilt.

*Kosten.* Ein Doppler aus Unachtsamkeit kostet einen von 16 Plätzen, bei 90 % Bestehensgrenze spürbar. Kein
dritter Zustand neben richtig/falsch — also auch kein Vertragsfeld dafür.

### E5 · Die Karte trägt „ungeordnet", die Sprache macht das Frontend

`PracticeCard` und `TestItem` bekommen ein **additives** Feld (Arbeitsname `AnyOrder`); das Frontend
formuliert daraus die Spielregel und nutzt den vorhandenen Zähler aus `cursor`/`total`.

*Begründung.* Die Ausformulierung empfahl einen fertigen Server-Prompt („Nenne einen weiteren Eintrag (3
von 16)"). Das ist verworfen: es wäre neue **deutsche Produktsprache im Backend** — dort stehen bisher nur
Seed- und Ledger-Texte — und die spätere Mehrsprachigkeit ([B-38](B-38-mehrsprachige-oberflaeche.md))
müsste sie wieder herausoperieren. B-76/E2 hat dieselbe Abwägung schon so entschieden: Zustand vom Server,
Darstellung vom Frontend. Ganz ohne Feld geht es aber nicht: `Ordered` steckt in der `ConfigJson`, die kein
Kind sieht — eine geordnete und eine ungeordnete Liste sind für die Sohn-App heute zeichengleich und
verhalten sich nach E1 entgegengesetzt.

*Kosten.* Zwei Vertragsfelder (additiv, nullable) plus Artefakt-Neubau (`docs/openapi/v1.json`,
`openapi-examples.generated.json`, `docs/api-examples/`, `frontend/src/lib/contract.ts`) und ein Bauteil in
**beiden** Sohn-Ansichten. Es ist wieder ein Feld, das nur bei einem Typ trägt — wovor B-76/R2 gewarnt hat;
der Unterschied ist derselbe wie dort: es trägt bei genau dem Typ, der es braucht.

### E6 · Der geordnete Fall kommt mit — ohne neues Feld

Dasselbe Bauteil bedient beide Fälle: ohne `Ordered` „ein Eintrag, der noch nicht dran war", mit `Ordered`
„Eintrag 8" aus dem vorhandenen `itemIndex`.

*Begründung.* Die Adressierung liegt längst auf der Karte — `TestItem(item.Index, …)`
([PositionTestsController.cs:75](../../backend/Pugling.Api/Controllers/Student/PositionTestsController.cs)),
und bei einer Liste **ist** der Item-Index die Eintragsposition. Der Fragezähler taugt dafür nicht: die
Reihenfolge friert `OrderStrategy = WeakestFirst` ein, „Frage 3" ist also nicht „Eintrag 3". Damit ist der
Typ nach dieser Story ganz reparariert statt halb, und die Sohn-Ansichten werden einmal angefasst statt
zweimal.

*Kosten.* Ein Akzeptanzkriterium und ein Testfall mehr. Der Fall ist nicht geseedet, also unbelegt am
laufenden System — er wird gegen eine selbst angelegte Übung geprüft.

### E7 · `Instruction` wird für `List` zum Pflichtfeld

`ValidateConfigAsync`-Override im `ListController`: leere Anweisung → `validation_error`.

*Begründung.* Nach E5 formuliert das Frontend die *Spielregel*, nicht den *Gegenstand*. „Nenne einen
Eintrag, der noch nicht dran war" ohne „wovon" bleibt unlösbar, und der Übungstitel steht auf dem
Klausurschirm nicht (`SohnTest.tsx:130` zeigt „Tagestest" und die Stufe). Ein Rückfall auf den Titel ist an
`ItemsOf(string configJson)` nicht zu haben — die Signatur sieht nur die Config.

*Kosten.* Ein Override plus Testfall; der Haken existiert und läuft auf POST **und** PUT
([ExerciseControllerBase.cs:52, :235, :307](../../backend/Pugling.Api/Controllers/Creator/ExerciseControllerBase.cs)).
Eine bestehende Liste ohne Anweisung wird beim nächsten Speichern abgewiesen — der Seed hat eine
(`Seed.cs:1164`), es gibt also keine Altdaten-Lücke.

### E8 · Die geseedeten 90 % bleiben

*Begründung.* Nach der Reparatur sind 15 von 16 erreichbar; eine gesenkte Grenze wäre das Weichmachen der
Pflicht statt der Reparatur der Bewertung. Der Wert ist im Seed bewusst abweichend gesetzt
(`Seed.cs:392-402`), damit die Testdaten eine andere Schwelle als 80 % zeigen.

*Kosten.* Eine Verhaltensänderung an laufenden Daten: die Wochenpflicht wird scharf. Bislang ist `DutyDone`
für die geseedete Familie dauerhaft `false` — danach ist es erreichbar **und** reißbar (ohne Münz-Malus,
`PenaltyCoins` ist 0).

### E9 · Die Story heißt „Beim Spielen …"

Titel neu: „Beim Spielen wird eine ungeordnete Liste als Folge bewertet". Der alte Wortlaut bleibt als
Alias.

*Begründung.* „Übungs-Pfad" benennt in diesem Repo die kleinere Hälfte — gespielt wird „Üben/Leitner **+**
Abschlusstest" (Root-CLAUDE.md), und der Hauptschauplatz ist nach E3 die Klausur. Wer nur den Index liest,
hielte sie sonst für nicht betroffen: genau der Irrtum, den die Ausformulierung korrigiert hat.

*Kosten.* Eine Zeile im Index ändert sich. B-76 beschreibt den Defekt in seiner Entscheidung E1 weiter mit
dem alten Wortlaut — das bleibt so: eine abgenommene Story ist ein Protokoll, kein Nachschlagewerk.

### E10 · Eine Fehlnennung bewegt keine Kiste (Auflösung von R2)

Trifft eine Antwort keinen offenen Eintrag, wird **nichts** herabgestuft; die Antwort trägt `box = 0` und
`dueOn = null`.

*Begründung.* Vom Nutzer entschieden am 2026-08-03, nachdem die Schätzung die Lücke als R2 sichtbar gemacht
hatte. Eine Falschnennung darf keinen Eintrag herabstufen, den das Kind nie beansprucht hat — und die
Alternativen waren beide schlechter: den Eintrag des Kartenplatzes zu strafen träfe bei
`OrderStrategy = WeakestFirst` systematisch den schwächsten, und alle offenen zu strafen würfe mit einem
Tippfehler den Lernstand der ganzen Liste zurück.

*Kosten.* Leitner bestraft in diesem Typ nur das falsche **Wiederholen**, nicht das Nichtwissen: ein Eintrag,
den das Kind nie nennt, bleibt auf seiner Kiste stehen. Dafür kostet die Entscheidung im Frontend nichts —
beide Anzeigen (`box` im Erfolgs-Toast, `dueOn` als „Nächste Fälligkeit") stehen schon hinter einem
Wahrheits-Guard, den `0` bzw. `null` von selbst schließt.

### Abgeleitet, nicht erfragt

Beides mit dem Hinweis vorgelegt und unwidersprochen geblieben:

1. **`ReviewOutcome.correctAnswer`** gibt im Mengen-Modus den *getroffenen* Eintrag zurück, bei einer
   Fehlnennung `null`. Heute liefert es `item.Answer` des Kartenplatzes
   (`PositionPracticeController.cs:352`) — „richtig wäre: Hessen" ist willkürlich, solange vierzehn
   Einträge offen sind, und verrät zugleich einen Eintrag, der noch gefragt wird.
2. **`ItemOutcome`** folgt E2: eine Zeile je Eintrag (genannt / nicht genannt), die Fehlnennungen darunter.

## Akzeptanzkriterien

- Eine ungeordnete Liste liefert Karten, die das Kind beantworten kann: **jede noch nicht genannte**
  richtige Antwort zählt, unabhängig davon, auf welcher Karte sie steht — beim Üben **und** in der Klausur.
- Dieselbe Nennung zweimal zählt **einmal**; die zweite ist eine Fehlnennung (E4).
- Der Auswertungsschirm nennt die **vergessenen Einträge** (E2), nicht ein „Erwartet" je Kartenplatz.
- Die Karte sagt, welche Regel gilt: ungeordnet „irgendeiner, der noch nicht dran war", geordnet
  „Eintrag 8" (E5/E6). Die rohe Unterscheidung `Ordered` erreicht das Kind nie als Konfigurationsfeld.
- Eine Liste ohne Anweisung lässt sich nicht mehr anlegen oder speichern (E7); keine Karte trägt einen
  leeren Prompt.
- Die geseedete Position „Die 16 Bundesländer" ist über den Weg bestehbar, den das Sohn-Web anbietet
  (die Klausur), mit unveränderter Grenze von 90 % (E8).
- **Regressionstest, vorher rot:** eine gespielte List-Position, deren 16 richtige Antworten in beliebiger
  Reihenfolge 100 % ergeben. Heute zählt nur, was zufällig auf der passenden Karte landet.

## Schätzung

**M · beides · keine Migration · Vertragsbruch: ja.**

**Keine Migration**, und das ist nachgesehen, nicht gehofft: kein Entity ändert sich. Die Fehlnennung aus E2
braucht keine neue Spalte, weil `TestItemResult.ItemIndex` **schon** nullable ist
([StudyPlanEntities.cs:169](../../backend/Pugling.Api/Models/StudyPlanEntities.cs)) — es entstehen nur neue
Zeilen. Und „schon genannt" in der Übungsrunde liest die vorhandene Tagessperre (E3), statt Zustand
anzulegen.

**Vertragsbruch: ja**, an genau *einer* Stelle: `ReviewOutcome.Expected` wird von `string` zu `string?`
(abgeleitete Entscheidung 1). Das ist keine Erweiterung, sondern eine Aufweichung an einem Feld, das das
Frontend heute bedingungslos anzeigt (R3). Die drei anderen Vertragsänderungen sind **additiv**: `AnyOrder`
auf `PracticeCard` und `TestItem` (E5) und die Fehlnennungs-Liste auf `SubmitResponse`. `ItemOutcome` bleibt
unangetastet — die Fehlnennungen kommen als eigene Liste, statt `ItemOutcome.ItemIndex` nullable zu machen;
das wäre der zweite, teurere Bruch gewesen.

**Größe M, am oberen Ende** — größer als [B-76](B-76-lueckentext-karte-ohne-luecke.md) (dort: zwei additive
Felder, ein Typ-Override, ein Bauteil in zwei Ansichten), weil hier zwei Spielwege mit je eigener Periode
umgestellt werden, vier Coalescing-Stellen mitziehen (R1) und ein Feld seine Nullbarkeit ändert. Kein `L`:
es wird kein Schema angefasst und keine bezahlten Daten gerettet.

**Der teuerste Posten ist billiger als gedacht.** Der „typ-eigene Haken" aus E1 ist **ein Boolean**:
`GradesAsSet(configJson)`, Vorgabe `false`, für `List` `!Ordered`. Ein neues Bewertungs-Primitiv braucht es
nicht — welche Antwort auf welchen Eintrag passt, steht schon in `ContentItem.AcceptedAnswers` **jedes**
Atoms; gesucht wird nur der erste Treffer unter den noch offenen. Der Haken nimmt `configJson` von Anfang an
als ersten Parameter: bei B-76 musste `Choices` genau deswegen nachträglich umgestellt werden.

### Risiken

**R1 · Vier Stellen coalescen `ItemIndex ?? 0` — eine Fehlnennung würde „Eintrag 0".**
[PositionTestsController.cs:258](../../backend/Pugling.Api/Controllers/Student/PositionTestsController.cs)
(`AttemptDetail`), `:295` (Sammelabgabe), `:310` (der `scorable`-Filter) und `:319` (die Ergebniskarten).
Die dritte ist die gefährliche: eine Zeile ohne Atom rutscht als Ergebniszeile von Eintrag 0 in die
Punktzahl, verfälscht `TotalItems` **und** senkt den Prozentwert doppelt. Alle vier müssen auf
`ItemIndex is not null` umgestellt werden — mechanisch, aber leicht zu übersehen, weil `?? 0` heute korrekt
ist (bisher trägt jede Zeile ein Atom).

**R2 · Eine Fehlnennung trifft kein Atom — welche Kiste bewegt sich?** In der Übungsrunde hängen
`ReviewOutcome.Box`/`DueOn` und die Leitner-Bewegung heute am `PositionItemProgress` des Kartenplatzes
(`PositionPracticeController.cs:268`, `:320`). Nach E2 gibt es bei einer Fehlnennung keinen getroffenen
Eintrag, also auch keine Zeile, die herabgestuft werden könnte. *Empfehlung:* **keine** Bewegung, und die
beiden Felder tragen den unveränderten Stand — eine Falschnennung darf keinen Eintrag herabstufen, den das
Kind nie beansprucht hat. Folge: Leitner bestraft in diesem Typ nur das *falsche Wiederholen*, nicht das
Nichtwissen. **Abgeleitet, nicht entschieden — vor dem Bauen zu bestätigen** (wie B-76/R1, aus dem E6 wurde).

**R3 · Der Vertragsbruch ist im Frontend eine sichtbare Zeile.** `SohnPractice.tsx:112` zeigt
`Lösung: ${outcome.expected}` bedingungslos; mit `null` stünde dort „Lösung: null". Der Zweig muss mit, und
`tsc` findet ihn nur, weil `contract.ts` generiert wird — von Hand getippt wäre es stumm durchgelaufen.

**R4 · Kein Test spielt heute eine List-Position, und ein Test pinnt den Defekt.**
`ExerciseContentProviderTests.List_NutztInstructionAlsPromptUndUebernimmtAlternativen`
([:94-108](../../backend/Pugling.Api.Tests/ExerciseContentProviderTests.cs)) prüft ausdrücklich, dass die
Anweisung der Karten-Prompt ist. Er wird mit umgeschrieben — sonst ist das Tor rot aus dem falschen Grund,
und jemand „reparariert" den Fix.

**R5 · Der Testmodus des Vaters geht denselben Weg.** `ExercisePreviewService.cs:60-64` bewertet ebenfalls
über `AcceptedAnswers`. Wird er vergessen, sagt die Vorschau dem Vater etwas anderes als die Klausur dem
Kind — genau die Klasse von Befund, die der `pugling-reviewer` bei B-76 gefunden hat (dort: rohe
`{{n}}`-Vorlage in der Vorschau).

### Angriffsplan

Backend zuerst; das Frontend hängt an der API.

1. **Typ-Haken**: `IExerciseType.GradesAsSet(string configJson)`, Vorgabe `false` in `ExerciseTypeBase`,
   Override in `ListExerciseType` als `!Ordered`.
2. **Geteilter Treffer-Helfer** — eine Stelle, drei Verbraucher: aus (Atome, schon gutgeschriebene Indizes,
   Antwort, `AnswerGrader`) → getroffener Index oder `null`. Gehört zu den geteilten Statics
   (`PositionPlayService`/`StageMechanics`), nicht in einen Controller; die Bewertungszeile steht heute
   viermal wortgleich da und darf nicht zum fünften Mal kopiert werden.
3. **Klausur**: `Answer` verbucht auf die getroffene Zeile, die Fehlnennung als Zeile ohne Atom;
   `Submit`/`Get` filtern `is not null` (R1); `SubmitResponse` bekommt die Fehlnennungen additiv.
4. **Übungsrunde**: „heute schon genannt" aus `PositionItemProgress.LastReviewedAt`, Punkte und Kiste auf dem
   getroffenen Eintrag. **R2 vorher klären.**
5. **Vertrag** (`Pugling.Contracts`, je mit englischem `/// <summary>`): `AnyOrder` **hinter** `Passage` an
   `PracticeCard`/`TestItem` — sonst brechen die positionalen Aufrufe, die Lehre aus B-76 —,
   `ReviewOutcome.Expected` auf `string?`, Fehlnennungs-Liste an `SubmitResponse`.
6. **Pflichtfeld** (E7): `ValidateConfigAsync`-Override im `ListController`; der Haken läuft auf POST **und**
   PUT ([ExerciseControllerBase.cs:235, :307](../../backend/Pugling.Api/Controllers/Creator/ExerciseControllerBase.cs)).
7. **Vorschau nachziehen** (R5).
8. **Artefakte** neu erzeugen: `docs/openapi/v1.json`, `openapi-examples.generated.json`,
   `docs/api-examples/` (schreiben die `DocsCaptureTests` im Lauf) und `frontend/src/lib/contract.ts` über
   `npm run gen:contract`.
9. **Frontend**: ein Bauteil mit **zwei** Fällen (ungeordnet „einer, der noch nicht dran war" / geordnet
   „Eintrag 8" aus `itemIndex`), eingesetzt in `SohnPractice` **und** `SohnTest`; der Toast-Zweig aus R3;
   die Fehlnennungen im Auswertungsschirm neben den vergessenen Einträgen.

### Testweg

- **Regressionstest, vorher rot** — neue Klasse `ListPlayTests` in `Pugling.Api.Tests` (R4): eine
  ungeordnete Liste, in der Klausur in **umgekehrter** Reihenfolge beantwortet → 100 %. Heute fällt der Test
  am Ist-Stand.
- Weitere Fälle in derselben Klasse: die **Wiederholung** zählt als Fehlnennung (E4); die Punktzahl bleibt
  „belegte Einträge / alle" trotz Fehlnennungs-Zeilen (R1); die **geordnete** Liste wertet weiter
  positionsgenau und ihre Karte trägt `anyOrder = false` (E6); die Übungsrunde einer Leitner-Position
  verweigert einen heute schon genannten Eintrag (E3).
- **Katalog** (`CatalogExerciseTests`, dort liegen die beiden bestehenden List-Fälle): Liste ohne Anweisung
  → `400 validation_error`, auf **POST und PUT** (E7).
- **Umschreiben**, nicht ergänzen: `ExerciseContentProviderTests.cs:94-108` (R4).
- **Frontend** (Vitest): Komponententest auf das neue Bauteil, beide Fälle; dazu der `expected === null`-Zweig
  aus R3.
- **`/smoke-test`** und ein Live-Durchgang gegen `localhost:5280`: die geseedete Position „Die 16
  Bundesländer" als Kind spielen und bestehen. Kein neuer Endpunkt, aber die geseedete Wochenpflicht ändert
  ihr Verhalten (E8) — genau das prüft kein Testlauf.
- **E2E**: `full-flow.spec.ts` (der Vater→Sohn-Durchstich) muss grün bleiben; ein eigener E2E ist nicht
  nötig, der Komponententest deckt die Darstellung ab.

## Verlauf

- **2026-08-02** — angelegt aus der Grill-Runde zu B-76, Entscheidung 1. `prio: P1` in Analogie zu B-76
  gesetzt (geseedet, wirkt heute, Wochenpflicht mit 90 %) — nicht vom Nutzer ausdrücklich bestätigt.
  Der Befund selbst ist am laufenden System belegt; `unverifiziert` steht, weil die Story als Ganzes noch
  nicht ausformuliert ist.
- **2026-08-03** — ausformuliert. Der Kernbefund hält: `ItemsOf` macht aus einer Menge eine Folge, und die
  vorhandene Mengen-Bewertung erreicht kein Kind — sie hängt allein am Creator-Endpunkt und ist dort sogar
  grün getestet. **Ein Satz der Idee war falsch, und zwar in die schlimmere Richtung:** „`CatalogCheck`,
  also kein Abschlusstest" stimmt nicht. `PositionTestsController` hat keine Check-Mode-Schranke, und
  `IsGoalMetAsync` verlangt für alles außer `None` einen bestandenen `TestAttempt` — die Klausur ist damit
  die **einzige** Erfüllung der Pflicht. Im Sohn-Web bietet die geseedete Position folgerichtig nur den
  TEST-Knopf an: 16 zeichengleiche getippte Karten, 90 %, zwei Versuche am Tag; wer alle Länder kennt und
  sie eintippt, trifft im Mittel eine Karte. Solange sie nicht besteht, ist `DutyDone` die ganze Woche
  unerreichbar. Der leere Prompt bestätigt sich (`List` ist der einzige Typ mit übungsweitem Prompt), und
  ein Rückfall auf den Übungstitel ist an `ItemsOf(configJson)` nicht zu haben. Nebenbefund: der Test
  `List_NutztInstructionAlsPromptUndUebernimmtAlternativen` pinnt das defekte Verhalten fest, und kein Test
  spielt heute eine List-Position.
- **2026-08-03** — gegrillt, neun Entscheidungen. Tragend ist **E1/E2**: die Menge wird über den Versuch
  geführt, und verbucht wird auf den **getroffenen Eintrag** statt auf den Kartenplatz — damit nennt der
  Auswertungsschirm die vergessenen Bundesländer, statt ein sinnloses „Erwartet" je Platz zu zeigen. Die
  Runde hat zwei eigene Empfehlungen widerlegt: die Übungsrunde ist **nicht** teuer (die Anti-Farming-
  Tagessperre `alreadyScoredToday` ist der „schon genannt"-Marker, also kein neuer Zustand und keine
  Migration → **E3**), und ein fertiger Server-Prompt ist **falsch** — er wäre deutsche Produktsprache im
  Backend, also trägt die Karte ein Flag und das Frontend die Sprache (**E5**, Muster B-76/E2). Billiger
  wurde der geordnete Fall: die Adressierung liegt mit `itemIndex` längst auf der Karte, er kommt ohne
  neues Feld mit (**E6**) — womit B-76/R2 für diesen Typ beantwortet ist, `GapIndex` wird nicht
  mitbenutzt. Dazu `Instruction` als Pflichtfeld (**E7**), unveränderte 90 % im Seed (**E8**) und der
  kanonische Titel (**E9**), weil „Übungs-Pfad" die kleinere Hälfte benennt. Offen gelassen und
  ausdrücklich der Schätzung überlassen: Name und Ort des typ-eigenen Bewertungs-Hakens neben
  `Check`/`Choices`.
- **2026-08-03** — geschätzt: **M · beides · keine Migration · Vertragsbruch ja**. Der als teuerster Posten
  gehandelte „typ-eigene Haken" ist **ein Boolean** (`GradesAsSet`) — welche Antwort auf welchen Eintrag
  passt, steht schon in den `AcceptedAnswers` jedes Atoms, gesucht wird nur der erste Treffer unter den noch
  offenen. Keine Migration, weil `TestItemResult.ItemIndex` bereits nullable ist; der Bruch sitzt an genau
  einem Feld (`ReviewOutcome.Expected` → `string?`), die drei anderen Vertragsänderungen sind additiv.
  Die Schätzung hat zwei Dinge freigelegt, die im Grillen nicht sichtbar waren: **R1** — vier Stellen
  coalescen `ItemIndex ?? 0`, und eine davon (`:310`) zöge eine Fehlnennung als Ergebniszeile von Eintrag 0
  in die Punktzahl, verfälschte `TotalItems` und senkte den Prozentwert doppelt; und **R2** — eine
  Fehlnennung trifft kein Atom, also gibt es keine Kiste, die herabgestuft werden könnte. Empfohlen ist
  „keine Bewegung" (eine Falschnennung darf keinen Eintrag herabstufen, den das Kind nie beansprucht hat),
  aber das ist abgeleitet und vor dem Bauen zu bestätigen — dieselbe Stelle in der Kette, an der bei B-76
  aus R1 die Entscheidung E6 wurde.
- **2026-08-03** — gebaut. **R2 vom Nutzer entschieden** (keine Kiste bewegt sich) und als **E10**
  nachgetragen. Ablauf wie im Angriffsplan, mit vier Abweichungen und einer Korrektur:
  1. **`TestItem` trug keinen Typ.** Die Klausur-Ansicht kann eine *geordnete* Liste damit nicht von einer
     Vokabelkarte unterscheiden — beide kommen mit `anyOrder = false` an. `Type` ist additiv nachgezogen;
     `PracticeCard` trägt es seit immer, das war eine Asymmetrie, nicht eine Absicht.
  2. **Die Übungsrunde brauchte einen ausdrücklichen Stempel.** In der Mengen-Bewertung *ist* die
     Fortschrittszeile der „heute schon genannt"-Marker — aber `ApplyReview` läuft nur auf Leitner-Positionen.
     Ohne den Stempel hätte eine Listen-Position ohne Leitner dieselbe Antwort auf jeder Karte angenommen.
  3. **Der Regressionstest ist als rot *belegt*, nicht behauptet:** mit abgeschaltetem Haken fallen **5 von 6**
     Fällen in `ListPlayTests`; der sechste ist der Wächter für die geordnete Liste und muss grün bleiben.
     Dabei fiel auf, dass der geseedete Fall zunächst **aus dem falschen Grund** grün war — bei leeren Kisten
     ist die Kartenreihenfolge aufsteigend, und ich hatte die 16 Länder in genau dieser Reihenfolge
     geantwortet. Jetzt werden sie rückwärts genannt, und der Test unterscheidet.
  4. **Der Toast-Zweig aus R3 bleibt ohne Komponententest.** Er sitzt im Antwort-Handler von `SohnPractice`,
     und ein nachgebauter Bildschirm mit gefälschtem `fetch` ist in diesem Frontend ausdrücklich keine
     Testform (`frontend/CLAUDE.md`). Getragen wird er von `tsc` (das Feld ist jetzt nullable) und dem
     Live-Durchgang, der noch offen ist.
  5. **Korrektur an R4:** `ExerciseContentProviderTests.List_NutztInstructionAlsPromptUndUebernimmtAlternativen`
     pinnt den Defekt **nicht** — dass die Anweisung der Prompt ist, bleibt nach **E5** richtig, weil die Regel
     am Flag hängt und nicht am Prompt. Der Test blieb unverändert. Gefallen ist ein anderer: der geordnete
     Katalog-Fall legte eine Liste **ohne Anweisung** an und läuft jetzt in die Pflichtprüfung aus E7 — genau
     ein Fehlschlag in der ganzen Suite, und der beabsichtigte.
  Stand: **661 Backend-Tests grün** (7 neu: 6 in `ListPlayTests`, 1 in `CatalogExerciseTests`),
  **90 Frontend-Tests grün** (4 neu in `ListRule.test.tsx`), `dotnet format Pugling.sln` und `tsc --noEmit`
  sauber, Artefakte neu erzeugt (`docs/openapi/v1.json`, `openapi-examples.generated.json`,
  `docs/api-examples/`, `contract.ts`).
  **Offen für die Abnahme:** `/smoke-test`, der Live-Durchgang gegen die geseedeten 16 Bundesländer,
  `full-flow.spec.ts`, beide Reviewer (`wo: beides`) und der Commit.
- **2026-08-03** — verifiziert am laufenden System. `/smoke-test` grün (13 Checks, eigene Wegwerf-DB), und
  der Durchgang, auf den es ankam: die **geseedete** Position 15 („Die 16 Bundesländer", Wochenpflicht,
  90 %) als Demo-Kind gegen `localhost:5280` gespielt. Die Karte kommt mit `anyOrder: true` und
  `type: "List"`; alle 16 Länder **rückwärts** genannt ergeben `100 % · 16/16 · passed`, und die
  Wochenpflicht verschwindet danach aus `outstanding` (`goalMet: true`) — vorher war sie dauerhaft offen.
  Der zweite Versuch prüfte die Gegenprobe: „Bayern, Bayern, Wien" plus dreizehnmal „Hessen" ergibt
  **2/16 = 12 %**, `wrongMentions` führt die 14 verworfenen Nennungen, und die vergessenen 14 Einträge
  stehen namentlich in der Auswertung. Damit ist **R1 am laufenden System belegt**: die 14 atomlosen
  Zeilen verschieben die Punktzahl nicht.
  `full-flow.spec.ts` grün, 661 Backend- und 90 Frontend-Tests grün, `dotnet format` und `tsc` sauber.
  **Selbst-Review des Diffs, drei eigene Befunde behoben:** der Mengen-Block stand **zweimal** fast
  wortgleich (schrittweise Antwort und Sammelabgabe) und liegt jetzt als `CreditSetAnswer` an einer Stelle —
  zwei Kopien derselben Regel sind, wie die Pfade auseinanderlaufen; eine **Fehlnennung buchte den
  plan-übergreifenden Lernstand gegen den Eintrag der Karte** und behauptete damit genau die falsche
  Zuschreibung, die diese Story beseitigt (jetzt wird sie für **niemanden** gebucht); und der
  „schon genannt"-Stempel las die Uhr ein zweites Mal statt den Zeitpunkt der Wiederholung zu nehmen.
  **Offen bleibt allein** der Lauf der beiden Reviewer-Agenten (`wo: beides`) und der Commit — beides
  braucht einen ausdrücklichen Auftrag, die Sitzungsregel steht dem Selbststart entgegen. Die Stufe bleibt
  darum `in-arbeit`: `abgenommen` verlangt den Reviewer, und ein Selbst-Review ist er nicht.
- **2026-08-03** — `pugling-reviewer` **und** `frontend-reviewer` gelaufen (`wo: beides`), **kein 🔴**, aber
  fünf Befunde behoben. Der einzige echte Fehler war einer, den der Selbst-Review erst halb erwischt hatte:
  der „schon genannt"-Stempel wurde zwei Zeilen später **von einer zweiten Uhr überschrieben**
  (`ApplyReview(…, DateTime.UtcNow)`), und der Kommentar daneben behauptete wortwörtlich das Gegenteil
  („from the same clock … not a second reading"). Live harmlos, weil die Testuhr durchreicht — aber sobald
  ein Test die Uhr einfriert und über eine Tagesgrenze schiebt, fiele der Stempel aus **beiden** Fenstern
  und jede folgende Karte nähme dieselbe Antwort erneut an, also genau der Ausfall, den der Stempel
  verhindert. Dazu loggte die Punkte-Zeile den **Kartenplatz** statt des gutgeschriebenen Eintrags — der eine
  Wert, mit dem die Buchung im Mengen-Modus nichts zu tun hat.
  **Vier der fünf Befunde lagen im Frontend, und drei davon an derselben Naht: der Vater.** E7 hatte im
  Editor kein Gegenstück — das Feld hieß „Anweisung (optional)", und der Vater lief erst nach sechzehn
  getippten Einträgen in die englische Serverantwort; der Testmodus ließ die **geordnete** Liste
  unadressiert (R5 war damit nur zur Hälfte erledigt: 16-mal derselbe fette Satz, genau die
  Ununterscheidbarkeit, die E6 beim Kind beseitigt); und sein Auswertungsschirm wiederholte den geteilten
  Auftrag sechzehnmal — **im Ergebnis stand wieder das Bild, das diese Story als Defekt beschreibt**. Beim
  Kind trug das ❌ eine neue Bedeutung, ohne sie zu sagen: „❌ Berlin" liest sich als „Berlin war falsch",
  während der Server „nicht genannt" meint — und das *ist* nach E2 die Lehre der Übung (jetzt als Wort neben
  dem Zeichen, das Zeichen `aria-hidden`).
  Mitgenommen, weil billig: der Typ-Schlüssel in `ListRule` getippt (`ExerciseTypeKey`, ein „list" hätte die
  Komponente stumm gemacht), die Fixtures ohne `as`-Cast (der schaltete die Prüfung auf überzählige Felder
  ab — dieselbe Fehlerklasse, die bei R3 nur auffiel, *weil* `contract.ts` generiert ist) und nummerierte
  `aria-label` an den Antwortfeldern der Vorschau.
  **Drei Befunde bewusst nicht behoben**, je mit Grund: `CreditSetAnswer` bleibt im Klausur-Controller (der
  geteilte *Treffer*-Helfer liegt wie im Angriffsplan in `PositionPlayService`; die Buchungsregel dorthin zu
  ziehen hieße, ein `TestAttempt` durch den Dienst zu fädeln — ein Umbau ohne dritten Verbraucher);
  `TestItem.Type` bleibt `string?` (der Preis eines additiven Feldes am positionalen Record); und die
  Vorschau führt weiter keine Fehlnennungen (im Code begründet, wäre eine weitere Vertragsänderung samt
  Artefakt-Neubau).
  **Einen Vorschlag habe ich verworfen:** den geteilten Aufgabentext im Auswertungsschirm des Kindes aus
  `anyOrder` zu lesen statt aus der Prompt-Gleichheit. Das wäre die **geordnete** Liste zurückgefallen —
  dort ist `anyOrder` gerade `false`, und ihre sechzehn Zeilen teilen den Auftrag genauso. In der *Vorschau*
  liegt es anders, dort hält der Dialog die Aufgaben noch und weiß den Typ; sie liest ihn jetzt.
  Stand danach: **662 Backend-Tests grün**, **95 Frontend-Tests grün** (1 neu: das Kreuz trägt sein Wort),
  `dotnet format Pugling.sln --verify-no-changes` und `npm run build` (tsc -b) sauber, kein Artefakt-Drift.
- **2026-08-03** — **abgenommen.** Alle sieben Akzeptanzkriterien belegt, beide Reviewer gelaufen, ihre
  Befunde behoben oder mit Grund abgelehnt. Commit `3204640` (Bau samt Review-Nachlauf) und der
  Doku-Nachtrag. Verifikation: 662 Backend- und 95 Frontend-Tests grün, `/smoke-test` grün,
  `full-flow.spec.ts` grün, und die geseedete Wochenpflicht „Die 16 Bundesländer" live gegen
  `localhost:5280` gespielt und bestanden (16 rückwärts genannt → 100 %, Gegenprobe 2/16 mit 14
  Fehlnennungen).
