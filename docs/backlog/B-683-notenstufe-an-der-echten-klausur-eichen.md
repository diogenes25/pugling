---
tags: [typ/story, status/ausformuliert, bereich/gamification, bereich/klassenarbeit, rolle/student, rolle/creator]
aliases: [Notenstufe eichen, Auf- und Abstieg durch Klausur]
status: ausformuliert
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/leitbild-privatlehrer.md
grund: ""
ersetzt_durch: []
---

# B-683 · Die echte Klausurnote lässt aufsteigen oder absteigen und eicht die Prognose

Die Notenstufe (B-682) ist eine Prognose; die echte Klassenarbeit verifiziert sie. Liegt die Note besser als die
Stufe, steigt das Kind auf, liegt sie schlechter, steigt es ab. Weicht die Note deutlich von der Prognose ab, lag aber
zum Teil auch der Agent falsch – die Prognose muss sich an der echten Note **eichen**, sonst verliert die Stufe ihren
Wert und das Kind glaubt ihr nicht mehr. Die Note liegt heute schon als `Klassenarbeit.Grade` vor (von Hand
eingetragen) und wird von einem Key-Result gelesen.

**Vorbehalt:** Hängt am Richtungswechsel „Leitbild Privatlehrer“ und an B-682 (Nutzer entscheidet). Nur **ausformuliert**;
Fragen unter „Offene Punkte“ sind **nicht entschieden**, kein Abschnitt `## Entscheidungen`.

## User Story

Als *Schüler* möchte ich, dass meine Notenstufe sich nach der echten Klassenarbeit richtig anfühlt – steige ich nach einer
guten Arbeit auf, und bei einer schlechten bekomme ich einen freundlichen Weg zurück, damit ich der Stufe vertraue. Als
*Vater* möchte ich, dass eine schlechte Prognose nach einer echten Note nachgeschärft wird und nicht wiederholt danebenliegt.

## Ist-Stand am Code

Gemessen am 2026-11-19 auf `main` (Stand `47791819`). Pfade relativ zu `backend/Pugling.Api/`.

- **Die Note existiert und ist schreibbar:** `Klassenarbeit.Grade` (`Models/KlassenarbeitEntities.cs:82`),
  `GradeComment` (`:84`), `Status` mit `Planned`/`Written` (`:80`; Contract-Enum). Schreiben nur der Supervisor:
  Anlegen `Controllers/Supervisor/KlassenarbeitenController.cs:92`, Ändern `:129-164` (PATCH, `ClearGrade` `:152`); gültig
  1,0 bis 6,0 (`:48`); mit gesetzter Note wird der Status automatisch `Written` (`:111`, `:164`).
- **Es gibt keine Historie der Prognose:** `Klassenarbeit` trägt weder eine prognostizierte Note noch einen Zeitpunkt oder
  einen Lernstand-Schnappschuss zur Klausur (`Models/KlassenarbeitEntities.cs:67-106` gelesen: Kind, Fach, `ScheduledDate`,
  `Grade`, `GradeComment`, Übungen, Tags). Eine Eichung hätte nichts, womit sie die Prognose vergleichen könnte.
- **Wer die Note liest:** nur `ObjectiveEvaluationService` (Ziel-Maß `ClassTestGrade`, `Services/Shared/ObjectiveEvaluationService.cs:105`)
  und der Klassenarbeits-Controller selbst (inkl. Liste schwacher Arbeiten ab `minBadGrade` 4,0, `KlassenarbeitenController.cs:25`,
  `:274-286`). Das Maß nimmt die **beste** Note je Fach (`BestGradeTimesTen`, `ObjectiveEvaluationService.cs:123-127`,
  `Min`) – eine schlechtere spätere Note ändert es nicht. Für eine Stufe, die **absteigen** darf, ist das die falsche Rechnung.
- Das Kind sieht seine Klausur und Note nicht (kein Treffer für `Klassenarbeit` unter `Controllers/Student/`).
- Notenstufe, Prognose, Eichung: **nicht gefunden** (vgl. B-682). Die Eintragung einer Note löst nichts aus – kein Ereignis, kein
  Dienstaufruf, keine Wallet-Wirkung (`KlassenarbeitenController.cs:156-164` setzt nur Feld und Status).
- Wie die Note an die Klassenarbeit kommt (Foto/Scan): **nicht gefunden**, siehe [B-684](B-684-korrigierte-klausur-einscannen.md) (`idee`).

## Die echte Lücke

Es fehlt der **Rückkanal** von der echten Note zur Stufe, und davor das Material dafür: (1) eine gespeicherte Prognose
zum Klausurtermin, gegen die die Note verglichen wird (sonst kann nichts „eichen“); (2) eine Regel, wie viel Gewicht die
Note gegenüber dem Lernstand hat; (3) die Absteigen-Mechanik samt Ton; (4) ein Auslöser beim Eintragen der Note
(`PATCH` heute ohne Folge). Außerdem ist das bestehende `ClassTestGrade`-Ziel („beste Note“) mit einer sinkenden Stufe
nicht vereinbar – die Story darf es nicht stillschweigend umdeuten.

## Offene Punkte

Jede Frage mit Empfehlung. **Nichts davon ist entschieden.**

