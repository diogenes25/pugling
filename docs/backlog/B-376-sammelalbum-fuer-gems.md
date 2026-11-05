---
tags: [typ/story, status/idee, bereich/gamification, rolle/student]
aliases: [Sammelalbum, Karten-Sets]
status: idee
prio: P3
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: docs/baobab-vergleich-plan.md#wo-baobab-weiter-ist
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-376 · Ein Sammelalbum mit Karten-Sets für Gems statt einzelner Skins

Gems kaufen heute einzelne Skins aus dem `SkinCatalog` (`backend/Pugling.Api/Models/AdminEntities.cs:185`),
am Kind als `OwnedSkins` gehalten. Baobab motiviert mit Sammelkarten; ein Album, das sich in Sets
füllt, trägt länger als ein einzelner Kauf, weil das fehlende Stück sichtbar bleibt. Die Idee: Karten in
Sets, freigeschaltet über Gems oder als Belohnung, mit einem Bonus für ein komplettes Set und einer
Album-Ansicht in der Sohn-Arcade. Abzugrenzen von [B-221](B-221-level-leiter-fuer-auszeichnungen.md)
(Level-Leiter der Auszeichnungen) und vom Familien-Shop, dessen Münzen reale Gegenwerte kaufen. Zu
klären: ob das Album die Skins ersetzt oder ergänzt, und ob Zufallsziehungen ausgeschlossen werden (für
ein Kind um 11 Jahre: ja, kein Lootbox-Mechanismus).

## Verlauf

- **2026-11-05** — angelegt (Quelle: Baobab-Vergleich, Etappe 3 des Übernahmeplans).
