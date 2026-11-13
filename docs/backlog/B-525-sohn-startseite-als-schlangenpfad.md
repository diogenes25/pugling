---
tags: [typ/story, status/geschaetzt, bereich/frontend, bereich/gamification, rolle/student]
aliases: [Schlangenpfad, Lernpfad-Startseite, Pfad statt Kachelliste]
status: geschaetzt
prio: P1
art: Wunsch
groesse: L
wo: beides
migration: nein
vertragsbruch: nein
quelle: "docs/duolingo-snipwise-vergleich-plan.md#entscheidung-am-prototyp (Variante A, Nutzer im Chat 2026-11-13)"
grund: ""
ersetzt_durch: []
wartet_auf: ""
---

# B-525 · Sohn-Startseite als Schlangenpfad

Die Basis der Sohn-Arcade zeigt heute eine Kachelliste der Positionen mit je zwei Knöpfen. Sie soll den
Stoff als **Weg** zeigen, wie die Kinder ihn aus Duolingo und Snipwise kennen: Abschnitts-Banner, Knoten
in Schlangenlinie mit Ring, genau **ein** hervorgehobener nächster Knoten, und die heutige Pflicht mit
Frist und Malus am Knoten. Ein Tipp auf den Knoten öffnet ein Blatt mit einem Hauptknopf.

Gewählt am Prototyp (Variante A von drei, `frontend/src/prototyp/PfadPrototyp.tsx`). Der Lektionsbildschirm
ist [B-526](B-526-lektionsbildschirm-festes-raster.md).

## User Story

Als **Sohn** möchte ich meinen Stoff als Weg mit einem klar markierten nächsten Schritt sehen, damit ich
ohne Nachdenken weiß, was jetzt dran ist, wie weit ich bin und was heute noch Pflicht ist.

## Ist-Stand am Code

**Die Startseite ist eine Liste, sortiert vom Server.**

- `SohnHome.tsx:112` rendert `today.positions` als Karten in der gelieferten Reihenfolge; die Reihenfolge
  ist `Order`, dann `Id` (`PositionProgressService.cs:156-160`).
- Jede Karte trägt bis zu **zwei** Einstiege: „Üben" (`SohnHome.tsx:150`) und „Test"
  (`SohnHome.tsx:155`); welcher sichtbar ist, entscheiden `useLeitner`, `testable`, `poolSize`
  (`SohnHome.tsx:127`).
- Bei mehreren Plänen steht ein `<select>` darüber (`SohnHome.tsx:74-80`). Der Schüler-Endpunkt liefert
  aber nur spielbare Pläne (`StudentPlansController.cs:37-41`), praktisch also genau einen.
- Der Tab „Weg" (`SohnApp.tsx:163`) zeigt dieselben Positionen noch einmal als Statusliste
  (`SohnProgress.tsx:50-59`), dazu Heatmap, Ziele und Abzeichen – keinen Pfad.

**Was die Tagesübersicht je Position liefert** (`Shared/ProgressDtos.cs:17-20`, gebaut in
`PositionProgressService.cs:201-205`): Titel, Typ, `Order`, `Cadence`, `UseLeitner`, `Testable`, `GoalMet`,
`DueCount`, `PoolSize`, `IntroducedCount`, `PointsGoalMet`. Dazu am Tag `DutyDone`, `GoalsTotal`,
`GoalsMet` (`Shared/ProgressDtos.cs:23-25`) und die Belohnungsbox `DailyBox`
(`Student/ProgressDtos.cs:9-10`).

**Was sie nicht liefert:**

- **Keinen Malus.** `PenaltyCoins` steht nur in `PositionResponse` (`StudyPlanDtos.cs:96`), und die
  Positionsliste ist auf `Roles.Supervisor` gegated (`PlanPositionsController.cs:23`). Das Kind kann den
  Betrag heute nicht lesen.
- **Keinen Abschnitt.** `PositionStatus` trägt weder `SeriesUnitId` noch einen Unit-Titel, obwohl jede
  Übung an einer Unit hängt (`LearnEntities.cs:80`).