1. **Zählt eine Klausur sofort voll oder wird sie mit der Prognose verrechnet (Tagesform)?** Empfehlung: Die Note setzt die
   Stufe **sofort auf den Bereich der Note**, aber nur **eine Stufe** pro Klausur abwärts (Ton ohne Tadel, Tagesform
   schluckt einen Ausreißer); aufwärts darf sie voll zählen. Eine zweite Klausur in Folge bestätigt oder korrigiert.
2. **Wie lernt das Modell aus einer einzelnen Note?** Empfehlung: im ersten Bau **gar nicht automatisch** – die Abweichung
   (Prognose ↔ Note) wird gespeichert und dem Vater gezeigt; der Umrechnungsschlüssel (B-682 Punkt 2) wird von Hand
   nachgestellt, bis genug Paare vorliegen. Ein einziger Datenpunkt eicht nichts.
3. **Was sieht das Kind beim Abstieg?** Empfehlung: kein „abgestiegen“, sondern „Stufe 3 – nächste Ziel: 12 Wörter
   sicher“ plus der Weg zurück; Ton als Produktentscheidung des Nutzers.
4. **Was passiert mit dem Ziel-Maß `ClassTestGrade`?** Empfehlung: unverändert lassen (beste Note); die Stufe ist eine
   eigene Größe. Beides zu vermengen würde laufende Ziele (`ObjectiveEvaluationService.cs:105`) ändern.
5. **Auslöser:** Empfehlung: Eintrag der Note (`PATCH` mit `Grade`) stößt die Neubewertung an, idempotent; ein nachträglich
   korrigierter Wert rechnet die Stufe neu (nicht doppelt buchen). Ob eine Klausurprämie in Münzen daran hängt: nicht
   vorwegnehmen – *Münz-Ökonomie, Nutzer entscheidet*.
6. **Wer trägt die Note ein?** Empfehlung: Vater (siehe B-681 Punkt 5); der Lehrer(-Agent) liest sie nur. Bei B-684
   kommt die Note aus dem Scan, aber die Freigabe bleibt beim Vater.
7. **Welche Klausuren zählen?** Empfehlung: nur solche mit `ScheduledDate` und Fach; eine Note ohne Fach verändert
   keine Stufe.

## Akzeptanzkriterien (Entwurf)

Final erst nach Punkt 1, 2 und 5. Je Kriterium die Änderung, die es rot macht:

1. Eine eingetragene Note speichert die zu diesem Zeitpunkt gültige Prognose (Stufe/Note) an der Klassenarbeit. *Rot, wenn*
   `Grade` gesetzt wird, ohne dass die Prognose festgehalten wird (Eichung ohne Vergleichswert).
2. Eine Note besser als die Stufe hebt sie; eine Note schlechter senkt sie höchstens um die in Punkt 1 festgelegte Zahl
   Stufen. *Rot, wenn* die Stufe nach einer Klausur unverändert bleibt oder um mehr als erlaubt fällt.
3. Dieselbe Note zweimal eingetragen (oder das Feld unverändert erneut gesendet) ändert die Stufe nicht ein zweites Mal.
   *Rot, wenn* der `PATCH` jedes Mal neu bewertet.
4. Eine nachträglich **korrigierte** Note ersetzt die Wirkung der alten, sie addiert sich nicht. *Rot, wenn* zwei
   Eintragungen zwei Auf-/Abstiege auslösen.
5. Das Ziel-Maß `ClassTestGrade` liefert für dieselben Daten dasselbe wie vorher (beste Note). *Rot, wenn* die Umsetzung
   `BestGradeTimesTen` ändert.
6. Eine Note ohne Fach oder Termin verändert keine Stufe. *Rot, wenn* sie ein beliebiges Fach trifft.
7. Der Text am Kind beim Absteigen enthält keinen Stufen-Etikett-Abstieg und nennt den nächsten Schritt. *Rot, wenn* das
   Wort „abgestiegen“ oder die schlechtere Notenbezeichnung erscheint.

## Beziehungen zu anderen Stories (nur gelesen)

- [B-682](B-682-notenstufe-je-fach-statt-liga.md): liefert die Stufe, die hier verifiziert wird; Voraussetzung.
- [B-684](B-684-korrigierte-klausur-einscannen.md): Weg der Note an die Klassenarbeit.
- [B-249](B-249-probeklausur-mit-notenprognose.md): Probeklausur, Notenschlüssel G-7; zweiter Datenpunkt der Eichung.
- [B-681](B-681-rollenschnitt-vater-belohnt-lehrer-unterrichtet.md): wer die Note einträgt.

## Verlauf

- **2026-11-19** — angelegt aus dem Leitbild-Gespräch mit dem Vater ([Leitbild](../leitbild-privatlehrer.md)).
- **2026-11-19** — `idee` → `ausformuliert` (Vorbereitungslauf, Sonnet): Ist-Stand am Code belegt. Kern: Die Note ist
  schreibbar, hat aber keinerlei Folge, und es gibt keine gespeicherte Prognose, gegen die geeicht werden könnte; das
  bestehende `ClassTestGrade`-Maß (beste Note) darf nicht umgedeutet werden. Sieben offene Punkte mit Empfehlung,
  **nichts entschieden**.
