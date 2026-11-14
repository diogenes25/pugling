---
tags: [typ/story, status/idee, bereich/frontend, bereich/training, rolle/supervisor]
aliases: [Lernkontrolle ignoriert die Stufe, Selbsteinschätzung zählt trotz Stufe 4]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Nachschau Sprint 74 (Retro-Pflicht), Backlog-Lauf 2026-11-14 – docs/pm-sitzung-2026-11-14-backlog-lauf.md"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-469, B-16]
wartet_auf: ""
---

# B-550 · Die Positionszeile warnt „Selbsteinschätzung zählt“, obwohl die Stufe getippt prüft

Die Lernkontrolle der Positionszeile (B-469) rechnet nur aus Prüfart, „nur getippte Tests“ und dem Manifest-Flag
`selfAssessedTest` des Typs (`frontend/src/vater/PlanPositions.tsx:833`, Regel `lernkontrolleText` `:972-981`); die Stufe
der Position, den Stufenplan und die `defaultStage` der Übung kennt sie nicht. Der Server prüft im Test aber auf der
Tagesstufe `stageSchedule → stage → defaultStage → Typ-Standard` (`backend/Pugling.Api/Services/Shared/PositionPlayService.cs:80`)
und senkt ein Item nie unter die erste geprüfte Sprosse (`TestItemStage`, `:157-165`). Szenario: Der Vater stellt im
Formular, das B-16 um „Stufe“ erweitert hat, eine Vokabel-Position auf eine getippte Stufe (z. B. 4) und lässt „nur
getippte Tests“ aus. Ein Aufdecken mit „Gewusst!“ gibt es in diesem Test nicht – die Zeile zeigt trotzdem gelb
„Test, Selbsteinschätzung zählt“, und die Feldhilfe `lernkontrolle` (`frontend/src/lib/fieldHelp.ts:124-129`) sagt
„Aufdecken und „Gewusst!“ zählen mit“ und rät, den Schalter einzuschalten. Dasselbe gilt für eine Übung, deren Ersteller
eine getippte `defaultStage` vorgibt. Offen für die Ausformulierung: ob `PositionResponse` die `defaultStage` der Übung
mitbringt oder das Frontend sie nachschlagen muss, und was die Zeile bei einem Stufenplan sagt, der erst ab Tag n
getippt prüft.

## Verlauf

- **2026-11-14** — angelegt in der Nachschau zu Sprint 74 (Backlog-Lauf 2026-11-14), Beleg am Code `HEAD`.
