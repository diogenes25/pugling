---
tags: [typ/story, status/idee, bereich/training, bereich/reward, rolle/student, rolle/supervisor]
aliases: [Kennenlern-Tag ohne Malus, Erster Tag einer Position]
status: idee
prio: P3
art: Frage
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: B-419
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-432 · Prüfauftrag: darf der erste Tag einer Position am Tagesziel vorbei ohne Malus enden?

Zurückgestellt aus [B-419](B-419-erstbegegnung-neue-karte-erst-zeigen.md), offener Punkt 4. Am ersten Tag
einer Position sind alle Items neu: das Kind führt sie in Runde 1 ein (0 Punkte), fragt sie in Runde 2 ab und
soll im Test **am selben Tag** 80 % erreichen – sonst kostet das Tagesziel beim Assistenten-Standard
„Normal" 5 Münzen (`VaterWizard.tsx`, Intensität Normal: −5 🪙). Gemessen im Rollengang vom 2026-11-08:
0 Münzen nach Runde 1, 20 % im Test; der Malus wäre um Mitternacht gefallen. Zu entscheiden ist, ob der
**Einführungstag** einer Position als erfüllt gilt, sobald alle Items eingeführt wurden (Empfehlung des PM:
ja, als serverseitige Regel in `PositionProgressService`, ohne neues Feld – „Kennenlernen ist die Pflicht des
ersten Tages"), oder ob der Vater das je Position über den Assistenten („erster Tag ohne Malus") einstellt,
oder ob alles bleibt, weil die Intensität „Locker" (kein Malus) genau dafür da ist. Eine Wertfrage über den
„Stick" – nur im Dialog.

## Verlauf

- **2026-11-08** — angelegt aus dem Rollengang Sohn ([Protokoll](../pm-sitzung-2026-11-08-rollengang.md)),
  zurückgestellt aus B-419.
