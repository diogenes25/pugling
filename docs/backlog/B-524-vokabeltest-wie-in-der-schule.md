---
tags: [typ/story, status/in-arbeit, rolle/student, rolle/supervisor]
aliases: [Schul-Vokabeltest, Lückensätze mit deutschem Wortkasten]
status: in-arbeit
prio: P1
art: Wunsch
groesse: L
wo: beides
migration: nein
vertragsbruch: nein
quelle: "Echter Vokabeltest von Friedrich (Englisch, 11.11.2026), abgeschrieben in raw/english/real_tests/2026-11-11-friedrich-vokabeltest.md (gitignored); Nutzer im Chat 2026-11-13: „Solche Tests soll Friedrich entsprechend öfter erhalten. Die Art des Tests ist mir wichtig.“"
grund: ""
ersetzt_durch: []
wartet_auf: "Merge von refs/heads/b-524-schul-vokabeltest und Rollengang in der Arcade (Klick)"
---

# B-524 · Vokabeltest im Format der Schule: englische Lückensätze, deutscher Wortkasten, frei geschrieben

Friedrichs Englischlehrer prüft Vokabeln in einem festen Format. Pugling soll Friedrich Tests **genau dieser Art**
regelmäßig stellen. So übt er die Prüfungsform, die in der Schule zählt, und nicht nur die Vokabeln.

Das Format, aus dem echten Test vom 11.11.2026:

- **Englische Lückensätze** (etwa 12 Stück, je eine oder zwei Lücken) aus dem Wortfeld einer Unit, alle auf
  einem Blatt.
- **Wortkasten auf Deutsch**, ungeordnet, mit mehr Einträgen als Lücken (16 zu 15). Er nennt die Bedeutung, nicht
  die Schreibweise und nicht, welche Lücke gemeint ist. Ablenker sind Wortfamilien (Rassismus/rassistisch).
- Das englische Wort wird **frei geschrieben**, in der Form, die der Satz verlangt („patients“, „Parliament
  debates“, „racism“ oder „racist“).
- **Rechtschreibung:** Auf dem Blatt kostet ein Schreibfehler bei richtiger Bedeutung nur −½ Punkt. Das ist
  **Friedrichs LRS-Nachteilsausgleich**, keine Regel des Formats; sonst gilt falsch geschrieben als falsch
  (Entscheidung 2).

Warum das wichtig ist: Im echten Test hatte Friedrich alle Bedeutungen richtig. Jeder Abzug kam von der
Rechtschreibung („clinik“, „heiht“, „femal“).

## User Story

Als **Supervisor** (Vater) möchte ich Friedrich regelmäßig einen Vokabeltest stellen, der aussieht und gewertet wird
wie der Test seines Englischlehrers, **damit** er die Prüfungsform übt, in der seine Note entsteht, und seine
eigentliche Schwäche, die Rechtschreibung, sichtbar wird und nicht untergeht.

Als **Student** (Friedrich) möchte ich in diesem Test sehen, was ich falsch geschrieben habe und was ich nicht
wusste, **damit** ich weiß, ob ich Wörter üben muss oder ihre Schreibweise.

## Ist-Stand am Code

Vieles ist schon da. Der Lückentext-Typ trägt das Format zu großen Teilen.

**Englische Lücke, deutsche Bedeutung, frei getippt: gibt es.**

- Der Typ `cloze` (`backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs:182`) hat drei Stufen
  (`:280-282`): „Wortbank“, „Übersetzung + Freitext“, „Freitext“. Alle drei werden serverseitig geprüft
  (`backend/Pugling.Api/Services/Shared/StageMechanics.cs:28`).
- Die deutsche Bedeutung kommt **pro Lücke** aus dem Vokabelspeicher: Hat die Lücke einen `VocabKey`, ist die
  Lösung das Speicherwort und der Hinweis dessen Übersetzung
  (`backend/Pugling.Api/Services/Shared/ExerciseContentResolver.cs:157-158`). Lücken ohne `VocabKey` haben gar
  keinen Hinweis (`:156`).
- Der Hinweis erscheint nur auf getippten Stufen (`backend/Pugling.Api/Services/Shared/PositionPlayService.cs:326`).

