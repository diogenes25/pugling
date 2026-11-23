---
tags: [typ/story, status/abgenommen, bereich/frontend, rolle/supervisor, rolle/student]
aliases: [Start-Stufe im Positionsformular, Stufenband]
status: abgenommen
prio: P1
art: Wunsch
groesse: M
wo: frontend
migration: nein
vertragsbruch: nein
quelle: B-704
unverifiziert: false
grund: ""
ersetzt_durch: []
wartet_auf: ""
---

# B-841 · Start-Stufe im Positionsformular, Stufenband beim Kind, Stufe in der Vater-Auswertung

Teil von [B-704](B-704-aufbau-weg-in-der-oberflaeche.md) (geteilt). Der Rest der Oberfläche zur Aufbau-Stufe; er
braucht [B-701](B-701-position-spielt-die-aufbau-stufe.md), weil Start-Stufe und Stufenband `CurrentBuildLevel` an der
Position lesen und schreiben.

## User Story

Als *Vater* möchte ich beim Anlegen einer Position sehen, ob zur Übung Vorstufen existieren, und festlegen, auf welcher
Stufe mein Kind beginnt. Als *Sohn* möchte ich sehen, auf welcher Stufe ich bin und wie viele noch kommen.

## Entscheidungen

Geerbt aus B-704 (am 2026-11-19 bestätigt): **Entscheidung 1** (Feld „Start-Stufe" nur, wenn die Übung Vorstufen hat),
**Entscheidung 3** (Stufenband „Stufe n von m" in der Kopfzeile von Üben und Test, kein Bedienelement),
**Entscheidung 4** (Vater-Auswertung zeigt die aktuelle Stufe; „Zeit auf der Stufe" bleibt in B-702).

## Akzeptanzkriterien

B-704 AK 3, 4, 5, 6 wörtlich (siehe dort), dazu AK 7 für die neuen Felder.

## Schätzung

**Größe: M** – wie in B-704 geschätzt, abzüglich des Editors (B-840). Testweg wie B-704: `PlanPositionsAufbau.test.tsx`,
`SohnStufenband.test.tsx`, E2E `aufbau-stufen.spec.ts` als Rollengang.

## Verlauf

- **2026-11-22** — Aus B-704 geteilt (Nachtlauf 2026-11-22-c, Opus). Wartet auf B-701; der Zuschnitt von B-701 ist
  offen (siehe dort, Nachprüfung vom 2026-11-22).
- **2026-11-22** — B-701 ist geteilt (B-848, B-849); wartet jetzt auf B-848, das Stufenband nach einem Aufstieg auf B-849.
- **2026-11-23** — Hinweis aus dem Review von B-849 (Nachtlauf E): Seit B-849 kann ein Aufstieg auf den nächsten Lerntag
  warten (`PlanPosition.BuildLevelEffectiveOn`). `PositionResponse` gibt dann die rohe `CurrentBuildLevel` (neu) neben
  `Build` (heute gespielt) aus, ohne den Unterschied zu erklären; ein trailing optionales `BuildLevelEffectiveOn` im
  Vertrag gehört hierher. Der Vater kann einen wartenden Aufstieg auch nicht „sofort“ setzen: dieselbe Stufe zu senden ist
  keine Änderung.
- **2026-11-23** — in-arbeit (Nachtlauf E, Sprint 124); B-849 ist gebaut (`2bf20d88`). Gebaut: `StartLevelField`
  (nur bei Vorstufen, Optionen „ohne Aufbau-Weg“, je Vorstufen-Stufe, Ziel; beschriftet nach dem Platz auf dem Weg),
  `cadencesFor(…, staged)` und `withStaged` (ein verdrängtes Tages-/Wochenziel kommt zurück, wenn der Weg wegfällt), die
  Stufe in Positionszeile und Report (`aufbauStufeText`), in der Sohn-App `useRundenKopf` (Buchbezug und Stufenband aus
  einer Lesung der Übersicht) im Kopf von Üben und Test, i18n `pfad.buildLevel`. Bewusst **nicht** gebaut: die Anzeige
  eines auf morgen wartenden Aufstiegs (keine Entscheidung dazu) und das Ändern der Stufe einer bestehenden Position im
  Bearbeiten-Formular (AK 3–7 verlangen es nicht) – beides in [B-882](B-882-stufe-im-positions-bearbeiten.md).
- **2026-11-23** — Review `frontend-reviewer` (nur gelesen): keine 🔴, zwei 🟡 behoben (Fehlerzähler 2): (1) der
  erzwungene Rhythmus „frei“ blieb stehen, wenn der Weg wegfiel (Stufe „ohne Weg“, andere Übung) – eine still
  pflichtfreie Position; jetzt merkt `withStaged` das verdrängte Ziel und gibt es zurück, Tests in
  `StartLevelField.test.ts` und im E2E; (2) „Stufe“ hieß im Feld die Katalog-Stufe (1/2/3), in Zeile und Band der Platz
  auf dem Weg („1 von 2“) – jetzt beschriftet das Feld nach dem Platz, Feldhilfe angeglichen. 🟢 ohne Zählung: die
  Abwesenheits-Prüfung im E2E kommentiert (der frühe Render trägt die Vitest-Probe); die im Schätztext genannten Dateien
  `PlanPositionsAufbau.test.tsx`/`SohnStufenband.test.tsx` heißen `StartLevelField.test.ts` bzw. liegen in
  `SohnPractice.test.tsx`/`SohnTest.test.tsx`; kein Ladehinweis, solange die Unit des Felds lädt (ein schneller Vater
  legt dann eine Position ohne Weg an – hingenommen).
- **2026-11-23** — AK → Test, Mutante → rot (gemessen nach der letzten Änderung, 15/15, Grundlinie grün): AK 3 kein Feld
  ohne Vorstufe → `StartLevelField.test.ts` „hat ohne Vorstufen keine Optionen“ (F1), E2E `aufbau-stufen.spec.ts`
  (F11 Feld immer sichtbar); AK 4 Wert erreicht die Position → E2E (F10 Stufe nicht im Payload), Optionen nur der eigenen
  Vorstufen (F2), Beschriftung nach dem Platz (F15); AK 5 Band nur an gestufter Position → `SohnPractice.test.tsx` /
  `SohnTest.test.tsx` (F7 Band ohne Weg, F8/F9 Band fehlt); AK 6 Rhythmus → `StartLevelField.test.ts` (F3, F4b, F5) und
  das verdrängte Ziel (F12 Helfer, F13 Stufenfeld, F14 Übungswechsel – die beiden letzten über das E2E); Entscheidung 4
  Vater-Auswertung → `aufbauStufeText` (F6). AK 7 (Beschriftung, Tastatur): natives `select` mit `FieldLabel`, im E2E
  über `getByLabel("Start-Stufe", { exact: true })` angesprochen – ohne eigene Mutante. Vitest 1383/1383, `tsc -b` grün,
  E2E 13 passed (`aufbau-stufen`, `planbau-ladefenster`, `punkte-empfehlung`, `position-zurueck-auf-standard`,
  `nacharbeit`, `pfad`, `lektion`, `full-flow`; Ausweichports).

| Mutante | Stelle | erwartet | gemessen | Anmerkung |
|---|---|---|---|---|
| F1 Optionen ohne Vorstufe | frontend/src/vater/StartLevelField.tsx:22 | red | red |  |
| F2 fremde Vorstufen | frontend/src/vater/StartLevelField.tsx:20 | red | red |  |
| F3 Rhythmus ungesperrt | frontend/src/vater/PlanPositions.tsx:44 | red | red |  |
| F5 Nacharbeit-Abwahl aufs Tagesziel | frontend/src/vater/PlanPositions.tsx:50 | red | red |  |
| F6 Zusatz fehlt | frontend/src/vater/StartLevelField.tsx:75 | red | red |  |
| F7 Band auch ohne Weg | frontend/src/sohn/buchbezug.ts:30 | red | red |  |
| F8 Band fehlt im Üben | frontend/src/sohn/SohnPractice.tsx:529 | red | red |  |
| F9 Band fehlt im Test | frontend/src/sohn/SohnTest.tsx:254 | red | red |  |
| F10 Stufe nicht im Payload | frontend/src/vater/PlanPositions.tsx:755 | red | red |  |
| F11 Feld immer sichtbar | frontend/src/vater/StartLevelField.tsx:57 | red | red |  |
| F4b Weg verdraengt nicht | frontend/src/vater/PlanPositions.tsx:60 | red | red |  |
| F12 Ziel kommt nicht zurueck | frontend/src/vater/PlanPositions.tsx:64 | red | red |  |
| F13 Stufenfeld vergisst Ziel | frontend/src/vater/PlanPositions.tsx:743 | red | red |  |
| F14 Uebungswechsel vergisst Ziel | frontend/src/vater/PlanPositions.tsx:865 | red | red |  |
| F15 Katalogstufe als Beschriftung | frontend/src/vater/StartLevelField.tsx:30 | red | red |  |

Grundlinie nach dem Zurückspielen: green.

- **2026-11-23** — abgenommen (Nachtlauf E, Sprint 124), Commit `34d6becc`. Rollengang: E2E `aufbau-stufen.spec.ts`
  gegen echten Server und echtes UI – Vater legt die Position mit Start-Stufe an, Sohn sieht „Stufe 1 von 2“ im Kopf der
  Runde. Ob das Band am Handy gut sitzt (Kopfzeile bei 360 Pixeln), ist ein Blick für einen Menschen.
