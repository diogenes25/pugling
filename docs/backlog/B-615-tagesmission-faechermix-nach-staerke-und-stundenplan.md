---
tags: [typ/story, status/gegrillt, bereich/lehrplan, bereich/training, rolle/supervisor, rolle/student, rolle/creator]
aliases: [Fächermix, Tagesmission über Fächer, Fach-Förderstufe, Stundenplan als Pflicht]
status: gegrillt
prio: P1
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Vater im Chat 2026-11-17: Tagesmission braucht einen gesunden Mix aus mehreren Fächern, gewichtet nach Schwäche, mit dem Stundenplan (Fachtag und Vortag)"
grund: ""
ersetzt_durch: []
---

# B-615 · Die Tagesmission mischt die Fächer – gewichtet nach Stärke und Stundenplan

Beobachtung des Vaters: Friedrich lernt in Pugling nur Französisch, Theodor nur Englisch. Jeder Plan
trägt genau ein Fach, weil ihn der Fachlehrer (Creator-Profil) dieses Fachs erzeugt hat. Eine Tagesmission
soll aber einen **gesunden Mix aus mehreren Fächern** enthalten. Wie viel ein Fach bekommt, richtet sich
danach, **wie stark oder schwach** das Kind darin ist: In einem schwachen Fach wird verstärkt geübt, in
einem guten nur so viel, dass das Kind auf dem Laufenden bleibt. Dazu kommt der **Stundenplan**: Am Tag,
an dem das Fach in der Schule dran ist, und am Tag davor (zur Vorbereitung) soll das Kind Übungen zu diesem
Fach machen **müssen**.

## User Story

Als *Vater* möchte ich je Kind festlegen, in welchen Fächern es gerade schwach, normal oder gut ist, und
seinen Stundenplan hinterlegen, damit die tägliche Pflicht aus mehreren Fächern besteht, schwache Fächer
mehr Gewicht bekommen und vor einer Schulstunde das passende Fach geübt wird, ohne dass ich jeden Plan von
Hand austariere. Als *Sohn* möchte ich eine Tagesmission aus mehreren Fächern sehen, die zu meinem
Schultag passt, damit ich nicht immer nur dasselbe Fach übe.

## Ist-Stand am Code

Gemessen am 2026-11-17 auf `main` (Stand `30711266`), Pfade relativ zu `backend/Pugling.Api/`. Der größte
Teil des Fundaments ist **seit heute Nacht gebaut**.

### Mehrere Fächer gleichzeitig: gebaut (B-539, B-606)

- **Ein aktiver Plan je Fach statt je Kind** ([B-539](B-539-ein-aktiver-plan-je-fach.md), abgenommen,
  Commit `30711266`): Aktivieren schaltet nur die Pläne **desselben Fachs** ab
  (`Controllers/Supervisor/StudyPlansController.cs:124`, `:159` `DeactivateSameSubjectPlansAsync`, `:217`).
  Ein Englisch- und ein Französisch-Plan können also gleichzeitig laufen.
- **Eine Tagessicht über alle aktiven Pläne** ([B-606](B-606-kind-tagessicht-ueber-alle-plaene.md),
  abgenommen, Commit `14fd6ae6`): `Services/Shared/PositionProgressService.cs:295` `ComputeChildDayAsync`
  vereinigt alle aktiven, laufenden Pläne (`:297-298`). Pflicht, Tagesbox (`Services/Shared/DailyBoxService.cs:37`),
  Bonus-Sperre (`Services/Student/ExtraTrainingService.cs:261`), Kind-Tagesendpunkt
  (`Controllers/Student/ChildTodayController.cs:32`) und Vater-Dashboard
  (`Services/Supervisor/ChildrenDashboardService.cs:30`) lesen alle diese eine Quelle.

**Folge:** Der Fächermix ist technisch schon möglich. Es fehlt nur der zweite Plan.

### Warum die Kinder trotzdem nur ein Fach haben: Inhalt und Werkzeug

In der echten DB (read-only, 2026-11-17):

