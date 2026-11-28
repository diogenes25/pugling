extends SceneTree
## The rule tests' runner: godot --headless --path . --script res://tests/run_tests.gd
## Runs every test_* method of every suite and exits with 0 when all pass, 1 otherwise. A suite that does not load
## counts as a failure, and so does a case during which the engine logs an error: a script error aborts the case
## before its checks, which would otherwise look like a case without complaints.

const SUITES: Array[String] = [
	"res://tests/test_tower_ammo.gd",
	"res://tests/test_run_tally.gd",
	"res://tests/test_balance.gd",
	"res://tests/test_wave_plan.gd",
	"res://tests/test_tower_level.gd",
	"res://tests/test_run_score.gd",
	"res://tests/test_token_pool.gd",
	"res://tests/test_cooldown.gd",
	"res://tests/test_trophy_path.gd",
	"res://tests/test_export_pitfalls.gd",
]


## Counts the errors the engine logs (script errors included).
class ErrorCount:
	extends Logger

	## Errors logged so far.
	var errors: int = 0

	func _log_error(_function: String, _file: String, _line: int, _code: String, _rationale: String, _editor_notify: bool,
			_error_type: int, _script_backtraces: Array[ScriptBacktrace]) -> void:
		errors += 1

	func _log_message(_message: String, _error: bool) -> void:
		pass


func _initialize() -> void:
	var cases := 0
	var failed := 0
	var log := ErrorCount.new()
	OS.add_logger(log)
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
			var errors_before := log.errors
			suite.call(case)
			if log.errors > errors_before:
				suite.failures.append("the engine logged an error")
			if suite.failures.is_empty():
				print("ok    %s" % case)
			else:
				failed += 1
				print("FAIL  %s: %s" % [case, "; ".join(suite.failures)])
	print("%d tests, %d failed" % [cases, failed])
	quit(1 if failed > 0 or cases == 0 else 0)
