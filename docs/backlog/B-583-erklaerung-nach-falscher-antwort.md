---
tags: [typ/story, status/abgenommen, bereich/training, rolle/student, rolle/creator]
aliases: [Erklärung nach dem Fehler, Warum falsch, Vokabel-Erklärung]
status: abgenommen
nachgeschaut: 2026-11-16
prio: P3
art: Wunsch
groesse: S
wo: beides
migration: ja
vertragsbruch: nein
quelle: docs/rockettutor-vergleich-plan.md#wo-rockettutor-weiter-ist
unverifiziert: false
grund: ""
ersetzt_durch: []
---

# B-583 · Nach einer falschen Antwort erklärt die Übung, warum

RocketTutors Kern ist ein KI-Tutor, der erklärt, warum eine Antwort falsch ist. Pugling zeigt nach der
Antwort nur das erwartete Ergebnis (`Expected`). Am Item gibt es einen optionalen `Hint`
(`backend/Pugling.Api/Models/ExerciseItemEntities.cs:34`), aber keine Erklärung, die *nach* der Antwort
kommt (etwa: „ne … pas umschließt das Verb", „unregelmäßig: went, nicht goed"). Die Idee: eine kurze
Erklärung je Item oder je Übung, die der KI-Creator **offline** beim Erzeugen mitschreibt und die der
Creator vor dem Zuweisen sieht und ändern kann. Der Server gibt sie wie `Expected` erst nach der Antwort
heraus. Das hält „kein LLM im Backend" und die Lösungsfeld-Regel ein: die Erklärung ist ein Reveal, kein
Lösungsfeld vor der Antwort.

**Neu geschnitten am 2026-11-15:** Der Transportweg steht seit B-529 (abgenommen) – `ReviewOutcome.Explanation`, nur
nach der Antwort, in der Arcade sichtbar. Gefüllt wird er aber nur von Regelaufgaben. Diese Story schließt die Lücke
für **Vokabeln**, den Hauptstoff beider Söhne; andere Typen und das Befüllen durch den KI-Creator sind zurückgestellt.

## User Story

Als **Creator** möchte ich an einer Vokabel einer Übung eine kurze Erklärung hinterlegen („unregelmäßig: went, nicht
goed“), damit mein Sohn nach dem Antworten nicht nur die Lösung sieht, sondern versteht, warum – ohne dass die
Erklärung ihm vor der Antwort etwas verrät.

## Ist-Stand am Code

Nachgemessen am 2026-11-15:

- **Vertrag steht** (B-529): `ReviewOutcome(…, string? Explanation = null)`
  (`backend/Pugling.Contracts/Student/PracticeDtos.cs:126-128`, Doku `:120-124`: nach der Antwort, bei richtig **und**
  falsch, `null` ohne getroffenes Atom und „für Inhalte ohne Erklärung (Vokabeln)“).
- **Einziger Ausgabeort**: `PositionPracticeController.cs:582` – `items[explained].Explanation` aus dem
  `ContentItem` des gutgeschriebenen Atoms, nur in der Antwort auf `…/answer`. Die Karte (`PracticeCard`,
  `PracticeDtos.cs:81-85`) trägt kein solches Feld; Test/Klausur (`PositionTestsController`) geben keine Erklärung aus.
- **Träger im Inhaltsmodell**: `ContentItem.Explanation` (`Services/Shared/ExerciseContentProvider.cs:49`, Doku
  `:29-30`: „nie auf der Karte“, Abgrenzung zum `Hint` = Hilfe vor der Antwort).
- **Wer füllt**: nur die Regelaufgabe aus `RuleHint` (`Exercises/BuiltInExerciseTypes.cs:118-121`).
- **Vokabeln**: `ExerciseContentResolver.ResolveVocabularyItemsAsync` liest je Item nur `Id, VocabularyId, Hint`
  (`Services/Shared/ExerciseContentResolver.cs:79`) und baut das `ContentItem` ohne Erklärung (`:101-103`). Die
  Richtungsumkehr `Swap` leert den `Hint`, übernimmt alles andere per `with` (`ExerciseContentProvider.cs:104`).