**Wortbank: gibt es, aber auf Englisch.**

- `ClozeConfig.WordBank` ist eine Liste von Lösungswörtern zum Anklicken
  (`backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs:78`). Sie wird nur auf der Stufe „Wortbank“ gezeigt,
  alphabetisch sortiert und um fehlende Lösungen ergänzt (`BuiltInExerciseTypes.cs:232-256`). Ein Wortkasten aus
  **deutschen** Bedeutungen, der nicht verrät, welche Lücke gemeint ist, existiert nicht.

**Rechtschreibung: streng, aber nur richtig oder falsch.**

- Die Prüfung vergleicht nach `Normalize`: Groß-/Kleinschreibung, Leerraum, Apostroph-Varianten und
  Satzzeichen am Ende fallen weg (`StageMechanics.cs:39-41`). Englische Artikel dürfen fehlen (`:61-64`).
  Einen Tippfehler-Abstand (Levenshtein o. ä.) gibt es nicht. „clinik“ gegen „clinic“ zählt also als falsch. Das ist
  das gewünschte Strenge-Niveau.
- Es gibt **keine Teilpunkte**. Der Test zählt richtige Items und rechnet Prozent
  (`backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs:568-574`). Bestanden heißt 80 %, wenn die Position
  nichts anderes setzt (`backend/Pugling.Api/Controllers/Student/PositionTestsController.cs:30`). „Bedeutung
  richtig, falsch geschrieben“ ist von „nicht gewusst“ nicht zu unterscheiden.

**Testablauf: anders als auf dem Blatt.**

- Der Abschlusstest einer Position ist eine Klausur, die eine Frage nach der anderen ausspielt (`/next`,
  `/answer`), ohne Zurückgehen und mit Rückmeldung erst beim Abgeben (`PositionTestsController.cs:116-121`).
- Beim Lückentext ist jede Karte **eine Lücke** (B-76). Der ganze Text steht als Kontext dabei, die gefragte Lücke
  ist markiert. Das Blatt der Schule zeigt dagegen alle Sätze gleichzeitig.
- Die Stufe des Tests legt für das Kind der Fahrplan fest, frei wählen darf sie nur der Supervisor
  (`PositionTestsController.cs:167-171`).

**Form im Satz gegen Grundform im Speicher.**

- Mit `VocabKey` ist die Lösung das **Speicherwort**, meist die Grundform („debate“, „patient“). Die Lücke
  verlangt aber oft eine gebeugte Form („debates“, „patients“). `Gap.Alternatives` wird zusätzlich akzeptiert
  (`ExerciseContentResolver.cs:158`). Die Grundform bleibt aber die Hauptlösung und wird damit als richtig gewertet,
  obwohl die Schule die falsche Form abzöge. Eine Inline-Lösung (`Gap.Answer`) ohne `VocabKey` hat die richtige Form,
  verliert aber den deutschen Hinweis.

**Wer erzeugt solche Tests, und wie oft kommen sie?**

- Der KI-Creator baut mit `exam` Übungsklausuren: je Typ ein Pipeline-Lauf, dazu für ein Kind eine geplante
  Klassenarbeit (`backend/Pugling.Agent.Creator/ExamPlanner.cs:39`, `backend/Pugling.Agent.Creator/CLAUDE.md`
  „ExamPlanner“). Ob die erzeugten Lückentexte `VocabKey`s und damit deutsche Hinweise tragen, ist nicht geprüft.
- „Öfter“ kann heute nur über Plan-Positionen mit Pflichtziel oder über Klassenarbeiten laufen. Einen
  wiederkehrenden Test (etwa jede Woche ein neuer Satz aus der aktuellen Unit) gibt es nicht. Ob das Pflichtziel
  einer Position einen Abschlusstest wiederholt verlangen kann, ist nicht nachgesehen.

## Die echte Lücke

Der Lückentext auf der Stufe „Übersetzung + Freitext“ ist **fast** der Schultest. Es fehlen vier Dinge, nach
Gewicht geordnet:

1. **Die Wertung kennt keinen halben Punkt.** Bedeutung richtig, aber falsch geschrieben, und gar nicht gewusst
   sind heute dasselbe „falsch“. Genau diese Unterscheidung ist der Kern des Schultests und von Friedrichs
   Befund.
