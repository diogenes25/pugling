---
tags: [typ/story, status/ausformuliert, bereich/training, rolle/creator, lerntechnik/vokabeln]
aliases: [Hinweis fehlt bei Fremdwort → Deutsch]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
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

1. **Wie erfährt der Creator es?** (a) Ein Erklärtext am Feld („ⓘ", Muster `FieldLabel`/`InfoHint` aus
   `frontend/CLAUDE.md`), der die drei Fälle nennt; (b) die Spalte je Richtung ausgrauen; (c) ein eigenes
   Schreib-Hinweisfeld je Zeile. **Empfehlung (a):** Frontend-only, keine Migration, und die Richtung steckt in der
   Config, nicht im Zeilen-Zustand – (b) bräuchte Plumbing für einen Randfall. (c) ist eine neue Funktion, keine
   Fehlerbehebung.
2. **Soll der Hinweis auch beim Schreiben erscheinen?** **Empfehlung nein:** er nennt die deutsche Antwort, das
   Schreiben fragt das Fremdwort. Wer einen Schreib-Hinweis will, hebt (c) als eigene Idee.
3. **Gehört „back-to-front verliert den Hinweis überall" in dieselbe Story?** **Empfehlung ja:** gleiche Ursache,
   derselbe Erklärtext, und ohne sie wäre der Text für diese Richtung falsch.

## Akzeptanzkriterien (Entwurf)

1. Die Spalte „Hinweis" im Editor trägt ein „ⓘ" mit einem Text, der Auswahl, Schreiben und die Richtung
   `back-to-front` nennt. Rot macht es: die Überschrift bleibt ein nackter `<th>`-Text (heute so).
2. Eine Vokabel-Übung mit Richtung `front-to-back` und Hinweis liefert auf der Auswahlstufe den Hinweis und auf
   Kästchen, Buchstaben, Freitext und Diktat keinen. Rot macht es: `Swap` übernimmt den Hinweis, oder `Written`
   wird ohne `Swap` gebaut.
3. Dieselbe Übung mit `back-to-front` liefert auf **keiner** Stufe einen Hinweis. Rot macht es: `Swap` lässt den
   Hinweis stehen.
4. Mit `both` trägt die Vorwärtskarte den Hinweis auf der Auswahl, die Rückkarte nie. Rot macht es: `RecognitionOnly`
   wird dem Hinweis zum Verhängnis oder die Rückkarte erbt ihn.
5. Der Text aus AK 1 steht in `fieldHelp.ts` (nicht am Feld); ein Tippfehler im Thema fällt über `HelpTopic` beim
   Übersetzen auf.

## Verlauf

- **2026-11-17** — angelegt aus der Nachschau von Sprint 82 (🟡 des `pugling-reviewer`), `entgangen_bei: [B-597]`.
- **2026-11-19** — ausformuliert (Vorbereitungslauf): Verwerfen je Richtung am Code belegt; die Lücke ist breiter
  (`back-to-front` verliert den Hinweis schon auf der Auswahl, auch vor B-597); „7 Übungen" nicht nachprüfbar.
