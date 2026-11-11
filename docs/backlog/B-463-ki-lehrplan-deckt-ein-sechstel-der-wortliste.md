---
tags: [typ/story, status/in-arbeit, bereich/training, rolle/creator, rolle/supervisor]
aliases: [KI-Lehrplan zu dünn, Positions-Vorgaben des Agenten]
status: in-arbeit
prio: P1
art: Defekt
groesse: S
wo: "backend"
migration: nein
vertragsbruch: nein
quelle: Rollengang am Access-Lehrwerk 2026-11-10 (Kopie der pugling.db, Sohn im Handy-Format 375×812, Vater-Web)
entgangen_bei: []
grund: ""
ersetzt_durch: []
---

# B-463 · Der KI-Lehrplan übt 20 von 124 Wörtern der Unit, ohne Karteikasten und nur Englisch → Deutsch

Lehrplan 3 („Lehrplan: Unit 1 This is London“, Lauf vom 2026-11-10 23:16) ist das Ergebnis nach dem Umbau aus
[B-460](B-460-ki-aufgaben-programm-entscheidet.md). Stand in der Datenbank:

- Zwei Vokabel-Positionen mit je 10 Wörtern (Übungen 26 und 27). Die Unit hat 124 Wörter. Bei 14 Tagen Laufzeit
  bleiben 104 Wörter der Unit ungeübt, obwohl in zehn Tagen der Vokabeltest ansteht.
- `useLeitner: false`, `newItemsPerDay: 0`, `requireTypedTest: false`: kein Karteikasten, keine Tagesdosis
  (B-04), und der Test ist eine Selbsteinschätzung.
- Richtung `front-to-back`: Das Kind sieht das englische Wort und nennt das deutsche. Für einen Vokabeltest
  der Klasse 7 zählt die Gegenrichtung mit richtiger Schreibung.
- Ein Lückentext, keine Position zu question tags, obwohl das Profil sie als neuen Stoff der Unit nennt.
- Der Plan ist inaktiv (Absicht: „Bitte prüfen und dann aktivieren“). Daneben liegt der ältere Plan 2 mit
  gleichem Titel. Der Sohn sieht nach dem Anmelden keinen Plan (`GET student/study-plans` → `[]`).

Die von Hand geladenen Übungen der Unit (13–23: vier Wortlisten mit 124 Wörtern in beide Richtungen, zwei
Zuordnungen, drei Lückentexte, Grammatik, Übersetzung) hängen an keinem Plan. Erst ein von Hand gebauter Plan
auf der Kopie machte sie spielbar.

Zu klären: Soll der Agent bei vorhandener Wortliste die bestehenden Übungen der Unit einplanen (ganze
Wortliste, Karteikasten, Tagesdosis so, dass sie bis zum Planende durch ist) statt neue Zehner-Scheiben zu
erzeugen? Welche Vorgaben setzt er an Vokabel-Positionen?

## User Story

Als Vater möchte ich mit einem `plan`-Lauf des KI-Creators einen Lehrplan bekommen, der die **ganze** Wortliste
der Unit mit Karteikasten, Tagesdosis und getipptem Test übt, damit mein Sohn bis zum Vokabeltest jedes Wort
gesehen hat und ich den Plan nach dem Prüfen nur noch einschalten muss.

## Ist-Stand am Code

Nachgeprüft am 2026-11-11 an einer Kopie der aktuellen `pugling.db` (die echte blieb unangetastet). Die Zahlen
der Story stimmen weiter, der Befund ist sogar doppelt da:

- Plan 2 gibt es nicht mehr (B-464 hat aufgeräumt). Dafür liegen **zwei** inaktive KI-Pläne für Kind 1 da:
  Plan 3 (Übungen 26, 27, 28) und Plan 4 (Übungen 29–32, Lauf 23:38). Jede Vokabel-Position übt genau 10 Wörter,
  alle mit `UseLeitner = 0`, `NewItemsPerDay = 0`, `RequireTypedTest = 0`, `Stage = null`. Kind 1 hat keinen
  aktiven Plan.
- Die Buch-Wortlisten 13–16 (41 + 44 + 18 + 21 = 124 Einträge, `direction: both`, `ExecutePublic = 1`) hängen an
  keinem Plan.

Woher das kommt:

