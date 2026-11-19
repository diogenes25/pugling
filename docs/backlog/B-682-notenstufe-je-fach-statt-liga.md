---
tags: [typ/story, status/ausformuliert, bereich/gamification, bereich/klassenarbeit, rolle/student]
aliases: [Notenstufe, Einser-Schüler, Liga-Ersatz]
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

# B-682 · Notenstufe je Fach: das Kind ist auf dem Stand eines Einser- bis Sechser-Schülers

Statt einer Liga (gegen andere Kinder ausgeschlossen, G-6 vom 2026-10-23) bekommt das Kind je Fach eine
**Notenstufe**: Der Lehrer-Agent weiß, welchen Lernstand ein Schüler für eine 1 braucht, und sagt etwa „Aktuell bist
du auf dem Stand eines Zweier-Schülers“. Das Kind steigt auf, indem es mehr lernt oder schwerere Übungen schafft. Die
Stufe darf **sinken** (Entscheidung des Vaters, 2026-11-19) – verifiziert wird sie an der echten Klausur (B-683). Sie
ist zugleich das Ziel, auf das der tägliche Plan (B-680) hinarbeitet.

**Vorbehalt:** Hängt am Richtungswechsel „Leitbild Privatlehrer“ (Nutzer entscheidet). Nur **ausformuliert**; die
Fragen unter „Offene Punkte“ sind **nicht entschieden**, es gibt keinen Abschnitt `## Entscheidungen`.

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

Jede Frage mit Empfehlung. **Nichts davon ist entschieden.** Rollenaufteilung/Motivationsmodell: Nutzer entscheidet.

1. **Aus welchem Maß entsteht die Stufe?** Empfehlung für den ersten Bau: nur **Vokabel-Fächer**, Maß = Anteil der
   Unit-Wörter mit `MasteryPercent` ab einer Schwelle (beherrscht), gewichtet mit der Menge der Stoff-Wörter bis zur
   nächsten Klausur. Mathe/Grammatik erst, wenn ein Beherrschungswert je Aufgabe belegt ist (siehe oben). Eine Notenstufe
   für ein Fach ohne Maß wäre erfunden.
2. **Wie kommt aus „Anteil“ eine Note?** Empfehlung: **derselbe feste Prozent-Schlüssel wie B-249** (Entscheidung G-7),
   damit die Stufe und die Probeklausur nicht zwei Notenschlüssel haben. Der Schlüssel muss je Fach/Schulart einstellbar bleiben.
3. **Wie heißen die unteren Stufen?** Empfehlung: Anzeige ohne Etikett („nächste Stufe: noch 12 Wörter sicher“), die Note
   nur dort, wo sie positiv wirkt; kein „Sechser-Schüler“ am Kind. Das ist die Produktfrage des Leitbilds, nicht technisch.
4. **Darf die Stufe zwischen Klausuren sinken?** Entscheidung des Vaters (2026-11-19): ja. Empfehlung: im ersten Bau sinkt
   sie **nur durch die echte Klausur** (B-683) und durch nachweislich nachlassenden Lernstand (viele Wörter fallen unter
   `WeakBelowPercent`), nicht durch Tagesform; sonst bricht ein schlechter Tag die Motivation, die die Stufe tragen soll.
5. **Bleibt die beste je erreichte Stufe als Auszeichnung?** Empfehlung: ja, als `Achievement`-artige Auszeichnung ohne
   Münzwert; die laufende Stufe darf sinken, die Auszeichnung nicht. Berührt die Münz-Ökonomie nur, wenn sie
   `RewardPoints` trägt – hier 0 (Nutzer entscheidet bei Bedarf anders).
6. **Wer bekommt die Stufe zu sehen?** Empfehlung: das Kind je Fach, der Vater im Kind-Hub; nie im Vergleich mit anderen
   Kindern (G-6).
7. **Reihenfolge zu B-680:** Empfehlung: B-680 baut zuerst ohne Notenstufe (B-680 Punkt 10); die Stufe ist ein späterer
   Planer-Eingang.

## Akzeptanzkriterien (Entwurf)

Final erst nach Punkt 1 bis 3. Je Kriterium die Änderung, die es rot macht:

1. Hat ein Kind in einem Vokabel-Fach null beherrschte Wörter, liegt seine Stufe unterhalb der besten Stufe; mit allen
   Unit-Wörtern beherrscht liegt sie an der Spitze. *Rot, wenn* die Stufe von der Zahl der Antworten statt von der
   Beherrschung abhängt (Vielübungs-Kind überholt ein sicheres).
2. Dieselben Daten ergeben dieselbe Stufe. *Rot, wenn* die Berechnung von Zufall, Uhrzeit oder einem Sprachmodell abhängt.
3. Die Stufe entsteht nur aus serverseitig gewerteten Antworten (`ItemReviewEvent`); ein Client-Feld kann sie nicht heben.
   *Rot, wenn* ein Endpunkt die Stufe aus Client-Angaben setzt.
4. Fällt der Lernstand (Wörter unter 50 % Beherrschung), sinkt die Stufe beim nächsten Lauf; sie steigt nicht allein durch
   Wiederholen eines einzigen Wortes. *Rot, wenn* ein Fach nach Rückgang unverändert bleibt.
5. Die Stufe nutzt denselben Umrechnungsschlüssel wie die Probeklausur (B-249). *Rot, wenn* zwei Stellen eine Note aus
   einem Anteil rechnen.
6. Die Anzeige für die unteren Stufen nennt das nächste erreichbare Ziel, kein Abstufungs-Etikett. *Rot, wenn* der Text
   „Fünfer“/„Sechser“ am Kind erscheint.
7. Ein Fach ohne Maß zeigt **keine** Stufe statt einer erfundenen. *Rot, wenn* Mathe ohne Beherrschungswert eine Stufe bekommt.

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
