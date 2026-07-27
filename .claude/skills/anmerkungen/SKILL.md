---
name: anmerkungen
description: >-
  Beim Testen im UI-Widget erfasste Anmerkungen (api/v1/remarks) beantworten und nachbereiten. Nutze dies,
  wenn der Nutzer eine im Widget notierte Frage per Log-Id einlöst — "Beantworte die Frage 123",
  "Anmerkung 45", "beantworte Remark 7", "was ist mit #12" — oder wenn er die gesammelten Anmerkungen
  sichten, einordnen und zu einem Befund verdichten will ("Anmerkungen durchgehen", "offene Anmerkungen
  aufräumen"). Die Anmerkung liefert Text UND den mitgeschnittenen Kontext (Route, Kind, Übung, letzte
  Fehler); die Antwort wird belegt recherchiert und über die API zurückgeschrieben. Dies ist NICHT das
  Erfassen — das passiert im Widget (Alt+A im Vater-Web).
---

# anmerkungen — Test-Beobachtungen beantworten

Beim Testen notiert der Nutzer Fragen und Beobachtungen im Widget und bekommt eine **Log-Id**. Hier wird
sie eingelöst: Anmerkung samt Kontext lesen, im Code nachsehen, **belegt** antworten, Antwort
zurückschreiben, Stand setzen.

Hintergrund und Datenmodell: [docs/anmerkungen-plan.md](../../../docs/anmerkungen-plan.md).

## Die eine Regel, die dieses Werkzeug wertvoll oder wertlos macht

**Jede Antwort wird belegt.** Datei und Zeile, oder ausdrücklich „nicht sicher" / „nicht gefunden".

Eine geratene Antwort ist hier schlimmer als gar keine: Sie landet in der Datenbank, wird über den Export
ins Repo getragen und liest sich Wochen später wie ein geprüfter Befund. „Das gibt es in der API, aber
nicht im UI" klingt nach einer Bauchantwort — belegen lässt es sich nur, indem man **beide** Seiten
ansieht: den Controller *und* die Screens.

Wenn du es nicht belegen kannst, schreibe das hin. Das ist eine brauchbare Antwort; eine erfundene nicht.

## Verbindung zur laufenden Instanz

Anders als `creator`/`supervisor`/`student` läuft dieser Skill **gegen die echte Instanz**, nicht gegen
eine Wegwerf-DB — die Anmerkungen des Nutzers liegen ja in der echten `pugling.db`. Der gemeinsame Helfer
kann das, seine Basis-URL ist überschreibbar:

```bash
export TUTORIAL_API_BASE=http://localhost:5200
source .claude/scripts/tutorial-api.sh
TOK=$(login_father 1 0000)          # das Konto des Nutzers, nicht der Seed-Lehrer
api_get /api/v1/auth/me             # Kontrolle: roles enthält "Supervisor"
```

Läuft dort nichts (`curl` scheitert), **nicht** eine Wegwerf-Instanz hochfahren — die hätte die
Anmerkungen nicht. Stattdessen den Nutzer bitten, das Backend zu starten.

Helfer: `api_get`, `api_post`, `api_patch` (setzen `TOK` voraus).

## Einsatz A — eine Frage beantworten

Auslöser: „Beantworte die Frage 123."

### 1. Lesen

```bash
api_get /api/v1/remarks/123
```

Der Kontext ist der Grund, warum diese Anmerkung mehr wert ist als eine Zeile im Textdokument — lies ihn
vollständig, bevor du suchst:

- `context.route` — **wo** es aufgefallen ist. Der direkte Weg zur Datei: `/vater/profil` →
  `frontend/src/vater/VaterProfil.tsx`.
- `context.appArea` — `vater` oder `sohn`.
- `context.childId` / `exerciseId` / `studyPlanId` / `planPositionId` — konkrete Daten zum Nachsehen.
- `context.recentErrorsJson` — die letzten Fehlschläge (Methode, Pfad, Status, `code`). Bei einem
  Bug-Bericht **zuerst** hier schauen: Ein `403 not_author` oder `409 media_no_alternative` benennt die
  Ursache oft schon.
