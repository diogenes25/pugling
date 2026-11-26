---
tags: [typ/story, status/abgenommen, bereich/katalog, rolle/creator, rolle/student]
aliases: [Gleichung lösen Typ, Lösungsmenge, QuadraticEquation]
status: abgenommen
prio: P2
art: Wunsch
groesse: L
wo: beides
migration: nein
vertragsbruch: nein
quelle: B-640
grund: ""
ersetzt_durch: []
wartet_auf: ""
nachgeschaut: "2026-11-26"
---

# B-710 · Übungstyp „Gleichung lösen“: Koeffizienten, Löser, Mengen-Wertung, Vater-Editor

Erster Teil von [B-640](B-640-uebungstyp-gleichung-loesen.md) (geteilt, weil das Ganze XL wäre): ein **senkrechter
Schnitt** durch den neuen Typ. Der Creator trägt `a`, `b`, `c` und eine Toleranz ein, der Server rechnet die
Lösungsmenge und wertet die Antwort als Menge, das Kind tippt sie **im vorhandenen Textfeld** (`2;4`, bei leerer Menge
`keine Lösung`). Getrennte Eingabefelder und den Knopf „keine Lösung“ liefert [B-712](B-712-sohn-eingabe-loesungsmenge.md),
die Auswahlstufe [B-711](B-711-loesungsmenge-auswaehlen.md).

**Reihenfolge:** erst [B-639](B-639-dezimalkomma-im-zahlenvergleich.md) (hart: Zahlen-Parser und Zahlen-Atom), dann
[B-638](B-638-formeln-in-aufgabentext-anzeigen.md) (weich: ohne B-638 zeigt die Karte den Prompt `$x^2 - 6x + 8 = 0$`
als Quelltext, bleibt aber spielbar), dann B-710, danach B-711 und B-712 in beliebiger Folge.

**Warum Typ und Editor in einer Story:** `frontend/e2e/uebungstypen.spec.ts` legt jeden Typ des Server-Manifests über das
Vater-UI an und wird rot, sobald ein Typ im Manifest kein UI hat (`frontend/CLAUDE.md`). Ein Backend-Teil allein
brächte also ein rotes Tor; der Editor ist deshalb hier mit drin.

## User Story

Als **Schüler** möchte ich eine quadratische Gleichung lösen und alle ihre Lösungen (oder „keine Lösung“) angeben, damit
Kapitel IV nicht an einer Ein-Zahl-Aufgabe scheitert.

Als **Creator** möchte ich nur die Gleichung (Koeffizienten) eintragen, damit der Server die Lösungsmenge rechnet und ich
sie nicht von Hand – und möglicherweise falsch – hinschreibe.

## Ist-Stand am Code

Recherchiert am 2026-11-19 gegen `47791819` (Fundstellen aus [B-640](B-640-uebungstyp-gleichung-loesen.md), beim Schätzen
nachgelesen).

- **Heute eine Zahl je Aufgabe:** `ArithmeticProblem(Prompt, Answer: decimal, Tolerance)`
  (`backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs:188`–`:197`); `ArithmeticExerciseType`
  (`backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs:467`–`:500`) legt je Aufgabe ein `ContentItem` mit der Lösung als
  Text ab; Wertung im Katalog-Check über `AnswerChecking.NumericMatch`.
- **Kein Löser im Backend** (Suche nach Diskriminante, `Math.Sqrt`, Bruch: nichts), kein Mengen-Atom: `ContentItem`
  (`backend/Pugling.Api/Services/Shared/ExerciseContentProvider.cs:43`) hat `Answer` und `AcceptedAnswers`, gewertet wird in
  `AnswerGrader.Matches(given, item)` über `item.GradedAnswers()` als normalisierter Text (`AnswerGrader.cs:15`–`:30`).
- **Ein Typ = eine Klasse** (`wiki/08-erweitern.md`): Config in `Pugling.Contracts`, Schlüssel in `ExerciseTypeKeys`,
  Typklasse, Registrierung (`ExerciseTypeRegistry.cs:70`), Controller nach dem Muster `ArithmeticController`
  (`backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs:697`–`:712`), Zeile im Wächter
  `ExerciseItemCountTests`, Editor-Eintrag `AUTHORABLE_TYPES` (`frontend/src/vater/exerciseConfig.tsx:82`) samt Rückweg
  (`CONTENT_EDITABLE`, `:100`).
