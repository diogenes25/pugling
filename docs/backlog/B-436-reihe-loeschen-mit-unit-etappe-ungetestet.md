---
tags: [typ/story, status/idee, bereich/katalog, rolle/creator]
aliases: [Reihe löschen mit Unit-Etappe, unit_content_in_use Reihen-Pfad]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-08-nacht.md
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: [B-413]
---

# B-436 · Eine Reihe zu löschen, die nur eine Ziel-Etappe auf einer ihrer Units sperrt, prüft kein Test

Gefunden in der Nachschau zu Sprint 21 (Commit `4c25504`, Lauf AE). B-413 hat für das Löschen einer Unit **oder Reihe**
den Code `unit_content_in_use` eingeführt; die Meldung (`TextbookSeriesController.cs:326-327`), die Swagger-Summary
(`:281-282`), der deutsche Text in `frontend/src/lib/api.ts` und `docs/tutorial-supervisor.md:341` sagen alle, dass auch
eine Ziel-Etappe auf einer Unit der Reihe sperrt. Getestet ist nur die Unit (`CatalogManagementTests.cs:132`,
`Delete_Unit_MitZielEtappeAufDerUnit_Liefert409`); die Reihe nur über eine Lehrplan-Position.

Folge: der Unit-Umfang im Reihen-Pfad (`TextbookSeriesController.cs:301`) ließe sich stilllegen, und das Löschen endete am
Restrict-FK `KeyResult→SeriesUnit` als nackter 500 – kein Test würde rot.

Reparaturvorschlag des Reviewers: Fall 3 als `[Theory]` über `wholeSeries` wie der Nachbartest, dazu eine Mutante auf
Zeile 301.

## Verlauf

- **2026-11-08** — angelegt aus der Nachschau zu Sprint 21 (Lauf AE, frischer `pugling-reviewer`), 🟡.
