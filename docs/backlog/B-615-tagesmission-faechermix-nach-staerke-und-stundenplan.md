---
tags: [typ/story, status/ausformuliert, bereich/lehrplan, bereich/training, rolle/supervisor, rolle/student, rolle/creator]
aliases: [Fächermix, Tagesmission über Fächer, Fach-Förderstufe, Stundenplan als Pflicht]
status: ausformuliert
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

- [B-250](B-250-stundenplan-in-der-tagesmission.md) (`idee`, P3): Der Stundenplan gewichtet die Tagesmission.
  **Entscheidung des Nutzers vom 2026-10-23 (G-7):** Der Stundenplan bewirkt **nur Reihenfolge und
  Hervorhebung**, an der Pflicht ändert er nichts. Der heutige Wunsch („am Fachtag und am Vortag **muss** das
  Kind Übungen machen“) **kehrt diese Entscheidung um**. Siehe offener Punkt 4.
- [B-248](B-248-klassenarbeit-countdown-treibt-den-plan.md) (`idee`, P2): Vor einer Klassenarbeit entsteht eine
  zusätzliche Pflicht mit Malus. Das ist dieselbe Mechanik wie „Pflicht an bestimmten Tagen“, nur an einem
  Termin statt an einem Wochentag.
- [B-220](B-220-learning-queue-im-sohn-dashboard.md) (`idee`): Learning Queue im Sohn-Dashboard, die B-250 als
  Ort der Sortierung vorsieht.
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

## Entwurf der Akzeptanzkriterien

Vorläufig. Sie hängen an den Entscheidungen zu den offenen Punkten.

1. Der Vater setzt je Kind und Fach eine Förderstufe (Vorschlag: `schwach` / `normal` / `stark`). Sie ist per
   API lesbar und änderbar und im Vater-Web am Kind sichtbar.
2. Die Förderstufe wirkt auf die **Tagesdosis** des Fachs, zum Beispiel über einen Faktor auf `NewItemsPerDay`
   und darauf, wie viele Positionen des Fachs täglich Pflicht sind. Ein Regressionstest zeigt bei gleichem Plan
   für `schwach` mehr neue Wörter pro Tag als für `stark`.
3. Eine Position kann die Taktung „Stundenplan“ tragen: Pflicht nur an den Wochentagen, an denen das Fach im
   Stundenplan des Kindes steht, und am Tag davor. An anderen Tagen keine Pflicht und kein Malus.
   Regressionstest: Fach am Dienstag ⇒ Pflicht Montag und Dienstag, Mittwoch kein Malus.
4. Kein Stundenplan-Eintrag für das Fach ⇒ die Position verhält sich wie `Daily` (oder wird abgewiesen,
   je nach Entscheidung), aber nie lautlos pflichtfrei.
5. Die Kind-Tagessicht (`ComputeChildDayAsync`) zeigt die Pflichten nach Fach gruppiert, mit dem Hinweis
   „morgen Englisch“ für Stundenplan-Pflichten.

## Offene Punkte

Jeweils mit meiner Empfehlung.

1. **Teilen?** Die Idee hat drei unabhängige Teile: (a) Inhalt und Pläne für die weiteren Fächer,
   (b) Förderstufe je Fach, (c) Stundenplan-Pflicht. *Empfehlung:* teilen. (a) ist sofort machbar und bringt
   den Mix schon morgen. (b) und (c) werden je eine Story, (c) zusammen mit B-250 und B-248.
2. **Welche Fächer zuerst?** *Empfehlung:* Friedrich Englisch (zweite Baustelle, Lehrwerk fotografieren und
   mit `lehrwerk-aufbereiten` laden). Mathe passt schlecht zu den heutigen Übungstypen, die auf Sprache
   zugeschnitten sind. Für Theodor erst das Fach Latein anlegen.
3. **Wo lebt die Stärke?** Am Kind je Fach (neue Tabelle `ChildSubject` o. ä.) oder am Plan (der Plan eines
   schwachen Fachs bekommt mehr Dosis)? *Empfehlung:* am Kind je Fach. Sie überdauert den Wechsel der Unit,
   und der KI-Creator kann sie beim Erzeugen eines Plans lesen.
4. **Stundenplan als Pflicht oder nur als Reihenfolge?** Das kehrt die Entscheidung G-7 aus B-250 um.
   *Empfehlung:* als Pflicht, wie jetzt gewünscht. Dann wird B-250 durch diese Story ersetzt
   (`verworfen` mit `ersetzt_durch`).
5. **Was heißt „Pflicht am Fachtag und am Vortag“?** Eine eigene Taktung an einer Position, oder ein Zuschlag
   (zum Beispiel doppelte Dosis) auf eine ohnehin tägliche Position? *Empfehlung:* eigene Taktung. Sie lässt
   sich mit eigener Periode ehrlich abrechnen, und `Daily` bleibt wie es ist.
6. **Fach eines guten Kindes:** Reicht dort eine Wiederholung der fälligen Leitner-Karten ohne neue Wörter
   („auf dem Laufenden bleiben“)? *Empfehlung:* ja, `stark` heißt: nur fällige Wiederholungen sind Pflicht,
   neue Wörter sind Kür.
7. **Obergrenze pro Tag:** feste Zahl Pflicht-Positionen (zum Beispiel höchstens 5) oder eine Zeitschätzung
   (zum Beispiel 20 Minuten)? *Empfehlung:* Zahl der Positionen, weil sie sich ohne Schätzmodell prüfen lässt.
8. **Woher kommt die Stärke?** Nur vom Vater gesetzt, oder später auch aus Noten (Klassenarbeiten tragen schon
   Noten) und Trefferquoten vorgeschlagen? *Empfehlung:* zuerst nur vom Vater. Ein Vorschlag aus den Daten
   wird eine Folge-Story.

## Verlauf

- **2026-11-17** — angelegt aus dem Chat mit dem Vater.
- **2026-11-17** — ausformuliert gegen `main` `30711266`: Mehrfach-Pläne und Tagessicht sind seit B-539/B-606
  gebaut. Die Lücken sind Inhalt, Stärke je Fach und Stundenplan-Pflicht. Widerspruch zur Entscheidung G-7 in
  B-250 als offener Punkt 4 aufgenommen.
- **2026-11-17** — Id von B-611 auf B-615 geändert (B-611 war gleichzeitig von einer parallelen Sitzung vergeben).
