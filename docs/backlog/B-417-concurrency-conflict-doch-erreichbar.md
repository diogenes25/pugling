---
tags: [typ/story, status/abgenommen, bereich/backend]
aliases: [concurrency_conflict doch erreichbar]
status: abgenommen
prio: P3
art: Defekt
groesse: "XS"
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

## Entscheidungen

Autonom gegrillt (`art: Defekt`, Tageslauf):

1. **Doku, nicht Verhalten:** der Code tut das Richtige (eine verlorene Kollision am Stamp ist „bitte nochmal"); falsch war
   der Satz. Er sagt jetzt: kein `ETag`/`If-Match`; `concurrency_conflict` nur bei verlorener Kollision am Stamp des Kindes
   (Shop, Skins, Interessen-Wunsch), nie als Versionsprüfung. Gleiche Länge, die Datei bleibt im Budget.

## Akzeptanzkriterien

1. Der Satz nennt die drei Stellen, die den Code liefern (`ShopService`, `MeController`, `MyInterestsController`).

## Verlauf

- **2026-11-08** — angelegt aus dem Backend-Review zu Sprint 21 (Lauf AD).
- **2026-11-13** — ausformuliert, autonom gegrillt (Defekt), geschätzt XS (Tageslauf, Sprint 51).
- **2026-11-13** — gebaut (Sprint 51), nur Doku. AK 1 am Code nachgezählt: `ConcurrencyConflict` steht in `MeController`
  (Kauf, Skin), `MyInterestsController`, `ShopService` – sonst nirgends (Review bestätigt). Kein Test, keine Mutante: ein
  Satz in einer Anweisungsdatei. Abgenommen.
