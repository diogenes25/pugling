---
tags: [typ/story, status/geschaetzt, bereich/frontend, rolle/student]
aliases: [Mehrfeld-Eingabe Lösungsmenge, Knopf keine Lösung]
status: geschaetzt
prio: P2
art: Wunsch
groesse: M
wo: beides
migration: nein
vertragsbruch: nein
quelle: B-640
grund: ""
ersetzt_durch: []
wartet_auf: ""
---

# B-712 · Sohn-Eingabe für Lösungsmengen: getrennte Felder und Knopf „keine Lösung“

Dritter Teil von [B-640](B-640-uebungstyp-gleichung-loesen.md): statt `2;4` in **ein** Textfeld zu tippen
([B-710](B-710-gleichung-loesen-typ-und-editor.md)), füllt das Kind **einzelne Felder** (Anzahl wählbar) und kann „keine Lösung“
antippen. Die Karte bindet beides zu dem String, den der Server schon versteht.

**Reihenfolge:** nach B-710; unabhängig von [B-711](B-711-loesungsmenge-auswaehlen.md). Die Karte muss vom Server erfahren,
**dass** sie eine Mengen-Eingabe zeigt (Entscheidung 1), denn das weiß sie bisher nicht.

## User Story

Als **Schüler** möchte ich meine Lösungen in getrennte Felder eintragen und „keine Lösung“ antippen können, damit ich nicht
wissen muss, dass ein Semikolon mehrere Lösungen trennt.

## Ist-Stand am Code

Recherchiert am 2026-11-19 gegen `47791819`.

- **Die Karte kennt nur ein Eingabefeld je Form.** `Antwortflaeche` (`frontend/src/sohn/Lektion.tsx:151`–`:195`) wählt zwischen
  Auswahlknöpfen (`karte.choices`), Buchstabenkacheln (`letterTiles`), Buchstabenkästchen (`answerLength`) und einem
  Textfeld; `onWert(string)` trägt den Wert. Mehrere Felder und ein Knopf „keine Lösung“: nicht gefunden.
- **Die Karte erfährt ihre Eingabeform vom Server** über Felder wie `answerLength`, `letterTiles`, `choices`
  (`LektionsKarte`); ein Feld „Mengen-Eingabe“ gibt es nicht. Woher `LektionsKarte` seine Felder bekommt (Antworten der Üben-
  und Test-Endpunkte), ist beim Bau zu lesen – **nicht nachgesehen**.
- **Fokus- und Tastaturregeln:** die Fläche wird beim Kartenwechsel **nicht** neu eingehängt (Review B-526), damit der Fokus
  bleibt und am Handy die Tastatur nicht zuklappt (`Lektion.tsx:140`–`:145`); Enter im Formular schickt ab.
- **Zifferntastatur:** `inputMode="decimal"` an einem Zahlenfeld: nicht gefunden ([B-639](B-639-dezimalkomma-im-zahlenvergleich.md)
  hat sie ausdrücklich ausgeklammert).
- **Die Bindung ist beschlossen:** `x1;x2` aufsteigend, leere Menge = `keine Lösung`, leere Eingabe falsch (B-710,
  Entscheidung 2).

## Die echte Lücke

Ein **Mehrfeld-Eingabeelement** in `Antwortflaeche` und ein **Hinweis vom Server**, dass diese Karte es braucht. Das Element
folgt den Regeln der anderen Eingaben: Fokus bleibt, Enter schickt ab, Sperre per `gesperrt`.

## Offene Punkte

Keine; wie die Karte erfährt, dass sie die Mengen-Eingabe zeigt, ist in Entscheidung 1 entschieden.

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-19 (Sonnet), unbestätigt.*

1. **Der Server sagt es der Karte mit einem neuen, additiven Feld `setAnswer` (boolean) an der Karte.** Begründung: ein
   Frontend, das die Eingabeform aus dem Text des Hinweises oder aus dem Typ-Schlüssel ableitet, hätte eine versteckte Kopplung,
   die beim ersten Umformulieren bricht und das Server-Manifest als Quelle der Wahrheit verdoppelt. Das Feld kommt aus demselben
   Pfad wie `answerLength`. **Verworfen:** Textmuster im Hinweis und eine Typ-Schlüssel-Abfrage im Frontend. **Ausgeschlossen:**
   dass ein älterer Client das Feld versteht – er zeigt das Textfeld und spielt weiter (B-710 bleibt spielbar). **Kosten:** das
   Feld berührt `Pugling.Contracts` und `Pugling.Api`; die Story ist darum `wo: beides`, Backend zuerst.
