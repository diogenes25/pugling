---
tags: [typ/story, status/abgenommen, bereich/frontend, bereich/reward, rolle/supervisor, rolle/student]
aliases: [Gems in Münzspalte, Buchungsverlauf ohne Währung]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-08-rollengang.md#feedback-vater
unverifiziert: false
grund: ""
ersetzt_durch: []
nachgeschaut: "2026-11-09"
entgangen_bei: []
---

# B-425 · Der Buchungsverlauf zeigt Gem-Buchungen in der Spalte „Münzen"

Im Rollengang vom 2026-11-08 stand unter `/vater/konto` die Zeile „Auszeichnung · Willkommens-Gems · **+300**"
in der Spalte **Münzen** – der Münzstand daneben sagt 50. Dieselbe Liste in der Sohn-Arcade („Mein Konto")
zeigt „+300" ohne jede Währung. Wer dem Verlauf glaubt, rechnet mit 350 Münzen.

## User Story

Als *Vater* (und als *Sohn*) möchte ich im Buchungsverlauf je Zeile sehen, **welche Währung** gebucht wurde,
damit der Verlauf zum Kontostand passt.

## Ist-Stand am Code

- `VaterKonto.tsx:67` – Tabellenkopf `Datum · Kategorie · Grund · Münzen`; `:74` rendert `e.amount` ohne
  Währung. `SohnKonto.tsx` zeigt dieselben Einträge als „+300" (Rollengang).
- Die Währung ist eine reine Funktion des `PointKind` (`PointKindCurrency.cs:19-26`, Server) – **keine**
  Ledger-Spalte, bewusst (`backend/Pugling.Api/CLAUDE.md`, „Reward-Ökonomie").
- `PointsEntryResponse(Id, ChildId, Amount, Kind, Reason, CreatedAt)` (`ChildDtos.cs:39`) trägt die Währung
  nicht; im Frontend gibt es keine Abbildung `PointKind → Währung` (kein Treffer für `PointKindCurrency`,
  `currencyOf` in `frontend/src`).
- `labels.ts:8-25` kennt nur die Kategorie-Namen (`Achievement → „Auszeichnung"`, `Base → „Wiederholung"`); das
  Startguthaben ist im Seed als `PointKind.Base` gebucht (`Seed.cs:1061`) und heißt darum „Wiederholung".

## Die echte Lücke

Der Vertrag verschweigt dem Client die Währung, die der Server kennt. Eine Frontend-Kopie der Abbildung wäre
eine zweite Fassung einer Server-Regel – die veraltete gewinnt beim nächsten `PointKind`.

## Offene Punkte

Keine Produktfrage; Defekt, autonom grillbar. Empfehlung für die Entscheidung: additives Feld `Currency` auf
`PointsEntryResponse` (API-First, Quelle der Wahrheit bleibt `PointKindCurrency`), Spalte „Betrag" mit 🪙/💎 im
Vater-Konto und in der Sohn-Arcade; `DocsCaptureTests` neu erzeugen. Das Seed-Startguthaben als
`PointKind.Manual` („Papa-Buchung") statt `Base` buchen, damit die Kategorie nicht „Wiederholung" heißt
(Teil von [B-429](B-429-seed-familie-widerspricht-dem-katalog.md), hier nur erwähnt).

## Akzeptanzkriterien (Entwurf)

1. `GET supervisor/children/{id}/points` liefert je Eintrag `currency` (`Coins`/`Gems`), abgeleitet aus
   `PointKindCurrency.Of(kind)`.
2. Vater-Konto und Sohn-Konto zeigen „+300 💎" bzw. „+50 🪙"; der Spaltenkopf heißt „Betrag".
3. Regressionstest: ein Gem-Eintrag erscheint nie ohne 💎 (Vitest, Mutante: Symbol weglassen → rot).

## Entscheidungen

1. **Additives Feld `Currency` auf `PointsEntryResponse` und `MyPointsEntryResponse`**, abgeleitet aus `PointKindCurrency.Of(kind)`
   (Empfehlung der Story). Kein Ledger-Feld, keine Frontend-Abbildung Art → Währung. *Entscheidung im Lauf (Defekt), getroffen von Claude.*
2. **Anzeige über einen Helfer `signedAmount`** (`lib/labels.ts`) in Vater-Konto und Sohn-Arcade gleich; Spaltenkopf „Betrag". Das
   Symbol ist sprachneutral, darum ohne `t()`.
3. Die Umbuchung des Seed-Startguthabens gehört zu B-429 (dort gebaut, gleicher Sprint).

## Schätzung

**Größe: S** — zwei DTO-Felder, drei Projektionen, ein Helfer, zwei Anzeigen. Kein Schema, additiv. Testweg: `PointsCurrencyTests`,
`labels.test.ts`, E2E `konto-waehrung-und-datum.spec.ts` (Rollengang Vater + Sohn).

## Verlauf

- **2026-11-08** — angelegt aus dem Rollengang Vater/Sohn ([Protokoll](../pm-sitzung-2026-11-08-rollengang.md)),
  Ist-Stand belegt (`ausformuliert`); Grillen und Schätzen im Nachtlauf (Defekt, Freigabe 1).
- **2026-11-09** — gegrillt (Defekt, Freigabe 1), geschätzt und gebaut in Lauf AG, Sprint 27. **Rote Probe mit Zahl:** die drei neuen
  Integrationstests gegen den Ausgangsstand → 3 von 3 rot an `KeyNotFoundException` (`currency` fehlt).
  **Belege je Fall** – `.claude/scripts/mutant-probe.py`, gemessen nach dem letzten Edit an `453a740 + Arbeitsbaum`, 2026-11-09 15:44:

  | Fall | Test | Mutante | gemessen |
  |---|---|---|---|
  | AK 1 Vaterliste | `PointsCurrencyTests.Vaterliste_NenntDieWaehrungJeBuchung` | MB1 Liste fest „Coins" (`ChildrenController.cs:233`) | red, am Fall |
  | AK 1 Buchungs-Antwort | `…Buchen_AntwortetMitDerWaehrung` | MB2 POST-Antwort fest „Coins" (`:255`) | red, am Fall |
  | AK 1 Kindliste | `…Kindansicht_NenntDieWaehrungInListeUndEinzelbuchung` | MB3 (`MeController.cs:56`) | red, am Fall |
  | AK 1 Kind-Einzelbuchung | dieselbe | MB4 (`:73`) | red, am Fall |
  | AK 3 | `labels.test.ts` „zeigt Gems mit 💎" | MF1 Symbol fest 🪙 (`labels.ts:33`) | red, am Fall |
  | AK 2 Sohn | `e2e/konto-waehrung-und-datum.spec.ts` | ME1 Arcade ohne Helfer (`SohnKonto.tsx:38`) | red, am Fall |
  | AK 2 Vater | dieselbe | ME2 Vater-Konto ohne Helfer (`VaterKonto.tsx:76`) | red, am Fall |

  Grundlinie nach dem Zurückspielen grün.
- **2026-11-09** — Review: `pugling-reviewer` keine 🔴/🟡; 🟢 Gegenrichtung „fest Gems" an POST-Antwort und Kind-Einzelbuchung ungeprüft →
  Tests prüfen jetzt beide Währungen (ohne Zählung); 🟢 Tutorial ohne das Feld → `docs/tutorial-supervisor.md` ergänzt; 🟢 im
  OpenAPI-Dokument fiel beim Schema `Currency` ein `"default": 0` weg (Nebeneffekt des Generators, für ein String-Enum ohnehin falsch).
  `frontend-reviewer`: 🟡 das Minus in `signedAmount` widersprach seiner Doku und dem Assistenten (ASCII statt U+2212) → typografisches
  Minus, Test angepasst (**Zähler Sprint 27: 4**, siehe Protokoll). nachgemessen nach dem letzten Edit an `453a740 + Arbeitsbaum`, 2026-11-09 16:14 (`mutant-probe.py`, alle Zeilen der Tabelle oben red, dazu:)

  | Fall | Test | Mutante | gemessen |
  |---|---|---|---|
  | AK 1 Buchungs-Antwort, Gegenrichtung | `…Buchen_AntwortetMitDerWaehrung` | MB2b POST-Antwort fest „Gems" (`ChildrenController.cs:255`) | red, am Fall |
  | AK 1 Kind-Einzelbuchung, Gegenrichtung | `…Kindansicht_…` | MB4b fest „Gems" (`MeController.cs:73`) | red, am Fall |
  | Review: Minus | `labels.test.ts` „zeigt einen Abzug mit seinem Minus" | MF4 ASCII-Minus (`labels.ts:34`) | red, am Fall |

- **2026-11-09** — abgenommen. Backend 1183/1183, Vitest 715/715, `tsc -b` sauber, Playwright **125 passed** (ganze Suite, Server vom Lauf nach dem letzten Edit gestartet). **Rollengang** Vater und Sohn = `e2e/konto-waehrung-und-datum.spec.ts` Fall 1 (frisches Kind, Gem- und Münzbuchung: Vater-Konto „Betrag“ mit „+9 💎“/„+7 🪙“, Sohn-Arcade „Mein Konto“ „+9 💎“). Creator als Regressionszeuge: Suite grün, keine Creator-Fläche im Diff. Commit: siehe `git log -- docs/backlog/B-425-kontoverlauf-gems-in-muenzspalte.md` (Lauf AG, Sprint 27).
- **2026-11-09** — Nachschau (Retro Sprint 28, Lauf AG, frischer `frontend-reviewer`, `git show a14961d`). Prüfpunkte: MB1 (`ChildrenController.cs:233`), MB2/MB2b (`:255`), MB4/MB4b (`MeController.cs:73`) und MF4 (`labels.ts:34`) am Code nachgerechnet, rot am Fall; alle vier Projektionen tragen `currency`, keine Frontend-Abbildung Art → Währung. 🟢: Betrag 0 („0 🪙“) ungetestet, hingenommen; Zeilenangabe MF1 jetzt `labels.ts:35`.
