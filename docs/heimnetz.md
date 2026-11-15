---
tags: [typ/referenz, bereich/qualitaet, rolle/student, rolle/supervisor]
aliases: [Heimnetz, Handy-Zugang, Heimnetz-Betrieb]
---

# Heimnetz-Betrieb: Pugling auf dem Rechner, der Sohn am Handy

Der Server läuft auf dem Rechner des Vaters, das Handy erreicht ihn im selben WLAN. Entschieden in
[B-468](backlog/B-468-sohn-erreicht-die-app-nur-am-pc.md); Hosting und Tunnel sind zurückgestellt.

## Starten

```powershell
./scripts/heimnetz.ps1
```

Das Skript baut das Frontend, veröffentlicht die API und startet sie unter Port 5200 auf allen Netzwerkadressen
des Rechners, in der Umgebung `Production`, mit der echten `backend/Pugling.Api/pugling.db`. Am Ende druckt es die
Adressen für Sohn und Vater, etwa `http://192.168.178.55:5200/sohn`. Beenden mit Strg+C.

- `-SkipBuild` startet ohne Neubau, wenn sich am Code nichts geändert hat.
- Vor einem Neustart mit neuem Stand prüft `pwsh scripts/artefakt-smoke.ps1`, ob das veröffentlichte Artefakt überhaupt
  hochkommt und Sohn- wie Vater-Seite ausliefert – gegen eine Wegwerf-Datenbank auf Port 5299, ohne die echte
  `pugling.db` zu berühren (B-47; derselbe Lauf steht als Job `artefakt` in CI).
- Läuft schon etwas auf Port 5200 (meist der Entwicklungsserver), bricht das Skript ab: zwei Server dürfen nicht
  gleichzeitig in dieselbe Datenbank schreiben. **Ein Entwicklungsserver auf einem anderen Port erkennt das
  Skript nicht** – dann vorher selbst beenden.
- Beim ersten Start legt es einen eigenen Signierschlüssel unter `%LOCALAPPDATA%\Pugling\jwt.key` an. Geht die
  Datei verloren, entsteht ein neuer, und alle müssen sich neu anmelden – sonst passiert nichts.
- Gegen die echte Datenbank läuft kein Seed (`Production`).

## Einmalige Schritte am Rechner

Diese Schritte ändern Systemeinstellungen und laufen deshalb **nicht** im Skript. Die PowerShell-Befehle brauchen
eine **PowerShell als Administrator**.

1. **Netzwerkprofil „Privat“.** Die Firewall-Regel unten gilt nur im privaten Profil, damit der Port in einem
   fremden WLAN zu bleibt. Profil ansehen und, falls `Public`, umstellen (Name aus der ersten Zeile übernehmen):

   ```powershell
   Get-NetConnectionProfile
   Set-NetConnectionProfile -InterfaceAlias "Ethernet 2" -NetworkCategory Private
   ```

2. **Firewall: Port 5200 eingehend erlauben**, nur im privaten Profil:

   ```powershell
   New-NetFirewallRule -DisplayName "Pugling Heimnetz" -Direction Inbound -Protocol TCP -LocalPort 5200 -Action Allow -Profile Private
   ```

   Fragt Windows beim ersten Start zusätzlich nach „dotnet“, dort nur **Private Netzwerke** ankreuzen.
   Rückgängig: `Remove-NetFirewallRule -DisplayName "Pugling Heimnetz"`.

3. **Feste Adresse im Router.** In der FRITZ!Box unter *Heimnetz → Netzwerk → Netzwerkverbindungen* den Rechner
   bearbeiten und „Diesem Netzwerkgerät immer die gleiche IPv4-Adresse zuweisen“ setzen. Sonst kann die Adresse
   im Lesezeichen des Handys nach einem Neustart des Routers ins Leere zeigen.

4. **Rechner wach halten**, solange gelernt wird. Am Netzteil nie in den Energiesparmodus:

   ```powershell
   powercfg /change standby-timeout-ac 0
   ```

   Rückgängig etwa mit `powercfg /change standby-timeout-ac 30` (Minuten).

## Am Handy

1. Im selben WLAN die Adresse aus der Ausgabe des Skripts öffnen, `…/sohn` für den Sohn.
2. Über das Browser-Menü „Zum Startbildschirm hinzufügen“.

