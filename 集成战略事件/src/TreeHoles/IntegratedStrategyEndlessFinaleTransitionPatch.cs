using HarmonyLib;
using IntegratedStrategyEvents.Map;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace IntegratedStrategyEvents.TreeHoles;

[HarmonyPatch(typeof(RunManager), nameof(RunManager.EnterNextAct))]
internal static class IntegratedStrategyEndlessFinaleEnterNextActPatch
{
	[HarmonyPriority(Priority.First)]
	[HarmonyBefore("Act4Placeholder")]
	private static bool Prefix(RunManager __instance, ref Task __result)
	{
		return IntegratedStrategyTreeHoleController.HandleEnterNextAct(__instance, ref __result);
	}
}

// 0.108 起 ActChangeSynchronizer.OnPlayerReady 新增"已从当前幕转换过则忽略"守卫
// （_lastTransitioningActIndex），只对 TheArchitect 事件房（IsVictoryRoom）豁免。
// 模组终局插层需要从同一幕序号二次转换（三幕BOSS→进终局消耗一次，终局BOSS→回建筑师再一次），
// 第二次会被该守卫吞掉（表现为打完终局BOSS点继续无反应）。终局会话激活或建筑师交接待办时
// 重置该记忆字段放行；真正过期投票的 actIndex < CurrentActIndex 防护不受影响。
[HarmonyPatch(typeof(ActChangeSynchronizer), nameof(ActChangeSynchronizer.OnPlayerReady))]
internal static class IntegratedStrategyFinaleActChangeGuardPatch
{
	private static readonly AccessTools.FieldRef<ActChangeSynchronizer, int> LastTransitioningActIndexRef =
		AccessTools.FieldRefAccess<ActChangeSynchronizer, int>("_lastTransitioningActIndex");

	[HarmonyPriority(Priority.First)]
	private static void Prefix(ActChangeSynchronizer __instance)
	{
		if (IntegratedStrategyTreeHoleController.ShouldAllowRepeatedActTransition())
		{
			ResetTransitionMemory(__instance);
		}
	}

	internal static void ResetTransitionMemory(ActChangeSynchronizer synchronizer)
	{
		LastTransitioningActIndexRef(synchronizer) = -1;
	}
}

[HarmonyPatch(typeof(EventModel), "SetEventState")]
internal static class IntegratedStrategyEndlessFinaleArchitectOptionsPatch
{
	[HarmonyPriority(Priority.Last)]
	[HarmonyAfter("Act4Placeholder")]
	private static void Postfix(EventModel __instance)
	{
		IntegratedStrategyTreeHoleController.SuppressArchitectActChangeOptions(__instance);
	}
}

[HarmonyPatch(typeof(NEventLayout), nameof(NEventLayout.AddOptions))]
internal static class IntegratedStrategyEndlessFinaleArchitectOptionDisplayPatch
{
	[HarmonyPriority(Priority.Last)]
	[HarmonyAfter("Act4Placeholder")]
	private static void Prefix(EventModel ____event, ref IEnumerable<EventOption> options)
	{
		options = IntegratedStrategyTreeHoleController.FilterArchitectActChangeOptionsForDisplay(
			____event,
			options);
	}
}

[HarmonyPatch(typeof(NEventRoom), nameof(NEventRoom.OptionButtonClicked))]
internal static class IntegratedStrategyEndlessFinaleArchitectOptionClickPatch
{
	[HarmonyPriority(Priority.First)]
	[HarmonyBefore("Act4Placeholder")]
	private static bool Prefix(EventModel ____event, EventOption option)
	{
		return IntegratedStrategyTreeHoleController.ShouldChooseArchitectOption(____event, option);
	}
}

[HarmonyPatch(typeof(RunManager), "CreateRoom")]
internal static class IntegratedStrategyEndlessFinaleCreateRoomPatch
{
	private static bool Prefix(
		ref RoomType roomType,
		MapPointType mapPointType,
		AbstractModel? model,
		ref AbstractRoom __result)
	{
		if (!IntegratedStrategyForcedRoomController.HandleCreateRoom(roomType, mapPointType, model, ref __result))
		{
			return false;
		}

		return IntegratedStrategyTreeHoleController.HandleCreateRoom(roomType, model, ref __result);
	}

	[HarmonyPriority(Priority.Last)]
	private static void Postfix(
		RoomType roomType,
		AbstractModel? model,
		ref AbstractRoom __result)
	{
		IntegratedStrategyTreeHoleController.EnsureCreatedRoomIsEndlessFinaleBoss(roomType, model, ref __result);
	}
}

[HarmonyPatch(typeof(NBossMapPoint), nameof(NBossMapPoint._Ready))]
internal static class IntegratedStrategyEndlessFinaleBossMapPointPatch
{
	private static void Prefix(NBossMapPoint __instance, out BossNodeRenderSwap? __state)
	{
		__state = IntegratedStrategyTreeHoleController.BeginEndlessFinaleBossNodeRender(__instance.Point);
	}

	private static void Postfix(BossNodeRenderSwap? __state)
	{
		IntegratedStrategyTreeHoleController.EndEndlessFinaleBossNodeRender(__state);
	}
}
