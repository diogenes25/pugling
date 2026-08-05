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
# Zweite Gruppe: der **Verfahrenstext**. Nicht resident, aber er steuert jede Sitzung, in der er gezogen
# wird — und er wächst nach demselben Muster, gegen das dieses Tor gebaut wurde. Am 2026-08-05 hat sich
# `pm-loop/SKILL.md` in EINER Sitzung verdoppelt (15.865 → 31.980 B) und der Regeltext des Backlog-README
# um 73 % zugelegt (18.751 → 32.520 B). Grund: die Retrospektive muss je Sprint einen Mechanismus landen,
# und nichts entfernt je einen. Diese Grenzen frieren den Stand vom 2026-08-05 ein — sie segnen ihn nicht
# ab. Wer etwas hinzufügt, sieht die Meldung und beantwortet die Frage, die sonst niemand stellt: welche
# bestehende Regel darf dafür gehen?
verfahren_over=""

# $1=Pfad  $2=Budget in Bytes  $3=Anzeigename  $4="prosa" = nur bis zum generierten Index messen
check() {
  [ -f "$1" ] || return 0
  local size sections
  if [ "${4:-}" = "prosa" ]; then
    size=$(sed '/backlog-index:start/,$d' "$1" 2>/dev/null | wc -c | tr -d ' ')
  else
    size=$(wc -c < "$1" 2>/dev/null | tr -d ' ')
  fi
  [ -n "$size" ] || return 0
  now="${now}${3}=${size};"
  [ "$size" -le "$2" ] && return 0

  local block
  block="$(printf '  %-30s %6d B   Budget %d B   (%+d)' "$3" "$size" "$2" "$((size - $2))")"$'\n'

  # Die drei größten Abschnitte nennen – ohne sie ist die Warnung nicht handlungsfähig.
  sections=$(awk '
    /^#{2,3} /{ if (n != "") printf "%d\t%s\n", b, n; n=$0; b=0; next }
    { b += length($0) + 1 }
    END { if (n != "") printf "%d\t%s\n", b, n }
  ' "$1" 2>/dev/null | sort -rn | head -3 | awk -F'\t' '{ printf "      %6d B  %s\n", $1, $2 }')
  [ -n "$sections" ] && block="${block}${sections}"$'\n'

  # Zwei Gruppen, zwei Meldungen: „resident" und „Verfahrenstext" verlangen verschiedene Antworten.
  if [ "${gruppe:-start}" = "verfahren" ]; then
    verfahren_over="${verfahren_over}${block}"
  else
    over="${over}${block}"
  fi
  return 0
}

# Immer geladen: die Wurzel. Bereichsweise geladen: die verschachtelten – eigenes, größeres Budget.
check "CLAUDE.md"                     19000 "CLAUDE.md"
check "backend/Pugling.Api/CLAUDE.md" 13000 "backend/Pugling.Api/CLAUDE.md"
check "frontend/CLAUDE.md"             9000 "frontend/CLAUDE.md"

# Das Memory liegt außerhalb des Repos, ist aber genauso resident.
check "$HOME/.claude/projects/C--Users-TjarkOnnen-source-repos-priv-pugling/memory/MEMORY.md" 8500 "MEMORY.md"

# Der Verfahrenstext. Grenzen = Stand 2026-08-05, damit der nächste Zuwachs sichtbar wird. Beim
# Backlog-README zählt nur der Regeltext (`prosa`): der generierte Index wächst mit der Zahl der Stories,
# und das ist legitimes Wachstum, kein Regel-Zuwachs.
gruppe=verfahren
check ".claude/skills/pm-loop/SKILL.md"  32000 "pm-loop/SKILL.md"
check ".claude/skills/backlog/SKILL.md"  14500 "backlog/SKILL.md"
check "docs/backlog/README.md"           32600 "backlog/README.md (Regeltext)" prosa
gruppe=start

state="$root/.claude/.context-budget-state"
prev=$( [ -f "$state" ] && cat "$state" 2>/dev/null || true )
printf '%s' "$now" > "$state" 2>/dev/null || true

# Alles im Budget → still bleiben. Über Budget, aber unverändert seit der letzten Meldung → nicht in
# jeder Antwort nerven.
[ -z "$over" ] && [ -z "$verfahren_over" ] && exit 0
[ "$now" = "$prev" ] && exit 0

msg=""
[ -n "$over" ] && msg="⚠ Startkontext über Budget – diese Dateien werden bei *jeder* Sitzung mitgeladen:
${over}
Leitfrage je Zeile: ändert sie eine Entscheidung bei einer *beliebigen* Änderung?
  - Nein, nur in einem Bereich  -> in die verschachtelte CLAUDE.md dieses Bereichs.
  - Nein, sie begründet/erzählt -> nach docs/ (und von dort verlinken).
  - Zahlen und 'seit <Datum> gilt...'-Historie verrotten am schnellsten - zuerst pruefen."

# Andere Gruppe, andere Frage: beim Verfahrenstext ist nicht die Verdrängung das Problem, sondern die
# Einbahnstrasse. Die Retro landet je Sprint einen Mechanismus; nichts nimmt je einen zurueck. Darum
# fragt diese Meldung nach dem Ruecknehmen und nicht nach dem Verschieben.
[ -n "$verfahren_over" ] && msg="${msg:+$msg
}⚠ Verfahrenstext gewachsen – diese Dateien steuern jede Sitzung, in der sie gezogen werden:
${verfahren_over}
Das Verfahren hat eine Zufuhr (Retro Step 8: je Sprint ein Mechanismus) und keinen Abfluss. Die Frage
gehoert also hierher, weil sie sonst niemand stellt:
  - Welche bestehende Regel hat seit mehreren Sprints nichts verhindert? Die darf gehen.
  - Ist die neue Regel eine Entscheidung - oder erzaehlt sie? Erzaehlendes nach docs/ und verlinken.
  - Ein Beleg (Datum, Commit, Zahl) darf bleiben; zwei Belege fuer dieselbe Regel sind einer zu viel."

msg="${msg}
Kein Blocker; PUGLING_SKIP_CONTEXT_BUDGET=1 schaltet die Meldung ab."

# JSON mit systemMessage: die Warnung erreicht den Nutzer, ohne das Beenden zu blocken.
if ! MSG="$msg" python -c '
import json, os
print(json.dumps({"systemMessage": os.environ["MSG"], "suppressOutput": True}))
' 2>/dev/null; then
  printf '%s\n' "$msg" >&2
fi
exit 0
