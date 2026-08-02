---
tags: [typ/story, status/gegrillt, bereich/frontend, rolle/creator]
aliases: [Komma-Feld ablösen, Wiederhol-Felder]
status: gegrillt
prio: P2
art: Defekt
quelle: B-65 (Entscheidung 7)
---

# B-69 · Kommagetrennte Sammelfelder: einer davon nimmt gar keine zweite Alternative an

## User Story

Als **Creator** möchte ich mehrere Alternativen als **einzelne Felder** eintragen, damit ich (a) eine
Alternative anlegen kann, die selbst ein Komma enthält, und (b) im Lückentext-Editor überhaupt eine
**zweite** Alternative tippen kann.

## Ist-Stand am Code

### Der generische Übungs-Editor: fünf Sammelfelder, nicht drei

[exerciseConfig.tsx](../../frontend/src/vater/exerciseConfig.tsx) hält jede Liste als **einen**
Text-String in einer untypisierten Zeile (`Row = Record<string, any>`, `:19`) und wandelt sie erst beim
Senden mit `splitList` (`:68-71`) bzw. beim Laden mit `joinList` (`:73`):

| Feld | Beschriftung | Zeile | Vertrag |
| --- | --- | --- | --- |
| Lückentext-Alternativen | „Alternativen (kommagetrennt)" | `:493` | `Gap.Alternatives` ([ExerciseConfigs.cs:80](../../backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs)) |
| Listen-Alternativen | „Alternativen (kommagetrennt)" | `:501` | `ListEntry.Alternatives` (`:218`) |
| Übersetzungs-Alternativen | „Alternativen (kommagetrennt)" | `:521` | `TranslationItem.Alternatives` (`:154`) |
| Auswahl (Reading/Listening) | „Auswahl (kommagetrennt = Multiple-Choice)" | `:506` | `Question.Choices` (`:14`) |
| Wortpool (Lückentext) | „Wortpool (optional, kommagetrennt)" | `:338` | `ClozeConfig.WordBank` (`:73`) |

Die Story ging von **drei** aus; es sind fünf. Dazu kommt ein sechstes Feld mit **zwei** Trennzeichen:
die Birkenbihl-Dekodierung „Wort:wörtlich, …" (`:525`), gebaut über `split(",")` + `split(":")` (`:203`)
und zurückgelesen über `.join(", ")` (`:295`) — Vertrag `BirkenbihlSentence.Decoding` als
`List<WordPair>` (`:249`).

### Der eigene Lückentext-Editor ist ein sechster Ort — und dort ist das Feld unbenutzbar

[ClozeTexts.tsx](../../frontend/src/vater/ClozeTexts.tsx) ist eine **zweite**, unabhängige Oberfläche für
Lücken und Wortpool; die Story kannte sie nicht. Der Wortpool arbeitet dort wie im generischen Editor
(roher String im State, `:127`/`:153`). Das Alternativen-Feld nicht:

```tsx
// :142-145   der State hält das ZERLEGTE Array …
const alternatives = raw.split(",").map((s) => s.trim()).filter(Boolean);
// :236-238   … und der Wert des Feldes wird daraus bei jedem Tastendruck neu zusammengesetzt
value={(g.alternatives ?? []).join(", ")}
```

Der Rundlauf verschluckt genau das Zeichen, das man gerade tippt:

| getippt | State | Feldwert danach |
| --- | --- | --- |
| `a` | `["a"]` | `a` |
| `a,` | `["a"]` | `a` ← **das Komma ist weg** |
| `a,` + Leerzeichen | `["a"]` | `a` |

Damit lässt sich in diesem Editor **keine zweite Alternative tippen**: das Trennzeichen verschwindet in
dem Moment, in dem es entsteht. Nur Einfügen aus der Zwischenablage kommt durch, weil dabei der ganze
Wert (`a, b`) in einem Schritt gesetzt wird. Beleg: die Zeilen oben plus die reine Funktion
durchgerechnet; ein Regressionstest, der das rot zeigt, fehlt noch (er ist das erste Stück Arbeit,
`art: Defekt`).

