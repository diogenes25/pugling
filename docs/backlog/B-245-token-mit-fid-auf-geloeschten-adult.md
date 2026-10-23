---
tags: [typ/story, status/idee, bereich/backend, bereich/auth]
aliases: [fid auf gelöschte Zeile, OwnerAdultId FK nach DELETE adults]
status: idee
prio: P3
art: Defekt
groesse: ""
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-10-23-nacht.md (Nachschau zu Sprint 3, zweiter Lauf – Punkt 4 des Prüfauftrags)
unverifiziert: true
grund: ""
ersetzt_durch: []
entgangen_bei: []
wartet_auf: ""
---

# B-245 · Ein Token, das vor `DELETE adults/{id}` ausgestellt wurde, trägt ein `fid` auf eine gelöschte Zeile – jeder `OwnerAdultId`-Schreiber liefe in die FK

**Unverifiziert** – aus der Nachschau zu B-158 (Prüfauftrag Punkt 4: was passiert, wenn `fid` fehlt oder
nicht stimmt). Die Invariante „Creator-Claim ⇒ `fid`" hält `AccountService.cs:46-47`; sie sagt nichts
darüber, ob die Zeile hinter dem `fid` noch existiert.

## Vermutung

Ein Adult löscht sich (`AdultsController.Delete`), behält aber ein gültiges JWT (Lebensdauer siehe
`TokenService`). Mit diesem Token `POST creator/subjects` (oder `textbook-series`, `categories`): der
Controller schreibt `OwnerAdultId = fid` – die FK auf `Adults` schlägt fehl → **500** statt 401/403.
Betrifft jeden Schreiber, der `fid` in eine FK-Spalte setzt, nicht nur Subjects.

## Zu klären

1. Messen: Adult anlegen, Token holen, löschen, mit dem alten Token ein Fach anlegen – Statuscode?
2. Wenn 500: gehört die Prüfung in die Auth-Wand (Token gegen Konto-Existenz je Request – kostet eine
   Abfrage) oder in `AuthAccess` (einmal je Schreiber)? Vorbild: wie prüft `IsSupervisor`/`SupervisorOwnsChildAsync`?

## Verlauf

- **2026-10-23** — als `idee` aufgenommen (unverifiziert) aus der Nachschau zu Sprint 3 des ersten Laufs;
  nicht gemessen, weil der Sprint sich nicht erweitert.
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)) (am Code hergeleitet, nicht gemessen): die 500 ist plausibel – `AdultsController.Delete` (`:129-160`) löscht Adult und Konto, das JWT bleibt 12 h gültig ohne Prüfung je Request (`Program.cs:271-279`, `TokenService.cs:12,53`), `SubjectsController.Create` setzt `OwnerAdultId = fid` gegen eine echte FK (`PuglingDbContext.cs:471`), und für `DbUpdateException` gibt es keinen Handler. **Betroffen sind mehr Schreiber:** `TextbookSeriesController.cs:179`, `ExerciseCategoriesController.cs:132`, `CreatorProfilesController.cs:98`, `ExerciseControllerBase.cs:329,339-345`, `ChildrenController.cs:86`. Kein Id-Recycling (AUTOINCREMENT), also keine Kontoübernahme. Die Messung (Zu klären 1) ist nachtlauffähig; der Ort der Prüfung (zentral in `OnTokenValidated` vs. je Schreiber) ist eine technische Wahl, die der Code verengt (sechs+ Schreiber sprechen für zentral).
