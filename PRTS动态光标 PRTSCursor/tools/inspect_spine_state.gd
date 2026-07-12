extends SceneTree


func _init() -> void:
	var args := OS.get_cmdline_user_args()
	if args.size() != 3:
		push_error("Usage: -- <atlas_path> <skel_path> <png_path>")
		quit(1)
		return

	var bytes := FileAccess.get_file_as_bytes(args[2])
	var image := Image.new()
	var err := image.load_png_from_buffer(bytes)
	if err != OK:
		push_error("png decode failed: %s" % err)
		quit(err)
		return

	var atlas := ClassDB.instantiate("SpineAtlasResource")
	var atlas_err = atlas.call("load_from_atlas_file", args[0])
	atlas.set("textures", [ImageTexture.create_from_image(image)])

	var skeleton_file := ClassDB.instantiate("SpineSkeletonFileResource")
	var skel_err = skeleton_file.call("load_from_file", args[1])

	var data_res := ClassDB.instantiate("SpineSkeletonDataResource")
	data_res.set("atlas_res", atlas)
	data_res.set("skeleton_file_res", skeleton_file)
	data_res.set("default_mix", 0.05)
	data_res.call("update_skeleton_data")

	var sprite := ClassDB.instantiate("SpineSprite")
	sprite.set("skeleton_data_res", data_res)
	get_root().add_child(sprite)
	await process_frame

	print("atlas_err=", atlas_err, " skel_err=", skel_err)
	var state = sprite.call("get_animation_state")
	print("state=", state)
	if state != null:
		for method in state.get_method_list():
			var name := String(method.get("name", ""))
			if not name.begins_with("_"):
				print(name)

		var set_result = state.call("set_animation", "Idle", true, 0)
		print("set_animation_result=", set_result)
		await process_frame

	quit()