- **Kein „geschafft".** `GoalMet` ist eine Aussage über die **laufende Periode** (Tag oder Woche,
  `PositionProgressService.cs:109-118`), kein bleibender Zustand. Eine Position ist morgen wieder offen.
- **Keine Stufe.** Die Abfragestufe einer Position hängt am **Plantag**, nicht am Können: `StageForDay`
  liest `StageSchedule` nach `DayNumber` (`PositionPlayService.cs:73-81`). Der Vater legt sie über den
  Kalender fest; das Kind „steigt" nicht auf.
- **Keine gemeinsame Wiederholungsrunde.** Eine Übungssitzung gehört zu genau einer Position
  (`PositionPracticeController.cs:19`). `DueCount` gibt es je Position, eine Runde über mehrere nicht.

**Tests, die an der heutigen Startseite hängen:** `SohnHome.test.tsx` prüft `PositionCard` direkt
(Zeilen 24, 30); fünf E2E-Specs greifen nach den Knöpfen der Kachel (`full-flow`, `karten-umdrehen`,
`test-durchgefallen-ueben`, `ueben-ohne-leitner`, `uebung-abbruch`).

## Die echte Lücke

Die Annahme „reine Darstellung über dem bestehenden Vertrag" hält **nicht ganz**. Vier Dinge, die der
Prototyp zeigt, haben heute keine Quelle:

1. **Der Stufen-Ring hat die falsche Bedeutung.** Im Prototyp füllt er sich durch Spielen. Im echten
   Modell sind Stufen ein Kalender des Vaters. Ehrlich darstellbar ist dagegen der Anteil
   **kennengelernter Inhalte** (`IntroducedCount / PoolSize`) – der ist verdient und schon da.
2. **„Erledigt / dran / gesperrt" gibt es nicht.** Alle Positionen eines Plans sind gleichzeitig
   spielbar. Ein Pfad mit grauen, gesperrten Knoten wäre entweder gelogen oder eine **neue Regel**
   (Reihenfolge erzwingen) – und die gehört nicht in eine Darstellungs-Story.
3. **Der Malus am Knoten braucht ein Vertragsfeld** (additiv, `PenaltyCoins` in `PositionStatus`).
4. **Das Abschnitts-Banner braucht die Unit** (additiv, Unit-Id und -Titel in `PositionStatus`).

Der **Wiederholungs-Knoten** aus dem Prototyp ist in dieser Story nicht baubar: er setzt eine Runde über
mehrere Positionen voraus, und das ist Stufe B des Vergleichsdokuments.

## Offene Punkte

1. ~~Was bedeutet der Ring?~~ → Entscheidung 2.
2. ~~Gibt es gesperrte Knoten?~~ → Entscheidung 1.
3. ~~Malus am Knoten zeigen?~~ → Entscheidung 3.
4. ~~Abschnitts-Banner aus der Unit oder aus dem Plan?~~ → Entscheidung 4.
5. ~~Üben oder Test: was ist der Hauptknopf im Blatt?~~ → Entscheidung 5.
6. ~~Wochenziele: wie heißt die Frist?~~ → Entscheidung 6.
7. ~~Restzeit und Zeitzone~~ → Entscheidung 6.
8. ~~Was wird aus dem Tab „Weg"?~~ → Entscheidung 7.
9. ~~Belohnungsbox als Truhe auf dem Pfad?~~ → Entscheidung 8, zurückgestellt nach
   [B-527](B-527-belohnungsbox-als-truhe-zum-oeffnen.md).
10. ~~Missionen-Panel und Interessen-Karte~~ → Entscheidung 9; der eigene Missionen-Tab ist
    [B-528](B-528-missionen-als-eigener-tab.md).
11. ~~Welcher Knoten ist der nächste?~~ (beim Grillen aufgetaucht) → Entscheidung 10.

**Ausdrücklich zurückgestellt:** die Reihenfolge-Sperre (Entscheidung 1) – keine Story angelegt, weil nicht
entschieden ist, ob es sie je geben soll. **Nicht gegrillt, bleibt wie heute:** der Plan-Umschalter
(`<select>` bei mehr als einem spielbaren Plan) steht unverändert über dem Pfad.

## Entscheidungen

Grill-Runde mit dem Nutzer am 2026-11-13, eine Frage je Entscheidung.

