---
tags: [typ/story, status/verworfen, bereich/gamification, bereich/klassenarbeit, rolle/student, rolle/creator]
aliases: [Notenstufe eichen, Auf- und Abstieg durch Klausur]
status: verworfen
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/leitbild-privatlehrer.md
grund: "geteilt, faktisch XL: Prognose-Schnappschuss mit Migration, Wirkung der Note und Abweichungsanzeige sind drei einzeln baubare Stücke"
ersetzt_durch: [B-744, B-745, B-746]
---

# B-683 · Die echte Klausurnote lässt aufsteigen oder absteigen und eicht die Prognose

Die Notenstufe (B-682) ist eine Prognose; die echte Klassenarbeit verifiziert sie. Liegt die Note besser als die
Stufe, steigt das Kind auf, liegt sie schlechter, steigt es ab. Weicht die Note deutlich von der Prognose ab, lag aber
zum Teil auch der Agent falsch – die Prognose muss sich an der echten Note **eichen**, sonst verliert die Stufe ihren
Wert und das Kind glaubt ihr nicht mehr. Die Note liegt heute schon als `Klassenarbeit.Grade` vor (von Hand
eingetragen) und wird von einem Key-Result gelesen.

**Stand:** Stellvertretend gegrillt im Vorbereitungslauf 2026-11-20 auf Anweisung des Nutzers („Entscheide die offenen Fragen nach deiner Empfehlung“), unbestätigt. Die Rahmenentscheidungen A bis G gelten für den ganzen Cluster B-644, B-680 bis B-683, B-662, B-663, B-702 und B-703; führend ist B-681.

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

Alle Punkte entschieden; die Empfehlung steht durchgestrichen daneben.

1. ~~**Zählt eine Klausur sofort voll oder wird sie mit der Prognose verrechnet (Tagesform)?** Empfehlung: Die Note setzt die Stufe **sofort auf den Bereich der Note**, aber nur **eine Stufe** pro Klausur abwärts (Ton ohne Tadel, Tagesform schluckt einen Ausreißer); aufwärts darf sie voll zählen. Eine zweite Klausur in Folge bestätigt oder korrigiert.~~ → Entscheidung 1.
2. ~~**Wie lernt das Modell aus einer einzelnen Note?** Empfehlung: im ersten Bau **gar nicht automatisch** – die Abweichung (Prognose ↔ Note) wird gespeichert und dem Vater gezeigt; der Umrechnungsschlüssel (B-682 Punkt 2) wird von Hand nachgestellt, bis genug Paare vorliegen. Ein einziger Datenpunkt eicht nichts.~~ → Entscheidung 2.
3. ~~**Was sieht das Kind beim Abstieg?** Empfehlung: kein „abgestiegen“, sondern „Stufe 3 – nächste Ziel: 12 Wörter sicher“ plus der Weg zurück; Ton als Produktentscheidung des Nutzers.~~ → Entscheidung 3.
4. ~~**Was passiert mit dem Ziel-Maß `ClassTestGrade`?** Empfehlung: unverändert lassen (beste Note); die Stufe ist eine eigene Größe. Beides zu vermengen würde laufende Ziele (`ObjectiveEvaluationService.cs:105`) ändern.~~ → Entscheidung 4.
5. ~~**Auslöser:** Empfehlung: Eintrag der Note (`PATCH` mit `Grade`) stößt die Neubewertung an, idempotent; ein nachträglich korrigierter Wert rechnet die Stufe neu (nicht doppelt buchen). Ob eine Klausurprämie in Münzen daran hängt: nicht vorwegnehmen – *Münz-Ökonomie, Nutzer entscheidet*.~~ → Entscheidung 5.
6. ~~**Wer trägt die Note ein?** Empfehlung: Vater (siehe B-681 Punkt 5); der Lehrer(-Agent) liest sie nur. Bei B-684 kommt die Note aus dem Scan, aber die Freigabe bleibt beim Vater.~~ → Entscheidung 6.
7. ~~**Welche Klausuren zählen?** Empfehlung: nur solche mit `ScheduledDate` und Fach; eine Note ohne Fach verändert keine Stufe.~~ → Entscheidung 7.

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-20 (Sonnet) auf Anweisung des Nutzers (Empfehlung übernommen), unbestätigt.*