- **Anlege-Validierung hat ein Vorbild:** `ArithmeticDrillExerciseType.Validate(config)` liefert eine Fehlermeldung, die der
  Controller als `ValidationError` zurückgibt (`ExerciseControllers.cs:739`, `:753`).
- **Eingabe in der Sohn-App:** eine getippte Antwort ist ein String (`Antwortflaeche`, `frontend/src/sohn/Lektion.tsx:151`
  ff.: ein Textfeld, `onWert(string)`); das reicht für `2;4`. Der Hinweis (`Hint`) wird auf der Karte vor der Antwort gezeigt.
- **Buch:** Aufgabe 9 auf S. 120 (`raw/Math/lambacher-schweizer-9-sh-pugling/md/s120-121.md:38`), Test S. 133; `raw/` ist
  gitignored und urheberrechtlich geschützt – in den Katalog gelangen eigene Gleichungen.

## Die echte Lücke

Ein **Typ mit eigener Wertungsregel** (Lösungsmenge als eine Antwort) und ein **Rechner**. Drei Dinge sind größer, als sie
wirken: die Antwort ist eine Menge **in einem String** (Trenner `;`, weil das Komma nach B-639 dezimal ist); die Menge
kann leer sein (ein gültiger Wert, den die Eintrags-Wertung `GradesAsSet` nicht kennt); und die Mengen-Wertung gehört in den
**geteilten** `AnswerGrader`, nicht an den Typ vorbei, sonst unterscheiden sich Üben und Testmodus.

## Offene Punkte

1. **Stufe „Anzahl der Lösungen bestimmen“ – zurückgestellt.** Sie fragt ein anderes Soll als die Menge; der Typ-Vertrag
   kennt das Soll nur je Atom. Frage an den Nutzer: Soll die Anzahl-Frage kommen? Empfehlung: **nein**, eigene Folge-Story,
   wenn der Typ steht und der Rollengang sie vermisst (zweites Atom je Aufgabe oder Vertragserweiterung, beim Schätzen der
   Folge-Story gemessen).
2. **Stufe „Verfahren wählen“ – zurückgestellt.** Das Soll ist nicht aus den Koeffizienten ableitbar (Vieta gegen
   pq-Formel ist ein didaktisches Urteil). Frage an den Nutzer: zählt das „am besten passende“ Verfahren oder jedes
   anwendbare? Empfehlung: **jedes anwendbare** (pq-Formel immer; Ausklammern bei `c = 0`; Wurzelziehen bei `b = 0`; Vieta bei
   `a = 1` und ganzzahligen Lösungen), gebaut mit der Anzahl-Stufe in der Folge-Story.

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-19 (Sonnet), unbestätigt. Die Nummern 1 bis 8 stammen aus
[B-640](B-640-uebungstyp-gleichung-loesen.md) (dort mit anderer Nummer, jeweils genannt), Nummer 9 und 10 sind beim Teilen
neu.*

1. **Ein Atom je Aufgabe; die Antwort ist die Menge, gewertet als „vollständig und ohne Falsches“** (B-640 Nr. 1).
   Begründung: „alle ihre Lösungen (oder ‚keine Lösung‘)“ ist eine Menge, und der leere Fall ist ein gültiger Wert, den eine
   Wertung je Eintrag (`GradesAsSet`, `BuiltInExerciseTypes.cs:559`) nicht kennt. **Verworfen:** ein Atom je Lösung (gäbe
   Teilpunkte, „keine Lösung“ bliebe unmodelliert). **Ausgeschlossen:** ein halber Punkt für eine von zwei Lösungen.
   **Kosten:** wer eine Lösung findet und die zweite vergisst, sieht dasselbe „falsch“ wie wer nichts rechnet; die Auflösung
   zeigt die ganze Menge.