2. **Der deutsche Hinweis steht an der Lücke statt im Wortkasten.** Heute sagt die Karte „Gesundheit“ direkt zur
   gefragten Lücke. Die Schule gibt einen ungeordneten Kasten mit Ablenkern. Das Kind muss erst aus dem Satz
   erschließen, welche Bedeutung passt.
3. **Gebeugte Formen sind mit Speicher-Lücken nicht sauber prüfbar.** Grundform als Lösung und gebeugte Form als
   Alternative werten „Parliament debate the future“ als richtig.
4. **Kein regelmäßiger Takt.** Tests dieser Art „öfter“ zu stellen, heißt heute, sie von Hand oder per `exam`
   einzeln anzulegen und zuzuweisen.

Nicht Teil der Lücke: die Strenge der Rechtschreibprüfung (sie ist schon exakt) und die freie Eingabe (Stufe 3 und
4 gibt es).

## Offene Punkte

Alle beantwortet in der Grill-Runde vom 2026-11-13, siehe Entscheidungen.

1. ~~**Halber Punkt: neue Wertung oder nur Anzeige?**~~ → Entscheidung 2 (gegen die Empfehlung: nur Anzeige). Soll der Test Teilpunkte vergeben (Note, Prozent und
   Bestehen rechnen mit ½), oder reicht es, die falsch geschriebenen Wörter als eigene Kategorie zu zeigen, während
   die Wertung binär bleibt? *Empfehlung:* echte Teilpunkte im Test, beim Üben (Leitner) aber weiter binär. Leitner
   muss wissen, ob das Wort sitzt, und falsch geschrieben sitzt es nicht. Kosten: ein dritter Ergebnis-Zustand am
   `TestItemResult` (Migration), eine Prozentrechnung mit Gewicht und ein Erkennungsmaß dafür, was „falsch
   geschrieben“ heißt.
2. ~~**Woran erkennt der Server „falsch geschrieben“?**~~ → Entscheidung 3. Vorschlag: höchstens ein bis zwei Zeichen Editierabstand
   zur Lösung, abhängig von der Wortlänge, und kein anderes Wort des Wortkastens näher dran. *Empfehlung:*
   Damerau-Levenshtein ≤ 1 bei Wörtern bis 5 Buchstaben, ≤ 2 darüber. Am echten Test prüfen: „clinik“, „heiht“,
   „femal“ müssen ½ bekommen, „racist“ statt „racism“ den vollen Abzug, obwohl der Abstand nur 2 ist. Daran zeigt
   sich: ein reiner Abstand reicht nicht, es braucht den Vergleich mit den anderen Lösungen.
3. ~~**Wortkasten: neues Feld oder neue Stufe?**~~ → Entscheidung 4. Entweder eine vierte Cloze-Stufe „Deutscher Wortkasten + Freitext“,
   die die Übersetzungen aller Lücken plus Ablenker ungeordnet zeigt und den Hinweis an der Lücke weglässt, oder ein
   eigener Übungstyp. *Empfehlung:* neue Stufe am bestehenden Typ (CLAUDE.md: kein Parallel-Stack). Ablenker als
   neues Feld `ClozeConfig.CueDistractors` (deutsche Wörter). Kosten: Vertragsfeld, Manifest-Fähigkeit, Editor.
4. ~~**Ein Blatt oder eine Lücke je Karte?**~~ → Entscheidung 5. Die Schule zeigt alle Sätze auf einmal. Die Klausur in Pugling fragt
   strikt einzeln, mit gutem Grund (Anti-Cheat, eingefrorene Reihenfolge). *Empfehlung:* einzeln lassen. Der
   Unterschied ändert, was geübt wird, kaum, aber ein Blattmodus wäre ein großer Umbau. Dafür den deutschen
   Wortkasten auf **jeder** Karte vollständig zeigen, damit sich Friedrich wie auf dem Blatt orientiert.
