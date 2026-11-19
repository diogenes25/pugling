---
tags: [typ/story, status/ausformuliert]
aliases: []
status: ausformuliert
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Chat mit dem Vater, 2026-11-17 (Mathe in die App; Material raw/Math/lambacher-schweizer-9-sh-pugling/md)"
grund: ""
ersetzt_durch: []
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
  `AnswerChecking.NumericMatch` (`:669`).
- **Kein Löser im Backend.** Eine Suche nach Diskriminante, `Math.Sqrt`, Bruch und quadratischen Gleichungen in
  `backend/` (ohne Tests) findet nichts – ein Rechner der Lösungsmenge ist **nicht gefunden**. Der vorhandene
  `ArithmeticProblemGenerator` erzeugt Grundrechenaufgaben, keine Gleichungen (`ArithmeticDrillExerciseType`,
  `BuiltInExerciseTypes.cs:506`–`:543`).
- **Mengen-Wertung hat ein Vorbild – für Text.** `GradesAsSet` (`BuiltInExerciseTypes.cs:559`, `ListExerciseType`):
  ohne `Ordered` darf jeder Eintrag in beliebiger Reihenfolge genannt werden; eine Nennung ohne passenden offenen
  Eintrag ist eine „falsche Nennung“ (`TestItemResult.ItemIndex == null`, `backend/Pugling.Api/Models/StudyPlanEntities.cs:214`–`:223`).
  Es ist eine Wertung **je Eintrag**, nicht „die Lösungsmenge als eine Antwort“.
- **Der Typ-Vertrag trägt die gewünschte Stufenleiter.** `IExerciseType` hat `StageOptions`, `Choices(...)`, `IsTypedStage`,
  `DefaultStage` (`backend/Pugling.Api/Exercises/IExerciseType.cs`, Abschnitt `IExerciseType`); `MatchingExerciseType.Choices`
  liefert Auswahl-Optionen je Stufe (`BuiltInExerciseTypes.cs:449`–`:450`). `Arithmetic` nutzt die Basisvorgaben
  (`ExerciseTypeBase.cs:37`–`:43`, `:89`: Stufe `SelfAssess`, keine `StageOptions`). Auf welcher Stufe `Arithmetic` heute in
  einer Position spielt, ist **nicht nachgesehen**.
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
Stufen-/Auswahl-Technik des Typ-Vertrags. Drei Dinge sind größer als sie wirken:

1. **Die Antwort ist eine Menge in einem String.** „x = 2 und 4“, „4; 2“, „2,4“ muss der Server in eine Menge zerlegen – das
   Trennzeichen kollidiert mit dem Dezimalkomma ([B-639](B-639-dezimalkomma-im-zahlenvergleich.md), Entscheidung 1).
2. **Brüche und Wurzeln.** B-639 deckt nur Dezimalzahlen; „3/4“ oder „√2“ als Lösung ist weder dort noch hier
   geregelt. Die Story erlaubt „Brüche und gerundete Werte“ – Rundungsregel und Bruchschreibweise sind offen.
3. **Gestufte Fragen haben verschiedene Antwortformen** (Zahl, Auswahl des Verfahrens, Menge, „keine Lösung“) in **einem** Typ.

## Offene Punkte

Alle ohne Entscheidung; Empfehlung dabei.

1. **Eine Antwort oder ein Atom je Lösung?** Empfehlung: **ein Atom je Aufgabe**, Antwort als Menge; die Wertung ist
   „vollständig und ohne Falsches“. Die Wertung je Eintrag (wie `GradesAsSet`) gäbe Teilpunkte und ließe „keine Lösung“
   unmodelliert.
2. **Format der Mengen-Antwort.** Empfehlung: Eingabe als **getrennte Felder** (Anzahl wählbar) plus Knopf „keine Lösung“,
   serverseitig zu einem String `x1;x2` gebunden – nie Freitext mit Komma-Trennung (Kollision mit B-639).
3. **Rundung und Toleranz.** Empfehlung: Toleranz je Aufgabe als Konfiguration (wie `Arithmetic.Tolerance`), Vorgabe aus den
   gerundeten Lösungen des Buchs (zwei Nachkommastellen); die Toleranz wirkt je Element der Menge.
4. **Brüche.** Empfehlung: **nicht in Stufe 1** – Dezimalzahl mit Toleranz genügt; Bruch-Eingabe eigene Story, wenn der
   Rollengang sie vermisst.
