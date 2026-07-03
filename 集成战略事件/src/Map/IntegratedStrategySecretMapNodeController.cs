using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using IntegratedStrategyEvents.Events;
using IntegratedStrategyEvents.Relics;
using IntegratedStrategyEvents.TreeHoles;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace IntegratedStrategyEvents.Map;

internal static class IntegratedStrategySecretMapNodeController
{
	private const int MinimumSecretNodes = 1;
	private const int MaximumSecretNodes = 3;
	private const string SecretIconPath = $"res://{ModInfo.ModId}/images/map/map_secret.png";
	private const string SecretOutlinePath = $"res://{ModInfo.ModId}/images/map/map_secret_outline.png";
	private static readonly Vector2 SecretMapIconSize = new(48f, 48f);
	private static readonly Vector2 SecretMapOutlineSize = new(54f, 54f);
	private static readonly Vector2 SecretMapHoverScale = Vector2.One * 1.45f;
	public const string SecretLegendItemName = "SecretLegendItem";
	public const MapPointType SecretLegendHighlightPointType = MapPointType.Boss;
	private static readonly Type[] TreeHoleEventTypes =
	[
		typeof(ForwardForestEvent),
		typeof(StoryToBeToldEvent),
		typeof(ShiftingCityEvent),
		typeof(GlimpseEvent)
	];

	private static readonly ConditionalWeakTable<RunState, SecretNodeStore> SecretNodes = new();
	private static Texture2D? _secretIcon;
	private static Texture2D? _secretOutline;

	public static void MarkSecretNodes(IRunState runState, ActMap map, int actIndex)
	{
		if (runState is not RunState state || ShouldSkipMap(state, map))
		{
			return;
		}

		SecretNodeStore store = SecretNodes.GetOrCreateValue(state);
		HashSet<MapCoord> selectedCoords = SelectSecretNodeCoords(state, map, actIndex);
		store.Set(actIndex, selectedCoords);
		if (selectedCoords.Count > 0)
		{
			Log.Info(
				$"{ModInfo.LogPrefix} Marked {selectedCoords.Count} secret map node(s) " +
				$"in act {actIndex}: {string.Join(", ", selectedCoords.Select(FormatCoord))}.");
		}
	}

	public static bool IsAtSecretNode(RunState state)
	{
		return TryGetCurrentUnknownMapPoint(state, out MapPoint point) &&
			IsSecretNode(state, state.Map, state.CurrentActIndex, point.coord);
	}

	public static bool IsAtProphetHornSecretNode(RunState state)
	{
		return TryGetCurrentUnknownMapPoint(state, out MapPoint point) &&
			TryGetProphetHornSecretNodeCoord(state, state.Map, state.CurrentActIndex, out MapCoord coord) &&
			point.coord.Equals(coord);
	}

	public static bool IsAtSecretNodeCurrentRun()
	{
		return RunManager.Instance.DebugOnlyGetState() is RunState state &&
			IsAtSecretNode(state);
	}

	public static bool TryApplySecretNodeIcon(NNormalMapPoint mapPointNode)
	{
		RunState? state = RunManager.Instance.DebugOnlyGetState();
		MapPoint? point = mapPointNode.Point;
		if (state == null || point == null || !IsSecretNode(state, point))
		{
			return false;
		}

		Texture2D? icon = GetSecretIcon();
		Texture2D? outline = GetSecretOutline();
		if (icon == null || outline == null)
		{
			return false;
		}

			TextureRect iconRect = IntegratedStrategyMapReflectionCache.NormalMapPointIcon(mapPointNode);
			TextureRect outlineRect = IntegratedStrategyMapReflectionCache.NormalMapPointOutline(mapPointNode);
		iconRect.Texture = icon;
		outlineRect.Texture = outline;
		Vector2 center = GetIconContainerCenter(mapPointNode, iconRect);
		ResizeAroundCenter(iconRect, SecretMapIconSize, center);
		ResizeSecretOutlineAroundIconCenter(outlineRect, iconRect);
		ApplySecretNodeOpacity(mapPointNode, iconRect, outlineRect);
		return true;
	}

