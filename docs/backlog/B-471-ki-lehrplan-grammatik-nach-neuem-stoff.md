---
tags: [typ/story, status/abgenommen, bereich/training, rolle/creator]
aliases: [Question tags fehlen im KI-Lehrplan]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: B-463
entgangen_bei: []
nachgeschaut: ""
grund: ""
ersetzt_durch: []
---

# B-471 · Die Grammatik-Teile des KI-Lehrplans nehmen die Themen alphabetisch statt den neuen Stoff zuerst

Abgespalten aus [B-463](B-463-ki-lehrplan-deckt-ein-sechstel-der-wortliste.md) (Entscheidung 4). Der `plan`-Lauf
verteilt die Grammatik-Themen der Unit reihum auf Lückentext- und Grammatik-Teile (`PlanPlanner.PartTopic`). Die
Themenliste kommt sortiert nach Bezeichnung aus `GET creator/grammar-topics`
(`GrammarTopicsController.cs:61`), also „Past progressive, Present progressive, Question tags, Simple past,
Simple present“. Bei zwei Lückentexten kommen question tags nie dran, obwohl das Profil sie als neuen Stoff der
Unit nennt; Access Unit 1 hat dafür sogar eine fertige Buch-Übung (19). Nicht in B-463 mitgenommen, weil dessen
Ziel (ganze Wortliste lernbar) ohne diesen Punkt erfüllt ist.

## User Story

Als **Vater** möchte ich, dass ein KI-Lehrplan die Grammatik übt, die in der Unit **neu** ist, **damit** mein Sohn vor
der Klassenarbeit nicht nur Wiederholung bekommt und das Neue nie.

## Ist-Stand am Code

- `BriefingBuilder.cs:83-88`: die Themen-Labels der Unit kommen aus `ListGrammarTopicsAsync` gefiltert auf die Ids der
  Unit – in der Reihenfolge der Liste, und die ist nach Bezeichnung sortiert (`GrammarTopicsController.cs:61`).
- `PlanPlanner.PartTopic` ([PlanPlanner.cs:319](../../backend/Pugling.Agent.Creator/PlanPlanner.cs)) nimmt die Themen
  reihum von vorn – bei zwei Lückentexten also die ersten beiden.
- Die Unit-Reihenfolge der Themen (`grammatik` in `buch.json`) folgt dem Buch, nicht „neu zuerst": bei Access Unit 1
  stehen die neuen Themen hinten.
- Was neu ist, steht nur in der Stoffnotiz `SeriesUnit.Grammar`: der Skill `lehrwerk-aufbereiten` schreibt dort
  „NEU question tags: …", „NEU past progressive: …" (gelesen in `raw/english/access-pugling/buch.json`, alle vier
  Units).

## Die echte Lücke

Die Reihenfolge der Themen trägt keine Bedeutung, wird aber als Priorität gelesen.

## Offene Punkte

1. ~~Woher weiß der Agent, was neu ist?~~ → Entscheidung 1.
2. ~~Wo wird umsortiert – im Planer oder im Briefing?~~ → Entscheidung 2.

## Entscheidungen

1. **Die Markierung `NEU <Label>` in der Stoffnotiz entscheidet** (Groß-/Kleinschreibung des Labels egal, der Marker
   selbst groß). Begründung: das Signal steht schon in jeder aufbereiteten Unit, und ein Feld „neu" am Thema wäre eine
   Schemaänderung für eine Lesart. Die Konvention steht jetzt in `FORMAT.md` des Skills. Gilt das längste Label, das an
   einer Wortgrenze endet. Kosten: eine Unit ohne Marker behält die alphabetische Reihenfolge; ein Thema, das nur
   erwähnt ist, rückt nicht vor; ein Marker mit anderem Wortlaut als das Label greift nicht.
2. **Im Briefing**, nicht im Planer: auch der Prompt nennt die Themen dann neu-zuerst, und Klausur und Einzelübung
   bekommen dieselbe Ordnung. Kosten: keine.

