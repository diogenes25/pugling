---
tags: [typ/story, status/gegrillt, bereich/gamification, bereich/klassenarbeit, rolle/student]
aliases: [Notenstufe, Einser-Schüler, Liga-Ersatz]
status: gegrillt
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

# B-682 · Notenstufe je Fach: das Kind ist auf dem Stand eines Einser- bis Sechser-Schülers

Statt einer Liga (gegen andere Kinder ausgeschlossen, G-6 vom 2026-10-23) bekommt das Kind je Fach eine
**Notenstufe**: Der Lehrer-Agent weiß, welchen Lernstand ein Schüler für eine 1 braucht, und sagt etwa „Aktuell bist
du auf dem Stand eines Zweier-Schülers“. Das Kind steigt auf, indem es mehr lernt oder schwerere Übungen schafft. Die
Stufe darf **sinken** (Entscheidung des Vaters, 2026-11-19) – verifiziert wird sie an der echten Klausur (B-683). Sie
ist zugleich das Ziel, auf das der tägliche Plan (B-680) hinarbeitet.

**Stand:** Stellvertretend gegrillt im Vorbereitungslauf 2026-11-20 auf Anweisung des Nutzers („Entscheide die offenen Fragen nach deiner Empfehlung“), unbestätigt. Die Rahmenentscheidungen A bis G gelten für den ganzen Cluster B-644, B-680 bis B-683, B-662, B-663, B-702 und B-703; führend ist B-681.

## User Story

Als *Schüler* möchte ich je Fach sehen, auf welchem Notenstand mein Können gerade ist und was mir bis zur nächsten Stufe
fehlt, damit ich ein greifbares Ziel habe, das nicht vom Vergleich mit anderen abhängt. Als *Vater* möchte ich, dass diese
Stufe nur aus tatsächlich getippten, serverseitig gemessenen Leistungen entsteht, damit sich das Kind nicht zur 1
spielt.

## Ist-Stand am Code

Gemessen am 2026-11-19 auf `main` (Stand `47791819`). Pfade relativ zu `backend/Pugling.Api/`.

### Was es nicht gibt

- Keine Notenstufe, keine Prognose „Lernstand → Note“, keine Liga: Suche nach `Notenstufe`, `Prognose`, `Liga`/`League` unter
  `Models/`, `Services/`, `Controllers/` und `frontend/src/sohn/pfad.ts` ohne einschlägigen Treffer (die Wortteil-Treffer
  in `PositionProgressService.cs` sind „obligation“). Nicht gefunden: eine Entität oder ein Dienst je Fach und Kind, der
  eine Stufe trägt.
- Die Auszeichnungen (`Achievement`, `Models/GamificationEntities.cs:61`; Vergabe `AchievementAward`, `:79`) kennen nur
  Schwelle und Kennzahl (`Metric`, `Threshold`), keine Fach- oder Notenstufe und kein Wieder-Absteigen; eine Auszeichnung wird
  „genau einmal“ vergeben (idempotent). Das ist die Form, in der „beste je bestätigte Note als Auszeichnung“ (B-595-Gedanke)
  heute überhaupt stehen könnte.

### Was als Eingang vorhanden ist

- **Lernstand je Wort, je Kind, über alle Pläne:** `ItemProgress` mit `Box` (1 bis `MaxBox = 5`) und `MasteryPercent`
  (`Models/ItemProgressEntities.cs:18`, `:39` `WeakBelowPercent = 50`, `:41`); jede Antwort steht als `ItemReviewEvent`
  (`:61`). Das ist die serverseitig gemessene Grundlage, die das Anti-Cheat-Kriterium verlangt: Antworten kommen über
  die Spiel-Endpunkte, nicht vom Client.
- **Rollup je Fach/Unit/Übung:** `MasteryRollup(TotalItems, IntroducedItems, MasteredItems, WeakItems, AvgMasteryPercent, …)`
  (`Pugling.Contracts/Student/LearnProgressDtos.cs:11`), gerechnet in `Services/Student/ChildLearnProgressService.cs:141`
  (Durchschnitt über **eingeführte** Wörter, nicht über alle; Abdeckung `:147`). „Wie viel der Unit sitzt“ ist damit für
  **Vokabeln** je Fach und Unit messbar.
