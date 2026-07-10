#!/usr/bin/env bash
# Gemeinsamer Wegwerf-API-Helfer für die Rollen-Skills creator/supervisor/student.
# Startet die Pugling-API isoliert gegen eine Temp-DB auf Port 5280 (dieselbe erprobte Recipe wie
# /smoke-test), damit die echte pugling.db unberührt bleibt und die Seed-IDs stabil/reproduzierbar sind.
#
# Zwei Nutzungsarten:
#   1) Als Kommando:  bash .claude/scripts/tutorial-api.sh {build|serve|wait|stop}
#        build : dotnet build (einmal)
#        serve : Server im VORDERGRUND starten  -> IMMER per run_in_background:true starten,
#                sonst blockiert der Tool-Aufruf (der Server hält die Ausgabe-Pipe offen)
#        wait  : blockiert (Vordergrund), bis /openapi/v1.json antwortet
#        stop  : 5280-Instanz killen + Temp-DB löschen (idempotent; auch zum Vor-Aufräumen)
#   2) Gesourcet:  source .claude/scripts/tutorial-api.sh
#        -> Helfer jget / login_father / login_child / api_get / api_post (BASE/TOK vorausgesetzt)
#
# Windows/Git-Bash-Fallstricke (aus smoke-checks.sh übernommen, nicht abweichen):
#   - relative *.db (kein /tmp-Pfad: "unable to open database file")
#   - ASCII-only in -d-Bodies (Umlaute werden sonst zu ungültigem UTF-8)
#   - Server als Hintergrund-Task starten

BASE="${TUTORIAL_API_BASE:-http://localhost:5280}"
API_DIR="backend/Pugling.Api"
DB="pugling_smoke.db"

# JSON-Feld aus stdin ziehen (wie smoke-checks.sh:15).
jget() { python -c "import sys,json;d=json.load(sys.stdin);print(d.get('$1',''))" 2>/dev/null; }

_port_pid() { netstat -ano | grep ":5280" | grep LISTENING | awk '{print $NF}' | head -1; }

api_stop() {
  local pid; pid=$(_port_pid)
  [ -n "$pid" ] && taskkill //PID "$pid" //F >/dev/null 2>&1 || true
  rm -f "$API_DIR/$DB"* 2>/dev/null || true
}

api_build() { dotnet build "$API_DIR" -v q -clp:NoSummary; }

# Im Hintergrund aufrufen! Läuft im Vordergrund bis zum Kill.
api_serve() {
  cd "$API_DIR" || exit 1
  ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="$BASE" \
    ConnectionStrings__Default="Data Source=$DB" \
    dotnet run --no-launch-profile --no-build
}

api_wait() {
  for _ in $(seq 1 40); do
    curl -s -m 3 -o /dev/null "$BASE/openapi/v1.json" && return 0
    sleep 1
  done
  echo "❌ API unter $BASE nicht bereit" >&2; return 1
}

# Seed-Logins (Defaults = Seed-Zugänge). Creator-Archetyp: login_father 2 9999 (Lehrer Herr Schmidt).
login_father() { curl -s -X POST "$BASE/api/v1/auth/father" -H "Content-Type: application/json" -d "{\"fatherId\":${1:-1},\"pin\":\"${2:-0000}\"}" | jget token; }
login_child()  { curl -s -X POST "$BASE/api/v1/auth/child"  -H "Content-Type: application/json" -d "{\"childId\":${1:-1},\"pin\":\"${2:-1111}\"}"  | jget token; }

# Authentifizierte Aufrufe – setzt eine Variable TOK voraus.
api_get()  { curl -s "$BASE$1" -H "Authorization: Bearer $TOK"; }
api_post() { curl -s -X POST "$BASE$1" -H "Authorization: Bearer $TOK" -H "Content-Type: application/json" -d "$2"; }

# Als Kommando -> Subcommand; gesourcet -> nur Funktionen (kein set -u, um die aufrufende Shell nicht zu stören).
if [ "${BASH_SOURCE[0]}" = "${0}" ]; then
  set -uo pipefail
  case "${1:-}" in
    build) api_build ;;
    serve) api_serve ;;
    wait)  api_wait ;;
    stop)  api_stop ;;
    *) echo "usage: $0 {build|serve|wait|stop}" >&2; exit 2 ;;
  esac
fi