- **Entity**: `ExerciseItem.Hint` (`Models/ExerciseItemEntities.cs:34`) – keine Spalte für eine Erklärung.
- **Creator-Vertrag**: `VocabItemResponse(…, string? Hint, …)` und `VocabItemInput(…, string? Hint = null, …)`
  (`Pugling.Contracts/Creator/ExerciseAuthoringDtos.cs:71-72`, `:90-91`; „leer löscht, gesetzt überschreibt“), Inline-
  Form in der Übungs-Konfiguration `VocabItem(Front, Back, Hint, …)` (`Pugling.Contracts/Exercise/ExerciseConfigs.cs:58`).
  Schreiber: `ExerciseControllers.cs:235` (Anlegen), `:279` (PATCH), `NormalizeHint` `:317`; Konfigurations-Abgleich
  `Services/Creator/ExerciseItemService.cs:119/127`; Lesen `MapItem` `ExerciseControllers.cs:169-171`.
- **Arcade** zeigt die Erklärung schon: `frontend/src/sohn/SohnPractice.tsx:263` (richtig) und `:278` (falsch).
- **Creator-Editor**: Hinweisfeld je Item in `frontend/src/vater/ExerciseEditModal.tsx:586-642` (PATCH je Item) und
  in der Anlege-Zeile `frontend/src/vater/exerciseConfig.tsx:38/641` (`VocabularyRow`).
- **Bestehender Test** `PositionPracticeFlowTests.cs:504-508` (B-529 AK 3): ein Vokabel-`Hint` erscheint **nicht** als
  Erklärung. Bleibt gültig.
- **Lösungsfeld-Regel** (`CLAUDE.md`, Tor `ConventionGuardTests.Actions_Mit_Loesungsfeld_Sind_Vor_Dem_Studenten_Gegated`)
  greift nur auf die Namen `Answer`/`Solution`/`CorrectAnswer`/`Translation`; `Explanation` fällt nicht darunter – das
  Tor schützt diese Story also **nicht** mechanisch.

## Die echte Lücke

Vokabel-Items können keine Erklärung tragen: keine Spalte, kein Vertragsfeld für den Creator, keine Abbildung ins
`ContentItem`. Der Weg zum Kind (Outcome, Arcade) ist fertig und bleibt unverändert.

## Offene Punkte

1. ~~Auf welcher Ebene hängt die Erklärung – Item, Übung, Vokabel im Store?~~ → Entscheidung 1.
2. ~~Erscheint sie auch bei richtiger Antwort?~~ → Entscheidung 2.
3. ~~Abgrenzung zum `Hint`?~~ → Entscheidung 3.
4. ~~Welche Übungstypen in diesem Schnitt?~~ → Entscheidung 4.
5. ~~Füllt der KI-Creator sie?~~ → Entscheidung 5 (zurückgestellt).
6. ~~Wie wird sichergestellt, dass sie nie vor der Antwort beim Kind ankommt?~~ → Entscheidung 6.
7. ~~Umkehr-Richtung (Übersetzung → Wort)?~~ → Entscheidung 7.

## Entscheidungen

1. **Am `ExerciseItem` (übungslokal), Spalte `Explanation`, neben `Hint`.** Stakeholder-Entscheidung im Auftrag des
   Nutzers. Begründung: gleiche Ebene und gleiche Pflege wie der `Hint`; der Creator schreibt sie für *diese* Übung,
   ohne Nebenwirkung auf andere Übungen mit demselben Store-Wort. Kosten: dieselbe Erklärung für dasselbe Wort muss in
   jeder Übung neu geschrieben werden – ein Store-weiter Rückfall (wie `ArticleHint` beim Hinweis) ist zurückgestellt
   und kann additiv folgen.
