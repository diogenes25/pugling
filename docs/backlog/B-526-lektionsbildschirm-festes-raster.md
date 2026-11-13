---
tags: [typ/story, status/ausformuliert, bereich/frontend, rolle/student]
aliases: [Lektionsraster, Rückmeldung als Blatt, Abschlussbild]
status: ausformuliert
prio: P1
art: Wunsch
wo: frontend
quelle: "docs/duolingo-snipwise-vergleich-plan.md#entscheidung-am-prototyp (Lektionsbildschirm des Prototyps, Nutzer im Chat 2026-11-13)"
grund: ""
ersetzt_durch: []
wartet_auf: ""
---

# B-526 · Lektionsbildschirm nach festem Raster

Üben und Test der Sohn-Arcade sollen die Geometrie bekommen, die die Kinder aus Duolingo und Snipwise
kennen: oben Schließen und Fortschrittsbalken, darunter die **Arbeitsanweisung** als Überschrift, die
Frage in einer **Sprechblase** am Skin-Maskottchen, die Antwortfläche, und unten **ein** Knopf. Nach
„Prüfen" erscheint die Rückmeldung als **Blatt an derselben Stelle** (richtig/falsch, Lösung, „Weiter")
statt als Toast. Am Ende steht ein Abschlussbild mit Münzen, Genauigkeit und Zeit.

Vorlage ist der Prototyp auf dem Branch `prototyp/lernpfad-b525` (`frontend/src/prototyp/Lektion.tsx`,
Wegwerf-Code, lokal bewertet). Die Pfad-Startseite ist [B-525](B-525-sohn-startseite-als-schlangenpfad.md).

## User Story

Als **Sohn** möchte ich jede Aufgabe im selben, vertrauten Aufbau sehen und meine Rückmeldung immer an
derselben Stelle bekommen, damit ich mich auf die Aufgabe konzentriere statt auf die Bedienung.

## Ist-Stand am Code

**Üben und Test sind zwei verschieden gebaute Bildschirme.**

- **Üben** (`SohnPractice.tsx:354-493`): Kopfzeile aus Pillen – „Karte i / n", Combo, Tempo-Schalter,
  „Runde beenden" (`:355-375`). Darunter die Karte `.fcard` mit dem festen Etikett „AUFGABE" (`:383`) und
  allem Material darin: Bild, Passage, Audio, Lückensatz, Dekodierung, Tipp, Wortkasten (`:389-428`). Die
  Antwortfläche steht **unter** der Karte (`:433-484`).
- **Test** (`SohnTest.tsx:203-291`): Titel „Tagestest" plus Pillen (Frage i / n, Stufe, „Später weiter",
  `:205-219`), eine Hinweiszeile (`:221`) und **eine** Karte, die Material **und** Antwortfläche zusammen
  enthält (`:224-290`).
- Die Antwortfläche ist in beiden Dateien getrennt ausgeschrieben: Auswahl, Buchstaben ordnen,
  Buchstabenkästchen, Freitext, Selbsteinschätzung (`SohnPractice.tsx:440-484`, `SohnTest.tsx:242-289`).
- Die Bausteine darin sind geteilt (`components/ClozePrompt`, `LetterBoxes`, `LetterTiles`, `CueBox`,
  `Passage`, `AudioButton`, …) und werden **auch von der Vater-Vorschau** benutzt
  (`vater/ExercisePreviewModal.tsx`, acht Bezüge).

**Wie heute geantwortet wird:**

- **Auswahl:** ein Tipp auf eine Option wertet sofort (`SohnPractice.tsx:443-444`, `SohnTest.tsx:247-248`).
  Es gibt kein „wählen, dann prüfen".
- **Getippt** (Freitext, Kästchen, Ordnen): ein Knopf darunter – im Üben „Prüfen" (`:469`), im Test
  „Weiter" (`SohnTest.tsx:254`, `:259`, `:277`).
- **Selbsteinschätzung:** „Umdrehen" (`SohnPractice.tsx:475`), dann „Nochmal" / „Gewusst"
  (`:481-482`); im Test „Aufdecken", dann „Nicht gewusst" / „Gewusst" (`SohnTest.tsx:319`, `:327-328`).
- **Kennenlern-Karte** (`displayOnly`): Lösung steht offen da, ein Knopf „Weiter", kein Urteil (`:471-473`).

**Wie heute rückgemeldet wird (nur Üben – der Test zeigt bewusst nichts):**

- **Richtig:** ein Toast „+n 🪙 · Box x" für 1,1 Sekunden, dann geht es von selbst weiter
  (`SohnPractice.tsx:224-227`, `:251`). Der Toast liegt fest über der Navigation (`index.css:312`).
- **Falsch getippt:** die Karte **hält an**; an die Stelle der Antwortfläche tritt ein Kasten mit der
  eigenen Antwort, der Lösung und „Weiter" (`:433-439`, B-420). Das ist dem gewünschten Blatt schon nah.
