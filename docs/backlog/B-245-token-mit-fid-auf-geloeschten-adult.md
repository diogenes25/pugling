---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/auth]
aliases: [fid auf gelöschte Zeile, OwnerAdultId FK nach DELETE adults]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-10-23-nacht.md (Nachschau zu Sprint 3, zweiter Lauf – Punkt 4 des Prüfauftrags)
unverifiziert: false
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

## Ist-Stand (gemessen 2026-10-26)

Erwachsener anlegen, Token holen, `DELETE supervisor/adults/{id}`, mit demselben Token `POST creator/subjects`:
**500** (`DeletedAccountTokenTests`, vor dem Fix: erwartet `Unauthorized`, gemessen `InternalServerError`).

## Entscheidungen

1. **Zentral nach der Token-Validierung** (`JwtBearerEvents.OnTokenValidated` → `Auth/TokenSubjectCheck`): Ein Token,
   dessen Konto (`aid`) nicht mehr existiert, ist ungültig, also 401. Begründung: Sechs und mehr Schreiber setzen
   `fid` in eine FK, eine Prüfung je Schreiber müsste jeder neue wiederholen. Kosten: eine Primärschlüssel-Abfrage
   je Anfrage mit Token. Autonom gegrillt (`Defekt`, Freigabe 1).
2. **Nur das Konto, nicht `fid`/`cid` einzeln.** Beide Löschpfade (`AdultsController.Delete`,
   `ChildrenController.Delete`) entfernen jedes Konto, dessen Profile alle auf den gelöschten Datensatz zeigen.
   Kein Konto trägt heute Profile zweier verschiedener Personen. Eine eigene Prüfung von `fid`/`cid` wäre
   unerreichbar und darum nicht testbar.
3. Der `404` im eigenen `Delete` (`adult is null`) ist damit unerreichbar. Er bleibt als Absicherung stehen.

## Akzeptanzkriterien

1. Nach dem Löschen eines Erwachsenen liefert jede Anfrage mit seinem alten Token 401, nicht 500.
2. Dasselbe Token wirkt vor dem Löschen (Gegenprobe im Test).
3. Alle übrigen Tests bleiben grün. Das zweite Löschen mit dem alten Token ist jetzt 401 statt 404.

## Schätzung

**S**, `wo: backend`, keine Migration, kein Vertragsbruch (401 statt 500 für ein ungültiges Token).

## Verlauf

- **2026-10-23** — als `idee` aufgenommen (unverifiziert) aus der Nachschau zu Sprint 3 des ersten Laufs;
  nicht gemessen, weil der Sprint sich nicht erweitert.
- **2026-10-23** — Nachprüfung in der Backlog-Sichtung 2026-10-23 ([Protokoll](../pm-sitzung-2026-10-23-backlog-sichtung.md)) (am Code hergeleitet, nicht gemessen): die 500 ist plausibel – `AdultsController.Delete` (`:129-160`) löscht Adult und Konto, das JWT bleibt 12 h gültig ohne Prüfung je Request (`Program.cs:271-279`, `TokenService.cs:12,53`), `SubjectsController.Create` setzt `OwnerAdultId = fid` gegen eine echte FK (`PuglingDbContext.cs:471`), und für `DbUpdateException` gibt es keinen Handler. **Betroffen sind mehr Schreiber:** `TextbookSeriesController.cs:179`, `ExerciseCategoriesController.cs:132`, `CreatorProfilesController.cs:98`, `ExerciseControllerBase.cs:329,339-345`, `ChildrenController.cs:86`. Kein Id-Recycling (AUTOINCREMENT), also keine Kontoübernahme. Die Messung (Zu klären 1) ist nachtlauffähig; der Ort der Prüfung (zentral in `OnTokenValidated` vs. je Schreiber) ist eine technische Wahl, die der Code verengt (sechs+ Schreiber sprechen für zentral).
- **2026-10-26** — gemessen, autonom gegrillt (Freigabe 1, `Defekt`), geschätzt und gebaut in Lauf C ([Protokoll](../pm-sitzung-2026-10-26-tag.md), NP-3). Rote Probe: `DeletedAccountTokenTests` vor dem Fix → erwartet 401, gemessen **500**. Danach grün. Ein bestehender Test hatte das alte Token nach dem Löschen weiterbenutzt und 404 erwartet (`AdultLifecycleTests`); er ist umbenannt und erwartet jetzt 401, wie AK 3 sagt. Backend **921/921 `exit=0`**.
- **2026-10-26** — Review ([Protokoll](../pm-sitzung-2026-10-26-tag.md), Lauf C, NP-3): `pugling-reviewer` ohne Blocker. Gezählt: Der Kind-Löschpfad war ungetestet → neuer Fall `Token_Eines_Geloeschten_Kindes_Bekommt_401`, Probe „`ChildrenController.Delete` ohne Konto“ → **1 failed**. Zurückgestellt (B-282): Abbruch in `OnTokenValidated` loggt auf Error. Rollengang ohne Browser (Extension nicht verbunden): **Vater und Sohn** melden sich in jeder E2E-Spec an (`vaterLogin`/`sohnLogin`), Playwright voll **95 passed** nach dem letzten Produktions-Code-Edit (danach nur Tests und Kommentare). **Creator** (Lehrer-Konto) fährt `lehrer-konto.spec.ts` mit. Backend: Solution-Build 0 Warnungen, **936/936 `exit=0`**. Zähler NP-3 am Ende **5** (Grenze erreicht, nicht überschritten). Zurückgestellt → [B-282](B-282-login-haertung-restpunkte.md). **Abgenommen**; Commit: folgt im Nachtrag.
