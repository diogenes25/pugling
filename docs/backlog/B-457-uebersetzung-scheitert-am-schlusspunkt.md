---
tags: [typ/story, status/abgenommen, bereich/training, rolle/student]
aliases: [Übersetzung ohne Punkt falsch]
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: B-375
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
---

# B-457 · Eine richtige Übersetzung ohne Schlusspunkt wird als falsch gewertet

Gemessen beim Laden des Access-Lehrwerks (B-375, 2026-11-10) im Testmodus (`preview/check`) einer
Translation-Übung: `Excuse me, how can I get to the station` gegen die Lösung `… station?` und
`the museum is on the corner` gegen `The museum is on the corner.` wurden beide als falsch gewertet. Groß-
und Kleinschreibung verzeiht der Vergleich (`StageMechanics.Normalize`), Satzzeichen am Ende nicht. Ein Kind,
das den Punkt weglässt, verliert also Punkte für eine richtige Übersetzung. Umgangen ist es bisher nur in
den Start-Übungen dieses Lehrwerks, mit zusätzlichen Lösungsvarianten ohne Schlusszeichen. KI-erzeugte und
handgeschriebene Übungen haben diese Varianten nicht.

## User Story

Als **Sohn** möchte ich, dass eine richtige Übersetzung auch ohne den Punkt oder das Fragezeichen am Ende als
richtig zählt, **damit** ich Punkte für die Sprache bekomme und nicht für Tipp-Disziplin bestraft werde.

## Ist-Stand am Code

- `StageMechanics.Normalize` ([StageMechanics.cs:35](../../backend/Pugling.Api/Services/Shared/StageMechanics.cs))
  trimmt, senkt auf Kleinbuchstaben, fasst Leerzeichen zusammen und vereinheitlicht den Apostroph (B-440) –
  Satzzeichen bleiben stehen.
- Es ist der **eine** Vergleich aller Text-Antworten: `AnswerGrader.Matches`/`MatchesGap`
  ([AnswerGrader.cs:28-40](../../backend/Pugling.Api/Services/Shared/AnswerGrader.cs)) für Üben, Test und Vorschau
  (Vokabel, Übersetzung, Lückentext), `AnswerChecking.TextMatch`/`EntryMatches`
  ([BuiltInExerciseTypes.cs:476-484](../../backend/Pugling.Api/Exercises/BuiltInExerciseTypes.cs)) für die
  Katalog-Prüfungen (Listen, Grammatik), `DistractorPool` ([StageMechanics.cs:151](../../backend/Pugling.Api/Services/Shared/StageMechanics.cs))
  fürs Entdoppeln der Auswahlknöpfe.
- Der KI-Creator hat eigene, unabhängige `Normalize`-Methoden (Entwurfs-Bereinigung), nicht betroffen.

## Die echte Lücke

Der Vergleich behandelt das Schlusszeichen als Teil der Antwort. Fehlt es, ist eine inhaltlich richtige Antwort
falsch – für jeden Typ, nicht nur Übersetzung.

## Offene Punkte

