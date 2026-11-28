class_name SkinsScreen
extends Page
## The hero's wardrobe: every skin with its price in gems. A skin the child owns is put on with one tap; another one
## is bought for gems first. The server keeps the prices and what is owned; which skin the hero wears is remembered
## on the device.

## "Zurück" was pressed (or a skin was put on).
signal back

## The server; set before the screen enters the tree.
var server: TowerServer

var _list: VBoxContainer = VBoxContainer.new()
var _gems: Label
var _busy: bool = false


func _ready() -> void:
	set_title("Dein Held")
	_gems = add_text("", 28)
	_list.add_theme_constant_override("separation", 10)
	add_node(_list)
	add_status()
	add_action("Zurück", back.emit)
	@warning_ignore("redundant_await")
	_show(await server.skins())


func _show(state: ApiResult) -> void:
	if not is_inside_tree():
		return
	if not state.ok:
		status.text = state.error
		return
	_gems.text = "Du hast %d Gems." % int(state.data.get("gems", 0))
	for child: Node in _list.get_children():
		_list.remove_child(child)
		child.queue_free()
	for entry: Variant in state.data.get("skins", []):
		if not entry is Dictionary:
			continue
		var offer := entry as Dictionary
		var skin := Catalog.skin(str(offer.get("id", "")))
		# A skin the server sells but this version of the app cannot draw is left out.
		if skin.id != str(offer.get("id", "")):
			continue
		_list.add_child(_row(skin, int(offer.get("cost", 0)), bool(offer.get("owned", false))))


func _row(skin: HeroSkin, cost: int, owned: bool) -> Button:
	var worn := _worn() == skin.id
	var label := "%s – angelegt" % skin.title if worn else "%s anlegen" % skin.title if owned else "%s kaufen – %d Gems" % [skin.title, cost]
	var color := UiKit.GO if worn else Color(0.22, 0.33, 0.62) if owned else Color(0.5, 0.3, 0.6)
	var row := UiKit.button(label, color.lerp(skin.tint.clamp(), 0.25), 28)
	row.custom_minimum_size = Vector2(0, 84)
	if owned:
		row.pressed.connect(_wear.bind(skin))
	else:
		row.pressed.connect(_buy.bind(skin))
	return row


## The id of the skin the hero wears; the free knight when nothing was chosen yet.
static func _worn() -> String:
	return Catalog.skin(Settings.skin()).id


func _wear(skin: HeroSkin) -> void:
	Settings.set_skin(skin.id)
	Log.line("skin angelegt: %s" % skin.title)
	back.emit()


func _buy(skin: HeroSkin) -> void:
	if _busy:
		return
	_busy = true
	@warning_ignore("redundant_await")
	var bought := await server.buy_skin(skin.id)
	_busy = false
	if not is_inside_tree():
		return
	if not bought.ok:
		status.text = bought.error
		return
	Settings.set_skin(skin.id)
	Log.line("skin gekauft: %s" % skin.title)
	_show(bought)
	status.text = "%s gehört jetzt dir!" % skin.title
