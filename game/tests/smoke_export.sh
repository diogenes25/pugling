#!/bin/bash
# Runs the smoke runs against the EXPORTED game instead of the project folder. An export converts resources and
# compiles scripts, and that can break what the editor runs fine (2026-11-27: every map lost its towers). So before
# an APK goes onto a phone: export the "Testpaket" preset (the game plus tests/) as a pack and play it headless.
# Usage (Git Bash, from game/):  GODOT=/path/to/godot_console.exe tests/smoke_export.sh
# Exit code 0 only when both runs pass.
set -u
cd "$(dirname "$0")/.."
: "${GODOT:?set GODOT to the Godot console binary}"

# The export caches converted resources by the resource file alone: after a change to a resource's script it would
# pack the stale conversion again (seen with the tower fix), so the cache goes first.
rm -rf .godot/exported
mkdir -p build/testpaket
pack="$(pwd -W 2>/dev/null || pwd)/build/testpaket/game.pck"
rm -f "$pack"
"$GODOT" --headless --path . --export-pack Testpaket "$pack" > build/testpaket/export.log 2>&1
if [ ! -f "$pack" ]; then
	echo "export failed, see build/testpaket/export.log"
	exit 1
fi

# From its own folder, so nothing can fall back to the project's files.
cd build/testpaket
failed=0
for scene in smoke_screens smoke_level; do
	"$GODOT" --headless --main-pack game.pck "res://tests/$scene.tscn" > "$scene.log" 2>&1
	code=$?
	echo "$scene: exit $code, $(grep -c '^TURM' "$scene.log") log lines, $(grep -c 'ABWEICHUNG\|SCRIPT ERROR' "$scene.log") deviations or script errors"
	[ "$code" -ne 0 ] && failed=1
	# The exit code alone is not proof: a run that closed the app early ends with 0 too. Only the closing line counts.
	grep -q 'fertig, 0 Abweichungen' "$scene.log" || { echo "$scene: no closing line with 0 deviations"; failed=1; }
	grep -q 'SCRIPT ERROR' "$scene.log" && failed=1
done
exit $failed
