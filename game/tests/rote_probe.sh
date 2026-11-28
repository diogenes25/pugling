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
test_a_tower_starts_with_sixteen_shots_and_a_wrong_word_costs_three|data/balance.gd|s/START_AMMO: int = 16/START_AMMO: int = 15/
test_a_correct_word_adds_eight_shots_plus_two_per_value_point|core/tower_ammo.gd|s/Balance.REFILL_PER_VALUE \* value/Balance.REFILL_PER_VALUE/
test_a_run_fed_with_every_word_keeps_its_towers_stocked_for_twenty_waves|data/balance.gd|s/REFILL_AMMO: int = 8/REFILL_AMMO: int = 2/
test_a_run_without_words_clears_the_first_wave_and_runs_dry_in_the_second|data/balance.gd|s/START_AMMO: int = 16/START_AMMO: int = 30/
test_the_words_of_the_first_wave_outweigh_the_shots_of_the_second|data/balance.gd|s/REFILL_PER_VALUE: int = 2/REFILL_PER_VALUE: int = 0/
test_a_penalty_never_drives_the_ammo_below_zero|core/tower_ammo.gd|s/count = maxi(0, count - Balance.WRONG_PENALTY)/count = count - Balance.WRONG_PENALTY/
test_more_than_thirty_shots_make_a_tower_strong_and_the_turn_is_reported_once|core/tower_ammo.gd|s/return not was_strong and is_strong()/return was_strong or is_strong()/
test_five_refills_keep_a_tower_strong_even_when_its_ammo_runs_low|core/tower_ammo.gd|s/ or refills >= Balance.MASTER_REFILLS//
test_a_shot_reports_the_strength_it_was_fired_with|core/tower_ammo.gd|s/^\treturn strong$/\treturn strong and is_strong()/
test_three_shots_or_fewer_count_as_low|core/tower_ammo.gd|s/count <= Balance.WRONG_PENALTY/count < Balance.WRONG_PENALTY/
test_only_correct_words_count_towards_the_goal|core/run_tally.gd|s/if was_correct:/if was_correct or true:/
test_the_castle_falls_with_the_tenth_skeleton|core/run_tally.gd|s/return lives <= 0/return lives <= 1/
test_the_summary_keeps_the_words_the_run_reports_are_read_by|core/run_tally.gd|s/Heldentreffer %d/Treffer des Helden %d/
test_a_repair_gives_lives_back_but_never_more_than_ten|core/run_tally.gd|s/lives = mini(Balance.CASTLE_LIVES, lives + amount)/lives = lives + amount/
test_the_first_wave_starts_after_two_seconds|data/balance.gd|s/FIRST_SPAWN_DELAY: float = 2.0/FIRST_SPAWN_DELAY: float = 1.0/
test_a_wave_grows_by_two_minions_and_a_carrier_every_second_wave|data/balance.gd|s/WAVE_MINIONS_GROWTH: int = 2/WAVE_MINIONS_GROWTH: int = 3/
test_carriers_march_spread_among_the_minions|core/wave_plan.gd|s/(result.size() + 1) % every == 0/(result.size() + 1) % (every * 100) == 0/
test_every_fifth_wave_ends_with_a_boss|data/balance.gd|s/BOSS_EVERY: int = 5/BOSS_EVERY: int = 4/
test_the_next_wave_waits_until_the_field_is_clear_and_rests_five_seconds|data/balance.gd|s/WAVE_REST: float = 5.0/WAVE_REST: float = 4.0/
test_enemies_follow_closer_from_wave_to_wave_down_to_a_floor|data/balance.gd|s/WAVE_INTERVAL_FLOOR: float = 0.25/WAVE_INTERVAL_FLOOR: float = 0.2/
test_enemies_get_faster_and_tougher_with_the_wave|data/balance.gd|s/MAX_SPEED_RAMP: float = 0.8/MAX_SPEED_RAMP: float = 1.0/
test_several_paths_take_turns|core/wave_plan.gd|s/spawn.path = spawned % _paths/spawn.path = 0/
test_a_tower_gains_its_second_level_at_four_value_points|data/balance.gd|s/LEVEL_STEP: int = 4/LEVEL_STEP: int = 3/
test_every_level_costs_one_step_more_than_the_one_before|core/tower_level.gd|s|current \* (current + 1) / 2|current|
test_a_new_word_lifts_a_tower_faster_than_a_well_learned_one|core/tower_level.gd|s/^\tpower += value$/\tpower += 1 + value * 0/
test_one_big_word_can_lift_a_tower_two_levels_and_reports_it_once|core/tower_level.gd|s/^\twhile power >= needed(level):$/\tif power >= needed(level):/
test_every_level_adds_one_damage|core/tower_level.gd|s/^\treturn level - 1$/\treturn level/
test_the_progress_runs_from_the_last_threshold_to_the_next|core/tower_level.gd|s/var from := 0 if level == 1 else needed(level - 1)/var from := 0 * level/
test_a_kill_pays_what_the_enemy_is_worth|core/run_score.gd|s/_kill_points += kind.points/_kill_points += 10 + kind.points * 0/
test_a_new_word_pays_three_times_a_well_learned_one|core/run_score.gd|s/_word_points += Balance.WORD_POINTS \* value/_word_points += Balance.WORD_POINTS + value * 0/
test_a_tower_level_pays_a_hundred_times_the_level|core/run_score.gd|s/_level_points += Balance.LEVEL_POINTS \* level/_level_points += Balance.LEVEL_POINTS + level * 0/
test_a_cleared_wave_pays_a_hundred_times_its_number_and_reports_the_bonus|core/run_score.gd|s/var bonus := Balance.WAVE_POINTS \* number/var bonus := Balance.WAVE_POINTS + number * 0/
test_the_summary_lines_add_up_to_the_total|core/run_score.gd|s/Line.new("Turm-Aufstiege", tower_levels, _level_points)/Line.new("Turm-Aufstiege", tower_levels, 0 * _level_points)/
test_every_token_drops_once_until_it_comes_back|core/token_pool.gd|s/^\t_out\[id\] = true$/\tpass/
test_returning_a_token_twice_does_not_double_it|core/token_pool.gd|/^func give_back/,/^func consume/ s/if not _out.erase(id):/if not _out.erase(id) and false:/
test_drops_come_from_the_first_eight_of_the_queue|data/balance.gd|s/DROP_WINDOW: int = 8/DROP_WINDOW: int = 40/
test_a_wrongly_given_token_goes_back_to_its_old_place|core/token_pool.gd|s/_rank\[_queue\[at\]\] < _rank\[id\]/false/
test_a_correctly_given_token_queues_up_again_behind_all_others|core/token_pool.gd|/^func consume/,$ s/^\t_rank\[id\] = _next_rank$/\tpass/
test_the_next_step_is_the_first_one_not_reached_yet|core/trophy_path.gd|s/if total < step.trophies:/if total <= step.trophies:/
test_reached_counts_the_steps_at_or_below_the_total|core/trophy_path.gd|s/if total >= step.trophies:/if total > step.trophies:/
test_the_progress_runs_from_the_last_reached_step_to_the_next|core/trophy_path.gd|s/var from := 0 if done == 0 else steps\[done - 1\].trophies/var from := 0 * done/
test_every_hero_level_costs_one_step_more_than_the_one_before|data/balance.gd|s/HERO_LEVEL_STEP: int = 30/HERO_LEVEL_STEP: int = 25/
test_the_level_follows_the_trophies|core/hero_level.gd|s/while trophies >= needed(level + 1):/while trophies > needed(level + 1):/
test_the_way_to_the_next_level_runs_from_zero_to_one|core/hero_level.gd|s/return float(trophies - needed(level)) \//return float(trophies) \//
test_a_higher_level_shoots_a_little_quicker_down_to_a_floor|data/balance.gd|s/HERO_RELOAD_FLOOR: float = 0.5/HERO_RELOAD_FLOOR: float = 0.4/
test_every_fourth_level_adds_one_damage_to_the_heros_shot|data/balance.gd|s/HERO_DAMAGE_EVERY: int = 4/HERO_DAMAGE_EVERY: int = 3/
test_armour_swallows_its_share_of_every_hit_but_never_all_of_it|core/enemy_rules.gd|s/return maxi(1, damage - armor)/return maxi(0, damage - armor)/
test_a_healer_never_mends_beyond_the_hit_points_an_enemy_came_with|core/enemy_rules.gd|s/return mini(max_hp, hp + amount)/return hp + amount/
test_runners_take_every_third_small_place_from_the_third_wave_on|data/balance.gd|s/RUNNER_FROM: int = 3/RUNNER_FROM: int = 2/
test_every_third_carrier_bears_a_shield_from_the_fourth_wave_on_and_still_carries_a_word|data/shield.tres|s/^drops = 1/drops = 0/
test_a_healer_marches_in_the_middle_from_the_sixth_wave_on_up_to_three|core/wave_plan.gd|s|result.insert(result.size() / 2, healer)|result.insert(0, healer)|
test_a_plan_without_special_kinds_sends_none|core/wave_plan.gd|s/if runner != null and number >= Balance.RUNNER_FROM/if number >= Balance.RUNNER_FROM/
test_the_special_enemies_do_not_starve_a_fed_run|data/shield.tres|s/^armor = 1/armor = 9/
test_a_started_cooldown_holds_until_its_time_has_passed|core/cooldown.gd|s/_left = seconds/_left = seconds * 0.0/
test_no_exported_packed_array_defaults_to_an_array_literal|data/tower_kind.gd|s/PackedStringArray = PackedStringArray()/PackedStringArray = []/
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
