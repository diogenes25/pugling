---
tags: [typ/story, status/geschaetzt, bereich/frontend, rolle/vater]
aliases: [Assistent ohne Bewertung]
status: geschaetzt
prio: P3
art: Wunsch
groesse: S
wo: frontend
migration: nein
vertragsbruch: nein
quelle: docs/pm-sitzung-2026-11-05.md#lauf-ab
grund: ""
ersetzt_durch: []
---

# B-408 · Der Plan-Assistent zeigt in seiner Übungsauswahl keine Bewertung – sein Zwilling im Planbau schon

Seit [B-270](B-270-bewertung-im-positions-formular.md) zeigt die Übungsauswahl im Planbau (`PlanPositions.tsx`) je Zeile die Bewertung und
sortiert auf Wunsch „beste zuerst". Der Assistent (`VaterWizard.tsx`) nennt der Code ausdrücklich ihren Zwilling (gleiche Filterleiste,
gleiche Zeilen) – dort fehlt beides. Ein Vater sieht den Unterschied direkt. Dazu als Verfeinerung für beide: beim Umsortieren die Auswahl
behalten, wenn die gewählte Übung in der neuen Seite noch steht.

## User Story

Als **Vater im Plan-Assistenten** möchte ich je Übung ihre Bewertung sehen und „beste Bewertung zuerst" sortieren können,
damit ich dort dieselben guten Übungen finde wie im Planbau – und beim Umsortieren meine Auswahl nicht verliere.

## Ist-Stand am Code

- **Planbau** (`frontend/src/vater/PlanPositions.tsx`): importiert `ratingSummary` (`:18`), hält `besteZuerst` (`:448`),
  fragt mit `sort: "rating", dir: "desc"` (`:463-464`), bietet die Wahl „Trefferliste sortieren" (`:564-568`) und zeigt je
  Zeile `ratingSummary(ex.averageRating, ex.ratingCount)` (`:644`). `besteZuerst` steckt in `planbauKriterien` (`:838-846`)
  und damit im Auswahl-Schlüssel `filterKey` (`:480`) – **Umsortieren verwirft die gewählte Übung** über
  `auswahlNachFilterwechsel` (`:494-500`), auch wenn sie weiter in der Seite steht.
- **Assistent** (`frontend/src/vater/VaterWizard.tsx`): Suche über `wizardSearchParams` (`:140-148`), Schlüssel
  `wizardFilterKey` (`:133-136`), Zeilen in `WizardTrefferbereich` (`:687`, Zeilentext `:794`) – **ohne** Bewertung,
  ohne Sortierwahl. Mehrfachauswahl `selected` (`:89`); was außerhalb der geladenen Seite gewählt ist, zählt
  `unsichtbareAuswahl` und zeigt „davon N unten nicht sichtbar" (`:225`, `:511`).
- `frontend/src/vater/wizardSearch.ts`: `WizardFilter` und `wizardSearchParams` kennen kein `sort`/`dir`;
  `ExerciseSearchParams` (`frontend/src/lib/uiTypes.ts:61`) und der Server können es bereits (`ExerciseSortKey` mit
  `"rating"`, `uiTypes.ts:51`).
- Abdeckung: `frontend/src/vater/wizardSearch.test.ts` (reine Regeln), `frontend/e2e/planbau-bewertung.spec.ts` (B-270/B-412,
  Rollengang mit bewerteten Übungen), `frontend/e2e/assistent.spec.ts`.

## Die echte Lücke

Rein Frontend: der Assistent fragt nicht sortiert und zeigt die Bewertung nicht an, obwohl die `ExerciseSummary`-Zeilen sie
schon tragen. Und im Planbau wirft ein Sortierwechsel die Auswahl weg, obwohl sich die Treffermenge nicht ändert.

## Offene Punkte

