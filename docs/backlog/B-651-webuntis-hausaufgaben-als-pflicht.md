---
tags: [typ/story, status/ausformuliert, bereich/backend, bereich/frontend, bereich/agent, bereich/auth, rolle/supervisor, rolle/creator]
aliases: [WebUntis-Connector, WebUntis-Hausaufgaben, Schul-Hausaufgaben importieren]
status: ausformuliert
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Chat mit dem Vater, 2026-11-18 (Sichtung der WebUntis-Seite student-homework im Elternkonto)"
grund: ""
ersetzt_durch: []
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

Der Vater hat die Empfehlungen am 2026-11-18 im Chat übernommen („Ja, ausformulieren“). Beim Grillen werden sie
als Entscheidungen mit Kosten festgehalten.

1. **Welcher Zugang wird beim Kind hinterlegt?** Empfehlung: ein Zugang je Kind, entweder der Elternzugang oder
   das Schülerkonto des Kindes, plus die WebUntis-Schüler-ID für die Zuordnung über `elementIds`. Grund: Der
   Elternzugang deckt Theodor nicht ab.
2. **Wer darf ihn setzen und sehen?** Empfehlung: nur der Supervisor. Das Geheimnis kann nur gesetzt werden und
   wird nie herausgegeben. Die API meldet nur „eingerichtet“, den letzten Abruf und dessen Ergebnis. Der Student
   sieht nichts.
3. **Wie wird gespeichert?** Empfehlung: ASP.NET Core Data Protection in einer eigenen Tabelle 1:1 zum Kind;
   neue Migration an der Kette. Der Schlüsselbund liegt dauerhaft neben `pugling.db` im Heimnetz. Geht er
   verloren, wird der Zugang neu eingegeben; die Lerndaten bleiben unberührt.
4. **Wann wird abgerufen?** Empfehlung: zuerst nur auf Anfrage, wenn der Vater die Liste öffnet. Kein
   Hintergrunddienst, keine Kopie der Hausaufgaben in der DB. Ein täglicher Abruf mit Zwischenspeicher kommt
   erst, wenn daraus Pflichtziele mit Malus werden (eigene Story).
5. **Wo liegt der Lese-Endpunkt?** Empfehlung: in der Supervisor-Ebene
   (`supervisor/children/{childId}/school-homework`), nicht beim Creator. Der Katalog des Creators ist bewusst
   nicht an ein Kind gebunden, die Hausaufgaben sind es. Der Vater ist beides, und der KI-Creator nutzt mit
   `--child` ohnehin Daten des Kindes.
6. **Wie meldet sich der Connector an?** Empfehlung: zuerst Benutzername und Passwort per JSON-RPC. Der
   QR-Code-Schlüssel kommt nur dazu, falls ein SSO den Login sperrt. Erste Bauhandlung ist ein Probe-Login, der
   das klärt.
7. **Welcher Umfang zuerst?** Empfehlung: nur Hausaufgaben. Prüfungen → `Klassenarbeit` und Hausaufgaben als
   Pflicht mit Malus werden je eine Folge-Story.
8. **Wie werden die Fächer zugeordnet?** WebUntis liefert das Fach als Text („Französisch“), Pugling hat
   `Subject`. Empfehlung: in Stufe 1 nur anzeigen, nicht zuordnen. Ein Abgleich nach Namen kommt erst, wenn
   Übungen automatisch in ein Fach sollen.
9. **Was tut der KI-Creator?** Empfehlung: ein Befehl, der mit `--child` die offenen Hausaufgaben lädt und je
   gewählter Aufgabe eine Übung über die bestehende `create`-Pipeline erzeugt. Der Text der Aufgabe geht als
   Stoffnotiz ins Briefing. Über die Pipeline entscheidet weiter C#.

## Akzeptanzkriterien

Entwurf, wird beim Grillen final.

1. Ein Supervisor kann für ein betreutes Kind einen WebUntis-Zugang setzen, ändern und löschen. Für ein fremdes
   Kind gibt es `404`/`403` wie bei den übrigen Kind-Unterressourcen. Ein Student erreicht den Endpunkt nicht.
2. Keine Antwort der API enthält das Geheimnis, auch kein GET des Zugangs. Prüfbar über einen Test, der jede
   Antwort nach dem gesetzten Wert durchsucht.
3. In der DB liegt das Geheimnis nicht im Klartext. Ein Test liest die Zeile roh und findet den Wert nicht.
4. `GET …/school-homework?from=&to=` liefert die Aufgaben des Kindes mit Fach, Lehrkraft, Aufgabedatum,
   Fälligkeit, Text, erledigt und verpasst. Aufgaben anderer `elementIds` werden herausgefiltert. Getestet gegen
   einen Fake-Connector mit aufgezeichneter, namenfreier Antwort.
5. Schlägt der Login fehl oder ist WebUntis nicht erreichbar, kommt ein `ProblemDetails` mit eigenem `code`
   (z. B. `school_login_failed`, `school_unreachable`), kein `500`.
6. Im Vater-Web kann der Zugang je Kind eingerichtet und die Hausaufgabenliste angesehen werden. Aus einer
   Aufgabe führt ein Weg zur Übungserstellung, deren Felder mit Fach und Aufgabentext vorbefüllt sind.
7. Der KI-Creator erzeugt mit `--child` aus einer gewählten Hausaufgabe eine Übung im Katalog.
8. Ein echter Abruf gegen die Schule mit Friedrichs Zugang zeigt im Heimnetz dieselben Aufgaben wie die Seite
   `student-homework`.

## Verlauf

- **2026-11-18** — angelegt (Quelle: Sichtung der WebUntis-Hausaufgabenseite im Elternkonto, Chat mit dem Vater).
- **2026-11-18** — ausformuliert: Ziel ist jetzt der Connector für die Übungserstellung, nicht der Malus.
  Ist-Stand am Code (keine ausgehenden Aufrufe, kein `DataProtection`, die PIN nur gehasht), WebUntis-Felder aus
  der Sichtung, neun offene Punkte mit Empfehlung, vom Vater im Chat übernommen.
