---
tags: [typ/story, status/in-arbeit, bereich/frontend, rolle/student]
aliases: [Lektionsraster, Rückmeldung als Blatt, Abschlussbild]
status: in-arbeit
prio: P1
art: Wunsch
groesse: L
wo: frontend
migration: nein
vertragsbruch: nein
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

1. ~~Gilt das Raster für Üben und Test?~~ → Entscheidung 1.
2. ~~Auswahl: wählen und dann „Prüfen", oder weiter ein Tipp?~~ → Entscheidung 2.
3. ~~Richtige Antwort: Blatt mit „Weiter", oder von selbst weiter?~~ → Entscheidung 3.
4. ~~Selbsteinschätzung im Raster~~ → Entscheidung 4.
5. ~~Kennenlern-Karte im Raster~~ → Entscheidung 4.
6. ~~Woher kommt die Arbeitsanweisung?~~ → Entscheidung 5.
7. ~~Maskottchen und Sprechblase bei langem Material~~ → Entscheidung 6.
8. ~~Kopfzeile~~ → Entscheidung 7.
9. ~~Glühbirne im Blatt~~ → Entscheidung 8; das Erklärfeld nach der Antwort ist
   [B-529](B-529-erklaerung-nach-der-antwort.md).
10. ~~Abschlussbild des Übens~~ → Entscheidung 9.
11. ~~Vater-Vorschau~~ → Entscheidung 10.
12. ~~Was aus dem Blatt wird, wenn die Lösung keine ist~~ → Entscheidung 8.

**Ausdrücklich zurückgestellt:** die Verfeinerung der Arbeitsanweisung nach Übungstyp (Entscheidung 5) und eine
Vater-Vorschau im Raster (Entscheidung 10) – für beide ist keine Story angelegt, weil niemand sie bisher vermisst.

## Entscheidungen

Grill-Runde mit dem Nutzer am 2026-11-13, eine Frage je Entscheidung.

1. **Ein Aufbau für Üben und Test, Rückmeldung nur im Üben.** Beide Bildschirme teilen den Rahmen (Kopf,
   Anweisung, Frage, Antwortfläche, ein Knopf unten) und die Antwortfläche. Im Test heißt der Knopf „Weiter",
   und es erscheint kein Blatt; der Test ist an einer eigenen Kopffarbe und dem Wort „Test" in der Anweisung
   erkennbar. Begründung: die Klausur bleibt eine Klausur, aber das Kind lernt die Bedienung nur einmal. Kosten:
   der Test sieht dem Üben sehr ähnlich, und ein Kind kann auf ein Blatt warten, das nicht kommt.
2. **Auswahl in zwei Schritten, im Üben wie im Test.** Antippen markiert (änderbar), „Prüfen" bzw. „Weiter"
   wertet. Begründung: die Form der Vorbilder, der eine Knopf hat immer dieselbe Aufgabe, und ein Vertipper im
   Test ist nicht mehr endgültig. Kosten: ein Tipp mehr je Auswahlkarte – gerade auf der leichtesten Stufe, die
   bei neuen Wörtern oft kommt; der Tempo-Bonus misst bis „Prüfen".
3. **Im Üben erscheint nach jeder vom Server geprüften Antwort das Blatt und wartet auf „Weiter".** Grün bei
   richtig, rot bei falsch; der Fokus liegt auf „Weiter", Enter genügt. Begründung: ein Blatt, das sich immer
   gleich verhält, ist der Kern der Story, und Münzen und Fälligkeit sind lesbar statt nach 1,1 Sekunden weg.
   Kosten: ein Tipp mehr je richtiger Karte; mit Entscheidung 2 sind es bei einer Auswahlkarte drei Tipps statt
   einem. Die Runden werden spürbar langsamer – hingenommen, weil die Tagesdosis die Kartenzahl begrenzt.
