---
tags: [typ/story, status/geschaetzt, bereich/frontend, bereich/katalog, lerntechnik/vokabeln, rolle/creator]
aliases: [Inline-Vokabeln ohne Varianten, Übungs-Editor Alternativen]
status: geschaetzt
prio: P3
art: Wunsch
groesse: M
wo: beides
migration: nein
vertragsbruch: nein
quelle: docs/backlog/B-65-vokabel-mehrere-uebersetzungen.md (Review-Nebenbefund)
---

# B-71 · Die Inline-Vokabelliste im Übungs-Editor kann keine gleichwertigen Übersetzungen anlegen

Vokabeln lassen sich auf zwei Wegen anlegen: im Vokabel-Store (`/vater/vokabeln`) und inline beim
Erstellen/Pflegen einer Vokabelübung (`VocabItem`/`VocabItemInput` mit Front/Back/Hint). Seit
[B-65](B-65-vokabel-mehrere-uebersetzungen.md) trägt der erste Weg ein Feld für gleichwertige
Übersetzungen (`TranslationAlternatives`), der zweite nicht — wer so autort, muss für jede Variante ein
zweites Mal in den Store gehen. Da der Inline-Weg der bequemere ist, entstehen die Einträge, die den
B-65-Defekt auslösen, weiter genau dort.

## User Story

Als Creator möchte ich beim inline Anlegen oder Pflegen eines Vokabel-Items direkt gleichwertige
Übersetzungen angeben können, damit ich nicht für jede Variante zusätzlich in den separaten Vokabel-Store
wechseln muss und ein Kind beim Abschlusstest nicht wegen einer nicht hinterlegten, aber richtigen
Übersetzung als falsch bewertet wird.

## Ist-Stand am Code

- Der inline-Vertrag kennt kein Alternativen-Feld: `VocabItem` (Config-Ebene,
  [ExerciseConfigs.cs:53-54](../../backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs)) und
  `VocabItemInput`/`VocabItemResponse`
  ([ExerciseAuthoringDtos.cs:49-59](../../backend/Pugling.Contracts/Creator/ExerciseAuthoringDtos.cs))
  tragen nur `Front`/`Back`/`Hint`/`VocabularyId` — kein `TranslationAlternatives`.
- Der Store-Vertrag hat das Feld seit B-65: `VocabularyResponse.TranslationAlternatives`,
  `CreateVocabularyDto.TranslationAlternatives`, `UpdateVocabularyDto.TranslationAlternatives` (+
  `ClearTranslationAlternatives`)
  ([VocabularyStoreDtos.cs:11-12,27-31,40-43](../../backend/Pugling.Contracts/Creator/VocabularyStoreDtos.cs)).
- Die Verlinkung inline → Store läuft ausschließlich über
  `VocabularyStoreService.GetOrCreateAsync(sourceLanguage, word, targetLanguage, translation, partOfSpeech?, ct)`
  ([VocabularyStoreService.cs:20-43](../../backend/Pugling.Api/Services/Creator/VocabularyStoreService.cs))
  — die Methode nimmt keine Alternativen entgegen und setzt Felder **nur beim Anlegen**; findet sie einen
  bestehenden Eintrag über den Key (Zeile 26-30), übernimmt sie an ihm nichts. Aufgerufen wird sie u. a. aus
  `ExerciseControllers.cs:287` (`ResolveVocabularyIdAsync`, die private Hilfsmethode hinter `AddItem`/
  `PatchItem`) und `:391`/`:558` (Batch-Resolve, freie Glosse).
- Zwei getrennte Vokabel-Endpunkte nutzen `VocabItemInput` unverändert:
  `POST …/vocabulary/{exerciseId}/items` (`AddItem`,
  [ExerciseControllers.cs:168](../../backend/Pugling.Api/Controllers/Creator/ExerciseControllers.cs)) und
  `PATCH …/items/{itemId}` (`PatchItem`, `:210`).