5. ~~**Gebeugte Formen:**~~ → Entscheidung 6. Soll eine Lücke mit `VocabKey` eine eigene **Satzform** tragen (z. B. `Gap.Form =
   "debates"`), die die Hauptlösung ersetzt, während der Speicher den Hinweis und die Lernstands-Verknüpfung
   liefert? *Empfehlung:* ja. Sonst bleibt die Wahl zwischen deutschem Hinweis und richtiger Form.
6. ~~**Takt „öfter“:**~~ → Entscheidung 7. Wie oft, und woher kommt der Stoff? *Empfehlung:* jede Woche ein Test aus den Wörtern der
   aktuellen Unit plus einem Anteil schwacher Wörter aus früheren Units, erzeugt vom KI-Creator (`exam` mit einem
   Format-Schalter) und als Klassenarbeit bzw. Position mit Pflichtziel zugewiesen. Erst prüfen, ob ein Pflichtziel
   wiederkehrende Tests schon trägt. Wenn ja, ist dieser Punkt reine Inhaltsarbeit und kein Code.
7. ~~**Nur Friedrich oder alle Kinder?**~~ → Entscheidung 8. *Empfehlung:* allgemein bauen und je Position per Stufe wählbar machen.
   Theodors Schule prüft vielleicht anders, das Format ist aber keine Friedrich-Sonderlogik.

## Entscheidungen

Grill-Runde mit dem Nutzer am 2026-11-13.

1. **Begriffe: Die Tests im Schulformat sind Abschlusstests von Plan-Positionen.** Nur dort entstehen Wertung,
   Pflicht und Münzen. Echte Schultests werden zusätzlich als `Klassenarbeit` mit Note erfasst (der vom 11.11.2026
   mit Note 1), damit man später sieht, ob das Üben die Noten bewegt. In der Story heißt es „Vokabeltest im
   Schulformat“, nicht „Klausur“: Das Wort ist in Pugling ein Ausspiel-Modus. Kosten: keine.
2. **Die Wertung bleibt binär, Schreibfehler werden nur gesondert ausgewiesen.** Das −½ auf dem Blatt ist
   Friedrichs LRS-Nachteilsausgleich, keine Regel des Formats. Pugling bildet ihn bewusst nicht nach und wertet
   falsch geschrieben als falsch, wie ohne Ausgleich. Gegen die Empfehlung (Teilpunkte im Test). Kosten: ein
   dritter Anzeige-Zustand je Lücke im Testergebnis; Prozent, Bestehen und Münzen bleiben unberührt.
3. **„Falsch geschrieben“ heißt: kleiner Tippabstand und keine andere Lösung näher dran.** Damerau-Levenshtein ≤ 1
   bei Lösungen bis 5 Buchstaben, ≤ 2 darüber, nach `Normalize` verglichen. Liegt die Antwort einer anderen
   Lösung desselben Tests mindestens so nah, ist sie ein falsches Wort („racist“ in der Lücke für „racism“).
   Begründung: Die Kategorie sagt dem Vater, ob Wörter oder Schreibweisen fehlen. Ein Fehlurteil kostet keine
   Punkte, nur eine schiefe Anzeige. Kosten: eine Abstandsfunktion und der Vergleich über alle Lücken.
4. **Neue vierte Cloze-Stufe „Wortkasten + Freitext“.** Die Karte zeigt die deutschen Bedeutungen aller Lücken
   (aus dem Vokabelspeicher) plus handgepflegte deutsche Ablenker, alphabetisch und auf jeder Karte gleich. An der
   gefragten Lücke steht kein Hinweis. Ablenker als neues Feld am `ClozeConfig`, gefüllt vom Autor oder vom
   KI-Creator. Kein eigener Übungstyp (Projektregel: kein Parallel-Stack); keine automatischen Ablenker, weil
   gezielte Wortfamilien (Rassismus/rassistisch) der Kern des Formats sind. Kosten: Vertragsfeld,
   Manifest-Fähigkeit, Editor, Loader, Stufenwahl.
5. **Eine Lücke je Karte wie bisher, der volle Wortkasten auf jeder Karte.** Kein Blattmodus: Die eingefrorene
   Reihenfolge ohne Zurückgehen ist Anti-Cheat. Was geübt wird, bleibt gleich. Kosten: Das Ausschlussprinzip vom
   Blatt fällt weg; das macht den Test eher schwerer, gewollt.
