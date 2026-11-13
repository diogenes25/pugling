---
tags: [typ/plan, bereich/frontend, bereich/gamification, bereich/training, rolle/student, rolle/supervisor]
aliases: [Duolingo-Vergleich, Snipwise-Vergleich, Lernpfad, Lektion, Trainingsraum]
---

# Duolingo und Snipwise als Vorbild: Lernpfad, Lektion, freies Üben

Status: **Analyse, Stufe A entschieden** (2026-11-13). Die Pfad-Form ist gewählt (siehe „Entscheidung am
Prototyp"); der Bau läuft als [B-525](backlog/B-525-sohn-startseite-als-schlangenpfad.md) und
[B-526](backlog/B-526-lektionsbildschirm-festes-raster.md). Über Stufe B und C ist nicht entschieden.
Dieses Dokument hält den Vergleich und die Empfehlung, **nicht** den Zustand der Stories.

← [Zurück zur Wissenskarte](obsidian.md) · Frühere Vergleiche: [Khan Academy](khan-academy-vergleich-plan.md),
[Knowunity](knowunity-vergleich-plan.md), [Baobab](baobab-vergleich-plan.md) · Ist-Stand der Sohn-App:
[sohn-app-funktionsbeschreibung.md](sohn-app-funktionsbeschreibung.md)

## Warum dieser Vergleich

Beide Apps benutzen die Kinder bereits; Bedienung und Lernrhythmus sind ihnen **bekannt**. Jede Abweichung
davon muss Pugling sich verdienen. Anders als die früheren Vergleiche geht es hier nicht um einzelne
Funktionen, sondern um die **Grundform**: Wie sieht das Kind seinen Stoff, und was ist „eine Runde"?

Quellenstand: 14 Screenshots vom 2026-11-13 unter `raw/apps/` (gitignored) – 9 Snipwise, 5 Duolingo
(Schachkurs). Beschrieben ist nur, was darauf zu sehen ist; Mechaniken dahinter (etwa wie Duolingo
Wiederholungen plant) sind **nicht** belegt und unten als Annahme markiert.

## Was die Screenshots zeigen

### Snipwise

| Fläche | Beobachtung |
| --- | --- |
| **Pfad** | Ein Buch = ein Pfad. Kopfkarte mit Cover, Titel, Autor, „10 Kapitel", aufklappbar. Darunter nummerierte runde Knoten in einer Schlangenlinie. Drei Zustände: erledigt (heller Ring), aktuell (kräftig), gesperrt (grau). Der Ring um den Knoten ist **segmentiert** – Teilfortschritt im Kapitel. Am Ende ein Fahnen-Knoten als Ziel. |
| **Navigation** | Fünf Einträge: Home, Entdecken, **Spielen** (mittig, hervorgehoben), Missionen, Liga. |
| **Lektion** | Immer derselbe Aufbau: ✕ und Fortschrittsbalken (mit Rakete) oben, darunter die **Arbeitsanweisung** als Überschrift („Ja oder nein", „Wähle eine Option", „Vervollständige den Satz"), dann Maskottchen mit **Sprechblase** (die Frage), darunter die Antwortfläche, ganz unten **ein** Knopf. |
| **Zwei-Schritt-Antwort** | Auswählen → „Prüfen" (bis dahin grau) → von unten schiebt sich ein grünes Blatt „Stark!" mit „Weiter" herein; eine Glühbirne öffnet die Erklärung. Die gewählte Antwort färbt sich, die anderen verblassen. |
| **Lückensatz** | Wort-Chips; der gewählte Chip wandert in die Lücke, sein Platz bleibt als leeres Feld stehen. |
| **Abschluss** | „Stark gemacht!", drei Kacheln: Gesamt-XP, Genauigkeit (86), Zeit (1:24). Kennzeichnung „Wiederholung · halbe XP". |

Vier Aufgabenformen in einer Lektion von unter anderthalb Minuten: Ja/Nein, Wahr/Falsch, Vierer-Auswahl, Lückensatz.

### Duolingo

| Fläche | Beobachtung |
| --- | --- |
| **Pfad** | Abschnitts-Banner („Stufe 6, Abschnitt 5 – Doppelangriffe"). Knoten **verschiedener Art**: Stern (Lektion), Hantel (Übungsrunde), Turm (kursspezifisch), dazwischen eine **Truhe** als Belohnung auf dem Weg. Kommendes ist grau. Maskottchen steht neben dem Pfad. Links ein Kurswechsler („Fr") und ein laufendes Ereignis („10 Tage"). |
| **Kopfzeile** | Avatar, Kurs-Wertung, Streak (1121). |
| **Missionen** (eigener Tab) | Drei Ebenen mit Restzeit: **Monat** (70 Punkte, 17 Tage), **Wochenende zu zweit** (gemeinsam 40 perfekte Lektionen, Beitrag je Person sichtbar, „Anstupsen"/„Beschenken"), **Tag** (drei Stück: 2 Lektionen, 10 Minuten, eine kursspezifische), jede mit Truhe. |
| **Üben** (eigener Tab) | Abseits des Pfads: „Fehler", „Wörter", „Hörverstehen", „Sprechen"; dazu Sammlungen („Storys", „DuoRadio"). |
| **Liga** | Wochenrangliste nach XP mit Auf-/Abstieg. |

## Was beide gemeinsam haben – und Pugling nicht

1. **Der Stoff ist ein Weg, keine Liste.** Das Kind sieht, wo es steht, was hinter ihm liegt und was
   kommt. Pugling zeigt auf der Basis eine Kachelliste der Positionen und im Tab „Weg" Kennzahlen – der
   Tab heißt Weg, zeigt aber keinen.
2. **Es gibt genau eine nächste Sache.** Ein Knoten leuchtet, ein Tipp startet. In Pugling entscheidet
   das Kind je Kachel zwischen „Üben" und „Test" und ggf. per Dropdown zwischen Plänen.
3. **Die Spieleinheit ist die Lektion: kurz, gemischt, mit festem Ende.** Rund ein bis zwei Minuten, eine
   Handvoll Aufgaben, wechselnde Formen, danach ein Abschlussbild. Puglings Einheit ist die **Position**:
   eine Übung, ein Typ, „alle fälligen Karten". Länge und Abwechslung hängen davon ab, wie der Vater den
   Plan gebaut hat.
4. **Ein Lektionsbildschirm mit fester Geometrie.** Anweisung oben, Frage in der Mitte, ein Knopf unten,
   Rückmeldung als Blatt an derselben Stelle. Pugling meldet per Toast; die Stelle wechselt.
5. **Missionen sind ein Ort mit Uhr**, nicht ein Panel am Fuß der Startseite.

Nur Duolingo: **Üben außerhalb des Pfads**. In Pugling ist das Gegenteil Absicht – genau ein Plan ist
spielbar, damit sich das Kind nicht an der Pflicht vorbeiübt.

## Was Pugling hat und beide nicht

Das bleibt der Kern und wird durch nichts unten aufgeweicht:

- **Der Vater.** Pflicht mit Konsequenz (`PenaltyCoins`), echte Belohnungen (Familien-Shop mit
  Genehmigung), Schulstoff aus dem eigenen Buch statt Konserve.
- **Echtes Abrufen.** Die Screenshots zeigen fast nur Wiedererkennen (Auswahl, Chips). Pugling führt je
  Wort über Stufen bis zum freien Schreiben – das, was der Vokabeltest in der Schule verlangt
  ([B-524](backlog/B-524-vokabeltest-wie-in-der-schule.md)).
- **Leitner je Item**, plan-übergreifend. Wiederholung ist geplant, nicht dem Zufall überlassen.

Die Aufgabe ist also: **Duolingos Form, Puglings Inhalt und Strenge.**

## Die eigentliche Spannung: Pfad gegen Karteikasten

Ein Pfad ist linear („weiter"), Leitner ist zyklisch („wieder"). Wer nur den Pfad baut, verliert die
Wiederholung; wer den Karteikasten als Pfad verkleidet, lügt über den Fortschritt. Die Vorbilder lösen es
sichtbar so, und das passt auf unser Modell:

| Vorbild | Abbildung in Pugling |
| --- | --- |
| Abschnitts-Banner | `SeriesUnit` (bzw. Plan, wenn kein Lehrwerk dahintersteht) |
| Knoten | `PlanPosition` – eine Übung |
| Segmentierter Ring am Knoten | die Stufen der Position (`StageSchedule`: Kennenlernen → … → frei schreiben) bzw. Anteil eingeführter Inhalte (`introducedCount / poolSize`) |
| Abschluss-/Fahnenknoten | der Positions-Test; am Abschnittsende die `Klassenarbeit` mit Datum |
| Hantel-Knoten | **Wiederholung**: fällige Karten aus *allen* erledigten Knoten, als eigener, täglich neu erscheinender Knoten an der aktuellen Stelle |
| Truhe auf dem Weg | Belohnungsbox ([B-105](backlog/B-105-taegliche-belohnungsbox.md)) / Ziel-Etappe eines Objectives |

Der Pfad zeigt damit **Erstkontakt und Aufstieg**, der Hantel-Knoten trägt den **Karteikasten**. Beides
ist ehrlich, und das Kind kennt beide Symbole.

## Drei Ausbaustufen

### Stufe A – Neue Haut (nur Frontend, Vertrag bleibt)

- **Basis wird der Pfad.** Aus `overview.today.positions` und den Plan-Positionen gezeichnet: Banner,
  Knoten mit Ring, ein leuchtender nächster Knoten, Kommendes grau. Tipp auf den Knoten öffnet ein kleines
  Blatt mit *einem* Hauptknopf (der Server sagt per `testable`/Stufe, was dran ist), „Test" bleibt als
  Nebenweg erreichbar.
- **Pflicht sichtbar am Knoten:** heutige Pflichtknoten tragen eine Markierung und – wie Duolingos
  Missionen – die Restzeit bis Tagesende. Der Malus steht dabei („sonst −5 🪙").
- **Lektionsbildschirm nach festem Raster:** Anweisung als Überschrift (das heutige Stufen-Label wird
  zur Arbeitsanweisung), Frage in einer Sprechblase am Skin-Maskottchen, Antwortfläche, ein Knopf unten;
  Rückmeldung als Blatt mit „Weiter" statt Toast, Lösung und Tipp im Blatt.
- **Abschlussbild:** Münzen, Genauigkeit, Zeit.
- **Missionen als eigener Tab** mit Restzeit; „Konto" wandert hinter den HUD-Zähler. Navigation:
  Pfad · Missionen · Shop · Skins (+ später Training).

Aufwand: klein bis mittel. Risiko: gering. Trägt aber Punkt 3 nicht – eine Runde bleibt so lang, wie die
Position es will.

### Stufe B – Lektion und Trainingsraum (neue Server-Begriffe)

- **Lektion als serverseitige Einheit.** Der Server schneidet aus dem, was heute dran ist, eine Runde
  fester Größe (Vorschlag: 10–12 Karten) und **mischt** dabei: neue Inhalte der aktuellen Position plus
  fällige Wiederholungen aus früheren. Der eingefrorene Cursor (`Order`/`Cursor`) und die Tagesdosis
  (`NewItemsPerDay`) sind die vorhandenen Bausteine dafür; neu ist, dass eine Runde **über Positionen
  hinweg** greift und ein festes Ende hat.
- **Pflicht in Lektionen.** Statt je Position ein eigenes Tages-/Wochenziel sagt der Vater: „3 Lektionen
  am Tag, 15 in der Woche". Das versteht das Kind sofort (Duolingo: „Schließ 2 Lektionen ab"), und der
  Vater konfiguriert eine Zahl statt zwölf.
- **Trainingsraum (Duolingos „Üben").** Eigener Tab mit drei Einstiegen, alle aus vorhandenen Daten:
  - **Fehler** – Karten, die zuletzt falsch waren.
  - **Wörter** – der plan-übergreifende Vokabel-Lernstand, schwächste zuerst.
  - **Hören** – Karten mit Audio.

  **Anti-Cheat-Regel, die das gefahrlos macht:** Training erfüllt **nie** eine Pflicht, bewegt **keine**
  Leitner-Box nach oben und zahlt **keine Münzen** – höchstens Gems, gedeckelt je Tag. Das ist exakt die
  vorhandene Trennung der Währungen (Münzen = Lernleistung, Gems = Bonus) und entspricht Snipwise'
  „Wiederholung · halbe XP". Offen und zu entscheiden: ob ein *Fehler* im Training die Box senken darf
  (ehrlicher, aber bestraft freiwilliges Üben – Empfehlung: nein).
- **Missionen in drei Takten** wie bei Duolingo: Tag, Woche, Monat. Tag und Woche gibt es
  (`MissionPeriod`), der Monat fehlt.
- **Familien-Mission statt Liga.** Duolingos Wochenend-Mission zu zweit passt besser auf zwei Brüder
  verschiedenen Alters und verschiedener Fächer als eine Rangliste: gemeinsames Ziel, Beitrag je Person
  sichtbar, Truhe für beide. Zählbasis wären Lektionen – fächerneutral und damit fair. Eine **Liga
  empfehle ich nicht**: mit zwei Teilnehmern gibt es jede Woche einen Verlierer, und es ist immer
  derselbe Vergleich. Der vorhandene Ersatz ist die Leiter ([B-221](backlog/B-221-level-leiter-fuer-auszeichnungen.md)).

Aufwand: groß (neuer Sitzungs-Schnitt, Pflichtmodell, drei Endpunktgruppen, Frontend). Bricht den Vertrag
der Positions-Ziele.

### Stufe C – Der Pfad wird das Fachmodell

Der Vater baut keinen Plan mehr aus Positionen, sondern **weist einen Kurs zu**: „Friedrich, Lehrwerk X,
Unit 3 bis 5, bis zur Klassenarbeit am …, 3 Lektionen täglich". Der Server erzeugt den Pfad aus
`SeriesUnit → Exercise` selbst, verteilt den Stoff rückwärts vom Termin (das ist die Knowunity-Idee des
Klausur-Countdowns) und entscheidet in jeder Lektion, was dran ist. `StudyPlan`/`PlanPosition` würden zum
internen Zustand des Pfads oder entfielen.

- **Gewinn:** ein Modell statt zwei; der Vater steuert mit drei Angaben (Stoff, Termin, Tagesmenge);
  mehrere Kurse nebeneinander (Duolingos Kurswechsler „Fr") ersetzen die Ein-Plan-Sperre, weil die
  Pflicht je Kurs in Lektionen zählt.
- **Preis:** Der Vater verliert die Feinsteuerung je Übung (eigene Punkte, eigener Rhythmus, eigene
  Zeitfenster), sofern man sie nicht als „Erweitert" behält. Katalog-Inhalte müssen pfadtauglich sein –
  eine Unit braucht eine Reihenfolge und genug Übungen. Alles, was heute an `PlanPosition` hängt
  (Objectives, Missionen, Auswertung, Vater-Web), wird angefasst.

Da der bisherige Fortschritt der Kinder vernachlässigt werden darf, ist das **machbar**. Zu beachten: Die
Migrationskette wird laut Projektregel nur verlängert; C hieße entweder eine Migration, die Pläne bewusst
verwirft, oder die ausdrückliche Entscheidung, die Kette einmal neu zu beginnen.

## Empfehlung

**B als Ziel, über A hinein, C erst nach Messung.**

1. **Zuerst A, und zwar als Prototyp an den Kindern.** Die Annahme „bekannte Form = mehr Lust" ist die
   tragende und die billigste zu prüfen. Pfad-Basis und Lektionsraster lassen sich ohne Vertragsänderung
   bauen und bleiben in B und C vollständig gültig – nichts davon ist Wegwerfarbeit.
2. **Dann aus B die zwei Dinge, die am meisten ändern:** die **Lektion** fester Länge mit gemischter
   Wiederholung und der **Trainingsraum** mit der Gems-Regel. Die Lektion ist der eigentliche Unterschied
   zu den Vorbildern; der Trainingsraum ist der ausdrücklich genannte Wunsch und durch die Währungstrennung
   ohne neues Anti-Cheat-Risiko.
3. **Pflicht in Lektionen** und **Familien-Mission** danach.
4. **C nur, wenn** sich nach B zeigt, dass der Vater die Positions-Feinsteuerung nicht mehr anfasst. Dann
   ist sie Ballast und der Umbau eine Vereinfachung; vorher wäre er eine Wette.

Was ich **nicht** übernehmen würde: die Liga (siehe oben); die Verflachung auf Auswahlaufgaben (unsere
Stufen bis zum freien Schreiben sind der Lernwert); Herzen/Leben als Fehlerstrafe (auf den Screenshots
nicht zu sehen, aber als Duolingo-Mechanik bekannt – unser Stick ist die verpasste Pflicht, nicht der
Fehler beim Üben).

## Entscheidung am Prototyp

Frage: Welche Form bekommt die Pfad-Startseite? Drei Varianten lagen als Wegwerf-Prototyp unter
`frontend/src/prototyp/` (Route `/prototyp/pfad`, nur im Dev-Modus, erfundene Daten): **A Schlangenpfad**
(Knoten in Schlangenlinie mit Stufen-Ring, „START" am nächsten Knoten), **B Heute zuerst** (ein großer
Startknopf, Pfad als Zeitleiste), **C Level-Raster** (Kacheln je Abschnitt).

**Gewählt: A Schlangenpfad** (Nutzer, 2026-11-13). Es ist die Form, die die Kinder aus beiden Apps kennen.
Der Lektionsbildschirm (Anweisung, Sprechblase, Antwortfläche, ein Knopf, Rückmeldung als Blatt,
Abschlussbild) war in allen Varianten derselbe und stand nicht zur Wahl.

Nicht belegt: ein Durchgang der Kinder selbst am Handy. Der Prototyp ist nicht committet.

## Offene Entscheidungen

| Frage | Vorschlag |
| --- | --- |
| Lektionsgröße | 10–12 Karten, vom Vater je Kind einstellbar |
| Mischverhältnis neu : Wiederholung | Fälliges zuerst, Rest mit Neuem auffüllen bis zur Tagesdosis |
| Zahlt Training etwas? | Nur Gems, Tagesdeckel; nie Münzen, nie Pflicht |
| Senkt ein Trainingsfehler die Box? | Nein |
| Mehrere Kurse gleichzeitig spielbar? | In B nein (Sperre bleibt), in C ja |
| Positions-Test im Pfad | Abschlussknoten je Position; Klassenarbeit als Fahne am Abschnittsende |
| Kennen die Kinder Snipwise' Raumfahrt-Look als „ihren"? | Am Prototyp fragen – die Dark-Arcade-Richtung ist nah dran |
