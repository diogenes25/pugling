---
tags: [typ/story, status/geschaetzt, bereich/frontend, rolle/supervisor, rolle/creator]
aliases: [Hausaufgaben im Vater-Web, Schulzugang einrichten]
status: geschaetzt
wartet_auf: "Bestätigung der Sonnet-Annahmen unter Schätzung; baubar erst nach B-654"
prio: P2
art: Wunsch
groesse: M
wo: frontend
migration: nein
vertragsbruch: nein
quelle: B-651
grund: ""
ersetzt_durch: []
---

# B-655 · Vater-Web: WebUntis-Zugang je Kind einrichten, Hausaufgaben ansehen, Übung daraus erstellen

Dies ist der Oberflächen-Teil aus [B-651](B-651-webuntis-hausaufgaben-als-pflicht.md). Die Story hängt an der
API aus [B-654](B-654-webuntis-connector-backend.md) und wird erst nach ihr gebaut (API-First).

## User Story

Als **Supervisor** möchte ich im Vater-Web den WebUntis-Zugang eines Kindes einrichten und prüfen und seine
Hausaufgaben sehen. Als **Creator** möchte ich aus einer Hausaufgabe direkt eine Übung erstellen, ohne den
Text abzuschreiben.

## Ist-Stand am Code

Steht mit Belegen (`Datei:Zeile`) in [B-651 → Ist-Stand am Code](B-651-webuntis-hausaufgaben-als-pflicht.md#ist-stand-am-code)
(Stand `main` 2adb3744, 2026-11-18). Vor dem Bauen gegen den dann aktuellen Code prüfen.

## Entscheidungen

Übernommen aus [B-651 → Entscheidungen](B-651-webuntis-hausaufgaben-als-pflicht.md#entscheidungen). Hier
tragen vor allem 2 (das Geheimnis kann nur geschrieben werden, die Oberfläche zeigt nur „eingerichtet“),
4 (Abruf beim Öffnen) und 8 (Fach als Text plus `subjectId?`).

## Akzeptanzkriterien

Ursprung ist B-651 Nummer 6, ein einziges Kriterium, das fünf Dinge zugleich verlangte und im Ausgangszustand
nichts trug. Beim Schätzen am 2026-11-19 in fünf geteilt, je mit der Änderung, die den Test rot macht.

1. **Zugang einrichten.** Im Reiter „Schule“ der Kind-Seite (`?bereich=schule`) setzt der Vater Server, Schule,
   Benutzer, Passwort und Schüler-ID, speichert und löscht den Zugang. Die Felder tragen je ein `FieldLabel`.
   *Rot, wenn „Speichern“ ohne Passwort beim ersten Einrichten möglich ist oder „Löschen“ keinen Aufruf auslöst.*
2. **Das Geheimnis kommt nie zurück.** Nach dem Speichern und nach `page.reload()` ist das Passwortfeld leer, und
   die Seite zeigt „eingerichtet“ mit Benutzer und letztem Abruf. Der gesetzte Wert steht an keiner Stelle im DOM.
   *Rot, wenn das Feld nach dem Neuladen mit dem Wert gefüllt ist oder der Wert irgendwo im Seitentext steht.*
3. **Hausaufgaben ansehen.** „Abrufen“ lädt die Liste für den gewählten Zeitraum und zeigt je Aufgabe Fach,
   Fälligkeit, Text und die Zustände erledigt und verpasst; ohne Zugang gibt es den Knopf nicht. *Rot, wenn „verpasst“
   und „erledigt“ nicht unterscheidbar sind oder ein Kind ohne Zugang einen Abruf anbietet.*
4. **Fehler am Platz, in Klartext.** Antwortet der Server mit `school_login_failed` oder `school_unreachable`,
   steht an der Liste eine verständliche Meldung (nicht der rohe Code, nicht „Fehler“), und der Zugang bleibt
   bearbeitbar. *Rot, wenn die Meldung fehlt oder die Seite danach leer und stumm bleibt.*
5. **Übung daraus erstellen.** Jede Aufgabe hat einen Weg nach `/vater/exercises/neu`; dort ist das Fach
   vorgewählt (nur wenn die Aufgabe ein `subjectId` trägt, sonst bleibt es leer) und der Aufgabentext steht in der
   Beschreibung. *Rot, wenn das Fach bei einer Aufgabe ohne `subjectId` trotzdem gesetzt ist oder der Text fehlt.*

## Schätzung

**Größe: M** – wie der Batch-Pfad im `MediaSelector`: ein neuer Abschnitt im Reiter „Schule“ nach dem Muster
von `TimetableSection` (`frontend/src/vater/ChildCarePanels.tsx:116`), eine Liste mit Zeitraum, ein Einstieg in die
Übungserstellung. Keine neue Logik außer der Zuordnung Fehlercode → Text, kein Schema.

- **`wo: frontend`** – schreibt nur unter `frontend/`. Das Backend liefert B-654.
- **`migration: nein`** – kein Schema berührt.
- **`vertragsbruch: nein`** – nur Lesen und Schreiben neuer, additiver Endpunkte (`school-account`,
  `school-homework`); `frontend/src/lib/contract.ts` wird aus `docs/openapi/v1.json` erzeugt (gitignored) und
  zieht nach B-654 per `npm run gen:contract` nach.

### Ist-Stand, nachgemessen am 2026-11-19

- Im Code gibt es noch nichts davon: die Suche nach `school-account`, `school-homework` und `WebUntis` unter
  `backend` und `frontend/src` findet nichts.
- Der Reiter „Schule“ ruft `ChildMaterialSection` und `TimetableSection`
  (`frontend/src/vater/VaterKind.tsx:94-97`); beide halten Zustand lokal und mutieren über `useAction`.
- Die Übungserstellung liest nur `subjectId`, `seriesId` und `seriesUnitId` aus der Query
  (`frontend/src/vater/VaterExerciseCreate.tsx:49-51`); `title` und `description` sind lokaler Zustand ohne
  Vorbefüllung (`:69-70`). Ein Aufgabentext aus der Query ist also **neu zu bauen**, nicht vorhanden.
- E2E läuft gegen den echten Server ohne Möglichkeit, den Connector zu ersetzen; ein Stub des Abrufs geht über
  `page.route` (Vorbild `frontend/e2e/assistent-klassenarbeit.spec.ts:132`).

### Annahmen (stellvertretend im Vorbereitungslauf 2026-11-19, Sonnet, unbestätigt)

1. **Der Aufgabentext geht in die Beschreibung der Übung (`description`), nicht in den Titel.** Verworfen: Titel
   – der ist kurz und wird im Katalog gelistet, ein ganzer Hausaufgabentext (oft mehrere Sätze) sprengte ihn.
   Kosten: Der Vater tippt den Titel selbst. Ausgeschlossen: ein Katalog voller Titel wie „S. 80 Nr. 1+2“.
2. **Abruf nur auf Knopfdruck, mit einem Zeitraum von heute minus 7 bis heute plus 30 Tage als Vorgabe.** Die Story
   sagt „Abruf beim Öffnen“ (B-651 Entscheidung 4); ein Abruf beim bloßen Öffnen des Reiters ginge bei jedem
   Reiterwechsel live in die Schule. Verworfen: automatisch beim Öffnen. Kosten: ein Klick mehr. Ausgeschlossen:
   unbemerkte Abrufe, die den Zugang der Kinder bei einer Schule mit Sperre nach Fehlversuchen belasten.
3. **Ort ist der Reiter „Schule“**, weil dort Buch, Fachlehrer und Stundenplan stehen; B-651 nennt nur „im Vater-Web“.

### Risiken

1. **Hängt an B-654.** Die Feldnamen der DTOs und die Fehlercodes stehen erst dort; AK 1 bis 5 sind gegen die
   gebaute API nachzusehen. Gebaut wird erst nach B-654 (API-First), und das OpenAPI-Dokument muss neu erzeugt sein.
2. **Das Geheimnis im Browser:** Das Passwortfeld braucht `autoComplete="new-password"`, sonst füllt der Browser
   das Elternpasswort in andere Felder; der Wert gehört nicht in `localStorage` oder in die URL.
3. **Die Schule ist langsam oder weg:** Der Abruf hängt an einem fremden Dienst. Die Liste braucht einen
   Ladezustand und `disabled={busy}` am Knopf (`frontend/CLAUDE.md`), sonst entstehen Doppelabrufe.
4. **Fach ohne Treffer** (B-651 Entscheidung 8, „Franz.“): Die Aufgabe wird angezeigt, aber ohne Fach-Vorwahl; die
   Seite darf das nicht als Fehler zeigen.
5. **Geteilter Text im DOM:** Hausaufgabentext sind fremde Daten; als Text rendern, nie als HTML.

### Angriffsplan

Voraussetzung: **B-654 gebaut** und `npm run gen:contract` gelaufen.

1. `frontend/src/lib/api.ts`: Aufrufe für `school-account` (GET/PUT/DELETE) und `school-homework` (GET `from`/`to`).
2. Abschnitt `SchulZugangSection` im Reiter „Schule“: Formular mit `FieldLabel` (Einträge in `fieldHelp.ts`),
   `useAction` + `StatusBanner`, `disabled={busy}`, Passwortfeld leer nach dem Speichern.
3. Liste `HausaufgabenListe` mit Zeitraum, Abruf-Knopf, Ladezustand, Fehlertext je Code, „Übung erstellen“ als Link.
4. `VaterExerciseCreate.tsx`: Query `description` (Annahme 1) einlesen und `subjectId` nur setzen, wenn vorhanden.
5. E2E, danach `tsc`, Lint und Vitest über die lokalen Binaries (`./node_modules/.bin/…`).

### Testweg

- `SchulZugang.test.tsx` (neu, Vitest, Vorbild `VaterKind.test.tsx`): Passwortfeld leer nach dem Speichern und nach
  Neuladen der Antwort (AK 2); „Speichern“ gesperrt ohne Passwort beim Einrichten (AK 1); Fehlertext je Code (AK 4);
  `verpasst` ≠ `erledigt` in der Zeile (AK 3). Rote Probe je Fall: Passwort aus der Antwort ins Feld spiegeln;
  Code-Zuordnung durch einen Einheitstext ersetzen.
- `VaterExerciseCreate`-Test (neu, Vitest): Query mit `description` und ohne `subjectId` füllt nur die Beschreibung
  (AK 5). Rot, wenn ein Standardfach gesetzt wird.
- `schul-hausaufgaben.spec.ts` (neu, E2E, Vorbild `bonus-deckel.spec.ts`): Vater richtet den Zugang über die echte
  API ein, `school-homework` per `page.route` mit einer namenfreien Antwort gestubbt; Reload zeigt „eingerichtet“
  bei leerem Passwortfeld (AK 2), die Liste erscheint (AK 3), „Übung erstellen“ landet im Formular mit Beschreibung
  (AK 5). Der echte Abruf gegen die Schule ist B-654 AK 6, nicht diese Story.
- Vor der Abnahme: Agent `frontend-reviewer` und der Rollengang als Vater mit dem Zugang von Friedrich im Heimnetz.

## Verlauf

- **2026-11-18** — angelegt durch Teilen von B-651 beim Schätzen (Vater: Variante A, drei Stories nach
  Schicht); Entscheidungen aus B-651 übernommen, damit auf `gegrillt`.
- **2026-11-19** — geschaetzt (Vorbereitungslauf, Sonnet): M, frontend, keine Migration, kein Vertragsbruch. Das eine AK aus B-651 in fünf mit je einer roten Änderung geteilt; nachgemessen, dass die Query der Übungserstellung keinen Text kennt (neu zu bauen). Drei Annahmen stellvertretend und unbestätigt (Text in die Beschreibung, Abruf auf Knopfdruck statt beim Öffnen, Reiter „Schule“); `wartet_auf` gesetzt. Baubar erst nach B-654.
