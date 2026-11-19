---
tags: [typ/story, status/geschaetzt, bereich/training, rolle/creator, lerntechnik/vokabeln]
aliases: [Hinweis fehlt bei Fremdwort → Deutsch]
status: geschaetzt
prio: P3
art: Defekt
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: "Nachschau Sprint 82 im Nachtlauf 2026-11-17 (docs/pm-sitzung-2026-11-17-nachtlauf.md)"
grund: ""
ersetzt_durch: []
entgangen_bei: [B-597]
---

# B-607 · Der Editor bietet einen Hinweis an, den die Schreibstufen bei „Fremdwort → Deutsch" still verschlucken

Seit B-597 wird auf jeder Schreibstufe die Lernsprache getippt. Dabei fällt der eigene Hinweis je Zeile und der
Artikel-Hinweis weg, weil die Schreibkarte aus `Swap` entsteht, und der Editor bietet das Hinweis-Feld weiter an,
ohne das zu sagen. B-597 hat das in keiner Entscheidung festgehalten; die zwei Tests, die den Hinweis zusicherten,
wurden auf die Auswahlstufe umgestellt statt den Verlust zu melden.

Das Verschlucken selbst ist wahrscheinlich richtig (der Hinweis war für die deutsche Antwort geschrieben, „beginnt mit
h" passt nicht zu „avoir"). Falsch ist, dass der Creator es nicht erfährt.

## User Story

Als Creator möchte ich im Editor sehen, auf welchen Stufen mein Hinweis zu einem Wort erscheint, damit ich keinen
Hinweis schreibe, den das Kind nie zu Gesicht bekommt.

## Ist-Stand am Code

Der Hinweis einer Vokabelkarte entsteht einmal, kanonisch, und wird dann je Stufe und Richtung weitergereicht oder
verworfen:

1. **Quelle:** der eigene Hinweis der Zeile, sonst der Artikel des Speichereintrags
   (`backend/Pugling.Api/Services/Shared/ExerciseContentResolver.cs:102`, `r.Hint ?? ArticleHint(v)`;
   `ArticleHint` selbst `:155`). Beides ist in der Sprache der *Übersetzung* gedacht – der Vertrag nennt den Artikel
   den der Zielsprache (`:150-153`).
2. **Verwerfen:** `ExerciseContentProvider.Swap` setzt `Hint = null` (`ExerciseContentProvider.cs:128`). Die Karte,
   deren Antwort das gelernte Wort ist, ist immer ein `Swap`.
3. **Schreibstufen:** `VocabularyExerciseType.CardForStage` nimmt auf Schreibstufen die `Written`-Karte
   (`backend/Pugling.Api/Exercises/VocabularyExerciseType.cs:115-116`); als Schreibstufen gelten Kästchen, Buchstaben
   ordnen, Freitext, Diktat und Lernkarte (`:123-125`). `Written` baut der Resolver per `Swap`
   (`ExerciseContentResolver.cs:120-122`) – ohne Hinweis.
4. **Ausspielung:** die Karte trägt den Hinweis nur auf getippten Stufen und nur, wenn kein Wortkasten ihn ersetzt
   (`backend/Pugling.Api/Services/Shared/PositionPlayService.cs:505`). Auf der Auswahlstufe steht er also noch.

Das ergibt je Richtung (aus `WithDirection`, `ExerciseContentProvider.cs:96-103`):

| Richtung | Auswahl-Stufe | Schreibstufen |
|---|---|---|
| `front-to-back` (Fremdwort → Deutsch) | Hinweis da | kein Hinweis (`Written` = `Swap`) |
| `both` | Hinweis da (Vorwärtskarte, `RecognitionOnly`, Decke Auswahl: `VocabularyExerciseType.cs:106-109`) | kein Hinweis (Rückkarte ist `Swap`) |
| `back-to-front` (Deutsch → Fremdwort) | **kein Hinweis** (die Karte selbst ist `Swap`, `ExerciseContentProvider.cs:101`) | kein Hinweis |

Editor: die Spalte „Hinweis" ist eine schlichte Überschrift (`frontend/src/vater/ExerciseEditModal.tsx:559`) über
einem Eingabefeld (`:656`), gespeichert mit „Hinweis gespeichert." (`:564`); im Anlegen-Formular heißt das Feld
„Hinweis (optional)" (`frontend/src/vater/exerciseConfig.tsx:670`). Ein Erklärtext zur Reichweite: nicht gefunden
(`frontend/src/lib/fieldHelp.ts` hat kein Thema für das Hinweis-Feld einer Vokabelzeile; die Treffer dort betreffen
andere Felder). Die Vorschau des Vaters zeigt den Hinweis, soweit die Stufe ihn ausspielt
(`frontend/src/vater/ExercisePreviewModal.tsx:154`, `:228`).

