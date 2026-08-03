---
tags: [typ/story, status/ausformuliert, bereich/backend, rolle/student]
aliases: [Tag-Endpunkt gibt Lösungen preis, ConfigJson über Tags lesbar, Transkript erreichbar,
  ExerciseBrief traegt die rohe Config, Klausur gibt Loesungen preis]
status: ausformuliert
prio: P1
art: Defekt
quelle: B-75 (Review pugling-reviewer, Befund außerhalb des Diffs)
---

# B-80 · Über die Tags kann ein Kind jede Übungs-Konfiguration lesen

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

## Akzeptanzkriterien (Entwurf)

- Ein Kind-Token bekommt über **keinen** Endpunkt die Konfiguration einer Übung — geprüft an beiden Türen
  (Tag-Liste, Klausur-Detail/`practice`/`repeat`).
- Insbesondere ist das **Transkript** eines Hörverstehens für das Kind auf keinem Weg lesbar.
- Das Kind kann seine Übungen **weiterhin markieren** (Tag anlegen, taggen, Liste lesen) — die Reparatur
  nimmt ihm keine Funktion.
- Der Vater/Creator verliert nichts, was er heute nutzt.
- **Regressionstest, vorher rot**: ein Kind-Client taggt eine fremde Übung und liest die Liste; die Antwort
  enthält keine Lösung. Dazu derselbe Nachweis für die Klausur-Endpunkte.

## Offene Punkte

Jeder Punkt mit meiner Empfehlung — Material für die Grill-Runde.

1. **Wird `Config` aus `ExerciseBrief` ganz entfernt oder nur für Nicht-Creator geleert?**
   *Empfehlung: ganz entfernen.* Niemand liest es (Abschnitt 5), und ein Feld, das je nach Rolle etwas
   anderes enthält, ist die Sorte Vertrag, die man beim Lesen nicht mehr prüfen kann. Ein rollenabhängig
   geleertes Feld verlagert die Zusicherung außerdem wieder in jeden Endpunkt — genau die Verteilung, die
   diese Story beseitigen soll. Kosten: Vertragsbruch (additiv ist es nicht), Artefakt-Neubau, und wer die
   Konfiguration wirklich braucht, holt sie über das Typ-Detail.
2. **Braucht es daneben trotzdem eine Schranke im Tag-Schreibpfad?**
   *Empfehlung: ja, aber als eigene, kleinere Entscheidung.* Dass ein Kind **jede** Übung des Katalogs an
   seinen Tag hängen kann, ist auch ohne Lösungspreisgabe falsch: es leckt Titel, Fach und Kapitel fremder
   Übungen und lädt einen Datenmüll-Weg ein. Nur reicht es als Reparatur nicht (Tür B), und es darf dem
   Kind das Markieren des Zugewiesenen nicht nehmen.
3. **Bleibt es bei `403`/`404` oder wird das Feld still weggelassen?**
   *Empfehlung: still weglassen* — die Endpunkte sollen für das Kind ja weiter funktionieren. Eine
   Fehlerantwort wäre die falsche Sprache für „du darfst die Liste sehen, nur nicht ihren Inhalt".
4. **Ist der Titel noch richtig?**
   *Empfehlung: umbenennen*, etwa „Ein Listen-DTO gibt jedem Kind die Lösungen preis". „Über die Tags"
   benennt nach diesem Durchgang die **kleinere** Hälfte — Tür B braucht keinen Tag und keinen Trick. Wer
   nur den Index liest, hielte die Klausur für nicht betroffen. Dieselbe Erwägung wie B-77/E9; der alte
   Wortlaut bleibt als Alias.
5. **Zurückgestellt: gehört `[Authorize]` an `KlassenarbeitenController` überhaupt auf Klassenebene?**
   Das Muster „Klasse offen, Schreib-Actions gegated" funktioniert hier korrekt und ist bei den
   Student-Endpunkten ausdrücklich gewollt (Root-`CLAUDE.md`). Auffällig ist nur, dass der Controller unter
   dem *Supervisor*-Präfix liegt und trotzdem vom Kind gelesen wird. Das ist eine Taxonomie-Frage, nicht
   diese Lücke — *Empfehlung: hier nicht mitbehandeln*, sondern bei Bedarf als eigene Idee.

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