1. ~~Bewertung und „beste zuerst" im Assistenten wie im Planbau – ja/nein?~~ → Entscheidung 1.
2. ~~Beim Umsortieren die Auswahl behalten – ja/nein, für beide Flächen oder nur den Assistenten?~~ → Entscheidungen 2 und 3.

## Entscheidungen

1. **Ja: Bewertung je Zeile und dieselbe Wahl „Trefferliste sortieren" (Katalog / beste Bewertung zuerst) im Assistenten.**
   `sort`/`dir` kommen über `wizardSearchParams` in die Abfrage **und** in „Alle wählen" (eine Quelle, B-18). Begründung: der
   Code nennt beide Flächen Zwillinge, der Vater sieht den Unterschied. Kosten: ein Feld mehr im Assistenten-Schritt.
2. **Assistent: Sortieren ist kein Suchkriterium.** Der Sortierwunsch geht nicht in `wizardFilterKey`, die Mehrfachauswahl
   bleibt beim Umsortieren vollständig; was dabei aus der Seite rutscht, zählt das vorhandene „davon N unten nicht
   sichtbar". Begründung: die Treffermenge ist dieselbe, und „Alle wählen" kann bis 500 gewählt haben – nur die Sichtbaren
   zu behalten, verwürfe stumm die meisten. Kosten: `wizardFilterKey` und Abfrage-Abhängigkeit trennen sich (Sortieren
   lädt neu, verwirft aber nicht) – die Regel wird in `wizardSearch.ts` ausgeschrieben und getestet.
3. **Planbau: die Auswahl bleibt, wenn die gewählte Übung in der neu geladenen Seite steht, sonst fällt sie mit dem
   vorhandenen Hinweis.** Begründung: die Formulierung der Story; die Einzelauswahl darf nach B-167 nie auf eine Übung
   zeigen, die der Vater nicht sieht. Kosten: die Entscheidung fällt erst nach dem Laden der neuen Seite, nicht beim
   Wechsel – eine reine Funktion in `wizardSearch.ts` (`auswahlNachUmsortieren`), geteilt mit dem Zwilling.

Alle drei: Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13,
[Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)). Damit ist die in Lauf AF vermisste Freigabe (Verlauf 2026-11-09)
für diese Story erteilt.

## Akzeptanzkriterien

1. `wizardSearchParams` mit „beste zuerst" liefert `sort: "rating", dir: "desc"`, ohne den Wunsch keines von beiden.
   *Rot macht ihn:* der Sortierwunsch erreicht die Abfrage nicht (oder `"asc"`).
2. `wizardFilterKey` ist für denselben Filter mit und ohne „beste zuerst" **gleich**. *Rot macht ihn:* Sortierung im
   Schlüssel – dann verwirft der Assistent die Auswahl beim Umsortieren.
3. `auswahlNachUmsortieren([7], [3, 7, 9])` behält 7 ohne Hinweis; `auswahlNachUmsortieren([7], [3, 9])` leert mit Hinweis.
   *Rot macht ihn:* immer leeren (heutiger Planbau) bzw. immer behalten (B-167 kehrt zurück).
4. E2E Assistent: zwei eigene Übungen, eine von einem zweiten Konto bewertet; die Zeile zeigt die Bewertung, „beste
   Bewertung zuerst" stellt sie vor die unbewertete, eine vorher gewählte Übung bleibt gewählt. *Rot macht ihn:* Zeile ohne
   `ratingSummary`, Sortierwahl ohne Wirkung, oder Auswahl beim Umsortieren verworfen.
5. E2E Planbau: gewählte Übung, die nach „beste zuerst" weiter in der Seite steht, bleibt gewählt (Absende-Knopf bedienbar).
   *Rot macht ihn:* `besteZuerst` im Auswahl-Schlüssel ohne die neue Regel (heutiger Stand).

## Schätzung

**Größe: S** — zwei Felder durch eine vorhandene reine Regel fädeln, eine neue reine Funktion, Zeilentext erweitern; kein
Backend. Grenzwertig zu M nur, falls der Planbau-Umbau der Abfrage-Abhängigkeit mehr als die Schlüssel-Trennung braucht.

