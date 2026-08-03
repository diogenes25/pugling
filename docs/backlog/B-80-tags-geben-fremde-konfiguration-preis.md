---
tags: [typ/story, status/in-arbeit, bereich/backend, rolle/student]
aliases: [Über die Tags kann ein Kind jede Übungs-Konfiguration lesen,
  Tag-Endpunkt gibt Lösungen preis, ConfigJson über Tags lesbar, Transkript erreichbar,
  ExerciseBrief traegt die rohe Config, Klausur gibt Loesungen preis]
status: in-arbeit
prio: P1
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: ja
quelle: B-75 (Review pugling-reviewer, Befund außerhalb des Diffs)
---

# B-80 · Das Kind kann die Lösungen jeder Übung lesen

## User Story

Als **Vater** möchte ich, dass die Lösungen, Alternativen und Transkripte einer Übung für mein Kind
unerreichbar bleiben — auch über die Nebenwege der API —, damit die Pflicht misst, was es **gelernt** hat,
und nicht, was es **gefunden** hat.

## Ist-Stand am Code

Der Befund der Idee hält. Er ist aber **zu klein geschnitten**: es gibt nicht einen Weg, sondern **zwei**,
und der zweite braucht keinen Trick.

Beide enden am selben Ort. `ExerciseBrief` führt die **rohe Konfiguration** als `JsonElement`
([ExerciseBrief.cs:12](../../backend/Pugling.Contracts/Creator/ExerciseBrief.cs)), gefüllt direkt aus
`Exercise.ConfigJson`
([ExerciseBriefMapping:27-28](../../backend/Pugling.Api/Controllers/Creator/ExerciseBrief.cs)) — ohne
Filter, ohne Rollenblick. Damit ist jedes Feld drin, das der Creator je eingetragen hat.

### 1 · Tür A: der Tag-Weg — er reicht an *jede* Übung des Katalogs

Drei für sich harmlose Entscheidungen:

1. `TagsController` trägt auf Klassenebene nur `[Authorize]`
   ([TagsController.cs:20](../../backend/Pugling.Api/Controllers/Creator/TagsController.cs)). Das ist
   **gewollt** und im Kommentar begründet (`:11-13`): das Kind darf seine Übungen selbst markieren.
2. Das Kind darf einen eigenen Tag anlegen (`:62-81`, Prüfung `:65` über
   [AuthAccess.cs:110](../../backend/Pugling.Api/Auth/AuthAccess.cs) — Student = eigenes Profil).
3. `POST tags/{tagId}/exercises` (`:124-141`) prüft von den Übungs-Ids **nur die Existenz** (`:131-133`).
   Kein Eigentum, kein `ExerciseGrant`, kein `ExecutePublic`.

`GET tags/{tagId}/exercises` (`:163-182`) antwortet dann mit `ExerciseBrief` — inklusive `Config`.

**Der Umfang ist der ganze Katalog**, nicht nur das Zugewiesene: die Ids sind fortlaufende Zahlen, und
`:131-133` fragt nichts weiter als „gibt es die?".

### 2 · Tür B: die Klausur — sie braucht keinen Trick

Das ist der Teil, den die Idee nicht gesehen hat. `KlassenarbeitenController` liegt unter dem
**Supervisor**-Präfix, trägt auf Klassenebene aber ebenfalls nur `[Authorize]`
([KlassenarbeitenController.cs:21](../../backend/Pugling.Api/Controllers/Supervisor/KlassenarbeitenController.cs));
das Eigentum läuft über `FindOwnedAsync` → `OwnsChildAsync` (`:40-45`), und das ist für das Kind **auf sein
eigenes Kind-Profil wahr**.

Drei **lesende** Endpunkte geben `ExerciseBrief` heraus:

| Endpunkt | Stelle | Antwort-DTO |
|---|---|---|
| `GET class-tests/{id}` | `:76-86` | `KlassenarbeitDetail` ([ClassTestDtos.cs:17](../../backend/Pugling.Contracts/Supervisor/ClassTestDtos.cs)) |
| `GET class-tests/{id}/practice` | `:260-262` | `PracticeResponse` (`ClassTestDtos.cs:35`) |
| `GET class-tests/repeat?childId=` | `:276-281` | `RepeatResponse` (`ClassTestDtos.cs:39`) |

**Die Schreibpfade sind dagegen sauber gegated** — `AssignExercises` (`:188`) und `UnassignExercise`
(`:205`) tragen je `[Authorize(Roles = Roles.Supervisor)]`. Ein Kind kann sich also *keine* fremde Übung in
die Klausur legen; es liest aber die Konfiguration von allem, was der **Vater** hineingelegt hat. Und das
ist genau der Stoff der nächsten Arbeit.

