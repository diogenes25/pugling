---
tags: [typ/story, status/abgenommen, bereich/backend, bereich/api, bereich/qualitaet]
aliases: [generischer Conflict, Conflict-Tor, Platzhalter-Tor, Paging-Obergrenze]
status: abgenommen
prio: P3
art: Aufräumen
groesse: S
wo: backend
migration: nein
vertragsbruch: nein
quelle: docs/api-design-bewertung.md (Vorschläge A3, A5, B3-Tor) — Arbeitsrunde PM/API-Designer/Entwickler am 2026-08-04
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-08-07"
wartet_auf: ""
---

# B-101 · Drei generische Fehlercodes ersetzen — und die drei Wächter, die daraus reif geworden sind

Drei Controller-Stellen melden ein nacktes `conflict`, wo ein fachlicher Code hingehört (einer davon
existiert bereits und wird zwei Dateien weiter benutzt). Der Wert der Reparatur liegt weniger in den drei
Zeilen als darin, dass danach ein Tor mit **leerer** Ausnahmeliste gezogen werden kann. Dazu zwei weitere
Wächter, die die Arbeitsrunde von fünf vorgeschlagenen als reif befunden hat.

## User Story

Als **Entwickler** möchte ich, dass ein fachlicher Konflikt einen eigenen `code` trägt und dass eine Regel,
die das sichert, mechanisch hält statt an Disziplin — damit die vierte Stelle nicht wieder generisch wird.

## Ist-Stand am Code

- `Controllers/AuthController.cs:174` benutzt `ApiErrors.Conflict`, obwohl `ApiErrors.DuplicateEmail`
  existiert (`Errors/ApiErrors.cs:103`) und in `Controllers/Supervisor/AdultsController.cs:57,78` benutzt wird.
- `Controllers/Creator/ExerciseCategoriesController.cs:66` und `:92` benutzen `Conflict`, wo der analoge
  Kapitel-Fall einen eigenen Code hat.
- Damit hat `ApiErrors.Conflict` **genau drei** Vorkommen in Controllern — die Ausnahmeliste eines Tors wäre
  nach der Reparatur leer.
- **Der Pfad hat eine Oberfläche** (in der Runde bestritten und dann gemessen): `PATCH auth/me` wird aus
  `frontend/src/vater/VaterProfil.tsx` über `api.ts:251` gerufen, für beide Kontoarten begründet in
  `VaterApp.tsx:104`.
- **Aber niemand verzweigt auf den Code:** das Frontend verzweigt auf insgesamt **drei** Codes
  (`SohnPractice.tsx:197`, `ExercisePreviewModal.tsx:42`, `VaterAnmerkungen.tsx:73`); `duplicate_email`
  kommt nur als generierter Union-Wert in `contract.ts` vor, in keiner Bedingung.
- Pfad-Platzhalter: `exercises` trägt `{id}` **und** `{exerciseId}`, `media` `{id}` und `{assetId}`,
  `vocabulary` `{id}` und `{vocabularyId}`, `tags` `{id}` und `{tagId}` (Stellen im Bericht, Dimension 1).
  `ApiSurface.RouteOf:60` und `RouteParameters:79` existieren und liefern das Material für ein Tor.

## Die echte Lücke

Der heutige Schaden ist **null** — kein Client verzweigt auf `duplicate_email`. Die Lücke ist die fehlende
Mechanik: solange drei Stellen generisch melden dürfen, ist „jeder fachliche Fehler hat einen Code" eine
Absicht und keine Zusicherung. Darum ist die Reparatur hier `art: Aufräumen` und nicht `Defekt`, und der
eigentliche Gegenstand sind die Tore.

## Ergebnis der Arbeitsrunde vom 2026-08-04

**Drei Zeilen + ein neuer Code:** `DuplicateEmail` in `AuthController` einsetzen, `duplicate_category_name`
additiv in `ApiErrors` ergänzen und in `ExerciseCategoriesController` einsetzen. Vorschätzung **XS**,
`wo: backend`, keine Migration, kein Vertragsbruch (ein `code` wird *spezifischer*, das ist für einen Client
additiv).

Von den **fünf** im Bericht vorgeschlagenen Wächtern sind nach der Runde **drei** übrig:

1. **Kein generischer `Conflict` im Controller — reif, Ausnahmeliste leer.**
   Wörtlich: *„Keine Datei unter `Controllers/**` nennt `ApiErrors.Conflict`. Ausnahmeliste: leer."*
   Muster: Quelltext-Prüfung wie `ConventionGuardTests.cs:31`.
   **Nicht** auf `NotFound`/`BadRequest` ausdehnen: `ApiErrors.NotFound` hat 5 berechtigte Vorkommen
   (u. a. `StudyPlansController.cs:85,131`, wo die 404 bewusst das Eigentum verschleiert), und jede
   Ausnahme hätte einen eigenen Grund — eine Liste, die jeden Eintrag einzeln begründen muss, ist eine
   Doku, kein Tor.
