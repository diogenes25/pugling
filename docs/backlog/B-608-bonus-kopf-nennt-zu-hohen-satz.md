---
tags: [typ/story, status/abgenommen, bereich/gamification, rolle/student]
aliases: [CoinsPerHit übertreibt]
status: abgenommen
prio: P3
art: Defekt
groesse: M
wo: beides
migration: nein
vertragsbruch: nein
quelle: "Nachschau Sprint 82 im Nachtlauf 2026-11-17 (docs/pm-sitzung-2026-11-17-nachtlauf.md)"
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-11-21"
entgangen_bei: [B-597]
---

# B-608 · Der Kopf eines Bonus-Laufs verspricht mehr Münzen je Treffer, als manche Karten zahlen

Eine Freitext-Runde über eine Liste mit Richtung „beides" nennt als Satz je Treffer den getippten (3). Seit B-597
zahlen die vorwärts gestellten Karten dieser Runde aber nur den Auswahl-Satz 1. Die Abrechnung am Ende stimmt, nur
die Ansage vorab übertreibt – der Sohn merkt so etwas sofort.

## User Story

Als Schüler möchte ich vor einer Karte sehen, was ein Treffer auf *dieser* Karte zahlt, damit die Ansage mit der
Abrechnung übereinstimmt.

## Ist-Stand am Code

1. **Sätze:** Auswahl 1, Tippen 3, Selbsteinschätzung 1
   (`backend/Pugling.Api/Services/Student/ExtraTrainingOptions.cs:16-20`, `appsettings.json:38-40`), abgebildet von
   `CoinsPerHit(ExtraForm)` (`ExtraTrainingService.cs:408-413`).
2. **Abrechnung:** jeder Treffer zahlt nach der Form seiner Karte (`ExtraTrainingService.cs:197-201`,
   `CardRatesAsync` `:392-406`). Die Vorwärtskarte einer „both"-Liste ist `RecognitionOnly`, ihre Decke ist die
   Auswahl (`backend/Pugling.Api/Exercises/VocabularyExerciseType.cs:106-109`; Karten aus `WithDirection`,
   `ExerciseContentProvider.cs:100`) – sie zahlt 1, die Rückkarte 3. Ein Test sichert die Abrechnung zu
   (`ExtraTrainingTests.cs:180-208`, B-597 AK 4).
3. **Ansage im Lauf:** `BuildRun` schreibt den Satz der Run-Form ins Feld (`ExtraTrainingService.cs:435-436`,
   `CoinsPerHit(run.Form)`); die Karten tragen keinen eigenen Satz (`ExtraCard`,
   `backend/Pugling.Contracts/Student/ExtraTrainingDtos.cs:67-71`). Das Frontend setzt daraus den Kopf jeder Karte
   (`frontend/src/sohn/SohnBonus.tsx:299-304`, `run.coinsPerHit`).
4. **Ansage in der Übersicht:** dieselbe Verkürzung für die nächste Runde (`ExtraTrainingService.cs:358`,
   `CoinsPerHit(form)`; Anzeige `SohnBonus.tsx:135`, `:258`).
5. **Ergebnis:** stimmt – `SohnBonus.tsx:272` zeigt die Summe getrennt, sobald `coinsForHits` nicht
   `Treffer × Satz` ist; das Feld erklärt der Vertrag (`ExtraTrainingDtos.cs:84-86`).

Die Fundstellen der Notiz stimmen nicht mehr: die Datei hat 438 Zeilen, die Ansage steht bei `:436`/`:358`, die
Satz-Funktion bei `:392-406`.

## Die echte Lücke

Nicht nur der Kopf einer Run-Karte, sondern auch die Übersichtszeile übertreibt, und nicht nur bei „both": jede Runde,
deren Karten verschiedene Sätze zahlen (zum Beispiel eine Karte, die auf Kästchen zurückfällt, `FormFor` `:385-387`
nennt die Runde nur dann Auswahl, wenn *alle* Karten wählen). Der Vertrag sagt „the full rate"
(`ExtraTrainingDtos.cs:47-48`) und kennt keinen Satz je Karte; die Information liegt serverseitig längst vor.

## Offene Punkte

1. ~~**Was nennt der Kopf bei gemischter Runde?**~~ Entschieden: Entscheidung 1.
2. ~~**Übersichtszeile mitziehen?**~~ Entschieden: Entscheidung 3.
3. ~~**Stimmt das Ergebnis?**~~ Gegenstandslos: es handhabt die Mischung schon (Ist-Stand 5).
4. ~~**`ExtraRunResponse.CoinsPerHit` behalten?**~~ Entschieden: Entscheidung 4.

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-19 (Sonnet), unbestätigt.*

