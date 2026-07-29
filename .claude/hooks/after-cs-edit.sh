#!/usr/bin/env bash
# PostToolUse-Hook: Nach Edit/Write/MultiEdit einer C#-Datei die Datei formatieren
# und bauen. Build-Fehler werden per exit 2 an Claude zurückgemeldet.
# Nicht-C#-Dateien werden ignoriert (exit 0), damit der Hook nur dort greift, wo er soll.
#
# Gebaut wird **nur das Projekt, zu dem die Datei gehört** – nicht die Solution: der Hook läuft nach
# *jedem* .cs-Edit und blockiert dabei, ein Solution-Build kostet hier also bei jedem Tastendruck.
# Referenzierte Projekte baut MSBuild transitiv mit (Pugling.Api zieht Pugling.Contracts nach), also
# fällt ein gebrochener Vertrag weiterhin auf. Was der Projekt-Build *nicht* sieht: ein Umbau in
# Contracts oder Client, der einen **abhängigen** Nachbarn bricht (Api.Tests/Agent.Creator) – dafür
# bleibt `dotnet build Pugling.sln` bzw. `dotnet test` vor dem Commit die Instanz.
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

# Das besitzende Projekt aus dem Pfad ableiten. Backslashes vorher normalisieren (Windows-Pfade), und
# `Pugling.Api.Tests` **vor** `Pugling.Api` prüfen – sonst gewinnt der Präfix und der Test-Build fehlt.
# Der Backslash im Suchmuster **muss quotiert** sein (`'\'`): als `\\` geschrieben liest bash das Muster
# als `/` und löscht die Schrägstriche, statt die Backslashes zu ersetzen – genau umgekehrt.
norm=${file//'\'//}
case "$norm" in
  */Pugling.Api.Tests/*)     proj="backend/Pugling.Api.Tests/Pugling.Api.Tests.csproj" ;;
  */Pugling.Agent.Creator/*) proj="backend/Pugling.Agent.Creator/Pugling.Agent.Creator.csproj" ;;
  */Pugling.Contracts/*)     proj="backend/Pugling.Contracts/Pugling.Contracts.csproj" ;;
  */Pugling.Client/*)        proj="backend/Pugling.Client/Pugling.Client.csproj" ;;
  *)                         proj="backend/Pugling.Api/Pugling.Api.csproj" ;;
esac

# `dotnet format --include` braucht einen **projekt-relativen** Pfad. Mit dem absoluten Pfad aus
# `tool_input.file_path` trifft das Muster nichts ("Formatted 0 of 237 files") – der Formatier-Schritt lief
# dann ins Leere, ohne zu meckern. Darum das Repo-Wurzel-Präfix abschneiden; Laufwerksbuchstabe
# case-insensitiv, weil Windows `C:` und `c:` beides liefert.
rootw=$(pwd -W 2>/dev/null || pwd); rootw=${rootw%/}
shopt -s nocasematch
if [[ $norm == "$rootw"/* ]]; then rel=${norm:${#rootw}+1}; else rel=$norm; fi
shopt -u nocasematch

# Formatieren (nur die geänderte Datei) – Formatierungsfehler sollen den Flow nicht blockieren.
# **Nur `whitespace`** (Einrückung/Umbrüche): der volle `dotnet format` lädt zusätzlich die Analyzer und
# kostet damit ~1,6 s mehr – bei jedem Edit. Der `style`-Anteil brächte hier ohnehin fast nichts, weil es
# **keine `.editorconfig`** gibt und `EnforceCodeStyleInBuild` nicht gesetzt ist: es gäbe nur .NET-Vorgaben
# zu erzwingen, die niemand einfordert. Vor dem Commit einmal `dotnet format Pugling.sln` deckt den Rest ab.
dotnet format whitespace "$proj" --include "$rel" --verbosity quiet >/dev/null 2>&1 || true

# Bauen; bei Fehler die relevanten Zeilen an Claude zurückgeben (exit 2 blockt + zeigt stderr).
#
# **`-c Release` ist Absicht** (gleiche Begründung wie in test-gate.sh): läuft parallel ein Dev-Server
# (`dotnet run` gegen localhost:5200 – laut CLAUDE.md der Normalfall beim Prüfen), hält er
# `bin/Debug/…/Pugling.Contracts.dll` gesperrt, und jeder Debug-Build scheitert mit MSB3021/MSB3027.
# Der Hook meldete dann bei *jedem* .cs-Edit einen Fehler, der keiner ist – die Rückkopplung wurde
# unbrauchbar, genau solange der Server lief. Release schreibt nach `bin/Release` und ist unabhängig.
if ! out=$(dotnet build "$proj" -c Release -clp:NoSummary -v q 2>&1); then
  {
    echo "❌ dotnet build ($proj) fehlgeschlagen nach Änderung an $file:"
    printf '%s\n' "$out" | grep -iE "error|warning" | head -30
  } >&2
  exit 2
fi
exit 0
