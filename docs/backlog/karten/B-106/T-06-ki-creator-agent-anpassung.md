# T-06 · Wie muss der KI-Creator-Agent angepasst werden?

Status: entschieden     <!-- offen | beansprucht | entschieden -->
Typ: task                <!-- research | prototype | grilling | task -->
Blockiert durch: T-01

## Frage

`BriefingBuilder.ResolveMaterialAsync` (`BriefingBuilder.cs:90-118`) löst Chapter und SeriesUnit heute
getrennt auf und fasst sie nur im Prompt-Text zusammen; `CreatorPipeline.CreateAsync` legt Übungen über
`chapterId` an. Nach der Verschmelzung entfällt die getrennte Chapter-Auflösung — reicht ein reiner
Parameter-Tausch (`SeriesUnitId` statt `ChapterId` durchreichen), oder ändert sich das Briefing inhaltlich
(z. B. entfällt die bisherige Doppelnennung von Chapter- und Unit-Stoff im Prompt)? Betrifft auch
`ExamPlanner` und die künftige Plan-Erzeugung aus B-19.

## Antwort

Mehr als ein reiner Parameter-Tausch, aber erzwungen statt gestaltet: `Chapter` existiert seit dem
Schema-Slice serverseitig nicht mehr, darum konnte `ResolveMaterialAsync` keine getrennte
Chapter-Auflösung mehr haben — die Doppelnennung von Chapter- und Unit-Stoff im Prompt ist ersatzlos
entfallen, nicht bewusst vereinfacht. `ResolveMaterialAsync` (`BriefingBuilder.cs:91-119`) löst heute
nur noch Reihe+Unit auf (`creator.GetSeriesAsync`/`ListUnitsAsync`), `Facts` (Zeile 122-128) kondensiert
sie in `ProfileFacts`. Die CLI trägt neue Flags `--series`/`--series-unit` (vorher `--subject`/`--chapter`)
plus einen neuen `ResolveSubjectIdAsync`-Helfer in `AgentCommands.cs`, weil das Fach jetzt transitiv über
die Reihe läuft statt direkt am Kapitel zu hängen. `ExamPlanner` zieht dieselbe Struktur nach (kein
separater Chapter-Pfad mehr). `CreatorAgentTests.cs` ist vollständig mitgezogen (`FreshChapterAsync` baut
heute Reihe+Unit, ~20 Aufrufstellen umgestellt, der `--chapter`→`--series-unit`-CLI-Test aktualisiert).
Verifiziert: 706/706 Backend-Tests grün, `dotnet build Pugling.sln` sauber. B-19 (Schüler-Profil für den
KI-Lehrplan) trifft damit bereits auf die neue Signatur, kein Nacharbeiten nötig, wenn diese Story startet.