- `PlanPlanner` erzeugt für **jeden** Typ in `--types` neu, `--per-type` Stück
  ([PlanPlanner.cs:150](../../backend/Pugling.Agent.Creator/PlanPlanner.cs)); jede Vokabelübung nimmt die
  nächste Scheibe der Wortliste (`Part = n - 1`, [PlanPlanner.cs:161](../../backend/Pugling.Agent.Creator/PlanPlanner.cs)),
  die Scheibe ist `--count` groß (Vorgabe 10, [AgentCommands.cs:16](../../backend/Pugling.Agent.Creator/AgentCommands.cs)).
  Zwei Teile × 10 = 20 Wörter – das ist die Story-Zahl, kein Zufall.
- Die Positionen lassen `Stage`, `RequireTypedTest` und `UseLeitner` leer
  ([PlanPlanner.cs:212–219](../../backend/Pugling.Agent.Creator/PlanPlanner.cs)) und setzen keine Tagesdosis.
  Der Server erbt dann die Vorgaben der Übung
  ([PlanPositionsController.cs:192–193](../../backend/Pugling.Api/Controllers/Supervisor/PlanPositionsController.cs)),
  und die sind bei erzeugten wie bei Buch-Übungen `false`/`0`.
- Erzeugte Vokabelübungen fragen fest `front-to-back`
  ([VocabularyStrategy.cs:139](../../backend/Pugling.Agent.Creator/Drafting/VocabularyStrategy.cs)).
- Die Wortliste der Unit liest der Agent schon heute aus den Buch-Übungen und lässt eigene aus
  ([BriefingBuilder.cs:124–137](../../backend/Pugling.Agent.Creator/Briefing/BriefingBuilder.cs)) – er kennt die
  Buch-Übungen also, nutzt sie aber nur als Wortvorrat.
- Der Plan entsteht bewusst inaktiv ([PlanPlanner.cs:198](../../backend/Pugling.Agent.Creator/PlanPlanner.cs),
  Regel aus B-19, festgehalten in `backend/Pugling.Agent.Creator/CLAUDE.md`).
- Die API trägt schon alles, was fehlt: `newItemsPerDay` braucht `useLeitner`
  ([PlanPositionsController.cs:101–103](../../backend/Pugling.Api/Controllers/Supervisor/PlanPositionsController.cs)),
  `stage` und `requireTypedTest` sind setzbar. **Kein Backend-Bau nötig** – API-First heißt hier: der Agent
  benutzt, was die API schon kann.
- Richtung `both` heißt im Server **abwechselnd**: Eintrag 1 Englisch → Deutsch, Eintrag 2 Deutsch → Englisch
  usw., je Wort genau eine Richtung
  ([ExerciseContentProvider.cs:77–82](../../backend/Pugling.Api/Services/Shared/ExerciseContentProvider.cs)).

## Die echte Lücke

Der Agent plant Wortschatz, den es im Katalog schon vollständig gibt, ein zweites Mal in Zehnerstücken – und gibt
den Positionen keine der Einstellungen, die eine lange Wortliste erst lernbar machen. Die Buch-Übungen tragen die
ganze Liste, nur nimmt sie kein Plan auf.

Nicht Teil der Lücke dieser Story (siehe Entscheidungen 3–5): die Richtung je Wort, die fehlende Position zu
question tags und das Einschalten.

## Offene Punkte

Als `art: Defekt` vom Agenten entschieden (Freigabe des Nutzers vom 2026-11-11 für diese Story).

1. ~~Buch-Übungen einplanen statt neue Scheiben erzeugen?~~ → Entscheidung 1.
2. ~~Welche Vorgaben bekommen Vokabel-Positionen?~~ → Entscheidung 2.
3. ~~Beide Richtungen je Wort?~~ → Entscheidung 3, abgespalten.
4. ~~Question tags fehlen?~~ → Entscheidung 4, abgespalten.
5. ~~Plan aktiv anlegen?~~ → Entscheidung 5, bleibt inaktiv.

## Entscheidungen

1. **Hat die Unit eine Wortliste aus dem Buch, plant der Agent für `Vocabulary` deren Übungen ein, statt neue zu
   erzeugen.** Eingeplant wird jede nicht vom Agenten erzeugte Vokabelübung der Unit, die der Wortliste
   mindestens ein neues Wort beiträgt (dieselbe Faltung wie der Wortvorrat, `VocabKey.Part`). `--per-type` gilt
   für Vokabeln dann nicht; ohne Buchliste bleibt alles wie bisher (erzeugen, Scheiben).
   *Begründung:* Die Buch-Übungen enthalten genau die Paare, die der Agent seit B-460 ohnehin nur noch kopiert –
   eine Kopie in Zehnern ist schlechter als das Original: sie deckt ein Sechstel ab, verliert die Richtung
   `both` und füllt den Katalog mit Dubletten (heute 26, 27, 29, 30). *Kosten:* kein Modellaufruf für Vokabeln
   mehr (ein Gewinn), aber auch keine kindgerechten Merkhinweise des Modells auf diesen Positionen; der Test
   `Zwei_Vokabeluebungen_eines_Lehrplans_ueben_verschiedene_Woerter` dreht sich (B-460s Scheiben gelten nur noch
   ohne Buchliste).
