---
tags: [typ/story, status/geschaetzt, bereich/frontend, rolle/creator, rolle/supervisor, rolle/student]
aliases: [Aufbau-Weg im Editor, Stufenband, Vater wählt Ziel-Level]
status: geschaetzt
prio: P1
art: Wunsch
groesse: M
wo: frontend
migration: nein
vertragsbruch: nein
quelle: B-536
unverifiziert: false
grund: ""
ersetzt_durch: []
wartet_auf: ""
---

# B-704 · Oberfläche: Vorstufen im Übungseditor, Ziel-Level im Positionsformular, Stufenband beim Kind

Teil 5 von 5 aus [B-536](B-536-aufbau-uebungen-statt-versagen.md): jeder Backend-Endpunkt braucht ein UI
(`frontend/CLAUDE.md`). Hängt an [B-700](B-700-aufbau-stufe-am-katalog.md) und
[B-701](B-701-position-spielt-die-aufbau-stufe.md); die Wahl der Kadenz für gestufte Positionen wird erst
freigeschaltet, wenn [B-702](B-702-abstieg-und-pflichtziel-der-aufbau-stufe.md) gebaut ist (B-701, Entscheidung 6).

## User Story

Als *Vater* möchte ich beim Anlegen einer Position sehen, ob zur Übung Vorstufen existieren, und mit einem Feld
festlegen, auf welcher Stufe mein Kind beginnt. Als *Creator* möchte ich im Übungseditor eine Übung als Vorstufe einer
anderen markieren. Als *Sohn* möchte ich sehen, auf welcher Stufe ich bin und wie viele noch kommen.

## Ist-Stand am Code

- Der Übungseditor ist `frontend/src/vater/ExerciseEditModal.tsx`; er trägt schon Felder wie `DefaultStage`
  (`frontend/src/vater/DefaultStageSelect.test.tsx`) und `grammarTopicIds`. Er schreibt die Vollrepräsentation
  (Voraussetzung von B-700, AK 3).
- Das Positionsformular ist `frontend/src/vater/PlanPositions.tsx` (Test `PlanPositionsStufe.test.tsx` deckt die
  Spielform-Stufe ab, nicht die Aufbau-Stufe).
- Die Frontend-Typen sind aus der OpenAPI-Beschreibung erzeugt (`frontend/src/lib/contract.ts`); neue Felder kommen
  mit dem Neuerzeugen, nicht von Hand. Unbekannte Felder lehnt der Server mit `unknown_field` ab (`lib/types.ts:6`).
- Die Sohn-Oberfläche zeigt Übung und Stufe der Spielform (`frontend/src/sohn/SohnPractice.tsx`,
  `SohnTest.tsx`); eine Anzeige „Stufe n von m" gibt es nicht. Nicht gefunden: eine Stelle, die Vorstufen auflistet.

## Entscheidungen

*Stellvertretend entschieden im Vorbereitungslauf 2026-11-19 (Sonnet), unbestätigt.* Geerbt aus B-536:
Entscheidung 1 (der Vater wählt Thema und Ziel-Level, nicht jede Einzelübung) und Entscheidung 5 (kein „zu schwer"-Knopf
fürs Kind).

1. **Der Vater sieht im Positionsformular nur dann ein Feld „Start-Stufe", wenn die gewählte Übung Vorstufen hat.**
   Begründung: Positionen ohne Vorstufen bleiben unverändert (B-701 AK 2), das Formular wächst nicht für jeden.
   Verworfene Alternative: das Feld immer zeigen – ein totes Feld ist die teuerste Sorte Bedienfehler. Kosten: eine
   Abfrage der Vorstufen je gewählter Übung. Schließt aus: Vorstufen anlegen im Positionsformular selbst.
2. **Der Creator markiert Vorstufen im Übungseditor mit zwei Feldern: „Vorstufe von" (Auswahl der Ziel-Übungen derselben
   Unit) und „Stufe" (1 oder 2).** Begründung: spiegelt B-700 1:1, die Auswahl ist auf die Unit begrenzt, damit die
   Validierung des Servers selten greift. Kosten: ein Editor-Abschnitt mehr, E2E-Pflicht (Normen: Editor beidseitig,
   Anlegen und Zurücklesen). Schließt aus: eine eigene Seite „Lernwege".