## Entscheidungen

Entscheidung 6 ist die Rahmenentscheidung D, Entscheidung 8 die Rahmenentscheidung F (führend B-681).

1. **Eine eingetragene Klausurnote setzt die Stufe des Fachs sofort auf den Notenbereich: aufwärts voll, abwärts höchstens eine Stufe je Klausur.** Begründung: Tagesform schluckt einen Ausreißer, eine zweite Klausur bestätigt oder korrigiert. Kosten: Ein Kind auf Stufe 2, das eine 5 schreibt, steht erst nach zwei Klausuren bei 4; der nachlassende Lernstand (B-682 Entscheidung 4) kann es schneller dorthin bringen. Verworfen: volles Absteigen nach einer Klausur. Schließt aus: einen Absturz um drei Stufen durch eine einzige Arbeit.
2. **Das Modell eicht sich im ersten Bau nicht von selbst.** Die Abweichung (Prognose-Stufe zu Note) wird an der Klassenarbeit gespeichert und dem Vater im Kind-Hub gezeigt; den Schlüssel stellt er bzw. der Entwickler von Hand nach (B-682 Entscheidung 2). Begründung: Ein einziger Datenpunkt eicht nichts. Kosten: Die Prognose kann wiederholt danebenliegen, bis jemand nachstellt; der Vater liest eine Abweichungsliste. Verworfen: ein automatisches Nachstellen nach jeder Note. Schließt aus: die Zeile der User Story „wird nachgeschärft“ in der Lesart „von selbst“; nachgeschärft wird von Hand.
3. **Beim Absteigen nennt die Oberfläche die neue Stufe und den Weg zurück („nächstes Ziel: noch N Wörter sicher“), nie das Wort „abgestiegen“ oder eine schlechtere Notenbezeichnung** (gleiche Textregel wie B-682 Entscheidung 3). Begründung: Der freundliche Weg zurück ist der Kern der User Story. Kosten: Das Kind sieht den Rückschritt nur an der Zahl der Stufe. Verworfen: der Text „Du bist abgestiegen“. Schließt aus: einen Tadel im Wortlaut.
4. **Das Ziel-Maß `ClassTestGrade` bleibt unverändert (beste Note je Fach); die Stufe ist eine eigene Größe.** Begründung: `BestGradeTimesTen` (`ObjectiveEvaluationService.cs:123`) liest die beste Note; das Maß für ein sinkendes Ergebnis umzudeuten, änderte laufende Ziele. Kosten: Zwei Größen für „Note im Fach“ nebeneinander, mit verschiedener Bedeutung (beste vs. aktuelle). Verworfen: das Maß auf die letzte Note umstellen. Schließt aus: ein Ziel, das nach einer schlechten Klausur plötzlich als verfehlt gilt, obwohl es schon erreicht wurde.
5. **Das Eintragen oder Ändern einer Note (`PATCH` mit geändertem `Grade`) stößt die Neubewertung an, idempotent je Klassenarbeit.** Die Prognose-Stufe wird als Schnappschuss am Klausurtag gespeichert (neue nullable Spalte `Klassenarbeit.PredictedLevel`, vom täglichen Lauf des `ScheduledDate` einmalig gesetzt). Die Stufe nach der Klausur ist eine Funktion aus Schnappschuss und Note; eine korrigierte Note ersetzt die Wirkung der alten, und sie ändert die laufende Stufe nur, wenn es die jüngste geschriebene Klausur des Fachs ist, sonst nur die gespeicherte Abweichung. Begründung: Ohne Schnappschuss gibt es nichts zum Vergleichen, und die Funktion aus Schnappschuss und Note ist wiederholbar. Kosten: eine Migration; Klassenarbeiten ohne Lauf am Klausurtag (Server aus) haben keinen Schnappschuss und eichen nicht. Verworfen: die Prognose zum Zeitpunkt der Noteneintragung – Tage nach der Klausur, der Lernstand ist weiter. Schließt aus: Klausuren ohne Prognose (Altmaterial) als Eichpunkte.
6. **Klausurtermin und Note trägt der Vater ein (Rahmen D, führend B-681).** Die Schreibrouten der Klassenarbeit bleiben `Roles.Supervisor` (`KlassenarbeitenController.cs:92`, `:130`); Planer, Agent und eine spätere Foto-Auswertung (B-684) lesen Termin und Note oder schlagen sie nur vor, die Freigabe bleibt beim Vater. Begründung: Er hat den Schulkontakt (WebUntis, Heft, Klausur), und eine Note beeinflusst Stufe und Pflicht – kein Konto, das davon profitiert oder Pflichten setzt, trägt sie selbst ein. Kosten: ein Handgriff mehr nach jedem Scan; solange B-684 fehlt, bleibt das Eintragen Handarbeit. Verworfen: Note direkt aus dem Scan übernehmen. Schließt aus: ein Kind oder einen Agenten, der die eigene Note einträgt (Selbstbetrug). (Beantwortet Punkt 6.)
7. **Nur Klassenarbeiten mit Fach zählen; eine Note ohne Fach (`SubjectId` ist nullable, `KlassenarbeitEntities.cs`) verändert keine Stufe.** Das Datum ist Pflichtfeld und damit immer vorhanden. Begründung: Eine Stufe gehört zu einem Fach; ein beliebig zugeordnetes Fach wäre erfunden. Kosten: Der Vater muss das Fach setzen, sonst bleibt die Note ohne Wirkung auf die Stufe. Verworfen: die Note dem Fach des Titels zuraten. Schließt aus: eine Note, die ein falsches Fach hebt oder senkt.
8. **Münzen hängen an der erfüllten Pflicht, nicht an Notenstufe oder Klausurnote (Rahmen F, führend B-681).** Zielpunkte, Truhe und Serie bleiben wie heute an der erfüllten Pflicht; in diesem Zyklus gibt es keine Münzprämie für eine Notenstufe oder eine Klausurnote, die Notenstufe ist Auszeichnung ohne Münzwert (`RewardPoints` 0). Begründung: Die Stufe ist aus Lerndaten geschätzt und keine harte Leistung wie ein bestandener Test, die Note wird von Hand eingetragen; eine Münzprämie darauf trüge Prognosefehler und Eintragsfehler in das Guthaben (Wallet-Invarianten). Die Probeklausur B-249 ist ebenfalls ohne Münzen entschieden. Kosten: Eine gute Klausur belohnt der Vater von Hand (`POST children/{}/points`, Manual); für die Stufe gibt es Ehre statt Geld. Verworfen: Klausurprämie in Münzen. Schließt aus: ein Kind, das für eine Eins in der Klausur automatisch Münzen bekommt. (Beantwortet die Münzfrage aus Punkt 5.)

