---
tags: [typ/story, status/abgenommen, bereich/training, bereich/tests]
aliases: [Vier-Wochen-Simulation, Uhr über TimeProvider]
status: abgenommen
prio: P0
art: Aufräumen
groesse: M
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-10-fertigstellung.md
grund: ""
ersetzt_durch: []
---

# B-451 · Die fachliche Uhr läuft über `TimeProvider`, und ein Test spielt vier Wochen

## User Story

Als Entwickler möchte ich einen Test, der ein Kind 28 Kalendertage lang täglich üben lässt, damit „vier Wochen
täglich Vokabeln" eine gemessene Eigenschaft des Produkts ist und keine Behauptung.

## Ist-Stand am Code

`TimeProvider.System` ist registriert (`Program.cs:162`), die Test-Factory ersetzt ihn durch `TestClock`
(`PuglingWebAppFactory.cs:68`). Benutzt wird er nur in `PositionPracticeController.cs:27`. Rund 50 weitere
Stellen in `Controllers/` und `Services/` lesen `DateTime.UtcNow` direkt (gezählt mit
`grep -rc "DateTime.UtcNow" backend/Pugling.Api --include=*.cs`, ohne `Models/`, `Seed`, `TokenService`).

## Die echte Lücke

Kein Test kann den Kalendertag vorstellen. Mehrtages-Regeln (Leitner-Fälligkeit, Malus, Tagesdosis) sind nur
über das Rückdatieren von Zeilen prüfbar – also nicht über den Weg, den das Kind geht.

## Entscheidungen

1. **Injizierter `TimeProvider`, kein statischer Ambient-Clock.** Der Saum existiert schon und ist im Test-Host
   verdrahtet. Kosten: rund 17 Klassen bekommen einen Konstruktor-Parameter.
2. **JWT-Laufzeit bleibt an der Wanduhr** (`TokenService`, `Program.cs`). Sonst müsste die Simulation täglich
   neu anmelden, nur um das Werkzeug zufriedenzustellen. Kosten: eine Token-Regel ist mit der Testuhr nicht prüfbar.
3. **Modell-Vorgabewerte (`= DateTime.UtcNow`) bleiben**; wo Logik einen solchen Stempel als Tag liest, setzt
   die Erzeugungsstelle ihn aus der Uhr.
4. **Die Simulation ist ein Integrationstest über die API in der Rolle des Kindes**, nicht über `dto.Day` des
   Vaters – der Nachtrag-Weg trägt andere Regeln (`BySupervisor`).

## Akzeptanzkriterien

1. Außerhalb von `Models/`, `Data/Seed.cs`, `Auth/TokenService.cs`, `Program.cs` und den Migrationen liest
   keine Stelle der API die Wanduhr direkt.
2. Die Suite ist so grün wie vorher (kein Verhalten geändert).
3. `VierWochenTests` spielt 28 Tage: je Tag Anmeldung, Übungsrunde, Test; es sichert zu, dass jeder Tag eine
   Portion unter einer festen Obergrenze hat, das Tagesziel erreichbar ist, an gespielten Tagen kein Malus
   fällt und ein ausgelassener Tag genau einen kostet.

## Schätzung

**Größe: M.** Risiko: ein übersehener Stempel macht die Simulation an einem Tageswechsel inkonsistent – die
Simulation selbst ist der Test dafür. Testweg: volle Suite, dann `VierWochenTests`.

## Verlauf

- **2026-11-10** — angelegt und entschieden im Fertigstellungs-Lauf (Vollmacht des Nutzers, [Protokoll](../pm-sitzung-2026-11-10-fertigstellung.md)); `in-arbeit`.
- **2026-11-10** — gebaut (Sprint 29): 45 Stellen in 14 Dateien auf den injizierten `TimeProvider` umgestellt (Subagent, danach `pugling-reviewer`: keine vergessene Stelle, kein Singleton mit neuer Abhängigkeit), Buchungs- und Sitzungsstempel ausdrücklich aus der Uhr, `TestClock.FreezeAt`. AK 1 gemessen mit `grep -rnE "DateTime\.UtcNow|DateTime\.Now|DateTimeOffset\.UtcNow" backend/Pugling.Api --include=*.cs | grep -vE "/(Models|Migrations|obj|bin)/|Data/Seed\.cs|Auth/TokenService\.cs|Pugling\.Api/Program\.cs"` → 0 Treffer. AK 2: Suite vor B-04 1187 grün. AK 3: `VierWochenTests.VierWochen_TaeglichEinePortion_…` spielt 28 Tage (120 Wörter, Dosis 8, ein ausgelassener Tag): je Tag ≤ 70 abgefragte Karten, Test 1–60 Wörter und bestanden, Pflicht erfüllt, genau ein Malus.
- **2026-11-10** — offen geblieben und bewusst nicht gebaut: der Zeitfenster-Multiplikator liest `GetLocalNow()` (Zeitzone des Hosts), der Seed datiert aus der Wanduhr; beides stört die Simulation nicht, weil sie keine Punktzahlen zusichert und eigene Pläne anlegt.
- **2026-11-10** — abgenommen. Backend 1211/1211, Vitest 741/741, E2E 134 passed; `pugling-reviewer` gelaufen; Rollengang: `VierWochenTests` ist der Gang des Kindes über 28 Tage. Commit `595b429`.
