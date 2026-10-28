---
tags: [typ/story, status/abgenommen, bereich/ki-agent, rolle/creator]
aliases: [Kindloser Interessen-Pfad ohne Einkleidung, --interests taggt nur]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
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

## User Story

Als Creator, der eine Katalog-Übung für ein Interessenprofil erzeugt (`--interests` ohne Kind), möchte ich, dass die
Aufgaben wirklich in diese Interessen eingekleidet sind – nicht nur, dass der Katalog es behauptet.

## Ist-Stand am Code

Siehe oben (`BriefingBuilder.cs:72,107-130`, `CreatorBriefing.cs:38` und `:110-112`, `DraftPrompts.cs:61`,
`ExerciseStrategy.cs:75`, Stand vor dem Fix): die Slugs wurden nur zu Ids, der Prompt verlangte „neutrale
Alltagssituationen“.

## Akzeptanzkriterien

1. Ohne Kind im Briefing nennt der Prompt die Labels der mit `--interests` genannten Tags als Einkleidung.
2. Der kindlose Prompt verlangt dann keine „neutralen“ Situationen mehr; ohne `--interests` bleibt er wie bisher.
3. Die Beschreibung der Übung nennt die Einkleidung auch für den gemeinsamen Katalog.
4. Gilt ebenso für `--child X --general --interests`.

## Entscheidungen

1. Ein eigenes Feld `CatalogInterests` statt die Labels in `Child` zu schmuggeln: der kindlose Pfad bleibt kindlos.
   Kosten: ein weiterer Konstruktor-Parameter am Briefing.

## Schätzung

S, `backend` (Agent), keine Migration, kein Vertragsbruch. Testweg: Prompt-Zusicherung über `FakeChatClient.LastMessages`.

## Verlauf

- **2026-10-26** — angelegt aus der Nachschau zu B-46 (sechster Nachtlauf, Sprint 11); Befund statisch am Code
  belegt, Test nicht ausgeführt.
- **2026-10-28** — in Lauf H, Teil 1 ([Protokoll](../pm-sitzung-2026-10-28-nacht.md)) gebaut nach dem Vorschlag der Story. Neues Feld
  `CreatorBriefing.CatalogInterests` (Labels der mit `--interests` genannten Tags, nur ohne Kind im Briefing);
  `Interests` fällt darauf zurück, also nennt `DraftPrompts` die Einkleidung; der kindlose Prompt sagt nicht mehr
  „neutrale Alltagssituationen“, wenn Interessen vorgegeben sind; die Beschreibung der Übung und die Ausgabe von `create`
  nennen sie. **Gemessen:** `KindloserPfad_KleidetDieAufgabenInDieAngegebenenInteressenEin` → **1 failed** (Label fehlt
  im Prompt). Mutanten „neutral immer“, „neu immer“ (Kontrollfall ohne Interessen), „Beschreibung alt“ → je **1 failed**.
  **Review-Fund 🟡 (gezählt, 1):** der Weg `--child X --general --interests` war ungetestet (Mutante `request.ChildId is
  null` statt `dressedChild is null` blieb grün) → Prompt-Zusicherung in
  `KindUndVorgegebeneInteressen_IstEinBedienfehler_MitGeneralAberErlaubt`, Mutante → **1 failed**. 🟢 genommen: Prompt
  ohne eingebettetes `\n`, veralteter Kommentar zu `InterestTagIds`/`Interests`, Ausgabe von `create`, Formatierungsrauschen
  in zwei Fördermodus-Zeilen. Suite **1000/1000 `exit=0`**. Rollengang: Tests sind der Weg. **`abgenommen`.**
- **2026-10-28** — Commit `c195ef2` (Lauf H, Teil 1).