- **Falsch bei Selbsteinschätzung:** Toast für 1,6 Sekunden, dann weiter (`:239`).
- **Kennenlernen:** nichts (`reviewFeedback` gibt `none`, `:42`).

**Was es nicht gibt:**

- **Keine Arbeitsanweisung je Aufgabenform.** Im Üben steht immer „AUFGABE"; der Test nennt die Stufe als
  Pille (`useStageLabel`, `SohnTest.tsx:32-39`) und einen von zwei festen Hinweisen (`:221`).
- **Kein Maskottchen** auf Üben und Test; es steht nur im Testergebnis (`SohnTest.tsx:362`).
- **Kein Fortschrittsbalken**, nur die Zähler-Pille. Die Klasse `.bar` gibt es (`index.css:96`).
- **Keine Erklärung nach der Antwort.** Die Karte trägt nur `Hint`, der vor dem Antworten aufgedeckt
  wird (`PracticeDtos.cs:74-79`, `SohnPractice.tsx:418-422`); `ReviewOutcome` trägt Lösung, Münzen, Box,
  Fälligkeit und Boni, aber keinen Erklärtext (`PracticeDtos.cs:102-103`).
- **Kein Abschlussbild mit Genauigkeit und Zeit.** Das Rundenende zeigt Münzen und Kartenzahl
  (`SohnPractice.tsx:309-327`). Die verdienten Münzen werden mitgezählt (`:66`, `:217`), richtige Antworten
  nicht; die Startzeit liegt für den Heartbeat vor (`:91`).

**Tests, die an der heutigen Struktur hängen:** zehn E2E-Specs greifen nach Klassen dieses Bildschirms
(`.pill.cyan` als Zähler, `.fcard`, `.card.mistake`), darunter `karten-umdrehen.spec.ts:53-58` (die
Dreh-Animation an `.fcard`), `erstbegegnung.spec.ts:108` und `sprache.spec.ts:117`. Die Komponententests
`SohnPractice.test.tsx` und `SohnTest.test.tsx` sind nicht im Einzelnen gezählt.

## Die echte Lücke

Es fehlt kein Inhalt, sondern der **gemeinsame Rahmen**. Drei Dinge trennen den Ist-Stand vom Raster:

1. **Zwei Geometrien statt einer.** Material und Antwort sitzen im Üben getrennt, im Test in einer Karte;
   die Antwortfläche ist doppelt ausgeschrieben. Ein Raster verlangt einen geteilten Rahmen
   (Kopf · Anweisung · Frage · Antwort · Fuß) und eine geteilte Antwortfläche.
2. **Rückmeldung an drei Stellen.** Toast, angehaltener Kasten, gar nichts – je nach Fall. Das Blatt wäre
   **eine** Stelle; der angehaltene Kasten aus B-420 ist dessen Vorläufer.
3. **Der Prototyp kennt nur vier Aufgabenformen.** Das echte Üben hat Material, das in keine Sprechblase
   passt (Lesetext, Bild, Lückensatz mit Wortkasten, Dekodierung), und zwei Formen ohne „Prüfen"
   (Selbsteinschätzung, Kennenlernen). Dafür muss das Raster Antworten haben, bevor es gebaut wird.

Reine Frontend-Arbeit: Für nichts davon fehlt ein Feld im Vertrag – mit einer Ausnahme, die Erklärung
hinter der Glühbirne (Punkt 9).

## Offene Punkte

1. **Gilt das Raster für Üben und Test?** Empfehlung: ja für die **Geometrie** (derselbe Rahmen, dieselbe
   Antwortfläche), nein für die **Rückmeldung**: der Test zeigt weiter kein Blatt, sein Knopf heißt
   „Weiter". Sonst wäre die Klausur keine mehr.
2. **Auswahl: wählen und dann „Prüfen", oder weiter ein Tipp?** Empfehlung: zwei Schritte, wie in beiden
   Vorbildern – ein Vertipper ist korrigierbar, und im Test ist er sonst endgültig. Kosten: ein Tipp mehr je
   Karte; der Tempo-Bonus misst dann bis „Prüfen".
3. **Richtige Antwort: Blatt mit „Weiter", oder wie heute von selbst weiter?** Empfehlung: Blatt mit
   „Weiter" (Fokus darauf, Enter genügt) – eine Stelle, ein Verhalten, und die Münzen sind lesbar statt
   nach 1,1 Sekunden weg. Kosten: ein Tipp mehr je richtiger Karte, die Runde wird spürbar langsamer.
4. **Selbsteinschätzung im Raster.** Sie hat kein „Prüfen". Empfehlung: der eine Knopf heißt „Aufdecken";
   danach stehen an seiner Stelle „Nochmal" und „Gewusst", und es geht ohne Blatt weiter – die Lösung stand
   ja schon da. Die Münzen zeigt der Zähler im Kopf.
