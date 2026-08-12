---
tags: [typ/story, status/ausformuliert, bereich/frontend, bereich/katalog, rolle/creator]
aliases: [Ownerlose Reihe behauptet fremden Ersteller]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: frontend-reviewer zu B-154 (Nachtlauf 2026-08-12, Folgefund)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
---

# B-159 · Eine Reihe ohne Eigentümer behauptet, jemand anderes habe sie angelegt

Dieselbe Unterscheidung, die [B-154](B-154-katalogseite-bietet-fremde-faecher-zum-umbenennen.md) für das
Fach eingeführt hat, fehlt eine Ebene höher bei der Lehrwerk-Reihe: dort steht bei jedem `!isOwn` derselbe
Satz, auch wenn die Reihe **niemandem** gehört.

## User Story

Als *Creator* möchte ich bei einer Reihe aus dem Grundbestand nicht lesen, ein anderer Creator habe sie
angelegt, damit ich nicht nach einem Eigentümer suche, den es nicht gibt.

## Ist-Stand am Code

- `frontend/src/vater/VaterLehrwerke.tsx:221-223` zeigt bei `!series.isOwn` ausnahmslos: „Diese Reihe hat
  jemand anderes angelegt – du kannst sie verwenden, aber nicht ändern."
- `TextbookSeries.OwnerAdultId` ist nullable (`backend/Pugling.Api/Models/CurriculumEntities.cs:53`), und
  die FK steht auf `SetNull` — eine Reihe wird also **ownerlos**, sobald ihr Eigentümer-Adult gelöscht wird,
  zusätzlich zu allem, was ohne Owner entsteht.
- `IsOwnedBy(null, …)` ist fail-closed (`backend/Pugling.Api/Auth/AuthAccess.cs:90-91`) — `isOwn` ist bei
  einer ownerlosen Reihe für **jeden** `false`, der Satz erscheint also garantiert.
- `TextbookSeriesResponse` trägt `ownerAdultId` neben `isOwn`
  (`backend/Pugling.Contracts/Creator/TextbookSeriesDtos.cs:14`) — die Unterscheidung ist ohne neuen
  Aufruf möglich, genau wie beim Fach.
- Dieselbe Stelle nennt der Reviewer als „wörtlich die Unterscheidung, die B-154 fürs Fach eingeführt hat,
  eine Ebene höher noch offen".

## Die echte Lücke

Kein fehlendes Feld und kein falsches Recht — die Knöpfe fehlen bei `!isOwn` korrekt. Falsch ist nur die
**Begründung**, die die Oberfläche dafür angibt, und zwar in genau dem Fall, der im Seed der häufigste ist.
Fehlerklasse wie [B-112](B-112-kommentar-begruendet-das-gegenteil.md), nur für den Nutzer sichtbar statt
für den Entwickler.

## Offene Punkte

1. **Gibt es ownerlose Reihen im Seed?** Zu prüfen: Wenn nein, ist der Fall heute nur über das Löschen eines
   Adults erreichbar, und die Prio sinkt. Empfehlung: nachsehen, bevor geschätzt wird — dieselbe Prüfung hat
   bei B-154 aus einem vermuteten Randfall den Normalfall gemacht.
2. **Denselben Wortlaut wie B-154 verwenden oder einen eigenen?** Empfehlung: denselben („gehört zum
   Grundbestand"), damit zwei Katalogebenen nicht zwei Begriffe für einen Zustand haben.
3. **Lohnt ein geteilter Baustein statt zweier Textstellen?** Empfehlung: erst bei der dritten
   Wiederholung — [B-157](B-157-kategorien-unter-fremdem-fach-ungeschuetzt.md) könnte sie sein, dann
   gemeinsam.

## Akzeptanzkriterien (Entwurf)

1. Bei `ownerAdultId == null` nennt der Satz keinen fremden Ersteller, sondern den Grundbestand.
2. Bei einer Reihe mit Eigentümer bleibt der bisherige Satz unverändert.
3. Ein Komponententest deckt beide Fälle (Vorbild `CatalogAdmin.test.tsx` aus B-154).

## Verlauf

- **2026-08-12** — angelegt aus dem `frontend-reviewer`-Befund zu B-154 (Nachtlauf Sprint A).
  **Bewusst nicht in B-154 mitgenommen:** dessen Ziel (`/vater/katalog` verspricht nichts, was der Server
  verweigert) ist ohne diese Story erfüllt, und der Fund liegt in einer anderen Datei und einer anderen
  Katalogebene. Der Ist-Stand ist **selbst am Code nachgesehen** (nicht aus dem Review übernommen):
  `VaterLehrwerke.tsx:221-223` und die Nullability in `CurriculumEntities.cs:53`.
