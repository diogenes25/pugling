#!/usr/bin/env bash
# Reine API-Checks gegen eine BEREITS LAUFENDE Pugling-API.
# Startet/stoppt selbst KEINEN Server (das übernimmt der /smoke-test-Ablauf via Hintergrund-Task),
# damit dieser Aufruf nicht blockiert. Erwartet die Basis-URL als $1 (Default http://localhost:5280).
# Exit 0 = alle Checks grün. Voraussetzung: frisch geseedete DB (Vater id=1/PIN 0000, Kind id=1, Vokabel-Store aus Seed).
#
# Prüft das aktuelle Positions-Modell: Plan = Container aus Katalog-Übungen; Test/Üben laufen pro Position
# unter .../study-plans/{planId}/positions/{positionId}/…  (das alte plan-weite /tests gibt es nicht mehr).
set -uo pipefail

BASE="${1:-http://localhost:5280}"
fail=0
pass() { echo "  ✅ $1"; }
bad()  { echo "  ❌ $1"; fail=1; }
jget() { python -c "import sys,json;d=json.load(sys.stdin);print(d.get('$1',''))" 2>/dev/null; }

if ! curl -s -o /dev/null "$BASE/openapi/v1.json"; then
  echo "❌ API unter $BASE nicht erreichbar. Erst den Server (Temp-DB) starten – siehe /smoke-test."
  exit 1
fi

echo "▶ Checks gegen $BASE:"

# 1) Ohne Token auf eine geschützte Positions-Subroute → 401
code=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/api/v1/study-plans/999/positions")
[ "$code" = "401" ] && pass "401 ohne Token" || bad "erwartete 401, war $code"

# 2) Vater-Login liefert Token
TOK=$(curl -s -X POST "$BASE/api/v1/auth/father" -H "Content-Type: application/json" -d '{"fatherId":1,"pin":"0000"}' | jget token)
[ -n "$TOK" ] && pass "Vater-Login liefert Token" || bad "Login fehlgeschlagen"
AUTH=(-H "Authorization: Bearer $TOK")

# 3) Ownership-Filter: fremder/fehlender Plan → 404 (beweist den [ServiceFilter]-Pfad)
code=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/api/v1/study-plans/999/positions" "${AUTH[@]}")
[ "$code" = "404" ] && pass "Ownership-Filter: 404 für fremden/fehlenden Plan" || bad "erwartete 404, war $code"

# 4) auth/me erfordert Token (Regressionsschutz für den AllowAnonymous-Fix)
code=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/api/v1/auth/me")
[ "$code" = "401" ] && pass "auth/me ohne Token → 401" || bad "auth/me sollte 401 sein, war $code"

# 5) Zwei Vokabeln aus dem geseedeten Store holen (robust statt hartkodiert).
# Refs sind ID-basiert: die Übung referenziert die Store-Einträge über vocabularyId, nicht mehr über den Key.
STORE=$(curl -s "$BASE/api/v1/learn/vocabulary?take=2" "${AUTH[@]}")
KEYS=$(echo "$STORE" | python -c "import sys,json;print(' '.join(v['key'] for v in json.load(sys.stdin)[:2]))" 2>/dev/null)
REFS_JSON=$(echo "$STORE" | python -c "import sys,json;print(json.dumps([{'vocabularyId':v['id']} for v in json.load(sys.stdin)[:2]]))" 2>/dev/null)
[ -n "$KEYS" ] && pass "Vokabel-Store liefert Keys ($KEYS)" || bad "keine Vokabeln im Store"

# 6) Fach + Kapitel + Vokabelübung (referenziert die Store-Vokabeln per vocabularyId) anlegen
SUBJ=$(curl -s -X POST "$BASE/api/v1/learn/subjects" "${AUTH[@]}" -H "Content-Type: application/json" -d '{"name":"Smoke-Fach"}' | jget id)
CHAP=$(curl -s -X POST "$BASE/api/v1/learn/subjects/$SUBJ/chapters" "${AUTH[@]}" -H "Content-Type: application/json" -d '{"name":"Smoke-Kapitel","orderIndex":1}' | jget id)
# Titel bewusst ASCII: Git-Bash/curl unter Windows verstümmeln Umlaute im -d-Body zu ungültigem UTF-8.
EX=$(curl -s -X POST "$BASE/api/v1/learn/subjects/$SUBJ/chapters/$CHAP/vocabulary" "${AUTH[@]}" -H "Content-Type: application/json" \
  -d "{\"title\":\"Smoke-Uebung\",\"orderIndex\":1,\"rewardPoints\":10,\"config\":{\"direction\":\"front-to-back\",\"refs\":$REFS_JSON}}" | jget id)
[ -n "$EX" ] && pass "Vokabeluebung angelegt (id=$EX)" || bad "Uebung anlegen fehlgeschlagen"

# 7) Plan (Container) + Position auf die Übung
PID=$(curl -s -X POST "$BASE/api/v1/study-plans" "${AUTH[@]}" -H "Content-Type: application/json" \
  -d '{"childId":1,"title":"Smoke Plan","durationDays":5}' | jget id)
[ -n "$PID" ] && pass "Plan angelegt (id=$PID)" || bad "Plan anlegen fehlgeschlagen"
POS=$(curl -s -X POST "$BASE/api/v1/study-plans/$PID/positions" "${AUTH[@]}" -H "Content-Type: application/json" \
  -d "{\"exerciseId\":$EX}" | jget id)
[ -n "$POS" ] && pass "Position angelegt (id=$POS)" || bad "Position anlegen fehlgeschlagen"

# 8) Positions-Test starten (Vater, SelfAssess) + submit „gewusst" → 100 % bestanden
if [ -n "${POS:-}" ]; then
  ATT=$(curl -s -X POST "$BASE/api/v1/study-plans/$PID/positions/$POS/tests" "${AUTH[@]}" -H "Content-Type: application/json" -d '{"stage":2}')
  AID=$(echo "$ATT" | jget attemptId)
  [ -n "$AID" ] && pass "Positions-Test gestartet (attemptId=$AID)" || bad "Test-Start fehlgeschlagen: $ATT"
  # Der Test-Start liefert nur noch totalItems (Aufgaben werden im Klausur-Fluss schrittweise über /next+/answer
  # geholt); der Bulk-Submit erwartet je Index eine Antwort. Für SelfAssess reicht wasKnown je Index 0..n-1.
  ans=$(echo "$ATT" | python -c "import sys,json;n=json.load(sys.stdin).get('totalItems',0);print(','.join('{\"itemIndex\":%d,\"wasKnown\":true}'%i for i in range(n)))" 2>/dev/null)
  RES=$(curl -s -X POST "$BASE/api/v1/study-plans/$PID/positions/$POS/tests/$AID/submit" "${AUTH[@]}" -H "Content-Type: application/json" -d "{\"answers\":[$ans]}")
  ok=$(echo "$RES" | python -c "import sys,json;d=json.load(sys.stdin);print('yes' if d.get('passed') and d.get('scorePercent')==100 else 'no')" 2>/dev/null)
  [ "$ok" = "yes" ] && pass "Submit: 100% bestanden" || bad "Submit-Ergebnis unerwartet: $RES"
fi

echo
if [ "$fail" = "0" ]; then echo "✅ Smoke-Checks bestanden."; else echo "❌ Smoke-Checks mit Fehlern."; fi
exit "$fail"
