---
tags: [typ/story, status/abgenommen, bereich/backend, rolle/creator, rolle/supervisor]
aliases: [Schlagwort speichern mit unverändertem Label scheitert am Synonym-409]
status: abgenommen
prio: P2
art: Defekt
groesse: XS
wo: backend
migration: nein
vertragsbruch: nein
quelle: Nachschau zu Lauf K, Teil 2 in Lauf L (docs/pm-sitzung-2026-10-30-nacht.md)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-341 · Ein Schlagwort lässt sich nicht mehr speichern, sobald ein anderes sein Label als Synonym führt

`3190f9e` (Review-Funde Lauf K, vom Nutzer außerhalb der Schleife) hat im `PATCH` von Interessen-Schlagwörtern eine
Synonym-Prüfung ergänzt (`backend/Pugling.Api/Controllers/Creator/InterestTagsController.cs`, `Update`). Sie läuft
auch, wenn das Label **unverändert** mitgeschickt wird. Synonyme selbst werden beim Anlegen oder Ändern nicht gegen
fremde Labels geprüft.

Repro aus dem Code: Tag B „Soccer 1" anlegen, Tag A per `PATCH` das Synonym „soccer 1" geben → `200`. Ab dann scheitert
jedes Speichern von B im Formular (`frontend/src/vater/InterestTagAdmin.tsx:148` schickt immer `label`) mit `409`, auch
eine reine Farbänderung. Das bricht [B-141](B-141-interest-tag-label-dublette.md) AK 2 („eigenes Label unverändert
mitschicken → 200").

Offen und **nicht** Teil dieses Defekts: ob die Regel symmetrisch sein soll (Synonyme gegen Labels prüfen) und ob der
Slug-Vergleich (ein Synonym „soccer" sperrt das Label „SOCCER") zu B-141 AK 3 passt. Das ist eine Produktfrage zur
Entscheidung hinter B-141.

## User Story

Als **Creator** oder **Vater**, der ein Interessen-Schlagwort pflegt, möchte ich Farbe, Facette oder Synonyme ändern
können, ohne dass ein anderes Schlagwort mein unverändertes Label blockiert.

## Ist-Stand (Lauf L, am Code belegt)

- `InterestTagsController.Update` prüft bei jedem mitgeschickten `label` Slug, Label und Synonyme anderer Tags – auch wenn
  das Label gleich bleibt. Das Formular (`frontend/src/vater/InterestTagAdmin.tsx:148`) schickt `label` immer mit.
- Synonyme werden beim Anlegen und Ändern nicht gegen fremde Labels geprüft; ein anderer Tag kann das Label also später
  als Synonym aufnehmen.
- Dasselbe gilt für den Slug: ein umbenannter Tag behält seinen alten Slug, ein anderer Tag darf den Slug tragen, zu dem
  sein **heutiges** Label führt (explizit gesetzter Slug beim Anlegen).
- **Gemessen** (rote Probe unten): beide Fälle `409` statt `200`.

## Die echte Lücke

Die Prüfungen fragen „darf dieses Label neu vergeben werden?" – und stellen die Frage auch, wenn nichts neu vergeben wird.

## Entscheidungen (autonom gegrillt, Freigabe 1 – `Defekt`)

1. **Ein unverändertes Label (ordinal gleich nach Trim) durchläuft keine der drei Prüfungen.** Es beansprucht nichts Neues;
   was es gestern durfte, darf es heute. Die drei Prüfungen liegen jetzt zusammen in `LabelConflictAsync`. *Kosten:* ein
   Tag, dessen Label inzwischen mit einem fremden Synonym kollidiert, bleibt so stehen, bis jemand umbenennt.
2. **Nicht entschieden, weil Produktfrage (Freigabe 1, `Frage`):** ob die Synonym-Regel symmetrisch werden soll (Synonyme
   gegen fremde Labels prüfen) und ob ihr Slug-Vergleich zu B-141 AK 3 passt. Ebenso offen: der Kommentar an
   `SynonymTakenAsync` begründet die Regel damit, dass `EnsureAsync` einen zweiten Tag „nie fände" – der Prüfer hält das für
   falsch (Suche Slug → Label → Synonym). Beides bleibt beim Nutzer, der die Regel in `3190f9e` gesetzt hat.

## Akzeptanzkriterien

1. Tag B trägt Label L, Tag A nimmt L später als Synonym auf → `PATCH` von B mit unverändertem L und neuer Farbe → `200`,
   Farbe gespeichert.
2. Ein umbenannter Tag, dessen heutiges Label zum Slug eines anderen Tags führt, lässt sich mit unverändertem Label
   speichern → `200`.
3. Umbenennen **auf** ein fremdes Label, einen fremden Slug oder ein fremdes Synonym bleibt `409` (bestehende Fälle).

## Schätzung

`XS`, Backend, keine Migration, kein Vertragsbruch (`409` → `200` nur für einen Fall, der nie hätte scheitern sollen).
Testweg: `InterestTagLabelTests`.

## Verlauf

- **2026-10-30** — angelegt aus der Nachschau zu Lauf K, Teil 2 (`pugling-reviewer`, 🟡, Fund in `3190f9e`). Aus Code
  gelesen. `entgangen_bei` leer: die Prüfung stammt aus keinem abgenommenen Increment einer Story.
- **2026-10-30** — Lauf L, Teil 2: ausformuliert, autonom gegrillt (Freigabe 1), geschätzt, gebaut. Rote Probe: zwei neue
  Fälle **2 failed** (erwartet 2; je `409` statt `200`). Danach `InterestTagLabelTests` **9/9**. Die Mutante „ohne
  `label != tag.Label`" ist genau der Ausgangsstand der roten Probe. AK 3 halten die bestehenden Fälle
  `Umbenennen_Auf_Das_Label_…`, `Label_Das_Ein_Anderer_Tag_Als_Synonym_Fuehrt_…` (Umbenennen-Zweig) und
  `Anlegen_Mit_Dem_Label_…` (grün).
- **2026-10-30** — Review `pugling-reviewer`: 🟡 „ordinal" aus Entscheidung 1 hielt kein Test (Mutante `OrdinalIgnoreCase` ließe zwei Tags „X" zu) → Fall `Umbenennen_Nur_In_Der_Schreibweise_Auf_Ein_Fremdes_Label_Wird_Abgewiesen`, Mutante **1 failed** (zählt); 🟢 „nach Trim" ungetestet → der Synonym-Fall schickt das Label mit Leerraum, Mutante „roher `dto.Label`" **1 failed** (ohne Zählung); 🟢 Slug-Fall prüft jetzt auch die Farbe (ohne Zählung); 🟢 AK 3 „auf einen fremden Slug" hält `SlugRenameGuardTests.Umbenennen_Auf_Einen_Belegten_Namen_Wird_Abgewiesen` (Zeile interest-tags), nicht `InterestTagLabelTests`.
- **2026-10-30** — Abnahme Lauf L, Teil 2: volle Suite **1028/1028**, Build ohne Warnung. E2E-Gesamtlauf **100 passed** (`exit=0`, nach dem letzten Produktcode-Edit gestartet; danach nur Tests, Vertragskommentar und die neue Spec), danach die neue Spec und die drei Nachbar-Specs **4 passed**. Rollengang: Vater/Creator über die neue Spec `schlagwort-trotz-fremdem-synonym` (Schlagwort-Verwaltung, Facette ändern → Neuladen → gespeichert; rote Probe ohne den Fix **1 failed**); Sohn Regressionszeuge (seine Specs im Gesamtlauf grün, kein Pfad von ihm im Diff). Kein Delta-Review nach den Nachbesserungen: sie sind Testfälle und Kommentare, je mit gemessener Mutante (Selbst-Check). **Abgenommen**, Commit siehe Protokoll ([Lauf L](../pm-sitzung-2026-10-30-nacht.md)).