1. **Keine gesperrten Knoten.** Alle Positionen bleiben antippbar und spielbar wie heute. Begründung: Eine
   Sperre bräuchte eine Server-Regel und eine Definition von „geschafft", die es nicht gibt, und eine
   gesperrte Position mit Tagesziel würde Malus kosten, ohne spielbar zu sein. Kosten: Der Pfad sieht
   weniger nach Duolingo aus (kein Grau, kein Schloss), und das Kind kann weiter in beliebiger Reihenfolge
   spielen.
2. **Der Ring zeigt den Kennenlern-Anteil** (`IntroducedCount / PoolSize`) als durchgehenden Bogen; eine
   Position ohne Pool trägt keinen Ring. Das Häkchen „Ziel der laufenden Periode erfüllt" ist ein eigenes
   Zeichen daneben. Begründung: der Anteil ist verdient, bewegt sich bei jeder Runde und liegt schon im
   Vertrag; Stufen sind ein Kalender des Vaters. Kosten: Ein voller Ring sieht fertig aus, obwohl die
   Wörter noch nicht sitzen und die Pflicht weiterläuft – das Häkchen muss diese Verwechslung abfangen.
3. **Der offene Pflicht-Knoten zeigt den Malus als Betrag.** `PositionStatus` bekommt additiv
   `PenaltyCoins`; bei 0 steht nur die Frist. Begründung: Verpasste Pflicht kostet – das wirkt nur, wenn das
   Kind den Preis vorher kennt, und ist fairer als eine überraschende Abbuchung. Kosten: Die Story wird
   `wo: beides` (Vertrag, OpenAPI-Dokument, `DocsCaptureTests`-Beispiele ziehen nach); kein Vertragsbruch.
4. **Das Abschnitts-Banner kommt aus der Unit.** `PositionStatus` bekommt additiv Unit-Id und Unit-Titel.
   Aufeinanderfolgende Positionen derselben Unit bilden einen Abschnitt; der Plantitel steht einmal ganz
   oben. Begründung: Die Unit ist die Gliederung, die das Kind aus dem Buch kennt. Kosten: Mischt ein Plan
   die Units, entstehen viele kurze Abschnitte – gewollt als ehrlicher Hinweis, den Plan zu ordnen; ob die
   echten Pläne das tun, ist **nicht nachgesehen**.
5. **Der Hauptknopf im Blatt heißt „Los" und startet die Kette Üben → Test.** Ist nichts zu üben (nichts
   fällig, alles kennengelernt) und meldet der Server `testable`, startet „Los" direkt den Test. Der Test
   bleibt zusätzlich als kleiner zweiter Knopf im Blatt, wenn `testable`. Begründung: ein Knopf, ein Tipp,
   und trotzdem bis zur Pflichterfüllung geführt – nur der bestandene Test erfüllt das Ziel
   (`PositionProgressService.cs:115-117`). Kosten: Wer nach der Übungsrunde abbricht, hat die Pflicht nicht
   erfüllt; das Blatt muss das beim nächsten Öffnen erkennbar lassen (kein Häkchen).
6. **Fristen nach Server-Uhr.** Tagesziel: Stunden bis zum UTC-Tagesende; Wochenziel: „bis Sonntag", am
   letzten Tag der Woche Stunden. Begründung: Eine Anzeige, die der Buchung widerspricht, untergräbt das
   Vertrauen in den Malus. Kosten: Um 23 Uhr Ortszeit steht „noch 2 Std." – das Kind lernt, dass der Tag
   erst um 1 bzw. 2 Uhr endet. Die Tagesgrenze selbst ist bestehender Zustand und nicht Teil dieser Story.
7. **Die Startseite heißt „Pfad", der bisherige Tab „Weg" heißt „Erfolge".** „Erfolge" behält
   7-Tage-Übersicht, große Ziele und Abzeichen und verliert die Positionsliste. Begründung: sonst führten
   zwei Tabs zu „Weg", und die Liste stünde doppelt. Kosten: zwei geänderte Tab-Namen in `de.ts`/`en.ts`,
   in den E2E-Specs, die danach greifen, und in `docs/sohn-app-funktionsbeschreibung.md`.
