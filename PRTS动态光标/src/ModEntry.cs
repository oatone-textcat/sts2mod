using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace PRTSCursor;

[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
	private const string HarmonyId = "Natsuki.PRTSCursor";
	private const string LogPrefix = "[PRTSCursor]";

	private static Harmony? _harmony;
	private static bool _hooksInstalled;

	public static void Initialize()
	{
		Harmony harmony = _harmony ??= new Harmony(HarmonyId);
		InstallHooks(harmony);
		CursorAnimationController.EnsureStarted("initializer");
		Log.Info($"{LogPrefix} Loaded.");
	}

	private static void InstallHooks(Harmony harmony)
	{
		if (_hooksInstalled)
		{
			return;
		}

		// Each hook is installed independently so that a single signature change in a future game
		// patch only disables the cursor animation instead of throwing out of the whole initializer
		// and making the mod fail to load.
		TryPatch(harmony, typeof(NGame), nameof(NGame._Ready),
			BindingFlags.Instance | BindingFlags.Public,
			postfix: nameof(NGameReadyPostfix));
		TryPatch(harmony, typeof(NCursorManager), nameof(NCursorManager._EnterTree),
			BindingFlags.Instance | BindingFlags.Public,
			postfix: nameof(NCursorManagerEnterTreePostfix));
		TryPatch(harmony, typeof(NCursorManager), nameof(NCursorManager._Ready),
			BindingFlags.Instance | BindingFlags.Public,
			postfix: nameof(NCursorManagerReadyPostfix));
		// NCursorManager.UpdateCursor is the single private method that pushes the vanilla cursor
		// image (CursorNotTilted when idle, CursorTilted while a mouse button is held) onto the OS
		// Arrow cursor. We suppress it so the game can never repaint the OS cursor over our hidden
		// (fully transparent) hardware cursor; the animated PRTS overlay then provides BOTH the idle
		// and the click/pressed cursor. It is still a private instance method on the current build,
		// verified against the decompiled sts2.dll.
		TryPatch(harmony, typeof(NCursorManager), "UpdateCursor",
			BindingFlags.Instance | BindingFlags.NonPublic,
			prefix: nameof(NCursorManagerUpdateCursorPrefix));

		_hooksInstalled = true;
	}

	private static void TryPatch(Harmony harmony, Type type, string name, BindingFlags flags,
		string? prefix = null, string? postfix = null)
	{
		try
		{
			MethodInfo target = RequireMethod(type, name, flags);
			harmony.Patch(target,
				prefix: prefix == null ? null : new HarmonyMethod(typeof(ModEntry), prefix),
				postfix: postfix == null ? null : new HarmonyMethod(typeof(ModEntry), postfix));
		}
		catch (Exception ex)
		{
			Log.Warn($"{LogPrefix} Failed to hook {type.Name}.{name}: {ex.Message}", 2);
		}
	}

	private static void NGameReadyPostfix(NGame __instance)
	{
		CursorAnimationController.EnsureStarted("NGame._Ready");
	}

	private static void NCursorManagerEnterTreePostfix(NCursorManager __instance)
	{
		CursorAnimationController.EnsureStarted("NCursorManager._EnterTree");
	}

	private static void NCursorManagerReadyPostfix(NCursorManager __instance)
	{
		CursorAnimationController.EnsureStarted("NCursorManager._Ready");
	}

	private static bool NCursorManagerUpdateCursorPrefix()
	{
		return CursorAnimationController.BeforeVanillaUpdateCursor();
	}

	private static MethodInfo RequireMethod(Type type, string name, BindingFlags flags, params Type[] parameters)
	{
		return type.GetMethod(name, flags, binder: null, parameters, modifiers: null)
			?? throw new InvalidOperationException($"Could not find required method {type.FullName}.{name}.");
	}
}