	public static bool TryApplySecretNodeOpacity(NNormalMapPoint mapPointNode)
	{
		if (!IsSecretMapPointNode(mapPointNode))
		{
			return false;
		}

			ApplySecretNodeOpacity(
				mapPointNode,
				IntegratedStrategyMapReflectionCache.NormalMapPointIcon(mapPointNode),
				IntegratedStrategyMapReflectionCache.NormalMapPointOutline(mapPointNode));
		return true;
	}

	public static bool ApplySecretNodeHighlightOpacity(NNormalMapPoint mapPointNode, bool highlighted)
	{
		if (!IsSecretMapPointNode(mapPointNode))
		{
			return false;
		}

			ApplySecretNodeOpacity(
				mapPointNode,
				IntegratedStrategyMapReflectionCache.NormalMapPointIcon(mapPointNode),
				IntegratedStrategyMapReflectionCache.NormalMapPointOutline(mapPointNode),
				highlighted);
		return true;
	}

	public static bool TryAnimateSecretNodeHover(NNormalMapPoint mapPointNode)
	{
		if (!IsSecretMapPointNode(mapPointNode))
		{
			return false;
		}

			TextureRect iconRect = IntegratedStrategyMapReflectionCache.NormalMapPointIcon(mapPointNode);
			TextureRect questIconRect = IntegratedStrategyMapReflectionCache.NormalMapPointQuestIcon(mapPointNode);
			TextureRect outlineRect = IntegratedStrategyMapReflectionCache.NormalMapPointOutline(mapPointNode);
			ref Tween? tweenRef = ref IntegratedStrategyMapReflectionCache.NormalMapPointTween(mapPointNode);
			Tween? tween = tweenRef;
			tween?.Kill();
			tween = mapPointNode.CreateTween().SetParallel();
			tweenRef = tween;
		tween.TweenProperty(iconRect, "scale", SecretMapHoverScale, 0.05);
		tween.TweenProperty(questIconRect, "scale", SecretMapHoverScale, 0.05);
		iconRect.Modulate = Colors.White;
		tween.TweenProperty(iconRect, "self_modulate", Colors.White, 0.05);
		tween.TweenProperty(outlineRect, "modulate", GetOpaqueMapBackgroundColor(), 0.05);
		ApplySecretOutlineBaseOpacity(mapPointNode, outlineRect);
		return true;
	}