- **Die echte Note:** `Klassenarbeit.Grade` (`Models/KlassenarbeitEntities.cs:82`), nur vom Supervisor schreibbar
  (`Controllers/Supervisor/KlassenarbeitenController.cs:130`, `:156`); Ziel-Maß `ClassTestGrade` im
  `ObjectiveEvaluationService.cs:105` liest die **beste** Note je Fach (`BestGradeTimesTen`, `:123-127`, `Min` über die Noten).
  Das **Maß kennt also kein Absinken** – Voraussetzung für die Stufe, die sinken darf.
- Kein Student-Endpunkt liest `Klassenarbeit` (kein Treffer unter `Controllers/Student/`); das Kind sieht seine Noten nicht.

### Nicht belegt / nicht gemessen

- Ob Mathe- und Grammatik-Übungen einen vergleichbaren Beherrschungswert tragen: `ItemProgress` ist am Vokabelpaar
  gebaut (`Models/ItemProgressEntities.cs:10-17`). Für Rechenaufgaben und Grammatik **nicht gefunden**, ob je Aufgabe eine
  Beherrschung mitläuft. Das ist die offene Messfrage hinter „für Grammatik und Mathe deutlich schwerer“.
- Eine **Wortliste der Unit als eigene Entität** gibt es nicht; die Unit ist an den Übungen ablesbar
  (`SeriesUnitId` an der Übung, `Models/CurriculumEntities.cs:153`), die Stoffnotizen sind Freitext (`:85`).

## Die echte Lücke

Nicht „Daten fehlen“, sondern ein **Rechenmodell und eine Anzeige**: Für Vokabeln liegt je Fach und Unit alles vor, was
eine Stufe braucht (eingeführt/beherrscht/schwach). Es fehlen (1) die Abbildung *Rollup → Note* je Fach, (2) ein Ort für
die Stufe (heute weder Entität noch Berechnung), (3) die Aussage, **woran** eine Note im Fach hängt, wo es keine Wortliste
gibt (Mathe, Grammatik), und (4) der Ton gegenüber dem Kind für die unteren Stufen.

Abgrenzung: Die **Probeklausur** [B-249](B-249-probeklausur-mit-notenprognose.md) rechnet *ein Bündel* einer Klassenarbeit in
eine Note um (Notenschlüssel, G-7); die Notenstufe rechnet den *laufenden Lernstand* in eine Note um. Beide brauchen
dieselbe Umrechnung „Anteil → Note“ – eine Stelle, nicht zwei.

## Offene Punkte

Alle Punkte entschieden; die Empfehlung steht durchgestrichen daneben.

1. ~~**Aus welchem Maß entsteht die Stufe?** Empfehlung für den ersten Bau: nur **Vokabel-Fächer**, Maß = Anteil der Unit-Wörter mit `MasteryPercent` ab einer Schwelle (beherrscht), gewichtet mit der Menge der Stoff-Wörter bis zur nächsten Klausur. Mathe/Grammatik erst, wenn ein Beherrschungswert je Aufgabe belegt ist (siehe oben). Eine Notenstufe für ein Fach ohne Maß wäre erfunden.~~ → Entscheidung 1.
2. ~~**Wie kommt aus „Anteil“ eine Note?** Empfehlung: **derselbe feste Prozent-Schlüssel wie B-249** (Entscheidung G-7), damit die Stufe und die Probeklausur nicht zwei Notenschlüssel haben. Der Schlüssel muss je Fach/Schulart einstellbar bleiben.~~ → Entscheidung 2.
3. ~~**Wie heißen die unteren Stufen?** Empfehlung: Anzeige ohne Etikett („nächste Stufe: noch 12 Wörter sicher“), die Note nur dort, wo sie positiv wirkt; kein „Sechser-Schüler“ am Kind. Das ist die Produktfrage des Leitbilds, nicht technisch.~~ → Entscheidung 3.
4. ~~**Darf die Stufe zwischen Klausuren sinken?** Entscheidung des Vaters (2026-11-19): ja. Empfehlung: im ersten Bau sinkt sie **nur durch die echte Klausur** (B-683) und durch nachweislich nachlassenden Lernstand (viele Wörter fallen unter `WeakBelowPercent`), nicht durch Tagesform; sonst bricht ein schlechter Tag die Motivation, die die Stufe tragen soll.~~ → Entscheidung 4.
5. ~~**Bleibt die beste je erreichte Stufe als Auszeichnung?** Empfehlung: ja, als `Achievement`-artige Auszeichnung ohne Münzwert; die laufende Stufe darf sinken, die Auszeichnung nicht. Berührt die Münz-Ökonomie nur, wenn sie `RewardPoints` trägt – hier 0 (Nutzer entscheidet bei Bedarf anders).~~ → Entscheidung 5.
6. ~~**Wer bekommt die Stufe zu sehen?** Empfehlung: das Kind je Fach, der Vater im Kind-Hub; nie im Vergleich mit anderen Kindern (G-6).~~ → Entscheidung 6.
7. ~~**Reihenfolge zu B-680:** Empfehlung: B-680 baut zuerst ohne Notenstufe (B-680 Punkt 10); die Stufe ist ein späterer Planer-Eingang.~~ → Entscheidung 7.

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-20 (Sonnet) auf Anweisung des Nutzers (Empfehlung übernommen), unbestätigt.*

