---
tags: [typ/story, status/gegrillt]
aliases: []
status: gegrillt
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Chat mit dem Vater, 2026-11-17 (Mathe in die App; Material raw/Math/lambacher-schweizer-9-sh-pugling/md)"
grund: ""
ersetzt_durch: []
wartet_auf: Bestätigung der Sonnet-Entscheidungen
---

# B-640 · Übungstyp „Gleichung lösen“ mit Lösungsmenge

Der Typ `Arithmetic` kennt je Aufgabe genau eine Dezimalzahl mit Toleranz. Eine quadratische Gleichung hat zwei, eine
oder keine Lösung – das Kernstück von Kapitel IV (Abschnitte IV.2–IV.4) ist damit nicht abfragbar. Gedacht: ein neuer
Typ mit festen Aufgaben, der die **Koeffizienten** speichert statt eines Lösungstexts; der Server rechnet die
Lösungsmenge aus (der Lösungsanhang des Buchs ist nicht fotografiert, und nach B-460 entscheidet C#, was eine Antwort
ist). Antwort als Menge: Reihenfolge egal, Brüche und gerundete Werte zugelassen, eigener Knopf „keine Lösung“.
Stufenleiter: Anzahl der Lösungen bestimmen (Diskriminante) → Verfahren wählen (Ausklammern, Wurzelziehen, Vieta,
pq-Formel; Buch S. 120 Aufgabe 9) → Lösungsmenge auswählen → Lösungsmenge tippen. Der Test von S. 133 dient als
Vorlage für die Klausur. Braucht B-638 (Formeln) und B-639 (Komma). Bewusst nicht enthalten: freie Term-Eingabe mit
Äquivalenzprüfung, Graphen, Begründungsaufgaben.

## User Story

Als **Schüler** möchte ich eine quadratische Gleichung lösen und alle ihre Lösungen (oder „keine Lösung“) angeben, damit
Kapitel IV nicht an einer Ein-Zahl-Aufgabe scheitert.

Als **Creator** möchte ich nur die Gleichung (Koeffizienten) eintragen, damit der Server die Lösungsmenge rechnet und ich
sie nicht von Hand – und möglicherweise falsch – hinschreibe.

## Ist-Stand am Code

Recherchiert am 2026-11-19 gegen `47791819`.

- **Heute: eine Zahl je Aufgabe.** `ArithmeticProblem(Prompt, Answer: decimal, Tolerance)`
  (`backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs:188`–`:197`); `ArithmeticExerciseType`
  (`backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs:467`–`:500`, Manifest `:472`–`:474`: `CatalogCheck`, kein
  `PlayRoute`/`Method`) legt je Aufgabe ein Atom mit der Lösung als Text ab (`:477`–`:485`) und prüft per
  `AnswerChecking.NumericMatch` (`:689`; die Story nannte `:669`, die Zeile ist inzwischen gewandert).
- **Kein Löser im Backend.** Eine Suche nach Diskriminante, `Math.Sqrt`, Bruch und quadratischen Gleichungen in
  `backend/` (ohne Tests) findet nichts – ein Rechner der Lösungsmenge ist **nicht gefunden**. Der vorhandene
  `ArithmeticProblemGenerator` erzeugt Grundrechenaufgaben, keine Gleichungen (`ArithmeticDrillExerciseType`,
  `BuiltInExerciseTypes.cs:506`–`:543`).
- **Mengen-Wertung hat ein Vorbild – für Text.** `GradesAsSet` (`BuiltInExerciseTypes.cs:559`, `ListExerciseType`):
  ohne `Ordered` darf jeder Eintrag in beliebiger Reihenfolge genannt werden; eine Nennung ohne passenden offenen
  Eintrag ist eine „falsche Nennung“ (`TestItemResult.ItemIndex == null`, `backend/Pugling.Api/Models/StudyPlanEntities.cs:214`–`:223`).
  Es ist eine Wertung **je Eintrag**, nicht „die Lösungsmenge als eine Antwort“.