	public static bool TryAnimateSecretNodeUnhover(NNormalMapPoint mapPointNode)
	{
		if (!IsSecretMapPointNode(mapPointNode))
		{
			return false;
		}

			TextureRect iconRect = IntegratedStrategyMapReflectionCache.NormalMapPointIcon(mapPointNode);
			TextureRect questIconRect = IntegratedStrategyMapReflectionCache.NormalMapPointQuestIcon(mapPointNode);
			TextureRect outlineRect = IntegratedStrategyMapReflectionCache.NormalMapPointOutline(mapPointNode);
			ref Tween? tweenRef = ref IntegratedStrategyMapReflectionCache.NormalMapPointTween(mapPointNode);
			Tween? tween = tweenRef;
			tween?.Kill();
			tween = mapPointNode.CreateTween().SetParallel();
			tweenRef = tween;
		tween.TweenProperty(iconRect, "scale", Vector2.One, 0.5)
			.SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Cubic);
		tween.TweenProperty(questIconRect, "scale", Vector2.One, 0.5)
			.SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Cubic);
		iconRect.Modulate = Colors.White;
		tween.TweenProperty(iconRect, "self_modulate", GetIconSelfModulate(mapPointNode, highlighted: false), 0.5)
			.SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Cubic);
		tween.TweenProperty(outlineRect, "modulate", GetOpaqueMapBackgroundColor(), 0.5)
			.SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Cubic);
		ApplySecretOutlineBaseOpacity(mapPointNode, outlineRect);
		return true;
	}

	public static bool IsSecretMapPointNode(NNormalMapPoint mapPointNode)
	{
		RunState? state = RunManager.Instance.DebugOnlyGetState();
		MapPoint? point = mapPointNode.Point;
		return state != null && point != null && IsSecretNode(state, point);
	}

	public static bool IsSecretLegendHighlightType(MapPointType pointType)
	{
		return pointType == SecretLegendHighlightPointType;
	}

	public static bool TryForceNextTreeHoleEvent(RoomSet roomSet, RunState state)
	{
		if (!IsAtSecretNode(state) || roomSet.events.Count == 0)
		{
			return false;
		}

		if (IntegratedStrategyEventReplay.TryRestoreSavedCurrentEvent(
				roomSet,
				state,
				IntegratedStrategyEventReplay.IsSecondActOpeningBranch,
				"saved second-act opening event on secret node"))
		{
			return true;
		}

		int desiredIndex = roomSet.eventsVisited % roomSet.events.Count;
		if (IsAtProphetHornSecretNode(state))
		{
			if (IntegratedStrategyEventReplay.TryRestoreSavedCurrentEvent(
					roomSet,
					state,
					static eventModel => eventModel is TruthToBeToldEvent,
					"saved Prophet Horn secret node event"))
			{
				return true;
			}

			RoomSet.SwapToOrCreateAtIndex<EventModel, TruthToBeToldEvent>(roomSet.events, desiredIndex);
			Log.Info($"{ModInfo.LogPrefix} Prophet Horn secret map node forced next event to TRUTH_TO_BE_TOLD.");
			return true;
		}

		if (IntegratedStrategyEventReplay.TryRestoreSavedCurrentEvent(
				roomSet,
				state,
				IntegratedStrategyEventReplay.IsTreeHoleEvent,
				"saved secret node event"))
		{
			return true;
		}

		Type selectedType = SelectTreeHoleEventType(state);
		SwapNextEvent(roomSet, desiredIndex, selectedType);
		Log.Info(
			$"{ModInfo.LogPrefix} Secret map node forced next event to " +
			$"{ModelDb.GetId(selectedType).Entry}.");
		return true;
	}

	public static bool TryGetForcedEventType(RunState state, out Type eventType)
	{
		if (!IsAtSecretNode(state) ||
			IntegratedStrategyFirstEventPatch.ShouldForceSecondActOpeningEvent(state))
		{
			eventType = null!;
			return false;
		}

		if (IsAtProphetHornSecretNode(state))
		{
			eventType = typeof(TruthToBeToldEvent);
			return true;
		}

		eventType = SelectTreeHoleEventType(state);
		return true;
	}

	private static bool IsSecretNode(RunState state, ActMap map, int actIndex, MapCoord coord)
	{
		if (ShouldSkipMap(state, map) ||
			map.GetPoint(coord) is not { PointType: MapPointType.Unknown })
		{
			return false;
		}

		SecretNodeStore store = SecretNodes.GetOrCreateValue(state);
		if (!store.ContainsAct(actIndex))
		{
			store.Set(actIndex, SelectSecretNodeCoords(state, map, actIndex));
		}

		return store.Contains(actIndex, coord);
	}

	private static bool IsSecretNode(RunState state, MapPoint point)
	{
		return point.PointType == MapPointType.Unknown &&
			IsSecretNode(state, state.Map, state.CurrentActIndex, point.coord);
	}

	private static bool TryGetCurrentUnknownMapPoint(RunState state, out MapPoint point)
	{
		point = null!;
		if (!state.CurrentMapCoord.HasValue)
		{
			return false;
		}

		MapCoord coord = state.CurrentMapCoord.Value;
		if (state.Map.GetPoint(coord) is not { PointType: MapPointType.Unknown } currentPoint)
		{
			return false;
		}

		point = currentPoint;
		return true;
	}

	private static bool ShouldSkipMap(RunState state, ActMap map)
	{
		return IntegratedStrategyTreeHoleController.IsActive(state) ||
			map is IntegratedStrategyTreeHoleActMap ||
			map is IntegratedStrategyEndlessFinaleActMap ||
			map is IntegratedStrategyEternalDustFinaleActMap ||
			map is IntegratedStrategyRadiantApexFinaleActMap ||
			map is IntegratedStrategyCarefreeViharaFinaleActMap ||
			map is IntegratedStrategyAbyssalJungleFinaleActMap ||
			map is IntegratedStrategyProphetHornFragmentActMap ||
			IntegratedStrategyTreeHoleController.IsCurrentTreeHoleMap(map) ||
			IntegratedStrategyTreeHoleController.IsCurrentEndlessFinaleMap(map) ||
			IntegratedStrategyTreeHoleController.IsCurrentEternalDustFinaleMap(map) ||
			IntegratedStrategyTreeHoleController.IsCurrentRadiantApexFinaleMap(map) ||
			IntegratedStrategyTreeHoleController.IsCurrentCarefreeViharaFinaleMap(map) ||
			IntegratedStrategyTreeHoleController.IsCurrentAbyssalJungleFinaleMap(map) ||
			IntegratedStrategyTreeHoleController.IsCurrentProphetHornFragmentMap(map);
	}

	private static HashSet<MapCoord> SelectSecretNodeCoords(RunState state, ActMap map, int actIndex)
	{
		if (TryGetProphetHornSecretNodeCoord(state, map, actIndex, out MapCoord prophetHornCoord))
		{
			return [prophetHornCoord];
		}

		List<MapPoint> candidates = map.GetAllMapPoints()
			.Where(static point => point.PointType == MapPointType.Unknown && point.CanBeModified)
			.OrderBy(static point => point.coord.row)
			.ThenBy(static point => point.coord.col)
			.ToList();
		if (candidates.Count == 0)
		{
			return [];
		}

		Rng rng = new(CreateSecretNodeSeed(state, actIndex), "integrated_strategy_secret_map_nodes");
		rng.Shuffle(candidates);
		int maxCount = Math.Min(MaximumSecretNodes, candidates.Count);
		int count = rng.NextInt(MinimumSecretNodes, maxCount + 1);
		return candidates.Take(count).Select(static point => point.coord).ToHashSet();
	}

	private static Type SelectTreeHoleEventType(RunState state)
	{
		MapCoord coord = state.CurrentMapCoord ?? default;
		uint seed = IntegratedStrategyStableRng.CreateSeed(
			state.Rng.Seed,
			"integrated_strategy_secret_tree_hole_event",
			unchecked((uint)state.CurrentActIndex),
			IntegratedStrategyStableRng.HashCoord(coord));
		Rng rng = new(seed, "integrated_strategy_secret_tree_hole_event");
		Type[] availableTypes = TreeHoleEventTypes
			.Where(eventType => CanTreeHoleEventEnterForAllPlayers(state, eventType))
			.ToArray();
		Type[] candidates = availableTypes
			.Where(eventType => !state.VisitedEventIds.Contains(ModelDb.GetId(eventType)))
			.ToArray();

		if (candidates.Length == 0)
		{
			candidates = availableTypes;
		}

		return candidates[rng.NextInt(candidates.Length)];
	}

	private static bool CanTreeHoleEventEnterForAllPlayers(RunState state, Type eventType)
	{
		if (eventType == typeof(ForwardForestEvent))
		{
			return ForwardForestEvent.CanEnterTreeHoleForAllPlayers(state);
		}

		if (eventType == typeof(GlimpseEvent))
		{
			return GlimpseEvent.CanEnterTreeHoleForAllPlayers(state);
		}

		return true;
	}

	private static uint CreateSecretNodeSeed(RunState state, int actIndex)
	{
		return IntegratedStrategyStableRng.CreateSeed(
			state.Rng.Seed,
			"integrated_strategy_secret_map_nodes",
			unchecked((uint)actIndex));
	}

	private static bool TryGetProphetHornSecretNodeCoord(
		RunState state,
		ActMap map,
		int actIndex,
		out MapCoord coord)
	{
		coord = default;
		return actIndex == ProphetHornRelic.TargetActIndex &&
			ProphetHornRelic.IsActiveInRun(state) &&
			ProphetHornActMap.TryGetSecretCoord(map, out coord);
	}

	private static void SwapNextEvent(RoomSet roomSet, int desiredIndex, Type selectedType)
	{
		if (selectedType == typeof(StoryToBeToldEvent))
		{
			RoomSet.SwapToOrCreateAtIndex<EventModel, StoryToBeToldEvent>(roomSet.events, desiredIndex);
			return;
		}

		if (selectedType == typeof(TruthToBeToldEvent))
		{
			RoomSet.SwapToOrCreateAtIndex<EventModel, TruthToBeToldEvent>(roomSet.events, desiredIndex);
			return;
		}

		if (selectedType == typeof(ShiftingCityEvent))
		{
			RoomSet.SwapToOrCreateAtIndex<EventModel, ShiftingCityEvent>(roomSet.events, desiredIndex);
			return;
		}

		if (selectedType == typeof(GlimpseEvent))
		{
			RoomSet.SwapToOrCreateAtIndex<EventModel, GlimpseEvent>(roomSet.events, desiredIndex);
			return;
		}

		RoomSet.SwapToOrCreateAtIndex<EventModel, ForwardForestEvent>(roomSet.events, desiredIndex);
	}

	public static Texture2D? GetSecretIcon()
	{
		return _secretIcon ??= LoadTexture(SecretIconPath);
	}

	private static Texture2D? GetSecretOutline()
	{
		return _secretOutline ??= LoadTexture(SecretOutlinePath);
	}

	private static Texture2D? LoadTexture(string path)
	{
		Texture2D? texture = ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
		if (texture == null)
		{
			Log.Warn($"{ModInfo.LogPrefix} Failed to load secret map node texture: {path}");
		}

		return texture;
	}

	private static string FormatCoord(MapCoord coord)
	{
		return $"({coord.col},{coord.row})";
	}

	private static void ResizeAroundCenter(TextureRect rect, Vector2 targetSize)
	{
		ResizeAroundCenter(rect, targetSize, rect.Position + rect.Size * 0.5f);
	}

	private static void ResizeAroundCenter(TextureRect rect, Vector2 targetSize, Vector2 center)
	{
		rect.CustomMinimumSize = targetSize;
		rect.Size = targetSize;
		rect.Position = center - targetSize * 0.5f;
		rect.PivotOffset = targetSize * 0.5f;
		rect.Scale = Vector2.One;
	}

	private static void ResizeSecretOutlineAroundIconCenter(TextureRect outlineRect, TextureRect iconRect)
	{
		// Outline is a child of Icon in the vanilla map point scene.
		ResizeAroundCenter(outlineRect, SecretMapOutlineSize, iconRect.Size * 0.5f);
	}

	private static Vector2 GetIconContainerCenter(NNormalMapPoint mapPointNode, TextureRect iconRect)
	{
		if (iconRect.GetParent() is Control iconContainer && iconContainer.Size != Vector2.Zero)
		{
			return iconContainer.Size * 0.5f;
		}

		if (mapPointNode.Size != Vector2.Zero)
		{
			return mapPointNode.Size * 0.5f;
		}

		return mapPointNode.PivotOffset != Vector2.Zero ? mapPointNode.PivotOffset : new Vector2(28f, 28f);
	}

	private static void ApplySecretNodeOpacity(
		NNormalMapPoint mapPointNode,
		TextureRect iconRect,
		TextureRect outlineRect,
		bool highlighted = false)
	{
		iconRect.Modulate = Colors.White;
		iconRect.SelfModulate = GetIconSelfModulate(mapPointNode, highlighted);
		outlineRect.Modulate = GetOpaqueMapBackgroundColor();
		ApplySecretOutlineBaseOpacity(mapPointNode, outlineRect);
	}

	private static void ApplySecretOutlineBaseOpacity(NNormalMapPoint mapPointNode, TextureRect outlineRect)
	{
			ref Color outlineColorRef = ref IntegratedStrategyMapReflectionCache.MapPointOutlineColor(mapPointNode);
			Color outlineColor = outlineColorRef;
			outlineColor.A = 1f;
			outlineColorRef = outlineColor;

		Color modulate = outlineRect.Modulate;
		modulate.A = 1f;
		outlineRect.Modulate = modulate;

		Color selfModulate = outlineRect.SelfModulate;
		selfModulate.A = 1f;
		outlineRect.SelfModulate = selfModulate;
	}

	private static Color GetIconSelfModulate(NNormalMapPoint mapPointNode, bool highlighted)
	{
		return highlighted || mapPointNode.State is MapPointState.Travelable or MapPointState.Traveled
			? Colors.White
			: StsColors.halfTransparentWhite;
	}

	private static Color GetOpaqueMapBackgroundColor()
	{
		Color color = RunManager.Instance.DebugOnlyGetState()?.Act.MapBgColor ?? Colors.White;
		color.A = 1f;
		return color;
	}

	private sealed class SecretNodeStore
	{
		private readonly Dictionary<int, HashSet<MapCoord>> _coordsByAct = [];

		public bool ContainsAct(int actIndex)
		{
			return _coordsByAct.ContainsKey(actIndex);
		}

		public void Set(int actIndex, HashSet<MapCoord> coords)
		{
			_coordsByAct[actIndex] = coords;
		}

		public bool Contains(int actIndex, MapCoord coord)
		{
			return _coordsByAct.TryGetValue(actIndex, out HashSet<MapCoord>? coords) &&
				coords.Contains(coord);
		}
	}
}