| Kind | aktive Pläne | Lehrwerke am Kind |
| --- | --- | --- |
| Theodor (Child 1) | Plan 5, Englisch | Access G9 Band 3 (Englisch) |
| Friedrich (Child 3) | Plan 8, Französisch | Découvertes Série bleue 2 (Französisch) |

- Es gibt die Fächer Englisch, Mathe, Erdkunde und Französisch, aber **kein Latein** (Theodors zweites Fach).
- Echte Buch-Übungen gibt es nur für Access (19) und Découvertes Série bleue 2 (34). Mathe und Erdkunde
  haben je zwei Seed-Übungen. Für Friedrichs Englisch und Mathe gibt es **kein Lehrwerk** am Kind
  (`Models/AdminEntities.cs:148`, `Textbook.SubjectId` `:159`) und keine passenden Übungen.
- Ein Plan entsteht je Fachlehrer: Das Creator-Profil-Matching wählt **ein** Profil je Kind und Fach
  (`Services/Creator/CreatorProfileService.cs`, Punkte Reihe 8 > Fach 4 …). Einen Schritt „Pläne für alle
  Fächer des Kindes“ gibt es nicht.

### Stärke je Fach: nicht vorhanden

- `Child` (`Models/AdminEntities.cs:61`) trägt Klasse, Schulart, Interessen und `ProfileNotes`, aber **keine
  Stärke oder Schwäche je Fach**. Die Reihenfolge „Französisch > Englisch > Mathe“ steht bisher nur in der
  Notiz des Vaters.
- Schwäche gibt es nur **je Wort** (`onlyWeak`, Schwelle 50 %, `ItemProgress.WeakBelowPercent`) und als
  Objective-Kennzahl (`Models/ObjectiveEntities.cs:69`, `MaxWeakItems`), nicht je Fach.
- Wie viel ein Fach täglich verlangt, stellt der Vater heute **je Position** ein: Zahl der Tages-Positionen,
  `NewItemsPerDay`, `Cadence`, Ziel und Malus. Ein Regler je Fach existiert nicht.

### Stundenplan: gespeichert, aber ohne Wirkung

- `TimetableEntry` (`Models/TimetableEntities.cs:7`: Kind, Fach, Wochentag, optional Tageszeit) mit
  Vater-CRUD unter `supervisor/children/{childId}/timetable` (`Controllers/Supervisor/TimetableController.cs:14`,
  `:28`, `:40`, `:59`).
- **Kein Service liest ihn**. Außerhalb des eigenen Controllers wird er nur in `SubjectsController` (Sperre
  beim Fach-Löschen) und im `DbContext` referenziert. In der echten DB ist **für kein Kind** ein Eintrag gepflegt.
- Die Pflicht kennt nur die Taktungen `None`, `Daily`, `Weekly`
  (`../Pugling.Contracts/Common/PlanPositionBaseTypes.cs:4`). Die Malus-Abrechnung zerlegt die Zeit nach diesen
  Taktungen in geschlossene Perioden (`Services/Shared/PositionProgressService.cs:490` `ClosedPeriods`, Aufruf `:589`).

### Verwandte Stories und eine frühere Entscheidung, die dem widerspricht

- [B-250](B-250-stundenplan-in-der-tagesmission.md) (seit 2026-11-17 `verworfen`, ersetzt durch diese Story;
  bei der Messung noch `idee`, P3): Der Stundenplan gewichtet die Tagesmission.
  **Entscheidung des Nutzers vom 2026-10-23 (G-7):** Der Stundenplan bewirkt **nur Reihenfolge und
  Hervorhebung**, an der Pflicht ändert er nichts. Der heutige Wunsch („am Fachtag und am Vortag **muss** das
  Kind Übungen machen“) **kehrt diese Entscheidung um**. Siehe Entscheidung 5.
- [B-248](B-248-klassenarbeit-countdown-treibt-den-plan.md) (`idee`, P2): Vor einer Klassenarbeit entsteht eine
  zusätzliche Pflicht mit Malus. Das ist dieselbe Mechanik wie „Pflicht an bestimmten Tagen“, nur an einem
  Termin statt an einem Wochentag.
