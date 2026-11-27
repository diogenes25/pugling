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

# A running mutant probe (.claude/scripts/mutant-probe.py) holds mutated sources: a run now would report a mutant as
# a defect of the real code and stamp no fingerprint. Skip loudly; the next stop after the probe measures. A lock older
# than four hours is a leftover of a killed probe and does not count (same limit as the script).
lock=.claude/.mutant-probe.lock
if [ -f "$lock" ] && [ -n "$(find "$lock" -mmin -240 2>/dev/null)" ]; then
  echo "Test-Tor übersprungen: eine Mutanten-Probe läuft ($lock) – gemessen wird nach ihrem Ende." >&2
  exit 0
fi

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

# A test run already in progress (a reviewer, a background suite, a second session) holds the Release output: a run now
# fails with MSB3027 before any test starts, which reads as a red gate without a defect (Sprint 195, three times). Skip
# loudly and stamp no fingerprint, so the next stop measures this state once the other run is done. Decided by the user
# on 2026-11-27.
if { command -v tasklist >/dev/null 2>&1 && tasklist //NH //FI "IMAGENAME eq Pugling.Api.Tests.exe" 2>/dev/null | grep -qi "Pugling.Api.Tests"; } \
  || { command -v pgrep >/dev/null 2>&1 && pgrep -f "Pugling.Api.Tests" >/dev/null 2>&1; }; then
  echo "Test gate skipped: a Pugling.Api.Tests run is already in progress – this state is measured on the next stop after it ends." >&2
  exit 0
fi

# Format of the changed files first (Sprint 194): after-cs-edit.sh only runs on the Edit/Write tools, so a .cs file changed
# through the shell (a script, sed) reaches CI with a BOM or an unformatted initializer and nothing local says so. Whitespace
# mode needs no build (~10 s); measured: a BOM is red, CRLF line ends are not.
# shellcheck disable=SC2086 # the list is word-split on purpose; repo paths carry no spaces
if ! fmt=$(dotnet format whitespace --folder --verify-no-changes --include $changed 2>&1); then
  {
    echo "❌ Test gate red: changed .cs files are not formatted (dotnet format whitespace)."
    printf '%s\n' "$fmt" | grep -E "error [A-Z]+:" | head -10
    echo "Fix: dotnet format whitespace --folder --include <file> (CI runs dotnet format --verify-no-changes)."
  } >&2
  exit 2
fi

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