- **Risiken:** Abfrage-Abhängigkeit und Auswahl-Schlüssel laufen bisher über **denselben** Wert (B-164); getrennt muss die
  Abfrage weiter auf Sortierwechsel neu laden, sonst zeigt die Liste die alte Reihenfolge. `exercises.stale` darf nach dem
  Umsortieren nicht dauerhaft „veraltet" melden.
- **Angriffsplan (kein Backend):** (1) `wizardSearch.ts`: `besteZuerst` in `WizardFilter`/`wizardSearchParams`,
  `wizardFilterKey` ohne Sortierung, `auswahlNachUmsortieren` – Vitest zuerst rot (AK 1–3); (2) `VaterWizard.tsx`:
  Sortierwahl und `ratingSummary` in `WizardTrefferbereich`, Abfrage-Abhängigkeit um den Sortierwunsch ergänzen;
  (3) `PlanPositions.tsx`: Auswahl-Schlüssel ohne `besteZuerst`, nach dem Laden `auswahlNachUmsortieren`; (4) E2E.
- **Testweg:** Vitest `frontend/src/vater/wizardSearch.test.ts` (AK 1–3) und ggf. `PlanPositions.test.ts`; E2E
  `frontend/e2e/planbau-bewertung.spec.ts` um AK 5 erweitern und eine Assistenten-Fassung daneben (AK 4, Muster derselben
  Spec); Lauf über `./node_modules/.bin/vitest run` und `./node_modules/.bin/playwright test`, gezählt wird „N passed".
  `frontend-reviewer` vor der Abnahme.

## Verlauf

- **2026-11-05** — angelegt aus dem Review zu B-270 (Lauf AB, Sprint 19; `frontend-reviewer`, 🟡/🟢). Bewusst nicht im Sprint: B-270 nennt nur
  den Planbau, und eine zweite Fläche ist Produktumfang (`Wunsch`), kein Herstellen.
- **2026-11-09** — von Lauf AF übergangen (Paket NP-26): `idee` + `Wunsch` ohne eigene Freigabe-Zeile – Freigabe 9 gilt
  nur für die Story, in der sie steht (die Zeile in B-424 deckt diese Nachbarin nicht). Gebraucht wird: (1) Bewertung und
  „beste zuerst" im Assistenten wie im Planbau – ja/nein; (2) beim Umsortieren die Auswahl behalten – ja/nein, und für
  beide Flächen oder nur den Assistenten. B-424 wurde ohne sie gebaut (die Reihenfolge war Komfort, keine Abhängigkeit).
- **2026-11-13** — ausformuliert (Backlog-Lauf): Ist-Stand belegt; zusätzlich gefunden, dass der Planbau beim Umsortieren
  die Auswahl verwirft (`besteZuerst` im Auswahl-Schlüssel, `PlanPositions.tsx:480`, `:838-846`).
- **2026-11-13** — gegrillt: Bewertung + Sortierwahl im Assistenten; Auswahl dort beim Umsortieren voll behalten, im Planbau
  nur, wenn sichtbar. Stakeholder-Entscheidung im Auftrag des Nutzers (Backlog-Lauf 2026-11-13,
  [Protokoll](../pm-sitzung-2026-11-13-backlog-lauf.md)).
- **2026-11-13** — geschaetzt: S, frontend, keine Migration, kein Vertragsbruch.
- **2026-11-13** — Schnitt im Backlog-Lauf: Angriffsplan-Schritt 3 (`PlanPositions.tsx`) und das zugehörige Planbau-Kriterium gehen an [B-540](B-540-planbau-sortierwechsel-verwirft-auswahl.md), weil die parallele B-534-Arbeit diese Datei unkommittiert umbaut. Diese Story baut nur den Assistenten (`VaterWizard.tsx`, `wizardSearch.ts`) samt `auswahlNachUmsortieren`.