8. **Keine Truhe in dieser Story.** Begründung: Die Belohnungsbox wird vom Server von selbst vergeben; eine
   Truhe, die nur anzeigt, ist die schwächste Fassung. Kosten: Der Pfad hat kein Belohnungs-Element auf dem
   Weg. Die Truhe mit echtem Öffnen ist [B-527](B-527-belohnungsbox-als-truhe-zum-oeffnen.md).
9. **Missionen wandern in den Tab „Erfolge", die Interessen-Karte bleibt oben auf dem Pfad.** Begründung:
   Der Pfad soll ohne Scrollen bis zum nächsten Knoten sichtbar sein. Kosten: Tagesmissionen stehen eine
   Ebene weg vom Spielen und werden womöglich seltener gesehen; die einmalige Feier neu erfüllter Missionen
   hängt am Panel und feuert dann erst beim Besuch von „Erfolge". Ein eigener Missionen-Tab ist
   [B-528](B-528-missionen-als-eigener-tab.md).
10. **Der nächste Knoten** ist die erste Position (in Plan-Reihenfolge) mit offenem **Tagesziel**, sonst
    die erste mit offenem **Wochenziel**, sonst **keiner**: dann zeigt der Pfad „Heute geschafft", und alle
    Knoten bleiben antippbar. Kein freiwilliger „START". Begründung: Die Grenze zwischen Pflicht und Kür
    bleibt scharf; die Kür bekommt in Stufe B ihren eigenen Ort. Kosten: Nach erfüllter Pflicht fordert
    nichts zum Weiterlernen auf, und ein offenes Wochenziel wird auch am Sonntagabend erst hervorgehoben,
    wenn alle Tagesziele erfüllt sind (seine Frist steht trotzdem am Knoten).

## Akzeptanzkriterien

1. Die Startseite zeigt den Plantitel einmal oben und darunter die Positionen des spielbaren Plans als
   Knoten in Schlangenlinie, in der Reihenfolge des Servers. Jeder Knoten ist antippbar; keiner ist gesperrt.
2. Aufeinanderfolgende Positionen derselben Unit stehen unter einem Banner mit dem Unit-Titel; wechselt die
   Unit, beginnt ein neues Banner.
3. Der Ring eines Knotens zeigt `IntroducedCount / PoolSize`; eine Position mit `PoolSize = 0` hat keinen Ring.
4. Ein Knoten mit erfülltem Ziel der laufenden Periode trägt ein Häkchen; eine freie Position
   (`Cadence = None`) trägt nie eines, auch wenn der Server sie als erfüllt meldet.
5. Genau ein Knoten ist als nächster hervorgehoben: die erste Position mit offenem Tagesziel, sonst die
   erste mit offenem Wochenziel. Sind alle Pflichtziele erfüllt, ist keiner hervorgehoben und der Pfad zeigt
   „Heute geschafft". Ein Plan nur aus freien Positionen hebt keinen Knoten hervor.
