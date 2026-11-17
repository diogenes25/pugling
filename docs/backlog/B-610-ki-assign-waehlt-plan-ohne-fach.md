---
tags: [typ/story, status/abgenommen, bereich/agent, rolle/creator, rolle/supervisor]
aliases: [assign nimmt den ersten spielbaren Plan]
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: agent
migration: nein
vertragsbruch: nein
quelle: "Bau von B-539 Teil A im Nachtlauf 2026-11-17 (docs/pm-sitzung-2026-11-17-nachtlauf.md)"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
---

# B-610 · `ki-creator assign` legt Übungen in den ersten spielbaren Plan – mit einem Plan je Fach womöglich ins falsche Fach

Der Konsolen-Agent übernimmt vorhandene Übungen in den laufenden Plan eines Kindes über
`PlanAssigner.ResolvePlanAsync` (`backend/Pugling.Agent.Creator/`), das `FirstOrDefault(IsPlayable)` wählt. Bis B-539
gab es je Kind genau einen spielbaren Plan. Seit B-539 (ein aktiver Plan je Fach) kann ein Kind mehrere haben, und
`assign` legt französische Übungen dann womöglich in den Englisch-Plan – ohne Meldung.

Heute (2026-11-17) hat in der echten DB kein Kind zwei aktive Pläne; der Fehler wäre erst sichtbar geworden, wenn der
Vater einen zweiten Fach-Plan anlegt. Ursprünglich als eigene Story zurückgestellt; der `pugling-reviewer` fand
denselben Fall im Review von B-539, darum im selben Sprint behoben (siehe Verlauf).

## Entscheidungen

1. **Abbrechen statt wählen.** Bei mehr als einem spielbaren Plan verlangt `assign` `--plan-id` und listet die Pläne;
   die naheliegende Alternative – den Plan des Fachs der Übungen nehmen – bleibt bewusst ungebaut (sie wäre eine
   Produktentscheidung über gemischte Fächer). *Autonom gegrillt (`art: Defekt`, Nachtlauf 2026-11-17).*

## Akzeptanzkriterien

1. Zwei spielbare Pläne verschiedener Fächer, kein `--plan-id` ⇒ Fehlbedienung (Exit 2) mit beiden Plan-Ids, keine
   Position in einem der Pläne. *Rot mit `FirstOrDefault(IsPlayable)`.*
2. Genau ein spielbarer Plan ⇒ unverändert (Bestandstests in `AssignAgentTests`).

## Verlauf

- **2026-11-17** — angelegt aus dem Bau-Bericht von B-539 Teil A (Nachtlauf 2026-11-17).
- 2026-11-17 · Im selben Sprint behoben (Nachtlauf-Freigabe 3: Review-Fund im Increment, autonom gegrillt, `art: Defekt`).
  **Entscheidung (autonom):** bei mehr als einem spielbaren Plan bricht `assign` ohne `--plan-id` mit `AgentUsageException`
  ab und listet die Pläne (Id, Titel, Fach-Id); es wählt **nie** nach Fach. Begründung: richtig herstellen heißt hier,
  nicht still in einen Plan zu schreiben; eine Fach-Zuordnung wäre eine Produktentscheidung. Genau ein spielbarer Plan
  bleibt wie bisher. Test `AssignAgentTests.cs:172`; A9 (`if (playable.Count > 1)` → `if (false)`) → rot.
- 2026-11-17 · abgenommen in Sprint 84 (30711266): Rollengang `frontend/e2e/zwei-faecher.spec.ts` in der vollen Suite
  **168 passed** gegen frisch gestarteten Server (Ausweichports 5201/5174); `dotnet test` 1795, Vitest 1160; Mutanten-Tabelle
  im Verlauf von B-539. Protokoll `docs/pm-sitzung-2026-11-17-nachtlauf.md`.
