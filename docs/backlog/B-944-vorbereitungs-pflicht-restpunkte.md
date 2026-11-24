---
tags: [typ/story, status/idee, bereich/klassenarbeit]
aliases: []
status: idee
prio: P3
art: Aufräumen
groesse: ""
wo: "beides"
migration: ""
vertragsbruch: ""
quelle: "Reviews von B-752/B-757 im Nachtlauf I 2026-11-23 (🟢, ohne Zählung)"
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-944 · Vorbereitungs-Pflicht: Restpunkte aus dem Review von B-752 und B-757

Hinweise ohne Zählung (🟢) aus den Reviews, gesammelt statt einzeln: (1) `ItemCount` der Position ohne Tagesdosis ist der
Deckel (10), nicht `min(Karten, Deckel)`; `CardsPerDayAsync` meldet darum 10 für eine Übung mit 3 Inhalten. (2) Die Vorschau
teilt durch das ganze Fenster, der Start durch die Resttage – ein später Start fragt mehr, als die Vorschau zeigte. (3)
`MapAsync` kostet je geplanter Arbeit mehrere Abfragen plus eine je Bündel-Übung, auch Wochen vor dem Fenster. (4)
`AlignSpansAsync` lädt bei jedem Login alle je angelegten Vorbereitungs-Positionen. (5) Beim Löschen einer Arbeit wird eine Position,
die eine zweite Arbeit mitdeckt, entkoppelt und nie wieder abgeglichen. (6) Ein Fachwechsel der Arbeit lässt ihre Positionen im
Plan des alten Fachs laufen. (7) Eine kaputte Übungs-Konfiguration im Bündel lässt `ItemsOfAsync` werfen und blockiert den Login
des Kindes. (8) Nicht getestet: Obergrenze 61, Fenster-Validierung im PATCH, Rolle (Kind PATCHt `prepWindowDays` → 403). (9) Der
Anspruch auf den Start (`ExecuteUpdateAsync`, gegen doppelte Positionen bei gleichzeitigen Logins) hat keinen deterministischen
Test – eine Mutante, die ihn entfernt, bleibt grün. (10) Frontend: dieselbe Einstellung trägt zwei Beschriftungen (Feldhilfe in
`fieldHelp.ts`?), „etwa 0 Karten je Tag“ ohne zugewiesene Übungen (besser ein Server-Grund), Anlege-Formular prüft den Bereich nicht
selbst, Fenster-Editor auch bei geschriebenen Arbeiten, E2E-`getByLabel` ohne `exact`.

## Verlauf

- **2026-11-24** — angelegt im Nachtlauf I (Sprint 134): 🟢-Hinweise der Reviews, gesammelt und zurückgestellt (ohne Zählung,
  Freigabe 10); der Lauf endete am Fehlerzähler. Protokoll [pm-sitzung-2026-11-23-i.md](../pm-sitzung-2026-11-23-i.md).
- **2026-11-24** — aus den Reviews von Sprint 135 (🟢, ohne Zählung): (a) `FensterEditor` liest den Wert nur beim Einhängen – ein Wert, den ein anderer Erwachsener ändert, erscheint erst nach dem Neuladen der Seite; die Liste zeigt ihn richtig. (b) `page.context().clearCookies()` in den Klassenarbeits-Specs tut nichts (die Sitzung liegt im `localStorage`); ein eigener Browser-Kontext wäre der saubere Rollenwechsel. (c) `MapAsync` stellt je Arbeit jetzt drei Abfragen.
