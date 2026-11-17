---
tags: [typ/story, status/geschaetzt, bereich/lehrplan, bereich/ui, rolle/supervisor, rolle/student]
aliases: [Stundenplan-Pflicht Frontend, Taktung Stundenplan im Vater-Web, morgen Englisch]
status: geschaetzt
wartet_auf: "B-644 (Tagesplaner) – nicht bauen, bis B-644 gegrillt ist"
prio: P1
art: Wunsch
groesse: M
wo: frontend
migration: nein
vertragsbruch: nein
quelle: B-615
grund: ""
ersetzt_durch: []
---

# B-622 · Die Stundenplan-Pflicht im Vater-Web einstellen und in der Sohn-App begründen

Teil (c) aus [B-615](B-615-tagesmission-faechermix-nach-staerke-und-stundenplan.md), Frontend-Hälfte (dort gegrillt, beim Schätzen geteilt). Der Vater wählt die
Taktung „Stundenplan“ an einer Position und setzt die Obergrenze am Kind; der Sohn sieht an der Pflicht, warum
sie heute verlangt ist („morgen Englisch“). Voraussetzung: [B-621](B-621-stundenplan-pflicht-backend.md) ist gebaut.

## User Story

Als *Vater* möchte ich die Taktung „Stundenplan“ und die Obergrenze im Vater-Web einstellen und sehen, wenn die
Obergrenze überschritten ist, damit ich die Stundenplan-Pflicht ohne API-Aufrufe pflege. Als *Sohn* möchte ich
an einer Pflicht sehen, zu welcher Schulstunde sie gehört, damit ich verstehe, warum sie heute dran ist.

## Ist-Stand am Code

Gemessen am 2026-11-17 auf `main` (Stand `74d2f9f5`), Pfade relativ zu `frontend/src/`.

- **Die Taktung ist an mehreren Stellen von Hand geführt:** `vater/PlanPositions.tsx:31` (Beschriftungen) und
  `:34` (`CADENCES = ["Daily", "Weekly", "None"]`), Feld „Ziel-Rhythmus“ `:393` mit Feldhilfe
  (`lib/fieldHelp.ts:21`); in der Sohn-App `sohn/SohnHome.tsx:31` (Beschriftung) und `:79` (Zähler je Taktung),
  `sohn/pfad.ts:43` (Reihenfolge: erst `Daily`, dann `Weekly`) und `:61` (Wochenziel wartet auf den Sonntag).
- **Der Stundenplan hat schon eine Oberfläche:** `vater/ChildCarePanels.tsx:116` (`TimetableSection`, Anlegen
  `:126`, Löschen `:133`).
- **Die Kind-Seite des Vaters** ist `vater/VaterKind.tsx`; dort wird `BonusDailyCoinCap` gepflegt, das Vorbild
  für die Obergrenze.
- `lib/contract.ts` wird aus der API erzeugt; der vierte Wert kommt mit B-621.

## Die echte Lücke

Vier Stellen kennen nur drei Taktungen, die Obergrenze hat kein Feld, und die Pflicht zeigt ihren Grund nicht.

## Offene Punkte

Keine Produktfragen offen; entschieden in [B-615](B-615-tagesmission-faechermix-nach-staerke-und-stundenplan.md).

## Entscheidungen

Übernommen aus [B-615](B-615-tagesmission-faechermix-nach-staerke-und-stundenplan.md) (gegrillt und nachgegrillt am 2026-11-17 mit dem Vater), wörtlich; hier die, die
die Oberfläche binden. Alle Entscheidungen zu Teil (c) stehen in [B-621](B-621-stundenplan-pflicht-backend.md).

