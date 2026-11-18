---
tags: [typ/story, status/idee, bereich/lehrplan, bereich/gamification, rolle/supervisor, rolle/student]
aliases: [WebUntis-Hausaufgaben, Schul-Hausaufgaben importieren]
status: idee
prio: P2
art: Wunsch
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: "Chat mit dem Vater, 2026-11-18 (Sichtung der WebUntis-Seite student-homework im Elternkonto)"
unverifiziert: true
grund: ""
ersetzt_durch: []
---

# B-651 · Schul-Hausaufgaben aus WebUntis abrufen und als Pflicht mit Münz-Malus abbilden

Die Söhne bekommen ihre Hausaufgaben über WebUntis. Die Seite `student-homework` sortiert sie in „bald fällig",
„noch nicht abgeschlossen" und „verpasst". Pugling weiß davon nichts, obwohl genau das sein Kern ist:
Pflichtziele, deren Verpassen Münzen kostet. Pugling soll die Hausaufgaben einmal täglich abrufen, dem Kind
zuordnen und dem Vater zeigen. Verpasste Aufgaben werden zu einem Pflichtverstoß. Prüfungstermine, die im
Aufgabentext stehen, könnten den Lernplan anstoßen (Beispiel: „petit contrôle mardi le 24 novembre").

Am 2026-11-18 im angemeldeten Elternkonto gesichtet; nicht gegen eine offizielle Doku geprüft. Die
Schnittstelle ist **inoffiziell** und kann sich ändern.

- **Hausaufgaben**: `GET /WebUntis/api/homeworks/lessons?startDate=YYYYMMDD&endDate=YYYYMMDD`, nur das
  Sitzungs-Cookie. Die Antwort hat vier Listen: `homeworks` (`id`, `lessonId`, `date`, `dueDate`, `text`,
  `remark`, `completed`, `attachments`), `lessons` (`id`, `subject`, `lessonType`), `teachers` (`id`, `name`)
  und `records` (`homeworkId`, `teacherId`, `elementIds`). Die Schüler-ID steht in `elementIds`.
- **„Verpasst"** ist kein eigenes Feld. Es ergibt sich aus `dueDate` in der Vergangenheit und `completed: false`.
  Im November standen alle 14 Aufgaben auf `false`. Offen ist, ob jemand `completed` überhaupt pflegt.
- **Konto**: `/api/rest/view/v1/app/data` braucht ein Bearer-Token von `/api/token/new`. Es liefert die Rolle
  `LEGAL_GUARDIAN` und die verknüpften Schüler. Verknüpft ist **nur Friedrich**; Theodor fehlt (andere Schule
  oder anderes Elternkonto?).
- **Freigeschaltet** sind außerdem Prüfungen, Stundenplan, Abwesenheiten, Mitteilungen, Befreiungen und der
  Schuljahreskalender. Noten gibt es nicht.
- **Login**: Benutzername/Passwort per JSON-RPC oder der QR-Code-Schlüssel aus „Freigaben". Ob die Schule ein
  SSO vorschaltet, ist nicht geprüft. Zugangsdaten gehören in User-Secrets bzw. die Heimnetz-Konfiguration,
  nie ins Repo.

## Verlauf

- **2026-11-18** — angelegt (Quelle: Sichtung der WebUntis-Hausaufgabenseite im Elternkonto, Chat mit dem Vater).