// Renders the animated PRTS cursor as a Sprite2D on a top-most CanvasLayer inside the game's own
// framebuffer and follows the mouse every frame.
//
// IMPORTANT: this mod is built with Microsoft.NET.Sdk (standard single-DLL mod layout), so the Godot
// C# source generators do NOT run. That means a custom Node subclass would never receive its
// _Process / _Ready / _EnterTree callbacks (Godot has no generated binding for it) — earlier versions
// failed for exactly this reason: the node entered the tree but never ticked, so the cursor never
// animated or even showed. We therefore use only built-in node types (CanvasLayer + Sprite2D, which
// need no overrides) and drive the per-frame update from the SceneTree.ProcessFrame signal via a
// plain Callable, which works without any source-generated bindings.
//
// Rendering in-engine also avoids the macOS issue where pushing a new OS hardware cursor image every
// frame flickers / is dropped, and it lets screenshots capture the cursor.
internal static class CursorAnimationController
{
	private const string LogPrefix = "[PRTSCursor]";
	private const string AssetRoot = "res://PRTSCursor/cursors";
	private const string LayerNodeName = "PRTSCursorOverlay";
	private const string SpriteNodeName = "PRTSCursorSprite";
	private const int MaxFrameCount = 4096;
	private const double FrameDurationSeconds = 1.0 / 60.0;
	private static readonly Vector2 Hotspot = new(29f, 37f);

	private static Texture2D[] _frames = Array.Empty<Texture2D>();
	private static Image? _transparentCursor;
	private static CanvasLayer? _layer;
	private static Sprite2D? _sprite;
	private static SceneTree? _connectedTree;
	private static Action? _processHandler;

	private static bool _loaded;
	private static bool _skipCursorApply;
	private static bool _transparentCursorApplied;
	private static bool _loggedHeadlessSkip;
	private static bool _loggedStarted;
	private static bool _loggedApplyFailure;

	private static double _frameAccumulator;
	private static int _frameIndex;
	private static ulong _lastTickMsec;

	// All cursor shapes the engine can request. The vanilla cursor manager only ever drives the
	// Arrow shape (plus Help for the inspect cursor), but if any control requests another shape the
	// OS would fall back to its system cursor and briefly show a non-PRTS pointer. Hiding every shape
	// with the same transparent 1x1 image guarantees the PRTS overlay is the only cursor on screen.
	private static readonly Input.CursorShape[] AllCursorShapes =
	{
		Input.CursorShape.Arrow,
		Input.CursorShape.Ibeam,
		Input.CursorShape.PointingHand,
		Input.CursorShape.Cross,
		Input.CursorShape.Wait,
		Input.CursorShape.Busy,
		Input.CursorShape.Drag,
		Input.CursorShape.CanDrop,
		Input.CursorShape.Forbidden,
		Input.CursorShape.Vsize,
		Input.CursorShape.Hsize,
		Input.CursorShape.Bdiagsize,
		Input.CursorShape.Fdiagsize,
		Input.CursorShape.Move,
		Input.CursorShape.Vsplit,
		Input.CursorShape.Hsplit,
		Input.CursorShape.Help,
	};

	public static void EnsureStarted(string source)
	{
		try
		{
			EnsureFramesLoaded();
			EnsureOverlay(source);
			ApplyTransparentCursor(force: true);
		}
		catch (Exception ex)
		{
			Log.Warn($"{LogPrefix} Failed to start cursor animation from {source}: {ex.Message}", 2);
		}
	}

	// Prefix for NCursorManager.UpdateCursor. Returning false skips the vanilla body so the game
	// never repaints the OS cursor over our transparent one. In headless runs (or before the overlay
	// exists) we let the vanilla logic run untouched.
	public static bool BeforeVanillaUpdateCursor()
	{
		try
		{
			EnsureFramesLoaded();
			if (_skipCursorApply || _sprite == null || !GodotObject.IsInstanceValid(_sprite))
			{
				return true;
			}

			// Re-assert the transparent cursor in case the engine reset it (e.g. after the mouse mode
			// toggled back to visible following controller use).
			ApplyTransparentCursor(force: true);
			return false;
		}
		catch (Exception ex)
		{
			if (!_loggedApplyFailure)
			{
				Log.Warn($"{LogPrefix} Failed to suppress vanilla cursor update: {ex.Message}", 2);
				_loggedApplyFailure = true;
			}

			return true;
		}
	}

