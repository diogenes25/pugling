#!/usr/bin/env bash
# Stop-Hook: Budget-Tor für den *Startkontext* – die Dateien, die bei jeder Sitzung mitgeladen werden.
#
# Warum überhaupt: CLAUDE.md wuchs zwischen 2026-07-04 und 2026-07-30 von 5.512 auf 29.987 Bytes (5,4×).
# Ein Trimm am 07-28 hielt zwei Tage, dann war der alte Stand wieder da – unbemerkt, weil Dateigröße
# niemandem auffällt. Das Problem ist nicht der Platz (~12k Tokens auf 1M Fenster), sondern die
# Verdrängung: je mehr Nachschlagewerk resident liegt, desto schwächer wirken die Regeln, die wirklich
# jede Änderung steuern. Und eine resident *falsche* Aussage (die CS1591-Behauptung war am 2026-07-30
# zwei Wochen veraltet) ist teurer als jedes Byte.
#
# Dieses Tor **warnt und blockt nicht** (exit 0). Größe an Doku zu blocken wäre feindlich; der Zweck ist,
# das Wachstum sichtbar zu machen. Die Budgets liegen knapp über dem aufgeräumten Stand vom 2026-07-30,
# damit das Tor Zuwachs meldet und nicht den Status quo.
#
# Bewusst **kein** .cs-Vorfilter wie im Test-Tor: eine reine Doku-Änderung berührt kein .cs und wäre
# damit unsichtbar – gerade sie ist hier aber der Anlass.
set -uo pipefail

input=$(cat 2>/dev/null || true)

# Läuft der Stop-Hook bereits, nicht erneut melden (gleiche Schleifensicherung wie im Test-Tor).
active=$(printf '%s' "$input" | python -c "import sys,json; print(json.load(sys.stdin).get('stop_hook_active', False))" 2>/dev/null || echo False)
[ "$active" = "True" ] && exit 0

[ "${PUGLING_SKIP_CONTEXT_BUDGET:-}" = "1" ] && exit 0

# Der Hook setzt CLAUDE_PROJECT_DIR. Der Fallback geht über git und nicht über den Skriptpfad: liegt eine
# Kopie woanders, zeigte `dirname/../..` sonst auf "/" und das Tor prüfte stillschweigend nichts.
root="${CLAUDE_PROJECT_DIR:-$(git -C "$(dirname "${BASH_SOURCE[0]}")" rev-parse --show-toplevel 2>/dev/null)}"
[ -n "$root" ] && [ -f "$root/CLAUDE.md" ] || exit 0
cd "$root" || exit 0

now=""
over=""

# $1=Pfad  $2=Budget in Bytes  $3=Anzeigename
check() {
  [ -f "$1" ] || return 0
  local size sections
  size=$(wc -c < "$1" 2>/dev/null | tr -d ' ')
  [ -n "$size" ] || return 0
  now="${now}${3}=${size};"
  [ "$size" -le "$2" ] && return 0

  over="${over}$(printf '  %-30s %6d B   Budget %d B   (%+d)' "$3" "$size" "$2" "$((size - $2))")"$'\n'

  # Die drei größten Abschnitte nennen – ohne sie ist die Warnung nicht handlungsfähig.
  sections=$(awk '
    /^#{2,3} /{ if (n != "") printf "%d\t%s\n", b, n; n=$0; b=0; next }
    { b += length($0) + 1 }
    END { if (n != "") printf "%d\t%s\n", b, n }
  ' "$1" 2>/dev/null | sort -rn | head -3 | awk -F'\t' '{ printf "      %6d B  %s\n", $1, $2 }')
  [ -n "$sections" ] && over="${over}${sections}"$'\n'
  return 0
}

# Immer geladen: die Wurzel. Bereichsweise geladen: die verschachtelten – eigenes, größeres Budget.
check "CLAUDE.md"                     19000 "CLAUDE.md"
check "backend/Pugling.Api/CLAUDE.md" 13000 "backend/Pugling.Api/CLAUDE.md"
check "frontend/CLAUDE.md"             9000 "frontend/CLAUDE.md"

# Das Memory liegt außerhalb des Repos, ist aber genauso resident.
check "$HOME/.claude/projects/C--Users-TjarkOnnen-source-repos-priv-pugling/memory/MEMORY.md" 8500 "MEMORY.md"

state="$root/.claude/.context-budget-state"
prev=$( [ -f "$state" ] && cat "$state" 2>/dev/null || true )
printf '%s' "$now" > "$state" 2>/dev/null || true

# Alles im Budget → still bleiben. Über Budget, aber unverändert seit der letzten Meldung → nicht in
# jeder Antwort nerven.
[ -z "$over" ] && exit 0
[ "$now" = "$prev" ] && exit 0

msg="⚠ Startkontext über Budget – diese Dateien werden bei *jeder* Sitzung mitgeladen:
${over}
Leitfrage je Zeile: ändert sie eine Entscheidung bei einer *beliebigen* Änderung?
  - Nein, nur in einem Bereich  -> in die verschachtelte CLAUDE.md dieses Bereichs.
  - Nein, sie begründet/erzählt -> nach docs/ (und von dort verlinken).
  - Zahlen und 'seit <Datum> gilt...'-Historie verrotten am schnellsten - zuerst pruefen.
Kein Blocker; PUGLING_SKIP_CONTEXT_BUDGET=1 schaltet die Meldung ab."

# JSON mit systemMessage: die Warnung erreicht den Nutzer, ohne das Beenden zu blocken.
if ! MSG="$msg" python -c '
import json, os
print(json.dumps({"systemMessage": os.environ["MSG"], "suppressOutput": True}))
' 2>/dev/null; then
  printf '%s\n' "$msg" >&2
fi
exit 0
