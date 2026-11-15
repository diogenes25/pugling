---
tags: [typ/story, status/idee, bereich/training, bereich/katalog, rolle/student, rolle/supervisor, rolle/creator]
aliases: [Lehrwerk-Bezug, Buchseite zur Übung, schon durchgenommen]
status: idee
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: Nutzer im Gespräch zum sofatutor-Vergleich (2026-11-15)
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-588 · Eine Übung zeigt ihren Bezug zum Lehrwerk (Unit und Seite), und das Kind sieht, ob der Stoff schon durchgenommen ist

Zu jeder Übung soll ein Bezug zum Lehrwerk angezeigt werden können: welche Unit, welche Seite. Damit
kann das Kind (oder der Vater) bei einer Übung **im Buch nachsehen**, und das Kind erkennt, dass es die
Übung **eigentlich können müsste**, weil sie sich auf eine Seite oder ein Kapitel bezieht, das in der
Schule schon durchgenommen wurde. Anlass ist sofatutors Video-Hilfe, die von der Übung an die passende
Stelle des Erklärvideos springt; Puglings Gegenstück ist das eigene Schulbuch.

## Ist-Stand am Code (erste Sicht, 2026-11-15)

- **Unit-Bezug ist Pflicht und wird ausgeliefert, aber nicht gezeigt.** Jede Übung hängt an einer
  Unit (`backend/Pugling.Api/Models/LearnEntities.cs:80`, seit B-106). Die Positions-Übersicht für das
  Kind trägt `SeriesUnitId` und `SeriesUnitLabel` (`backend/Pugling.Contracts/Shared/ProgressDtos.cs:25`–`29`),
  im Frontend liest `seriesUnitLabel` aber keine Komponente (kein Treffer in `frontend/src/`). Nur das
  Bonus-Training zeigt eine Unit als Pille (`frontend/src/sohn/SohnBonus.tsx:119`, aus
  `ExtraExercise.UnitLabel`, `backend/Pugling.Contracts/Student/ExtraTrainingDtos.cs:35`).
- **Eine Seite gibt es nirgends.** Die Übung trägt kein Seitenfeld (`Models/LearnEntities.cs:77`–`115`),
  die Unit nur `Label`, `OrderIndex` und `Grade` (`Models/CurriculumEntities.cs:73`–`77`). Die
  Seitenzahlen liegen nur außerhalb der App: Der Skill `lehrwerk-aufbereiten` benennt jede Seiten-Datei
  nach den Buchseiten (`s004-005.md`, `.claude/skills/lehrwerk-aufbereiten/MARKDOWN.md:16`).
- **„Durchgenommen" ist ableitbar, aber nicht abgeleitet.** Das Kind zeigt auf seine aktuelle Unit
  (`Textbook.CurrentUnitId`, `Models/AdminEntities.cs:180`); mit `Grade` und `OrderIndex` der Units ließe
  sich sagen „liegt vor der aktuellen Unit". Ein solches Merkmal gibt es in keinem Vertrag.

## Die Idee

1. **Seitenangabe an der Übung** (optional, etwa „S. 42" oder „S. 42–43"), gepflegt vom Creator, beim
   Aufbereiten aus Fotos und vom KI-Creator mit befüllt (die Seiten-Datei kennt ihre Seiten).
2. **Anzeige beim Kind:** an der Position bzw. Karte „Access 7 · Unit 3 · S. 42", dazu ein Merkmal
   **„schon durchgenommen"**, wenn die Unit vor der aktuellen Unit des Kindes liegt (oder sie ist).
3. **Anzeige beim Vater:** derselbe Bezug in Plan und Lernstand, damit er bei einer Frage
   ([B-586](B-586-kind-fragt-den-betreuer-zur-aufgabe.md)) oder einem Fehler auf die Buchseite zeigen kann.

## Offene Punkte

- **Anti-Cheat (die wichtigste Frage):** Eine Seitenangabe zur Vokabelliste ist während einer getippten
  Abfrage ein Lösungsweg am Server vorbei – das Kind schlägt die Seite auf. Empfehlung: beim Üben und
  **nach** der Antwort zeigen, im Positions-Test und in der Klausur erst nach der Abgabe; der Server
  liefert die Seite dort gar nicht aus (nicht nur verstecken).
- **Granularität:** Seite an der Übung oder an jedem Item (eine Vokabel steht auf genau einer Seite der
  Wortliste)? Empfehlung: erst an der Übung, Items später, falls die Erklärung nach der Antwort
  ([B-583](B-583-erklaerung-nach-falscher-antwort.md)) auf die Seite verweisen will.
- **Was heißt „durchgenommen"?** Allein aus `CurrentUnitId` (alles davor) oder ein eigenes Datum je Unit,
  das der Vater setzt? Empfehlung: aus `CurrentUnitId` ableiten, kein neues Feld.
- **Wirkung auf Pflicht und Münzen:** Soll „schon durchgenommen" nur ein Hinweis sein, oder etwas
  bewirken (etwa: diese Übungen verlangen eine höhere Bestehensgrenze)? Empfehlung: zuerst nur Hinweis.
  Für Friedrich ist die Botschaft „das hattet ihr schon" Ansporn; ist der Stoff zu schwer, greift
  [B-536](B-536-aufbau-uebungen-statt-versagen.md).
- **Mehrere Bücher je Reihe** (`BookType`: Schülerbuch, Workbook): der Bezug muss das Buch nennen.

## Verwandt

[B-375](B-375-heftseite-wird-uebungsentwurf.md) (Buchseite wird Übungsentwurf – liefert die Seite gleich
mit) · [B-580](B-580-plan-aus-unit.md) (Plan aus Unit) · [B-583](B-583-erklaerung-nach-falscher-antwort.md)
(Erklärung nach der Antwort, Sprung zum Stoff) · [B-586](B-586-kind-fragt-den-betreuer-zur-aufgabe.md)
(Frage an den Betreuer) · Sammel-Story [B-585](B-585-sofatutor-vergleich-sammel-story.md).

## Verlauf

- **2026-11-15** — angelegt (Quelle: Wunsch des Nutzers im Gespräch zum sofatutor-Vergleich; Anlass sofatutors Video-Hilfe, die von der Übung zur passenden Stelle springt).
