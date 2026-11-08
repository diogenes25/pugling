---
tags: [typ/story, status/ausformuliert, bereich/frontend, bereich/reward, rolle/supervisor, rolle/student]
aliases: [Gems in Münzspalte, Buchungsverlauf ohne Währung]
status: ausformuliert
prio: P3
art: Defekt
groesse: ""
wo: beides
migration: ""
vertragsbruch: ""
quelle: docs/pm-sitzung-2026-11-08-rollengang.md#feedback-vater
unverifiziert: false
grund: ""
ersetzt_durch: []
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

## Verlauf

- **2026-11-08** — angelegt aus dem Rollengang Vater/Sohn ([Protokoll](../pm-sitzung-2026-11-08-rollengang.md)),
  Ist-Stand belegt (`ausformuliert`); Grillen und Schätzen im Nachtlauf (Defekt, Freigabe 1).