## Entscheidungen

Entscheidung 9 ist die Rahmenentscheidung F (führend B-681).

1. **Die Stufe entsteht im ersten Bau nur in Vokabel-Fächern aus dem Anteil beherrschter Wörter am gesamten Stoff:** `MasteredItems` geteilt durch `TotalItems` des Stoffrahmens (Units des aktiven Plans), nicht der Durchschnitt über eingeführte Wörter. Begründung: `AvgMasteryPercent` mittelt nur über eingeführte Wörter (`ChildLearnProgressService.cs`, `ToRollup`); ein Kind mit drei perfekten Wörtern stünde an der Spitze. Der Anteil am ganzen Stoff misst, was die Klausur verlangt, und stammt aus serverseitig gewerteten `ItemReviewEvent`-Daten (Anti-Cheat). Kosten: Die Stufe steigt langsam, solange der Stoff groß ist; Fächer ohne Wortliste (Mathe, Grammatik) bekommen keine Stufe. Verworfen: ein Anteil, gewichtet mit den Wörtern bis zur nächsten Klausur – ein zweiter Nenner ohne Messung. Schließt aus: Mathe und Grammatik; die User Story verspricht „je Fach“, sie gilt vorerst für Vokabel-Fächer, und ein Fach ohne Maß zeigt keine Stufe statt einer erfundenen.
2. **Der Anteil wird mit demselben festen Prozent-Schlüssel in eine Note umgerechnet wie die Probeklausur (B-249, G-7):** Standardtabelle in `appsettings.json` (Muster `Scoring:TimeSlots`), eine Funktion `PercentToGrade`, die B-249 und B-682 gemeinsam nutzen – wer zuerst baut, legt sie an. Begründung: Zwei Schlüssel würden dieselbe Leistung verschieden benoten. Kosten: Eine Überschreibung je Fach oder Schulart gibt es im ersten Bau nicht (B-249 erlaubt sie je Klassenarbeit); die Bau-Reihenfolge der beiden Stories bindet. Verworfen: ein eigener Schlüssel für die Stufe. Schließt aus: eine Stufe, die der Vater je Fach feiner einstellt.
3. **Am Kind heißen nur die Stufen 1 bis 3 „Einser-“, „Zweier-“ und „Dreier-Stand“; darunter steht kein Etikett, sondern das nächste Ziel („noch N Wörter sicher bis zur nächsten Stufe“).** Begründung: Ein „Fünfer-Schüler“ zerstört die Motivation, die die Stufe tragen soll (Leitbild); die Note erscheint nur dort, wo sie positiv wirkt. Kosten: Das Kind ahnt die Stufe unter 3 nur aus dem Zielsatz; der Vater sieht die Zahl im Kind-Hub. Verworfen: alle sechs Stufen benennen, und der Verzicht auf jede Benennung. Schließt aus: ein Kind, das unter Stufe 3 gar nichts über seinen Stand erfährt – es sieht das Ziel.
4. **Die Stufe wird nur im täglichen Lauf neu bewertet (nicht je Antwort) und sinkt durch den Lernstand höchstens eine Stufe je sieben Tage; zusätzlich senkt die echte Klausur (B-683).** Der tägliche Lauf ist der träge Aufruf beim ersten Zugriff des Tages (B-644 Entscheidung 9), solange der Planer fehlt, derselbe Mechanismus allein. Begründung: Ein schlechter Tag soll die Stufe nicht kippen, die Entscheidung des Vaters vom 2026-11-19 „darf sinken“ gilt trotzdem. Kosten: zwei gespeicherte Werte (Stufe, Datum der letzten Senkung); der Anstieg ist nicht begrenzt. Verworfen: Neuberechnung bei jeder Antwort. Schließt aus: einen Abstieg an einem einzelnen schlechten Tag.
5. **Die beste je erreichte Stufe steht als Spalte `BestLevel` neben der laufenden Stufe und sinkt nie.** Begründung: Der Rang, der nie sinkt (B-595), bleibt als Ehre erhalten, ohne eine Auszeichnungs-Kennzahl zu bauen. Kosten: eine Spalte mehr; kein Eintrag in der Liste der `Achievement`s. Verworfen: ein `Achievement` mit neuer Kennzahl – die Vergabe „genau einmal“ passt, die Kennzahl-Verdrahtung ist ein eigener Bau. Schließt aus: eine Auszeichnung, die Münzen oder Gems zahlt (Rahmen F).
6. **Das Kind sieht die Stufe je Fach, der Vater im Kind-Hub; sie erscheint nie im Vergleich mit anderen Kindern (G-6).** Begründung: Der Vergleich mit Fremden ist ausgeschlossen, und ein Geschwistervergleich würde ihn durch die Hintertür holen. Kosten: Der Wochenvergleich der Familie (B-253) nimmt die Stufe nicht auf. Verworfen: die Stufe im Familienvergleich. Schließt aus: einen Vergleich der Stufen der Brüder.
7. **B-680 baut zuerst ohne Notenstufe; die Stufe ist ein späterer Eingang des Planers** (B-680 Entscheidung 10). Begründung: B-680 darf nicht auf ein Prognosemodell warten, das es nicht gibt. Kosten: Zwei Wege kennen kurze Zeit denselben Stoff-Anteil; der Planer rechnet auf `MasteredItems`, die Stufe ist nur Anzeige. Verworfen: den Planer an die Stufe binden. Schließt aus: einen Plan, der von einer Stufe abhängt, bevor sie bestätigt ist.
8. **Die Stufe liegt in einer eigenen Tabelle je (Kind, Fach) mit Stufe, Bestwert, Datum der letzten Senkung und Berechnungstag;** Schema und Migration hängen an die Kette. Begründung: Senkungsfrist, Bestwert und der Schnappschuss für B-683 brauchen Verlauf und lassen sich nicht je Lesen neu berechnen. Kosten: eine Migration; ein Tor G-Liste (neue Beziehung) bekommt eine bewusste Zeile. Verworfen: die Stufe nur berechnen und nie speichern – Bestwert und Frist gingen verloren. Schließt aus: eine Berechnung zur Lesezeit.
9. **Münzen hängen an der erfüllten Pflicht, nicht an Notenstufe oder Klausurnote (Rahmen F, führend B-681).** Zielpunkte, Truhe und Serie bleiben wie heute an der erfüllten Pflicht; in diesem Zyklus gibt es keine Münzprämie für eine Notenstufe oder eine Klausurnote, die Notenstufe ist Auszeichnung ohne Münzwert (`RewardPoints` 0). Begründung: Die Stufe ist aus Lerndaten geschätzt und keine harte Leistung wie ein bestandener Test, die Note wird von Hand eingetragen; eine Münzprämie darauf trüge Prognosefehler und Eintragsfehler in das Guthaben (Wallet-Invarianten). Die Probeklausur B-249 ist ebenfalls ohne Münzen entschieden. Kosten: Eine gute Klausur belohnt der Vater von Hand (`POST children/{}/points`, Manual); für die Stufe gibt es Ehre statt Geld. Verworfen: Klausurprämie in Münzen. Schließt aus: ein Kind, das für eine Eins in der Klausur automatisch Münzen bekommt. (Beantwortet die Münzfrage der Punkte 4 und 5.)