1. ~~Werden Schlusszeichen grundsätzlich ignoriert?~~ → Entscheidung 1.
2. ~~Gilt das für Cloze und Grammar genauso?~~ → Entscheidung 2.
3. ~~Was ist mit einem *falschen* Schlusszeichen (`?` statt `.`)?~~ → Entscheidung 3.
4. Satzzeichen **im** Satz (Komma in „Yes, I am") – ausdrücklich zurückgestellt: dort trägt das Zeichen manchmal
   Bedeutung, und kein Fund liegt vor.

## Entscheidungen

1. **Schlusszeichen `.` `?` `!` `…` am Ende werden beim Vergleich ignoriert** (auch mehrere, auch mit Leerzeichen
   davor). Begründung: Gemessen wird die Sprache, nicht die Tipp-Disziplin – dieselbe Haltung wie bei Groß-/
   Kleinschreibung und Apostroph (B-440). Kosten: eine Zeile in `Normalize`; eine Übung, die gezielt das
   Schlusszeichen abfragt, kann es nicht mehr verlangen (gibt es im Katalog nicht). Ebenso zählt `3` für eine
   Lücke `3.` (Ordnungszahl, „am 3. Mai") und `usw` für `usw.` – hingenommen, kein Fall im Katalog bekannt.
2. **Für alle Typen**, weil `Normalize` der eine geteilte Vergleich ist (Klassen-Doku von `AnswerGrader`: „one shared
   comparison rule"). Eine Ausnahme je Typ wäre eine zweite Regel. Kosten: keine.
3. **Ein anderes Schlusszeichen ist ebenso egal** – es wird entfernt, nicht verglichen. Eine Frage als Aussage zu
   tippen ist im Wortlaut gleich; die Satzstellung ist das, was zählt. Kosten: „It is." gegen „Is it?" bleibt
   trotzdem falsch, weil die Wörter verschieden sind.
4. **Besteht die Antwort nur aus Schlusszeichen**, bleibt sie, wie sie ist – sonst würde eine Lösung `?` zu leer
   und damit unerreichbar (leer ist nie richtig).

## Akzeptanzkriterien

1. Übersetzung: `the museum is on the corner` gegen `The museum is on the corner.` ist richtig – im Testmodus des
   Vaters (`preview/check`) über die API.
2. Fragezeichen: `Excuse me, how can I get to the station` gegen `…station?` ist richtig.
3. Lückentext (`MatchesGap`) und Listen (`EntryMatches`) folgen derselben Regel.
4. Mehrere Zeichen und Leerzeichen davor (`station ?!`) werden ebenso entfernt.
5. Ein anderes Wort bleibt falsch (`the museum is in the corner.`), ein Komma im Satz wird nicht entfernt.
6. Eine Lösung, die nur aus `?` besteht, bleibt mit `?` lösbar.

## Schätzung

- **Größe XS**, `wo: backend`, keine Migration, kein Vertragsbruch.
- **Risiko:** Entdoppeln der Auswahlknöpfe (`DistractorPool`) – „Hello!" und „Hello" gelten jetzt als dieselbe
  Antwort und werden nicht mehr als zwei Knöpfe angeboten. Das ist gewollt (sonst zwei richtige Knöpfe).
- **Angriffsplan:** rote Probe (Unit-Tests am `AnswerGrader` + Integrationstest `preview/check`), dann
  `Normalize` um das Abschneiden der Schlusszeichen ergänzen.
- **Testweg:** `SchlusszeichenTests` (Unit + ein Integrationstest über die API), volle Suite.

## Verlauf

- **2026-11-10** — angelegt (Fund beim Laden des Lehrwerks in B-375).
- **2026-11-11** — ausformuliert, gegrillt, geschätzt im Backlog-Lauf (art Defekt, Freigabe autonomes Grillen):
  Ist-Stand am Code belegt, vier Entscheidungen, sechs AKs.
- **2026-11-11** — in-arbeit. Rote Probe vor dem Fix (`SchlusszeichenTests`): erwartet 8 rot / 3 grün, gemessen
  8 rot / 3 grün; die drei grünen sind die Schutzfälle von AK 5 und AK 6 (sie fallen erst bei einem Fix, der zu viel
  entfernt). Fix in `StageMechanics.Normalize` (`WithoutClosingMarks`).
- **2026-11-11** — Mutanten (`mutant-probe.py`, zuerst an `abbe8b5 + Arbeitsbaum`): M1 ohne Fix → rot, M2 nur `.` →
  rot, M4 Leer-Schutz weg → rot (AK 6), M5 Komma auch entfernen → rot (AK 5). **M3** (kein Trimmen zwischen den
  Zeichen) war zuerst **grün** – `?!` hat kein Leerzeichen dazwischen, das am Ende schluckt das spätere `Split`;
  Fall `the station ? !` ergänzt, danach rot.
- **2026-11-11** — `pugling-reviewer`: kein 🔴; 🟡 AK 3 nur für die Lücke belegt, nicht für Listen (`EntryMatches`) –
  **Fund 1 des Sprints (zählt)**, behoben mit `Liste_Ohne_Schlusszeichen_Ist_Richtig` (geordnet/ungeordnet, über den
  Katalog-`/check`). 🟢 ohne Zählung behoben: Doku-Satz in `AnswerGrader` (verweist jetzt auf `Normalize`), die
  Frontend-Kopie `normalizeAnswer` (`exerciseConfig.tsx`, Warnung „Lösung fehlt in der Auswahl") folgt jetzt
  Apostroph und Schlusszeichen; Ordnungszahl-Kosten in Entscheidung 1 nachgetragen.
- **2026-11-12** — Frontend-Review (in B-466): die Frontend-Kopie `normalizeAnswer` war ungetestet – Fund 3 des Sprints,
  belegt in `exerciseConfig.test.ts`.
- **2026-11-12** — Mutanten nach der letzten Änderung (Stand `abbe8b5 + Arbeitsbaum`, 01:15):

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | B457-M1-ohne-Fix (fällt u. a. an `Liste_Ohne_Schlusszeichen_Ist_Richtig`) | StageMechanics.cs:37 | red | red |
  | B457-FE-ohne-Schlusszeichen | exerciseConfig.tsx:139 | red | red |

  Grundlinie nach dem Zurückspielen grün. Volle Suite vorher: 1326/1326.
- **2026-11-12** — **abgenommen.** Commit `97daf9f`. Volle Suiten nach der letzten Änderung: Backend 1328/1328, Vitest 748/748, E2E 137 passed. Rollengang (Sprint 31): Sohn über die API-Probe `Vorschau_Wertet_Uebersetzung_Ohne_Punkt_Als_Richtig` und die Liste über den Katalog-`/check`; eine Browser-Runde mit Übersetzung ohne Punkt fuhr keine E2E – der Grader ist serverseitig, die Oberfläche zeigt nur dessen Urteil. Review-Zähler des Sprints 3 von 5.
