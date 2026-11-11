---
tags: [typ/story, status/idee, bereich/qualitaet, rolle/student, rolle/supervisor]
aliases: [Handy-Zugang, Hosting, Heimnetz]
status: idee
prio: P1
art: Wunsch
groesse: ""
wo: "beides"
migration: ""
vertragsbruch: ""
quelle: Rollengang am Access-Lehrwerk 2026-11-10 (Kopie der pugling.db, Sohn im Handy-Format 375×812, Vater-Web)
unverifiziert: true
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

## Verlauf

- **2026-11-10** — angelegt (Prüfgang am Access-Lehrwerk: Übungen, Vater-Sicht, Handy).