6. **Neues optionales Feld „Satzform“ an der Lücke.** Ist es gesetzt, ist es die Lösung, und die Grundform allein
   zählt als falsch. Bedeutung für den Wortkasten und die Lernstand-Verknüpfung kommen weiter über `VocabKey`
   aus dem Speicher. Der KI-Creator setzt es beim Erzeugen. Kosten: Vertragsfeld am `Gap`, Editor und Loader
   ziehen nach; keine Migration, weil die Lücken als JSON in der Übung liegen.
7. **Rhythmus: wöchentlich eine neue Übung vom KI-Creator.** Ein Format-Schalter erzeugt einen Test im
   Schulformat aus der aktuellen Unit plus schwachen Wörtern früherer Units. Der Vater hängt ihn als Position mit
   Takt wöchentlich an; Pflichtziel und Münz-Malus tragen das schon
   (`backend/Pugling.Api/Models/PlanPositionEntities.cs:51`, `:89`). Keine feste Übung je Unit (Friedrich lernte
   die Sätze auswendig), keine Vollautomatik (bräuchte ein LLM im Backend). Kosten: ein Format-Schalter am
   KI-Creator und ein Aufruf je Woche durch den Vater.
8. **Allgemein gebaut, pro Position wählbar; zuerst setzt nur Friedrich es ein.** Nach Entscheidung 2 trägt das
   Format keine Kind-Sonderlogik mehr. Theodors Schulformat ist unbekannt; ein echter Test von ihm wird eine
   eigene Story. Kosten: keine.

## Akzeptanzkriterien

1. Ein Lückentext lässt sich auf der Stufe „Wortkasten + Freitext“ abspielen: Die Karte zeigt die deutschen
   Bedeutungen aller Lücken plus die Ablenker, ungeordnet und auf jeder Karte gleich. An der gefragten Lücke steht
   kein Hinweis, das englische Wort wird frei getippt.
2. Das Testergebnis zeigt Kind und Supervisor je Lücke „richtig“, „falsch geschrieben (Lösung …)“ oder „falsch“.
   Gewertet wird binär: Prozent, Bestehen und Münzen sind dieselben wie ohne die neue Kategorie.
3. Am echten Test belegt (Integrationstest): „clinik“, „heiht“ und „femal“ erscheinen als „falsch geschrieben“,
   „racist“ in der Lücke für „racism“ als „falsch“.
4. Eine Lücke mit Satzform („patients“) wertet die Grundform allein („patient“) als falsch, die Satzform als
   richtig.
5. Der KI-Creator erzeugt auf einen Format-Schalter hin einen Lückentext im Schulformat mit Ablenkern und
   Satzformen.
6. Der echte Test vom 11.11.2026 lässt sich als Lückentext nachbauen und als Position mit Takt wöchentlich auf
   der neuen Stufe abspielen (Rollengang als Friedrich).

## Schätzung

**Größe: L.** Die Story ist ein Bündel aus vier Teilen, die je für sich lieferbar sind. Einzeln geschätzt:

| Teil | Entscheidung | Größe | wo |
| --- | --- | --- | --- |
| A · Stufe „Wortkasten + Freitext“ mit Ablenkern | 4, 5 | M (wie B-03: neuer Pfad durch Typ, Karte, Editor) | beides |
| B · Satzform an der Lücke | 6 | S | beides |
| C · „Falsch geschrieben“ im Testergebnis | 2, 3 | S | beides |
| D · Format-Schalter am KI-Creator | 7 | M | backend (Agent) |

Zusammen ist das mehr als eine DB-Etappe wie E6 an Breite, aber ohne deren Datenrisiko. Darum L und nicht
„geteilt“. Teilen nach den vier Zeilen ist möglich und wäre der saubere Weg, falls die Story nicht in einer
Sitzung fertig wird (README → „Teilen und Zusammenlegen“; die Entscheidung liegt beim Menschen).

**`migration: nein`**, nachgesehen:

- Die neue Stufe ist ein weiterer `ClozeStage`-Wert (`backend/Pugling.Api/Models/ClozeEntities.cs:7-19`). Er
  landet in den vorhandenen `StageValue`-Spalten; das Schema ändert sich nicht.
