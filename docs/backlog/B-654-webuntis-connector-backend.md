---
tags: [typ/story, status/geschaetzt, bereich/backend, bereich/auth, rolle/supervisor]
aliases: [WebUntis-Connector Backend, Schulzugang am Kind]
status: geschaetzt
prio: P2
art: Wunsch
groesse: L
wo: backend
migration: ja
vertragsbruch: nein
quelle: B-651
grund: ""
ersetzt_durch: []
---

# B-654 · WebUntis-Connector im Backend: Schulzugang am Kind, Hausaufgaben je Kind abrufen

Dies ist der Backend-Teil aus [B-651](B-651-webuntis-hausaufgaben-als-pflicht.md); geteilt wurde B-651 beim
Schätzen, weil die Story zu groß war. Hier entstehen der verschlüsselte WebUntis-Zugang je Kind, die
Connector-Klasse und die Supervisor-Endpunkte. Die Oberfläche folgt in
[B-655](B-655-webuntis-hausaufgaben-vater-web.md), der KI-Creator-Weg in
[B-656](B-656-ki-creator-uebung-aus-hausaufgabe.md). Ist-Stand am Code, die Lücke und die neun Entscheidungen
mit Begründung und Kosten stehen in B-651 und gelten hier unverändert. Hier betroffen sind vor allem die
Entscheidungen 1–8.

## User Story

Als **Supervisor** möchte ich je Kind einen WebUntis-Zugang hinterlegen und über die API die Hausaufgaben des
Kindes abrufen. So können Oberfläche und KI-Creator darauf aufbauen.

## Ist-Stand am Code