2. **Das Element hat zwei Felder, per „+“ bis zu vier, plus den Knopf „keine Lösung“; die Antwort wird zu `x1;x2` aufsteigend
   sortiert** (B-640 Nr. 2, Teil Eingabe). Der Knopf ist ein Umschalter: aktiv leert und sperrt er die Felder und sendet
   `keine Lösung`. Begründung: zwei Lösungen sind der Regelfall; die Obergrenze verhindert eine Fläche, die wie ein Formular
   aussieht. **Verworfen:** nur das Textfeld (B-710) und beliebig viele Felder. **Ausgeschlossen:** Brüche und Wurzeln in den
   Feldern (B-710, Entscheidung 4). **Kosten:** eine Obergrenze, die ein Polynom höheren Grades später wieder aufmacht.
3. **Die Felder tragen `inputMode="decimal"`, kein `type="number"`.** Begründung: am Handy erscheint die Zahlentastatur mit Komma
   und Punkt, beides ist nach B-639 dezimal; `type="number"` verschluckt das Komma. **Verworfen:** `type="number"`.
4. **Der Hinweis aus B-710 entfällt für Karten mit Mengen-Eingabe.** Begründung: die Trenner-Regel ist bei getrennten Feldern
   überflüssig und irreführend. **Kosten:** zwei Wege (Textfeld mit Hinweis ohne `setAnswer`, Felder mit `setAnswer`), solange
   ein Client ohne das Feld spielt.

## Akzeptanzkriterien

Je Kriterium die Änderung, die es rot macht.

1. Eine Karte mit `setAnswer` zeigt zwei Zahlenfelder und den Knopf „keine Lösung“, eine ohne das Feld das Textfeld. *Rot, wenn:*
   die Karte die Felder auch ohne das Feld zeigt oder beim Feld das Textfeld bleibt (zwei Komponententests).
2. Bei Eingabe `4` und `2` sendet die Karte `2;4` (aufsteigend). *Rot, wenn:* ein Komponententest bei `4`, `2` etwas anderes
   als `2;4` bekommt.
3. Ein Feld mit Dezimalkomma (`2,5`) und eines mit Punkt (`3.5`) senden `2,5;3.5` unverändert (der Server vergleicht, B-639).
   *Rot, wenn:* die Karte das Komma in einen Punkt umschreibt oder das Feld es verwirft.
4. Der Knopf „keine Lösung“ leert die Felder, sperrt sie und sendet `keine Lösung`; ein zweiter Druck hebt es auf. *Rot, wenn:*
   die Felder beim Umschalten ihren Wert behalten oder `0` gesendet wird.
5. Mit allen Feldern leer und ohne Knopf ist „Prüfen“ gesperrt. *Rot, wenn:* eine leere Menge abgeschickt wird.
6. Beim Kartenwechsel behält das erste Feld den Fokus, und die Fläche wird nicht neu eingehängt. *Rot, wenn:* die Fläche mit
   einem `key` neu entsteht (Fokus auf `body`) – ein Test nach dem Muster der bestehenden Fläche in `Lektion.test.tsx`.
7. Enter im letzten Feld schickt ab. *Rot, wenn:* es nicht abschickt.
8. Die Felder tragen `inputMode="decimal"` und kein `type="number"`. *Rot, wenn:* das Attribut fehlt oder `type="number"` steht.
9. Die Karte eines Gleichungs-Atoms trägt `setAnswer`, die Karte jedes anderen Typs nicht. *Rot, wenn:* das Feld auf allen Karten
   gesetzt wird oder auf der Gleichungs-Karte fehlt.

## Schätzung

**Größe: M** — wie der Batch-Pfad im `MediaSelector`: ein neues Eingabeelement in einer Fläche mit festen Regeln (Fokus,
Sperre, Tastatur) plus ein additives Karten-Feld in Vertrag und Server. Wird der Karten-Pfad zweigeteilt gefunden (Risiko 1),
kippt die Größe Richtung L.