	private static void EnsureFramesLoaded()
	{
		if (_loaded)
		{
			return;
		}

		_frames = LoadFrameTextures("default", "default");
		_skipCursorApply = IsHeadlessRun();
		_loaded = true;
		Log.Info($"{LogPrefix} Loaded {_frames.Length} PRTS cursor frames (used for both the idle and the click/pressed cursor).");
		if (_skipCursorApply && !_loggedHeadlessSkip)
		{
			Log.Info($"{LogPrefix} Headless mode detected; skipping cursor overlay and actual cursor apply calls.");
			_loggedHeadlessSkip = true;
		}
	}

	private static void EnsureOverlay(string source)
	{
		if (_skipCursorApply || _frames.Length == 0)
		{
			return;
		}

		if (Engine.GetMainLoop() is not SceneTree tree
			|| tree.Root == null || !GodotObject.IsInstanceValid(tree.Root))
		{
			return;
		}

		// (Re)build the CanvasLayer + Sprite2D if it is missing or has fallen out of the tree.
		if (_layer == null || !GodotObject.IsInstanceValid(_layer) || !_layer.IsInsideTree())
		{
			try
			{
				if (_layer != null && GodotObject.IsInstanceValid(_layer))
				{
					_layer.GetParent()?.RemoveChild(_layer);
					_layer.QueueFree();
				}

				var layer = new CanvasLayer
				{
					Name = LayerNodeName,
					Layer = 4096,
					FollowViewportEnabled = false
				};
				var sprite = new Sprite2D
				{
					Name = SpriteNodeName,
					Centered = false,
					ZIndex = 4096,
					Visible = false,
					Texture = _frames[0]
				};
				layer.AddChild(sprite);

				// Parent to the root Window so the layer is always inside the live tree and rendered
				// above all gameplay canvas layers. Add directly from live-tree hooks; fall back to a
				// deferred add if the tree is still being built (mod initializer).
				try
				{
					tree.Root.AddChild(layer);
				}
				catch (Exception)
				{
					tree.Root.CallDeferred(Node.MethodName.AddChild, layer);
				}

				_layer = layer;
				_sprite = sprite;
				_frameIndex = 0;
				_frameAccumulator = 0.0;
				_lastTickMsec = 0;

				if (!_loggedStarted)
				{
					Log.Info($"{LogPrefix} Started PRTS cursor overlay from {source}; fps=60, hotspot={Hotspot}.");
					_loggedStarted = true;
				}
			}
			catch (Exception ex)
			{
				Log.Warn($"{LogPrefix} Failed to create cursor overlay from {source}: {ex.Message}", 2);
				return;
			}
		}

		// Drive the animation from the SceneTree's per-frame signal. A plain Callable (delegate)
		// works without the Godot C# source generators that a custom Node._Process would require.
		if (_connectedTree == null || !GodotObject.IsInstanceValid(_connectedTree))
		{
			_processHandler ??= OnProcessFrame;
			tree.ProcessFrame += _processHandler;
			_connectedTree = tree;
			Log.Info($"{LogPrefix} Connected to SceneTree.ProcessFrame.");
		}
	}

	private static void OnProcessFrame()
	{
		if (_sprite == null || !GodotObject.IsInstanceValid(_sprite) || !_sprite.IsInsideTree() || _frames.Length == 0)
		{
			return;
		}

		try
		{
			AdvanceFrame();
			UpdatePosition();
		}
		catch (Exception ex)
		{
			if (!_loggedApplyFailure)
			{
				Log.Warn($"{LogPrefix} ProcessFrame update failed: {ex.Message}", 2);
				_loggedApplyFailure = true;
			}
		}
	}

