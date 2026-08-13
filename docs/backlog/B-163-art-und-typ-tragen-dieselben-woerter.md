---
tags: [typ/story, status/gegrillt, bereich/katalog, rolle/creator, rolle/supervisor]
aliases: [Art gegen Typ, Vokabeln heißt zweimal etwas anderes]
status: gegrillt
prio: P2
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: B-157 (Grill-Runde 2026-08-13, Entscheidung 5)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-163 · „Art" und „Typ" tragen dieselben Wörter — in einer Zeile sogar zweimal hintereinander

Die Übungssuche hat zwei unabhängige Achsen. Von den sieben geseedeten Arten kollidieren **fünf** mit einem
Übungstyp-Namen, und in der Auswahlliste des Planbaus stehen beide Werte punktgetrennt nebeneinander — beim
Seed-Bestand heißt das Zeilen wie „Begrüßungen · **Vokabeln** · **Vokabeln**".

## User Story

Als *Supervisor* möchte ich in der Übungsliste erkennen, welches Wort das Lernverfahren benennt und welches
den Ordnungsbegriff, damit ich beim Planbau nicht rate, was ich gerade auswähle.

## Ist-Stand am Code

**Die zwei Achsen und ihre Herkunft — und die ist entscheidend für die Reparatur.**

- Der **Typ** ist das Lernverfahren; sein Anzeigename ist eine **Konstante im Code** und kommt über
  `GET creator/exercise-types` aus dem Manifest (`frontend/CLAUDE.md`: „Anzeigename und Routen-Segment kommen
  aus dem Typ-Manifest"). Zwölf Typen, je eine Zeile in `backend/Pugling.Api/Exercises/` (z. B.
  `VocabularyExerciseType.cs:18` → „Vokabeln", `BuiltInExerciseTypes.cs:22` → „Leseverständnis").
- Die **Art** (`ExerciseCategory`) ist ein freier Ordnungsbegriff je Fach; ihr Name ist eine
  **Datenbankspalte** (`Models/LearnEntities.cs:40-46`), im Seed gesetzt und seit
  [B-157](B-157-kategorien-unter-fremdem-fach-ungeschuetzt.md) an den geseedeten Fächern für **niemanden**
  mehr änderbar.

**Die Kollisionen, ausgezählt** (das war der offene Punkt der Idee):

| Wort | als Typ | als Art | Fälle im Seed |
| --- | --- | --- | --- |
| **Vokabeln** | `Vocabulary` (`VocabularyExerciseType.cs:18`) | `Seed.cs:605`, `:997` | 2 (Französisch, Englisch) |
| **Grammatik** | `Grammar` (`BuiltInExerciseTypes.cs`) | `Seed.cs:606`, `:998` | 2 (Französisch, Englisch) |
| **Leseverstehen** / **Leseverständnis** | `Reading` → „Leseverständnis" | `Seed.cs:999` → „Leseverstehen" | 1 (Englisch) |

Von **sieben** geseedeten Arten kollidieren damit **fünf**; unbetroffen sind nur „Grundrechenarten" und
„Algebra" (`Seed.cs:1002-1003`). Die Idee sprach von zwei Fundstellen — „Grammatik" war übersehen, und das
ist die folgenreichste, weil sie auf beiden Achsen der *natürliche* Name ist.

**Wo es sichtbar wird, in aufsteigender Schwere:**

1. **`ExerciseFilterBar.tsx:96-108`** — zwei Auswahlfelder mit den sichtbaren Beschriftungen „Typ" und
   „Art" untereinander. Hier ist die Achse benannt; verwirrend sind nur die Werte.
2. **`VaterWizard.tsx:492-499`** — dieselben zwei Felder, aber **ohne** sichtbare Beschriftung: nur die
   Platzhalter „– alle Arten –" und „– alle Typen –" (`aria-label` trägt „Art" bzw. „Übungstyp"). Wer den
   Platzhalter durch eine Auswahl ersetzt hat, sieht die Achse nicht mehr.
3. **`PlanPositions.tsx:431-434`** — der schlimmste Fall und **heute reproduzierbar**: Titel, Typ-Label,
   Klassenstufe, Art und Quelle stehen punktgetrennt in *einer* Zeile, ohne ein Wort, das sagt welches
   welches ist:

   ```text
   {ex.title} · {typeLabel(ex.type)} · Kl. 5–7 · {ex.categoryName} · {ex.source}
   ```

   Mit dem Seed-Bestand ergibt das unter anderem:

   - „**Begrüßungen** · Vokabeln · Vokabeln" (`Seed.cs:1055`, Typ `Vocabulary`, Art „Vokabeln")
   - „**Vokabeln: En ville** · Vokabeln · Vokabeln" (`Seed.cs:639`) — dasselbe Wort **dreimal** in einer
     Zeile, einmal als Titel, einmal als Verfahren, einmal als Ordnungsbegriff

   Und das ist genau die Zeile, auf der der Supervisor beim **Planbau** seine Übungen anhakt.

