---
tags: [typ/story, status/in-arbeit, bereich/backend, bereich/frontend, rolle/student]
aliases: [Lückentext ohne Lücke, Welche Lücke ist gemeint, Wortbank kommt nie an]
status: in-arbeit
prio: P1
art: Defekt
groesse: M
wo: beides
migration: nein
vertragsbruch: nein
quelle: B-75 (Grill-Runde, Entscheidung 2)
---

# B-76 · Der Lückentext sagt dem Kind nicht, welche Lücke gemeint ist

## User Story

Als **Kind** möchte ich bei einem Lückentext sehen, **welche** Lücke gerade gefragt ist — und die
Wortbank bekommen, die die Stufe verspricht —, damit ich die Aufgabe lösen kann, statt zwischen zwei
gleich aussehenden Karten zu raten.

## Ist-Stand am Code

Angelegt wurde exakt die geseedete Übung ([Seed.cs:1022-1038](../../backend/Pugling.Api/Data/Seed.cs)),
gespielt als Kind über `/practice-sessions/{id}/cards`:

```json
--- TranslationWordBank (die geseedete Stufe) ---
[{"itemIndex":0,"prompt":"A: {{1}}, how are you? B: I'm {{2}}, thank you.","choices":null,"reveal":"Hello"},
 {"itemIndex":1,"prompt":"A: {{1}}, how are you? B: I'm {{2}}, thank you.","choices":null,"reveal":"fine"}]

--- FreeText ---
[{"itemIndex":0,"prompt":"A: {{1}}, how are you? B: I'm {{2}}, thank you.","choices":null,"reveal":null},
 {"itemIndex":1,"prompt":"A: {{1}}, how are you? B: I'm {{2}}, thank you.","choices":null,"reveal":null}]
```

Drei Befunde stecken darin:

1. **Die Lücke ist nicht adressiert.** `ClozeExerciseType.ItemsOf` setzt `GapIndex`
   ([BuiltInExerciseTypes.cs:118](../../backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs), ebenso
   [ExerciseContentResolver.cs:123](../../backend/Pugling.Api/Services/Shared/ExerciseContentResolver.cs)),
   aber das Feld erreicht **nur** `PreviewItem`, den Testmodus des Vaters
   ([ExercisePreviewDtos.cs:10](../../backend/Pugling.Contracts/Creator/ExercisePreviewDtos.cs)).
   `PracticeCard` ([PracticeDtos.cs:27-29](../../backend/Pugling.Contracts/Student/PracticeDtos.cs)) und
   `TestItem` ([TestDtos.cs:11-12](../../backend/Pugling.Contracts/Student/TestDtos.cs)) führen es nicht.
2. **Die Wortbank kommt nie an.** `ClozeConfig.WordBank` ist gefüllt, das Manifest führt `wordBank` als
   Fähigkeit (`BuiltInExerciseTypes.cs:112`) — `choices` ist trotzdem `null`. Der Grund:
   `IExerciseType.Choices` ist genau dafür da, wird aber **nur** von `VocabularyExerciseType`
   überschrieben ([:54](../../backend/Pugling.Api/Exercises/VocabularyExerciseType.cs)); alle anderen
   Typen erben die Vorgabe `null` ([ExerciseTypeBase.cs:38](../../backend/Pugling.Api/Exercises/ExerciseTypeBase.cs)).
   Die Stufe heißt „Wortbank" (`ClozeEntities.cs:14`) und liefert keine.
3. **Die Vorlagensyntax ist sichtbar.** `SohnPractice.tsx:228` rendert `card.prompt` roh; im Frontend gibt
   es keine Behandlung von `{{n}}`.

Auf der geseedeten Stufe ist das Kind nicht blockiert — sie ist nicht getippt, also liefert die Karte die
Lösung (`reveal`), und es bleibt eine Umdreh-Karte. Auf `FreeText` — ebenfalls geseedet
(`Seed.cs:357-365`) — ist es echtes Raten.

### Es ist nicht der Lückentext allein

Beim Ausformulieren war offen, ob weitere Typen dieselbe Lücke haben. Sie haben. Am laufenden System
gemessen, gleiche Methode:

