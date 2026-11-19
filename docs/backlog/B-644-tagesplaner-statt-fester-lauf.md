---
tags: [typ/story, status/gegrillt, bereich/lehrplan, bereich/training, bereich/auswertung, rolle/supervisor, rolle/student, rolle/creator]
aliases: [Tagesplaner, Tagesauswertung, Leistungsprofil, adaptiver Tagesplan]
status: gegrillt
prio: P1
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Vater im Chat 2026-11-17: statt eines fest geplanten Laufs je Kapitel nur noch Tages- und Bonusaufgaben, jeden Morgen um 6 Uhr aus der Auswertung der Vortage, dem Leistungsprofil und dem Stundenplan erzeugt"
grund: ""
ersetzt_durch: []
---

# B-644 · Ein Tagesplaner erzeugt jeden Morgen die Aufgaben aus der Auswertung der Vortage

Heute legt der Lehrer (oder der Vater) einen ganzen Lauf im Voraus an: ein `StudyPlan` je Fach mit
Positionen, deren Pflicht (`Cadence`, `NewItemsPerDay`, Ziel, Malus) für die ganze Laufzeit feststeht. Der
Plan reagiert nicht darauf, wie das Kind gestern gelernt hat. Der Vater möchte das umdrehen: Es gibt nur
noch **Tagesaufgaben** (Pflicht) und **Bonusaufgaben**. Jeden Morgen um 6 Uhr wertet Pugling die Vortage
aus, schreibt mit, was gelernt wurde und wo es hakte, und erzeugt daraus je Kind die Aufgaben des Tages –
nach Leistungsprofil, bisherigen Daten und Stundenplan.

Das Vorhaben passt nicht in eine Sitzung und sollte nach dem Grillen als Karte (`/wayfinder`) laufen. Die
Grill-Runde mit dem Vater lief **vor** dem Ausformulieren (siehe `## Entscheidungen`); der Ist-Stand unten
hat daraus sechs neue offene Punkte gemacht.

## User Story

Als *Vater* möchte ich, dass Pugling jeden Morgen selbst entscheidet, was jedes Kind heute lernen muss,
damit die Pflicht zum tatsächlichen Lernstand passt und ich keinen Lauf mehr von Hand austariere. Als
*Vater* möchte ich außerdem je Tag nachlesen, was gelernt wurde und wo es Probleme gab. Als *Sohn* möchte
ich Aufgaben, die zu dem passen, was ich gestern konnte und was morgen in der Schule dran ist.

## Ist-Stand am Code

Gemessen am 2026-11-17 auf `main` (Stand `36185a56`). Pfade relativ zu `backend/`, wenn nicht anders
genannt. Im selben Checkout baute währenddessen eine zweite Sitzung an den Lernkarten; die hier belegten
Stellen liegen außerhalb ihres Umbaus, die Zeilennummern in `PositionProgressService.cs` können sich aber
verschoben haben und sind beim Schätzen nachzusehen.

### Die Pflicht hängt an der Position und ist statisch

- `PlanPosition` trägt `NewItemsPerDay`, `Cadence`, `GoalThreshold` und `PenaltyCoins`
  (`Pugling.Api/Models/PlanPositionEntities.cs:40`, `:51`, `:75`, `:91`). Die Taktung kennt nur `None`,
  `Daily`, `Weekly` (`Pugling.Contracts/Common/PlanPositionBaseTypes.cs:4-12`).
- Belohnung und Malus werden je Position und Periode gebucht (`PositionGoalReward`
  `PlanPositionEntities.cs:166`, `PositionGoalPenalty` `:189`), jeweils mit Fremdschlüssel auf die Position.
- Der Malus wird **träge** abgerechnet: `SettleClosedPeriodsAsync`
  (`Pugling.Api/Services/Shared/PositionProgressService.cs:585`) läuft nur beim Kind-Login
  (`Pugling.Api/Controllers/AuthController.cs:85`) und an einer Stelle in
  `Controllers/Student/MeController.cs:296`, höchstens 14 Tage zurück (`MaxSettleLookbackDays`, `:25`).
  Der Doc-Kommentar der Methode sagt ausdrücklich „There is no scheduler".

### Es gibt keinen Zeitgeber und keine Ortszeit

- Kein `BackgroundService`, `IHostedService`, `AddHostedService` oder `PeriodicTimer` im ganzen Backend
  (Suche ohne Treffer). Der 6-Uhr-Lauf wäre das erste zeitgesteuerte Stück.
- „Heute" wird an **35 Stellen in 15 Dateien** als `DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime)`
  gebildet (z. B. `Controllers/Student/ChildTodayController.cs:31`, `AuthController.cs:84`). Die Uhr ist
  überall ein injizierter `TimeProvider` – eine Naht für Tests gibt es also. `TimeZoneInfo` kommt im
  Backend **nicht** vor; es gibt keine zentrale „Heute"-Funktion.

### Serie und Malus kennen keinen freien Tag

- Die Serie läuft Kalendertag für Kalendertag rückwärts (`ChildStreakAsync`,
  `PositionProgressService.cs:391`, Schleife `:402` `day.AddDays(-1)`) und bricht am ersten unvollständigen
  Tag (`IsCompleteDay`, `:352`). Einen Wochentag fragt sie nicht ab.
- Die Malus-Perioden (`ClosedPeriods`, `:517`) zerlegen nur nach Taktung. `DayOfWeek` wird im Backend nur
  im Shop (Auffülltag) und im Stundenplan-Controller benutzt.
- **Folge:** Ein freier Sonntag (Entscheidung 2) und eine Pause (Entscheidung 8) existieren heute nicht –
  ein Sonntag ohne Lernen reißt die Serie und kostet Malus. Eine Suche nach Pause/Ferien/Krank in Modell,
  Services und Vertrag hat keinen Treffer.

### Rohdaten für die Auswertung: Mengen ja, Fehlerart nein

- Jede Antwort im Üben liegt als `ItemReviewEvent` vor (`Models/ItemProgressEntities.cs:61`): Kind, Item,
  Übung, Vokabel, Position, Quelle (`:80`), Stufe (`:82`), **gegebene Antwort** (`:84`), richtig/falsch
  (`:86`), Zeitpunkt (`:88`).
- Jede Testantwort liegt als `TestItemResult` vor (`Models/StudyPlanEntities.cs:202`): gegebene Antwort
  (`:218`), richtig/falsch (`:219`), benutzte Hilfen (`:226`). Der Versuch selbst trägt Tag, Stufe, Quote
  und Bestanden (`TestAttempt`, `:163-188`).
- Geübte Zeit: `PracticeSession.ActiveSeconds` (`StudyPlanEntities.cs:112`), je Sitzung mit Tag (`:108`).
- Lernstand je Vokabel über alle Pläne: `ItemProgress` (`ItemProgressEntities.cs:18`) mit Fach (`:35`),
  Beherrschung in Prozent (`:41`), gesehen/richtig (`:44`, `:46`), Einführungstag (`:49`). „Schwach" ist
  Beherrschung unter der Schwelle `ItemProgress.WeakBelowPercent`
  (`Controllers/Student/ChildVocabularyProgressController.cs:27`, `:47`).