2. **Die Antwort ist der String `x1;x2`, aufsteigend sortiert; der Server kennt nur `;` als Trenner.** Die leere Menge ist
   der feste Text `keine Lösung`; eine leere Eingabe ist falsch (B-640 Nr. 2, ohne den Teil über die Eingabefelder, der nach
   [B-712](B-712-sohn-eingabe-loesungsmenge.md) wanderte). Begründung: [B-639](B-639-dezimalkomma-im-zahlenvergleich.md)
   macht Komma **und** Punkt zu Dezimaltrennern, ein Komma kann also nicht Mengen trennen; „2,4“ ist die Zahl 2,4 und damit
   **keine** Antwort für {2, 4}. **Verworfen:** Freitext mit „und“, Komma oder Leerzeichen als Trenner. **Ausgeschlossen:** dass
   ein über die API geschickter String „2 und 4“ gewertet wird. **Kosten:** das Kind muss `;` tippen, solange B-712 fehlt;
   Entscheidung 9 dämpft das.
3. **Die Toleranz ist eine Konfiguration je Aufgabe (Vorgabe 0,01), gilt je Element der Menge und wird mit der Zahlenregel aus
   B-639 verglichen** (B-640 Nr. 3). Beide Mengen werden aufsteigend sortiert und Element für Element verglichen. Eine
   Konfiguration, in der `2 × Toleranz` mindestens so groß ist wie der Abstand der beiden Lösungen, wird beim Anlegen
   abgelehnt. Begründung: sonst könnte ein Wert beide Lösungen treffen; die Rundung nennt die Aufgabenstellung. **Verworfen:**
   eine globale Rundungsregel. **Ausgeschlossen:** Gleichungen, deren Lösungen enger liegen als die Toleranz. **Kosten:** eine
   Validierung mehr; wer auf eine Stelle rundet, liegt falsch.
4. **Keine Brüche und keine Wurzelschreibweise; die Lösung wird als Dezimalzahl mit Toleranz getippt** (B-640 Nr. 4).
   Begründung: B-639 regelt nur Dezimalzahlen, ein zweiter Parser wäre eine Dublette. **Verworfen:** Bruch-Eingabe jetzt.
   **Ausgeschlossen – weicht vom Wortlaut der Story ab („Brüche … zugelassen“):** „3/4“ und „√2“ sind falsch, auch bei
   richtigem Wert. **Kosten:** wer in der Schule Brüche schreibt, tippt hier dezimal; die Aufgabenstellung sagt es. Eigene Story,
   wenn der Rollengang es vermisst.
5. **Eine Gleichungsform: `a x² + b x + c = 0` mit `a ≠ 0`** (B-640 Nr. 5). Den Prompt erzeugt C# aus den Koeffizienten als
   LaTeX (`$x^2 - 6x + 8 = 0$`, Dezimalkommas als `{,}` wie im Material `s074-075.md:25`); der Creator tippt nie LaTeX.
   **Verworfen:** freie Gleichungs-Eingabe mit Äquivalenzprüfung und Parameter-Aufgaben. **Ausgeschlossen:** Aufgaben, bei
   denen das Kind erst in die Normalform umformen muss. **Kosten:** das Kind sieht die Gleichung stets in Normalform.
6. **B-639 kommt zuerst; B-710 benutzt den Zahlen-Parser und das Zahlen-Kennzeichen am `ContentItem`, erweitert um eine Liste
   von Zahlen** (B-640 Nr. 7). Begründung: ein zweiter Parser wäre eine Dublette, und eine Menge am `AnswerGrader` vorbei wäre
   genau der Unterschied zwischen Üben und Testmodus, den B-639 beseitigt. **Verworfen:** eigener Parser. **Kosten:** B-710
   wartet auf B-639; wird es anders gebaut als geplant, ändert sich der Aufsatzpunkt.
7. **Lösungsfeld-Regel: Die Konfiguration trägt `A`, `B`, `C`, `Tolerance`, nie `Answer`/`Solution`; die Menge steht erst nach
   der Antwort als `Expected`** (B-640 Nr. 8). Ein Lösungsfeld im Konfigurations-DTO liefert `400 unknown_field`. **Verworfen:**
   die Menge in der Konfiguration (doppelte Wahrheit mit dem Rechner). **Kosten:** `ConventionGuardTests` muss den Typ prüfen;
   ob der Wächter ein DTO mit Feldern `A`/`B`/`C` korrekt als lösungsfrei einordnet, wird beim Bau mit der roten Probe belegt.
8. **Die Menge hat verschiedene Werte; eine doppelte Lösung zählt einmal** (B-640 Nr. 10). `x² − 6x + 9 = 0` hat die Menge
   {3}; „3;3“ ist falsch, „3“ richtig; ein leerer String ist nie „keine Lösung“. **Verworfen:** Vielfachheit mitzählen.
   **Kosten:** wer „3;3“ schreibt, um die Doppellösung zu betonen, liegt falsch.