## Akzeptanzkriterien

Final. Je Kriterium die Änderung, die es rot macht.

1. Der Lauf am `ScheduledDate` schreibt die Prognose-Stufe einmal an die Klassenarbeit; ein zweiter Lauf überschreibt sie nicht. *Rot, wenn* der Schnappschuss fehlt oder beim zweiten Lauf ersetzt wird.
2. Eine Note besser als die Stufe hebt sie voll, eine schlechtere senkt sie um höchstens eine Stufe. *Rot, wenn* die Stufe unverändert bleibt oder um mehr als eine fällt.
3. Dieselbe Note zweimal gesendet ändert die Stufe nicht ein zweites Mal. *Rot, wenn* der `PATCH` jedes Mal neu bewertet.
4. Eine korrigierte Note ersetzt die Wirkung der alten. *Rot, wenn* zwei Eintragungen zwei Auf- oder Abstiege auslösen.
5. Die Korrektur der Note einer älteren Klausur ändert nur die gespeicherte Abweichung, nicht die laufende Stufe. *Rot, wenn* die laufende Stufe dabei springt.
6. *Absichtserklärung.* `ClassTestGrade` liefert für dieselben Daten dasselbe wie vorher (beste Note). *Rot, wenn* die Umsetzung `BestGradeTimesTen` ändert.
7. Eine Note an einer Klassenarbeit ohne Fach verändert keine Stufe. *Rot, wenn* sie ein beliebiges Fach trifft.
8. Der Text beim Absteigen enthält weder „abgestiegen“ noch eine Notenbezeichnung der neuen Stufe und nennt den nächsten Schritt. *Rot, wenn* eines der Wörter erscheint.
9. Das Eintragen der Note bucht keine Münzen. *Rot, wenn* dabei ein `ChildPointsEntry` entsteht.

