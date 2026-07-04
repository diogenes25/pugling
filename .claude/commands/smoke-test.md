---
description: API isoliert gegen eine Wegwerf-DB starten und den End-to-End-Flow (Auth, Ownership, Plan→Test→Submit) prüfen
allowed-tools: Bash, Read
---

Führe einen Smoke-Test der Pugling-API durch. Ziel: verifizieren, dass Auth, der Plan-Ownership-Filter
und ein kompletter Plan→Test→Submit-Flow (inkl. Punktevergabe) live funktionieren – **ohne** die echte
`pugling.db` anzufassen.

Wichtige, in diesem Repo erprobte Randbedingungen (nicht abweichen):

- **DB-Pfad relativ** (`Data Source=pugling_smoke.db`) – ein `mktemp`-`/tmp/..`-Pfad scheitert unter Windows-.NET
  („unable to open database file"). Relative `*.db` liegen im Projekt und sind gitignored.
- **Server als Hintergrund-Task** starten (`run_in_background: true`), sonst blockiert der Tool-Aufruf, weil
  der laufende Server die Ausgabe-Pipe offen hält.
- Port **5280** (nicht 5200, damit eine evtl. laufende Dev-Instanz nicht kollidiert).

Ablauf:

1. Bauen: `dotnet build backend/Pugling.Api -v q -clp:NoSummary`
2. Alte Instanz/Reste entfernen:
   `PID=$(netstat -ano | grep ":5280" | grep LISTENING | awk '{print $NF}' | head -1); [ -n "$PID" ] && taskkill //PID $PID //F; rm -f backend/Pugling.Api/pugling_smoke.db*`
3. Server im **Hintergrund** starten (aus `backend/Pugling.Api`):
   `ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="http://localhost:5280" ConnectionStrings__Default="Data Source=pugling_smoke.db" dotnet run --no-launch-profile --no-build`
4. Auf Bereitschaft warten (Vordergrund, gebremst):
   `for i in $(seq 1 30); do curl -s -m 3 -o /dev/null http://localhost:5280/openapi/v1.json && break; sleep 1; done`
5. Checks ausführen: `bash .claude/scripts/smoke-checks.sh http://localhost:5280`
6. **Immer** aufräumen (auch bei Fehler): Server per `taskkill` auf Port 5280 stoppen und
   `rm -f backend/Pugling.Api/pugling_smoke.db*`.

Berichte am Ende knapp: welche Checks grün/rot waren. Bei Rot die relevante Server-Logzeile aus der
Hintergrund-Task-Ausgabe zitieren. Für schnelle, CI-taugliche Verifikation ist `dotnet test`
(Integrationstests in `backend/Pugling.Api.Tests`) die erste Wahl; dieser Smoke-Test ist für das
Prüfen der echten HTTP-Schicht gedacht.

$ARGUMENTS