9. **Bis B-712 steht am Typ ein fester Hinweis: „Mehrere Lösungen mit ; trennen, ohne Lösung: keine Lösung“, gesetzt über
   `ContentItem.Hint`.** Begründung: das Kind tippt im vorhandenen Textfeld, und die Trenner-Regel steht sonst nirgends auf
   der Karte. Der Hinweis verrät die Lösung nicht. **Verworfen:** den Prompt um die Regel verlängern (der Prompt ist die
   Formel, ein zweiter Satz darin liefe durch `MathText`) und einen Platzhalter am Textfeld (der Platzhalter kommt von der
   Karte, nicht vom Typ). **Ausgeschlossen:** ein Kind, das den Hinweis überliest und „2,4“ tippt – es gilt als falsch
   (Entscheidung 2). **Kosten:** eine Zeile Text, die B-712 wieder streicht.
10. **Die Teilung folgt der Naht „Backend + Editor + vorhandenes Textfeld“ gegen „Auswahlstufe“ gegen „Mehrfeld-Eingabe“.**
    Begründung: Der senkrechte Schnitt ist ohne die beiden anderen spielbar (Entscheidung 9) und lässt `uebungstypen.spec.ts`
    grün. **Verworfen:** Backend und Editor getrennt (rotes Tor dazwischen). **Kosten:** B-710 ist die größte der drei Stories.

## Akzeptanzkriterien

Je Kriterium die Änderung, die es rot macht.

1. Der Typ speichert `A`, `B`, `C`, `Tolerance`; für `1, −6, 8` liefert `ItemsOf` die Antwort `2;4` und den Prompt
   `$x^2 - 6x + 8 = 0$`. *Rot, wenn:* der Typ die Lösung aus der Konfiguration statt aus dem Rechner nimmt (ein Test schickt
   ein Lösungsfeld und erwartet `400 unknown_field`) oder der Prompt abweicht.
2. Die Antworten `2;4` und `4;2` werten richtig; `2`, `2;4;5`, `2;2` und `2,4` (die Zahl 2,4) falsch – im Katalog-Check **und**
   beim Spielen in einer Position. *Rot, wenn:* die Wertung nur ein Element prüft, zusätzliche Elemente zulässt, das Komma als
   Trenner liest oder ein Spielpfad am `AnswerGrader` vorbei wertet.
3. Eine Gleichung ohne Lösung (`x² + 1 = 0`) wertet `keine Lösung` richtig und `0` sowie die leere Eingabe falsch. *Rot, wenn:*
   eine leere Antwort und „keine Lösung“ gleich gewertet werden.
4. Eine Gleichung mit den Lösungen 2,33 und 5 und Toleranz 0,05 wertet `2.3;5` richtig und `2.2;5` falsch, und eine Gleichung,
   deren **zweite** Lösung an der Toleranz hängt, wertet deren Rundung richtig. *Rot, wenn:* die Toleranz nur für das erste
   Element gilt.
5. Eine Doppellösung (`x² − 6x + 9 = 0`) wertet `3` richtig, `3;3` und `keine Lösung` falsch. *Rot, wenn:* die Menge die
   Vielfachheit zählt.
6. Eine Konfiguration mit `A = 0` oder mit `2 × Toleranz` ≥ Abstand der Lösungen wird beim Anlegen mit `validation_error`
   abgelehnt. *Rot, wenn:* sie angelegt werden kann.
7. Die Karte trägt den Hinweis mit der Trenner-Regel, und er enthält keine Lösung. *Rot, wenn:* `ItemsOf` keinen `Hint`
   setzt oder der Hinweis die Menge enthält.
8. Der Creator-Editor legt den Typ an und liest ihn zurück (`AUTHORABLE_TYPES`, `CONTENT_EDITABLE`). *Rot, wenn:*
   `e2e/uebungstypen.spec.ts` den Typ nicht findet.
9. Die Koeffizienten-Konfiguration und die Lösungsmenge erreichen den Studenten nicht vor der Antwort. *Rot, wenn:* eine Aktion
   `Answer`/`Solution` für `Student` ungegated liefert (`ConventionGuardTests`, beim Bau mit roter Probe belegt, Entscheidung 7).
