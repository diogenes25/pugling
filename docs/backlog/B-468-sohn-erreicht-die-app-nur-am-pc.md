---
tags: [typ/story, status/ausformuliert, bereich/qualitaet, rolle/student, rolle/supervisor]
aliases: [Handy-Zugang, Hosting, Heimnetz]
status: ausformuliert
prio: P1
art: Wunsch
groesse: ""
wo: "beides"
migration: ""
vertragsbruch: ""
quelle: Rollengang am Access-Lehrwerk 2026-11-10 (Kopie der pugling.db, Sohn im Handy-Format 375×812, Vater-Web)
grund: ""
ersetzt_durch: []
---

# B-468 · Der Sohn erreicht die App nur am Rechner des Vaters – „jederzeit am Handy“ braucht einen Ort für den Server

Die Oberfläche der Sohn-App ist für das Handy gebaut (PWA-Manifest, Hochformat, Ziffernfeld, untere
Navigation; im Prüfgang bei 375×812 ohne seitliches Scrollen bedienbar). Erreichbar ist sie heute nur über
`localhost`: Die API läuft auf `:5200`, das Frontend über Vite auf `:5173`, das Azure-Deployment ist seit
2026-07-30 stillgelegt ([deployment-azure.md](../deployment-azure.md), B-07 und B-33 sind verworfen).

Solange das so ist, kann das Kind nicht im Bus, bei der Oma oder im eigenen Zimmer lernen, und der Stick
(Malus bei verpasster Pflicht) trifft es an Tagen, an denen der Rechner des Vaters aus ist.

Zu entscheiden (Nutzer):

1. **Heimnetz**: Ein-Host-Bau (`frontend/dist` → `wwwroot`) auf einem immer laufenden Gerät im WLAN. Billig,
   aber nur zu Hause und ohne HTTPS keine installierbare PWA.
2. **Hosting**: Azure wieder aufbauen oder ein kleiner VPS mit TLS. Dann zählen die offenen Punkte der
   Anmeldung: Der Login-Name ist eine fortlaufende Nummer („Helden-Nummer“) mit vierstelliger PIN.
3. **Tunnel** (Tailscale o. ä.) vom Handy zum Rechner zu Hause: kein öffentlicher Dienst, aber der Rechner
   muss laufen.

Dazu gehört die Prüfung am echten Gerät ([B-31](B-31-geraete-vorbehalt-klang.md)) und die Frage, ob die
Sitzung des Kindes den Tag übersteht (Token-Laufzeit 12 Stunden).

## User Story

Als Sohn möchte ich die App auf meinem Handy im WLAN zu Hause öffnen und mich anmelden, damit ich nicht am
Rechner meines Vaters lernen muss. Als Vater möchte ich den Server mit einem Befehl auf meinem Rechner starten,
ohne dass er für jeden im Netz Testdaten oder Entwicklerwerkzeuge zeigt.

## Ist-Stand am Code

Nachgesehen am 2026-11-11.

- **Bindung Backend:** Kestrel lauscht nur auf `http://localhost:5200`
  ([launchSettings.json](../../backend/Pugling.Api/Properties/launchSettings.json)). Ein Handy erreicht das nicht.
  Eine HTTPS-Umleitung gibt es nicht (`UseHttpsRedirection` fehlt in `Program.cs`), HTTP im Heimnetz geht also.
- **Bindung Frontend:** Vite lauscht nur auf `localhost:5173` (kein `server.host` in
  [vite.config.ts](../../frontend/vite.config.ts)) und reicht `/api` an `localhost:5200` weiter.
- **API-Adresse im Frontend:** relativ, `const V1 = "/api/v1"` ([api.ts:309](../../frontend/src/lib/api.ts)). Das
  Frontend fragt also immer den Server, von dem es geladen wurde. Eine Adresse muss nirgends eingetragen werden.
- **Ein-Host-Betrieb ist schon gebaut:** Die API liefert `wwwroot` aus und fällt für jede SPA-Route auf
  `index.html` zurück ([Program.cs:587](../../backend/Pugling.Api/Program.cs), [Program.cs:703](../../backend/Pugling.Api/Program.cs)).
  Der stillgelegte Azure-Workflow kopiert dafür `frontend/dist` nach `wwwroot`
  ([deploy-azure.yml:67–68](../../.github/workflows/deploy-azure.yml)). Lokal gibt es dafür kein Skript.
- **CORS:** erlaubt per Vorgabe nur `http://localhost:5173`, einstellbar über `Cors:Origins`
  ([Program.cs:246–252](../../backend/Pugling.Api/Program.cs)). Im Ein-Host-Betrieb und über den Vite-Proxy ist
  jede Anfrage same-origin – CORS greift dann gar nicht.
- **Umgebung:** Außerhalb von `Development` verlangt die API einen `Jwt:Key`
  ([Program.cs:271](../../backend/Pugling.Api/Program.cs)), und der Seed läuft nicht mehr von selbst
  ([Program.cs:662](../../backend/Pugling.Api/Program.cs)). In `Development` liest jedes Konto alle Anmerkungen
  (`GlobalRead`, [Program.cs:209](../../backend/Pugling.Api/Program.cs)).