**Der Vertrag hält die zwei Achsen sauber getrennt** — das Problem ist rein die Anzeige:
`ExerciseSummary`/`ExerciseResponse` tragen `Type` (der **Schlüssel**, nicht das Label) neben
`CategoryId`/`CategoryName` (`Contracts/Creator/ExerciseCatalogDtos.cs:14,26`). Ein Client kann sie also
unterscheiden; er tut es beim Rendern nur nicht.

**Wie viele Stellen behaupten ein Typ-Label als Literal?** Nachgezählt: **eine** —
`frontend/e2e/uebungstypen.spec.ts:139` führt die Liste
`["Leseverständnis", "Hörverständnis", "Aufsatz", "Grammatik", "Übersetzung", "Rechen-Drill"]`. Eine zweite
Stelle ist beim Bauen zu prüfen (`vater-von-null.spec.ts:241`, „Grammatik" — Art oder Typ ist dort nicht
eindeutig). Alle übrigen Treffer auf „Vokabeln"/„Grammatik" in Tests und Frontend sind **Art**-Namen in
Fixtures und bleiben unberührt.

## Die echte Lücke

Nicht die Doppelbelegung an sich — zwei Achsen dürfen sich Vokabular teilen, solange die Anzeige sagt, welche
gemeint ist. Die Lücke ist, dass die **eine Stelle, an der es zählt** (die Auswahlliste des Planbaus), beide
Werte als namenlose, punktgetrennte Fragmente zeigt. Und dass die Reparatur eine Asymmetrie hat, die die Idee
falsch vermutet hatte: **das Typ-Label ist billig zu ändern, der Art-Name teuer.** Das Label ist eine
Code-Konstante und wirkt beim nächsten Request in *jeder* bestehenden Datenbank; der Art-Name liegt in der DB
und ist seit B-157 an geseedeten Fächern gar nicht mehr änderbar.

## Offene Punkte

Alle in der Grill-Runde vom 2026-08-13 geschlossen (Nummern zeigen auf die Entscheidungen).

1. ~~Welche Achse weicht aus?~~ → Entscheidungen 1 und 2.
2. ~~Reicht es, die Werte zu entzerren?~~ → Entscheidung 4. Die Antwort hat sich dabei **verschoben**: nach
   den Entscheidungen 1–3 gibt es keine identischen Wörter mehr, die Kennzeichnung behebt also keinen Defekt
   mehr, sondern nur noch Unklarheit — und darum nur an *einer* Stelle.
3. ~~Zieht `VaterWizard` mit?~~ → Entscheidung 5.
4. ~~Ist „Art" überhaupt der richtige Begriff?~~ → Entscheidung 6 (zurückgestellt).
5. ~~Vor oder nach B-155?~~ → Entscheidung 7. Die Empfehlung der Ausformulierung wurde **abgeschwächt**: sie
   behauptete drei Achsen mit dem Wort „Grammatik", aber Entscheidung 1 nimmt den Typ aus dem Spiel.

In der Runde **neu aufgetaucht** und mitentschieden: die konkreten Namen (Entscheidung 2) und die
Fast-Kollision „Leseverstehen"/„Leseverständnis", die Entscheidung 1 nicht abdeckt (Entscheidung 3).

## Entscheidungen

1. **Die Typ-Achse benennt Verfahren oder Form, die Art-Achse den Stoff** — und die zwei Typ-Namen, die
   dagegen verstoßen, weichen. Begründung: Die zwölf Typ-Namen benennen heute vier verschiedene Dinge —
   Verfahren (Birkenbihl, Lückentext, Zuordnung, Rechen-Drill, Übersetzung), Kompetenz (Lese-/Hörverständnis),
   Aufgabenform (Aufsatz, Rechenaufgaben, Liste) und **Stoff** (Vokabeln, Grammatik). Die Art-Achse ist die
   Achse *für* Stoff; jeder stoff-benannte Typ **muss** darum mit ihr kollidieren, und es kollidieren genau
   die zwei. Der Typ ist außerdem die billigere Seite: sein Label ist eine Code-Konstante und wirkt beim
   nächsten Request in *jeder* bestehenden Datenbank. *Kosten:* Zwei eingeführte Namen ändern sich für alle
   Nutzer, und die Achse bleibt **in sich uneinheitlich** — Kompetenz- und Formnamen bleiben stehen. Wir
   beheben die Kollision, nicht die Unordnung.
