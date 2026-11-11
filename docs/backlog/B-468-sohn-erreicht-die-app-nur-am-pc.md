---
tags: [typ/story, status/abgenommen, bereich/qualitaet, rolle/student, rolle/supervisor]
aliases: [Handy-Zugang, Hosting, Heimnetz]
status: abgenommen
prio: P1
art: Wunsch
groesse: S
wo: "beides"
migration: nein
vertragsbruch: nein
quelle: Rollengang am Access-Lehrwerk 2026-11-10 (Kopie der pugling.db, Sohn im Handy-Format 375×812, Vater-Web)
wartet_auf: ""
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

1. ~~**Betriebsart:**~~ Ein-Host-Bau (eine Adresse `http://<rechner>:5200`, Frontend gebaut in `wwwroot`) oder
   Entwicklungsbetrieb (Vite mit `--host` auf `:5173`, Backend dahinter)? *Empfehlung:* Ein-Host-Bau. Eine Adresse,
   ein offener Port, kein Vite-Server, der bei jedem Speichern neu lädt. Kosten: Nach Frontend-Änderungen muss neu
   gebaut werden.
2. ~~**Umgebung:**~~ `Production` mit eigenem `Jwt:Key` (Umgebungsvariable im Startskript) oder `Development`?
   *Empfehlung:* `Production`. Kein Seed gegen die echte Datenbank, keine Entwickler-Lesesicht auf Anmerkungen im
   WLAN. Kosten: Der Schlüssel muss einmal erzeugt und gesichert werden; geht er verloren, sind nur die laufenden
   Anmeldungen ungültig.
3. ~~**HTTPS / PWA:**~~ Reicht zunächst eine Verknüpfung auf dem Startbildschirm ohne Offline-Cache, oder soll die App
   echt installierbar sein? Dann braucht es ein Zertifikat, dem das Handy vertraut (z. B. `mkcert` und die
   Stammzertifizierungsstelle auf dem Handy installieren). *Empfehlung:* erst ohne HTTPS, am echten Handy prüfen
   (B-31), Zertifikat als eigene Story, falls das Vollbild fehlt.
4. ~~**Adresse:**~~ Feste IP per Reservierung im Router oder der Rechnername (z. B. `rechner.fritz.box`)?
   *Empfehlung:* Reservierung im Router, die IP steht dann als Lesezeichen im Handy.
5. ~~**Start:**~~ Von Hand per Skript oder automatisch beim Anmelden an Windows (Aufgabenplanung)? *Empfehlung:*
   erst ein Skript (`scripts/heimnetz.ps1`), Autostart später.
6. ~~**Datenbank:**~~ Läuft der Heimnetz-Server auf der echten `backend/Pugling.Api/pugling.db`, also derselben, die
   die Entwicklung nutzt? *Empfehlung:* ja, dann sieht der Vater im Vater-Web dieselben Daten. Kosten: Ein
   parallel laufender Entwicklungsserver darf nicht gleichzeitig auf dieselbe Datei schreiben – das Skript prüft,
   ob Port 5200 schon belegt ist.
7. ~~**Rechner wach:**~~ Der Rechner darf nicht in den Energiesparmodus, solange das Kind lernen will. Das ist eine
   Einstellung bei dir, keine Code-Frage – nur zur Kenntnis.

Alle sieben am 2026-11-11 vom Nutzer entschieden: „Übernehme sämtliche Empfehlungen“ → Entscheidungen 1–7.

## Entscheidungen

1. **Ein-Host-Betrieb.** Das gebaute Frontend wird von der API mit ausgeliefert, eine Adresse `http://<ip>:5200`,
   ein offener Port. *Begründung:* kein Vite-Server im Netz, kein Proxy, same-origin – CORS greift nicht.
   *Kosten:* Nach einer Frontend-Änderung muss neu gebaut werden (das Startskript tut das bei jedem Start).
   Ausgeliefert wird direkt aus `frontend/dist` (`--webroot`), nicht über eine Kopie nach `wwwroot` – sonst läge
   im Entwicklungsbetrieb ein veraltetes Frontend unter `:5200`.