## Beziehungen zu anderen Stories (nur gelesen)

- [B-682](B-682-notenstufe-je-fach-statt-liga.md): liefert die Stufe, die hier verifiziert wird; Voraussetzung.
- [B-684](B-684-korrigierte-klausur-einscannen.md): Weg der Note an die Klassenarbeit.
- [B-249](B-249-probeklausur-mit-notenprognose.md): Probeklausur, Notenschlüssel G-7; zweiter Datenpunkt der Eichung.
- [B-681](B-681-rollenschnitt-vater-belohnt-lehrer-unterrichtet.md): wer die Note einträgt.

## Schätzung

Nicht geschätzt, sondern geteilt: Schnappschuss-Spalte mit Migration, Rückkanal der Note auf die Stufe und eine Abweichungsanzeige
über Backend, Vertrag und Frontend ergäben ein XL, das zudem an [B-682](B-682-notenstufe-je-fach-statt-liga.md) hängt. Zerlegung nach
„Teilen und Zusammenlegen":

1. [B-744](B-744-klausur-prognose-schnappschuss.md) · `PredictedLevel` am Klausurtag festhalten (M, Migration; Entscheidungen 5 erster Teil, 7, 8).
2. [B-745](B-745-klausurnote-wirkt-auf-die-notenstufe.md) · Note hebt oder senkt die Stufe (M; Entscheidungen 1, 4, 5 zweiter Teil, 6, 7, 8).
3. [B-746](B-746-klausur-abweichung-fuer-den-vater.md) · Abweichung für den Vater (S, beides; Entscheidung 2).

Die Textregel für den Abstieg (Entscheidung 3, Kriterium 8) liegt in [B-743](B-743-notenstufe-anzeige-kind-und-vater.md), weil es dieselbe
Anzeige ist. Bau-Reihenfolge: B-741 (aus B-682) vor B-744 vor B-745 vor B-746. Offene Frage, die aus der Schätzung folgt und die
Entscheidungen nicht beantworten: ob die Klausurwirkung neben dem täglichen Lauf Bestand hat (siehe B-745, Migration `offen`).

## Verlauf

- **2026-11-19** — angelegt aus dem Leitbild-Gespräch mit dem Vater ([Leitbild](../leitbild-privatlehrer.md)).
- **2026-11-19** — `idee` → `ausformuliert` (Vorbereitungslauf, Sonnet): Ist-Stand am Code belegt. Kern: Die Note ist
  schreibbar, hat aber keinerlei Folge, und es gibt keine gespeicherte Prognose, gegen die geeicht werden könnte; das
  bestehende `ClassTestGrade`-Maß (beste Note) darf nicht umgedeutet werden. Sieben offene Punkte mit Empfehlung,
  **nichts entschieden**.
- **2026-11-20** — `ausformuliert` → `gegrillt` (Vorbereitungslauf, Sonnet, stellvertretend auf Anweisung des Nutzers, unbestätigt): Alle sieben Punkte entschieden. Kern: Note setzt die Stufe sofort (abwärts höchstens eine Stufe), keine automatische Eichung, Prognose-Schnappschuss am Klausurtag, `ClassTestGrade` unverändert, Note trägt der Vater ein, keine Münzprämie.
- **2026-11-20** — `gegrillt` → `verworfen` (geteilt, Vorbereitungslauf, Sonnet): faktisch XL, zerlegt in [B-744](B-744-klausur-prognose-schnappschuss.md), [B-745](B-745-klausurnote-wirkt-auf-die-notenstufe.md), [B-746](B-746-klausur-abweichung-fuer-den-vater.md); Eintrittsbedingung `gegrillt` geprüft und erfüllt.