1. (B-615 Entscheidung 6) **Eigene Taktung „Stundenplan“** an der Position, neben `None`/`Daily`/`Weekly`: Pflicht am Wochentag des
   Fachs laut Stundenplan und am Tag davor, an den übrigen Tagen bleibt die Position spielbar ohne Pflicht.
   *Begründung:* eigene Periode, ehrlich abrechenbar, `Daily` bleibt unverändert. *Kosten:* neuer Enum-Wert im
   Vertrag (Vertragsbruch für Clients, die die Liste fest kennen), eigene Perioden in `ClosedPeriods`, und die
   Malus-Abrechnung muss den Stundenplan lesen.
   **Nachfrage:** Steht das Fach nicht im Stundenplan des Kindes, **weist der Server die Taktung ab**. Wird der
   Eintrag später gelöscht, gilt die Position wie `Daily`, damit sie nie lautlos pflichtfrei wird. *Kosten:*
   eine Querprüfung beim Anlegen und eine Rückfallregel bei der Abrechnung. Den Schnitt der Perioden
   regelt Entscheidung 11.
2. (B-615 Entscheidung 7) **Obergrenze: höchstens N Pflicht-Positionen pro Tag**, je Kind vom Vater gesetzt (Vorschlag 5). Der Server
   weist beim Anlegen oder Ändern ab, was die Grenze an **irgendeinem** Wochentag überschreiten würde (wegen der
   Stundenplan-Taktung ist die Zahl je Wochentag verschieden). *Begründung:* ohne Schätzmodell prüfbar.
   *Kosten:* Prüfung über alle aktiven Pläne des Kindes und alle sieben Wochentage, und ein Stundenplan-Eintrag,
   der die Grenze nachträglich reißt, braucht eine eigene Regel (Entscheidung 12).
3. (B-615 Entscheidung 12) **Ein Stundenplan-Eintrag, der die Obergrenze reißt, wird abgewiesen; das Löschen wird geduldet.** Hebt das
   Löschen (Rückfall auf `Daily`) einen Wochentag über die Grenze, zeigt das Vater-Web die Überschreitung an.
   *Begründung:* Der Vater muss einen falschen Stundenplan-Eintrag immer entfernen können. *Kosten:* Die Grenze
   ist nach einem Löschen nicht mehr hart, und der Stundenplan-Controller braucht dieselbe Prüfung wie die
   Position.
4. (B-615 Entscheidung 13) **Keine Sortierung nach Stundenplan.** Die Pflicht aus Entscheidung 5 mit dem Hinweis aus AK 8 genügt;
   B-220 sortiert nicht nach Stundenplan. *Begründung:* eine Wirkung des Stundenplans statt zwei. *Kosten:*
   Positionen des morgigen Fachs ohne Taktung „Stundenplan“ rücken nicht nach vorn.

## Akzeptanzkriterien

Final. AK 1 ist B-615 AK 8 an der Oberfläche; AK 2 bis 5 folgen aus den Entscheidungen oben und sind vom Vater
nicht einzeln bestätigt. Je Fall die Änderung, die seinen Test rot macht.

- **AK 1.** Die Sohn-App zeigt an einer Stundenplan-Pflicht den Hinweis auf die Stunde („morgen Englisch“ bzw.
  „heute Englisch“). *Rot, wenn die Pflicht ohne Grund erscheint.*
- **AK 2.** Im Vater-Web ist „Stundenplan“ als Ziel-Rhythmus wählbar, wird gespeichert und nach dem Neuladen
  angezeigt; die Feldhilfe erklärt Fachtag und Vortag. *Rot, wenn die Auswahl nur drei Werte kennt.*
- **AK 3.** Weist der Server die Taktung ab (Fach nicht im Stundenplan, Plan ohne Fach, Obergrenze), zeigt das
  Formular die Meldung am Feld, und die Eingabe bleibt erhalten. *Rot, wenn der Fehler verschluckt wird.*
- **AK 4.** Der Vater setzt die Obergrenze am Kind; ein abgewiesener Stundenplan-Eintrag zeigt seine Meldung im
  Stundenplan-Abschnitt, und eine Überschreitung nach einem Löschen erscheint als Hinweis auf der Kind-Seite.
  *Rot, wenn der Hinweis fehlt, obwohl die API ihn meldet.*