- **Eine Fehlerart wird nirgends gespeichert.** Es gibt kein Feld und keinen Typ dafür; eine Suche nach
  Akzent/Diakritik/Verdict findet nur einen Kommentar zur Zeichen-Normalisierung
  (`Services/Shared/StageMechanics.cs:55`). Wo die Regel „Akzentfehler zählt im Üben halb" (B-624) sitzt,
  habe ich unter diesen Suchwörtern **nicht gefunden** – beim Schätzen nachzusehen. Weil die gegebene
  Antwort gespeichert ist, lässt sich die Fehlerart für getippte Antworten **nachträglich** bestimmen; für
  Auswahl-Antworten nicht.
- Eine gespeicherte Tagesauswertung oder ein Leistungsprofil je Fach gibt es nicht (keine Entität).

### Der Plan ist schon fast ein Stoffrahmen

- `StudyPlan` trägt Fach (`Models/StudyPlanEntities.cs:83`) und optional die **Unit der Reihe** (`:86`),
  dazu `StartDate`/`EndDate` (`:88-89`) und `Active` (`:90`). `EndDate` ist **Pflicht** – „Stoff ohne
  Termin" (Entscheidung 1) lässt sich so nicht ausdrücken.
- Ein aktiver Plan je Kind und Fach (B-539) und eine Tagessicht über alle aktiven Pläne
  (`ComputeChildDayAsync`, `PositionProgressService.cs:322`, B-606) sind gebaut. Die Sohn-App liest den Tag
  über `student/children/{childId}/today` (`Controllers/Student/ChildTodayController.cs:14`,
  `frontend/src/lib/api.ts:720`).

### Termin und Teilprüfung: die Klassenarbeit trägt beides, wirkt aber nicht

- `Klassenarbeit` (`Models/KlassenarbeitEntities.cs:67`) hat Kind, Fach, Titel, Thema und ein
  **Pflicht-Datum** (`ScheduledDate`, `:79`). Ihr Stoff ist ein Bündel aus Übungen
  (`KlassenarbeitExercise`, `:92`) und Tags (`KlassenarbeitTag`, `:106`).
