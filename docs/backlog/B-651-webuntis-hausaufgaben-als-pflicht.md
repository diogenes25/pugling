---
tags: [typ/story, status/verworfen, bereich/backend, bereich/frontend, bereich/agent, bereich/auth, rolle/supervisor, rolle/creator]
aliases: [WebUntis-Connector, WebUntis-Hausaufgaben, Schul-Hausaufgaben importieren]
status: verworfen
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Chat mit dem Vater, 2026-11-18 (Sichtung der WebUntis-Seite student-homework im Elternkonto)"
grund: "geteilt"
ersetzt_durch: [B-654, B-655, B-656]
---

# B-651 · WebUntis-Connector: Schul-Hausaufgaben je Kind abrufen, damit der Vater daraus Übungen baut

Die Söhne bekommen ihre Hausaufgaben über WebUntis. Die Seite `student-homework` sortiert sie in „bald fällig",
„noch nicht abgeschlossen" und „verpasst". Pugling weiß davon nichts. Der Vater will den WebUntis-Zugang **am
Kind** hinterlegen. Als Creator und Supervisor nutzt er die abgerufenen Aufgaben dann, um Übungen zu erstellen,
von Hand oder über den KI-Creator. Hausaufgaben als Pflichtziel mit Münz-Malus waren der Ausgangsgedanke. Sie
sind jetzt ein späterer Schritt, ebenso die Prüfungstermine.

## User Story

Als **Supervisor** möchte ich je Kind einen WebUntis-Zugang hinterlegen und dessen aktuelle Schul-Hausaufgaben
in Pugling sehen. Damit kann ich als **Creator** (selbst oder über den KI-Creator) passende Übungen erstellen,
ohne den Aufgabentext aus WebUntis abzuschreiben.

## Ist-Stand am Code

**In Pugling (Stand `main` 2adb3744):**

- **Kein Hausaufgaben-Konzept.** „homework" kommt im Code nur als Beispiel für ein Zeitfenster vor
  (`Pugling.Api/Models/PlanPositionEntities.cs:103`, `Services/Shared/ScoringService.cs:43`). Es gibt weder eine
  Entität noch einen Endpunkt.
- **Keine ausgehenden Aufrufe.** Die API ruft keinen fremden Dienst auf: kein `HttpClient`, kein `AddHttpClient`
  in `Pugling.Api` (gesucht am 2026-11-18). Die Dienste werden in `Pugling.Api/Program.cs:156-188` registriert.
  Ein Connector wäre die **erste externe Abhängigkeit zur Laufzeit**.
- **Keine Verschlüsselung für Geheimnisse.** `DataProtection` wird nirgends verwendet. Das einzige Geheimnis im
  Modell ist die PIN. Sie wird nur **gehasht** (`Auth/AccountService.cs:74` `SetPin`), nie entschlüsselt und nie
  zurückgegeben; die DTOs nehmen sie nur an (`Contracts/Auth/AuthDtos.cs:44`,
  `Contracts/Supervisor/ChildDtos.cs:21`). Ein WebUntis-Passwort muss dagegen wieder **lesbar** sein, sonst kann
  sich der Server nicht anmelden. Ein Hash geht dafür nicht.
- **Das Kind** (`Models/AdminEntities.cs:61`) hat Name, Jahrgang, Klassenstufe, Schulart, Interessen und
  `ProfileNotes`, aber keine Schule und keinen Bezug zu einem fremden System.
- **Muster für Kind-Unterressourcen** beim Supervisor: `Controllers/Supervisor/TimetableController.cs:14`
  (`supervisor/children/{childId}/timetable`). Der Zugriff wird über `AuthAccess.SupervisorOwnsChildAsync`
  (`Auth/AuthAccess.cs:112`) geprüft, also über `SupervisorLink`.
- **Ziel für Prüfungstermine:** `Klassenarbeit` (`Models/KlassenarbeitEntities.cs:67`) hat `ChildId`,
  `SubjectId?`, `Title`, `Topic?` und `ScheduledDate`. Der Endpunkt ist
  `Controllers/Supervisor/KlassenarbeitenController.cs:18` (`supervisor/class-tests`). Für einen Import später
  passt das direkt.
- **KI-Creator:** Kennt `--child` bereits, für Bilder und Profile (`Pugling.Agent.Creator/AgentCommands.cs:112`,
  `:246`). Das Briefing baut er über `ShowBriefingAsync` (`AgentCommands.cs:300`). Hausaufgaben kennt er nicht.

**In WebUntis (am 2026-11-18 im angemeldeten Elternkonto gesichtet, inoffizielle Schnittstelle):**

- **Hausaufgaben:** `GET /WebUntis/api/homeworks/lessons?startDate=YYYYMMDD&endDate=YYYYMMDD` braucht nur das
  Sitzungs-Cookie. Die Antwort hat vier Listen:
  - `homeworks`: `id`, `lessonId`, `date`, `dueDate`, `text`, `remark`, `completed`, `attachments`
  - `lessons`: `id`, `subject`, `lessonType`
  - `teachers`: `id`, `name`
  - `records`: `homeworkId`, `teacherId`, `elementIds`, wobei `elementIds` die WebUntis-Schüler-ID enthält