- Ablenker (`ClozeConfig`) und Satzform (`Gap`) liegen im JSON der Übung
  (`backend/Pugling.Contracts/Exercise/ExerciseConfigs.cs:71-86`), nicht in eigenen Spalten.
- „Falsch geschrieben“ wird beim Lesen des Ergebnisses aus `GivenAnswer` und Lösung berechnet
  (`backend/Pugling.Api/Models/StudyPlanEntities.cs:191-192`), nicht gespeichert. Damit gilt es auch rückwirkend
  für alte Versuche.

**`vertragsbruch: nein`**: Alle Vertragsänderungen sind additiv und optional, also nach README kein Bruch.

- `ClozeConfig` bekommt eine Ablenker-Liste, `Gap` ein optionales Feld `Form`.
- `TestItem` (`backend/Pugling.Contracts/Student/TestDtos.cs:34`) und `PracticeCard`
  (`backend/Pugling.Contracts/Student/PracticeDtos.cs:74`) bekommen den Wortkasten als Liste.
- `ItemOutcome` (`TestDtos.cs:73`) bekommt ein Kennzeichen „falsch geschrieben“.
- Alte Payloads bleiben gültig. Neue Felder müssen aber im Frontend-Editor und in `Pugling.Client` ankommen,
  sonst greift beim Schreiben `unknown_field` nicht, sondern das Feld fehlt still.

**Risiken**

- **Anti-Cheat am Wortkasten:** Der Kasten darf die Zuordnung Lücke → Bedeutung nicht verraten. Nicht in
  Lückenreihenfolge, nicht je Karte verschieden, und keine Bedeutung wegen eines Großbuchstabens auffällig. Das
  ist dieselbe Fehlerklasse wie bei der englischen Wortbank (B-487, B-497, B-501 in `BuiltInExerciseTypes.cs:207-256`).
- **Lösungsfeld-Regel:** Der Wortkasten enthält Übersetzungen, keine Lösungen, aber die neue Satzform ist eine. Sie
  darf nur über Creator-/Supervisor-gegatete Actions herausgehen (`ConventionGuardTests`). Der Feldname darf nicht
  `Translation` heißen, sonst schlägt das Tor zu Recht an.
- **Schwelle für „falsch geschrieben“:** Die Grenze ≤ 1 / ≤ 2 ist am echten Test gerechnet, nicht an vielen
  Antworten. Ein Fehlurteil kostet keine Punkte, aber eine falsche Diagnose für den Vater.
- **Lücken ohne `VocabKey`** haben keine Bedeutung für den Kasten. Die Stufe muss sie erkennbar behandeln (Warnung
  im Editor oder Inline-Übersetzung), statt still eine Lücke ohne Eintrag zu zeigen.
- **KI-Creator:** Die Qualität der Ablenker und Satzformen hängt am lokalen Modell. Seit B-460 entscheidet C#, was
  eine Antwort ist; das muss für die Satzform genauso gelten.

**Angriffsplan (Backend zuerst)**

1. **C** zuerst: klein, sofort nützlich für die heutigen Freitext-Tests und unabhängig von den anderen Teilen.
   Abstandsfunktion und Nachbarvergleich am `AnswerGrader` (`backend/Pugling.Api/Services/Shared/AnswerGrader.cs`),
   Kennzeichen an `ItemOutcome`, Anzeige im Testergebnis der Sohn-App (`frontend/src/sohn/SohnTest.tsx`) und in der
   Vater-Sicht (`frontend/src/vater/VaterLernstand.tsx`).
2. **B:** `Gap.Form` im Vertrag, Auflösung in `ExerciseContentResolver.cs:153-159` (Form schlägt Speicherwort, der
   Hinweis bleibt aus dem Speicher), Editor `frontend/src/vater/ClozeTexts.tsx`.
3. **A:** neuer `ClozeStage`-Wert, `StageOptions`, Wortkasten-Facette in `PositionPlayService.CardFacets`
   (`PositionPlayService.cs:314`), Hinweis an der Lücke auf dieser Stufe unterdrücken, Karte in
   `frontend/src/components/ClozePrompt.tsx` und `frontend/src/sohn/SohnTest.tsx:223`, Ablenker im Editor.
