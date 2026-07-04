#!/usr/bin/env bash
# PostToolUse-Hook: Nach Edit/Write/MultiEdit einer C#-Datei die Datei formatieren
# und die Solution bauen. Build-Fehler werden per exit 2 an Claude zurückgemeldet.
# Nicht-C#-Dateien werden ignoriert (exit 0), damit der Hook nur dort greift, wo er soll.
set -uo pipefail

input=$(cat)
file=$(printf '%s' "$input" | python -c "import sys,json; print(json.load(sys.stdin).get('tool_input',{}).get('file_path',''))" 2>/dev/null || true)

# Nur auf C#-Quelldateien reagieren.
case "$file" in
  *.cs) ;;
  *) exit 0 ;;
esac

root="${CLAUDE_PROJECT_DIR:-$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)}"
cd "$root" || exit 0
sln="Pugling.sln"

# Formatieren (nur die geänderte Datei) – Formatierungsfehler sollen den Flow nicht blockieren.
dotnet format "$sln" --include "$file" --verbosity quiet >/dev/null 2>&1 || true

# Bauen; bei Fehler die relevanten Zeilen an Claude zurückgeben (exit 2 blockt + zeigt stderr).
if ! out=$(dotnet build "$sln" -clp:NoSummary -v q 2>&1); then
  {
    echo "❌ dotnet build fehlgeschlagen nach Änderung an $file:"
    printf '%s\n' "$out" | grep -iE "error|warning" | head -30
  } >&2
  exit 2
fi
exit 0