	private static void AdvanceFrame()
	{
		ulong now = Time.GetTicksMsec();
		if (_lastTickMsec == 0)
		{
			_lastTickMsec = now;
		}

		double delta = Math.Clamp((now - _lastTickMsec) / 1000.0, 0.0, 0.25);
		_lastTickMsec = now;

		_frameAccumulator += delta;
		int steps = (int)(_frameAccumulator / FrameDurationSeconds);
		if (steps > 0)
		{
			_frameAccumulator -= steps * FrameDurationSeconds;
			_frameIndex = (_frameIndex + steps) % _frames.Length;
		}

		_sprite!.Texture = _frames[_frameIndex];
	}

	private static void UpdatePosition()
	{
		// The game hides the OS cursor (mouse mode) when a controller is in use; mirror that so the
		// PRTS cursor disappears too instead of floating at its last spot.
		if (Input.MouseMode == Input.MouseModeEnum.Hidden)
		{
			_sprite!.Visible = false;
			return;
		}

		// GetGlobalMousePosition() is self-consistent with GlobalPosition through the canvas
		// transform, so the cursor stays glued to the pointer on HiDPI / content-scaled displays.
		Vector2 mousePosition = _sprite!.GetGlobalMousePosition();
		if (float.IsNaN(mousePosition.X) || float.IsNaN(mousePosition.Y)
			|| float.IsInfinity(mousePosition.X) || float.IsInfinity(mousePosition.Y))
		{
			_sprite.Visible = false;
			return;
		}

		_sprite.GlobalPosition = mousePosition - Hotspot;
		_sprite.Visible = true;
	}

	private static void ApplyTransparentCursor(bool force)
	{
		if (_skipCursorApply)
		{
			return;
		}

		if (_transparentCursorApplied && !force)
		{
			return;
		}

		Image transparent = EnsureTransparentCursor();
		foreach (Input.CursorShape shape in AllCursorShapes)
		{
			Input.SetCustomMouseCursor(transparent, shape, Vector2.Zero);
		}

		_transparentCursorApplied = true;
		_loggedApplyFailure = false;
	}

	private static Image EnsureTransparentCursor()
	{
		if (_transparentCursor != null)
		{
			return _transparentCursor;
		}

		Image image = Image.CreateEmpty(1, 1, useMipmaps: false, Image.Format.Rgba8);
		image.Fill(new Color(0f, 0f, 0f, 0f));
		_transparentCursor = image;
		return image;
	}

	private static bool IsHeadlessRun()
	{
		try
		{
			if (string.Equals(DisplayServer.GetName(), "headless", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		catch
		{
			// Some test hosts may not have a display server initialized yet.
		}

		foreach (string arg in OS.GetCmdlineArgs())
		{
			if (string.Equals(arg, "--headless", StringComparison.Ordinal)
				|| arg.StartsWith("--headless=", StringComparison.Ordinal))
			{
				return true;
			}
		}

		return false;
	}

	private static Texture2D[] LoadFrameTextures(string folder, string prefix)
	{
		var frames = new List<Texture2D>();
		for (int i = 0; i < MaxFrameCount; i++)
		{
			string path = $"{AssetRoot}/{folder}/{prefix}_{i:00}.png";
			if (!Godot.FileAccess.FileExists(path) && !ResourceLoader.Exists(path))
			{
				break;
			}

			frames.Add(LoadTexture(path));
		}

		if (frames.Count == 0)
		{
			throw new FileNotFoundException($"Could not find cursor frames matching {AssetRoot}/{folder}/{prefix}_*.png.");
		}

		return frames.ToArray();
	}

	private static Texture2D LoadTexture(string path)
	{
		Texture2D? texture = ResourceLoader.Load<Texture2D>(path, cacheMode: ResourceLoader.CacheMode.Reuse);
		if (texture != null)
		{
			return texture;
		}

		Image? image = null;
		try
		{
			image = Image.LoadFromFile(path);
		}
		catch
		{
			image = null;
		}

		if (image != null)
		{
			return ImageTexture.CreateFromImage(image);
		}

		throw new FileNotFoundException($"Could not load cursor texture from {path}.");
	}
}