- **Der Typ-Vertrag trägt Stufen mit anderer Antwortform, aber nach dem Gelesenen nur mit demselben Soll.**
  `IExerciseType` hat `StageOptions`, `Choices(configJson, items, item, stage)` (`IExerciseType.cs:134`),
  `IsTypedStage`, `DefaultStage`; `MatchingExerciseType.Choices` liefert die Optionen der Auswahlstufe
  (`BuiltInExerciseTypes.cs:449`–`:450`: `MatchStage.Distractors`; `MatchStage.Direct` bleibt Freitext) – beide Stufen
  fragen **dasselbe** Soll (die Gegenseite des Paares) in zwei Formen. Eine Stufe, die ein *anderes* Soll fragt (etwa „wie
  viele Lösungen“), ist im Vertrag **nicht gefunden**: das Soll kommt aus `ContentItem.Answer` je Atom. `Arithmetic` nutzt
  die Basisvorgaben (`ExerciseTypeBase.cs:37`–`:43`, `:89`: Stufe `SelfAssess`, keine `StageOptions`); auf welcher
  Stufe `Arithmetic` heute in einer Position spielt, ist **nicht nachgesehen**.
- **Eine neue Klasse, aber mehr als eine Zeile.** Checkliste: Config, `ExerciseTypeKeys`, Typklasse, Registrierung,
  Controller, `ExerciseItemCountTests`-Zeile, Frontend-Editor in `frontend/src/vater/exerciseConfig.tsx`
  (`AUTHORABLE_TYPES`, Zeile `:82`) samt Rückweg (`wiki/08-erweitern.md`, Checkliste; `frontend/e2e/uebungstypen.spec.ts`
  wird rot, sobald ein Server-Typ kein UI hat – `frontend/CLAUDE.md`).
- **Eingabe in der Sohn-App:** eine getippte Antwort ist **ein String** (`GivenAnswer`); ein Mehrfach-Eingabefeld oder ein Knopf
  „keine Lösung“ ist in `Antwortflaeche` (`frontend/src/sohn/Lektion.tsx:151`) **nicht gefunden**.
- **Buch.** `raw/Math/lambacher-schweizer-9-sh-pugling/md/s120-121.md:38` – Aufgabe 9 steht auf **S. 120** (die Story nannte
  S. 121), neun Gleichungen mit Verfahren A/W/L/F; der Test auf S. 133 hat zwei Runden mit Lösungen S. 268/269
  (`s132-133.md:25`–`:26`), die Lösungen des Anhangs sind nicht erfasst. Die Aufgaben selbst liegen im urheberrechtlich
  geschützten `raw/` (gitignored) – in den Katalog gelangen eigene Gleichungen, nicht die des Buchs.

## Die echte Lücke

Fehlt ein **Typ mit eigener Wertungsregel** (Lösungsmenge als eine Antwort) und ein **Rechner**. Nicht fehlt: die
Stufen-/Auswahl-Technik des Typ-Vertrags für Stufen mit gleichem Soll. Drei Dinge sind größer als sie wirken:

1. **Die Antwort ist eine Menge in einem String.** „x = 2 und 4“, „4; 2“, „2,4“ muss der Server in eine Menge zerlegen – das
   Trennzeichen kollidiert mit dem Dezimalkomma ([B-639](B-639-dezimalkomma-im-zahlenvergleich.md), Entscheidung 1).
2. **Brüche und Wurzeln.** B-639 deckt nur Dezimalzahlen; „3/4“ oder „√2“ als Lösung ist weder dort noch hier
   geregelt. Die Story erlaubt „Brüche und gerundete Werte“ – Rundungsregel und Bruchschreibweise sind offen.
3. **Gestufte Fragen haben verschiedene Antwortformen** (Zahl, Auswahl des Verfahrens, Menge, „keine Lösung“) in **einem**
   Typ – und zwei der vier Stufen der Story fragen ein anderes Soll als die Menge (Anzahl, Verfahren).

## Offene Punkte