- **PWA:** Manifest mit `display: standalone` und Service Worker über `vite-plugin-pwa`
  ([vite.config.ts](../../frontend/vite.config.ts)). Browser registrieren einen Service Worker nur in einem
  sicheren Kontext (HTTPS oder `localhost`). Unter `http://192.168.…` gibt es also keinen Offline-Cache, und
  Android-Chrome bietet keine echte Installation an, nur eine Verknüpfung auf dem Startbildschirm, die im Browser
  öffnet. Wie sich iOS dabei verhält, ist nicht am Gerät geprüft ([B-31](B-31-geraete-vorbehalt-klang.md)).
- **Anmeldung:** PIN-Login, gebremst auf 10 Versuche je IP und Minute
  ([Program.cs:259–265](../../backend/Pugling.Api/Program.cs)); das Token gilt 12 Stunden
  ([TokenService.cs:12](../../backend/Pugling.Api/Auth/TokenService.cs)), das Kind meldet sich also täglich neu an.
- **Windows-Firewall:** Ein eingehender Port 5200 ist auf einem Windows-Rechner von Haus aus gesperrt. Das ist
  Systemeinstellung, kein Code.

## Die echte Lücke

Am Code fehlt wenig: CORS und API-Adresse sind schon richtig, und der Ein-Host-Betrieb existiert. Es fehlen ein
**Startweg** für den Heimnetz-Betrieb (Frontend bauen, nach `wwwroot` legen, API auf `0.0.0.0` binden, mit
eigenem `Jwt:Key` und ohne Seed), die **Schritte für Firewall und feste Adresse** und eine Antwort auf die Frage,
ob die PWA ohne HTTPS reicht.

## Offene Punkte

1. **Betriebsart:** Ein-Host-Bau (eine Adresse `http://<rechner>:5200`, Frontend gebaut in `wwwroot`) oder
   Entwicklungsbetrieb (Vite mit `--host` auf `:5173`, Backend dahinter)? *Empfehlung:* Ein-Host-Bau. Eine Adresse,
   ein offener Port, kein Vite-Server, der bei jedem Speichern neu lädt. Kosten: Nach Frontend-Änderungen muss neu
   gebaut werden.
2. **Umgebung:** `Production` mit eigenem `Jwt:Key` (Umgebungsvariable im Startskript) oder `Development`?
   *Empfehlung:* `Production`. Kein Seed gegen die echte Datenbank, keine Entwickler-Lesesicht auf Anmerkungen im
   WLAN. Kosten: Der Schlüssel muss einmal erzeugt und gesichert werden; geht er verloren, sind nur die laufenden
   Anmeldungen ungültig.
3. **HTTPS / PWA:** Reicht zunächst eine Verknüpfung auf dem Startbildschirm ohne Offline-Cache, oder soll die App
   echt installierbar sein? Dann braucht es ein Zertifikat, dem das Handy vertraut (z. B. `mkcert` und die
   Stammzertifizierungsstelle auf dem Handy installieren). *Empfehlung:* erst ohne HTTPS, am echten Handy prüfen
   (B-31), Zertifikat als eigene Story, falls das Vollbild fehlt.
4. **Adresse:** Feste IP per Reservierung im Router oder der Rechnername (z. B. `rechner.fritz.box`)?
   *Empfehlung:* Reservierung im Router, die IP steht dann als Lesezeichen im Handy.
5. **Start:** Von Hand per Skript oder automatisch beim Anmelden an Windows (Aufgabenplanung)? *Empfehlung:*
   erst ein Skript (`scripts/heimnetz.ps1`), Autostart später.
6. **Datenbank:** Läuft der Heimnetz-Server auf der echten `backend/Pugling.Api/pugling.db`, also derselben, die
   die Entwicklung nutzt? *Empfehlung:* ja, dann sieht der Vater im Vater-Web dieselben Daten. Kosten: Ein
   parallel laufender Entwicklungsserver darf nicht gleichzeitig auf dieselbe Datei schreiben – das Skript prüft,
   ob Port 5200 schon belegt ist.
7. **Rechner wach:** Der Rechner darf nicht in den Energiesparmodus, solange das Kind lernen will. Das ist eine
   Einstellung bei dir, keine Code-Frage – nur zur Kenntnis.

## Akzeptanzkriterien (Entwurf)

1. Ein Befehl auf dem Rechner baut das Frontend, legt es nach `wwwroot` und startet die API erreichbar im WLAN.
2. Ein Handy im selben WLAN öffnet `http://<adresse>:5200/sohn`, das Kind meldet sich an und spielt eine Position.
3. Der Vater erreicht `/vater` über dieselbe Adresse.
4. Gegen die echte Datenbank läuft kein Seed.
5. Die Firewall-Schritte stehen in der Doku und wurden von dir ausgeführt.

## Verlauf

- **2026-11-10** — angelegt (Prüfgang am Access-Lehrwerk: Übungen, Vater-Sicht, Handy).
- **2026-11-11** — Entscheidung des Nutzers vorab: Der Server läuft zunächst auf dem lokalen Rechner des Vaters und ist
  über das Heimnetz erreichbar. Hosting und Tunnel sind damit vorerst zurückgestellt.
- **2026-11-11** — ausformuliert: CORS und API-Adresse sind schon richtig (relativ, Ein-Host-Betrieb existiert); offen
  sind Startweg, Umgebung, HTTPS/PWA, Adresse und Firewall – sieben Fragen an den Nutzer.
