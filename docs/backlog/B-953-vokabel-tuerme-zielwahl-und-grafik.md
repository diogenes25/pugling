---
tags: [typ/story, status/idee, rolle/student]
aliases: [Turm-Zielwahl, Turmspiel-Grafik]
status: idee
prio: P3
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Nutzer im Dialog 2026-11-24; baut auf B-822"
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-953 · Vokabel-Türme: Kanone mit einstellbarer Zielwahl und echte Grafik für Figur und Türme

Die Türme der [Vokabel-Türme](B-822-vokabel-tuerme-natives-spiel.md) sollen eine sichtbare Kanone tragen,
die sich auf ihr Ziel dreht, und je Turm einstellbar sein, worauf sie zielt: das **erste**, **letzte**,
**nächste** oder **stärkste** Monster. Dazu eine laufende, animierte Spielfigur, die einsammelt, und
gezeichnete Türme statt der Platzhalter – in der Anmutung der bekannten Turm-Spiele, ohne deren Figuren,
Namen oder Optik zu übernehmen ([turm-spiel-plan.md](../turm-spiel-plan.md), Abschnitt „Grafik").

Heute zielt jeder Turm fest auf das Monster, das am weitesten gelaufen ist, und trifft ohne Flugzeit
([Level.cs:226](../../game/Scripts/Level.cs:226)); es gibt eine Monsterart mit zwei Leben
([Level.cs:27](../../game/Scripts/Level.cs:27)), „stärkstes" hat also noch keinen Inhalt. Gezeichnet wird
alles im Code einer Datei. Das ist eine reine App-Geschichte ohne Backend-Anteil.

## Verlauf

- 2026-11-24 · `idee` angelegt aus dem Dialog, zusammen mit B-952.
