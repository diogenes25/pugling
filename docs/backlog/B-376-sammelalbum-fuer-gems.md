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
- **2026-11-05** — **Entscheidung im Auftrag des Nutzers** (getroffen von Claude; Delegation im Dialog am 2026-11-05: „Wenn es etwas zu entscheiden gibt, übernimm meine Position und entscheide nach bestem Wissen und Gewissen“; [Protokoll](../pm-sitzung-2026-11-05-delegation.md)): (1) ergänzt die Skins; (2) keine Zufallsziehung, jede Karte gezielt mit Gems, komplettes Set gibt Gems-Bonus; (3) Karten sind Emoji plus Name in einem Katalog im Code. Die Anmutung braucht einen Augencheck. **Freigabe des Nutzers (Autonomer Modus, Opt-in, erteilt im Dialog am 2026-11-05 mit derselben Delegation):** ein Lauf darf diese Story auf Grundlage der Entscheidungen selbst ausformulieren, schätzen und bauen; trifft er auf eine Produktfrage, die sie nicht hergeben, hält er an.
- **2026-11-14** — Abgleich Backlog-Lauf: Ist-Stand bestätigt – kein Album/Karten-Set im Code (Suche nach `album`/`CardSet`/`collectible` in `backend/Pugling.Api` und `frontend/src` leer); `SkinCatalog` steht jetzt in `backend/Pugling.Api/Models/AdminEntities.cs:191`, `OwnedSkins` in `:123`.