2. **Eingeplante Wortlisten bekommen Karteikasten, Tagesdosis, getippten Test und die Stufe „frei tippen“.**
   `useLeitner: true`, `newItemsPerDay = ⌈Einträge / Laufzeit in Tagen⌉`, `requireTypedTest: true`,
   `stage: 4` (`FreeText`). *Begründung:* Die Tagesdosis ist das Mittel, mit dem B-04 eine große Liste über Wochen
   trägt, und sie verlangt den Karteikasten; „bis Planende durch“ ist die einzige Größe, die der Lauf kennt
   (`--days`). Ein Vokabeltest der Klasse 7 wird geschrieben, also zählt nur Getipptes; neue Wörter zeigt die
   Lernrunde zuerst zum Kennenlernen (B-419). *Kosten:* Für Access Unit 1 bei 14 Tagen 3 + 4 + 2 + 2 = 11 neue
   Wörter je Tag plus Wiederholungen – spürbar mehr als 20 Wörter in zwei Wochen, und genau das ist gewollt. Wer
   früher fertig sein will, setzt `--days` auf den Testtermin. Für erzeugte Übungen (ohne Buchliste) bleibt es
   bei den Vorgaben der Übung – deren Länge ist `--count`, eine Dosis darauf wäre Unsinn.
3. **Die Richtung bleibt die der Übung; „jedes Wort in beide Richtungen“ ist eine eigene Story.** Die
   Buchlisten fragen `both`, also abwechselnd – nach dieser Story übt der Plan Deutsch → Englisch für die Hälfte
   der Wörter statt für keines. Jedes Wort in beide Richtungen verlangt eine Entscheidung am Server (Bedeutung
   von `both` oder Richtung an der Position) und ist ein Wunsch, kein Defekt dieses Agenten. *Kosten:* das Ziel
   „beide Richtungen“ ist mit dieser Story nur halb erreicht; abgelegt als [B-472](B-472-jedes-wort-in-beide-richtungen.md).
4. **Die fehlende Position zu question tags wird eine eigene Story.** Die Grammatik-Teile nehmen die Themen der
   Unit in fester Reihenfolge, nicht nach „neu in dieser Unit“; das ist ein eigener Defekt neben dieser Story
   (README: eigene Story, wenn das Ziel der laufenden ohne sie erfüllt ist). *Kosten:* ein zweiter Durchgang;
   abgelegt als [B-471](B-471-ki-lehrplan-grammatik-nach-neuem-stoff.md).
5. **Der Plan bleibt inaktiv.** Die Regel aus B-19 (ungeprüfter KI-Inhalt wird nicht über Nacht zur Pflicht, und
   ein aktiver Plan schaltet den laufenden ab) gilt weiter, denn Lückentexte erzeugt der Agent nach wie vor
   selbst. *Kosten:* ein Klick des Vaters im Vater-Web, bevor der Sohn den Plan sieht.

## Akzeptanzkriterien

1. Ein `plan`-Lauf mit `--types Vocabulary,…` auf einer Unit mit Buch-Wortliste legt **keine** neue Vokabelübung
   an, sondern je Buchliste eine Position; zusammen decken sie die ganze Liste ab.
2. Jede dieser Positionen hat `useLeitner: true`, `requireTypedTest: true`, `stage: 4` und
   `newItemsPerDay = ⌈Einträge / --days⌉`.
3. Eine Vokabelübung, die der Agent selbst erzeugt hat, wird nie als Buchliste eingeplant.
4. Ohne Buchliste verhält sich `plan` wie bisher (erzeugte Scheiben, Vorgaben der Übung).
5. Gemessen an der laufenden App gegen eine Kopie der `pugling.db`: der Plan für Access Unit 1 enthält die
   Übungen 13–16 mit 124 Einträgen und den Vorgaben aus AK 2; nach dem Einschalten liefert
   `GET student/study-plans` für den Sohn diesen Plan.

## Schätzung

**Größe: S** — eine Verzweigung im `PlanPlanner` plus das Weiterreichen der Buchlisten aus dem
`BriefingBuilder`, Anker B-01 (S). `migration: nein` (kein Schema), `vertragsbruch: nein` (nur Agent; die DTOs
`CreatePositionDto` tragen alle Felder schon).

