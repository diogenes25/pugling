class_name Records
extends Object
## The best score per map, kept on the device (user://records.cfg). A motivation on the result screen only; nothing
## the server pays for.

const FILE: String = "user://records.cfg"
const SECTION: String = "best"


## The best score on a map so far, 0 without one.
static func best(map: String) -> int:
	var file := ConfigFile.new()
	file.load(FILE)
	return int(file.get_value(SECTION, map, 0))


## Stores a score when it beats the map's best; returns whether it did.
static func submit(map: String, score: int) -> bool:
	var file := ConfigFile.new()
	file.load(FILE)
	if score <= int(file.get_value(SECTION, map, 0)):
		return false
	file.set_value(SECTION, map, score)
	file.save(FILE)
	return true