- **AK 5.** Die Sohn-Startseite zählt und ordnet eine Stundenplan-Pflicht an ihren Pflichttagen wie ein
  Tagesziel; an den übrigen Tagen erscheint sie nicht als Pflicht. *Rot, wenn sie als Wochenziel auf den Sonntag
  wartet oder gar nicht gezählt wird.*

## Schätzung

**Größe: M** — vier bekannte Stellen bekommen einen vierten Wert, dazu ein Feld am Kind, drei Fehlermeldungen
und ein Hinweis in der Sohn-App; kein neues Bauteil. Die Schätzung ist an den Dateien gemessen, aber **ohne die
fertige API aus B-621**; sie ist dort am dünnsten, wo die Form der Hinweise (Grund der Pflicht, Überschreitung)
erst mit B-621 feststeht.

- **`migration: nein`**, **`vertragsbruch: nein`** — reine Oberfläche; den Vertrag ändert B-621.

### Risiken

1. **Die Sohn-Startseite verzweigt hart nach Taktung** (`sohn/pfad.ts:43`, `:61`, `sohn/SohnHome.tsx:79`). Ein
   unbekannter Wert fällt heute durch alle Zweige; nach B-621 und vor dieser Story zeigt die Sohn-App eine
   Stundenplan-Pflicht also falsch oder gar nicht. **Beide Stories sollten zusammen ausgeliefert werden.**
2. **Übungstypen und Listen kommen sonst aus dem Server** (`frontend/CLAUDE.md`); die Taktung ist die Ausnahme
   mit handgeführter Liste. Ein fünfter Wert träfe dieselben vier Stellen wieder.
3. **Geteilte E2E-Datenbank:** Der Test braucht einen eigenen Stundenplan-Eintrag und darf andere Specs nicht
   stören (Auswahl nie per Index).

### Angriffsplan

1. `lib/contract.ts` nach B-621 neu erzeugen.
2. Vater-Web: vierter Wert in `PlanPositions.tsx` samt Feldhilfe; Fehlermeldungen am Feld.
3. Vater-Web: Obergrenze auf der Kind-Seite (über `useAction` und `StatusBanner`), Meldungen im
   Stundenplan-Abschnitt, Hinweis bei Überschreitung.
4. Sohn-App: Beschriftung, Zähler und Reihenfolge für die neue Taktung, Hinweis auf die Stunde.

### Testweg

- Vitest: `sohn/SohnHome.test.tsx` und ein Fall für `sohn/pfad.ts` (AK 1, AK 5); ein Formular-Test für
  `vater/PlanPositions.tsx` (AK 2, AK 3).
- Playwright: neue Spec `frontend/e2e/stundenplan-pflicht.spec.ts` (Vater legt Eintrag und Position an, Sohn
  sieht die Pflicht mit Grund). Gemessen wird an „N passed“, nicht am Exit-Code.
- `tsc` und Lint über die lokalen Binaries unter `frontend/node_modules/.bin`.
- Vor der Abnahme der Agent `frontend-reviewer`; die E2E zählt als Rollengang.

## Verlauf

- **2026-11-17** — angelegt als Frontend-Hälfte von Teil (c) der Teilung von B-615. Die AKs 2 bis 5 hat der
  Assistent aus den Entscheidungen abgeleitet, vom Vater nicht einzeln bestätigt. Geschätzt: M, `frontend`,
  keine Migration, kein Vertragsbruch; die Schätzung entstand ohne die fertige API aus B-621.
- **2026-11-17** — **Entscheidung des Vaters (Grill-Runde B-644, Entscheidung 6): angehalten.** Die Story wird nicht gebaut, bis [B-644](B-644-tagesplaner-statt-fester-lauf.md) gegrillt ist: der Tagesplaner verlegt die Pflicht von der Position an die Tagesaufgabe. Stufe und Entscheidungen bleiben bestehen; danach wird sie als Regel des Planers überführt oder verworfen.
