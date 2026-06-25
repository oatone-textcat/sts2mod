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

	var skeleton = sprite.call("get_skeleton")
	var data = skeleton.call("get_data")
	var animations = data.call("get_animations")
	for animation in animations:
		var name := str(animation.call("get_name"))
		var duration := -1.0
		if animation.has_method("get_duration"):
			duration = float(animation.call("get_duration"))
		elif animation.has_method("getDuration"):
			duration = float(animation.call("getDuration"))
		print("%s duration=%.6f" % [name, duration])

	quit(0)
