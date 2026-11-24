---
tags: [typ/story, status/ausformuliert, rolle/student]
aliases: [Buchstaben-Türme, Turmspiel Buchstaben-Modus]
status: ausformuliert
prio: P3
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Nutzer im Dialog 2026-11-24; baut auf B-822"
grund: ""
ersetzt_durch: []
---

# B-952 · Vokabel-Türme: Buchstaben-Modus – Monster lassen Buchstaben fallen, der Turm wird buchstabiert

Zweite Spielart der [Vokabel-Türme](B-822-vokabel-tuerme-natives-spiel.md). Der Turm zeigt die Bedeutung
und darunter leere **Buchstabenfelder** für die Übersetzung – die Übersetzung selbst zeigt er nicht.
Besiegte Monster lassen keine ganzen Wörter fallen, sondern **einzelne Buchstaben**. Die Spielfigur sammelt
sie, läuft zum Turm und setzt jeden Buchstaben in das passende Feld; gesetzte Buchstaben lassen sich im
Turm vertauschen. Ist das Wort vollständig und richtig, ist der Turm gelöst. Das ist das Spiel-Gegenstück
zur Teststufe „Buchstabenkästchen": die Länge der Antwort ist bekannt, jeder Buchstabe wird einzeln gesetzt.

## User Story

Als **Student** möchte ich im Turmspiel die Übersetzung aus gesammelten Buchstaben selbst zusammensetzen,
damit ich das Wort nicht nur wiedererkenne, sondern schreiben kann – so, wie es der Vokabeltest in der
Schule verlangt.

Als **Supervisor** möchte ich, dass ein buchstabiertes Wort im Lernstand mehr wiegt als ein bloß
zugeordnetes, damit das Spiel das Schreiben übt und nicht nur das Raten.

## Ist-Stand am Code

**Das Spiel kennt heute nur Zuordnen (Wiedererkennen).** Der Run liefert je Turm eine Bedeutung
(`TowerTarget(TowerId, Prompt)`) und je Token ein ganzes Wort (`TowerToken(TokenId, Word)`)
([TowerGameDtos.cs:34](../../backend/Pugling.Contracts/Student/TowerGameDtos.cs:34),
[:37](../../backend/Pugling.Contracts/Student/TowerGameDtos.cs:37)). `AssignAsync` vergleicht Turm und
Token über die Item-Id bzw. gleiches Wort/gleiche Bedeutung
([TowerGameService.cs:136](../../backend/Pugling.Api/Services/Student/TowerGameService.cs:136),
Vergleich [:156](../../backend/Pugling.Api/Services/Student/TowerGameService.cs:156)) – es gibt **keinen
Weg, eine geschriebene Antwort einzureichen**.

**Die App trägt genau ein Token.** `_carried` ist ein einzelnes `TowerToken`
([Level.cs:64](../../game/Scripts/Level.cs:64)); ein neues aufzuheben legt das alte ab
([:285](../../game/Scripts/Level.cs:285)). Zugewiesen wird per Ja/Nein-Frage
([:277](../../game/Scripts/Level.cs:277)). Ein Inventar für mehrere Teile und eine Turm-Ansicht mit
Feldern gibt es nicht.

**Die Maske der Buchstabenfelder gibt es serverseitig schon.** `StageMechanics.LetterBoxPattern` macht aus
der Lösung eine Maske: Unterstrich je Buchstabe/Ziffer, Leerzeichen, Bindestrich und Apostroph bleiben
stehen ([StageMechanics.cs:96](../../backend/Pugling.Api/Services/Shared/StageMechanics.cs:96), B-66). Die
Teststufe `LetterBoxes` liefert darüber Länge und Muster aus
([StudyPlanEntities.cs:44](../../backend/Pugling.Api/Models/StudyPlanEntities.cs:44),
[VocabularyExerciseType.cs:158](../../backend/Pugling.Api/Exercises/VocabularyExerciseType.cs:158),
`AnswerLength` in [PracticeDtos.cs:127](../../backend/Pugling.Contracts/Student/PracticeDtos.cs:127)).

**Buchstaben gemischt herausgeben ist ein bestehendes, als unbedenklich entschiedenes Muster.** Die Stufe
`LetterScramble` (B-05) gibt die Buchstaben der Lösung gemischt aus
([StudyPlanEntities.cs:55](../../backend/Pugling.Api/Models/StudyPlanEntities.cs:55)); spielbar sind
Wörter von 2–12 Buchstaben, auch mehrere durch Leerzeichen getrennt
(`IsScramblable`, [StageMechanics.cs:119](../../backend/Pugling.Api/Services/Shared/StageMechanics.cs:119)).
Der Buchstaben-Modus verrät also nicht mehr als diese Stufe – solange der Server **nur das ganze Wort**
bewertet (siehe Offener Punkt 1).

**Lernstand und Dämpfung tragen unverändert.** Jede Bewertung schreibt ein `ItemReviewEvent` mit Quelle
`Game`; die Box steigt höchstens einmal je Wort und Tag
([TowerGameService.cs:163](../../backend/Pugling.Api/Services/Student/TowerGameService.cs:163)).

**Balance ist auf „ein Gang = ein Treffer" gestellt.** Start 10 Schuss, 8 Schuss je gelöstem Turm, Monster
2 Leben ([Level.cs:27](../../game/Scripts/Level.cs:27)). Ein Wort aus acht Buchstaben braucht ein
Vielfaches der Zeit einer Zuordnung – diese Zahlen tragen den Modus nicht.

