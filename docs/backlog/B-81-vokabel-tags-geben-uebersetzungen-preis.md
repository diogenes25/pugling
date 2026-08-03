---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/student]
aliases: [Vokabel-Tag gibt Übersetzungen preis, TaggedVocabularyDto trägt die Lösung,
  Kind liest jede Übersetzung des Stores, Tür D]
status: abgenommen
prio: P1
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: B-80 (Schätzung, Befund außerhalb des Schnitts)
---

# B-81 · Über die Vokabel-Tags kann ein Kind jede Übersetzung des Stores lesen

## User Story

Als **Vater** möchte ich, dass mein Kind eine Übersetzung erst dann lesen kann, wenn seine Stufe sie zeigen
darf, damit das Markieren von Vokabeln ein Lernwerkzeug bleibt und nicht der Weg wird, den ganzen
Vokabelspeicher als Wörterbuch auszulesen.

## Ist-Stand am Code

Die **vierte Tür** in dieselbe Kammer wie [B-75](B-75-lese-hoerverstehen-ohne-inhalt.md),
[B-77](B-77-liste-menge-als-folge.md), [B-80](B-80-tags-geben-fremde-konfiguration-preis.md) und
[B-82](B-82-positions-report-gibt-loesungen-preis.md) — und von keiner ihrer Reparaturen gedeckt.

### 1 · Der Schreibpfad prüft nur die Existenz

`POST tags/{tagId}/vocabulary`
([TagsController.cs:245](../../backend/Pugling.Api/Controllers/Creator/TagsController.cs)) prüft von den
Vokabel-Ids ausschließlich, **dass sie existieren** (`:252-254`) — kein Eigentum, keine Zuweisung. Der
Controller trägt klassenweit nur `[Authorize]` (`:20`), und das ist **Absicht**: Vater *und* Kind dürfen
taggen, Eigentum läuft über das Kind (`FindOwnedAsync`, `:40`).

**Die Asymmetrie ist der Kern.** Für Übungen steht die Regel elf Zeilen weiter oben schon da: `:166`
`if (User.IsStudent())` beschränkt den Studenten auf **zugewiesenes** Material — eingezogen von B-80/E2, mit
der Begründung, dass Ids fortlaufende Zahlen sind. Für Vokabeln fehlt genau dieser Block.

### 2 · Der Lesepfad nennt Wort und Übersetzung

`GET tags/{tagId}/vocabulary` (`:286`) projiziert
`new TaggedVocabularyDto(v.Id, v.Key, v.Word, v.Translation)` (`:297`)
([TagDtos.cs:21](../../backend/Pugling.Contracts/Creator/TagDtos.cs)) — also **das Paar**, und `Key` trägt es
ein zweites Mal (er ist aus Wort und Übersetzung gebildet).

### 3 · Am laufenden System nachgespielt

2026-08-03, Wegwerf-DB auf `:5280` (die echte `pugling.db` unangetastet). **Zwei Aufrufe, kein Trick, keine
Zuweisung** — das Kind benutzt sein *eigenes* Tag:

```text
POST creator/tags                {childId:1,name:"Spickzettel"}      (Kind-Token) → 200  id=3, createdBy=Sohn
POST creator/tags/3/vocabulary   {vocabularyIds:[1..12]}             (Kind-Token) → 200
GET  creator/tags/3/vocabulary                                       (Kind-Token) → 200  12 Zeilen
  house → Haus · go → gehen · goes → geht · la ville → die Stadt · la rue → die Straße
  la maison → das Haus · l'école → die Schule · le magasin → das Geschäft · l'ami → der Freund
  acheter → kaufen · manger → essen · parler → sprechen
```

Zwölf Paare aus **zwei Fächern** (Englisch und Französisch), von denen dem Kind nichts zugewiesen sein muss.
Aufzählbar ist es, weil `Vocabulary.Id` eine fortlaufende Zahl ist — dasselbe Argument, mit dem B-80/E2 den
Übungs-Zweig geschlossen hat.

### 4 · Der Schreibpfad ist zusätzlich ein Aufzählungs-Orakel

Ebenfalls nachgespielt: eine unbekannte Id beantwortet der Endpunkt mit `400 invalid_reference` und **nennt
sie namentlich** (`Unknown vocabulary item IDs: 9999`), eine bekannte mit `200`. Damit liest ein Kind in
*einem* Aufruf ab, welche Ids existieren. Für Übungen hat B-80 genau das behoben, indem die Zuweisungsprüfung
**vor** die Existenzprüfung gestellt wurde („does not exist" und „not yours" müssen ununterscheidbar sein).
Heute ist das Orakel hier gar nicht nötig — das Kind darf ohnehin alles markieren —, aber es ist die Falle
beim Reparieren: die neue Prüfung muss vor der Existenzprüfung stehen, sonst tauscht der Fix ein Leck gegen
ein Orakel.

### 5 · Die Verbraucher, nachgezählt

Wie bei B-80 und B-82 hängt der Schnitt daran, und die Antwort ist noch schärfer als dort: **der Lesepfad hat
im ganzen Produkt keinen Verbraucher.**

| Wo | Befund |
|---|---|
| Frontend Vater | `api.ts:441-444` kennt **nur** `tagVocabulary` (POST) und `untagVocabulary` (DELETE); `VaterVocab.tsx:798,806` ruft beide. **Kein** `GET tags/{id}/vocabulary` |
| Frontend Sohn | kein Treffer |
| `Pugling.Client` | keine Methode (`CreatorApi.cs:172` hat nur den Übungs-Zweig `tags/{id}/exercises`) |
| Tests | `TagsRatingsTimetableTests.cs:41-55,73-75` fährt alle drei Aktionen durchweg mit dem **Vater**-Client; kein Kind-Token |

### 6 · Die Gegenproben — alle anderen Wege sind dicht

Ebenfalls am laufenden System, mit demselben Kind-Token:

| Weg | Erwartet | Gemessen |
|---|---|---|
| `GET creator/vocabulary` (Store direkt) | `403` | **403** — `[Authorize(Roles = Roles.Creator)]`, `VocabularyStoreController.cs:24` |
| `GET creator/vocabulary/tags` (kindneutrale Tags) | `403` | **403** — `VocabularyTagsController.cs:22` |
| `GET student/children/1/vocabulary-progress` (eigener Stand) | `200`, erlaubt | **200** |

Der Lernstand-Weg ist damit die aufgelöste Vermutung der Ideen-Fassung, und zwar **am Code**, nicht am
Statuscode: `ChildVocabularyProgressController` (`:22` nur `[Authorize]`) liest an allen drei Stellen
`db.ItemProgress` gefiltert auf `ChildId` (`:44,72,108`) — dort gibt es nur Zeilen für Items, die das Kind
**schon beantwortet** hat. `ItemProgressResponse.Back` und `WordMasteryResponse.Translation`
([ProgressDtos.cs:17,23](../../backend/Pugling.Contracts/Student/ProgressDtos.cs)) sind also der eigene Stand
über bereits gesehene Wörter, kein Leck. Dieselbe Feststellung hat der `pugling-reviewer` bei der Abnahme von
B-82 unabhängig für den Nachbarn `ChildLearnProgressService.cs:284` getroffen.