- [B-220](B-220-learning-queue-im-sohn-dashboard.md) (`idee`): Learning Queue im Sohn-Dashboard, die B-250 als
  Ort der Sortierung vorsah. Mit B-250 ist dieser Mitnutzer entfallen; ob die Queue trotzdem nach Stundenplan
  sortiert, beantwortet Entscheidung 13 (nein).
- [B-536](B-536-aufbau-uebungen-statt-versagen.md) (`gegrillt`): Aufbau-Übungen. Ein schwaches Fach braucht
  vor allem *leichtere* Übungen, nicht nur *mehr*.

## Die echte Lücke

1. **Inhalt und ein Schritt für den zweiten Plan.** Der Mix scheitert nicht an der Mechanik, sondern daran,
   dass für Friedrich (Englisch, Mathe) und Theodor (Latein) weder Lehrwerk noch Übungen noch Plan existieren.
   Das ist zuerst Creator-Arbeit (Lehrwerk aufbereiten, `ki-creator plan`), keine Code-Arbeit.
2. **Eine Stärke je Fach am Kind**, die der Vater pflegt und die die **Dosis** eines Fachs vorgibt, statt sie
   je Position von Hand einzustellen.
3. **Eine Pflicht, die dem Stundenplan folgt:** eine Taktung „an Stundenplan-Tagen und am Vortag“ neben
   `Daily`/`Weekly`, mit eigener Malus-Periode.
4. **Ein Gesamtrahmen pro Tag.** Mit mehreren Fächern addieren sich die Pflichten. Ohne Obergrenze wird die
   Tagesmission zu lang, und an Tagen mit drei Fächern im Stundenplan stapelt sich alles.

## Entscheidungen

Gegrillt im Chat am 2026-11-17 mit dem Vater, neun Fragen, jeweils die Empfehlung gewählt. Die Entscheidungen
9 bis 14 stammen aus dem Nachgrillen am selben Tag zu den sechs Lücken, die die Review der Grill-Fassung
gefunden hat, ebenfalls jeweils die Empfehlung.

1. **Dreiteilung.** (a) Inhalt und Plan für ein weiteres Fach, (b) Stärke je Fach, (c) Pflicht nach Stundenplan.
   Sie werden getrennt gebaut. *Begründung:* (a) bringt den Fächermix sofort und ohne Code, weil die Mechanik seit
   B-539/B-606 steht. *Kosten:* drei Stories, deren Wechselwirkung (Dosis × Taktung × Obergrenze) beim Schätzen von
   (b) und (c) abgestimmt werden muss. Die Teil-Stories legt der Schritt `geschaetzt` an, nicht diese Bearbeitung.
2. **Als Nächstes Friedrich Englisch.** *Begründung:* seine zweite Baustelle, und die Sprach-Übungstypen passen.
   Weg: Englischbuch fotografieren, `lehrwerk-aufbereiten`, dann ein Englisch-Plan neben Plan 8. *Kosten:* der
   Vater fotografiert. Latein für Theodor und Mathe für Friedrich folgen später (Mathe fehlen passende Übungstypen).
3. **Die Stärke lebt am Kind je Fach.** *Begründung:* Sie überdauert Unit- und Planwechsel, und der KI-Creator
   kann sie beim Erzeugen eines Plans lesen. *Kosten:* eine neue Tabelle (Kind × Fach) mit Migration an die Kette.
4. **Abgelöst am 2026-11-17 durch [B-616](B-616-faecher-gewichtung-in-prozent.md) Entscheidung 2 (Prozente statt Stufen).** Ursprünglich:
   **Wirkung der Stärke** in drei Stufen: `schwach` erhöht die Tagesdosis neuer Wörter (Richtwert ×1,5),
   `normal` lässt die eingestellte Dosis stehen, `stark` macht nur die **fälligen Wiederholungen** zur Pflicht,
   neue Wörter sind dann Kür. *Begründung:* Ein gutes Fach soll nur „auf dem Laufenden“ halten, ein schwaches
   mehr Stoff bekommen. *Kosten:* Die Pflicht-Rechnung je Position muss die Stärke des Fachs kennen, und die
   Fachzuordnung einer Position läuft über den Plan. Seit B-539 **kann** ein Plan ein Fach tragen,
   `StudyPlan.SubjectId` ist aber nullable (`Models/StudyPlanEntities.cs:83`); den Plan ohne Fach
   regelt Entscheidung 9, den Tag ohne fällige Karten und die Nicht-Vokabel-Position Entscheidung 10.