[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Hooks.Hook), nameof(MegaCrit.Sts2.Core.Hooks.Hook.AfterMapGenerated))]
internal static class IntegratedStrategyTreeHoleEarlyRestorePatch
{
	[HarmonyPriority(Priority.First)]
	private static void Prefix(IRunState runState, ActMap map, int actIndex)
	{
		IntegratedStrategyTreeHoleController.TryRestoreSavedSessionForCurrentRun(map);
	}
}

[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Hooks.Hook), nameof(MegaCrit.Sts2.Core.Hooks.Hook.AfterMapGenerated))]
internal static class IntegratedStrategySecretMapNodeGenerationPatch
{
	private static void Prefix(IRunState runState, ActMap map, int actIndex)
	{
		IntegratedStrategySecretMapNodeController.MarkSecretNodes(runState, map, actIndex);
	}
}

[HarmonyPatch(typeof(RoomSet), nameof(RoomSet.EnsureNextEventIsValid))]
internal static class IntegratedStrategySecretMapNodeEventPatch
{
	[HarmonyPriority(Priority.First)]
	private static bool Prefix(RoomSet __instance, RunState runState)
	{
		if (!IntegratedStrategySecretMapNodeController.IsAtSecretNode(runState))
		{
			return true;
		}

		if (IntegratedStrategyFirstEventPatch.ShouldForceSecondActOpeningEvent(runState))
		{
			Log.Info($"{ModInfo.LogPrefix} Secret map node deferred to second-act opening event.");
			return true;
		}

		return !IntegratedStrategySecretMapNodeController.TryForceNextTreeHoleEvent(__instance, runState);
	}
}

