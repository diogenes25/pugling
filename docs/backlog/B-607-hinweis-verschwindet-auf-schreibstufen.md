---
tags: [typ/story, status/idee, bereich/training, rolle/creator, lerntechnik/vokabeln]
aliases: [Hinweis fehlt bei Fremdwort → Deutsch]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Nachschau Sprint 82 im Nachtlauf 2026-11-17 (docs/pm-sitzung-2026-11-17-nachtlauf.md)"
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-597]
---

# B-607 · Der Editor bietet einen Hinweis an, den die Schreibstufen bei „Fremdwort → Deutsch" still verschlucken

Seit B-597 wird auf jeder Schreibstufe die Lernsprache getippt. Bei Übungen mit der Richtung „Fremdwort → Deutsch"
entsteht die Schreibkarte über `Swap` (`backend/Pugling.Api/Services/Shared/ExerciseContentResolver.cs:122`), und
`Swap` setzt `Hint = null` (`ExerciseContentProvider.cs:127`). Der eigene Hinweis je Zeile und der Artikel-Hinweis
erscheinen damit auf Freitext, Kästchen, Buchstaben ordnen und Diktat nicht mehr – in der echten DB betrifft das 7
Übungen. Der Editor bietet das Hinweis-Feld weiter an, ohne das zu sagen. B-597 hat das in keiner Entscheidung
festgehalten; die zwei Tests, die den Hinweis zusicherten (`PositionTestFlowTests`, `VokabelTippTests`), wurden auf die
Auswahlstufe umgestellt statt den Verlust zu melden.

Das Verschlucken selbst ist wahrscheinlich richtig (der Hinweis war für die deutsche Antwort geschrieben, „beginnt mit
h" passt nicht zu „avoir"). Falsch ist, dass der Creator es nicht erfährt.

## Verlauf

- **2026-11-17** — angelegt aus der Nachschau von Sprint 82 (🟡 des `pugling-reviewer`), `entgangen_bei: [B-597]`.