6. Jeder Pflicht-Knoten mit offenem Ziel zeigt seine Frist nach Server-Uhr (Tagesziel: Stunden bis zum
   UTC-Tagesende; Wochenziel: „bis Sonntag", am letzten Tag der Woche Stunden) und, wenn `PenaltyCoins > 0`,
   den Malus als Betrag. Bei `PenaltyCoins = 0` steht kein Betrag da.
7. `PositionStatus` trägt `PenaltyCoins`, die Unit-Id und den Unit-Titel; ein Integrationstest belegt die
   Werte für ein Kind an seinem eigenen Plan.
8. Ein Tipp auf einen Knoten öffnet ein Blatt mit Titel, Kennenlern-Stand, Frist und Malus und dem
   Hauptknopf „Los". „Los" startet das Üben; ist nichts zu üben und `testable = true`, startet es den Test.
9. Meldet der Server `testable = true`, bietet das Blatt den Test zusätzlich als zweiten Knopf an; bei
   `testable = false` gibt es keinen Test-Knopf, und die Position bleibt über „Los" spielbar.
10. Der Tab der Startseite heißt „Pfad", der bisherige Tab „Weg" heißt „Erfolge". „Erfolge" zeigt
    7-Tage-Übersicht, große Ziele, Abzeichen und das Missionen-Panel, aber keine Positionsliste mehr; die
    Startseite zeigt kein Missionen-Panel mehr. Die Interessen-Karte steht weiter oben auf der Startseite.
11. Der Pfad ist per Tastatur und Screenreader bedienbar (Knoten sind Knöpfe mit Namen, Stand und Frist),
    und „Bewegung reduzieren" schaltet die Animationen ab.
12. Alle neuen Texte laufen über `t()` in Deutsch und Englisch.
13. Der Vater→Sohn-Durchstich (`full-flow.spec.ts`) und die vier weiteren Specs, die heute nach den
    Kachel-Knöpfen greifen, fahren über den Pfad und sind grün.

## Schätzung

**Größe: L.** Der Backend-Teil allein wäre XS (drei Felder an einem Record, eine Konstruktor-Stelle). Die
Größe kommt aus dem Frontend: Die Startseite wird neu gebaut, ein neues Blatt kommt dazu, zwei Tabs werden
umbenannt und umsortiert, und sieben E2E-Specs hängen an dem, was sich ändert. Das liegt deutlich über dem
M-Anker (ein Batch-Pfad in einem Dienst) und bleibt unter XL, weil keine Regel und kein Schema sich ändert.

- **`migration: nein`** – nachgesehen: Malus (`PlanPosition.PenaltyCoins`) und Unit
  (`Exercise.SeriesUnitId`, `LearnEntities.cs:80`) liegen schon in der DB; es kommt keine Spalte dazu.
- **`vertragsbruch: nein`** – nachgesehen: `PositionStatus` wird an genau **einer** Stelle gebaut
  (`PositionProgressService.cs:201`), `Pugling.Client` liest die Tagesübersicht nicht (kein Treffer für
  `overview`/`PositionStatus` außer dem Vater-`daily-overview`), und die drei Felder sind in einer
  **Antwort** additiv – die `unknown_field`-Abweisung gilt nur für Anfragen.

**Risiken**

1. **Sieben Specs, nicht fünf.** Zu den fünf, die nach den Kachel-Knöpfen greifen, kommen
   `sprache.spec.ts:19,24,76` (Tab „Basis") und `vater-von-null.spec.ts:448` (Tab „Weg"). Jede fährt einen
   anderen Weg; das Umstellen ist der größte Einzelposten und der, an dem am ehesten etwas flackert.
2. **Laufende Arbeit im selben Checkout.** Im Index liegen die gestagten Änderungen von B-524, darunter
   `SohnPractice.tsx`, `de.ts`, `en.ts` und das OpenAPI-Dokument. B-525 fasst `de.ts`/`en.ts` und das
   Dokument ebenfalls an. Gebaut wird erst, wenn B-524 committet ist.
3. **`ComputeDayAsync` läuft einmal je Tag der Historie** (Kommentar `PositionProgressService.cs:180`). Der
   Unit-Titel muss über **einen** zusätzlichen `Include` in `LoadPositionsAsync` kommen, nicht über eine
   Abfrage je Position – sonst multipliziert er sich mit Tagen und Positionen.
4. **Byte-stabile Doku.** `DocsCaptureTests` schreibt `docs/api-examples` und der Testlauf das
   OpenAPI-Dokument neu; beide ändern sich durch die Felder und gehören in den Commit.
5. **Frist aus der Geräte-Uhr.** Die Stunden bis zum UTC-Tagesende rechnet das Frontend aus der Uhr des
   Handys. Geht sie falsch, stimmt die Anzeige nicht; die Buchung bleibt richtig. Hingenommen – ein
   Server-Feld „Periodenende" wäre die saubere Fassung und eine eigene Story.
6. **Feier neu erfüllter Missionen** hängt am Panel (`GamificationPanels.tsx:36`) und feuert nach dem
   Umzug erst beim Besuch von „Erfolge" (Kosten von Entscheidung 9). Kein Fehler, aber im Rollengang ansehen.
7. **Der Prototyp ist Wegwerf-Code** (keine `t()`, kein `useAction`, lokale Bewertung). Übernommen wird die
   Geometrie und das CSS als Vorlage, nicht die Komponenten.

**Angriffsplan** (Backend zuerst)

1. `PositionStatus` um `PenaltyCoins`, `SeriesUnitId`, `SeriesUnitTitle` erweitern; `LoadPositionsAsync`
   lädt die Unit mit. Integrationstest, Lauf erzeugt OpenAPI-Dokument und `api-examples`.
2. `npm run gen:contract`; die Fixture in `SohnHome.test.tsx` zieht die neuen Felder nach.
3. Die Regeln als **reine Funktionen** in einer eigenen Datei unter `frontend/src/sohn/`: Abschnitte aus
   aufeinanderfolgenden Units, nächster Knoten (Entscheidung 10), Frist-Text (Entscheidung 6),
   Hauptknopf-Ziel (Entscheidung 5). Vitest je Regel, bevor eine Komponente sie benutzt.
4. Pfad-Komponente und Knoten-Blatt; `SohnHome` rendert sie statt der Kachelliste. Texte über `t()`.
5. Tabs: `nav.home` → „Pfad", `nav.path` → „Erfolge"; `SohnProgress` verliert die Positionsliste und
   bekommt das Missionen-Panel.
6. Die sieben Specs umstellen; dazu eine neue Spec für den Pfad selbst (eine Fläche je Spec).
7. `docs/sohn-app-funktionsbeschreibung.md` nachziehen (Abschnitte 3.2, 4.2, 4.5, 5).
8. Prototyp aus dem Arbeitsbaum nehmen (`frontend/src/prototyp/`, Route in `App.tsx`, Eintrag
   `prototyp-pfad` in `.claude/launch.json`) und auf einem Wegwerf-Branch festhalten.

**Testweg**

- **Integration:** `PositionGoalOverviewTests` – ein Fall, der für ein Kind an seinem Plan `penaltyCoins`,
  `seriesUnitId` und `seriesUnitTitle` der Tagesübersicht gegen die angelegten Werte prüft (AK 7).
- **Vitest:** die Regel-Datei aus Schritt 3 (AK 2, 4, 5, 6, 8, 9 je mit dem Fall, der sie rot macht:
  freie Position ohne Häkchen, Wochenziel erst nach allen Tageszielen, `PenaltyCoins = 0` ohne Betrag,
  `PoolSize = 0` ohne Ring, „Los" führt bei nichts Fälligem zum Test); `SohnHome.test.tsx` umgeschrieben
  auf den Knoten.
- **E2E (zugleich der Rollengang):** `full-flow.spec.ts` über den Pfad bis zum bestandenen Test und dem
  Häkchen am Knoten; neue `pfad.spec.ts` für Banner, hervorgehobenen Knoten, Frist mit Malus und Blatt;
  `sprache.spec.ts` und `vater-von-null.spec.ts` mit den neuen Tab-Namen. Gemessen an „N passed", nicht am
  Exit-Code.
- **Reviewer:** `pugling-reviewer` für Schritt 1, `frontend-reviewer` für den Rest.
- **Bleibt beim Menschen:** ob sich der Pfad für die Kinder richtig anfühlt – das kann keine Spec.

## Verlauf

- 2026-11-13 · `idee` – aufgenommen nach der Variantenwahl am Prototyp; Prio P1 ist ein Vorschlag.
- 2026-11-13 · `ausformuliert` – gegen den Code belegt: der Vertrag trägt weder Malus, Abschnitt noch einen
  bleibenden Erledigt-Zustand, und Stufen folgen dem Plantag statt dem Können. `wo` von `frontend` auf
  `beides` (additive Felder in `PositionStatus`, falls Punkt 3 und 4 so entschieden werden).
- 2026-11-13 · `gegrillt` – zehn Entscheidungen im Dialog mit dem Nutzer, alle gemäß Empfehlung; Truhe und
  Missionen-Tab als B-527 und B-528 abgespalten, Reihenfolge-Sperre zurückgestellt.
- 2026-11-13 · `geschaetzt` – L, keine Migration, kein Vertragsbruch (beides nachgesehen); sieben betroffene E2E-Specs statt fünf; Bau erst nach dem Commit von B-524.
