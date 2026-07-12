extends SceneTree

const SkeletonDataPath := "res://PRTSCursor/animations/prts_pointer/enemy_10072_mpprhd_skel_data.tres"


func _init() -> void:
	var args := OS.get_cmdline_user_args()
	if args.size() != 1:
		push_error("Usage: -- <PRTSCursor.pck>")
		quit(1)
		return

	if not ProjectSettings.load_resource_pack(args[0], false):
		push_error("Could not load PCK: %s" % args[0])
		quit(2)
		return

	var sprite := ClassDB.instantiate("SpineSprite")
	sprite.set("skeleton_data_res", ResourceLoader.load(SkeletonDataPath))
	get_root().add_child(sprite)
	await process_frame
	await process_frame

	print("--- SpineSprite properties ---")
	for property in sprite.get_property_list():
		var name := String(property.get("name", ""))
		var lower_name := name.to_lower()
		if lower_name.contains("update") or lower_name.contains("time") or lower_name.contains("process") or lower_name.contains("advance"):
			print(name, "=", sprite.get(name), " hint=", property.get("hint", -1), " hint_string=", property.get("hint_string", ""))

	print("--- SpineSprite methods ---")
	for method in sprite.get_method_list():
		var name := String(method.get("name", ""))
		var lower_name := name.to_lower()
		if lower_name.contains("update") or lower_name.contains("time") or lower_name.contains("process") or lower_name.contains("advance"):
			print(name)

	var state = sprite.call("get_animation_state")
	print("--- SpineAnimationState methods ---")
	if state != null:
		for method in state.get_method_list():
			var name := String(method.get("name", ""))
			var lower_name := name.to_lower()
			if lower_name.contains("update") or lower_name.contains("time") or lower_name.contains("process") or lower_name.contains("advance") or lower_name.contains("apply"):
				print(name)

	quit(0)