Damit ist die Empfehlung der Idee, notfalls „den Schreibpfad zu schließen", als **allein unzureichend
belegt**: Tür B hat keinen Schreibpfad, den man schließen könnte.

### 3 · Am laufenden System nachgespielt

Wie die Idee gefordert hat („erst nachspielen"), gegen eine **Wegwerf-DB** auf `localhost:5280`
(Seed-Familie, `pugling.db` unangetastet). Kind-Token aus `POST auth/child` (`childId: 1`), Claims
`role: Student`, `cid: 1`.

**Tür A**, mit einer Übung des geseedeten *Lehrers* (`authorAdultId: 2` — fremd für dieses Kind):

```text
POST creator/tags            {"childId":1,"name":"Spickzettel"}   → 201, createdBy: "Sohn"
POST creator/tags/3/exercises{"exerciseIds":[10,11,12]}           → 200, exerciseCount: 3
GET  creator/tags/3/exercises                                      → 200
```

Geliefert wurde unter anderem die vollständige Lösung einer fremden Lückentext-Übung:

```json
{"index":1,"answer":"used","alternatives":[]},
{"index":2,"answer":"would be","alternatives":["'d be"]}
```

**Die Kernbehauptung der Idee ist belegt.** Ein Hörverstehen ist im Seed nicht enthalten (`Seed.cs` kennt
keine `ListeningConfig`), also als Lehrer eines angelegt und als Kind getaggt:

```text
GET creator/tags/3/exercises  → "transcript":"GEHEIM: Tomorrow will be rainy in the north and sunny in the south."
```

Das ist wörtlich das Feld, das der Vertrag als „**for the creator only, never for the child (anti-cheat)**"
ausweist ([ExerciseConfigs.cs:105-106](../../backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs)) und
das [B-75](B-75-lese-hoerverstehen-ohne-inhalt.md) eigens von der Karte fernhält.

**Tür B**, ohne jeden Trick, nur mit dem Kind-Token:

```text
GET supervisor/class-tests?childId=1        → 200 (beide Klausuren des Kindes)
GET supervisor/class-tests/2               → 200, config: {"problems":[{"prompt":"7 × 6","answer":42,…}]}
GET supervisor/class-tests/1/practice      → 200, gaps: [{"index":1,"answer":"Hello","alternatives":["Hi"]…}]
GET supervisor/class-tests/repeat?childId=1→ 200, enthält config
```

### 4 · Die Gegenprobe: alles andere ist dicht

Wichtig für den Schnitt der Reparatur — die Auth-Wand hält überall sonst. Mit demselben Kind-Token:

| Aufruf | Antwort |
|---|---|
| `GET creator/exercises?take=5` | `403` |
| `GET creator/subjects/1/chapters/6/cloze/11` (Typ-Detail) | `403` |
| `GET creator/subjects` | `403` |
| `GET creator/exercises/11/usage` | `403` |
| `GET creator/tags?childId=2` (fremdes Kind) | `403` |
| `GET creator/tags/3/exercises` ohne Token | `401` |

Es sind also **genau zwei** Controller, die aus der Reihe fallen — und beide, weil sie `ExerciseBrief`
herausgeben, nicht weil ihre Ownership-Prüfung falsch wäre. Die Kind-Skopierung selbst ist korrekt.

### 5 · Niemand liest `Config` — das ist der billige Ausweg

Nachgesehen, nicht vermutet: **kein Verbraucher wertet das Feld aus.**

- **Frontend**: `tags/{tagId}/exercises` wird in `lib/api.ts` **gar nicht** aufgerufen; die Klausur-Ansicht
  nutzt nur `id`, `title`, `type`, `subjectName`
  ([VaterClassTests.tsx:205-209](../../frontend/src/vater/VaterClassTests.tsx)). Der Übungs-Editor liest
  `detail.config`, aber das ist die **Typ-Detail**-Antwort, ein anderes DTO.
- **Client-Bibliothek**: `CreatorApi.cs:172` schickt nur und bekommt `TagResponse` zurück.
- **Tests**: `CreatorAgentTests.cs:528`/`:556` und `KlassenarbeitenTests.cs:63` lesen ausschließlich `Id`.

`Config` an `ExerciseBrief` ist damit ein Feld, das **niemand braucht** und das an zwei Stellen zu viel
sagt. Nebenwirkung beim Entfernen: `DocsCaptureTests.cs:782` schneidet den Tag-Endpunkt mit, die Beispiele
unter `docs/api-examples/` werden also neu geschrieben.

### 6 · Die Testlage

Kein Test prüft, dass ein Kind eine fremde Konfiguration **nicht** lesen kann — weder für Tür A noch für
Tür B. `TagsRatingsTimetableTests.cs:19` fährt den Tag-Weg mit dem **Vater**-Client, also genau an der
Lücke vorbei; dieselbe Klasse „Regel getestet, Grenzfall offen" wie bei B-77
([docs/testplan.md](../testplan.md)).

## Die echte Lücke

Die Zusicherung „das Kind sieht die Lösung nur, wenn die Stufe sie zeigen darf" ist **an der Ausspielung**
gebaut — `CardFacets` verschweigt `reveal`, das Transkript bleibt beim Creator, B-75 und B-77 haben genau
dort gefeilt. Sie ist aber **keine Eigenschaft der Daten**: derselbe `Exercise` fließt über ein zweites,
viel älteres DTO ungefiltert nach draußen, sobald ein Endpunkt ihn „nur zur Übersicht" auflistet.

Deshalb ist es kein Ownership-Fehler (die Prüfungen sind richtig) und kein Rollenfehler (die beiden
`[Authorize]` sind Absicht). Es ist ein **Vertragsfehler**: ein Listen-DTO, das ein Geheimnis trägt. Solange
`Config` an `ExerciseBrief` steht, ist jeder künftige Endpunkt, der Übungen auflistet, ein neuer Kandidat —
die Lücke wächst also mit dem Produkt, ohne dass jemand einen Fehler macht.

## Entscheidungen

Aus der Grill-Runde vom 2026-08-03. Eine Entscheidung hat einen offenen Punkt **aufgelöst** statt ihn zu
beantworten (E1 → Punkt 3), und zwei eigene Empfehlungen der Ausformulierung sind dabei korrigiert worden
(E2 und E4).

### E1 · `Config` fällt aus `ExerciseBrief` ganz weg

Nicht rollenabhängig geleert, nicht durch einen Zwilling ersetzt: das Feld verschwindet aus dem Vertrag.

*Begründung.* Niemand liest es (Ist-Stand 5), und damit wird die Zusicherung eine Eigenschaft des **Typs**
statt einer Pflicht jedes Endpunkts — auch jeder Listen-Endpunkt, den es noch nicht gibt, ist danach sicher.
Genau das ist der Kern der Lücke: die Zusicherung hing an der Ausspielung, nicht an den Daten. Ein
rollenabhängig geleertes Feld hätte sie in jeden Endpunkt zurückverlagert und wäre durch Lesen des Vertrags
nicht mehr prüfbar („`JsonElement Config`" im Dokument, Inhalt abhängig vom Token). Der Zwilling war beim
Nachsehen gegenstandslos: es gibt **keinen** Creator-gegateten Verbraucher von `ExerciseBrief`, der Zwilling
hätte das Original also überall ersetzt und einen toten Typ hinterlassen.

*Kosten.* Vertragsbruch, nicht additiv — plus Artefakt-Neubau (`docs/openapi/v1.json`,
`openapi-examples.generated.json`, `docs/api-examples/`, `frontend/src/lib/contract.ts`). Wer die
Konfiguration wirklich braucht, holt sie über das Creator-gegatete Typ-Detail. **Beide Türen schließen sich
in einem Zug**, weil beide dasselbe DTO ausgeben.

### E2 · Ein Kind darf nur Zugewiesenes taggen

`POST tags/{tagId}/exercises` beschränkt für ein **Student**-Token auf Übungen, die dem Kind zugewiesen sind
(Plan-Position oder eigene Klausur). Der Erwachsene behält die volle Reichweite.

*Begründung.* Es ist, was der Controller ohnehin behauptet („Tags per child for marking catalog exercises",
`TagsController.cs:11-13`) — heute prüft der Pfad nur die Existenz der Ids. Und es **nimmt nichts weg**: das
Kind bekommt auf jeden Katalog-Weg `403` (Ist-Stand 4), kann von einer fremden Übung also gar nicht legitim
erfahren. Damit fällt zugleich die Metadaten-Reichweite (Titel, Kapitel, Fach) und der Datenmüll-Weg. Die
Ausformulierung hatte das als „eigene, kleinere Entscheidung" abgetun — das war zu vorsichtig.

*Verworfen:* der fertige Haken `perms.CanExecuteAsync`. Er gibt für jede `ExecutePublic`-Übung `true`
zurück, unabhängig vom Aufrufer ([ExercisePermissionService.cs:46](../../backend/Pugling.Api/Auth/ExercisePermissionService.cs)),
und `ExecutePublic` ist der Standard — er hätte also nur *zurückgezogenes* Material ausgesperrt. Dazu dehnte
er „ausführen/zuweisen" auf „markieren" aus, während Zurückziehen ausdrücklich nur neue **Zuweisungen**
stoppen soll.

*Kosten.* Eine Query plus ein Testfall. **Abgeleitet, nicht erfragt** (vorgelegt und unwidersprochen
geblieben): ein neuer additiver Fehlercode `exercise_not_assigned` (403) — das vorhandene `forbidden` wäre
vom Eigentumsfehler nicht unterscheidbar.

### E3 · Der Lesepfad bleibt ungefiltert

`GET tags/{tagId}/exercises` zeigt dem Kind weiter alles, was in seinem Tag steht.

*Begründung.* Nach E1 steckt dort kein Geheimnis mehr, und hineinlegen kann nur noch das Kind selbst
(Zugewiesenes, E2) oder der Vater. Einen Tag zu filtern hieße, dem Kind zu verbergen, was sein Vater ihm
ausdrücklich markiert hat — das ist der Zweck eines Tags. Es hätte außerdem eine stille Diskrepanz erzeugt:
`TagResponse.exerciseCount` zählt die Verknüpfungen, die Liste hätte weniger geliefert.

*Kosten.* Keine. Der Gewinn ist die Zahl der Stellen: die Zusicherung hängt an **zwei** (Typ ohne Config,
Schreibpfad) statt an jedem Endpunkt, der Übungen auflistet.

### E4 · Die Story heißt „Das Kind kann die Lösungen jeder Übung lesen"

Der alte Wortlaut bleibt als Alias.

*Begründung.* „Über die Tags" benennt nach diesem Durchgang die **kleinere** Hälfte — Tür B braucht keinen
Tag und keinen Trick; wer nur den Index liest, hielte die Klausur für nicht betroffen (dieselbe Erwägung wie
B-77/E9). Die eigene Empfehlung der Ausformulierung („Ein Listen-DTO gibt jedem Kind die Lösungen preis")
ist dabei **korrigiert**: sie benennt die Ursache, während die Titel dieses Repos durchweg den Produktfehler
aus Rollensicht benennen — und ein Titel, der ein DTO nennt, altert mit dem Typnamen.

*Kosten.* Eine Zeile im Index; die Verweise aus [B-75](B-75-lese-hoerverstehen-ohne-inhalt.md) tragen über
den Alias.

### E5 · Das Ebenen-Präfix bleibt, wie es ist (Nicht-Ziel)

Dass ein Kind `api/v1/supervisor/class-tests/…` liest, wird **nicht** geändert — weder hier noch als eigene
Story.

*Begründung.* Nach E1 hat es keine Sicherheitsfolge, und das Muster „Klasse offen, Schreib-Actions gegated"
funktioniert korrekt (die schreibenden Actions tragen `[Authorize(Roles = Roles.Supervisor)]`). Fachlich ist
die Klausur eine **geteilte** Ressource: der Vater plant sie, das Kind übt darauf. Als eigene Idee hätte der
Punkt in jeder künftigen Sichtung Aufmerksamkeit gekostet, ohne dass ein Nutzer etwas davon merkt.

*Kosten.* Ein dokumentierter Geruch statt einer offenen Frage: das Ebenen-Präfix ist laut Root-`CLAUDE.md`
die Taxonomie, und hier deckt es sich nicht mit dem Leser. Wer das später begradigen will, findet hier die
Begründung statt einer Leerstelle.

## Akzeptanzkriterien

- Ein Kind-Token bekommt über **keinen** Endpunkt die Konfiguration einer Übung — geprüft an beiden Türen
  (Tag-Liste, Klausur-Detail/`practice`/`repeat`).
- Insbesondere ist das **Transkript** eines Hörverstehens für das Kind auf keinem Weg lesbar.
- Das Kind kann die ihm **zugewiesenen** Übungen weiterhin markieren (Tag anlegen, taggen, Liste lesen) —
  die Reparatur nimmt ihm keine Funktion, die es heute sinnvoll nutzen kann.
- Eine **nicht** zugewiesene Übung kann ein Kind nicht mehr taggen (E2); der Erwachsene kann es weiterhin.
- Der Vater/Creator verliert nichts, was er heute nutzt.
- **Regressionstest, vorher rot**: ein Kind-Client taggt eine fremde Übung und liest die Liste; die Antwort
  enthält keine Lösung. Dazu derselbe Nachweis für die Klausur-Endpunkte.

## Schätzung

**S · backend · keine Migration · Vertragsbruch: ja.**

**Größe S**, am Anker gemessen: der Umfang ist „`childId` aus dem Test-Pfad ziehen" (B-01) — zwei
Vertragszeilen, ein Mapping, eine Query, ein Fehlercode, vier Testfälle. Kein `M`: es entsteht kein neuer
Pfad wie der vokabel-basierte Batch im `MediaSelector`, und die Artefakte schreiben sich im Testlauf selbst.
Kein `XS`: es ist ein Vertragsbruch mit Artefakt-Neubau, kein Textzusatz.

**Keine Migration**, und das ist nachgesehen: **kein Entity wird angefasst.** E1 entfernt ein Feld aus einem
Response-Record, E2 fügt eine Leseabfrage hinzu — `Exercise`, `Tag`, `ExerciseTag` und `PlanPosition` bleiben
unverändert, `SchemaGuardTests` hat also nichts zu melden und die Kette bleibt bei 1.

**Vertragsbruch: ja**, an einer Stelle, aber nicht additiv: `ExerciseBrief.Config` **verschwindet**. Der
Bruch ist billiger als er klingt, weil er nachgezählt ist — kein Verbraucher liest das Feld (Ist-Stand 5).
Betroffen sind vier Schemata im Dokument, weil drei DTOs das Record einbetten
(`KlassenarbeitDetail`, `PracticeResponse`, `RepeatResponse`). Der zweite Vertragsteil ist **additiv**: der
Code `exercise_not_assigned`.

**`wo: backend`** — und das ist die Prüfung, nicht die Vermutung: das Frontend liest `config` an dieser
Stelle nirgends (`VaterClassTests.tsx:205-209` nimmt `id`/`title`/`type`/`subjectName`), also ändert sich
**keine** Frontend-Quelle. `contract.ts` wird neu erzeugt und muss durch `tsc` — das ist Artefakt, nicht
Arbeit. Reviewer vor der Abnahme: `pugling-reviewer`.

### Risiken

**R1 · „Zugewiesen" darf nicht über die Tags definiert werden — sonst ist E2 zirkulär.**
`LoadRelevantExercisesAsync`
([KlassenarbeitenController.cs:320-326](../../backend/Pugling.Api/Controllers/Supervisor/KlassenarbeitenController.cs))
löst die Übungen einer Klausur aus **direkten** Zuweisungen **und** aus verknüpften Tags auf. Nimmt die
Prüfung aus E2 diese Menge, dann macht Markieren eine Übung zugewiesen, und zugewiesen macht sie markierbar
— die Schranke fällt in sich zusammen. Gemeint sind **Plan-Positionen des Kindes plus direkte
`KlassenarbeitExercise`-Zeilen**. Mechanisch klein, aber genau die Sorte Fehler, die grün testet, solange
niemand den Rundweg probiert: der Testfall muss ihn ausdrücklich fahren.

**R2 · Der Endpunkt ist zweitrollig, die Schranke gilt nur für eine.**
`POST tags/{tagId}/exercises` wird von **Vater und Kind** benutzt (`CurrentRole()`, `TagsController.cs:25`).
Eine Prüfung ohne `User.IsStudent()`-Zweig nähme dem Vater das Markieren fremden Materials — das er darf und
im Web auch tut. Umgekehrt ist ein Lehrer-Konto (Creator ohne Betreuungsauftrag) hier gar nicht
zugriffsberechtigt, weil `OwnsChildAsync` für ihn falsch ist; der Zweig ist also „Student vs. Supervisor",
nicht „Kind vs. jeder andere".

**R3 · Vier Schemata im Dokument, ein Record.** Weil `ExerciseBrief` in drei weiteren DTOs eingebettet ist,
schlägt E1 an vier Stellen in `docs/openapi/v1.json` durch. Der `ContractDocumentTests`-Wächter und der
`ClientRouteGuard` müssen mitziehen; der Client selbst ist nicht betroffen (`CreatorApi.cs:172` bekommt
`TagResponse`).

**R4 · `DocsCaptureTests` schreibt die Beispiele um.** `:782` schneidet den Tag-Endpunkt mit („Unbekannte
Übungen taggen", der Fehlerfall). Nach E2 kommt ein zweiter Fehlerfall dazu, und die Beispiele unter
`docs/api-examples/` ändern sich im Lauf — kein Fehler, aber ein Diff, den man erwarten muss statt ihn zu
untersuchen.

**R5 · Der Endpunkt-Abdeckungs-Wächter sieht keinen neuen Endpunkt**, also warnt er nicht. Es entsteht
keine neue Action; die Abdeckung wächst nur um Fälle an vorhandenen. Wer sich auf das Tor verlässt, bekommt
hier kein Signal.

### Angriffsplan

Backend zuerst; das Frontend hat an dieser Änderung nichts zu tun.

1. **Vertrag**: `JsonElement Config` aus `ExerciseBrief` entfernen
   ([Pugling.Contracts/Creator/ExerciseBrief.cs](../../backend/Pugling.Contracts/Creator/ExerciseBrief.cs)),
   `ExerciseBriefMapping.From` um das Argument kürzen (`:27-28`) — damit ist `System.Text.Json` dort nicht
   mehr nötig. Ein positionaler Record: das letzte Argument fällt weg, der Compiler zeigt jeden Aufrufer.
2. **Fehlercode** additiv in `ApiErrors`: `exercise_not_assigned` (403).
3. **Schranke** in `TagsController.TagExercises` (`:124-141`), **nur im Student-Zweig** (R2) und **ohne die
   tag-verknüpften Klausur-Übungen** (R1).
4. **Tests** (siehe Testweg), inklusive des Rundwegs aus R1.
5. **Artefakte**: `docs/openapi/v1.json`, `openapi-examples.generated.json` und `docs/api-examples/`
   schreiben die `DocsCaptureTests` im Lauf; `frontend/src/lib/contract.ts` über `npm run gen:contract`,
   danach `npm run build` als Gegenprobe (es darf sich **keine** Frontend-Quelle ändern müssen).

### Testweg

- **Regressionstest, vorher rot** — in `AntiCheatTests` (dort liegen die serverseitigen Zusicherungen):
  ein Kind-Client (`TestApi.ChildAsync(factory)`, existiert schon) taggt eine Übung und liest die Liste; die
  Antwort trägt **kein** `config`-Feld. Heute trägt sie es.
- **Beide Türen**, damit die Reparatur nicht halb belegt ist: derselbe Nachweis für
  `GET class-tests/{id}`, `…/{id}/practice` und `…/repeat` — die drei laufen über dasselbe DTO, aber ein
  Test, der nur den Tag-Weg prüft, würde einen Rückbau an den Klausur-Endpunkten nicht merken.
- **E2 in `TagsRatingsTimetableTests`** (dort liegen die vorhandenen Tag-Fälle): Kind taggt eine **nicht**
  zugewiesene Übung → `403 exercise_not_assigned`; Kind taggt seine **zugewiesene** → `200`; **Vater** taggt
  dieselbe fremde Übung → `200` (R2).
- **Der Rundweg aus R1**: eine Übung, die *nur* über einen tag-verknüpften Klausur-Eintrag „relevant" ist,
  gilt **nicht** als zugewiesen.
- **`/smoke-test`** plus der Live-Durchgang, der diese Story belegt hat (Rezept im Ist-Stand 3): Kind-Token,
  fremde Übung taggen, Liste lesen — und diesmal steht keine Lösung darin. Das Transkript ist der Fall, auf
  den es ankommt.
- **E2E**: nicht nötig. Es ändert sich keine Oberfläche; `full-flow.spec.ts` muss grün bleiben.

## Offene Punkte

Alle fünf sind in der Runde vom 2026-08-03 erledigt — durchgestrichen statt gelöscht, damit die Frage
nachlesbar bleibt.

1. ~~**Wird `Config` aus `ExerciseBrief` ganz entfernt oder nur für Nicht-Creator geleert?**~~ → **E1**
2. ~~**Braucht es daneben trotzdem eine Schranke im Tag-Schreibpfad?**~~ → **E2**, und die Empfehlung
   („eigene, kleinere Entscheidung") war zu vorsichtig: die Schranke gehört in diese Story.
3. ~~**Bleibt es bei `403`/`404` oder wird das Feld still weggelassen?**~~ → **aufgelöst durch E1**: es gibt
   kein Feld mehr zurückzuhalten. An seiner Stelle stand die Frage, ob auch der *Lesepfad* filtern muss →
   **E3**.
4. ~~**Ist der Titel noch richtig?**~~ → **E4**, mit korrigierter Empfehlung.
5. ~~**Gehört `[Authorize]` an `KlassenarbeitenController` auf Klassenebene?**~~ → **E5**, ausdrücklich als
   Nicht-Ziel festgehalten statt als eigene Idee abgelegt.

## Verlauf

- **2026-08-02** — angelegt aus dem `pugling-reviewer`-Befund zum Commit `dab72e3` (B-75). `prio: P1`
  vorgeschlagen, weil es eine Anti-Cheat-Zusicherung des Produkts betrifft und ohne Zutun des Vaters
  ausnutzbar ist — nicht vom Nutzer bestätigt.
- **2026-08-03** — ausformuliert und **am laufenden System nachgespielt** (Wegwerf-DB auf `:5280`, die echte
  `pugling.db` unangetastet). Der Befund hält, ist aber **zu klein geschnitten gewesen**: es gibt zwei
  Türen. Tür A (Tags) ist wie beschrieben und reicht an **jede** Übung des Katalogs, weil der Schreibpfad
  nur die Existenz der Ids prüft — belegt mit drei Übungen des geseedeten Lehrers, gelesen wurden
  Lückentext-Lösungen samt Alternativen. Die Kernbehauptung ist belegt: ein selbst angelegtes Hörverstehen
  gab sein `transcript` an das Kind heraus, wörtlich das Feld mit dem Kommentar „never for the child".
  **Neu und wichtiger: Tür B braucht keinen Trick.** `KlassenarbeitenController` trägt klassenweit nur
  `[Authorize]`, und seine drei *lesenden* Endpunkte (`{id}`, `{id}/practice`, `repeat`) geben
  `ExerciseBrief` heraus — das Kind liest also die Konfiguration jeder Übung, die der Vater in seine
  Klausur gelegt hat (`"answer":42`, `"answer":"Hello"`). Damit ist die Empfehlung der Idee, notfalls den
  Tag-Schreibpfad zu schließen, als allein unzureichend widerlegt: Tür B hat keinen.
  Die Gegenprobe zeigt, dass sonst alles dicht ist (Typ-Detail, Katalog-Listen, Fächer, fremdes Kind: je
  `403`) — es sind genau zwei Controller, und die Ownership-Prüfungen sind richtig. Der billige Ausweg ist
  nachgesehen statt vermutet: **niemand liest `ExerciseBrief.Config`** — nicht das Frontend (der Tag-Endpunkt
  wird gar nicht aufgerufen), nicht die Client-Bibliothek, nicht die Tests. Kein Test deckt die Lücke ab;
  der vorhandene Tag-Test fährt den Weg als Vater.
- **2026-08-03** — gegrillt, fünf Entscheidungen. Tragend ist **E1**: `Config` fällt aus `ExerciseBrief`
  **ganz** weg, statt rollenabhängig geleert zu werden — damit wird die Zusicherung eine Eigenschaft des
  *Typs*, und beide Türen schließen sich in einem Zug, auch für jeden Listen-Endpunkt, den es noch nicht
  gibt. Der in der Idee erwogene „Config-freie Zwilling" war beim Nachsehen gegenstandslos: es gibt keinen
  Creator-gegateten Verbraucher des DTOs, der Zwilling hätte das Original überall ersetzt.
  Die Runde hat zwei eigene Empfehlungen korrigiert. **E2**: die Schranke im Tag-Schreibpfad ist *nicht* eine
  spätere Kleinigkeit, sondern gehört hierher — ein Kind darf nur Zugewiesenes taggen, und das nimmt ihm
  nichts, weil es von fremden Übungen ohnehin nicht legitim erfahren kann (überall `403`). Dabei ist der
  fertige Haken `CanExecuteAsync` **verworfen**: er gibt für jede `ExecutePublic`-Übung `true` zurück, hätte
  also nur zurückgezogenes Material ausgesperrt, und hätte „zuweisen" auf „markieren" gedehnt. **E4**: der
  vorgeschlagene Titel benannte die Ursache statt des Produktfehlers — die Story heißt jetzt „Das Kind kann
  die Lösungen jeder Übung lesen", der alte Wortlaut bleibt Alias.
  **E3** hält den Lesepfad ungefiltert (einen Tag zu filtern verbärge dem Kind, was sein Vater ihm markiert
  hat — der Zweck eines Tags), **E5** hält das Ebenen-Präfix als ausdrückliches Nicht-Ziel fest, statt eine
  P3-Idee anzulegen, die in jeder Sichtung Aufmerksamkeit kostet. Ein offener Punkt wurde **aufgelöst statt
  beantwortet**: die Frage „`403` oder still weglassen" hat mit E1 keinen Gegenstand mehr.
  Abgeleitet und unwidersprochen: ein additiver Fehlercode `exercise_not_assigned` (403) für E2.
- **2026-08-03** — geschätzt: **S · backend · keine Migration · Vertragsbruch ja**. Keine Migration, weil
  **kein Entity angefasst wird** — E1 nimmt ein Feld aus einem Response-Record, E2 fügt eine Leseabfrage
  hinzu. Der Bruch ist nachgezählt statt befürchtet: kein Verbraucher liest `Config`, betroffen sind aber
  **vier** Schemata im Dokument, weil drei DTOs das Record einbetten (R3). `wo: backend` ist geprüft, nicht
  vermutet: es ändert sich **keine** Frontend-Quelle, `contract.ts` ist Artefakt.
  Die Schätzung hat zwei Dinge freigelegt, die im Grillen nicht sichtbar waren. **R1**: „zugewiesen" darf
  nicht über die tag-verknüpften Klausur-Übungen definiert werden, sonst ist E2 **zirkulär** — Markieren
  machte zugewiesen, zugewiesen machte markierbar, und die Schranke fiele in sich zusammen; gemeint sind
  Plan-Positionen plus *direkte* Klausur-Zuweisungen, und der Rundweg braucht einen eigenen Testfall.
  **R2**: der Endpunkt ist zweitrollig (Vater *und* Kind, `CurrentRole()`), die Schranke gilt also nur im
  Student-Zweig — ohne ihn nähme sie dem Vater das Markieren fremden Materials, das er darf und im Web tut.
  Beim Messen fiel ein Befund **außerhalb des Schnitts** auf: `POST tags/{tagId}/vocabulary` prüft ebenfalls
  nur die Existenz, und `GET tags/{tagId}/vocabulary` gibt `Word` **und** `Translation` heraus — die Antwort
  einer Vokabelkarte, die der Spielpfad auf getippten Stufen zurückhält. Von E1 (anderes DTO) und E2
  (anderer Endpunkt) **nicht** gedeckt. Bewusst nicht eingefaltet, weil `gegrillt` abgeschlossen und die
  Akzeptanzkriterien final sind: liegt als [B-81](B-81-vokabel-tags-geben-uebersetzungen-preis.md) auf
  `idee` (Handhabung wie B-76 → B-79).
- **2026-08-03** — gebaut, nach dem Angriffsplan und ohne Abweichung von den Entscheidungen. **E1**:
  `ExerciseBrief` trägt kein `Config` mehr, `ExerciseBriefMapping` mappt `ConfigJson` nicht mehr (das
  `using System.Text.Json` fällt in beiden Dateien weg). Beide Aufrufer waren die vorhergesagten zwei.
  **E2**: die Schranke sitzt in `TagsController.TagExercises`, hinter `User.IsStudent()` (R2) und über einen
  eigenen Helfer `AssignedExerciseIdsAsync`, der **Plan-Positionen plus direkte `KlassenarbeitExercise`-Zeilen**
  nimmt und die tag-verknüpften ausdrücklich **nicht** (R1, im Code begründet). Dazu der additive Code
  `exercise_not_assigned` (403).
  **Verifikation, belegt statt behauptet.** Drei neue Tests, und alle drei sind **vor der Reparatur rot
  gelaufen** (die drei Produktionsdateien dafür weggestasht): `AntiCheatTests` prüft beide Türen — die
  Tag-Liste und die drei Klausur-Endpunkte `{id}`/`practice`/`repeat` — gegen ein selbst angelegtes
  Hörverstehen; sie fiel mit `Assert.DoesNotContain() Sub-string found`, also **auf dem Transkript selbst**.
  In `TagsRatingsTimetableTests` liegen die drei E2-Fälle (nicht zugewiesen → `403 exercise_not_assigned`,
  zugewiesen → `200`, Vater auf derselben Route → `200`) und der **Rundweg aus R1**: eine Übung, die nur über
  einen verknüpften Tag „relevant" ist, gilt nicht als zugewiesen. Suite: **665 grün** (`dotnet test
  Pugling.sln -c Release`), `dotnet format Pugling.sln` ohne Änderung.
  Der Live-Durchgang gegen eine Wegwerf-DB auf `:5280` spielt genau die Aufrufe nach, die die Story belegt
  haben: das Kind bekommt beim Taggen fremder Übungen jetzt `403 exercise_not_assigned`, der Vater darf
  weiterhin, und alle vier Lesewege liefern Briefs ohne `config` und ohne Transkript. Die Standard-Smoke-Checks
  sind grün. Artefakte wie vorhergesagt: `docs/openapi/v1.json` verliert `config` an **einem** Schema (die
  drei einbettenden DTOs referenzieren es, R3 überschätzte den Diff also), gewinnt die `403`-Antwort am
  Tag-Endpunkt und den neuen Code in der `enum`; `docs/api-examples/index.md` zählt jetzt 56 Codes.
  `openapi-examples.generated.json` blieb unverändert. Das Frontend musste **keine** Quelle ändern:
  `npm run build` (mit neu erzeugter `contract.ts`) läuft durch — `wo: backend` hat gehalten.
  **Offen für `abgenommen`**: der `pugling-reviewer`.