5. **Welche Gleichungsformen?** Empfehlung: `x² + px + q = 0` und die vier Verfahren aus Aufgabe 9 (Ausklammern,
   Wurzelziehen, Vieta, pq-Formel) als Koeffizienten (`a`, `b`, `c`); Parameter-Aufgaben (Aufgabe 12) nicht.
6. **Stufenleiter in einem Typ?** Empfehlung: **ja**, als `StageOptions`/`Choices` der Typklasse (Anzahl bestimmen → Verfahren
   wählen → Menge auswählen → Menge tippen), jede Stufe mit eigener Antwortform; das Verfahren wählt die Auswahlstufe
   (Optionen aus dem Server), nicht die Rechenregel.
7. **Reihenfolge zu B-638/B-639.** Empfehlung: B-638 (Anzeige) und B-639 (Zahlenvergleich) **zuerst**; B-640 baut auf
   dem Zahlen-Parser von B-639 auf, ein zweiter Parser in dieser Story wäre ein Dublette.
8. **Lösungsfeld-Regel.** Der Typ gibt die Lösungsmenge als Antwort heraus (Vorschau, Auflösung): sie muss vor dem Studenten
   gegated bleiben (Root-`CLAUDE.md`, Lösungsfeld-Regel; `ConventionGuardTests`). Empfehlung: im Konfigurations-DTO
   Koeffizienten, nie `Answer`/`Solution`; die Menge wird erst nach der Antwort als `Expected` gezeigt.
9. **Klausurvorlage S. 133.** Empfehlung: kein Teil dieser Story; eine Übungsklausur aus dem neuen Typ ist Sache des
   KI-Creators (Strategie für `Vocabulary`/`Cloze`/`Translation`/`Grammar` vorhanden, keine für Mathe –
   `backend/Pugling.Agent.Creator/Program.cs:114`–`:117`).

## Akzeptanzkriterien (Entwurf)

1. Eine Aufgabe `x² − 6x + 8 = 0` speichert Koeffizienten, der Server rechnet die Menge {2, 4}. *Rot, wenn:* der Typ die
   Lösung als Text aus dem Editor übernimmt statt sie zu rechnen.
2. Die Antwort „4 und 2“, „2;4“ und „2,4“ (Feldwerte 2 und 4) wertet richtig; „2“ allein und „2; 4; 5“ falsch. *Rot, wenn:*
   die Wertung nur ein Element prüft oder zusätzliche Elemente zulässt.
3. Eine Gleichung ohne Lösung (`x² + 1 = 0`) wertet den Knopf „keine Lösung“ richtig und jede Zahl falsch. *Rot, wenn:*
   eine leere Antwort und „keine Lösung“ gleich gewertet werden.
4. Eine gerundete Lösung (2,33 bei Toleranz 0,05) wertet „2,3“ richtig, „2,2“ falsch – je Element der Menge. *Rot, wenn:* die
   Toleranz nicht je Element gilt.
5. Die Stufe „Anzahl bestimmen“ fragt zwei/eine/keine Lösung und wertet gegen den Diskriminanten-Fall der Koeffizienten.
   *Rot, wenn:* die Optionen aus der Zahl der Lösungen nicht serverseitig kommen.
6. Der Creator-Editor legt den Typ an und liest ihn zurück (`AUTHORABLE_TYPES`, Hin- und Rückweg). *Rot, wenn:*
   `e2e/uebungstypen.spec.ts` den Typ nicht findet.
7. Die Koeffizienten und die Lösungsmenge erreichen den Studenten nie vor der Antwort. *Rot, wenn:* eine Aktion `Answer`/
   `Solution` für `Student` ungegated liefert (`ConventionGuardTests`).

## Verlauf

- **2026-11-17** — angelegt (Quelle: Konzept „Mathe in die App" aus dem Chat, Lambacher Schweizer 9, Kapitel IV).
- **2026-11-19** — ausformuliert (Vorbereitungslauf, Sonnet): kein Löser im Backend, Typ-Vertrag trägt die Stufenleiter,
  Mengen-Wertung nur als Eintrags-Wertung (`GradesAsSet`); Antwort als Menge kollidiert mit dem Dezimalkomma (B-639);
  Buchverweis berichtigt (Aufgabe 9 steht auf S. 120).