10. Ein Sohn spielt die Position und tippt `2;4` im vorhandenen Textfeld; die Antwort zählt richtig. *Rot, wenn:* der Typ im
    Spielpfad nicht erreichbar ist (E2E-Durchlauf: Vater legt an, Sohn spielt).

## Schätzung

**Größe: L** — eine DB-Umbau-Etappe wie E6 ist es nicht, aber mehr als der Batch-Pfad des `MediaSelector` (M): ein neuer Typ
mit Config, Löser, Validierung, Controller und Wächter-Zeilen, dazu ein **neues Atom-Format in der geteilten Wertung**
(`AnswerGrader`, trägt jeden Spielpfad) und der Vater-Editor samt Rückweg. Kein Schema, kein brechender Vertrag.

- **`migration: nein`** — nachgesehen: der Typ ist ein String-Schlüssel (`Exercise.Type`) und die Config liegt als JSON in
  `ConfigJson`; es entsteht keine Spalte und keine Tabelle.
- **`vertragsbruch: nein`** — nachgesehen: `Pugling.Contracts` bekommt **additiv** eine Config (`QuadraticEquationConfig`) und
  der Client einen Endpunkt; kein vorhandenes Feld ändert sich. `frontend/src/lib/contract.ts` wird neu erzeugt
  (`gen:contract`), additiv.
- **`wo: beides`** — **Backend zuerst:** Config, Typ, Löser, Mengen-Atom, Controller, Tests; erst danach Editor und E2E.

### Risiken

- **Der geteilte `AnswerGrader` trägt jeden Typ.** Das Mengen-Atom darf Text-Typen nicht berühren (die Regressionstests der
  Text-Typen laufen unverändert grün, wie in B-639); `IsMisspelled` muss für Mengen-Atome `false` liefern (eine falsche Zahl
  ist kein Tippfehler).
- **Gleitkomma:** die Lösungen kommen aus `Math.Sqrt` auf `double`; B-639 hält die Zahlen als `decimal`. Die Abbildung (Rundung
  auf wie viele Stellen, Diskriminante exakt als `decimal` oder `double`) ist beim Bau zu entscheiden und mit Grenzfällen
  (Diskriminante 0, sehr kleine und große Koeffizienten) im Test zu belegen – **nicht gemessen**.
- **Koeffizienten im Prompt:** die Prompt-Erzeugung muss Vorzeichen und die Koeffizienten 1 und −1 stimmig setzen (`x^2 - x`,
  nicht `1x^2`) und Dezimalkommas als `{,}`; ein Test je Sonderfall.
- **Der Editor kennt für diesen Typ keine Zeilenform „Paar“ oder „Text“**, sondern drei Zahlenfelder und die Toleranz; das ist
  neu in `exerciseConfig.tsx` (`emptyRow`, `buildTypeConfig`, `configToEditorState`, `contentProblem`) – die Zahl der
  `switch`-Stellen ist am Code nicht vollständig gezählt.
- **Hängt an zwei ungebauten Stories.** Wird B-639 anders gebaut, ändert sich der Aufsatzpunkt (Entscheidung 6).

### Angriffsplan

1. **Backend:** `QuadraticEquationConfig(A, B, C, Tolerance)` in `Pugling.Contracts`; `ExerciseTypeKeys.QuadraticEquation`;
   Löser (rein, ohne Zustand) mit der Menge als sortierter Liste.
2. **Mengen-Atom:** `ContentItem` um eine Zahlenliste erweitern (auf B-639 aufsetzen) und `AnswerGrader.Matches` für dieses
   Atom die Menge vergleichen lassen; `IsMisspelled` liefert `false`.
3. **Typ:** `QuadraticEquationExerciseType` nach dem Muster `ArithmeticExerciseType` (`ItemsOf` mit Prompt, Antwort, Hinweis;
   `Check` als `CatalogCheck`), `Validate` für `A = 0` und Toleranz gegen Abstand; Registrierung in `ExerciseTypeRegistry.cs`;
   Controller `QuadraticEquationController` mit `check` nach dem Muster `ArithmeticController`.
4. **Wächter:** Zeile in `ExerciseItemCountTests`; `ConventionGuardTests` prüft die Lösungsfeld-Regel am neuen Typ (rote
   Probe: ein Feld `Answer` im Config-DTO).