4. **Selbsteinschätzung und Kennenlernen ohne Blatt.** Selbsteinschätzung: der Knopf heißt „Aufdecken", danach
   stehen an seiner Stelle „Nochmal" und „Gewusst", und es geht direkt weiter; die Münzen zeigt der Zähler im
   Kopf. Kennenlernen: Lösung offen, Knopf „Weiter". Begründung: das Blatt wiederholte dort nur, was das Kind
   gerade gesehen oder selbst gedrückt hat. Kosten: die Regel „jede gewertete Karte endet gleich" gilt nur für
   servergeprüfte Antworten; der Münzgewinn einer Selbsteinschätzung ist nur am Zähler abzulesen.
5. **Die Arbeitsanweisung leitet das Frontend aus den Feldern der Karte ab**, mit allgemeinen Formulierungen:
   Auswahl → „Wähle die richtige Antwort"; gemischte Buchstaben → „Ordne die Buchstaben"; Buchstabenkästchen →
   „Fülle die Kästchen"; Wortkasten → „Schreibe das passende Wort"; nur eine Aufnahme → „Hör zu und schreibe";
   sonst getippt → „Schreibe die Antwort"; aufdeckbare Lösung → „Weißt du es?"; offene Lösung → „Neu – präg es
   dir ein". Begründung: dieselbe Unterscheidung wählt heute schon die Antwortfläche; kein Server-Feld. Kosten:
   die Anweisung weiß nicht, was gefragt ist („die Übersetzung"), und eine neue Antwortform muss die Abbildung
   nachziehen. Die Verfeinerung nach Übungstyp ist zurückgestellt.
6. **Sprechblase für Kurzes, Karte für Langes.** Trägt die Aufgabe nur Wort oder Satz, steht sie in der Blase
   neben einem kleinen Maskottchen. Sobald Lesetext, Bild, Wortkasten oder Dekodierung dabei sind, steht das
   Material als volle Karte ohne Maskottchen. Die Grenze hängt an den **Feldern** der Karte, nicht an der
   Textlänge. Zusätzlich reagiert das Maskottchen im Blatt. Begründung: die Vokabelkarte ist der häufigste Fall
   und der, den die Kinder kennen. Kosten: zwei Erscheinungsbilder; der Schulvokabeltest aus B-524 hat nie ein
   Maskottchen an der Aufgabe.
7. **Kopfzeile:** links ein ✕ (Üben: Runde beenden; Test: „Später weiter"; ohne Wort, mit sprechendem
   Vorlesenamen, ohne Rückfrage wie heute), daneben der Fortschrittsbalken mit dem Zähler „3 / 8" als kleinem
   Text, rechts die Combo. Der Tempo-Schalter zieht in eine Zeile darunter, nur im Üben. Die Stufen-Pille des
   Tests entfällt. Begründung: die Form der Vorbilder; der Zähler bleibt für das Kind, den Screenreader und die
   Specs. Kosten: ein Kind sieht dem ✕ nicht an, dass es im Test nichts verwirft, und die Stufe des Tests steht
   nur noch indirekt in der Anweisung.
8. **Inhalt des Blatts, keine Glühbirne.** Richtig: ein Lobwort, die Münzen samt Bonus und, wo der Karteikasten
   es meldet, „wieder fällig am …". Falsch: die eigene Antwort durchgestrichen und die Lösung; nennt der Server
   keine (Menge), nur „Leider nicht". Der Hinweis der Karte bleibt **vor** der Antwort und bekommt dort das
   Glühbirnen-Symbol. Begründung: ein Hinweis ist als Hilfe vor der Antwort geschrieben und liest sich danach
   seltsam. Kosten: das Blatt erklärt nichts – der eigentliche Lernwert des Vorbilds fehlt, bis
   [B-529](B-529-erklaerung-nach-der-antwort.md) gebaut ist.
9. **Abschlussbild mit Münzen und Genauigkeit, ohne Zeit.** Münzen vom Server; die Genauigkeit zählt das
   Frontend aus der gespielten Runde, und zwar **nur aus servergeprüften Antworten** – gab es keine, entfällt die
   Kachel. Eine reine Kennenlern-Runde zeigt wie heute „n neue Wörter kennengelernt". Die Knöpfe („Jetzt
   abfragen", „Weiter zum Test", „Zum Pfad") und das Testergebnis bleiben unverändert. Begründung: eine sichtbare
   Zeit lädt zum Hetzen ein und ist für ein Kind mit LRS Druck; selbst vergebene 100 % wären keine Genauigkeit.
   Kosten: das einzige Element des Vorbilds, das bewusst fehlt; eine Runde nur aus Selbsteinschätzung zeigt eine
   einzige Kachel.
10. **Die Vater-Vorschau bleibt, wie sie ist.** Das Raster ist ein Rahmen der Arcade um die Bausteine; die
    geteilte Antwortfläche darf die Vorschau nicht mitreißen (dort bleibt die Auswahl ein Tipp). Begründung: die
    Vorschau prüft den Stoff, nicht das Spielgefühl. Kosten: der Vater sieht nicht genau, was das Kind sieht.
11. **Der schwerere Tempo-Bonus wird in Kauf genommen** (Nutzer, 2026-11-13, nach der Schätzung). Der Server
    misst die Antwortzeit ab der vorigen Antwort (`PositionPracticeController.cs:420`); die Lesezeit am Blatt zählt
    damit in die nächste Karte. Begründung: B-526 bleibt reine Frontend-Arbeit, und betroffen sind nur Positionen
    mit eingestelltem Schnell-Bonus. Kosten: den Bonus bekommt fast nur noch, wer das Blatt wegdrückt, ohne es zu
    lesen – ein Anreiz gegen genau das, wofür das Blatt da ist. Die saubere Messung ist
    [B-530](B-530-antwortzeit-ab-sichtbarer-karte.md).

## Akzeptanzkriterien

1. Üben und Test haben denselben Aufbau: Kopf mit ✕, Fortschrittsbalken und Zähler; Arbeitsanweisung; Frage;
   Antwortfläche; ein Hauptknopf am unteren Rand. Der Test trägt eine eigene Kopffarbe und das Wort „Test" in
   der Anweisung und zeigt keine Stufen-Pille.
2. Jede Antwortform des Servers ist im Raster spielbar und trägt die Anweisung aus Entscheidung 5: Auswahl,
   Buchstaben ordnen, Buchstabenkästchen, Freitext, Freitext mit Wortkasten, Hören, Selbsteinschätzung,
   Kennenlernen.
3. Eine Auswahl wird durch Antippen nur markiert und ist änderbar; gewertet wird erst mit dem Hauptknopf. Ohne
   Markierung ist der Hauptknopf gesperrt. Das gilt in Üben und Test.
4. Im Üben erscheint nach jeder vom Server geprüften Antwort das Blatt und bleibt bis „Weiter": bei richtig ein
   Lobwort, Münzen samt Bonus und – wenn gemeldet – die nächste Fälligkeit; bei falsch die eigene Antwort und die
   Lösung, ohne Lösung vom Server nur „Leider nicht". Der Fokus liegt auf „Weiter".
5. Nach einer Selbsteinschätzung und nach einer Kennenlern-Karte erscheint kein Blatt.
6. Im Test erscheint kein Blatt und kein Hinweis auf richtig oder falsch; der Hauptknopf heißt „Weiter".
7. Eine Aufgabe nur aus Wort oder Satz steht in der Sprechblase neben dem Maskottchen; trägt die Karte Lesetext,
   Bild, Wortkasten oder Dekodierung, steht sie als Karte ohne Maskottchen.
8. Der Hinweis einer Karte ist vor der Antwort über ein Glühbirnen-Symbol aufdeckbar; im Blatt gibt es keine
   Glühbirne.
9. Das ✕ beendet im Üben die Runde und verlässt den Test, ohne den Versuch zu verwerfen; sein Vorlesename sagt,
   was es jeweils tut. Der Tempo-Schalter steht nur im Üben.
10. Das Ende einer Übungsrunde zeigt Münzen und – wenn es servergeprüfte Antworten gab – die Genauigkeit daraus;
    keine Zeit. Eine reine Kennenlern-Runde zeigt „n neue Wörter kennengelernt" statt der Kacheln. Die Knöpfe des
    Rundenendes und das Testergebnis sind unverändert.
11. Die Antwortfläche ist ein Baustein für Üben und Test, nicht zweimal ausgeschrieben.
12. Die Vater-Vorschau verhält sich wie vorher, einschließlich Auswahl mit einem Tipp.
13. Tastatur und Screenreader: die Aufgabe wird wie heute angesagt (eine Live-Region über allen Ansichten), das
    Blatt wird angesagt, „Bewegung reduzieren" schaltet die Animationen ab.
14. Alle neuen Texte laufen über `t()` in Deutsch und Englisch.
15. Der Vater→Sohn-Durchstich und die Specs, die heute nach Klassen dieses Bildschirms greifen, sind grün.

## Schätzung

**Größe: L, am oberen Rand.** Kein Backend, kein Vertrag – aber die Story baut die beiden dichtesten Bildschirme
der Arcade um (`SohnPractice.tsx` 527 Zeilen, `SohnTest.tsx` 426 Zeilen), an denen 63 Vitest-Fälle und 15
E2E-Specs hängen. Das ist mehr als der Frontend-Teil von B-525 (ebenfalls L). XL wird sie nicht, weil sie sich in
zwei Etappen schneiden lässt, die je für sich grün und auslieferbar sind; nach Etappe 1 kann man anhalten.

- **`migration: nein`** – nachgesehen: keine Schemaänderung, die Story fasst kein Backend an.
- **`vertragsbruch: nein`** – nachgesehen: alles, was das Blatt zeigt, liefert `ReviewOutcome` schon
  (`PracticeDtos.cs:102-103`: Lösung, Münzen, Box, Fälligkeit, Combo- und Tempo-Bonus).

**Risiken**

1. **Der Tempo-Bonus wird schwerer – eine Folge von Entscheidung 3, die beim Grillen nicht auf dem Tisch lag.**
   Der Server misst die Antwortzeit als Abstand zur **vorigen** Antwort (`PositionPracticeController.cs:420`),
   nicht ab dem Erscheinen der Karte. Wartet das Blatt auf „Weiter", zählt die Zeit, die das Kind dort liest,
   in die nächste Karte hinein. Heute sind es feste 1,1 Sekunden. Ohne Gegenmaßnahme gibt es den Schnell-Bonus
   praktisch nur noch für den, der das Blatt wegdrückt, ohne es zu lesen. Die saubere Abhilfe (der Client meldet
   den Zeitpunkt, an dem die Karte sichtbar wurde) wäre Backend und Vertrag und damit eine eigene Story – **vor
   dem Bau zu entscheiden**, ob B-526 den Bonus so in Kauf nimmt. → Entscheidung 11: in Kauf genommen.
2. **Fokus und Ansage sind hier mehrfach repariert worden** (B-134, B-286, B-420, B-439, B-443, B-515). Zwei
   Regeln beißen direkt: Eine Live-Region muss **vor** ihrem Text im DOM stehen (`liveRegions.test.ts` – der
   Prototyp ist genau daran gescheitert), das Blatt darf also nicht selbst die Region sein, die mit ihm
   erscheint; und nach jedem Phasenwechsel muss der Fokus ein Ziel haben, sonst fällt er auf `body`.
3. **Zwei Schritte bei der Auswahl ändern, was die Sperre schützt.** Heute verhindert `useAction`, dass zwei
   schnelle Tipper zwei Bewertungen schicken (B-49, B-73, B-284). Künftig markiert der Tipp nur; die Sperre
   gehört an den Hauptknopf, und ein Doppel-Enter darf keine Karte überspringen.
4. **Die Vater-Vorschau teilt die Bausteine** (`ExercisePreviewModal.tsx`). Die neue geteilte Antwortfläche ist
   ein Baustein der Arcade; rutscht sie in die Vorschau, hat der Vater dort plötzlich zwei Schritte und kein
   Ergebnis.
5. **15 Specs und ein Helfer** (`introduceAndQuiz`, `e2e/helpers.ts:65`) greifen nach dem Zähler `.pill.cyan`,
   nach `.fcard`, `.card.mistake` und den Knopfnamen. Die Dreh-Animation prüft `karten-umdrehen.spec.ts:53-58`
   am Element `.fcard` – die Selbsteinschätzung muss also weiter eine Karte sein, die sich dreht, auch wenn die
   Frage in einer Sprechblase steht.
6. **Der Test darf über die geteilte Antwortfläche nichts verraten.** Markierung ja, Färbung nach richtig und
   falsch nie; der Baustein bekommt die Wertung nur im Üben überhaupt gereicht.
7. **Handyhöhe mit Bildschirmtastatur.** Ein Knopf am unteren Rand und ein Freitextfeld darüber: mit offener
   Tastatur muss beides sichtbar bleiben (`lueckentext-am-handy.spec.ts` fährt genau diesen Fall).

**Angriffsplan**

*Etappe 1 – ein Aufbau, eine Antwortfläche (Entscheidungen 1, 2, 5, 6, 7):*

1. Reine Funktion „Aufgabenform einer Karte": Anweisung, Darstellung (Blase oder Karte) und Antwortart aus den
   Feldern der Karte. Vitest je Zeile der Tabelle aus Entscheidung 5, bevor eine Komponente sie benutzt.
2. Geteilte Antwortfläche der Arcade mit Zwei-Schritt-Auswahl; zuerst in den Test (keine Rückmeldung, also der
   kleinere Umbau), dann ins Üben.
3. Rahmen-Komponente (Kopf mit ✕, Balken, Zähler, Combo; Anweisung; Frage; Antwort; Fuß mit Hauptknopf) und
   Umzug beider Bildschirme hinein. Rückmeldung bleibt in dieser Etappe, wie sie ist.
4. Specs und Komponententests nachziehen; Commit.

*Etappe 2 – Blatt und Abschluss (Entscheidungen 3, 4, 8, 9):*

5. Blatt statt Toast und statt des angehaltenen Kastens, gespeist aus einer Region, die schon vor ihm steht;
   Fokus auf „Weiter".
6. Abschlussbild mit Münzen und Genauigkeit aus servergeprüften Antworten.
7. `docs/sohn-app-funktionsbeschreibung.md` (Abschnitte 2.8, 4.3, 4.4) nachziehen; Commit.

**Testweg**

- **Vitest, neu:** die Aufgabenform-Funktion (jede Antwortform, je mit dem Fall, der sie rot macht); die
  Antwortfläche (Markieren ändert nichts am Server, Hauptknopf gesperrt ohne Markierung, Doppelklick wertet
  einmal; im Test keine Färbung).
- **Vitest, umgestellt:** `SohnPractice.test.tsx` (Blatt nach servergeprüfter Antwort, **kein** Blatt nach
  Selbsteinschätzung und Kennenlernen, „Leider nicht" ohne Lösung, Genauigkeit zählt nur Servergeprüftes),
  `SohnTest.test.tsx` (kein Blatt, Knopf „Weiter"), `arcadeSpielweg.test.tsx`. Unverändert grün bleiben müssen
  `SelfAssessAnswer.test.tsx`, `TestResult.test.tsx`, `liveRegions.test.ts` und die Tests der Vater-Vorschau.
- **E2E (zugleich der Rollengang):** `full-flow.spec.ts` durch Üben und Test; `erstbegegnung.spec.ts` (aus dem
  angehaltenen Kasten wird das Blatt); `karten-umdrehen.spec.ts`; `lueckentext-am-handy.spec.ts`;
  `sprache.spec.ts`; dazu eine neue Spec für den Lektionsbildschirm (Zwei-Schritt-Auswahl, Blatt, Abschluss).
  Gemessen an „N passed" im ganzen Lauf.
- **Reviewer:** `frontend-reviewer` je Etappe.
- **Bleibt beim Menschen:** ob sich die langsameren Runden für die Kinder richtig anfühlen – und Risiko 1.

## Verlauf

- 2026-11-13 · `idee` – aufgenommen nach der Variantenwahl am Prototyp; Prio P1 ist ein Vorschlag.
- 2026-11-13 · `ausformuliert` – gegen den Code belegt: Üben und Test sind zwei Geometrien mit doppelt
  ausgeschriebener Antwortfläche, die Rückmeldung sitzt an drei Stellen, und der angehaltene Kasten aus B-420
  ist schon der Vorläufer des Blatts. Reine Frontend-Arbeit, zwölf offene Punkte.
- 2026-11-13 · `gegrillt` – zehn Entscheidungen im Dialog mit dem Nutzer, alle gemäß Empfehlung; das Erklärfeld nach
  der Antwort als B-529 abgespalten, Anweisung nach Übungstyp und Vorschau im Raster zurückgestellt.
- 2026-11-13 · `geschaetzt` – L am oberen Rand, in zwei Etappen; keine Migration, kein Vertragsbruch (beides
  nachgesehen). Beim Nachsehen gefunden: der Tempo-Bonus misst ab der vorigen Antwort, das wartende Blatt macht
  ihn schwerer (Risiko 1) – vor dem Bau zu entscheiden.
- 2026-11-13 · Entscheidung 11 nachgetragen (Tempo-Bonus in Kauf genommen), saubere Messung als B-530 abgespalten.
- 2026-11-13 · `in-arbeit` – **Etappe 1 gebaut**: `aufgabenform.ts` (Anweisung, Darstellung, Antwortart aus den Feldern
  der Karte), `Lektion.tsx` (Rahmen, Aufgabe, geteilte Antwortfläche mit Zwei-Schritt-Auswahl); `SohnPractice` und
  `SohnTest` rendern ihre Karte darüber. Die Rückmeldung ist unverändert (Toast und angehaltene Karte) – das Blatt
  und das Abschlussbild sind Etappe 2.
- 2026-11-13 · Review Etappe 1 (`frontend-reviewer`, kein Blocker). Übernommen: **Regression im Hauptweg** – ein `key`
  je Karte hängte die Antwortfläche neu ein, das Eingabefeld verlor nach jeder Antwort den Fokus (am Handy klappt
  die Tastatur zu); jetzt setzt `kartenId` nur die Markierung zurück, belegt durch einen Test mit roter Probe. Im
  Test erscheint die Lösung nach „Aufdecken“ wieder **in** der Karte, die sich dreht. Die Combo steht in der Zeile
  unter dem Kopf (bei 360 px stünde er sonst über), ein Hörtext mit Frage ist eine Karte, lange Wörter brechen in der
  Blase um. „Getippt“ entscheidet nur noch die Aufgabenform. Drei Zusicherungen, die nie fallen konnten, sind
  ersetzt oder als Absichtserklärung beschriftet.
- 2026-11-13 · Abweichungen und offene Reste, bewusst: (a) Entscheidung 5 verfeinert – eine offene Karte heißt nur
  „Neu – präg es dir ein“, wenn der Server sie als Erstkontakt meldet, sonst „Schau es dir noch mal an“ (eine
  Anzeigestufe des Tages zeigt auch bekannte Wörter). (b) AK 11 nur teilweise: die getippten Formen sind ein Baustein,
  Selbsteinschätzung und Kennenlernen sind weiter je Bildschirm geschrieben (Üben: eigene Knöpfe „Nochmal/Gewusst!“,
  Test: `SelfAssessAnswer`) – offen für Etappe 2. (c) Der Knopf des Übens heißt schon jetzt „Aufdecken“
  (Entscheidung 4 vorgezogen). (d) Nicht gemessen: das Layout bei 360 px im Browser; Enter auf einer markierten Option
  nimmt die Markierung zurück, statt zu werten.
