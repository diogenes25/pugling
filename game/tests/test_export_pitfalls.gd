extends TestSuite
## Guards against what only breaks in an exported build. Found on the phone on 2026-11-27: the maps had no towers,
## while every run in the editor was fine.

const SOURCE_DIRS: Array[String] = ["autoload", "combat", "core", "data", "heroes", "level", "screens", "ui", "words", "world"]


## The export turns text resources into binary ones. An exported packed array whose default is written as the array
## literal [] comes out empty there - the .tres values are silently dropped. Write the default as the packed type's
## constructor (or leave it out).
func test_no_exported_packed_array_defaults_to_an_array_literal() -> void:
	var pattern := RegEx.create_from_string("@export var \\w+: Packed\\w+Array = \\[")
	var scanned := 0
	for dir: String in SOURCE_DIRS:
		for file: String in DirAccess.get_files_at("res://" + dir):
			if not file.ends_with(".gd"):
				continue
			scanned += 1
			var lines := FileAccess.get_file_as_string("res://%s/%s" % [dir, file]).split("\n")
			for number: int in lines.size():
				if pattern.search(lines[number]) != null:
					failures.append("%s/%s:%d defaults an exported packed array to []" % [dir, file, number + 1])
	check(scanned > 30, "the scan to find the game's scripts (found %d)" % scanned)
