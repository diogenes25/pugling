---
tags: [typ/story, status/idee, bereich/backend]
aliases: [concurrency_conflict doch erreichbar]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-08.md#runde-2--review
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-417 · `backend/Pugling.Api/CLAUDE.md` nennt `concurrency_conflict` „für Clients bewusst unerreichbar" – der Shop liefert ihn aber

Gefunden im Backend-Review zu Sprint 21 (`pugling-reviewer`, Nebenbefund zu B-411), bewusst nicht im Sprint. Der Absatz „Der Stamp ist
*nur* das – kein ETag" in `backend/Pugling.Api/CLAUDE.md` (Reward-Ökonomie, Hinweis auf B-103) sagt, `ApiErrors.ConcurrencyConflict` sei für
Clients bewusst unerreichbar. Gemeint ist: es gibt kein `ETag`/`If-Match`. Tatsächlich antworten `ShopService` (Kaufen, Stornieren,
Genehmigen, Ablehnen: `ShopError.Conflict` → `ToApiError`) sowie `MeController` und `MyInterestsController` bei einer verlorenen
Speicher-Kollision mit `409 concurrency_conflict`. Am Code geprüft am 2026-11-08 (`ShopService.cs:225/282/324/358/376`).

Zu entscheiden beim Ausformulieren: Satz auf „kein ETag" einschränken (Doku), oder der Code soll bei einer Kollision tatsächlich etwas anderes
liefern (Verhalten). Die Datei ist resident und über Budget – eine Berichtigung muss sich bezahlen.

## Verlauf

- **2026-11-08** — angelegt aus dem Backend-Review zu Sprint 21 (Lauf AD).
