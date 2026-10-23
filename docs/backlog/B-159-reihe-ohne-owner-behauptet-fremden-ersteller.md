---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/katalog, rolle/creator]
aliases: [Ownerlose Reihe behauptet fremden Ersteller]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: frontend
migration: nein
vertragsbruch: nein
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

## Akzeptanzkriterien

1. Bei `ownerAdultId == null` nennt der Satz keinen fremden Ersteller, sondern den Grundbestand.
2. Bei einer Reihe mit Eigentümer bleibt der bisherige Satz unverändert.
3. Ein Komponententest deckt beide Fälle (Vorbild `CatalogAdmin.test.tsx` aus B-154).

## Entscheidungen

Gegrillt im Nachtlauf 2026-10-23 (Freigabe 1, `art: Defekt`).

1. **Ownerlose Reihen sind im Seed der Normalfall, nicht der Randfall** (Punkt 1). Nachgesehen in
   `backend/Pugling.Api/Data/Seed.cs`: `Découvertes 1` (`:611`), `Green Line 1` (`:1017`), `Mathe-Sammlung`
   und `Erdkunde-Sammlung` (`:1036-1037`) setzen alle kein `OwnerAdultId`. Jede geseedete Reihe zeigt den
   falschen Satz, sobald ein Creator sie aufklappt – dieselbe Wendung wie bei B-154. Die Prio bleibt P3,
   weil es ein Text ohne Rechtefolge ist. **Kosten:** keine.
2. **Derselbe Wortlaut wie B-154** (Punkt 2): „Diese Reihe gehört zum Grundbestand – du kannst sie verwenden,
   aber niemand kann sie ändern." Begründung: zwei Katalogebenen, ein Begriff. „ändern" statt „umbenennen
   oder löschen", weil an der Reihe auch die Units hängen und `UnitPanel` sie bei `!isOwn` ebenfalls sperrt
   (`VaterLehrwerke.tsx:661`) – der Satz deckt beides. `== null`, weil der Vertrag `ownerAdultId` optional
   herausgibt (`contract.ts`, wie in `CatalogAdmin.tsx:250`). **Kosten:** keine.
3. **Kein geteilter Baustein** (Punkt 3). Nachgezählt: der Satz „hat jemand anderes angelegt" steht genau
   zweimal im Frontend (`CatalogAdmin.tsx:256`, `VaterLehrwerke.tsx:362`); B-157 hat keine dritte Stelle
   gebracht. Die Regel der Story – erst bei der dritten Wiederholung – greift noch nicht. **Kosten:** Beim
   dritten Mal kostet das Zusammenziehen dann drei Stellen statt zwei.
4. **Der Satz zieht in ein kleines exportiertes Bauteil `SeriesEigentumsHinweis`** (in `VaterLehrwerke.tsx`,
   exportiert nur für den Test – dasselbe Muster wie `SeriesForm`, B-143). Begründung: `SeriesRow` ist nicht
   exportiert und rendert `UnitPanel`, das beim Aufklappen die Units abruft; ein Test über `SeriesRow`
   bräuchte ein gefälschtes `fetch`. Das Bauteil ist reine Prop-Arbeit. **Kosten:** eine Funktion mehr für
   zwei Zeilen JSX – gerechtfertigt allein durch den Test (AK 3).

## Schätzung

**Größe XS** (Anker B-02). `wo: frontend`, keine Migration, kein Vertrag.

**Angriffsplan:** rote Probe (Komponententest, drei Fälle wie `CatalogAdmin.test.tsx:58-78`) → Bauteil →
Suite → Reviewer → Commit.

**Risiko:** keines mit Verhaltensfolge; Rechte ändern sich nicht (die Knöpfe fehlen bei `!isOwn` weiterhin).

**Testweg:** `VaterLehrwerke.test.tsx`, neuer `describe` mit drei Fällen (fremd, ownerlos, Feld fehlt). Der
Rollengang als Creator (Lehrwerke-Seite, Seed-Reihe aufklappen) ist ohne Browser nur als E2E führbar; die
bestehende `lehrwerke.spec.ts` klappt Reihen auf und bleibt die Gegenprobe – eine eigene Spec für einen
Satz wäre teurer als ihr Beleg.

## Verlauf

- **2026-10-23** — **abgenommen** (Nachtlauf, Sprint 1). `frontend-reviewer`: keine Blocker, Wortlaut und
  `== null` gegen B-154 geprüft; **ein Fund mit Testwirkung (Zähler 3 des Sprints):** die Verdrahtung in
  `SeriesRow` war unbelegt – die Zeile ließ sich streichen, 394/394 blieben grün. Darum jetzt eine eigene
  Spec `reihe-aus-dem-grundbestand.spec.ts` (Vater #1 klappt „Green Line 1" auf → „gehört zum Grundbestand",
  kein „jemand anderes"), **1/1** grün, zusammen mit `lehrwerke.spec.ts` **3/3**. **Das ist zugleich der
  Rollengang aus der Creator-Rolle** – die Zeile aus dem Verlauf darunter („was ein Mensch noch sieht") ist
  damit maschinell gedeckt. Commit: siehe Nachtrag.
- **2026-10-23** — **gebaut** (Nachtlauf, Sprint 1): `SeriesEigentumsHinweis` in `VaterLehrwerke.tsx`,
  `SeriesRow` rendert es bei `!isOwn`. **Rote Probe (erwartet/gemessen):** Vitest `VaterLehrwerke.test.tsx`
  **3 rot von 30** – alle drei neuen Fälle mit „Element type is invalid … got: undefined" (das Bauteil
  existierte nicht; der Import ist die Probe, wie bei B-216s Hook). Nach dem Fix 30/30 in der Datei,
  **394/394** gesamt. **Rollengang:** Chrome-Extension nicht verbunden (Freigabe 6); die Lehrwerke-Seite
  fährt `lehrwerke.spec.ts` (grün, 5/5 im Lauf mit den neuen Specs) – sie klappt Reihen auf, sichert den
  Satz aber nicht zu. Der Satz selbst ist nur im Komponententest belegt (drei Fälle); **was ein Mensch noch
  sieht:** `/vater/lehrwerke` als Vater #1, „Green Line 1" aufklappen → „gehört zum Grundbestand".
- **2026-10-23** — gegrillt und geschätzt im Nachtlauf (Freigabe 1). Punkt 1 am Seed nachgesehen: alle
  geseedeten Reihen sind ownerlos.
- **2026-08-12** — angelegt aus dem `frontend-reviewer`-Befund zu B-154 (Nachtlauf Sprint A).
  **Bewusst nicht in B-154 mitgenommen:** dessen Ziel (`/vater/katalog` verspricht nichts, was der Server
  verweigert) ist ohne diese Story erfüllt, und der Fund liegt in einer anderen Datei und einer anderen
  Katalogebene. Der Ist-Stand ist **selbst am Code nachgesehen** (nicht aus dem Review übernommen):
  `VaterLehrwerke.tsx:221-223` und die Nullability in `CurriculumEntities.cs:53`.
