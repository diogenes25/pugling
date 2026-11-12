---
tags: [typ/story, status/abgenommen, bereich/katalog, rolle/creator]
aliases: [Ablage ohne --series, erste Reihe mit Unit]
status: abgenommen
prio: P2
art: Defekt
groesse: S
wo: "backend"
migration: nein
vertragsbruch: nein
quelle: B-470 (Briefing-Probe beim Bau des Skills lehrwerk-aufbereiten, 2026-11-11)
entgangen_bei: []
nachgeschaut: 2026-11-12
grund: ""
ersetzt_durch: []
---

# B-473 · Ohne `--profile` und `--series` legt der KI-Creator die Übung in der ersten Reihe überhaupt ab, nicht im Buch des Kindes

Probe auf einer frischen Seed-DB mit geladenem Access-Buch: Kind 1 hat Klasse 7, Gymnasium und als Englischbuch
„English G Access G9 – Band 3“ mit Unit 1. `briefing --child 1 --type Vocabulary` ergibt trotzdem „Fach:
Französisch, Unit: Unité 2 – En ville“, mit französischen Wörtern. `profiles --child 1` nennt Access als besten
Treffer.

Ursache, gelesen in `AgentCommands.BuildRequestAsync`/`ResolveLocationAsync`: Ohne `--profile` und
`--series`/`--series-unit` wird die Ablage zur „ersten Reihe mit Unit“ (im Seed „Découvertes 1“), und das Fach
kommt aus dieser Reihe. Erst danach sucht der Agent das Profil zum Kind, aber im falschen Fach. Das Lehrbuch des
Kindes zieht die Ablage nie heran. Das README beschreibt dieses Verhalten als Vorgabe; für einen Aufruf mit
`--child` ist es trotzdem ein stiller Griff ins falsche Regal.

Umgangen im Skill `lehrwerk-aufbereiten`: Der Aufruf nennt immer `--profile`, `--series-unit` und `--unit`.
Naheliegende Abhilfe: Mit `--child` und ohne Ortsangabe gilt das bestpassende Profil zum Kind (ohne Fachfilter)
mit der aktuellen Unit seines Buchs in dessen Reihe; sonst eine Fehlbedienung statt der ersten Reihe.

Nicht in dieser Sitzung gebaut, weil eine andere Sitzung gerade `BriefingBuilder.cs` und `CreatorAgentTests.cs`
umbaut (B-472).

## User Story

Als **Vater** möchte ich mit `pugling-creator create --child <id>` eine Übung für das Buch meines Kindes bekommen,
**damit** sie in seiner Unit liegt und nicht als französische Übung in irgendeiner ersten Reihe.

## Ist-Stand am Code

- `AgentCommands.BuildRequestAsync`/`ResolveLocationAsync` (Stand vor dem Fix): ohne `--profile`, `--series`,
  `--series-unit` die erste Reihe mit Unit, Fach aus dieser Reihe; danach sucht das Briefing das Profil **im falschen
  Fach** (`BriefingBuilder.cs:239`, `MatchProfilesAsync(childId, request.SubjectId)`).
- `profiles/match?childId=` rangiert Reihe 8 > Fach 4 > Klassenstufe 2 > Schulart 1 – die Reihe des Kinder-Buchs
  gewinnt.
- Das Lehrbuch des Kindes (`TextbookResponse.SeriesId`/`CurrentUnitId`) liest heute nur das Briefing (für den Stoff),
  nie die Ablage.

## Die echte Lücke

Die Ablage fällt vor dem Briefing und kennt das Kind nicht.

## Offene Punkte

1. ~~Welches Profil, wenn mehrere passen?~~ → Entscheidung 1.
2. ~~Was, wenn keines eine Reihe kennt?~~ → Entscheidung 2.

## Entscheidungen

1. **Mit `--child` und ohne Ortsangabe gilt das bestpassende Profil auf der Reihe eines Kinder-Buchs** (`series_match`,
   ohne Fachfilter; zwei gleich gute verschiedene Reihen sind eine Fehlbedienung mit Liste), und die Übung liegt
   in der aktuellen Unit des Kinder-Buchs in dieser Reihe, sonst in deren erster Unit. Das Profil wird als `--profile`
   weitergereicht, damit Briefing und Ablage dasselbe meinen.
2. **Kein Profil mit Reihe → Fehlbedienung** (Exit 2) mit dem Hinweis auf `--profile`/`--series` – statt eines
   geratenen Regals. Kosten: ein Aufruf, der früher „irgendwo" anlegte, bricht jetzt ab.
