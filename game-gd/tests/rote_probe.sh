#!/bin/bash
# The red probe for every rule test: falsifies one rule at a time, runs the tests and demands that the named case
# turns red - a case that stays green proves nothing. Every file is restored afterwards and the suite must be green.
# Usage (Git Bash, from game/):  GODOT=/path/to/godot_console.exe tests/rote_probe.sh
set -u
cd "$(dirname "$0")/.."
: "${GODOT:?set GODOT to the Godot console binary}"

# case | file | sed expression that falsifies the rule. The falsified file must still compile: with warnings as
# errors an unused variable stops the whole suite from loading, which is red in the wrong place.
PROBES='
test_a_correct_word_adds_eight_shots_and_a_wrong_one_costs_three|core/tower_ammo.gd|s/count += Balance.REFILL_AMMO/count += 7/
test_a_penalty_never_drives_the_ammo_below_zero|core/tower_ammo.gd|s/count = maxi(0, count - Balance.WRONG_PENALTY)/count = count - Balance.WRONG_PENALTY/
test_more_than_thirty_shots_make_a_tower_strong_and_the_turn_is_reported_once|core/tower_ammo.gd|s/return not was_strong and is_strong()/return was_strong or is_strong()/
test_five_refills_keep_a_tower_strong_even_when_its_ammo_runs_low|core/tower_ammo.gd|s/ or refills >= Balance.MASTER_REFILLS//
test_a_shot_reports_the_strength_it_was_fired_with|core/tower_ammo.gd|s/^\treturn strong$/\treturn strong and is_strong()/
test_three_shots_or_fewer_count_as_low|core/tower_ammo.gd|s/count <= Balance.WRONG_PENALTY/count < Balance.WRONG_PENALTY/
test_only_correct_words_count_towards_the_goal|core/run_tally.gd|s/if was_correct:/if was_correct or true:/
test_the_castle_falls_with_the_tenth_skeleton|core/run_tally.gd|s/return lives <= 0/return lives <= 1/
test_the_summary_keeps_the_words_the_run_reports_are_read_by|core/run_tally.gd|s/Heldentreffer %d/Treffer des Helden %d/
test_the_first_skeleton_comes_after_two_seconds|data/balance.gd|s/FIRST_SPAWN_DELAY: float = 2.0/FIRST_SPAWN_DELAY: float = 1.0/
test_every_fourth_skeleton_is_a_slower_warrior|data/balance.gd|s/WARRIOR_EVERY: int = 4/WARRIOR_EVERY: int = 3/
test_several_paths_take_turns|core/spawn_schedule.gd|s/spawn.path = spawned % _paths/spawn.path = 0/
test_the_interval_shrinks_with_play_time_down_to_its_floor|data/balance.gd|s/SPAWN_INTERVAL_FLOOR: float = 0.55/SPAWN_INTERVAL_FLOOR: float = 0.5/
test_skeletons_speed_up_by_at_most_point_eight|data/balance.gd|s/MAX_SPEED_RAMP: float = 0.8/MAX_SPEED_RAMP: float = 1.0/
test_every_token_drops_once_until_it_comes_back|core/token_pool.gd|s/^\t_out\[id\] = true$/\tpass/
test_a_consumed_token_never_drops_again|core/token_pool.gd|/^func consume/,$ s/^\t_out.erase(id)$/\t_out[id] = true/
test_returning_a_token_twice_does_not_double_it|core/token_pool.gd|s/if _out.erase(id):/if _out.erase(id) or true:/
test_a_started_cooldown_holds_until_its_time_has_passed|core/cooldown.gd|s/_left = seconds/_left = seconds * 0.0/
'

run_tests() {
	"$GODOT" --headless --path . --script res://tests/run_tests.gd 2>&1
}

bad=0
count=0
while IFS='|' read -r case file expr; do
	[ -z "$case" ] && continue
	count=$((count + 1))
	cp "$file" "$file.probe-backup"
	sed -i "$expr" "$file"
	if cmp -s "$file" "$file.probe-backup"; then
		echo "BROKEN  $case: the probe changed nothing in $file"
		bad=$((bad + 1))
	else
		out=$(run_tests)
		red=$(echo "$out" | grep -c '^FAIL')
		if echo "$out" | grep -q "^FAIL  $case:"; then
			echo "red     $case ($red red in total)"
		else
			echo "GREEN   $case stays green against: $expr"
			bad=$((bad + 1))
		fi
	fi
	# cp gives the file a new time stamp, so nothing can mistake the restored rule for the probe.
	cp "$file.probe-backup" "$file"
	rm "$file.probe-backup"
done <<< "$PROBES"

final=$(run_tests | tail -1)
echo "$count probes, $bad without a red case; restored suite: $final"
[ "$bad" -eq 0 ] && [[ "$final" == *", 0 failed" ]]