Steht mit Belegen (`Datei:Zeile`) in [B-651 → Ist-Stand am Code](B-651-webuntis-hausaufgaben-als-pflicht.md#ist-stand-am-code)
(Stand `main` 2adb3744, 2026-11-18). Vor dem Bauen gegen den dann aktuellen Code prüfen.

## Entscheidungen

Übernommen aus [B-651 → Entscheidungen](B-651-webuntis-hausaufgaben-als-pflicht.md#entscheidungen), 1–8.
Entscheidung 9 betrifft nur B-656.

## Akzeptanzkriterien

Übernommen aus B-651, Nummern 1–5 und 8.

1. Ein Supervisor kann für ein betreutes Kind einen WebUntis-Zugang setzen, ändern und löschen. Für ein fremdes
   Kind gibt es `404`/`403` wie bei den übrigen Kind-Unterressourcen. Ein Student erreicht die Endpunkte nicht.
2. Keine Antwort der API enthält das Geheimnis, auch kein GET des Zugangs. Prüfbar über einen Test, der jede
   Antwort nach dem gesetzten Wert durchsucht.
3. In der DB liegt das Geheimnis nicht im Klartext. Ein Test liest die Zeile roh und findet den Wert nicht.
4. `GET …/school-homework?from=&to=` liefert die Aufgaben des Kindes mit WebUntis-Id, Fach (Text und
   `subjectId?` nach Entscheidung 8), Lehrkraft, Aufgabedatum, Fälligkeit, Text, erledigt und verpasst.
   Aufgaben anderer `elementIds` werden herausgefiltert. Getestet gegen einen Fake-Connector mit
   aufgezeichneter, namenfreier Antwort.
5. Schlägt der Login fehl oder ist WebUntis nicht erreichbar, kommt ein `ProblemDetails` mit eigenem `code`
   (z. B. `school_login_failed`, `school_unreachable`), kein `500`.
6. Ein echter Abruf gegen die Schule mit Friedrichs Zugang zeigt im Heimnetz dieselben Aufgaben wie die Seite
   `student-homework`.

## Schätzung

**Größe: L.** Das entspricht dem Anker E6. Dazu gehören:

- eine neue Tabelle mit Migration an der Kette,
- das erste Geheimnis, das der Server wieder lesen muss (Data Protection mit Schlüsselbund),
- der erste Aufruf der API an einen fremden Dienst (typisierter `HttpClient`),
- zwei Endpunktgruppen mit Ownership-Prüfung samt Integrationstests.

Jeder Punkt für sich wäre M; zusammen und mit Betriebsfolgen sind sie L.

- **`migration: ja`**: Neue Tabelle 1:1 zum Kind. Bestandszeilen gibt es keine, die Migration ist rein additiv.
- **`vertragsbruch: nein`**: Es kommen nur neue DTOs und Endpunkte dazu. Kein bestehendes Feld ändert sich.

### Risiken

1. ~~**SSO sperrt die Anmeldung** mit Benutzername und Passwort (Entscheidung 6). Dann kommt der Weg mit dem
   QR-Code-Schlüssel dazu (TOTP), und die Story wird neu geschätzt. Deshalb ist Schritt 0 die Probe.~~
   **Ausgeräumt am 2026-11-18** (siehe Verlauf): Die Anmeldeseite bietet SSO **oder** Passwort, und der Login
   per JSON-RPC ohne Browser klappt.
2. **Die Schnittstelle ist inoffiziell** und kann sich ändern. Gegenmaßnahme: Das Parsen steckt hinter
   `IWebUntisConnector`, unbekannte Felder werden ignoriert. Eine Formabweichung meldet `school_unreachable`
   bzw. einen eigenen Code statt `500`.
3. **Schlüsselbund geht verloren.** `scripts/heimnetz.ps1` sichert heute nur `pugling.db` (Zeilen 83–85). Der
   Schlüsselordner muss in dieselbe Sicherung. Ohne ihn ist der Zugang nicht mehr lesbar; die Lerndaten
   bleiben unberührt.
4. **Produktionsstart:** `ProductionWebAppFactory` startet die App in Production. Fehlt der Schlüsselpfad dort,
   darf der Start nicht scheitern. Vorgabe ist ein Ordner neben der DB.
5. **Endpunkt-Abdeckung:** `EndpointCoverageGuard` verlangt, dass jede neue Action erfolgreich aufgerufen wird.
   Steht sie nicht in den Tests, wird das Tor rot.

### Angriffsplan (Backend, Reihenfolge)

0. **Probe-Login.** Ein Wegwerfskript im Scratchpad, nicht im Repo, meldet sich mit Friedrichs Zugang per
   JSON-RPC an und ruft `homeworks/lessons` ab. Den Zugang trägt der Vater selbst als Umgebungsvariable oder
   User-Secret ein. Ergebnis: Ist Entscheidung 6 haltbar? Wenn nicht: Halt und neu schätzen.
1. **Modell und Migration:** Eine Entität für den Zugang mit `ChildId` (PK/FK), Server-URL, Schule, Benutzer,
   geschütztem Geheimnis, WebUntis-Schüler-ID, letztem Abruf und dessen Ergebnis. Danach
   `dotnet dotnet-ef migrations add` an die Kette und `SchemaGuardTests` grün.
2. **Data Protection:** `AddDataProtection().SetApplicationName(…).PersistKeysToFileSystem(…)`. Der Pfad kommt
   aus der Konfiguration, Vorgabe ein Ordner neben der DB. `heimnetz.ps1` kopiert ihn in die Sicherung.
3. **Connector:** `IWebUntisConnector` mit `FetchHomeworkAsync(zugang, von, bis, ct)`. Die Implementierung ist
   ein typisierter `HttpClient` mit Timeout. Er meldet sich an, ruft ab, mappt auf ein internes Modell und meldet
   sich ab. Fehler werden zu Ausnahmen mit Code.
4. **Vertrag:** DTO zum Setzen (mit Geheimnis); eine Antwort ohne Geheimnis („eingerichtet“, Benutzer,
   Schüler-ID, letzter Abruf); eine Hausaufgaben-Antwort mit `subjectId?`.
5. **Controller** unter `supervisor/children/{childId}/…` nach dem Muster von `TimetableController`:
   `school-account` (GET/PUT/DELETE) und `school-homework` (GET `from`/`to`). Geprüft über
   `SupervisorOwnsChildAsync`, Fehler als `ProblemDetails` mit `code`. Die Fach-Zuordnung (Entscheidung 8) und
   „verpasst“ (Fälligkeit vor heute nach `TimeProvider`, nicht erledigt) entstehen im Service.
6. **Heimnetz-Probe:** nach dem Neustart über `heimnetz.ps1` per `curl` gegen `:5200` mit Friedrichs Zugang
   (AC 6).

### Testweg

- **Integrationstests** in einer neuen Klasse `SchoolHomeworkTests` (`Pugling.Api.Tests`). Der Fake-Connector
  wird in der Testfabrik registriert, so wie `PuglingWebAppFactory` schon `TimeProvider` ersetzt.
  - Ownership: fremdes Kind, Student → AC 1.
  - Das Geheimnis in keiner Antwort → AC 2.
  - Rohe DB-Zeile ohne Klartext → AC 3.
  - Filtern nach `elementIds`, `subjectId` und verpasst mit fester Uhr → AC 4.
  - Fake wirft → `school_login_failed` / `school_unreachable` → AC 5.
- **Unit-Test des Parsers** gegen eine aufgezeichnete, von Namen bereinigte WebUntis-Antwort als Fixture. Echte
  Namen von Lehrkräften und Kindern kommen nicht ins Repo.
- **`ProductionStartupTests`** grün mit dem neuen Data-Protection-Setup.
- **`/smoke-test`** für den Durchstich, **Heimnetz-Probe** für AC 6.
- Vor der Abnahme: Agent `pugling-reviewer`.

## Verlauf

- **2026-11-18** — angelegt durch Teilen von B-651 beim Schätzen (Vater: Variante A, drei Stories nach
  Schicht); Entscheidungen 1–8 aus B-651 übernommen, damit auf `gegrillt`.
- **2026-11-18** — geschätzt: L, backend, Migration ja, Vertragsbruch nein; Probe-Login als Schritt 0.
- **2026-11-18** — Schritt 0, Probe-Login: Wegwerfskript im Scratchpad, den Zugang hat der Vater im eigenen
  Terminal eingegeben. JSON-RPC `authenticate` gegen die Schule `altesgymnasium-flensburg` klappt mit dem
  Elternzugang (`personType` 12, Sitzung erhalten). `homeworks/lessons` liefert danach allein mit dem
  Sitzungs-Cookie 15 Aufgaben (±30 Tage), mit den gesichteten Feldern und einer einzigen `elementId`.
  `token/new` gibt ein JWT, `logout` klappt. Entscheidung 6 hält, die Schätzung bleibt L.