## Akzeptanzkriterien

Final. Je Kriterium die Änderung, die es rot macht.

1. Ein Kind mit null beherrschten Stoff-Wörtern steht unterhalb der besten Stufe, mit allen beherrscht an der Spitze. *Rot, wenn* der Nenner die eingeführten statt aller Stoff-Wörter ist oder die Antwortzahl zählt (ein Vielübungs-Kind überholt ein sicheres).
2. Drei perfekt beherrschte Wörter von 200 ergeben eine niedrigere Stufe als 150 beherrschte. *Rot, wenn* `AvgMasteryPercent` das Maß ist.
3. Dieselben Daten ergeben dieselbe Stufe. *Rot, wenn* die Berechnung von Zufall, einem Sprachmodell oder etwas anderem als dem Tagesdatum abhängt.
4. Die Stufe entsteht nur aus serverseitig gewerteten Antworten; kein Endpunkt setzt sie aus Client-Angaben. *Rot, wenn* ein Request-Feld die Stufe beeinflusst.
5. Fällt der Lernstand stark, sinkt die Stufe um höchstens eine je sieben Tage. *Rot, wenn* ein Lauf um zwei Stufen senkt oder gar nicht senkt.
6. Umrechnung von Anteil zu Note läuft über `PercentToGrade`, die einzige Stelle. *Rot, wenn* eine zweite Umrechnung entsteht.
7. Die Anzeige trägt für die Stufen 1 bis 3 den Stand-Text, für 4 bis 6 nur das nächste Ziel. *Rot, wenn* ein Text für Stufe 4 bis 6 ein Stufenwort („Vierer“, „Fünfer“, „Sechser“) enthält.
8. Ein Fach ohne Maß (kein Wortschatz) zeigt keine Stufe. *Rot, wenn* Mathe ohne Beherrschungswert eine bekommt.
9. `BestLevel` sinkt nie. *Rot, wenn* eine Senkung der laufenden Stufe den Bestwert überschreibt.
10. Ein Stufenwechsel bucht nichts auf das Kassenbuch. *Rot, wenn* dabei ein `ChildPointsEntry` entsteht.