4. **D:** Format-Schalter in `backend/Pugling.Agent.Creator/Drafting/ClozeStrategy.cs` (Ablenker aus Wortfamilien
   der Unit, Satzform aus `ClozeMarkup`), danach `ki-creator`-Skill und Agent-`CLAUDE.md` nachziehen.

**Testweg**

- Integrationstest in `Pugling.Api.Tests` mit dem nachgebauten Test vom 11.11. (AK 3, AK 4): Friedrichs Antworten
  einreichen, die drei Kategorien je Lücke prüfen, Prozent unverändert binär (AK 2). Rote Probe je Fall benennen
  (CLAUDE.md): Was macht „clinik“ rot, was „racist“?
- Integrationstest: Die Karte auf der neuen Stufe trägt denselben sortierten Kasten auf jeder Lücke und keinen
  Hinweis (AK 1).
- Vitest für `ClozePrompt` und `SohnTest` (`TestResult.test.tsx`); der `frontend-reviewer` vor der Abnahme, für A–C zusätzlich der
  `pugling-reviewer`.
- `/smoke-test` für den Ablauf Anlegen → Zuweisen → Test → Ergebnis; Agent-Lauf `exam` gegen eine Wegwerf-DB für
  D (AK 5).
- Rollengang als Friedrich an der laufenden App (AK 6), nicht an der echten `pugling.db`, sondern an einer Kopie.

## Verlauf

- **2026-11-13** — angelegt (Quelle: echter Test vom 11.11.2026; Nutzer verlangt dieses Format ausdrücklich, darum P1).
- **2026-11-13** — ausformuliert: Ist-Stand am Code. Die Cloze-Stufe „Übersetzung + Freitext“ trägt freie Eingabe
  und strenge Schreibweise schon. Es fehlen Teilpunkte, ein deutscher Wortkasten mit Ablenkern, gebeugte Formen bei
  Speicher-Lücken und ein Takt. Sieben offene Punkte für die Grill-Runde.
- **2026-11-13** — gegrillt: acht Entscheidungen mit dem Nutzer. Wertung bleibt binär (−½ war
  LRS-Nachteilsausgleich), neue Stufe „Wortkasten + Freitext“ mit Ablenkern, Satzform an der Lücke, wöchentlich
  neue Übung vom KI-Creator.
- **2026-11-13** — geschaetzt: L als Bündel aus vier lieferbaren Teilen (A Stufe M, B Satzform S, C Schreibfehler
  S, D KI-Creator M); keine Migration, kein Vertragsbruch (alles additiv). Reihenfolge C → B → A → D.
- **2026-11-13** — in Arbeit, im Worktree `.claude/worktrees/b-524` auf Branch `b-524-schul-vokabeltest` (der
  Haupt-Checkout war durch die parallele B-485-Sitzung gesperrt: DLL-Sperren, `Pugling.Agent.Creator` kompilierte
  nicht). Als eine Story gebaut, nicht geteilt (Nutzer).
- **2026-11-13** — Fund beim Bauen: **keine** Lücke im Bestand trägt einen `VocabKey`. Der KI-Creator legt Lücken
  inline an (`backend/Pugling.Agent.Creator/Drafting/ClozeStrategy.cs:113`), der Loader ebenso, und der
  Übungs-Editor reicht `vocabKey` nur durch (`frontend/src/vater/exerciseConfig.tsx:243-246`). Ohne Verweis hat der
  deutsche Wortkasten (Entscheidung 4) nichts anzuzeigen. Folge für Teil D: Der Format-Schalter muss `VocabKey` aus
  der Wortliste der Unit setzen, nicht nur Ablenker und Satzform.
- **2026-11-13** — Teil C gebaut: `AnswerGrader.IsMisspelled`, Kennzeichen `misspelled` an `ItemOutcome` und
  `PreviewItemOutcome`, Anzeige in Sohn-Testergebnis und Vater-Testmodus. `SchulVokabeltestTests` 15/15 grün; rote
  Proben: ohne Nachbarvergleich fallen `Nachbarwort_Ist_Kein_Schreibfehler` und der API-Weg, ohne Vertauschung nur
  `Vertauschte_Buchstaben_Zaehlen_Als_Ein_Fehler`.