5. **Kennenlern-Karte im Raster.** Empfehlung: Anweisung „Neu – präg es dir ein", Lösung offen, Knopf
   „Weiter", kein Blatt.
6. **Woher kommt die Arbeitsanweisung?** Empfehlung: aus den Feldern der Karte, so wie die Antwortfläche
   schon gewählt wird (Auswahl da → „Wähle die Übersetzung", gemischte Buchstaben → „Ordne die Buchstaben",
   Wortkasten → „Schreibe das passende Wort", nur Aufnahme → „Hör zu und schreibe" …). Kein neues
   Server-Feld und keine Stufennamen im Client. Kosten: eine Abbildung, die bei einer neuen Antwortform
   nachgezogen werden muss.
7. **Maskottchen und Sprechblase bei langem Material.** Eine Vokabel passt in eine Blase, ein Lesetext oder
   ein Bild nicht. Empfehlung: kurze Aufgaben (ein Wort, ein Satz) in der Blase neben einem kleinen
   Maskottchen; sobald die Karte Passage, Bild oder Wortkasten trägt, steht das Material als Karte ohne
   Maskottchen. Alternative: Maskottchen nur im Blatt und im Abschlussbild.
8. **Kopfzeile.** Empfehlung: ✕ links (Üben: „Runde beenden", Test: „Später weiter"), Fortschrittsbalken,
   Combo rechts; der Zähler „Karte i / n" bleibt als kleiner Text am Balken, Tempo als Schalter darunter.
   Der Zähler bleibt, weil zehn Specs und der Screenreader an ihm hängen.
9. **Glühbirne im Blatt.** Es gibt nur den `Hint` der Karte, keinen Erklärtext nach der Antwort.
   Empfehlung: die Glühbirne zeigt den vorhandenen Hinweis, wenn die Karte einen trägt, sonst fehlt sie. Ein
   eigenes Erklärfeld wäre eine Vertrags- und Creator-Story und wird zurückgestellt.
10. **Abschlussbild des Übens.** Empfehlung: Münzen, Genauigkeit und Zeit, im Client aus der gerade
    gespielten Runde gezählt – es ist Anzeige, keine Wertung; die Münzen kommen weiter vom Server. Eine
    reine Kennenlern-Runde zeigt keine Genauigkeit. Das Testergebnis bleibt, wie es ist.
11. **Vater-Vorschau.** Sie teilt die Bausteine. Empfehlung: bleibt unverändert; das Raster ist ein Rahmen
    der Arcade um die Bausteine, kein Umbau der Bausteine.
12. **Was aus dem Blatt wird, wenn die Lösung keine ist.** Bei einer Menge nennt der Server keine Lösung
    (`null`). Empfehlung: das Blatt zeigt dann nur „Leider nicht" – wie heute der Kasten.

## Akzeptanzkriterien

Entwurf, final nach dem Grillen.

1. Üben und Test haben denselben Aufbau: Kopf mit Schließen und Fortschritt, Arbeitsanweisung, Frage,
   Antwortfläche, ein Hauptknopf am unteren Rand.
2. Jede Antwortform des Servers ist im Raster spielbar: Auswahl, Buchstaben ordnen, Buchstabenkästchen,
   Freitext, Freitext mit Wortkasten, Hören, Selbsteinschätzung, Kennenlernen.
3. Im Üben erscheint nach einer gewerteten Antwort die Rückmeldung als Blatt an fester Stelle: richtig oder
   falsch, bei falsch die Lösung (sofern der Server eine nennt) und die eigene Antwort, verdiente Münzen,
   „Weiter".
4. Im Test erscheint kein Blatt und kein Hinweis auf richtig oder falsch.
5. Die Antwortfläche ist ein Baustein für Üben und Test, nicht zweimal ausgeschrieben.
6. Das Ende einer Übungsrunde zeigt Münzen, Genauigkeit und Zeit; eine reine Kennenlern-Runde zeigt keine
   Genauigkeit.
7. Die Vater-Vorschau verhält sich wie vorher.
8. Tastatur und Screenreader: Fokus liegt nach „Prüfen" auf „Weiter", die Aufgabe wird wie heute angesagt
   (eine Live-Region über allen Ansichten), „Bewegung reduzieren" schaltet die Animationen ab.
9. Alle neuen Texte laufen über `t()` in Deutsch und Englisch.
10. Der Vater→Sohn-Durchstich und die Specs, die heute nach Klassen dieses Bildschirms greifen, sind grün.

## Verlauf

- 2026-11-13 · `idee` – aufgenommen nach der Variantenwahl am Prototyp; Prio P1 ist ein Vorschlag.
- 2026-11-13 · `ausformuliert` – gegen den Code belegt: Üben und Test sind zwei Geometrien mit doppelt
  ausgeschriebener Antwortfläche, die Rückmeldung sitzt an drei Stellen, und der angehaltene Kasten aus B-420
  ist schon der Vorläufer des Blatts. Reine Frontend-Arbeit, zwölf offene Punkte.
