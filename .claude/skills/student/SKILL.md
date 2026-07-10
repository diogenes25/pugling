---
name: student
description: >-
  Drive the Pugling REST API from the STUDENT seat (api/v1/student/*) to learn and redeem — read the
  daily mission, practice a position (Leitner review), sit the position exam, read own progress, and
  spend coins in the family shop; AND, in the same pass, smoke-test that the Student surface works and
  (re)write the verified Student tutorial. Use this whenever the user wants to exercise/validate the
  Student API, play through a plan against the running app, or refresh docs/tutorial-student.md, e.g.
  "run the student skill", "test the student API", "student", "Sohn-API prüfen", "Student-Tutorial
  aktualisieren". This is NOT the file-based Lehrplan format (that is `lehrplan-autor`/`lehrplan-lerner`)
  — it drives the real product API.
---

# student — der Lernende (technische Rolle „Student")

Du bist der **Student**: du siehst nur die **eigenen** Pläne, erledigst die Tagesmission, übst
(server-bewertet, Anti-Schummel), schreibst den Abschlusstest, siehst deinen Fortschritt und **löst**
Münzen im Familien-Shop **ein**. Im Produkt ist das der **Sohn** (Seed: *Sohn*, `childId=1`, PIN `1111`).

Zwei Ziele in einem Lauf: **(1)** die Student-Endpunkte end-to-end **verifizieren** und **(2)** daraus
**`docs/tutorial-student.md`** schreiben/aktualisieren. Mängel werden **berichtet**, nicht heimlich gefixt.

## Voraussetzung

Der Student braucht einen **spielbaren Plan** (aktiv + heute in Laufzeit) mit mindestens einer Position.
Der Seed liefert Katalog, aber **keinen** fertigen Plan → lege vor dem Student-Flow einen an
(Supervisor-Login `login_father 1 0000`): `POST /api/v1/supervisor/study-plans` +
`POST …/study-plans/{planId}/positions {"exerciseId":1}`. (Der `supervisor`-Skill macht genau das.)

## Ablauf

Gegen die **Wegwerf-Instanz** (Port 5280, Temp-DB) über `.claude/scripts/tutorial-api.sh`.

1. **Hochfahren**: `build` → `stop` → `serve` (Hintergrund, `run_in_background: true`) → `wait`.
   Plan+Position als Supervisor anlegen (siehe Voraussetzung), Plan-/Positions-`id` merken.
2. **Einloggen** als Student: `source .claude/scripts/tutorial-api.sh; TOK=$(login_child 1 1111)`.
   Kontrolle: `api_get /api/v1/auth/me` → `role:"Student"`, `childId:1`, `roles:["Student"]`.
3. **Lern-Flow durchspielen** (Status + Schlüsselfelder prüfen):
   - `GET /api/v1/student/study-plans/{planId}/overview` → Tagesmission, `today.outstanding`, Positionen
     (mit `renderer`, `checkMode`).
   - **Üben** (Lern-Modus, server-bewertet):
     - `POST …/positions/{positionId}/practice-sessions {}` → Session (`mode:"Lern"`, `cursor`, `total`).
     - `GET …/practice-sessions/{sessionId}/cards` → Karten; im Lern-Modus mit `reveal` (Lösung sichtbar).
     - `POST …/practice-sessions/{sessionId}/review {"itemIndex":0,"givenAnswer":"hallo"}` → serverseitige
       Prüfung: `wasCorrect`, `expected`, `awarded`, `box`, `dueOn`, `combo`, `comboBonus`, `speedBonus`,
       `next`, `done`. (Antwort = die **Ziel**-Seite, hier „hallo" zu Prompt „hello".)
     - `POST …/heartbeat {"seconds":45,"active":true}` (Aktivzeit, geclampt) und `POST …/end {}`.
   - **Abschlusstest** (Klausur, strikt one-at-a-time):
     - `POST …/positions/{positionId}/tests {}` → `attemptId`, `stage`, `totalItems`. **Wichtig:** Der Student
       kann die Stufe **nicht** frei wählen (nur der Vater darf `stage` setzen); ohne `stageSchedule` fällt sie
       auf **Stufe 2 = Selbsteinschätzung**. Und `totalItems` umfasst nur die **eingeführten** Items (was der
       Student im Üben schon gesehen hat), nicht zwingend alle.
     - Bei **Selbsteinschätzung** (Stufe 2) wird **nicht getippt**, sondern selbst berichtet: Submit mit
       `wasKnown` je Item: `POST …/tests/{attemptId}/submit {"answers":[{"itemIndex":0,"wasKnown":true}]}`
       → `scorePercent`, `passed`, `passPercent`. (Bei getippten Stufen stattdessen `/next` → `/answer`
       `{"itemIndex","givenAnswer"}` → `/submit`.) **Nicht** `givenAnswer` bei Selbsteinschätzung schicken —
       das wertet als 0.
   - `GET …/positions/{positionId}/report` → Box/Beherrschung je Item (`introduced`, `box`, `testsCorrect`).
   - `GET /api/v1/student/me/points | missions | achievements | skins` → eigener Kontostand, Ziele, Badges,
     Skin-Zustand (`selected`, `owned`).
   - **Shop/Einlösen** (der **einzige** Münz-Ausgabeweg): `GET /api/v1/student/me/shop` (Wallet + kaufbare
     Angebote, `affordable`) → `POST …/me/shop/listings/{listingId}/purchase {}` (bucht Coins ab, erhöht
     Inventar) → `POST …/me/shop/inventory/{articleId}/activate {"quantity":10}` (Aktivierungsanfrage; der
     Vater genehmigt sie über den `supervisor`-Skill).
4. **Tutorial** `docs/tutorial-student.md` schreiben — echte Requests + Schlüssel-Response-Felder, volle
   Bodies per Link auf `docs/api-examples/me.md`/`vocabulary.md`. Frontmatter
   `[typ/tutorial, bereich/training, rolle/student, lerntechnik/vokabeln]`, relative Links, „Verwandt"-Footer,
   Brücken-Satz *Sohn = Student*. Für Tiefe auf `wiki/06-sohn-app.md`-Themen aufsetzen (das wird zum Stub →
   dieses Tutorial ist die neue kanonische Fassung).
5. **Herunterfahren & Bericht**: `stop`; kurz grün/rot + Mängel (z. B. Selbsteinschätzung vs. getippt,
   `totalItems`=eingeführte), `pugling.db` unberührt.

## Regeln

- **Rolle sauber halten**: nur `api/v1/student/*` (+ `auth`). Der Server erzwingt Selbstschutz (Stufe aus dem
  Fahrplan, Heartbeat geclampt, fremde Tage nur der Vater) — nicht dagegen anarbeiten, sondern dokumentieren.
- **Verifizieren, nicht behaupten.** ASCII-Bodies, relative Temp-DB, Hintergrund-Server.
- **Mängel melden, nicht heimlich fixen.**