2. **Bei richtiger und falscher Antwort**, wie B-529 und die Vertrags-Doku (`PracticeDtos.cs:120-124`) es schon sagen.
   Stakeholder-Entscheidung im Auftrag des Nutzers. Begründung: eine zweite Regel nur für Vokabeln wäre eine zweite
   Wahrheit über dasselbe Feld; die Arcade zeigt sie schon in beiden Blättern. Kosten: der Titel („nach einer falschen
   Antwort“) ist enger als das Verhalten – bewusst.
3. **`Hint` = Hilfe vor der Antwort, `Explanation` = Begründung nach der Antwort; kein gegenseitiger Rückfall.**
   Stakeholder-Entscheidung im Auftrag des Nutzers. Begründung: ein Hinweis als Erklärung wäre wertlos (das Kind hat
   ihn schon gesehen), eine Erklärung als Hinweis verriete die Lösung. Kosten: zwei Felder im Editor, beide mit
   Feldhilfe erklärt.
4. **Nur Vokabeln** (der `ExerciseItem`-Typ). Stakeholder-Entscheidung im Auftrag des Nutzers. Begründung: kleinster
   Schnitt mit Wert – Vokabeln sind der Stoff beider Söhne, und nur dieser Typ hat eine eigene Item-Tabelle.
   Lückentext, Übersetzung, Zuordnung u. a. speichern ihre Atome in der JSON-Konfiguration und bräuchten je Typ ein
   Feld im Konfigurations-Record. Kosten: diese Typen bleiben ohne Erklärung; **zurückgestellt** als eigene Story,
   anzulegen beim Bau (README „Ein Fund beim Bauen wird eine eigene Story“).
