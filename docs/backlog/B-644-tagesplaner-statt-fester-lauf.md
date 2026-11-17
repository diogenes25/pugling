---
tags: [typ/story, status/idee, bereich/lehrplan, bereich/training, bereich/auswertung, rolle/supervisor, rolle/student, rolle/creator]
aliases: [Tagesplaner, Tagesauswertung, Leistungsprofil, adaptiver Tagesplan]
status: idee
prio: P1
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Vater im Chat 2026-11-17: statt eines fest geplanten Laufs je Kapitel nur noch Tages- und Bonusaufgaben, jeden Morgen um 6 Uhr aus der Auswertung der Vortage, dem Leistungsprofil und dem Stundenplan erzeugt"
unverifiziert: true
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

Dies ist ein **Entwurf auf Stufe `idee`**. Die Belege unten stammen zum Teil aus B-615 (dort am 2026-11-17
gemessen) und sind beim Ausformulieren neu zu prüfen. Das Vorhaben passt nicht in eine Sitzung und sollte
nach dem Grillen als Karte (`/wayfinder`) laufen.

## User Story

Als *Vater* möchte ich, dass Pugling jeden Morgen selbst entscheidet, was jedes Kind heute lernen muss,
damit die Pflicht zum tatsächlichen Lernstand passt und ich keinen Lauf mehr von Hand austariere. Als
*Vater* möchte ich außerdem je Tag nachlesen, was gelernt wurde und wo es Probleme gab. Als *Sohn* möchte
ich Aufgaben, die zu dem passen, was ich gestern konnte und was morgen in der Schule dran ist.

## Ist-Stand am Code (Teilbefund, 2026-11-17)

- **Die Pflicht hängt an der Position und ist statisch.** `PlanPosition` trägt `NewItemsPerDay`, `Cadence`,
  `GoalThreshold`, `PenaltyCoins` (`backend/Pugling.Api/Models/PlanPositionEntities.cs:40`, `:51`, `:75`, `:91`).
  Malus und Belohnung werden je Position und Periode gebucht (`PositionGoalReward` `:168`,
  `PositionGoalPenalty` `:191`).
- **Die Rohdaten für eine Auswertung sind da:** `ItemReviewEvent`, `ReviewEvent`, `PracticeSession`,
  `TestAttempt`/`TestItemResult`, `ItemProgress` (plan-übergreifend je Vokabel) und `PositionItemProgress`.
  Eine **gespeicherte Tagesauswertung** oder ein **Leistungsprofil je Fach** gibt es nicht (laut B-615:
  Schwäche existiert nur je Wort, Schwelle 50 %).
- **Eine Tagessicht über alle aktiven Pläne gibt es schon** (B-606, `PositionProgressService.ComputeChildDayAsync`);
  ein aktiver Plan je Kind und Fach (B-539).
- **Es gibt keinen Hintergrunddienst.** Im Backend existiert kein `BackgroundService`/`IHostedService`; der
  Malus wird träge beim nächsten Zugriff abgerechnet. Ein „6-Uhr-Lauf" wäre das erste zeitgesteuerte Stück.
- **Der Stundenplan ist gespeichert, aber wirkungslos** (`TimetableEntry`, kein Service liest ihn; laut B-615
  ist für kein Kind ein Eintrag gepflegt).
- **Kein LLM im Backend** (bewusste Entscheidung); der KI-Creator ist ein externer Konsolen-Agent und kann
  mit `--use-weak` schon Übungen aus schwachen Wörtern bauen (B-21).

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

## Etappen (Vorschlag)

| Etappe | Inhalt | Wirkung auf die Kinder |
| --- | --- | --- |
| E1 | Tagesauswertung und Leistungsprofil, Vater-Ansicht mit Verlauf | keine, nur lesen |
| E2 | Planer im **Schattenmodus**: erzeugt täglich einen Plan, der Vater sieht ihn neben dem echten | keine; ein bis zwei Wochen an Theodor und Friedrich kalibrieren |
| E3 | Planer scharf, **je Kind einschaltbar**: Tagesaufgaben tragen Pflicht und Malus, alte Pläne laufen weiter | Pflicht kommt aus dem Planer |
| E4 | Bonusaufgaben, Stundenplan- und Klassenarbeits-Regeln, Sohn-App zeigt die Begründung | voll |
| E5 | Lehrer liefert nur noch den Stoffrahmen; KI-Creator baut nachts Förder-Übungen | Inhalt wächst mit |
| E6 | Feste Pflicht an der Position entfernen (das „Ersetzen") | altes Modell weg |

Jede Etappe bringt eine anhängende Migration; E1 und E2 ändern am Verhalten für die Söhne nichts.

## Berührte Stories

Der Planer macht aus mehreren geplanten Einzelmechaniken **Regeln eines Mechanismus**:
B-618/B-619 (Fächer-Verteilung, `geschaetzt`/`gegrillt`), B-621/B-622 (Stundenplan-Pflicht, `geschaetzt`),
B-617 (Verteilung vorschlagen), B-248 (Klassenarbeit verdichtet den Plan), B-218 (Mischwiederholung),
B-220 (Learning Queue), B-536 (Aufbau-Übungen). **B-618, B-621 und B-622 stehen kurz vor dem Bau** – sie
würden die Pflicht an einer Stelle verankern, die dieser Umbau wieder auflöst. Empfehlung: ihren Bau
anhalten, bis diese Story gegrillt ist; ihre Entscheidungen (Prozent-Gewichte, Fachtag und Vortag) werden
Eingaben des Planers.

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

## Verlauf

- **2026-11-17** — angelegt aus dem Wunsch des Vaters im Chat, mit Entwurf, Etappen und offenen Punkten.
  Ist-Stand nur als Teilbefund; `unverifiziert` bleibt bis zum Ausformulieren.
- **2026-11-17** — **Grill-Runde mit dem Vater im Dialog:** 14 Entscheidungen (siehe `## Entscheidungen`), alle zehn offenen Punkte erledigt. B-618, B-619, B-621 und B-622 angehalten (`wartet_auf`). Die Stufe bleibt `idee`: Der Ist-Stand ist noch nicht Zeile für Zeile belegt, und mehrere Entscheidungen nennen Prüfpunkte am Code (freier Sonntag in Serie und Malus, Fehlerart je Übungstyp, Münz-Verlauf nach dem Reset, UTC-Umstellung). Nächster Schritt: ausformulieren, dann als Karte schneiden.