### 7 · Die Testlage — und ein Tor, das schon auf diese Story zeigt

Kein Test fährt einen dieser Endpunkte mit einem Kind-Token. Zusätzlich hat die Abnahme von B-82 den Befund
**mechanisch bestätigt**: das dort gebaute Lösungsfeld-Tor wurde probehalber auf `Translation`/`Back`/`Target`/
`Reveal` erweitert, und von elf Treffern war `TagsController.GetVocabulary` der **einzige echte Defekt** — die
übrigen zehn sind der Normalfall. Die Messung steht in
[ConventionGuardTests.cs:291](../../backend/Pugling.Api.Tests/ConventionGuardTests.cs) und nennt B-81
namentlich als den Ort, an dem die Namensliste erweitert wird.

## Die echte Lücke

Zwei Löcher, die zusammen wirken, und nur eines davon ist neu:

Das **Lese-DTO nennt das Geheimnis** (`Translation`) an einem Endpunkt, den ein Kind erreichen darf — das ist
die Familie von B-82, wo der Schnitt „Endpunkt gehört dem Erwachsenen" lautete. Und der **Schreibpfad hat
keine Reichweitengrenze** — das ist die Familie von B-80/E2, wo der Schnitt „ein Student markiert nur
Zugewiesenes" lautete. Das Besondere hier ist, dass **beide Schnitte zur Verfügung stehen** und sich
gegenseitig ersetzen könnten: gate man den Lesepfad, ist das Markieren fremder Vokabeln harmlos; beschränkt
man den Schreibpfad, ist das Lesen harmlos. Genau *das* ist die Entscheidung, die noch fehlt — nicht der
Befund.

Nicht durch B-80 gedeckt: dessen **E1** entfernt `Config` aus `ExerciseBrief` (anderer Typ), dessen **E2**
beschränkt den *Übungs*-Schreibpfad (andere Aktion, `:166`). Nicht durch B-82 gedeckt: dort wanderte ein
*anderer* Endpunkt in die Supervisor-Ebene, und das dort gebaute Tor greift `Answer`/`Solution`/
`CorrectAnswer` — nicht `Translation`.

## Offene Punkte

