using HarmonyLib;
using IntegratedStrategyEvents.Encounters;
using IntegratedStrategyEvents.Relics;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace IntegratedStrategyEvents.Map;

internal static class PetalSpecialEliteNodeController
{
	public static bool IsAtSpecialEliteNode(RunState state)
	{
		return state.CurrentMapCoord.HasValue &&
			TryGetSpecialEliteNodeCoord(state, state.Map, state.CurrentActIndex, out MapCoord coord) &&
			state.CurrentMapCoord.Value.Equals(coord);
	}

	public static bool TryPullForcedEncounter(RoomType roomType, out EncounterModel encounter)
	{
		encounter = null!;
		if (roomType != RoomType.Elite ||
			RunManager.Instance.DebugOnlyGetState() is not RunState state ||
			!IsAtSpecialEliteNode(state))
		{
			return false;
		}

		encounter = ModelDb.GetById<EncounterModel>(ModelDb.GetId<ReincarnationLotusDuoEncounter>());
		Log.Info($"{ModInfo.LogPrefix} Petal special elite node forced next encounter to REINCARNATION_LOTUS_DUO_ENCOUNTER.");
		return true;
	}

	private static bool TryGetSpecialEliteNodeCoord(
		RunState state,
		ActMap map,
		int actIndex,
		out MapCoord coord)
	{
		coord = default;
		return actIndex == PetalRelic.TargetActIndex &&
			PetalRelic.IsActiveInRun(state) &&
			PetalActMap.TryGetSpecialEliteCoord(map, out coord);
	}
}

[HarmonyPatch(typeof(ActModel), nameof(ActModel.PullNextEncounter))]
internal static class PetalSpecialEliteNodeEncounterPatch
{
	[HarmonyPriority(Priority.First)]
	private static bool Prefix(RoomType roomType, ref EncounterModel __result)
	{
		if (!PetalSpecialEliteNodeController.TryPullForcedEncounter(roomType, out EncounterModel encounter))
		{
			return true;
		}

		__result = encounter;
		return false;
	}
}