- **Zwei getrennte Frontend-Oberflächen** zeigen dieselbe Lücke, nicht nur eine:
  - Neuanlage-Assistent `frontend/src/vater/exerciseConfig.tsx` — `emptyRow` für den Typ `Vocabulary`
    liefert nur `{ front, back, hint }` (Zeile 82), die Zeilenfelder rendern nur Front/Back/Hint
    (Zeilen 487-491). Zum Vergleich: die Typen `Translation`/`List`/`Cloze` haben dort längst ein
    `alternatives`-Array samt fertiger `RowRepeatedField`-Komponente „Auch richtig" (Zeilen 500-502,
    510-512, 535-537) — nur eben als exercise-lokales Feld, nicht als Durchreiche zum Store (siehe „Die
    echte Lücke").
  - Item-Verwaltung an der bestehenden Übung `frontend/src/vater/ExerciseEditModal.tsx` — die Zeilen-
    Anzeige zeigt Front/Back/Hint (Zeilen 384-421), die Anlegen-Form `NewItemForm` fragt nur Front/Back ab
    (Zeilen 440-499). Auch hier fehlt jedes Alternativen-Feld.
- B-65 selbst nennt die Lücke als abgeleiteten, bewusst nicht mitgebauten Nebenbefund
  ([B-65, Verlauf](B-65-vokabel-mehrere-uebersetzungen.md#verlauf): „B-71 … die Inline-Liste im
  Übungs-Editor kann keine Varianten anlegen").

## Die echte Lücke

Enger als der erste Blick vermuten lässt: Es ist **nicht** dasselbe Alternativen-Muster wie bei
`Translation`/`List`/`Cloze`, das nur im UI fehlt. `TranslationItem.Alternatives`
([ExerciseConfigs.cs:154](../../backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs)) ist ein
**exercise-lokales** Akzeptanz-Feld in der Config-JSON — unabhängig davon, ob das Item überhaupt mit dem
Store verlinkt ist (`VocabularyId` dort ist optional). Ein `VocabItem` dagegen ist **immer** store-gebunden
(`VocabularyId` steht nach dem Resolve fest, `ExerciseItemService.cs:17-63`); Front/Back kommen laut
Vertragsdoku ohnehin live vom Store (`VocabItemResponse`-Kommentar,
[ExerciseAuthoringDtos.cs:44-45](../../backend/Pugling.Contracts/Creator/ExerciseAuthoringDtos.cs)). Eine
zweite, exercise-lokale Alternativen-Liste für ein VocabItem wäre also eine **zweite Wahrheit** für
dieselbe Vokabel — genau die Verdopplung, die B-65 beheben sollte.

Die echte Lücke ist damit: **das inline-Formular hat keinen Weg, auf `Vocabulary.TranslationAlternatives`
zu schreiben** — weder beim Anlegen (der Store-Eintrag entsteht ohne Alternativen) noch beim Pflegen (kein
Feld dafür in beiden Editoren), und der Schreibpfad selbst (`GetOrCreateAsync`) müsste erweitert werden,
weil er heute an bestehenden Treffern nichts verändert.

## Offene Punkte

1. ~~Gehört das Feld an die Inline-Zeile (mehr Formular in einer ohnehin dichten Maske) oder soll der Weg
   stattdessen auf den Store verweisen?~~ → siehe Entscheidung 1.
2. ~~Wo im Vertrag landet das Feld — dupliziert wie bei `Translation`/`List`/`Cloze` (`Alternatives` in der
   Config) oder als Durchreiche zum Store-Feld `TranslationAlternatives`?~~ → siehe Entscheidung 2.
3. ~~Wie verhält sich das Schreiben, wenn `GetOrCreateAsync` eine bereits existierende Vokabel *findet*
   statt sie anzulegen — werden mitgeschickte Alternativen dann verworfen oder auf den bestehenden Eintrag
   geschrieben?~~ → siehe Entscheidung 3.
4. ~~Braucht das Feld einen expliziten Clear-Schalter (PATCH-Semantik: `null` = unverändert)?~~ → siehe
   Entscheidung 4.
5. ~~Eine Editor-Oberfläche oder zwei — betrifft es `exerciseConfig.tsx` (Neuanlage-Assistent) *und*
   `ExerciseEditModal.tsx` (Item-Pflege an der bestehenden Übung) gleichermaßen?~~ → siehe Entscheidung 5.
6. ~~Muss `VocabItemResponse` das Feld beim Lesen mitliefern, damit ein Editor bestehende Alternativen
   überhaupt anzeigen/bearbeiten kann?~~ → siehe Entscheidung 6.

## Entscheidungen

1. **Das Feld gehört an die Inline-Zeile, kein Verweis auf den Store.** Der Inline-Weg ist der bequemere —
   genau deshalb entstehen dort die B-65-auslösenden Lücken (siehe Story-Einleitung). Ein Verweis auf den
   Store verlagert die Reibung nur zurück auf den Weg, den diese Story schließen soll. *Kosten:* ein
   zusätzliches Eingabefeld in einer bereits dichten Zeile (Front/Back/Hint) — vertretbar, weil
   `RowRepeatedField` („Auch richtig") als fertige Komponente für `Translation`/`List`/`Cloze` schon
   existiert (`frontend/src/vater/exerciseConfig.tsx:500-502` u. a.) und nur eingebunden werden muss.
2. **Durchreiche zum Store-Feld `TranslationAlternatives`, keine zweite/parallele Liste.** Jedes
   `VocabItem` ist — anders als ein `TranslationItem` — immer store-gebunden; eine exercise-lokale
   Alternativen-Liste wäre eine zweite Wahrheit für dieselbe Vokabel und liefe B-65 zuwider. *Kosten:*
   `VocabItemInput`/`VocabItemResponse` bekommen ein additives Feld `TranslationAlternatives` (Contracts-
   Projekt, `ExerciseAuthoringDtos.cs`); kein neues Config-Schema, kein Feld auf `VocabItem` (Config-Ebene)
   nötig, da diese Ebene nur beim initialen Batch-Import greift und danach ohnehin durch die Item-Tabelle
   ersetzt wird (`ExerciseItemService`).
3. **Ein neuer, expliziter Schreibpfad statt Erweiterung von `GetOrCreateAsync`.** Die Methode setzt Felder
   bewusst nur beim Anlegen (Kommentar `VocabularyStoreService.cs:25f`: mehrere Items derselben Vokabel in
   einer Übung dürfen sich nicht gegenseitig überschreiben) — mitgeschickte Alternativen dürfen darum nicht
   in diese Methode einsickern, sonst ändert ein zweites Item mit anderen Alternativen still den ersten
   Fund. Stattdessen: ein kleiner, expliziter Folgeschritt in `ResolveVocabularyIdAsync`
   (`ExerciseControllers.cs:280`), der die Alternativen — ob neu angelegt oder gefunden — gezielt auf den
   Store-Eintrag schreibt, wenn welche mitgeschickt wurden. *Kosten:* ein zusätzlicher, kleiner EF-Write-
   Pfad in `VocabularyStoreService`, keine Signaturänderung der bestehenden Methode (kein Risiko für die
   drei anderen Aufrufer).
4. **Kein eigener Clear-Schalter — die explizit leere Liste heißt löschen.** Anders als `UpdateVocabularyDto`
   (echtes PATCH-DTO mit `ClearTranslationAlternatives`, weil dort `null` zwingend „unverändert" bedeutet)
   ist `VocabItemInput` bei `PatchItem` bereits so verdrahtet, dass ein gesetztes Feld überschreibt
   (`Hint` nutzt Leerstring statt `null` als Löschsignal, `ExerciseControllers.cs:226`). Gleiches Muster für
   die Liste: `null` = unverändert lassen, `[]` (explizit leere Liste, von `null` in JSON unterscheidbar) =
   löschen. *Kosten:* keine zusätzliche Property, aber die Regel muss ausdrücklich in der `///`-Doku des
   Feldes stehen — sonst wiederholt sich die B-65-Fußangel „Leerung sieht aus wie Nicht-Angabe" (in B-65
   erst im zweiten Review-Durchgang gefunden, siehe dessen Verlauf).
5. **Beide Oberflächen bekommen das Feld.** `exerciseConfig.tsx` (Neuanlage-Assistent, Zeilen 82/487-491)
   *und* `ExerciseEditModal.tsx` (Item-Pflege an der bestehenden Übung, Zeilen 384-421/440-499) erzeugen/
   patchen dieselben `VocabItemInput`-Objekte gegen dieselben Endpunkte — ein Auslassen einer Seite hieße,
   dass die Lücke nur zur Hälfte schließt: wer eine Übung nachträglich pflegt (der häufigere Fall, da neue
   Vokabeln meist einzeln nachgetragen werden), träfe sie weiter. *Kosten:* zwei Frontend-Stellen statt
   einer, das treibt die Größe von S auf M.
6. **`VocabItemResponse` liefert die Alternativen mit (additiv).** Ohne Rücklesen zeigt der Editor beim
   erneuten Öffnen eines Items nie, was schon hinterlegt ist — dieselbe Falle, die die Root-`CLAUDE.md`
   unter PATCH-Semantik nennt („Formular zeigt „– keine Angabe –", obwohl ein Wert da ist"). *Kosten:* ein
   zusätzliches, rein lesendes Feld in `VocabItemResponse` (`ExerciseAuthoringDtos.cs:49-51`), gespeist aus
   `Vocabulary.TranslationAlternatives` beim Mapping (analog zu `Front`/`Back`).

## Akzeptanzkriterien

1. Beim Anlegen eines neuen Vokabel-Items (`POST …/vocabulary/{exerciseId}/items`, inline via Front/Back)
   können optional weitere gleichwertige Übersetzungen mitgeschickt werden; sie landen auf demselben
   Store-Eintrag, den `GET creator/vocabulary/{id}` unter `translationAlternatives` zeigt.
2. Beim Ändern eines Items (`PATCH …/items/{itemId}`) können Alternativen ergänzt, ersetzt oder (explizit
   leere Liste) gelöscht werden; ein weggelassenes Feld lässt den bestehenden Stand unangetastet.
3. Trifft das Anlegen/Ändern auf einen bereits im Store existierenden Eintrag (Fund über den Vokabel-Key),
   werden mitgeschickte Alternativen trotzdem übernommen — nicht stillschweigend verworfen, weil
   `GetOrCreateAsync` nur den Neuanlage-Pfad bedient.
4. Das einzelne Item (`VocabItemResponse`) liefert die aktuell hinterlegten Alternativen zurück
   (Round-Trip fürs Bearbeiten).
5. Im Vater-Web zeigen sowohl der Neuanlage-Assistent (`/vater/exercises/neu`, Typ Vokabel) als auch der
   Item-Dialog einer bestehenden Vokabelübung (`ExerciseEditModal`) ein Eingabefeld „Auch richtig" je
   Zeile/Item, das denselben Store-Eintrag befüllt, den `/vater/vokabeln` schon zeigt.
6. Eine über diesen Weg hinterlegte Alternative wertet beim Abschlusstest weiterhin als richtig
   (bestehendes B-65-Verhalten, hier nur über den zweiten Eingabeweg erreichbar — kein neuer
   Auswertungscode nötig).
7. `unknown_field` bleibt scharf: ein falsch benanntes Feld im Request wird weiter mit 400 abgelehnt (kein
   stiller Sonderfall für das neue Feld).

## Schätzung

**Größe: M** — Anker [B-03](B-03-lueckensaetze-mit-bild.md) (vokabel-basierter Batch-Pfad im
`MediaSelector`): ähnlich verteilte Arbeit über Contracts + Service + zwei Endpunkte + zwei
Frontend-Stellen. Kleiner als B-65 (kein Scoring-Codepfad, keine Migration, kein neues Schema-Tor), größer
als eine reine S, weil zwei Editor-Oberflächen statt einer gepflegt werden und der Store-Fund-Sonderfall
(Entscheidung 3) ein echter neuer Verzweigungspunkt ist.

- **wo:** beides — Backend (`Pugling.Contracts`, `VocabularyStoreService`, `ExerciseControllers.cs`) und
  Frontend (`exerciseConfig.tsx`, `ExerciseEditModal.tsx`).
- **migration:** nein — `Vocabulary.TranslationAlternatives` existiert als Spalte/JSON-Property bereits
  seit B-65; diese Story verdrahtet nur einen zweiten Schreibweg dorthin.
- **vertragsbruch:** nein — `VocabItemInput`/`VocabItemResponse` bekommen additive, optionale Felder;
  bestehende Clients bleiben unverändert lauffähig.

**Risiken:**

- Der Store-Fund-Sonderfall (Entscheidung 3) ist die einzige neue Verzweigung mit echtem
  Seiteneffekt-Risiko: ein zu freizügiges Schreiben könnte den Alternativen-Stand einer Vokabel
  überschreiben, die aus einer ganz anderen Übung heraus schon anders gepflegt wurde. Ein Integrationstest
  muss genau diesen Fall (zwei Items, dieselbe Vokabel, unterschiedlich mitgeschickte Alternativen)
  abdecken.
- Zwei Frontend-Oberflächen zu pflegen heißt auch: zwei Stellen, an denen das neue Feld auseinanderlaufen
  kann (unterschiedliche Feld-Bezeichnung, unterschiedliches Lade-/Sende-Mapping). `frontend-reviewer`
  sollte beide Diffs sehen.
- `VocabItemInput` fällt nicht ins Namensraster von `PatchSemanticsTests.UpdateDtos()`
  (`Update…Dto`/`…Request`) — der reflexive Wächter greift hier nicht automatisch. Entscheidung 4 (leere
  Liste statt Clear-Schalter) muss darum über einen eigenen Integrationstest abgesichert werden, nicht über
  das Tor.

**Angriffsplan** (Backend zuerst)

1. `VocabularyStoreService`: neue, kleine Methode für den expliziten Alternativen-Schreibpfad
   (Entscheidung 3), mit Integrationstest für den Fund-Fall.
2. Contracts: `TranslationAlternatives` additiv auf `VocabItemInput`/`VocabItemResponse`
   (`ExerciseAuthoringDtos.cs`), mit `///`-Doku zur Lösch-Semantik (Entscheidung 4).
3. `ExerciseControllers.cs`: `AddItem`/`PatchItem`/`ResolveVocabularyIdAsync` um den neuen Schreibpfad
   ergänzen, Mapping in `VocabItemResponse`.
4. Integrationstests in `Pugling.Api.Tests` (Anlegen mit Alternativen, Patchen, Store-Fund-Fall,
   Round-Trip lesen, `unknown_field` weiter scharf).
5. Frontend: `exerciseConfig.tsx` (Vocabulary-Zeile um `RowRepeatedField` ergänzen, `emptyRow`/Sende-/
   Lade-Mapping).
6. Frontend: `ExerciseEditModal.tsx` (Zeilen-Anzeige + `NewItemForm` um dasselbe Feld ergänzen).
7. `/smoke-test` gegen den echten Flow (Vokabelübung anlegen, Alternative mitschicken, Abschlusstest mit
   der Alternative bestehen).

**Testweg:** neue Integrationstests in `Pugling.Api.Tests` (Vorbild: die `VocabularyStoreTests`/
`VocabularyController`-Tests rund um B-65/B-81) für die vier Backend-Akzeptanzkriterien; ein
Komponententest für die beiden Frontend-Stellen; abschließend `/smoke-test` als End-to-End-Beleg vor der
Abnahme, dazu `pugling-reviewer` und `frontend-reviewer` (`wo: beides`).

## Verlauf

- **2026-08-02** — aufgenommen als Nebenbefund des `frontend-reviewer` beim Bau von B-65; nicht selbst
  nachgeprüft.
- **2026-08-03** — ausformuliert: Ist-Stand gegen den Code belegt (`VocabItemInput`/`VocabItemResponse`
  ohne Alternativen-Feld, `VocabularyStoreService.GetOrCreateAsync` ohne Alternativen-Parameter und ohne
  Update-Pfad für gefundene Einträge, zwei getrennte Frontend-Oberflächen `exerciseConfig.tsx` und
  `ExerciseEditModal.tsx`); dabei die echte Lücke präzisiert — sie ist kein Kopieren des
  `Translation`-Alternativen-Musters, weil dessen `Alternatives` ein exercise-lokales Feld ist, während ein
  `VocabItem` immer store-gebunden ist.
- **2026-08-03** — gegrillt: alle sechs offenen Punkte in nummerierte Entscheidungen überführt (Feldort an
  der Inline-Zeile, Durchreiche zum Store-Feld statt Duplikat, expliziter neuer Schreibpfad statt
  `GetOrCreateAsync`-Erweiterung, keine Clear-Schalter-Property, beide Editor-Oberflächen, additives
  Rücklesen in `VocabItemResponse`); autonom getroffen, Nutzerauftrag 2026-08-04.
- **2026-08-03** — geschätzt: **Größe M**, `wo: beides`, `migration: nein`, `vertragsbruch: nein`,
  Angriffsplan (Backend zuerst: Service-Schreibpfad → Contracts → Controller → Tests → beide
  Frontend-Stellen → `/smoke-test`) und Testweg festgelegt; autonom getroffen, Nutzerauftrag 2026-08-04.
- **2026-08-07** — Autonomer Modus (Opt-in je Vorhaben, README → „Autonomer Modus") vom Nutzer im Dialog
  ausdrücklich freigegeben: ein Nachtlauf darf diese Story trotz `art: Wunsch` ohne weitere Rückfrage bauen
  (Rollengang/Reviewer bleiben Pflicht wie bei jeder Abnahme).