- `context.contextJson` — Filter/Auswahl, sofern der Screen etwas gemeldet hat.

Bekommst du `404 remark_not_found`, ist die Id falsch **oder** gehört einem anderen Konto — nachfragen,
nicht raten.

### 2. Recherchieren

Nutze die Route als Einstieg, nicht eine Volltextsuche über das Repo. Für Fragen der Art „gibt es X?"
sind **beide** Seiten zu prüfen, sonst ist die Antwort nicht belegbar:

- Backend: gibt es den Endpunkt? (`backend/Pugling.Api/Controllers/…`)
- Frontend: gibt es dafür eine Bedienung? (`frontend/src/lib/api.ts` und der Screen zur Route)

Die Wissenskarte spart Sucherei: [docs/endpunkt-beziehungen.md](../../../docs/endpunkt-beziehungen.md).

### 3. Antworten und zurückschreiben

Zuerst dem Nutzer im Terminal antworten — knapp, mit Beleg. Dann festhalten:

```bash
api_patch /api/v1/remarks/123 '{"answer":"Geht bereits: /vater/profil hat ein E-Mail-Feld (VaterProfil.tsx:87) ueber api.updateFather (api.ts:211) auf PATCH supervisor/fathers/{id}; Backend FathersController.cs:76.","answeredBy":"claude-code","status":"Done"}'
```

> Dieses Beispiel ist echt und stammt aus dem Verifikationslauf — und es zeigt, warum die Belegregel
> existiert: Die naheliegende Bauchantwort auf „ich kann meine E-Mail nirgends ändern" wäre „die API kann
> es, das UI nicht" gewesen. **Sie wäre falsch gewesen.** Das Formular ist da; erst der Blick in beide
> Seiten hat das gezeigt.

**ASCII-only im `-d`-Body** (Windows/Git-Bash-Fallstrick aus dem Helfer): Umlaute werden sonst zu
ungültigem UTF-8. Formuliere die gespeicherte Antwort ohne Umlaute oder schreibe sie über eine Datei.

### 4. Nachfragen, wie es weitergeht

Frage den Nutzer — und setze den Stand danach:

| Antwort des Nutzers | `status` |
|---|---|
| „Damit ist es beantwortet." | `Done` |
| „Zurückstellen, da muss was gebaut werden." | `Planned` |
| „Trifft nicht zu / erledigt sich." | `Rejected` |

**Die Antwort bleibt in beiden Fällen stehen.** Ein zurückgestellter Fall ist damit kein offener Zettel
mehr, sondern ein analysierter Backlog-Eintrag — die Vorarbeit ist getan.

Entsteht aus der Antwort eine **neue Aufgabe**, lege sie als eigene Anmerkung mit Verweis an, statt die
Frage umzudeuten:

```bash
api_post /api/v1/remarks '{"text":"E-Mail-Formular im Vater-Profil ergaenzen","category":"Ui","parentRemarkId":123}'
```

Die Frage geht dann auf `Done` (sie *ist* beantwortet), die neue Aufgabe steht auf `Open`.

## Export nach `docs/anmerkungen/`

Ein Markdown-Schnappschuss der sichtbaren Anmerkungen – versioniert und ohne laufenden Server lesbar:

```bash
# Ohne status-Filter, damit auch `eingeplant` mitkommt – die Rollen-Skills lesen ausdruecklich beides,
# und gerade die eingeplanten tragen schon eine Analyse. `-f`, damit ein 401/403 nicht als
# ProblemDetails-JSON in der Exportdatei landet (api_get schluckt den Fehler).
curl -sf "$TUTORIAL_API_BASE/api/v1/remarks/export" -H "Authorization: Bearer $TOK" \
  -o docs/anmerkungen/aktuell.md
```

Das ist zugleich die **einzige Brücke zu den Test-Skills**: `creator`, `supervisor`, `student` und
`/smoke-test` laufen gegen eine Wegwerf-DB und sehen die echten Anmerkungen nur als Datei im Repo.