5. **Der Stundenplan erzeugt eine Pflicht mit Malus.** Das **kehrt die Entscheidung G-7 aus B-250 um**
   (2026-10-23: nur Reihenfolge und Hervorhebung). B-250 wird `verworfen` mit `ersetzt_durch: [B-615]`.
   *Begründung:* Die Kernidee der App ist, dass der Vater Lernen erzwingt; die Vorbereitung auf die Schulstunde
   ist genau so eine Pflicht. *Kosten:* eine weitere Pflichtquelle, die den Tag verlängert (siehe Entscheidung 7)
   und Malus kosten kann.
6. **Eigene Taktung „Stundenplan“** an der Position, neben `None`/`Daily`/`Weekly`: Pflicht am Wochentag des
   Fachs laut Stundenplan und am Tag davor, an den übrigen Tagen bleibt die Position spielbar ohne Pflicht.
   *Begründung:* eigene Periode, ehrlich abrechenbar, `Daily` bleibt unverändert. *Kosten:* neuer Enum-Wert im
   Vertrag (Vertragsbruch für Clients, die die Liste fest kennen), eigene Perioden in `ClosedPeriods`, und die
   Malus-Abrechnung muss den Stundenplan lesen.
   **Nachfrage:** Steht das Fach nicht im Stundenplan des Kindes, **weist der Server die Taktung ab**. Wird der
   Eintrag später gelöscht, gilt die Position wie `Daily`, damit sie nie lautlos pflichtfrei wird. *Kosten:*
   eine Querprüfung beim Anlegen und eine Rückfallregel bei der Abrechnung. Den Schnitt der Perioden
   regelt Entscheidung 11.
7. **Obergrenze: höchstens N Pflicht-Positionen pro Tag**, je Kind vom Vater gesetzt (Vorschlag 5). Der Server
   weist beim Anlegen oder Ändern ab, was die Grenze an **irgendeinem** Wochentag überschreiten würde (wegen der
   Stundenplan-Taktung ist die Zahl je Wochentag verschieden). *Begründung:* ohne Schätzmodell prüfbar.
   *Kosten:* Prüfung über alle aktiven Pläne des Kindes und alle sieben Wochentage, und ein Stundenplan-Eintrag,
   der die Grenze nachträglich reißt, braucht eine eigene Regel (Entscheidung 12).
8. **Die Stärke setzt zuerst nur der Vater.** Ein Vorschlag aus Klassenarbeitsnoten und Trefferquoten wird eine
   Folge-Story. *Begründung:* Teil (b) bleibt klein. *Kosten:* Der Vater muss die Stärke selbst pflegen.
   Kandidat für die Folge-Story ist [B-616](B-616-faecher-gewichtung-in-prozent.md) (Prozent-Gewichtung, noch
   `idee`). Sie konkurriert mit den drei Stufen aus Entscheidung 4 um dieselbe Tabelle Kind × Fach; die
   Reihenfolge regelt Entscheidung 14.
9. **Ein Plan ohne Fach rechnet mit Stärke `normal`, und die Taktung „Stundenplan“ wird dort abgewiesen.**
   *Begründung:* kein Umbau am Bestand, und die Abweisung ist dieselbe wie beim Fach ohne Stundenplan-Eintrag.
   *Kosten:* Ein Plan ohne Fach nimmt an Stärke und Stundenplan nicht teil; der Vater muss dem Plan ein Fach
   geben, um beides zu nutzen. Eine zweite Fachquelle (Fach der Übung) wird bewusst nicht eingeführt.
10. **Abgelöst am 2026-11-17 durch [B-616](B-616-faecher-gewichtung-in-prozent.md) Entscheidung 2**, die dieselbe Regel für ein Fach mit
    0 % übernimmt. Ursprünglich:
    **`stark` ohne fällige Karten gilt als erfüllt; Positionen ohne Tagesdosis berührt die Stärke nicht.** Die
    erfüllte Pflicht wird als erfüllt angezeigt, kostet keinen Malus und zählt für Truhe und Serie.
    *Begründung:* „auf dem Laufenden“ heißt an einem Tag ohne Fälliges: nichts zu tun. *Kosten:* An solchen
    Tagen bekommt das Kind die Truhe für dieses Fach ohne Arbeit, und Grammatik-Positionen eines schwachen Fachs
    bekommen nicht mehr Stoff (das bleibt bei B-536).
