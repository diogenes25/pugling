---
tags: [typ/story, status/idee, bereich/backend, bereich/katalog, rolle/creator]
aliases: [Fach umbenennen, denormalisierter SubjectName veraltet, Selbstheilung beim PATCH]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: pugling-reviewer zu B-142 (Nachtlauf 2026-08-10, Fund 7)
unverifiziert: true
entgangen_bei: []
---

# B-145 · Ein umbenanntes Fach lässt seinen Namen in drei Tabellen stehen

Dieselbe Widersprüchlichkeit wie [B-142](B-142-fachname-driftet-gegen-fach-id.md), nur von der anderen
Seite: dort wanderte die **Id** ohne den Namen, hier ändert sich der **Name** und die Kopien bleiben.

`Controllers/Creator/SubjectsController.cs:64` benennt ein Fach um, ohne die drei denormalisierten
`SubjectName`-Spalten nachzuziehen (Lehrwerk-Reihe, Fachlehrer-Profil, Lehrbuch des Kindes). Danach
behauptet eine Reihe per Id „Französisch" und per Name „Franz.".

**Die Dringlichkeit ist durch B-142 gesunken, nicht gestiegen** — und das gehört dazu, sonst liest sich
die Story dramatischer, als sie ist: Weil der Server den Namen seit B-142 bei *jedem* Schreibzugriff aus
der Id ableitet, **heilt sich eine betroffene Zeile beim nächsten beliebigen `PATCH` selbst**. Der
Zustand ist also nicht dauerhaft, sondern hält nur bis zur nächsten Änderung. Zu klären ist damit vor
allem, ob das genügt oder ob das Umbenennen die Kopien aktiv nachziehen soll.

Beim Ausformulieren mitzunehmen (zwei Beobachtungen desselben Reviews):

- **`Data/Seed.cs`** (`:206-207`, `:614-615`, `:1019-1020`, `:1033-1034`) setzt das Paar von Hand und ist
  damit der vierte Schreiber, der an `SubjectNaming` vorbeigeht. Heute sind alle Werte konsistent
  (nachgesehen) — aber es ist die verbliebene Stelle, an der die Invariante wieder auseinanderlaufen kann.
  B-142s Entscheidung 3 hat ausdrücklich **keinen** Wächter gebaut; wenn einer je gerechtfertigt ist, dann
  aus diesem Grund.
- **Uneinheitlicher Fehlercode für dieselbe Regel:** Eine nicht existierende `SubjectId` meldet
  `TextbookSeriesController.cs:115` als `invalid_reference`, `TextbooksController.cs:62,95` dagegen als
  `validation_error`. Bestand, unabhängig von dieser Story, aber am selben Feld.

## Verlauf

- **2026-08-10** — angelegt aus dem `pugling-reviewer`-Befund zu B-142 (Fund 7), Nachtlauf Sprint 2.
  **Bewusst nicht in B-142 mitgenommen:** dessen Ziel — die Zeile widerspricht sich nicht mehr selbst,
  wenn die Id wandert — ist ohne diese Story erfüllt, und der Fehler liegt außerhalb des Diffs (er ist
  älter). `unverifiziert: true`, weil die Belege aus dem Review stammen und nicht aus eigener Recherche.
