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
	private const string TickerNodeName = "PRTSCursorTicker";

	private static Harmony? _harmony;
	private static bool _hooksInstalled;

	public static void Initialize()
	{
		Harmony harmony = _harmony ??= new Harmony(HarmonyId);
		InstallHooks(harmony);
		TryStartController(NGame.Instance, "initializer");
		Log.Info($"{LogPrefix} Loaded.");
	}

	private static void InstallHooks(Harmony harmony)
	{
		if (_hooksInstalled)
		{
			return;
		}

		// Each hook is installed independently so that a single signature change in a future
		// game patch only disables the cursor animation instead of throwing out of the whole
		// initializer and making the mod fail to load.
		TryPatch(harmony, typeof(NGame), nameof(NGame._Ready),
			BindingFlags.Instance | BindingFlags.Public, Type.EmptyTypes,
			postfix: nameof(NGameReadyPostfix));
		TryPatch(harmony, typeof(NCursorManager), nameof(NCursorManager._EnterTree),
			BindingFlags.Instance | BindingFlags.Public, Type.EmptyTypes,
			postfix: nameof(NCursorManagerEnterTreePostfix));
		TryPatch(harmony, typeof(NCursorManager), nameof(NCursorManager._Ready),
			BindingFlags.Instance | BindingFlags.Public, Type.EmptyTypes,
			postfix: nameof(NCursorManagerReadyPostfix));
		// NCursorManager.UpdateCursor is the single private method that pushes the OS cursor
		// through Input.SetCustomMouseCursor (Arrow shape). Suppressing it stops the vanilla
		// static arrow from overwriting our per-frame animated hardware cursor whenever the
		// game refreshes the cursor (e.g. on mouse down/up). It is still a private instance
		// method on the current build, verified against the decompiled sts2.dll.
		TryPatch(harmony, typeof(NCursorManager), "UpdateCursor",
			BindingFlags.Instance | BindingFlags.NonPublic, Type.EmptyTypes,
			prefix: nameof(NCursorManagerUpdateCursorPrefix));

		_hooksInstalled = true;
	}

	private static void TryPatch(Harmony harmony, Type type, string name, BindingFlags flags, Type[] parameters,
		string? prefix = null, string? postfix = null)
	{
		try
		{
			MethodInfo target = RequireMethod(type, name, flags, parameters);
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
		TryStartController(__instance, "NGame._Ready");
	}

	private static void NCursorManagerEnterTreePostfix(NCursorManager __instance)
	{
		TryStartController(NGame.Instance, "NCursorManager._EnterTree");
	}

	private static void NCursorManagerReadyPostfix(NCursorManager __instance)
	{
		TryStartController(NGame.Instance, "NCursorManager._Ready");
	}

	private static bool NCursorManagerUpdateCursorPrefix()
	{
		return CursorAnimationController.BeforeVanillaUpdateCursor();
	}

	private static void TryStartController(NGame? game, string source)
	{
		if (game == null || !GodotObject.IsInstanceValid(game))
		{
			return;
		}

		CursorAnimationController.EnsureStarted(game, TickerNodeName, source);
	}

	private static MethodInfo RequireMethod(Type type, string name, BindingFlags flags, params Type[] parameters)
	{
		return type.GetMethod(name, flags, binder: null, parameters, modifiers: null)
			?? throw new InvalidOperationException($"Could not find required method {type.FullName}.{name}.");
	}
}

internal static class CursorAnimationController
{
	private const string LogPrefix = "[PRTSCursor]";
	private const string AssetRoot = "res://PRTSCursor/cursors";
	private const int MaxFrameCount = 4096;
	private const double FrameDurationSeconds = 1.0 / 60.0;
	private static readonly Vector2 Hotspot = new(29f, 37f);

	private static Resource[] _frames = Array.Empty<Resource>();
	private static CursorAnimationTicker? _ticker;
	private static bool _loaded;
	private static bool _skipCursorApply;
	private static bool _loggedHeadlessSkip;
	private static bool _loggedStarted;

	public static void EnsureStarted(NGame game, string tickerNodeName, string source)
	{
		if (!GodotObject.IsInstanceValid(game))
		{
			return;
		}

		try
		{
			EnsureFramesLoaded();
			EnsureTicker(game, tickerNodeName, source);
		}
		catch (Exception ex)
		{
			Log.Warn($"{LogPrefix} Failed to start cursor animation from {source}: {ex.Message}", 2);
		}
	}

	// Prefix for NCursorManager.UpdateCursor. Returning false skips the vanilla body so the
	// game never overwrites our animated hardware cursor with its static arrow image. In
	// headless runs (or before frames are loaded) we let the vanilla logic run untouched.
	public static bool BeforeVanillaUpdateCursor()
	{
		if (_skipCursorApply || !_loaded || _frames.Length == 0)
		{
			return true;
		}

		return false;
	}

	private static void EnsureFramesLoaded()
	{
		if (_loaded)
		{
			return;
		}

		_frames = LoadFrameImages("default", "default");
		_skipCursorApply = IsHeadlessRun();
		_loaded = true;
		Log.Info($"{LogPrefix} Loaded {_frames.Length} default/Idle cursor frames. Dragging/Click cursor frames are disabled.");
		if (_skipCursorApply && !_loggedHeadlessSkip)
		{
			Log.Info($"{LogPrefix} Headless mode detected; skipping cursor animation apply calls.");
			_loggedHeadlessSkip = true;
		}
	}