1. **Der Kopf nennt den Satz der gezeigten Karte; der Server schickt ihn je Karte.** `ExtraCard` bekommt ein Feld
   `CoinsPerHit` (nicht optional, ohne Vorgabewert), `BuildRun` füllt es, `SohnBonus.tsx:299-304` liest es statt
   `run.coinsPerHit`. Begründung: die Information liegt serverseitig vor (`CardRatesAsync`, `:392-406`), und der Kopf
   wird ohnehin je Karte gesetzt. Verworfen: (b) Spanne „1–3 🪙", (c) Durchschnitt – beides ist eine Ansage, die keine
   Karte hält –, und (d) das Frontend rechnet den Satz selbst aus Karte und Stufe: ein zweiter Rechenweg für dieselbe
   Zahl, genau die Drift, die B-597 aufgedeckt hat. Kosten (Vertrag): **additives Antwortfeld in `Pugling.Contracts`,
   `vertragsbruch: ja`** – `ExtraCard` hat genau einen Aufrufer (`ExtraTrainingService.cs:431`), das Frontend zieht
   `contract.ts` über `gen:contract` nach (läuft bei `postinstall`/`prebuild`/`predev`), `Pugling.Client` teilt die
   Records. Die Vitest-Fälle in `SohnBonus.test.tsx` bauen Karten als `as unknown as ExtraRunResponse` ohne den
   Satz (`:130`, `:205`, `:234`, `:258`, `:301`): sie müssen das Feld ergänzen, sonst zeigt der Kopf „undefined".
   Eingehende Payloads bleiben unberührt, `unknown_field` betrifft nur Requests. Ausgeschlossen wird: die Ansage
   „3" auf einer Karte, die 1 zahlt, in jeder Runde, nicht nur bei „beides" (User Story: „was ein Treffer auf *dieser*
   Karte zahlt").
2. **Der Satz der Karte kommt aus derselben Funktion wie die Abrechnung.** Den Kern von `CardRatesAsync` ziehen wir
   in eine synchrone Hilfsfunktion (Karte, Übung, Stufe der Runde → Satz); Abrechnung (`:197-201`), `BuildRun` und die
   Übersicht rufen sie. Begründung: `BuildRun` hat die Items schon (`:421`), die Abrechnung liest sie noch einmal
   (`:395`); zwei Wege hießen zwei Wahrheiten. Verworfen: den Satz in `BuildRun` neu herleiten. Kosten: ein
   Umbau im Abrechnungspfad (dort liegt Geld, B-597 AK 4 sichert ihn: `ExtraTrainingTests.cs:180-208` muss
   grün bleiben). Ausgeschlossen: Ansage und Abrechnung laufen später auseinander, wenn jemand eine Karten-Regel
   ändert.
3. **Die Übersichtszeile nennt bei gemischten Sätzen „bis zu"; dafür kommt `MixedRates` an `ExtraExercise`.**
   Der Server berechnet in der Pool-Schleife (`ExtraTrainingService.cs:355-358`, Items und Stufe liegen dort vor) die
   Karten-Sätze, setzt `CoinsPerHit` auf den **höchsten** und `MixedRates` auf „nicht alle gleich". Begründung: bei einer
   einheitlichen Runde ist der Höchste der Satz der Form (Typed: mindestens eine Karte tippt, sonst hieße die Form
   Auswahl, `FormFor` `:385-387`; Choice: alle wählen) – dort ändert sich nichts. Gemischt kann nur die Form Tippen
   sein (nicht gefunden: eine andere Form, die mischt), darum genügt eine neue Zeichenkette `bonus.formTypedMixed`
   („Tippen · bis zu {{coins}} 🪙 je Treffer", de und en). Verworfen: die Zeile unverändert lassen (der Widerspruch
   bliebe eine Ebene höher stehen), und ein Satz je Karte in der Übersicht (zu lang). Kosten: ein **zweites**
   additives Vertragsfeld (`ExtraExercise.MixedRates`, ein Aufrufer `:357`), zwei Texte, die gepflegt werden. Die
   Ansage „bis zu 3" verspricht weiterhin mehr, als die Runde im Schnitt zahlt – nur nicht mehr als ein Treffer
   überhaupt zahlen kann; für „was zahlt *diese* Karte" ist der Kopf (Entscheidung 1) zuständig, nicht die Übersicht.
   Das ist die ehrliche Grenze der User Story an dieser Stelle.
4. **`ExtraRunResponse.CoinsPerHit` bleibt.** Begründung: Bestand im Vertrag; heute liest nur der Hinweis der Runde es (`:301-304`, das
   Ergebnis hat ein eigenes Feld, `ExtraRunResult.CoinsPerHit`); ein Entfernen wäre ein weiterer Vertragsbruch ohne
   Nutzen für den Defekt. Der Hinweistext (`hintSelf`, `hintPaid`, `hintPaidReduced`,
   `:301-304`) liest künftig ebenfalls den Satz der Karte, eine Stelle statt vier Weichen im JSX. Kosten: das Feld
   bleibt ohne Leser im Frontend, bis jemand es entfernt (eigene Aufräum-Story, wenn gewünscht).
5. **Wirkung auf Münzen- oder Malus-Regeln: keine.** Geändert wird nur die Ansage, nicht die Abrechnung (`:197-201`
   bleibt wertgleich, nur auf eine gemeinsame Hilfsfunktion gestützt). Darum fällt die Story nicht unter die
   Rückstellung für Münz-Ökonomie. Prüfbar durch AK 2.

## Akzeptanzkriterien

Final; je Kriterium die Änderung, die es rot macht.

1. In einer getippten Runde über eine „both"-Liste trägt die Vorwärtskarte `coinsPerHit = 1`, die Rückkarte `3`
   (`ExtraTrainingTests`, neben `:180-208`). Rot macht es: `BuildRun` füllt das Feld mit `CoinsPerHit(run.Form)` statt
   je Karte (beide 3).
2. Der Satz einer Karte ist der Wert der Abrechnung: ein Test spielt die „both"-Runde fehlerfrei und vergleicht
   `coinsForHits` mit der Summe der Karten-`coinsPerHit` der Antwort von `/runs`. Rot macht es: `BuildRun` oder die
   Abrechnung rechnet über einen zweiten Weg (zum Beispiel `CardRatesAsync` ohne `CardCeiling`).
3. Eine Runde, deren Karten alle tippen (`back-to-front`), nennt 3 je Karte; eine Auswahl-Runde 1; eine
   Karteikarten-Runde 1. Rot macht es: eine Korrektur, die Sätze pauschal nach unten zieht oder nur Typed behandelt
   (der Auswahl- und der Karteikarten-Fall fallen dann um).
4. Der Kopf im Frontend zeigt den Satz der gezeigten Karte: Vitest mit zwei Karten desselben Laufs (1 und 3), Erwartung
   der Wechsel nach dem Weiter; Fixtures tragen `coinsPerHit` je Karte. Rot macht es: der Kopf liest
   `run.coinsPerHit` (heute so).
5. Die Übersicht einer „both"-Liste sagt „bis zu 3" (`MixedRates = true`), eine einheitliche Runde („back-to-front",
   Auswahl, Karteikarte, Rechnen) den Satz unverändert. Rot macht es: `MixedRates` fehlt oder wird immer gesetzt, oder
   `CoinsPerHit(form)` bleibt am Pool-Eintrag unverändert (`:358`, die Übersicht einer „both"-Liste im
   Tippschritt nennt dann weiter ein nacktes „3" ohne „bis zu").
6. Festschreibung: der Ergebnisbildschirm bleibt unverändert (Ist-Stand 5); die vorhandenen Fälle bleiben grün.

## Schätzung

*Geschätzt im Vorbereitungslauf 2026-11-19 (Sonnet), am Code nachgesehen.*

**Größe: M** — wie der Batch-Pfad im `MediaSelector` (B-03): ein Umbau an einer geteilten Stelle (die Satz-Funktion
neben der Abrechnung), dazu zwei Vertragsfelder und ihre Nachzieher in Frontend und Texten. Nicht L: kein Schema, keine
neue Regel.

- **`wo: beides`** — Backend zuerst (`ExtraTrainingService`, `ExtraTrainingDtos.cs`), dann `SohnBonus.tsx`,
  `de.ts`/`en.ts` und die Vitest-Fixtures.
- **`migration: nein`** — nichts am Schema; die Sätze kommen aus `ExtraTrainingOptions` und werden pro Aufruf berechnet.
- **`vertragsbruch: nein`** — nachgesehen: beide Felder sind **additiv** in Antworten (`ExtraCard.CoinsPerHit`,
  `ExtraExercise.MixedRates`); nach README ist ein additives Feld kein Bruch. Der Text in Entscheidung 1 („`vertragsbruch:
  ja`") meint den *Nachzieh-Aufwand* und gilt für die Einstufung nicht. Zu beachten bleibt trotzdem:
  `ExtraCard` und `ExtraExercise` sind positionale Records, und die Felder müssen **vor** den Parametern mit
  Vorgabewert stehen (`ExtraCard`: vor `DictationSpokenAnswer`); beide haben genau einen Aufrufer
  (`ExtraTrainingService.cs:357`, `:431`), `Pugling.Client` und `Pugling.Agent.Creator` bauen sie nicht (nicht gefunden).
  Das eingecheckte `docs/openapi/v1.json` ändert sich (`ContractDocumentTests` schreibt es bei jedem Lauf; CI wird rot, wenn die
  Änderung nicht im selben Commit steht). `frontend/src/lib/contract.ts` ist nicht eingecheckt, sondern wird bei
  `postinstall`/`prebuild` erzeugt.

### Risiken

1. **Hier liegt Geld:** Die Satz-Funktion ersetzt den Kern von `CardRatesAsync` (`ExtraTrainingService.cs:392-406`),
   den die Abrechnung nutzt (`:197-201`). Ein Versehen verschiebt Münzen. `ExtraTrainingTests.Getippte_Bonusrunde_Fragt_Die_Vorwaertskarte_Bei_Beiden_Als_Auswahl`
   (`:185`, `coinsEarned` 4 × 1 + 4 × 3) muss unverändert grün bleiben und ist das Netz.
2. **`MixedRates` vergleicht Sätze, nicht Formen:** in der Pool-Schleife (`:355-358`) liegen `items` und `stage` vor,
   aber `CardRatesAsync` lädt die Items selbst noch einmal (`:395`). Die Hilfsfunktion nimmt die geladenen Items, sonst
   kostet die Übersicht je Übung eine zweite Abfrage.
3. **Fixtures in `SohnBonus.test.tsx`:** Karten werden als `as unknown as ExtraRunResponse` ohne `coinsPerHit` gebaut
   (Zeilen `:130`, `:205`, `:234`, `:258`, `:301`); ohne Ergänzung zeigt der Kopf „undefined". Der Cast verdeckt den
   Fehler beim Übersetzen – vor `tsc` ist er also nicht zu finden, nur im Test selbst.
4. **Texte in zwei Sprachen:** `bonus.formTypedMixed` fehlt in `de.ts` (`:199`) und `en.ts` (`:186`); ein
   Paritäts-Wächter für die Schlüssel ist nicht nachgesehen.
5. **Fremde Arbeit im Baum:** `SohnBonus.tsx`, `SohnBonus.test.tsx`, `de.ts`, `en.ts` sind sauber; die ungecommittete
   LearningDad-Arbeit liegt in `sohn/Lektion.tsx`, `SohnApp.tsx`, `SohnPractice.tsx`, `SohnTest.tsx`. Berührt wird
   nur, wer dort in der Bonusrunde etwas ändert – Zeilennummern in `sohn/*` können driften.

### Angriffsplan

1. Backend: synchrone Satz-Funktion (Karte, Übung, Stufe der Runde → Satz) aus `CardRatesAsync` herauslösen; Abrechnung
   ruft sie; Tests für AK 2 und AK 6 zuerst festschreiben (grün).
2. `ExtraCard.CoinsPerHit` und `ExtraExercise.MixedRates` im Vertrag; `BuildRun` und die Pool-Schleife füllen sie
   (AK 1, 3, 5, rot vor der Änderung).
3. Frontend: `SohnBonus.tsx` liest den Satz der Karte (`:301-304`) und nennt in der Übersicht „bis zu" bei `mixedRates`
   (`:135`, `:258`); Texte `bonus.formTypedMixed` (de, en); Fixtures nachziehen.
4. `dotnet build Pugling.sln`, `dotnet format Pugling.sln`, danach der Lauf, der `docs/openapi/v1.json` neu schreibt; im
   selben Commit.

### Testweg

- **AK 1, 3 (Regressionstest, vorher rot):** `backend/Pugling.Api.Tests/ExtraTrainingTests.cs`, neben `:185`; ein Fall je
  Form (`both`, `back-to-front`, Auswahl-Runde, Karteikarte). Rot macht es: `BuildRun` füllt jede Karte mit
  `CoinsPerHit(run.Form)`.
- **AK 2:** derselbe Klassenrahmen, `coinsForHits` der „both"-Runde gegen die Summe der Karten-`coinsPerHit` aus `/runs`.
  Rot macht es: ein zweiter Rechenweg für Ansage oder Abrechnung.
- **AK 5:** `ExtraTrainingTests.cs` über die Übersicht (`MixedRates`), Fälle „both" und eine einheitliche Runde.
- **AK 4:** `frontend/src/sohn/SohnBonus.test.tsx`, Fall mit zwei Karten (1 und 3) und Wechsel nach dem Weiter. Rot macht
  es: der Kopf liest `run.coinsPerHit`.
- **AK 6:** vorhandene Ergebnis-Fälle (`:258-306` in `SohnBonus.test.tsx`) bleiben grün.
- Vor der Abnahme: `pugling-reviewer` und `frontend-reviewer`; Rollengang als Sohn an einer „beides"-Liste;
  `e2e/bonus.spec.ts` ist die Stelle, falls ein Durchstich gewünscht ist.

## Verlauf

- **2026-11-17** — angelegt aus der Nachschau von Sprint 82 (🟢 des `pugling-reviewer`), `entgangen_bei: [B-597]`.
- **2026-11-19** — ausformuliert (Vorbereitungslauf): Notiz-Zeilen veraltet (Datei 438 Zeilen), Ansage steht bei
  `:436` und `:358`; Lücke breiter (Übersichtszeile, jede gemischte Runde); Ergebnis ist korrekt.
- **2026-11-19** — gegrillt (Vorbereitungslauf, Sonnet, stellvertretend, unbestätigt): fünf Entscheidungen; Satz je Karte als additives Vertragsfeld (`ExtraCard.CoinsPerHit`, `ExtraExercise.MixedRates`), eine gemeinsame Satz-Funktion für Ansage und Abrechnung; keine Münz-Regel geändert.
- **2026-11-19** — geschaetzt (Vorbereitungslauf, Sonnet): M, beides (Backend zuerst), keine Migration, kein Vertragsbruch (beide Felder additiv; `docs/openapi/v1.json` ändert sich); Risiko: die Satz-Funktion berührt den Abrechnungspfad.
- **2026-11-21** — in-arbeit (Nachtlauf, Sprint 108). Rote Probe vor dem Fix: 9 von 33 Fällen in `ExtraTrainingTests` rot, alle mit `KeyNotFoundException` (Feld `coinsPerHit` bzw. `mixedRates` fehlt) – das belegt nur das fehlende Feld, nicht die Regel; die Last tragen die Mutanten unten.
- **2026-11-21** — Fälle → Tests: AK 1 („both“, Vorwärtskarte 1, Rückkarte 3) → `ExtraTrainingTests.Jede_Bonuskarte_Nennt_Was_Ein_Treffer_Auf_Ihr_Zahlt`, M1 → rot; AK 2 (Ansage = Abrechnung) → derselbe Fall (`coinsForHits` = Summe der Karten) und die Lückentext-Theorie (`CardRates(run).Sum()`), M1 und M2 → rot; AK 3 (einheitlich: Tippen 3, Auswahl 1, Karteikarte 1) → `Eine_Einheitliche_Runde_Nennt_Den_Satz_Ihrer_Form_Auf_Jeder_Karte`, M5 (Tippen) und M6 (Karteikarte) → rot – Auswahl und Karteikarte zahlen beide 1, eine Verwechslung der beiden bleibt ungemessen; AK 4 (Kopf je Karte) → Vitest `SohnBonus.test.tsx` „wechselt den genannten Satz mit der Karte“, F1 → rot; AK 5 (Übersicht „bis zu“) → `Die_Uebersicht_Nennt_Bis_Zu_Wo_Die_Karten_Verschieden_Zahlen` (M3, M4 → rot) und Vitest „nennt bei gemischten Karten …“ (F2, F3 → rot); AK 6 (Ergebnis unverändert) → vorhandene Ergebnis-Fälle in `SohnBonus.test.tsx` grün, Ergebnis-Code nicht im Diff.
- **2026-11-21** — Mutanten nach der letzten Änderung (`mutant-probe.py`, Backend Debug, Filter `ExtraTrainingTests`; Frontend Vitest `SohnBonus.test.tsx`):

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | M1 Karte trägt den Satz der Runde | `ExtraTrainingService.cs:446` | red | red |
  | M2 Abrechnung nach dem Satz der Runde | `ExtraTrainingService.cs:200` | red | red |
  | M3 `MixedRates` nie | `ExtraTrainingService.cs:361` | red | red |
  | M4 `MixedRates` immer | `ExtraTrainingService.cs:361` | red | red |
  | M5 jede geprüfte Karte als Auswahl | `ExtraTrainingService.cs:417` | red | red |
  | M6 Karteikarte zum Tipp-Satz | `ExtraTrainingService.cs:412` | red | red |
  | F1 Hinweis liest `run.coinsPerHit` | `SohnBonus.tsx:329` | red | red |
  | F2 „bis zu“ nie | `SohnBonus.tsx:43` | red | red |
  | F3 „bis zu“ nur beim Tippen (Review-Fund) | `SohnBonus.tsx:43` | red | red |

  E2E-Probe E1 (M1 gegen `frontend/e2e/bonus-satz-je-karte.spec.ts`, Ausweichports) → rot. `rates.Max()` statt `CoinsPerHit(form)` am Vorrat ist in keinem gemessenen Fall verschieden (nur bei der gemischten Auswahlrunde) – ungemessen.
- **2026-11-21** — Review (`pugling-reviewer`, `frontend-reviewer`): **ein 🟡, von beiden gefunden** – Entscheidung 3 nahm an, nur die Form Tippen könne mischen; eine Vokabel-Auswahlrunde mischt aber, wenn eine Karte keinen Ablenker hat (`FormFor` prüft im Kletterzweig nur die Anzahl, `DistractorPool` liefert dann `null`, die Karte wird getippt und zahlt 3). Behoben: „bis zu“ hängt nur noch an `mixedRates`, je Form ein Text (`formChoiceMixed`, `formSelfAssessMixed`), Vitest-Fall „Auswahl gemischt“ (F3). **Zähler 1 von 5.** 🟢 ohne Zählung: veralteter Kommentar (`CardRatesAsync` → `CardRate`), Doc-Satz zu `RunPercent` umgebrochen und eindeutig. 🟢 Überblickskosten (gestufte Liste je Karte neu): ein Cache je Stufe wurde gebaut und wieder entfernt – seine Mutante (Schlüssel falsch) blieb grün, weil die Ablenker in beiden gestuften Listen dieselben sind; ungeprüfter Code in einem Geldpfad wiegt schwerer als Millisekunden. 🟢 außerhalb des Diffs: der Aufwärm-Hinweis verspricht „ab der Auswahl gibt es mehr“ → [B-785](B-785-aufwaermhinweis-verspricht-mehr-ab-der-auswahl.md). Volle Suite (Release) 1999/1999, Vitest 1304/1304.
- **2026-11-21** — abgenommen (Sprint 108). Rollengang Sohn als E2E (`frontend/e2e/bonus-satz-je-karte.spec.ts`, Server frisch von Playwright auf 5201/5174 nach der letzten Änderung): Lückentext auf „Buchstaben ordnen“ mit einer Lücke, die auf Kästchen zurückfällt – die Übersicht sagt „Tippen · bis zu 3 🪙 je Treffer“, Karte 1 „bringt 1 🪙“, Karte 2 „bringt 3 🪙“. Vater und Creator: Regressionszeugen (`bonus`, `bonus-deckel`, `bonus-deckel-voll`, `full-flow` grün; keine Vater-/Creator-Fläche im Diff). Entscheidung 3 im Bau erweitert („bis zu“ in jeder Form, siehe Review) – keine neue Produktentscheidung, dieselbe Regel auf einen Fall, den die Annahme übersah. Commit `26f2863e`.
- **2026-11-21** — nachgeschaut (Nachtlauf, Retro Sprint 109), kein Befund. Geprüft: (1) `coinsPerHit` hat im Frontend außerhalb der Tests nur Leser in `SohnBonus.tsx` – Übersicht (`:145`) und „nächste Runde“ (`:282`) mit `mixedRates`, der Hinweis (`:325-329`) mit dem Satz der Karte, das Ergebnis (`:273`, `:296-297`) mit dem Satz der Runde wie vor B-608 (AK 6); (2) `CoinsPerHit(` in `ExtraTrainingService.cs`: `:195` Rückfall der Abrechnung, `:412`/`:417` in `CardRate`, `:449` das Rundenfeld ohne Leser im Frontend (Entscheidung 4) – kein Weg kündigt einer Karte einen anderen Satz an, als die Abrechnung bucht.