3. **Das Kind sieht ein Stufenband („Stufe 2 von 3") an der Kopfzeile von Üben und Test; kein Bedienelement.**
   Begründung: B-536 Entscheidung 5 – Information ja, ein Knopf nein. Kosten: Platz in der Kopfzeile am Handy.
   Schließt aus: dass das Kind die Stufe selbst wählt.
4. **Die Vater-Auswertung zeigt die aktuelle Stufe je gestufter Position; die Auswertung „Zeit auf der Stufe" bleibt
   in B-702.** Begründung: die Anzeige braucht nur `CurrentBuildLevel`, die Zeit auf der Stufe braucht die Entscheidung
   zum Abstieg. Kosten: die Auswertung ist bis dahin kürzer als B-536 es wünscht.

## Akzeptanzkriterien

1. Im Übungseditor lässt sich eine Übung als Vorstufe einer anderen derselben Unit mit Stufe 1 anlegen; nach dem
   Speichern und Neuladen zeigt der Editor beide Werte. Rot macht ihn: ein Feld, das im Payload fehlt (der Server
   antwortet `unknown_field` oder der Wert kommt leer zurück).
2. Das Auswahlfeld „Vorstufe von" bietet nur Übungen der eigenen Unit an. Rot macht ihn: eine Auswahl, die nicht nach
   `seriesUnitId` filtert.
3. Wählt der Vater im Positionsformular eine Übung **ohne** Vorstufen, erscheint kein Feld „Start-Stufe". Rot macht
   ihn: ein Feld, das immer sichtbar ist.
4. Wählt er eine Übung **mit** Vorstufen, erscheint es, und der gewählte Wert steht nach dem Speichern in der
   Position. Rot macht ihn: ein Formular, das den Wert nicht an die API schickt.
5. Das Sohn-Üben und der Sohn-Test zeigen „Stufe n von m" nur bei einer gestuften Position; bei einer ungestuften
   nichts. Rot macht ihn: ein Band, das auch für `null` gezeichnet wird.
6. Die Kadenz-Auswahl einer gestuften Position bietet bis B-702 nur „keine" und „bis erledigt" an. Rot macht ihn: ein
   Formular, das Tages- oder Wochenpflicht zulässt.
7. Die Felder sind per Tastatur erreichbar und haben Beschriftungen (A11y-Norm). Rot macht ihn: ein Feld ohne Label.

## Schätzung

**Größe: M** — wie der vokabel-basierte Batch-Pfad im `MediaSelector`: zwei Formularabschnitte, eine Anzeige, kein
neuer Endpunkt. Aufwand steckt in Editor-Rundlauf und E2E, nicht im Rechnen.

- **`migration: nein`** — kein Schema; alles Frontend.
- **`vertragsbruch: nein`** — nachgesehen: B-700/B-701 ergänzen nur trailing optionale Felder; das Frontend zieht über
  das erneute Erzeugen von `lib/contract.ts` nach, kein bestehendes Feld ändert sich.
- **`wo: frontend`** — die Backend-Arbeit liegt vollständig in B-700 und B-701 (Backend zuerst).

### Risiken

- **Editor beidseitig:** wer ein Feld nur schreibt und nicht zurückliest, verliert die Stufe still beim nächsten PUT
  (Vollrepräsentation, B-700 AK 3). AK 1 prüft genau den Rundlauf.
- **Mobile Kopfzeile:** das Stufenband konkurriert mit Zeitanzeige und Fortschritt (`SohnPractice.tsx`).
- **Kadenz-Sperre (AK 6)** ist eine Übergangsregel; sie muss mit B-702 wieder fallen, sonst bleibt sie als Altlast.
- **Nicht enthalten:** die Auswertung „Zeit auf der Stufe" und jeder Abstiegshinweis (B-702).

### Angriffsplan

1. `lib/contract.ts` aus der laufenden Spezifikation neu erzeugen (nach B-700/B-701).
2. Editor: Abschnitt „Aufbau" in `ExerciseEditModal.tsx` mit Schreib-Primitiv (`useAction` + `StatusBanner`).
3. Positionsformular: Feld „Start-Stufe" und die Kadenz-Einschränkung in `PlanPositions.tsx`.
4. Sohn: Stufenband in `SohnPractice.tsx`/`SohnTest.tsx`.
5. Vitest-Komponententests, dann E2E.

### Testweg

- Neue Komponententests `ExerciseEditModalAufbau.test.tsx` (AK 1, 2) und `PlanPositionsAufbau.test.tsx` (AK 3, 4, 6)
  in `frontend/src/vater/`, `SohnStufenband.test.tsx` in `frontend/src/sohn/` (AK 5).
- Neue E2E-Spec `frontend/e2e/aufbau-stufen.spec.ts` (Muster `grammatik-themen.spec.ts`): Creator legt Vorstufe an
  (Rundlauf), Vater legt Position mit Start-Stufe an, Sohn sieht das Stufenband, besteht den Test und sieht
  „Stufe 2 von 2". Das ist zugleich der Rollengang. Playwright-Lauf über `./node_modules/.bin/playwright`, nur
  „N passed" zählt.
- Vor der Abnahme: Agent `frontend-reviewer` und `pugling-reviewer` (B-700/B-701), `/smoke-test`.

## Verlauf

- **2026-11-19** — Teil von B-536, geteilt und geschätzt im Vorbereitungslauf (Sonnet): M, frontend, keine Migration,
  kein Vertragsbruch. Entscheidungen stellvertretend, unbestätigt. Wartet fachlich auf B-700 und B-701; Kadenz-Sperre
  fällt mit B-702.
- **2026-11-19** — Entscheidungen bestätigt. Stellvertretend auf Anweisung des Nutzers („nimm für alle Fragen deine Empfehlung“): die Empfehlung der Sonnet-Entscheidungen wurde übernommen.
