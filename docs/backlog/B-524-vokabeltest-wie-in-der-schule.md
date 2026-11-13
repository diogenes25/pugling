---
tags: [typ/story, status/ausformuliert, rolle/student, rolle/supervisor]
aliases: [Schul-Vokabeltest, Lückensätze mit deutschem Wortkasten]
status: ausformuliert
prio: P1
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Echter Vokabeltest von Friedrich (Englisch, 11.11.2026), abgeschrieben in raw/english/real_tests/2026-11-11-friedrich-vokabeltest.md (gitignored); Nutzer im Chat 2026-11-13: „Solche Tests soll Friedrich entsprechend öfter erhalten. Die Art des Tests ist mir wichtig.“"
grund: ""
ersetzt_durch: []
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
- **Rechtschreibung zählt halb:** Ist die Bedeutung richtig, das Wort aber falsch geschrieben, gibt es −½ Punkt.
  Ein falsches Wort kostet vermutlich den ganzen Punkt; im Beispieltest kam keins vor.

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

1. **Halber Punkt: neue Wertung oder nur Anzeige?** Soll der Test Teilpunkte vergeben (Note, Prozent und
   Bestehen rechnen mit ½), oder reicht es, die falsch geschriebenen Wörter als eigene Kategorie zu zeigen, während
   die Wertung binär bleibt? *Empfehlung:* echte Teilpunkte im Test, beim Üben (Leitner) aber weiter binär. Leitner
   muss wissen, ob das Wort sitzt, und falsch geschrieben sitzt es nicht. Kosten: ein dritter Ergebnis-Zustand am
   `TestItemResult` (Migration), eine Prozentrechnung mit Gewicht und ein Erkennungsmaß dafür, was „falsch
   geschrieben“ heißt.
2. **Woran erkennt der Server „falsch geschrieben“?** Vorschlag: höchstens ein bis zwei Zeichen Editierabstand
   zur Lösung, abhängig von der Wortlänge, und kein anderes Wort des Wortkastens näher dran. *Empfehlung:*
   Damerau-Levenshtein ≤ 1 bei Wörtern bis 5 Buchstaben, ≤ 2 darüber. Am echten Test prüfen: „clinik“, „heiht“,
   „femal“ müssen ½ bekommen, „racist“ statt „racism“ den vollen Abzug, obwohl der Abstand nur 2 ist. Daran zeigt
   sich: ein reiner Abstand reicht nicht, es braucht den Vergleich mit den anderen Lösungen.
3. **Wortkasten: neues Feld oder neue Stufe?** Entweder eine vierte Cloze-Stufe „Deutscher Wortkasten + Freitext“,
   die die Übersetzungen aller Lücken plus Ablenker ungeordnet zeigt und den Hinweis an der Lücke weglässt, oder ein
   eigener Übungstyp. *Empfehlung:* neue Stufe am bestehenden Typ (CLAUDE.md: kein Parallel-Stack). Ablenker als
   neues Feld `ClozeConfig.CueDistractors` (deutsche Wörter). Kosten: Vertragsfeld, Manifest-Fähigkeit, Editor.
4. **Ein Blatt oder eine Lücke je Karte?** Die Schule zeigt alle Sätze auf einmal. Die Klausur in Pugling fragt
   strikt einzeln, mit gutem Grund (Anti-Cheat, eingefrorene Reihenfolge). *Empfehlung:* einzeln lassen. Der
   Unterschied ändert, was geübt wird, kaum, aber ein Blattmodus wäre ein großer Umbau. Dafür den deutschen
   Wortkasten auf **jeder** Karte vollständig zeigen, damit sich Friedrich wie auf dem Blatt orientiert.
5. **Gebeugte Formen:** Soll eine Lücke mit `VocabKey` eine eigene **Satzform** tragen (z. B. `Gap.Form =
   "debates"`), die die Hauptlösung ersetzt, während der Speicher den Hinweis und die Lernstands-Verknüpfung
   liefert? *Empfehlung:* ja. Sonst bleibt die Wahl zwischen deutschem Hinweis und richtiger Form.
6. **Takt „öfter“:** Wie oft, und woher kommt der Stoff? *Empfehlung:* jede Woche ein Test aus den Wörtern der
   aktuellen Unit plus einem Anteil schwacher Wörter aus früheren Units, erzeugt vom KI-Creator (`exam` mit einem
   Format-Schalter) und als Klassenarbeit bzw. Position mit Pflichtziel zugewiesen. Erst prüfen, ob ein Pflichtziel
   wiederkehrende Tests schon trägt. Wenn ja, ist dieser Punkt reine Inhaltsarbeit und kein Code.
7. **Nur Friedrich oder alle Kinder?** *Empfehlung:* allgemein bauen und je Position per Stufe wählbar machen.
   Theodors Schule prüft vielleicht anders, das Format ist aber keine Friedrich-Sonderlogik.

## Akzeptanzkriterien (Entwurf)

1. Ein Lückentext lässt sich auf einer Stufe abspielen, auf der die Karte einen **deutschen Wortkasten** mit den
   Bedeutungen aller Lücken und zusätzlichen Ablenkern zeigt, ungeordnet und auf jeder Karte gleich. An der
   gefragten Lücke steht kein Hinweis, und das englische Wort wird frei getippt.
2. Im Abschlusstest gibt eine Antwort mit der richtigen Bedeutung und einem kleinen Schreibfehler ½ Punkt, eine
   falsche oder fehlende 0. Am echten Test belegt: „clinik“, „heiht“ und „femal“ bekommen ½, „racist“ in der
   Lücke für „racism“ bekommt 0.
3. Das Testergebnis zeigt Friedrich und dem Supervisor je Lücke „richtig“, „falsch geschrieben (Lösung …)“ oder
   „falsch“.
4. Beim Üben (Leitner) zählt „falsch geschrieben“ weiter als nicht gewusst.
5. Eine Lücke mit `VocabKey` kann eine gebeugte Satzform als Lösung tragen. Die Grundform allein wird dann nicht
   als richtig gewertet.
6. Der echte Test vom 11.11.2026 lässt sich als Lückentext nachbauen und als Klausur abspielen. Mit Friedrichs
   Antworten ergibt er dieselben Abzüge wie beim Lehrer (siebenmal −½).
7. Es gibt einen dokumentierten Weg, Friedrich solche Tests regelmäßig zu stellen (je nach Entscheidung zu Punkt 6
   als Code oder als Ablauf).

## Verlauf

- **2026-11-13** — angelegt (Quelle: echter Test vom 11.11.2026; Nutzer verlangt dieses Format ausdrücklich, darum P1).
- **2026-11-13** — ausformuliert: Ist-Stand am Code. Die Cloze-Stufe „Übersetzung + Freitext“ trägt freie Eingabe
  und strenge Schreibweise schon. Es fehlen Teilpunkte, ein deutscher Wortkasten mit Ablenkern, gebeugte Formen bei
  Speicher-Lücken und ein Takt. Sieben offene Punkte für die Grill-Runde.