- **2026-11-13** — Teil B gebaut (`d44dd57`): `Gap.Form` ist die Lösung, wenn gesetzt, auf jedem Pfad
  (`ClozeGaps.SolutionOf` für Provider, Resolver und Grader); Editor zeigt bei Speicherlücken Verweis und Satzform.
  Nebenfund behoben: Die Editor-Prüfung verlangte eine Inline-Lösung und blockierte damit jede Speicherlücke. Rote
  Proben: Resolver ohne Form → `Satzform_Schlaegt_Grundform_Aus_Dem_Speicher`, Provider ohne Form →
  `Satzform_Gilt_Auch_Ohne_Speicherverweis`.
- **2026-11-13** — Teil A gebaut (`8eb9a04`): Stufe `CueBoxFreeText` (5), `ClozeConfig.CueDistractors`,
  `IExerciseType.CueBox`, Facette `CueBox` an `PracticeCard`/`TestItem`/`PreviewItem`, der Hinweis an der Lücke
  entfällt auf dieser Stufe. Frontend: Baustein `CueBox` in Arcade (Üben, Test) und Vater-Testmodus, Ablenker im
  Editor. Rote Proben: Hinweis bleibt → Kasten- und Testkarten-Fall, unsortiert → Kasten-Fall, Testweg ohne Kasten →
  Testkarten-Fall.
- **2026-11-13** — Teil D gebaut (`c7380e9`): `--schultest` am KI-Creator. Einschränkung gegenüber Entscheidung 7:
  „schwache Wörter früherer Units“ fließen noch nicht ein – die Scheibe (`UnitVocabulary.Slice`) nimmt nur schwache
  Wörter **der Unit**. Rote Proben: ohne Regeln → Zurückweisungs-Fall, ohne Satzform und ohne Wortliste im
  Briefing → Erfolgsfall.
- **2026-11-13** — Reviews: `pugling-reviewer` (ein Blocker: `LadenFieldListTests`, Loader kannte `cueDistractors`/`form`
  nicht – behoben in `b4cfe19`; dazu Pflichtwörter im Schultest, Artikel-Toleranz in `IsMisspelled`) und
  `frontend-reviewer` (keine Blocker; Stufe hieß in der Arcade „Hören“, Satzform bei Inline-Lücken unsichtbar,
  Testmodus nur mit Emoji). Alles eingearbeitet in `4b9e1c4`, je mit Test und roter Probe. Offen gelassen (nur
  Anzeige, keine Punkte): Schwelle bei sehr kurzen Alternativen und je Phrase statt je Wort; doppelte Listenwörter
  über mehrere Lücken prüft der Agent nur im Prompt; Listeneinträge mit „/“ oder Klammern sind im Schultest nicht
  erzeugbar.
- **2026-11-13** — Verifikation: `dotnet test Pugling.sln -c Release` **1510/1510** grün, Vitest **868/868**, `tsc -b`
  grün. Rollengang an der laufenden App **über die API** (eigener Server :5390, Wegwerf-DB): Vater legt den Test an,
  sieht im Testmodus vier Stufen inkl. „Wortkasten + Freitext“ und den Kasten mit sieben Einträgen, weist ihn als
  Position mit Wochenziel zu; der Sohn bekommt fünf Karten mit demselben Kasten und ohne Hinweis, Antworten wie
  Friedrich (clinik, heiht → falsch geschrieben; racist für racism → falsch), 2/5 = 40 %, binär. **Der Klick-Rollengang
  in der Arcade fiel aus**: Die Sitzung war in keinem Fenster offen, Eingaben im versteckten Browser-Pane kamen nicht
  an. Beobachtung: „patient“ statt „patients“ zählt nach Entscheidung 3 als „falsch geschrieben“ (fehlendes s),
  fachlich eher ein Formfehler.
- **2026-11-13** — Nicht `abgenommen`: Die Commits liegen auf `b-524-schul-vokabeltest`, noch nicht auf
  `auto/fertigstellung`, und der Rollengang per Klick fehlt (`wartet_auf`).