Was ohne HTTPS fehlt (Entscheidung 3 in B-468): Browser registrieren den Service Worker nur auf HTTPS oder
`localhost`. Es gibt also keinen Offline-Cache, und Android legt nur eine Verknüpfung an, die im Browser öffnet,
keine installierte App im Vollbild. Wie sich das am echten Gerät anfühlt, prüft [B-31](backlog/B-31-geraete-vorbehalt-klang.md).
Mit HTTPS über Tailscale (nächster Abschnitt) entfällt das.

## HTTPS über Tailscale: Pugling als App, in Family Link „Immer erlaubt“

Family Link kann nur **installierte Apps** von der Bildschirmzeit ausnehmen, keine Webseiten. Chrome auf Android
installiert Pugling als echte App (eine WebAPK, mit eigenem Eintrag in der App-Liste), aber nur über HTTPS mit
gültigem Zertifikat. Das liefert Tailscale: `tailscale serve` stellt Pugling unter
`https://<rechner>.<tailnet>.ts.net` bereit und reicht an Port 5200 weiter. Erreichbar ist die Adresse nur für
Geräte im eigenen Tailnet – auch unterwegs, ohne Port im Router zu öffnen.

Einmalig:

1. **Tailscale auf dem Rechner** installieren (tailscale.com/download) und mit dem Vater-Konto anmelden.
2. **In der Tailscale-Konsole** (login.tailscale.com → *DNS*) **MagicDNS** und **HTTPS Certificates** einschalten.
   Ohne Zertifikate scheitert `tailscale serve`; das Skript meldet das als Warnung.
3. **`./scripts/heimnetz.ps1`** neu starten. Es richtet `tailscale serve` selbst ein (abschaltbar mit
   `-NoTailscale`, nur für den Vorgabe-Port 5200) und druckt die `https://…ts.net/sohn`-Adresse. Der erste Aufruf
   holt das Zertifikat und kann einige Sekunden dauern.
4. **Am Kinderhandy:** Tailscale aus dem Play Store installieren (in Family Link genehmigen) und **mit dem
   Vater-Konto** anmelden – das Kind braucht kein eigenes. In den Android-Einstellungen unter *Netzwerk → VPN →
   Tailscale* „Durchgehend aktives VPN“ einschalten, sonst ist Pugling weg, sobald die Verbindung abreißt.
5. **Empfohlen: Kinderhandys einschränken.** Per Standard darf jedes Gerät im Tailnet jedes andere erreichen.
   In der Konsole unter *Access controls* die Kinderhandys mit einem Tag (`tag:kind`) versehen und ihnen nur
   den Rechner auf Port 443 erlauben.
6. **Pugling installieren:** In Chrome `https://<rechner>.<tailnet>.ts.net/sohn` öffnen, Menü → **„App
   installieren“** (nicht „Zum Startbildschirm hinzufügen“ – das ist wieder nur eine Verknüpfung). Danach steht
   Pugling mit eigenem Symbol in der App-Liste.
7. **Family Link:** Kind → *Apps* (bzw. *Steuerelemente → App-Limits*) → Pugling → **„Immer erlaubt“**.
   Taucht Pugling dort nicht auf, ist es nur als Verknüpfung angelegt – Schritt 6 wiederholen.

Grenzen: „Immer erlaubt“ zählt nicht zur Bildschirmzeit, ist in der **Ruhezeit** aber gesperrt, solange das am
Gerät nicht eigens freigegeben ist. Die neue Adresse ist ein anderer Ursprung als `http://<ip>:5200` – das Kind
meldet sich dort einmal neu an; der Lernstand liegt in der Datenbank und bleibt.

Die Anmeldung gilt 12 Stunden – das Kind meldet sich also jeden Tag neu mit Helden-Nummer und PIN an.

## Was im Heimnetz sonst noch erreichbar ist

Die API-Dokumentation (`/swagger`, `/scalar/v1`) wird auch in `Production` ausgeliefert. Sie zeigt nur die
Schnittstelle, keine Daten; jeder Aufruf braucht weiter ein Token. Im eigenen WLAN ist das hinnehmbar, vor einem
Betrieb im Internet nicht.

---

**Verwandt:** [deployment-azure.md](deployment-azure.md) (stillgelegter Ein-Host-Deploy, dieselbe Auslieferung
des Frontends durch die API) · [B-468](backlog/B-468-sohn-erreicht-die-app-nur-am-pc.md)
