extends Node
## The game log. project.godot turns on file logging, so the lines survive in user://logs on the phone - its logcat
## buffer is overwritten within minutes.


## Writes one line with the local time. Keep the "TURM" prefix: the run reports are read by it.
func line(text: String) -> void:
	print("TURM %s %s" % [Time.get_time_string_from_system(), text])
