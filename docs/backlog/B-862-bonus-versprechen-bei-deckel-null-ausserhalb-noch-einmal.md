---
tags: [typ/story, status/abgenommen, bereich/gamification, rolle/student]
aliases: [Bonus-Versprechen bei Deckel null]
status: abgenommen
prio: P3
art: Defekt
groesse: S
wo: beides
migration: nein
vertragsbruch: nein
quelle: "Review von B-845 im Nachtlauf 2026-11-23-c (docs/pm-sitzung-2026-11-23-c.md, frontend-reviewer 🟡)"
grund: ""
ersetzt_durch: []
nachgeschaut: ""
entgangen_bei: [B-660]
wartet_auf: ""
---

# B-862 · Bonus-Training verspricht Münzen an drei weiteren Stellen, auch wenn der Deckel 0 ist oder erreicht

B-845 hat den Satz im leeren Üben berichtigt; dasselbe Versprechen steht an drei Stellen der Sohn-App, die einen
Bonus-Deckel von 0 bzw. einen erreichten Deckel nicht kennen:

- **Runden-Hinweis** `frontend/src/sohn/SohnBonus.tsx:329-335`: hängt nur an `run.runPercent`; der Server
  (`ExtraTrainingService.cs:284`, `RunPercent`) kennt den Deckel dort nicht. Der Sohn liest „Jede richtige Antwort bringt
  N 🪙“ und bekommt am Ende „Diesmal ohne Münzen“.
- **Startseiten-Teaser** `SohnBonus.tsx:68` (`BonusEinstieg`, `SohnHome.tsx:145`): „Extra-Münzen für extra Übung …“ auch
  bei Deckel 0; `TodayResponse` trägt keinen Deckel.
- **Listenzeilen** `SohnBonus.tsx:145`: der Münzsatz je Treffer steht unter der Karte „heute keine Münzen“.

Gelesen am Code (Review 2026-11-23), nicht nachgestellt. `entgangen_bei: B-660`, weil dessen Leerzustand das Versprechen
neu in den Weg gelegt hat.

## User Story

Als Sohn möchte ich, dass mir das Bonus-Training nirgends Münzen verspricht, wenn der Vater den Deckel auf 0 gesetzt hat
oder er heute voll ist, damit ich der App glaube.

## Entscheidungen

*Stellvertretend entschieden im Nachtlauf 2026-11-23-c (Defekt, Freigabe 1), unbestätigt.*

1. **Runden-Hinweis:** `ExtraRunResponse` bekommt `CoinsLeftToday` (`int?`, `null` = ohne Deckel, additiv), berechnet beim
   Start; bei 0 zeigt der Hinweis `bonus.hintNoWorth`. API-First statt eines zweiten Abrufs der Übersicht auf der
   Rundenseite. Kosten: ein Feld; der Wert ist ein Schnappschuss vom Start (die Abgabe zahlt ohnehin höchstens den Rest).
2. **Listenzeilen:** am erreichten Deckel (auch 0) zeigt eine wiederholbare Zeile `bonus.vocabNoWorth` statt des Satzes je
   Treffer; Zeilen fester Form bleiben, sie sagen schon „wartet“.
3. **Startseiten-Teaser:** `BonusEinstieg` lädt geöffnet die Bonus-Übersicht (Muster `NochEinmal`, Beiwerk: Ladefehler →
   alter Satz) und nimmt dann die vorhandenen Sätze `bonus.capZero`/`bonus.capReached`. Kein Vertragsfeld an
   `TodayResponse` – der Teaser ist der einzige Leser, und ein Feld an der Tagesübersicht wäre teurer. Kosten: ein
   zusätzlicher Abruf auf der Startseite, solange das Bonus-Training offen ist.

## Akzeptanzkriterien

1. Eine Runde bei Deckel 0 meldet `coinsLeftToday: 0`, bei Deckel 3 `3`; der Hinweis sagt bei 0 „keine Münzen“, ohne
   Deckel den Satz je Treffer.
2. Der offene Teaser sagt bei Deckel 0 `capZero`, am vollen Deckel `capReached`, mit Münzen übrig den alten Satz.
3. Die Vokabelzeile nennt am vollen Deckel keinen Münzsatz, mit Münzen übrig schon.

## Schätzung

S, beides, keine Migration, additiv. Testweg: `ExtraTrainingTests.Runde_Nennt_Die_Heute_Verbleibenden_Muenzen`,
`SohnBonus.test.tsx` („Bonus-Versprechen am leeren Deckel“).

## Verlauf

- **2026-11-23** — angelegt aus dem Review von B-845 (Nachtlauf 2026-11-23-c), bewusst nicht im Sprint 118.
- **2026-11-23** — ausformuliert, gegrillt (stellvertretend, Defekt), geschätzt und gebaut im Nachtlauf 2026-11-23-c
  (Sprint 120). Rote Probe Backend: `Runde_Nennt_Die_Heute_Verbleibenden_Muenzen` erwartet rot, gemessen rot (2/2, Feld
  fehlte). Frontend nach dem Bau gemessen (Mutanten statt Vorher-Lauf):
  - AK 1 → Backend B1 (`CoinsLeftToday = null`), B2 (fester Wert 30) → rot; Frontend G1 (Abfrage `=== 0` aus) → rot.
  - AK 2 → G4 (`capZero`-Zweig aus), G5 (`capReached`-Zweig aus), G6 (Teaser immer `capZero`) → rot.
  - AK 3 → G2 (Zeile ohne Deckel-Abfrage), G3 (Zeile immer „ohne Münzen“) → rot.
  - Ungemessen: die Abbildung „ohne Deckel → `null`“ (`int.MaxValue`); die Testfabrik hat immer einen Deckel.
  Review (`frontend-reviewer`, `pugling-reviewer`): 🟡 der Teaser zeigte beim Laden kurz „Extra-Münzen“ → im Ladezustand
  kein Satz (**gezählt**); 🟡 der Gegenfall des Teasers prüfte nur den Ladezustand → wartet auf die Antwort (**gezählt**);
  🟡 der Backend-Test ließ den aufgebrauchten Deckel aus → Fall 3−2=1 (**gezählt**; Zähler Sprint 120: 3/5); 🟡 das
  Ergebnisblatt kündigte die nächste Form mit Münzsatz an → am vollen Deckel nur der Formname (außerhalb des Diffs, ohne
  Zählung, aber in diese Story genommen); 🟢 Wortlaut des `///` (Start oder Fortsetzen, Server-Vorgabe), lesbareres `with`
  → behoben. Nachgemessen nach der letzten Änderung: G1–G9, B1–B3 rot (G7 Ladezustand, G8/G9 Vorschau, B3 Rest statt
  Deckel neu).
- **2026-11-23** — abgenommen (Sprint 120, Commit HASH). Verifikation nach der letzten Änderung: Backend 2134/2134
  (Release), Vitest 1371/1371, `tsc -b` sauber; Rollengang Sohn per E2E gegen frisch gestartete Server (5211/5184):
  `bonus`, `bonus-deckel`, `bonus-deckel-voll`, `bonus-satz-je-karte`, `nochmal-nach-pflicht`, `full-flow` – 6 passed.
  Der Fall „Deckel 0 auf der Startseite“ ist nur auf Komponentenebene gegangen (keine Spec setzt den Deckel auf 0 und
  öffnet danach die Startseite). Vater Regressionszeuge (`bonus-deckel`), Creator Regressionszeuge (Suite grün).