[HarmonyPatch(typeof(NNormalMapPoint), "UpdateIcon")]
internal static class IntegratedStrategySecretMapNodeIconPatch
{
	private static void Postfix(NNormalMapPoint __instance)
	{
		IntegratedStrategySecretMapNodeController.TryApplySecretNodeIcon(__instance);
	}
}

[HarmonyPatch(typeof(NNormalMapPoint), "RefreshColorInstantly")]
internal static class IntegratedStrategySecretMapNodeColorPatch
{
	private static void Postfix(NNormalMapPoint __instance)
	{
		IntegratedStrategySecretMapNodeController.TryApplySecretNodeOpacity(__instance);
	}
}

[HarmonyPatch(typeof(NNormalMapPoint), "AnimHover")]
internal static class IntegratedStrategySecretMapNodeHoverPatch
{
	private static bool Prefix(NNormalMapPoint __instance)
	{
		return !IntegratedStrategySecretMapNodeController.TryAnimateSecretNodeHover(__instance);
	}
}

[HarmonyPatch(typeof(NNormalMapPoint), "AnimUnhover")]
internal static class IntegratedStrategySecretMapNodeUnhoverPatch
{
	private static bool Prefix(NNormalMapPoint __instance)
	{
		return !IntegratedStrategySecretMapNodeController.TryAnimateSecretNodeUnhover(__instance);
	}
}