5. **KI-Creator füllt sie nicht in diesem Schnitt – zurückgestellt.** Stakeholder-Entscheidung im Auftrag des
   Nutzers. Begründung: zuerst muss das Feld existieren und von Hand pflegbar sein; das Befüllen durch die Pipeline
   (`Pugling.Agent.Creator`, deterministische Prüfung, „C# entscheidet“) ist ein eigener Schritt mit eigenem Risiko
   (falsche Erklärungen). Kosten: bis dahin nur von Hand gepflegte Erklärungen.
6. **Kein Weg vor der Antwort: ein ausdrücklicher Regressionstest statt des Namens-Tors.** Stakeholder-Entscheidung
   im Auftrag des Nutzers. Das Feld wird nur in `ContentItem.Explanation` abgebildet, das ausschließlich
   `PositionPracticeController.cs:582` ausgibt; `PracticeCard` und die Test-/Klausur-DTOs bekommen es **nicht**. Weil
   das Tor der Lösungsfeld-Regel `Explanation` nicht kennt, hält AK 3 die Wand. Die Creator-Endpunkte, die das Feld
   lesen, sind `Roles.Creator`-gegated wie der Rest von `api/v1/creator`. Kosten: ein Test mehr; kein Umbau des Tors
   (ein Name mehr in seiner Liste würde `ReviewOutcome` – bewusst Student-lesbar – rot machen).
7. **Gilt in beiden Richtungen.** Stakeholder-Entscheidung im Auftrag des Nutzers. Begründung: die Erklärung betrifft
   das Wortpaar, nicht die Abfragerichtung; `Swap` übernimmt sie ohnehin per `with` (`ExerciseContentProvider.cs:104`).
   Kosten: eine Erklärung, die nur für eine Richtung passt, muss der Creator neutral formulieren.

## Akzeptanzkriterien

1. **Creator schreibt und liest**: `POST …/vocabulary/{id}/items` und `PATCH …/items/{itemId}` nehmen `explanation`
   an (leer löscht, gesetzt überschreibt, weggelassen bleibt – wie `hint`); `GET …/items` und die Konfigurations-
   Rücklesung liefern sie zurück, auch über die Inline-Form `VocabItem.explanation` beim Anlegen. Integrationstest.
   **Rot**, wenn der Controller das Feld nicht übernimmt oder `MapItem` es nicht ausgibt.
2. **Kind bekommt sie nach der Antwort**: eine Vokabel-Position, Item mit Erklärung, falsch getippt →
   `ReviewOutcome.explanation` = die Erklärung; richtig getippt ebenso. Item ohne Erklärung → `null`
   (B-529 AK 3 bleibt grün). **Rot**, wenn `ResolveVocabularyItemsAsync` die Spalte nicht ins `ContentItem` abbildet.
3. **Nie vor der Antwort**: die Karte (`…/next` bzw. `PracticeCard` im Sitzungsstart), die Test-Karten und die
   Test-Auswertung derselben Position enthalten die Erklärung **nicht** (Assertion auf den rohen JSON-Text: der
   Erklärungstext kommt nicht vor). **Rot**, wenn jemand `Explanation` in `PracticeCard` oder ein Test-DTO aufnimmt.
4. **Umkehr-Richtung**: mit `direction: back-to-front` liefert das Outcome dieselbe Erklärung. **Rot**, wenn `Swap`
   sie leert.
5. **Migration**: neue Migration hängt an der Kette (Spalte `ExerciseItems.Explanation`, nullable, keine
   Datenänderung), `SchemaGuardTests` grün. **Rot** (G-Tore) ohne Migration.
6. **Editor beidseitig**: im Bearbeiten-Dialog (`ExerciseEditModal`) ein Feld „Erklärung (nach der Antwort)“ je Item
   mit Read-back und PATCH; in der Anlege-Zeile (`exerciseConfig.tsx`) ein optionales Feld. Vitest. **Rot**, wenn der
   gespeicherte Wert beim Öffnen fehlt.

## Schätzung

**Größe S** (Anker B-01; der Transport ist da, es fehlen eine Spalte, zwei Vertragsfelder, eine Abbildung, zwei
Editor-Felder). `wo: beides`, `migration: ja` (eine nullable Spalte, angehängt, Bestand unverändert),
`vertragsbruch: nein` (additive Felder mit Vorgabe `null` als **letzte** Parameter der Records – positionale
C#-Aufrufer wie `Pugling.Client/CreatorApi.cs:254-260` brechen nicht).

### Risiken

| Risiko | Gegenmittel |
| --- | --- |
| Erklärung leckt vor der Antwort (das Namens-Tor sieht `Explanation` nicht) | AK 3 als ausdrücklicher Regressionstest, Text-Assertion auf die rohen Antworten |
| Konfigurations-Abgleich (`ExerciseItemService.cs:119/127`) übernimmt das Feld nicht, Inline-Anlage verliert es | AK 1 auch über die Inline-Form fahren |
| `unknown_field`: ein Frontend, das `explanation` schickt, bevor der Vertrag es kennt | Backend und Vertrag (`docs/openapi/v1.json`) zuerst, Frontend danach |
| Migration auf der echten DB | nur `AddColumn` nullable; vor dem Commit gegen eine Kopie starten |

### Angriffsplan

1. Entity `ExerciseItem.Explanation` + Migration anhängen.
2. Vertrag: `VocabItemInput.Explanation`, `VocabItemResponse.Explanation`, `VocabItem.Explanation` (je letzter
   Parameter, Vorgabe `null`); Controller Anlegen/PATCH/Map mit derselben Normalisierung wie `Hint`;
   `ExerciseItemService` Abgleich.
3. `ExerciseContentResolver.cs:79/101`: Spalte lesen und als `Explanation` ins `ContentItem`.
4. Integrationstests AK 1–5 (`PositionPracticeFlowTests` neben B-529, Creator-Item-Tests); Vertrag neu erzeugen.
5. Frontend: zwei Editor-Felder mit Feldhilfe, Vitest AK 6.

### Testweg

Integrationstests (AK 1–5), `SchemaGuardTests`, Vitest (AK 6). Rollengang: als Creator an einer Vokabel eine
Erklärung setzen, als Sohn das Wort falsch tippen und die Erklärung im Blatt sehen; eine bestehende Arcade-E2E, die
ein Blatt öffnet, kann die Erklärung mit prüfen.

## Verlauf

- **2026-11-15** — angelegt (Quelle: RocketTutor-Vergleich, Etappe 4 des Übernahmeplans; Sammel-Story [B-581](B-581-rockettutor-vergleich-sammel-story.md)).
- **2026-11-15** — Backlog-Lauf 2026-11-15: ausformuliert gegen den Code – B-529 (abgenommen) liefert Vertrag, Ausgabe nach der Antwort und Arcade-Anzeige schon; Story auf die echte Lücke **Vokabel-Items** verengt. Gegrillt mit sieben Stakeholder-Entscheidungen im Auftrag des Nutzers (andere Übungstypen und KI-Creator-Befüllung zurückgestellt) und geschätzt: **S**, `beides`, Migration ja (eine nullable Spalte), kein Vertragsbruch.
- **2026-11-15** — Prioritäts-Beleg von außen (sofatutor-Vergleich): sofatutors Übungen bieten nach dem Fehler eine Video-Hilfe, die an die passende Stelle des Erklärvideos springt. Das Pugling-Gegenstück wäre ein Verweis von der Erklärung auf die Stoffnotiz der Unit (`SeriesUnit.Grammar`/`VocabularyNotes`). Beim Ausformulieren das Verhältnis zu [B-529](B-529-erklaerung-nach-der-antwort.md) klären: beide beschreiben eine Erklärung nach der Antwort. Vergleich in `docs/sofatutor-vergleich-plan.md` (Sammel-Story [B-585](B-585-sofatutor-vergleich-sammel-story.md)).
- **2026-11-15** — umnummeriert von B-543 auf B-583: die Id war auf `origin/main` schon vergeben (main belegt bis B-552, der Backlog-Lauf vom 2026-11-15 bis B-584). Inhalt unverändert.
- **2026-11-15** — `geschaetzt` → **`abgenommen`** in Sprint 80 des Backlog-Laufs 2026-11-15, Code-Commit `73c73a6a`.
  `ExerciseItem.Explanation` (Migration `20261115215256_ExerciseItemExplanation` angehängt, gegen eine Kopie der echten
  `pugling.db` angewendet: 305 Items erhalten), Vertrag (`VocabItemInput`/`VocabItemResponse`/`VocabItem`, additiv),
  Items-POST/PATCH (leer löscht, weggelassen bleibt), Abgleich der Inline-Form, Abbildung im `ExerciseContentResolver`;
  Feld „Erklärung“ je Wort im Bearbeiten-Dialog. Abweichung: die Anlege-Zeile in `exerciseConfig.tsx` bekam kein Feld –
  Vokabel-Übungen werden dort über den Store-Picker angelegt, die Zeilen sind für diesen Typ ungenutzt; Erklärungen
  setzt der Creator im Bearbeiten-Dialog. Der `laden.py`-Feldabgleich (`LadenFieldListTests`) kennt das Feld.
  - AK 1 → `VokabelErklaerungTests.Creator_Schreibt_Und_Liest_…`; Mutanten „PATCH“, „Ausgabe“, „Inline“ → rot.
  - AK 2 → `…Das_Kind_Bekommt_…`, `…Auch_Ein_Treffer_…`; Mutante „Abbildung“ → rot.
  - AK 3 → `…Das_Kind_Bekommt_Die_Erklaerung_Erst_Nach_Der_Antwort` (roher Text von Karten, Sitzung, Teststart und
    Testauswertung enthält die Erklärung nicht).
  - AK 4 → `…Umgekehrte_Richtung_…`. AK 5 → `SchemaGuardTests` grün mit Migration.
  - AK 6 → Vitest `ExerciseItemErklaerung.test.tsx`; Mutante „Rückweg“ → rot.
  - Belege und Review: siehe Abschluss im Protokoll (Gesamt-Review deckt Sprint 80 mit ab).
