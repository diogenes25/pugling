#!/usr/bin/env bash
# Status-Line für Pugling: Projekt · Git-Branch (+Dirty-Flag) · Modell.
# Bekommt Session-JSON auf stdin (model.display_name, workspace.current_dir). Muss schnell sein.
input=$(cat)
model=$(printf '%s' "$input" | python -c "import sys,json;print(json.load(sys.stdin).get('model',{}).get('display_name','?'))" 2>/dev/null || echo "?")
dir=$(printf '%s' "$input" | python -c "import sys,json;d=json.load(sys.stdin);print(d.get('workspace',{}).get('current_dir') or d.get('cwd',''))" 2>/dev/null)
[ -n "$dir" ] && cd "$dir" 2>/dev/null

branch=$(git rev-parse --abbrev-ref HEAD 2>/dev/null)
dirty=""
[ -n "$branch" ] && [ -n "$(git status --porcelain 2>/dev/null)" ] && dirty="*"

DIM=$'\e[2m'; CYAN=$'\e[36m'; YEL=$'\e[33m'; RST=$'\e[0m'
line="🐶 ${CYAN}pugling${RST}"
[ -n "$branch" ] && line="$line ${DIM}·${RST} ⎇ ${YEL}${branch}${dirty}${RST}"
line="$line ${DIM}· ${model}${RST}"
printf '%s' "$line"
