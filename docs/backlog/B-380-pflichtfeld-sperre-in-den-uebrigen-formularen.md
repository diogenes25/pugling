---
tags: [typ/story, status/ausformuliert, bereich/frontend, rolle/supervisor, rolle/creator]
aliases: [Pflichtfeld-Sweep]
status: ausformuliert
prio: P3
art: Aufräumen
groesse: ""
wo: frontend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-05.md#sprint-1
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-380 · Die Hausregel „Pflichtfeld leer → Speichern gesperrt" gilt erst auf der Lehrwerke-Seite

B-233 hat die Regel in `frontend/CLAUDE.md` gesetzt und für `SeriesForm`/`UnitForm` gebaut. Dasselbe Muster – Knopf
klickbar, lokale Meldung „… fehlt." – steht noch in fünfzehn Formularen (gezählt per `grep "action.fail("` am
2026-11-05, nur die Pflichtfeld-Fälle): `ChildMaterialSection.tsx:254` (neue Reihe) und `:292` (Titel),
`ClozeTexts.tsx:165/166`, `VaterFachlehrer.tsx:259`, `VaterKind.tsx:176`, `VaterDashboard.tsx:33`,
`VaterClassTests.tsx:69/70`, `VaterAnmerkungen.tsx:271`, `ChildCarePanels.tsx:36/121`, `ExerciseSharingPanels.tsx:36`,
`RemarkWidget.tsx:107`, `VaterProfil.tsx:111`, `VaterZiele.tsx:433`, `PlanPositions.tsx:490` (die letzten drei aus der Nachschau vom 2026-11-05 nachgetragen). Nicht mitgezählt sind Prüfungen, die keinen leeren Pflichtwert meinen (PIN-Länge, Klassenstufen-
Reihenfolge, Sterne).

Bewusst nicht in B-233 genommen: das Sprint-Ziel (eine Seite, ein Sperrmuster) war ohne diese Stellen erfüllt, und
mehrere davon werden per exaktem Label in E2E und Vitest angesprochen (`"Name der neuen Reihe"` allein zehnmal) – das
Nachziehen ist eigenes Handwerk.

## User Story

Als *Vater* möchte ich an einem leeren Pflichtfeld schon am gesperrten Speichern-Knopf und am „(Pflicht)" im Label sehen,
was fehlt – überall gleich wie auf der Lehrwerke-Seite –, statt erst nach dem Klick eine lokale Meldung „… fehlt." zu lesen.

## Ist-Stand am Code (nachgezählt 2026-11-13)

- „(Pflicht)" im Label trägt heute nur `VaterLehrwerke.tsx` (`grep -l "(Pflicht)"`).
- Die Form `fail("… fehlt")` steht noch an fünf Stellen: `ChildMaterialSection.tsx:254`, `:292`, `ClozeTexts.tsx:165`,
  `:166`, `VaterFachlehrer.tsx:267` (vorher `:259` – die Zeile ist gewandert).
- Die übrigen zehn Dateien der Liste oben rufen weiter `.fail(` auf (VaterKind 4, VaterProfil 3, PlanPositions 3,
  VaterDashboard 2, VaterClassTests 2, ChildCarePanels 2, je 1 in VaterAnmerkungen, ExerciseSharingPanels, RemarkWidget,
  VaterZiele) – mit anderem Wortlaut. **Welche davon Pflichtfeld-Fälle sind, ist je Zeile zu lesen**, nicht zu zählen.

## Offene Punkte

1. Je Zeile entscheiden: Pflichtfeld (→ Sperre + „(Pflicht)") oder andere Prüfung (bleibt).
2. Schnitt: ein Sprint je Seite (E2E und Vitest sprechen Labels exakt an, `"Name der neuen Reihe"` allein zehnmal) oder
   ein gemeinsamer Baustein, der Label-Zusatz und Sperre aus einer Quelle setzt? Empfehlung PM: Baustein zuerst, dann je
   Seite nachziehen – sonst entstehen fünfzehn Fassungen derselben Regel.

## Verlauf

- **2026-11-05** — angelegt beim Bau von B-233 (Sprint 1 der Sitzung 2026-11-05).
- **2026-11-13** — ausformuliert (Tageslauf, Restzeit): Bestand nachgezählt (5 Stellen „fehlt", zehn Dateien mit anderem Wortlaut je Zeile zu prüfen), User Story, zwei offene Punkte.