2. **Umgebung `Production`** mit eigenem `Jwt:Key`. *Begründung:* Kein Seed gegen die echte Datenbank, keine
   Entwickler-Lesesicht auf alle Anmerkungen im WLAN. *Kosten:* Der Schlüssel wird beim ersten Start erzeugt und
   außerhalb des Repos unter `%LOCALAPPDATA%\Pugling\jwt.key` abgelegt; geht er verloren, muss sich nur jeder neu
   anmelden.
3. **Ohne HTTPS starten.** *Begründung:* Erst am echten Handy prüfen, was ohne Service Worker fehlt (B-31).
   *Kosten:* kein Offline-Cache, auf Android nur eine Verknüpfung statt einer installierten App. Ein Zertifikat
   wird bei Bedarf eine eigene Story.
4. **Feste IP per Reservierung im Router.** *Kosten:* ein Handgriff des Nutzers im Router; das Skript zeigt die
   Adresse beim Start an.
5. **Start von Hand per Skript** `scripts/heimnetz.ps1`, Autostart später. *Kosten:* Der Vater muss den Server
   starten, bevor der Sohn lernt.
6. **Echte Datenbank** `backend/Pugling.Api/pugling.db`. *Begründung:* Vater-Web und Sohn sehen dieselben Daten.
   *Kosten:* Zwei Server dürfen nicht gleichzeitig darauf schreiben – das Skript bricht ab, wenn der Port belegt
   ist. Läuft der Entwicklungsserver auf einem anderen Port, schützt das nicht; das steht in der Doku.
   Der Heimnetz-Server läuft aus einem eigenen Veröffentlichungs-Ordner, damit er die Build-Ausgabe von Entwicklung
   und Test-Tor nicht sperrt; Inhaltswurzel bleibt `backend/Pugling.Api`, damit Konfiguration und hochgeladene
   Bilder dieselben sind.
7. **Rechner wach halten** ist Einstellung des Nutzers, keine Code-Frage; die Schritte stehen in der Doku.

## Akzeptanzkriterien

1. `scripts/heimnetz.ps1` baut Frontend und API und startet die API in `Production`, gebunden an alle
   Netzwerkadressen, mit Webroot `frontend/dist` und der echten Datenbank.
2. Über die Netzwerkadresse des Rechners liefert `/sohn` die App, ein Kind meldet sich an und bekommt eine
   Übungsrunde; `/vater` ist unter derselben Adresse erreichbar.
3. Gegen die Datenbank läuft kein Seed; eine fehlende Datenbankdatei bricht ab, statt eine leere anzulegen.
4. Ist der Port belegt, bricht das Skript mit einer Meldung ab.
5. `docs/heimnetz.md` nennt Firewall-, Router- und Energiespar-Schritte; der Nutzer hat sie ausgeführt und die App
   am Handy geöffnet (Nutzer-Schritt, `wartet_auf`).

## Schätzung

**Größe: S** — ein Startskript und eine Doku-Seite, kein Code an API oder Frontend (Anker B-01).
`migration: nein`, `vertragsbruch: nein`.

**Risiken:** `--webroot` und `--contentRoot` müssen von `WebApplication.CreateBuilder(args)` übernommen werden
(nachmessen). Der Abbruch bei belegtem Port schützt nur denselben Port. Eine Firewall-Regel kann ich nicht testen –
eine Verbindung vom Rechner auf seine eigene Netzwerkadresse geht nicht durch die eingehende Filterung.

**Angriffsplan:** Skript schreiben → gegen eine Kopie der Datenbank auf einem anderen Port starten → über die
Netzwerkadresse `/sohn`, `/vater`, Login und Übungsrunde messen, Seed-Freiheit an Zeilenzahlen prüfen → Doku.

