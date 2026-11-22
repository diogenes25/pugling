---
tags: [typ/story, status/idee]
aliases: []
status: idee
prio: P3
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: B-822
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-850 · Vokabel-Türme: Godot-C#-App meldet sich auf dem Android-Handy an

Teil 1 von 4 aus B-822, ohne Abhängigkeit. Ein leeres Godot-4-C#-Projekt unter `game/` wird als APK
gebaut, läuft auf Friedrichs Handy und holt per `auth/child` über die Tailscale-HTTPS-Adresse ein JWT.
Der Spike misst nebenbei, ob `Pugling.Contracts` (`net10.0`) aus Godot referenzierbar ist. Godot ist
auf dem Entwickler-PC noch nicht installiert.
Entscheidungen und Akzeptanzkriterien: [B-822](B-822-vokabel-tuerme-natives-spiel.md); Spielidee und
Technik: [turm-spiel-plan.md](../turm-spiel-plan.md).

## Verlauf

- 2026-11-22 · `idee` angelegt beim Teilen von B-822 (Entscheidung 8).