11. **Je Pflichttag eine eigene Tagesperiode wie bei `Daily`.** Vortag verpasst kostet Malus, Fachtag verpasst
    kostet noch einmal. Ein Tag, der zugleich Fachtag und Vortag ist, zählt einmal. Ein Sonntag als Vortag gilt;
    ein Vortag vor Planbeginn oder nach Planende ist keine Periode. *Begründung:* dieselbe Abrechnung wie
    `Daily`, nur an weniger Tagen; das Kind kann die Vorbereitung nicht auf den Fachtag schieben. *Kosten:* bis
    zu zwei Malus je Schulstunde, und ein Fach mit vielen Wochenstunden wird fast zu `Daily`.
12. **Ein Stundenplan-Eintrag, der die Obergrenze reißt, wird abgewiesen; das Löschen wird geduldet.** Hebt das
    Löschen (Rückfall auf `Daily`) einen Wochentag über die Grenze, zeigt das Vater-Web die Überschreitung an.
    *Begründung:* Der Vater muss einen falschen Stundenplan-Eintrag immer entfernen können. *Kosten:* Die Grenze
    ist nach einem Löschen nicht mehr hart, und der Stundenplan-Controller braucht dieselbe Prüfung wie die
    Position.
13. **Keine Sortierung nach Stundenplan.** Die Pflicht aus Entscheidung 5 mit dem Hinweis aus AK 8 genügt;
    B-220 sortiert nicht nach Stundenplan. *Begründung:* eine Wirkung des Stundenplans statt zwei. *Kosten:*
    Positionen des morgigen Fachs ohne Taktung „Stundenplan“ rücken nicht nach vorn.
14. **Teil (b) wird erst geschätzt, wenn [B-616](B-616-faecher-gewichtung-in-prozent.md) gegrillt ist.**
    *Begründung:* Drei Stufen und Prozent wollen dieselbe Tabelle; so braucht sie nur eine Migration.
    *Kosten:* Teil (b) wartet auf eine Grill-Runde; die Entscheidungen 3, 4, 8 und 10 können dort noch fallen.
    Teil (a) und (c) laufen unabhängig weiter. **Eingelöst am 2026-11-17:** B-616 ist gegrillt, die
    Prozente ersetzen die Stufen, und Teil (b) geht in B-616 auf. Entscheidung 3 (Tabelle Kind × Fach)
    und 8 (zuerst setzt nur der Vater) gelten dort weiter; für die Stärke ist Entscheidung 9 durch
    B-616 Entscheidung 12 abgelöst, für die Taktung „Stundenplan“ gilt sie unverändert.

## Akzeptanzkriterien

Final, nach Teilen. Je Fall die Änderung, die seinen Test rot macht. Teil (b) steht unter dem Vorbehalt
aus Entscheidung 14. Die Nummern 9 bis 14 stammen aus dem Nachgrillen und stehen bei ihrem Teil.

### Teil (a), Inhalt, ohne Code

- **AK 1.** **Absichtserklärung, kein Regressionstest** (es gibt keinen Code, den eine Änderung rot machen könnte):
  Friedrichs Englischbuch liegt als Reihe mit Units und Wortlisten in der echten DB, das Lehrwerk hängt am Kind,
  und ein aktiver Englisch-Plan läuft neben Plan 8. Abnahme durch einmaliges Nachsehen: `student/today` für
  Child 3 zeigt Pflichten aus beiden Fächern.

### Teil (b), Stärke je Fach

**Abgelöst am 2026-11-17:** Teil (b) wird nicht hier gebaut, sondern als [B-616](B-616-faecher-gewichtung-in-prozent.md). Die AKs 2, 3, 4, 9, 10
und 11 gelten nicht mehr; ihre Nachfolger sind dort die AKs 1 bis 14. Sie bleiben als Beleg stehen.