**Testweg:** kein Integrationstest (keine Zeile C#); Messung mit `curl` gegen den gestarteten Server auf der Kopie,
danach das Handy des Nutzers.

## Verlauf

- **2026-11-10** — angelegt (Prüfgang am Access-Lehrwerk: Übungen, Vater-Sicht, Handy).
- **2026-11-11** — Entscheidung des Nutzers vorab: Der Server läuft zunächst auf dem lokalen Rechner des Vaters und ist
  über das Heimnetz erreichbar. Hosting und Tunnel sind damit vorerst zurückgestellt.
- **2026-11-11** — ausformuliert: CORS und API-Adresse sind schon richtig (relativ, Ein-Host-Betrieb existiert); offen
  sind Startweg, Umgebung, HTTPS/PWA, Adresse und Firewall – sieben Fragen an den Nutzer.
- **2026-11-11** — gegrillt im Dialog: Nutzer übernimmt alle sieben Empfehlungen; geschätzt S, nur Skript und Doku.
- **2026-11-11** — in Arbeit, gebaut: `scripts/heimnetz.ps1` und [heimnetz.md](../heimnetz.md). Gemessen gegen eine
  Kopie der `pugling.db` auf Port 5282 über die Netzwerkadresse `192.168.178.55` (dieser Rechner, nicht das Handy):
  `Production`, Inhaltswurzel `backend/Pugling.Api`, Webroot `frontend/dist`; `/sohn`, `/vater`, `/`,
  `manifest.webmanifest` und `sw.js` liefern 200; Login als Sohn und Vater mit dem neuen Schlüssel; nach `plan`
  (B-463) und Einschalten meldet sich der Sohn **in der Oberfläche** im Handy-Format an und sieht die vier
  Wortlisten mit 3/4/2/2 fälligen Karten; eine Übungsrunde liefert 3 Karten. Kein Seed (Zeilenzahlen von
  Übungen, Kindern, Erwachsenen, Shop-Artikeln unverändert). Abbruch bei belegtem Port und bei fehlender
  Datenbank gemessen. Nachgebessert: Die Adressliste zeigte auch VirtualBox- und VPN-Adressen, jetzt nur Netze mit
  Standard-Gateway. Nicht messbar von hier: die Firewall (eine Verbindung auf die eigene Adresse geht nicht durch
  die eingehende Filterung) und das echte Handy – das wartet auf den Nutzer. Kein Reviewer: keine Zeile C# oder
  TypeScript geändert.
- **2026-11-11** — Nutzer hat die Schritte aus [heimnetz.md](../heimnetz.md) ausgeführt und `scripts/heimnetz.ps1`
  gegen die echte `pugling.db` gestartet: Das Handy zeigt unter `http://192.168.178.55:5200/sohn` den Loginschirm
  (Server-Log 13:23: `GET /index.html` → 200). Damit sind Firewall, Adresse und Auslieferung am echten Gerät
  belegt. Offen für AK 2: Anmeldung und Übungsrunde am Handy – auf der echten Datenbank hat der Sohn noch keinen
  aktiven Plan.
- **2026-11-11** — Auf Wunsch des Nutzers über den laufenden Heimnetz-Server an der echten Datenbank: die nie
  gespielten, inaktiven KI-Pläne 3 und 4 gelöscht (keine Tests, keine Übungsrunden daran) und mit `plan --child 1
  --series-unit 7 --types Vocabulary,Cloze --days 14` Plan 5 angelegt: die Wortlisten 13–16 mit Dosis 3/4/2/2,
  Karteikasten, getippter Test, Laufzeit 2026-11-11 bis 2026-11-24. Beide Lückentexte hat der Agent nach der
  Reparatur-Runde verworfen (siehe B-465), der Katalog ist unverändert. Der Plan ist inaktiv; Einschalten und
  Anmeldung am Handy liegen beim Nutzer.
- **2026-11-11** — abgenommen. Rollengang am echten Handy: Der Nutzer hat Plan 5 eingeschaltet, der Sohn hat sich am
  Handy angemeldet und von 13:43 bis 13:47 Uhr vier Übungsrunden auf den Wortlisten 13 und 14 gespielt und drei
  Tests abgelegt (`PracticeSessions` 1–4, `TestAttempts` 3; im Karteikasten 3 bzw. 4 Einträge – genau die
  Tagesdosis). AK 1–5 erfüllt. Commits `67a6af4` (Skript, Doku), `0c2067c` und `a71dfa9` (Befunde am Gerät). Kein
  Reviewer und keine Testzahl: keine Zeile C# oder TypeScript geändert. Offen und nicht Teil dieser Story: HTTPS
  für eine installierbare PWA (Entscheidung 3, bei Bedarf eigene Story) und der Autostart (Entscheidung 5).