- Eine Teilprüfung („Vokabeltest über einen Teil") ist damit als zweite Klassenarbeit mit kleinerem Bündel
  abbildbar – aber nur **auf Ebene ganzer Übungen oder Tags**, nicht als Wortbereich innerhalb einer Übung.
- Kein Student-Endpunkt und kein Service liest die Klassenarbeit für die Pflicht (Befund aus B-248,
  2026-11-14; hier nicht neu gemessen).

### Stundenplan: gespeichert, ohne Wirkung

- `TimetableEntry` (`Models/TimetableEntities.cs:7`): Kind, Fach, Wochentag (`:14`), optionale Uhrzeit.
  Gelesen wird er nur im eigenen Controller (`Controllers/Supervisor/TimetableController.cs:25-51`).

### Einstellungen je Kind

- `Child` (`Models/AdminEntities.cs:63-139`) trägt Klasse, Schulart, Interessen, Notizen und als einzige
  Lern-Stellgröße `BonusDailyCoinCap` (`:96`). Boden/Deckel für Aufgaben oder Wörter, ein Malus-Betrag je
  Kind und eine Pause fehlen.

### „Bonus" gibt es schon dreimal

- **Bonus-Training:** `ExtraTrainingRun` (`Models/GamificationEntities.cs:124`), gespielt über
  `student/children/{childId}/extra-training` (`Controllers/Student/ExtraTrainingController.cs:17`, `:38`,
  `:57`), gedeckelt über `BonusDailyCoinCap`.
- **Tagesbox:** `DailyBoxClaim` (`GamificationEntities.cs:98`) mit dem Serien-Stand beim Öffnen (`:107`).
- **Missionen** mit eigener Punktart (`PointKind.Mission`, `Pugling.Contracts/Common/AdminBaseTypes.cs:49`).
- Alle drei hängen an der Regel „erst die Pflicht, dann die Belohnung" aus der Tagessicht.

### Der Reset ist für das Guthaben unkritisch

- `ChildPointsEntry` (`Models/AdminEntities.cs:211-220`) trägt Kind, Betrag, Art, **Begründung als Text**
  und Zeit – **keinen** Fremdschlüssel auf Plan oder Position. Löscht man Pläne und Positionen, bleibt das
  Kassenbuch vollständig lesbar. Die Sorge aus Entscheidung 12 („Buchungen verweisen auf Positionen, die es
  nicht mehr gibt") trifft nur den Text der Begründung, keine Verknüpfung.
- Mit den Positionen verschwinden `PositionGoalReward`/`PositionGoalPenalty` (Fremdschlüssel). Das sind nur
  die Sperren gegen Doppelbuchung; die Münzen selbst stehen im Kassenbuch.

### KI-Creator kann Förder-Übungen schon bauen

- `--use-weak` (`Pugling.Agent.Creator/AgentCommands.cs:509`, `:575`; `GenerationRequest.cs:36`) nimmt die
  schwach beherrschten Wörter eines Kindes als Wortschatz. Der Agent läuft außerhalb der API; im Backend
  gibt es kein Sprachmodell.

## Die echte Lücke

Kleiner als gedacht ist die **Datenseite**: Antworten, Zeiten und Lernstand je Wort liegen vollständig vor,
der Plan kennt Fach und Unit, die Klassenarbeit kennt Termin und Stoffbündel, das Kassenbuch übersteht den
Reset. Größer als gedacht ist die **Zeitseite**. Es fehlen:

1. **Ein Tagesbegriff in Ortszeit.** 35 Stellen bilden „heute" in UTC, ohne gemeinsame Funktion. Vor dem
   Planer muss „heute" an einer Stelle entstehen (deutsche Zeit, Tagesende Mitternacht).
2. **Lerntage.** Serie und Malus kennen weder den freien Sonntag noch eine Pause.
3. **Ein Zeitgeber** – oder ersatzweise der träge Aufruf beim ersten Zugriff nach 6 Uhr.
4. **Die Tagesaufgabe** als Träger der Pflicht (statt `Cadence`/`PenaltyCoins` an der Position), samt
   Abrechnung je Aufgabe und Begründung.
5. **Tagesauswertung und Leistungsprofil** als gespeicherte Größen, einschließlich der **Fehlerart**, die
   heute nirgends festgehalten wird.
6. **Der Planer** selbst mit seinen Regeln.
7. **Einstellungen je Kind:** zwei Zahlenpaare, Malus-Betrag, Pause.
8. **Stoff ohne Termin:** `StudyPlan.EndDate` und `Klassenarbeit.ScheduledDate` sind Pflichtfelder.

## Akzeptanzkriterien

Final. Je Kriterium die Änderung, die es rot macht.

1. „Heute“ entsteht an genau einer Stelle in deutscher Zeit; ein Tor in `ConventionGuardTests` lehnt neue direkte UTC-Tagesbildung ab; Tests decken den Tag der Umstellung und die Zeitumstellung im März und Oktober, und eine Antwort um 23:30 Ortszeit zählt für den alten, um 00:30 für den neuen Tag. *Rot, wenn* das Tor fehlt oder der Tagesbegriff auf UTC läuft.
2. Je Kind und Lerntag (Montag bis Samstag, nicht in einer Pause) entsteht genau ein Tagesplan, um 6 Uhr durch den Zeitgeber oder beim ersten Zugriff danach; zweimaliges Auslösen legt keinen zweiten an. *Rot, wenn* der Unique-Index auf (Kind, Tag) fehlt.
3. Der Tagesplan enthält mindestens den Boden und höchstens den Deckel an Pflichtaufgaben und an neuen Wörtern und ist nie leer; Wiederholungen zählen nicht gegen den Wörter-Deckel. *Rot, wenn* Boden oder Deckel nicht angewandt werden oder Wiederholungen den Wörter-Deckel verbrauchen.
4. Jede Aufgabe trägt eine für Vater und Kind lesbare Begründung aus den Eingangsdaten. *Rot, wenn* der Text konstant ist.
5. Derselbe Datenstand ergibt denselben Plan. *Rot, wenn* der Planer von Zufall oder einem Sprachmodell abhängt.
6. Eine bis Mitternacht nicht erledigte Pflichtaufgabe kostet genau einmal den Malus-Betrag des Kindes, eine erledigte nichts; der verpasste Stoff erscheint am nächsten Lerntag wieder, ohne den Deckel zu überschreiten. *Rot, wenn* doppelt gebucht wird oder der Rückstand über den Deckel wächst.
7. Sonntag und Pause reißen die Serie nicht und kosten keinen Malus. *Rot, wenn* `ChildStreakAsync` am Sonntag oder in einer Pause bricht oder Malus bucht.
8. Ein nachgetragener Termin ändert den Plan des Folgetags; liegt das nötige Tempo über dem Deckel, sieht der Vater eine Meldung mit der fehlenden Menge. *Rot, wenn* die Meldung fehlt oder die Pflicht still über den Deckel steigt.
9. Fach von heute und morgen laut Stundenplan hat im Plan Vorrang. *Rot, wenn* der Stundenplan ignoriert wird.
10. Der Vater kann eine nicht begonnene Aufgabe streichen und eine eigene dazulegen; beides ist in der Auswertung des Folgetags als Eingriff erkennbar; eine begonnene Aufgabe lässt sich nicht streichen. *Rot, wenn* ein Eingriff nicht vermerkt wird oder das Streichen einer begonnenen Aufgabe gelingt.
11. Die Tagesauswertung ist je Kind, Tag und Fach gespeichert (Mengen und Fehlerarten); das Vater-Web zeigt Verlauf und Problemwörter, die Sohn-App nur die eigene Kurve und die Begründung. *Rot, wenn* ein Student-Endpunkt Fehlerarten oder Zahlen des Bruders liefert.
12. Im Schattenmodus entsteht der Plan täglich, wirkt aber auf Pflicht, Malus und Sohn-App nicht. *Rot, wenn* ein Schattenplan Münzen bucht oder in der Sohn-App erscheint.
13. Nach dem Stichtag sind Lernstand, Pläne und Positionen beider Kinder leer; Münzen, Gems, Skins und Shop-Einlösungen sind unverändert, das Kassenbuch vollständig (Test gegen eine Kopie der echten DB). *Rot, wenn* der Reset `ChildPointsEntry` oder das Inventar anfasst.
14. Fällt das Sprachmodell aus, entsteht der Tagesplan trotzdem. *Rot, wenn* der Planer-Dienst eine Abhängigkeit auf einen Chat-Client hat.
15. Eine Bonusaufgabe ist ein vorgeschlagenes Bonus-Training und bucht nur im Rahmen von `BonusDailyCoinCap`. *Rot, wenn* eine Bonusaufgabe Münzen außerhalb des Deckels bucht.
16. Eine Teilprüfung (zweite Klassenarbeit mit kleinerem Bündel) verdichtet den Plan auf ihren Termin; der Planer kennt mehr als eine Klassenarbeit je Fach. *Rot, wenn* nur die späteste Klassenarbeit wirkt.
17. Ein `StudyPlan` ohne `EndDate` wird von Tagessicht, Planer, Dashboard und Medienbedarf ohne Fehler gelesen (je Lesestelle ein Fall). *Rot, wenn* eine der sieben Lesestellen bei `NULL` fehlschlägt oder ein Enddatum erfindet.
18. Bei jeder getippten Antwort steht die Fehlerart beim Speichern fest; eine Auswahl-Antwort trägt keine. *Rot, wenn* die Fehlerart erst beim Lesen berechnet wird oder bei Auswahl gefüllt ist.
19. Ein Tag ohne erzeugten Plan (Server aus) kostet keinen Malus, lässt die Serie stehen und erscheint beim Vater als Meldung. *Rot, wenn* Malus gebucht wird oder die Meldung fehlt.

## Entwurf

### Grundsatz: Stoff und Lernstand bleiben, nur die Pflicht wandert

Positionen bleiben als **dauerhafter Lernstand je Übung** bestehen (Leitner-Fächer, Item-Fortschritt,
Punkte hängen daran, und die Daten der Söhne müssen überleben). Was sich ändert: Die **Pflicht** steht nicht
mehr an der Position, sondern an einer neuen **Tagesaufgabe**. Der `StudyPlan` schrumpft vom Fahrplan zum
**Stoffrahmen** („diese Units, bis zu diesem Termin"). Das ist erst ein Erweitern und am Ende ein Ersetzen.

### Drei Bausteine

1. **Tagesauswertung** (neu, gespeichert, eine Zeile je Kind, Tag und Fach). Rechnet in C# aus den
   vorhandenen Ereignissen: geübte Zeit, neue / gefestigte / zurückgefallene Wörter, Fehlerarten (Akzent,
   Tippfehler, falsches Wort, Ausweichen auf Synonym), Testergebnis, erfüllte und verpasste Pflicht. Weil
   sie gespeichert ist, lässt sich der Verlauf plotten und bleibt stabil, auch wenn sich Regeln später ändern.
2. **Leistungsprofil** (neu, je Kind und Fach, gleitend über die letzten 7 und 28 Tage): Beherrschung,
   tragfähiges Tempo (neue Wörter je Tag, die tatsächlich hängen bleiben), typische Fehlerarten,
   Belastbarkeit (wie oft wurde die Pflicht zuletzt geschafft).
3. **Tagesplaner** (neu): erzeugt je Kind und Tag einen **eingefrorenen Tagesplan** aus Tagesaufgaben
   (`Pflicht` mit Malus, `Bonus` ohne). Jede Aufgabe trägt Übung/Position, Wortauswahl, Ziel, Münzen, Malus
   und eine **Begründung** („gestern 6 von 10 Akzentfehler", „morgen Französisch"). Eindeutig je
   (Kind, Tag), also idempotent.

### Der Planer ist regelbasiert, nicht LLM

Empfehlung: Der Planer ist deterministisches C# mit festen Regeln in fester Reihenfolge – prüfbar,
testbar, erklärbar, und er passt zu „kein LLM im Backend". Vorschlag für die Regeln:

1. Fällige Wiederholungen zuerst (Leitner), gedeckelt.
2. Gestern Gescheitertes kommt wieder, aber eine Stufe leichter (berührt B-536, B-635).
3. Stundenplan: Fach von heute und morgen bekommt Vorrang (übernimmt B-621/B-622).
4. Neuer Stoff nur, wenn das Profil es trägt; Menge aus dem tragfähigen Tempo und dem Abstand zum Termin
   des Stoffrahmens bzw. der Klassenarbeit (übernimmt B-248).
5. Verteilung über die Fächer nach Schwäche (übernimmt B-618/B-617).
6. Deckel und Boden je Kind (Minuten oder Aufgabenzahl), die der Vater setzt.
7. Was über den Deckel hinausgeht oder Kür ist, wird Bonusaufgabe (berührt B-218 Mischwiederholung).

Ein LLM kommt nur **außerhalb** der API in Frage: der KI-Creator baut nachts Förder-Übungen zu den
schwachen Wörtern, und optional einen Prosa-Satz zur Tagesauswertung. Beides ist eine spätere Etappe.

### Der 6-Uhr-Lauf

Empfehlung: Die Wahrheit ist ein **träger, idempotenter Aufruf** („gibt es für heute noch keinen Plan und
ist es nach 6 Uhr Ortszeit, erzeuge ihn"), ausgelöst beim ersten Zugriff. Ein kleiner Hintergrunddienst
ruft genau dieselbe Methode um 6 Uhr auf. So entsteht der Plan auch, wenn der Heimserver um 6 Uhr aus war,
und es gibt nur einen Codepfad. Die Tageslogik rechnet bisher in UTC; „6 Uhr" braucht eine konfigurierte
Zeitzone.

### Selbstbetrug

- Der Tagesplan ist nach dem Erzeugen eingefroren; das Kind kann ihn nicht neu würfeln.
- Schlecht spielen darf die Pflicht nicht beliebig senken: Boden je Kind, und „leichter" heißt andere
  Mechanik, nicht weniger Arbeit.
- Verpasste Pflicht kostet wie bisher Münzen; der Stoff kommt am Folgetag wieder, aber gedeckelt, damit
  keine Schuldenspirale entsteht.

## Etappen (Vorschlag, nach Entscheidung 11 und 14)

| Etappe | Inhalt | Wirkung auf die Kinder |
| --- | --- | --- |
| E0 | Ein Tagesbegriff in deutscher Zeit, Lerntage (Sonntag frei), Pause je Kind | Tag endet um Mitternacht, Sonntag frei |
| E1 | Tagesauswertung mit Fehlerart, Leistungsprofil, Vater-Ansicht mit Verlauf | keine, nur lesen |
| E2 | Planer im **Schattenmodus**, eine Woche; Vater sieht die Tagespläne | keine |
| E3 | **Stichtag:** Reset von Lernstand und Plänen, Tagesaufgaben tragen Pflicht und Malus, die feste Pflicht an der Position entfällt | Pflicht kommt aus dem Planer |
| E4 | Bonusaufgaben, Termin- und Stundenplan-Regeln, Eingriffe des Vaters, Sohn-App zeigt die Begründung | voll |
| E5 | KI-Creator baut nachts Förder-Übungen; Zusammenfassung in Sätzen | Inhalt wächst mit |

E0 ist neu und steht bewusst vorn: Es ist der einzige Teil, der schon vor dem Stichtag das Verhalten für
die Söhne ändert, und er ist auch ohne Planer richtig. Jede Etappe bringt eine anhängende Migration.

## Berührte Stories

Der Planer macht aus mehreren geplanten Einzelmechaniken **Regeln eines Mechanismus**:
B-618/B-619 (Fächer-Verteilung, `geschaetzt`/`gegrillt`), B-621/B-622 (Stundenplan-Pflicht, `geschaetzt`),
B-617 (Verteilung vorschlagen), B-248 (Klassenarbeit verdichtet den Plan), B-218 (Mischwiederholung),
B-220 (Learning Queue), B-536 (Aufbau-Übungen). **B-618, B-621 und B-622 stehen kurz vor dem Bau** – sie
würden die Pflicht an einer Stelle verankern, die dieser Umbau wieder auflöst. Empfehlung: ihren Bau
anhalten, bis diese Story gegrillt ist; ihre Entscheidungen (Prozent-Gewichte, Fachtag und Vortag) werden
Eingaben des Planers.

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-20 (Sonnet) auf Anweisung des Nutzers (Empfehlung übernommen), unbestätigt.*

## Entscheidungen

Grill-Runde mit dem Vater am 2026-11-17, im Dialog. Die Story steht dabei noch auf `idee`; der Ist-Stand
ist nachzuprüfen, bevor sie weiterwandert.

1. **Der Stoffrahmen bleibt, der Termin ist optional und kommt oft später.** Je Kind und Fach steht fest,
   welcher Stoff (Kapitel) dran ist; Tagesmengen, Reihenfolge und Pflicht entscheidet der Planer. Der
   Klausurtermin steht anfangs meist **nicht** fest und wird nachgetragen. Dazu kommen **kurzfristige
   Teilprüfungen** (Vokabeltest über einen Teil des Stoffs). Begründung: Was dran ist, lässt sich aus den
   Lerndaten nicht ableiten; so läuft es in der Schule der Söhne tatsächlich. Kosten: Der Planer braucht ein
   Tempo **ohne** Termin, muss beim Nachtragen eines Termins am Folgetag neu rechnen, und eine Teilprüfung
   muss sich schnell eintragen lassen (Teilmenge des Stoffs plus Datum) – sonst pflegt sie niemand.

2. **Der Planer wählt das Tempo selbst zwischen Boden und Deckel – und der Boden ist nie null.** Ohne
   Termin kommt das Tempo aus dem Leistungsprofil; mit Termin rechnet der Planer rückwärts und meldet dem
   Vater, wenn das nötige Tempo über dem Deckel liegt, statt die Pflicht still zu erhöhen. Es gibt **jeden
   Tag von Montag bis Samstag** eine Pflicht, der **Sonntag ist frei**; die Serie (Streak) läuft über diese
   sechs Tage. Begründung: Festigung braucht tägliches Lernen, auch wenn gerade kein neuer Stoff ansteht.
   Kosten: Auch wenn alles sitzt, muss der Planer eine Mindestaufgabe finden (Wiederholung statt neuem
   Stoff); Pflicht, Malus und Serie müssen den Sonntag überspringen – ob die heutige Serie und die
   Malus-Abrechnung das schon können, ist beim Ausformulieren am Code nachzusehen. Ohne Termin bestimmt das
   Kind sein Tempo oberhalb des Bodens mit; der Boden muss für die Schule reichen.
3. **Boden und Deckel sind feste Mengen, keine Minuten.** Begründung: für den Vater einfacher einzustellen
   und exakt prüfbar; eine Zeitschätzung je Kind und Aufgabenart entfällt. Kosten: Aufgaben sind
   unterschiedlich lang, der Vater schätzt den Zeitaufwand selbst; der Planer muss darum auf ähnlich große
   Aufgaben achten, sonst schwankt die echte Lernzeit bei gleicher Menge stark.
4. **Zwei Zahlenpaare je Kind: Pflichtaufgaben am Tag und neue Wörter am Tag, je mit Boden und Deckel.**
   Beide gelten über alle Fächer zusammen; die Verteilung auf die Fächer entscheidet der Planer.
   Wiederholungen fälliger Wörter zählen nicht gegen den Wörter-Deckel. Begründung: Die Aufgabenzahl ist,
   was das Kind sieht; die Wörterzahl gibt dem Vater den direkten Griff auf das Stofftempo. Kosten: vier
   Zahlen je Kind; Wiederholungen sind nur über die Aufgabenzahl begrenzt, nach einer Pause kann ein Tag
   voller sein. Das Tagesbudget neuer Wörter aus B-618 geht hierin auf.
5. **Malus je verpasster Pflichtaufgabe, mit einem Betrag je Kind.** Verpasster Stoff kommt am nächsten
   Lerntag wieder, aber nur innerhalb des Deckels – es wächst kein Rückstand an Aufgaben. Die Serie reißt,
   sobald an einem Lerntag eine Pflichtaufgabe offen bleibt. Begründung: Teilarbeit lohnt sich (drei von
   vier geschafft kostet eine), und ein Tages-Malus nähme nach der ersten verpassten Aufgabe jeden Anreiz.
   Kosten: Der Betrag muss weh tun, sonst rechnet das Kind das Auslassen durch; ein fauler Tag schiebt den
   Stoff nach hinten, der Planer meldet es bei einem Termin nur früher. Der Malus an der Position
   (`PlanPosition.PenaltyCoins`) verliert für Planer-Kinder seine Bedeutung.
6. **B-618, B-619, B-621 und B-622 sind angehalten und werden nicht gebaut**, bis diese Story gegrillt ist.
   Sie tragen `wartet_auf: B-644`; Stufe und Entscheidungen bleiben. Danach wird jede als Regel des Planers
   überführt oder verworfen. Begründung: Sie verankern die Pflicht an der Position (Taktung „Stundenplan",
   Budget je Position), also dort, wo der Planer sie wegnimmt. Kosten: Fächermix nach Schwäche und
   Stundenplan-Pflicht kommen erst mit der scharfen Etappe des Planers; die Sprints 89 und 95 der
   [Sprintplanung](../pm-sitzung-2026-11-17-sprintplanung.md) entfallen vorerst. Der laufende Sprint
   (Lernkarten) ist nicht betroffen.
7. **Der Tagesplan gilt automatisch, ohne Freigabe; der Vater kann eingreifen.** Er sieht den Plan mit
   Begründung je Aufgabe und kann einzelne Aufgaben streichen oder eine eigene dazulegen, auch nachdem das
   Kind den Plan sieht. Jeder Eingriff wird vermerkt, damit die Auswertung des Folgetags Planer-Menge und
   Vater-Menge unterscheidet. Begründung: Eine Freigabe ließe das Kind an jedem Morgen ohne Aufgaben, an dem
   der Vater nicht hineinschaut. Kosten: Begonnene Aufgaben sind nicht mehr streichbar (Vorschlag, beim
   Ausformulieren festzuziehen); der Vater kann durch Streichen den Boden aus Entscheidung 2 unterlaufen –
   das ist dann seine bewusste Ausnahme.
8. **Pause von Hand je Kind; Schulferien sind keine automatische Pause.** Der Vater trägt eine Pause von
   Datum bis Datum ein, auch rückwirkend für heute. In der Pause entsteht kein Tagesplan, kein Malus, und
   die Serie bleibt stehen statt zu reißen; fällige Wiederholungen kommen danach innerhalb des Deckels
   zurück. Ohne Eintrag läuft die Pflicht in den Ferien am Boden weiter. Begründung: Festigung über die
   Ferien ist gewollt, und ein Ferienkalender deckte Krankheit ohnehin nicht ab. Kosten: Eine vergessene
   Pause bucht Malus, den der Vater von Hand zurückgibt; nach langer Pause bestehen die ersten Tage fast
   nur aus Wiederholung.
9. **Der Lerntag ist der Kalendertag in deutscher Zeit; der Plan entsteht um 6 Uhr, mit Plan B.** Der Tag
   endet um Mitternacht, dann werden offene Pflichtaufgaben abgerechnet; zwischen Mitternacht und 6 Uhr gibt
   es nur freies Üben. **Der Heimserver läuft nicht garantiert durch** (Auskunft des Vaters): Der Plan
   entsteht darum beim ersten Zugriff nach 6 Uhr, falls der Zeitgeber ihn nicht erzeugt hat – derselbe
   idempotente Aufruf, ein Codepfad. Vorschlag für den Rest von Plan B, beim Ausformulieren festzuziehen:
   Ein Tag, an dem der Server gar nicht lief und darum nie ein Plan entstand, zählt wie eine Pause (kein
   Malus, Serie steht) und wird dem Vater **sichtbar** gemeldet – sonst wäre Server-Ausschalten ein Weg um
   die Pflicht. Begründung: „bis heute Abend" soll für das Kind stimmen; heute endet der Tag in UTC um 1
   bzw. 2 Uhr nachts. Kosten: Die Umstellung von UTC auf deutsche Zeit trifft auch bestehende Pläne, Serien
   und die Malus-Abrechnung – ein eigener, heikler Schritt an den echten Daten, mit Tests für den Tag der
   Umstellung und die Zeitumstellung im März und Oktober.
10. **Der Planer entscheidet mit festen Regeln im Programm; ein Sprachmodell stellt den Plan nicht
    zusammen.** Jede Aufgabe trägt den auslösenden Grund. Ein Sprachmodell kommt nur außerhalb der
    Entscheidung dazu: Der KI-Creator baut nachts Förder-Übungen zu den schwachen Wörtern, und auf Wunsch
    entsteht eine kurze Zusammenfassung der Tagesauswertung in Sätzen. Begründung: Die Pflicht kostet
    Münzen und muss nachrechenbar und wiederholbar sein; ein Ausfall des lokalen Modells darf keinen Tag
    ohne Plan erzeugen. Kosten: Regeln finden nur, wonach sie suchen – ein unvorhergesehenes Muster bleibt
    unentdeckt, bis jemand eine Regel dafür schreibt; der Planer wird schrittweise klüger, nicht von allein.
11. **Umstellung zum Stichtag, ohne Rücksicht auf den bisherigen Lernstand.** Auskunft des Vaters: Die
    Menge der bisherigen Übungen von Theodor und Friedrich ist zu vernachlässigen, beide können komplett
    zurückgesetzt werden. Damit entfallen der Parallelbetrieb „je Kind einschaltbar" und die Pflicht, die
    alten Pläne und Positionen in das neue Modell zu überführen; das alte Modell (feste Pflicht an der
    Position) kann mit der scharfen Etappe wegfallen statt in einer eigenen Etappe danach. Begründung:
    Zwei Wege zur Pflicht nebeneinander kosten Code und Tests, die der geringe Bestand nicht rechtfertigt.
    Kosten: Fehler des Planers treffen ab dem Stichtag direkt die Münzen beider Kinder; ein Zurück gibt es
    nur über das Backup. Dies ist eine **Ausnahme für diesen Umbau** von der Regel „die Daten der Söhne
    überleben jede Schemaänderung" (`CLAUDE.md`, Fallstricke) – die Regel selbst gilt weiter, und die
    Migrationskette wird weiterhin nur verlängert. Umfang des Resets → Entscheidung 12, Schattenmodus →
    Entscheidung 14.
12. **Der Reset umfasst Lernstand und Pläne; das Guthaben bleibt.** Zurückgesetzt werden Leitner-Stände,
    Testergebnisse, Verlauf sowie die festen Pläne und Positionen. Münzen, Gems, gekaufte Skins und
    eingelöste Shop-Artikel bleiben; die Serie beginnt neu, weil sie am alten Tagesbegriff hängt.
    Begründung: Münzen sind der stärkste Antrieb der Kinder, ein Nullstand wäre eine Strafe für eine
    technische Umstellung. Kosten: Der Planer startet ohne Leistungsprofil und behandelt in den ersten ein
    bis zwei Wochen auch bekannte Wörter als neu; Buchungen im Münz-Verlauf verweisen auf Positionen, die
    es nicht mehr gibt (als Text lesbar, nicht mehr verlinkt) – wie der Verlauf das technisch übersteht,
    ist beim Ausformulieren am Code nachzusehen.
13. **Die Tagesauswertung speichert Mengen und Fehlerarten; es gibt zwei Ansichten.** Gespeichert wird je
    Kind, Tag und Fach. **Vater-Web:** Verlauf über die Wochen (gekonnte Wörter, neue Wörter je Tag,
    erfüllte und verpasste Pflicht), Liste der Problemwörter mit Fehlerart (Akzent, Tippfehler, falsches
    Wort, Ausweichen auf ein Synonym), Tagesplan mit Begründung je Aufgabe. **Sohn-App**, bewusst schmaler:
    der eigene Fortschritt als Kurve und die Begründung an der Aufgabe; keine Fehlerstatistik, kein
    Vergleich mit dem Bruder. Begründung: „Wo gab es Probleme" soll mit dem Warum beantwortet werden, nicht
    nur mit einer Wortliste. Kosten: Die Fehlerart muss bei jeder Antwort bestimmt und gespeichert werden –
    für welche Übungstypen das heute schon geht, ist beim Ausformulieren am Code nachzusehen; später
    ergänzte Fehlerarten fehlen in den alten Tagen.

14. **Eine Woche Schattenmodus vor dem Stichtag.** Der Planer rechnet jeden Morgen mit den Daten aus den
    heutigen festen Plänen, ohne Wirkung; der Vater sieht die Tagespläne im Vater-Web und setzt den Stichtag,
    wenn sie ihm sieben Tage lang plausibel vorkommen. Begründung: Ab dem Stichtag kostet jeder Fehler des
    Planers die Kinder Münzen. Kosten: eine Woche später scharf; die Anzeige der Schattenpläne wird vor der
    eigentlichen Planer-Ansicht gebraucht und ist als deren Vorstufe zu bauen, nicht als Wegwerf-Oberfläche.

<!-- markdownlint-disable MD029 -->

**Ab hier stellvertretend** (Vorbereitungslauf 2026-11-20, siehe Zeile über der Überschrift): Die Entscheidungen 1 bis 14 stammen vom Vater (2026-11-17) und sind unberührt. Die Entscheidungen 15 bis 20 beantworten die offenen Punkte 11 bis 16, die Entscheidungen 21 bis 25 sind die Rahmenentscheidungen A, B, D, E und G des Clusters (führend B-681, dort mit F), Entscheidung 26 den Zuschnitt.

15. **Eine Bonusaufgabe ist ein vom Planer vorgeschlagenes Bonus-Training mit Begründung** – dieselbe Mechanik wie `ExtraTrainingRun` (`GamificationEntities.cs:124`), gedeckelt über `BonusDailyCoinCap`; Tagesbox und Missionen bleiben, wie sie sind. Begründung: Bonus gibt es schon dreimal; ein vierter Weg würde die Münz-Obergrenze umgehen. Kosten: Der Planer braucht einen Vorschlagspfad in das bestehende Training; die Bonus-Kasse bleibt am Deckel. Verworfen: eine Bonusaufgabe als eigene Entität mit eigener Münzregel. Schließt aus: Bonus ohne Münzdeckel – die Sohn-Story „Aufgaben, die zu mir passen“ gilt für das Angebot, nicht für unbegrenzte Münzen.
16. **Eine Teilprüfung ist eine eigene Klassenarbeit mit kleinerem Bündel aus Übungen oder Tags;** einen Wortbereich innerhalb einer Übung gibt es nicht. Begründung: Das Modell trägt es schon (`KlassenarbeitExercise`, `KlassenarbeitTag`), solange die Wortlisten je Buchabschnitt als eigene Übung geschnitten sind; ob das für alle geladenen Bücher stimmt, ist beim Schätzen an den Übungen nachzusehen. Kosten: Eine Teilprüfung über die Hälfte einer Übung braucht erst eine Teilung der Übung. Verworfen: ein Wortbereich (Von-Bis) an `KlassenarbeitExercise` – Schema und Migration für einen seltenen Fall. Schließt aus: einen Test über die Wörter 1 bis 15 einer 30-Wörter-Übung ohne vorherige Teilung; die User Story „was morgen in der Schule dran ist“ wird dort vom Vater durch Teilen erfüllt.
17. **`StudyPlan.EndDate` wird optional, `Klassenarbeit.ScheduledDate` bleibt Pflicht.** Der Stoffrahmen bleibt der `StudyPlan` (Fach, Unit, Start); ein Plan ohne Termin trägt kein `EndDate`. Ein noch nicht feststehender Klausurtermin heißt: noch keine Klassenarbeit angelegt, sie entsteht beim Nachtragen. Begründung: B-644 Entscheidung 1 (Termin optional, kommt oft später); das ist die kleinste Änderung am Modell, ohne zweites Aggregat mit eigenem Eigentumsfilter und eigener Aktivierungsregel je Fach (B-539). Kosten: `EndDate` wird an 29 Fundstellen gelesen (Api und Contracts ohne Migrationen und Tests, Zählung 2026-11-20; Api-Dateien: PlanOverviewController, StudentPlansController, StudyPlansController, MediaDemandService, PositionPlayService, PositionProgressService, ChildrenDashboardService) – jede braucht den Fall „ohne Ende“; im Vertrag wird das Feld nullable (Vertragsbruch), dazu eine Migration. Verworfen: eine eigene, schlankere Stoffrahmen-Entität mit Wegfall des `StudyPlan` zum Stichtag – zweite Gruppe für die Aktivierungsregel, zweite Eigentumsprüfung, und die Positionen hängen am Container. Schließt aus: einen Stoffrahmen, der ein Pflicht-Enddatum vortäuscht, und eine Klassenarbeit ohne Datum (Termin unbekannt heißt: keine Klassenarbeit).
18. **Die Fehlerart (Akzent, Tippfehler, falsches Wort, Ausweichen auf ein Synonym) wird beim Antworten bestimmt und mit der Antwort gespeichert, nur für getippte Antworten;** Auswahl-Antworten tragen keine Fehlerart. Begründung: Stabil gegen spätere Regeländerungen; die Altdaten entfallen durch den Reset (Entscheidung 12). Kosten: eine Spalte an `ItemReviewEvent` und `TestItemResult` (Migration) und die Bestimmung im Antwortpfad; wo die Regel „Akzentfehler zählt im Üben halb“ (B-624) sitzt, ist beim Schätzen zu finden (unter Akzent/Diakritik/Verdict nicht gefunden) – dieselbe Vergleichsfunktion soll die Quelle sein. Verworfen: die Fehlerart nachträglich aus der gespeicherten Antwort bestimmen – für getippte Antworten möglich, aber instabil gegen Regeländerungen. Schließt aus: eine Fehlerart für Auswahl-Antworten; das Vater-Web zeigt dort nur das Wort ohne Art.
19. **Ein Tag, an dem der Server nie lief und darum kein Plan entstand, zählt wie eine Pause (kein Malus, Serie steht) und wird dem Vater im Vater-Web sichtbar gemeldet.** Begründung: Der Tag des Kindes darf nicht an der Laufzeit des Heimservers hängen (Entscheidung 9); die Meldung schließt „Server ausschalten“ als Weg um die Pflicht (Rahmen E). Kosten: Das Fehlen eines Plans wird erst beim nächsten Start erkannt; der Vater muss die Meldung lesen. Verworfen: Malus für Tage ohne Server. Schließt aus: einen Malus an einem Tag, an dem das Kind keinen Plan sehen konnte.
20. **Eine Aufgabe ist begonnen, sobald es zu ihr heute eine Antwort oder Sitzung des Kindes gibt; begonnene Aufgaben sind nicht streichbar, unbegonnene und selbst hinzugefügte schon.** Begründung: Sonst nähme ein Streichen nach dem Spielen nachträglich Pflicht, Serie und Malus-Grundlage, und schlecht gespielte Aufgaben ließen sich löschen. Kosten: Eine versehentlich begonnene Aufgabe lässt sich nicht mehr entfernen; Ausweg ist die Pause für den Tag (Entscheidung 8, auch rückwirkend). Verworfen: jederzeit streichbar. Schließt aus: das Streichen einer Aufgabe, die das Kind schon gespielt hat.
21. **Der serverseitige Planer entscheidet den Tagesplan, nicht ein Sprachmodell (Rahmen A, führend B-681).** Der Tagesplan entsteht durch den Planer aus B-644 mit festen Regeln in C#; „Lehrer-Agent“ ist der Name dieses Planers gegenüber dem Kind, kein Modellaufruf. Der Konsolen-Agent (`Pugling.Agent.Creator`) bleibt das Werkzeug für Inhalte und entscheidet keine Pflicht. Begründung: Die Pflicht kostet Münzen und muss nachrechenbar und wiederholbar sein (B-644 Entscheidung 10); ein Agent mit Plan-Steuerung bräuchte außerdem ein eigenes Konto und einen Zugriff auf Kinder, den es nicht gibt (Eigentum läuft über `SupervisorLink`, `AuthAccess.cs:112`). Kosten: Der Planer ist nur so klug wie seine Regeln; ein unvorhergesehenes Muster bleibt unentdeckt, bis jemand eine Regel schreibt. Verworfen: ein Sprachmodell, das den Plan je Kind täglich selbst festlegt – nicht nachrechenbar und mit dem lokalen Modell ausfallgefährdet. Schließt aus: einen frei „erfundenen“ Plan je Kind, wie ihn der Satz „Lehrer-Agent erzeugt jeden Tag den Plan“ nahelegt; das Kind bekommt einen täglichen Plan nach seinem Lernstand, kein Modell-Ergebnis. (Gleichlautend B-680 Entscheidung 2 und B-681 Entscheidung 7; bestätigt B-644 Entscheidung 10.)
22. **Der Vater setzt Leitplanken und hat eine Notbremse, er legt keine Tagesmengen fest (Rahmen B, führend B-681).** Je Kind setzt der Vater einmal Boden und Deckel für Pflichtaufgaben und neue Wörter, den Malus-Betrag und Pausen (B-644 Entscheidungen 2 bis 5 und 8) und behält die Notbremse: eine nicht begonnene Aufgabe streichen oder eine eigene dazulegen (B-644 Entscheidung 7). Er baut keinen Plan und bestimmt keine Tagesmenge. Begründung: Ein Planer braucht eine menschliche Grenze gegen Fehlplanung; die vier Zahlen sind die eigenen Entscheidungen des Vaters vom 2026-11-17 und bleiben unberührt; Belohnung, Shop und Einlösen bleiben seine Aufgabe, die Tagesarbeit nicht. Kosten: Der Vater trägt weiter vier Zahlen und eine Pause je Kind; „nur Belohnung“ aus dem Leitbild ist damit nicht wörtlich erfüllt (abgewichen von B-680 Punkt 3, der Boden und Deckel zu Planer-Voreinstellungen machte). Verworfen: Boden und Deckel als reine Planer-Voreinstellung – hübe B-644 Entscheidung 4 auf und ließe den Planer ohne harte Grenze. Schließt aus: einen Vater, der gar nicht mehr in die Mengen eingreifen kann; die User Story „ich möchte die Plansteuerung nicht mehr selbst tragen“ gilt für die tägliche Arbeit, nicht für die Grenzen. (Gleichlautend B-680 Entscheidung 3 und B-681 Entscheidung 3; berührt nicht B-644 Entscheidungen 2 bis 8.)
23. **Klausurtermin und Note trägt der Vater ein (Rahmen D, führend B-681).** Die Schreibrouten der Klassenarbeit bleiben `Roles.Supervisor` (`KlassenarbeitenController.cs:92`, `:130`); Planer, Agent und eine spätere Foto-Auswertung (B-684) lesen Termin und Note oder schlagen sie nur vor, die Freigabe bleibt beim Vater. Begründung: Er hat den Schulkontakt (WebUntis, Heft, Klausur), und eine Note beeinflusst Stufe und Pflicht – kein Konto, das davon profitiert oder Pflichten setzt, trägt sie selbst ein. Kosten: ein Handgriff mehr nach jedem Scan; solange B-684 fehlt, bleibt das Eintragen Handarbeit. Verworfen: Note direkt aus dem Scan übernehmen. Schließt aus: ein Kind oder einen Agenten, der die eigene Note einträgt (Selbstbetrug).
24. **Der Tagesplan steht ab 6 Uhr fest, und Malus kostet nur, was das Kind sehen konnte (Rahmen E, führend B-681).** Der Plan des Tages ist ab 6 Uhr Ortszeit eingefroren (B-644 Entscheidung 9); eine verpasste Pflichtaufgabe eines gesehenen Plans kostet genau einmal den Malus-Betrag des Kindes (B-644 Entscheidung 5); ein Tag, an dem nie ein Plan entstand, zählt wie eine Pause (B-644 Entscheidung 19). Begründung: Heute steht die Pflicht Wochen vorher an der Position (`PlanPosition.PenaltyCoins`); eine Pflicht, die erst heute früh entsteht, darf nicht rückwirkend oder unsichtbar kosten (Leitbild: „nicht unfair machen“). Kosten: Wird der Plan um 6 Uhr nicht erzeugt, entgeht dem Kind ein Pflichttag (als Pause beim Vater gemeldet); ein erzeugter Plan lässt sich nur über die Notbremse des Vaters ändern. Verworfen: Malus über die Pflicht an der Position auch für Planer-Kinder. Schließt aus: Strafe für Aufgaben, die das Kind nie angezeigt bekam oder die der Planer nach dem Erzeugen verändert hätte.
25. **Modellerzeugter Inhalt wird erst durch einen Menschen Pflicht (Rahmen G, führend B-681).** Förder-Übungen und Vorstufen aus dem Agenten entstehen als Katalog-Entwurf; Pflicht werden sie erst, wenn ein Mensch sie in einen Plan übernimmt (`assign`, Positionsformular, Einschalten der Stufung). Der Planer wählt nur aus Positionen des aktiven Plans und legt aus Modellinhalt keine Position selbst an. Begründung: Das ersetzt die heutige Sicherung „KI-Plan entsteht inaktiv“ (`PlanPlanner.cs:101`) durch die kleinste menschliche Handlung statt einer Freigabe je Aufgabe; C# prüft die Lösbarkeit (B-460), nicht die Didaktik, und die Qualität hängt am Modell. Kosten: Neue Förder-Übungen erreichen das Kind erst nach einem Vater-Klick; der Agent kann nicht unbeaufsichtigt Inhalt bis zum Kind durchreichen. Verworfen: Der Planer nimmt frische Modellübungen selbst auf, Schattenwoche plus Deckel und Boden als einzige Sicherung. Schließt aus: den täglich vollautomatischen Kreislauf „Agent baut, Planer verteilt, Kind lernt ohne Vater“, den das Leitbild andeutet.
26. **B-644 bleibt die Sammel-Story des Planers; die Etappen E0 bis E5 sind der Schnitt, es gibt keine Karte mit Ticket-Dateien.** Beim Schätzen entsteht je Etappe eine eigene Story oder B-644 wird je Etappe geschätzt (README „Teilen und Zusammenlegen“, dann geht B-644 auf `verworfen` mit `ersetzt_durch`); das entscheidet der Schätz-Lauf, nicht diese Story. Begründung: Alle offenen Punkte sind entschieden und hängen nicht mehr voneinander ab – damit entfällt der Grund für eine Karte (README „Karten“). Kosten: Entscheidungen 15 bis 26 sind stellvertretend und unbestätigt; widerspricht der Nutzer bei einer Rahmenentscheidung, kann doch eine Karte nötig werden. Verworfen: eine Karte mit Entscheidungs-Tickets. Schließt aus: Ticket-Dateien unter `karten/B-644/`.

<!-- markdownlint-enable MD029 -->

## Offene Punkte

1. ~~**Ersetzen oder daneben?** Empfehlung: je Kind einschaltbar daneben (E3), Ersetzen erst in E6.~~
   → Entscheidung 11 (ersetzen zum Stichtag; die Etappentabelle oben ist beim Ausformulieren anzupassen).
2. ~~**Bleibt ein Stoffrahmen?** Der Planer muss wissen, welcher Stoff bis wann dran ist. Empfehlung: ja,
   der Plan bleibt als Rahmen („Unit 1 bis 28.12."), nur ohne Tagesvorgaben.~~ → Entscheidung 1.
3. ~~**Regeln oder LLM für die Entscheidung?** Empfehlung: Regeln in C#; LLM nur für Inhalt und Prosa.~~
   → Entscheidung 10.
4. ~~**Sieht oder ändert der Vater den Tagesplan vor dem Kind?** Empfehlung: automatisch gültig, der Vater
   kann einzelne Aufgaben streichen oder ergänzen, ohne Freigabepflicht.~~ → Entscheidung 7.
5. ~~**Deckel und Boden:** in Minuten oder in Aufgaben? Wochenende?~~ → Entscheidungen 2 bis 4.
   Ferien und Krankheit → Entscheidung 8.
6. ~~**Verpasste Pflicht:** Malus wie heute plus gedeckeltes Nachholen – oder verfällt der Stoff des Tages?~~
   → Entscheidung 5.
7. ~~**Münzen:** Bleibt der Malus je Aufgabe, oder gibt es einen Tages-Malus für den ganzen Plan?~~
   → Entscheidung 5.
8. ~~**6 Uhr:** Läuft der Heimserver durchgehend? Welche Zeitzone gilt, und wann endet der „Tag" (Mitternacht
   Ortszeit statt UTC)?~~ → Entscheidung 9.
9. ~~**Was wird mitgeplottet und für wen?** Je Wort, je Fehlerart, je Fach; nur Vater oder auch das Kind?~~
   → Entscheidung 13.
10. ~~**B-618/B-621/B-622 jetzt anhalten?** Siehe „Berührte Stories".~~ → Entscheidung 6.

### Nach dem Ausformulieren neu offen (alle am 2026-11-20 stellvertretend entschieden)

<!-- markdownlint-disable MD029 -->

11. ~~**Was ist eine Bonusaufgabe?** Bonus gibt es schon dreimal (Bonus-Training, Tagesbox, Missionen). Empfehlung: Eine Bonusaufgabe ist ein vom Planer **vorgeschlagenes Bonus-Training** mit Begründung – dieselbe Mechanik, kein vierter Weg; Tagesbox und Missionen bleiben, wie sie sind.~~ → Entscheidung 15.
12. ~~**Wie fein ist eine Teilprüfung?** Die Klassenarbeit bündelt ganze Übungen oder Tags. Reicht das („Vokabeltest über Unit 1, Teil A" = eine Übung), oder braucht es einen Wortbereich innerhalb einer Übung? Empfehlung: Übungen und Tags reichen, solange die Wortlisten je Buchabschnitt geschnitten sind.~~ → Entscheidung 16.
13. ~~**Stoff ohne Termin im Modell:** `EndDate` am Plan optional machen, oder der Stoffrahmen wird eine eigene, schlankere Größe und der `StudyPlan` entfällt mit dem Stichtag? Empfehlung: beim Schneiden der Karte entscheiden, weil es die Migration bestimmt.~~ → Entscheidung 17.
14. ~~**Fehlerart: nachträglich oder beim Antworten?** Empfehlung: beim Antworten bestimmen und speichern (stabil gegen spätere Regeländerungen); die Altdaten entfallen durch den Reset ohnehin.~~ → Entscheidung 18.
15. ~~**Ein Tag ganz ohne Server** zählt wie eine Pause und wird dem Vater gemeldet (Vorschlag aus Entscheidung 9, noch nicht bestätigt).~~ → Entscheidung 19.
16. ~~**Begonnene Aufgaben sind nicht mehr streichbar** (Vorschlag aus Entscheidung 7, noch nicht bestätigt).~~ → Entscheidung 20.

<!-- markdownlint-enable MD029 -->

## Verlauf

- **2026-11-17** — angelegt aus dem Wunsch des Vaters im Chat, mit Entwurf, Etappen und offenen Punkten.
  Ist-Stand nur als Teilbefund; `unverifiziert` bleibt bis zum Ausformulieren.
- **2026-11-17** — **Grill-Runde mit dem Vater im Dialog:** 14 Entscheidungen (siehe `## Entscheidungen`), alle zehn offenen Punkte erledigt. B-618, B-619, B-621 und B-622 angehalten (`wartet_auf`). Die Stufe bleibt `idee`: Der Ist-Stand ist noch nicht Zeile für Zeile belegt, und mehrere Entscheidungen nennen Prüfpunkte am Code (freier Sonntag in Serie und Malus, Fehlerart je Übungstyp, Münz-Verlauf nach dem Reset, UTC-Umstellung). Nächster Schritt: ausformulieren, dann als Karte schneiden.
- **2026-11-17** — `idee` → `ausformuliert`: Ist-Stand am Code belegt (Stand `36185a56`). Kern: Die Daten für Auswertung und Reset liegen vollständig vor, das Kassenbuch hat keinen Fremdschlüssel auf Positionen; es fehlen Ortszeit (35 UTC-Stellen ohne gemeinsame Funktion), Lerntage (Serie und Malus kennen keinen freien Sonntag und keine Pause), ein Zeitgeber und die Fehlerart. Etappe E0 neu, Etappentabelle an Entscheidung 11 und 14 angepasst, sechs neue offene Punkte (11–16).
- **2026-11-20** — `ausformuliert` → `gegrillt` (Vorbereitungslauf, Sonnet): Die 14 Entscheidungen des Vaters unangetastet; die sechs neuen offenen Punkte (11 bis 16) stellvertretend auf Anweisung des Nutzers entschieden, unbestätigt (Entscheidungen 15 bis 20), dazu die Rahmenentscheidungen A, B, D, E, G aus dem Cluster Leitbild Privatlehrer (21 bis 25, führend B-681) und der Zuschnitt (26). Eintrittsbedingung `gegrillt` erfüllt: kein Punkt offen, Akzeptanzkriterien final mit je einer roten Änderung. Prüfpunkte für das Schätzen: Ort der Akzentregel B-624, die sieben `EndDate`-Lesestellen, Wortlisten je Buchabschnitt.