5. `dotnet build Pugling.sln` und `dotnet format Pugling.sln`.
6. **Frontend:** `gen:contract`; `AUTHORABLE_TYPES` und `CONTENT_EDITABLE` um den Typ; Zeilenform und Felder `A`, `B`, `C`,
   `Toleranz` in `exerciseConfig.tsx` (alle `switch`-Stellen); Anzeigename aus dem Manifest.
7. `tsc -b`, `./node_modules/.bin/vitest run`, `./node_modules/.bin/playwright test uebungstypen.spec.ts`.

### Testweg

- `backend/Pugling.Api.Tests/QuadraticEquationExerciseTypeTests.cs` (neu) – Löser (zwei, eine, keine Lösung, Doppellösung,
  Koeffizienten mit Komma), `ItemsOf`-Prompt und Hinweis, Validierung (`A = 0`, Toleranz gegen Abstand) – AK 1, 3, 5, 6, 7.
- `AnswerGraderTests` – Mengen-Atom: `2;4`, `4;2`, `2`, `2;4;5`, `2;2`, `2,4`, `keine Lösung`, leer, Toleranz je Element
  (zweites Element allein an der Toleranz) – AK 2, 3, 4. Rote Probe je Fall: eine Wertung nur über das erste Element fällt an
  AK 4; mit Komma als Trenner an `2,4`.
- `ExerciseCheckEndpointTests` – Katalog-Check des Typs (AK 2); `PositionPracticeFlowTests` – dieselbe Menge beim Spielen
  (AK 2, 10).
- `ConventionGuardTests` (`Actions_Mit_Loesungsfeld_Sind_Vor_Dem_Studenten_Gegated`) und `ExerciseItemCountTests` (AK 9).
- `frontend/src/vater/exerciseConfig.test.ts` – Hin- und Rückweg des Typs (`buildTypeConfig` und `configToEditorState`).
- E2E: `frontend/e2e/uebungstypen.spec.ts` (AK 8), dazu ein neuer Durchlauf `frontend/e2e/gleichung-loesen.spec.ts`: Vater legt
  `1, −6, 8` an, Sohn tippt `2;4` und an einer zweiten Gleichung `keine Lösung` (AK 10).
- Vor der Abnahme: `pugling-reviewer`, `frontend-reviewer`; Rollengang als Vater (Editor) und als Sohn (Karte mit Hinweis).

## Verlauf

- **2026-11-19** — angelegt als Teil von [B-640](B-640-uebungstyp-gleichung-loesen.md) (geteilt, Vorbereitungslauf, Sonnet):
  Vertikalschnitt Backend + Editor + vorhandenes Textfeld.
- **2026-11-19** — geschaetzt (Vorbereitungslauf, Sonnet): L, beides (Backend zuerst), keine Migration, kein Vertragsbruch
  (additiv); hängt hart an B-639, weich an B-638. `wartet_auf` bleibt.
- **2026-11-19** — Vom Nutzer bestätigt: zwei Stufen (Auswählen, Tippen) mit gleichem Soll; „Anzahl bestimmen“ und „Verfahren wählen“ bleiben zurückgestellt, keine Brüche. Die übrigen Entscheidungen warten weiter, darum bleibt `wartet_auf`.
- **2026-11-19** — Entscheidungen bestätigt; „Anzahl bestimmen“ und „Verfahren wählen“ bleiben zurückgestellt. Stellvertretend auf Anweisung des Nutzers („nimm für alle Fragen deine Empfehlung“): die Empfehlung der Sonnet-Entscheidungen wurde übernommen.
- **2026-11-25** — in-arbeit, Sprint 171 (Nachtlauf M, [Protokoll](../pm-sitzung-2026-11-25-m.md)). Abweichungen vom Plan:
  Wertung über `ContentItem.NumericSet` + `NumericAnswer.MatchesSet` an der Senke `AnswerGrader.Matches` (nicht
  `NumericTarget` erweitert – eine Menge ist ein eigenes Atom); Validierung im Review verschärft (|A| ≥ 0,001,
  Toleranz 0 oder 10⁻⁶…1000, durch Rundung verschmolzene Wurzeln). B-638 ist ungebaut: die Karte zeigt den LaTeX-Prompt
  als Quelltext (Entscheidung 5, bewusst).
