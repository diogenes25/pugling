---
tags: [typ/story, status/idee, bereich/frontend, rolle/supervisor, rolle/creator]
aliases: [Pflichtfeld-Sweep]
status: idee
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
klickbar, lokale Meldung „… fehlt." – steht noch in mindestens zehn Formularen (gezählt per `grep "action.fail("` am
2026-11-05, nur die Pflichtfeld-Fälle): `ChildMaterialSection.tsx:254` (neue Reihe) und `:292` (Titel),
`ClozeTexts.tsx:165/166`, `VaterFachlehrer.tsx:259`, `VaterKind.tsx:176`, `VaterDashboard.tsx:33`,
`VaterClassTests.tsx:69/70`, `VaterAnmerkungen.tsx:271`, `ChildCarePanels.tsx:36/121`, `ExerciseSharingPanels.tsx:36`,
`RemarkWidget.tsx:107`. Nicht mitgezählt sind Prüfungen, die keinen leeren Pflichtwert meinen (PIN-Länge, Klassenstufen-
Reihenfolge, Sterne).

Bewusst nicht in B-233 genommen: das Sprint-Ziel (eine Seite, ein Sperrmuster) war ohne diese Stellen erfüllt, und
mehrere davon werden per exaktem Label in E2E und Vitest angesprochen (`"Name der neuen Reihe"` allein elfmal) – das
Nachziehen ist eigenes Handwerk.

## Verlauf

- **2026-11-05** — angelegt beim Bau von B-233 (Sprint 1 der Sitzung 2026-11-05).
