class_name UiDriver
extends RefCounted
## Helpers for the smoke runs: they find controls by what a player reads on them and press them the way a tap does.


## The first visible button under [param root] whose text contains [param text], or null.
static func find_button(root: Node, text: String) -> Button:
	for node: Node in root.find_children("*", "Button", true, false):
		var button := node as Button
		# A button that shows only a symbol is found by its tooltip.
		if button.is_visible_in_tree() and (button.text.contains(text) or (button.text == "" and button.tooltip_text.contains(text))):
			return button
	return null


## Presses the button labelled [param text]; returns whether it was there.
static func press(root: Node, text: String) -> bool:
	var button := find_button(root, text)
	if button == null:
		return false
	button.pressed.emit()
	return true


## Every visible label and button text under [param root], top to bottom - what the screen says.
static func texts(root: Node) -> PackedStringArray:
	var found := PackedStringArray()
	for node: Node in root.find_children("*", "Control", true, false):
		var control := node as Control
		if not control.is_visible_in_tree():
			continue
		if control is Label and (control as Label).text != "":
			found.append((control as Label).text)
		elif control is Button and (control as Button).text != "":
			found.append("[%s]" % (control as Button).text)
		elif control is Button and control.tooltip_text != "":
			found.append("[%s]" % control.tooltip_text)
		elif control is Icon and not control.get_parent() is Button:
			# A symbol stands for a word; the run reads the word.
			found.append("(%s)" % (control as Icon).caption)
	return found


## The value of a "--name=value" argument after "--" on the command line, or [param fallback].
static func arg(name: String, fallback: String = "") -> String:
	for raw: String in OS.get_cmdline_user_args():
		if raw.begins_with("--%s=" % name):
			return raw.get_slice("=", 1)
		if raw == "--" + name:
			return "1"
	return fallback


## Saves what the window shows as a PNG, when a screenshot folder was given ("--shots=dir") and there is a window.
static func shot(tree: SceneTree, name: String) -> void:
	var dir := arg("shots")
	if dir == "" or DisplayServer.get_name() == "headless":
		return
	await RenderingServer.frame_post_draw
	tree.root.get_texture().get_image().save_png(dir.path_join(name + ".png"))
