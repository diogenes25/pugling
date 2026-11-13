---
tags: [typ/story, status/abgenommen, bereich/ki, rolle/creator]
aliases: [Nachschau-Reste Sprint 33, doppeltes Grammatik-Label verschwindet]
status: abgenommen
prio: P3
art: Defekt
groesse: "S"
wo: "backend"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-backlog-lauf.md (Nachschau Sprint 33)
unverifiziert: true
entgangen_bei: []
grund: ""
ersetzt_durch: []
---

# B-482 · Nachschau-Reste Sprint 33: Kleinigkeiten an Ablage und NEU-Marker

Drei 🟢 der Nachschau von Sprint 33 (frischer `pugling-reviewer`, gelesen, nicht nachgestellt), gesammelt, damit
sie nicht verloren gehen:

1. **Doppeltes Label verschwindet** (`GrammarOrder.cs:39`): eindeutig ist nur `(SubjectId, Slug)`. Tragen zwei
   Themen dasselbe markierte Label, fallen beide aus dem Rest, aber nur eines steht vorn – eines fehlt im Briefing.
2. **Andere Trenner nach `NEU`** (`GrammarOrder.cs:13`): `NEU – question tags` oder `NEU „question tags"` greifen
   still nicht; `FORMAT.md` nennt nur „Label wörtlich", keine Trenner.
3. **Zwei Bücher derselben Reihe** (`AgentCommands.cs:434`): `FirstOrDefault` entscheidet nach Listenreihenfolge;
   dazu eine Zusicherung im Test `Ohne_Ortsangabe_…`, die nur den Ausgangszustand prüft (`Assert.Empty` auf Unit 1).

## Entscheidungen

Autonom gegrillt (`art: Defekt`, Tageslauf):

1. Doppelte Labels: jedes markierte Label nimmt **einen** Eintrag aus dem Rest, nicht alle gleichen.
2. Nach `NEU` dürfen Doppelpunkt, Gedankenstrich und öffnende Anführungszeichen stehen.
3. Zwei Bücher derselben Reihe bei verschiedenen Units: Fehlbedienung mit „--series-unit", statt der Listenreihenfolge.
   Der Hinweis zur Zusicherung `Assert.Empty` in `Ohne_Ortsangabe_…` trägt nicht: sie steht neben `Assert.Single` auf
   der aktuellen Unit, die bei falscher Ablage rot wird – kein Umbau.

## Akzeptanzkriterien

1. Zwei gleich benannte Themen bleiben beide im Briefing. 2. „NEU – …" und „NEU „…"" greifen. 3. Zwei Bücher derselben
Reihe bei Unit 1 und 2: Fehlbedienung, mit `--series-unit` läuft es.

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 33 im Backlog-Lauf).
- **2026-11-13** — ausformuliert, autonom gegrillt (Defekt), geschätzt S (Tageslauf, Sprint 58).
- **2026-11-13** — gebaut (Sprint 58). Review: 🟡 die erweiterte Trenner-Klasse fraß den ersten Buchstaben eines Labels, das
  selbst mit `-`/`'` beginnt („NEU -ing form") → das Label wird an jeder Stelle hinter „NEU" versucht (Zähler 1); 🟢 Kommentar
  zu doppelt markierten Zwillingen.
  - AK 1/2 → `Neuer_Grammatik_Stoff_der_Unit_kommt_zuerst` (Trenner, Zwillinge, „-ing form"); G1, G2, G4 → rot.
  - AK 3 → `Zwei_Buecher_derselben_Reihe_bei_verschiedenen_Units_sind_eine_Fehlbedienung` (Rat ausgeführt: mit
    `--series-unit` läuft es); G3 → rot.
  - Verifikation: `dotnet test Pugling.sln -c Release` 1441/1441; Mutanten G1–G4, N1, N2 6/6 rot nach der letzten Änderung
    (Release, gefiltert).
  - Rollengang Creator: Konsolenaufrufe gegen den echten In-Process-Server (Modell gefälscht); Vater, Sohn
    Regressionszeugen. Abgenommen.
