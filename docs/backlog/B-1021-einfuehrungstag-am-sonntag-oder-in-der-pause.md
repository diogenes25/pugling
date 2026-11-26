---
tags: [typ/story, status/idee, bereich/lehrplan, rolle/student]
aliases: []
status: idee
prio: P1
art: Defekt
unverifiziert: true
quelle: docs/pm-sitzung-2026-11-25-o.md#sprint-191--ziel--umfang
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-1021 · Der verschonte Einführungstag (B-432) kann auf den freien Sonntag oder in eine Pause fallen – der Montag danach kostet Malus

Fund des `pugling-reviewer` im Sprint 191 (B-731), nicht mehr behoben (Fehlerzähler). `IsIntroductionDaySparedAsync`
(`backend/Pugling.Api/Services/Shared/PositionProgressService.cs`, um Z. 839) nimmt als Einführungstag
`max(erster Spieltag, erster Pflichttag)`. Legt der Vater am Sonntag eine Tagespflicht an, ist der Sonntag der Einführungstag –
den überspringt die Abrechnung seit B-731 ohnehin; der Montag, an dem das Kind die neuen Wörter einführt, ist dann nicht mehr
verschont und kostet Malus. Gleiches für eine Position, die in einer Pause entsteht. Reparaturweg laut Review: den
Einführungstag auf den ersten Pflichttag ab diesem Datum vorschieben (`ChildDutyDays` hineinreichen); Test: Position am
Sonntag angelegt, Einführung am Montag, kein Malus.

## Verlauf

- **2026-11-25** — angelegt im Nachtlauf O (Sprint 191) aus dem Review von B-731; der Lauf endete am Fehlerzähler, bevor der
  Fund behoben werden durfte.