2. **Ein Platzhaltername je Sammlung — reif, aber nur als gepinnte Rot-Liste.**
   Wörtlich: *„Für jedes Sammlungs-Segment folgt in allen Routen höchstens **ein** Platzhaltername.
   Rot-Liste (Stand 2026-08-04, jeder Eintrag ist eine Schuld, keine Ausnahme): `exercises`, `media`,
   `vocabulary`, `tags`."* Das Tor verhindert **neue** Abweichungen und fasst die 37 Routen nicht an; die
   im Bericht vorgeschlagene Umbenennung ist **zurückgezogen** (37 Routen + `CreatedAtAction`-Routenwerte +
   Client + Frontend + Tutorials für Lesbarkeit, die niemand braucht — und eine *halbe* Umbenennung wäre
   schlimmer, weil die verbliebene Uneinheitlichkeit dann absichtlich aussieht).
   **Auflage aus der Runde:** die Rot-Liste trägt Tripel `(Segment, geduldeter Zweitname, Grund)`, keine
   nackten Wörter. Sonst benennt der, der sie in einem Jahr abarbeitet, vier Routen **falsch** um: bei
   `media` ist `{id}` vs. `{assetId}` echte Schuld, `{linkId}` aber ein `MediaLink`
   (`exercises/{exerciseId}/media/{linkId}`) und korrekt; bei `vocabulary` ist `{id}` vs. `{vocabularyId}`
   Schuld, `{exerciseId}` aber die Vokabel-**Übung** unter `chapters/…` und korrekt.