3. Mit `--profile` ohne Reihe bleibt es wie bisher (Reihe des Profils, erste Unit) – nicht Teil des Funds.

## Akzeptanzkriterien

1. Kind mit Buch in Reihe R (aktuelle Unit 2) und passendem Profil auf R: `create --child` ohne Ortsangabe legt in
   Unit 2 an, nicht in Unit 1 und nicht in einer anderen Reihe.
2. README und Hilfetext nennen die neue Vorgabe.

## Schätzung

- **S**, `wo: backend` (KI-Creator), keine Migration, kein Vertragsbruch. Testweg: `CreatorAgentTests`, volle Suite.

## Verlauf

- **2026-11-11** — angelegt (beim Bau des Skills `lehrwerk-aufbereiten`).
- **2026-11-12** — ausformuliert, gegrillt, geschätzt (Defekt, autonom).
- **2026-11-12** — in-arbeit. Rote Probe `Ohne_Ortsangabe_landet_die_Uebung_im_Buch_des_Kindes_bei_seiner_Unit`:
  erwartet eine Übung in Unit 2, gemessen keine (sie lag in der ersten Reihe des Katalogs). Fix in `AgentCommands`
  (`BestProfileForChildAsync`, `SupervisorApi` als neue Abhängigkeit), README und Hilfetext nachgezogen.
- **2026-11-12** — Mutanten (`4c52b69 + Arbeitsbaum`, 02:34): ohne den Kind-Zweig → rot; erste Unit statt der des
  Kindes → rot.
- **2026-11-12** — `pugling-reviewer`: kein 🔴. 🟡 der Match liefert auch Profile, die nur nach Klassenstufe und Schulart
  passen – ohne Buch mit Profil hätte der Agent eines davon genommen (das geratene Regal); bei zwei gleich guten Büchern
  entschied die Id. **Fund 1 des Sprints 33 (zählt)**: nur Treffer mit `series_match` zählen, Gleichstand verschiedener
  Reihen ist eine Fehlbedienung mit Liste; zwei Tests (`Ohne_Profil_zum_Buch_…`, `Zwei_gleich_gute_Buecher_…`). 🟢 ohne
  Handlung: `--unit` ohne `--series-unit` legt in der aktuellen Unit ab, auch wenn der Stoff aus einer anderen Unit
  derselben Reihe kommt.
- **2026-11-12** — Mutanten nach der letzten Änderung (`4c52b69 + Arbeitsbaum`, 02:45):

  | Mutante | Stelle | erwartet | gemessen |
  |---|---|---|---|
  | ohne den Kind-Zweig | AgentCommands.cs:430 | red | red |
  | erste Unit statt der des Kindes | AgentCommands.cs:434 | red | red |
  | jedes Profil mit Reihe zählt | AgentCommands.cs:482 | red | red |
  | Gleichstand entscheidet die Id | AgentCommands.cs:490 | red | red |

  Grundlinie nach dem Zurückspielen grün.
- **2026-11-12** — **abgenommen.** Commit `e94aa1e` (gemeinsam mit B-471). Volle Suite nach der letzten Änderung: Backend 1335/1335 (Agent-Tests 109/109); Frontend unverändert seit dem E2E-Lauf von Sprint 32 (137 passed). Review-Zähler Sprint 33: 2 von 5. **Rollengang (Creator) ohne Live-Lauf:** gegen Ollama und die echte API lief der Agent nicht – die echte DB gehört den Kindern, und ein zweiter Server auf 5200 ist verboten; gefahren hat ihn `CreatorAgentTests` mit `FakeChatClient` gegen die In-Process-API. Ein Mensch prüft: `pugling-creator briefing --child 1 --type Cloze` gegen eine Kopie der DB.
- **2026-11-12** — Nachschau (Sprint 34, frischer `pugling-reviewer` an `e94aa1e`): alle vier Verben über `BuildRequestAsync`, `series_match` nur auf Kinder-Büchern bestätigt. **Durchrutscher:** `--child` mit `--series` ohne `--series-unit` rät die Unit; zwei Fächer des Kindes ohne exakten Gleichstand wählen still; der Fehlbedienungs-Test unterscheidet die zwei Meldungen nicht → [B-481](B-481-ki-creator-raet-unit-und-fach-beim-kind.md). 🟢 → [B-482](B-482-nachschau-reste-sprint-33.md).
