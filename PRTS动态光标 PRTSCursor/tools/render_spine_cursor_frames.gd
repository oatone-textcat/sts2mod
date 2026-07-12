extends SceneTree

const SkeletonDataPath := "res://PRTSCursor/animations/prts_pointer/enemy_10072_mpprhd_skel_data.tres"
const CanvasSize := Vector2i(256, 256)
const Fps := 60.0
const SpriteScale := Vector2(0.34, 0.34)
const SpritePosition := Vector2(72, 72)


func _init() -> void:
	var args := OS.get_cmdline_user_args()
	if args.size() != 2:
		push_error("Usage: -- <PRTSCursor.pck> <output_root>")
		quit(1)
		return

	var pck_path := args[0]
	var output_root := args[1]
	if not ProjectSettings.load_resource_pack(pck_path, false):
		push_error("Could not load PCK: %s" % pck_path)
		quit(2)
		return

	DirAccess.make_dir_recursive_absolute(output_root.path_join("default_raw"))

	await _render_animation("Idle", output_root.path_join("default_raw"), "default")
	quit(0)


func _render_animation(animation_name: String, output_dir: String, prefix: String) -> void:
	var viewport := SubViewport.new()
	viewport.size = CanvasSize
	viewport.transparent_bg = true
	viewport.render_target_clear_mode = SubViewport.CLEAR_MODE_ALWAYS
	viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	get_root().add_child(viewport)

	var sprite := ClassDB.instantiate("SpineSprite")
	if sprite == null:
		push_error("Could not instantiate SpineSprite")
		quit(3)
		return

	sprite.set("skeleton_data_res", ResourceLoader.load(SkeletonDataPath))
	sprite.call("set_time_scale", 0.0)
	sprite.set("position", SpritePosition)
	sprite.set("scale", SpriteScale)
	viewport.add_child(sprite)

	await process_frame
	await process_frame

	var animation_state = sprite.call("get_animation_state")
	if animation_state == null:
		push_error("Could not get SpineAnimationState for %s" % animation_name)
		quit(4)
		return

	var duration := _get_animation_duration(sprite, animation_name)
	var frame_count := max(1, int(round(duration * Fps)))
	var frame_step := duration / float(frame_count)
	print("render animation=%s duration=%.6f fps=%.3f frames=%d step=%.6f" % [
		animation_name,
		duration,
		Fps,
		frame_count,
		frame_step,
	])

	animation_state.call("set_animation", animation_name, true, 0)
	for frame in frame_count:
		var skeleton = sprite.call("get_skeleton")
		if frame > 0:
			animation_state.call("update", frame_step)
		if skeleton != null:
			animation_state.call("apply", skeleton)

		await process_frame
		await RenderingServer.frame_post_draw

		var image := viewport.get_texture().get_image()
		var path := output_dir.path_join("%s_%02d.png" % [prefix, frame])
		var err := image.save_png(path)
		if err != OK:
			push_error("Failed to save %s: %s" % [path, err])
			quit(err)
			return

	viewport.queue_free()


func _get_animation_duration(sprite: Object, animation_name: String) -> float:
	var skeleton = sprite.call("get_skeleton")
	if skeleton == null:
		return 1.0

	var data = skeleton.call("get_data")
	if data == null:
		return 1.0

	var animations = data.call("get_animations")
	for animation in animations:
		if str(animation.call("get_name")) != animation_name:
			continue

		if animation.has_method("get_duration"):
			return max(1.0 / Fps, float(animation.call("get_duration")))
		if animation.has_method("getDuration"):
			return max(1.0 / Fps, float(animation.call("getDuration")))

	return 1.0