- **AK 2.** Der Vater setzt je Kind und Fach `schwach`/`normal`/`stark`, per API lesbar und änderbar. Je Zusage ein Fall:
  - Ein Kind ohne gesetzte Stärke liest `normal`. *Rot ohne Vorgabe.*
  - Ein gesetzter Wert kommt beim nächsten Lesen zurück. *Rot, wenn das Schreiben nicht speichert.*
  - Ein fremdes Kind liefert 404. *Rot ohne Eigentumsprüfung.*
  - Das Vater-Web zeigt die Stärke am Kind und ändert sie. *Rot, wenn die Seite den Wert nicht rendert.*
- **AK 3.** Bei gleichem Plan führt `schwach` mehr neue Wörter pro Tag ein als `normal`. *Rot, wenn die Stärke nicht in die
  Tagesdosis eingeht.*
- **AK 4.** Bei `stark` ist die Tagespflicht mit den fälligen Wiederholungen erfüllt, ohne dass neue Wörter eingeführt
  werden. Neue Wörter bleiben spielbar. *Rot, wenn die Pflicht weiter neue Wörter verlangt.*
- **AK 9.** Bei `stark` und null fälligen Karten meldet die Tagessicht die Pflicht als erfüllt, und der Tagesabschluss
  bucht keinen Malus. *Rot, wenn die Pflicht offen bleibt oder Malus kostet.*
- **AK 10.** Eine Position ohne Tagesdosis (Grammatik) verlangt bei `schwach`, `normal` und `stark` dasselbe. *Rot, wenn
  die Stärke ihr Ziel verändert.*
- **AK 11.** Eine Position in einem Plan ohne Fach rechnet wie `normal`, auch wenn für ein Fach des Kindes `schwach`
  gesetzt ist. *Rot, wenn eine fremde Stärke durchschlägt.*

### Teil (c), Stundenplan-Pflicht

- **AK 5.** Fach am Dienstag ⇒ eine Position mit Taktung „Stundenplan“ ist Montag und Dienstag Pflicht; ein am Montag
  verpasstes Ziel kostet Malus, Mittwoch kostet nichts. *Rot ohne eigene Perioden.*
- **AK 6.** Taktung „Stundenplan“ für ein Fach ohne Stundenplan-Eintrag ⇒ `400 validation_error`. Wird der Eintrag danach
  gelöscht, rechnet die Position wie `Daily`. *Rot, wenn sie dann pflichtfrei wird.*
- **AK 7.** Überschreitet ein neuer Pflicht-Eintrag die Obergrenze an einem Wochentag ⇒ `400` mit eigenem Code
  (Vorschlag `daily_duty_limit_exceeded`, beim Schätzen festzulegen). Der Testfall reißt die Grenze nur an
  **einem** Wochentag, der nicht heute ist. *Rot ohne Prüfung über alle Wochentage.*
- **AK 8.** Die Kind-Tagessicht zeigt Stundenplan-Pflichten mit Hinweis auf die Stunde („morgen Englisch“). *Rot, wenn die
  Pflicht ohne Grund erscheint.*
- **AK 12.** Fach am Dienstag, Montag **und** Dienstag verpasst ⇒ zwei Malus-Buchungen. Fach am Montag **und** Dienstag,
  alles verpasst ⇒ drei Buchungen (Sonntag, Montag, Dienstag), nicht vier. *Rot bei einer gemeinsamen Periode
  bzw. wenn der Montag doppelt zählt.*
- **AK 13.** Taktung „Stundenplan“ an einer Position in einem Plan ohne Fach ⇒ `400 validation_error`. *Rot ohne die
  Prüfung auf das Plan-Fach.*
- **AK 14.** Ein neuer Stundenplan-Eintrag, der einen Wochentag über die Obergrenze hebt ⇒ `400` mit demselben Code wie
  AK 7. Das Löschen eines Eintrags mit derselben Folge geht durch, und die Vater-Sicht des Kindes meldet die
  Überschreitung. *Rot, wenn der Eintrag durchgeht bzw. das Löschen abgewiesen wird.*

## Offene Punkte