	private static void EnsureTicker(NGame game, string tickerNodeName, string source)
	{
		if (_skipCursorApply || _frames.Length == 0)
		{
			return;
		}

		if (_ticker != null && GodotObject.IsInstanceValid(_ticker) && _ticker.GetParent() != null)
		{
			return;
		}

		try
		{
			if (game.GetNodeOrNull<CursorAnimationTicker>(tickerNodeName) is { } existing && GodotObject.IsInstanceValid(existing))
			{
				existing.Configure(_frames, Hotspot, FrameDurationSeconds);
				_ticker = existing;
				return;
			}

			var ticker = new CursorAnimationTicker
			{
				Name = tickerNodeName
			};
			ticker.Configure(_frames, Hotspot, FrameDurationSeconds);
			game.AddChild(ticker);
			_ticker = ticker;

			if (!_loggedStarted)
			{
				Log.Info($"{LogPrefix} Started animated hardware cursor from {source}; fps=60, hotspot={Hotspot}.");
				_loggedStarted = true;
			}
		}
		catch (Exception ex)
		{
			Log.Warn($"{LogPrefix} Failed to create cursor ticker from {source}: {ex.Message}", 2);
		}
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

	// Each frame is converted to a Godot.Image when possible because the vanilla cursor manager
	// notes that Input.SetCustomMouseCursor is far cheaper with an Image than a Texture2D. We
	// fall back to the Texture2D itself if the imported texture cannot be read back to an Image.
	private static Resource[] LoadFrameImages(string folder, string prefix)
	{
		var frames = new List<Resource>();
		for (int i = 0; i < MaxFrameCount; i++)
		{
			string path = $"{AssetRoot}/{folder}/{prefix}_{i:00}.png";
			if (!Godot.FileAccess.FileExists(path) && !ResourceLoader.Exists(path))
			{
				break;
			}

			frames.Add(LoadFrame(path));
		}

		if (frames.Count == 0)
		{
			throw new FileNotFoundException($"Could not find cursor frames matching {AssetRoot}/{folder}/{prefix}_*.png.");
		}

		return frames.ToArray();
	}

	private static Resource LoadFrame(string path)
	{
		Texture2D? texture = ResourceLoader.Load<Texture2D>(path, cacheMode: ResourceLoader.CacheMode.Reuse);
		if (texture != null)
		{
			Image? fromTexture = texture.GetImage();
			if (fromTexture != null)
			{
				return fromTexture;
			}

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
			return image;
		}

		throw new FileNotFoundException($"Could not load cursor frame from {path}.");
	}
}

// Lightweight per-frame driver. It owns no visuals of its own; every frame it advances the
// animation index and pushes the current frame to the OS through Input.SetCustomMouseCursor,
// which lets the hardware cursor itself animate. Because the OS positions the cursor, this is
// immune to viewport content-scale / HiDPI coordinate mismatches that broke the old Sprite2D
// overlay (the mouse position reported in physical pixels fell outside the logical visible
// rect on retina displays, so the overlay sprite was permanently hidden).
internal sealed partial class CursorAnimationTicker : Node
{
	private Resource[] _frames = Array.Empty<Resource>();
	private Vector2 _hotspot;
	private double _frameDurationSeconds;
	private double _frameAccumulator;
	private int _frameIndex;
	private bool _configured;
	private int _appliedIndex = -1;

	public void Configure(Resource[] frames, Vector2 hotspot, double frameDurationSeconds)
	{
		_frames = frames;
		_hotspot = hotspot;
		_frameDurationSeconds = frameDurationSeconds;
		ProcessMode = ProcessModeEnum.Always;

		_frameIndex = 0;
		_frameAccumulator = 0.0;
		_appliedIndex = -1;
		_configured = _frames.Length > 0;

		SetProcess(true);
	}

	public override void _Ready()
	{
		SetProcess(true);
	}

	public override void _Process(double delta)
	{
		if (!_configured || _frames.Length == 0)
		{
			return;
		}

		AdvanceFrame(delta);

		// Respect the game hiding the cursor for controller play. When hidden we leave the OS
		// cursor alone (the game has already set MouseMode.Hidden) and force a re-apply next
		// time it becomes visible.
		if (Input.MouseMode == Input.MouseModeEnum.Hidden)
		{
			_appliedIndex = -1;
			return;
		}

		if (_frameIndex == _appliedIndex)
		{
			return;
		}

		Input.SetCustomMouseCursor(_frames[_frameIndex], Input.CursorShape.Arrow, _hotspot);
		_appliedIndex = _frameIndex;
	}

	private void AdvanceFrame(double delta)
	{
		double clampedDelta = Math.Clamp(delta, 0.0, 0.25);
		_frameAccumulator += clampedDelta;
		int steps = (int)(_frameAccumulator / _frameDurationSeconds);
		if (steps <= 0)
		{
			return;
		}

		_frameAccumulator -= steps * _frameDurationSeconds;
		_frameIndex = (_frameIndex + steps) % _frames.Length;
	}
}
