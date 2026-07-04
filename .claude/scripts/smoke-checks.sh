#!/usr/bin/env bash
# Reine API-Checks gegen eine BEREITS LAUFENDE Pugling-API.
# Startet/stoppt selbst KEINEN Server (das übernimmt der /smoke-test-Ablauf via Hintergrund-Task),
# damit dieser Aufruf nicht blockiert. Erwartet die Basis-URL als $1 (Default http://localhost:5280).
# Exit 0 = alle Checks grün. Voraussetzung: frisch geseedete DB (Vater id=1/PIN 0000, Vokabel-Keys aus Seed).
set -uo pipefail

BASE="${1:-http://localhost:5280}"
fail=0
pass() { echo "  ✅ $1"; }
bad()  { echo "  ❌ $1"; fail=1; }

if ! curl -s -o /dev/null "$BASE/openapi/v1.json"; then
  echo "❌ API unter $BASE nicht erreichbar. Erst den Server (Temp-DB) starten – siehe /smoke-test."
  exit 1
fi

echo "▶ Checks gegen $BASE:"

# 1) Ohne Token auf geschützte Plan-Subroute → 401
code=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE/api/v1/study-plans/999/tests" -H "Content-Type: application/json" -d '{}')
[ "$code" = "401" ] && pass "401 ohne Token" || bad "erwartete 401, war $code"

# 2) Vater-Login liefert Token
TOK=$(curl -s -X POST "$BASE/api/v1/auth/father" -H "Content-Type: application/json" -d '{"fatherId":1,"pin":"0000"}' \
  | python -c "import sys,json;print(json.load(sys.stdin).get('token',''))" 2>/dev/null)
[ -n "$TOK" ] && pass "Vater-Login liefert Token" || bad "Login fehlgeschlagen"
AUTH=(-H "Authorization: Bearer $TOK")

# 3) Ownership-Filter: fehlender Plan → 404 (beweist den [ServiceFilter]-Pfad)
code=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE/api/v1/study-plans/999/tests" "${AUTH[@]}" -H "Content-Type: application/json" -d '{}')
[ "$code" = "404" ] && pass "Ownership-Filter: 404 für fremden/fehlenden Plan" || bad "erwartete 404, war $code"

# 4) auth/me erfordert Token (Regressionsschutz für den AllowAnonymous-Fix)
code=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/api/v1/auth/me")
[ "$code" = "401" ] && pass "auth/me ohne Token → 401" || bad "auth/me sollte 401 sein, war $code"

# 5) Plan anlegen (Vokabel) mit Seed-Keys
PLAN=$(curl -s -X POST "$BASE/api/v1/study-plans" "${AUTH[@]}" -H "Content-Type: application/json" \
  -d '{"childId":1,"title":"Smoke Plan","method":"Vocabulary","durationDays":5,"contentKeys":["en_house_de_haus","en_go_de_gehen"],"dailyTestRequired":true}')
PID=$(echo "$PLAN" | python -c "import sys,json;print(json.load(sys.stdin).get('id',''))" 2>/dev/null)
[ -n "$PID" ] && pass "Plan angelegt (id=$PID)" || bad "Plan anlegen fehlgeschlagen: $PLAN"

# 6) Test starten (Vater, SelfAssess) + submit → bestanden & Punkte vergeben
if [ -n "${PID:-}" ]; then
  ATT=$(curl -s -X POST "$BASE/api/v1/study-plans/$PID/tests" "${AUTH[@]}" -H "Content-Type: application/json" -d '{"stage":"SelfAssess"}')
  AID=$(echo "$ATT" | python -c "import sys,json;print(json.load(sys.stdin).get('attemptId',''))" 2>/dev/null)
  IDS=$(echo "$ATT" | python -c "import sys,json;print(' '.join(str(i['vocabularyId']) for i in json.load(sys.stdin).get('items',[])))" 2>/dev/null)
  [ -n "$AID" ] && pass "Test gestartet (attemptId=$AID)" || bad "Test-Start fehlgeschlagen: $ATT"
  ans=$(python -c "import sys;print(','.join('{\"vocabularyId\":%s,\"wasKnown\":true}'%i for i in sys.argv[1:]))" $IDS)
  RES=$(curl -s -X POST "$BASE/api/v1/study-plans/$PID/tests/$AID/submit" "${AUTH[@]}" -H "Content-Type: application/json" -d "{\"answers\":[$ans]}")
  ok=$(echo "$RES" | python -c "import sys,json;d=json.load(sys.stdin);print('yes' if d.get('passed') and d.get('scorePercent')==100 and d.get('dayProgress',{}).get('pointsAwarded',0)>0 else 'no')" 2>/dev/null)
  [ "$ok" = "yes" ] && pass "Submit: 100% bestanden + Punkte vergeben" || bad "Submit-Ergebnis unerwartet: $RES"
fi

echo
if [ "$fail" = "0" ]; then echo "✅ Smoke-Checks bestanden."; else echo "❌ Smoke-Checks mit Fehlern."; fi
exit "$fail"