- **„Verpasst"** ist kein eigenes Feld. Es ergibt sich aus `dueDate` und `completed`. Im November standen alle 14
  Aufgaben auf `completed: false`.
- **Konto:** `/api/rest/view/v1/app/data` braucht ein Bearer-Token von `/api/token/new`. Es liefert die Rolle
  `LEGAL_GUARDIAN` und die verknüpften Schüler. Verknüpft ist **nur Friedrich**, Theodor fehlt.
- **Freigeschaltet** sind außerdem Prüfungen, Stundenplan, Abwesenheiten, Mitteilungen und der
  Schuljahreskalender. Noten gibt es nicht.
- **Anmeldung:** Wie sich ein Server ohne Browser anmeldet, ist **nicht geprüft**. Möglich sind JSON-RPC
  `authenticate` mit Benutzername und Passwort oder der QR-Code-Schlüssel. Ob ein SSO vorgeschaltet ist, ist
  ebenfalls ungeprüft.

## Die echte Lücke

1. Ein **Ablageort** für den WebUntis-Zugang eines Kindes (Server-URL, Schule, Benutzer, Geheimnis,
   WebUntis-Schüler-ID). Das Geheimnis muss **umkehrbar verschlüsselt** sein, das gibt es im Projekt noch nicht.
2. Eine **Connector-Klasse**, die sich anmeldet und die Hausaufgaben eines Zeitraums holt. Sie ist austauschbar
   und gegen eine aufgezeichnete Beispielantwort ohne echte Namen testbar.
3. Ein **Supervisor-Endpunkt**, der die Hausaufgaben eines Kindes als Pugling-DTO liefert: Fach, Lehrkraft,
   Aufgabedatum, Fälligkeit, Text, Bemerkung, erledigt, verpasst.
4. **Oberfläche:** im Vater-Web den Zugang einrichten bzw. prüfen und die Hausaufgabenliste sehen, mit einem
   Einstieg „Übung daraus erstellen".
5. Einen **KI-Creator-Weg**, der die Hausaufgaben eines Kindes als Quelle für die Übungserstellung nimmt.

## Offene Punkte

Alle neun am 2026-11-18 im Dialog mit dem Vater entschieden; siehe Entscheidungen.

1. ~~Welcher Zugang wird beim Kind hinterlegt?~~ → Entscheidung 1
2. ~~Wer darf ihn setzen und sehen?~~ → Entscheidung 2
3. ~~Wie wird gespeichert?~~ → Entscheidung 3
4. ~~Wann wird abgerufen?~~ → Entscheidung 4
5. ~~Wo liegt der Lese-Endpunkt?~~ → Entscheidung 5
6. ~~Wie meldet sich der Connector an?~~ → Entscheidung 6
7. ~~Welcher Umfang zuerst?~~ → Entscheidung 7
8. ~~Wie werden die Fächer zugeordnet?~~ → Entscheidung 8
9. ~~Was tut der KI-Creator?~~ → Entscheidung 9

## Entscheidungen

1. **Ein WebUntis-Zugang je Kind, am Kinderprofil (`Child`), nicht am Login-Konto (`Account`).** Er kann der
   Elternzugang oder das Schülerkonto des Kindes sein. Dazu kommt die WebUntis-Schüler-ID. Begründung: Der
   Elternzugang deckt nur Friedrich ab, Theodor nicht. Die Schüler-ID trennt die Aufgaben über `elementIds`,
   wenn ein Elternzugang mehrere Kinder sieht. Kosten: Derselbe Elternzugang steht bei zwei Kindern doppelt und
   wird doppelt gepflegt.
2. **Nur der Supervisor setzt, ändert und löscht den Zugang. Das Geheimnis kann nur geschrieben werden.** Die
   API meldet „eingerichtet“, den Zeitpunkt des letzten Abrufs und dessen Ergebnis; der Student sieht nichts.
   Begründung: Die Schulzugangsdaten des Kindes sind das empfindlichste Datum im System, und niemand muss sie
   zurücklesen. Kosten: Wer das Passwort vergessen hat, liest es nicht in Pugling nach, sondern setzt es neu.
3. **Verschlüsselt mit ASP.NET Core Data Protection, eigene Tabelle 1:1 zum Kind, neue Migration an der
   Kette.** Der Schlüsselbund liegt dauerhaft neben `pugling.db`. Begründung: Der Server muss das Passwort
   wieder lesen, ein Hash wie bei der PIN geht nicht. Data Protection gehört zum Framework und braucht keine
   eigene Kryptografie. Kosten: Ein neues Betriebsartefakt (Schlüsselordner), das `scripts/heimnetz.ps1`
   mitsichern muss. Geht es verloren, wird der Zugang neu eingegeben; die Lerndaten sind nicht betroffen.
4. **Abruf nur auf Anfrage, keine Kopie der Hausaufgaben in der DB.** Begründung: Für die Übungserstellung
   genügt der Abruf beim Öffnen. Ohne Hintergrunddienst entstehen weder Zwischenspeicher noch Schemadaten, die
   veralten. Kosten: Jedes Öffnen fragt WebUntis live ab und ist langsamer. Ist die Schule nicht erreichbar,
   sieht der Vater nichts. Der spätere Malus braucht trotzdem einen Speicher (Folge-Story).
