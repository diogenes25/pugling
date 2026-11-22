---
tags: [typ/story, status/abgenommen, bereich/katalog, rolle/creator]
aliases: [Geleerter Band einer Unit bleibt stehen]
status: abgenommen
prio: P3
art: Defekt
groesse: XS
wo: beides
migration: nein
vertragsbruch: nein
quelle: "Review von B-791 im Nachtlauf 2026-11-23-a (docs/pm-sitzung-2026-11-23-a.md, frontend-reviewer 🟡)"
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: "2026-11-22"
---

# B-800 · Ein geleerter „Band" einer Unit wird als gespeichert gemeldet und bleibt stehen

Die Zahlenfeld-Schwester von [B-791](B-791-geleerte-felder-ausserhalb-des-shops.md): `UnitForm` schickt einen geleerten
Band als `grade: null` (`frontend/src/vater/VaterLehrwerke.tsx:800`), `SeriesUnitsController.UpdateUnit` übernimmt nur
einen gesetzten Wert (`if (dto.Grade.HasValue)`, `:126`), und `UpdateSeriesUnitDto` hatte keinen Schalter. Die Oberfläche
meldet „Gespeichert.", der alte Band bleibt. Die Suche von B-791 lief nur über Textfelder (`trim() || null`) und hat ihn
darum nicht gesehen.

## User Story

Als Creator/Vater möchte ich den Band einer Unit wieder leeren können, damit eine Unit, die keinem Band zugeordnet ist,
auch keinen trägt – ein leeres Feld nach „Gespeichert." muss leer bleiben.

## Ist-Stand am Code

- Client: `VaterLehrwerke.tsx:800` (`grade: … === "" ? null : Number(…)`), ein DTO für Anlegen und Bearbeiten.
- Server: `SeriesUnitsController.cs:126`, `TextbookSeriesDtos.cs:66` (kein `ClearGrade`).
- Gegenprobe der Zahlenfelder in `frontend/src/vater` (`=== "" ? null`): Kind (`VaterKind.tsx:216-219`) und Bonus-Deckel
  (`:356-361`) nutzen `clear…`-Schalter, Positionen ebenso (`PlanPositions.tsx:393-402`), Fachlehrer im Bearbeiten-Zweig
  ebenso; `ExerciseEditModal` ist ein Vollersatz-PUT. Kein weiterer Fall.

## Entscheidungen

*Stellvertretend entschieden im Nachtlauf 2026-11-23-a (Defekt, Freigabe 1 und 3: Fund im selben Sprint), unbestätigt.*

1. **`ClearGrade` im `UpdateSeriesUnitDto`** (optional, Vorgabe `false` – additiv, kein Vertragsbruch), Wert zuerst,
   Schalter danach, wie in `backend/CLAUDE.md` („PATCH-Semantik"). Für ein `int?` gibt es kein `""`; ein Schalter ist der
   Weg des Repos (`ClearGrade` am Kind und an der Klassenarbeit). Kosten: ein Feld mehr im Vertrag;
   `PatchSemanticsTests.Jeder_ClearSchalter_Ist_Belegt` verlangt seine Belegung – er ist eingetragen.
2. **Das Formular schickt den Schalter nur, wenn ein vorhandener Band geleert wurde.**

## Akzeptanzkriterien

1. PATCH `clearGrade: true` leert den Band; `grade: null` allein lässt ihn stehen; `grade` und `clearGrade` zusammen
   enden leer. Rot ohne die Zeile im Controller.
2. Unit bearbeiten, Band leeren, Speichern → der PATCH trägt `clearGrade: true`. Rot ohne den Schalter im Formular.

## Verlauf

- **2026-11-22** — angelegt aus dem Review von B-791 (frontend-reviewer 🟡, außerhalb des Diffs, darum ohne Zählung),
  ausformuliert, gegrillt (stellvertretend) und geschätzt (XS, beides, additiv) im Nachtlauf 2026-11-23-a; nach Freigabe 3
  im selben Sprint 115 gebaut. Rote Probe vor dem Fix: `B800_Band_Einer_Unit_Laesst_Sich_Leeren` erwartet rot, gemessen
  rot (1/1; das Feld war unbekannt → 400).
  - AK 1 → `PatchClearFieldTests.B800_Band_Einer_Unit_Laesst_Sich_Leeren` und `PatchSemanticsTests.Clear_Schalter_Leert_Und_Gewinnt`
    (Fall `UpdateSeriesUnitDto/clearGrade`), M6 (Zeile im Controller entfernt) → rot.
  - AK 2 → `VaterFelderLeeren.test.tsx` „eine Unit schickt einen geleerten Band über den Schalter", T6 (ohne Schalter) → rot;
    E2E `vater-felder-leeren` („Band nach dem Leeren"), E2 (= T6) → rot. Grundlinien grün.
- **2026-11-22** — abgenommen (`d9af0c17`, Sprint 115). Verifikation: Backend 2028/2028 (Release, nach der letzten Änderung),
  Vitest `src/vater` 549/549, `dotnet format Pugling.sln --verify-no-changes` sauber. Rollengang Vater per E2E
  `vater-felder-leeren` gegen frisch gestarteten Server (5211/5184, Wegwerf-DB): Plan-Beschreibung, Unit-Grammatik, Band und
  Audio-URL nach dem Neuladen leer. Sohn Regressionszeuge (`full-flow` grün; die Audio-URL erreicht die Karte nur gesetzt
  oder `null`). Creator Regressionszeuge (Vokabel-Store und Units sind Creator-Fläche; dieselbe E2E fährt sie).
- **2026-11-22** — nachgeschaut (Retro Sprint 116). Prüfpunkte: `d9af0c17` sendet `clearGrade` nur im Bearbeiten-Zweig
  (`VaterLehrwerke.tsx:819`), das Anlegen bekommt das DTO ohne den Schalter (sonst `unknown_field`); der Agent
  (`UnitNotesFiller.cs:142`) schickt `null` und lässt damit alles stehen, was er nicht setzt; `AudioUrlOrNull` ist der
  einzige Schreiber der Audio-URL (Review-Befund). Kein Befund.

