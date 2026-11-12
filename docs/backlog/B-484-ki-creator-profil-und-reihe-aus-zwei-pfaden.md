---
tags: [typ/story, status/idee, bereich/ki, rolle/creator]
aliases: [Briefing wählt das Profil nach Punkten, werkunabhängiges Profil landet im ersten Regal]
status: idee
prio: P2
art: Defekt
groesse: ""
wo: "backend"
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-12-backlog-lauf.md (Nachschau Sprint 35)
unverifiziert: true
entgangen_bei: [B-481]
grund: ""
ersetzt_durch: []
---

# B-484 · Der KI-Creator wählt Profil und Reihe an zwei Stellen

Nachschau von Sprint 35 (frischer `pugling-reviewer` an `d941ca1`; gelesen und gegrept, Suite 1343/1343, kein
eigener Lauf der Fälle). B-481 hat die **Unit** an eine Stelle gezogen – `create`, `exam`, `plan` und `briefing`
laufen alle über `BuildRequestAsync`. Das **Profil** und die **Reihe ohne Profilreihe** werden aber weiter an
zweiter Stelle mit eigener Regel bestimmt. Dieselbe Klasse wie B-473 und B-481: zwei Leser, zwei Regeln.

1. **Das Briefing wählt das Profil nach Punkten** (`Briefing/BriefingBuilder.cs:238-241`): bei `--child` mit
   `--series` oder `--series-unit` überspringt `BuildRequestAsync` die Profilwahl (`AgentCommands.cs:429`), und
   `ResolveProfileAsync` nimmt `matches[0]` ohne Blick auf die Reihe. Ein Profil auf einer anderen Reihe Y, das über
   Fach/Klasse/Schulart mehr Punkte hat, liefert dann Stoff aus Y für eine Übung, die in Reihe X entsteht; mit
   `--unit` bricht der Lauf mit „Unit … gehört nicht zur Reihe Y" ab (`BriefingBuilder.cs:266-270`). Die Tests legen
   das Profil immer auf die genannte Reihe.
2. **`--child --profile <Profil ohne Reihe>` landet im ersten Regal des Katalogs** (`AgentCommands.cs:426-434`,
   `:549-566`): ohne Profilreihe nimmt `ResolveLocationAsync` die erste Reihe mit Units, während das Briefing den
   Stoff aus dem Buch des Kindes holt. Werkunabhängige Profile gibt es ausdrücklich (`profiles` zeigt sie so an).

## Reste aus derselben Nachschau (🟢)

- `exam` und `plan` sind nur mit `--series-unit` getestet; die neue Unit-Regel läuft dort nie. Bei `plan` hängen
  die Buch-Wortlisten an der Unit.
- `--profile <ohne Reihe> --unit U` ohne Kind meldet „Unit gehört nicht zur Reihe …" statt „--unit setzt eine
  Lehrwerk-Reihe voraus" (`BriefingBuilder.cs:257-259`); verschwindet mit Punkt 2.
- Zwei Profile auf **derselben** Reihe entscheiden still nach Punkten (`AgentCommands.cs:512`) – so gewollt
  („zwei *Bücher*"), nur festgehalten.

## Verlauf

- **2026-11-12** — angelegt (Nachschau Sprint 35 im Backlog-Lauf).
