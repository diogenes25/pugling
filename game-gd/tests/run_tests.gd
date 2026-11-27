extends SceneTree
## The rule tests' runner: godot --headless --path . --script res://tests/run_tests.gd
## Runs every test_* method of every suite and exits with 0 when all pass, 1 otherwise. A suite that does not load
## counts as a failure, so a broken rule file can never look green.

const SUITES: Array[String] = [
	"res://tests/test_tower_ammo.gd",
	"res://tests/test_run_tally.gd",
	"res://tests/test_spawn_schedule.gd",
	"res://tests/test_token_pool.gd",
	"res://tests/test_cooldown.gd",
	"res://tests/test_export_pitfalls.gd",
]


func _initialize() -> void:
	var cases := 0
	var failed := 0
	for path: String in SUITES:
		var script := load(path) as GDScript
		if script == null or not script.can_instantiate():
			print("FAIL  %s does not load" % path)
			failed += 1
			continue
		var suite: TestSuite = script.new()
		for method: Dictionary in script.get_script_method_list():
			var case: String = method["name"]
			if not case.begins_with("test_"):
				continue
			cases += 1
			suite.failures.clear()
			suite.call(case)
			if suite.failures.is_empty():
				print("ok    %s" % case)
			else:
				failed += 1
				print("FAIL  %s: %s" % [case, "; ".join(suite.failures)])
	print("%d tests, %d failed" % [cases, failed])
	quit(1 if failed > 0 or cases == 0 else 0)