3. **Paging-Tor — die Form ist die offene Entscheidung.** Beide Rollen haben ihre Position aus Runde 1
   aufgegeben (Details in [B-99](B-99-kaufhistorie-endet-lautlos.md), Entscheidung 3). Übrig sind zwei
   Formen, und der Mensch entscheidet:
   - **(a) Enge Regel, kurze Liste:** *„Ein Array-GET **ohne Routenparameter** (Top-Level-Sammlung) hat
     `take`."* Trifft 10 Endpunkte, davon 3 begründete Ausnahmen (`exercise-types` = Manifest,
     `profiles/match` = ein Treffer, `profiles` = wenige) ⇒ 7 zu reparieren. Fängt die vier *scoped*
     Wachstumsfälle nicht.
   - **(b) Gepinnte Zahl:** *„Genau 35 Array-liefernde GETs im Dokument tragen kein `take`; jede Änderung
     der Zahl braucht eine bewusste Zeile."* Kein Eintrag je Ausnahme, rot bei jeder neuen ungepagten Liste.
     **PM-Anmerkung, die keine der beiden Rollen hat:** das trägt **nur als exakte Pinnung**, nicht als
     Obergrenze („höchstens 35"). Eine Obergrenze verrottet genau wie die festen Untergrenzen des
     Client-Routen-Wächters: sinkt die Wirklichkeit auf 20, lässt „höchstens 35" fünfzehn neue ungepagte
     Listen stumm durch. Eine exakte Zahl ist rot in **beide** Richtungen — das ist die Form der Tore
     G1–G9.
   - Empfehlung: **(a) und (b) zusammen** — (a) repariert und hält den Normalfall, (b) fängt den Rest, ohne
     eine 33-Zeilen-Ausnahmeliste zu pflegen.

**Zurückgezogen (nicht Teil dieser Story, Begründung in den Nachbarstories):** das Unique-Index-Tor (siehe
[B-97](B-97-unique-index-ohne-vorpruefung.md), Entscheidung 3) und das `summary`-Tor, das erst **nach**
[B-100](B-100-vertragsdokument-unterdeklariert.md) gezogen werden darf — vorher ist es kein Tor, sondern ein
blockierter Build.

## Entscheidungen

Eigene Liste, eigene Nummerierung — verweist im Text auf die drei Wächter aus „Ergebnis der Arbeitsrunde"
oben (dort als 1–3 nummeriert) über ihre Namen, nicht über ihre Nummer.

1. **Wächter 2 (Platzhaltername) und 3 (Paging) nach [B-121](B-121-platzhalter-und-paging-tore.md)
   abgespalten — Wächter 1 (kein generischer `Conflict`) bleibt hier.** Begründung: Beim Implementieren von
   Wächter 2 (die Rot-Liste-Auflage aus dem obigen Punkt 2) fiel eine **dritte, im Bericht nicht erfasste**
   Inkonsistenz auf derselben Fehlerklasse auf — das Sammlungs-Segment `units` trägt sowohl `{unitId}`
   (`SeriesUnitsController`, eigene Route `textbook-series/{seriesId}/units`) als auch `{seriesUnitId}`
   (`ExerciseRoutes.Base` in `Controllers/Creator/ExerciseControllers.cs:17`, von allen 13
   Übungstyp-Controllern geerbt). Diese Rot-Liste korrekt zu bauen heißt jetzt: alle vier Segmente
   (`exercises`, `media`, `vocabulary`, `units`) gegen die *tatsächliche* Route-Oberfläche verifizieren,
   nicht gegen die Bericht-Prosa — und `tags` dazu, macht fünf. Wächter 3 (Paging) braucht dieselbe
   Sorgfalt (die 35 exakt zu zählenden Endpunkte sind eine Momentaufnahme, kein Vorschlag). Beides in
   derselben Sitzung wie der risikoarme Code-Fix zu bauen hätte den ganzen Fortschritt an einem einzigen,
   ungeprüften mechanischen Tor riskiert (README: „ein Fund beim Bauen wird eine eigene Story, wenn das Ziel
   der laufenden ohne ihn erfüllt ist"). Kosten: B-101s AK3/AK4 werden zu B-121, `groesse` sinkt von M auf S;
   der `units`-Fund geht als Ist-Stand in B-121 mit, nicht verloren.

## Akzeptanzkriterien

1. `ApiErrors.Conflict` kommt unter `Controllers/**` nicht mehr vor; ein Tor mit leerer Ausnahmeliste hält
   das.
2. `PATCH auth/me` mit einer belegten E-Mail antwortet mit `code: duplicate_email`; ein Kategorie-Konflikt
   mit `code: duplicate_category_name`.
3. Alles so grün wie vorher — kein Verhalten ändert sich (Abnahmeform für `art: Aufräumen`), abgesehen von
   den zwei spezifischeren Codes.

**Nach [B-121](B-121-platzhalter-und-paging-tore.md) abgespalten** (siehe „## Entscheidungen"): das
Platzhalter-Tor (ehem. AK3) und das Paging-Tor (ehem. AK4).

## Schätzung

**Größe S** (Anker: „`childId` aus dem Test-Pfad ziehen", B-01, plus ein neuer `ApiErrors`-Code und ein
Wächter-Test etwas größer) — `wo: backend`. `migration: nein`. `vertragsbruch: nein`: ein Code wird
*spezifischer*, das ist für einen Client additiv (kein Feld verschwindet, kein Status ändert sich).

### Testweg

`ConventionGuardTests.Controller_Nennt_Keinen_Generischen_Conflict_Code` (neu, Ausnahmeliste leer) plus die
bestehenden `AccountSelfServiceTests.FremdeEMail_WirdAbgewiesen` und `DocsCaptureTests`-Capture „Doppelte
Art anlegen" (beide bereits vorhanden, Erwartung auf den neuen Code umgestellt statt eines neuen Tests).

## Verlauf

- **2026-08-04** — angelegt aus `docs/api-design-bewertung.md` (A3, A5, B3-Tor) und der Arbeitsrunde. Von
  fünf vorgeschlagenen Toren sind drei übrig; die Tripel-Auflage für die Rot-Liste und die exakte statt
  obere Pinnung sind Ergebnisse der zweiten Runde.
- **2026-08-06** — gegrillt, geschätzt und (im reduzierten Umfang AK1–3) abgenommen: autonom, Nachtlauf-
  Freigabe 1 (`art: Aufräumen`). `DuplicateEmail`/`DuplicateCategoryName` eingesetzt, neuer Wächter gegen
  `ApiErrors.Conflict` unter `Controllers/**` (leere Ausnahmeliste, self-protection ≥30 Dateien). Zwei
  bestehende Tests (`AccountSelfServiceTests`, `DocsCaptureTests`) mussten auf den spezifischeren Code
  umgestellt werden — das *ist* der belegte Vorher/Nachher-Beweis, kein neuer roter Test nötig. Volle Suite
  grün (Zahl im Sprint-Protokoll `docs/pm-sitzung-2026-08-06.md`). AK3/AK4 nach B-121 abgespalten.
- **2026-08-07** — Nachschau (Nachtlauf): geprüft, ob `ApiErrors.Conflict` weiterhin unter `Controllers/**`
  fehlt und der Wächter mit leerer Ausnahmeliste noch aktiv ist — hält (0 Treffer auf `ApiErrors\.Conflict\b`
  unter `Controllers/`, `ConventionGuardTests.cs:73-90`). Kein Fund.