1. ~~**Eine Antwort oder ein Atom je Lösung?**~~ → Entscheidung 1.
2. ~~**Format der Mengen-Antwort.**~~ → Entscheidung 2.
3. ~~**Rundung und Toleranz.**~~ → Entscheidung 3.
4. ~~**Brüche.**~~ → Entscheidung 4.
5. ~~**Welche Gleichungsformen?**~~ → Entscheidung 5.
6. ~~**Stufenleiter in einem Typ?**~~ → Entscheidung 6 (teilweise; die Stufen „Anzahl“ und „Verfahren“ stehen unten).
7. ~~**Reihenfolge zu B-638/B-639.**~~ → Entscheidung 7.
8. ~~**Lösungsfeld-Regel.**~~ → Entscheidung 8.
9. ~~**Klausurvorlage S. 133.**~~ → Entscheidung 9.
10. ~~**Doppelte Lösung und leere Antwort.**~~ (beim Grillen ergänzt) → Entscheidung 10.
11. **Stufe „Anzahl der Lösungen bestimmen“ – zurückgestellt.** Grund: Sie fragt ein anderes Soll als die Menge (zwei /
    eine / keine Lösung); der Typ-Vertrag kennt das Soll nur je Atom (siehe Ist-Stand). Frage an den Nutzer: Soll die
    Anzahl-Frage mit dem Typ kommen? Empfehlung: **nein**, eine eigene Folge-Story, wenn der Typ steht und der Rollengang
    sie vermisst – entweder als zweites Atom je Aufgabe oder über eine Vertragserweiterung, die beim Schätzen dieser
    Folge-Story gemessen wird.
12. **Stufe „Verfahren wählen“ – zurückgestellt.** Grund: Das Soll ist nicht aus den Koeffizienten ableitbar. Ausklammern
    (`c = 0`) und Wurzelziehen (`b = 0`) lassen sich erkennen, Vieta gegen pq-Formel ist ein didaktisches Urteil, und die
    pq-Formel passt immer. Frage an den Nutzer (Didaktik, nicht Rollen/Münzen/Malus): Zählt das „am besten passende“
    Verfahren oder jedes anwendbare? Empfehlung: **jedes anwendbare** zählt (pq-Formel immer; Ausklammern bei `c = 0`;
    Wurzelziehen bei `b = 0`; Vieta bei `a = 1` und ganzzahligen Lösungen); gebaut mit der Anzahl-Stufe in der
    Folge-Story, nicht hier.

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-19 (Sonnet), unbestätigt.*

1. **Ein Atom je Aufgabe; die Antwort ist die Menge, gewertet als „vollständig und ohne Falsches“.** Begründung: Die
   User Story sagt „alle ihre Lösungen (oder ‚keine Lösung‘)“ – das ist eine Menge, und der leere Fall ist ein
   gültiger Wert, den eine Wertung je Eintrag (wie `GradesAsSet`, `BuiltInExerciseTypes.cs:559`) nicht kennt.
   **Verworfen:** ein Atom je Lösung (gäbe Teilpunkte, „keine Lösung“ bliebe unmodelliert). **Ausgeschlossen:** dass
   ein Kind mit einer von zwei Lösungen einen halben Punkt bekommt. **Kosten:** ein Kind, das eine Lösung findet und die
   zweite vergisst, sieht dasselbe „falsch“ wie eines, das gar nichts rechnet; die Auflösung zeigt die ganze Menge.
2. **Eingabe als getrennte Felder (Anzahl wählbar) plus Knopf „keine Lösung“; die Antwort wird zum String `x1;x2`
   gebunden, aufsteigend sortiert. Der Server kennt nur `;` als Trenner.** Die leere Menge ist der feste Text
   `keine Lösung`; eine leere Eingabe ist falsch. Begründung: [B-639](B-639-dezimalkomma-im-zahlenvergleich.md),
   Entscheidung 1, macht Komma **und** Punkt zu Dezimaltrennern – ein Komma kann also nicht Mengen trennen. **Folge für
   den Entwurf der Akzeptanzkriterien:** „2,4“ ist die Zahl 2,4 und damit **keine** Antwort für die Menge {2, 4}; der
   alte Entwurf (AK 2) hat beides gleichgesetzt und wird berichtigt. **Verworfen:** Freitext mit „und“, Komma oder
   Leerzeichen als Trenner (kollidiert mit B-639). **Ausgeschlossen:** dass ein über die API geschickter String
   „2 und 4“ gewertet wird. **Kosten:** ein neues Mehrfeld-Eingabeelement in `Antwortflaeche` (nicht gefunden, muss
   gebaut werden), `wo: beides`.
