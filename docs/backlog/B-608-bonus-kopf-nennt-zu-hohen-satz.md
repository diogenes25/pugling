---
tags: [typ/story, status/ausformuliert, bereich/gamification, rolle/student]
aliases: [CoinsPerHit übertreibt]
status: ausformuliert
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

1. **Was nennt der Kopf bei gemischter Runde?** (a) Der Satz der gerade gezeigten Karte, neu am `ExtraCard`;
   (b) eine Spanne „1–3 🪙"; (c) der Durchschnitt. **Empfehlung (a):** der Kopf wird ohnehin je Karte gesetzt
   (`SohnBonus.tsx:299`), die Sätze je Karte berechnet `CardRatesAsync` schon, und (b)/(c) bleiben eine Ansage, die
   keine Karte hält. Kosten: neues Vertragsfeld (additiv) – das ist die Vertragsbruch-Frage der Schätzung.
2. **Übersichtszeile mitziehen?** **Empfehlung ja, als „bis zu {{coins}}"**, wenn die nächste Runde Sätze mischt –
   dieselbe Ursache, derselbe Helfer, und sonst bleibt der Widerspruch eine Ebene höher stehen.
3. ~~**Stimmt das Ergebnis?**~~ Gegenstandslos: es handhabt die Mischung schon (Ist-Stand 5).
4. **`ExtraRunResponse.CoinsPerHit` behalten?** **Empfehlung ja** (Bestand, Selbsteinschätzungs-Hinweis `:301`);
   nur die Kopf-Ansage liest künftig den Satz der Karte.

## Akzeptanzkriterien (Entwurf)

1. In einer getippten Runde über eine „both"-Liste nennt die Vorwärtskarte 1, die Rückkarte 3. Rot macht es: der
   Kopf liest weiter `run.coinsPerHit` (heute 3 für beide).
2. Der genannte Satz einer Karte ist derselbe Wert, den `CardRatesAsync` bei der Abrechnung nimmt – ein Test spielt
   die Runde fehlerfrei und vergleicht `coinsForHits` mit der Summe der Karten-Sätze bei 100 %. Rot macht es: die
   Ansage rechnet über einen zweiten Weg (die Drift, die B-597 gerade erst aufgedeckt hat).
3. Eine Runde, deren Karten alle tippen (`back-to-front`), nennt weiter 3 je Karte. Rot macht es: eine Korrektur, die
   die Sätze nach unten zieht statt je Karte zu lesen.
4. Die Übersichtszeile einer Liste mit gemischten Karten sagt „bis zu 3", eine einheitliche Runde den Satz unverändert.
   Rot macht es: `CoinsPerHit(form)` bleibt am Pool-Eintrag (`:358`) unverändert.
5. Der Ergebnisbildschirm bleibt unverändert (Ist-Stand 5) – die vorhandenen Fälle bleiben grün.

## Verlauf

- **2026-11-17** — angelegt aus der Nachschau von Sprint 82 (🟢 des `pugling-reviewer`), `entgangen_bei: [B-597]`.
- **2026-11-19** — ausformuliert (Vorbereitungslauf): Notiz-Zeilen veraltet (Datei 438 Zeilen), Ansage steht bei
  `:436` und `:358`; Lücke breiter (Übersichtszeile, jede gemischte Runde); Ergebnis ist korrekt.