### Die Alternativen wirken auf die Bewertung

Sie sind kein Beiwerk: `AnswerGrader.cs:23` und `BuiltInExerciseTypes.cs:81,118,269,333` sowie
`ExerciseContentResolver.cs:123,125` nehmen sie in die akzeptierten Antworten auf — dieselbe Kette, an
der in B-65 der Münz-Malus hing.

### Was schon dasteht, und was daran hängt

- Die Zielform existiert seit B-65: [RepeatedTextFields.tsx](../../frontend/src/components/RepeatedTextFields.tsx)
  (ein Feld je Wert, „+ …", Entfernen, `scope` gegen doppelte Namen) samt `nonEmpty(values)` für den
  Sendeweg und fünf Vitest-Fällen.
- **E2E**: nur **ein** Test fasst eines dieser Felder an —
  [uebungstypen.spec.ts:69](../../frontend/e2e/uebungstypen.spec.ts) füllt „Auswahl" mit
  `"Leeds, York, Hull"`. Die drei Alternativen-Felder, der Wortpool und die Dekodierung kommen in keinem
  E2E vor. Die Annahme der Idee („die E2E-Tests der drei Übungstypen hängen mit dran") trifft also nur
  für das Auswahl-Feld zu.
- **Unit-Tests**: zu `splitList`/`joinList` und `exerciseConfig.tsx` gibt es keinen einzigen.
- **Bestand**: in der lokalen Entwicklungs-DB stehen sechs verschiedene Alternativen-Listen
  (`["Hi"]`, `["good","well"]`, `["achète"]`, `["maison"]`, `["'d be"]`, `[]`); **keine** enthält ein
  Komma, keine sieht zerrissen aus. Für andere Datenbestände ist das keine Aussage — es gibt nur diese
  eine.

### Gleiche Bauart außerhalb des Übungs-Editors

Vier weitere Komma-Felder, **nicht** Teil dieser Story, hier nur notiert, damit sie beim Zählen nicht
wieder auftauchen: Interessen am Kind (`VaterKind.tsx:188,239`), Medien-Schlagworte
(`VaterMedia.tsx:93,136,179`), Interessen-Synonyme (`InterestTagAdmin.tsx:101`) und die Schularten im
Übungs-Dialog (`ExerciseEditModal.tsx:142`). Die letzte ist harmlos: Enum-Werte enthalten kein Komma.

## Die echte Lücke

Zwei verschiedene Fehler unter einer Überschrift — der zweite ist der teurere:

1. **Ein Wert mit Komma ist nicht eintragbar** und wird beim Senden stillschweigend in zwei zerlegt. Das
   trifft alle sechs Felder, realistisch aber vor allem die **Übersetzungs**-Alternativen (ganze Sätze)
   und die **Auswahl** (Antwortsätze).
2. **Im eigenen Lückentext-Editor ist das Feld für mehr als einen Wert kaputt** — nicht am Rand, sondern
   im Normalfall: Wer eine zweite Alternative tippen will, sieht sein Komma verschwinden. Kein Fehler,
   keine Meldung.

Der gemeinsame Grund ist derselbe: Ein Trennzeichen in der Oberfläche, wo der Vertrag längst eine Liste
führt (`List<string>?`). Die Zielform steht seit B-65 fertig da.

## Offene Punkte

Alle sechs sind in der Grill-Runde vom 2026-08-02 gefallen, siehe [Entscheidungen](#entscheidungen).

1. ~~**Bleibt die Prio bei P3?**~~ → **E1.** Befund 2 macht ein Feld im Normalfall unbenutzbar, nicht nur im Randfall —
   das ist eine andere Größenordnung als die Idee annahm. *Empfehlung: auf P2, mit demselben Argument wie
   bei B-65 (die Alternativen entscheiden über die Bewertung).* Wahlweise: Befund 2 sofort als eigene
   kleine Story ziehen und den Rest bei P3 lassen.
2. ~~**Alle sechs Felder oder nur die drei Alternativen?**~~ → **E2.** *Empfehlung: die fünf Listenfelder gemeinsam —
   es ist dieselbe Komponente, und drei umzustellen und zwei stehen zu lassen erzeugt genau die
   Uneinheitlichkeit, die später niemand mehr erklärt.* Die **Dekodierung** (`:525`) ist ein anderer Fall
   (Paare, zwei Trennzeichen) und braucht eine eigene Form — *Empfehlung: hier ausdrücklich
   zurückstellen.*
3. ~~**Der Wortpool ist keine Alternative, sondern ein Pool**~~ → **E2.** (`:338`, `ClozeTexts.tsx:249`) — einzelne
   Wörter, ein Komma darin ist unwahrscheinlich. *Empfehlung: trotzdem mitnehmen, weil er im selben
   Formular steht; die Kosten sind eine Zeile.*
4. ~~**Was passiert mit einem Bestandswert, der ein Komma enthält?**~~ → **E6.** Er ist heute nicht erzeugbar, also
   kann es ihn nur aus einem Import oder direkt aus der API geben. *Empfehlung: keine Datenwanderung —
   die neue Form zeigt jeden gespeicherten Wert unverändert in seinem eigenen Feld, und damit wird ein
   zerrissener Wert überhaupt erst sichtbar.*
5. ~~**Braucht der generische Editor typisierte Zeilen?**~~ → **E5.** `Row = Record<string, any>` (`:19`) lässt den
   Wechsel `string` → `string[]` **stumm** durchgehen; ein vergessener Aufrufer fällt erst zur Laufzeit
   auf. *Empfehlung: nicht in dieser Story — aber der Umbau ist der Anlass, es als eigene
   Aufräum-Story zu notieren.*
6. ~~**Wird `splitList` danach noch gebraucht?**~~ → **E2 (Folge).** Nach der Umstellung aller fünf Felder hätte es keinen
   Aufrufer mehr. *Empfehlung: löschen, nicht „für später" stehen lassen.*

## Entscheidungen

Gefallen in der Grill-Runde vom 2026-08-02.

**E1 · Ein Zuschnitt, hoch auf P2.** Beide Befunde bleiben in dieser Story; die Prio steigt von P3 auf P2.
*Begründung:* Sie haben dieselbe Ursache (Trennzeichen in der Oberfläche, Liste im Vertrag) und dieselbe
Lösung (`RepeatedTextFields`); getrennt gebaut würde dieselbe Datei zweimal angefasst — erst ein Pflaster
im Split/Join, später der Umbau. *Kosten:* Die Story wird größer, und der leichte Befund erbt die
Dringlichkeit des schweren, obwohl er sie allein nicht hätte.

**E2 · Alle fünf Listenfelder, an beiden Orten; die Dekodierung ausdrücklich zurückgestellt.** Umgestellt
werden die drei Alternativen-Felder, das Auswahl-Feld und der Wortpool — in
[exerciseConfig.tsx](../../frontend/src/vater/exerciseConfig.tsx) **und** in
[ClozeTexts.tsx](../../frontend/src/vater/ClozeTexts.tsx). *Begründung:* Es ist dieselbe Komponente in
einem Durchgang; drei umzustellen und zwei stehen zu lassen hinterlässt zwei Bauarten im selben Formular,
und das liest der Nächste als Absicht. Der Wortpool ist zwar kein Alternativen-Feld (einzelne Wörter, ein
Komma darin unwahrscheinlich), steht aber im selben Formular — die Kosten sind eine Zeile.
*Kosten:* [uebungstypen.spec.ts:69](../../frontend/e2e/uebungstypen.spec.ts) muss nachgezogen werden (das
Auswahl-Feld ist das einzige, das ein E2E anfasst). Die Birkenbihl-Dekodierung bleibt draußen: Paare mit
zwei Trennzeichen brauchen eine andere Form, die `RepeatedTextFields` nicht kann → [B-72](B-72-birkenbihl-dekodierung-paarfelder.md).
*Folge, keine eigene Entscheidung:* `splitList` verliert damit alle fünf Aufrufer und wird gelöscht;
`joinList` überlebt allein für die zurückgestellte Dekodierung (`:295`).

**E3 · „Auch richtig" ist das eine Wort — und die Vokabel behält ihren eigenen Hilfetext.** Für „weitere
richtige Antwort" stehen heute drei Formulierungen nebeneinander: „Alternativen (kommagetrennt)"
(`:493,501,521`), „Auch gültig (Komma)" (`ClozeTexts.tsx:226`) und „Gleichwertige Übersetzungen"
(`VaterVocab.tsx`, aus B-65). Alle drei werden zu **„Auch richtig"**. *Begründung:* Das Wort sagt, was
passiert, und grenzt gegen „Auswahl" ab, wo auch Falsches steht — „Alternative" ist im Deutschen
zweideutig (Ersatz oder Zusatz), und genau darum hat B-65 es schon einmal vermieden. Drei Wortlaute für
einen Begriff sind der Fall, vor dem `frontend/CLAUDE.md` warnt. *Kosten:* B-65s frisch abgenommene
Beschriftung wird mitgeändert (kein Test hängt daran, geprüft). **Zwei `HelpTopic`s, nicht einer:** der
Vokabel-Text trägt die Homonym-Regel („lege keine zweite Zeile mit demselben Wort an"), die dort tragend
und bei Lücke, Liste und Übersetzung sinnlos ist — es gibt dort keine zweite Zeile mit demselben Wort.
Ein gemeinsamer Text müsste die Regel entweder verlieren oder an vier Stellen als Rauschen zeigen.

**E4 · Das Auswahl-Feld wird umgestellt, sein toter Pfad wird eine eigene Story.**
`AnswerChecking.FromQuestions` ([BuiltInExerciseTypes.cs:307](../../backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs))
baut `new ContentItem(i, q.Prompt, q.Answer, [q.Answer])` und verwirft `q.Choices`; nur
`VocabularyExerciseType` überschreibt `Choices`, Reading und Listening erben die Basis mit `null`. Das
Feld wird also gespeichert und wieder angezeigt, aber das Kind sieht nie eine Auswahl. *Begründung:*
Oberfläche und Engine sind zwei verschiedene Fehler; der zweite ist Backend-Arbeit und würde diese Story
sprengen. *Kosten:* Bis [B-73](B-73-auswahl-feld-ohne-wirkung.md) läuft, ist das Feld eine schönere
Attrappe als vorher.

**E5 · Abgesichert wird mit Vitest auf Hin- und Rückweg; `Row` bleibt untypisiert.** Je umgestelltem
Übungstyp ein Fall: Zeile befüllen → `buildTypeConfig` → `configToEditorState` → dieselben Werte zurück,
darunter einer mit Komma. Beide Funktionen sind exportiert (`:121`, `:225`), der Test braucht keinen
Umbau. *Begründung:* `Row = Record<string, any>` (`:19`) lässt den Wechsel `string` → `string[]` **stumm**
durchgehen; ein Test fängt genau den vergessenen Aufrufer, den der Typ nicht fängt — und die Datei, die
elf Formulare trägt, bekommt endlich eine Testebene. Typisieren wäre größer als der Umbau selbst und
ließe den Rückweg aus `unknown` trotzdem ungetypt → [B-74](B-74-editor-zeilen-typisieren.md).
*Kosten:* Der Compiler hilft weiter nicht; die Absicherung hängt an der Vollständigkeit der Fälle.

**E6 · Kein Datenumzug, aber Normalisierung beim Speichern.** Gespeicherte Werte bleiben, wie sie sind;
die neue Form zeigt jeden in seinem eigenen Feld. Eine leere Liste (in der lokalen DB steht ein
`alternatives":[]`) wird beim nächsten Speichern über `nonEmpty` zu `null`/weggelassen. *Begründung:* Ein
zerrissener Wert wird durch die Einzelfelder überhaupt erst sichtbar — das ist mehr wert als eine stille
Reparatur; und `[]` als zweite Schreibweise für „nichts" ist derselbe Fehler, den B-65 gerade abgeschafft
hat. *Kosten:* In unberührten Übungen bleibt `[]` stehen, wer nach „ein Wert oder keiner" fragt, muss
weiter beides prüfen.

## Akzeptanzkriterien

1. Im **eigenen Lückentext-Editor** lassen sich zwei Alternativen **tippen**; ein Regressionstest, der
   vorher rot ist, hält das fest.
2. Eine Alternative, die selbst ein Komma enthält („groß, wirklich groß"), lässt sich in jedem der fünf
   umgestellten Felder anlegen, speichern und unverändert wieder aufrufen.
3. Die fünf Felder benutzen `RepeatedTextFields`; jede Instanz trägt einen `scope`, sodass zwei Zeilen
   desselben Formulars keine gleichlautenden Feldnamen haben.
4. Der Rückweg zeigt jeden gespeicherten Wert in **seinem eigenen** Feld — auch einen, der ein Komma
   enthält.
5. Leere Felder senden **keinen** leeren Eintrag (`nonEmpty`); eine vorhandene leere Liste wird beim
   Speichern zu `null`/weggelassen.
6. Die Beschriftung heißt an allen drei Orten **„Auch richtig"** (auch im Vokabel-Store); „(kommagetrennt)"
   und „(Komma)" sind verschwunden. Zwei `HelpTopic`s: der allgemeine und der vokabel-eigene mit der
   Homonym-Regel.
7. Ein Vitest je umgestelltem Übungstyp über `buildTypeConfig` → `configToEditorState`, mit einem
   Komma-Wert darin.
8. `uebungstypen.spec.ts` (Auswahl-Feld, `:69`) ist nachgezogen und grün; die übrigen E2E bleiben
   unberührt.
9. `splitList` ist gelöscht und hat keinen Aufrufer mehr.
10. Vitest und `npm run build` grün, `frontend-reviewer` durch.

## Verlauf

- **2026-08-02** — angelegt als Folge von B-65, Entscheidung 7.
- **2026-08-02** — ausformuliert am Code. Zwei Funde ändern den Zuschnitt: Es sind **fünf** Listenfelder
  im Übungs-Editor statt drei (plus die Birkenbihl-Dekodierung als eigener Fall), und es gibt einen
  **zweiten** Editor — `ClozeTexts.tsx`. Dort ist das Alternativen-Feld nicht bloß komma-empfindlich,
  sondern für einen zweiten Wert **unbenutzbar**: Der State hält das zerlegte Array, der Feldwert wird
  daraus bei jedem Tastendruck neu zusammengesetzt, und dabei verschwindet das gerade getippte Komma
  (`:142-145` / `:236-238`). Widerlegt ist dagegen die Annahme, die E2E hingen mit dran — nur
  `uebungstypen.spec.ts:69` fasst eines dieser Felder an, und in der lokalen DB liegt kein zerrissener
  Wert.
- **2026-08-02** — gegrillt. Sechs Entscheidungen, keine zurückgestellt. Der Zuschnitt bleibt eine Story
  und steigt auf **P2**, weil ein Feld im Normalfall unbenutzbar ist und nicht bloß im Randfall. Zwei
  Funde der Runde selbst: Für „weitere richtige Antwort" standen **drei** Formulierungen nebeneinander —
  jetzt heißt es überall „Auch richtig" —, und das Auswahl-Feld ist **wirkungslos**, weil
  `AnswerChecking.FromQuestions` seine Optionen verwirft. Drei abgeleitete Stories: [B-72](B-72-birkenbihl-dekodierung-paarfelder.md)
  (Dekodierung als Paar-Form), [B-73](B-73-auswahl-feld-ohne-wirkung.md) (der tote Pfad) und
  [B-74](B-74-editor-zeilen-typisieren.md) (`Row` typisieren).