Alle entschieden: die Punkte 1 bis 8 in der Grill-Runde vom 2026-11-17, die Punkte 9 bis 14 (Funde der Review
desselben Tages) im Nachgrillen am selben Tag.

1. ~~**Teilen?**~~ → Entscheidung 1.
2. ~~**Welche Fächer zuerst?**~~ → Entscheidung 2.
3. ~~**Wo lebt die Stärke?**~~ → Entscheidung 3.
4. ~~**Stundenplan als Pflicht oder nur als Reihenfolge?**~~ → Entscheidung 5.
5. ~~**Was heißt „Pflicht am Fachtag und am Vortag“?**~~ → Entscheidung 6.
6. ~~**Fach eines guten Kindes**~~ → Entscheidung 4.
7. ~~**Obergrenze pro Tag**~~ → Entscheidung 7.
8. ~~**Woher kommt die Stärke?**~~ → Entscheidung 8.
9. ~~**Plan ohne Fach**~~ → Entscheidung 9.
10. ~~**`stark` ohne fällige Karten, und Nicht-Vokabel-Positionen**~~ → Entscheidung 10.
11. ~~**Periodenschnitt der Taktung „Stundenplan“**~~ → Entscheidung 11.
12. ~~**Obergrenze nachträglich gerissen**~~ → Entscheidung 12.
13. ~~**Reihenfolge nach Stundenplan**~~ → Entscheidung 13.
14. ~~**Drei Stufen oder Prozent**~~ → Entscheidung 14.

## Verlauf

- **2026-11-17** — angelegt aus dem Chat mit dem Vater.
- **2026-11-17** — ausformuliert gegen `main` `30711266`: Mehrfach-Pläne und Tagessicht sind seit B-539/B-606
  gebaut. Die Lücken sind Inhalt, Stärke je Fach und Stundenplan-Pflicht. Widerspruch zur Entscheidung G-7 in
  B-250 als offener Punkt 4 aufgenommen.
- **2026-11-17** — Id von B-611 auf B-615 geändert (B-611 war gleichzeitig von einer parallelen Sitzung vergeben).
- **2026-11-17** — gegrillt im Chat mit dem Vater: acht Entscheidungen plus eine Nachfrage, AKs final; G-7 aus B-250 umgekehrt, B-250 verworfen.
- **2026-11-17** — Review der Grill-Fassung: sechs Lücken als offene Punkte 9 bis 14 **ausdrücklich
  zurückgestellt** (Plan ohne Fach, `stark` ohne fällige Karten, Periodenschnitt, nachträglich gerissene
  Obergrenze, Reihenfolge nach Stundenplan, Verhältnis zu B-616), je mit Empfehlung, keine davon entschieden.
  Falsche Annahme „jeder Plan trägt ein Fach“ korrigiert, AK 1 als Absichtserklärung beschriftet, AK 2 in vier
  Fälle zerlegt, AK 7 mit Code-Vorschlag, veraltete Verweise auf B-250 nachgezogen.
- **2026-11-17** — Nachgrillen mit dem Vater: die Punkte 9 bis 14 sind als Entscheidungen 9 bis 14 entschieden
  (jeweils die Empfehlung), AKs 9 bis 14 ergänzt. Teil (b) wartet auf das Grillen von B-616 (Entscheidung 14).
- **2026-11-17** — [B-616](B-616-faecher-gewichtung-in-prozent.md) gegrillt: Die Prozent-Gewichtung ersetzt die
  drei Stufen. Entscheidungen 4 und 10 abgelöst, Teil (b) samt AKs 2, 3, 4, 9, 10 und 11 geht in B-616 auf.
  Diese Story trägt damit noch Teil (a) (Inhalt) und Teil (c) (Stundenplan-Pflicht).
- **2026-11-17** — B-616 ist beim Schätzen geteilt worden: [B-618](B-618-faecher-verteilung-backend.md) (Backend) und [B-619](B-619-faecher-verteilung-vater-web-und-sohn-app.md) (Vater-Web und Sohn-App). Wo diese Story auf B-616 verweist, tragen jetzt diese beiden die Arbeit; die Entscheidungen stehen unverändert in B-616 und in B-618.