| Typ | Was das Kind bekommt | Bewertung |
|---|---|---|
| `List` | **Alle** Karten tragen denselben `prompt` (die Anweisung der Übung), z. B. dreimal „Nenne die Bundeslaender." — und ohne Anweisung ist er `""`. `ContentItem(i, c.Instruction ?? "", …)` (`BuiltInExerciseTypes.cs:269`) | Derselbe Defekt, **reiner**: die geseedete Liste hat 16 Einträge, also 16 gleiche Karten |
| `Matching` | `Direct` und `Distractors` liefern **identische** Karten, `choices: null` in beiden (`:159`) | Die Stufe `Distractors` (`StudyPlanEntities.cs:26`) liefert keine Ablenker |
| `Grammar` | Die übergreifende `Instruction` („Setze das Verb ins Simple Past.") wird verworfen, nur der Einzel-Prompt kommt an (`:64`) | Milder: der Einzel-Prompt trägt für sich |
| `Translation`, `Arithmetic` | Prompt ist die Aufgabe selbst | Unauffällig |

Nicht am laufenden System, sondern nur am Code geprüft: **`Birkenbihl`** baut
`ContentItem(i, s.LearningSentence, s.NaturalTranslation, …)` (`:99`) — die `Decoding`, die
Wort-für-Wort-Entschlüsselung, wird verworfen (`ExerciseConfigs.cs:249`). Das ist der ganze Zweck der
Methode; übrig bleibt eine gewöhnliche Übersetzungskarte. Sollte vor dem Grillen nachgespielt werden.

`List` und `Matching` liegen wie der Lückentext als **Positionen im Seed** (`Seed.cs:369-385`, `:401-402`).

## Die echte Lücke

Es ist ein einziger Konstruktionsfehler mit zwei Gesichtern, und beide sitzen in derselben Naht
(`PositionPlayService.CardFacets` → `PracticeCard`/`TestItem`):

1. **Übungsweiter Inhalt hat keinen Platz.** Text, Anweisung, Wortbank gehören der *Übung*, nicht dem
   Inhalts-Atom. Wer sie trotzdem ausliefern will, hat heute nur den `Prompt` — also wird kopiert
   (Lückentext, Liste) oder weggeworfen (Grammatik, Birkenbihl). Kopieren macht die Karten
   ununterscheidbar, Wegwerfen nimmt dem Kind die Aufgabe.
2. **Das Atom kann sich nicht ausweisen.** `GapIndex` existiert, kommt aber nur beim Vater an. Ohne ein
   „welches von diesen" ist eine Karte, deren Prompt sie mit anderen teilt, nicht beantwortbar — egal wie
   gut der Prompt ist.

Der zweite Punkt ist der eigentliche: Selbst wenn der Text sauber übertragen wird, bleiben zwei Lücken
zwei gleiche Karten. **`Choices`** ist dabei kein dritter Fehler, sondern eine ungenutzte Vorkehrung —
der Haken ist da, nur hängt außer dem Vokabel-Typ niemand etwas hinein.

Beachtenswert für den Zuschnitt: [B-75](B-75-lese-hoerverstehen-ohne-inhalt.md) hat für Punkt 1 bereits
entschieden (E1: additives Feld auf der Karte). Punkt 2 ist neu und gehört hierher.

## Offene Punkte

1. ~~**Wie weist sich das Atom aus — Nummer oder aufgelöste Vorlage?**~~ → **E2**
2. ~~**Ein Feld für alle oder je Typ eines?**~~ — durch **E1** gegenstandslos: Der Zuschnitt trennt nach
   Defekt, also gibt es keine drei Typen mehr, die sich ein Feld teilen müssten. `GapIndex` heißt, was es
   ist. Die i18n-Frage („fertiger Text vom Server") stellt sich damit gar nicht.
3. ~~**Wächst diese Story auf Liste, Zuordnung, Grammatik und Birkenbihl?**~~ → **E1** — und die
   Empfehlung, die hier stand, war **falsch**. Sie ist mit der Begründung korrigiert stehen geblieben.
4. ~~**Bekommt der Lückentext seine Wortbank über `Choices`?**~~ → **E4**
5. ~~**Gilt dasselbe für die Klausur?**~~ → **E5**
6. ~~**Was passiert mit der rohen Vorlagensyntax?**~~ → durch **E2** miterledigt: Wer die gefragte Lücke
   hervorhebt, muss `{{n}}` ohnehin erkennen und ersetzen.

## Entscheidungen

Aus der Grill-Runde vom 2026-08-02. Die Runde hat vor allem die Ausformulierung korrigiert: Deren
Empfehlung, die Liste mitzunehmen, hielt der Prüfung nicht stand (siehe E1).

### E1 · Nach Defekt trennen, nicht nach Naht

B-76 bleibt beim Lückentext. Die drei Nachbarbefunde gehen dorthin, wo ihr Fehler hingehört:

| Befund | Wohin | Weil |
|---|---|---|
| Liste | **[B-77](B-77-liste-menge-als-folge.md)** (neu) | Der Übungs-Pfad bewertet eine **ungeordnete Menge als Folge** |
| Zuordnung | **[B-73](B-73-auswahl-feld-ohne-wirkung.md)** | Stufe verspricht eine Auswahl, liefert Freitext — genau deren Muster |
| Grammatik | **[B-75](B-75-lese-hoerverstehen-ohne-inhalt.md)** | Übungsweiter Text fällt weg — von E1 dort abgedeckt |
| Birkenbihl | **[B-78](B-78-birkenbihl-dekodierung-erreicht-kind-nicht.md)** (neu) | Die Dekodierung ist **strukturiert**, kein Text — passt in keine der beiden Formen |

*Begründung.* Die Ausformulierung empfahl, Liste und Zuordnung mitzunehmen, weil sie „derselbe Defekt an
derselben Naht" seien. Beim Grillen zeigte sich: Das stimmt für die Naht und nicht für den Defekt.
`ListConfig` hat ein `Ordered`-Feld, und `ListExerciseType.Check` bewertet eine ungeordnete Liste als
**Menge** (`BuiltInExerciseTypes.cs:288-295`) — der Katalog-Check kann es also längst richtig. Nur der
Übungs-Pfad baut je Eintrag eine Karte, die genau *ihren* Eintrag verlangt. Bei den geseedeten „16
Bundesländern" (ohne `Ordered`) ist die Karte damit prinzipiell unlösbar, und ein Etikett „3 von 16"
würde daran **nichts** ändern. Das ist ein anderer Fehler mit einer anderen Reparatur.

*Kosten.* Vier Stories statt einer, zwei fremde angefasst. Dafür trägt jede genau einen Fehler, und alle
erweitern dieselbe Naht (`CardFacets` → `PracticeCard`/`TestItem`) additiv, ohne sich zu widersprechen.

### E2 · Die Karte trägt die Lückennummer, das Frontend stellt sie dar

`GapIndex` kommt additiv auf `PracticeCard` und `TestItem`. Das Frontend hebt den gefragten Platzhalter
hervor und stellt die übrigen neutral dar.

*Begründung.* Die Gegenvariante — der Server liefert den Text aufgelöst — hat drei Ausprägungen und jede
einen Haken: gelöste Nachbarlücken verraten die nächste Karte; stehengelassene `{{2}}` lösen nichts;
neutrale Striche machen die Karte wieder mehrdeutig. Die Nummer lässt den Server aus der Darstellung
heraus, und `GapIndex` existiert bereits am `ContentItem` — es fehlt nur der letzte Meter.

*Kosten.* Zwei Vertragsfelder (additiv) und ein `{{n}}`-Renderer in der Sohn-Ansicht. Der Parser dafür
ist schon da: `placeholderIndices` in [ClozeTexts.tsx:50](../../frontend/src/vater/ClozeTexts.tsx)
validiert heute den Editor und gehört dann nach `lib/`.

### E3 · „Trägertext" bleibt dem Lückentext-Store vorbehalten

Das Wort bezeichnet weiterhin **nur** den Store-Eintrag (`ClozeText`,
[ClozeEntities.cs:22](../../backend/Pugling.Api/Models/ClozeEntities.cs), Endpunkt
`api/v1/creator/cloze-texts`). Der Text einer Leseverstehen-Übung heißt `Passage`, in der Oberfläche
schlicht „Text"; B-75 wird entsprechend korrigiert.

*Begründung.* „Trägertext" ist im Frontend durchgehend besetzt (`lib/types.ts:146-156`, `lib/api.ts:281`,
der Editor selbst) und hat eine Entität mit Titel, Key und Sprache hinter sich — eine wiederverwendbare
Lerngrundlage. Der Lesetext hat nichts davon: kein Store, keine Entität, er gehört genau einer Übung. Zwei
Dinge, zwei Wörter. In diesem Repo hat Begriffsdrift zweimal teuer bezahlt (`Father`→`Adult`,
Lernziel→`KeyResult`), und beide Male begann sie mit einem Wort, das zwei Sachen meinte.

*Kosten.* Eine dritte fremde Story anfassen — B-75s Prosa benutzt das Wort an mehreren Stellen falsch.

### E4 · `Choices` liefert die ganze Wortbank, unverändert

Auf der Wortbank-Stufe bekommt jede Karte den vollen Pool aus `ClozeConfig.WordBank`; auf getippten
Stufen keinen.

*Begründung.* Ein Pool, aus dem Verbrauchtes verschwindet, braucht Sitzungszustand **und** verrät: Bei
zwei Lücken und zwei Wörtern wäre die zweite Karte geschenkt. Die Vokabel-Variante („Lösung plus drei
Ablenker") beschneidet still, was der Creator bewusst gepflegt hat — und stolperte hier zusätzlich über
B-65: „Hi" ist eine *gleichwertige* Antwort auf Lücke 1 und dürfte nie als Ablenker gelten.

*Kosten.* Bei einer großen Wortbank viele Knöpfe. Das ist dann eine Entscheidung des Autors, nicht der
Engine — und sichtbar, statt still korrigiert.

### E5 · Die Klausur zieht mit

`TestItem` bekommt `GapIndex` genauso wie die Übungskarte. **Abgeleitet, nicht erfragt** — mit dem
Hinweis versehen und unwidersprochen geblieben.

*Begründung.* Die geseedete Freitext-Position trägt `RequireTypedTest = true`
([Seed.cs:357-365](../../backend/Pugling.Api/Data/Seed.cs)); die Klausur ist also genau der Ort, an dem
das Kind heute feststeckt. Die Übungsrunde zu reparieren und die Prüfung kaputt zu lassen, hieße die
unwichtigere Hälfte zu reparieren.

*Kosten.* `TestItem` bekommt ein Feld zurück, kurz nachdem [B-01](B-01-bildwahl-einfrieren.md) zwei
entfernt hat. Kein Widerspruch: jene waren **immer** `null`, dieses ist es nur bei Typen ohne Lücken.

### E6 · Die Wortbank-Stufe wird getippt (Auflösung von R1)

`StageMechanics.IsTyped(ClozeStage)` nimmt `TranslationWordBank` auf. Damit verschwindet dort die Lösung
aus `Reveal`, die Auswahl erscheint (beide Sohn-Ansichten rendern sie im `typed`-Zweig), und der Server
bewertet die Antwort.

*Begründung.* E4 setzte eine Stufe voraus, auf der eine Auswahl ankommt — die gab es nicht. Vom Nutzer
entschieden, am 2026-08-02, nachdem die Schätzung die Lücke als R1 sichtbar gemacht hatte. Fachlich ist es
dieselbe Begründung wie beim Multiple-Choice der Vokabel: Auswählen **ist** eine prüfbare Antwort.

*Kosten.* Die geseedete Wochenpflicht wechselt von „umdrehen und selbst bewerten" zu „Wort auswählen" —
eine Verhaltensänderung an laufenden Daten. Kein Test hat sie bemerkt (643 grün vor wie nach), was R4
bestätigt: den Lückentext hat nie jemand gespielt.

## Akzeptanzkriterien

- Zwei Lücken eines Lückentexts liefern zwei **unterscheidbare** Karten; das Kind sieht, welche gefragt
  ist.
- Die Stufe „Wortbank" liefert eine Wortbank.
- Die rohe Vorlagensyntax `{{n}}` erscheint nirgends in der Sohn-Ansicht.
- Dasselbe gilt in der Klausur, nicht nur beim Üben.
- Regressionstest, der vorher rot ist: eine gespielte Position mit **zwei** Lücken, geprüft darauf, dass
  die Karten sich unterscheiden — heute wären sie zeichengleich.

## Schätzung

**M · beides · keine Migration · kein Vertragsbruch.**

Kein Schema fasst das an: `GapIndex` liegt schon am `ContentItem` (ein Laufzeit-Record, keine Entität),
die Wortbank schon in der `ConfigJson`. Und beide neuen Vertragsfelder sind **additiv** und nullable —
`unknown_field` betrifft ohnehin nur Requests, diese beiden sind Response-DTOs.

Größer als [B-01](B-01-bildwahl-einfrieren.md) (S, ein Backend-Pfad), kleiner als eine Umbau-Etappe: zwei
Verträge, ein Typ-Override, ein neues Frontend-Bauteil in **zwei** Ansichten, und eine Stufe, die ihre
Bedeutung ändert (R1).

### Risiken

**R1 · Die Wortbank-Stufe ist heute gar nicht getippt — E4 läuft ins Leere.**
`StageMechanics.IsTyped(ClozeStage)` kennt nur `TranslationFreeText` und `FreeText`
([StageMechanics.cs:21-22](../../backend/Pugling.Api/Services/Shared/StageMechanics.cs)). Auf
`TranslationWordBank` ist `typed` also `false`, und daran hängen zwei Dinge: `CardFacets` liefert die
**Lösung** als `reveal` (`PositionPlayService.cs:120`), und beide Sohn-Ansichten rendern die Auswahl nur
im Zweig `typed && card.choices` ([SohnPractice.tsx:236](../../frontend/src/sohn/SohnPractice.tsx),
[SohnTest.tsx:153](../../frontend/src/sohn/SohnTest.tsx)). Eine Wortbank auf dieser Stufe wäre damit
doppelt wirkungslos: unsichtbar, und neben der schon sichtbaren Lösung sinnlos.

*Empfehlung:* `TranslationWordBank` wird **getippt** — dann verschwindet `reveal`, die Auswahl erscheint,
und die Stufe tut, was ihr Name verspricht. *Kosten:* Die geseedete Position (`Seed.cs:353`,
Wochenpflicht) wechselt von „Umdreh-Karte mit Selbsteinschätzung" zu „Wort auswählen" — die Antwort kommt
dann als `GivenAnswer` statt `WasKnown`, und der Server bewertet sie. Das ist eine Verhaltensänderung an
laufenden Daten, und sie ist in der Grill-Runde **nicht** gefallen: E4 setzt voraus, dass es eine
Wortbank-Stufe gibt, auf der eine Auswahl ankommt, und die gibt es noch nicht. Abgeleitet, nicht bestätigt
— vor dem Bauen zu klären, wie E5.

**R2 · `GapIndex` ist nur beim Lückentext gefüllt.** Bei allen anderen Typen steht `null` — kurz nachdem
B-01 zwei immer-`null`-Felder aus `TestItem` entfernt hat. Der Unterschied ist echt (dieses Feld trägt bei
genau dem Typ, der es braucht), aber der Name ist lückentext-eigen. Sollte [B-77](B-77-liste-menge-als-folge.md)
später ebenfalls eine Adressierung brauchen, ist zu entscheiden, ob sie sich das Feld teilen — **nicht**
vorsorglich verallgemeinern.

**R3 · Alte Daten können die Vorlage verletzen.** Der Editor prüft beim Anlegen, dass Platzhalter und
Lücken zueinander passen (`gapProblem`, [ClozeTexts.tsx:49](../../frontend/src/vater/ClozeTexts.tsx)) —
für bereits gespeicherte Übungen gilt das nicht. Der Renderer braucht darum einen Rückfall: findet er
`{{n}}` zum gefragten `GapIndex` nicht, zeigt er den Text unverändert statt gar nichts.

**R4 · Kein Test spielt heute einen Lückentext.** Die Cloze-Abdeckung liegt vollständig beim Anlegen und
beim Auflösen (`ExerciseContentProviderTests`, `CatalogReadDeleteTests`, `ExerciseTypeManifestTests`);
eine gespielte Position gibt es nirgends. Der Regressionstest wird also mit angelegt, nicht erweitert.

### Angriffsplan

Backend zuerst — das Frontend hängt an der API.

1. **Vertrag** (`Pugling.Contracts`): `int? GapIndex` additiv an `PracticeCard` und `TestItem`, je mit
   `/// <summary>` auf Englisch. Bei `PracticeCard` **hinter** die vorhandenen Vorgabewerte, sonst brechen
   die positionalen Aufrufe.
2. **Durchreichen**: `PositionPlayService.CardFacets` nimmt `GapIndex` in ihr Ergebnis-Tupel; die beiden
   Bauplätze ([PositionPracticeController.cs:108](../../backend/Pugling.Api/Controllers/Student/PositionPracticeController.cs),
   [PositionTestsController.cs:74](../../backend/Pugling.Api/Controllers/Student/PositionTestsController.cs))
   reichen es weiter. Eine Stelle, zwei Verbraucher — dieselbe Naht wie beim Bild.
3. **R1 entscheiden**, dann `ClozeExerciseType.Choices` überschreiben: auf der Wortbank-Stufe der volle
   Pool aus `ClozeConfig.WordBank`, sonst `null` (E4). Vorbild ist `VocabularyExerciseType.Choices`, nur
   ohne dessen Ablenker-Auswahl — hier wird nichts beschnitten.
4. **Artefakte** neu erzeugen: `docs/openapi/v1.json`, `openapi-examples.generated.json`,
   `docs/api-examples/` (die `DocsCaptureTests` schreiben sie im Lauf) und `frontend/src/lib/contract.ts`
   über `npm run gen:contract`.
5. **Frontend**: `placeholderIndices` von `ClozeTexts.tsx:28` nach `lib/` ziehen (der Editor benutzt es
   weiter), darauf ein Bauteil, das den Text mit hervorgehobener `gapIndex`-Lücke rendert. Eingesetzt in
   **beiden** Ansichten, `SohnPractice` und `SohnTest`, an der Stelle des heutigen
   `<div className="word">{card.prompt}</div>`. Die Wortbank-Knöpfe entstehen dabei von selbst — der
   `choices`-Zweig steht in beiden Dateien schon.

### Testweg

- **Regressionstest, vorher rot** (`Pugling.Api.Tests`, neue Klasse `ClozePlayTests` — R4): eine gespielte
  Position auf der geseedeten Übung mit **zwei** Lücken; geprüft wird, dass die beiden Karten sich in
  `gapIndex` unterscheiden. Heute sind sie zeichengleich, der Test fällt also am Ist-Stand.
- **Zweiter Fall in derselben Klasse:** die Wortbank-Stufe liefert `choices` mit allen fünf Einträgen aus
  `Seed.cs:1037`; die Freitext-Stufe liefert `null`.
- **Klausur (E5):** derselbe Durchlauf über `…/tests/{attemptId}/next` — die geseedete Freitext-Position
  trägt `RequireTypedTest = true`.
- **Frontend:** Komponententest (Vitest ist da, siehe `lib/useAction.test.tsx`) auf den Renderer: die
  gefragte Lücke hervorgehoben, die übrigen neutral, `{{n}}` nirgends sichtbar — dazu der Rückfall aus R3.
- **E2E:** `uebungstypen.spec.ts` ist der passende Ort, falls der Durchstich bis in die Sohn-Ansicht
  gewünscht ist; nicht zwingend, der Komponententest deckt die Darstellung ab.
- Kein `/smoke-test` nötig: Es entsteht kein neuer Endpunkt, nur ein Feld auf zwei bestehenden.

## Verlauf

- **2026-08-02** — angelegt aus der Grill-Runde zu B-75. Der Befund war schon dort am laufenden System
  belegt; P1 vom Nutzer gesetzt, mit Vorrang vor B-75, weil dieser Defekt heute an der geseedeten Familie
  wirkt.
- **2026-08-02** — ausformuliert. Die offene Frage „haben weitere Typen dieselbe Lücke?" ist beantwortet,
  und die Antwort ist größer als erwartet: **Liste** trifft es reiner als den Lückentext (16 geseedete
  Einträge, 16 gleiche Karten; ohne Anweisung sogar ein leerer Prompt), **Zuordnung** liefert auf beiden
  Stufen identische Karten ohne die versprochenen Ablenker, **Grammatik** verwirft die übergreifende
  Anweisung. Damit ist erkennbar, dass es *ein* Konstruktionsfehler ist und nicht drei Einzelfälle: Die
  Karte hat keinen Platz für übungsweiten Inhalt, und das Inhalts-Atom kann sich nicht ausweisen. Ob die
  Story auf Liste und Zuordnung wächst, ist als offener Punkt 3 formuliert — nicht entschieden.
- **2026-08-02** — gegrillt, fünf Entscheidungen. Die Runde hat die eigene Ausformulierung widerlegt:
  Deren Empfehlung, die Liste mitzunehmen, fiel an `ListConfig.Ordered` — der Katalog-Check bewertet eine
  ungeordnete Liste längst als Menge, nur der Übungs-Pfad nicht. Ein Etikett „3 von 16" hätte daran nichts
  geändert, also ist es ein anderer Fehler und wird [B-77](B-77-liste-menge-als-folge.md). Zuordnung ging
  an B-73, Grammatik an B-75, Birkenbihl wurde [B-78](B-78-birkenbihl-dekodierung-erreicht-kind-nicht.md)
  — seine Dekodierung ist strukturiert und passt in keine der beiden vorhandenen Formen.
  Nebenbei fiel eine Begriffskollision auf, die ich selbst gebaut hatte: „Trägertext" ist im Repo längst
  der Store-Eintrag `ClozeText`, nicht „irgendein Text, auf den sich eine Frage bezieht" (E3).
- **2026-08-02** — geschätzt: **M · beides · keine Migration · kein Vertragsbruch** (beide Felder additiv,
  `GapIndex` liegt schon am `ContentItem`). Die Schätzung hat eine Lücke in der Grill-Runde freigelegt und
  als **R1** festgehalten: `TranslationWordBank` ist gar keine getippte Stufe, also liefert die Karte dort
  heute die Lösung als `reveal`, und beide Sohn-Ansichten rendern `choices` nur im `typed`-Zweig. E4 setzt
  eine Stufe voraus, auf der eine Auswahl ankommt — die gibt es noch nicht. Empfohlen ist, die Stufe
  getippt zu machen; das ändert aber das Verhalten der geseedeten Wochenpflicht und ist vor dem Bauen zu
  bestätigen. Nebenbefund **R4**: Kein Test spielt heute einen Lückentext, die Abdeckung endet beim
  Anlegen und Auflösen.
- **2026-08-02** — gebaut. R1 vom Nutzer entschieden (Stufe wird getippt) und als **E6** nachgetragen.
  Ablauf wie geplant, mit **einer** Abweichung: `IExerciseType.Choices` konnte die Wortbank gar nicht sehen.
  Der Haken bekam nur `items` und war damit auf Pools beschränkt, die sich aus den Atomen ableiten lassen —
  für die Vokabel reicht das, für eine vom Autor gepflegte Bank nicht. `Choices` nimmt jetzt wie `Check`
  den `configJson` als ersten Parameter; das zieht `ExerciseTypeBase`, `VocabularyExerciseType`,
  `ExercisePreviewService` und `CardFacets` nach (rein intern, kein Vertragsfeld).
  Verifikation: **643 grün** (5 neu in `ClozePlayTests`, davon 4 vorher rot — `gapIndex` gab es nicht),
  **78 Frontend-Tests grün** (5 neu in `ClozePrompt.test.tsx`), `dotnet format` und `tsc --noEmit` sauber,
  `/smoke-test` grün, und der geseedete Lückentext live gegen `localhost:5280` gespielt:
  `gapIndex: 1` bzw. `2`, `choices` mit allen fünf Wörtern, `reveal: null`.
  **Offen für die Abnahme:** der `pugling-reviewer` — die Sitzungsregel „keine Agenten ohne Auftrag" steht
  dem entgegen, also von Hand gegengelesen und hier vermerkt statt still übergangen.
