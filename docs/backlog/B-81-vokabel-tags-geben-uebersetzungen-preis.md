---
tags: [typ/story, status/ausformuliert, bereich/backend, rolle/student]
aliases: [Vokabel-Tag gibt Übersetzungen preis, TaggedVocabularyDto trägt die Lösung,
  Kind liest jede Übersetzung des Stores, Tür D]
status: ausformuliert
prio: P1
art: Defekt
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

Fünf Fragen, jede mit Empfehlung. Die ersten drei sind die eigentliche Grill-Runde; 4 und 5 sind
Folgeentscheidungen.

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

## Akzeptanzkriterien (Entwurf)

- Ein Kind-Token kann eine Store-Vokabel, die in **keiner ihm zugewiesenen Übung** vorkommt, **nicht**
  markieren; der Versuch endet mit einem eigenen `ApiErrors`-Code, und „existiert nicht" ist von „nicht
  zugewiesen" **nicht unterscheidbar** (kein Orakel).
- Ein Kind-Token liest über `GET tags/{tagId}/vocabulary` **keine** Übersetzung — nach dem Gaten antwortet der
  Endpunkt ihm mit `403`.
- Der Vater verliert nichts: er markiert und entmarkiert weiter beliebiges Store-Material (der Fall aus
  `TagsRatingsTimetableTests.cs:41-55` bleibt unverändert grün), und der Vokabel-Editor in `VaterVocab.tsx`
  funktioniert unverändert.
- **Regressionstest, vorher rot**: der Durchgang aus Ist-Stand 3 mit einem Kind-Token — Tag anlegen, fremde
  Vokabel-Ids markieren, Liste lesen — endet nicht mehr mit zwölf Paaren.
- Die Gegenproben aus Ist-Stand 6 bleiben, wie sie sind: Store und kindneutrale Tags `403`, der eigene
  `vocabulary-progress` `200`.

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