## Beziehungen zu anderen Stories (nur gelesen)

- [B-683](B-683-notenstufe-an-der-echten-klausur-eichen.md): echte Note lässt auf- und absteigen und eicht.
- [B-680](B-680-taeglicher-lehrplan-je-fach-just-in-time.md): Ziel des Plans, aber keine Voraussetzung (Punkt 7).
- [B-249](B-249-probeklausur-mit-notenprognose.md) (`idee`, hat G-7-Entscheidung): gemeinsamer Notenschlüssel.
- [B-595](B-595-rang-der-nie-sinkt.md) (`verworfen`, ersetzt durch diese Story).

## Verlauf

- **2026-11-19** — angelegt aus dem Leitbild-Gespräch mit dem Vater ([Leitbild](../leitbild-privatlehrer.md));
  ersetzt die Liga-Idee und [B-595](B-595-rang-der-nie-sinkt.md).
- **2026-11-19** — `idee` → `ausformuliert` (Vorbereitungslauf, Sonnet): Ist-Stand am Code belegt. Kern: Für Vokabeln liegt
  der Lernstand je Fach/Unit bereits serverseitig vor; es fehlen Umrechnung, Ablage und Anzeige – und ein Beherrschungswert
  für Mathe/Grammatik ist nicht belegt. Sieben offene Punkte mit Empfehlung, **nichts entschieden**.
- **2026-11-20** — `ausformuliert` → `gegrillt` (Vorbereitungslauf, Sonnet, stellvertretend auf Anweisung des Nutzers, unbestätigt): Alle sieben Punkte entschieden. Kern: Maß ist der Anteil beherrschter Wörter am ganzen Stoff (nur Vokabel-Fächer), Schlüssel gemeinsam mit B-249, Etikett nur für Stufe 1 bis 3, Senkung höchstens eine Stufe je Woche, Bestwert als Spalte, eigene Tabelle, keine Münzen.