- AK 1, Antwort/Prompt aus dem Löser → `backend/Pugling.Api.Tests/QuadraticEquationExerciseTypeTests.cs:28`, `:44` (5 Sonderfälle);
  Lösungsfeld → `backend/Pugling.Api.Tests/QuadraticEquationApiTests.cs:37` (`unknown_field`); Mutante M8 (`1x`) → rot.
- AK 2, Menge in jeder Reihenfolge, Unvollständiges/Zusätzliches/Doppeltes/Komma falsch → `…TypeTests.cs:75` (beide Pfade:
  `Check` und Atom), Katalog-Check `…ApiTests.cs:67`, Vorschau (`4;2`) ebd., Üben `…ApiTests.cs:91`, Test-Abgabe `…ApiTests.cs:174`;
  Mutanten M1 (nicht sortiert), M2 (Zusatz erlaubt), M5 (Grader umgangen) → rot.
- AK 3, leere Menge → `…TypeTests.cs:89`; M4 (leer = keine Lösung) → rot.
- AK 4, Toleranz je Element (zweites Element an der Toleranz) → `…TypeTests.cs:105`; M3 (nur erstes) → rot.
- AK 5, Doppellösung → `…TypeTests.cs:128`, Löser `:56`; M7 (Doppelwurzel spaltet) → rot.
- AK 6, Validierung → `…TypeTests.cs:151` (11 Fälle), `:160`, API POST/PUT `…ApiTests.cs:49`; M14–M20 → rot (M15 erst
  nach dem Fall `:160`, vorher fing die |A|-Grenze `A = 0` mit ab).
- AK 7, Hinweis ohne Lösung → `…TypeTests.cs:167`, Karte `…ApiTests.cs:109`; M13 → rot.
- AK 8, Editor Hin-/Rückweg → `frontend/src/vater/exerciseConfig.test.ts:270`–`:313`; F13–F16 → rot;
  `e2e/uebungstypen.spec.ts` grün (Pulldown deckt das Manifest).
- AK 9, keine Lösung vor der Antwort → `…ApiTests.cs:109` (Karte trägt weder `2;4` noch Reveal); die Config ist
  Creator-gegated, ein Lösungsfeld gibt es nicht (AK 1). Die vorgesehene rote Probe am `ConventionGuardTests` entfällt: ein
  Feld `Answer` im Config-DTO bliebe dort grün, weil `ExerciseResponse<T>` hinter `Roles.Creator` steht – der Wächter ist
  hier nicht der Träger, `unknown_field` + Kartentest sind es.
- AK 10, Sohn spielt → `…ApiTests.cs:91` und `frontend/e2e/gleichung-loesen.spec.ts` (Vater legt im Editor an, Sohn
  löst im Test; seit B-712 über die Felder statt `2;4` im Textfeld – das Textfeld bleibt für Karten ohne `setAnswer`,
  `frontend/src/sohn/Lektion.test.tsx:448`).
- Weitere Mutanten: M6 (`IsNumber` ohne Menge, Tippfehler-Kategorie) → rot; alle Tabellen im Protokoll.
- **2026-11-26** — abgenommen (Sprint 171, Nachtlauf M). Code `a961897b`. Backend 2542 passed (`dotnet test Pugling.sln
  -c Release`), Vitest 1488 passed, `tsc -b` sauber, Playwright 4 passed (`gleichung-loesen`, `uebungstypen`, `lektion`,
  `rechnen-mit-komma`); Mutanten 26/26 (Backend) und 16/16 (Frontend) rot. Review `pugling-reviewer` und `frontend-reviewer`
  eingearbeitet. Rollengang aller drei Rollen an einer Kopie der echten DB (:5241) plus UI-Spec; offen für den Menschen nur
  der Geräte-Check (Zahlentastatur am Handy) und die Formel-Darstellung (B-638).
  Protokoll: [pm-sitzung-2026-11-25-m](../pm-sitzung-2026-11-25-m.md).
- **2026-11-26** — nachgeschaut: Merge-Review der Läufe M/N/O (Commits `61a11f55`, `5446640c`), festgehalten in der Retrospektive von Sprint 192. Kein Defekt; eine Testlücke (403 des Sohns auf den Katalog-Check) schloss `61a11f55`.