## Akzeptanzkriterien

1. Mit der Access-Notiz (Wiederholung simple past/present progressive, NEU question tags, NEU past progressive) kommen
   „Question tags", „Past progressive" zuerst, der Rest in der gegebenen Reihenfolge.
2. Ohne Notiz oder ohne Marker bleibt die Reihenfolge; eine bloße Erwähnung ohne Marker rückt nicht vor.
3. Das Briefing einer Unit trägt die Labels in dieser Ordnung.

## Schätzung

- **S**, `wo: backend` (KI-Creator), keine Migration, kein Vertragsbruch. Testweg: zwei Tests in `CreatorAgentTests`.

## Verlauf

- **2026-11-11** — angelegt (beim Ausformulieren von B-463).
- **2026-11-12** — ausformuliert, gegrillt, geschätzt (Defekt, autonom); Konvention „NEU <Label>" in `FORMAT.md`.
- **2026-11-12** — in-arbeit. Rote Probe gegen einen Stub, der die Reihenfolge lässt: Regel erwartet „Question tags",
  gemessen „Past progressive"; Briefing erwartet „Zebra neu", gemessen „Afrikanisch alt". Fix: `GrammarOrder`,
  verdrahtet in `BriefingBuilder`. Agent-Tests 107/107.
- **2026-11-12** — Mutanten (`4c52b69 + Arbeitsbaum`, 02:34): Label-Vergleich mit Groß/Klein → rot; jede Nennung statt
  Marker → rot; Briefing reicht keine Notiz durch → rot. (Eine erste Fassung der Briefing-Mutante kompilierte nicht –
  ungemessen, ersetzt.)
- **2026-11-12** — `pugling-reviewer`: kein 🔴. 🟡 ein Label, das Präfix eines anderen ist, griff das falsche Thema
  („NEU present perfect progressive" markierte „Present perfect" – Access Unit 3). **Fund 2 des Sprints 33 (zählt)**:
  längstes Label an einer Wortgrenze; Fall im Regeltest. 🟢 ohne Zählung: `FORMAT.md` sagt jetzt, dass hinter `NEU` das
  Label wörtlich steht und ein Marker je Thema gilt; das überflüssige Sortieren entfernt. 🟢 an den Menschen: in der
  echten Access-Notiz greifen zwei Marker nicht („NEU conditional 2" gegen das Label „Conditional sentences (if-clauses)",
  „NEU the passive" gegen „Passive") – Daten unter `raw/` und in der echten DB, nicht angefasst.
- **2026-11-12** — Mutanten nach der letzten Änderung (`4c52b69 + Arbeitsbaum`, 02:45):

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | Label-Vergleich mit Groß/Klein | GrammarOrder.cs:31 | red | red |
  | jede Nennung statt Marker | GrammarOrder.cs:13 | red | red |
  | kürzestes statt längstes Label | GrammarOrder.cs:33 | red | red |
  | ohne Wortgrenze | GrammarOrder.cs:32 | red | red |
  | Briefing ohne Stoffnotiz | BriefingBuilder.cs:89 | red | red |

  Grundlinie nach dem Zurückspielen grün.
- **2026-11-12** — **abgenommen.** Commit `e94aa1e` (gemeinsam mit B-473). Volle Suite nach der letzten Änderung: Backend 1335/1335 (Agent-Tests 109/109); Frontend unverändert seit dem E2E-Lauf von Sprint 32 (137 passed). Review-Zähler Sprint 33: 2 von 5. **Rollengang (Creator) ohne Live-Lauf:** gegen Ollama und die echte API lief der Agent nicht – die echte DB gehört den Kindern, und ein zweiter Server auf 5200 ist verboten; gefahren hat ihn `CreatorAgentTests` mit `FakeChatClient` gegen die In-Process-API. Ein Mensch prüft: `pugling-creator briefing --child 1 --type Cloze` gegen eine Kopie der DB.
