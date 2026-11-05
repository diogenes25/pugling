---
tags: [typ/story, status/idee, bereich/backend, rolle/creator]
aliases: [Übersetzung PUT mit Id 0 ungetestet]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-05.md#lauf-w
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: [B-386]
---

# B-393 · B-386 AK 1 „Ändern mit `0`" hat keinen Test

B-386 AK 1 verlangt `400 validation_error` für eine `vocabularyId` von `0` beim Anlegen **und** Ändern. Belegt ist `0` nur beim
`POST` (`backend/Pugling.Api.Tests/UnknownReferenceCodeTests.cs`, Fall `Unbekannte_Vokabel_an_einem_Uebersetzungspaar_…`); der `PUT`
ist nur mit einer unbekannten Id geprüft. Das Risiko ist klein (derselbe Validator), der Fix eine Zeile: `PUT … Translation(0)` →
`validation_error`. Dieselbe Klasse wie B-388.

## Verlauf

- **2026-11-05** — angelegt aus der Nachschau zu B-386 (Lauf W, Retro Sprint 8; frischer `pugling-reviewer`, 🟢). Entgangen bei B-386,
  dem Increment von Lauf V – darum kein Limit 2 für Lauf W.