3. **Die Toleranz ist eine Konfiguration je Aufgabe (Vorgabe 0,01) und gilt je Element der Menge; verglichen wird mit der
   Zahlenregel aus B-639.** Die Mengen werden beide aufsteigend sortiert und Element für Element verglichen. Eine
   Konfiguration, in der `2 × Toleranz` mindestens so groß ist wie der Abstand der beiden Lösungen, wird beim Anlegen
   abgelehnt (sonst könnte ein Wert beide Lösungen treffen). Begründung: Die Vorgabe 0,01 lässt „2.33“ für 2,3333 zu und
   ist von den gerundeten Werten des Buchs unabhängig – dessen Lösungsanhang ist nicht erfasst (Ist-Stand), die
   Empfehlung „Vorgabe aus den Werten des Buchs“ ließ sich also nicht belegen. Die Aufgabenstellung nennt die verlangte
   Rundung („auf zwei Nachkommastellen“). **Verworfen:** eine globale Rundungsregel (zwei Aufgaben mit verschiedener
   Genauigkeit gäbe es dann nicht). **Ausgeschlossen:** Gleichungen, deren Lösungen enger beieinanderliegen als die
   Toleranz – sie sind nicht anlegbar. **Kosten:** eine Validierung mehr; ein Kind, das auf eine Stelle rundet, liegt
   falsch.
4. **Keine Brüche und keine Wurzelschreibweise in dieser Stufe; die Lösung wird als Dezimalzahl mit Toleranz getippt.**
   Begründung: B-639 regelt nur Dezimalzahlen, und ein zweiter Parser in dieser Story wäre eine Dublette (Entscheidung 7).
   **Verworfen:** Bruch-Eingabe jetzt (eigene Parser-Regeln: gemischte Zahlen, Kürzen). **Ausgeschlossen – und hier
   weicht die Entscheidung vom Wortlaut der Story ab („Brüche … zugelassen“):** „3/4“ und „√2“ werden als Eingabe nicht
   verstanden und sind falsch, auch wenn der Wert stimmt. **Kosten:** Ein Kind, das in der Schule Brüche schreibt, muss
   hier dezimal tippen; die Aufgabenstellung sagt das. Eigene Story, wenn der Rollengang es vermisst.
5. **Eine Gleichungsform: `a x² + b x + c = 0` mit `a ≠ 0`; alle vier Verfahren aus Aufgabe 9 sind durch dieselben
   Koeffizienten ausgedrückt.** Die Aufgabenstellung (`Prompt`) erzeugt C# aus den Koeffizienten als LaTeX, z. B.
   `$x^2 - 6x + 8 = 0$`, Dezimalkommas als `{,}` (wie das Material, `s074-075.md:25`, und passend zu
   [B-638](B-638-formeln-in-aufgabentext-anzeigen.md)); der Creator tippt nie LaTeX. Die Lösungen rechnet C# aus der
   Diskriminante. **Verworfen:** eine freie Gleichungs-Eingabe mit Umformung (Äquivalenzprüfung – im Text der Story
   ausgeschlossen) und Parameter-Aufgaben (Aufgabe 12). **Ausgeschlossen:** Aufgaben, bei denen das Kind erst in die
   Normalform umformen muss (`x² = 9x`); der Schritt fehlt. **Kosten:** das Kind sieht die Gleichung stets in
   Normalform; Umformen übt der Typ nicht.
