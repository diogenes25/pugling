#!/usr/bin/env bash
# Stop-Hook: Das Test-Tor im Arbeitsfluss (docs/codequalitaet-gates-plan.md, A4).
#
# Warum überhaupt: der PostToolUse-Hook (after-cs-edit.sh) baut nur das besitzende Projekt. Damit ist die
# engste Rückkopplung für generierten Code "kompiliert" – eine Stufe zu schwach für ein Projekt, dessen
# Regeln Laufzeitregeln sind (Ownership, Rollen, Idempotenz, Wallet-Serialisierung). Die ganze Suite kostet
# warm ~63 s; das ist am Ende einer Antwort tragbar, "nur bauen" ist es fachlich nicht.
#
# Drei Sparmaßnahmen, damit das Tor nicht zur Bremse wird:
#   1. Keine .cs-Änderung gegenüber HEAD → gar nichts tun. HEAD hat das CI-Tor schon passiert.
#   2. Derselbe .cs-Stand wie beim letzten grünen Lauf → nicht erneut testen (Fingerprint-Datei).
#   3. PUGLING_SKIP_TEST_GATE=1 schaltet ab (langer Umbau, in dem rot der erwartete Zwischenstand ist).
#
# Rot → exit 2: blockt das Beenden und gibt die gefallenen Tests an Claude zurück.
set -uo pipefail

input=$(cat)

# Schutz gegen Endlosschleife: läuft der Stop-Hook bereits (Claude arbeitet also gerade an unserem
# Befund weiter), nicht noch einmal blocken.
active=$(printf '%s' "$input" | python -c "import sys,json; print(json.load(sys.stdin).get('stop_hook_active', False))" 2>/dev/null || echo False)
[ "$active" = "True" ] && exit 0

[ "${PUGLING_SKIP_TEST_GATE:-}" = "1" ] && exit 0

root="${CLAUDE_PROJECT_DIR:-$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)}"
cd "$root" || exit 0

# Welche .cs-Dateien weichen von HEAD ab (geändert oder unversioniert)? Nichts davon → nichts zu prüfen.
changed=$( { git diff --name-only HEAD -- '*.cs'; git ls-files --others --exclude-standard -- '*.cs'; } 2>/dev/null | sort -u)
[ -z "$changed" ] && exit 0

# Fingerprint über den *Inhalt* der Abweichung, nicht über Dateinamen oder Zeitstempel: derselbe Code
# soll nicht zweimal getestet werden, eine echte Änderung aber immer.
fingerprint=$(
  {
    git rev-parse HEAD 2>/dev/null
    git diff HEAD -- '*.cs' 2>/dev/null
    printf '%s\n' "$changed"
    git ls-files --others --exclude-standard -- '*.cs' 2>/dev/null | while IFS= read -r f; do cat -- "$f" 2>/dev/null; done
  } | sha1sum | cut -d' ' -f1
)

state="$root/.claude/.test-gate-state"
[ -f "$state" ] && [ "$(cat "$state" 2>/dev/null)" = "$fingerprint" ] && exit 0

# **`-c Release` ist Absicht, nicht Kosmetik.** Läuft parallel ein Dev-Server (`dotnet run` gegen
# localhost:5200 – laut CLAUDE.md der Normalfall beim Prüfen), hält er `bin/Debug/.../Pugling.Contracts.dll`
# gesperrt und ein Debug-Build der Solution scheitert mit MSB3021, bevor ein einziger Test läuft. Release
# schreibt nach `bin/Release` und ist damit unabhängig – und deckt sich zusätzlich mit dem CI-Lauf.
if out=$(dotnet test Pugling.sln -c Release --nologo -clp:NoSummary 2>&1); then
  printf '%s' "$fingerprint" > "$state"
  exit 0
fi

# Rot. Die gefallenen Tests nach oben, dazu Build-Fehler (dann lief die Suite gar nicht erst an).
{
  echo "❌ Test-Tor rot: 'dotnet test Pugling.sln' schlägt fehl bei geänderten .cs-Dateien."
  printf '%s\n' "$out" | grep -E "^\s*(Failed|Fehler)\s" | head -20
  printf '%s\n' "$out" | grep -iE "error [A-Z]+[0-9]+" | head -10
  printf '%s\n' "$out" | grep -iE "^(Failed!|Fehlgeschlagen!)" | head -3

  # **Der Abdeckungs-Wächter braucht eine Extrawurst.** Er urteilt im Aufräumen eines Assembly-Fixtures
  # (nur dort steht fest, dass alle Tests durch sind). Eine Ausnahme von dort lässt den Lauf scheitern
  # (Exit 1, und die .trx trägt die Meldung), aber der Konsolen-Zusammenzug meldet trotzdem "Passed!" und
  # zeigt bloß `Xunit.Sdk.TestPipelineException` – ohne Grund. Selbst Console.WriteLine aus dem Fixture
  # kommt nicht durch. Ohne die folgenden Zeilen wäre das ein rotes Tor ohne Befund.
  if printf '%s\n' "$out" | grep -q "Cleanup Failure"; then
    echo "— Assembly-Fixture (Abdeckungs-Wächter) hat abgebrochen; die Konsole verschluckt die Meldung."
    if [ -f "$root/TestResults/endpoint-coverage.txt" ]; then
      echo "— Nicht abgedeckte Actions (TestResults/endpoint-coverage.txt):"
      head -25 "$root/TestResults/endpoint-coverage.txt"
    fi
  fi

  echo "→ Ursache beheben, nicht das Tor umgehen. Beabsichtigt roter Zwischenstand: PUGLING_SKIP_TEST_GATE=1."
} >&2
exit 2
