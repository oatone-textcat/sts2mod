using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;

namespace IntegratedStrategyEvents.TreeHoles;

internal static class TreeHoleEntryCoordinator
{
	public static Task EnterFromEvent(Player owner)
	{
		return EnterFromEvent(owner, TreeHoleConstants.DeepBuriedActName, TreeHoleConstants.DeepBuriedStageLabel);
	}

	public static Task EnterFromEvent(Player owner, string destinationActName)
	{
		return EnterFromEvent(owner, destinationActName, TreeHoleConstants.UnknownStageLabel);
	}

	public static Task EnterFromEvent(Player owner, string destinationActName, string stageLabel)
	{
		_ = TaskHelper.RunSafely(EnterFromEventDeferred(owner, destinationActName, stageLabel));
		return Task.CompletedTask;
	}

	public static Task EnterFromDebugCommand(Player owner, string destinationActName, string stageLabel)
	{
		return EnterFromEventDeferred(owner, destinationActName, stageLabel);
	}

	private static async Task EnterFromEventDeferred(Player owner, string destinationActName, string stageLabel)
	{
		RunManager runManager = RunManager.Instance;
		if (owner.RunState is not RunState state)
		{
			Log.Warn($"{ModInfo.LogPrefix} Tried to enter a tree-hole without a run state.");
			return;
		}

		if (TreeHoleSessionManager.IsActive(state))
		{
			Log.Warn($"{ModInfo.LogPrefix} Tried to enter a tree-hole while one is already active.");
			return;
		}

		await TreeHoleSessionManager.AwaitNextProcessFrame();

		if (!ReferenceEquals(runManager.DebugOnlyGetState(), state))
		{
			Log.Warn($"{ModInfo.LogPrefix} Tree-hole entry was cancelled because the active run changed.");
			return;
		}

		if (TestMode.IsOff && NGame.Instance != null)
		{
			await NGame.Instance.Transition.RoomFadeOut();
		}

		Log.Info($"{ModInfo.LogPrefix} Preparing to enter {destinationActName} tree-hole.");
		await TreeHoleRunAccessor.ExitCurrentRooms(runManager);
		TreeHoleRunAccessor.ClearScreens(runManager);
		Rng treeHoleRng = new(
			TreeHoleSeedFactory.CreateTreeHoleMapSeed(state, destinationActName, stageLabel),
			"integrated_strategy_tree_hole_map");
		IntegratedStrategyTreeHoleActMap treeHoleMap = IntegratedStrategyTreeHoleActMap.Create(treeHoleRng);
		TreeHoleSession session = new(
			state.Map,
			state.VisitedMapCoords.ToList(),
			state.MapPointHistory.Select(static history => history.ToList()).ToList(),
			state.ActFloor,
			stageLabel,
			destinationActName,
			treeHoleMap,
			treeHoleMap.TerminalCoord);

		TreeHoleSessionManager.SetTreeHoleSession(state, session);
		state.Map = treeHoleMap;
		state.ClearVisitedMapCoordsDebug();
		state.AddVisitedMapCoord(treeHoleMap.StartingMapPoint.coord);
		TreeHoleSessionManager.RefreshLocationSynchronizers(state);
		TreeHoleSessionManager.SetMapScreen(treeHoleMap, state, initMarker: false);

		Log.Info($"{ModInfo.LogPrefix} Entering {destinationActName} tree-hole.");
		await TreeHoleRunAccessor.EnterRoomInternal(runManager, new MapRoom());
		Log.Info($"{ModInfo.LogPrefix} Entered {destinationActName} tree-hole map room.");
		await TreeHoleRunAccessor.FadeIn(runManager, showTransition: true);
	}
}