- **`migration: nein`** — nachgesehen: das Feld ist abgeleitet aus dem Atom, keine Spalte.
- **`vertragsbruch: nein`** — nachgesehen: ein **additives** Feld an der Karte; vorhandene Felder bleiben, ältere Clients
  ignorieren es. Es zieht `gen:contract`, `Pugling.Client` und die Karten-Fixtures nach, bricht aber keinen Leser.
- **`wo: beides`** — **Backend zuerst:** das Atom meldet „Mengen-Antwort“, die Karten-Projektion setzt `setAnswer`, der Vertrag
  bekommt das Feld; danach die Komponente.

### Risiken

- **Der Karten-Pfad am Server ist ungelesen:** ob Üben und Test dieselbe Projektion nutzen oder zwei, ist **nicht nachgesehen**.
  Sind es zwei Stellen, wächst der Backend-Anteil.
- **`Antwortflaeche` ist schon dicht:** vier Eingabeformen in einer Funktion; eine fünfte gehört in eine eigene Komponente
  (nach dem Muster `LetterBoxes`), sonst wird sie unlesbar.
- **Die Klausur ist strikt „one at a time“:** ob `SohnTest` dieselbe `Antwortflaeche` nutzt oder eine eigene Eingabe führt, ist
  nicht nachgesehen.
- **A11y:** Felder brauchen je ein Label („Lösung 1“, „Lösung 2“); der Knopf ist ein Umschalter (`aria-pressed`, wie die
  Auswahlknöpfe), kein Tab.

### Angriffsplan

1. **Backend zuerst:** Atom aus B-710 trägt die Mengen-Eigenschaft; Karten-Projektion (Üben und Test) setzt `setAnswer`; Vertrag
   additiv; `gen:contract`; `dotnet build Pugling.sln`.
2. **Komponente** `Mengeneingabe` (neu, `frontend/src/sohn/Mengeneingabe.tsx`) nach dem Muster `LetterBoxes`: Felder, „+“,
   Umschalter; die Bindung zu `x1;x2` als reine Funktion in `frontend/src/lib/mengeAntwort.ts`.
3. `Antwortflaeche`: Zweig `karte.setAnswer` mit den Fokus- und Sperrregeln der anderen Zweige; den Hinweis aus B-710 dort nicht
   zeigen.
4. `tsc -b`, `./node_modules/.bin/vitest run`, `./node_modules/.bin/playwright test gleichung-loesen.spec.ts`.

### Testweg

- `backend/Pugling.Api.Tests/PositionPracticeFlowTests.cs` und `PositionTestFlowTests.cs` – die Karte trägt `setAnswer` für den
  Gleichungs-Typ und nicht für andere (AK 9). Rote Probe: das Feld auf allen Karten setzen.
- `frontend/src/lib/mengeAntwort.test.ts` (neu) – Bindung: `4`,`2` wird `2;4`; Komma bleibt; leer bleibt leer; „keine Lösung“.
  Rote Probe: ohne Sortierung fällt der erste Fall.
- `frontend/src/sohn/Mengeneingabe.test.tsx` (neu) – AK 2, 3, 4, 5, 7, 8; `frontend/src/sohn/Lektion.test.tsx` – AK 1, 6 (Zweig
  und Fokus beim Kartenwechsel).
- E2E: `frontend/e2e/gleichung-loesen.spec.ts` (aus B-710) um den Weg mit Feldern erweitern: zwei Felder, dann der Knopf bei der
  Gleichung ohne Lösung.
- Vor der Abnahme: `pugling-reviewer` (das Feld), `frontend-reviewer`; Rollengang als Sohn im Handy-Format (Zifferntastatur,
  Fokus nach Kartenwechsel).

## Verlauf

- **2026-11-19** — angelegt als Teil von [B-640](B-640-uebungstyp-gleichung-loesen.md) (geteilt, Vorbereitungslauf, Sonnet):
  Mehrfeld-Eingabe (AK 10 der Ursprungsstory).
- **2026-11-19** — geschaetzt (Vorbereitungslauf, Sonnet): M, beides (Backend zuerst), keine Migration, kein Vertragsbruch
  (additives Karten-Feld); hängt an B-710. `wartet_auf` bleibt.
- **2026-11-19** — Entscheidungen bestätigt, einschließlich des Kartenfelds `setAnswer`. Stellvertretend auf Anweisung des Nutzers („nimm für alle Fragen deine Empfehlung“): die Empfehlung der Sonnet-Entscheidungen wurde übernommen.