2. **„Vokabeln" → „Vokabelkarten", „Grammatik" → „Regelaufgaben".** Begründung: Beide benennen die Form, wie
   der Code sie selbst schon nennt — das Manifest trägt `Renderer: "flashcards"` bzw. `"prompts"`, und die
   Grammatik-Fähigkeit heißt `ruleHints`. „Vokabelkarten" behält den erkennbaren Wortstamm, niemand muss den
   Typ neu lernen. *Kosten:* „Vokabelkarten" und die Art „Vokabeln" teilen weiter den Stamm — in der
   Auswahlliste steht „Begrüßungen · Vokabelkarten · Vokabeln": unterscheidbar, aber ähnlich. Die reineren
   Namen („Karteikarten", „Frage & Antwort") sind verworfen, weil der erste den Vokabel-Bezug verliert und der
   zweite so allgemein ist, dass sich ein künftiger Typ daran vorbeibenennen müsste.
3. **Die Seed-Art „Leseverstehen" weicht, nicht der Typ „Leseverständnis"** (→ „Lesetexte" o. Ä.).
   Begründung: „Leseverständnis" ist ein **Kompetenz**-Name und nach Entscheidung 1 legitim; die
   Fast-Kollision liegt also auf der Art-Seite. Dass eine Seed-Änderung nur **frische** Datenbanken erreicht,
   ist hier ausdrücklich **kein** Mangel — der Seed *ist* der Inhalt frischer Datenbanken; die B-157-Sperre
   beißt nur, wenn man bestehende reparieren will. *Kosten:* Eine inhaltliche Meinung mehr im Seed, und
   bestehende Datenbanken behalten „Leseverstehen" — dort ist es allerdings längst die Wahl des Nutzers.
4. **In `PlanPositions` wird nur die Art gekennzeichnet** („Art: …"), Typ, Klassenstufe und Quelle bleiben
   nackt. Begründung: Der Typ kommt aus zwölf festen Werten, die ein Supervisor lernt; die Art erfindet jeder
   Creator je Fach **frei**, ihr Wortschatz ist unbegrenzt — nur bei ihr kann der Leser die Achse
   grundsätzlich nicht kennen. *Kosten:* Die Zeile wird ein Wort länger, und die **Ungleichbehandlung** der
   Fragmente braucht einen Kommentar, sonst zieht sie ein späterer Umbau glatt.
5. **Der Assistent bekommt sichtbare Beschriftungen** für Art- und Typ-Filter, wie sie
   `ExerciseFilterBar.tsx:96-108` schon hat. Begründung: In `VaterWizard.tsx:492-499` steht die Achse nur im
   Platzhalter — der verschwindet, sobald etwas ausgewählt ist. Danach ist die Oberfläche für Screenreader
   besser beschriftet (`aria-label`) als für Sehende, und zwar genau dort, wo zwei ähnliche Wortlisten
   nebeneinanderstehen. *Kosten:* zwei Beschriftungen mehr in einer schon dichten Filterzeile.
6. **Der Begriff „Art" bleibt — zurückgestellt, nicht entschieden.** Begründung: Die Umbenennung der Achse
   wäre teurer als die Entzerrung der Werte (UI an fünf Stellen, `ExerciseCategory` im Code, die Entity-Doku,
   `CatalogAdmin`, zwei Filterleisten) und hätte diese Story von klein auf `M` gehoben. *Kosten:* „Art" bleibt
   das allgemeinste Wort, das die Sprache hergibt, und kollidiert darum grundsätzlich weiter leicht mit
   allem — die Frage kommt wieder, dann aber nicht als Defekt.
7. **B-155 hängt nicht von dieser Story ab, erbt aber zwei Auflagen.** Begründung: Nach Entscheidung 1 ist der
   Typ „Grammatik" weg; übrig bleiben die Art „Grammatik" und B-155s „Grammatik-Thema" — ein Kompositum auf
   einer anderen Ebene, unterscheidbar. Die Ausformulierung hatte hier drei kollidierende Achsen behauptet;
   das trifft nach Entscheidung 1 nicht mehr zu. B-155 darf also unabhängig gebaut werden, muss aber (a) seine
   **achte** Facette sichtbar beschriften (Entscheidung 5) und (b) keinen neu stoff-benannten Typ einführen
   (Entscheidung 1). *Kosten:* Zwei Auflagen an einer fremden Story — sie müssen **dort** als Zeile stehen,
   sonst wirken sie nicht; nachgetragen am 2026-08-13.

## Akzeptanzkriterien

1. Kein Übungstyp-Anzeigename ist mit einem geseedeten Art-Namen identisch **oder** verwechselbar nah.
2. Das Manifest trägt „Vokabelkarten" und „Regelaufgaben"; der Anzeigename kommt weiter **ausschließlich**
   von dort — keine Tabelle im Frontend (`frontend/CLAUDE.md`).
3. Die geseedete Art „Leseverstehen" trägt einen Namen, der nicht mit dem Typ „Leseverständnis" verwechselbar
   ist.
4. In der Auswahlliste des Planbaus (`PlanPositions`) trägt **die Art** ein Etikett; Typ, Klassenstufe und
   Quelle bleiben unbeschriftet, und ein Kommentar sagt, warum das kein Versehen ist.
5. Im Assistenten tragen der Art- und der Typ-Filter eine sichtbare Beschriftung.
6. **Ein Test hält die Nicht-Kollision**, statt sie einmalig herzustellen: er vergleicht **alle**
   Typ-Anzeigenamen gegen **alle** geseedeten Art-Namen und wird rot, sobald ein neuer Typ wie eine Art heißt.
   Beide Listen liegen im Backend, der Vergleich ist also mechanisierbar.
7. `frontend/e2e/uebungstypen.spec.ts` bleibt grün — seine Label-Liste (`:139`) ist nachgezogen.
8. Die zwei Auflagen aus Entscheidung 7 stehen als Zeile in [B-155](B-155-grammatik-themen-als-tags.md).

## Verlauf

- **2026-08-13** — angelegt beim Grillen von
  [B-157](B-157-kategorien-unter-fremdem-fach-ungeschuetzt.md) (Entscheidung 5). **Bewusst nicht dort
  mitgenommen:** B-157 ist eine Eigentums-Story, ihr Ziel ist ohne die Entzerrung erfüllt, und eine
  Umbenennung von Produktinhalt daran zu hängen hätte ihre Akzeptanzkriterien unscharf gemacht.
- **2026-08-13** — Prio **P3 → P2** und damit vorgezogen, auf Entscheid des Nutzers. Der Grund ist nicht
  gestiegene Wichtigkeit, sondern ein **geschlossenes Fenster**: Mit der Abnahme von B-157 am selben Tag sind
  die sieben Seed-Arten fail-closed — `PATCH` liefert für **jeden** `403 not_owner`.
- **2026-08-13** — `idee → ausformuliert`. Die Recherche hat drei Dinge verschoben: **(1)** Es sind nicht zwei
  Kollisionen, sondern **fünf von sieben** geseedeten Arten — „Grammatik" war übersehen und ist die
  folgenreichste. **(2)** Der Schaden sitzt nicht im Filter, sondern in `PlanPositions.tsx:431-434`, wo Typ und
  Art punktgetrennt in *einer* Zeile stehen; der Seed erzeugt damit heute „Begrüßungen · Vokabeln · Vokabeln"
  und sogar „Vokabeln: En ville · Vokabeln · Vokabeln". **(3)** Die Empfehlung dieser Story wird damit
  **umgekehrt**: nicht der Art-Name weicht aus, sondern das **Typ-Label**. Dazu gefunden: eine **dritte** Achse
  mit demselben Wort ist mit B-155 unterwegs. `unverifiziert` entfernt.
- **2026-08-13** — `ausformuliert → gegrillt`. Sieben Entscheidungen im Dialog. Die tragende ist eine
  **Begriffsschärfung**: die zwölf Typ-Namen benennen vier verschiedene Dinge, und die Art-Achse ist die Achse
  *für Stoff* — jeder stoff-benannte Typ muss darum kollidieren, und es kollidieren genau die zwei. Damit war
  die Frage nicht mehr „welche Achse gibt nach", sondern ob die Typ-Achse ihre eigene Regel einhält.
  **Zwei Empfehlungen der Ausformulierung wurden dabei korrigiert:** die Kennzeichnung behebt nach den
  Entscheidungen 1–3 keinen Defekt mehr (darum nur an einer Stelle statt überall), und die behauptete
  Dreifach-Kollision mit B-155 löst sich auf, sobald der Typ „Grammatik" weicht — B-155 wartet also nicht.
  Neu aufgetaucht und mitentschieden: die konkreten Namen und die Fast-Kollision, die Entscheidung 1 nicht
  abdeckt. Zur Größe nachgesehen: nur **eine** E2E-Label-Liste behauptet Typ-Labels als Literal
  (`uebungstypen.spec.ts:139`), alle anderen Treffer auf „Vokabeln"/„Grammatik" sind Art-Namen in Fixtures.
