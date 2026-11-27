class_name TestSuite
extends RefCounted
## Base of the rule tests: every method named test_* is one case; a case fails when one of its checks does.
## Run all of them with: godot --headless --path . --script res://tests/run_tests.gd

## What went wrong in the case that is running; the runner reads and clears it.
var failures: PackedStringArray = PackedStringArray()


## Fails the case unless [param condition] holds.
func check(condition: bool, what: String = "condition") -> void:
	if not condition:
		failures.append("expected %s" % what)


## Fails the case unless both values are equal.
func equal(expected: Variant, actual: Variant) -> void:
	if typeof(expected) != typeof(actual) or expected != actual:
		failures.append("expected %s, got %s" % [var_to_str(expected), var_to_str(actual)])


## Fails the case unless both numbers agree to three decimals.
func near(expected: float, actual: float) -> void:
	if absf(expected - actual) >= 0.0005:
		failures.append("expected about %s, got %s" % [expected, actual])
