---
tags: [typ/story, status/gegrillt, bereich/gamification, rolle/student]
aliases: [CoinsPerHit übertreibt]
status: gegrillt
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Nachschau Sprint 82 im Nachtlauf 2026-11-17 (docs/pm-sitzung-2026-11-17-nachtlauf.md)"
grund: ""
ersetzt_durch: []
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

## Verlauf

- **2026-11-17** — angelegt aus der Nachschau von Sprint 82 (🟢 des `pugling-reviewer`), `entgangen_bei: [B-597]`.
- **2026-11-19** — ausformuliert (Vorbereitungslauf): Notiz-Zeilen veraltet (Datei 438 Zeilen), Ansage steht bei
  `:436` und `:358`; Lücke breiter (Übersichtszeile, jede gemischte Runde); Ergebnis ist korrekt.
- **2026-11-19** — gegrillt (Vorbereitungslauf, Sonnet, stellvertretend, unbestätigt): fünf Entscheidungen; Satz je Karte als additives Vertragsfeld (`ExtraCard.CoinsPerHit`, `ExtraExercise.MixedRates`), eine gemeinsame Satz-Funktion für Ansage und Abrechnung; keine Münz-Regel geändert.
