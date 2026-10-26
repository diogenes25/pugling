---
tags: [typ/story, status/idee, bereich/ki-agent, rolle/creator]
aliases: [Kindloser Interessen-Pfad ohne Einkleidung, --interests taggt nur]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: Nachschau Sprint 11 (sechster Nachtlauf, docs/pm-sitzung-2026-10-26-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-46]
wartet_auf: ""
---

# B-264 · `--interests` ohne Kind setzt die Tags an die Übung, kleidet ihren Inhalt aber nicht ein

B-46 sollte eine kindlose, aber interessenbasierte Übung liefern. Im kindlosen Pfad landen die aufgelösten
Slugs nur als Ids in `InterestTagIds` (`backend/Pugling.Agent.Creator/Briefing/BriefingBuilder.cs:72,107-130`);
`CreatorBriefing.Interests` ist ohne Kind leer (`CreatorBriefing.cs:38`: `Child?.Interests ?? []`), und der Prompt
sagt im kindlosen Fall ausdrücklich das Gegenteil – „neutrale Alltagssituationen, keine erfundenen Vorlieben“
(`CreatorBriefing.cs:110-112`). `DraftPrompts.cs:61` lässt die Einkleidungszeile darum weg. Gespeichert werden die
Tags trotzdem (`Drafting/ExerciseStrategy.cs:75`): der Katalog behauptet eine Einkleidung, die die Übung nicht hat.

Ursache: die eine Bedingung `Child is null` zieht „kein Kind“ und „keine Interessen“ zusammen.
Warum kein Tor es fing: `CreatorAgentTests.KindloserPfad_TaggtDieAngegebenenSlugs` (`CreatorAgentTests.cs:122-140`)
prüft nur `detail.InterestTagIds`, nie den Prompt (`chat.LastMessages`).

Vorschlag: die Labels der aufgelösten Tags ins Briefing tragen und im kindlosen Zweig als Einkleidung nennen;
Test: `Assert.Contains(chat.LastMessages, m => m.Text.Contains(einhorn.Label))` – heute vermutlich rot (nicht
gemessen, der Nachschau-Prüfer durfte keine Tests fahren). **Nicht in diesem Lauf behoben:** Increment einer
früheren Runde, außerhalb des Sprint-11-Diffs.

## Verlauf

- **2026-10-26** — angelegt aus der Nachschau zu B-46 (sechster Nachtlauf, Sprint 11); Befund statisch am Code
  belegt, Test nicht ausgeführt.