Der Endpunkt ist Supervisor-only (Antworten tragen Code-Interna) und liefert älteste zuerst – beim
Nacharbeiten ist die Reihenfolge des Auffallens die hilfreichere.

## Einsatz B — Sammel-Nachbereitung

Auslöser: „Anmerkungen durchgehen", „offene Anmerkungen aufräumen". Ergebnis ist ein **priorisierter
Befund**, den `pm-loop` als Feedback-Quelle liest.

### 1. Holen

```bash
api_get "/api/v1/remarks?status=Open&take=100"
```

Nichts offen? Sagen und aufhören – kein Befund über ein leeres Feld.

### 2. Einordnen

Die Kategorie durfte beim Erfassen leer bleiben (`Unspecified`) – **das ist der Regelfall und Absicht**.
Hier wird sie aus dem Text abgeleitet: `Bug`, `Ui`, `Code`, `Content`, `Idea`, `Question`.

Die Grenze, auf die es ankommt: **`Question` braucht eine Antwort, kein Ticket.** Eine Frage, die du hier
beantworten kannst, behandle nach Einsatz A und lass sie nicht in den Befund wandern – sonst wird eine
zweiminütige Auskunft zu einem Backlog-Eintrag.

### 3. Gegen den aktuellen Code prüfen

**Der Schritt, der am meisten spart.** Anmerkungen sind Wochen alt, der Code nicht. Prüfe jede, bevor sie
in den Befund kommt:

- Inzwischen behoben? → dem Nutzer vorschlagen, sie auf `Done` zu setzen. **Nicht selbst entscheiden.**
- Nicht mehr nachvollziehbar (Route existiert nicht mehr, Übung gelöscht)? → als solches kennzeichnen.
- Doppelt? → zusammenfassen, alle Ids nennen (die Häufung ist selbst ein Signal).

Es gilt dieselbe **Belegpflicht** wie in Einsatz A: „ist behoben" ohne Datei und Zeile ist geraten.

### 4. Befund schreiben

Nach `docs/anmerkungen/befund-JJJJ-MM-TT.md`, gruppiert nach Thema (nicht nach Kategorie – ein Thema
trägt oft Bug *und* Idee). Je Eintrag: die Ids, was beobachtet wurde, was die Prüfung ergab, und ein
Vorschlag zur Dringlichkeit. Halte die Beobachtung des Nutzers und deine Prüfung **getrennt** – sonst
liest sich später deine Vermutung wie seine Beobachtung.

### 5. Status nachziehen — erst nach Rückfrage

Zeige, was du setzen würdest, und frage. **Nie ungefragt einen Stapel Status ändern:** Was auf `Planned`
steht, verschwindet aus der Standardsicht, und ein falsch weggeräumter Befund ist schwerer zu bemerken
als ein offener zu viel.

| Fall | Status |
|---|---|
| Im Befund gelandet, es folgt Arbeit | `Planned` |
| Beim Prüfen als erledigt belegt | `Done` |
| Trifft nicht mehr zu | `Rejected` |
| Unklar, muss offen bleiben | unverändert |

Das ist der Mechanismus, der das System am Leben hält: Was einmal eingeplant ist, liegt beim nächsten Lauf
nicht wieder oben auf.

### 6. Übergeben

Sag dem Nutzer, dass `pm-loop` den Befund als Feedback-Quelle liest – die Entwicklungsentscheidung fällt
dort, nicht hier.

## Grenzen

- **Das Widget ist der Eingang, dieser Skill nicht.** Lege keine Anmerkungen „für den Nutzer" an; die
  einzige Ausnahme ist die Folge-Aufgabe aus Schritt 4.
- **Kein Chat.** Rückfragen des Nutzers beantwortest du hier im Gespräch, nicht als weitere Anmerkung.
- **Fixen ist eine eigene Entscheidung.** Eine beantwortete Frage ist keine Beauftragung: Wenn aus der
  Antwort Arbeit folgt, frage, ob du sie jetzt machen sollst.