Alle fünf sind in der Runde vom 2026-08-03 erledigt — durchgestrichen statt gelöscht, damit die Frage
nachlesbar bleibt. Jede Empfehlung wurde übernommen; **eine Frage ist dabei weggefallen**, weil ein Fakt sie
entschieden hat (Punkt 2), und **eine ist dazugekommen** (E2, der Begriff „zugewiesene Vokabel").

1. ~~**Welcher Schnitt: Lesepfad gaten, Schreibpfad begrenzen — oder beides?**~~ → **E1**, beides.
2. ~~**Heben oder `Translation` aus dem DTO nehmen?**~~ → **entschieden durch einen Fakt statt durch eine
   Abwägung**: `Key` ist `{quelle}_{wort}_{ziel}_{übersetzung}` als Slug (`VocabKey.cs:21-27`), enthält die
   Übersetzung also ein zweites Mal. Feld-Entfernen schließt nichts, solange `Key` bleibt — und ohne `Key`
   bliebe vom DTO die `Id`. Also **heben** (E3).
3. ~~**Zieht das Ebenen-Präfix mit?**~~ → **E4**, nein.
4. ~~**Namensliste des Tors um `Translation` erweitern?**~~ → **E6**, ja; die Kosten sind für `Translation`
   **allein** neu gemessen (eine Ausnahme), nicht aus der Vier-Namen-Messung übernommen.
5. ~~**Bekommt das Aufzählungs-Orakel eine eigene Zeile?**~~ → **E5**, ja, als Reihenfolge-Anforderung.

Die ursprüngliche Fassung der fünf Fragen, mit den Empfehlungen, die in die Runde gingen:

1. **Welcher Schnitt: Lesepfad gaten, Schreibpfad begrenzen — oder beides?**
   *Empfehlung: **beides**, und zwar in dieser Reihenfolge der Begründung.* Der Schreibpfad ist die
   symmetrische Fortsetzung von B-80/E2 (`:166` steht schon da, es fehlt nur der Vokabel-Zweig) und schließt
   das Loch dort, wo es entsteht. Der Lesepfad ist mit dem Nachzählen aus Ist-Stand 5 **kostenlos** zu gaten,
   weil ihn kein Verbraucher aufruft — und ein zweites Schloss an einer Tür, die viermal aufgegangen ist, ist
   billiger als die Diskussion, welches genügt. Wer nur *einen* will: den Schreibpfad, weil er auch die
   Tag-Anzahl (`TagResponse.VocabularyCount`) und künftige Sichten mitschützt.
2. **Wenn der Lesepfad gegatet wird — auf `Roles.Supervisor` heben (wie B-82) oder `Translation` aus dem DTO
   nehmen (wie B-80/E1)?**
   *Empfehlung: **heben**, wie B-82/E1.* `TaggedVocabularyDto` hat genau einen denkbaren Nutzer, und das ist
   ein Erwachsener, der seine Markierungen prüft; ein Kind braucht diese Liste nicht (es hat
   `vocabulary-progress`). Das Feld zu entfernen machte das DTO für den Erwachsenen wertlos — dann bliebe nur
   `Key`, der die Übersetzung ohnehin enthält.
3. **Zieht das Ebenen-Präfix mit, wie bei B-82?**
   *Empfehlung: **nein**, und diesmal ist das keine Trägheit.* Ein Tag ist eine **wirklich** dual gelesene
   Ressource — Vater *und* Kind dürfen taggen, das hat B-80/E3 ausdrücklich entschieden. Der Controller bleibt
   also unter `creator/tags`, und nur diese *eine* Action trägt ein `[Authorize(Roles = …)]`. Das ist genau der
   Fall, für den der Kommentar in `ApiRoutes.cs:14-16` steht (die Klausur ist sein anderes Beispiel).
4. **Wird die Namensliste des Lösungsfeld-Tors um `Translation` erweitert?**
   *Empfehlung: **ja**, aber erst am Ende und mit gemessener Ausnahmeliste.* Die Erweiterung ist der
   mechanische Abschluss der ganzen Story-Reihe und in `ConventionGuardTests.cs:291` schon vorgesehen. Sie
   kostet gemessen **10 Ausnahmen**, wenn `Back`/`Target`/`Reveal` mitkommen — darum vermutlich nur
   `Translation` allein aufnehmen und die Kosten dafür getrennt messen (nicht die alte Messung übernehmen, sie
   galt für alle vier Namen zusammen).
5. **Bekommt das Aufzählungs-Orakel aus Ist-Stand 4 eine eigene Zeile in den Akzeptanzkriterien?**
   *Empfehlung: **ja**, als Reihenfolge-Anforderung.* Es ist heute kein eigener Defekt (das Kind darf alles
   markieren), wird aber zu einem, sobald Punkt 1 umgesetzt ist. Ohne die Zeile baut jemand die Prüfung
   hinter die Existenzprüfung und tauscht ein Leck gegen ein Orakel — B-80s Reviewer hat genau das gefunden.

## Entscheidungen

Aus der Grill-Runde vom 2026-08-03. Sechs Entscheidungen; **E2 ist im Gespräch entstanden** (der Begriff war
vorher nicht definiert), **E3 hat ein Fakt entschieden** statt eine Abwägung.

### E1 · Beide Pfade werden geschlossen, nicht einer

Der Schreibpfad begrenzt einen Studenten auf zugewiesenes Material (E2), **und** der Lesepfad wird
rollen-gegatet (E3).

*Begründung.* Jeder Pfad allein genügte theoretisch — gate man das Lesen, ist das Markieren fremder Vokabeln
harmlos; begrenzt man das Markieren, ist das Lesen harmlos. Beide zu nehmen ist trotzdem richtig, weil die
Einzel-Varianten je eine *stille* Abhängigkeit hätten: nur-Schreibpfad hängt daran, dass der Lesepfad nie mehr
zeigt als das Tag enthält — und der Vater darf fremdes Material ins Tag des Kindes legen (B-80/E3), das Kind
läse also genau das. Nur-Lesepfad hängt daran, dass niemand eine dritte Sicht auf `VocabularyTag` baut; schon
`TagResponse.VocabularyCount` ist eine.

*Kosten.* Zwei Änderungen statt einer, ein neuer Fehlercode (E5), zwei Regressionstests statt einem.

### E2 · „Dem Kind zugewiesene Vokabel" heißt: über `ExerciseItem` einer zugewiesenen Übung

Die Kette ist `Vocabulary ← ExerciseItem → Exercise → (PlanPosition | Klassenarbeit) des Kindes`.
Umgesetzt wird das als **ein Join vor** `AssignedExerciseIdsAsync`
([TagsController.cs:129](../../backend/Pugling.Api/Controllers/Creator/TagsController.cs)), nicht als zweiter
Helfer.

*Begründung.* Eine Vokabel ist einem Kind **nie direkt** zugewiesen — die Beziehung existiert im Modell nicht,
sie ist abgeleitet, und darum musste der Begriff überhaupt entschieden werden. Dieselbe Definition von
„zugewiesen" wie B-80/E2 zu benutzen ist der Kern: zwei Definitionen desselben Wortes laufen auseinander, und
dann gilt die veraltete (die Erfahrung dieses Repos mit `Father`/`Adult` und Lernziel/`KeyResult`). Die
Alternativen sind ausdrücklich verworfen: „zusätzlich alles schon Beantwortete" macht die erlaubte Menge zu
einer *wachsenden* Funktion von `ItemProgress`, „nur schon Beantwortetes" verhindert genau den sinnvollen Fall
(ein Kind merkt sich ein schwieriges Wort **vor** der Antwort vor) und wäre die zweite Definition.

*Kosten.* Eine **benannte Lücke**: Birkenbihls `DecodedWord.VocabularyId`
([ExerciseAuthoringDtos.cs:76](../../backend/Pugling.Contracts/Creator/ExerciseAuthoringDtos.cs)) verweist aus
der **Config** auf den Store, ohne `ExerciseItem`-Zeile — per SQL nicht joinbar. Ein Kind könnte ein
dekodiertes Wort nicht markieren. Inert, solange keine Sohn-Oberfläche Vokabeln markiert (heute keine, siehe
Ist-Stand 5); wird sie gebaut, ist das eine eigene Zeile, keine Änderung an dieser Regel.

### E3 · Das Lese-Gate ist ein **Attribut** mit beiden Erwachsenen-Rollen

`[Authorize(Roles = Roles.AnyAdult)]` an der `GetVocabulary`-Action, mit einer neuen Konstante
`AnyAdult = "Creator,Supervisor"` in [AuthAccess.cs](../../backend/Pugling.Api/Auth/AuthAccess.cs). `Translation`
bleibt unverändert im Vertrag.

*Begründung.* Zwei Fakten haben das entschieden, nicht eine Abwägung. **Erstens**: `Key` ist
`{quelle}_{wort}_{ziel}_{übersetzung}` als Slug (`VocabKey.cs:21-27`) — nur `Translation` zu entfernen
(B-80/E1-Stil) schließt nichts, und ohne `Key` bliebe vom DTO die `Id`. **Zweitens**: das in B-82 gebaute
Lösungsfeld-Tor liest **Attribute**, nicht Rumpf-Logik — eine Inline-Prüfung `if (User.IsStudent())` (das
Muster der Student-Endpunkte) machte es rot und kostete eine namentliche Ausnahme, also genau die Verrottung,
gegen die es gebaut wurde. Beide Erwachsenen-Arten behalten den Zugriff, weil der Controller unter `creator/`
liegt und bewusst *jeden* Erwachsenen taggen lässt; `Roles.Supervisor` allein hätte diese eine Action anders
verhalten lassen als ihre Nachbarn POST und DELETE.

*Kosten.* Die **erste mehrrollige Autorisierung im Repo** (50 Verwendungen, alle einrollig) — darum als
Konstante, nicht als Freitext-String an der Action.

### E4 · Kein Präfix-Umzug: das Tag bleibt eine dual gelesene Ressource

Der Controller bleibt unter `creator/tags`; nur die eine Action trägt das Rollen-Attribut.

*Begründung.* Das grenzt an B-82/E2, geht aber ausdrücklich anders aus, und der Unterschied ist der Grund:
Dort durfte nach dem Gaten **kein** Student die Route mehr aufrufen, das Präfix war also nachweislich falsch.
Hier bleiben nach E2 POST und DELETE kind-aufrufbar — B-80/E3 hat entschieden, dass das Kind selbst markieren
darf. Es wird also nicht die **Route** einrollig, sondern ein **Fenster** darin geschlossen; der Satz in
`ApiRoutes.cs:17-18` („Dualität ist keine Entschuldigung für ein rollen-gegatetes Fremdpräfix") greift nicht,
er redet über Routen, die nur eine Ebene noch aufrufen darf. Die Alternativen kosten mehr, als sie bringen:
nur die Lese-Action zu verschieben verteilt eine Ressource über zwei Präfixe, den ganzen Controller zu
verschieben wäre sachlich falsche Taxonomie (Tags markieren Katalog-*Inhalt*) und bräche vier Routen mit
echten Verbrauchern.

*Kosten.* Eine Action verhält sich anders als ihre unmittelbaren Nachbarn. Das braucht **an der Stelle** einen
Kommentar mit dem Grund, sonst liest es sich wie ein Versehen — und der nächste Umbau macht es „konsistent".

### E5 · Neuer Code `vocabulary_not_assigned`, und die Prüfung sitzt **vor** der Existenzprüfung

Additiv in [ApiErrors.cs](../../backend/Pugling.Api/Errors/ApiErrors.cs):
`vocabulary_not_assigned`, `403`, „Vocabulary is not assigned to this child." — parallel zum vorhandenen
`exercise_not_assigned` (`:117`).

*Begründung.* Der Code ist stabiler Vertragsbestandteil, also darf er nicht über seinen Gegenstand lügen:
`exercise_not_assigned` wiederzuverwenden benennt eine Übung, wo eine Vokabel gemeint ist, und beide auf ein
generisches `not_assigned` zusammenzuführen bräche den frisch aus B-80 verifizierten Code. Eine Oberfläche kann
die Fälle so unterscheiden („die Übung gehört dir nicht" vs. „dieses Wort kommt in deinem Material nicht vor").
**Die Reihenfolge** ist keine Stilfrage: steht die Existenzprüfung vorn, antwortet eine unbekannte Id `400` und
eine fremde `403` — per Binärsuche liest ein Kind daran ab, wo der Store endet (Ist-Stand 4, heute schon
messbar). Erwachsene behalten ihren `400`, sie durchlaufen die Zuweisungsprüfung nicht.

*Kosten.* Ein Registry-Eintrag. Und der `400`-Pfad ist für ein Kind danach unerreichbar — ein Test, der ihn
mit einem Kind-Token prüft, wäre falsch.

### E6 · Die Namensliste des Lösungsfeld-Tors bekommt `Translation`

In `ConventionGuardTests.SolutionPropertyNames`, als **letzter** Schritt des Baus.

*Begründung.* Erst das macht die Reihe B-75/B-77/B-80/B-82/B-81 mechanisch dicht: eine künftige Sicht, die eine
Übersetzung an ein Student-Token gibt, ist dann ein rotes Tor und keine sechste Story. **Für `Translation`
allein neu gemessen** statt die Vier-Namen-Zahl zu übernehmen: es gibt genau **zwei** Treffer — diesen Defekt
selbst und `ChildVocabularyProgressController.ByWord`. Nach der Reparatur bleibt also **eine** begründete
Ausnahme (`WordMasteryResponse.Translation` ist der eigene Stand des Kindes über schon beantwortete Wörter).
`Back`, `Target` und `Reveal` bleiben **draußen**: zusammen kosten sie gemessen 10 Ausnahmen, die den
Normalfall aufzählen — dasselbe Argument, das E3 in B-82 umgeworfen hat.

*Kosten.* Ein Ausnahme-Eintrag plus eine **neu gemessene** Untergrenze des Selbstschutzes. Und eine
Reihenfolge-Abhängigkeit im Bau: vor der Reparatur geschärft, ist die Suite zwischendurch rot.

### Zwei Anforderungen, die mitkommen, ohne eine Entscheidung zu sein

Beide sind Erträge aus B-80s Abnahme und gelten hier wörtlich:

1. **Geprüft wird nur, was wirklich hinzukommt.** Ein Auswahlformular schickt die volle Menge zurück, und im
   Tag des Kindes darf fremdes Material liegen, das der **Vater** hineingelegt hat — die ganze gesendete Menge
   zu prüfen gäbe dem Kind `403` für eine Nulloperation. Das Muster steht in `TagExercises` (`:151-156`).
2. **Der Löschweg bleibt unbeschränkt**, symmetrisch zu `UntagExercise` (`:190`): ein entferntes Häkchen
   verrät nichts.

## Akzeptanzkriterien

- Ein Kind-Token kann eine Store-Vokabel, die in **keiner ihm zugewiesenen Übung** vorkommt, **nicht**
  markieren; der Versuch endet mit `403 vocabulary_not_assigned`, und „existiert nicht" ist von „nicht
  zugewiesen" **nicht unterscheidbar** (kein Orakel — die unbekannte Id liefert denselben `403`).
- Ein Kind-Token darf eine Vokabel aus **zugewiesenem** Material weiter markieren, und ein erneutes Senden
  einer schon markierten Vokabel bleibt eine Nulloperation mit `200` — auch wenn der Vater sie dort
  hineingelegt hat.
- Ein Kind-Token liest über `GET tags/{tagId}/vocabulary` **keine** Übersetzung: der Endpunkt antwortet ihm
  mit `403`.
- Der Vater verliert nichts: er markiert, liest und entmarkiert weiter beliebiges Store-Material (der Fall aus
  `TagsRatingsTimetableTests.cs:41-55` bleibt unverändert grün), und der Vokabel-Editor in `VaterVocab.tsx`
  funktioniert unverändert. Ein **Lehrer-Konto** (Creator-only) darf den Lesepfad ebenfalls aufrufen.
- **Regressionstest, vorher rot**: der Durchgang aus Ist-Stand 3 mit einem Kind-Token — Tag anlegen, fremde
  Vokabel-Ids markieren, Liste lesen — endet nicht mehr mit zwölf Paaren, sondern schon am Markieren.
- Die Gegenproben aus Ist-Stand 6 bleiben, wie sie sind: Store und kindneutrale Tags `403`, der eigene
  `vocabulary-progress` `200`.
- Das Lösungsfeld-Tor ist mit `Translation` in der Namensliste **grün**, mit genau einer begründeten Ausnahme;
  seine Untergrenze ist neu gemessen.

## Schätzung

**S · backend · keine Migration · kein Vertragsbruch.**

**Größe S**, an den Ankern gemessen: der Umfang ist „`childId` aus dem Test-Pfad ziehen" (B-01, der S-Anker) —
ein Helfer mit *einem* Join, ein `if (User.IsStudent())`-Block als Zwilling des vorhandenen (`:166`), eine
Registry-Zeile, eine Rollen-Konstante, ein Attribut, ein Listeneintrag im Tor.

**Kein M**, und der Vergleich ist B-82: dort war es M **allein wegen des Tors**, weil ein *neuer Mechanismus*
dazukam (reflexiver Wächter samt Ausnahmeliste und Selbstschutz). Hier existiert der Mechanismus — E6 ist ein
Name in einer Liste, ein Ausnahme-Eintrag und eine neu gemessene Zahl. Kein neues Werkzeug, kein Löschverhalten,
kein bezahltes Inventar, keine Etappe eines Umbaus.

**Keine Migration**, nachgesehen: **kein Entity wird angefasst.** `ApiErrors` ist eine statische Klasse, `Roles`
sind Konstanten, E2 ist eine *Query* über die vorhandene Beziehung `ExerciseItem`
(`PuglingDbContext.cs:35` hat den `DbSet` schon). `SchemaGuardTests` hat nichts zu melden, die Kette bleibt
bei 1.

**Kein Vertragsbruch**, und das ist der Unterschied zu B-82: `Pugling.Contracts` ändert sich **nicht** —
`TaggedVocabularyDto` behält seine vier Felder (E3 hebt die Rolle, statt das Feld zu schneiden), die Route
bleibt liegen (E4). Was sich am **Vertragsdokument** ändert, ist additiv: ein Wert in der Fehlercode-Aufzählung
und eine `403`-Antwort. Weder `Pugling.Client` (kennt den Endpunkt nicht) noch das Frontend (kein Verbraucher
des Lesepfads) müssen nachziehen.

**`wo: backend`**, und das ist geprüft, nicht vermutet: **keine** Frontend-Quelle ändert sich. Der Lesepfad hat
keinen Aufrufer (Ist-Stand 5), und der Schreibpfad wird nur aus `VaterVocab.tsx:798` mit einem *Vater*-Token
gerufen — der durchläuft die neue Prüfung nicht. Anders als bei B-82, wo eine Routen-Zeichenkette in `api.ts`
zog. Reviewer: `pugling-reviewer` vollständig, **kein** `frontend-reviewer`.

### Risiken

**R1 · Die Untergrenze des Tors muss neu gemessen werden, nicht angepasst.** `Actions_Mit_Loesungsfeld_…`
trägt `inScope.Count >= 25`, gemessen am 2026-08-03 mit drei Namen (30 im Geltungsbereich). `Translation`
hinzuzunehmen hebt den Geltungsbereich, um wie viel ist **nicht** erhoben — nur die Zahl der *Offender* ist
gemessen (zwei, nach der Reparatur einer). Die Grenze wird also beim Bauen gelesen und knapp unter den Istwert
gesetzt; feste Untergrenzen verrotten ohnehin (B-40).

**R2 · Die Reihenfolge ist eine echte Abhängigkeit, kein Stil.** Wird das Tor vor der Reparatur geschärft, ist
die Suite zwischendurch rot — `TagsController.GetVocabulary` wäre dann Offender. Also: erst E2/E3/E5, dann E6.
Wer die Etappen anders schneidet, hält einen roten Zwischenstand für einen Fehler.

**R3 · Ein neuer Fehlercode zieht zwei eingecheckte Artefakte nach.** `ErrorCodeTests:153-169` ist ein
Drift-Wächter: die im OpenAPI dokumentierte Code-Aufzählung muss **exakt** `ApiErrors.AllCodes` sein (beide
Seiten reflexiv). Also ändert sich `docs/openapi/v1.json` (ein Enum-Wert plus die `403`-Antwort), und
`docs/api-examples/index.md` bekommt eine Zeile in der Liste „über HTTP im In-Process-Test nicht erreichbar"
samt neuem `Verifiziert: X / Y`-Zähler. Beides **mitcommitten**, sonst ist CI rot. Der Selbstschutz
`AllCodes.Count >= 40` ist nicht betroffen (die Zahl wächst).

**R4 · Die Idempotenz-Falle aus B-80s Abnahme wiederholt sich hier wörtlich.** Prüft der neue Block die *ganze*
gesendete Menge statt nur des Zuwachses, bekommt das Kind `403` für eine Nulloperation — denn im Tag des Kindes
darf fremdes Material liegen, das der **Vater** hineingelegt hat (B-80/E3), und ein Auswahlformular schickt die
volle Menge zurück. Der heutige `TagVocabulary` berechnet `already` erst **nach** der Existenzprüfung
(`:256`); die neue Prüfung braucht das `fresh` also **vor** sich, anders als der Code heute gebaut ist. Das ist
die einzige Stelle, an der E2/E5 den vorhandenen Ablauf umstellen statt zu ergänzen.

**R5 · `Roles.AnyAdult` ist die erste mehrrollige Autorisierung — nachgesehen, dass es niemanden stört.** Kein
Wächter außer dem neuen Tor liest `AuthorizeAttribute`, und dessen `HidesFromStudent` zerlegt den Wert an `,`
(`ConventionGuardTests.cs:322`), trifft also zu. Ein Vater trägt beide Ebenen-Claims, ein Lehrer-Konto nur
`Creator` — die OR-Semantik des Attributs lässt beide durch, ein Kind nicht.

**R6 · Was *nicht* betroffen ist**, damit niemand danach sucht: **kein E2E** (es ändert sich keine Oberfläche;
`frontend/e2e/` fährt die Tag-Vokabeln nicht). **`/smoke-test` prüft keine Tags** (kein Treffer in
`smoke-checks.sh`) — er ist hier also *kein* Beleg, der Beleg ist die Integrations-Suite plus ein
Live-Durchgang. `DocsCaptureTests` schneidet nur `tags/{id}/exercises` mit (`:782`), **nicht** den
Vokabel-Zweig — also keine neue Beispieldatei unter `docs/api-examples/`. `Pugling.Client` hat nichts
nachzuziehen, und der Endpunkt-Abdeckungs-Wächter sieht keinen neuen Endpunkt (die Actions behalten ihre Namen).

### Angriffsplan

Backend zuerst; es gibt kein Frontend zu ziehen. **Die Reihenfolge ist bindend** (R2).

1. **Fehlercode** (E5): `vocabulary_not_assigned` additiv in
   [ApiErrors.cs](../../backend/Pugling.Api/Errors/ApiErrors.cs) neben `exercise_not_assigned` (`:117`).
2. **Rollen-Konstante** (E3): `AnyAdult = Creator + "," + Supervisor` in
   [AuthAccess.cs](../../backend/Pugling.Api/Auth/AuthAccess.cs), mit einem Satz Begründung — sie ist die erste
   ihrer Art und muss erklären, warum nicht zwei Attribute.
3. **Lese-Gate** (E3/E4): `[Authorize(Roles = Roles.AnyAdult)]` an `GetVocabulary` (`:286`), dazu
   `[ProducesResponseType(403)]` und **der Kommentar**, warum diese eine Action anders ist als POST/DELETE
   daneben (E4s Kosten — ohne ihn macht der nächste Umbau sie „konsistent").
4. **Schreibpfad** (E2/E5), und hier wird umgestellt statt ergänzt (R4): `already`/`fresh` **vor** die
   Prüfungen ziehen, dann für `User.IsStudent()` die Zuweisungsprüfung über einen neuen Helfer
   `AssignedVocabularyIdsAsync(childId, fresh, ct)` — `db.ExerciseItems` auf `VocabularyId` gefiltert, join auf
   die Übungs-Ids aus `AssignedExerciseIdsAsync` (`:129`) —, **danach** die Existenzprüfung.
5. **Tests** (siehe Testweg).
6. **Tor** (E6): `Translation` in `SolutionPropertyNames`, der Ausnahme-Eintrag für
   `ChildVocabularyProgressController.ByWord` mit englischer Begründung, Untergrenze **messen** (R1).
7. **Artefakte**: `docs/openapi/v1.json` und `docs/api-examples/index.md` schreibt der Testlauf, mitcommitten
   (R3).

### Testweg

- **Regressionstest, vorher rot** — in `AntiCheatTests` (dort liegen die serverseitigen Zusicherungen, wie bei
  B-80 und B-82): Kind-Client (`TestApi.ChildAsync(factory)`) legt ein Tag an und markiert eine **nicht
  zugewiesene** Store-Vokabel → `403 vocabulary_not_assigned`. Heute liefert derselbe Aufruf `200`.
- **Zweiter Regressionstest**: dasselbe Kind liest `GET tags/{tagId}/vocabulary` → `403`. Mit
  **Gegenprobe Vater** auf derselben URL → `200` und die Übersetzung im Rumpf, sonst erfüllt ein Tippfehler im
  Pfad den Test (die Lehre aus B-82).
- **Kein Orakel** (E5): dasselbe Kind sendet eine **unbekannte** Id → ebenfalls `403 vocabulary_not_assigned`,
  ausdrücklich **nicht** `400 invalid_reference`. Das ist der Fall, der die Reihenfolge festnagelt.
- **Erlaubter Fall**: das Kind markiert eine Vokabel aus einer ihm über eine Plan-Position zugewiesenen
  Vokabelübung → `200`. Ohne diesen Fall beweist die Reparatur nur, dass sie *etwas* verbietet.
- **Idempotenz** (R4): der Vater legt eine fremde Vokabel ins Tag des Kindes, das Kind sendet die volle Menge
  erneut → `200`, keine Änderung. Genau der Fall, den B-80s Reviewer für Übungen gefunden hat.
- **`TagsRatingsTimetableTests.cs:41-55,73-75`** muss **unverändert** grün bleiben — der Nachweis „der Vater
  verliert nichts", inklusive des fremden Vaters mit `403`.
- **Ein Lehrer-Konto** (Creator-only, Seed „Herr Schmidt") auf dem Lesepfad: nicht `403` wegen der Rolle. Ob
  `404` (kein eigenes Tag) oder `200` (mit Betreuung) — der Fall belegt, dass E3 die richtige Rollenmenge
  gewählt hat und nicht `Roles.Supervisor`.
- **Das Tor gegen sich selbst prüfen** (E6): `[Authorize]` an `GetVocabulary` probehalber auf einen Wert *mit*
  Student setzen — der Wächter muss rot werden. Ein Tor, das nie rot gesehen wurde, ist unbelegt.
- **`ErrorCodeTests`** läuft mit und erzwingt den Gleichstand von `ApiErrors.AllCodes` und der OpenAPI-Enum
  (R3).
- **Live-Durchgang** gegen eine Wegwerf-DB auf `:5280`: der Ablauf aus Ist-Stand 3 endet jetzt am Markieren.
  **Nicht** `/smoke-test` — der fährt keine Tags (R6).

## Verlauf

- **2026-08-03** — angelegt beim Schätzen von B-80, als der Befund derselben Bauart am Nachbar-Endpunkt
  auffiel. Der Ist-Stand ist **am Code** belegt (`:209-211` prüft nur Existenz, `TagDtos.cs:21` trägt
  `Translation`), aber **nicht am laufenden System nachgespielt** — darum `unverifiziert: true`.
  `prio: P1` in Analogie zu B-80 vorgeschlagen (dieselbe Anti-Cheat-Zusicherung, ohne Zutun des Vaters
  ausnutzbar) — nicht vom Nutzer bestätigt. Bewusst **nicht** in B-80 eingefaltet: dessen Stufe `gegrillt`
  ist abgeschlossen und seine Akzeptanzkriterien sind final; eine sechste Entscheidung hätte sie wieder
  aufgemacht (dieselbe Handhabung wie B-76 → [B-79](B-79-position-stufe-unvalidiert.md)).
- **2026-08-03** — **ausformuliert**, `unverifiziert` ist weg: der Ist-Stand ist jetzt am Code belegt **und**
  am laufenden System nachgespielt. Der Durchgang ist kürzer als befürchtet — **zwei Aufrufe mit dem eigenen
  Tag des Kindes, ohne Zuweisung, zwölf Paare aus zwei Fächern**.
  Die Zeilennummern der Ideen-Fassung waren **verrottet** (`:209-211` und `:241-257` stimmen nicht mehr, die
  B-80-Reparatur hat den Controller verschoben) — nachgesehen statt übernommen: Schreibpfad `:245`,
  Existenzprüfung `:252-254`, Lesepfad `:286`, Projektion `:297`.
  Der **schärfste Fund war nicht der Defekt, sondern die Asymmetrie**: elf Zeilen über dem Vokabel-Zweig steht
  die Regel für Übungen schon da (`:166` `if (User.IsStudent())`, eingezogen von B-80/E2, mit derselben
  Begründung „Ids sind fortlaufende Zahlen"). Es fehlt buchstäblich der zweite Zweig. Das macht den Schnitt
  vorgezeichnet und die Story vermutlich klein.
  Ein **zweiter Befund kam beim Nachspielen dazu und stand in keiner Notiz**: der Schreibpfad ist ein
  Aufzählungs-Orakel — eine unbekannte Id wird namentlich zurückgemeldet (`400 invalid_reference:
  Unknown vocabulary item IDs: 9999`), eine bekannte mit `200`. Heute harmlos, weil das Kind ohnehin alles
  markieren darf; nach der Reparatur genau die Falle, die B-80s Reviewer für den Übungs-Zweig gefunden hat.
  Darum als eigene Reihenfolge-Anforderung in den Akzeptanzkriterien.
  **Zwei Vermutungen der Ideen-Fassung aufgelöst.** Der Lernstand-Weg ist in Ordnung, und zwar am Code
  (`ChildVocabularyProgressController` liest an allen drei Stellen `db.ItemProgress` auf `ChildId` gefiltert —
  nur schon beantwortete Items), nicht bloß am Statuscode. Und der Lesepfad hat **überhaupt keinen
  Verbraucher**: das Frontend kennt nur POST und DELETE, die Client-Bibliothek keine Methode, die Tests fahren
  ihn nur mit dem Vater. Das ist noch schärfer als bei B-80 und macht Empfehlung 2 (gaten statt Feld
  entfernen) billig.
  Rückenwind aus der B-82-Abnahme: das dort gebaute Lösungsfeld-Tor hat diesen Endpunkt bei einer Probemessung
  als **einzigen echten Defekt unter elf Treffern** ausgeworfen; die Messung steht in
  `ConventionGuardTests.cs:291` und nennt B-81 als den Ort für die Erweiterung der Namensliste.
  **Nächste Stufe `gegrillt`** — die fünf offenen Punkte liegen je mit Empfehlung vor, tragend ist Punkt 1
  (nur Schreibpfad, nur Lesepfad, oder beides).
- **2026-08-03** — gegrillt, **sechs** Entscheidungen. Tragend ist **E1**: es werden **beide** Pfade
  geschlossen, obwohl jeder allein genügte. Der Grund ist, dass die Einzel-Varianten je eine *stille*
  Abhängigkeit hätten — nur-Schreibpfad hängt daran, dass der Lesepfad nie mehr zeigt als das Tag enthält, und
  der Vater darf dort fremdes Material hineinlegen (B-80/E3); nur-Lesepfad hängt daran, dass niemand eine
  dritte Sicht auf `VocabularyTag` baut, und `TagResponse.VocabularyCount` ist schon eine.
  **E2 ist erst im Gespräch entstanden**, und das war der Wertbeitrag der Runde: „dem Kind zugewiesene
  Vokabel" war **kein definierter Begriff** — die Beziehung existiert im Modell nicht, sie ist abgeleitet über
  `Vocabulary ← ExerciseItem → Exercise → (PlanPosition | Klassenarbeit)`. Entschieden wurde die Fassung, die
  **dieselbe** Definition von „zugewiesen" benutzt wie B-80/E2 (ein Join vor `AssignedExerciseIdsAsync`), weil
  zwei Definitionen desselben Wortes auseinanderlaufen und dann die veraltete gilt. Verworfen: „zusätzlich alles
  schon Beantwortete" (macht die erlaubte Menge zu einer wachsenden Funktion von `ItemProgress`) und „nur schon
  Beantwortetes" (verhindert das Vormerken eines schwierigen Wortes *vor* der Antwort).
  **Zwei Entscheidungen hat ein Fakt getroffen, keine Abwägung.** `Key` ist
  `{quelle}_{wort}_{ziel}_{übersetzung}` als Slug (`VocabKey.cs:21-27`) — damit fiel „`Translation` aus dem DTO
  nehmen" (B-80/E1-Stil) als Option weg, es schließt nichts, solange `Key` bleibt. Und das in B-82 gebaute
  Lösungsfeld-Tor liest **Attribute**, nicht Rumpf-Logik — damit fiel die Inline-Prüfung weg, sie hätte das Tor
  rot gemacht und eine namentliche Ausnahme gekostet. Ergebnis **E3**: `[Authorize(Roles = Roles.AnyAdult)]`
  mit einer neuen Konstante `"Creator,Supervisor"`, die erste mehrrollige Autorisierung im Repo (50
  Verwendungen, alle einrollig).
  **E4 grenzt bewusst an B-82/E2 und geht anders aus**: dort wanderte das Präfix, weil nach dem Gaten **kein**
  Student die Route mehr aufrufen durfte. Hier bleiben POST und DELETE kind-aufrufbar, das Tag ist also eine
  *wirklich* dual gelesene Ressource — es wird nicht die Route einrollig, sondern ein Fenster darin
  geschlossen. Der Satz, den ich in B-82 in `ApiRoutes.cs` geschrieben habe, greift hier nicht; er redet über
  Routen, die nur eine Ebene noch aufruft. Kosten: eine Action verhält sich anders als ihre Nachbarn und
  braucht **dort** einen Kommentar, sonst macht der nächste Umbau sie „konsistent".
  **E5** nimmt einen eigenen Code `vocabulary_not_assigned` (403) statt `exercise_not_assigned`
  wiederzuverwenden — der `code` ist stabiler Vertragsbestandteil und darf nicht über seinen Gegenstand lügen —
  und schreibt die **Reihenfolge** fest: Zuweisungsprüfung vor Existenzprüfung, sonst bleibt das
  Aufzählungs-Orakel aus Ist-Stand 4.
  **E6** erweitert die Namensliste des Tors um `Translation`, und die Kosten sind für diesen einen Namen
  **neu gemessen** statt aus der Vier-Namen-Messung übernommen: genau zwei Treffer, nach der Reparatur bleibt
  **eine** Ausnahme. `Back`/`Target`/`Reveal` bleiben draußen (zusammen 10 Ausnahmen, die den Normalfall
  aufzählen).
  Zwei Dinge sind **keine** Entscheidung, sondern Erträge aus B-80s Abnahme, die wörtlich mitkommen: geprüft
  wird nur, was wirklich hinzukommt (sonst `403` für eine Nulloperation), und der Löschweg bleibt
  unbeschränkt — symmetrisch zu `UntagExercise` (`:190`), nachgesehen statt vermutet.
- **2026-08-03** — geschätzt: **S · backend · keine Migration · kein Vertragsbruch**. Damit ist es die
  **kleinste** Story dieser Reihe, und der Vergleich ist B-82: dort war es M *allein wegen des Tors*, weil ein
  neuer Mechanismus dazukam. Hier existiert er — E6 ist ein Name in einer Liste. Der Rest ist der S-Anker
  (B-01): ein Helfer mit einem Join, ein `if`-Block als Zwilling des vorhandenen, eine Registry-Zeile, eine
  Konstante, ein Attribut.
  **`wo: backend` ist geprüft, nicht vermutet**: keine Frontend-Quelle ändert sich. Der Lesepfad hat keinen
  Aufrufer, und der Schreibpfad wird nur mit einem *Vater*-Token gerufen — der durchläuft die neue Prüfung
  nicht. Also **kein** `frontend-reviewer`, anders als bei B-82.
  **Kein Vertragsbruch**, und das ist der zweite Unterschied zu B-82: `TaggedVocabularyDto` behält seine vier
  Felder (E3 hebt die Rolle statt das Feld zu schneiden) und die Route bleibt liegen (E4). Am Vertragsdokument
  ändert sich nur Additives.
  Die Schätzung hat **drei Dinge freigelegt, die im Grillen nicht sichtbar waren.** **R3**: ein neuer
  Fehlercode zieht zwei *eingecheckte* Artefakte nach — `ErrorCodeTests:153-169` erzwingt, dass die
  OpenAPI-Fehlercode-Aufzählung exakt `ApiErrors.AllCodes` ist, und `docs/api-examples/index.md` führt eine
  Liste der nie ausgelösten Codes samt Zähler. Nicht mitcommittet ⇒ CI rot. **R4**: die Idempotenz-Falle aus
  B-80s Abnahme wiederholt sich wörtlich, und schlimmer — der heutige `TagVocabulary` berechnet `already` erst
  **nach** der Existenzprüfung (`:256`), die neue Prüfung braucht `fresh` aber davor. Das ist die einzige
  Stelle, an der der Bau den vorhandenen Ablauf **umstellt** statt ihn zu ergänzen. **R1**: die Untergrenze des
  Tors ist für drei Namen gemessen (30); wie weit `Translation` den Geltungsbereich hebt, ist **nicht** erhoben
  — nur die Offender-Zahl ist es. Also beim Bauen lesen, nicht anpassen.
  Gegenprobe zum Umfang, damit niemand danach sucht: **kein E2E** (keine Oberfläche ändert sich),
  **`/smoke-test` ist hier kein Beleg** (er fährt keine Tags — nachgesehen in `smoke-checks.sh`),
  `DocsCaptureTests` schneidet nur den Übungs-Zweig mit (`:782`), `Pugling.Client` kennt den Endpunkt nicht,
  und der Abdeckungs-Wächter sieht keinen neuen Endpunkt. Der Beleg ist die Integrations-Suite plus ein
  Live-Durchgang.
- **2026-08-03** — **in Arbeit**: alle sechs Entscheidungen gebaut, in der bindenden Reihenfolge (E5 → E3 →
  E2 → Tests → E6). **670 Tests grün**, `dotnet format --verify-no-changes` sauber.
  **Der Live-Durchgang aus Ist-Stand 3 endet jetzt am Markieren.** Wörtlich derselbe Ablauf auf einer
  Wegwerf-DB (`:5280`, die echte `pugling.db` unangetastet): das Kind legt sein Tag an (`200`), sendet
  `[1..12]` → **`403 vocabulary_not_assigned`** mit allen zwölf Ids im `detail`, sendet `[9999]` →
  **derselbe** `403` mit derselben Satzform (kein Orakel), liest die Liste → `403`. Gegenprobe: der Vater
  markiert dieselben Ids in *diesem* Tag (`200`, `vocabularyCount: 3`) und liest sie mit Übersetzung
  (`200`) — und das Kind sendet danach die volle Menge erneut und bekommt `200`, keine Änderung. Das ist
  **R4 live**, nicht nur im Test.
  **R1 war berechtigt, und die Zahl war größer als vermutet**: `Translation` hebt den Geltungsbereich des
  Tors von 30 auf **42** Actions (die Schätzung hatte „nicht erhoben" stehen). Die Untergrenze ist darum
  **neu gemessen** und von 25 auf 38 gehoben — die alte hätte ein Drittel der Fläche wegfallen lassen und
  wäre grün geblieben. **E6s Vorhersage traf exakt**: genau zwei Treffer, nach der Reparatur bleibt **eine**
  Ausnahme (`ChildVocabularyProgressController.ByWord`).
  **Beide Tore wurden rot gesehen, bevor sie grün genannt wurden.** Die zwei neuen Tests ohne die Reparatur:
  genau zwei rot, neun grün. Und das Lösungsfeld-Tor mit `Student` probehalber im Rollenwert: rot, mit
  namentlicher Nennung von `TagsController.GetVocabulary`.
  **R3 traf ein wie beschrieben**: `docs/openapi/v1.json` (Enum-Wert + `403`-Antwort) und
  `docs/api-examples/index.md` (`34 / 56` → `34 / 57` plus die Zeile „nicht erreichbar") sind mitcommittet.
  Der neue Code steht dort in derselben Liste wie sein Zwilling `exercise_not_assigned` — nachgesehen, kein
  Ausrutscher. Zwei weitere `api-examples`-Dateien wurden nur in den Zeilenenden angefasst (CRLF) und
  zurückgesetzt; `git diff --ignore-cr-at-eol` war leer.
  **Zwei bewusste Abweichungen vom Plan.** Erstens: der Testweg nannte `AntiCheatTests` „wie bei B-80" — das
  war **falsch erinnert**, B-80s Fall liegt in `TagsRatingsTimetableTests` direkt neben den Übungs-Fällen.
  Die neuen Fälle liegen darum dort, denn die beiden Schreibpfade sind Zwillinge bis in die Reihenfolge ihrer
  Prüfungen, und wer den einen ändert muss den anderen sehen. Zweitens: E2 sagte „ein Join **vor**
  `AssignedExerciseIdsAsync`, nicht als zweiter Helfer", der Angriffsplan dagegen „ein neuer Helfer". Gebaut
  ist die Fassung, die E2s *Zweck* erfüllt: `AssignedVocabularyIdsAsync` **delegiert** an
  `AssignedExerciseIdsAsync` statt dessen Query zu wiederholen — es gibt weiterhin genau **eine** Definition
  von „zugewiesen".
  Offen für die Abnahme: `pugling-reviewer`.
- **2026-08-03** — **abgenommen**. `pugling-reviewer` gelaufen: **kein Blocker**, ein 🟡 und vier 🟢.
  Belege: **671 Tests grün**, `dotnet format --verify-no-changes` sauber, Live-Durchgang (siehe voriger
  Eintrag), Commits `1aaa8a3` (Bau) und der Abnahme-Commit dieser Zeile. Kein E2E und kein `/smoke-test` —
  das war R6 und bleibt richtig: keine Oberfläche ändert sich, und `smoke-checks.sh` fährt keine Tags.
  **Der 🟡 war eine echte Testlücke und ist behoben.** Klausur-Zweig und Zirkularität waren nur für den
  *Übungs*-Pfad abgedeckt; für Vokabeln hingen sie allein an der Delegation — und weil E2 wörtlich „ein Join
  **vor** `AssignedExerciseIdsAsync`" verlangte, hätte ein späterer Umbau in genau diese Richtung eine grüne
  Suite als Beleg gelesen, während der Rundweg für Wörter wieder offen ist. Neuer Test
  `Kind_MarkiertKlausurVokabel_AberNichtDieUeberEinenVerknuepftenTag`: die Vokabel einer *direkt*
  zugewiesenen Klausur-Übung ist markierbar (`200`), die einer nur **über einen verknüpften Tag** relevanten
  nicht (`403 vocabulary_not_assigned`). **Und er wurde rot gesehen**: die Zirkularität probehalber in
  `AssignedExerciseIdsAsync` eingebaut (tag-verknüpfte Klausur-Übungen mitgezählt) → **beide**
  Zirkularitäts-Tests rot, der Übungs- und der neue Vokabel-Fall. Er deckt also den Rundweg, nicht bloß
  „irgendetwas verboten".
  **Zwei 🟢 waren Belege, die ich nicht hatte — nachgesehen und übernommen.** Erstens: der „Known
  gap"-Kommentar nannte nur Birkenbihl, es gibt eine **zweite** Stelle derselben Bauart (Lückentext-Gaps
  verweisen über `Gap.VocabKey` aus der Config auf den Store, `ExerciseContentResolver.cs:113-127`) — beide
  sind jetzt genannt, beide sind fail-closed. Zweitens: `Roles.AnyAdult` ist **heute funktional gleich**
  `Roles.Creator`, weil jeder Adult-Account ein Creator-Profil bekommt und Supervisor nur *zusätzlich*
  (`AccountService.cs:46-47`) — nachgeprüft, stimmt. Der Wert bleibt trotzdem so, denn die ausgedrückte
  Regel ist „jeder Erwachsene"; der Konstante steht jetzt ein „nicht vereinfachen"-Satz mit diesem Grund
  bei, sonst kürzt der nächste Umbau sie auf `Creator` und ein Supervisor-only-Konto verliert am Tag seiner
  Entstehung lautlos den Zugriff.
  **Eine Reviewer-Empfehlung habe ich zur Hälfte anders umgesetzt, weil ihr naheliegender Fix schlimmer war
  als der Befund.** Die Begründung für die abweichende Rolle stand als `//` zwischen den `<param>`-Docs und
  den Attributen. Sie in das `<summary>` zu heben — der erste Versuch — schob fünf Zeilen interne Begründung
  als Endpunkt-Einzeiler ins **Vertragsdokument** (`docs/openapi/v1.json` zeigte es sofort). Sie sitzt jetzt
  als `//`-Block direkt über dem Attributblock, mit einem Satz dazu, *warum* sie nicht in der `<summary>`
  steht. Das OpenAPI-Dokument ist dadurch in dieser Runde unverändert.
  **Drei Befunde bleiben bewusst offen**, alle außerhalb des Schnitts und keiner ein Leck: (1)
  `docs/api-examples/index.md` behauptet für den neuen Code „über HTTP im In-Process-Test nicht erreichbar",
  was nachweislich falsch ist — es ist die **Generator-Vorgabe** für jeden von `DocsCaptureTests` nicht
  erfassten Code (`DocsCaptureTests.cs:1242`) und betrifft rund zwanzig Zeilen; die richtige Formulierung
  wäre „nicht von `DocsCaptureTests` erfasst". Das ist eine eigene Story, kein Nebenbei-Fix am Generator.
  (2) `vocabularyIds` ist unbegrenzt lang und ein Student kann eine sehr große `IN`-Liste erzwingen —
  vorbestehend und beim Übungs-Zwilling identisch. (3) `GET tags/{tagId}/exercises` bleibt kind-aufrufbar und
  zeigt Titel/Kapitel/Fach fremder Übungen, die der Vater ins Tag gelegt hat; seit B-80/E1 trägt
  `ExerciseBrief` aber kein `Config` und damit kein Geheimnis.
  **Was der Reviewer unabhängig bestätigt hat**, und das ist der eigentliche Abnahme-Beleg:
  `TaggedVocabularyDto` hat genau **einen** Produzenten (kein zweites Fenster), **kein**
  student-erreichbares DTO trägt einen Vokabel-`Key` (der die Übersetzung ja mitführt), ein Token kann nicht
  Student *und* Creator tragen (`AccountService.cs:46-47,63`), der `400`-Zweig ist für einen Studenten
  **prinzipiell** unerreichbar (was zugewiesen ist, existiert — `VocabularyId` ist ein FK), und `already`
  kann nur überspringen, nie einfügen. Dazu fünf Mutationen, von denen jede mindestens eine Assertion
  bricht.