Nicht nachgesehen: wie die Antwortprüfung der Übungstypen Akzente und Groß-/Kleinschreibung normalisiert
und ob `TowerGameService` sie wiederverwenden kann (heute vergleicht er nur Ids und rohe Strings).

## Die echte Lücke

1. **Ein Run im Buchstaben-Modus:** je Turm Bedeutung **und Maske**, dazu der Buchstaben-Vorrat des Levels
   (alle Buchstaben aller Turmwörter, gemischt) statt der Wort-Tokens.
2. **Ein Bewertungsweg für ein buchstabiertes Wort** (`spell`: Turm + gesetzte Buchstabenfolge → richtig /
   falsch, danach die erwartete Lösung), der dieselbe Antwortprüfung nutzt wie Üben und Test.
3. **In der App:** ein Rucksack für mehrere Buchstaben, eine Turm-Ansicht mit Feldern (setzen, vertauschen,
   zurücknehmen), Buchstaben-Drops, die zu den offenen Türmen passen, und eine neue Balance.

## Offene Punkte

1. **Wann bewertet der Server – je Buchstabe oder erst das volle Wort?**
   *Empfehlung:* nur das volle Wort. Eine Rückmeldung je Buchstabe ist ein Orakel: durch Probieren fällt
   jedes Wort ohne Wissen. Nach einem falschen Wort zeigt der Turm die Lösung kurz, leert sich und das Wort
   kommt später im Run wieder (wie das zurückgelegte Token heute).
2. **Wie viele Buchstaben trägt die Figur?**
   *Empfehlung:* ein Rucksack mit 6–8 Plätzen. Je Buchstabe einzeln zum Turm zu laufen heißt neun Gänge
   für ein Wort mit neun Buchstaben – das ist Laufarbeit, kein Lernen.
3. **Welche Buchstaben fallen?**
   *Empfehlung:* nur Buchstaben, die an den gerade offenen Türmen noch fehlen, gezogen aus dem eingefrorenen
   Vorrat des Runs; überzählige lassen sich ablegen. Reiner Zufall führt zu „ich warte auf das q". Ablenker
   (falsche Buchstaben) erst als spätere Schwierigkeitsstufe.
4. **Wovon schießt der Turm, solange das Wort unfertig ist?**
   *Empfehlung:* Grundfeuer ohne Munition, ein gelöstes Wort gibt dem Turm einen kräftigen Schub (Salve oder
   zeitweise doppelte Feuerrate). Munition je gesetztem Buchstaben scheidet aus – ungeprüfte Buchstaben
   wären eine Munitionsquelle ohne Wissen.
5. **Läuft das Spiel weiter, während die Turm-Ansicht offen ist?**
   *Empfehlung:* Zeitlupe. Buchstabieren unter vollem Monsterdruck prüft Fingerfertigkeit, nicht Vokabeln.
6. **Zählt ein buchstabiertes Wort im Lernstand mehr als ein zugeordnetes?**
   *Empfehlung:* dieselbe Dämpfung (einmal je Wort und Tag), aber die Münzen je Level höher als im
   Zuordnen-Modus, weil das Level länger dauert.
7. **Erfüllt der Buchstaben-Modus eine Pflicht mit `RequireTypedTest`?** B-822 Entscheidung 2 schließt das
   Spiel dort aus, weil der Vater Tippen verlangt hat.
   *Empfehlung:* bleibt ausgeschlossen. Aus einem sichtbaren Vorrat zu legen ist leichter als frei zu tippen.
8. **Wer wählt den Modus?**
   *Empfehlung:* das Kind beim Level-Start, je Position; Wörter, die nicht legbar sind (über 12 Buchstaben),
   fallen im Buchstaben-Modus aus dem Level.
9. **Eigene Teilstories?**
   *Empfehlung:* erst nach dem Rollengang von B-853 schneiden – ob das Grundspiel trägt, ist noch nicht an
   einem Sohn gemessen.

## Akzeptanzkriterien

Entwurf, final erst nach dem Grillen.

- [ ] Der Run-Start im Buchstaben-Modus liefert je Turm Bedeutung und Maske und den gemischten
  Buchstaben-Vorrat; kein Payload enthält die Lösung oder die Zuordnung Buchstabe → Turm, die
  Lösungsfeld-Regel bleibt grün.
- [ ] `spell` bewertet nur ein vollständig gefülltes Wort, mit derselben Antwortprüfung wie die Teststufe
  Buchstabenkästchen, und schreibt ein `ItemReviewEvent` mit Quelle `Game`; ein Integrationstest belegt,
  dass ein unvollständiges Wort keine Rückmeldung über einzelne Buchstaben gibt.
- [ ] Ein falsches Wort löst den Turm nicht, die Antwort nennt die Lösung, das Wort erscheint später im
  selben Run erneut.
- [ ] In der App: mehrere Buchstaben tragen, am Turm einzeln in ein Feld setzen, vertauschen und
  zurücknehmen; feste Zeichen der Maske (Leerzeichen, Bindestrich) sind vorbelegt.
- [ ] Ein Sohn spielt ein Level im Buchstaben-Modus an der echten Instanz durch (Rollengang).

## Verlauf

- 2026-11-24 · `ausformuliert` angelegt aus dem Dialog: Ist-Stand am Code belegt, neun offene Punkte mit
  Empfehlung. Kernfund: Maske und gemischte Buchstaben gibt es serverseitig schon (B-66, B-05); neu sind
  der Bewertungsweg für ein geschriebenes Wort und fast die ganze App-Seite.
