---
tags: [bereich/api, doku/tutorial, werkzeug/rest-client]
---

# REST-Client-Tutorials (`.http`)

Ausführbare End-to-End-Flows der Pugling-API, einer je Rolle. Sie sind die **klickbare,
verifizierbare** Ergänzung zu den erzählenden Markdown-Tutorials
([tutorial-creator](../tutorial-creator.md) · [supervisor](../tutorial-supervisor.md) ·
[student](../tutorial-student.md)) und laufen mit der VS-Code-Extension
[REST Client](https://marketplace.visualstudio.com/items?itemName=humao.rest-client) (`humao.rest-client`).

| Datei | Rolle | Login | Was der Flow zeigt |
|---|---|---|---|
| [Creator.http](Creator.http) | Creator | Vater `1` / PIN `0000` | Fach → Verlag/Reihe → Unit → Vokabelübung → Items → Preview → Tags → Rechte → Profil |
| [Supervisor.http](Supervisor.http) | Supervisor | Vater `1` / PIN `0000` | Kind → Plan → Position → Lernziel → Shop → Missionen → Kontrolle |
| [Student.http](Student.http) | Student | Sohn `1` / PIN `1111` | Tagesmission → Üben (Leitner) → Test → Report → Shop-Kauf |

> Ein **Vater** hält die Creator- **und** die Supervisor-Rolle; deshalb loggen sich beide Admin-Flows
> als Vater `1` ein. Ein reiner **Lehrer** (Vater `2` / PIN `9999`) kann alle Katalog-Schritte in
> `Creator.http` ebenso, nur den child-skopierten Tag-Schritt nicht (kein eigenes Kind).

## Voraussetzungen

- Backend läuft: `cd backend/Pugling.Api && dotnet run` → `http://localhost:5200` (Swagger `/swagger`).
- Seed-Daten sind beim ersten Start vorhanden (Vater `1`/`0000`, Lehrer `2`/`9999`, Sohn `1`/`1111`).

## Nutzung

1. Extension **REST Client** installieren.
2. Unten rechts in der Statusleiste die **Umgebung** wählen (`local` = Dev-Instanz `:5200`,
   `smoke` = Wegwerf-Instanz `:5280`). Die Umgebungen stehen in
   [`.vscode/settings.json`](../../.vscode/settings.json) unter `rest-client.environmentVariables`
   (Default `baseUrl` liegt in `$shared`, funktioniert also auch ohne Auswahl).
3. Datei öffnen und die Requests **von oben nach unten** senden (über jedem Request steht
   `Send Request`). Die Reihenfolge ist wichtig: jeder Request zieht Token/IDs aus der Antwort des
   vorherigen.

## Wie die Flows sich selbst prüfen

Die Dateien sind **self-verifying** über die Verkettung – kein separates Assertion-Framework nötig:

- **Token-Kette:** `@authToken = {{login.response.body.token}}` – schlägt der Login fehl, tragen
  alle Folge-Requests kein gültiges Bearer-Token und antworten `401`.
- **Readback nach dem Schreiben:** Nach `POST` einer Ressource liest ein `GET` sie wieder
  (z. B. `createSubject` → `GET .../subjects/{{subjectId}}`, `createExercise` → `.../items`).
  Materialisierte eine POST keine gültige `id`, läuft der Readback in `404` – der Fehler wird sofort
  sichtbar.
- **Array-Zugriff:** IDs aus Listen werden per JSONPath-Index gezogen
  (`{{children.response.body.$[0].id}}`, `{{shop.response.body.$.available[0].id}}`).

## Reihenfolge über Rollen hinweg

`Student.http` setzt einen **aktiven Plan mit mindestens einer Position** voraus. Entweder vorher
`Supervisor.http` laufen lassen (erzeugt Plan + Position) oder den Seed-Plan verwenden; die Plan-ID
setzt du oben in `Student.http` als `@planId` (die `positionId` wird aus der Overview-Antwort
abgeleitet). Der Shop-Kauf am Ende von `Student.http` kann `409` liefern, wenn das erste Angebot
zu teuer ist (`affordable:false`) – dann `@listingId` auf eine bezahlbare Angebots-ID setzen.

## Automatisierte API-Verifikation

Die `.http`-Dateien sind für die **interaktive** Nutzung im Editor gedacht. Automatisierte
grün/rot-Prüfung der API läuft über die Integrationstests (`dotnet test` in `backend/Pugling.Api.Tests`)
und `/smoke-test` – nicht über die `.http`-Dateien.