6. **Der Typ bietet zwei Stufen mit demselben Soll: „Lösungsmenge auswählen“ und „Lösungsmenge tippen“.** Die
   Auswahlstufe liefert die richtige Menge und bis zu drei falsche, die C# aus den Koeffizienten bildet (Vorzeichen
   vertauscht, nur eine Lösung, Lösungen der mit `p` statt `p/2` gerechneten Formel; paarweise verschieden und nie gleich
   der richtigen), deterministisch je Aufgabe. Begründung: genau das Muster der Zuordnung (`Distractors` gegen `Direct`,
   `BuiltInExerciseTypes.cs:449`–`:450`). **Verworfen:** die vier Stufen der Story in einem Zug (zwei davon fragen ein
   anderes Soll, siehe offene Punkte 11 und 12). **Ausgeschlossen – gegen die User Story gerechnet:** der Schüler
   *bestimmt* die Anzahl der Lösungen oder das Verfahren nicht als eigene Frage; seine Aufgabe in der Story („alle
   Lösungen angeben“) wird erfüllt, die Stufenleiter der Idee nur teilweise. **Kosten:** der Typ hat im Anschluss ein
   zweites Mal Arbeit (Folge-Story), und die Wahl der falschen Mengen ist eine Fachentscheidung, die der Rollengang
   prüfen muss.
7. **B-639 und B-638 kommen zuerst; B-640 benutzt den Zahlen-Parser von B-639 und das dort eingeführte Zahlen-Kennzeichen
   am `ContentItem`, erweitert um eine Liste von Zahlen.** Begründung: B-639 (geschätzt) legt den Parser, `AnswerGrader`
   und ein Zahlen-Kennzeichen an (dort Angriffsplan 1–3); ein zweiter Parser hier wäre eine Dublette, und eine Menge
   über einen zweiten Weg am `AnswerGrader` vorbei wäre genau der Unterschied zwischen Üben und Testmodus, den B-639
   beseitigt. **Verworfen:** B-640 mit eigenem Parser vorziehen. **Ausgeschlossen:** B-640 vor B-639 zu bauen. **Kosten:**
   B-640 wartet auf zwei andere Stories; wird B-639 anders gebaut als geplant, ändert sich der Aufsatzpunkt.
8. **Lösungsfeld-Regel: Die Konfiguration trägt `A`, `B`, `C`, `Tolerance`, nie `Answer`/`Solution`; die Menge steht erst
   nach der Antwort als `Expected`.** Begründung: Wer die Koeffizienten kennt, sieht die Aufgabe, die er ohnehin sieht;
   die Lösung ist erst das Ergebnis des Rechners. Das Konfigurations-DTO nimmt kein Lösungsfeld an – ein solches Feld
   liefert `400 unknown_field` (Root-`CLAUDE.md`, „Unbekannte Felder werden abgelehnt“). **Verworfen:** die Menge in
   der Konfiguration ablegen (doppelte Wahrheit mit dem Rechner). **Kosten:** `ConventionGuardTests` muss den Typ prüfen;
   ob der Wächter ein DTO mit Feldern `A`/`B`/`C` korrekt als lösungsfrei einordnet, wird beim Bau mit der roten Probe
   belegt, nicht vermutet.
9. **Die Klausurvorlage von S. 133 gehört nicht zu dieser Story.** Es gibt keine Strategie des KI-Creators für Mathe
   (`Program.cs:114`–`:117`: `Vocabulary`, `Cloze`, `Translation`, `Grammar`); eine Übungsklausur aus dem neuen Typ ist
   eigene Arbeit. **Ausgeschlossen:** dass der Vater die Klausur des Buchs 1:1 übernimmt – die Aufgaben liegen im
   urheberrechtlich geschützten `raw/`, in den Katalog gelangen eigene Gleichungen. **Kosten:** Der Test des Buchs bleibt
   Vorlage im Kopf des Vaters, kein Datensatz.
10. **Die Menge hat verschiedene Werte; eine doppelte Lösung zählt einmal.** `x² − 6x + 9 = 0` hat die Menge {3};
    „3;3“ ist falsch (eine Nennung zu viel), „3“ ist richtig. Ein leerer String ist nie „keine Lösung“. Begründung:
    „vollständig und ohne Falsches“ (Entscheidung 1) braucht eine eindeutige Menge. **Verworfen:** Vielfachheit mitzählen.
    **Kosten:** ein Kind, das „3;3“ schreibt, weil es die Doppellösung betonen will, liegt falsch.

## Akzeptanzkriterien

Je Kriterium die Änderung, die es rot macht.