**Risiken:** Execute-Recht – eine fremde Buchliste ohne `ExecutePublic` lehnt der Server mit
`exercise_not_executable` ab; das landet wie jede abgelehnte Position in `FailedPositions` (sichtbar, Exit 1).
Dubletten zwischen Buchlisten – abgefangen durch „trägt mindestens ein neues Wort bei“. Die Wortliste liest
der Agent aus höchstens 50 Übungen der Unit (`take: 50` in `BriefingBuilder`); eine Unit mit mehr Übungen bekäme
still eine unvollständige Liste – schon vorher so, jetzt entscheidet es die Abdeckung des Plans. Eine Liste mit mehr
als 200 × `--days` Einträgen sprengt die Dosis-Grenze des Servers und landet sichtbar als gescheiterte Position.

**Angriffsplan:** (1) Regressionstest in `CreatorAgentTests` schreiben, rot gegen den heutigen Stand.
(2) `BriefingBuilder` gibt die Buchlisten (Id, Einträge) mit dem Briefing heraus. (3) `PlanPlanner` plant sie für
`Vocabulary` ein statt zu erzeugen, mit den Vorgaben aus Entscheidung 2. (4) README/Skill `ki-creator` und die
`CLAUDE.md` des Agenten nachziehen.

**Testweg:** `CreatorAgentTests` (neu: Buchlisten werden ganz und mit Dosis eingeplant; umgebaut:
`Zwei_Vokabeluebungen…`; unverändert grün: `Eigene_KI_Uebungen_und_eine_zu_kurze_Liste_sind_keine_Wortliste` als
AK 3/4), volle Suite über das Test-Tor, `pugling-reviewer`, dann `plan --dry-run` und ein echter `plan`-Lauf
gegen eine API auf einer Kopie der `pugling.db`, Abfrage der Positionen und von `student/study-plans` nach dem
Einschalten.

## Verlauf

- **2026-11-10** — angelegt (Prüfgang am Access-Lehrwerk: Übungen, Vater-Sicht, Handy).
- **2026-11-11** — ausformuliert: Fund an einer DB-Kopie nachgeprüft (jetzt zwei inaktive KI-Pläne, 3 und 4),
  Ursache am Code belegt (`PlanPlanner` erzeugt Scheiben und lässt die Positions-Vorgaben leer).
- **2026-11-11** — gegrillt (autonom, `art: Defekt`, Freigabe des Nutzers): fünf Entscheidungen, zwei
  abgespalten (B-471, B-472).
- **2026-11-11** — geschätzt: S, nur Agent, keine Migration, kein Vertragsbruch.
- **2026-11-11** — in Arbeit, gebaut und verifiziert. Regressionstest
  `CreatorAgentTests.Ein_Lehrplan_plant_die_ganze_Buch_Wortliste_mit_Karteikasten_und_Tagesdosis_ein` war vorher rot
  (am Aufrufzähler des Modells: der Plan erzeugte wieder), ersetzt den Scheiben-Test. Rote Proben gemessen, je an
  ihrer Zeile: Dosis abrunden, nur neue Wörter zählen (Zeile Dosis), Duplikat-Filter weg, Vocabulary doppelt
  (Zeile Positionsliste). Volle Suite 1273/1273 grün (Release). `pugling-reviewer`: kein Blocker; ein 🟡 (die
  Dosis-Zusicherung trug den Kommentar „Überlappungswort zählt mit“ nicht – mit `--days 2` ergaben beide
  Zählweisen 2) behoben über `DurationDays: 3`; Vocabulary doppelt plant die Listen nicht mehr doppelt;
  `take: 50` als Risiko oben notiert.
- **2026-11-11** — Messung an der laufenden App (API auf `:5281` gegen eine Kopie der `pugling.db`, Seed aus):
  `plan --child 1 --series-unit 7 --types Vocabulary --days 14` legt Plan 5 an mit den Übungen 13–16
  (41 + 44 + 18 + 21 = 124 Einträge), je `stage 4`, Karteikasten, getippter Test, Dosis 3/4/2/2; die KI-Scheiben
  26/27/29/30 bleiben außen vor. Nach dem Einschalten als Vater liefert `GET student/study-plans` als Sohn Plan 5
  (`isPlayable: true`), eine Übungsrunde der ersten Position bringt genau 3 neue Karten zum Kennenlernen, die
  zweite in Gegenrichtung. Rollengang damit über die API auf dem Sitz des Sohnes, nicht in der Oberfläche – die
  Story ändert nichts am Frontend. Lückentexte wurden im Lauf nicht erzeugt (das konfigurierte Ollama-Modell
  fehlt auf diesem Rechner); dieser Zweig ist unverändert.