5. **Lese-Endpunkt in der Supervisor-Ebene: `supervisor/children/{childId}/school-homework`,** nach dem Muster
   von `TimetableController`, geprüft mit `SupervisorOwnsChildAsync`. Begründung: Der Creator-Katalog ist bewusst
   an kein Kind gebunden, Hausaufgaben sind es. Der Vater ist beides. Kosten: Ein Lehrer-Konto, das nur Creator
   ist, kommt nicht an Hausaufgaben. Das ist gewollt.
6. **Anmeldung zuerst mit Benutzername und Passwort per JSON-RPC. Der QR-Code-Schlüssel kommt nur, wenn ein SSO
   das sperrt.** Erste Bauhandlung ist ein Probe-Login gegen die Schule. Begründung: Das ist der einfachste
   Weg, und die Bibliotheken der Community nutzen ihn. Kosten: Sperrt ein SSO, wächst die Story um den
   Einmalcode-Weg (TOTP) und muss neu geschätzt werden.
7. **Umfang: nur Hausaufgaben.** Prüfungstermine → `Klassenarbeit` und Hausaufgaben als Pflicht mit Malus werden
   je eine eigene Story. Begründung: So beweist der Connector zuerst, dass er im Alltag stabil läuft, bevor
   Münzen an ihm hängen. Kosten: Der ursprüngliche Hebel (verpasst kostet Münzen) kommt erst später.
8. **Fach nach Namen beim Abruf zuordnen.** Der Abgleich ignoriert Groß- und Kleinschreibung. Das DTO trägt
   `subjectId?` neben dem WebUntis-Text; ohne Treffer wird die Aufgabe nur angezeigt. Begründung: Der KI-Creator
   braucht ein Fach, um über das Buch des Kindes die Reihe zu finden. Die Fächer mit Büchern heißen in WebUntis
   gleich. Kosten: Ein abweichend geschriebenes Fach („Franz.“) findet still keinen Treffer. Eine
   Zuordnungstabelle je Kind wäre dann eine eigene Story.
9. **KI-Creator: Die Hausaufgabe wird Eingabe der bestehenden `create`-Pipeline.** `homework --child <id>`
   listet die offenen Aufgaben mit WebUntis-Id. `create --type <Typ> --child <id> --homework <id>` übernimmt den
   Aufgabentext als Thema ins Briefing und die Reihe über das zugeordnete Fach. Der Vater wählt den Typ; die
   Übung landet nur im Katalog, in den Plan kommt sie mit `assign`. Begründung: Das ist die kleinste
   Erweiterung, und Typ und Pflicht bleiben in der Hand des Vaters (Linie aus B-460: C# entscheidet). Kosten:
   Eine Aufgabe, die nur auf eine Seite verweist („S. 80 Nr. 1+2“), gibt dem Modell keinen Stoff. Seitenverweise
   über das Buch-Markdown aufzulösen, wäre eine Folge-Story.

## Akzeptanzkriterien

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
6. Im Vater-Web kann der Zugang je Kind eingerichtet und die Hausaufgabenliste angesehen werden. Aus einer
   Aufgabe führt ein Weg zur Übungserstellung, deren Felder mit Fach und Aufgabentext vorbefüllt sind.
7. Der KI-Creator listet mit `homework --child <id>` die offenen Aufgaben. `create --type <Typ> --child <id>
   --homework <id>` erzeugt die Übung in der Reihe, die zum zugeordneten Fach gehört. Eine Aufgabe ohne
   zugeordnetes Fach bricht mit einer verständlichen Meldung ab und erzeugt nichts.
8. Ein echter Abruf gegen die Schule mit Friedrichs Zugang zeigt im Heimnetz dieselben Aufgaben wie die Seite
   `student-homework`.

## Verlauf

- **2026-11-18** — angelegt (Quelle: Sichtung der WebUntis-Hausaufgabenseite im Elternkonto, Chat mit dem Vater).
- **2026-11-18** — ausformuliert: Ziel ist jetzt der Connector für die Übungserstellung, nicht der Malus.
  Ist-Stand am Code (keine ausgehenden Aufrufe, kein `DataProtection`, die PIN nur gehasht), WebUntis-Felder aus
  der Sichtung, neun offene Punkte mit Empfehlung, vom Vater im Chat übernommen.
- **2026-11-18** — gegrillt: neun Entscheidungen mit Begründung und Kosten, vom Vater bestätigt (1–7 als übernommene Empfehlungen, 8 und 9 einzeln: je Variante A). Akzeptanzkriterien final.
- **2026-11-18** — beim Schätzen geteilt (über L: Backend L, Vater-Web M, KI-Creator M; Vater: Variante A nach Schicht) in B-654 (Backend, AC 1–5 und 8), B-655 (Vater-Web, AC 6), B-656 (KI-Creator, AC 7). Ist-Stand und Entscheidungen bleiben hier und werden von dort verlinkt.
