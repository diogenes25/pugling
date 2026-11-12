---
tags: [typ/story, status/abgenommen, bereich/werkzeug, rolle/creator]
aliases: [series_without_subject im Agenten, Reihe ohne Fach roher 400]
status: abgenommen
prio: P2
art: Defekt
groesse: "S"
wo: "backend"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-nachtlauf.md (Sprint 47, gefunden beim Testen von B-506)
unverifiziert: false
entgangen_bei: []
grund: ""
ersetzt_durch: []
nachgeschaut: 2026-11-13
---

# B-508 · Konsole: eine Reihe ohne Fach endet bei `create`/`plan`/`exam` erst am rohen 400 des Servers

Gefunden beim Bauen von B-506 (Sprint 47), bewusst nicht dort gebaut – das Sprint-Ziel („die Konsole wählt nur, was das
Briefing annimmt") hält ohne.

- `BuildRequestAsync` (`backend/Pugling.Agent.Creator/AgentCommands.cs`, „The exercise's subject …") nimmt für eine Reihe
  ohne Fach das Fach des Profils (B-488); das Briefing (`ProfileFit.Problem`) nimmt das Profil an.
- Der Server lehnt jede Übung in einer Reihe ohne Fach ab: `ExerciseControllerBase.cs:349`, `series_without_subject`.
- Folge: `create` läuft durch Briefing und Modellaufruf und endet dann an einer `PuglingApiException` (400) statt an einer
  Fehlbedienung mit Ausweg („setze ein Fach im Vater-Web"). Gemessen im ersten Lauf von
  `CreatorAgentTests.Reihe_ohne_Fach_nimmt_den_Fachlehrer` (vor dem Umstellen auf `briefing`). Das gilt schon vor
  B-506 für den gewöhnlichen Fall (Buchfach = Profilfach auf einer fachlosen Reihe); die bestehenden Tests
  (`Nur_mit_Kind_in_einer_Reihe_ohne_Fach_spricht_der_Fachlehrer`) weichen auf `briefing` aus.

Frage fürs Ausformulieren: die Fehlbedienung vor dem ersten Modellaufruf für jedes schreibende Verb (wie `--types`,
B-313), während `briefing`/`--dry-run` weiter das Profilfach zeigen dürfen – oder der Fallback auf das Profilfach
entfällt ganz, weil er nur noch für nicht schreibende Verben trägt.

## Ist-Stand am Code

- `ExerciseControllerBase.cs:347-349`: `Create` lehnt jede Übung in einer Reihe ohne Fach mit `series_without_subject` ab.
- `AgentCommands.cs:532` (`BuildRequestAsync`): `ProfileFit.SubjectIdOf(series, profile)` nimmt auf einer fachlosen Reihe
  das Profilfach; nur wenn beide fehlen, endet die Konsole mit „hat kein Fach zugeordnet – setze eines im Vater-Web" –
  ohne Rücksicht darauf, ob die Reihe dem Konto gehört (`TextbookSeriesResponse.IsOwn`).
- `create`, `plan`, `exam` rufen `BuildRequestAsync` und schreiben danach; `briefing` schreibt nie.

## Entscheidungen

Autonom gegrillt (`art: Defekt`, Backlog-Lauf 2026-11-12, Nutzerrolle übertragen):

1. **Jedes schreibende Verb (`create`, `plan`, `exam`) endet an einer fachlosen Reihe vor dem ersten Modellaufruf mit
   einer Fehlbedienung – auch mit `--dry-run`.** Ein Trockenlauf sagt voraus, was der echte Lauf tut; einer, der
   durchläuft, wo der echte scheitert, wäre eine falsche Zusage. Kosten: ein Trockenlauf auf einer fachlosen Reihe zeigt
   nichts mehr – das zeigt weiter `briefing`.
2. **`briefing` behält den Rückfall auf das Profilfach** (B-488/B-506): es zeigt, was der Agent wüsste, und legt nichts
   an. Die Regel des Servers gilt dort nicht, weil dort nichts beim Server ankommt.
3. **Die Meldung nennt den Ausweg nach dem Recht des Lesers** ((CZ)): gehört die Reihe dem Konto, „setze ein Fach im
   Vater-Web (Lehrwerke)"; sonst „bitte den Eigentümer darum oder wähle mit --series eine Reihe mit Fach". Dieselbe
   Meldung auch für den bisherigen Fall „weder Reihe noch Profil hat ein Fach" – eine Stelle für den Satz.
4. Die Buchwahl (`SeriesOfChildsBookAsync`, Mehrfachliste) bleibt beim Prädikat des Briefings; ein fachloses Buch endet
   jetzt sofort an der Fehlbedienung. Bewusst nicht mitgebaut.

## Akzeptanzkriterien

1. `create`, `plan`, `exam` auf einer eigenen fachlosen Reihe mit passendem Fachlehrer: `AgentUsageException` mit
   „kein Fach" und „Vater-Web", kein Modellaufruf, nichts angelegt.
2. Dasselbe mit `--dry-run`: gleiche Fehlbedienung.
3. `briefing` auf derselben Reihe läuft weiter durch (Exit 0).
4. Fachlose Reihe eines anderen Kontos: die Meldung rät nicht zum Vater-Web, sondern nennt `--series`; eine Reihe des
   Grundbestands (kein Eigentümer) schickt auch zu keinem Eigentümer (Review).

## Schätzung

**Größe: S** — eine Bedingung in `BuildRequestAsync`, ein Parameter, ein Meldungs-Helfer, Tests in `CreatorAgentTests`.
Angriffsplan: `BuildRequestAsync(…, writes)` → Helfer `NoSubject(series)` → Tests. Testweg: `CreatorAgentTests`
gegen den In-Process-Server, Modell gefälscht; Mutanten-Probe je AK.

## Verlauf

- **2026-11-12** — angelegt (Sprint 47, beim Testen von B-506).
- **2026-11-12** — ausformuliert, autonom gegrillt (Defekt), geschätzt S (Tageslauf, Sprint 48).
- **2026-11-12** — gebaut (Sprint 48). `BuildRequestAsync(…, writes)`; die Meldung liegt als `ProfileFit.NoSubject` an
  einer Stelle (drei Zweige: eigene Reihe, Grundbestand, fremde Reihe). Tests in `CreatorAgentTests` (B-508-Block):
  - AK 1/2 → `Schreibendes_Verb_endet_an_einer_Reihe_ohne_Fach_vor_dem_Modell` (Theory: create, create --dry-run, plan,
    plan --dry-run, exam, exam --dry-run; `chat.Calls == 0`); M1 (Bedingung aus), M4/M5/M6 (je Verb `writes: false`) → rot.
  - AK 3 → `Briefing_laeuft_auf_einer_Reihe_ohne_Fach_weiter`; M2 (`briefing` mit `writes: true`) → rot.
  - AK 4 → `Fremde_Reihe_ohne_Fach_raet_nicht_zum_Vater_Web`, `Grundbestands_Reihe_ohne_Fach_nennt_nur_eine_andere_Reihe`;
    M3 (`IsOwn` immer wahr) → rot an beiden, M7 (Grundbestand-Zweig aus) → rot.
  - Mutanten-Probe nach der letzten Änderung (Debug, gefiltert auf `CreatorAgentTests`): 7/7 rot, je am genannten Test,
    Grundlinie danach grün. Volle Suite `dotnet test Pugling.sln -c Release`: 1428/1428.
  - Review (`pugling-reviewer`): kein weiterer Pfad legt Übungen auf einer fachlosen Reihe an (einzige Stelle
    `ExerciseStrategy.cs:159`); 🟡 Grundbestands-Reihe bekam „bitte ihren Eigentümer" → behoben (dritter Zweig + Test);
    🟢 Testname versprach mehr als geprüft → umbenannt; 🟢 `plan --dry-run` fehlte → ergänzt.
  - Rollengang Creator: jeder AK als Konsolenaufruf gegen den echten In-Process-Server (Modell gefälscht). Vater, Sohn:
    Regressionszeugen, Suite grün, kein Pfad ihrer Ebenen im Diff. Abgenommen.
- **2026-11-13** — nachgeschaut (Retro Sprint 49, frischer `pugling-reviewer` an `7c17e07`, gelesen). Geprüft: Serverregel
  `ExerciseControllerBase.cs:347-349` gegen `AgentCommands.cs:535` – dasselbe Feld; einzige Ablage `ExerciseStrategy.cs:159`,
  `GenerationRequest` nur an einer Stelle gebaut; „niemand kann sie ändern" gegen `AuthAccess.cs:90-91` und die
  Owner-Prüfungen in `TextbookSeriesController`/`SeriesUnitsController` nachgerechnet – wahr; „Reihe bearbeiten" existiert
  (`VaterLehrwerke.tsx:353-367`). Funde → [B-511](B-511-gebundenes-profil-reihe-ohne-fach-ausweg-im-kreis.md): 🟡 ein an
  die fachlose Reihe gebundenes Profil macht „--series" zum Kreis; 🟢 Hilfetext nennt die Fachregel verkehrt.