1. Der Typ speichert `A`, `B`, `C`, `Tolerance`; für `1, −6, 8` liefert `ItemsOf` die Antwort `2;4` und den Prompt
   `$x^2 - 6x + 8 = 0$`. *Rot, wenn:* der Typ die Lösung aus der Konfiguration statt aus dem Rechner nimmt (ein Test
   schickt ein Lösungsfeld und erwartet `400 unknown_field`) oder der Prompt abweicht.
2. Die Antworten `2;4` und `4;2` werten richtig; `2`, `2;4;5`, `2;2` und `2,4` (die Zahl 2,4) falsch. *Rot, wenn:* die
   Wertung nur ein Element prüft, zusätzliche Elemente zulässt oder das Komma als Trenner liest.
3. Eine Gleichung ohne Lösung (`x² + 1 = 0`) wertet `keine Lösung` richtig und `0` sowie die leere Eingabe falsch. *Rot,
   wenn:* eine leere Antwort und „keine Lösung“ gleich gewertet werden.
4. Eine Gleichung mit der Lösung 2,33 und Toleranz 0,05 wertet `2.3` richtig und `2.2` falsch – je Element der Menge (ein
   Test mit zwei Lösungen, bei dem nur das **zweite** Element an der Toleranz hängt). *Rot, wenn:* die Toleranz nur
   für das erste Element gilt.
5. Eine Doppellösung (`x² − 6x + 9 = 0`) wertet `3` richtig, `3;3` und `keine Lösung` falsch. *Rot, wenn:* die Menge die
   Vielfachheit zählt.
6. Eine Konfiguration mit `A = 0` oder mit `2 × Toleranz` ≥ Abstand der Lösungen wird beim Anlegen abgelehnt. *Rot, wenn:*
   sie angelegt werden kann.
7. Die Stufe „Lösungsmenge auswählen“ liefert die richtige Menge und bis zu drei paarweise verschiedene falsche, nie
   gleich der richtigen, gleich bei jedem Aufruf. *Rot, wenn:* die Optionen die richtige Menge doppeln oder zwischen zwei
   Aufrufen wechseln.
8. Der Creator-Editor legt den Typ an und liest ihn zurück (`AUTHORABLE_TYPES`, Hin- und Rückweg). *Rot, wenn:*
   `e2e/uebungstypen.spec.ts` den Typ nicht findet.
9. Die Koeffizienten-Konfiguration und die Lösungsmenge erreichen den Studenten nicht vor der Antwort. *Rot, wenn:* eine
   Aktion `Answer`/`Solution` für `Student` ungegated liefert (`ConventionGuardTests`, beim Bau mit roter Probe
   belegt, siehe Entscheidung 8).
10. Die Sohn-Eingabe bindet zwei Felder zu `x1;x2` aufsteigend und den Knopf zu `keine Lösung`. *Rot, wenn:* ein
    Komponententest bei Eingabe `4` und `2` etwas anderes als `2;4` sendet.

## Verlauf

- **2026-11-17** — angelegt (Quelle: Konzept „Mathe in die App" aus dem Chat, Lambacher Schweizer 9, Kapitel IV).
- **2026-11-19** — ausformuliert (Vorbereitungslauf, Sonnet): kein Löser im Backend, Typ-Vertrag trägt die Stufenleiter,
  Mengen-Wertung nur als Eintrags-Wertung (`GradesAsSet`); Antwort als Menge kollidiert mit dem Dezimalkomma (B-639);
  Buchverweis berichtigt (Aufgabe 9 steht auf S. 120).
- **2026-11-19** — gegrillt (Vorbereitungslauf, Sonnet, stellvertretend, unbestätigt): ein Atom je Aufgabe, Menge als
  `x1;x2` mit `;`, Toleranz je Element, keine Brüche, Normalform `a x² + b x + c`, zwei Stufen mit gleichem Soll;
  „Anzahl“ und „Verfahren“ zurückgestellt (anderes Soll je Stufe ist im Typ-Vertrag nicht gefunden); AK 2 des Entwurfs
  berichtigt („2,4“ ist nach B-639 die Zahl 2,4). Abgestimmt mit [B-638](B-638-formeln-in-aufgabentext-anzeigen.md)
  und [B-639](B-639-dezimalkomma-im-zahlenvergleich.md).