Die Tests halten das Verwerfen bisher nur in Kommentaren fest: `PositionTestFlowTests.cs:511` („a writing stage
asks the learnt word and drops it with the turn") und `VokabelTippTests.cs:32`. Ein Test, der den Wegfall auf einer
Schreibstufe **zusichert**: nicht gefunden.

Nicht prüfbar in diesem Lauf (echte DB bleibt unberührt): die Zahl „7 Übungen in der echten DB".

## Die echte Lücke

Breiter als die Notiz: nicht nur „Fremdwort → Deutsch" verliert den Hinweis auf Schreibstufen, sondern **jede**
Richtung – und `back-to-front` verliert ihn schon auf der Auswahlstufe, **auch vor B-597** (der `Swap` stammt aus
dem Richtungsschalter, nicht aus B-597). Der Hinweis ist damit eine Eigenschaft der *Erkennen*-Karte, die es nur in
den Richtungen `front-to-back` und `both` gibt. Der Editor sagt davon nichts, und kein Test hält es fest.

Das Verhalten selbst ist stimmig (der Hinweis gehört zur deutschen Antwort); es fehlt die Auskunft an den Creator und
die Zusicherung im Test.

## Offene Punkte

1. ~~**Wie erfährt der Creator es?**~~ Entschieden: Entscheidung 1.
2. ~~**Soll der Hinweis auch beim Schreiben erscheinen?**~~ Entschieden: Entscheidung 2.
3. ~~**Gehört „back-to-front verliert den Hinweis überall" in dieselbe Story?**~~ Entschieden: Entscheidung 3.

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-19 (Sonnet), unbestätigt.*

1. **Ein Erklärtext am Feld, kein Ausgrauen und kein zweites Feld.** Das Thema `vocabularyHint` kommt in
   `frontend/src/lib/fieldHelp.ts`; die Spalte „Hinweis" im Editor (`ExerciseEditModal.tsx:559`) trägt ein „ⓘ"
   daran, ebenso das Zeilenfeld im Anlegen-Formular (`exerciseConfig.tsx:670`, `RowField` nimmt ein `topic`
   schon an, `:773-782`). Begründung: die Richtung steckt in der Übungs-Config, nicht im Zeilen-Zustand
   der Tabelle – ein Ausgrauen bräuchte Plumbing für einen Randfall und würde bei `both` doch wieder erklären
   müssen; ein Schreib-Hinweisfeld wäre eine neue Funktion. Verworfen: (b) ausgrauen je Richtung, (c) eigenes
   Schreib-Hinweisfeld. Kosten: der Text erreicht nur, wer das „ⓘ" anklickt; ein Creator, der es nie öffnet,
   schreibt weiter Hinweise ins Leere. Ausgeschlossen wird damit der Fall der User Story, dass der Creator den
   Verlust *nicht erfahren kann*; nicht ausgeschlossen: dass er ihn nicht liest. Das ist für einen Defekt
   „Auskunft fehlt" der richtige Schnitt – ein Zwang wäre (c) oder (b).
2. **Der Hinweis erscheint beim Schreiben weiterhin nicht.** Begründung: der Hinweis nennt die Antwort der
   Erkennen-Karte (Artikel oder „beginnt mit h" der *Übersetzung*, `ExerciseContentResolver.cs:102`, `:150-153`);
   auf Schreibstufen ist die Antwort das Fremdwort, und `Swap` löscht den Hinweis bewusst
   (`ExerciseContentProvider.cs:114-116`, Kommentar: „it belonged to the word now being asked"). Verworfen: den
   Hinweis auch auf der Schreibkarte zeigen. Kosten: ein Kind, das schreibt, bekommt keinen Hinweis, auch wenn
   der Creator einen geschrieben hat; wer einen Schreib-Hinweis will, braucht eine eigene Idee (neues Feld je Zeile,
   Migration, Vertragsbruch). Ausgeschlossen: dass ein Hinweis, der die deutsche Antwort verrät, beim
   Schreiben die Lösung nennt. Das gehört zur User Story („keinen Hinweis schreiben, den das Kind nie sieht"),
   weil der Text genau dort ansetzt.
3. **`back-to-front` steht im selben Text.** Begründung: dieselbe Ursache (`Swap` mit `Hint = null`), und ein
   Text, der nur „Fremdwort → Deutsch" nennt, wäre für diese Richtung falsch. Verworfen: eine eigene Story.
   Kosten: der Text nennt drei Fälle statt einem und wird länger. Ausgeschlossen: ein Creator, der bei
   „Deutsch → Fremdwort" einen Hinweis schreibt und glaubt, die Auswahlstufe zeige ihn.
4. **Der Text spricht die Richtungen mit den Namen des Formulars an** („Fremdwort → Deutsch", „Deutsch → Fremdwort",
   „beides"; `exerciseConfig.tsx:486-489`), nicht mit den Server-Schlüsseln. Begründung: der Creator sieht nur die
   Namen. Kosten: ändert jemand die Beschriftung im Formular, driftet der Text – dafür sichert AK 3 den Wortlaut.
5. **Die Zusicherungen zum Verwerfen (AK 2–4) sind Festschreibungen, keine Regressionstests.** Sie bestehen im
   Ausgangszustand, weil das Verhalten stimmt; sie halten es, damit ein späterer Umbau den Text nicht zur Lüge
   macht. Jede nennt die Änderung, die sie rot macht. Verworfen: sie wegzulassen (dann hängt der Text an Kommentaren
   wie `PositionTestFlowTests.cs:511`). Kosten: drei Backend-Tests, die heute grün sind. Das Regressionskriterium
   des Defekts ist AK 1 (rot vor der Änderung).

## Akzeptanzkriterien

Final; je Kriterium die Änderung, die es rot macht.

1. Die Spalte „Hinweis" im Editor einer Vokabel-Übung trägt einen Knopf `Erklärung zu „Hinweis"`; im Anlegen-Formular
   das Zeilenfeld „Hinweis". Rot macht es: die Überschrift bleibt ein nackter `<th>`-Text (heute so) – Vitest am
   Editor, `getByRole("button", { name: /Erklärung zu „Hinweis"/ })` (vor der Änderung rot).
2. Der Text aus AK 1 nennt die Auswahlstufe, das Schreiben und die Richtung „Deutsch → Fremdwort" beim Namen und steht
   als `FIELD_HELP.vocabularyHint` in `fieldHelp.ts`, nicht am Feld; `fieldHelp.test.ts` prüft die drei Nennungen. Rot
   macht es: ein Satz gestrichen, etwa die Richtung, oder der Text direkt ins JSX geschrieben (der Test liest
   `FIELD_HELP`, das Thema fehlt dann); ein Tippfehler im Themennamen fällt über `HelpTopic` beim Übersetzen auf.
3. Festschreibung: eine Vokabel-Übung mit Richtung `front-to-back` und eigenem Hinweis liefert auf der Auswahlstufe
   den Hinweis und auf Kästchen, Buchstaben, Freitext und Diktat keinen. Rot macht es: `Swap` übernimmt den Hinweis
   (`ExerciseContentProvider.cs:128`), oder `Written` wird ohne `Swap` gebaut (`ExerciseContentResolver.cs:120-122`).
4. Festschreibung: dieselbe Übung mit `back-to-front` liefert auf **keiner** Stufe einen Hinweis. Rot macht es:
   `Swap` lässt den Hinweis stehen.
5. Festschreibung: mit `both` trägt die Vorwärtskarte den Hinweis auf der Auswahl, die Rückkarte nie. Rot macht es:
   die Rückkarte erbt ihn, oder `RecognitionOnly` (`ExerciseContentProvider.cs:100`) kappt die Vorwärtskarte so, dass
   sie auf der Auswahl keinen Hinweis mehr trägt.

## Schätzung

*Geschätzt im Vorbereitungslauf 2026-11-19 (Sonnet), am Code nachgesehen.*

**Größe: S** — mehr als der Anker XS (zwei Sätze in `fieldHelp.ts` plus der E2E, der sie prüft, B-02), weil derselbe
Text an zwei Stellen hängt und drei Backend-Festschreibungen dazukommen; weniger als M, weil kein Produktcode im
Backend und kein Vertrag berührt wird.

- **`wo: frontend`** — der gesamte Produktcode liegt im Frontend (`fieldHelp.ts`, `ExerciseEditModal.tsx`,
  `exerciseConfig.tsx`). Das Backend bekommt nur die drei Festschreibungen (AK 3–5), keine Änderung am Verhalten. Wer
  „Backend zuerst" will: die drei Tests zuerst schreiben, sie sind im Ausgangszustand grün.
- **`migration: nein`** — kein Schema, der Hinweis liegt unverändert am Eintrag.
- **`vertragsbruch: nein`** — `Pugling.Contracts` bleibt unberührt; das Verwerfen geschieht in `Swap`
  (`ExerciseContentProvider.cs:128`), und genau das wird nicht geändert.

### Risiken

1. **Der Knopfname hängt am Titel des Themas:** `InfoHint` setzt `aria-label` aus `help.title`
   (`frontend/src/components/InfoHint.tsx:99`). Das Thema `vocabularyHint` muss also `title: "Hinweis"` tragen, sonst
   findet AK 1 den Knopf `Erklärung zu „Hinweis"` nicht.
2. **Mehrere Treffer im Anlegen-Formular:** `RowField label="Hinweis"` steht je Zeile (`exerciseConfig.tsx:670`). Mit zwei
   Zeilen gibt es zwei gleichnamige Knöpfe; `getByRole` würfe. Der Test nimmt `getAllByRole` oder eine Zeile.
3. **Die Spalte ist ein `<th>`** (`ExerciseEditModal.tsx:559`), kein `FieldLabel`. Der `InfoHint` kommt als Geschwister des
   Textes in die Zelle; ob er in der Tabellenkopfzeile sauber umbricht, sieht nur der Rollengang.
4. **AK 5 (`both`) hängt an der Vorschau:** die Backend-Tests laufen am billigsten über
   `creator/exercises/{id}/preview?stage=` (Vorbild `VokabelTippTests.HintAsync`). Ob die Vorschau die Karten-Decke
   (`CardCeiling`) anwendet und die Vorwärtskarte einer `both`-Übung auf einer Schreibstufe auf die Auswahl kappt, ist
   nicht nachgesehen; sonst läuft AK 5 über den Üben-Pfad (`SeedLeitnerPosition` + `/cards`).
5. **Fremde Arbeit im Baum:** `frontend/src/vater/*` und `frontend/src/lib/fieldHelp.ts` sind sauber (kein Konflikt
   mit der ungecommitteten LearningDad-Arbeit in `sohn/*`).

### Angriffsplan

1. Backend zuerst: eine Testklasse-Erweiterung mit den Festschreibungen AK 3–5, je Richtung und Stufe über die Vorschau
   (Kästchen, Buchstaben, Freitext, Diktat, Auswahl); sie müssen im Ausgangszustand grün sein.
2. `FIELD_HELP.vocabularyHint` in `fieldHelp.ts` (Titel „Hinweis"; Auswahlstufe, Schreiben, „Deutsch → Fremdwort"),
   dazu der Test in `fieldHelp.test.ts` (AK 2).
3. `InfoHint topic="vocabularyHint"` in die Spalte (`ExerciseEditModal.tsx:559`) und als `topic` an das `RowField`
   (`exerciseConfig.tsx:670`).
4. Vitest am Editor und am Anlegen-Formular (AK 1), `dotnet build`/`dotnet format` für die Backend-Tests.

### Testweg

- **AK 1 (Regressionstest, vorher rot):** neuer Fall in `frontend/src/vater/ExerciseEditSpeichern.test.tsx` (rendert das
  Editor-Modal schon, `render(<ExerciseEditModal …>)`) – `getByRole("button", { name: /Erklärung zu „Hinweis"/ })` in der
  Wortpaare-Tabelle; ein zweiter Fall am Anlegen-Formular in `frontend/src/vater/exerciseConfig.test.ts` bzw. dem Test,
  der das Formular rendert. Rot macht es: der `<th>` bleibt ein nackter Text, der Knopf fehlt.
- **AK 2:** `frontend/src/lib/fieldHelp.test.ts`, drei `toMatch` auf `FIELD_HELP.vocabularyHint.text` (Auswahl, Schreiben,
  „Deutsch → Fremdwort"). Rot macht es: ein gestrichener Satz oder ein fehlendes Thema.
- **AK 3–5 (Festschreibungen, grün im Ausgangszustand):** `backend/Pugling.Api.Tests/VokabelTippTests.cs` (Hilfsmethode
  `HintAsync` auf Stufe und Eigen-Hinweis verallgemeinern) oder `ExercisePreviewTests.cs`; der Test
  `PositionTestFlowTests.Abschlusstest_Liefert_Keinen_Tipp_Das_Ueben_Schon` sichert heute nur die Auswahlstufe, das
  Schreiben steht dort als Kommentar (`:511`). Rot macht sie: `Swap` übernimmt den Hinweis, oder `Written` wird ohne
  `Swap` gebaut.
- Vor der Abnahme: `frontend-reviewer`; Rollengang als Creator (Editor öffnen, „ⓘ" lesen) – oder ein E2E nach dem
  Vorbild von `e2e/auch-richtig-im-uebungseditor.spec.ts`.

## Verlauf

- **2026-11-17** — angelegt aus der Nachschau von Sprint 82 (🟡 des `pugling-reviewer`), `entgangen_bei: [B-597]`.
- **2026-11-19** — ausformuliert (Vorbereitungslauf): Verwerfen je Richtung am Code belegt; die Lücke ist breiter
  (`back-to-front` verliert den Hinweis schon auf der Auswahl, auch vor B-597); „7 Übungen" nicht nachprüfbar.
- **2026-11-19** — gegrillt (Vorbereitungslauf, Sonnet, stellvertretend, unbestätigt): fünf Entscheidungen; Erklärtext in `fieldHelp.ts`, kein Schreib-Hinweisfeld; AK 3–5 sind Festschreibungen.
- **2026-11-19** — geschaetzt (Vorbereitungslauf, Sonnet): S, frontend, keine Migration, kein Vertragsbruch; Produktcode nur im Frontend, drei Festschreibungen im Backend.
